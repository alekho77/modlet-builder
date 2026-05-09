using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.SourceDiscovery;

internal static class SourceDiscoverer
{
    private const string FragmentExtension = ".frag.xml";
    private const string FragmentPattern = "*.frag.xml";

    internal static (IReadOnlyList<string> Files, IReadOnlyList<Diagnostic> Diagnostics) Discover(
        string[] paths,
        bool recursive)
    {
        var diagnostics = new List<Diagnostic>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new List<string>();

        var searchOption = recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                if (!path.EndsWith(FragmentExtension, StringComparison.OrdinalIgnoreCase))
                {
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Error,
                        $"File does not have the '{FragmentExtension}' extension.",
                        path));
                    continue;
                }

                var full = Path.GetFullPath(path);
                if (seen.Add(full))
                    files.Add(full);
            }
            else if (Directory.Exists(path))
            {
                var found = Directory.EnumerateFiles(path, FragmentPattern, searchOption);
                foreach (var f in found)
                {
                    var full = Path.GetFullPath(f);
                    if (seen.Add(full))
                        files.Add(full);
                }
            }
            else
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    "Path does not exist.",
                    path));
            }
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);
        return (files, diagnostics);
    }
}
