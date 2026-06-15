---
phase: 357-standarisasi-istilah-tipe-soal
plan: 03
subsystem: question-type-labels
tags: [label, docs, guide]
requires: []
provides: [docs-relabel]
affects: [Services/GuideContentProvider.cs, wwwroot/documents/guides, wwwroot/documents/TKI]
tech-stack:
  added: []
  patterns: [context-aware-replace]
key-files:
  created: []
  modified:
    - Services/GuideContentProvider.cs
    - wwwroot/documents/guides/Struktur-Website-PortalHC-KPB.html
    - wwwroot/documents/guides/Release-Notes-HC-Portal-KPB.html
    - wwwroot/documents/guides/Penjelasan-Halaman-PortalHC-KPB.html
    - wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html
    - wwwroot/documents/guides/Panduan-Lengkap-Assessment.html
    - wwwroot/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html
    - wwwroot/documents/TKI/generate_bab_x.py
    - wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA.html
    - wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md
key-decisions:
  - "Context-aware per-occurrence replace (bukan blind sed); MA preserved, no double-replace Multiple Answer"
  - "TKI HTML diedit manual (sync dgn .py) bukan regen"
  - "PDF panduan defer manual user (Phase 305 D-14)"
requirements-completed: [LBL-02]
duration: ~18 min
completed: 2026-06-09
---

# Phase 357 Plan 03: Grup D Docs Relabel Summary

Istilah tipe soal di dokumentasi served + konten guide in-code direword context-aware: "Single Choice"→"Single Answer", "Multiple Answers"→"Multiple Answer", "Multiple Choice"→"Single Answer" (istilah lama pra-305), abbrev "MC"→"SA". "MA"/"Multiple Answer" yang sudah benar dipertahankan (no double-replace).

## Tasks
- **Task 1** (`76828e11`): GuideContentProvider.cs — Title/Step(1)/body/Keywords (build 0 error; "Multiple Choice"=0, "Single Answer (SA)"=1, "Multiple Answer (MA)" intact).
- **Task 2** (`c4a442ca`): 6 guide HTML — 19 occurrence context-aware; residual "Single Choice"/"Multiple Answers"/"Multiple Choice" = 0 di semua 6.
- **Task 3** (`70ff9195`): TKI BAB-X (py + outline + html) — residual 0; python clean-check pass; .docx/.pdf di-defer.

## Verification
- `dotnet build` 0 error (GuideContentProvider.cs).
- grep residual old terms = 0 di GuideContentProvider + 6 guide HTML + 3 TKI.
- MA / "Multiple Answer" valid tetap; .docx/.pdf binary tak tersentuh.

## Deviations from Plan
**[Discretion]** TKI HTML diedit manual (3 baris) alih-alih regen via .py — lebih reliable tanpa resolusi path runtime; .py source juga diupdate agar regen masa depan konsisten. Sesuai CONTEXT.md Discretion.

## Issues Encountered
None.

## Next Phase Readiness
Grup D selesai. Semua kode+docs relabel done (01/02/03). Ready 357-04 (gate + Playwright UAT 5 surface + human-verify).
