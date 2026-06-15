---
phase: 371-sesi-online-tampil-di-tab-input-records-visibility-only
verified: 2026-06-12T10:00:00+08:00
status: passed
score: 7/7
overrides_applied: 0
re_verification: false
---

# Phase 371: Sesi Online Tampil di Tab Input Records — Verification Report

**Phase Goal:** URG-03 — Sesi assessment online (IsManualEntry=false) tampil di tab Input Records per worker dengan badge pembeda "Assessment Online" — visibility-only, aksi hapus tetap scope Phase 367.
**Verified:** 2026-06-12T10:00:00+08:00
**Status:** passed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Expand worker (Rino GAST) menampilkan sesi online termasuk >7 hari | VERIFIED | `onlineRows` projection di baris 300-302: `.Where(a => !a.IsManualEntry)` tanpa filter umur; `allRows = trainingRows.Concat(assessmentRows).Concat(onlineRows)` baris 303. UAT SC1 PASS (sesi 2025, 2024 muncul). |
| 2 | Setiap row online punya badge tipe "Assessment Online" | VERIFIED | Baris 349-352: `else if (row.Type == "AssessmentOnline") { <span class="badge bg-secondary">Assessment Online</span> }`. grep: `Assessment Online`=1 occurrence, `AssessmentOnline`=2 occurrences (proyeksi + badge branch). |
| 3 | Badge status online 6-way benar (Lulus/Tidak Lulus/Menunggu Penilaian/Sedang Dikerjakan/Dibatalkan+Abandoned/Belum Mulai) | VERIFIED | Local function `OnlineLabel` baris 266-274 meniru `DeriveUserStatus` dengan urutan cek kritis PendingGrading pertama. `OnlineClass` baris 275-284 memetakan ke bg-success/danger/warning/primary/dark/secondary. Semua 6 label terverifikasi di kode: grep match untuk tiap label. |
| 4 | Row manual & training existing render tak berubah; tombol edit/hapus manual tetap | VERIFIED | Branch `else if (row.Type == "Training")` dan `else` (Assessment Manual) baris 380-409 dipertahankan verbatim dengan `EditTraining`, `DeleteTraining`, `EditManualAssessment`, `DeleteManualAssessment` + antiToken + hx-vals + hx-confirm. grep: `DeleteTraining`=1, `DeleteManualAssessment`=1. |
| 5 | Row online TANPA tombol hapus/edit | VERIFIED | Branch `@if (row.IsOnline)` baris 368-378: HANYA render tombol "Lihat hasil" bila `row.CanViewResult`, tidak ada tombol Edit/Hapus. Komentar desain eksplisit "NO Edit, NO Hapus (placeholder Phase 367 — extension point cascade delete)". |
| 6 | Tombol "Lihat hasil" hanya untuk Completed/Menunggu Penilaian, link ke CMP/Results | VERIFIED | `CanViewResult = (a.Status == AssessmentConstants.AssessmentStatus.PendingGrading \|\| a.CompletedAt != null)` baris 302. Render: `@if (row.CanViewResult) { <a href="@Url.Action("Results", "CMP", new { id = row.Id })">` baris 372-377. grep: `Url.Action("Results", "CMP"`=1. UAT SC2 PASS (Lihat hasil Completed -> /CMP/Results/126 load penuh). |
| 7 | Empty-state copy = "Belum ada record untuk pekerja ini." (tanpa kata "manual") | VERIFIED | Baris 308: `Belum ada record untuk pekerja ini.`. grep: match=1. grep "Belum ada record manual"=0. UAT SC3 PASS. |

**Score:** 7/7 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Views/Admin/Shared/_TrainingRecordsTab.cshtml` | onlineRows projection + 3-way badge Tipe + online status derivation + Lihat hasil branch + empty-state copy | VERIFIED | File dimodifikasi di commit `d1d03e13`. 74 baris perubahan (+57/-17). Semua 5 edit hadir: proyeksi anon 10-prop, empty-state copy, badge 3-way, StatusClass dari proyeksi, kolom Aksi branch online. Build 0 error 0 warning. |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `_TrainingRecordsTab.cshtml` (onlineRows) | `worker.AssessmentSessions` | `.Where(a => !a.IsManualEntry).Select(...)` | WIRED | Baris 300-302. grep: `!a\.IsManualEntry`=1 match. |
| `_TrainingRecordsTab.cshtml` (allRows) | `trainingRows + assessmentRows + onlineRows` | `.Concat()` rantai identik | WIRED | Baris 303: `trainingRows.Concat(assessmentRows).Concat(onlineRows)`. grep -o `\.Concat(`=2 occurrences. Shape anon 10-prop identik di ketiganya (dikonfirmasi code review). |
| `_TrainingRecordsTab.cshtml` (tombol Lihat hasil) | `CMP/Results` | `Url.Action("Results", "CMP", new { id = row.Id })` | WIRED | Baris 374. grep: `Url\.Action\("Results", "CMP"`=1. Server-side auth via `IsResultsAuthorized` + `IsAssessmentSubmitted` di CMPController (tidak berubah). |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|--------------|--------|-------------------|--------|
| `_TrainingRecordsTab.cshtml` | `worker.AssessmentSessions` | `WorkerTrainingStatus` model dari service layer (tidak diubah Phase 371) | Ya — service load per worker dari DB existing | FLOWING |
| `_TrainingRecordsTab.cshtml` | `onlineRows` | `.Where(a => !a.IsManualEntry)` dari `worker.AssessmentSessions` | Ya — filter dari koleksi real (bukan hardcoded empty); 55 sesi online tersedia di DB lokal | FLOWING |

Catatan: Phase 371 adalah view-only change — tidak menyentuh service/controller. Data source `worker.AssessmentSessions` sudah ada dan terisi dari fasa sebelumnya. Perubahan hanya melonggarkan filter render dari `IsManualEntry == true` ke menambah proyeksi `!IsManualEntry`.

### Behavioral Spot-Checks

| Behavior | Command / Evidence | Result | Status |
|----------|-------------------|--------|--------|
| Build kompilasi sukses (shape anon `.Concat()` valid) | `dotnet build` = 0 error 0 warning (tercatat di SUMMARY) | 0 error, 0 warning | PASS |
| Suite regresi hijau | `dotnet test HcPortal.Tests` = 226/226 baseline pass (tercatat di SUMMARY; 2 failure di `AssessmentWindowRemovalTests.cs` = WIP sesi paralel 370-secure, independen) | 226/226 pass | PASS |
| Badge "Assessment Online" ada di file | `grep -c "Assessment Online" Views/Admin/Shared/_TrainingRecordsTab.cshtml` = 1 | 1 | PASS |
| onlineRows filter hadir | `grep "!a.IsManualEntry"` = 1 match | 1 | PASS |
| Rantai Concat 3-operand | `grep -o ".Concat(" \| wc -l` = 2 | 2 | PASS |
| Empty-state copy baru | `grep "Belum ada record untuk pekerja ini."` = 1, `grep "Belum ada record manual"` = 0 | PASS | PASS |
| UAT @5277 SC1: row online Rino GAST + badge + status 6-way + >7 hari + no 500 | Playwright MCP inline, user approved | 3/3 SC PASS | PASS |
| UAT @5277 SC2: manual tetap + online read-only + Lihat hasil gated | Playwright MCP inline, user approved | PASS | PASS |
| UAT @5277 SC3: empty-state copy baru | Playwright MCP inline, user approved | PASS | PASS |

Step 7b spot-checks dilakukan via SUMMARY evidence (build + test + UAT Playwright) — tidak ada server yang perlu distart untuk pemeriksaan statis; behavioral checks already executed as part of Task 2 checkpoint.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|---------|
| URG-03 | 371-01-PLAN.md | Sesi assessment online (IsManualEntry=false) tampil di tab Input Records per worker dengan badge pembeda "Assessment Online" — visibility-only, aksi hapus tetap scope Phase 367 | SATISFIED | `onlineRows` projection + badge "Assessment Online" + 6-way status + Lihat hasil + read-only branch, semua terverifikasi di kode aktual. UAT 3/3 SC PASS. REQUIREMENTS.md baris 45: `- [ ] **URG-03**: ...` (checkbox belum dicentang di file requirements — perlu diupdate, tapi bukan blocker verifikasi). |

Tidak ada requirement ID orphan. REQUIREMENTS.md §Traceability baris 67: `URG-03 | 371 (v26.0)` — mapping 1:1 dengan plan frontmatter.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `_TrainingRecordsTab.cshtml` | 147 | `placeholder="Cari nama atau nopeg..."` | Info | HTML input placeholder attribute — bukan anti-pattern kode, UI label form pencarian. Tidak terkait perubahan Phase 371. |
| `_TrainingRecordsTab.cshtml` | 371 | `placeholder Phase 367` dalam komentar Razor | Info | Komentar desain eksplisit menandai extension point untuk Phase 367 delete cascade. Bukan empty implementation — branch online sengaja read-only (scope-by-design). |

Tidak ada anti-pattern Blocker atau Warning. Dua temuan Info di atas bukan indikasi stub:
- Baris 147: input placeholder adalah atribut HTML standar.
- Baris 371: komentar arsitektur yang menjelaskan mengapa no-action adalah keputusan desain, bukan incomplete implementation. Branch `@if (row.IsOnline)` berfungsi penuh untuk kasus Completed/PendingGrading (tombol Lihat hasil); kosong untuk status lain adalah perilaku yang disengaja sesuai spec.

### Human Verification Required

UAT @5277 sudah dilaksanakan sebagai Task 2 checkpoint blocking via Playwright MCP inline oleh orchestrator, dengan persetujuan user. 3/3 SC PASS:

- SC1: row online Rino NIP 29007720 GAST tampil (termasuk sesi 17 Feb 2025, 10 Mar 2024, 15 Jan 2024 yang >7 hari); badge "Assessment Online" bg-secondary; 6-way status visual benar; Upcoming/Open -> "Belum Mulai"; no HTTP 500/RuntimeBinderException.
- SC2: row manual/training + Edit/Hapus tetap; online tanpa Edit/Hapus; Lihat hasil hanya Completed -> `/CMP/Results/126` load halaman hasil penuh (85% LULUS, Peserta Rino); online belum-selesai -> Aksi kosong.
- SC3: expand Choirul Anam (0 record) -> "Belum ada record untuk pekerja ini." (tanpa "manual"); tombol Tambah tetap.

Catatan SUMMARY: status "Menunggu Penilaian" tidak ada sesi essay-pending di DB lokal untuk visual-confirm, tetapi logic PendingGrading dicek pertama di `OnlineLabel` (urutan kritis sesuai spec) dan `CanViewResult` mencakupinya. Bukan blocker.

**Tidak ada item tersisa yang membutuhkan human verification lebih lanjut.**

### Gaps Summary

Tidak ada gap. Semua 7 truths verified, semua artifacts hadir dan substantif, semua key links wired, data flows dari source ke render, no anti-pattern blockers, UAT sudah dilakukan dan user approved.

---

_Verified: 2026-06-12T10:00:00+08:00_
_Verifier: Claude (gsd-verifier)_
