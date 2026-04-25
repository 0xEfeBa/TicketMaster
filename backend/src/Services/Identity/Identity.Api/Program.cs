using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Identity.Application;
using Identity.Application.Abstractions;
using Identity.Application.Admin;
using Identity.Application.Auth;
using Identity.Application.Users;
using Identity.Domain.Enums;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Redis;
using Identity.Infrastructure.Security;
using Identity.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TicketFlow.BuildingBlocks.Observability;
using TicketFlow.BuildingBlocks.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddTicketFlowObservability("Identity.Api");

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var signingKey = jwtSection["SigningKey"] ?? string.Empty;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtSection["Issuer"]),
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtSection["Audience"]),
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                          ?? context.Principal?.FindFirst("jti")?.Value;
                if (string.IsNullOrEmpty(jti))
                    return;
                var blacklist = context.HttpContext.RequestServices.GetRequiredService<IAccessTokenBlacklist>();
                if (await blacklist.ContainsAsync(jti, context.HttpContext.RequestAborted))
                    context.Fail("token_revoked");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole(nameof(UserRole.Admin)));
});

var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
    ?? builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Identity")!)
    .AddRedis(redisConnectionString);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Identity.Api", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Authorization: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var jsonCacheOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await db.Database.MigrateAsync();
    await IdentityDbSeeder.SeedAdminIfConfiguredAsync(app.Services, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RedisAuthRateLimitMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapGet("/health/details", () => Results.Ok(new { service = "Identity.Api", status = "ok" }))
    .WithName("Health")
    .WithOpenApi();

app.MapPost("/api/v1/auth/register",
        async (RegisterRequest body, RegisterUserHandler register, IAuthTokenPairIssuer pair, CancellationToken ct) =>
        {
            var (user, err) = await register.HandleAsync(body, ct);
            if (err is not null)
            {
                return err switch
                {
                    "email_taken" => Results.Conflict(new { error = err }),
                    "invalid_email" or "invalid_password" => Results.BadRequest(new { error = err }),
                    _ => Results.BadRequest(new { error = err })
                };
            }

            var response = await pair.IssueForUserAsync(user!, ct);
            return Results.Ok(response);
        })
    .WithName("Register")
    .WithOpenApi();

app.MapPost("/api/v1/auth/login",
        async (LoginRequest body, LoginUserHandler login, IAuthTokenPairIssuer pair, CancellationToken ct) =>
        {
            var user = await login.HandleAsync(body.Email, body.Password, ct);
            if (user is null)
                return Results.Json(new { error = "invalid_credentials" }, statusCode: 401);

            var response = await pair.IssueForUserAsync(user, ct);
            return Results.Ok(response);
        })
    .WithName("Login")
    .WithOpenApi();

app.MapPost("/api/v1/auth/refresh",
        async (RefreshTokenRequest body, RefreshTokenHandler handler, CancellationToken ct) =>
        {
            var (response, err) = await handler.HandleAsync(body.RefreshToken, ct);
            if (err is not null)
                return Results.Json(new { error = err }, statusCode: 401);
            return Results.Ok(response);
        })
    .WithName("RefreshToken")
    .WithOpenApi();

app.MapPost("/api/v1/auth/logout",
        async (ClaimsPrincipal principal, LogoutRequest? body, ILogoutService logout, CancellationToken ct) =>
        {
            await logout.ExecuteAsync(principal, body?.RefreshToken, ct);
            return Results.NoContent();
        })
    .RequireAuthorization()
    .WithName("Logout")
    .WithOpenApi();

app.MapGet("/api/v1/users/me",
        async (
            ClaimsPrincipal principal,
            GetCurrentUserHandler handler,
            IDistributedCache cache,
            IOptions<RedisOptions> redisOpts,
            CancellationToken ct) =>
        {
            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (sub is null || !Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            var cacheKey = LogoutService.UserMeCacheKey(userId);
            var bytes = await cache.GetAsync(cacheKey, ct);
            if (bytes is not null)
            {
                var me = JsonSerializer.Deserialize<UserMeResponse>(bytes, jsonCacheOptions);
                if (me is not null)
                    return Results.Ok(me);
            }

            var fresh = await handler.HandleAsync(userId, ct);
            if (fresh is null)
                return Results.NotFound();

            var payload = JsonSerializer.SerializeToUtf8Bytes(fresh, jsonCacheOptions);
            await cache.SetAsync(
                cacheKey,
                payload,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(
                        Math.Max(10, redisOpts.Value.UserProfileCacheSeconds))
                },
                ct);

            return Results.Ok(fresh);
        })
    .RequireAuthorization()
    .WithName("Me")
    .WithOpenApi();

app.MapPut("/api/v1/admin/users/{userId:guid}/role",
        async (
            Guid userId,
            AssignRoleRequest body,
            AssignRoleHandler handler,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            if (! Enum.TryParse<UserRole>(body.Role, ignoreCase: true, out var role))
                return Results.BadRequest(new { error = "invalid_role" });

            var (ok, err) = await handler.HandleAsync(userId, role, ct);
            if (! ok)
            {
                return err switch
                {
                    "user_not_found" => Results.NotFound(new { error = err }),
                    "admin_protected" or "admin_role_forbidden" => Results.Json(new { error = err },
                        statusCode: StatusCodes.Status403Forbidden),
                    _ => Results.BadRequest(new { error = err })
                };
            }

            await cache.RemoveAsync(LogoutService.UserMeCacheKey(userId), ct);
            return Results.NoContent();
        })
    .RequireAuthorization("Admin")
    .WithName("AssignRole")
    .WithOpenApi();

app.Run();
