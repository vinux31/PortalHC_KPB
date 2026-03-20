---
phase: 209
slug: bulk-renew-filter-compatibility
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-20
---

# Phase 209 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (no frontend unit test framework) |
| **Config file** | none |
| **Quick run command** | `dotnet run` then browse /Admin/RenewalCertificate |
| **Full suite command** | Verify all success criteria manually |
| **Estimated runtime** | ~60 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet run` and verify affected behavior
- **After every plan wave:** Verify all success criteria
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 209-01-01 | 01 | 1 | BULK-01 | manual | Browse /Admin/RenewalCertificate, test select-all | N/A | ⬜ pending |
| 209-01-02 | 01 | 1 | BULK-02 | manual | Test renew button per group + modal | N/A | ⬜ pending |
| 209-01-03 | 01 | 1 | FILT-01 | manual | Test filters hide empty groups | N/A | ⬜ pending |
| 209-01-04 | 01 | 1 | FILT-02 | manual | Test summary cards update with filter | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Select-all per group | BULK-01 | Browser DOM interaction | Expand group, click select-all, verify all rows checked + other groups disabled |
| Renew button per group | BULK-02 | Browser DOM + modal | Check checkbox, verify button appears in group header, click it, verify modal |
| Filter hides empty groups | FILT-01 | Browser AJAX rendering | Apply filter, verify groups with 0 matching rows disappear |
| Summary cards update | FILT-02 | Browser AJAX rendering | Apply filter, verify Expired/Akan Expired counts change |

---

## Validation Sign-Off

- [ ] All tasks have manual verify instructions
- [ ] Sampling continuity: verified after each task
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
