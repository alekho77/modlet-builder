namespace ModletBuilder.Tests;

/// <summary>
/// YAML-driven golden tests that build checked-in samples and verify either
/// golden output equality or deterministic repeated output.
/// </summary>
[Collection("Integration")]
public sealed class SampleGoldenTests : IDisposable
{
    private readonly string _tempRoot;

    public SampleGoldenTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    public static IEnumerable<object[]> GoldenCases() => SampleTestHelper.GetGoldenCases();

    [Theory]
    [MemberData(nameof(GoldenCases))]
    public void Golden_case_matches_yaml_specification(SampleTestCase testCase)
    {
        SampleTestHelper.ExecuteYamlCase(testCase, _tempRoot);
    }
}
