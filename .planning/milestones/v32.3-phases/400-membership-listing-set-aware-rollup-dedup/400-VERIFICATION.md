---
phase: 400-membership-listing-set-aware-rollup-dedup
verified: 2026-06-18T20:00:00+08:00
status: passed
score: 6/6
overrides_applied: 0
---

# Phase 400: Membership Listing Set-Aware + Rollup Dedup — Verification Report

**Phase Goal:** Pekerja multi-unit muncul di TIAP unit-nya pada listing keanggotaan (roster tim/section, role-filter, tabel CMP records) — set-aware (bukan hanya `u.Unit==unitFilter` scalar); rollup tingkat Bagian DEDUP (completion%/denominator tidak hitung pekerja ganda); CMP analytics/renewal TIDAK diubah (D1=b primary). 0 migration.
**Verified:** 2026-06-18T20:00:00+08:00
**Status:** passed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Pekerja anggota 2 unit {X,Y} muncul saat difilter unit-X DAN unit-Y (set-aware, bukan hanya primary) | VERIFIED | `WorkerDataService.cs:258-259` correlated subquery `_context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive)` di 3 lokasi. Test `MultiUnitWorker_AppearsInBothUnitFilters_SetAware` hijau. UAT runtime localhost:5277 filter "RFCC NHT (053)" non-primary Iwan TETAP muncul. |
| 2 | Tanpa filter unit, pekerja {X,Y} muncul sebagai TEPAT 1 baris (dedup by-construction — completion%/denominator tidak ganda) | VERIFIED | `.Any()` boolean subquery menghasilkan 1 row/user (no JOIN fan-out). TANPA `.Distinct()` di path filter unit. Test `MultiUnitWorker_SingleRow_NoFilter` hijau. UAT: Iwan tampil 1 baris (bukan 2) saat tanpa filter unit di /CMP/Records Team View. |
| 3 | Unit yang di-deactivate (IsActive=false, jalur MU-07) TIDAK muncul di roster maupun kolom Unit (D-03) | VERIFIED | Predikat `&& uu.IsActive` di semua 3 lokasi + batch-load `unitsByUser` filter `&& uu.IsActive`. Test `InactiveUnit_ExcludedFromFilter_D03` hijau (unitFilter "UnitY" deactivated → Empty). |
| 4 | Kolom Unit tabel CMP records team kontekstual: saat difilter unit-X tampil "X", tanpa filter tampil semua unit aktif primary-first comma-join "X, Y" (D-02) | VERIFIED | `WorkerDataService.cs:363-367` ternary `unitFilter ?? string.Join(", ", uList) ?? user.Unit`. Test `UnfilteredColumn_AllActiveUnits_PrimaryFirst_D02` (Unit="UnitY, UnitX") + `FilteredColumn_ShowsUnitFilter_D02` (Unit="UnitY") hijau. UAT: kolom kontekstual benar di runtime. |
| 5 | Pekerja 0 unit aktif (legacy/Unit=null) fallback ke user.Unit, view render "---" bila null (D-05) | VERIFIED | Fallback `(user.Unit ?? "")` di `WorkerDataService.cs:367`. Test `ZeroUnit_Fallback_D05` hijau (0 baris UserUnits aktif → `r[0].Unit == "Legacy"`). |
| 6 | CMP analytics/renewal + Team View list (tanpa unitFilter) TIDAK berubah perilakunya — 0 drift D1=b (SC#3) | VERIFIED | `git diff 085dd10b HEAD -- Controllers/CMPController.cs Views/CMP/_RecordsTeamBody.cshtml Models/WorkerTrainingStatus.cs Services/IWorkerDataService.cs HcPortal.Tests/FakeWorkerDataService.cs` = 0 baris. CMPController `:2581/:2589` masih scalar `s.User!.Unit == unit` (diintensikan D1=b). Team View call `:543` (no unitFilter) tidak terdampak predikat. Test `Scope_Null_NoFilter_BackwardCompat` hijau. |

**Score:** 6/6 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Services/WorkerDataService.cs` | Predikat unit set-aware (correlated subquery `_context.UserUnits`) + kolom Unit kontekstual + batch-load dict `unitsByUser` | VERIFIED | Baris 257-259: correlated subquery aktif. Baris 279-286: batch-load dict `unitsByUser` active-only primary-first. Baris 363-367: ternary kontekstual. Dikonfirmasi commit `520058b8`. |
| `Controllers/WorkerController.cs` | Predikat unit set-aware di ManageWorkers (:204-208) + ExportWorkers (:305-307) — predicate-only, display tidak diubah | VERIFIED | Keduanya menggunakan `_context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive)`. Scalar lama dihapus (grep `query.Where(u => u.Unit == unitFilter)` = no match). `userUnitsDict` ViewBag, validasi unit-vs-section, badge display tidak berubah. |
| `HcPortal.Tests/WorkerDataServiceSearchTests.cs` | ~7 test MU-06 (set-aware both-units, dedup, IsActive D-03, kontekstual filtered/unfiltered D-02, fallback D-05) | VERIFIED | 6 test MU-06 ada: `MultiUnitWorker_AppearsInBothUnitFilters_SetAware`, `MultiUnitWorker_SingleRow_NoFilter`, `InactiveUnit_ExcludedFromFilter_D03`, `UnfilteredColumn_AllActiveUnits_PrimaryFirst_D02`, `FilteredColumn_ShowsUnitFilter_D02`, `ZeroUnit_Fallback_D05`. Plus regresi `Scope_Null_NoFilter_BackwardCompat` = SC#3. Commit `24a71b7f`. |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `WorkerDataService.cs:258-259` GetWorkersInSection predikat | Junction `UserUnits` (Phase 399) | Correlated subquery `.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive)` | WIRED | Pattern terkonfirmasi di file. EF Core translate ke SQL EXISTS. PITFALL #1 dihindari (nav-prop tidak dipakai). |
| `WorkerDataService.cs:363-367` assign Unit | `unitsByUser` dict (batch-load active-only, primary-first) | Ternary `unitFilter ?? string.Join(", ", uList) ?? user.Unit` | WIRED | `string.Join(", ", uList)` terkonfirmasi di file. Dict `unitsByUser` dibuat di baris 279-286 sebelum foreach. |
| Consumer #4 `AssessmentAdminController.cs:278` ManageAssessmentTab | `GetWorkersInSection(section, unit, ...)` | Meneruskan `unit` → mewarisi set-aware otomatis (no code change) | WIRED | grep terkonfirmasi `fullList = await _workerDataService.GetWorkersInSection(section, unit, ...)` di baris 278. No code change diperlukan — set-aware diperoleh otomatis. |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|-------------------|--------|
| `WorkerDataService.cs` GetWorkersInSection | `unitsByUser` dict | `_context.UserUnits.Where(uu => userIds.Contains(uu.UserId) && uu.IsActive).ToListAsync()` | Ya — query EF Core terhadap junction `UserUnits` (Phase 399, backfill 399 = 0 anomali) | FLOWING |
| `WorkerDataService.cs` GetWorkersInSection | `WorkerTrainingStatus.Unit` | Ternary dari `unitsByUser` dict (pre-populated) | Ya — driven dari dict real DB, bukan hardcoded | FLOWING |
| `WorkerDataService.cs` GetWorkersInSection | `usersQuery` (predikat set-aware) | `_context.UserUnits.Any(...)` correlated subquery → SQL EXISTS di SQL Server | Ya — anomali-backfill check 0 baris di HcPortalDB_Dev | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Command / Method | Result | Status |
|----------|-----------------|--------|--------|
| 6 test MU-06 hijau | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~WorkerDataServiceSearchTests"` | 17/17 pass (SUMMARY.md) | PASS |
| Full suite 0 regresi | `dotnet test HcPortal.Tests` | 507 passed / 0 failed / 3 skipped (3 = SQLEXPRESS-gated Phase 404) | PASS |
| Build 0 error (PITFALL #1 guard) | `dotnet build` | 0 error (commit `520058b8`) | PASS |
| UAT runtime filter non-primary muncul | localhost:5277 /CMP/Records filter "RFCC NHT (053)" untuk user Iwan | Iwan TETAP muncul (set-aware SQL EXISTS translation di SQL Server riil) | PASS |
| Kolom Unit kontekstual runtime | unfiltered = "Alkylation Unit (065), RFCC NHT (053)"; filtered Alkylation = "Alkylation Unit (065)"; filtered RFCC = "RFCC NHT (053)" | Sesuai D-02 (SUMMARY.md Task 3 UAT) | PASS |
| Dedup runtime (1 baris/pekerja) | Iwan muncul 1 baris saat tanpa filter unit, total GAST = 7 (tidak menggelembung) | 1 baris, total count akurat | PASS |
| No-drift D1=b (analytics tidak berubah) | git diff 085dd10b HEAD -- CMPController.cs etc. | 0 baris diff di 5 file protected | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| MU-06 | 400-01-PLAN.md | Listing keanggotaan set-aware — pekerja multi-unit muncul di tiap unit-nya; rollup Bagian dedup | SATISFIED | 3 predikat set-aware + kolom kontekstual + 6 test MU-06 hijau + UAT runtime PASS. REQUIREMENTS.md baris 19: `[x] MU-06`. |

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| (none) | — | Tidak ada TODO/FIXME/placeholder/empty handler ditemukan di 3 file modified | — | — |

Catatan pemeriksaan:
- `Services/WorkerDataService.cs` `.Distinct(` di baris 466: pre-existing, untuk deduplicate recipient HC+Admin (bukan jalur filter unit) — BUKAN hasil Phase 400.
- `Controllers/WorkerController.cs` `.Distinct(` × 3: semua pre-existing (ParseUnits helper, unit cleaned set, coacheeIds cascade) — BUKAN jalur filter unit Phase 400.
- Tidak ada `.Distinct()` yang ditambahkan di jalur filter unit (PLAN larangan terpenuhi).

---

### Human Verification Required

Tidak ada item yang memerlukan verifikasi manusia lebih lanjut. UAT lokal runtime (Task 3 checkpoint) sudah dilaksanakan oleh orchestrator via Playwright + SQL fixture snapshot→seed→RESTORE dan disetujui (`user_response: "approved"`):

- Filter non-primary "RFCC NHT (053)" → Iwan TETAP muncul (set-aware SQL EXISTS di SQL Server riil ✓)
- Kolom Unit kontekstual runtime benar (filtered/unfiltered ✓)
- Dedup 1 baris/pekerja (no menggelembung ✓)
- No-drift D1=b analytics identik ✓
- DB lokal di-restore + SEED_JOURNAL cleaned ✓

---

### Gaps Summary

Tidak ada gaps. Semua 6 must-have truths terverifikasi.

---

## Summary

Phase 400 mencapai tujuannya secara penuh:

1. **3 predikat set-aware** terpasang di `WorkerDataService.GetWorkersInSection` + `WorkerController.ManageWorkers` + `WorkerController.ExportWorkers` — semua via correlated subquery `_context.UserUnits.Any(...)` (bukan scalar `u.Unit == unitFilter`).
2. **Dedup by-construction** — `.Any()` boolean subquery menghasilkan tepat 1 baris/user (no fan-out), tanpa `.Distinct()`.
3. **Kolom Unit kontekstual D-02** + batch-load dict `unitsByUser` di `GetWorkersInSection`.
4. **6 test MU-06** + regresi existing semua hijau (507/0/3 full suite; 3 skip = Phase 404 SQLEXPRESS-gated).
5. **0 drift D1=b** — 5 file protected (CMPController.cs, _RecordsTeamBody.cshtml, WorkerTrainingStatus.cs, IWorkerDataService.cs, FakeWorkerDataService.cs) tidak berubah sejak Phase 399.
6. **0 migration** dikonfirmasi; UAT runtime approved.

REQ MU-06 SATISFIED. Semua 4 Success Criteria ROADMAP terpenuhi.

---

_Verified: 2026-06-18T20:00:00+08:00_
_Verifier: Claude (gsd-verifier)_
