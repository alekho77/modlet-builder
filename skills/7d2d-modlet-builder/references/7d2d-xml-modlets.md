# 7 Days to Die XML Modlets

This reference covers XML-first 7 Days to Die modlets. It is intentionally focused
on data modding, not Harmony DLLs or total conversion asset pipelines.

## Ground Rules

- The game loads vanilla XML from `Data/Config`, then applies mod XML patches from
  installed `Mods/<ModName>/Config` folders in alphabetical mod folder order.
- Mod XML files patch the in-memory vanilla XML tree. They should not replace
  vanilla files wholesale unless the user explicitly wants a full replacement
  strategy and accepts the compatibility cost.
- The mod folder must contain `ModInfo.xml` for the game to load it.
- XML-only modlets are usually server-side friendly when they only change synced
  game data. Client-side installation is needed for custom assets, icons, sounds,
  UI files that clients must render, or DLL/Harmony behavior.
- Test without EAC when possible. Harmony/DLL mods require EAC disabled; XML-only
  mods can often work with EAC but should still be tested in a modding launch.

## Native Modlet Layout

```text
Mods/
  EV_MyMod/
    ModInfo.xml
    README.md
    NEXUS_DESCRIPTION.bbcode
    Config/
      items.xml
      blocks.xml
      recipes.xml
      Localization.txt
      XUi/
        windows.xml
    Resources/
    UIAtlases/
    Harmony/
```

Use one modlet for one focused feature or balance concern. For EpicVales-style
mods, use the `EV_` folder prefix and PascalCase names with no spaces.

## XPath Patch File Shape

Native handwritten 7 Days to Die patch files are commonly wrapped in `<configs>`:

```xml
<configs>
  <set xpath="/items/item[@name='gunPistol']/property[@name='DamageEntity']/@value">50</set>

  <append xpath="/items">
    <item name="evCustomItem">
      <property name="Extends" value="meleeToolPickaxeT1Iron"/>
      <property name="CustomIcon" value="meleeToolPickaxeT1Iron"/>
    </item>
  </append>
</configs>
```

When using `modlet-builder`, author the patch operations inside `<fragment>`
elements instead. The current tool writes generated config files with a `<config>`
root; document and preserve that current behavior unless changing the generator is
explicitly in scope.

## Common XPath Operations

| Operation | Purpose | Typical shape |
| --- | --- | --- |
| `set` | Change text or attribute value | `<set xpath=".../@value">50</set>` |
| `append` | Add children to a selected parent | `<append xpath="/items">...</append>` |
| `insertAfter` | Insert after a selected sibling | `<insertAfter xpath="...">...</insertAfter>` |
| `insertBefore` | Insert before a selected sibling | `<insertBefore xpath="...">...</insertBefore>` |
| `remove` | Delete a selected node | `<remove xpath="/items/item[@name='oldItem']"/>` |
| `removeattribute` | Remove an attribute | `<removeattribute xpath="..." name="count"/>` |
| `setattribute` | Add or replace an attribute | `<setattribute xpath="..." name="count" value="5"/>` |

Use exact paths where practical. Avoid broad `//` selectors unless the vanilla XML
shape makes a precise path brittle or impossible.

## Lookup Workflow

1. Resolve the user-facing object name to an internal ID. Search a local catalog
   such as `docs/inventory_catalog.md` first when available.
2. Read the relevant vanilla XML from `Data/Config` for the internal ID.
3. Check neighboring vanilla objects for inheritance, property naming, effects,
   display types, unlocks, recipes, loot groups, and localization patterns.
4. Write the smallest patch that expresses the requested behavior.
5. Validate XPath targets against the vanilla file before generating output.

Important config files:

| File | Controls |
| --- | --- |
| `items.xml` | Items, weapons, tools, consumables, ammo, armor, action properties, effects |
| `blocks.xml` | Placeable blocks, workstations, doors, traps, containers, crops, materials |
| `recipes.xml` | Crafting recipes, ingredients, craft area, unlock tags, craft time |
| `loot.xml` | Loot containers, groups, probabilities, quality templates |
| `entityclasses.xml` | Zombies, animals, NPCs, traders, entity stats and AI properties |
| `entitygroups.xml` | Spawn group membership and weights |
| `buffs.xml` | Buffs, debuffs, triggered effects, passive effects, requirements |
| `progression.xml` | Attributes, perks, crafting skills, level gates, unlocks |
| `gamestages.xml` | Horde and wave scaling by game stage |
| `spawning.xml` | Biome and zone spawn rules |
| `traders.xml` | Trader inventories, tiers, quest offerings |
| `vehicles.xml` | Vehicle stats, storage, fuel, crafting relationships |
| `item_modifiers.xml` | Weapon/tool mods, dyes, attachments |
| `qualityinfo.xml` | Quality tier scaling |
| `XUi/*` | In-game HUD and UI layout/style patches |
| `XUi_Menu/*` | Main menu UI patches |
| `XUi_Common/*` | Shared UI controls and styles |
| `Localization.txt` | Text keys and translations |

## Inheritance and Data Patterns

- Prefer `Extends` when adding a new item, block, or entity that is a variant of a
  vanilla object.
- Override only the properties that need to differ from the parent.
- Add `CustomIcon` or `CustomIconTint` when reusing a vanilla icon for a new item.
- Add recipes in `recipes.xml` and unlock links in `progression.xml` when the new
  object should be craftable through normal progression.
- Add loot entries in `loot.xml` only after checking the existing group structure
  and probability style.
- Use `CreativeMode` deliberately. `None` hides from the creative menu but does
  not necessarily make an object unavailable through console commands.

## Localization

Mod localization rows are appended to the game's localization data. The canonical
column order is `Key`, `File`, `Type`, `UsedInMainMenu`, `NoTranslate`,
`english`, `Context / Alternate Text`, `german`, `spanish`, `french`, `italian`,
`japanese`, `koreana`, `polish`, `brazilian`, `russian`, `turkish`, `schinese`,
and `tchinese`.

Rules:

- Use stable, unique keys.
- For object descriptions, prefer `{objectName}Desc`.
- Link descriptions from XML with `<property name="DescriptionKey" value="..."/>`.
- Use `NoTranslate` value `x` for proper names or text that should remain
  unchanged across languages.
- Keep localization close to the fragment or patch that introduces the object.

## Debugging

- Check the game log after launch. XPath failures normally identify the mod file
  and line.
- If a patch does nothing, verify the XPath against the vanilla XML for the target
  game version.
- If an object appears but has missing text, verify `DescriptionKey` and
  localization key spelling.
- If a recipe, loot entry, perk unlock, or entity reference does not work, search
  all related vanilla files for the same ID and expected cross-file references.

## Boundaries

- Use Harmony only when XML cannot express the behavior.
- Use total conversion asset workflows only for custom models, textures, bundles,
  sounds, animations, prefabs, or full UI replacement.
- Do not introduce DLL or asset pipeline requirements for a request that can be
  solved with XML data patches.
