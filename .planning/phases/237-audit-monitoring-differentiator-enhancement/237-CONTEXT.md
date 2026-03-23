# Phase 237: Audit Monitoring & Differentiator Enhancement - Context

**Gathered:** 2026-03-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit accuracy dashboard, tracking, override, dan export Proton coaching. Tambah differentiator: workload indicator coach, batch approval HC, bottleneck analysis. Fix bugs yang ditemukan saat audit.

</domain>

<decisions>
## Implementation Decisions

### Dashboard & Chart Accuracy (MON-01)
- **D-01:** Audit accuracy saja — Claude investigasi apakah query stats dan Chart.js data sudah benar, fix bugs tanpa tambah card baru
- **D-02:** Tambah chart bottleneck — horizontal bar chart menampilkan top 5-10 deliverable paling lama pending (>30 hari), jumlah hari sebagai value
- **D-03:** Chart bottleneck ditambahkan di `_CoachingProtonContentPartial.cshtml` bersama trend line dan doughnut chart yang sudah ada

### Batch Approval HC (DIFF-02)
- **D-04:** Batch approval dilakukan dari halaman CoachingProton tracking (CDPController) — tambah checkbox per row + "Approve Selected" button
- **D-05:** Scope: HC Review saja — hanya deliverable dengan status "Pending HC Review" yang bisa di-batch approve
- **D-06:** UX: modal konfirmasi sebelum proses — tampilkan "Approve X deliverable?" dengan daftar item, user konfirmasi baru proses
- **D-07:** Endpoint baru di CDPController untuk batch HC approve — POST dengan list of deliverable IDs

### Workload Indicator (DIFF-01)
- **D-08:** Workload indicator ditampilkan di halaman CoachCoacheeMapping (AdminController) — tambah kolom "Jumlah Coachee Aktif" per coach
- **D-09:** Tidak perlu tampilkan di dashboard — cukup di mapping page dimana HC assign coachee

### Bottleneck Analysis (DIFF-03)
- **D-10:** Threshold bottleneck: 30 hari — deliverable pending >30 hari masuk kategori bottleneck
- **D-11:** Visualisasi: horizontal bar chart di dashboard (D-02) — top deliverable terlama

### CoachingProton Tracking Audit (MON-02)
- **D-12:** Claude audit filter cascade, pagination, role-based column visibility — fix bugs yang ditemukan

### Override Audit (MON-03)
- **D-13:** Audit trail + transition rules — Claude audit apakah setiap override tercatat di audit log
- **D-14:** Validasi status transition — tidak bisa override ke status ilegal (misalnya dari Approved kembali ke Pending)

### Export Audit + Export Baru (MON-04)
- **D-15:** Audit existing exports: N+1 query elimination, projection (select hanya kolom yang dipakai), role attribute check
- **D-16:** Export baru 1 — Bottleneck report Excel: daftar deliverable pending >30 hari dengan coachee, coach, section, jumlah hari pending
- **D-17:** Export baru 2 — Coaching tracking Excel: export data dari halaman CoachingProton tracking sesuai filter yang aktif
- **D-18:** Export baru 3 — Workload summary Excel: daftar coach dengan jumlah coachee aktif, jumlah deliverable pending per coach

### Claude's Discretion
- Detail implementasi horizontal bar chart (Chart.js config, color scheme)
- Batch approve endpoint design (AJAX + partial refresh atau full page reload)
- Checkbox UI pattern di CoachingProton tracking (select all, per-row, header checkbox)
- Export file naming dan column layout
- Override audit trail mechanism (existing AuditLog service atau tambahan)
- Query optimization detail untuk existing exports

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Milestone & Requirements
- `.planning/ROADMAP.md` — Phase 237 success criteria (7 items), dependency Phase 236→237
- `.planning/REQUIREMENTS.md` — MON-01 through MON-04, DIFF-01 through DIFF-03 requirement definitions

### Phase 233 Research
- `docs/audit-v7.7.html` — Dokumen riset perbandingan coaching platform, gap analysis monitoring flow

### Predecessor Decisions
- `.planning/phases/234-audit-setup-flow/234-CONTEXT.md` — Transaction patterns, progression validation
- `.planning/phases/235-audit-execution-flow/235-CONTEXT.md` — Race condition first-write-wins (D-10), StatusHistory patterns (D-15), approval chain
- `.planning/phases/236-audit-completion/236-CONTEXT.md` — Completion criteria (D-13), lifecycle (D-14-16), unique constraints

### Existing Code (audit targets)
- `Controllers/CDPController.cs` — Dashboard (L260), BuildProtonProgressSubModelAsync (L264-619), CoachingProton tracking (L1386-1679), ExportProgressExcel (L2347), ExportHistoriProton (L3022)
- `Controllers/ProtonDataController.cs` — Override (L209), OverrideList (L1246), OverrideDetail (L1311), OverrideSave (L1368), ExportSilabus (L720)
- `Controllers/AdminController.cs` — CoachCoacheeMapping management (workload indicator target)
- `Views/CDP/Shared/_CoachingProtonContentPartial.cshtml` — Chart.js implementation (L206-279), stat cards
- `Views/CDP/Dashboard.cshtml` — AJAX filter cascade (L48-147)
- `Models/CDPDashboardViewModel.cs` — ProtonProgressSubModel (L35+)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Chart.js pattern di `_CoachingProtonContentPartial.cshtml` — line/doughnut sudah ada, tambah bar chart mengikuti pattern yang sama
- `OverrideSave` batch endpoint (ProtonDataController:1368) — pattern batch save bisa di-reuse untuk batch approval
- ClosedXML Excel export pattern — sudah ada di ExportProgressExcel, ExportHistoriProton
- AJAX filter cascade pattern di Dashboard.cshtml — AbortController + partial replace
- `RecordStatusHistory` helper (CDPController:3021) — untuk tracking state changes
- `GetSectionUnitsDictAsync()` — org structure filtering

### Established Patterns
- Role-scoped filtering: HC/Admin=all, Coach=mapped coachees, SrSpv=same section
- GroupBy pagination di CoachingProton — max 20 rows/page, no split within SubKompetensi group
- Chart data passed via ViewBag/ViewModel arrays (TrendLabels, TrendValues, StatusLabels, StatusData)
- Export naming: `{Entity}_{Date}.xlsx`

### Integration Points
- `_CoachingProtonContentPartial.cshtml` — target untuk bottleneck chart baru
- CoachingProton tracking view — target untuk checkbox + batch approve button
- CoachCoacheeMapping view (AdminController) — target untuk workload kolom baru
- CDPController — target untuk batch approve endpoint baru
- CDPDashboardViewModel.ProtonProgressSubModel — perlu extend untuk bottleneck data

</code_context>

<specifics>
## Specific Ideas

- Bottleneck chart: horizontal bar, top 5-10 paling lama pending, threshold 30 hari
- Batch approval: checkbox per row + "Approve Selected" button + modal konfirmasi dengan daftar item
- Workload: kolom sederhana "Jumlah Coachee Aktif" di tabel mapping — HC langsung lihat beban saat assign
- 3 export baru: bottleneck report, coaching tracking (sesuai filter), workload summary

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 237-audit-monitoring-differentiator-enhancement*
*Context gathered: 2026-03-23*
