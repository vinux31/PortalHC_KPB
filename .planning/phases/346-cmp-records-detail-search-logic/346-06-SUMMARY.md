---
phase: 346-cmp-records-detail-search-logic
plan: 06
subsystem: CMP/Records tests + UAT
tags: [xunit, playwright, uat, seed-workflow, security-test]
requires: [346-01, 346-02, 346-03, 346-04, 346-05]
provides: ["authz matrix tests", "searchScope + include-pending tests", "Playwright UAT spec", "browser-verified Phase 346"]
affects: ["HcPortal.Tests/ResultsAuthorizationTests.cs", "HcPortal.Tests/WorkerDataServiceSearchTests.cs", "tests/e2e/cmp-records-346.spec.ts", "tests/sql/cmp346-seed.sql", "docs/SEED_JOURNAL.md", "Controllers/AssessmentAdminController.cs", "Views/Admin/Shared/_HistoryTab.cshtml"]
tech-stack:
  added: []
  patterns: ["pure-static [Theory] authz matrix", "InMemory DbContext service test", "SEED_WORKFLOW snapshot/restore"]
key-files:
  created: ["HcPortal.Tests/ResultsAuthorizationTests.cs", "HcPortal.Tests/WorkerDataServiceSearchTests.cs", "tests/e2e/cmp-records-346.spec.ts", "tests/sql/cmp346-seed.sql"]
  modified: ["docs/SEED_JOURNAL.md", "Controllers/AssessmentAdminController.cs", "Views/Admin/Shared/_HistoryTab.cshtml"]
key-decisions:
  - "REC-04 test = pure-static IsResultsAuthorized [Theory] (mirror Phase 345 ComputeHistoryStats); REC-06/07 = InMemory"
  - "Human-verify checkpoint executed by Claude via Playwright MCP (per user); 9/9 REC browser-verified"
  - "T-346-UAT-01 finding fixed in-scope: UserAssessmentHistory WHERE + _HistoryTab badge (pulls MAP-20 forward)"
requirements-completed: [REC-04, REC-06, REC-07]
duration: 95 min
completed: 2026-06-04
---

# Phase 346 Plan 06: Tests + UAT Summary

Regression-proof + end-to-end verifikasi Phase 346. xUnit (authz matrix + searchScope + include-pending) + Playwright UAT spec + human-verify UAT (Claude via Playwright MCP) + fix temuan UAT.

**Tasks:** 3 (2 auto + 1 human-verify checkpoint) | **Commits:** `~76xx` xUnit + `~Playwright` spec + `ce13e6a8` journal + `79db6ddc` finding-fix + `deaa2b55` journal

## What was built

- **Task 1 — xUnit:** `ResultsAuthorizationTests.cs` (11 `[InlineData]` matrix untuk `IsResultsAuthorized`: owner/Admin/HC/L3/L4-same/L4-other/L4-null/L4-empty/L5/L6/roleLevel-0) + `WorkerDataServiceSearchTests.cs` (InMemory: searchScope Nama/Training/Keduanya/Null + GetUnifiedRecords include-PendingGrading + exclude-other-status). **`dotnet test` → 76/76 PASS.**
- **Task 2 — Playwright:** `tests/e2e/cmp-records-346.spec.ts` (6 test, SEED_WORKFLOW backup→seed→restore-afterAll, pola assessment-pending-grade.spec.ts) + `tests/sql/cmp346-seed.sql` (sesi pure `Status='Menunggu Penilaian'`). Spec compiles (`--list` 6 test).
- **Task 3 — Human-verify (Claude via Playwright MCP, per user):** UAT live di `localhost:5277` (env Development, DB HcPortalDB_Dev), seed sesi 162 → verify → RESTORE.

## UAT Results (browser-verified) — 9/9 REC PASS

| REQ | Hasil |
|-----|-------|
| REC-01 | My Records kolom Aksi (7) + "Lihat Hasil"→/CMP/Results/157 ✓ |
| REC-02 | `#trainingDetailModal` 11 field + PDF toggle ✓ |
| REC-03 | Worker Detail "Lihat Hasil" un-gated tiap assessment; cert row = Lihat Hasil + Sertifikat (AUTHZ-01) ✓ |
| REC-04 🔐 | L1 OK · L4 same-section (GAST→rino) OK · **L4 cross-section (GAST→Section-NULL) → Akses Ditolak** ✓ |
| REC-05 | Modal Kategori "Mandatory HSSE" + Sub Kategori "Gas Tester" ✓ |
| REC-06 | Search "rino"/Nama→1 worker; "k3"/Training→2 worker (by Judul); export href `?search=&searchScope=` ✓ |
| REC-07 | Sesi [PENDING346] muncul "Menunggu Penilaian" di Worker Detail ✓ |
| REC-08 | dateFrom>dateTo → warning "Tanggal Awal lebih besar..." ✓ |
| REC-09 | Header Team View "Assessment Lulus" ✓ |

## UAT Finding + Fix — T-346-UAT-01 (in-scope resolution, user-approved)

**Temuan:** sesi pure `Status='Menunggu Penilaian'` tidak muncul/mislabel di admin-history surfaces (`/Admin/UserAssessmentHistory` + ManageAssessment Tab3) — query pakai `AssessmentAdminController`, **di luar scope `WorkerDataService` REC-07**.

**Fix (commit `79db6ddc`):**
- (A) `AssessmentAdminController.UserAssessmentHistory` WHERE: `+ OR Status==AssessmentConstants.AssessmentStatus.PendingGrading`. VM/ComputeHistoryStats sudah exclude-pending (Phase 345) → passRate/avg unchanged, Total +1, indikator "Menunggu Penilaian: N".
- (B) `_HistoryTab.cshtml` badge: null IsPassed → amber "Menunggu Penilaian" (was `—`); pulls **MAP-20 forward** (rows sudah datang via REC-07 GetAllWorkersHistory).

**Re-verified browser:** UserAssessmentHistory row (Total 20→21) + Tab3 History row keduanya "Menunggu Penilaian"; graded rows unchanged.

## Verification

- `dotnet build` 0 error · `dotnet test HcPortal.Tests` **76/76** · `npx playwright --list` 6 test compile.
- Browser UAT 9/9 REC + finding-fix PASS (Playwright MCP).
- SEED_WORKFLOW: 2× snapshot→seed→verify→RESTORE (1922 pages, Layer 4 pending346=0); journal `cleaned`.

## Deviations from Plan

**[Rule 4 - user-approved scope extension] T-346-UAT-01 fix** — UAT menemukan admin-history surfaces tak include pending (di luar REC-07 WorkerDataService scope). User approve "tangani temuan dulu". Fixed UserAssessmentHistory WHERE + _HistoryTab badge. Pulls MAP-20 (Phase 349) forward → Phase 349 drop MAP-20.

**[Environment] launch profile** — initial `dotnet run --no-launch-profile` defaulted ke Production env (placeholder connection string). Restarted `ASPNETCORE_ENVIRONMENT=Development`. Bukan isu kode.

**Total deviations:** 1 scope-extension (user-approved) + 1 env-note. **Impact:** finding fully resolved + verified; MAP-20 covered.

## Self-Check: PASSED

- 4 test/spec files created + 2 fix files modified ✓ · 76/76 test ✓ · build 0 ✓ · 9/9 REC browser-verified ✓ · SEED restored ✓.

## Notes

- **MAP-20 (Phase 349) now COVERED** — flag for Phase 348/349 planning: drop MAP-20 / mark covered.
- Playwright spec belum di-run as suite (env-heavy); UAT dilakukan manual via MCP (lebih thorough untuk RBAC/visual). Spec tersedia + compiles untuk CI future.
