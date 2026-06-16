---
phase: 385-exam-taking-image-render-hotfix
plan: 01
subsystem: ui
tags: [razor, pathbase, image, exam, playwright]

requires:
  - phase: 354-355 (image in assessment)
    provides: "_QuestionImage.cshtml partial single-source render <img> + lightbox 6 surface"
provides:
  - "_QuestionImage.cshtml PathBase-aware: src + data-img-src resolve via Url.Content('~'+path)"
  - "e2e image-pathbase-385: gambar load 200 via URL ber-prefix /KPB-PortalHC (naturalWidth>0)"
affects: [exam-taking, StartExam, ExamSummary, Results, essay-grading, image-render]

tech-stack:
  added: []
  patterns:
    - "Path render-time selalu PathBase-aware via Url.Content('~'+leadingSlashPath) — preseden Certificate.cshtml"

key-files:
  created:
    - tests/e2e/image-pathbase-385.spec.ts
  modified:
    - Views/Shared/_QuestionImage.cshtml

key-decisions:
  - "Variabel lokal resolvedSrc di blok @{} (bukan method @functions static) — IUrlHelper ambient di body Razor, tak butuh using"
  - "Defensive D-01a: prepend '/' bila path tak diawali '/'; bila diawali '~' jangan double-resolve"
  - "e2e akses via URL absolut ber-prefix (page.fill leading-slash buang path baseURL) — reproduce PathBase nyata"

patterns-established:
  - "Single-source partial fix = 1 file cover 6 surface (StartExam/ExamSummary/Results/grading + lightbox)"

requirements-completed: [PXF-01]

duration: ~20min
completed: 2026-06-15
---

# Phase 385 Plan 01: Gambar Soal PathBase-Aware (F-09/PXF-01) Summary

**`_QuestionImage.cshtml` render `<img src>` + lightbox `data-img-src` via `Url.Content("~"+path)` → gambar soal/opsi tidak lagi 404 di sub-path `/KPB-PortalHC` (Dev/Prod), e2e terbukti load 200 + naturalWidth>0.**

## Performance
- **Duration:** ~20 min
- **Completed:** 2026-06-15
- **Tasks:** 2
- **Files modified:** 1 (+1 created)

## Accomplishments
- `_QuestionImage.cshtml`: var `resolvedSrc` resolve PathBase (`/KPB-PortalHC/uploads/..` di sub-path, `/uploads/..` di lokal); src + lightbox pakai nilai sama; guard null/whitespace dipertahankan.
- e2e `image-pathbase-385.spec.ts`: akses StartExam via URL absolut ber-prefix → assert src `/KPB-PortalHC/uploads/` + `naturalWidth>0` + tak ada 404 ke `/uploads/questions/`. PASS 2/2.

## Task Commits
1. **Task 1: _QuestionImage PathBase-aware** - `c5b0a478` (fix)
2. **Task 2: e2e gambar load ber-prefix** - `e2412496` (test)

## Files Created/Modified
- `Views/Shared/_QuestionImage.cshtml` - resolusi PathBase render-time (src + data-img-src)
- `tests/e2e/image-pathbase-385.spec.ts` - e2e reproduce + verifikasi via URL ber-prefix

## Decisions Made
- Pendekatan variabel lokal (bukan @functions method) — paling minim risiko, IUrlHelper ambient.
- e2e WAJIB URL absolut ber-prefix: `page.goto('/x')` leading-slash membuang path baseURL → nav jatuh ke origin bare → app layani bare (UsePathBase passthrough) → src tak ber-prefix. URL absolut memaksa request via PathBase.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug in plan] Nama project file salah di verify command**
- **Found during:** Task 1 (dotnet build)
- **Issue:** Plan verify pakai `PortalHC_KPB.csproj` (tidak ada). Project aktual `HcPortal.csproj`.
- **Fix:** Build `HcPortal.csproj` — 0 error.
- **Files modified:** none (command-only)
- **Verification:** `dotnet build HcPortal.csproj` EXIT 0.
- **Committed in:** N/A (tak ada perubahan file)

**2. [Rule 1 - Test bug] e2e nav awal tidak ber-prefix**
- **Found during:** Task 2 (run pertama)
- **Issue:** `test.use({baseURL})` + `page.goto('/CMP/Assessment')` leading-slash → URL resolution buang prefix → StartExam dimuat bare → src tak ber-prefix → assert gagal.
- **Fix:** Hapus test.use, pakai `page.goto('http://localhost:5277/KPB-PortalHC/CMP/Assessment')` absolut → AJAX VerifyToken ber-prefix → redirectUrl server-gen ber-prefix.
- **Verification:** re-run PASS, URL StartExam ber-prefix, src `/KPB-PortalHC/uploads/`, naturalWidth>0.
- **Committed in:** `e2412496`

---
**Total deviations:** 2 (1 plan-bug verify cmd, 1 e2e nav fix). **Impact:** Tak ada scope creep; fix produksi tepat seperti plan.

## Issues Encountered
None pada kode produksi — fix `_QuestionImage.cshtml` benar di run pertama (test admin-create + render PASS). Penyesuaian hanya di konstruksi e2e.

## User Setup Required
None.

## Next Phase Readiness
- PXF-01 selesai + e2e PASS lokal. UAT final di Dev (`http://10.55.3.3/KPB-PortalHC` StartExam bergambar) diserahkan IT setelah re-deploy.
- Migration: FALSE (tak ubah DB).

---
*Phase: 385-exam-taking-image-render-hotfix*
*Completed: 2026-06-15*
