# Project Retrospective: Portal HC KPB

*A living document updated after each milestone. Lessons feed forward into future planning.*

---

## Milestone: v2.2 — Attempt History

**Shipped:** 2026-02-26
**Phases:** 1 (Phase 46) | **Plans:** 2 | **Tasks:** 4

### What Was Built
- AssessmentAttemptHistory table — archive row written at Reset time, preserving Score, IsPassed, AttemptNumber, timestamps
- Archival logic in ResetAssessment — Completed sessions only; archive + reset share one SaveChangesAsync
- Unified history query — merged archived + current Completed sessions with batch Attempt # computation (GroupBy avoids N+1)
- Dual sub-tab History tab — Riwayat Assessment + Riwayat Training with client-side filters (worker search + title dropdown)

### What Worked
- Archive-before-clear pattern: inserting the archive block before UserResponse deletion meant session field values were still available — no extra query needed to capture them
- Batch count pattern: computing archived AttemptNumber via one GroupBy query + ToDictionary lookup eliminated N+1 for all current session rows
- Tuple return from helper: returning `(assessment, training)` from GetAllWorkersHistory() kept the two sorted/shaped lists cleanly separated without a discriminator flag

### What Was Inefficient
- Plan spec said "3 plans" but 2 plans covered all requirements cleanly — the spec was slightly over-estimated; quick review before planning could have reduced to 2 upfront

### Patterns Established
- **Archive-before-clear**: When resetting stateful records, archive the current row *before* deletions/resets so field values are still available in memory. Share the downstream SaveChangesAsync.
- **Batch count for sequence numbers**: Compute `AttemptNumber` as `existingRows.Count + 1` using a single `GroupBy` across all (UserId, Title) pairs, then dictionary lookup per row — no sequence column needed.
- **Nested Bootstrap sub-tabs**: `ul.nav.nav-tabs` inside an existing `div.tab-pane` works cleanly for two-level navigation; default active sub-tab set via `active show` classes.
- **Client-side `data-*` filter**: `data-worker` + `data-title` attributes on `<tr>` elements; JS filterAssessmentRows() reads both inputs and sets `row.style.display` — no round-trip, works with static server render.

### Key Lessons
1. EF migrations require `--configuration Release` when the Debug build exe is locked by a running process — standard environment constraint for this project.
2. `GetAllWorkersHistory()` returning a tuple is appropriate when two result sets have fundamentally different shapes (sort order, columns) — don't force them into a single typed list.
3. For sequential numbering without a DB sequence: count existing rows for the same (UserId, title) key, add 1. Consistent at both archive time (Plan 01) and query time (Plan 02).

### Cost Observations
- Model profile: budget
- 1-day milestone (one sitting)
- Fast execution: 2 plans × ~10 min average = ~20 min total active work

---

## Cross-Milestone Trends

| Milestone | Phases | Plans | Days | Avg plans/day |
|-----------|--------|-------|------|---------------|
| v1.0 | 3 | 10 | 1 | 10 |
| v1.1 | 5 | 13 | 2 | 6.5 |
| v1.2 | 4 | 7 | 1 | 7 |
| v1.3 | 3 | 3 | 1 | 3 |
| v1.4 | 1 | 3 | 1 | 3 |
| v1.5 | 1 | 7 | 1 | 7 |
| v1.6 | 3 | 3 | 1 | 3 |
| v1.7 | 6 | 14 | 2 | 7 |
| v1.8 | 6 | 10 | 2 | 5 |
| v1.9 | 5 | 8 | 2 | 4 |
| v2.0 | 3 | 5 | 1 | 5 |
| v2.1 | 5 | 13 | 2 | 6.5 |
| v2.2 | 1 | 2 | 1 | 2 |

**Running total:** 46 phases, ~98 plans, 12 days
