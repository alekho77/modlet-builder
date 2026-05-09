namespace ModletBuilder.Cli;

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
                Console.Error.WriteLine($"Command '{command}' is not implemented yet.");
                return 2;

            default:
                Console.Error.WriteLine($"Unknown command: {command}");
                Console.Error.WriteLine();
                PrintHelp(Console.Error);
                return 64;
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
        writer.WriteLine("  build                              Assemble fragments into output config files.");
        writer.WriteLine();
        writer.WriteLine("'build' command options:");
        writer.WriteLine("      --dry-run                      Validate sources and simulate the build");
        writer.WriteLine("                                     without writing any files to the output folder.");
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
