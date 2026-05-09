using System.Xml;
using System.Xml.Linq;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Parsing;

internal static class FragmentParser
{
    internal static (IReadOnlyList<Fragment> Fragments, IReadOnlyList<Diagnostic> Diagnostics) Parse(string filePath)
    {
        var diagnostics = new List<Diagnostic>();
        XDocument doc;

        try
        {
            doc = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Malformed XML: {ex.Message}",
                filePath));
            return ([], diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not read file: {ex.Message}",
                filePath));
            return ([], diagnostics);
        }

        var root = doc.Root;

        if (root is null || root.Name.LocalName != "modlet")
        {
            var found = root is null ? "(empty document)" : $"<{root.Name.LocalName}>";
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Root element must be <modlet>, found {found}.",
                filePath));
            return ([], diagnostics);
        }

        // Read modlet-level hint (inherited by any fragment that has no own hint).
        var modletHints = ParseHintAttribute(root.Attribute("hint")?.Value);

        // Report unexpected non-<fragment> children
        foreach (var child in root.Elements().Where(e => e.Name.LocalName != "fragment"))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Unexpected element <{child.Name.LocalName}> inside <modlet>. Only <fragment> elements are allowed.",
                filePath));
        }

        var fragmentElements = root.Elements()
            .Where(e => e.Name.LocalName == "fragment")
            .ToList();

        if (fragmentElements.Count == 0)
        {
            // Only add the "no fragments" diagnostic when no more specific errors were already reported
            if (diagnostics.Count == 0)
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    "Source document <modlet> contains no <fragment> elements.",
                    filePath));
            }
            return ([], diagnostics);
        }

        var fragments = new List<Fragment>(fragmentElements.Count);
        foreach (var el in fragmentElements)
        {
            var (fragment, fragDiagnostics) = ParseFragmentElement(el, filePath, modletHints);
            diagnostics.AddRange(fragDiagnostics);
            if (fragment is not null)
                fragments.Add(fragment);
        }

        return (fragments, diagnostics);
    }

    private static (Fragment? Fragment, IReadOnlyList<Diagnostic> Diagnostics) ParseFragmentElement(
        XElement el, string filePath, string[]? modletHints)
    {
        var diagnostics = new List<Diagnostic>();

        var name = el.Attribute("name")?.Value;
        if (string.IsNullOrWhiteSpace(name))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Fragment is missing required attribute 'name'.",
                filePath));
        }

        var target = el.Attribute("target")?.Value;
        if (string.IsNullOrWhiteSpace(target))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Fragment is missing required attribute 'target'.",
                filePath));
        }
        else if (!KnownTargets.IsKnown(target))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Unknown target '{target}'. Must be one of the known target values.",
                filePath));
            target = null;
        }

        if (diagnostics.Count > 0)
            return (null, diagnostics);

        var requiresAttr = el.Attribute("requires")?.Value ?? string.Empty;
        var requires = requiresAttr
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Fragment-level hint overrides modlet-level hint; null means no hint at either level.
        var fragmentHints = ParseHintAttribute(el.Attribute("hint")?.Value);
        var effectiveHints = fragmentHints ?? modletHints;

        var body = el.Elements().ToList();

        return (new Fragment(name!, target!, requires, filePath, body)
        {
            RawHints = effectiveHints,
        }, diagnostics);
    }

    private static string[]? ParseHintAttribute(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts : null;
    }
}
