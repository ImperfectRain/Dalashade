using Dalamud.Configuration;
using System;

namespace Dalashade;

public enum TargetStyle
{
    Gameplay,
    Balanced,
    Cinematic
}

public enum PerformanceBudget
{
    Low,
    Medium,
    High,
    Ultra
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public string BasePresetPath { get; set; } = string.Empty;
    public string GeneratedPresetPath { get; set; } = string.Empty;
    public string ReloadHotkeyName { get; set; } = "Use your configured ReShade reload hotkey";

    public TargetStyle Style { get; set; } = TargetStyle.Balanced;
    public PerformanceBudget PerformanceBudget { get; set; } = PerformanceBudget.Medium;

    public bool Enabled { get; set; } = true;
    public bool AutoAdjustInCombat { get; set; } = true;
    public bool AutoAdjustAtNight { get; set; } = true;
    public bool AutoAdjustForWeather { get; set; } = true;
    public bool AutoAdjustForTerritory { get; set; } = true;
    public bool AutoAdjustFromScreenshots { get; set; } = false;
    public bool AutoAdjustInCutscenes { get; set; } = true;
    public bool UsePremiumImmerseEffects { get; set; } = false;
    public bool WriteBackups { get; set; } = true;
    public int MinimumSecondsBetweenWrites { get; set; } = 10;
    public int MinimumSecondsBetweenImageSamples { get; set; } = 10;
    public string ScreenshotFolderPath { get; set; } = string.Empty;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
