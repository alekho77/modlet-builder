namespace ModletBuilder.Cli;

using ModletBuilder.Core.Generation;
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
            return 0;
        }

        var command = args[0];

        switch (command)
        {
            case "-h":
            case "--help":
                PrintHelp(Console.Out);
                return 0;

            case "--version":
                Console.WriteLine(GetVersion());
                return 0;

            case "build":
                return RunBuild(args[1..]);

            default:
                Console.Error.WriteLine($"Unknown command: {command}");
                Console.Error.WriteLine();
                PrintHelp(Console.Error);
                return 64;
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
            Console.Error.WriteLine("Usage: modlet-builder build --src <path> [<path> ...] --out <mod-dir> [--recursive] [--dry-run]");
            return 64;
        }

        var allDiagnostics = new List<Diagnostic>();

        var (files, discoverDiagnostics) = SourceDiscoverer.Discover(options.Sources, options.Recursive);
        allDiagnostics.AddRange(discoverDiagnostics);

        var fragments = new List<Fragment>();
        foreach (var file in files)
        {
            var (fragment, parseDiagnostics) = FragmentParser.Parse(file);
            allDiagnostics.AddRange(parseDiagnostics);
            if (fragment is not null)
                fragments.Add(fragment);
        }

        IReadOnlyList<Fragment> ordered = fragments;
        if (!HasErrors(allDiagnostics))
        {
            var (resolvedFragments, resolveDiagnostics) = DependencyResolver.Resolve(fragments);
            allDiagnostics.AddRange(resolveDiagnostics);
            ordered = resolvedFragments;
        }

        if (!HasErrors(allDiagnostics))
        {
            var generateDiagnostics = OutputGenerator.Generate(ordered, options.OutputDir, options.DryRun);
            allDiagnostics.AddRange(generateDiagnostics);
        }

        foreach (var d in allDiagnostics)
        {
            var writer = d.Severity == DiagnosticSeverity.Error ? Console.Error : Console.Out;
            writer.WriteLine(d.ToString());
        }

        if (HasErrors(allDiagnostics))
            return 1;

        if (options.DryRun)
            Console.WriteLine("Dry run completed successfully. No files were written.");
        else
            Console.WriteLine($"Build complete. Output written to '{options.OutputDir}'.");

        return 0;
    }

    private static bool HasErrors(IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

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
        writer.WriteLine("  build                              Assemble fragments into output config files.");
        writer.WriteLine();
        writer.WriteLine("'build' command options:");
        writer.WriteLine("      --src <path> [<path> ...]  One or more source files or directories");
        writer.WriteLine("                                 containing *.frag.xml files. Required.");
        writer.WriteLine("      --out <mod-dir>            Output mod root directory. Required.");
        writer.WriteLine("                                 Generated XML is written to {mod-dir}/Config/.");
        writer.WriteLine("      --recursive                Scan source directories recursively.");
        writer.WriteLine("      --dry-run                  Validate sources and simulate the build");
        writer.WriteLine("                                 without writing any files to the output folder.");
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
