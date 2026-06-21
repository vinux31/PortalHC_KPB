# Phase 404: Test (SQL Riil) + UAT + Docs + Invariants - Pattern Map

**Mapped:** 2026-06-21
**Files analyzed:** 7 (4 test artifacts + 1 modified stub + 2 docs)
**Analogs found:** 7 / 7 (all exact or role-match)

> **Boundary reminder (R-1 research):** Phase 404 is **NOL kode produksi**. Every "create" below is a **test artifact** or **doc**. Do NOT extract seams from controllers (`CoachCoacheeMappingAssign`/`Edit`/`Reactivate`, `ImportCoachCoacheeMapping`) — that is a production change and re-triggers code-review+secure. Drive write-paths via (a) direct DB constraint assert, (b) DbContext write-pattern replication (pola `AbandonGuardTests`), or (c) existing static helpers/services (`ValidateAssignmentUnitInUserUnits`, `ProtonDeliverableBootstrap`, `ProtonBypassService`).

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `HcPortal.Tests/MultiUnitSqlFixture.cs` (new) | test-fixture | batch (migrate+seed) | `OrgLabelMigrationFixture` (`OrgLabelMigrationIntegrationTests.cs:24-66`) | exact |
| `HcPortal.Tests/MultiUnitInvariantSqlTests.cs` (new) | test | CRUD / event-driven (constraint assert) | `ProtonBypassServiceTests.cs` + `AbandonGuardTests.cs` + `AssignmentUnitInUserUnitsTests.cs` | role-match (composite) |
| `HcPortal.Tests/CrossUnitAssignTests.cs:105` (modify stub) | test | request-response | self (existing InMemory CXU tests) + `ProtonBypassServiceTests.cs` | exact (in-file) |
| `HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs:72-91` (modify 3 stubs) | test | batch | self (`UserUnitsBackfillFixture` already present) | exact (in-file) |
| `docs/milestone-v32.3/index.html` (new) | doc (HTML handoff) | static | `docs/milestone-v31.0/index.html` | exact |
| `docs/` markdown D1=b note (new) | doc (markdown) | static | (inline section in HTML, or new `.md`) | role-match |
| `docs/SEED_JOURNAL.md` (+1 entry) | doc (audit-trail row) | static | existing rows 402-03 / 400-01 / 399 (`SEED_JOURNAL.md:9-13`) | exact |

---

## Pattern Assignments

### `HcPortal.Tests/MultiUnitSqlFixture.cs` (test-fixture, migrate+seed)

**Analog:** `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs:24-66` (`OrgLabelMigrationFixture`) — identical shape to 6 existing fixtures (`AbandonGuardFixture`, `ProtonCompletionFixture`, `UserUnitsBackfillFixture`, `ImageCleanupFixture`, `EssayFinalizeRecomputeFixture`).

**Connection-string + DB-name-per-guid pattern** (copy verbatim, `OrgLabelMigrationIntegrationTests.cs:26-37`):
```csharp
public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
private readonly string _cs;
public DbContextOptions<ApplicationDbContext> Options { get; private set; } = null!;

public MultiUnitSqlFixture()
{
    // localhost-only + Integrated Security (no secrets, no env vars).
    // TrustServerCertificate=True — SQLEXPRESS self-signed cert.
    _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
}
```

**`MigrateAsync()` + catch→`EnsureDeletedAsync` + seed-hook pattern** (copy structure, `OrgLabelMigrationIntegrationTests.cs:39-58`). CHANGE: call new `SeedCanonicalAsync(ctx)` instead of `SeedData.SeedOrganizationLevelLabelsAsync`. Keep `MigrateAsync()` (D-03 — proves migration 399 `AddUserUnits` applies; Pitfall 2: never `EnsureCreated`):
```csharp
public async Task InitializeAsync()
{
    Options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
    try
    {
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.MigrateAsync();   // D-03: full chain incl 399 AddUserUnits + filtered-unique IX
        await SeedCanonicalAsync(ctx);        // D-02: {X,Y} 1 Bagian + coach cross-unit + PROTON T1@X→T2@Y
    }
    catch (Exception ex)
    {
        try { await using var c = new ApplicationDbContext(Options); await c.Database.EnsureDeletedAsync(); } catch { }
        throw new Xunit.Sdk.XunitException(
            $"Phase 404 MultiUnit fixture setup gagal saat MigrateAsync/seed DB {DbName}. " +
            $"Indikasi MIGRATION-CHAIN break (full chain run, incl 399), BUKAN tentu bug multi-unit. Inner: {ex}");
    }
}

public async Task DisposeAsync()
{
    await using var ctx = new ApplicationDbContext(Options);
    await ctx.Database.EnsureDeletedAsync();   // drop disposable DB on success
}
```

**Seed-chain helper pattern for `SeedCanonicalAsync`** — copy the Kompetensi→Sub→Deliverable chain from `ProtonBypassServiceTests.cs:52-66` (`SeedDeliverablesAsync`), and the `ProtonTrack` lookup-by-type from `:39-40` (`TrackIdAsync`). Master `ProtonTracks` rows arrive **free** from the migration `InsertData` (`Migrations/20260223060707_CreateProtonTrackTable.cs`), so query them, don't insert:
```csharp
// ProtonTrack lookup (master seeded by migration — query, don't insert):
private static async Task<int> TrackIdAsync(ApplicationDbContext ctx, string trackType, string tahunKe)
    => (await ctx.ProtonTracks.FirstAsync(t => t.TrackType == trackType && t.TahunKe == tahunKe)).Id;

// Kompetensi(unit)→Sub→N Deliverable chain (per unit X and Y):
var komp = new ProtonKompetensi { Bagian = "Bagian-B", Unit = unitX, NamaKompetensi = "...", Urutan = 1, ProtonTrackId = trackId };
ctx.ProtonKompetensiList.Add(komp); await ctx.SaveChangesAsync();
var sub = new ProtonSubKompetensi { ProtonKompetensiId = komp.Id, NamaSubKompetensi = "Sub", Urutan = 1 };
ctx.ProtonSubKompetensiList.Add(sub); await ctx.SaveChangesAsync();
// AddRange ProtonDeliverable...
```

**Seed `UserUnit` junction pattern** (from `AssignmentUnitInUserUnitsTests.cs:24-28` + `CrossUnitAssignTests.cs:62-63`). Coachee = {X(primary), Y} in 1 Bagian; coach = cross-unit X & Y. **Pitfall 3:** `ApplicationUser` has NO `.UserUnits` nav — insert into `ctx.UserUnits` directly:
```csharp
ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = unitX, IsPrimary = true,  IsActive = true });
ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = unitY, IsPrimary = false, IsActive = true });
```

**What to copy:** connstr const, DbName-per-guid, `MigrateAsync` + catch-cleanup + `XunitException`, `DisposeAsync`/`EnsureDeletedAsync`, `IAsyncLifetime`, the seed-chain helpers.
**What to change:** seed body → canonical {X,Y}/coach/PROTON dataset; expose `Options` as public `{ get; }` (research §Pattern 1 uses public — `OrgLabelMigrationFixture` uses `=> _options`, both fine).
**Discretion note (Open Q2):** with one shared fixture across N test classes, xUnit instantiates the fixture (→ `MigrateAsync`, expensive) **once per class**. Prefer 1 invariant test class to run the ~94-migration chain once; split only if >8 Facts harm readability.

---

### `HcPortal.Tests/MultiUnitInvariantSqlTests.cs` (test, constraint assert — QA-03/QA-04)

**Analogs (composite):** `ProtonBypassServiceTests.cs` (drive bypass + count assert), `AbandonGuardTests.cs:88-95` (DbContext write-pattern replication), `AssignmentUnitInUserUnitsTests.cs` (helper assert). Wire `[Trait("Category","Integration")]` + `IClassFixture<MultiUnitSqlFixture>` exactly like `ProtonBypassServiceTests.cs:17-25`.

**Class wiring** (copy from `ProtonBypassServiceTests.cs:17-25`):
```csharp
[Trait("Category", "Integration")]
public class MultiUnitInvariantSqlTests : IClassFixture<MultiUnitSqlFixture>
{
    private readonly MultiUnitSqlFixture _fixture;
    public MultiUnitInvariantSqlTests(MultiUnitSqlFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);
```

**QA-03 single-active MAPPING — assert via `DbUpdateException`** (DB-enforced filtered-unique `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`, `ApplicationDbContext.cs:333-336`). This is **Pattern 2** — covers Assign/Edit/Import/Reactivate at once since all hit the SAME index (R-2). Research §Pattern 2:
```csharp
[Fact]
public async Task SecondActiveMapping_SameCoachee_ViolatesFilteredUnique()
{
    await using var ctx = NewCtx();
    var coachee = $"cee-{Guid.NewGuid():N}";
    ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoachId="co1", CoacheeId=coachee, IsActive=true, StartDate=DateTime.UtcNow, AssignmentUnit="UnitX" });
    await ctx.SaveChangesAsync();
    ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoachId="co2", CoacheeId=coachee, IsActive=true, StartDate=DateTime.UtcNow, AssignmentUnit="UnitY" });
    await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());  // 2601/2627; InMemory would NOT throw
}
```

**QA-03 single-active PTA — assert via COUNT, NOT exception** (Pitfall 1: `ProtonTrackAssignment` has only NON-unique `(CoacheeId,IsActive)` index `ApplicationDbContext.cs:393` — app-level deactivate-before-create). Drive bypass T1@X→T2@Y via `ProtonBypassService`, then count. Copy `NewBypassSvc` from `ProtonBypassServiceTests.cs:29-37` and the count-assert idiom from `:113`/`:223`/`:711-714`:
```csharp
// Construct service (copy ProtonBypassServiceTests.cs:29-37):
private static ProtonBypassService NewBypassSvc(ApplicationDbContext ctx, FakeNotificationService? notif = null)
{
    var fake = notif ?? new FakeNotificationService();
    return new(ctx,
        new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fake, new AuditLogService(ctx)),
        fake, new AuditLogService(ctx), NullLogger<ProtonBypassService>.Instance);
}
// After bypass T1→T2: assert exactly 1 active PTA (Pattern 3):
var activeCount = await ctx.ProtonTrackAssignments.CountAsync(a => a.CoacheeId == coachee && a.IsActive);
Assert.Equal(1, activeCount);   // T1 deactivated, T2 active → tepat 1 (NOT DbUpdateException)
```

**QA-03 Reactivate / Import-reactivate single-active — DbContext write-pattern replication** (R-1/R-2; controller not drivable). Replicate the deactivate-before-reactivate write at DbContext level, then assert count (pola `AbandonGuardTests.RunAbandonGuardAsync`, `AbandonGuardTests.cs:88-95`). For the mapping path, the DB index already rejects a 2nd active row, so a reactivation that forgets to deactivate the old row → `DbUpdateException` (same Pattern 2 assert).

**QA-04 `AssignmentUnit ∈ UserUnits` — drive static helper** (copy assert style from `AssignmentUnitInUserUnitsTests.cs:36-38`, but against SQL-real fixture ctx). Helper signature `CoachMappingController.cs:52-62`:
```csharp
Assert.True (await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coachee, unitY)); // ∈ UserUnits
Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coachee, "UnitZ")); // ∉ → reject
```

**QA-04 B-06 anti-dobel + `ProtonKompetensi.Unit` 1:1 — drive `ProtonDeliverableBootstrap`** (Pattern 5). Helper `Helpers/ProtonDeliverableBootstrap.cs:24-95` (B-06 skip-guard at `:55-65`). Bootstrap unit X then unit Y for the SAME coachee; assert unit-Y deliverables still created (cross-unit not skipped). For 1:1: insert two `ProtonDeliverableProgress` with identical `(ProtonTrackAssignmentId, ProtonDeliverableId)` → `DbUpdateException` (unique `ApplicationDbContext.cs:429`):
```csharp
// B-06: bootstrap X then Y, coachee sama — unit Y deliverables NOT skipped:
await ProtonDeliverableBootstrap.CreateProgressAsync(ctx, asgX.Id, trackT1, coachee, unitX);
await ProtonDeliverableBootstrap.CreateProgressAsync(ctx, asgY.Id, trackT2, coachee, unitY);
// assert progress for both unit-X and unit-Y deliverables co-exist (count > X-only).
// 1:1: duplicate (PTAId, DeliverableId) insert → Assert.ThrowsAsync<DbUpdateException>.
```

**QA-01 one-primary UserUnits — assert via `DbUpdateException`** (filtered-unique `IX_UserUnits_UserId_PrimaryUnique` WHERE `[IsPrimary]=1`, `ApplicationDbContext.cs:350-353`). Insert 2nd `IsPrimary=true` row for same user → throw (Pattern 2).

**What to copy:** `[Trait]`+`IClassFixture` wiring, `NewBypassSvc`/seed helpers from `ProtonBypassServiceTests`, count-assert idiom, `Assert.ThrowsAsync<DbUpdateException>` idiom (research §Pattern 2), helper-call asserts.
**What to change:** seed per-Fact coachee with `$"...-{Guid.NewGuid():N}"` (DB shared across Facts in one fixture — see `ProtonBypassServiceTests.cs:92` pattern) so Facts don't collide on the single-active index.

---

### `HcPortal.Tests/CrossUnitAssignTests.cs:105` (modify stub — D-09)

**Analog:** self (the file's own InMemory CXU tests `:25-103`) + `ProtonBypassServiceTests` for the SQL-real drive.

**Current stub** (`CrossUnitAssignTests.cs:106-107`):
```csharp
[Fact(Skip = "deferred to Phase 404 QA-03 — SQL-real single-active invariant")]
public void SingleActive_invariant_is_sql_real_phase404() { }
```

**Two options (D-01/D-09 discretion):**
- **(a) Implement in place** — convert to `[Trait("Category","Integration")]`, but the class itself uses InMemory (`:18-21`); a SQL-real Fact needs the fixture. Cleanest: keep a thin `[Fact]` that delegates, OR
- **(b) Delete the stub here and cover single-active in `MultiUnitInvariantSqlTests`** (research-recommended). Either way: stub must no longer be `[Skip]`-empty; single-active MUST be covered SQL-real.

**What to copy:** the count-assert + `DbUpdateException` patterns from `MultiUnitInvariantSqlTests` above.
**What to change:** remove `Skip=`; ensure the anchor named in 402 VALIDATION (`SingleActive_invariant_is_sql_real_phase404`) resolves to a live, passing SQL-real test (rename/relocate allowed per D-01).

---

### `HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs:72-91` (modify 3 stubs — Open Q1, recommended scope)

**Analog:** self — `UserUnitsBackfillFixture` (`:25-60`) already exists (migrate-only, no seed). The 3 `[Skip]` Facts are message-flagged **"diisi plan 02/404"**.

**Current stubs** (`UserUnitsBackfillIntegrationTests.cs:72-91`): `Backfill_UserWithNonNullUnit_HasExactlyOnePrimaryRow`, `_WithNullUnit_HasZeroRows`, `_RerunIsIdempotent_NoDuplicateRows` — each `Assert.True(false, ...)`.

**Implementation pattern:** fixture migrated on an empty DB (0 Users → 0 backfill rows). Each Fact: seed `ApplicationUser` rows (Unit non-null / null), run the backfill SQL (the same `INSERT...SELECT WHERE NOT EXISTS` from migration 399), assert primary-row count. Seed-user idiom from `AbandonGuardTests.cs:68-74` or `ProtonBypassServiceTests.cs:402-406`:
```csharp
ctx.Users.Add(new ApplicationUser { Id = userId, UserName = userId, FullName = $"Worker {userId[..8]}" });
await ctx.SaveChangesAsync();
// then run backfill SQL via ctx.Database.ExecuteSqlRawAsync(...) and assert:
// Unit non-null → exactly 1 UserUnits row IsPrimary=1; Unit null → 0 rows; re-run → 0 new rows.
```

**What to copy:** seed-user idiom, `[Trait("Category","Integration")]`+`IClassFixture<UserUnitsBackfillFixture>` (already wired `:62-70`).
**What to change:** replace each `Assert.True(false,...)` body with seed→backfill→assert. **Locate the exact backfill SQL** in the 399 migration (`Migrations/*AddUserUnitsTable*.cs`) and re-run it via `ExecuteSqlRawAsync` (do NOT hand-roll new SQL — reuse the migration's statement to prove idempotency truthfully).
**Planner note:** confirm Open Q1 (include or skip). Recommended: include — low-cost, strengthens QA-01 migration-399 evidence, closes residual `[Skip]`.

---

### `docs/milestone-v32.3/index.html` (doc — HTML handoff IT, D-13)

**Analog:** `docs/milestone-v31.0/index.html` (full file; also `docs/milestone-v29.0/index.html`). Self-contained styled report — copy the `<style>` block (`:7-56`), header/verdict/grid/table/note structure.

**Header + meta pattern** (`milestone-v31.0/index.html:61-71`) — CHANGE title/date/counts; **migration=TRUE** notice (v31.0 said "Tidak"; 404 says migration=TRUE Phase 399):
```html
<header class="top">
  <span class="tag">PORTAL HC KPB · RINGKASAN UNTUK TIM IT</span>
  <h1>Akun Multi-Unit (1 Bagian) — Paket v32.3</h1>
  <p>Pekerja >1 Unit dalam 1 Bagian + coaching cross-unit + PROTON sekuensial</p>
  <div class="meta">
    <span><b>Tanggal laporan:</b> ...</span>
    <span><b>Status:</b> Selesai &amp; teruji di komputer lokal</span>
    <span><b>Perlu ubah database:</b> YA — migration Fase 399 (AddUserUnits + backfill)</span>
  </div>
</header>
```

**"Langkah berikutnya (oleh Tim IT)" + commit-hash note pattern** (copy structure `milestone-v31.0/index.html:158-171`) — CHANGE: list fase 399-404, state **migration=TRUE Phase 399** `AddUserUnits` + backfill, include **commit hash**, deploy steps to Dev (10.55.3.3 / `/KPB-PortalHC`):
```html
<h3>Langkah berikutnya (oleh Tim IT)</h3>
<ol>
  <li>Pasang kode terbaru (fase 399-404) ke server Development (10.55.3.3).</li>
  <li><b>Jalankan migration database</b> — Fase 399 `AddUserUnits` (tabel junction + backfill 1 primary-row/pekerja).</li>
  <li>Verifikasi alur PROTON sekuensial cross-unit + coach multi-unit.</li>
</ol>
<p class="muted">Catatan teknis untuk IT: branch <code>ITHandoff</code>, commit <code>&lt;hash&gt;</code>, <b>flag migration = TRUE (Fase 399)</b>.</p>
```

**What to copy:** entire `<style>` block, header/verdict/grid/table/note/status/footer skeleton.
**What to change:** all content → v32.3 multi-unit; flip migration notice to TRUE; daftar fase 399-404; commit hash placeholder; embed D1=b limitation (see below).

---

### `docs/` markdown D1=b note (doc — D-14)

**Analog:** the "Yang perlu diperhatikan / batasan" note-box section in `milestone-v31.0/index.html:143-156` (HTML), OR a standalone `.md`.

**Content to document (D-14):** cert/analytics attribute to **primary unit** (`ApplicationUser.Unit` mirror), not per-unit fully — a conscious milestone design limit. Per research Open Q3: cert history per-unit IS preserved structurally via `ProtonFinalAssessment` (1:1 per `ProtonTrackAssignment` → T1 and T2 co-exist as 2 assignments), but **aggregate reporting** attributes to primary. Phrase: "cert/laporan agregat atribusi ke unit primary; cert per-track tetap tersimpan utuh sebagai histori (tidak hilang)."

**What to copy:** the styled `.note` box idiom (`milestone-v31.0/index.html:146-155`) if embedding in HTML; or plain markdown if standalone `docs/*.md`.
**What to change:** content → D1=b explanation per spec `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md`.

---

### `docs/SEED_JOURNAL.md` (+1 entry — D-12)

**Analog:** existing rows for multi-unit UAT — `SEED_JOURNAL.md:9` (403-02), `:11-12` (400-01), `:13` (399). Same table schema, same snapshot→seed→restore→`cleaned` lifecycle.

**Row pattern** (one line, columns: Tanggal | Phase | Klasifikasi | Tujuan | Dampak | Snapshot file | Status). Copy the 403-02 / 400-01 cadence. For 404: classification `temporary + local-only`; tujuan = UAT PROTON sekuensial cross-unit T1@X→T2@Y @5270 (D-10/D-11); dampak = additive UserUnits/ProtonTrackAssignment/CoachCoacheeMapping rows for {X,Y}+coach; snapshot `.bak`; status `active` during, flip to `cleaned` after `RESTORE WITH REPLACE`.

**What to copy:** exact column structure + the snapshot→seed→RESTORE→verify-baseline cadence (note how 400-01 records "UserUnits=6 baseline verified" post-restore — replicate that proof).
**What to change:** phase=404, the UAT scenario description, the entity-impact list.

---

## Shared Patterns

### SQL-real disposable fixture (applies to ALL test files)
**Source:** `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs:24-66` (+ 5 sibling fixtures, all identical).
**Apply to:** `MultiUnitSqlFixture`; reused by `MultiUnitInvariantSqlTests`, `CrossUnitAssignTests` SQL-real Fact. Existing `UserUnitsBackfillFixture` already follows it.
```csharp
public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
_cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
// InitializeAsync: MigrateAsync() → seed; catch → EnsureDeletedAsync + XunitException "MIGRATION-CHAIN break"
// DisposeAsync: EnsureDeletedAsync()
```

### Integration trait (applies to ALL test files)
**Source:** every SQL-real test class, e.g. `ProtonBypassServiceTests.cs:17`, `AbandonGuardTests.cs:60`, `UserUnitsBackfillIntegrationTests.cs:62`.
**Apply to:** all 404 SQL-real test classes — without it CI SQL-less cannot skip (`dotnet test --filter "Category!=Integration"`).
```csharp
[Trait("Category", "Integration")]
```

### Assert strategy split — exception vs count (the load-bearing decision, QA-03)
**Source:** research Pitfall 1 + `ApplicationDbContext.cs:333-336` (mapping filtered-unique) vs `:393` (PTA non-unique).
**Apply to:** every single-active Fact.
- **DB-enforced (filtered-unique):** `CoachCoacheeMapping` 2nd-active, `UserUnits` 2nd-primary, `ProtonKompetensi.Unit` 1:1 → `Assert.ThrowsAsync<DbUpdateException>`.
- **App-enforced (non-unique index):** `ProtonTrackAssignment` single-active → drive write-path then `Assert.Equal(1, CountAsync(a => a.CoacheeId==id && a.IsActive))`. **Never** expect `DbUpdateException` for PTA.

### Write-path drivability (R-1 — avoid seam extraction)
**Source:** research §R-1; `AbandonGuardTests.cs:88-95` (replication), `ProtonBypassServiceTests.cs:29-37` (service drive), `CoachMappingController.cs:52-62` / `ProtonDeliverableBootstrap.cs:24` (static helpers).
**Apply to:** all controller-gated invariants.
- (a) Direct DB constraint assert (mapping/primary/1:1) — index rejects regardless of which controller path.
- (b) DbContext write-pattern replication (reactivate single-active) — test code, NOT production change.
- (c) Existing static helper / `ProtonBypassService` drive (unit-membership, B-06, bypass T1→T2).
> If any invariant truly needs a NEW seam, **flag it as an explicit planning decision** (trade-off: re-trigger code-review+secure 404). Do not silently extract.

### Per-Fact unique coachee id (shared-DB fixtures)
**Source:** `ProtonBypassServiceTests.cs` — every Fact uses `$"<prefix>-{Guid.NewGuid():N}"` (e.g. `:92`, `:124`).
**Apply to:** all Facts sharing `MultiUnitSqlFixture` — single-active indexes are per-coachee; collisions across Facts would false-fail. Use a fresh guid coachee per Fact.

---

## No Analog Found

None. Every 404 artifact maps to an exact or composite analog already in `HcPortal.Tests/` or `docs/`. The phase value is **applying** existing patterns to the multi-unit dimension, not building new harness (research §Don't Hand-Roll "Key insight").

| File | Status |
|------|--------|
| (all) | analog found — see table above |

---

## Metadata

**Analog search scope:** `HcPortal.Tests/` (fixtures, CXU/bypass/backfill/abandon/assignment-unit tests), `Data/ApplicationDbContext.cs` (filtered-unique index map), `Controllers/CoachMappingController.cs` (static helper), `Helpers/ProtonDeliverableBootstrap.cs`, `Services/ProtonBypassService.cs` (MoveAssignmentAsync), `docs/milestone-v31.0/index.html`, `docs/SEED_JOURNAL.md`.
**Files scanned:** 12
**Pattern extraction date:** 2026-06-21
