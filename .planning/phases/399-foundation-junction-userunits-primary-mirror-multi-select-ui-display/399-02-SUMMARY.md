---
phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display
plan: 02
subsystem: api
tags: [ef-core-8, asp-net-core-mvc, write-through-mirror, junction-userunits, multi-unit, closedxml, audit-set-diff, transaction, xunit, inmemory]

# Dependency graph
requires:
  - phase: 399-01
    provides: "Entity UserUnit + DbSet UserUnits + filtered-unique IX_UserUnits_UserId_PrimaryUnique (applied DB lokal) + 6 test scaffold logic (RED skip)"
provides:
  - "WorkerController.SyncUserUnitsAsync (static, testable) — write-through terpusat: replace-set baris UserUnits + mirror ApplicationUser.Unit + set-diff (D-12). Caller TIDAK commit di helper."
  - "WorkerController.ParseUnitCell (MU-04 pipe parse), ValidateUnitsInSection (MU-05), EvaluateRemoveUnitGuardAsync (MU-07 asimetris), WorkerUnitsView record (display proyeksi)"
  - "CreateWorker/EditWorker/Import POST ter-wire ke SyncUserUnitsAsync (no scalar user.Unit langsung); EditWorker tx-atomic (UpdateAsync + UserUnits + deactivate dalam 1 BeginTransactionAsync)"
  - "Validasi Unit∈Bagian + primary∈set tiap junction-write (Create/Edit/Import); unit asing ditolak server-side"
  - "MU-07 guard: PTA aktif → hard-block (D-11); CoachCoacheeMapping aktif → confirm→auto-deactivate 1 tx (D-10); audit set-diff event"
  - "Export Excel kolom 7 = semua unit primary-first comma-join; ManageWorkers ViewBag.UserUnitsDict (untuk display Plan 04); import template help-text + contoh pipe (D-06)"
  - "ManageUserViewModel + Units/PrimaryUnit/ConfirmedDeactivate/ImpactedMappings (Section tetap scalar)"
  - "6 test logic Wave 0 GREEN (WriteThrough 4, PrimaryMirror 2, AuditDiff 2, UnitInSectionValidation 3, RemoveUnitGuard 5, ImportMultiUnitParse 3 = 19 fakta)"
affects: [phase-400, phase-401, phase-402, phase-403, phase-404, multi-select-ui, display-surfaces, account-controller, proton-unit-resolution]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Write-through terpusat sebagai public static helper (ApplicationDbContext arg) — pola AdminBaseController.FindTitleDuplicatesAsync (testable seam, no InternalsVisibleTo)"
    - "Replace-set junction (RemoveRange lama + Add baru dalam 1 SaveChanges) → EF emit DELETE sebelum INSERT, no window 2-primary vs filtered-unique"
    - "MU-07 guard sebagai pure-result record (RemoveUnitGuardResult) — caller (controller) yang mutasi DB di tx; logic terpisah & testable InMemory"
    - "Atomicity lintas UserManager.UpdateAsync + _context via 1 BeginTransactionAsync (Open Q3) — mirror + junction commit bersama"

key-files:
  created:
    - .planning/phases/399-foundation-junction-userunits-primary-mirror-multi-select-ui-display/399-02-SUMMARY.md
  modified:
    - Models/ManageUserViewModel.cs
    - Controllers/WorkerController.cs
    - HcPortal.Tests/UserUnitsWriteThroughTests.cs
    - HcPortal.Tests/PrimaryMirrorTests.cs
    - HcPortal.Tests/UserUnitsAuditDiffTests.cs
    - HcPortal.Tests/UnitInSectionValidationTests.cs
    - HcPortal.Tests/RemoveUnitGuardTests.cs
    - HcPortal.Tests/ImportMultiUnitParseTests.cs
    - docs/SEED_JOURNAL.md

key-decisions:
  - "SyncUserUnitsAsync (+ ParseUnitCell, ValidateUnitsInSection, EvaluateRemoveUnitGuardAsync) dibuat PUBLIC STATIC (ApplicationDbContext arg) bukan private instance — mengikuti pola testable seam AdminBaseController.FindTitleDuplicatesAsync (FindTitleDuplicatesTests). Memungkinkan 6 test logic langsung tanpa InternalsVisibleTo / reflection / instantiate WorkerController (DI berat: UserManager/env)."
  - "Open Q3 (atomicity) DIPILIH: bungkus SyncUserUnitsAsync + UserManager.UpdateAsync + auto-deactivate + SaveChanges dalam SATU BeginTransactionAsync di EditWorker. SyncUserUnitsAsync set user.Unit SEBELUM UpdateAsync. Create/Import (user baru) tak butuh tx (CreateAsync sukses dulu, lalu Sync+SaveChanges+UpdateAsync)."
  - "Open Q1 (MU-07 hard-block resolusi unit PROTON) DIPILIH: protonUnit = activeMapping.AssignmentUnit ?? oldPrimary; block bila hasActivePta && protonUnit ∈ removed. Menutup kedua cabang (a) AssignmentUnit∈removed, (b) AssignmentUnit==null && oldPrimary∈removed."
  - "Open Q2 (kosongkan SEMUA unit saat PTA aktif) DIPILIH: ter-cover otomatis oleh guard Q1 — bila units=[] maka removed ⊇ {oldPrimary}, protonUnit (=AssignmentUnit ?? oldPrimary) pasti ∈ removed → block."
  - "MU-07 result type = enum {Allowed, Blocked, NeedConfirm, Deactivated} + mapping ref. Caller set IsActive/EndDate hanya pada Deactivated (D-10), DALAM tx EditWorker. Hard-block (D-11) return View tanpa mutasi."
  - "ManageWorkers list populate via ViewBag.UserUnitsDict (Dictionary<userId, WorkerUnitsView>) bukan modifikasi ApplicationUser (Identity entity) — Plan 04 BACA dict di view. unitFilter TETAP scalar (set-aware = Phase 400 MU-06)."

patterns-established:
  - "Pattern: write-through primary-mirror sebagai static helper testable + caller commit (separation persist vs logic)"
  - "Pattern: guard data-loss (MU-07) sebagai pure evaluator → controller transaksi (block/confirm/deactivate)"

requirements-completed: [MU-01, MU-02, MU-04, MU-05, MU-07]

# Metrics
duration: ~14min
completed: 2026-06-18
---

# Phase 399 Plan 02: Write-Through Primary-Mirror + Validasi + MU-07 Guard + Import/Export Multi-Unit Summary

**Write-through terpusat `SyncUserUnitsAsync` (junction UserUnits + mirror `ApplicationUser.Unit` + audit set-diff) di-wire ke WorkerController Create/Edit/Import; validasi Unit∈Bagian tiap write; MU-07 guard asimetris (PTA aktif hard-block / coach-mapping aktif confirm→auto-deactivate atomic); Import pipe multi-unit + Export primary-first comma-join; 6 test logic Wave 0 hijau (19 fakta); round-trip 2-unit terverifikasi di SQL Server lokal (mirror MATCH + filtered-unique enforce).**

## Performance

- **Duration:** ~14 menit
- **Started:** 2026-06-18T05:11:27Z
- **Completed:** 2026-06-18T05:24:53Z
- **Tasks:** 3 (TDD, semua auto)
- **Files modified:** 9 (1 created [SUMMARY], 8 modified)

## Accomplishments
- **Satu jalur tulis junction + mirror** (`SyncUserUnitsAsync`) dikonsumsi Create/Edit/Import — `ApplicationUser.Unit` selalu = baris `UserUnits.IsPrimary` (invariant #3, kill-desync). Replace-set DELETE-before-INSERT → tak ada window 2-primary.
- **Validasi `Unit ∈ Bagian` + `primary ∈ set` server-side** tiap junction-write (mass-assignment guard T-399-02-01) — unit asing/cross-Bagian ditolak; tidak percaya client checkbox-list.
- **MU-07 asimetris ter-implement** (T-399-02-03): PTA tahun-berjalan aktif → HARD-BLOCK (D-11, resolusi unit PROTON `AssignmentUnit ?? oldPrimary` karena PTA tak punya kolom Unit); CoachCoacheeMapping aktif tanpa PTA → confirm (NeedConfirm re-prompt) → auto-deactivate (IsActive=false + EndDate) dalam 1 tx + audit (D-10).
- **Audit set-diff (D-12)** menggantikan anti-pattern scalar `if (user.Unit != model.Unit)` (dihapus) — log "Unit +'X'"/"Unit -'Y'"/"Primary: 'A' → 'B'" + event deactivate.
- **Import pipe multi-unit** ("UnitA|UnitB", first=primary, dedup, backward-compat 1-unit) + per-unit validasi (MU-04/05); **Export** kolom 7 = semua unit primary-first comma-join (D-08); template help-text + contoh pipe (D-06).
- **Atomicity Open Q3**: EditWorker write-through + UpdateAsync + deactivate dalam 1 `BeginTransactionAsync` → mirror & junction commit bersama.
- **6 test logic Wave 0 GREEN** (19 fakta) + suite penuh **366/366** (sebelumnya 347 pass + 16 skip → 0 skip; no regresi). **DB round-trip 2-unit di SQL Server lokal**: 2 baris UserUnits, 1 IsPrimary, mirror Users.Unit == primary-row **MATCH**, filtered-unique tolak 2nd primary; DB di-RESTORE ke baseline (6/6).

## Task Commits

Each task committed atomically (TDD: helper+impl+tests digabung per task karena helper static = subjek test):

1. **Task 1: ManageUserViewModel + SyncUserUnitsAsync (write-through + set-diff)** — `862003b7` (feat) — + 3 test GREEN (WriteThrough/PrimaryMirror/AuditDiff)
2. **Task 2: Wire Create/Edit POST (validasi Unit∈Bagian + MU-07 guard + audit set-diff + tx atomic)** — `facc0df6` (feat) — + 2 test GREEN (UnitInSectionValidation/RemoveUnitGuard)
3. **Task 3: Import pipe-parse + Export primary-first + ManageWorkers populate + help-text** — `23fb5033` (feat) — + 1 test GREEN (ImportMultiUnitParse)

**DB gate artifact:** `dadca0cc` (docs: SEED_JOURNAL round-trip cleaned)
**Plan metadata:** (docs commit final — SUMMARY + STATE + ROADMAP + REQUIREMENTS)

## Files Created/Modified
- `Models/ManageUserViewModel.cs` — + `Units` (List<string>), `PrimaryUnit`, `ConfirmedDeactivate`, `ImpactedMappings` (Section TETAP scalar)
- `Controllers/WorkerController.cs` — `SyncUserUnitsAsync`/`ParseUnitCell`/`ValidateUnitsInSection`/`EvaluateRemoveUnitGuardAsync`/`WorkerUnitsView` (static helpers); CreateWorker/EditWorker/Import wiring; EditWorker MU-07 guard + tx; Export multi-unit; ManageWorkers ViewBag.UserUnitsDict; DownloadImportTemplate help-text
- `HcPortal.Tests/UserUnitsWriteThroughTests.cs` — 4 fakta (persist multi, 1 primary, null→first, mirror)
- `HcPortal.Tests/PrimaryMirrorTests.cs` — 2 fakta (promote primary, clear→null)
- `HcPortal.Tests/UserUnitsAuditDiffTests.cs` — 2 fakta (added/removed, primary-change)
- `HcPortal.Tests/UnitInSectionValidationTests.cs` — 3 fakta (unit asing ditolak, valid diterima, primary∉set ditolak)
- `HcPortal.Tests/RemoveUnitGuardTests.cs` — 5 fakta (PTA+mapping block, PTA-only via oldPrimary block, re-prompt, confirm→deactivate, no-ref allowed)
- `HcPortal.Tests/ImportMultiUnitParseTests.cs` — 3 fakta (pipe split/trim/dedup, backward-compat, empty)
- `docs/SEED_JOURNAL.md` — entri round-trip DB gate (cleaned)

## Decisions Made
- **Testable seam = public static helper** (lihat key-decisions). Konsisten dgn pola repo (`AdminBaseController.FindTitleDuplicatesAsync`). Menghindari InternalsVisibleTo / reflection / DI-berat saat test.
- **Open Q1/Q2/Q3 resolusi** sesuai rekomendasi RESEARCH (lihat frontmatter key-decisions): Q1 protonUnit=`AssignmentUnit ?? oldPrimary`; Q2 ter-cover guard Q1; Q3 EditWorker 1-tx atomic.
- **ManageWorkers populate** via ViewBag dict (bukan ubah ApplicationUser Identity entity). Plan 04 read-only.

## Open Question Resolutions (dipakai)
- **Open Q1 (MU-07 hard-block):** `protonUnit = activeMapping?.AssignmentUnit ?? oldPrimary`; block bila `hasActivePta && protonUnit != null && removed.Contains(protonUnit)`. Test `RemoveUnit_WithActiveProtonTrackAssignment_IsBlocked` (mapping AssignmentUnit) + `RemoveUnit_WithActivePta_NoMapping_ResolvesViaOldPrimary_IsBlocked` (fallback oldPrimary) keduanya hijau.
- **Open Q2 (kosongkan semua unit saat PTA aktif):** otomatis ter-block oleh guard Q1 (protonUnit ∈ removed saat units=[]).
- **Open Q3 (UpdateAsync vs _context atomicity):** EditWorker bungkus `SyncUserUnitsAsync` (set user.Unit) + deactivate + `UpdateAsync` + `SaveChangesAsync` dalam 1 `BeginTransactionAsync` → `CommitAsync`. Mirror + junction commit bersama (no desync).

## Round-Trip 2-Unit (DB lokal SQLEXPRESS, HcPortalDB_Dev)

| Cek | Hasil | Harapan | Status |
|-----|-------|---------|--------|
| UserUnits rows (worker 2-unit) | 2 | 2 | OK |
| IsPrimary count | 1 | 1 | OK |
| Users.Unit mirror vs primary-row Unit | RFCC LPG Treating Unit (062) == sama | MATCH | OK |
| Filtered-unique tolak 2nd primary | rejected | enforced | OK |
| RESTORE ke baseline | test-row=0, UserUnits=6/6 | clean | OK |

(App boot localhost:5277 HTTP 200; AD off; DB snapshot `HcPortalDB_Dev_pre399-02.bak` → RESTORE, SEED_JOURNAL cleaned.)

## Authz / CSRF (TIDAK dilonggarkan)
- `[Authorize(Roles = "Admin, HC")]` = 12 site; `[ValidateAntiForgeryToken]` = 6 site — utuh (Create/Edit/Import/Export/Delete POST). Bind hanya `model.Units`/`model.PrimaryUnit` (no over-posting). Authz Section scalar tak disentuh (de-risk).

## Deviations from Plan

None - plan executed exactly as written. (3 Open Question dari RESEARCH di-resolve sesuai rekomendasi planner/researcher — bukan deviasi, melainkan keputusan yang memang diserahkan ke executor; didokumentasikan di atas.)

## Issues Encountered
- **sqlcmd default ke master DB** saat round-trip awal ("Invalid object name 'OrganizationUnits'") + ambiguous column pada JOIN. Resolusi: tambah `-d HcPortalDB_Dev` + qualify kolom alias. Bukan defect kode — operasi verifikasi DB.

## Known Stubs
None. Semua jalur menulis/membaca data riil. `ViewBag.UserUnitsDict` (ManageWorkers) diisi data riil dari `UserUnits` — view yang mengonsumsinya adalah scope Plan 04 (sekuensing terdokumentasi, bukan stub).

## Threat Surface Scan
Tidak ada surface keamanan baru di luar `<threat_model>` plan. Semua mitigasi (T-399-02-01..06) terpasang: validasi server-side (01), write-through+tx (02), MU-07 guard server-side (03), audit set-diff (04), authz/CSRF utuh (05), pipe dedup+per-unit validasi (06).

## Next Phase Readiness
- **Write-through + mirror + ViewModel (`Units`/`PrimaryUnit`) SIAP** dikonsumsi Plan 03 (multi-select widget — konsumsi `Units`/`PrimaryUnit`/`ViewBag.SectionUnitsJson`) + Plan 04 (display surfaces — konsumsi `ViewBag.UserUnitsDict`, VM Units/PrimaryUnit, _PSign).
- **Tidak ada blocker.** Filtered-unique enforce SQL-riil resmi tetap di Phase 404 (QA-01); round-trip lokal sudah mengonfirmasi index aktif.
- **Carry:** migration=TRUE (`AddUserUnitsTable` `fc015f4d`, dari Plan 01) — notify IT saat milestone push (SATU-SATUNYA migration v32.3). Plan 02 sendiri = 0 migration.

## Self-Check: PASSED

- 9/9 file diverifikasi ada di disk (SUMMARY + WorkerController + ManageUserViewModel + 6 test).
- 4/4 commit task/gate diverifikasi di git log (862003b7, facc0df6, 23fb5033, dadca0cc).
- Suite 366/366 hijau (0 skip); build 0 error; app boot localhost:5277 HTTP 200; DB round-trip MATCH + filtered-unique enforce + RESTORE baseline.

---
*Phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display*
*Plan: 02*
*Completed: 2026-06-18*
