---
phase: 341
plan: 02
subsystem: admin-org-label
tags: [razor-view, bootstrap-modal, frontend, antiforgery]
requires: [341-01 OrgLabelController, ManageOrgLevelLabelsViewModel]
provides: [ManageOrgLevelLabels page, admin card navigation]
affects: [Views/Admin/ManageOrgLevelLabels.cshtml, Views/Admin/Index.cshtml]
tech-stack:
  added: []
  patterns: [server-render Razor model-bound, Bootstrap 5 modal, fetch+JSON AJAX, antiforgery JS, shared-toast]
key-files:
  created:
    - Views/Admin/ManageOrgLevelLabels.cshtml
  modified:
    - Views/Admin/Index.cshtml
key-decisions: [D-01 fetch+JSON, D-03 native confirm, D-04 admin card, D-07 shared-toast, D-10 reload]
requirements-completed: [ORG-LABEL-04]
duration: "~25 min"
completed: 2026-06-03
---

# Phase 341 Plan 02: Razor View + Admin Card Summary

Built `Views/Admin/ManageOrgLevelLabels.cshtml` (server-render table + 2 Bootstrap modals + inline JS fetch/antiforgery/toast) consuming Plan 01 controller, plus admin card in `Views/Admin/Index.cshtml`. Browser smoke UAT 10/10 PASS via Playwright MCP.

## Commits

| Task | Hash | Description |
|------|------|-------------|
| 1 | `689f6384` | feat(341-02): add ManageOrgLevelLabels Razor view |
| 2 | `eed64495` | feat(341-02): add Label Tier Organisasi admin card |

## Tasks Completed

- **Task 1** — `Views/Admin/ManageOrgLevelLabels.cshtml` (NEW, 229 LoC): breadcrumb + heading + info banner + `@Html.AntiForgeryToken()` + server-render table (`@foreach Model.Rows`, buffer row, Delete visibility 3-state) + 2 distinct-id modals (Edit/Add) + `@section Scripts` inline JS (getAntiForgeryToken/ajaxPost/submit handlers/native confirm/tooltip init).
- **Task 2** — `Views/Admin/Index.cshtml` (+16 LoC): card "Label Tier Organisasi" inserted after "Organization Structure" (Section A), role-gated Admin+HC, `bi-tags` icon, link `/Admin/ManageOrgLevelLabels`.
- **Task 3** — checkpoint:human-verify → **automated via Playwright MCP, 10/10 PASS** (see UAT log below).

## Acceptance Criteria (all PASS)

**Task 1 (15):** model directive=1, antiforgery=1, labelEditModal=4(≥3), labelAddModal=4(≥3), shared-toast.js=1, confirm verbatim=1, 3 endpoint URLs each=1, showToast=10(≥6), tooltip text=1, `hx-`=0, lines=229(≥180), build PASS.
**Task 2 (7):** ManageOrgLevelLabels=1, OrgLabel=1, "Label Tier Organisasi"=1, bi-tags=2, ManageOrganization preserved=1, bi-diagram-3 preserved=1, build PASS.

## Browser Smoke UAT (Playwright MCP, admin@pertamina.com)

| # | Step | Result |
|---|------|--------|
| 3 | Card "Label Tier Organisasi" after "Organization Structure" | ✅ PASS |
| 4 | Page render: breadcrumb + heading + banner + 4 rows (0 Bagian/1 Unit/2 Sub-unit[+Hapus]/3 buffer[+Tambah]) | ✅ PASS |
| 4b | Delete visibility: Hapus only on Level 2 (highest=2, 0 units) | ✅ PASS |
| 5 | Edit Level 0 → "Direktorat" → toast + reload → row updated; rollback to "Bagian" | ✅ PASS |
| 6 | Add Level 3 "Kelompok" → reload → row + new buffer Level 4; Delete moved to Level 3 | ✅ PASS |
| 7 | Delete Level 3 → native confirm verbatim "Hapus label Level 3 \"Kelompok\"? Tidak bisa diundo." → OK → reload → back to buffer | ✅ PASS |
| 8 | Empty label → toast "Label tidak boleh kosong."; 60-char → toast "Label maksimal 50 karakter." (client preview) | ✅ PASS |
| 9 | Edit Level 0 → "Unit" → **server reject** toast "Label 'Unit' sudah dipakai level lain." | ✅ PASS |
| 10 | Cancel (Batal) → modal closes, no change | ✅ PASS |

**Network:** 5 POST all 200 OK (2 Update + 1 Add + 1 Delete + 1 duplicate-Update). `[ValidateAntiForgeryToken]` passed → token wired (no 400). **Console:** 0 errors.

**DB hygiene (CLAUDE.md SEED_WORKFLOW):** snapshot `HcPortalDB_Dev_pre341uat.bak` before UAT → mutations self-rolled-back → full RESTORE after → labels 0/1/2 + 0 OrgLabel audit rows verified clean. SEED_JOURNAL marked cleaned.

## Build + Test Evidence

- `dotnet build` → Build succeeded, 0 Error (Razor compile clean).
- App ran clean on localhost:5277, 0 startup exception.

## Deviations from Plan

None - plan executed exactly as written. View markup verbatim RESEARCH Example 6 + PATTERNS §3.

## Next Phase Readiness

Ready for Plan 03 (xUnit `OrgLabelControllerTests` + manual UAT Coach 403 + audit log inspection). Full Edit/Add/Delete flow browser-verified functional.

## Self-Check: PASSED
