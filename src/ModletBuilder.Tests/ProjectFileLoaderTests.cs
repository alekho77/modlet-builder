using ModletBuilder.Cli;

namespace ModletBuilder.Tests;

public sealed class ProjectFileLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectFileLoaderTests()
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
    public void Valid_project_yaml_loads_metadata_and_mixed_sources()
    {
        var projectFile = WriteProject("""
modFolder: EV_LootBox
output: dist
modInfo:
  name: EV_LootBox
  displayName: Loot Box
  description: Adds a loot box.
  author: Aleksei Khozin
  version: 0.1.0
  website: https://github.com/alekho77/epic_7d2d_mods
sources:
  - path: src
    recursive: true
  - path: shared/common.frag.xml
  - shared/non-recursive-dir
""");

        var (project, diagnostics) = ProjectFileLoader.Load(projectFile);

        Assert.Empty(diagnostics);
        Assert.NotNull(project);
        Assert.Equal("EV_LootBox", project.ModFolder);
        Assert.Equal(Path.Combine(_tempDir, "dist"), project.OutputRoot);
        Assert.Equal("Loot Box", project.ModInfo.DisplayName);
        Assert.Collection(project.Sources,
            source =>
            {
                Assert.Equal(Path.Combine(_tempDir, "src"), source.Path);
                Assert.True(source.Recursive);
            },
            source =>
            {
                Assert.Equal(Path.Combine(_tempDir, "shared", "common.frag.xml"), source.Path);
                Assert.False(source.Recursive);
            },
            source =>
            {
                Assert.Equal(Path.Combine(_tempDir, "shared", "non-recursive-dir"), source.Path);
                Assert.False(source.Recursive);
            });
    }

    [Theory]
    [InlineData("modFolder")]
    [InlineData("output")]
    [InlineData("sources")]
    public void Missing_required_top_level_field_returns_error(string field)
    {
        var yaml = field == "sources"
            ? ValidYaml().Split("sources:", StringSplitOptions.None)[0]
            : ValidYaml().Replace($"{field}: {ValueFor(field)}\n", string.Empty, StringComparison.Ordinal);
        var projectFile = WriteProject(yaml);

        var (project, diagnostics) = ProjectFileLoader.Load(projectFile);

        Assert.Null(project);
        Assert.Contains(diagnostics, d => d.Message.Contains(field, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("displayName")]
    [InlineData("description")]
    [InlineData("author")]
    [InlineData("version")]
    [InlineData("website")]
    public void Missing_required_modinfo_field_returns_error(string field)
    {
        var yaml = ValidYaml().Replace($"  {field}: {ValueFor(field)}\n", string.Empty, StringComparison.Ordinal);
        var projectFile = WriteProject(yaml);

        var (project, diagnostics) = ProjectFileLoader.Load(projectFile);

        Assert.Null(project);
        Assert.Contains(diagnostics, d => d.Message.Contains($"modInfo.{field}", StringComparison.Ordinal));
    }

    [Fact]
    public void Invalid_recursive_value_returns_error()
    {
        var yaml = ValidYaml().Replace("recursive: true", "recursive: sometimes", StringComparison.Ordinal);
        var projectFile = WriteProject(yaml);

        var (project, diagnostics) = ProjectFileLoader.Load(projectFile);

        Assert.Null(project);
        Assert.Contains(diagnostics, d => d.Message.Contains("recursive", StringComparison.Ordinal));
    }

    private string WriteProject(string yaml)
    {
        var path = Path.Combine(_tempDir, "mod.proj.yml");
        File.WriteAllText(path, yaml.Replace("\r\n", "\n"));
        return path;
    }

    private static string ValidYaml() => """
modFolder: EV_LootBox
output: dist
modInfo:
  name: EV_LootBox
  displayName: Loot Box
  description: Adds a loot box.
  author: Aleksei Khozin
  version: 0.1.0
  website: https://github.com/alekho77/epic_7d2d_mods
sources:
  - path: src
    recursive: true
""";

    private static string ValueFor(string field) => field switch
    {
        "modFolder" => "EV_LootBox",
        "output" => "dist",
        "sources" => string.Empty,
        "name" => "EV_LootBox",
        "displayName" => "Loot Box",
        "description" => "Adds a loot box.",
        "author" => "Aleksei Khozin",
        "version" => "0.1.0",
        "website" => "https://github.com/alekho77/epic_7d2d_mods",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
    };
}
