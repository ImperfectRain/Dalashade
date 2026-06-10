using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record ColorFamilyComparisonRow(
    ColorFamily Family,
    ColorFamilyStats Current,
    ColorFamilyStats Master,
    ColorFamilyAdjustment Adjustment);

public static class ColorFamilyComparisonRows
{
    private const float MeaningfulAdjustmentScore = 0.0005f;
    private const float LowConfidence = 0.10f;

    public static IReadOnlyList<ColorFamilyComparisonRow> Build(ImageAnalysisResult current, ImageAnalysisResult master, VisualProfile profile, bool includeLowConfidenceRows = true)
    {
        return Enum.GetValues<ColorFamily>()
            .Select(family =>
            {
                var currentStats = current.ColorFamilies.TryGetValue(family, out var currentValue) ? currentValue : ColorFamilyStats.Empty(family);
                var masterStats = master.ColorFamilies.TryGetValue(family, out var masterValue) ? masterValue : ColorFamilyStats.Empty(family);
                var adjustment = profile.GetColorFamilyAdjustment(family);
                return new ColorFamilyComparisonRow(family, currentStats, masterStats, adjustment);
            })
            .OrderByDescending(row => row.Adjustment.Score)
            .ThenByDescending(row => MathF.Max(row.Current.Confidence, row.Master.Confidence))
            .ThenBy(row => row.Family)
            .Where(row => includeLowConfidenceRows || IsVisibleByDefault(row))
            .ToArray();
    }

    public static bool IsVisibleByDefault(ColorFamilyComparisonRow row)
    {
        return row.Adjustment.Score > MeaningfulAdjustmentScore
            || MathF.Max(row.Current.Confidence, row.Master.Confidence) >= LowConfidence;
    }
}
