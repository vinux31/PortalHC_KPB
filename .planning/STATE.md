# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-19)

**Latest milestone:** v1.5 Question and Exam UX — IN PROGRESS
**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 17 — Question and Exam UX Improvements (5/7 plans complete)

## Current Position

**Milestone:** v1.5 Question and Exam UX
**Phase:** 17 of 17 (Question and Exam UX Improvements)
**Status:** In Progress (5/7 plans complete)
**Last activity:** 2026-02-19 — Phase 17 Plan 05 complete: Paged exam layout with 10Q/page JS navigation, countdown timer (red at 5 min), collapsible question number panel, answered counter, Review and Submit on last page

Progress: [██████████░░░░░░░░░░] 71% (v1.5, 5/7 plans)

## Performance Metrics

**Velocity (v1.0–v1.3):**
- Total plans completed: 32
- Average duration: ~5 min/plan
- Total execution time: ~2.5 hours

**v1.3 Phase Summary:**

| Phase | Plans | Duration |
|-------|-------|----------|
| 13-navigation-and-creation-flow | 1 | ~3 min |
| 14-bulk-assign | 1 | ~25 min |

**Recent Trend:** Phase 14 was longer due to multi-task complexity (sibling query, picker UI, JS, transaction).

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.

**v1.5 decisions (Phase 17):**
- No Letter field on PackageOption — letters (A/B/C/D) are display-only at render time; grading uses PackageOption.Id
- UserPackageAssignment -> AssessmentPackage FK uses Restrict (not Cascade) — assignments must survive package deletion post-exam
- Shuffle data stored as JSON strings on UserPackageAssignment, not as join table rows
- Unique index on UserPackageAssignment(AssessmentSessionId, UserId) — one assignment per session per user
- ManagePackages uses ViewBag (untyped) — consistent with how Assessment action passes data to its view
- Import Questions button in ManagePackages links to ImportPackageQuestions action — wired in 17-03
- ImportPackageQuestions: ClosedXML .xlsx parser skips header row; TSV paste auto-detects header; Correct=A/B/C/D maps to option index 0/1/2/3
- ShuffledOptionIdsPerQuestion serialized with string keys (.ToDictionary(kv => kv.Key.ToString(), ...)) — JSON object keys must be strings; GetShuffledOptionIds() parses string keys back to int
- StartExam.cshtml @model updated to PackageExamViewModel immediately in 17-04 (not deferred to 17-05) — required for compile; view now uses AssessmentSessionId, TotalQuestions, DisplayNumber, QuestionId, OptionId
- Radio buttons use name=radio_{questionId} (not answers[{questionId}]) so JS change events work independently of hidden form inputs — avoids double-submission
- Letters A/B/C/D assigned at render time by option index, not stored in model or DB
- id=page_@(page) uses explicit parentheses to prevent Razor compiler treating @page as a @page directive token

**v1.4 decisions:**
- In-memory grouping after ToListAsync() for monitor query — consistent with existing manage view pattern
- MonitoringGroupViewModel is the canonical shape for all monitoring data (Plans 02 and 03 depend on this)
- DateTime.UtcNow.AddDays(-30) cutoff for recently-closed sessions — UTC matches CompletedAt storage
- 70% pass rate threshold for green/red display in monitoring tab — matches default PassPercentage config; display heuristic only

**v1.3 decisions (now in PROJECT.md):**
- Separate cards per concern on CMP Index — Assessment Lobby (all roles) + Manage Assessments (HC/Admin) as independent cards
- Sibling session matching uses Title+Category+Schedule.Date for bulk assign
- Already-assigned users excluded at Razor render time, not JS
- Phase 15 Quick Edit cancelled — EditAssessment page covers the need without extra controller surface area

### Pending Todos

None.

### Blockers/Concerns

None.

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 001 | (prior) | — | — | — |
| 002 | (prior) | — | — | — |
| 003 | Verify and clean all remaining Assessment Analytics access points in CMP after card removal | 2026-02-19 | 8e364df | [3-verify-and-clean-all-remaining-assessmen](.planning/quick/3-verify-and-clean-all-remaining-assessmen/) |
| 004 | Add persistent Create Assessment button to Assessment manage view header for HC users | 2026-02-19 | b9518d6 | [4-when-hc-want-to-make-new-assessment-wher](.planning/quick/4-when-hc-want-to-make-new-assessment-wher/) |
| 005 | Group manage view cards by assessment (Title+Category+Schedule.Date) — 1 card per assessment, compact user list, group delete | 2026-02-19 | 8d0b76a | [5-group-manage-view-cards-by-assessment](.planning/quick/5-group-manage-view-cards-by-assessment/) |

### Roadmap Evolution

- Phase 8 added (post-v1.1 fix): Fix admin role switcher and add Admin to supported roles
- Phases 9-12 defined for v1.2 UX Consolidation (2026-02-18)
- Phases 13-15 defined for v1.3 Assessment Management UX (2026-02-19)
- Phase 14 BLK scope updated: EditAssessment page extension, not a separate bulk assign view (2026-02-19)
- Phase 15 Quick Edit removed: feature reverted before shipping — Edit page is sufficient, reduces controller surface area (2026-02-19)
- v1.3 milestone archived (2026-02-19)
- Phase 16 defined for v1.4 Assessment Monitoring (2026-02-19)
- Phase 17 added: Question and Exam UX improvements (2026-02-19)

## Session Continuity

Last session: 2026-02-19
Stopped at: Completed 17-question-and-exam-ux-improvements 17-05-PLAN.md
Resume file: None.
