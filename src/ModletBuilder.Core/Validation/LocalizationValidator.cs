using System.Xml.Linq;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Validation;


internal static class LocalizationValidator
{
    /// <summary>
    /// Targets that are known to contain objects supporting the DescriptionKey property
    /// (items, blocks, item_modifiers — confirmed from vanilla 7 Days to Die config).
    /// </summary>
    private static readonly HashSet<string> DescriptionKeyTargets = new(StringComparer.Ordinal)
    {
        "items",
        "blocks",
        "item_modifiers",
    };

    /// <summary>
    /// Validates that <c>&lt;property name="DescriptionKey"/&gt;</c> elements only appear
    /// inside fragments whose target is one of the known supported targets
    /// (<c>items</c>, <c>blocks</c>, <c>item_modifiers</c>). Emits one warning per
    /// offending fragment.
    /// </summary>
    internal static IReadOnlyList<Diagnostic> ValidateDescriptionKeyTargets(
        IReadOnlyList<Fragment> fragments)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (var fragment in fragments)
        {
            if (DescriptionKeyTargets.Contains(fragment.Target))
                continue;

            foreach (var element in fragment.Body.SelectMany(e => e.DescendantsAndSelf()))
            {
                if (element.Name.LocalName == "property"
                    && element.Attribute("name")?.Value == "DescriptionKey")
                {
                    var value = element.Attribute("value")?.Value ?? "(no value)";
                    var label = fragment.Name is not null
                        ? $"Fragment '{fragment.Name}'"
                        : "An unnamed fragment";
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Warning,
                        $"{label} targets '{fragment.Target}', which does not support DescriptionKey. " +
                        $"The property value '{value}' will have no effect in the generated output. " +
                        $"DescriptionKey is only valid inside objects in: {string.Join(", ", DescriptionKeyTargets)}.",
                        fragment.SourceFile));
                    break; // one warning per fragment is sufficient
                }
            }
        }

        return diagnostics;
    }


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
