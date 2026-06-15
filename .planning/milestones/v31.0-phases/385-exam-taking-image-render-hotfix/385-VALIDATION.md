---
phase: 385
slug: exam-taking-image-render-hotfix
status: audited
nyquist_compliant: true
nyquist_outcome: compliant
wave_0_complete: true
created: 2026-06-16
audited: 2026-06-16
backfill: true
---

# Phase 385 — Validation (Audited, backfill)

> Backfill audit (no VALIDATION.md was authored during the hotfix). Both REQ have green automated e2e coverage. Phase shipped + browser-UAT 2/2 APPROVED.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + Playwright (tests/e2e) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj · tests/playwright.config.ts |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` (454/454 GREEN at phase close) |
| **E2E command** | `cd tests; npx playwright test image-pathbase-385 essay-flush-385 --workers=1` (4/4 PASS) |
| **Estimated runtime** | ~60s unit · ~90s e2e |

---

## Per-Task Verification Map

| Requirement | Plan | Wave | Behavior | Test Type | Test File / Tests | Status |
|-------------|------|------|----------|-----------|-------------------|--------|
| PXF-01 | 385-01 | W1 | `_QuestionImage` `<img src>` + lightbox `data-img-src` via `Url.Content("~"+path)` → question/option images load (200, naturalWidth>0) under sub-path `/KPB-PortalHC`, no 404 to bare `/uploads/` | e2e (Playwright) | `tests/e2e/image-pathbase-385.spec.ts` — (1) admin upload gambar soal+opsi, (2) peserta StartExam src ber-prefix `/KPB-PortalHC/uploads/` + `naturalWidth>0` + no 404. 2/2 PASS | ✅ COVERED |
| PXF-03 | 385-02 | W1 | `flushEssay()` awaited before submit/changePage/blur → last essay keystroke saved (no SignalR-debounce data loss), no false "belum dijawab" reject | e2e (Playwright) | `tests/e2e/essay-flush-385.spec.ts` — (1) admin buat soal essay, (2) peserta ketik→submit langsung (tanpa debounce)→`PackageUserResponses.TextAnswer` utuh exact-match + no false-reject banner + lands Results. 2/2 PASS | ✅ COVERED |

*Status: ✅ COVERED (automated, green) · 🟡 MANUAL-ONLY · ❌ MISSING*

**Coverage:** 2/2 REQ automated (PXF-01, PXF-03) · 0 manual-only · 0 MISSING.

---

## Manual-Only Verifications

*None — both behaviors have automated e2e verification.*

> Deployment note (NOT a test-coverage gap): PXF-01's real PathBase 404 only reproduces on the Dev/Prod sub-path deployment. The e2e reproduces it locally by driving an absolute prefixed URL (`http://localhost:5277/KPB-PortalHC/...`) so the request traverses UsePathBase. A final browser UAT on `http://10.55.3.3/KPB-PortalHC` post-redeploy remains a deploy-gate handled by IT (385-UAT.md), separate from test coverage.

---

## Validation Sign-Off

- [x] Every REQ has automated verification
- [x] Automated tests green (454/454 unit + 4/4 e2e at phase close)
- [x] No watch-mode flags
- [x] No manual-only gaps
- [x] `nyquist_compliant: true`

**Approval:** audited — COMPLIANT

---

## Validation Audit 2026-06-16
| Metric | Count |
|--------|-------|
| Requirements | 2 |
| Automated (COVERED) | 2 (PXF-01, PXF-03) |
| Manual-only | 0 |
| Missing | 0 |

State B backfill (no VALIDATION.md existed; reconstructed from 385-01/02 SUMMARY + e2e specs). v31.0 auto-close chain — closes the v30.0-style gap where a hotfix phase shipped without a validation audit.
