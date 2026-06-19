---
phase: 397-link-pre-post-ke-room-existing
plan: 03
subsystem: assessment
tags: [auth, assessment, pre-post-link, controller, json-picker, csrf, brownfield, INJ-12]

# Dependency graph
requires:
  - phase: 397-02
    provides: "Service seams — req.LinkTargetRepId (server re-resolves real link in InjectBatchAsync), PreviewPairingAsync(int? linkTargetRepId, string injectAssessmentType, IReadOnlyList<string> injectUserIds, DateTime injectCompletedAt)→InjectPairingPreview, UnlinkInjectGroupAsync(int injectGroupId, string actorUserId, string actorName)→InjectResult; Kasus B write-to-all grouping key = Title+Category+Schedule.Date"
  - phase: 396-03
    provides: "DownloadInjectTemplate + UploadInjectExcel controller actions (line anchors shifted; re-grepped at HEAD e99a2cf2)"
  - phase: 395
    provides: "PreviewInjectScore score-path + InjectPreviewResult (preview==commit) + MapToRequest server-authoritative + InjectAssessmentViewModel.LinkedTargetRepId hidden field"
provides:
  - "SearchLinkTargets JSON GET picker — opposite-type rooms (inject + online, NO IsManualEntry filter D-10), parameterized term search, injectType whitelist Pre/Post, returns Json NOT PartialView"
  - "MapToRequest populates req.LinkTargetRepId from vm.LinkedTargetRepId (server re-resolves; raw client LinkedGroupId/LinkedSessionId never set — T-397-13)"
  - "PreviewPairing POST — batch-level pairing dry-run via PreviewPairingAsync (D-07), returns Json, NO write; score path PreviewInjectScore untouched"
  - "UnlinkInjectGroup POST (RBAC + CSRF) — calls UnlinkInjectGroupAsync, TempData Bahasa Indonesia, returns Json({ ok, message }) NOT RedirectToAction (Wave 4 JS contract)"
  - "PreviewPairingRequest payload DTO (controller-local: LinkTargetRepId, AssessmentType, UserIds, CompletedAt)"
affects: [397-04 (Wave 3 view/modal/chip/preview/unlink JS wires against these HTTP contracts), 398 (verification)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "JSON picker action returns Json(...) (NOT PartialView) so the ~/Views/Admin/ View() override — which only affects ViewResult — does not intercept it (Pitfall 6)"
    - "Server-authoritative link: controller forwards only LinkTargetRepId hint; service re-resolves real LinkedGroupId/LinkedSessionId (Tampering guard T-397-13)"
    - "Picker standalone grouping key LOCKED = Title+Category+Schedule.Date, mirroring 397-02 Kasus B write-to-all key exactly (picker set == sticker set)"
    - "Fetch-driven POST endpoints return Json({ ok, message }) for in-place toast + UI reset (no RedirectToAction) — view JS contract"

key-files:
  created: []
  modified:
    - "Controllers/InjectAssessmentController.cs"
    - "HcPortal.Tests/InjectViewModelMapTests.cs"

key-decisions:
  - "SearchLinkTargets is GET read-only (no antiforgery); PreviewPairing + UnlinkInjectGroup are POST with [ValidateAntiForgeryToken] + [Authorize(Roles=\"Admin, HC\")]"
  - "PreviewPairing is a SEPARATE companion endpoint (not folded into per-worker PreviewInjectScore) — keeps 395/396 score preview==commit untouched (lower-risk choice per plan)"
  - "PreviewPairingRequest defined IN-FILE (controller namespace) to keep scope locked to Controllers/InjectAssessmentController.cs ONLY (0 edits to Models/InjectAssessmentDtos.cs)"
  - "injectType whitelisted to AssessmentConstants.AssessmentType.PreTest/PostTest (Standard → empty array, D-06); term filtered via EF LINQ Title/Category Contains (parameterized, no SQL concat)"
  - "UnlinkInjectGroup also sets TempData Success/Error (Bahasa Indonesia) in addition to JSON return — belt-and-suspenders notice; JSON shape { ok, message } is the load-bearing contract for Wave 4"

patterns-established:
  - "Picker JSON row shape exposed to Wave 3 modal (exact field names below)"
  - "Controller-local request DTO when the seam must not widen a shared Models file (scope-lock discipline)"

requirements-completed: []  # INJ-12 spans Plans 01-04; final close deferred to verification phase (398), consistent with 395/396 convention

# Metrics
duration: 12min
completed: 2026-06-18
---

# Phase 397 Plan 03: Link Pre/Post Controller Wiring (Wave 2) Summary

**Exposes the Wave-1 service linking to the HTTP surface in InjectAssessmentController: a `SearchLinkTargets` JSON picker (opposite-type rooms, inject + online, parameterized search, RBAC), `MapToRequest` populating `req.LinkTargetRepId` (server re-resolves the real link — never trusts raw client LinkedGroupId), a `PreviewPairing` batch-level dry-run (D-07), and a `UnlinkInjectGroup` POST (RBAC + CSRF, Bahasa Indonesia notice, `Json({ ok, message })`) — build green, fast suite 390/390, 397 integration 15/15, 0 migration.**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-06-18T18:43Z (approx)
- **Completed:** 2026-06-18T18:55Z
- **Tasks:** 2
- **Files modified:** 2 (Controllers/InjectAssessmentController.cs +147, HcPortal.Tests/InjectViewModelMapTests.cs included in count)

## Accomplishments
- **Task 1 — SearchLinkTargets JSON picker + MapToRequest link wiring (Surface 7):** Added `[HttpGet][Authorize(Roles="Admin, HC")] SearchLinkTargets(string? term, string injectType)` returning `Json(...)` (NOT PartialView — Pitfall 6). Filters opposite-type rooms (inject Pre → PostTest; inject Post → PreTest), NO IsManualEntry filter (D-10 — shows inject + online), injectType whitelisted to Pre/Post (Standard → empty array, D-06), term parameterized via EF LINQ. Grouped (Kasus A) by `LinkedGroupId`; standalone (Kasus B) by `Title+Category+Schedule.Date` (LOCKED — matches 397-02 write-to-all key). `MapToRequest` now sets `req.LinkTargetRepId = vm.LinkedTargetRepId` only — raw client `LinkedGroupId`/`LinkedSessionId` never assigned (T-397-13). New unit test `Maps_LinkTargetRepId_from_chip` (RED→GREEN).
- **Task 2 — PreviewPairing dry-run (D-07) + UnlinkInjectGroup POST (D-12):** Added `[HttpPost][Authorize][ValidateAntiForgeryToken] PreviewPairing([FromBody] PreviewPairingRequest)` calling `_injectService.PreviewPairingAsync` and returning `Json(summary)` (no write). Added `[HttpPost][Authorize][ValidateAntiForgeryToken] UnlinkInjectGroup(int injectGroupId)` resolving actor like the commit POST, calling `UnlinkInjectGroupAsync`, setting TempData Bahasa Indonesia, and returning `Json(new { ok, message })` (NOT RedirectToAction — Wave 4 JS contract). Score path `PreviewInjectScore` untouched.

## Task Commits

Each task was committed atomically (TDD: Task 1 RED test + GREEN impl in one feat commit; Task 2 wiring of pinned service symbols):

1. **Task 1: SearchLinkTargets JSON picker + MapToRequest link wiring (Surface 7)** — `14102d02` (feat)
2. **Task 2: PreviewPairing dry-run (D-07) + UnlinkInjectGroup POST (D-12)** — `65595e75` (feat)

_TDD note: 397-01 authored the RED integration suites; 397-02 made them GREEN at the service level. This plan (Wave 2) wires the controller HTTP seams to those already-green service symbols. Task 1's new unit test `Maps_LinkTargetRepId_from_chip` was authored RED (asserted req.LinkTargetRepId == 1234, got null) then turned GREEN by the MapToRequest edit — committed together as feat per the controller-wiring nature._

## Contracts for Wave 3 (Plan 04 view/JS)

### SearchLinkTargets JSON row shape (the fields the Wave 3 modal renders)
`GET /Admin/SearchLinkTargets?term={text}&injectType={PreTest|PostTest}` → `Json([...])`, each row:

| Field | Type | Meaning |
|-------|------|---------|
| `RepresentativeId` | int | id of representative session (oldest by CreatedAt) — set into chip → `vm.LinkedTargetRepId` |
| `Title` | string | room title (render via `.textContent`, XSS-safe) |
| `Category` | string | room category |
| `Schedule` | DateTime | representative schedule |
| `CompletedAt` | DateTime? | representative completed-at (may be null for upcoming online) |
| `AssessmentType` | string | always the opposite type (e.g. "PostTest" when injecting Pre) |
| `LinkedGroupId` | int? | non-null = Kasus A (already grouped); null = Kasus B (standalone) — badge indicator |
| `UserCount` | int | distinct UserId count in the group/room |
| `IsPrePostGroup` | bool | true = Kasus A grouped room |
| `IsManualEntry` | bool | true if any session in the group is inject (badge "Inject", D-10) |

Non-Pre/Post `injectType` → `Json(Array.Empty<object>())` (Standard has no picker, D-06). Rows capped at 50.

### Standalone grouping key used (must match 397-02 Kasus B write-to-all key)
`GroupBy(s => new { s.Title, s.Category, Date = s.Schedule.Date })` — IDENTICAL to `ResolveLinkContextAsync` Kasus B target-room collection key (Title + Category + Schedule.Date). The picker set == the sticker set.

### Pairing preview endpoint
`POST /Admin/PreviewPairing` (RBAC + CSRF), body `PreviewPairingRequest { int? LinkTargetRepId; string AssessmentType; List<string> UserIds; DateTime CompletedAt }` → `Json(InjectPairingPreview { HasLink, Paired, Unpaired, WillTouchOnline, DateWarn, DoubleLinkErrors[] })`. Dry-run, NO write. `UserIds` = user.Id (picker checkbox value).

### Unlink endpoint (return shape)
`POST /Admin/UnlinkInjectGroup` (RBAC + CSRF), form field `injectGroupId` (int) → `Json(new { ok: bool, message: string })`. NOT a redirect — Wave 4 JS calls via fetch and drives a toast + resets the link UI in place. (TempData Success/Error in Bahasa Indonesia is also set as a secondary notice.)

## Files Created/Modified
- `Controllers/InjectAssessmentController.cs` (+147):
  - `SearchLinkTargets` (NEW GET JSON action) — opposite-type picker, whitelist + parameterized search, Kasus A/B grouping, returns Json.
  - `MapToRequest` (MODIFY) — added `LinkTargetRepId = vm.LinkedTargetRepId` in the req initializer (only change; raw link columns untouched).
  - `PreviewPairing` (NEW POST action) — pairing dry-run via PreviewPairingAsync, returns Json.
  - `UnlinkInjectGroup` (NEW POST action) — atomic revert via UnlinkInjectGroupAsync, TempData BI + Json({ ok, message }).
  - `PreviewPairingRequest` (NEW controller-local DTO class in namespace) — keeps scope to this file (no Models edit).
- `HcPortal.Tests/InjectViewModelMapTests.cs` (MODIFY) — added unit `Maps_LinkTargetRepId_from_chip` (asserts req.LinkTargetRepId == chip, raw LinkedGroupId/LinkedSessionId stay null, standalone → null).

## Decisions Made
See key-decisions frontmatter. Notable:
- PreviewPairing is a **separate companion** endpoint (not an overload of the per-worker PreviewInjectScore) — the plan's lower-risk choice; preview==commit for 395/396 score is provably untouched (diff shows no edits to AssessmentScoreAggregator.Compute / InjectPreviewResult).
- `PreviewPairingRequest` lives **in the controller file** (not Models/InjectAssessmentDtos.cs) to honor the scope-lock "owns Controllers/InjectAssessmentController.cs ONLY".
- Standalone grouping key kept EXACTLY `Title+Category+Schedule.Date` to match Wave 2's write-to-all key (picker shows the same room set the Kasus B sticker covers).

## Deviations from Plan

None — plan executed exactly as written. The plan's Task 1 (a) code block and Task 2 (a)/(b) code blocks were implemented as specified, using `AssessmentConstants.AssessmentType.PreTest/PostTest` constants for the whitelist instead of inline string literals (equivalent behavior, follows codebase convention — not a behavioral deviation). The cited line anchors (verified at 396-03 HEAD cdcbf0f3) had shifted at the actual HEAD `e99a2cf2`; all anchors were re-grepped and re-read before editing per the critical note (MapToRequest at :341 not :336; PreviewInjectScore at :108-161 not :103-160).

## Issues Encountered
- Cited line numbers in the plan were stale (396 commits shifted them). Resolved by re-grepping `MapToRequest`, `PreviewInjectScore`, `SearchLinkTargets`, and the service signatures before any edit — no functional impact.
- Grep acceptance checks for "PartialView", "IsManualEntry .Where filter", and "RedirectToAction" returned non-zero counts; on inspection each non-zero match was a **comment** documenting the absence of that pattern (e.g. "// Json, BUKAN PartialView", "// TIDAK ada filter IsManualEntry", "// BUKAN RedirectToAction"). Confirmed via `grep -n` that there is no actual `return PartialView`, no `.Where(...IsManualEntry...)` filter in SearchLinkTargets, and no `RedirectToAction` call in UnlinkInjectGroup.

## Known Stubs
None — controller wires real service calls; no hardcoded empty data flows to UI; no TODO/FIXME/placeholder introduced (grep on the diff = 0).

## Threat Flags
None — no NEW security surface beyond the plan's `<threat_model>`. All new actions carry `[Authorize(Roles="Admin, HC")]` (T-397-09); POST actions carry `[ValidateAntiForgeryToken]` (T-397-10); injectType whitelisted + term parameterized (T-397-11); MapToRequest forwards only LinkTargetRepId (T-397-13); IDOR guard remains in the service for unlink (T-397-12). SearchLinkTargets is GET read-only (no AF needed).

## Verification Results
- `dotnet build HcPortal.csproj` → **Build succeeded**, 0 errors (after each task).
- Unit `Maps_LinkTargetRepId_from_chip` → RED (Assert.Equal failure, null vs 1234) before MapToRequest edit; GREEN after.
- Fast suite (`--filter "Category!=Integration"`) → **Passed! 390/390** (389 baseline + 1 new unit; no regression to 395/396 score path).
- 397 integration suites (`InjectLink|AntiDoubleLink|PreviewPairing|CrossGrouping|UnlinkInject`) → **Passed! 15/15** (service unchanged this wave; contract intact, real SQLEXPRESS).
- **0 migration**: `git diff --name-only HEAD~2 HEAD` = `Controllers/InjectAssessmentController.cs` + `HcPortal.Tests/InjectViewModelMapTests.cs` only; no `Migrations/`/`Data/`/`.sql`.
- No file deletions in either task commit.

## Acceptance Criteria (grep-verified)
- `SearchLinkTargets` signature → 1 match; returns `Json(rows)`; no actual `return PartialView` (0); `[Authorize(Roles="Admin, HC")]` present; no IsManualEntry `.Where` filter; Pre/Post whitelist present; `LinkTargetRepId = vm.LinkedTargetRepId` → 1; raw `LinkedGroupId =`/`LinkedSessionId =` from `vm.` → 0; standalone key `Date = s.Schedule.Date` → 1.
- `PreviewPairing` → 1, calls `PreviewPairingAsync`, returns `Json(summary)`, no SaveChanges in body.
- `UnlinkInjectGroup` → 1, has BOTH `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`, calls `UnlinkInjectGroupAsync`, TempData Bahasa Indonesia, returns `Json(new { ok, message })`, no `RedirectToAction` call.
- `PreviewInjectScore` score path unchanged (diff scope: no edit to Compute / InjectPreviewResult shape).

## User Setup Required
None — no external service configuration required. (Branch main; notify IT at push with migration=FALSE per CLAUDE.md Develop Workflow.)

## Next Phase Readiness
- **Wave 3 (397-04) view/JS** can now wire: the room picker modal against `SearchLinkTargets` JSON (row shape above), the chip → `LinkedTargetRepId` hidden field, the `#previewPairingSummary` against `PreviewPairing` (InjectPairingPreview shape), and the unlink button against `UnlinkInjectGroup` (`{ ok, message }` JSON, fetch + toast). Render all user-authored strings via `.textContent` (XSS-safe).
- **0 migration** maintained. No blockers. Score preview==commit (395/396) untouched.

## Self-Check: PASSED

- SUMMARY.md verified on disk (FOUND `.planning/phases/397-link-pre-post-ke-room-existing/397-03-SUMMARY.md`)
- `Controllers/InjectAssessmentController.cs` verified on disk (FOUND)
- Both task commits verified in git log (`14102d02`, `65595e75`)
- Build green; fast suite 390/390; 397 integration 15/15; 0 migration

---
*Phase: 397-link-pre-post-ke-room-existing*
*Completed: 2026-06-18*
