---
phase: 120
slug: pdf-evidence
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-08
---

# Phase 120 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing + PDF visual inspection |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd:verify-work`:** Full build must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 120-01-01 | 01 | 1 | PDF-01 | build | `dotnet build` | ✅ | ⬜ pending |
| 120-01-02 | 01 | 1 | PDF-02 | build | `dotnet build` | ✅ | ⬜ pending |
| 120-01-03 | 01 | 1 | PDF-03 | build | `dotnet build` | ✅ | ⬜ pending |
| 120-01-04 | 01 | 1 | PDF-04 | manual | Browser test | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements. QuestPDF v2026.2.2 already installed.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| PDF layout matches spec (portrait A4, form fields) | PDF-02 | Visual layout verification | Open generated PDF, verify all fields present and correctly formatted |
| P-Sign badge renders in bottom-right | PDF-03 | Visual rendering | Download PDF, check P-Sign badge position and logo |
| Download button appears on Deliverable page | PDF-04 | UI verification | Navigate to Deliverable detail, verify button renders for sessions with evidence |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
