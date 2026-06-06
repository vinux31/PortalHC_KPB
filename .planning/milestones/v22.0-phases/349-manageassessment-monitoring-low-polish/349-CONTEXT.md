# Phase 349: ManageAssessment + Monitoring LOW Polish - Context

**Gathered:** 2026-06-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Tutup **29 LOW polish** (23 REQ bucket MAP-01..23) di **ManageAssessment** (Tab Assessment Groups / Input Records / History) + **Assessment Monitoring** (list + detail). Kategori: i18n Bahasa Indonesia, a11y (aria/chevron), empty-state/feedback copy, display nits, code-hygiene (magic-number, dead param, dead var).

**NO logic-bearing change, NO migration, NO schema change, NO behavior-expansion.** Pure polish. Final phase v22.0 (setelah ditutup → close milestone). Depends Phase 348 (sequential — sentuh file yang sama: `_AssessmentGroupsTab.cshtml`, `AssessmentMonitoring*.cshtml`, `ManageAssessment.cshtml`, `_HistoryTab.cshtml`, `_TrainingRecordsTab.cshtml`, `AssessmentAdminController.cs`).
</domain>

<decisions>
## Implementation Decisions

### Carry-forward dari Phase 348 (locked, tidak re-discuss)
- **D-A:** No migration, no schema change.
- **D-B:** Sequential strict — Phase 349 setelah 348 selesai (348 COMPLETE 2026-06-05). File overlap dengan 348 → jangan paralel; basis pada kode pasca-348.
- **D-C:** Pakai konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` + label "Menunggu Penilaian" (mapan Phase 345), BUKAN literal string. Berlaku MAP-18, MAP-20.
- **D-D:** M4 (Tab3 History PendingGrading) sudah dicakup REC-07/346 — MAP-20 = badge tambahan opsional, jangan duplikat logic.

### Gray-area resolved (user pilih semua rekomendasi 2026-06-05)
- **D-01 (MAP-06):** Tombol clear filter Tab1 → **relabel "Reset Semua Filter"** yang hapus SEMUA (search + kategori + status) sekaligus. BUKAN clear-search-only. Alasan: jujur + simpel, hindari kebingungan user. (`_AssessmentGroupsTab.cshtml:143-146`)
- **D-02 (MAP-05/07/08):** Empty-state/feedback **full**:
  - MAP-05: Tab1 empty-state filter-aware — bila kategori/status aktif tapi search kosong → "Tidak ada assessment untuk filter ini" + Reset (bukan "Buat assessment pertama").
  - MAP-07: Tab3 client-filter 0-match → inject baris "Tidak ada hasil untuk filter ini." + `aria-live="polite"` (assessment & training).
  - MAP-08: Tab3 tambah baris **"Menampilkan X dari Y"** (visible-row count ikut filter aktif).
- **D-03 (MAP-10/12):** Monitoring Detail summary cards **lengkap**:
  - MAP-10: tambah kartu **"Abandoned"** supaya Total = jumlah semua kartu; sync JS `updateSummaryFromDOM` (L1280-1298).
  - MAP-12: tombol **"Akhiri Semua Ujian" kondisional** — render hanya bila `@Model.InProgressCount > 0 || Model.GroupStatus == "Open"`; modal wording "X belum mulai" pakai predikat identik dgn aksi cancel.
- **D-04 (MAP-17):** Monitoring list grup Pre-Post token-required → **tambah "Regenerate Token"** (target `LinkedGroupId`, koordinasi dengan fix MAM-01 Phase 348 yang sudah route-by-LinkedGroupId). BUKAN minimal "View Detail" saja.

### Claude's Discretion (mekanis / spec-decided — planner ikuti spec §Phase 349)
- **MAP-01** i18n Monitoring Detail: header tabel + kartu + "Back to Monitoring" → Indonesia (spec list verbatim).
- **MAP-02** standar label identitas → **"NIP"** (History Assessment + Training, header + placeholder).
- **MAP-03** chevron + `aria-label` deskriptif toggle collapse Tab1 "N peserta" + Tab2 expand-records (chevron rotate via CSS `[aria-expanded="true"]`).
- **MAP-04** Tab3 drill-down: drop ARIA nested-interactive — **simpan tombol "Lihat", drop `role/tabindex` pada `<tr>`** (rekomendasi spec).
- **MAP-09** skeleton loader match konten asli (Tab2 5 filter/7 kolom; History 8/5 kolom).
- **MAP-11** "In Progress" card pakai `@Model.InProgressCount` (bukan inline LINQ); **drop dead var `completedPct`/`passRatePct`** (pilih drop, bukan surface — minimal-risk, hindari nambah display baru).
- **MAP-13** Monitor list `TotalCount` exclude Cancelled (`g.Count(a => a.Status != "Cancelled")`) → progress bar bisa 100%; parity Pre-Post path L2757.
- **MAP-14** Monitor list subtitle buang "real-time" (list tak ada SignalR).
- **MAP-15** Monitor list dropdown Status saat search jangan misrepresent (set "Semua Status" / `ViewBag.SelectedStatus="All"` bila search broaden scope).
- **MAP-16** Monitor list buang kategori dobel (muted subtitle vs badge).
- **MAP-18** Tab2 Detail manual-assessment tri-state `IsPassed==null → "Menunggu Penilaian"` (konstanta D-C, defensive).
- **MAP-19** Tab2 "Status Training" badge → **selalu render `CompletionDisplayText`** (pilih konsisten, bukan gated "Belum ada").
- **MAP-20** (koord MAM-04) Tab3 History cell tambah badge "Menunggu Penilaian" saat `IsPassed==null && Status==PendingGrading` (konstanta D-C; jangan duplikat REC-07 logic).
- **MAP-21** pagination Tab1 magic-number `20` → expose `paging.Take` via ViewBag (rowNum + "Menampilkan X-Y" + hidden input).
- **MAP-22** drop param mati `pageSize`/`statusFilter` (History) + `pageSize` (Training) + no-op `page` wiring (pilih **drop**, bukan dokumentasi-intent).
- **MAP-23** extend search Monitoring list ke **Category** (Nama/NIP TIDAK — list aggregate); update placeholder.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec utama
- `docs/superpowers/specs/2026-06-04-manageassessment-monitoring-audit-design.md` §"Phase 349" — tabel MAP-01..23 dengan file:line + fix exact per item (6 grup: i18n/a11y/empty-state/display-nits/code-hygiene). Setiap MAP punya target `file:line`.

### Dependency (carry-forward decisions)
- `.planning/phases/348-manageassessment-monitoring-med-fix/348-CONTEXT.md` — D-A/D-B/D-C/D-D + konstanta PendingGrading + 4 gray-area 348 (MAM-01 LinkedGroupId untuk koord MAP-17).
- `.planning/phases/345-assessment-pending-grade-display-fix/345-CONTEXT.md` — passRate exclude-pending + label "Menunggu Penilaian" (koord MAP-11/18/20).

### Requirements
- `.planning/REQUIREMENTS.md` — MAP-01..23 (Phase 349 REQ).
- `.planning/ROADMAP.md` — §Phase 349 (goal + 5 Success Criteria + files affected).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets (dari Phase 348, sudah di kode)
- **Pagination markup HTMX** (`_AssessmentGroupsTab.cshtml` footer + `_TrainingRecordsTab.cshtml` footer Phase 348) — pola First/Prev/PageNumbers/Next/Last + `hx-include` filter form. MAP-21 reuse ViewBag `paging.Take`.
- **`DeriveUserStatus` helper** (`AssessmentAdminController.cs`, Phase 348 MAM-04) — derivasi status; MAP-18/20 bisa konsisten dengan label "Menunggu Penilaian".
- **HX-Trigger re-fetch pattern** (Phase 348 MAM-08) — bila MAP butuh re-swap.
- **`AssessmentConstants.AssessmentStatus.PendingGrading`** konstanta (D-C).
- **`updateSummaryFromDOM` JS** (`AssessmentMonitoringDetail.cshtml:1280-1298`) — MAP-10 sync Abandoned card.

### Established Patterns
- i18n: copy Bahasa Indonesia langsung di Razor (bukan resource file) — konsisten codebase.
- a11y: `aria-label` + CSS `[aria-expanded]` rotate (pola existing badge/tooltip).
- Empty-state: card `text-center py-5 text-muted` (pola `_TrainingRecordsTab.cshtml` empty-state).

### Integration Points
- File overlap Phase 348 — basis kode pasca-348 (348 COMPLETE). Edit incremental di view + controller existing, no new endpoint.
</code_context>

<specifics>
## Specific Ideas

- User non-teknis, prefer simpel/jujur (D-01 "Reset Semua Filter" dipilih atas clear-search-only justru karena lebih tak bikin bingung).
- MAP-17 Regenerate Token dipilih lengkap karena fix MAM-01 (route-by-LinkedGroupId) Phase 348 sudah benar — tinggal expose di list.
- Semua 23 MAP dalam scope (D-02 audit: semua 29 LOW ditangani). Tidak ada MAP yang di-drop.
</specifics>

<deferred>
## Deferred Ideas

- Extend search Monitoring list ke Nama/NIP per-user — **OUT OF SCOPE** (MAP-23 catatan: list Monitoring aggregate, tak render per-user; cuma Category yang ditambah).
- Resource-file i18n (proper localization framework) — out of scope, codebase pakai inline Indonesia.

None lain — diskusi tetap dalam scope phase (pure polish).
</deferred>

---

*Phase: 349-manageassessment-monitoring-low-polish*
*Context gathered: 2026-06-05*
