---
phase: 378
slug: fix-cmp-certificationmanagement-route-500
status: approved
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-14
---

# Phase 378 — Validation Strategy

> Per-phase validation contract. Reconstructed from artifacts (State B — research skipped, bug-fix phase). All requirements have automated verification.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (unit) + Playwright (e2e) |
| **Config file** | `HcPortal.Tests/` (xUnit) · `tests/playwright.config.ts` (baseURL http://localhost:5277) |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet test` + `npx playwright test exam-types.spec.ts --workers=1` |
| **Estimated runtime** | build ~35s · dotnet test ~80s (361 tests) · e2e Y0 ~20s |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (gate utama delete-heavy — tangkap orphan-reference)
- **After plan complete:** `dotnet test` (361/361) + e2e `-g "Y0" --workers=1`
- **Before verify:** full suite green
- **Max feedback latency:** ~35s (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 378-01-01 | 01 | 1 | CMPRT-01 | T-378-01 | redirect 302 fixed-target ("CertificationManagement","CDP"), no open-redirect | build | `dotnet build` | ✅ | ✅ green |
| 378-01-02 | 01 | 1 | CMPRT-01 | T-378-02 | 6 method dead removed; CDP [Authorize] intact; surface reduced | build + unit | `dotnet build` + `dotnet test` | ✅ | ✅ green (361/361) |
| 378-01-03 | 01 | 1 | CMPRT-01 | — | 2 builder orphan removed; BuildSertifikatRowsAsync KEEP | build | `dotnet build` | ✅ | ✅ green |
| 378-01-04 | 01 | 1 | CMPRT-01 | T-378-04 | GET /CMP/CertificationManagement → redirect → /CDP 200 (no 500, no loop) | e2e | `npx playwright test exam-types.spec.ts -g "Y0" --workers=1` | ✅ | ✅ green (2 passed) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Coverage:** CMPRT-01 = COVERED (automated). SC2 (no-500/redirect) → e2e Y0 assert `page.url()` /CDP + `status 200` + `not 500`. SC1 (entry→CDP audit) → `dotnet build` + grep evidence (Views/CMP/Index.cshtml:98 unchanged). SC3 (CDP no-regression) → e2e FLOW X/W0.X0/Y1/Y2 + `dotnet test` 361/361 (git diff CDP empty).

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. Tidak ada test baru di-scaffold — Task 4 me-tighten test e2e **existing** (Y0 di `tests/e2e/exam-types.spec.ts`) dari documenting-only jadi assert. xUnit + Playwright sudah terpasang.

---

## Manual-Only Verifications

All phase behaviors have automated verification. (SC2 e2e Y0; SC3 e2e CDP-flows + dotnet test; SC1 build + grep audit.)

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify (dotnet build per task; e2e Y0 on Task 4)
- [x] Sampling continuity: tiap task ada automated verify (no 3 consecutive gap)
- [x] Wave 0 covers all MISSING references (none — existing infra)
- [x] No watch-mode flags (`--workers=1`, no `--watch`)
- [x] Feedback latency < 35s (build)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-14

---

## Validation Audit 2026-06-14

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |

CMPRT-01 COVERED oleh automated e2e Y0 (live PASS 2/2) + dotnet build (0 err) + dotnet test (361/361). 0 MISSING, 0 PARTIAL. State B reconstruct (research skipped, bug-fix). nyquist-compliant.
