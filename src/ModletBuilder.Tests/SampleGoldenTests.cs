namespace ModletBuilder.Tests;

/// <summary>
/// Golden tests that build each real sample and compare the output byte-for-byte
/// against the checked-in expected/ directory. These tests are the regression
/// safety net for output format changes.
/// </summary>
[Collection("Integration")]
public sealed class SampleGoldenTests : IDisposable
{
    private readonly string _tempOut;
    private readonly string _repoRoot;

    public SampleGoldenTests()
    {
        _tempOut = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _repoRoot = SampleTestHelper.FindRepoRoot();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempOut))
            Directory.Delete(_tempOut, recursive: true);
    }

    [Fact]
    public void Alloy_motor_tool_parts_matches_expected_output()
    {
        var src = Path.Combine(_repoRoot, "samples", "real", "alloy-motor-tool-parts", "src");
        var expected = Path.Combine(_repoRoot, "samples", "real", "alloy-motor-tool-parts", "expected");

        SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--verbosity", "none"]);

        SampleTestHelper.AssertOutputMatchesExpected(_tempOut, expected);
    }

    [Fact]
    public void Epic_cash_matches_expected_output()
    {
        var src = Path.Combine(_repoRoot, "samples", "real", "epic-cash", "src");
        var expected = Path.Combine(_repoRoot, "samples", "real", "epic-cash", "expected");

        SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--verbosity", "none"]);

        SampleTestHelper.AssertOutputMatchesExpected(_tempOut, expected);
    }

    [Fact]
    public void Project_z_cash_matches_expected_output()
    {
        var src = Path.Combine(_repoRoot, "samples", "real", "project-z-cash", "src");
        var expected = Path.Combine(_repoRoot, "samples", "real", "project-z-cash", "expected");

        SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--verbosity", "none"]);

        SampleTestHelper.AssertOutputMatchesExpected(_tempOut, expected);
    }

    [Fact]
    public void Alloy_motor_tool_parts_build_is_deterministic()
    {
        var src = Path.Combine(_repoRoot, "samples", "real", "alloy-motor-tool-parts", "src");
        var outA = Path.Combine(_tempOut, "run-a");
        var outB = Path.Combine(_tempOut, "run-b");

        SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", outA, "--verbosity", "none"]);
        SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", outB, "--verbosity", "none"]);

        // Use run-a as the "expected" baseline for run-b.
        SampleTestHelper.AssertOutputMatchesExpected(outB, outA);
    }

    [Fact]
    public void Epic_cash_with_recursive_flag_discovers_all_fragments()
    {
        // The src/ directory is flat, so --recursive behaves identically to non-recursive.
        // This verifies that --recursive does not break or duplicate discovery.
        var src = Path.Combine(_repoRoot, "samples", "real", "epic-cash", "src");
        var expected = Path.Combine(_repoRoot, "samples", "real", "epic-cash", "expected");

        var exitCode = SampleTestHelper.RunBuildSuppressed(
            ["build", "--src", src, "--out", _tempOut, "--recursive", "--verbosity", "none"]);

        Assert.Equal(0, exitCode);
        SampleTestHelper.AssertOutputMatchesExpected(_tempOut, expected);
    }
}
