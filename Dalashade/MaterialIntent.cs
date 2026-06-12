using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record MaterialIntent(
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
    IReadOnlyList<MaterialIntentContribution> Contributions)
{
    public const string FoliageChannel = "Foliage";
    public const string WaterSpecularChannel = "WaterSpecular";
    public const string SandDustChannel = "SandDust";
    public const string SnowIceChannel = "SnowIce";
    public const string StoneRuinsChannel = "StoneRuins";
    public const string MetalIndustrialChannel = "MetalIndustrial";
    public const string CrystalAetherChannel = "CrystalAether";
    public const string NeonGlassChannel = "NeonGlass";
    public const string FireLavaHeatChannel = "FireLavaHeat";
    public const string SkyCloudFogChannel = "SkyCloudFog";
    public const string SkinProtectionChannel = "SkinProtection";
    public const string VoidDarknessChannel = "VoidDarkness";

    public static IReadOnlyList<string> ChannelNames { get; } =
    [
        FoliageChannel,
        WaterSpecularChannel,
        SandDustChannel,
        SnowIceChannel,
        StoneRuinsChannel,
        MetalIndustrialChannel,
        CrystalAetherChannel,
        NeonGlassChannel,
        FireLavaHeatChannel,
        SkyCloudFogChannel,
        SkinProtectionChannel,
        VoidDarknessChannel
    ];

    public static MaterialIntent Neutral { get; } = new(
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
        Array.Empty<MaterialIntentContribution>());

    public float ValueFor(string channel) => channel switch
    {
        FoliageChannel => Foliage,
        WaterSpecularChannel => WaterSpecular,
        SandDustChannel => SandDust,
        SnowIceChannel => SnowIce,
        StoneRuinsChannel => StoneRuins,
        MetalIndustrialChannel => MetalIndustrial,
        CrystalAetherChannel => CrystalAether,
        NeonGlassChannel => NeonGlass,
        FireLavaHeatChannel => FireLavaHeat,
        SkyCloudFogChannel => SkyCloudFog,
        SkinProtectionChannel => SkinProtection,
        VoidDarknessChannel => VoidDarkness,
        _ => 0f
    };

    public MaterialIntent WithStrength(float strength)
    {
        var multiplier = MathF.Min(1f, MathF.Max(0f, strength));
        if (multiplier <= 0f)
        {
            return Neutral;
        }

        return new MaterialIntent(
            Foliage * multiplier,
            WaterSpecular * multiplier,
            SandDust * multiplier,
            SnowIce * multiplier,
            StoneRuins * multiplier,
            MetalIndustrial * multiplier,
            CrystalAether * multiplier,
            NeonGlass * multiplier,
            FireLavaHeat * multiplier,
            SkyCloudFog * multiplier,
            SkinProtection * multiplier,
            VoidDarkness * multiplier,
            Contributions.Select(contribution => contribution with { Amount = contribution.Amount * multiplier }).ToArray());
    }
}
