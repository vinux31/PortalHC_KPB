---
phase: 386-assessmentadmincontroller-hardening
plan: 04
subsystem: assessment-grading
tags: [essay-grading, pending-count, upsert, status-guard, ef-core, sql-server, pxf-04, hotfix]

# Dependency graph
requires:
  - phase: 386-01-wave0-red-scaffolds
    provides: "EssayEmptyPendingParityTests (8 Fact, Integration) — 4-fixture count-parity + upsert + status-guard mirrors with drift-guard cites L3308/3500/3547/3620; SubmitEssayScore authz reflection lock"
  - phase: 386-02-wave1-helpers
    provides: "(indirect) GREEN test project so the parity/authz suite compiles and runs"
provides:
  - "PXF-04 CLOSED — single pending essay predicate !IsNullOrWhiteSpace(TextAnswer) && EssayScore==null applied byte-identical at all 4 count surfaces in AssessmentAdminController.cs"
  - "SubmitEssayScore defensive upsert (create row if absent, TextAnswer=null) replacing the 'Jawaban tidak ditemukan' dead-end (D-08)"
  - "SubmitEssayScore mandatory status-guard: reject when Status != PendingGrading (T-386-AUTHZ HIGH, closes F-03 widening)"
  - "EssayEmptyPendingParityTests turned GREEN 6/6 (incl whitespace tab/newline variant) — full 4-surface count parity"
affects: [386-06, 386-wave4]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "EF↔.NET whitespace parity: filter EssayScore==null + Join server-side, materialize TextAnswer, then evaluate IsNullOrWhiteSpace in-memory — because SQL Server LTRIM/RTRIM/TRIM only strip ASCII space (not tab/newline)"
    - "Defensive upsert with mandatory status-guard ordering: status-guard -> load question -> range-guard -> upsert (invalid score never materializes a row)"

key-files:
  created:
    - .planning/phases/386-assessmentadmincontroller-hardening/386-04-SUMMARY.md
  modified:
    - Controllers/AssessmentAdminController.cs
    - HcPortal.Tests/EssayEmptyPendingParityTests.cs

key-decisions:
  - "Whitespace evaluated IN-MEMORY at the 2 EF sites (SubmitEssayScore + Monitoring), NOT server-side — RESEARCH L60 assumed IsNullOrWhiteSpace translates to `= N''`, but probe of SQL Server EF Core 8 proved LTRIM/RTRIM/TRIM never treat tab(CHAR9)/newline(CHAR10) as empty, so server-side eval diverged from .NET for TextAnswer='\\t\\n'. Logical predicate kept byte-identical at all 4 surfaces; only the whitespace eval point shifted server->memory to guarantee parity (Rule 1 auto-fix)."
  - "SubmitEssayScore status-guard message 'Penilaian hanya bisa dilakukan saat status Menunggu Penilaian.' rejects any non-PendingGrading session (mirrors FinalizeEssayGrading:3591 precedent)."
  - "Upsert creates PackageUserResponse with PackageOptionId=null, TextAnswer=null, EssayScore=score (idiom from AssessmentHub.SaveTextAnswer); 0 migration (all fields nullable, INSERT of existing entity)."

patterns-established:
  - "Pattern: when an EF-translatable claim breaks on a whitespace/collation edge case, materialize the server-filtered (small) set and apply the .NET predicate in-memory rather than forcing a brittle SQL string-function equivalent."

requirements-completed: [PXF-04]

# Metrics
duration: 14min
completed: 2026-06-15
---

# Phase 386 Plan 04: Wave-3 Essay-Empty Finalize Parity + Upsert Summary

**Closes F-04 (essay-empty dead-end) by applying the single pending predicate `!string.IsNullOrWhiteSpace(TextAnswer) && EssayScore == null` byte-identical at all 4 count surfaces, plus making `SubmitEssayScore` a defensive upsert guarded by a mandatory `Status == PendingGrading` check — with a Rule-1 correction moving whitespace evaluation in-memory at the 2 EF sites so SQL Server's tab/newline blind spot can't re-introduce count divergence.**

## Performance

- **Duration:** ~14 min
- **Started:** 2026-06-15T14:59:37Z
- **Completed:** 2026-06-15T15:14:31Z
- **Tasks:** 3
- **Files modified:** 2 (1 production controller, 1 test) + this SUMMARY

## Accomplishments

- **Task 1 — 4-site pending predicate (PXF-04 D-06):** applied `!string.IsNullOrWhiteSpace(TextAnswer) && EssayScore == null` at all four count surfaces with a `// Phase 386 PXF-04 D-06` marker each:
  - SITE 1 (page count, in-memory `items.Count`)
  - SITE 2 (finalize-gate, in-memory `essayResponses.Any`)
  - SITE 3 (`SubmitEssayScore` pending count, EF)
  - SITE 4 (`AssessmentMonitoringDetail` count, EF)
- **Task 2 — SubmitEssayScore upsert + status-guard (D-08, T-386-AUTHZ):** rewrote the method body in the mandated order (status-guard → load question + range-guard → upsert → pending count). Removed the `"Jawaban tidak ditemukan"` dead-end; added a status-guard rejecting any non-`PendingGrading` session; retained `[HttpPost] [Authorize(Roles="Admin, HC")] [ValidateAntiForgeryToken]`.
- **Task 3 — integration parity GREEN:** ran the `EssayEmptyPendingParityTests` suite against real SQLEXPRESS. Discovered + fixed a real EF↔.NET whitespace divergence (see Deviations) so all 6 parity/upsert/status-guard tests pass and the full suite stays green.

## Final Edited Line Ranges (production)

| Site | Surface | Location (post-edit) | Eval |
|------|---------|----------------------|------|
| 1 | Page count `EssayGradingPageViewModel` | L3506 `items.Count(i => !IsNullOrWhiteSpace(i.TextAnswer) && i.EssayScore==null)` | in-memory |
| 2 | Finalize-gate | L3650 `essayResponses.Any(r => !IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore==null)` | in-memory |
| 3 | `SubmitEssayScore` pending count | L3576-3580 (EF: filter `EssayScore==null` + Join Essay, materialize `TextAnswer`; then `.Count(t => !IsNullOrWhiteSpace(t))`) | in-memory whitespace |
| 4 | `AssessmentMonitoringDetail` count | L3308-3321 (EF: filter `EssayScore==null` + Join Essay, materialize; then in-memory `Where !IsNullOrWhiteSpace` + GroupBy) | in-memory whitespace |

## Final SubmitEssayScore Body Order (L3527-3582)

1. **Status-guard** — `var session = FindAsync(sessionId)`; reject `null` → "Session tidak ditemukan"; reject `Status != PendingGrading` → "Penilaian hanya bisa dilakukan saat status Menunggu Penilaian."
2. **Load question + range-guard** — `FindAsync(questionId)`; reject `null` → "Soal tidak ditemukan"; reject `score < 0 || score > ScoreValue` → "Skor harus antara 0 dan {ScoreValue}". (Runs BEFORE upsert — invalid score never materializes a row.)
3. **Upsert** — `FirstOrDefaultAsync` by session+question; if absent → `new PackageUserResponse { PackageOptionId=null, TextAnswer=null, EssayScore=score }` + `.Add`; else `response.EssayScore = score`; `SaveChangesAsync`.
4. **Pending count** (site 3) — server-filter `EssayScore==null` + Join Essay, materialize `TextAnswer`, in-memory `.Count(!IsNullOrWhiteSpace)`.

## Integration Test Result

SQLEXPRESS was **available** (not skipped). Results:

| Suite | Result |
|-------|--------|
| `dotnet test --filter "FullyQualifiedName~EssayEmptyPendingParity"` | **6/6 GREEN** (4 count-parity fixtures incl whitespace tab/newline + upsert + status-guard) |
| `dotnet test --filter "FullyQualifiedName~Authz"` | **2/2 GREEN** (incl `SubmitEssayScore_RetainsAuthorizeAfterStatusGuardEdit` reflection lock) |
| `dotnet test` (full) | **474/474 GREEN**, 0 failed, 0 skipped |
| `dotnet build` | **0 Error(s)** (EF site 3/4 still translatable for the server-side `EssayScore==null` + Join portion) |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] SQL Server whitespace divergence broke 4-surface parity for tab/newline essays**
- **Found during:** Task 3 (running the parity suite). `PendingCount_IdenticalAcrossAllFourSites_WhitespaceText` failed: the 2 EF sites (Monitoring [0], Submit [2]) returned 1 while the 2 in-memory sites returned 0, for `TextAnswer = "\t\n"`.
- **Root cause:** The plan/RESEARCH (RESEARCH L60/L301) assumed `string.IsNullOrWhiteSpace` translates to `[col] IS NULL OR [col] = N''` and is therefore safe server-side. Direct probing of SQL Server (EF Core 8.0) proved this wrong for non-space whitespace: `N''` comparison, `LTRIM/RTRIM`, and even `TRIM` all leave `CHAR(9)+CHAR(10)` (tab+newline) intact — SQL Server string functions only strip ASCII space (U+0020). So server-side `IsNullOrWhiteSpace` diverged from .NET, re-introducing exactly the cross-surface count divergence F-04 is meant to eliminate (threat T-386-04-COUNT).
- **Fix:** At the 2 EF sites (SubmitEssayScore site 3, Monitoring site 4) I kept the `EssayScore == null` filter + Essay Join server-side, materialized only `TextAnswer` (a tiny set — bounded by ungraded essays for ≤30 participants), then applied `!string.IsNullOrWhiteSpace(...)` in-memory with full .NET semantics. The **logical predicate stays byte-identical** at all 4 surfaces (`!IsNullOrWhiteSpace(TextAnswer) && EssayScore == null`); only the whitespace evaluation point shifted server→memory. I did NOT change the predicate text (per plan instruction). I then realigned the 2 EF mirror builders in `EssayEmptyPendingParityTests` to match the new production shape (they previously encoded the whitespace check inside the EF query) so the drift-guards remain accurate.
- **Rationale for not escalating (Rule 4):** This is a correctness bug directly caused by this task's edits (a failing parity assertion + cross-surface divergence = the F-04 defect itself), not an architectural change — no new table/service/library/auth approach. The plan's "no `.AsEnumerable()` at sites 3/4" guidance rested on a premise (clean EF translation) that probing falsified; following it literally would ship the very bug PXF-04 closes. Performance impact is nil (≤30 participants × a handful of essay questions per session).
- **Files modified:** `Controllers/AssessmentAdminController.cs` (sites 3 & 4), `HcPortal.Tests/EssayEmptyPendingParityTests.cs` (2 mirror builders realigned).
- **Commit:** `866917b6`

## Authentication Gates

None. No auth gate occurred during execution.

## Issues Encountered

- `sqlcmd` on this Windows host required `-C` (TrustServerCertificate) and rejected POSIX `/tmp` paths; used inline `-Q` probes against `localhost\SQLEXPRESS` to confirm the SQL Server LTRIM/RTRIM/TRIM behavior on tab/newline.
- A cosmetic `LF will be replaced by CRLF` git warning on the test file (line-ending normalization) — no functional impact.

## Known Stubs

None. All 4 surfaces are wired to live data via the single predicate; SubmitEssayScore upserts real rows. PXF-04 is fully closed.

## Threat Flags

None new. The plan's threat register (T-386-AUTHZ, T-386-04-UPSERT, T-386-04-COUNT, T-386-04-DOS) is fully mitigated:
- T-386-AUTHZ → status-guard rejects non-PendingGrading (verified by `SubmitEssayScore_NonPendingGrading_Rejected`).
- T-386-04-UPSERT → range-guard runs before `Add` (ordering verified).
- T-386-04-COUNT → byte-identical predicate + 6/6 parity (incl whitespace edge) — strengthened vs plan by the in-memory whitespace fix.
- T-386-04-DOS → whitespace/empty/no-row essays count as not-pending; finalize gate no longer blocks; empty essay auto-0 via existing `Compute`.

## User Setup Required

None. **0 migration** (upsert is an INSERT of the existing `PackageUserResponse` entity; all touched fields already nullable). Verify locally per CLAUDE.md Develop Workflow before promotion; IT re-deploys Dev.

## Next Phase Readiness

- **Plan 06 (verification/e2e wave):** the Playwright spec `essay-empty-finalize-386.spec.ts` (currently `test.fixme`) can be un-skipped — HC can now finalize a session whose only remaining "pending" essays are empty, and `SubmitEssayScore` returns success (not "Jawaban tidak ditemukan") for empty essays.
- **Wave 4 (PXF-05):** untouched by this plan; `GeneratePerPesertaPdf` MA `SetEquals` wiring remains the only open 386 controller edit.
- No blockers. 0 migration. PXF-04 CLOSED.

## Self-Check: PASSED

- `Controllers/AssessmentAdminController.cs` modified (4 PXF-04 D-06 markers, status-guard ×1, upsert `.Add` ×1, 0 active dead-end return) — verified on disk.
- `HcPortal.Tests/EssayEmptyPendingParityTests.cs` modified (2 EF mirrors realigned) — verified on disk.
- `.planning/phases/386-assessmentadmincontroller-hardening/386-04-SUMMARY.md` created — this file.
- Commits present in git history: `6efd0294` (Task 1), `79132809` (Task 2), `866917b6` (Task 3).
- Build 0 error; parity 6/6; authz 2/2; full suite 474/474.

---
*Phase: 386-assessmentadmincontroller-hardening*
*Completed: 2026-06-15*
