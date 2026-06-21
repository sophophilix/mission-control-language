# Phase 26 — Spoke 1: Source Positions

## Status: Done

## Problem

Every AST node currently carries only semantic content — names, bindings, pipelines. There is no record of where in the source file a node came from. Without this, hover, go-to-definition, and error underlining are impossible — a tool cannot map a cursor position back to an AST node.

## Solution

Add a `SourceSpan` record to every AST node. ANTLR provides token positions for free; the change is carrying them through into the AST during the parse visitor pass.

```csharp
record SourceSpan(int StartLine, int StartCol, int EndLine, int EndCol);
```

Every AST node gains a `Span` property:

```csharp
record ExpertRef(string Name, SourceSpan Span);
record Step(ExpertRef Expert, WithClause? With, SourceSpan Span);
record MissionDeclaration(string Name, IReadOnlyList<string> Params, Pipeline Pipeline, SourceSpan Span);
// ... all nodes
```

## ANTLR position extraction

In the ANTLR visitor, every context object exposes `.Start` and `.Stop` tokens with `.Line` and `.Column`:

```csharp
SourceSpan SpanOf(ParserRuleContext ctx) => new(
    ctx.Start.Line,
    ctx.Start.Column,
    ctx.Stop.Line,
    ctx.Stop.Column + ctx.Stop.Text.Length
);
```

Call this when constructing each AST node in the visitor.

## Why now

Source positions are cheap to add while the AST is being actively worked on (Phase 25 Spoke 1 is already touching every node). Retrofitting across a stable AST later requires touching every construction site twice. This is the one structural addition that must precede all editor tooling.

## Test gate

Existing parser tests must still pass. Add new tests asserting that parsed nodes carry correct line/column values for a known input string.
