namespace ModletBuilder.Tests;

[CollectionDefinition("Integration", DisableParallelization = true)]
public sealed class IntegrationCollection { }

/// <summary>
/// End-to-end tests that call <c>CommandLine.Run</c>. Filesystem-oriented cases are
/// driven from samples/tests.yml; pure CLI parser/dispatch cases remain in code.
/// </summary>
[Collection("Integration")]
public sealed class CommandLineIntegrationTests : IDisposable
{
    private readonly string _tempRoot;

    public CommandLineIntegrationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    public static IEnumerable<object[]> IntegrationCases() => SampleTestHelper.GetIntegrationCases();

    [Theory]
    [MemberData(nameof(IntegrationCases))]
    public void File_system_case_matches_yaml_specification(SampleTestCase testCase)
    {
        SampleTestHelper.ExecuteYamlCase(testCase, _tempRoot);
    }

    // ── CLI argument errors (exit code 64) ───────────────────────────────────

    [Fact]
    public void Unknown_command_returns_64()
    {
        var exitCode = SampleTestHelper.RunBuildSuppressed(["unknowncmd"]);

        Assert.Equal(64, exitCode);
    }

    [Fact]
    public void Unknown_option_returns_64()
    {
        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", "dummy.frag.xml", "--out", "out", "--not-a-real-flag"]);

        Assert.Equal(64, exitCode);
    }

    [Fact]
    public void Missing_src_returns_64()
    {
        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--out", "out"]);

        Assert.Equal(64, exitCode);
    }

    [Fact]
    public void Missing_out_returns_64()
    {
        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", "dummy.frag.xml"]);

        Assert.Equal(64, exitCode);
    }

    [Fact]
    public void Project_build_with_out_override_writes_mod_folder_under_cli_output()
    {
        var projectFile = WriteProjectWithSingleSource();
        var cliOut = Path.Combine(_tempRoot, "cli-out");
        var yamlOut = Path.Combine(_tempRoot, "yaml-out");

        var (exitCode, stdout, _) = SampleTestHelper.RunBuildCapturing(
            ["build", "--proj", projectFile, "--out", cliOut, "--verbosity", "information"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("overridden by --out", stdout, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(cliOut, "EV_LootBox", "ModInfo.xml")));
        Assert.True(File.Exists(Path.Combine(cliOut, "EV_LootBox", "Config", "items.xml")));
        Assert.False(Directory.Exists(yamlOut));
    }

    [Fact]
    public void Project_build_without_out_uses_yaml_output_root()
    {
        var projectFile = WriteProjectWithSingleSource();

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--proj", projectFile]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(_tempRoot, "yaml-out", "EV_LootBox", "ModInfo.xml")));
        Assert.True(File.Exists(Path.Combine(_tempRoot, "yaml-out", "EV_LootBox", "Config", "items.xml")));
    }

    [Fact]
    public void Project_build_with_cli_src_appends_sources_and_logs_it()
    {
        var projectFile = WriteProjectWithSingleSource();
        var extraSource = Path.Combine(_tempRoot, "extra.frag.xml");
        File.WriteAllText(extraSource, """
<modlet>
  <fragment target="recipes"><append xpath="/recipes"/></fragment>
</modlet>
""");

        var cliOut = Path.Combine(_tempRoot, "mixed-out");

        var (exitCode, stdout, _) = SampleTestHelper.RunBuildCapturing(
            ["build", "--proj", projectFile, "--src", extraSource, "--out", cliOut, "--verbosity", "information"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Adding 1 CLI --src source path", stdout, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(cliOut, "EV_LootBox", "Config", "items.xml")));
        Assert.True(File.Exists(Path.Combine(cliOut, "EV_LootBox", "Config", "recipes.xml")));
    }

    private string WriteProjectWithSingleSource()
    {
        var sourceDir = Path.Combine(_tempRoot, "project-src");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "items.frag.xml"), """
<modlet>
  <fragment target="items"><append xpath="/items"/></fragment>
</modlet>
""");

        var projectFile = Path.Combine(_tempRoot, "mod.proj.yml");
        File.WriteAllText(projectFile, """
modFolder: EV_LootBox
output: yaml-out
modInfo:
  name: EV_LootBox
  displayName: Loot Box
  description: Adds a loot box.
  author: Aleksei Khozin
  version: 0.1.0
  website: https://github.com/alekho77/epic_7d2d_mods
sources:
  - path: project-src
    recursive: false
""");

        return projectFile;
    }
}
