using ModletBuilder.Core.Models;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace ModletBuilder.Cli;

internal static class ProjectFileLoader
{
    internal static (ModProject? Project, IReadOnlyList<Diagnostic> Diagnostics) Load(string projectFile)
    {
        var diagnostics = new List<Diagnostic>();
        var fullProjectFile = Path.GetFullPath(projectFile);

        if (!File.Exists(fullProjectFile))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Project file does not exist.",
                projectFile));
            return (null, diagnostics);
        }

        YamlStream yaml;
        try
        {
            yaml = new YamlStream();
            using var reader = File.OpenText(fullProjectFile);
            yaml.Load(reader);
        }
        catch (YamlException ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Malformed YAML: {ex.Message}",
                fullProjectFile));
            return (null, diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not read project file: {ex.Message}",
                fullProjectFile));
            return (null, diagnostics);
        }

        if (yaml.Documents.Count == 0 || yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Project file root must be a YAML mapping.",
                fullProjectFile));
            return (null, diagnostics);
        }

        var projectDir = Path.GetDirectoryName(fullProjectFile)!;
        var modFolder = RequireScalar(root, "modFolder", fullProjectFile, diagnostics);
        var output = RequireScalar(root, "output", fullProjectFile, diagnostics);
        var modInfo = ParseModInfo(root, fullProjectFile, diagnostics);
        var readme = ParseReadme(root, projectDir, fullProjectFile, diagnostics);
        var sources = ParseSources(root, projectDir, fullProjectFile, diagnostics);

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return (null, diagnostics);

        return (new ModProject(
            ModFolder: modFolder!,
            OutputRoot: ResolveProjectPath(projectDir, output!),
            ModInfo: modInfo!,
            Readme: readme,
            Sources: sources), diagnostics);
    }

    private static ReadmeSource? ParseReadme(
        YamlMappingNode root,
        string projectDir,
        string sourceFile,
        List<Diagnostic> diagnostics)
    {
        if (!TryGetChild(root, "readme", out var node))
            return null;

        if (node is not YamlScalarNode scalar || string.IsNullOrWhiteSpace(scalar.Value))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Project field 'readme' must be a non-empty scalar value.",
                sourceFile));
            return null;
        }

        var readmePath = ResolveProjectPath(projectDir, scalar.Value);
        if (Directory.Exists(readmePath))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Project field 'readme' must point to a file, not a directory.",
                readmePath));
            return null;
        }

        if (!File.Exists(readmePath))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Project readme file does not exist.",
                readmePath));
            return null;
        }

        return new ReadmeSource(readmePath);
    }

    private static ModInfo? ParseModInfo(
        YamlMappingNode root,
        string sourceFile,
        List<Diagnostic> diagnostics)
    {
        if (!TryGetChild(root, "modInfo", out var node))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Project field 'modInfo' is required.",
                sourceFile));
            return null;
        }

        if (node is not YamlMappingNode mapping)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Project field 'modInfo' must be a YAML mapping.",
                sourceFile));
            return null;
        }

        var name = RequireScalar(mapping, "name", sourceFile, diagnostics, "modInfo");
        var displayName = RequireScalar(mapping, "displayName", sourceFile, diagnostics, "modInfo");
        var description = RequireScalar(mapping, "description", sourceFile, diagnostics, "modInfo");
        var author = RequireScalar(mapping, "author", sourceFile, diagnostics, "modInfo");
        var version = RequireScalar(mapping, "version", sourceFile, diagnostics, "modInfo");
        var website = RequireScalar(mapping, "website", sourceFile, diagnostics, "modInfo");

        if (new[] { name, displayName, description, author, version, website }.Any(v => v is null))
            return null;

        return new ModInfo(name!, displayName!, description!, author!, version!, website!);
    }

    private static IReadOnlyList<SourceSpec> ParseSources(
        YamlMappingNode root,
        string projectDir,
        string sourceFile,
        List<Diagnostic> diagnostics)
    {
        if (!TryGetChild(root, "sources", out var node))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Project field 'sources' is required.",
                sourceFile));
            return [];
        }

        if (node is not YamlSequenceNode sequence)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Project field 'sources' must be a YAML sequence.",
                sourceFile));
            return [];
        }

        if (sequence.Children.Count == 0)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Project field 'sources' must contain at least one source entry.",
                sourceFile));
            return [];
        }

        var sources = new List<SourceSpec>();
        for (var i = 0; i < sequence.Children.Count; i++)
        {
            var child = sequence.Children[i];
            switch (child)
            {
                case YamlScalarNode scalar:
                    AddScalarSource(scalar, projectDir, sourceFile, i, sources, diagnostics);
                    break;

                case YamlMappingNode mapping:
                    AddMappingSource(mapping, projectDir, sourceFile, i, sources, diagnostics);
                    break;

                default:
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Error,
                        $"Source entry #{i + 1} must be a string path or mapping with 'path'.",
                        sourceFile));
                    break;
            }
        }

        return sources;
    }

    private static void AddScalarSource(
        YamlScalarNode scalar,
        string projectDir,
        string sourceFile,
        int index,
        List<SourceSpec> sources,
        List<Diagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(scalar.Value))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Source entry #{index + 1} path is required.",
                sourceFile));
            return;
        }

        sources.Add(new SourceSpec(ResolveProjectPath(projectDir, scalar.Value), Recursive: false));
    }

    private static void AddMappingSource(
        YamlMappingNode mapping,
        string projectDir,
        string sourceFile,
        int index,
        List<SourceSpec> sources,
        List<Diagnostic> diagnostics)
    {
        var path = RequireScalar(mapping, "path", sourceFile, diagnostics, $"sources[{index}]");
        var recursive = ParseOptionalBool(mapping, "recursive", sourceFile, diagnostics, $"sources[{index}]");

        if (path is null)
            return;

        sources.Add(new SourceSpec(ResolveProjectPath(projectDir, path), recursive));
    }

    private static bool ParseOptionalBool(
        YamlMappingNode mapping,
        string key,
        string sourceFile,
        List<Diagnostic> diagnostics,
        string prefix)
    {
        if (!TryGetChild(mapping, key, out var node))
            return false;

        if (node is not YamlScalarNode scalar || string.IsNullOrWhiteSpace(scalar.Value))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Project field '{prefix}.{key}' must be true or false.",
                sourceFile));
            return false;
        }

        if (bool.TryParse(scalar.Value, out var value))
            return value;

        diagnostics.Add(new Diagnostic(
            DiagnosticSeverity.Error,
            $"Project field '{prefix}.{key}' must be true or false.",
            sourceFile));
        return false;
    }

    private static string? RequireScalar(
        YamlMappingNode mapping,
        string key,
        string sourceFile,
        List<Diagnostic> diagnostics,
        string? prefix = null)
    {
        var label = prefix is null ? key : $"{prefix}.{key}";

        if (!TryGetChild(mapping, key, out var node))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Project field '{label}' is required.",
                sourceFile));
            return null;
        }

        if (node is not YamlScalarNode scalar || string.IsNullOrWhiteSpace(scalar.Value))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Project field '{label}' must be a non-empty scalar value.",
                sourceFile));
            return null;
        }

        return scalar.Value;
    }

    private static bool TryGetChild(YamlMappingNode mapping, string key, out YamlNode node)
    {
        foreach (var (childKey, childValue) in mapping.Children)
        {
            if (childKey is YamlScalarNode scalar
                && string.Equals(scalar.Value, key, StringComparison.Ordinal))
            {
                node = childValue;
                return true;
            }
        }

        node = null!;
        return false;
    }

    private static string ResolveProjectPath(string projectDir, string value)
    {
        var normalized = value.Replace('/', Path.DirectorySeparatorChar);
        return Path.IsPathRooted(normalized)
            ? Path.GetFullPath(normalized)
            : Path.GetFullPath(Path.Combine(projectDir, normalized));
    }
}

internal sealed record ModProject(
    string ModFolder,
    string OutputRoot,
    ModInfo ModInfo,
    ReadmeSource? Readme,
    IReadOnlyList<SourceSpec> Sources);
