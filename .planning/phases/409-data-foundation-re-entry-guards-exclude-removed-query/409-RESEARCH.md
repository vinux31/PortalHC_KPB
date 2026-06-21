# Phase 409: Data Foundation + Re-entry Guards + Exclude-Removed Query - Research

**Researched:** 2026-06-21
**Domain:** ASP.NET Core MVC + EF Core 8 (SQL Server) — additive nullable migration, soft-remove invariant, server-side re-entry guards, monitoring-query filtering
**Confidence:** HIGH (spec-driven; all call-sites code-verified in this session at exact file:line)

## Summary

Phase 409 adalah fase fondasi murni: tambah 3 kolom nullable (`RemovedAt/RemovedBy/RemovalReason`) ke `AssessmentSession` lewat migration additif, definisikan invarian tunggal (`soft-removed ⇔ RemovedAt != null`), pasang guard server-side anti-resubmit di 3 titik (`StartExam`, `SubmitExam`, `AssessmentHub.JoinBatch`), dan exclude sesi removed dari surface admin batch-aktif. Tidak ada endpoint mutasi, UI, atau SignalR baru (itu 410/411/412). Karena ini spec-driven dengan codebase yang sudah dipetakan audit 4-agen, riset ini memverifikasi kode aktual di setiap call-site dan menetapkan batas over/under-exclude secara presisi.

Semua call-site terkonfirmasi di sesi ini. Guard insertion point bersih: `StartExam` memuat sesi di `:903-906` (guard masuk tepat setelah authz `:912`, SEBELUM transisi `Upcoming→Open` dan SEBELUM mark-InProgress `:1001`); `SubmitExam` memuat di `:1581-1584` (guard masuk setelah authz `:1592`, SEBELUM gating/grading); `JoinBatch` predikat `AnyAsync` di `AssessmentHub.cs:29-31` (tambah `&& s.RemovedAt == null`, pertahankan silent-skip). Exclude diterapkan ke 4 surface admin: `ManageAssessmentTab_Assessment` (`managementQuery` `:119`), `AssessmentMonitoring` (`query` `:2822`), `AssessmentMonitoringDetail` (`query` `:3328` → menggerakkan semua count termasuk `InProgressCount` `:3439`).

**Batas over-exclude KRITIS (STATE Open Concern (c)):** `UserAssessmentHistory` (`:5262`, per-pekerja `a.UserId == userId`, pass-rate via `ComputeHistoryStats`), `WorkerDataService.GetUnifiedRecords`, dan jalur sertifikat pekerja — JANGAN di-exclude (D-01a). `GetDeleteImpact`/`GetAkhiriSemuaCounts`/`ExportAssessmentResults`/`BulkExportPdf` adalah surface aksi/ekspor (bukan "daftar peserta aktif"); riset merekomendasikan TIDAK menyentuhnya di 409 (di luar §D literal) untuk menjaga blast-radius minimal — flag untuk konfirmasi planner.

**Primary recommendation:** Tambah `.Where(... .RemovedAt == null)` HANYA ke 4 query admin-aktif yang menggerakkan list/count monitoring (`:119`, `:2822`, `:3328`); pasang 3 guard server-side dengan pola block identik existing; uji exclude via pola controller-InMemory nyata (`AssessmentWindowRemovalTests`, NON-tautologis) dan migration-chain via SQLEXPRESS disposable (`FlexibleParticipantAddFixture`). Defer panel "Peserta Dikeluarkan", endpoint mutasi, SignalR, dan test suite penuh ke 410-413.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Exclude `RemovedAt != null` **HANYA di surface admin batch-aggregate** yang terdaftar di spec §D — `AssessmentMonitoring` (`:2815`), `AssessmentMonitoringDetail` (`:3273`, termasuk `InProgressCount`), grouping `ManageAssessmentTab_Assessment` (`:179`, group status & count), serta jalur hasil/grading-list + cert-count + pass-rate **dalam konteks hasil-batch**.
- **D-01a:** **JANGAN** sentuh `/CMP/Records` pekerja / `WorkerDataService.GetUnifiedRecords` / sertifikat pekerja di Phase 409. Sesi soft-removed yang bersertifikat **tetap** jadi record historis utuh & tetap tampil di riwayat pekerja. Blast-radius Phase 409 sengaja minimal (hanya surface admin aktif).
- **D-02:** Saat sesi `RemovedAt != null` mencoba `StartExam`/`SubmitExam` → **ikut konvensi block existing**: `TempData["Error"] = "Anda telah dikeluarkan dari ujian ini."; return RedirectToAction("Assessment");`. **Tidak** ada view/halaman dedicated baru di Phase 409.
- **D-02a:** `SubmitExam` — guard ditempatkan **sebelum grading** (setelah load sesi, dekat `if (assessment == null) return NotFound();`): jawaban yang dikirim setelah penghapusan **di-discard**, redirect + pesan. Pesan locked: **"Anda telah dikeluarkan dari ujian ini."**
- **D-03:** `RemovedBy` = **userId** Admin/HC pelaku (cermin kolom existing `CreatedBy` `string?` `AssessmentSession.cs:95`). `RemovalReason` = **`nvarchar(500)`** (set via Fluent `HasMaxLength(500)` atau `[MaxLength(500)]`). `RemovedAt` = **UTC** (`DateTime?`).
- **D-03a:** 3 kolom **nullable additif tanpa default destruktif** — semua baris existing dapat NULL (data lama tak berubah). Verifikasi `dotnet ef migrations add AddParticipantRemovalColumns` + apply DB lokal + cek sqlcmd kolom hadir. Nama migration locked: **`AddParticipantRemovalColumns`**.
- **D-04:** `AssessmentHub.JoinBatch` (`:21`) saat ini cek `s.Status == "InProgress"` lalu **silent `return`**. Soft-remove **TIDAK mengubah `Status`** → guard WAJIB **eksplisit** tambah `&& s.RemovedAt == null` ke predikat `AnyAsync`, pertahankan pola **silent skip** existing (bukan throw).
- **Carry-forward (LOCKED):** Sumber-kebenaran removed = `RemovedAt != null`. `AssessmentSession` per-peserta; batch = `Title+Category+Schedule.Date`; InProgress = turunan (`StartedAt!=null && CompletedAt==null`, `DeriveUserStatus :2715/:2768`). RBAC/antiforgery endpoint add/remove/restore = scope 410/411. Proton reject = scope 410/411.

### Claude's Discretion

- Penempatan presisi guard line di `StartExam` (rekomendasi: sangat awal, sebelum `justStarted`/StartedAt ditulis, agar sesi removed tak pernah ter-mark InProgress).
- Pilih Fluent API vs Data Annotation untuk `HasMaxLength(500)` — ikut pola dominan `ApplicationDbContext`.
- Bentuk exact query exclude (tambah `.Where(s => s.RemovedAt == null)` ke chain existing vs predikat gabungan) — selama semua surface §D ter-cover.
- Cakupan unit/integration test guard + exclude (minimal: guard block 3 jalur + exclude count/list).

### Deferred Ideas (OUT OF SCOPE)

- Endpoint `AddParticipantsLive`/`RemoveParticipantLive`/`RestoreParticipantLive` + RBAC/antiforgery/Proton-reject — Phase 410/411.
- Panel UI "Peserta Dikeluarkan" + SignalR `participantAdded/Removed/examRemoved` + halaman dedicated force-kick — Phase 412.
- Test+UAT penuh (xUnit integration + Playwright e2e) — Phase 413 (409 cukup unit/integration minimal untuk guard + exclude).
- Exclude di `/CMP/Records` pekerja / `WorkerDataService` — sengaja TIDAK dilakukan (D-01a).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PRMV-03 | Peserta yang telah dihapus tidak dapat melanjutkan atau mensubmit ujian (guard di `StartExam`/`SubmitExam`/`Hub.JoinBatch`) — jawaban setelah penghapusan tidak terhitung. | Insertion points terverifikasi: `StartExam` @ CMPController.cs:906/912 (setelah load+authz, sebelum mark-InProgress `:1001`); `SubmitExam` @ CMPController.cs:1584/1592 (setelah load+authz, sebelum gating `:1596` & grading); `JoinBatch` @ AssessmentHub.cs:29-31 (AnyAsync predicate, silent-skip). Pesan locked D-02. Pola block existing reuse @ CMPController.cs:953-971. |
| PLIV-01 (foundation only) | Peserta soft-remove dikecualikan dari semua daftar & perhitungan aktif (panel "Peserta Dikeluarkan" = Phase 412). | Exclude diterapkan ke 4 query admin-aktif: `managementQuery` @:119, `AssessmentMonitoring.query` @:2822, `AssessmentMonitoringDetail.query` @:3328 (menggerakkan `InProgressCount`/`CompletedCount`/`PassedCount` @:3431-3442). Panel restore + count-UI = 412 (deferred). Migration foundation = kolom `RemovedAt` ini. |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Migration 3 kolom removal | Database / Storage | — | Skema `AssessmentSessions` table; EF migration + Fluent config di `ApplicationDbContext` |
| Definisi invarian `RemovedAt != null` | API / Backend (Model) | — | Property di `Models/AssessmentSession.cs`; tak ada enum/Status baru (Status TIDAK berubah) |
| Guard re-entry StartExam/SubmitExam | API / Backend (CMPController) | — | Server-authoritative; client tak boleh bisa bypass (mirror semua guard exam existing) |
| Guard re-join JoinBatch | API / Backend (SignalR Hub) | — | Hub server-side; predikat DB `AnyAsync` sebelum `AddToGroupAsync` |
| Exclude-removed monitoring | API / Backend (AssessmentAdminController) | — | Filter LINQ di read-path; count/list dihitung server-side, view hanya render |

## Standard Stack

### Core (existing — TIDAK ada paket baru)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | ORM + migration runtime | `[VERIFIED: HcPortal.csproj]` provider produksi (`Program.cs:28 UseSqlServer`) |
| Microsoft.EntityFrameworkCore.Tools | 8.0.0 | `Add-Migration`/`Update-Database` | `[VERIFIED: HcPortal.csproj]` design-time tools |
| Microsoft.EntityFrameworkCore.Design | 8.0.0 | Migration scaffolding | `[VERIFIED: HcPortal.csproj]` |
| Microsoft.AspNetCore.SignalR | (in ASP.NET Core 8 runtime) | `AssessmentHub` real-time | `[VERIFIED: Hubs/AssessmentHub.cs]` existing |
| xunit | 2.9.3 | Test framework | `[VERIFIED: HcPortal.Tests.csproj]` |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | In-memory DB untuk controller unit test | `[VERIFIED: HcPortal.Tests.csproj]` pola `AssessmentWindowRemovalTests` |

**Installation:** Tidak ada. Phase 409 NOL paket baru `[VERIFIED: spec §A + Out-of-Scope "Migration tabel/skema selain 3 kolom removal"]`.

### Tooling Environment (verified this session)
| Item | Value | Note |
|------|-------|------|
| .NET SDK | 8.0.418 | `[VERIFIED: dotnet --version]` |
| EF runtime (project) | 8.0.0 | `[VERIFIED: HcPortal.csproj]` |
| `dotnet ef` global tool | 10.0.3 | `[VERIFIED: dotnet ef --version]` — **mismatch** vs project 8.0.0; lihat Pitfall 5 |
| Local DB | `localhost\SQLEXPRESS`, `HcPortalDB_Dev` | `[VERIFIED: appsettings.Development.json]` Integrated Security |

## Architecture Patterns

### System Architecture Diagram

```
                          PHASE 409 DATA FLOW (read-path guard + exclude)

  WORKER BROWSER                          ADMIN/HC BROWSER
       |                                        |
       | GET StartExam / POST SubmitExam        | GET ManageAssessment / Monitoring / Detail
       v                                        v
  +-------------------------------+      +----------------------------------------+
  | CMPController                 |      | AssessmentAdminController              |
  |  StartExam :901               |      |  ManageAssessmentTab_Assessment :112  |
  |   load session :903           |      |   managementQuery :119  --[+RemovedAt==null]
  |   authz :911                  |      |  AssessmentMonitoring :2815           |
  |   >>> GUARD RemovedAt!=null <<|      |   query :2822          --[+RemovedAt==null]
  |   (redirect "dikeluarkan")    |      |  AssessmentMonitoringDetail :3326     |
  |   mark InProgress :1001       |      |   query :3328          --[+RemovedAt==null]
  |  SubmitExam :1571             |      |     |                                  |
  |   load session :1581          |      |     v drives sessionViewModels :3395  |
  |   authz :1589                 |      |     InProgressCount/CompletedCount    |
  |   >>> GUARD RemovedAt!=null <<|      |     /PassedCount :3431-3442           |
  |   gate :1596 -> GradingService|      +----------------------------------------+
  +-------------------------------+                 |
       |                                            | (panel "Peserta Dikeluarkan"
       | SignalR JoinBatch                          |  = SEPARATE query, Phase 412)
       v                                            |
  +-------------------------------+                 |
  | AssessmentHub.JoinBatch :21   |                 v
  |  AnyAsync(Status=="InProgress"|         +-----------------+
  |   >>> && RemovedAt==null <<<  |         | AssessmentSessions table         |
  |  silent return if !hasSession |         | + RemovedAt (datetime2 null)     |
  +-------------------------------+         | + RemovedBy (nvarchar(max) null) |
                                            | + RemovalReason (nvarchar(500))  |
   BOUNDARY — NOT TOUCHED in 409 (D-01a):   +-----------------+
   UserAssessmentHistory :5262 (per-worker pass-rate)
   WorkerDataService.GetUnifiedRecords (/CMP/Records)
   worker certificate surfaces
```

Trace utama: worker yang sudah di-soft-remove (oleh endpoint 411 nanti) → `RemovedAt` terisi → setiap GET StartExam / POST SubmitExam / SignalR JoinBatch terblok di guard → tak bisa lanjut/submit/re-join. Admin monitoring → 3 query exclude `RemovedAt != null` → sesi removed hilang dari count aktif (tapi DB-nya utuh untuk panel 412 + riwayat pekerja).

### Recommended File Touch Map (409 only)
```
Models/AssessmentSession.cs                # +3 properti (RemovedAt/RemovedBy/RemovalReason) cermin CreatedBy :95
Data/ApplicationDbContext.cs               # +RemovalReason HasMaxLength(500) di Entity<AssessmentSession> block :188-222
Migrations/{TS}_AddParticipantRemovalColumns.cs  # NEW (3 AddColumn, additif nullable)
Migrations/ApplicationDbContextModelSnapshot.cs   # auto-updated oleh scaffolder
Controllers/CMPController.cs               # guard StartExam (~:912) + SubmitExam (~:1592)
Hubs/AssessmentHub.cs                      # JoinBatch AnyAsync predicate (:29-31)
Controllers/AssessmentAdminController.cs   # exclude di managementQuery :119, AssessmentMonitoring.query :2822, AssessmentMonitoringDetail.query :3328
HcPortal.Tests/ParticipantRemovalGuardTests.cs   # NEW (minimal: guard + exclude + migration-chain)
```

### Pattern 1: Additive nullable migration (mirror Phase 372 ShuffleToggles / 352 image cols)
**What:** Tambah kolom nullable tanpa default destruktif; data existing dapat NULL otomatis.
**When to use:** Setiap kolom baru yang opsional untuk baris lama.
**Example (kontras: 372 pakai `nullable: false + defaultValue`; 409 pakai `nullable: true`):**
```csharp
// Source: [VERIFIED: Migrations/20260613095102_AddShuffleTogglesToAssessmentSession.cs]
// Phase 409 menghasilkan (via scaffolder) bentuk SETARA tapi nullable:true:
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<DateTime>(
        name: "RemovedAt", table: "AssessmentSessions", type: "datetime2", nullable: true);
    migrationBuilder.AddColumn<string>(
        name: "RemovedBy", table: "AssessmentSessions", type: "nvarchar(max)", nullable: true);
    migrationBuilder.AddColumn<string>(
        name: "RemovalReason", table: "AssessmentSessions",
        type: "nvarchar(500)", maxLength: 500, nullable: true);  // dari HasMaxLength(500)
}
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(name: "RemovedAt", table: "AssessmentSessions");
    migrationBuilder.DropColumn(name: "RemovedBy", table: "AssessmentSessions");
    migrationBuilder.DropColumn(name: "RemovalReason", table: "AssessmentSessions");
}
```
**Catatan:** `RemovedBy` mengikuti `CreatedBy` (plain `string?`, tanpa HasMaxLength) → `nvarchar(max)`. Acceptable (userId GUID-length); plan boleh opsional kasih `HasMaxLength(450)` agar match index-able key length, tapi spec/D-03 hanya mandat MaxLength untuk `RemovalReason`.

### Pattern 2: Fluent property config (mirror existing TahunKe)
**What:** `RemovalReason` HasMaxLength via Fluent di block `Entity<AssessmentSession>`.
**Example:**
```csharp
// Source: [VERIFIED: Data/ApplicationDbContext.cs:222]  (TahunKe = template eksak)
//   entity.Property(a => a.TahunKe).HasMaxLength(20).IsRequired(false);
// Tambah ke block :188-247 (dekat baris :222):
entity.Property(a => a.RemovalReason).HasMaxLength(500).IsRequired(false);
```
Pola dominan repo = **Fluent** (block `Entity<AssessmentSession>` sudah ada di `:188`). Data Annotation `[MaxLength(500)]` di model juga valid tapi inkonsisten dengan konvensi blok ini — rekomendasi: Fluent.

### Pattern 3: Re-entry guard (mirror block convention StartExam)
**What:** Block sesi removed dengan pola TempData+redirect identik existing.
**Example:**
```csharp
// Source: [VERIFIED: Controllers/CMPController.cs:966-971]  (Abandoned block = template)
//   if (assessment.Status == "Abandoned") {
//       TempData["Error"] = "Ujian Anda sebelumnya telah dibatalkan. Hubungi HC untuk mengulang.";
//       return RedirectToAction("Assessment"); }
// StartExam — sisipkan SETELAH authz (:912), SEBELUM auto-transition Upcoming->Open (:914):
if (assessment.RemovedAt != null)
{
    TempData["Error"] = "Anda telah dikeluarkan dari ujian ini.";
    return RedirectToAction("Assessment");
}
// SubmitExam — sisipkan SETELAH authz (:1592), SEBELUM ShouldGateMissingStart (:1596):
if (assessment.RemovedAt != null)
{
    TempData["Error"] = "Anda telah dikeluarkan dari ujian ini.";
    return RedirectToAction("Assessment");
}
```
**Penempatan StartExam (Discretion → rekomendasi):** taruh segera setelah `if (assessment.UserId != user.Id && !Admin && !HC) return Forbid();` (`:912`). Ini SEBELUM `justStarted`/`StartedAt`/`Status="InProgress"` ditulis (`:973-1004`), sehingga sesi removed TIDAK PERNAH ter-mark InProgress (memenuhi invarian D-04 dari sisi worker). Admin/HC bypass owner-check existing tetap berlaku — tapi guard removed berlaku ke SEMUA pemanggil (Admin yang impersonate/preview pun terblok; acceptable karena sesi memang removed).

### Pattern 4: Exclude-removed di read-path (mirror chain .Where existing)
**What:** Tambah satu klausa `.Where(... .RemovedAt == null)` ke query monitoring.
**Example:**
```csharp
// Source: [VERIFIED: AssessmentAdminController.cs:119-121]
var managementQuery = _context.AssessmentSessions
    .AsNoTracking()
    .Where(a => a.RemovedAt == null)   // <-- Phase 409 PLIV-01 foundation
    .AsQueryable();
// :2822 AssessmentMonitoring.query — sisipkan .Where(a => a.RemovedAt == null) sebelum filter search/category.
// :3328 AssessmentMonitoringDetail.query — tambah && a.RemovedAt == null ke predikat Where existing:
var query = _context.AssessmentSessions
    .Include(a => a.User)
    .Where(a => a.Title == title && a.Category == category
             && a.Schedule.Date == scheduleDate.Date
             && a.RemovedAt == null);   // <-- exclude removed; count InProgressCount :3439 ikut bersih
// JoinBatch — Source: [VERIFIED: Hubs/AssessmentHub.cs:29-31]
var hasSession = await db.AssessmentSessions
    .AnyAsync(s => s.UserId == userId && s.Status == "InProgress" && s.RemovedAt == null);
```
`ManageAssessmentTab_Assessment` grouping `:179` mengkonsumsi `allSessions` (turunan `managementQuery`) — exclude di sumber (`:119`) otomatis meng-cover group status & count. Tak perlu sentuh blok grouping.

### Anti-Patterns to Avoid
- **Over-exclude ke surface pekerja (STATE Open Concern (c) + D-01a):** JANGAN tambah `RemovedAt == null` ke `UserAssessmentHistory` (`:5262`), `WorkerDataService.GetUnifiedRecords`, atau jalur sertifikat. Itu melanggar prinsip "sertifikat utuh & reversibel".
- **Mengandalkan Status untuk deteksi removed:** Soft-remove TIDAK mengubah `Status` (tetap InProgress/Completed). Setiap guard/exclude WAJIB cek `RemovedAt` eksplisit, BUKAN `Status` (akar D-04). `DeriveUserStatus` (`:2768`) tak punya cabang "removed" — dan tak boleh ditambah di 409 (status-derivation tak berubah).
- **Lupa exclude di salah satu dari 3 monitoring query:** Detail (`:3328`) menggerakkan SEMUA count via `sessionViewModels`; List (`:2822`) menggerakkan group aggregate; Tab (`:119`) menggerakkan grouping. Ketiganya independen → harus di-edit ketiganya.
- **Menulis test yang me-replika keputusan (tautologi, 999.12):** Lihat Validation Architecture — gunakan pola controller-InMemory nyata yang menjalankan query asli, bukan menulis ulang predikat lalu meng-assert-nya.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Tambah kolom skema | Manual `ALTER TABLE` SQL | `dotnet ef migrations add` scaffolder | Snapshot + Down() + designer auto-sync; manual SQL putus migration-chain (akan break `MigrateAsync` di test fixture) |
| Soft-delete flag | EF global query filter `HasQueryFilter` | Explicit `.Where(RemovedAt == null)` per surface | Global filter akan SECARA OTOMATIS exclude removed dari SEMUA query termasuk surface pekerja (langgar D-01a) + panel 412 (yang JUSTRU butuh removed). Spec sengaja per-surface. **Jangan pakai global filter.** |
| Block message i18n | String literal acak | Pesan locked verbatim D-02 | "Anda telah dikeluarkan dari ujian ini." sudah locked; konsisten dgn konvensi BI existing |
| RemovedBy column type | Custom user-FK table | Plain `string?` cermin `CreatedBy :95` | Konvensi audit existing AssessmentSession (CreatedBy/CreatedAt/UpdatedAt) sudah pakai string userId |

**Key insight:** Godaan terbesar adalah `HasQueryFilter` (EF global soft-delete filter). Itu SALAH untuk milestone ini karena (a) langgar D-01a (akan sembunyikan removed dari riwayat pekerja & sertifikat), dan (b) panel "Peserta Dikeluarkan" Phase 412 butuh MEMBACA removed (global filter harus di-`IgnoreQueryFilters()` di banyak tempat — lebih rapuh). Per-surface `.Where` eksplisit = kontrol presisi yang dimaui spec §D.

## Runtime State Inventory

> Phase 409 menambah kolom + guard read-path; TIDAK rename/migrate data existing. Inventory minimal, tapi diisi eksplisit:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `AssessmentSessions` di `HcPortalDB_Dev` (lokal) + DB Dev server (IT). 3 kolom baru = NULL untuk semua baris existing (additif). TIDAK ada backfill/data-migration. | Apply migration lokal (`database update`); IT apply ke Dev/Prod saat deploy (migration=TRUE). NOL backfill. |
| Live service config | None — tak ada n8n/Datadog/external. SignalR group naming (`monitor-{batchKey}`/`batch-{batchKey}`) tak berubah. | None. |
| OS-registered state | None — tak ada Task Scheduler/pm2/systemd terkait kolom ini. | None. |
| Secrets/env vars | None baru. `ASPNETCORE_ENVIRONMENT=Development` WAJIB di-set saat `dotnet ef`/`dotnet run` agar provider SqlServer + connstring `HcPortalDB_Dev` dipakai (lihat Pitfall 6). | Set env var saat menjalankan EF/run lokal. |
| Build artifacts | `Migrations/ApplicationDbContextModelSnapshot.cs` ter-update oleh scaffolder; `HcPortal.Tests` re-build akan menjalankan `MigrateAsync` di `FlexibleParticipantAddFixture` (chain harus utuh). | Commit snapshot + migration `.cs` + `.Designer.cs`; jalankan `dotnet test` untuk verifikasi chain. |

## Common Pitfalls

### Pitfall 1: Soft-remove tidak mengubah Status (akar D-04)
**What goes wrong:** Developer berasumsi sesi removed bisa dideteksi via `Status`. JoinBatch/SaveTextAnswer/SaveMultipleAnswer semua memfilter `Status == "InProgress"` — sesi InProgress yang di-soft-remove TETAP `Status == "InProgress"`, lolos filter.
**Why it happens:** Spec §B2 melarang sentuh Score/IsPassed/Status saat soft-remove.
**How to avoid:** Setiap guard cek `RemovedAt` eksplisit. JoinBatch (`:29-31`) WAJIB tambah `&& s.RemovedAt == null`.
**Warning signs:** Test "removed worker JoinBatch" lolos join.

### Pitfall 2: Answer-write paths SignalR (SaveTextAnswer/SaveMultipleAnswer) tidak di-guard
**What goes wrong:** `AssessmentHub.SaveTextAnswer` (`:134-194`) dan `SaveMultipleAnswer` (`:200-264`) memvalidasi `Status == "InProgress"` TANPA cek `RemovedAt`. Worker InProgress yang di-soft-remove masih bisa menulis jawaban via Hub langsung (bukan via SubmitExam).
**Why it happens:** Spec §E hanya menyebut `JoinBatch` eksplisit (3 guard: StartExam/SubmitExam/JoinBatch). Tapi PRMV-03 berbunyi "jawaban setelah penghapusan tidak terhitung".
**How to avoid:** **FLAG UNTUK PLANNER (Assumption A1):** Riset merekomendasikan menambah `&& s.RemovedAt == null` juga ke predikat session-load di `SaveTextAnswer` (`:143-144`) dan `SaveMultipleAnswer` (`:209-210`) — kalau worker tak bisa JoinBatch tapi koneksi SignalR existing masih hidup, dia masih bisa invoke Save* langsung. JoinBatch guard saja TIDAK cukup memutus koneksi yang sudah join. Spec literal = 3 guard; PRMV-03 spirit = jawaban tak terhitung. Karena `SubmitExam` guard men-discard grading, dampak praktis kecil (jawaban tersimpan tapi tak ter-grade), tapi defense-in-depth menyarankan guard Save* juga. **Keputusan IN/OUT scope = planner/discuss.**
**Warning signs:** Worker removed mid-exam masih bisa `SaveTextAnswer` (jawaban masuk DB walau tak ter-grade).

### Pitfall 3: Over-exclude ke surface pekerja (STATE Open Concern (c))
**What goes wrong:** Menambah `RemovedAt == null` ke `UserAssessmentHistory` (`:5262`) / `GetUnifiedRecords` / sertifikat → sesi bersertifikat yang di-soft-remove hilang dari riwayat pekerja (langgar "sertifikat utuh & reversibel").
**Why it happens:** `:5262` mengandung `PassedCount`/`PassRate` (terlihat seperti "pass-rate" yang disebut §D), tapi konteksnya per-PEKERJA (`a.UserId == userId`), bukan hasil-BATCH.
**How to avoid:** Exclude HANYA di 3 query batch-admin (`:119`, `:2822`, `:3328`). `:5262` = boundary line, JANGAN sentuh.
**Warning signs:** Test riwayat pekerja menunjukkan sesi removed hilang.

### Pitfall 4: Filtered unique index NomorSertifikat + QUOTED_IDENTIFIER (test insert)
**What goes wrong:** `IX_AssessmentSessions_NomorSertifikat_Unique` adalah filtered index (`HasFilter("[NomorSertifikat] IS NOT NULL")`, `ApplicationDbContext.cs:226-229`). Insert ke tabel dengan filtered index via SQL mentah butuh `SET QUOTED_IDENTIFIER ON`. (Lesson Phase 397: seed via raw insert ke tabel ber-filtered-index gagal tanpa QUOTED_IDENTIFIER ON.)
**Why it happens:** EF Core mengirim `SET QUOTED_IDENTIFIER ON` otomatis, jadi insert via `ctx.AssessmentSessions.Add()` AMAN. Masalah hanya muncul kalau test seed pakai `ExecuteSqlRaw` / sqlcmd manual.
**How to avoid:** Seed test sesi via EF (`ctx.AssessmentSessions.Add`), bukan raw SQL (pola `FlexibleParticipantAddTests.SeedSiblingSessionAsync :71`). Migration `AddColumn` tak menyentuh index ini → aman.
**Warning signs:** `INSERT failed because the following SET options have incorrect settings: 'QUOTED_IDENTIFIER'`.

### Pitfall 5: `dotnet ef` global tool versi 10.0.3 vs project EF 8.0.0
**What goes wrong:** Global tool 10.x bisa memunculkan warning ("tools version newer than runtime") atau, lebih buruk, scaffold snapshot dengan annotation EF10 yang tak kompatibel runtime EF8.
**Why it happens:** `dotnet ef --version` = 10.0.3 (verified), project = 8.0.0.
**How to avoid:** Jalankan via `dotnet ef migrations add` (resolusi ke tool global) ATAU pin lewat local tool manifest. Setelah scaffold, REVIEW `Migrations/...Designer.cs` + `ApplicationDbContextModelSnapshot.cs` — pastikan `ProductVersion` tetap `8.0.x` (scaffolder pakai design assembly project = 8.0.0, jadi biasanya OK). Bila warning muncul: `dotnet tool update dotnet-ef --version 8.0.0 --global` atau pakai `dotnet ef` via `--project HcPortal.csproj`. Verifikasi build hijau setelah scaffold.
**Warning signs:** Snapshot `ProductVersion = "10.x"`; `dotnet build` error annotation tak dikenal.

### Pitfall 6: Provider/connstring salah (SQLite vs SqlServer) saat EF run
**What goes wrong:** `appsettings.json` (base) berisi `"DefaultConnection": "Data Source=HcPortal.db"` (string SQLite) yang di-pass ke `UseSqlServer` (`Program.cs:28`). Tanpa `ASPNETCORE_ENVIRONMENT=Development`, `dotnet ef`/`dotnet run` memuat base appsettings → SqlServer mencoba parse connstring SQLite → gagal/connect ke server salah.
**Why it happens:** Provider hardcoded SqlServer; connstring environment-dependent.
**How to avoid:** SELALU set `ASPNETCORE_ENVIRONMENT=Development` (loads `appsettings.Development.json` → `HcPortalDB_Dev` di SQLEXPRESS) saat `dotnet ef migrations add`, `dotnet ef database update`, dan `dotnet run`. Verifikasi via sqlcmd setelah update.
**Warning signs:** EF mencoba connect ke server bernama "HcPortal.db" / login failure.

### Pitfall 7: UTC vs WIB konsistensi RemovedAt
**What goes wrong:** Window/time compares di repo memakai `DateTime.UtcNow.AddHours(7)` (WIB) — mis. StartExam window `:953`, `DeriveReadyStatus`. Tapi `CreatedAt`/`CompletedAt`/`StartedAt` disimpan `DateTime.UtcNow` (UTC murni).
**Why it happens:** Konvensi repo: kolom timestamp persist = UTC; perbandingan jadwal/window = WIB (UTC+7).
**How to avoid:** `RemovedAt = DateTime.UtcNow` (UTC), cermin `CompletedAt`/`StartedAt` (`AssessmentSession.cs:45-46` di-set `DateTime.UtcNow` di StartExam `:1002`). D-03 sudah locked UTC. JANGAN pakai `AddHours(7)` untuk RemovedAt. Penulisan RemovedAt = scope 411 (409 hanya kolom + guard baca null/non-null, tak menulis), tapi invarian harus didokumentasikan agar 411 konsisten.
**Warning signs:** Display "dihapus pada" off-by-7-jam di panel 412.

## Code Examples

### Migration add command (Develop Workflow CLAUDE.md)
```bash
# Source: [VERIFIED: appsettings.Development.json + Program.cs:28 + CLAUDE.md Develop Workflow]
export ASPNETCORE_ENVIRONMENT=Development
dotnet ef migrations add AddParticipantRemovalColumns
dotnet ef database update          # apply ke HcPortalDB_Dev (SQLEXPRESS) lokal
# Verifikasi kolom hadir (CLAUDE.md: sqlcmd -C -I):
sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -C -I -E -Q \
  "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssessmentSessions' AND COLUMN_NAME IN ('RemovedAt','RemovedBy','RemovalReason');"
# Expected: RemovedAt datetime2 YES, RemovedBy nvarchar YES (-1/max), RemovalReason nvarchar YES 500
```

### Model addition (cermin CreatedBy :95)
```csharp
// Source: [VERIFIED: Models/AssessmentSession.cs:92-95 audit fields region]
// Tambah di region audit (dekat CreatedBy :95):
// ===== v32.5 Phase 409: Soft-remove participant fields =====
/// <summary>UTC. null = aktif; non-null = soft-removed (sumber kebenaran "removed").</summary>
public DateTime? RemovedAt { get; set; }
/// <summary>userId Admin/HC pelaku (cermin CreatedBy).</summary>
public string? RemovedBy { get; set; }
/// <summary>Alasan opsional dari modal (max 500).</summary>
public string? RemovalReason { get; set; }
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| DELETE 1-peserta hard via `DeleteAssessmentPeserta` (stub mati, grep=0) | Soft-remove via `RemovedAt` + guard | v32.5 (409+) | Sertifikat utuh & reversibel; tombol mati `EditAssessment.cshtml:666` diperbaiki di 411 |
| Deteksi "aktif" via `Status` saja | `Status` + `RemovedAt == null` | 409 | Status tak cukup (soft-remove tak ubah Status) |

**Deprecated/outdated:** Tidak ada deprecation di 409 (murni additif). `DeleteAssessmentPeserta` stub mati tetap mati sampai 411.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `SaveTextAnswer`/`SaveMultipleAnswer` (Hub answer-write paths) JUGA perlu guard `RemovedAt == null` untuk memenuhi PRMV-03 "jawaban tak terhitung", meski spec §E hanya menyebut 3 guard eksplisit. | Pitfall 2 | Worker removed mid-exam dengan koneksi SignalR hidup masih bisa tulis jawaban via Hub langsung. SubmitExam guard men-discard grading, jadi dampak = jawaban tersimpan tak ter-grade (impact rendah). Planner/discuss putuskan IN/OUT scope 409. |
| A2 | `GetDeleteImpact`/`GetAkhiriSemuaCounts`/`ExportAssessmentResults`/`BulkExportPdf` TIDAK di-exclude di 409 (bukan "daftar peserta aktif" dalam arti monitoring; surface aksi/ekspor). | Summary + Boundary | Bila spec §D "hasil/grading list" dimaksud termasuk ekspor, sesi removed muncul di ekspor batch. Mitigasi: kecil (ekspor jarang; removed jarang). Planner konfirmasi cakupan literal §D. |
| A3 | EF scaffolder (tool 10.0.3) menghasilkan snapshot `ProductVersion 8.0.x` (pakai design assembly project), bukan 10.x. | Pitfall 5 | Bila snapshot ter-stamp 10.x → build/runtime annotation mismatch. Mitigasi: review snapshot + pin tool 8.0.0. |

## Open Questions (RESOLVED)

1. **Cakupan exclude literal §D "hasil/grading list + cert-count + pass-rate"** — **RESOLVED: A2 OUT scope.**
   - What we know: §D menyebut "Hasil/grading list, cert count, pass-rate" dalam konteks hasil-batch. 3 query monitoring jelas in-scope. `UserAssessmentHistory :5262` jelas out (per-pekerja, D-01a).
   - What's unclear: Apakah `ExportAssessmentResults`/`BulkExportPdf`/`GetDeleteImpact.certCount` termasuk "cert count hasil-batch"?
   - **RESOLUTION (orchestrator, 2026-06-21):** A2 = **OUT scope 409** — minimal 3 query monitoring saja (D-01a blast-radius minimal). Diimplementasikan Plan 02 Task 3 + dicatat Deferred. Revisit 412/413 bila perlu.

2. **Guard SignalR answer-write (A1)** — **RESOLVED: A1 IN scope.**
   - What we know: Spec §E = 3 guard. PRMV-03 = "jawaban tak terhitung".
   - What's unclear: Apakah guard Save* masuk 409 atau cukup SubmitExam-discard.
   - **RESOLUTION (orchestrator, 2026-06-21):** A1 = **IN scope 409** — guard `&& RemovedAt==null` di `SaveTextAnswer`/`SaveMultipleAnswer` (defense-in-depth PRMV-03 "jawaban tak terhitung"). Diimplementasikan Plan 02 Task 2 section D.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/test/run | ✓ | 8.0.418 | — |
| `dotnet ef` tool | migration scaffold | ✓ | 10.0.3 (mismatch) | pin 8.0.0 bila warning (Pitfall 5) |
| SQL Server (SQLEXPRESS) | local DB + integration test | ✓ (assumed running) | — | start service bila down |
| `HcPortalDB_Dev` | local apply + sqlcmd verify | ✓ | — | `database update` membuat bila absen |
| sqlcmd | kolom verify (CLAUDE.md) | ✓ (assumed, `-C -I`) | — | SSMS query manual |

**Missing dependencies with no fallback:** None terdeteksi (semua tool hadir; SQLEXPRESS diasumsikan jalan per workflow existing milestone sebelumnya).
**Missing dependencies with fallback:** `dotnet ef` 10.0.3 mismatch → pin 8.0.0 bila masalah.

## Validation Architecture

> nyquist_validation = true (config.json) → section disertakan.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (`[VERIFIED: HcPortal.Tests.csproj]`) |
| Config file | none (xunit auto-discovery); 2 pola DB: InMemory + SQLEXPRESS disposable |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "Category!=Integration"` (unit-only, cepat) |
| Full suite command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (termasuk Integration @SQLEXPRESS) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PRMV-03 | StartExam sesi removed → redirect "dikeluarkan", TIDAK mark InProgress | integration (controller-InMemory, real action) | `dotnet test --filter "FullyQualifiedName~ParticipantRemovalGuard"` | ❌ Wave 0 |
| PRMV-03 | SubmitExam sesi removed → redirect, grading di-skip (Score tak berubah) | integration (controller-InMemory) | idem | ❌ Wave 0 |
| PRMV-03 | JoinBatch sesi removed → silent skip (tak masuk group) | unit (predicate) / integration (Hub via real db scope) | idem | ❌ Wave 0 |
| PLIV-01 | `ManageAssessmentTab_Assessment` exclude removed dari ViewBag.ManagementData | integration (controller-InMemory, pola `AssessmentWindowRemovalTests`) | idem | ❌ Wave 0 (pola ada) |
| PLIV-01 | `AssessmentMonitoringDetail` InProgressCount/TotalCount exclude removed | integration (controller-InMemory) | idem | ❌ Wave 0 |
| migration | `AddParticipantRemovalColumns` apply bersih (3 kolom nullable, chain utuh) | integration (`MigrateAsync` @SQLEXPRESS disposable) | `dotnet test --filter "Category=Integration"` | ✅ pola `FlexibleParticipantAddFixture` (chain auto-tervalidasi oleh fixture manapun) |

### Sampling Rate
- **Per task commit:** `dotnet build HcPortal.csproj` (0 error) + unit-only quick run.
- **Per wave merge:** full suite (`dotnet test`) — termasuk Integration @SQLEXPRESS (validasi migration-chain).
- **Phase gate:** full suite green + `dotnet ef database update` lokal sukses + sqlcmd verify 3 kolom hadir, SEBELUM `/gsd-verify-work`. (Test+UAT penuh + Playwright = Phase 413; 409 cukup minimal.)

### Observable behaviors yang HARUS divalidasi (drives VALIDATION.md Dimension 8)
1. **Guard StartExam:** seed sesi `RemovedAt != null` + `StartedAt == null` → panggil `StartExam(id)` → assert (a) hasil `RedirectToActionResult` ke "Assessment", (b) DB `Status != "InProgress"` & `StartedAt == null` (sesi removed TIDAK ter-mark). Observasi nyata, bukan replica predikat.
2. **Guard SubmitExam:** seed sesi `RemovedAt != null` + Score lama → panggil `SubmitExam` → assert redirect + `Score`/`Status` DB tak berubah (grading di-skip).
3. **Guard JoinBatch:** seed sesi `Status=="InProgress"` + `RemovedAt != null` → assert predikat `AnyAsync(... RemovedAt==null)` = false (tidak join group). Karena Hub butuh `Context`/`Groups`, uji predikat via query nyata di disposable DB (bukan mock Hub) atau ekstrak predikat ke helper testable.
4. **Exclude monitoring:** seed 1 sesi aktif + 1 removed (sama batch) → panggil action nyata → assert count/list hanya 1 (aktif). Pola `AssessmentWindowRemovalTests:38-60` (instansiasi controller real, `null!` untuk dep tak terpakai, assert ViewBag).
5. **Boundary non-regression:** assert `UserAssessmentHistory :5262` TETAP menampilkan sesi removed (anti over-exclude).

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ParticipantRemovalGuardTests.cs` — covers PRMV-03 (3 guard) + PLIV-01 (exclude count/list). Pola: InMemory controller-real (exclude) + SQLEXPRESS disposable (migration). NON-tautologis (jalankan action/query asli, bukan replica keputusan — lesson 999.12).
- [ ] Reuse `FlexibleParticipantAddFixture` pattern untuk migration-chain assert (atau biarkan fixture existing memvalidasi chain otomatis saat `MigrateAsync`).
- [ ] Framework install: none — xUnit + InMemory + SqlServer test packages sudah ada.

*(Catatan: full e2e Playwright + 10-scenario integration spec §Testing = Phase 413, BUKAN 409. 409 = minimal guard + exclude.)*

## Security Domain

> security_enforcement absen di config → treat as enabled. Section disertakan.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tak ada perubahan auth; guard pakai identity existing (`GetCurrentUserRoleLevelAsync`) |
| V3 Session Management | yes | Re-entry guard = mencegah penggunaan sesi yang sudah dicabut akses (removed). `RemovedAt` adalah otorisasi-data, di-cek server-side (tak bisa di-spoof client) |
| V4 Access Control | yes | Guard server-authoritative di StartExam/SubmitExam/JoinBatch; owner-check existing (`:911`/`:1589`) dipertahankan. Endpoint mutasi RBAC = 410/411 (bukan 409) |
| V5 Input Validation | yes | `RemovalReason` HasMaxLength(500) (server truncate via EF); penulisan = 411 (409 hanya kolom). Tak ada input user baru di 409 (read-path only) |
| V6 Cryptography | no | Tak ada crypto baru |

### Known Threat Patterns for ASP.NET Core MVC + EF (Phase 409 scope)

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Removed worker re-submits exam via direct POST (bypass UI) | Elevation/Tampering | Server guard di `SubmitExam` (`:1592`), cek `RemovedAt` SEBELUM grading; jawaban discard |
| Removed worker resumes via StartExam reload | Elevation | Guard `StartExam` (`:912`) sebelum mark-InProgress |
| Removed worker re-joins SignalR monitor/batch group | Information disclosure | `JoinBatch` predicate `&& RemovedAt == null` (D-04) |
| Removed worker writes answers via Hub (SaveTextAnswer/SaveMultipleAnswer) bypass | Tampering | **GAP (A1):** Save* belum cek RemovedAt — flag planner (Pitfall 2) |
| Over-exclude bocorkan/sembunyikan sertifikat sah pekerja | Repudiation/Availability | JANGAN exclude di surface pekerja (D-01a); test boundary non-regression |
| SQL injection via RemovalReason | Tampering | EF parameterized (LINQ); penulisan = 411 |

## Sources

### Primary (HIGH confidence — code-verified this session)
- `Models/AssessmentSession.cs:45-46,92-95` — CreatedBy/CompletedAt/StartedAt template untuk kolom baru
- `Data/ApplicationDbContext.cs:188-247` — Entity<AssessmentSession> Fluent block; `:222` TahunKe HasMaxLength template; `:226-229` filtered unique index NomorSertifikat
- `Controllers/CMPController.cs:901-1016` (StartExam load/authz/guards/mark-InProgress) + `:1571-1612` (SubmitExam load/authz/gate)
- `Hubs/AssessmentHub.cs:21-34` (JoinBatch) + `:134-264` (SaveTextAnswer/SaveMultipleAnswer)
- `Controllers/AssessmentAdminController.cs:112-152` (ManageAssessmentTab_Assessment + grouping :179) + `:2768` (DeriveUserStatus) + `:2815-3019` (AssessmentMonitoring) + `:3326-3510` (AssessmentMonitoringDetail + counts :3431-3442) + `:4520-4660` (GetAkhiriSemuaCounts/GetDeleteImpact/ExportAssessmentResults) + `:5037` (BulkExportPdf) + `:5262-5317` (UserAssessmentHistory + ComputeHistoryStats — BOUNDARY)
- `Services/GradingService.cs:215-273` — grading via ExecuteUpdate (protected upstream by SubmitExam guard)
- `Services/WorkerDataService.cs:31-433` — AssessmentSessions queries (worker-facing, NOT touched per D-01a)
- `Migrations/20260613095102_AddShuffleTogglesToAssessmentSession.cs` — additive migration pattern
- `HcPortal.Tests/FlexibleParticipantAddTests.cs` (SQLEXPRESS disposable fixture + 999.12 de-tautology lesson) + `AssessmentWindowRemovalTests.cs:38-60` (InMemory real-controller pattern) + `MonitoringUserStatusTests.cs` (static predicate test)
- `appsettings.Development.json` (HcPortalDB_Dev connstring) + `Program.cs:26-28` (UseSqlServer)
- `HcPortal.csproj` / `HcPortal.Tests.csproj` (EF 8.0.0, xunit 2.9.3, InMemory 8.0.0)
- `dotnet --version` = 8.0.418; `dotnet ef --version` = 10.0.3 (verified terminal)

### Secondary (MEDIUM confidence)
- Design spec `docs/superpowers/specs/2026-06-19-flexible-add-remove-participant-design.md` §A/§D/§E/§H (committed `ccdc78ef`)
- `.planning/REQUIREMENTS.md` (PRMV-03→409, PLIV-01 foundation) + `.planning/STATE.md` (Open Concern (c) over-exclude)

### Tertiary (LOW confidence)
- None — semua klaim diverifikasi di kode atau dikutip spec.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua paket existing, NOL paket baru, versi verified di csproj
- Architecture (call-sites): HIGH — setiap insertion/exclude point dibaca langsung di sesi ini (file:line exact)
- Migration mechanics: HIGH — pola 372 verified + env/connstring/tool versi verified terminal
- Pitfalls: HIGH untuk 1/3/4/6/7 (code-verified); MEDIUM untuk 2 (A1 — scope decision) dan 5 (tool mismatch belum direproduksi)
- Test approach: HIGH — 2 pola test existing verified (InMemory real-controller + SQLEXPRESS disposable)

**Research date:** 2026-06-21
**Valid until:** 2026-07-21 (stable; codebase branch main, low churn). Re-verify call-site line numbers bila `AssessmentAdminController.cs`/`CMPController.cs` di-edit oleh fase lain sebelum 409 di-execute.
