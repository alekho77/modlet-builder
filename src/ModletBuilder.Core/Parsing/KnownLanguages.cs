namespace ModletBuilder.Core.Parsing;

internal static class KnownLanguages
{
    // Ordered to match the 7 Days to Die Localization.txt column order.
    internal static readonly string[] All =
    [
        "english",
        "german",
        "spanish",
        "french",
        "italian",
        "japanese",
        "koreana",
        "polish",
        "brazilian",
        "russian",
        "turkish",
        "schinese",
        "tchinese",
    ];

    private static readonly HashSet<string> Set = new(All, StringComparer.Ordinal);

    internal static bool IsKnown(string language) => Set.Contains(language);
}
