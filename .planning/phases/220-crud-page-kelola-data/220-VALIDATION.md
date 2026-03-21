---
phase: 220
slug: crud-page-kelola-data
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-21
---

# Phase 220 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Browser manual verification (ASP.NET Core MVC — no automated test framework in project) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd:verify-work`:** Full build must succeed + manual browser verification
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 220-01-01 | 01 | 1 | CRUD-01 | build+manual | `dotnet build` | N/A | ⬜ pending |
| 220-01-02 | 01 | 1 | CRUD-02 | build+manual | `dotnet build` | N/A | ⬜ pending |
| 220-01-03 | 01 | 1 | CRUD-03 | build+manual | `dotnet build` | N/A | ⬜ pending |
| 220-01-04 | 01 | 1 | CRUD-04 | build+manual | `dotnet build` | N/A | ⬜ pending |
| 220-01-05 | 01 | 1 | CRUD-05 | build+manual | `dotnet build` | N/A | ⬜ pending |
| 220-01-06 | 01 | 1 | CRUD-06 | build+manual | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework install needed — project uses manual browser verification pattern.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Halaman Struktur Organisasi tampil di Kelola Data | CRUD-01 | UI rendering | Navigate to Admin/Index, verify card exists, click to open page |
| Tambah node baru | CRUD-02 | Form interaction | Click Tambah, fill form, submit, verify node appears |
| Edit nama node | CRUD-03 | Inline form | Click Edit, change name, submit, verify updated |
| Pindah parent | CRUD-04 | Dropdown + tree validation | Edit node, change parent, verify children follow, test circular rejection |
| Soft-delete & hard-delete | CRUD-05 | Modal + blocking logic | Toggle nonaktif, try delete with children, verify blocks |
| Reorder nodes | CRUD-06 | Arrow buttons | Click ↑↓, verify order changes in table |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
