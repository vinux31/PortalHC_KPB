# Phase 348: manageassessment-monitoring-med-fix - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Tutup **13 bug MED correctness** (MAM-01..13) di halaman **ManageAssessment** (tab Assessment Groups / Input Records / History) + **Assessment Monitoring** (list + detail), hasil audit eksklusif 2026-06-04 (59-agent Workflow, 6×5-lensa + adversarial → 44 confirmed: 0 HIGH/15 MED/29 LOW, 9 FP).

Tema: (A) konsistensi Pre-Post group (token/link/badge), (B) essay PendingGrading status di Monitoring surface, (C) Tab2 Input Records struktural (empty-state/pagination/delete-filter/status-filter), (D) status-badge match filter, (E) Monitoring list filter data-driven, (F) Monitoring detail reshuffle selector.

**NO behavior-expansion, NO migration, NO schema change.** 29 LOW polish = Phase 349 (terpisah, sequential setelah 348). Pure correctness/bug-fix.

Files tersentuh: `Views/Admin/ManageAssessment.cshtml`, `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` + `_TrainingRecordsTab.cshtml` + `_HistoryTab.cshtml`, `Views/Admin/AssessmentMonitoring.cshtml` + `AssessmentMonitoringDetail.cshtml`, `Controllers/AssessmentAdminController.cs`, `Controllers/CMPController.cs` (SignalR), `Services/WorkerDataService.cs`.
</domain>

<decisions>
## Implementation Decisions

### Terkunci dari spec (user APPROVED 2026-06-04) — JANGAN dilitigasi ulang
Spec `docs/superpowers/specs/2026-06-04-manageassessment-monitoring-audit-design.md` §"Phase 348" (baris 44-126) = tabel MAM-01..13 dengan file:line + fix + pitfall exact.
- **D-A:** No migration, no schema change (spec D-05).
- **D-B:** Sequential strict v22.0 345→346→347→**348**→349. Sentuh file sama dgn 349 → JANGAN paralel.
- **D-C:** Pakai konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` + label "Menunggu Penilaian" (mapan dari Phase 345) — **JANGAN literal string**. Berlaku MAM-04/MAM-05.
- **D-D:** M4 (Tab3 History PendingGrading, `WorkerDataService.cs:134`) **BUKAN REQ 348** — sudah dicakup REC-07 Phase 346. JANGAN duplikat. (Optional badge → MAP-20/349.)

### 8 MAM fix-tunggal terkunci (tak ada gray area — implement per spec verbatim)
- **MAM-01:** RegenerateToken match by `LinkedGroupId` untuk Pre-Post (`AssessmentAdminController.cs:2616-2621`). PostTest bisa beda tanggal.
- **MAM-03:** Set `MenungguPenilaianCount = postSubs.Count(a => a.IsMenungguPenilaian)` untuk prePostGroups (`:2749-2796`), parity standardGroups `:2825`.
- **MAM-04:** Status derivation Detail cek `if (a.Status == PendingGrading)` SEBELUM `CompletedAt != null` (`:3229-3239`); `CompletedCount` exclude ungraded essay.
- **MAM-05:** SignalR `workerSubmitted` (`CMPController.cs:1767-1770`) branch bila graded-state = PendingGrading → push `status="Menunggu Penilaian"`, `result="—"`. Update handler `AssessmentMonitoringDetail.cshtml:1336-1362`. **PITFALL:** `hasEssay` lokal di GradingService → service harus surface state pending ke caller. *(Event name workerPendingGrading vs reuse workerSubmitted = Claude's discretion planner; minimal-risk = reuse workerSubmitted dgn status override.)*
- **MAM-06:** `isInitialState` diturunkan dari absennya filter (`:251`), bukan hardcode false. Skip full-roster query sampai admin filter. **Verifikasi parity Phase 287 `fc161a18` + cek 322-UAT.md jangan break.**
- **MAM-10:** Badge Tab1 bind `@group.GroupStatus` (bukan rep `@group.Status`) (`_AssessmentGroupsTab.cshtml:195-221`); drop arm Completed/InProgress/Abandoned, pertahankan Open/Upcoming/Closed, **tambah case "Closed"**.
- **MAM-11:** Dropdown Kategori Monitoring data-driven dari `_context.AssessmentCategories.Where(c=>c.IsActive).OrderBy(c=>c.SortOrder)` via ViewBag (pola CreateAssessment L312/345/355); buang "Proton" phantom (`AssessmentMonitoring.cshtml:125-148`).
- **MAM-13:** Selector tombol Reshuffle scope ke tombol (class distinct / `[data-reshuffle-session-id]` atau `reshuffleWorker(this)`), jangan bentrok `<tr data-session-id>` (`AssessmentMonitoringDetail.cshtml:739-743`).

### 4 gray-area RESOLVED (user "Terima semua 4 reko" 2026-06-04)
- **D-01 (MAM-02) Pre-Post Monitoring/Export link = route-by-LinkedGroupId.** Prinsip inti: **JANGAN filter Pre-Post by single `scheduleDate`** di endpoint mana pun (`AssessmentMonitoringDetail:3165`, `ExportAssessmentResults:4120`, `BulkExportPdf:4503`). Implementasi: (1) **Monitoring detail link → pecah per-half** (Pre/Post terpisah), **reuse pola existing** `AssessmentMonitoring.cshtml:337-383` (preDetailUrl/postDetailUrl + `assessmentType=PreTest/PostTest`); (2) **Export Excel + Bulk PDF → LinkedGroupId-aware**, ekspor **KEDUA half** (jangan silently miss PostTest). Berlaku row `IsPrePostGroup` di `_AssessmentGroupsTab.cshtml:261-285`. Koord MAP-17/349.
- **D-02 (MAM-07) Tab2 pagination = (A) pagination ASLI.** Tambah Skip/Take di `GetWorkersInSection` (`WorkerDataService.cs:242` — saat ini TANPA Skip/Take) + render kontrol pagination tiru Tab1 (`_AssessmentGroupsTab.cshtml:348-427`). Alasan: section refinery bisa ratusan worker; MAM-06 sudah gate query (load cuma pasca-filter) → pagination = robust+jujur. BUKAN drop-param.
- **D-03 (MAM-08) Delete filter-preservation = (A) hx-post re-swap.** Konversi delete Training/ManualAsm (`_TrainingRecordsTab.cshtml:327-349`) dari full-page form POST → `hx-post` yang re-swap wrapper tab, preserve filter via `hx-include` filter form. Fits arsitektur HTMX (311/322). **Koord MAM-06:** pasca-delete re-swap, `isInitialState` HARUS tetap false (filter aktif) — jangan balik ke empty-state. Handler `TrainingAdminController.cs` DeleteTraining `:586/619` + DeleteManualAssessment `:985/1016`.
- **D-04 (MAM-09) Tab2 Status filter = (A) relabel "Status Training" saja.** Relabel kontrol filter `_TrainingRecordsTab.cshtml:107-125` → "Status Training" (jujur: dipetakan ke training `CompletionPercentage`). **JANGAN** fold passed manual-assessment ke Sudah/Belum (ubah `WorkerDataService.cs:360-397` logic + semantik ambigu) — defer ke deferred/backlog. Koord MAP-19/349 (badge "Status Training").

### Claude's Discretion (planner refine)
- MAM-05 event shape: reuse `workerSubmitted` dgn status override (minimal-risk) vs event baru `workerPendingGrading`. Default minimal-risk.
- MAM-02 exact button layout Tab1 row (hindari clutter 6 tombol) — planner putuskan; prinsip no-single-date-filter wajib.
- Plan split (mungkin per-tema A/B/C/D/E/F atau per-file). Planner putuskan; M1/M5 sentuh shared grading+token+SignalR layak diisolasi.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & Requirements & Roadmap
- `docs/superpowers/specs/2026-06-04-manageassessment-monitoring-audit-design.md` — **spec utama** §"Phase 348" (baris 44-126): tabel MAM-01..13 file:line + fix + pitfall + §"Pitfalls" (184-192) + §"Dependensi" (176-182).
- `.planning/REQUIREMENTS.md` — MAM-01..13 (Phase 348 REQ).
- `.planning/ROADMAP.md` — §Phase 348 (goal, SC, files affected).

### Dependency (sequential predecessors)
- `.planning/phases/345-assessment-pending-grade-display-fix/345-CONTEXT.md` — konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` + label "Menunggu Penilaian" + passRate exclude-pending (D-C koordinasi).
- `.planning/phases/346-cmp-records-detail-search-logic/346-VERIFICATION.md` — REC-07 (Tab3 History PendingGrading, M4 dedup) sudah fix; 348 TIDAK duplikat (D-D).
- `.planning/phases/347-cmp-records-i18n-a11y-polish/347-CONTEXT.md` — pola i18n "Menunggu Penilaian" (label mapan; 348 Monitoring net-new surface, beda dari POL-* CMP/Records).

### Kode wajib (anchor verified 2026-06-04)
- `Views/Admin/AssessmentMonitoring.cshtml:337-383` — pola preDetailUrl/postDetailUrl split per-half (`assessmentType=PreTest/PostTest`) → reuse MAM-02 (D-01).
- `Services/WorkerDataService.cs:242` — `GetWorkersInSection` (TANPA Skip/Take saat ini) → MAM-07 (D-02).
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:348-427` — pola kontrol pagination Tab1 → reuse MAM-07.
- `Controllers/AssessmentAdminController.cs` — tab actions L106/245/284; AssessmentMonitoring L2670; Detail L3163/3229-3239 (MAM-04); RegenerateToken L2616 (MAM-01); prePostGroups L2749-2796 (MAM-03); GroupStatus L167/177-180/188 (MAM-10).
- `Controllers/CMPController.cs:1767-1770` — SignalR `workerSubmitted` (MAM-05).
- `.planning/phases/*322*/322-UAT.md` — full-roster-on-load UAT (MAM-06 pitfall: jangan break).

### Konstanta
- `AssessmentConstants.AssessmentStatus.PendingGrading` — pakai konstanta, BUKAN literal "Menunggu Penilaian" (MAM-04/05).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **preDetailUrl/postDetailUrl split** (`AssessmentMonitoring.cshtml:337-383`) — pola per-half Pre/Post sudah jalan di Monitoring list; reuse untuk MAM-02 Tab1 link.
- **Tab1 pagination markup** (`_AssessmentGroupsTab.cshtml:348-427`) — tiru untuk MAM-07 Tab2.
- **HTMX wrapper re-swap + hx-include** (arsitektur ManageAssessment 3-tab, Phase 311/322) — pakai untuk MAM-08 delete.
- **`PendingGrading` konstanta + "Menunggu Penilaian" label** (Phase 345) — MAM-04/05 status derivation.
- **CreateAssessment ViewBag categories pattern** (`AssessmentAdminController.cs:312/345/355`) — MAM-11 dropdown data-driven.

### Established Patterns
- ManageAssessment = HTMX host 3 tab; History tab = JS client-filter. Delete saat ini full-page POST (anti-pattern → MAM-08).
- `GroupStatus` derived (`:177-180`) sudah dipakai filter/stats/hide-Closed, tapi badge masih pakai rep `Status` (divergence → MAM-10).
- Pre-Post = `LinkedGroupId`; PostTest schedule bisa > PreTest (beda tanggal NORMAL — validasi cuma enforce PostSchedule > PreSchedule).

### Integration Points
- MAM-05 SignalR: GradingService harus surface state PendingGrading ke CMPController caller (`hasEssay` lokal saat ini).
- MAM-02/MAM-17: Export/PDF/Monitoring endpoints (Detail:3165, Export:4120, PDF:4503) harus LinkedGroupId-aware, bukan single-date.
- MAM-08 koord MAM-06: delete re-swap harus jaga isInitialState=false.
</code_context>

<specifics>
## Specific Ideas

- **MAM-02 prinsip:** "JANGAN filter Pre-Post by single scheduleDate" — ini akar 3 endpoint silently-miss-PostTest. Monitoring link reuse split pattern; Export/PDF both-half.
- **MAM-06 pitfall kritis:** fix kembalikan empty-state "Pilih filter" → koordinasi dgn Phase 322 UAT yang dulu PASS dgn full-roster-on-load (FP-rejected "Reset full-roster by design"). Cek 322-UAT.md sebelum execute.
- **MAM-10:** GroupStatus values = Open/Upcoming/Closed (BUKAN Completed/InProgress/Abandoned). Tambah case "Closed" (saat ini fallthrough bg-secondary).
- Dev creds UAT: admin `admin@pertamina.com` pwd `123456`, DB `HcPortalDB_Dev`. App run WAJIB `ASPNETCORE_ENVIRONMENT=Development` (tanpa env → connection string placeholder Production). Local `http://localhost:5277`.
</specifics>

<deferred>
## Deferred Ideas

- **MAM-09 combined-status semantics** (fold passed manual-assessment ke Sudah/Belum, ubah WorkerDataService logic) — deferred; MED-fix cukup relabel "Status Training". Pertimbangkan jadi REQ tersendiri bila admin minta filter status gabungan training+assessment.
- **MAM-12 extend search ke Kota** (`AssessmentAdminController.cs:2685-2688`) — out of scope MED; spec MAM-12 = minimal (buang "lokasi" dari tooltip). Extend search-scope → MAP-23/349 (opsional) atau backlog.
- **29 LOW polish (MAP-01..23)** — Phase 349 (i18n Monitoring, a11y, empty-state, code-hygiene). Sequential setelah 348.

### Reviewed Todos (not folded)
Tidak ada — `todo match-phase 348` = 0 match.
</deferred>

---

*Phase: 348-manageassessment-monitoring-med-fix*
*Context gathered: 2026-06-04*
