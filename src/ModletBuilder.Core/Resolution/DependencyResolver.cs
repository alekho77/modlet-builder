using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Resolution;

internal static class DependencyResolver
{
    internal static (IReadOnlyList<Fragment> Ordered, IReadOnlyList<Diagnostic> Diagnostics) Resolve(
        IReadOnlyList<Fragment> fragments)
    {
        var diagnostics = new List<Diagnostic>();

        var fragmentsById = new Dictionary<string, Fragment>(StringComparer.Ordinal);
        var nameMap = new Dictionary<string, Fragment>(StringComparer.Ordinal);
        foreach (var fragment in fragments)
        {
            if (!fragmentsById.TryAdd(fragment.InternalId, fragment))
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Duplicate internal fragment id '{fragment.InternalId}'.",
                    fragment.SourceFile));
            }

            if (fragment.Name is null)
                continue;

            if (!nameMap.TryAdd(fragment.Name, fragment))
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Duplicate fragment name '{fragment.Name}'. " +
                    $"First defined in '{nameMap[fragment.Name].SourceFile}'.",
                    fragment.SourceFile));
            }
        }

        foreach (var fragment in fragments)
        {
            foreach (var req in fragment.Requires)
            {
                if (!nameMap.ContainsKey(req))
                {
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Error,
                        $"Fragment {DescribeFragment(fragment)} requires '{req}' which is not defined.",
                        fragment.SourceFile));
                }
            }
        }

        if (diagnostics.Count > 0)
            return (Array.Empty<Fragment>(), diagnostics);

        var inDegree = new Dictionary<string, int>(StringComparer.Ordinal);
        var dependents = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var fragment in fragments)
        {
            if (!inDegree.ContainsKey(fragment.InternalId))
                inDegree[fragment.InternalId] = 0;

            if (!dependents.ContainsKey(fragment.InternalId))
                dependents[fragment.InternalId] = [];

            foreach (var req in fragment.Requires)
            {
                var dependency = nameMap[req];
                if (!dependents.ContainsKey(dependency.InternalId))
                    dependents[dependency.InternalId] = [];

                dependents[dependency.InternalId].Add(fragment.InternalId);
                inDegree[fragment.InternalId] = inDegree.GetValueOrDefault(fragment.InternalId, 0) + 1;
            }
        }

        var idComparer = Comparer<string>.Create((left, right) =>
            CompareFragmentIds(left, right, fragmentsById));

        var ready = new SortedSet<string>(idComparer);
        foreach (var (id, degree) in inDegree)
        {
            if (degree == 0)
                ready.Add(id);
        }

        var ordered = new List<Fragment>(fragments.Count);

        while (ready.Count > 0)
        {
            var current = ready.Min!;
            ready.Remove(current);
            ordered.Add(fragmentsById[current]);

            foreach (var dependent in dependents[current].OrderBy(n => n, idComparer))
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                    ready.Add(dependent);
            }
        }

        if (ordered.Count != fragments.Count)
        {
            var cycleMembers = inDegree
                .Where(kv => kv.Value > 0)
                .Select(kv => fragmentsById[kv.Key])
                .OrderBy(f => OrderingKey(f), StringComparer.Ordinal);

            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Circular dependency detected among fragments: " +
                string.Join(", ", cycleMembers.Select(DescribeFragment)) + "."));

            return (Array.Empty<Fragment>(), diagnostics);
        }

        return (ordered, diagnostics);
    }

    private static int CompareFragmentIds(
        string left,
        string right,
        IReadOnlyDictionary<string, Fragment> fragmentsById)
    {
        if (left == right)
            return 0;

        var leftFragment = fragmentsById[left];
        var rightFragment = fragmentsById[right];

        var compare = string.Compare(
            OrderingKey(leftFragment),
            OrderingKey(rightFragment),
            StringComparison.Ordinal);

        return compare != 0
            ? compare
            : string.Compare(left, right, StringComparison.Ordinal);
    }

    private static string OrderingKey(Fragment fragment) =>
        fragment.Name is null ? $"1:{fragment.InternalId}" : $"0:{fragment.Name}";

    private static string DescribeFragment(Fragment fragment) =>
        fragment.Name is null
            ? $"(unnamed fragment at {fragment.InternalId})"
            : $"'{fragment.Name}'";
}
