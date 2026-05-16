namespace ModletBuilder.Core.Models;

/// <summary>
/// Represents a single row in the generated Localization.txt file.
/// Language values are stored in <see cref="Languages"/>, keyed by the lower-case language name
/// as defined in <c>KnownLanguages.All</c>. Absent languages are written as empty CSV cells.
/// </summary>
internal sealed record LocalizationEntry(
    string Key,
    string File,
    string Type,
    string UsedInMainMenu,
    string NoTranslate,
    string Context,
    IReadOnlyDictionary<string, string> Languages,
    string SourceFile);
