using System.Security.Claims;
using System.Text;
using Catalog.Api.Filters;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TicketFlow.BuildingBlocks.Observability;
using TicketFlow.BuildingBlocks.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddTicketFlowObservability("Catalog.Api");

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers(options => options.Filters.Add(new CatalogDomainExceptionFilter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];

builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
{
    options.MapInboundClaims = false;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer),
        ValidIssuer = jwtIssuer,
        ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = !string.IsNullOrWhiteSpace(jwtSigningKey),
        IssuerSigningKey = string.IsNullOrWhiteSpace(jwtSigningKey)
            ? null
            : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
        RoleClaimType = ClaimTypes.Role
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrganizerOrAdmin", p => p.RequireRole("Organizer", "Admin"));
});

var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
var kafkaSaslUsername = builder.Configuration["Kafka:SaslUsername"];
var kafkaSaslPassword = builder.Configuration["Kafka:SaslPassword"];
var kafkaHealthConfig = new Confluent.Kafka.ProducerConfig
{
    BootstrapServers = kafkaBootstrapServers
};

if (!string.IsNullOrWhiteSpace(kafkaSaslUsername) && !string.IsNullOrWhiteSpace(kafkaSaslPassword))
{
    kafkaHealthConfig.SecurityProtocol = Confluent.Kafka.SecurityProtocol.SaslPlaintext;
    kafkaHealthConfig.SaslMechanism = Confluent.Kafka.SaslMechanism.Plain;
    kafkaHealthConfig.SaslUsername = kafkaSaslUsername;
    kafkaHealthConfig.SaslPassword = kafkaSaslPassword;
}

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Catalog")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379")
    .AddKafka(kafkaHealthConfig);


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await dbContext.Database.MigrateAsync();
    await CatalogDbSeeder.SeedAsync(dbContext);
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/health/details", () => "Catalog Service is Healthy!");

app.Run();
