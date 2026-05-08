---
phase: 313
slug: block-manual-submit-saat-waktu-habis
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-08
---

# Phase 313 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet test (xUnit/MSTest) + Playwright 1.58.2 (TypeScript) |
| **Config file** | `KPB-PortalHC.Tests/KPB-PortalHC.Tests.csproj` (existing) + `tests/playwright.config.ts` |
| **Quick run command** | `dotnet build` (compile gate) |
| **Full suite command** | `dotnet test && npx playwright test --grep "FLOW 313"` |
| **Estimated runtime** | dotnet build ~30s · Playwright FLOW 313 ~3-5min (6 scenarios × DB seed) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (compile gate)
- **After every plan wave:** Run targeted Playwright `--grep "FLOW 313"` for affected scenarios
- **Before `/gsd-verify-work`:** Full FLOW 313 suite + manual UAT sign-off
- **Max feedback latency:** 30 seconds for compile, ~5 min for full FLOW 313

---

## Per-Task Verification Map

> Filled by planner during PLAN.md generation. Each task in PLAN.md MUST map to one row here with concrete `Automated Command`. Planner writes back to this file before plan-checker review.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| _planner_fills_ | _01_ | _1_ | TMR-01 | _—_ | _2-tier guard rejects manual after timeup_ | _unit/e2e_ | _e.g. `dotnet test --filter "EnsureCanSubmitExamAsync"`_ | _❌ W0_ | _⬜ pending_ |

---

## Wave 0 Requirements

- [ ] `KPB-PortalHC.Tests/Controllers/CMPControllerTests.cs` — unit test for `EnsureCanSubmitExamAsync` helper (Pre/Post/Online types × manual/auto × elapsed scenarios)
- [ ] `tests/e2e/exam-timer.spec.ts` (atau `tests/e2e/flow-313.spec.ts`) — Playwright FLOW 313 6-skenario suite
- [ ] `.planning/seeds/313-timer-fixtures.sql` — SQL script untuk seed 6 fixture (manual/auto × before/at/in-grace/after-grace), title pattern `Phase 313 Timer Fixture {Type} {Scenario}` (per D-08)
- [ ] `tests/helpers/db-fixtures.ts` (atau equivalent) — helper Playwright untuk back-date `AssessmentSessions.StartedAt` (per D-07)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual UX modal "Waktu Habis" tampil + spinner indicator (per C-03) | TMR-01 SC#4 | Modal animation + visual feedback tidak fully assertable via DOM check | (1) Login worker, (2) Set DB StartedAt = NOW - (Duration - 0.5min), (3) Tunggu countdown 00:00, (4) Verify modal muncul, button OK hilang/disabled spinner, lalu redirect ExamSummary < 5 detik |
| AuditLog row review query | TMR-01 SC#5 | DB row content check via SSMS/DBeaver karena automated framework belum ada AuditLog assertion helper | Run SQL: `SELECT TOP 10 ActionType, Description, CreatedAt FROM AuditLogs WHERE ActionType='SubmitExamBlocked' ORDER BY CreatedAt DESC` — verify `Description` contain key=value untuk UserId/SessionId/ElapsedMin/AllowedMin/Type |
| Network failure retry banner (D-11) | TMR-01 ext | Memerlukan network throttling DevTools (offline mode + simulate 5xx) | DevTools: Network tab → Offline mode → click Submit → verify retry 3x dengan backoff (1s/2s/4s) di console + banner "Submit gagal..." muncul setelah retry exhausted |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (test files, seed SQL, fixtures helper)
- [ ] No watch-mode flags (`dotnet watch`, `playwright test --ui`) in automated commands
- [ ] Feedback latency < 30s for compile, < 5min for full FLOW 313
- [ ] `nyquist_compliant: true` set in frontmatter setelah planner finalize Per-Task map

**Approval:** pending
