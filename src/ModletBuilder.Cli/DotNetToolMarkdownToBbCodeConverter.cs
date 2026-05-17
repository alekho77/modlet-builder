using System.Diagnostics;
using ModletBuilder.Core.Generation;
using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Cli;

internal sealed class DotNetToolMarkdownToBbCodeConverter : IMarkdownToBbCodeConverter
{
    internal static readonly DotNetToolMarkdownToBbCodeConverter Instance = new();

    private const string Package = "Converter.MarkdownToBBCodeNM.Tool@1.0.0.17";
    private const int TimeoutMilliseconds = 120_000;

    private DotNetToolMarkdownToBbCodeConverter()
    {
    }

    public IReadOnlyList<Diagnostic> Convert(
        string markdownPath,
        string outputPath,
        BuildLogger logger)
    {
        var diagnostics = new List<Diagnostic>();
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.StartInfo.ArgumentList.Add("tool");
        process.StartInfo.ArgumentList.Add("exec");
        process.StartInfo.ArgumentList.Add(Package);
        process.StartInfo.ArgumentList.Add("--allow-roll-forward");
        process.StartInfo.ArgumentList.Add("--");
        process.StartInfo.ArgumentList.Add("-i");
        process.StartInfo.ArgumentList.Add(markdownPath);
        process.StartInfo.ArgumentList.Add("-o");
        process.StartInfo.ArgumentList.Add(outputPath);

        try
        {
            logger.Debug(
                $"Running Markdown to BBCode converter: dotnet tool exec {Package} --allow-roll-forward -- -i \"{markdownPath}\" -o \"{outputPath}\"");

            if (!process.Start())
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    "Could not start Markdown to BBCode converter process.",
                    markdownPath));
                return diagnostics;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(TimeoutMilliseconds))
            {
                TryKill(process);
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    "Markdown to BBCode converter timed out.",
                    markdownPath));
                return diagnostics;
            }

            var stdout = stdoutTask.GetAwaiter().GetResult();
            var stderr = stderrTask.GetAwaiter().GetResult();

            if (!string.IsNullOrWhiteSpace(stdout))
                logger.Debug(stdout.Trim());

            if (process.ExitCode != 0)
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Markdown to BBCode converter failed with exit code {process.ExitCode}: {TrimProcessOutput(stderr)}",
                    markdownPath));
            }
            else if (!string.IsNullOrWhiteSpace(stderr))
            {
                logger.Debug(stderr.Trim());
            }
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not run Markdown to BBCode converter: {ex.Message}",
                markdownPath));
        }

        return diagnostics;
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // The process may have exited after the timeout check.
        }
    }

    private static string TrimProcessOutput(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrEmpty(trimmed)
            ? "no error output"
            : trimmed;
    }
}
