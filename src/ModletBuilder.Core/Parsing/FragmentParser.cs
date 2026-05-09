using System.Xml;
using System.Xml.Linq;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Parsing;

internal static class FragmentParser
{
    internal static (Fragment? Fragment, IReadOnlyList<Diagnostic> Diagnostics) Parse(string filePath)
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
            return (null, diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not read file: {ex.Message}",
                filePath));
            return (null, diagnostics);
        }

        var root = doc.Root;

        if (root is null || root.Name.LocalName != "fragment")
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Root element must be <fragment>.",
                filePath));
            return (null, diagnostics);
        }

        var name = root.Attribute("name")?.Value;
        if (string.IsNullOrWhiteSpace(name))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "Fragment is missing required attribute 'name'.",
                filePath));
        }

        var target = root.Attribute("target")?.Value;
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

        var requiresAttr = root.Attribute("requires")?.Value ?? string.Empty;
        var requires = requiresAttr
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var body = root.Elements().ToList();

        return (new Fragment(name!, target!, requires, filePath, body), diagnostics);
    }
}
