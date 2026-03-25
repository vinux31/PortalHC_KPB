---
phase: 257
slug: setup-mapping
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-25
---

# Phase 257 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual UAT (browser-based) |
| **Config file** | N/A — UAT phase |
| **Quick run command** | Claude code review + user browser verification |
| **Full suite command** | Checklist MAP-01..08 all pass |
| **Estimated runtime** | ~30 minutes (manual browser testing) |

---

## Sampling Rate

- **After every task commit:** Claude code review before user verifies
- **After every plan wave:** User verifikasi semua requirements di browser
- **Before `/gsd:verify-work`:** All MAP-01..08 checklist pass
- **Max feedback latency:** N/A (manual — depends on user availability)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 257-01-01 | 01 | 1 | MAP-01 | manual | Browse /Admin/CoachCoacheeMapping | N/A | ⬜ pending |
| 257-01-02 | 01 | 1 | MAP-02 | manual | POST /Admin/CoachCoacheeMappingAssign | N/A | ⬜ pending |
| 257-01-03 | 01 | 1 | MAP-05 | manual | Verify DB after assign with TrackId | N/A | ⬜ pending |
| 257-01-04 | 01 | 1 | MAP-08 | manual | Assign Tahun 2+ track, observe warning | N/A | ⬜ pending |
| 257-02-01 | 02 | 1 | MAP-04 | manual | GET /Admin/DownloadMappingImportTemplate | N/A | ⬜ pending |
| 257-02-02 | 02 | 1 | MAP-03 | manual | POST /Admin/ImportCoachCoacheeMapping | N/A | ⬜ pending |
| 257-03-01 | 03 | 1 | MAP-06 | manual | POST /Admin/CoachCoacheeMappingDeactivate | N/A | ⬜ pending |
| 257-03-02 | 03 | 1 | MAP-07 | manual | POST /Admin/CoachCoacheeMappingReactivate | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. Ini UAT phase — semua verifikasi manual via browser.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| List page loads with data, pagination, search | MAP-01 | Browser UI verification | Navigate to /Admin/CoachCoacheeMapping, verify data table, pagination, search |
| Assign modal creates mapping | MAP-02 | Browser form interaction | Click Assign, fill modal, submit, verify new row |
| Import Excel processes rows | MAP-03 | File upload + result display | Upload Excel file, verify summary (Success/Error/Skip/Reactivated) |
| Template download | MAP-04 | File download verification | Click download template, verify Excel file |
| Track assignment auto-created | MAP-05 | DB state verification | After assign with TrackId, check ProtonTrackAssignment in DB |
| Deactivate cascades | MAP-06 | DB cascade verification | Deactivate mapping, verify TrackAssignment also deactivated |
| Reactivate reuses assignment | MAP-07 | DB state verification | Reactivate mapping, verify TrackAssignment reused (not new) |
| Progression warning | MAP-08 | UI warning display | Assign Tahun 2+ with incomplete Tahun sebelumnya, verify warning |

---

## Validation Sign-Off

- [ ] All tasks have manual verify steps defined
- [ ] Sampling continuity: Claude code review before every user test
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] All MAP-01..08 pass in browser
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
