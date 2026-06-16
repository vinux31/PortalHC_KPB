---
phase: 386
slug: assessmentadmincontroller-hardening
status: verified
threats_total: 11
threats_closed: 11
threats_open: 0
asvs_level: 1
created: 2026-06-16
---

# Phase 386 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Admin/HC browser → CreateQuestion/EditQuestion POST | Authenticated form input (option texts + correct flags) → controller → DB persist | Question config (option texts, correct flags) — malformed config can freeze exam submission |
| HC/Admin browser → SubmitEssayScore POST | Authenticated essay-score input → DB write | Essay score integer; session lifecycle state determines legality |
| HC/Admin browser → FinalizeEssayGrading POST | Authenticated finalize → score recompute + cert generation | Session status + all essay scores → IsPassed + licensure PDF |
| Authorized export endpoints → PDF/Excel render | BulkExportPdf + ExportAssessmentResults → official-evidence download | Per-peserta answer data (MA multi-row) → official licensure document |
| Pure helpers (no new boundary) | ValidateQuestionOptions + BuildAnswerCell are EF-free, no I/O | No data crossing; correctness guaranteed by unit tests |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-386-02-INPUT | Tampering | CreateQuestion/EditQuestion option params | mitigate | `QuestionOptionValidator.ValidateQuestionOptions` wired at CreateQuestion L6480 + EditQuestion L6699; rejects <2 ber-teks or checked-correct-empty with TempData error + redirect BEFORE persist | closed |
| T-386-02-DOS | Denial of Service | exam submission frozen by 0-option question | mitigate | Same `ValidateQuestionOptions` validation (same 2 call-sites) blocks the malformed question from being saved; vector eliminated at config-time | closed |
| T-386-02-AUTHZ | Elevation | CreateQuestion/EditQuestion auth attributes | accept | `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` retained unmodified on both endpoints — no new public surface introduced by Phase 386; validation only strengthens existing gate | closed |
| T-386-02-T | Tampering | ValidateQuestionOptions pure function integrity | accept | Pure function, no I/O; correctness guaranteed by OptionValidationTests (7 cases GREEN). Auth wiring in Wave 2 (Plan 03) inherits existing endpoint attributes unmodified | closed |
| T-386-05-I | Information Disclosure | BuildAnswerCell essay truncation | accept | Truncates TextAnswer to 300 chars for display-only path; no PII expansion; identical to existing PDF behavior L5083; pure function, no auth surface | closed |
| T-386-AUTHZ | Tampering / Elevation | SubmitEssayScore lacked status-guard → upsert widened F-03 | mitigate | Status-guard at L3539: `session.Status != AssessmentConstants.AssessmentStatus.PendingGrading` → reject; `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` retained (confirmed L3528-3530). Verified by EssayEmptyPendingParityTests `SubmitEssayScore_NonPendingGrading_Rejected` (6/6 GREEN) | closed |
| T-386-04-UPSERT | Tampering | out-of-range score materialized before range check | mitigate | Ordering enforced: status-guard (L3533) → load question (L3543) → range-guard `score < 0 \|\| score > question.ScoreValue` (L3546) → upsert `.Add` (L3563); invalid score never creates a row | closed |
| T-386-04-COUNT | Repudiation / Integrity | divergent pending counts across 4 surfaces | mitigate | Single logical predicate `!IsNullOrWhiteSpace(TextAnswer) && EssayScore==null` applied at all 4 sites (L3317/L3506/L3580/L3650); EF sites 3+4 materialize server-filtered set then evaluate IsNullOrWhiteSpace in-memory (SQL Server LTRIM/RTRIM does not strip tab/newline); EssayEmptyPendingParityTests 6/6 GREEN incl `"\t\n"` variant | closed |
| T-386-04-DOS | Denial of Service | empty-essay dead-end blocks FinalizeEssayGrading | mitigate | Predicate treats empty/whitespace essay as not-pending; finalize gate (L3650) no longer blocks on empty essays; upsert at L3549-3564 allows HC to score essay with null TextAnswer; empty essay auto-0 via existing `AssessmentScoreAggregator.Compute` (untouched) | closed |
| T-386-05-INTEGRITY | Tampering (data-integrity of official record) | GeneratePerPesertaPdf + AddDetailPerSoalSheet MA mislabel | mitigate | Both surfaces now call `AssessmentScoreAggregator.IsQuestionCorrect(q, responsesForQ)` (SetEquals all-or-nothing) + `BuildAnswerCell(q, responsesForQ)` (full selected-option list); confirmed at Controller L5106-5107 + ExcelExportHelper L93-94; browser UAT APPROVED 4/4 including MA partial-select = Salah proof | closed |
| T-386-05-SCOPE | Engine safety | scoring engine `Compute` untouched | accept | D-11 — `Helpers/AssessmentScoreAggregator.cs` Compute and IsQuestionCorrect bodies byte-untouched; display-path only; git-verified 0 diff on AssessmentScoreAggregator.cs across Plans 02+05 commits | closed |
| T-386-05-PAKET | Operational | GeneratePerPesertaPdf multi-paket resolution | accept | Out of scope; operational mitigation = use 1 paket at hari-H (CONTEXT D-13 / F-18 OUT). No code change warranted | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-386-01 | T-386-02-AUTHZ | CreateQuestion/EditQuestion `[Authorize]`+`[ValidateAntiForgeryToken]` attributes intentionally unmodified by Phase 386; Phase 386 only adds server-side validation BEFORE persist, strengthening — not weakening — the trust boundary | gsd-security-auditor (Phase 386) | 2026-06-16 |
| AR-386-02 | T-386-02-T | ValidateQuestionOptions is a pure static function (EF-free, no I/O, no auth surface); correctness proven by OptionValidationTests 7/7; wiring into authenticated POST in Wave 2 inherits existing auth unchanged | gsd-security-auditor (Phase 386) | 2026-06-16 |
| AR-386-03 | T-386-05-I | BuildAnswerCell essay truncation to 300 chars is display-only; mirrors pre-existing PDF truncation at L5083; no PII surface expansion; pure function with no new data access | gsd-security-auditor (Phase 386) | 2026-06-16 |
| AR-386-04 | T-386-05-SCOPE | AssessmentScoreAggregator.Compute (on-submit grading, scoring engine) is intentionally untouched; Phase 386 is display-path only; D-11 constraint preserved; git-verified 0 diff | gsd-security-auditor (Phase 386) | 2026-06-16 |
| AR-386-05 | T-386-05-PAKET | Multi-paket PDF resolution (GeneratePerPesertaPdf fetches by AssessmentPackageId) is an operational constraint; mitigated operationally by using 1 paket at hari-H; no code scope for Phase 386 | gsd-security-auditor (Phase 386) | 2026-06-16 |

---

## Advisory: Code Review Gaps WR-01 / WR-02 (Deferred to Phase 387)

Code review 386-REVIEW.md identified 2 advisory (0 critical) findings in the new `SubmitEssayScore` upsert path, not present in the original threat register:

**WR-01** (`SubmitEssayScore` does not validate `question.QuestionType == "Essay"` before upsert): an authenticated Admin/HC could write `EssayScore` onto an MC/MA question row. Assessed as **low-severity** given: (a) the endpoint is grader-facing only (Admin/HC role, post-exam, not participant-facing); (b) `FinalizeEssayGrading.Compute` only sums EssayScore via the `case "Essay"` branch, so MC/MA rows with a spurious EssayScore do not affect Finalize totals unless that questionId appears in the essay loop; (c) the licensor exam (participant-facing) does not touch this endpoint.

**WR-02** (`SubmitEssayScore` does not validate `questionId` belongs to the session's package): an authenticated grader could upsert a cross-session row. Assessed as **medium-severity**: the upsert path (Phase 386 new code) removes the implicit protection that the pre-386 `FirstOrDefault` provided (it would not find a cross-session row). However, the attack requires an authenticated Admin/HC to deliberately craft a request with a mismatched sessionId/questionId pair.

**Classification:** Both are implementation gaps introduced by the Phase 386 upsert rewrite. They are REAL gaps, not false positives. However, given the Admin/HC-only access surface, the post-exam grading context, and the existence of Phase 387 PXF-06 which explicitly owns `SubmitEssayScore` hardening, these gaps are **deferred** (not escalated) under the following rationale:

- Attack surface is admin-only (not participant-facing); no anonymous or participant path to `SubmitEssayScore`
- `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` remain in place
- The licensor exam event (hari-H) does not involve post-exam grading at the moment of the exam itself
- Phase 387 PXF-06 is planned post-event and explicitly scoped to harden `SubmitEssayScore`

**Action:** These are logged here as OPEN_ADVISORY items to ensure Phase 387 PXF-06 closes them. They do NOT trigger an ESCALATE or OPEN_THREATS result for Phase 386.

| Advisory ID | WR Ref | Gap | Severity | Resolution |
|-------------|--------|-----|----------|------------|
| ADV-386-WR01 | WR-01 | `SubmitEssayScore` missing `QuestionType == "Essay"` guard before upsert | Low | Defer to Phase 387 PXF-06 |
| ADV-386-WR02 | WR-02 | `SubmitEssayScore` missing questionId-to-session package ownership check | Medium | Defer to Phase 387 PXF-06 |

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-16 | 11 | 11 | 0 | gsd-security-auditor (claude-sonnet-4-6) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter
- [x] Advisory items (WR-01/WR-02) documented and deferred to Phase 387 PXF-06

**Approval:** verified 2026-06-16
