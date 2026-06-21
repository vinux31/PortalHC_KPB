---
phase: 409-data-foundation-re-entry-guards-exclude-removed-query
verified: 2026-06-21T02:06:45Z
status: passed
score: 10/10 must-haves verified
overrides_applied: 0
re_verification: null
gaps: []
deferred:
  - truth: "PLIV-01 panel 'Peserta Dikeluarkan' terpisah di Monitoring Detail"
    addressed_in: "Phase 412"
    evidence: "REQUIREMENTS.md traceability: PLIV-01 → Phase 412 (Live Monitoring UI + SignalR). Phase 409 hanya menyiapkan fondasi exclude-query; panel UI = scope Phase 412."
human_verification: []
---

# Phase 409: Laporan Verifikasi

**Goal Phase:** Skema + invarian dasar soft-remove tersedia & terpasang — kolom RemovedAt/RemovedBy/RemovalReason di AssessmentSession (migration additif nullable); definisi tunggal "soft-removed ⇔ RemovedAt != null"; peserta soft-removed TIDAK bisa StartExam/SubmitExam/JoinBatch (guard re-entry); semua daftar/perhitungan peserta AKTIF (Monitoring/grouping/detail + count) mengecualikan RemovedAt != null; UserAssessmentHistory boundary TETAP tampil removed.

**Diverifikasi:** 2026-06-21T02:06:45Z
**Status:** LULUS
**Re-verifikasi:** Tidak — verifikasi awal

---

## Pencapaian Goal

### Kebenaran yang Dapat Diobservasi

| #  | Kebenaran | Status | Bukti |
|----|-----------|--------|-------|
| 1  | Kolom RemovedAt/RemovedBy/RemovalReason hadir di tabel AssessmentSessions | TERVERIFIKASI | sqlcmd: 3 baris (RemovedAt datetime2 YES NULL, RemovedBy nvarchar YES -1, RemovalReason nvarchar YES 500) |
| 2  | Migration AddParticipantRemovalColumns ter-apply ke HcPortalDB_Dev tanpa error | TERVERIFIKASI | sqlcmd konfirmasi 3 kolom hadir; `01cd7dd0` adalah commit migration |
| 3  | Semua baris existing AssessmentSessions punya 3 kolom baru = NULL (additif, non-destruktif) | TERVERIFIKASI | sqlcmd: `SELECT COUNT(*) WHERE RemovedAt IS NOT NULL` → 0 |
| 4  | Invarian tunggal terdefinisi: soft-removed ⇔ RemovedAt != null; aktif ⇔ RemovedAt == null | TERVERIFIKASI | `CMPController.IsParticipantRemoved(session) => session.RemovedAt != null` (:2540); XML doc di model konfirmasi invarian |
| 5  | Peserta dengan sesi RemovedAt != null tidak dapat memulai ujian (StartExam → redirect, sesi TIDAK ter-mark InProgress) | TERVERIFIKASI | CMPController.cs:924 guard `if (IsParticipantRemoved(assessment))` + TempData["Error"]="Anda telah dikeluarkan dari ujian ini." sebelum mark-InProgress |
| 6  | Peserta dengan sesi RemovedAt != null tidak dapat mensubmit (SubmitExam → redirect sebelum grading) | TERVERIFIKASI | CMPController.cs:1611 guard identik sebelum `ShouldGateMissingStart`/grading |
| 7  | AssessmentHub JoinBatch + SaveTextAnswer + SaveMultipleAnswer menolak sesi RemovedAt != null (silent-skip / null return) | TERVERIFIKASI | AssessmentHub.cs:31 AnyAsync `&& s.RemovedAt == null`; :146 + :213 FirstOrDefaultAsync += `&& s.RemovedAt == null` (3 occurrence terverifikasi) |
| 8  | Monitoring (Tab Assessment, AssessmentMonitoring, AssessmentMonitoringDetail + semua count) mengecualikan sesi RemovedAt != null | TERVERIFIKASI | AssessmentAdminController.cs:121, :2825, :3335 — masing-masing `.Where(a => a.RemovedAt == null)` (3 site terverifikasi); count inherit otomatis |
| 9  | UserAssessmentHistory per-pekerja TETAP menampilkan sesi removed (boundary, anti over-exclude) | TERVERIFIKASI | UserAssessmentHistory (:5263) tidak disentuh; test boundary `UserAssessmentHistory_StillShows_RemovedSession` GREEN |
| 10 | 6 test de-tautologis GREEN (3 guard + 2 exclude + 1 boundary); full suite 569/569 hijau | TERVERIFIKASI | `dotnet test --filter ~ParticipantRemoval`: Passed 6/6; full suite Passed 569/569, 0 gagal |

**Skor: 10/10 kebenaran terverifikasi**

---

### Item Deferrred (Scope Phase Lain)

Item berikut bukan gap Phase 409 — secara eksplisit ditangani di fase milestone selanjutnya.

| # | Item | Ditangani Di | Bukti |
|---|------|-------------|-------|
| 1 | Panel "Peserta Dikeluarkan" terpisah di Monitoring Detail (tampilan UI removed) | Phase 412 | REQUIREMENTS.md PLIV-01 → Phase 412; Phase 409 hanya fondasi exclude-query, panel UI bukan scope-nya |
| 2 | ExportAssessmentResults / BulkExportPdf / GetDeleteImpact.certCount exclude sesi removed | Phase 412/413 | Keputusan A2=OUT (scope Phase 409); dicatat eksplisit di 409-02-SUMMARY.md Deferred |
| 3 | RemovalReason XSS-at-render di panel | Phase 412 | T-409-10 defer: penulisan RemovalReason = Phase 411; render+escape = Phase 412 |

---

### Artifact yang Dibutuhkan

| Artifact | Deskripsi | Status | Detail |
|----------|-----------|--------|--------|
| `Models/AssessmentSession.cs` | 3 properti soft-remove nullable, XML doc invarian | TERVERIFIKASI | :97-103: `DateTime? RemovedAt`, `string? RemovedBy`, `string? RemovalReason`; NO `[MaxLength]` annotation |
| `Data/ApplicationDbContext.cs` | Fluent HasMaxLength(500) RemovalReason; NO HasQueryFilter | TERVERIFIKASI | :225-226: `entity.Property(a => a.RemovalReason).HasMaxLength(500).IsRequired(false)`; tidak ada `HasQueryFilter` di file |
| `Migrations/20260621011101_AddParticipantRemovalColumns.cs` | 3 AddColumn nullable:true tanpa defaultValue; Down() simetris 3 DropColumn | TERVERIFIKASI | Up(): `nullable: true` ketiga kolom; tidak ada `defaultValue`; `RemovalReason` type `nvarchar(500)` maxLength:500; Down() 3 DropColumn |
| `Migrations/ApplicationDbContextModelSnapshot.cs` | ProductVersion 8.0.0 (bukan 10.x) | TERVERIFIKASI | :20 `"ProductVersion", "8.0.0"` — terverifikasi grep |
| `Controllers/CMPController.cs` | Guard IsParticipantRemoved seam + guard StartExam + SubmitExam + SaveAnswer (WR-01) + exclude Assessment active-list (WR-02) | TERVERIFIKASI | :2540 seam static; :924 StartExam guard; :1611 SubmitExam guard; :373 SaveAnswer guard (WR-01 fix); :218 Assessment active-list exclude (WR-02 fix) |
| `Hubs/AssessmentHub.cs` | Predikat RemovedAt==null di JoinBatch + SaveTextAnswer + SaveMultipleAnswer | TERVERIFIKASI | :31, :146, :213 — 3 occurrence `RemovedAt == null` |
| `Controllers/AssessmentAdminController.cs` | Exclude RemovedAt==null di 3 query monitoring; UserAssessmentHistory TIDAK disentuh | TERVERIFIKASI | :121, :2825, :3335 — 3 `.Where(a => a.RemovedAt == null)` |
| `HcPortal.Tests/ParticipantRemovalGuardTests.cs` | 6 fact de-tautologis (Guard SQL nyata + Exclude InMemory real-controller + Boundary) | TERVERIFIKASI | Kelas `ParticipantRemovalExcludeTests` (3 Fact InMemory) + `ParticipantRemovalGuardTests` (3 Fact Integration SQLEXPRESS) |

---

### Verifikasi Key Link

| Dari | Ke | Via | Status | Detail |
|------|----|-----|--------|--------|
| `Migration/20260621011101_AddParticipantRemovalColumns.cs` | `HcPortalDB_Dev.AssessmentSessions` | `dotnet ef database update` | TERHUBUNG | sqlcmd: 3 kolom hadir, nullable, RemovalReason len=500 |
| `CMPController.StartExam` | Blok sesi `RemovedAt != null` | `IsParticipantRemoved(assessment)` sebelum mark-InProgress :1008 | TERHUBUNG | :924 guard verifikasi file |
| `CMPController.SubmitExam` | Blok sesi `RemovedAt != null` | `IsParticipantRemoved(assessment)` sebelum grading | TERHUBUNG | :1611 guard verifikasi file |
| `CMPController.SaveAnswer` | Blok write jawaban MC sesi removed | `IsParticipantRemoved(session)` sebelum upsert response | TERHUBUNG | :373 — WR-01 fix commit `a3fb72ab` |
| `AssessmentHub.JoinBatch` | Tolak (silent-skip) sesi `InProgress` + `RemovedAt != null` | `AnyAsync(...&& s.RemovedAt == null)` | TERHUBUNG | :31 verifikasi file |
| `AssessmentHub.SaveTextAnswer` | Tolak write Essay sesi removed | `FirstOrDefaultAsync(...&& s.RemovedAt == null)` | TERHUBUNG | :146 verifikasi file |
| `AssessmentHub.SaveMultipleAnswer` | Tolak write MA sesi removed | `FirstOrDefaultAsync(...&& s.RemovedAt == null)` | TERHUBUNG | :213 verifikasi file |
| `AssessmentAdminController.managementQuery` | Exclude sesi removed dari Tab grouping | `.Where(a => a.RemovedAt == null)` :121 | TERHUBUNG | :121 verifikasi file |
| `AssessmentAdminController.AssessmentMonitoring.query` | Exclude sesi removed dari list monitoring | `.Where(a => a.RemovedAt == null)` :2825 | TERHUBUNG | :2825 verifikasi file |
| `AssessmentAdminController.AssessmentMonitoringDetail.query` | Exclude sesi removed dari detail + semua count | `&& a.RemovedAt == null` dalam predikat :3335 | TERHUBUNG | :3335 verifikasi file; count InProgressCount/TotalCount inherit |
| `CMPController.Assessment` active-list | Exclude sesi removed dari daftar ujian pekerja | `.Where(a => a.RemovedAt == null)` :218 | TERHUBUNG | WR-02 fix commit `a3fb72ab`; completedHistory SENGAJA tidak disentuh |

---

### Trace Alur Data (Level 4)

| Artifact | Variabel Data | Sumber | Menghasilkan Data Nyata | Status |
|----------|--------------|--------|------------------------|--------|
| `AssessmentSession.RemovedAt` | `RemovedAt` (nullable DateTime) | Kolom DB `HcPortalDB_Dev` | Ya — sqlcmd confirm kolom hadir, nullable | MENGALIR |
| `AssessmentAdminController` managementQuery | `managementQuery` IQueryable | `_context.AssessmentSessions.AsNoTracking().Where(RemovedAt==null)` | Ya — EF query ke SQL Server; InMemory test 1/1 aktif terhitung | MENGALIR |
| `AssessmentAdminController` MonitoringDetail | `model.TotalCount` / `model.InProgressCount` | Query dengan `.Where(&&RemovedAt==null)`; count inherit | Ya — InMemory test: TotalCount=1 (bukan 2) terverifikasi | MENGALIR |
| `AssessmentAdminController` UserAssessmentHistory | daftar sesi pekerja | Query tanpa filter `RemovedAt` (SENGAJA boundary) | Ya — sesi removed tetap muncul, test boundary GREEN | MENGALIR (BOUNDARY) |
| `CMPController.IsParticipantRemoved` | `session.RemovedAt` | Entitas dimuat dari SQLEXPRESS nyata via `FindAsync` | Ya — test: `removed.RemovedAt != null` → true; aktif → false | MENGALIR |

---

### Spot-Check Perilaku

| Perilaku | Hasil | Status |
|----------|-------|--------|
| 3 kolom hadir di DB lokal (sqlcmd) | `RemovedAt datetime2 YES NULL`, `RemovedBy nvarchar YES -1`, `RemovalReason nvarchar YES 500` | LULUS |
| Baris existing tidak terdampak (sqlcmd) | COUNT(RemovedAt IS NOT NULL) = 0 | LULUS |
| 6 test ParticipantRemoval GREEN | Passed 6/6 (durasi 9s) | LULUS |
| Full test suite tanpa regresi | Passed 569/569, 0 gagal, 0 dilewati | LULUS |
| ProductVersion snapshot = 8.0.0 | grep konfirmasi `.HasAnnotation("ProductVersion", "8.0.0")` | LULUS |
| Pesan guard tepat 2x di CMPController | grep -c mengembalikan 2 (StartExam + SubmitExam) | LULUS |
| `HasQueryFilter` tidak dipakai | grep di ApplicationDbContext.cs: 0 hasil | LULUS |
| `RemovedAt == null` di AssessmentHub: 3x | grep -c mengembalikan 3 (JoinBatch + SaveTextAnswer + SaveMultipleAnswer) | LULUS |
| `a.RemovedAt == null` di AssessmentAdminController: 3x | grep -c mengembalikan 3 (managementQuery + Monitoring.query + MonitoringDetail.query) | LULUS |
| NO `[MaxLength]` annotation pada 3 prop baru | grep: 0 hasil pada region soft-remove di model | LULUS |

---

### Cakupan Persyaratan

| REQ-ID | Rencana Sumber | Deskripsi | Status | Bukti |
|--------|---------------|-----------|--------|-------|
| PRMV-03 | 409-01-PLAN, 409-02-PLAN | Peserta yang telah dihapus tidak dapat melanjutkan atau mensubmit ujian (guard di StartExam/SubmitExam/Hub.JoinBatch) — jawaban setelah penghapusan tidak terhitung | TERPENUHI | Guard 5 titik terverifikasi (StartExam:924, SubmitExam:1611, SaveAnswer:373, JoinBatch:31, SaveText:146, SaveMultiple:213); 6/6 test GREEN; REQUIREMENTS.md PRMV-03 status Complete |
| PLIV-01 (fondasi) | 409-02-PLAN | Fondasi exclude-query untuk peserta soft-removed dikecualikan dari daftar & perhitungan aktif | TERPENUHI PARSIAL (Fondasi) | 3 query monitoring + Assessment active-list sudah exclude; panel UI "Peserta Dikeluarkan" = Phase 412 (PLIV-01 penuh = pending Phase 412) |

**Catatan traceability REQUIREMENTS.md:** PLIV-01 secara resmi di-mapping ke Phase 412 (bukan 409). Phase 409 hanya menyiapkan fondasi exclude-query (PLIV-01 partial) yang akan dikonsumsi oleh panel UI di Phase 412. Ini sesuai komentar di 409-02 PLAN: "PLIV-01 exclude-foundation landing (panel UI = Phase 412)". Tidak ada REQ yang ter-orphan untuk Phase 409.

---

### Anti-Pattern yang Ditemukan

| File | Baris | Pattern | Severity | Dampak |
|------|-------|---------|----------|--------|
| (tidak ada) | — | — | — | — |

Tidak ada placeholder, stub residual, hardcoded-empty yang mengalir ke UI, atau TODO/FIXME yang memblok goal Phase 409. `IsParticipantRemoved` sempat stub `return false` di Wave-0 commit `cf7838b5` tetapi diimplementasikan penuh di `a0afd785` — bukan stub residual.

---

### Catatan Code Review (Sudah Tertutup)

Code review (409-REVIEW.md) menemukan 2 Warning:

- **WR-01** (tertutup): `SaveAnswer` controller (MC write-path) tidak mem-filter sesi soft-removed — **DIPERBAIKI** di commit `a3fb72ab` dengan guard `IsParticipantRemoved(session)` di CMPController.cs:373.
- **WR-02** (tertutup): Daftar ujian aktif pekerja (`Assessment` action) menampilkan sesi removed sebagai clickable — **DIPERBAIKI** di commit `a3fb72ab` dengan `.Where(a => a.RemovedAt == null)` di CMPController.cs:218 (completedHistory boundary SENGAJA tidak disentuh).

3 Info (IN-01 JoinBatch predicate copy, IN-02 wiring guard via seam bukan action langsung, IN-03 hasInProgress nudge ikut count removed) bersifat non-blocker dan di-defer ke Phase 411/412/413.

---

### Ringkasan

**Goal Phase 409 TERCAPAI PENUH.** Semua kontrak utama terverifikasi terhadap kode aktual (bukan klaim SUMMARY):

1. **Skema DB:** 3 kolom nullable additif (RemovedAt/RemovedBy/RemovalReason) hadir di `AssessmentSession` dan di DB lokal `HcPortalDB_Dev` — sqlcmd konfirmasi langsung. Snapshot ProductVersion 8.0.0 (chain konsisten). Data existing tidak terdampak (0 baris non-null).

2. **Invarian tunggal:** `CMPController.IsParticipantRemoved(session) => session.RemovedAt != null` (:2540) adalah single source of truth — deteksi eksplisit via `RemovedAt`, bukan `Status` (spec §B2 / D-04).

3. **Guard re-entry (6 titik):** StartExam (:924) + SubmitExam (:1611) + SaveAnswer-MC (:373) di controller; JoinBatch (:31) + SaveTextAnswer (:146) + SaveMultipleAnswer (:213) di Hub — semua pakai predikat `RemovedAt == null` / helper `IsParticipantRemoved`. Pesan "Anda telah dikeluarkan dari ujian ini." terpasang di 2 controller endpoint (redirect), silent-skip di Hub.

4. **Exclude-removed:** 4 site total — `managementQuery` (:121), `AssessmentMonitoring.query` (:2825), `AssessmentMonitoringDetail.query` (:3335) di `AssessmentAdminController`; dan `Assessment` active-list (:218) di `CMPController` (WR-02 fix). Tidak ada global `HasQueryFilter` (FORBIDDEN). Boundary `UserAssessmentHistory` SENGAJA tidak disentuh.

5. **Test de-tautologis:** 6/6 GREEN; full suite 569/569 GREEN (0 regresi). Tests memanggil action/query produksi ASLI (InMemory real-controller + SQLEXPRESS disposable), bukan replica predikat.

Review finding WR-01 (SaveAnswer write-path) dan WR-02 (Assessment active-list) sudah ditutup di commit `a3fb72ab` sebelum verifikasi ini.

---

_Diverifikasi: 2026-06-21T02:06:45Z_
_Verifier: Claude (gsd-verifier)_
