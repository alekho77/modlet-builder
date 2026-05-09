using ModletBuilder.Core.Models;

namespace ModletBuilder.Cli;

internal static class BuildCommand
{
    internal static (BuildOptions? Options, IReadOnlyList<string> Errors) ParseArgs(string[] args)
    {
        var errors = new List<string>();
        var sources = new List<string>();
        string? outputDir = null;
        bool recursive = false;
        bool dryRun = false;

        int i = 0;
        while (i < args.Length)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--src":
                    i++;
                    if (i >= args.Length || args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        errors.Add("Option '--src' requires at least one path argument.");
                        break;
                    }
                    while (i < args.Length && !args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        sources.Add(args[i]);
                        i++;
                    }
                    continue;

                case "--out":
                    i++;
                    if (i >= args.Length || args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        errors.Add("Option '--out' requires a directory path argument.");
                        break;
                    }
                    outputDir = args[i];
                    i++;
                    continue;

                case "--recursive":
                    recursive = true;
                    i++;
                    continue;

                case "--dry-run":
                    dryRun = true;
                    i++;
                    continue;

                default:
                    errors.Add($"Unknown option: {arg}");
                    i++;
                    continue;
            }
        }

        if (sources.Count == 0)
            errors.Add("Option '--src' is required and must specify at least one source path.");

        if (outputDir is null)
            errors.Add("Option '--out' is required.");

        if (errors.Count > 0)
            return (null, errors);

        return (new BuildOptions(sources.ToArray(), outputDir!, recursive, dryRun), errors);
    }
}
