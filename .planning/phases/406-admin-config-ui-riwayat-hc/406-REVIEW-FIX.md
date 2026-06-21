---
phase: 406-admin-config-ui-riwayat-hc
fixed_at: 2026-06-21T00:00:00Z
review_path: .planning/phases/406-admin-config-ui-riwayat-hc/406-REVIEW.md
iteration: 1
findings_in_scope: 1
fixed: 1
skipped: 0
status: all_fixed
---

# Phase 406: Code Review Fix Report

**Fixed at:** 2026-06-21
**Source review:** .planning/phases/406-admin-config-ui-riwayat-hc/406-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope (critical + warning): 1
- Fixed: 1
- Skipped: 0
- Info findings (out of scope, NON-blocking): 3 — accepted as-is, see "Accepted (non-blocking) Info findings" below.

## Fixed Issues

### WR-01: Badge header accordion menampilkan "Gagal" untuk attempt pending (IsPassed null)

**Files modified:** `Views/Admin/_RiwayatPercobaan.cshtml`
**Commit:** `819beb76`
**Applied fix:**

Mengubah badge Lulus/Gagal di header accordion dari derivasi DUA-keadaan menjadi TIGA-cabang tri-state, sejajar dengan handling `bool?` yang sudah benar pada tabel per-soal:

- Menghapus variabel `var passSuccess = attempt.IsPassed == true;` (line 21) yang me-runtuhkan tri-state `bool?` jadi `true`/`false`.
- Mengganti badge satu-baris `text-bg-@(passSuccess ? "success" : "danger")` + teks `@(passSuccess ? "Lulus" : "Gagal")` (line 28) dengan blok `@if/@else if/@else`:
  - `attempt.IsPassed == true`  → `<span class="badge text-bg-success ms-2">Lulus</span>`
  - `attempt.IsPassed == false` → `<span class="badge text-bg-danger ms-2">Gagal</span>`
  - `null` (essay pending grading) → `<span class="badge bg-warning text-dark ms-2">Menunggu Penilaian</span>`

**Konvensi yang diikuti:** wording "Menunggu Penilaian" + badge class `bg-warning text-dark` mengikuti pola pending app-wide yang sudah ada di `Views/Admin/AssessmentMonitoringDetail.cshtml` (lines 248, 255, 430, 1395 — status map `"Menunggu Penilaian" => "bg-warning text-dark"` dan badge essay-pending) serta konstanta `AssessmentConstants.AssessmentStatus.PendingGrading = "Menunggu Penilaian"`. Hasil: header attempt kini selaras secara internal — skor menampilkan "—%" DAN badge "Menunggu Penilaian" (bukan lagi "—%" + "Gagal").

**Cakupan & batasan:**
- VIEW display fix saja. Logika `RiwayatUnifier` (sumber `IsPassed` dari `AssessmentAttemptHistory.IsPassed` / sesi current) TIDAK diubah.
- XSS-safety dipertahankan: seluruh teks badge statis/literal, konten peserta tetap `@`-encoded; tidak ada `Html.Raw`.
- Fix ini juga otomatis menutup IN-02 (current attempt dengan `session.Score` null kini menampilkan "Menunggu Penilaian", bukan "Gagal" turunan) — sesuai catatan reviewer.

**Verification evidence:**
- Tier 1 (re-read): badge tiga-cabang hadir (lines 27-38), variabel `passSuccess` terhapus, markup sekitarnya (skor, tanggal selesai, badge "Percobaan saat ini") utuh.
- Tier 2 (Razor compile): `dotnet build` → **0 Error(s)**, 25 Warning(s) (semua pre-existing, NOL di `_RiwayatPercobaan.cshtml`). Razor mengkompilasi bersih.
- Logic regression: `dotnet test --filter "FullyQualifiedName~RiwayatUnifier"` → **6/6 Passed** (logika unifier tak tersentuh, sesuai harapan view-only fix).
- Runtime (Playwright): skenario "pending" pada `tests/e2e/riwayat-hc-406.spec.ts` sudah meng-cover jalur badge pending. Siklus penuh SEED_WORKFLOW (snapshot → seed essay-pending → app @5270 → restore) tidak dijalankan untuk perubahan view kecil ini; **re-run `riwayat-hc-406.spec.ts` (skenario pending) direkomendasikan pada gate phase-verify**. Tidak ada Playwright-green yang dipalsukan.

## Accepted (non-blocking) Info findings

Ketiga temuan Info di luar `fix_scope` (critical_warning) dan diterima apa adanya — tidak ada perubahan kode:

- **IN-01 (IDOR by-design):** `RiwayatPercobaan` role-only (`Admin, HC`) tanpa unit-scoping — KONSISTEN dengan pola otorisasi monitoring eksisting (`EssayGrading`, `EditHistoryPartial`, `AssessmentMonitoringDetail`). Bukan regresi. Saat unit-scoping HC diaktifkan app-wide (mis. v32.3 multi-unit), endpoint ini perlu ikut difilter UserUnit. **Accepted.**
- **IN-02 (current attempt badge "Gagal" saat Score null):** turunan dari WR-01. **Otomatis tertutup** oleh fix WR-01 (current attempt null kini "Menunggu Penilaian"). Tidak perlu perubahan di unifier — provenance dari session sudah benar. **Accepted (resolved via WR-01).**
- **IN-03 (query efficiency / no paging):** volume baris dibatasi domain oleh `MaxAttempts` (1–5) per worker per (Title, Category); pola query sudah efisien (satu query histories, satu query archive via `Contains`, tanpa N+1). Bukan masalah nyata, di luar scope v1. **Accepted.**

---

_Fixed: 2026-06-21_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
