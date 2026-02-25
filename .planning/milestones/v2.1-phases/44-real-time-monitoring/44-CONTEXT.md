---
phase: 44-real-time-monitoring
created: 2026-02-25
status: ready-for-planning
---

# Phase 44: Real-Time Monitoring — Context

**Phase goal:** HC's AssessmentMonitoringDetail page auto-updates worker status, progress, scores, and time remaining without manual refresh.

---

## Area 1: Update Interval

| Decision | Value |
|----------|-------|
| Poll interval | 10 seconds |
| Stop condition | All sessions in group have Status = Completed |
| Stop behavior | Silent — no toast, no message, polling just stops |
| Endpoint type | New JSON GET endpoint |
| Endpoint route | `/CMP/GetMonitoringProgress?title=&category=&scheduleDate=` |
| On fetch failure | Show a small error indicator (not a disruptive toast); retry on next cycle |

---

## Area 2: What Data Updates

| Column | Live update source |
|--------|--------------------|
| Status | Session.Status from endpoint |
| Progress | `X/Total` — X = count of PackageUserResponse rows for session (auto-save driven); Total = total package questions |
| Score | SessionScore when available (post-Completed) |
| Result (Pass/Fail) | Derived from Score vs passing threshold when Completed |
| Time Remaining | Client-side JS countdown; endpoint provides `remainingSeconds` snapshot each cycle to re-sync drift |

**Summary counts** (top of page — total workers, completed, in-progress, not-started): also update on each polling cycle.

**Open sessions (Status = "Not Started"):** No countdown shown — display `—` or `Belum Mulai` in Time Remaining cell. Countdown only starts once `Status = InProgress`.

---

## Area 3: Visual Feedback

| Decision | Value |
|----------|-------|
| Row flash on change | None — silent update, no highlight |
| Last updated indicator | Yes — show "Last updated: HH:MM:SS" somewhere on the page (e.g., below table or in header area) |
| Stop behavior | Silent — polling just stops, no notification to HC |

---

## Area 4: Table Behavior

### Column changes

| Current column | New column |
|----------------|------------|
| Name | Name (keep) |
| NIP | **Removed** |
| Jumlah Soal | **Progress** (format: `X/Total`) |
| Status | Status (keep, live) |
| Score | Score (keep, live) |
| Result | Result (keep, live) |
| Completed At | Completed At (keep) |
| — | **Time Remaining** (new, client-side countdown) |
| Actions | Actions (keep, live-updated) |

### DOM targeting

- `data-session-id="{sessionId}"` attribute on each `<tr>`
- Targeted cell updates per row — no full table re-render on each poll
- Actions buttons update live when Status changes (see Actions section below)

### Actions buttons (live update)

| Status | Buttons shown |
|--------|---------------|
| Not Started | Reshuffle + ForceClose |
| InProgress | Reshuffle (disabled) + ForceClose |
| Completed | **View Results** + Reset |
| Abandoned | Reset |

- **View Results** button: label `"View Results"`, class `btn-success btn-sm`, opens `/CMP/Results/{sessionId}` in new tab (`target="_blank"`)
- Appears immediately in the same DOM update as Status → Completed (atomic update)
- Not shown for Abandoned sessions

### Group-level UI

- **"Tutup Lebih Awal"** button: hidden when all sessions in the group have Status = Completed (client checks `groupStatus == Completed` after each poll)

---

## Deferred Ideas

*(Out of scope for Phase 44 — capture for later)*

- Real-time push via SignalR/WebSockets (currently polling-only)
- Per-question breakdown view from monitoring page
- Export monitoring data to CSV

---

## Implementation Notes for Planner

- Endpoint returns array of session DTOs; each DTO contains: `sessionId`, `status`, `progress` (answered count), `totalQuestions`, `score`, `result`, `remainingSeconds`, `completedAt`
- Summary counts can be derived client-side from the array (count by status) rather than a separate server field
- `groupStatus = Completed` condition: `sessions.every(s => s.status === 'Completed')`
- Time Remaining countdown: `setInterval` at 1s to decrement; re-sync to server `remainingSeconds` on each 10s poll to prevent drift
- Endpoint uses same title/category/scheduleDate params already used by the page (read from existing query string or hidden fields)
- No antiforgery needed — GET endpoint, read-only
