---
phase: 116
slug: modal-cleanup
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-07
---

# Phase 116 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification (ASP.NET MVC — no unit test project) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build`
- **Before `/gsd:verify-work`:** Build must succeed
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 116-01-01 | 01 | 1 | MOD-01 | manual | Browser: open CoachingProton, check modal | N/A | ⬜ pending |
| 116-01-02 | 01 | 1 | MOD-02 | manual | Browser: submit evidence, check DB | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework setup needed — this is a pure field removal verified by build success and manual browser check.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Modal no longer shows Kompetensi Coachee textarea | MOD-01 | UI removal — visual check | Open CoachingProton as Coach, click evidence button, verify no "Kompetensi Coachee" textarea |
| Submit evidence without koacheeCompetencies | MOD-02 | End-to-end form submission | Submit evidence via modal, verify success response, check CoachingSession in DB has empty CoacheeCompetencies |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
