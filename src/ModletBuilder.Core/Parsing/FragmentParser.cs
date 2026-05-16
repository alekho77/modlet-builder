using System.Xml;
using System.Xml.Linq;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Parsing;

internal static class FragmentParser
{
    internal static (IReadOnlyList<Fragment> Fragments, IReadOnlyList<LocalizationEntry> LocalizationEntries, IReadOnlyList<Diagnostic> Diagnostics) Parse(string filePath)
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
            return ([], [], diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not read file: {ex.Message}",
                filePath));
            return ([], [], diagnostics);
        }

        var root = doc.Root;

        if (root is null || root.Name.LocalName != "modlet")
        {
            var found = root is null ? "(empty document)" : $"<{root.Name.LocalName}>";
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Root element must be <modlet>, found {found}.",
                filePath));
            return ([], [], diagnostics);
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

        // Report unexpected children — only <fragment> and <localization> are allowed at modlet level.
        foreach (var child in root.Elements()
            .Where(e => e.Name.LocalName != "fragment" && e.Name.LocalName != "localization"))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Unexpected element <{child.Name.LocalName}> inside <modlet>. " +
                "Only <fragment> and <localization> elements are allowed.",
                filePath));
        }

        // ── Parse <localization> blocks at modlet level ───────────────────────
        var localizationEntries = new List<LocalizationEntry>();
        foreach (var locEl in root.Elements().Where(e => e.Name.LocalName == "localization"))
        {
            var (entry, locDiagnostics) = ParseLocalizationElement(locEl, filePath);
            diagnostics.AddRange(locDiagnostics);
            if (entry is not null)
                localizationEntries.Add(entry);
        }

        // ── Parse <fragment> elements ─────────────────────────────────────────
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
            return ([], localizationEntries, diagnostics);
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

        return (fragments, localizationEntries, diagnostics);
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

        // Warn about unknown attributes but still parse the fragment.
        var knownAttribs = new HashSet<string>(StringComparer.Ordinal) { "name", "target", "requires" };
        foreach (var attr in el.Attributes())
        {
            if (!knownAttribs.Contains(attr.Name.LocalName))
            {
                var fragLabel = DescribeFragment(name, CreateInternalId(el, filePath, fragmentOrdinal));
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Warning,
                    $"Unknown attribute '{attr.Name.LocalName}' on fragment {fragLabel}. " +
                    "Allowed attributes are: name, target, requires.",
                    filePath));
            }
        }

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return (null, diagnostics);

        var requiresAttr = el.Attribute("requires")?.Value ?? string.Empty;
        var requires = requiresAttr
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var internalId = CreateInternalId(el, filePath, fragmentOrdinal);

        // Detect misplaced <localization> blocks inside <fragment> and guide the user.
        foreach (var child in el.Elements().Where(c => c.Name.LocalName == "localization"))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"<localization> must be declared at the <modlet> level, not inside <fragment> " +
                $"{DescribeFragment(name, internalId)}. Move it to be a direct child of <modlet>.",
                filePath));
        }

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return (null, diagnostics);

        // All children of <fragment> are pure XML payload — written verbatim to the target file.
        var body = el.Elements().ToList();

        return (new Fragment(internalId, name, target!, requires, filePath, body), diagnostics);
    }

    private static (LocalizationEntry? Entry, IReadOnlyList<Diagnostic> Diagnostics) ParseLocalizationElement(
        XElement el, string filePath)
    {
        var diagnostics = new List<Diagnostic>();

        var key = el.Attribute("key")?.Value;
        var file = el.Attribute("file")?.Value ?? string.Empty;
        var type = el.Attribute("type")?.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(key))
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error,
                "Localization block is missing required attribute 'key'.", filePath));

        var usedInMainMenu = el.Attribute("usedInMainMenu")?.Value ?? string.Empty;
        var noTranslate = el.Attribute("noTranslate")?.Value ?? string.Empty;
        var context = el.Attribute("context")?.Value ?? string.Empty;

        var knownAttribs = new HashSet<string>(StringComparer.Ordinal)
            { "key", "file", "type", "usedInMainMenu", "noTranslate", "context" };
        foreach (var attr in el.Attributes())
        {
            if (!knownAttribs.Contains(attr.Name.LocalName))
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error,
                    $"Unknown attribute '{attr.Name.LocalName}' on <localization> block. " +
                    $"Allowed attributes are: key, file, type, usedInMainMenu, noTranslate, context.",
                    filePath));
            }
        }

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return (null, diagnostics);

        var seenLanguages = new HashSet<string>(StringComparer.Ordinal);
        var langValues = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var langEl in el.Elements())
        {
            var lang = langEl.Name.LocalName;

            if (!KnownLanguages.IsKnown(lang))
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error,
                    $"Unknown language element <{lang}> in <localization key=\"{key}\">. " +
                    $"Supported languages: {string.Join(", ", KnownLanguages.All)}.",
                    filePath));
                continue;
            }

            if (!seenLanguages.Add(lang))
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error,
                    $"Duplicate language element <{lang}> in <localization key=\"{key}\">.",
                    filePath));
                continue;
            }

            var text = langEl.Attribute("text")?.Value;
            if (text is null)
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error,
                    $"Language element <{lang}> in <localization key=\"{key}\"> is missing required attribute 'text'.",
                    filePath));
                continue;
            }

            langValues[lang] = text;
        }

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return (null, diagnostics);

        var entry = new LocalizationEntry(
            Key: key!,
            File: file,
            Type: type,
            UsedInMainMenu: usedInMainMenu,
            NoTranslate: noTranslate,
            Context: context,
            Languages: langValues,
            SourceFile: filePath);

        return (entry, diagnostics);
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
