using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Generation;

internal static class ReadmeArtifactsGenerator
{
    internal const string ReadmeRelativePath = "README.md";
    internal const string NexusDescriptionRelativePath = "NEXUS_DESCRIPTION.bbcode";

    internal static IReadOnlyList<Diagnostic> Generate(
        ReadmeSource? readme,
        IMarkdownToBbCodeConverter? converter,
        string outDir,
        bool dryRun,
        BuildLogger logger)
    {
        var diagnostics = new List<Diagnostic>();

        if (readme is null)
            return diagnostics;

        if (dryRun)
        {
            logger.Information(
                $"[DRY RUN] README '{readme.Path}' -> {ReadmeRelativePath} and {NexusDescriptionRelativePath}.");
            return diagnostics;
        }

        if (converter is null)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Markdown to BBCode converter is not configured."));
            return diagnostics;
        }

        try
        {
            Directory.CreateDirectory(outDir);
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not create output directory '{outDir}': {ex.Message}"));
            return diagnostics;
        }

        var readmePath = Path.Combine(outDir, ReadmeRelativePath);
        try
        {
            CopyReadme(readme.Path, readmePath);
            logger.Debug($"Written '{readmePath}'.");
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not write '{readmePath}': {ex.Message}",
                readme.Path));
            return diagnostics;
        }

        var nexusPath = Path.Combine(outDir, NexusDescriptionRelativePath);
        try
        {
            if (File.Exists(nexusPath))
                File.Delete(nexusPath);
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not remove stale '{nexusPath}': {ex.Message}"));
            return diagnostics;
        }

        diagnostics.AddRange(converter.Convert(readme.Path, nexusPath, logger));
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return diagnostics;

        if (!File.Exists(nexusPath))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Markdown to BBCode converter did not create '{nexusPath}'.",
                readme.Path));
            return diagnostics;
        }

        logger.Debug($"Written '{nexusPath}'.");
        return diagnostics;
    }

    private static void CopyReadme(string sourcePath, string outputPath)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (string.Equals(
            Path.GetFullPath(sourcePath),
            Path.GetFullPath(outputPath),
            comparison))
        {
            return;
        }

        File.Copy(sourcePath, outputPath, overwrite: true);
    }
}
