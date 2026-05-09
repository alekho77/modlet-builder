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
        IReadOnlyList<ModBuild> modBuilds,
        string modsDir,
        bool dryRun,
        bool clean,
        BuildLogger logger)
    {
        var diagnostics = new List<Diagnostic>();

        logger.Information($"Stage 3: Generating output{(dryRun ? " [DRY RUN — no files will be written]" : "")}...");

        if (dryRun)
        {
            ReportDryRun(modBuilds, modsDir, clean, logger);
            return diagnostics;
        }

        // ── Real build ────────────────────────────────────────────────────────────

        if (clean && Directory.Exists(modsDir))
        {
            logger.Information($"Cleaning output directory '{modsDir}'...");
            try
            {
                Directory.Delete(modsDir, recursive: true);
            }
            catch (Exception ex)
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Failed to clean output directory '{modsDir}': {ex.Message}"));
                return diagnostics;
            }
        }

        try
        {
            Directory.CreateDirectory(modsDir);
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not create output directory '{modsDir}': {ex.Message}"));
            return diagnostics;
        }

        foreach (var modBuild in modBuilds)
        {
            var modDiagnostics = GenerateMod(modBuild, modsDir, logger);
            diagnostics.AddRange(modDiagnostics);
        }

        return diagnostics;
    }

    // ── Dry-run reporting (no filesystem access) ──────────────────────────────

    private static void ReportDryRun(
        IReadOnlyList<ModBuild> modBuilds,
        string modsDir,
        bool clean,
        BuildLogger logger)
    {
        if (!Directory.Exists(modsDir))
        {
            logger.Warning(
                $"[DRY RUN] Output directory '{modsDir}' does not exist and would be created on a real build.");
        }
        else if (clean)
        {
            logger.Information(
                $"[DRY RUN] --clean would delete all contents of '{modsDir}' before building.");
        }
        else
        {
            var existingMods = modBuilds
                .Select(m => m.ModName)
                .Where(name => Directory.Exists(Path.Combine(modsDir, name)))
                .ToList();

            if (existingMods.Count > 0)
            {
                logger.Information(
                    $"[DRY RUN] The following mod directories would be overwritten in '{modsDir}': " +
                    string.Join(", ", existingMods) + ".");
            }
        }

        foreach (var modBuild in modBuilds)
        {
            var configFiles = modBuild.OrderedFragments
                .Select(f => f.Target)
                .Distinct()
                .Select(target => KnownTargets.GetFilePath(target))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            logger.Information(
                $"[DRY RUN] Mod '{modBuild.ModName}': {modBuild.OrderedFragments.Count} fragment(s) " +
                $"→ {configFiles.Count} config file(s): " +
                string.Join(", ", configFiles.Select(f => $"Config/{f}")) + ".");
        }
    }

    // ── Real mod output ───────────────────────────────────────────────────────

    private static IReadOnlyList<Diagnostic> GenerateMod(
        ModBuild modBuild,
        string modsDir,
        BuildLogger logger)
    {
        var diagnostics = new List<Diagnostic>();
        var configDir = Path.Combine(modsDir, modBuild.ModName, "Config");

        // Group fragments by target, preserving the resolved order within each group.
        var byTarget = new Dictionary<string, List<Fragment>>(StringComparer.Ordinal);
        foreach (var fragment in modBuild.OrderedFragments)
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

        logger.Information(
            $"Mod '{modBuild.ModName}': {modBuild.OrderedFragments.Count} fragment(s) → " +
            $"{byTarget.Count} config file(s).");

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

