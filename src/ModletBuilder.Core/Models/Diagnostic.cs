namespace ModletBuilder.Core.Models;

internal sealed record Diagnostic(
    DiagnosticSeverity Severity,
    string Message,
    string? SourceFile = null)
{
    public override string ToString() =>
        SourceFile is null
            ? $"{Severity.ToString().ToUpperInvariant()}: {Message}"
            : $"{Severity.ToString().ToUpperInvariant()}: {SourceFile}: {Message}";
}
