// Dalapad ReShade/native addon first-test implementation.
//
// This file is intentionally not included in Dalashade.sln. Build it only in a
// separate experimental DLL project. Stage 1 proves that an addon can load and
// report status to Dalashade through a small JSON file. It does not read,
// copy, register, or expose render targets yet.

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>

#include <chrono>
#include <cstdio>
#include <filesystem>
#include <fstream>
#include <iterator>
#include <sstream>
#include <string>
#include <string_view>
#include <vector>

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
    "Dalashade experimental status-file IPC bridge. Stage 1 reports addon load state only.";

namespace dalapad
{
    constexpr std::string_view ContractVersion = "0.1-diagnostic";
    constexpr std::string_view IpcContractVersion = "0.1-ipc-diagnostic";
    constexpr std::string_view BridgeVersion = "0.1.0-stage1-status";
    constexpr std::string_view StatusFileName = "dalapad-status.json";
    constexpr std::string_view ControlPipeName = R"(\\.\pipe\Dalapad.Control.v1)";
    constexpr std::string_view RealtimeUniformsChannel = "Dalapad.RealtimeUniforms.v1";
    constexpr std::string_view SurfaceNormalName = "Dalapad_SurfaceNormal";
    constexpr std::string_view SurfaceDiffuseName = "Dalapad_SurfaceDiffuse";
    constexpr std::string_view SurfaceDepthName = "Dalapad_SurfaceDepth";

    struct ResourceStatus
    {
        std::string_view name;
        std::string_view source;
        bool available = false;
        unsigned int width = 0;
        unsigned int height = 0;
        std::string_view format = "unknown";
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
        return {
            ResourceStatus{
                SurfaceNormalName,
                "RenderTargetManager.GBuffers[0]",
                false,
                0,
                0,
                "unknown",
                "Stage 1 does not read, copy, or register the normal render target." },
            ResourceStatus{
                SurfaceDiffuseName,
                "RenderTargetManager.GBuffers[2]",
                false,
                0,
                0,
                "unknown",
                "Stage 1 does not read, copy, or register the diffuse render target." },
            ResourceStatus{
                SurfaceDepthName,
                "RenderTargetManager.DepthStencil",
                false,
                0,
                0,
                "unknown",
                "Stage 1 does not read, copy, or register the depth/stencil render target." },
        };
    }

    static BridgeStatus BuildStatus(std::string status, std::string summary, bool reshadeRegistered)
    {
        BridgeStatus bridge;
        bridge.status = std::move(status);
        bridge.summary = std::move(summary);
        bridge.reshadeRegistered = reshadeRegistered;
        bridge.resources = BuildResourceStatuses();

        if (!bridge.reshadeHeaderCompiled)
            bridge.warnings.emplace_back("Built without reshade.hpp; this DLL can only test status-file IPC, not ReShade addon registration.");

        bridge.warnings.emplace_back("Render-target bridge is disabled in Stage 1; resources intentionally report unavailable.");
        bridge.warnings.emplace_back("Realtime uniform movement is reserved but disabled until render-layer validation succeeds.");
        return bridge;
    }

    static std::string BuildStatusJson(const BridgeStatus& bridge)
    {
        std::ostringstream json;
        json << "{\n";
        json << "  \"ipcContractVersion\": \"" << IpcContractVersion << "\",\n";
        json << "  \"contractVersion\": \"" << ContractVersion << "\",\n";
        json << "  \"bridgeVersion\": \"" << BridgeVersion << "\",\n";
        json << "  \"addonProcess\": \"DalapadAddon\",\n";
        json << "  \"status\": \"" << JsonEscape(bridge.status) << "\",\n";
        json << "  \"summary\": \"" << JsonEscape(bridge.summary) << "\",\n";
        json << "  \"lastUpdateUtc\": \"" << UtcNowIso8601() << "\",\n";
        json << "  \"reshade\": {\n";
        json << "    \"headerCompiled\": " << (bridge.reshadeHeaderCompiled ? "true" : "false") << ",\n";
        json << "    \"registered\": " << (bridge.reshadeRegistered ? "true" : "false") << "\n";
        json << "  },\n";
        json << "  \"resources\": [\n";

        for (size_t i = 0; i < bridge.resources.size(); ++i)
        {
            const auto& resource = bridge.resources[i];
            json << "    {\n";
            json << "      \"name\": \"" << resource.name << "\",\n";
            json << "      \"available\": " << (resource.available ? "true" : "false") << ",\n";
            json << "      \"source\": \"" << resource.source << "\",\n";
            json << "      \"width\": " << resource.width << ",\n";
            json << "      \"height\": " << resource.height << ",\n";
            json << "      \"format\": \"" << resource.format << "\",\n";
            json << "      \"reason\": \"" << JsonEscape(resource.reason) << "\"\n";
            json << "    }" << (i + 1 < bridge.resources.size() ? "," : "") << "\n";
        }

        json << "  ],\n";
        json << "  \"ipc\": {\n";
        json << "    \"statusFileName\": \"" << StatusFileName << "\",\n";
        json << "    \"controlPipeName\": \"" << ControlPipeName << "\",\n";
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
        json << "  \"warnings\": [\n";

        for (size_t i = 0; i < bridge.warnings.size(); ++i)
            json << "    \"" << JsonEscape(bridge.warnings[i]) << "\"" << (i + 1 < bridge.warnings.size() ? "," : "") << "\n";

        json << "  ]\n";
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

    static void WriteLoadedStatus(bool reshadeRegistered)
    {
        const auto status = BuildStatus(
            reshadeRegistered ? "Loaded" : "SelfTest",
            reshadeRegistered
                ? "Dalapad addon loaded and registered with ReShade. Render-target resources are intentionally unavailable in Stage 1."
                : "Dalapad DLL loaded and wrote status-file IPC. ReShade registration was not available in this build.",
            reshadeRegistered);

        WriteStatusFile(status);
    }

    static void WriteStoppedStatus()
    {
        auto status = BuildStatus(
            "Stopped",
            "Dalapad addon unloaded. Render-target resources were never exposed by this Stage 1 build.",
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
#if DALAPAD_HAS_RESHADE
        reshadeRegistered = reshade::register_addon(module);
#endif
        dalapad::WriteLoadedStatus(reshadeRegistered);
        return TRUE;
    }

    case DLL_PROCESS_DETACH:
#if DALAPAD_HAS_RESHADE
        reshade::unregister_addon(module);
#endif
        dalapad::WriteStoppedStatus();
        return TRUE;

    default:
        return TRUE;
    }
}
