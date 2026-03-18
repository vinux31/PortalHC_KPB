# Phase 195: Sub-Categories & Signatory Settings - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Add hierarchical sub-categories to AssessmentCategory (self-referencing ParentId FK, up to 2 levels deep), Admin CRUD on ManageCategories with indented list and signatory user selection, grouped optgroup dropdown on both CreateAssessment wizard and EditAssessment, per-category signatory rendered as P-Sign on certificate. Certificate design updated to Design A2 (Pertamina logo header, compact P-Sign, no QR code).

</domain>

<decisions>
## Implementation Decisions

### Sub-category Hierarchy
- Two levels deep maximum: Parent > Child > Grandchild
- Self-referencing nullable ParentId FK on AssessmentCategory
- Add form gets optional "Parent Category" dropdown (shows only categories where depth < 2)
- Block delete on categories that have children — must delete children first

### Admin ManageCategories UI
- Indented list in existing table: children shown indented under parent row
- Add/Edit form gains: Parent Category dropdown + Signatory user dropdown
- Signatory dropdown shows all users (no role filter), searchable
- Mini P-Sign preview appears below signatory dropdown after selection (shows how it will look on certificate)

### Wizard & Edit Dropdown
- CreateAssessment wizard: HTML `<optgroup>` with parent as group label, children as options
- EditAssessment: same optgroup-style dropdown
- Any level selectable — both parents and leaf categories can be assigned to assessments

### Signatory Configuration
- New SignatoryUserId nullable FK on AssessmentCategory → ApplicationUser
- Admin selects a user account; system reads their FullName + Position for P-Sign
- Inheritance: sub-category with no signatory falls back to parent's signatory
- Global fallback: if no signatory on category or parent, certificate shows old static "Authorized Sig." + line + "HC Manager"

### Certificate Design (A2)
- Header: Pertamina logo image (reuse psign-pertamina.png) + "HC PORTAL KPB" text + "Human Capital Development Portal" subtitle — replaces old text-only icon header
- Footer layout: Date section (left) — P-Sign (right, shifted slightly left with margin-right)
- No QR code
- P-Sign style on certificate: no border, no padding, no "KPB" line — compact (logo + position + name only)
- P-Sign sized larger than Settings version: logo ~48px, font 0.85-0.9rem
- Score badge stays as-is (bottom-right circle)
- Both HTML Certificate.cshtml and QuestPDF CertificatePdf updated to Design A2
- Settings page _PSign.cshtml partial unchanged (keeps border + KPB line)

### Migration & Seed Data
- Add ParentId (nullable int, self-ref FK) and SignatoryUserId (nullable string FK to AspNetUsers) columns to AssessmentCategory
- Existing categories get null for both new columns — admin sets values manually
- No default signatory assigned during migration

### Claude's Discretion
- Exact spacing/typography adjustments in certificate layout
- Searchable dropdown implementation (Select2, tom-select, or native)
- P-Sign preview rendering approach on ManageCategories (partial view, JS template, etc.)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Certificate design
- `wwwroot/cert-preview-A2.html` — Approved certificate design mockup (Design A2) with logo header, compact P-Sign, no QR
- `Views/CMP/Certificate.cshtml` — Current HTML certificate to be updated
- `Controllers/CMPController.cs` (CertificatePdf action ~line 2332+) — QuestPDF PDF generation to be updated
- `Views/Shared/_PSign.cshtml` — Existing P-Sign partial (DO NOT modify)
- `Models/PSignViewModel.cs` — P-Sign view model (LogoUrl, Position, Unit, FullName)

### Category management
- `Models/AssessmentCategory.cs` — Current model (needs ParentId + SignatoryUserId)
- `Controllers/AdminController.cs` (ManageCategories ~line 759) — Current CRUD actions
- `Views/Admin/ManageCategories.cshtml` — Current admin UI to be extended
- `Views/Admin/CreateAssessment.cshtml` — Wizard category dropdown (needs optgroup)
- `Views/Admin/EditAssessment.cshtml` — Edit form category dropdown (needs optgroup)

### P-Sign system
- `wwwroot/images/psign-pertamina.png` — Pertamina logo (reuse for certificate header + P-Sign)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `_PSign.cshtml` partial + `PSignViewModel`: existing P-Sign badge system — certificate P-Sign uses same data model (FullName, Position) but different rendering (no border, no Unit line)
- `psign-pertamina.png`: logo already in wwwroot/images, reusable for certificate header
- `AssessmentCategory` model: already has Id, Name, DefaultPassPercentage, IsActive, SortOrder — extend with ParentId + SignatoryUserId

### Established Patterns
- ManageCategories: existing Add/Edit/Delete pattern with TempData messages and PRG redirects
- CreateAssessment/EditAssessment: ViewBag.Categories passes `IEnumerable<AssessmentCategory>` to views
- Certificate auth: 4-guard pattern (owner/Admin/HC, Completed, GenerateCertificate, IsPassed)

### Integration Points
- Certificate.cshtml footer section (lines 263-287): signature area to be replaced with P-Sign
- CertificatePdf QuestPDF: signature area rendering to match new HTML
- AdminController ManageCategories actions: extend for parent/signatory fields
- ViewBag.Categories in Create/Edit: needs to include parent-child hierarchy for optgroup rendering

</code_context>

<specifics>
## Specific Ideas

- Certificate design approved as "Design A2" — see `wwwroot/cert-preview-A2.html` for exact visual reference
- P-Sign on certificate: Pertamina logo + Position (e.g., "HC Manager") + FullName — no border, no Unit/KPB line, no padding
- User specifically wants P-Sign like the existing system on Account/Settings and CDP/Deliverable evidence reports — picking a user account so the P-Sign data auto-populates

</specifics>

<deferred>
## Deferred Ideas

- QR code verification on certificate — discussed and explicitly removed from scope (user decided not needed)
- Public certificate verification page — not needed
- Training hours/duration on certificate — not in scope for this phase

</deferred>

---

*Phase: 195-certificate-signatory-settings*
*Context gathered: 2026-03-18*
