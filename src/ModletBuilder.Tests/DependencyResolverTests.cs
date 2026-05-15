using ModletBuilder.Core.Models;
using ModletBuilder.Core.Resolution;

namespace ModletBuilder.Tests;

public class DependencyResolverTests
{
    [Fact]
    public void Single_fragment_with_no_deps_is_returned_as_is()
    {
        var fragment = Frag("a", "items");
        var (ordered, diagnostics) = DependencyResolver.Resolve([fragment]);

        Assert.Empty(diagnostics);
        Assert.Single(ordered);
        Assert.Equal("a", ordered[0].Name);
    }

    [Fact]
    public void Simple_dependency_chain_is_ordered_correctly()
    {
        var a = Frag("a", "items");
        var b = Frag("b", "items", requires: ["a"]);

        var (ordered, diagnostics) = DependencyResolver.Resolve([b, a]);

        Assert.Empty(diagnostics);
        Assert.Equal(2, ordered.Count);
        Assert.Equal("a", ordered[0].Name);
        Assert.Equal("b", ordered[1].Name);
    }

    [Fact]
    public void Diamond_dependency_is_resolved_correctly()
    {
        // a → b, a → c, d requires b and c
        var a = Frag("a", "items");
        var b = Frag("b", "items", requires: ["a"]);
        var c = Frag("c", "items", requires: ["a"]);
        var d = Frag("d", "items", requires: ["b", "c"]);

        var (ordered, diagnostics) = DependencyResolver.Resolve([d, c, b, a]);

        Assert.Empty(diagnostics);
        Assert.Equal(4, ordered.Count);
        Assert.Equal("a", ordered[0].Name);
        // b and c both depend only on a — they are ordered by name
        Assert.Equal("b", ordered[1].Name);
        Assert.Equal("c", ordered[2].Name);
        Assert.Equal("d", ordered[3].Name);
    }

    [Fact]
    public void Cycle_detection_produces_error()
    {
        var a = Frag("a", "items", requires: ["b"]);
        var b = Frag("b", "items", requires: ["a"]);

        var (ordered, diagnostics) = DependencyResolver.Resolve([a, b]);

        Assert.Empty(ordered);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("Circular"));
    }

    [Fact]
    public void Missing_requires_reference_produces_error()
    {
        var a = Frag("a", "items", requires: ["missing"]);

        var (ordered, diagnostics) = DependencyResolver.Resolve([a]);

        Assert.Empty(ordered);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("missing"));
    }

    [Fact]
    public void Duplicate_name_produces_error()
    {
        var a1 = Frag("a", "items");
        var a2 = Frag("a", "recipes");

        var (ordered, diagnostics) = DependencyResolver.Resolve([a1, a2]);

        Assert.Empty(ordered);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'a'"));
    }

    [Fact]
    public void Cross_target_requires_is_resolved_correctly()
    {
        var a = Frag("a", "items");
        var b = Frag("b", "recipes", requires: ["a"]);

        var (ordered, diagnostics) = DependencyResolver.Resolve([b, a]);

        Assert.Empty(diagnostics);
        Assert.Equal(2, ordered.Count);
        Assert.Equal("a", ordered[0].Name);
        Assert.Equal("b", ordered[1].Name);
    }

    [Fact]
    public void Independent_fragments_are_sorted_by_name_for_determinism()
    {
        var z = Frag("z", "items");
        var a = Frag("a", "items");
        var m = Frag("m", "items");

        var (ordered, diagnostics) = DependencyResolver.Resolve([z, a, m]);

        Assert.Empty(diagnostics);
        Assert.Equal(["a", "m", "z"], ordered.Select(f => f.Name!).ToArray());
    }

    [Fact]
    public void Duplicate_name_diagnostic_includes_first_definition_source_file()
    {
        // The diagnostic for a duplicate must reference the source file of the FIRST definition.
        var first = new Fragment("id:first", "a", "items", [], "first.frag.xml", []);
        var second = new Fragment("id:second", "a", "recipes", [], "second.frag.xml", []);

        var (_, diagnostics) = DependencyResolver.Resolve([first, second]);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("first.frag.xml"));
    }

    [Fact]
    public void Three_fragment_cycle_diagnostic_includes_all_cycle_members_sorted()
    {
        // c→a, a→b, b→c forms a 3-way cycle.
        var a = Frag("a", "items", requires: ["c"]);
        var b = Frag("b", "items", requires: ["a"]);
        var c = Frag("c", "items", requires: ["b"]);

        var (ordered, diagnostics) = DependencyResolver.Resolve([a, b, c]);

        Assert.Empty(ordered);
        var error = Assert.Single(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // All three member names must appear in the message, sorted.
        Assert.Contains("'a'", error.Message);
        Assert.Contains("'b'", error.Message);
        Assert.Contains("'c'", error.Message);
        // Verify sorted order: 'a' appears before 'b', 'b' before 'c'.
        Assert.True(error.Message.IndexOf("'a'", StringComparison.Ordinal)
            < error.Message.IndexOf("'b'", StringComparison.Ordinal));
        Assert.True(error.Message.IndexOf("'b'", StringComparison.Ordinal)
            < error.Message.IndexOf("'c'", StringComparison.Ordinal));
    }

    [Fact]
    public void Long_transitive_chain_resolves_in_dependency_order()
    {   // a ← b ← c ← d ← e (each requires the one before it)
        var a = Frag("a", "items");
        var b = Frag("b", "items", requires: ["a"]);
        var c = Frag("c", "items", requires: ["b"]);
        var d = Frag("d", "items", requires: ["c"]);
        var e = Frag("e", "items", requires: ["d"]);

        // Submit in reverse order to verify sorting is not input-order dependent.
        var (ordered, diagnostics) = DependencyResolver.Resolve([e, d, c, b, a]);

        Assert.Empty(diagnostics);
        Assert.Equal(["a", "b", "c", "d", "e"], ordered.Select(f => f.Name!).ToArray());
    }

    [Fact]
    public void Unnamed_fragment_with_no_dependencies_is_resolved()
    {
        var fragment = Frag(null, "items", internalId: "source/items.frag.xml#L1#F0");

        var (ordered, diagnostics) = DependencyResolver.Resolve([fragment]);

        Assert.Empty(diagnostics);
        Assert.Single(ordered);
        Assert.Null(ordered[0].Name);
        Assert.Equal("source/items.frag.xml#L1#F0", ordered[0].InternalId);
    }

    [Fact]
    public void Unnamed_fragment_may_require_named_fragment()
    {
        var named = Frag("items.base", "items");
        var unnamed = Frag(null, "recipes", requires: ["items.base"], internalId: "source/recipes.frag.xml#L1#F0");

        var (ordered, diagnostics) = DependencyResolver.Resolve([unnamed, named]);

        Assert.Empty(diagnostics);
        Assert.Equal("items.base", ordered[0].Name);
        Assert.Null(ordered[1].Name);
    }

    [Fact]
    public void Duplicate_name_detection_ignores_unnamed_fragments()
    {
        var unnamedA = Frag(null, "items", internalId: "source/a.frag.xml#L1#F0");
        var unnamedB = Frag(null, "recipes", internalId: "source/b.frag.xml#L1#F0");

        var (ordered, diagnostics) = DependencyResolver.Resolve([unnamedB, unnamedA]);

        Assert.Empty(diagnostics);
        Assert.Equal(2, ordered.Count);
    }

    [Fact]
    public void Missing_requires_from_unnamed_fragment_reports_internal_id()
    {
        var unnamed = Frag(null, "recipes", requires: ["missing"], internalId: "source/recipes.frag.xml#L7#F0");

        var (ordered, diagnostics) = DependencyResolver.Resolve([unnamed]);

        Assert.Empty(ordered);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error
            && d.Message.Contains("unnamed fragment")
            && d.Message.Contains("source/recipes.frag.xml#L7#F0")
            && d.Message.Contains("missing"));
    }

    [Fact]
    public void Independent_mixed_named_and_unnamed_fragments_are_ordered_deterministically()
    {
        var unnamedB = Frag(null, "items", internalId: "source/b.frag.xml#L1#F0");
        var named = Frag("a", "items");
        var unnamedA = Frag(null, "items", internalId: "source/a.frag.xml#L1#F0");

        var (ordered, diagnostics) = DependencyResolver.Resolve([unnamedB, named, unnamedA]);

        Assert.Empty(diagnostics);
        Assert.Equal("a", ordered[0].Name);
        Assert.Null(ordered[1].Name);
        Assert.Null(ordered[2].Name);
        Assert.Equal("source/a.frag.xml#L1#F0", ordered[1].InternalId);
        Assert.Equal("source/b.frag.xml#L1#F0", ordered[2].InternalId);
    }

    private static Fragment Frag(
        string? name,
        string target,
        string[]? requires = null,
        string? internalId = null) =>
        new(
            internalId ?? $"id:{name}",
            name,
            target,
            requires ?? [],
            $"{name ?? internalId}.frag.xml",
            []);
}
