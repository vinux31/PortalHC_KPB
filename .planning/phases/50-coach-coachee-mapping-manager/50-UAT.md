---
status: complete
phase: 50-coach-coachee-mapping-manager
source: 50-01-SUMMARY.md, 50-02-SUMMARY.md
started: 2026-02-27T12:00:00Z
updated: 2026-02-27T12:30:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Admin/Index Card Navigation
expected: On /Admin page, Section B shows "Coach-Coachee Mapping" card with no "Segera" badge, no faded opacity. Clicking the card navigates to /Admin/CoachCoacheeMapping.
result: pass

### 2. Assign Modal — Bulk Create
expected: Clicking "Tambah Mapping" opens a modal with coach dropdown, section filter for coachees, scrollable coachee checklist, optional ProtonTrack dropdown, StartDate. Selecting a coach, checking coachees, and clicking "Simpan" creates the mappings. Page reloads and new mappings appear in the grouped table.
result: pass

### 3. Grouped-by-Coach Table Display
expected: After assigning, /Admin/CoachCoacheeMapping shows the new mapping in a grouped table with coach header row (name, section, active count) and coachee rows (Name, NIP, Section, Position, Status, Start Date, Actions).
result: pass
note: "User requested adding Proton Track column to the coachee rows table"

### 4. Bootstrap Collapse Toggle
expected: Clicking a coach header row toggles (collapses/expands) the coachee rows beneath it. Chevron icon indicates state.
result: pass

### 5. Assign Validation
expected: Trying to assign a coachee who already has an active coach shows an error. Trying to assign a coach to themselves shows an error. Both are blocked.
result: pass

### 6. Edit Modal
expected: Clicking "Edit" opens the Edit modal with pre-populated fields (coachee name read-only, current coach, track, start date). Changing coach and clicking "Simpan" updates the mapping.
result: pass

### 7. Deactivate — Two-Step Flow
expected: Clicking "Nonaktifkan" opens confirmation modal with coachee name and active session count. Confirming soft-deletes the mapping (grey/muted, "Inactive" badge).
result: pass

### 8. Show All Toggle + Inactive Styling
expected: Checking "Tampilkan Semua" reveals inactive mappings with grey/muted styling. Unchecking hides them.
result: pass

### 9. Reactivate
expected: With "Tampilkan Semua" enabled, clicking "Aktifkan" on inactive mapping restores it to active.
result: pass

### 10. Section Filter
expected: Section dropdown auto-submits and narrows displayed coach groups to matching section.
result: pass

### 11. Text Search
expected: Search box filters by coach/coachee name or NIP.
result: pass

### 12. AuditLog Entries
expected: /Admin/AuditLog shows entries for Assign, Edit, Deactivate, Reactivate actions with correct action type and actor info.
result: pass

### 13. Excel Export
expected: "Export Excel" downloads CoachCoacheeMapping.xlsx with 10 columns including Current Track. All mappings included.
result: pass

## Summary

total: 13
passed: 12
issues: 1
pending: 0
skipped: 0

## Gaps

- truth: "Coachee rows in grouped table show Proton Track column (e.g., Panelman Tahun X, Operator Tahun X)"
  status: failed
  reason: "User requested: tambahkan di tabel kolom Proton Track (Panelman tahun x atau Operator tahun x)"
  severity: minor
  test: 3
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""
