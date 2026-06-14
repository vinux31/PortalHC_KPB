---
phase: 363-audit-fix-alur-proton-temuan-verifikasi-t1-t10
plan: 06
subsystem: proton-histori-evidence
tags: [cdp, histori-proton, export, evidence-history, belum-mulai]
requires:
  - "363-02: blok resubmit SubmitEvidenceWithCoaching sudah HC-reset (blok berbeda)"
provides:
  - "Status 'Belum Mulai' reachable di HistoriProton + ExportHistoriProton (role-scoped)"
  - "AppendEvidencePathHistory shared — kedua jalur evidence append sebelum overwrite"
affects: [363-07]
tech-stack:
  added: []
  patterns: ["shared private helper untuk list+export (anti-drift Pitfall 5)"]
key-files:
  created:
    - HcPortal.Tests/ProtonHistoriAndEvidenceTests.cs
  modified:
    - Controllers/CDPController.cs
key-decisions:
  - "T5 pakai pendekatan SHARED HELPER (BuildBelumMulaiRowsAsync) bukan mirror verbatim — sefilosofi D-01, drift list-vs-export mati struktural"
  - "AppendEvidencePathHistory public static (bukan internal per plan) — proyek tanpa InternalsVisibleTo, konsisten deviasi 363-01"
  - "Filter status export: tidak perlu kode khusus — filter generik `w.Status == status` otomatis menerima 'Belum Mulai'"
requirements-completed: [T5, T8]
duration: 13 min
completed: 2026-06-11
---

# Phase 363 Plan 06: 'Belum Mulai' + Evidence History Parity Summary

Badge/filter "Belum Mulai" yang selama ini unreachable kini hidup: `BuildBelumMulaiRowsAsync` (satu helper, dipanggil HistoriProton `:3302` DAN ExportHistoriProton `:3455`) menambah baris untuk coachee ber-mapping aktif tanpa assignment; `SubmitEvidenceWithCoaching` kini append `EvidencePath` lama ke `EvidencePathHistory` sebelum overwrite via `AppendEvidencePathHistory` shared (UploadEvidence di-refactor pakai helper sama).

- Duration: 13 min | Tasks: 3/3 | Files: 2

## Task Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1+2 | `a9fc8961` | feat(363-06): baris 'Belum Mulai' + shared evidence-history append |
| 3 | `e1a10e94` | test(363-06): append-history + belum-mulai set computation |

## What Was Built

- **T5**: helper `BuildBelumMulaiRowsAsync(userLevel, user, coacheeIdsWithAssignments)` — role-scoping 3 cabang (≤3 semua mapping aktif; 4 join Section; 5 mapping milik coach), `Except` assignment, baris semua flag Tahun false + `Status="Belum Mulai"` + `Jalur=""`. Ditambahkan SEBELUM sort Nama di kedua method. View TIDAK diubah.
- **T8**: `AppendEvidencePathHistory` (public static, no-op saat path kosong) — dipanggil UploadEvidence `:1342` (refactor inline) + SubmitEvidenceWithCoaching `:2320` (baru, sebelum overwrite `:2322`). Tanpa File.Delete (E10).
- **Tests**: 3 fact — append kumulatif, no-op empty, set computation (predikat-replikasi). 3/3 PASS.

## Deviations from Plan

**[Rule 3 - Blocker] AppendEvidencePathHistory `public static` alih-alih `internal static`** — Found during: Task 2 | Issue: tanpa InternalsVisibleTo test tak bisa panggil internal | Fix: public static (konsisten deviasi 363-01) | Verification: build 0 err + 3/3 test | Commit: a9fc8961

**Total deviations:** 1 auto-fixed. **Impact:** none.

## Verification

- `AppendEvidencePathHistory` 3 lokasi (def + 2 call, call SubmitEvidence sebelum overwrite); `BuildBelumMulaiRowsAsync` def + 2 call; `git diff` view kosong; tidak ada File.Delete baru.
- `dotnet test --filter ProtonHistoriAndEvidence` → 3/3 PASS.
- UAT visual (list/filter/export + DB check history) → Plan 07.

## Self-Check: PASSED

## Next

Ready for 363-07 (gate: full test suite + UAT live @5277 — plan checkpoint `autonomous: false`).
