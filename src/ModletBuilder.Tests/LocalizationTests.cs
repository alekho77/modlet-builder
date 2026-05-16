using System.Text;
using ModletBuilder.Core.Generation;
using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;
using ModletBuilder.Core.Parsing;
using ModletBuilder.Core.Validation;

namespace ModletBuilder.Tests;

public class LocalizationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly BuildLogger _nullLogger;

    public LocalizationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        _nullLogger = new BuildLogger(VerbosityLevel.None, TextWriter.Null, TextWriter.Null);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Parser: localization block extraction ─────────────────────────────────

    [Fact]
    public void Localization_block_is_parsed_with_all_required_attributes()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""myKey"" file=""items"" type=""Item"">
      <english text=""My Item""/>
    </localization>
    <append xpath=""/items""/>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        Assert.Single(fragments[0].LocalizationEntries);
        var entry = fragments[0].LocalizationEntries[0];
        Assert.Equal("myKey", entry.Key);
        Assert.Equal("items", entry.File);
        Assert.Equal("Item", entry.Type);
        Assert.Equal("My Item", entry.English);
        Assert.Equal(string.Empty, entry.Russian);
    }

    [Fact]
    public void Localization_block_is_stripped_from_fragment_body()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""k"" file=""items"" type=""Item""><english text=""x""/></localization>
    <append xpath=""/items""/>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        // Body must contain only <append>, not the <localization> node.
        Assert.Single(fragments[0].Body);
        Assert.Equal("append", fragments[0].Body[0].Name.LocalName);
    }

    [Fact]
    public void Fragment_body_only_localization_produces_empty_body()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""k"" file=""items"" type=""Item""><english text=""x""/></localization>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        Assert.Empty(fragments[0].Body);
        Assert.Single(fragments[0].LocalizationEntries);
    }

    [Fact]
    public void Multiple_localization_blocks_in_one_fragment_are_all_parsed()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""k1"" file=""items"" type=""Item""><english text=""A""/></localization>
    <localization key=""k2"" file=""items"" type=""Item""><english text=""B""/></localization>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        Assert.Equal(2, fragments[0].LocalizationEntries.Count);
        Assert.Equal("k1", fragments[0].LocalizationEntries[0].Key);
        Assert.Equal("k2", fragments[0].LocalizationEntries[1].Key);
    }

    [Fact]
    public void Localization_block_missing_key_produces_error()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization file=""items"" type=""Item""><english text=""x""/></localization>
    <append/>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'key'"));
    }

    [Fact]
    public void Localization_block_missing_file_produces_error()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""k"" type=""Item""><english text=""x""/></localization>
    <append/>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'file'"));
    }

    [Fact]
    public void Localization_block_missing_type_produces_error()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""k"" file=""items""><english text=""x""/></localization>
    <append/>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'type'"));
    }

    [Fact]
    public void Localization_block_unknown_attribute_produces_error()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""k"" file=""items"" type=""Item"" extra=""x""><english text=""x""/></localization>
    <append/>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'extra'"));
    }

    [Fact]
    public void Localization_block_unknown_language_element_produces_error()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""k"" file=""items"" type=""Item"">
      <elvish text=""Namárië""/>
    </localization>
    <append/>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("<elvish>"));
    }

    [Fact]
    public void Localization_block_duplicate_language_element_produces_error()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""k"" file=""items"" type=""Item"">
      <english text=""first""/>
      <english text=""second""/>
    </localization>
    <append/>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("<english>"));
    }

    [Fact]
    public void Localization_block_language_element_missing_text_produces_error()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""k"" file=""items"" type=""Item"">
      <english/>
    </localization>
    <append/>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'text'"));
    }

    [Fact]
    public void Localization_block_optional_attributes_are_captured()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""k"" file=""items"" type=""Item"" context=""hint"" usedInMainMenu=""True"" noTranslate=""False"">
      <english text=""x""/>
    </localization>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        var entry = fragments[0].LocalizationEntries[0];
        Assert.Equal("hint", entry.Context);
        Assert.Equal("True", entry.UsedInMainMenu);
        Assert.Equal("False", entry.NoTranslate);
    }

    [Fact]
    public void Fragment_without_localization_block_has_empty_entries_list()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <append xpath=""/items""/>
  </fragment>
</modlet>");

        var (fragments, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Empty(fragments[0].LocalizationEntries);
    }

    // ── Validator: duplicate key detection ────────────────────────────────────

    [Fact]
    public void Validator_returns_no_errors_for_unique_keys()
    {
        var fragments = new[]
        {
            FragWith("fragA", "items", "k1"),
            FragWith("fragB", "items", "k2"),
        };

        var diagnostics = LocalizationValidator.Validate(fragments);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validator_returns_error_for_duplicate_key()
    {
        var fragments = new[]
        {
            FragWith("fragA", "items", "dupKey"),
            FragWith("fragB", "items", "dupKey"),
        };

        var diagnostics = LocalizationValidator.Validate(fragments);

        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
        Assert.Contains("dupKey", diagnostics[0].Message);
    }

    [Fact]
    public void Validator_identifies_first_occurrence_in_error_message()
    {
        var fragments = new[]
        {
            FragWith("fragA", "items", "shared"),
            FragWith("fragB", "items", "shared"),
        };

        var diagnostics = LocalizationValidator.Validate(fragments);

        Assert.Contains("fragA", diagnostics[0].Message);
    }

    [Fact]
    public void Validator_reports_each_duplicate_occurrence_separately()
    {
        var fragments = new[]
        {
            FragWith("fragA", "items", "key"),
            FragWith("fragB", "items", "key"),
            FragWith("fragC", "items", "key"),
        };

        var diagnostics = LocalizationValidator.Validate(fragments);

        Assert.Equal(2, diagnostics.Count);
    }

    // ── LocalizationGenerator: CSV output ────────────────────────────────────

    [Fact]
    public void Generator_writes_localization_txt_with_correct_header()
    {
        var fragments = new[] { FragWith("frag", "items", "myKey", english: "My Item") };

        LocalizationGenerator.Generate(fragments, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        Assert.True(File.Exists(path));

        var firstLine = File.ReadLines(path).First();
        Assert.Equal(LocalizationGenerator.Header, firstLine);
    }

    [Fact]
    public void Generator_writes_correct_row_values()
    {
        var fragments = new[] { FragWith("frag", "items", "myKey", english: "My Item", russian: "Мой предмет") };

        LocalizationGenerator.Generate(fragments, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length); // header + 1 row

        var row = lines[1];
        Assert.StartsWith("myKey,items,Item,,,My Item,,,,,,,,,,Мой предмет,,,", row);
    }

    [Fact]
    public void Generator_does_not_create_file_when_no_localization_entries()
    {
        var fragments = new[] { FragWithBody("frag", "items") };

        LocalizationGenerator.Generate(fragments, _tempDir, dryRun: false, _nullLogger);

        Assert.False(File.Exists(Path.Combine(_tempDir, "Config", "Localization.txt")));
    }

    [Fact]
    public void Generator_dry_run_does_not_create_file()
    {
        var fragments = new[] { FragWith("frag", "items", "k", english: "x") };

        LocalizationGenerator.Generate(fragments, _tempDir, dryRun: true, _nullLogger);

        Assert.False(File.Exists(Path.Combine(_tempDir, "Config", "Localization.txt")));
    }

    [Fact]
    public void Generator_output_is_utf8_without_bom()
    {
        var fragments = new[] { FragWith("frag", "items", "k", english: "hello") };

        LocalizationGenerator.Generate(fragments, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        var rawBytes = File.ReadAllBytes(path);

        Assert.False(rawBytes.Length >= 3
            && rawBytes[0] == 0xEF && rawBytes[1] == 0xBB && rawBytes[2] == 0xBF,
            "Localization.txt must not start with a UTF-8 BOM.");
    }

    [Fact]
    public void Generator_rows_are_written_in_resolved_fragment_order()
    {
        var fragments = new[]
        {
            FragWith("fragA", "items", "first", english: "First"),
            FragWith("fragB", "items", "second", english: "Second"),
        };

        LocalizationGenerator.Generate(fragments, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("first,", lines[1]);
        Assert.StartsWith("second,", lines[2]);
    }

    [Fact]
    public void Generator_csv_escapes_commas_in_values()
    {
        var fragments = new[] { FragWith("frag", "items", "k", english: "Hello, world") };

        LocalizationGenerator.Generate(fragments, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        var row = File.ReadAllLines(path)[1];
        Assert.Contains("\"Hello, world\"", row);
    }

    [Fact]
    public void Generator_csv_escapes_quotes_in_values()
    {
        var fragments = new[] { FragWith("frag", "items", "k", english: "Say \"hi\"") };

        LocalizationGenerator.Generate(fragments, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        var row = File.ReadAllLines(path)[1];
        Assert.Contains("\"Say \"\"hi\"\"\"", row);
    }

    // ── OutputGenerator integration with localization ─────────────────────────

    [Fact]
    public void Output_generator_writes_both_config_and_localization()
    {
        var fragments = new[]
        {
            FragWithLocAndBody("frag", "items",
                bodyXml: "<append xpath=\"/items\"/>",
                key: "myKey", english: "My Item"),
        };

        OutputGenerator.Generate(fragments, _tempDir, dryRun: false, clean: false, _nullLogger);

        Assert.True(File.Exists(Path.Combine(_tempDir, "Config", "items.xml")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "Config", "Localization.txt")));
    }

    [Fact]
    public void Output_generator_dry_run_does_not_write_localization()
    {
        var fragments = new[]
        {
            FragWithLocAndBody("frag", "items",
                bodyXml: "<append/>",
                key: "k", english: "x"),
        };

        OutputGenerator.Generate(fragments, _tempDir, dryRun: true, clean: false, _nullLogger);

        Assert.False(File.Exists(Path.Combine(_tempDir, "Config", "Localization.txt")));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string Write(string xml)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.frag.xml");
        File.WriteAllText(path, xml);
        return path;
    }

    private static Fragment FragWith(
        string id,
        string target,
        string key,
        string english = "",
        string russian = "") =>
        new(id, id, target, [],
            $"{id}.frag.xml",
            [],
            [new LocalizationEntry(
                Key: key,
                File: target,
                Type: "Item",
                UsedInMainMenu: string.Empty,
                NoTranslate: string.Empty,
                English: english,
                Context: string.Empty,
                German: string.Empty,
                Spanish: string.Empty,
                French: string.Empty,
                Italian: string.Empty,
                Japanese: string.Empty,
                Koreana: string.Empty,
                Polish: string.Empty,
                Brazilian: string.Empty,
                Russian: russian,
                Turkish: string.Empty,
                Schinese: string.Empty,
                Tchinese: string.Empty,
                SourceFile: $"{id}.frag.xml",
                ParentFragmentId: id)]);

    private static Fragment FragWithBody(string id, string target) =>
        new(id, id, target, [], $"{id}.frag.xml", [System.Xml.Linq.XElement.Parse("<append/>")], []);

    private static Fragment FragWithLocAndBody(
        string id,
        string target,
        string bodyXml,
        string key,
        string english) =>
        new(id, id, target, [],
            $"{id}.frag.xml",
            [System.Xml.Linq.XElement.Parse(bodyXml)],
            [new LocalizationEntry(
                Key: key,
                File: target,
                Type: "Item",
                UsedInMainMenu: string.Empty,
                NoTranslate: string.Empty,
                English: english,
                Context: string.Empty,
                German: string.Empty,
                Spanish: string.Empty,
                French: string.Empty,
                Italian: string.Empty,
                Japanese: string.Empty,
                Koreana: string.Empty,
                Polish: string.Empty,
                Brazilian: string.Empty,
                Russian: string.Empty,
                Turkish: string.Empty,
                Schinese: string.Empty,
                Tchinese: string.Empty,
                SourceFile: $"{id}.frag.xml",
                ParentFragmentId: id)]);
}
