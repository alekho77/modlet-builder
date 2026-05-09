using System.Xml.Linq;

namespace ModletBuilder.Core.Models;

internal sealed record Fragment(
    string Name,
    string Target,
    string[] Requires,
    string SourceFile,
    IReadOnlyList<XElement> Body);
