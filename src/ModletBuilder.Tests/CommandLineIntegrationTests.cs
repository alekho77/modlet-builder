namespace ModletBuilder.Tests;

[CollectionDefinition("Integration", DisableParallelization = true)]
public sealed class IntegrationCollection { }

/// <summary>
/// End-to-end tests that call <c>CommandLine.Run</c>. Filesystem-oriented cases are
/// driven from samples/tests.yaml; pure CLI parser/dispatch cases remain in code.
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

    public static IEnumerable<object[]> IntegrationCases() => SampleTestHelper.GetCases("integration");

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
}
