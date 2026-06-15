---
phase: 361
slug: bypass-ui-b
audited: 2026-06-11
asvs_level: 1
threats_total: 15
threats_closed: 15
threats_open: 0
status: secured
---

# SECURITY.md — Phase 361 (bypass-ui-b)

**Generated:** 2026-06-11
**ASVS Level:** 1
**Phase:** 361 — bypass-ui-b (Tab2 Bypass Tahun UI + backend prep + SQL fixture + e2e spec)
**Threats Closed:** 15/15
**Threats Open:** 0/15

---

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-361-01 | Information disclosure (BypassPendingList extended select) | accept | CLOSED | `Controllers/ProtonDataController.cs:97` — `[Authorize(Roles="Admin,HC")]` class-level attribute on `ProtonDataController` |
| T-361-02 | Information disclosure (ViewBag.AllCoaches) | accept | CLOSED | `Controllers/ProtonDataController.cs:97` — same class-level authz; pattern identical to `CoachMappingController.cs:146-149` already live |
| T-361-03 | Injection / SQLi (extended LINQ projection + LEFT JOIN) | mitigate | CLOSED | `Controllers/ProtonDataController.cs:1544-1572` — pure EF Core LINQ query syntax, no raw SQL or string interpolation; LEFT JOIN via `join c in _context.Users on p.TargetCoachId equals c.Id into cj` |
| T-361-04 | Tampering (null coach name when TargetCoachId has no match) | mitigate | CLOSED | `Controllers/ProtonDataController.cs:1552-1553` — `from c in cj.DefaultIfEmpty()` + `:1571` — `targetCoachNama = c != null ? (c.FullName ?? c.UserName) : null` |
| T-361-05 | Tampering / data pollution (fixture INSERT) | mitigate | CLOSED | `.planning/seeds/361-bypass-fixtures.sql:79-97` — WIPE-AND-INSERT by marker `AssignedById='PHASE361-FIXTURE'` + `Reason LIKE 'Phase 361%'` before `BEGIN TRAN`; scope ketat 4 worker fixture; `tests/e2e/proton-bypass.spec.ts:65` — `afterAll` restore DB |
| T-361-06 | DoS / FK violation crash (INSERT referencing missing user/track) | mitigate | CLOSED | `.planning/seeds/361-bypass-fixtures.sql:36` — `SET XACT_ABORT ON`; `:42-66` — 9 THROW guards (50001–50009) for all referenced users, tracks, and deliverables; `:105/180` — `BEGIN TRAN` / `COMMIT` boundary |
| T-361-07 | Tampering (fixture run against Dev/Prod) | accept | CLOSED | `.planning/seeds/361-bypass-fixtures.sql:13-14` — header comment "DB lokal SAJA — JANGAN jalankan di Dev/Prod"; `docs/SEED_JOURNAL.md:168` — entry klasifikasi `temporary + local-only`; `tests/helpers/dbSnapshot.ts:33-42` — localhost-only guard rejects non-localhost `-S` argument |
| T-361-08 | Tampering / XSS (server data via innerHTML) | mitigate | CLOSED | `Views/ProtonData/Override.cshtml:660` — `escB()` function defined for Tab2 IIFE scope (body identical to `escHtml` at :630); used on ALL server-origin strings: `:769-771` (pending panel rows), `:820-824` (worker table rows), `:967-972` (wizard recap), `:1050-1051` (confirm modal body); `showToast` at :641 uses `textContent` (not innerHTML) |
| T-361-09 | Tampering / CSRF (POST BypassSave/Confirm/CancelPending) | mitigate | CLOSED | Backend: `Controllers/ProtonDataController.cs:1625` `[ValidateAntiForgeryToken]` on `BypassSave`; `:1668` on `BypassConfirm`; `:1686` on `BypassCancelPending`. Client: `Views/ProtonData/Override.cshtml:669` — `getToken()` reads `input[name="__RequestVerificationToken"]`; `:1015` — `headers: { 'RequestVerificationToken': getToken() }` on wizard submit POST; `postPendingAction` at :1075 same pattern |
| T-361-10 | Tampering / double-submit race (ganda klik tombol POST) | mitigate | CLOSED | `Views/ProtonData/Override.cshtml:1001` — wizard submit button disabled + spinner `Menyimpan...` before POST; `postPendingAction` same pattern (Konfirmasi/Batal); backend atomicity from Phase 360 service layer (D-12) |
| T-361-11 | Tampering / stale-state confirm (konfirmasi pending basi) | mitigate | CLOSED | `Controllers/ProtonDataController.cs` — `BypassConfirm` delegates to `_protonBypassService.ConfirmBypassAsync` which re-checks preconditions (D-11, "Kondisi rencana sudah berubah" surfaced in UAT 361-04 summary); `Views/ProtonData/Override.cshtml:1085` — `loadPendingPanel()` called after confirm action (D-20 auto-refresh); `:1131` — WR-01 fix: `if (!loadOk) return` prevents false stale toast on fetch failure |
| T-361-12 | Elevation (coach dropdown DOM manipulation) | accept | CLOSED | Backend service re-validates coach eligibility (constraint E15 from Phase 360); UI dropdown is decorative — server is authoritative |
| T-361-13 | Tampering / DB pollution from test (seed fixture + pending flow) | mitigate | CLOSED | `tests/e2e/proton-bypass.spec.ts:49` — `beforeAll` calls `db.backup(snapshotPath)`; `:65` — `afterAll` calls `db.restore(snapshotPath)` (sukses OR gagal); `docs/SEED_JOURNAL.md:168` — status `cleaned` post-restore |
| T-361-14 | Info disclosure (test credentials hc/admin pwd 123456) | accept | CLOSED | `tests/e2e/proton-bypass.spec.ts:88` — uses `login(page, 'hc')` DEV LOCAL accounts only; pattern identical to existing specs; CLAUDE.md forbids use against Dev/Prod |
| T-361-15 | DoS (spec run without app at :5277) | accept | CLOSED | `tests/e2e/proton-bypass.spec.ts:1-14` — header comment documents PRECONDITION: `Authentication__UseActiveDirectory=false dotnet run`; Plan 04 human-verify checkpoint gates live run |

---

## Unregistered Threat Flags

None. No `## Threat Flags` section was present in any 361-01 through 361-04 SUMMARY files.

---

## Accepted Risks Log

| Threat ID | Rationale |
|-----------|-----------|
| T-361-01 | Extended fields (skor/reason/coach) are operational data visible only to HC/Admin roles. Not cross-tenant PII. Class-level `[Authorize]` on controller is sufficient at ASVS L1. |
| T-361-02 | Coach name list is not confidential. Identical pattern (`CoachMappingController`) already live and accepted. |
| T-361-07 | Execution control is procedural (header comment + SEED_JOURNAL + localhost guard in dbSnapshot.ts). Risk is accepted because the fixture file is developer-tooling only, never deployed. |
| T-361-12 | Backend service holds authority; client dropdown is UX-only. No additional control needed at ASVS L1. |
| T-361-14 | DEV LOCAL accounts. CLAUDE.md workflow rules and dbSnapshot localhost guard prevent accidental staging/prod use. |
| T-361-15 | Test infrastructure constraint documented as precondition. App availability is a runtime dependency, not a remediable security gap. |

---

## Notes

- **WR-01 surface fix (commit 77394f15):** `loadPendingPanel()` return semantics change (returns `true` on success including 0-pending, `false` on fetch error) guarded by `if (!loadOk) return` at Override.cshtml:1131. This prevents the deep-link stale toast from firing spuriously when the pending panel fetch itself fails. Primary mitigation for T-361-11 remains the backend D-11 re-check in `ConfirmBypassAsync`.
- **escB deviation (361-03-SUMMARY):** `escHtml` at Override.cshtml:630 is scoped inside IIFE Tab1. Tab2 IIFE defines `escB()` with identical implementation at :660. All server-origin data in Tab2 innerHTML uses `escB()`. T-361-08 mitigation is intact.
- **SEED_JOURNAL Phase 361 entry status:** `cleaned` — DB lokal restored post-UAT (spec afterAll + manual RESTORE WITH REPLACE, 1954 pages, verified fixture rows 0/0/0).
