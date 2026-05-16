namespace ModletBuilder.Cli;

using ModletBuilder.Core.Generation;
using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;
using ModletBuilder.Core.Parsing;
using ModletBuilder.Core.Resolution;
using ModletBuilder.Core.SourceDiscovery;

internal static class CommandLine
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintAbout(Console.Out);
            return ExitCodes.Success;
        }

        var command = args[0];

        switch (command)
        {
            case "-h":
            case "--help":
                PrintHelp(Console.Out);
                return ExitCodes.Success;

            case "--version":
                Console.WriteLine(GetVersion());
                return ExitCodes.Success;

            case "build":
                return RunBuild(args[1..]);

            default:
                Console.Error.WriteLine($"Unknown command: {command}");
                Console.Error.WriteLine();
                PrintHelp(Console.Error);
                return ExitCodes.UsageError;
        }
    }

    private static int RunBuild(string[] args)
    {
        var (options, parseErrors) = BuildCommand.ParseArgs(args);
        if (options is null)
        {
            foreach (var error in parseErrors)
                Console.Error.WriteLine($"error: {error}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                "Usage: modlet-builder build --src <path> [<path> ...] --out <mod-dir> " +
                "[--recursive] [--dry-run] [--clean] [--verbosity <level>]");
            return ExitCodes.UsageError;
        }

        var logger = new BuildLogger(options.Verbosity, Console.Out, Console.Error);
        var allDiagnostics = new List<Diagnostic>();

        // ── Stage 0: Source discovery ─────────────────────────────────────────
        var (files, discoverDiagnostics) = SourceDiscoverer.Discover(options.Sources, options.Recursive);
        allDiagnostics.AddRange(discoverDiagnostics);
        logger.Debug($"Discovered {files.Count} source file(s).");

        // ── Stage 0: Fragment parsing ─────────────────────────────────────────
        var fragments = new List<Fragment>();
        foreach (var file in files)
        {
            var (fileFragments, parseDiagnostics) = FragmentParser.Parse(file);
            allDiagnostics.AddRange(parseDiagnostics);
            fragments.AddRange(fileFragments);
        }

        logger.Debug($"Parsed {fragments.Count} fragment(s) from {files.Count} file(s).");

        if (HasErrors(allDiagnostics))
        {
            EmitDiagnostics(allDiagnostics, logger);
            return ExitCodes.BuildError;
        }

        if (fragments.Count == 0)
        {
            Console.Error.WriteLine("error: No fragments found in the specified source paths.");
            return ExitCodes.BuildError;
        }

        // ── Stage 1: Dependency resolution ────────────────────────────────────
        var (ordered, resolveDiagnostics) = DependencyResolver.Resolve(fragments);
        allDiagnostics.AddRange(resolveDiagnostics);

        if (HasErrors(allDiagnostics))
        {
            EmitDiagnostics(allDiagnostics, logger);
            return ExitCodes.BuildError;
        }

        logger.Debug($"Resolved order for {ordered.Count} fragment(s).");

        // ── Stage 2: Output generation ────────────────────────────────────────
        var generateDiagnostics = OutputGenerator.Generate(
            ordered, options.OutputDir, options.DryRun, options.Clean, logger);
        allDiagnostics.AddRange(generateDiagnostics);

        EmitDiagnostics(allDiagnostics, logger);

        if (HasErrors(allDiagnostics))
            return ExitCodes.BuildError;

        if (options.DryRun)
            logger.Information("Dry run completed. No files were written.");
        else
            logger.Information($"Build complete. Output written to '{options.OutputDir}'.");

        return ExitCodes.Success;
    }

    private static bool HasErrors(IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    private static void EmitDiagnostics(IEnumerable<Diagnostic> diagnostics, BuildLogger logger)
    {
        foreach (var d in diagnostics)
        {
            switch (d.Severity)
            {
                case DiagnosticSeverity.Error:
                    logger.Error(d.ToString());
                    break;
                case DiagnosticSeverity.Warning:
                    logger.Warning(d.ToString());
                    break;
            }
        }
    }

    private static void PrintAbout(TextWriter writer)
    {
        writer.WriteLine($"modlet-builder {GetVersion()}");
        writer.WriteLine("A build tool for assembling modlets and XML patch fragments into final mod output.");
        writer.WriteLine();
        writer.WriteLine("Run 'modlet-builder -h' or 'modlet-builder --help' for detailed help.");
    }

    private static void PrintHelp(TextWriter writer)
    {
        writer.WriteLine($"modlet-builder {GetVersion()}");
        writer.WriteLine("A build tool for assembling modlets and XML patch fragments into final mod output.");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  modlet-builder                     Show brief tool info.");
        writer.WriteLine("  modlet-builder [options]");
        writer.WriteLine("  modlet-builder <command> [options]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  -h, --help                         Display this help.");
        writer.WriteLine("      --version                      Display tool version.");
        writer.WriteLine();
        writer.WriteLine("Commands:");
        writer.WriteLine("  build                              Assemble fragments into output mod Config files.");
        writer.WriteLine();
        writer.WriteLine("'build' command options:");
        writer.WriteLine("      --src <path> [<path> ...]  One or more source files or directories");
        writer.WriteLine("                                 containing *.frag.xml files. Required.");
        writer.WriteLine("      --out <mod-dir>            Output mod directory. Required.");
        writer.WriteLine("                                 Config files are written to {mod-dir}/Config/.");
        writer.WriteLine("      --recursive                Scan source directories recursively.");
        writer.WriteLine("      --dry-run                  Validate sources, resolve dependencies, and");
        writer.WriteLine("                                 report what would be built — without writing files.");
        writer.WriteLine("      --clean                    Delete the entire --out directory before building.");
        writer.WriteLine("                                 Has no effect with --dry-run.");
        writer.WriteLine("      --verbosity <level>        Log verbosity: debug, information (default),");
        writer.WriteLine("                                 warning, error, none.");
        writer.WriteLine();
    }

    private static string GetVersion()
    {
        var assembly = typeof(CommandLine).Assembly;
        var informational = assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault();

        return informational?.InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
