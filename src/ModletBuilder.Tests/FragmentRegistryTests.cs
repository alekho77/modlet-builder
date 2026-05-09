using System.Xml.Linq;
using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;
using ModletBuilder.Core.Registry;

namespace ModletBuilder.Tests;

public class FragmentRegistryTests
{
    private static readonly BuildLogger NullLogger =
        new(VerbosityLevel.None, TextWriter.Null, TextWriter.Null);

    // ── Mod assignment via hint ───────────────────────────────────────────────

    [Fact]
    public void Fragment_with_hint_is_assigned_to_declared_mod()
    {
        var frag = Frag("a", "items", hints: ["ModA"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([frag], [], NullLogger);

        Assert.Empty(diagnostics);
        Assert.Single(mods);
        Assert.Equal("ModA", mods[0].ModName);
        Assert.Single(mods[0].OrderedFragments);
    }

    [Fact]
    public void Fragment_with_multiple_hints_appears_in_each_mod()
    {
        var frag = Frag("a", "items", hints: ["ModA", "ModB"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([frag], [], NullLogger);

        Assert.Empty(diagnostics);
        Assert.Equal(2, mods.Count);
        Assert.Contains(mods, m => m.ModName == "ModA");
        Assert.Contains(mods, m => m.ModName == "ModB");
    }

    [Fact]
    public void Fragment_without_hint_uses_targets_as_fallback()
    {
        var frag = Frag("a", "items");

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([frag], ["MyMod"], NullLogger);

        Assert.Empty(diagnostics);
        Assert.Single(mods);
        Assert.Equal("MyMod", mods[0].ModName);
    }

    [Fact]
    public void Fragment_without_hint_and_no_targets_is_an_error()
    {
        var frag = Frag("a", "items");

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([frag], [], NullLogger);

        Assert.Empty(mods);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'a'"));
    }

    // ── --targets as filter ───────────────────────────────────────────────────

    [Fact]
    public void Targets_filters_mods_to_only_selected_set()
    {
        var fragA = Frag("a", "items", hints: ["ModA"]);
        var fragB = Frag("b", "items", hints: ["ModB"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([fragA, fragB], ["ModA"], NullLogger);

        Assert.Empty(diagnostics);
        Assert.Single(mods);
        Assert.Equal("ModA", mods[0].ModName);
    }

    [Fact]
    public void Fragment_hints_not_overlapping_targets_are_silently_skipped()
    {
        // Fragment hints ModA and ModB; we only build ModC. Fragment should be skipped.
        var frag = Frag("a", "items", hints: ["ModA", "ModB"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([frag], ["ModC"], NullLogger);

        // Fragment is silently skipped (no per-fragment error), but since no fragments
        // remain in scope the build ends with a "no fragments matched" error.
        Assert.Empty(mods);
        Assert.DoesNotContain(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'a'"));
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("No fragments"));
    }

    // ── Dependency ordering ───────────────────────────────────────────────────

    [Fact]
    public void Fragments_are_topologically_ordered_within_mod()
    {
        var a = Frag("a", "items", hints: ["Mod"]);
        var b = Frag("b", "items", hints: ["Mod"], requires: ["a"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([b, a], [], NullLogger);

        Assert.Empty(diagnostics);
        Assert.Single(mods);
        Assert.Equal("a", mods[0].OrderedFragments[0].Name);
        Assert.Equal("b", mods[0].OrderedFragments[1].Name);
    }

    [Fact]
    public void Independent_fragments_in_mod_are_sorted_by_name()
    {
        var z = Frag("z", "items", hints: ["Mod"]);
        var a = Frag("a", "items", hints: ["Mod"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([z, a], [], NullLogger);

        Assert.Empty(diagnostics);
        Assert.Equal("a", mods[0].OrderedFragments[0].Name);
        Assert.Equal("z", mods[0].OrderedFragments[1].Name);
    }

    // ── Cross-mod dependency detection ───────────────────────────────────────

    [Fact]
    public void Cross_mod_dependency_produces_error()
    {
        // 'b' is in ModB and requires 'a' which is only in ModA.
        var a = Frag("a", "items", hints: ["ModA"]);
        var b = Frag("b", "items", hints: ["ModB"], requires: ["a"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([a, b], [], NullLogger);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error &&
            d.Message.Contains("'b'") &&
            d.Message.Contains("ModB") &&
            d.Message.Contains("'a'"));
    }

    [Fact]
    public void Fragment_in_multiple_mods_satisfies_deps_in_each_mod()
    {
        // 'a' is in both mods, so 'b' (ModA) and 'c' (ModB) can depend on it.
        var a = Frag("a", "items", hints: ["ModA", "ModB"]);
        var b = Frag("b", "items", hints: ["ModA"], requires: ["a"]);
        var c = Frag("c", "items", hints: ["ModB"], requires: ["a"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([a, b, c], [], NullLogger);

        Assert.Empty(diagnostics);
        Assert.Equal(2, mods.Count);
    }

    // ── Cycle detection ───────────────────────────────────────────────────────

    [Fact]
    public void Circular_dependency_within_mod_produces_error()
    {
        var a = Frag("a", "items", hints: ["Mod"], requires: ["b"]);
        var b = Frag("b", "items", hints: ["Mod"], requires: ["a"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([a, b], [], NullLogger);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("Circular"));
    }

    // ── Global duplicate and missing-requires checks ──────────────────────────

    [Fact]
    public void Duplicate_fragment_name_produces_error()
    {
        var a1 = Frag("a", "items", hints: ["ModA"]);
        var a2 = Frag("a", "recipes", hints: ["ModB"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([a1, a2], [], NullLogger);

        Assert.Empty(mods);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'a'"));
    }

    [Fact]
    public void Missing_required_fragment_globally_produces_error()
    {
        var a = Frag("a", "items", hints: ["Mod"], requires: ["missing"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([a], [], NullLogger);

        Assert.Empty(mods);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'missing'"));
    }

    // ── Multi-mod builds ──────────────────────────────────────────────────────

    [Fact]
    public void Multiple_mods_are_built_independently()
    {
        var a = Frag("a", "items", hints: ["ModA"]);
        var b = Frag("b", "recipes", hints: ["ModB"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([a, b], [], NullLogger);

        Assert.Empty(diagnostics);
        Assert.Equal(2, mods.Count);
        var modA = mods.Single(m => m.ModName == "ModA");
        var modB = mods.Single(m => m.ModName == "ModB");
        Assert.Single(modA.OrderedFragments);
        Assert.Single(modB.OrderedFragments);
    }

    [Fact]
    public void Mods_are_returned_in_alphabetical_order()
    {
        var z = Frag("z", "items", hints: ["ZMod"]);
        var a = Frag("a", "items", hints: ["AMod"]);

        var (mods, diagnostics) = FragmentRegistryBuilder.Build([z, a], [], NullLogger);

        Assert.Empty(diagnostics);
        Assert.Equal("AMod", mods[0].ModName);
        Assert.Equal("ZMod", mods[1].ModName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Fragment Frag(
        string name,
        string target,
        string[]? hints = null,
        string[]? requires = null) =>
        new(name, target, requires ?? [], $"{name}.frag.xml", [XElement.Parse("<append/>")])
        {
            RawHints = hints,
        };
}
