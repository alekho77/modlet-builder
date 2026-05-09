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

**What comes out:** generated XML files inside `{mod-dir}/Config/`, ready to drop into your game's `Mods/` folder.

```bash
modlet-builder build --src path/to/my-fragments --out path/to/MyMod --recursive
```

This scans `path/to/my-fragments/` recursively for `*.frag.xml` files, resolves their order, and writes the assembled XML files into `path/to/MyMod/Config/`:

```text
path/to/MyMod/
└─ Config/
   ├─ items.xml
   └─ recipes.xml
```

Drop the `MyMod/` folder into your game's `Mods/` directory and you're done.

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

A **source fragment** is a `*.frag.xml` file containing exactly one `<fragment>` root element. Fragment files can be passed to `build` as explicit file paths, or collected automatically from a directory.

### Fragment file format

```xml
<fragment name="my-mod.items.base" target="items">
  <append xpath="/items">
    <!-- item definitions go here -->
  </append>
</fragment>
```

```xml
<fragment name="my-mod.recipes.base" target="recipes" requires="my-mod.items.base">
  <append xpath="/recipes">
    <!-- recipe definitions go here -->
  </append>
</fragment>
```

### Attributes

| Attribute | Required | Description |
| --------- | -------- | ----------- |
| `name` | Yes | Unique identifier for this fragment. Used as a reference target in `requires`. Suggested convention: `{mod}.{target}.{role}`. |
| `target` | Yes | The output config file this fragment contributes to. Must be one of the [known target values](#known-target-values). |
| `requires` | No | Comma-separated list of `name` values this fragment depends on. The tool places all dependencies before this fragment in the output. Dependencies may cross target boundaries. |

Build-only metadata (`name`, `target`, `requires`) is stripped from all generated output. Only the child elements of `<fragment>` appear in the final XML.

### Directory scanning

When a directory is passed as a source, the tool scans it for `*.frag.xml` files. Use `--recursive` to include subdirectories. Explicit file paths and directory paths can be mixed freely in the same command. All discovered files are deduplicated and sorted deterministically before processing.

## `build` Command

Assembles source fragments into final game-ready XML files.

```text
modlet-builder build --src <path> [<path> ...] --out <mod-dir> [--recursive] [--dry-run]
```

### Options

| Option | Required | Description |
| ------ | -------- | ----------- |
| `--src <path> [<path> ...]` | Yes | One or more source paths. Each path can be an explicit `*.frag.xml` file or a directory. Paths are space-separated and may be mixed freely. |
| `--out <mod-dir>` | Yes | Path to the output mod root directory. Generated XML is written into `{mod-dir}/Config/`. Required even with `--dry-run`. |
| `--recursive` | No | When a directory is given in `--src`, scan its subdirectories recursively for `*.frag.xml` files. |
| `--dry-run` | No | Validate all source fragments, resolve dependencies, and check the output location — without writing any files. |

### Example

Given these source files:

```text
src/
  items/
    base.frag.xml    — name="mymod.items.base"   target="items"
    extra.frag.xml   — name="mymod.items.extra"  target="items"    requires="mymod.items.base"
  recipes.frag.xml   — name="mymod.recipes.main" target="recipes"  requires="mymod.items.base"
```

Run:

```bash
modlet-builder build --src src/items src/recipes.frag.xml --out /path/to/MyMod --recursive
```

Result:

```text
/path/to/MyMod/
└─ Config/
   ├─ items.xml      — content of base.frag.xml followed by extra.frag.xml
   └─ recipes.xml    — content of recipes.frag.xml
```

Each generated file has the following structure:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<config>
  <!-- fragment body elements, in dependency-resolved order -->
</config>
```

### Dry run

`--dry-run` performs all parsing, validation, and dependency resolution steps but does not write any output files. It verifies that `{mod-dir}/Config/` exists or can be created. Use it to catch errors before committing to disk.

```bash
modlet-builder build --src src/ --out /path/to/MyMod --recursive --dry-run
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

- No `ModInfo.xml` generation.
- No `Localization.txt` support.
- Only `*.frag.xml` source format is supported.
- `target` values not in the table above are hard errors; no custom target extensibility yet.

## License

MIT — see [LICENSE](LICENSE).
