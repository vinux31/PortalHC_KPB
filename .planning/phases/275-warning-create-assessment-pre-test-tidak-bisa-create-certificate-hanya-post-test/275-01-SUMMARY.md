---
phase: 275
plan: 1
status: complete
started: 2026-03-28
completed: 2026-03-28
---

# Summary: 275-01 Warning Pre-Test Certificate

## What was built
Warning alert dinamis di form CreateAssessment yang muncul ketika judul mengandung "pre test"/"pretest"/"pre-test" DAN checkbox "Terbitkan Sertifikat" dicentang.

## Key changes
- Added hidden warning div below certificate checkbox (alert-warning, d-none by default)
- Added JavaScript `checkPreTestWarning()` function with title detection + certificate checkbox monitoring
- Event listeners on Title input and GenerateCertificate change
- Initial check on page load

## Key files
- `Views/Admin/CreateAssessment.cshtml` — warning div (line ~439) + script (line ~1330)

## Deviations
None.

## Self-Check: PASSED
- [x] preTestWarning div exists with d-none and alert-warning classes
- [x] checkPreTestWarning function with pre test/pretest/pre-test detection
- [x] addEventListener for input and change events
- [x] Warning is non-blocking (no form validation)
