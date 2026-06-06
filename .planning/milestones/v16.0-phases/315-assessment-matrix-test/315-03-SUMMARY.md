---
phase: 315
plan: 03
subsystem: test-infra
tags: [wave-2, seed-sql, lifecycle, assessment, matrix-test, qa-01, playwright, sql-server]
requires:
  - .planning/phases/315-assessment-matrix-test/315-01-SUMMARY.md
  - .planning/phases/315-assessment-matrix-test/315-02-SUMMARY.md
provides:
  - tests/sql/assessment-matrix-seed.sql
  - tests/e2e/global.setup.ts (extended)
  - tests/e2e/global.teardown.ts
  - tests/playwright.config.ts (globalTeardown registered)
  - tests/helpers/dbSnapshot.ts (queryString helper added)
  - docs/SEED_JOURNAL.md (entry Phase 315 appended, status=active)
affects:
  - .planning/phases/315-assessment-matrix-test/315-04-PLAN.md (consumer: spec utama akan baca tests/.matrix-state.json + import buildScenarios via state, pakai db.restore/queryScalar di teardown)
tech_stack:
  added: []
  patterns:
    - IDENTITY_INSERT per-table block (Pitfall 4 mitigation — deterministic PK)
    - 6-step FK-respecting cleanup chain (Pattern D — copy dari 313-timer-fixtures.sql)
    - BEGIN TRAN + COMMIT envelope with SET XACT_ABORT ON (Pattern E — defense-in-depth)
    - Title prefix marker `[MATRIX_TEST_2026_05_11]` (Pattern F — D-05 fallback, Notes field absent)
    - SERVERPROPERTY('InstanceDefaultBackupPath') runtime resolve (override C:\\Temp\\ block)
    - flush-before-restore ordering (preserve findings if RESTORE crash)
    - SEED_JOURNAL regex replace active→cleaned (Pattern G)
    - Layer 4 row-count = 0 throw guard (T-315-03 mitigation)
key_files:
  created:
    - tests/sql/assessment-matrix-seed.sql (518 lines)
    - tests/e2e/global.teardown.ts (109 lines)
    - .planning/phases/315-assessment-matrix-test/315-03-SUMMARY.md (this)
  modified:
    - tests/e2e/global.setup.ts (7 lines → 285 lines, existing assertion preserved)
    - tests/playwright.config.ts (+1 line globalTeardown register)
    - tests/helpers/dbSnapshot.ts (+27 lines queryString helper)
    - docs/SEED_JOURNAL.md (+1 entry)
decisions:
  - Snapshot path resolve runtime via `SERVERPROPERTY('InstanceDefaultBackupPath')` — override CONTEXT D-discretion `C:/temp/` karena SQL Server service account TIDAK punya write access ke `C:\Temp\` di environment ini
  - Pakai forward-slash di TS literal saat compose path; SQL Server BACKUP/RESTORE accept both forward-slash dan backslash, tapi forward-slash menghindari escape complexity di TS template literal
  - queryString() helper di dbSnapshot.ts tambah baru (Rule 2 — missing critical functionality; queryScalar hanya int return type, butuh string-scalar untuk SERVERPROPERTY)
  - Schema correction: PackageOption hanya 4 kolom (Id, PackageQuestionId, OptionText, IsCorrect) — TIDAK ada OrderNumber/CreatedAt yang disebut di plan contoh. PackageQuestion field name = `[Order]` bracketed reserved keyword + no CreatedAt. Verified via Migrations/ApplicationDbContextModelSnapshot.cs:1198-1263
  - HasManualGrading=1 di-set untuk session yang ada Essay (S1-S4 mixed, S7 Essay-only, S8-S10 sentinels) supaya konsisten dengan domain semantic AssessmentSession.HasManualGrading
  - Sentinel S9 [META-AllWrong] + S10 [META-CollectorCheck] = single-peserta (1 session each, sessionIdPeserta1 == sessionIdPeserta2 di buildScenarios). Total session tetap 18: 7 discovery × 2 + S8 × 2 + S9 × 1 + S10 × 1 = 14 + 2 + 1 + 1 = 18 ✓
metrics:
  duration_min: 28
  tasks_completed: 2
  files_changed: 6
  total_lines_new: 627
completed_at: 2026-05-11T12:00:00Z
requirements: [QA-01]
---

# Phase 315 Plan 03: Wave 2 Seed SQL + Lifecycle Integration Summary

**One-liner:** Seed SQL hierarchical (18 sessions + 18 packages + 54 questions + 144 options dengan IDENTITY_INSERT deterministic + 6-step FK cleanup + BEGIN TRAN) terhubung ke globalSetup pipeline (SERVERPROPERTY backup dir resolve → BACKUP → execScript → Layer 1 5-check) dan globalTeardown pipeline (flush FIRST → RESTORE → Layer 4 throw guard → journal regex cleaned); TS compile clean (`npx tsc --noEmit` exit 0).

## Files Created / Modified

| Path | Action | Lines | Purpose |
|------|--------|-------|---------|
| `tests/sql/assessment-matrix-seed.sql` | NEW | 518 | Hierarchical seed: 18 sessions + 18 packages + 54 questions + 144 options + idempotent cleanup |
| `tests/e2e/global.teardown.ts` | NEW | 109 | Flush + RESTORE + Layer 4 + journal regex + cleanup pipeline |
| `tests/e2e/global.setup.ts` | EDIT | 7→285 | Existing app-check assertion PRESERVED + BACKUP/seed/Layer 1/state.json/journal-append extension |
| `tests/playwright.config.ts` | EDIT | 27→28 | Single line `globalTeardown: require.resolve(...)` ditambahkan |
| `tests/helpers/dbSnapshot.ts` | EDIT | 128→156 | `queryString()` helper untuk SERVERPROPERTY string-scalar (Rule 2) |
| `docs/SEED_JOURNAL.md` | EDIT | +1 row | Entry Phase 315 status=active sesuai format 7-column existing |

## Seed SQL Row Counts (Final Verification SELECT Output Schema)

Expected layout (saat run jalan terhadap DB lokal):

| Aspek | Nilai | Catatan |
|-------|-------|---------|
| AssessmentSessions seeded | 18 | Id range 9001-9018; Title prefix `[MATRIX_TEST_2026_05_11]` |
| AssessmentPackages seeded | 18 | Id range 9001-9018; 1-per-session (A1 verdict) |
| PackageQuestions seeded | 54 | Id range 50001-50054; 3 per package |
| PackageOptions seeded | 144 | Id range 80001-80212 (gap for Essay); 4 per MC/MA question |
| UserPackageAssignments seeded | 0 | A6 verdict AUTO-CREATE-LAZY |
| PackageUserResponses seeded | 0 | Insert saat exam taken (runtime) |

### Question-Type Distribution

| Scenario | MC | MA | Essay | Total Q |
|----------|----|----|----|---------|
| S1-S4 Mixed (× 2 peserta = 8 packages) | 8 | 8 | 8 | 24 |
| S5 Online MC only (2 packages × 3 MC) | 6 | 0 | 0 | 6 |
| S6 Online MA only (2 packages × 3 MA) | 0 | 6 | 0 | 6 |
| S7 Online Essay only (2 packages × 3 Essay) | 0 | 0 | 6 | 6 |
| S8 META-AllCorrect (2 × 1 mixed) | 2 | 2 | 2 | 6 |
| S9 META-AllWrong (1 × 1 mixed) | 1 | 1 | 1 | 3 |
| S10 META-CollectorCheck (1 × 1 mixed) | 1 | 1 | 1 | 3 |
| **Total** | **18** | **18** | **18** | **54** |

Options: 18 MC × 4 + 18 MA × 4 + 18 Essay × 0 = **144** options (deterministic via formula `optId = 80001 + (qId-50001)*4 + optIndex`).

## Setup Pipeline Steps (tests/e2e/global.setup.ts)

1. **EXISTING (preserve):** `page.goto('/Account/Login')` → `expect(response.ok()).toBeTruthy()` → assert login button visible.
2. Resolve `InstanceDefaultBackupPath` via `db.queryString(SELECT CAST(SERVERPROPERTY(...) AS NVARCHAR(260)))`; normalize trailing backslash + convert `\` → `/`.
3. Compose snapshotPath: `${defaultBackupDir}/HcPortalDB_Dev-matrix-${ts}.bak` (timestamp sanitized via `Date.toISOString().replace(/[:.]/g, '-')`).
4. **Layer 1 pre-check:** `queryScalar(SELECT COUNT(*) FROM AssessmentSessions WHERE Id BETWEEN 9001 AND 9018)` → expect 0, gagal kalau collision.
5. `db.backup(snapshotPath)` → `BACKUP DATABASE HcPortalDB_Dev TO DISK='${snapshotPath}' WITH INIT, FORMAT;` (no COMPRESSION — SQL Express limit).
6. `db.execScript('tests/sql/assessment-matrix-seed.sql')` → sqlcmd -i.
7. **Layer 1 post-seed validation** (5 checks): sessions=18, packages=18, questions=54, options=144, UPA=0 (A6 cross-check).
8. `mkdir docs/test-reports/ { recursive: true }` — supaya teardown flush() tidak race mkdir.
9. `writeFile tests/.matrix-state.json` — { snapshotPath, seededAt, scenarios[10] } JSON.
10. `appendFile docs/SEED_JOURNAL.md` — entry status=active sesuai format 7-column existing.

## Teardown Pipeline Steps (tests/e2e/global.teardown.ts)

1. **FLUSH FIRST:** `collector.flush(docs/test-reports/${today}-assessment-matrix.md)` — preserve findings sebelum RESTORE crash (Pitfall: flush before restore).
2. Read `tests/.matrix-state.json` untuk dapat snapshotPath. Throw kalau missing/invalid.
3. `db.restore(state.snapshotPath)` → SINGLE_USER + RESTORE WITH REPLACE + MULTI_USER (di dbSnapshot.restore). Try-catch + log manual restore command kalau gagal.
4. **Layer 4 validation:** `queryScalar(SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[MATRIX_TEST_2026_05_11]%')` → throw kalau != 0 (T-315-03 mitigation).
5. SEED_JOURNAL regex replace `active` → `cleaned` (Pattern G). Warn kalau regex tidak match (entry sudah cleaned previously atau snapshot path format berbeda).
6. Cleanup state.json + .bak best-effort (`unlink().catch(() => {})`).

## Layer 1 + Layer 4 Expected vs Acceptance

| Layer | Query | Expected | Actual (saat runtime) |
|-------|-------|----------|------------------------|
| Layer 1 sessions | `COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[MATRIX_TEST_2026_05_11]%'` | 18 | (runtime — sesuai seed) |
| Layer 1 packages | `COUNT(*) FROM AssessmentPackages WHERE AssessmentSessionId IN (sessions...)` | 18 | (runtime) |
| Layer 1 questions | `COUNT(*) FROM PackageQuestions WHERE AssessmentPackageId BETWEEN 9001 AND 9018` | 54 | (runtime) |
| Layer 1 options | `COUNT(*) FROM PackageOptions WHERE PackageQuestionId BETWEEN 50001 AND 50054` | 144 | (runtime) |
| Layer 1 UPA | `COUNT(*) FROM UserPackageAssignments WHERE AssessmentSessionId BETWEEN 9001 AND 9018` | 0 (A6) | (runtime) |
| Layer 4 sessions | same as Layer 1 sessions, post-RESTORE | 0 | (throw kalau != 0) |

## TS Compile Result

```
cd tests && npx tsc --noEmit
EXIT=0  (clean, no error, no warning)
```

tsconfig `strict: true` aktif. Semua type narrowing safe (state.snapshotPath cast string, queryScalar/queryString return types compatible, FullConfig import dari @playwright/test).

## Manual Smoke `--list` (Tidak Dijalankan di Plan 03)

Per scope: plan 03 hanya wire-up lifecycle + seed SQL. Smoke `npx playwright test --list assessment-matrix` butuh:
1. Kestrel running (`dotnet run`) — TIDAK dijalankan di executor karena Plan 04 belum nulis spec.
2. SQL Server reachable — assumption local-only.

Plan 04 atau Plan 05 polish iteration yang akan exercise full smoke. Plan 03 commit hanya syntactic + semantic correctness (TS compile clean, acceptance criteria pass).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Schema mismatch di plan contoh PackageOption & PackageQuestion**
- **Found during:** Task 1 (seed SQL drafting)
- **Issue:** Plan contoh SQL pakai kolom `OrderNumber` + `CreatedAt` untuk PackageOption dan `OrderNumber` untuk PackageQuestion. Verified via `Migrations/ApplicationDbContextModelSnapshot.cs:1198-1263`: PackageOption hanya 4 kolom (Id, PackageQuestionId, OptionText, IsCorrect — TIDAK ada OrderNumber/CreatedAt); PackageQuestion field name = `Order` (reserved keyword, bracket required di SQL) tanpa CreatedAt.
- **Fix:** Seed SQL ditulis dengan schema actual: `INSERT INTO PackageOptions (Id, PackageQuestionId, OptionText, IsCorrect)` dan `INSERT INTO PackageQuestions (Id, AssessmentPackageId, [Order], QuestionText, QuestionType, ScoreValue, MaxCharacters, Rubrik, ElemenTeknis)`.
- **Files modified:** tests/sql/assessment-matrix-seed.sql
- **Commit:** `6a5081d4`

**2. [Rule 2 - Missing critical functionality] dbSnapshot.queryString() helper**
- **Found during:** Task 2 (setup.ts drafting, butuh resolve SERVERPROPERTY return string)
- **Issue:** Existing `db.queryScalar()` return type adalah `number` — tidak bisa untuk SERVERPROPERTY('InstanceDefaultBackupPath') yang return path string. Tanpa helper baru, snapshotPath harus hardcoded yang dilarang prompt context override.
- **Fix:** Tambah `queryString()` helper di tests/helpers/dbSnapshot.ts dengan signature `(sql: string) => Promise<string>`. Pakai SQLCMD_BASE_ARGS yang sama (hostname guard, -E, -C, -I, -b) + `-h -1 -W`. Extract first non-empty trimmed line dari stdout.
- **Files modified:** tests/helpers/dbSnapshot.ts
- **Commit:** `b07e62dd`

**3. [Rule 1 - Bug] Worktree path mismatch saat Write/Edit tools (SEED_JOURNAL.md)**
- **Found during:** Task 2 (saat append SEED_JOURNAL entry awal)
- **Issue:** Edit tool initial call mengedit `docs/SEED_JOURNAL.md` di main repo path (`PortalHC_KPB/docs/SEED_JOURNAL.md`) bukan di worktree (`PortalHC_KPB/.claude/worktrees/agent-a38ec6e80adfc6237/docs/SEED_JOURNAL.md`). Same deviation pattern dengan Plan 02 worktree quirk.
- **Fix:** Read worktree path SEED_JOURNAL.md untuk Read-before-Edit eligibility; apply Edit ke worktree path; `git checkout -- docs/SEED_JOURNAL.md` di main repo path untuk restore bersih.
- **Files modified:** docs/SEED_JOURNAL.md (worktree only — main repo restored)
- **Commit:** `b07e62dd`

**4. [Rule 2 - Missing critical functionality] HasManualGrading=1 untuk session ber-Essay**
- **Found during:** Task 1 (seed SQL drafting)
- **Issue:** Plan contoh punya inconsistency — S1 Manual Mixed di-set `HasManualGrading=0` padahal punya Essay question. AssessmentSession.HasManualGrading semantic = true kalau ada Essay (per Models/AssessmentSession.cs:177 docblock "hanya true jika ada soal Essay dalam package").
- **Fix:** Set HasManualGrading=1 untuk semua session yang punya Essay question: S1-S4 mixed, S2-S4 mixed, S7 Essay-only, S8-S10 sentinels. S5 MC-only, S6 MA-only HasManualGrading=0.
- **Files modified:** tests/sql/assessment-matrix-seed.sql
- **Commit:** `6a5081d4`

### Auth Gates Encountered

Tidak ada. Plan 03 fully autonomous (no Playwright runtime, no Kestrel start).

## TDD Gate Compliance

Plan 03 `tdd="false"` (auto execution tanpa TDD). Tidak applicable.

## Known Stubs

Tidak ada stub. Semua deliverable fully wired:
- Seed SQL: 144 options di-generate mekanis menggunakan formula (no `NOTE Plan 03 Task 1 executor` placeholder).
- setup.ts buildScenarios(): 10 ScenarioConfig entries lengkap dengan formula closures (mcQ/maQ/essayQ), no hand-typed arrays.
- teardown.ts: full pipeline 6 step, no TODO.
- dbSnapshot.queryString: concrete implementation, error-handled.

## Acceptance Criteria Verification

### Task 1 (seed SQL)
- File exists: PASS
- SET NOCOUNT ON: 1 match ✓
- SET XACT_ABORT ON: 2 matches (1 statement + 1 docblock ref) — semantic OK ✓
- rino.prasetyo@pertamina.com: 3 matches (1 SQL literal + 2 docblock) ✓
- iwan3@pertamina.com: 3 matches ✓
- THROW 5000[12]: 2 matches (user-guard + collision-guard) ✓
- `[MATRIX_TEST_2026_05_11]`: 28 matches (≥10 required) ✓
- SET IDENTITY_INSERT: 9 matches (≥8: 4 ON + 4 OFF + 1 docblock) ✓
- BEGIN TRAN: 4 matches (1 statement + 3 docblock/PRINT) ✓
- ^COMMIT;$: 1 match ✓
- DELETE 6-step cleanup: 5 matches (pur/upa/po/pq/ap) ✓
- Final SELECT block: 1 match ✓
- No placeholder `NOTE Plan 03 Task 1 executor`: 0 matches ✓
- Deterministic formula `optId = 80001 + (qId - 50001) * 4 + optIndex`: 2 matches (header + helper comment) ✓

### Task 2 (lifecycle integration)
- global.setup.ts existing /Account/Login preserved: 2 matches ✓
- db.backup(snapshotPath): 1 match ✓
- db.execScript: 1 match ✓
- Layer 1 mentions: 10 matches ✓
- .matrix-state.json reference: 4 matches ✓
- SEED_JOURNAL reference: 4 matches ✓
- function buildScenarios: 1 match ✓
- scenario id 1..10 entries: 10 matches ✓
- collector.flush in teardown: 1 match ✓
- db.restore in teardown: 2 matches ✓
- Layer 4 mentions: 6 matches ✓
- cleaned in teardown: 7 matches ✓
- export default globalTeardown: 1 match ✓
- globalTeardown in config: 1 match ✓
- fullyParallel: false PRESERVED: 1 match ✓
- screenshot: 'on' PRESERVED: 1 match ✓
- npx tsc --noEmit exit 0: PASS

## Threat Flags

Tidak ada new threat surface di luar yang sudah dicover threat_model PLAN. Semua T-315-01 sampai T-315-04 ter-mitigate sesuai design:
- T-315-01 (Tampering dbSnapshot non-localhost): mitigated via hostname guard di runSqlcmd (Plan 02 deliverable, tetap dipakai queryString baru).
- T-315-02 (Information Disclosure Title prefix): Title `[MATRIX_TEST_2026_05_11]` non-PII synthetic; seed SQL committed (deliberate review), .bak gitignored di tests/.gitignore (Plan 02).
- T-315-03 (cleanup leak): globalTeardown Layer 4 throw kalau remainingSessions != 0; Pattern G journal regex replace mengubah status active → cleaned.
- T-315-04 (SQL injection via snapshotPath): snapshotPath constructed dari `Date.toISOString().replace(/[:.]/g, '-')` (regex sanitized) + `defaultBackupDirRaw.replace(/\\+$/, '').replace(/\\/g, '/')` (normalize); no user input; RESTORE wraps path di single-quote literal.

## Self-Check: PASSED

- File `tests/sql/assessment-matrix-seed.sql`: FOUND (518 lines)
- File `tests/e2e/global.setup.ts`: FOUND (285 lines, existing assertion preserved)
- File `tests/e2e/global.teardown.ts`: FOUND (109 lines)
- File `tests/playwright.config.ts`: FOUND (29 lines, globalTeardown registered)
- File `tests/helpers/dbSnapshot.ts`: FOUND (156 lines, queryString helper added)
- File `docs/SEED_JOURNAL.md`: ENTRY APPENDED (status=active, line 11)
- Commit `6a5081d4` (feat(315-03): add assessment matrix seed SQL ...): FOUND
- Commit `b07e62dd` (feat(315-03): wire matrix test lifecycle ...): FOUND
- TypeScript compile: exit 0
