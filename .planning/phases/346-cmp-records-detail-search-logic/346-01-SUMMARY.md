---
phase: 346-cmp-records-detail-search-logic
plan: 01
subsystem: CMP/Records (My Records tab)
tags: [view, razor, bootstrap-modal, discoverability]
requires: []
provides: ["My Records Aksi column", "trainingDetailModal", "Lihat Hasil/Detail buttons"]
affects: ["Views/CMP/Records.cshtml"]
tech-stack:
  added: []
  patterns: ["data-* attribute modal (show.bs.modal + dataset)", "btn-outline action affordance"]
key-files:
  created: []
  modified: ["Views/CMP/Records.cshtml"]
key-decisions:
  - "Reuse existing resultsUrl var (L162) for Lihat Hasil link instead of re-inlining Url.Action — DRY, behaviorally identical"
  - "D-02: modal shows BOTH ValidUntil and CertificateType (not either/or)"
requirements-completed: [REC-01, REC-02]
duration: 8 min
completed: 2026-06-04
---

# Phase 346 Plan 01: My Records — Kolom Aksi + Modal Training Summary

Menambah kolom "Aksi" (ke-7) di tabel My Records `/CMP/Records` dengan tombol eksplisit per-baris: Assessment → `Lihat Hasil` (link ke `/CMP/Results`), Training → `Detail` (buka `trainingDetailModal`). Modal di-port dari `RecordsWorkerDetail.cshtml` dengan 11 field flat + tombol PDF (toggle `SertifikatUrl`). Row tetap clickable (D-03 — keyboard-nav handler L439 tak disentuh). Tanpa perubahan controller/service (semua data sudah di `UnifiedTrainingRecord`).

**Tasks:** 2 | **Files:** 1 | **Commits:** 2 (`fd89058f` Task 1, `5b22eae3` Task 2)

## What was built

- **Task 1** (`fd89058f`): thead `<th>Aksi</th>` (kolom ke-7) + per-row `<td>` Aksi (reuse `resultsUrl` var → `Lihat Hasil` btn-outline-primary `bi-bar-chart-line` untuk assessment; `Detail` btn-outline-info `bi-info-circle` + 12 `data-*` attr untuk training). Fix colspan empty-state 6→7 di 2 lokasi (Razor L227 + JS-injected L406).
- **Task 2** (`5b22eae3`): `#trainingDetailModal` (11-field `<dl>`: Nama/Penyelenggara/Kota/TglMulai/TglSelesai/NomorSertifikat/Kategori/SubKategori/Status/ValidUntil/CertificateType) + PDF toggle (`mdPdfWrap`/`mdPdfLink rel=noopener target=_blank`) + JS `show.bs.modal` handler baca `event.relatedTarget.dataset.*` (camelCase).

## Verification

- `dotnet build` → Build succeeded, 0 Error (21 warnings pre-existing).
- grep: `<th>Aksi</th>` ✓ · `Url.Action("Results","CMP"` ✓ · `data-bs-target="#trainingDetailModal"` ✓ · NO `colspan="6"`, 2× `colspan="7"` ✓ · modal div + 13 `md*` ids (11 dd + mdPdfWrap + mdPdfLink) ✓ · `show.bs.modal`+`event.relatedTarget` ✓.
- RecordsTeam partial (Team tab, same page) verified NO `trainingDetailModal` → no duplicate-id risk.

## Deviations from Plan

**[Rule 1 - DRY improvement] Reuse resultsUrl var** — Found during Task 1. Plan action inlined `Url.Action("Results","CMP",new{id=item.AssessmentSessionId.Value})` in the Aksi cell; instead reused the existing `resultsUrl` var (computed L162) via `@if (resultsUrl != null)` / `<a href="@resultsUrl">`. Behaviorally identical (resultsUrl non-null ⟺ assessment+HasValue), satisfies key_links + acceptance grep (Url.Action present L163). Single-source, no recompute.

**Total deviations:** 1 (DRY, no behavior change). **Impact:** none — acceptance criteria all pass.

## Self-Check: PASSED

- key-files modified exist ✓ · 2 commits present (`git log --grep="346-01"`) ✓ · all acceptance_criteria re-run PASS ✓ · build green ✓.

## Notes

- Visual UAT (klik tombol → modal terisi / Results) ditunda ke Plan 346-06 (Playwright). Build-gate only di plan ini.
- Ready for 346-02 (independent file, Wave 1).
