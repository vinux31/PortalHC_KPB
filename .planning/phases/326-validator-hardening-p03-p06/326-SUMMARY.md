---
phase: 326
status: SHIPPED-LOCAL
shipped: 2026-05-27
milestone: v19.0
push_status: NOT_PUSHED
---

# Phase 326 SHIPPED LOCAL — Validator Hardening (P03 + P06)

**Milestone:** v19.0 Portal HC Bug Fixes (Sertifikat Ecosystem Audit)
**Phase boundary:** Cegah data kontradiktif tersimpan via validator form Add/Edit Training. DAG monotonic renewal + Permanent+ValidUntil mutual exclusion.

## Commits (main lokal, NOT PUSHED)

| Wave | Commit | Description |
|------|--------|-------------|
| 1 | `718c67b8` | feat(326-01): P03 DAG + P06 + self-renewal validators backend + Edit VM extend |
| 2 | `c4c5da2e` | feat(326-02): Razor section read-only renewal + clear button + 2 ValidUntil span |
| 3 | _(this commit)_ | feat(326-03): UAT ALL 6 SC PASS + SHIPPED LOCAL |

## Plans

| Plan | Wave | Tasks | Files | Outcome |
|------|------|-------|-------|---------|
| 326-01 backend | 1 | 3 auto | TrainingAdminController.cs, EditTrainingRecordViewModel.cs | ✅ 7 validators added + GET populate + FK persist + VM extend 3 field |
| 326-02 view | 2 | 2 auto | EditTraining.cshtml, AddTraining.cshtml | ✅ Section card + clear button + 2 ValidUntil span (delta 32 baris ≤ budget 35) |
| 326-03 UAT | 3 | 1 auto + 2 checkpoint | 326-UAT.md | ✅ ALL 6 SC PASS browser-verified + bonus Edit P06 |

## Success Criteria — ALL 6/6 PASS

1. ✅ SC-1 Add TR renewal tanggal < source → form error display (P03)
2. ✅ SC-2 Add TR renewal tanggal valid > source → lolos (P03 no regression)
3. ✅ SC-3 Add TR Permanent + ValidUntil isi → form error display field ValidUntil (P06)
4. ✅ SC-4 Add TR Permanent + ValidUntil null → lolos (P06 no regression)
5. ✅ SC-5 Add TR Annual + ValidUntil valid → lolos (P06 no regression)
6. ✅ SC-6 Edit case self-renewal check + clear button (4-step combined)

## Decision Compliance (D-01..D-10)

All 10 CONTEXT decisions honored:
- D-01..D-05 spec-locked patterns ✅
- D-06 Extend EditTrainingRecordViewModel ✅ (3 field tambahan)
- D-07 Read-only display + clear button UX ✅ (Q1 follow-up A)
- D-08 Symmetric kedua FK (TR + AS via Schedule field) ✅
- D-09 Manual repro only, no xUnit ✅
- D-10 Strict `>` reject same-day via `>=` ✅

## Critical Gap Resolution (PATTERNS.md)

3 critical gap pre-implementation flagged + resolved:
1. AssessmentSession.Schedule field (D-08 open item) → confirmed + used di P03 AS branch
2. EditTraining POST L479-490 missing FK mapping → added 2-line entity assignment
3. ValidUntil span absent di Views/Admin/ → introduced 2 span (Edit + Add), first-time addition

## Threat Mitigation Verified (5/5)

T-326-01, T-326-02, T-326-03, T-326-04, T-326V-01 all browser-verified mitigated.

## Findings (Non-Blocking)

1. Validator order self-renewal scenario — DAG short-circuit catches before self-renewal guard. Tampering tetap rejected. Optional future enhancement.
2. Tom Select Worker re-select UX (pre-existing, out-of-scope).

## Migration & IT Promo

- **Migration:** ❌ Tidak ada (D-04 honored — pure code changes)
- **IT promo:** Deferred ke Phase 327 batch akhir per v19.0 spec §11 strategy (3 phase 325+326+327 push bareng + 1× `dotnet ef database update` saat Phase 327 ship)

## Effort vs Estimate

- ROADMAP estimate: S (~1-2 jam)
- Actual: ~1-1.5 jam (planner overhead included, plan stack lengkap CONTEXT+UI-SPEC+PATTERNS+3 PLAN)
- Risk: Low (no migration, no new dependency, narrow scope)

## Next Phase

`/gsd-plan-phase 327` — Timezone DateOnly Refactor (P04). Migration `ChangeValidUntilToDateOnly` required. Effort M (~1 hari). Risk Medium.
