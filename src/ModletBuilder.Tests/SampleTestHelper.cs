using ModletBuilder.Cli;

namespace ModletBuilder.Tests;

internal static class SampleTestHelper
{
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

    /// <summary>
    /// Asserts that every XML file in <paramref name="expectedDir"/>/Config/ has
    /// a matching counterpart in <paramref name="outDir"/>/Config/ with identical
    /// content (line-ending-normalised), and that no extra files exist in the
    /// actual output.
    /// </summary>
    internal static void AssertOutputMatchesExpected(string outDir, string expectedDir)
    {
        var expectedConfigDir = Path.Combine(expectedDir, "Config");
        var actualConfigDir = Path.Combine(outDir, "Config");

        Assert.True(Directory.Exists(actualConfigDir),
            $"Output Config directory was not created: {actualConfigDir}");

        var expectedFiles = Directory
            .GetFiles(expectedConfigDir, "*.xml", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(expectedConfigDir, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        foreach (var relative in expectedFiles)
        {
            var actualFile = Path.Combine(actualConfigDir, relative);
            Assert.True(File.Exists(actualFile), $"Expected output file missing: {relative}");

            var expectedContent = NormalizeLineEndings(
                File.ReadAllText(Path.Combine(expectedConfigDir, relative)));
            var actualContent = NormalizeLineEndings(File.ReadAllText(actualFile));
            Assert.Equal(expectedContent, actualContent);
        }

        var actualFiles = Directory
            .GetFiles(actualConfigDir, "*.xml", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(actualConfigDir, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expectedFiles, actualFiles);
    }

    private static string NormalizeLineEndings(string content) =>
        content.Replace("\r\n", "\n");
}
