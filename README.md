# modlet-builder

A build tool for assembling modlets and XML patch fragments into final mod output, designed first for **7 Days to Die**.

`modlet-builder` reads modular XML source fragments, resolves their ordering and routing metadata, and generates final game-ready config files deterministically.

Instead of managing dozens of separate modlets with unpredictable load order, you write small focused fragment files. The tool assembles them into a single vanilla-compatible mod output with explicit, reproducible ordering.

## Quick Start for Modders

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) matching the version in [global.json](global.json).
2. Clone the repository:

   ```bash
   git clone https://github.com/yourusername/modlet-builder.git
   cd modlet-builder
   ```

3. Publish a self-contained single-file executable to the folder of your choice.
   Replace `C:\Tools\modlet-builder` with whatever path you want the binary in:

   **Windows (x64):**

   ```bash
   dotnet publish src/ModletBuilder.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o C:\Tools\modlet-builder
   ```

   **Linux (x64):**

   ```bash
   dotnet publish src/ModletBuilder.Cli -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ~/tools/modlet-builder
   ```

   **macOS (arm64):**

   ```bash
   dotnet publish src/ModletBuilder.Cli -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o ~/tools/modlet-builder
   ```

   This produces a single `modlet-builder` (or `modlet-builder.exe` on Windows) file.
   No .NET runtime is required on the machine where you run the tool.

4. Optionally, add the output folder to your `PATH` so you can call `modlet-builder` from anywhere.

### Basic usage

**What goes in:** one or more `*.frag.xml` source fragment files, or directories containing them.

**What comes out:** generated XML files inside `{mod-dir}/Config/`, ready to drop into your game's `Mods/` folder. Project builds also generate `{mod-dir}/ModInfo.xml`.

```bash
modlet-builder build --src path/to/my-fragments --out path/to/Mods/MyMod --recursive
```

This scans `path/to/my-fragments/` recursively for `*.frag.xml` files, resolves their order, and writes the assembled XML files into `path/to/Mods/MyMod/Config/`:

```text
path/to/Mods/MyMod/
└─ Config/
   ├─ items.xml
   └─ recipes.xml
```

Drop the `MyMod/` directory into your game's `Mods/` folder and you're done.

For a complete mod build with `ModInfo.xml`, use a project YAML file:

```bash
modlet-builder build --proj path/to/mod.proj.yml
```

To build the same source fragments into two different mods, run `build` twice with different `--out` values:

```bash
modlet-builder build --src src/ --out path/to/Mods/ModA
modlet-builder build --src src/ --out path/to/Mods/ModB
```

## Quick Start for Developers

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) matching the version in [global.json](global.json).
2. Clone the repository and open the root folder in VS Code or any editor with C# language support.
3. Build the solution to verify everything compiles:

   ```bash
   dotnet build ModletBuilder.sln
   ```

4. Run the tests:

   ```bash
   dotnet test ModletBuilder.sln
   ```

5. Start from the CLI entry point: [src/ModletBuilder.Cli/Program.cs](src/ModletBuilder.Cli/Program.cs).
   Command dispatching lives in [src/ModletBuilder.Cli/CommandLine.cs](src/ModletBuilder.Cli/CommandLine.cs).
   Core domain logic lives in `src/ModletBuilder.Core/`.
   Tests live in `src/ModletBuilder.Tests/`.

Build outputs are redirected to the repository-root `build/` folder via [Directory.Build.props](Directory.Build.props). Do not commit the `build/` folder.

To produce an optimized build:

```bash
dotnet build ModletBuilder.sln -c Release
```

### Publishing as a single executable

Self-contained single file (no .NET runtime required on the target machine):

```bash
dotnet publish src/ModletBuilder.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Native AOT (fully native binary):

```bash
dotnet publish src/ModletBuilder.Cli -c Release -r win-x64 -p:PublishAot=true
```

Published output lands at `build/bin/ModletBuilder.Cli/Release/net10.0/<runtime>/publish/`.

## Project Structure

```text
modlet-builder/
├─ README.md
├─ LICENSE
├─ global.json              — pinned .NET SDK version
├─ Directory.Build.props    — shared MSBuild properties (redirects build output)
├─ ModletBuilder.sln        — solution file
├─ docs/                    — design notes and specs
├─ src/
│  ├─ ModletBuilder.Cli/    — command-line entry point
│  ├─ ModletBuilder.Core/   — core domain logic: parsing, resolution, generation
│  └─ ModletBuilder.Tests/  — automated tests
├─ samples/                 — minimal working examples
└─ build/                   — generated build output (git-ignored)
```

Build output layout:

```text
build/
├─ bin/<ProjectName>/<Configuration>/<TargetFramework>/
└─ obj/<ProjectName>/<Configuration>/<TargetFramework>/
```

## Source Structure and Fragment Files

Each `*.frag.xml` file is a **source document** with root element `<modlet>`. A source document may contain one `<fragment>` or many. The logical build unit is the individual `<fragment>` element, not the file.

A single source document may contribute content to multiple output targets. Dependency resolution, validation, ordering, and target routing all operate per `<fragment>` element.

Source documents can be passed to `build` as explicit file paths, or collected automatically from a directory.

### Source document format

A source document with localization blocks and two fragments targeting different output files:

```xml
<modlet>

  <!-- Localization entries: build metadata, not part of final XML. -->
  <localization key="myItemDesc" file="items" type="Item">
    <english text="My item description"/>
    <russian text="Описание моего предмета"/>
  </localization>

  <!-- Fragments: pure XML patch payloads. -->
  <fragment name="my-mod.items.base" target="items">
    <append xpath="/items">
      <item name="myItem">
        <property name="DescriptionKey" value="myItemDesc"/>
      </item>
    </append>
  </fragment>

  <fragment target="recipes" requires="my-mod.items.base">
    <append xpath="/recipes">
      <!-- recipe definitions go here -->
    </append>
  </fragment>

</modlet>
```

### Attributes

| Attribute | Required | Description |
| --------- | -------- | ----------- |
| `name` | Only when referenced | Public identifier for this fragment. Required only when another fragment needs to reference it in `requires`. Suggested convention: `{mod}.{target}.{role}`. |
| `target` | Yes | The output config file this fragment contributes to. Must be one of the [known target values](#known-target-values). |
| `requires` | No | Comma-separated list of public `name` values this fragment depends on. The tool places all dependencies before this fragment in the resolved output order. |

Build-only metadata (`name`, `target`, `requires`) is stripped from all generated output. Only the child elements of `<fragment>` appear in the final XML.

Fragments that are not referenced by `requires` should omit `name`. The tool still assigns every fragment an internal deterministic source-location id for validation and ordering diagnostics; that internal id is not part of the source format and cannot be used in `requires`.

### Directory scanning

When a directory is passed as a source, the tool scans it for `*.frag.xml` files. Use `--recursive` to include subdirectories. Explicit file paths and directory paths can be mixed freely in the same command. All discovered files are deduplicated and sorted deterministically before processing.

Project YAML sources can set `recursive` per source entry, so one source directory can be recursive while another remains top-level only.

## `build` Command

Assembles source fragments into final game-ready XML files.

```text
modlet-builder build --src <path> [<path> ...] --out <mod-dir> [--recursive] [--dry-run] [--clean] [--verbosity <level>]
modlet-builder build --proj <file.yml> [--src <path> ...] [--out <output-root>] [--recursive] [--dry-run] [--clean] [--verbosity <level>]
```

### Options

| Option | Required | Description |
| ------ | -------- | ----------- |
| `--src <path> [<path> ...]` | Source mode: yes. Project mode: optional. | One or more source paths. In project mode these are appended to project `sources`. |
| `--proj <file.yml>` | Source mode: no. Project mode: yes. | Project YAML file with mod metadata, output root, mod folder name, and source entries. |
| `--out <path>` | Source mode: yes. Project mode: no. | In source mode, output mod directory. In project mode, overrides the YAML output root and final output becomes `{--out}/{modFolder}`. |
| `--recursive` | No | When a directory is given through CLI `--src`, scan its subdirectories recursively. Project YAML sources use their own per-entry `recursive` value. |
| `--dry-run` | No | Validate all source fragments, resolve dependencies, and report what would be written — without touching the filesystem at all. No directories are created or deleted. |
| `--clean` | No | Delete the final mod output directory before writing output. In project mode this is `{outputRoot}/{modFolder}`, not the higher-level output root. Ignored when combined with `--dry-run`. |
| `--verbosity <level>` | No | Controls how much is logged. One of: `debug`, `information` (default), `warning`, `error`, `none`. |

### Project file format

Project files are YAML. They describe mod-specific metadata and the source set for one concrete mod:

```yaml
modFolder: EV_LootBox
output: dist

modInfo:
  name: EV_LootBox
  displayName: Loot Box
  description: Adds a loot box with Simple, Good, and Valuable reward categories.
  author: Aleksei Khozin
  version: 0.1.0
  website: https://github.com/alekho77/epic_7d2d_mods

sources:
  - path: src
    recursive: true
  - path: shared/common.frag.xml
  - shared/non-recursive-dir
```

Project paths are resolved relative to the project file. `modFolder`, `output`, all six `modInfo` fields, and at least one `sources` entry are required. String source entries default to `recursive: false`.

Running `modlet-builder build --proj mod.proj.yml` writes to `{output}/{modFolder}`. Passing `--out` in project mode overrides only the output root, so `--out build-output` writes to `build-output/{modFolder}`.

### Example

Given source fragments:

```xml
<!-- src/items.frag.xml -->
<modlet>
  <fragment name="mymod.items.base" target="items"> ... </fragment>
  <fragment target="items" requires="mymod.items.base"> ... </fragment>
</modlet>

<!-- src/recipes.frag.xml -->
<modlet>
  <fragment target="recipes" requires="mymod.items.base"> ... </fragment>
</modlet>
```

Run:

```bash
modlet-builder build --src src/ --out /path/to/Mods/MyMod --recursive
```

Result:

```text
/path/to/Mods/MyMod/
└─ Config/
  ├─ items.xml      — mymod.items.base followed by the unnamed items fragment
  └─ recipes.xml    — unnamed recipes fragment
```

Each generated file has the following structure:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<config>
  <!-- fragment body elements, in dependency-resolved order -->
</config>
```

### Dry run

`--dry-run` performs all parsing, validation, and dependency resolution steps but does not touch the filesystem in any way — no files are written, no directories are created or deleted. Use it to catch errors before committing to disk.

```bash
modlet-builder build --src src/ --out /path/to/Mods --recursive --dry-run
```

## Known Target Values

The `target` attribute in a fragment file must be one of the values below. Each maps to a specific output path inside `Config/`.

| Target value | Output path |
| ------------ | ----------- |
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

## Project Status

> **Early development.** The `build` command implementation is in progress. The command contract and source file format described in this document reflect the intended design.

Known limitations for this phase:

- Raw `--src` builds do not generate `ModInfo.xml`; use `--proj` for complete mod folder output.
- Source fragments still use the `*.frag.xml` format.
- `target` values not in the table above are hard errors; no custom target extensibility yet.

**Breaking change:** the `--out` option expects a **single mod directory**. Config files are written to `{mod-dir}/Config/`. To build the same sources into two different mods, run `build` twice with different `--out` values. The `hint` attribute on `<modlet>` and `<fragment>` elements and the `--targets` option are no longer supported and will produce errors.

**Breaking change (previous):** source documents must use root element `<modlet>`. The previous root `<fragment>` format is not supported.

## Localization

`modlet-builder` can generate `Config/Localization.txt` alongside your XML config files. Place `<localization>` blocks inside the `<modlet>` root element alongside the fragments they describe. The blocks are build metadata: they are stripped from the generated output and assembled into a single `Config/Localization.txt` in the 7 Days to Die CSV format.

### Localization block format

```xml
<modlet>

  <!-- file and type are omitted — auto-derived from the <item> element below. -->
  <localization key="myItemDesc" context="Item description">
    <english text="A custom item added by the mod."/>
    <russian text="Мой предмет"/>
    <german text="Mein Gegenstand"/>
  </localization>

  <fragment name="mymod.items.base" target="items">
    <append xpath="/items">
      <item name="myItem">
        <property name="DescriptionKey" value="myItemDesc"/>
      </item>
    </append>
  </fragment>

</modlet>
```

### Localization block attributes

| Attribute | Required | Description |
| --------- | -------- | ----------- |
| `key` | Yes | Unique localization key. Must be unique across all fragments in the build; duplicates are a build error. |
| `file` | No | The `File` column value in `Localization.txt`. Auto-derived from the game object that references this key via `DescriptionKey` (see table below). Provide explicitly for targets where auto-derivation is not supported, or to override the derived value. |
| `type` | No | The `Type` column value in `Localization.txt`. Auto-derived alongside `file`. Provide explicitly when auto-derivation is not possible. |
| `context` | No | The `Context / Alternate Text` column value. |

#### `file` and `type` auto-derivation

When `file` and `type` are omitted, the tool scans all fragment bodies for a matching `<property name="DescriptionKey" value="..."/>` and derives both attributes from the parent game object element:

| Parent element | `file` | `type` |
| -------------- | ------ | ------ |
| `<item>` | `items` | `Item` |
| `<block>` | `blocks` | `Block` |
| `<item_modifier>` | `item_modifiers` | `Mod` |

For all other targets, provide `file` and `type` explicitly. Any string value is accepted; the tool does not restrict the column to the table above.
| `usedInMainMenu` | No | The `UsedInMainMenu` column value. |
| `noTranslate` | No | Boolean. `true` or `1` → the `NoTranslate` column in `Localization.txt` contains `x` (7 Days to Die convention for "do not translate this string"). `false`, `0`, or absent (default) → column is left empty. |

#### `noTranslate` examples

Normal translatable entry — `noTranslate` absent, column will be empty:

```xml
<localization key="myItemDesc" file="items" type="Item">
  <english text="A custom item added by the mod."/>
  <russian text="Мой предмет"/>
</localization>
```

Proper name that must not be translated — column will contain `x`:

```xml
<localization key="myModCreatorLabel" file="items" type="Item" noTranslate="true">
  <english text="Created by YourName"/>
</localization>
```

The `x` convention is used in vanilla `Localization.txt` for developer names, memorial plaques, and other strings that should appear as-is in all locales.

### Supported language elements

Each `<localization>` block may contain zero or more of these child elements. Absent languages produce empty CSV cells.

`english`, `german`, `spanish`, `french`, `italian`, `japanese`, `koreana`, `polish`, `brazilian`, `russian`, `turkish`, `schinese`, `tchinese`

Each language element requires a `text` attribute:

```xml
<english text="My Item"/>
```

### Output

When any fragment contains at least one `<localization>` block, the build produces:

```text
{mod-dir}/
└─ Config/
   ├─ items.xml
   └─ Localization.txt
```

The CSV uses the exact column order from `Localization.txt`:

```text
Key,File,Type,UsedInMainMenu,NoTranslate,english,Context / Alternate Text,german,...
```

Row order follows the discovered file order (files sorted deterministically), then source order within each file. Values containing commas, quotes, or newlines are quoted per RFC 4180. The file is UTF-8 without BOM.

### Duplicate key rule

Two `<localization>` blocks with the same `key` across any source documents are a build error. The error message identifies both the duplicate and the first definition location. No output files are written when duplicate keys are detected.

### DescriptionKey linkage rule

Every `<localization>` key must be referenced by at least one `<property name="DescriptionKey" value="..."/>` inside a fragment body in the same build. A localization key that is not linked to any `DescriptionKey` property is a build error. No output files are written.

## License

MIT — see [LICENSE](LICENSE).
