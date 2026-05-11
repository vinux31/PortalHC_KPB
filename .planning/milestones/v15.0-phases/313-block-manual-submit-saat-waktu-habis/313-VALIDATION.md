---
phase: 313
slug: block-manual-submit-saat-waktu-habis
status: ready
nyquist_compliant: true
wave_0_complete: false
created: 2026-05-08
updated: 2026-05-08
---

# Phase 313 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution. Updated by planner setelah PLAN.md generation (3 plans, 7 tasks total).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Backend test framework** | None (no .NET unit test project di repo — verified Phase 312 Path B precedent). Compile gate via `dotnet build`. |
| **E2E framework** | Playwright 1.58.2 (TypeScript 5.9.3) |
| **Config file** | `tests/playwright.config.ts` |
| **Quick run command** | `dotnet build --nologo --verbosity minimal` (compile gate, ~30s) |
| **FLOW 313 list command** | `cd tests && npx playwright test --grep "Phase 313" --list` |
| **FLOW 313 run command** | `cd tests && npx playwright test e2e/exam-taking.spec.ts --grep "Phase 313" --reporter=list` |
| **TypeScript compile gate** | `cd tests && npx tsc --noEmit` (~10s) |
| **Manual UAT** | `.planning/phases/313-block-manual-submit-saat-waktu-habis/313-UAT.md` (mandatory before `/gsd-verify-work`) |
| **Estimated runtime** | dotnet build ~30s · TypeScript compile ~10s · Playwright FLOW 313 listing ~5s · Manual UAT 7 step ~30 menit |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build --nologo --verbosity minimal` (compile gate) untuk task yang touch C# files. Untuk Razor view changes, build masih wajib (Razor compile gate).
- **After Plan 01 task completion:** Run `cd tests && npx tsc --noEmit && npx playwright test --grep "Phase 313" --list` (verify 7 test listed, RED/SKIP state).
- **After Plan 02 + Plan 03 wave completion:** Manual UAT pre-flight: jalankan `.planning/seeds/313-timer-fixtures.sql` + jalankan FLOW 313 (expect tests transisi RED → GREEN setelah seed + backend + frontend merged).
- **Before `/gsd-verify-work`:** Manual UAT 7-step sign-off `313-UAT.md` (PASS rows ≥ 7) + DB AuditLog spot-check verified + FLOW 313 PASS atau SKIP graceful (no FAIL).
- **Max feedback latency:** 30 detik untuk dotnet build, 10 detik untuk tsc compile, 5 detik untuk playwright list, 30 menit untuk manual UAT 7-step.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 313-01-T1 | 01 | 0 | TMR-01 | T-313-W0-01 | SQL seed idempotent + UserId valid (anti-FK violation Phase 309) | file presence + grep | `test -f .planning/seeds/313-timer-fixtures.sql && grep -c "Phase 313 Timer Fixture" .planning/seeds/313-timer-fixtures.sql` | ✅ Wave 0 | ⬜ pending exec |
| 313-01-T2 | 01 | 0 | TMR-01 | T-313-W0-03 | Playwright selector regex exact-match (anti-substring Pitfall 5 / Phase 312 WR-03) | TypeScript compile + Playwright list | `cd tests && npx tsc --noEmit && npx playwright test --grep "Phase 313" --list` | ✅ Wave 0 | ⬜ pending exec |
| 313-01-T3 | 01 | 0 | TMR-01 | T-313-W0-02 | UAT.md SQL block parameterized via `{fixtureSessionId}` placeholder (no PII hardcode) | file presence + grep | `test -f .planning/phases/313-block-manual-submit-saat-waktu-habis/313-UAT.md && grep -c "SubmitExamBlocked" .planning/phases/313-block-manual-submit-saat-waktu-habis/313-UAT.md` | ✅ Wave 0 | ⬜ pending exec |
| 313-02-T1 | 02 | 1 | TMR-01 | T-313-01, T-313-02, T-313-03, T-313-04, T-313-06 | 2-tier guard helper + AuditLog SubmitExamBlocked + try/catch swallow + AssessmentConstants no magic-string | dotnet build + grep | `dotnet build --nologo --verbosity minimal && grep -c "EnsureCanSubmitExamAsync" Controllers/CMPController.cs && grep -c "WriteSubmitBlockedAuditAsync" Controllers/CMPController.cs` | ✅ Wave 1 | ⬜ pending exec |
| 313-02-T2 | 02 | 1 | TMR-01 | T-313-01, T-313-02 | LIFE-03 single-tier inline block replaced dengan helper invocation (atomic refactor) | dotnet build + grep | `dotnet build --nologo --verbosity minimal && grep -c "EnsureCanSubmitExamAsync" Controllers/CMPController.cs && grep -c "Phase 313 2-tier TMR-01" Controllers/CMPController.cs` (expect: 2 helper-occur + 1 comment-marker) | ✅ Wave 1 | ⬜ pending exec |
| 313-03-T1 | 03 | 1 | TMR-01 | T-313-09, T-313-10, T-313-12, T-313-13 | 3-branch button (timerExpired/unanswered/else) + retry handler `form.action` DOM property (no hardcode URL WR-02) + D-11 banner | dotnet build + grep | `dotnet build --nologo --verbosity minimal 2>&1 \| tail -20 && grep -c "manualSubmitDisabledBtn" Views/CMP/ExamSummary.cshtml; grep -c "Submit gagal karena masalah jaringan" Views/CMP/ExamSummary.cshtml` | ✅ Wave 1 | ⬜ pending exec |
| 313-03-T2 | 03 | 1 | TMR-01 | T-313-09, T-313-15 | Modal info-only (no OK button) + JS fire submit paralel langsung (C-03) + submitted flag pre-modal (race mitigation T-313-15) | dotnet build + grep | `dotnet build --nologo --verbosity minimal 2>&1 \| tail -20 && grep -c "timeUpOkBtn" Views/CMP/StartExam.cshtml; grep -c "Phase 313 C-03" Views/CMP/StartExam.cshtml` (expect: timeUpOkBtn=0, C-03≥1) | ✅ Wave 1 | ⬜ pending exec |

---

## Wave 0 Requirements

- [x] **Plan 01 Task 1 covers:** `.planning/seeds/313-timer-fixtures.sql` — SQL script untuk seed 7 fixture (manual/auto × before/at/in-grace/after-grace + 1 Manual exclude D-15), title pattern `Phase 313 Timer Fixture {Type} {Scenario}` (per D-08)
- [x] **Plan 01 Task 2 covers:** `tests/e2e/exam-taking.spec.ts` — append FLOW 313 describe block (7 tests dengan `test.skip` graceful)
- [x] **Plan 01 Task 3 covers:** `.planning/phases/313-block-manual-submit-saat-waktu-habis/313-UAT.md` — manual UAT 7-step Bahasa Indonesia + DB AuditLog SQL spot-check
- [x] **NOT REQUIRED:** `.NET test project (KPB-PortalHC.Tests)` — verified absent di repo (Phase 312 Path B precedent). Coverage via Playwright + Manual UAT.
- [x] **NOT REQUIRED:** `tests/helpers/db-fixtures.ts` — D-07 back-date strategy via SQL seed direct (Plan 01 Task 1), tidak butuh Playwright runtime helper.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual UX modal "Waktu Habis" tampil + spinner indicator (per C-03) | TMR-01 SC#4 | Modal animation + visual feedback tidak fully assertable via DOM check | (1) Login worker `rino.prasetyo@pertamina.com`, (2) Set DB `StartedAt = SYSUTCDATETIME() - 60min - 30s` (atau pakai fixture "AutoInGrace"), (3) Tunggu countdown 00:00, (4) Verify modal muncul info-only (NO OK button, spinner indicator visible), lalu redirect ExamSummary < 5 detik. Reference: `313-UAT.md` Step 3. |
| AuditLog row content review via DB query | TMR-01 SC#5 | AuditLog Description string content check via SSMS/DBeaver — no automated framework AuditLog assertion helper di repo | Run SQL: `SELECT TOP 10 ActionType, Description, CreatedAt FROM AuditLogs WHERE ActionType='SubmitExamBlocked' ORDER BY CreatedAt DESC` — verify `Description` contain key=value untuk `Type=`, `ElapsedMin=`, `AllowedMin=`, `SessionId=`. Reference: `313-UAT.md` Step 2 SQL block. |
| Network failure retry banner + console log (D-10 + D-11) | TMR-01 ext (D-10/D-11) | Memerlukan network throttling DevTools (offline mode + simulate 5xx) — tidak portable di Playwright headless run | DevTools: Network tab → Offline mode → trigger ExamSummary timerExpired path → verify retry 3x dengan backoff (1s/2s/4s) di console (`[Phase 313] Submit attempt N failed`) + banner `"Submit gagal karena masalah jaringan..."` muncul setelah retry exhausted. Manual UAT only — di luar scope `313-UAT.md` (extension test, not blocking sign-off). |
| Manual type exclude (D-15) negative assertion | TMR-01 SC#6 | NEGATIVE assertion (no AuditLog entry) lebih reliable via SQL count delta | Reference: `313-UAT.md` Step 7 — submit fixture `Phase 313 Timer Fixture Manual ExcludeVerify`, verify SQL `SELECT COUNT(*) ... WHERE TargetId={fixtureId}` returns 0. |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify command (per acceptance_criteria di setiap task) atau Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify (every task has `dotnet build` atau file presence/grep gate)
- [x] Wave 0 covers all MISSING references (test file FLOW 313, seed SQL, manual UAT script) — Plan 01 satisfies
- [x] No watch-mode flags (`dotnet watch`, `playwright test --ui`) in automated commands
- [x] Feedback latency < 30s untuk dotnet build, < 10s untuk tsc compile, < 5s untuk playwright list, < 30 menit manual UAT
- [x] `nyquist_compliant: true` — frontmatter updated by planner setelah finalize Per-Task Map (this update)

**Approval:** ready for plan-checker review

---

## Threat Coverage Summary

Cross-reference dari individual PLAN.md `<threat_model>` blocks:

| Threat ID | Description | Plan(s) | Mitigation |
|-----------|-------------|---------|------------|
| T-313-01 | Manual submit bypass setelah timeup (TMR-01 root) | 02 | Tier-1 strict 0-grace reject di `EnsureCanSubmitExamAsync` |
| T-313-02 | DevTools force `isAutoSubmit=true` skip Tier-1 | 02 | Tier-2 hard cap `Duration+ExtraTime+2min` regardless of flag |
| T-313-03 | Audit gap untuk blocked attempts | 02 | AuditLog `SubmitExamBlocked` entry dengan Description key=value (D-05) |
| T-313-04 | AuditLog DB exception block primary action | 02 | Try/catch swallow di `WriteSubmitBlockedAuditAsync` + `_logger.LogWarning` fallback |
| T-313-05 | TOCTOU race multi-tab parallel submit | 02 | Accept (informational noise) — guard read-only check, no state mutation |
| T-313-06 | Manual type incorrectly blocked | 02 | D-15 explicit `AssessmentConstants.AssessmentType.{Online,PreTest,PostTest}` field check |
| T-313-07 | TempData["Error"] info disclosure | 02 | D-01 generic message, no SessionId/UserId/internal state |
| T-313-08 | CSRF replay POST `/CMP/SubmitExam` | 02 | `[ValidateAntiForgeryToken]` (existing line 1555 preserved) |
| T-313-09 | DevTools `removeAttribute('disabled')` bypass UI deterrent | 03 | Defense-in-depth dengan backend Tier-1 reject (Plan 02 T-313-01) |
| T-313-10 | Network 5xx storm infinite retry | 03 | Hard cap `maxAttempts=3` + exponential backoff `[1s, 2s, 4s]` |
| T-313-11 | console.error log info disclosure | 03 | Accept — log generic, no PII |
| T-313-12 | Hardcoded URL path-prefix bug (Phase 312 WR-02) | 03 | `form.action` DOM property + `form[action$="/SubmitExam"]` suffix selector |
| T-313-13 | User refresh saat retry in-progress | 03 | Accept — D-11 banner explicit instruct preserve tab; server-side last-resort save deferred v16.0+ |
| T-313-14 | XSS via TempData["Error"] | 03 | Razor auto-escape (`@TempData["Error"]` di `_Layout.cshtml:202` verified) + D-01 static literal |
| T-313-15 | Modal show + form submit fire paralel race | 03 | `submitted = true` set SEBELUM `timeupModal.show()` (mitigates visibilitychange listener race) |
| T-313-W0-01 | Test fixture DB direct INSERT | 01 | Test env only; idempotent cleanup pattern; CLAUDE.md DEV_WORKFLOW lock prevents Dev/Prod misuse |
| T-313-W0-02 | UAT.md DB SQL examples in repo | 01 | Generic SQL via `{fixtureSessionId}` placeholder, no PII hardcoded |
| T-313-W0-03 | Playwright selector substring match | 01 | `escapeRegex` + `^...$` exact regex pattern (Pitfall 5 / Phase 312 WR-03 mitigation) |

---

*Phase: 313-block-manual-submit-saat-waktu-habis*
*Validation finalized: 2026-05-08 (planner update post PLAN.md generation)*
*Stack: ASP.NET Core MVC 8 + Razor + Bootstrap 5 + Playwright 1.58.2 (TS) + SSMS/sqlcmd*
*Bahasa: Bahasa Indonesia (per CLAUDE.md)*
*Honors: C-01 (`AssessmentType` field) + C-02 (ExamSummary.cshtml primary) + C-03 (popup tetap muncul + submit paralel)*
