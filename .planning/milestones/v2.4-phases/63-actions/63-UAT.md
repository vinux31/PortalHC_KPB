---
status: complete
phase: 65-actions
source: 65-01-SUMMARY.md, 65-02-SUMMARY.md, 65-03-SUMMARY.md
started: 2026-02-27T11:00:00Z
updated: 2026-02-27T11:05:00Z
---

## Current Test

[testing complete]

## Tests

### 1. ProtonProgress Table Layout
expected: Navigate to the ProtonProgress page with a coachee selected. The table should display 8 columns: Kompetensi | Sub Kompetensi | Deliverable | Evidence | Approval Sr. Spv | Approval SH | Approval HC | Detail.
result: pass
note: "UX suggestion — when bagian+unit selected but no coachee, message 'Tidak ada data yang sesuai filter' is misleading. Should indicate user needs to select a coachee for data to appear."

### 2. SrSpv Approval Flow
expected: Log in as Sr Supervisor role. On ProtonProgress, deliverables with Status=Submitted and SrSpvApprovalStatus=Pending should show a "Tinjau" button in the Sr. Spv column. Clicking it opens a modal with Action dropdown (Approve/Reject) and a comment field. Selecting Approve and submitting should update the badge in-place to "Approved" without page reload, and a success toast notification appears.
result: skipped
reason: User/struktur belum di-develop, belum bisa test role-based flow

### 3. SH Approval Flow
expected: Log in as Section Head role. On ProtonProgress, deliverables with Status=Submitted and ShApprovalStatus=Pending should show a "Tinjau" button in the SH column. Approving via the modal updates the SH badge in-place to "Approved" with a success toast.
result: skipped
reason: User/struktur belum di-develop, belum bisa test role-based flow

### 4. HC Review Flow
expected: Log in as HC or Admin role. On ProtonProgress, deliverables with Status=Submitted and HCApprovalStatus=Pending should show a "Review" button in the HC column. Clicking and reviewing updates the HC badge in-place to "Reviewed" with a success toast.
result: skipped
reason: User/struktur belum di-develop, belum bisa test role-based flow

### 5. Rejection Flow
expected: Using SrSpv or SH role, click Tinjau on a Submitted deliverable. Select "Reject" from the Action dropdown — the comment field should be required. Submit with a rejection reason. The badge updates to "Rejected", overall Status becomes Rejected, and a toast notification appears.
result: skipped
reason: User/struktur belum di-develop, belum bisa test role-based flow

### 6. Approval Badge Tooltips
expected: After a deliverable has been approved by any role, hover over the approval badge (e.g. "Approved" in SrSpv column). A tooltip should appear showing the approver's name and the approval date/time.
result: skipped
reason: User/struktur belum di-develop, belum bisa test role-based flow

### 7. Submit Evidence Modal (Coach)
expected: Log in as Coach role. On ProtonProgress, Pending or Rejected deliverables should show a "Submit Evidence" button in the Evidence column. Clicking it opens a modal with: a batch deliverable selector (grouped by Kompetensi > Sub Kompetensi > Deliverable with checkboxes), date field, koacheeCompetencies, catatanCoach, kesimpulan, result, and evidenceFile upload field.
result: skipped
reason: User/struktur belum di-develop, belum bisa test role-based flow

### 8. Evidence Submission Result
expected: As Coach, fill in the evidence modal fields, optionally attach a PDF/JPG/PNG file (max 10MB), select one or more deliverables via the batch selector, and submit. After successful submission: the Evidence cell updates to "Sudah Upload" badge, SrSpv and SH approval columns reset to "Pending", stat cards on the page recalculate, and a success toast appears.
result: skipped
reason: User/struktur belum di-develop, belum bisa test role-based flow

### 9. Export Excel Button and Download
expected: Log in as SrSpv, SH, HC, or Admin (Level 1-4). With a specific coachee selected on ProtonProgress, Export Excel button should be visible. Clicking it downloads a .xlsx file named CoacheeName_Progress_YYYY-MM-DD.xlsx with 10 columns: Kompetensi, Sub Kompetensi, Deliverable, Evidence, Approval SrSpv, Approval SH, Approval HC, Catatan Coach, Kesimpulan, Result.
result: skipped
reason: User/struktur belum di-develop, belum bisa test role-based flow

### 10. Export PDF Button and Download
expected: With the same conditions as Export Excel, an Export PDF button should be visible. Clicking it downloads a PDF file in A4 landscape layout with the coachee name as header and a table showing the progress data.
result: skipped
reason: User/struktur belum di-develop, belum bisa test role-based flow

### 11. Deliverable Detail — Coaching Reports
expected: Navigate to a Deliverable detail page (click "Lihat Detail" from ProtonProgress). If coaching sessions have been submitted for this deliverable, a Coaching Reports card should appear showing each session ordered newest-first, with: Coach name, date, CoacheeCompetencies, CatatanCoach, Kesimpulan, and a Result badge.
result: skipped
reason: User/struktur belum di-develop, belum bisa test role-based flow

### 12. Role-Aware Pending Approvals Count
expected: The pendingApprovals count displayed on ProtonProgress should be role-aware: SrSpv sees count of deliverables pending their SrSpv approval, SH sees count pending their SH approval, HC sees count pending HC review. Other roles see the total pending count.
result: skipped
reason: User/struktur belum di-develop, belum bisa test role-based flow

## Summary

total: 12
passed: 1
issues: 0
pending: 0
skipped: 11

## Gaps

[none yet]
