# Phase 409: Data Foundation + Re-entry Guards + Exclude-Removed Query - Pattern Map

**Mapped:** 2026-06-21
**Files analyzed:** 7 (6 modified + 1 new migration + 1 new test) — all in single repo `HcPortal`
**Analogs found:** 7 / 7 (100% — pure brownfield, every touch-point has a code-verified in-repo analog)

> Konvensi: respons UI/pesan = Bahasa Indonesia (CLAUDE.md). Migration=TRUE (3 kolom nullable additif) → wajib `dotnet ef` + apply DB lokal + verify sqlcmd + notify IT (commit hash + flag). Semua excerpt di bawah **code-verified** di sesi pemetaan (file:line eksak).

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Models/AssessmentSession.cs` | model (entity) | persistence/transform | `CreatedBy` :95 / `CompletedAt` :45 / `NomorSertifikat` :79 (nullable audit props) | exact |
| `Data/ApplicationDbContext.cs` | config (Fluent) | persistence | `entity.Property(a => a.TahunKe).HasMaxLength(20).IsRequired(false)` :222 | exact |
| `Migrations/{TS}_AddParticipantRemovalColumns.cs` (NEW) | migration | batch (DDL) | `20260613095102_AddShuffleTogglesToAssessmentSession.cs` (additive AddColumn) | role-match (nullable vs default) |
| `Controllers/CMPController.cs` | controller | request-response | Abandoned block :966-971 / window-close :952-957 (TempData+redirect) | exact |
| `Hubs/AssessmentHub.cs` | hub | event-driven (pub-sub) | `JoinBatch` AnyAsync predicate :29-31 itself | exact |
| `Controllers/AssessmentAdminController.cs` | controller | CRUD (read-path) | existing `.Where` in `managementQuery` :119, `AssessmentMonitoring.query` :2822, `AssessmentMonitoringDetail.query` :3328 | exact |
| `HcPortal.Tests/ParticipantRemovalGuardTests.cs` (NEW) | test | request-response + batch | `AssessmentWindowRemovalTests.cs` (InMemory real-ctrl) + `FlexibleParticipantAddTests.cs` (SQLEXPRESS disposable) | exact |

**Authoritative file list (from CONTEXT §canonical_refs + RESEARCH §Recommended File Touch Map):** the 7 above + `Migrations/ApplicationDbContextModelSnapshot.cs` (auto-updated by scaffolder, do not hand-edit).

---

## Pattern Assignments

### `Models/AssessmentSession.cs` (model, persistence)

**Analog:** same file — audit-field region :92-95 (`CreatedAt`/`UpdatedAt`/`CreatedBy`) + nullable timestamp props (`CompletedAt :45`, `StartedAt :46`, `ExamWindowCloseDate :65`).

**Existing nullable-prop pattern** (`Models/AssessmentSession.cs:44-46, 92-95`):
```csharp
public bool? IsPassed { get; set; }
public DateTime? CompletedAt { get; set; }
public DateTime? StartedAt { get; set; }
// ...
// Audit fields
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime? UpdatedAt { get; set; }
public string? CreatedBy { get; set; }   // <-- string? userId — TEMPLATE EKSAK untuk RemovedBy (D-03)
```

**Code to add** (copy-paste-ready — mirror `CreatedBy` for `RemovedBy`, `CompletedAt` for `RemovedAt`; place in/after audit region near :95):
```csharp
// ===== v32.5 Phase 409: Soft-remove participant fields =====
/// <summary>UTC. null = aktif; non-null = soft-removed (SUMBER KEBENARAN "removed"). Ditulis di Phase 411.</summary>
public DateTime? RemovedAt { get; set; }
/// <summary>userId Admin/HC pelaku (cermin CreatedBy :95).</summary>
public string? RemovedBy { get; set; }
/// <summary>Alasan opsional dari modal (max 500, Fluent HasMaxLength).</summary>
public string? RemovalReason { get; set; }
```
**Notes:** NO `[MaxLength]` data-annotation here — repo convention puts length constraint in Fluent block (see next file). `RemovedAt` = UTC (cermin `CompletedAt`/`StartedAt` set `DateTime.UtcNow`), **NOT** `AddHours(7)` (Pitfall 7). All 3 nullable → existing rows get NULL automatically (additive, non-destructive — D-03a).

---

### `Data/ApplicationDbContext.cs` (config, persistence)

**Analog:** same file — `Entity<AssessmentSession>` Fluent block :188-247, specifically the `HasMaxLength` line at :222.

**Existing Fluent length-config pattern** (`Data/ApplicationDbContext.cs:220-223`):
```csharp
// AssessmentSession: Proton exam fields (Phase 53)
entity.Property(a => a.ProtonTrackId).IsRequired(false);
entity.Property(a => a.TahunKe).HasMaxLength(20).IsRequired(false);   // <-- TEMPLATE EKSAK
entity.Property(a => a.InterviewResultsJson).HasColumnType("NVARCHAR(MAX)").IsRequired(false);
```

**Code to add** (insert inside the `builder.Entity<AssessmentSession>(entity => { ... })` block, near :222-223):
```csharp
// v32.5 Phase 409: RemovalReason nvarchar(500) (D-03). RemovedBy/RemovedAt = no extra config (mirror CreatedBy/CompletedAt).
entity.Property(a => a.RemovalReason).HasMaxLength(500).IsRequired(false);
```
**Discretion resolved (CONTEXT):** Fluent **wins** — block already exists at :188, dominant repo convention. `RemovedBy` left as plain `string?` (no Fluent) → scaffolds `nvarchar(max)`, identical to `CreatedBy` — acceptable per D-03/RESEARCH Pattern 1 note. Do **NOT** touch the filtered unique index :226-229 (`IX_AssessmentSessions_NomorSertifikat_Unique`) — AddColumn doesn't affect it (Pitfall 4).

---

### `Migrations/{TIMESTAMP}_AddParticipantRemovalColumns.cs` (NEW migration, batch DDL)

**Analog:** `Migrations/20260613095102_AddShuffleTogglesToAssessmentSession.cs` (additive `AddColumn` shape + symmetric `Down` `DropColumn`).

**Generate, do not hand-write** (RESEARCH "Don't Hand-Roll" — manual `ALTER TABLE` breaks migration-chain → breaks `MigrateAsync` in test fixture). Migration name **locked**: `AddParticipantRemovalColumns`.
```bash
export ASPNETCORE_ENVIRONMENT=Development   # WAJIB — else SQLite connstring leaks into UseSqlServer (Pitfall 6)
dotnet ef migrations add AddParticipantRemovalColumns
dotnet ef database update                   # apply ke HcPortalDB_Dev (SQLEXPRESS) lokal
# verify (CLAUDE.md sqlcmd -C -I):
sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -C -I -E -Q "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssessmentSessions' AND COLUMN_NAME IN ('RemovedAt','RemovedBy','RemovalReason');"
# Expected: RemovedAt datetime2 YES NULL | RemovedBy nvarchar YES -1(max) | RemovalReason nvarchar YES 500
```

**Expected scaffolder output** (CONTRAST with analog: 372 uses `nullable: false + defaultValue: true`; 409 uses `nullable: true`, NO defaultValue — D-03a). Analog `Up`/`Down` shape (`AddShuffleTogglesToAssessmentSession.cs:11-38`):
```csharp
// ANALOG (Phase 372 — nullable:false, NOT what we want; shows AddColumn/DropColumn shape):
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<bool>(name: "ShuffleOptions", table: "AssessmentSessions",
        type: "bit", nullable: false, defaultValue: true);
    // ...
}
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(name: "ShuffleOptions", table: "AssessmentSessions");
}
```
```csharp
// PHASE 409 expected (nullable:true, additive — verify scaffolder produced this):
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<DateTime>(name: "RemovedAt", table: "AssessmentSessions",
        type: "datetime2", nullable: true);
    migrationBuilder.AddColumn<string>(name: "RemovedBy", table: "AssessmentSessions",
        type: "nvarchar(max)", nullable: true);
    migrationBuilder.AddColumn<string>(name: "RemovalReason", table: "AssessmentSessions",
        type: "nvarchar(500)", maxLength: 500, nullable: true);   // from HasMaxLength(500)
}
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(name: "RemovedAt", table: "AssessmentSessions");
    migrationBuilder.DropColumn(name: "RemovedBy", table: "AssessmentSessions");
    migrationBuilder.DropColumn(name: "RemovalReason", table: "AssessmentSessions");
}
```
**Post-scaffold check (Pitfall 5):** REVIEW `Migrations/ApplicationDbContextModelSnapshot.cs` + the `.Designer.cs` → confirm `ProductVersion` stays `8.0.x` (global tool is 10.0.3; design assembly is 8.0.0 → usually OK). Commit migration `.cs` + `.Designer.cs` + updated snapshot together. `dotnet build` must be green.

---

### `Controllers/CMPController.cs` (controller, request-response) — 2 guards

**Analog:** same file — the block-convention guards in `StartExam` (window-close :952-957, durasi-0 :959-964, Abandoned :966-971) and the terminal-status guard in `SubmitExam` :1605-1612.

**Existing block pattern** (`Controllers/CMPController.cs:966-971` — Abandoned = TEMPLATE EKSAK):
```csharp
// Block re-entry of Abandoned sessions — worker must contact HC for Reset (LIFE-02)
if (assessment.Status == "Abandoned")
{
    TempData["Error"] = "Ujian Anda sebelumnya telah dibatalkan. Hubungi HC untuk mengulang.";
    return RedirectToAction("Assessment");
}
```

**Guard A — `StartExam`** (insert at ~:912, immediately AFTER owner-check `if (assessment.UserId != user.Id && !Admin && !HC) return Forbid();` and BEFORE Upcoming→Open auto-transition :914 and BEFORE mark-InProgress :999-1004). Placement rationale (Discretion → resolved): earliest possible so a removed session is **never** marked InProgress.
```csharp
// v32.5 Phase 409 (PRMV-03 / D-02): sesi soft-removed tak boleh lanjut ujian. Guard SEBELUM mark-InProgress.
if (assessment.RemovedAt != null)
{
    TempData["Error"] = "Anda telah dikeluarkan dari ujian ini.";
    return RedirectToAction("Assessment");
}
```

**Guard B — `SubmitExam`** (insert at ~:1592, AFTER owner-check :1589-1592 and BEFORE `ShouldGateMissingStart` :1596 / grading). Jawaban setelah penghapusan **di-discard** (D-02a).
```csharp
// v32.5 Phase 409 (PRMV-03 / D-02a): discard submit dari sesi soft-removed SEBELUM grading.
if (assessment.RemovedAt != null)
{
    TempData["Error"] = "Anda telah dikeluarkan dari ujian ini.";
    return RedirectToAction("Assessment");
}
```
**Locked:** message verbatim `"Anda telah dikeluarkan dari ujian ini."` (both sites). `assessment` is already loaded via `FirstOrDefaultAsync` at :903-904 (StartExam) / :1581-1582 (SubmitExam) and null-checked — `RemovedAt` is available on the entity, no extra query. Guard applies to ALL callers incl. Admin/HC impersonation preview (acceptable — session genuinely removed).

---

### `Hubs/AssessmentHub.cs` (hub, event-driven) — JoinBatch predicate

**Analog:** the `JoinBatch` `AnyAsync` predicate itself (`Hubs/AssessmentHub.cs:29-31`).

**Existing predicate + silent-skip** (`Hubs/AssessmentHub.cs:29-31`):
```csharp
var hasSession = await db.AssessmentSessions
    .AnyAsync(s => s.UserId == userId && s.Status == "InProgress");
if (!hasSession) return;   // <-- silent skip — PRESERVE this (do NOT throw)
```

**Code to change** (add one term to the predicate — D-04; soft-remove does NOT mutate `Status`, so the guard MUST be explicit on `RemovedAt`, cannot rely on `Status`):
```csharp
var hasSession = await db.AssessmentSessions
    .AnyAsync(s => s.UserId == userId && s.Status == "InProgress" && s.RemovedAt == null);
if (!hasSession) return;
```
**Keep silent-return behavior** (no throw) — mirrors the 4 other `return;` early-exits in this hub method family (:24, :31, :45, :51, :55).

**FLAG FOR PLANNER (Assumption A1 — IN/OUT scope decision):** RESEARCH Pitfall 2 recommends the SAME `&& s.RemovedAt == null` term on the session-load predicates of `SaveTextAnswer` (~:143-144) and `SaveMultipleAnswer` (~:209-210) as defense-in-depth (a worker removed mid-exam with a live SignalR connection can still invoke `Save*` directly — JoinBatch guard does not eject an already-joined connection). Spec §E literal = 3 guards only; PRMV-03 spirit = "jawaban tak terhitung". Practical impact low (SubmitExam guard discards grading). **Planner: decide IN (cheap 1-line each, closes PRMV-03 gap) or OUT (carry to 412 force-kick) and record.**

---

### `Controllers/AssessmentAdminController.cs` (controller, CRUD read-path) — 3 exclude sites

**Analog:** existing `.Where` chains in the three same queries. D-01 scope: exclude **ONLY** these 3 admin batch-aggregate surfaces. Insert one term; the consuming grouping/count code (:179, :3431-3442) inherits the exclusion automatically — no need to touch grouping/count blocks.

**Site 1 — `managementQuery`** (`ManageAssessmentTab_Assessment`, `:119-121`):
```csharp
// EXISTING:
var managementQuery = _context.AssessmentSessions
    .AsNoTracking()
    .AsQueryable();
// CHANGE TO (add .Where after .AsNoTracking()):
var managementQuery = _context.AssessmentSessions
    .AsNoTracking()
    .Where(a => a.RemovedAt == null)   // v32.5 Phase 409 PLIV-01 — exclude soft-removed
    .AsQueryable();
```
> Covers grouping `:179` (`standardGrouped` consumes `allSessions` ← `managementQuery`) AND pre/post grouping `:157` — both inherit. No edit to grouping blocks.

**Site 2 — `AssessmentMonitoring.query`** (`:2822-2824`):
```csharp
// EXISTING:
var query = _context.AssessmentSessions
    .AsNoTracking()
    .AsQueryable();
// CHANGE TO:
var query = _context.AssessmentSessions
    .AsNoTracking()
    .Where(a => a.RemovedAt == null)   // v32.5 Phase 409 PLIV-01
    .AsQueryable();
```
> Covers list aggregate groups (`prePostGroups` :2867 / `standardSessions` :2864) inherit via `allSessions`.

**Site 3 — `AssessmentMonitoringDetail.query`** (`:3328-3332`) — drives ALL counts incl. `InProgressCount :3439`:
```csharp
// EXISTING:
var query = _context.AssessmentSessions
    .Include(a => a.User)
    .Where(a => a.Title == title
             && a.Category == category
             && a.Schedule.Date == scheduleDate.Date);
// CHANGE TO (add the && term inside the existing predicate):
var query = _context.AssessmentSessions
    .Include(a => a.User)
    .Where(a => a.Title == title
             && a.Category == category
             && a.Schedule.Date == scheduleDate.Date
             && a.RemovedAt == null);   // v32.5 Phase 409 PLIV-01 — InProgressCount :3439 + TotalCount :3431 ikut bersih
```
> `sessionViewModels :3395` ← `sessions` ← this query → `TotalCount/CompletedCount/PassedCount/PendingCount/InProgressCount/AbandonedCount/MenungguPenilaianCount` (:3431-3442) ALL inherit. No edit to count block.

**All 3 are independent — must edit all three** (RESEARCH Anti-Pattern: "lupa exclude di salah satu dari 3 monitoring query").

---

## Shared Patterns

### Soft-remove invariant (single source of truth)
**Source:** new `RemovedAt` prop. **Apply to:** every guard + every exclude.
```
soft-removed  ⇔  RemovedAt != null
aktif         ⇔  RemovedAt == null
```
NEVER detect "removed" via `Status` — soft-remove does NOT mutate `Status`/`Score`/`IsPassed` (spec §B2). Every check is explicit `RemovedAt` (root cause of D-04). `DeriveUserStatus` (:2715/:2768) gains NO "removed" branch in 409.

### Block-guard convention (TempData + redirect)
**Source:** `CMPController.cs:966-971` (Abandoned). **Apply to:** both CMPController guards.
```csharp
TempData["Error"] = "<pesan BI>";
return RedirectToAction("Assessment");
```
Phase 409 message locked: `"Anda telah dikeluarkan dari ujian ini."`

### Per-surface `.Where`, NOT global query filter
**Source:** existing `.Where` chains :119/:2822/:3328. **Apply to:** all 3 exclude sites.
EF `HasQueryFilter` is **FORBIDDEN** (RESEARCH "Don't Hand-Roll" key insight): it would auto-hide removed from worker records + certs (violates D-01a) AND from the Phase-412 "Peserta Dikeluarkan" panel (which MUST read removed). Per-surface explicit `.Where` = the precise control spec §D wants.

### Test pattern A — InMemory real-controller (NON-tautological)
**Source:** `AssessmentWindowRemovalTests.cs:38-74` (factory) + `:100-172` (fact). **Apply to:** exclude + StartExam/SubmitExam guard tests.
- `UseInMemoryDatabase(Guid.NewGuid())` per run; `null!` for unused ctor deps; `NullLogger<...>.Instance` for logger that IS called.
- **MUST seed matching `ApplicationUser` rows** — EF InMemory does in-memory join on `a.User` projection and silently drops rows with absent FK (`MakeUser` helper :77-86).
- Read results via `ctrl.ViewData["ManagementData"]` (NOT `ViewBag as T` → silent null); anonymous types are internal → read props via reflection (`GetTitle` :158-159).

### Test pattern B — SQLEXPRESS disposable fixture (migration-chain + real schema)
**Source:** `FlexibleParticipantAddTests.cs:20-53` (fixture) + `:62-118` (seed helpers + decision-replica helpers). **Apply to:** migration-chain assert + any test needing real `RemovedAt` column / filtered index.
- `IAsyncLifetime` fixture: disposable DB `HcPortalDB_Test_{guid:N}` on `localhost\SQLEXPRESS`, `MigrateAsync()` in `InitializeAsync`, `EnsureDeletedAsync()` in `DisposeAsync`. `[Trait("Category","Integration")]`.
- Migration-chain is auto-validated by ANY fixture's `MigrateAsync` — if `AddParticipantRemovalColumns` breaks the chain, fixture throws `XunitException` "MIGRATION-CHAIN break".
- Seed sessions via EF `ctx.AssessmentSessions.Add()` (NOT raw SQL) → EF auto-sends `SET QUOTED_IDENTIFIER ON`, safe with filtered index (Pitfall 4).

### De-tautology rule (backlog 999.12) — MANDATORY for new test
**Source:** `FlexibleParticipantAddTests.cs:122-162` (the "BEFORE seed wrong-value → DECISION → AFTER assert transformation" structure).
Do **NOT** rewrite the exclude/guard predicate in the test then assert it. Instead run the REAL action/query and observe behavior:
1. **Guard StartExam:** seed `RemovedAt != null` + `StartedAt == null` → call `StartExam(id)` → assert `RedirectToActionResult` to "Assessment" AND DB `Status != "InProgress"` & `StartedAt == null` (session NEVER marked).
2. **Guard SubmitExam:** seed `RemovedAt != null` + old `Score` → call `SubmitExam` → assert redirect AND `Score`/`Status` unchanged (grading skipped).
3. **Guard JoinBatch:** seed `Status=="InProgress"` + `RemovedAt != null` → assert `AnyAsync(... RemovedAt==null)` = false via REAL query on disposable DB (Hub needs `Context`/`Groups` → test the predicate via real db query, not a mocked Hub).
4. **Exclude monitoring:** seed 1 active + 1 removed (same batch) → call real action → assert count/list == 1.
5. **Boundary non-regression:** assert `UserAssessmentHistory :5262` STILL shows the removed session (anti over-exclude).

---

## No Analog Found

None. All 7 touch-points have a code-verified in-repo analog. (This is pure brownfield additive work.)

---

## Boundary — Files That Look Like Analogs But Must NOT Be Touched (D-01a / Pitfall 3)

| File / method | Why excluded from 409 |
|---------------|----------------------|
| `AssessmentAdminController.UserAssessmentHistory` (:5262, `ComputeHistoryStats`) | per-WORKER `a.UserId == userId` (not batch); has `PassedCount`/`PassRate` but worker-context → certs must stay visible. **JANGAN exclude.** |
| `Services/WorkerDataService.GetUnifiedRecords` (`/CMP/Records`) | worker-facing history; "sertifikat utuh & reversibel". **JANGAN exclude.** |
| Worker certificate surfaces | soft-removed-with-cert = intact historical record. **JANGAN exclude.** |
| `GetDeleteImpact` / `GetAkhiriSemuaCounts` / `ExportAssessmentResults` / `BulkExportPdf` | action/export surfaces, not "daftar peserta aktif" monitoring. RESEARCH A2 recommends OUT of 409 (minimal blast-radius). **Planner: confirm literal §D scope (Open Question 1) — default = minimal, 3 query saja.** |
| `DeriveUserStatus` (:2715/:2768) | status-derivation unchanged in 409 (no "removed" branch). |

---

## Metadata

**Analog search scope:** `Models/`, `Data/`, `Controllers/`, `Hubs/`, `Migrations/`, `HcPortal.Tests/`
**Files scanned (read this session):** `AssessmentSession.cs`, `ApplicationDbContext.cs`, `AssessmentHub.cs`, `CMPController.cs`, `AssessmentAdminController.cs`, `20260613095102_AddShuffleTogglesToAssessmentSession.cs`, `AssessmentWindowRemovalTests.cs`, `FlexibleParticipantAddTests.cs`
**Line numbers code-verified:** 2026-06-21 (re-verify if `CMPController.cs`/`AssessmentAdminController.cs` edited by another phase before 409 executes — RESEARCH "Valid until")
**Pattern extraction date:** 2026-06-21
