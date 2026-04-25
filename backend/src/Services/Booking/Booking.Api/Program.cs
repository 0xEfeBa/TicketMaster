using System.Security.Claims;
using System.Text;
using Booking.Api.Filters;
using Booking.Application;
using Booking.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using TicketFlow.BuildingBlocks.Observability;
using TicketFlow.BuildingBlocks.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddTicketFlowObservability("Booking.Api");

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers(options => options.Filters.Add(new BookingExceptionFilter()));
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
builder.Services.AddAuthorization();

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
    .AddNpgSql(builder.Configuration.GetConnectionString("Booking")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379")
    .AddKafka(kafkaHealthConfig);


var app = builder.Build();

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
app.MapGet("/health/details", () => "Booking Service is Healthy!");

app.Run();
