# Phase 2 — Parser

## Goal

Implement the FML lexer, token stream, recursive-descent parser, and AST. Pure C#, no external dependencies. Input is a string, output is an AST.

## Completion condition

All unit tests pass. Parser correctly handles valid missions, valid experts, valid pipelines, and produces clear errors for invalid input — with no LLM or disk I/O involved.

## Tasks

| # | Task | Status |
|---|------|--------|
| 1 | Define token types (`Mission`, `Expert`, `Pipe`, `Identifier`, `Equals`, `EOF`, `Unknown`) | Not Started |
| 2 | Implement `Lexer` — converts input string to `IEnumerable<Token>` | Not Started |
| 3 | Implement `TokenStream` — provides `Peek()`, `Consume()`, `Expect()` over token sequence | Not Started |
| 4 | Define AST node types (`Program`, `MissionDeclaration`, `ExpertDeclaration`, `Pipeline`, `Identifier`) | Not Started |
| 5 | Implement `Parser.ParseProgram()` — entry point, loops calling `ParseDeclaration()` | Not Started |
| 6 | Implement `Parser.ParseDeclaration()` — dispatches on `mission` / `expert` keyword | Not Started |
| 7 | Implement `Parser.ParsePipeline()` — consumes identifiers separated by `\|>` | Not Started |
| 8 | Implement `ParseError` with line/column information | Not Started |
| 9 | Unit test: valid mission with single expert | Not Started |
| 10 | Unit test: valid mission with multi-step pipeline | Not Started |
| 11 | Unit test: valid expert declaration | Not Started |
| 12 | Unit test: recursive expert (expert referencing other experts) | Not Started |
| 13 | Unit test: mission and expert declared in same file | Not Started |
| 14 | Unit test: lowercase identifier produces parse error | Not Started |
| 15 | Unit test: missing `=` produces parse error with useful message | Not Started |
| 16 | Unit test: empty pipeline produces parse error | Not Started |
