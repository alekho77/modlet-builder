using System.Text;
using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Generation;

internal static class LocalizationGenerator
{
    internal const string RelativePath = "Config/Localization.txt";

    internal static readonly string Header =
        "Key,File,Type,UsedInMainMenu,NoTranslate,english,Context / Alternate Text," +
        "german,spanish,french,italian,japanese,koreana,polish,brazilian,russian,turkish,schinese,tchinese";

    internal static IReadOnlyList<Diagnostic> Generate(
        IReadOnlyList<Fragment> fragments,
        string outDir,
        bool dryRun,
        BuildLogger logger)
    {
        var diagnostics = new List<Diagnostic>();
        var entries = fragments.SelectMany(f => f.LocalizationEntries).ToList();

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
        return string.Join(",",
            Escape(entry.Key),
            Escape(entry.File),
            Escape(entry.Type),
            Escape(entry.UsedInMainMenu),
            Escape(entry.NoTranslate),
            Escape(entry.English),
            Escape(entry.Context),
            Escape(entry.German),
            Escape(entry.Spanish),
            Escape(entry.French),
            Escape(entry.Italian),
            Escape(entry.Japanese),
            Escape(entry.Koreana),
            Escape(entry.Polish),
            Escape(entry.Brazilian),
            Escape(entry.Russian),
            Escape(entry.Turkish),
            Escape(entry.Schinese),
            Escape(entry.Tchinese));
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
