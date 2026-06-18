---
phase: 397-link-pre-post-ke-room-existing
asvs_level: 1
threats_total: 17
threats_open: 0
threats_closed: 17
block_on: high
result: SECURED
reviewed: 2026-06-18
reviewer: gsd-security-auditor (claude-sonnet-4-6)
---

# 397-SECURITY.md — Phase 397: Link Pre/Post ke Room Existing

**Phase:** 397 — INJ-12 (Link Pre/Post inject sessions to existing room)
**Threats Closed:** 17/17
**ASVS Level:** 1
**Result:** SECURED

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-397-01 | Tampering | mitigate | CLOSED | `Models/InjectAssessmentDtos.cs:69` — `LinkTargetRepId` hint only; no `LinkCaseB` client field. Service re-resolves via `ResolveLinkContextAsync`. `InjectViewModelMapTests.Maps_LinkTargetRepId_from_chip` asserts `req.LinkedGroupId` stays null. |
| T-397-02 | Info Disclosure | mitigate | CLOSED | `HcPortal.Tests/InjectLinkPrePostTests.cs:32` — `IClassFixture<InjectAssessmentFixture>` (disposable `HcPortalDB_Test_{guid}`, `MigrateAsync` + `EnsureDeletedAsync`). `HcPortalDB_Dev` appears only in comments asserting it is untouched (0 connection-string references confirmed by grep). Same pattern in `InjectCrossGroupingTests.cs:16`. |
| T-397-03 | Tampering | mitigate | CLOSED | `HcPortal.Tests/InjectLinkPrePostTests.cs:179,221,271` — `KasusA_Adopt_OnlineScoreStatusUnchanged` + `KasusB_WriteSticker_AllTargetSessions_AuditPerMutated` assert online `Score`/`IsPassed`/`Status`/responses unchanged before vs after. RED test existed before service code, locking the invariant. |
| T-397-04 | Tampering | mitigate | CLOSED | `Services/InjectAssessmentService.cs` — grep for `.Score =`/`.Status =`/`.IsPassed =` returns only a pre-existing `WHERE` comparison (`.Status ==`, not assignment). No assignment to online score/status/responses in link or unlink code. Confirmed by 397-REVIEW.md line 38. |
| T-397-05 | Repudiation | mitigate | CLOSED | `Services/InjectAssessmentService.cs:366,800` — `ActionType = "LinkPrePost"` (11 char) per mutated online session in Kasus B; `ActionType = "LinkPrePostUndo"` (15 char) per reverted session on unlink. Both via in-tx `_context.AuditLogs.Add` (not `LogAsync`). Both ≤ `MaxLength(50)`. |
| T-397-06 | Tampering | mitigate | CLOSED | `Services/InjectAssessmentService.cs:617` — `ResolveLinkContextAsync(int? linkTargetRepId, string assessmentType)` re-resolves rep from DB, validates `s.AssessmentType == oppositeType`. Raw client `LinkedGroupId` never used. Consumed by `InjectBatchAsync`, anti-double preflight, and `PreviewPairingAsync` (single source of truth, no drift). |
| T-397-07 | Elevation/IDOR | mitigate | CLOSED | `Services/InjectAssessmentService.cs:744` — `UnlinkInjectGroupAsync` loads only `Where(s => s.LinkedGroupId == injectGroupId && s.IsManualEntry)`. Only inject (manual-entry) sessions loadable/revertable. Non-manual (online) sessions never subject to the unlink query. |
| T-397-08 | DoS/Tampering | mitigate | CLOSED | `Services/InjectAssessmentService.cs:88-411` — all writes (inject sessions + Kasus B sticker + bidirectional write-back + audit) inside single `BeginTransactionAsync`; single `SaveChangesAsync` at line 321 before `CommitAsync`. `_context.AuditLogs.Add` direct (not `LogAsync`). Confirmed by `AtomicRollback_NoInjectSession_NoOnlineLinkMutation` test. |
| T-397-09 | Spoofing/Elevation | mitigate | CLOSED | `Controllers/InjectAssessmentController.cs:176,244,263` — `[Authorize(Roles = "Admin, HC")]` on `SearchLinkTargets` (GET), `PreviewPairing` (POST), `UnlinkInjectGroup` (POST). All three new actions RBAC-guarded. |
| T-397-10 | Tampering | mitigate | CLOSED | `Controllers/InjectAssessmentController.cs:245,264` — `[ValidateAntiForgeryToken]` on `PreviewPairing` (POST) and `UnlinkInjectGroup` (POST). `SearchLinkTargets` is GET read-only — no antiforgery needed. View sends `RequestVerificationToken` header in fetch at lines 2246 and 2275. |
| T-397-11 | Tampering | mitigate | CLOSED | `Controllers/InjectAssessmentController.cs:180-182` — `injectType` whitelisted: `if (injectType != AssessmentConstants.AssessmentType.PreTest && injectType != AssessmentConstants.AssessmentType.PostTest) return Json(Array.Empty<object>())`. `term` filtered via EF LINQ `.Contains` (parameterized, no SQL concat). |
| T-397-12 | Elevation/IDOR | mitigate | CLOSED | Service IDOR guard: `Services/InjectAssessmentService.cs:744` loads only `IsManualEntry` sessions. Controller RBAC `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` at line 263-264. Raw client `LinkedGroupId` never written (T-397-13 covers `MapToRequest`). |
| T-397-13 | Tampering | mitigate | CLOSED | `Controllers/InjectAssessmentController.cs:473` — `MapToRequest` sets only `LinkTargetRepId = vm.LinkedTargetRepId`. No assignment of `LinkedGroupId`/`LinkedSessionId` from `vm.*`. Locked by `InjectViewModelMapTests.Maps_LinkTargetRepId_from_chip`. |
| T-397-14 | Tampering/XSS | mitigate | CLOSED | `Views/Admin/InjectAssessment.cshtml:2054,2055,2058,2122,2125,2134,2139,2144,2200,2201,2209,2228` — all room title/category, type badge, group badge, NIP/name, and double-link error messages rendered via `.textContent`. The single `innerHTML` usage at line 2105 is for static error markup with no server-data interpolation (confirmed as safe by 397-REVIEW.md IN-05). |
| T-397-15 | Tampering | mitigate | CLOSED | `Views/Admin/InjectAssessment.cshtml:2011,2246,2275` — `antiforgeryToken()` helper reads `__RequestVerificationToken` from DOM; `RequestVerificationToken` header sent on `PreviewPairing` fetch (line 2246) and `UnlinkInjectGroup` fetch (line 2275). Controller `[ValidateAntiForgeryToken]` on both actions. |
| T-397-16 | Elevation | mitigate | CLOSED | `Controllers/InjectAssessmentController.cs:263` — `[Authorize(Roles = "Admin, HC")]` on `UnlinkInjectGroup`. Unlink control (`#btnUnlinkRoom`) rendered only inside the post-commit success surface of the Admin/HC page (`Views/Admin/InjectAssessment.cshtml:57`). Server is authoritative; UI visibility is defense-in-depth only. |
| T-397-17 | Tampering | mitigate | CLOSED | `tests/e2e/inject-assessment-397.spec.ts:167,263-270,273-277` — Contract 8 (KRITIS §13) asserts: `onlineScoreAfter == onlineScoreBefore` (DB query), `audit "LinkPrePost"` present post-Kasus-B commit, online `Status` unchanged. UAT live (9/9) also confirmed Score=85 + Status=Completed unchanged on session 173 after Kasus B commit. |

## Unregistered Flags

No unregistered threat flags were raised in any of the four SUMMARY.md `## Threat Flags` sections. 397-02-SUMMARY.md explicitly states "Threat Flags: None — no new network endpoint, auth path, or schema surface introduced." 397-03-SUMMARY.md states "Threat Flags: None — no NEW security surface beyond the plan's `<threat_model>`."

## Accepted Risks Log

None — all 17 threats carry disposition `mitigate` and are CLOSED.

## Known Non-Security Deferred Issues

The following items from 397-REVIEW.md are confirmed NON-security (operational/UX only) and deferred to Phase 398 / backlog per the orchestrator's decision:

| ID | Severity | Description |
|----|----------|-------------|
| WR-01 | Warning | `UnlinkInjectGroupAsync` reverts ALL inject batches sharing a group id on Kasus A (broader scope than UI implies). No score/status leak — audit present. |
| WR-02 | Warning | `g.First()` in `siblingByUserId` non-deterministic when >1 opposite-type sibling per user; affects `LinkedSessionId` pointer fidelity only (gain-score pairs by `LinkedGroupId+UserId`, so grouping is correct). |
| WR-03 | Warning | `UnlinkInjectGroup` sets `TempData` on a JSON-only response; stale toast may appear on next navigation. No data integrity impact. |
| IN-01 | Info | `SearchLinkTargets` `Take(50)` without `OrderBy` — non-deterministic ordering in picker. |
| IN-02 | Info | Redundant `?? "Standard"` null-coalescing in `PreviewPairing` controller. |
| IN-03 | Info | Audit `ActionType` strings not centralized in constants — typo risk low (tests assert exact strings). |
| IN-04 | Info | Stale XML-doc class comment in `InjectAssessmentService.cs`. |
| IN-05 | Info | `innerHTML` used for static (non-interpolated) error markup in picker — safe per analysis, cosmetic inconsistency. |
