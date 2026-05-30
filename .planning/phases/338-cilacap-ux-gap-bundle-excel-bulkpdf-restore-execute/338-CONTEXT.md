---
phase: 338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute
type: context
created: 2026-05-30
status: locked
requirements: [CIL-01, CIL-02, CIL-03, CIL-04, CIL-05, CIL-06, REST-04, REST-05, REST-06, REST-07]
depends_on: [336, 337]
---

<phase_summary>
**Phase 338** = bundle 10 REQ across 5 wave: 6 Cilacap UX gap fix (CIL-01..06) + restore execute Strategy A dari Phase 336 decision (REST-04) + 3 guardrail item (REST-05/06/07). Source: 2 todo (`001-gap-ux-assessment-monitoring`, `002-restore-pretest-ojt-gast-cilacap`) + Phase 336 outcome.

**Severity:** MED-HIGH (Gap #5 Excel breakdown = future loss recovery enabler, REST-04 = data restoration).
**Effort:** M-L (~1 minggu, 5 wave internal, 5 plan per wave).
</phase_summary>

<canonical_refs>
- `.planning/ROADMAP.md` — Phase 338 entry full goal + REQ mapping
- `.planning/REQUIREMENTS.md` — CIL-01..06 + REST-04..07 acceptance criteria
- `.planning/todos/pending/001-gap-ux-assessment-monitoring.md` — Gap #1-6 detail + technical notes
- `.planning/todos/pending/002-restore-pretest-ojt-gast-cilacap.md` — Restore strategy options + risk
- `.planning/phases/336-investigate-pretest-loss-cilacap-restore-strategy/336-RESTORE-DECISION.md` — REST-04 Strategy A LOCKED (user-approved 2026-05-30)
- `.planning/phases/336-investigate-pretest-loss-cilacap-restore-strategy/336-NAMING-CONVENTION-SPEC.md` — REST-06 naming format spec + Track Master
- `.planning/phases/336-investigate-pretest-loss-cilacap-restore-strategy/336-ROOT_CAUSE.md` — Root cause: IT redeploy tanpa backup, NOT app bug
- `docs/DB_HANDOFF_IT_2026-05-13.html` — Existing handoff doc precedent (REST-05 enhance baseline)
- `docs/DB_HANDOFF_IT_2026-05-26.html` — Second precedent
- `docs/DEV_WORKFLOW.md` — Existing dev SOP (REST-07 target update)
- `Controllers/AssessmentAdminController.cs` L59 ManageAssessment, L277 ManageAssessmentTab_History, L4077 ExportAssessmentResults
- `Controllers/CMPController.cs` L195 Assessment action (CIL-04 banner target)
- `Helpers/ExcelExportHelper.cs` (CIL-05 +2 sheet target)
- `Models/AssessmentResultsViewModel.cs` (Detail Per Soal + Elemen Teknis source)
- `Views/CMP/Results.cshtml` (CIL-04 banner + CIL-06 spider chart layout reference)
- `HcPortal.csproj` L27 — QuestPDF 2026.2.2 already available (CIL-06 ready)
</canonical_refs>

<decisions>

## Locked dari Phase 336

### REST-04: Restore Strategy
**LOCKED Strategy A — Re-import via Excel Backup** (336-RESTORE-DECISION.md user-approved 2026-05-30).
- Source: `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx` (13 peserta, score total)
- Mechanism: Admin endpoint `AddManualAssessment` (commit `0dedd7b7` Apr 14)
- Audit tag: `ManualImport-Backfill` (transparent traceable)
- CompletedAt: manual set 2026-03-30 (non-organic timestamp accepted)
- Trade-off accepted: Spider Elemen Teknis untuk PreTest TIDAK recoverable (Excel cuma score total)

### REST-06: Naming Convention
**LOCKED format** (336-NAMING-CONVENTION-SPEC.md):
```
{Stage} Test {Track} {Lokasi}
```
- {Stage}: `Pre` | `Post` (literal, capitalize first letter)
- `Test`: literal word dengan space pemisah
- {Track}: dari master list (OJT GAST, OJT Pekerja GAST, CMP, CDP, BP, KKJ)
- {Lokasi}: `di Unit {Unit} RU {Refinery} {Kota}` ATAU fallback `{Kota}`

Phase 338 W5 task: validate Track Master vs DB + enforce `LinkedGroupId` auto-pair Pre/Post di admin create form.

## Phase 338 Decisions (user 2026-05-30)

### D-01: CIL-01 Implementation
**Badge counter Closed** — preserve default "Open" filter, tambah badge counter Closed di tab list ManageAssessment + AssessmentMonitoring. Backward-compat user lama + visual cue Closed exist.
**Why:** User lama tidak bingung default berubah; badge cue cukup discoverable untuk surface Closed.

### D-02: CIL-03 History Drill-down UI
**Row clickable + Actions column** — `<tr>` seluruh row clickable (cursor pointer + data-href + JS handler ala Plan 337-02 CMP-19 pattern) + tambah kolom Actions dengan ikon link explicit ke `/CMP/Results/{sessionId}`.
**Why:** Best discoverability + a11y (keyboard + screen reader). Pattern reuse Plan 337-02.

### D-03: CIL-05 Excel "Detail Per Soal" Format
**Grid per-peserta-per-soal** — kolom dinamis: `No | Nama | NIP | Soal 1 Jawaban | Soal 1 Benar? | Soal 2 Jawaban | Soal 2 Benar? | ... | Score Total`. 1 row per peserta.
**Why:** Best untuk pivot analysis HC/admin (filter peserta, compare antar soal).

### D-04: CIL-05 Excel "Elemen Teknis" Format
**Matrix peserta x elemen** — kolom: `Nama | NIP | Elemen 1 Score | Elemen 2 Score | ... | Avg`. 1 row per peserta. Matches spider chart radar dimensi.
**Why:** Konsisten dengan spider visualization existing; aggregate analysis ready.

### D-05: CIL-06 BulkExportPdf Layout
**Multi-page lengkap per peserta** — Page 1: cover (nama/nip/score/pass status) + spider chart visualization. Page 2+: jawaban per soal dengan correctness highlight (correct=green, wrong=red).
**Why:** Comprehensive evidence package per peserta (~3-5 page); ZIP terhitung wajar untuk batch 13-50 peserta. Justify QuestPDF complexity.

### D-06: REST-05 Backup Hook Architecture
**A+ Enhanced existing DB_HANDOFF_IT workflow** — bangun di atas pattern doc 2026-05-13 + 2026-05-26 yang sudah include backup section. Phase 338 W5 deliverable:
1. **Template generator**: `docs/templates/DB_HANDOFF_IT.template.md` (Markdown source → render HTML) yang WAJIB include backup section
2. **Standalone backup script**: `scripts/backup-dev-pre-migration.ps1` PowerShell yang IT jalankan via 1 command (referensikan dari handoff doc)
3. **DEV_WORKFLOW.md SOP** (REST-07): "Setiap deploy ke Dev/Prod: (1) generate DB_HANDOFF_IT_*.html via template, (2) attach ke IT email + WhatsApp, (3) IT WAJIB jalankan `scripts/backup-dev-pre-migration.ps1` SEBELUM migration"
**Why:** Existing precedent ada (handoff doc 2026-05-13 sudah BACKUP DATABASE SQL inline). Systematize via template + script standalone supaya konsisten antar deployment, tidak bergantung disiplin tulis doc manual tiap kali.

### D-07: Plan Split Structure
**5 plan per wave** matches ROADMAP entry:
- **Plan 01 (Wave 1)** — CIL-01 + CIL-02: filter default badge + search aggregation
- **Plan 02 (Wave 2)** — CIL-03 + CIL-04: history drill-down + banner alert
- **Plan 03 (Wave 3 HIGH PRIORITY)** — CIL-05: Excel +2 sheet Detail Per Soal + Elemen Teknis
- **Plan 04 (Wave 4)** — REST-04 + CIL-06: restore execute Strategy A + BulkExportPdf ZIP
- **Plan 05 (Wave 5)** — REST-05 + REST-06 + REST-07: backup hook + LinkedGroupId enforce + DEV_WORKFLOW update
**Why:** Clean wave boundary, parallel UAT per plan, audit-friendly atomic commit per concern. Trade-off: more files (5 vs 3) tapi worth granularity.

</decisions>

<specifics>

## CIL-01 Badge Counter Detail
- Lokasi: tab list `/Admin/ManageAssessment` + `/Admin/AssessmentMonitoring`
- Format: `<span class="badge bg-secondary ms-1">N Closed</span>` next to "Closed" status filter option
- Counter source: same query yang sudah dipakai untuk Open count (aggregate per status)
- Default filter: TETAP "Open" (no change)

## CIL-02 Search Aggregation Fix
- Bug current: search "Cilacap" Semua Status return 0 untuk Assessment Groups tab
- Root cause: query aggregation Title+Category+Schedule.Date hanya include Open status
- Fix: extend query include Closed status di aggregation (per todo 001)
- File: `Controllers/AssessmentAdminController.cs` ManageAssessmentTab_Assessment (L106)

## CIL-03 Drill-down Pattern (reuse Plan 337-02 CMP-19)
- `<tr>` add `data-href="/CMP/Results/{sessionId}" tabindex="0" role="link"`
- JS handler `addEventListener('click', ...)` + `addEventListener('keydown', Enter/Space)`
- Kolom Actions: `<a class="btn btn-sm btn-outline-primary" href="/CMP/Results/{sessionId}"><i class="bi bi-eye"></i> Lihat</a>`

## CIL-04 Banner Alert
- File: `Views/CMP/Assessment.cshtml` (sekitar L195 controller action)
- Condition: `if (User.IsInRole("Admin") || User.IsInRole("HC"))`
- Banner: `<div class="alert alert-info">Cari completed assessment lain? <a href="/Admin/ManageAssessment?tab=history">View admin assessment history</a></div>`
- Dismissable: ya, dengan close button (cookie remember dismissal)

## CIL-05 Excel Implementation
- File: `Helpers/ExcelExportHelper.cs` extend ExportAssessmentResults caller di `Controllers/AssessmentAdminController.cs` L4077
- Sheet 1 (existing): Summary peserta
- Sheet 2 (NEW): "Detail Per Soal" — query `PackageUserResponses` JOIN `PackageOptions` per session, grid format D-03
- Sheet 3 (NEW): "Elemen Teknis" — query `SessionElemenTeknisScores` per session, matrix format D-04
- ClosedXML library sudah dipakai (existing project package)

## CIL-06 BulkExportPdf Implementation
- New endpoint: `Controllers/AssessmentAdminController.cs` `BulkExportPdf(string title, string category, DateTime scheduleDate)`
- QuestPDF 2026.2.2 generate per-peserta PDF (~3-5 page each)
- ZIP via `System.IO.Compression.ZipArchive` (built-in .NET, no extra dep)
- Output: `Cilacap_PostTest_OJT_GAST_2026-05-20_Bundle.zip`
- Spider chart: render via Chart.js → server-side PNG via headless rendering ATAU client-side capture via Razor + JS (decide di Plan 04 research)

## REST-04 Restore Execute Implementation
- 13 peserta dari `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx`
- Manual entry per peserta via `AddManualAssessment` admin UI
- Atau: bulk SQL INSERT script (Plan 04 decide approach)
- Audit tag wajib: `ManualImport-Backfill`
- LinkedGroupId set untuk pair dengan PostTest counterpart (Cilacap 20 May)

## REST-05 Implementation Detail
- Template: `docs/templates/DB_HANDOFF_IT.template.md` markdown dengan placeholder {date}, {migration_list}, {affected_tables}, dll
- Renderer: Python/PowerShell script OR plain markdown→pandoc HTML
- Backup script: `scripts/backup-dev-pre-migration.ps1` accept connection string param + output `.bak` path
- Sample call: `.\scripts\backup-dev-pre-migration.ps1 -Server "10.55.3.3" -Database "HcPortalDB_Dev" -OutputPath "C:\Temp\HcPortalDB_Dev_pre_$(Get-Date -Format yyyyMMdd_HHmmss).bak"`

## REST-06 LinkedGroupId Auto-Pair
- File: `Controllers/AssessmentAdminController.cs` CreateAssessment POST handler (L815)
- Logic: kalau judul match pattern `{Stage} Test {Track} {Lokasi}`, parse {Track} + {Lokasi}, auto-suggest atau auto-assign `LinkedGroupId` ke session dengan judul kebalikan stage
- UI: form admin create show "Linked to: [auto-detected counterpart]" badge

## REST-07 DEV_WORKFLOW.md Update
- Section baru: "## Pre-Deploy Backup SOP" mention template + script + IT handoff flow
- Reference: link ke DB_HANDOFF_IT_*.html samples + backup script doc

</specifics>

<deferred>
- Banner CIL-04: per-user dismissal preference (cookie remember). Out of scope untuk Phase 338, tambah jika user request lanjutan.
- CIL-01 advanced: per-user setting filter default override (UserPreferences table). Defer ke backlog kalau user lama complain.
- REST-04 alternative: bulk SQL INSERT script vs manual UI per peserta. Defer ke Plan 04 research/decision.
- CIL-06 spider chart server-side: headless browser PNG rendering (Playwright) vs JS client-capture base64. Defer ke Plan 04 research.
- GitHub Actions auto-backup (REST-05 Option B): defer kalau user/IT prefer otomatisasi penuh nanti.
- CIL-01 implementation alternative `Both (badge + opt-in default)`: defer kalau user request user-level preference.
</deferred>

<threats>
- T-338-01 (CIL-05 Excel format breaking change): Tool IT external consume Excel format saat ini. Mitigation: sheet baru ADDITIVE (existing Summary sheet unchanged); sheet 2+3 baru ditambah, tidak modify struktur lama.
- T-338-02 (CIL-06 BulkExportPdf DoS): Generate 50+ PDF concurrent bisa memory spike. Mitigation: enforce max 50 peserta per batch + cancel token + memory profiling.
- T-338-03 (REST-04 audit trust): Insert "fake" row 30 Mar 2026 dengan AuditLog tag `ManualImport-Backfill`. Mitigation: tag prefix mandatory + reviewer note di DEV_WORKFLOW.md history section.
- T-338-04 (REST-05 backup script secret): Connection string + credential di script bisa leak via git. Mitigation: script accept param/env var, NEVER hardcode credential, .gitignore .env files.
- T-338-05 (CIL-01 user confusion): Badge counter visual cue user lama mungkin tidak notice. Mitigation: animate badge first-load + tooltip "X assessment Closed status".
- T-338-06 (REST-06 LinkedGroupId orphan): Auto-pair logic bug bisa link wrong counterpart. Mitigation: admin confirmation dialog SEBELUM auto-assign; manual override option.
</threats>

<open_questions>
- OQ-338-1: CIL-06 spider chart server-side rendering — Playwright headless PNG vs JS client-side base64 capture? **Decide di Plan 04 research** (Claude's discretion based on existing infra).
- OQ-338-2: REST-04 bulk insert mechanism — SQL script langsung vs loop AddManualAssessment endpoint? **Decide di Plan 04 research** (transactional safety, audit trail consistency).
- OQ-338-3: CIL-05 PackageUserResponses query optimization — JOIN-heavy query bisa N+1 untuk 50 peserta x 30 soal. **Decide di Plan 03 research** (EF Core single query vs batched).
</open_questions>

<next_steps>
1. **`/gsd-plan-phase 338`** — generate 5 plan file (Plan 01..05 per wave). Each plan should detail tasks, line numbers, threat mitigations, UAT criteria
2. Optional: spawn researcher untuk OQ-338-1/2/3 (spider chart rendering, bulk insert mechanism, query optimization)
3. After plan ready: `/gsd-execute-phase 338 --interactive` (proven pattern dari Phase 337)
</next_steps>
