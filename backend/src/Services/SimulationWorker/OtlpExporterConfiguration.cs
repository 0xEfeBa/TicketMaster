using System;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Exporter;

namespace TicketFlow.SimulationWorker;

internal static class OtlpExporterConfiguration
{
    public static void Apply(OtlpExporterOptions options, IConfiguration configuration)
    {
        var raw = configuration["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"]
            ?? configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? configuration["Otlp:Endpoint"];

        if (string.IsNullOrWhiteSpace(raw))
            raw = "http://localhost:4317";
        var uri = new Uri(raw);

        var protocolRaw = configuration["Otlp:Protocol"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
        var protocol = ResolveOtlpProtocol(protocolRaw, uri.Port);
        options.Protocol = protocol;
        options.Endpoint = EnsureOtlpHttpProtobufTracePath(uri, protocol);
    }

    private static Uri EnsureOtlpHttpProtobufTracePath(Uri uri, OtlpExportProtocol protocol)
    {
        if (protocol != OtlpExportProtocol.HttpProtobuf)
            return uri;

        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.Length > 0 && path.Contains("v1", StringComparison.Ordinal))
            return uri;

        var root = new UriBuilder(uri) { Path = "/" }.Uri;
        return new Uri(root, "v1/traces");
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
}
