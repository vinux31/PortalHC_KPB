# Delete Input Records — Full Cascade Overhaul (Design)

**Tanggal:** 2026-06-10
**Status:** Spec final — disetujui user (brainstorm 2026-06-10)
**Asal:** Kasus lapangan — admin hapus assessment worker via Manage Assessment > Input Records > Aksi hapus > "berhasil", tapi worker login masih melihat assessment-nya (kasus lokal: seed repro; kasus Dev: Rino Adi Prasetyo, audit log 2026-06-10 10:21).

---

## 1. Masalah (29 temuan, terverifikasi adversarial + repro live)

### A. Akar kasus "hapus berhasil tapi masih ada"

| # | Temuan | Bukti |
|---|--------|-------|
| 1 | **Sukses palsu HTMX** — `DeleteTabResult()` selalu balas HTTP 200 + HX-Trigger `recordDeleted` di SEMUA jalur (sukses, pre-check blokir, catch DbUpdateException, NotFound); tidak ada toast/error di tab; re-fetch menyembunyikan/memunculkan ulang baris tanpa pesan | `TrainingAdminController.cs:561-569`, dipanggil `:601/:634/:1000/:1031`; tombol `hx-swap="none"` `_TrainingRecordsTab.cshtml:348-366`; handler error `ManageAssessment.cshtml:264-275` hanya target `.htmx-tab-wrapper` |
| 2 | **Blokir renewal diam-diam** — pre-check membatalkan hapus bila ada anak renewal; error hanya TempData, tidak pernah dirender di partial; muncul telat di full-page berikutnya | `TrainingAdminController.cs:590-601, 989-1000`; flash hanya `ManageAssessment.cshtml:33-43` |
| 3 | **Mismatch baris** — tab Input Records hanya tampil+hapus session `IsManualEntry`; assessment ONLINE worker tak terjangkau (kasus Rino: yang terhapus = TrainingRecord "Assessment: ...", yang dilihat Rino = 2 sesi online) | `_TrainingRecordsTab.cshtml:266`; gate `:978` |
| 4 | **Assessment online >7 hari tak bisa dihapus via UI** — tab 1 hard-filter `(ExamWindowCloseDate ?? Schedule) >= -7 hari` sebelum search/status. Endpoint POST-nya sendiri tidak membatasi umur (UI-only gap) | `AssessmentAdminController.cs:116, 121` |

### B. Residu saat hapus sukses

| # | Temuan | Bukti |
|---|--------|-------|
| 5 | AttemptHistory orphan — `DeleteManualAssessment` tidak bersihkan (gold standard membersihkan) | `TrainingAdminController.cs:976-1031` vs `AssessmentAdminController.cs:2288-2296` |
| 6 | UserNotifications yatim — denormalized tanpa FK, tak pernah dibersihkan alur delete | `UserNotification.cs:39,46` |
| 7 | FK renewal anak tak pernah di-null-clear — mandat app-level (`ApplicationDbContext.cs:167-168`) terealisasi sebagai BLOKIR, bukan pelepasan link | `ApplicationDbContext.cs:171-180, 235-243` |
| 8 | LinkedSessionId dangling — no FK nyata (komentar ON DELETE SET NULL menyesatkan); vektor riil = `DeleteAssessmentGroup` (tanpa guard D-19); dampak: gain score Post-Test silent missing | `AssessmentSession.cs:168-172`; `CMPController.cs:2461-2476, 2743-2776` |
| 9 | Penanda Proton `Origin='Exam'` tak dicabut saat sesi ujian Proton dihapus — `RemoveExamOriginAsync` hanya dipanggil GradingService re-grade | `ProtonCompletionService.cs:70-86`; `GradingService.cs:492-497`; DeleteAssessment 0 panggilan |
| 10 | `PendingProtonBypasses.LinkedAssessmentSessionId` dangling (no FK, no cleanup) | `ProtonModels.cs:244`; migration `20260610094950` |
| 11 | EditLogs + PackageUserResponses FK Restrict → DeleteManualAssessment bisa gagal DbUpdateException (pesan generik); gold standard membersihkan eksplisit | `ApplicationDbContext.cs:256, 511-514`; `TrainingAdminController.cs:1025-1030` |

### C. Sumber duplikat & integritas

| # | Temuan | Bukti |
|---|--------|-------|
| 12 | AddManualAssessment + ImportTraining tanpa guard duplikat (user+judul+tanggal) | `TrainingAdminController.cs:653-725, 1152-1362` |
| 14 | **BulkBackfill tanpa guard duplikat** — jalur "resurrection" manual record terhapus + double-click = dobel. (Bukan auto-restore: murni Excel insert, tak baca mirror; label UI "Restore Lost Data" menyesatkan) | `TrainingAdminController.cs:748-878, 816-849` |
| 15 | **Mirror TrainingRecord legacy** (auto-create pra-Phase 324, regresi 766011b6) tanpa FK ke session → tak terjangkau cascade; tampil terus di worker view pasca hapus session | `GradingService.cs:262-265` (komentar); `TrainingRecord.cs:62-68` |

### D. UI & alur tetangga (ditemukan re-check #2)

| # | Temuan | Bukti |
|---|--------|-------|
| 16 | Badge "X assessments" hitung SEMUA `IsPassed==true` (termasuk online yang tak tampil di list manual-only) | `WorkerDataService.cs:306-311, 351`; `WorkerTrainingStatus.cs:55-57` |
| 17 | Badge "Y trainings" hanya hitung Passed/Valid/Permanent; list tampilkan semua status | `WorkerDataService.cs:330-332` vs `_TrainingRecordsTab.cshtml:263-264` |
| 18 | DeleteAssessmentGroup sibling over-match (Title+Category+Schedule.Date tanpa filter LinkedGroupId/AssessmentType/IsManualEntry) → bisa sapu separuh pasangan Pre/Post + record manual se-judul | `AssessmentAdminController.cs:2395-2400` |
| 19 | Delete tab 1 tak hapus file fisik sertifikat manual session (manual bisa nongol di tab 1, tanpa guard IsManualEntry) | `AssessmentAdminController.cs:2187-2369` (0 file ops) |
| 20 | ResetAssessment tanpa guard IsManualEntry → record sertifikat manual jadi sesi Open kosong, korup permanen | `AssessmentAdminController.cs:4013-4046` (kontras EditAssessment `:3069`) |
| 21 | Edit (Training + ManualAssessment): replace file non-atomik — file lama dihapus PRE-save/PRE-commit | `TrainingAdminController.cs:516-526, 939-944` |
| 22 | ResetAssessment tak hapus SessionElemenTeknisScores → retake kena unique index, exception ditelan, analitik ET stale | `AssessmentAdminController.cs:4071-4101`; `GradingService.cs:174-194` |
| 23 | AttemptHistory orphan tampil di History tab + inflasi AttemptNumber sesi baru se-judul | `WorkerDataService.cs:104-131, 161-186` |
| 24 | ImportTraining tanpa audit log; `AssessmentType=""`; `GenerateCertificate=true` tanpa syarat | `TrainingAdminController.cs:1152-1362, 1243, 1253` |
| 25 | CertificationManagement CMP+CDP `ToDictionary(c=>c.Name)` → 500 bila sub-kategori kembar lintas parent (AdminBase sudah GroupBy) | `CMPController.cs:4157-4159`; `CDPController.cs:3888-3890`; `AdminBaseController.cs:139-142` |
| 26 | EditTraining terima Renews*Id arbitrer — nonexistent → 500 FK; cross-user → IsRenewed palsu sembunyikan sertifikat expired orang lain | `TrainingAdminController.cs:483-494, 541-544` |
| 27 | Sesi hasil BulkBackfill berubah identitas (IsManualEntry=true, AssessmentType="Standard", Id baru) — bukan replika asli | `TrainingAdminController.cs:824-844` |

### Di luar scope (backlog 999.6)
Impersonate "view as X" menampilkan data admin asli (`CMPController.cs:2535-2542` resolve principal asli; `ImpersonationMiddleware.cs:111-142` hanya isi HttpContext.Items).

### Terverifikasi AMAN (tidak perlu diubah)
Authorization `Admin, HC` + ValidateAntiForgeryToken pada kedua endpoint delete tab 2; file delete tab 2 sudah pola fase 331 (capture pre-Remove, File.Delete post-commit warn-only); cleanup parity DeleteAssessmentGroup/DeletePrePostGroup = gold standard.

---

## 2. Keputusan desain (kebijakan user, final)

1. **Cascade penuh, no blocker.** Hapus record → SEMUA turunan renewal ikut terhapus, rekursif, lintas tabel (`TrainingRecords ↔ AssessmentSessions` via `RenewsTrainingId`/`RenewsSessionId`), dengan guard siklus. Rasional user: induk dihapus = turunan kehilangan dasar → worker assessment ulang.
2. **Konfirmasi = preview, bukan blokir.** Sebelum eksekusi, admin melihat daftar persis apa yang ikut terhapus (judul + tanggal + jenis, termasuk turunan renewal dan kandidat mirror legacy) → tombol "Hapus Semua".
3. **Notifikasi lonceng ikut dibersihkan** (matching konservatif: ActionUrl menunjuk rute entitas terhapus, mis. `/CMP/Results/{id}`).
4. **Tab Input Records juga menampilkan assessment ONLINE** milik worker (badge pembeda) + tombol hapus per-session → menutup gap #3 dan #4 (sesi >7 hari).
5. **UI jujur**: sukses → sinyal sukses di tab; gagal → pesan merah langsung di tab. Tidak ada lagi respons gagal yang identik dengan sukses.
6. **Anak renewal turunan**: IKUT DIHAPUS (bukan detach) — keputusan eksplisit user menolak opsi detach.

## 3. Desain

### 3.1 Cascade engine (service baru, mis. `RecordCascadeDeleteService`)
- Input: node awal (TrainingRecord ATAU AssessmentSession). Traversal BFS rekursif anak renewal lintas dua tabel, guard cycle (HashSet visited).
- **Preview mode**: kembalikan pohon (jenis, Id, judul, tanggal, pemilik) tanpa mutasi — dipakai modal konfirmasi.
- **Execute mode**: satu transaction; per node:
  - AssessmentSession: RemoveRange `AssessmentEditLogs`, `PackageUserResponses`, `AssessmentAttemptHistory` (by SessionId), `UserPackageAssignments`, `AssessmentPackages`+Questions+Options (blueprint gold standard `AssessmentAdminController.cs:2270-2329`); null-clear `LinkedSessionId` pasangan; cleanup `PendingProtonBypasses` (LinkedAssessmentSessionId == Id → batalkan/hapus pending); bila Category Proton → `RemoveExamOriginAsync`; kumpulkan path file sertifikat manual (`ManualSertifikatUrl`).
  - TrainingRecord: kumpulkan path `SertifikatUrl`.
  - Keduanya: hapus `UserNotifications` terkait (matching ActionUrl), Remove node.
- AuditLog 1 entri per operasi: aktor, node akar, jumlah + daftar Id turunan.
- File.Delete POST-commit, inner try/catch warn-only (pola fase 331-334).
- **Mirror legacy (temuan 15)**: preview menampilkan kandidat mirror via heuristik (`TrainingRecords` user sama dengan `Judul == session.Title` ATAU `Judul == "Assessment: " + Title`, tanggal ± sama) dengan checkbox opsional "ikut hapus" — default tercentang, admin bisa kecualikan. Heuristik TIDAK auto-hapus tanpa tampil di preview.

### 3.2 Endpoint & UI tab Input Records
- `GET DeletePreview(type, id)` → partial modal daftar korban cascade.
- `POST DeleteTraining` / `DeleteManualAssessment` direfactor pakai cascade engine; pre-check renewal lama DIHAPUS (digantikan preview).
- Endpoint baru `POST DeleteWorkerSession` (online, gate `!IsManualEntry` dihilangkan — satu endpoint generik boleh) dengan cascade sama; guard role `Admin, HC` + antiforgery.
- `_TrainingRecordsTab.cshtml`: tampilkan SEMUA session worker (manual + online, badge "Assessment Online" / "Assessment Manual") + tombol hapus per baris; render blok flash error/sukses DI DALAM partial; HX-Trigger dibedakan `recordDeleted` vs `recordDeleteFailed` (payload pesan).
- Tab 1 (DeleteAssessment/Group/PrePost): pre-check renewal fase 325/329 diubah dari blocker → konfirmasi cascade yang sama (konsisten kebijakan no-blocker).

### 3.3 Fix tetangga (masuk fase ini per keputusan user "masukkan semua")
- **Badge count** (16, 17): satu formula dengan list — definisikan eksplisit (count = jumlah baris yang tampil di tab per jenis), atau label diubah jujur. Pilih saat planning; acceptance: badge == jumlah baris list.
- **DeleteAssessmentGroup over-match** (18): sibling query tambah filter `LinkedGroupId == null && AssessmentType bukan PreTest/PostTest && !IsManualEntry` (samakan scope dengan baris yang ditampilkan tab 1 `:155-156`).
- **File fisik tab 1** (19): ketiga endpoint delete tab 1 ikut kumpulkan + hapus file sertifikat manual session post-commit.
- **ResetAssessment** (20): guard `IsManualEntry` → tolak dengan pesan; (22): reset ikut RemoveRange `SessionElemenTeknisScores`; (23): reset/hapus bersihkan AttemptHistory terkait sudah tercakup cascade; orphan legacy dibersihkan one-time di plan terpisah (script/endpoint admin sekali jalan, opsional).
- **Guard duplikat** (12, 14): AddManualAssessment, ImportTraining, BulkBackfill — cek `UserId + Title/Judul + Tanggal` existing → tolak/skip dengan laporan per baris (import: kolom status "duplikat — dilewati").
- **Edit atomic file** (21): EditTraining + EditManualAssessment ikuti pola fase 331 — simpan file baru dulu, SaveChanges, baru hapus file lama post-commit warn-only.
- **ImportTraining** (24): tambah `_auditLog.LogAsync` per operasi import (ringkasan) + `AssessmentType = AssessmentConstants.AssessmentType.Manual` + `GenerateCertificate = isPassed`.
- **CertificationManagement dedup** (25): CMP + CDP pakai GroupBy ala AdminBase (atau refactor panggil helper AdminBase).
- **EditTraining renewal validation** (26): Renews*Id wajib exist + UserId sama dengan record yang diedit; selain itu → ModelState error.
- **BulkBackfill** (14, 27): guard duplikat + set `AssessmentType` konstanta Manual + rename label UI "Bulk Import Nilai (Excel)" agar tidak menyesatkan.

### 3.4 Testing
- Unit: cascade engine (traversal multi-level, lintas tabel, cycle guard, preview == execute set), guard duplikat, badge formula.
- Integration real-SQL (pola fase 360): cascade execute menghapus semua artefak (assert per tabel), notifikasi terhapus, penanda Proton tercabut, transaction rollback saat exception.
- UAT Playwright @5277 (pola fase 359/362): repro skenario seed (induk+anak renewal) → preview menampilkan anak → hapus → DB bersih → worker view bersih; skenario gagal → pesan merah tampil di tab; sesi online >7 hari tampil + terhapus dari tab 2.
- Seed Workflow wajib (snapshot → seed → restore + journal).

### 3.5 Out of scope
- Impersonate identity (backlog 999.6).
- Soft-delete/undo (opsi C ditolak).
- Tab 1 filter 7 hari tetap (kebutuhan delete sesi lama dipenuhi via tab 2).

## 4. Estimasi & risiko
- Estimasi: 4-6 hari (2 fase di roadmap; fase inti dulu).
- Risiko utama: cascade menghapus data sah bila admin tidak membaca preview → mitigasi: preview eksplisit + audit log lengkap + snapshot DB lokal saat UAT. Heuristik mirror bisa false-positive → mitigasi: checkbox opt-out di preview, default visible.
- Migration: **tidak ada** (semua perubahan kode; tidak ada kolom baru).
