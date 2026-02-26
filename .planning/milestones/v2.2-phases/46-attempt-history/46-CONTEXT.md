# Phase 46: Attempt History - Context

**Gathered:** 2026-02-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Preserve assessment attempt data when HC clicks Reset, and upgrade the History tab at /CMP/Records to show all attempts per worker per assessment — including archived attempts from previous resets and current completed sessions.

</domain>

<decisions>
## Implementation Decisions

### Trigger Archival (when to save attempt history)
- Only the **Reset button** in HC monitoring creates a history record
- AbandonExam and ForceClose do NOT trigger archival
- Only save to history if session Status = "Completed" (has score and was submitted)
- Skip archival if session is Abandoned, Not Started, or InProgress (no meaningful data)
- Rule: Reset on Completed session → archive → clear session. Reset on anything else → just clear, no archive.

### History Tab Layout
- Current unified History tab is **replaced with 2 sub-tabs:**
  - **"Riwayat Assessment"** — all assessment attempts (archived + current completed)
  - **"Riwayat Training"** — training manual records (existing content, moved here)
- Sub-tabs live inside the existing "History" tab of RecordsWorkerList — or as a direct split of that tab

### Filters on Riwayat Assessment
- Filter by **worker name or NIP** (search input)
- Filter by **assessment title** (dropdown or search)
- Both filters can be combined

### Attempt # Logic
- Sequential per worker per assessment title: Attempt #1, #2, #3...
- Current completed session (never reset) = **Attempt #1**
- Abandoned attempts are NOT shown in Riwayat Assessment (not saved to history)
- Attempt # for current session computed as: count of archived attempts for that worker+title + 1

### Sort Order in Riwayat Assessment
- **Grouped by assessment title**, then within each group: **date descending** (newest attempt first)

### Claude's Discretion
- New table name and schema for archived attempts (e.g., `AssessmentAttemptHistory`)
- Fields to persist per archive: SessionId, UserId, Title, Category, Score, IsPassed, StartedAt, CompletedAt, AttemptNumber
- EF Core migration approach
- Exact filter UX (dropdown vs text search for assessment title)
- Badge styling for Pass/Fail in the new sub-tab

</decisions>

<specifics>
## Specific Ideas

- The upgrade is to `/CMP/Records` → HC/Admin view (`RecordsWorkerList.cshtml`) → History tab
- Riwayat Training sub-tab keeps existing content unchanged (just moved into a sub-tab)
- Riwayat Assessment sub-tab surfaces: archived AttemptHistory rows + current AssessmentSession rows where Status=Completed — unified in one query

</specifics>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope

</deferred>

---

*Phase: 46-attempt-history*
*Context gathered: 2026-02-26*
