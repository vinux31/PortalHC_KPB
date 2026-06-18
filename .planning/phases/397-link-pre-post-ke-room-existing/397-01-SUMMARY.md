---
phase: 397-link-pre-post-ke-room-existing
plan: 01
subsystem: testing
tags: [tdd, xunit, integration, pre-post-link, assessment, inject, brownfield]

# Dependency graph
requires:
  - phase: 393-backend-core-inject
    provides: "InjectAssessmentService.InjectBatchAsync (atomic tx + GradingService delegation), InjectRequest DTO, InjectAssessmentFixture (disposable real-SQL)"
  - phase: 395-mode-jawaban-input-asli-auto-generate
    provides: "InjectPreviewResult/PreviewInjectScore preview==commit engine (AssessmentScoreAggregator), MapToRequest server-authoritative"
  - phase: 396-import-excel-retire-bulkbackfill
    provides: "Step5Method VM precedent, daftar-error-lengkap pattern (D-09), EssayTextRequired flag"
provides:
  - "5 RED integration test files locking INJ-12 contract: per-worker bidirectional linking (anti-broadcast), Kasus A adopt + online untouched, Kasus B write-to-ALL-target + audit LinkPrePost, atomic rollback, anti-double full-list (D-08), preview==commit pairing (D-07), cross inject↔online grouping intact (spec §13 KRITIS), inject↔inject no online touch (D-10), unlink revert (D-12)"
  - "InjectRequest.LinkTargetRepId (server-resolved link hint, Tampering guard T-397-01)"
  - "InjectPairingPreview result type (HasLink/Paired/Unpaired/WillTouchOnline/DateWarn/DoubleLinkErrors — D-07/D-08/D-11)"
  - "ViewModel.LinkedTargetRepId hidden link field (VM-only layer)"
  - "Pinned contract symbols Wave 1 must provide: UnlinkInjectGroupAsync, PreviewPairingAsync, per-worker link resolution via req.LinkTargetRepId"
affects: [397-02 (Wave 1 service implementation), 397-03 (Wave 2 controller/preview wiring), 397-04 (Wave 3 view/modal/chip + Playwright), 398 (verification)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "TDD RED interface-first: DTO/VM declares contract shape (build GREEN) while test assembly stays clean RED (missing-symbol only)"
    - "Cross-grouping invariant asserted by replicating GetGainScoreData EF query (LinkedGroupId + UserId, Status=Completed, Score.HasValue) — not calling the controller action (no HttpContext)"

key-files:
  created:
    - "HcPortal.Tests/InjectLinkPrePostTests.cs"
    - "HcPortal.Tests/InjectAntiDoubleLinkTests.cs"
    - "HcPortal.Tests/UnlinkInjectGroupTests.cs"
    - "HcPortal.Tests/InjectPreviewPairingTests.cs"
    - "HcPortal.Tests/InjectCrossGroupingTests.cs"
  modified:
    - "Models/InjectAssessmentDtos.cs"
    - "ViewModels/InjectAssessmentViewModel.cs"

key-decisions:
  - "InjectRequest carries LinkTargetRepId (hint only) — NO client LinkCaseB flag; server re-derives Kasus A/B from target room's LinkedGroupId (Tampering guard T-397-01)"
  - "InjectPairingPreview is a SEPARATE result type from score-only InjectPreviewResult so 395/396 preview==commit stays untouched"
  - "RED is behavioral for 4 files (compile but FAIL at runtime in Wave 1) + missing-symbol for files referencing UnlinkInjectGroupAsync/PreviewPairingAsync; LinkTargetRepId already in DTO (Task 1) so InjectBatchAsync-only tests compile"
  - "Cross-grouping (spec §13 KRITIS) asserted via GetGainScoreData-equivalent EF query, not the controller action (action returns Json, needs HttpContext)"
  - "Unlink Kasus B sticker revert uses single-type heuristic (Open Q 1 opt-b) — test pins revert-when-group-becomes-single-type"
  - "DateWarn skips when sibling CompletedAt null (Open Q 2) — pinned in PairingPreview_DateWarn"

patterns-established:
  - "Pin Wave-1 service contract via direct RED test calls: UnlinkInjectGroupAsync(int,string,string) returns InjectResult; PreviewPairingAsync(int?,string,IReadOnlyList<string>,DateTime) returns InjectPairingPreview"
  - "Per-worker link resolution driven by req.LinkTargetRepId (InjectBatchAsync signature unchanged) — anti-broadcast asserted via Assert.NotEqual on two workers' LinkedSessionId"

requirements-completed: []  # INJ-12 spans Plans 01-04; final close deferred to verification phase (398), consistent with 395/396 convention

# Metrics
duration: 18min
completed: 2026-06-18
---

# Phase 397 Plan 01: Link Pre/Post TDD Lock (Wave 0) Summary

**5 failing xUnit integration test files lock the INJ-12 per-worker bidirectional linking contract (anti-broadcast), Kasus A adopt / Kasus B write-to-online + atomic rollback, anti-double full-list (D-08), preview==commit pairing (D-07), cross inject↔online grouping intact (spec §13 KRITIS) + inject↔inject no-online-touch (D-10), and unlink revert (D-12) — plus the DTO/VM contract fields (LinkTargetRepId, InjectPairingPreview, LinkedTargetRepId) Waves 1-3 implement against.**

## Performance

- **Duration:** ~18 min
- **Started:** 2026-06-18T10:05Z (approx)
- **Completed:** 2026-06-18T10:23Z
- **Tasks:** 3
- **Files modified:** 7 (2 modified, 5 created)

## Accomplishments
- **Task 1 (interface-first, build GREEN):** Extended `InjectRequest` with `LinkTargetRepId` (server-resolved link hint, NO client LinkCaseB), added `InjectPairingPreview` result type (6 D-07/D-08/D-11 fields), added `ViewModel.LinkedTargetRepId`. `dotnet build HcPortal.csproj` → "Build succeeded", 0 errors.
- **Task 2 (RED):** 3 integration test files locking per-worker linking (anti-broadcast `Assert.NotEqual` on two LinkedSessionIds), Kasus A adopt (online Score/IsPassed/Status/responses UNCHANGED), Kasus B write-to-ALL-target-sessions (Pitfall 2) + 2 LinkPrePost audits, atomic rollback (0 inject sessions + 0 online mutation), anti-double full-list (≥2 offending in PerRowErrors, no early-return), unlink revert bidirectional + Kasus B sticker single-type heuristic + atomic.
- **Task 3 (RED):** 2 integration test files locking preview==commit pairing (dry-run, session+audit count before==after, Paired/Unpaired==P/M-P matches commit), and the KRITIS spec §13 cross-grouping test (inject Pre ↔ online Post pair surfaces via GetGainScoreData-equivalent query; inject↔inject pairing intact + 0 LinkPrePost audit per D-10).

## Task Commits

1. **Task 1: Extend DTO + ViewModel** — `4f330a82` (feat)
2. **Task 2: RED linking + Kasus A/B + atomic + anti-double + unlink** — `2a6afbfe` (test)
3. **Task 3: RED preview==commit pairing + cross-grouping (KRITIS §13)** — `9417527f` (test)

_TDD note: This is a Wave-0 RED-only plan (no GREEN/REFACTOR in this plan). GREEN is the Wave 1 (397-02) gate — restoring the fast suite + integration tests to green by implementing UnlinkInjectGroupAsync + PreviewPairingAsync + per-worker link resolution._

## Files Created/Modified
- `Models/InjectAssessmentDtos.cs` — added `InjectRequest.LinkTargetRepId` (link hint, server re-resolves; no LinkCaseB), new `InjectPairingPreview` class (HasLink/Paired/Unpaired/WillTouchOnline/DateWarn/DoubleLinkErrors)
- `ViewModels/InjectAssessmentViewModel.cs` — added `LinkedTargetRepId` hidden link field (VM-only, Step5Method precedent)
- `HcPortal.Tests/InjectLinkPrePostTests.cs` — RED: per-worker anti-broadcast, Kasus A adopt/untouched, Kasus B write-to-ALL + audit, atomic rollback
- `HcPortal.Tests/InjectAntiDoubleLinkTests.cs` — RED: D-08 same-type sibling reject FULL list (Bahasa Indonesia NIP)
- `HcPortal.Tests/UnlinkInjectGroupTests.cs` — RED: D-12 revert bidirectional + Kasus B single-type heuristic + atomic; pins `UnlinkInjectGroupAsync`
- `HcPortal.Tests/InjectPreviewPairingTests.cs` — RED: D-07 dry-run NO write, preview==commit pairing, WillTouchOnline, DateWarn skip-when-null, DoubleLinkErrors; pins `PreviewPairingAsync`
- `HcPortal.Tests/InjectCrossGroupingTests.cs` — RED: spec §13 KRITIS cross inject↔online via GetGainScoreData-equivalent; inject↔inject 0 LinkPrePost audit (D-10)

## Symbols Pinned (Wave 1 must provide)
- `InjectAssessmentService.UnlinkInjectGroupAsync(int injectGroupId, string actorUserId, string actorName)` → `Task<InjectResult>` (NEW; genuinely-missing symbol driving the clean RED)
- `InjectAssessmentService.PreviewPairingAsync(int? linkTargetRepId, string injectAssessmentType, IReadOnlyList<string> injectUserIds, DateTime injectCompletedAt)` → `Task<InjectPairingPreview>` (NEW; recommended seam from plan)
- Per-worker link resolution inside `InjectBatchAsync` driven by `req.LinkTargetRepId` (signature unchanged): resolve `LinkedSessionId` by-UserId, Kasus A adopt / Kasus B write-to-all-target + bidirectional write-back + audit `"LinkPrePost"`; anti-double-link preflight; rollback on error
- Audit ActionType `"LinkPrePost"` (per online session mutated, Kasus B) + `"LinkPrePostUndo"` (per session reverted on unlink)

## Decisions Made
See key-decisions frontmatter. Notable: NO client LinkCaseB flag (server re-derives Kasus A/B — T-397-01); `InjectPairingPreview` separate from score-only `InjectPreviewResult`; cross-grouping asserted via GetGainScoreData-equivalent EF query (not the controller action which needs HttpContext); unlink Kasus B sticker revert uses single-type heuristic (Open Q 1 opt-b); DateWarn skips when sibling CompletedAt null (Open Q 2).

## Deviations from Plan
None - plan executed exactly as written.

The plan anticipated the test assembly would NOT compile after this wave (clean RED, missing-symbol only inside the 5 new files). This held exactly: build reports only CS1061/missing-method errors for `UnlinkInjectGroupAsync` and `PreviewPairingAsync` (2 unique symbols), all inside the 5 new test files. `HcPortal.csproj` (DTO/VM) builds GREEN. The 4 files that reference only `req.LinkTargetRepId` (now present in the DTO from Task 1) compile and will FAIL at runtime in Wave 1 (correct behavioral RED); the files referencing the genuinely-missing service methods are the compile-RED.

## Issues Encountered
- The `HcPortalDB_Dev` grep guard returns 1 match per test file, but all matches are in COMMENTS asserting "HcPortalDB_Dev TAK tersentuh" (T-397-02 mitigation) — no connection string references the Dev DB. Verified `grep "Database=HcPortalDB_Dev\|Server=...HcPortalDB_Dev"` returns 0. All 5 files use the disposable `InjectAssessmentFixture` (`HcPortalDB_Test_{guid}`). DB safety satisfied.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- **Wave 1 (397-02) gate:** Implement `UnlinkInjectGroupAsync` + `PreviewPairingAsync` + per-worker link resolution in `InjectAssessmentService.InjectBatchAsync` (replace broadcast `:120`), making the test assembly compile and the 5 integration test files + fast suite turn GREEN.
- **0 migration** maintained (link columns `LinkedGroupId`/`LinkedSessionId` already exist; this plan adds only DTO/VM fields + tests).
- **Verification commands for Wave 1:** `dotnet test --filter "FullyQualifiedName~InjectLink"` / `~AntiDoubleLink` / `~PreviewPairing` / `~CrossGrouping` / `~UnlinkInject` (real SQLEXPRESS); full `dotnet test`; then Playwright (397-04).
- No blockers. Branch main; notify IT at push with migration=FALSE (CLAUDE.md Develop Workflow).

## Self-Check: PASSED

- All 5 created test files + SUMMARY.md verified on disk (FOUND)
- All 3 task commits verified in git log (`4f330a82`, `2a6afbfe`, `9417527f`)
- `HcPortal.csproj` build GREEN (0 errors); test assembly clean RED (2 unique missing symbols inside 5 new files); no `HcPortalDB_Dev` connection string; all 5 files `[Trait("Category","Integration")]`

---
*Phase: 397-link-pre-post-ke-room-existing*
*Completed: 2026-06-18*
