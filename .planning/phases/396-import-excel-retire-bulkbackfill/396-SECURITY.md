---
phase: 396
slug: import-excel-retire-bulkbackfill
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-18
---

# Phase 396 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Excel batch-import path for inject-assessment (template generator + matrix parser + 2 endpoints + Step-5 UI) and hard-removal of the legacy BulkBackfill tool.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Excel file bytes → ParseMatrix | Untrusted .xlsx content (cells, NIPs) crosses into the static helper — first validation gate | Cell strings, NIPs, option letters, essay scores |
| Browser (HC) → POST UploadInjectExcel | Untrusted IFormFile (.xlsx) + UserIds + QuestionsJson cross into the controller | File upload (≤10MB), form fields, antiforgery token |
| Browser (HC) → POST DownloadInjectTemplate | UserIds + QuestionsJson cross in; output is a generated file (no DB write) | Form fields, antiforgery token |
| Server JSON → DOM render | Preview rows / error messages (containing NIP/Name) rendered into the page | NIP, worker name, score, error text |
| Browser → /Admin/BulkBackfill (post-removal) | The removed route is now an unmapped path | (none — endpoint deleted → 404) |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-396-01 | Tampering | NIP outside picker (privilege-via-data, D-02) | mitigate | `ParseMatrix` gates `allowedNips.Contains(nip)` → per-row `InjectRowError` (`InjectExcelHelper.cs:178-182`); controller builds `allowedNips` from `vm.UserIds` only, never adds NIPs from file (`InjectAssessmentController.cs:243`) | closed |
| T-396-02 | DoS / Input Validation | Malformed / huge .xlsx | mitigate | `try/catch` around `new XLWorkbook` → friendly BI error (`InjectExcelHelper.cs:154-163`); `[RequestFormLimits 10MB]` + `.xlsx/.xls` whitelist (`InjectAssessmentController.cs:203,216-222`); in-memory stream (not saved); outer endpoint try/catch (no 500) | closed |
| T-396-03 | Tampering | Blank cell must OMIT, not reject-all batch (D-06) | mitigate | Blank MC/MA → `skippedBlank++; continue` (`InjectExcelHelper.cs:237-240`); blank essay score → same (`:198-209`); never pushes empty-selection spec; unit test #3 locks it | closed |
| T-396-04 | Tampering | Essay text-optional (D-05) scoped to Excel only | mitigate | `req.EssayTextRequired=false` only when `vm.Step5Method=="excel"` (`InjectAssessmentController.cs:64-67`); service guard `req.EssayTextRequired && …` (`InjectAssessmentService.cs:397`); default `true` keeps form path 395 byte-identical (`InjectAssessmentDtos.cs:60`) | closed |
| T-396-05 | Elevation / Access Control | New endpoints (V4) | mitigate | Both `DownloadInjectTemplate` + `UploadInjectExcel` carry `[Authorize(Roles="Admin, HC")]` (`InjectAssessmentController.cs:167,200-202`); non-Admin/HC → 403 | closed |
| T-396-06 | Tampering / CSRF | CSRF on upload + template POST (V5) | mitigate | Both POST `[ValidateAntiForgeryToken]` (`:168,202`); client sends `__RequestVerificationToken` (`InjectAssessment.cshtml:1836`) | closed |
| T-396-07 | Tampering / Info-Disclosure | Cert# reserved/leaked during preview | accept (by-design) | `BuildExcelPreviews` calls `AssessmentScoreAggregator.Compute` only — no `CertNumberHelper`, no `SaveChanges` (`InjectAssessmentController.cs:282-312`); `InjectExcelUploadResult` has no cert field; preview table renders no cert#. Cert reserved only at commit (`InjectBatchAsync`) — matches 395 D-09 | closed |
| T-396-08 | Repudiation | Audit of Excel inject | mitigate | Commit reuses `InjectBatchAsync` (`:82`) which writes `AuditLog "ManualInject"` per session (inherited Phase 393); no separate Excel commit branch | closed |
| T-396-09 | XSS / Info-Disclosure | Rendering NIP/Name/error from server JSON into DOM | mitigate | All dynamic Excel-path text via `.textContent`/`createElement` (error list `:1774`, NIP/Nama `:1803`, score `:1806`, badge `:1810`, terjawab `:1813`, blank warn `:1787`); no `innerHTML` with server/user data | closed |
| T-396-10 | Tampering | Excel cache bypassing the preview gate (silent-grade-0) | mitigate | Submit listener fills `#AnswersJson` from `injExcelAnswersCache` (`:1086-1091`); cache set `'[]'` on invalid upload (`:1767`), filled from `result.answersJson` only after 0-error upload (`:1821`) — preview is the gate (D-08) | closed |
| T-396-11 | DoS / Availability | Wrong line-range removal breaks CleanupAttemptHistory / training-import | mitigate | Non-contiguous removal KEEPS `CleanupAttemptHistory` (`TrainingAdminController.cs:780-812`) + `using ClosedXML.Excel` + `ManualDuplicatePredicate`; build 0-error + DuplicateGuardTests 9/9 | closed |
| T-396-12 | Integrity | Dead link / orphan divider after BulkBackfill removal | mitigate | Both UI entry-points removed (Index.cshtml Section D card + `_AssessmentGroupsTab` dropdown-item + orphan divider); 0 residual "BulkBackfill"/"Bulk Import" refs; `BulkBackfill.cshtml` deleted; routes 404 at runtime | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

> Note: T-396-03 also covered the 396-05 "legacy BulkBackfill kept alive as a 2nd commit-without-preview entry-point" (Elevation) — closed by hard-removal (routes 404, runtime-confirmed).

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-396-01 | T-396-07 | Preview is intentionally cert-free (Aggregator.Compute, no SaveChanges); cert number reserved only at commit. By-design, verified no cert# leaks in preview path. | Rino (developer) | 2026-06-18 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-18 | 12 | 12 | 0 | gsd-security-auditor (sonnet, ASVS L1) |

> Cross-references: code review 396-REVIEW.md (0 Critical / 3 Warning / 5 Info) — WR-01 (dup-NIP ToDictionary throw), WR-02 (essay decimal int-truncation), WR-03 (download hidden-form race) are correctness/robustness issues, NOT ASVS L1 security gaps; deferred to backlog/Phase 398. Build 0-error, fast suite 389/389, DuplicateGuardTests 9/9, browser UAT 5/5.

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-18
