# Phase 171: Guide & FAQ Cleanup - Context

**Gathered:** 2026-03-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Remove redundant accordion guides covered by PDF tutorials, fix card counts to be dynamic, improve FAQ with reorder + expand/collapse all, integrate AD guide, and add role-based visibility for admin-only content.

</domain>

<decisions>
## Implementation Decisions

### CMP Accordion Simplification
- Remove CMP 3 (Mengerjakan Assessment), CMP 4 (Melihat Hasil), CMP 5 (Download Sertifikat) — fully covered by PDF tutorial
- Keep CMP 1 (Library KKJ), CMP 2 (Mapping KKJ-CPDP), CMP 6 (Training Records), CMP 7 (Monitoring Records Tim)
- After removal: 4 accordion items for workers, 5 for Admin/HC (includes Monitoring)

### CDP Accordion Simplification
- Claude's discretion to analyze PDF Coaching Proton tutorial coverage and remove covered items
- Keep CDP 1 (Plan IDP/Silabus) and CDP 6 (Dashboard) at minimum
- CDP 5 (Approve/Reject) — if kept, must be role-gated to Admin/HC only (see Role-Based Visibility)

### Guide Card Dynamic Counts
- Replace hardcoded "X panduan tersedia" with server-side dynamic count per module + user role
- PDF tutorial counts as +1 in the total guide count per module
- CMP example: 5 for workers (4 accordion + 1 PDF), 6 for Admin/HC (5 accordion + 1 PDF)

### Tutorial PDF Card Styling (GUIDE-04)
- Replace all 6 inline styles with guide.css classes
- Use module color scheme: CMP = purple (#667eea) gradient, CDP = green (#11998e) gradient
- AD tutorial card for admin module uses pink/warning gradient matching admin module

### FAQ Category Reorder
- New order: Akun & Login > Assessment > CDP & Coaching > Umum > KKJ & CPDP > Admin & Kelola Data
- KKJ & CPDP stays as separate category (not merged into Assessment)

### FAQ Item Cleanup
- Remove FAQ items that duplicate PDF tutorial content — Claude analyzes coverage and decides which
- Reorder items within each category by priority (most common/basic questions first, e.g., "Apa itu X" before "Bagaimana cara X")

### Expand/Collapse All
- FAQ section only (not GuideDetail accordions)
- Single toggle button switching between "Buka Semua" and "Tutup Semua"
- Placement: above FAQ section, between subtitle and first category

### Role-Based Visibility
- CDP 5 (Approve/Reject Deliverable) — hide from regular workers, show only to Admin/HC (role-based check, no CoachCoacheeMapping DB query)
- Existing role gates stay: CMP 7 (Admin/HC), CDP 6 (Admin/HC), Admin FAQ category (Admin/HC)
- Dynamic card counts must reflect role-filtered guide count

### AD Tutorial Integration
- Add AD Guide tutorial card to GuideDetail when module="admin" (Admin/HC only)
- Move docs/ActiveDirectory-Guide.html to wwwroot/documents/guides/ for consistency
- Use HTML format with Lihat/Download buttons like existing tutorial cards

### Remaining Accordion Content
- Quick review & polish of kept accordion items for accuracy/consistency
- Minor tweaks only — no major content rewrites

### Claude's Discretion
- Which specific CDP accordion items to remove (analyze PDF coverage)
- Which specific FAQ items to remove as duplicates of PDF tutorials
- Search keyword cleanup on remaining items after removals
- Exact placement and styling of expand/collapse toggle button

</decisions>

<specifics>
## Specific Ideas

- User wants role-based auto-hide: "ada beberapa materi atau tutorial yang hanya untuk admin/hc dan atasan. apakah kamu bisa auto hide" — implemented via server-side role checks
- Dynamic counts should never go stale — derive from actual visible content per role
- AD guide goes to admin module section only

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `guide.css`: Complete styling system with module gradients (--gradient-primary, --gradient-success, --gradient-warning, etc.), step badges, FAQ styling
- Role check pattern: `@if (userRole == "Admin" || userRole == "HC")` already used for CMP 7, CDP 6, Admin FAQ category
- Bootstrap collapse: FAQ uses `data-bs-toggle="collapse"` with `data-bs-target` pattern
- Search system: Client-side filtering via `data-keywords` attributes and `searchable-card`/`searchable-faq` classes

### Established Patterns
- GuideDetail uses `accordion-item` + `guide-list-btn` for expandable guides
- Tutorial PDF cards use `guide-tutorial-card` wrapper class (but inline styles inside — needs fix)
- FAQ categories use `faq-category` with `faq-category-title` headers
- Module color variants: `.icon-cmp`, `.guide-header-cmp`, `.card-cmp` etc.

### Integration Points
- Guide.cshtml line 75-134: Card count text in `<p>` elements — needs dynamic replacement
- GuideDetail.cshtml line 64-111: Tutorial card section — inline styles to replace
- GuideDetail.cshtml line 118-244 (CMP), 246-373 (CDP): Accordion items to remove
- Guide.cshtml line 148-507: FAQ section — reorder categories, remove items, add toggle
- HomeController.cs: May need to pass guide counts via ViewBag

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 171-guide-faq-cleanup*
*Context gathered: 2026-03-16*
