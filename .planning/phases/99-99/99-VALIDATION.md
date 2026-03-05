---
phase: 99
slug: notification-database-service
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-05
---

# Phase 99 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None (manual testing only - existing project pattern) |
| **Config file** | None |
| **Quick run command** | Manual browser verification (existing project pattern from Phase 82-91) |
| **Full suite command** | Manual testing across use-case flows |
| **Estimated runtime** | ~5-10 minutes per verification round |

---

## Sampling Rate

- **After every task commit:** Manual verification in browser
- **After every plan wave:** Full use-case flow testing
- **Before `/gsd:verify-work`:** All 5 success criteria verified in browser
- **Max feedback latency:** User follows pattern: Claude analyzes code → user verifies in browser → Claude fixes bugs

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 99-01-01 | 01 | 1 | INFRA-01 | manual | Verify schema via SSMS or EF Core migration SQL | ❌ W0 | ⬜ pending |
| 99-01-02 | 01 | 1 | INFRA-07 | manual | Check ApplicationDbContext.Notification and UserNotification DbSets | ❌ W0 | ⬜ pending |
| 99-02-01 | 02 | 1 | INFRA-02 | manual | Check Program.cs line ~50 for AddScoped<INotificationService> | ❌ W0 | ⬜ pending |
| 99-02-02 | 02 | 1 | INFRA-09 | manual | Mock database failure, verify main workflow continues | ❌ W0 | ⬜ pending |
| 99-02-03 | 02 | 1 | INFRA-07 | manual | Insert test notification, query database to verify audit fields | ❌ W0 | ⬜ pending |
| 99-03-01 | 03 | 1 | INFRA-08 | manual | Call SendAsync() for each trigger type, verify message format | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

**Note:** Project currently has no automated tests (verified via HcPortal.csproj and directory scan). All testing is manual in browser following existing project pattern. Phase 99 should continue this pattern rather than introducing test infrastructure mid-milestone. Automated testing can be v3.4 initiative.

**Existing infrastructure covers all phase requirements via manual testing.**

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Notification and UserNotification tables exist with proper indexes | INFRA-01 | No automated test framework | Verify schema via SSMS or EF Core migration SQL, check indexes on UserId, IsRead, CreatedAt DESC |
| NotificationService registered as scoped dependency in Program.cs | INFRA-02 | No automated test framework | Check Program.cs line ~50 for AddScoped<INotificationService, NotificationService> |
| SendAsync() creates notifications with audit trail | INFRA-07 | No automated test framework | Insert test notification, query database to verify CreatedBy, CreatedAt, ReadAt, DeliveryStatus populated |
| Notification templates provide consistent messaging | INFRA-08 | No automated test framework | Call SendAsync() for each trigger type (assessment_assigned, coaching_proton_submitted, etc.), verify message format consistency |
| Notification failures gracefully degrade | INFRA-09 | No automated test framework | Mock database failure (stop SQL service), verify main workflow continues without crash |

---

## Validation Sign-Off

- [x] All tasks have `<manual>` verify or Wave 0 dependencies (manual testing per existing project pattern)
- [x] Sampling continuity: verification via user browser testing (existing pattern)
- [x] Wave 0 covers all MISSING references (manual testing - existing infrastructure)
- [x] No watch-mode flags
- [x] Feedback latency: User-driven (existing pattern)
- [ ] `nyquist_compliant: true` set in frontmatter (pending Wave 0 completion)

**Approval:** pending

---

*Phase: 99-99*
*Validation strategy: 2026-03-05*
