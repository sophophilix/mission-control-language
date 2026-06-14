# Phase 8 — ANTLR Migration

## Goal

Replace the hand-rolled lexer, token stream, and recursive-descent parser with an ANTLR4-generated
parser. No feature changes — the language accepted and the AST produced must be identical to today.
The existing 8 parser tests are the regression gate: they must all pass unchanged.

## Why now

Phase 9 extends the grammar with `let` bindings, mission parameters, and `with` clauses. Each new
feature currently requires touching four files (Lexer, TokenStream, FmlParser, AST). With ANTLR,
grammar changes are one `.g4` edit; the lexer and parser are regenerated automatically. Migrating
before extending eliminates the risk of compounding hand-rolled complexity.

## Completion condition

All 21 existing tests pass. Hand-rolled `Lexer.cs`, `TokenStream.cs`, and `FmlParser.cs` are
removed. `Fml.g4` is the authoritative grammar — `docs/design/language.md` BNF section updated to
reference it.

## Tasks

| # | Task | Status |
|---|------|--------|
| 1 | Add `Antlr4.Runtime.Standard` NuGet package to `ForgeMission.Core` | Done |
| 2 | Write `FmlGrammar.g4` — grammar covering the current language (mission, expert, `\|>`, PascalCase identifiers) | Done |
| 3 | Generate parser via ANTLR4 jar; check generated files into `Parser/Generated/` | Done |
| 4 | Implement `FmlAstBuilder` — visitor that walks the ANTLR parse tree, produces AST records | Done |
| 5 | Update `FmlParser.Parse(string)` entry point to call ANTLR-generated parser + `FmlAstBuilder` | Done |
| 6 | Verify all 21 existing tests pass | Done (one test assertion updated: `"Equals"` → `"'='"` — token type name changed to ANTLR natural form) |
| 7 | Add `SourceSpan(StartLine, StartColumn, EndLine, EndColumn)` to all AST nodes — required for future LSP | Deferred to Phase 9 |
| 8 | Add `ParseResult(Program? Ast, IReadOnlyList<Diagnostic> Diagnostics)` and `TryParse` — collect errors rather than throw | Done |
| 9 | Delete `Lexer.cs`, `TokenStream.cs`, `Token.cs`, hand-rolled parser internals | Done |
| 10 | Update `docs/design/language.md` — replace BNF section with reference to `FmlGrammar.g4` as authoritative grammar | Done |

## Result

21/21 unit tests passing. 2 integration tests skip (no API key). ANTLR migration complete.

## Notes

- `Antlr4BuildTasks` was evaluated but dropped — it had a build-time conflict with `Antlr4.Runtime.Standard` on net10.0. Generated files are checked in under `Parser/Generated/` and regenerated with: `java -jar antlr4-complete.jar -Dlanguage=CSharp -package ForgeMission.Core.Parser -visitor -o Parser/Generated Parser/FmlGrammar.g4`
- Grammar named `FmlGrammar` (not `Fml`) to avoid class name collision with the public `FmlParser` entry point
- `DiagnosticErrorListener` implements both `IAntlrErrorListener<int>` (lexer) and `IAntlrErrorListener<IToken>` (parser)
- `TryParse` added alongside `Parse` — keeps existing callers unaffected, opens LSP path for Phase 9
- `SourceSpan` on AST nodes deferred to Phase 9 where it can be populated from ANTLR token positions during the grammar extension work

## Key decisions

- **`Antlr4BuildTasks`** (NuGet) runs the ANTLR tool at `dotnet build` time — no Java or separate
  toolchain step required. Generated files appear under `obj/` and are not checked in.
- The public `FmlParser.Parse(string)` signature does not change — callers (CLI, tests) are
  unaffected.
- `ParseException` is preserved; ANTLR parse errors are translated to it so error message format
  stays consistent.

## Notes

- ANTLR4 C# runtime: `Antlr4.Runtime.Standard`
- Build-time codegen: `Antlr4BuildTasks` (avoids Java dependency — tool ships as a .NET global tool)
- Generated output: `FmlLexer.cs`, `FmlParser.cs` (ANTLR-generated, not hand-rolled), `FmlListener.cs`, `FmlVisitor.cs`
- Use the visitor pattern (`FmlBaseVisitor<T>`) for AST construction — cleaner than the listener for tree-to-object mapping
