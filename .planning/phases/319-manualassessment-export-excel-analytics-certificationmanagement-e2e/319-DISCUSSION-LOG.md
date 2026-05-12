# Phase 319: ManualAssessment + Export Excel + Analytics + CertificationManagement E2E - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-12
**Phase:** 319-manualassessment-export-excel-analytics-certificationmanagement-e2e
**Mode:** `--auto` (Claude picked recommended defaults inline; user-review-friendly)
**Areas auto-decided:** FLOW Structure, Wave Structure, Export Excel verification, Analytics chart assertion, CertificationManagement scope, ManageCategories scope, ManualAssessment scope, Helper extensions, Requirements mapping

---

## FLOW Structure (D-319-01)

| Option | Description | Selected |
|--------|-------------|----------|
| 5 FLOWs (T/U/V/W/X) one per feature (Recommended) | Cleanest 1:1 feature→FLOW mapping; 20+ sub-tests target | ✓ |
| Consolidated single FLOW Y dengan sub-section | Less granularity, harder grep | |
| 3 super-FLOWs grouping related features | Premature abstraction | |

**Auto-selected:** Recommended (5 FLOWs T/U/V/W/X).

---

## Wave Structure (D-319-02)

| Option | Description | Selected |
|--------|-------------|----------|
| Wave 1 (T+U parallel) + Wave 2 (V→W→X sequential) (Recommended) | Maximize parallelism; sequential where file-locked | ✓ |
| All sequential 5 plans | Simpler reasoning, slower | |
| All parallel | Impossible (shared exam-types.spec.ts file) | |

**Auto-selected:** Recommended (Wave 1 T+U parallel, Wave 2 V→W→X sequential).

---

## Export Excel Verification (D-319-03)

| Option | Description | Selected |
|--------|-------------|----------|
| APIRequest pattern adapting `verifyCertificatePdfDownload` (Recommended) | Proven Phase 318 Plan 04 idiom; consistency | ✓ |
| UI-driven download trigger (button click + `page.waitForEvent('download')`) | More brittle, browser-specific download dir | |
| HTTP fetch via Node fetch | Different auth context, complexity | |

**Auto-selected:** Recommended (APIRequest pattern).

---

## Analytics Chart Assertion Strategy (D-319-04)

| Option | Description | Selected |
|--------|-------------|----------|
| JSON endpoint intercept primary + DOM canvas smoke secondary (Recommended) | Chart.js canvas opaque, JSON ground truth | ✓ |
| Pure DOM scrape (`canvas` element only) | Cannot read chart values, only existence | |
| DB-only cross-check no UI | Skip UI validation entirely | |

**Auto-selected:** Recommended (JSON intercept + DOM smoke + DB cross-check).

---

## CertificationManagement Scope (D-319-05)

| Option | Description | Selected |
|--------|-------------|----------|
| CMP variant only, 3 sub-tests (X1-X3) (Recommended) | Scope-bounded; CDP variant deferred | ✓ |
| Both CMP + CDP variants (6 sub-tests) | Doubles scope, CDP context different | |
| Skip CertificationManagement entirely | Misses feature coverage | |

**Auto-selected:** Recommended (CMP only, 3 sub-tests).

---

## ManageCategories Scope (D-319-06)

| Option | Description | Selected |
|--------|-------------|----------|
| 4 sub-tests CRUD + 1 negative (Recommended) | Balanced coverage, includes duplicate-name reject | ✓ |
| 3 sub-tests CRUD only (no negative) | Misses validation path | |
| 6 sub-tests full edge cases | Time over-budget for low-risk feature | |

**Auto-selected:** Recommended (4 sub-tests U1-U4).

---

## ManualAssessment Scope (D-319-07)

| Option | Description | Selected |
|--------|-------------|----------|
| 6 sub-tests CRUD lifecycle + worker visibility (Recommended) | Full lifecycle + cross-actor verify | ✓ |
| 4 sub-tests CRUD only (no worker visibility) | Misses key UAT confirmation | |
| 8 sub-tests detailed edge cases | Time over-budget for known-stable feature | |

**Auto-selected:** Recommended (6 sub-tests T1-T6).

---

## Helper Extensions (D-319-08)

| Option | Description | Selected |
|--------|-------------|----------|
| Append ke existing `examTypes.ts` (Recommended) | Consistency Phase 317-318 single-file pattern | ✓ |
| Create new `tests/e2e/helpers/adminFeatures.ts` | Split concerns but breaks pattern | |
| Inline helpers per FLOW (no extraction) | Code duplication | |

**Auto-selected:** Recommended (append `examTypes.ts`).

---

## Requirements Mapping (D-319-09)

| Option | Description | Selected |
|--------|-------------|----------|
| Add QA-09 entry to REQUIREMENTS.md + sync ROADMAP (Recommended) | Resolves ROADMAP QA-04 placeholder/conflict cleanly | ✓ |
| Reuse QA-04 (visual regression) for admin features | Conflates two distinct requirements | |
| Skip REQUIREMENTS update | Breaks traceability pattern (Phase 318 precedent) | |

**Auto-selected:** Recommended (QA-09 new entry).

---

## Claude's Discretion

- Exact Plan numbering split (3 plans vs 4 plans) — `/gsd-plan-phase 319` planner decides
- Per-FLOW selector pattern (data-testid vs class+text) — researcher reads existing patterns
- Test runtime budget per sub-test — planner sets (default 60s typical)
- Wave 0 smoke kalau researcher flag YELLOW asumsi (Analytics JSON shape, etc.) — planner adds W0.x sub-test

## Deferred Ideas

- CDP CertificationManagement variant — Phase 320+
- Search-by-NomorSertifikat UAT scenarios — separate phase if UAT bug muncul
- Excel re-query independent verification — proven Phase 301 D-19
- Multi-page pagination edge cases (CertificationManagement page>1) — happy-path sufficient
- ManualAssessment bulk import — out of scope kalau exist
- Analytics drill-down per-employee — dashboard summary only

---

*Auto mode rationale:* User invoked `--auto` flag — all gray areas auto-selected dengan recommended defaults. User review encouraged via CONTEXT.md inspection before `/gsd-plan-phase 319` invocation.
