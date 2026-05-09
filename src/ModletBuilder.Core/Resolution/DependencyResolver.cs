using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Resolution;

internal static class DependencyResolver
{
    internal static (IReadOnlyList<Fragment> Ordered, IReadOnlyList<Diagnostic> Diagnostics) Resolve(
        IReadOnlyList<Fragment> fragments)
    {
        var diagnostics = new List<Diagnostic>();

        // Detect duplicate names
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

        // Detect missing requires references
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
            return (Array.Empty<Fragment>(), diagnostics);

        // Kahn's topological sort
        // Build in-degree map and adjacency list
        var inDegree = new Dictionary<string, int>(StringComparer.Ordinal);
        var dependents = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var fragment in fragments)
        {
            if (!inDegree.ContainsKey(fragment.Name))
                inDegree[fragment.Name] = 0;

            if (!dependents.ContainsKey(fragment.Name))
                dependents[fragment.Name] = [];

            foreach (var req in fragment.Requires)
            {
                if (!dependents.ContainsKey(req))
                    dependents[req] = [];

                dependents[req].Add(fragment.Name);
                inDegree[fragment.Name] = inDegree.GetValueOrDefault(fragment.Name, 0) + 1;
            }
        }

        // Seed queue with zero-in-degree nodes, sorted by name for determinism
        var ready = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (name, degree) in inDegree)
        {
            if (degree == 0)
                ready.Add(name);
        }

        var ordered = new List<Fragment>(fragments.Count);

        while (ready.Count > 0)
        {
            var current = ready.Min!;
            ready.Remove(current);
            ordered.Add(nameMap[current]);

            foreach (var dependent in dependents[current].OrderBy(n => n, StringComparer.Ordinal))
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                    ready.Add(dependent);
            }
        }

        if (ordered.Count != fragments.Count)
        {
            // Cycle detected — report the names involved
            var cycleMembers = inDegree
                .Where(kv => kv.Value > 0)
                .Select(kv => kv.Key)
                .OrderBy(n => n, StringComparer.Ordinal);

            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Circular dependency detected among fragments: " +
                string.Join(", ", cycleMembers.Select(n => $"'{n}'")) + "."));

            return (Array.Empty<Fragment>(), diagnostics);
        }

        return (ordered, diagnostics);
    }
}
