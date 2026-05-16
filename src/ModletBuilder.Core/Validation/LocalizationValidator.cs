using System.Xml.Linq;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Validation;


internal static class LocalizationValidator
{
    /// <summary>
    /// Validates that no two localization entries across all resolved fragments share the same Key.
    /// Returns one error diagnostic per duplicate key, identifying both source locations.
    /// </summary>
    internal static IReadOnlyList<Diagnostic> Validate(IReadOnlyList<LocalizationEntry> entries)
    {
        var diagnostics = new List<Diagnostic>();
        var seen = new Dictionary<string, LocalizationEntry>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            if (seen.TryGetValue(entry.Key, out var existing))
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Duplicate localization key '{entry.Key}'. " +
                    $"First defined in '{existing.SourceFile}'.",
                    entry.SourceFile));
            }
            else
            {
                seen[entry.Key] = entry;
            }
        }

        return diagnostics;
    }

    /// <summary>
    /// Validates that every localization entry key is referenced by at least one
    /// <c>&lt;property name="DescriptionKey" value="..."/&gt;</c> element inside
    /// a fragment body. Keys that are not linked to any DescriptionKey property
    /// are considered orphaned and are reported as errors.
    /// </summary>
    internal static IReadOnlyList<Diagnostic> ValidateOrphanedLocalizationKeys(
        IReadOnlyList<LocalizationEntry> entries,
        IReadOnlyList<Fragment> fragments)
    {
        var descriptionKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var fragment in fragments)
        {
            foreach (var element in fragment.Body.SelectMany(e => e.DescendantsAndSelf()))
            {
                if (element.Name.LocalName == "property"
                    && element.Attribute("name")?.Value == "DescriptionKey"
                    && element.Attribute("value") is XAttribute valueAttr)
                {
                    descriptionKeys.Add(valueAttr.Value);
                }
            }
        }

        var diagnostics = new List<Diagnostic>();
        foreach (var entry in entries)
        {
            if (!descriptionKeys.Contains(entry.Key))
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Localization key '{entry.Key}' is not referenced by any " +
                    $"<property name=\"DescriptionKey\" value=\"{entry.Key}\"/> in any fragment. " +
                    $"Every localization entry must be explicitly linked to a game object via DescriptionKey.",
                    entry.SourceFile));
            }
        }

        return diagnostics;
    }
}
