---
phase: 47
slug: kkj-matrix-manager
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-02-26
---

# Phase 47 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser + Razor view smoke checks (ASP.NET Core MVC — no automated test project detected) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds (build only) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Manual browser smoke test of affected pages
- **Before `/gsd:verify-work`:** Full CRUD flow verified manually in browser
- **Max feedback latency:** 15 seconds (build gate)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 47-01-01 | 01 | 1 | MDAT-01 | build | `dotnet build` | ✅ | ⬜ pending |
| 47-01-02 | 01 | 1 | MDAT-01 | build | `dotnet build` | ✅ | ⬜ pending |
| 47-01-03 | 01 | 2 | MDAT-01 | manual | browse /Admin/KkjMatrix | ❌ W0 | ⬜ pending |
| 47-02-01 | 02 | 2 | MDAT-01 | manual | create row, verify list updates | ❌ W0 | ⬜ pending |
| 47-02-02 | 02 | 2 | MDAT-01 | manual | edit row, verify persistence | ❌ W0 | ⬜ pending |
| 47-02-03 | 02 | 2 | MDAT-01 | manual | delete row (with guard), verify | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- Existing `dotnet build` covers all compile-time checks
- No automated test project exists — manual verification required for browser behavior

*Existing infrastructure covers all phase requirements that can be automated (build gate only).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| KKJ Matrix list renders all rows | MDAT-01 | No test project | Navigate to /Admin/KkjMatrix, verify row count matches DB |
| Create new KkjMatrixItem | MDAT-01 | No test project | Click Add, fill fields, save, verify row appears in list |
| Edit existing item inline | MDAT-01 | No test project | Click edit on row, modify fields, save, verify changes persist |
| Delete item with guard | MDAT-01 | No test project | Delete item in use — verify warning shown; delete unused item — verify removal |
| Clipboard paste from Excel | MDAT-01 | No test project | Copy TSV from Excel, paste into edit table, verify rows populate |
| No full page reload on CRUD | MDAT-01 | No test project | Perform each CRUD op, verify page does not fully reload (no flash) |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
