---
phase: 323
type: browser_verify_findings
date: 2026-05-26
status: blocking_finding
---

# Browser Verify Findings — Plan 01 Wave 1

## Setup

- App: `dotnet run` lokal port 5277 (started post Plan 01 commits, latest build artifacts)
- DB: `HcPortalDB_Dev` lokal, BACKUP `HcPortalDB_Dev-pre323-20260526-165911.bak`
- Login: `admin@pertamina.com` OK
- Seed: 1 row `AssessmentEditLog` ke `AssessmentSessionId=2` (ActorUserId `p323-seed-user`)

## Test Method

UI ListGroups filter `>= sevenDaysAgo` exclude Session 2 (Schedule 2025). Bypass via direct POST `/Admin/DeleteAssessment` dengan antiforgery token dari `/Home/Index`. Endpoint path sama dengan UI submit — exec full controller method termasuk Plan 01 cascade block.

## Hasil

### TEMUAN KRITIS — Bug LAIN (di luar scope Phase 323 original)

**Endpoint response:** `"Gagal menghapus assessment. Silakan coba lagi."` MUNCUL.

**Root cause:** BUKAN AssessmentEditLog FK (Plan 01 fix berhasil clear FK itu). Bug lain di FK `UserPackageAssignments.AssessmentPackageId → AssessmentPackages` declared **Restrict** (`Data/ApplicationDbContext.cs:476`).

**Stack trace exception (dari dotnet log lokal):**

```
Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes.
 ---> Microsoft.Data.SqlClient.SqlException (0x80131904):
 The DELETE statement conflicted with the REFERENCE constraint
 "FK_UserPackageAssignments_AssessmentPackages_AssessmentPackageId".
 The conflict occurred in database "HcPortalDB_Dev",
 table "dbo.UserPackageAssignments", column 'AssessmentPackageId'.
 Error Number:547,State:0,Class:16
   ...
   at HcPortal.Controllers.AssessmentAdminController.DeleteAssessment(Int32 id)
   in ...AssessmentAdminController.cs:line 2129
```

**Existing code comment di L2110 SALAH:**

```csharp
// Note: UserPackageAssignments are cascade-deleted by DB (Cascade FK on AssessmentSessionId)
```

UserPackageAssignment punya 2 FK:
- `AssessmentSessionId` → **Cascade** ✅ (L471)
- `AssessmentPackageId` → **Restrict** ❌ (L476)

Saat `RemoveRange(AssessmentPackages)` di L2106 fire, FK Restrict ke Package conflicts — meskipun ada FK Cascade ke Session, SQL Server check ALL FK references parent.

### Data state Session 2 lokal

```
Session2_UserPackageAssignments  | 1
Session2_AssessmentPackages      | 1
Session2_EditLogs                | 1   (seed Phase 323 verify)
```

### Diagnosis Plan 01 fix

- ✅ AssessmentEditLog cascade block tidak crash (exception fire DI HILIR pada AssessmentPackages, bukan di EditLog block paling atas)
- ✅ Build clean, grep verify pass
- ❌ End-to-end delete masih FAIL karena FK kedua (UserPackageAssignments)

## Implikasi

1. **Phase 323 fix kurang lengkap** untuk repro Dev — kalau Dev Session Id 2+5 punya UserPackageAssignment, fix saat ini TIDAK akan resolve "Gagal menghapus assessment"
2. Pattern fix sama dengan Plan 01: insert `RemoveRange(UserPackageAssignments)` SEBELUM `RemoveRange(AssessmentPackages)` di 3 endpoint
3. Bug pre-existing — bukan regression Phase 323. Mungkin Dev test sebelumnya tidak punya UserPackageAssignment di session yang ditest, sehingga belum surface

## Opsi Lanjutan (perlu user decide)

### Opsi A — Extend Phase 323 scope (tambah patch UserPackageAssignments)
- 3 endpoint sama → insert `RemoveRange(UserPackageAssignments)` sebelum `RemoveRange(AssessmentPackages)`
- Update audit description optional (`UserPackageAssignmentsCount={N}`)
- 1-2 jam kerja tambahan + re-verify E2E

### Opsi B — Defer ke Phase 324 (atau 323.1 decimal phase)
- Phase 323 commit + ship apa adanya (EditLog fix benar — partial scope)
- Buka phase baru untuk UserPackageAssignments FK fix
- Risk: Dev IT promo akan tetap "Gagal menghapus assessment" sampai Phase 324 ship

### Opsi C — Rollback Plan 01 commit + restart dengan scope luas
- Tidak recommend — Plan 01 commits clean, AddRange EditLog tetap benar fix

## Recommendation

**Opsi A** — Extend Phase 323 scope. Bug serupa pattern, fix mekanik sama, repro Dev kemungkinan butuh BOTH fixes. Tunda commit Wave 2 sampai patch UserPackageAssignments masuk.

## Status next steps

- DB lokal MASIH ada seed (`AssessmentEditLog WHERE ActorUserId='p323-seed-user'` di Session 2)
- BACKUP file ready untuk RESTORE: `C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\HcPortalDB_Dev-pre323-20260526-165911.bak`
- `dotnet run` task `bsbttwoij` aktif (kill jika perlu)
- Browser session admin aktif
