# Phase 411: Remove + Restore Backend Live - Research

**Researched:** 2026-06-21
**Domain:** ASP.NET Core MVC backend (C# / EF Core 8.0.0 / SQL Server) — soft-remove + hard-delete cascade, idempotent endpoints, audit + RBAC
**Confidence:** HIGH (all key claims VERIFIED against production code file:line in this session)

## Summary

Phase 411 menambah dua endpoint AJAX baru (`RemoveParticipantLive` POST, `RestoreParticipantLive` POST) + menghidupkan stub mati `DeleteAssessmentPeserta`, semuanya di `Controllers/AssessmentAdminController.cs`. Tidak ada package baru, tidak ada migration — semua infrastruktur sudah ada: kolom `RemovedAt`/`RemovedBy`/`RemovalReason` (409), `RecordCascadeDeleteService.ExecuteAsync` (hard-delete single-root 1-tx), `AuditLogService.LogAsync`, `IsPrePostSession`, `EnsureCanDeleteAsync`, dan `BuildReadyParticipantSession` (410). Pekerjaan 411 = orkestrasi aset-aset ini di balik tiga endpoint baru + satu varian redirect.

**Temuan paling penting (VERIFIED):** `RecordCascadeDeleteService.ExecuteAsync` **SUDAH** menghapus `UserPackageAssignments` (line 221-222) sebagai bagian cascade per-session node. Ini menyelesaikan kekhawatiran D-01 secara langsung — eager-UPA dari 410 akan ikut terhapus saat hard-delete, **tanpa perlu memperluas service**. Cascade juga menghapus `PackageUserResponses`, `AssessmentAttemptHistory` (by `SessionId`), `AssessmentEditLogs`, `AssessmentPackages`+`PackageQuestions`+`PackageOptions`, plus null-clear `LinkedSessionId` pasangan dan file sertifikat post-commit. Concern `AssessmentAttemptHistory` FK-ke-User (deferred idea) TIDAK menimbulkan orphan untuk hard-delete satu sesi — cascade menghapus history by `SessionId` (line 218), bukan menghapus User.

**Catatan kritis arsitektur (PRMV-05):** `DeletePrePostGroup` existing (line 2830) bekerja pada `LinkedGroupId` (menghapus **seluruh batch lintas semua user**) — BUKAN model yang tepat untuk pair-as-unit **satu peserta**. Untuk PRMV-05 yang benar, pasangan satu peserta diidentifikasi via `LinkedSessionId` (cross-link Pre↔Post yang di-set 410 di `AddParticipantsLive` line 2438-2439). Mirror **partner-handling logic** (evaluasi gabungan: salah satu berdata → soft keduanya; keduanya bersih → hard keduanya), BUKAN copy-paste signature `DeletePrePostGroup`.

**Primary recommendation:** Ekstrak satu private method bersama `RemoveParticipantCoreAsync(sessionId, reason, actorId, actorName) → outcome` yang melakukan: idempotency check → resolve Pre/Post partner via `LinkedSessionId` → evaluasi gabungan has-data → hard (cascade) atau soft (set 3 kolom) → audit. `RemoveParticipantLive` membungkusnya dengan Proton-reject + reason-validation + JSON outcome; `DeleteAssessmentPeserta` membungkusnya dengan redirect ke `EditAssessment/{returnToId}`. Reuse infra test live dari 410 (`FlexibleParticipantAddLiveTests` + `FlexibleParticipantAddFixture`).

## User Constraints (from CONTEXT.md)

### Locked Decisions

**D-01 (Hard-delete vs eager-UPA):** Threshold hard-delete = `StartedAt == null` **&&** **0 `PackageUserResponse`**. `UserPackageAssignment` (eager dari 410) **TIDAK dihitung "data"** → peserta belum-mulai tetap hard-delete. `RecordCascadeDeleteService.ExecuteAsync` (single root sesi) wajib cascade bersihkan eager-UPA + assignment milik sesi itu. **[VERIFIED: service SUDAH hapus UPA — line 221-222. Tidak perlu perluas service.]**

**D-02 (RemovalReason wajib/opsional):** `RemovalReason` **WAJIB** saat soft-remove (peserta sudah-mulai/Completed/bersertifikat — jejak audit PLIV-03). **Opsional** saat hard-delete (peserta belum-mulai, bersih). Validasi server: jika jalur = soft-remove dan `reason` kosong → 400 "Alasan penghapusan wajib diisi."

**D-03 (SignalR):** DEFER SignalR `examRemoved` (force-kick worker) + `participantRemoved` (broadcast monitor) ke Phase 412. 411 = backend `RemoveParticipantLive`/`RestoreParticipantLive` + audit + RBAC saja. Endpoint return JSON outcome (hard/soft + sessionId). **JANGAN tambah `_hubContext` call di 411.**

**D-04 (Fix stub DeleteAssessmentPeserta):** Implement `DeleteAssessmentPeserta` (tombol mati `EditAssessment.cshtml:666`) sebagai delegasi ke service bersama yang sama dengan `RemoveParticipantLive` (varian full-page redirect, bukan JSON). Ekstrak logika remove jadi service/private method bersama → satu sumber kebenaran.

**Carry-forward LOCKED (bukan dibahas ulang):**
- Soft-remove **JANGAN sentuh** `Score`, `IsPassed`, `NomorSertifikat`, file sertifikat, `PackageUserResponse`, `Status` (sesi InProgress tetap InProgress — guard 409 D-04 andalkan `RemovedAt`). (spec §B2.2)
- Idempoten: `RemovedAt != null` → no-op sukses. (spec §B2.1)
- Pre/Post (PRMV-05): salah satu ada-data → soft-remove keduanya; kedua belum-mulai+tanpa-data → hard-delete keduanya. (spec §B2.4)
- Restore hanya untuk soft-removed (hard-deleted tak bisa restore). (spec §B3)
- RBAC `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` semua endpoint; audit Remove/Restore (siapa/kapan/alasan). (PLIV-03, §H)
- Proton reject (`Category=="Assessment Proton"`). (spec §F)
- Keputusan #5 spec: lepas `EnsureCanDeleteAsync` untuk HC hapus Completed/bersertifikat — mitigasi: soft-remove (cert utuh) + audit + (412 modal keras).

### Claude's Discretion
- Bentuk service bersama (private method vs service class) untuk remove-logika yang dipakai RemoveParticipantLive + DeleteAssessmentPeserta.
- Apakah `RecordCascadeDeleteService` sudah hapus UPA atau perlu ditambah → **VERIFIED: SUDAH (line 221-222), tak perlu tambah.**
- Bentuk JSON outcome RemoveParticipantLive (`{ sessionId, mode: "hard"|"soft", linkedSessionId? }`).
- Penempatan validasi reason-wajib (sebelum tentukan jalur vs sesudah).
- Cakupan integration test (hard not-started, soft in-progress/completed-cert preserved, idempotent, Pre/Post pair, restore, Proton reject, reason-wajib-soft).

### Deferred Ideas (OUT OF SCOPE)
- SignalR `examRemoved`/`participantRemoved` + force-kick + modal konfirmasi keras + panel "Peserta Dikeluarkan" + handler client → **Phase 412**.
- Playwright e2e + xUnit suite lengkap remove/restore live → **Phase 413** (411 cukup integration minimal backend).
- `AssessmentAttemptHistory` FK ke User (bukan Session) — komplikasi cascade → **VERIFIED non-issue untuk hard-delete satu sesi** (cascade hapus by `SessionId`, lihat Pitfall 4).

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PRMV-01 | Hybrid by-state delete: belum-mulai+0-response → hard-delete cascade; sudah-mulai/Completed/berdata → soft-remove (Score/IsPassed/cert/file/response utuh, set RemovedAt/RemovedBy/RemovalReason); idempoten | `RecordCascadeDeleteService.ExecuteAsync` (hard, VERIFIED hapus UPA) + set 3 kolom (soft) + has-data detection via `PackageUserResponses.CountAsync` + `StartedAt==null`. Idempoten via `RemovedAt!=null` guard. Lihat Pattern 1-3. |
| PRMV-04 | Restore soft-removed (RemovedAt=null + clear RemovedBy/RemovalReason); hard-deleted tak bisa restore | `RestoreParticipantLive`: load sesi → guard `RemovedAt!=null` → set null → audit. Lihat Pattern 4. |
| PRMV-05 | Pre/Post pair-as-unit (salah satu berdata → soft keduanya; keduanya bersih → hard keduanya) | Resolve partner via `LinkedSessionId` (cross-link 410:2438), evaluasi gabungan. **BUKAN** `DeletePrePostGroup` (yang by-LinkedGroupId, lintas-user). Lihat Pattern 5 + Pitfall 1. |
| PLIV-03 | Audit Add/Remove/Restore (siapa/kapan/alasan) + RBAC Admin+HC + antiforgery semua endpoint | `AuditLogService.LogAsync` (signature VERIFIED) + `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` (mirror AddParticipantsLive 2353-2355). Lihat Pattern 6. |

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Remove/Restore endpoint (auth, route, antiforgery, JSON) | API / Backend (`AssessmentAdminController`) | — | Mutasi data assessment = server-authoritative; client (412) konsumsi JSON outcome saja |
| Hybrid by-state decision (hard vs soft) | API / Backend | — | Keputusan berbasis kolom DB (`StartedAt`, `PackageUserResponse` count) — tidak boleh dipercayakan ke client |
| Hard-delete cascade (9 artefak + UPA + file cert) | Service (`RecordCascadeDeleteService`) | Database (transaksi + FK) | Engine cascade existing, 1-tx atomik; reuse, jangan re-implement |
| Soft-remove write (set 3 kolom) | API / Backend | Database | Set kolom existing 409; tak butuh service terpisah |
| Audit trail | Service (`AuditLogService`) | Database | Single-source audit existing |
| Pre/Post partner resolution | API / Backend | Database (`LinkedSessionId` FK) | Identifikasi pasangan via cross-link kolom, server-side |
| Force-kick / broadcast | — (DEFER 412) | — | D-03: TIDAK di 411 |

## Standard Stack

### Core (semua EXISTING — tidak ada install baru)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | ORM + transaksi DB | Pinned di `.csproj` [VERIFIED: grep .csproj]. Sudah dipakai seluruh data-access |
| Microsoft.AspNetCore (MVC) | net8.0 | Controller, `[Authorize]`, `[ValidateAntiForgeryToken]`, `Json()`, `BadRequest()` | Framework aplikasi [VERIFIED: `<TargetFramework>net8.0`] |
| xUnit | (existing) | Integration test | Pola `FlexibleParticipantAddLiveTests` + `IClassFixture` [VERIFIED: HcPortal.Tests] |

**Installation:** Tidak ada. Phase 411 = **migration=FALSE**, **zero package baru**. Semua aset reuse dari kode existing.

**Version verification:** `grep PackageReference *.csproj` → EF Core 8.0.0 (Identity.EFCore, Sqlite, SqlServer, Tools, Design semua 8.0.0). `<TargetFramework>net8.0</TargetFramework>`. [VERIFIED: codebase grep, 2026-06-21]

### Reusable Production Assets (file:line VERIFIED hari ini)

| Asset | Location | Signature / Behavior | Use in 411 |
|-------|----------|---------------------|-----------|
| `RecordCascadeDeleteService.ExecuteAsync` | `Services/RecordCascadeDeleteService.cs:175` | `Task<CascadeResult> ExecuteAsync(string rootType, int rootId, IEnumerable<int> mirrorTrainingIdsToInclude, string actorId, string actorName)`. `rootType="session"`. 1-tx. Return `CascadeResult(bool Success, int DeletedCount, List<int> DeletedSessionIds, List<int> DeletedTrainingIds, string? ErrorMessage)` | Jalur **hard-delete**. Pass `Enumerable.Empty<int>()` untuk mirror (tak relevan single live-remove). Resolve via `HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>()` (pola DeleteAssessment:2551) |
| Cascade UPA cleanup | `RecordCascadeDeleteService.cs:221-222` | `_context.UserPackageAssignments.Where(a => a.AssessmentSessionId == id)` → RemoveRange | **D-01 satisfied** — eager-UPA terhapus otomatis saat hard-delete |
| `AuditLogService.LogAsync` | `Services/AuditLogService.cs:21` | `Task LogAsync(string actorUserId, string actorName, string actionType, string description, int? targetId = null, string? targetType = null)`. SaveChanges internal | Audit Remove/Restore. Field `_auditLog` di controller |
| `IsPrePostSession` | `Controllers/AdminBaseController.cs:248` | `public static bool IsPrePostSession(AssessmentSession s)` => `AssessmentType=="PreTest" \|\| =="PostTest"`. Dipanggil langsung | Deteksi sesi Pre/Post untuk pair-as-unit |
| `EnsureCanDeleteAsync` | `AssessmentAdminController.cs:7465` | `private Task<IActionResult?>` — Admin override (`User.IsInRole("Admin")` → null), HC diblok bila `anyCompleted \|\| responseCount>0` → redirect | **Keputusan #5: untuk soft-remove JANGAN panggil ini** (atau bypass). Soft-remove = jalur HC boleh hapus Completed/cert. Lihat Pattern 3 |
| `BuildReadyParticipantSession` | `AssessmentAdminController.cs:2322` | Factory sesi ready-status (410). Tersedia bila restore butuh re-derive — **tidak dipakai** (restore cukup clear kolom) | Available (tak terpakai 411) |
| `AddParticipantsLive` audit pattern | `AssessmentAdminController.cs:2456` | `await _auditLog.LogAsync(actorId, actorName, "AddParticipantLive", $"...", rep.Id, "AssessmentSession")` | Template verbatim audit Remove/Restore |
| Actor resolution | `AssessmentAdminController.cs:2394-2397` | `var hcUser = await _userManager.GetUserAsync(User); actorId = hcUser?.Id ?? ""; actorName = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}"` | Reuse verbatim untuk `RemovedBy` + audit actorName |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Private method `RemoveParticipantCoreAsync` di controller | Service class baru `ParticipantRemovalService` | Service class = lebih testable/DI-clean, tapi tambah file + registrasi DI. Private method = konsisten dgn 410 (`CreateEagerAssignmentsAsync`/`BuildReadyParticipantSession` semua private di controller). **Rekomendasi: private method** (konsisten, lebih ringan, D-04 cuma butuh 1 sumber kebenaran — bukan reusability lintas-controller) |
| Resolve partner via `LinkedSessionId` | Resolve via `LinkedGroupId` (seperti DeletePrePostGroup) | `LinkedGroupId` = seluruh batch lintas-user (SALAH untuk single peserta). `LinkedSessionId` = cross-link Pre↔Post **per peserta** (BENAR). **Rekomendasi: `LinkedSessionId`** (lihat Pitfall 1) |
| Reuse `EnsureCanDeleteAsync` apa adanya | Bypass untuk jalur soft-remove | Keputusan #5 spec: soft-remove HARUS izinkan HC hapus Completed/cert. Jangan panggil guard di jalur soft (atau hanya panggil di jalur hard yang justru hanya not-started=tak pernah Completed) → guard jadi no-op natural di hard. Lihat Pattern 3 |

## Architecture Patterns

### System Architecture Diagram — RemoveParticipantLive flow

```
[Admin/HC client (412 UI)]
         │  POST /Admin/RemoveParticipantLive { sessionId, reason }  (antiforgery token)
         ▼
[RBAC gate: [Authorize(Roles="Admin, HC")] + [ValidateAntiForgeryToken]]
         │
         ▼
[Load sesi by sessionId] ──not found──> 404 JSON
         │ found
         ▼
[Proton guard: Category=="Assessment Proton"] ──yes──> 400 "tidak didukung untuk Proton"
         │ no
         ▼
[Idempotency: RemovedAt != null] ──yes──> 200 JSON { mode:"noop", sessionId }   (no-op sukses)
         │ no (aktif)
         ▼
[Resolve Pre/Post partner via LinkedSessionId]  (jika IsPrePostSession)
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│  EVALUASI GABUNGAN has-data (self + partner):                │
│    hasData(s) = s.StartedAt != null                          │
│                 OR PackageUserResponses.Any(r.SessionId==s)  │
│    Pair: anyHasData = hasData(self) || hasData(partner)      │
└─────────────────────────────────────────────────────────────┘
         │                                    │
   anyHasData == false                  anyHasData == true
   (semua bersih)                       (≥1 berdata)
         ▼                                    ▼
[reason opsional]                  [reason WAJIB (D-02)]──kosong──> 400 "Alasan ... wajib"
         │                                    │ ada
         ▼                                    ▼
[HARD-DELETE keduanya            [SOFT-REMOVE keduanya:
 via cascade.ExecuteAsync          set RemovedAt=UtcNow, RemovedBy=actorId,
 per session (self+partner);       RemovalReason=reason. JANGAN sentuh
 cascade hapus UPA+response+       Score/IsPassed/NomorSertifikat/file/
 history+packages+file cert]       response/Status. SaveChanges]
         │                                    │
         └──────────────┬─────────────────────┘
                        ▼
         [Audit LogAsync("RemoveParticipantLive", mode+reason)]
                        ▼
         [JSON outcome: { sessionId, mode:"hard"|"soft", linkedSessionId? }]
                        │
                        ▼   (D-03: NO SignalR di 411 — 412 broadcast participantRemoved)
              [Client 412 konsumsi → pindah baris]
```

### Recommended Code Organization (single file: `AssessmentAdminController.cs`)

```
Controllers/AssessmentAdminController.cs  (sisip dekat AddParticipantsLive :2348-2542)
├── RemoveParticipantLive (HttpPost, JSON)         # endpoint 1 — wrapper JSON
├── RestoreParticipantLive (HttpPost, JSON)        # endpoint 2 — soft-removed only
├── DeleteAssessmentPeserta (HttpPost, redirect)   # stub fix — wrapper redirect (D-04)
└── RemoveParticipantCoreAsync (private)           # shared core — hybrid + Pre/Post + audit
    └── helper: SessionHasDataAsync(session)        # StartedAt!=null OR response>0
```

### Pattern 1: Hybrid by-state detection ("has data")
**What:** Tentukan jalur hard vs soft berdasarkan state sesi.
**When:** Awal `RemoveParticipantCoreAsync`, setelah idempotency check.
**VERIFIED detail:** "has data" = `StartedAt != null` **OR** ada `PackageUserResponse` untuk sesi. UPA **TIDAK** dihitung (D-01) — query khusus `PackageUserResponses`, bukan `UserPackageAssignments`.

```csharp
// Source: pola count VERIFIED dari DeleteAssessment:2599-2600 + EnsureCanDeleteAsync:2488-2489
private async Task<bool> SessionHasDataAsync(AssessmentSession s)
{
    if (s.StartedAt != null) return true;               // sudah mulai = berdata (D-01)
    return await _context.PackageUserResponses
        .AnyAsync(r => r.AssessmentSessionId == s.Id);  // ada jawaban = berdata
    // CATATAN: UserPackageAssignment SENGAJA tak dihitung (D-01) — eager-UPA bukan jejak pengerjaan.
}
```

### Pattern 2: Hard-delete via cascade (reuse, jangan re-implement)
**What:** Hapus sesi bersih (belum-mulai + 0 response) lewat engine cascade.
**When:** `anyHasData == false`.
**VERIFIED:** `ExecuteAsync` hapus 9 artefak termasuk **UserPackageAssignments** (D-01 satisfied) dalam 1-tx.

```csharp
// Source: DeleteAssessment:2551 + 2618 (pola resolve service + invoke)
var cascade = HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>();
var result = await cascade.ExecuteAsync("session", session.Id, Enumerable.Empty<int>(), actorId, actorName);
if (!result.Success)
    return /* 500 JSON */ StatusCode(500, new { error = result.ErrorMessage ?? "Gagal menghapus peserta." });
// result.DeletedSessionIds berisi root (+ turunan renewal bila ada — biasanya 0 untuk sesi live baru)
```

### Pattern 3: Soft-remove write (set 3 kolom, JANGAN sentuh lainnya)
**What:** Tandai sesi removed tanpa menghapus data.
**When:** `anyHasData == true`.
**CRITICAL invariant:** JANGAN mutasi `Score`, `IsPassed`, `NomorSertifikat`, `ManualSertifikatUrl`, `Status`, `PackageUserResponse`. Guard 409 (StartExam/SubmitExam/JoinBatch) andalkan `RemovedAt` — Status InProgress sengaja TIDAK berubah.

```csharp
// Source: kolom VERIFIED AssessmentSession.cs:98-103 (RemovedAt/RemovedBy/RemovalReason)
session.RemovedAt = DateTime.UtcNow;     // UTC (sumber kebenaran "removed")
session.RemovedBy = actorId;             // userId Admin/HC (cermin CreatedBy)
session.RemovalReason = reason;          // WAJIB pada jalur soft (D-02)
// JANGAN sentuh: Score, IsPassed, NomorSertifikat, ManualSertifikatUrl, Status, response
await _context.SaveChangesAsync();
```

**Keputusan #5 (longgarkan EnsureCanDeleteAsync):** Jalur soft-remove **TIDAK memanggil** `EnsureCanDeleteAsync` sama sekali — soft-remove justru DIPILIH untuk peserta Completed/cert sehingga HC boleh melakukannya (cert utuh + reversibel). Jalur hard-delete hanya berlaku untuk not-started+0-response, yang per definisi tak pernah Completed/ber-response → guard akan no-op natural andai dipanggil. **Rekomendasi: tidak panggil guard di endpoint live ini** (mitigasi = soft-remove + audit wajib, sesuai §H).

### Pattern 4: Restore (clear kolom, soft-removed only)
```csharp
// RestoreParticipantLive
var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
if (session == null) return NotFound(new { error = "Sesi tidak ditemukan." });
if (session.RemovedAt == null)
    return BadRequest(new { error = "Sesi ini tidak dalam keadaan dihapus." }); // hard-deleted tak ada barisnya = 404 natural
session.RemovedAt = null;
session.RemovedBy = null;
session.RemovalReason = null;
await _context.SaveChangesAsync();
await _auditLog.LogAsync(actorId, actorName, "RestoreParticipantLive",
    $"Restored participant session [ID={sessionId}] '{session.Title}'", sessionId, "AssessmentSession");
// Pre/Post: restore partner juga (LinkedSessionId) untuk konsistensi pair-as-unit — symmetric dgn remove.
return Json(new { sessionId, restored = true });
```

### Pattern 5: Pre/Post pair-as-unit (via LinkedSessionId, BUKAN LinkedGroupId)
**What:** Perlakukan pasangan Pre+Post **satu peserta** sebagai satu unit.
**When:** `IsPrePostSession(session) == true`.

```csharp
// Resolve partner satu peserta (cross-link di-set 410 AddParticipantsLive:2438-2439).
AssessmentSession? partner = null;
if (IsPrePostSession(session) && session.LinkedSessionId.HasValue)
    partner = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == session.LinkedSessionId.Value);

// Evaluasi GABUNGAN: salah satu berdata → soft KEDUANYA; keduanya bersih → hard KEDUANYA.
bool anyHasData = await SessionHasDataAsync(session)
                  || (partner != null && await SessionHasDataAsync(partner));

// Terapkan SAMA ke session + partner (soft set 3 kolom keduanya, atau cascade keduanya).
```

### Pattern 6: RBAC + antiforgery + audit (mirror AddParticipantsLive)
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RemoveParticipantLive(int sessionId, string? reason) { ... }
// audit DALAM operasi (untuk soft, sebelum/sesudah SaveChanges; untuk hard, cascade.ExecuteAsync
//   menulis audit "CascadeDelete" sendiri — tambah audit endpoint "RemoveParticipantLive" untuk konteks user-facing,
//   pola DeleteAssessment:2635 yang double-log: engine + endpoint).
```

### Pattern 7: DeleteAssessmentPeserta redirect variant (D-04)
**Dead form contract (VERIFIED `EditAssessment.cshtml:666-670`):** POST `asp-action="DeleteAssessmentPeserta"`, fields: `sessionId` (hidden) + `returnToId` (= `Model.Id`, sesi yang sedang di-edit) + antiforgery token.

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteAssessmentPeserta(int sessionId, int returnToId)
{
    var (actorId, actorName) = await ResolveActorAsync();   // pola :2394-2397
    var outcome = await RemoveParticipantCoreAsync(sessionId, reason: null, actorId, actorName);
    // reason null OK di sini? — HATI-HATI: jalur soft butuh reason (D-02). Form lama tak punya field reason.
    //   → Opsi: form ini hanya untuk not-started (hard, reason opsional); bila peserta berdata, set
    //     TempData["Error"]="Gunakan kontrol Hapus di Monitoring Detail (butuh alasan)." + redirect.
    //   → ATAU tambah field reason ke form (412 scope). Diskresi planner — lihat Open Question #1.
    TempData[outcome.Ok ? "Success" : "Error"] = outcome.Message;
    return RedirectToAction("EditAssessment", new { id = returnToId });
}
```

### Anti-Patterns to Avoid
- **Re-implement cascade manual:** JANGAN tulis ulang penghapusan 9 tabel — gunakan `ExecuteAsync`. Hilang transaksi atomik + audit + UPA cleanup + file cert.
- **Pakai `DeletePrePostGroup` untuk pair satu peserta:** Itu menghapus seluruh batch lintas-user (by LinkedGroupId). Pakai `LinkedSessionId`.
- **Soft-remove menyentuh Status:** Memecah guard 409. Status InProgress HARUS tetap InProgress.
- **Hitung UPA sebagai "data":** Melanggar D-01 — hampir tak ada peserta yang bisa hard-delete (410 buat UPA eager untuk semua).
- **Broadcast SignalR di 411:** Melanggar D-03. JANGAN sentuh `_hubContext`.
- **Reason wajib di hard-delete:** Friksi tak perlu untuk koreksi salah-tambah belum-mulai (D-02 = opsional di hard).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Hard-delete cascade 9 tabel + file cert | Loop manual RemoveRange per tabel | `RecordCascadeDeleteService.ExecuteAsync` | 1-tx atomik, audit, UPA cleanup, file cert post-commit, LinkedSessionId null-clear, renewal traversal — sudah teruji (Phase 367) |
| Audit row | `_context.AuditLogs.Add(new AuditLog{...})` | `AuditLogService.LogAsync` | SaveChanges internal, field konsisten (ActorUserId/ActorName/ActionType/...) |
| Deteksi sesi Pre/Post | Cek `AssessmentType` string inline | `IsPrePostSession(session)` | Single-source (AdminBaseController:248), dipakai 410 + DeleteAssessment |
| Actor name format | Format NIP/FullName ad-hoc | Pola `:2394-2397` (copy 3 baris) | Konsisten dgn semua audit existing |
| Resolve service dari DI | Constructor inject `RecordCascadeDeleteService` | `HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>()` | Pola existing DeleteAssessment:2551 (service tidak di constructor controller) |

**Key insight:** Phase 411 adalah **orkestrasi murni** — hampir semua primitif sudah ada dan teruji. Nilai 411 = menyusunnya di balik kontrak endpoint yang benar (hybrid, idempotent, pair-as-unit) + satu varian redirect, BUKAN membangun mekanisme baru.

## Runtime State Inventory

> Bukan rename/migrate phase, tapi remove menyentuh runtime state — relevan untuk hard-delete cleanup.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data (hard-delete) | `RecordCascadeDeleteService` hapus: `AssessmentSession`, `PackageUserResponses`, `UserPackageAssignments` (D-01 ✓), `AssessmentAttemptHistory` (by SessionId), `AssessmentEditLogs`, `AssessmentPackages`+`PackageQuestions`+`PackageOptions`, `UserNotifications` (exact-match URL), file sertifikat (post-commit). [VERIFIED: RecordCascadeDeleteService.cs:212-301] | None — reuse cascade. Verifikasi test: setelah hard-delete, UPA row hilang |
| Stored data (soft-remove) | `RemovedAt`/`RemovedBy`/`RemovalReason` di-set; SEMUA data lain DIPERTAHANKAN | Code edit — set 3 kolom only |
| Live service config | n/a — tak ada config eksternal disentuh | None |
| OS-registered state | n/a | None |
| Secrets/env vars | n/a | None |
| Build artifacts | n/a — tak ada package/file baru | None — verifikasi `git status Migrations/` kosong (migration=FALSE) |

**Nothing found:** Live service config / OS-registered / secrets / build artifacts — None (verified: phase = controller logic only, migration=FALSE, zero package baru).

## Common Pitfalls

### Pitfall 1: Pre/Post pair via wrong key (LinkedGroupId vs LinkedSessionId)
**What goes wrong:** Menggunakan `DeletePrePostGroup` atau `LinkedGroupId` untuk pair satu peserta → menghapus/soft-remove **seluruh peserta dalam batch**, bukan hanya pasangan Pre+Post milik satu user.
**Why:** `DeletePrePostGroup` (line 2830) by-design beroperasi pada `LinkedGroupId` = seluruh grup batch lintas-user. `LinkedGroupId` di-share semua peserta batch Pre/Post (410:2428-2434 set `LinkedGroupId = rep.LinkedGroupId.Value` untuk semua). `LinkedSessionId` adalah cross-link per-peserta (410:2438-2439).
**How to avoid:** Resolve partner via `session.LinkedSessionId`. Mirror **partner-handling logic** (evaluasi gabungan), bukan copy `DeletePrePostGroup`. [VERIFIED: 410:2438-2439 cross-link; DeletePrePostGroup:2838 by-LinkedGroupId]
**Warning sign:** Test "remove 1 peserta Pre/Post" menghapus lebih dari 2 sesi.

### Pitfall 2: Soft-remove melanggar invariant guard 409
**What goes wrong:** Mutasi `Status` saat soft-remove → guard re-entry 409 (`JoinBatch` cek `Status=="InProgress"` + `RemovedAt==null`) jadi inkonsisten; atau menyentuh Score/cert → melanggar prinsip "sertifikat utuh".
**Why:** Spec §B2.2 + 409 D-04 secara eksplisit: soft-remove HANYA 3 kolom removal. `RemovedAt` adalah satu-satunya sumber kebenaran "removed".
**How to avoid:** Set HANYA `RemovedAt`/`RemovedBy`/`RemovalReason`. Test assert: `Status`, `Score`, `IsPassed`, `NomorSertifikat` UNCHANGED setelah soft-remove. [VERIFIED: 409-CONTEXT D-04; AssessmentSession.cs:98-103]
**Warning sign:** Guard re-entry test (ParticipantRemovalGuardTests 409) gagal setelah soft-remove.

### Pitfall 3: Idempotency tak ditangani → double-cascade error / double-audit
**What goes wrong:** `RemoveParticipantLive` dipanggil dua kali; panggilan kedua atas sesi yang sudah soft-removed mencoba cascade/soft lagi → noise audit atau error.
**Why:** UI 412 / network retry bisa kirim ulang. Spec §B2.1: `RemovedAt != null` → no-op sukses.
**How to avoid:** Guard paling awal (setelah load, sebelum has-data): `if (session.RemovedAt != null) return Json(new { sessionId, mode = "noop" });`. Untuk hard-delete kasus idempoten lebih halus — bila sesi sudah hard-deleted, load = null → 404 (acceptable, baris tak ada). [VERIFIED: spec §B2.1]
**Warning sign:** Test idempotent (panggil 2×) → panggilan kedua bukan sukses.

### Pitfall 4: Misperhitungkan AssessmentAttemptHistory FK-ke-User sebagai blocker cascade
**What goes wrong:** Mengira hard-delete sesi akan men-orphan atau gagal karena `AssessmentAttemptHistory.UserId` FK ke User dengan `OnDelete(Cascade)`.
**Why (resolved):** `AssessmentAttemptHistory` punya DUA relasi: `SessionId` (kolom biasa, no FK navigation ke Session) + `UserId` FK→User (Cascade). Cascade service menghapus history **by `SessionId`** (line 218-219), bukan by menghapus User. User tidak pernah dihapus di flow ini. Jadi `OnDelete(Cascade)` ke User TIDAK terpicu — no orphan, no blocker. [VERIFIED: RecordCascadeDeleteService.cs:218; ApplicationDbContext.cs:550-555; AssessmentAttemptHistory.cs:9-11]
**How to avoid:** Tidak ada aksi khusus — cascade existing sudah benar. Deferred concern di CONTEXT = non-issue untuk hard-delete satu sesi.
**Warning sign:** (tak ada — sudah teratasi; dokumentasikan agar planner tak buat task mubazir).

### Pitfall 5: reason-validation salah-tempat (D-02)
**What goes wrong:** Validasi `reason` wajib SEBELUM menentukan jalur → tolak hard-delete (not-started) yang sebenarnya reason-opsional. Atau tak validasi sama sekali → soft-remove tanpa jejak alasan (langgar PLIV-03).
**Why:** D-02: wajib HANYA pada jalur soft. Penentuan jalur (has-data + partner) HARUS dievaluasi dulu.
**How to avoid:** Urutan: idempotency → resolve partner → evaluasi `anyHasData` → **JIKA soft DAN reason kosong → 400** → baru eksekusi. Validasi reason di-gate oleh hasil evaluasi jalur, bukan di awal. [VERIFIED: D-02 CONTEXT]
**Warning sign:** Test "hard-delete not-started tanpa reason" → ditolak 400 (seharusnya sukses).

## Code Examples

### Resolve actor (verbatim dari produksi)
```csharp
// Source: AssessmentAdminController.cs:2394-2397 (AddParticipantsLive Langkah 6)
var hcUser = await _userManager.GetUserAsync(User);
var actorId = hcUser?.Id ?? "";
var actorName = string.IsNullOrWhiteSpace(hcUser?.NIP)
    ? (hcUser?.FullName ?? "Unknown")
    : $"{hcUser.NIP} - {hcUser.FullName}";
```

### Hard-delete invoke (verbatim pola)
```csharp
// Source: AssessmentAdminController.cs:2551 + 2618 (DeleteAssessment)
var cascade = HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>();
var result = await cascade.ExecuteAsync("session", session.Id, Enumerable.Empty<int>(), actorId, actorName);
if (!result.Success) { /* return 500 JSON dengan result.ErrorMessage (generik) */ }
```

### Audit double-log (engine + endpoint, pola DeleteAssessment)
```csharp
// Source: AssessmentAdminController.cs:2635 (audit endpoint konteks user-facing)
// (cascade.ExecuteAsync sudah menulis audit "CascadeDelete" internal; tambah ini untuk konteks RemoveParticipantLive)
await _auditLog.LogAsync(actorId, actorName, "RemoveParticipantLive",
    $"Removed participant session [ID={sessionId}] '{title}' mode={mode} reason='{reason}'",
    sessionId, "AssessmentSession");
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Dead stub `DeleteAssessmentPeserta` (grep=0, form mati EditAssessment:666) | Implement sebagai delegasi ke core remove (D-04) | Phase 411 | Tombol hapus per-peserta hidup; satu sumber kebenaran |
| `DeletePrePostGroup` by-LinkedGroupId (batch lintas-user) | Pair-as-unit by-LinkedSessionId (per peserta) untuk live-remove | Phase 411 | Hapus 1 peserta Pre/Post tak hapus seluruh batch |
| Hard-delete only (DeleteAssessment) | Hybrid by-state (hard untuk bersih, soft untuk berdata) | Phase 411 | Cert/jawaban peserta utuh + reversibel |

**Deprecated/outdated:** Tidak ada — semua primitif current (EF Core 8.0.0, kode aktif).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `DeleteAssessmentPeserta` form lama tak punya field `reason` → varian redirect mungkin perlu fallback bila peserta berdata (butuh reason) | Pattern 7 / Open Q1 | Rendah — planner putuskan: form hard-only atau tambah field. Tak ganggu endpoint JSON utama |
| A2 | Restore Pre/Post sebaiknya simetris (restore partner juga via LinkedSessionId) untuk konsistensi pair | Pattern 4 | Rendah — spec §B3 tak eksplisit soal pair pada restore; tapi simetri = konsisten. Planner konfirmasi |

**Catatan:** Hanya 2 asumsi minor (keduanya area diskresi planner, bukan klaim teknis). Semua klaim teknis inti VERIFIED file:line.

## Open Questions

1. **DeleteAssessmentPeserta (form lama) + reason-wajib (D-02) untuk peserta berdata**
   - What we know: Form `EditAssessment.cshtml:666` POST `sessionId` + `returnToId`, TANPA field `reason`. D-02 wajibkan reason pada jalur soft.
   - What's unclear: Apakah varian redirect ini boleh menangani peserta berdata (yang butuh reason) atau hanya not-started.
   - Recommendation: Untuk 411, varian redirect tangani **hard-delete** (not-started, reason opsional); bila peserta berdata via form ini → `TempData["Error"]` arahkan ke kontrol Monitoring Detail (412) yang punya modal+reason. ATAU tambah field reason ke form lama (diskresi planner). Endpoint JSON `RemoveParticipantLive` (untuk 412) tetap full hybrid. **Bukan blocker** — endpoint utama tak terpengaruh.

2. **Idempotency untuk hard-delete (sesi sudah terhapus)**
   - What we know: Soft-remove idempoten via `RemovedAt!=null` no-op. Hard-deleted = baris hilang.
   - What's unclear: Panggil ulang remove atas sesi yang sudah hard-deleted → load null → 404.
   - Recommendation: 404 dapat diterima sebagai "idempoten" untuk hard (baris memang tak ada lagi). Test boleh assert 404 atau treat-as-success — diskresi planner. Spec §B2.1 fokus pada soft-removed no-op.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8 | build/run/test | ✓ (assumed — proyek aktif net8.0) | net8.0 | — |
| SQL Server (LocalDB/Dev) | `dotnet run` + DB lokal verify | ✓ (CLAUDE.md workflow) | — | — |
| SQL Server Express (`localhost\SQLEXPRESS`) | Integration test write-path (`FlexibleParticipantAddFixture`) | ✓ (dipakai 409/410 tests) | — | InMemory untuk read-path test (pola Bagian A) |
| EF Core 8.0.0 | data-access | ✓ | 8.0.0 | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** Write-path test butuh SQLEXPRESS; read-path (idempotency/Proton-reject/restore-guard) bisa InMemory real-controller (pola `FlexibleParticipantAddLiveEligibleTests` Bagian A) bila SQLEXPRESS tak tersedia di runner.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (existing, `HcPortal.Tests/HcPortal.Tests.csproj`) |
| Config file | none (xUnit default) |
| Quick run command | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~FlexibleParticipantRemove"` |
| Full suite command | `dotnet test` (fast-suite ~394+ pasca-410) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PRMV-01 | Remove not-started (0 response) → hard-delete (baris+UPA hilang) | integration (SQLEXPRESS) | `dotnet test --filter "Name~RemoveNotStarted_HardDeletes"` | ❌ Wave 0 |
| PRMV-01 | Remove in-progress (StartedAt!=null) → soft (RemovedAt set, Score/response utuh, Status unchanged) | integration (SQLEXPRESS) | `dotnet test --filter "Name~RemoveInProgress_SoftRemoves_PreservesData"` | ❌ Wave 0 |
| PRMV-01 | Remove completed-certified → soft (NomorSertifikat+file preserved) | integration (SQLEXPRESS) | `dotnet test --filter "Name~RemoveCertified_SoftRemoves_PreservesCert"` | ❌ Wave 0 |
| PRMV-01 | Idempotent: removed sesi → no-op sukses | integration | `dotnet test --filter "Name~Remove_Idempotent_NoOp"` | ❌ Wave 0 |
| PRMV-01 | UPA bukan "data" (D-01): eager-UPA only + not-started → hard | integration (SQLEXPRESS) | `dotnet test --filter "Name~RemoveWithEagerUPA_StillHardDeletes"` | ❌ Wave 0 |
| PRMV-04 | Restore soft-removed → RemovedAt=null, muncul aktif | integration | `dotnet test --filter "Name~Restore_SoftRemoved_ClearsColumns"` | ❌ Wave 0 |
| PRMV-04 | Restore non-removed → 400/no-op | integration | `dotnet test --filter "Name~Restore_NotRemoved_Rejected"` | ❌ Wave 0 |
| PRMV-05 | Pre/Post: salah satu berdata → soft keduanya (via LinkedSessionId) | integration (SQLEXPRESS) | `dotnet test --filter "Name~RemovePrePost_OneHasData_SoftBoth"` | ❌ Wave 0 |
| PRMV-05 | Pre/Post: keduanya bersih → hard keduanya | integration (SQLEXPRESS) | `dotnet test --filter "Name~RemovePrePost_BothClean_HardBoth"` | ❌ Wave 0 |
| PLIV-03 | Audit row tertulis (Remove + Restore) | integration | `dotnet test --filter "Name~Remove_WritesAuditRow"` | ❌ Wave 0 |
| PLIV-03 | reason-wajib pada soft (D-02): soft tanpa reason → 400 | integration | `dotnet test --filter "Name~RemoveSoft_NoReason_Rejected"` | ❌ Wave 0 |
| (scope) | Proton reject → 400 | integration/InMemory | `dotnet test --filter "Name~Remove_Proton_Rejected"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "FullyQualifiedName~FlexibleParticipantRemove"` (< 30s subset)
- **Per wave merge:** `dotnet test` (full fast-suite, no regression — termasuk 409 guard + 410 add)
- **Phase gate:** Full suite green + `dotnet run` (localhost:5277) sebelum commit (CLAUDE.md Develop Workflow)

### Test Infrastructure (REUSE 410)
- **Read-path / logic-only (idempotency, Proton-reject, restore-guard, reason-validation):** InMemory real-controller — pola `FlexibleParticipantAddLiveEligibleTests` Bagian A (`MakeController` line 52-85). Drive action ASLI, assert JSON/kolom in-memory. UserManager/Notif null aman bila action tak panggil.
- **Write-path (hard-delete cascade, soft-remove, Pre/Post pair, audit):** SQLEXPRESS disposable — REUSE `FlexibleParticipantAddFixture` (`HcPortalDB_Test_{guid}`, `MigrateAsync`, line 20-46). Stub `StubUserManager` (override `GetUserAsync` → actor) + `NoopNotificationService` (pola `FlexibleParticipantAddLiveWriteTests` line 214-285). **CATATAN:** `RemoveParticipantLive` hard-delete resolve `RecordCascadeDeleteService` via `HttpContext.RequestServices` — test butuh `ControllerContext.HttpContext.RequestServices` ber-service. Lihat Wave 0 Gap (service provider stub).
- **De-tautology (WAJIB, lesson 999.12):** Setiap test MENJALANKAN action ASLI / assert kolom DB NYATA. JANGAN replica predikat has-data. Hard-delete assert: row + UPA + response benar-benar hilang (`AnyAsync == false`). Soft assert: `RemovedAt` set NYATA, Score/cert UNCHANGED.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/FlexibleParticipantRemoveTests.cs` (NEW) — covers PRMV-01/04/05 + PLIV-03 (kelompokkan read-path InMemory + write-path SQLEXPRESS, pola 2-bagian seperti `FlexibleParticipantAddLiveTests`)
- [ ] **Service-provider stub untuk RecordCascadeDeleteService** — hard-delete path panggil `HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>()`. Test write-path harus set `ControllerContext.HttpContext.RequestServices` ke `ServiceProvider` yang punya `RecordCascadeDeleteService` (+ deps: `ProtonCompletionService`, `AuditLogService`, `IWebHostEnvironment`, `ILogger`). **Ini gap test-infra terbesar 411** — 410 tak butuh ini (add tak cascade). Planner: pertimbangkan `ServiceCollection().AddScoped(...)` mini-DI di fixture, atau test hard-delete path via `RecordCascadeDeleteService` langsung + test soft/idempotent/Proton via controller.
- [ ] Seed helper: sesi in-progress (`StartedAt` set + ≥1 `PackageUserResponse`) + completed-certified (`NomorSertifikat` + `Status="Completed"`) + Pre/Post pair (`LinkedSessionId` cross-set) — extend `SeedRepSessionAsync` (line 300).
- [ ] Framework install: none — xUnit + fixture existing.

*(Catatan: backlog 999.12 menyarankan `WebApplicationFactory` untuk de-tautology penuh; 411 boleh ikut pola 410 yang sudah di-review hijau — escalate ke WebApplicationFactory hanya bila service-provider stub jadi rumit.)*

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Server-authoritative state decision (hard/soft tentukan di server dari kolom DB, bukan client) |
| V2 Authentication | yes | `[Authorize(Roles="Admin, HC")]` di semua 3 endpoint (mirror AddParticipantsLive:2354) |
| V3 Session Management | no | ASP.NET Identity existing, tak diubah |
| V4 Access Control | yes | RBAC Admin/HC; keputusan #5 longgarkan EnsureCanDeleteAsync untuk soft (mitigasi audit wajib). IDOR: `sessionId` di-load + validasi exist; partner via `LinkedSessionId` server-resolved (tak trust client) |
| V5 Input Validation | yes | `sessionId` int (model-bind), `reason` string (max 500 — kolom `RemovalReason` HasMaxLength 500 dari 409). Validasi reason-wajib pada soft (D-02). Antiforgery `[ValidateAntiForgeryToken]` |
| V6 Cryptography | no | Tak ada crypto baru |
| V7 Error Handling/Logging | yes | Cascade return pesan GENERIK (no info leak, RecordCascadeDeleteService:289). Audit setiap remove/restore (PLIV-03 = non-repudiation) |
| V11 Business Logic | yes | Idempotency (anti double-action), Pre/Post atomicity, Proton-reject (anti korup ProtonTrack) |

### Known Threat Patterns for ASP.NET Core MVC + EF Core

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF pada POST mutasi | Spoofing/Tampering | `[ValidateAntiForgeryToken]` (VERIFIED pola :2355) + form antiforgery token (EditAssessment:667) |
| IDOR (hapus sesi orang lain via id tebakan) | Tampering | `[Authorize(Roles="Admin, HC")]` = hanya admin/HC; mereka memang berwenang lintas-peserta. Partner Pre/Post resolved server-side via LinkedSessionId (tak terima id partner dari client) |
| Privilege escalation (HC hapus cert) | Elevation | Keputusan #5: soft-remove (cert utuh, reversibel) + audit wajib `RemovedBy`/`RemovalReason` (mitigasi, BUKAN block) |
| Repudiation (siapa hapus apa) | Repudiation | `AuditLogService.LogAsync` setiap Remove/Restore (actorId+name+reason+timestamp) — PLIV-03 |
| Mass-delete DoS | DoS | Endpoint single-session (sessionId tunggal), bukan batch — risiko rendah. (AddParticipantsLive cap 50; remove tak perlu cap karena 1 sesi/call) |
| Proton track corruption | Tampering | Reject `Category=="Assessment Proton"` (spec §F, mirror AddParticipantsLive:2368) |
| Information leak via error | Information Disclosure | Cascade error generik "Gagal menghapus..." (V7); detail hanya ke logger |

## Sources

### Primary (HIGH confidence — VERIFIED file:line this session)
- `Services/RecordCascadeDeleteService.cs:175-314` — `ExecuteAsync` signature + 9-tabel cascade + **UPA cleanup line 221-222** + file cert post-commit + audit
- `Controllers/AssessmentAdminController.cs:2348-2542` — `AddParticipantsLive` (audit/actor/Proton/window pattern) + `GetEligibleParticipantsToAdd`:2294 + `BuildReadyParticipantSession`:2322 + `CreateEagerAssignmentsAsync`:2506
- `Controllers/AssessmentAdminController.cs:2548-2665` — `DeleteAssessment` (hard-delete cascade invoke pattern + service resolve via RequestServices:2551)
- `Controllers/AssessmentAdminController.cs:2830-2959` — `DeletePrePostGroup` (by-LinkedGroupId — partner-handling reference, BUKAN model langsung untuk single-user pair)
- `Controllers/AssessmentAdminController.cs:7465-7542` — `EnsureCanDeleteAsync` (Admin override + HC block logic)
- `Controllers/AdminBaseController.cs:248-249` — `IsPrePostSession` static
- `Services/AuditLogService.cs:21-27` — `LogAsync` signature
- `Models/AssessmentSession.cs:98-103` — `RemovedAt`/`RemovedBy`/`RemovalReason` (409)
- `Models/AssessmentSession.cs:130-186` — `LinkedSessionId`/`LinkedGroupId`/`AssessmentType` semantik
- `Models/AssessmentAttemptHistory.cs:9-11` + `Data/ApplicationDbContext.cs:550-555` — FK UserId Cascade (Pitfall 4 resolution)
- `Models/PackageUserResponse.cs:11` — `AssessmentSessionId` (has-data count)
- `Views/Admin/EditAssessment.cshtml:666-670` — dead form `DeleteAssessmentPeserta` (sessionId + returnToId)
- `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` (full) — test infra pattern (read InMemory + write SQLEXPRESS, stubs)
- `HcPortal.Tests/FlexibleParticipantAddTests.cs:20-46` — `FlexibleParticipantAddFixture` (HcPortalDB_Test_{guid}, MigrateAsync)
- `HcPortal.Tests/ParticipantRemovalGuardTests.cs:1-60` — 409 guard test pattern (de-tautology)
- `*.csproj` grep — EF Core 8.0.0, net8.0
- `.planning/config.json` — nyquist_validation:true, security_enforcement absent (=enabled)

### Secondary (MEDIUM)
- `.planning/phases/410-*/410-01-SUMMARY.md` — eager-UPA (A1), Pre/Post cross-link, helper availability
- `.planning/phases/409-*/409-CONTEXT.md` — soft-removed definisi, guard re-entry invariant
- `docs/superpowers/specs/2026-06-19-flexible-add-remove-participant-design.md` §B2/B3/B5/F/G/H

### Tertiary (LOW)
- None — semua klaim terverifikasi via codebase.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — zero package baru, semua aset existing VERIFIED file:line
- Architecture: HIGH — cascade/soft/audit/Pre-Post semua dipetakan ke kode nyata; D-01 (UPA cleanup) resolved oleh inspeksi langsung service
- Pitfalls: HIGH — Pitfall 1 (LinkedSessionId vs LinkedGroupId) + Pitfall 4 (AttemptHistory non-issue) keduanya VERIFIED
- Test infra: HIGH — reuse 410 pattern; satu gap nyata teridentifikasi (service-provider stub untuk cascade)

**Research date:** 2026-06-21
**Valid until:** 2026-07-21 (stable — kode internal, EF 8.0.0 pinned; tak ada dependency fast-moving). Re-verify file:line bila 412/413 mengubah `AssessmentAdminController.cs` sebelum 411 dieksekusi (file edit sequential).
