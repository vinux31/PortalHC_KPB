# Phase 235: Audit Execution Flow - Research

**Researched:** 2026-03-22
**Domain:** ASP.NET Core MVC — CDPController execution flow (evidence, approval chain, status history, notifikasi, PlanIdp)
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

**Evidence & Resubmit**
- D-01: Coach yang upload evidence, bukan coachee
- D-02: File lama dipertahankan saat resubmit — evidence baru ditambah sebagai versi terbaru, file lama tetap di server sebagai history. Fix: simpan history path di DB agar traceable
- D-03: Concurrent upload: last-write-wins
- D-04: Single file per deliverable — sesuai model saat ini
- D-05: Rejection reason wajib diisi oleh approver — sudah ada di codebase (L897 CDPController)
- D-06: Resubmit tanpa batas
- D-07: Approval chain reset dari awal saat resubmit — sudah di-implement (L935-943)
- D-08: File validation server-side: PDF/JPG/PNG, max 10MB — sudah ada (L1099-1112)
- D-09: Upload gagal: rollback + error message — jangan update status deliverable

**Approval Chain Consistency**
- D-10: Race condition: first-write-wins — cek status sebelum update, approve kedua mendapat error "sudah diproses"
- D-11: Admin Override: Claude investigasi di codebase apakah ada override flow dan audit consistency-nya
- D-12: Status per-level dan chain flow: sesuai existing — co-sign pattern dipertahankan
- D-13: Status "Approved" prematur (SrSpv approve langsung set overall Approved) — TIDAK di-fix
- D-14: Fix notification dedup fragile — `CreateHCNotificationAsync` pakai `Message.Contains()`, ganti dengan structured field check

**Status History & Notifikasi**
- D-15: StatusHistory insert di SEMUA transisi: Initial Pending, Evidence Upload/Submit, Resubmit after reject, Approve, Reject, HC Review
- D-16: Gap: `UploadEvidence` (L1086) tidak record StatusHistory — hanya `SubmitEvidenceWithCoaching`. Fix: tambah RecordStatusHistory di UploadEvidence
- D-17: Initial "Pending" insert saat ProtonDeliverableProgress pertama kali di-seed
- D-18: Tambah notifikasi resubmit — kirim notif khusus saat evidence diresubmit setelah reject

**PlanIdp View Accuracy**
- D-19: Audit general — verifikasi silabus display, guidance tabs, role filtering semua benar
- D-20: Coach role access — pastikan Coach hanya lihat data coachee yang di-map ke mereka
- D-21: Inactive silabus filtering — pastikan PlanIdp tidak tampilkan silabus/deliverable yang IsActive=false
- D-22: Guidance tab access — pastikan coachee tidak bisa akses admin guidance management tab

### Claude's Discretion
- Evidence path history storage mechanism (new column vs separate table)
- Notification dedup structured field approach
- Exact implementation of first-write-wins race condition guard
- PlanIdp audit detail findings dan fix approach

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| EXEC-01 | Audit Evidence submission flow end-to-end — upload, reject+resubmit, multi-file handling, verifikasi completeness | D-01 sampai D-09: role check Coach, file validation, status guard, history gap di UploadEvidence |
| EXEC-02 | Audit Approval chain — verifikasi state consistency di edge cases (concurrent approve, Override admin, partial approval) | D-10 sampai D-13: race condition guard, co-sign pattern, ApproveDeliverable code audit |
| EXEC-03 | Audit DeliverableStatusHistory — verifikasi completeness insert di setiap state transition termasuk initial Pending | D-15 sampai D-17: gap di UploadEvidence (L1165) dan seed di AdminController (L6838) |
| EXEC-04 | Audit Notifikasi — verifikasi semua Proton notification triggers terpanggil (evidence submit, approve, reject, HC review, final assessment) | D-14, D-18: dedup fragile, missing resubmit notif, HC review tidak punya trigger |
| EXEC-05 | Audit PlanIdp view — silabus display accuracy, guidance tabs, role-based access correctness | D-19 sampai D-22: inactive filter, coach data scoping, tab access control |
</phase_requirements>

---

## Summary

Phase 235 adalah audit kode CDPController yang berfokus pada jalur operasional harian Proton: upload evidence, approval chain, audit trail status, notifikasi, dan tampilan PlanIdp. Audit dilakukan melalui pembacaan kode aktual, identifikasi gap, dan perbaikan langsung di controller/view yang sama.

Berdasarkan pembacaan kode saat ini, ditemukan empat gap konkret yang harus diperbaiki: (1) `UploadEvidence` tidak memanggil `RecordStatusHistory`, (2) seed `ProtonDeliverableProgress` di `AdminController.SeedProtonDeliverableProgressAsync` tidak insert StatusHistory "Pending" awal, (3) `CreateHCNotificationAsync` menggunakan `Message.Contains(coacheeId)` untuk dedup yang rentan salah match jika coacheeId pendek, (4) tidak ada notifikasi khusus saat resubmit setelah reject. Race condition guard di `ApproveDeliverable` sudah ada (cek `canApprove`) namun tidak atomik — perlu pengamanan tambahan via reload-before-write pattern. PlanIdp sudah memiliki sebagian besar guard yang benar, dengan satu potensi gap pada `SubKompetensiList.Deliverables` yang tidak memfilter IsActive pada ProtonDeliverable.

**Primary recommendation:** Semua perbaikan dilakukan sebagai audit + targeted fix di CDPController.cs, AdminController.cs, dan Views/CDP/PlanIdp.cshtml — tidak memerlukan library baru, schema migration besar, atau perubahan arsitektur.

---

## Standard Stack

### Core (sudah ada di project — tidak perlu install baru)
| Component | Lokasi | Purpose |
|-----------|--------|---------|
| `RecordStatusHistory` | CDPController:3021 | Private helper untuk insert DeliverableStatusHistory — tinggal panggil di titik yang belum ada |
| `_notificationService.SendAsync()` | CDPController (injected) | Kirim UserNotification — tinggal tambah trigger point baru |
| `FileUploadHelper.SaveFileAsync()` | Helpers/FileUploadHelper.cs | Upload file ke wwwroot — sudah dipakai di UploadEvidence L1143 |
| EF Core transaction pattern | Phase 234 carry-forward | `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` — untuk upload rollback (D-09) |

**Tidak perlu instalasi baru** — semua komponen sudah tersedia.

---

## Architecture Patterns

### Pattern 1: StatusHistory Insertion

Setiap transisi state harus memanggil `RecordStatusHistory` sebelum `SaveChangesAsync`. Helper ini syncronous (tidak async) dan langsung menambahkan entitas ke EF context:

```csharp
// Source: CDPController:3021
private void RecordStatusHistory(int progressId, string statusType, string actorId, string actorName, string actorRole, string? rejectionReason = null)
{
    _context.DeliverableStatusHistories.Add(new DeliverableStatusHistory { ... });
}
```

Gap saat ini:
- `UploadEvidence` (L1165) memanggil `SaveChangesAsync` tanpa `RecordStatusHistory` sebelumnya
- Seed (`AdminController:6838`) memanggil `AddRange` tanpa insert StatusHistory

### Pattern 2: Race Condition Guard (first-write-wins untuk D-10)

Pola yang AMAN untuk concurrent approve adalah **reload-before-write**: load ulang status dari DB di dalam blok yang sama, cek ulang, baru update. Saat ini `ApproveDeliverable` memuat progress sekali lalu langsung menulis — tidak ada jaminan atomik.

Implementasi yang direkomendasikan (sesuai discretion):
```csharp
// Reload fresh from DB immediately before write
var freshProgress = await _context.ProtonDeliverableProgresses
    .FirstOrDefaultAsync(p => p.Id == progressId);
if (freshProgress.Status != "Submitted")
{
    TempData["Error"] = "Deliverable sudah diproses oleh approver lain.";
    return RedirectToAction("Deliverable", new { id = progressId });
}
// Lanjutkan update ...
```

Catatan: aplikasi ini tidak menggunakan SignalR atau background job paralel — race condition hanya terjadi jika dua approver membuka halaman sama dan submit hampir bersamaan. Reload-before-write (tanpa SELECT FOR UPDATE) adalah solusi proporsional.

### Pattern 3: Evidence Path History Storage (D-02)

Pilihan yang direkomendasikan untuk menyimpan file history: **new column** `EvidencePathHistory` (string, nullable) di `ProtonDeliverableProgress` — serialized sebagai JSON array of strings (e.g. `["/uploads/evidence/5/v1.pdf", "/uploads/evidence/5/v2.pdf"]`). Alasannya:

- Model sudah tidak pakai FK ketat — kolom string baru konsisten dengan pola project
- Tidak perlu tabel baru, join query baru, atau migration kompleks
- History cukup untuk audit trail; tidak perlu query individual per entry

Alternatif (separate table `EvidencePathHistory`) lebih fleksibel tapi overkill untuk single-file deliverable.

### Pattern 4: Notification Dedup (D-14)

Dedup saat ini:
```csharp
// FRAGILE — coacheeId mungkin match sebagian string lain
bool alreadyNotified = await _context.UserNotifications
    .AnyAsync(n => n.Type == "COACH_ALL_COMPLETE" && n.Message.Contains(coacheeId));
```

Fix yang direkomendasikan (structured field check):
```csharp
// SAFE — match exact RecipientId + Type + CoacheeId via dedicated field
bool alreadyNotified = await _context.UserNotifications
    .AnyAsync(n => n.Type == "COACH_ALL_COMPLETE"
                && n.RecipientId == hc.Id
                && n.RelatedEntityId == coacheeId);
```

Perlu cek apakah `UserNotification` model punya field `RelatedEntityId`. Jika tidak, gunakan exact string matching: `n.Message == $"Semua deliverable {coacheeName} telah selesai"` (lebih deterministik dari `.Contains(coacheeId)`).

### Pattern 5: PlanIdp — Inactive Deliverable Filtering (D-21)

Kode saat ini di PlanIdp (L118-143): `ProtonKompetensi` difilter dengan `k.IsActive` — benar. Namun `ProtonDeliverable` tidak punya field `IsActive` di model (konfirmasi dari ProtonModels.cs L56-64). Ini berarti D-21 partial: silabus inactive difilter di level Kompetensi, tapi jika kebutuhan filtering ada di level SubKompetensi atau Deliverable, model saat ini tidak mendukungnya. **Tidak perlu tambah IsActive ke ProtonDeliverable** karena di luar scope — cukup verifikasi filter Kompetensi sudah cukup.

### Anti-Patterns yang Harus Dihindari
- **Fire-and-forget notification tanpa rollback main op**: sudah benar — semua notif di-wrap `try-catch`
- **SaveChangesAsync dua kali dalam satu request**: hindari jika bisa digabung satu transaksi
- **Nested Include chain berulang untuk data yang sama**: SubmitEvidenceWithCoaching sudah include lengkap di awal, jangan query ulang

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| Status history insert | Custom audit table builder | `RecordStatusHistory` helper yang sudah ada |
| Notification send | Direct DB insert ke UserNotifications | `_notificationService.SendAsync()` |
| File upload | Manual File.WriteAllBytes | `FileUploadHelper.SaveFileAsync()` |
| Role check | Custom string comparison | `UserRoles.GetRoleLevel()`, `HasSectionAccess()`, `HasFullAccess()` |

---

## Common Pitfalls

### Pitfall 1: UploadEvidence — History Gap
**What:** `UploadEvidence` memanggil `SaveChangesAsync` (L1165) tanpa `RecordStatusHistory` sebelumnya
**Why it happens:** Action ini lebih tua dari Phase 117 (saat StatusHistory diperkenalkan) dan tidak di-update
**How to avoid:** Tambah `RecordStatusHistory(progress.Id, statusType, ...)` sebelum `SaveChangesAsync` di L1165. `statusType` = `"Submitted"` (first upload) atau `"Re-submitted"` (jika `wasRejected == true`)
**Warning sign:** DeliverableStatusHistory tidak memiliki entry untuk progress yang pernah melalui UploadEvidence

### Pitfall 2: Seed tanpa StatusHistory
**What:** `AdminController.SeedProtonDeliverableProgressAsync` (L6829-6838) melakukan `AddRange` tanpa menyertakan DeliverableStatusHistory "Pending" awal
**Why it happens:** StatusHistory baru ditambahkan Phase 117, seed tidak di-update
**How to avoid:** Setelah `AddRange(progresses)`, loop melalui ID yang baru dibuat dan insert `RecordStatusHistory` dengan `statusType = "Pending"`. Harus dilakukan SETELAH `SaveChangesAsync` pertama untuk mendapatkan generated ID, atau gunakan EF tracking untuk mengakses `Id` setelah save

### Pitfall 3: Dedup Fragile di CreateHCNotificationAsync
**What:** `.Message.Contains(coacheeId)` — jika coacheeId adalah substring dari ID lain, bisa false positive
**Why it happens:** Field relasi tidak tersedia di UserNotification saat notification dikirim via generic service
**How to avoid:** Gunakan field match yang lebih spesifik, atau simpan `coacheeId` di `RelatedEntityId` jika field tersedia

### Pitfall 4: Missing Resubmit Notification (D-18)
**What:** Saat Coach resubmit via `UploadEvidence` (wasRejected = true), `NotifyReviewersAsync` dipanggil — namun ini mengirim notif "Deliverable Disubmit", bukan notif khusus resubmit
**How to avoid:** Tambah `notifType` parameter ke NotifyReviewersAsync, atau tambah conditional: jika `wasRejected`, kirim `"COACH_EVIDENCE_RESUBMITTED"` dengan pesan "Deliverable diresubmit setelah ditolak"

### Pitfall 5: ProtonDeliverableProgress seed di dua tempat
**What:** Seed terjadi di `AdminController.SeedProtonDeliverableProgressAsync` DAN di `ProtonDataController` (L492). Keduanya harus mendapat update StatusHistory insert
**How to avoid:** Cari semua lokasi seed dengan grep pattern `ProtonDeliverableProgresses.Add`

---

## Code Examples

### Menambah RecordStatusHistory di UploadEvidence
```csharp
// Di CDPController.UploadEvidence, sebelum SaveChangesAsync (L1165)
bool wasRejected = progress.Status == "Rejected";
// ... (existing update fields) ...

// TAMBAHKAN:
string uploadStatusType = wasRejected ? "Re-submitted" : "Submitted";
RecordStatusHistory(progress.Id, uploadStatusType, user.Id, user.FullName, "Coach");

await _context.SaveChangesAsync();
```

### Menambah StatusHistory "Pending" di Seed
```csharp
// Di AdminController, setelah AddRange dan SaveChangesAsync
_context.ProtonDeliverableProgresses.AddRange(progresses);
await _context.SaveChangesAsync(); // Diperlukan untuk mendapat generated IDs

// TAMBAHKAN — system actor untuk initial seed:
foreach (var p in progresses)
{
    _context.DeliverableStatusHistories.Add(new DeliverableStatusHistory
    {
        ProtonDeliverableProgressId = p.Id,
        StatusType = "Pending",
        ActorId = assignedById,  // atau system user ID
        ActorName = "System",
        ActorRole = "System",
        Timestamp = DateTime.UtcNow
    });
}
await _context.SaveChangesAsync();
```

Catatan: Ini menyebabkan dua `SaveChangesAsync` — acceptable karena seed bukan hot path.

### Fix Dedup CreateHCNotificationAsync
```csharp
// SEBELUM (fragile):
bool alreadyNotified = await _context.UserNotifications
    .AnyAsync(n => n.Type == "COACH_ALL_COMPLETE" && n.Message.Contains(coacheeId));

// SESUDAH — exact message match (deterministik):
var expectedMessage = $"Semua deliverable {coacheeName} telah selesai";
bool alreadyNotified = await _context.UserNotifications
    .AnyAsync(n => n.RecipientId == hc.Id
                && n.Type == "COACH_ALL_COMPLETE"
                && n.Message == expectedMessage);
```

### Race Condition Guard di ApproveDeliverable
```csharp
// Reload status fresh sebelum commit (first-write-wins)
// Letakkan setelah set per-role approval fields, sebelum set overall Status
var currentStatus = await _context.ProtonDeliverableProgresses
    .Where(p => p.Id == progressId)
    .Select(p => new { p.Status, p.SrSpvApprovalStatus, p.ShApprovalStatus })
    .FirstOrDefaultAsync();

if (currentStatus == null) return NotFound();

// Re-check guard dengan data fresh
bool stillCanApprove = currentStatus.Status == "Submitted" ||
    (currentStatus.Status == "Approved" && (
        (isSrSpv && currentStatus.SrSpvApprovalStatus != "Approved") ||
        (isSH && currentStatus.ShApprovalStatus != "Approved")));
if (!stillCanApprove)
{
    TempData["Error"] = "Deliverable sudah diproses oleh approver lain.";
    return RedirectToAction("Deliverable", new { id = progressId });
}
```

---

## Gaps & Findings dari Code Audit

### EXEC-01: Evidence Submission Flow

| Item | Status | Temuan |
|------|--------|--------|
| Role check Coach-only | PASS | L1122 — `uploadUserRole != UserRoles.Coach` → Forbid() |
| IDOR guard | PASS | L1128-1133 — CoachCoacheeMapping check |
| File validation (ext + size) | PASS | L1099-1112 |
| Status guard (Pending/Rejected) | PASS | L1136-1140 |
| Approval chain reset saat resubmit | PASS | L1152-1163 |
| StatusHistory insert di UploadEvidence | **GAP** | L1165 SaveChanges tanpa RecordStatusHistory |
| Upload rollback jika file save gagal | **GAP** | Tidak ada try-catch sekitar FileUploadHelper.SaveFileAsync (L1143) — jika throws, status tidak berubah tapi tidak ada explicit rollback message |
| SubmitEvidenceWithCoaching StatusHistory | PASS | L2036-2037 |
| Evidence path history (D-02) | **MISSING** | Tidak ada mekanisme menyimpan path lama — `EvidencePath` langsung di-overwrite |

### EXEC-02: Approval Chain Consistency

| Item | Status | Temuan |
|------|--------|--------|
| Guard status sebelum approve | PASS | L771-779 — `canApprove` flag |
| Reset chain saat reject | PASS | L935-943 |
| Section check | PASS | L782-789 |
| Race condition atomik | **PARTIAL** | Guard ada tapi berbasis data yang di-load sebelum set fields — non-atomik |
| Admin override flow | Perlu investigasi | Tidak ditemukan dedicated "override" action — Admin dapat Forbid bypass via `userRole != UserRoles.Admin` di section check saja |

### EXEC-03: DeliverableStatusHistory

| Titik Transisi | Punya History? |
|----------------|----------------|
| Initial Pending (seed AdminController) | **TIDAK** — L6838 tidak insert history |
| Initial Pending (seed ProtonDataController L492) | **TIDAK** |
| Submit via SubmitEvidenceWithCoaching | YA — L2036-2037 |
| Submit via UploadEvidence | **TIDAK** — L1165 gap |
| Approve SrSpv/SH | YA — L835 |
| Reject | YA — L949 |
| HC Review | YA — L1076 |
| Resubmit via SubmitEvidenceWithCoaching | YA — L2036 (`"Re-submitted"`) |
| Resubmit via UploadEvidence | **TIDAK** |

### EXEC-04: Notifikasi

| Trigger | Status |
|---------|--------|
| Evidence submit (SrSpv/SH) | PASS — NotifyReviewersAsync L1173 (UploadEvidence), L2083 (SubmitEvidenceWithCoaching) |
| Approve — notify Coach | PASS — L855-862 |
| Approve — notify Coachee | PASS — L863-870 |
| Reject — notify Coach | PASS — L966-971 |
| Reject — notify Coachee | PASS — L974-980 |
| HC Review | **MISSING** — HCReviewDeliverable (L1048) tidak panggil notifikasi apapun |
| All Deliverables Complete → HC | PASS tapi fragile — CreateHCNotificationAsync L1016 ada, dedup via Message.Contains |
| Resubmit after reject (notif khusus) | **MISSING** — hanya kirim notif "Submitted" biasa |

### EXEC-05: PlanIdp View

| Item | Status |
|------|--------|
| Silabus filter IsActive (Kompetensi level) | PASS — L121: `k.IsActive` |
| Silabus SubKompetensi/Deliverable tidak ada IsActive | N/A — model tidak punya field IsActive di level itu |
| Coachee lock ke track mereka | PASS — L82-86 force override URL params |
| L4 lock ke section | PASS — L102-107 |
| Guidance filter Coachee (bagian-only) | PASS — L151-152 |
| Guidance filter L4 (section-only) | PASS — L153-154 |
| Coach: guidance tidak difilter per mapping | Perlu verifikasi — guidanceQuery tidak ada coach-specific filter (L148-154 hanya handle isCoachee dan isL4) |
| Admin guidance management tab access | Perlu verifikasi di View — server menyediakan UserLevel via ViewBag.UserLevel, tab hiding logic di Razor |

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no xUnit/MSTest detected) |
| Config file | None — tidak ada test project |
| Quick run command | Browser: load halaman yang relevan |
| Full suite command | Browser UAT per use-case flow |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Command |
|--------|----------|-----------|---------|
| EXEC-01 | Coach upload evidence → status berubah ke Submitted, ReviewersAsync dipanggil | Manual | Login sebagai Coach, upload ke deliverable Pending |
| EXEC-01 | Resubmit setelah reject → approval chain reset, file lama tersimpan | Manual | Reject deliverable, coach upload ulang |
| EXEC-02 | Dua approver approve hampir bersamaan → hanya satu sukses, satu dapat error | Manual | Dua browser session, approve bersamaan |
| EXEC-02 | Admin approve cross-section | Manual | Login Admin, approve deliverable section lain |
| EXEC-03 | Seed progress → StatusHistory "Pending" ada | Manual/DB | Assign track baru, cek tabel DeliverableStatusHistories |
| EXEC-03 | Upload via UploadEvidence → StatusHistory "Submitted" ada | Manual/DB | Upload evidence, cek tabel |
| EXEC-04 | HC Review → notifikasi terkirim (jika ada requirement notif HC review) | Manual | Review deliverable sebagai HC, cek UserNotifications |
| EXEC-04 | All approved → HC notifikasi dengan dedup benar | Manual | Approve semua deliverable coachee, trigger dua kali |
| EXEC-05 | Coachee lihat PlanIdp → hanya track miliknya | Manual | Login Coachee, akses /CDP/PlanIdp |
| EXEC-05 | L4 lihat PlanIdp → hanya section mereka | Manual | Login SrSpv, akses /CDP/PlanIdp |

### Wave 0 Gaps
- Tidak ada test infrastructure formal — semua validasi via manual browser testing sesuai pola project

---

## Open Questions

1. **Admin Override flow (D-11)**
   - Yang diketahui: Admin di-bypass section check di ApproveDeliverable (L786), namun tidak ada action terpisah bernama "AdminOverride"
   - Yang tidak jelas: Apakah ada ProtonDataController.OverrideStatus atau similar?
   - Rekomendasi: Grep codebase untuk `Override` di CDPController dan ProtonDataController sebelum membuat task

2. **UserNotification model — apakah ada field RelatedEntityId?**
   - Yang diketahui: Model tidak dibaca dalam sesi ini
   - Rekomendasi: Baca `Models/NotificationModels.cs` atau grep field names sebelum implement fix dedup

3. **Coach access ke Guidance tab di PlanIdp**
   - Yang diketahui: Server tidak filter guidance per Coach mapping — kirim semua guidance yang sesuai bagian/section
   - Yang tidak jelas: Apakah ini intended? Coach mungkin perlu lihat semua guidance untuk bagiannya
   - Rekomendasi: Anggap ini acceptable (Coach perlu akses guidance), fokus audit pada tab admin management saja

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CDPController.cs` — dibaca langsung: UploadEvidence (L1086-1177), ApproveDeliverable (L746-880), RejectDeliverable (L884-986), HCReviewDeliverable (L1048-1082), SubmitEvidenceWithCoaching (L1943-2106), RecordStatusHistory (L3021-3033), PlanIdp (L57-208), NotifyReviewersAsync (L988-1014), CreateHCNotificationAsync (L1016-1044)
- `Models/ProtonModels.cs` — dibaca langsung: semua model fields termasuk konfirmasi tidak ada `IsActive` di ProtonDeliverable
- `Controllers/AdminController.cs` — dibaca langsung: SeedProtonDeliverableProgressAsync (L6800-6840)
- `.planning/phases/235-audit-execution-flow/235-CONTEXT.md` — keputusan locked D-01 sampai D-22

### Secondary (MEDIUM confidence)
- `Controllers/ProtonDataController.cs` — dilihat sepintas via grep: seed kedua di L492

---

## Metadata

**Confidence breakdown:**
- EXEC-01 gaps: HIGH — dibaca langsung dari kode, gap UploadEvidence terkonfirmasi
- EXEC-02 race condition: HIGH — pattern non-atomik teridentifikasi, Admin override perlu investigasi lebih
- EXEC-03 history gaps: HIGH — dua titik seed tanpa history terkonfirmasi
- EXEC-04 notif gaps: HIGH — HC Review tidak ada notif, resubmit notif tidak ada
- EXEC-05 PlanIdp: MEDIUM — sebagian besar guard sudah ada, Coach guidance filter dan admin tab access perlu verifikasi view

**Research date:** 2026-03-22
**Valid until:** 60 hari (kode stabil, tidak ada dependency eksternal berubah)
