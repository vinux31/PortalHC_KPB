---
phase: 89
slug: kkj-matrix-dynamic-columns
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-02
---

# Phase 89 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (ASP.NET MVC — no automated test framework in project) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run` + manual smoke test
- **Before `/gsd:verify-work`:** Full build must succeed, manual UAT pass
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 89-01-01 | 01 | 1 | Schema | build | `dotnet build` | ✅ | ⬜ pending |
| 89-02-01 | 02 | 1 | Admin CRUD | build+manual | `dotnet build` | ✅ | ⬜ pending |
| 89-03-01 | 03 | 2 | Views | build+manual | `dotnet build` | ✅ | ⬜ pending |
| 89-04-01 | 04 | 2 | Assessment | build+manual | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No additional test framework needed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Dynamic columns render per Bagian | Schema/View | Browser UI rendering | Select different Bagian, verify columns change |
| KkjColumn CRUD in admin | Admin CRUD | Interactive UI | Add/rename/delete/reorder columns |
| PositionColumnMapping admin | Position mapping | Interactive UI | Map positions to columns, verify assessment flow |
| Assessment target lookup | Assessment flow | End-to-end flow | Run assessment, verify correct target values from new schema |
| CMP/Kkj worker view | Worker view | Role-based UI | Login as worker, verify Kkj page shows correct dynamic columns |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
