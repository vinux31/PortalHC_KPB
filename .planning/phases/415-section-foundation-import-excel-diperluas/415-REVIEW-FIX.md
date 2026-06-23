---
phase: 415
title: Section Foundation + Import Excel Diperluas
report: code-review-fix
status: all_fixed
findings_in_scope: 11
fixed: 11
skipped: 4
iteration: 1
applied_by: user (working-tree, pre-commit) — orchestrator AUDITED + VERIFIED, did not author
committed: false
verification:
  build: "0 error (dotnet build HcPortal.csproj)"
  section_suites: "22/22 passed (SectionCrud + SectionImport + SectionSyncPrePost + SectionMismatchGuard)"
  full_suite: "633/633 passed (no regression)"
re_check_round_2:
  trigger: "adversarial re-verify of applied fixes — 8 problems surfaced, 3 distinct root-causes"
  fixed: 3
  items: "H4 (sync destructive/unguarded), H3 (re-approach: reject >4-opt edit instead of delete E/F), L2 (filter unique-index + log)"
  re_verify: "build 0-error + Section 22/22 + full 633/633"
date: 2026-06-23
---

# Phase 415 Code-Review-Fix Report

**Penting:** fix di-terapkan oleh user langsung di working tree (BELUM commit), bukan oleh `gsd-code-fixer`.
Orchestrator meng-AUDIT tiap fix vs temuan `415-REVIEW.md`, lalu memverifikasi build + test. Tidak ada kode yang
di-author/commit oleh orchestrator (changes masih uncommitted — keputusan commit di tangan user).

## Fixed (11/11 in-scope) — semua AUDITED benar

| ID | Sev | Lokasi | Fix diterapkan | Verdict audit |
|----|-----|--------|----------------|---------------|
| H1 | HIGH | `CMPController.cs:1078`, `AssessmentAdminController.cs:7194` | Guard StartExam + validasi import pakai `SiblingSessionQuery.SiblingPrePostAwarePredicate(..., AssessmentType)` (IDENTIK jalur assignment) | ✅ benar |
| M4 | MED | `Helpers/SectionStructureComparer.cs` (baru) | Helper `MismatchedSections` + `SectionLabel` shared; dipakai di re-guard CMP & validasi import | ✅ bersih, 1 sumber kebenaran |
| H4 | HIGH | helper `SyncToPostIfSamePackageAsync` @`:6665`; dipanggil CreateSection/EditSection/DeleteSection/SetAllSectionsNewPage + UploadPackageQuestions | Mirror persis pola inline `CreateQuestion:7790` (PreTest+LinkedSessionId+SamePackage→SyncPackagesToPost) | ✅ parity terbukti |
| H2 | HIGH | `AssessmentAdminController.cs` RowIsValid `:7166` + build loop `:7387` | `ParseCorrectLetters` + `CorrectLettersMapToFilledOptions` (≥1 benar DAN tiap huruf benar→opsi non-kosong); tolak baris ungradeable | ✅ kedua jalur (count-validate + insert) konsisten |
| M1 | MED | `AssessmentAdminController.cs:7230` | Count incoming dihitung pasca-dedup fingerprint (vs existing + dalam-batch) | ✅ benar |
| M3 | MED | `AssessmentAdminController.cs:7240` | Baseline = soal existing paket (grouped-by-section) + incoming deduped; nav `q.Section` ter-load (`:6994`, `:7222`) | ✅ tak misgroup |
| M2 | MED | `AssessmentAdminController.cs:7044` | `isNewFormat` butuh `colCount>9` DAN nama header cocok ("opsi e"/"opsi f"/"no. section"/"nama section"); string match generator `:6880` | ✅ happy-path aman |
| H3 | HIGH | `AssessmentAdminController.cs:7912` | **Re-approach (Round 2):** tolak edit soal >4 opsi di awal EditQuestion (preserve data); blok `Skip(4)` lama dihapus. Edit penuh A–F → Phase 418 | ✅ (lihat Round 2) |
| L2 | LOW | `AssessmentAdminController.cs:6298/6354` | **Hardened (Round 2):** filter `when (2601/2627)` + `_logger.LogWarning`; DbUpdateException lain propagate | ✅ (lihat Round 2) |
| H4 | HIGH | helper `SyncToPostIfSamePackageAsync` `:6674` | **Hardened (Round 2):** skip-if-Post-taken + best-effort try/catch+log; inline pre-existing (Create/Edit/DeleteQuestion) dialihkan ke helper | ✅ (lihat Round 2) |
| L3 | LOW | `AssessmentAdminController.cs:7262` | Cabang legacy banding vs SETIAP sibling (bukan First) | ✅ |
| L6 | LOW | `ManagePackageQuestions.cshtml:266` | Header grouped-list pakai `sectionLabel(s)` konsisten dropdown | ✅ |

## Deferred (4) — sengaja TIDAK di-fix (keputusan desain)

| ID | Sev | Alasan defer |
|----|-----|--------------|
| M5 | MED | Re-guard hanya jalur first-build (`assignment==null`); resume lewat. Ada safety-net `SavedQuestionCount` (`CMPController.cs:1179`); tak re-blok worker mid-exam = defensible. **Butuh keputusan user**, bukan auto-fix. |
| L1 | LOW | `FindOrCreateSection` tak backfill Nama section existing — komentar kode bilang disengaja. **Konfirmasi user**: apakah re-label via Excel memang no-op? |
| L4 | LOW | Tak ada validasi sibling-vs-sibling. PLAUSIBLE; runtime StartExam guard sudah jaring. Low value. |
| L5 | LOW | View re-derive grouping O(n²). Refactor → risiko regresi tampilan. Defer. |

## Verifikasi (gate CLAUDE.md — verify lokal sebelum commit)

```
dotnet build HcPortal.csproj    → Build succeeded. 0 Error (warning pre-existing: view nullable + int? Dictionary M1/M3)
dotnet test (Section filters)   → 22/22 Passed (SectionCrud/Import/SyncPrePost/MismatchGuard)
dotnet test (full)              → 633/633 Passed (0 fail, 0 skip) — no regresi
```

---

## Round 2 — Adversarial re-verify (3 gap DI DALAM fix → di-fix)

Re-verifikasi adversarial atas fix yang diterapkan menemukan **8 problem / 3 akar** (lolos build+suite karena belum ada test yang cover perilaku-fix). Ketiganya kini DI-FIX:

| Gap | Sev | Masalah | Fix Round 2 |
|-----|-----|---------|-------------|
| **H4** | MED | `SyncPackagesToPost` `RemoveRange`(Post.Questions) → kalau Post sudah dikerjakan (`PackageUserResponse`, FK Restrict) → `DbUpdateException`/500; H4 perlebar ke 5 surface; juga unwrapped pasca-commit → gagal transien = Pre commit + 500 + Post basi | Helper `SyncToPostIfSamePackageAsync` (`:6674`) di-hardening: **skip-if-Post-punya-response** (`AnyAsync r.AssessmentSessionId==linkedPost.Id`) + **try/catch best-effort + log** (sync gagal tak men-500-kan Pre). Inline pre-existing CreateQuestion/EditQuestion/DeleteQuestion dialihkan ke helper (D: "sekalian inline") |
| **H3** | MED | Hapus E/F di SETIAP save (data-loss senyap walau cuma edit typo) + gate `correctCount` (A-D) hard-block soal benar=E | **Ganti approach (D: tolak+preserve):** di awal EditQuestion non-essay, `q.Options.Count>4` → tolak dgn pesan jelas, return sebelum mutasi. Blok `Skip(4)` lama dihapus. Data utuh. Edit penuh A–F → Phase 418 |
| **L2** | LOW | `catch(DbUpdateException)` polos telan SEMUA → pesan "No. Section sudah ada" menyesatkan + no log | Filter `when (inner 2601/2627)` + `_logger.LogWarning`; DbUpdateException lain propagate (mirror `CoachMappingController:645`) |

Re-verify Round 2: **build 0-error + Section 22/22 + full 633/633**.

Inti correctness lain (H1/M4/H2/M1/M3/M2/L3/L6) re-check **0 masalah** — solid.

---

## Rounds 3–5 — Adversarial loop-until-dry (KONVERGEN DRY)

Re-check berulang; tiap ronde nemu bug DI DALAM fix ronde sebelumnya sampai 1 ronde bersih.

- **Validate-phase (escalation):** `Dictionary<int?,int>` tolak key null → import legacy/"Lainnya" + sibling = `ArgumentNullException` 500 (langgar keystone, laten sejak 415-03). **FIX:** sentinel `SectionStructureComparer.KeyOf`/`LainnyaKey`. +null-safe tests.
- **Round 3 (3 fix + 1 backlog):** (1) **H4 skip-if-taken cuma sebagian** — 3 call-site paket (`CopyPackagesFromPre`/`CreatePackage`/`DeletePackage`) panggil `SyncPackagesToPost` mentah → tetap 500 → **FIX: guard dipindah ke DALAM `SyncPackagesToPost` (di sumber, lindungi semua caller)**. (2) **L2 filter MATI** — `Message.Contains("2601"/"2627")` tak pernah cocok (SQL taruh angka di `.Number`; message isi nama-index) → **FIX: tambah klausa `IX_AssessmentPackageSections_AssessmentPackageId_SectionNumber`**. (3) **H3 bypass via konversi Essay** → **FIX: guard jadi `q.Options.Count>4` (tipe apa pun)**. (4) **EditQuestion hapus opsi soal sudah-dijawab → FK 500** (pre-existing) → **backlog `999.14`**.
- **Round 4 (1 fix):** **`CopyPackagesFromPre` sukses-palsu** — source skip-if-taken (no-op) tapi tombol eksplisit "Salin dari Pre" tetap banner sukses → HC tertipu, Post stale → **FIX: pre-check → Error jelas**.
- **Round 5: DRY** ✅ — 0 temuan 415-introduced. Loop konvergen.

Re-verify akhir: **build 0-error + full suite 655/655** (22 regression test `SectionFixRegressionTests.cs`).

Backlog dari loop: **`999.13`** (M5 resume-guard), **`999.14`** (EditQuestion option-delete FK 500, pre-existing).

**Lesson:** re-check adversarial atas fix nangkap bug DI DALAM fix yang build+green-suite lewatkan (perilaku-fix tak ter-test); loop-until-dry perlu sampai 1 ronde bersih — fix bisa beranak (R3 source-guard → R4 false-success). Bug paling serius (L2 dead-filter, null-key keystone) hanya muncul di ronde lanjutan + di-warning compiler (CS8714).

---

## ⚠️ Caveat & rekomendasi tindak lanjut

1. **H3 keterbatasan (bukan lagi data-loss):** soal hasil import 5–6 opsi (E/F) **belum bisa diedit** lewat form (ditolak dgn pesan, data dipertahankan). Untuk ubah: hapus + impor ulang. Edit penuh A–F = Phase 418.

2. **Gap test regresi (MASIH ADA):** semua fix LULUS build + suite existing TANPA test gagal (no regresi), TAPI **belum ada test BARU yang mengunci perilaku-fix** (H1 Pre/Post tak campur, H2 tolak blank-E-correct, M1/M3 baseline count, H3 tolak >4-opt, H4 skip-if-taken, L2 filter). **Rekomendasi: `/gsd-validate-phase 415`** atau `/gsd-add-tests 415`.

3. **Belum di-commit:** 4 file berubah (`AssessmentAdminController.cs`, `CMPController.cs`, `ManagePackageQuestions.cshtml`) + 1 baru (`Helpers/SectionStructureComparer.cs`). Commit per-grup atomik direkomendasikan: `fix(415): H1+M4 Pre/Post-aware guard + shared comparer`, `fix(415): H4 SamePackage post-sync hardened (skip-if-taken + best-effort, 5 surfaces + inline)`, `fix(415): H2+M1+M3 import grading/count integrity`, `fix(415): H3 reject >4-opt edit + L2 filter + L3 + L6`.

4. **Terpisah dari review ini:** fase **415.1 hotfix** ("guard penilaian essay cross-package soal bukan milik") sudah di-plan (`ae418e9d`) — bukan bagian dari temuan ini.
