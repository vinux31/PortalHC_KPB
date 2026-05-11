---
phase: 313-block-manual-submit-saat-waktu-habis
plan: 01
subsystem: test-infrastructure
tags: [wave-0, playwright, sql-seed, manual-uat, test-fixture, bahasa-indonesia]
dependency_graph:
  requires:
    - .planning/phases/313-block-manual-submit-saat-waktu-habis/313-CONTEXT.md
    - .planning/phases/313-block-manual-submit-saat-waktu-habis/313-RESEARCH.md
    - .planning/phases/313-block-manual-submit-saat-waktu-habis/313-PATTERNS.md
    - Models/AssessmentSession.cs (schema verify)
    - Models/AssessmentConstants.cs (AssessmentType constants)
    - tests/helpers/{accounts,auth}.ts (login fixture)
  provides:
    - .planning/seeds/313-timer-fixtures.sql (7 fixture SQL seed, idempotent)
    - tests/e2e/exam-taking.spec.ts FLOW 313 (7 test RED/SKIP state)
    - .planning/phases/313-.../313-UAT.md (7-step manual UAT)
  affects:
    - Plan 02 (backend EnsureCanSubmitExamAsync) — UAT.md menjadi acceptance verification
    - Plan 03 (frontend ExamSummary + StartExam modal) — UAT.md verify visual + retry handler
tech_stack:
  added: []  # Tidak ada dependency baru
  patterns:
    - SQL seed idempotent (DELETE-by-prefix + INSERT)
    - THROW guard untuk validation (anti-pattern Phase 309 mitigation)
    - Playwright test.skip graceful (Wave 0 RED state)
    - escapeRegex helper untuk regex exact-match selector (Pitfall 5 mitigation)
    - Manual UAT struktur Bahasa Indonesia (mirror Phase 312 precedent)
key_files:
  created:
    - .planning/seeds/313-timer-fixtures.sql
    - .planning/phases/313-block-manual-submit-saat-waktu-habis/313-UAT.md
  modified:
    - tests/e2e/exam-taking.spec.ts (append FLOW 313 — 102 lines added)
decisions:
  - "Wave 0 RED state: 7 Playwright test SKIP graceful sampai SQL seed dijalankan di DB lokal"
  - "Schema correction: AssessmentSession.Schedule adalah DateTime (bukan FK Schedules) — PLAN.md asumsi ScheduleId diperbaiki, pakai @Now"
  - "AccessToken NOT NULL (verified ApplicationDbContextModelSnapshot.cs:294-296) — set empty string untuk fixture"
  - "BannerColor NOT NULL — set 'bg-primary' default per existing pattern"
  - "Status fixture = 'InProgress' (Phase 310 WR-04 constant)"
metrics:
  duration_minutes: ~15
  completed_date: 2026-05-08
  task_count: 3
  file_count: 3
  lines_added_total: 519  # SQL 171 + spec 102 + UAT 246
---

# Phase 313 Plan 01: Wave 0 Test Infrastructure Setup Summary

Wave 0 menyiapkan test infrastructure (Playwright FLOW 313 RED state + SQL seed 7 fixture + 313-UAT.md manual checklist) **sebelum** kode produksi backend/frontend Plan 02/03 dimulai. Per Phase 312 Path B precedent, .NET unit test project tidak ada di repo — coverage closure phase 313 = Playwright FLOW 313 supplemental + manual UAT primary. Wave 0 menjamin RED state: 7 test SKIP graceful sampai fixture seed dijalankan oleh tester sebelum FLOW 313 transisi GREEN.

## Tasks Completed

### Task 1: SQL Seed `.planning/seeds/313-timer-fixtures.sql`

**Commit:** `a2dfe521`

Membuat SQL script idempotent dengan 7 fixture AssessmentSessions menggunakan title pattern `Phase 313 Timer Fixture {Type} {Scenario}` (D-08) dan StartedAt back-dated (D-07) untuk trigger kondisi target tanpa real-time wait.

**Matrix fixture:**

| # | Title | AssessmentType | StartedAt offset | Expected (Plan 02/03) |
|---|-------|----------------|------------------|----------------------|
| 1 | `Phase 313 Timer Fixture Online ManualBeforeTime` | Online | NOW − 5 min | Submit OK (regression) |
| 2 | `Phase 313 Timer Fixture Online ManualAfterGrace` | Online | NOW − 61 min | Tier-1 BLOCK + AuditLog |
| 3 | `Phase 313 Timer Fixture Online AutoInGrace` | Online | NOW − 61 min | Submit OK (Tier-2 grace covers) |
| 4 | `Phase 313 Timer Fixture Online AutoAfterGrace` | Online | NOW − 67 min | Tier-2 BLOCK (existing preserved) |
| 5 | `Phase 313 Timer Fixture PreTest ManualAfterGrace` | PreTest | NOW − 61 min | Tier-1 BLOCK (Type=PreTest) |
| 6 | `Phase 313 Timer Fixture PostTest ManualAfterGrace` | PostTest | NOW − 61 min | Tier-1 BLOCK (Type=PostTest) |
| 7 | `Phase 313 Timer Fixture Manual ExcludeVerify` | Manual | NOW − 161 min | Submit OK (D-15 exclude) |

**Fitur kunci:**
- **Idempotent:** `DELETE FROM AssessmentSessions WHERE Title LIKE 'Phase 313 Timer Fixture%'` dijalankan sebelum INSERT — re-run aman tanpa duplicate.
- **Validation guard:** `THROW 50001` jika user `rino.prasetyo@pertamina.com` tidak ditemukan (anti-pattern Phase 309 UserId=NULL FK violation mitigation).
- **Schema-verified:** Field NOT NULL (Title, Category, UserId, AccessToken, BannerColor, Status, Schedule, DurationMinutes, IsTokenRequired, dll.) di-set explicit per `ApplicationDbContextModelSnapshot.cs:286-459`.
- **Final SELECT** menampilkan `ElapsedMinutes` computed untuk verifikasi visual setelah seed.

### Task 2: FLOW 313 Playwright Tests `tests/e2e/exam-taking.spec.ts`

**Commit:** `d0eff9dc`

Append `test.describe('Exam Taking - Phase 313 Block Manual Submit')` block dengan 7 test (313.1..313.7). Tiap test:
- Login `coachee` (rino.prasetyo@pertamina.com per `tests/helpers/accounts.ts:4`)
- Cari fixture row di `/CMP/Assessment` dengan regex exact-match (`escapeRegex` helper) untuk hindari Pitfall 5 selector substring bug
- `test.skip(true, ...)` graceful jika fixture tidak ditemukan (Wave 0 RED state)
- Wave 0 placeholder assertion `await expect(targetRow).toBeVisible()` — Plan 02/03 finalisasi assertion bodies (banner verify, redirect URL, AuditLog read)

**Verifikasi:**
- `cd tests && npx tsc --noEmit` — exit 0 (TypeScript compile sukses)
- `cd tests && npx playwright test --grep "Phase 313" --list` — 7 tests listed:
  ```
  313.1 - Manual + before-time + Online → submit OK (regression)
  313.2 - Manual + after-time (in grace) + Online → BLOCKED + AuditLog SubmitExamBlocked
  313.3 - Auto + after-time (in grace) + Online → submit OK (Tier 2 grace covers)
  313.4 - Auto + after-grace + Online → BLOCKED Tier 2 (existing preserved)
  313.5 - Manual + after-time + PreTest → BLOCKED (Tier-1)
  313.6 - Manual + after-time + PostTest → BLOCKED (Tier-1)
  313.7 - Manual + after-time + Manual type → submit OK (D-15 exclude verify)
  ```

### Task 3: Manual UAT `.planning/phases/.../313-UAT.md`

**Commit:** `1fa25161`

Membuat manual UAT script Bahasa Indonesia 7-step mirror struktur `312-UAT.md` Phase 312 precedent. Setiap step lengkap dengan Pre-condition + Action + Expect + DB SQL spot-check + Sign-off checkbox.

**Highlight per step:**
- **Step 1** (Manual + before-time + Online): regression baseline, NEGATIVE assertion AuditLog
- **Step 2** (Tier-1 BLOCK CRITICAL): Banner D-01 verbatim + AuditLog `SubmitExamBlocked` SQL spot-check (`Description LIKE %Type=Online% AND %ElapsedMin=% AND %AllowedMin=60% AND %SessionId=%`); termasuk DevTools `removeAttribute('disabled')` simulation untuk simulasi user race click manual
- **Step 3** (Auto + in-grace + Online): Tier-2 grace covers, no Blocked entry
- **Step 4** (Tier-2 BLOCK existing preserved): D-06 — Tier-2 path TIDAK tulis Blocked entry (hanya Tier-1)
- **Step 5/6** (PreTest/PostTest Tier-1 BLOCK): verify D-14 — Type metadata uniform (sama ActionType, info di Description)
- **Step 7** (D-15 Manual exclude): NEGATIVE assertion `BlockedForManualType = 0`

**Final Sign-Off section** dengan increment expected = 3 entries (Step 2/5/6) dan post-snapshot SQL untuk verify total bertambah 3.

## Verification Commands Run

| Command | Result |
|---------|--------|
| `test -f .planning/seeds/313-timer-fixtures.sql && grep -c "Phase 313 Timer Fixture" ...` | 18 occurrences (>= 8 minimum) |
| `grep -oE "Phase 313 Timer Fixture [A-Za-z ]+" 313-timer-fixtures.sql \| sort -u` | 7 distinct titles |
| `grep "DELETE FROM AssessmentSessions" 313-timer-fixtures.sql` | Found (idempotent) |
| `grep "THROW 50001" 313-timer-fixtures.sql` | Found (validation guard) |
| `grep "UserId = NULL" 313-timer-fixtures.sql` | (empty) — anti-pattern avoided |
| `cd tests && npx tsc --noEmit` | exit 0 |
| `cd tests && npx playwright test --grep "Phase 313" --list` | 7 tests + 1 setup = 8 total |
| `grep -c "test.skip(true," exam-taking.spec.ts FLOW 313 region` | 7 graceful skip |
| `grep -cE "^## Step [1-7]" 313-UAT.md` | 7 step headers |
| `grep -c "SubmitExamBlocked" 313-UAT.md` | 17 occurrences (>= 4 minimum) |
| `grep -c "Waktu ujian Anda sudah habis. ..." 313-UAT.md` | 1 (D-01 verbatim) |
| `grep -c "Waktu ujian Anda telah habis. ..." 313-UAT.md` | 1 (Tier-2 existing) |

## Decisions Implemented

- **D-07 (back-dated StartedAt):** SQL seed pakai `DATEADD(MINUTE, -N, @Now)` untuk trigger kondisi target tanpa real-time wait
- **D-08 (dedicated fixture title pattern):** `Phase 313 Timer Fixture {Type} {Scenario}` pattern persis di SQL seed + Playwright + UAT cross-reference
- **D-15 (Manual exclude):** Step 7 UAT NEGATIVE assertion `BlockedForManualType = 0` + fixture #7 `AssessmentType=Manual` dengan StartedAt -161 min (very late, but Manual type harus bypass guard)
- **D-01 (banner copy verbatim):** UAT Step 2 verify exact string "Waktu ujian Anda sudah habis. Sistem akan otomatis mengirim jawaban Anda dalam beberapa detik. Mohon tunggu, jangan refresh halaman."
- **D-05 (AuditLog format):** UAT SQL spot-check verify `Description LIKE %Type=...% AND %ElapsedMin=% AND %AllowedMin=% AND %SessionId=%`
- **D-06 (Tier-2 no audit):** Step 4 Expect — `BlockedForFixture = 0` (hanya Tier-1 yang punya Blocked entry)
- **D-14 (Type metadata uniform):** Step 5/6 verify `Description LIKE %Type=PreTest%` / `%Type=PostTest%` — ActionType sama, info di Description
- **C-03 (modal info-only):** UAT Pre-conditions + final sign-off explicit verify "Modal `timeUpWarningModal` info-only (no OK button, spinner indicator)"
- **Anti-pattern Phase 309 mitigation:** SQL seed `THROW 50001` guard kalau UserId null + valid subquery `SELECT TOP 1 Id FROM AspNetUsers WHERE Email='rino.prasetyo@pertamina.com'`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Schema asumsi `ScheduleId` (FK Schedules) salah di PLAN.md**

- **Found during:** Task 1 (SQL seed authoring)
- **Issue:** PLAN.md `<action>` step 2 mention `DECLARE @ScheduleId INT = (SELECT TOP 1 Id FROM Schedules ORDER BY Date DESC)` dan `THROW 50002` validation untuk Schedule null. Verifikasi via `Models/AssessmentSession.cs:18` dan `Migrations/ApplicationDbContextModelSnapshot.cs:402-403` — field aktual adalah `Schedule` (DateTime, not nullable, no FK ke `Schedules` table).
- **Fix:** SQL seed pakai `Schedule = @Now` (DateTime literal) untuk semua 7 fixture. Hapus `@ScheduleId` declaration dan `THROW 50002` guard. Acceptance criteria PLAN line 211-212 mention "THROW 50001 atau THROW 50002" — saya tetap pakai `THROW 50001` saja (tetap memenuhi "THROW 50001 atau THROW 50002" with OR semantics).
- **Files modified:** `.planning/seeds/313-timer-fixtures.sql`
- **Commit:** `a2dfe521`

**2. [Rule 2 - Missing critical functionality] AccessToken + BannerColor NOT NULL handling**

- **Found during:** Task 1 (schema verification)
- **Issue:** `ApplicationDbContextModelSnapshot.cs:294-296, 309-311` — `AccessToken` dan `BannerColor` IS NOT NULL (no default value). PLAN.md hanya mention "set sesuai default reasonable" tanpa explicit instruction.
- **Fix:** Set `AccessToken = ''` (empty string, fits IsRequired tapi bukan token actual karena fixture tidak butuh token-gated access) dan `BannerColor = 'bg-primary'` (default existing pattern).
- **Files modified:** `.planning/seeds/313-timer-fixtures.sql`
- **Commit:** `a2dfe521`

## Notes for Plan 02/03 Executor

1. **Fixture seed perlu dijalankan manual sebelum FLOW 313 transisi GREEN.** Tester / executor Plan 02/03 harus:
   ```bash
   sqlcmd -S localhost -d HcPortal -E -i .planning/seeds/313-timer-fixtures.sql
   ```
   atau buka file di SSMS + F5. Sebelum seed dijalankan, Playwright `npx playwright test --grep "Phase 313"` akan return 7 SKIP (bukan FAIL) — itu RED state correct.

2. **Wave 0 placeholder assertion** di FLOW 313 (`await expect(targetRow).toBeVisible()`) hanya verify fixture row visible. Plan 03 finalisasi assertion bodies:
   - 313.2: assert redirect to `/CMP/StartExam` + `.alert-danger` contains D-01 verbatim
   - 313.3: assert redirect to `/CMP/Results` (Tier-2 grace covers)
   - 313.4: assert redirect to `/CMP/StartExam` + Tier-2 message
   - 313.7: assert redirect to `/CMP/Results` + NEGATIVE AuditLog check (manual SQL via UAT karena Playwright API non-trivial untuk DB query)

3. **313-UAT.md adalah PRIMARY closure verification** untuk Phase 313 (Path B precedent Phase 312). Playwright SUPPLEMENTAL. Phase verifier (`/gsd-verify-work`) wajib sign-off Final Sign-Off section terisi PASS untuk semua 7 step + post-snapshot SQL increment = 3 entries.

4. **AuditLog `SubmitExamBlocked` ActionType** belum tertulis di kode produksi (Plan 02 implementation responsibility). Test fixture + UAT spec sudah mengasumsikan ActionType ini per D-05.

5. **Server time source `DateTime.UtcNow`** — fixture StartedAt offset pakai `SYSUTCDATETIME()` (SQL Server UTC). Konsisten dengan `DateTime.UtcNow` di backend (Plan 02). Tester yang seed di luar UTC zone harus aware fixture langsung relative to server clock UTC, bukan local timezone.

## Self-Check: PASSED

**Files verified to exist:**
- FOUND: `.planning/seeds/313-timer-fixtures.sql` (171 lines)
- FOUND: `.planning/phases/313-block-manual-submit-saat-waktu-habis/313-UAT.md` (246 lines)
- FOUND: `tests/e2e/exam-taking.spec.ts` (1695 lines, FLOW 313 line 1596+)

**Commits verified to exist (in `git log 424b0d300..HEAD`):**
- FOUND: `a2dfe521` feat(313-01): add Phase 313 timer fixture SQL seed
- FOUND: `d0eff9dc` test(313-01): add FLOW 313 7 tests (RED/SKIP state)
- FOUND: `1fa25161` docs(313-01): add 313-UAT.md manual checklist 7-step

**Playwright list verified:** 7 tests (313.1..313.7) listed via `npx playwright test --grep "Phase 313" --list`.

**TypeScript compile verified:** `cd tests && npx tsc --noEmit` exit 0.

---

*Phase: 313-block-manual-submit-saat-waktu-habis*
*Plan: 01 (Wave 0 — Test Infrastructure)*
*Completed: 2026-05-08*
*Executor: Claude Opus 4.7 (1M context) — parallel worktree agent*
*Honors: D-07, D-08, D-15, D-01, D-05, D-06, D-14, C-03, anti-pattern Phase 309*
