using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Dalashade;

public sealed record DalapadControlPipeCapabilities(
    bool SupportsStatusFile,
    bool SupportsControlPipe,
    bool SupportsRealtimeUniforms,
    bool SupportsResourceCatalog,
    bool SupportsDebugVisualization,
    bool ReadsRenderTargets,
    bool CopiesRenderTargets,
    bool RegistersShaderResources,
    bool MovesRealtimeShaderValues);

public sealed record DalapadPinnedCandidateStatus(
    string Name,
    string Label,
    string Semantic,
    string AvailabilityUniform,
    string Source,
    string SourceSemantic,
    string ClassificationHint,
    bool Observed,
    bool Copied,
    int Width,
    int Height,
    float Confidence);

public sealed record DalapadDebugVisualizationStatus(
    string Version,
    bool Enabled,
    string Status,
    string Source,
    string Shader,
    string TextureName,
    bool ShaderTextureFound,
    bool SyntheticTextureUploaded,
    bool UsesSyntheticTexture,
    int Width,
    int Height,
    int FrameCounter,
    int FrameAge,
    int CopyFrameInterval,
    int ObservedSourceCount,
    int CopiedSourceCount,
    bool ReadsRenderTargets,
    bool CopiesRenderTargets,
    bool RegistersGameResources,
    IReadOnlyList<DalapadPinnedCandidateStatus> PinnedCandidates,
    string Reason);

public sealed record DalapadControlPipeStatus(
    string PipeName,
    bool Attempted,
    bool PipeListening,
    bool ResponseReceived,
    bool ContractCompatible,
    string Status,
    string Summary,
    string BridgeVersion,
    string ResponseType,
    string RequestId,
    long ElapsedMilliseconds,
    DalapadControlPipeCapabilities Capabilities,
    IReadOnlyList<DalapadResourceCatalogEntry> ResourceCatalog,
    DalapadDebugVisualizationStatus DebugVisualization,
    IReadOnlyList<string> Warnings);

public static class DalapadIpcClient
{
    private const string PipeName = "Dalapad.Control.v1";
    private const string PipeDisplayName = @"\\.\pipe\Dalapad.Control.v1";
    private const string Contract = "Dalapad.Control.v1";
    private const int TimeoutMilliseconds = 150;

    private static readonly DalapadControlPipeCapabilities EmptyCapabilities = new(
        SupportsStatusFile: false,
        SupportsControlPipe: false,
        SupportsRealtimeUniforms: false,
        SupportsResourceCatalog: false,
        SupportsDebugVisualization: false,
        ReadsRenderTargets: false,
        CopiesRenderTargets: false,
        RegistersShaderResources: false,
        MovesRealtimeShaderValues: false);

    public static readonly DalapadDebugVisualizationStatus EmptyDebugVisualization = new(
        Version: string.Empty,
        Enabled: false,
        Status: "NotReported",
        Source: string.Empty,
        Shader: string.Empty,
        TextureName: string.Empty,
        ShaderTextureFound: false,
        SyntheticTextureUploaded: false,
        UsesSyntheticTexture: false,
        Width: 0,
        Height: 0,
        FrameCounter: 0,
        FrameAge: 0,
        CopyFrameInterval: 0,
        ObservedSourceCount: 0,
        CopiedSourceCount: 0,
        ReadsRenderTargets: false,
        CopiesRenderTargets: false,
        RegistersGameResources: false,
        PinnedCandidates: Array.Empty<DalapadPinnedCandidateStatus>(),
        Reason: "Debug visualization status was not reported.");

    public static DalapadControlPipeStatus NotProbed(string summary)
    {
        return new DalapadControlPipeStatus(
            PipeDisplayName,
            false,
            false,
            false,
            false,
            "NotProbed",
            summary,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            EmptyCapabilities,
            Array.Empty<DalapadResourceCatalogEntry>(),
            EmptyDebugVisualization,
            Array.Empty<string>());
    }

    public static DalapadControlPipeStatus ProbeCapabilities()
    {
        var requestId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.None);
            pipe.Connect(TimeoutMilliseconds);
            if (pipe.CanTimeout)
            {
                pipe.ReadTimeout = TimeoutMilliseconds;
                pipe.WriteTimeout = TimeoutMilliseconds;
            }

            var request = JsonSerializer.Serialize(new
            {
                contract = Contract,
                id = requestId,
                type = "QueryStatus",
                timestampUtc = DateTimeOffset.UtcNow
            }) + "\n";
            var requestBytes = Encoding.UTF8.GetBytes(request);
            pipe.Write(requestBytes, 0, requestBytes.Length);
            pipe.Flush();

            var response = ReadResponse(pipe);
            stopwatch.Stop();
            if (string.IsNullOrWhiteSpace(response))
            {
                return BuildStatus(
                    "NoResponse",
                    "Dalapad control pipe accepted the connection but did not return a response before the timeout.",
                    true,
                    false,
                    false,
                    string.Empty,
                    string.Empty,
                    requestId,
                    stopwatch.ElapsedMilliseconds,
                    EmptyCapabilities,
                    Array.Empty<DalapadResourceCatalogEntry>(),
                    EmptyDebugVisualization,
                    new[] { "No response was received from the diagnostic control pipe." });
            }

            return ParseResponse(response, requestId, stopwatch.ElapsedMilliseconds);
        }
        catch (TimeoutException)
        {
            stopwatch.Stop();
            return BuildStatus(
                "NotListening",
                "Dalapad control pipe is not listening. This is expected unless the separate addon prototype is loaded.",
                false,
                false,
                false,
                string.Empty,
                string.Empty,
                requestId,
                stopwatch.ElapsedMilliseconds,
                EmptyCapabilities,
                Array.Empty<DalapadResourceCatalogEntry>(),
                EmptyDebugVisualization,
                Array.Empty<string>());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or ObjectDisposedException)
        {
            stopwatch.Stop();
            return BuildStatus(
                "Unavailable",
                $"Dalapad control pipe probe failed: {ex.Message}",
                false,
                false,
                false,
                string.Empty,
                string.Empty,
                requestId,
                stopwatch.ElapsedMilliseconds,
                EmptyCapabilities,
                Array.Empty<DalapadResourceCatalogEntry>(),
                EmptyDebugVisualization,
                new[] { "Control pipe failures are diagnostic-only and cannot affect generated presets or shader behavior." });
        }
    }

    private static string ReadResponse(Stream stream)
    {
        var buffer = new byte[8192];
        var count = stream.Read(buffer, 0, buffer.Length);
        if (count <= 0)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(buffer, 0, count).Trim();
    }

    private static DalapadControlPipeStatus ParseResponse(string response, string requestId, long elapsedMilliseconds)
    {
        try
        {
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;
            var contract = ReadString(root, "contract");
            var responseId = ReadString(root, "id");
            var responseType = ReadString(root, "type");
            var status = ReadString(root, "status");
            var summary = ReadString(root, "summary");
            var bridgeVersion = ReadString(root, "bridgeVersion");
            var warnings = ReadStringArray(root, "warnings");
            var compatible = string.Equals(contract, Contract, StringComparison.OrdinalIgnoreCase);
            var sameRequest = string.Equals(responseId, requestId, StringComparison.OrdinalIgnoreCase);
            if (!compatible)
            {
                warnings = warnings.Concat(new[] { $"Control pipe contract mismatch. Expected {Contract}, found {FormatEmpty(contract)}." }).ToArray();
            }

            if (!sameRequest)
            {
                warnings = warnings.Concat(new[] { $"Control pipe response id mismatch. Expected {requestId}, found {FormatEmpty(responseId)}." }).ToArray();
            }

            return BuildStatus(
                string.IsNullOrWhiteSpace(status) ? (compatible ? "Responded" : "ContractMismatch") : status,
                string.IsNullOrWhiteSpace(summary) ? "Dalapad control pipe returned a diagnostic response." : summary,
                true,
                true,
                compatible && sameRequest,
                bridgeVersion,
                responseType,
                responseId,
                elapsedMilliseconds,
                ReadCapabilities(root),
                ReadResourceCatalog(root),
                ReadDebugVisualization(root),
                warnings);
        }
        catch (JsonException ex)
        {
            return BuildStatus(
                "InvalidResponse",
                $"Dalapad control pipe returned invalid JSON: {ex.Message}",
                true,
                true,
                false,
                string.Empty,
                string.Empty,
                requestId,
                elapsedMilliseconds,
                EmptyCapabilities,
                Array.Empty<DalapadResourceCatalogEntry>(),
                EmptyDebugVisualization,
                new[] { "Invalid control pipe responses are ignored and cannot affect generated presets or shader behavior." });
        }
    }

    private static DalapadControlPipeStatus BuildStatus(
        string status,
        string summary,
        bool pipeListening,
        bool responseReceived,
        bool contractCompatible,
        string bridgeVersion,
        string responseType,
        string requestId,
        long elapsedMilliseconds,
        DalapadControlPipeCapabilities capabilities,
        IReadOnlyList<DalapadResourceCatalogEntry> resourceCatalog,
        DalapadDebugVisualizationStatus debugVisualization,
        IReadOnlyList<string> warnings)
    {
        return new DalapadControlPipeStatus(
            PipeDisplayName,
            true,
            pipeListening,
            responseReceived,
            contractCompatible,
            status,
            summary,
            bridgeVersion,
            responseType,
            requestId,
            elapsedMilliseconds,
            capabilities,
            resourceCatalog,
            debugVisualization,
            warnings);
    }

    private static DalapadControlPipeCapabilities ReadCapabilities(JsonElement root)
    {
        if (!root.TryGetProperty("capabilities", out var capabilities) || capabilities.ValueKind != JsonValueKind.Object)
        {
            return EmptyCapabilities;
        }

        return new DalapadControlPipeCapabilities(
            ReadBool(capabilities, "supportsStatusFile"),
            ReadBool(capabilities, "supportsControlPipe"),
            ReadBool(capabilities, "supportsRealtimeUniforms"),
            ReadBool(capabilities, "supportsResourceCatalog"),
            ReadBool(capabilities, "supportsDebugVisualization"),
            ReadBool(capabilities, "readsRenderTargets"),
            ReadBool(capabilities, "copiesRenderTargets"),
            ReadBool(capabilities, "registersShaderResources"),
            ReadBool(capabilities, "movesRealtimeShaderValues"));
    }

    private static string ReadString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static IReadOnlyList<DalapadResourceCatalogEntry> ReadResourceCatalog(JsonElement root)
    {
        if (!root.TryGetProperty("resources", out var resources) || resources.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<DalapadResourceCatalogEntry>();
        }

        return resources.EnumerateArray()
            .Where(resource => resource.ValueKind == JsonValueKind.Object)
            .Select(resource => new DalapadResourceCatalogEntry(
                ReadString(resource, "name"),
                ReadString(resource, "source"),
                ReadString(resource, "kind"),
                ReadString(resource, "availabilityFlag"),
                ReadBool(resource, "available"),
                ReadInt(resource, "width"),
                ReadInt(resource, "height"),
                ReadString(resource, "format"),
                ReadString(resource, "freshness"),
                ReadFloat(resource, "confidence"),
                ReadString(resource, "safetyState"),
                ReadString(resource, "metadataSource"),
                ReadString(resource, "reason")))
            .Where(resource => !string.IsNullOrWhiteSpace(resource.Name))
            .ToArray();
    }

    public static DalapadDebugVisualizationStatus ReadDebugVisualization(JsonElement root)
    {
        if (!root.TryGetProperty("debugVisualization", out var debug) || debug.ValueKind != JsonValueKind.Object)
        {
            return EmptyDebugVisualization;
        }

        return new DalapadDebugVisualizationStatus(
            ReadString(debug, "version"),
            ReadBool(debug, "enabled"),
            ReadString(debug, "status"),
            ReadString(debug, "source"),
            ReadString(debug, "shader"),
            ReadString(debug, "textureName"),
            ReadBool(debug, "shaderTextureFound"),
            ReadBool(debug, "syntheticTextureUploaded"),
            ReadBool(debug, "usesSyntheticTexture"),
            ReadInt(debug, "width"),
            ReadInt(debug, "height"),
            ReadInt(debug, "frameCounter"),
            ReadInt(debug, "frameAge"),
            ReadInt(debug, "copyFrameInterval"),
            ReadInt(debug, "observedSourceCount"),
            ReadInt(debug, "copiedSourceCount"),
            ReadBool(debug, "readsRenderTargets"),
            ReadBool(debug, "copiesRenderTargets"),
            ReadBool(debug, "registersGameResources"),
            ReadPinnedCandidates(debug),
            ReadString(debug, "reason"));
    }

    private static IReadOnlyList<DalapadPinnedCandidateStatus> ReadPinnedCandidates(JsonElement debug)
    {
        if (!debug.TryGetProperty("pinnedCandidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<DalapadPinnedCandidateStatus>();
        }

        return candidates.EnumerateArray()
            .Where(candidate => candidate.ValueKind == JsonValueKind.Object)
            .Select(candidate => new DalapadPinnedCandidateStatus(
                ReadString(candidate, "name"),
                ReadString(candidate, "label"),
                ReadString(candidate, "semantic"),
                ReadString(candidate, "availabilityUniform"),
                ReadString(candidate, "source"),
                ReadString(candidate, "sourceSemantic"),
                ReadString(candidate, "classificationHint"),
                ReadBool(candidate, "observed"),
                ReadBool(candidate, "copied"),
                ReadInt(candidate, "width"),
                ReadInt(candidate, "height"),
                ReadFloat(candidate, "confidence")))
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Name))
            .ToArray();
    }

    private static bool ReadBool(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value)
               && value.ValueKind is JsonValueKind.True or JsonValueKind.False
               && value.GetBoolean();
    }

    private static int ReadInt(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result)
            ? result
            : 0;
    }

    private static float ReadFloat(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out var result)
            ? result
            : 0f;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }

    private static string FormatEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "empty" : value;
    }
}
