---
phase: 374
slug: ui-managepackages-lock-pre-post
status: secured
threats_open: 0
asvs_level: 1
created: 2026-06-13
---

# Phase 374 — Security

> Per-phase security contract: threat register, accepted risks, audit trail. ASVS L1, block_on=high.

---

## Audit Status

**Threats Closed:** 6/6 · **Open:** 0/6 · **Unregistered Flags:** 0

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| HC/Admin browser → UpdateShuffleSettings POST | Form mengirim assessmentId(int) + 2 checkbox bool; state-changing write ke semua sibling grup | toggle config (low sensitivity) + AntiForgery token |
| ManagePackages GET → DB | Query sibling lock-state + Pre saved-state via LinkedSessionId | read-only metadata assessment |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-374-csrf | Tampering | UpdateShuffleSettings POST + form | mitigate | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` | closed |
| T-374-lock | Tampering/Elevation | Server lock guard | mitigate | Server-side re-check `ShuffleToggleRules.IsShuffleLocked` → locked→TempData error+redirect TANPA write (D-04a); UI disabled UX-only | closed |
| T-374-priv | Elevation | Endpoint authorization | mitigate | `[Authorize(Roles = "Admin, HC")]` | closed |
| T-374-audit | Repudiation | Audit trail | mitigate | `AuditLogService.LogAsync` actor "NIP - FullName" + targetId, try/catch warn-only | closed |
| IDOR | Tampering | assessmentId milik orang lain | accept | Role Admin/HC global, no per-owner check — konsisten Reshuffle/Edit | closed |
| XSS | Tampering | Razor copy statis | accept | Copy literal statis, no user-input interpolasi, Razor auto-encode | closed |

*Status: open · closed*

---

## Mitigate — Bukti Detail (re-grep anchor; line = hint, area drift v25.0)

### T-374-csrf
- `Controllers/AssessmentAdminController.cs` (~L5254-5256): `[HttpPost]` + `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` trio di atas `UpdateShuffleSettings`.
- `Views/Admin/ManagePackages.cshtml` (~L105): `@Html.AntiForgeryToken()` di dalam form `asp-action="UpdateShuffleSettings"`.

### T-374-lock
- `AssessmentAdminController.cs` POST (~L5277): `if (ShuffleToggleRules.IsShuffleLocked(anyStarted, anyAssignment))` → `TempData["Error"]` + `RedirectToAction` TANPA `SaveChangesAsync`.
- `AssessmentAdminController.cs` GET (~L5400): `ViewBag.IsShuffleLocked = ShuffleToggleRules.IsShuffleLocked(shufAnyStarted, shufAnyAssignment)` — single-source, helper sama (Pitfall 2 mati).
- `Helpers/ShuffleToggleRules.cs`: `IsShuffleLocked(bool,bool) => anyStarted || anyAssignment` (pure, no EF).
- `HcPortal.Tests/ShuffleLockGuardTests.cs`: 3 test real-SQL (Guard_RejectsWrite_WhenSiblingStarted / Guard_AllowsWrite_WhenClean / Guard_RejectsWrite_WhenAssignmentExists) membuktikan POST-replica tidak menulis saat locked, menulis saat bersih. UAT browser scenario 4: switch+tombol disabled + banner.

### T-374-priv
- `AssessmentAdminController.cs` (~L5255): `[Authorize(Roles = "Admin, HC")]` tepat di atas signature `UpdateShuffleSettings`. Non-Admin/HC ditolak.

### T-374-audit
- `AssessmentAdminController.cs` (~L5296-5309): `_auditLog.LogAsync(hcUser?.Id ?? "", actorNameStr, "UpdateShuffleSettings", desc, assessmentId, "AssessmentSession")` try/catch warn-only. Actor "NIP - FullName". UAT scenario 2: audit row tertulis (DB-verified).

---

## Accepted Risks Log

| Risk ID | Category | Rationale | Reviewed |
|---------|----------|-----------|---------|
| IDOR | Tampering | Role Admin/HC global — tidak ada per-owner restriction di assessment admin. Konsisten pola ReshufflePackage/EditAssessment yang juga tanpa per-owner check. Model otorisasi sistem memang role-based global untuk admin/HC. | 2026-06-13 |
| XSS | Tampering | Seluruh copy card "Pengacakan Soal & Jawaban" literal statis Razor (no user-input interpolasi ke markup). Razor auto HTML-encode semua `@variable`. Tidak ada vector XSS relevan. | 2026-06-13 |

---

## Unregistered Flags

Tidak ada flag SUMMARY.md di luar register.

---

## Audit Trail

### Security Audit 2026-06-13
| Metric | Count |
|--------|-------|
| Threats found | 6 |
| Closed | 6 |
| Open | 0 |

Komponen diaudit: `ShuffleToggleRules.cs`, `AssessmentAdminController.cs` (UpdateShuffleSettings POST + ManagePackages GET), `ManagePackages.cshtml` card, `ShuffleLockGuardTests.cs`. Full suite 347/347 PASS, UAT browser 7/7. Auditor: gsd-security-auditor (sonnet). Verdict: SECURED.
