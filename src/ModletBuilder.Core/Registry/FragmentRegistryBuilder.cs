using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;
using ModletBuilder.Core.Resolution;

namespace ModletBuilder.Core.Registry;

internal static class FragmentRegistryBuilder
{
    /// <summary>
    /// Builds per-mod ordered fragment lists from a flat set of parsed fragments.
    /// </summary>
    /// <param name="fragments">All fragments discovered across all source files.</param>
    /// <param name="selectedTargets">
    ///   Mod names from --targets. When non-empty, acts as both a build filter and the
    ///   fallback mod assignment for fragments with no hint. When empty, all mods
    ///   declared via hints are built, and fragments with no hint are errors.
    /// </param>
    /// <param name="logger">Logger for progress and diagnostic messages.</param>
    internal static (IReadOnlyList<ModBuild> Mods, IReadOnlyList<Diagnostic> Diagnostics) Build(
        IReadOnlyList<Fragment> fragments,
        string[] selectedTargets,
        BuildLogger logger)
    {
        var diagnostics = new List<Diagnostic>();

        logger.Information($"Stage 1: Building fragment registry from {fragments.Count} fragment(s)...");

        // ── 1. Global duplicate name detection ───────────────────────────────────
        var nameMap = new Dictionary<string, Fragment>(StringComparer.Ordinal);
        foreach (var fragment in fragments)
        {
            if (!nameMap.TryAdd(fragment.Name, fragment))
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Duplicate fragment name '{fragment.Name}'. " +
                    $"First defined in '{nameMap[fragment.Name].SourceFile}'.",
                    fragment.SourceFile));
            }
        }

        if (diagnostics.Count > 0)
            return ([], diagnostics);

        // ── 2. Global missing-requires detection ─────────────────────────────────
        foreach (var fragment in fragments)
        {
            foreach (var req in fragment.Requires)
            {
                if (!nameMap.ContainsKey(req))
                {
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Error,
                        $"Fragment '{fragment.Name}' requires '{req}' which is not defined.",
                        fragment.SourceFile));
                }
            }
        }

        if (diagnostics.Count > 0)
            return ([], diagnostics);

        // ── 3. Compute effective mod membership per fragment ──────────────────────
        var selectedSet = selectedTargets.Length > 0
            ? new HashSet<string>(selectedTargets, StringComparer.OrdinalIgnoreCase)
            : null;

        var modToFragments = new Dictionary<string, List<Fragment>>(StringComparer.OrdinalIgnoreCase);
        bool hasOrphans = false;

        foreach (var fragment in fragments)
        {
            string[] effectiveMods;

            if (fragment.RawHints is { Length: > 0 })
            {
                if (selectedSet is not null)
                {
                    // Filter hints to the selected set; fragments with no intersection are out of scope.
                    effectiveMods = fragment.RawHints
                        .Where(h => selectedSet.Contains(h))
                        .ToArray();

                    if (effectiveMods.Length == 0)
                    {
                        logger.Debug(
                            $"Fragment '{fragment.Name}' skipped: hints [{string.Join(", ", fragment.RawHints)}] " +
                            $"do not overlap with selected targets [{string.Join(", ", selectedTargets)}].");
                        continue;
                    }
                }
                else
                {
                    effectiveMods = fragment.RawHints;
                }
            }
            else
            {
                if (selectedSet is not null)
                {
                    // No hint: use --targets as fallback assignment.
                    effectiveMods = selectedTargets;
                }
                else
                {
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Error,
                        $"Fragment '{fragment.Name}' has no 'hint' attribute and no --targets fallback is specified. " +
                        "Add a 'hint' attribute to the <fragment> or <modlet> element, or pass --targets.",
                        fragment.SourceFile));
                    hasOrphans = true;
                    continue;
                }
            }

            logger.Debug(
                $"Fragment '{fragment.Name}' → mod(s): [{string.Join(", ", effectiveMods)}].");

            foreach (var mod in effectiveMods)
            {
                if (!modToFragments.TryGetValue(mod, out var list))
                {
                    list = [];
                    modToFragments[mod] = list;
                }
                list.Add(fragment);
            }
        }

        if (hasOrphans)
            return ([], diagnostics);

        if (modToFragments.Count == 0)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "No fragments matched the current build selection. " +
                "Check --targets or add 'hint' attributes to your source documents."));
            return ([], diagnostics);
        }

        var modNames = modToFragments.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        logger.Information(
            $"Registry: {fragments.Count} fragment(s), {modToFragments.Count} mod(s) " +
            $"({string.Join(", ", modNames)}).");

        // ── 4. Per-mod cross-mod dependency check + topological ordering ─────────
        logger.Information("Stage 2: Resolving dependencies and ordering fragments per mod...");

        var modBuilds = new List<ModBuild>();

        foreach (var modName in modNames)
        {
            var modFragments = modToFragments[modName];
            var modFragmentNames = new HashSet<string>(
                modFragments.Select(f => f.Name),
                StringComparer.Ordinal);

            bool hasCrossModDep = false;
            foreach (var fragment in modFragments)
            {
                foreach (var req in fragment.Requires)
                {
                    if (!modFragmentNames.Contains(req))
                    {
                        var reqFragment = nameMap[req];
                        diagnostics.Add(new Diagnostic(
                            DiagnosticSeverity.Error,
                            $"Fragment '{fragment.Name}' in mod '{modName}' requires '{req}' " +
                            $"('{reqFragment.SourceFile}') which is not assigned to mod '{modName}'. " +
                            $"Add hint=\"{modName}\" to fragment '{req}' or its <modlet> element.",
                            fragment.SourceFile));
                        hasCrossModDep = true;
                    }
                }
            }

            if (hasCrossModDep)
                continue;

            var (ordered, resolveDiagnostics) = DependencyResolver.Resolve(modFragments);
            diagnostics.AddRange(resolveDiagnostics);

            if (resolveDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                continue;

            logger.Debug(
                $"Mod '{modName}': {ordered.Count} fragment(s) ordered across " +
                $"{ordered.Select(f => f.Target).Distinct().Count()} target(s).");

            modBuilds.Add(new ModBuild(modName, ordered));
        }

        return (modBuilds, diagnostics);
    }
}
