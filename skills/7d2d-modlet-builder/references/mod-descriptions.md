# Mod Descriptions, ModInfo, and Publishing Text

Use this reference when creating or updating mod metadata, GitHub README content,
or Nexus Mods description content for 7 Days to Die modlets.

## ModInfo

Every loadable 7 Days to Die mod folder needs `ModInfo.xml`.

Current `modlet-builder` project builds generate this shape:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ModInfo>
  <Name value="EV_MyMod"/>
  <DisplayName value="My Mod"/>
  <Description value="Short description of what this mod does."/>
  <Author value="Aleksei Khozin"/>
  <Version value="1.0.0"/>
  <Website value="https://github.com/alekho77/epic_7d2d_mods"/>
</ModInfo>
```

Rules:

- `Name` is the internal mod ID. Use a stable, globally distinct value with no
  spaces.
- `DisplayName` is the human-readable title.
- `Description` should be one concise sentence.
- `Version` should be SemVer-like.
- Keep project YAML `modInfo` values aligned with the README footer and changelog.

7 Days to Die Alpha 21+ also supports a newer `<xml>` root format. Do not change
`modlet-builder` output shape unless the task explicitly includes generator
behavior changes and tests.

## README Structure

Use this section order for generated or maintained `README.md` files:

1. `# Mod Title`
2. `## Description`
3. server-side or client-side badge blockquote
4. `## Features`
5. `## How It Works` when useful for non-obvious mechanics
6. `## Installation`
7. `## Compatibility`
8. `## Changelog`
9. `## Credits` when there are real credits
10. footer separator with author, version, and website

Description should explain the user-facing purpose in one or two sentences. Keep
technical implementation detail in `How It Works`.

## Server-Side Badge

Use this when the mod only changes server-synced XML/data and clients do not need
custom local assets:

```markdown
> ### Server-Side Friendly
>
> **If you are running a dedicated server, this mod only needs to be installed on the server.**
> Players connecting to the server **do not need to download or install anything** on their game clients.
```

Use a client-side badge instead when the mod requires local assets, UI files,
icons, sounds, bundles, or client DLLs.

## Installation Section

Use direct numbered steps:

```markdown
## Installation

1. Download the mod archive.
2. Extract the `EV_MyMod` folder.
3. Copy `EV_MyMod` into your 7 Days to Die `Mods` folder.
4. Start the game and check the log if the mod does not load.
```

Mention the recommended `%APPDATA%/7DaysToDie/Mods` location when writing for a
general audience. Mention a dedicated server `Mods` folder for server-side mods.

## Compatibility Section

State:

- target game version or tested version
- whether the mod is server-side friendly or client-side required
- likely conflicts, especially shared edits to the same vanilla XML nodes
- whether EAC must be disabled

Do not claim broad compatibility unless it has been tested or the patch is narrow
enough to justify the claim.

## Changelog

Use descending version order:

```markdown
## Changelog

### v1.0.0

- Initial release.
```

Keep changelog bullets user-facing. Avoid internal implementation details unless
they affect behavior or compatibility.

## Footer

Markdown footer:

```markdown
---

Author: Aleksei Khozin
Version: 1.0.0
Website: https://github.com/alekho77/epic_7d2d_mods
```

Keep these values in sync with project YAML `modInfo`.

## Nexus BBCode

`modlet-builder` project builds copy `README.md` and generate
`NEXUS_DESCRIPTION.bbcode` through the configured Markdown-to-BBCode converter.

Use conservative Markdown that converts cleanly:

- headings
- paragraphs
- unordered and ordered lists
- bold and italic text
- code blocks only when necessary
- blockquotes for server/client badges

Supported Nexus-style BBCode tags include:

| Purpose | Syntax |
| --- | --- |
| Bold | `[b]text[/b]` |
| Italic | `[i]text[/i]` |
| Underline | `[u]text[/u]` |
| Strikethrough | `[s]text[/s]` |
| Color | `[color=#RRGGBB]text[/color]` |
| Size | `[size=N]text[/size]` |
| Code | `[code]text[/code]` |
| Quote | `[quote]text[/quote]` |
| Unordered list | `[list][*]item[/list]` |
| Ordered list | `[list=1][*]item[/list]` |

Avoid complex nested Markdown that may produce invalid or hard-to-read BBCode.

## Review Checklist

- README title matches `modInfo.displayName`.
- Internal folder and `modInfo.name` use a stable no-spaces ID.
- Version appears consistently in project YAML, README changelog, and footer.
- Server-side/client-side claim matches the actual files included in the mod.
- Compatibility notes mention known shared XML patch areas.
- Nexus output file name is `NEXUS_DESCRIPTION.bbcode` for current
  `modlet-builder` generated projects.
