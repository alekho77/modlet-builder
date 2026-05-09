using System.Xml.Linq;

namespace ModletBuilder.Core.Models;

internal sealed record Fragment(
    string Name,
    string Target,
    string[] Requires,
    string SourceFile,
    IReadOnlyList<XElement> Body)
{
    // Mod names declared by the hint attribute (fragment hint overrides modlet hint).
    // Null means no hint was specified in XML; mod assignment is resolved from
    // --targets during registry building. An empty array is treated as null.
    internal string[]? RawHints { get; init; }
}
