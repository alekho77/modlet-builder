namespace ModletBuilder.Cli;

internal static class CommandLine
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage(Console.Out);
            return 0;
        }

        var command = args[0];

        switch (command)
        {
            case "-h":
            case "--help":
                PrintUsage(Console.Out);
                return 0;

            case "--version":
                Console.WriteLine(GetVersion());
                return 0;

            case "build":
            case "validate":
                Console.Error.WriteLine($"Command '{command}' is not implemented yet.");
                return 2;

            default:
                Console.Error.WriteLine($"Unknown command: {command}");
                Console.Error.WriteLine();
                PrintUsage(Console.Error);
                return 64;
        }
    }

    private static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine();
        writer.WriteLine("Usage: modlet-builder [options]");
        writer.WriteLine("Usage: modlet-builder [command] [arguments]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  -h|--help         Display help.");
        writer.WriteLine("  --version         Display tool version.");
        writer.WriteLine();
        writer.WriteLine("Commands:");
        writer.WriteLine("  build             Assemble fragments into output config files.");
        writer.WriteLine("  validate          Validate sources without generating output.");
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
