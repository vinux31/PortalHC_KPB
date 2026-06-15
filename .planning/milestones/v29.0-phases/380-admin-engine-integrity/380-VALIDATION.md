---
phase: 380
slug: admin-engine-integrity
status: planned
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-14
---

# Phase 380 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests, net8.0) + Playwright (tests/e2e) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` · `playwright.config.ts` |
| **Quick run command** | `dotnet test HcPortal.Tests --nologo` |
| **Full suite command** | `dotnet test --nologo` then `npx playwright test --workers=1` |
| **Estimated runtime** | ~90s xUnit + ~3-5min e2e (workers=1, shared DB) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test HcPortal.Tests --nologo`
- **After every plan wave:** Run full xUnit suite
- **Before `/gsd-verify-work`:** Full xUnit green + targeted e2e (#5 token, #6 empty-package) green
- **Max feedback latency:** ~90 seconds (xUnit)

---

## Per-Task Verification Map

| Task | Req | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|------|-----|------------|-----------------|-----------|-------------------|-------------|--------|
| SHF-01 engine filter | WSE-01 | — | empty packages excluded before K; ≥2 pkg one empty → non-empty result | unit | `dotnet test HcPortal.Tests --filter ShuffleEngine` | ❌ W0 (add cases to ShuffleEngineTests.cs) | ⬜ pending |
| SHF-01 all-empty guard | WSE-01 | — | StartExam blocks (no StartedAt/Status/assignment write) when all pkg empty | unit/integration | `dotnet test HcPortal.Tests --filter ShuffleEngine` (engine returns empty) + manual StartExam trace | ❌ W0 | ⬜ pending |
| TOK-01 defensive compare | WSE-02 | T-380 token-gate | stored lowercase token matches uppercased input | unit | `dotnet test HcPortal.Tests --filter Token` | ❌ W0 (new test) | ⬜ pending |
| RST-01 authz | WSE-03 | T-380 authz | AddExtraTime carries `[Authorize(Roles="Admin, HC")]` (reflection); non-admin → 403 | unit (reflection) | `dotnet test HcPortal.Tests --filter ExtraTime` (template CDPControllerAuthTests.cs) | ❌ W0 | ⬜ pending |
| RST-04 cap | WSE-03 | — | total extra time > original DurationMinutes → rejected | unit | `dotnet test HcPortal.Tests --filter ExtraTime` | ❌ W0 | ⬜ pending |
| E2E #5 token | WSE-02 | — | admin edit token lowercase → worker enters Pre/Post (not "Token tidak valid") | e2e | `npx playwright test exam --workers=1 -g "token"` | ✅ (helpers exist) | ⬜ pending |
| E2E #6 empty-pkg | WSE-01 | — | ≥2 pkg one empty + shuffle ON → worker gets questions > 0 | e2e | `npx playwright test exam --workers=1 -g "empty"` | ✅ (helpers exist) | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky · W0 = Wave 0 creates the test*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/ShuffleEngineTests.cs` — add ON-path empty-package cases (≥2 pkg one empty → non-empty; all-empty → empty). Template: existing `Pkg(...)` builder.
- [ ] `HcPortal.Tests/` new token-compare test — defensive both-sides uppercase (lowercase stored matches input).
- [ ] `HcPortal.Tests/` new AddExtraTime test — reflection-authz (`"Admin, HC"` exact string, mind the space) + cap reject. Template: `CDPControllerAuthTests.cs`.
- [ ] e2e scenarios #5/#6 — reuse `createAssessmentViaWizard` (token support), `addExtraTimeViaModal`, `dbSnapshot`.

*Existing infra (xUnit + Playwright) covers the rest — no framework install needed.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| All-empty friendly message wording (BI) | WSE-01 | UI string text, subjective | Visual check StartExam all-empty → pesan ramah arah admin |

*Core behaviors have automated verification; only message wording is manual.*

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
