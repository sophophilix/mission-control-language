# Phase 3 — Expert Loader

## Goal

Resolve expert names from the AST to markdown files on disk. Parse YAML frontmatter. Validate that every expert referenced in a mission exists before execution begins.

## Completion condition

All unit tests pass. Loader correctly resolves experts from fixture files, parses frontmatter, and produces clear errors for missing or malformed experts — before any LLM call is made.

## Tasks

| # | Task | Status |
|---|------|--------|
| 1 | Define `ExpertDefinition` model (`Name`, `Input`, `Output`, `SystemPrompt`) | Not Started |
| 2 | Implement `ExpertLoader` — scans `experts/` directory for `.md` files | Not Started |
| 3 | Implement YAML frontmatter parser using `YamlDotNet` (`name`, `input`, `output`) | Not Started |
| 4 | Implement body extraction — content below the frontmatter block becomes `SystemPrompt` | Not Started |
| 5 | Implement `ExpertLoader.LoadAll()` — returns `Dictionary<string, ExpertDefinition>` | Not Started |
| 6 | Implement `ExpertLoader.Validate(Program ast)` — checks all referenced experts exist | Not Started |
| 7 | Unit test: loads a valid expert markdown file correctly | Not Started |
| 8 | Unit test: parses `name`, `input`, `output` from frontmatter | Not Started |
| 9 | Unit test: body below frontmatter becomes `SystemPrompt` | Not Started |
| 10 | Unit test: missing expert referenced in mission produces clear error | Not Started |
| 11 | Unit test: missing frontmatter field produces clear error | Not Started |
| 12 | Unit test: directory with multiple experts loads all correctly | Not Started |
