# Phase 119: Deliverable Page Restructure - Context

**Gathered:** 2026-03-08
**Status:** Ready for planning

<domain>
## Phase Boundary

Reorganize the existing Deliverable detail page into 4 clearly separated card sections: Detail Coachee & Kompetensi, Evidence Coach, Approval Chain, Riwayat Status. Remove redundant elements (alert banners, upload form). This is a visual restructure — no changes to approval logic or data model.

</domain>

<decisions>
## Implementation Decisions

### Section layout
- 4 separate Bootstrap cards, not tabs or single-card-with-dividers
- Desktop layout: Detail Coachee & Approval Chain **side-by-side** (row with 2 columns), Evidence Coach and Riwayat Status **full-width** below
- Mobile: all cards stack vertically
- Order top-to-bottom: Detail Coachee & Kompetensi + Approval Chain (side-by-side) > Evidence Coach > Riwayat Status

### Card 1: Detail Coachee & Kompetensi
- Header: "Detail Coachee & Kompetensi"
- Content: Coachee name, Track, Kompetensi, SubKompetensi, Deliverable name (hanya deliverable yang sedang dilihat, bukan list semua deliverable dalam satu sub kompetensi)
- Selalu tampil

### Card 2: Approval Chain
- Header: "Approval Chain" dengan badge status (Pending/Submitted/Approved/Rejected) di pojok kanan header
- Content: Stepper vertikal SrSpv > SH > HC (existing stepper code)
- Rejection reason ditampilkan di bawah step yang menolak (di dalam card ini, bukan alert banner terpisah)
- Tombol Approve/Reject (SrSpv/SH) dan HC Review di bagian bawah card, sesuai role
- Selalu tampil (stepper Pending jika belum ada aksi)

### Card 3: Evidence Coach
- Header: "Evidence Coach"
- Content: File evidence dengan tombol download + tanggal upload, lalu coaching session data (Coach name, Tanggal, Catatan Coach, Kesimpulan, Result badge)
- Selalu 1 coaching session per deliverable (tidak perlu handle multiple sessions)
- **Tersembunyi** jika belum ada evidence/coaching session

### Card 4: Riwayat Status
- Header: "Riwayat Status"
- Content: Timeline kronologis dari DeliverableStatusHistory (existing timeline code dari Phase 117)
- **Tersembunyi** jika belum ada history entry

### Elemen yang dihapus
- Alert banner status (Rejected/Submitted/Approved notices) — digantikan badge di header Approval Chain
- Form Upload Evidence — redundan, coach upload via halaman CoachingProton
- Upload ulang setelah rejection juga via CoachingProton, bukan dari halaman Deliverable

### Breadcrumb
- Disederhanakan: `Coaching Proton > Deliverable` (atau `IDP Plan > Deliverable` jika dari PlanIDP)
- Breadcrumb lama (4-5 level dengan Track, Kompetensi, SubKompetensi) dihapus karena info tersebut sudah di card Detail

### Claude's Discretion
- Exact spacing, padding, font sizes
- Responsive breakpoint untuk side-by-side vs stacked
- Column width ratio untuk Detail vs Approval Chain (misal col-md-6/col-md-6 atau col-md-7/col-md-5)

</decisions>

<specifics>
## Specific Ideas

- Layout referensi: Detail + Approval Chain side-by-side di atas, Evidence Coach dan Riwayat Status full-width di bawah
- Existing stepper dan timeline code dari Phase 117 sudah ada dan berfungsi — restructure memindahkan ke card terpisah
- Badge status warna: Pending (abu), Submitted (biru), Approved (hijau), Rejected (merah) — sudah ada di kode saat ini

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Views/CDP/Deliverable.cshtml`: Existing page with all data display code — restructure moves sections into cards
- Approval Chain stepper (lines 174-279): Vertical stepper with icons, badges, approver names — move to own card
- Status timeline (lines 404-467): Timeline from `DeliverableStatusHistory` — move to own card
- Coaching Reports section (lines 363-402): CoachingSession display with table — merge into Evidence Coach card
- Evidence display (lines 96-115): File download + upload date — merge into Evidence Coach card

### Established Patterns
- Bootstrap cards with `card border-0 shadow-sm` styling (used throughout portal)
- `list-group list-group-flush` for timeline entries
- Badge classes: `bg-secondary`, `bg-primary`, `bg-success`, `bg-danger`
- Breadcrumb with dynamic back link based on referer

### Integration Points
- `DeliverableViewModel` model — may need additional properties for Kompetensi/SubKompetensi names
- `CDPController.Deliverable()` action — may need to pass sibling deliverable data
- `ViewBag.CoachingSessions`, `ViewBag.StatusHistories`, `ViewBag.ApproverNames` — existing data sources
- `CanApprove`, `CanHCReview`, `CanUpload` flags on ViewModel — CanUpload no longer needed in view

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 119-deliverable-page-restructure*
*Context gathered: 2026-03-08*
