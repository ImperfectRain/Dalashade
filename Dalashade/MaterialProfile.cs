using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record MaterialProfile(
    string Family,
    float Foliage,
    float WaterSpecular,
    float SandDust,
    float SnowIce,
    float StoneRuins,
    float MetalIndustrial,
    float CrystalAether,
    float NeonGlass,
    float FireLavaHeat,
    float SkyCloudFog,
    float SkinProtection,
    float VoidDarkness,
    IReadOnlyList<string> ProfileTags,
    IReadOnlyList<MaterialProfileContribution> Contributions)
{
    public static MaterialProfile Neutral { get; } = new(
        "neutral",
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        Array.Empty<string>(),
        Array.Empty<MaterialProfileContribution>());

    public float ValueFor(string channel) => channel switch
    {
        MaterialIntent.FoliageChannel => Foliage,
        MaterialIntent.WaterSpecularChannel => WaterSpecular,
        MaterialIntent.SandDustChannel => SandDust,
        MaterialIntent.SnowIceChannel => SnowIce,
        MaterialIntent.StoneRuinsChannel => StoneRuins,
        MaterialIntent.MetalIndustrialChannel => MetalIndustrial,
        MaterialIntent.CrystalAetherChannel => CrystalAether,
        MaterialIntent.NeonGlassChannel => NeonGlass,
        MaterialIntent.FireLavaHeatChannel => FireLavaHeat,
        MaterialIntent.SkyCloudFogChannel => SkyCloudFog,
        MaterialIntent.SkinProtectionChannel => SkinProtection,
        MaterialIntent.VoidDarknessChannel => VoidDarkness,
        _ => 0f
    };

    public IReadOnlyList<(string Channel, float Value)> TopPriors(int count) => MaterialIntent.ChannelNames
        .Select(channel => (Channel: channel, Value: ValueFor(channel)))
        .Where(item => item.Value > 0.001f)
        .OrderByDescending(item => item.Value)
        .Take(count)
        .ToArray();
}
