---
phase: 272-block-submit-jika-belum-semua-soal-terisi
verified: 2026-03-28T08:00:00Z
status: human_needed
score: 4/4 must-haves verified
human_verification:
  - test: "Buka ExamSummary saat ada soal belum dijawab — verifikasi tombol disabled dan pesan peringatan tampil"
    expected: "Tombol 'Kumpulkan Ujian' grayed out tidak bisa diklik, alert warning menampilkan 'Jawab semua soal terlebih dahulu sebelum mengumpulkan'"
    why_human: "Rendering kondisional disabled button bergantung pada nilai runtime ViewBag.UnansweredCount — tidak bisa diverifikasi tanpa browser"
  - test: "Buka ExamSummary saat semua soal terjawab — verifikasi tombol aktif"
    expected: "Tombol 'Kumpulkan Ujian' aktif dan bisa diklik, alert success 'Semua soal sudah dijawab'"
    why_human: "Perlu session ujian dengan semua jawaban terisi"
  - test: "Auto-submit saat waktu habis — verifikasi tetap berhasil meskipun ada soal kosong"
    expected: "Timer expired redirect ke ExamSummary dengan timerExpired=true, tombol submit aktif, form submit berhasil ke halaman hasil"
    why_human: "Memerlukan ujian aktif dengan timer yang berjalan sampai habis"
---

# Phase 272: Block Submit Jika Belum Semua Soal Terisi — Verification Report

**Phase Goal:** Block submit jika belum semua soal terisi — Cegah peserta submit ujian jika masih ada soal yang belum dijawab. Tombol submit di-disable dan validasi backend menolak submission yang tidak lengkap. Pengecualian: auto-submit saat waktu habis tetap dibolehkan.
**Verified:** 2026-03-28T08:00:00Z
**Status:** human_needed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Tombol Kumpulkan Ujian disabled jika ada soal belum dijawab | VERIFIED | `ExamSummary.cshtml` line 103–108: `@if (unanswered > 0 && !timerExpired)` → `<button type="button" ... disabled title="Jawab semua soal terlebih dahulu">` |
| 2 | Pesan warning muncul saat tombol disabled | VERIFIED | `ExamSummary.cshtml` line 23: "Jawab semua soal terlebih dahulu sebelum mengumpulkan" — teks diperbarui dari versi lama |
| 3 | Backend menolak submit manual jika ada soal kosong (return error redirect) | VERIFIED | `CMPController.cs` line 1369–1384: blok `// Block incomplete submission (Phase 272)` — cek `!isAutoSubmit`, hitung `answeredCount < totalQuestions`, TempData["Error"] + RedirectToAction("ExamSummary") |
| 4 | Auto-submit saat waktu habis tetap diterima meskipun ada soal kosong | VERIFIED | Guard dibungkus `if (!isAutoSubmit)` — jika `isAutoSubmit=true` (dari hidden field set via JS timer), validasi di-skip sepenuhnya |

**Score:** 4/4 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Views/CMP/ExamSummary.cshtml` | Disabled submit button when unanswered > 0 | VERIFIED | File ada, substantif (119 baris, logika kondisional disabled button, hidden field isAutoSubmit, teks pesan diperbarui), terhubung ke form POST SubmitExam |
| `Controllers/CMPController.cs` | Backend validation in SubmitExam | VERIFIED | File ada, substantif (blok Phase 272 ditemukan di line 1369–1384 dengan `isAutoSubmit`, `answeredCount`, `totalQuestions`, TempData error, redirect), terhubung ke endpoint `[HttpPost] SubmitExam` |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `Views/CMP/ExamSummary.cshtml` | `CMPController.cs SubmitExam` | `form POST + hidden field isAutoSubmit` | WIRED | Line 81: `<form asp-action="SubmitExam" method="post">`, line 83: `<input type="hidden" name="isAutoSubmit" value="false" id="autoSubmitFlag" />` — form POST dengan parameter `isAutoSubmit` tersedia di backend signature (`bool isAutoSubmit = false`) |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `ExamSummary.cshtml` | `unanswered` (ViewBag.UnansweredCount) | `ExamSummary` action line 1322: `int unansweredCount = summaryItems.Count(s => !s.SelectedOptionId.HasValue)` | Ya — dihitung dari DB via `summaryItems` yang di-query dari `PackageUserResponses` | FLOWING |
| `ExamSummary.cshtml` | `timerExpired` (ViewBag.TimerExpired) | `ExamSummary` action line 1341: `ViewBag.TimerExpired = timerExpired` | Ya — dihitung dari `session.ExpiresAt` vs `DateTime.UtcNow` | FLOWING |
| `CMPController.cs SubmitExam` | `totalQuestions` | `pkgAssign.GetShuffledQuestionIds().Count` | Ya — dari DB UserPackageAssignments | FLOWING |
| `CMPController.cs SubmitExam` | `answeredCount` | `answers.Count(a => a.Value > 0)` | Ya — dari form POST dictionary | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| ExamSummary.cshtml mengandung `disabled` pada button | `grep -n "disabled" Views/CMP/ExamSummary.cshtml` | Line 105: `<button type="button" class="btn btn-success btn-lg fw-bold" disabled` | PASS |
| ExamSummary.cshtml mengandung `isAutoSubmit` hidden field | `grep -n "isAutoSubmit" Views/CMP/ExamSummary.cshtml` | Line 83: `<input type="hidden" name="isAutoSubmit" value="false" id="autoSubmitFlag" />` | PASS |
| ExamSummary.cshtml mengandung teks pesan peringatan baru | `grep -n "Jawab semua soal terlebih dahulu sebelum" Views/CMP/ExamSummary.cshtml` | Line 23: ditemukan | PASS |
| CMPController.cs signature SubmitExam mengandung `isAutoSubmit` | `grep -n "isAutoSubmit" Controllers/CMPController.cs` | Line 1347: `bool isAutoSubmit = false` di signature | PASS |
| CMPController.cs mengandung blok validasi completeness | `grep -n "Block incomplete" Controllers/CMPController.cs` | Line 1369: `// ---- Block incomplete submission (Phase 272) ----` | PASS |
| CMPController.cs mengandung `answeredCount < totalQuestions` | `grep -n "answeredCount < totalQuestions" Controllers/CMPController.cs` | Line 1378: ditemukan | PASS |
| CMPController.cs mengandung TempData["Error"] redirect | `grep -n "TempData\[.Error.\]" Controllers/CMPController.cs` | Line 1381: `TempData["Error"] = $"Masih ada {unanswered} soal..."` | PASS |
| Commit 364f9df4 valid di git log | `git show --stat 364f9df4` | feat(272-01): block submit jika belum semua soal terisi — 2 files changed | PASS |

---

### Requirements Coverage

REQUIREMENTS.md tidak mendefinisikan requirement ID dengan prefix `BLOCK-`. Requirement IDs `BLOCK-01`, `BLOCK-02`, `BLOCK-03` adalah internal phase (didefinisikan di CONTEXT.md decisions D-01 sampai D-08).

| Requirement | Source Plan | Deskripsi (dari CONTEXT.md decisions) | Status | Evidence |
|-------------|-------------|---------------------------------------|--------|----------|
| BLOCK-01 | 272-01-PLAN.md | Frontend: disable button Kumpulkan Ujian di ExamSummary jika `unanswered > 0` (D-01, D-02, D-03, D-04) | SATISFIED | ExamSummary.cshtml line 103–108: conditional disabled button |
| BLOCK-02 | 272-01-PLAN.md | Backend: SubmitExam menolak manual submit jika soal tidak lengkap, return error redirect (D-05, D-06) | SATISFIED | CMPController.cs line 1369–1384: blok validasi completeness |
| BLOCK-03 | 272-01-PLAN.md | Exception: auto-submit saat waktu habis tetap diterima (D-07, D-08) | SATISFIED | Guard `if (!isAutoSubmit)` — auto-submit dengan `isAutoSubmit=true` melewati validasi |

Catatan: Requirement IDs ini tidak terdaftar di `.planning/REQUIREMENTS.md` (file global tidak mengandung prefix `BLOCK-`). IDs bersifat phase-lokal dan tidak memerlukan registrasi global — tidak ada orphaned requirements.

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| Tidak ada | — | — | — | — |

Tidak ditemukan TODO/FIXME, placeholder, return kosong, atau stub pattern pada kedua file yang dimodifikasi.

---

### Human Verification Required

#### 1. Disabled Button saat Soal Belum Lengkap

**Test:** Login sebagai worker, mulai ujian assessment, jawab sebagian soal saja, klik "Tinjau Jawaban"
**Expected:** Alert warning "Jawab semua soal terlebih dahulu sebelum mengumpulkan" tampil, tombol "Kumpulkan Ujian" grayed out dan tidak bisa diklik
**Why human:** Rendering kondisional bergantung pada nilai runtime `ViewBag.UnansweredCount` — tidak bisa diverifikasi tanpa browser + session ujian aktif

#### 2. Tombol Aktif saat Semua Soal Terjawab

**Test:** Dari ujian yang sama, jawab semua soal yang tersisa, kembali ke "Tinjau Jawaban"
**Expected:** Alert success "Semua soal sudah dijawab", tombol "Kumpulkan Ujian" aktif dan bisa diklik, submit berhasil redirect ke halaman hasil
**Why human:** Memerlukan alur ujian lengkap end-to-end

#### 3. Auto-submit saat Waktu Habis

**Test:** Biarkan timer ujian berjalan sampai habis (atau gunakan ujian dengan durasi sangat pendek)
**Expected:** Auto-submit terpicu, proses berhasil ke halaman hasil meskipun ada soal yang belum dijawab — tidak terblokir
**Why human:** Memerlukan timer habis secara nyata, tidak bisa disimulasikan via grep

---

### Gaps Summary

Tidak ada gap — semua 4 truths terverifikasi di level kode. Verifikasi otomatis PASSED untuk seluruh artifacts, key links, data flow, dan acceptance criteria dari PLAN.

Tiga item diarahkan ke human verification untuk konfirmasi perilaku runtime di browser:
1. Rendering disabled button yang kondisional
2. Alur submit lengkap end-to-end
3. Behavior auto-submit saat timer habis

---

*Verified: 2026-03-28T08:00:00Z*
*Verifier: Claude (gsd-verifier)*
