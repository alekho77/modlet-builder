# 7D2D Modlet Builder Instructions

Use these instructions when working in a repository that creates 7 Days to Die
XML modlets with `modlet-builder`.

## Core Rules

- Write repository-facing code, XML examples, Markdown, YAML, comments, and commit
  messages in English.
- Treat vanilla 7 Days to Die `Data/Config` files as the source of truth. Never
  guess item names, block names, property names, buff IDs, perk IDs, UI window
  names, or XPath targets.
- Prefer XML-only modlets when the requested behavior can be implemented through
  XPath patches, localization, XUi XML, recipes, loot, buffs, progression, or
  other config data.
- Keep each modlet focused on one feature or balance concern.
- Do not copy third-party mod content verbatim.

## modlet-builder Workflow

- Author source files as `*.frag.xml` documents with a `<modlet>` root.
- Put patch payload inside `<fragment target="...">` elements.
- Put build-only localization metadata as direct `<localization>` children of
  `<modlet>`, not inside `<fragment>`.
- Use `requires` only for real ordering dependencies.
- Omit fragment `name` when nothing references the fragment.
- Validate with `modlet-builder build --dry-run` before writing output when
  changing fragments or project files.

## Downstream Copy Path

This file is an adapter generated from the canonical skill package at
`skills/7d2d-modlet-builder/`. Copy it to `.github/copilot-instructions.md` or
merge it into `.github/instructions/*.instructions.md` in downstream mod
repositories.

For the complete reference, use:

- `skills/7d2d-modlet-builder/SKILL.md`
- `skills/7d2d-modlet-builder/references/7d2d-xml-modlets.md`
- `skills/7d2d-modlet-builder/references/modlet-builder.md`
- `skills/7d2d-modlet-builder/references/mod-descriptions.md`
