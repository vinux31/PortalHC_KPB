---
phase: 94
slug: cdp-section-audit
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-05
---

# Phase 94 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None — manual browser verification only |
| **Config file** | N/A (no automated tests in project) |
| **Quick run command** | N/A (browser testing manual) |
| **Full suite command** | N/A (browser testing manual) |
| **Estimated runtime** | ~30 minutes (3 flows × 5 roles) |

---

## Sampling Rate

- **After every task commit:** Smoke test the specific bug/feature fixed in browser
- **After every plan wave:** Full flow verification for affected workflows
- **Before `/gsd:verify-work`:** All 3 flows (IDP Planning, Coaching Workflow, Evidence & Approval) verified PASS by user
- **Max feedback latency:** ~5 minutes (developer test) + ~30 minutes (user verification cycle)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 94-01-01 | 01 | 1 | CDP-01 | manual | Browser verification | ❌ Manual only | ⬜ pending |
| 94-02-01 | 02 | 2 | CDP-02, CDP-03 | manual | Browser verification | ❌ Manual only | ⬜ pending |
| 94-02-02 | 02 | 2 | CDP-05 | manual | Browser verification | ❌ Manual only | ⬜ pending |
| 94-03-01 | 03 | 3 | CDP-04 | manual | Browser verification | ❌ Manual only | ⬜ pending |
| 94-03-02 | 03 | 3 | CDP-06 | manual | Browser verification | ❌ Manual only | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements — no Wave 0 needed.

**Rationale:** Project uses manual browser verification pattern established in Phase 85/93. Automated test infrastructure does not exist and is not required for this audit phase.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| PlanIdp loads for all 5 roles | CDP-01 | Requires UI interaction + role context | Log in as Coachee, Coach, Spv, SectionHead, HC/Admin — verify PlanIdp page loads without errors |
| CoachingProton shows correct coachee lists | CDP-02 | Requires database relationships + role scoping | As Coach, verify coachee dropdown shows only assigned coachees; as Spv, verify coachee list scoped by level |
| Progress approval workflows work per role | CDP-03 | Requires multi-step approval chain | Submit coaching session as Coachee → Coach approval → Spv/SH approval → verify status transitions |
| Evidence upload/download works | CDP-04 | Requires file I/O + browser file handling | Upload PDF/JPG/PNG evidence file, verify file saved and download returns correct content |
| Coaching session flows complete end-to-end | CDP-05 | Requires complete workflow traversal | Create session → add notes → submit → approve through all levels → verify final status |
| Forms handle validation gracefully | CDP-06 | Requires error condition testing | Submit empty forms, invalid files (>10MB), wrong extensions — verify TempData error messages display |

---

## Validation Sign-Off

- [ ] All tasks have manual verification steps defined
- [ ] Sampling continuity: bug fixes verified after each commit
- [ ] Wave 0: N/A (manual testing phase)
- [ ] No watch-mode flags (not applicable)
- [ ] Feedback latency: ~35 minutes (developer + user cycle)
- [ ] `nyquist_compliant: false` — Manual testing only (acceptable for audit phase)

**Approval:** pending

---

*Phase: 94-cdp-section-audit*
*Validation strategy created: 2026-03-05*
