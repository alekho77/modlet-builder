using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Validation;

internal static class LocalizationValidator
{
    /// <summary>
    /// Validates that no two localization entries across all resolved fragments share the same Key.
    /// Returns one error diagnostic per duplicate key, identifying both source locations.
    /// </summary>
    internal static IReadOnlyList<Diagnostic> Validate(IReadOnlyList<Fragment> fragments)
    {
        var diagnostics = new List<Diagnostic>();
        var seen = new Dictionary<string, LocalizationEntry>(StringComparer.Ordinal);

        foreach (var fragment in fragments)
        {
            foreach (var entry in fragment.LocalizationEntries)
            {
                if (seen.TryGetValue(entry.Key, out var existing))
                {
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Error,
                        $"Duplicate localization key '{entry.Key}'. " +
                        $"First defined in fragment '{existing.ParentFragmentId}' ({existing.SourceFile}).",
                        entry.SourceFile));
                }
                else
                {
                    seen[entry.Key] = entry;
                }
            }
        }

        return diagnostics;
    }
}
