---
phase: 95
slug: admin-portal-audit
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-05
---

# Phase 95 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual smoke testing (browser verification) |
| **Config file** | none — ASP.NET MVC application |
| **Quick run command** | `dotnet build` (verify compilation) |
| **Full suite command** | Browser smoke test per plan |
| **Estimated runtime** | ~5-10 minutes per plan |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` to verify compilation
- **After every plan wave:** Smoke test via browser (verify specific bug fixed)
- **Before `/gsd:verify-work`:** All smoke tests must pass
- **Max feedback latency:** 30 seconds (build) + 5 minutes (smoke test)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 95-01-01 | 01 | 1 | ADMIN-01 | code-review | N/A | N/A | ⬜ pending |
| 95-01-02 | 01 | 1 | ADMIN-01 | smoke-test | Manual browser test | N/A | ⬜ pending |
| 95-02-01 | 02 | 1 | ADMIN-03 | code-review | N/A | N/A | ⬜ pending |
| 95-02-02 | 02 | 1 | ADMIN-03 | smoke-test | Manual browser test | N/A | ⬜ pending |
| 95-03-01 | 03 | 2 | ADMIN-07 | code-review | N/A | N/A | ⬜ pending |
| 95-03-02 | 03 | 2 | ADMIN-07 | smoke-test | Manual browser test | N/A | ⬜ pending |
| 95-04-01 | 04 | 2 | ADMIN-08 | code-review | N/A | N/A | ⬜ pending |
| 95-04-02 | 04 | 2 | ADMIN-08 | smoke-test | Manual browser test | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements — ASP.NET MVC application with manual smoke testing approach.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| ManageWorkers page loads and filters work | ADMIN-01 | UI interaction requires browser | 1. Login as Admin/HC 2. Navigate to /Admin/ManageWorkers 3. Test filters (Nama, Unit, Bagian) 4. Verify pagination |
| CoachCoacheeMapping assignment works | ADMIN-03 | UI interaction requires browser | 1. Login as Admin/HC 2. Navigate to /Admin/CoachCoacheeMapping 3. Select coach and coachee 4. Click Assign 5. Verify assignment created |
| Validation errors display correctly | ADMIN-07 | UI feedback requires browser | 1. Attempt to submit form with invalid data 2. Verify TempData["Error"] message shown (not raw exception) |
| Role gates work correctly | ADMIN-08 | Auth requires browser session | 1. Login as Admin 2. Access HC-only actions (if any) 3. Login as HC 4. Access Admin-only actions (if any) 5. Verify correct behavior |

---

## Validation Sign-Off

- [ ] All tasks have code-review or smoke-test verification
- [ ] Sampling continuity: no 3 consecutive tasks without verification
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s (build) + 5m (smoke test)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
