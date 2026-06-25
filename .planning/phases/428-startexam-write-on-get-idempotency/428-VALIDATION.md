---
phase: 428
slug: startexam-write-on-get-idempotency
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-25
---

# Phase 428 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET 8) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` (fast, SQL-less regression) |
| **Full suite command** | `dotnet test HcPortal.Tests` (incl Integration real-SQL @ SQLEXPRESS) |
| **Estimated runtime** | ~30-60 s (Integration needs SQLEXPRESS) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` + quick run (Category!=Integration)
- **After every plan wave:** full suite incl Integration
- **Before `/gsd-verify-work`:** full suite green
- **Max feedback latency:** ~60 s

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 428-01-01 | 01 | 1 | EXSEC-02 | T-428-01 | GET StartExam TIDAK persist transisi Upcoming→Open; effective-status in-memory; gate time/GRDF/token/window utuh | refactor | `dotnet build` + grep (no `Status = "Open"` + SaveChanges di GET) | ❌ W0 | ⬜ pending |
| 428-01-02 | 01 | 1 | EXSEC-02 | T-428-01 | Idempotensi GET + gate parity terbukti real-SQL | integration | `dotnet test --filter "FullyQualifiedName~StartExamIdempotencyTests"` | ❌ W0 (6 test) | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/StartExamIdempotencyTests.cs` — 6 test real-SQL (reuse RetakeServiceFixture + CMPController factory + StubSession impersonation)
  - [ ] `StartExam_Impersonate_TimeArrivedUpcoming_RendersWithoutPersisting` (SC#1/#2 — Status tetap Upcoming, ViewResult)
  - [ ] `StartExam_Impersonate_DoubleGet_StatusStaysUpcoming` (SC#1 idempoten)
  - [ ] `StartExam_Upcoming_NotYetTime_BlocksAndNoWrite` (SC#3 time-gate)
  - [ ] `StartExam_PostTest_PreNotCompleted_Blocks` (SC#3 GRDF-01)
  - [ ] `StartExam_Owner_TimeArrived_StartsInProgress` (SC#4 worker start end-to-end)
  - [ ] `StartExam_TokenRequired_NotVerified_Blocks` (regresi 427 token-gate)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| (none) | EXSEC-02 | UI hint=no, tak ada view berubah; semua ter-cover integration | — |

*All phase behaviors have automated verification.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (StartExamIdempotencyTests 6 test)
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
