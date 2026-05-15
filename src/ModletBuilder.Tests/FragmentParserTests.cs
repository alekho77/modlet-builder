using ModletBuilder.Core.Models;
using ModletBuilder.Core.Parsing;

namespace ModletBuilder.Tests;

public class FragmentParserTests : IDisposable
{
    private readonly string _tempDir;

    public FragmentParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Document-level validation ─────────────────────────────────────────────

    [Fact]
    public void Root_element_must_be_modlet()
    {
        var file = Write(@"<fragment name=""x"" target=""items""><append/></fragment>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(fragments);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("<modlet>"));
    }

    [Fact]
    public void Non_modlet_root_element_produces_error()
    {
        var file = Write(@"<config><item name=""x""/></config>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(fragments);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("<modlet>"));
    }

    [Fact]
    public void Empty_modlet_produces_error()
    {
        var file = Write(@"<modlet></modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(fragments);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("no <fragment>"));
    }

    [Fact]
    public void Unexpected_child_element_in_modlet_produces_error()
    {
        var file = Write(@"<modlet><other/></modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(fragments);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("<other>"));
    }

    [Fact]
    public void Malformed_xml_produces_error()
    {
        var file = Write(@"<modlet><fragment name=""x"" target=""items""><unclosed>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(fragments);
        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    // ── Single-fragment documents ─────────────────────────────────────────────

    [Fact]
    public void Single_valid_fragment_is_parsed_correctly()
    {
        var file = Write(@"
<modlet>
  <fragment name=""mymod.items.base"" target=""items"">
    <append xpath=""/items"">
      <item name=""testItem"" />
    </append>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        var f = fragments[0];
        Assert.Equal("mymod.items.base", f.Name);
        Assert.Equal("items", f.Target);
        Assert.Empty(f.Requires);
        Assert.Single(f.Body);
    }

    [Fact]
    public void Fragment_with_requires_is_parsed_correctly()
    {
        var file = Write(@"
<modlet>
  <fragment name=""mymod.recipes.base"" target=""recipes"" requires=""mymod.items.base, mymod.items.extra"">
    <append xpath=""/recipes"" />
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        Assert.Equal(["mymod.items.base", "mymod.items.extra"], fragments[0].Requires);
    }

    [Fact]
    public void Empty_requires_attribute_results_in_no_requires()
    {
        var file = Write(@"<modlet><fragment name=""mymod.items.base"" target=""items"" requires=""""><append/></fragment></modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        Assert.Empty(fragments[0].Requires);
    }

    [Fact]
    public void Body_elements_are_extracted_as_child_elements_only()
    {
        var file = Write(@"
<modlet>
  <fragment name=""mymod.items.base"" target=""items"">
    <append xpath=""/items"" />
    <set xpath=""/items/@x"">1</set>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        Assert.Equal(2, fragments[0].Body.Count);
        Assert.Equal("append", fragments[0].Body[0].Name.LocalName);
        Assert.Equal("set", fragments[0].Body[1].Name.LocalName);
    }

    [Fact]
    public void Source_file_path_is_recorded_on_fragment()
    {
        var file = Write(@"<modlet><fragment name=""mymod.items.base"" target=""items""><append/></fragment></modlet>");

        var (fragments, _) = FragmentParser.Parse(file);

        Assert.Single(fragments);
        Assert.Equal(file, fragments[0].SourceFile);
    }

    // ── Multi-fragment documents ──────────────────────────────────────────────

    [Fact]
    public void Multiple_fragments_in_one_file_are_all_parsed()
    {
        var file = Write(@"
<modlet>
  <fragment name=""mymod.items.a"" target=""items""><append/></fragment>
  <fragment name=""mymod.items.b"" target=""items""><append/></fragment>
  <fragment name=""mymod.items.c"" target=""items""><append/></fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Equal(3, fragments.Count);
        Assert.Equal(["mymod.items.a", "mymod.items.b", "mymod.items.c"],
            fragments.Select(f => f.Name).ToArray());
    }

    [Fact]
    public void Multiple_fragments_may_target_different_outputs()
    {
        var file = Write(@"
<modlet>
  <fragment name=""mymod.items"" target=""items""><append/></fragment>
  <fragment name=""mymod.recipes"" target=""recipes""><append/></fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Equal(2, fragments.Count);
        Assert.Equal("items", fragments[0].Target);
        Assert.Equal("recipes", fragments[1].Target);
    }

    [Fact]
    public void Requires_may_reference_fragment_in_same_file()
    {
        var file = Write(@"
<modlet>
  <fragment name=""mymod.items.base"" target=""items""><append/></fragment>
  <fragment name=""mymod.recipes"" target=""recipes"" requires=""mymod.items.base""><append/></fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Equal(2, fragments.Count);
        Assert.Equal(["mymod.items.base"], fragments[1].Requires);
    }

    [Fact]
    public void Source_file_path_is_recorded_on_every_fragment_in_multi_fragment_document()
    {
        var file = Write(@"
<modlet>
  <fragment name=""mymod.items"" target=""items""><append/></fragment>
  <fragment name=""mymod.recipes"" target=""recipes""><append/></fragment>
</modlet>");

        var (fragments, _) = FragmentParser.Parse(file);

        Assert.Equal(2, fragments.Count);
        Assert.All(fragments, f => Assert.Equal(file, f.SourceFile));
    }

    // ── Fragment-level validation ─────────────────────────────────────────────

    [Fact]
    public void Missing_name_attribute_produces_error()
    {
        var file = Write(@"<modlet><fragment target=""items""><append/></fragment></modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(fragments);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("name"));
    }

    [Fact]
    public void Missing_target_attribute_produces_error()
    {
        var file = Write(@"<modlet><fragment name=""mymod.items.base""><append/></fragment></modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(fragments);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("target"));
    }

    [Fact]
    public void Unknown_target_produces_error()
    {
        var file = Write(@"<modlet><fragment name=""mymod.x"" target=""not_a_valid_target""><append/></fragment></modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(fragments);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("not_a_valid_target"));
    }

    [Fact]
    public void Valid_fragments_are_returned_alongside_errors_from_invalid_fragment()
    {
        // First fragment is valid; second is invalid (missing target)
        var file = Write(@"
<modlet>
  <fragment name=""mymod.items"" target=""items""><append/></fragment>
  <fragment name=""mymod.broken""><append/></fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Single(fragments);
        Assert.Equal("mymod.items", fragments[0].Name);
        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    // ── unknown attribute diagnostics ──────────────────────────────────────────

    [Fact]
    public void Fragment_with_hint_attribute_produces_error()
    {
        var file = Write(@"
<modlet>
  <fragment name=""mymod.items"" target=""items"" hint=""ModA""><append/></fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(fragments);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("hint"));
    }

    [Fact]
    public void Modlet_with_hint_attribute_produces_error()
    {
        var file = Write(@"
<modlet hint=""SharedMod"">
  <fragment name=""mymod.items"" target=""items""><append/></fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("hint"));
    }

    [Fact]
    public void Unknown_attribute_on_fragment_produces_error()
    {
        var file = Write(@"<modlet><fragment name=""mymod.items"" target=""items"" unknown=""value""><append/></fragment></modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(fragments);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("unknown"));
    }

    private string Write(string xml)
    {
        var path = Path.Combine(_tempDir, Path.GetRandomFileName() + ".frag.xml");
        File.WriteAllText(path, xml.Trim());
        return path;
    }
}

