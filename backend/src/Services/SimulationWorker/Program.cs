using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TicketFlow.SimulationWorker;

var builder = Host.CreateApplicationBuilder(args);

const string serviceName = "TicketFlow.SimulationWorker";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
            .SetSampler(new AlwaysOnSampler())
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options => OtlpExporterConfiguration.Apply(options, builder.Configuration));
    });

var gatewayUri = new Uri(builder.Configuration["ApiGateway:BaseAddress"] ?? "http://gateway:8080");

builder.Services.Configure<SimulationOptions>(builder.Configuration.GetSection("Simulation"));

builder.Services.AddHttpClient("GatewayClient", client =>
{
    client.BaseAddress = gatewayUri;
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TicketFlow-SimulationWorker/2.0");
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
