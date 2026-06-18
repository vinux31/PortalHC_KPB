# Phase 389: CoachCoacheeMapping Redesign — Accordion Card per Coach (DSN-01 + DSN-02 + DSN-03) - Context

**Gathered:** 2026-06-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Redesign **1 file**: `Views/Admin/CoachCoacheeMapping.cshtml` (1059 baris). Ganti tabel grouped telanjang
(satu `<table>` dengan baris `table-primary` per coach + `<tbody class="collapse show">` coachee) menjadi
**accordion card per coach**: tiap coach = 1 card; header card = avatar inisial + nama + section + badge beban;
klik header buka/tutup tabel coachee mini di dalam card.

Cakupan: DSN-01 (card + header), DSN-02 (collapse + tabel mini), DSN-03 (toolbar seragam + hapus dead onclick).
**RISK TERTINGGI milestone** = behavior regression (modal/AJAX/collapse/threshold wiring kompleks).

**Murni view edit (`.cshtml` + `@@section Scripts` inline).** 0 backend, 0 controller, 0 endpoint, 0 JS-contract,
0 migration. Semua aksi existing WAJIB jalan byte-for-byte (parity) — diverifikasi tuntas di Phase 390.

OUT: ubah kolom/fungsi baru, ubah controller `CoachMapping`/`Admin` atau endpoint AJAX, redesign CoachWorkload
(Phase 388), sentuh file view lain.
</domain>

<decisions>
## Implementation Decisions

### DSN-01 — Accordion card per coach + header
- **D-01:** Tiap coach group (`groupedCoaches`) di-render sebagai **1 card** `card border-0 shadow-sm mb-3`
  (idiom app, konsisten dgn import-results card L132 & 388 polish). BUKAN `<table>` grouped lagi.
- **D-02:** Header card = `card-header` clickable berisi (kiri→kanan): **avatar inisial** + **nama coach** +
  **section** (muted, mis. `— @@group.CoachSection`) + **badge jumlah coachee aktif** (kanan).
- **D-03 (avatar):** Reuse idiom `Views/Admin/ManageWorkers.cshtml:251-254` verbatim —
  `<div class="avatar-initial rounded-circle bg-primary text-white d-flex align-items-center justify-content-center fw-bold" style="width:36px;height:36px;font-size:0.8rem">` isi `CoachName.Substring(0,1).ToUpper()`
  (fallback `"?"` bila kosong). **Warna = bg-primary NETRAL** (bukan ikut beban / section) — badge sudah membawa
  warna beban, hindari double-encode.
- **D-04 (badge beban — PARITY KRITIS):** Pertahankan logika threshold existing PERSIS:
  `activeCount >= 8 ? "bg-danger" : activeCount >= 5 ? "bg-warning text-dark" : "bg-info text-dark"`
  (L260-264). Hardcoded 5/8 — JANGAN tautkan ke threshold konfigurabel CoachWorkload (roadmap kunci ke "logika
  badge ActiveCount saat ini"). Badge isi `@@group.ActiveCount`.

### DSN-02 — Collapse + tabel coachee mini
- **D-05 (default state):** **TERTUTUP semua**, **card independen** (multiple boleh terbuka bersamaan).
  Pakai Bootstrap `collapse` manual (header `data-bs-toggle="collapse" data-bs-target="#collapse-@@idx"`) —
  **BUKAN** komponen `.accordion` ber-`data-bs-parent` (yang memaksa single-open). Body collapse = `collapse`
  (TANPA `show`). Ini perubahan disengaja dari `collapse show` sekarang (rapi untuk scan banyak coach; badge
  beban tetap tampak di header tertutup). Chevron header refleksikan state (mis. `bi-chevron-down`, rotate saat buka).
- **D-06 (tabel mini):** **Tabel penuh + `table-responsive`** (scroll-x horizontal di layar sempit) — parity,
  risiko terendah. Kolom existing utuh **9**: Nama, NIP, @@OrgLabels.GetLabel(0) Penugasan, @@OrgLabels.GetLabel(1)
  Penugasan, Jabatan, Proton Track, Status, Mulai, Aksi.
- **D-07 (drop kolom redundan):** Kolom **"Coachee Aktif"** lama (header tabel L241 + sel kosong L300) **DILEPAS**
  dari tabel mini — nilainya (badge ActiveCount) sudah pindah ke header card (D-02/D-04). Roadmap SC#2 mendaftar 9
  kolom TANPA "Coachee Aktif" → konsisten.
- **D-08 (PARITY KRITIS — hook delete):** Tiap baris coachee WAJIB tetap `<tr data-mapping-id="@@coachee.Id">`.
  `submitDelete()` cari `document.querySelector('tr[data-mapping-id="..."]').remove()` (L973-974) — selector ini
  HARUS tetap valid di markup baru. Baris non-aktif tetap `table-light text-muted` (L273).
- **D-09 (PARITY KRITIS — kolom Aksi):** Pertahankan SELURUH blok Aksi (L301-341) verbatim: tombol Edit
  (`openEditModal(...)` 7 arg), badge "Graduated" (cek `IsCompleted` DULU), Nonaktifkan (`confirmDeactivate`),
  form `MarkMappingCompleted` (POST + AntiForgeryToken + confirm), Aktifkan (`reactivateMapping`), Hapus
  (`confirmDelete`). Urutan if/else `IsCompleted → IsActive → else` JANGAN diubah (Phase 356 D-06 lesson).

### DSN-03 — Toolbar seragam + dead-code
- **D-10:** Toolbar header (L48-61): **normalisasi ukuran/gaya** semua tombol seragam (`btn-sm`); **kelompokkan
  Excel** (Download Template / Import Excel / Export Excel) secara visual (mis. `btn-group` atau segmen rapat,
  gaya outline konsisten); **"Tambah Mapping" jadi CTA `btn-primary` solo** di kanan.
- **D-11 (dead-code):** HAPUS atribut `onclick="document.getElementById('assignModal').querySelector('[data-bs-dismiss]') && null"`
  pada tombol "Tambah Mapping" (L58). Tombol TETAP buka modal via `data-bs-toggle="modal" data-bs-target="#assignModal"`
  (fungsi tak berubah). Tombol Import tetap `data-bs-toggle="modal" data-bs-target="#importMappingModal"`.

### Behavior parity locked (verifikasi runtime — Phase 354 lesson: grep+build TAK cukup, WAJIB Playwright)
- **D-12:** TIDAK menyentuh `@@section Scripts` kontrak (L606-1028): `openEditModal`, `submitAssign/Edit/Deactivate/Delete`,
  `confirmDeactivate/Delete`, `reactivateMapping`, `filterAssignmentUnits`, `updateAssignmentDefaults`,
  `filterCoacheesBySection`, `updateCoachSuggestHint`, `resetPageAndSubmit`. Semua AJAX tetap `appUrl('/Admin/...')`
  + `RequestVerificationToken` (PathBase-aware, tak hardcode, tak 404 di sub-path).
- **D-13:** Semua **4 modal + import modal** (assign L376 / edit L480 / deactivate L547 / delete L574 / import L1031)
  + id internal field TIDAK diubah. Filter form (L176), pagination (L350), empty-state (L217), import-results card
  (L104), cleanup alert (L64), TempData alert TETAP.
- **D-14:** Header `collapse-@@idx` id + `data-bs-target` dipindah dari `<tr class="table-primary">` ke
  `card-header`; pasangan id↔target tetap unik per idx. `idx++` loop dipertahankan.

### Claude's Discretion
- Markup persis `btn-group` Excel (D-10) + util spacing antar-card (`mb-3`/`gap`).
- Ikon chevron + animasi rotate saat buka/tutup (D-05) — boleh inline transition atau kelas scoped.
- a11y header card toggle: `role="button"`, `aria-expanded`, `aria-controls`, keyboard-focusable (Phase 354 lesson —
  assert via Playwright runtime, bukan asumsi).
- Penempatan badge "Graduated"/Status di tabel mini (selama markup data tak hilang).
- Blok `<style>` scoped minimal bila perlu (jangan buat file CSS bersama untuk 1 halaman).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirement & roadmap
- `.planning/REQUIREMENTS.md` §DSN-01/02/03 (L18-20) + Out of Scope (L35) + DSN-06 (L29, parity Phase 390).
- `.planning/ROADMAP.md` §v32.1 Phase 389 (L79-91) — goal + 5 success criteria + file-overlap note (L55).

### Target file (satu-satunya yang ditulis)
- `Views/Admin/CoachCoacheeMapping.cshtml` (1-1059) — toolbar L48-61 (dead onclick L58), grouped table L230-347
  (coach header row L250-267, badge threshold L260-264, coachee rows L271-343, kolom Aksi L301-341, hook
  `data-mapping-id` L273), `@@section Scripts` L606-1028 (JANGAN ubah kontrak), modal L376-604 + import L1031.

### Pattern referensi (idiom yang ditiru)
- `Views/Admin/ManageWorkers.cshtml:251-254` — idiom `avatar-initial rounded-circle bg-primary text-white` 36px
  (D-03, reuse verbatim).
- `.planning/phases/388-.../388-CONTEXT.md` — sibling milestone, idiom card `border-0 shadow-sm` + `card-header`
  (konsistensi v32.1).
- `CLAUDE.md` §Develop Workflow — verifikasi WAJIB `dotnet build` + `dotnet run` (localhost:5277) + Playwright + UAT.

No external ADR/spec — requirements fully captured in decisions above.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Avatar inisial**: `ManageWorkers.cshtml:251` `avatar-initial rounded-circle bg-primary text-white` 36px — tiru verbatim (D-03).
- **Card idiom**: `card border-0 shadow-sm` + `card-header` + `bi-*` — sudah dipakai di file ini (import-results card L132).
- **Bootstrap collapse**: SUDAH dipakai (coach header row toggle L250 `data-bs-toggle="collapse"`) — tinggal pindah ke `card-header` + buang `show` (D-05/D-14).
- Bootstrap utility (`btn-sm`, `btn-group`, `table-responsive`, `mb-3`) — tanpa file CSS baru.

### Established Patterns (JANGAN dirusak)
- AJAX semua `fetch(appUrl('/Admin/...'))` + `RequestVerificationToken` (PathBase-aware). Endpoint:
  `CoachCoacheeMappingAssign/Edit/Deactivate/Delete/Reactivate/GetSessionCount/ActiveAssignmentCount/DeletePreview`.
- Selector struktural yang dipakai JS: `tr[data-mapping-id]` (delete row.remove L973), id modal (`#assignModal`,
  `#editModal`, `#deactivateModal`, `#deleteModal`, `#importMappingModal`), `.coachee-checkbox`/`.coachee-item`
  (assign modal). Markup baru HARUS jaga semua ini.
- Badge threshold hardcoded 5/8 (L262) — parity, jangan tautkan ke threshold konfigurabel.
- Graduated logic: cek `IsCompleted` sebelum `IsActive` (Phase 356 D-06).

### Integration Points
- Tak ada integrasi baru. Controller `CoachMapping`/`Admin` + semua endpoint AJAX TIDAK disentuh. `ViewBag.GroupedCoaches`
  (CoachName, CoachSection, ActiveCount, Coachees[Id, CoacheeName, CoacheeNIP, AssignmentSection, AssignmentUnit,
  CoacheePosition, ProtonTrack, IsActive, IsCompleted, StartDate, CoachId]) dikonsumsi apa adanya.

### Verification approach (RISK TERTINGGI → ketat)
- `dotnet build` 0 error (Razor compile) + `dotnet run` localhost:5277 + **Playwright runtime** (Phase 354 lesson):
  card collapse buka/tutup, modal assign/edit muncul, AJAX reactivate/deactivate/delete OK di sub-path, a11y header
  toggle. UAT browser semua aksi (tambah/edit/nonaktif/graduated/hapus/aktifkan + import/export) — parity penuh Phase 390.
</code_context>

<specifics>
## Specific Ideas

- Arah desain ditetapkan via brainstorm + visual-companion: CoachCoacheeMapping = **opsi B accordion card** (avatar
  header + badge beban warna-ikut-threshold + klik buka/tutup). Opsi C (master-detail/kanban penuh) **DITOLAK**.
- "Rapi" = default tertutup + toolbar seragam + tanpa dead-code; bukan menambah kapabilitas.
- Card independen (bukan single-open accordion) dipilih agar admin bisa banding beberapa coach sekaligus.
</specifics>

<deferred>
## Deferred Ideas

None — diskusi tetap dalam scope phase. Verifikasi parity penuh semua aksi = Phase 390 (DSN-06). Polish CoachWorkload
+ label hasil = Phase 388 (file disjoint).
</deferred>

---

*Phase: 389-coachcoacheemapping-redesign-accordion-card-per-coach-dsn-01-dsn-02-dsn-03*
*Context gathered: 2026-06-17*
