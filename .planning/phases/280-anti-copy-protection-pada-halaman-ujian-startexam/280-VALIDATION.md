---
phase: 280
slug: anti-copy-protection-pada-halaman-ujian-startexam
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-01
---

# Phase 280 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (no automated test framework for Razor views) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` + manual browser verification |
| **Estimated runtime** | ~30 seconds (build) + manual testing |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + manual browser verification
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 280-01-01 | 01 | 1 | Anti-copy CSS | manual | `dotnet build` | ✅ | ⬜ pending |
| 280-01-02 | 01 | 1 | Anti-copy JS events | manual | `dotnet build` | ✅ | ⬜ pending |
| 280-01-03 | 01 | 1 | Keyboard shortcuts | manual | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Text selection blocked on questions | Anti-copy CSS | Browser interaction required | Try to select question text with mouse — should not highlight |
| Right-click blocked on question area | Anti-copy JS | Browser interaction required | Right-click on question text — context menu should not appear |
| Ctrl+C blocked during exam | Keyboard shortcuts | Browser interaction required | Select any text, press Ctrl+C — nothing copied to clipboard |
| Ctrl+U/S/P blocked during exam | Keyboard shortcuts | Browser interaction required | Press Ctrl+U, Ctrl+S, Ctrl+P — browser actions should not trigger |
| Radio buttons still clickable | No regression | Browser interaction required | Click answer options — should still work normally |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
