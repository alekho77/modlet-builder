namespace ModletBuilder.Core.Models;

internal sealed record BuildOptions(
    string[] Sources,
    string? OutputDir,
    bool Recursive,
    bool DryRun,
    bool Clean,
    VerbosityLevel Verbosity,
    string? ProjectFile = null)
{
    internal IReadOnlyList<SourceSpec> ToSourceSpecs() =>
        Sources.Select(source => new SourceSpec(source, Recursive)).ToArray();
}
