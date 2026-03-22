# Architecture Research: Proton Coaching Ecosystem Audit

**Domain:** ASP.NET Core MVC — Brownfield coaching/mentoring platform audit & improvement
**Researched:** 2026-03-22
**Confidence:** HIGH (langsung dari source code: 3 controller, 14 tabel, semua model)

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Presentation Layer                           │
│  ┌──────────────────┐  ┌───────────────────┐  ┌──────────────────┐  │
│  │ AdminController  │  │  CDPController    │  │ ProtonDataCtrl   │  │
│  │  (Setup)         │  │  (Execution &     │  │  (Silabus &      │  │
│  │  12 actions      │  │   Monitoring)     │  │   Guidance)      │  │
│  │                  │  │   20+ actions     │  │  16 actions      │  │
│  └──────┬───────────┘  └────────┬──────────┘  └──────────┬───────┘  │
└─────────┼───────────────────────┼─────────────────────────┼──────────┘
          │                       │                         │
┌─────────▼───────────────────────▼─────────────────────────▼──────────┐
│              Business Logic (inline di controller actions)            │
│  Sequential lock  │  Multi-role approval chain  │  Role-scoped query  │
│  Evidence path    │  Notification trigger        │  Silabus upsert     │
│  Assignment seed  │  Interview result → Final    │  Cascade cleanup    │
└──────────────────────────────────────┬────────────────────────────────┘
                                       │
┌──────────────────────────────────────▼────────────────────────────────┐
│                    Data Layer (EF Core + SQL Server)                   │
│                                                                        │
│  SETUP TABLES              EXECUTION TABLES        COMPLETION TABLES   │
│  ┌────────────────┐        ┌──────────────────┐    ┌────────────────┐  │
│  │ ProtonTrack    │        │ ProtonDeliverable │    │ ProtonFinalAs. │  │
│  │ ProtonKomp.    ├───────►│   Progress        ├───►│ CoachingSession│  │
│  │ ProtonSubKomp. │        │ DeliverableStatus │    │ ActionItem     │  │
│  │ ProtonDeliv.   │        │  History          │    │ ProtonNotif.   │  │
│  │ ProtonTrackAs. │        │ CoachingGuidance  │    │                │  │
│  │ CoachCoachee   │        │  File             │    │                │  │
│  │  Mapping       │        │                   │    │                │  │
│  └────────────────┘        └──────────────────┘    └────────────────┘  │
│                                                                        │
│  LEGACY: CoachingLog (masih ada di DbContext, tidak dipakai di flow)  │
│  PARALLEL: UserNotification (sistem notif umum, beda dari Proton)     │
└────────────────────────────────────────────────────────────────────────┘
```

---

## Component Responsibilities

| Komponen | Tanggung Jawab | File |
|----------|----------------|------|
| **AdminController** | Setup: coach-coachee mapping CRUD, track assignment, import/export mapping, interview results, cascade deactivation | `Controllers/AdminController.cs` (~line 3615+) |
| **CDPController** | Execution: evidence upload, approval chain, CoachingProton list, Dashboard, HistoriProton, deliverable view | `Controllers/CDPController.cs` (3405 baris) |
| **ProtonDataController** | Content: silabus CRUD/import/export, guidance file upload/download/replace, StatusData | `Controllers/ProtonDataController.cs` (1361 baris) |
| **ProtonTrack** | Master track types (TrackType + TahunKe + Urutan display) | `Models/ProtonModels.cs` |
| **CoachCoacheeMapping** | Relasi coach ke coachee, dengan AssignmentSection/Unit override (bisa beda dari unit pekerja) | `Models/CoachCoacheeMapping.cs` |
| **ProtonTrackAssignment** | Penugasan coachee ke track spesifik, IsActive flag, timestamp | `Models/ProtonModels.cs` |
| **ProtonDeliverableProgress** | Status per deliverable per coachee, 3 approval kolom independen | `Models/ProtonModels.cs` |
| **DeliverableStatusHistory** | Audit trail setiap perubahan status deliverable (actor, role, timestamp) | `Models/ProtonModels.cs` |
| **CoachingSession** | Catatan sesi coaching (CatatanCoach, Kesimpulan, Result), terhubung ke DeliverableProgress via nullable no-FK | `Models/CoachingSession.cs` |
| **ActionItem** | Task tindak lanjut per sesi coaching (DueDate, Status) | `Models/ActionItem.cs` |
| **ProtonFinalAssessment** | Rekord penyelesaian track, dibuat saat interview lulus | `Models/ProtonModels.cs` |
| **CoachingGuidanceFile** | File panduan coaching per (Bagian, Unit, TrackId) | `Models/ProtonModels.cs` |

---

## Arsitektur yang Ada: Detail Aktual

### Distribusi 3 Controller

```
AdminController          ProtonDataController        CDPController
(SETUP)                  (CONTENT MANAGEMENT)        (EXECUTION + MONITORING)
├─ CoachCoacheeMapping   ├─ Index (silabus view)      ├─ Index (CDP hub)
├─ CoachCoacheeMappingAs ├─ Override (admin view)     ├─ PlanIdp
├─ CoachCoacheeMappingEd ├─ StatusData                ├─ Dashboard
├─ CoachCoacheeMappingDe ├─ SilabusSave               ├─ FilterCoachingProton
├─ ImportCoachCoacheeMap ├─ SilabusDelete             ├─ GetCascadeOptions
├─ CoachCoacheeMappingEx ├─ SilabusDeactivate         ├─ GetSubCategories
├─ GetEligibleCoachees   ├─ SilabusReactivate         ├─ CoachingProton
├─ SubmitInterviewResult ├─ ExportSilabus             ├─ Deliverable
├─ DeleteWorker (cascade)├─ DownloadSilabusTemplate   ├─ ApproveDeliverable
├─ DeactivateWorker      ├─ ImportSilabus             ├─ RejectDeliverable
└─ ReactivateWorker      ├─ GuidanceList              ├─ HCReviewDeliverable
                         ├─ GuidanceUpload            ├─ UploadEvidence
                         ├─ GuidanceReplace           ├─ SubmitEvidenceWithCoaching
                         └─ GuidanceDelete            ├─ DownloadEvidence
                                                      ├─ HistoriProton
                                                      ├─ HistoriProtonDetail
                                                      └─ ExportHistoriProton
```

### Hirarki Data 4 Level (Silabus)

```
ProtonTrack (master, global — tidak per unit)
    └── ProtonKompetensi [scoped: Bagian + Unit + ProtonTrackId]
            └── ProtonSubKompetensi
                    └── ProtonDeliverable [Urutan → sequential lock trigger]
                            └── ProtonDeliverableProgress [per CoacheeId + ProtonTrackAssignmentId]
                                    ├── DeliverableStatusHistory [append-only audit trail]
                                    └── CoachingSession [nullable ProtonDeliverableProgressId]
                                            └── ActionItem [FK ke CoachingSession]
```

### Multi-Role Approval Chain

```
ProtonDeliverableProgress — 3 kolom approval INDEPENDEN:

  SrSpvApprovalStatus  ─► SrSpv atau SH: CDPController.ApproveDeliverable
  ShApprovalStatus     ─► SH: CDPController.ApproveDeliverable
  HCApprovalStatus     ─► HC: CDPController.HCReviewDeliverable

  Status (overall): "Pending" → "Submitted" → "Approved" / "Rejected"

  Logika: Status = "Approved" hanya jika SEMUA approval kolom = "Approved"
```

### Sistem Role-Scoped Filtering (6 Level)

```
GetCurrentUserRoleLevelAsync() → level int

Level 1-2: Admin / HC      → semua coachee (global)
Level 3:   Dir / VP / Mgr  → coachee di section yang sama via mapping
Level 4:   SH              → coachee di unit yang sama
Level 5:   Coach           → hanya coachee yang di-map ke coach ini
Level 6:   Coachee         → hanya data diri sendiri
```

---

## Data Flow Utama

### Flow 1: Setup — Mapping Coach-Coachee + Auto-Seed

```
AdminController.CoachCoacheeMappingAssign [POST]
    │
    ├─ CoachCoacheeMapping INSERT (per coacheeId yang dipilih)
    │
    ├─ ProtonTrackAssignment INSERT (1 per mapping, IsActive=true)
    │   └─ Jika sudah ada active assignment → skip (guard duplikasi)
    │
    ├─ ProtonDeliverableProgress INSERT (bulk — semua deliverable track)
    │   └─ Hanya jika silabus untuk (Bagian, Unit, TrackId) sudah ada
    │
    └─ AuditLog INSERT

PENTING: Progress di-seed saat mapping dibuat, bukan saat deliverable dibuka.
Implikasi: CoachCoacheeMappingAssign bisa INSERT ratusan progress rows sekaligus.
```

### Flow 2: Execution — Evidence Upload + Sequential Lock Check

```
CDPController.UploadEvidence [POST] atau SubmitEvidenceWithCoaching
    │
    ├─ Validasi sequential: apakah progress sebelumnya (Urutan lebih kecil) sudah Approved?
    │   └─ Jika tidak → reject upload
    │
    ├─ File disimpan ke /uploads/evidence/{progressId}/{filename}
    │
    ├─ ProtonDeliverableProgress UPDATE: EvidencePath, Status="Submitted", SubmittedAt
    │
    ├─ DeliverableStatusHistory INSERT (actor=Coachee)
    │
    └─ ProtonNotification INSERT (ke coach: ada evidence baru)
```

### Flow 3: Approval Chain (Multi-Role)

```
CDPController.ApproveDeliverable [POST]
    │
    ├─ Tentukan role actor: SrSpv → update SrSpvApprovalStatus
    │                       SH    → update ShApprovalStatus
    │
    ├─ Cek apakah semua approval kolom = "Approved"?
    │   YES → Status = "Approved", ApprovedAt = now
    │   NO  → Status tetap "Submitted"
    │
    ├─ DeliverableStatusHistory INSERT (actor=approver)
    │
    ├─ ProtonNotification INSERT (ke coachee: disetujui)
    │
    └─ Cek: apakah SEMUA deliverable dalam track sudah Approved?
        YES → ProtonNotification INSERT ke HC (AllDeliverablesComplete)

CDPController.HCReviewDeliverable [POST] — jalur terpisah untuk HC
    └─ Update HCApprovalStatus (dan recompute Status overall)
```

### Flow 4: Completion — Interview + Final Assessment

```
AdminController.SubmitInterviewResults [POST]
    │
    ├─ CoachingSession.InterviewResultsJson diisi (JSON DTO)
    │
    └─ Jika IsPassed == true:
        ├─ ProtonFinalAssessment INSERT
        │     (ProtonTrackAssignmentId, CoacheeId, Status="Completed",
        │      Notes = "Interview Tahun 3 lulus. Assessor: ...")
        └─ ProtonTrackAssignment.IsActive = false (opsional per logika)
```

### Flow 5: Dashboard Aggregation (In-Memory)

```
CDPController.Dashboard [GET]
    │
    ├─ GetCurrentUserRoleLevelAsync() → tentukan scope coacheeIds
    │
    ├─ ProtonTrackAssignments.Where(active + scope) → activeAssignmentIds
    │
    ├─ ProtonDeliverableProgresses.Where(ids in activeAssignmentIds) → bulk load
    │   [POTENSI MASALAH: over-fetching semua kolom termasuk EvidencePath, RejectionReason]
    │
    ├─ In-memory grouping: per (CoacheeId, ProtonTrackId)
    │   → hitung total / pending / submitted / approved per coachee
    │
    ├─ ProtonFinalAssessments.Where(coacheeIds) → completion flags
    │
    └─ Build ViewModel → Chart.js data (completion rate per track/unit)
```

---

## Pola Query yang Dipakai

### Pattern: Deep 4-Level Include Chain (Diulang 10+ kali di CDPController)

```csharp
_context.ProtonDeliverableProgresses
    .Include(p => p.ProtonDeliverable)
        .ThenInclude(d => d.ProtonSubKompetensi)
            .ThenInclude(s => s.ProtonKompetensi)
                .ThenInclude(k => k.ProtonTrack)
    .Include(p => p.ProtonTrackAssignment)
        .ThenInclude(a => a.ProtonTrack)
    .Where(p => activeAssignmentIds.Contains(p.ProtonTrackAssignmentId))
```

Pola ini muncul di: `Deliverable`, `ApproveDeliverable`, `RejectDeliverable`, `CoachingProton`, `SubmitEvidenceWithCoaching`, dan beberapa tempat lain. Potensi over-fetching jika jumlah progress besar.

### Pattern: GroupBy Latest Assignment (DEF-01 Guard)

```csharp
// Mencegah duplikasi progress saat coachee di-reassign ke track yang sama
var activeAssignmentIds = await _context.ProtonTrackAssignments
    .GroupBy(a => new { a.CoacheeId, a.ProtonTrackId })
    .Select(g => g.OrderByDescending(a => a.AssignedAt).First().Id)
    .ToListAsync();
```

### Pattern: AssignmentUnit Fallback

```csharp
// CoachCoacheeMapping punya override field, jika null fallback ke user.Unit
var resolvedUnit = mapping?.AssignmentUnit ?? user.Unit;
```

Penting untuk filtering CoachingProton — coachee bisa di-assign ke unit berbeda dari unit profil mereka.

---

## Titik Integrasi Audit: New vs Modified

### Komponen yang Kemungkinan DIMODIFIKASI (bukan baru)

| Komponen | Alasan | Controller |
|----------|--------|------------|
| `CDPController.CoachingProton` | Query performance, pagination, filter edge cases | CDPController |
| `CDPController.Dashboard` | In-memory aggregation → kemungkinan perlu projection | CDPController |
| `CDPController.ApproveDeliverable` | Audit approval chain, validasi urutan, notifikasi | CDPController |
| `CDPController.Deliverable` | Tampilan CoachingSession list, UI improvement | CDPController |
| `CDPController.UploadEvidence` | Sequential lock validation detail | CDPController |
| `AdminController.CoachCoacheeMappingAssign` | Audit bulk seed logic, orphan handling | AdminController |
| `ProtonDataController.SilabusSave` | Audit cascade ke existing DeliverableProgress | ProtonDataController |

### Komponen yang Kemungkinan BARU

| Komponen | Deskripsi | Target |
|----------|-----------|--------|
| Dashboard AJAX filter | Filter section/unit/track di Dashboard tanpa full reload (seperti FilterCoachingProton yang sudah ada untuk list view) | CDPController |
| Progress timeline view | Visualisasi urutan deliverable sequential lock — mana yang locked/unlocked | CDPController |
| CoachingSession UI improvement | Form sesi lebih terstruktur: Acuan fields, ActionItem inline add | CDPController views |
| Notifikasi unifikasi | Mapping ProtonNotification → UserNotification agar masuk bell icon yang sama | CDPController |

---

## Batasan Arsitektur yang Harus Diperhatikan

### 1. Tidak Ada FK Explicit di CoachingSession

`CoachingSession.CoachId` dan `.CoacheeId` adalah `string` tanpa FK constraint ke `ApplicationUser`. Ini pola yang disengaja (komentar di model: "no FK — matches CoachingLog pattern"). Konsekuensi: tidak bisa `.Include()` ke ApplicationUser dari CoachingSession — harus manual lookup by Id.

### 2. Dua Sistem Notifikasi Paralel

```
ProtonNotification          UserNotification
(coaching-specific)         (sistem umum, bell icon)
Type = "AllDeliverablesComplete"   berbagai type
Hanya dibaca dari CoachingProton   dibaca dari header/bell
```

Di `ApproveDeliverable`, ada deduplication check yang membaca `UserNotification` (bukan `ProtonNotification`). Ini menunjukkan ada overlap yang belum dikonsolidasi.

### 3. CoachingLog — Tabel Legacy

`CoachingLog` masih ada di DbContext dan di `Models/CoachingLog.cs` tapi tidak digunakan di flow aktif. Bukan blocking issue tapi perlu dicatat saat audit.

### 4. File Storage: Dua Lokasi Terpisah

```
/uploads/evidence/{progressId}/{filename}      ← evidence coachee (UploadEvidence)
/uploads/guidance/{bagian}/{unit}/{filename}   ← panduan HC (GuidanceUpload)
```

Keduanya path-based, disimpan sebagai web-relative path di DB. Tidak ada cleanup otomatis saat record dihapus — orphaned files mungkin terjadi jika mapping didelete tanpa cascade file cleanup.

### 5. Business Logic Inline di Controller

Sequential lock, approval chain, silabus cascade, notification trigger semuanya ada inline di controller. Ini konsisten dengan pola project (bukan anti-pattern untuk project ini), tapi perlu hati-hati saat menambah fitur agar tidak membuat action menjadi lebih dari ~100 baris.

### 6. ProtonDeliverableProgress Seed Pattern (Saat Mapping)

Saat `CoachCoacheeMappingAssign` dipanggil, semua DeliverableProgress langsung di-INSERT untuk semua deliverable dalam track. Artinya:
- Jika silabus berubah setelah assignment → existing progress bisa jadi orphan
- `ProtonDataController.SilabusSave` sudah punya orphan cleanup logic — perlu diverifikasi apakah semua edge case tertangani

---

## Urutan Build yang Benar (Dependency Order untuk Fase Audit)

```
FASE SETUP AUDIT
    Silabus CRUD + Guidance + Coach-Coachee Mapping
    (prerequisite untuk semua fase lain)
    ↓
FASE EXECUTION AUDIT
    Evidence upload + Sequential lock + Deliverable view
    (bergantung pada mapping + assignment yang sudah ada)
    ↓
FASE APPROVAL AUDIT
    Multi-role approval chain (SrSpv → SH → HC)
    DeliverableStatusHistory + Notifikasi
    (bergantung pada evidence submission)
    ↓
FASE COMPLETION AUDIT          FASE MONITORING AUDIT
    Final Assessment               CoachingProton list
    CoachingSession + ActionItem   Dashboard aggregation
    HistoriProton timeline         Export
    (bisa paralel dengan Approval)    (baca dari semua fase di atas)
```

**Implikasi roadmap:**
- Setup harus selesai sebelum Execution bisa ditest end-to-end
- Monitoring/Dashboard hanya baca — bisa diaudit terakhir
- CoachingSession dan ActionItem relatif independen dari approval chain — bisa diaudit paralel

---

## Pertimbangan Skalabilitas

| Skala | Pendekatan |
|-------|-----------|
| 0-200 coachee | Arsitektur saat ini OK. In-memory grouping di Dashboard tidak terasa. |
| 200-1000 coachee | Dashboard aggregation mulai lambat. Ganti `.ToList()` + in-memory group → `.GroupBy()` di SQL. Deep Include chains perlu projection. |
| 1000+ coachee | ProtonDeliverableProgress tumbuh besar (coachee × deliverable per track). Perlu index pada `(ProtonTrackAssignmentId, Status)` dan `(CoacheeId, Status)`. |

**Bottleneck pertama saat ini:** `CDPController.CoachingProton` di line ~1413 — load semua progress dengan activeAssignmentIds (Contains query), lalu grouping di memory.

---

## Anti-Pola yang Harus Dihindari

### Anti-Pola 1: Kolom Approval Ke-4 Langsung di ProtonDeliverableProgress

**Yang dilakukan:** Tambah `VpApprovalStatus` kolom baru langsung di tabel untuk hirarki baru
**Mengapa salah:** Sudah 3 approval kolom. Pendekatan kolom-per-approver tidak skalabel — setiap perubahan hirarki butuh migrasi schema
**Sebaiknya:** Jika approval chain perlu diperluas, gunakan tabel `DeliverableApproval` terpisah dengan kolom `ApproverRole` dan FK ke Progress

### Anti-Pola 2: Query Silabus Tanpa Filter IsActive

**Yang dilakukan:** `_context.ProtonKompetensis.Where(k => k.Bagian == bagian)` tanpa `.Where(k => k.IsActive)`
**Mengapa salah:** Silabus yang dinonaktifkan akan muncul di form evidence dan tampilan coachee
**Sebaiknya:** Selalu tambahkan filter `IsActive == true` untuk ProtonKompetensi queries (ProtonSubKompetensi dan ProtonDeliverable tidak punya IsActive — hanya Kompetensi level)

### Anti-Pola 3: File Evidence Dihapus Sebelum Record DB

**Yang dilakukan:** Delete file di filesystem dulu → baru hapus record
**Mengapa salah:** Jika DB commit gagal, file hilang tapi record masih ada — link rusak
**Sebaiknya:** Hapus record DB dulu, commit berhasil, baru hapus file. Atau: soft-delete record, file cleanup via scheduled task.

### Anti-Pola 4: Load Semua Progress Kolom Untuk Dashboard

**Yang dilakukan:** `.Include(all navigation props).Where(ids).ToList()` untuk dashboard
**Mengapa salah:** Over-fetching — EvidencePath, RejectionReason, dll ikut terbawa ke memory padahal hanya Status yang dibutuhkan untuk statistik
**Sebaiknya:** Projection: `.Select(p => new { p.CoacheeId, p.Status, p.ProtonTrackAssignmentId }).ToListAsync()`

### Anti-Pola 5: Assume AssignmentUnit = User.Unit

**Yang dilakukan:** Gunakan `user.Unit` langsung untuk scoping tanpa cek CoachCoacheeMapping.AssignmentUnit
**Mengapa salah:** CoachCoacheeMapping punya override field — coachee bisa di-assign ke unit berbeda
**Sebaiknya:** Selalu resolusi unit via: `mapping?.AssignmentUnit ?? user.Unit`

---

## Sumber

- Source code langsung: `Controllers/CDPController.cs` (3405 baris), `Controllers/AdminController.cs` (7630 baris), `Controllers/ProtonDataController.cs` (1361 baris)
- Model definitions: `Models/ProtonModels.cs`, `Models/CoachCoacheeMapping.cs`, `Models/CoachingSession.cs`, `Models/ActionItem.cs`, `Models/CoachingLog.cs`
- Project history: `.planning/PROJECT.md` — v8.2 milestone context, semua keputusan arsitektur (CLN-06 dll)
- Confidence: HIGH — semua analisis dari kode aktual di repository, bukan asumsi

---

*Architecture research untuk: Proton Coaching Ecosystem Audit (v8.2)*
*Researched: 2026-03-22*
