# Phase 17: Question and Exam UX improvements - Context

**Gathered:** 2026-02-19
**Status:** Ready for planning

<domain>
## Phase Boundary

Improve the question management interface for HC (creating/importing test packages with questions) and the exam-taking experience for users (paged layout, navigation, review, submission). Package randomization and ID-based grading are core architectural requirements.

</domain>

<decisions>
## Implementation Decisions

### Exam layout
- **Paged layout**: N questions per page (HC-configurable or fixed at ~10 per page), not one-at-a-time and not all-on-one-page
- **Countdown timer**: Show time remaining; warning state (e.g., red) when low (e.g., last 5 minutes)
- **Header**: Assessment name + progress count only (e.g., "Tes Kompetensi OJT — 7/30 answered")
- **Answer options**: Radio buttons with A/B/C/D letter labels — but grading stores option IDs (not letters), since options are randomized per user

### Question navigation
- **Page-by-page only**: Prev/Next page buttons — no jumping to arbitrary question numbers
- **Unanswered tracking**: Simple; no per-question color status required (keep it simple)
- **Skipping allowed**: User can advance to next page even with unanswered questions on the current page — no blocking
- **Collapsible question panel**: A collapsible sidebar/panel showing question numbers (e.g., 1–10 for current page); can be toggled open/closed

### Pre-submit review
- **Summary page**: After the last question page, user sees a summary showing all questions and their selected answers before final submission
- **Unanswered warning**: If user has unanswered questions, show warning ("You have X unanswered questions. Submit anyway?") but allow submission
- **Submit button location**: On the last exam page (navigates to summary page); then a final Submit button on the summary page
- **Post-submit redirect**: Goes to the existing Results page (score, pass/fail, answer review if enabled)

### Test packages (architecture)
- **Multiple packages per assessment**: Each assessment can have 1–N packages (typically 3); each package has its own question set and answer keys
- **Per-user package assignment**: System randomly assigns each user one package when the assessment starts
- **Per-user question randomization**: Questions within the assigned package are shuffled independently for each user — displayed question numbers are dynamic, not DB order
- **Per-user option randomization**: Answer options (A/B/C/D) are shuffled independently per user
- **ID-based grading**: Grading must use stable question IDs and answer option IDs — never displayed question numbers or letter labels

### Question management (HC)
- **Dedicated page**: Separate "Manage Questions" page per assessment (not inline on Edit Assessment page)
- **Package structure**: HC creates/manages questions per package (Package 1, Package 2, Package 3...)
- **Excel file upload**: HC can upload a .xlsx file per package; each row = one question
- **Paste from Excel**: HC can also copy rows from Excel and paste into a text area; same format, same parser
- **Column format**: `Question | Option A | Option B | Option C | Option D | Correct`
  - "Correct" column uses letter (A/B/C/D) to identify which option is the answer key
  - System maps the letter to the corresponding option and stores the option ID
- **Package assignment**: Random — system auto-assigns each user a package when the assessment opens
- **Preview**: HC can preview what the exam looks like for a user (simulates real exam UI with the package's questions, without randomization applied)

### Claude's Discretion
- Exact number of questions per page (HC-configurable or fixed — Claude decides sensible default, e.g., 10)
- Timer warning threshold (e.g., last 5 minutes turns red)
- Collapsible panel visual design and toggle button placement
- Import error handling (invalid format, missing columns, duplicate questions)
- Summary page layout (table of all answers vs grouped by page)

</decisions>

<specifics>
## Specific Ideas

- "1 page for some question. example: 1 page for 10 question number" — paged display confirmed
- The exam system must support 3 packages where two users getting the same package still see questions in different order
- HC imports packages via Excel (upload or paste) — no manual question-by-question entry needed
- Even with A/B/C/D labels shown to user, grading is never based on the displayed letter

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 17-question-and-exam-ux-improvements*
*Context gathered: 2026-02-19*
