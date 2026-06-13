---
phase: 372
slug: data-foundation-propagasi-toggle
audit_date: 2026-06-13
asvs_level: 1
threats_total: 9
threats_closed: 9
threats_open: 0
block_on: high
status: secured
---

# Phase 372 — Security Threat Verification

## Phase 372 — Data Foundation Propagasi Toggle (v27.0 Shuffle)

**Audit Date:** 2026-06-13
**ASVS Level:** 1
**Threats Closed:** 9/9
**Threats Open:** 0
**block_on:** high

---

### Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-372-01 | Tampering — Migration DDL AddColumn defaultValue:true | accept | CLOSED | `Migrations/20260613095102_AddShuffleTogglesToAssessmentSession.cs` lines 13-25: `AddColumn<bool>` with `type: "bit", nullable: false, defaultValue: true` ×2; no user input path. |
| T-372-02 | Information Disclosure — 2 bool cols AssessmentSessions | accept | CLOSED | `Models/AssessmentSession.cs` lines 39,42: plain `bool` fields (ShuffleQuestions/ShuffleOptions); no PII, no sensitive data exposure change. |
| T-372-03 | Denial of Service — Migration apply on table | accept | CLOSED | `ADD COLUMN bit NOT NULL DEFAULT` (SQL Server metadata-only DDL for constant default). Local-only apply; Dev/Prod = IT task per DEV_WORKFLOW. |
| T-372-04 | Tampering — over-posting/mass-assignment on POST bind | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs` lines 1228-1229, 1264-1265, 1443-1444, 1809-1810, 1913-1914, 1934-1935, 2040-2041, 2137-2138: only 2 new bools added to form-bound model; 8 explicit set-sites (5 model.* + 1 savedAssessment.* + 2 foreach). Non-form fields set server-side. Auth + antiforgery guard both POSTs. No `[Bind]` exclusion regression detected. |
| T-372-05 | Elevation of Privilege — non-Admin/HC flip flag | accept (mitigated by existing) | CLOSED | `Controllers/AssessmentAdminController.cs` line 844: `[Authorize(Roles = "Admin, HC")]` on CreateAssessment POST; line 1770: same on EditAssessment POST. Phase 372 does not relax this. |
| T-372-06 | Tampering — cross-group propagation foreach | accept | CLOSED | `Controllers/AssessmentAdminController.cs` lines 1809-1810 (F1 Pre-Post `s.*`) and 2040-2041 (F2 standard `sibling.*`): foreach scope anchored on existing group-key query (Title/Category/Schedule.Date). Pattern identical to AllowAnswerReview propagation. No new query broadening scope. |
| T-372-07 | Tampering — client JS populateSummary read-only mirror | accept | CLOSED | `Views/Admin/CreateAssessment.cshtml` lines 1150-1151, 1199-1200: JS reads `.checked` and writes to summary `span` elements only; no server roundtrip, no authoritative state. |
| T-372-08 | Information Disclosure — help-text/summary copy | accept | CLOSED | `Views/Admin/CreateAssessment.cshtml` lines 529,534: static Indonesian educational text; no data/PII exposure. |
| T-372-09 | Spoofing/CSRF — submit form with toggle | accept (mitigated) | CLOSED | `Controllers/AssessmentAdminController.cs` lines 844-845 and 1770-1771: both POST actions carry `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]`. View adds no new endpoint; toggles submit through existing guarded form. |

---

### Threat Flags from SUMMARY.md

No threat flags declared in 372-01-SUMMARY.md, 372-02-SUMMARY.md, or 372-03-SUMMARY.md `## Threat Flags` section.

### Unregistered Flags

None.

---

### Grading Safety (Cross-Cutting)

Phase 372 does not touch any grading service (`Services/GradingService.cs`, `Services/ExamService.cs`). Git log for commits `be1a7178..47ba09d4` shows zero changes to grading files. The migration only alters `AssessmentSessions` table (metadata-only `AddColumn`). Spec §13 grading safety (PackageOption.Id-based, position-independent) is unaffected.

---

### Accepted Risks Log

| Threat ID | Accepted Risk | Rationale |
|-----------|--------------|-----------|
| T-372-01 | Migration DDL is static, no user input | ADD COLUMN bit DEFAULT 1 — SQL Server metadata-only; no attack surface |
| T-372-02 | Bool columns not sensitive | ShuffleQuestions/ShuffleOptions are configuration bools, not PII |
| T-372-03 | DoS risk accepted | Local-only apply; Dev/Prod = IT handoff task per DEV_WORKFLOW |
| T-372-05 | EoP mitigated by existing [Authorize] | No new role/privilege change introduced |
| T-372-06 | Cross-group scope mitigated by existing group key | Foreach anchored on same group-key as AllowAnswerReview (pre-existing) |
| T-372-07 | Client JS is read-only display | No server state modified by JS |
| T-372-08 | Help-text is static copy | No data leak |
| T-372-09 | CSRF mitigated by existing [ValidateAntiForgeryToken] | Both POST actions already protected |

---

### Audit Trail

| Date | Event | Result |
|------|-------|--------|
| 2026-06-13 | Initial threat verification (gsd-security-auditor, ASVS L1) | SECURED — 9/9 closed, 0 open |
