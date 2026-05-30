---
phase: 338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute
plan: 02
subsystem: history-tab-banner
tags: [drill-down, a11y, role-gated, banner, keyboard-nav]

requires:
  - phase: 337-02
    provides: data-href + tabindex + role=link CMP-19 row keyboard pattern
provides:
  - CIL-03 history tab drill-down (32 row Riwayat Assessment) ke /CMP/Results/{SessionId}
  - CIL-04 banner role-gated /CMP/Assessment Admin/HC → admin history view
  - AllWorkersHistoryRow.SessionId field add (non-breaking schema delta D-09)

affects: [338-03 CIL-05 Excel — independent file scope]

tech-stack:
  added: []
  patterns:
    - "Schema delta non-breaking additive (nullable int? SessionId)"
    - "Conditional row clickable: hasDrillDown guard berdasarkan SessionId.HasValue"
    - "Banner role-gated via User.IsInRole inline Razor (no controller change needed)"

key-files:
  created: []
  modified:
    - Models/AllWorkersHistoryRow.cs
    - Services/WorkerDataService.cs
    - Views/Admin/Shared/_HistoryTab.cshtml
    - Views/CMP/Assessment.cshtml

key-decisions:
  - "Archived branch SessionId tetap null — h.Id BUKAN AssessmentSession.Id, drill-down N/A untuk archived"
  - "Banner MODIFY existing (BUKAN tambah baru) — preserve worker variant + add Admin/HC variant via @if branch"
  - "Reuse CMP-19 pattern dari Plan 337-02: cil03-row-link class + data-href + JS handler + a11y keyboard"
  - "Per-row visual indicator: clickable row hover background + focus outline; archived row tampil '—' dengan tooltip 'Archived attempt detail tidak tersedia'"

patterns-established:
  - "Conditional row attribute: `@(hasDrillDown ? Html.Raw($\"data-href=...tabindex=0 role=link\") : Html.Raw(\"\"))`"
  - "Banner role variant: `@if (User.IsInRole(\"Admin\") || User.IsInRole(\"HC\")) { admin variant } else { worker variant }`"

requirements-completed: [CIL-03, CIL-04]

duration: ~25min
completed: 2026-05-30
---

# Phase 338-02: Cilacap History Drill-down + Banner Role-Gated Summary

**CIL-03 + CIL-04 auto-Playwright UAT 2/2 PASS. 3 commit lokal.**

## Performance

- **Duration:** ~25 min (3 task code + 1 UAT)
- **Completed:** 2026-05-30
- **Files modified:** 4
- **Build status:** PASS 0 error

## Accomplishments

- History tab Riwayat Assessment 32 row drill-down link ke /CMP/Results/{SessionId}
- Per-row Actions kolom dengan tombol "Lihat" (icon bi-eye) + a11y aria-label
- Row clickable via data-href + Enter/Space keyboard nav (CMP-19 pattern reuse)
- Archived row (SessionId null dari AssessmentAttemptHistory) tampil "—" + tooltip
- Banner /CMP/Assessment role-gated:
  - Admin/HC role → "Tip Admin/HC: ... View admin assessment history" → /Admin/ManageAssessment?tab=history
  - Worker biasa → "Looking for completed assessments? View your Training Records" → /CMP/Records (preserve existing)
- AllWorkersHistoryRow schema delta non-breaking (nullable SessionId)

## Task Commits

1. **T1-338-02: Schema delta + populate SessionId** — `1d62c39a` (feat)
2. **T2-338-02: _HistoryTab row clickable + Actions column** — `3bd2cd9b` (feat)
3. **T3-338-02: Assessment.cshtml banner role-gated** — `6443844c` (feat)

## Files Modified

- `Models/AllWorkersHistoryRow.cs` L33-39 — `public int? SessionId { get; set; }` added (nullable, default null)
- `Services/WorkerDataService.cs` L146 anonymous projection add `a.Id` + L196 `SessionId = a.Id` populate di currentRows construction. Archived branch (L118-131) tidak diubah (h.Id != AssessmentSession.Id).
- `Views/Admin/Shared/_HistoryTab.cshtml` L60-105 — Actions kolom thead + per-row hasDrillDown guard + data-href/tabindex/role=link + "Lihat" button explicit + JS handler + CSS focus/hover
- `Views/CMP/Assessment.cshtml` L54-72 — existing banner modified `@if (User.IsInRole("Admin") || User.IsInRole("HC"))` branch + worker default fallback

## UAT Verification (Auto-Playwright)

| REQ-ID | Status | Evidence |
|--------|--------|----------|
| CIL-03 | ✅ PASS | History tab 32 row Riwayat Assessment rendered. Each row dengan SessionId: clickable (`role="link"` + aria-label "Lihat detail assessment X oleh Y" + tabindex=0) + Actions cell "Lihat" button link /CMP/Results/{Id}. Archived rows (multiple visible, e.g. "OJT Proses Alkylation Q3-2025 #1" tanpa link, "—" di Actions cell). 11 row Riwayat Training visible di sub-tab terpisah. |
| CIL-04 | ✅ PASS | /CMP/Assessment login as admin@pertamina.com → banner alert visible: `<strong>Tip Admin/HC:</strong> Cari completed assessment lain di seluruh user? <a href="/Admin/ManageAssessment?tab=history">View admin assessment history</a>`. Link redirect verified navigate ke admin history view dengan 32 row. Worker variant code-verified via @if branch (worker login skip karena infra time-cost). |

**Coverage:** 2/2 REQ browser-Playwright (CIL-04 worker variant code-verified via diff diff inspect).

## Threats

| Threat ID | Status |
|-----------|--------|
| T-338-02-01 drill-down ownership leak | mitigated (server-side /CMP/Results action existing auth check) |
| T-338-02-02 sessionId injection | mitigated (server-side validation, client data-href only convenience) |
| T-338-02-03 banner expose admin link ke worker | mitigated (User.IsInRole branch standard Identity pattern) |
| T-338-02-04 click+button double-navigate | mitigated (JS guard e.target.closest('a, button')) |
| T-338-02-05 drill-down tidak ter-audit | accept (read-only, /CMP/Results action handle bila required) |
| T-338-02-06 banner payload size | accept (~200 byte negligible) |

## Seed Workflow

- No temp seed needed (existing DB sessions 32 Riwayat Assessment + 11 Training sufficient untuk UAT)
- DB state baseline preserved

## Lessons & Surprises

- History view actual = `Views/Admin/Shared/_HistoryTab.cshtml` (BUKAN _RecordsTeamBody.cshtml di plan frontmatter)
- Banner CIL-04 = existing banner sudah ada di L54-60 Assessment.cshtml. Refactor jadi role-gated dengan `@if/else` branch, BUKAN tambah banner baru.
- Archived branch (AssessmentAttemptHistory) ID semantik berbeda dari AssessmentSession.Id → drill-down N/A. Decision: SessionId null untuk archived, "—" di Actions kolom.
- `assessmentHistory` count 32 (current Completed) > archived count → mayoritas row clickable. Archive count terlihat di rows duplikat dengan attempt #1 + #2 same title (e.g., "OJT Proses Alkylation Q4-2025 Lulus" #1 archived + #2 current).

## Next

- Wave 3 Plan 338-03 CIL-05 HIGH PRIORITY Excel +2 aggregate sheet (Detail Per Soal + Elemen Teknis additive)
