using System.Text;
using System.Xml.Linq;
using ModletBuilder.Core.Generation;
using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Tests;

public class OutputGeneratorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly BuildLogger _nullLogger;

    public OutputGeneratorTests()
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
}

