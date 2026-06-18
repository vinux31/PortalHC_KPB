---
phase: 397-link-pre-post-ke-room-existing
plan: 02
subsystem: assessment
tags: [pre-post-link, service, inject, ef-core, audit, brownfield, INJ-12]

# Dependency graph
requires:
  - phase: 393-backend-core-inject
    provides: "InjectAssessmentService.InjectBatchAsync (atomic tx + GradingService delegation), InjectRequest DTO, in-tx _context.AuditLogs.Add pattern, InjectAssessmentFixture (disposable real-SQL)"
  - phase: 397-01
    provides: "5 RED integration suites (InjectLink/AntiDoubleLink/PreviewPairing/CrossGrouping/UnlinkInject) + DTO/VM contract (InjectRequest.LinkTargetRepId, InjectPairingPreview, VM.LinkedTargetRepId); pinned symbols UnlinkInjectGroupAsync + PreviewPairingAsync"
provides:
  - "Per-worker bidirectional LinkedSessionId resolution by-UserId inside InjectBatchAsync (replaces broadcast :120, D-02)"
  - "ResolveLinkContextAsync (shared, server-authoritative Kasus A adopt / Kasus B sticker=RepresentativeId) — single source of truth for batch + preflight + preview (no drift)"
  - "Kasus B write-to-online: LinkedGroupId sticker to ALL target-room sessions (Title+Category+Schedule.Date) + audit LinkPrePost per mutated online session (D-01/D-09)"
  - "Anti-double-link preflight (D-08, full collect-all list) in PreflightValidateAsync"
  - "PreviewPairingAsync (D-07 dry-run, NO write): Paired/Unpaired/WillTouchOnline/DateWarn/DoubleLinkErrors"
  - "UnlinkInjectGroupAsync (D-12 atomic revert + single-type Kasus B sticker revert + audit LinkPrePostUndo)"
affects: [397-03 (Wave 2 controller/preview wiring consumes req.LinkTargetRepId + PreviewPairingAsync + UnlinkInjectGroupAsync), 397-04 (Wave 3 view/modal/chip), 398 (verification)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Server-authoritative link resolution: re-resolve rep + opposite-type from req.LinkTargetRepId (never trust client LinkedGroupId — T-397-06)"
    - "Shared read-only resolution helper (ResolveLinkContextAsync) consumed by commit + preflight + preview → guarantees preview==commit Kasus A/B (no drift)"
    - "mutatedOnlineSessionIds HashSet gated on !IsManualEntry → audit LinkPrePost only for online sessions (inject↔inject = 0 audit, D-10); dedup paired-sticker double-count"

key-files:
  modified:
    - "Services/InjectAssessmentService.cs"

key-decisions:
  - "ResolveLinkContextAsync resolution rule: Kasus A (rep.LinkedGroupId != null) → groupId = rep.LinkedGroupId, kasusB=false, no online sticker; Kasus B (standalone) → groupId = rep.Id (RepresentativeId, konvensi :1270), kasusB=true, collect ALL target-room sessions"
  - "Kasus B target-room grouping key LOCKED = Title + Category + Schedule.Date (must match Plan 03-T1 picker standalone projection exactly)"
  - "Audit ActionType strings: 'LinkPrePost' (11 char) per mutated online session; 'LinkPrePostUndo' (15 char) per session reverted on unlink — both ≤ MaxLength(50)"
  - "Unlink Kasus B sticker revert via single-type heuristic (Open Q 1 opt-b): revert online LinkedGroupId only when group becomes single-type after inject removed"
  - "Audit via in-tx _context.AuditLogs.Add (NOT AuditLogService.LogAsync — partial-commit risk, Pitfall 3); all link/unlink writes in one transaction"
  - "NEVER assign online .Score/.Status/.IsPassed/responses in link or unlink code (T-397-04) — only LinkedGroupId/LinkedSessionId columns"

patterns-established:
  - "Per-worker link wiring AFTER session SaveChanges (session.Id exists), resolve sibling by-UserId, bidirectional write-back; LinkedGroupId ALWAYS set when linking (D-03 unpaired = LinkedSessionId null)"
  - "Anti-double-link contributes to existing reject-all path (errors list in PreflightValidateAsync) — no new reject mechanism"

requirements-completed: []  # INJ-12 spans Plans 01-04; final close deferred to verification phase (398), consistent with 395/396 convention

# Metrics
duration: 22min
completed: 2026-06-18
---

# Phase 397 Plan 02: Link Pre/Post Service Wiring (Wave 1 GREEN) Summary

**Per-worker bidirectional Pre/Post linking inside InjectBatchAsync (replaces the broadcast at :120), server-authoritative Kasus A adopt / Kasus B sticker-to-all-online + audit LinkPrePost, anti-double-link preflight (D-08), the PreviewPairingAsync dry-run (D-07), and UnlinkInjectGroupAsync atomic revert (D-12) — turning all 5 Wave-0 397 RED integration suites GREEN (15/15) with the fast suite intact (389/389).**

## Performance

- **Duration:** ~22 min
- **Started:** 2026-06-18T10:18Z (approx)
- **Completed:** 2026-06-18T10:40Z
- **Tasks:** 3
- **Files modified:** 1 (Services/InjectAssessmentService.cs, +343/-2)

## Accomplishments
- **Task 1 — per-worker bidirectional + Kasus A/B + write-to-online:** Removed the broadcast (`LinkedGroupId/LinkedSessionId = req.*` at the session initializer); LinkedGroupId/LinkedSessionId now wired per-worker AFTER each session SaveChanges by resolving the sibling **by-UserId**. Kasus A adopts `rep.LinkedGroupId` without touching online data; Kasus B writes the sticker `LinkedGroupId = rep.Id` to **ALL** target-room standalone sessions (Pitfall 2) + bidirectional online write-back, with audit `LinkPrePost` per mutated online session (gated `!IsManualEntry` → inject↔inject = 0 audit, D-10). Online Score/Status/responses untouched. All inside the existing transaction → atomic rollback.
- **Task 2 — anti-double-link (D-08) + PreviewPairingAsync (D-07):** Factored the resolution into the read-only `ResolveLinkContextAsync` helper (single source of truth for commit + preflight + preview). Anti-double-link appended to `PreflightValidateAsync` collect-all (full list, no early-return, message contains NIP). `PreviewPairingAsync(int?,string,IReadOnlyList<string>,DateTime)` returns the pairing summary as a pure dry-run (NO SaveChanges): Paired/Unpaired, WillTouchOnline (Kasus B), DateWarn (skip when sibling CompletedAt null — Open Q 2), DoubleLinkErrors.
- **Task 3 — UnlinkInjectGroupAsync (D-12):** Atomic revert — null inject LinkedGroupId/LinkedSessionId, revert sibling bidirectional, revert Kasus B sticker via single-type heuristic (Open Q 1 opt-b), audit `LinkPrePostUndo` per mutated session; IDOR guard loads only `IsManualEntry` sessions; bogus group → `Success=false` + state intact. Online Score/Status never touched.

## Task Commits

Each task committed atomically:

1. **Task 1: Per-worker bidirectional linking + Kasus A/B + write-to-online** — `af28e9db` (feat)
2. **Task 2: Anti-double-link preflight + PreviewPairingAsync dry-run** — `a5c3b050` (feat)
3. **Task 3: UnlinkInjectGroupAsync atomic revert + audit** — `e474dda5` (feat)

_TDD note: This is the Wave-1 GREEN gate. The RED tests were authored in 397-01; this plan implements the pinned symbols (UnlinkInjectGroupAsync, PreviewPairingAsync, per-worker link resolution via req.LinkTargetRepId) and turns the 5 suites green. No new RED commits in this plan (test files unchanged)._

## Files Created/Modified
- `Services/InjectAssessmentService.cs` (+343/-2):
  - **InjectBatchAsync**: link-context resolution block (drives off `req.LinkTargetRepId`, calls `ResolveLinkContextAsync`, builds `siblingByUserId`); session initializer sets `LinkedGroupId/LinkedSessionId = null` (broadcast removed); per-worker link wiring after SaveChanges; Kasus B write-to-all-online block; `LinkPrePost` audit loop gated on `mutatedOnlineSessionIds`.
  - **PreflightValidateAsync**: anti-double-link section (D-08) feeding the existing reject-all path.
  - **ResolveLinkContextAsync** (NEW private): server-authoritative Kasus A/B resolution + Kasus B target-room collection (key Title+Category+Schedule.Date).
  - **PreviewPairingAsync** (NEW public): D-07 dry-run pairing summary, NO write.
  - **UnlinkInjectGroupAsync** (NEW public): D-12 atomic revert + `LinkPrePostUndo` audit.

## Decisions Made
See key-decisions frontmatter. Notable runtime-verified points:
- **ResolveLinkContextAsync** is the single Kasus A/B authority shared by InjectBatchAsync, the anti-double preflight, and PreviewPairingAsync → preview==commit grouping behavior guaranteed by construction.
- **Kasus B write-to-all key = Title + Category + Schedule.Date** (locked; Plan 03-T1 picker standalone projection must match exactly).
- **Audit ActionType strings**: `LinkPrePost` / `LinkPrePostUndo` (both ≤ MaxLength 50).
- **Unlink Kasus-B-revert heuristic = single-type** (Open Q 1 opt-b) — revert online sticker only when the group becomes single-type after the inject sessions are removed.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added ResolveLinkContextAsync helper during Task 1 (plan placed it in Task 2)**
- **Found during:** Task 1 (per-worker linking + Kasus A/B resolution)
- **Issue:** The plan authored Task 1's inline resolution to call `ResolveLinkContextAsync`, but the helper itself was specified as a Task 2 deliverable. Building Task 1 in isolation (calling a not-yet-existing method) would not compile.
- **Fix:** Added `ResolveLinkContextAsync` as part of Task 1 so the inline resolution compiles and is already the shared single-source-of-truth. Task 2 then reuses it (anti-double preflight + PreviewPairingAsync) with no re-implementation — the plan's "refactor Task 1's inline resolution to call this helper" became a no-op because it already called the helper.
- **Files modified:** Services/InjectAssessmentService.cs
- **Verification:** `dotnet build` green after Task 1; `grep -c ResolveLinkContextAsync` = 5 (used by batch + preflight + preview).
- **Committed in:** `af28e9db` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking — helper-ordering only).
**Impact on plan:** No scope change. The helper is exactly as the plan specified; only its first-appearance commit moved from Task 2 to Task 1 to satisfy compile order. No drift in Kasus A/B logic.

## Issues Encountered
- A grep false-positive: `grep -cE "\.Score =|\.Status ="` returned 1, which is the pre-existing essay-finalize WHERE clause `s.Status == AssessmentConstants.AssessmentStatus.PendingGrading` (a `==` comparison, not an assignment). Confirmed via line inspection — no online Score/Status assignment exists in the new link/unlink code (T-397-04 satisfied).

## Known Stubs
None — pure service logic; no UI/data-source stubs introduced.

## Threat Flags
None — no new network endpoint, auth path, or schema surface introduced. Service-layer link/unlink only; controller RBAC + CSRF arrive in Wave 2 (397-03).

## Verification Results
- `dotnet build HcPortal.csproj` → **Build succeeded**, 0 errors.
- `dotnet build HcPortal.Tests` → **Build succeeded** (RED symbols `UnlinkInjectGroupAsync` + `PreviewPairingAsync` now resolved).
- 5 Wave-0 397 integration suites (`--filter "FullyQualifiedName~InjectLink|...~AntiDoubleLink|...~PreviewPairing|...~CrossGrouping|...~UnlinkInject"`) → **Passed! 15/15** (real SQLEXPRESS, disposable HcPortalDB_Test_{guid}; HcPortalDB_Dev untouched).
- Fast suite (`--filter "Category!=Integration"`) → **Passed! 389/389** (no regression to 395/396).
- **0 migration**: `git diff --name-only` across all 3 task commits shows only `Services/InjectAssessmentService.cs`; no `Migrations/` or `Data/` edits.

## Wave 0 Tests Now Green
- `InjectLinkPrePostTests` (4): PerWorker_NoBroadcast, KasusA_Adopt_OnlineUnchanged, KasusB_WriteSticker_AllTargetSessions_AuditPerMutated, AtomicRollback.
- `InjectAntiDoubleLinkTests` (1): SameTypeSibling_RejectFullList_AllOffendingWorkers.
- `InjectPreviewPairingTests` (4): PairingPreview_MatchesCommit_NoWriteDuringPreview, KasusB_WillTouchOnline, DateWarn_OnlyWhenBothNonNullAndPreNewer, DoubleLink_ListedInDoubleLinkErrors.
- `InjectCrossGroupingTests` (3, KRITIS §13): CrossLink_GainScore_Intact_InjectPre_OnlinePost, CrossLink_Both_Inject_PairingIntact_NoOnlineTouched, CrossLink_PreviewPairing_InjectInject_ReportsPaired.
- `UnlinkInjectGroupTests` (3): Unlink_RevertBidirectional, Unlink_KasusB_RevertSticker_WhenGroupBecomesSingleType, Unlink_Atomic_InvalidGroupLeavesStateIntact.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- **Wave 2 (397-03) seam consumed:** `req.LinkTargetRepId` (server re-resolves authoritatively in InjectBatchAsync); `PreviewPairingAsync(int? linkTargetRepId, string injectAssessmentType, IReadOnlyList<string> injectUserIds, DateTime injectCompletedAt)`; `UnlinkInjectGroupAsync(int injectGroupId, string actorUserId, string actorName)`. Wave 2 wires `MapToRequest` to populate `LinkTargetRepId` from the VM hidden field, extends `PreviewInjectScore` to call `PreviewPairingAsync`, and adds `SearchLinkTargets` (JSON picker) + `UnlinkInjectGroup` (POST, RBAC + CSRF) controller actions.
- **0 migration** maintained throughout. Branch main; notify IT at push with migration=FALSE (CLAUDE.md Develop Workflow).
- No blockers. Integration tests use real SQLEXPRESS (reachable; no env fallback needed this run).

## Self-Check: PASSED

- SUMMARY.md verified on disk (FOUND `.planning/phases/397-link-pre-post-ke-room-existing/397-02-SUMMARY.md`)
- `Services/InjectAssessmentService.cs` verified on disk (FOUND)
- All 3 task commits verified in git log (`af28e9db`, `a5c3b050`, `e474dda5`)
- Build green; 5 Wave-0 397 integration suites 15/15 green; fast suite 389/389 green; 0 migration

---
*Phase: 397-link-pre-post-ke-room-existing*
*Completed: 2026-06-18*
