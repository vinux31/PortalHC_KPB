---
phase: 90
slug: kkj-matrix-admin-full-rewrite
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-02
---

# Phase 90 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual QA (no automated test suite in project) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~30 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + manual smoke test
- **Before `/gsd:verify-work`:** Full build must succeed + manual UAT
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 90-01-01 | 01 | 1 | DB cleanup | build | `dotnet build` | N/A | pending |
| 90-02-01 | 02 | 1 | Admin file CRUD | manual | browser test | N/A | pending |
| 90-03-01 | 03 | 2 | Admin UI rewrite | manual | browser test | N/A | pending |
| 90-04-01 | 04 | 2 | CMP worker view | manual | browser test | N/A | pending |

*Status: pending · green · red · flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework to install.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Admin tab navigation per bagian | UI layout | Visual/interactive | Navigate Admin KKJ Matrix, verify tabs render per bagian |
| File upload (PDF/Excel) | File CRUD | File I/O + browser | Upload PDF and Excel, verify stored + displayed |
| File download | File access | Browser download | Click download, verify file opens correctly |
| File delete + archive | File lifecycle | State verification | Delete file, verify archived, check history view |
| CMP worker file view (L1-L4 vs L5-L6) | Role filtering | Role-based access | Login as L5 worker, verify only own bagian files visible |
| Assessment flow still works post-migration | DB cleanup | Integration | Run assessment flow, verify no GetTargetLevel errors |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
