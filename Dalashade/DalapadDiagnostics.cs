using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Dalashade;

public sealed record DalapadCapabilityDiagnostic(string Name, bool Available, string Detail);

public sealed record DalapadImplementationOption(string Name, string Feasibility, string Risk, string Summary);

public sealed record DalapadBackendStep(string Stage, string Goal, string SafetyBoundary, string ExitCriteria);

public sealed record DalapadBridgeResource(string Name, string Kind, string ExpectedSource, string DiagnosticOnlyUse, string AvailabilityFlag);

public sealed record DalapadDiagnosticRoute(string Name, string Producer, string Output, string Purpose);

public sealed record DalapadIpcEndpoint(string Name, string Kind, string Direction, string Address, string Purpose, string SafetyBoundary);

public sealed record DalapadRealtimeChannel(string Name, string Direction, string Payload, string Priority, string SafetyBoundary);

public sealed record DalapadIpcStatus(
    string ContractVersion,
    string Status,
    string Summary,
    string StatusFilePath,
    bool StatusFileFound,
    bool BridgeReported,
    bool ContractCompatible,
    string BridgeVersion,
    string AddonProcess,
    DateTimeOffset? LastUpdateUtc,
    IReadOnlyList<string> ReportedResources,
    IReadOnlyList<string> Warnings);

public sealed record DalapadDiagnostics(
    string DisplayName,
    bool Probed,
    DateTimeOffset ProbeTimestamp,
    string Status,
    string Summary,
    string RuntimeAssembly,
    string RenderTargetManagerTypeName,
    bool RenderTargetManagerTypeFound,
    bool InstanceMethodFound,
    bool GBufferMemberFound,
    bool DepthStencilMemberFound,
    bool TextureTypeFound,
    string AddonContractVersion,
    string IpcContractVersion,
    IReadOnlyList<DalapadIpcEndpoint> IpcEndpoints,
    DalapadIpcStatus IpcStatus,
    IReadOnlyList<DalapadRealtimeChannel> RealtimeChannels,
    IReadOnlyList<DalapadBridgeResource> AddonResources,
    IReadOnlyList<DalapadDiagnosticRoute> DiagnosticRoutes,
    IReadOnlyList<DalapadImplementationOption> ImplementationOptions,
    IReadOnlyList<DalapadBackendStep> NextBackendSteps,
    IReadOnlyList<DalapadCapabilityDiagnostic> Capabilities,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> RemovalNotes)
{
    private const string Name = "Dalapad";
    private const string AddonContract = "0.1-diagnostic";
    private const string IpcContract = "0.1-ipc-diagnostic";
    private const string StatusFileName = "dalapad-status.json";

    public static DalapadDiagnostics NotProbed(string reason)
    {
        return new DalapadDiagnostics(
            Name,
            false,
            DateTimeOffset.MinValue,
            "NotProbed",
            reason,
            string.Empty,
            string.Empty,
            false,
            false,
            false,
            false,
            false,
            AddonContract,
            IpcContract,
            BuildIpcEndpoints(string.Empty),
            DalapadIpcStatusNotProbed(reason, string.Empty),
            BuildRealtimeChannels(),
            BuildAddonResources(),
            BuildDiagnosticRoutes(),
            BuildImplementationOptions(),
            BuildNextBackendSteps(),
            Array.Empty<DalapadCapabilityDiagnostic>(),
            SafetyNotes("Dalapad has not run a runtime surface-data probe yet."),
            BuildRemovalNotes());
    }

    public static DalapadDiagnostics Probe(string? pluginConfigDirectory = null)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var statusPath = BuildStatusFilePath(pluginConfigDirectory);
        var ipcStatus = ProbeIpcStatus(statusPath);
        var endpoints = BuildIpcEndpoints(statusPath);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var clientStructsAssembly = assemblies.FirstOrDefault(assembly =>
            assembly.GetName().Name?.Contains("FFXIVClientStructs", StringComparison.OrdinalIgnoreCase) == true);

        if (clientStructsAssembly is null)
        {
            return BuildUnavailable(
                timestamp,
                "FFXIVClientStructs assembly is not loaded in the current AppDomain.",
                string.Empty,
                string.Empty,
                false,
                false,
                false,
                false,
                endpoints,
                ipcStatus);
        }

        var types = SafeGetTypes(clientStructsAssembly);
        var renderTargetManagerType = types.FirstOrDefault(type =>
            string.Equals(type.FullName, "FFXIVClientStructs.FFXIV.Client.Graphics.Render.RenderTargetManager", StringComparison.Ordinal)
            || type.FullName?.EndsWith(".RenderTargetManager", StringComparison.Ordinal) == true);
        var textureType = types.FirstOrDefault(type =>
            string.Equals(type.FullName, "FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture", StringComparison.Ordinal)
            || type.FullName?.EndsWith(".Texture", StringComparison.Ordinal) == true);

        if (renderTargetManagerType is null)
        {
            return BuildUnavailable(
                timestamp,
                "FFXIVClientStructs is loaded, but RenderTargetManager was not discoverable by reflection.",
                clientStructsAssembly.GetName().Name ?? clientStructsAssembly.FullName ?? string.Empty,
                string.Empty,
                false,
                false,
                false,
                textureType is not null,
                endpoints,
                ipcStatus);
        }

        var members = GetMembers(renderTargetManagerType);
        var instanceMethodFound = renderTargetManagerType.GetMethod("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) is not null;
        var gBufferMemberFound = members.Any(member => member.Name.Contains("GBuffer", StringComparison.OrdinalIgnoreCase));
        var depthStencilMemberFound = members.Any(member => member.Name.Contains("DepthStencil", StringComparison.OrdinalIgnoreCase));
        var textureTypeFound = textureType is not null;
        var status = gBufferMemberFound || depthStencilMemberFound ? "Candidate" : "Partial";
        var summary = gBufferMemberFound || depthStencilMemberFound
            ? "Runtime metadata exposes candidate render target members. Dalapad still has not read, copied, or bridged any texture."
            : "RenderTargetManager type exists, but expected GBuffer/depth members were not found by reflection.";

        return new DalapadDiagnostics(
            Name,
            true,
            timestamp,
            status,
            summary,
            clientStructsAssembly.GetName().Name ?? clientStructsAssembly.FullName ?? string.Empty,
            renderTargetManagerType.FullName ?? renderTargetManagerType.Name,
            true,
            instanceMethodFound,
            gBufferMemberFound,
            depthStencilMemberFound,
            textureTypeFound,
            AddonContract,
            IpcContract,
            endpoints,
            ipcStatus,
            BuildRealtimeChannels(),
            BuildAddonResources(),
            BuildDiagnosticRoutes(),
            BuildImplementationOptions(),
            BuildNextBackendSteps(),
            BuildCapabilities(instanceMethodFound, gBufferMemberFound, depthStencilMemberFound, textureTypeFound, false),
            SafetyNotes("Dalapad found only runtime metadata. This pass intentionally does not invoke RenderTargetManager.Instance or touch GPU resources."),
            BuildRemovalNotes());
    }

    private static DalapadDiagnostics BuildUnavailable(
        DateTimeOffset timestamp,
        string summary,
        string runtimeAssembly,
        string renderTargetManagerTypeName,
        bool typeFound,
        bool instanceFound,
        bool gBufferFound,
        bool textureTypeFound,
        IReadOnlyList<DalapadIpcEndpoint> ipcEndpoints,
        DalapadIpcStatus ipcStatus)
    {
        return new DalapadDiagnostics(
            Name,
            true,
            timestamp,
            "Unavailable",
            summary,
            runtimeAssembly,
            renderTargetManagerTypeName,
            typeFound,
            instanceFound,
            gBufferFound,
            false,
            textureTypeFound,
            AddonContract,
            IpcContract,
            ipcEndpoints,
            ipcStatus,
            BuildRealtimeChannels(),
            BuildAddonResources(),
            BuildDiagnosticRoutes(),
            BuildImplementationOptions(),
            BuildNextBackendSteps(),
            BuildCapabilities(instanceFound, gBufferFound, false, textureTypeFound, false),
            SafetyNotes("Dalapad did not find enough runtime metadata to identify external surface-data candidates."),
            BuildRemovalNotes());
    }

    private static IReadOnlyList<DalapadCapabilityDiagnostic> BuildCapabilities(
        bool instanceFound,
        bool gBufferFound,
        bool depthStencilFound,
        bool textureTypeFound,
        bool shaderBridgeAvailable)
    {
        return new[]
        {
            new DalapadCapabilityDiagnostic("RenderTargetManager.Instance metadata", instanceFound, instanceFound ? "Found by reflection; not invoked." : "Not found."),
            new DalapadCapabilityDiagnostic("GBuffer metadata", gBufferFound, gBufferFound ? "Candidate member name found; no texture was read." : "No GBuffer-like member name found."),
            new DalapadCapabilityDiagnostic("DepthStencil metadata", depthStencilFound, depthStencilFound ? "Candidate member name found; no texture was read." : "No DepthStencil-like member name found."),
            new DalapadCapabilityDiagnostic("Texture metadata", textureTypeFound, textureTypeFound ? "Texture type found by reflection." : "Texture type not found."),
            new DalapadCapabilityDiagnostic("ReShade shader sampling bridge", shaderBridgeAvailable, "Not implemented. Requires a separate native/addon bridge before .fx code can sample external resources.")
        };
    }

    private static IReadOnlyList<string> SafetyNotes(string firstNote)
    {
        return new[]
        {
            firstNote,
            "Diagnostic-only: no generated preset values are changed.",
            "Diagnostic-only: no shader files, FrameData, MaterialMasks, or NormalField formulas are changed.",
            "Diagnostic-only: no native hook, render target copy, G-buffer read, or network access is performed.",
            "Dalapad is an optional addon direction, not a required Dalashade runtime dependency."
        };
    }

    private static IReadOnlyList<DalapadIpcEndpoint> BuildIpcEndpoints(string statusPath)
    {
        var displayPath = string.IsNullOrWhiteSpace(statusPath)
            ? "PluginConfig/Dalapad/dalapad-status.json"
            : statusPath;
        return new[]
        {
            new DalapadIpcEndpoint(
                "Dalapad.Status",
                "JSON status file",
                "addon-to-plugin",
                displayPath,
                "Reports bridge availability, resource catalog, frame freshness, confidence, and failure reasons without requiring a live pipe.",
                "Read-only from the plugin side. Missing or invalid status is neutral and cannot change generated presets."),
            new DalapadIpcEndpoint(
                "Dalapad.SurfaceCatalog",
                "status payload section",
                "addon-to-plugin",
                "resources[] inside Dalapad.Status",
                "Describes optional render-layer resources such as normal, diffuse, and depth candidates before shader consumption is allowed.",
                "Diagnostic metadata only in Stage 1. It must not imply that .fx code can sample a resource."),
            new DalapadIpcEndpoint(
                "Dalapad.Control.v1",
                "named pipe contract",
                "plugin-to-addon",
                @"\\.\pipe\Dalapad.Control.v1",
                "Future opt-in control channel for bridge reload, self-test, and runtime effect-adaptation messages.",
                "Not opened by the plugin in Stage 1. Future use must be short-timeout, optional, and fail closed."),
            new DalapadIpcEndpoint(
                "Dalapad.RealtimeUniforms.v1",
                "named pipe message family",
                "plugin-to-addon-to-reshade",
                @"\\.\pipe\Dalapad.Control.v1",
                "Future low-priority route for real-time Dalashade value deltas after the render-layer bridge is stable.",
                "Contract-only groundwork. It must not bypass generated preset safety or first-party shader write gates.")
        };
    }

    private static IReadOnlyList<DalapadRealtimeChannel> BuildRealtimeChannels()
    {
        return new[]
        {
            new DalapadRealtimeChannel(
                "SceneIntentSnapshot",
                "plugin-to-addon",
                "Normalized scene intent values and confidence metadata.",
                "future-low",
                "No Stage 1 writes. Generated presets remain the authority until an explicit opt-in live backend exists."),
            new DalapadRealtimeChannel(
                "MaterialIntentSnapshot",
                "plugin-to-addon",
                "Normalized material intent values, evidence confidence, and suppression notes.",
                "future-low",
                "Scene-level hint only. It must not claim true material IDs or bypass shader-side masks."),
            new DalapadRealtimeChannel(
                "EffectUniformDelta",
                "plugin-to-addon-to-reshade",
                "Small set of validated first-party Dalashade uniform deltas.",
                "future-low",
                "Only after bridge reliability is proven. Must remain opt-in, bounded, and reversible."),
            new DalapadRealtimeChannel(
                "BridgeSelfTest",
                "plugin-to-addon",
                "Ping/self-test request and bridge capability query.",
                "stage-1-contract",
                "Safe diagnostic message family; the plugin does not open the pipe in this pass.")
        };
    }

    private static string BuildStatusFilePath(string? pluginConfigDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginConfigDirectory))
        {
            return string.Empty;
        }

        try
        {
            return Path.Combine(Path.GetFullPath(pluginConfigDirectory), Name, StatusFileName);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return string.Empty;
        }
    }

    private static DalapadIpcStatus DalapadIpcStatusNotProbed(string summary, string statusPath)
    {
        return new DalapadIpcStatus(
            IpcContract,
            "NotProbed",
            summary,
            statusPath,
            false,
            false,
            false,
            string.Empty,
            string.Empty,
            null,
            Array.Empty<string>(),
            Array.Empty<string>());
    }

    private static DalapadIpcStatus ProbeIpcStatus(string statusPath)
    {
        if (string.IsNullOrWhiteSpace(statusPath))
        {
            return new DalapadIpcStatus(
                IpcContract,
                "Unavailable",
                "Plugin config directory was unavailable, so Dalapad IPC status was not checked.",
                string.Empty,
                false,
                false,
                false,
                string.Empty,
                string.Empty,
                null,
                Array.Empty<string>(),
                new[] { "No plugin config directory was available for the Stage 1 status-file probe." });
        }

        if (!File.Exists(statusPath))
        {
            return new DalapadIpcStatus(
                IpcContract,
                "NotConnected",
                "No Dalapad IPC status file was found. This is expected unless a separate Dalapad addon prototype is running.",
                statusPath,
                false,
                false,
                false,
                string.Empty,
                string.Empty,
                null,
                Array.Empty<string>(),
                Array.Empty<string>());
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(statusPath));
            var root = document.RootElement;
            var contract = ReadString(root, "ipcContractVersion");
            if (string.IsNullOrWhiteSpace(contract))
            {
                contract = ReadString(root, "contractVersion");
            }

            var bridgeVersion = ReadString(root, "bridgeVersion");
            var addonProcess = ReadString(root, "addonProcess");
            var status = ReadString(root, "status");
            var summary = ReadString(root, "summary");
            var timestamp = ReadTimestamp(root, "lastUpdateUtc") ?? ReadTimestamp(root, "timestampUtc");
            var reportedResources = ReadResourceNames(root);
            var warnings = ReadStringArray(root, "warnings");
            var compatible = string.Equals(contract, IpcContract, StringComparison.OrdinalIgnoreCase);
            if (!compatible)
            {
                warnings = warnings.Concat(new[] { $"IPC contract mismatch. Expected {IpcContract}, found {FormatEmpty(contract)}." }).ToArray();
            }

            return new DalapadIpcStatus(
                IpcContract,
                string.IsNullOrWhiteSpace(status) ? (compatible ? "Reported" : "ContractMismatch") : status,
                string.IsNullOrWhiteSpace(summary) ? "Dalapad IPC status file was read." : summary,
                statusPath,
                true,
                true,
                compatible,
                bridgeVersion,
                addonProcess,
                timestamp,
                reportedResources,
                warnings);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return new DalapadIpcStatus(
                IpcContract,
                "InvalidStatus",
                $"Dalapad IPC status file exists but could not be read: {ex.Message}",
                statusPath,
                true,
                false,
                false,
                string.Empty,
                string.Empty,
                null,
                Array.Empty<string>(),
                new[] { "Invalid Dalapad status files are ignored and cannot affect generated presets or shader behavior." });
        }
    }

    private static string ReadString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static DateTimeOffset? ReadTimestamp(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTimeOffset.TryParse(value.GetString(), out var parsed) ? parsed : null;
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

    private static IReadOnlyList<string> ReadResourceNames(JsonElement root)
    {
        if (!root.TryGetProperty("resources", out var resources) || resources.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return resources.EnumerateArray()
            .Select(resource => resource.ValueKind == JsonValueKind.Object ? ReadString(resource, "name") : string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
    }

    private static string FormatEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "empty" : value;
    }

    private static IReadOnlyList<DalapadBridgeResource> BuildAddonResources()
    {
        return new[]
        {
            new DalapadBridgeResource(
                "Dalapad_SurfaceNormal",
                "optional texture",
                "RenderTargetManager GBuffer normal candidate, expected to start with GBuffer[0] only after pointer/resource validation",
                "Compare against NormalField and FrameData surface normal debug views; do not affect production output in the first bridge pass.",
                "Dalapad_NormalAvailable"),
            new DalapadBridgeResource(
                "Dalapad_SurfaceDiffuse",
                "optional texture",
                "RenderTargetManager diffuse/albedo candidate, expected to start with GBuffer[2] only after pointer/resource validation",
                "Compare broad material/color evidence against ScreenshotMaterialEvidence and MaterialIntent; do not treat as true material ID.",
                "Dalapad_DiffuseAvailable"),
            new DalapadBridgeResource(
                "Dalapad_SurfaceDepth",
                "optional texture",
                "RenderTargetManager DepthStencil candidate after format, scaling, and reverse-Z behavior are validated",
                "Compare ReShade depth reliability against runtime depth shape; do not replace existing depth assumptions in production.",
                "Dalapad_DepthAvailable"),
            new DalapadBridgeResource(
                "Dalapad_SurfaceStatus",
                "uniform/status block",
                "Dalapad bridge runtime",
                "Expose bridge availability, resource dimensions, frame freshness, and confidence for diagnostics.",
                "Dalapad_BridgeAvailable")
        };
    }

    private static IReadOnlyList<DalapadDiagnosticRoute> BuildDiagnosticRoutes()
    {
        return new[]
        {
            new DalapadDiagnosticRoute(
                "Plugin metadata probe",
                "Dalamud plugin",
                "Compatibility report, Developer Mode, dalapad-diagnostics.json",
                "Prove runtime metadata exists without invoking render-target access."),
            new DalapadDiagnosticRoute(
                "Plugin pointer probe",
                "Dalamud plugin, developer-only opt-in",
                "Pointer/resource shape rows in compatibility report and debug bundle",
                "Validate candidate resource presence, nullability, dimensions, and stability without texture copy or shader exposure."),
            new DalapadDiagnosticRoute(
                "Bridge addon self-test",
                "Separate Dalapad ReShade/native addon",
                "Dalapad IPC status file, addon log/status block, and Dalapad_BridgeAvailable style flags",
                "Prove the bridge loaded and can publish named diagnostic resources without requiring production shaders."),
            new DalapadDiagnosticRoute(
                "IPC status-file handshake",
                "Separate Dalapad ReShade/native addon",
                "PluginConfig/Dalapad/dalapad-status.json and dalapad-diagnostics.json",
                "Let the plugin report bridge status, resource catalog, freshness, and errors without opening a live pipe."),
            new DalapadDiagnosticRoute(
                "Shader diagnostic compare",
                "Debug-only Dalapad/FrameData shader views",
                "False-color comparison of external surface data versus NormalField/FrameData",
                "Decide whether external data is stable enough to become an optional FrameSurfaceData backend.")
        };
    }

    private static IReadOnlyList<DalapadImplementationOption> BuildImplementationOptions()
    {
        return new[]
        {
            new DalapadImplementationOption(
                "Managed metadata probe",
                "High",
                "Low",
                "Current path. It can prove that runtime types and likely fields exist, but it cannot expose textures to ReShade .fx code."),
            new DalapadImplementationOption(
                "Managed pointer/resource observation",
                "Medium",
                "Medium",
                "A diagnostic-only Dalamud step could invoke RenderTargetManager.Instance and report pointer/resource shape, nullability, and sizes without copying or sampling textures."),
            new DalapadImplementationOption(
                "Native ReShade addon bridge",
                "Medium-high",
                "High",
                "The practical path for .fx sampling. A separate addon would copy/register selected render targets as named ReShade resources with availability flags."),
            new DalapadImplementationOption(
                "Status-file IPC handshake",
                "High",
                "Low-medium",
                "Stage 1 path. A separate addon can write a small JSON status file that the plugin reads for diagnostics without touching GPU resources or blocking on pipes."),
            new DalapadImplementationOption(
                "Named-pipe realtime uniform bridge",
                "Medium",
                "Medium-high",
                "Future path for live first-party shader values. It should stay behind the render-layer bridge priority and remain opt-in, bounded, and fail-closed."),
            new DalapadImplementationOption(
                "Pure Dalamud shader bridge",
                "Low",
                "High",
                "A Dalamud plugin alone can inspect runtime metadata, but it does not give ReShade .fx shaders a texture binding path for game render targets.")
        };
    }

    private static IReadOnlyList<DalapadBackendStep> BuildNextBackendSteps()
    {
        return new[]
        {
            new DalapadBackendStep(
                "1. Metadata inventory",
                "Keep reporting RenderTargetManager, GBuffer, DepthStencil, and Texture metadata.",
                "Reflection only; no Instance invocation and no GPU resources touched.",
                "Debug bundle proves candidate metadata consistently appears across zones and sessions."),
            new DalapadBackendStep(
                "2. Diagnostic pointer probe",
                "Add an opt-in developer-only probe that invokes RenderTargetManager.Instance and reports pointer/resource presence, dimensions, format names if safely readable, and nullability.",
                "No render-target copy, no shader exposure, no generated-preset changes, and no normal-user UI.",
                "Reports whether GBuffer[0], GBuffer[2], and depth/stencil candidates are non-null and stable during live play."),
            new DalapadBackendStep(
                "3. Dalapad bridge addon spike",
                "Prototype a native/ReShade addon that writes the Stage 1 IPC status file and then registers or copies external normal, diffuse, and depth candidates as named optional resources matching the Dalapad diagnostic contract.",
                "Separate package, compile guarded, debug-only, and removable without breaking Dalashade shaders.",
                ".fx code can detect an availability flag and sample a diagnostic texture without crashing when absent."),
            new DalapadBackendStep(
                "4. Shader contract guard",
                "Add optional FrameSurfaceData fields behind Dalapad availability guards.",
                "Fallback remains NormalField; no required shader variable or include for users without the bridge.",
                "Shaders compile and render identically when Dalapad resources are unavailable."),
            new DalapadBackendStep(
                "5. Compare and calibrate",
                "Add debug views comparing external normal/diffuse/depth confidence against NormalField and screenshot material evidence.",
                "Diagnostic-only comparison; no production visual influence.",
                "Reports identify where external data is better, stale, missing, transparent-only, or mismatched."),
            new DalapadBackendStep(
                "6. Opt-in backend selection",
                "Let FrameSurfaceData prefer external surface data only when the bridge is present, confidence is high, and the user enables the experimental backend.",
                "Default off; generated presets and shaders keep NormalField fallback.",
                "Production shaders can consume a single FrameSurfaceData contract without knowing which backend supplied it.")
            ,
            new DalapadBackendStep(
                "7. Realtime value bridge",
                "After surface data is reliable, prototype a bounded live-value channel for first-party Dalashade uniforms that mirrors generated preset authority.",
                "Default off, low priority, and never required for render-layer use. Must respect existing shader write gates and generated-preset fallback.",
                "Live values can be disabled with no visual contract break and generated presets still produce the same baseline.")
        };
    }

    private static IReadOnlyList<string> BuildRemovalNotes()
    {
        return new[]
        {
            "Remove DalapadDiagnostics.cs.",
            "Remove the Dalapad diagnostics calls from Plugin, CompatibilityReportExporter, DebugBundleExporter, and ConfigWindow.",
            "Remove DalapadAddon/ if the addon scaffold is no longer useful.",
            "Remove PluginConfig/Dalapad/dalapad-status.json if an experimental addon prototype created it.",
            "Remove docs/Dalapad.md and its index links.",
            "No shader or generated-preset contract cleanup is required because this pass does not add shader variables, techniques, includes, or preset writes."
        };
    }

    private static Type[] SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static IEnumerable<MemberInfo> GetMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        return type.GetFields(flags).Cast<MemberInfo>().Concat(type.GetProperties(flags));
    }
}
