# Phase 65: Actions - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Make approve, reject, coaching report, evidence, and export actions persist to the database from the ProtonProgress page. All action buttons work end-to-end: coach submits evidence+coaching, SrSpv/SectionHead approve/reject via modal, HC reviews with confirm, and supervisors can export data to Excel/PDF.

Additionally: update the Deliverable detail page to show coaching report data, and remove the Locked status (all deliverables start as Pending).

</domain>

<decisions>
## Implementation Decisions

### Approve/Reject UX
- Modal-based approval: clicking "Tinjau" button on a row opens a modal with Action dropdown (Approve/Reject) + Comment field
- Comment is **required for Reject**, optional for Approve
- Modal shows full context: Kompetensi, Sub-Kompetensi, Deliverable name, and a link to view/download the uploaded evidence
- "Tinjau" button is a **visible button per row**, not a dropdown item
- Button enabled only when coach has uploaded evidence (status = Submitted)
- **Column ownership:** SrSpv button only in SrSpv column, SectionHead button only in SH column, HC button only in HC column
- **Independent approval:** Either SrSpv OR SectionHead can approve (no sequential dependency between them). Both roles can still approve even if the other already has
- After modal submission: **toast notification + in-place row update** (AJAX, no full page reload)
- **HC Review:** Simple confirm button (not the full modal). Small confirm dialog "Mark as reviewed?"

### Approval Badge Display
- Status badge with **hover tooltip** showing approver name and date
- When SH approves but SrSpv hasn't acted, SrSpv column still shows "Pending" (each column reflects only that role's action)
- Rejection reason is only visible on the **Deliverable detail page**, not in the table

### Rejection Flow
- Status stays **"Rejected"** until coach re-uploads (clear audit trail)
- Rejected deliverable **blocks the next** deliverable in sequence from proceeding
- Coach re-uploads evidence -> status becomes **"Submitted"** again
- Old evidence is **replaced** (no version history)
- On resubmission: approval columns for SrSpv/SH **reset to Pending** (fresh review cycle)
- No visual distinction between first-time submission and re-submission

### Status Flow Redesign
- **Remove Locked status entirely** — all deliverables start as **Pending**
- New flow: Pending -> Submitted -> Approved / Rejected -> (if rejected) -> Submitted
- Badge colors: Pending = Gray, Submitted = Blue, Approved = Green, Rejected = Red
- HC statuses: Pending = Gray, Reviewed = Green

### Combined Evidence + Coaching Modal
- **Single "Submit Evidence" button** that combines evidence submission and coaching report into one modal
- Coach only, visible on **Pending and Rejected** deliverables
- Modal fields:
  1. Header (auto-filled, read-only): Kompetensi, Sub-Kompetensi, Deliverable
  2. Date (auto-filled today, editable)
  3. Kompetensi Coachee (free-text, hint: "Coach menuliskan competencies yang harus dipenuhi sesuai dengan workbook")
  4. Catatan Coach (free-text notes)
  5. Kesimpulan (dropdown: "Kompeten secara mandiri" / "Masih perlu dikembangkan")
  6. Result (dropdown: "Need Improvement" / "Suitable" / "Good" / "Excellence")
  7. File upload (optional, PDF/JPG/PNG, max 10MB)
- **Batch submission:** Coach can select multiple deliverables in one modal (select Kompetensi > Sub-Kompetensi > Deliverable checkboxes). Pre-filled with the clicked deliverable, can add more
- **All fields are shared** across selected deliverables (one set of Catatan/Kesimpulan/Result applies to all)
- **One file** for all selected deliverables (optional)
- **No lock enforcement on selection:** Coach can select any deliverable including previously Locked ones
- All selected deliverables change to **Submitted** on submit
- Creates CoachingSession record(s) linked to selected deliverables
- Coach can **edit** coaching reports but **cannot delete** them
- All roles can view coaching reports (full transparency)
- Previous coaching reports viewable on **Deliverable detail page only** (not inline on progress table)

### Progress Calculation
- **Average-based:** Pending = 0%, Submitted = 50%, Approved = 100%, Rejected = 0%
- Overall progress % = sum of each deliverable's % / total deliverables
- Stat cards updated:
  - Progress % (average calculation)
  - Pending Evidence (counts Pending + Rejected deliverables — both need coach action)
  - Pending Approvals (counts Submitted deliverables awaiting approval, **role-aware** — each role sees only their own pending count)

### Table Column Layout
- Columns: Kompetensi | Sub-Kompetensi | Deliverable | Evidence | Approval Sr.Spv | Approval SH | Approval HC | Detail
- **Evidence column:** Coach sees "Submit Evidence" button; other roles see status badge only (Sudah Upload / Belum Upload)
- **Approval columns:** Contain "Tinjau"/"Review" buttons for respective roles alongside status badges
- **Detail column:** Single "Lihat Detail" link to Deliverable detail page (shows everything: coaching report, evidence file, approval history, rejection reason)
- Keep **rowspan merging** for Kompetensi/Sub-Kompetensi grouping

### Coachee View
- **Read-only table** with all statuses visible (same table, no action buttons)
- Sees own stat cards (Progress %, Pending Evidence, Pending Approvals)
- Can click "Lihat Detail" — full read access to coaching reports, approval status, rejection reasons
- Can **download evidence files** if coach uploaded one
- Badge + tooltip for approval columns (same as other roles — consistent UI)
- No coachee dropdown (sees own data directly) — existing behavior kept
- **No export buttons** (export is SrSpv/SH/HC/Admin only)

### Export
- **Excel (ClosedXML) + PDF** export for current coachee only (what's on screen)
- Excel format: Claude's discretion (flat or merged)
- PDF: Table-style with header (coachee name, export date)
- **Includes coaching data** (Catatan Coach, Kesimpulan, Result) alongside deliverable status
- Buttons at **top of page** near stat cards — "Export Excel" and "Export PDF"
- Filename: `CoacheeName_Progress_YYYY-MM-DD.xlsx` / `.pdf`
- **Roles:** SrSpv, SectionHead, HC, Admin only (not Coach or Coachee)

### Deliverable Detail Page
- Update in this phase to show coaching report data (Catatan, Kesimpulan, Result, uploaded file)
- Shows rejection reason for rejected deliverables
- Evidence file viewable/downloadable

### Claude's Discretion
- Excel format (flat rows vs merged cells)
- PDF table layout details
- Exact modal styling and button placement
- Toast notification design
- Confirm dialog design for HC Review
- Evidence column badge text/style for non-coach roles
- How multi-deliverable selector UI works in the combined modal

</decisions>

<specifics>
## Specific Ideas

- Coach biasanya coaching beberapa deliverable sekaligus dalam 1 sesi — that's why batch submission is important
- Perhitungan per deliverable: Upload Evidence = 50%, Approved = 100%
- "Kompeten secara mandiri" / "Masih perlu dikembangkan" are the standard kesimpulan options from the workbook
- Result options follow a 4-level scale: Need Improvement, Suitable, Good, Excellence

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 65-actions*
*Context gathered: 2026-02-27*
