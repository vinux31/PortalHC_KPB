# Phase 324: Fix duplicate TrainingRecord auto-create on assessment completion — Research

**Researched:** 2026-05-26
**Domain:** ASP.NET Core 8 MVC + EF Core + SQL Server bug fix (regression removal + data cleanup)
**Confidence:** HIGH (target code, call graph, dan cleanup pattern semua verified via `Read` + `Grep` di codebase lokal)

## Summary

Phase 324 menghapus mekanisme auto-create `TrainingRecord` saat session assessment completed (path normal Submit + path FinalizeEssayGrading + path RegradeAfterEdit Pass↔Fail flip). Akar masalah: `WorkerDataService.GetUnifiedRecords` melakukan `UNION` antara `AssessmentSessions Status="Completed"` (RecordType="Assessment Online") dengan `TrainingRecords` (RecordType="Training Manual") tanpa dedup — sehingga setiap auto-generated TR muncul side-by-side dengan source AssessmentSession-nya di `/CMP/Records`. Auto-create TR pernah dihapus `79284609` (2026-03-18) tepat karena alasan ini; commit `766011b6` (2026-04-10) re-introduce-nya dengan dead-code `try-catch DbUpdateException` guard (tidak ada unique index di tabel — verifikasi `ApplicationDbContext.cs:143-170`).

**Cross-grep audit (HIGH confidence):** hanya 4 lokasi `_context.TrainingRecords.Add(...)` di production code: 3 di scope phase (GradingService:268, GradingService:545, AssessmentAdminController:3410) + 4 di `TrainingAdminController` (admin manual add — out of scope). Tidak ada hidden path lain.

**Primary recommendation:** 3-wave sequencing — Wave 1 surgical code edit di 3 lokasi (single commit per file, atomic, opsi parallel di Wave 1 karena 2 file berbeda) → Wave 2 Playwright UAT automated 7-skenario → Wave 3 SQL cleanup script + HTML handoff IT. Lokal cleanup lewat SEED_WORKFLOW BACKUP/RESTORE (bukan delete langsung). Dev/Prod cleanup via IT pakai HTML handoff template `2026-05-13.html` adaptasi `2026-05-26.html`.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Code Changes**
- **D-01:** Hapus block `_context.TrainingRecords.Add(...)` di `Services/GradingService.cs:255-285` (path `GradeAndCompleteAsync`, normal submit flow)
- **D-02:** Hapus block `_context.TrainingRecords.Add(...)` di `Controllers/AssessmentAdminController.cs:3404-3421` (path `FinalizeEssayGrading`, manual essay grading)
- **D-03:** Hapus cascade `TrainingRecord` update/insert di `Services/GradingService.cs:483-562` (path `RegradeAfterEditAsync`, Pass↔Fail flip). `AssessmentSession.IsPassed` + `NomorSertifikat` update tetap; cascade ke `TrainingRecord` seluruhnya dihapus. Records page baca status terbaru dari `AssessmentSession`.

**Data Cleanup**
- **D-04:** Cleanup scope = `WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10'`. Filter tanggal untuk hindari hapus row legitimate yang admin pernah ketik manual dengan pattern sama pra-bug.
- **D-05:** Lokal: backup DB lokal via `sqlcmd ... BACKUP DATABASE` SEBELUM eksekusi cleanup script (per `docs/SEED_WORKFLOW.md` mandatory). Catat di `docs/SEED_JOURNAL.md` sebagai temporary classification.
- **D-06:** Dev/Prod: tidak touch langsung. Buat `docs/DB_HANDOFF_IT_2026-05-26.html` dengan template + style mengikuti `docs/DB_HANDOFF_IT_2026-05-13.html` (Pertamina branding). Isi: commit hash, SQL cleanup script, prerequisite backup, verification query, rollback plan. IT yang eksekusi.

**Testing**
- **D-07:** Playwright UAT automated mengikuti pattern Phase 322. Spec coverage minimum:
  - Worker submit assessment biasa (non-essay) → assert `/CMP/Records` hanya tampil 1 row "Assessment Online" (bukan 2)
  - PreTest tetap skip TR (regression guard existing behavior)
  - Essay flow finalize → assert tidak insert TR
  - HC `AkhiriUjian` (force-end single) → assert grading tetap jalan, tidak insert TR
  - HC `AkhiriSemuaUjian` (bulk) → assert grading tetap jalan untuk semua, tidak insert TR
  - HC `RegradeAfterEdit` Pass→Fail flip → `AssessmentSession.IsPassed` update, tidak ada TR cascade
  - HC `RegradeAfterEdit` Fail→Pass flip → sertifikat generate via `AssessmentSession.NomorSertifikat`, tidak ada TR cascade

**Verification**
- **D-08:** Pre-fix repro lokal: bikin assessment baru, submit sebagai worker, buka `/CMP/Records`, capture screenshot 2-row state (proof of bug).
- **D-09:** Post-fix verify lokal: ulang flow, capture screenshot 1-row state (proof of fix).
- **D-10:** SQL verify count: `SELECT COUNT(*) FROM TrainingRecords WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10';` sebelum dan sesudah cleanup.

### Claude's Discretion
- Naming Playwright spec file + folder structure (ikut convention existing `tests/e2e/`)
- SQL script file naming + folder (saran: `docs/sql/cleanup-2026-05-26-trainingrecord-duplicates.sql`)
- HTML handoff content structure (selama follow template 2026-05-13)
- Logger statement format saat hapus block (kalau ada log yang relevan untuk audit removal)

### Deferred Ideas (OUT OF SCOPE)
- Tambah unique index `(UserId, Judul, Tanggal)` di TrainingRecord — tidak diperlukan setelah auto-create dihapus. Phase masa depan defensive measure jika Excel import generate duplicate.
- Refactor `GetUnifiedRecords` query — kalau di masa depan ada perubahan source-of-truth (misal pindah ke materialized view), boleh refactor. Tidak Phase 324.
- Audit TR legacy dengan Judul similar pattern tapi admin manual — kalau ternyata ada admin yang ketik manual "Assessment: ..." pre-bug, audit terpisah.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| (TBD) | Phase belum mapped ke REQ-ID di `REQUIREMENTS.md` — milestone v18.0 saat ini hanya CASCADE-01 (Phase 323). Planner harus derive REQ-ID baru (saran: `DUPE-01` atau `TR-DUPE-01`) di Plan 01 dan update `REQUIREMENTS.md` table traceability. | Code change scope: 3 lokasi insert/cascade TR (D-01..D-03). Display invariant: `/CMP/Records` 1 row per assessment completion (D-09). Data hygiene: cleanup legacy TR Judul LIKE 'Assessment:%' (D-04..D-06, D-10). |

**Recommendation:** Planner buat REQ baru `DUPE-01: Worker melihat 1 row "Assessment Online" per event submit di /CMP/Records (bukan 2)` dan tambahkan ke `.planning/REQUIREMENTS.md` di Plan 01 atau lebih awal saat planning.
</phase_requirements>

## Project Constraints (from CLAUDE.md)

| Constraint | Source | Compliance Plan |
|------------|--------|------------------|
| Respons Bahasa Indonesia | `CLAUDE.md` | RESEARCH/PLAN/SUMMARY/UAT dokumen pakai Bahasa Indonesia, code comments tetap teknis (bisa EN bila perlu) |
| Lokal → Dev → Prod workflow | `docs/DEV_WORKFLOW.md` | Verifikasi lokal (build+run+browser) sebelum push. NO push langsung. HTML handoff IT untuk Dev/Prod cleanup. |
| Tidak edit DB Dev/Prod langsung | `CLAUDE.md` §Develop Workflow | Cleanup data Dev/Prod hanya via IT (HTML handoff). Lokal cleanup via SEED_WORKFLOW BACKUP/RESTORE. |
| EF migration wajib untuk schema change | `DEV_WORKFLOW.md` §4 | Phase 324 TIDAK ubah schema (NO migration). Hanya code edit + data cleanup. Konfirmasi di IT_NOTIFY. |
| SEED_WORKFLOW classify + journal sebelum seed | `docs/SEED_WORKFLOW.md` | Saat repro lokal D-08 + cleanup data lokal D-05: classify `temporary + local-only`, snapshot DB ke `.bak`, log di `SEED_JOURNAL.md` (active → cleaned). |
| Atomic commit per task | konvensi gsd-executor | Convention existing: `feat(324-XX): <desc>` per task |

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Grading & status update (Score, IsPassed, Status=Completed) | Service (GradingService) | — | Existing single source of truth (D-71 Phase 296). TIDAK diubah. |
| Generate NomorSertifikat (cert number) | Service (GradingService) | — | Tetap di GradingService (D-71). TIDAK diubah Phase 324. |
| Generate `TrainingRecord` auto-copy | (REMOVED) | — | **Hilang di Phase 324.** Auto-copy redundant terhadap AssessmentSession sebagai source-of-truth. |
| Cascade certificate revoke saat Pass→Fail flip | Service (GradingService.RegradeAfterEdit) | — | `NomorSertifikat = null` + `ValidUntil = null` tetap (Phase 321 EDIT-04). HANYA cascade ke TR dihapus. |
| Display unified records page | View (`/CMP/Records`) backed by `WorkerDataService.GetUnifiedRecords` | Backend (CMP/CDP/AdminBaseController) | UNION dari `AssessmentSessions.Where(Status="Completed")` + `TrainingRecords`. AssessmentSession branch jadi single source untuk row "Assessment Online". |
| Data cleanup legacy TR | Database (SQL script) | IT operations (Dev/Prod) | Lokal via SEED_WORKFLOW. Dev/Prod via IT handoff HTML. Developer JANGAN edit DB Dev langsung. |
| Regression validation | Test (Playwright e2e) + manual UAT browser | Build (`dotnet build`) + DB query (D-10 count) | 7-skenario coverage E2E + screenshot pre/post repro. |

## Standard Stack

### Core (sudah established, tidak ada install baru)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.x | Controller layer (AssessmentAdminController, CMPController) | Existing stack [VERIFIED: project] |
| Entity Framework Core | 8.x | ORM (`_context.TrainingRecords`, `ExecuteUpdateAsync`, `ExecuteDeleteAsync`) | Existing stack [VERIFIED: project] |
| SQL Server (lokal SQLEXPRESS) | 2017+ | DB `HcPortalDB_Dev` | `docs/SEED_WORKFLOW.md` §1 [VERIFIED: docs] |
| Playwright TypeScript | latest project version | E2E test framework di `tests/e2e/*.spec.ts` | Existing `tests/playwright.config.ts` [VERIFIED: file] |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `@playwright/test` | (pinned dalam tests/package.json) | Test runner + fixtures + assertions | Wave 2 UAT 7-skenario |
| `sqlcmd` (mssql-tools) | system-installed | BACKUP/RESTORE DB lokal | D-05 pre/post repro |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Hapus block insert TR | Set `TrainingRecord.Status = "Suppressed"` (mark inactive) | Lebih kompleks + UI tetap harus filter — auto-create tetap menulis row useless. Decision: clean delete (per D-01..D-03). |
| Tambah unique index (UserId, Judul, Tanggal) | Tetap auto-create tapi prevent dup via DB constraint | Tidak menyelesaikan visual duplicate (UNION tetap 2 row). Defer (Deferred Ideas). |
| Soft-delete TR auto (set IsDeleted flag) | Tambah column + filter di GetUnifiedRecords | Schema migration + extra query branch — over-engineer untuk pure code removal. |

**Installation:** Tidak ada install paket baru. Hanya code edit + SQL script.

**Version verification (skip):** Tidak ada package baru yang perlu di-verify versinya.

## Architecture Patterns

### System Data Flow (Submit → Grading → Display)

```
Worker browser
    │ (1) POST /CMP/SubmitExam
    ▼
CMPController.SubmitExam (line 1727)
    │ persist PackageUserResponses
    │ (2) invoke GradingService
    ▼
GradingService.GradeAndCompleteAsync
    │ compute Score + IsPassed (status guard via ExecuteUpdateAsync WHERE Status!="Completed")
    │ insert SessionElemenTeknisScores
    │ ┌─────────────────────────────────────────────┐
    │ │ DELETED (D-01): insert TrainingRecord copy  │
    │ │ block line 255-285 dihapus                  │
    │ └─────────────────────────────────────────────┘
    │ generate NomorSertifikat (jika GenerateCertificate && isPassed)
    │ invoke WorkerDataService.NotifyIfGroupCompleted
    ▼
DB
    │
    │ AssessmentSession (Status=Completed, Score, IsPassed, NomorSertifikat)
    │
    │ (3) Worker visit /CMP/Records
    ▼
CMPController.Records → WorkerDataService.GetUnifiedRecords
    │ Query 1: AssessmentSessions WHERE UserId=X AND Status="Completed"
    │           → 1 UnifiedTrainingRecord per session (RecordType="Assessment Online")
    │ Query 2: TrainingRecords WHERE UserId=X
    │           → 1 per row TR (RecordType="Training Manual")
    │ UNION + OrderByDescending(Date)
    ▼
Views/CMP/Records.cshtml
    │
    │ ✅ Post-fix: 1 row per assessment event
    │ ❌ Pre-fix: 2 row per assessment event (1 dari AssessmentSession + 1 dari TR copy)
    ▼
Worker melihat halaman Records
```

### Edit Path (RegradeAfterEdit Flip)

```
Admin/HC edit MC/MA jawaban worker
    │ POST /Admin/EditPesertaAnswers/SubmitEditAnswers (Phase 321)
    ▼
GradingService.RegradeAfterEditAsync
    │ DELETE SessionElemenTeknisScores existing
    │ recompute (oldScore, oldIsPassed, newScore, newIsPassed)
    │ INSERT SessionElemenTeknisScores baru
    │ ExecuteUpdateAsync session (Score, IsPassed, UpdatedAt)
    │
    │ IF Pass → Fail:
    │   AssessmentSessions.NomorSertifikat = null + ValidUntil = null   ← TETAP (D-03)
    │   ┌─────────────────────────────────────────────────────┐
    │   │ DELETED (D-03): TrainingRecords SetProperty Status   │
    │   │                = "Failed" block (line 495-498)       │
    │   └─────────────────────────────────────────────────────┘
    │
    │ IF Fail → Pass:
    │   Generate NomorSertifikat baru (retry 3x)             ← TETAP (D-03)
    │   ┌─────────────────────────────────────────────────────┐
    │   │ DELETED (D-03): TrainingRecord insert/update         │
    │   │                 ("Passed") block (line 541-561)      │
    │   └─────────────────────────────────────────────────────┘
    ▼
DB updated (AssessmentSession sole source of truth)
    │
    ▼
SignalR push workerAnswerEdited → HC monitor + worker /CMP/Records re-render shows new IsPassed/Score
```

### Pattern 1: Surgical Block Removal (Atomic Commit)

**What:** Hapus block kode terisolasi tanpa refactor logic surrounding.
**When to use:** Bug fix yang mensubtraksi behavior (bukan menambah).
**Example:**
```csharp
// Sebelum (GradingService.cs:255-285)
var judul = $"Assessment: {session.Title}";
bool trainingRecordExists = await _context.TrainingRecords.AnyAsync(t =>
    t.UserId == session.UserId &&
    t.Judul == judul &&
    t.Tanggal == session.Schedule);

if (!trainingRecordExists && session.AssessmentType != "PreTest")
{
    try
    {
        _context.TrainingRecords.Add(new TrainingRecord { ... });
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        _logger.LogWarning("Duplicate TrainingRecord detected for UserId={UserId}...");
    }
}

// Sesudah (D-01 applied)
// (block dihapus seluruhnya — komentar 1 baris boleh: "Phase 324 D-01: TR auto-create removed; AssessmentSession is sole source for Records page")
```

### Pattern 2: SEED_WORKFLOW Lifecycle (Lokal D-05)

**What:** BACKUP → modify → RESTORE pattern untuk operasi DB lokal yang reversible.
**When to use:** Setiap kali insert seed temporary atau jalankan SQL DML reversible-required.
**Reference:** `docs/SEED_WORKFLOW.md` §5.

```bash
# Pre-cleanup snapshot
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "BACKUP DATABASE HcPortalDB_Dev TO DISK='C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\HcPortalDB_Dev-pre324-cleanup-2026-05-26.bak' WITH INIT"

# Apply cleanup
sqlcmd -S "localhost\SQLEXPRESS" -E -d HcPortalDB_Dev -i "docs/sql/cleanup-2026-05-26-trainingrecord-duplicates.sql"

# Verify count
sqlcmd -S "localhost\SQLEXPRESS" -E -d HcPortalDB_Dev -Q "SELECT COUNT(*) FROM TrainingRecords WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10';"

# Restore kalau perlu rollback
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "USE master; ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE HcPortalDB_Dev FROM DISK='C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\HcPortalDB_Dev-pre324-cleanup-2026-05-26.bak' WITH REPLACE; ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;"
```

### Pattern 3: SQL Cleanup Script — Transaction + Count Guard

**Structure** (saran untuk `docs/sql/cleanup-2026-05-26-trainingrecord-duplicates.sql`):

```sql
USE HcPortalDB_Dev;  -- atau HcPortalDB di Prod (IT yang execute)
GO

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION cleanup324;

    -- 1. Pre-count (informasi)
    DECLARE @preCount INT;
    SELECT @preCount = COUNT(*)
    FROM TrainingRecords
    WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10';
    PRINT CONCAT('Pre-cleanup count: ', @preCount);

    -- 2. Safety cap: kalau >5000 row, abort (kemungkinan filter terlalu lebar).
    --    Angka 5000 = adjust setelah verify di lokal/Dev. Tujuan: prevent runaway delete.
    IF @preCount > 5000
    BEGIN
        ROLLBACK TRANSACTION cleanup324;
        RAISERROR('Safety cap exceeded (%d > 5000). Investigate before bumping.', 16, 1, @preCount);
        RETURN;
    END

    -- 3. Soft pre-check: sample 5 row untuk visual sanity (komentar saja di handoff,
    --    IT bisa pakai sebelum run)
    -- SELECT TOP 5 Id, UserId, Judul, Kategori, Tanggal, CreatedAt, Status
    -- FROM TrainingRecords
    -- WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10'
    -- ORDER BY CreatedAt DESC;

    -- 4. Cleanup
    DELETE FROM TrainingRecords
    WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10';

    DECLARE @deletedCount INT = @@ROWCOUNT;
    PRINT CONCAT('Deleted: ', @deletedCount);

    -- 5. Post-count verify
    DECLARE @postCount INT;
    SELECT @postCount = COUNT(*)
    FROM TrainingRecords
    WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10';
    PRINT CONCAT('Post-cleanup count: ', @postCount);

    IF @postCount > 0
    BEGIN
        ROLLBACK TRANSACTION cleanup324;
        RAISERROR('Post-count not zero (%d). Rolled back.', 16, 1, @postCount);
        RETURN;
    END

    COMMIT TRANSACTION cleanup324;
    PRINT 'Cleanup committed successfully.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION cleanup324;
    DECLARE @errMsg NVARCHAR(4000) = ERROR_MESSAGE();
    PRINT CONCAT('Cleanup failed: ', @errMsg);
    THROW;
END CATCH;
GO
```

Key elements:
- `SET XACT_ABORT ON` — abort transaction on any runtime error
- Transaction-wrapped — atomic, rollback-safe
- Safety cap (5000 row) — guard against runaway query
- Pre/post count — verify scope sebelum + sesudah
- Sample query commented — IT bisa toggle untuk visual sanity
- `THROW` di catch — propagate error keluar untuk visibility

### Anti-Patterns to Avoid

- **Hapus cuma `Add(...)` line tapi biarkan variable surrounding (`judul`, `trainingRecordExists`):** Akan jadi dead variable, compiler warning. Hapus blok seluruhnya termasuk variable yang khusus dipakai untuk insert TR.
- **Edit DB Dev/Prod langsung lewat SSMS/Azure Data Studio:** Melanggar `CLAUDE.md` §Golden Rules. Selalu via HTML handoff IT.
- **Apply SQL cleanup tanpa BACKUP terlebih dahulu:** Bahkan di lokal. Per SEED_WORKFLOW §2 mandatory.
- **Hapus block tapi tetap call `await _context.SaveChangesAsync()` di line yang ikut ke-delete:** Hilang call SaveChanges bisa berdampak ke ET scores commit. Verifikasi: `GradeAndCompleteAsync` sudah call `SaveChangesAsync` di line 180 (sebelum block TR) — block TR pasang `SaveChangesAsync`-nya sendiri yang akan ikut dihapus. OK.
- **Generate ulang sertifikat di RegradeAfterEdit setelah hapus TR insert:** Logic generate cert (line 506-538) wajib TETAP. Yang dihapus HANYA TR insert/update line 540-561 dan TR update Status="Failed" line 495-498.
- **Cleanup pakai `>= '2026-04-10'` filter tanpa juga filter `< '2026-XX-XX'` (current date):** Pertimbangkan apakah cutoff atas perlu — kalau ada lag waktu antara code deploy fix dan cleanup, ada window dimana code masih insert TR baru. Saran: planner pertimbangkan cleanup dijalankan SETELAH code deploy ke Dev (urutan: code Dev first, lalu cleanup data, supaya tidak race dengan worker yang lagi submit).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| DB backup/restore wrapper | Custom PowerShell BACKUP script | `sqlcmd ... BACKUP DATABASE` per `SEED_WORKFLOW.md` §5.1 | Pattern sudah established + battle-tested di Phase 313/315/317 |
| Transactional SQL cleanup | Multiple ad-hoc DELETE statements | Single transaction-wrapped script with pre/post count + safety cap | Atomicity + rollback + audit trail |
| Pertamina-branded HTML handoff | Bangun template HTML dari nol | Copy + adapt `docs/DB_HANDOFF_IT_2026-05-13.html` (verbatim CSS variables + structure) | Konsisten dengan precedent IT sudah biasa baca |
| Playwright login helper | Buat ulang loginAny pattern | Reuse `tests/e2e/manage-assessment-filter.spec.ts` + `tests/e2e/edit-peserta-answers.spec.ts` pattern (`loginAny` + `accounts` fixture) | Pattern reusable; existing Phase 322/321 menggunakan pola identik |
| Count + DB verify dalam Playwright | Bangun DB client di JS | Pakai `sqlcmd` shell out via Node `child_process.exec` ATAU manual SQL verify post-test | Phase 313/315 sudah pakai pola execScript via globalTeardown |

**Key insight:** Semua tooling sudah ada. Phase 324 = subtract + cleanup, bukan build new infrastructure.

## Runtime State Inventory

> Phase 324 = rename/refactor/cleanup phase (data removal). Wajib inventory.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| **Stored data** | `TrainingRecords` rows WHERE `Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10'` — auto-generated dari Phase 153-04 ↩️ Phase 296 hingga regression `766011b6` (2026-04-10) sampai phase 324 ship | Cleanup via SQL script di lokal (D-05) + Dev/Prod (D-06 IT handoff). Code edit + data cleanup keduanya wajib. |
| **Live service config** | Tidak ada — TR copy hanya konsumsi internal app, tidak ter-broadcast ke service eksternal (Datadog/Tailscale/etc N/A — proyek single-tenant ASP.NET). | None — verified by scan: tidak ada `RecordType="Training Manual"` reference di config eksternal. |
| **OS-registered state** | Tidak ada — TR row internal DB, tidak ter-register di OS layer (Task Scheduler / pm2 / systemd / cron). | None — verified by absence of OS registration in CLAUDE.md / docs. |
| **Secrets/env vars** | Tidak ada secret name terkait `TrainingRecord` atau `Assessment:` prefix. Connection string (di `appsettings.Development.json`) tetap. | None. |
| **Build artifacts / installed packages** | Tidak ada package baru. `dotnet build` akan recompile `GradingService.dll` + `HcPortal.dll`; tidak ada artifact eksternal yang carry old code. EF Migration unchanged (NO migration in this phase). | Re-build + restart Kestrel/IIS lokal saat verify D-09. IT akan rebuild saat promo Dev/Prod. |

**Critical migration note:** Cleanup TIDAK menghapus AssessmentSession atau dependent — hanya TR copy. AssessmentSession status="Completed" + Score + IsPassed + NomorSertifikat semua tetap utuh sebagai source-of-truth display di Records page.

## Common Pitfalls

### Pitfall 1: Hapus block ETScore SaveChangesAsync ikut terhapus

**What goes wrong:** Block TR insert (line 255-285) didahului oleh `SaveChangesAsync` line 180 (ET scores commit). Kalau planner salah ambil scope hapus, bisa ikut hilang.
**Why it happens:** Lines 250-260 ada whitespace + 1 comment di tengah — easy to over-select.
**How to avoid:** Hapus PRECISELY lines 255-285 (block `var judul = ...` sampai akhir `}` catch). Comment line 254 (`// ---- 5. Buat TrainingRecord ...`) ikut dihapus. Lines 251-253 (`UserPackageAssignments.Where(...).ExecuteUpdateAsync(...)`) HARUS TETAP karena commit `IsCompleted=true`.
**Warning signs:** `dotnet build` warning "ET scores not committed" tidak akan muncul (compiler tidak tahu semantic). Manual code review wajib. Saran Plan: tampilkan diff lengkap di Plan task untuk verifikasi human.

### Pitfall 2: PreTest skip behavior accidentally broken

**What goes wrong:** Existing logic line 264 `if (!trainingRecordExists && session.AssessmentType != "PreTest")` — gate ini ada karena PreTest "tidak count sebagai training" (ISS-03 fix Phase 153). Setelah block dihapus, PreTest behavior tetap = tidak ada TR. Sesuai harapan.
**Why it happens:** Tidak ada — ini PASS-THROUGH. Tapi planner mungkin salah interpret bahwa gate `AssessmentType != "PreTest"` perlu dimigrasi ke gate baru.
**How to avoid:** Tegaskan di Plan: "Tidak ada gate baru. Semua flow (Online/PreTest/PostTest/Mixed/Essay) seragam tidak insert TR auto."
**Warning signs:** Plan task yang menambah `if (session.AssessmentType == "PreTest") return;` di awal — itu salah, tidak perlu.

### Pitfall 3: RegradeAfterEditAsync Pass→Fail tidak revoke sertifikat

**What goes wrong:** Block line 487-498 (Pass→Fail) berisi 2 hal: (a) revoke NomorSertifikat + ValidUntil — WAJIB TETAP, (b) update TR Status="Failed" — DIHAPUS (D-03). Kalau planner over-delete, sertifikat tidak ke-revoke.
**Why it happens:** 2 statement back-to-back dalam `if (wasPassed && !isPassed)` branch.
**How to avoid:** Plan task EXPLICITLY tunjukkan diff:
- KEEP: `await _context.AssessmentSessions.Where(s => s.Id == session.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, (string?)null).SetProperty(r => r.ValidUntil, (DateTime?)null));`
- DELETE: `var judul = $"Assessment: {session.Title}"; await _context.TrainingRecords.Where(...).ExecuteUpdateAsync(t => t.SetProperty(r => r.Status, "Failed"));`
- KEEP: `_logger.LogInformation(...)` — adjust pesan log (TR=Failed reference hapus).
**Warning signs:** UAT skenario "RegradeAfterEdit Pass→Fail" expects `AssessmentSessions.NomorSertifikat == null` setelah flip. Kalau fail = revoke logic ikut ke-hapus.

### Pitfall 4: RegradeAfterEditAsync Fail→Pass tidak generate sertifikat

**What goes wrong:** Block line 506-561 (Fail→Pass) berisi: (a) generate NomorSertifikat retry 3x — WAJIB TETAP, (b) insert/update TR Status="Passed" — DIHAPUS (D-03).
**Why it happens:** Sama dengan Pitfall 3 — 2 logic back-to-back dalam 1 branch.
**How to avoid:** Plan task EXPLICITLY tunjukkan range delete = line 540-561 (TR block). KEEP line 506-538 (cert generate retry loop).
**Warning signs:** UAT skenario "RegradeAfterEdit Fail→Pass" expects `AssessmentSessions.NomorSertifikat != null` (if `GenerateCertificate && !PreTest`). Kalau fail = cert generate ikut hapus.

### Pitfall 5: SQL cleanup race condition saat deploy belum complete di Dev

**What goes wrong:** Sequencing IT — kalau IT jalanin SQL cleanup DULUAN sebelum deploy code DLL baru, worker yang submit di tengah cleanup akan tetap generate TR baru (code lama masih jalan). Window race kecil tapi nyata.
**Why it happens:** Promo IT biasa: pull code → restart pool → apply migration. SQL cleanup adalah step terpisah.
**How to avoid:** HTML handoff EXPLICIT mention ordering: "Step 1: pull + build + restart (deploy code first). Step 2: SQL cleanup (data cleanup after code stops generating new TR)." Tambahkan callout warning di handoff.
**Warning signs:** Post-cleanup count > 0 → entry baru di-insert di window race. Mitigation: re-run cleanup script (idempotent).

### Pitfall 6: Cleanup filter tertangkap row legitimate admin manual

**What goes wrong:** Asumsi `Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10'` hanya match auto-generated. Kalau admin pernah manual add TR dengan judul "Assessment: Foo" via `TrainingAdminController` setelah 2026-04-10, akan ikut ter-delete.
**Why it happens:** Pattern judul tidak unik (admin-input string).
**How to avoid:** Sebelum cleanup di Dev/Prod, IT (atau dev) jalankan pre-check sample query:
```sql
SELECT TOP 20 Id, UserId, Judul, Kategori, Tanggal, CreatedAt, Penyelenggara, Status
FROM TrainingRecords
WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10'
ORDER BY CreatedAt;
```
Visual cek: kalau Penyelenggara konsisten "Internal" + Kategori match `session.Category` patterns + Status `Passed/Failed` (bukan `Valid/Expired`), itu auto. Kalau ada anomali (Penyelenggara berbeda, Kategori asing), STOP dan investigasi.
**Warning signs:** Sample query menampilkan row dengan Penyelenggara ≠ "Internal" — kemungkinan manual entry. Update filter atau exclude by ID.

### Pitfall 7: Plan checker/verifier perlu re-run setelah code edit untuk confirm zero `TrainingRecords.Add` di GradingService/AssessmentAdminController

**What goes wrong:** Lupa final grep audit setelah edit selesai.
**Why it happens:** Phase 324 = subtract phase. Easy to forget verification.
**How to avoid:** Plan terakhir (Verification plan or last task) include:
```bash
# Expected: only TrainingAdminController matches (admin manual add — out of scope)
grep -rn "TrainingRecords\.(Add|AddAsync|AddRange)" Services/ Controllers/AssessmentAdminController.cs Controllers/CMPController.cs
```
**Warning signs:** Grep returns hits di GradingService or AssessmentAdminController.

## Code Examples

### Diff Spec — D-01 (GradingService.cs:255-285)

**Before:**
```csharp
            // ---- 5. Buat TrainingRecord (dengan duplicate guard) ----
            // PPT SC6 (ISS-03 fix): TrainingRecord HANYA dari Post-Test, bukan Pre-Test.
            // Sertifikat sudah guarded via session.GenerateCertificate=false saat Pre-Test create.
            var judul = $"Assessment: {session.Title}";
            bool trainingRecordExists = await _context.TrainingRecords.AnyAsync(t =>
                t.UserId == session.UserId &&
                t.Judul == judul &&
                t.Tanggal == session.Schedule);

            if (!trainingRecordExists && session.AssessmentType != "PreTest")
            {
                try
                {
                    _context.TrainingRecords.Add(new TrainingRecord
                    {
                        UserId = session.UserId,
                        Judul = judul,
                        Kategori = session.Category ?? "Assessment",
                        Tanggal = session.Schedule,
                        TanggalSelesai = DateTime.UtcNow,
                        Penyelenggara = "Internal",
                        Status = isPassed ? "Passed" : "Failed"
                    });
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    _logger.LogWarning("Duplicate TrainingRecord detected for UserId={UserId}, Judul={Judul}, Tanggal={Tanggal}. Skipping insert.",
                        session.UserId, judul, session.Schedule);
                }
            }
```

**After:**
```csharp
            // Phase 324 D-01: TrainingRecord auto-create removed.
            // AssessmentSession (Status=Completed) is sole source for "Assessment Online" row
            // di /CMP/Records via WorkerDataService.GetUnifiedRecords. Re-introduce hanya jika
            // ada perubahan source-of-truth display. Lihat .planning/phases/324-*/324-CONTEXT.md.
```

### Diff Spec — D-02 (AssessmentAdminController.cs:3404-3421)

**Before:**
```csharp
            // 4. Generate TrainingRecord (duplicate guard — same as GradingService)
            var judul = $"Assessment: {session.Title}";
            bool trExists = await _context.TrainingRecords.AnyAsync(t =>
                t.UserId == session.UserId && t.Judul == judul && t.Tanggal == session.Schedule);
            if (!trExists)
            {
                _context.TrainingRecords.Add(new TrainingRecord
                {
                    UserId = session.UserId,
                    Judul = judul,
                    Kategori = session.Category ?? "Assessment",
                    Tanggal = session.Schedule,
                    TanggalSelesai = DateTime.UtcNow,
                    Penyelenggara = "Internal",
                    Status = isPassed ? "Passed" : "Failed"
                });
                await _context.SaveChangesAsync();
            }
```

**After:**
```csharp
            // Phase 324 D-02: TrainingRecord auto-create removed dari FinalizeEssayGrading path.
            // Konsisten dengan GradingService.GradeAndCompleteAsync (D-01). AssessmentSession sole source.
```

### Diff Spec — D-03 (GradingService.cs:483-562)

Range yang DIHAPUS: lines 493-498 (Pass→Fail TR update) + lines 540-561 (Fail→Pass TR insert/update).
Range yang TETAP: lines 487-492 (Pass→Fail sertifikat revoke ExecuteUpdateAsync) + lines 506-538 (Fail→Pass cert generate retry loop) + LogInformation calls (adjust message text).

**Before (line 483-502, Pass→Fail block):**
```csharp
            // 5. Cascade sertifikat + TrainingRecord (only when flip)
            bool wasPassed = oldIsPassed ?? false;
            if (wasPassed && !isPassed)
            {
                // Pass -> Fail
                await _context.AssessmentSessions
                    .Where(s => s.Id == session.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.NomorSertifikat, (string?)null)
                        .SetProperty(r => r.ValidUntil, (DateTime?)null));

                var judul = $"Assessment: {session.Title}";
                await _context.TrainingRecords
                    .Where(t => t.UserId == session.UserId && t.Judul == judul && t.Tanggal == session.Schedule)
                    .ExecuteUpdateAsync(t => t.SetProperty(r => r.Status, "Failed"));

                _logger.LogInformation(
                    "RegradeAfterEditAsync: session {SessionId} flip Pass->Fail — cert dicabut, TR=Failed.",
                    session.Id);
            }
```

**After:**
```csharp
            // 5. Cascade sertifikat saat flip (Phase 324 D-03: cascade TR seluruhnya dihapus)
            bool wasPassed = oldIsPassed ?? false;
            if (wasPassed && !isPassed)
            {
                // Pass -> Fail: revoke sertifikat (TR cascade removed per Phase 324)
                await _context.AssessmentSessions
                    .Where(s => s.Id == session.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.NomorSertifikat, (string?)null)
                        .SetProperty(r => r.ValidUntil, (DateTime?)null));

                _logger.LogInformation(
                    "RegradeAfterEditAsync: session {SessionId} flip Pass->Fail — sertifikat dicabut.",
                    session.Id);
            }
```

**Before (line 503-567, Fail→Pass block):**
```csharp
            else if (!wasPassed && isPassed)
            {
                // Fail -> Pass
                if (session.GenerateCertificate && session.AssessmentType != "PreTest")
                {
                    // ... cert generate retry loop (line 508-538) ...

                    var judul = $"Assessment: {session.Title}";
                    var existingTr = await _context.TrainingRecords
                        .FirstOrDefaultAsync(t => t.UserId == session.UserId && t.Judul == judul && t.Tanggal == session.Schedule);
                    if (existingTr == null)
                    {
                        _context.TrainingRecords.Add(new TrainingRecord { /* ... */ });
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        existingTr.Status = "Passed";
                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation(
                    "RegradeAfterEditAsync: session {SessionId} flip Fail->Pass — cert generated (if applicable), TR=Passed.",
                    session.Id);
            }
```

**After:**
```csharp
            else if (!wasPassed && isPassed)
            {
                // Fail -> Pass: generate sertifikat (TR cascade removed per Phase 324 D-03)
                if (session.GenerateCertificate && session.AssessmentType != "PreTest")
                {
                    // ... cert generate retry loop TETAP (line 508-538) ...
                }

                _logger.LogInformation(
                    "RegradeAfterEditAsync: session {SessionId} flip Fail->Pass — sertifikat dibuat (jika applicable).",
                    session.Id);
            }
```

### Playwright Test Pattern (D-07)

Reuse `tests/e2e/manage-assessment-filter.spec.ts` + `tests/e2e/edit-peserta-answers.spec.ts` style:

```typescript
// tests/e2e/Phase324_NoDuplicateTrainingRecord.spec.ts
import { test, expect, type Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

async function loginAny(page: Page, accountKey: AccountKey) {
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}

test.describe('Phase 324 — No Duplicate TrainingRecord on Assessment Completion', () => {
  test('S1: Worker submit non-essay assessment → /CMP/Records show 1 row "Assessment Online"', async ({ page }) => {
    // Requires SQL seed via SEED_WORKFLOW dengan assessment Open status.
    // Alternative: assume session existing dari fixture.
    await loginAny(page, 'coachee');
    // ... submit flow ...
    await page.goto('/CMP/Records');
    const assessmentRows = page.locator('tr', { hasText: 'Assessment Online' })
      .filter({ hasText: SESSION_TITLE });
    await expect(assessmentRows).toHaveCount(1);  // bukan 2
    const trainingRows = page.locator('tr', { hasText: 'Training Manual' })
      .filter({ hasText: SESSION_TITLE });
    await expect(trainingRows).toHaveCount(0);  // tidak ada copy TR
  });

  // S2-S7: PreTest skip, Essay finalize, AkhiriUjian, AkhiriSemuaUjian, Regrade Pass→Fail, Regrade Fail→Pass
  // Pattern sama: setup → action → assert /CMP/Records count + DB query verify (via shell sqlcmd kalau perlu).
});
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Auto-copy AssessmentSession ke TrainingRecord untuk unified view | AssessmentSession sole source via `GetUnifiedRecords` UNION branch | Phase 324 (2026-05-26) — fix regression dari `766011b6` | Visual: 1 row per event. Storage: ~50% saving di TR table (semua auto-copy dihapus). Code: 3 lokasi clean. |
| Dead-code `try-catch DbUpdateException` guard | No insert, no guard needed | Phase 324 | Removes confusion; catch handler tidak pernah trigger (no unique index — verified) |

**Deprecated/outdated (intra-codebase):**
- Comment line 254 GradingService "PPT SC6 (ISS-03 fix): TrainingRecord HANYA dari Post-Test, bukan Pre-Test" — obsolete setelah D-01. Hapus comment ini.
- Block try-catch line 280-284 GradingService — dead code (catch branch tidak pernah trigger). Hapus.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Cross-grep audit `TrainingRecords.(Add|AddAsync|AddRange)` complete — tidak ada path lain di codebase yang insert TR dengan pola `Judul = "Assessment: ..."` | Summary, Anti-pattern check | Kalau ada hidden path, visual duplicate akan tetap muncul untuk subset event. Mitigation: planner re-run grep di Plan task akhir. [VERIFIED: codebase grep returns 4 hits — 3 in scope + 4 in TrainingAdminController out of scope] |
| A2 | Cleanup filter `WHERE CreatedAt >= '2026-04-10'` safely scoped ke auto-generated TR | Pitfall 6, SQL script | Bisa accidentally hapus row admin manual entry dengan judul `Assessment:` post-2026-04-10. Mitigation: sample pre-check query di handoff IT (Pitfall 6). [ASSUMED — needs sample query verification di Dev DB sebelum IT execute] |
| A3 | DB `TrainingRecords.CreatedAt` column ada + populated reliably | D-04, SQL script | Filter `CreatedAt >= '2026-04-10'` tidak akan match apapun kalau column null/missing. [ASSUMED — needs verify: `SELECT TOP 1 CreatedAt FROM TrainingRecords;` di lokal sebelum cleanup. Code di `GradingService.cs` line 268-277 TIDAK explicitly set CreatedAt — kemungkinan kolom punya `default GETUTCDATE()` di DB layer atau di model. Verifikasi via `Models/TrainingRecord.cs` — TIDAK ada property `CreatedAt` di model. **HIGH RISK ASSUMPTION** — kemungkinan kolom tidak ada atau di-set lewat shadow property/migration. Planner WAJIB verify schema sebelum execute cleanup.] |
| A4 | Race-condition di Dev saat code deploy + worker submit di tengah window kecil dan acceptable | Pitfall 5 | Worker yang submit selama deploy window bisa generate TR baru lagi → cleanup script perlu re-run. Mitigation: rerun idempotent. [ASSUMED — typical deploy window <30s, low traffic risk] |
| A5 | Playwright `tests/e2e/global.teardown.ts` BACKUP/RESTORE lifecycle compatible dengan spec baru Phase 324 | Don't Hand-Roll, Code Examples | Kalau spec Phase 324 punya side-effect di luar yang ter-cover RESTORE, residual seed. [VERIFIED: existing pattern di Phase 322 + Phase 321 reuse identical fixture — proven safe] |
| A6 | HTML handoff template `2026-05-13.html` masih representatif untuk Pertamina branding 2026-05-26 | D-06, HTML handoff structure | Branding bisa berubah (warna, logo, layout). [VERIFIED: read of `2026-05-13.html` shows stable CSS variables — brand #e30613, navy #1e3a8a — likely stable, no internal communication changes Pertamina lately] |
| A7 | `WorkerDataService.GetUnifiedRecords` AssessmentSession branch akan tetap render "Assessment Online" row setelah TR copy dihapus | Architecture diagram | Kalau view template Records.cshtml filter by RecordType atau ada side-condition di assessment branch, row bisa hilang. [VERIFIED: read `WorkerDataService.cs:31-56` confirms branch unconditional untuk Status="Completed"] |

**Critical follow-up:** A3 `CreatedAt` column existence WAJIB di-verify sebelum SQL script di-finalize. Saran Plan 01 task awal: `sqlcmd ... -Q "SELECT TOP 1 name FROM sys.columns WHERE object_id = OBJECT_ID('TrainingRecords') AND name = 'CreatedAt';"` — kalau return 0 row, filter clause harus diganti (mis. pakai `Tanggal >= '2026-04-10'` ATAU `Id >= <id-first-post-regression>`).

## Open Questions (RESOLVED — see resolution markers per question)

1. **`TrainingRecords.CreatedAt` column existence di DB** — **RESOLVED via Plan 03 Task 1** (schema verify via sqlcmd INFORMATION_SCHEMA query → filter column decision documented di 324-03-SUMMARY.md)
   - What we know: Model `TrainingRecord.cs` TIDAK punya property `CreatedAt`. Bug repro CONTEXT.md asumsi kolom ada.
   - What's unclear: Apakah kolom ada di DB (shadow property / migration default) atau memang tidak ada?
   - Recommendation: Plan 01 first task → verify via `sqlcmd` query schema. Kalau tidak ada, alternative filter:
     - `WHERE Judul LIKE 'Assessment:%' AND TanggalSelesai >= '2026-04-10'` (TanggalSelesai DI-SET di code line 273)
     - ATAU `WHERE Judul LIKE 'Assessment:%' AND Id > <max-id-before-2026-04-10>` (cari max Id pre-regression dari `Migrations` history atau audit log)

2. **Cutoff atas cleanup window** — **RESOLVED**: no upper bound needed. Cleanup script idempotent (re-run aman, post-count tetap 0). Pitfall 5 race condition di-handle via Plan 04 IT handoff explicit ordering callout (Step 1 deploy code DULU, Step 2 SQL cleanup) — bukan via filter cutoff atas.
   - What we know: D-04 spec hanya `CreatedAt >= '2026-04-10'` (no upper bound)
   - What's unclear: Apakah perlu `< '2026-05-26'` (date cleanup) untuk avoid race condition selama deploy?
   - Recommendation: Tidak perlu upper bound; cleanup idempotent → kalau ada drift, re-run. Tapi documentation di HTML handoff WAJIB explicit "deploy code DLL DULU, baru SQL cleanup."

3. **TrainingRecord legacy yang RenewsSessionId/RenewsTrainingId-nya FK ke session yang di-delete** — **RESOLVED via Plan 03 Task 2** (orphan-check query measured + decision branch: kalau N=0 standard cleanup, kalau N>0 tambah null-clear pre-DELETE statement di SQL script Task 3)
   - What we know: TrainingRecord punya `RenewsSessionId` + `RenewsTrainingId` (Phase 200 renewal chain) — FK `NoAction`, nullable.
   - What's unclear: Adakah TR auto-generated yang punya child TR me-renew-nya (jadi delete akan leave orphan reference)?
   - Recommendation: Pre-check sample query: `SELECT COUNT(*) FROM TrainingRecords WHERE RenewsTrainingId IN (SELECT Id FROM TrainingRecords WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10');` — kalau 0, aman. Kalau >0, planner perlu decide: null-clear `RenewsTrainingId` di child sebelum delete, atau skip parent dari cleanup. **MEDIUM RISK** — perlu addressed di Plan 01.

4. **PreTest dengan `IsPassed=true` apakah generate sertifikat (existing behavior intent)?** — **RESOLVED**: out of scope Phase 324. Phase 324 hanya subtract TR auto-create — cert generation existing behavior tidak diubah. Kalau ada PreTest dengan `GenerateCertificate=true`, post-Phase-324 PreTest yang passed akan punya NomorSertifikat visible di /CMP/Records via AssessmentSession branch (existing WorkerDataService.GetUnifiedRecords flow, tidak ada regression). Defer audit ke Phase masa depan kalau ada user report.
   - What we know: Pre-fix code line 264 `if (!trainingRecordExists && session.AssessmentType != "PreTest")` — TR skip untuk PreTest. Tapi cert generation (line 289-321) TIDAK gate by AssessmentType.
   - What's unclear: Apakah ada PreTest dengan `GenerateCertificate=true`? Kalau iya, post-Phase-324 PreTest yang passed akan punya NomorSertifikat (visible di Records via AssessmentSession branch).
   - Recommendation: Planner check apakah Spec asli `GenerateCertificate` selalu false untuk PreTest. Konteks: `WorkerDataService.GetUnifiedRecords:54` ekspos `GenerateCertificate` flag — kemungkinan UI filter di view. Tidak block phase, tapi worth flagging.

## Environment Availability

> External dependencies untuk Phase 324 execution.

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8.x | `dotnet build` + `dotnet run` | ✓ | 8.x (existing project) | — |
| SQL Server Express (SQLEXPRESS) | DB lokal cleanup + BACKUP/RESTORE | ✓ | localhost\SQLEXPRESS (`HcPortalDB_Dev`) | — |
| sqlcmd | SEED_WORKFLOW + cleanup script execute | ✓ (assumed — used in Phase 313/315/322) | system-installed | — |
| Node.js + npm (untuk `tests/`) | Playwright UAT D-07 | ✓ (existing `tests/` setup) | — | — |
| Playwright | E2E spec | ✓ existing | per `tests/package.json` | — |
| `dotnet-ef` CLI | Migration check (DUAL-CHECK no schema change) | ✓ | bundled with EF Core 8 | — |
| Browser (Chrome/Edge) | Manual UAT D-08/D-09 screenshot | ✓ | system | — |

**Missing dependencies with no fallback:** None.

**Missing dependencies with fallback:** None — all infrastructure existing.

## Validation Architecture

> Per `.planning/config.json` `workflow.nyquist_validation: true` — section MANDATORY.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright TypeScript (existing) + `dotnet build` (C# compile check) + `sqlcmd` (DB count verify D-10) |
| Config file | `tests/playwright.config.ts` (verified — testDir `./e2e`, baseURL `http://localhost:5277`, globalTeardown wired) |
| Quick run command | `cd tests && npx playwright test e2e/Phase324_NoDuplicateTrainingRecord.spec.ts` (single spec) |
| Full suite command | `cd tests && npx playwright test` (regression) |
| Build check | `dotnet build` (must be 0 Error) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DUPE-01 (proposed) | `/CMP/Records` 1 row per Assessment Online event (bukan 2) | E2E Playwright | `cd tests && npx playwright test e2e/Phase324_NoDuplicateTrainingRecord.spec.ts -g "S1"` | ❌ Wave 0 |
| DUPE-01 (regression) | PreTest tetap skip TR (no regression) | E2E Playwright | `... -g "S2"` | ❌ Wave 0 |
| DUPE-01 (regression) | Essay finalize tidak insert TR | E2E Playwright | `... -g "S3"` | ❌ Wave 0 |
| DUPE-01 (regression) | AkhiriUjian tidak insert TR | E2E Playwright | `... -g "S4"` | ❌ Wave 0 |
| DUPE-01 (regression) | AkhiriSemuaUjian tidak insert TR untuk semua | E2E Playwright | `... -g "S5"` | ❌ Wave 0 |
| DUPE-01 (regression) | RegradeAfterEdit Pass→Fail revoke cert, no TR cascade | E2E Playwright | `... -g "S6"` | ❌ Wave 0 |
| DUPE-01 (regression) | RegradeAfterEdit Fail→Pass generate cert, no TR cascade | E2E Playwright | `... -g "S7"` | ❌ Wave 0 |
| DUPE-01 (compile) | Build 0 Error after 3 file edit | Unit/compile | `dotnet build` | ✓ existing |
| DUPE-01 (regression guard) | Cross-grep 0 hits `TrainingRecords.Add` di GradingService.cs + AssessmentAdminController.cs | Manual grep | `grep -rn "TrainingRecords\.\(Add\|AddAsync\|AddRange\)" Services/ Controllers/AssessmentAdminController.cs` | ✓ shell |
| DUPE-01 (data hygiene) | Pre/post cleanup count delta | DB query | `sqlcmd ... -Q "SELECT COUNT(*) FROM TrainingRecords WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10';"` | ✓ shell |

### Sampling Rate

- **Per task commit:** `dotnet build` + grep audit (no spec run yet).
- **Per wave merge:** `cd tests && npx playwright test e2e/Phase324_NoDuplicateTrainingRecord.spec.ts` (single file).
- **Phase gate:** Full regression `cd tests && npx playwright test` (≥ existing baseline pass count) + browser manual screenshot D-08/D-09 + SQL count verify D-10 (lokal).
- **Pre-IT-handoff:** HTML handoff ready + commit hash pinned + Wave 1 + Wave 2 + Wave 3 all green.

### Wave 0 Gaps

- [ ] `tests/e2e/Phase324_NoDuplicateTrainingRecord.spec.ts` — covers DUPE-01 (NEW)
- [ ] (Optional, future) Helper `tests/e2e/helpers/recordsAssertions.ts` untuk reusable assertion `/CMP/Records` 1-row pattern — not blocker
- [ ] Seed strategy untuk 7-skenario — leverage existing fixture pattern (`docs/SEED_WORKFLOW.md` lifecycle) atau create per-test session manually via UI

*(Framework install N/A — Playwright already installed di Phase 322.)*

## Security Domain

> Default behavior — section MANDATORY per `security_enforcement` absent (treat as enabled).

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | Existing ASP.NET Identity — TIDAK diubah |
| V3 Session Management | no | N/A (server-rendered, no JWT/token mgmt) |
| V4 Access Control | yes | Existing `[Authorize(Roles="Admin, HC")]` di EndPoint Delete/Edit — TIDAK diubah |
| V5 Input Validation | no | Phase 324 hapus logic insert, tidak ada new input. SQL cleanup hard-coded literal `'Assessment:%'` |
| V6 Cryptography | no | N/A |

### Known Threat Patterns for ASP.NET Core 8 + SQL Server

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| SQL injection via cleanup script | Tampering | Cleanup script literal-only (no parameter, no user input concatenation). IT execute via `sqlcmd -i <file>` — safe. |
| Mass-delete runaway | Tampering / DoS-self | Safety cap (`IF @preCount > 5000 ROLLBACK`) di script. |
| Cleanup race condition (worker submit selama IT run script) | Tampering | Documented ordering: code deploy FIRST → DB cleanup SECOND. Idempotent re-run safe. |
| Lost audit trail | Repudiation | TR row di-delete TANPA audit log entry — by design (data hygiene, bukan user action). HTML handoff document pre-count + post-count + sample row sebagai audit trail. |
| Accidental delete row legitimate manual | Tampering | Pre-check sample query mandatory di handoff (Pitfall 6). IT review sebelum execute DELETE. |

**Critical security note:** Phase 324 = subtract phase, tidak introduce new attack surface. SQL cleanup script HARUS reviewed manual sebelum execute di Prod (HTML handoff structure makes this explicit).

## Sources

### Primary (HIGH confidence)
- `Services/GradingService.cs:255-285, 446-571` — verbatim code Read [VERIFIED: file content]
- `Controllers/AssessmentAdminController.cs:3260-3470, 3750-3870` — Read verbatim [VERIFIED: file content]
- `Services/WorkerDataService.cs:28-82` — Read verbatim [VERIFIED: file content]
- `Models/TrainingRecord.cs` — Read verbatim (no `CreatedAt` property confirmed) [VERIFIED: file content]
- `Data/ApplicationDbContext.cs:140-170` — TrainingRecord config no unique index [VERIFIED: file content]
- `Controllers/CMPController.cs:1727` — GradeAndCompleteAsync call site [VERIFIED: file content]
- `docs/SEED_WORKFLOW.md` — BACKUP/RESTORE pattern [VERIFIED: file content]
- `docs/DEV_WORKFLOW.md` — IT handoff SOP [VERIFIED: file content]
- `docs/DB_HANDOFF_IT_2026-05-13.html` — HTML template Pertamina branding [VERIFIED: file content]
- `tests/playwright.config.ts` + `tests/helpers/accounts.ts` — test infra [VERIFIED: file content]
- `tests/e2e/manage-assessment-filter.spec.ts` + `tests/e2e/edit-peserta-answers.spec.ts` — reusable login + spec pattern [VERIFIED: file content]
- `.planning/phases/323-*/IT_NOTIFY.md` — IT notification recent precedent [VERIFIED: file content]
- Grep `TrainingRecords\.(Add|AddAsync|AddRange)` — full codebase audit returned 4 hits in scope + 4 hits TrainingAdminController out of scope [VERIFIED: grep output]

### Secondary (MEDIUM confidence)
- Phase 322 `tests/e2e/manage-assessment-filter.spec.ts` pattern adaptable to Phase 324 — verified by reading + cross-check Phase 321 [VERIFIED: project context]

### Tertiary (LOW confidence)
- TR `CreatedAt` column existence assumption (A3) — needs runtime verification before SQL cleanup script finalized [ASSUMED — high risk]
- TR `RenewsTrainingId` orphan risk (Open Question 3) — needs pre-check query [ASSUMED — medium risk]

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua existing infrastructure, no new lib
- Architecture: HIGH — call graph fully traced via `Read` + `Grep`, no hidden path (cross-grep audit confirmed)
- Pitfalls: HIGH — diff spec line-precise, verified against actual file content
- Validation: HIGH — pattern reused from Phase 321/322 SHIPPED
- Data cleanup (SQL script): MEDIUM — `CreatedAt` column existence (A3) unverified; script structure HIGH confidence
- Security: HIGH — subtract phase, no new attack surface

**Research date:** 2026-05-26
**Valid until:** 2026-06-25 (30 days — stable internal code, no external dep churn)

---

*Phase 324 RESEARCH — ready for `/gsd-plan-phase 324`.*
