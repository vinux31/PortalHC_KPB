---
phase: 427
slug: exam-token-gate-server-authoritative
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-24
---

# Phase 427 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET 8) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` (fast, SQL-less regression) |
| **Full suite command** | `dotnet test HcPortal.Tests` (incl Integration real-SQL @ SQLEXPRESS) |
| **Estimated runtime** | ~30-60 s (Integration needs SQLEXPRESS + SQLBrowser) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` + quick run (Category!=Integration)
- **After migration task:** `dotnet ef database update` + `sqlcmd` column-exists check
- **After every plan wave:** full suite incl Integration
- **Before `/gsd-verify-work`:** full suite green
- **Max feedback latency:** ~60 s

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 427-01-01 | 01 | 1 | EXSEC-01 | — | Column TokenVerifiedAt added; migration applied; no backfill | migration | `dotnet ef database update` + `sqlcmd ... COL_LENGTH` | ❌ W0 | ⬜ pending |
| 427-01-02 | 01 | 1 | EXSEC-01 | — | Gate reads TokenVerifiedAt (server-authoritative), not TempData; stamp on verify; reset on retake | integration | `dotnet test HcPortal.Tests --filter "Category=Integration"` | ❌ W0 (T1-T5) | ⬜ pending |

---

## Wave 0 Requirements

- [ ] `AddTokenVerifiedAt` migration + `ApplicationDbContextModelSnapshot.cs` regen (R-2)
- [ ] T1 `StartExam_TokenRequired_TokenVerifiedAtNull_Blocks` (SC#1)
- [ ] T2 `StartExam_TokenRequired_TokenVerifiedAtSet_Proceeds` (SC#1)
- [ ] T3 `VerifyToken_CorrectToken_StampsTokenVerifiedAt` (SC#2)
- [ ] T4 `RetakeService_Execute_ResetsTokenVerifiedAtNull` (SC#3 — single source D-01)
- [ ] T5 `StartExam_LegacyInProgress_StartedAtSet_TokenVerifiedAtNull_NotLocked` (SC#4 no-lockout)

*Reuse `RetakeServiceFixture` (real-SQL, MigrateAsync full chain auto-applies new migration) + `RetakeExamEndpointTests` CMPController factory (FakeUserStore/MakeUserManager). New tests are `[Trait("Category","Integration")]`. `VerifyTokenTests.cs` (AccessTokenMatches pure helper) unchanged.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Column exists in live local DB | EXSEC-01 / SC#4 | CLI/DB check outside xUnit | `sqlcmd -C -I -S localhost\SQLEXPRESS -d <localdb> -Q "SELECT COL_LENGTH('AssessmentSessions','TokenVerifiedAt')"` → non-null |

*Token-gate behaviors otherwise have automated integration coverage. UI hint: no — no Playwright UAT required (controller/service/migration only).*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (migration + T1-T5)
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
