---
phase: 372-data-foundation-propagasi-toggle
plan: 02
subsystem: api
tags: [aspnet-mvc, assessment-admin-controller, shuffle-toggle, ef-bool-trap, propagation, xunit, real-sql]

requires:
  - phase: 372-01
    provides: ShuffleQuestions/ShuffleOptions entity columns + migration applied
provides:
  - "ShuffleQuestions/ShuffleOptions set explicitly at all 6 form-built new AssessmentSession write-sites (5 model.* + 1 savedAssessment.*) — anti EF bool-trap"
  - "Sibling propagation on EditAssessment: 2 foreach (standard + Pre-Post) set both flags"
  - "SHUF-02 tests (3) + SHUF-03 tests (2), real-SQL, green"
affects: [372-03, 373, 374]

tech-stack:
  added: []
  patterns: ["explicit flag-set at every new-row write-site (entity default insufficient for form-bound rows)", "anchored-insertion keyed on AllowAnswerReview to hit all 8 controller write/propagate sites"]

key-files:
  modified:
    - Controllers/AssessmentAdminController.cs
    - HcPortal.Tests/ShuffleCreatePersistenceTests.cs
    - HcPortal.Tests/ShufflePropagationTests.cs

key-decisions:
  - "Included the 3 add-participant-on-Edit write-sites (newPre/newPost/newSessions) that the spec omitted — prevents a second EF bool-trap for participants added during Edit (CONFIRMED in-scope)"
  - "Site newSessions sources from savedAssessment.* (not model.*), mirroring its AllowAnswerReview anchor"
  - "GET CreateAssessment init (:684) left untouched — entity default = true suffices for render-checked"
  - "SHUF-02/03 tested at data/persistence level (real-SQL); controller wiring covered by anchored-insertion + grep count >= 8 + build (POST actions too heavy to instantiate end-to-end)"

patterns-established:
  - "Anchor every assessment-level field replication on AllowAnswerReview (1:1, never add/drop a site)"

requirements-completed: [SHUF-02, SHUF-03]

duration: ~20min
completed: 2026-06-13
---

# Phase 372 Plan 02: Controller Write-Sites + Propagation Summary

**ShuffleQuestions/ShuffleOptions set explicitly at all 6 form-built AssessmentSession write-sites + 2 sibling-propagation foreach in AssessmentAdminController, with 5 real-SQL tests (SHUF-02 persist ON/OFF/independent, SHUF-03 standard + Pre-Post propagation) green**

## Performance

- **Duration:** ~20 min
- **Tasks:** 2/2 (interactive inline)
- **Files modified:** 3

## Accomplishments
- Re-grepped live anchors (367/368 shipped — no line drift): 6 object-init `new AssessmentSession` + 2 `foreach`, all anchored on `AllowAnswerReview`.
- Inserted `ShuffleQuestions`/`ShuffleOptions` at 8 locations via verified node script (asserts each anchor before inserting; indentation-preserving): Pre/Post/standard create loops + newPre/newPost add-participant (`model.*`), newSessions add-participant (`savedAssessment.*`), F1 Pre-Post `s.*`, F2 standard `sibling.*`. GET init untouched. Self-check `ShuffleQuestions =` count = 8.
- SHUF-02 (3 [Fact]): ON default → true; **OFF explicit → false (not forced true — the real EF bool-trap / DB-DEFAULT concern)**; independent (true/false). SHUF-03 (2 [Fact]): standard 3-sibling group + Pre-Post group all follow model.
- Full suite **313/313 green** — no regression.

## Task Commits

1. **Task 1: set flags at 6 write-sites + 2 foreach** - `512c52af` (feat)
2. **Task 2: SHUF-02 (3) + SHUF-03 (2) real-SQL tests + VALIDATION nyquist_compliant** - `64968e2f` (test)

## Files Created/Modified
- `Controllers/AssessmentAdminController.cs` — 6 object-init + 2 foreach set both flags
- `HcPortal.Tests/ShuffleCreatePersistenceTests.cs` — SHUF-02 (3 [Fact])
- `HcPortal.Tests/ShufflePropagationTests.cs` — SHUF-03 (2 [Fact])

## Decisions Made
- 3 add-participant-on-Edit sites included (spec omitted them) — second EF bool-trap prevention.
- Data-level testing (real-SQL) for SHUF-02/03; controller wiring verified by anchored-insertion + grep≥8 + build. Honest scope note: these tests prove the EF persist/propagate invariant on real SQL, not the full POST action (too many deps to drive end-to-end). The verbatim AllowAnswerReview-anchored insertion gives high confidence the controller sets the same values.

## Deviations from Plan
None - plan executed as written (anchored-insertion via verified script rather than 8 manual edits, due to duplicate anchor text; same result).

## Issues Encountered
None.

## Next Phase Readiness
- Backend persistence + propagation complete → Plan 03 (wizard UI toggles + Step 4 summary) is the last remaining plan.
- Migration (Plan 01) is the only IT-notify item for this phase.

---
*Phase: 372-data-foundation-propagasi-toggle*
*Completed: 2026-06-13*
