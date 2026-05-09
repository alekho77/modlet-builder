namespace ModletBuilder.Core.Models;

internal sealed record BuildOptions(
    string[] Sources,
    string OutputDir,
    bool Recursive,
    bool DryRun);
