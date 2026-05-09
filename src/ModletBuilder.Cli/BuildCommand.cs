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
        var targets = new List<string>();
        bool clean = false;
        var verbosity = VerbosityLevel.Information;

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

                case "--targets":
                    i++;
                    if (i >= args.Length || args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        errors.Add("Option '--targets' requires at least one mod name argument.");
                        break;
                    }
                    while (i < args.Length && !args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        targets.Add(args[i]);
                        i++;
                    }
                    continue;

                case "--recursive":
                    recursive = true;
                    i++;
                    continue;

                case "--dry-run":
                    dryRun = true;
                    i++;
                    continue;

                case "--clean":
                    clean = true;
                    i++;
                    continue;

                case "--verbosity":
                    i++;
                    if (i >= args.Length || args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        errors.Add("Option '--verbosity' requires a level argument (debug, information, warning, error, none).");
                        break;
                    }
                    if (!TryParseVerbosity(args[i], out verbosity))
                        errors.Add($"Unknown verbosity level '{args[i]}'. Expected one of: debug, information, warning, error, none.");
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

        return (new BuildOptions(
            sources.ToArray(),
            outputDir!,
            recursive,
            dryRun,
            targets.ToArray(),
            clean,
            verbosity), errors);
    }

    private static bool TryParseVerbosity(string value, out VerbosityLevel level)
    {
        (level, bool known) = value.ToLowerInvariant() switch
        {
            "debug"       => (VerbosityLevel.Debug,       true),
            "information" => (VerbosityLevel.Information, true),
            "warning"     => (VerbosityLevel.Warning,     true),
            "error"       => (VerbosityLevel.Error,       true),
            "none"        => (VerbosityLevel.None,        true),
            _             => (VerbosityLevel.Information, false),
        };
        return known;
    }
}
