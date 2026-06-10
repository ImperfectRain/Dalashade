using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Dalashade;

public enum SanitizeActionType
{
    SetToZero,
    Scale
}

public sealed record SanitizeAction(
    string Section,
    string Key,
    SanitizeActionType Type,
    float Amount,
    string Reason,
    EffectRole Role,
    bool RequiresSecondaryAuthority = false);

public sealed record SanitizedShaderVariable(
    string Section,
    string Key,
    string OldValue,
    string NewValue,
    SanitizeActionType ActionType,
    string Reason,
    EffectRole Role,
    bool TechniqueActive);

public sealed class SanitizeActionPipeline
{
    private static readonly IReadOnlyList<SanitizeAction> BaseActions =
    [
        new("LensDiffusion.fx", "TINT_AMOUNT", SanitizeActionType.SetToZero, 0f, "Lens diffusion tint amount", EffectRole.Diffusion),

        new("qUINT_dof.fx", "fADOF_BokehIntensity", SanitizeActionType.Scale, 0.25f, "DOF bokeh intensity", EffectRole.Dof),
        new("qUINT_dof.fx", "fADOF_ShapeChromaAmount", SanitizeActionType.SetToZero, 0f, "DOF chroma amount", EffectRole.Dof),
        new("qUINT_dof.fx", "fADOF_ShapeCurvatureAmount", SanitizeActionType.Scale, 0.50f, "DOF blur shape curvature", EffectRole.Dof),
        new("qUINT_dof.fx", "fADOF_SmootheningAmount", SanitizeActionType.Scale, 0.50f, "DOF smoothening amount", EffectRole.Dof),

        new("FilmGrain.fx", "Intensity", SanitizeActionType.SetToZero, 0f, "Film grain intensity", EffectRole.FilmGrain),
        new("FilmGrain2.fx", "grainamount", SanitizeActionType.SetToZero, 0f, "Film grain amount", EffectRole.FilmGrain),
        new("FilmGrain2.fx", "coloramount", SanitizeActionType.SetToZero, 0f, "Film grain color amount", EffectRole.FilmGrain),
        new("FilmGrain2.fx", "lumamount", SanitizeActionType.SetToZero, 0f, "Film grain luma amount", EffectRole.FilmGrain),
        new("SimpleGrain.fx", "Intensity", SanitizeActionType.SetToZero, 0f, "Simple grain intensity", EffectRole.FilmGrain),
        new("SmartNoise.fx", "noise", SanitizeActionType.SetToZero, 0f, "Smart noise amount", EffectRole.FilmGrain),

        new("Prism.fx", "AchromatAmount", SanitizeActionType.SetToZero, 0f, "Prism chromatic amount", EffectRole.Diffusion),
        new("ChromaticAberration.fx", "Strength", SanitizeActionType.SetToZero, 0f, "Chromatic aberration strength", EffectRole.Diffusion),
        new("CA.fx", "Strength", SanitizeActionType.SetToZero, 0f, "Chromatic aberration strength", EffectRole.Diffusion),
        new("FlexibleCA.fx", "cLayer_CAb_Strength", SanitizeActionType.SetToZero, 0f, "Flexible chromatic aberration strength", EffectRole.Diffusion)
    ];

    private static readonly IReadOnlyDictionary<string, string[]> SecondaryBloomKeys = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["MagicBloom.fx"] = ["fBloom_Intensity", "fDirt_Intensity"],
        ["GaussianBloom.fx"] = ["GaussianBloomStrength"],
        ["qUINT_bloom.fx"] = ["BLOOM_INTENSITY", "BLOOM_ADAPT_STRENGTH"],
        ["BloomingHDR.fx"] = ["HDR_Adjust", "BloomSensitivity"],
        ["Pirate_Bloom.fx"] = ["BLOOM_STRENGTH"],
        ["MartysMods_FFTBLOOM.fx"] = ["HDR_BLOOM_INT"]
    };

    private static readonly IReadOnlyDictionary<string, string[]> SecondarySharpenKeys = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["MartysMods_SHARPEN.fx"] = ["SHARP_AMT"],
        ["FilmicSharpen.fx"] = ["Strength"],
        ["FineSharp.fx"] = ["sstr", "cstr", "lstr", "pstr"],
        ["LumaSharpen.fx"] = ["sharp_strength"],
        ["CAS.fx"] = ["Sharpening"],
        ["HighPassSharpen.fx"] = ["HighPassSharpStrength", "HighPassLightIntensity", "HighPassDarkIntensity"],
        ["AdaptiveSharpen.fx"] = ["curve_height", "scale_lim", "scale_cs"]
    };

    public IReadOnlyList<SanitizedShaderVariable> Apply(
        string[] lines,
        Configuration configuration,
        IReadOnlySet<string> activeTechniques,
        GenerationAuthorityPolicy authorityPolicy)
    {
        if (configuration.CompatibilityMode != PresetCompatibilityMode.GameplaySanitize)
        {
            return Array.Empty<SanitizedShaderVariable>();
        }

        var actions = BuildActions(authorityPolicy);
        var changes = new List<SanitizedShaderVariable>();
        var currentSection = string.Empty;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (TryReadSection(line, out var section))
            {
                currentSection = section;
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            if (!actions.TryGetValue(new ShaderVariableKey(currentSection, key), out var action))
            {
                continue;
            }

            var techniqueActive = PresetAnalyzer.IsTechniqueActive(activeTechniques, currentSection);
            if (!techniqueActive)
            {
                continue;
            }

            var currentValue = line[(separatorIndex + 1)..];
            if (!TryApply(currentValue, action, out var newValue))
            {
                continue;
            }

            if (string.Equals(currentValue, newValue, StringComparison.Ordinal))
            {
                continue;
            }

            lines[i] = $"{line[..(separatorIndex + 1)]}{newValue}";
            changes.Add(new SanitizedShaderVariable(
                currentSection,
                key,
                currentValue,
                newValue,
                action.Type,
                action.Reason,
                action.Role,
                techniqueActive));
        }

        return changes;
    }

    private static Dictionary<ShaderVariableKey, SanitizeAction> BuildActions(GenerationAuthorityPolicy authorityPolicy)
    {
        var actions = new Dictionary<ShaderVariableKey, SanitizeAction>(ShaderVariableKeyComparer.Instance);
        foreach (var action in BaseActions)
        {
            actions[new ShaderVariableKey(action.Section, action.Key)] = action;
        }

        AddSecondaryActions(actions, authorityPolicy, EffectRole.Bloom, SecondaryBloomKeys, 0.50f, "Secondary bloom strength");
        AddSecondaryActions(actions, authorityPolicy, EffectRole.Sharpen, SecondarySharpenKeys, 0.50f, "Secondary sharpen strength");
        return actions;
    }

    private static void AddSecondaryActions(
        Dictionary<ShaderVariableKey, SanitizeAction> actions,
        GenerationAuthorityPolicy authorityPolicy,
        EffectRole role,
        IReadOnlyDictionary<string, string[]> keysBySection,
        float scale,
        string reason)
    {
        var secondarySections = authorityPolicy.Roles
            .Where(policy => policy.Role == role)
            .SelectMany(policy => policy.SecondarySections)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var section in secondarySections)
        {
            if (!keysBySection.TryGetValue(section, out var keys))
            {
                continue;
            }

            foreach (var key in keys)
            {
                actions[new ShaderVariableKey(section, key)] = new SanitizeAction(section, key, SanitizeActionType.Scale, scale, reason, role, true);
            }
        }
    }

    private static bool TryApply(string rawValue, SanitizeAction action, out string newValue)
    {
        newValue = rawValue;
        if (!float.TryParse(rawValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        var sanitized = action.Type switch
        {
            SanitizeActionType.SetToZero => 0f,
            SanitizeActionType.Scale => value * action.Amount,
            _ => value
        };

        newValue = sanitized.ToString("0.######", CultureInfo.InvariantCulture);
        return true;
    }

    private static bool TryReadSection(string line, out string section)
    {
        var trimmed = line.Trim();
        if (trimmed.Length > 2 && trimmed[0] == '[' && trimmed[^1] == ']')
        {
            section = trimmed[1..^1];
            return true;
        }

        section = string.Empty;
        return false;
    }
}
