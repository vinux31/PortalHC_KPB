---
phase: 231
slug: audit-assessment-management-monitoring
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-22
---

# Phase 231 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (no unit test framework) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | Manual flow verification in browser |
| **Estimated runtime** | ~5 seconds (build only) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Manual browser verification
- **Before `/gsd:verify-work`:** Full manual test pass
- **Max feedback latency:** 5 seconds (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 231-01-01 | 01 | 1 | AMGT-05 | manual | `dotnet build` | N/A | ⬜ pending |
| 231-01-02 | 01 | 1 | AMGT-01 | manual | `dotnet build` | N/A | ⬜ pending |
| 231-01-03 | 01 | 1 | AMGT-02 | manual | `dotnet build` | N/A | ⬜ pending |
| 231-01-04 | 01 | 1 | AMGT-03 | manual + DB check | `dotnet build` | N/A | ⬜ pending |
| 231-01-05 | 01 | 1 | AMGT-04 | manual | `dotnet build` | N/A | ⬜ pending |
| 231-02-01 | 02 | 1 | AMON-01 | manual | `dotnet build` | N/A | ⬜ pending |
| 231-02-02 | 02 | 1 | AMON-02 | manual (2 tabs) | `dotnet build` | N/A | ⬜ pending |
| 231-02-03 | 02 | 1 | AMON-03 | manual | `dotnet build` | N/A | ⬜ pending |
| 231-02-04 | 02 | 1 | AMON-04 | manual | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements — manual browser testing only.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| ManageAssessment filter kategori/status | AMGT-05 | Browser UI interaction | Apply category + status filters, verify list updates |
| CreateAssessment form validation | AMGT-01 | Form submission behavior | Submit with missing fields, verify error messages |
| EditAssessment data preservation | AMGT-02 | Complex form state | Edit assessment with packages, verify no data loss |
| DeleteAssessment cascade cleanup | AMGT-03 | DB state verification | Delete assessment, check DB for orphan packages/questions |
| Package single + bulk assign | AMGT-04 | Multi-step UI flow | Assign packages to users individually and in bulk |
| Monitoring group stats accuracy | AMON-01 | Data aggregation | Compare displayed stats with DB counts |
| Live progress via SignalR | AMON-02 | Real-time multi-tab | Open monitoring + take exam in 2 tabs, verify updates |
| HC actions (Reset/ForceClose/BulkClose) | AMON-03 | State mutation + grading | Execute each action, verify session state + scores |
| Token copy + regenerate | AMON-04 | Clipboard + token invalidation | Copy token, regenerate, verify old token invalid |

---

## Validation Sign-Off

- [ ] All tasks have manual verify instructions
- [ ] Build succeeds after each task
- [ ] All 9 requirements verified in browser
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
