---
phase: 194-pdf-certificate-download
plan: 01
subsystem: ui
tags: [questpdf, pdf, certificate, skiasharp, fonts]

# Dependency graph
requires:
  - phase: 192-certificate-number
    provides: NomorSertifikat, ValidUntil fields on AssessmentSession used in PDF footer
  - phase: 191-certificate-fields
    provides: CompletedAt field on AssessmentSession used for certificate date
provides:
  - PDF certificate download via CertificatePdf action (GET /CMP/CertificatePdf/{id})
  - HTML Certificate view updated with NIP, NomorSertifikat, ValidUntil, actual CompletedAt date
  - Playfair Display and Lato TTF fonts in wwwroot/fonts/ for QuestPDF rendering
affects: [future certificate customization, CMP certificate workflow]

# Tech tracking
tech-stack:
  added: [QuestPDF (already present), SkiaSharp canvas API for watermark and score badge]
  patterns: [FontManager.RegisterFont loop from wwwroot/fonts/*.ttf, page.Background().Canvas() for SVG-equivalent watermark, page.Foreground() for overlaid score badge]

key-files:
  created:
    - wwwroot/fonts/PlayfairDisplay-Regular.ttf
    - wwwroot/fonts/PlayfairDisplay-Bold.ttf
    - wwwroot/fonts/PlayfairDisplay-Italic.ttf
    - wwwroot/fonts/Lato-Regular.ttf
    - wwwroot/fonts/Lato-Bold.ttf
    - wwwroot/fonts/Lato-Light.ttf
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Certificate.cshtml

key-decisions:
  - "Font files downloaded from Google Fonts GitHub repo and committed to wwwroot/fonts/ — avoids runtime HTTP dependency"
  - "Triangle watermark rendered via SkiaSharp Canvas API at 5% opacity (alpha=13) — QuestPDF Canvas() used instead of SVG"
  - "Score badge placed on page.Foreground() layer so it overlays all content without affecting layout flow"
  - "Filename pattern: Sertifikat_{NIP}_{SafeTitle}_{Year}.pdf with Regex.Replace for title sanitization"
  - "PDF layout fixed post-checkpoint to match HTML certificate: SVG-style watermark, circular badge, vertically centered content"

patterns-established:
  - "CertificatePdf auth guard: mirrors Certificate action — ownership OR Admin/HC role check before serving binary file"
  - "QuestPDF font registration: loop Directory.GetFiles(fontsPath, *.ttf) + FontManager.RegisterFont(stream)"

requirements-completed: [CERT-03]

# Metrics
duration: ~60min
completed: 2026-03-18
---

# Phase 194 Plan 01: PDF Certificate Download Summary

**QuestPDF A4 landscape certificate download with SVG-style watermark, score badge overlay, and auth-guarded download matching HTML certificate visual design**

## Performance

- **Duration:** ~60 min
- **Started:** 2026-03-18
- **Completed:** 2026-03-18
- **Tasks:** 3 (including human-verify checkpoint)
- **Files modified:** 8

## Accomplishments

- Added CertificatePdf action to CMPController with full auth guards (ownership OR Admin/HC), QuestPDF A4 landscape generation, font registration, and filename pattern Sertifikat_{NIP}_{Title}_{Year}.pdf
- Updated HTML Certificate view to show NIP below recipient name, NomorSertifikat and ValidUntil in footer, and actual CompletedAt date instead of DateTime.Now
- Downloaded and committed Playfair Display + Lato TTF fonts for server-side PDF rendering without runtime HTTP dependency
- Post-checkpoint fix matched PDF layout to HTML: triangle SkiaSharp watermark at 5% opacity, circular score badge via page.Foreground(), vertically centered content

## Task Commits

1. **Task 1: Download font TTF files and update Certificate.cshtml** - `a33d795` (feat)
2. **Task 2: Add CertificatePdf action to CMPController** - `91a636c` (feat)
3. **Post-checkpoint fix: Match PDF layout to HTML version** - `b088307` (fix)
4. **Task 3: Verify PDF certificate download** - checkpoint approved by user

## Files Created/Modified

- `Controllers/CMPController.cs` - Added CertificatePdf action with QuestPDF generation, auth guards, font registration
- `Views/CMP/Certificate.cshtml` - Added NIP display, NomorSertifikat, ValidUntil, actual CompletedAt date, green Download PDF button
- `wwwroot/fonts/PlayfairDisplay-Regular.ttf` - Playfair Display Regular for PDF
- `wwwroot/fonts/PlayfairDisplay-Bold.ttf` - Playfair Display Bold for PDF
- `wwwroot/fonts/PlayfairDisplay-Italic.ttf` - Playfair Display Italic for PDF
- `wwwroot/fonts/Lato-Regular.ttf` - Lato Regular for PDF
- `wwwroot/fonts/Lato-Bold.ttf` - Lato Bold for PDF
- `wwwroot/fonts/Lato-Light.ttf` - Lato Light for PDF

## Decisions Made

- Font files committed to wwwroot/fonts/ to avoid runtime HTTP dependency — consistent with the project's self-contained deployment approach
- PDF layout adjusted post-checkpoint to match the HTML certificate more closely: moved from box-based watermark to SkiaSharp path triangle, used page.Foreground() for the score badge overlay
- Regex.Replace on assessment title sanitizes unsafe filename characters before constructing download filename

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] PDF layout did not match HTML certificate visual design**
- **Found during:** Task 3 checkpoint (user verification)
- **Issue:** Initial PDF layout used a simpler box-based watermark and score placement that did not match the HTML certificate's circular SVG watermark and bottom-right badge position
- **Fix:** Rewrote watermark using SkiaSharp SKPath triangle at 5% opacity; replaced score badge with circular SkiaSharp canvas on page.Foreground() layer; added vertical centering to content column
- **Files modified:** Controllers/CMPController.cs
- **Verification:** User approved after fix
- **Committed in:** b088307

---

**Total deviations:** 1 auto-fixed (Rule 1 bug)
**Impact on plan:** Fix necessary for visual correctness. No scope creep.

## Issues Encountered

None beyond the layout deviation documented above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- PDF certificate download is fully functional and verified
- NomorSertifikat and ValidUntil are now surfaced in both HTML and PDF certificate views
- Ready for next milestone planning

---
*Phase: 194-pdf-certificate-download*
*Completed: 2026-03-18*
