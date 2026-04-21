# modlet-builder

A build tool for assembling modlets and XML patch fragments into final mod output, designed first for **7 Days to Die**.

## What It Does

`modlet-builder` reads modular XML source fragments, resolves their ordering and routing metadata, and generates final game-ready config files deterministically.

Instead of managing dozens of separate modlets with unpredictable load order, you write small focused fragment files. The tool assembles them into a single vanilla-compatible mod output with explicit, reproducible ordering.

**Input** — developer-friendly source fragments:

```xml
<fragment name="my-mod.items.base" target="items">
  <append xpath="/items">
    <!-- item definitions -->
  </append>
</fragment>

<fragment name="my-mod.recipes.base" target="recipes" requires="my-mod.items.base">
  <append xpath="/recipes">
    <!-- recipe definitions -->
  </append>
</fragment>
```

**Output** — vanilla-compatible XML files ready to drop into your `Mods/` folder:

```text
Config/
  items.xml
  recipes.xml
  blocks.xml
  buffs.xml
  ...
```

Build-only metadata (`name`, `target`, `requires`) is stripped from all generated files.

## Who It Is For

- 7 Days to Die mod authors building large or complex mods
- Modpack authors who want deterministic, reproducible XML assembly
- Anyone who manages XML patch fragments across multiple source files and needs reliable merge ordering

## Terminology

| Term | Meaning |
| ---- | ------- |
| **fragment** | A single `<fragment>` element in a source file, containing XML operations for one target config |
| **name** | Required unique identifier of a fragment (e.g. `my-mod.items.base`); used as a reference target for `requires` |
| **target** | Required. The output config file a fragment contributes to. Must be one of the known target values (see below) |
| **requires** | Optional. Comma-separated list of `name` values this fragment depends on; the tool ensures dependents are placed after their dependencies |

### Known `target` Values

| Value | Game config file | Notes |
| ----- | ---------------- | ----- |
| `items` | `Data/Config/items.xml` | Weapons, tools, consumables, resources, ammo |
| `blocks` | `Data/Config/blocks.xml` | Placeable blocks: terrain, structures, doors, traps |
| `recipes` | `Data/Config/recipes.xml` | Crafting recipes |
| `loot` | `Data/Config/loot.xml` | Loot containers and probability tables |
| `entityclasses` | `Data/Config/entityclasses.xml` | Zombie, animal, NPC class definitions |
| `entitygroups` | `Data/Config/entitygroups.xml` | Named entity groups for gamestage spawning |
| `buffs` | `Data/Config/buffs.xml` | Buffs, debuffs, status effects |
| `progression` | `Data/Config/progression.xml` | Skills, perks, attributes, level scaling |
| `gamestages` | `Data/Config/gamestages.xml` | Horde night wave definitions |
| `spawning` | `Data/Config/spawning.xml` | Biome and zone spawning rules |
| `traders` | `Data/Config/traders.xml` | Trader inventories and quest offerings |
| `vehicles` | `Data/Config/vehicles.xml` | Vehicle definitions and properties |
| `item_modifiers` | `Data/Config/item_modifiers.xml` | Weapon/tool mod attachments |
| `quests` | `Data/Config/quests.xml` | Quest definitions and reward tables |
| `biomes` | `Data/Config/biomes.xml` | Biome definitions |
| `sounds` | `Data/Config/sounds.xml` | Sound event mappings |
| `materials` | `Data/Config/materials.xml` | Block material properties |
| `shapes` | `Data/Config/shapes.xml` | Block shape definitions |
| `qualityinfo` | `Data/Config/qualityinfo.xml` | Item quality tiers and stat scaling |
| `worldglobal` | `Data/Config/worldglobal.xml` | Global world settings |
| `weathersurvival` | `Data/Config/weathersurvival.xml` | Weather effects on player survival |
| `painting` | `Data/Config/painting.xml` | Block painting textures catalogue |
| `nav_objects` | `Data/Config/nav_objects.xml` | Minimap/compass navigation icons |
| `archetypes` | `Data/Config/archetypes.xml` | Entity archetypes (base templates) |
| `dialogs` | `Data/Config/dialogs.xml` | NPC dialog trees |
| `npc` | `Data/Config/npc.xml` | NPC-specific settings |
| `challenges` | `Data/Config/challenges.xml` | In-game challenges and objectives |
| `events` | `Data/Config/events.xml` | Game event trigger definitions |
| `gameevents` | `Data/Config/gameevents.xml` | Game event response/action definitions |
| `rwgmixer` | `Data/Config/rwgmixer.xml` | Random World Generation recipe |
| `utilityai` | `Data/Config/utilityai.xml` | AI utility scoring and behaviour trees |
| `misc` | `Data/Config/misc.xml` | Miscellaneous global game variables |
| `physicsbodies` | `Data/Config/physicsbodies.xml` | Ragdoll/physics body definitions |
| `ui_display` | `Data/Config/ui_display.xml` | Stat/property display labels for UI |
| `music` | `Data/Config/music.xml` | Background music event mappings |
| `subtitles` | `Data/Config/subtitles.xml` | Subtitle entries for audio events |
| `dmscontent` | `Data/Config/dmscontent.xml` | Dynamic Music System configuration |
| `twitch` | `Data/Config/twitch.xml` | Twitch integration configuration |
| `twitch_events` | `Data/Config/twitch_events.xml` | Twitch integration event definitions |
| `videos` | `Data/Config/videos.xml` | Intro/cutscene video references |
| `loadingscreen` | `Data/Config/loadingscreen.xml` | Loading screen tip text |
| `blockplaceholders` | `Data/Config/blockplaceholders.xml` | Block placeholder substitution rules |
| `xui_windows` | `Data/Config/XUi/windows.xml` | In-game HUD windows |
| `xui_controls` | `Data/Config/XUi/controls.xml` | Reusable HUD UI components |
| `xui_styles` | `Data/Config/XUi/styles.xml` | HUD UI styles |
| `xui_menu_windows` | `Data/Config/XUi_Menu/windows.xml` | Main menu windows |
| `xui_menu_controls` | `Data/Config/XUi_Menu/controls.xml` | Main menu UI components |
| `xui_menu_styles` | `Data/Config/XUi_Menu/styles.xml` | Main menu UI styles |
| `xui_common_controls` | `Data/Config/XUi_Common/controls.xml` | Shared UI controls (HUD + Menu) |
| `xui_common_styles` | `Data/Config/XUi_Common/styles.xml` | Shared UI styles (HUD + Menu) |

## Project Status

> **Early development.** The tool is not yet functional. This repository is being set up.

## Build and Run

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). The exact SDK version is pinned in [global.json](global.json).

Build the entire solution:

```bash
dotnet build ModletBuilder.sln
```

Build outputs are redirected to the repository-root `build/` folder (configured in [Directory.Build.props](Directory.Build.props)):

```text
build/
├─ bin/<ProjectName>/<Configuration>/<TargetFramework>/
└─ obj/<ProjectName>/<Configuration>/<TargetFramework>/
```

For example, the CLI `Debug` binary lands at:

```text
build/bin/ModletBuilder.Cli/Debug/net10.0/modlet-builder.exe
```

Run the built executable directly:

```powershell
.\build\bin\ModletBuilder.Cli\Debug\net10.0\modlet-builder --version
```

Or run via the SDK without locating the binary:

```bash
dotnet run --project src/ModletBuilder.Cli -- build
```

To produce an optimized build, use `-c Release`:

```bash
dotnet build ModletBuilder.sln -c Release
```

The resulting executable is placed at `build/bin/ModletBuilder.Cli/Release/net10.0/modlet-builder.exe`.

## Publishing as a Single Executable

Self-contained single file (no .NET runtime required on target machine):

```bash
dotnet publish src/ModletBuilder.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Native AOT (fully native binary, if compatible with chosen dependencies):

```bash
dotnet publish src/ModletBuilder.Cli -c Release -r win-x64 -p:PublishAot=true
```

Published output is written under `build/bin/ModletBuilder.Cli/Release/net10.0/<runtime>/publish/`.

## CLI Commands

| Command | Description |
| ------- | ----------- |
| `build` | Assemble fragments into output config files |
| `validate` | Validate sources without generating output |

## Repository Layout

```text
modlet-builder/
├─ README.md
├─ LICENSE
├─ docs/                    — design notes, specs, format docs
├─ src/
│  ├─ ModletBuilder.Cli/    — command-line entrypoint
│  ├─ ModletBuilder.Core/   — core domain logic
│  ├─ ModletBuilder.Xml/    — XML parsing and generation
│  └─ ModletBuilder.Tests/  — automated tests
├─ samples/                 — minimal example inputs and outputs
└─ schemas/                 — format definitions
```

## License

MIT — see [LICENSE](LICENSE).
