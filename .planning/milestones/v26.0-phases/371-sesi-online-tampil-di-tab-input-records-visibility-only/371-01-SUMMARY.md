---
phase: 371-sesi-online-tampil-di-tab-input-records-visibility-only
plan: 01
status: complete
completed: 2026-06-12
requirements: [URG-03]
commits:
  - d1d03e13
files_modified:
  - Views/Admin/Shared/_TrainingRecordsTab.cshtml
migration: false
---

## What Was Built

Longgarkan filter `IsManualEntry` di `Views/Admin/Shared/_TrainingRecordsTab.cshtml` (Tab Input Records, expand per-worker) sehingga sesi assessment online (`IsManualEntry == false`) kini tampil bersama record manual & training. Visibility-only: tanpa aksi hapus/edit untuk online (delete cascade tetap scope Phase 367).

5 edit di 1 file (Razor view):
1. **Blok proyeksi anon** — tambah 3 local function (`OnlineLabel`, `OnlineClass`, `ManualStatusClass`) + rewrite ketiga proyeksi (training, manual, online) ke shape anon identik 10-property (`Type, Date, Title, Detail, Status, StatusClass, ValidUntil, Id, IsOnline, CanViewResult`). `allRows = trainingRows.Concat(assessmentRows).Concat(onlineRows)`.
2. **Empty-state copy** — "Belum ada record manual untuk pekerja ini." → "Belum ada record untuk pekerja ini." (drop "manual", D-04).
3. **Badge Tipe 3-way** — tambah cabang `AssessmentOnline` → `<span class="badge bg-secondary">Assessment Online</span>`.
4. **Status badge** — hapus switch inline lama; pakai `row.StatusClass` yang dihitung saat proyeksi.
5. **Kolom Aksi** — tambah cabang `@if (row.IsOnline)` paling atas: hanya tombol "Lihat hasil" (gated `CanViewResult` = Completed/PendingGrading) → `Url.Action("Results", "CMP", new { id = row.Id })`, NO edit/hapus. Branch Training & Assessment Manual (Edit + HTMX delete `DeleteTraining`/`DeleteManualAssessment` + `antiToken`) dipertahankan verbatim.

Derivasi status online 6-way meniru `DeriveUserStatus` (AssessmentAdminController:2799) dengan urutan cek kritis PendingGrading pertama, dilapis IsPassed→Lulus/Tidak Lulus + pemetaan warna inline (D-03, no `@using HcPortal.Controllers`).

## Verification

- `dotnet build` → **0 error, 0 warning** (anon-shape 3-operand `.Concat()` compile OK — lesson Phase 354).
- `dotnet test HcPortal.Tests` → **226/226 baseline pass**. (2 failure di `AssessmentWindowRemovalTests.cs` = file untracked WIP sesi paralel 370-secure, target action `ManageAssessmentTab_Assessment` — beda tab/action, independen dari view edit ini.)
- Grep acceptance: `Assessment Online`=1, `AssessmentOnline`=2, `!a.IsManualEntry` match, `Url.Action("Results","CMP"` match, empty-state baru match + "Belum ada record manual"=0, `DeleteTraining`+`DeleteManualAssessment` retained, `.Concat(`=2 occurrences.

## UAT @5277 (Playwright MCP, AD-off, ZERO seed)

3/3 SC PASS:
- **SC1** — Expand Rino (NIP 29007720, GAST): semua row online + badge "Assessment Online"; 6-way status visual benar (Lulus/Tidak Lulus/Belum Mulai/Sedang Dikerjakan/Dibatalkan); sesi >7 hari muncul (17 Feb 2025, 10 Mar 2024, 15 Jan 2024); Upcoming/Open → "Belum Mulai" (Pitfall 3); no HTTP 500/RuntimeBinderException.
- **SC2** — Manual/training row + Edit/Hapus tetap; online read-only (no Edit/Hapus); Lihat hasil hanya Completed → `/CMP/Results/126` load halaman hasil penuh (85% LULUS, Peserta Rino), bukan 403; online belum-selesai → Aksi kosong.
- **SC3** — Expand Choirul Anam (0 record) → "Belum ada record untuk pekerja ini." (tanpa "manual"); tombol Tambah tetap.

Catatan: status "Menunggu Penilaian" tak ada sesi essay-pending di DB lokal untuk visual-confirm; logic tetap ada di kode (PendingGrading dicek pertama). Bukan blocker.

## Deviations

Tidak ada deviasi dari plan. Kelima edit sesuai spec; `—` em-dash file dipertahankan literal.

## Notes for Next Phase

Phase 367 (delete cascade) membangun aksi hapus DI ATAS badge ini — branch `@if (row.IsOnline)` di kolom Aksi = extension point. SC4 Phase 367 build di atas struktur ini. Service TIDAK disentuh; migration=false.
