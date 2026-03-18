---
phase: 190
slug: certificationmanagement-filter-category-sub-category-role-based-view-content-and-access-logic
status: draft
nyquist_compliant: false
wave_0_complete: true
created: 2026-03-18
---

# Phase 190 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (project pattern) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` + manual browser verification |
| **Estimated runtime** | ~10 seconds (build) + manual |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + manual browser check
- **Before `/gsd:verify-work`:** Full manual browser verification
- **Max feedback latency:** 10 seconds (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| TBD | 01 | 1 | Category filter | manual-UI | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework needed — project uses manual browser testing.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Filter Category populate dari AssessmentCategory | Category filter | UI interaction | Buka page, cek dropdown Category terisi |
| Cascade Category → Sub-Category | Sub-Category filter | UI interaction | Pilih Category, cek Sub-Category populate |
| Filter Category: Training rows hilang | Filter logic | UI interaction | Pilih Category spesifik, verifikasi hanya Assessment rows |
| L4: Bagian disabled + pre-filled | Role-based filter | Role-specific UI | Login L4, cek Bagian disabled |
| L5: own-data-only + kolom hidden + summary hidden | Role-based view | Role-specific UI | Login L5, verifikasi scope + tampilan |
| L6: identik L5 | Role-based view | Role-specific UI | Login L6, verifikasi |
| Kolom Sub Kategori di tabel | Table column | UI render | Cek header + data Sub Kategori |
| AJAX filter tetap berjalan | Regression | UI interaction | Ganti filter, cek AJAX refresh |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
