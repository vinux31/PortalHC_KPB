---
phase: 427
slug: exam-token-gate-server-authoritative
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-24
validated: 2026-06-25
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
| 427-01-01 | 01 | 1 | EXSEC-01 | T-427-04 | Column TokenVerifiedAt added; migration applied; nullable; no backfill | migration | `dotnet ef database update` + `sqlcmd ... COL_LENGTH` | ✅ `Migrations/20260624133656_AddTokenVerifiedAt.cs` | ✅ COVERED |
| 427-01-02 | 01 | 1 | EXSEC-01 | T-427-01/02 | Gate reads TokenVerifiedAt (server-authoritative), not TempData; stamp on verify; reset on retake | integration | `dotnet test HcPortal.Tests --filter "Category=Integration"` | ✅ `HcPortal.Tests/TokenVerifiedAtTests.cs` (T1-T5) | ✅ COVERED |

---

## Wave 0 Requirements

- [x] `AddTokenVerifiedAt` migration + `ApplicationDbContextModelSnapshot.cs` regen (R-2) — kolom live `HcPortalDB_Dev` = `datetime2 NULL` (sqlcmd `COL_LENGTH` non-null, `is_nullable=1`)
- [x] T1 `StartExam_TokenRequired_TokenVerifiedAtNull_Blocks` (SC#1)
- [x] T2 `StartExam_TokenRequired_TokenVerifiedAtSet_Proceeds` (SC#1)
- [x] T3 `VerifyToken_CorrectToken_StampsTokenVerifiedAt` (SC#2)
- [x] T4 `RetakeService_Execute_ResetsTokenVerifiedAtNull` (SC#3 — single source D-01)
- [x] T5 `StartExam_LegacyInProgress_StartedAtSet_TokenVerifiedAtNull_NotLocked` (SC#4 no-lockout)

*Reuse `RetakeServiceFixture` (real-SQL, MigrateAsync full chain auto-applies new migration) + `RetakeExamEndpointTests` CMPController factory (FakeUserStore/MakeUserManager). New tests are `[Trait("Category","Integration")]`. `VerifyTokenTests.cs` (AccessTokenMatches pure helper) unchanged.*

**Coverage tambahan (review-fix WR-01):** `RetakeExamEndpointTests.RetakeExam_Success_ClearsTokenAndRedirectsToStartExam` kini meng-assert kolom DB `TokenVerifiedAt == null` pasca-retake lewat endpoint worker (bukan TempData) — coverage reset token gate di lapisan endpoint, melengkapi T4 (lapisan service).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions | Status |
|----------|-------------|------------|-------------------|--------|
| Column exists in live local DB | EXSEC-01 / SC#4 | CLI/DB check outside xUnit | `sqlcmd -C -I -S localhost\SQLEXPRESS -d HcPortalDB_Dev -Q "SELECT COL_LENGTH('AssessmentSessions','TokenVerifiedAt')"` → non-null | ✅ CONFIRMED 2026-06-25 (datetime2 NULL) |

*Token-gate behaviors otherwise have automated integration coverage. UI hint: no — no Playwright UAT required (controller/service/migration only).*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (migration + T1-T5)
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

State A audit. Kontrak Wave-0 sudah dipenuhi penuh saat execute (Phase 427-01). Bukti hidup:
- `dotnet test --filter "FullyQualifiedName~TokenVerifiedAtTests"` → **5/5 PASS** (T1-T5, real-SQL).
- `dotnet test --filter "FullyQualifiedName~RetakeExamEndpointTests"` → **3/3 PASS** (incl reset token gate via endpoint, WR-01 fix).
- Kolom `AssessmentSessions.TokenVerifiedAt` live di `HcPortalDB_Dev` = `datetime2 NULL`.

Tidak ada test di-generate (zero gap). Nyquist-compliant.
