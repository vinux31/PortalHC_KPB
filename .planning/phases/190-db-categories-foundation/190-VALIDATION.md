---
phase: 190
slug: db-categories-foundation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-17
---

# Phase 190 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet test (xUnit) |
| **Config file** | none — no test project yet |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run --urls "http://localhost:5000" &; sleep 5; curl -s http://localhost:5000/Admin/ManageCategories -o /dev/null -w "%{http_code}"` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run full suite command
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 190-01-01 | 01 | 1 | FORM-02 | build | `dotnet build` | ✅ | ⬜ pending |
| 190-01-02 | 01 | 1 | FORM-02 | migration | `dotnet ef migrations list` | ✅ | ⬜ pending |
| 190-02-01 | 02 | 2 | FORM-02 | build+manual | `dotnet build` | ✅ | ⬜ pending |
| 190-02-02 | 02 | 2 | FORM-02 | manual | browser verification | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Categories CRUD UI | FORM-02 | UI interaction flow | Navigate to Admin/ManageCategories, add/edit/delete category |
| CreateAssessment dropdown | FORM-02 | JS interaction | Open CreateAssessment, verify category dropdown loads from DB, verify pass percentage changes on selection |
| Delete protection | FORM-02 | Referential integrity | Try deleting a category with existing sessions — should show error |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
