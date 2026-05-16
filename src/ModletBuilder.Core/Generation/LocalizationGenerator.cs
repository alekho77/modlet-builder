using System.Text;
using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;
using ModletBuilder.Core.Parsing;

namespace ModletBuilder.Core.Generation;

internal static class LocalizationGenerator
{
    internal const string RelativePath = "Config/Localization.txt";

    internal static readonly string Header = BuildHeader();

    private static string BuildHeader()
    {
        var parts = new List<string> { "Key", "File", "Type", "UsedInMainMenu", "NoTranslate" };
        foreach (var lang in KnownLanguages.All)
        {
            parts.Add(lang);
            if (lang == "english")
                parts.Add("Context / Alternate Text");
        }
        return string.Join(",", parts);
    }

    internal static IReadOnlyList<Diagnostic> Generate(
        IReadOnlyList<LocalizationEntry> entries,
        string outDir,
        bool dryRun,
        BuildLogger logger)
    {
        var diagnostics = new List<Diagnostic>();

        if (entries.Count == 0)
            return diagnostics;

        if (dryRun)
        {
            logger.Information(
                $"[DRY RUN] {entries.Count} localization row(s) → {RelativePath}.");
            return diagnostics;
        }

        var outputPath = Path.Combine(
            outDir,
            RelativePath.Replace('/', Path.DirectorySeparatorChar));

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
            return diagnostics;
        }

        try
        {
            WriteLocalizationFile(outputPath, entries);
            logger.Debug($"Written '{outputPath}' ({entries.Count} row(s)).");
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not write '{outputPath}': {ex.Message}"));
        }

        return diagnostics;
    }

    internal static void WriteLocalizationFile(string outputPath, IReadOnlyList<LocalizationEntry> entries)
    {
        // UTF-8 without BOM, consistent with Config/*.xml files.
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        writer.WriteLine(Header);

        foreach (var entry in entries)
        {
            writer.WriteLine(ToCsvRow(entry));
        }
    }

    internal static string ToCsvRow(LocalizationEntry entry)
    {
        var parts = new List<string>
        {
            Escape(entry.Key),
            Escape(entry.File),
            Escape(entry.Type),
            Escape(entry.UsedInMainMenu),
            Escape(entry.NoTranslate),
        };
        foreach (var lang in KnownLanguages.All)
        {
            parts.Add(Escape(entry.Languages.TryGetValue(lang, out var v) ? v : string.Empty));
            if (lang == "english")
                parts.Add(Escape(entry.Context));
        }
        return string.Join(",", parts);
    }

    // RFC 4180 CSV escaping: wrap in quotes if the value contains commas, quotes, CR, or LF.
    // Double any embedded quotes.
    private static string Escape(string value)
    {
        if (value.Length == 0)
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }
}
