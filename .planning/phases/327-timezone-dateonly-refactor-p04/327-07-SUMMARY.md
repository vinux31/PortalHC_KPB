---
phase: 327-timezone-dateonly-refactor-p04
plan: 07
status: complete (SC-1 + SC-2 PASS, SC-7 documented)
date: 2026-05-28
commits: [17cb82bb, "(EF gen)", 5984f951]
migration: 20260528064336_ChangeValidUntilToDateOnly
---

# Plan 327-07 — SUMMARY

## One-Liner
EF migration `ChangeValidUntilToDateOnly` GENERATED + APPLIED lokal sukses. SC-1 schema verify `date` 2 tabel ✓. SC-2 pre-check 2 row jam non-zero legacy AS (Id 2+9) accept lossy CAST. Backup ready (15.3MB). Build + 18/18 test GREEN regression.

## What Was Built

### Task 1: Pre-check + BACKUP DATABASE + Journal entry
**sqlcmd D-11 pre-check:**
- TrainingRecords.ValidUntil non-midnight: **0** ✓
- AssessmentSessions.ValidUntil non-midnight: **2** ⚠ (Id 2 + 9, legacy pre-Plan-05 grading)

**T-327-01 escalation outcome:** User approved "Accept lossy CAST" — tanggal preserved, jam truncated, semantik harian OK.

**BACKUP:** `C:/temp/HcPortalDB_Dev_pre-327-migration_20260528_144151.bak`
- Size: 15.3 MB (1858 pages)
- Duration: 0.216s @ 67.183 MB/sec
- Source: HcPortalDB_Dev di localhost\SQLEXPRESS Integrated Security

**Journal entry:** SEED_JOURNAL.md +1 row classification `permanent + prod-required`, status `active` (commit `17cb82bb`).

### Task 2: EF migration generate + review
**Command:** `dotnet ef migrations add ChangeValidUntilToDateOnly --project HcPortal.csproj`

**Generated files:**
- `Migrations/20260528064336_ChangeValidUntilToDateOnly.cs` (55 baris)
- `Migrations/20260528064336_ChangeValidUntilToDateOnly.Designer.cs` (snapshot scaffold)
- `Migrations/ApplicationDbContextModelSnapshot.cs` (auto-updated 2× Property<DateOnly?>)

**File content review (PERFECT):**
- Up(): 2× `AlterColumn<DateOnly>` datetime2 → date
- Down(): 2× `AlterColumn<DateTime>` date → datetime2 (D-03 rollback siap)
- ZERO `DropColumn` (Pitfall 4 clean)

**EF warning di output:** "An operation was scaffolded that may result in the loss of data" — expected acknowledgment T-327-01 CAST truncate, already mitigated via Task 1.

### Task 3 CHECKPOINT: User approval
User selected "Apply migration (proceed)" via AskUserQuestion. Backup + pre-check + reviewed file = green light.

### Task 4: Apply migration + post-verify + JSON audit
**Command:** `dotnet ef database update --project HcPortal.csproj` → **"Done."**

SQL executed (per EF log):
```sql
ALTER TABLE [TrainingRecords] ALTER COLUMN [ValidUntil] date NULL;
ALTER TABLE [AssessmentSessions] ALTER COLUMN [ValidUntil] date NULL;
INSERT INTO [__EFMigrationsHistory] (...) VALUES (N'20260528064336_ChangeValidUntilToDateOnly', N'8.0.0');
```

**SC-1 verify (INFORMATION_SCHEMA):**
```
TABLE_NAME            COLUMN_NAME   DATA_TYPE
AssessmentSessions    ValidUntil    date  ✓
TrainingRecords       ValidUntil    date  ✓
```

**SC-2 verify (CAST truncate legacy):**
```
Id   ValidUntil
2    2027-02-22  (was 2027-02-22 09:46:57.6520496 → jam dibuang)
9    2026-02-17  (was 2026-02-17 10:36:16.1027118 → jam dibuang)
```

**Regression:** `dotnet test HcPortal.sln --nologo` → **Passed 18/0, 82 ms** (10 Phase 325 FileUploadHelper + 8 Phase 327 CertificateStatusTests).

**JSON API audit (D-15):**
- **1 risk endpoint:** `/CMP/GetExpiringSoonData` (method @2975) — anonymous type `tanggalExpired` list (`tanggalExpired = t.ValidUntil!.Value` di L2998, L3017). Consumer `wwwroot/js/analyticsDashboard.js:852-853` `new Date("yyyy-MM-dd")` parse → tz shift Pitfall 3. **Plan 08 SC-5/Pitfall 3 smoke target.**
- Other ValidUntil endpoints return counts/metrics only (e.g., `expiringCount` di GetAnalyticsSummary L2817). No date payload to consumer. Zero JS Date parse risk.

## Verification
- ✅ SC-1: INFORMATION_SCHEMA TrainingRecords + AssessmentSessions ValidUntil DATA_TYPE = `date`
- ✅ SC-2: Pre-check captured + CAST truncate result verified (2 row jam dibuang, tanggal preserved)
- ✅ Build SUKSES 0 Error
- ✅ Test 18/18 GREEN
- ✅ SEED_JOURNAL entry: snapshot + applied status (audit trail)
- ✅ JSON API audit: 1 risk endpoint flagged for Plan 08 smoke
- ✅ SC-7: Rollback procedure documented (Down() AlterColumn reverse + BACKUP restore)

## Threats
| ID | Status |
|----|--------|
| T-327-01 (data loss CAST datetime2 → date) | **MITIGATED COMPLETE** — pre-check + snapshot + accept lossy + verified post-apply tanggal preserved |

## Decisions Applied
- D-02: Migration name `ChangeValidUntilToDateOnly` ✓
- D-03: Down() rollback reverse documented + EF-generated ✓
- D-11: Pre-check sqlcmd inline + BACKUP + SEED_JOURNAL entry ✓
- D-15: JSON API audit ✓ (1 risk endpoint, no JsonConverter spoof)

## Pending Downstream
- **Plan 08:** Manual UAT 7 SC + **Pitfall 3 JSON timezone smoke @ /CMP/GetExpiringSoonData (CRITICAL FROM AUDIT)** + Razor TagHelper #47628 verify (Plan 06 Task 2 bundle) + Phase 326 regression smoke + IT_NOTIFY.md draft batch v19.0

## Commits
- `17cb82bb` — docs(327-07): SEED_JOURNAL pre-migration snapshot entry + sqlcmd pre-check
- (EF migration generate) — feat(327-07): EF migration ChangeValidUntilToDateOnly (2× AlterColumn)
- `5984f951` — feat(327-07): apply ChangeValidUntilToDateOnly migration lokal + SC-1 verified

## Next Plan
Plan 327-08 — Manual UAT 7 SC (FINAL phase gate) + Pitfall 3 JSON smoke + Phase 326 regression + IT_NOTIFY.md draft batch v19.0 promo runbook. Push approval gate user-explicit.
