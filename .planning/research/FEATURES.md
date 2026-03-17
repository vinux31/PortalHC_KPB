# Feature Research

**Domain:** Certificate Monitoring Dashboard (CDP module, internal HR portal)
**Researched:** 2026-03-17
**Confidence:** HIGH — direct inspection of TrainingRecord, UnifiedTrainingRecord, and PROJECT.md v7.4 specification

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = dashboard feels broken.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Unified certificate table | Core purpose — show all certs in one place | LOW | UnifiedTrainingRecord ViewModel exists; extend with UserId/UserName/Bagian/Unit for multi-user view |
| Expiry status badge per row | Visual at-a-glance — can't scan 100 rows of raw dates | LOW | `IsExpired` already on UnifiedTrainingRecord; add "Akan Expired" (≤30 days) using `DaysUntilExpiry` from TrainingRecord |
| Summary stat cards (Total / Aktif / Akan Expired / Expired) | Standard monitoring dashboard convention | LOW | Computed from same query result set — no extra DB round-trip |
| Role-scoped data visibility | Admin/HC see all; SH/SrSpv see own Bagian; Coach/Coachee see own records | MEDIUM | Pattern established in CDPController/CMPController — replicate Bagian>Unit cascade filter logic |
| Status filter dropdown | Users need to isolate just expired or just expiring-soon rows | LOW | Client-side JS filter sufficient at this data scale |
| Bagian > Unit cascade filter | HC/SH drill down by org unit | LOW | Pattern already built in CMP/CDP filters — copy pattern verbatim |
| Text search (worker name or cert title) | Find specific person or cert quickly | LOW | JS filter on rendered table; no server round-trip needed |
| Export to Excel | HC needs offline reporting and to share status list | LOW | ClosedXML pattern from v7.1 export — direct reuse |
| CDP/Index entry card | Discovery — users navigate to monitoring from CDP hub | LOW | One new card on CDP/Index; same card layout as existing cards |

### Differentiators (Competitive Advantage)

Features that make the dashboard genuinely useful beyond minimum viability.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Days-until-expiry numeric column | SH can see exactly who is 3 days vs 25 days away — actionable prioritization | LOW | `DaysUntilExpiry` already computed on TrainingRecord; null/blank for Permanent certs |
| Color-coded row highlighting | Red = expired, amber = expiring soon, green = valid — instant visual scan | LOW | CSS class on `<tr>` based on status; zero additional logic |
| "Permanent" label for online assessment certs | Clarifies these never expire vs training certs that do | LOW | AssessmentSession rows have no ValidUntil — show "Permanen" badge instead of a date |
| View certificate action per row | HC can immediately verify the actual document without navigating elsewhere | LOW | `SertifikatUrl` on TrainingRecord; link to assessment cert view for online rows |

### Anti-Features (Commonly Requested, Often Problematic)

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Automatic renewal email alerts | "Remind me before cert expires" | Background scheduler + email infra + preference management = 3x milestone scope | Surface expiry date and days-remaining prominently; SH/HC export and act manually |
| Inline cert editing from monitoring page | "Quick fix the validity date here" | Mixes read and write concerns; existing Edit modal in CMP/Records is the correct write path | Link from monitoring row to CMP Records page for edits |
| Bulk status update from dashboard | Mass-update expired certs at once | Online cert status is derived (not stored) — bulk write creates inconsistency; manual training certs have diverse validity periods | Export Excel → review offline → use existing Import if batch update needed |
| Server-side pagination with AJAX | "Scales to large data sets" | Adds controller complexity + loading states + URL state management; this portal serves a bounded single-refinery workforce | Client-side JS filter; revisit only if rows per role scope exceed ~500 |

## Feature Dependencies

```
Summary stat cards (Total / Aktif / Akan Expired / Expired)
    └──requires──> Unified certificate query (multi-user, role-scoped)
                       └──requires──> Role scope resolution (Bagian/Unit for current user)

Bagian > Unit cascade filter
    └──requires──> Bagian + Unit lists in DB (already exist)
    └──requires──> Role scope resolution (limits which Bagian options are shown)

Color-coded row highlight + expiry badge
    └──requires──> Expiry status computed per row (IsExpired on UnifiedTrainingRecord; extend with IsExpiringSoon)

Export Excel
    └──requires──> Same unified certificate query as table (pass same role-scope params)

View certificate action
    └──requires──> SertifikatUrl populated (Training Manual rows) OR GenerateCertificate path (Assessment Online rows)

CDP/Index entry card
    └──requires──> CDPController action for monitoring page registered and returning a view
```

### Dependency Notes

- **Summary cards require the same query as the table:** Compute counts in a single pass over the filtered result list — do not issue separate COUNT queries.
- **Export must respect role scope:** Export action receives same Bagian/Unit/status filter parameters as the table action; HC export should not leak records beyond the user's scope.
- **Role scope resolution is a prerequisite for all data features:** Admin/HC get all records; SH/SrSpv filter by their Bagian; Coach/Coachee filter by their own UserId. This branching must be resolved before any query runs.

## MVP Definition

### Launch With (v7.4 — this milestone)

- [ ] CDPController action (`MonitoringSertifikat`) + View
- [ ] Unified certificate query: all `TrainingRecord` rows (role-scoped by Bagian/Unit/UserId) + passed `AssessmentSession` rows where `GenerateCertificate = true` (role-scoped same way)
- [ ] Extend ViewModel for multi-user: add `UserName`, `Bagian`, `Unit` fields (training manual rows have these via User navigation; assessment rows via User)
- [ ] Summary stat cards: Total, Aktif, Akan Expired (≤30 days), Expired
- [ ] Table columns: Nama Pekerja, Judul Sertifikat, Jenis (Training Manual / Assessment Online), Tipe (Permanent / Annual / 3-Year), Tanggal, Berlaku Hingga, Status badge
- [ ] Color-coded row highlight (red/amber/green) + days-remaining for non-permanent certs
- [ ] Role-scoped data access: Admin/HC all, SH/SrSpv their Bagian, Coach/Coachee own records
- [ ] Bagian > Unit cascade filter + Status filter + text search (client-side JS)
- [ ] View certificate action (SertifikatUrl link or assessment cert link)
- [ ] Export Excel — same columns, same role scope
- [ ] CDP/Index entry card

### Add After Validation (v1.x)

- [ ] Direct download button for certificate file (distinct from "view" link) — needs affordance to distinguish file path vs external URL
- [ ] Sortable "Hari Tersisa" column for SH to prioritize by urgency

### Future Consideration (v2+)

- [ ] Email/notification alerts for expiring certs — requires background service (out of scope)
- [ ] Renewal request workflow

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Unified cert table (role-scoped) | HIGH | MEDIUM | P1 |
| Summary stat cards | HIGH | LOW | P1 |
| Expiry status badge + row color | HIGH | LOW | P1 |
| Bagian > Unit cascade filter | HIGH | LOW | P1 |
| Status + text search filter | HIGH | LOW | P1 |
| Export Excel | MEDIUM | LOW | P1 |
| CDP/Index entry card | HIGH | LOW | P1 |
| View certificate link | MEDIUM | LOW | P1 |
| Days-until-expiry column | MEDIUM | LOW | P2 |
| Sortable days-remaining | LOW | LOW | P2 |
| Download certificate file | LOW | LOW | P2 |

## Existing System Integration

These pieces are already built and directly enable the monitoring dashboard.

| Existing Component | How Monitoring Dashboard Uses It |
|-------------------|----------------------------------|
| `TrainingRecord` model | Primary source for manual training certs — `ValidUntil`, `CertificateType`, `IsExpiringSoon`, `DaysUntilExpiry`, `NomorSertifikat`, `SertifikatUrl` all present |
| `AssessmentSession` model | Source for online certs — rows where `IsPassed = true AND GenerateCertificate = true` are Permanent certificates |
| `UnifiedTrainingRecord` ViewModel | Starting point — extend with `UserName`, `Bagian`, `Unit` to support multi-user monitoring view |
| Role-scope filter pattern (CDPController) | Copy Coach/Coachee/SH/SrSpv/HC/Admin branching for data access |
| Bagian > Unit cascade (existing views) | Reuse JS cascade and dropdown population pattern |
| ClosedXML Excel export (v7.1) | Replicate export action and file-result pattern directly |
| CMP/Certificate.cshtml | Reference for certificate display link patterns |
| CDP/Index card layout | Replicate for new Monitoring Sertifikat entry card |

## Sources

- `PROJECT.md` v7.4 target feature specification (direct project requirements)
- `Models/TrainingRecord.cs` — inspected; all expiry fields confirmed present
- `Models/UnifiedTrainingRecord.cs` — inspected; bridging ViewModel structure confirmed
- Project memory: CDPController role-scope pattern, ClosedXML export pattern (v7.1), cascade filter pattern
- Standard certificate/compliance monitoring dashboard UX conventions (summary cards + filterable table + export)

---
*Feature research for: CDP Certificate Monitoring Dashboard (PortalHC KPB v7.4)*
*Researched: 2026-03-17*
