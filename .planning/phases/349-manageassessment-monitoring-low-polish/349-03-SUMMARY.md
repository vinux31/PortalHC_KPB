---
phase: 349-manageassessment-monitoring-low-polish
plan: 03
subsystem: Assessment Monitoring list (view + controller query)
tags: [i18n, display-nits, regenerate-token, aggregate-count, search, razor, efcore]
requires: [349-02]
provides:
  - "Monitoring list subtitle tanpa 'real-time'; kategori tidak dobel"
  - "Pre-Post grup punya dropdown Aksi (View Detail + Regenerate Token)"
  - "TotalCount exclude Cancelled -> progress bar bisa 100% (Pre-Post + standard parity)"
  - "Dropdown Status jujur saat search broaden (status=All)"
  - "Search list cocok Category"
affects:
  - Views/Admin/AssessmentMonitoring.cshtml
  - Controllers/AssessmentAdminController.cs
tech-stack:
  added: []
  patterns: [ef-parameterized-contains, switch-constant, aggregate-exclude-cancelled]
key-files:
  created: []
  modified:
    - Views/Admin/AssessmentMonitoring.cshtml
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "MAP-17: drop gate @if(!group.IsPrePostGroup) sehingga dropdown Aksi render untuk SEMUA grup (konten identik Pre-Post+standard, DRY) — bukan tambah else duplikat"
  - "MAP-13 Pre-Post exclude: (postSubs.Count>0 ? postSubs : preSubs).Count(a => a.Status != Cancelled) — apply ke list sub, BUKAN g (Pitfall 6)"
  - "MAP-13 konstanta AssessmentConstants.AssessmentStatus.Cancelled di kode baru (TotalCount+CancelledCount parity); literal sub-row pre-existing (preSubs/postSubs) dibiarkan (out of scope MAP-13 TotalCount, hindari scope creep)"
  - "MAP-15: cabang else-if baru (search non-empty + status kosong) set status=All; tak ubah filter result -> CIL-02 preserved"
requirements-completed: [MAP-13, MAP-14, MAP-15, MAP-16, MAP-17, MAP-23]
duration: ~18 min
completed: 2026-06-05
---

# Phase 349 Plan 03: Monitoring List Polish Summary

Polish Assessment Monitoring **list** (full reload, NO HTMX/SignalR) — buang "real-time", buang kategori dobel, dropdown Aksi Pre-Post (View Detail + Regenerate Token), extend search ke Category, TotalCount exclude Cancelled (progress bisa 100%), dan dropdown Status jujur saat search broaden. 2 file (view + controller list query), build 0 error.

**Tasks:** 3 | **Files:** 2 modified | **Duration:** ~18 min

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1+2 | `845c51fa` | feat(349-03): Monitoring list i18n + Pre-Post Regenerate dropdown (MAP-14/16/17/23) |
| 3 | `c1e38638` | fix(349-03): Monitoring list exclude-Cancelled + status jujur + search Category (MAP-13/15/23) |

> Split per-file: view (Task 1+2) vs controller (Task 3). MAP-23 lintas keduanya (placeholder di view commit, search predicate di controller commit).

## What Was Built

- **MAP-14:** Subtitle Monitoring list buang frasa "real-time" (list full reload, no SignalR).
- **MAP-16:** Hapus kategori muted subtitle dobel (badge kolom Kategori dipertahankan).
- **MAP-17 (D-04):** Drop gate `@if (!group.IsPrePostGroup)` di Aksi `<td>` → dropdown render untuk SEMUA grup. Pre-Post: `data-id=@group.RepresentativeId` (PreTest rep, LinkedGroupId) → `RegenerateToken` (MAM-01) regen semua siblings. Reuse JS `.btn-regenerate-token` + endpoint `[ValidateAntiForgeryToken]`, zero endpoint baru.
- **MAP-23:** Search predicate `|| a.Category.ToLower().Contains(lower)` (EF parameterized) + placeholder `Cari nama atau kategori assessment...`. Nama/NIP TIDAK (aggregate).
- **MAP-13:** standard `TotalCount = g.Count(a => a.Status != AssessmentConstants.AssessmentStatus.Cancelled)` + `CancelledCount` parity (sebelumnya MISSING); Pre-Post `(postSubs.Count>0 ? postSubs : preSubs).Count(a => a.Status != Cancelled)` (apply ke sub list, bukan g — Pitfall 6). Progress bar bisa 100%.
- **MAP-15:** Cabang `else if (string.IsNullOrEmpty(status) && !string.IsNullOrEmpty(search))` → `status = "All"` → dropdown jujur "Semua Status" saat search broaden scope (Closed muncul). Tak ubah hasil filter (CIL-02 preserved).

## Deviations from Plan

**[Rule 1 — DRY] MAP-17 implementasi via drop-gate, bukan else-branch** — Found during: Task 2 | Plan mencontohkan tambah dropdown Pre-Post; konten dropdown identik dengan standard (View Detail + Regenerate gated). Pilih drop gate `@if (!group.IsPrePostGroup)` sehingga 1 code path render untuk semua grup (DRY, no duplikasi). detailUrl + RepresentativeId sudah benar untuk Pre-Post (PreTest detail + LinkedGroupId rep). | Files: `AssessmentMonitoring.cshtml` | Verification: grep btn-regenerate-token=2 (1 markup + 1 JS), gate `!IsPrePostGroup`=0 | Commit `845c51fa`.

**[Scope] Literal "Cancelled" sub-row dibiarkan** — sub-row `preSubs/postSubs CancelledCount` masih literal "Cancelled" (pre-existing, BUKAN kode MAP-13 TotalCount baru). Tak dikonversi untuk hindari scope creep; acceptance "no NEW literal di MAP-13" terpenuhi (kode baru pakai konstanta).

**Total deviations:** 2 (1 DRY improvement, 1 scope boundary). **Impact:** none — semua acceptance PASS, build 0 error.

## Verification

- `dotnet build HcPortal.csproj -c Debug` → **0 Error**
- Grep T1: "assessment real-time"=0, muted-kategori-dobel=0, placeholder "Cari nama atau kategori assessment..."=1
- Grep T2: btn-regenerate-token=2 (markup+JS), gate `if (!group.IsPrePostGroup)`=0
- Grep T3: `Category.ToLower().Contains(lower)`=1, standard exclude-Cancelled konstanta=1, Pre-Post exclude=1, `status = "All"`=1, reshuffleWorker intact=1 (tidak disentuh, Pitfall 2)
- Logic-bearing MAP-13/MAP-23 → unit test di Plan 05 (`ManageAssessmentLowPolishTests.cs`)
- Phase gate (Plan 05): Playwright browser-verify MAP-13 progress 100%, MAP-17 Regenerate Pre-Post, MAP-23 search kategori, MAP-15 dropdown "Semua Status" saat search Closed

## Self-Check: PASSED

- key-files modified exist on disk ✓
- `git log --grep="349-03"` → 2 commits ✓
- All `<acceptance_criteria>` re-verified PASS ✓
- build 0 error ✓

Ready for Plan 349-04 (Monitoring Detail: i18n + 7-kartu summary MAP-10 + InProgressCount + Akhiri conditional).
