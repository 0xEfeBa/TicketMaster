using Microsoft.AspNetCore.RateLimiting;
using TicketFlow.BuildingBlocks.Observability;
using TicketFlow.BuildingBlocks.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddTicketFlowObservability("TicketFlow.Gateway");

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Length > 0)
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
        else
            policy.SetIsOriginAllowed(_ => false);
    });
});

builder.Services.AddRateLimiter(options =>
{
    var permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 50);
    var queueLimit = builder.Configuration.GetValue("RateLimiting:QueueLimit", 10);
    var fixedWindowSeconds = builder.Configuration.GetValue("RateLimiting:FixedWindowSeconds", 10);

    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(fixedWindowSeconds);
        opt.PermitLimit = permitLimit;
        opt.QueueLimit = queueLimit;
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseCors();
app.UseRateLimiter();

app.MapHealthChecks("/health");

app.MapGet("/health/details", () => Results.Ok(new { service = "TicketFlow.Gateway", status = "ok" }))
    .WithName("Health")
    .WithOpenApi();

app.MapReverseProxy();

app.Run();