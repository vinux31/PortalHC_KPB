---
phase: 399
slug: foundation-junction-userunits-primary-mirror-multi-select-ui-display
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-18
---

# Phase 399 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Foundation: junction UserUnits + primary mirror + multi-select widget UI + display 7 surface.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| migration `Up` SQL → DB | Backfill statement literal statik (no user input) menyentuh Users + UserUnits | DDL + backfill rows |
| EF model → DB schema | Filtered-unique index enforce invariant 1-primary/user di DB-level | schema constraint |
| client form/Excel → WorkerController POST | `model.Units[]`, `model.PrimaryUnit`, sel Excel `Cell(6)` = input tak-tepercaya | membership data (untrusted) |
| WorkerController → DB (UserUnits + mirror) | Write-through transaksional; MU-07 deactivate mengubah mapping/PTA | membership + PTA mutation |
| AccountController → DB (read) | Profile/Settings baca `UserUnits` user login (scoped ke user.Id) | own-user units |
| display surfaces → render | Nama unit (admin-curated org-tree) di-render HTML/cetak | unit names |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-399-01-01 | Tampering | Backfill raw SQL (`migrationBuilder.Sql`) | mitigate | `Migrations/20260618045427_AddUserUnitsTable.cs:51-57` — static literal SQL, no string-concat user input, `WHERE NOT EXISTS` idempotent. Comment line 50 cites T-399-01-01. | closed |
| T-399-01-02 | Tampering (integrity) | Mirror desync Unit≠IsPrimary | mitigate | `Data/ApplicationDbContext.cs:350-353` — `HasIndex(uu => uu.UserId).IsUnique().HasFilter("[IsPrimary] = 1")` → `IX_UserUnits_UserId_PrimaryUnique` (migration L35-40). DB lokal: `users_with_more_than_1_primary = 0`. | closed |
| T-399-01-03 | DoS | Index pada nvarchar(max) | mitigate | `Models/UserUnit.cs:21` — `[MaxLength(200)]`; migration kolom `nvarchar(200)` (L20); composite unique `(UserId, Unit)` index-able. | closed |
| T-399-01-04 | Repudiation | Audit (deferred ke plan 02) | accept | Plan 01 = migration-only, no user-driven mutation. Audit set-diff di Plan 02. Lihat Accepted Risks Log. | closed |
| T-399-02-01 | Tampering / EoP | Mass-assignment `List<string> Units` (inject cross-Bagian) | mitigate | `WorkerController.cs:397-399` (Create) + `554-556` (Edit) — `ValidateUnitsInSection(GetUnitsForSectionAsync(...), ...)` server-side; reject `PrimaryUnit ∉ Units` (L63-76); Import L1225-1233. Bind surface dibatasi `ManageUserViewModel.cs:42-52`. | closed |
| T-399-02-02 | Tampering (integrity) | Mirror desync `ApplicationUser.Unit` ≠ IsPrimary row | mitigate | `WorkerController.cs:82-117` — `SyncUserUnitsAsync` replace-set; `BeginTransactionAsync` bungkus Create (L455), Edit (L656), Import (L1293); mirror di-set dalam tx sama sebelum `UpdateAsync`. | closed |
| T-399-02-03 | Tampering (data-loss) | MU-07 hapus unit dirujuk PTA/mapping aktif → orphan | mitigate | `WorkerController.cs:127-163` — `EvaluateRemoveUnitGuardAsync` server-side. PTA hard-block L144 (`hasActivePta && protonUnit != null && removed.Contains(protonUnit)`) independen dari `confirmedDeactivate`. Mapping deactivate atomik L660-665 dalam `uuTx`. | closed |
| T-399-02-04 | Repudiation | Perubahan membership tanpa jejak | mitigate | `WorkerController.cs:91-92` — set-diff string di `SyncUserUnitsAsync`; `changes.AddRange(unitDiff)` L658; `_auditLog.LogAsync(actor.Id, ...)` L703-709 (Edit), L468-471 (Create), L1327-1329 (Import). | closed |
| T-399-02-05 | EoP | Non-Admin/HC ubah unit | mitigate | `WorkerController.cs:375-376` (Create) + `530-531` (Edit) — `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`. Import POST authz juga. | closed |
| T-399-02-06 | DoS | Excel cell patologis (oversized/pipe berlebih) | mitigate | `WorkerController.cs:51-57` — `ParseUnitCell` split `'\|'` + `.Distinct(OrdinalIgnoreCase)` (L55); validasi per-unit whitelist Bagian L1225-1233 tolak unit asing sebelum DB write. | closed |
| T-399-03-01 | Tampering | Client kirim PrimaryUnit ∉ Units / unit asing via devtools | mitigate | Widget UX-only. Server re-validasi tiap POST via `ValidateUnitsInSection` (Create L397-399, Edit L554-556). Nilai client tak dipercaya. | closed |
| T-399-03-02 | Tampering | Bypass modal MU-07 → ConfirmedDeactivate=true tanpa lihat dampak | mitigate | `WorkerController.cs:144` — PTA hard-block independen `confirmedDeactivate`. Set `ConfirmedDeactivate=true` via devtools tak bypass server guard. Modal UX-only. | closed |
| T-399-03-03 | EoP | Akses form Create/Edit oleh non-Admin/HC | accept | `[Authorize(Roles="Admin, HC")]` di controller (L375, L530). View tak tambah surface. Lihat Accepted Risks Log. | closed |
| T-399-03-04 | Information Disclosure (XSS) | XSS nama unit di innerHTML render | mitigate | `wwwroot/js/shared-cascade.js:85-88` — `esc()` escape `&<>"'`; semua render innerHTML pakai `esc(unit)` (L159, L167-170). SUMMARY 399-03: DITUTUP via mitigate, bukan accept residual. | closed |
| T-399-04-01 | Information Disclosure | Profile/Settings tampil unit user lain | mitigate | `AccountController.cs:155-157` (Profile) + `204-206` (Settings) — `_context.UserUnits.Where(uu => uu.UserId == user.Id)`, user dari `GetUserAsync(User)`, no param user-controlled. WorkerDetail = Admin/HC only. | closed |
| T-399-04-02 | EoP | Non-authenticated akses Profile/Settings | accept | `AccountController.cs:13` — `[Authorize]` class-level, tak dilonggarkan. Lihat Accepted Risks Log. | closed |
| T-399-04-03 | Information Disclosure (XSS) | XSS nama unit di Razor | mitigate | Razor `@u` auto-encode: `Profile.cshtml:94,99`, `Settings.cshtml:126,131`, `WorkerDetail.cshtml:116,121`; `_PSign.cshtml:44` `@string.Join(...)` (encoded). Auto-encode default, tak di-disable. | closed |
| T-399-04-04 | Tampering (read-consistency) | Display unit beda dari mirror (stale) | accept | Display baca `UserUnits` langsung (sumber kebenaran); mirror hanya legacy. Write-through atomik (tx sama) → no stale-read window. By-design. Lihat Accepted Risks Log. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-399-01 | T-399-01-04 | Plan 01 = migration-only, no user-driven mutation path. Audit set-diff trail diimplementasi Plan 02 (`SyncUserUnitsAsync` + `_auditLog.LogAsync`). No mutation surface to audit at this layer. | 399-01-PLAN.md threat_model | 2026-06-18 |
| AR-399-02 | T-399-03-03 | Form Create/Edit dirender hanya setelah lolos `[Authorize(Roles="Admin, HC")]` di action level. View tak punya endpoint/data-exposure surface baru. Gate pre-existing, tak diubah Phase 399. | 399-03-PLAN.md (disposition `accept`) | 2026-06-18 |
| AR-399-03 | T-399-04-02 | AccountController `[Authorize]` class-level (L13), wajib sesi terautentikasi semua action Profile/Settings. Phase 399 tak ubah gate ini. ASP.NET Core Identity redirect-to-login default. | 399-04-PLAN.md (disposition `accept`) | 2026-06-18 |
| AR-399-04 | T-399-04-04 | Display baca `UserUnits` langsung (sumber kebenaran). `ApplicationUser.Unit` scalar mirror hanya untuk legacy caller. Write-through (`SyncUserUnitsAsync` + `BeginTransactionAsync`) update keduanya atomik tx sama — no stale-read window normal-op. | 399-CONTEXT.md + 399-04-PLAN.md (by-design) | 2026-06-18 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-18 | 18 | 18 | 0 | gsd-security-auditor (sonnet), ASVS L1 |

---

## Unregistered Flags

None. Keempat SUMMARY.md `## Threat Surface Scan` melaporkan no new attack surface di luar threat model. Semua mitigasi yang dicatat executor map 1:1 ke threat register di atas.

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-18
