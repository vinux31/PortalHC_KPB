---
phase: 218
slug: recordsworkerdetail-redesign-importtraining-update
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-21
---

# Phase 218 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification (ASP.NET MVC — no automated test suite) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + manual browser check
- **Before `/gsd:verify-work`:** Full build must pass + all manual verifications green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 218-01-01 | 01 | 1 | SC-1 | manual | `dotnet build` | ✅ | ⬜ pending |
| 218-01-02 | 01 | 1 | SC-2 | manual | `dotnet build` | ✅ | ⬜ pending |
| 218-01-03 | 01 | 1 | SC-3 | manual | `dotnet build` | ✅ | ⬜ pending |
| 218-02-01 | 02 | 2 | SC-4 | manual | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Tabel RecordsWorkerDetail menampilkan 7 kolom baru | SC-1 | UI rendering | Browse to RecordsWorkerDetail, verify kolom Kategori & Sub Kategori ada, Score hilang |
| Kolom Action berisi Detail + Download Sertifikat | SC-2 | UI interaction | Verify Assessment row hanya Download, Training row punya Detail + Download |
| SubCategory cascade filter works | SC-3 | JS interaction | Pilih Kategori, verify SubCategory dropdown enabled dan populated |
| ImportTraining form & template updated | SC-4 | Form + Excel | Download template, verify 12 kolom sesuai urutan D-17 |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
