---
phase: 405
slug: backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint
status: verified
threats_total: 17
threats_open: 0
asvs_level: 1
created: 2026-06-21
---

# Phase 405 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> ASVS Level 1. Block-on: high (no high threats open).

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| EF model → DB schema | Migration apply changes local DB schema; wrong default/FK = data integrity risk | Schema defaults (AllowRetake=false, MaxAttempts=2, RetakeCooldownHours=24) |
| AssessmentAttemptResponseArchive → AssessmentAttemptHistory | FK cascade — delete parent must cascade to snapshot children (no orphan) | Per-question snapshot rows (worker answer text + frozen verdict) |
| Concurrent requests → ExecuteAsync | Double-click or parallel calls against the same sessionId | Race condition on session status field |
| Browser/form → UpdateRetakeSettings | Untrusted POST with assessmentId + numeric values | Config values (maxAttempts, cooldown hours), cross-session propagation |
| Browser/form → ResetAssessment | Untrusted POST with sessionId; triggers archive + delete | Exam responses, attempt history, worker score |
| HC actor → cross-assessment config | Sibling propagation writes to all sessions in group | Retake policy applied to all workers in a Title/Category/Schedule.Date group |
| Caller authz → RetakeService | Service does NOT authorize; caller must RBAC-guard (HC plan 405-04, ownership worker Phase 407) | sessionId, actorUserId |
| Legacy data → cap counting | Legacy AttemptHistory rows (no snapshot child) must not lock workers out of retake | D-01 snapshot-presence EXISTS discriminator |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-405-01 | Tampering | Snapshot table without cascade — orphan rows on parent delete | mitigate | `OnDelete(DeleteBehavior.Cascade)` in `ApplicationDbContext.cs:594`; migration `ReferentialAction.Cascade` at `Migrations/20260621065918_AddRetakeColumnsAndArchive.cs:57` | closed |
| T-405-02 | Information Disclosure | AnswerText stores worker answer — not the answer key | accept | `AssessmentAttemptResponseArchive.AnswerText` = worker's own answer + frozen verdict only. Answer-key not persisted. Display gating (showWrongFlagsOnly) deferred to Phase 407. See Accepted Risks Log AR-405-01. | closed |
| T-405-03 | Tampering | Migration default wrong (e.g. AllowRetake default true) → retake active without admin decision | mitigate | `defaultValue: false` for AllowRetake at migration `20260621065918_AddRetakeColumnsAndArchive.cs:19`; MaxAttempts `defaultValue: 2` at :26; RetakeCooldownHours `defaultValue: 24` at :33 (hand-fixed from EF-generated 0 per 405-01-SUMMARY deviation log) | closed |
| T-405-04 | Denial of Service | Snapshot-presence discriminator D-01 — legacy archive (no snapshot child) must not lock workers | mitigate | Archive table born empty (verified via sqlcmd: 0 rows); `CanRetakeAsync` uses EXISTS subquery `AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id)` at `Services/RetakeService.cs:241` — legacy AttemptHistory rows with no child naturally excluded from cap counting | closed |
| T-405-05 | Tampering | Re-grade inline in builder → verdict diverges from Results/PDF/Excel | mitigate | `RetakeArchiveBuilder.Build` calls `AssessmentScoreAggregator.IsQuestionCorrect` at `Helpers/RetakeArchiveBuilder.cs:34` (single source; no inline re-grade). Essay uses `TextAnswer` full-text; MC/MA uses `BuildAnswerCell`. Test `Build_EssayLongText_NotTruncated` asserts length==500 (no truncation). | closed |
| T-405-06 | Information Disclosure | Essay full-text in archive — does it leak the answer key? | accept | AnswerText = WORKER's own submitted answer (essay TextAnswer, not the correct-answer key). Answer key is not stored anywhere in the archive entity. Display gating = Phase 407 (showWrongFlagsOnly). See Accepted Risks Log AR-405-02. | closed |
| T-405-07 | Elevation of Privilege | CanRetake too permissive (e.g. misses IsPassed check) → passing or pending worker can retake | mitigate | Guard chain in `RetakeRules.CanRetake` at `Helpers/RetakeRules.cs:41-51`: `isPassed != false → false` (blocks null=PendingGrading and true=Lulus) + `status != "Completed" → false` (blocks InProgress/Abandoned/Cancelled/Open). Verified by unit tests `Blocked_WhenPassed`, `Blocked_WhenPendingGrading`, `Blocked_WhenNotCompleted` (16/16 passed). | closed |
| T-405-08 | Tampering | Double-click double-archive (two AttemptHistory rows + two snapshot sets) | mitigate | Claim-atomic first at `RetakeService.cs:101-102`: `ExecuteUpdateAsync WHERE Status NOT IN (Cancelled, Open)` + `rows==0 → abort` prevents second archive. WR-02 fix wraps entire mutation in `BeginTransactionAsync`+`CommitAsync` at `RetakeService.cs:95,194`. WR-01 fix: already-Open session returns success no-op (no second archive created). Integration test `Claim_DoubleExecute_NoSecondArchive` asserts `histCount==1`. | closed |
| T-405-09 | Denial of Service | Legacy archive (no snapshot) consumes cap → worker locked when AllowRetake ON | mitigate | D-01 snapshot-presence EXISTS subquery at `RetakeService.cs:237-242` — only AttemptHistory rows WITH child AssessmentAttemptResponseArchive are counted. Integration test `CanRetake_LegacyArchiveWithoutSnapshot_DoesNotConsumeCap` (5/5 passed). | closed |
| T-405-10 | Elevation of Privilege | Cap/cooldown bypass via client-controlled input (worker path) | mitigate (partial) | `CanRetakeAsync` at `RetakeService.cs:232-247` is server-authoritative: loads session from DB, computes era-retake count via EXISTS, delegates to `RetakeRules.CanRetake` with server-side values — no client input trusted for eligibility. Worker endpoint + ownership check (session.UserId == user.Id) is Phase 407 scope. Documented below. | closed |
| T-405-11 | Information Disclosure | Counting (UserId, Title) conflates Pre/Post → Pre attempt locks Post cap | mitigate | Counting triple key `(UserId, Title, Category)` at `RetakeService.cs:146-150` and `RetakeService.cs:238-241`. Integration test `Counting_PrePostSameTitle_NoConflate` verifies Pre archive count does not affect Post session cap (5/5 passed). | closed |
| T-405-12 | Repudiation | Reset without audit trail | mitigate | `AuditLogService.LogAsync(actorUserId, actorName, actionType, "... (reason={reason})", sessionId, "AssessmentSession")` at `RetakeService.cs:199-205`, wrapped in try/catch warn-only at `:207-209`. `actionType` is caller-supplied ("ResetAssessment" / "RetakeAssessment") differentiating HC vs worker. Audit+SignalR intentionally outside transaction commit (warn-only — failure must not roll back completed reset). | closed |
| T-405-13 | Tampering | CSRF on UpdateRetakeSettings / ResetAssessment | mitigate | `[ValidateAntiForgeryToken]` on `UpdateRetakeSettings` at `AssessmentAdminController.cs:5566` and `ResetAssessment` at `:4198` (existing, preserved by refactor). | closed |
| T-405-14 | Elevation of Privilege | Non-Admin/HC changes retake config or resets another user's session | mitigate | `[Authorize(Roles = "Admin, HC")]` on `UpdateRetakeSettings` at `AssessmentAdminController.cs:5565` and `ResetAssessment` at `:4197` (existing, preserved). | closed |
| T-405-15 | Tampering | MaxAttempts/cooldown outside range (e.g. 9999) via form bypass of [Range] | mitigate | `Math.Clamp(maxAttempts, 1, 5)` at `AssessmentAdminController.cs:5580`; `Math.Clamp(retakeCooldownHours, 0, 168)` at `:5581`. Server-side defense-in-depth above model validation. | closed |
| T-405-16 | Spoofing | Stale TempData token re-entry after reset (worker enters without token re-verification) | mitigate | `TempData.Remove($"TokenVerified_{id}")` at `AssessmentAdminController.cs:4270` after successful `ExecuteAsync`. Service intentionally does not touch TempData (HTTP-scoped); caller responsibility is documented in XML-doc of RetakeService. | closed |
| T-405-17 | Tampering | Retake config set for PreTest / ManualEntry (must not be retakeable) | mitigate | Guard `RetakeRules.ShouldHideRetakeToggle(assessment.AssessmentType, assessment.IsManualEntry)` at `AssessmentAdminController.cs:5573` — rejects and redirects for PreTest or manual-entry assessments before writing any config. `CanRetake` also blocks these paths at `RetakeRules.cs:42-43`. | closed |

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-405-01 | T-405-02 | `AssessmentAttemptResponseArchive.AnswerText` stores the worker's own submitted answer (essay full-text or MC/MA option text). This is the worker's own data, not a shared answer key. The answer key is never stored in the archive. Display gating (which flags to show: showWrongFlagsOnly) is a Phase 407 concern. The archive table is not exposed to any UI in Phase 405. | Architecture decision (405-02-SUMMARY) | 2026-06-21 |
| AR-405-02 | T-405-06 | Essay TextAnswer stored full-text (per D-04 / ISO 17024 permanent-record requirement). This is still the worker's own essay response, not the grader's model answer. No answer key exists in the system for essay questions (subjective grading). Phase 407 will implement display gating. | Architecture decision (405-02-SUMMARY Pitfall 2) | 2026-06-21 |

---

## Unregistered Threat Flags

No new unregistered threat flags raised in any 405-0x-SUMMARY.md `## Threat Flags` section. All four SUMMARYs confirmed "Tidak ada surface keamanan baru di luar `<threat_model>` plan."

---

## Out-of-Scope (Deferred to Downstream Phases)

| Item | Deferred To | Notes |
|------|-------------|-------|
| Worker `RetakeExam` endpoint ownership check (session.UserId == user.Id) | Phase 407 | T-405-10 partial mitigation — server-authoritative CanRetakeAsync already implemented; caller-level ownership guard is 407 scope |
| Card UI for retake config (ManagePackages) | Phase 406 | ViewBag state (AllowRetake/MaxAttempts/RetakeCooldownHours/HideRetakeToggle/RetakeMaxAttemptsUsedInGroup) is set; UI rendering is 406 scope |
| Worker self-service RetakeExam UI flow | Phase 407 | CanRetakeAsync + ExecuteAsync(actionType=RetakeAssessment) available; UI wiring is 407 scope |

---

## Review-Fix Impact

Three warnings (WR-01, WR-02, WR-03) from code review (405-REVIEW-FIX.md) were fixed at commit `4e538ee4` and are security-relevant:

- **WR-02 (transaction atomicity):** The entire mutation sequence (claim → AttemptHistory insert → snapshot AddRange → response/assignment/ET-score deletes) is now wrapped in a single `BeginTransactionAsync`/`CommitAsync` at `RetakeService.cs:95,194`. A mid-operation failure rolls the entire unit back — no claimed (Open/Score=null) session with orphan AttemptHistory or surviving PackageUserResponses. This directly strengthens T-405-08 (anti double-archive) and prevents partial corruption.
- **WR-01 (Open session no-op):** Already-Open session returns `RetakeResult(true, null)` at `RetakeService.cs:89-90` — no second archive. Combined with the claim `WHERE Status NOT IN (Cancelled, Open)`, double-click race now produces one archive at most.
- **WR-03 (childless AttemptHistory prevention):** AttemptHistory is deferred-inserted only when `questions.Count > 0` at `RetakeService.cs:141`. Completed sessions with missing/empty assignment produce no orphan AttemptHistory row — D-01 cap counting is unaffected.

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-21 | 17 | 17 | 0 | gsd-secure-phase (Claude Sonnet 4.6) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log (AR-405-01, AR-405-02)
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-21
