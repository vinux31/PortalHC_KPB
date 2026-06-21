---
phase: 400-membership-listing-set-aware-rollup-dedup
plan: 01
subsystem: api
tags: [efcore, correlated-subquery, exists, multi-unit, cmp-records, worker-listing, dedup, sql-server]

# Dependency graph
requires:
  - phase: 399-foundation-userunits-junction
    provides: "junction UserUnits (UserId/Unit/IsPrimary/IsActive) + filtered-unique index primary + backfill 1 primary-row/pekerja + kontrak write-through primary-mirror"
provides:
  - "Predikat unit set-aware (correlated subquery _context.UserUnits.Any) di 3 lokasi: WorkerDataService.GetWorkersInSection + WorkerController.ManageWorkers + WorkerController.ExportWorkers"
  - "Kolom Unit kontekstual D-02 di tabel CMP records team (filtered -> matched unit; unfiltered -> all-active primary-first comma-join; 0-unit -> fallback user.Unit)"
  - "Batch-load dict unitsByUser (1 query active-only primary-first, no N+1) di GetWorkersInSection"
  - "Dedup by-construction tingkat Bagian (1 baris/pekerja via .Any() boolean subquery, TANPA .Distinct())"
affects: [401-proton-unit-resolution-hardening, 402-coaching-cross-unit, 404-test-sql-riil-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Correlated EXISTS subquery untuk keanggotaan set-aware (pengganti scalar equality) — _context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive)"
    - "Kolom value-driven kontekstual (markup view byte-stable, perilaku digerakkan dari service)"
    - "Batch-load dict gaya CMP-25 (GroupBy->ToDictionary primary-first) untuk hindari N+1"

key-files:
  created: []
  modified:
    - "Services/WorkerDataService.cs — predikat set-aware + batch-load dict unitsByUser + assign Unit kontekstual"
    - "Controllers/WorkerController.cs — predikat set-aware di ManageWorkers + ExportWorkers (predicate-only, D-06 display tak diubah)"
    - "HcPortal.Tests/WorkerDataServiceSearchTests.cs — 6 test MU-06 (set-aware both-units, dedup, IsActive D-03, kontekstual filtered/unfiltered D-02, fallback D-05)"

key-decisions:
  - "Anomali-backfill check pada HcPortalDB_Dev = 0 baris -> .Any() MURNI final (TIDAK menambah OR-scalar-fallback; backfill 399 lengkap)"
  - "Dedup by-construction via .Any() boolean subquery (1 row/user, no fan-out) — TANPA .Distinct() (RESEARCH/PATTERNS lean)"
  - "PITFALL #1 dihindari: pakai _context.UserUnits (correlated subquery), BUKAN u.UserUnits (nav-prop tak ada, CS1061)"
  - "Consumer #4 AssessmentAdminController:278 (ManageAssessmentTab) mewarisi set-aware OTOMATIS (no code change)"
  - "Path analytics CMPController (:2581/:2589 scalar mirror + Team View :543 no-filter) TIDAK disentuh -> no-drift D1=b (SC#3)"

patterns-established:
  - "Set-aware membership filter: correlated EXISTS terhadap junction UserUnits dengan && uu.IsActive (D-03 exclude deactivated)"
  - "Kolom Unit kontekstual: ternary unitFilter ?? string.Join(\", \", uList primary-first) ?? user.Unit"

requirements-completed: [MU-06]

# Metrics
duration: ~95min (lintas Task 1-2 RED/GREEN + checkpoint UAT)
completed: 2026-06-18
---

# Phase 400 Plan 01: Membership Listing Set-Aware + Rollup Dedup Summary

**Filter unit listing pekerja diubah dari scalar `u.Unit == unitFilter` (hanya primary) menjadi keanggotaan set-aware via correlated EXISTS terhadap junction `UserUnits`, sehingga pekerja anggota >1 unit dalam 1 Bagian muncul di tiap unit-nya — dengan kolom Unit kontekstual D-02 dan dedup rollup Bagian by-construction (tanpa `.Distinct()`).**

## Performance

- **Duration:** ~95 min (Task 1 RED + Task 2 GREEN + checkpoint UAT lokal)
- **Started:** 2026-06-18 (Task 1-2 commit)
- **Completed:** 2026-06-18
- **Tasks:** 3 (2 kode + 1 checkpoint UAT)
- **Files modified:** 3

## Accomplishments
- **SC#1 set-aware:** 3 predikat unit (`GetWorkersInSection` + `ManageWorkers` + `ExportWorkers`) jadi correlated subquery `_context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive)` — pekerja {X,Y} muncul saat difilter unit-X DAN unit-Y (termasuk unit non-primary).
- **SC#2 dedup:** rollup tingkat Bagian dedup by-construction (`.Any()` boolean subquery = 1 row/user, no fan-out) — TANPA `.Distinct()`; completion%/denominator tidak menggelembung.
- **Kolom Unit kontekstual D-02:** tabel CMP records team value-driven — filtered tampil unit yang dicocokkan, tanpa filter tampil semua unit aktif primary-first comma-join, 0-unit fallback `user.Unit` (D-05). Markup `_RecordsTeamBody.cshtml` TIDAK berubah.
- **Batch-load dict D-04:** `unitsByUser` (1 query active-only primary-first) di `GetWorkersInSection`, hindari N+1.
- **SC#3 no-drift D1=b:** path analytics (`CMPController:2581/:2589`) + Team View call (`:543` no-filter) TIDAK disentuh — perilaku & angka analytics identik.

## Task Commits

Masing-masing task di-commit atomik (TDD RED -> GREEN):

1. **Task 1 (Wave-0 RED): 6 unit test MU-06** — `24a71b7f` (test) — `HcPortal.Tests/WorkerDataServiceSearchTests.cs`
2. **Task 2 (GREEN): set-aware predicate 3 site + kolom Unit kontekstual + batch-load dict** — `520058b8` (feat) — `Services/WorkerDataService.cs`, `Controllers/WorkerController.cs`
3. **Task 3 (checkpoint:human-verify UAT lokal):** APPROVED — bookkeeping checkpoint `b6defa31` (STATE.md); seed journal cleaned `e203c9ad`

**Plan metadata:** (final commit ini — docs: finalize plan 01)

_Note: TDD task RED/GREEN = 2 commit; tidak ada perubahan REFACTOR._

## Files Created/Modified
- `Services/WorkerDataService.cs` — Predikat unit scalar diganti correlated subquery set-aware (active-only); sisip batch-load dict `unitsByUser` (active-only, primary-first, GroupBy->ToDictionary); assign `Unit` kontekstual (filtered=unitFilter / unfiltered=comma-join primary-first / fallback user.Unit).
- `Controllers/WorkerController.cs` — Predikat unit set-aware di `ManageWorkers` + `ExportWorkers` (predicate-only; display/badge/userUnitsDict/validasi unit-vs-section TIDAK diubah, D-06).
- `HcPortal.Tests/WorkerDataServiceSearchTests.cs` — 6 test MU-06: `MultiUnitWorker_AppearsInBothUnitFilters_SetAware`, `MultiUnitWorker_SingleRow_NoFilter`, `InactiveUnit_ExcludedFromFilter_D03`, `UnfilteredColumn_AllActiveUnits_PrimaryFirst_D02`, `FilteredColumn_ShowsUnitFilter_D02`, `ZeroUnit_Fallback_D05` (+ regresi existing `Scope_Null_NoFilter_BackwardCompat` = SC#3 no-drift).

## Decisions Made
- **Anomali-backfill check = 0 → `.Any()` MURNI final.** Query `SELECT COUNT(*) FROM Users u WHERE u.Unit IS NOT NULL AND NOT EXISTS(SELECT 1 FROM UserUnits uu WHERE uu.UserId=u.Id AND uu.IsActive=1)` pada `HcPortalDB_Dev` mengembalikan **0** (backfill Phase 399 lengkap). Keputusan: TIDAK menambah OR-scalar-fallback (`u.Unit == unitFilter OR ...`) — invariant mirror + backfill membuatnya redundan (sesuai lean RESEARCH Open Q2). Bila >0 akan eskalasi, tapi tidak terjadi.
- **Dedup by-construction (tanpa `.Distinct()`).** `.Any()` subquery boolean menghasilkan tepat 1 baris/user (no fan-out) — `.Distinct()` tak perlu dan dilarang.
- **Consumer #4 (AssessmentAdminController:278 / ManageAssessmentTab) mewarisi set-aware OTOMATIS** — meneruskan `unit` ke `GetWorkersInSection`, no code change (terverifikasi di kode).
- **PITFALL #1 dihindari:** `_context.UserUnits.Any(...)` correlated subquery (nav-prop `u.UserUnits` tak ada karena `.WithMany()` tanpa argumen — CS1061 jika dipakai).

## Deviations from Plan

None - plan executed exactly as written. (3 predikat set-aware + kolom kontekstual D-02 + batch-load dict sesuai action A-E; tidak ada `.Distinct()`, tidak ada nav-prop, tidak ada OR-fallback, tidak ada perubahan analytics/view/model/interface.)

## Issues Encountered
None - RED gagal-assert sesuai harapan (predikat scalar membuat filter unit non-primary kosong + kolom masih `user.Unit`); GREEN membuat 6 test MU-06 + regresi existing hijau.

## UAT Evidence (Task 3 — checkpoint:human-verify, APPROVED)

UAT didorong orchestrator via Playwright + SQL (snapshot->seed->RESTORE), patuh CLAUDE.md Develop Workflow + Seed Workflow. migration=FALSE.

1. **Anomali-backfill query** `HcPortalDB_Dev` → **0 baris** → `.Any()` murni FINAL.
2. **Build + full suite:** `dotnet build` 0 error; `dotnet test HcPortal.Tests` → **507 passed / 0 failed / 3 skipped** (3 skip = `UserUnitsBackfillIntegrationTests` SQLEXPRESS-gated, milik Phase 404; baseline >=366 terlampaui, 0 regresi). Filter `~WorkerDataServiceSearchTests` → 17/17.
3. **Fixture (Seed Workflow, journal cleaned `e203c9ad`):** user "Iwan" (Bagian GAST, primary "Alkylation Unit (065)") + 1 baris UserUnits sekunder aktif "RFCC NHT (053)". Snapshot `HcPortalDB_Dev_pre400uat.bak` → seed → RESTORE WITH REPLACE (UserUnits kembali 6 baseline; Iwan=1 baris terverifikasi; .bak dihapus; `docs/SEED_JOURNAL.md` cleaned).
4. **Runtime UAT (localhost:5277, admin terotentikasi, /CMP/RecordsTeamPartial) — semua PASS:**
   - Tanpa filter unit → Iwan **1 baris** (dedup ✓); kolom Unit = **"Alkylation Unit (065), RFCC NHT (053)"** (primary-first comma-join, D-02 ✓). Total GAST = 7 baris.
   - Filter "Alkylation Unit (065)" (primary) → Iwan ada; kolom Unit = "Alkylation Unit (065)" (D-02 ✓). Total = 6 (tak berubah — no-drift).
   - Filter "RFCC NHT (053)" (NON-primary) → Iwan **TETAP ada** (SET-AWARE ✓ — pre-fix scalar tidak akan match); kolom Unit = "RFCC NHT (053)". Total = 1.
   - SQL `EXISTS` translation benar di runtime SQL Server riil (bukan false-pass in-memory).
   - **No-drift D1=b (SC#3):** analytics + Team View (no-unit-filter) row count tak terpengaruh; hanya cell kontekstual Iwan + membership filter-RFCC yang berubah.
5. **Consumer #4** AssessmentAdminController:278 mewarisi set-aware otomatis (no code change) — terkonfirmasi di kode.

Semua 4 Success Criteria + D-01..D-06 terverifikasi. **Checkpoint user_response: "approved".**

## TDD Gate Compliance
- RED gate: `24a71b7f` (test — 6 test MU-06 gagal sesuai harapan dengan predikat scalar).
- GREEN gate: `520058b8` (feat — set-aware + kontekstual; 6 test + regresi hijau).
- REFACTOR: tidak diperlukan (implementasi sudah bersih sesuai action plan).

## Next Phase Readiness
- **Phase 400 = 1/1 plan COMPLETE** → siap `/gsd-verify-work` / lanjut Wave-1 paralel.
- **Wave-1 paralel {401, 403}** (depends 399; cluster file disjoint) dapat dieksekusi via git worktree — 400 sudah selesai.
- **migration = FALSE.** Reminder: saat milestone v32.3 di-push, **notify IT migration=FALSE untuk Phase 400** (satu-satunya migration v32.3 = Phase 399 `AddUserUnitsTable` `fc015f4d`).
- **Defer ke Phase 404:** test SQL-real EXISTS translation + pagination count di SQLEXPRESS + UAT browser penuh (sebagian sudah dilakukan di UAT Task 3 ini, formalisasi penuh di 404).

## Self-Check: PASSED

- FOUND: `Services/WorkerDataService.cs`, `Controllers/WorkerController.cs`, `HcPortal.Tests/WorkerDataServiceSearchTests.cs`
- FOUND commits: `24a71b7f` (RED), `520058b8` (GREEN), `b6defa31` (checkpoint bookkeeping), `e203c9ad` (seed journal cleaned)
- Diff scope: hanya 3 file (WorkerDataService.cs + WorkerController.cs + WorkerDataServiceSearchTests.cs) — analytics/view/model/interface utuh.

---
*Phase: 400-membership-listing-set-aware-rollup-dedup*
*Completed: 2026-06-18*
