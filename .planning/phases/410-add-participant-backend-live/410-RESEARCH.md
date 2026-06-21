# Phase 410: Add-Participant Backend Live - Research

**Researched:** 2026-06-21
**Domain:** ASP.NET Core MVC backend (C#, EF Core, SQL Server) — endpoint AJAX penambahan peserta assessment live, reuse mesin sesi/assignment existing
**Confidence:** HIGH (semua klaim file:line di-VERIFIED via Grep/Read di repo; nol asumsi training untuk perilaku kode)

## Summary

Phase 410 menambah 2 endpoint ke `Controllers/AssessmentAdminController.cs`: `AddParticipantsLive` (HttpPost) dan `GetEligibleParticipantsToAdd` (HttpGet). Tidak ada migration (semua kolom sudah ada; 409 sudah menambah kolom removal). Riset menemukan bahwa **mesin pembuatan sesi peserta sudah ada dalam 3 varian** — BULK ASSIGN (`EditAssessment` `:2151-2272`, standard only), Pre/Post create (`EditAssessment` `:1942-1998` + canonical `CreateAssessment` `:1235-1320`), dan pola transaksi atomic `InjectAssessmentService.InjectBatchAsync` (`:88-421`). `DeriveReadyStatus` (Phase 391, `:2276-2283`) sudah ada dan siap dipakai langsung.

**Temuan paling penting untuk planner:** `UserPackageAssignment` **TIDAK dibuat saat assign awal**. Ia dibuat **lazy** oleh `CMPController.StartExam` (`:1064-1102`) saat worker pertama kali membuka ujian (mode paket). Jalur BULK ASSIGN (`:2151`) dan Pre/Post create (`:1942`) **hanya** membuat `AssessmentSession` — bukan `UserPackageAssignment`, bukan `AssessmentPackage`. Ini berarti CONTEXT/spec "buat `UserPackageAssignment` otomatis" perlu keputusan eksplisit: (A) ikuti pola existing = sesi saja, assignment lazy saat worker StartExam (paling konsisten, zero-regresi), ATAU (B) eager-create assignment via `ShuffleEngine.BuildQuestionAssignment` (pola `:1074` / `:5565`). Rekomendasi: **opsi A** — peserta baru otomatis sama persis dengan peserta assign-awal; tidak ada peserta existing yang punya `UserPackageAssignment` sebelum StartExam, jadi opsi A sudah memenuhi "siap-mulai".

**Primary recommendation:** Buat helper privat bersama `BuildReadyParticipantSession(repSession, model-or-rep, userId, actorId)` yang meng-emit `AssessmentSession` ber-status `DeriveReadyStatus`, dibungkus 1 `BeginTransactionAsync` (pola `InjectBatchAsync`). Untuk Pre/Post, reuse pola pair-create `:1942-1998`. Eligible query = `_context.Users.Where(u => u.IsActive)` minus userId yang sudah punya sesi APAPUN di batch (D-01). JANGAN eager-create `UserPackageAssignment` kecuali tim memutuskan opsi B secara eksplisit.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (Re-add soft-removed):** Picker `GetEligibleParticipantsToAdd` **EXCLUDE** user yang punya sesi **APAPUN** di batch (aktif `RemovedAt==null` **maupun** removed `RemovedAt!=null`). User soft-removed hanya bisa balik lewat **Restore** (411/412), BUKAN Add. Query eligible = user yang **belum punya sesi sama sekali** di batch (cek existence by `UserId` + batchKey, tanpa pandang `RemovedAt`). Idempotency `AddParticipantsLive` (skip sesi aktif `RemovedAt==null`) tetap berlaku sebagai guard server kedua.
- **D-02 (Cakupan eligible) — ⚠️ OVERRIDE spec §B4:** Picker tampilkan **SEMUA pekerja eligible TANPA batasan unit/section**. JANGAN batasi ke unit/section assign-awal. Eligible = seluruh pekerja aktif minus yang sudah punya sesi di batch. Isu multi-unit (UserUnits v32.3) jadi moot. (Discretion: pastikan sumber pekerja benar + tidak ikutkan akun non-pekerja/admin bila tak relevan.)
- **D-03 (Feedback duplikat):** JSON response `AddParticipantsLive` kembalikan **`added[]` + `skipped[]`** dengan **nama + NIP** tiap entri (bukan sekadar count). Sertakan juga `addedCount`/`skippedCount`.
- **D-04 (Broadcast SignalR):** **DEFER** wiring SignalR `participantAdded` ke Phase 412. Endpoint 410 **cukup** return JSON baris baru (`id, fullName, nip, status`). Broadcast + handler client = 412.

### Carry-forward (LOCKED, bukan dibahas ulang)
- **Guard window:** `ExamWindowCloseDate` di-set & `DateTime.UtcNow.AddHours(7) > ExamWindowCloseDate` → tolak **400** "Window ujian sudah tutup, tidak bisa tambah peserta." Window null = bebas.
- **Status sesi baru:** `DeriveReadyStatus(schedule, window)` → Open/Upcoming, **BUKAN** InProgress. `StartedAt/CompletedAt/RemovedAt = null`.
- **Inherit fields** dari representatif: `Title/Category/Schedule/DurationMinutes/PassPercentage/Shuffle*/GenerateCertificate/AllowAnswerReview/AssessmentType/LinkedGroupId`.
- **`UserPackageAssignment`** cermin paket batch; **transaksi atomic** (gagal buat assignment → rollback seluruh request).
- **Pre/Post:** batch Pre/Post → buat **pasangan Pre+Post**.
- **Proton reject:** `Category == "Assessment Proton"` → tolak dengan pesan jelas.
- **Notif + audit:** notif `ASMT_ASSIGNED` existing + audit `AddParticipantLive`.
- **Soft-removed ⇔ `RemovedAt != null`** (fondasi 409); batch = `Title+Category+Schedule.Date`.

### Claude's Discretion
- Bentuk param endpoint: representative `sessionId` vs `batchKey` (rekomendasi: `sessionId` representatif, resolve batch key darinya).
- Sumber daftar pekerja eligible (tabel/role query) + exclude akun non-pekerja.
- Ekstraksi helper bersama sesi+assignment.
- Cakupan integration test (minimal: ready-status, idempotent, window-tolak, Pre/Post pair, Proton tolak, eligible exclude-by-batch).

### Deferred Ideas (OUT OF SCOPE)
- SignalR `participantAdded` broadcast + handler client → Phase 412.
- `RemoveParticipantLive` / `RestoreParticipantLive` / fix `DeleteAssessmentPeserta` → Phase 411.
- Panel "Peserta Dikeluarkan" + picker UI + modal → Phase 412.
- Playwright e2e + xUnit suite lengkap → Phase 413.
- Filter eligible by unit/section → sengaja TIDAK dilakukan (D-02 override).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PART-06 | Penambahan peserta membuat `AssessmentSession` + `UserPackageAssignment` otomatis dengan status siap-mulai (Open/Upcoming, bukan InProgress), ditolak bila window lewat, idempoten (skip user aktif di batch) | `DeriveReadyStatus` `:2276-2283` (VERIFIED) + window guard pola `CMPController.cs:968-969` (VERIFIED) + idempotency pola existing-sibling `:2161-2174` (VERIFIED) + `UserPackageAssignment` lazy-create `CMPController.cs:1064-1102` (VERIFIED — lihat "Don't Hand-Roll" tentang pilihan eager vs lazy) |
| PART-07 | Penambahan ke assessment Pre/Post membuat pasangan sesi Pre+Post | Pre/Post pair-create `EditAssessment :1942-1998` (VERIFIED) + canonical `CreateAssessment :1235-1320` (VERIFIED) + `LinkedGroupId`/`LinkedSessionId` cross-link mechanism (VERIFIED) |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Resolve batch dari sessionId representatif | API/Backend (`AssessmentAdminController`) | DB (query `AssessmentSessions`) | Server-authoritative; jangan trust batchKey dari client (anti-tampering) |
| Window guard (ExamWindowCloseDate) | API/Backend | — | Business rule, server-side only — mirror `CMPController.StartExam:968` |
| Idempotency (skip sesi aktif) | API/Backend | DB (existence query) | Server adalah guard terakhir; cek `RemovedAt==null` |
| Eligible-user list | API/Backend | DB (`_context.Users` + sesi exclusion) | Data shaping; D-02 = no unit/section scope |
| Create sesi + assignment atomic | API/Backend (service/helper) | DB (transaction) | Atomic write; pola `InjectBatchAsync` `BeginTransactionAsync` |
| Notif ASMT_ASSIGNED + audit | API/Backend | DB (Notifications/AuditLogs) | Side-effect setelah commit (notif), atau in-tx (audit) — ikut pola existing |
| Proton reject | API/Backend | — | Guard dini sebelum write; cek `Category == "Assessment Proton"` |
| DOM inject baris baru / broadcast | Browser/Client + SignalR | — | **DEFER ke Phase 412** (D-04). 410 hanya return JSON. |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 (project TFM) | Controller endpoints (HttpPost/HttpGet, RBAC, antiforgery) | Sudah dipakai seluruh repo; `AssessmentAdminController` host endpoint |
| EF Core | 8.0.0 (pinned via `.config/dotnet-tools.json`) | DbContext, transaksi, query | `ApplicationDbContext` = data layer tunggal |
| `ShuffleEngine` (internal `Helpers/ShuffleEngine.cs`) | — | `BuildQuestionAssignment` / `BuildOptionShuffle` untuk `UserPackageAssignment` (HANYA bila eager-create dipilih) | Single-source shuffle, dipakai StartExam + reshuffle |

### Supporting (internal services — inject via DI, sudah terdaftar)
| Service | Inject sebagai | Purpose | Pola Existing |
|---------|----------------|---------|---------------|
| `INotificationService` | `_notificationService` | `SendAsync(userId, "ASMT_ASSIGNED", title, message, actionUrl)` | `AssessmentAdminController.cs:2232-2238` (BULK ASSIGN notif) |
| `AuditLogService` | `_auditLog` | `LogAsync(actorUserId, actorName, actionType, description, targetId?, targetType?)` | `:2244-2250` (audit "BulkAssign") |
| `UserManager<ApplicationUser>` | `_userManager` | `GetUserAsync(User)` → actor identity | `:1948`, `:2216` |

**Tidak perlu install apa pun.** Semua sudah ada di constructor `AssessmentAdminController` (`:33-56`).

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Reuse `InjectAssessmentService` langsung | Helper privat baru di controller | `InjectAssessmentService` di-desain untuk inject hasil JADI (Completed + grading + cert), bukan sesi ready-status fresh. JANGAN reuse service inject — pola transaksinya bagus tapi semantiknya beda. Ambil **pola** (transaction wrapper), bukan kode-nya. |
| Eager-create `UserPackageAssignment` | Lazy-create (pola existing StartExam) | Lihat "Don't Hand-Roll" — opsi A (lazy) = zero-regresi & konsisten dgn semua peserta existing; opsi B (eager) tambah kompleksitas shuffle tanpa benefit. |

## Architecture Patterns

### System Architecture Diagram

```
Monitoring Detail (Phase 412 UI, belum ada di 410)
        │ AJAX
        ▼
┌─────────────────────────────────────────────────────────────┐
│ AssessmentAdminController                                     │
│                                                              │
│  [HttpGet] GetEligibleParticipantsToAdd(sessionId?/batchKey) │
│        │                                                     │
│        ├─► resolve batch (Title+Category+Schedule.Date)      │
│        ├─► _context.Users.Where(IsActive)                    │
│        ├─► MINUS userIds yang punya sesi APAPUN di batch (D-01)│
│        └─► JSON [{ id, fullName, nip }]                       │
│                                                              │
│  [HttpPost][AntiForgery] AddParticipantsLive(sessionId, ids) │
│        │                                                     │
│        ├─1► load rep session → 404 jika absen                │
│        ├─2► Proton reject (Category=="Assessment Proton"→400) │
│        ├─3► window guard (UtcNow+7h > CloseDate → 400)        │
│        ├─4► idempotency: skip ids dgn sesi aktif RemovedAt==null│
│        │      → masuk skipped[] (D-03)                        │
│        ├─5► BeginTransactionAsync ───────────────┐           │
│        │      foreach new id:                    │ atomic    │
│        │        Standard → 1 AssessmentSession    │           │
│        │        Pre/Post → pair Pre+Post + link   │           │
│        │        Status = DeriveReadyStatus(...)   │           │
│        │      SaveChangesAsync + audit "AddParticipantLive"   │
│        │      CommitAsync ◄───────────────────────┘           │
│        ├─6► notif ASMT_ASSIGNED per user (post-commit)        │
│        └─7► JSON { added[{id,fullName,nip,status}], skipped[] }│
└─────────────────────────────────────────────────────────────┘
        │                              ▲
        ▼ (lazy, saat worker buka ujian)│
   CMPController.StartExam :1064-1102 ──┘ create UserPackageAssignment on-demand
```

### Recommended Code Organization
```
Controllers/AssessmentAdminController.cs
├── AddParticipantsLive (HttpPost)            # endpoint baru
├── GetEligibleParticipantsToAdd (HttpGet)    # endpoint baru
├── BuildReadyParticipantSession(...)         # helper privat baru (emit AssessmentSession ready)
│   └── pakai DeriveReadyStatus :2276 (existing)
└── (opsional) ResolveBatchFromSessionId(...) # helper privat resolve rep + batchKey
```

### Pattern 1: Transaksi atomic per request (template)
**What:** Bungkus seluruh create dalam 1 `BeginTransactionAsync`; gagal → `RollbackAsync`, tak ada sesi setengah-jadi.
**When:** PART-06 "atomic per request".
**Source (VERIFIED):** `Services/InjectAssessmentService.cs:88-421`
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // ... AddRange sessions; SaveChangesAsync (dapat Id) ...
    // ... audit dalam tx ...
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    // notif HANYA setelah commit (hindari notif untuk tx yang rollback)
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    _logger.LogError(ex, "...");
    // return error JSON
}
```
> Catatan: jalur BULK ASSIGN `:2221-2258` juga pakai pola ini (transaction + notif post-commit + audit). Ini referensi terdekat dengan kebutuhan 410.

### Pattern 2: DeriveReadyStatus (Phase 391, PAKAI LANGSUNG)
**Source (VERIFIED):** `Controllers/AssessmentAdminController.cs:2276-2283`
```csharp
private static string DeriveReadyStatus(DateTime schedule, DateTime? examWindowCloseDate)
{
    var nowWib = DateTime.UtcNow.AddHours(7);
    if (schedule <= nowWib)
        return AssessmentConstants.AssessmentStatus.Open;
    return AssessmentConstants.AssessmentStatus.Upcoming;
}
```
> Sudah `private static` di kelas yang sama — endpoint baru bisa panggil langsung. Parameter `examWindowCloseDate` saat ini tak dipakai di body (hanya schedule), tapi signature tetap.

### Pattern 3: Window guard (mirror StartExam)
**Source (VERIFIED):** `Controllers/CMPController.cs:968-969`
```csharp
if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value)
{
    // 410: return BadRequest/Json status 400 dengan pesan LOCKED:
    // "Window ujian sudah tutup, tidak bisa tambah peserta."
}
```
> Window null = bebas (`.HasValue` false). WIB = UTC+7. Pesan 410 BERBEDA dari StartExam ("Ujian sudah ditutup...") — gunakan pesan LOCKED CONTEXT.

### Pattern 4: Idempotency by existence (skip sesi aktif)
**Source (VERIFIED):** `Controllers/AssessmentAdminController.cs:2161-2174` (BULK ASSIGN sibling filter)
```csharp
var existingSiblingUserIds = await _context.AssessmentSessions
    .Where(a => a.Title == rep.Title && a.Category == rep.Category && a.Schedule.Date == rep.Schedule.Date
             && a.RemovedAt == null)        // 410 idempotency = skip sesi AKTIF (Phase 409 invariant)
    .Select(a => a.UserId).Distinct().ToListAsync();
```
> **Beda penting Add vs Eligible:**
> - **Idempotency `AddParticipantsLive` (guard kedua):** skip user dgn sesi **aktif** (`RemovedAt==null`). User dengan sesi removed → tetap di-skip? Per D-01, picker tak akan menawarkannya, tapi jika request langsung membawa user removed, server harus skip juga (jangan buat sesi dobel). Aman: skip user dengan sesi **APAPUN** (tanpa pandang RemovedAt) di Add juga, untuk konsistensi D-01.
> - **Eligible query `GetEligibleParticipantsToAdd`:** exclude user dengan sesi **APAPUN** (`RemovedAt` diabaikan) — D-01 verbatim.

### Pattern 5: Pre/Post pair-create + cross-link
**Source (VERIFIED):** `EditAssessment :1942-1998` (add-to-existing-PrePost) + canonical `CreateAssessment :1235-1320`
```csharp
// per newUserId:
var newPre  = new AssessmentSession { ..., AssessmentType="PreTest",  LinkedGroupId=linkedGroupId, Status=DeriveReadyStatus(...) };
var newPost = new AssessmentSession { ..., AssessmentType="PostTest", LinkedGroupId=linkedGroupId, Status=DeriveReadyStatus(...) };
_context.AssessmentSessions.AddRange(newPre, newPost);
await _context.SaveChangesAsync();          // dapat Id
newPre.LinkedSessionId  = newPost.Id;        // cross-link
newPost.LinkedSessionId = newPre.Id;
await _context.SaveChangesAsync();
```
> `LinkedGroupId` = Id sesi Pre pertama di grup (lihat `CreateAssessment:1271` `linkedGroupId = preSessions[0].Id`). Untuk add ke grup existing, ambil `LinkedGroupId` dari sesi representatif (`assessment.LinkedGroupId!.Value` `:1946`).
> **⚠️ Catatan line-drift:** spec/CONTEXT menyebut "cabang `:1926`". Lokasi AKTUAL pair-create add-to-existing kini di `:1942-1998` (drift +16 baris). `:1926` sekarang adalah baris cascade-delete package (bagian remove). Planner harus pakai `:1942-1998` sebagai referensi pair-create, BUKAN `:1926`.

### Pattern 6: Proton detect + reject
**Source (VERIFIED):** literal `"Assessment Proton"` dipakai konsisten (`:945`, `:1191`, `:3456`, dst); `Models/AssessmentConstants.cs` punya konstanta. Pre/Post detect via helper bersama `AdminBaseController.IsPrePostSession(session)` (`:248-249`).
```csharp
// Guard dini sebelum write:
if (rep.Category == "Assessment Proton")
{
    // 400 + pesan jelas, mis. "Penambahan peserta tidak didukung untuk Assessment Proton."
}
// Pre/Post branch detect:
bool isPrePost = IsPrePostSession(rep);  // AssessmentType == "PreTest" || "PostTest"
```

### Pattern 7: Notif + audit (mirror BULK ASSIGN)
**Source (VERIFIED):** `:2227-2250`
```csharp
// Notif per peserta baru (post-commit, try/catch swallow):
await _notificationService.SendAsync(ns.UserId, "ASMT_ASSIGNED", "Assessment Baru",
    $"Anda telah di-assign assessment \"{ns.Title}\"", $"/CMP/StartExam/{ns.Id}");
// Audit (CONTEXT: actionType "AddParticipantLive"):
await _auditLog.LogAsync(actorId, actorName, "AddParticipantLive",
    $"Added {count} participant(s) to '{rep.Title}' ({rep.Category})", rep.Id, "AssessmentSession");
```
> `actorName` pola: `string.IsNullOrWhiteSpace(hcUser?.NIP) ? hcUser.FullName : $"{hcUser.NIP} - {hcUser.FullName}"` (`:5583`).

### Anti-Patterns to Avoid
- **Trust batchKey/LinkedGroupId mentah dari client:** resolve server-side dari `sessionId` representatif (anti-tampering — pola `InjectAssessmentService` `ResolveLinkContextAsync` T-397-06). Rekomendasi param = `sessionId` representatif.
- **EF global `HasQueryFilter`** untuk RemovedAt: Phase 409 sengaja TIDAK pakai (FORBIDDEN per 409 decision) — pakai `.Where(s => s.RemovedAt == null)` eksplisit.
- **Broadcast SignalR di 410:** OUT OF SCOPE (D-04). Jangan sentuh `_hubContext` untuk participantAdded.
- **Reuse `InjectAssessmentService` untuk create sesi ready:** semantik beda (inject = Completed+graded+cert). Ambil pola transaksi, bukan kode.
- **AssessmentType NULL:** kolom NOT NULL default 'Standard' di Dev/Prod. Saat warisi, gunakan `rep.AssessmentType ?? "Standard"` (pelajaran Phase 391 UAT `:2199-2201`).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Status siap-mulai dari schedule+window | Logika `if schedule <= now ...` baru | `DeriveReadyStatus` `:2276` | Sudah ada, Phase 391, mirror StartExam WIB=UTC+7 |
| `UserPackageAssignment` dengan shuffle | Loop shuffle manual | **(opsi A)** biarkan lazy-create di `StartExam:1064-1102`, ATAU **(opsi B)** `ShuffleEngine.BuildQuestionAssignment` `:1074`/`:5557` | Lihat catatan kritis di bawah |
| Transaksi atomic + rollback | try/catch SaveChanges manual | Pola `BeginTransactionAsync` `InjectBatchAsync:88` / BULK ASSIGN `:2221` | Pola teruji byte-konsisten |
| Pre/Post pair + cross-link | Loop link manual | Pola `:1942-1998` (LinkedGroupId + LinkedSessionId) | Mekanisme link existing |
| Pre/Post detect | Cek string AssessmentType ad-hoc | `IsPrePostSession(session)` `AdminBaseController:248` | Single-source predikat |
| Notif assignment | Buat notif baru | `SendAsync(..., "ASMT_ASSIGNED", ...)` `:2232` | Type & template existing |
| Audit | Insert AuditLog manual | `_auditLog.LogAsync(...)` `:2244` | Signature standar |
| Eligible-user list | Query custom | `_context.Users.Where(u => u.IsActive)` (pola `CreateAssessment:655-659`) | Sumber pekerja existing; D-02 = no unit/section filter |

**Key insight — `UserPackageAssignment` (KRITIS, baca sebelum plan):** Di repo ini, **tidak ada peserta yang punya `UserPackageAssignment` sebelum mereka membuka ujian.** BULK ASSIGN (`:2151`), Pre/Post create (`:1942`, `:1235`), dan create awal semuanya **hanya** membuat `AssessmentSession`. `UserPackageAssignment` dibuat **lazy** di `CMPController.StartExam:1064-1102` (mode paket) saat worker pertama membuka exam. Maka:
- **Opsi A (REKOMENDASI):** Peserta baru 410 cukup punya `AssessmentSession` ready-status. Assignment dibuat lazy seperti peserta lain. Zero-regresi, konsisten, paling sederhana. "Siap-mulai" terpenuhi karena status Open/Upcoming + lazy-assignment identik dengan semua peserta existing.
- **Opsi B:** Eager-create `UserPackageAssignment` via `ShuffleEngine` (butuh load `AssessmentPackages` batch + hitung `workerIndex`). Hanya pilih bila tim ingin assignment ada di DB segera (mis. untuk konsistensi count). Tambah kompleksitas + risiko `workerIndex` drift.
- CONTEXT/spec bilang "buat `UserPackageAssignment` cermin paket batch" — **secara literal ini opsi B**, tapi melanggar pola existing (assign-awal tak buat UPA). **Planner WAJIB angkat ini sebagai keputusan eksplisit** (kandidat `/gsd-discuss-phase` atau Claude's discretion sesuai CONTEXT yang menyebut "ekstraksi helper bersama"). Test integration harus mencerminkan opsi yang dipilih.

## Common Pitfalls

### Pitfall 1: Line-number drift dari spec
**What:** Spec menyebut `:1926` untuk cabang Pre/Post pair-create. Lokasi aktual = `:1942-1998`. `:1926` kini baris cascade-delete package.
**Why:** Phase 409 + edit sebelumnya menambah baris.
**How to avoid:** Verifikasi setiap file:line via Grep saat implement; jangan percaya angka spec mentah. RESEARCH ini sudah re-verify semua angka.

### Pitfall 2: AssessmentType NULL → insert gagal
**What:** Kolom `AssessmentType` NOT NULL (default 'Standard') di Dev/Prod; `model.AssessmentType` adalah `string?`.
**Why:** EF kirim NULL bila tak di-set → insert error (muncul di Dev, fixed `34f102b0` Phase 391).
**How to avoid:** `AssessmentType = rep.AssessmentType ?? "Standard"` (pola `:2201`).

### Pitfall 3: Idempotency cek RemovedAt salah scope
**What:** Idempotency Add harus skip user yang sudah punya sesi; eligible query harus exclude APAPUN (D-01).
**Why:** Add yang skip hanya `RemovedAt==null` bisa membuat sesi dobel untuk user removed (bertabrakan dengan Restore-only D-01).
**How to avoid:** Eligible = exclude sesi APAPUN; Add idempotency = skip sesi APAPUN juga (konsisten D-01). Sesi aktif (`RemovedAt==null`) → skipped[] dengan alasan "sudah terdaftar".

### Pitfall 4: Notif/broadcast sebelum commit
**What:** Mengirim notif/SignalR sebelum `CommitAsync` → notif untuk tx yang ternyata rollback.
**Why:** spec §G "broadcast HANYA setelah CommitAsync sukses".
**How to avoid:** Notif post-commit (pola BULK ASSIGN `:2227`). SignalR = 412 (skip).

### Pitfall 5: Resolve batch dari sessionId tanpa validasi
**What:** sessionId representatif tidak ada / sudah removed.
**How to avoid:** `FirstOrDefaultAsync(s => s.Id == sessionId)` → null → 404/400. Pertimbangkan: rep boleh removed? Lebih aman pilih rep yang `RemovedAt==null` atau resolve batchKey lalu re-pick rep aktif.

### Pitfall 6: Pre/Post hanya buat 1 sesi
**What:** Lupa branch Pre/Post → peserta hanya dapat Pre (atau Post), pasangan pincang.
**How to avoid:** `IsPrePostSession(rep)` → branch pair-create `:1942-1998`. Test PART-07 wajib assert 2 sesi (PreTest+PostTest) + LinkedSessionId cross-set.

## Code Examples

### Resolve actor + audit name (VERIFIED)
```csharp
// Source: AssessmentAdminController.cs:5582-5583
var hcUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
```

### Eligible query skeleton (D-01 + D-02)
```csharp
// Source pattern: CreateAssessment GET :655-659 (user source) + BULK ASSIGN :2161 (batch existence)
var rep = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
var alreadyInBatch = await _context.AssessmentSessions
    .Where(a => a.Title == rep.Title && a.Category == rep.Category && a.Schedule.Date == rep.Schedule.Date)
    // D-01: TANPA filter RemovedAt — exclude sesi APAPUN
    .Select(a => a.UserId).Distinct().ToListAsync();
var eligible = await _context.Users
    .Where(u => u.IsActive && !alreadyInBatch.Contains(u.Id))   // D-02: tanpa unit/section filter
    .OrderBy(u => u.FullName)
    .Select(u => new { u.Id, u.FullName, u.NIP })
    .ToListAsync();
```
> Discretion D-02: pertimbangkan exclude akun admin/HC bila tak relevan. `_context.Users` tak punya kolom role; eksklusi role butuh `_userManager.GetUsersInRoleAsync("Admin"/"HC")` lalu minus (CATATAN: repo TIDAK punya pemakaian `GetUsersInRoleAsync` existing — jika dipakai, ini pola baru). Alternatif lebih sederhana: biarkan semua `IsActive` (admin/HC jarang jadi peserta; tak berbahaya — idempotency tetap melindungi).

## State of the Art

| Old Approach | Current Approach | When | Impact |
|--------------|------------------|------|--------|
| Add peserta via full-page `EditAssessment` POST `NewUserIds` (`:2151`) | Endpoint AJAX `AddParticipantsLive` (410) | v32.5 | Live tanpa reload; reuse pola create yang sama |
| `DeleteAssessmentPeserta` stub mati | Backend nyata `RemoveParticipantLive` | Phase 411 | OUT OF SCOPE 410 |

**Tidak deprecated:** semua pola yang dipakai (DeriveReadyStatus, BeginTransactionAsync, ShuffleEngine, SendAsync, LogAsync) aktif & current.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | "Buat `UserPackageAssignment` otomatis" (spec/CONTEXT) sebaiknya **opsi A (lazy)** karena tak ada peserta existing yang punya UPA sebelum StartExam | Don't Hand-Roll | Bila tim mau opsi B (eager), helper + test berubah. **Mitigasi:** planner angkat sebagai keputusan eksplisit sebelum plan. Bukan blocker — kedua opsi memenuhi "siap-mulai". |
| A2 | Eksklusi akun admin/HC dari eligible bersifat opsional (Claude discretion); `_context.Users.Where(IsActive)` cukup | Code Examples / Eligible | Bila user ingin tegas exclude admin/HC, butuh `GetUsersInRoleAsync` (pola baru di repo). Low risk — idempotency melindungi dari sesi tak diinginkan. |

> 2 asumsi di atas adalah keputusan desain, bukan fakta kode. Semua klaim file:line/perilaku = VERIFIED via Read/Grep.

## Open Questions

1. **Eager vs lazy `UserPackageAssignment` (A1)**
   - What we know: assign-awal existing TIDAK buat UPA; lazy-create di StartExam `:1064-1102`.
   - What's unclear: apakah test PART-06 "PackageAssignment tercipta" mengharuskan eager-create?
   - Recommendation: pilih opsi A (lazy); test assert sesi ready-status + (opsional) bahwa StartExam berikutnya membuat UPA. Bila stakeholder mau UPA di DB segera → opsi B. **Planner putuskan di awal.**

2. **Rep session boleh removed?**
   - What we know: rep dipakai untuk inherit fields.
   - Recommendation: resolve batchKey dari sessionId, lalu pilih rep aktif (`RemovedAt==null`) untuk inherit — hindari mewarisi dari sesi yang sudah removed.

3. **Param endpoint: sessionId vs batchKey (Claude discretion CONTEXT)**
   - Recommendation: terima `sessionId` representatif (anti-tampering, resolve batch server-side). `AssessmentMonitoringDetail` view sudah punya `(title, category, scheduleDate, assessmentType)` — bisa kirim salah satu sessionId baris yang tampil.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK / `dotnet` | build + run + test | ✓ (repo aktif, suite 569/569 hijau) | net8.0 | — |
| SQL Server (SQLEXPRESS lokal) | integration test DB disposable `HcPortalDB_Test_{guid}` | ✓ (pola `FlexibleParticipantAddFixture` jalan) | localhost\\SQLEXPRESS | EF InMemory untuk unit (lihat ParticipantRemovalExcludeTests) |
| `dotnet-ef` 8.0.0 (local tool manifest) | TIDAK dipakai (migration=FALSE) | ✓ | 8.0.0 pinned | — (no migration di 410) |

**Tidak ada dependency baru.** 410 = logic controller murni, reuse skema + service existing.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (project `HcPortal.Tests`) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` |
| Quick run command | `dotnet test HcPortal.Tests --filter "Category!=Integration"` (fast suite, InMemory) |
| Full suite command | `dotnet test HcPortal.Tests` (incl. `[Trait("Category","Integration")]` SQLEXPRESS) |

### Phase Requirements → Test Map
| Req | Behavior | Test Type | Pola / Command | File |
|-----|----------|-----------|----------------|------|
| PART-06 | Sesi baru ready-status (Open/Upcoming, bukan InProgress); StartedAt/CompletedAt/RemovedAt null | integration (SQLEXPRESS) | extend `FlexibleParticipantAddFixture` pola | ❌ Wave 0 (NEW `FlexibleParticipantAddLiveTests` atau extend) |
| PART-06 | Idempotent: user dengan sesi di batch → skip (skipped[]) | integration | seed sesi batch + assert tak dobel | ❌ Wave 0 |
| PART-06 | Window tutup (`ExamWindowCloseDate < now+7h`) → tolak 400 + tak ada sesi | integration | assert no-write | ❌ Wave 0 |
| PART-06 | Atomic: gagal create → rollback (0 sesi) | integration | inject failure / assert count | ❌ Wave 0 (opsional) |
| PART-06 | `GetEligibleParticipantsToAdd` exclude user yang sudah punya sesi APAPUN di batch (incl. removed, D-01) | integration/InMemory real-controller | pola `ParticipantRemovalExcludeTests` (panggil action ASLI) | ❌ Wave 0 |
| PART-07 | Pre/Post → buat pasangan Pre+Post (2 sesi, LinkedSessionId cross-set) | integration | assert AssessmentType PreTest+PostTest + link | ❌ Wave 0 |
| PART-07 | Proton (`Category=="Assessment Proton"`) → tolak | integration | assert reject + no-write | ❌ Wave 0 |

**De-tautology (WAJIB, lesson 999.12):** Test harus MENJALANKAN logika produksi ASLI:
- **Eligible/idempotency/exclude** → panggil action `AssessmentAdminController` ASLI via InMemory real-controller (pola `ParticipantRemovalExcludeTests:51-85` — controller di-instantiate dengan service null untuk yang tak dipakai). Constructor signature: `(context, userManager, auditLog, env, cache, logger, notificationService, hubContext, workerDataService, gradingService, protonCompletionService, protonBypassService)` (`:33-45`).
- **Create sesi ready-status / Pre-Post / window** → SQLEXPRESS disposable (kolom nyata, schema nyata; pola `FlexibleParticipantAddFixture:20-53` + `[Trait("Category","Integration")]`). JANGAN replica predikat (`WindowAllowsAddition` fiktif = anti-pattern dicatat 999.12).
- **Eager-create UPA** (jika opsi B dipilih) → assert UPA tercipta di SQLEXPRESS.

> ⚠️ Repo TIDAK punya `WebApplicationFactory` (catatan 999.12). Endpoint baru sulit di-drive end-to-end via test tanpa banyak stub (`_userManager`, antiforgery). Strategi: test action langsung dengan controller instance + stub HttpContext/User (pola exclude test), seed DB SQLEXPRESS untuk write-path. Penuh e2e endpoint → Phase 413.

### Sampling Rate
- **Per task commit:** `dotnet build HcPortal.csproj` (0 error) + fast suite.
- **Per wave merge:** full `dotnet test HcPortal.Tests` (incl. integration).
- **Phase gate:** build 0 error + full suite hijau (tak regresi 569) + `dotnet run` @5277 OK + cek DB lokal (CLAUDE.md Develop Workflow).

### Wave 0 Gaps
- [ ] `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` (NEW) atau extend `FlexibleParticipantAddTests.cs` — covers PART-06/07 write-path (SQLEXPRESS disposable).
- [ ] Test eligible/idempotency exclude-by-batch via InMemory real-controller (pola `ParticipantRemovalExcludeTests`).
- [ ] Helper seed: batch standard + batch Pre/Post + batch Proton + sesi removed (untuk exclude D-01).
- [ ] (Jika opsi B) seed `AssessmentPackage` + assert `UserPackageAssignment` tercipta.

*(Framework sudah ada; fixture SQLEXPRESS pola ada — gap = file test baru + seed helper, bukan infra.)*

## Security Domain

> `security_enforcement` default enabled. Endpoint mutasi admin.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Server-authoritative: resolve batch dari sessionId server-side, jangan trust batchKey/LinkedGroupId client (pola T-397-06) |
| V4 Access Control | yes | `[Authorize(Roles="Admin, HC")]` pada KEDUA endpoint (mutasi + read picker) |
| V5 Input Validation | yes | Validasi `userIds` exist di `_context.Users`; rep session exist; Proton/window guard sebelum write |
| V6 Cryptography | no | Tak ada crypto baru |
| V13 API/Web Service | yes | `[ValidateAntiForgeryToken]` pada `AddParticipantsLive` (POST). GET picker = read-only (antiforgery tak wajib di GET) |

### Known Threat Patterns for ASP.NET Core MVC + AJAX
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF pada POST add | Spoofing/Tampering | `[ValidateAntiForgeryToken]` (LOCKED CONTEXT) |
| Tambah peserta tanpa otoritas | Elevation | `[Authorize(Roles="Admin, HC")]` |
| Tampering batchKey/LinkedGroupId untuk write lintas-batch | Tampering | Resolve dari `sessionId` representatif server-side; jangan terima batchKey mentah |
| IDOR userIds (assign user arbitrer) | — | D-02 sengaja membolehkan semua pekerja; tetap validasi `IsActive` + exist. Bukan IDOR by design. |
| Mass-add abuse | DoS | Rate-limit existing BULK ASSIGN = max 50 (`:2032`). Pertimbangkan cap serupa di 410. |
| XSS pada nama/NIP di JSON | — | JSON response; rendering DOM = Phase 412 (gunakan `.textContent`, carry T-409-10 prinsip). 410 hanya emit data. |

> Catatan: tidak ada `RemovalReason` di 410 (itu remove path 411). XSS-at-render = 412.

## Sources

### Primary (HIGH confidence — VERIFIED via Read/Grep di repo)
- `Controllers/AssessmentAdminController.cs` — `DeriveReadyStatus :2276-2283`; BULK ASSIGN `:2151-2272`; Pre/Post pair-create `:1942-1998`; canonical Pre/Post `CreateAssessment :1235-1320`; constructor `:33-56`; eligible/user source `CreateAssessment GET :655-659`; Proton literal `:945/1191/3456`; notif `:2232`; audit `:2244`; AssessmentType-null fix `:2199-2201`; rate-limit 50 `:2032`; AssessmentMonitoringDetail batch key `:3328-3340`; AssessmentMonitoring exclude-removed `:2816-2826`
- `Controllers/CMPController.cs` — window guard `:968-969`; lazy UPA create `:1064-1102`; `IsParticipantRemoved :2540`; guard usage `:373/924/1611`
- `Services/InjectAssessmentService.cs` — atomic transaction template `:88-421`; session build `:143-171`; UPA build `:233-245`
- `Models/AssessmentSession.cs` — semua field inherit + removal cols 409 `:97-103`; ExamWindowCloseDate `:65`; LinkedGroupId/LinkedSessionId `:180-186`
- `Models/UserPackageAssignment.cs` — field wajib: `AssessmentSessionId`, `AssessmentPackageId`, `UserId`, `ShuffledQuestionIds`, `ShuffledOptionIdsPerQuestion`
- `Services/AuditLogService.cs:21-42` — `LogAsync` signature
- `Services/INotificationService.cs:20` — `SendAsync` signature
- `Services/WorkerDataService.cs:244-270` — `GetWorkersInSection` (user source pola)
- `Controllers/AdminBaseController.cs:248-249` — `IsPrePostSession`
- `Models/ApplicationUser.cs` — `IsActive:66`, `NIP:18`, `FullName:13`, `Section:28`, `Unit:33`
- `HcPortal.Tests/ParticipantRemovalGuardTests.cs:51-107` — InMemory real-controller pattern + constructor stub
- `HcPortal.Tests/FlexibleParticipantAddTests.cs:20-53` — SQLEXPRESS disposable fixture

### Project docs (HIGH)
- `docs/superpowers/specs/2026-06-19-flexible-add-remove-participant-design.md` §B1/B4/F/G/H
- `.planning/phases/410-add-participant-backend-live/410-CONTEXT.md` (decisions)
- `.planning/REQUIREMENTS.md` (PART-06/07) + `.planning/ROADMAP.md` (Phase 410 details)
- `CLAUDE.md` (Develop + Seed Workflow)

## Project Constraints (from CLAUDE.md)
- **Respond Bahasa Indonesia** (prose); code/identifier English. ✓ diikuti.
- **Develop Workflow:** verifikasi lokal `dotnet build` + `dotnet run` (localhost:5277) + cek DB lokal (+ Playwright bila ada UI) SEBELUM commit. Phase 410 = backend, tak ada UI → Playwright tak wajib (UI = 412).
- **migration=FALSE** untuk 410 — JANGAN scaffold migration; semua kolom sudah ada.
- **Jangan edit kode/DB Dev/Prod.** Jangan push tanpa verifikasi lokal. Promosi Dev/Prod = IT.
- **Seed Workflow:** bila integration test perlu seed di DB lokal nyata, gunakan DB disposable `HcPortalDB_Test_{guid}` (sudah pola) — TIDAK menyentuh `HcPortalDB_Dev`. Snapshot/restore hanya bila menyentuh DB lokal langsung.
- **Branch main.** Notify IT saat merge: 410 = migration=FALSE.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua service/helper existing, file:line VERIFIED, nol dependency baru.
- Architecture: HIGH — 3 pola create existing dipetakan + transaksi atomic template.
- Pitfalls: HIGH — line-drift, AssessmentType-null, lazy-UPA semua confirmed di kode.
- Eager-vs-lazy UPA: MEDIUM — keputusan desain terbuka (A1), bukan ketidakpastian fakta.

**Research date:** 2026-06-21
**Valid until:** 2026-07-21 (stabil — kode internal, tak ada lib eksternal fast-moving). Re-verify file:line bila `AssessmentAdminController.cs` di-edit Phase 411 (file-overlap sequential).
