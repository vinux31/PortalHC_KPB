---
phase: 406-admin-config-ui-riwayat-hc
plan: 02
subsystem: ui
tags: [razor, bootstrap, retake, assessment, progressive-disclosure, asp-for, playwright]

# Dependency graph
requires:
  - phase: 405-backend-core
    provides: "UpdateRetakeSettings endpoint + ViewBag retake (AllowRetake/MaxAttempts/RetakeCooldownHours/HideRetakeToggle/RetakeMaxAttemptsUsedInGroup) + RetakeRules.ShouldHideRetakeToggle + AssessmentSession retake fields with [Range]"
provides:
  - "Retake config card 'Ujian Ulang' in ManagePackages.cshtml (mirror shuffle, no-lock, progressive disclosure, non-blocking warning, hide Pre-Test/Manual)"
  - "asp-for retake binding in CreateAssessment Step 3 + EditAssessment (native AssessmentSession model bind)"
  - "Playwright e2e retake-config-406.spec.ts (6 scenarios runtime-verify the Razor surface @5270)"
affects: [408-test-uat, 407-worker-self-service]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Progressive-disclosure JS keyed on toggle id (#allowRetake / #AllowRetake) toggling .d-none — inputs stay in DOM, server Math.Clamp is the real guard"
    - "Defensive ViewBag value-type cast with ?? coalesce on dynamic warning condition (Pitfall 1 — immunize against future ViewBag nullability refactor)"
    - "Mirror existing config-card markup (shuffle) MINUS lock logic — config card POSTs to existing endpoint, no controller change"

key-files:
  created:
    - "tests/e2e/retake-config-406.spec.ts"
  modified:
    - "Views/Admin/ManagePackages.cshtml"
    - "Views/Admin/CreateAssessment.cshtml"
    - "Views/Admin/EditAssessment.cshtml"
    - "docs/SEED_JOURNAL.md"

key-decisions:
  - "Reused shuffle card markup verbatim but dropped ALL lock/disabled logic (D-03 — retake config is retroactive, must stay editable; disabled count stayed 3 = shuffle baseline)"
  - "Warning cast uses (int)(ViewBag.X ?? default) defensive coalesce per RESEARCH Pitfall 1 even though controller guarantees non-null"
  - "EditAssessment route is /Admin/EditAssessment?id={id} (query string) — [Route(\"Admin/[action]\")] has no {id} segment"
  - "Warning e2e triggered by seeding 2 AssessmentAttemptHistory rows for one user (RetakeMaxAttemptsUsedInGroup=3) + MaxAttempts=1, instead of relying on live attempt counts"

patterns-established:
  - "Pattern: config card that mirrors a sibling card but selectively omits lock — keep grep-able disabled count unchanged as a regression guard"
  - "Pattern: e2e for hidden-card condition via SQL UPDATE on a wizard-created session (AssessmentType='PreTest') then assert .card hasText count 0"

requirements-completed: [RTK-05]

# Metrics
duration: 38min
completed: 2026-06-21
---

# Phase 406 Plan 02: Admin Retake Config UI Summary

**"Ujian Ulang" config card in ManagePackages (mirror shuffle, no-lock, toggle-driven disclosure of MaxAttempts/cooldown, non-blocking warning, hide for Pre-Test/Manual) plus native asp-for binding in CreateAssessment Step 3 and EditAssessment — all runtime-verified by 6 green Playwright scenarios @5270.**

## Performance

- **Duration:** ~38 min
- **Started:** 2026-06-21T11:59Z (approx, post 406-01)
- **Completed:** 2026-06-21T12:22:37Z
- **Tasks:** 3
- **Files modified:** 4 (3 views + journal) + 1 created (spec)

## Accomplishments
- Retake config card renders after the shuffle card for graded assessments, hidden entirely for Pre-Test / Manual (ViewBag.HideRetakeToggle guard), with progressive-disclosure of two clamped number inputs (MaxAttempts 1–5, RetakeCooldownHours 0–168).
- Non-blocking alert-warning when MaxAttempts < RetakeMaxAttemptsUsedInGroup, using defensive `?? `-coalesced casts; Save is never disabled (D-03 no-lock confirmed — disabled count unchanged from shuffle baseline of 3).
- Card POSTs to the EXISTING UpdateRetakeSettings endpoint (RBAC Admin/HC + AntiForgery + clamp + sibling propagation + PRG) — zero controller change.
- CreateAssessment (Step 3, after shuffle column) + EditAssessment (Pengaturan Assessment card) bind AllowRetake/MaxAttempts/RetakeCooldownHours via native asp-for on the AssessmentSession model with validation spans and disclosure JS keyed on #AllowRetake.
- 6 Playwright scenarios GREEN @5270 (card render+disclosure, hide Pre-Test, save-persist PRG, non-blocking warning + Save still enabled, Create Step-3 binding, Edit reflect-persisted).

## Task Commits

Each task was committed atomically:

1. **Task 1: Retake config card in ManagePackages (mirror shuffle, no lock) + disclosure JS** - `67301e35` (feat)
2. **Task 2: asp-for retake binding in CreateAssessment (Step 3) + EditAssessment** - `96dd26bd` (feat)
3. **Task 3: Playwright retake-config-406.spec.ts (runtime-verify @5270) + seed journal** - `5d77f2ba` (test)

**Plan metadata:** _(this SUMMARY + STATE + ROADMAP)_ — see final docs commit.

## Files Created/Modified
- `Views/Admin/ManagePackages.cshtml` - Retake card after shuffle card (toggle + #retakeFields disclosure + 2 number inputs + non-blocking warning + hide guard) + disclosure JS in @section Scripts
- `Views/Admin/CreateAssessment.cshtml` - Retake column in Step 3 / Group B grid (after shuffle col, before Sertifikat) + disclosure JS
- `Views/Admin/EditAssessment.cshtml` - Retake fields in "Pengaturan Assessment" card (near PassPercentage / AllowAnswerReview) + disclosure JS
- `tests/e2e/retake-config-406.spec.ts` - 6-scenario Playwright spec mirroring shuffle.spec.ts (DB backup/restore + wizard + SQL temp seed)
- `docs/SEED_JOURNAL.md` - temp-seed entry for the e2e run, marked cleaned

## Decisions Made
- Mirrored the shuffle card structure verbatim but dropped lock logic entirely (D-03 retroactive config). Verified the only `disabled` occurrences remain in the shuffle card (grep count = 3, unchanged).
- Used `(int)(ViewBag.MaxAttempts ?? 2)` / `(int)(ViewBag.RetakeMaxAttemptsUsedInGroup ?? 0)` defensive casts on the warning condition (RESEARCH Pitfall 1) despite the controller guaranteeing non-null.
- EditAssessment opened via `/Admin/EditAssessment?id={id}` query route (no `{id}` route segment under `[Route("Admin/[action]")]`).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Corrected wizard selectors + EditAssessment route in the e2e spec**
- **Found during:** Task 3 (Playwright spec authoring)
- **Issue:** First draft of Scenario 5 used non-existent selectors (`#step1`, `#categoryInput`, `#titleInput`) and Scenario 6 used a path-segment route `/Admin/EditAssessment/{id}` that does not match the controller's attribute route.
- **Fix:** Imported the real `wizardSelectors` (`#step-1`, `#Category`, `#Title`, `#btnNext1/2`) + selected a participant to pass `validateStep(2)` gate before reaching Step 3; switched Scenario 6 to `/Admin/EditAssessment?id={id}` (query-string bind, since `[Route("Admin/[action]")]` has no `{id}` segment).
- **Files modified:** tests/e2e/retake-config-406.spec.ts
- **Verification:** All 6 scenarios pass; Scenario 5 reaches Step 3 and reveals the fields, Scenario 6 loads the Edit form and reflects persisted values.
- **Committed in:** `5d77f2ba` (Task 3 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking — test-infra selector/route correction).
**Impact on plan:** No production-code deviation; the card/binding markup followed the UI-SPEC contract verbatim. The fix was confined to the e2e spec to match real DOM ids and routing. No scope creep.

## Issues Encountered
- The targeted Playwright run nests inside the project's global setup/teardown (matrix-seed snapshot/restore). My spec's own beforeAll/afterAll snapshot covered the wizard + history temp seed; the global teardown restored the DB to baseline. Verified post-run: 60 AssessmentSessions (pre-test baseline) and 0 RTK406 history rows — DB fully RESTORED, journal marked cleaned, no leftover `.bak`.

## User Setup Required
None - no external service configuration required. migration=FALSE (UI-only; retake columns/endpoint shipped in Phase 405).

## Threat Surface
No new security surface introduced. The card POSTs to the existing UpdateRetakeSettings (RBAC Admin/HC + AntiForgery + server Math.Clamp); inputs' min/max are UX hints only. Card hidden for Pre-Test/Manual (T-406-09). No new endpoint, no Html.Raw, no user-content rendering in this plan.

## Known Stubs
None. All surfaces wired to live ViewBag data / the existing endpoint and runtime-verified.

## Next Phase Readiness
- RTK-05 (Admin/HC retake config UI) complete and e2e-verified. Plan 406-03 (Riwayat HC modal in AssessmentMonitoringDetail, RTK-08) is the remaining 406 surface — disjoint files (not touched here). Phase 408 (Test & UAT) will run the full retake lifecycle.
- Unpushed (branch ITHandoff); deploy bundles with v32.1+v32.3 (migration=TRUE carry from 405); this plan adds no migration.

## Self-Check: PASSED

- All created/modified files exist on disk (5/5 verified).
- All task commits exist in git history (67301e35, 96dd26bd, 5d77f2ba).
- `dotnet build` 0 error; Playwright 6/6 GREEN @5270; DB RESTORED (journal cleaned).

---
*Phase: 406-admin-config-ui-riwayat-hc*
*Completed: 2026-06-21*
