# Phase 343: Integrasi App-wide - Discussion Log

> **Audit trail only.** Do not use as input to planning/research/execution agents.
> Decisions captured in CONTEXT.md — this log preserves alternatives considered.

**Date:** 2026-06-03
**Phase:** 343-integrasi-app-wide
**Areas discussed:** @inject placement, audit ganti-vs-skip philosophy, scope area realism, ambiguous occurrence handling

---

## @inject placement

| Option | Description | Selected |
|--------|-------------|----------|
| Global _ViewImports.cshtml | 1 line @inject, all views get OrgLabels, spec §5 aligned, minimal footprint | ✓ |
| Per-view @inject | Explicit per file, no global, but repetitive + forgettable | |

**Choice:** D-01 Global _ViewImports.cshtml

---

## Audit ganti-vs-skip philosophy

| Option | Description | Selected |
|--------|-------------|----------|
| Pragmatic high-value | Replace only meaning-changing display labels (filter/th/form/breadcrumb); skip doc/technical/ambiguous | ✓ |
| Exhaustive | Replace every display occurrence in matched files | |

**Choice:** D-02 Pragmatic high-value (focus SC2, lower over-replace risk)

---

## Scope area realism

| Option | Description | Selected |
|--------|-------------|----------|
| Audit-driven actual | Scope = actual grep (CMP/CDP/ProtonData + Admin); Worker/CoachMapping/Renewal/DocumentAdmin audit-only (0/few) | ✓ |
| Strict 7-area spec | Force-search all 7 even if Views empty | |

**Choice:** D-03 Audit-driven actual (reality over spec-literal; Worker/CoachMapping/Renewal/DocumentAdmin Views = 0 "Bagian"/"Unit")

---

## Ambiguous occurrence handling

| Option | Description | Selected |
|--------|-------------|----------|
| Claude's discretion + rule | Per-case in SC1 audit: replace clear user-facing; skip data-field-name desc (import help); combined → GetLabel per-part or skip | ✓ |
| Aggressive replace all | Include help-table + combined | |
| Conservative skip ambiguous | Only 100%-clear labels | |

**Choice:** D-04 Claude's discretion + documented rule

## Claude's Discretion

- Audit deliverable format (SC1 grep + ganti/skip table)
- Controller @inject only where display TempData/ViewBag exists
- Plan structure: audit-then-apply vs per-area (planner)

## Deferred Ideas

- Formal test + UAT + regression → Phase 344 (TEST-01..06, ORG-INTEG-03)
- ManageOrganization (JS-fetch, Phase 342) not re-touched
