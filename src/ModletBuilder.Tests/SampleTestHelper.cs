using ModletBuilder.Cli;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ModletBuilder.Tests;

internal static class SampleTestHelper
{
    private static readonly Lazy<IReadOnlyList<SampleTestCase>> CachedTestCases = new(LoadAllCases);

    /// <summary>
    /// Walks up from the test binary output directory to find the repository root
    /// by locating the ModletBuilder.sln file.
    /// </summary>
    internal static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ModletBuilder.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            "Could not find repository root (no ModletBuilder.sln found in any parent directory).");
    }

    /// <summary>
    /// Runs <see cref="CommandLine.Run"/> with stdout and stderr redirected to
    /// <see cref="TextWriter.Null"/> so that test runner output stays clean.
    /// </summary>
    internal static int RunBuildSuppressed(string[] args)
    {
        var prevOut = Console.Out;
        var prevErr = Console.Error;
        Console.SetOut(TextWriter.Null);
        Console.SetError(TextWriter.Null);
        try
        {
            return CommandLine.Run(args);
        }
        finally
        {
            Console.SetOut(prevOut);
            Console.SetError(prevErr);
        }
    }

    internal static IEnumerable<object[]> GetGoldenCases() =>
        CachedTestCases.Value
            .Where(testCase => !string.IsNullOrWhiteSpace(testCase.Expected.ExpectedResultPath))
            .OrderBy(testCase => testCase.Id, StringComparer.Ordinal)
            .Select(testCase => new object[] { testCase });

    internal static IEnumerable<object[]> GetIntegrationCases() =>
        CachedTestCases.Value
            .Where(testCase => string.IsNullOrWhiteSpace(testCase.Expected.ExpectedResultPath))
            .OrderBy(testCase => testCase.Id, StringComparer.Ordinal)
            .Select(testCase => new object[] { testCase });

    internal static void ExecuteYamlCase(SampleTestCase testCase, string tempRoot)
    {
        var caseRoot = Path.Combine(tempRoot, SanitizePathSegment(testCase.Id));
        Directory.CreateDirectory(caseRoot);

        var context = new SampleExecutionContext(FindRepoRoot(), caseRoot);

        ApplySetupDirectories(testCase.SetupDirectories, context);
        ApplyFileSetup(testCase.PreSetupFiles, context);
        ApplyFileSetup(testCase.InlineSetupFiles, context);

        ExecuteSingleRunCase(testCase, context);
    }

    /// <summary>
    /// Asserts that every file under <paramref name="expectedDir"/> has
    /// a matching counterpart under <paramref name="outDir"/> with identical
    /// content (line-ending-normalised), and that no extra files exist in the
    /// actual output tree.
    /// </summary>
    internal static void AssertOutputMatchesExpected(string outDir, string expectedDir)
    {
        Assert.True(Directory.Exists(expectedDir),
            $"Expected directory does not exist: {expectedDir}");
        Assert.True(Directory.Exists(outDir),
            $"Output directory was not created: {outDir}");

        var expectedFiles = Directory
            .GetFiles(expectedDir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(expectedDir, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        foreach (var relative in expectedFiles)
        {
            var actualFile = Path.Combine(outDir, relative);
            Assert.True(File.Exists(actualFile), $"Expected output file missing: {relative}");

            var expectedContent = NormalizeLineEndings(
                File.ReadAllText(Path.Combine(expectedDir, relative)));
            var actualContent = NormalizeLineEndings(File.ReadAllText(actualFile));
            Assert.Equal(expectedContent, actualContent);
        }

        var actualFiles = Directory
            .GetFiles(outDir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(outDir, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expectedFiles, actualFiles);
    }

    private static string NormalizeLineEndings(string content) =>
        content.Replace("\r\n", "\n");

    private static IReadOnlyList<SampleTestCase> LoadAllCases()
    {
        var yamlPath = Path.Combine(FindRepoRoot(), "samples", "tests.yml");
        using var reader = File.OpenText(yamlPath);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var suite = deserializer.Deserialize<SampleTestSuite>(reader) ?? new SampleTestSuite();
        ValidateCases(suite.Tests);
        return suite.Tests;
    }

    private static void ValidateCases(IReadOnlyList<SampleTestCase> testCases)
    {
        var duplicateIds = testCases
            .GroupBy(testCase => testCase.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate test case ids in samples/tests.yml: {string.Join(", ", duplicateIds)}");
        }
    }

    private static void ExecuteSingleRunCase(SampleTestCase testCase, SampleExecutionContext context)
    {
        var outputPath = ResolvePathToken(testCase.Command.Output, context);
        var exitCode = RunBuildSuppressed(BuildArgs(testCase.Command, context));

        Assert.Equal(testCase.Expected.ExitCode, exitCode);
        AssertExpectedFilesystemState(outputPath, testCase.Expected, context);
    }

    private static void AssertExpectedFilesystemState(
        string outputPath,
        SampleExpectedResult expected,
        SampleExecutionContext context)
    {
        if (expected.ConfigCreated.HasValue)
        {
            var configDir = Path.Combine(outputPath, "Config");
            Assert.Equal(expected.ConfigCreated.Value, Directory.Exists(configDir));
        }

        if (!string.IsNullOrWhiteSpace(expected.ExpectedResultPath))
        {
            var expectedPath = ResolvePathToken(expected.ExpectedResultPath, context);
            AssertOutputMatchesExpected(outputPath, expectedPath);
        }

        AssertExpectedRelativePaths(outputPath, expected, context);
    }

    private static void AssertExpectedRelativePaths(
        string outputPath,
        SampleExpectedResult expected,
        SampleExecutionContext context)
    {
        foreach (var relativePath in expected.ExistingPaths)
        {
            var fullPath = ResolveOutputRelativePath(outputPath, relativePath);
            Assert.True(File.Exists(fullPath) || Directory.Exists(fullPath),
                $"Expected path does not exist for test case '{context.CaseRoot}': {relativePath}");
        }

        foreach (var relativePath in expected.MissingPaths)
        {
            var fullPath = ResolveOutputRelativePath(outputPath, relativePath);
            Assert.False(File.Exists(fullPath) || Directory.Exists(fullPath),
                $"Path should not exist for test case '{context.CaseRoot}': {relativePath}");
        }
    }

    private static string ResolveOutputRelativePath(string outputPath, string relativePath) =>
        Path.Combine(outputPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string[] BuildArgs(
        SampleBuildCommand command,
        SampleExecutionContext context)
    {
        var args = new List<string> { command.Verb, "--src" };

        foreach (var source in command.Sources)
            args.Add(ResolvePathToken(source, context));

        args.Add("--out");
        args.Add(ResolvePathToken(command.Output, context));

        if (command.Recursive)
            args.Add("--recursive");

        if (command.DryRun)
            args.Add("--dry-run");

        if (command.Clean)
            args.Add("--clean");

        args.Add("--verbosity");
        args.Add(command.Verbosity);

        return args.ToArray();
    }

    private static void ApplySetupDirectories(
        IReadOnlyList<string> setupDirectories,
        SampleExecutionContext context)
    {
        foreach (var directory in setupDirectories)
            Directory.CreateDirectory(ResolvePathToken(directory, context));
    }

    private static void ApplyFileSetup(
        IReadOnlyList<SampleFileSpec> files,
        SampleExecutionContext context)
    {
        foreach (var file in files)
        {
            var filePath = ResolvePathToken(file.Path, context);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, file.Content);
        }
    }

    private static string ResolvePathToken(string value, SampleExecutionContext context)
    {
        var resolved = value
            .Replace("{repoRoot}", context.RepoRoot, StringComparison.Ordinal)
            .Replace("{tempRoot}", context.CaseRoot, StringComparison.Ordinal)
            .Replace("{tempSrc}", context.TempSrc, StringComparison.Ordinal)
            .Replace("{tempOut}", context.TempOut, StringComparison.Ordinal)
            .Replace('/', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(resolved))
            return Path.GetFullPath(resolved);

        return Path.GetFullPath(Path.Combine(context.RepoRoot, resolved));
    }

    private static string SanitizePathSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new char[value.Length];

        for (var i = 0; i < value.Length; i++)
            builder[i] = invalidChars.Contains(value[i]) ? '-' : value[i];

        return new string(builder);
    }
}

public sealed class SampleTestSuite
{
    public List<SampleTestCase> Tests { get; set; } = [];
}

public sealed class SampleTestCase
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public SampleBuildCommand Command { get; set; } = new();

    public List<string> SetupDirectories { get; set; } = [];

    public List<SampleFileSpec> InlineSetupFiles { get; set; } = [];

    public List<SampleFileSpec> PreSetupFiles { get; set; } = [];

    public SampleExpectedResult Expected { get; set; } = new();

    public override string ToString() => Name;
}

public sealed class SampleBuildCommand
{
    public string Verb { get; set; } = "build";

    public List<string> Sources { get; set; } = [];

    public string Output { get; set; } = "{tempOut}";

    public bool Recursive { get; set; }

    public bool DryRun { get; set; }

    public bool Clean { get; set; }

    public string Verbosity { get; set; } = "none";
}

public sealed class SampleExpectedResult
{
    public int ExitCode { get; set; }

    public string? ExpectedResultPath { get; set; }

    public bool? ConfigCreated { get; set; }

    public List<string> ExistingPaths { get; set; } = [];

    public List<string> MissingPaths { get; set; } = [];
}

public sealed class SampleFileSpec
{
    public string Path { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

internal sealed class SampleExecutionContext
{
    internal SampleExecutionContext(string repoRoot, string caseRoot)
    {
        RepoRoot = repoRoot;
        CaseRoot = caseRoot;
        TempSrc = Path.Combine(caseRoot, "src");
        TempOut = Path.Combine(caseRoot, "out");
    }

    internal string RepoRoot { get; }

    internal string CaseRoot { get; }

    internal string TempSrc { get; }

    internal string TempOut { get; }
}
