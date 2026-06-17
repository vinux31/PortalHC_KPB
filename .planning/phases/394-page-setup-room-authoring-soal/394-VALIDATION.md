---
phase: 394
slug: page-setup-room-authoring-soal
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-17
---

# Phase 394 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) + Playwright (Razor + JS runtime → mandatory per Phase 354 lesson) |
| **Config file** | existing test project (KPB.Tests) + tests/e2e Playwright config |
| **Quick run command** | `dotnet build` (0 error gate) |
| **Full suite command** | `dotnet test` (xUnit) + Playwright e2e (`--workers=1`) |
| **Estimated runtime** | ~60–120s (build+unit); Playwright ~adds per-spec |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite + Playwright green; `dotnet run` (localhost:5277) manual UAT
- **Max feedback latency:** ~120 seconds (build+unit)

---

## Per-Task Verification Map

> Populated by gsd-planner from the plan tasks. Razor markup/JS contracts (wizard nav, picker binding, cert radio toggle, authoring capture, Cek-title) are runtime-driven → verify via Playwright, NOT grep/build alone.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 394-01-01 | 01 | 1 | INJ-03 | T-394-RBAC | RBAC Admin,HC enforced server-side (not just UI hide); role lain 403 | playwright + xunit | `dotnet test` / Playwright RBAC spec | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Playwright spec scaffold for inject wizard (RBAC + setup + authoring + picker + cert toggle) — INJ-03..07
- [ ] xUnit controller test scaffold (RBAC authz on GET/POST + ViewModel→InjectRequest field map sans Answers)
- [ ] AD-off env for local Playwright (`Authentication__UseActiveDirectory=false`) — lesson Phase 355

*Existing xUnit + Playwright infra covers most; Wave 0 adds inject-specific specs.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual layout fidelity vs CreateAssessment (mirror) | INJ-04 | Subjective visual parity | `dotnet run` → /Admin/InjectAssessment → compare wizard look to /Admin/CreateAssessment |

*Most behaviors automatable via Playwright; planner to maximize automated coverage.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags (Playwright `--workers=1`)
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
