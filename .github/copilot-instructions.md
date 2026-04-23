# modlet-builder — Copilot Instructions

This repository contains **modlet-builder**, a build tool authored by **Aleksei Khozin** for assembling modular XML sources and patch fragments into final game-ready mod output, designed first for **7 Days to Die** but implemented as a general-purpose **C# / modern .NET** CLI tool. The repository is currently public, MIT-licensed, and described as "A build tool for assembling modlets and XML patch fragments into final mod output, designed first for 7 Days to Die."

## General Instructions

### Language
- All source code, comments, XML examples, JSON examples, Markdown files, commit messages, and repository documentation must be written in **English**.
- Respond in the same language the user used in their request.
- When generating repository-facing content, prefer concise, technical, implementation-oriented wording.

### Response Completion
- After completing every user request, always end with a single closing sentence in English written in the style of a git commit message — concise, imperative, describing what was done.
- Examples: `Add copilot-instructions.md to .github/`, `Fix markdownlint errors in README.md`, `Scaffold solution with Cli, Core, and Tests projects`.
- No additional closing remarks, summaries, or explanations after this sentence.

### Scope of This Repository
- This is **not** a game mod repository.
- This is **not** a repository for gameplay content, lore, assets, or 7DTD XML patches themselves.
- This repository is for the **tooling layer**: parsing, validation, ordering, dependency resolution, transformation, build orchestration, packaging, diagnostics, and tests for `modlet-builder`.

### Project Intent
- Treat the project as a **developer tool** and **CLI application**, not as a prototype script collection.
- Favor solutions that improve:
  - deterministic builds
  - maintainability
  - testability
  - clear diagnostics
  - explicit configuration
  - reproducible output
- Prefer a clean internal architecture over quick hacks.

## Technology Baseline

### Language and Runtime
- Use **C#** targeting **.NET 10** (`net10.0`).
- The SDK version is pinned in [global.json](../global.json); do not change it casually.
- The tool must be designed as a **CLI-first application**.
- Keep the codebase ready for publishing as a **single executable**.

### Packaging Goal
- The long-term default expectation is that `modlet-builder` should be publishable as:
  - a normal .NET CLI app
  - a **single-file executable**
  - optionally a **self-contained** build
  - optionally **Native AOT**, if compatible with the implementation choices
- Do not introduce dependencies that make single-file publishing, self-contained publishing, or Native AOT unnecessarily difficult unless clearly justified.

## Development Principles

### Architecture
- Keep the design modular and layered.
- Separate the code into clear responsibilities such as:
  - CLI entrypoint
  - configuration loading
  - source discovery
  - parsing
  - metadata extraction
  - dependency graph resolution
  - target routing
  - ordering/sorting
  - XML generation
  - validation
  - diagnostics/reporting
  - filesystem output
- Avoid mixing CLI concerns with domain logic.

### Determinism
- All builds must be deterministic.
- The same input tree and configuration must produce the same output ordering and file content.
- Never rely on incidental filesystem ordering.
- Always use explicit sorting rules.

### Explicitness Over Guessing
- Never guess target files, dependency rules, fragment ordering, or metadata semantics if the repository already defines them.
- If a convention is not defined yet, add it explicitly in code or documentation instead of encoding hidden assumptions.

### Small Public API Surface
- Keep internal APIs narrow and intentional.
- Prefer internal types until there is a clear reason to expose public contracts.
- Avoid unnecessary framework-like abstraction too early.

## Repository Structure Guidance

Use a structure close to this unless the repository evolves differently by explicit decision:

```text
modlet-builder/
├─ README.md
├─ LICENSE
├─ global.json              — pinned .NET SDK version
├─ Directory.Build.props    — shared MSBuild properties (redirects build output)
├─ ModletBuilder.sln        — solution file
├─ docs/                    — design notes, specs, format docs
├─ src/
│  ├─ ModletBuilder.Cli/    — command-line entrypoint
│  ├─ ModletBuilder.Core/   — core domain logic
│  ├─ ModletBuilder.Xml/    — XML parsing / generation helpers if separated
│  └─ ModletBuilder.Tests/  — automated tests
├─ samples/                 — minimal example inputs and outputs
├─ schemas/                 — optional schemas or formal format definitions
└─ build/                   — generated build output (bin/ and obj/), git-ignored
```

### Structural Rules

- Put executable startup code in the CLI project only.
- Put reusable business logic in `Core`.
- Keep tests separate from production code.
- Keep sample projects minimal, focused, and runnable.

### Build Output Layout

- All MSBuild output is redirected to the repository-root `build/` folder via `Directory.Build.props`.
- Compiled binaries land at `build/bin/<ProjectName>/<Configuration>/<TargetFramework>/`.
- Intermediate files land at `build/obj/<ProjectName>/<Configuration>/<TargetFramework>/`.
- The CLI assembly name is `modlet-builder`, so the Debug executable is `build/bin/ModletBuilder.Cli/Debug/net10.0/modlet-builder.exe`.
- Do not reintroduce per-project `bin/` or `obj/` folders.

## Domain Model Expectations

The tool is expected to work with **source fragments** that may later be compiled into final XML output files.

Typical concepts may include:

- **fragment**
- **name**
- **target**
- **requires**
- **source file**
- **generated file**
- **build report**
- **diagnostic**
- **warning**
- **error**

When implementing these concepts:

- Treat them as explicit domain objects.
- Avoid passing loosely structured dictionaries everywhere.
- Prefer well-named record/class types over anonymous structures for core build concepts.

## XML and Build Semantics

### Source vs Output

- Distinguish clearly between:
  - **source format** used by developers
  - **generated output format** written for the target mod/game
- Build-only metadata must not leak into final generated XML unless explicitly intended.

### Ordering

- Fragment ordering is determined solely by `requires` dependency declarations.
- The tool performs a topological sort over the declared dependency graph.
- Fragments with no dependencies relative to each other are ordered deterministically by their `name` attribute.
- Circular dependencies must be detected and reported as errors.

### Validation

- Validate as early as practical.
- Report:
  - malformed source XML
  - unknown metadata keys
  - invalid target names
  - dependency cycles
  - missing dependencies
  - duplicate identifiers when disallowed
  - ambiguous ordering
- Error messages must identify the source file and enough local context to fix the problem quickly.

### Diagnostics

- Diagnostics must be readable and actionable.
- Prefer messages like:
  - what failed
  - where it failed
  - why it failed
  - what the user should check next
- Avoid vague exceptions with no context.

## CLI Design Rules

### Command Behavior

- The CLI must be predictable and script-friendly.
- Favor explicit commands and options.
- Keep stdout/stderr separation sensible.
- Return non-zero exit codes on build failure.

### Usability

- A first-time user should be able to understand the primary workflow quickly.
- The primary command is:
  - `build` — assemble fragments into output config files
- Validation-only runs are expressed as a flag on `build` (`--dry-run`), not as a separate `validate` command. `--dry-run` performs all parsing, resolution, and validation steps but must not write any files to the output folder.
- If new commands are added, keep naming short and conventional.

### Output Modes

- Prefer human-readable output by default.
- Machine-readable output such as JSON may be added only when it serves clear automation scenarios.

## Testing Standards

### Required Testing Mindset

- Every non-trivial parser, resolver, sorter, and generator change should be accompanied by tests.
- Testing is mandatory for:
  - XML parsing
  - metadata extraction
  - dependency resolution
  - ordering rules
  - output generation
  - failure cases
  - regression scenarios

### Test Style

- Prefer small, focused tests.
- Use descriptive English test names.
- Cover both happy paths and invalid inputs.
- Include golden-file or snapshot-style tests only when they improve clarity and remain easy to review.

### Regression Safety

- If a bug is fixed, add a test reproducing it.
- Do not fix complex behavior silently without encoding the expected behavior in tests.

## Dependency Policy

### External Libraries

- Prefer the .NET standard library unless a third-party package clearly improves:
  - XML handling
  - CLI ergonomics
  - diagnostics
  - testing
- Avoid dependency bloat.
- Do not add packages "just in case".

### Version Discipline

- Keep package versions intentional and minimal.
- When updating dependencies, preserve compatibility with the project's publish goals.

## File Editing Rules

### After Changing Any File

After editing **any** file — including C#, Markdown, JSON, XML, YAML, plain text, configuration files, or project files — always run relevant validation for the modified content before finishing.

At minimum:

- build the affected project (or the whole solution via `dotnet build ModletBuilder.sln`) if code changed
- run tests if behavior changed
- validate formatting if formatting tools are already configured in the repository
- validate generated examples or samples if they were changed
- check any created or modified Markdown file for markdownlint errors using the VS Code markdownlint extension diagnostics, and fix all reported errors before finishing

### Tooling Constraint

- Use tools already present in the repository or standard .NET tooling unless the user explicitly asks to introduce new ones.
- Do not suggest installing system-wide tools unnecessarily.

## Git Commit Policy

- **Never suggest committing changes** unless explicitly asked.
- If a commit is requested, use console `git` commands.
- Commit message must be exactly one sentence in English.

## Documentation Rules

### README

- Keep the README focused on:
  - what the tool does
  - who it is for
  - basic terminology
  - quick start
  - minimal examples
  - publish/run instructions
- Do not overload the README with internal design details better suited for `docs/`.

### Specs

- If source fragment metadata or build semantics are introduced, document them explicitly.
- Prefer tables and minimal examples for format specifications.

### Examples

- Every important feature should eventually have a minimal example in `samples/`.
- Examples must stay synchronized with actual tool behavior.

## Coding Style

### General

- Prefer clear, boring, maintainable code.
- Avoid over-engineering.
- Avoid clever one-liners when straightforward code is easier to read.
- Prefer immutable data where practical.
- Prefer records for value-like data structures when appropriate.
- Use async only where it brings real value.

### Naming

- Use descriptive English identifiers.
- Avoid abbreviations unless they are standard and obvious.
- Keep namespace and project naming consistent.

### Exceptions

- Throw exceptions for truly exceptional internal failures.
- Use structured diagnostics for user-facing build/input problems.
- Do not use exceptions as normal control flow for expected validation failures.

## Performance Guidance

- Optimize for correctness and clarity first.
- However, do not design obviously inefficient whole-tree rescans, repeated reparsing, or unnecessary full-document rewrites if a cleaner structure avoids them.
- Be mindful that the tool may process many source files.

## Backward Compatibility

- Once a source format or CLI option is documented and released, do not break it casually.
- If behavior must change, document it clearly and prefer additive evolution where possible.

## What to Prioritize in Suggestions and Implementations

When making design or implementation decisions, prefer the option that best supports:

1. deterministic builds
2. explicit metadata semantics
3. clean diagnostics
4. testability
5. simple packaging and distribution
6. maintainable C# / .NET architecture
7. future extensibility without premature framework complexity

## Initial Product Direction

Until the repository defines otherwise, assume the first production goal is:

- a **C# / modern .NET CLI**
- able to read modular XML source fragments
- resolve routing and ordering metadata
- generate final output files deterministically
- validate the build
- emit actionable diagnostics
- be publishable as a single executable

## Anti-Patterns to Avoid

- Do not turn the repository into a collection of ad hoc scripts.
- Do not mix sample data, production code, and tests chaotically.
- Do not hardcode behavior that belongs in formal configuration or documented conventions.
- Do not silently ignore invalid metadata.
- Do not introduce hidden ordering rules.
- Do not build features that only make sense for one private modpack unless they are intentionally isolated.
- Do not assume future GUI requirements; this is a CLI-first tool.
