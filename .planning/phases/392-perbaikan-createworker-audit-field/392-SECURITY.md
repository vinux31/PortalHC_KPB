---
phase: 392-perbaikan-createworker-audit-field
auditor: gsd-security-auditor
asvs_level: 1
block_on: high
date: 2026-06-17
threats_total: 8
threats_closed: 8
threats_open: 0
---

# SECURITY.md — Phase 392 (Perbaikan CreateWorker + Audit Field)

## Result: SECURED — 8/8 threats closed, 0 open

---

## Threat Verification

| Threat ID | Category | Disposition | Evidence |
|-----------|----------|-------------|----------|
| T-392-01 | Tampering | accept | `Views/Admin/CreateWorker.cshtml` L62-63 and L72-73: no `readonly` attribute on FullName or Email inputs (unconditionally removed). TEST A in `tests/e2e/createworker-392.spec.ts` L41 asserts absence of `readonly=`. FROZEN controller L212 `CreateWorker(ManageUserViewModel model)` with ModelState validation unchanged (0-diff confirmed by 392-02-SUMMARY D-08). |
| T-392-02 | Tampering | accept | `[ValidateAntiForgeryToken]` + ModelState at `Controllers/WorkerController.cs` L210-212 confirmed present and 0-diff. Client-side validation is UX-only addendum; server gate unaltered. |
| T-392-03 | Spoofing/Elevation | mitigate | All three controls present: `@Html.AntiForgeryToken()` at `Views/Admin/CreateWorker.cshtml` L49; `[Authorize(Roles = "Admin, HC")]` at `Controllers/WorkerController.cs` L210; `[ValidateAntiForgeryToken]` at `Controllers/WorkerController.cs` L211. All FROZEN/unchanged this phase. |
| T-392-04 | Info Disclosure | accept | `@Html.Raw(ViewBag.SectionUnitsJson ?? "{}")` at `Views/Admin/CreateWorker.cshtml` L203 inside `@section Scripts` — same server-serialized OrganizationUnits dict as before; relocation does not expand attack surface. No user-controlled data in the serialization path. |
| T-392-05 | Info Disclosure | mitigate | `tests/e2e/createworker-392.spec.ts` L11-12: imports `{ accounts }` from fixture; L25 `const { email, password } = accounts.admin` (dev-local `admin@pertamina.com`). No hardcoded production credentials anywhere in spec. |
| T-392-06 | Tampering (hygiene) | mitigate | Unique-per-run email at spec L19 (`e2e-cw-${TS}@local.test`); teardown in `test.afterAll` L111-137 calls `page.request.post('/Admin/DeleteWorker', ...)` with Identity cascade; no `DELETE FROM` raw-SQL in spec. SEED_JOURNAL entry confirmed added and marked CLEANED (392-02-SUMMARY). |
| T-392-07 | Tampering | accept/mitigate | Teardown at spec L121 reads real `__RequestVerificationToken` from authenticated ManageWorkers page; L124-126 posts it to `/Admin/DeleteWorker` which has `[ValidateAntiForgeryToken]` at `Controllers/WorkerController.cs` L486. Anti-forgery not bypassed. |
| T-392-08 | Elevation | mitigate | `tests/helpers/dbSnapshot.ts` L39-43: `runSqlcmd()` checks `-S` arg and rejects any target not matching `/^localhost/i` with `Refusing to target non-localhost SQL Server: <host>`. Guard present and active. |

---

## Accepted Risks Log

| Threat ID | Rationale |
|-----------|-----------|
| T-392-01 | `readonly` is a client-side display attribute only; server [Required]/[EmailAddress] in the FROZEN model remain the authoritative gate. Removing it closes a usability bug without creating a security gap. |
| T-392-02 | Client-side jquery-validation is UX defense-in-depth. FROZEN server ModelState at WorkerController.cs L212 is the authoritative validation gate and cannot be bypassed by disabling client-side validation. |
| T-392-04 | `@Html.Raw(ViewBag.SectionUnitsJson)` emits server-owned OrganizationUnits data (JsonSerializer.Serialize of an app-controlled dictionary). No user input flows into this serialization. Relocating into `@section Scripts` changes load order only, not the data or its source. |

---

## Unregistered Flags

None. Neither 392-01-SUMMARY.md nor 392-02-SUMMARY.md contains a `## Threat Flags` section. No unregistered attack surface was flagged by the executor during implementation.

---

## Scope Notes

- This phase is **VIEW-ONLY** (Plan 01) + **test-only** (Plan 02). All server-side authorization, anti-forgery, and model validation live in the FROZEN `Controllers/WorkerController.cs` and `Models/ManageUserViewModel.cs` (confirmed 0-diff by D-08 guard).
- `readonly` on HTML inputs was never a security boundary in this application.
- ASVS Level 1 checks satisfied: authorization present (T-392-03), CSRF protection present (T-392-03/T-392-07), no sensitive data in test artifacts (T-392-05), test residue self-cleaning (T-392-06), DB helper non-production guarded (T-392-08).
