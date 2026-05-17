---
name: 7d2d-modlet-builder
description: >-
  Use when creating, reviewing, or updating 7 Days to Die XML modlets with
  modlet-builder, including .frag.xml sources, project YAML files, ModInfo
  metadata, README/Nexus descriptions, generated Config XML, and downstream
  agent instruction packages.
---

# 7D2D Modlet Builder

Use this skill for XML-first 7 Days to Die modlet work that uses the
`modlet-builder` CLI and its `*.frag.xml` source format.

## Core Workflow

1. Classify the task:
   - For vanilla 7 Days to Die XML patching, read `references/7d2d-xml-modlets.md`.
   - For `modlet-builder` CLI, fragment, project, target, localization, or output
     behavior, read `references/modlet-builder.md`.
   - For `README.md`, `ModInfo.xml`, Nexus description, or publishing text, read
     `references/mod-descriptions.md`.
2. Treat vanilla game data as the source of truth. Before inventing item names,
   block names, property names, buff IDs, perk IDs, UI window names, or XPath
   targets, inspect the relevant vanilla `Data/Config` XML or an available catalog.
3. Prefer XML-only modlets when the requested behavior can be expressed through
   XPath patches, localization, XUi XML, loot, recipes, progression, buffs, or
   data tables.
4. Keep `*.frag.xml` sources modular. Put one focused concern per fragment, use
   `requires` only for real ordering dependencies, and omit fragment `name` when
   no other fragment references it.
5. Use `modlet-builder build --dry-run` for validation before writing output when
   changing fragments or project files. Use a real build after validation when the
   user asks for generated mod output.

## Source Rules

- Repository-facing code, XML examples, Markdown, YAML, and comments must be in
  English unless the target file already has a stronger local convention.
- Do not guess 7 Days to Die identifiers. Resolve them from vanilla config files,
  `docs/inventory_catalog.md`, or checked-in source material.
- Do not copy third-party mod content verbatim. Use reference mods only to learn
  implementation patterns.
- Keep Harmony and total conversion work out of scope for this skill unless the
  user explicitly asks for DLL patches, custom assets, Addressables, bundles, or
  full UI replacement.

## Downstream Agent Adapters

This package includes adapter files for projects that do not load Codex skills
directly:

- Copy `adapters/copilot-instructions.md` to `.github/copilot-instructions.md`
  or merge it into `.github/instructions/*.instructions.md` for GitHub Copilot.
- Copy `adapters/AGENTS.md` to `AGENTS.md` for agents that read repository-level
  agent instructions.

The source of truth remains this skill package. When updating instructions, update
`SKILL.md` and the reference files first, then refresh the adapters.
