---
phase: 318
plan: 04
status: completed
commit: d84309bd
date: 2026-05-12
requirements-completed: [QA-08]
---

# Plan 318-04 Summary — FLOW R Cert PDF + FLOW S AllowAnswerReview Paired (11/11 PASS)

## Outcome

**FLOW R 5/5 + FLOW S 6/6 HIJAU + cumulative regression 49/49 + APIRequest pattern verified.**

## Files Modified

| File | LOC delta | Purpose |
|------|-----------|---------|
| `tests/e2e/helpers/examTypes.ts` | +42 LOC | Append `verifyCertificatePdfDownload` (APIRequest cookies-inherited) |
| `tests/e2e/exam-types.spec.ts` | +311 LOC | Append FLOW R (5 sub-tests) + FLOW S (6 sub-tests) |

## FLOW R (5/5 PASS, 24.9s)

| Test | Runtime | Verify |
|------|---------|--------|
| R1 — HC wizard generateCertificate=true | 4.6s | assessmentId>0 |
| R2 — HC createDefaultPackage + MC Q | 3.5s | packageId>0 |
| R3 — Worker correct answer + DB | 4.5s | IsPassed=1, Status=Completed, Score=100 |
| R4 — APIRequest PDF download | 1.9s | 200 + pdf + Sertifikat_ + 159406 bytes |
| R5 — DB NomorSertifikat | 79ms | `KPB/005/V/2026` |

**R4 observed:**
- PDF size: **159,406 bytes** (well above 1024 byte zero-byte guard threshold)
- Filename: `Sertifikat_29007720__318_R__Cert_Exam_1778553832465_2026.pdf`
- Pattern: `Sertifikat_{NIP}_{safeTitle}_{year}.pdf` (per `CMPController.cs:1898-1962`)

**R5 observed:**
- NomorSertifikat: `KPB/005/V/2026`
- Format inferred: `KPB/{sequence}/{month-roman}/{year}` — sync generated via `GradingService.cs:288-322` retry-3x WHERE IS NULL

## FLOW S (6/6 PASS, 33.7s)

| Test | Runtime | Verify |
|------|---------|--------|
| S1 — Assessment A (allowAnswerReview=true) | 6.7s | aIdTrue>0, package created |
| S2 — Worker submits A | 4.4s | sessTrue captured |
| S3 — Results A positive | 1.2s | `.card "Tinjauan Jawaban"` visible + list-group-items>0 + `.alert-info` count=0 |
| S4 — Assessment B (allowAnswerReview=false) | 6.5s | aIdFalse>0, package created |
| S5 — Worker submits B | 4.1s | sessFalse captured |
| S6 — Results B negative | 1.2s | `.alert-info "tidak tersedia"` visible + `.card "Tinjauan Jawaban"` count=0 |

**Razor branch verified post Plan 02 SURF-317-A fix** — `Views/CMP/Results.cshtml:316-399` markers (`<h5>Tinjauan Jawaban</h5>` + `<div class="alert alert-info">...tidak tersedia...</div>`) preserved.

## Cumulative Regression

```
exam-types.spec.ts: 49/49 PASS (3.2m)
  setup (1) + W0.x (2) + FLOW K (5) + L (6) + M (5) + N (4) + O (5) + P (6) + Q (4) + R (5) + S (6)
```

Phase 317 baseline 28/28 preserved (no regression dari Plan 04 append).

## Wave 0 YELLOW Assumption — Resolution

| Assumption | Status | Detail |
|------------|--------|--------|
| A4 — APIRequest cookies inheritance untuk binary PDF | RESOLVED | `page.request.get()` inherits page context cookies sucessfully. R4 PASS dengan response 200 + bytes 159KB |

## Deviation dari Plan 04 RESEARCH

1. **S3 acceptance `list-group-item count=1` relaxed** — Razor view (`Results.cshtml:354-386`) renders **1 question wrapper + 4 option wrappers** = 5 list-group-items per question, not 1. Switched to `count>0` assertion (verify at least 1 item present).
2. **R3/R5 use `queryString` (bukan `queryScalar`)** untuk Status dan NomorSertifikat string lookups — same fix pattern dari Plan 03.

## Build + Type Gate

- `cd tests && npx tsc --noEmit`: **exit 0**

## QA-08 Coverage Status (post Plan 04)

| FLOW | Status | Phase |
|------|--------|-------|
| FLOW P PreTest/PostTest paired full cycle | ✓ 6/6 | 318-03 |
| FLOW Q ExamWindowCloseDate reject | ✓ 4/4 | 318-03 |
| FLOW R Certificate PDF download + NomorSertifikat | ✓ 5/5 | 318-04 |
| FLOW S AllowAnswerReview true vs false paired | ✓ 6/6 | 318-04 |

**QA-08 4/4 FLOWs covered.** Target ≥40 sub-tests met (49 actual).

## Next

**Plan 318-05** — docs finalize + REQUIREMENTS.md QA-08 entry + ROADMAP.md sync + Phase 318 consolidated summary report + final regression gate.
