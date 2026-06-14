# 379 Full Green Run Evidence (D-03 DoD)

**Run date:** 2026-06-14
**Command:** `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1`
**Result:** **75 passed, 7 skipped, 0 failed** (6.4m, serial)

## Environment
- App lokal: `Authentication__UseActiveDirectory=false dotnet run` → `http://localhost:5277` (profil HcPortal, ASPNETCORE_ENVIRONMENT=Development).
- SQL: `localhost\SQLEXPRESS` + `HcPortalDB_Dev`; service `MSSQL$SQLEXPRESS` + `SQLBrowser` Running (NTLM loopback fix). `lpc:` override TIDAK diperlukan (login OK di Development).
- Playwright chromium, `--workers=1` (DB isolation), `retries:0`. globalSetup BACKUP + globalTeardown RESTORE (full DB reset).
- Prereq data: ProtonTrack T3 eligibility seed (assignment Id 9, rino→track 3, Origin='Bypass') AKTIF saat run (di-restore Task 2 setelah run ini).

## Struktural (pre-run, verified)
- `grep -c test.fixme exam-taking.spec.ts` = **0** (10 fixme A-J SEMUA dihapus).
- `grep "No Tahun 3 ProtonTrack"` = **0** (skip-Proton dihapus, D-02).
- `grep -c createAssessmentViaWizard` = **12** (≥10 — semua flow create migrasi wizard).
- `grep -c "'/Admin/CreateAssessment'"` = **0** (tak ada residu flat-form create di spec; helper di file lain).
- Helper `examTypes.ts`/`wizardSelectors.ts` = **additive** (diff 5fb6bc35..HEAD: 0 signature existing diubah; hanya field/blok/internal-guard tambahan).

## Per-flow status (semua A-K HIJAU)

| Flow | Deskripsi | Sub-test | Status |
|------|-----------|----------|--------|
| A | Legacy full lifecycle | A1-A15 | ✅ 15/15 |
| B | Token-protected exam | B1-B5 | ✅ 5/5 |
| C | Force Close & Close Early (2 worker) | C1-C7 | ✅ 7/7 |
| D | Package-based + paste-import + reshuffle | D1-D7 | ✅ 7/7 |
| E | Proton Tahun 3 Interview (FULL, no skip) | E1-E4 | ✅ 4/4 |
| F | Multiple workers same assessment | F1-F6 | ✅ 6/6 |
| G | Exam timer expired (deterministik) | G1-G3 | ✅ 3/3 |
| H | Real-time monitoring | H1-H8 | ✅ 8/8 |
| I | Edit assessment | I1-I5 | ✅ 5/5 |
| J | Abandon & reset recovery | J1-J8 | ✅ 8/8 |
| **K** | **Essay full cycle + DB Score===80 (GRADE-01/376)** | K1-K6 | ✅ **7/7** (K6 DB-assert Score=80, 122ms) |
| 313 | Phase 313 block-manual-submit (PRE-EXISTING, di luar scope 379) | 313.1-313.7 | ⏭ 7 SKIPPED (graceful — fixture `313-timer-fixtures.sql` belum di-seed; bukan flow A-J/K) |

**Total:** setup(1) + A-K(75 tests minus setup) hijau + 313(7 skip graceful) = **75 passed, 7 skipped, 0 failed**.

## Catatan
- 7 skip = Phase 313 timer-fixture tests yang skip otomatis bila fixture absent (perilaku by-design `clickResumeForFixture`, komentar spec L1437). BUKAN regresi, BUKAN bagian migrasi A-J/K. Scope 379 = create-flow A-J migrasi + Flow K — semuanya HIJAU.
- Flow K K6 = bukti hidup fix GRADE-01 Phase 376 (`AssessmentSessions.Score === 80`, bukan 0) via DB-scalar.
- Flow G timer deterministik (G2 1.0m, event-driven `waitForFunction` — bukan sleep-buta 70s; resolve saat expired).

## Temuan backlog (drift produksi ter-surface, BUKAN difix di fase test)
*(tidak ada bug produksi terungkap — seluruh drift bersifat test-side: lokalisasi Bahasa Indonesia, dropdown kebab, shuffle, positional td → semua diselesaikan test-side. Eligibility Proton T3 = data prereq, di-seed temporary, bukan bug.)*
