// Dalapad ReShade/native addon first-test implementation.
//
// This file is intentionally not included in Dalashade.sln. Build it only in a
// separate experimental DLL project. It reports status to Dalashade through a
// small JSON file and control pipe. The debug visualization bridge may copy
// render-layer candidates into addon-owned textures. First-party production
// shader use remains optional, default-off, and gated through Dalashade_Dalapad.fxh.

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>

#include <atomic>
#include <array>
#include <chrono>
#include <cstdio>
#include <filesystem>
#include <fstream>
#include <iterator>
#include <sstream>
#include <string>
#include <string_view>
#include <vector>

#include <cstdint>

#if defined(__has_include)
#if __has_include(<reshade.hpp>)
#include <reshade.hpp>
#define DALAPAD_HAS_RESHADE 1
#endif
#endif

#ifndef DALAPAD_HAS_RESHADE
#define DALAPAD_HAS_RESHADE 0
#endif

extern "C" __declspec(dllexport) const char* NAME = "Dalapad";
extern "C" __declspec(dllexport) const char* DESCRIPTION =
    "Dalashade experimental IPC and debug visualization bridge.";

namespace dalapad
{
    constexpr std::string_view ContractVersion = "0.1-diagnostic";
    constexpr std::string_view IpcContractVersion = "0.1-ipc-diagnostic";
    constexpr std::string_view ControlPipeContract = "Dalapad.Control.v1";
    constexpr std::string_view BridgeVersion = "0.1.10-uniform-consumer-sync-pinned-water";
    constexpr std::string_view ResourceCatalogVersion = "0.1-metadata-only";
    constexpr std::string_view DebugVisualizationVersion = "0.7-pinned-water-candidate";
    constexpr std::string_view StatusFileName = "dalapad-status.json";
    constexpr std::string_view ControlPipeName = R"(\\.\pipe\Dalapad.Control.v1)";
    constexpr std::string_view RealtimeUniformsChannel = "Dalapad.RealtimeUniforms.v1";
    constexpr std::string_view SurfaceNormalName = "Dalapad_SurfaceNormal";
    constexpr std::string_view SurfaceDiffuseName = "Dalapad_SurfaceDiffuse";
    constexpr std::string_view SurfaceDepthName = "Dalapad_SurfaceDepth";
    constexpr std::string_view DebugTextureName = "Dalapad_DebugTexture";
    constexpr std::string_view DebugEffectName = "Dalapad_Debug.fx";
    constexpr std::string_view DebugDepthSemantic = "DALAPAD_DEPTH";
    constexpr uint32_t DebugTextureWidth = 256;
    constexpr uint32_t DebugTextureHeight = 256;
    constexpr uint32_t DebugMrtGroupCount = 8;
    constexpr uint32_t DebugMrtSlotCount = 8;
    constexpr uint32_t DebugMrtSourceCount = DebugMrtGroupCount * DebugMrtSlotCount;
    constexpr uint32_t DebugScanSlotCount = 4;
    constexpr uint32_t DebugPinnedCandidateCount = 6;
    constexpr uint32_t DebugLayerCopyFrameInterval = 2;
    constexpr uint32_t DebugSyntheticUploadFrameInterval = 15;
    constexpr uint32_t ReShadeApiVersion = 18;

    using ReShadeRegisterAddonFn = bool (*)(void*, uint32_t);
    using ReShadeUnregisterAddonFn = void (*)(void*);

    struct ResourceStatus
    {
        std::string name;
        std::string source;
        std::string_view kind = "optionalTexture";
        std::string_view availabilityFlag;
        bool available = false;
        unsigned int width = 0;
        unsigned int height = 0;
        std::string_view format = "unknown";
        std::string_view freshness = "disabled";
        float confidence = 0.0f;
        std::string_view safetyState = "metadata-only-unavailable";
        std::string_view metadataSource = "static-contract";
        std::string_view reason = "not probed";
    };

    struct BridgeStatus
    {
        std::string status;
        std::string summary;
        bool reshadeHeaderCompiled = DALAPAD_HAS_RESHADE != 0;
        bool reshadeRegistered = false;
        bool namedPipeOpen = false;
        bool realtimeEnabled = false;
        std::vector<ResourceStatus> resources;
        std::vector<std::string> warnings;
    };

    struct DebugVisualizationSnapshot
    {
        bool enabled = true;
        bool shaderTextureFound = false;
        bool syntheticTextureUploaded = false;
        bool usesSyntheticTexture = true;
        bool readsRenderTargets = false;
        bool copiesRenderTargets = false;
        bool registersGameResources = false;
        uint32_t width = DebugTextureWidth;
        uint32_t height = DebugTextureHeight;
        uint32_t frameCounter = 0;
        uint32_t frameAge = 9999;
        uint32_t observedSourceCount = 0;
        uint32_t copiedSourceCount = 0;
        std::string status = "WaitingForShader";
        std::string source = "synthetic";
        std::string reason = "Dalapad_DebugTexture has not been discovered in a loaded ReShade effect yet.";
    };

    enum class DebugLayerSource : size_t
    {
        Depth = DebugMrtSourceCount,
        Count = DebugMrtSourceCount + 1,
    };

    struct DebugLayerStatus
    {
        std::string_view name;
        std::string_view semantic;
        std::string_view availabilityUniform;
        std::string_view widthUniform;
        std::string_view heightUniform;
        std::string_view classificationHint;
        std::string source;
        bool observed = false;
        bool copied = false;
        uint32_t width = 0;
        uint32_t height = 0;
        uint32_t copyCount = 0;
        uint32_t formatId = 0;
        uint32_t samples = 0;
    };

    struct DebugPinnedCandidate
    {
        std::string_view name;
        std::string_view label;
        std::string_view semantic;
        std::string_view availabilityUniform;
        std::string_view widthUniform;
        std::string_view heightUniform;
        DebugLayerSource source;
        std::string_view classificationHint;
        float confidence = 0.0f;
    };

    struct DebugScanBinding
    {
        std::string_view semantic;
        std::string_view availabilityUniform;
        std::string_view widthUniform;
        std::string_view heightUniform;
    };

    static HANDLE g_stopEvent = nullptr;
    static HANDLE g_pipeThread = nullptr;
    static std::atomic<bool> g_reshadeRegistered = false;
    static std::atomic<bool> g_pipeListening = false;
    static std::atomic<bool> g_debugShaderTextureFound = false;
    static std::atomic<bool> g_debugSyntheticUploaded = false;
    static std::atomic<uint32_t> g_debugFrameCounter = 0;
    static std::atomic<uint32_t> g_debugLastUploadFrame = 0;
    static std::atomic<bool> g_reshadeEventsRegistered = false;

    static size_t LayerIndex(DebugLayerSource source)
    {
        return static_cast<size_t>(source);
    }

    constexpr size_t DebugLayerCount = static_cast<size_t>(DebugLayerSource::Count);

    static bool IsMrtLayer(DebugLayerSource source)
    {
        return LayerIndex(source) < DebugMrtSourceCount;
    }

    static uint32_t MrtGroup(DebugLayerSource source)
    {
        return static_cast<uint32_t>(LayerIndex(source) / DebugMrtSlotCount);
    }

    static uint32_t MrtSlot(DebugLayerSource source)
    {
        return static_cast<uint32_t>(LayerIndex(source) % DebugMrtSlotCount);
    }

    static DebugLayerSource MrtLayer(uint32_t group, uint32_t slot)
    {
        return static_cast<DebugLayerSource>(group * DebugMrtSlotCount + slot);
    }

    static const std::array<DebugScanBinding, DebugScanSlotCount>& ScanBindings()
    {
        static constexpr std::array<DebugScanBinding, DebugScanSlotCount> bindings = {
            DebugScanBinding{ "DALAPAD_SCAN0", "Dalapad_Scan0Available", "Dalapad_Scan0Width", "Dalapad_Scan0Height" },
            DebugScanBinding{ "DALAPAD_SCAN1", "Dalapad_Scan1Available", "Dalapad_Scan1Width", "Dalapad_Scan1Height" },
            DebugScanBinding{ "DALAPAD_SCAN2", "Dalapad_Scan2Available", "Dalapad_Scan2Width", "Dalapad_Scan2Height" },
            DebugScanBinding{ "DALAPAD_SCAN3", "Dalapad_Scan3Available", "Dalapad_Scan3Width", "Dalapad_Scan3Height" },
        };
        return bindings;
    }

    static const std::array<DebugPinnedCandidate, DebugPinnedCandidateCount>& PinnedCandidates()
    {
        static const std::array<DebugPinnedCandidate, DebugPinnedCandidateCount> candidates = {
            DebugPinnedCandidate{
                "pinned_primary_normal",
                "Pinned dense surface detail",
                "DALAPAD_PINNED_NORMAL",
                "Dalapad_PinnedNormalAvailable",
                "Dalapad_PinnedNormalWidth",
                "Dalapad_PinnedNormalHeight",
                MrtLayer(0, 0),
                "candidate-dense-surface-detail-g0m0",
                0.70f },
            DebugPinnedCandidate{
                "pinned_albedo_material",
                "Pinned albedo/luma candidate",
                "DALAPAD_PINNED_ALBEDO",
                "Dalapad_PinnedAlbedoAvailable",
                "Dalapad_PinnedAlbedoWidth",
                "Dalapad_PinnedAlbedoHeight",
                MrtLayer(0, 1),
                "candidate-albedo-luma-g0m1",
                0.55f },
            DebugPinnedCandidate{
                "pinned_surface_mask",
                "Pinned surface/object mask",
                "DALAPAD_PINNED_MASK",
                "Dalapad_PinnedMaskAvailable",
                "Dalapad_PinnedMaskWidth",
                "Dalapad_PinnedMaskHeight",
                MrtLayer(0, 2),
                "candidate-surface-object-mask-g0m2",
                0.60f },
            DebugPinnedCandidate{
                "pinned_alternate_normal",
                "Pinned alternate dense surface detail",
                "DALAPAD_PINNED_NORMAL_ALT",
                "Dalapad_PinnedNormalAltAvailable",
                "Dalapad_PinnedNormalAltWidth",
                "Dalapad_PinnedNormalAltHeight",
                MrtLayer(1, 1),
                "candidate-dense-surface-detail-g1m1",
                0.50f },
            DebugPinnedCandidate{
                "pinned_emissive_lighting",
                "Pinned emissive/lighting candidate",
                "DALAPAD_PINNED_EMISSIVE",
                "Dalapad_PinnedEmissiveAvailable",
                "Dalapad_PinnedEmissiveWidth",
                "Dalapad_PinnedEmissiveHeight",
                MrtLayer(4, 1),
                "candidate-emissive-lighting-g4m1",
                0.58f },
            DebugPinnedCandidate{
                "pinned_water_surface",
                "Pinned water/reflection surface candidate",
                "DALAPAD_PINNED_WATER_SURFACE",
                "Dalapad_PinnedWaterSurfaceAvailable",
                "Dalapad_PinnedWaterSurfaceWidth",
                "Dalapad_PinnedWaterSurfaceHeight",
                MrtLayer(7, 0),
                "candidate-water-reflection-surface-g7m0-page14-top-left",
                0.62f },
        };
        return candidates;
    }

    static const std::array<std::string_view, DebugLayerCount>& LayerNames()
    {
        static constexpr std::array<std::string_view, DebugLayerCount> names = {
            "group0_mrt0", "group0_mrt1", "group0_mrt2", "group0_mrt3", "group0_mrt4", "group0_mrt5", "group0_mrt6", "group0_mrt7",
            "group1_mrt0", "group1_mrt1", "group1_mrt2", "group1_mrt3", "group1_mrt4", "group1_mrt5", "group1_mrt6", "group1_mrt7",
            "group2_mrt0", "group2_mrt1", "group2_mrt2", "group2_mrt3", "group2_mrt4", "group2_mrt5", "group2_mrt6", "group2_mrt7",
            "group3_mrt0", "group3_mrt1", "group3_mrt2", "group3_mrt3", "group3_mrt4", "group3_mrt5", "group3_mrt6", "group3_mrt7",
            "group4_mrt0", "group4_mrt1", "group4_mrt2", "group4_mrt3", "group4_mrt4", "group4_mrt5", "group4_mrt6", "group4_mrt7",
            "group5_mrt0", "group5_mrt1", "group5_mrt2", "group5_mrt3", "group5_mrt4", "group5_mrt5", "group5_mrt6", "group5_mrt7",
            "group6_mrt0", "group6_mrt1", "group6_mrt2", "group6_mrt3", "group6_mrt4", "group6_mrt5", "group6_mrt6", "group6_mrt7",
            "group7_mrt0", "group7_mrt1", "group7_mrt2", "group7_mrt3", "group7_mrt4", "group7_mrt5", "group7_mrt6", "group7_mrt7",
            "depth"
        };
        return names;
    }

    static const std::array<std::string_view, DebugLayerCount>& LayerSemantics()
    {
        static constexpr std::array<std::string_view, DebugLayerCount> semantics = {
            "DALAPAD_G0_MRT0", "DALAPAD_G0_MRT1", "DALAPAD_G0_MRT2", "DALAPAD_G0_MRT3", "DALAPAD_G0_MRT4", "DALAPAD_G0_MRT5", "DALAPAD_G0_MRT6", "DALAPAD_G0_MRT7",
            "DALAPAD_G1_MRT0", "DALAPAD_G1_MRT1", "DALAPAD_G1_MRT2", "DALAPAD_G1_MRT3", "DALAPAD_G1_MRT4", "DALAPAD_G1_MRT5", "DALAPAD_G1_MRT6", "DALAPAD_G1_MRT7",
            "DALAPAD_G2_MRT0", "DALAPAD_G2_MRT1", "DALAPAD_G2_MRT2", "DALAPAD_G2_MRT3", "DALAPAD_G2_MRT4", "DALAPAD_G2_MRT5", "DALAPAD_G2_MRT6", "DALAPAD_G2_MRT7",
            "DALAPAD_G3_MRT0", "DALAPAD_G3_MRT1", "DALAPAD_G3_MRT2", "DALAPAD_G3_MRT3", "DALAPAD_G3_MRT4", "DALAPAD_G3_MRT5", "DALAPAD_G3_MRT6", "DALAPAD_G3_MRT7",
            "DALAPAD_G4_MRT0", "DALAPAD_G4_MRT1", "DALAPAD_G4_MRT2", "DALAPAD_G4_MRT3", "DALAPAD_G4_MRT4", "DALAPAD_G4_MRT5", "DALAPAD_G4_MRT6", "DALAPAD_G4_MRT7",
            "DALAPAD_G5_MRT0", "DALAPAD_G5_MRT1", "DALAPAD_G5_MRT2", "DALAPAD_G5_MRT3", "DALAPAD_G5_MRT4", "DALAPAD_G5_MRT5", "DALAPAD_G5_MRT6", "DALAPAD_G5_MRT7",
            "DALAPAD_G6_MRT0", "DALAPAD_G6_MRT1", "DALAPAD_G6_MRT2", "DALAPAD_G6_MRT3", "DALAPAD_G6_MRT4", "DALAPAD_G6_MRT5", "DALAPAD_G6_MRT6", "DALAPAD_G6_MRT7",
            "DALAPAD_G7_MRT0", "DALAPAD_G7_MRT1", "DALAPAD_G7_MRT2", "DALAPAD_G7_MRT3", "DALAPAD_G7_MRT4", "DALAPAD_G7_MRT5", "DALAPAD_G7_MRT6", "DALAPAD_G7_MRT7",
            DebugDepthSemantic
        };
        return semantics;
    }

    static const std::array<std::string_view, DebugLayerCount>& LayerAvailabilityUniforms()
    {
        static constexpr std::array<std::string_view, DebugLayerCount> uniforms = {
            "Dalapad_G0MRT0Available", "Dalapad_G0MRT1Available", "Dalapad_G0MRT2Available", "Dalapad_G0MRT3Available", "Dalapad_G0MRT4Available", "Dalapad_G0MRT5Available", "Dalapad_G0MRT6Available", "Dalapad_G0MRT7Available",
            "Dalapad_G1MRT0Available", "Dalapad_G1MRT1Available", "Dalapad_G1MRT2Available", "Dalapad_G1MRT3Available", "Dalapad_G1MRT4Available", "Dalapad_G1MRT5Available", "Dalapad_G1MRT6Available", "Dalapad_G1MRT7Available",
            "Dalapad_G2MRT0Available", "Dalapad_G2MRT1Available", "Dalapad_G2MRT2Available", "Dalapad_G2MRT3Available", "Dalapad_G2MRT4Available", "Dalapad_G2MRT5Available", "Dalapad_G2MRT6Available", "Dalapad_G2MRT7Available",
            "Dalapad_G3MRT0Available", "Dalapad_G3MRT1Available", "Dalapad_G3MRT2Available", "Dalapad_G3MRT3Available", "Dalapad_G3MRT4Available", "Dalapad_G3MRT5Available", "Dalapad_G3MRT6Available", "Dalapad_G3MRT7Available",
            "Dalapad_G4MRT0Available", "Dalapad_G4MRT1Available", "Dalapad_G4MRT2Available", "Dalapad_G4MRT3Available", "Dalapad_G4MRT4Available", "Dalapad_G4MRT5Available", "Dalapad_G4MRT6Available", "Dalapad_G4MRT7Available",
            "Dalapad_G5MRT0Available", "Dalapad_G5MRT1Available", "Dalapad_G5MRT2Available", "Dalapad_G5MRT3Available", "Dalapad_G5MRT4Available", "Dalapad_G5MRT5Available", "Dalapad_G5MRT6Available", "Dalapad_G5MRT7Available",
            "Dalapad_G6MRT0Available", "Dalapad_G6MRT1Available", "Dalapad_G6MRT2Available", "Dalapad_G6MRT3Available", "Dalapad_G6MRT4Available", "Dalapad_G6MRT5Available", "Dalapad_G6MRT6Available", "Dalapad_G6MRT7Available",
            "Dalapad_G7MRT0Available", "Dalapad_G7MRT1Available", "Dalapad_G7MRT2Available", "Dalapad_G7MRT3Available", "Dalapad_G7MRT4Available", "Dalapad_G7MRT5Available", "Dalapad_G7MRT6Available", "Dalapad_G7MRT7Available",
            "Dalapad_DepthAvailable"
        };
        return uniforms;
    }

    static const std::array<std::string_view, DebugLayerCount>& LayerWidthUniforms()
    {
        static constexpr std::array<std::string_view, DebugLayerCount> uniforms = {
            "Dalapad_G0MRT0Width", "Dalapad_G0MRT1Width", "Dalapad_G0MRT2Width", "Dalapad_G0MRT3Width", "Dalapad_G0MRT4Width", "Dalapad_G0MRT5Width", "Dalapad_G0MRT6Width", "Dalapad_G0MRT7Width",
            "Dalapad_G1MRT0Width", "Dalapad_G1MRT1Width", "Dalapad_G1MRT2Width", "Dalapad_G1MRT3Width", "Dalapad_G1MRT4Width", "Dalapad_G1MRT5Width", "Dalapad_G1MRT6Width", "Dalapad_G1MRT7Width",
            "Dalapad_G2MRT0Width", "Dalapad_G2MRT1Width", "Dalapad_G2MRT2Width", "Dalapad_G2MRT3Width", "Dalapad_G2MRT4Width", "Dalapad_G2MRT5Width", "Dalapad_G2MRT6Width", "Dalapad_G2MRT7Width",
            "Dalapad_G3MRT0Width", "Dalapad_G3MRT1Width", "Dalapad_G3MRT2Width", "Dalapad_G3MRT3Width", "Dalapad_G3MRT4Width", "Dalapad_G3MRT5Width", "Dalapad_G3MRT6Width", "Dalapad_G3MRT7Width",
            "Dalapad_G4MRT0Width", "Dalapad_G4MRT1Width", "Dalapad_G4MRT2Width", "Dalapad_G4MRT3Width", "Dalapad_G4MRT4Width", "Dalapad_G4MRT5Width", "Dalapad_G4MRT6Width", "Dalapad_G4MRT7Width",
            "Dalapad_G5MRT0Width", "Dalapad_G5MRT1Width", "Dalapad_G5MRT2Width", "Dalapad_G5MRT3Width", "Dalapad_G5MRT4Width", "Dalapad_G5MRT5Width", "Dalapad_G5MRT6Width", "Dalapad_G5MRT7Width",
            "Dalapad_G6MRT0Width", "Dalapad_G6MRT1Width", "Dalapad_G6MRT2Width", "Dalapad_G6MRT3Width", "Dalapad_G6MRT4Width", "Dalapad_G6MRT5Width", "Dalapad_G6MRT6Width", "Dalapad_G6MRT7Width",
            "Dalapad_G7MRT0Width", "Dalapad_G7MRT1Width", "Dalapad_G7MRT2Width", "Dalapad_G7MRT3Width", "Dalapad_G7MRT4Width", "Dalapad_G7MRT5Width", "Dalapad_G7MRT6Width", "Dalapad_G7MRT7Width",
            "Dalapad_DepthWidth"
        };
        return uniforms;
    }

    static const std::array<std::string_view, DebugLayerCount>& LayerHeightUniforms()
    {
        static constexpr std::array<std::string_view, DebugLayerCount> uniforms = {
            "Dalapad_G0MRT0Height", "Dalapad_G0MRT1Height", "Dalapad_G0MRT2Height", "Dalapad_G0MRT3Height", "Dalapad_G0MRT4Height", "Dalapad_G0MRT5Height", "Dalapad_G0MRT6Height", "Dalapad_G0MRT7Height",
            "Dalapad_G1MRT0Height", "Dalapad_G1MRT1Height", "Dalapad_G1MRT2Height", "Dalapad_G1MRT3Height", "Dalapad_G1MRT4Height", "Dalapad_G1MRT5Height", "Dalapad_G1MRT6Height", "Dalapad_G1MRT7Height",
            "Dalapad_G2MRT0Height", "Dalapad_G2MRT1Height", "Dalapad_G2MRT2Height", "Dalapad_G2MRT3Height", "Dalapad_G2MRT4Height", "Dalapad_G2MRT5Height", "Dalapad_G2MRT6Height", "Dalapad_G2MRT7Height",
            "Dalapad_G3MRT0Height", "Dalapad_G3MRT1Height", "Dalapad_G3MRT2Height", "Dalapad_G3MRT3Height", "Dalapad_G3MRT4Height", "Dalapad_G3MRT5Height", "Dalapad_G3MRT6Height", "Dalapad_G3MRT7Height",
            "Dalapad_G4MRT0Height", "Dalapad_G4MRT1Height", "Dalapad_G4MRT2Height", "Dalapad_G4MRT3Height", "Dalapad_G4MRT4Height", "Dalapad_G4MRT5Height", "Dalapad_G4MRT6Height", "Dalapad_G4MRT7Height",
            "Dalapad_G5MRT0Height", "Dalapad_G5MRT1Height", "Dalapad_G5MRT2Height", "Dalapad_G5MRT3Height", "Dalapad_G5MRT4Height", "Dalapad_G5MRT5Height", "Dalapad_G5MRT6Height", "Dalapad_G5MRT7Height",
            "Dalapad_G6MRT0Height", "Dalapad_G6MRT1Height", "Dalapad_G6MRT2Height", "Dalapad_G6MRT3Height", "Dalapad_G6MRT4Height", "Dalapad_G6MRT5Height", "Dalapad_G6MRT6Height", "Dalapad_G6MRT7Height",
            "Dalapad_G7MRT0Height", "Dalapad_G7MRT1Height", "Dalapad_G7MRT2Height", "Dalapad_G7MRT3Height", "Dalapad_G7MRT4Height", "Dalapad_G7MRT5Height", "Dalapad_G7MRT6Height", "Dalapad_G7MRT7Height",
            "Dalapad_DepthHeight"
        };
        return uniforms;
    }

    static std::string_view LayerName(DebugLayerSource source)
    {
        const auto index = LayerIndex(source);
        return index < DebugLayerCount ? LayerNames()[index] : "unknown";
    }

    static std::string_view LayerSemantic(DebugLayerSource source)
    {
        const auto index = LayerIndex(source);
        return index < DebugLayerCount ? LayerSemantics()[index] : "";
    }

    static std::string_view LayerAvailabilityUniform(DebugLayerSource source)
    {
        const auto index = LayerIndex(source);
        return index < DebugLayerCount ? LayerAvailabilityUniforms()[index] : "";
    }

    static std::string_view LayerWidthUniform(DebugLayerSource source)
    {
        const auto index = LayerIndex(source);
        return index < DebugLayerCount ? LayerWidthUniforms()[index] : "";
    }

    static std::string_view LayerHeightUniform(DebugLayerSource source)
    {
        const auto index = LayerIndex(source);
        return index < DebugLayerCount ? LayerHeightUniforms()[index] : "";
    }

    static std::string LayerCandidateSource(DebugLayerSource source)
    {
        if (!IsMrtLayer(source))
            return "pre-ReShade MRT group DSV heuristic";

        std::ostringstream label;
        label << "pre-ReShade MRT group " << MrtGroup(source) << " slot " << MrtSlot(source);
        return label.str();
    }

    static std::string_view DebugLayerClassificationHint(DebugLayerSource source)
    {
        for (const auto& candidate : PinnedCandidates())
        {
            if (LayerIndex(candidate.source) == LayerIndex(source))
                return candidate.classificationHint;
        }

        if (!IsMrtLayer(source))
            return "candidate-depth-unvalidated";

        return "unclassified-debug-candidate";
    }

    static HMODULE GetReShadeModuleHandle()
    {
        HMODULE modules[1024] = {};
        DWORD bytesNeeded = 0;
        if (!K32EnumProcessModules(GetCurrentProcess(), modules, sizeof(modules), &bytesNeeded))
            return nullptr;

        const DWORD count = min(bytesNeeded, static_cast<DWORD>(sizeof(modules))) / sizeof(HMODULE);
        for (DWORD index = 0; index < count; ++index)
        {
            if (GetProcAddress(modules[index], "ReShadeRegisterAddon") != nullptr &&
                GetProcAddress(modules[index], "ReShadeUnregisterAddon") != nullptr)
            {
                return modules[index];
            }
        }

        return nullptr;
    }

    static bool TryRegisterWithReShade(HMODULE module)
    {
        const auto reshadeModule = GetReShadeModuleHandle();
        if (reshadeModule == nullptr)
            return false;

        const auto registerAddon = reinterpret_cast<ReShadeRegisterAddonFn>(
            GetProcAddress(reshadeModule, "ReShadeRegisterAddon"));
        return registerAddon != nullptr && registerAddon(module, ReShadeApiVersion);
    }

#if DALAPAD_HAS_RESHADE
    struct DebugLayerCandidate
    {
        reshade::api::resource resource = { 0 };
        reshade::api::resource_desc desc = {};
        reshade::api::resource_usage expectedUsage = reshade::api::resource_usage::undefined;
        bool valid = false;
    };

    struct DebugLayerCopy
    {
        reshade::api::device* device = nullptr;
        reshade::api::resource resource = { 0 };
        reshade::api::resource_view srv = { 0 };
        reshade::api::resource_desc desc = {};
        reshade::api::resource_usage currentUsage = reshade::api::resource_usage::undefined;
        bool valid = false;
    };

    static std::array<DebugLayerCandidate, DebugLayerCount> g_debugLayerCandidates = {};
    static std::array<DebugLayerCopy, DebugLayerCount> g_debugLayerCopies = {};
    static std::array<std::atomic<bool>, DebugLayerCount> g_debugLayerObserved = {};
    static std::array<std::atomic<bool>, DebugLayerCount> g_debugLayerCopied = {};
    static std::array<std::atomic<uint32_t>, DebugLayerCount> g_debugLayerWidth = {};
    static std::array<std::atomic<uint32_t>, DebugLayerCount> g_debugLayerHeight = {};
    static std::array<std::atomic<uint32_t>, DebugLayerCount> g_debugLayerCopyCount = {};
    static std::array<std::atomic<uint32_t>, DebugLayerCount> g_debugLayerFormat = {};
    static std::array<std::atomic<uint32_t>, DebugLayerCount> g_debugLayerSamples = {};
    static std::atomic<uint32_t> g_debugCaptureGroupIndex = 0;
    static std::atomic<bool> g_debugInsideReShadeEffects = false;

    static int32_t GetIntUniform(reshade::api::effect_runtime* runtime, const char* name, int32_t fallback);

    static bool IsEligibleDebugLayerDesc(const reshade::api::resource_desc& desc)
    {
        return desc.type == reshade::api::resource_type::texture_2d &&
            desc.texture.width > 0 &&
            desc.texture.height > 0 &&
            desc.texture.format != reshade::api::format::unknown &&
            desc.texture.samples <= 1 &&
            desc.texture.depth_or_layers >= 1;
    }

    static bool SameDebugLayerShape(const reshade::api::resource_desc& left, const reshade::api::resource_desc& right)
    {
        return left.type == right.type &&
            left.texture.width == right.texture.width &&
            left.texture.height == right.texture.height &&
            left.texture.format == right.texture.format &&
            left.texture.depth_or_layers == right.texture.depth_or_layers;
    }

    static void DestroyDebugLayerCopy(DebugLayerCopy& copy)
    {
        if (copy.device != nullptr)
        {
            if (copy.srv.handle != 0)
                copy.device->destroy_resource_view(copy.srv);

            if (copy.resource.handle != 0)
                copy.device->destroy_resource(copy.resource);
        }

        copy = {};
    }

    static bool EnsureDebugLayerCopy(reshade::api::device* device, DebugLayerSource source, const reshade::api::resource_desc& sourceDesc)
    {
        if (device == nullptr || !IsEligibleDebugLayerDesc(sourceDesc))
            return false;

        auto& copy = g_debugLayerCopies[LayerIndex(source)];
        if (copy.valid && copy.device == device && SameDebugLayerShape(copy.desc, sourceDesc))
            return true;

        DestroyDebugLayerCopy(copy);

        const reshade::api::resource_usage copyUsage =
            reshade::api::resource_usage::copy_dest | reshade::api::resource_usage::shader_resource;
        reshade::api::resource_desc copyDesc(
            sourceDesc.type,
            sourceDesc.texture.width,
            sourceDesc.texture.height,
            1,
            1,
            sourceDesc.texture.format,
            1,
            reshade::api::memory_heap::default_,
            copyUsage);

        if (!device->check_format_support(sourceDesc.texture.format, reshade::api::resource_usage::shader_resource) ||
            !device->create_resource(copyDesc, nullptr, reshade::api::resource_usage::copy_dest, &copy.resource))
        {
            copy = {};
            return false;
        }

        reshade::api::resource_view_desc viewDesc(
            reshade::api::resource_view_type::texture_2d,
            sourceDesc.texture.format,
            0,
            1,
            0,
            1);

        if (!device->create_resource_view(copy.resource, reshade::api::resource_usage::shader_resource, viewDesc, &copy.srv))
        {
            DestroyDebugLayerCopy(copy);
            return false;
        }

        copy.device = device;
        copy.desc = copyDesc;
        copy.currentUsage = reshade::api::resource_usage::copy_dest;
        copy.valid = true;
        return true;
    }

    static void CaptureDebugLayerCandidate(
        reshade::api::device* device,
        DebugLayerSource source,
        reshade::api::resource_view view,
        reshade::api::resource_usage expectedUsage)
    {
        if (device == nullptr || view.handle == 0)
            return;

        const auto resource = device->get_resource_from_view(view);
        if (resource.handle == 0)
            return;

        const auto desc = device->get_resource_desc(resource);
        if (!IsEligibleDebugLayerDesc(desc))
            return;

        auto& candidate = g_debugLayerCandidates[LayerIndex(source)];
        candidate.resource = resource;
        candidate.desc = desc;
        candidate.expectedUsage = expectedUsage;
        candidate.valid = true;

        g_debugLayerObserved[LayerIndex(source)] = true;
        g_debugLayerWidth[LayerIndex(source)] = desc.texture.width;
        g_debugLayerHeight[LayerIndex(source)] = desc.texture.height;
        g_debugLayerFormat[LayerIndex(source)] = static_cast<uint32_t>(desc.texture.format);
        g_debugLayerSamples[LayerIndex(source)] = desc.texture.samples;
    }

    static bool IsEligibleDebugLayerView(reshade::api::device* device, reshade::api::resource_view view)
    {
        if (device == nullptr || view.handle == 0)
            return false;

        const auto resource = device->get_resource_from_view(view);
        if (resource.handle == 0)
            return false;

        return IsEligibleDebugLayerDesc(device->get_resource_desc(resource));
    }

    static uint32_t CountEligibleDebugLayerViews(
        reshade::api::device* device,
        uint32_t count,
        const reshade::api::resource_view* rtvs)
    {
        if (device == nullptr || rtvs == nullptr)
            return 0;

        const uint32_t slots = count < DebugMrtSlotCount ? count : DebugMrtSlotCount;
        uint32_t eligible = 0;
        for (uint32_t slot = 0; slot < slots; ++slot)
        {
            if (IsEligibleDebugLayerView(device, rtvs[slot]))
                ++eligible;
        }

        return eligible;
    }

    static void CopyDebugLayerCandidate(reshade::api::command_list* cmdList, DebugLayerSource source)
    {
        if (cmdList == nullptr)
            return;

        auto* device = cmdList->get_device();
        auto& candidate = g_debugLayerCandidates[LayerIndex(source)];
        if (device == nullptr || !candidate.valid || candidate.resource.handle == 0)
            return;

        if (!EnsureDebugLayerCopy(device, source, candidate.desc))
            return;

        auto& copy = g_debugLayerCopies[LayerIndex(source)];
        if (!copy.valid || copy.resource.handle == 0)
            return;

        if (copy.currentUsage != reshade::api::resource_usage::copy_dest)
        {
            cmdList->barrier(copy.resource, copy.currentUsage, reshade::api::resource_usage::copy_dest);
            copy.currentUsage = reshade::api::resource_usage::copy_dest;
        }

        cmdList->barrier(candidate.resource, candidate.expectedUsage, reshade::api::resource_usage::copy_source);
        cmdList->copy_texture_region(candidate.resource, 0, nullptr, copy.resource, 0, nullptr);
        cmdList->barrier(candidate.resource, reshade::api::resource_usage::copy_source, candidate.expectedUsage);
        cmdList->barrier(copy.resource, reshade::api::resource_usage::copy_dest, reshade::api::resource_usage::shader_resource);
        copy.currentUsage = reshade::api::resource_usage::shader_resource;

        g_debugLayerCopied[LayerIndex(source)] = true;
        g_debugLayerCopyCount[LayerIndex(source)] = g_debugLayerCopyCount[LayerIndex(source)].load() + 1;
        g_debugLayerWidth[LayerIndex(source)] = candidate.desc.texture.width;
        g_debugLayerHeight[LayerIndex(source)] = candidate.desc.texture.height;
        g_debugLayerFormat[LayerIndex(source)] = static_cast<uint32_t>(candidate.desc.texture.format);
        g_debugLayerSamples[LayerIndex(source)] = candidate.desc.texture.samples;
    }

    static void CopyDebugScanPageCandidates(reshade::api::command_list* cmdList, reshade::api::effect_runtime* runtime)
    {
        int32_t page = GetIntUniform(runtime, "Dalapad_QuadPage", 0);
        const int32_t maxPage = static_cast<int32_t>((DebugMrtSourceCount / DebugScanSlotCount) - 1);
        if (page < 0)
            page = 0;
        if (page > maxPage)
            page = maxPage;

        const uint32_t pageStart = static_cast<uint32_t>(page) * DebugScanSlotCount;
        for (uint32_t slot = 0; slot < DebugScanSlotCount; ++slot)
            CopyDebugLayerCandidate(cmdList, static_cast<DebugLayerSource>(pageStart + slot));
    }

    static void CopyPinnedDebugCandidates(reshade::api::command_list* cmdList)
    {
        for (const auto& candidate : PinnedCandidates())
            CopyDebugLayerCandidate(cmdList, candidate.source);
    }

    static void CopyDebugLayerCandidates(reshade::api::command_list* cmdList, reshade::api::effect_runtime* runtime)
    {
        const uint32_t frame = g_debugFrameCounter.load();
        if (frame > 0 && (frame % DebugLayerCopyFrameInterval) != 0)
            return;

        CopyPinnedDebugCandidates(cmdList);

        const bool debugShaderLoaded = g_debugShaderTextureFound.load();
        const int32_t debugSource = GetIntUniform(runtime, "Dalapad_DebugSource", 0);
        const int32_t debugMode = GetIntUniform(runtime, "Dalapad_DebugMode", 0);
        const bool debugViewActive = debugShaderLoaded && debugMode > 0;
        if (!debugViewActive)
            return;

        if (debugMode >= 11 && debugMode <= 13)
        {
            CopyDebugScanPageCandidates(cmdList, runtime);
            return;
        }

        if (debugSource >= 1 && debugSource <= 4)
        {
            CopyDebugScanPageCandidates(cmdList, runtime);
        }
        else if (debugSource == 5)
        {
            CopyDebugLayerCandidate(cmdList, DebugLayerSource::Depth);
        }
    }

    static void ResetDebugLayerCandidatesForNextFrame()
    {
        for (auto& candidate : g_debugLayerCandidates)
            candidate = {};

        g_debugCaptureGroupIndex = 0;
    }

    static std::array<DebugLayerStatus, DebugLayerCount> BuildDebugLayerStatuses()
    {
        const auto build = [](DebugLayerSource source) {
            const auto index = LayerIndex(source);
            return DebugLayerStatus{
                LayerName(source),
                LayerSemantic(source),
                LayerAvailabilityUniform(source),
                LayerWidthUniform(source),
                LayerHeightUniform(source),
                DebugLayerClassificationHint(source),
                LayerCandidateSource(source),
                g_debugLayerObserved[index].load(),
                g_debugLayerCopied[index].load(),
                g_debugLayerWidth[index].load(),
                g_debugLayerHeight[index].load(),
                g_debugLayerCopyCount[index].load(),
                g_debugLayerFormat[index].load(),
                g_debugLayerSamples[index].load() };
        };

        std::array<DebugLayerStatus, DebugLayerCount> statuses = {};
        for (size_t index = 0; index < DebugLayerCount; ++index)
            statuses[index] = build(static_cast<DebugLayerSource>(index));

        return statuses;
    }

    static std::array<uint32_t, DebugTextureWidth * DebugTextureHeight> BuildSyntheticDebugPixels(uint32_t frame)
    {
        std::array<uint32_t, DebugTextureWidth * DebugTextureHeight> pixels = {};
        for (uint32_t y = 0; y < DebugTextureHeight; ++y)
        {
            for (uint32_t x = 0; x < DebugTextureWidth; ++x)
            {
                const uint32_t checker = ((x / 16) + (y / 16) + (frame / 20)) & 1;
                const uint8_t r = static_cast<uint8_t>((x * 255u) / (DebugTextureWidth - 1));
                const uint8_t g = static_cast<uint8_t>((y * 255u) / (DebugTextureHeight - 1));
                const uint8_t b = checker ? 255 : 48;
                const uint8_t a = 255;
                pixels[y * DebugTextureWidth + x] =
                    (static_cast<uint32_t>(a) << 24) |
                    (static_cast<uint32_t>(b) << 16) |
                    (static_cast<uint32_t>(g) << 8) |
                    static_cast<uint32_t>(r);
            }
        }

        return pixels;
    }

    static const std::array<const char*, 9>& UniformConsumerEffects()
    {
        static constexpr std::array<const char*, 9> effects = {
            "Dalapad_Debug.fx",
            "Dalashade_SceneGI.fx",
            "Dalashade_FrameDataDebug.fx",
            "Dalashade_AdaptiveGrade.fx",
            "Dalashade_WeatherAtmosphere.fx",
            "Dalashade_AtmosphereBloom.fx",
            "Dalashade_ContactTone.fx",
            "Dalashade_SurfaceReflection.fx",
            "Dalashade_SmartSharpen.fx",
        };

        return effects;
    }

    static bool SetIntUniform(reshade::api::effect_runtime* runtime, const char* name, int32_t value)
    {
        if (runtime == nullptr || name == nullptr)
            return false;

        bool updated = false;
        for (const char* effectName : UniformConsumerEffects())
        {
            const auto variable = runtime->find_uniform_variable(effectName, name);
            if (variable.handle == 0)
                continue;

            runtime->set_uniform_value_int(variable, &value, 1);
            updated = true;
        }

        if (!updated)
        {
            const auto variable = runtime->find_uniform_variable(nullptr, name);
            if (variable.handle != 0)
            {
                runtime->set_uniform_value_int(variable, &value, 1);
                updated = true;
            }
        }

        return updated;
    }

    static bool SetFloatUniform(reshade::api::effect_runtime* runtime, const char* name, float value)
    {
        if (runtime == nullptr || name == nullptr)
            return false;

        bool updated = false;
        for (const char* effectName : UniformConsumerEffects())
        {
            const auto variable = runtime->find_uniform_variable(effectName, name);
            if (variable.handle == 0)
                continue;

            runtime->set_uniform_value_float(variable, &value, 1);
            updated = true;
        }

        if (!updated)
        {
            const auto variable = runtime->find_uniform_variable(nullptr, name);
            if (variable.handle != 0)
            {
                runtime->set_uniform_value_float(variable, &value, 1);
                updated = true;
            }
        }

        return updated;
    }

    static int32_t GetIntUniform(reshade::api::effect_runtime* runtime, const char* name, int32_t fallback)
    {
        if (runtime == nullptr)
            return fallback;

        const auto variable = runtime->find_uniform_variable(nullptr, name);
        if (variable.handle == 0)
            return fallback;

        int32_t value = fallback;
        runtime->get_uniform_value_int(variable, &value, 1);
        return value;
    }

    static void BindDebugLayerTextures(reshade::api::effect_runtime* runtime)
    {
        if (runtime == nullptr)
            return;

        for (const auto& candidate : PinnedCandidates())
        {
            const auto index = LayerIndex(candidate.source);
            const auto& copy = g_debugLayerCopies[index];
            const bool copied = g_debugLayerCopied[index].load() && copy.valid && copy.srv.handle != 0;
            const reshade::api::resource_view view = copied ? copy.srv : reshade::api::resource_view{ 0 };

            runtime->update_texture_bindings(candidate.semantic.data(), view, view);
            SetIntUniform(runtime, candidate.availabilityUniform.data(), copied ? 1 : 0);
            SetIntUniform(runtime, candidate.widthUniform.data(), copied ? static_cast<int32_t>(g_debugLayerWidth[index].load()) : 0);
            SetIntUniform(runtime, candidate.heightUniform.data(), copied ? static_cast<int32_t>(g_debugLayerHeight[index].load()) : 0);
        }

        int32_t page = GetIntUniform(runtime, "Dalapad_QuadPage", 0);
        const int32_t maxPage = static_cast<int32_t>((DebugMrtSourceCount / DebugScanSlotCount) - 1);
        if (page < 0)
            page = 0;
        if (page > maxPage)
            page = maxPage;

        const uint32_t pageStart = static_cast<uint32_t>(page) * DebugScanSlotCount;
        SetIntUniform(runtime, "Dalapad_ScanPageStart", static_cast<int32_t>(pageStart));

        const auto& scanBindings = ScanBindings();
        for (uint32_t slot = 0; slot < DebugScanSlotCount; ++slot)
        {
            const uint32_t sourceIndex = pageStart + slot;
            const auto& binding = scanBindings[slot];
            const bool sourceValid = sourceIndex < DebugMrtSourceCount;
            const auto& copy = sourceValid ? g_debugLayerCopies[sourceIndex] : g_debugLayerCopies[0];
            const bool copied = sourceValid && g_debugLayerCopied[sourceIndex].load() && copy.valid && copy.srv.handle != 0;
            const reshade::api::resource_view view = copied ? copy.srv : reshade::api::resource_view{ 0 };

            runtime->update_texture_bindings(binding.semantic.data(), view, view);
            SetIntUniform(runtime, binding.availabilityUniform.data(), copied ? 1 : 0);
            SetIntUniform(runtime, binding.widthUniform.data(), copied ? static_cast<int32_t>(g_debugLayerWidth[sourceIndex].load()) : 0);
            SetIntUniform(runtime, binding.heightUniform.data(), copied ? static_cast<int32_t>(g_debugLayerHeight[sourceIndex].load()) : 0);
        }

        const auto depthIndex = LayerIndex(DebugLayerSource::Depth);
        const auto& depthCopy = g_debugLayerCopies[depthIndex];
        const bool depthCopied = g_debugLayerCopied[depthIndex].load() && depthCopy.valid && depthCopy.srv.handle != 0;
        const reshade::api::resource_view depthView = depthCopied ? depthCopy.srv : reshade::api::resource_view{ 0 };
        runtime->update_texture_bindings(DebugDepthSemantic.data(), depthView, depthView);
        SetIntUniform(runtime, "Dalapad_DepthAvailable", depthCopied ? 1 : 0);
        SetIntUniform(runtime, "Dalapad_DepthWidth", depthCopied ? static_cast<int32_t>(g_debugLayerWidth[depthIndex].load()) : 0);
        SetIntUniform(runtime, "Dalapad_DepthHeight", depthCopied ? static_cast<int32_t>(g_debugLayerHeight[depthIndex].load()) : 0);
    }

    static void UpdateDebugRuntime(reshade::api::effect_runtime* runtime)
    {
        if (runtime == nullptr)
            return;

        const uint32_t frame = g_debugFrameCounter.fetch_add(1) + 1;
        const auto texture = runtime->find_texture_variable(nullptr, DebugTextureName.data());
        const bool textureFound = texture.handle != 0;
        g_debugShaderTextureFound = textureFound;

        const uint32_t lastUploadBeforeUpdate = g_debugLastUploadFrame.load();
        if (textureFound && (!g_debugSyntheticUploaded.load() || frame - lastUploadBeforeUpdate >= DebugSyntheticUploadFrameInterval))
        {
            const auto pixels = BuildSyntheticDebugPixels(frame);
            runtime->update_texture(texture, DebugTextureWidth, DebugTextureHeight, pixels.data());
            g_debugSyntheticUploaded = true;
            g_debugLastUploadFrame = frame;
        }

        const uint32_t lastUpload = g_debugLastUploadFrame.load();
        const int32_t available = textureFound && g_debugSyntheticUploaded.load() ? 1 : 0;
        const int32_t frameAge = available && frame >= lastUpload ? static_cast<int32_t>(frame - lastUpload) : 9999;

        SetIntUniform(runtime, "Dalapad_DebugAvailable", available);
        SetIntUniform(runtime, "Dalapad_DebugWidth", available ? static_cast<int32_t>(DebugTextureWidth) : 0);
        SetIntUniform(runtime, "Dalapad_DebugHeight", available ? static_cast<int32_t>(DebugTextureHeight) : 0);
        SetIntUniform(runtime, "Dalapad_DebugFrameAge", frameAge);
        BindDebugLayerTextures(runtime);
    }

    static void OnBindRenderTargetsAndDepthStencil(
        reshade::api::command_list* cmdList,
        uint32_t count,
        const reshade::api::resource_view* rtvs,
        reshade::api::resource_view dsv)
    {
        if (cmdList == nullptr || g_debugInsideReShadeEffects.load())
            return;

        auto* device = cmdList->get_device();
        if (device == nullptr)
            return;

        if (rtvs == nullptr || count < 2)
            return;

        if (CountEligibleDebugLayerViews(device, count, rtvs) < 2)
            return;

        const uint32_t group = g_debugCaptureGroupIndex.fetch_add(1);
        if (group >= DebugMrtGroupCount)
            return;

        const uint32_t slots = count < DebugMrtSlotCount ? count : DebugMrtSlotCount;
        for (uint32_t slot = 0; slot < slots; ++slot)
            CaptureDebugLayerCandidate(device, MrtLayer(group, slot), rtvs[slot], reshade::api::resource_usage::render_target);

        if (dsv.handle != 0)
            CaptureDebugLayerCandidate(device, DebugLayerSource::Depth, dsv, reshade::api::resource_usage::depth_stencil);
    }

    static void OnReShadeBeginEffects(
        reshade::api::effect_runtime* runtime,
        reshade::api::command_list* cmdList,
        reshade::api::resource_view,
        reshade::api::resource_view)
    {
        CopyDebugLayerCandidates(cmdList, runtime);
        ResetDebugLayerCandidatesForNextFrame();
        UpdateDebugRuntime(runtime);
        g_debugInsideReShadeEffects = true;
    }

    static void OnReShadeFinishEffects(
        reshade::api::effect_runtime*,
        reshade::api::command_list*,
        reshade::api::resource_view,
        reshade::api::resource_view)
    {
        g_debugInsideReShadeEffects = false;
    }

    static void OnReShadePresent(reshade::api::effect_runtime* runtime)
    {
        UpdateDebugRuntime(runtime);
    }

    static void OnReShadeReloadedEffects(reshade::api::effect_runtime* runtime)
    {
        g_debugShaderTextureFound = false;
        g_debugSyntheticUploaded = false;
        UpdateDebugRuntime(runtime);
    }

    static void RegisterDebugEvents()
    {
        if (g_reshadeEventsRegistered.exchange(true))
            return;

        reshade::register_event<reshade::addon_event::reshade_present>(&OnReShadePresent);
        reshade::register_event<reshade::addon_event::reshade_reloaded_effects>(&OnReShadeReloadedEffects);
        reshade::register_event<reshade::addon_event::bind_render_targets_and_depth_stencil>(&OnBindRenderTargetsAndDepthStencil);
        reshade::register_event<reshade::addon_event::reshade_begin_effects>(&OnReShadeBeginEffects);
        reshade::register_event<reshade::addon_event::reshade_finish_effects>(&OnReShadeFinishEffects);
    }

    static void UnregisterDebugEvents()
    {
        if (!g_reshadeEventsRegistered.exchange(false))
            return;

        reshade::unregister_event<reshade::addon_event::reshade_present>(&OnReShadePresent);
        reshade::unregister_event<reshade::addon_event::reshade_reloaded_effects>(&OnReShadeReloadedEffects);
        reshade::unregister_event<reshade::addon_event::bind_render_targets_and_depth_stencil>(&OnBindRenderTargetsAndDepthStencil);
        reshade::unregister_event<reshade::addon_event::reshade_begin_effects>(&OnReShadeBeginEffects);
        reshade::unregister_event<reshade::addon_event::reshade_finish_effects>(&OnReShadeFinishEffects);

        for (auto& copy : g_debugLayerCopies)
            DestroyDebugLayerCopy(copy);
    }
#else
    static std::array<DebugLayerStatus, static_cast<size_t>(DebugLayerSource::Count)> BuildDebugLayerStatuses()
    {
        std::array<DebugLayerStatus, static_cast<size_t>(DebugLayerSource::Count)> statuses = {};
        const auto& names = LayerNames();
        const auto& semantics = LayerSemantics();
        const auto& availability = LayerAvailabilityUniforms();
        const auto& widths = LayerWidthUniforms();
        const auto& heights = LayerHeightUniforms();

        for (size_t index = 0; index < statuses.size(); ++index)
        {
            const auto source = static_cast<DebugLayerSource>(index);
            statuses[index] = DebugLayerStatus{
                names[index],
                semantics[index],
                availability[index],
                widths[index],
                heights[index],
                DebugLayerClassificationHint(source),
                LayerCandidateSource(source) };
        }

        return statuses;
    }

    static void RegisterDebugEvents()
    {
    }

    static void UnregisterDebugEvents()
    {
    }
#endif

    static void TryUnregisterFromReShade(HMODULE module)
    {
        const auto reshadeModule = GetReShadeModuleHandle();
        if (reshadeModule == nullptr)
            return;

        const auto unregisterAddon = reinterpret_cast<ReShadeUnregisterAddonFn>(
            GetProcAddress(reshadeModule, "ReShadeUnregisterAddon"));
        if (unregisterAddon != nullptr)
            unregisterAddon(module);
    }

    static std::filesystem::path GetDefaultStatusDirectory()
    {
        wchar_t appData[MAX_PATH] = {};
        const DWORD len = GetEnvironmentVariableW(L"APPDATA", appData, static_cast<DWORD>(std::size(appData)));
        if (len == 0 || len >= std::size(appData))
            return {};

        return std::filesystem::path(appData)
            / L"XIVLauncher"
            / L"pluginConfigs"
            / L"Dalashade"
            / L"Dalapad";
    }

    static std::filesystem::path GetStatusDirectory()
    {
        wchar_t overrideDir[MAX_PATH] = {};
        const DWORD len = GetEnvironmentVariableW(L"DALAPAD_STATUS_DIR", overrideDir, static_cast<DWORD>(std::size(overrideDir)));
        if (len > 0 && len < std::size(overrideDir))
            return std::filesystem::path(overrideDir);

        return GetDefaultStatusDirectory();
    }

    static std::filesystem::path GetStatusFilePath()
    {
        const auto dir = GetStatusDirectory();
        if (dir.empty())
            return {};

        return dir / std::filesystem::path(std::string(StatusFileName));
    }

    static std::string JsonEscape(std::string_view value)
    {
        std::string escaped;
        escaped.reserve(value.size() + 8);

        for (const char c : value)
        {
            switch (c)
            {
            case '\\':
                escaped += "\\\\";
                break;
            case '"':
                escaped += "\\\"";
                break;
            case '\b':
                escaped += "\\b";
                break;
            case '\f':
                escaped += "\\f";
                break;
            case '\n':
                escaped += "\\n";
                break;
            case '\r':
                escaped += "\\r";
                break;
            case '\t':
                escaped += "\\t";
                break;
            default:
                escaped += c;
                break;
            }
        }

        return escaped;
    }

    static std::string ExtractJsonString(std::string_view json, std::string_view key)
    {
        const std::string needle = "\"" + std::string(key) + "\"";
        const size_t keyPosition = json.find(needle);
        if (keyPosition == std::string_view::npos)
            return {};

        const size_t colonPosition = json.find(':', keyPosition + needle.size());
        if (colonPosition == std::string_view::npos)
            return {};

        const size_t firstQuote = json.find('"', colonPosition + 1);
        if (firstQuote == std::string_view::npos)
            return {};

        std::string value;
        bool escaping = false;
        for (size_t index = firstQuote + 1; index < json.size(); ++index)
        {
            const char c = json[index];
            if (escaping)
            {
                value += c;
                escaping = false;
                continue;
            }

            if (c == '\\')
            {
                escaping = true;
                continue;
            }

            if (c == '"')
                return value;

            value += c;
        }

        return {};
    }

    static std::string UtcNowIso8601()
    {
        const auto now = std::chrono::system_clock::now();
        const auto seconds = std::chrono::system_clock::to_time_t(now);

        tm utc = {};
        gmtime_s(&utc, &seconds);

        char buffer[32] = {};
        std::strftime(buffer, sizeof(buffer), "%Y-%m-%dT%H:%M:%SZ", &utc);
        return buffer;
    }

    static std::vector<ResourceStatus> BuildResourceStatuses()
    {
        const auto layers = BuildDebugLayerStatuses();
        const auto build = [](const DebugLayerStatus& layer, const std::string& name, const std::string& fallbackSource) {
            return ResourceStatus{
                name,
                layer.observed ? layer.source : fallbackSource,
                "optionalDebugTexture",
                layer.availabilityUniform,
                layer.copied,
                layer.width,
                layer.height,
                "runtime",
                layer.copied ? "copied-this-session" : (layer.observed ? "observed-not-copied" : "disabled"),
                layer.copied ? 0.35f : (layer.observed ? 0.15f : 0.0f),
                layer.copied ? "debug-copy-only" : (layer.observed ? "debug-observed-only" : "metadata-only-unavailable"),
                layer.copied ? "reshade-debug-copy" : (layer.observed ? "reshade-bind-event" : "static-contract"),
                layer.copied
                    ? "Debug bridge copied this candidate into an addon-owned texture for Dalapad_Debug.fx. This is not a production shader input."
                    : (layer.observed
                        ? "Debug bridge observed this candidate, but no addon-owned copy is available to the shader yet."
                        : "Metadata catalog names this candidate, but no live debug bridge observation is available yet.")
            };
        };

        std::vector<ResourceStatus> resources;
        resources.reserve(DebugLayerCount);
        for (size_t index = 0; index < DebugMrtSourceCount; ++index)
        {
            const auto source = static_cast<DebugLayerSource>(index);
            std::ostringstream name;
            name << "Dalapad_Group" << MrtGroup(source) << "_MRT" << MrtSlot(source);

            std::ostringstream fallback;
            fallback << "pre-ReShade MRT group " << MrtGroup(source) << " slot " << MrtSlot(source);
            resources.emplace_back(build(layers[index], name.str(), fallback.str()));
        }

        for (const auto& candidate : PinnedCandidates())
        {
            const auto& layer = layers[LayerIndex(candidate.source)];
            ResourceStatus resource = build(layer, std::string(candidate.name), LayerCandidateSource(candidate.source));
            resource.source = std::string(candidate.label) + " -> " + LayerCandidateSource(candidate.source);
            resource.availabilityFlag = candidate.availabilityUniform;
            resource.confidence = layer.copied ? candidate.confidence : (layer.observed ? 0.20f : 0.0f);
            resource.reason = layer.copied
                ? "Pinned debug alias is bound to an addon-owned copy of the current candidate. Treat this as a validation hint, not a production shader contract."
                : (layer.observed
                    ? "Pinned debug alias points at an observed candidate, but no addon-owned copy is available to the shader yet."
                    : "Pinned debug alias is configured, but its raw candidate has not been observed this session.");
            resources.emplace_back(std::move(resource));
        }

        resources.emplace_back(build(layers[LayerIndex(DebugLayerSource::Depth)], std::string(SurfaceDepthName), "RenderTargetManager.DepthStencil"));
        return resources;
    }

    static DebugVisualizationSnapshot BuildDebugVisualizationSnapshot()
    {
        DebugVisualizationSnapshot snapshot;
        snapshot.shaderTextureFound = g_debugShaderTextureFound.load();
        snapshot.syntheticTextureUploaded = g_debugSyntheticUploaded.load();
        const auto layers = BuildDebugLayerStatuses();
        for (const auto& layer : layers)
        {
            if (layer.observed)
                ++snapshot.observedSourceCount;
            if (layer.copied)
                ++snapshot.copiedSourceCount;
        }

        snapshot.readsRenderTargets = snapshot.observedSourceCount > 0;
        snapshot.copiesRenderTargets = snapshot.copiedSourceCount > 0;
        snapshot.registersGameResources = false;
        if (snapshot.copiedSourceCount > 0)
        {
            for (const auto& layer : layers)
            {
                if (layer.copied)
                {
                    snapshot.width = layer.width;
                    snapshot.height = layer.height;
                    break;
                }
            }
        }

        const uint32_t frame = g_debugFrameCounter.load();
        const uint32_t lastUpload = g_debugLastUploadFrame.load();
        snapshot.frameCounter = frame;
        snapshot.frameAge = snapshot.syntheticTextureUploaded && frame >= lastUpload ? frame - lastUpload : 9999;

        if (snapshot.copiedSourceCount > 0)
        {
            snapshot.status = "RenderLayerCopied";
            snapshot.source = "debug-layer-candidates";
            snapshot.reason = "Debug bridge copied at least one observed render-layer candidate into an addon-owned texture and bound it to Dalapad_Debug.fx by semantic. It does not expose game texture handles directly.";
        }
        else if (snapshot.observedSourceCount > 0)
        {
            snapshot.status = "RenderLayerObserved";
            snapshot.source = "debug-layer-candidates";
            snapshot.reason = "Debug bridge observed render-layer candidates from ReShade bind events, but no addon-owned copy is available to the shader yet. Check format support and effect begin callbacks.";
        }
        else if (snapshot.syntheticTextureUploaded)
        {
            snapshot.status = "SyntheticUploaded";
            snapshot.reason = "Dalapad addon uploaded a synthetic debug texture into Dalapad_DebugTexture. No render-layer candidate has been observed yet.";
        }
        else if (snapshot.shaderTextureFound)
        {
            snapshot.status = "TextureFound";
            snapshot.reason = "Dalapad_DebugTexture was found, but no synthetic upload has been confirmed yet.";
        }
        else if (g_reshadeRegistered.load())
        {
            snapshot.status = "WaitingForShader";
            snapshot.reason = "ReShade registered the addon, but Dalapad_DebugTexture has not been found. Install/reload Dalapad_Debug.fx.";
        }
        else
        {
            snapshot.status = "NoReShadeRuntime";
            snapshot.reason = "Addon is not registered with ReShade, so no debug texture can be uploaded.";
        }

        return snapshot;
    }

    static std::string BuildDebugLayerStatusJson()
    {
        const auto layers = BuildDebugLayerStatuses();
        std::ostringstream json;
        json << "[";
        for (size_t i = 0; i < layers.size(); ++i)
        {
            const auto& layer = layers[i];
            json << "{";
            json << "\"name\":\"" << JsonEscape(std::string(layer.name)) << "\",";
            json << "\"semantic\":\"" << JsonEscape(std::string(layer.semantic)) << "\",";
            json << "\"availabilityUniform\":\"" << JsonEscape(std::string(layer.availabilityUniform)) << "\",";
            json << "\"classificationHint\":\"" << JsonEscape(std::string(layer.classificationHint)) << "\",";
            json << "\"source\":\"" << JsonEscape(std::string(layer.source)) << "\",";
            json << "\"observed\":" << (layer.observed ? "true" : "false") << ",";
            json << "\"copied\":" << (layer.copied ? "true" : "false") << ",";
            json << "\"width\":" << layer.width << ",";
            json << "\"height\":" << layer.height << ",";
            json << "\"formatId\":" << layer.formatId << ",";
            json << "\"samples\":" << layer.samples << ",";
            json << "\"copyCount\":" << layer.copyCount;
            json << "}" << (i + 1 < layers.size() ? "," : "");
        }

        json << "]";
        return json.str();
    }

    static std::string BuildPinnedCandidateStatusJson()
    {
        const auto layers = BuildDebugLayerStatuses();
        std::ostringstream json;
        json << "[";
        const auto& candidates = PinnedCandidates();
        for (size_t i = 0; i < candidates.size(); ++i)
        {
            const auto& candidate = candidates[i];
            const auto& layer = layers[LayerIndex(candidate.source)];
            json << "{";
            json << "\"name\":\"" << JsonEscape(std::string(candidate.name)) << "\",";
            json << "\"label\":\"" << JsonEscape(std::string(candidate.label)) << "\",";
            json << "\"semantic\":\"" << JsonEscape(std::string(candidate.semantic)) << "\",";
            json << "\"availabilityUniform\":\"" << JsonEscape(std::string(candidate.availabilityUniform)) << "\",";
            json << "\"source\":\"" << JsonEscape(std::string(layer.name)) << "\",";
            json << "\"sourceSemantic\":\"" << JsonEscape(std::string(layer.semantic)) << "\",";
            json << "\"classificationHint\":\"" << JsonEscape(std::string(candidate.classificationHint)) << "\",";
            json << "\"observed\":" << (layer.observed ? "true" : "false") << ",";
            json << "\"copied\":" << (layer.copied ? "true" : "false") << ",";
            json << "\"width\":" << layer.width << ",";
            json << "\"height\":" << layer.height << ",";
            json << "\"confidence\":" << candidate.confidence;
            json << "}" << (i + 1 < candidates.size() ? "," : "");
        }

        json << "]";
        return json.str();
    }

    static std::string BuildDebugVisualizationJson()
    {
        const auto debug = BuildDebugVisualizationSnapshot();
        std::ostringstream json;
        json << "{";
        json << "\"version\":\"" << DebugVisualizationVersion << "\",";
        json << "\"enabled\":" << (debug.enabled ? "true" : "false") << ",";
        json << "\"status\":\"" << JsonEscape(debug.status) << "\",";
        json << "\"source\":\"" << JsonEscape(debug.source) << "\",";
        json << "\"shader\":\"" << DebugEffectName << "\",";
        json << "\"textureName\":\"" << DebugTextureName << "\",";
        json << "\"shaderTextureFound\":" << (debug.shaderTextureFound ? "true" : "false") << ",";
        json << "\"syntheticTextureUploaded\":" << (debug.syntheticTextureUploaded ? "true" : "false") << ",";
        json << "\"usesSyntheticTexture\":" << (debug.usesSyntheticTexture ? "true" : "false") << ",";
        json << "\"width\":" << debug.width << ",";
        json << "\"height\":" << debug.height << ",";
        json << "\"frameCounter\":" << debug.frameCounter << ",";
        json << "\"frameAge\":" << debug.frameAge << ",";
        json << "\"copyFrameInterval\":" << DebugLayerCopyFrameInterval << ",";
        json << "\"observedSourceCount\":" << debug.observedSourceCount << ",";
        json << "\"copiedSourceCount\":" << debug.copiedSourceCount << ",";
        json << "\"readsRenderTargets\":" << (debug.readsRenderTargets ? "true" : "false") << ",";
        json << "\"copiesRenderTargets\":" << (debug.copiesRenderTargets ? "true" : "false") << ",";
        json << "\"registersGameResources\":" << (debug.registersGameResources ? "true" : "false") << ",";
        json << "\"sources\":" << BuildDebugLayerStatusJson() << ",";
        json << "\"pinnedCandidates\":" << BuildPinnedCandidateStatusJson() << ",";
        json << "\"reason\":\"" << JsonEscape(debug.reason) << "\"";
        json << "}";
        return json.str();
    }

    static BridgeStatus BuildStatus(std::string status, std::string summary, bool reshadeRegistered, bool namedPipeOpen)
    {
        BridgeStatus bridge;
        bridge.status = std::move(status);
        bridge.summary = std::move(summary);
        bridge.reshadeRegistered = reshadeRegistered;
        bridge.namedPipeOpen = namedPipeOpen;
        bridge.resources = BuildResourceStatuses();

        if (!bridge.reshadeHeaderCompiled)
            bridge.warnings.emplace_back("Built without reshade.hpp; this DLL can only test status-file IPC, not ReShade addon registration.");

        bridge.warnings.emplace_back("Render-target copies are limited to Dalapad_Debug.fx diagnostic visualization; production shader data flow remains disabled.");
        bridge.warnings.emplace_back("Diagnostic control pipe can answer ping, self-test, status, and capability queries only.");
        bridge.warnings.emplace_back("Resource catalog reports debug observations and addon-owned copies only; it does not expose game texture handles.");
        bridge.warnings.emplace_back("Debug visualization keeps the synthetic ReShade texture as source 0 and adds semantic-bound render-layer candidates when copied.");
        bridge.warnings.emplace_back("Realtime uniform movement is reserved but disabled until render-layer validation succeeds.");
        return bridge;
    }

    static std::string BuildStatusJson(const BridgeStatus& bridge)
    {
        std::ostringstream json;
        json << "{\n";
        json << "  \"ipcContractVersion\": \"" << IpcContractVersion << "\",\n";
        json << "  \"contractVersion\": \"" << ContractVersion << "\",\n";
        json << "  \"resourceCatalogVersion\": \"" << ResourceCatalogVersion << "\",\n";
        json << "  \"bridgeVersion\": \"" << BridgeVersion << "\",\n";
        json << "  \"addonProcess\": \"DalapadAddon\",\n";
        json << "  \"status\": \"" << JsonEscape(bridge.status) << "\",\n";
        json << "  \"summary\": \"" << JsonEscape(bridge.summary) << "\",\n";
        json << "  \"lastUpdateUtc\": \"" << UtcNowIso8601() << "\",\n";
        json << "  \"reshade\": {\n";
        json << "    \"headerCompiled\": " << (bridge.reshadeHeaderCompiled ? "true" : "false") << ",\n";
        json << "    \"requestedApiVersion\": " << ReShadeApiVersion << ",\n";
        json << "    \"registered\": " << (bridge.reshadeRegistered ? "true" : "false") << "\n";
        json << "  },\n";
        json << "  \"resources\": [\n";

        for (size_t i = 0; i < bridge.resources.size(); ++i)
        {
            const auto& resource = bridge.resources[i];
            json << "    {\n";
            json << "      \"name\": \"" << resource.name << "\",\n";
            json << "      \"kind\": \"" << resource.kind << "\",\n";
            json << "      \"availabilityFlag\": \"" << resource.availabilityFlag << "\",\n";
            json << "      \"available\": " << (resource.available ? "true" : "false") << ",\n";
            json << "      \"source\": \"" << resource.source << "\",\n";
            json << "      \"width\": " << resource.width << ",\n";
            json << "      \"height\": " << resource.height << ",\n";
            json << "      \"format\": \"" << resource.format << "\",\n";
            json << "      \"freshness\": \"" << resource.freshness << "\",\n";
            json << "      \"confidence\": " << resource.confidence << ",\n";
            json << "      \"safetyState\": \"" << resource.safetyState << "\",\n";
            json << "      \"metadataSource\": \"" << resource.metadataSource << "\",\n";
            json << "      \"reason\": \"" << JsonEscape(resource.reason) << "\"\n";
            json << "    }" << (i + 1 < bridge.resources.size() ? "," : "") << "\n";
        }

        json << "  ],\n";
        json << "  \"debugVisualization\": " << BuildDebugVisualizationJson() << ",\n";
        json << "  \"ipc\": {\n";
        json << "    \"statusFileName\": \"" << StatusFileName << "\",\n";
        json << "    \"controlPipeName\": \"" << JsonEscape(ControlPipeName) << "\",\n";
        json << "    \"namedPipeOpen\": " << (bridge.namedPipeOpen ? "true" : "false") << "\n";
        json << "  },\n";
        json << "  \"realtime\": {\n";
        json << "    \"enabled\": " << (bridge.realtimeEnabled ? "true" : "false") << ",\n";
        json << "    \"channel\": \"" << RealtimeUniformsChannel << "\",\n";
        json << "    \"reason\": \"render-layer bridge validation has priority\"\n";
        json << "  },\n";
        json << "  \"safety\": {\n";
        json << "    \"readsRenderTargets\": false,\n";
        json << "    \"copiesRenderTargets\": false,\n";
        json << "    \"registersShaderResources\": false,\n";
        json << "    \"movesRealtimeShaderValues\": false,\n";
        json << "    \"changesGeneratedPresets\": false\n";
        json << "  },\n";
        json << "  \"capabilities\": {\n";
        json << "    \"supportsStatusFile\": true,\n";
        json << "    \"supportsControlPipe\": true,\n";
        json << "    \"supportsRealtimeUniforms\": false,\n";
        json << "    \"supportsResourceCatalog\": true,\n";
        json << "    \"supportsDebugVisualization\": true,\n";
        json << "    \"readsRenderTargets\": false,\n";
        json << "    \"copiesRenderTargets\": false,\n";
        json << "    \"registersShaderResources\": false,\n";
        json << "    \"movesRealtimeShaderValues\": false\n";
        json << "  },\n";
        json << "  \"warnings\": [\n";

        for (size_t i = 0; i < bridge.warnings.size(); ++i)
            json << "    \"" << JsonEscape(bridge.warnings[i]) << "\"" << (i + 1 < bridge.warnings.size() ? "," : "") << "\n";

        json << "  ]\n";
        json << "}\n";
        return json.str();
    }

    static std::string BuildResourceStatusJson()
    {
        const auto resources = BuildResourceStatuses();
        std::ostringstream json;
        json << "[";
        for (size_t i = 0; i < resources.size(); ++i)
        {
            const auto& resource = resources[i];
            json << "{";
            json << "\"name\":\"" << resource.name << "\",";
            json << "\"kind\":\"" << resource.kind << "\",";
            json << "\"availabilityFlag\":\"" << resource.availabilityFlag << "\",";
            json << "\"available\":" << (resource.available ? "true" : "false") << ",";
            json << "\"source\":\"" << resource.source << "\",";
            json << "\"width\":" << resource.width << ",";
            json << "\"height\":" << resource.height << ",";
            json << "\"format\":\"" << resource.format << "\",";
            json << "\"freshness\":\"" << resource.freshness << "\",";
            json << "\"confidence\":" << resource.confidence << ",";
            json << "\"safetyState\":\"" << resource.safetyState << "\",";
            json << "\"metadataSource\":\"" << resource.metadataSource << "\",";
            json << "\"reason\":\"" << JsonEscape(resource.reason) << "\"";
            json << "}" << (i + 1 < resources.size() ? "," : "");
        }

        json << "]";
        return json.str();
    }

    static std::string BuildControlResponse(std::string_view request)
    {
        const auto id = ExtractJsonString(request, "id");
        const auto type = ExtractJsonString(request, "type");
        const bool knownType =
            type == "Ping" ||
            type == "BridgeSelfTest" ||
            type == "QueryStatus" ||
            type == "QueryCapabilities" ||
            type == "QueryDebugVisualization" ||
            type == "SetDebugVisualization";
        const bool includeResources = knownType;

        std::ostringstream json;
        json << "{";
        json << "\"contract\":\"" << ControlPipeContract << "\",";
        json << "\"id\":\"" << JsonEscape(id) << "\",";
        json << "\"type\":\"" << JsonEscape(type.empty() ? "Unknown" : type) << "\",";
        json << "\"ok\":" << (knownType ? "true" : "false") << ",";
        json << "\"status\":\"" << (knownType ? "Listening" : "UnknownRequest") << "\",";
        json << "\"summary\":\"" << (knownType
            ? "Dalapad diagnostic control pipe is listening and can report diagnostic resource rows plus debug visualization status. Raw handle IPC, production shader-resource dependency, and realtime uniform movement remain disabled."
            : "Dalapad diagnostic control pipe received an unknown request type.") << "\",";
        json << "\"bridgeVersion\":\"" << BridgeVersion << "\",";
        json << "\"resourceCatalogVersion\":\"" << ResourceCatalogVersion << "\",";
        json << "\"addonProcess\":\"DalapadAddon\",";
        json << "\"lastUpdateUtc\":\"" << UtcNowIso8601() << "\",";
        json << "\"reshadeRegistered\":" << (g_reshadeRegistered.load() ? "true" : "false") << ",";
        json << "\"requestedApiVersion\":" << ReShadeApiVersion << ",";
        json << "\"capabilities\":{";
        json << "\"supportsStatusFile\":true,";
        json << "\"supportsControlPipe\":true,";
        json << "\"supportsRealtimeUniforms\":false,";
        json << "\"supportsResourceCatalog\":true,";
        json << "\"supportsDebugVisualization\":true,";
        json << "\"readsRenderTargets\":false,";
        json << "\"copiesRenderTargets\":false,";
        json << "\"registersShaderResources\":false,";
        json << "\"movesRealtimeShaderValues\":false";
        json << "},";
        json << "\"safety\":{";
        json << "\"readsRenderTargets\":false,";
        json << "\"copiesRenderTargets\":false,";
        json << "\"registersShaderResources\":false,";
        json << "\"movesRealtimeShaderValues\":false,";
        json << "\"changesGeneratedPresets\":false";
        json << "},";
        json << "\"debugVisualization\":" << BuildDebugVisualizationJson() << ",";
        json << "\"resources\":" << (includeResources ? BuildResourceStatusJson() : "[]") << ",";
        json << "\"warnings\":[";
        json << "\"Control pipe is diagnostic-only and cannot change generated presets or shader values.\",";
        json << "\"Resource catalog contains debug observations and addon-owned copy status only; it does not contain texture handles.\",";
        json << "\"Debug visualization can bind semantic render-layer candidates only after an addon-owned copy succeeds.\",";
        json << "\"Realtime uniform movement is disabled in this build.\"";
        if (!knownType)
            json << ",\"Unknown request type.\"";
        json << "]";
        json << "}\n";
        return json.str();
    }

    static bool WriteStatusFile(const BridgeStatus& status)
    {
        const auto path = GetStatusFilePath();
        if (path.empty())
            return false;

        std::error_code ec;
        std::filesystem::create_directories(path.parent_path(), ec);
        if (ec)
            return false;

        std::ofstream file(path, std::ios::out | std::ios::trunc);
        if (!file)
            return false;

        file << BuildStatusJson(status);
        return file.good();
    }

    static DWORD WINAPI ControlPipeThreadProc(LPVOID)
    {
        while (WaitForSingleObject(g_stopEvent, 0) == WAIT_TIMEOUT)
        {
            HANDLE pipe = CreateNamedPipeA(
                R"(\\.\pipe\Dalapad.Control.v1)",
                PIPE_ACCESS_DUPLEX,
                PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_NOWAIT,
                1,
                8192,
                8192,
                0,
                nullptr);

            if (pipe == INVALID_HANDLE_VALUE)
            {
                g_pipeListening = false;
                WaitForSingleObject(g_stopEvent, 250);
                continue;
            }

            g_pipeListening = true;
            bool connected = false;
            while (WaitForSingleObject(g_stopEvent, 0) == WAIT_TIMEOUT)
            {
                if (ConnectNamedPipe(pipe, nullptr))
                {
                    connected = true;
                    break;
                }

                const DWORD error = GetLastError();
                if (error == ERROR_PIPE_CONNECTED)
                {
                    connected = true;
                    break;
                }

                if (error != ERROR_PIPE_LISTENING)
                    break;

                WaitForSingleObject(g_stopEvent, 50);
            }

            if (connected)
            {
                char buffer[4096] = {};
                DWORD bytesRead = 0;
                std::string request;
                const auto start = GetTickCount64();
                while (WaitForSingleObject(g_stopEvent, 0) == WAIT_TIMEOUT)
                {
                    if (ReadFile(pipe, buffer, static_cast<DWORD>(sizeof(buffer) - 1), &bytesRead, nullptr) && bytesRead > 0)
                    {
                        buffer[bytesRead] = '\0';
                        request.assign(buffer, bytesRead);
                        break;
                    }

                    const DWORD error = GetLastError();
                    if (error != ERROR_NO_DATA && error != ERROR_MORE_DATA)
                        break;

                    if (GetTickCount64() - start > 500)
                        break;

                    WaitForSingleObject(g_stopEvent, 10);
                }

                const auto response = BuildControlResponse(request);
                DWORD bytesWritten = 0;
                WriteFile(pipe, response.data(), static_cast<DWORD>(response.size()), &bytesWritten, nullptr);
                FlushFileBuffers(pipe);
            }

            DisconnectNamedPipe(pipe);
            CloseHandle(pipe);
        }

        g_pipeListening = false;
        return 0;
    }

    static bool StartControlPipe()
    {
        if (g_pipeThread != nullptr)
            return true;

        g_stopEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);
        if (g_stopEvent == nullptr)
            return false;

        g_pipeThread = CreateThread(nullptr, 0, ControlPipeThreadProc, nullptr, 0, nullptr);
        if (g_pipeThread == nullptr)
        {
            CloseHandle(g_stopEvent);
            g_stopEvent = nullptr;
            return false;
        }

        return true;
    }

    static void StopControlPipe()
    {
        if (g_stopEvent != nullptr)
            SetEvent(g_stopEvent);

        if (g_pipeThread != nullptr)
        {
            WaitForSingleObject(g_pipeThread, 750);
            CloseHandle(g_pipeThread);
            g_pipeThread = nullptr;
        }

        if (g_stopEvent != nullptr)
        {
            CloseHandle(g_stopEvent);
            g_stopEvent = nullptr;
        }

        g_pipeListening = false;
    }

    static void WriteLoadedStatus(bool reshadeRegistered, bool pipeStarted)
    {
        const auto status = BuildStatus(
            reshadeRegistered ? "Loaded" : "SelfTest",
            reshadeRegistered
                ? "Dalapad addon loaded and registered with ReShade. Debug visualization can copy observed render-layer candidates into addon-owned textures; production shader data flow remains disabled."
                : "Dalapad DLL loaded and wrote status-file IPC. Resource catalog status is available; ReShade registration was not available in this build.",
            reshadeRegistered,
            pipeStarted);

        WriteStatusFile(status);
    }

    static void WriteStoppedStatus()
    {
        auto status = BuildStatus(
            "Stopped",
            "Dalapad addon unloaded. Debug render-layer copies, if any, were addon-owned diagnostic textures and production shader data flow remained disabled.",
            false,
            false);
        status.warnings.emplace_back("Stopped status may be stale if the host terminated without clean unload.");

        WriteStatusFile(status);
    }
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID)
{
    switch (reason)
    {
    case DLL_PROCESS_ATTACH:
    {
        DisableThreadLibraryCalls(module);

        bool reshadeRegistered = false;
        reshadeRegistered = dalapad::TryRegisterWithReShade(module);
        dalapad::g_reshadeRegistered = reshadeRegistered;
        if (reshadeRegistered)
            dalapad::RegisterDebugEvents();
        const bool pipeStarted = dalapad::StartControlPipe();
        dalapad::WriteLoadedStatus(reshadeRegistered, pipeStarted);
        return TRUE;
    }

    case DLL_PROCESS_DETACH:
        dalapad::UnregisterDebugEvents();
        dalapad::StopControlPipe();
        dalapad::TryUnregisterFromReShade(module);
        dalapad::g_reshadeRegistered = false;
        dalapad::WriteStoppedStatus();
        return TRUE;

    default:
        return TRUE;
    }
}
