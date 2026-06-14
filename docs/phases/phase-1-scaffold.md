# Phase 1 — Project Scaffold

## Goal

Create the solution structure, projects, and package references. Nothing functional yet, but all dependency boundaries are enforced by project references and `dotnet build` passes clean.

## Completion condition

`dotnet build` passes with zero errors and zero warnings across all projects.

## Tasks

| # | Task | Status |
|---|------|--------|
| 1 | Create `ForgeMission.sln` solution file | Not Started |
| 2 | Create `ForgeMission.Core` class library project | Not Started |
| 3 | Create `ForgeMission.Cli` console project | Not Started |
| 4 | Create `ForgeMission.Tests` xUnit test project | Not Started |
| 5 | Add project reference: `Cli` → `Core` | Not Started |
| 6 | Add project reference: `Tests` → `Core` | Not Started |
| 7 | Add MAF package references to `Core` (`Microsoft.Agents.AI`) | Not Started |
| 8 | Add `YamlDotNet` package reference to `Core` (frontmatter parsing) | Not Started |
| 9 | Add `System.CommandLine` package reference to `Cli` | Not Started |
| 10 | Create top-level folder structure: `src/`, `examples/`, `runs/` | Not Started |
| 11 | Add `runs/` to `.gitignore` | Not Started |
| 12 | Verify `dotnet build` passes clean | Not Started |
