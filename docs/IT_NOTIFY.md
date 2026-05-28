# IT Notify — v19.0 Batch Promo Dev/Prod

**Tanggal:** 2026-05-28
**Milestone:** v19.0 — Portal HC Bug Fixes (4 phase batch sequential)
**Status batch:** Phase 325 + 326 + 327 SHIPPED LOCAL — siap promo Dev (Prod menyusul post-Dev smoke verify)
**Repo:** https://github.com/vinux31/PortalHC_KPB
**Branch:** main
**Developer contact:** rino (asistensme@gmail.com)

## TL;DR untuk IT

- **Jumlah commit batch:** ~48 commit total (20 Phase 325 + 4 Phase 326 + ~24 Phase 327) di branch `main` lokal
- **Migration flag:** ✅ **MIGRATION REQUIRED** — `ChangeValidUntilToDateOnly` (Phase 327). 2 tabel `ALTER COLUMN datetime2 → date`
- **Schema change:** ✅ YA (Phase 327 saja). Phase 325 + 326 zero schema change
- **Yang IT lakukan:** (1) pull latest main, (2) `dotnet build`, (3) pre-check sqlcmd, (4) BACKUP DATABASE, (5) `dotnet ef database update`, (6) restart IIS / Kestrel pool, (7) post-apply verify
- **Yang TIDAK perlu IT lakukan:** apa-apa selain di atas (no manual SQL data fix, no config edit, no NuGet package install — semua sudah at-tag latest)

## Commit Hash Range

- **Phase 325** (Security Hardening P01+P02+P05): `7069ead2..77a9c375` — 20 commit
  - File uploads magic byte gate (P02), path traversal strip (P01), FK pre-check delete (P05)
  - Bootstrap `HcPortal.Tests/` xUnit project + 9 test FileUploadHelper
- **Phase 326** (Validator Hardening P03+P06): `718c67b8..f659ff91` — 4 commit
  - Add/Edit Training POST validators: P03 DAG monotonic + self-renewal guard + P06 Permanent/ValidUntil mutual exclusion
  - `EditTrainingRecordViewModel` extended 3 field (RenewsTrainingId, RenewsSessionId, RenewalSourceTitle)
- **Phase 327** (Timezone DateOnly Refactor P04): `ca33b7ba..<HEAD-after-Plan-08>` — ~24 commit
  - Entity flip `ValidUntil DateTime? → DateOnly?` (TrainingRecord + AssessmentSession)
  - 4 VM flip + 5 rollup props flip + DeriveCertificateStatus DateOnly signature
  - DateOnly arithmetic via DayNumber (replace `(DateTime - DateTime).Days`)
  - EF migration `ChangeValidUntilToDateOnly` (2× AlterColumn)
  - xUnit `CertificateStatusTests` 8/8 GREEN
  - JSON consumer audit: zero `JsonConverter` change, default System.Text.Json `"yyyy-MM-dd"` serialization

**Exact hash range Phase 327 — diisi post-push** (saat ini final hash menunggu Plan 08 task 2 commits).

## Pre-Migration Check (Phase 327 — WAJIB)

**WAJIB run sqlcmd berikut SEBELUM `dotnet ef database update`** untuk confirm zero data loss profile:

```sql
-- Connect: sqlcmd -S <SERVER>\<INSTANCE> -d HcPortalDB_Dev -E -C
-- Replace SERVER/INSTANCE per Dev/Prod config

-- Konfirmasi count row ValidUntil punya komponen jam non-zero
SELECT COUNT(*) AS TR_NonMidnight FROM TrainingRecords
WHERE ValidUntil IS NOT NULL AND CAST(ValidUntil AS TIME) <> '00:00:00';

SELECT COUNT(*) AS AS_NonMidnight FROM AssessmentSessions
WHERE ValidUntil IS NOT NULL AND CAST(ValidUntil AS TIME) <> '00:00:00';
```

**Expected lokal (per Plan 07 pre-check 2026-05-28):**
- `TR_NonMidnight` = 0 ✓
- `AS_NonMidnight` = 2 (legacy Id 2 + Id 9, accept lossy CAST — tanggal preserved, jam truncated, semantik harian OK)

**Kalau Dev/Prod return values berbeda:**
- `TR_NonMidnight > 0` atau `AS_NonMidnight > 2` → STOP, eskalasi developer (kontak: rino). Developer akan investigate apakah data drift Dev-only butuh fix manual sebelum apply migration.
- Kalau accept lossy (precedent Plan 07): jalan migration → SQL Server auto `CAST(datetime2 AS date)` truncate komponen jam.

## BACKUP DATABASE (CRITICAL)

```sql
BACKUP DATABASE HcPortalDB_Dev
TO DISK = '<server-backup-path>\HcPortalDB_Dev_pre-327-migration_<YYYYMMDD>_<HHMMSS>.bak'
WITH FORMAT, INIT, NAME = 'Pre-327-Migration Snapshot';
```

Capture lokasi `.bak` path — required untuk rollback emergency.

## Migration Apply Command

```bash
cd <server-deploy-path>
dotnet ef database update --project HcPortal.csproj
```

**Expected output (per Plan 07 lokal):**
```
Build started...
Build succeeded.
Applying migration '20260528064336_ChangeValidUntilToDateOnly'.
Done.
```

SQL executed (per EF log):
```sql
ALTER TABLE [TrainingRecords] ALTER COLUMN [ValidUntil] date NULL;
ALTER TABLE [AssessmentSessions] ALTER COLUMN [ValidUntil] date NULL;
INSERT INTO [__EFMigrationsHistory] (...) VALUES (N'20260528064336_ChangeValidUntilToDateOnly', N'8.0.0');
```

## Post-Apply Verification

```sql
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('TrainingRecords','AssessmentSessions') AND COLUMN_NAME='ValidUntil';
```

**Expected:** 2 row, `DATA_TYPE = date` keduanya.

Spot-check sample data:
```sql
SELECT TOP 3 Id, ValidUntil FROM TrainingRecords WHERE ValidUntil IS NOT NULL ORDER BY Id DESC;
SELECT TOP 3 Id, ValidUntil FROM AssessmentSessions WHERE ValidUntil IS NOT NULL ORDER BY Id DESC;
```

**Expected:** ValidUntil format `YYYY-MM-DD` tanpa komponen jam (e.g., `2027-05-29` bukan `2027-05-29 00:00:00.0000000`).

## Smoke Verify Dev (6 scenario per spec §11)

1. **P01 path traversal:** Upload sertifikat dengan nama file `../../etc/passwd.pdf` → expect rejected (filename sanitized).
2. **P02 magic byte:** Upload `.exe` rename `.pdf` → expect rejected (magic byte mismatch).
3. **P03 DAG validator:** Add Training renewal dengan Tanggal < source.Tanggal → expect ModelState `"Tanggal renewal harus lebih besar..."`.
4. **P05 FK pre-check:** Delete TrainingRecord yang punya renewal child → expect friendly error message (bukan 500 stack trace).
5. **P06 Permanent+ValidUntil:** Add Training CertificateType=Permanent + ValidUntil set → expect ModelState `"Permanent tidak boleh punya ValidUntil..."`.
6. **P04 DateOnly display:** Buka 5 halaman wajib (`/Admin/ManageAssessment` tab Training, `/Admin/RenewalCertificate`, `/CMP/Records`, `/CDP/CertificationManagement`, `/Home/Index` worker dashboard) — tanggal harus render `dd MMM yyyy` atau `yyyy-MM-dd` **TANPA suffix `00:00:00`**.

Smoke ALL 6 PASS → IT promo Prod ulang prosedur sama (BACKUP, pre-check, apply, post-verify, smoke).

## Rollback Plan (kalau drama)

### Option A: EF Down migration
```bash
dotnet ef database update <previous-migration-name> --project HcPortal.csproj
```
Down() reverse: `date → datetime2` (lossy ke arah balik: jam komponen = 00:00:00, acceptable per design D-03).

### Option B: BACKUP restore
Kalau Down() gagal atau ada bug skema lain:
```sql
USE master;
ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE HcPortalDB_Dev FROM DISK = '<backup-path>.bak' WITH REPLACE;
ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;
```

### Option C: Git revert
```bash
git revert ca33b7ba..<HEAD-after-Plan-08>  # Phase 327 commits
git revert 718c67b8..f659ff91              # Phase 326 commits
git revert 7069ead2..77a9c375              # Phase 325 commits
# rebuild + redeploy
```

## Phase 329 Update (2026-05-28)

**Phase 329** (fix-cascade-deleteassessmentgroup-deleteprepostgroup-renewal) SHIPPED LOCAL.

- **Commit:** `aa643bdf` `feat(329): cascade renewal pre-check DeleteAssessmentGroup + DeletePrePostGroup`
- **Migration flag:** ✅ **TIDAK ADA** — zero schema change, controller-only fix
- **Scope:** `Controllers/AssessmentAdminController.cs` +52 LoC (pre-check + catch refactor)
- **Severity fix:** HIGH D5 (renewal chain) di `DeleteAssessmentGroup` + `DeletePrePostGroup`
- **UAT:** UAT-329-01 ✅ PASS + UAT-329-02 ✅ PASS (Playwright verified 2026-05-28)

Batch v19.0 update: Phase 325 + 326 + 327 + 329 = **4 fix phase** (Phase 328 audit-only, no kode delta).

**Jumlah commit batch update:** ~57 commit total (53 Phase 325+326+327 + 1 Phase 329 + 3 Phase 328 docs-only).

Tambah smoke scenario #7 ke **Smoke Verify Dev**:

**#7 Renewal pre-check group:** Delete grup Assessment yang salah satu session-nya jadi `RenewsSessionId` source → expect redirect ManageAssessment + error banner "Tidak bisa hapus grup: N sertifikat lain..." (BUKAN FK 500 exception page).

## Phase 330 Update (2026-05-28)

**Phase 330** (fix-cascade-med-bundle-delete-category-package-question-orgu) SHIPPED LOCAL.

- **Commit:** `40518631`
- **Migration flag:** ✅ **TIDAK ADA** — zero schema change, zero migration, controller/service-only fix
- **Scope:**
  - `Controllers/AssessmentAdminController.cs` — DeleteCategory + DeletePackage + DeleteQuestion: try/catch DbUpdateException + `_auditLog.LogAsync` (DeleteQuestion baru)
  - `Controllers/OrganizationController.cs` — DeleteOrganizationUnit: try/catch DbUpdateException dual-path (JSON+TempData) + `_auditLog.LogAsync`
  - `Services/NotificationService.cs` — DeleteAsync: `catch (Exception)` → `catch (DbUpdateException)`
- **Severity fix:** MED D6 (no try/catch DbUpdateException) + D3 (no audit log) di 5 endpoint
- **Source:** Phase 328 RESEARCH.md §5 MED Findings + §9 proposal #7

Batch v19.0 update: Phase 325 + 326 + 327 + 329 + 330 = **5 fix phase** (Phase 328 audit-only, no kode delta).

**Jumlah commit batch update:** ~60 commit total.

Tambah smoke scenario #8 ke **Smoke Verify Dev**:

**#8 MED FK friendly error:** Attempt delete Category/Package/Question/OrgUnit yang masih direferensi FK → expect TempData["Error"] friendly message "Tidak bisa hapus {entity}: masih ada data yang berelasi." (BUKAN raw HTTP 500).

## Phase 331 Update (2026-05-28)

**Phase 331** (fix-cascade-deletetraining-deletemanualassessment-atomicity) SHIPPED LOCAL.

- **Commit:** `[hash — lihat git log setelah Task 3 commit]`
- **Migration flag:** ✅ **TIDAK ADA** — zero schema change, zero migration, controller-only fix
- **Scope:**
  - `Controllers/TrainingAdminController.cs` — DeleteTraining + DeleteManualAssessment: wrap BeginTransactionAsync + reorder File.Delete POST CommitAsync + inner try/catch warn-only (D2+D7 fix)
- **Severity fix:** HIGH D2 (file-DB atomicity broken) + D7 (no transaction wrap) di 2 endpoint
- **Source:** Phase 328 RESEARCH.md §4.1 + §4.2 + §9 proposal #1
- **D5 status:** sudah Phase 325 P05 covered (pre-check renewal L568-580 + L802-805 preserved verbatim)

Batch v19.0 update: Phase 325 + 326 + 327 + 329 + 330 + 331 = **6 fix phase** (Phase 328 audit-only, no kode delta).

**Jumlah commit batch update:** ~65 commit total.

Tambah smoke scenario #9 ke **Smoke Verify Dev**:

**#9 HIGH atomicity DeleteTraining + DeleteManualAssessment:** Trigger DB FK violation midway (e.g., manual SQL INSERT child row antara pre-check dan commit) → expect tx rollback + file sertifikat **TETAP ada di disk** + TempData["Error"] friendly. Tanpa fix Phase 331: file gone tapi row alive = sertifikat rusak.

## Order of Operations (CRITICAL)

1. Deploy code dulu (4 phase batch sudah merged di main, pull/checkout latest)
2. `dotnet build` (atau publish ulang artifact)
3. Pre-check sqlcmd (Step "Pre-Migration Check")
4. BACKUP DATABASE (Step "BACKUP DATABASE")
5. Run `dotnet ef database update` (Step "Migration Apply Command")
6. Post-apply verification (Step "Post-Apply Verification")
7. Restart IIS / Kestrel pool
8. Smoke 6 scenario (Step "Smoke Verify Dev")
9. Kalau Dev OK → IT promo Prod ulang prosedur sama

## Reference Artifacts

- `.planning/phases/325-security-hardening-p01-p02-p05/325-UAT.md` — Phase 325 5/5 SC PASS browser-verified
- `.planning/phases/326-validator-hardening-p03-p06/326-UAT.md` — Phase 326 6/6 SC PASS browser-verified
- `.planning/phases/327-timezone-dateonly-refactor-p04/327-UAT.md` — Phase 327 7/7 SC PASS automated Playwright + JSON smoke
- `docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` §11 (line 407-427) — IT promo batch strategy spec
- `docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` §7 (line 255-361) — Phase 327 design (DateOnly strategy A)
- `docs/sertifikat-ecosystem/bug-findings.html` §P01-P06 — root cause analysis 6 bug

## Contact

Developer: rino (asistensme@gmail.com)
Repo: https://github.com/vinux31/PortalHC_KPB
