---
phase: 258
slug: silabus-guidance
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-25
---

# Phase 258 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual UAT (Claude code review + browser verification) |
| **Config file** | none — UAT phase, no automated test framework |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~30 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd:verify-work`:** Build must succeed + browser verification
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 258-01-01 | 01 | 1 | SIL-01 | code-review + manual | `dotnet build` | ✅ | ⬜ pending |
| 258-01-02 | 01 | 1 | SIL-02 | code-review + manual | `dotnet build` | ✅ | ⬜ pending |
| 258-01-03 | 01 | 1 | SIL-03 | code-review + manual | `dotnet build` | ✅ | ⬜ pending |
| 258-02-01 | 02 | 1 | SIL-04 | code-review + manual | `dotnet build` | ✅ | ⬜ pending |
| 258-02-02 | 02 | 1 | SIL-05 | code-review + manual | `dotnet build` | ✅ | ⬜ pending |
| 258-02-03 | 02 | 1 | SIL-06 | code-review + manual | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. This is a UAT phase — code already exists, testing is via code review + browser verification.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Import silabus Excel | SIL-01 | UI file upload + result display | Upload Excel via ProtonData/ImportSilabus, verify success/error counts |
| Orphan cleanup | SIL-02 | Requires comparing DB state before/after | Import Excel with fewer items, verify orphans deleted |
| Deactivate/reactivate | SIL-03 | Visual verification of hide/show | Toggle IsActive, verify list updates |
| Guidance upload | SIL-04 | File upload via browser | Upload file via Guidance tab, verify file appears |
| Guidance download | SIL-05 | File download verification | Click download as Coach/Coachee role, verify file received |
| Guidance replace/delete | SIL-06 | File management via browser | Replace file, verify new version; delete, verify removed |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
