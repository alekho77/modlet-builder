namespace ModletBuilder.Tests;

[CollectionDefinition("Integration", DisableParallelization = true)]
public sealed class IntegrationCollection { }

/// <summary>
/// End-to-end tests that call <c>CommandLine.Run</c> with real filesystem paths.
/// All tests in this collection run sequentially to prevent Console redirection races.
/// </summary>
[Collection("Integration")]
public sealed class CommandLineIntegrationTests : IDisposable
{
    private readonly string _tempOut;
    private readonly string _repoRoot;

    public CommandLineIntegrationTests()
    {
        _tempOut = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _repoRoot = SampleTestHelper.FindRepoRoot();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempOut))
            Directory.Delete(_tempOut, recursive: true);
    }

    // ── Positive build scenarios ──────────────────────────────────────────────

    [Fact]
    public void Successful_build_returns_0_and_creates_Config()
    {
        var src = Path.Combine(_repoRoot, "samples", "real", "alloy-motor-tool-parts", "src");

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--verbosity", "none"]);

        Assert.Equal(0, exitCode);
        Assert.True(Directory.Exists(Path.Combine(_tempOut, "Config")));
    }

    [Fact]
    public void Dry_run_returns_0_and_does_not_create_output_directory()
    {
        var src = Path.Combine(_repoRoot, "samples", "real", "alloy-motor-tool-parts", "src");

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--dry-run", "--verbosity", "none"]);

        Assert.Equal(0, exitCode);
        Assert.False(Directory.Exists(Path.Combine(_tempOut, "Config")));
    }

    [Fact]
    public void Clean_removes_stale_file_before_building()
    {
        var src = Path.Combine(_repoRoot, "samples", "real", "alloy-motor-tool-parts", "src");

        // Pre-populate the output Config with a stale file.
        Directory.CreateDirectory(Path.Combine(_tempOut, "Config"));
        File.WriteAllText(Path.Combine(_tempOut, "Config", "stale.xml"), "<stale/>");

        SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--clean", "--verbosity", "none"]);

        Assert.False(
            File.Exists(Path.Combine(_tempOut, "Config", "stale.xml")),
            "Stale file must be removed when --clean is used.");
        Assert.True(File.Exists(Path.Combine(_tempOut, "Config", "items.xml")));
    }

    // ── Negative build scenarios (parse and resolution errors) ───────────────

    [Fact]
    public void Build_with_missing_dependency_returns_1_and_no_output()
    {
        var src = Path.Combine(_repoRoot, "samples", "invalid", "missing-dependency.frag.xml");

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--verbosity", "none"]);

        Assert.Equal(1, exitCode);
        Assert.False(Directory.Exists(Path.Combine(_tempOut, "Config")));
    }

    [Fact]
    public void Build_with_duplicate_names_returns_1_and_no_output()
    {
        var src = Path.Combine(_repoRoot, "samples", "invalid", "duplicate-names.frag.xml");

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--verbosity", "none"]);

        Assert.Equal(1, exitCode);
        Assert.False(Directory.Exists(Path.Combine(_tempOut, "Config")));
    }

    [Fact]
    public void Build_with_cycle_returns_1_and_no_output()
    {
        var src = Path.Combine(_repoRoot, "samples", "invalid", "cycle.frag.xml");

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--verbosity", "none"]);

        Assert.Equal(1, exitCode);
        Assert.False(Directory.Exists(Path.Combine(_tempOut, "Config")));
    }

    [Fact]
    public void Build_with_unknown_target_returns_1_and_no_output()
    {
        var src = Path.Combine(_repoRoot, "samples", "invalid", "unknown-target.frag.xml");

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--verbosity", "none"]);

        Assert.Equal(1, exitCode);
        Assert.False(Directory.Exists(Path.Combine(_tempOut, "Config")));
    }

    [Fact]
    public void Build_with_hint_attribute_returns_1_and_no_output()
    {
        var src = Path.Combine(_repoRoot, "samples", "invalid", "hint-attribute.frag.xml");

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--verbosity", "none"]);

        Assert.Equal(1, exitCode);
        Assert.False(Directory.Exists(Path.Combine(_tempOut, "Config")));
    }

    [Fact]
    public void Build_with_malformed_xml_returns_1_and_no_output()
    {
        var src = Path.Combine(_repoRoot, "samples", "invalid", "malformed.frag.xml");

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--verbosity", "none"]);

        Assert.Equal(1, exitCode);
        Assert.False(Directory.Exists(Path.Combine(_tempOut, "Config")));
    }

    [Fact]
    public void Build_with_no_fragments_found_returns_1()
    {
        // Directory with no .frag.xml files produces "no fragments found" error.
        var emptyDir = Path.Combine(_tempOut, "empty-src");
        Directory.CreateDirectory(emptyDir);

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", emptyDir, "--out", Path.Combine(_tempOut, "out"), "--verbosity", "none"]);

        Assert.Equal(1, exitCode);
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
        var src = Path.Combine(_repoRoot, "samples", "real", "alloy-motor-tool-parts", "src");

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--not-a-real-flag"]);

        Assert.Equal(64, exitCode);
    }

    [Fact]
    public void Missing_src_returns_64()
    {
        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--out", _tempOut]);

        Assert.Equal(64, exitCode);
    }

    [Fact]
    public void Missing_out_returns_64()
    {
        var src = Path.Combine(_repoRoot, "samples", "real", "alloy-motor-tool-parts", "src");

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src]);

        Assert.Equal(64, exitCode);
    }
}
