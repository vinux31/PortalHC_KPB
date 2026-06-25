---
phase: 428
slug: startexam-write-on-get-idempotency
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-25
validated: 2026-06-25
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
| 428-01-01 | 01 | 1 | EXSEC-02 | T-428-01/03/04 | GET StartExam TIDAK persist transisi Upcoming→Open; effective-status in-memory; gate time/GRDF/token/window utuh | refactor | `dotnet build` + grep (0× `Status = "Open"` di GET; 1× `Schedule > nowWib`) | ✅ `Controllers/CMPController.cs` | ✅ COVERED |
| 428-01-02 | 01 | 1 | EXSEC-02 | T-428-01..05 | Idempotensi GET + gate parity terbukti real-SQL | integration | `dotnet test --filter "FullyQualifiedName~StartExamIdempotencyTests"` | ✅ `HcPortal.Tests/StartExamIdempotencyTests.cs` (6 test) | ✅ COVERED |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/StartExamIdempotencyTests.cs` — 6 test real-SQL (reuse RetakeServiceFixture + CMPController factory + StubSession impersonation)
  - [x] `StartExam_Impersonate_TimeArrivedUpcoming_RendersWithoutPersisting` (SC#1/#2 — Status tetap Upcoming, ViewResult)
  - [x] `StartExam_Impersonate_DoubleGet_StatusStaysUpcoming` (SC#1 idempoten)
  - [x] `StartExam_Upcoming_NotYetTime_BlocksAndNoWrite` (SC#3 time-gate)
  - [x] `StartExam_PostTest_PreNotCompleted_Blocks` (SC#3 GRDF-01)
  - [x] `StartExam_Owner_TimeArrived_StartsInProgress` (SC#4 worker start end-to-end)
  - [x] `StartExam_TokenRequired_NotVerified_Blocks` (regresi 427 token-gate)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| (none) | EXSEC-02 | UI hint=no, tak ada view berubah; semua ter-cover integration | — |

*All phase behaviors have automated verification.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (StartExamIdempotencyTests 6 test)
- [x] No watch-mode flags
- [x] Feedback latency < 60s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** validated 2026-06-25

---

## Validation Audit 2026-06-25

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |

State A audit. Kontrak Wave-0 dipenuhi penuh saat execute (Phase 428-01). Bukti hidup:
- `dotnet test --filter "FullyQualifiedName~StartExamIdempotencyTests"` → **6/6 PASS** (real-SQL, verifier 2026-06-25).
- Full suite `dotnet test HcPortal.Tests` → **784 passed / 2 skipped / 0 failed** (no regresi; +6 dari 778 pra-428).
- Static: grep `assessment.Status = "Open"` di `Controllers/CMPController.cs` = 0 (di StartExam); `Schedule > nowWib` = 1.
- migration=FALSE (tak ada file Migrations/ baru).

Tidak ada test di-generate (zero gap). Nyquist-compliant.
