# Phase 323: Fix Cascade Bug AssessmentEditLogs - Research

**Researched:** 2026-05-26
**Domain:** ASP.NET Core MVC + Entity Framework Core cascade-delete pattern (.NET 8 / EF Core 8) + Playwright E2E smoke test
**Confidence:** HIGH

## Summary

Phase 323 menutup oversight Phase 321: model `AssessmentEditLog` baru di-deklarasi **explicit** `DeleteBehavior.Restrict` di `Data/ApplicationDbContext.cs:241`, sehingga `RemoveRange(AssessmentEditLogs)` HARUS dipanggil sebelum `Remove(AssessmentSession)` di 3 endpoint cascade existing (`DeleteAssessment` L2071, `DeleteAssessmentGroup` L2215, `DeletePrePostGroup` L2348). Tanpa block ini, FK `AssessmentEditLogs.AssessmentSessionId` memblokir delete dengan `DbUpdateException`, yang ditangkap generic `catch (Exception ex)` di L2140/2278/2404 dan ditampilkan sebagai pesan flash `"Gagal menghapus assessment. Silakan coba lagi."` — persis behavior repro di Dev untuk session Id 2+5.

Pola implementasi sudah ada 100% di codebase: copy pattern `PackageUserResponses` block (L2073-2080, L2216-2220, L2348-2352) — `var editLogs = await _context.AssessmentEditLogs.Where(...).ToListAsync()` + `if (editLogs.Any()) { logger.LogInformation(...); _context.AssessmentEditLogs.RemoveRange(editLogs); }`. Posisi: SETELAH `preDeleteResponseCount` re-check guard, SEBELUM `PackageUserResponses` block. Snapshot `preDeleteEditLogsCount` ikut diambil sebelum cascade untuk masuk ke audit description.

Smoke test ikuti pola existing `tests/e2e/edit-peserta-answers.spec.ts` (Phase 321) — Playwright TypeScript 1.58, `accounts.admin` fixture (`admin@pertamina.com / 123456`), `loginAny()` helper inline, `baseURL: http://localhost:5277`. Seed AssessmentEditLog temporary cukup INSERT minimum 1-2 row dengan FK ke sesi target (PackageQuestion exists tidak wajib di-resolve karena FK `PackageQuestionId` tidak punya navigation back-restriction); SEED_WORKFLOW snapshot+journal+restore wajib diikuti.

**Primary recommendation:** Tambahkan block `RemoveRange(AssessmentEditLogs)` paling awal di cascade chain di 3 endpoint dengan pattern identik `PackageUserResponses` (guard `.Any()`, log info, transaction-wrapped), capture `preDeleteEditLogsCount` ke audit description, dan tulis 1 Playwright spec `tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts` dengan 3 test (no-edits, with-edits, group-mixed) — seed via SQL INSERT temporary mengikuti SEED_WORKFLOW.

## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01: Cascade chain order** — Block `RemoveRange(AssessmentEditLogs)` taruh **paling awal** di cascade chain — sebelum `PackageUserResponses`. Urutan final: `1.EditLogs → 2.PackageUserResponses → 3.AttemptHistory → 4.Packages(+Questions+Options) → 5.Session`. Alasan: edit logs = snapshot soal yang akan dihapus di step 4; hapus snapshot dulu paling clean buat audit trail.
- **D-02: Audit log description** — Tambah `EditLogsCount=N` field. Format final:
  - `DeleteAssessment`: `"Deleted assessment '{title}' [ID={id}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount} EditLogsCount={preDeleteEditLogsCount}"`
  - `DeleteAssessmentGroup`: `"Deleted assessment group '{title}' ({category}) [RepId={id}] SessionCount={N} Status={...} ResponseCount={...} EditLogsCount={...}"`
  - `DeletePrePostGroup`: ikuti pola yang sama.
  - Capture `preDeleteEditLogsCount` SEBELUM cascade.
- **D-04: Logging pattern** — Skip `LogInformation` kalau `editLogs.Count == 0`. Pakai guard `if (editLogs.Any()) { logger.LogInformation(...); _context.AssessmentEditLogs.RemoveRange(editLogs); }` — pola Phase 312.
- **D-03: Smoke test** — Playwright E2E lokal (`tests/e2e/`), login `admin@pertamina.com`, connect `localhost:5277`. 3 skenario wajib:
  - (a) Session no-edits → delete OK
  - (b) Session 1+ edits → delete OK + EditLogs ikut terhapus
  - (c) Group campuran sibling no-edits + edits → delete OK
  - Seed temporary `AssessmentEditLog` untuk (b)+(c) ikut SEED_WORKFLOW: snapshot DB, catat `docs/SEED_JOURNAL.md` klasifikasi `temporary + local-only`, restore + tandai `cleaned`.

### Claude's Discretion

- Variable naming snapshot: `preDeleteEditLogsCount` (ikut pola existing `preDeleteResponseCount`).
- Comment header per block: `// PHASE 323: Delete AssessmentEditLogs (Restrict FK — must be removed before session)`.
- Commit strategy: 1 commit covering 3 endpoint patch + 1 commit untuk test (atomic per logical change) — planner boleh override.

### Deferred Ideas (OUT OF SCOPE)

- Extract `CascadeAssessmentSessionDependents(sessionIds)` helper — wait for ke-4 cascade signal.
- Audit endpoint delete lain (DeleteCategory, DeletePackage, DeleteQuestion, DeleteWorker, DeleteTraining, dll.) — milestone `v19.0 Cascade Audit Sweep` (calon).
- Migration FK Restrict → Cascade DB-level — keputusan tetap endpoint explicit (audit-friendly).
- UI filter old assessment di `ManageAssessmentTab_Assessment` line 115 — backlog UX tersendiri.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CASCADE-01 | Admin/HC dapat menghapus AssessmentSession (single, group, atau Pre-Post group) yang sudah pernah di-edit soalnya — AssessmentEditLogs ikut ter-cascade tanpa FK Restrict exception. | (1) FK Restrict confirmed di `ApplicationDbContext.cs:241` `OnDelete(DeleteBehavior.Restrict)`; (2) Pattern reference existing `PackageUserResponses` block di 3 endpoint (L2073-2080, L2216-2220, L2348-2352); (3) Transaction wrapping existing di L2040, L2184, L2313 — semua di-confirm via Read tool. |

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia** untuk semua output user-facing (komentar code optional EN, tapi description audit log, log messages, pesan flash bilingual sesuai pola existing).
- **Develop Workflow** — verifikasi lokal wajib: `dotnet build` + `dotnet run` + cek `http://localhost:5277` + Playwright (kalau ada) sebelum commit/push. Promosi ke Dev `10.55.3.3` = tanggung jawab Team IT.
- **Notify IT setelah push** — commit hash + flag NO migration (Phase 323 explicit out-of-scope schema change per REQ acceptance #7).
- **Seed Data Workflow** — klasifikasi `temporary + local-only`, snapshot DB sebelum INSERT seed, catat di `docs/SEED_JOURNAL.md` (status `active`), restore setelah test (status `cleaned`). Sqlcmd BACKUP/RESTORE template di `docs/SEED_WORKFLOW.md` §5.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| FK Restrict cascade chain extension | API / Backend (Controllers/AssessmentAdminController) | Database / Storage (EF Core OnDelete config) | Logic harus eksplisit di controller karena `OnDelete(Restrict)` di DbContext (`ApplicationDbContext.cs:241`) memang sengaja Restrict — bukan Cascade — supaya endpoint pegang kontrol audit-friendly. |
| Pre-delete snapshot capture (`preDeleteEditLogsCount`) | API / Backend | — | Diambil sebelum `RemoveRange` supaya nilai available di audit description setelah commit (entity sudah hilang post-SaveChanges). |
| Audit log description format extension | API / Backend (AuditLogService) | — | Description string dibangun di endpoint sebelum `_auditLog.LogAsync(...)` — extension berupa append `EditLogsCount={N}` token. |
| Smoke test E2E (3 skenario) | Test / E2E (Playwright tests/e2e/) | Database / Storage (SQL seed via sqlcmd) | Playwright TypeScript ekosistem existing; SQL seed via `sqlcmd -E -Q "INSERT INTO AssessmentEditLogs ..."` di Wave 0 setup. |
| DB snapshot + restore lifecycle | Test / E2E (manual via sqlcmd per SEED_WORKFLOW) | — | Bukan auto-wire ke globalSetup/Teardown — manual sebelum/sesudah Playwright run per SEED_WORKFLOW §4. |

## Standard Stack

### Core (already in project — no install needed)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET | 8.0 | Runtime | [VERIFIED: `HcPortal.csproj` `<TargetFramework>net8.0</TargetFramework>`] — pinned project framework |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | ORM + SQL Server provider | [VERIFIED: `HcPortal.csproj:16`] — `RemoveRange` + `BeginTransactionAsync` semua dari EF Core 8 |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.0 | Auth + Roles | [VERIFIED: `HcPortal.csproj:14`] — `[Authorize(Roles = "Admin, HC")]` existing di endpoint |
| @playwright/test | ^1.58.2 | E2E test runner | [VERIFIED: `tests/package.json:17`] — pattern test existing di `tests/e2e/edit-peserta-answers.spec.ts` |
| TypeScript | ^5.9.3 | Spec language | [VERIFIED: `tests/package.json:18`] |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| sqlcmd | (SQL Server CLI) | Seed INSERT + BACKUP/RESTORE | [CITED: `docs/SEED_WORKFLOW.md` §5] — `localhost\SQLEXPRESS` + Windows Integrated Security `-E` flag |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Endpoint explicit `RemoveRange` | DB-level `OnDelete(DeleteBehavior.Cascade)` via migration | DB cascade silent (no audit count); endpoint explicit = sesuai pattern Phase 312 + audit-friendly. **Locked by CONTEXT.md deferred ideas — DO NOT use.** |
| Playwright TypeScript spec | Playwright .NET (NUnit + Microsoft.Playwright nuget) | Ekosistem proyek 100% TypeScript [VERIFIED: 9 spec di `tests/e2e/*.spec.ts`, 0 file `.cs` test]. Pakai TypeScript untuk konsistensi. |
| Capture via `Load + Count` | `CountAsync(...)` (server-side aggregation) | `CountAsync` lebih efisien (no row materialization). Pakai `CountAsync` mengikuti pola existing `preDeleteResponseCount` L2056-2057. |
| Seed via Razor wizard UI | Seed via SQL INSERT direct | UI wizard slow + butuh 5 step setup; SQL INSERT direct cukup karena `AssessmentEditLog` self-contained (FK ke SessionId, PackageQuestionId saja). |

**Installation:** Tidak ada package baru. Semua dependency existing.

**Version verification:** Tidak perlu npm/nuget install — semua dependency sudah locked di `HcPortal.csproj` (.NET 8) dan `tests/package.json` (Playwright 1.58 + TS 5.9).

## Architecture Patterns

### System Architecture Diagram

```
[Browser: Admin/HC Manage Assessment page]
      │ POST /AssessmentAdmin/DeleteAssessment/{id}
      │ POST /AssessmentAdmin/DeleteAssessmentGroup/{id}
      │ POST /AssessmentAdmin/DeletePrePostGroup/{linkedGroupId}
      ▼
[ASP.NET Core MVC: AssessmentAdminController]
      │
      ├─ [Authorize(Roles="Admin, HC")] guard
      ├─ Fetch target session(s) — FirstOrDefaultAsync / Where ToListAsync
      ├─ Pre-Post block (DeleteAssessment only — L2031)
      │
      ├─ BEGIN TRANSACTION (BeginTransactionAsync) ────────────┐
      │                                                          │
      ├─ EnsureCanDeleteAsync (Phase 312 role guard)             │
      │                                                          │
      ├─ Capture snapshot (PRE-cascade):                         │
      │     • preDeleteStatus                                    │
      │     • preDeleteResponseCount (CountAsync)                │
      │     • preDeleteEditLogsCount (CountAsync) ◄── NEW P323   │
      │                                                          │
      ├─ Re-check responseCount HC tier guard                    │
      │                                                          │
      ├─ ★ NEW (P323): RemoveRange(AssessmentEditLogs)          │
      │     IF .Any() → LogInformation + RemoveRange             │
      │                                                          │
      ├─ RemoveRange(PackageUserResponses)  ─┐                   │
      ├─ RemoveRange(AssessmentAttemptHistory)│ existing cascade  │
      ├─ RemoveRange(Packages+Q+Options)     │                   │
      ├─ Remove(AssessmentSession)           ─┘                   │
      │                                                          │
      ├─ SaveChangesAsync ◄── single batch DELETE                │
      ├─ COMMIT TRANSACTION ─────────────────────────────────────┘
      │
      ├─ Audit log write (try/catch — non-blocking):
      │     _auditLog.LogAsync(..., $"... EditLogsCount={preDeleteEditLogsCount}", ...)
      │
      └─ Redirect to ManageAssessment + TempData["Success" | "Error"]
                  │
                  ▼
            [Browser: flash banner + page reload]

[On Exception (FK Restrict or other DbUpdateException)]
   → catch (Exception ex) L2140/2278/2404
   → logger.LogError(ex, "Error deleting ...")
   → tx auto-rollback (using var disposal)
   → TempData["Error"] = "Gagal menghapus assessment. Silakan coba lagi."

[Playwright E2E smoke test — separate process]
   localhost:5277 ←─ Page (admin@pertamina.com)
                 ↓ navigate Manage Assessment → click Delete
                 ↓ assert toast success + check DB row gone via subsequent navigation
   sqlcmd ─────→ pre-test snapshot (.bak)
   sqlcmd ─────→ INSERT AssessmentEditLogs temporary (skenario b, c)
   [run npx playwright test]
   sqlcmd ─────→ RESTORE FROM .bak (cleanup)
   docs/SEED_JOURNAL.md status → cleaned
```

### Component Responsibilities

| File | Responsibility | Change Type |
|------|----------------|-------------|
| `Controllers/AssessmentAdminController.cs:2017-2146` (`DeleteAssessment`) | Insert EditLogs cascade block + snapshot + audit description | Edit (~10 LOC) |
| `Controllers/AssessmentAdminController.cs:2147-2284` (`DeleteAssessmentGroup`) | Insert EditLogs cascade block + snapshot + audit description | Edit (~10 LOC) |
| `Controllers/AssessmentAdminController.cs:2286-2410` (`DeletePrePostGroup`) | Insert EditLogs cascade block + snapshot + audit description | Edit (~10 LOC) |
| `tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts` | 3 test E2E (no-edits, with-edits, group-mixed) | New |
| `docs/SEED_JOURNAL.md` | Append entry P323 — `temporary + local-only` | Append (1 row, status `active` → `cleaned`) |

### Recommended Project Structure (existing — no new folders)

```
Controllers/
  └── AssessmentAdminController.cs    # 3 endpoint patched
Data/
  └── ApplicationDbContext.cs         # NO CHANGE — FK config already correct
Models/
  └── AssessmentEditLog.cs            # NO CHANGE — model already exists
tests/
  └── e2e/
      └── Phase323_CascadeAssessmentEditLogs.spec.ts  # NEW
  └── helpers/
      └── accounts.ts                 # existing — reuse `accounts.admin`
docs/
  ├── SEED_JOURNAL.md                 # append 1 row
  └── SEED_WORKFLOW.md                # reference only
```

### Pattern 1: EF Core RemoveRange + Restrict FK Cascade

**What:** Saat FK di-konfigurasi `OnDelete(DeleteBehavior.Restrict)`, child rows TIDAK auto-delete saat parent dihapus — EF Core throw `DbUpdateException` (FK constraint violation) di `SaveChangesAsync`. Solusi: explicit `RemoveRange(childCollection)` SEBELUM `Remove(parent)`, semua dalam 1 transaction supaya atomic.

**When to use:** Selalu untuk Phase 312-pattern cascade endpoints — supaya control count + audit trail explicit.

**Example (Phase 323 — direct adaptation dari Phase 312 pattern existing):**

```csharp
// Source: Controllers/AssessmentAdminController.cs:2073-2080 (PackageUserResponses pattern — existing Phase 312)
// PHASE 323: Delete AssessmentEditLogs (Restrict FK — must be removed before session)
var editLogs = await _context.AssessmentEditLogs
    .Where(e => e.AssessmentSessionId == id)
    .ToListAsync();
if (editLogs.Any())
{
    logger.LogInformation($"Deleting {editLogs.Count} assessment edit logs");
    _context.AssessmentEditLogs.RemoveRange(editLogs);
}
```

Untuk `DeleteAssessmentGroup` + `DeletePrePostGroup` (multi-session), pakai `siblingIds.Contains(...)`:

```csharp
// PHASE 323: Delete AssessmentEditLogs untuk semua siblings
var allEditLogs = await _context.AssessmentEditLogs
    .Where(e => siblingIds.Contains(e.AssessmentSessionId))  // atau groupIds untuk PrePost
    .ToListAsync();
if (allEditLogs.Any())
{
    logger.LogInformation($"DeleteAssessmentGroup: deleting {allEditLogs.Count} edit logs across {siblingIds.Count} sessions");
    _context.AssessmentEditLogs.RemoveRange(allEditLogs);
}
```

### Pattern 2: Pre-cascade snapshot capture untuk audit description

**What:** Audit log description berisi count entity yang ikut terhapus. Karena entity di-Remove sebelum `SaveChangesAsync`, count harus di-snapshot SEBELUM `RemoveRange`. Pola existing: `int preDeleteResponseCount = await _context.PackageUserResponses.CountAsync(...)` di L2056-2057.

**Example (Phase 323):**

```csharp
// Source: Controllers/AssessmentAdminController.cs:2055-2057 (existing pattern)
int preDeleteResponseCount = await _context.PackageUserResponses
    .CountAsync(r => r.AssessmentSessionId == id);

// PHASE 323: snapshot EditLog count SEBELUM cascade (sama pola)
int preDeleteEditLogsCount = await _context.AssessmentEditLogs
    .CountAsync(e => e.AssessmentSessionId == id);
```

Lalu di audit description:

```csharp
await _auditLog.LogAsync(
    deleteUser?.Id ?? "",
    deleteActorName,
    "DeleteAssessment",
    $"Deleted assessment '{assessmentTitle}' [ID={id}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount} EditLogsCount={preDeleteEditLogsCount}",
    id,
    "AssessmentSession");
```

### Pattern 3: Transaction-wrapped cascade

**What:** `using var tx = await _context.Database.BeginTransactionAsync()` membungkus guard + cascade + commit. Saat exception → `using` disposal auto-rollback (atau explicit `await tx.RollbackAsync()` di guard fail).

**Why it matters:** Kalau `RemoveRange(EditLogs)` sukses tapi `RemoveRange(Packages)` throw (e.g., FK lain belum kelar), transaction rollback bersih — EditLogs tetap utuh, tidak corrupt state.

**Existing usage:** L2040, L2184, L2313 (3 endpoint). Block EditLogs sisip DALAM scope `tx`, otomatis ikut atomicity.

### Anti-Patterns to Avoid

- **`SaveChangesAsync` per cascade block.** Hanya 1 `SaveChangesAsync` di akhir (existing pattern L2115). Multiple SaveChanges = multiple round-trip + lose atomicity di-luar transaction.
- **Skip `if (editLogs.Any())` guard.** Tanpa guard, log noise muncul untuk 99% sesi yang belum pernah di-edit. Locked per D-04.
- **Capture `preDeleteEditLogsCount` SETELAH `RemoveRange`.** Akan return 0 (rows sudah marked-for-delete di change tracker). Pre-snapshot WAJIB.
- **Reorder cascade chain (PackageUserResponses dulu).** Locked per D-01 — EditLogs MUST be first.
- **Add migration / model change.** Locked per REQ acceptance #7 — `schema/model/migration` change explicit out-of-scope.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Cascade helper untuk 3 endpoint | Custom `CascadeAssessmentSessionDependents(sessionIds)` method | Inline `RemoveRange` per endpoint (copy 3x) | Locked per CONTEXT.md deferred — wait for ke-4 cascade signal. 3x copy lebih jelas + audit-traceable per endpoint. |
| Audit log row schema extension | New field `EditLogsCount int` di `AuditLog.cs` | Append `EditLogsCount={N}` ke `Description` string | `Description` field type `string` (no MaxLength attribute per `Models/AuditLog.cs:36-37`) — `nvarchar(max)` di DB. Append token = backward compat, queryable via `LIKE '%EditLogsCount=%'`. |
| Test fixture management | Custom Playwright `globalSetup` extension | Manual sqlcmd BACKUP/RESTORE per SEED_WORKFLOW | Phase 315 globalSetup ada (`tests/e2e/global.setup.ts`) tapi scope spesifik MATRIX_TEST. Phase 323 cukup pakai manual sqlcmd — lebih simple, tidak coupling ke Phase 315 lifecycle. |
| FK behavior config change | EF migration `OnDelete(Cascade)` | Endpoint explicit `RemoveRange` | Locked per CONTEXT.md deferred — DB cascade silent vs audit-friendly explicit, sudah diputuskan. |

**Key insight:** Phase 323 hampir pure-pattern-copy. Tidak ada "library decision" — semua tooling sudah ada di codebase. Risiko terbesar = melenceng dari pattern existing (e.g., reorder cascade, lupa snapshot, miss test skenario).

## Runtime State Inventory

> Phase 323 = refactor / bugfix di code path delete. Inventory diperlukan walau tidak ada rename — supaya planner aware dari potensial sisa runtime state.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `AssessmentEditLogs` table di `HcPortalDB_Dev` (local) dan Dev `HcPortalDB_Dev` (server IT). Row count belum di-probe untuk lokal; di Dev ada untuk Session Id 2 + Id 5 (per repro evidence CONTEXT.md). | Tidak ada data migration. Code fix → IT promote → retry hapus Session 2+5 di Dev via UI Admin (bonus optional: SQL one-off di STATE.md Next Action #3 untuk hapus 2 record sekarang tanpa tunggu code fix). |
| Live service config | None — fix ini code-only di controller, tidak menyentuh konfigurasi service eksternal (n8n / Datadog / SignalR hub config). | None. |
| OS-registered state | None — tidak ada Windows Task Scheduler / pm2 / systemd unit ter-affected oleh perubahan ini. | None. |
| Secrets / env vars | None baru. Tetap pakai `appsettings.Development.json` connection string + Playwright env `COMPLETED_PASS_SESSION_ID` (opsional reuse pattern dari Phase 321 spec). | None — kalau spec Phase 323 perlu env var untuk session ID temporary, dokumentasikan di test file header. |
| Build artifacts | None — `dotnet build` regenerate bin/obj normal; `bin/Debug/net8.0/HcPortal.dll` auto-refresh. | Standard `dotnet build` setelah edit. |

**Catatan tambahan:** `IIS` di server Dev 10.55.3.3 cache assembly — Team IT perlu IIS recycle setelah deploy DLL baru (bagian dari standard Develop Workflow IT handoff, bukan tanggung jawab Phase 323 scope).

## Common Pitfalls

### Pitfall 1: Snapshot capture timing salah (POST-cascade)

**What goes wrong:** `preDeleteEditLogsCount = await _context.AssessmentEditLogs.CountAsync(...)` di-panggil SETELAH `RemoveRange(editLogs)`. Result: 0 karena change tracker sudah mark rows untuk delete (CountAsync hit DB tapi pending changes belum SaveChanges; behavior dependent on tracker mode).

**Why it happens:** Refactor "consolidate snapshot collection" → urutan capture rusak.

**How to avoid:** SEMUA snapshot capture berurutan SEBELUM block pertama `RemoveRange` (mengikuti existing pattern L2055-2057). Plan task description harus eksplisit: "Sisip capture `preDeleteEditLogsCount` di line **antara** existing `preDeleteResponseCount` capture dan response re-check guard."

**Warning signs:** Audit log entry menampilkan `EditLogsCount=0` di Dev meskipun session ada edit logs (Id 2+5 di repro evidence).

### Pitfall 2: Block urutan tertukar (EditLogs setelah Packages)

**What goes wrong:** EditLogs block disisip SETELAH `RemoveRange(Packages)` — fungsional masih jalan (delete sukses) tapi melanggar D-01 (EditLogs harus paling awal). Akibat: kalau ada FK lain di future, edit log snapshot point-in-time hilang.

**Why it happens:** Implementer "copy pattern" tapi paste di posisi salah karena cascade chain panjang (4 block).

**How to avoid:** Plan task wajib spesifik insertion line (e.g., "Sisip block baru ANTARA `preDeleteResponseCount` re-check guard (L2070) dan `PackageUserResponses` block (L2073)"). Code review wajib cek urutan via diff.

**Warning signs:** Reorder check di code review — `editLogs` variable muncul setelah `pkgResponses` variable di file.

### Pitfall 3: Lupa cabang `DeleteAssessmentGroup` / `DeletePrePostGroup` (hanya patch 1 dari 3)

**What goes wrong:** Implementer fokus `DeleteAssessment` (paling sering ditest), lupa `DeleteAssessmentGroup` (multi-session via Title+Category+Schedule grouping) dan `DeletePrePostGroup` (multi-session via LinkedGroupId). Session group dengan ≥1 edit log tetap exception.

**Why it happens:** 3 endpoint terlihat redundant; implementer "DRY" mental shortcut.

**How to avoid:** Plan task explicit 3 sub-task: Patch DeleteAssessment, Patch DeleteAssessmentGroup, Patch DeletePrePostGroup. Smoke test skenario (c) "group campuran" → cover DeleteAssessmentGroup. Untuk PrePostGroup, planner boleh add bonus 4th smoke skenario atau code-review-only verification.

**Warning signs:** Smoke test skenario (c) fail; OR grep audit menemukan `editLogs` muncul hanya di 1 dari 3 endpoint.

### Pitfall 4: Hardcoded session ID di Playwright spec (bukan env var)

**What goes wrong:** Spec hardcode `const SESSION_ID = 123` — saat seed re-run, ID drift (IDENTITY column tidak deterministic). Test fail di run kedua.

**Why it happens:** Convenience saat develop spec local.

**How to avoid:** Pakai env var pattern existing dari `tests/e2e/edit-peserta-answers.spec.ts:20-23`:

```typescript
const SESSION_NO_EDITS = parseInt(process.env.P323_SESSION_NO_EDITS ?? '0', 10);
const SESSION_WITH_EDITS = parseInt(process.env.P323_SESSION_WITH_EDITS ?? '0', 10);
const SESSION_GROUP_REP = parseInt(process.env.P323_SESSION_GROUP_REP ?? '0', 10);
```

Atau alternative: dynamic discovery via Playwright fetch ke `/Admin/ManageAssessment` + parse DOM untuk pick session matching condition (lebih kompleks; pilih env var pattern Phase 321 untuk konsistensi).

**Warning signs:** Test pass di run pertama, fail di run kedua dengan "session not found".

### Pitfall 5: Test menyentuh DB Dev/Prod (forbidden per CLAUDE.md)

**What goes wrong:** Playwright config `baseURL` di-set ke `http://10.55.3.3/KPB-PortalHC` instead of `http://localhost:5277`. Seed temporary insert + delete operation execute di DB IT.

**Why it happens:** Copy spec from old context; salah env var.

**How to avoid:** Spec hardcode assertion `expect(baseURL).toContain('localhost:5277')` di `beforeAll` (defensive). Plan checklist eksplisit: "Verify `tests/playwright.config.ts:13` `baseURL: 'http://localhost:5277'` UNCHANGED."

**Warning signs:** `docs/SEED_JOURNAL.md` entry tidak `cleaned` setelah test (restore tidak applicable ke DB Dev).

### Pitfall 6: Seed AssessmentEditLog tanpa AssessmentSession yang valid

**What goes wrong:** INSERT `AssessmentEditLog` row dengan `AssessmentSessionId = 99999` (tidak exist) — FK Restrict di OPPOSITE direction (EditLog → Session) menolak INSERT dengan FK constraint violation.

**Why it happens:** Spec writer asumsi seed self-contained.

**How to avoid:** Seed workflow: (1) Pilih existing AssessmentSession Completed atau Open dengan PackageQuestion exists, (2) INSERT EditLog row dengan FK valid + `PackageQuestionId` valid (FK juga ada). Query helper:

```sql
SELECT TOP 1 s.Id AS SessionId, pq.Id AS PackageQuestionId
FROM AssessmentSessions s
JOIN AssessmentPackages p ON p.AssessmentSessionId = s.Id
JOIN PackageQuestions pq ON pq.AssessmentPackageId = p.Id
WHERE s.Status = 'Completed'
ORDER BY s.Id;
```

**Warning signs:** sqlcmd output `The INSERT statement conflicted with the FOREIGN KEY constraint`.

## Code Examples

### Endpoint 1: DeleteAssessment patch (full snippet to apply)

```csharp
// Source: derived from Controllers/AssessmentAdminController.cs:2055-2080 existing pattern
// Insertion zone: ANTARA line 2070 (HC tier re-check guard return) dan line 2073 (existing PackageUserResponses block)

// === SISIP DI line 2057-an (capture snapshot — berurutan dengan existing) ===
int preDeleteResponseCount = await _context.PackageUserResponses
    .CountAsync(r => r.AssessmentSessionId == id);

// PHASE 323: snapshot EditLog count SEBELUM cascade (sama pola preDeleteResponseCount)
int preDeleteEditLogsCount = await _context.AssessmentEditLogs
    .CountAsync(e => e.AssessmentSessionId == id);

// ... (HC race re-check guard tetap di sini, no change) ...

// === SISIP DI line 2071-an (block baru SEBELUM PackageUserResponses) ===
// PHASE 323: Delete AssessmentEditLogs (Restrict FK — must be removed before session)
var editLogs = await _context.AssessmentEditLogs
    .Where(e => e.AssessmentSessionId == id)
    .ToListAsync();
if (editLogs.Any())
{
    logger.LogInformation($"Deleting {editLogs.Count} assessment edit logs");
    _context.AssessmentEditLogs.RemoveRange(editLogs);
}

// Existing PackageUserResponses block continues (no change)
var pkgResponses = await _context.PackageUserResponses
    .Where(r => r.AssessmentSessionId == id)
    .ToListAsync();
// ... rest unchanged ...

// === UBAH line 2127 (audit description) ===
await _auditLog.LogAsync(
    deleteUser?.Id ?? "",
    deleteActorName,
    "DeleteAssessment",
    $"Deleted assessment '{assessmentTitle}' [ID={id}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount} EditLogsCount={preDeleteEditLogsCount}",
    id,
    "AssessmentSession");
```

### Endpoint 2: DeleteAssessmentGroup patch (multi-session adaptation)

```csharp
// Source: derived from Controllers/AssessmentAdminController.cs:2198-2220 existing pattern

// === SISIP DI line 2202-an (snapshot collection) ===
int preDeleteResponseCount = await _context.PackageUserResponses
    .CountAsync(r => siblingIds.Contains(r.AssessmentSessionId));
int preDeleteSessionCount = siblings.Count;

// PHASE 323: snapshot EditLog count agregat semua siblings
int preDeleteEditLogsCount = await _context.AssessmentEditLogs
    .CountAsync(e => siblingIds.Contains(e.AssessmentSessionId));

// === SISIP DI line 2215-an (block baru SEBELUM PackageUserResponses) ===
// PHASE 323: Delete AssessmentEditLogs untuk semua siblings (Restrict FK)
var allEditLogs = await _context.AssessmentEditLogs
    .Where(e => siblingIds.Contains(e.AssessmentSessionId))
    .ToListAsync();
if (allEditLogs.Any())
{
    logger.LogInformation($"DeleteAssessmentGroup: deleting {allEditLogs.Count} edit logs across {siblingIds.Count} sessions");
    _context.AssessmentEditLogs.RemoveRange(allEditLogs);
}

// === UBAH line 2265 (audit description) ===
await _auditLog.LogAsync(
    dgUser?.Id ?? "",
    dgActorName,
    "DeleteAssessmentGroup",
    $"Deleted assessment group '{rep.Title}' ({rep.Category}) [RepId={id}] SessionCount={preDeleteSessionCount} Status={preDeleteStatus} ResponseCount={preDeleteResponseCount} EditLogsCount={preDeleteEditLogsCount}",
    id,
    "AssessmentSession");
```

### Endpoint 3: DeletePrePostGroup patch (LinkedGroupId variant)

```csharp
// Source: derived from Controllers/AssessmentAdminController.cs:2327-2352 existing pattern

// === SISIP DI line 2333-an (snapshot collection) ===
int preDeleteResponseCount = await _context.PackageUserResponses
    .CountAsync(r => groupIds.Contains(r.AssessmentSessionId));

// PHASE 323: snapshot EditLog count agregat PrePost group
int preDeleteEditLogsCount = await _context.AssessmentEditLogs
    .CountAsync(e => groupIds.Contains(e.AssessmentSessionId));

// === SISIP DI line 2348-an (block baru SEBELUM PackageUserResponses) ===
// PHASE 323: Delete AssessmentEditLogs untuk semua sessions di Pre-Post group
var allEditLogs = await _context.AssessmentEditLogs
    .Where(e => groupIds.Contains(e.AssessmentSessionId))
    .ToListAsync();
if (allEditLogs.Any())
{
    logger.LogInformation($"DeletePrePostGroup: deleting {allEditLogs.Count} edit logs across {groupIds.Count} sessions (LinkedGroupId={linkedGroupId})");
    _context.AssessmentEditLogs.RemoveRange(allEditLogs);
}

// === UBAH line 2392 (audit description) ===
await _auditLog.LogAsync(
    dpgUser?.Id ?? "",
    dpgActorName,
    "DeletePrePostGroup",
    $"Deleted Pre-Post group '{groupTitle}' [LinkedGroupId={linkedGroupId}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount} EditLogsCount={preDeleteEditLogsCount}",
    linkedGroupId,
    "AssessmentSession");
```

### Playwright spec template

```typescript
// Source: pattern from tests/e2e/edit-peserta-answers.spec.ts (Phase 321)
// File: tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts

import { test, expect, type Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

// Env var dari seed setup (set sebelum run via shell export atau dotenv)
const SESSION_NO_EDITS = parseInt(process.env.P323_SESSION_NO_EDITS ?? '0', 10);
const SESSION_WITH_EDITS = parseInt(process.env.P323_SESSION_WITH_EDITS ?? '0', 10);
const SESSION_GROUP_REP = parseInt(process.env.P323_SESSION_GROUP_REP ?? '0', 10);

async function loginAny(page: Page, key: AccountKey) {
  const { email, password } = accounts[key];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}

test.describe('Phase 323 — Cascade AssessmentEditLogs Delete', () => {

  test.beforeAll(() => {
    if (!SESSION_NO_EDITS || !SESSION_WITH_EDITS || !SESSION_GROUP_REP) {
      throw new Error('Set P323_SESSION_NO_EDITS, P323_SESSION_WITH_EDITS, P323_SESSION_GROUP_REP env vars (see docs/SEED_JOURNAL.md Phase 323 entry).');
    }
  });

  test('a) Session no-edits → delete OK (no regression)', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/ManageAssessment');
    // ... click Delete button untuk SESSION_NO_EDITS row, confirm modal,
    //     assert TempData success banner muncul + row hilang dari listing
    // (selector spesifik: lihat existing tests/e2e/manage-assessment-filter.spec.ts)
  });

  test('b) Session 1+ edits → delete OK, EditLogs ikut terhapus', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/ManageAssessment');
    // ... click Delete untuk SESSION_WITH_EDITS,
    //     assert success banner (NOT "Gagal menghapus assessment"),
    //     navigate ke audit log atau AssessmentMonitoring untuk verify session hilang
  });

  test('c) Group campuran sibling no-edits + edits → delete OK', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/ManageAssessment');
    // ... click "Hapus Grup" untuk SESSION_GROUP_REP,
    //     assert success banner + semua sibling hilang
  });
});
```

### Seed SQL template (untuk Wave 0 setup)

```sql
-- Source: derived from docs/SEED_WORKFLOW.md §5 + Phase 313 seed pattern
-- Run via: sqlcmd -S "localhost\SQLEXPRESS" -E -d HcPortalDB_Dev -i seed-p323.sql

-- Step 1: Snapshot DB dulu (di luar SQL — manual command per SEED_WORKFLOW)
-- sqlcmd -S "localhost\SQLEXPRESS" -E -Q "BACKUP DATABASE HcPortalDB_Dev TO DISK='C:\Temp\HcPortalDB_Dev.pre323.bak' WITH INIT"

-- Step 2: Pilih 3 session target (manual verify dulu, lalu fix Id di env var):
SELECT TOP 5 s.Id, s.Title, s.Status, s.LinkedGroupId,
       (SELECT COUNT(*) FROM AssessmentEditLogs e WHERE e.AssessmentSessionId = s.Id) AS EditLogCount
FROM AssessmentSessions s
WHERE s.Status IN ('Completed', 'Open')
ORDER BY s.Id DESC;

-- Step 3: INSERT temporary edit log untuk SESSION_WITH_EDITS (skenario b)
-- Asumsi PackageQuestion FK valid; pilih 1 question dari session:
DECLARE @SessionId INT = <set manually from query above>;
DECLARE @QuestionId INT = (
  SELECT TOP 1 pq.Id
  FROM AssessmentPackages p
  JOIN PackageQuestions pq ON pq.AssessmentPackageId = p.Id
  WHERE p.AssessmentSessionId = @SessionId
);

INSERT INTO AssessmentEditLogs (
  AssessmentSessionId, PackageQuestionId,
  QuestionTextSnapshot, OldAnswerJson, OldAnswerTextSnapshot,
  NewAnswerJson, NewAnswerTextSnapshot,
  OldScore, NewScore, OldIsPassed, NewIsPassed,
  ActorUserId, ActorName, ActorRole,
  EditedAt, ReasonCode, ReasonText
) VALUES (
  @SessionId, @QuestionId,
  '[P323 SEED] Question snapshot temporary', '[]', '[P323 SEED] Old',
  '[]', '[P323 SEED] New',
  NULL, NULL, NULL, NULL,
  'p323-seed-user', '[P323 SEED] Seed User', 'Admin',
  GETUTCDATE(), 'BugSistem', '[P323 SEED] Temporary seed untuk smoke test'
);

-- Step 4: Catat di docs/SEED_JOURNAL.md (entry baru, status `active`)

-- Step 5: Run Playwright test
-- cd tests && P323_SESSION_NO_EDITS=<x> P323_SESSION_WITH_EDITS=<y> P323_SESSION_GROUP_REP=<z> npx playwright test Phase323

-- Step 6: RESTORE DB (cleanup)
-- sqlcmd -S "localhost\SQLEXPRESS" -E -Q "USE master; ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE HcPortalDB_Dev FROM DISK='C:\Temp\HcPortalDB_Dev.pre323.bak' WITH REPLACE; ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;"

-- Step 7: Update journal entry status `cleaned`
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hand-roll cascade DELETE SQL via ADO.NET | EF Core `RemoveRange` + `SaveChangesAsync` batching | EF Core 5+ (~2020) | EF Core 8 batches DELETE statement per type into single SQL roundtrip; transaction-safe by default. Existing pattern Phase 312 already uses this. |
| `DeleteBehavior.Cascade` (DB-level) | `DeleteBehavior.Restrict` + explicit endpoint `RemoveRange` | Phase 312+ design decision | Explicit cascade = audit count visible + opportunity to log per cascade tier. DB cascade silent. |

**Deprecated / outdated:** None applicable — pattern Phase 312 (2026-05-07) masih state-of-the-art untuk codebase ini.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `Description` field di `AuditLog` model tidak punya MaxLength constraint (cek L36-37 `[Required] public string Description`) — append `EditLogsCount={N}` aman tanpa truncation risk. | Audit log description format | [VERIFIED via Read tool 2026-05-26: `Models/AuditLog.cs:36-37` — `[Required] public string Description = ""`; tidak ada `[MaxLength]` attribute. Default EF Core string mapping = `nvarchar(max)`. SAFE — bukan assumption, verified.] |
| A2 | EF Core 8 `BeginTransactionAsync` reliably rollback saat `SaveChangesAsync` throw `DbUpdateException` (FK constraint violation). | Transaction scope wrap | [CITED: Microsoft EF Core 8 docs https://learn.microsoft.com/en-us/ef/core/saving/transactions — `using var tx` disposal auto-rollback if not committed. Pattern existing 3 endpoint sudah pakai sejak Phase 312 (2026-05-07), terbukti reliable di production Dev.] |
| A3 | `AssessmentEditLogs.PackageQuestionId` FK valid saat INSERT seed temporary (assume PackageQuestion existing yang belum dihapus). | Seed SQL template | LOW — verifiable via `SELECT TOP 1 ... FROM AssessmentPackages JOIN PackageQuestions` query. Plan task wajib include verifikasi query sebelum INSERT. |
| A4 | Selector tombol Delete di Razor view `ManageAssessment` (atau partial views post Phase 322) stable enough untuk Playwright `page.click('button[data-action="delete"]')` atau similar. | Playwright spec template | MEDIUM — selector belum di-verify untuk Phase 323. Plan Wave 0 task wajib include "inspect HTML manage-assessment partials, identify delete selectors". Existing test `tests/e2e/manage-assessment-filter.spec.ts` mungkin already reveals selectors. |
| A5 | DB lokal `localhost\SQLEXPRESS` instance + `HcPortalDB_Dev` database tersedia + Windows Integrated Security configured. | Seed workflow | [VERIFIED: `docs/SEED_WORKFLOW.md` §1 + `appsettings.Development.json`. SAFE.] |

## Open Questions (RESOLVED)

1. **Selektor exact untuk tombol Delete / Hapus Grup / Hapus Pre-Post di view ManageAssessment**
   - What we know: View existing di `Views/Admin/ManageAssessment*.cshtml` (post Phase 322 partial split). Tombol delete pasti ada di sub-partial.
   - What's unclear: CSS selector spesifik (button class, data attribute, form action) — belum di-probe untuk Phase 323.
   - **RESOLVED:** Defer ke Plan 02 Task 1 Sub-step A "Selector probe" — selector identified saat probe live partial view sebelum write spec body, dokumentasi sebagai comment header di spec file. Reference fallback: `tests/e2e/manage-assessment-filter.spec.ts` (Phase 322) untuk pattern navigasi ke halaman ini.

2. **Apakah perlu test untuk DeletePrePostGroup secara eksplisit (skenario 4)?**
   - What we know: 3 skenario CONTEXT.md cover (a)(b)(c) untuk single + group via Title-Schedule grouping. PrePostGroup variant (LinkedGroupId-based) tidak explicit di-test.
   - What's unclear: Risk of regression di endpoint ke-3 tanpa E2E coverage. Code review-only confirmation cukup?
   - **RESOLVED:** Tidak buat skenario ke-4 eksplisit. Code-review verification cukup karena pattern Plan 01 Task 3 (`groupIds.Contains` predicate di DeletePrePostGroup) struktural identik dengan Task 2 (`siblingIds.Contains` di DeleteAssessmentGroup) yang sudah di-cover skenario (c) E2E. Effort hemat tanpa kompromi coverage.

3. **Seed strategy — single AssessmentEditLog row cukup, atau perlu multiple untuk variasi `ReasonCode` / actor?**
   - What we know: REQ acceptance #3 hanya butuh "≥1 edit log" untuk verify cascade.
   - What's unclear: Apakah simulate "realistic edit history" (3-5 row dengan timestamp berbeda) tambah value coverage?
   - **RESOLVED:** 1-3 row cukup (1 untuk WITH_EDITS scenario + 1-2 untuk sibling GROUP_REP per Plan 02 Task 1 Sub-step C). Tidak perlu simulate multi-actor / multi-ReasonCode untuk acceptance CASCADE-01 #3. Hindari over-seed yang nambah cleanup risk.

4. **Apakah `DELETE FROM AssessmentSessions` cascade trigger SignalR broadcast (workerAnswerEdited dari Phase 321)?**
   - What we know: Phase 321 SignalR signal di-emit saat EDIT, bukan saat delete cascade.
   - What's unclear: Apakah ada broadcast lain (e.g., dashboard refresh) yang ter-trigger oleh delete cascade?
   - **RESOLVED:** Out-of-scope. SignalR `workerAnswerEdited` di-emit hanya di EDIT path Phase 321, bukan DELETE cascade. Tidak ada broadcast lain yang relevan untuk Phase 323 scope.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8.0 | `dotnet build` + `dotnet run` | [VERIFIED: HcPortal.csproj TargetFramework] | 8.0 | — |
| SQL Server Express (localhost\SQLEXPRESS) | Seed + restore | [CITED: docs/SEED_WORKFLOW.md §1, used Phase 313+315+321] | SQL Server 2022 Express (assumed via MSSQL17 backup path) | — |
| sqlcmd | BACKUP / RESTORE / INSERT seed | [CITED: docs/SEED_WORKFLOW.md §5.1-5.4] | bundled with SQL Server Express | — |
| Node.js + npm | Playwright test runner | [VERIFIED: tests/package.json npm scripts] | Compatible with Playwright 1.58 (Node 18+) | — |
| Playwright 1.58.2 | E2E spec runner | [VERIFIED: tests/package.json:17 + tests/playwright.config.ts] | 1.58.2 | — |
| Browser (Chromium) | Playwright project | [VERIFIED: playwright.config.ts:28-30 — `browserName: 'chromium'`] | Bundled with Playwright | — |
| Admin user `admin@pertamina.com / 123456` | Login fixture | [VERIFIED: tests/helpers/accounts.ts:2 + reference_dev_credentials.md memory] | Local seed DB | Memory note: kredensial UAT lokal; bukan staging/prod |

**Missing dependencies with no fallback:** None — semua tooling existing.

**Missing dependencies with fallback:** None.

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Playwright 1.58.2 + TypeScript 5.9.3 |
| Config file | `tests/playwright.config.ts` (testDir: `./e2e`, baseURL: `http://localhost:5277`) |
| Quick run command | `cd tests && npx playwright test Phase323 --headed` |
| Full suite command | `cd tests && npx playwright test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CASCADE-01 #1 (acceptance criteria 1: 3 endpoint tambah RemoveRange) | Source code presence | static code | grep `AssessmentEditLogs.RemoveRange\|_context.AssessmentEditLogs` count di `Controllers/AssessmentAdminController.cs` ≥3 | N/A (verify via grep / code review) |
| CASCADE-01 #2 (no regression for sessions with 0 edits) | E2E delete-success | e2e | `npx playwright test Phase323 --grep "no-edits"` | ❌ Wave 0 (create spec) |
| CASCADE-01 #3 (sessions with ≥1 edit) | E2E delete-success + EditLogs hilang | e2e | `npx playwright test Phase323 --grep "with-edits"` | ❌ Wave 0 |
| CASCADE-01 #4 (audit log tercatat normal) | Manual DB check post-test | manual + DB query | `SELECT TOP 5 * FROM AuditLogs WHERE ActionType LIKE 'Delete%' ORDER BY CreatedAt DESC` — verify `EditLogsCount=N` token present | manual UAT |
| CASCADE-01 #5 (transaction rollback bersih saat exception) | Integration test (simulate exception) | hard to automate; code review + manual | Inspect existing `using var tx + tx.RollbackAsync()` patterns intact di 3 endpoint post-patch | code review |
| CASCADE-01 #6 (smoke test 3 skenario) | E2E full suite | e2e | `npx playwright test Phase323` (3 tests pass) | ❌ Wave 0 |
| CASCADE-01 #7 (no schema/model/migration change) | Static verification | static | `git diff --stat Models/ Migrations/ Data/ApplicationDbContext.cs` — expect 0 changes | git diff |

### Sampling Rate

- **Per task commit:** `dotnet build` (must pass clean — 0 warning baru per CLAUDE.md Develop Workflow Step 4)
- **Per wave merge:** `npx playwright test Phase323` (3 tests green) — Wave 1 cascade patch, Wave 2 spec write
- **Phase gate:** Full suite `npx playwright test` regression no new failures + manual UAT 3 skenario via browser localhost (per CLAUDE.md Step 4 checklist)

### Wave 0 Gaps

- [ ] `tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts` — covers CASCADE-01 acceptance #2, #3, #6 (file does not exist yet — create in Wave 0 atau Wave 2 spec phase)
- [ ] `docs/SEED_JOURNAL.md` entry append — status `active` saat seed insert, `cleaned` setelah restore (manual gate per SEED_WORKFLOW)
- [ ] Probe selector tombol Delete + Hapus Grup di `Views/Admin/ManageAssessment*.cshtml` partials (Phase 322 post-split) — Open Question #1
- [ ] Identify 3 session ID seed (no-edits, with-edits, group-rep) via SELECT query (template di Code Examples seed SQL)

*(Test infrastructure framework + config + helpers `accounts.ts` + loginAny pattern + sqlcmd workflow semua sudah ada — only NEW spec file + seed setup needed.)*

## Security Domain

Phase 323 = bugfix scope kecil di endpoint already-authorized `[Authorize(Roles = "Admin, HC")]`. Threat model tidak berubah secara material — guard rail tetap sama dengan Phase 312.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes (no change) | ASP.NET Core Identity 8.0 — existing |
| V3 Session Management | yes (no change) | ASP.NET Core cookie auth — existing |
| V4 Access Control | yes (no change) | `[Authorize(Roles = "Admin, HC")]` attribute + `EnsureCanDeleteAsync` role-tier guard (Phase 312) — existing |
| V5 Input Validation | yes | `[ValidateAntiForgeryToken]` + integer `id` route binding — existing. EF parameterized queries inherent in `.Where(e => e.AssessmentSessionId == id)`. |
| V6 Cryptography | no | Phase 323 tidak menyentuh crypto path |

### Known Threat Patterns for ASP.NET Core MVC + EF Core 8 + Razor

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| SQL injection via cascade query | Tampering | EF Core parameterized LINQ (`.Where(e => e.AssessmentSessionId == id)`) — auto-parameterized, safe. No raw SQL added. |
| Audit description injection (special chars in title) | Tampering | `assessmentTitle` interpolated via `$"..."` C# string — no SQL/HTML context. Audit `Description` stored as plain text di `nvarchar(max)`. SAFE. |
| TOCTOU race (concurrent delete + edit) | Tampering | Existing `BeginTransactionAsync` + HC re-check `preDeleteResponseCount` guard (Phase 312 WR-01). Phase 323 adds EditLogs cascade DALAM scope yang sama — atomic. |
| CSRF on delete endpoint | Tampering | `[ValidateAntiForgeryToken]` existing di L2010, L2151, L2289 — no change. |
| Authorization bypass (Worker delete) | Elevation of Privilege | `[Authorize(Roles = "Admin, HC")]` attribute — no change. |

Phase 323 tidak introduce new attack surface. Risk: LOW.

## Sources

### Primary (HIGH confidence)

- `Controllers/AssessmentAdminController.cs:2007-2410` (Read tool 2026-05-26) — 3 endpoint full source code dengan cascade existing pattern, transaction scope, audit description format
- `Data/ApplicationDbContext.cs:235-246` (Read tool 2026-05-26) — `AssessmentEditLog` FK config explicit `OnDelete(DeleteBehavior.Restrict)` confirmed
- `Models/AssessmentEditLog.cs` (Read tool) — model fields: SessionId FK + PackageQuestionId + snapshot text fields + actor + reason
- `Models/AuditLog.cs:36-37` (Read tool) — Description field tidak punya MaxLength attribute — append safe
- `Services/AuditLogService.cs:21-42` (Read tool) — `LogAsync` signature `(actorUserId, actorName, actionType, description, targetId?, targetType?)` confirmed
- `tests/playwright.config.ts` (Read tool) — `baseURL: http://localhost:5277`, testDir `./e2e`, Playwright 1.58
- `tests/package.json` (Read tool) — `@playwright/test ^1.58.2` + `typescript ^5.9.3`
- `tests/helpers/accounts.ts` (Read tool) — `accounts.admin = admin@pertamina.com / 123456 / Admin`
- `tests/e2e/edit-peserta-answers.spec.ts` (Read tool) — Phase 321 spec pattern (loginAny + accounts fixture + env var session ID)
- `HcPortal.csproj` (Read tool) — .NET 8.0 + EF Core 8.0.0 confirmed
- `.planning/phases/323-*/323-CONTEXT.md` (Read tool) — locked decisions D-01..D-04
- `.planning/REQUIREMENTS.md` (Read tool) — CASCADE-01 acceptance criteria 1-7
- `.planning/STATE.md` (Read tool) — repro evidence Session Id 1/2/5
- `.planning/ROADMAP.md` (Read tool) — v18.0 milestone scope
- `CLAUDE.md` (Read tool) — Develop Workflow + Seed Data Workflow
- `docs/SEED_WORKFLOW.md` (Read tool) — sqlcmd BACKUP/RESTORE template, journal format
- `docs/DEV_WORKFLOW.md` (Read tool) — Lokal → Dev → Prod environment map, pre-commit checklist
- `docs/SEED_JOURNAL.md` (Read tool first 110 lines) — pattern entry append + cleaned status

### Secondary (MEDIUM confidence)

- Microsoft Learn EF Core 8 docs (knowledge) — `DeleteBehavior.Restrict` semantics + `BeginTransactionAsync` rollback contract (cross-verified dengan pola Phase 312 existing yang sudah production-tested)

### Tertiary (LOW confidence)

- None — semua claim verified via Read tool atau cited dari docs di-repo.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua dependency verified di csproj + package.json, pattern existing Phase 312 di file yang sama
- Architecture: HIGH — 3 endpoint source code direct-inspected, FK config direct-inspected, pattern Phase 312 100% reusable
- Pitfalls: HIGH — pitfall #1 (snapshot timing) #2 (block order) sudah confirmed via existing Phase 312 code yang ikut pattern; pitfall #3 (lupa cabang) historical pattern di codebase
- Smoke test: MEDIUM — selector exact untuk delete button belum di-probe (Open Question #1); seed pattern terbukti dari Phase 313 + 321

**Research date:** 2026-05-26
**Valid until:** 2026-06-25 (30 days — codebase stable, no major refactor expected)
