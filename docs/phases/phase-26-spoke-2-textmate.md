# Phase 26 — Spoke 2: TextMate Grammar

## Status: Done

## What it delivers

Syntax highlighting in VS Code, GitHub, Sublime Text, and any editor that supports TextMate grammars — with no LSP required. The `.mcl` file lights up correctly on first open.

## Token categories

Derived directly from the ANTLR grammar:

| Category | Tokens | Colour role |
|----------|--------|-------------|
| Keywords | `mission`, `expert`, `let`, `with`, `env`, `parallel`, `loop`, `from`, `as` | keyword |
| Operators | `->`, `=` | operator |
| Expert identifiers | `UPPER_ID` (PascalCase) | entity.name / type |
| Variable identifiers | `LOWER_ID` (camelCase) | variable |
| String literals | `"..."` | string |
| Numbers | integer literals (loop count) | constant.numeric |
| Comments | `//` to end of line | comment |
| Block delimiters | `{`, `}`, `(`, `)` | punctuation |
| Placeholder | `{{key}}` inside strings | keyword.other (distinct colour) |

## Deliverables

- `editors/vscode/syntaxes/mcl.tmLanguage.json` — TextMate grammar
- `editors/vscode/package.json` — VS Code extension manifest (`forge-mcl`, publisher `forgelang`)
- `editors/vscode/language-configuration.json` — bracket matching, comment toggling, folding
- Publication to VS Code Marketplace — deferred (requires registered publisher)

## Notes

The `{{key}}` placeholder pattern inside expert system prompts (in `expert.md` files) should also be highlighted. This requires a second grammar scope for `.md` files that embed MCL placeholder syntax — low priority, can follow in a later iteration.
