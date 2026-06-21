using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dalashade;

public sealed record ShaderUniformSourceFile(
    string FileName,
    string Path,
    bool Found,
    bool ManualDebugShader,
    IReadOnlyList<string> Uniforms,
    string Message);

public sealed record ShaderUniformParityIssue(
    string Severity,
    string Variable,
    string Detail);

public sealed record ShaderUniformParityDiagnostics(
    string Status,
    string Summary,
    IReadOnlyList<string> ExpectedGeneratedVariables,
    IReadOnlyList<string> ShaderUniforms,
    IReadOnlyList<string> ManualDebugUniforms,
    IReadOnlyList<ShaderUniformParityIssue> Issues,
    IReadOnlyList<ShaderUniformSourceFile> Files,
    IReadOnlyList<string> Notes);

public static partial class ShaderUniformParityDiagnosticsBuilder
{
    [GeneratedRegex(@"\buniform\s+(?:bool|int|uint|float|float2|float3|float4)\s+([A-Za-z_][A-Za-z0-9_]*)\b", RegexOptions.Compiled)]
    private static partial Regex UniformRegex();

    public static ShaderUniformParityDiagnostics Build(Configuration configuration)
    {
        var expected = CustomShaderVariableMapper.KnownVariableNames
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var expectedSet = new HashSet<string>(expected, StringComparer.OrdinalIgnoreCase);
        var sourceFiles = FirstPartyShaderRegistry.All
            .Select(shader => BuildSourceFile(configuration, shader))
            .ToArray();

        var productionUniforms = sourceFiles
            .Where(file => file.Found && !file.ManualDebugShader)
            .SelectMany(file => file.Uniforms)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var manualDebugUniforms = sourceFiles
            .Where(file => file.Found && file.ManualDebugShader)
            .SelectMany(file => file.Uniforms)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var discoveredSet = new HashSet<string>(
            productionUniforms.Concat(manualDebugUniforms),
            StringComparer.OrdinalIgnoreCase);
        var productionDiscoveredSet = new HashSet<string>(productionUniforms, StringComparer.OrdinalIgnoreCase);

        var missingShaderUniforms = expected
            .Where(variable => !discoveredSet.Contains(variable))
            .Select(variable => new ShaderUniformParityIssue(
                "Warning",
                variable,
                "Generated-variable mapper knows this variable, but no installed first-party shader uniform with this name was found. This can be normal if the shader file is not installed, but it is a dead-write risk if the shader should consume it."))
            .ToArray();

        var unmanagedShaderUniforms = productionUniforms
            .Where(variable => IsDalashadeManagedUniform(variable) && !expectedSet.Contains(variable))
            .Select(variable => new ShaderUniformParityIssue(
                "Warning",
                variable,
                "Installed first-party shader exposes a Dalashade-managed uniform that the generated-variable mapper does not know how to write."))
            .ToArray();

        var issues = missingShaderUniforms
            .Concat(unmanagedShaderUniforms)
            .OrderBy(issue => issue.Variable, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var foundFileCount = sourceFiles.Count(file => file.Found);
        var status = foundFileCount == 0
            ? "NoShaderFilesFound"
            : issues.Length == 0
                ? "Pass"
                : "Warnings";

        return new ShaderUniformParityDiagnostics(
            status,
            foundFileCount == 0
                ? "No installed first-party shader files were found in inferred ReShade shader paths, so uniform parity could not be validated."
                : $"Scanned {foundFileCount} installed first-party shader file(s); {issues.Length} parity warning(s) found.",
            expected,
            productionUniforms,
            manualDebugUniforms,
            issues,
            sourceFiles,
            [
                "This is diagnostic-only. It does not change generated preset output, shader code, or technique activation.",
                "Dalapad_Debug.fx uniforms are listed separately because that shader is manual/debug-only and is not part of production technique sync.",
                "Missing shader uniforms are most actionable when the related shader file is installed and expected to consume the generated value."
            ]);
    }

    private static ShaderUniformSourceFile BuildSourceFile(Configuration configuration, FirstPartyShaderMetadata shader)
    {
        var fileName = shader.FileName;
        var location = ShaderFileLocator.Find(configuration, fileName);
        var manualDebugShader = shader.ManualDebugShader;

        if (!location.Found || string.IsNullOrWhiteSpace(location.FullPath))
        {
            return new ShaderUniformSourceFile(fileName, string.Empty, false, manualDebugShader, Array.Empty<string>(), location.Message);
        }

        try
        {
            var text = File.ReadAllText(location.FullPath);
            var uniforms = UniformRegex()
                .Matches(text)
                .Select(match => match.Groups[1].Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return new ShaderUniformSourceFile(fileName, location.FullPath, true, manualDebugShader, uniforms, location.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new ShaderUniformSourceFile(fileName, location.FullPath, false, manualDebugShader, Array.Empty<string>(), $"Failed to read shader file: {ex.Message}");
        }
    }

    private static bool IsDalashadeManagedUniform(string variable)
    {
        return variable.StartsWith("Dalashade_", StringComparison.OrdinalIgnoreCase)
            || variable.StartsWith("CanopyGap", StringComparison.OrdinalIgnoreCase)
            || variable.StartsWith("Sharpen", StringComparison.OrdinalIgnoreCase);
    }
}
