---
phase: 423
slug: certificate-issuance-consistency
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-24
---

# Phase 423 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail per-task diisi oleh planner (Per-Task Verification Map) + gsd-validate-phase. Strategi arsitektur: lihat `423-RESEARCH.md` §"Validation Architecture".

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test` (Integration butuh SQLEXPRESS live) |
| **Estimated runtime** | ~60-120 detik (unit), lebih lama dgn integration |

---

## Sampling Rate

- **After every task commit:** Run quick (non-integration) suite
- **After every plan wave:** Run full suite (incl real-SQL integration @SQLEXPRESS)
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** ~120 detik

---

## Per-Task Verification Map

*Diisi planner per task (REQ CERT-01..07). Pure-helper → unit truth-table; issue-path/anti-dup/seq → integration real-SQL; badge umur → Playwright UAT (2h).*

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | — | — | CERT-01..07 | TBD | TBD | unit/integration | `dotnet test` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/CertIssuanceRulesTests.cs` — pure truth-table utk ShouldIssueCertificate / DeriveValidUntil / ResemblesAutoCertFormat / PendingAgeBadgeClass (analog `SessionEditLockRulesTests`)
- [ ] Integration tests real-SQL `IClassFixture<RetakeServiceFixture>` + `NoOpHubContext` (recipe `RetakeThenPassCertTests`) — issue-path 4 site, anti-dup, seq-failure flag

*Index `IX_AssessmentSessions_NomorSertifikat_Unique` sudah ada (no migration).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Badge umur PendingGrading (warna >3hr/>7hr) di EssayGrading + ManageAssessment | CERT-07 | Render Razor + warna ambang | Playwright UAT @5270 (gate 2h) + assert DOM badge class |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
