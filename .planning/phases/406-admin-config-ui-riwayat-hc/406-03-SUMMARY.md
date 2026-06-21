---
phase: 406-admin-config-ui-riwayat-hc
plan: 03
subsystem: ui
tags: [aspnet-mvc, razor, bootstrap, modal, ajax, lazy-fetch, playwright, retake, riwayat, xss-safe]

# Dependency graph
requires:
  - phase: 406-01-riwayat-backend
    provides: "RiwayatPercobaan GET endpoint ([Authorize Admin,HC], lazy-AJAX, @-encoded PartialView) + _RiwayatPercobaan.cshtml accordion+per-soal tri-state partial + RiwayatUnifier (current floats top via max+1)"
provides:
  - "Per-peserta 'Riwayat Percobaan' dropdown trigger (.btn-riwayat-percobaan, Completed-gated) in AssessmentMonitoringDetail carrying data-session-id + data-worker-name"
  - "ONE shared #riwayatPercobaanModal (modal-lg modal-dialog-scrollable) + #riwayatBody — not per-row"
  - "Lazy-fetch JS: delegate click -> title via .textContent (XSS-safe) -> spinner -> appUrl('/Admin/RiwayatPercobaan?sessionId='+encodeURIComponent) -> innerHTML server @-encoded partial -> alert-warning on failure"
  - "tests/e2e/riwayat-hc-406.spec.ts (5 scenarios @5270: open/per-soal/current/pending/xss) + tests/sql/riwayat-hc-406-seed.sql"
affects: [407-worker-self-service, 408-test-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Lazy-AJAX modal: dropdown-item trigger -> shared modal shell -> appUrl-prefixed fetch -> innerHTML of server @-encoded partial (mirror EditHistoryPartial :986-1001)"
    - "Anti-DOM-XSS: modal title set via .textContent (never innerHTML) for user-content (worker name)"
    - "PathBase-aware fetch via appUrl() (Dev /KPB-PortalHC sub-path) — never a bare /Admin/... string (Lesson 385 PXF-01)"

key-files:
  created:
    - "tests/e2e/riwayat-hc-406.spec.ts"
    - "tests/sql/riwayat-hc-406-seed.sql"
  modified:
    - "Views/Admin/AssessmentMonitoringDetail.cshtml"
    - "docs/SEED_JOURNAL.md"

key-decisions:
  - "Trigger placement = dropdown <li> (Research Open Q2 recommendation), gated session.UserStatus == 'Completed' (consistent with Edit/Reset items; partial shows empty-state otherwise)"
  - "Modal shell placed after #activityLogModal (OUTSIDE @if(Model.IsPackageMode)) so it always renders regardless of package mode"
  - "Lazy-fetch JS appended inside the existing main IIFE (:804-1031) before })(); — reuses page-global appUrl + bootstrap"

patterns-established:
  - "Single shared modal + delegated click on .btn-riwayat-percobaan (event delegation survives dynamic rows)"
  - "e2e dropdown-item interaction: open ⋮ dropdown (button[aria-haspopup]) FIRST, then click the now-visible dropdown-item (Bootstrap collapses menu by default)"

requirements-completed: [RTK-08]

# Metrics
duration: 23min
completed: 2026-06-21
---

# Phase 406 Plan 03: Riwayat HC Modal Mount (RTK-08) Summary

**Per-peserta 'Riwayat Percobaan' dropdown trigger + ONE shared Bootstrap modal-lg-scrollable + appUrl-prefixed lazy-fetch JS that drops the 406-01 _RiwayatPercobaan partial into the modal body (title via .textContent XSS-safe), runtime-verified @5270 by a 5-scenario Playwright spec (open/per-soal/current/pending/xss) — closing RTK-08's interactive path. migration=FALSE.**

## Performance

- **Duration:** 23 min
- **Started:** 2026-06-21T12:30:26Z
- **Completed:** 2026-06-21T12:53:44Z
- **Tasks:** 2
- **Files modified:** 4 (2 created, 2 modified)

## Accomplishments
- Wired the SURFACE 2 front door over 406-01's data path: dropdown trigger + single modal shell + lazy-fetch JS in `AssessmentMonitoringDetail.cshtml` (56 insertions, 1 file). The fetched accordion (per-attempt + per-soal tri-state) comes entirely from 406-01 — this plan only mounts trigger/shell/wiring, did NOT re-implement the partial.
- XSS posture proven: modal title set via `.textContent` (T-406-10); body innerHTML receives already-`@`-encoded server HTML (T-406-11); fetch uses `appUrl(...)` + `encodeURIComponent` (T-406-13). The e2e "xss" test confirms a `<script>` AnswerText payload is rendered inert (`window.__riwayatXss406` stays `undefined`).
- New Playwright spec `riwayat-hc-406.spec.ts` GREEN 5/5 @5270 (`--workers=1`) — runtime Razor verification (Lesson 354). Self-contained SEED_WORKFLOW seed (`riwayat-hc-406-seed.sql`): a `[RIWAYAT406]` Completed session + package chain (current live attempt) + 2 archived attempts + 5 archive rows covering correct/wrong/XSS/essay-pending. DB snapshot→seed→RESTORE honored; journal `cleaned`.
- Full xUnit regression held at 604/0/2 (no regression; 2 skips = pre-existing SQLEXPRESS-gated Phase 404 tests).

## Task Commits

Each task was committed atomically:

1. **Task 1: Riwayat dropdown trigger + shared modal shell + lazy-fetch JS** - `83054ba8` (feat)
2. **Task 2: Playwright riwayat-hc-406 e2e + seed + journal** - `a4cea6b0` (test)

**Plan metadata:** (final docs commit — this SUMMARY + STATE.md + ROADMAP.md + REQUIREMENTS.md)

## Files Created/Modified
- `Views/Admin/AssessmentMonitoringDetail.cshtml` (modified) - +dropdown `<li>` `.btn-riwayat-percobaan` (Completed-gated, ~:368) + ONE `#riwayatPercobaanModal` shell (after `#activityLogModal`, always-rendered) + lazy-fetch JS inside main IIFE (`.textContent` title, `appUrl` fetch, spinner + alert-warning states)
- `tests/e2e/riwayat-hc-406.spec.ts` (created) - 5 scenarios (open/per-soal/current/pending/xss) + SEED_WORKFLOW beforeAll/afterAll snapshot/restore, login admin@pertamina.com, `--workers=1` @5270
- `tests/sql/riwayat-hc-406-seed.sql` (created) - `[RIWAYAT406]` Completed session + package chain + 2 `AssessmentAttemptHistory` + 5 `AssessmentAttemptResponseArchive`; idempotent WIPE-AND-INSERT + THROW 51406 guard
- `docs/SEED_JOURNAL.md` (modified) - one entry (active→cleaned), Layer 4 leftovers=0

## Decisions Made
- **Trigger = dropdown `<li>`** (not inline icon) per Research Open Q2; gated `session.UserStatus == "Completed"` (the partial itself renders an empty-state when no archived attempts exist). Confirmed the real dropdown structure (`<ul class="dropdown-menu dropdown-menu-end">` per session row, items Edit Jawaban/Reset/Akhiri Ujian/Reshuffle) before inserting before its `</ul>`.
- **Modal placement after `#activityLogModal`** (outside `@if(Model.IsPackageMode)`) so it always renders — the reshuffle modals at `:481-496` ARE inside the package-mode `@if`, but `#akhiriSemuaModal`/`#activityLogModal` are not. Mirrored those.
- **JS inside the existing main IIFE** (`:804-1031`) to reuse the page-global `appUrl` + `bootstrap` and the established lazy-fetch idiom (EditHistoryPartial loader). Used event delegation on `document` (survives dynamic rows / future SignalR updates).
- **Route confirmed against 406-01:** `GET /Admin/RiwayatPercobaan?sessionId=N` (`AssessmentAdminController.cs:3485`, `[Authorize(Roles="Admin, HC")]`, read-only GET, returns `PartialView("_RiwayatPercobaan", ...)`).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Escaped bare `@` in two JS comment lines (Razor parse error)**
- **Found during:** Task 1 (first `dotnet build` after view edits)
- **Issue:** Two `//` JS comments inside the `<script>` block contained the literal `@-encoded` (e.g. "body innerHTML = server @-encoded HTML"). Razor parses `@` inside `<script>`; `@-` was treated as a code transition emitting an empty `Write()`, producing `CS1501: No overload for method 'Write' takes 0 arguments` (4 errors).
- **Fix:** Changed `@-encoded` → `@@-encoded` (Razor escape) in both comment lines. The `@@-encoded` inside the Razor `@* ... *@` comments was already fine.
- **Files modified:** Views/Admin/AssessmentMonitoringDetail.cshtml
- **Verification:** `dotnet build` 0 errors after the fix; grep gates all pass; e2e GREEN.
- **Committed in:** `83054ba8` (Task 1 commit)

**2. [Rule 1 - Bug] e2e helper opened modal before revealing the dropdown-item trigger**
- **Found during:** Task 2 (first spec run — "open" failed)
- **Issue:** `openRiwayatModal` asserted `.btn-riwayat-percobaan` `toBeVisible()` then clicked it directly, but it is a Bootstrap dropdown-item (hidden while the `⋮` menu is collapsed). The locator resolved to the real button (proving Task 1 wiring renders) but `hidden`.
- **Fix:** Open the row's `⋮` dropdown toggle (`button[aria-haspopup="true"]`) FIRST, then assert+click the now-visible item. (Established as a reusable e2e pattern for dropdown-item triggers.)
- **Files modified:** tests/e2e/riwayat-hc-406.spec.ts
- **Verification:** Re-run → 5/5 GREEN @5270.
- **Committed in:** `a4cea6b0` (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (1 blocking Razor-escape, 1 e2e interaction bug)
**Impact on plan:** Both trivial and necessary to build/run; no scope creep, no production-behavior change. The `@@` escape is required for the view to compile.

## Issues Encountered
None beyond the two auto-fixed items. The seed SQL was dry-run against live SQLEXPRESS (snapshot→seed→verify→RESTORE) before wiring into the spec — caught one NOT NULL column (`PackageQuestions.MaxCharacters`) and added it before the spec run, so the spec's own beforeAll seed executed cleanly first try.

## Known Stubs
None. The trigger/modal/JS consume the real 406-01 endpoint + partial with live + archived data. No hardcoded empty values, placeholder text, or unwired data sources.

## Threat Flags
None — no new security surface beyond the plan's `<threat_model>` (T-406-10..13). The GET endpoint, `@`-encoding, `.textContent` title, and `appUrl` fetch are all covered; the e2e "xss" test actively proves inertness.

## User Setup Required
None - no external service configuration required. migration=FALSE (Razor view + e2e only; no schema or backend change).

## Next Phase Readiness
- **Phase 406 = ALL 3 PLANS COMPLETE** (406-01 riwayat backend ✅, 406-02 retake config UI ✅, 406-03 riwayat HC modal mount ✅). RTK-05 + RTK-08 done; migration=FALSE for the whole phase (405 carried the only v32.4 migration).
- **Phase 407** (worker self-service Results/Records + `RetakeExam` endpoint, parallel sibling depends 405) and **Phase 408** (final Test & UAT, depends 406+407) are unblocked. The lazy-fetch + tri-state partial pattern is ready for any worker-facing riwayat reuse.
- No blockers. `dotnet build` 0 errors; e2e riwayat-hc-406 5/5 GREEN @5270; full xUnit 604/0/2. DB confirmed RESTORED (RIWAYAT406 + matrix leftovers = 0).

## Self-Check: PASSED

- Files verified on disk: Views/Admin/AssessmentMonitoringDetail.cshtml, tests/e2e/riwayat-hc-406.spec.ts, tests/sql/riwayat-hc-406-seed.sql, 406-03-SUMMARY.md — all FOUND.
- Commits verified: 83054ba8 (Task 1 feat), a4cea6b0 (Task 2 test) — all FOUND.
- AssessmentMonitoringDetail contains `btn-riwayat-percobaan`, exactly one `id="riwayatPercobaanModal"`, `appUrl('/Admin/RiwayatPercobaan`, `encodeURIComponent(sid)`, `.textContent` title — confirmed.
- e2e riwayat-hc-406 5/5 GREEN @5270; full xUnit 604/0/2; DB RESTORED (RIWAYAT406 + matrix leftovers = 0).

---
*Phase: 406-admin-config-ui-riwayat-hc*
*Completed: 2026-06-21*
