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
        Assert.Equal(["a", "m", "z"], ordered.Select(f => f.Name).ToArray());
    }

    private static Fragment Frag(string name, string target, string[]? requires = null) =>
        new(name, target, requires ?? [], $"{name}.frag.xml", []);
}
