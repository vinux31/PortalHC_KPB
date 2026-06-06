---
phase: 318
name: PreTest/PostTest full cycle + ExamWindowCloseDate + Certificate PDF E2E
slug: pretest-posttest-full-cycle-examwindowclosedate-certificate-pdf-e2e
milestone: v16.0
requirements: [QA-08]
depends_on: [317]
created: 2026-05-12
mode: discuss
---

# Phase 318 — Context Decisions

## Domain Boundary

E2E test coverage untuk **advanced exam features** + **SURF-317 carryover fix** (production code + test fixture). Test scope = user-observable QA-08 outcomes; fix scope = SURF-317-A (production code) + SURF-317-A1 (legacy test selector patch). Phase boundary FIXED dari ROADMAP — no scope creep ke admin features (Phase 319 QA-09 territory).

## Reused Assets (Phase 317 Carryover)

- `tests/e2e/helpers/examTypes.ts` — 8 exports (createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm, submitExamTwoStep, checkMAOptionsForQuestion, fillEssayAnswer, gradeSingleEssaySession, addExtraTimeViaModal) + QuestionInput discriminated union + CreateAssessmentOpts interface.
- `tests/e2e/helpers/wizardSelectors.ts` — selectors (Phase 307/308 preserved), wizardSelectors (4-step wizard), questionFormSelectors, extraTimeSelectors. **Wizard `assessmentType: '#assessmentTypeInput'` selector available untuk PrePostTest switching.**
- `tests/e2e/exam-types.spec.ts` — 27 sub-tests existing (smoke W0 + FLOW K-O). Phase 318 APPEND FLOW P/Q/R/S.
- `tests/helpers/dbSnapshot.ts` — `db.queryScalar` pattern untuk DB-based verify (SURF-317-A workaround).
- Matrix snapshot/restore via `tests/e2e/global.setup.ts` + `global.teardown.ts` (Phase 315 infra — auto-cleanup compatible).

## Prior Decisions Carried Forward

| Decision | Source | Apply to Phase 318 |
|----------|--------|--------------------|
| Local-only test scope (no Dev/Prod modify) | CLAUDE.md DEV_WORKFLOW | All FLOW P-S |
| DOM-text matching post-shuffle (anti-cheat A4 verdict) | Phase 317 Plan 01 | FLOW P PreTest/PostTest worker answer |
| Direct SignalR hub invoke untuk Essay | Phase 317 Plan 01 fillEssayAnswer | Reuse jika FLOW P pakai Essay |
| Multi-context try/finally defensive close | Phase 317 Plan 02 FLOW O | N/A (no FLOW butuh multi-context) |
| examTypes.ts POM-flat pattern (reuse, jangan refactor) | Phase 317 Plan 01 | Add new helpers as flat exports |
| Wizard tidak auto-create package — wajib createDefaultPackage | Phase 317 Wave 0 W0.1 | All FLOW P-S yang butuh questions |
| Modal manage-btn href = `?assessmentId={id}` query-string | Phase 317 Plan 01 | Reuse regex pattern |
| `/Admin/ManagePackageQuestions?packageId={N}` (NOT ManageQuestions) | Phase 317 Plan 01 | Reuse addQuestionViaForm helper |

## Locked Decisions

### D-318-01: SURF-317 fix scope = BOTH A1 + A included

Phase 318 menggabungkan QA-08 advanced features coverage + Phase 317 SURF carryover fixes:

- **SURF-317-A1** (test fixture): Patch `tests/e2e/exam-taking.spec.ts` FLOW A1 selector `.user-check-item input` → match Phase 304+ form-check Bootstrap markup. Single-file ~10 LOC change. Unblock 74 cascade-skipped tests.
- **SURF-317-A** (production code): Fix `Controllers/CMPController.cs:2190` `packageResponses.ToDictionary(r => r.PackageQuestionId)` → `ToLookup` pattern. Update Razor view `Views/CMP/Results.cshtml` loop. Risk: MA tests existing bisa regression (mitigation: rerun Phase 317 FLOW K + FLOW M post-fix verify still hijau).

**Why this scope:** Phase 317 closure recommendation di regression baseline report. SURF-317-A1 prerequisite untuk per-FLOW pass rate visibility. SURF-317-A unblock UI Results verify (eliminates need DB-based workaround di future MA flows). Risk acceptable — Phase 317 suite (28/28) jadi regression gate.

### D-318-02: PreTest/PostTest pairing test depth = PAIRING + SCORING

FLOW P scope:
- **Pairing:** HC wizard 1 dengan `AssessmentTypeInput = "PrePostTest"` → DB verify 2 AssessmentSessions same user (`AssessmentType = "PreTest"` + `"PostTest"`) linked via `IsPrePostTest = true`.
- **Both complete-able:** Worker take PreTest → submit → score. Worker take PostTest → submit → score.
- **Scoring summary:** HC MonitoringDetail page → verify `statusSummary` format `"PreTest:Completed,PostTest:Completed"` per user. Source: `AssessmentAdminController.cs:2342/3614/5584`.

**Skipped:**
- Razor dual-render Pre vs Post side-by-side di Results page → **tidak exist di code** (grep `Views/CMP/Results.cshtml` returns 0 matches untuk PreTest/PostTest branching). Out of scope.
- PostTest start-gating (worker tidak bisa start PostTest sebelum PreTest Completed) → **tidak explicit di controller**. Phase 313 sudah cover Tier-1 reject manual submit. Out of scope.
- Analytics endpoint paired score delta → Phase 319 admin features territory.

### D-318-03: EWCD test = wizard past date, Cert PDF = UI download + DB NomorSertifikat verify

FLOW Q (EWCD):
- HC wizard set `ewcdDateInput` ke yesterday + `ewcdTimeInput` 23:59 → submit assessment → verify hidden field `ExamWindowCloseDate` < `DateTime.UtcNow.AddHours(7)`.
- Worker attempt `/CMP/StartExam/{id}` atau `/CMP/Assessment` → expect reject via `CMPController.cs:863` enforcement (TempData warning OR redirect).
- DB verify session NOT created OR session.Status stays `NotStarted`.

FLOW R (Cert PDF):
- HC wizard create assessment dengan `generateCertificate: true` + `passPercentage: 70` + 1 MC question.
- Worker submit dengan correct answer (score 100, IsPassed=true).
- HC OR Worker GET `/CMP/CertificatePdf/{sessionId}` → assert response status=200 + `Content-Type: application/pdf` + body bytes > 0.
- DB verify `SELECT NomorSertifikat FROM AssessmentSessions WHERE Id={sessionId}` returns non-null + non-empty.

**Skipped:** PDF text parse (binary extraction via pdf-parse/pdfjs) → brittle terhadap PDF template change + nambah test dep. UI download + DB verify sudah cover user-observable outcome.

**Skipped:** EWCD Tier-1/Tier-2 extension → Phase 313 sudah cover manual-submit-after-time. EWCD = orthogonal feature (window close, not duration overrun).

### D-318-04: FLOW S — AllowAnswerReview true vs false comparison

Single describe block dengan 2 paired sub-tests:
- **S1-S3:** HC create assessment `allowAnswerReview: true` + 1 MC → worker submit → Results page shows `.card "Tinjauan Jawaban"` visible (positive assertion match Phase 317 FLOW N negative pattern inverse).
- **S4-S6:** HC create assessment `allowAnswerReview: false` + 1 MC → worker submit → Results page shows `.alert-info "Tinjauan jawaban tidak tersedia"` visible + NO `.card "Tinjauan Jawaban"`.
- Comparison: same MC structure, only `allowAnswerReview` differs. Verify Razor branch toggle di `Views/CMP/Results.cshtml:316-399`.

**Note:** Phase 317 FLOW N already covers negative case. FLOW S formally pairs positive vs negative untuk explicit comparison documentation.

### D-318-05: Test file organization = extend exam-types.spec.ts

Append FLOW P/Q/R/S ke `tests/e2e/exam-types.spec.ts`. Pattern consistent dengan Phase 317 (smoke W0 + FLOW K-O). Reuse:
- Global.setup matrix snapshot (1 setup per file)
- Sequential mode shared state per describe
- DOM-text marker matching pattern
- DB-based verify untuk affected flows (UNLESS SURF-317-A fix landed early — then revert ke UI assertion)

Target total: 27 (existing) + ~16-20 (new FLOW P/Q/R/S) = ~43-47 sub-tests dalam exam-types.spec.ts.

**SURF-317-A1** = separate file change (`tests/e2e/exam-taking.spec.ts` patch). Standalone fix commit.

**SURF-317-A** = production code change. 2-file change (controller + Razor view). Standalone fix commit dengan Phase 317 regression rerun gate.

### D-318-06: REQUIREMENTS.md QA mapping = add QA-08 baru

Insert QA-08 di `.planning/REQUIREMENTS.md` Future Requirements section:

```
- **QA-08** — Advanced exam features E2E coverage: PreTest/PostTest paired full cycle, ExamWindowCloseDate enforcement (server-side reject post-window), Certificate PDF download (NomorSertifikat generated + downloadable), AllowAnswerReview true vs false comparison di Results page.
```

Preserve QA-02 (Phase 317 coverage) + QA-03 (regression suite deferred — original meaning kept). Update ROADMAP Phase 318 `Requirements: QA-08`. Phase 319 → Requirements: QA-09 (admin features) — add nanti saat Phase 319 discuss.

## Folded SURF Anchors

| Anchor | Origin | Disposition Phase 318 |
|--------|--------|------------------------|
| SURF-317-A | Phase 317 Plan 01 (Results MA aggregation 500) | FIX — controller + Razor (D-318-01) |
| SURF-317-A1 | Phase 317 Plan 02 (legacy FLOW A1 selector cascade) | FIX — test patch (D-318-01) |

## Deferred Ideas

| Idea | Why deferred | Tracking |
|------|--------------|----------|
| PDF text extraction + NomorSertifikat parse from PDF body | Brittle template-dependent + tambah dep | Future phase if compliance audit needs full PDF content verify |
| PostTest start-gating server-side reject test | Code path tidak explicit; Phase 313 partial coverage | Backlog — if behavior surfaces in production |
| Analytics endpoint paired Pre/Post score delta | Phase 319 admin features scope | ROADMAP Phase 319 |
| Cross-session score improvement metric assertion | Beyond user-observable outcome | Out of scope QA-08 |
| Razor dual-render Pre vs Post side-by-side test | Tidak exist di code base | Backlog — only if feature added later |

## Canonical References

| Ref | Path | Purpose |
|-----|------|---------|
| ROADMAP Phase 318 | `.planning/ROADMAP.md:394-399` | Phase boundary + goal statement |
| REQUIREMENTS.md | `.planning/REQUIREMENTS.md:17-25` | QA-08 insertion target |
| EWCD enforcement | `Controllers/CMPController.cs:863` | Server-side reject logic |
| Certificate PDF endpoint | `Controllers/CMPController.cs:1898-1920` | Action signature + auth |
| PrePostTest controller logic | `Controllers/AssessmentAdminController.cs:1177-1256` | Auto-create paired sessions |
| MonitoringDetail statusSummary | `Controllers/AssessmentAdminController.cs:2342, 3614, 5584` | Pre/Post status format |
| SURF-317-A ToDictionary bug | `Controllers/CMPController.cs:2190` | Production fix target |
| Results.cshtml AllowAnswerReview branch | `Views/CMP/Results.cshtml:316-399` | FLOW S positive vs negative |
| Phase 317 examTypes helper | `tests/e2e/helpers/examTypes.ts` | Helper reuse + extension target |
| Phase 317 wizardSelectors | `tests/e2e/helpers/wizardSelectors.ts` | Selector reuse + ewcd selector available |
| exam-types.spec.ts current state | `tests/e2e/exam-types.spec.ts` | Append target FLOW P/Q/R/S |
| exam-taking.spec.ts legacy (SURF-317-A1 target) | `tests/e2e/exam-taking.spec.ts:6, 30-310` | Single-file selector patch |
| Phase 317 Plan 02 SUMMARY | `.planning/phases/317-fix-surf-316-a-ma-essay-mixed-e2e-via-ui/317-02-SUMMARY.md` | Carryover decisions + SURF anchors |
| Phase 317 regression baseline | `docs/test-reports/2026-05-11-flow-a-j-regression.md` | SURF-317-A1 anchor + fix recommendation |
| Phase 313 Tier-1/Tier-2 logic | `.planning/phases/313-block-manual-submit-saat-waktu-habis/313-02-PLAN.md` | EWCD orthogonal — don't duplicate |
| Phase 308 wizard selectors | `Views/Admin/CreateAssessment.cshtml:391` (ewcdDateInput) | EWCD wizard field source |

## Next Steps

Run `/gsd-plan-phase 318` untuk research + planning. Expected output: ~4-5 plans across 2 waves:
- Wave 1 (parallel): Plan 01 (SURF-317-A1 patch + Phase 317 regression rerun gate), Plan 02 (SURF-317-A production fix + Razor view update + Phase 317 MA regression rerun gate)
- Wave 2 (sequential): Plan 03 (FLOW P PreTest/PostTest paired + FLOW Q EWCD reject), Plan 04 (FLOW R Cert PDF + FLOW S AllowAnswerReview comparison), Plan 05 (REQUIREMENTS QA-08 doc + final suite regression gate).

User invoke `/gsd-plan-phase 318` saat ready.
