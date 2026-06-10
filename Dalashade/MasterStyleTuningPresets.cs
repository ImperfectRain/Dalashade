namespace Dalashade;

public sealed record MasterStyleTuningPresetValues(
    float TonalMatchStrength,
    float TonalColorStrength,
    float ColorFamilyStrength,
    float MaxHueShift,
    float MaxSaturationShift,
    float MaxLuminanceShift);

public static class MasterStyleTuningPresets
{
    public static MasterStyleTuningPresetValues GetValues(MasterStyleTuningPreset preset, Configuration configuration)
    {
        return preset switch
        {
            MasterStyleTuningPreset.Subtle => new(0.60f, 0.35f, 0.25f, 0.04f, 0.08f, 0.06f),
            MasterStyleTuningPreset.Strong => new(1.30f, 1.00f, 0.90f, 0.12f, 0.22f, 0.18f),
            MasterStyleTuningPreset.Cinematic => new(1.40f, 1.15f, 1.00f, 0.14f, 0.26f, 0.20f),
            MasterStyleTuningPreset.AggressiveGpose => new(1.70f, 1.50f, 1.35f, 0.18f, 0.32f, 0.28f),
            MasterStyleTuningPreset.Balanced => new(1.00f, 0.75f, 0.65f, 0.08f, 0.15f, 0.12f),
            _ => new(
                configuration.MasterTonalMatchStrength,
                configuration.MasterTonalColorStrength,
                configuration.MasterColorFamilyStrength,
                configuration.MasterMaxHueShift,
                configuration.MasterMaxSaturationShift,
                configuration.MasterMaxLuminanceShift)
        };
    }

    public static void Apply(Configuration configuration, MasterStyleTuningPreset preset)
    {
        if (preset == MasterStyleTuningPreset.Custom)
        {
            return;
        }

        var values = GetValues(preset, configuration);
        configuration.MasterTonalMatchStrength = values.TonalMatchStrength;
        configuration.MasterTonalColorStrength = values.TonalColorStrength;
        configuration.MasterColorFamilyStrength = values.ColorFamilyStrength;
        configuration.MasterMaxHueShift = values.MaxHueShift;
        configuration.MasterMaxSaturationShift = values.MaxSaturationShift;
        configuration.MasterMaxLuminanceShift = values.MaxLuminanceShift;
    }
}
