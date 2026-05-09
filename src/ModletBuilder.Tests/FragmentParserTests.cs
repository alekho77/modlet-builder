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

    [Fact]
    public void Valid_fragment_is_parsed_correctly()
    {
        var file = Write(@"
<fragment name=""mymod.items.base"" target=""items"">
  <append xpath=""/items"">
    <item name=""testItem"" />
  </append>
</fragment>");

        var (fragment, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.NotNull(fragment);
        Assert.Equal("mymod.items.base", fragment.Name);
        Assert.Equal("items", fragment.Target);
        Assert.Empty(fragment.Requires);
        Assert.Single(fragment.Body);
    }

    [Fact]
    public void Fragment_with_requires_is_parsed_correctly()
    {
        var file = Write(@"
<fragment name=""mymod.recipes.base"" target=""recipes"" requires=""mymod.items.base, mymod.items.extra"">
  <append xpath=""/recipes"" />
</fragment>");

        var (fragment, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.NotNull(fragment);
        Assert.Equal(["mymod.items.base", "mymod.items.extra"], fragment.Requires);
    }

    [Fact]
    public void Missing_name_attribute_produces_error()
    {
        var file = Write(@"<fragment target=""items""><append/></fragment>");

        var (fragment, diagnostics) = FragmentParser.Parse(file);

        Assert.Null(fragment);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("name"));
    }

    [Fact]
    public void Missing_target_attribute_produces_error()
    {
        var file = Write(@"<fragment name=""mymod.items.base""><append/></fragment>");

        var (fragment, diagnostics) = FragmentParser.Parse(file);

        Assert.Null(fragment);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("target"));
    }

    [Fact]
    public void Unknown_target_produces_error()
    {
        var file = Write(@"<fragment name=""mymod.x"" target=""not_a_valid_target""><append/></fragment>");

        var (fragment, diagnostics) = FragmentParser.Parse(file);

        Assert.Null(fragment);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("not_a_valid_target"));
    }

    [Fact]
    public void Non_fragment_root_element_produces_error()
    {
        var file = Write(@"<config><item name=""x""/></config>");

        var (fragment, diagnostics) = FragmentParser.Parse(file);

        Assert.Null(fragment);
        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("<fragment>"));
    }

    [Fact]
    public void Malformed_xml_produces_error()
    {
        var file = Write(@"<fragment name=""x"" target=""items""><unclosed>");

        var (fragment, diagnostics) = FragmentParser.Parse(file);

        Assert.Null(fragment);
        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Body_elements_are_extracted_as_child_elements_only()
    {
        var file = Write(@"
<fragment name=""mymod.items.base"" target=""items"">
  <append xpath=""/items"" />
  <set xpath=""/items/@x"">1</set>
</fragment>");

        var (fragment, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.NotNull(fragment);
        Assert.Equal(2, fragment.Body.Count);
        Assert.Equal("append", fragment.Body[0].Name.LocalName);
        Assert.Equal("set", fragment.Body[1].Name.LocalName);
    }

    [Fact]
    public void Empty_requires_attribute_results_in_no_requires()
    {
        var file = Write(@"<fragment name=""mymod.items.base"" target=""items"" requires=""""><append/></fragment>");

        var (fragment, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.NotNull(fragment);
        Assert.Empty(fragment.Requires);
    }

    [Fact]
    public void Source_file_path_is_recorded_on_fragment()
    {
        var file = Write(@"<fragment name=""mymod.items.base"" target=""items""><append/></fragment>");

        var (fragment, _) = FragmentParser.Parse(file);

        Assert.NotNull(fragment);
        Assert.Equal(file, fragment.SourceFile);
    }

    private string Write(string xml)
    {
        var path = Path.Combine(_tempDir, Path.GetRandomFileName() + ".frag.xml");
        File.WriteAllText(path, xml.Trim());
        return path;
    }
}
