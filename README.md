# modlet-builder

A build tool for assembling modlets and XML patch fragments into final mod output, designed first for **7 Days to Die**.

## What It Does

`modlet-builder` reads modular XML source fragments, resolves their ordering and routing metadata, and generates final game-ready config files deterministically.

Instead of managing dozens of separate modlets with unpredictable load order, you write small focused fragment files. The tool assembles them into a single vanilla-compatible mod output with explicit, reproducible ordering.

**Input** — developer-friendly source fragments:

```xml
<fragment target="items" order="200">
  <append xpath="/items">
    <!-- item definitions -->
  </append>
</fragment>

<fragment target="recipes" after="core.items.base">
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

Build-only metadata (`target`, `order`, `before`, `after`, `requires`, `phase`) is stripped from all generated files.

## Who It Is For

- 7 Days to Die mod authors building large or complex mods
- Modpack authors who want deterministic, reproducible XML assembly
- Anyone who manages XML patch fragments across multiple source files and needs reliable merge ordering

## Terminology

| Term | Meaning |
| ---- | ------- |
| **fragment** | A single `<fragment>` element in a source file, containing XML operations for one target config |
| **target** | The output config file a fragment contributes to (e.g. `items`, `recipes`, `blocks`) |
| **order** | Numeric hint for fragment sequencing within a target |
| **before / after** | Named dependency constraints between fragments |
| **requires** | Explicit dependency on another named fragment |
| **phase** | Optional grouping for build stages |

## Project Status

> **Early development.** The tool is not yet functional. This repository is being set up.

## Build and Run

Requires [.NET SDK](https://dotnet.microsoft.com/download) (current stable).

```bash
dotnet build src/ModletBuilder.Cli
dotnet run --project src/ModletBuilder.Cli -- build
```

## Publishing as a Single Executable

Self-contained single file (no .NET runtime required on target machine):

```bash
dotnet publish src/ModletBuilder.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Native AOT (fully native binary, if compatible with chosen dependencies):

```bash
dotnet publish src/ModletBuilder.Cli -c Release -r win-x64 -p:PublishAot=true
```

## CLI Commands

| Command | Description |
| ------- | ----------- |
| `build` | Assemble fragments into output config files |
| `validate` | Validate sources without generating output |
| `inspect` | Show resolved fragment order and routing |
| `init` | Initialize a new source directory layout |

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
