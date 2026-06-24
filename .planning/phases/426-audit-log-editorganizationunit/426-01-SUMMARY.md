---
phase: 426-audit-log-editorganizationunit
plan: 01
subsystem: api
tags: [audit, organization, traceability, auditlog, csharp, xunit]

# Dependency graph
requires:
  - phase: 403-organization-userunits-aware
    provides: "EditOrganizationUnit cascade UserUnits-aware + counters cascadedUsers/cascadedMappings/cascadedUserUnits + tx.CommitAsync atomic"
provides:
  - "AuditLog ActionType='EditOrganizationUnit' ditulis pada setiap rename/reparent unit (mirror DeleteOrganizationUnit)"
  - "Audit only-on-change (D-01), single combined row (D-02), raw parent IDs (D-03), swallow-on-failure post-commit"
  - "MakeControllerWithUser + FakeUserStore/MakeUserManager factory di OrganizationControllerTests (UserManager-aware controller test harness)"
affects: [427-exam-token-gate, 428-startexam-idempotency, audit, traceability]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Post-commit audit block guarded only-on-change, try/catch swallow (mirror DeleteOrganizationUnit)"
    - "User-aware controller test factory: FakeUserStore + UserManager + ClaimsPrincipal(NameIdentifier) untuk surface yang deref _userManager.GetUserAsync(User)"

key-files:
  created: []
  modified:
    - "Controllers/OrganizationController.cs"
    - "HcPortal.Tests/OrganizationControllerTests.cs"

key-decisions:
  - "D-01 only-on-change: guard if (oldName != name.Trim() || oldParentId != parentId) — no-op edit commit sukses tanpa audit"
  - "D-02 single combined row: satu LogAsync per Edit (rename+reparent gabung tetap 1 baris)"
  - "D-03 raw parent IDs: tulis oldParentId/parentId mentah (null render 'null'), tanpa query DB di blok swallow"
  - "Audit ditempatkan SETELAH tx.CommitAsync() + swallow-on-failure → kegagalan audit tak memblokir respons edit"

patterns-established:
  - "Audit aditif post-commit: guard di luar try (perbandingan murah), try membungkus actor-resolution + LogAsync I/O"
  - "Test user-aware factory disalin verbatim dari RetakeExamEndpointTests untuk controller yang resolve actor via UserManager"

requirements-completed: [AUDIT-01]

# Metrics
duration: 5min
completed: 2026-06-24
---

# Phase 426 Plan 01: Audit-Log EditOrganizationUnit Summary

**Jejak `AuditLog` ActionType="EditOrganizationUnit" aditif post-commit pada rename/reparent unit organisasi — mirror `DeleteOrganizationUnit`, guarded only-on-change, swallow-on-failure, dengan cascade counts di Description. Menutup asimetri traceability pre-existing (Delete ber-audit, Edit tidak).**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-06-24T12:58:39Z
- **Completed:** 2026-06-24T13:02:55Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Blok audit aditif di `EditOrganizationUnit` setelah `await tx.CommitAsync();` — menulis satu baris `AuditLog` hanya saat ada perubahan nyata (rename atau reparent), dengan actor server-resolved `{NIP} - {FullName}`, ringkasan `oldName→newName`, parent IDs mentah `oldParentId→parentId`, dan cascade counts (users/mappings/UserUnits).
- 5 test cases T1-T5 + factory `MakeControllerWithUser` (UserManager-aware) + `FakeUserStore`/`MakeUserManager` disalin dari `RetakeExamEndpointTests.cs:47-99`.
- Semua 19 test `OrganizationControllerTests` hijau (5 baru + 14 regression existing) — swallow-on-failure terbukti melindungi harness null-userManager (T5 + regression).

## Task Commits

Each task was committed atomically:

1. **Task 1: Sisipkan blok audit AUDIT-01 di EditOrganizationUnit** - `ee1f5524` (feat)
2. **Task 2: Tambah factory user + 5 test cases T1-T5** - `3a2f6f1c` (test)

_Catatan: Task 2 ber-`tdd="true"` namun karena implementasi controller (Task 1) sudah commit lebih dulu, fase GREEN langsung tercapai (test hijau saat ditambahkan, tanpa RED commit terpisah). Lihat "TDD Gate Compliance" di bawah._

## Files Created/Modified
- `Controllers/OrganizationController.cs` - +19 baris: blok audit `EditOrganizationUnit` (guard only-on-change + try/catch swallow + `_auditLog.LogAsync(..., "EditOrganizationUnit", ...)`) setelah commit, sebelum `var msg`.
- `HcPortal.Tests/OrganizationControllerTests.cs` - +187 baris: usings tambahan, `FakeUserStore` + `MakeUserManager` + `MakeControllerWithUser` factory, helper `SeedActor()`, dan 5 test T1-T5.

## Decisions Made
None - mengikuti CONTEXT decisions D-01/D-02/D-03 + RESEARCH §3 (kode persis) tanpa deviasi. Format Description dan penempatan blok sesuai spesifikasi plan.

## Deviations from Plan

None - plan executed exactly as written.

Catatan minor (non-deviation): blok dipasang persis seperti template RESEARCH §3; ActionType="EditOrganizationUnit" (19 chars, di bawah MaxLength(50)); guard berada di luar try; tidak ada migration (migration=FALSE — diverifikasi: 0 file migration/snapshot pada diff `HEAD~2..HEAD`); atribut `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` tidak diubah.

## TDD Gate Compliance

Task 2 ditandai `tdd="true"`, namun pola eksekusi adalah: implementasi (Task 1, commit `feat` `ee1f5524`) lebih dulu, lalu test (Task 2, commit `test` `3a2f6f1c`) yang langsung GREEN. Ini sesuai struktur plan (Task 1 = implementasi, Task 2 = test+factory) dan bukan plan-level `type: tdd`. Tidak ada RED commit terpisah karena test ditambahkan setelah implementasi sudah ada — semua 5 test (T1-T5) hijau pada run pertama, dan masing-masing diskriminatif (T4 membuktikan only-on-change menulis 0 baris; T5 membuktikan swallow). Gate fungsional (test-membedakan-perilaku) terpenuhi; urutan commit test-after-impl dicatat sebagai catatan kepatuhan, bukan pelanggaran (gate sequence RED→GREEN tidak wajib untuk plan `type: execute` dengan task tdd individual yang implementasinya di task terpisah).

## Issues Encountered
None. Build 0 error pada run pertama; test 19/19 hijau pada run pertama. Warnings build (`CS86xx` di file Razor/controller lain seperti `ImportTraining.cshtml`, `_TrainingRecordsTab.cshtml`, `AssessmentAdminController.cs`, dan `xUnit2031` di `WorkerDataServiceSearchTests.cs:135`) semuanya pre-existing dan di luar scope task ini — tidak disentuh.

## Verification
- `dotnet build HcPortal.csproj -c Debug` → **0 Error** (24 warning pre-existing, out of scope).
- `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationControllerTests"` → **Passed! Failed: 0, Passed: 19, Skipped: 0**.
- Regression guard SC#4: `EditOrganizationUnit_RenameLevel1_RenamesAllUserUnitsRows`, `...ReparentSingleUnitWorker_Allowed`, `PreviewEditCascade_*` semua tetap GREEN (swallow melindungi harness null-userManager).
- migration=FALSE terverifikasi: `git diff --name-only HEAD~2 HEAD` tidak memuat file migration/snapshot.

## Success Criteria
- SC#1 (AUDIT-01) — rename/reparent → 1 baris ActionType="EditOrganizationUnit" + actor + oldName→newName + parent IDs ✅ (T1, T2, T3)
- SC#2 — cascade counts (users/mappings/UserUnits) di Description ✅ (T1 assert `cascade:`)
- SC#3 — audit post-commit + swallow-on-failure → kegagalan tak memblokir respons ✅ (T5 null userManager)
- SC#4 — edit valid tetap sukses, cascade ph403 utuh, authz/CSRF tak berubah ✅ (regression 14/14 existing)
- D-01 only-on-change → no-op = 0 baris ✅ (T4)
- D-02 single combined row → rename+reparent = 1 baris ✅ (T3)
- D-03 raw parent IDs → `parent 1→5` ✅ (T2)
- migration=FALSE ✅

## Next Phase Readiness
- AUDIT-01 lengkap. Asimetri traceability Edit/Delete tertutup.
- Phase 427 (EXSEC-01 token-gate server-authoritative, migration=TRUE) berikutnya — file-disjoint dari 426 (`CMPController.cs`+`AssessmentSession.cs`); tak ada blocker dari plan ini.
- Pola `MakeControllerWithUser`/`FakeUserStore` kini tersedia di `OrganizationControllerTests` bila surface OrganizationController lain butuh actor-resolution di test mendatang.

## Self-Check: PASSED
- Files: `Controllers/OrganizationController.cs`, `HcPortal.Tests/OrganizationControllerTests.cs`, `426-01-SUMMARY.md` — semua FOUND.
- Commits: `ee1f5524` (feat), `3a2f6f1c` (test) — semua FOUND.
- Key links: `_auditLog.LogAsync(..., "EditOrganizationUnit", ...)` + test `EditOrganizationUnit_NoChange_WritesZeroAuditRows` — FOUND.

---
*Phase: 426-audit-log-editorganizationunit*
*Completed: 2026-06-24*
