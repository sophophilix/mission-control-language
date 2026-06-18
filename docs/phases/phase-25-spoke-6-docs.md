# Phase 25 — Spoke 6: Docs

## Status: Todo

## Files to update

### `docs/design/language.md`
- Replace all `|>` with `->` in grammar and examples
- Update ANTLR grammar block
- ~~Add `->` rationale and `parallel { }` block to syntax decisions~~ — done (pre-emptively captured in this session)
- ~~Add `{{ExpertName}}` to reserved variables table~~ — done
- ~~Document domain vs infrastructure variable split rule~~ — done
- Remove OCI expert declaration syntax (`from`, `version`) from grammar
- Update "One mission per file" section to reference two-file model

### `docs/design/architecture.md`
- Update two-file model description: `mission.mcl` + `forge.toml`
- Update expert resolution flow (local → cache → error)
- Update `forge init` description

### `README.md`
- Replace all `|>` with `->` in examples
- Update "Writing a mission" section — no OCI declarations in `.mcl`
- Add `forge.toml` section with schema example
- Update file tree in "Writing an expert" section
- Update CLI command table if needed
- Update "Variables" section — remove infrastructure `let` bindings from `.mcl` examples

### `CLAUDE.md`
- No changes required — AOT rules are unaffected

## Checklist

- [ ] `language.md` — operator, grammar, parallel block
- [ ] `architecture.md` — two-file model, resolution flow
- [ ] `README.md` — all examples updated, forge.toml documented
- [ ] Existing `.mcl` example files in `missions/` updated to use `->`
- [ ] `missions/build-operator/mission.mcl` — remove OCI declarations, add `forge.toml`
- [ ] `missions/elevator-pitch-refined/mission.mcl` — same
