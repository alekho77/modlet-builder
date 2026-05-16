using System.Xml.Linq;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Resolution;

/// <summary>
/// Resolves the <see cref="LocalizationEntry.File"/> and <see cref="LocalizationEntry.Type"/>
/// fields for localization entries that omitted those attributes in source XML.
/// Values are derived by locating the <c>&lt;property name="DescriptionKey" value="key"/&gt;</c>
/// reference in the fragment bodies and inspecting the parent game object element.
/// </summary>
/// <remarks>
/// Supported parent elements and their derived values (verified against vanilla
/// 7 Days to Die <c>Data/Config/Localization.txt</c>):
/// <list type="table">
///   <item><term>item</term><description>file=items, type=Item</description></item>
///   <item><term>block</term><description>file=blocks, type=Block</description></item>
///   <item><term>item_modifier</term><description>file=item_modifiers, type=Mod</description></item>
/// </list>
/// When a key cannot be resolved (orphaned or in an unsupported target), the entry
/// is returned unchanged; the orphan validator downstream will emit the relevant error.
/// </remarks>
internal static class LocalizationAttributeResolver
{
    /// <summary>
    /// Maps game object element name → (File, Type) for <c>Localization.txt</c>.
    /// Verified against vanilla 7 Days to Die Data/Config/Localization.txt.
    /// </summary>
    private static readonly Dictionary<string, (string File, string Type)> ElementToLocalization =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["item"]          = ("items",          "Item"),
            ["block"]         = ("blocks",         "Block"),
            ["item_modifier"] = ("item_modifiers", "Mod"),
        };

    internal static IReadOnlyList<LocalizationEntry> Resolve(
        IReadOnlyList<LocalizationEntry> entries,
        IReadOnlyList<Fragment> fragments)
    {
        // Fast path: nothing to resolve.
        if (entries.All(e => !string.IsNullOrEmpty(e.File) && !string.IsNullOrEmpty(e.Type)))
            return entries;

        // Build key → (File, Type) lookup from all fragment bodies.
        var keyToAttributes = new Dictionary<string, (string File, string Type)>(StringComparer.Ordinal);
        foreach (var fragment in fragments)
        {
            foreach (var bodyElement in fragment.Body)
            {
                foreach (var element in bodyElement.DescendantsAndSelf())
                {
                    if (element.Name.LocalName != "property"
                        || element.Attribute("name")?.Value != "DescriptionKey"
                        || element.Attribute("value") is not XAttribute valueAttr)
                    {
                        continue;
                    }

                    var key = valueAttr.Value;
                    if (keyToAttributes.ContainsKey(key))
                        continue;

                    if (element.Parent is XElement parent
                        && ElementToLocalization.TryGetValue(parent.Name.LocalName, out var ft))
                    {
                        keyToAttributes[key] = ft;
                    }
                }
            }
        }

        var result = new List<LocalizationEntry>(entries.Count);
        foreach (var entry in entries)
        {
            if ((string.IsNullOrEmpty(entry.File) || string.IsNullOrEmpty(entry.Type))
                && keyToAttributes.TryGetValue(entry.Key, out var resolved))
            {
                result.Add(entry with
                {
                    File = string.IsNullOrEmpty(entry.File) ? resolved.File : entry.File,
                    Type = string.IsNullOrEmpty(entry.Type) ? resolved.Type : entry.Type,
                });
            }
            else
            {
                result.Add(entry);
            }
        }

        return result;
    }
}
