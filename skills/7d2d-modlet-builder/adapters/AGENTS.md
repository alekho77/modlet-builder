# 7D2D Modlet Builder Agent Instructions

This repository uses the canonical agent skill package at
`skills/7d2d-modlet-builder/`.

## Required Behavior

- Load `skills/7d2d-modlet-builder/SKILL.md` before creating, reviewing, or
  updating 7 Days to Die modlets, `*.frag.xml` files, project YAML files,
  ModInfo metadata, README files, or Nexus description text.
- Load `references/7d2d-xml-modlets.md` for vanilla 7 Days to Die XML patching
  rules and lookup workflow.
- Load `references/modlet-builder.md` for CLI syntax, fragment format, project
  YAML, targets, localization, dependency ordering, diagnostics, and generated
  output.
- Load `references/mod-descriptions.md` for README, ModInfo, compatibility,
  changelog, and Nexus BBCode conventions.

## Development Rules

- Use vanilla `Data/Config` files or an available inventory catalog before
  choosing internal 7 Days to Die IDs or XPath targets.
- Prefer XML-only changes before Harmony or asset-pipeline work.
- Run a `modlet-builder build --dry-run` validation when changing fragments or
  project files.
- Keep generated mod metadata and README/version text aligned.

## Downstream Copy Path

Copy this file to `AGENTS.md` in downstream repositories that use agents with
repository-level instruction files. Keep the skill package as the source of truth
and refresh this adapter after changing the references.
