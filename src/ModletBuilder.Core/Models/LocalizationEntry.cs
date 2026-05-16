namespace ModletBuilder.Core.Models;

/// <summary>
/// Represents a single row in the generated Localization.txt file.
/// All language values that are absent in the source XML will be written as empty CSV cells.
/// </summary>
internal sealed record LocalizationEntry(
    string Key,
    string File,
    string Type,
    string UsedInMainMenu,
    string NoTranslate,
    string English,
    string Context,
    string German,
    string Spanish,
    string French,
    string Italian,
    string Japanese,
    string Koreana,
    string Polish,
    string Brazilian,
    string Russian,
    string Turkish,
    string Schinese,
    string Tchinese,
    string SourceFile,
    string ParentFragmentId);
