using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace TicketFlow.BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    private static void ApplyOtlpExporterOptions(OtlpExporterOptions options, IConfiguration configuration)
    {
        var raw = configuration["Otlp:Endpoint"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (string.IsNullOrWhiteSpace(raw))
            raw = "http://localhost:4317";
        var uri = new Uri(raw);
        options.Endpoint = uri;

        var protocolRaw = configuration["Otlp:Protocol"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
        options.Protocol = ResolveOtlpProtocol(protocolRaw, uri.Port);
    }

    private static OtlpExportProtocol ResolveOtlpProtocol(string? protocolRaw, int port)
    {
        if (!string.IsNullOrWhiteSpace(protocolRaw))
        {
            if (string.Equals(protocolRaw, "grpc", StringComparison.OrdinalIgnoreCase))
                return OtlpExportProtocol.Grpc;
            if (string.Equals(protocolRaw, "http/protobuf", StringComparison.OrdinalIgnoreCase))
                return OtlpExportProtocol.HttpProtobuf;
        }
        return port == 4318 ? OtlpExportProtocol.HttpProtobuf : OtlpExportProtocol.Grpc;
    }

    public static WebApplicationBuilder AddTicketFlowObservability(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console()
            .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options => ApplyOtlpExporterOptions(options, builder.Configuration));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options => ApplyOtlpExporterOptions(options, builder.Configuration));
            });

        return builder;
    }
}
