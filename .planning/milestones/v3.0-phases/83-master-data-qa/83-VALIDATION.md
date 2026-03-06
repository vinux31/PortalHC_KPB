---
phase: 83
slug: master-data-qa
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-02
---

# Phase 83 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual QA (no automated test framework) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` to verify compilation
- **After every plan wave:** Manual browser verification against success criteria
- **Before `/gsd:verify-work`:** Full manual QA pass per UAT criteria
- **Max feedback latency:** 15 seconds (build check)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 83-01-xx | 01 | 1 | DATA-01 | manual + build | `dotnet build` | N/A | ⬜ pending |
| 83-02-xx | 02 | 1 | DATA-02 | manual + build | `dotnet build` | N/A | ⬜ pending |
| 83-03-xx | 03 | 2 | DATA-03 | manual + build | `dotnet build` | N/A | ⬜ pending |
| 83-04-xx | 04 | 2 | DATA-04 | manual + build | `dotnet build` | N/A | ⬜ pending |
| 83-05-xx | 05 | 3 | DATA-05, DATA-06, DATA-07 | manual + build | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No automated test framework needed — this is a QA phase with manual browser verification as the primary validation method.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| KKJ Matrix CRUD via spreadsheet editor | DATA-01 | UI interaction (inline editing, bulk save) | Create/edit/delete rows, verify CMP/Kkj reflects changes |
| KKJ-IDP Mapping CRUD + export | DATA-02 | UI interaction + file download | Create/edit/delete entries, export to Excel, verify CMP/Mapping |
| Silabus CRUD + cross-feature links | DATA-03 | Cross-view data flow (Silabus → Plan IDP, Coaching Proton) | Create silabus entry, verify it appears as dropdown option in dependent views |
| Coaching Guidance file management | DATA-04 | File upload/download browser interaction | Upload/replace/delete files, verify download links work |
| Worker CRUD | DATA-05 | Form-based UI interaction | Create/edit/delete/view worker details via Admin hub |
| Worker import from Excel template | DATA-06 | File upload + template download | Download template, fill data, upload, verify import and error display |
| Worker export with filters | DATA-07 | File download + filter state | Apply filters, export, verify Excel matches filtered data |

---

## Validation Sign-Off

- [ ] All tasks have build verification after each commit
- [ ] Manual QA criteria documented per requirement
- [ ] Cross-feature links verified (Silabus → Plan IDP, Coaching Guidance → Plan IDP)
- [ ] No automated test gaps (manual QA covers all behaviors)
- [ ] Feedback latency < 15s (build check)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
