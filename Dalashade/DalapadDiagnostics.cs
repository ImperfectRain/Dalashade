using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Dalashade;

public sealed record DalapadCapabilityDiagnostic(string Name, bool Available, string Detail);

public sealed record DalapadImplementationOption(string Name, string Feasibility, string Risk, string Summary);

public sealed record DalapadBackendStep(string Stage, string Goal, string SafetyBoundary, string ExitCriteria);

public sealed record DalapadBridgeResource(string Name, string Kind, string ExpectedSource, string DiagnosticOnlyUse, string AvailabilityFlag);

public sealed record DalapadDiagnosticRoute(string Name, string Producer, string Output, string Purpose);

public sealed record DalapadIpcEndpoint(string Name, string Kind, string Direction, string Address, string Purpose, string SafetyBoundary);

public sealed record DalapadRealtimeChannel(string Name, string Direction, string Payload, string Priority, string SafetyBoundary);

public sealed record DalapadResourceCatalogEntry(
    string Name,
    string Source,
    string Kind,
    string AvailabilityFlag,
    bool Available,
    int Width,
    int Height,
    string Format,
    string Freshness,
    float Confidence,
    string SafetyState,
    string MetadataSource,
    string Reason);

public sealed record DalapadResourceShapeRow(
    string Name,
    string Source,
    bool CandidateFound,
    bool PointerObserved,
    string PointerFingerprint,
    int Width,
    int Height,
    string Format,
    string Freshness,
    float Confidence,
    string SafetyState,
    string MetadataSource,
    string Reason);

public sealed record DalapadResourceShapeProbe(
    bool Enabled,
    bool Attempted,
    bool InstanceInvoked,
    string Status,
    string Summary,
    DateTimeOffset Timestamp,
    IReadOnlyList<DalapadResourceShapeRow> Resources,
    IReadOnlyList<string> Warnings);

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
    IReadOnlyList<DalapadResourceCatalogEntry> ResourceCatalog,
    DalapadDebugVisualizationStatus DebugVisualization,
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
    DalapadControlPipeStatus ControlPipeStatus,
    DalapadResourceShapeProbe ResourceShapeProbe,
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
            DalapadIpcClient.NotProbed("Dalapad control pipe has not been probed yet."),
            ResourceShapeProbeDisabled("Dalapad resource shape probe has not been requested."),
            BuildRealtimeChannels(),
            BuildAddonResources(),
            BuildDiagnosticRoutes(),
            BuildImplementationOptions(),
            BuildNextBackendSteps(),
            Array.Empty<DalapadCapabilityDiagnostic>(),
            SafetyNotes("Dalapad has not run a runtime surface-data probe yet."),
            BuildRemovalNotes());
    }

    public static DalapadDiagnostics Probe(string? pluginConfigDirectory = null, bool includeResourceShapeProbe = false)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var statusPath = BuildStatusFilePath(pluginConfigDirectory);
        var ipcStatus = ProbeIpcStatus(statusPath);
        var controlPipeStatus = DalapadIpcClient.ProbeCapabilities();
        var endpoints = BuildIpcEndpoints(statusPath);
        var disabledShapeProbe = ResourceShapeProbeDisabled("Developer-only resource shape probe was not requested.");
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
                ipcStatus,
                controlPipeStatus,
                disabledShapeProbe);
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
                ipcStatus,
                controlPipeStatus,
                disabledShapeProbe);
        }

        var members = GetMembers(renderTargetManagerType);
        var instanceMethodFound = renderTargetManagerType.GetMethod("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) is not null;
        var gBufferMemberFound = members.Any(member => member.Name.Contains("GBuffer", StringComparison.OrdinalIgnoreCase));
        var depthStencilMemberFound = members.Any(member => member.Name.Contains("DepthStencil", StringComparison.OrdinalIgnoreCase));
        var textureTypeFound = textureType is not null;
        var resourceShapeProbe = includeResourceShapeProbe
            ? ProbeResourceShape(renderTargetManagerType, timestamp)
            : disabledShapeProbe;
        var status = gBufferMemberFound || depthStencilMemberFound ? "Candidate" : "Partial";
        var summaryPrefix = gBufferMemberFound || depthStencilMemberFound
            ? "Runtime metadata exposes candidate render target members. Dalapad still has not read, copied, or bridged any texture."
            : "RenderTargetManager type exists, but expected GBuffer/depth members were not found by reflection.";
        var summary = includeResourceShapeProbe
            ? $"{summaryPrefix} Developer-only shape probe status: {resourceShapeProbe.Status}."
            : summaryPrefix;

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
            controlPipeStatus,
            resourceShapeProbe,
            BuildRealtimeChannels(),
            BuildAddonResources(),
            BuildDiagnosticRoutes(),
            BuildImplementationOptions(),
            BuildNextBackendSteps(),
            BuildCapabilities(instanceMethodFound, gBufferMemberFound, depthStencilMemberFound, textureTypeFound, controlPipeStatus.ResponseReceived, resourceShapeProbe, false),
            SafetyNotes(includeResourceShapeProbe
                ? "Dalapad ran an explicit developer-only resource shape probe. It may invoke RenderTargetManager.Instance, but it does not copy resources, sample textures, expose shader bindings, or change generated preset values."
                : "Dalapad found only runtime metadata. This pass intentionally does not invoke RenderTargetManager.Instance or touch GPU resources."),
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
        DalapadIpcStatus ipcStatus,
        DalapadControlPipeStatus controlPipeStatus,
        DalapadResourceShapeProbe resourceShapeProbe)
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
            controlPipeStatus,
            resourceShapeProbe,
            BuildRealtimeChannels(),
            BuildAddonResources(),
            BuildDiagnosticRoutes(),
            BuildImplementationOptions(),
            BuildNextBackendSteps(),
            BuildCapabilities(instanceFound, gBufferFound, false, textureTypeFound, controlPipeStatus.ResponseReceived, resourceShapeProbe, false),
            SafetyNotes("Dalapad did not find enough runtime metadata to identify external surface-data candidates."),
            BuildRemovalNotes());
    }

    private static IReadOnlyList<DalapadCapabilityDiagnostic> BuildCapabilities(
        bool instanceFound,
        bool gBufferFound,
        bool depthStencilFound,
        bool textureTypeFound,
        bool controlPipeAvailable,
        DalapadResourceShapeProbe resourceShapeProbe,
        bool shaderBridgeAvailable)
    {
        return new[]
        {
            new DalapadCapabilityDiagnostic("RenderTargetManager.Instance metadata", instanceFound, instanceFound ? "Found by reflection; not invoked." : "Not found."),
            new DalapadCapabilityDiagnostic("GBuffer metadata", gBufferFound, gBufferFound ? "Candidate member name found; no texture was read." : "No GBuffer-like member name found."),
            new DalapadCapabilityDiagnostic("DepthStencil metadata", depthStencilFound, depthStencilFound ? "Candidate member name found; no texture was read." : "No DepthStencil-like member name found."),
            new DalapadCapabilityDiagnostic("Texture metadata", textureTypeFound, textureTypeFound ? "Texture type found by reflection." : "Texture type not found."),
            new DalapadCapabilityDiagnostic("Dalapad control pipe", controlPipeAvailable, controlPipeAvailable ? "Diagnostic control pipe answered capability negotiation." : "Control pipe did not answer. This is neutral unless the addon is expected to be loaded."),
            new DalapadCapabilityDiagnostic("Developer resource shape probe", resourceShapeProbe.Attempted && resourceShapeProbe.Resources.Any(resource => resource.CandidateFound), resourceShapeProbe.Attempted ? resourceShapeProbe.Summary : "Disabled by default; enable only in Developer Mode when collecting render-layer evidence."),
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
            "Diagnostic-only: no native hook, render target copy, texture sampling, shader resource registration, or network access is performed.",
            "Dalapad is an optional addon direction, not a required Dalashade runtime dependency."
        };
    }

    private static DalapadResourceShapeProbe ResourceShapeProbeDisabled(string summary)
    {
        return new DalapadResourceShapeProbe(
            false,
            false,
            false,
            "Disabled",
            summary,
            DateTimeOffset.MinValue,
            Array.Empty<DalapadResourceShapeRow>(),
            new[] { "The resource shape probe is developer-only and disabled by default." });
    }

    private static DalapadResourceShapeProbe ProbeResourceShape(Type renderTargetManagerType, DateTimeOffset timestamp)
    {
        var warnings = new List<string>
        {
            "Developer-only shape probe: no render target is copied, sampled, registered with ReShade, or sent over IPC.",
            "Pointer state is redacted to observed/unavailable; raw addresses are not reported."
        };

        object? manager;
        try
        {
            var typedProbe = ProbeResourceShapeTyped(timestamp);
            if (typedProbe.Status != "TypedUnavailable")
            {
                return typedProbe;
            }

            warnings.Add("Typed ClientStructs probe was unavailable, so Dalapad fell back to reflection.");
            var instance = renderTargetManagerType.GetMethod("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (instance is null)
            {
                return new DalapadResourceShapeProbe(
                    true,
                    true,
                    false,
                    "Unavailable",
                    "RenderTargetManager.Instance was not found, so no resource shape probe was attempted.",
                    timestamp,
                    BuildUnavailableShapeRows("RenderTargetManager.Instance was not found."),
                    warnings);
            }

            manager = instance.Invoke(null, Array.Empty<object>());
        }
        catch (Exception ex)
        {
            return new DalapadResourceShapeProbe(
                true,
                true,
                false,
                "FailedClosed",
                $"RenderTargetManager.Instance could not be invoked safely: {DescribeReflectionFailure(ex)}",
                timestamp,
                BuildUnavailableShapeRows("RenderTargetManager.Instance invocation failed."),
                warnings.Concat(new[] { DescribeReflectionFailure(ex) }).ToArray());
        }

        if (manager is null)
        {
            return new DalapadResourceShapeProbe(
                true,
                true,
                true,
                "Unavailable",
                "RenderTargetManager.Instance returned no observable manager object.",
                timestamp,
                BuildUnavailableShapeRows("RenderTargetManager.Instance returned null or an unsupported pointer wrapper."),
                warnings);
        }

        var rows = new[]
        {
            BuildShapeRow("Dalapad_SurfaceNormal", "RenderTargetManager.GBuffers[0]", TryGetIndexedMember(manager, 0, "GBuffers", "_gBuffers", "GBuffer", "GBufferTextures")),
            BuildShapeRow("Dalapad_SurfaceDiffuse", "RenderTargetManager.GBuffers[2]", TryGetIndexedMember(manager, 2, "GBuffers", "_gBuffers", "GBuffer", "GBufferTextures")),
            BuildShapeRow("Dalapad_SurfaceDepth", "RenderTargetManager.DepthStencil", TryGetMemberValue(manager, "DepthStencil", "_depthStencil", "DepthStencilTexture", "Depth"))
        };
        var observed = rows.Count(row => row.PointerObserved);
        var status = observed > 0 ? "Observed" : "NoCandidatesObserved";
        var summary = observed > 0
            ? $"Observed {observed} candidate resource shape row(s). Treat these as diagnostic-only until lifecycle stability is proven."
            : "Resource shape probe ran but did not observe non-null candidate resource objects.";

        return new DalapadResourceShapeProbe(
            true,
            true,
            true,
            status,
            summary,
            timestamp,
            rows,
            warnings);
    }

    private static unsafe DalapadResourceShapeProbe ProbeResourceShapeTyped(DateTimeOffset timestamp)
    {
        var warnings = new List<string>
        {
            "Developer-only typed shape probe: no render target is copied, sampled, registered with ReShade, or sent over IPC.",
            "Pointer state is redacted to observed/unavailable; raw addresses are not reported."
        };

        try
        {
            var manager = RenderTargetManager.Instance();
            if (manager is null)
            {
                return new DalapadResourceShapeProbe(
                    true,
                    true,
                    true,
                    "TypedUnavailable",
                    "RenderTargetManager.Instance returned null from the typed ClientStructs probe.",
                    timestamp,
                    BuildUnavailableShapeRows("Typed RenderTargetManager.Instance returned null."),
                    warnings);
            }

            var gBuffers = manager->GBuffers;
            var rows = new[]
            {
                BuildTypedTextureShapeRow("Dalapad_SurfaceNormal", "RenderTargetManager.GBuffers[0]", gBuffers[0]),
                BuildTypedTextureShapeRow("Dalapad_SurfaceDiffuse", "RenderTargetManager.GBuffers[2]", gBuffers[2]),
                BuildTypedTextureShapeRow("Dalapad_SurfaceDepth", "RenderTargetManager.DepthStencil", manager->DepthStencil)
            };
            var observed = rows.Count(row => row.PointerObserved);
            var status = observed > 0 ? "Observed" : "NoCandidatesObserved";
            var summary = observed > 0
                ? $"Observed {observed} candidate resource shape row(s) through typed ClientStructs access. Treat these as diagnostic-only until lifecycle stability is proven."
                : "Typed resource shape probe ran but did not observe non-null candidate texture pointers.";

            return new DalapadResourceShapeProbe(
                true,
                true,
                true,
                status,
                summary,
                timestamp,
                rows,
                warnings);
        }
        catch (Exception ex)
        {
            return new DalapadResourceShapeProbe(
                true,
                true,
                false,
                "TypedFailedClosed",
                $"Typed resource shape probe failed closed: {DescribeReflectionFailure(ex)}",
                timestamp,
                BuildUnavailableShapeRows("Typed ClientStructs probe failed closed."),
                warnings.Concat(new[] { DescribeReflectionFailure(ex) }).ToArray());
        }
    }

    private static unsafe DalapadResourceShapeRow BuildTypedTextureShapeRow(string name, string source, Texture* texture)
    {
        if (texture is null)
        {
            return BuildUnavailableShapeRow(name, source, "Candidate texture pointer was null.");
        }

        var width = ClampDimension(texture->AllocatedWidth);
        var height = ClampDimension(texture->AllocatedHeight);
        var confidence = width > 0 && height > 0 ? 0.5f : 0.25f;
        return new DalapadResourceShapeRow(
            name,
            source,
            true,
            true,
            "observed-redacted",
            width,
            height,
            "unknown",
            "observed-on-probe",
            confidence,
            "shape-only-observed",
            "RenderTargetManager.Instance opt-in typed ClientStructs probe",
            width > 0 && height > 0
                ? "Candidate texture pointer was observed and allocated dimensions were readable."
                : "Candidate texture pointer was observed, but allocated dimensions were zero.");
    }

    private static int ClampDimension(uint value)
    {
        return value > int.MaxValue ? int.MaxValue : (int)value;
    }

    private static IReadOnlyList<DalapadResourceShapeRow> BuildUnavailableShapeRows(string reason)
    {
        return new[]
        {
            BuildUnavailableShapeRow("Dalapad_SurfaceNormal", "RenderTargetManager.GBuffers[0]", reason),
            BuildUnavailableShapeRow("Dalapad_SurfaceDiffuse", "RenderTargetManager.GBuffers[2]", reason),
            BuildUnavailableShapeRow("Dalapad_SurfaceDepth", "RenderTargetManager.DepthStencil", reason)
        };
    }

    private static DalapadResourceShapeRow BuildUnavailableShapeRow(string name, string source, string reason)
    {
        return new DalapadResourceShapeRow(
            name,
            source,
            false,
            false,
            "unavailable",
            0,
            0,
            "unknown",
            "unavailable",
            0f,
            "shape-only-unavailable",
            "RenderTargetManager.Instance opt-in reflection",
            reason);
    }

    private static DalapadResourceShapeRow BuildShapeRow(string name, string source, object? candidate)
    {
        if (candidate is null)
        {
            return BuildUnavailableShapeRow(name, source, "Candidate member was missing, null, or unreadable by reflection.");
        }

        var width = ReadFirstInt(candidate, "Width", "ActualWidth", "TextureWidth");
        var height = ReadFirstInt(candidate, "Height", "ActualHeight", "TextureHeight");
        var format = ReadFirstString(candidate, "Format", "TextureFormat", "D3DFormat", "DxgiFormat");
        if (string.IsNullOrWhiteSpace(format))
        {
            format = "unknown";
        }

        var confidence = 0.25f;
        if (width > 0 && height > 0)
        {
            confidence += 0.25f;
        }

        if (!string.Equals(format, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            confidence += 0.1f;
        }

        return new DalapadResourceShapeRow(
            name,
            source,
            true,
            true,
            "observed-redacted",
            width,
            height,
            format,
            "observed-on-probe",
            confidence,
            "shape-only-observed",
            "RenderTargetManager.Instance opt-in reflection",
            width > 0 && height > 0
                ? "Candidate object was observed and dimensions were readable."
                : "Candidate object was observed, but dimensions were not readable by reflection.");
    }

    private static object? TryGetIndexedMember(object owner, int index, params string[] memberNames)
    {
        var container = TryGetMemberValue(owner, memberNames);
        if (container is null)
        {
            return null;
        }

        try
        {
            if (container is Array array && index >= 0 && index < array.Length)
            {
                return array.GetValue(index);
            }

            var type = container.GetType();
            var indexer = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(property =>
                    property.GetIndexParameters().Length == 1
                    && property.GetIndexParameters()[0].ParameterType == typeof(int));
            return indexer?.GetValue(container, new object[] { index });
        }
        catch
        {
            return null;
        }
    }

    private static object? TryGetMemberValue(object owner, params string[] memberNames)
    {
        var ownerType = owner.GetType();
        foreach (var memberName in memberNames)
        {
            try
            {
                var property = ownerType.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (property is not null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(owner);
                }

                var field = ownerType.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (field is not null)
                {
                    return field.GetValue(owner);
                }
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static int ReadFirstInt(object owner, params string[] memberNames)
    {
        foreach (var value in ReadCandidateValues(owner, memberNames))
        {
            if (value is int intValue)
            {
                return intValue;
            }

            if (value is uint uintValue && uintValue <= int.MaxValue)
            {
                return (int)uintValue;
            }

            if (value is short shortValue)
            {
                return shortValue;
            }

            if (value is ushort ushortValue)
            {
                return ushortValue;
            }
        }

        return 0;
    }

    private static string ReadFirstString(object owner, params string[] memberNames)
    {
        foreach (var value in ReadCandidateValues(owner, memberNames))
        {
            var text = value?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<object?> ReadCandidateValues(object owner, params string[] memberNames)
    {
        foreach (var memberName in memberNames)
        {
            var value = TryGetMemberValue(owner, memberName);
            if (value is not null)
            {
                yield return value;
            }
        }

        foreach (var nestedName in new[] { "Desc", "Description", "Texture", "Resource" })
        {
            var nested = TryGetMemberValue(owner, nestedName);
            if (nested is null)
            {
                continue;
            }

            foreach (var memberName in memberNames)
            {
                var value = TryGetMemberValue(nested, memberName);
                if (value is not null)
                {
                    yield return value;
                }
            }
        }
    }

    private static string DescribeReflectionFailure(Exception ex)
    {
        var root = ex is TargetInvocationException { InnerException: not null } ? ex.InnerException : ex;
        return $"{root.GetType().Name}: {root.Message}";
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
                "Diagnostic control channel for ping, self-test, status, and capability negotiation.",
                "Opened only during explicit diagnostics with a short timeout. It must not move shader values, resource handles, render-target data, or generated preset changes."),
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
            Array.Empty<DalapadResourceCatalogEntry>(),
            DalapadIpcClient.EmptyDebugVisualization,
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
                Array.Empty<DalapadResourceCatalogEntry>(),
                DalapadIpcClient.EmptyDebugVisualization,
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
                Array.Empty<DalapadResourceCatalogEntry>(),
                DalapadIpcClient.EmptyDebugVisualization,
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
            var resourceCatalog = ReadResourceCatalog(root);
            var reportedResources = resourceCatalog.Count > 0
                ? resourceCatalog.Select(resource => resource.Name).ToArray()
                : ReadResourceNames(root);
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
                resourceCatalog,
                DalapadIpcClient.ReadDebugVisualization(root),
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
                Array.Empty<DalapadResourceCatalogEntry>(),
                DalapadIpcClient.EmptyDebugVisualization,
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
                "Named-pipe diagnostic control",
                "High",
                "Low-medium",
                "Current next path. The plugin can ask the addon for ping, self-test, status, and capability rows with short timeouts and no shader or render-target authority."),
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
                "Implemented as an opt-in developer-only plugin probe that invokes RenderTargetManager.Instance and reports pointer/resource presence, dimensions, format names if safely readable, and nullability.",
                "No render-target copy, no shader exposure, no generated-preset changes, and no normal-user UI.",
                "Reports whether GBuffer[0], GBuffer[2], and depth/stencil candidates are observable during live play; stability still requires repeated lifecycle testing."),
            new DalapadBackendStep(
                "3. Dalapad bridge addon spike",
                "Prototype a native/ReShade addon that writes the Stage 1 IPC status file and answers a diagnostic control pipe for ping, self-test, status, and capability negotiation.",
                "Separate package, compile guarded, debug-only, short-timeout, and unable to change generated presets or shader values.",
                "Developer diagnostics can distinguish status-file presence, ReShade registration, pipe listening, self-test response, and intentionally unavailable resources."),
            new DalapadBackendStep(
                "4. Diagnostic resource catalog",
                "Implemented as a metadata-only schema with static candidate names, neutral dimensions, unknown formats, disabled freshness, zero confidence, safety state, metadata source, and disabled reasons over status-file and control-pipe IPC.",
                "No live render-target inspection, no texture handles, no copies, no shader exposure, and no production visual influence.",
                "Debug bundles can verify catalog row shape while resources remain unavailable to .fx code."),
            new DalapadBackendStep(
                "5. Developer-only pointer/resource shape probe",
                "Collect repeated observations from the opt-in shape probe across login, zone change, resolution change, ReShade reload, and plugin reload.",
                "No render-target copy, no texture handles over IPC, no shader exposure, no generated-preset changes, and no normal-user UI.",
                "Reports prove whether candidate resources can be observed consistently across zones and device/lifecycle changes."),
            new DalapadBackendStep(
                "6. Shader contract guard",
                "Add optional FrameSurfaceData fields behind Dalapad availability guards.",
                "Fallback remains NormalField; no required shader variable or include for users without the bridge.",
                "Shaders compile and render identically when Dalapad resources are unavailable."),
            new DalapadBackendStep(
                "7. Compare and calibrate",
                "Add debug views comparing external normal/diffuse/depth confidence against NormalField and screenshot material evidence.",
                "Diagnostic-only comparison; no production visual influence.",
                "Reports identify where external data is better, stale, missing, transparent-only, or mismatched."),
            new DalapadBackendStep(
                "8. Opt-in backend selection",
                "Let FrameSurfaceData prefer external surface data only when the bridge is present, confidence is high, and the user enables the experimental backend.",
                "Default off; generated presets and shaders keep NormalField fallback.",
                "Production shaders can consume a single FrameSurfaceData contract without knowing which backend supplied it.")
            ,
            new DalapadBackendStep(
                "9. Realtime value bridge",
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
