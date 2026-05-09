using System.Text;
using System.Xml;
using System.Xml.Linq;
using ModletBuilder.Core.Models;
using ModletBuilder.Core.Parsing;

namespace ModletBuilder.Core.Generation;

internal static class OutputGenerator
{
    internal static IReadOnlyList<Diagnostic> Generate(
        IReadOnlyList<Fragment> orderedFragments,
        string outputDir,
        bool dryRun)
    {
        var diagnostics = new List<Diagnostic>();
        var configDir = Path.Combine(outputDir, "Config");

        if (dryRun)
        {
            if (!Directory.Exists(outputDir))
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Output directory does not exist: '{outputDir}'."));
                return diagnostics;
            }

            if (!Directory.Exists(configDir))
            {
                try
                {
                    Directory.CreateDirectory(configDir);
                    Directory.Delete(configDir);
                }
                catch (Exception ex)
                {
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Error,
                        $"Cannot create Config directory under '{outputDir}': {ex.Message}"));
                }
            }

            return diagnostics;
        }

        // Group fragments by target, preserving the resolved order within each group
        var byTarget = new Dictionary<string, List<Fragment>>(StringComparer.Ordinal);
        foreach (var fragment in orderedFragments)
        {
            if (!byTarget.TryGetValue(fragment.Target, out var list))
            {
                list = [];
                byTarget[fragment.Target] = list;
            }
            list.Add(fragment);
        }

        foreach (var (target, targetFragments) in byTarget.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var relativePath = KnownTargets.GetFilePath(target);
            var outputPath = Path.Combine(configDir, relativePath.Replace('/', Path.DirectorySeparatorChar));

            var dir = Path.GetDirectoryName(outputPath)!;
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Could not create output directory '{dir}': {ex.Message}"));
                continue;
            }

            try
            {
                WriteConfigFile(outputPath, targetFragments);
            }
            catch (Exception ex)
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Could not write output file '{outputPath}': {ex.Message}"));
            }
        }

        return diagnostics;
    }

    private static void WriteConfigFile(string outputPath, List<Fragment> fragments)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            OmitXmlDeclaration = true,
        };

        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

        // Write the XML declaration manually so the encoding value is uppercase "UTF-8".
        var declaration = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        stream.Write(declaration, 0, declaration.Length);

        using var writer = XmlWriter.Create(stream, settings);

        var config = new XElement("config");
        foreach (var fragment in fragments)
        {
            foreach (var element in fragment.Body)
                config.Add(new XElement(element));
        }

        config.WriteTo(writer);
    }
}
