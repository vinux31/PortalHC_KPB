---
phase: 277
slug: coba-check-dan-tambahkan-fitur-delete-peserta-assessment-pada-page-editassessment
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-01
---

# Phase 277 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build + manual browser UAT |
| **Config file** | PortalHC_KPB.csproj |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` + browser UAT |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + browser smoke test
- **Before `/gsd:verify-work`:** Full build must be green + UAT pass
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 277-01-01 | 01 | 1 | D-01 guard | build | `dotnet build` | ✅ | ⬜ pending |
| 277-01-02 | 01 | 1 | D-02 delete | build | `dotnet build` | ✅ | ⬜ pending |
| 277-01-03 | 01 | 1 | D-08 UI | build | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Delete peserta Open/Upcoming berhasil | D-01, D-02 | Requires browser + seeded data | Navigate to EditAssessment, click delete on eligible row, confirm |
| Delete peserta Completed diblok | D-01 | Requires browser interaction | Click delete on Completed row, verify blocked |
| Redirect saat current session dihapus | D-05, D-06 | Requires browser navigation | Delete session being viewed, verify redirect to sibling |
| Redirect peserta terakhir | D-07 | Requires browser navigation | Delete last participant, verify redirect to ManageAssessment |
| Badge status tampil benar | D-09 | Visual check | Verify Open=hijau, Upcoming=biru, Completed=abu |
| Confirm dialog muncul | D-03 | Browser dialog | Click delete, verify confirm() appears |

*If none: "All phase behaviors have automated verification."*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
