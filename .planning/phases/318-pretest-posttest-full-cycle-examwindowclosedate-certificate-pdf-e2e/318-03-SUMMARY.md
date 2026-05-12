---
phase: 318
plan: 03
status: completed
commit: 063a4763
date: 2026-05-12
requirements-completed: [QA-08]
---

# Plan 318-03 Summary — FLOW P PrePostTest + FLOW Q EWCD Reject (10/10 PASS)

## Outcome

**FLOW P 6/6 + FLOW Q 4/4 HIJAU + cumulative regression 38/38 + arsitektur shared-package design discovered.**

## Files Modified

| File | LOC delta | Purpose |
|------|-----------|---------|
| `tests/e2e/helpers/wizardSelectors.ts` | +16 LOC | Append `prePostWizardSelectors` (8 fields) |
| `tests/helpers/utils.ts` | +6 LOC | Append `yesterday()` helper |
| `tests/e2e/helpers/examTypes.ts` | +110 LOC | Append `CreatePrePostOpts` + `createPrePostAssessmentViaWizard` |
| `tests/e2e/exam-types.spec.ts` | +295 LOC | Append FLOW P (6 sub-tests) + FLOW Q (4 sub-tests) |

## FLOW P (6/6 PASS, 30.9s)

- **P1** (5.1s): HC wizard PrePostTest create — ATOMIC 2-session pair (LinkedSessionId cross-link verified DB linkedCount=2)
- **P2** (3.8s): HC creates `Paket-Pre` package + Q_PRE_MARKER untuk preId
- **P3** (3.6s): HC creates `Paket-Post` package + Q_POST_MARKER untuk postId (DB-based id lookup filter by AssessmentSessionId + PackageName)
- **P4** (3.8s): Worker direct nav `/CMP/StartExam/${preId}` → generic answer (first qcard + first option) → submit → DB Status=Completed
- **P5** (3.7s): Worker direct nav `/CMP/StartExam/${postId}` → generic answer → submit → DB Status=Completed
- **P6** (1.8s): HC MonitoringDetail loads HTTP 200 + DB-based pair Status=Completed re-assert

## FLOW Q (4/4 PASS, 21s)

- **Q1** (4.8s): HC wizard standard mode dengan today scheduleDate (00:01) + today early-time EWCD (00:02) — wizard validation pass + EWCD already past at WIB run-time
- **Q2** (3.9s): HC creates package + extract sessionId via DB query (`Users` table, custom Identity rename — bukan default `AspNetUsers`)
- **Q3** (1.3s): Worker direct nav `/CMP/StartExam/{sessionId}` → server-side guard `CMPController.cs:863` → `redirect /CMP/Assessment` + `.alert-danger "Ujian sudah ditutup"` visible
- **Q4** (167ms): DB verify Status NOT IN ('InProgress', 'Completed') + StartedAt IS NULL

## Cumulative Regression

```
exam-types.spec.ts: 38/38 PASS (2.9m)
  setup (1) + W0.x (2) + FLOW K (5) + L (6) + M (5) + N (4) + O (5) + P (6) + Q (4)
```

Phase 317 baseline 28/28 preserved (no regression dari Plan 03 append).

## Wave 0 YELLOW Assumptions — Resolution

| Assumption | Status | Detail |
|------------|--------|--------|
| A2 — `createdAssessmentData` JSON parse | RESOLVED | Helper parse success. DB-fallback coded but not triggered di P1. |
| A3 — MonitoringDetail statusSummary rendering | DEVIATED | Literal `"PreTest:Completed,PostTest:Completed"` text TIDAK rendered di initial DOM (likely per-card badge via JS post-load). P6 switch ke DB-based pair Status verify + light UI HTTP 200 smoke. |
| A7 — PreTest schedule today visibility | RESOLVED via direct nav | Direct `/CMP/StartExam/${id}` bypass `/CMP/Assessment` card list entirely. No card disambiguation hazard. |

## Architectural Finding — Shared Package Pool Design

**Critical discovery dalam Plan 03 execution:**

`CMPController.cs:905-934` `BuildCrossPackageAssignment`:
```csharp
var siblingSessionIds = await _context.AssessmentSessions
    .Where(s => s.Title == assessment.Title &&
                s.Category == assessment.Category &&
                s.Schedule.Date == assessment.Schedule.Date)
    .Select(s => s.Id).ToListAsync();

var packages = await _context.AssessmentPackages
    .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))
    .ToListAsync();
// ...
var shuffledIds = BuildCrossPackageAssignment(packages, rng);
```

**Implikasi:** Untuk PrePostTest assessment, sibling sessions (Pre + Post yang punya Title+Category+Schedule.Date sama) **share package pool**. Saat Worker StartExam, cross-package shuffle pick soal random dari pool gabungan. Bukan: "PreTest wajib pakai Paket-Pre, PostTest wajib pakai Paket-Post".

**Bukti:** Plan 03 awal asumsi distinct marker (Q_PRE_MARKER hanya di Pre, Q_POST_MARKER hanya di Post) — fails karena postId session render Q_PRE_MARKER (pool-random pick).

**Decision (user-confirmed 2026-05-12 diskusi rekan kerja):** Ikuti desain sistem sekarang. P4/P5 rewrite generic-answer (first qcard + first option). Distinct marker assertion dropped. Markers `Q_PRE_MARKER`, `Q_POST_MARKER`, `PRE_CORRECT`, `POST_CORRECT` kept di code sebagai documentation only (suppressed via `void`).

**SamePackage flag** (controller `AssessmentAdminController.cs:1217`, `4434-4441`) — separate mechanism:
- `SamePackage=true` → HC bikin package di Pre auto-sync ke Post + Post locked
- `SamePackage=false` (default Plan 03) → Pre+Post terpisah tapi tetap share pool via siblingSessionIds query above

## Deviations dari Plan 03 RESEARCH

1. **P4/P5 generic-answer pattern** — distinct marker assertion impossible per shared-package design (above). Use `firstQCard.locator('label.list-group-item').first().locator('input.exam-radio').check()`.
2. **P2/P3 inline package creation** — `createDefaultPackage` helper `.first()` ambiguous untuk shared-package group. Inline `Paket-Pre` / `Paket-Post` create + DB-based packageId lookup (filter AssessmentSessionId + PackageName).
3. **P6 DB-based pair verify** (bukan DOM scrape / JSON intercept) — MonitoringDetail tidak render literal statusSummary text di initial DOM.
4. **Q1 schedule = today (bukan yesterday)** — wizard validation reject "Schedule date cannot be in the past". EWCD pakai today 00:02 (already past WIB at run-time) untuk trigger guard.
5. **Q2 DB table = `Users` (bukan `AspNetUsers`)** — project pakai custom Identity table rename.
6. **`queryString` (bukan `queryScalar`)** untuk Status string lookups — `queryScalar` numeric-only.
7. **Helper `prePostWizardSelectors.jadwalSection` waitFor removed dari Step 1** — section di Step 3 wrapper, tidak visible saat Step 1 aktif.

## Build + Type Gate

- `cd tests && npx tsc --noEmit`: **exit 0**

## Next

Wave 2 sequential lanjut:
- **Plan 318-04** — FLOW R (Certificate PDF Download 5 sub-tests R1-R5) + FLOW S (AllowAnswerReview True vs False 6 sub-tests S1-S6) — append exam-types.spec.ts
- **Plan 318-05** — docs finalize + REQUIREMENTS.md QA-08 entry + ROADMAP.md sync + Phase 318 summary report + final regression gate
