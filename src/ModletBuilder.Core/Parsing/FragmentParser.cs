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
            doc = XDocument.Load(filePath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
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

        // Validate that <modlet> carries no attributes.
        foreach (var attr in root.Attributes())
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Unknown attribute '{attr.Name.LocalName}' on <modlet> element. " +
                "The <modlet> element does not accept any attributes.",
                filePath));
        }

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
        for (var i = 0; i < fragmentElements.Count; i++)
        {
            var el = fragmentElements[i];
            var (fragment, fragDiagnostics) = ParseFragmentElement(el, filePath, i);
            diagnostics.AddRange(fragDiagnostics);
            if (fragment is not null)
                fragments.Add(fragment);
        }

        return (fragments, diagnostics);
    }

    private static (Fragment? Fragment, IReadOnlyList<Diagnostic> Diagnostics) ParseFragmentElement(
        XElement el, string filePath, int fragmentOrdinal)
    {
        var diagnostics = new List<Diagnostic>();

        var nameAttr = el.Attribute("name")?.Value;
        var name = string.IsNullOrWhiteSpace(nameAttr) ? null : nameAttr;

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

        // Validate that no unknown attributes are present.
        var knownAttribs = new HashSet<string>(StringComparer.Ordinal) { "name", "target", "requires" };
        foreach (var attr in el.Attributes())
        {
            if (!knownAttribs.Contains(attr.Name.LocalName))
            {
                var fragLabel = DescribeFragment(name, CreateInternalId(el, filePath, fragmentOrdinal));
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Unknown attribute '{attr.Name.LocalName}' on fragment {fragLabel}. " +
                    "Allowed attributes are: name, target, requires.",
                    filePath));
            }
        }

        if (diagnostics.Count > 0)
            return (null, diagnostics);

        var requiresAttr = el.Attribute("requires")?.Value ?? string.Empty;
        var requires = requiresAttr
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var body = el.Elements().ToList();

        var internalId = CreateInternalId(el, filePath, fragmentOrdinal);
        return (new Fragment(internalId, name, target!, requires, filePath, body), diagnostics);
    }

    private static string CreateInternalId(XElement element, string filePath, int fragmentOrdinal)
    {
        var lineInfo = (IXmlLineInfo)element;
        var lineNumber = lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0;
        return $"{filePath}#L{lineNumber}#F{fragmentOrdinal}";
    }

    private static string DescribeFragment(string? name, string internalId) =>
        name is null ? $"(unnamed fragment at {internalId})" : $"'{name}'";
}
