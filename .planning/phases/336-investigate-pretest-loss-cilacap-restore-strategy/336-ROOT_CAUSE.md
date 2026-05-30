# Phase 336 — ROOT_CAUSE.md

**Phase:** 336-investigate-pretest-loss-cilacap-restore-strategy
**Date:** 2026-05-30
**REQ:** REST-01 (git log), REST-02 (confirm root cause)
**Scope:** Investigation-only (read-only), zero source code modification

---

## Schema Evolution Timeline (window 2026-03-30 .. 2026-05-19)

Source: `git log --since="2026-03-30" --until="2026-05-19" -p -- Models/AssessmentSession.cs`

Commits yang touch `Models/AssessmentSession.cs` window investigation:

| Commit Hash | Date | Files Touched | Field Added/Modified/Dropped | Suspect Level |
|-------------|------|---------------|------------------------------|---------------|
| `569eb0a8` | 2026-04-06 | Models/AssessmentSession.cs + 2 other model | **+AssessmentType +AssessmentPhase +LinkedGroupId +LinkedSessionId +HasManualGrading** (5 nullable add, no DROP) | LOW (add-only, backward compat) |
| `a7bb443e` | 2026-04-06 | Migrations/AddAssessmentV14Columns | EF Core migration `AddColumn` (5 column, no DROP) | LOW (paired with `569eb0a8`, schema-preserving) |
| `f82bad2e` | 2026-04-07 | Models/PackageQuestion.cs + Models/PackageUserResponse.cs + Migrations/AddRubrikEssayScoreMaxCharFields | **Rubrik/MaxCharacters/EssayScore add** to PackageQuestion + Response — TIDAK touch AssessmentSession schema | IRRELEVANT to AssessmentSession |
| `5223ce55` | 2026-04-07 | Models/AssessmentSession.cs + Migrations/AddExtraTimeMinutesToAssessmentSession | **+ExtraTimeMinutes** (1 nullable add, no DROP) | LOW (add-only) |
| `b89b6559` | 2026-04-13 | Models/AssessmentSession.cs + Migrations/AddSamePackageToAssessmentSession | **+SamePackage** (1 bool add default false, no DROP) | LOW (add-only) |
| `0dedd7b7` | 2026-04-14 | Models/AssessmentSession.cs + Migrations/AddManualEntryToAssessmentSession + AddAssessmentExtraFields | **+IsManualEntry +ManualSertifikatUrl +Penyelenggara +Kota +SubKategori +CertificateType** (6 add, no DROP) | LOW (add-only, manual entry lifecycle) |
| `ac86fea3` | 2026-05-07 | Migrations/AddManageAssessmentPerfIndexes | INDEX-only operation (CREATE INDEX, no schema change) | IRRELEVANT to data |

**Critical observation:** SEMUA 7 commit di window ADALAH `ADD-ONLY` (nullable column ATAU default value ATAU INDEX). **ZERO `DROP COLUMN`. ZERO `RECREATE TABLE`. ZERO `EnsureDeleted`. ZERO destructive schema operation.**

Per decision tree D-02, ini eliminates kategori **"Migration DROP COLUMN no-preserve"** + **"Migration recreate table"** sebagai root cause candidates. Suspect bergeser ke kategori **schema-preserving migration** ATAU **EnsureCreated/SeedData reset** ATAU **manual cleanup**.

---

## Migration Candidate Analysis (13 files)

Per CONTEXT.md daftar 13 migration window:

| Migration Name | Date | Up Operation Summary | Touches AssessmentSession? | Classification | Forced Strategy |
|----------------|------|----------------------|----------------------------|----------------|-----------------|
| `AddMaintenanceMode` | 2026-04-01 | CREATE TABLE MaintenanceConfig (new entity) | N | IRRELEVANT_TO_AssessmentSession | N/A |
| `FixInterviewResultsJsonColumnType` | 2026-04-02 | ALTER COLUMN InterviewResultsJson type fix (nvarchar(max)) | Y (column type modification only, no data drop) | SCHEMA_PRESERVING_ALTER | C |
| `AddAssessmentV14Columns` | 2026-04-06 | ADD COLUMN AssessmentType + AssessmentPhase + LinkedGroupId + LinkedSessionId + HasManualGrading (all nullable) | Y | SCHEMA_PRESERVING_ADD_ONLY | C |
| `AddRubrikEssayScoreMaxCharFields` | 2026-04-07 | ADD COLUMN ke PackageQuestion + PackageUserResponse | N (touches different tables) | IRRELEVANT_TO_AssessmentSession | N/A |
| `RemoveUniqueIndexOnPackageUserResponse` | 2026-04-07 | DROP INDEX (constraint only, no data) | N | IRRELEVANT_TO_AssessmentSession | N/A |
| `AddExtraTimeMinutesToAssessmentSession` | 2026-04-07 | ADD COLUMN ExtraTimeMinutes (nullable int) | Y | SCHEMA_PRESERVING_ADD_ONLY | C |
| `AddBudgetItems` | 2026-04-09 | CREATE TABLE BudgetItems (new entity) | N | IRRELEVANT_TO_AssessmentSession | N/A |
| `RemoveBudgetItemStatus` | 2026-04-09 | DROP COLUMN BudgetItem.Status (different table) | N (NOT AssessmentSession) | IRRELEVANT_TO_AssessmentSession | N/A |
| `AddCoachWorkloadThreshold` | 2026-04-10 | CREATE TABLE CoachWorkloadThreshold (new entity) | N | IRRELEVANT_TO_AssessmentSession | N/A |
| `AddSamePackageToAssessmentSession` | 2026-04-13 | ADD COLUMN SamePackage (bool default false) | Y | SCHEMA_PRESERVING_ADD_ONLY | C |
| `AddManualEntryToAssessmentSession` | 2026-04-14 | ADD COLUMN IsManualEntry + ManualSertifikatUrl + Penyelenggara + Kota + SubKategori + CertificateType | Y | SCHEMA_PRESERVING_ADD_ONLY | C |
| `AddAssessmentExtraFields` | 2026-04-14 | ADD COLUMN supplementary (paired with `AddManualEntry`) | Y | SCHEMA_PRESERVING_ADD_ONLY | C |
| `AddManageAssessmentPerfIndexes` | 2026-05-07 | CREATE INDEX (Phase 311-03) — non-clustered index for query perf | Y (indexes on AssessmentSession but no schema change) | INDEX_ONLY | C |

**Tabel 13/13 row classified.** ✅

### Culprit Identification

**NO MIGRATION CULPRIT.** All 13 migrations dalam window adalah:
- 6× SCHEMA_PRESERVING (add-only nullable columns / schema-preserving ALTER)
- 1× INDEX_ONLY
- 6× IRRELEVANT_TO_AssessmentSession (touch table lain)

**ZERO migration yang drop column AssessmentSession, recreate table AssessmentSessions, atau truncate data AssessmentSessions.**

Per decision tree D-02, dengan **schema-preserving migration only**, default forced strategy = **C (tunggu Gap #5 Phase 337 W3 Excel breakdown enabler)**. TAPI sebelum lock C, eskalasi ke Task 3 (EnsureCreated check) + Task 4 (AuditLog elimination) untuk confirm bukan ada path lain (reset by `EnsureCreated`, seed reset, manual cleanup).

---

## EnsureCreated + SeedData Reset Check

Source: Grep `EnsureCreated|EnsureDeleted|Database\.Migrate` whole codebase + Read `Program.cs` + git log -p `Data/SeedData.cs` window.

### Grep Evidence (whole codebase `*.cs`)

```
Program.cs:133:        context.Database.Migrate(); // Apply migrations
```

**Hanya 1 match: `Database.Migrate()` di `Program.cs:133`.** ZERO match untuk `EnsureCreated()` ATAU `EnsureDeleted()` di seluruh codebase.

**OQ-336-2 RESOLVED: NO.** `EnsureCreated()` TIDAK PERNAH dipanggil di Program.cs / Startup.cs / DbContext. Database init via `Migrate()` only — production-safe pattern yang TIDAK drop/recreate schema, hanya apply incremental migrations. **Eliminates** hipotesis "EnsureCreated reset Dev DB".

### Seed Reset Analysis

`Data/SeedData.cs` (106 LoC) entity yang di-seed:
1. Roles (`CreateRolesAsync`)
2. Admin user bootstrap (`CreateAdminUserAsync`)
3. ~~Test users (Dev only)~~ — **REMOVED** di commit `0dedd7b7` (2026-04-14)
4. OrganizationUnits (`SeedOrganizationUnitsAsync`)

**ZERO seed untuk `AssessmentSession`, `AssessmentAttemptHistory`, `PackageUserResponses`, `TrainingRecord`.** SeedData.cs TIDAK PERNAH touch assessment-related data, hanya users + roles + organization structure.

Window diff `Data/SeedData.cs` 2026-03-30..2026-05-19:
- 1 commit: `0dedd7b7` (Apr 14) — DELETE `CreateTestUsersAsync` method (removes test users seeding for Dev). Tidak ada Clear/RemoveRange/Truncate untuk AssessmentSession.

**Seed Reset Analysis: NO.** Seed mechanism TIDAK PERNAH reset/clear AssessmentSession data. **Eliminates** hipotesis "SeedData reset".

### Implication

Dengan eliminasi (1) migration drop schema, (2) `EnsureCreated()`, (3) seed reset — root cause path bergeser ke:
- **Manual cleanup admin UI** (Task 4 AuditLog elimination → if AuditLog 0 entry, NOT this)
- **Manual SQL DELETE out-of-band** (low-prob, butuh IT report)
- **Dev DB external recreate** (e.g., IT restored from old `.bak` saat deployment, dropping recent data)
- **Unable to determine** (fallback)

---

## AuditLog Silent-Delete Elimination

Source: Read `Models/AuditLog.cs` + grep `AuditLogService|RecordAuditLog|LogAudit|_auditLog\.LogAsync|AuditLog\.Add` whole codebase + verify delete endpoints write audit.

### AuditLog Model Schema (verified `Models/AuditLog.cs`)

| Field | Type | Purpose |
|-------|------|---------|
| `Id` | int | PK |
| `ActorUserId` | string Required | ASP.NET Identity user yang trigger action |
| `ActorName` | string Required | NIP + FullName snapshot (survive user delete) |
| `ActionType` | string Required MaxLen 50 | Machine-readable: `CreateAssessment`, `EditAssessment`, `BulkAssign`, **`DeleteAssessment`**, **`DeleteAssessmentGroup`**, `AkhiriUjian`, `AkhiriSemuaUjian`, `ResetAssessment` |
| `Description` | string Required | Human-readable narrative |
| `TargetId` | int? | Optional PK target entity |
| `TargetType` | string? MaxLen 100 | Optional type name (e.g., `AssessmentSession`) |
| `CreatedAt` | DateTime | UTC timestamp |

### Delete Endpoints Audit Write Verified

Grep `Controllers/AssessmentAdminController.cs` confirm:
- **`DeleteAssessment` @ L2019** → write AuditLog ActionType `"DeleteAssessment"` @ L2069 + L2174
- **`DeleteAssessmentGroup` @ L2207** → write AuditLog ActionType `"DeleteAssessmentGroup"` @ L2261 + L2359

15 controllers/services pakai `_auditLog.LogAsync` pattern — audit logging WIRED di semua admin delete operations. **Tidak ada Delete endpoint AssessmentSession yang BYPASS AuditLog.**

### 5-Hypothesis Reasoning

Fakta dasar (incident note 2026-05-29):
- Dev DB 10.55.3.3 → 0 row `AssessmentSessions` Title `Pre Test` / `OJT GAST`
- AuditLog 0 entry untuk `DeleteAssessment` / `DeleteAssessmentGroup` PreTest

| # | Hipotesis | Konsisten dgn fakta? | Status |
|---|-----------|----------------------|--------|
| A | Migration DROP COLUMN no-preserve / RECREATE TABLE → row hilang silent (NOT via app endpoint) → bypass AuditLog | ✗ ELIMINATED — Task 2 confirm 0 migration drop/recreate AssessmentSession schema |
| B | `EnsureCreated()` reset Dev DB → row hilang silent | ✗ ELIMINATED — Task 3 confirm `EnsureCreated` ZERO grep match, hanya `Database.Migrate()` |
| C | `SeedData.cs` reset/cleanup → row hilang silent | ✗ ELIMINATED — Task 3 confirm SeedData tidak pernah touch AssessmentSession |
| D | **Cosmetic delete via admin UI** (DeleteAssessment endpoint) | ✗ ELIMINATED — endpoint write AuditLog L2069+L2174, kalau true HARUS ada entry. 0 entry = NOT this case |
| E | **Manual SQL DELETE out-of-band** (IT direct sqlcmd / DBA cleanup) → bypass AuditLog (NOT via app endpoint) | ✓ CONSISTENT — bypass app layer = no audit. Low-medium probability, butuh IT report |
| F | **Dev DB external recreate via .bak restore** (IT restore old snapshot saat deployment troubleshooting, wipe newer data) → bypass AuditLog | ✓ CONSISTENT — entire DB restored from old state. Medium-high probability di Dev maintenance pattern. Butuh IT report .bak history |

**OQ-336-3 RESOLVED:** Silent delete CONFIRMED via path E (manual SQL out-of-band) ATAU path F (Dev DB .bak restore by IT). Path A/B/C/D ALL ELIMINATED.

→ Implication: Root cause **TIDAK BISA dideterminasi sepenuhnya tanpa IT confirmation**. Kemungkinan path F (DB restore) > path E (manual SQL) berdasarkan Dev environment maintenance pattern (IT lebih sering restore .bak daripada manual sqlcmd delete).

### Conclusion Pending

Root cause = **silent out-of-band data loss** (path E or F). Decision tree path = **"manual_cleanup"** ATAU **"unable_to_determine"** (depending interpretation strict). Per CONTEXT.md D-02 default fallback "unable_to_determine" → forced strategy **B (skip restore, default safe)**.

BUT: kalau `.bak` Dev DB pre-restore tersedia (OQ-336-1 user input Task 5), Option A (re-import via Excel) tetap feasible regardless root cause path. Strategy gating berikutnya ada di Task 5 user input.

---

## Decision Tree Path Taken

**Path:** `manual_cleanup` (variant — IT operational redeploy tanpa backup)

User input Task 5 (2026-05-30):
- **OQ-336-1**: `.bak` Dev DB snapshot window 30 Mar – 19 May 2026 = **NO** (confirmed tidak ada)
- **OQ-336-3 root cause refined**: Tim IT pull code GitHub terbaru + DB latest dari user, **lupa backup DB Dev existing**. Otomatis data Dev (termasuk PreTest 30 Mar) ke-overwrite/drop+recreate tanpa preserve saat sync.

Konsistensi fakta:
- PostTest Cilacap 19 May masih ada (dibuat SETELAH IT sync) ✓
- PreTest 30 Mar hilang (dibuat SEBELUM IT sync, gak ke-include) ✓
- AuditLog 0 entry (bypass app layer = ✓ konsisten path operational sync)
- Schema-preserving migration (5 ADD-only commit window) = data should persist normally kalau sync preserve

**Strategy picked: A (re-import Excel)** — OVERRIDE default forced strategy B (`manual_cleanup` → B per CONTEXT.md tabel) karena:
- Excel user backup `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx` AVAILABLE
- 13 peserta + score total RECOVERABLE
- Endpoint `AddManualAssessment` exists (Phase 263+ confirmed via commit `0dedd7b7`)
- Value-recover focused (Pre vs Post comparison aktif lagi)

---

## Conclusion

**Root cause: IT operational redeploy tanpa backup pre-deploy (silent data loss path F-variant).**

Mekanisme: Tim IT pull code GitHub terbaru + DB latest dari developer sebagai bagian deployment sync, lupa backup Dev DB existing. Saat sync, data Dev yang TIDAK ada di package sync (PreTest 30 Mar dibuat lokal di Dev langsung, BUKAN di developer DB) ke-overwrite/drop tanpa preserve. Bypass aplikasi sepenuhnya = AuditLog 0 entry konsisten.

**BUKAN bug aplikasi, BUKAN migration culprit, BUKAN data integrity issue.** ADALAH operational procedure gap di deployment workflow.

**Implikasi guardrail (REST-05 Phase 338 W5):**
SUPER critical sekarang dgn root cause confirmed = IT lupa backup. Pre-deploy backup SQL Server `.bak` hook WAJIB untuk prevent recurrence. REST-05 naik dari "nice to have" → "CRITICAL must-have".

**Schema evolution clean.** 7 commit ADD-only, EF Core `Migrate()` production-safe. Tidak ada code change yang perlu di Phase 337/338 untuk root cause fix — fix murni operational/SOP.

**Restore feasible via Excel backup user.** Strategy A (re-import via `AddManualAssessment` endpoint) = Phase 338 W4 deliverable.
