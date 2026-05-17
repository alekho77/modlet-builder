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
        var readmePath = Path.Combine(_tempDir, "docs", "lootbox.md");
        Directory.CreateDirectory(Path.GetDirectoryName(readmePath)!);
        File.WriteAllText(readmePath, "# Loot Box\n");

        var projectFile = WriteProject("""
modFolder: EV_LootBox
output: dist
readme: docs/lootbox.md
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
        Assert.NotNull(project.Readme);
        Assert.Equal(readmePath, project.Readme.Path);
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

    [Fact]
    public void Missing_readme_field_is_allowed()
    {
        var projectFile = WriteProject(ValidYaml());

        var (project, diagnostics) = ProjectFileLoader.Load(projectFile);

        Assert.Empty(diagnostics);
        Assert.NotNull(project);
        Assert.Null(project.Readme);
    }

    [Fact]
    public void Readme_path_resolves_relative_to_project_file()
    {
        var readmePath = Path.Combine(_tempDir, "docs", "custom-name.md");
        Directory.CreateDirectory(Path.GetDirectoryName(readmePath)!);
        File.WriteAllText(readmePath, "# Loot Box\n");

        var yaml = InsertAfterLine(ValidYaml(), "output: dist", "readme: docs/custom-name.md");
        var projectFile = WriteProject(yaml);

        var (project, diagnostics) = ProjectFileLoader.Load(projectFile);

        Assert.Empty(diagnostics);
        Assert.NotNull(project);
        Assert.NotNull(project.Readme);
        Assert.Equal(readmePath, project.Readme.Path);
    }

    [Fact]
    public void Empty_readme_path_returns_error()
    {
        var yaml = InsertAfterLine(ValidYaml(), "output: dist", "readme: \"\"");
        var projectFile = WriteProject(yaml);

        var (project, diagnostics) = ProjectFileLoader.Load(projectFile);

        Assert.Null(project);
        Assert.Contains(diagnostics, d => d.Message.Contains("readme", StringComparison.Ordinal));
    }

    [Fact]
    public void Missing_readme_file_returns_error()
    {
        var yaml = InsertAfterLine(ValidYaml(), "output: dist", "readme: docs/missing.md");
        var projectFile = WriteProject(yaml);

        var (project, diagnostics) = ProjectFileLoader.Load(projectFile);

        Assert.Null(project);
        Assert.Contains(diagnostics, d => d.Message.Contains("readme file does not exist", StringComparison.Ordinal));
    }

    [Fact]
    public void Readme_directory_returns_error()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "docs"));
        var yaml = InsertAfterLine(ValidYaml(), "output: dist", "readme: docs");
        var projectFile = WriteProject(yaml);

        var (project, diagnostics) = ProjectFileLoader.Load(projectFile);

        Assert.Null(project);
        Assert.Contains(diagnostics, d => d.Message.Contains("not a directory", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("modFolder")]
    [InlineData("output")]
    [InlineData("sources")]
    public void Missing_required_top_level_field_returns_error(string field)
    {
        var yaml = field == "sources"
            ? RemoveSourcesBlock(ValidYaml())
            : RemoveLineStartingWith(ValidYaml(), $"{field}: ");
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
        var yaml = RemoveLineStartingWith(ValidYaml(), $"  {field}: ");
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
        File.WriteAllText(path, NormalizeYaml(yaml));
        return path;
    }

    private static string RemoveSourcesBlock(string yaml) =>
        NormalizeYaml(yaml).Split("sources:", StringSplitOptions.None)[0];

    private static string RemoveLineStartingWith(string yaml, string prefix) =>
        string.Join(
            "\n",
            NormalizeYaml(yaml)
                .Split('\n')
                .Where(line => !line.StartsWith(prefix, StringComparison.Ordinal)));

    private static string InsertAfterLine(string yaml, string targetLine, string insertedLine)
    {
        var lines = NormalizeYaml(yaml).Split('\n').ToList();
        var index = lines.FindIndex(line => string.Equals(line, targetLine, StringComparison.Ordinal));
        if (index < 0)
            throw new InvalidOperationException($"Line not found: {targetLine}");

        lines.Insert(index + 1, insertedLine);
        return string.Join("\n", lines);
    }

    private static string NormalizeYaml(string yaml) =>
        yaml.Replace("\r\n", "\n").Replace("\r", "\n");

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

}
