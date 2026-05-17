# modlet-builder Reference

`modlet-builder` assembles modular XML source fragments into deterministic
7 Days to Die mod output. This reference documents current tool behavior.

## CLI

Show tool info:

```bash
modlet-builder
```

Show help and version:

```bash
modlet-builder --help
modlet-builder --version
```

Build directly from source fragments:

```bash
modlet-builder build --src path/to/src --out path/to/Mods/EV_MyMod --recursive
```

Build from a project YAML file:

```bash
modlet-builder build --proj path/to/project.yml
```

Build with project output root override:

```bash
modlet-builder build --proj path/to/project.yml --out path/to/output-root
```

Validation-only run:

```bash
modlet-builder build --src src --out dist/EV_MyMod --recursive --dry-run
```

Build options:

| Option | Meaning |
| --- | --- |
| `--src <path> [<path> ...]` | One or more `*.frag.xml` files or directories |
| `--proj <file.yml>` | Project YAML with metadata, output, readme, and sources |
| `--out <path>` | Source mode: final mod directory. Project mode: output root override |
| `--recursive` | Recursively scan CLI `--src` directories |
| `--dry-run` | Parse, validate, and report without creating, deleting, or writing files |
| `--clean` | Delete the final mod output directory before writing; ignored with `--dry-run` |
| `--verbosity <level>` | `debug`, `information`, `warning`, `error`, or `none` |

## Source Discovery

- Source files must use the `.frag.xml` extension.
- Directory sources scan for `*.frag.xml`; use `--recursive` or per-project
  `recursive: true` to include subdirectories.
- Explicit files and directories can be mixed.
- Discovered files are deduplicated and sorted deterministically before parsing.

## Fragment Source Format

Each source document has a `<modlet>` root with direct child `<fragment>` and
optional `<localization>` blocks.

```xml
<modlet>
  <localization key="evSampleItemDesc" context="Item description">
    <english text="A custom item added by the mod."/>
    <russian text="Custom item description in Russian."/>
  </localization>

  <fragment name="ev-sample.items.base" target="items">
    <append xpath="/items">
      <item name="evSampleItem">
        <property name="Extends" value="meleeToolPickaxeT1Iron"/>
        <property name="DescriptionKey" value="evSampleItemDesc"/>
      </item>
    </append>
  </fragment>

  <fragment target="recipes" requires="ev-sample.items.base">
    <append xpath="/recipes">
      <recipe name="evSampleItem" count="1" craft_time="10">
        <ingredient name="resourceWood" count="10"/>
      </recipe>
    </append>
  </fragment>
</modlet>
```

`<modlet>` accepts no attributes. Only `<fragment>` and `<localization>` are
allowed as direct child elements.

Fragment attributes:

| Attribute | Required | Meaning |
| --- | --- | --- |
| `target` | Yes | Output config target; must be one of the known target values |
| `name` | Only when referenced | Public dependency ID for `requires` |
| `requires` | No | Comma-separated list of fragment `name` values that must come first |

Rules:

- Build metadata attributes are stripped from generated XML.
- Only child elements of `<fragment>` are emitted.
- Do not put `<localization>` inside `<fragment>`.
- Omit `name` for fragments that are not referenced by another fragment.
- Use names like `{mod}.{target}.{role}` or `{mod}-{feature}.{target}.{role}`.

## Dependency Ordering

- The tool topologically sorts fragments by `requires`.
- Missing dependencies, duplicate fragment names, and cycles are build errors.
- Unrelated fragments are still ordered deterministically.
- Named fragments sort before unnamed fragments when no dependency decides order.
- Dependencies can cross source files and targets.

## Known Targets

| Target | Output path |
| --- | --- |
| `items` | `Config/items.xml` |
| `blocks` | `Config/blocks.xml` |
| `recipes` | `Config/recipes.xml` |
| `loot` | `Config/loot.xml` |
| `entityclasses` | `Config/entityclasses.xml` |
| `entitygroups` | `Config/entitygroups.xml` |
| `buffs` | `Config/buffs.xml` |
| `progression` | `Config/progression.xml` |
| `gamestages` | `Config/gamestages.xml` |
| `spawning` | `Config/spawning.xml` |
| `traders` | `Config/traders.xml` |
| `vehicles` | `Config/vehicles.xml` |
| `item_modifiers` | `Config/item_modifiers.xml` |
| `quests` | `Config/quests.xml` |
| `biomes` | `Config/biomes.xml` |
| `sounds` | `Config/sounds.xml` |
| `materials` | `Config/materials.xml` |
| `shapes` | `Config/shapes.xml` |
| `qualityinfo` | `Config/qualityinfo.xml` |
| `worldglobal` | `Config/worldglobal.xml` |
| `weathersurvival` | `Config/weathersurvival.xml` |
| `painting` | `Config/painting.xml` |
| `nav_objects` | `Config/nav_objects.xml` |
| `archetypes` | `Config/archetypes.xml` |
| `dialogs` | `Config/dialogs.xml` |
| `npc` | `Config/npc.xml` |
| `challenges` | `Config/challenges.xml` |
| `events` | `Config/events.xml` |
| `gameevents` | `Config/gameevents.xml` |
| `rwgmixer` | `Config/rwgmixer.xml` |
| `utilityai` | `Config/utilityai.xml` |
| `misc` | `Config/misc.xml` |
| `physicsbodies` | `Config/physicsbodies.xml` |
| `ui_display` | `Config/ui_display.xml` |
| `music` | `Config/music.xml` |
| `subtitles` | `Config/subtitles.xml` |
| `dmscontent` | `Config/dmscontent.xml` |
| `twitch` | `Config/twitch.xml` |
| `twitch_events` | `Config/twitch_events.xml` |
| `videos` | `Config/videos.xml` |
| `loadingscreen` | `Config/loadingscreen.xml` |
| `blockplaceholders` | `Config/blockplaceholders.xml` |
| `xui_windows` | `Config/XUi/windows.xml` |
| `xui_controls` | `Config/XUi/controls.xml` |
| `xui_styles` | `Config/XUi/styles.xml` |
| `xui_menu_windows` | `Config/XUi_Menu/windows.xml` |
| `xui_menu_controls` | `Config/XUi_Menu/controls.xml` |
| `xui_menu_styles` | `Config/XUi_Menu/styles.xml` |
| `xui_common_controls` | `Config/XUi_Common/controls.xml` |
| `xui_common_styles` | `Config/XUi_Common/styles.xml` |

Unknown targets are hard errors. Custom target extensibility is not currently
implemented.

## Localization Blocks

`<localization>` blocks are build metadata and are emitted into
`Config/Localization.txt`.

Attributes:

| Attribute | Required | Meaning |
| --- | --- | --- |
| `key` | Yes | Unique localization key |
| `file` | No | `Localization.txt` `File` column; can be auto-derived for some objects |
| `type` | No | `Localization.txt` `Type` column; can be auto-derived for some objects |
| `usedInMainMenu` | No | `UsedInMainMenu` column |
| `noTranslate` | No | `true` or `1` emits `x`; `false`, `0`, or absent emits empty |
| `context` | No | `Context / Alternate Text` column |

Supported language child elements:

```text
english, german, spanish, french, italian, japanese, koreana, polish, brazilian, russian, turkish, schinese, tchinese
```

Each language element requires a `text` attribute.

Auto-derivation for `file` and `type` works when a matching
`<property name="DescriptionKey" value="..."/>` is found under:

| Parent element | `file` | `type` |
| --- | --- | --- |
| `<item>` | `items` | `Item` |
| `<block>` | `blocks` | `Block` |
| `<item_modifier>` | `item_modifiers` | `Mod` |

Validation rules:

- Duplicate localization keys are build errors.
- Every localization key must be referenced by a `DescriptionKey` property in the
  same build.
- `DescriptionKey` in unsupported targets produces a warning because the game will
  ignore it there.

## Project YAML

Project files describe one concrete generated mod folder.

```yaml
modFolder: EV_LootBox
output: dist
readme: readme.md

modInfo:
  name: EV_LootBox
  displayName: Loot Box
  description: Adds a loot box with Simple, Good, and Valuable reward categories.
  author: Aleksei Khozin
  version: 0.1.0
  website: https://github.com/alekho77/epic_7d2d_mods

sources:
  - path: src
    recursive: false
  - shared/common.frag.xml
```

Required project fields:

- `modFolder`
- `output`
- `modInfo.name`
- `modInfo.displayName`
- `modInfo.description`
- `modInfo.author`
- `modInfo.version`
- `modInfo.website`
- at least one `sources` entry

Rules:

- Project paths are resolved relative to the project file.
- String source entries default to `recursive: false`.
- Mapping source entries use `path` and optional boolean `recursive`.
- `readme` is optional; when present, the file must exist.
- In project mode, `--out` overrides the output root only. Final output becomes
  `{--out}/{modFolder}`.

## Generated Output

Source-mode build writes generated config files under the `--out` mod directory:

```text
EV_MyMod/
  Config/
    items.xml
    recipes.xml
```

Project-mode build can also write:

```text
EV_MyMod/
  ModInfo.xml
  README.md
  NEXUS_DESCRIPTION.bbcode
  Config/
    Localization.txt
    items.xml
```

Current generated config XML shape:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<config>
  <!-- fragment body elements in dependency-resolved order -->
</config>
```

Current generated `ModInfo.xml` shape:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ModInfo>
  <Name value="EV_MyMod"/>
  <DisplayName value="My Mod"/>
  <Description value="Short description."/>
  <Author value="Aleksei Khozin"/>
  <Version value="1.0.0"/>
  <Website value="https://example.com"/>
</ModInfo>
```

The tool currently uses `NEXUS_DESCRIPTION.bbcode` for generated Nexus text.

## Diagnostics and Exit Behavior

- Usage errors return a usage error exit code.
- Build errors return a build error exit code.
- Warning-only builds can still succeed.
- Errors are intended to include the source path and enough context to fix the
  source document, project file, or dependency graph.
- `--dry-run` performs parsing, source discovery, dependency resolution,
  localization validation, and output reporting without touching the filesystem.
