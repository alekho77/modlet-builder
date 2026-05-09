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
        var build = Build("mymod", Frag("a", "items", "<append xpath=\"/items\"><item name=\"x\"/></append>"));
        var diagnostics = OutputGenerator.Generate([build], _tempDir, dryRun: false, clean: false, _nullLogger);

        Assert.Empty(diagnostics);

        var outputFile = Path.Combine(_tempDir, "mymod", "Config", "items.xml");
        Assert.True(File.Exists(outputFile));

        var doc = XDocument.Load(outputFile);
        Assert.Equal("config", doc.Root!.Name.LocalName);
        Assert.Single(doc.Root.Elements("append"));
    }

    [Fact]
    public void Output_file_starts_with_xml_declaration_with_uppercase_encoding()
    {
        var build = Build("mymod", Frag("a", "items", "<append/>"));
        OutputGenerator.Generate([build], _tempDir, dryRun: false, clean: false, _nullLogger);

        var outputFile = Path.Combine(_tempDir, "mymod", "Config", "items.xml");
        var rawBytes = File.ReadAllBytes(outputFile);
        var firstLine = Encoding.UTF8.GetString(rawBytes, 0, Math.Min(rawBytes.Length, 60));

        Assert.Contains("encoding=\"UTF-8\"", firstLine);
    }

    [Fact]
    public void Output_file_has_no_byte_order_mark()
    {
        var build = Build("mymod", Frag("a", "items", "<append/>"));
        OutputGenerator.Generate([build], _tempDir, dryRun: false, clean: false, _nullLogger);

        var outputFile = Path.Combine(_tempDir, "mymod", "Config", "items.xml");
        var rawBytes = File.ReadAllBytes(outputFile);

        // UTF-8 BOM is EF BB BF
        Assert.False(rawBytes.Length >= 3
            && rawBytes[0] == 0xEF && rawBytes[1] == 0xBB && rawBytes[2] == 0xBF,
            "Output file must not start with a UTF-8 BOM.");
    }

    [Fact]
    public void Fragments_for_same_target_are_merged_in_order()
    {
        var build = Build("mymod",
            Frag("a", "items", "<append id=\"1\"/>"),
            Frag("b", "items", "<append id=\"2\"/>"));

        OutputGenerator.Generate([build], _tempDir, dryRun: false, clean: false, _nullLogger);

        var doc = XDocument.Load(Path.Combine(_tempDir, "mymod", "Config", "items.xml"));
        var appends = doc.Root!.Elements("append").ToList();
        Assert.Equal(2, appends.Count);
        Assert.Equal("1", appends[0].Attribute("id")?.Value);
        Assert.Equal("2", appends[1].Attribute("id")?.Value);
    }

    [Fact]
    public void Multiple_targets_produce_separate_files()
    {
        var build = Build("mymod",
            Frag("a", "items", "<append/>"),
            Frag("b", "recipes", "<append/>"));

        OutputGenerator.Generate([build], _tempDir, dryRun: false, clean: false, _nullLogger);

        Assert.True(File.Exists(Path.Combine(_tempDir, "mymod", "Config", "items.xml")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "mymod", "Config", "recipes.xml")));
    }

    [Fact]
    public void Multiple_mods_produce_separate_mod_directories()
    {
        var modA = Build("ModA", Frag("a", "items", "<append/>"));
        var modB = Build("ModB", Frag("b", "recipes", "<append/>"));

        OutputGenerator.Generate([modA, modB], _tempDir, dryRun: false, clean: false, _nullLogger);

        Assert.True(File.Exists(Path.Combine(_tempDir, "ModA", "Config", "items.xml")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "ModB", "Config", "recipes.xml")));
    }

    [Fact]
    public void XUi_target_produces_file_in_subdirectory()
    {
        var build = Build("mymod", Frag("a", "xui_windows", "<window name=\"x\"/>"));

        OutputGenerator.Generate([build], _tempDir, dryRun: false, clean: false, _nullLogger);

        var expected = Path.Combine(_tempDir, "mymod", "Config", "XUi", "windows.xml");
        Assert.True(File.Exists(expected));
    }

    [Fact]
    public void Dry_run_does_not_write_any_files()
    {
        var build = Build("mymod", Frag("a", "items", "<append/>"));

        OutputGenerator.Generate([build], _tempDir, dryRun: true, clean: false, _nullLogger);

        var modDir = Path.Combine(_tempDir, "mymod");
        Assert.False(Directory.Exists(modDir));
    }

    [Fact]
    public void Dry_run_with_nonexistent_output_dir_produces_no_errors()
    {
        var missing = Path.Combine(_tempDir, "does_not_exist");
        var build = Build("mymod", Frag("a", "items", "<append/>"));

        var diagnostics = OutputGenerator.Generate([build], missing, dryRun: true, clean: false, _nullLogger);

        // Dry run must never fail due to a missing output directory — it should only warn.
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.False(Directory.Exists(missing));
    }

    [Fact]
    public void Dry_run_does_not_create_the_output_directory()
    {
        var missing = Path.Combine(_tempDir, "would_be_created");
        var build = Build("mymod", Frag("a", "items", "<append/>"));

        OutputGenerator.Generate([build], missing, dryRun: true, clean: false, _nullLogger);

        Assert.False(Directory.Exists(missing));
    }

    [Fact]
    public void Mod_directory_is_created_automatically()
    {
        var build = Build("mymod", Frag("a", "items", "<append/>"));

        OutputGenerator.Generate([build], _tempDir, dryRun: false, clean: false, _nullLogger);

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "mymod", "Config")));
    }

    [Fact]
    public void Clean_deletes_existing_output_directory_before_building()
    {
        // Pre-populate a file that should not survive clean.
        var staleDir = Path.Combine(_tempDir, "OldMod");
        Directory.CreateDirectory(staleDir);
        File.WriteAllText(Path.Combine(staleDir, "stale.txt"), "old");

        var build = Build("NewMod", Frag("a", "items", "<append/>"));

        OutputGenerator.Generate([build], _tempDir, dryRun: false, clean: true, _nullLogger);

        Assert.False(File.Exists(Path.Combine(staleDir, "stale.txt")), "Stale file must be removed by --clean.");
        Assert.True(File.Exists(Path.Combine(_tempDir, "NewMod", "Config", "items.xml")));
    }

    [Fact]
    public void Clean_with_dry_run_does_not_delete_anything()
    {
        var staleDir = Path.Combine(_tempDir, "OldMod");
        Directory.CreateDirectory(staleDir);
        File.WriteAllText(Path.Combine(staleDir, "stale.txt"), "old");

        var build = Build("NewMod", Frag("a", "items", "<append/>"));

        OutputGenerator.Generate([build], _tempDir, dryRun: true, clean: true, _nullLogger);

        Assert.True(File.Exists(Path.Combine(staleDir, "stale.txt")),
            "Dry run with --clean must not delete files.");
    }

    private static ModBuild Build(string modName, params Fragment[] fragments) =>
        new(modName, fragments);

    private static Fragment Frag(string name, string target, string bodyXml = "<append/>") =>
        new(name, target, [], $"{name}.frag.xml", [XElement.Parse(bodyXml)]);
}

