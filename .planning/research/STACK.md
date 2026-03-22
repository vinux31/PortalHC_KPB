# Stack Research

**Domain:** Proton Coaching Ecosystem Audit — ASP.NET Core MVC Portal
**Researched:** 2026-03-22
**Confidence:** HIGH — berdasarkan inspeksi langsung codebase + model inventory

---

## Stack yang Sudah Ada (JANGAN GANTI)

| Technology | Version | Role | Status |
|------------|---------|------|--------|
| ASP.NET Core MVC | net8.0 | Web framework, controllers, Razor views | SOLID |
| Entity Framework Core | 8.0.0 | ORM, migrations, LINQ queries | SOLID |
| SQLite | via EF | Database | ADEQUATE untuk skala ini |
| ASP.NET Core Identity | built-in | Auth, roles (Admin/HC/SrSpv/SectionHead/Coach/Coachee) | SOLID |
| SignalR | built-in | Real-time notifikasi (AssessmentHub sudah ada) | ADA, belum dipakai di Proton |
| ClosedXML | 0.105.0 | Excel import/export (Silabus, HistoriProton, Mapping) | SOLID |
| QuestPDF | 2026.2.2 | PDF generation | SOLID |
| Bootstrap 5 | CDN | UI framework | SOLID |
| jQuery | CDN | DOM/AJAX | SOLID |
| Chart.js 4.x | CDN | Dashboard charts | SOLID, sudah ada di beberapa views |
| INotificationService | custom | In-app notification dengan template | SOLID, sudah cover semua Proton events |
| AuditLogService | custom | Audit trail setiap aksi | SOLID |
| FileUploadHelper | custom | File upload pattern (evidence, guidance) | SOLID |
| ExcelExportHelper | custom | ClosedXML boilerplate wrapper | SOLID |

---

## Audit Gap: Apa yang Ada vs Apa yang Perlu Diperbaiki

Tidak perlu menambah library baru. Semua kebutuhan audit dapat dipenuhi dengan pattern dan library yang sudah ada. Yang dibutuhkan adalah **perbaikan implementasi**, bukan **penambahan stack**.

---

## Pola yang Perlu Diterapkan Lebih Konsisten

### 1. Approval Chain Notifications — Sudah Ada, Perlu Audit Coverage

**Kondisi saat ini:** `INotificationService` sudah punya template lengkap untuk seluruh Proton approval events:
- `COACH_ASSIGNED`
- `COACH_EVIDENCE_SUBMITTED`
- `COACH_EVIDENCE_REJECTED`
- `COACH_EVIDENCE_APPROVED_SRSPV`
- `COACH_EVIDENCE_APPROVED_SH`
- `COACH_EVIDENCE_APPROVED_HC`
- `COACH_SESSION_COMPLETED`

**Yang perlu diaudit:** Apakah semua trigger di CDPController dan ProtonDataController benar-benar memanggil `SendByTemplateAsync` pada setiap state transition? Audit per-deliverable status change di `ProtonDeliverableProgress` harus memverifikasi bahwa notifikasi dikirim ke pihak yang tepat.

**Pattern yang digunakan (tidak perlu ganti):**
```csharp
await _notificationService.SendByTemplateAsync(recipientId, "COACH_EVIDENCE_SUBMITTED", new Dictionary<string, object>
{
    { "CoachName", coachName },
    { "CoacheeName", coacheeName }
});
```

---

### 2. DeliverableStatusHistory — Sudah Ada, Perlu Audit Completeness

**Kondisi saat ini:** Model `DeliverableStatusHistory` sudah lengkap dengan:
- `StatusType` (Submitted / SrSpv Approved / SH Approved / HC Reviewed / SrSpv Rejected / SH Rejected / Re-submitted)
- `ActorId`, `ActorName`, `ActorRole` — denormalized untuk audit trail
- `RejectionReason`
- `Timestamp`

**Yang perlu diaudit:** Setiap titik di CDPController yang mengubah status `ProtonDeliverableProgress` harus dipastikan juga menulis ke `DeliverableStatusHistory`. Audit kelengkapan insert ke tabel history ini.

---

### 3. Sequential Deliverable Lock — Sudah Ada, Perlu Audit Logic

**Kondisi saat ini:** Lock logic mencegah coachee submit deliverable berikutnya sebelum yang sebelumnya approved. Status flow: `Pending → Submitted → Approved/Rejected`.

**Yang perlu diaudit:** Edge cases:
- Rejected → kembali ke Pending (apakah UI menampilkan rejection reason dengan jelas?)
- Multi-track assignment (apakah lock per-track atau global?)
- Re-submission setelah rejected (apakah history dicatat sebagai "Re-submitted"?)

---

### 4. Evidence File Handling — Sudah Ada, Perlu Audit Security

**Kondisi saat ini:** `FileUploadHelper` menangani upload. `ProtonDeliverableProgress` menyimpan `EvidencePath` (relative web path) dan `EvidenceFileName` (display name).

**Pattern saat ini:**
```
/uploads/evidence/{progressId}/{filename}
```

**Yang perlu diaudit:**
- Apakah file type allowlist diterapkan (tidak hanya PDF/image)?
- Apakah ada size limit per evidence?
- Apakah path traversal dicegah?
- Apakah file lama dihapus saat re-submit?

**Tidak perlu library baru.** `FileUploadHelper` yang ada sudah handle pattern ini — hanya perlu verifikasi konfigurasinya benar.

---

### 5. Dashboard Analytics — Chart.js Sudah Ada, Perlu Audit Data Accuracy

**Kondisi saat ini:** `CDPController` sudah punya `BuildProtonProgressSubModelAsync` yang menghasilkan data untuk dashboard chart. Chart.js 4.x sudah di-CDN.

**Yang perlu diaudit (lihat bug yang sudah dicatat di CDPController):**
- Filter `IsActive` pada coachee queries
- Status counting (Pending vs Submitted vs Approved distinction)
- Role-scoped filtering (Coach hanya melihat coachee-nya sendiri)
- SrSpv dan SectionHead hanya melihat scope unit mereka

**Pattern yang sudah proven:**
```csharp
// ViewBag.ChartData = new { labels, datasets }
// Di view: Chart.js render dari ViewBag JSON
```

---

### 6. SignalR untuk Real-Time Proton Updates — ADA tapi BELUM DIPAKAI

**Kondisi saat ini:** `AssessmentHub` sudah ada dan SignalR sudah di-register di `Program.cs`. Namun Proton coaching belum menggunakan SignalR — semua notifikasi adalah in-app polling via `INotificationService`.

**Rekomendasi untuk audit:** Jangan tambahkan SignalR ke Proton sekarang. In-app notification yang ada sudah cukup untuk coaching workflow. SignalR dibutuhkan hanya untuk skenario high-urgency real-time (seperti exam monitoring). Coaching approval bisa tunggu page refresh normal.

**Alasan tidak tambah SignalR ke Proton:**
- Coaching approval bukan time-critical seperti exam monitoring
- In-app notification bell sudah ada dan bekerja
- Menambah SignalR hub baru = menambah complexity maintenance
- ROI rendah dibanding effort

---

### 7. CoachingSession & ActionItem — Sudah Ada, Perlu Audit Linkage

**Kondisi saat ini:**
- `CoachingSession` sudah linked ke `ProtonDeliverableProgressId` (nullable FK)
- `ActionItem` linked ke `CoachingSession`
- Session status: "Draft" → "Submitted"

**Yang perlu diaudit:**
- Apakah semua CoachingSession yang ada punya `ProtonDeliverableProgressId` terisi?
- Apakah ada orphaned sessions (linked ke deliverable yang sudah dihapus)?
- Apakah ActionItem DueDate ditampilkan dengan warning jika overdue?

---

## Apa yang TIDAK Perlu Ditambahkan

| Jangan Tambah | Kenapa |
|---------------|--------|
| Workflow engine (Elsa, Windows Workflow) | Over-engineering; approval chain sudah ter-handle via status fields di ProtonDeliverableProgress |
| Hangfire / background job | Tidak ada scheduled job yang dibutuhkan Proton audit; jika nanti diperlukan, IHostedService sudah cukup |
| Redis / cache layer | Volume data terlalu kecil; EF query sudah cukup cepat |
| SignalR hub baru untuk Proton | In-app notification yang ada sudah cukup untuk coaching approval flow |
| Email SMTP (MailKit) | Sudah dipertimbangkan di v8.1; coaching tidak butuh email — in-app notification cukup |
| React/Vue untuk coaching UI | Overkill; Razor + jQuery + AJAX sudah proven di seluruh codebase |
| AutoMapper | Tidak sesuai style; explicit mapping lebih readable |
| MediatR / CQRS | Over-engineering untuk skala dan pattern yang ada |
| File storage cloud (Azure Blob, S3) | Evidence file di wwwroot/uploads sudah adequate untuk volume internal Pertamina KPB |

---

## Integration Points dengan Stack yang Ada

| Area | Stack yang Dipakai | Integration Point |
|------|-------------------|-------------------|
| Approval state change | EF Core + `ProtonDeliverableProgress` | Setiap state change → tulis `DeliverableStatusHistory` → kirim `INotificationService` |
| Dashboard stats | EF Core LINQ + Chart.js | `BuildProtonProgressSubModelAsync` → ViewBag → Chart.js render |
| Evidence upload | `FileUploadHelper` + wwwroot/uploads | Upload → simpan path di `EvidencePath` |
| Silabus management | ClosedXML (import/export) + EF | 3-level hierarchy (Kompetensi → SubKompetensi → Deliverable) |
| Coach-Coachee setup | EF + Admin CRUD | `CoachCoacheeMapping` → `ProtonTrackAssignment` cascade |
| Completion + Final Assessment | EF + `ProtonFinalAssessment` | HC creates record → triggers `COACH_SESSION_COMPLETED` notification |
| Audit trail | `AuditLogService` | Setiap aksi admin/HC/approval → log entry |
| History timeline | `DeliverableStatusHistory` | HistoriProton view renders timeline dari tabel ini |

---

## Pola Instalasi

Tidak ada package baru yang perlu diinstall untuk milestone ini.

Semua yang dibutuhkan sudah ada di `HcPortal.csproj`. Audit cukup menggunakan:
- EF Core migration (jika perlu tambah kolom)
- Razor view update
- Controller action update
- Service method update

---

## Alternatives Considered

| Recommended | Alternative | Why Not |
|-------------|-------------|---------|
| INotificationService yang ada | SignalR push untuk Proton | Coaching approval tidak time-critical; polling bell sudah cukup |
| Status field di ProtonDeliverableProgress | Dedicated workflow engine | Status machine sudah encoded di fields; engine menambah dependency besar |
| FileUploadHelper yang ada | Azure Blob Storage | Deployment internal; cloud storage tidak available/dibutuhkan |
| DeliverableStatusHistory yang ada | Generic AuditLog saja | Coaching perlu history granular per-deliverable — tabel dedicated lebih queryable |

---

## Sources

- Inspeksi langsung `Models/ProtonModels.cs` — confirmed data model approval chain
- Inspeksi langsung `Services/INotificationService.cs` + `Services/NotificationService.cs` — confirmed template coverage
- Inspeksi langsung `Controllers/CDPController.cs` — confirmed bug history dan existing patterns
- Inspeksi langsung `Models/CoachingSession.cs`, `Models/ActionItem.cs`, `Models/CoachCoacheeMapping.cs`
- `Controllers/ProtonDataController.cs` — confirmed Admin/HC override pattern

---
*Stack research untuk: Proton Coaching Ecosystem Audit (v8.2)*
*Researched: 2026-03-22*
