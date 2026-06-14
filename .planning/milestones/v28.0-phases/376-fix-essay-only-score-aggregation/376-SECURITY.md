---
phase: 376-fix-essay-only-score-aggregation
auditor: gsd-security-auditor
asvs_level: 1
block_on: high
threats_open: 0
date: 2026-06-14
---

# Phase 376 — Security Audit Report

**Phase:** 376 — Fix Essay-Only Score Aggregation
**ASVS Level:** 1
**Block On:** HIGH
**Threats Closed:** 14/14
**Threats Open:** 0/14
**Result:** SECURED

---

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-376-01 | I (Info Disclosure) | accept | CLOSED | 376-DIAGNOSE.md contains only local test data (sessionId 9019, Score=80, local HcPortalDB_Dev). No prod credentials, no PII. File at .planning (not deployed). DB Dev/Prod untouched per CLAUDE.md constraint confirmed in 376-01-SUMMARY.md Cleanup section. |
| T-376-02 | T (Tampering) | mitigate | CLOSED | e2e harness (global.setup/teardown) executed auto snapshot-restore for all 2 repro runs. 376-01-SUMMARY.md Cleanup: "DB lokal verified bersih (58 sessions, 0 leftover). teardown RESTORE OK." No permanent seed left. No manual seed = no SEED_JOURNAL entry required; harness-managed teardown satisfies SEED_WORKFLOW discipline. |
| T-376-03 | T (Tampering) | mitigate | CLOSED | Helpers/AssessmentScoreAggregator.cs L58: `int percentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;   // D-04 LOCKED` — formula matches declared mitigation verbatim. AssessmentScoreAggregatorTests.cs Mixed_McMaEssay_MatchesInlineFormula_90Percent [Fact] asserts Percentage==90 (no-drift). 376-02-SUMMARY: unit test 6/6 GREEN. |
| T-376-04 | E (Elevation) | accept | CLOSED | AssessmentAdminController.cs L3466-3468: `[HttpPost]`, `[Authorize(Roles = "Admin, HC")]`, `[ValidateAntiForgeryToken]` all present on FinalizeEssayGrading. 376-02-SUMMARY invariant table confirms "Replay-guard (310) Status==PendingGrading 3 preserved". Not modified by this phase. |
| T-376-05 | R (Repudiation) | accept | CLOSED | AssessmentAdminController.cs L3621-3634: `_auditLog.LogAsync(currentUser?.Id ?? "", actorName, "FinalizeEssayGrading", ...)` present. 376-02-SUMMARY invariant table shows no touch to audit region. AuditLogService.cs L21-27 confirms LogAsync signature intact. |
| T-376-06 | D/anomaly | mitigate | CLOSED | AssessmentAdminController.cs L3540: `_logger.LogWarning("FinalizeEssayGrading: shuffledIds kosong session {SessionId} — fallback derive question-set dari PackageUserResponses (D-06).", sessionId)` — LogWarning at shuffledIds fallback. L3556-3557: `if (agg.MaxScore == 0) _logger.LogWarning("FinalizeEssayGrading: maxScore=0 session {SessionId} — anomali data, Score fallback 0 (D-05).", sessionId)` — LogWarning at maxScore=0. Both warning points confirmed present. |
| T-376-07 | T (Tampering) | mitigate | CLOSED | AssessmentScoreAggregator.Compute uses same D-04 formula for IsPassed (Helpers/AssessmentScoreAggregator.cs L59: `percentage >= passPercentage`). FinalizeEssayGrading L3641-3648 Proton hook block preserved verbatim (guarded by `session.Category == "Assessment Proton" && isPassed && session.ProtonTrackId.HasValue`). 376-02-SUMMARY invariant: "Proton hook (358) EnsureAsync=2/NotifyIfGroupCompleted=2 preserved". Unit test EssayOnly_Graded80_Returns80AndPassed locks IsPassed=true output. |
| T-376-08 | S/T (CSRF) | mitigate | CLOSED | AssessmentAdminController.cs L3898: `[ValidateAntiForgeryToken]` on RecomputeEssayScores. Confirmed present three lines above action declaration at L3899. |
| T-376-09 | E (Elevation) | mitigate | CLOSED | AssessmentAdminController.cs L3897: `[Authorize(Roles = "Admin")]` — Admin-only (stricter than "Admin, HC" used on FinalizeEssayGrading). Confirmed at L3897 with inline comment "mass-repair → Admin-only (lebih ketat dari 'Admin, HC', BulkBackfill precedent)". |
| T-376-10 | T (Mass-state corruption) | mitigate | CLOSED | RecomputeEssayScores L3951: reuses `AssessmentScoreAggregator.Compute` (tested helper, no-drift). L3959-3963: `ExecuteUpdateAsync` WHERE guard `(s.Score == null || s.Score == 0)`. L3952-3955: skip if `agg.MaxScore == 0`. Counters repaired/skipped/alreadyOk tracked. 376-03-SUMMARY: "Compute call-sites = 2 (kill-drift D-02 verified)". Integration test Recompute_Idempotent_OnlyTouchesScoreZero verifies guard. |
| T-376-11 | T (Blast radius) | mitigate | CLOSED | grep of forbidden patterns within RecomputeEssayScores action body (L3899-3990): CertNumberHelper=0, _protonCompletionService=0, _protonBypassService=0, NotifyIfGroupCompleted=0, TrainingRecords.Add=0, SetProperty(r => r.Status=0. All occurrences of these identifiers in controller are in FinalizeEssayGrading or unrelated actions outside this body. 376-03-SUMMARY D-03 table: all 6 forbidden items count=0. Integration test Recompute_NoSideEffects_NoCertNoProtonNoTR proves NomorSertifikat=null, TrainingRecord count=0, Status=Completed after recompute. |
| T-376-12 | R (Repudiation) | mitigate | CLOSED | AssessmentAdminController.cs L3967-3978: `_auditLog.LogAsync(actor?.Id ?? "", actorName, "RecomputeEssayScores", $"Recompute essay-only: {repaired} diperbaiki, {skipped} dilewati, {alreadyOk} sudah benar", 0, "AssessmentSession")` — batch audit with counters, warn-only (failure caught at L3975-3977). AuditLogService signature at L21-27 matches call. |
| T-376-13 | I (Info Disclosure) | mitigate | CLOSED | AssessmentAdminController.cs L3982-3987: outer catch block logs `_logger.LogError(ex, "RecomputeEssayScores gagal.")` (detail to log) and sets `TempData["Error"] = "Recompute skor essay gagal. Cek log untuk detail."` (generic Bahasa Indonesia, no internal detail exposed to user). Pattern matches declared mitigation. |
| T-376-14 | T (Idempotency) | mitigate | CLOSED | AssessmentAdminController.cs L3959-3963: WHERE-guard `(s.Score == null || s.Score == 0)` on ExecuteUpdateAsync ensures 2nd run with already-repaired rows (Score=80) does not match predicate. Integration test Recompute_Idempotent_OnlyTouchesScoreZero L187-193: `repaired2 == 0` on second run asserted and GREEN (3/3 integration tests passed per 376-03-SUMMARY). |

---

## Unregistered Flags

None. No `## Threat Flags` section found in any SUMMARY.md for this phase. 376-01-SUMMARY, 376-02-SUMMARY, and 376-03-SUMMARY report no new attack surface beyond the declared threat register.

---

## Accepted Risks Log

| Threat ID | Rationale |
|-----------|-----------|
| T-376-04 | Auth attributes on FinalizeEssayGrading verified intact and not touched by this phase. Pre-existing secure posture accepted as-is. |
| T-376-05 | Audit log on FinalizeEssayGrading verified intact and not touched by this phase. Pre-existing secure posture accepted as-is. |
