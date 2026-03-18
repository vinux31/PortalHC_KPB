---
phase: 189
slug: certificate-actions-and-excel-export
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-18
---

# Phase 189 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification (ASP.NET MVC + vanilla JS) |
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
| 189-01-01 | 01 | 1 | ACT-01 | build+manual | `dotnet build` | ✅ | ⬜ pending |
| 189-01-02 | 01 | 1 | ACT-02, ACT-03 | build+manual | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Training cert link opens SertifikatUrl in new tab | ACT-01 | Browser navigation | Click training cert link → verify new tab opens with file |
| Assessment cert link opens CertificatePdf in new tab | ACT-01 | Browser navigation | Click assessment cert link → verify PDF opens |
| Export button visible only for Admin/HC | ACT-02 | Role-gated UI | Login as regular user → verify no export button; login as Admin → verify button visible |
| Export produces correct Excel with filtered data | ACT-03 | File content verification | Apply filters → click export → open Excel → verify filtered data matches |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
