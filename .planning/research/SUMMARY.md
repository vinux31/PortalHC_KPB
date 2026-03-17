# Project Research Summary

**Project:** PortalHC KPB — CDP Certificate Monitoring Dashboard (v7.4)
**Domain:** Internal HR portal — certificate monitoring extension to CDP module
**Researched:** 2026-03-17
**Confidence:** HIGH

## Executive Summary

The certificate monitoring dashboard is a read-only compliance overview page extending the existing CDP module. It unifies two already-modeled data sources — `TrainingRecord` (manual training certificates) and `AssessmentSession` (online assessment certificates) — into a single role-scoped table with expiry status badges, summary counters, filter controls, and an Excel export. All required dependencies (EF Core, ClosedXML, Bootstrap 5, QuestPDF) are already installed; no new packages are needed. The implementation follows established patterns in `CDPController` and can reuse or copy verbatim the role-scoping helper, the cascade filter endpoint, and the ClosedXML export action.

The recommended approach is a straightforward MVC extension: one new ViewModel file (`MonitoringSertifikatViewModel`), three new controller actions (`MonitoringSertifikat`, `FilterMonitoringSertifikat`, `ExportSertifikatExcel`) plus a private `BuildSertifikatRowsAsync` helper, and two new views (full page + AJAX partial). All role-scoping, filter wiring, and export logic mirrors code already present and tested in the same controller. The build sequence is well-defined by dependency order and produces independently testable increments.

The primary risks are correctness traps in the dual-source ViewModel mapping layer, not in infrastructure. The two most dangerous are: (1) overloading `ValidUntil == null` to mean both "permanent assessment cert" and "training cert with missing validity date," and (2) applying role-scoping to `TrainingRecord` but forgetting to apply the same scope to `AssessmentSession`. Both are avoidable by defining a `RecordType` discriminator on the ViewModel and building a single `GetAllowedUserIdsAsync` helper consumed by both queries. These must be addressed in the ViewModel and query phases before any view or filter work begins.

---

## Key Findings

### Recommended Stack

The feature requires no new packages. All infrastructure is in place and validated in production by earlier milestones. EF Core 8 handles the LINQ queries; ClosedXML 0.105.0 (installed v7.1) covers export; Bootstrap 5 (CDN) handles the UI. Server-rendered Razor was chosen over DataTables.js because role-scoped filtering must be server-enforced — sending the full unfiltered dataset to the client is incompatible with the access model.

**Core technologies:**
- ASP.NET Core MVC (.NET 8, existing): New actions in `CDPController` — no friction, shared DI constructor
- EF Core 8 SqlServer (existing): LINQ projection of `TrainingRecord` + `AssessmentSession` into flat ViewModel rows
- ClosedXML 0.105.0 (existing): Excel export using the same `XLWorkbook` + `FileStreamResult` pattern as `AdminController`
- Bootstrap 5 / Bootstrap Icons (existing CDN): Summary cards, status badges, responsive table — no custom CSS needed

### Expected Features

All P1 features are table stakes for a monitoring dashboard and map directly to existing infrastructure. P2 features add value but are not blocking.

**Must have (table stakes):**
- Unified certificate table (role-scoped, dual-source) — core purpose of the page
- Summary stat cards: Total / Aktif / Akan Expired / Expired — standard monitoring convention
- Expiry status badge per row with color-coded row highlight — required for at-a-glance scanning
- Role-scoped data access (Admin/HC = all, SH/SrSpv = own Bagian, Coach/Coachee = own records)
- Bagian > Unit cascade filter + status filter + text search (client-side JS)
- Export to Excel (Admin/HC only, ClosedXML) — HC needs offline reporting
- CDP/Index entry card — navigation discovery

**Should have (differentiators):**
- Days-until-expiry numeric column — enables SH to prioritize by urgency
- "Permanen" label for online assessment certs — clarifies no expiry vs. null date
- View certificate action per row — immediate document verification without navigating elsewhere

**Defer (v2+):**
- Automated email/notification alerts for expiring certs — requires background scheduler and email infra
- Renewal request workflow
- Sortable days-remaining column (low complexity, low demand — add post-validation)

### Architecture Approach

The feature adds three public actions and one private helper to `CDPController`, one new ViewModel file, one full-page view, and one AJAX partial. Nothing is restructured. The AJAX filter pattern mirrors `FilterCoachingProton` exactly; the cascade dropdown reuses the existing `GetCascadeOptions` endpoint unchanged; the export mirrors CDPController lines 2137-2184. The key architectural decision is the `BuildSertifikatRowsAsync` private helper that centralizes role-scope enforcement and both queries — all three public actions consume this helper to guarantee consistent scoping.

**Major components:**
1. `MonitoringSertifikatViewModel` + `SertifikatRow` — flat ViewModel with `RecordType` discriminator, `CertificateStatus` (computed string), canonical date fields, and summary counters
2. `CDPController.BuildSertifikatRowsAsync` — private helper; single role-scoped user ID set applied to both `TrainingRecord` and `AssessmentSession` queries before concatenation
3. `MonitoringSertifikat.cshtml` + `_MonitoringSertifikatTablePartial.cshtml` — full page and AJAX partial; table and summary cards always derived from the same filtered list

### Critical Pitfalls

1. **Null ValidUntil overloading** — `ValidUntil == null` means "Permanent" for assessment rows but "unknown/missing" for training rows. Fix: add `RecordType` discriminator to `SertifikatRow`; derive `CertificateStatus` server-side in the mapping layer, never in Razor.

2. **Role-scoping applied to only one source** — Scoping `TrainingRecord` but forgetting `AssessmentSession` causes SH to see cross-section assessment certs. Fix: build a single `HashSet<string>` of allowed user IDs once, apply to both queries via `.Where(x => allowedIds.Contains(x.UserId))`.

3. **Expiry status computed in Razor view** — Summary card counts diverge from table rows when filters are applied. Fix: compute `CertificateStatus` as a stored string on `SertifikatRow`; derive summary counts from `.Count()` on the already-filtered ViewModel list.

4. **Failed assessment sessions appearing as certificates** — `IsPassed` is `bool?`; filtering only on `Status == "Completed"` includes failed exams. Fix: always filter with `IsPassed == true && GenerateCertificate == true`.

5. **Coach role-scoping copies coaching oversight pattern** — CDPController's existing Coach scope shows coachees' data; certificate monitoring needs own-records only. Fix: for Coach/Coachee, scope to `userId == currentUser.Id`; do not traverse `CoachCoacheeMapping`.

---

## Implications for Roadmap

Based on research, the natural build order flows from ViewModel definition through data query, then controller actions, then views, then wiring. Each step is independently testable before the next begins.

### Phase 1: ViewModel and Data Model Foundation
**Rationale:** All downstream phases depend on `SertifikatRow` and `MonitoringSertifikatViewModel` being correct. Pitfalls 1, 3, and 5 (null overloading, status in view, wrong date anchor) must be prevented here before any query or view code is written.
**Delivers:** `Models/MonitoringSertifikatViewModel.cs` with `SertifikatRow` — `RecordType` discriminator, `CertificateStatus` as a computed string, canonical date mapping (`TanggalSelesai ?? TanggalMulai ?? Tanggal` for training; `CompletedAt ?? Schedule` for assessment) documented in comments.
**Addresses:** Unified certificate table structure, expiry status badge logic for both sources.
**Avoids:** Pitfalls 1, 3, 5 — null overloading, view-side status derivation, wrong date baseline.

### Phase 2: Role-Scoped Data Query
**Rationale:** Data access correctness must be established before any view renders to avoid leaking cross-scope records. Pitfalls 2 and 6 (scoping asymmetry, coach role confusion) are addressed here and nowhere else.
**Delivers:** `BuildSertifikatRowsAsync` private helper in `CDPController` — single allowed-user-ID set applied to both sources; Pitfall 4 guard (`IsPassed == true && GenerateCertificate == true`); all four role tiers documented in code comments.
**Addresses:** Role-scoped data access for all roles, dual-source query merge.
**Avoids:** Pitfalls 2, 4, 6 — scoping asymmetry, failed sessions, coach sees coachees.

### Phase 3: Full-Page Controller Action and Static View
**Rationale:** With ViewModel and query helper in place, the main GET action and static Razor view are low-risk. Summary cards are computed from the filtered list in a single pass — no separate DB queries.
**Delivers:** `CDPController.MonitoringSertifikat` action + `Views/CDP/MonitoringSertifikat.cshtml` with static table (no AJAX yet) + summary stat cards (Total / Aktif / Akan Expired / Expired).
**Uses:** EF Core, Bootstrap 5 card/badge/table-responsive components.
**Implements:** Full-page data flow (architecture steps 3-4).

### Phase 4: AJAX Filter and Cascade Dropdown
**Rationale:** Filter wiring is decoupled from the base page; the AJAX partial can be developed and tested independently once the base view exists.
**Delivers:** `FilterMonitoringSertifikat` AJAX action + `_MonitoringSertifikatTablePartial.cshtml` + JS filter bar wired in `MonitoringSertifikat.cshtml` + Bagian > Unit cascade (reusing existing `GetCascadeOptions` endpoint unchanged).
**Uses:** Vanilla `fetch()`, existing `GetCascadeOptions` endpoint (no modifications needed).
**Implements:** AJAX filter pattern mirroring `FilterCoachingProton`.

### Phase 5: Excel Export and CDP Entry Card
**Rationale:** Export depends on `BuildSertifikatRowsAsync` (Phase 2) but not on AJAX filter wiring; it can proceed once the query helper and ViewModel are stable. Entry card requires the page to exist (Phase 3). Both are low-complexity.
**Delivers:** `ExportSertifikatExcel` action (Admin/HC role-gated) + Export button in view + CDP/Index entry card.
**Uses:** ClosedXML `XLWorkbook` pattern (copy from CDPController lines 2137-2184).
**Implements:** Architecture export flow.

### Phase Ordering Rationale

- ViewModel first because all pitfalls root-cause to incorrect ViewModel design; fixing them later is medium recovery cost per the PITFALLS.md recovery table.
- Query helper second because all three public actions consume it — building the helper before any action prevents copy-paste scope divergence.
- Static page before AJAX filter because the base view is independently testable; AJAX is an incremental layer.
- Export and entry card last because they have no blockers once the core page and query helper are complete; they are independently parallelizable within Phase 5.

### Research Flags

Phases with standard, well-documented patterns — no `/gsd:research-phase` needed:
- **Phase 3 (Controller action + static view):** Standard CDPController pattern; no unknowns.
- **Phase 4 (AJAX filter):** Direct structural copy of `FilterCoachingProton`; endpoint reuse confirmed with line number.
- **Phase 5 (Export + entry card):** ClosedXML export pattern proven in v7.1; card markup is a direct copy from existing CDP/Index cards.

Phases that benefit from reading specific existing code before writing (not a full research cycle):
- **Phase 1 (ViewModel):** Read `Models/AllWorkersHistoryRow.cs` for the Phase 40 `RecordType` discriminator precedent, and `Models/TrainingRecord.cs` nullable fields before finalizing `SertifikatRow` shape.
- **Phase 2 (Query helper):** Read the role-scoping block in `CDPController.BuildProtonProgressSubModelAsync` and confirm `AssessmentSession` FK structure (no navigation property to `ApplicationUser`) before writing the query.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All packages confirmed in `HcPortal.csproj`; versions verified; no new dependencies required |
| Features | HIGH | Derived from direct `PROJECT.md` v7.4 spec + model inspection; requirements are unambiguous |
| Architecture | HIGH | Derived from direct CDPController inspection; all reference patterns identified with line numbers |
| Pitfalls | HIGH | Based on model inspection revealing actual nullable types and missing navigation properties |

**Overall confidence:** HIGH

### Gaps to Address

- **`SertifikatUrl` file serving approach:** PITFALLS.md flags that raw `SertifikatUrl` should not be exposed as a direct `<a href>` path because it points to `wwwroot/uploads/certificates/`. Decide in Phase 3 whether to serve files through a controller action with ownership verification or to confirm the URL is an absolute/external path safe to link directly. Do not defer this until the view is built.

- **`TrainingRecord` rows without `ValidUntil`:** Existing records may predate the `ValidUntil` column. Decide in Phase 1 whether these display as "Tidak Diketahui" or are excluded from the active/expiry counts. Verify against actual DB data before finalizing `CertificateStatus` derivation logic.

---

## Sources

### Primary (HIGH confidence)
- `HcPortal.csproj` — confirmed all installed packages and exact versions
- `Models/TrainingRecord.cs` — confirmed `ValidUntil` (nullable), `CertificateType`, `DaysUntilExpiry`, `IsExpiringSoon`, `SertifikatUrl`
- `Models/AssessmentSession.cs` — confirmed `IsPassed` is `bool?`, `GenerateCertificate` is `bool`, no `ValidUntil` field, no EF navigation property to `ApplicationUser`
- `Models/UnifiedTrainingRecord.cs` + `Models/AllWorkersHistoryRow.cs` — established ViewModel patterns for dual-source unified rows with `RecordType` discriminator
- `Controllers/CDPController.cs` — `FilterCoachingProton` (L267), `GetCascadeOptions` (L287), ClosedXML export block (L2137-2184), role-scoping pattern
- `Models/UserRoles.cs` — role level hierarchy, `HasSectionAccess()`, `IsCoachingRole()` helpers
- `PROJECT.md` v7.4 — feature specification and role-scope requirements

### Secondary (MEDIUM confidence)
- Standard certificate/compliance monitoring dashboard UX conventions — summary cards + filterable table + export is the established pattern for internal HR compliance dashboards

---
*Research completed: 2026-03-17*
*Synthesized from: STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md*
*Ready for roadmap: yes*
