---
phase: 360
slug: bypass-backend-b
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-10
---

# Phase 360 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~90 detik (unit) / ~120+ detik (dengan integration real-SQL) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "Category!=Integration"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| (diisi planner dari Validation Architecture di 360-RESEARCH.md) | | | PBYP-01..07 | | | | | | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] (diisi planner — lihat "## Validation Architecture" 360-RESEARCH.md untuk gap fixture/stub)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| UAT lokal:5277 end-to-end bypass | PBYP-02/03/06 | Render banner + notif + state machine butuh server jalan | Playwright MCP @ localhost:5277, AD off |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
