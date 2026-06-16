---
phase: 387
slug: post-lisensor-assessment-polish
status: verified
threats_total: 10
threats_closed: 10
threats_open: 0
asvs_level: 1
created: 2026-06-16
---

# Phase 387 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| HC/Admin browser → SubmitEssayScore / FinalizeEssayGrading POST | Authenticated HC POST; both `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` | Essay score + session lifecycle state; cert (official evidence) generated on finalize |
| Controller → SignalR monitor group | Server-originated fire-and-forget broadcast | No untrusted input crosses; workerName already broadcast by sibling events |
| Participant browser → SubmitExam (POST) | Authenticated exam submission; form may be partial (JS failure) — untrusted shape | MC/MA/Essay answers; partial form must not destroy autosaved answers |
| Participant browser → AssessmentHub.SaveTextAnswer (SignalR) | Authenticated via Context.UserIdentifier; essay autosave write path | Essay text; timer state determines write legality |
| Server (Razor render) → participant browser | Render-time a11y markup only | No untrusted input, no state mutation |
| Test harness → disposable SQL DB | Tests create/drop `HcPortalDB_Test_{guid}`; never touch `HcPortalDB_Dev` | Test fixtures only; Dev DB isolation enforced |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Status | Evidence |
|-----------|----------|-----------|-------------|--------|----------|
| T-387-01 | Tampering | SubmitEssayScore editing a finalized session | mitigate | CLOSED | `AssessmentAdminController.cs:3539` `session.Status != PendingGrading` blocks post-finalize edit; WR-01 type-guard L3555 (`QuestionType != "Essay"`); WR-02 ownership-guard L3559-3562 (`AnyAsync` cross-session check) |
| T-387-02 | Elevation | Guard freezes legit grading | mitigate | CLOSED | Guard predicate L3539 is `!= PendingGrading`, NOT `== Completed`; PendingGrading passes through; non-blocked statuses get distinct message L3641-3648. Not over-broad |
| T-387-03 | Repudiation | Cert-number collision silently swallowed | mitigate | CLOSED | `AssessmentAdminController.cs:3747-3776` `maxCertAttempts=3` retry loop, `DbUpdateException` catch on collision; `_logger.LogError` L3774 on persistent failure; `certError` in JSON L3834-3842. No silent swallow |
| T-387-04 | Info Disclosure | Monitor broadcast leaks worker name | accept | CLOSED | `monitor-{batchKey}` join requires authenticated session; workerName already broadcast by sibling CMPController events. No new exposure |
| T-387-05 | Tampering / DoS | SubmitExam nulling a saved MC answer when question absent | mitigate | CLOSED | `CMPController.cs:1714` `if (answers.ContainsKey(q.Id))` guards MC upsert; absent question skips assignment; `PackageOptionId` never null-overwritten |
| T-387-06 | Tampering | Participant writing essay after timer expired | mitigate | CLOSED | `Hubs/AssessmentHub.cs:151-160` `elapsed > allowed`, `allowed = (DurationMinutes + (ExtraTimeMinutes ?? 0)) * 60`; mirrors SaveMultipleAnswer L217-227; rejects + returns |
| T-387-07 | Repudiation | Post-expiry write leaves no trace | mitigate | CLOSED | `Hubs/AssessmentHub.cs:158` `_logger.LogWarning("SaveTextAnswer: timer expired for session {SessionId}", sessionId)` |
| T-387-08 | (a11y display) | Results/ExamSummary option-image aria-label | accept | CLOSED | Pure render-time string from option-letter index; no data/auth/state path. No security impact |
| T-387-09 | (test-integrity) | Integration tests mutating local Dev DB | mitigate | CLOSED | `HcPortal.Tests/PostLisensorPolishTests.cs:27` `DbName = $"HcPortalDB_Test_{Guid.NewGuid():N}"`; `[Trait("Category","Integration")]` L60; `HcPortalDB_Dev` absent; `EnsureDeletedAsync` on dispose L55-57 |
| T-387-10 | (verification-gap) | LOW items PXF-08/10/13 shipping unverified | mitigate | CLOSED | `387-04-SUMMARY.md` L75-83 — manual browser+SignalR+DB checkpoint for PXF-08/10/13, all PASS, APPROVED |

**Totals:** 10 threats · 10 CLOSED · 0 OPEN

---

## Accepted Risks Log

| Threat ID | Category | Rationale | Accepted By |
|-----------|----------|-----------|-------------|
| T-387-04 | Info Disclosure | workerName already in prior broadcast surface; monitor group requires authenticated join; no new data exposed | Phase 387 threat register |
| T-387-08 | a11y display | Aria-label string is display-only; touches no auth/state/data path | Phase 387 threat register |

---

## Files Verified

- `Controllers/AssessmentAdminController.cs` — SubmitEssayScore (L3531-3598), FinalizeEssayGrading (L3610-3844)
- `Controllers/CMPController.cs` — SubmitExam MC upsert (L1712-1718)
- `Hubs/AssessmentHub.cs` — SaveTextAnswer (L134-193), SaveMultipleAnswer (L200-227)
- `HcPortal.Tests/PostLisensorPolishTests.cs` — fixture + tests (L1-387)
- `387-04-SUMMARY.md` — human-verify checkpoint

---

## Audit Trail

## Security Audit 2026-06-16
| Metric | Count |
|--------|-------|
| Threats found | 10 |
| Closed | 10 |
| Open | 0 |

State B (create from artifacts). Auditor verdict: **## SECURED 10/10**, threats_open: 0. ASVS L1, block_on: high. Verified by gsd-security-auditor (autonomous v31.0 auto-close). No implementation files modified.
