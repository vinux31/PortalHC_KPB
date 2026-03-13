---
phase: 170-security-review
plan: "02"
subsystem: security
tags: [xss, file-upload, input-validation, sql-injection, open-redirect]
dependency_graph:
  requires: []
  provides: [input-validation-hardening, file-upload-security]
  affects: [Controllers/AdminController.cs, Controllers/CDPController.cs, Controllers/ProtonDataController.cs]
tech_stack:
  added: []
  patterns: [Json.Serialize for JS context encoding, data-attribute XSS mitigation, file extension allowlist]
key_files:
  created: []
  modified:
    - Views/Admin/AssessmentMonitoringDetail.cshtml
    - Views/Admin/ManageWorkers.cshtml
    - Views/Admin/ManagePackages.cshtml
    - Controllers/AdminController.cs
decisions:
  - "Json.Serialize() used instead of Html.Raw(x.Replace()) for all JS string contexts"
  - "PackageName moved to data-attribute in ManagePackages to avoid inline JS injection"
  - "Import endpoints get extension allowlist (.xlsx/.xls only); other upload endpoints were already secured"
metrics:
  duration: 8m
  completed: "2026-03-13"
  tasks: 2
  files: 4
requirements: [SEC-03, SEC-04]
---

# Phase 170 Plan 02: Input Validation & File Upload Security Summary

**One-liner:** Replaced 4 unsafe Html.Raw JS injections with Json.Serialize and added .xlsx/.xls extension allowlists to 2 import endpoints that lacked them.

## Tasks Completed

| # | Task | Commit | Status |
|---|------|--------|--------|
| 1 | Audit input validation — XSS, SQL injection, open redirect | 62bbf60 | Done |
| 2 | Audit and harden file upload endpoints | f0d65cd | Done |

## Findings

### Task 1: Input Validation Audit

**XSS — Html.Raw patterns fixed (4 instances):**

| File | Line | Issue | Fix |
|------|------|-------|-----|
| AssessmentMonitoringDetail.cshtml | 306 | `Html.Raw(session.UserFullName.Replace("'","\'"))` — only escapes single quotes, not `</script>` or other vectors | Replaced with `@Json.Serialize(session.UserFullName)` |
| AssessmentMonitoringDetail.cshtml | 608 | `Html.Raw(Model.Title.Replace("'","\'"))` — same issue with Title field | Replaced with `@Json.Serialize(Model.Title)` |
| ManageWorkers.cshtml | 274 | `Html.Raw(user.FullName.Replace("'","\'"))` — same issue with FullName | Replaced with `@Json.Serialize(user.FullName)` |
| ManagePackages.cshtml | 141 | `Html.Raw(confirmMsg)` where confirmMsg contains `pkg.PackageName` from DB | Moved PackageName to `data-pkg-name` attribute; confirmDeletePackage() JS reads it safely |

**XSS — Safe patterns confirmed (no action needed):**
- `@Html.Raw(statusBadge)` in ImportWorkers.cshtml — statusBadge is a hardcoded switch on enum values, no user data
- All `@Html.Raw(Json.Serialize(...))` and `@Html.Raw(System.Text.Json.JsonSerializer.Serialize(...))` — server-generated JSON
- `@Html.Raw(ViewBag.SavedAnswers ?? "{}")` — server-generated JSON
- `@Html.Raw(GetApprovalBadge(...))` — controlled enum values

**SQL injection:** No `FromSqlRaw`, `ExecuteSqlRaw`, or string-concatenated SQL found anywhere. EF Core LINQ only — clean.

**Open redirect:** `AccountController.Redirect(returnUrl)` at line 123 is guarded by `Url.IsLocalUrl(returnUrl)` check at line 121 — confirmed fixed.

### Task 2: File Upload Security Audit

| Endpoint | Extension Allowlist | Size Limit | Path Safety | Status |
|----------|--------------------|-----------|-----------:|--------|
| AdminController.KkjUpload | .pdf,.xlsx,.xls | 10MB | Timestamp prefix + sanitized original name | Pre-secured |
| AdminController.CpdpUpload | .pdf,.xlsx,.xls | 10MB | Path.GetFileName | Pre-secured |
| AdminController.SubmitInterviewResults (supportingDoc) | .pdf,.doc,.docx,.jpg,.jpeg,.png | 10MB | GUID-based name | Pre-secured |
| AdminController.ImportWorkers | .xlsx,.xls (ADDED) | 10MB (ADDED) | OpenReadStream — no path write | Fixed |
| AdminController.ImportPackageQuestions | .xlsx,.xls (ADDED) | 5MB (pre-existing) | OpenReadStream — no path write | Fixed |
| CDPController.UploadEvidence | .pdf,.jpg,.jpeg,.png | 10MB | Path.GetFileName + timestamp | Pre-secured |
| ProtonDataController.GuidanceUpload | .pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx | 10MB | GUID-based name | Pre-secured |
| ProtonDataController.GuidanceReplace | .pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx | 10MB | GUID-based name | Pre-secured |

**Note on path traversal:** Import endpoints (ImportWorkers, ImportPackageQuestions) read via `OpenReadStream()` and never write to disk, so path traversal is not applicable. Validation added to prevent non-Excel files from being parsed by ClosedXML.

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Commit 62bbf60: FOUND — XSS fixes (AssessmentMonitoringDetail, ManageWorkers, ManagePackages)
- Commit f0d65cd: FOUND — Import endpoint file type validation
- Views/Admin/AssessmentMonitoringDetail.cshtml: FOUND
- Views/Admin/ManageWorkers.cshtml: FOUND
- Views/Admin/ManagePackages.cshtml: FOUND
- Controllers/AdminController.cs: FOUND
