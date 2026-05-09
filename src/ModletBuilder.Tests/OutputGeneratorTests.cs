using System.Text;
using System.Xml.Linq;
using ModletBuilder.Core.Generation;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Tests;

public class OutputGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public OutputGeneratorTests()
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
    public void Generates_config_file_with_correct_structure()
    {
        var fragment = Frag("a", "items", "<append xpath=\"/items\"><item name=\"x\"/></append>");
        var diagnostics = OutputGenerator.Generate([fragment], _tempDir, dryRun: false);

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
        var fragment = Frag("a", "items", "<append/>");
        OutputGenerator.Generate([fragment], _tempDir, dryRun: false);

        var outputFile = Path.Combine(_tempDir, "Config", "items.xml");
        var rawBytes = File.ReadAllBytes(outputFile);
        var firstLine = Encoding.UTF8.GetString(rawBytes, 0, Math.Min(rawBytes.Length, 60));

        Assert.Contains("encoding=\"UTF-8\"", firstLine);
    }

    [Fact]
    public void Output_file_has_no_byte_order_mark()
    {
        var fragment = Frag("a", "items", "<append/>");
        OutputGenerator.Generate([fragment], _tempDir, dryRun: false);

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
        var a = Frag("a", "items", "<append id=\"1\"/>");
        var b = Frag("b", "items", "<append id=\"2\"/>");

        OutputGenerator.Generate([a, b], _tempDir, dryRun: false);

        var doc = XDocument.Load(Path.Combine(_tempDir, "Config", "items.xml"));
        var appends = doc.Root!.Elements("append").ToList();
        Assert.Equal(2, appends.Count);
        Assert.Equal("1", appends[0].Attribute("id")?.Value);
        Assert.Equal("2", appends[1].Attribute("id")?.Value);
    }

    [Fact]
    public void Multiple_targets_produce_separate_files()
    {
        var a = Frag("a", "items", "<append/>");
        var b = Frag("b", "recipes", "<append/>");

        OutputGenerator.Generate([a, b], _tempDir, dryRun: false);

        Assert.True(File.Exists(Path.Combine(_tempDir, "Config", "items.xml")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "Config", "recipes.xml")));
    }

    [Fact]
    public void XUi_target_produces_file_in_subdirectory()
    {
        var fragment = Frag("a", "xui_windows", "<window name=\"x\"/>");

        OutputGenerator.Generate([fragment], _tempDir, dryRun: false);

        var expected = Path.Combine(_tempDir, "Config", "XUi", "windows.xml");
        Assert.True(File.Exists(expected));
    }

    [Fact]
    public void Dry_run_does_not_write_any_files()
    {
        var fragment = Frag("a", "items", "<append/>");

        OutputGenerator.Generate([fragment], _tempDir, dryRun: true);

        var configDir = Path.Combine(_tempDir, "Config");
        Assert.False(Directory.Exists(configDir) && Directory.EnumerateFiles(configDir).Any());
    }

    [Fact]
    public void Dry_run_with_nonexistent_output_dir_produces_error()
    {
        var missing = Path.Combine(_tempDir, "does_not_exist");

        var diagnostics = OutputGenerator.Generate([], missing, dryRun: true);

        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Config_subdirectory_is_created_automatically()
    {
        var fragment = Frag("a", "items", "<append/>");

        OutputGenerator.Generate([fragment], _tempDir, dryRun: false);

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "Config")));
    }

    private static Fragment Frag(string name, string target, string bodyXml) =>
        new(name, target, [], $"{name}.frag.xml",
            [XElement.Parse(bodyXml)]);
}
