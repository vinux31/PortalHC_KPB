# Phase 29: Auto-transition Upcoming to Open - Context

**Gathered:** 2026-02-21
**Status:** Ready for gap-closure planning (current implementation is date-only; user wants time-based)

<domain>
## Phase Boundary

Assessment sessions with status Upcoming automatically become Open when their scheduled date AND time arrives — HC does not need to manually open each assessment. The transition applies at read-time across the worker assessment list, HC monitoring, and StartExam.

</domain>

<decisions>
## Implementation Decisions

### Transition trigger timing
- Trigger is **time-based**, not date-only: the assessment opens at the exact scheduled time on the scheduled date
- HC sets the opening time **per assessment** when creating/editing (requires a time picker on the Create/Edit form)
- HC inputs times in **WIB (UTC+7)**
- Comparison logic: `Schedule (WIB) <= DateTime.UtcNow.AddHours(7)` — i.e., compare against current WIB time

### Where the transition is applied
- **Worker assessment list** — display-only in-memory override (no DB write)
- **HC monitoring (GetMonitorData)** — display-only in-memory override (no DB write)
- **StartExam** — persisted: when the first worker accesses StartExam and the scheduled time has arrived, `Status = "Open"` is saved to DB
- **Reports (Phase 31)** — apply the same time-based override logic when generating (no need for immediate DB flip)

### Future-dated assessments
- Workers **see** Upcoming assessments in their list (not hidden) — labeled "Upcoming"
- Workers see the opening date **and time** displayed: e.g., "Opens 22 Feb 2026, 08:00 WIB"
- No Start button shown for Upcoming assessments
- **StartExam must block access** for future-scheduled assessments — even if a worker navigates directly to the URL, they are redirected with an error message (not just hidden via UI)

### Claude's Discretion
- Exact error message when worker hits StartExam too early (e.g., "Ujian belum dibuka. Akan dibuka pada [date time] WIB")
- Whether to display WIB label explicitly or just format the time in WIB
- Exact time picker UI component on the Create/Edit form

</decisions>

<specifics>
## Specific Ideas

- HC sets the time in WIB format (e.g., "08:00") — this should be the local Indonesian time shown to HC
- Workers in different timezones (WIB = UTC+7, WITA = UTC+8) will see the assessment become available at different local times, but the authoritative trigger is the WIB time HC set
- The "Opens at [time] WIB" label on the worker's assessment list helps workers know exactly when to return

</specifics>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope

</deferred>

---

## Gap Analysis

The **current Phase 29 implementation** uses date-only comparison (`Schedule.Date <= DateTime.UtcNow.Date`), which diverges from these decisions in three ways:

1. **Time-based comparison** — Currently date-only; needs to compare `Schedule <= DateTime.UtcNow.AddHours(7)` (WIB)
2. **Time picker on Create/Edit form** — HC currently sets only a date; needs a time input added to the assessment Create/Edit form
3. **StartExam time gate** — Currently StartExam only blocks Completed sessions; needs to also block access when `Schedule > DateTime.UtcNow.AddHours(7)` (future-scheduled)
4. **Worker list display** — Currently shows date only; needs to show "Opens [date] [time] WIB" for Upcoming assessments

These gaps should be addressed via `/gsd:plan-phase 29 --gaps` after creating this CONTEXT.md.

---

*Phase: 29-auto-transition-upcoming-to-open*
*Context gathered: 2026-02-21*
