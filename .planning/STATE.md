---
gsd_state_version: 1.0
milestone: v23.0
milestone_name: CMP/Records Search & Filter Consistency Audit
status: executing
last_updated: "2026-06-05T13:55:06.144Z"
last_activity: 2026-06-05
progress:
  total_phases: 2
  completed_phases: 1
  total_plans: 3
  completed_plans: 3
  percent: 50
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-05)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 350 ✅ SHIPPED LOCAL · Phase 351 CONTEXT ready (--auto) — next `/gsd-plan-phase 351`

## Current Position

Milestone: v23.0 🚧 ACTIVE 2026-06-05 — CMP/Records Search & Filter Consistency Audit (audit-driven)
Phase: 351 (CONTEXT @8cf1ba8e + UI-SPEC approved @b0ddf853; belum di-plan). Phase 350 ✅ COMPLETE (3/3). (parser "next 999.2" = backlog artifact, abaikan)
Plan: 351 belum di-plan (UI-SPEC done; NEXT /gsd-plan-phase 351 pilih "Research dulu")
Status: Phase 351 discuss DONE (--auto). SF-03 mirror My Records counter+empty-state · SF-04 Kategori opsi dari record aktual (bukan master, no GetUnifiedRecords change) · SF-05 +Kategori/Tipe ke My Records paritas · SF-07 sessionStorage-primary (planner verify restore precedence; fallback query-string round-trip). No migration. Phase 350 SHIPPED LOCAL (SF-01/02/06, 109/109 xUnit + Playwright 2 passed + review 0C/0W/2I + verifier 5/5; 2 HUMAN-UAT visual deferred). NOT PUSHED bundle v19-v23.
Last activity: 2026-06-05 -- /gsd-ui-phase 351: UI-SPEC approved @b0ddf853 (researcher @1aaf625a; checker 3 PASS/3 FLAG non-blocking APPROVED; 3 load-bearing traps documented: copy no-data-vs-no-match, Tipe value-map data-type=assessment, My Records rows need data-category)

Scope: fix 999.2 (Team View search "Keduanya" cakup judul assessment) + audit search/filter My Records + Team View + Worker Detail (scope per field, konsistensi cross-surface, edge case) → 7 confirmed gaps. 999.1 Realtime SignalR DROPPED.

Predecessor: v22.0 ✅ CLOSED 2026-06-05 (60/60 REQ, tag v22.0 lokal). Bundle v19-v22 NOT PUSHED pending IT.

## v23.0 Phase Map

| Phase | Goal | REQ | Sev | Depends on | UI hint |
|-------|------|-----|-----|-----------|---------|
| **350** Team View Search Scope + Export Parity | Search cakup judul Assessment (fix 999.2) + dropdown Lingkup jujur + export WYSIWYG; preserve REC-06 D-07 | SF-01, SF-02, SF-06 | HIGH+MED+MED | — (foundation predicate `GetWorkersInSection`) | yes |
| **351** Worker Detail + Cross-Surface Consistency | 0-match feedback+counter Worker Detail + Kategori match actual-records + paritas My Records↔Worker Detail + back-nav preserve param | SF-03, SF-04, SF-05, SF-07 | 3 MED-LOW | Phase 350 (file-overlap `WorkerDataService.cs`) | yes |

**Audit-informed shaping notes:**

- SF-01/02/06 cohere ke satu fase: semua menyentuh search predicate `WorkerDataService.GetWorkersInSection:402-417` + Team View surface `RecordsTeam.cshtml` + export `CMPController.ExportRecordsTeam*`. SF-06 (export parity) tergantung SF-01 (search assessment-title kembalikan worker benar).
- SF-03/04/05/07 cohere ke fase kedua: Worker Detail `RecordsWorkerDetail.cshtml` + My Records `Records.cshtml` + `GetUnifiedRecords`.
- Sequential strict: Phase 351 SF-04 sentuh `GetUnifiedRecords` di `WorkerDataService.cs` yang juga di-touch Phase 350 → hindari konflik write file.
- **Tests folded per phase** (bukan fase verify terpisah, scope kecil): audit menemukan `WorkerDataServiceSearchTests.cs` tak punya test assessment-title → Phase 350 wajib tambah (logic-bearing SF-01/06). Phase 351 tambah test Kategori actual-match (SF-04). Reuse pola v22: xUnit predicate-mirror + Playwright UAT.
- **No migration** (search/filter predicate + view + export saja).

## Next Action

1. **`/clear` lalu `/gsd-plan-phase 351`** — CONTEXT.md ready (@8cf1ba8e, D-01..D-04). Phase 351 Worker Detail + cross-surface (SF-03/04/05/07). SF-04 fix di controller+view (pakai `unifiedRecords` yg sudah di-return) → overlap `WorkerDataService.cs` dgn 350 MINIM (revisi asumsi STATE awal). NO migration. UI hint=yes (counter/empty-state/dropdown — pertimbangkan `/gsd-ui-phase 351` bila mau kontrak visual).
2. **(Opsional) `/gsd-verify-work 350`** — tutup 2 HUMAN-UAT item visual (XLSX content Category drop-archived + badge unchanged) saat dev/IT sempat eyeball; non-blocking, sudah code+automated verified.
3. **Carry-over IT promo v19.0+v20.0+v21.0+v22.0+v23.0/350** — push bundle ~130+ commit lokal + Dev migration coordination tetap pending (paralel jalur; Phase 350 flag migration = false).
4. **(Backlog housekeeping non-blocker):** v16.0+v17.0+v18.0 MILESTONES.md entries belum ditambah (defer batch retro). Pre-existing Tom Select UX regression dari v20.0 audit defer.

## Deferred Items

### v15.0 Deferred (carry-over)

| REQ | Item | Status | Due |
|-----|------|--------|-----|
| EPRV-01 | Preview Essay rubrik/jawaban — Jalur A (label) vs Jalur B (field baru) | menunggu user verifikasi save/load Rubrik | 2026-05-12 |

### Carry-over dari v14.0 close (2026-04-24)

| Category | Item | Status | Source |
|----------|------|--------|--------|
| UAT | Phase 303 Plan 02 Task 3 — Coach Workload 12-langkah human verification | paused-at-checkpoint | HANDOFF.json (2026-04-10) |
| UAT | Phase 235 — 5 items butuh human verification via browser | pending | STATE.md (prior) |
| UAT | Phase 247 approval chain — 2 TODO (HC review + resubmit notification) | pending | STATE.md (prior) |
| Research gap | Phase 297 Pre-Post Renewal behavior — keputusan 2 sesi baru otomatis | undecided | v14.0 planning |
| Research gap | Phase 298 essay max character limit — nvarchar(max) vs nvarchar(2000) | undecided | v14.0 planning |
| Blocker | Phase 293 `GetSectionUnitsDictAsync` Level 2+ support | undecided | v13.0 carry-over |
| v11.2 paused | Phase 281 (System Settings) + Phase 285 (Dedicated Impersonation Page) | paused | MILESTONES.md v11.2 |

### v22.0 Tech Debt (acknowledged at close 2026-06-05)

| Item | Status |
|------|--------|
| Push batch v19+v20+v21+v22 (~127 commit leg) pending IT availability | pending |
| CMP06R-03 PDF env-blocked lokal (QuestPDF 204, Phase 327 known) — code-verified | needs Dev/Prod render-confirm |
| 348/349 tanpa VERIFICATION.md (human-verify + UAT substantif) | accepted |

### Dropped (v23.0 scope decision)

| Item | Reason | Date |
|------|--------|------|
| Phase 999.1 Realtime Assessment SignalR | Tidak diprioritaskan user | 2026-06-05 |
| Phase 999.2 Team View search → Assessment title | PROMOTED → v23.0 Phase 350 (SF-01/02/06) | 2026-06-05 |

## Quick Tasks Completed

| Date | Slug | Description |
|------|------|-------------|
| 2026-05-26 | cdp-portal-platform-rename | Rename CDP label "Competency Development Portal" → "Platform" (parity dgn CMP). 4 edit di Views/CDP/Index.cshtml + Views/Home/Index.cshtml. |

## Accumulated Context

### Decisions (persist across milestones)

- [v23.0 / Phase 350]: **REC-06 D-07 invariant LOCKED** — SF-01 search assessment-title HARUS filter di level worker (post-load), badge/count per-worker tetap utuh. Pola sama dgn Category filter `WorkerDataService.cs:373-381` yang sudah union `AssessmentSessions.Any(a => a.Category ...)` — terapkan pola identik untuk `a.Title`. JANGAN ubah ke all-SQL pre-narrow (asimetri Nama-via-SQL vs Training/Assessment-via-post-load disengaja per D-07/audit §3.D).
- [v23.0 / Phase 350]: Export Team View = WYSIWYG terhadap filter aktif (snapshot saat klik, `RecordsTeam.cshtml:329-346` updateExportLinks) — SF-06 hanya butuh propagasi fix SF-01; assessment row tak punya kolom Category row-level (audit by-design #B), keputusan simetri Category di-dokumentasikan saat plan.
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` ("Menunggu Penilaian") = single source of truth label lintas 11+ surface; exclude-pending denominator konsisten 3 jalur.
- [v14.0 / Phase 296]: GradeFromSavedAnswers dihapus — GradingService satu-satunya source of truth grading
- [v14.0 / Phase 301]: Export endpoints re-query database independen (tidak share state dengan API endpoints)
- [v13.0]: SortableJS 1.15.7 via CDN; drag-drop sibling-only (group: false); orgTree.js single JS orchestrator
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route] attribute
- [v15.0 / Phase 307]: Selectors helper di `tests/e2e/helpers/wizardSelectors.ts` (NEW folder) untuk separation e2e-specific selectors vs shared utilities
- [v21.0]: Configurable display labels via cached `IOrgLabelService` + global `@inject` di `_ViewImports.cshtml` (real-time propagation via cache-invalidate-on-mutation)

### Open Blockers/Concerns

- Phase 293 `GetSectionUnitsDictAsync` — hardcoded 2-level, unit Level 2+ tidak muncul di dropdown ManageWorkers secara diam-diam (keputusan masih tertunda)
- SF-04 (Phase 351) gray-area: perlu audit saat plan apakah nilai `Kategori` record dijamin = master `AssessmentCategories` name. Jika YA → severity turun ke LOW (hanya dead-option cleanup); jika TIDAK (free-text/legacy ada) → butuh build opsi dari distinct `unifiedRecords`. Audit `RecordsWorkerDetail.cshtml:352-353` + opsi `:140-148`.

### Roadmap Evolution

- v23.0 added (2026-06-05): CMP/Records Search & Filter Consistency Audit — 2 phase 350-351 dari audit 3-surface (`docs/superpowers/specs/2026-06-05-cmp-records-search-filter-audit.md`, 7 confirmed [1 HIGH/4 MED/2 LOW]). Phase 350 = Team View server-side search scope + export parity (SF-01/02/06, fix 999.2, preserve REC-06 D-07, foundation predicate). Phase 351 = Worker Detail + cross-surface filter consistency (SF-03/04/05/07). Sequential strict (file-overlap `WorkerDataService.cs`). Tests folded per phase (xUnit predicate-mirror + Playwright UAT reuse v22). No migration. Backlog Phase 999.2 promoted → Phase 350; 999.1 SignalR dropped.

## Session Continuity

Last activity: 2026-06-05 — ROADMAP v23.0 dibuat (roadmapper). 2 phase derived dari 7 audit findings:

- **Phase 350** (Team View): SF-01 HIGH (search predicate `GetWorkersInSection:402-417` tambah `AssessmentSessions.Any(a=>a.Title contains)`) + SF-02 MED (dropdown Lingkup + placeholder jujur `RecordsTeam.cshtml:92-105`) + SF-06 MED (export parity `CMPController.cs:669-680/721-732`). Cohesion: sama search predicate + Team View surface + export. Preserve REC-06 D-07 (worker-level post-load filter, badge count utuh).
- **Phase 351** (Worker Detail + cross-surface): SF-03 MED (0-match message + counter `RecordsWorkerDetail.cshtml:336-358`, reuse My Records pola) + SF-04 MED (filter Kategori match actual-records `:352-353`, build opsi dari distinct unifiedRecords) + SF-05 LOW (paritas filter My Records `Records.cshtml:54-93` vs Worker Detail `:128-181`) + SF-07 LOW (back-nav preserve subCategory/dateFrom/dateTo/searchScope `:27-47`).

Files written: ROADMAP.md (v23.0 block appended — milestone list line + Phase Details 350/351 + Progress Table + Coverage Validation + footer log; backlog 999.2 marked PROMOTED; existing milestone history + Backlog section preserved), REQUIREMENTS.md (traceability 7/7 mapped), STATE.md (this file).

Next action: `/gsd-plan-phase 350` — spec audit jadi input CONTEXT.md; effort S (SF-01/02) + M (SF-06 keputusan simetri export). Verifikasi lokal `dotnet build` + `dotnet run` localhost:5277 + Playwright per CLAUDE.md Develop Workflow sebelum commit.
