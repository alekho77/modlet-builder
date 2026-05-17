using System.Text;
using System.Xml;
using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Generation;

internal static class ModInfoGenerator
{
    internal const string RelativePath = "ModInfo.xml";

    internal static IReadOnlyList<Diagnostic> Generate(
        ModInfo? modInfo,
        string outDir,
        bool dryRun,
        BuildLogger logger)
    {
        var diagnostics = new List<Diagnostic>();

        if (modInfo is null)
            return diagnostics;

        if (dryRun)
        {
            logger.Information($"[DRY RUN] Mod metadata -> {RelativePath}.");
            return diagnostics;
        }

        try
        {
            Directory.CreateDirectory(outDir);
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not create output directory '{outDir}': {ex.Message}"));
            return diagnostics;
        }

        var outputPath = Path.Combine(outDir, RelativePath);
        try
        {
            WriteModInfoFile(outputPath, modInfo);
            logger.Debug($"Written '{outputPath}'.");
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not write '{outputPath}': {ex.Message}"));
        }

        return diagnostics;
    }

    internal static void WriteModInfoFile(string outputPath, ModInfo modInfo)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            OmitXmlDeclaration = true,
        };

        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var declaration = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        stream.Write(declaration, 0, declaration.Length);

        using var writer = XmlWriter.Create(stream, settings);
        writer.WriteStartElement("ModInfo");
        WriteValueElement(writer, "Name", modInfo.Name);
        WriteValueElement(writer, "DisplayName", modInfo.DisplayName);
        WriteValueElement(writer, "Description", modInfo.Description);
        WriteValueElement(writer, "Author", modInfo.Author);
        WriteValueElement(writer, "Version", modInfo.Version);
        WriteValueElement(writer, "Website", modInfo.Website);
        writer.WriteEndElement();
        writer.Flush();
        stream.WriteByte((byte)'\n');
    }

    private static void WriteValueElement(XmlWriter writer, string name, string value)
    {
        writer.WriteStartElement(name);
        writer.WriteAttributeString("value", value);
        writer.WriteEndElement();
    }
}
