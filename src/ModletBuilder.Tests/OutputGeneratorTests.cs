using System.Text;
using System.Xml.Linq;
using ModletBuilder.Core.Generation;
using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Tests;

public class OutputGeneratorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sourceDir;
    private readonly BuildLogger _nullLogger;

    public OutputGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _sourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(_sourceDir);
        _nullLogger = new BuildLogger(VerbosityLevel.None, TextWriter.Null, TextWriter.Null);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        if (Directory.Exists(_sourceDir))
            Directory.Delete(_sourceDir, recursive: true);
    }

    [Fact]
    public void Generates_config_file_with_correct_structure()
    {
        var diagnostics = OutputGenerator.Generate(
            [Frag("a", "items", "<append xpath=\"/items\"><item name=\"x\"/></append>")],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        Assert.Empty(diagnostics);

        var outputFile = Path.Combine(_tempDir, "Config", "items.xml");
        Assert.True(File.Exists(outputFile));

        var doc = XDocument.Load(outputFile);
        Assert.Equal("config", doc.Root!.Name.LocalName);
        Assert.Single(doc.Root.Elements("append"));
    }

    [Fact]
    public void Output_file_starts_with_xml_declaration_with_uppercase_encoding()
    {
        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        var outputFile = Path.Combine(_tempDir, "Config", "items.xml");
        var rawBytes = File.ReadAllBytes(outputFile);
        var firstLine = Encoding.UTF8.GetString(rawBytes, 0, Math.Min(rawBytes.Length, 60));

        Assert.Contains("encoding=\"UTF-8\"", firstLine);
    }

    [Fact]
    public void Output_file_has_no_byte_order_mark()
    {
        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        var outputFile = Path.Combine(_tempDir, "Config", "items.xml");
        var rawBytes = File.ReadAllBytes(outputFile);

        // UTF-8 BOM is EF BB BF
        Assert.False(rawBytes.Length >= 3
            && rawBytes[0] == 0xEF && rawBytes[1] == 0xBB && rawBytes[2] == 0xBF,
            "Output file must not start with a UTF-8 BOM.");
    }

    [Fact]
    public void Generates_modinfo_file_with_expected_structure()
    {
        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [],
            SampleModInfo(),
            _tempDir,
            dryRun: false,
            clean: false,
            _nullLogger);

        var outputFile = Path.Combine(_tempDir, "ModInfo.xml");
        Assert.True(File.Exists(outputFile));

        var doc = XDocument.Load(outputFile);
        Assert.Equal("ModInfo", doc.Root!.Name.LocalName);
        Assert.Equal(
            ["Name", "DisplayName", "Description", "Author", "Version", "Website"],
            doc.Root.Elements().Select(e => e.Name.LocalName).ToArray());
        Assert.Equal("EV_LootBox", doc.Root.Element("Name")?.Attribute("value")?.Value);
        Assert.Equal("Loot Box", doc.Root.Element("DisplayName")?.Attribute("value")?.Value);
    }

    [Fact]
    public void Modinfo_file_starts_with_xml_declaration_with_uppercase_encoding()
    {
        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [],
            SampleModInfo(),
            _tempDir,
            dryRun: false,
            clean: false,
            _nullLogger);

        var rawBytes = File.ReadAllBytes(Path.Combine(_tempDir, "ModInfo.xml"));
        var firstLine = Encoding.UTF8.GetString(rawBytes, 0, Math.Min(rawBytes.Length, 60));

        Assert.Contains("encoding=\"UTF-8\"", firstLine);
    }

    [Fact]
    public void Modinfo_file_has_no_byte_order_mark()
    {
        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [],
            SampleModInfo(),
            _tempDir,
            dryRun: false,
            clean: false,
            _nullLogger);

        var rawBytes = File.ReadAllBytes(Path.Combine(_tempDir, "ModInfo.xml"));

        Assert.False(rawBytes.Length >= 3
            && rawBytes[0] == 0xEF && rawBytes[1] == 0xBB && rawBytes[2] == 0xBF,
            "ModInfo.xml must not start with a UTF-8 BOM.");
    }

    [Fact]
    public void Modinfo_values_are_xml_escaped()
    {
        var modInfo = SampleModInfo() with
        {
            DisplayName = "Loot & \"Box\"",
            Description = "Adds <rare> loot.",
        };

        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [],
            modInfo,
            _tempDir,
            dryRun: false,
            clean: false,
            _nullLogger);

        var raw = File.ReadAllText(Path.Combine(_tempDir, "ModInfo.xml"));
        Assert.Contains("Loot &amp; &quot;Box&quot;", raw, StringComparison.Ordinal);
        Assert.Contains("Adds &lt;rare&gt; loot.", raw, StringComparison.Ordinal);

        var doc = XDocument.Load(Path.Combine(_tempDir, "ModInfo.xml"));
        Assert.Equal("Loot & \"Box\"", doc.Root!.Element("DisplayName")?.Attribute("value")?.Value);
    }

    [Fact]
    public void Fragments_for_same_target_are_merged_in_order()
    {
        OutputGenerator.Generate(
            [Frag("a", "items", "<append id=\"1\"/>"), Frag("b", "items", "<append id=\"2\"/>")],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        var doc = XDocument.Load(Path.Combine(_tempDir, "Config", "items.xml"));
        var appends = doc.Root!.Elements("append").ToList();
        Assert.Equal(2, appends.Count);
        Assert.Equal("1", appends[0].Attribute("id")?.Value);
        Assert.Equal("2", appends[1].Attribute("id")?.Value);
    }

    [Fact]
    public void Multiple_targets_produce_separate_files()
    {
        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>"), Frag("b", "recipes", "<append/>")],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        Assert.True(File.Exists(Path.Combine(_tempDir, "Config", "items.xml")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "Config", "recipes.xml")));
    }

    [Fact]
    public void XUi_target_produces_file_in_subdirectory()
    {
        OutputGenerator.Generate(
            [Frag("a", "xui_windows", "<window name=\"x\"/>")],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        var expected = Path.Combine(_tempDir, "Config", "XUi", "windows.xml");
        Assert.True(File.Exists(expected));
    }

    [Fact]
    public void Dry_run_does_not_write_any_files()
    {
        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [], _tempDir, dryRun: true, clean: false, _nullLogger);

        Assert.False(Directory.Exists(Path.Combine(_tempDir, "Config")));
    }

    [Fact]
    public void Dry_run_does_not_write_modinfo()
    {
        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [],
            SampleModInfo(),
            _tempDir,
            dryRun: true,
            clean: false,
            _nullLogger);

        Assert.False(File.Exists(Path.Combine(_tempDir, "ModInfo.xml")));
    }

    [Fact]
    public void Generates_readme_and_nexus_description_files()
    {
        var readme = WriteReadme("source-description.md", "# Loot Box\r\n\r\nAdds loot.\r\n");
        var converter = new TestMarkdownToBbCodeConverter(fixedOutput: "[b]Loot Box[/b]\n");

        var diagnostics = OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [],
            SampleModInfo(),
            readme,
            converter,
            _tempDir,
            dryRun: false,
            clean: false,
            _nullLogger);

        Assert.Empty(diagnostics);
        Assert.Equal(File.ReadAllBytes(readme.Path), File.ReadAllBytes(Path.Combine(_tempDir, "README.md")));
        Assert.Equal("[b]Loot Box[/b]\n", File.ReadAllText(Path.Combine(_tempDir, "NEXUS_DESCRIPTION.bbcode")));
        Assert.True(converter.WasCalled);
        Assert.Equal(readme.Path, converter.MarkdownPath);
        Assert.Equal(Path.Combine(_tempDir, "NEXUS_DESCRIPTION.bbcode"), converter.OutputPath);
    }

    [Fact]
    public void Dry_run_does_not_write_readme_or_call_converter()
    {
        var readme = WriteReadme("source-description.md", "# Loot Box\n");
        var converter = new TestMarkdownToBbCodeConverter();

        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [],
            SampleModInfo(),
            readme,
            converter,
            _tempDir,
            dryRun: true,
            clean: false,
            _nullLogger);

        Assert.False(File.Exists(Path.Combine(_tempDir, "README.md")));
        Assert.False(File.Exists(Path.Combine(_tempDir, "NEXUS_DESCRIPTION.bbcode")));
        Assert.False(converter.WasCalled);
    }

    [Fact]
    public void Converter_failure_returns_error()
    {
        var readme = WriteReadme("source-description.md", "# Loot Box\n");
        var converter = new TestMarkdownToBbCodeConverter(
            [
                new Diagnostic(
                    DiagnosticSeverity.Error,
                    "conversion failed",
                    readme.Path)
            ]);

        var diagnostics = OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [],
            SampleModInfo(),
            readme,
            converter,
            _tempDir,
            dryRun: false,
            clean: false,
            _nullLogger);

        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error
            && d.Message.Contains("conversion failed", StringComparison.Ordinal));
        Assert.False(File.Exists(Path.Combine(_tempDir, "NEXUS_DESCRIPTION.bbcode")));
    }

    [Fact]
    public void Missing_converter_output_returns_error()
    {
        var readme = WriteReadme("source-description.md", "# Loot Box\n");
        var converter = new TestMarkdownToBbCodeConverter(writeOutput: false);

        var diagnostics = OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [],
            SampleModInfo(),
            readme,
            converter,
            _tempDir,
            dryRun: false,
            clean: false,
            _nullLogger);

        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error
            && d.Message.Contains("did not create", StringComparison.Ordinal));
    }

    [Fact]
    public void Dry_run_with_nonexistent_output_dir_produces_no_errors()
    {
        var missing = Path.Combine(_tempDir, "does_not_exist");

        var diagnostics = OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [], missing, dryRun: true, clean: false, _nullLogger);

        // Dry run must never fail due to a missing output directory — it should only warn.
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.False(Directory.Exists(missing));
    }

    [Fact]
    public void Dry_run_does_not_create_the_output_directory()
    {
        var missing = Path.Combine(_tempDir, "would_be_created");

        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [], missing, dryRun: true, clean: false, _nullLogger);

        Assert.False(Directory.Exists(missing));
    }

    [Fact]
    public void Config_directory_is_created_automatically()
    {
        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "Config")));
    }

    [Fact]
    public void Clean_deletes_existing_output_directory_before_building()
    {
        // Pre-populate a file that should not survive clean.
        File.WriteAllText(Path.Combine(_tempDir, "stale.txt"), "old");

        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [], _tempDir, dryRun: false, clean: true, _nullLogger);

        Assert.False(File.Exists(Path.Combine(_tempDir, "stale.txt")), "Stale file must be removed by --clean.");
        Assert.True(File.Exists(Path.Combine(_tempDir, "Config", "items.xml")));
    }

    [Fact]
    public void Clean_removes_stale_readme_and_nexus_files()
    {
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "stale");
        File.WriteAllText(Path.Combine(_tempDir, "NEXUS_DESCRIPTION.bbcode"), "stale");

        var readme = WriteReadme("source-description.md", "# Loot Box\n");

        var diagnostics = OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [],
            SampleModInfo(),
            readme,
            new TestMarkdownToBbCodeConverter(fixedOutput: "[b]fresh[/b]\n"),
            _tempDir,
            dryRun: false,
            clean: true,
            _nullLogger);

        Assert.Empty(diagnostics);
        Assert.Equal("# Loot Box\n", File.ReadAllText(Path.Combine(_tempDir, "README.md")));
        Assert.Equal("[b]fresh[/b]\n", File.ReadAllText(Path.Combine(_tempDir, "NEXUS_DESCRIPTION.bbcode")));
    }

    [Fact]
    public void Clean_with_dry_run_does_not_delete_anything()
    {
        File.WriteAllText(Path.Combine(_tempDir, "stale.txt"), "old");

        OutputGenerator.Generate(
            [Frag("a", "items", "<append/>")],
            [], _tempDir, dryRun: true, clean: true, _nullLogger);

        Assert.True(File.Exists(Path.Combine(_tempDir, "stale.txt")),
            "Dry run with --clean must not delete files.");
    }

    [Fact]
    public void Rebuild_overwrites_existing_output_file_without_error()
    {
        // First build.
        OutputGenerator.Generate(
            [Frag("a", "items", "<append id=\"1\"/>")],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        // Second build with different content — must overwrite silently.
        var diagnostics = OutputGenerator.Generate(
            [Frag("a", "items", "<append id=\"2\"/>")],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        Assert.Empty(diagnostics);
        var doc = XDocument.Load(Path.Combine(_tempDir, "Config", "items.xml"));
        Assert.Equal("2", doc.Root!.Elements("append").Single().Attribute("id")?.Value);
    }

    [Fact]
    public void Three_distinct_targets_produce_three_output_files()
    {
        OutputGenerator.Generate(
            [
                Frag("a", "items", "<append/>"),
                Frag("b", "recipes", "<append/>"),
                Frag("c", "progression", "<set xpath=\"/x\">1</set>")
            ],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        var xmlFiles = Directory.GetFiles(
            Path.Combine(_tempDir, "Config"), "*.xml", SearchOption.TopDirectoryOnly);
        Assert.Equal(3, xmlFiles.Length);
    }

    [Fact]
    public void XUi_Menu_target_produces_file_in_XUi_Menu_subdirectory()
    {
        OutputGenerator.Generate(
            [Frag("a", "xui_menu_windows", "<window name=\"x\"/>")],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        var expected = Path.Combine(_tempDir, "Config", "XUi_Menu", "windows.xml");
        Assert.True(File.Exists(expected));
    }

    [Fact]
    public void Unnamed_fragment_contributes_body_to_output()
    {
        OutputGenerator.Generate(
            [Frag(null, "items", "<append xpath=\"/items\"><item name=\"x\"/></append>", "source/items.frag.xml#L1#F0")],
            [], _tempDir, dryRun: false, clean: false, _nullLogger);

        var doc = XDocument.Load(Path.Combine(_tempDir, "Config", "items.xml"));

        Assert.Single(doc.Root!.Elements("append"));
        Assert.Equal("x", doc.Root.Element("append")?.Element("item")?.Attribute("name")?.Value);
    }

    private static Fragment Frag(string name, string target, string bodyXml = "<append/>") =>
        Frag(name, target, bodyXml, internalId: $"id:{name}");

    private static Fragment Frag(string? name, string target, string bodyXml, string internalId) =>
        new(internalId, name, target, [], $"{name ?? internalId}.frag.xml", [XElement.Parse(bodyXml)]);

    private static ModInfo SampleModInfo() => new(
        Name: "EV_LootBox",
        DisplayName: "Loot Box",
        Description: "Adds a loot box with Simple, Good, and Valuable reward categories.",
        Author: "Aleksei Khozin",
        Version: "0.1.0",
        Website: "https://github.com/alekho77/epic_7d2d_mods");

    private ReadmeSource WriteReadme(string fileName, string content)
    {
        var path = Path.Combine(_sourceDir, fileName);
        File.WriteAllText(path, content);
        return new ReadmeSource(path);
    }
}

