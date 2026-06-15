---
phase: 368-delete-records-hygiene-lanjutan-edit-atomic-file-reset-et-sc
type: security-verification
asvs_level: 1
block_on: high
threats_total: 13
threats_closed: 13
threats_open: 0
unregistered_flags: 0
verified: 2026-06-13
---

# Phase 368 — Security Verification (State B)

Verifies that every threat declared in the `<threat_model>` blocks of PLAN 01–04 has its
declared mitigation present in the implemented code. No new-threat scan. Implementation files
read-only (zero modified).

**Result: SECURED — 13/13 threats closed, 0 open, 0 unregistered flags. ASVS L1.**

## Threat Verification (mitigate)

| Threat ID | Category | Component | Evidence (file:line) |
|-----------|----------|-----------|----------------------|
| T-368-01 | Tampering / Info Disclosure | #26 EditTraining `Renews*Id` IDOR | `Controllers/TrainingAdminController.cs:517-528` — same-user check `srcTr.UserId != record.UserId` (L520) + `srcAsRenew.UserId != record.UserId` (L526), guarded only-when-changed (`model.Renews*Id != record.Renews*Id && .HasValue`, L517/L523); invalid → `ModelState.AddModelError` → TempData firstError + redirect (L529-533) |
| T-368-02 | Tampering | #21 File.Delete path traversal | `Helpers/FileUploadHelper.cs` — `SaveFileAsync` strips directory via `Path.GetFileName` (L90) + audit-logs traversal attempt (L93-98) + magic-byte via `ValidateCertificateFile` (L13-39); `DeleteFile` null-safe (L116) + `File.Exists` guard (L119) + confined `Path.Combine(webRootPath, relativeUrl.TrimStart('/'))` (L118). Old path = server-stored `record.SertifikatUrl` (not direct user input) |
| T-368-03 | Denial of Service | #21 upload fail → data loss | `Controllers/TrainingAdminController.cs` — capture-before (L540/L1050); upload fail nulls oldUrl (L543/L1053); delete strictly conditional `!string.IsNullOrEmpty(oldSertifikatUrl) && oldSertifikatUrl != record.SertifikatUrl` POST-commit (L565/L1074); zero PRE-save `File.Delete(oldPath)` remaining (grep = 0) |
| T-368-04 | Info Disclosure | #26/#21 ex.Message leak | `Controllers/TrainingAdminController.cs` — renewal error messages generic, no `ex.Message` (L521/L527/L531); File.Delete catch → `_logger.LogWarning` server-side only (L568/L1077) |
| T-368-05 | Tampering / CSRF | #23 CleanupAttemptHistoryExecute POST | `Controllers/TrainingAdminController.cs:809-812` — `[HttpPost][ValidateAntiForgeryToken][Authorize(Roles="Admin")]`; view `Views/Admin/CleanupAttemptHistory.cshtml:47` `@Html.AntiForgeryToken()` + confirm dialog (L46) |
| T-368-06 | Elevation of Privilege | #23 endpoint non-admin | `Controllers/TrainingAdminController.cs` — `[Authorize(Roles="Admin")]` on GET (L798) and POST (L811) |
| T-368-07 | DoS (mass over-delete) | #23 orphan query too broad | `Controllers/TrainingAdminController.cs` — narrow predicate `!_context.AssessmentSessions.Any(s => s.Id == h.SessionId)` identical in GET preview (L802) and POST execute (L815); preview-count before execute (L801-804); idempotent (re-run query auto-empty); non-orphan (valid SessionId) never enters query |
| T-368-08 | Repudiation | #23/#24 no audit trail | `Controllers/TrainingAdminController.cs` — `_auditLog.LogAsync(..., "CleanupAttemptHistory", ...)` (L824-825) + `_auditLog.LogAsync(..., "ImportTraining", ...)` (L1496-1497) |
| T-368-10 | Tampering | #22 RemoveRange ET cross-session | `Controllers/AssessmentAdminController.cs:3975-3979` — strict filter `.Where(e => e.AssessmentSessionId == id)` where `id` = route param of `ResetAssessment(int id)` (L3889); no cross-session delete; no new `BeginTransactionAsync` added in method |
| T-368-12 | Denial of Service | #25 ToDictionary(c=>c.Name) → 500 | `Models/CertificationManagementViewModel.cs:72-80` — `BuildParentNameLookup` uses `GroupBy(c => c.Name)` (L78), no throw on duplicate child Name; replaces throwing `ToDictionary(c=>c.Name)` |

## Threat Verification (accept) — rationale confirmed, no contradicting evidence

| Threat ID | Category | Component | Acceptance evidence |
|-----------|----------|-----------|---------------------|
| T-368-09 | Info Disclosure | #23 preview-count leaks internal count | Admin-gated (`[Authorize(Roles="Admin")]` on GET, `Controllers/TrainingAdminController.cs:798`); count is maintenance info (orphan rows), non-sensitive; explicit contract D-01. Rationale holds |
| T-368-11 | (no new surface) | #22 cleanup on already-authorized ResetAssessment | Endpoint + authz unchanged: `[HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]` (L3886-3888); ET RemoveRange added to existing SaveChanges batch only, no new endpoint/transaction; ET = derived data of same session. Rationale holds |
| T-368-13 | (no new surface) | #25 helper = internal data transform | Pure refactor: 1-line callsite swap (anon-type → tuple → shared helper) at `CMPController.cs:4158` + `CDPController.cs:4007`; `CertificationManagement` actions + controller authz untouched; no user input flows to helper. Rationale holds |

## Anti-drift / negative checks

- `System.IO.File.Delete(oldPath)` PRE-save in TrainingAdminController — **0 matches** (NON-atomic bug removed).
- `ToDictionary(c => c.Name` in CMPController.cs / CDPController.cs — **0 matches** (throwing callsite removed).
- inline `GroupBy(c => c.Name)` in CMPController.cs / CDPController.cs — **0 matches** (no duplicate inline; single-source helper only).
- `BeginTransactionAsync` added inside ResetAssessment — **none** (existing SaveChanges + ExecuteUpdateAsync batch preserved; matches elsewhere are unrelated methods).
- Orphan predicate `!AssessmentSessions.Any(s => s.Id == h.SessionId)` — single-source, identical in GET + POST (no divergence between preview and execute).

## Unregistered Flags

None. No `## Threat Flags` section present in 368-01..04-SUMMARY.md; executors flagged no new attack surface.
UAT note #2 (368-04-SUMMARY) — pre-existing `/CMP/CertificationManagement` 500 (view-not-found) — is NOT a 368
regression (`git diff` shows 0 `.cshtml` touched; #25 change = 1 LINQ line) and is unrelated to any registered
threat; logged there as a backlog candidate, not a security gap.

## ASVS L1 alignment

- V4 Access Control: `[Authorize(Roles=...)]` on all new/modified destructive endpoints (T-06, T-11).
- V4.2.2 / CSRF: `[ValidateAntiForgeryToken]` + view `@Html.AntiForgeryToken()` on POST (T-05).
- V5.1 Input Validation: same-user IDOR guard (T-01); magic-byte + extension + size validation (T-02).
- V7.4 Error Handling: generic user messages, exception detail server-side log only (T-04).
- V12 File Handling: directory-stripping + webroot-confined save/delete (T-02).

## Conclusion

All 10 `mitigate` threats have their declared mitigations present in implemented code; all 3 `accept`
threats have their acceptance rationale confirmed with no contradicting evidence. No mitigate-disposition
threat lacks its mitigation. `threats_open: 0`. Under `block_on: high`, nothing blocks. **SECURED.**
