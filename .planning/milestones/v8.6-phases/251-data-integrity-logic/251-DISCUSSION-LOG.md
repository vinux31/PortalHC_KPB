# Phase 251: Data Integrity & Logic - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-24
**Phase:** 251-data-integrity-logic
**Areas discussed:** (auto mode — no interactive discussion)

---

## Auto Mode Summary

All 6 requirements (DATA-01 through DATA-06) are targeted bug fixes from codebase audit with clear, unambiguous implementations. No gray areas required user input.

[auto] All decisions derived directly from requirements and codebase analysis.

### Key Findings During Codebase Scout

| Area | Finding |
|------|---------|
| DATA-01 | 3 locations confirmed: TrainingRecord.cs:77,91 and CertificationManagementViewModel.cs:59 |
| DATA-02 | Unique indexes at ApplicationDbContext.cs:532 (OrganizationUnit) and :559 (AssessmentCategory) |
| DATA-03 | `isRenewalModePost` only checks model FK fields, misses `RenewalFkMap` param for bulk renewal |
| DATA-04 | Past-date validation at AdminController.cs:1727 blocks editing assessments with past schedule |
| DATA-05 | Bare catch at AdminController.cs:1437, `_logger` already available |
| DATA-06 | `_lastScopeLabel` at CDPController.cs:661, called only from Dashboard():286 |

## Claude's Discretion

- Migration naming convention
- DATA-04 exact relaxation approach
- DATA-06 refactor strategy (tuple vs model property)

## Deferred Ideas

None — all items within phase scope.
