---
status: complete
phase: 63-data-source-fix
source: [63-01-SUMMARY.md, 63-02-SUMMARY.md]
started: 2026-02-27T03:00:00Z
updated: 2026-02-27T03:15:00Z
---

## Current Test

[testing complete]

## Tests

### 1. CDP Index card links to Proton Progress
expected: Navigate to /CDP/Index. The card formerly labeled "Progress & Tracking" now shows title "Proton Progress" and description about Proton deliverable monitoring. Button says "Proton Progress".
result: pass

### 2. ProtonProgress page loads
expected: Click the "Proton Progress" button on CDP Index. The /CDP/ProtonProgress page loads with a header "Proton Progress", a "Kembali" back button, and role-appropriate content (dropdown or direct data depending on role).
result: pass

### 3. Old /CDP/Progress URL redirects to Index
expected: Manually navigate to /CDP/Progress in the browser. You are redirected to /CDP/Index (the CDP hub page), NOT to /CDP/ProtonProgress.
result: pass

### 4. Coach coachee dropdown shows real coachees
expected: As a Coach (Level 5), the ProtonProgress page shows a "Pilih Coachee" dropdown. The dropdown lists real coachees assigned via CoachCoacheeMapping — not mock/hardcoded names. If no coachees assigned, dropdown is disabled with "Tidak ada coachee".
result: pass

### 5. Selecting coachee loads data via AJAX (no page reload)
expected: Select a coachee from the dropdown. A loading spinner appears briefly, then the table and stat cards populate WITHOUT a full page reload (URL stays the same, no flash/blink).
result: pass

### 6. Deliverable table with rowspan merging
expected: The table shows columns: Kompetensi, Sub Kompetensi, Deliverable, Evidence, Approval Sr. Spv, Approval Section Head, Approval HC, Aksi. Kompetensi and Sub Kompetensi cells are merged vertically (rowspan) when values repeat across rows.
result: skipped
reason: No coachee has track assignment with deliverable data — cannot verify table rendering

### 7. Summary stat cards display correct values
expected: Three stat cards appear above the table: Progress % (with progress bar), Pending Actions count, Pending Approvals count. Values reflect the selected coachee's actual data.
result: skipped
reason: No coachee has deliverable data to verify stats

### 8. Track label displayed near coachee name
expected: After selecting a coachee, their name appears with a badge showing their track assignment (e.g., "Panelman Tahun 2") displayed OUTSIDE the table, near the coachee name — not as a table column.
result: skipped
reason: No coachee has track assignment

### 9. Evidence column shows badge status only
expected: The Evidence column shows only a badge: green "Uploaded" or gray "Pending". No file names, no previews, no links in the evidence column.
result: skipped
reason: No deliverable data to verify evidence column

### 10. Action column redirects to Deliverable page
expected: The Aksi column has a dropdown with "Lihat Detail". Clicking it navigates to the Deliverable page for that item — no inline approve/reject actions on the Progress page.
result: skipped
reason: No deliverable data to verify action column

### 11. Coachee (Level 6) sees own data without dropdown
expected: As a Coachee (Level 6), the ProtonProgress page shows your own name and track badge directly — no dropdown selector. Your deliverable table and stats load automatically.
result: pass

### 12. Switching coachee updates stats and table
expected: Switch to a different coachee in the dropdown. The stat cards, table rows, track label, and coachee name all update to reflect the newly selected coachee's data.
result: skipped
reason: No coachee has deliverable data to verify switching behavior

## Summary

total: 12
passed: 6
issues: 0
pending: 0
skipped: 6

## Gaps

[none — 6 skipped tests require track assignment + deliverable data to verify]
