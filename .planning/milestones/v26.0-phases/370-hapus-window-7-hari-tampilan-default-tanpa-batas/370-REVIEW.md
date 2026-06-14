---
phase: 370-hapus-window-7-hari-tampilan-default-tanpa-batas
reviewed: 2026-06-11T21:30:00+08:00
depth: standard
files_reviewed: 1
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
findings:
  critical: 0
  warning: 0
  info: 2
  total: 2
status: clean
---

# Phase 370: Code Review Report

**Reviewed:** 2026-06-11
**Depth:** standard
**Files Reviewed:** 1
**Status:** clean (2 info non-blocking)

## Summary

Phase 370 (URG-02) menghapus window 7-hari dari dua endpoint read-only `[Authorize(Roles = "Admin, HC")]`: `ManageAssessmentTab_Assessment` (L112) dan `AssessmentMonitoring` (L2846), menambah `.AsNoTracking()` di query Monitoring (L2853-2855), dan menghapus `HcPortal.Tests/AssessmentSearchWindowTests.cs` (3 [Fact] penguji helper yang di-retire). Diff bersih dan minimal — murni query-layer, view tidak berubah, sesuai 370-CONTEXT.md.

Hasil verifikasi region berubah + dampak downstream:

1. **Residual nol** — grep seluruh codebase (exclude `.planning/`, `docs/`): tidak ada sisa `ApplySevenDayWindow`, `sevenDaysAgo`, atau teks UI "7 hari". Hanya 2 komentar penanda Phase 370 (L117, L2851). Tidak ada teks stale di `Views/Admin/AssessmentMonitoring.cshtml`. Test file terhapus tanpa dangling reference (suite 226/226).
2. **CIL-01 badge counts** — counter Open/Upcoming/Closed tetap dihitung SEBELUM filter apply (L204-206, L3013-3015); kini mencakup seluruh histori (ClosedCount membesar) — sesuai intent URG-02 "tanpa batas umur". View badge (AssessmentMonitoring.cshtml L162-169) konsisten.
3. **CIL-02 hide-Closed default** — utuh di kedua endpoint (L210-213 Tab Assessment; L3020-3024 Monitoring): Closed disembunyikan HANYA saat status & search keduanya kosong. Perilaku search-override 260611-m9r tidak regresi (search non-empty → Closed ikut tampil).
4. **MAP-15 status="All"** — utuh (L3029-3034); kontrak controller-view match: view default `selStatus="active"` (view L12) selaras `status = "active"` controller L3023, opsi dropdown "All" tersedia (view L88-94).
5. **Pagination Tab Assessment** — `PaginationHelper.Calculate` post-grouping in-memory (L215-217) tetap benar untuk dataset berapapun; MAP-21 `ViewBag.PageSize` utuh.
6. **`.AsNoTracking()` Monitoring** — aman; query memproyeksi ke anonymous type (memang tidak di-track), jadi efeknya defensif/konsistensi dengan pola Phase 311. Method tidak punya `SaveChanges`.
7. **Grouping in-memory** — key standard group `(Title, Category, Schedule.Date)` tetap unik lintas tahun (Date mengandung year); `g.First()` selalu pada group non-empty; tidak ada edge case baru akibat histori penuh.

Tidak ditemukan bug correctness ataupun isu keamanan di region berubah. Dua catatan Info di bawah — keduanya non-blocking (satu sudah didokumentasikan sebagai deferral sadar di 370-CONTEXT.md).

## Info

### IN-01: Pertumbuhan dataset unbounded — Monitoring tanpa pagination, Tab Assessment full-table load

**File:** `Controllers/AssessmentAdminController.cs:2869-2891, 3049` (Monitoring) dan `:141-152` (Tab Assessment)
**Issue:** Dengan window dihapus, `AssessmentMonitoring` me-load seluruh tabel `AssessmentSessions` lalu `return View(grouped)` tanpa paging (view `AssessmentMonitoring.cshtml` tidak punya pagination) — filter `status=Closed`/`All` akan me-render seluruh histori dalam satu halaman dan memanjang seiring waktu. `ManageAssessmentTab_Assessment` juga full-table load + grouping in-memory per request HTMX (render-nya tetap paginated). Ini tradeoff yang SUDAH didokumentasikan dan di-defer sadar di `370-CONTEXT.md` (baris 89: "Kandidat fase perf/UX nanti kalau row membengkak"), dengan mitigasi default filter Aktif menyembunyikan Closed. Dicatat di sini agar tetap di radar fase berikutnya.
**Fix:** Saat row membengkak: tambah server-side pagination di Monitoring (pola `PaginationHelper` Tab Assessment) dan/atau push-down grouping ke SQL. Bukan untuk phase ini (roadmap 370: view tak berubah).

### IN-02: Pre-existing — `pageSize` Tab Assessment tidak divalidasi; blast radius membesar pasca window dihapus

**File:** `Controllers/AssessmentAdminController.cs:112` dan `Helpers/PaginationHelper.cs:7-13`
**Issue:** `ManageAssessmentTab_Assessment` menerima `pageSize` mentah tanpa whitelist, berbeda dari `ManageAssessmentTab_Training` yang memvalidasi (`L279`: hanya 20/50/100). `PaginationHelper.Calculate` juga tidak meng-clamp: `pageSize=0` menghasilkan halaman kosong silent (`Take(0)`), dan `pageSize` raksasa (mis. 999999) me-render seluruh `grouped` dalam satu partial — kini berarti seluruh histori karena dataset tidak lagi dibatasi window. Risiko rendah (endpoint Admin/HC-only, self-inflicted), bukan scope phase 370 — dicatat sebagai pre-existing.
**Fix:** Terapkan validasi yang sama dengan Training tab:
```csharp
var pageSizeValidated = (pageSize == 20 || pageSize == 50 || pageSize == 100) ? pageSize : 20;
var paging = PaginationHelper.Calculate(grouped.Count, page, pageSizeValidated);
```

---

_Reviewed: 2026-06-11T21:30:00+08:00_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
