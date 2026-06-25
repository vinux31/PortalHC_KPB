---
phase: 420
slug: editquestion-identity-based-option-editing
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-25
---

# Phase 420 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (net8.0) — `HcPortal.Tests` |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test HcPortal.Tests` (Integration trait needs SQLEXPRESS) |
| **Estimated runtime** | quick ~20–40s · full ~2–4 min (real-SQL fixtures) |

Notes: identity-edit proof tests are `[Trait("Category","Integration")]` (real SQL Server via `SectionFixture` — FK `PackageUserResponse→PackageOption` Restrict only real on SQL Server, not InMemory). Pure-helper `EditShrinkGuardLogicTests` runs in the quick set. Playwright e2e is separate (`--workers=1`, SEED_WORKFLOW snapshot→seed→restore).

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` + quick test set
- **After every plan wave:** Run full suite (incl. Integration)
- **Before `/gsd-verify-work`:** Full suite green + Playwright UAT green
- **Max feedback latency:** ~40s (quick) / ~4 min (full)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| (planner fills) | | | OPTEDIT-01..05 / VRF-01 | T-420-* | identity match by Id; foreign Id rejected fail-closed; answered-option delete blocked | integration/unit/e2e | `dotnet test HcPortal.Tests` | — | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky — planner to expand one row per task from RESEARCH §5 table.*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/IdentityOptionEditTests.cs` — new controller-integration tests (RESEARCH §4 #1–#6) for OPTEDIT-01/02/03/04/05 + anti-tamper
- [ ] Update `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` — port TEST 1/TEST 2 to identity contract (pass `OptionInput.Id`); see RESEARCH §3
- [ ] Verify `HcPortal.Tests/EditShrinkGuardLogicTests.cs` still green (pure helper, signature unchanged — no edit)
- [ ] Playwright spec: delete-middle-answered (blocked) + add-option-then-save (new option, not silent overwrite of A — catches §2c hidden-Id clone gotcha)

*Existing harness (`SectionFixture`, `MakeController`, seed helpers) reused verbatim from `EditShrinkGuardIntegrationTests.cs`.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Real-browser authoring form: hidden Id survives JS reletter/add/remove; friendly error renders | VRF-01 | Razor+JS DOM behavior (lesson 354) — controller tests can't exercise client reindex | Playwright @5277: edit 4-opt question, delete middle (answered→blocked msg / unanswered→correct delete), add option then save (assert added, A untouched) |

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 4 min (full)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
