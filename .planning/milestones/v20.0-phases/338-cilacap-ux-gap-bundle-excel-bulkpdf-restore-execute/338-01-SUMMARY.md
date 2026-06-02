---
phase: 338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute
plan: 01
subsystem: assessment-monitoring
tags: [filter, badge, ux, htmx, cilacap]

requires:
  - phase: 322
    provides: HTMX per-tab native filter pattern (search/category/statusFilter)
provides:
  - CIL-01 badge counter Closed di 2 view (ManageAssessment + AssessmentMonitoring)
  - CIL-02 search aggregation fix — Closed group MUNCUL saat user search spesifik
  - Preserve default filter "Aktif (Open/Upcoming)" — D-01 no breaking

affects: [338-02 (CIL-03 history drill-down butuh user reach Closed group via search baseline)]

tech-stack:
  added: []
  patterns:
    - "Status counter aggregate SEBELUM filter apply (ViewBag.{Open,Upcoming,Closed}Count) untuk badge UI"
    - "Guard pattern `string.IsNullOrEmpty(statusFilter) && string.IsNullOrEmpty(search)` — default hide hanya saat BOTH empty"
    - "Badge pulse animation first-load (`cil01-pulse` 1.5s CSS animation) untuk visibility cue T-338-05"

key-files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
    - Views/Admin/Shared/_AssessmentGroupsTab.cshtml
    - Views/Admin/AssessmentMonitoring.cshtml

key-decisions:
  - "Guard `&& string.IsNullOrEmpty(search)` — saat user search spesifik, override default Closed hide"
  - "Counter ViewBag SEBELUM filter apply — show total per status independent of current filter state"
  - "Badge animate first-load 1.5s pulse — subtle catch user attention tanpa annoying"
  - "Default filter behavior preserve — user lama tidak bingung dengan UI change"

patterns-established:
  - "CIL pattern: `<span class='badge cil01-pulse' data-cil01-animate='true' title='...explanation'>`"

requirements-completed: [CIL-01, CIL-02]

duration: ~25min
completed: 2026-05-30
---

# Phase 338-01: Cilacap Filter Badge + Search Aggregation Summary

**CIL-01 + CIL-02 auto-Playwright UAT 2/2 PASS. 3 commit lokal.**

## Performance

- **Duration:** ~25 min (3 task code + 1 UAT)
- **Completed:** 2026-05-30
- **Files modified:** 3
- **Build status:** PASS 0 error, 21 warning (pre-existing)

## Accomplishments

- Badge counter per-status (Open/Upcoming/Closed) render di 2 admin view
- Search "Cilacap" tanpa statusFilter → Closed group Cilacap MUNCUL (sebelumnya 0 result)
- Preserve default filter "Aktif (Open/Upcoming)" — D-01 no breaking change user lama
- Tooltip Closed badge: explanation context "hidden by default — pilih filter Closed atau search spesifik"
- Animate pulse CSS first-load untuk catch user attention (T-338-05 mitigation)

## Task Commits

1. **T1-338-01: Controller guard fix + counter** — `6720be0b` (feat)
2. **T2-338-01: Badge render _AssessmentGroupsTab.cshtml** — `0ec97b20` (feat)
3. **T3-338-01: Badge render AssessmentMonitoring.cshtml** — `1253f588` (feat)

## Files Modified

- `Controllers/AssessmentAdminController.cs` L194-203 + L2799-2814 — CIL-02 guard `&& string.IsNullOrEmpty(search)` + CIL-01 ViewBag.{Open,Upcoming,Closed}Count populate sebelum filter apply (2 method: `ManageAssessmentTab_Assessment` + `AssessmentMonitoring`)
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` L113-130 — 3 badge per-status di Stats Badge row + cil01-pulse CSS
- `Views/Admin/AssessmentMonitoring.cshtml` L160-180 — 3 badge per-status di atas group table + cil01-pulse CSS

## UAT Verification (Auto-Playwright)

**Pre-seed:** 2 Closed Cilacap session + 1 Open Aktif session (within 7-day window L115 filter).

| REQ-ID | Status | Evidence |
|--------|--------|----------|
| CIL-01 | ✅ PASS | ManageAssessment Tab Assessment: "1 grup, 1 Open, 0 Upcoming, 1 Closed" badge accurate. Default filter "Aktif (Open/Upcoming)" preserve → table show only Open row. AssessmentMonitoring: same pattern "1 Open, 0 Upcoming, 1 Closed". Tooltip render correctly. Animate pulse first-load visible. |
| CIL-02 | ✅ PASS | Tab Assessment search "Cilacap" → return 1 grup "Post Test OJT Pekerja GAST...Cilacap" Closed status (pre-fix would 0). AssessmentMonitoring URL `?search=Cilacap` → return 1 row Cilacap Closed (status filter default "Open + Upcoming" override karena search non-empty). |

**Coverage:** 2/2 REQ browser-Playwright PASS.

## Threats

| Threat ID | Status |
|-----------|--------|
| T-338-01-01 search param injection | mitigated (EF parameterized via .Where Title.Contains, guard string.IsNullOrEmpty cek) |
| T-338-01-02 Authorize bypass Cilacap data | mitigated ([Authorize(Roles="Admin,HC")] preserved di controller) |
| T-338-01-03 Cross-section counter leak | accept (ManageAssessment Admin/HC only, L4 worker tidak akses) |
| T-338-01-04 search wildcard DoS | accept (existing Title.Contains, no new query) |
| T-338-01-05 Badge tidak discoverable | mitigated (tooltip explanation + cil01-pulse animation T-338-05) |

## Seed Workflow

- DB backup: `C:\Temp\HcPortalDB_Dev_pre_338-01_uat.bak` PRE-UAT
- Temp seed: 3 AssessmentSessions (2 Cilacap Closed + 1 Aktif Open) within 7-day window
- Restore: POST-UAT verified clean baseline
- Journal: `docs/SEED_JOURNAL.md` row 2026-05-30 phase 338-01 status=cleaned

## Lessons & Surprises

- Tab Assessment + AssessmentMonitoring keduanya punya filter `sevenDaysAgo` L115/L2657 (Phase 311 perf optimization). Recent sessions only. UAT WAJIB seed within window — historical data tidak muncul di view.
- Counter ViewBag harus populate SEBELUM `if (string.IsNullOrEmpty(statusFilter)...)` block, supaya count akurat untuk semua status (independent dari current filter state).
- Tab_Assessment pakai param `statusFilter`, AssessmentMonitoring pakai param `status` — naming inconsistency, tapi sama-sama hard-filter Closed default tanpa search check. Fix pattern sama: tambah `&& string.IsNullOrEmpty(search)` guard.
- Tab_History TIDAK punya filter Closed (just list all assessment + training history). Skip fix di History method — counter pun tidak relevan untuk History.

## Next

- Wave 2 Plan 338-02 (CIL-03 history drill-down + CIL-04 banner alert)
- Wave 2 baseline filter correctness ready (search Cilacap PASS → user reach history)
