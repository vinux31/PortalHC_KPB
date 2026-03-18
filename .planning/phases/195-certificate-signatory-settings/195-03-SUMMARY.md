---
phase: 195-certificate-signatory-settings
plan: 03
subsystem: ui
tags: [certificate, questpdf, signatory, pertamina, design-a2]

requires:
  - phase: 195-01
    provides: AssessmentCategory.SignatoryUserId FK + ApplicationUser.Signatory nav prop

provides:
  - CMPController.ResolveCategorySignatory helper (category name → signatory chain → HC Manager fallback)
  - Certificate.cshtml Design A2 header (Pertamina logo + HC PORTAL KPB text)
  - Certificate.cshtml Design A2 footer (P-Sign: logo + position + name, no border)
  - CertificatePdf QuestPDF updated with logo header and dynamic P-Sign footer

affects:
  - certificate-display
  - pdf-generation

tech-stack:
  added: []
  patterns:
    - ResolveCategorySignatory: async helper resolving category by name string, walking parent chain, returning PSignViewModel with HC Manager fallback

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Certificate.cshtml

key-decisions:
  - "Signatory lookup uses string match (c.Name == categoryName) because AssessmentSession.AssessmentCategory is a string field, not FK"
  - "Fallback chain: category signatory → parent signatory → static HC Manager (FullName empty)"
  - "No QR code on certificate — removed entirely from both HTML and PDF versions"
  - "P-Sign rendered without border, compact — logo + position label + name only"

patterns-established:
  - "ResolveCategorySignatory: place private helper after the public action that uses it for co-location"
  - "ViewBag.PSign set in Certificate action, cast to (HcPortal.Models.PSignViewModel?) in view"

requirements-completed: [R195-4]

duration: 10min
completed: 2026-03-18
---

# Phase 195 Plan 03: Certificate Signatory Settings — Design A2 Summary

**Pertamina logo header + dynamic category signatory P-Sign footer in both HTML and PDF certificates, with category→parent→HC Manager fallback chain**

## Performance

- **Duration:** 10 min
- **Started:** 2026-03-18T02:00:00Z
- **Completed:** 2026-03-18T02:10:00Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments

- Added `ResolveCategorySignatory` private async helper to CMPController: looks up category by name string, walks to parent, falls back to static HC Manager
- Updated Certificate.cshtml header: replaced bootstrap icon + text with Pertamina logo + "HC PORTAL KPB" / "Human Capital Development Portal" (Design A2)
- Updated Certificate.cshtml footer: replaced static "Authorized Sig." / "HC Manager" with dynamic P-Sign badge (logo + position + name, no border)
- Updated CertificatePdf QuestPDF: Row header with Pertamina logo image + text column; right footer column with Pertamina logo + pSign.Position + pSign.FullName
- No QR code anywhere in certificate (neither HTML nor PDF)

## Task Commits

1. **Task 1: Design A2 certificate — signatory resolution + header/footer update** - `6e24c89` (feat)

## Files Created/Modified

- `Controllers/CMPController.cs` - Added `ResolveCategorySignatory` helper, `ViewBag.PSign` in Certificate action, `pSign` variable in CertificatePdf, updated QuestPDF header and footer rendering
- `Views/CMP/Certificate.cshtml` - Design A2 header (Pertamina logo + text), Design A2 P-Sign footer (dynamic signatory), removed static signature block

## Decisions Made

- Signatory lookup uses `c.Name == categoryName` string match (AssessmentSession.AssessmentCategory is string, not FK — categories table is for settings only)
- Helper placed as private method between Certificate and CertificatePdf actions for locality
- QuestPDF footer uses `row.AutoItem()` for the P-Sign column (not `RelativeItem`) to prevent the logo expanding to fill the full right half

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Design A2 certificate complete for both HTML and PDF
- Signatory settings UI (Phase 195 plan 01/02) allows Admin to assign signatories per category
- Full certificate flow ready: create assessment → complete → view certificate with category signatory

---
*Phase: 195-certificate-signatory-settings*
*Completed: 2026-03-18*
