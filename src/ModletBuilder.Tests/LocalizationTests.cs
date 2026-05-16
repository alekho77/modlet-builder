using System.Text;
using System.Xml.Linq;
using ModletBuilder.Core.Generation;
using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;
using ModletBuilder.Core.Parsing;
using ModletBuilder.Core.Resolution;
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

    // ── Parser: localization block extraction at modlet level ─────────────────

    [Fact]
    public void Localization_block_is_parsed_with_all_required_attributes()
    {
        var file = Write(@"
<modlet>
  <localization key=""myKey"" file=""items"" type=""Item"">
    <english text=""My Item""/>
  </localization>
  <fragment target=""items"">
    <append xpath=""/items""/>
  </fragment>
</modlet>");

        var (fragments, localizationEntries, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        Assert.Single(localizationEntries);
        var entry = localizationEntries[0];
        Assert.Equal("myKey", entry.Key);
        Assert.Equal("items", entry.File);
        Assert.Equal("Item", entry.Type);
        Assert.Equal("My Item", entry.English);
        Assert.Equal(string.Empty, entry.Russian);
    }

    [Fact]
    public void Fragment_body_is_pure_payload_with_no_localization_nodes()
    {
        var file = Write(@"
<modlet>
  <localization key=""k"" file=""items"" type=""Item""><english text=""x""/></localization>
  <fragment target=""items"">
    <append xpath=""/items""/>
  </fragment>
</modlet>");

        var (fragments, _, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        // Body must contain only <append> — localization is not a child of <fragment>.
        Assert.Single(fragments[0].Body);
        Assert.Equal("append", fragments[0].Body[0].Name.LocalName);
    }

    [Fact]
    public void Localization_inside_fragment_produces_error_with_guidance()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <localization key=""k"" file=""items"" type=""Item""><english text=""x""/></localization>
    <append/>
  </fragment>
</modlet>");

        var (_, _, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error
            && d.Message.Contains("<localization>")
            && d.Message.Contains("<modlet>"));
    }

    [Fact]
    public void Multiple_localization_blocks_at_modlet_level_are_all_parsed()
    {
        var file = Write(@"
<modlet>
  <localization key=""k1"" file=""items"" type=""Item""><english text=""A""/></localization>
  <localization key=""k2"" file=""items"" type=""Item""><english text=""B""/></localization>
  <fragment target=""items"">
    <append/>
  </fragment>
</modlet>");

        var (fragments, localizationEntries, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        Assert.Equal(2, localizationEntries.Count);
        Assert.Equal("k1", localizationEntries[0].Key);
        Assert.Equal("k2", localizationEntries[1].Key);
    }

    [Fact]
    public void Localization_block_missing_key_produces_error()
    {
        var file = Write(@"
<modlet>
  <localization file=""items"" type=""Item""><english text=""x""/></localization>
  <fragment target=""items""><append/></fragment>
</modlet>");

        var (_, _, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'key'"));
    }

    [Fact]
    public void Localization_block_without_file_is_parsed_with_empty_file()
    {
        var file = Write(@"
<modlet>
  <localization key=""k"" type=""Item""><english text=""x""/></localization>
  <fragment target=""items""><append/></fragment>
</modlet>");

        var (_, localizationEntries, diagnostics) = FragmentParser.Parse(file);

        Assert.DoesNotContain(diagnostics, d => d.Message.Contains("'file'"));
        Assert.Single(localizationEntries);
        Assert.Equal(string.Empty, localizationEntries[0].File);
    }

    [Fact]
    public void Localization_block_without_type_is_parsed_with_empty_type()
    {
        var file = Write(@"
<modlet>
  <localization key=""k"" file=""items""><english text=""x""/></localization>
  <fragment target=""items""><append/></fragment>
</modlet>");

        var (_, localizationEntries, diagnostics) = FragmentParser.Parse(file);

        Assert.DoesNotContain(diagnostics, d => d.Message.Contains("'type'"));
        Assert.Single(localizationEntries);
        Assert.Equal(string.Empty, localizationEntries[0].Type);
    }

    [Fact]
    public void Localization_block_unknown_attribute_produces_error()
    {
        var file = Write(@"
<modlet>
  <localization key=""k"" file=""items"" type=""Item"" extra=""x""><english text=""x""/></localization>
  <fragment target=""items""><append/></fragment>
</modlet>");

        var (_, _, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'extra'"));
    }

    [Fact]
    public void Localization_block_unknown_language_element_produces_error()
    {
        var file = Write(@"
<modlet>
  <localization key=""k"" file=""items"" type=""Item"">
    <elvish text=""Namárië""/>
  </localization>
  <fragment target=""items""><append/></fragment>
</modlet>");

        var (_, _, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("<elvish>"));
    }

    [Fact]
    public void Localization_block_duplicate_language_element_produces_error()
    {
        var file = Write(@"
<modlet>
  <localization key=""k"" file=""items"" type=""Item"">
    <english text=""first""/>
    <english text=""second""/>
  </localization>
  <fragment target=""items""><append/></fragment>
</modlet>");

        var (_, _, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("<english>"));
    }

    [Fact]
    public void Localization_block_language_element_missing_text_produces_error()
    {
        var file = Write(@"
<modlet>
  <localization key=""k"" file=""items"" type=""Item"">
    <english/>
  </localization>
  <fragment target=""items""><append/></fragment>
</modlet>");

        var (_, _, diagnostics) = FragmentParser.Parse(file);

        Assert.Contains(diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.Message.Contains("'text'"));
    }

    [Fact]
    public void Localization_block_optional_attributes_are_captured()
    {
        var file = Write(@"
<modlet>
  <localization key=""k"" file=""items"" type=""Item"" context=""hint"" usedInMainMenu=""True"" noTranslate=""False"">
    <english text=""x""/>
  </localization>
  <fragment target=""items""><append/></fragment>
</modlet>");

        var (_, localizationEntries, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        var entry = localizationEntries[0];
        Assert.Equal("hint", entry.Context);
        Assert.Equal("True", entry.UsedInMainMenu);
        Assert.Equal("False", entry.NoTranslate);
    }

    [Fact]
    public void Document_without_localization_blocks_returns_empty_localization_list()
    {
        var file = Write(@"
<modlet>
  <fragment target=""items"">
    <append xpath=""/items""/>
  </fragment>
</modlet>");

        var (fragments, localizationEntries, diagnostics) = FragmentParser.Parse(file);

        Assert.Empty(diagnostics);
        Assert.Single(fragments);
        Assert.Empty(localizationEntries);
    }

    // ── LocalizationAttributeResolver ─────────────────────────────────────────

    [Fact]
    public void Resolver_fills_file_and_type_for_item_from_items_fragment()
    {
        var entry = UnresolvedEntry("f.frag.xml", "myItemDesc");
        var fragment = FragWith("f.frag.xml",
            "<append xpath=\"/items\"><item name=\"myItem\"><property name=\"DescriptionKey\" value=\"myItemDesc\"/></item></append>");

        var resolved = LocalizationAttributeResolver.Resolve([entry], [fragment]);

        Assert.Single(resolved);
        Assert.Equal("items", resolved[0].File);
        Assert.Equal("Item", resolved[0].Type);
    }

    [Fact]
    public void Resolver_fills_file_and_type_for_block_from_blocks_fragment()
    {
        var entry = UnresolvedEntry("f.frag.xml", "myBlockDesc");
        var fragment = new Fragment(
            InternalId: "id:f",
            Name: null,
            Target: "blocks",
            Requires: [],
            SourceFile: "f.frag.xml",
            Body: [XElement.Parse("<append xpath=\"/blocks\"><block name=\"myBlock\"><property name=\"DescriptionKey\" value=\"myBlockDesc\"/></block></append>")]);

        var resolved = LocalizationAttributeResolver.Resolve([entry], [fragment]);

        Assert.Single(resolved);
        Assert.Equal("blocks", resolved[0].File);
        Assert.Equal("Block", resolved[0].Type);
    }

    [Fact]
    public void Resolver_fills_file_and_type_for_item_modifier_from_item_modifiers_fragment()
    {
        var entry = UnresolvedEntry("f.frag.xml", "myModDesc");
        var fragment = new Fragment(
            InternalId: "id:f",
            Name: null,
            Target: "item_modifiers",
            Requires: [],
            SourceFile: "f.frag.xml",
            Body: [XElement.Parse("<append xpath=\"/item_modifiers\"><item_modifier name=\"myMod\"><property name=\"DescriptionKey\" value=\"myModDesc\"/></item_modifier></append>")]);

        var resolved = LocalizationAttributeResolver.Resolve([entry], [fragment]);

        Assert.Single(resolved);
        Assert.Equal("item_modifiers", resolved[0].File);
        Assert.Equal("Mod", resolved[0].Type);
    }

    [Fact]
    public void Resolver_does_not_change_entry_with_explicit_file_and_type()
    {
        var entry = EntryWith("f.frag.xml", "myItemDesc");
        var fragment = FragWith("f.frag.xml",
            "<append xpath=\"/items\"><item name=\"myItem\"><property name=\"DescriptionKey\" value=\"myItemDesc\"/></item></append>");

        var resolved = LocalizationAttributeResolver.Resolve([entry], [fragment]);

        Assert.Single(resolved);
        Assert.Same(entry, resolved[0]);
    }

    [Fact]
    public void Resolver_leaves_entry_unchanged_when_key_not_found_in_any_fragment()
    {
        var entry = UnresolvedEntry("f.frag.xml", "orphanDesc");
        var fragment = FragWith("f.frag.xml",
            "<append xpath=\"/items\"><item name=\"x\"/></append>");

        var resolved = LocalizationAttributeResolver.Resolve([entry], [fragment]);

        Assert.Single(resolved);
        Assert.Equal(string.Empty, resolved[0].File);
        Assert.Equal(string.Empty, resolved[0].Type);
    }

    // ── Validator: orphaned localization key detection ────────────────────────

    [Fact]
    public void Validator_orphan_check_returns_no_errors_when_all_keys_are_referenced()
    {
        var entries = new[] { EntryWith("frag.frag.xml", "myItemDesc") };
        var fragments = new[]
        {
            FragWith("frag.frag.xml", "<append xpath=\"/items\"><item name=\"myItem\"><property name=\"DescriptionKey\" value=\"myItemDesc\"/></item></append>"),
        };

        var diagnostics = LocalizationValidator.ValidateOrphanedLocalizationKeys(entries, fragments);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validator_orphan_check_returns_error_for_key_not_in_any_description_key()
    {
        var entries = new[] { EntryWith("frag.frag.xml", "orphanKey") };
        var fragments = new[]
        {
            FragWith("frag.frag.xml", "<append xpath=\"/items\"><item name=\"myItem\"/></append>"),
        };

        var diagnostics = LocalizationValidator.ValidateOrphanedLocalizationKeys(entries, fragments);

        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
        Assert.Contains("orphanKey", diagnostics[0].Message);
        Assert.Contains("DescriptionKey", diagnostics[0].Message);
    }

    [Fact]
    public void Validator_orphan_check_returns_no_errors_for_empty_entry_list()
    {
        var fragments = new[]
        {
            FragWith("frag.frag.xml", "<append xpath=\"/items\"/>"),
        };

        var diagnostics = LocalizationValidator.ValidateOrphanedLocalizationKeys([], fragments);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validator_orphan_check_reports_each_orphaned_entry_separately()
    {
        var entries = new[]
        {
            EntryWith("frag.frag.xml", "orphanA"),
            EntryWith("frag.frag.xml", "validDesc"),
            EntryWith("frag.frag.xml", "orphanB"),
        };
        var fragments = new[]
        {
            FragWith("frag.frag.xml", "<append><item name=\"x\"><property name=\"DescriptionKey\" value=\"validDesc\"/></item></append>"),
        };

        var diagnostics = LocalizationValidator.ValidateOrphanedLocalizationKeys(entries, fragments);

        Assert.Equal(2, diagnostics.Count);
        Assert.Contains(diagnostics, d => d.Message.Contains("orphanA"));
        Assert.Contains(diagnostics, d => d.Message.Contains("orphanB"));
    }

    [Fact]
    public void Validator_orphan_check_scans_nested_elements_in_fragment_body()
    {
        var entries = new[] { EntryWith("frag.frag.xml", "deepDesc") };
        var fragments = new[]
        {
            FragWith("frag.frag.xml", "<append xpath=\"/items\"><item name=\"x\"><properties><property name=\"DescriptionKey\" value=\"deepDesc\"/></properties></item></append>"),
        };

        var diagnostics = LocalizationValidator.ValidateOrphanedLocalizationKeys(entries, fragments);

        Assert.Empty(diagnostics);
    }

    // ── Validator: duplicate key detection ────────────────────────────────────

    [Fact]
    public void Validator_returns_no_errors_for_unique_keys()
    {
        var entries = new[]
        {
            EntryWith("fragA.frag.xml", "k1"),
            EntryWith("fragB.frag.xml", "k2"),
        };

        var diagnostics = LocalizationValidator.Validate(entries);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validator_returns_error_for_duplicate_key()
    {
        var entries = new[]
        {
            EntryWith("fragA.frag.xml", "dupKey"),
            EntryWith("fragB.frag.xml", "dupKey"),
        };

        var diagnostics = LocalizationValidator.Validate(entries);

        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
        Assert.Contains("dupKey", diagnostics[0].Message);
    }

    [Fact]
    public void Validator_identifies_first_occurrence_source_file_in_error_message()
    {
        var entries = new[]
        {
            EntryWith("fragA.frag.xml", "shared"),
            EntryWith("fragB.frag.xml", "shared"),
        };

        var diagnostics = LocalizationValidator.Validate(entries);

        Assert.Contains("fragA.frag.xml", diagnostics[0].Message);
    }

    [Fact]
    public void Validator_reports_each_duplicate_occurrence_separately()
    {
        var entries = new[]
        {
            EntryWith("fragA.frag.xml", "key"),
            EntryWith("fragB.frag.xml", "key"),
            EntryWith("fragC.frag.xml", "key"),
        };

        var diagnostics = LocalizationValidator.Validate(entries);

        Assert.Equal(2, diagnostics.Count);
    }

    // ── LocalizationGenerator: CSV output ────────────────────────────────────

    [Fact]
    public void Generator_writes_localization_txt_with_correct_header()
    {
        var entries = new[] { EntryWith("frag.xml", "myKey", english: "My Item") };

        LocalizationGenerator.Generate(entries, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        Assert.True(File.Exists(path));

        var firstLine = File.ReadLines(path).First();
        Assert.Equal(LocalizationGenerator.Header, firstLine);
    }

    [Fact]
    public void Generator_writes_correct_row_values()
    {
        var entries = new[] { EntryWith("frag.xml", "myKey", english: "My Item", russian: "Мой предмет") };

        LocalizationGenerator.Generate(entries, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length); // header + 1 row

        var row = lines[1];
        Assert.StartsWith("myKey,items,Item,,,My Item,,,,,,,,,,Мой предмет,,,", row);
    }

    [Fact]
    public void Generator_does_not_create_file_when_no_localization_entries()
    {
        LocalizationGenerator.Generate([], _tempDir, dryRun: false, _nullLogger);

        Assert.False(File.Exists(Path.Combine(_tempDir, "Config", "Localization.txt")));
    }

    [Fact]
    public void Generator_dry_run_does_not_create_file()
    {
        var entries = new[] { EntryWith("frag.xml", "k", english: "x") };

        LocalizationGenerator.Generate(entries, _tempDir, dryRun: true, _nullLogger);

        Assert.False(File.Exists(Path.Combine(_tempDir, "Config", "Localization.txt")));
    }

    [Fact]
    public void Generator_output_is_utf8_without_bom()
    {
        var entries = new[] { EntryWith("frag.xml", "k", english: "hello") };

        LocalizationGenerator.Generate(entries, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        var rawBytes = File.ReadAllBytes(path);

        Assert.False(rawBytes.Length >= 3
            && rawBytes[0] == 0xEF && rawBytes[1] == 0xBB && rawBytes[2] == 0xBF,
            "Localization.txt must not start with a UTF-8 BOM.");
    }

    [Fact]
    public void Generator_rows_are_written_in_input_order()
    {
        var entries = new[]
        {
            EntryWith("a.frag.xml", "first", english: "First"),
            EntryWith("b.frag.xml", "second", english: "Second"),
        };

        LocalizationGenerator.Generate(entries, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("first,", lines[1]);
        Assert.StartsWith("second,", lines[2]);
    }

    [Fact]
    public void Generator_csv_escapes_commas_in_values()
    {
        var entries = new[] { EntryWith("frag.xml", "k", english: "Hello, world") };

        LocalizationGenerator.Generate(entries, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        var row = File.ReadAllLines(path)[1];
        Assert.Contains("\"Hello, world\"", row);
    }

    [Fact]
    public void Generator_csv_escapes_quotes_in_values()
    {
        var entries = new[] { EntryWith("frag.xml", "k", english: "Say \"hi\"") };

        LocalizationGenerator.Generate(entries, _tempDir, dryRun: false, _nullLogger);

        var path = Path.Combine(_tempDir, "Config", "Localization.txt");
        var row = File.ReadAllLines(path)[1];
        Assert.Contains("\"Say \"\"hi\"\"\"", row);
    }

    // ── OutputGenerator integration with localization ─────────────────────────

    [Fact]
    public void Output_generator_writes_both_config_and_localization()
    {
        var fragment = new Fragment("id:frag", "frag", "items", [], "frag.frag.xml",
            [XElement.Parse("<append xpath=\"/items\"/>")]);
        var entries = new[] { EntryWith("frag.frag.xml", "myKey", english: "My Item") };

        OutputGenerator.Generate([fragment], entries, _tempDir, dryRun: false, clean: false, _nullLogger);

        Assert.True(File.Exists(Path.Combine(_tempDir, "Config", "items.xml")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "Config", "Localization.txt")));
    }

    [Fact]
    public void Output_generator_dry_run_does_not_write_localization()
    {
        var fragment = new Fragment("id:frag", "frag", "items", [], "frag.frag.xml",
            [XElement.Parse("<append/>")]);
        var entries = new[] { EntryWith("frag.frag.xml", "k", english: "x") };

        OutputGenerator.Generate([fragment], entries, _tempDir, dryRun: true, clean: false, _nullLogger);

        Assert.False(File.Exists(Path.Combine(_tempDir, "Config", "Localization.txt")));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string Write(string xml)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.frag.xml");
        File.WriteAllText(path, xml);
        return path;
    }

    private static LocalizationEntry EntryWith(
        string sourceFile,
        string key,
        string english = "",
        string russian = "") =>
        new(
            Key: key,
            File: "items",
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
            SourceFile: sourceFile);

    /// <summary>Helper that creates a <see cref="LocalizationEntry"/> with empty File and Type,
    /// simulating a source localization block that omitted those attributes.</summary>
    private static LocalizationEntry UnresolvedEntry(string sourceFile, string key) =>
        new(
            Key: key,
            File: string.Empty,
            Type: string.Empty,
            UsedInMainMenu: string.Empty,
            NoTranslate: string.Empty,
            English: string.Empty,
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
            SourceFile: sourceFile);

    private static Fragment FragWith(string sourceFile, string bodyXml) =>
        new(
            InternalId: $"id:{sourceFile}",
            Name: null,
            Target: "items",
            Requires: [],
            SourceFile: sourceFile,
            Body: [XElement.Parse(bodyXml)]);
}
