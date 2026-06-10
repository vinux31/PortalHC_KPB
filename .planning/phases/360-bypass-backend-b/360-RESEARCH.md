# Phase 360: Bypass Backend (B) - Research

**Researched:** 2026-06-10
**Domain:** ASP.NET Core 8 MVC backend — EF Core migration + domain service + GradingService hook + 6 controller endpoint (fitur Bypass Tahun Proton)
**Confidence:** HIGH (spec terkunci + semua integration point diverifikasi langsung di kode sesi ini)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (Implementation Decisions, salin verbatim)

**CL-B(b) konfigurasi exam**
- **D-01:** CL-B(b) = **2-step (Opsi B)**. `BypassSave` bikin `AssessmentSession` source-year **"bare"** di dalam transaksi (`Category="Assessment Proton"`, `ProtonTrackId`=source, `TahunKe`=S, jadwal/durasi/KKM default atau dari form) **TANPA paket soal**. HC lampirkan paket soal lewat alur **ManagePackages / Kelola Assessment existing**. `LinkedAssessmentSessionId` nunjuk sesi bare ini.
- **D-02:** **WAJIB pengingat TempData** setelah `BypassSave` CL-B(b): "Sesi exam dibuat — lampirkan paket soal di Kelola Assessment sebelum worker bisa ujian."
- **D-03 (MISS-2):** CL-B(b) source exam **selalu online (Tahun 1/2)** — naik cuma 1→2 / 2→3. Klausa "Tahun 3=interview" di spec §4 tidak relevan untuk CL-B(b).

**Exempt gate antar-tahun**
- **D-04:** **Stempel permanen (Opsi A).** `BypassSave` set kolom baru `Origin="Bypass"` di `ProtonTrackAssignment` target. Kolom `string? Origin` nullable; baris lama = null (= "Normal") → **tidak perlu backfill**. Nilai = {null, "Bypass"}. **Gabung ke migration #2** (`PendingProtonBypass`) → 1 operasi migration, 2 tabel. Notify IT.
- **D-05:** Exempt **CUMA cross-year prereq, BUKAN gate deliverable 100%.** Stempel cuma lewatkan cek "Tahun N-1 lulus". Gate deliverable 100% target-year **TETAP berlaku**.
- **D-06:** Isi **2 titik exempt** (cek `assignment aktif worker punya Origin=="Bypass"` untuk `ProtonTrackId` itu): (a) `CreateAssessment` gate `AssessmentAdminController.cs:1368` — `|| isBypassAssignment` sebelum skip cross-year; (b) Placeholder `CoachMappingController.cs:533`.
- **D-07:** **Renewal exempt tetap session-based** (`RenewsSessionId`/`RenewsTrainingId`). TERPISAH dari stempel assignment.

**Penempatan logic + transaksi**
- **D-08:** **`ProtonBypassService` (Opsi A)** — komponen sendiri, scoped DI di `Program.cs`, dipakai `ProtonDataController` (6 endpoint) DAN hook notif `GradingService` §7. Inject `ApplicationDbContext` + `ProtonCompletionService` + `NotificationService` + `IAuditLogService`.
- **D-09:** **Transaksi all-or-nothing** per operasi pakai pola Phase 333/334 (`BeginTransactionAsync` + `CommitAsync` + catch `DbUpdateException`). Tidak ada file op (E10 KEEP orphan) → tidak ada post-commit File.Delete.

**Guard rail pending (ketiganya dipasang)**
- **D-10:** **Blok dobel pending — SEMUA mode.** Worker punya `PendingProtonBypass` Status ∈ {Menunggu, Siap} → tolak bypass baru apapun modenya.
- **D-11:** **Cek ulang saat konfirmasi.** Sebelum eksekusi pindah: (a) assignment asal masih sama, (b) exam beneran lulus (`LinkedAssessmentSession.IsPassed` + penanda Origin="Exam" ada), (c) pending masih Status="Siap".
- **D-12:** **Konfirmasi anti-dobel (atomik).** Transisi Siap→Selesai dikunci atomik (conditional update `WHERE Status="Siap"` / rowsAffected guard pola `:3729`).

**Detail spec di-baking**
- **D-13:** Force-approve deliverable tulis `DeliverableStatusHistory` `StatusType="Bypassed-AutoApprove"` + `ActorId/Name/Role`=HC + Timestamp.
- **D-14:** Step `cancel exam aktif S` (E5) — cancel exam in-progress source-year, **kecuali** AssessmentSession CL-B(b) yang baru dibuat.
- **D-15:** Re-grade Pass→Fail exam "Siap" → pending balik `Status="Menunggu"` (penanda Origin="Exam" dihapus per A-M1).
- **D-16:** Coach `TargetCoachId == null` = **pertahankan** mapping existing; diisi = deactivate aktif lama DULU → create baru (E15 filtered-unique).

### Claude's Discretion
- Default nilai jadwal/durasi/KKM sesi bare CL-B(b).
- Penamaan method `ProtonBypassService` + pemecahan per closure mode (strategy/switch).
- Mekanisme extract `AutoCreateProgressForAssignment` + create-coach-mapping agar reusable (extract ke helper/service vs internal-visible).
- Strategi test (xUnit real-SQL disposable fixture pola Phase 344 TEST-05 + integration gate exempt).

### Deferred Ideas (OUT OF SCOPE)
- **Semua UI bypass** (Tab2 redesign, wizard 3-langkah, panel pending, notif deep-link, e2e UAT) → **Phase 361** (PBYP-08..10).
- **Audit/improve Tab1 Override Deliverable** → backlog 999.x.
- **Undo bypass executed** (tombol undo) → tidak ada (spec §8.2 Opsi C).
- **Menghidupkan level kompetensi** → dibuang (A-3, dormant).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PBYP-01 | Tabel `PendingProtonBypass` (migration #2) — lifecycle Menunggu→Siap→Selesai/Dibatalkan | Migration pattern verified (`Migrations/20260610014907_*.cs`); DbSet registration pola `ApplicationDbContext.cs:38-43`; model field list spec §6 |
| PBYP-02 | 4 closure mode (CL-A/B(a)/B(b)/C) + validasi \|Δtahun\|≤1 + 1-assignment-aktif | Decision tree spec §5; `ProtonCompletionService.EnsureAsync` reuse (Origin="Bypass"); ordering pitfall (lihat Pitfall 1) |
| PBYP-03 | Exam CL-B(b) lulus → pending "Siap" + notif `PROTON_BYPASS_READY` (GradingService flip flag) | 3 hook point GradingService diverifikasi (lihat Architecture); `NotificationService._templates` pattern `:34` |
| PBYP-04 | Coach handling — deactivate mapping aktif lama → create baru (E15); HC bisa ganti coach | Filtered-unique `ApplicationDbContext.cs:326`; pola deactivate+create `CoachMappingController.cs:573-619` |
| PBYP-05 | Bootstrap deliverable target pakai Unit dari **form** (bukan mapping) | `AutoCreateProgressForAssignment` `CoachMappingController.cs:1424` resolve unit dari mapping → harus diparametrisasi (Pitfall 4) |
| PBYP-06 | HC batal pending (auto-cancel exam: belum-dikerjakan→hapus, sudah-lulus→pertahankan) | spec §8.1; endpoint `BypassCancelPending` |
| PBYP-07 | 6 endpoint `[Authorize(Admin,HC)]` + AntiForgery + audit | Pola `OverrideSave` `ProtonDataController.cs:1400`; `_auditLog.LogAsync` signature `AuditLogService.cs:21` |
</phase_requirements>

## Summary

Phase 360 = backend murni fitur **Bypass Tahun** di atas fondasi yang sudah shipped (358 penanda+`Origin`, 359 gate). Spec B (`2026-06-09-proton-bypass-tahun-design.md`) **terkunci** dan CONTEXT.md sudah memutuskan 16 keputusan implementasi (D-01..D-16). Riset ini **bukan eksplorasi** — semua keputusan teknologi sudah dibuat; tugas riset adalah **memverifikasi setiap integration point di kode aktual** dan mengangkat pitfall yang tidak terlihat dari spec.

Stack = existing: **ASP.NET Core 8 MVC + EF Core 8.0.0 (SQL Server `localhost\SQLEXPRESS`, `HcPortalDB_Dev`) + xUnit**. Tidak ada library baru. Pola yang dipakai semuanya sudah ada di codebase: `ProtonCompletionService` (358, untuk penanda), transaksi `BeginTransactionAsync` (Phase 333/334), atomic guard `ExecuteUpdateAsync` rowsAffected (Phase 310/grading), filtered-unique index (E15), notif template (`NotificationService`), audit log (`AuditLogService`).

**Temuan riset paling kritikal** (yang gampang ke-miss planner): (1) `ProtonCompletionService.EnsureAsync` resolve assignment lewat filter `IsActive` → **urutan "terbitkan penanda source SEBELUM deactivate source assignment" wajib dijaga**, kalau dibalik penanda gak terbit. (2) Hook notif GradingService harus dipasang di **3 titik**, bukan 1 — `GradeAndCompleteAsync` early-return saat ada essay (`:229`) → sesi bare CL-B(b) yang punya soal essay TIDAK lewat hook utama, hanya lewat `FinalizeEssayGrading`. (3) `AutoCreateProgressForAssignment` resolve unit dari **active mapping**, bukan parameter → tidak bisa di-reuse apa adanya untuk PBYP-05 (unit dari form). (4) `AuditLogService.LogAsync` + `EnsureAsync` + bootstrap semuanya `SaveChangesAsync` internal → di dalam `BeginTransactionAsync` aman, tapi hook GradingService TIDAK boleh buka transaksi sendiri (jalan di hot-path grading).

**Primary recommendation:** Buat `ProtonBypassService` scoped (pola `ProtonCompletionService`), dengan method orchestrator per operasi yang **membuka transaksi sendiri** (BypassSave/Confirm/CancelPending), PLUS satu method ringan `MarkPendingReadyIfAnyAsync(session)` / reverse yang **TIDAK** buka transaksi (dipanggil dari dalam GradingService). Ekstrak helper bootstrap+coach-mapping yang **menerima unit eksplisit** dari `CoachMappingController` agar filter unit konsisten. Gate exempt = cek `ProtonTrackAssignment.Origin=="Bypass"` di 2 titik, jaga eligibility 100% tetap utuh.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Validasi + eksekusi bypass (decision tree §5) | API/Backend (`ProtonBypassService`) | — | Logic bisnis murni, testable, dishare controller + GradingService (D-08) |
| Schema `PendingProtonBypass` + `Origin` | Database/Storage (EF migration) | — | Migration #2; snapshot DB dulu (SEED_WORKFLOW) |
| Notif flip flag CL-B(b) lulus | API/Backend (GradingService hook) | Notif (`NotificationService`) | Coupling ringan — flip+notif saja, BUKAN eksekusi pindah (spec §7) |
| Penanda kelulusan (Origin marker) | API/Backend (`ProtonCompletionService` reuse) | — | Sumber tunggal 358 (A-4); jangan duplikat |
| Gate exempt cross-year | API/Backend (gate 2 titik) | — | Cek `Origin=="Bypass"`; gate 100% TETAP (D-05) |
| Bootstrap deliverable + coach mapping | API/Backend (helper extracted) | Database | Unit-filtered; harus parametrik unit (PBYP-05) |
| 6 endpoint Authorize/AntiForgery/audit | API/Backend (`ProtonDataController`) | — | Pola `OverrideSave`; UI = Phase 361 |

## Standard Stack

### Core (existing — TIDAK ada library baru)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller + DI + `[Authorize]`/`[ValidateAntiForgeryToken]` | Framework proyek [VERIFIED: HcPortal.csproj:4] |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | ORM + migration | Stack data proyek [VERIFIED: HcPortal.csproj:18] |
| xUnit | (test proj) | Unit + integration test (`[Fact]`, `[Trait]`) | 130 `[Fact]`/`[Theory]` existing [VERIFIED: grep HcPortal.Tests] |

### Supporting (services existing yang di-inject)
| Service | Purpose | When to Use |
|---------|---------|-------------|
| `ProtonCompletionService` | `EnsureAsync(coacheeId, trackId, createdById, origin, notes)` terbitkan penanda; `RemoveExamOriginAsync` | CL-B(a) + konfirmasi B(b) Origin="Bypass"/"Exam" [VERIFIED: Services/ProtonCompletionService.cs:36,70] |
| `GradingService` | `GradeAndCompleteAsync` (:52) + `RegradeAfterEditAsync` (:432) — titik hook notif §7 | Pasang `MarkPendingReadyIfAnyAsync` setelah `EnsureAsync` [VERIFIED] |
| `NotificationService` | `SendAsync(userId, type, title, message, actionUrl)` + `_templates` dict + `SendByTemplateAsync` | Tambah template `PROTON_BYPASS_READY` [VERIFIED: Services/NotificationService.cs:34,100,231] |
| `AuditLogService` (`_auditLog`) | `LogAsync(actorUserId, actorName, actionType, description, targetId?, targetType?)` | Tiap operasi bypass [VERIFIED: Services/AuditLogService.cs:21] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `ProtonBypassService` (D-08 Opsi A) | Inline di controller | DITOLAK user — susah dites + tak bisa dishare GradingService hook |
| Kolom `Origin` di assignment (D-04 Opsi A) | Andalkan penanda lulus saja | DITOLAK — CL-C geser track bisa nyangkut di gate |

**Installation:** Tidak ada `npm`/`dotnet add package`. Migration via:
```bash
dotnet ef migrations add AddPendingProtonBypassAndAssignmentOrigin --context ApplicationDbContext
dotnet ef database update --context ApplicationDbContext   # SNAPSHOT DB lokal dulu (SEED_WORKFLOW)
```

**Version verification (diverifikasi sesi ini):**
- dotnet SDK: `8.0.418` [VERIFIED: dotnet --version]
- dotnet ef CLI: `10.0.3` [VERIFIED: dotnet ef --version] — CLI lebih baru dari package EF Core 8.0.0; aman (CLI = launcher, design-time pakai package proyek). Migration #1 `AddOriginToProtonFinalAssessment` (2026-06-10) sukses dengan toolchain ini.
- sqlcmd: `17.0.1000.7` [VERIFIED] — untuk BACKUP/RESTORE snapshot.

## Architecture Patterns

### System Architecture Diagram

```
                          ┌─────────────────────────────────────────┐
   HC/Admin (browser) ───▶│  ProtonDataController  [Authorize Admin,HC]│
   (UI = Phase 361)       │  6 endpoint bypass + AntiForgery + audit  │
                          └───────────────┬───────────────────────────┘
                                          │ delegasi
                                          ▼
        ┌──────────────────────────────────────────────────────────────┐
        │  ProtonBypassService (scoped DI)                               │
        │  ┌────────────────────────────────────────────────────────┐  │
        │  │ BypassSave(req)  ── validasi §5 ──▶ switch(mode)         │  │
        │  │   CL-A/B(a)/C ─▶ §5.1 PINDAH-INSTAN (1 transaksi)        │  │
        │  │   CL-B(b)     ─▶ §5.2 BUAT-TUNGGU  (1 transaksi)         │  │
        │  │ BypassConfirm(pendingId) ─▶ §5.3 (atomic Siap→Selesai)   │  │
        │  │ BypassCancelPending(pendingId) ─▶ §8.1                    │  │
        │  │ MarkPendingReadyIfAnyAsync(session)  ◀── (no own tx)      │  │
        │  └──────┬───────────────┬──────────────┬───────────┬────────┘  │
        └─────────│───────────────│──────────────│───────────│───────────┘
                  ▼               ▼              ▼           ▼
        ProtonCompletion-   AuditLog-    Notification-   ApplicationDbContext
        Service             Service      Service         (PendingProtonBypass,
        (Origin penanda)    (jejak)      (PROTON_BYPASS  ProtonTrackAssignment.Origin,
                                          _READY)         CoachCoacheeMapping E15)
                  ▲
                  │ hook §7 (flip flag, BUKAN pindah)
   Worker submit exam ─▶ GradingService.GradeAndCompleteAsync ─┐
                       ▶ GradingService.RegradeAfterEditAsync ─┤─▶ EnsureAsync(Origin="Exam")
                       ▶ AssessmentAdminController.FinalizeEssayGrading ┘   THEN
                                                                MarkPendingReadyIfAnyAsync(session)
```
Trace use-case utama (CL-B(b)): HC `BypassSave` → service buka tx → force-approve source + bikin sesi bare + insert pending "Menunggu" + audit → commit → TempData reminder. Worker kerjakan exam → GradingService grade → lulus → `EnsureAsync(Exam)` + `MarkPendingReadyIfAnyAsync` flip pending "Siap" + notif HC. HC `BypassConfirm` → service §5.1 pindah atomik → pending "Selesai".

### Pola yang WAJIB diikuti

**Pattern 1: Service scoped + reuse helper (pola `ProtonCompletionService`)**
**What:** `ProtonBypassService` di-`AddScoped` di `Program.cs` (sebaris `ProtonCompletionService`).
**Source:** [VERIFIED: Program.cs:54-64]
```csharp
builder.Services.AddScoped<HcPortal.Services.ProtonBypassService>();
```
Inject ke `ProtonDataController` (yang sudah punya `_auditLog`) DAN ke `GradingService` ctor (yang sudah inject `ProtonCompletionService`).

**Pattern 2: Transaksi all-or-nothing (Phase 333/334, D-09)**
**Source:** [VERIFIED: CoachMappingController.cs:565-646]
```csharp
await using var tx = await _context.Database.BeginTransactionAsync();
try
{
    // ... semua step (EnsureAsync, bootstrap, coach, insert pending) — masing-masing SaveChangesAsync internal OK di dalam tx
    await _context.SaveChangesAsync();
    await tx.CommitAsync();
}
catch (DbUpdateException dbEx) when (
    dbEx.InnerException?.Message.Contains("IX_CoachCoacheeMappings_CoacheeId_ActiveUnique") == true
    || dbEx.InnerException?.Message.Contains("2601") == true
    || dbEx.InnerException?.Message.Contains("2627") == true)
{
    await tx.RollbackAsync();
    return Json(new { success = false, message = "Coachee sudah punya coach aktif. Nonaktifkan dulu." });
}
catch (Exception ex)
{
    await tx.RollbackAsync();
    return Json(new { success = false, message = "Gagal. Operasi dibatalkan." }); // JANGAN expose ex.Message (D6, Phase 334)
}
```

**Pattern 3: Atomic status transition guard (D-12, pola Phase 310/grading)**
**Source:** [VERIFIED: Services/GradingService.cs:234-251] (`ExecuteUpdateAsync` + WHERE + rowsAffected==0 guard)
```csharp
var rowsAffected = await _context.PendingProtonBypasses
    .Where(p => p.Id == pendingId && p.Status == "Siap")
    .ExecuteUpdateAsync(p => p
        .SetProperty(x => x.Status, "Selesai")
        .SetProperty(x => x.ResolvedAt, DateTime.UtcNow));
if (rowsAffected == 0) { /* race / sudah diproses → tolak ramah */ }
```

**Pattern 4: Penanda via helper (reuse 358, JANGAN duplikat)**
**Source:** [VERIFIED: Services/ProtonCompletionService.cs:36] + pemakaian existing `AssessmentAdminController.cs:3865` (Origin="Interview")
```csharp
await _protonCompletionService.EnsureAsync(coacheeId, sourceTrackId, hcId, "Bypass", reason);
// ⚠️ EnsureAsync resolve assignment WHERE IsActive — panggil SEBELUM deactivate source (Pitfall 1)
```

**Pattern 5: Notif template (PROTON_BYPASS_READY)**
**Source:** [VERIFIED: Services/NotificationService.cs:34-91 + 231-258]
```csharp
// di _templates dict:
["PROTON_BYPASS_READY"] = new NotificationTemplate {
    Title = "Bypass Siap Diselesaikan",
    // Message template + ActionUrlTemplate = "/ProtonData/Override?tab=bypass&pending={PendingId}"
},
// kirim ke InitiatedById (HC inisiator), BUKAN worker:
await _notificationService.SendByTemplateAsync(pending.InitiatedById, "PROTON_BYPASS_READY",
    new Dictionary<string, object> { ["PendingId"] = pending.Id });
```

**Pattern 6: Migration multi-tabel (D-04, 1 file 2 tabel)**
**Source:** [VERIFIED: Migrations/20260610014907_AddOriginToProtonFinalAssessment.cs]
```csharp
protected override void Up(MigrationBuilder mb)
{
    mb.AddColumn<string>(name: "Origin", table: "ProtonTrackAssignments",
        type: "nvarchar(20)", maxLength: 20, nullable: true);     // null = Normal, no backfill (D-04)
    mb.CreateTable(name: "PendingProtonBypasses", /* §6 kolom */); // PLURAL — DbSet konvensi proyek
}
```

### Recommended Project Structure (delta)
```
Models/ProtonModels.cs           # + PendingProtonBypass class; + Origin di ProtonTrackAssignment (:71)
Services/ProtonBypassService.cs  # BARU — orchestrator 4 closure mode + hook method
Services/GradingService.cs       # + inject ProtonBypassService + 3 hook MarkPendingReady
Controllers/ProtonDataController.cs       # + 6 endpoint bypass (pola OverrideSave :1400)
Controllers/AssessmentAdminController.cs  # :1368 tambah || isBypassAssignment; :3754 hook essay
Controllers/CoachMappingController.cs     # :533 isi exempt; extract AutoCreateProgressForAssignment (parametrik unit)
Data/ApplicationDbContext.cs     # + DbSet<PendingProtonBypass> + index config
Migrations/<ts>_AddPending...    # migration #2
HcPortal.Tests/ProtonBypassServiceTests.cs # BARU — pola ProtonCompletionFixture real-SQL
```

### Anti-Patterns to Avoid
- **Membuka transaksi di dalam hook GradingService:** `MarkPendingReadyIfAnyAsync` dipanggil dari `GradeAndCompleteAsync`/`FinalizeEssayGrading` yang berjalan di hot-path grading. Hook = flip flag + notif saja, **tanpa** `BeginTransactionAsync` (hindari nested tx + jangan eksekusi pindah di grading, spec §7).
- **Reuse `AutoCreateProgressForAssignment` apa adanya untuk bootstrap target:** ia resolve unit dari active mapping (`:1429`), bukan dari form → langgar PBYP-05. Harus parametrik unit (Pitfall 4).
- **Deactivate source assignment sebelum terbitkan penanda:** `EnsureAsync` butuh assignment `IsActive` (Pitfall 1).
- **Expose `ex.Message` mentah di Json error:** info-leak (D6 Phase 334) — detail ke logger saja.
- **Lupa cabang essay early-return** GradingService `:229` → hook penanda + notif ke-skip untuk sesi bare ber-essay.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Terbitkan/cek penanda kelulusan | Insert `ProtonFinalAssessment` manual | `ProtonCompletionService.EnsureAsync` (Origin="Bypass") | Dedup + resolve assignment + dormant level sudah ditangani (358) [VERIFIED] |
| Hapus penanda saat re-grade fail | Query+delete manual | `RemoveExamOriginAsync` | Selektif Exam-only — Bypass/Interview kebal (A-M9) [VERIFIED:ProtonCompletionService.cs:70] |
| Cek "Tahun N-1 lulus" | Loop deliverable Approved | `IsPrevYearPassedAsync` / `ProtonYearGate.IsAllowed` | Penanda-based, sudah dipakai gate 359 [VERIFIED:ProtonCompletionService.cs:107] |
| Bootstrap deliverable progress | Tulis loop insert + StatusHistory baru | Extract dari `AutoCreateProgressForAssignment` (parametrik unit) | Filter unit `.Trim()` 2-sisi + initial "Pending" StatusHistory identik [VERIFIED:CoachMappingController.cs:1424-1493] |
| Enforce 1-coach-aktif | Cek manual di app | Filtered-unique index + catch 2601/2627 | DB-level guarantee E15 [VERIFIED:ApplicationDbContext.cs:326] |
| Notif | Insert Notification + render URL manual | `SendByTemplateAsync` / `SendAsync` | Template + actionUrl substitution [VERIFIED:NotificationService.cs:231] |
| Audit | Insert `AuditLog` manual | `_auditLog.LogAsync(...)` | Field + timestamp konsisten [VERIFIED:AuditLogService.cs:21] |
| Atomic claim transition | Lock/SELECT-then-UPDATE | `ExecuteUpdateAsync` + WHERE + rowsAffected | Race-safe single-statement [VERIFIED:GradingService.cs:234] |

**Key insight:** Hampir seluruh "kerja" Phase 360 = **mengorkestrasi service yang sudah ada di urutan + transaksi yang benar**. Bahaya terbesar bukan menulis logic baru, tapi (a) urutan pemanggilan yang salah relatif terhadap filter `IsActive`, dan (b) filter unit yang tidak konsisten saat bootstrap.

## Common Pitfalls

### Pitfall 1: `EnsureAsync` resolve assignment via `IsActive` → urutan penanda vs deactivate
**What goes wrong:** CL-B(a)/konfirmasi yang men-deactivate assignment source DULU lalu panggil `EnsureAsync` → `EnsureAsync` cari assignment `IsActive==true` untuk (coachee, sourceTrack), tidak ketemu (sudah inactive) → return false → **penanda tidak terbit** → tahun asal tidak pernah "Lulus".
**Why:** `EnsureAsync` filter `a.IsActive` [VERIFIED: ProtonCompletionService.cs:38-39].
**How to avoid:** Patuhi urutan spec §5.1: `[tutup tahun asal: force-approve + EnsureProtonFinalAssessment(source)]` → BARU `[deactivate assignment S]` → `[aktifkan target T]`. Tulis test yang assert penanda source ada setelah CL-B(a).
**Warning signs:** CL-B(a) jalan tapi dashboard source tetap "belum lulus".

### Pitfall 2: Hook notif harus di 3 titik (essay early-return)
**What goes wrong:** Pasang `MarkPendingReadyIfAnyAsync` cuma di `GradeAndCompleteAsync` non-essay path. Sesi bare CL-B(b) yang kebetulan punya soal **Essay** → `GradeAndCompleteAsync` early-return di `:229` (status "Menunggu Penilaian"), tidak pernah sampai hook → pending nyangkut "Menunggu" selamanya walau worker lulus.
**Why:** Cabang `hasEssay` return lebih awal [VERIFIED: GradingService.cs:193-230]; finalisasi essay terjadi di `AssessmentAdminController.FinalizeEssayGrading` `:3754`.
**How to avoid:** Pasang hook di **3 titik** (sama persis pola `EnsureAsync` existing):
1. `GradeAndCompleteAsync` setelah `EnsureAsync` `:304-309` (exam langsung lulus).
2. `RegradeAfterEditAsync` — cabang Fail→Pass `:531-534` (flip ke Siap) **dan** cabang Pass→Fail `:485-487` (D-15: pending balik "Menunggu", setelah `RemoveExamOriginAsync`).
3. `AssessmentAdminController.FinalizeEssayGrading` setelah `EnsureAsync` `:3754-3759` (essay finalisasi lulus).
**Warning signs:** Pending tidak pernah jadi "Siap" untuk exam ber-essay; re-grade fail tidak revert pending.

### Pitfall 3: `AutoCreateProgressForAssignment` resolve unit dari mapping, bukan form
**What goes wrong:** Reuse helper apa adanya → bootstrap pakai `CoachCoacheeMapping.AssignmentUnit` active worker (`:1429`). Tapi PBYP-05/D-05 minta unit dari **form bypass**. Worker yang ganti unit lewat bypass dapat deliverable unit LAMA.
**Why:** Helper hardcode resolve-from-mapping [VERIFIED: CoachMappingController.cs:1428-1441].
**How to avoid:** Ekstrak helper varian yang **terima `string resolvedUnit` eksplisit** (Claude's Discretion). ATAU pastikan urutan: create coach-mapping baru DENGAN unit form DULU, baru bootstrap (helper baca mapping baru). Opsi parameter-eksplisit lebih aman (independent dari urutan coach). Jaga filter `.Trim()` 2-sisi identik supaya konsisten dengan gate 100% `AssessmentAdminController.cs:1382`.
**Warning signs:** Worker bypass ganti unit → progress deliverable mismatch dengan gate eligibility → gak akan pernah 100%.

### Pitfall 4: SaveChanges internal di service vs transaksi caller
**What goes wrong:** `EnsureAsync`, `AuditLogService.LogAsync`, dan bootstrap helper masing-masing `SaveChangesAsync()` internal. Kalau orchestrator TIDAK membungkus dengan `BeginTransactionAsync`, kegagalan di tengah meninggalkan partial state (mis. assignment baru tanpa coach mapping).
**Why:** `LogAsync` SaveChanges di `:41`; `EnsureAsync` di `:62`; bootstrap di `:1475`/`:1490` [VERIFIED].
**How to avoid:** Bungkus seluruh operasi `BypassSave`/`Confirm`/`CancelPending` dengan `BeginTransactionAsync` (D-09). Karena `_context` scoped sama dishare semua service, SaveChanges internal jadi bagian transaksi dan rollback bersih. **KECUALI** hook GradingService (`MarkPendingReadyIfAnyAsync`) yang jalan di grading flow — jangan buka tx di situ.
**Warning signs:** Bypass setengah jadi setelah error (assignment ada, coach/penanda tidak).

### Pitfall 5: Filtered-unique E15 saat ganti coach
**What goes wrong:** Create `CoachCoacheeMapping` baru tanpa deactivate yang lama → violation `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (SQL 2601/2627).
**Why:** Filtered unique `CoacheeId WHERE IsActive=1` [VERIFIED: ApplicationDbContext.cs:325-328].
**How to avoid:** D-16 — kalau `TargetCoachId` diisi: `mapping lama.IsActive=false` → SaveChanges → create baru. Kalau `null`: jangan sentuh mapping. Catch 2601/2627 untuk pesan ramah (pola `:628-639`).
**Warning signs:** Bypass gagal dengan DbUpdateException saat ganti coach.

### Pitfall 6: Migration #2 melebar 2 tabel — snapshot + IT notify
**What goes wrong:** Apply migration tanpa snapshot DB lokal; atau notify IT cuma sebut 1 tabel.
**Why:** D-04 gabung `PendingProtonBypass` + `ProtonTrackAssignment.Origin` dalam 1 migration.
**How to avoid:** `sqlcmd ... BACKUP DATABASE` sebelum `dotnet ef database update` (SEED_WORKFLOW); catat di `docs/SEED_JOURNAL.md`. IT notify: "migration#2 = tabel `PendingProtonBypass` + kolom `ProtonTrackAssignment.Origin`" (DEV_WORKFLOW — flag data-migration jika ada UPDATE; di sini Origin nullable tanpa backfill = pure schema, tetap notify). Verifikasi `ApplicationDbContextModelSnapshot.cs` ter-update.
**Warning signs:** `dotnet ef migrations add` menghasilkan diff tak terduga (snapshot drift).

### Pitfall 7: Cek-ulang konfirmasi stale (D-11)
**What goes wrong:** HC konfirmasi pending yang sudah basi (worker sudah dipindah manual, exam di-regrade fail, atau assignment source berubah) → eksekusi pindah ganda/salah.
**How to avoid:** Sebelum §5.3: validasi (a) assignment asal worker masih = `SourceProtonTrackId` aktif, (b) `LinkedAssessmentSession.IsPassed==true` + penanda Origin="Exam" untuk source ADA, (c) atomic guard `Status=="Siap"` (D-12). Tolak ramah kalau gagal.
**Warning signs:** Worker pindah 2x; pending "Selesai" padahal exam gagal.

## Code Examples

### Resolve "apakah assignment aktif worker ber-Origin Bypass" (gate exempt D-06)
```csharp
// AssessmentAdminController.cs :1368 — tambah SEBELUM skip cross-year
bool isBypassAssignment = await _context.ProtonTrackAssignments
    .AnyAsync(a => a.CoacheeId == uid && a.ProtonTrackId == protonTrackId
                && a.IsActive && a.Origin == "Bypass");
if (!isRenewal && !isBypassAssignment
    && !await _protonCompletionService.IsPrevYearPassedAsync(uid, trackType, prevTahunKe))
{ gateSkippedPrevYear++; continue; }
// Gate 100% deliverable (:1373-1389) TIDAK diubah — D-05.
```
[Source: pola existing AssessmentAdminController.cs:1364-1389, VERIFIED]

### Blok dobel pending (D-10) — semua mode, di BypassSave
```csharp
bool hasActivePending = await _context.PendingProtonBypasses
    .AnyAsync(p => p.CoacheeId == req.CoacheeId
                && (p.Status == "Menunggu" || p.Status == "Siap"));
if (hasActivePending)
    return Json(new { success = false, message = "Worker sudah punya rencana bypass aktif. Selesaikan/batalkan dulu." });
```

### Hook GradingService (flip + notif, no tx)
```csharp
// di GradeAndCompleteAsync :309 (dan FinalizeEssayGrading, dan RegradeAfterEditAsync flip)
if (session.Category == "Assessment Proton" && isPassed && session.ProtonTrackId.HasValue)
{
    await _protonCompletionService.EnsureAsync(session.UserId, session.ProtonTrackId.Value,
        session.CreatedBy ?? "", "Exam", $"...");
    await _protonBypassService.MarkPendingReadyIfAnyAsync(session.Id); // flip Menunggu→Siap + notif HC
}
```
[Source: GradingService.cs:304-309 VERIFIED — sisipkan setelah EnsureAsync existing]

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Penanda inline di SubmitInterviewResults | `ProtonCompletionService.EnsureAsync` helper bersama | Phase 358 (v25.0) | Bypass reuse helper, jangan duplikat |
| Gate cross-year = warning override (escapeable) | Hard-block + exempt placeholder | Phase 359 (D-05) | Bypass isi exempt, jaga hard-block tetap utuh |
| `CompetencyLevelGranted` (0-5) | Dormant, penanda Lulus murni | A-3 | Form bypass CL-B(a) TANPA input level |

**Deprecated/outdated:**
- `ConfirmProgressionWarning` (escape gate) — di-drop Phase 359 D-05. Jangan referensikan.
- Mengandalkan `AssessmentSession.IsPassed`+cert sebagai sinyal "lulus tahun" — sekarang penanda `ProtonFinalAssessment` yang otoritatif.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Sesi bare CL-B(b) bisa lulus lewat path essay (worker pasang paket ber-essay) → hook `FinalizeEssayGrading` wajib | Pitfall 2 | Jika HC dijamin hanya pasang paket non-essay, hook essay jadi defensive idle (tetap aman dipasang — idempotent). Tidak menghalangi; over-coverage murah. |
| A2 | "Cancel exam aktif S" (D-14/E5) = set Status non-aktif / hapus `UserPackageAssignment` untuk sesi Proton in-progress source-year | Open Q1 | Definisi konkret "cancel" belum di kode — planner/spec harus tetapkan; salah → exam zombie atau data terhapus berlebih |
| A3 | Notif `PROTON_BYPASS_READY` pakai `INotificationService.SendByTemplateAsync` (registered DI `:64`) yang dipakai existing | Pattern 5 | Jika GradingService belum inject INotificationService, perlu tambah ke ctor — verifikasi saat planning |
| A4 | EF CLI 10.0.3 vs package 8.0.0 tidak bikin migration korup (terbukti migration #1 sukses) | Standard Stack | Rendah — migration#1 (Origin) sukses 2026-06-10 dengan toolchain sama |

## Open Questions

1. **Definisi konkret "cancel exam aktif S" (D-14/E5)**
   - What we know: spec minta auto-cancel exam in-progress source-year saat instan, kecuali sesi bare CL-B(b).
   - What's unclear: "cancel" = set `Status`? hapus session? hapus `UserPackageAssignment`? Kode belum punya operasi ini eksplisit.
   - Recommendation: Planner tetapkan = set `Status` ke nilai non-completable (mis. "Dibatalkan"/sejenis yang sudah dikenal sistem) ATAU hapus `UserPackageAssignment` agar worker tak bisa lanjut; pilih yang minimal + reversible. Konfirmasi ke user bila ambigu. Scope query: `Category="Assessment Proton" && ProtonTrackId==source && UserId==worker && Status` belum Completed, EXCLUDE `LinkedAssessmentSessionId` CL-B(b).

2. **Default jadwal/durasi/KKM sesi bare CL-B(b)** (Claude's Discretion)
   - What we know: D-01 sesi tanpa paket; HC pasang paket di Kelola Assessment (step 2).
   - Recommendation: Planner set default minimal sane (`DurationMinutes` dari form atau default 60; `PassPercentage` 70 default proyek `AssessmentSession.cs:30`; `Schedule` = now/+1 hari). Field bisa di-edit HC saat pasang paket. Konsisten D-01.

3. **`GradingService` ctor menambah dependency `ProtonBypassService`** — apakah ada risiko circular DI?
   - What we know: `ProtonBypassService` inject `ProtonCompletionService`+`NotificationService`+`AuditLog`+`Context`. `GradingService` inject `ProtonCompletionService`+`ProtonBypassService`.
   - What's unclear: `ProtonBypassService` TIDAK boleh inject `GradingService` (akan circular).
   - Recommendation: Pastikan dependency satu arah (GradingService → ProtonBypassService, bukan sebaliknya). Hook method tidak butuh GradingService.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/test/run | ✓ | 8.0.418 | — |
| dotnet ef CLI | migration #2 | ✓ | 10.0.3 | — (kompat dgn EF Core 8 package) |
| SQL Server | DB lokal HcPortalDB_Dev | ✓ (asumsi running, dipakai 358/359) | SQLEXPRESS | — |
| sqlcmd | snapshot/restore (SEED_WORKFLOW) | ✓ | 17.0.1000.7 | SSMS BACKUP/RESTORE |
| xUnit (test proj) | unit+integration test | ✓ | HcPortal.Tests.csproj | — |

**Missing dependencies with no fallback:** Tidak ada — toolchain lengkap (terbukti migration#1 + 130 test existing jalan).
**Catatan:** AD lokal: jalankan `Authentication__UseActiveDirectory=false dotnet run` untuk UAT admin/HC (CLAUDE.md / memory). Integration test pakai disposable DB `HcPortalDB_Test_<guid>` (BUKAN HcPortalDB_Dev) → tidak butuh snapshot.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (net8.0) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "Category!=Integration"` |
| Full suite command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (butuh SQLEXPRESS untuk `[Trait("Category","Integration")]`) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PBYP-01 | Migration apply (tabel + kolom Origin) di SQL nyata | integration | `dotnet test --filter "FullyQualifiedName~ProtonBypass"` (fixture MigrateAsync) | ❌ Wave 0 (`ProtonBypassServiceTests.cs`) |
| PBYP-02 | CL-A/B(a)/C instan: deactivate+create+penanda+bootstrap; validasi Δtahun/1-aktif | integration | `dotnet test --filter "...Bypass...InstanMode"` | ❌ Wave 0 |
| PBYP-02 | CL-B(b): pending "Menunggu" + sesi bare + force-approve | integration | `dotnet test --filter "...BypassBb"` | ❌ Wave 0 |
| PBYP-03 | Exam lulus → pending "Siap" + notif (3 hook); re-grade fail → "Menunggu" | integration | `dotnet test --filter "...MarkPendingReady"` | ❌ Wave 0 |
| PBYP-04 | Coach deactivate-lama→create-baru (E15); null=pertahankan | integration | `dotnet test --filter "...BypassCoach"` | ❌ Wave 0 |
| PBYP-05 | Bootstrap pakai unit FORM (bukan mapping) | integration | `dotnet test --filter "...BootstrapUnit"` | ❌ Wave 0 |
| PBYP-06 | Batal pending: belum-kerjakan→hapus exam, lulus→pertahankan | integration | `dotnet test --filter "...CancelPending"` | ❌ Wave 0 |
| PBYP-07 | 6 endpoint Authorize/AntiForgery (smoke) | unit (reflection attr) / manual | `dotnet test --filter "...BypassEndpointAuth"` + UAT | ❌ Wave 0 + UAT |
| Gate exempt (D-06) | Assignment Origin="Bypass" lewat cross-year, gate 100% tetap | integration | extend `ProtonYearGateIntegrationTests` | ⚠️ extend existing |
| Validasi \|Δtahun\|≤1, 1-aktif, alasan wajib | unit murni (no DB) | unit | `dotnet test --filter "...BypassValidation"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "Category!=Integration"` (unit cepat <5s).
- **Per wave merge:** full suite (termasuk Integration real-SQL).
- **Phase gate:** `dotnet build` 0 error + full suite hijau + UAT lokal `:5277` (4 closure mode + pending konfirmasi + batal + re-grade fail) sebelum `/gsd-verify-work`. UAT detail = Phase 361 e2e (PBYP-10), tapi 360 verifikasi backend via endpoint/SQL cross-check.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ProtonBypassServiceTests.cs` — reuse `ProtonCompletionFixture` (disposable real-SQL, `[Trait("Category","Integration")]`), pola `ProtonCompletionServiceTests.cs`. Covers PBYP-02..06.
- [ ] Pure-unit validasi (Δtahun/1-aktif/alasan) — bisa kelas static predikat pola `ProtonYearGateTests` (no DB) untuk feedback cepat.
- [ ] Extend `ProtonYearGateIntegrationTests.cs` — kasus exempt Origin="Bypass".
- Framework sudah ada — tidak perlu install. Fixture `ProtonCompletionFixture` reusable apa adanya [VERIFIED: ProtonCompletionServiceTests.cs:25-61].

## Security Domain

> `security_enforcement` tidak di-set di config.json → diperlakukan enabled. Fitur = internal admin/HC tooling.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Identity existing; `[Authorize(Roles="Admin, HC")]` tiap endpoint |
| V4 Access Control | yes | Role gate `Admin, HC` (pola OverrideSave); bypass = aksi hak tinggi → audit wajib |
| V5 Input Validation | yes | Validasi server-side: alasan wajib, Δtahun≤1, mode valid, status pending valid (jangan percaya form) |
| V6 Cryptography | no | Tidak ada crypto baru |
| CSRF | yes | `[ValidateAntiForgeryToken]` tiap POST (BypassSave/Confirm/CancelPending) |
| Audit/Logging | yes | `_auditLog.LogAsync` tiap operasi (siapa, kapan, alasan) |

### Known Threat Patterns for ASP.NET Core MVC + EF
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Info-leak via `ex.Message` di Json error | Information Disclosure | Pesan ramah generik ke user; `ex` ke logger saja (D6 Phase 334) [VERIFIED pattern :634] |
| CSRF pada POST mutasi state | Tampering | `[ValidateAntiForgeryToken]` |
| Privilege escalation (non-HC trigger bypass) | Elevation of Privilege | `[Authorize(Roles="Admin, HC")]` + server-side re-validate, bukan hanya UI |
| Race double-confirm / double-pending | Tampering | Atomic `ExecuteUpdateAsync` rowsAffected (D-12) + blok dobel pending (D-10) |
| Stale confirm (assignment/exam berubah) | Tampering | Re-check D-11 sebelum eksekusi |
| SQL injection | Tampering | EF Core parameterized (LINQ) — jangan raw SQL string-concat |

## Project Constraints (from CLAUDE.md)

- **Verifikasi lokal WAJIB sebelum commit:** `dotnet build` + `dotnet run` (cek `http://localhost:5277`) + cek DB lokal. UAT pakai `Authentication__UseActiveDirectory=false dotnet run`.
- **Migration SOP:** Jangan ALTER tabel manual — selalu EF migration. Test apply migration di lokal (`dotnet ef database update`). File migration WAJIB ter-commit. Notify IT dengan commit hash + flag migration (migration#2 = `PendingProtonBypass` + `ProtonTrackAssignment.Origin`). [DEV_WORKFLOW.md]
- **Jangan edit kode/DB langsung di Dev/Prod.** Promosi = tanggung jawab Team IT.
- **Seed Workflow:** Snapshot DB lokal (`sqlcmd ... BACKUP DATABASE`) sebelum apply migration / sebelum seed test. Catat di `docs/SEED_JOURNAL.md`. Restore setelah test (sukses atau gagal) → tandai journal `cleaned`. [SEED_WORKFLOW.md]
- **Bahasa:** Respon + dokumentasi developer-facing dalam Bahasa Indonesia (campur istilah teknis EN, mengikuti konvensi proyek).

## Sources

### Primary (HIGH confidence — diverifikasi langsung di kode sesi ini)
- `Services/ProtonCompletionService.cs` — EnsureAsync (:36), RemoveExamOriginAsync (:70), IsPrevYearPassedAsync (:107), ProtonYearGate.IsAllowed (:124)
- `Services/GradingService.cs` — hook GradeAndComplete (:304), RegradeAfterEdit (:485/:531), essay early-return (:193-229)
- `Controllers/AssessmentAdminController.cs` — gate (:1336-1392), FinalizeEssayGrading hook (:3754), SubmitInterviewResults Origin="Interview" (:3865), Backfill (:3904)
- `Controllers/CoachMappingController.cs` — exempt placeholder (:533), assign tx+deactivate+create (:565-646), AutoCreateProgressForAssignment (:1424-1493)
- `Controllers/ProtonDataController.cs` — Override GET (:210), OverrideSave (:1400), `_auditLog` (:84)
- `Data/ApplicationDbContext.cs` — filtered-unique E15 (:325-328), DbSet pattern (:38-43), index config
- `Models/ProtonModels.cs` — ProtonTrackAssignment (:71, perlu +Origin), ProtonFinalAssessment+Origin (:207-226)
- `Models/AssessmentSession.cs` — Category/ProtonTrackId/TahunKe/PassPercentage/RenewsSessionId (:16-166)
- `Services/NotificationService.cs` — _templates (:34), SendAsync (:100), SendByTemplateAsync (:231)
- `Services/AuditLogService.cs` — LogAsync signature (:21)
- `Migrations/20260610014907_AddOriginToProtonFinalAssessment.cs` — pola migration AddColumn+Sql
- `HcPortal.Tests/ProtonCompletionServiceTests.cs` (:25-61 fixture) + `ProtonYearGateIntegrationTests.cs` — pola test
- `HcPortal.csproj`, `Program.cs:54-64` — versi + DI registration
- dotnet/ef/sqlcmd `--version` — toolchain availability

### Primary (spec otoritas — locked)
- `docs/superpowers/specs/2026-06-09-proton-bypass-tahun-design.md` (Diskusi B, §3-§13)
- `docs/superpowers/specs/2026-06-09-proton-completion-logic-design.md` (Diskusi A, dependency)
- `.planning/REQUIREMENTS.md` §PBYP; `.planning/phases/360-bypass-backend-b/360-CONTEXT.md`

### Secondary (MEDIUM)
- `docs/DEV_WORKFLOW.md` (migration SOP) + `docs/SEED_WORKFLOW.md` + `CLAUDE.md`

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua existing, versi diverifikasi `--version` + csproj.
- Architecture/integration points: HIGH — tiap baris (:line) dibaca langsung sesi ini, bukan asumsi training.
- Pitfalls: HIGH — diturunkan dari kode aktual (IsActive filter, essay early-return, unit-from-mapping, SaveChanges internal).
- Open questions: 1 substantif (definisi "cancel exam" D-14) yang murni keputusan spec/planner, bukan gap riset.

**Research date:** 2026-06-10
**Valid until:** 2026-07-10 (codebase internal stabil; valid sampai file integration point berubah signifikan). Re-verify jika 358/359 di-refactor.
</content>
</invoke>
