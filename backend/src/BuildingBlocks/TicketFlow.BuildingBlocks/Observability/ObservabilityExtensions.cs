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
    private enum OtlpSignal
    {
        Traces,
        Metrics
    }

    private static void ApplyOtlpExporterOptions(OtlpExporterOptions options, IConfiguration configuration, OtlpSignal signal)
    {
        var raw = signal == OtlpSignal.Traces
            ? (configuration["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"]
               ?? configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
               ?? configuration["Otlp:Endpoint"])
            : (configuration["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"]
               ?? configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
               ?? configuration["Otlp:Endpoint"]);

        if (string.IsNullOrWhiteSpace(raw))
            raw = "http://localhost:4317";
        var uri = new Uri(raw);

        var protocolRaw = configuration["Otlp:Protocol"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
        var protocol = ResolveOtlpProtocol(protocolRaw, uri.Port);
        options.Protocol = protocol;
        // HttpProtobuf: setting only host:port does not get /v1/traces|metrics appended; Jaeger would get wrong POST path.
        options.Endpoint = EnsureOtlpHttpProtobufResourcePath(uri, protocol, signal);
    }

    private static Uri EnsureOtlpHttpProtobufResourcePath(Uri uri, OtlpExportProtocol protocol, OtlpSignal signal)
    {
        if (protocol != OtlpExportProtocol.HttpProtobuf)
            return uri;

        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.Length > 0 && path.Contains("v1", StringComparison.Ordinal))
            return uri;

        var root = new UriBuilder(uri) { Path = "/" }.Uri;
        var relative = signal == OtlpSignal.Traces ? "v1/traces" : "v1/metrics";
        return new Uri(root, relative);
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
                    .SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options => ApplyOtlpExporterOptions(options, builder.Configuration, OtlpSignal.Traces));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options => ApplyOtlpExporterOptions(options, builder.Configuration, OtlpSignal.Metrics));
            });

        return builder;
    }
}
