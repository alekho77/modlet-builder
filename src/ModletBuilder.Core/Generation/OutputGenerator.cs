using System.Text;
using System.Xml;
using System.Xml.Linq;
using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;
using ModletBuilder.Core.Parsing;

namespace ModletBuilder.Core.Generation;

internal static class OutputGenerator
{
    internal static IReadOnlyList<Diagnostic> Generate(
        IReadOnlyList<Fragment> fragments,
        string outDir,
        bool dryRun,
        bool clean,
        BuildLogger logger)
    {
        var diagnostics = new List<Diagnostic>();

        logger.Information($"Stage 2: Generating output{(dryRun ? " [DRY RUN — no files will be written]" : "")}...");

        if (dryRun)
        {
            ReportDryRun(fragments, outDir, clean, logger);
            return diagnostics;
        }

        // ── Real build ────────────────────────────────────────────────────────────

        if (clean && Directory.Exists(outDir))
        {
            logger.Information($"Cleaning output directory '{outDir}'...");
            try
            {
                Directory.Delete(outDir, recursive: true);
            }
            catch (Exception ex)
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Failed to clean output directory '{outDir}': {ex.Message}"));
                return diagnostics;
            }
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

        var generateDiagnostics = GenerateOutput(fragments, outDir, logger);
        diagnostics.AddRange(generateDiagnostics);

        var localizationDiagnostics = LocalizationGenerator.Generate(fragments, outDir, dryRun: false, logger);
        diagnostics.AddRange(localizationDiagnostics);

        return diagnostics;
    }

    // ── Dry-run reporting (no filesystem access) ──────────────────────────────

    private static void ReportDryRun(
        IReadOnlyList<Fragment> fragments,
        string outDir,
        bool clean,
        BuildLogger logger)
    {
        if (!Directory.Exists(outDir))
        {
            logger.Warning(
                $"[DRY RUN] Output directory '{outDir}' does not exist and would be created on a real build.");
        }
        else if (clean)
        {
            logger.Information(
                $"[DRY RUN] --clean would delete '{outDir}' before building.");
        }

        var configFiles = fragments
            .Select(f => f.Target)
            .Distinct()
            .Select(target => KnownTargets.GetFilePath(target))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        logger.Information(
            $"[DRY RUN] {fragments.Count} fragment(s) → {configFiles.Count} config file(s): " +
            string.Join(", ", configFiles.Select(f => $"Config/{f}")) + ".");

        LocalizationGenerator.Generate(fragments, outDir, dryRun: true, logger);
    }

    // ── Real output ───────────────────────────────────────────────────────────

    private static IReadOnlyList<Diagnostic> GenerateOutput(
        IReadOnlyList<Fragment> fragments,
        string outDir,
        BuildLogger logger)
    {
        var diagnostics = new List<Diagnostic>();
        var configDir = Path.Combine(outDir, "Config");

        // Group fragments by target, preserving the resolved order within each group.
        var byTarget = new Dictionary<string, List<Fragment>>(StringComparer.Ordinal);
        foreach (var fragment in fragments)
        {
            if (!byTarget.TryGetValue(fragment.Target, out var list))
            {
                list = [];
                byTarget[fragment.Target] = list;
            }
            list.Add(fragment);
        }

        foreach (var (target, targetFragments) in byTarget.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var relativePath = KnownTargets.GetFilePath(target);
            var outputPath = Path.Combine(configDir, relativePath.Replace('/', Path.DirectorySeparatorChar));

            var dir = Path.GetDirectoryName(outputPath)!;
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Could not create directory '{dir}': {ex.Message}"));
                continue;
            }

            try
            {
                WriteConfigFile(outputPath, targetFragments);
                logger.Debug($"Written '{outputPath}' ({targetFragments.Count} fragment(s)).");
            }
            catch (Exception ex)
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Could not write '{outputPath}': {ex.Message}"));
            }
        }

        var localizationCount = fragments.Sum(f => f.LocalizationEntries.Count);
        if (localizationCount > 0)
            logger.Information($"{localizationCount} localization row(s) → {LocalizationGenerator.RelativePath}.");

        logger.Information(
            $"{fragments.Count} fragment(s) → {byTarget.Count} config file(s).");

        return diagnostics;
    }

    private static void WriteConfigFile(string outputPath, List<Fragment> fragments)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            OmitXmlDeclaration = true,
        };

        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

        // Write the XML declaration manually so the encoding value is uppercase "UTF-8".
        var declaration = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        stream.Write(declaration, 0, declaration.Length);

        using var writer = XmlWriter.Create(stream, settings);

        var config = new XElement("config");
        foreach (var fragment in fragments)
        {
            foreach (var element in fragment.Body)
                config.Add(new XElement(element));
        }

        config.WriteTo(writer);
    }
}

