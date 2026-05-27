# Plan 325-04 SUMMARY — P05 FK quick patch 3 endpoint delete

**Status:** COMPLETE
**Wave:** 2
**Commits:** `bea6cb6e` (TrainingAdmin) + `9d2ffe99` (AssessmentAdmin)
**Date:** 2026-05-27

## Endpoints Patched

| Endpoint | File | Entity | FK column | Line (pre/post) |
|----------|------|--------|-----------|------|
| `DeleteTraining(id)` | `TrainingAdminController.cs` | TrainingRecord | `RenewsTrainingId == record.Id` | 507-528 → 507-555 |
| `DeleteManualAssessment(id)` | `TrainingAdminController.cs` | **AssessmentSession** (CORRECTION 2) | `RenewsSessionId == session.Id` | 736-758 → 736-781 |
| `DeleteAssessment(id)` | `AssessmentAdminController.cs` | AssessmentSession | `RenewsSessionId == id` | 2010-2186 → 2010-2204 |

## FK Column Distinction (CRITICAL pitfall RESEARCH §Pitfall 2)

| Entity dihapus | FK column anak | Children scan |
|---------------|----------|---------------|
| TrainingRecord | `RenewsTrainingId` | `TR.RenewsTrainingId == id` + `AS.RenewsTrainingId == id` |
| AssessmentSession | `RenewsSessionId` | `TR.RenewsSessionId == id` + `AS.RenewsSessionId == id` |

## Pattern (verbatim D-04/D-05/D-06)

```csharp
try
{
    // D-04/D-11: pre-check referencing rows SEBELUM hapus
    var refTr = await _context.TrainingRecords.CountAsync(t => t.{FK} == id);
    var refAs = await _context.AssessmentSessions.CountAsync(a => a.{FK} == id);

    if (refTr + refAs > 0)
    {
        var total = refTr + refAs;
        TempData["Error"] = $"Tidak bisa hapus: {total} sertifikat lain "
                          + "menggunakan record ini sebagai sumber renewal. "
                          + "Hapus atau update sertifikat pemakai terlebih dulu.";
        return RedirectToAction(...);
    }

    // ... existing physical file delete + EF Remove + SaveChanges + audit log
}
catch (DbUpdateException ex)
{
    // D-05: safety net TOCTOU race
    _logger.LogWarning(ex, "Delete failed for {Entity} {Id}", id);
    TempData["Error"] = "Gagal hapus: ada constraint database yang dilanggar.";
}
return RedirectToAction(...);
```

## Preservation Guarantees

- **Phase 312 WR-01 tx scope** `BeginTransactionAsync` di `DeleteAssessment` INTACT (line 2056 post-edit). Pre-check D-11 inserted DI LUAR tx.
- **Phase 323 cascade body** line 2076-2133 (AssessmentEditLogs + PackageUserResponses + AttemptHistory + AssessmentPackages cascade) INTACT.
- **Existing `catch (Exception ex)`** di `DeleteAssessment` line 2188 PRESERVED — `catch (DbUpdateException)` prepended di atas (specific-before-general C# idiom RESEARCH CORRECTION 5).
- **Race window file delete** (Phase 325 RESEARCH §Risk 1): physical file delete INSIDE try block. Acceptable trade-off — Portal HC traffic profile rendah, file restorable dari backup harian IT.

## Threat Mitigation

| Threat | Severity | Status |
|--------|----------|--------|
| T-325-03 unhandled DB 500 leak | MED | MITIGATED — pre-check + dual catch + friendly TempData |
| T-325-05 TOCTOU race | LOW | ACCEPTABLE — race window sempit, catch DbUpdateException sebagai safety net (RESEARCH §Risk 1 line 541-547) |

## Verification

```
grep -nE "RenewsTrainingId == record.Id|RenewsTrainingId == id" TrainingAdminController.cs  → 2 match
grep -nE "RenewsSessionId == session.Id|RenewsSessionId == id"  TrainingAdminController.cs  → 2 match
grep -nE "RenewsSessionId == id"  AssessmentAdminController.cs (DeleteAssessment scope) → 2 match
grep -c "catch (DbUpdateException"  TrainingAdminController.cs   → 2 (DeleteTraining + DeleteManualAssessment)
grep -c "catch (DbUpdateException"  AssessmentAdminController.cs → 3 (1 new DeleteAssessment + 2 existing pre-325)
grep -c "Tidak bisa hapus:" TrainingAdminController.cs  → 2 (D-06 verbatim)
grep -c "Tidak bisa hapus:" AssessmentAdminController.cs → 1 (D-06 verbatim)
grep "Gagal hapus: ada constraint database yang dilanggar." → 3 total (D-06 verbatim)
dotnet build HcPortal.sln  → 0 error, 23 pre-existing warning + 1 minor CS8601 di TempData (non-blocker)
dotnet test HcPortal.Tests → Passed: 7, Skipped: 0, Failed: 0
```

## Success Criteria

- **SC-4** Delete dengan referencing → pre-check trigger TempData error: pending Plan 05 UAT
- **SC-5** Delete tanpa referencing → sukses normal: pending Plan 05 UAT
- T-325-03 mitigated (MED)
- T-325-05 documented acceptable (LOW)

## Handoff Plan 05

Manual UAT batch akhir Phase 325 via Postman + browser + SEED_WORKFLOW snapshot:
- **SC-1** path traversal: Postman upload `../../test.pdf` → verify file system saved sebagai `{ts}_{guid}_test.pdf` (flat, tidak escape `wwwroot/uploads/certificates/`)
- **SC-2** magic byte reject: Postman upload `.exe→.pdf` → 400/redirect dengan error magic byte
- **SC-3** valid format: browser upload PDF/JPG/PNG normal → sukses
- **SC-4** FK pre-check: seed renewal chain → delete parent → TempData error
- **SC-5** sukses delete: orphan record → delete → sukses
