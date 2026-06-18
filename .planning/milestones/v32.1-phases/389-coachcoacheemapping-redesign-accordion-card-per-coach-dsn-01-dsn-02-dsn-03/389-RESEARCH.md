# Phase 389: CoachCoacheeMapping Redesign — Accordion Card per Coach (DSN-01 + DSN-02 + DSN-03) - Research

**Researched:** 2026-06-17
**Domain:** Razor view structural rewrite (Bootstrap 5 grouped `<table>` → accordion `card` per coach) under strict **behavior parity** — 1 file only, 0 backend.
**Confidence:** HIGH (semua klaim diverifikasi langsung dari source di repo session ini — file target, controller, _Layout, sibling spec)

> **Bahasa:** Riset ini ditulis campuran ID/EN agar selaras dgn CONTEXT/UI-SPEC. UI labels final = Bahasa Indonesia (verbatim, tak diubah — D-08/copy note).

## Summary

Phase 389 mengganti satu `<table class="table table-bordered">` grouped (di mana coach-header row `table-primary` + `<tbody class="collapse show">` coachee berbagi grid kolom yang sama) menjadi **satu `card border-0 shadow-sm mb-3` per coach**, dengan `card-header` clickable yang men-toggle `collapse` body berisi tabel coachee mini. Ini adalah **rewrite struktural dari markup `else`-block L228-347** + normalisasi toolbar L48-61. **Tidak ada satu pun perubahan controller/endpoint/JS-contract** — `@section Scripts` L606-1028 dibekukan total (D-12).

Risiko utama **bukan kompleksitas desain** (idiom card + collapse sudah ada di app, bahkan di file ini), melainkan **parity regression**: ada **9 fungsi JS** dan **~6 selector struktural** yang bergantung pada bentuk markup tertentu. Yang paling rapuh: (1) `submitDelete()` melakukan `document.querySelector('tr[data-mapping-id="..."]').remove()` (L973-974) — selector ini HARUS tetap valid; (2) blok Aksi L301-341 dengan urutan `if IsCompleted → else if IsActive → else` (Phase 356 D-06) yang JANGAN diubah; (3) badge threshold hardcoded 5/8 (L262) yang pindah ke header card; (4) `openEditModal(7 args)` inline yang harus tetap valid string-escape-nya. Routing aman: AJAX target `/Admin/...` resolve karena `CoachMappingController` ber-`[Route("Admin/[action]")]` — itu properti controller, **kebal** terhadap rewrite view.

**Primary recommendation:** Rewrite HANYA blok `@if (!groupedCoaches.Any()) { ... } else { <table>...</table> ... pagination }` (L217-373) + toolbar (L48-61). Pertahankan setiap `onclick`, setiap `data-mapping-id`, setiap modal id, dan pasangan `collapse-@idx`↔`#collapse-@idx` verbatim — pindahkan id/target dari `<tr class="table-primary">` ke `card-header`. Validasi WAJIB runtime Playwright (Phase 354 lesson), pakai sibling spec `coachworkload-388.spec.ts` sebagai template.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
**DSN-01 — Accordion card per coach + header**
- **D-01:** Tiap coach group (`groupedCoaches`) di-render sebagai **1 card** `card border-0 shadow-sm mb-3` (idiom app, konsisten dgn import-results card L132 & 388 polish). BUKAN `<table>` grouped lagi.
- **D-02:** Header card = `card-header` clickable berisi (kiri→kanan): **avatar inisial** + **nama coach** + **section** (muted, mis. `— @group.CoachSection`) + **badge jumlah coachee aktif** (kanan).
- **D-03 (avatar):** Reuse idiom `Views/Admin/ManageWorkers.cshtml:251-254` verbatim — `<div class="avatar-initial rounded-circle bg-primary text-white d-flex align-items-center justify-content-center fw-bold" style="width:36px;height:36px;font-size:0.8rem">` isi `CoachName.Substring(0,1).ToUpper()` (fallback `"?"` bila kosong). **Warna = bg-primary NETRAL** (bukan ikut beban / section).
- **D-04 (badge beban — PARITY KRITIS):** Pertahankan logika threshold existing PERSIS: `activeCount >= 8 ? "bg-danger" : activeCount >= 5 ? "bg-warning text-dark" : "bg-info text-dark"` (L260-264). Hardcoded 5/8 — JANGAN tautkan ke threshold konfigurabel CoachWorkload. Badge isi `@group.ActiveCount`.

**DSN-02 — Collapse + tabel coachee mini**
- **D-05 (default state):** **TERTUTUP semua**, **card independen** (multiple boleh terbuka bersamaan). Pakai Bootstrap `collapse` manual (header `data-bs-toggle="collapse" data-bs-target="#collapse-@idx"`) — **BUKAN** komponen `.accordion` ber-`data-bs-parent`. Body collapse = `collapse` (TANPA `show`). Chevron header refleksikan state (rotate saat buka).
- **D-06 (tabel mini):** **Tabel penuh + `table-responsive`** (scroll-x). Kolom existing utuh **9**: Nama, NIP, @OrgLabels.GetLabel(0) Penugasan, @OrgLabels.GetLabel(1) Penugasan, Jabatan, Proton Track, Status, Mulai, Aksi.
- **D-07 (drop kolom redundan):** Kolom **"Coachee Aktif"** lama (header L241 + sel kosong L300) **DILEPAS** dari tabel mini — nilainya (badge ActiveCount) sudah pindah ke header card.
- **D-08 (PARITY KRITIS — hook delete):** Tiap baris coachee WAJIB tetap `<tr data-mapping-id="@coachee.Id">`. `submitDelete()` cari `document.querySelector('tr[data-mapping-id="..."]').remove()` (L973-974). Baris non-aktif tetap `table-light text-muted` (L273).
- **D-09 (PARITY KRITIS — kolom Aksi):** Pertahankan SELURUH blok Aksi (L301-341) verbatim: Edit (`openEditModal` 7 arg), badge "Graduated" (cek `IsCompleted` DULU), Nonaktifkan (`confirmDeactivate`), form `MarkMappingCompleted`, Aktifkan (`reactivateMapping`), Hapus (`confirmDelete`). Urutan if/else `IsCompleted → IsActive → else` JANGAN diubah.

**DSN-03 — Toolbar seragam + dead-code**
- **D-10:** Toolbar header (L48-61): normalisasi semua tombol seragam (`btn-sm`); kelompokkan Excel (Download Template / Import Excel / Export Excel) visual; "Tambah Mapping" jadi CTA `btn-primary` solo di kanan.
- **D-11 (dead-code):** HAPUS atribut `onclick="document.getElementById('assignModal').querySelector('[data-bs-dismiss]') && null"` pada tombol "Tambah Mapping" (L58). Tombol TETAP buka modal via `data-bs-toggle="modal" data-bs-target="#assignModal"`. Import tetap `data-bs-target="#importMappingModal"`.

**Behavior parity locked (verifikasi runtime — Phase 354 lesson)**
- **D-12:** TIDAK menyentuh `@section Scripts` kontrak (L606-1028). Semua AJAX tetap `appUrl('/Admin/...')` + `RequestVerificationToken`.
- **D-13:** Semua **4 modal + import modal** (assign L376 / edit L480 / deactivate L547 / delete L574 / import L1031) + id internal field TIDAK diubah. Filter form (L176), pagination (L350), empty-state (L217), import-results card (L104), cleanup alert (L64), TempData alert TETAP.
- **D-14:** Header `collapse-@idx` id + `data-bs-target` dipindah dari `<tr class="table-primary">` ke `card-header`; pasangan id↔target tetap unik per idx. `idx++` loop dipertahankan.

### Claude's Discretion
- Markup persis `btn-group` Excel (D-10) + util spacing antar-card (`mb-3`/`gap`).
- Ikon chevron + animasi rotate saat buka/tutup (D-05) — boleh inline transition atau kelas scoped.
- a11y header card toggle: `role="button"`, `aria-expanded`, `aria-controls`, keyboard-focusable (assert via Playwright runtime).
- Penempatan badge "Graduated"/Status di tabel mini (selama markup data tak hilang).
- Blok `<style>` scoped minimal bila perlu (jangan buat file CSS bersama untuk 1 halaman).

### Deferred Ideas (OUT OF SCOPE)
- Verifikasi parity penuh semua aksi = **Phase 390 (DSN-06)** — bukan phase ini.
- Polish CoachWorkload + label hasil = **Phase 388** (file disjoint).
- Redesign master-detail / kanban penuh (arah "C") — **DITOLAK saat brainstorm**.
- Perubahan backend/controller/endpoint/JS-contract — **OUT** (milestone murni view/JS).
- Kolom data baru / sort / pencarian baru — **OUT** (hanya tata-letak & teks).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **DSN-01** | Daftar mapping sbg **accordion card per coach** — header: avatar inisial + nama + section + badge beban warna-ikut-threshold (info `<5` / warning `>=5` / danger `>=8`). | §Architecture Patterns (Pattern 1 card+header, Pattern 2 avatar), §Code Examples (card-header markup), §Don't Hand-Roll (avatar idiom, badge logic verbatim). Threshold logic source = L260-264, dipindah ke header (Color contract UI-SPEC). |
| **DSN-02** | Klik header buka/tutup tabel coachee mini; semua 9 kolom data tetap tampil. | §Architecture Patterns (Pattern 3 collapse-in-card, Pattern 4 mini-table), §Bootstrap Collapse-in-Card Technique (markup + a11y), §Code Examples (mini table + Aksi block). Default CLOSED, independent (no `data-bs-parent`). |
| **DSN-03** | Toolbar rapi/seragam + dead-code `onclick` dihapus tanpa ubah fungsi. | §Architecture Patterns (Pattern 5 toolbar btn-group), §Common Pitfalls (P-7 dead onclick removal), §Code Examples (toolbar markup). Tombol tetap buka modal via `data-bs-toggle`. |
</phase_requirements>

## Architectural Responsibility Map

> Phase ini single-tier (server-rendered Razor view + inline JS markup). "Tier" di sini = lapisan dalam satu file view.

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Render accordion card per coach | Razor markup (`else`-block L228-347) | — | Pure server-side loop atas `ViewBag.GroupedCoaches`; data sudah disediakan controller, **tak perlu controller diubah** [VERIFIED: file L246-346]. |
| Collapse toggle (buka/tutup) | Bootstrap 5 JS (CDN, declarative `data-bs-toggle`) | Razor (id/target render) | Bootstrap collapse component sudah loaded via `_Layout.cshtml` L38; toggle 100% declarative — **0 custom JS** [VERIFIED: _Layout L38]. |
| Avatar inisial | Razor markup | — | Idiom reuse dari `ManageWorkers.cshtml:251-254`; pure C# substring [VERIFIED: ManageWorkers L251-254]. |
| Badge beban threshold | Razor markup (logika C# inline) | — | Logika `>=8/>=5` dipindah verbatim dari sel tabel L260-264 ke header card; **hardcoded, tak ke service** [VERIFIED: file L262]. |
| Semua aksi (assign/edit/deactivate/delete/reactivate/graduated/import/export) | `@section Scripts` JS + Controller AJAX | — | **PARITY-LOCKED — tier ini TIDAK disentuh** (D-12). Hanya wiring `onclick`/`data-*` di markup yang harus tetap memanggil fungsi yang sama [VERIFIED: file L606-1028]. |
| Routing AJAX `/Admin/...` | Controller `[Route("Admin/[action]")]` | `_Layout` `appUrl()` PathBase | Properti controller — **kebal terhadap rewrite view**; `appUrl` prepend `basePath` (PathBase-aware, sub-path Dev) [VERIFIED: CoachMappingController L14 + _Layout L54-55]. |

**Catatan tier-correctness untuk planner:** Semua kapabilitas non-render = PARITY-LOCKED. Planner JANGAN menugaskan task apa pun yang menyentuh tier "JS/Controller/Routing". Hanya tier "Razor markup" (toolbar + `else`-block render) yang ditulis.

## Standard Stack

> Brownfield Bootstrap 5 app. TIDAK ada library baru di-install. Semua komponen sudah loaded via CDN di `_Layout.cshtml`.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap | **5.3.0** | card, collapse, table, badge, btn, btn-group, modal | Sudah loaded CDN `_Layout.cshtml` L38 [VERIFIED: _Layout L38]. Collapse + card komponen native — tak perlu plugin. |
| Bootstrap Icons | **1.10.0** | `bi-chevron-down`, `bi-people`, `bi-pencil`, `bi-award`, dll | Sudah loaded CDN `_Layout.cshtml` L39 [VERIFIED: _Layout L39]. Semua ikon yang dipakai sudah ada di file. |
| ASP.NET Core MVC Razor | net (repo `.cshtml`) | Server-side render loop `ViewBag.GroupedCoaches` | Engine view existing; `@section Styles`/`@section Scripts` slots tersedia [VERIFIED: _Layout L44 + file L606]. |
| Bootstrap JS bundle | 5.3.0 | `bootstrap.Modal`, collapse toggle | Dipakai oleh `openEditModal`/`confirmDeactivate`/`confirmDelete` (`new bootstrap.Modal(...)`) [VERIFIED: file L823, L908, L941]. |

### Supporting (sudah ada, JANGAN diubah — hanya disebut untuk konteks parity)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| SweetAlert2 (`Swal`) | (loaded global) | toast saat reactivate `showAssignPrompt` | Dipakai di `reactivateMapping` L1006 — **JS-contract, tak disentuh** [VERIFIED: file L1006]. |
| Inter font + site.css | — | tipografi app | Inherited dari layout — phase set NO font [CITED: UI-SPEC Typography]. |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Bootstrap `collapse` manual (D-05) | Bootstrap `.accordion` component | `.accordion` + `data-bs-parent` MEMAKSA single-open (auto-close sibling) — **ditolak D-05** karena admin perlu banding banyak coach sekaligus. `collapse` manual = independent. |
| Avatar inisial (idiom reuse) | Gravatar / foto user | Tak ada foto user di data; idiom inisial sudah established app-wide. Reuse verbatim (D-03). |
| `table-responsive` mini-table (D-06) | card-list / definition-list per coachee | Tabel = parity tertinggi (kolom identik), risiko terendah. `table-responsive` solusi scroll-x. **Locked D-06.** |

**Installation:** `(tidak ada — 0 dependency baru)` [VERIFIED: semua via CDN _Layout].

**Version verification:** Tidak ada paket npm/nuget baru. Bootstrap 5.3.0 + Icons 1.10.0 = versi yang sudah pinned di `_Layout.cshtml` CDN URL [VERIFIED: _Layout L38-39]. JANGAN bump versi (out of scope, risiko visual regression lintas halaman).

## Parity Contract Inventory (CRITICAL — enumerasi penuh hook yang HARUS bertahan)

> Ini adalah jantung phase. Setiap baris = sebuah ketergantungan JS/struktural yang akan PECAH bila markup baru salah. Planner WAJIB pakai tabel ini sebagai checklist verifikasi per-task. Semua [VERIFIED] dari pembacaan langsung file session ini.

### A. Hook di blok yang DI-REWRITE (markup coachee row + toolbar) — risiko PECAH bila lalai

| # | Hook (existing) | Lokasi lama | Bergantung pada markup | Aturan parity di markup BARU |
|---|-----------------|-------------|------------------------|------------------------------|
| H-1 | `data-mapping-id="@coachee.Id"` pada `<tr>` | L273 | `submitDelete()` → `querySelector('tr[data-mapping-id="..."]').remove()` (L973-974) | **WAJIB** tetap `<tr ... data-mapping-id="@coachee.Id">` di mini-table. Elemen tetap `<tr>` (bukan `<div>`) [VERIFIED: L273↔L973]. |
| H-2 | `onclick="openEditModal(@coachee.Id, '@coachee.CoachId', 0, '@coachee.StartDate...', '@coachee.CoacheeName', '@coachee.AssignmentSection', '@coachee.AssignmentUnit')"` | L303-304 | `openEditModal(id, coachId, trackId, startDate, coacheeName, assignmentSection, assignmentUnit)` (L806, 7 param) | **WAJIB** 7 argumen, urutan & escaping identik. trackId selalu literal `0` [VERIFIED: L304↔L806]. |
| H-3 | `onclick="confirmDeactivate(@coachee.Id, '@coachee.CoacheeName')"` | L317 | `confirmDeactivate(id, coacheeName)` (L869) | **WAJIB** 2 arg identik. Hanya muncul saat `IsActive && !IsCompleted` [VERIFIED: L317↔L869]. |
| H-4 | `<form asp-action="MarkMappingCompleted" asp-controller="CoachMapping">` + `@Html.AntiForgeryToken()` + hidden `mappingId` + `onclick="return confirm('Tandai...graduated?')"` | L320-327 | POST form route `/Admin/MarkMappingCompleted` (controller `[Route("Admin/[action]")]`) | **WAJIB** form + token + hidden + confirm verbatim. Submit non-AJAX (page reload) [VERIFIED: L320-327 + controller L14]. |
| H-5 | `onclick="reactivateMapping(@coachee.Id)"` | L332 | `reactivateMapping(id)` (L991) | **WAJIB** 1 arg. Hanya muncul saat `!IsCompleted && !IsActive` [VERIFIED: L332↔L991]. |
| H-6 | `onclick="confirmDelete(@coachee.Id, '@coachee.CoacheeName')"` | L336 | `confirmDelete(id, coacheeName)` (L934) | **WAJIB** 2 arg. Hanya muncul saat `!IsCompleted && !IsActive` [VERIFIED: L336↔L934]. |
| H-7 | Urutan branching `@if (IsCompleted) {badge} else if (IsActive) {Nonaktif+Graduated} else {Aktifkan+Hapus}` | L310-339 | Logika tampil tombol (Phase 356 D-06) | **WAJIB** urutan `IsCompleted → IsActive → else` TAK BERUBAH. IsCompleted dicek DULU [VERIFIED: L310-339; CITED: CONTEXT D-09]. |
| H-8 | `<tr class="@(coachee.IsActive ? "" : "table-light text-muted")">` | L273 | styling baris non-aktif | **WAJIB** kelas conditional ini tetap di `<tr>` mini-table [VERIFIED: L273]. |
| H-9 | Toolbar tombol "Tambah Mapping": `data-bs-toggle="modal" data-bs-target="#assignModal"` | L58 | Bootstrap modal open declarative | **WAJIB** atribut modal toggle tetap. **HAPUS** hanya `onclick="...&& null"` (dead, D-11) [VERIFIED: L58]. |
| H-10 | Toolbar "Import Excel": `data-bs-toggle="modal" data-bs-target="#importMappingModal"` | L52 | Bootstrap modal open | **WAJIB** tetap [VERIFIED: L52]. |
| H-11 | Toolbar Download Template (`asp-action="DownloadMappingImportTemplate"`) + Export (`asp-action="CoachCoacheeMappingExport"`) | L49, L55 | tag-helper route gen | **WAJIB** `asp-action`/`asp-controller` tetap; hanya kelas tombol dinormalisasi [VERIFIED: L49, L55]. |

### B. Hook di blok yang DIPINDAH (coach-header `<tr>` → `card-header`)

| # | Hook (existing) | Lokasi lama | Aturan parity di markup BARU |
|---|-----------------|-------------|------------------------------|
| H-12 | `data-bs-toggle="collapse" data-bs-target="#collapse-@idx"` | `<tr class="table-primary">` L250 | **PINDAH** ke `card-header` div. Target id `#collapse-@idx` tetap unik per idx [VERIFIED: L250; CITED: D-14]. |
| H-13 | `id="collapse-@idx"` pada container collapse | `<tbody class="collapse show">` L270 | **PINDAH** ke `<div class="collapse" id="collapse-@idx">` (card-body wrapper). **BUANG `show`** (default closed, D-05) [VERIFIED: L270; CITED: D-05]. |
| H-14 | `var idx = 0;` + `idx++;` per loop | L245, L345 | **WAJIB** loop counter dipertahankan agar pasangan id↔target unik [VERIFIED: L245, L345; CITED: D-14]. |
| H-15 | Badge threshold `activeCount >= 8 ? "bg-danger" : activeCount >= 5 ? "bg-warning text-dark" : "bg-info text-dark"` | sel `<td>` L260-264 | **PINDAH** ke header card (kanan). Logika & angka 5/8 verbatim [VERIFIED: L262; CITED: D-04]. |

### C. Hook di blok yang TIDAK DISENTUH (cek negatif — harus tetap utuh, jangan tergeser)

| # | Element | Lokasi | Status |
|---|---------|--------|--------|
| H-16 | Filter form `#filterForm`, `#pageInput`, `resetPageAndSubmit()` | L176-214 | **TAK DISENTUH** — di atas `else`-block, parity by non-edit [VERIFIED: L176↔L666-669]. |
| H-17 | Empty-state `@if (!groupedCoaches.Any())` | L217-227 | **TAK DISENTUH** (bagian `if`; hanya `else` di-rewrite) [VERIFIED: L217-227]. |
| H-18 | Pagination `@if (totalPages > 1)` | L350-372 | **DI DALAM `else`-block** — harus tetap muncul SETELAH list card (di luar loop card). Re-emit verbatim [VERIFIED: L350-372]. |
| H-19 | Semua 5 modal (`#assignModal` L376, `#editModal` L480, `#deactivateModal` L547, `#deleteModal` L574, `#importMappingModal` L1031) + id field internal | L376-604, L1031 | **TAK DISENTUH** (di luar `else`-block) [VERIFIED; CITED: D-13]. |
| H-20 | `.coachee-item` / `.coachee-checkbox` / `data-section` / `data-unit` (assign modal) | L412-414 | **TAK DISENTUH** — milik assign modal, bukan list utama. `filterCoacheesBySection`/`updateAssignmentDefaults` baca ini [VERIFIED: L412↔L675, L686]. |
| H-21 | Import-results card + stat cards | L104-173 | **TAK DISENTUH** (di atas `else`-block) [VERIFIED: L104-173]. |
| H-22 | Cleanup alert / ImportError / ImportWarnings / TempData alert | L64-102 | **TAK DISENTUH** [VERIFIED: L64-102]. |
| H-23 | `@Html.AntiForgeryToken()` top-level (token sumber untuk `[name=__RequestVerificationToken]`) | L30 | **TAK DISENTUH** — semua AJAX baca token ini [VERIFIED: L30↔L771]. |

### D. AJAX endpoints (tier controller — NOL perubahan; daftar untuk verifikasi runtime parity)

| Fungsi JS | Endpoint (via `appUrl`) | Method | Trigger markup |
|-----------|-------------------------|--------|----------------|
| `submitAssign` | `/Admin/CoachCoacheeMappingAssign` | POST json | tombol Simpan modal assign [VERIFIED: L767] |
| `submitEdit` | `/Admin/CoachCoacheeMappingEdit` | POST json | tombol Simpan modal edit [VERIFIED: L849] |
| `confirmDeactivate` | `/Admin/CoachCoacheeMappingGetSessionCount` + `/Admin/CoachCoacheeMappingActiveAssignmentCount` | POST/GET | tombol Nonaktifkan (H-3) [VERIFIED: L875, L894] |
| `submitDeactivate` | `/Admin/CoachCoacheeMappingDeactivate` | POST form | tombol modal deactivate [VERIFIED: L914] |
| `confirmDelete` | `/Admin/CoachCoacheeMappingDeletePreview` | GET | tombol Hapus (H-6) [VERIFIED: L944] |
| `submitDelete` | `/Admin/CoachCoacheeMappingDelete` | POST form | tombol modal delete; **lalu `tr[data-mapping-id].remove()`** [VERIFIED: L961, L973] |
| `reactivateMapping` | `/Admin/CoachCoacheeMappingReactivate` | POST form | tombol Aktifkan (H-5) [VERIFIED: L994] |
| (form) `MarkMappingCompleted` | `/Admin/MarkMappingCompleted` | POST form (page reload) | form Graduated (H-4) [VERIFIED: L320 + controller route] |

**Routing parity guarantee:** `CoachMappingController` di-anotasi `[Route("Admin/[action]")]` (L14) → semua `/Admin/Coach...` resolve ke controller ini meskipun namanya `CoachMapping`. Ini **properti controller, bukan view** → rewrite view TIDAK bisa memecahkan routing [VERIFIED: CoachMappingController.cs L14].

## Architecture Patterns

### System Architecture Diagram (data flow — render path)

```
                           ┌──────────────────────────────────────────────┐
   HTTP GET /Admin/         │  CoachMappingController.CoachCoacheeMapping() │
   CoachCoacheeMapping ───► │  (UNCHANGED — builds ViewBag.GroupedCoaches)  │
   ?search&section&showAll  └───────────────────┬──────────────────────────┘
                                                 │ ViewBag.GroupedCoaches : IEnumerable<dynamic>
                                                 │   each group: { CoachName, CoachSection, ActiveCount,
                                                 │                 Coachees:[{ Id, CoacheeName, CoacheeNIP,
                                                 │                 AssignmentSection, AssignmentUnit,
                                                 │                 CoacheePosition, ProtonTrack, IsActive,
                                                 │                 IsCompleted, StartDate, CoachId }] }
                                                 ▼
        ┌────────────────────  CoachCoacheeMapping.cshtml (THE ONLY FILE EDITED)  ───────────────────┐
        │  Toolbar (REWRITE D-10/11) ── Filter form (KEEP) ── Empty/Import/Alerts (KEEP)              │
        │                                                                                             │
        │   @foreach group in GroupedCoaches  (REWRITE — was <table>, now per-coach card)             │
        │   ┌─────────────────────────────────────────────────────────────────────────────────┐     │
        │   │  <div class="card border-0 shadow-sm mb-3">                                       │     │
        │   │   <div class="card-header" role=button data-bs-toggle=collapse                    │     │
        │   │        data-bs-target=#collapse-@idx aria-controls=collapse-@idx                  │     │
        │   │        aria-expanded=false tabindex=0>                                             │     │
        │   │        [avatar 36px] [CoachName] [— Section muted] ──ms-auto── [badge ActiveCount] │     │
        │   │        [chevron]                                                                   │     │
        │   │   <div class="collapse" id="collapse-@idx">   ◄── NO `show`, NO data-bs-parent     │     │
        │   │     <div class="card-body p-0"><table-responsive><table> mini coachee rows         │     │
        │   │       <tr data-mapping-id=@Id class="table-light?"> ... <Aksi block verbatim> ... │     │
        │   └─────────────────────────────────────────────────────────────────────────────────┘     │
        │   idx++                                                                                      │
        │   Pagination (KEEP, after loop) ── 5 Modals (KEEP) ── @section Scripts (FROZEN, D-12)        │
        └─────────────────────────────────────────────────────────────────────────────────────────────┘
                                                 │
              user clicks card-header  ──────────┼──► Bootstrap collapse toggle (declarative, 0 JS)
              user clicks Edit/Nonaktif/...  ────┴──► existing onclick → @section Scripts JS → AJAX
                                                         (appUrl + RequestVerificationToken) ──► controller
```

### Recommended Project Structure (single file — blok yang ditulis)

```
Views/Admin/CoachCoacheeMapping.cshtml
├── L1-46     @{ ... } + breadcrumb + page-header H2          # KEEP
├── L48-61    Toolbar                                          # REWRITE (D-10, D-11)
├── L64-173   Cleanup/Import alerts + import-results card      # KEEP
├── L176-214  Filter form                                      # KEEP
├── L217-227  Empty-state (@if !.Any())                        # KEEP
├── L228-347  else { <table> grouped }                         # REWRITE → @foreach card
├── L350-372  Pagination (inside else)                         # KEEP (re-emit after card loop)
├── L376-604  Modals assign/edit/deactivate/delete             # KEEP (D-13)
├── L606-1028 @section Scripts                                 # FROZEN (D-12)
├── L1031     Import modal                                     # KEEP (D-13)
└── (+ optional @section Styles for chevron rotation)          # NEW, scoped, Claude's discretion
```

### Pattern 1: Clickable card-header as collapse toggle (DSN-01 + DSN-02 core)
**What:** `card-header` div ber-`data-bs-toggle="collapse"` men-toggle collapse body. Bootstrap 5 mengizinkan elemen non-`<a>`/non-`<button>` jadi trigger via `data-bs-toggle="collapse"` + `data-bs-target` [CITED: getbootstrap.com/docs/5.3/components/collapse].
**When to use:** Setiap card coach.
**a11y wajib (Phase 354 lesson — assert runtime):** Karena `card-header` adalah `<div>` (bukan `<button>`), tambahkan `role="button"`, `tabindex="0"`, `aria-expanded="false"`, `aria-controls="collapse-@idx"`. Bootstrap collapse otomatis toggle `aria-expanded` true/false saat buka/tutup [CITED: Bootstrap collapse a11y].
**Catatan keyboard:** `data-bs-toggle="collapse"` pada `<div role="button" tabindex="0">` aktif via klik + (Bootstrap menangani) Enter; Space mungkin perlu dicek runtime. **Alternatif paling aman a11y: render header sebagai `<button class="card-header ...">` real** (otomatis Enter+Space+focus) — UI-SPEC mengizinkan ("OR render as a real `<button>`"). Planner boleh memilih; assert via Playwright.

### Pattern 2: Avatar inisial reuse (DSN-01, D-03)
**What:** Lingkaran 36px `bg-primary` berisi huruf pertama nama coach. **Verbatim** dari `ManageWorkers.cshtml:251-254` [VERIFIED: ManageWorkers L251-254].
**When:** Sisi kiri card-header.
**Penting:** `bg-primary` NETRAL (bukan warna beban) — badge sudah membawa warna threshold (hindari double-encode, D-03). Avatar `aria-hidden="true"` (dekoratif, nama coach adjacent).

### Pattern 3: Independent collapse (NO data-bs-parent) (DSN-02, D-05)
**What:** Collapse manual, BUKAN komponen `.accordion`. Tidak ada `data-bs-parent` → multiple card boleh terbuka bersamaan.
**When:** Default semua card CLOSED (`class="collapse"` TANPA `show`).
**Anti-pattern:** Jangan pakai `<div class="accordion">` wrapper + `.accordion-item` + `data-bs-parent="#..."` — itu memaksa single-open (auto-close sibling). Ditolak D-05.

### Pattern 4: Mini coachee table in card-body (DSN-02, D-06/D-07)
**What:** `<div class="card-body p-0"><div class="table-responsive"><table class="table align-middle mb-0">`. 9 kolom (TANPA "Coachee Aktif").
**When:** Isi collapse body.
**Penting:** Drop kolom "Coachee Aktif" (header lama L241 + sel kosong L300). Sel kosong placeholder L300 (`<td></td>`) JUGA dibuang — bukan hanya headernya.

### Pattern 5: Toolbar normalize + Excel group (DSN-03, D-10)
**What:** `<div class="d-flex gap-2 flex-wrap">`; 3 tombol Excel di-`btn-group` (outline konsisten, `btn-sm`); "Tambah Mapping" solo `btn-primary btn-sm` di kanan.
**When:** Page-header kanan.
**Penting:** HAPUS dead `onclick` (D-11) tapi PERTAHANKAN `data-bs-toggle="modal" data-bs-target="#assignModal"`.

### Anti-Patterns to Avoid
- **Mengubah `<tr>` jadi `<div>` di mini-table:** PECAH `submitDelete()` (H-1). Baris coachee WAJIB `<tr>`.
- **Membungkus cards dalam `.accordion` + `data-bs-parent`:** memaksa single-open (langgar D-05).
- **Menulis ulang/menyentuh blok Aksi atau urutan if/else:** langgar D-09/H-7, regresi Phase 356 D-06.
- **Mengganti `appUrl(...)` dengan path hardcoded:** 404 di sub-path Dev `/KPB-PortalHC` (D-12).
- **Membuat file CSS bersama untuk chevron:** pakai `@section Styles` scoped inline (Claude's discretion).
- **Memindah/menyentuh modal/filter/pagination:** langgar D-13 (cek negatif H-16..H-23).
- **Mengubah label teks UI:** copy verbatim (Bahasa Indonesia, D-08/copy note UI-SPEC).

## Bootstrap Collapse-in-Card Technique (DSN-02 deep dive)

Markup target (menggabungkan D-02/D-05/D-14 + a11y):

```html
<div class="card border-0 shadow-sm mb-3">
  <!-- card-header = collapse toggle (id/target PINDAH dari <tr> lama) -->
  <div class="card-header bg-white d-flex align-items-center gap-2"
       role="button" tabindex="0" style="cursor:pointer"
       data-bs-toggle="collapse" data-bs-target="#collapse-@idx"
       aria-expanded="false" aria-controls="collapse-@idx">
    <div class="avatar-initial rounded-circle bg-primary text-white d-flex align-items-center justify-content-center fw-bold"
         style="width:36px;height:36px;font-size:0.8rem" aria-hidden="true">
      @( ((string)group.CoachName).Length > 0 ? ((string)group.CoachName).Substring(0,1).ToUpper() : "?" )
    </div>
    <span class="fw-semibold">@group.CoachName</span>
    @if (!string.IsNullOrEmpty((string)group.CoachSection))
    {
        <span class="text-muted small">— @group.CoachSection</span>
    }
    <span class="ms-auto">
      @{
          int activeCount = (int)group.ActiveCount;
          string badgeClass = activeCount >= 8 ? "bg-danger" : activeCount >= 5 ? "bg-warning text-dark" : "bg-info text-dark";
      }
      <span class="badge @badgeClass" title="@activeCount coachee aktif">@activeCount</span>
    </span>
    <i class="bi bi-chevron-down chevron-toggle"></i>
  </div>
  <!-- collapse body: NO `show`, NO data-bs-parent -->
  <div class="collapse" id="collapse-@idx">
    <div class="card-body p-0">
      <div class="table-responsive">
        <table class="table align-middle mb-0"> ... mini rows ... </table>
      </div>
    </div>
  </div>
</div>
```

**Chevron rotation (scoped `@section Styles`, Claude's discretion):** Bootstrap menambahkan kelas `.collapsed` pada trigger saat collapse TERTUTUP, dan menghapusnya saat terbuka [CITED: Bootstrap collapse — trigger gets `.collapsed` when target closed]. Pola idiomatik:

```css
/* default (closed → .collapsed present): chevron down */
[data-bs-toggle="collapse"] .chevron-toggle { transition: transform .2s ease; }
/* expanded (NOT .collapsed): rotate */
[data-bs-toggle="collapse"]:not(.collapsed) .chevron-toggle { transform: rotate(180deg); }
```

> **Pitfall a11y:** karena default CLOSED, trigger HARUS punya kelas `.collapsed` saat render awal ATAU `aria-expanded="false"`. Bootstrap menentukan state awal dari `aria-expanded`/keberadaan `.show` pada target. Karena body TANPA `show` + trigger ber-`aria-expanded="false"` → state konsisten "closed" sejak load. **Assert runtime** (jangan asumsi) — bila chevron tampak rotated saat load, berarti `.collapsed` tak ter-set; perbaiki dengan eksplisit `class="... collapsed"` pada trigger awal.

## The Grouped Data Shape (DSN-01/02 — konfirmasi: 0 perubahan controller)

`ViewBag.GroupedCoaches` = `IEnumerable<dynamic>`; field yang **benar-benar di-render** (dikonsumsi apa adanya — controller TIDAK perlu diubah) [VERIFIED: file L246-346]:

| Field group | Dipakai di | | Field per coachee | Dipakai di |
|-------------|-----------|--|-------------------|-----------|
| `CoachName` | header + avatar (L253) | | `Id` | `data-mapping-id` + semua onclick (L273) |
| `CoachSection` | header muted (L254-256) | | `CoacheeName` | kol Nama + arg onclick (L274) |
| `ActiveCount` | badge threshold (L261-264) | | `CoacheeNIP` | kol NIP (L275) |
| `Coachees` (list) | loop rows (L271) | | `AssignmentSection` | kol + arg edit (L276) |
| | | | `AssignmentUnit` | kol + arg edit (L277) |
| | | | `CoacheePosition` | kol Jabatan (L278) |
| | | | `ProtonTrack` | badge (L280-287) |
| | | | `IsActive` | row class + branch Aksi (L273, L290, L314) |
| | | | `IsCompleted` | branch Graduated DULU (L310) |
| | | | `StartDate` | kol Mulai `dd MMM yyyy` id-ID + arg edit (L299, L304) |
| | | | `CoachId` | arg edit (L304) |

**Konfirmasi:** SEMUA field yang dibutuhkan markup baru sudah tersedia di shape lama. **Tidak ada field baru** yang perlu controller emit. Avatar (`Substring(0,1)`) derive dari `CoachName` yang sudah ada. Badge threshold derive dari `ActiveCount` yang sudah ada. [VERIFIED: tidak ada referensi field di luar daftar ini pada blok render].

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Collapse buka/tutup | Custom JS show/hide + toggle class | Bootstrap `data-bs-toggle="collapse"` declarative | 0 JS, a11y aria-expanded otomatis, animasi built-in. Sudah dipakai di file ini (L250). |
| Avatar inisial | Markup avatar dari nol | Reuse `ManageWorkers.cshtml:251-254` verbatim | Idiom established; styling 36px/0.8rem sudah teruji visual app-wide (D-03). |
| Badge warna beban | `if/else` baru / threshold service | Salin verbatim L260-264 `>=8/>=5` | PARITY KRITIS — angka 5/8 hardcoded, JANGAN tautkan ke CoachWorkload configurable (D-04). |
| Modal open | `new bootstrap.Modal` manual di markup | `data-bs-toggle="modal" data-bs-target="#..."` (toolbar) / fungsi JS existing (Aksi) | Kontrak existing; JANGAN tambah JS baru (D-12). |
| AJAX path | Hardcode `/Admin/...` | `appUrl('/Admin/...')` (existing JS) | PathBase-aware sub-path Dev. **TAK DISENTUH** (D-12). |
| Chevron rotate | JS event listener custom | CSS `:not(.collapsed)` selector | Declarative, tanpa JS, ikut state Bootstrap (lihat §Technique). |

**Key insight:** Phase ini hampir 100% reuse. Setiap "solusi custom" = risiko parity regression. Disiplin = SALIN, jangan REINVENT.

## Common Pitfalls

### Pitfall 1: `<tr>` diganti `<div>` → delete row hilang dari DOM
**What goes wrong:** Setelah ganti tabel jadi card, godaan render coachee sebagai list `<div>`. `submitDelete()` (L973) cari `tr[data-mapping-id]` → tak ketemu → row tak terhapus dari UI walau backend sukses.
**Why:** Selector di JS hardcoded `tr[...]`.
**How to avoid:** Mini-table TETAP `<table>` dengan `<tr data-mapping-id>` (D-06/D-08). [VERIFIED: L973].
**Warning signs:** Playwright: setelah klik Hapus Permanen, row masih tampak (test akan FAIL).

### Pitfall 2: Collapse default-open karena lupa buang `show`
**What goes wrong:** Salin `<tbody class="collapse show">` lama → semua card terbuka saat load (langgar D-05).
**Why:** `show` = expanded.
**How to avoid:** Body = `class="collapse"` TANPA `show` + trigger `aria-expanded="false"` (+ `.collapsed` bila perlu).
**Warning signs:** Runtime: semua card terbuka saat halaman load; chevron tampak rotated.

### Pitfall 3: id↔target collision atau idx tak increment
**What goes wrong:** Pindah `collapse-@idx` ke card-header tapi `idx++` hilang/salah posisi → dua card pakai `#collapse-0` → klik satu buka dua.
**Why:** Bootstrap target by id; id duplikat = behavior ambigu.
**How to avoid:** Pertahankan `var idx = 0;` (L245) + `idx++;` di akhir tiap iterasi (L345). Pasangkan `data-bs-target="#collapse-@idx"` (header) ↔ `id="collapse-@idx"` (body) DALAM iterasi yang sama, increment SETELAH (H-14).
**Warning signs:** Runtime: klik header coach A juga membuka card coach B.

### Pitfall 4: Inline `onclick` argument escaping pecah (apostrof di nama)
**What goes wrong:** `openEditModal(...,'@coachee.CoacheeName',...)` — bila nama/section/unit mengandung apostrof (`O'Brien`) string JS putus. (Risiko ini SUDAH ADA di markup lama; jangan PERPARAH, dan jangan "perbaiki" di luar scope.)
**Why:** Razor interpolasi langsung ke literal JS string.
**How to avoid:** SALIN escaping persis seperti markup lama (jangan ubah quoting). Bila planner ingin hardening (mis. data-attribute) → itu **JS-contract change = OUT** (D-12). Pertahankan apa adanya = parity.
**Warning signs:** Runtime di data dgn apostrof: JS error di console saat klik Edit. (Catat sebagai pre-existing, bukan regresi phase ini.)

### Pitfall 5: Kolom "Coachee Aktif" setengah-dilepas
**What goes wrong:** Buang header "Coachee Aktif" (L241) tapi lupa buang sel kosong `<td></td>` (L300) → kolom mini-table geser/misalign.
**Why:** Header & sel data harus dilepas BERSAMA.
**How to avoid:** Drop KEDUANYA (D-07). Hasil = tepat 9 kolom: Nama, NIP, Section, Unit, Jabatan, Proton Track, Status, Mulai, Aksi.
**Warning signs:** Runtime: jumlah `<th>` ≠ jumlah `<td>` per row; layout geser.

### Pitfall 6: `@OrgLabels.GetLabel(0/1)` header label hilang
**What goes wrong:** Re-type header kolom dengan teks statis alih-alih `@OrgLabels.GetLabel(0) Penugasan` (L235-236).
**Why:** Label org dinamis per-tenant.
**How to avoid:** Salin verbatim `@OrgLabels.GetLabel(0) Penugasan` / `@OrgLabels.GetLabel(1) Penugasan` di thead mini-table [VERIFIED: L235-236].
**Warning signs:** Header kolom menunjukkan teks default, bukan label org.

### Pitfall 7: Dead `onclick` removal mengganggu fungsi modal (D-11)
**What goes wrong:** Saat buang `onclick="...&& null"` (L58), tak sengaja ikut buang `data-bs-toggle="modal" data-bs-target="#assignModal"` → tombol mati.
**Why:** Keduanya di elemen yang sama.
**How to avoid:** HAPUS HANYA atribut `onclick`. Pertahankan `data-bs-toggle`/`data-bs-target` (H-9). [VERIFIED: L58].
**Warning signs:** Runtime: klik "Tambah Mapping" → modal tak muncul (test FAIL).

### Pitfall 8: Pagination ter-render di dalam loop card (duplikat)
**What goes wrong:** Pindah blok pagination (L350-372) ke dalam `@foreach` → muncul N kali.
**Why:** Pagination harus SEKALI, setelah seluruh list.
**How to avoid:** Emit pagination SETELAH `@foreach` card ditutup, masih di dalam `else`-block (posisi struktural sama dgn lama, L350 setelah `</table>` L347).
**Warning signs:** Runtime: N nav pagination muncul.

### Pitfall 9 (Phase 354 lesson): grep+build TAK cukup untuk a11y/Razor dinamis
**What goes wrong:** `dotnet build` hijau + grep konfirmasi onclick → asumsi parity OK, ternyata collapse tak toggle / aria salah / modal tak buka di runtime.
**Why:** Razor compile tak menjamin behavior; a11y attribute hanya teruji di browser.
**How to avoid:** WAJIB Playwright runtime assertion (lihat §Validation Architecture). [CITED: STATE/Phase 354 lesson].
**Warning signs:** —(by definition tak terlihat tanpa runtime test).

## Code Examples

### Mini coachee row + Aksi block (SALIN VERBATIM struktur Aksi — D-09)
```html
@* di dalam <tbody> mini-table, loop coachee *@
<tr class="@(coachee.IsActive ? "" : "table-light text-muted")" data-mapping-id="@coachee.Id">
    <td>@coachee.CoacheeName</td>
    <td>@coachee.CoacheeNIP</td>
    <td>@(string.IsNullOrEmpty((string)coachee.AssignmentSection) ? "—" : coachee.AssignmentSection)</td>
    <td>@(string.IsNullOrEmpty((string)coachee.AssignmentUnit) ? "—" : coachee.AssignmentUnit)</td>
    <td>@coachee.CoacheePosition</td>
    <td>
        @if (!string.IsNullOrEmpty((string)coachee.ProtonTrack))
        { <span class="badge bg-info text-dark">@coachee.ProtonTrack</span> }
        else { <span class="text-muted">—</span> }
    </td>
    <td>
        @if (coachee.IsActive) { <span class="badge bg-success">Aktif</span> }
        else { <span class="badge bg-secondary">Non-aktif</span> }
    </td>
    <td>@coachee.StartDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id-ID"))</td>
    <td>
        <div class="d-flex gap-1">
            <button class="btn btn-sm btn-outline-secondary"
                    onclick="openEditModal(@coachee.Id, '@coachee.CoachId', 0, '@coachee.StartDate.ToString("yyyy-MM-dd")', '@coachee.CoacheeName', '@coachee.AssignmentSection', '@coachee.AssignmentUnit')">
                <i class="bi bi-pencil"></i> Edit
            </button>
            @if (coachee.IsCompleted)
            {
                <span class="badge bg-info"><i class="bi bi-award me-1"></i>Graduated</span>
            }
            else if (coachee.IsActive)
            {
                <button class="btn btn-sm btn-outline-danger" onclick="confirmDeactivate(@coachee.Id, '@coachee.CoacheeName')">
                    <i class="bi bi-x-circle"></i> Nonaktifkan
                </button>
                <form method="post" asp-action="MarkMappingCompleted" asp-controller="CoachMapping" class="d-inline">
                    @Html.AntiForgeryToken()
                    <input type="hidden" name="mappingId" value="@coachee.Id" />
                    <button type="submit" class="btn btn-sm btn-outline-info"
                            onclick="return confirm('Tandai coachee ini sebagai graduated?')">
                        <i class="bi bi-award"></i> Graduated
                    </button>
                </form>
            }
            else
            {
                <button class="btn btn-sm btn-outline-success" onclick="reactivateMapping(@coachee.Id)">
                    <i class="bi bi-arrow-repeat"></i> Aktifkan
                </button>
                <button class="btn btn-sm btn-outline-danger" onclick="confirmDelete(@coachee.Id, '@coachee.CoacheeName')">
                    <i class="bi bi-trash me-1"></i> Hapus
                </button>
            }
        </div>
    </td>
</tr>
```
> Source: existing `CoachCoacheeMapping.cshtml` L273-342 [VERIFIED]. **Hanya kolom "Coachee Aktif" (`<td></td>` L300) yang dilepas; sisanya verbatim.**

### Toolbar (DSN-03 — D-10/D-11)
```html
<div class="d-flex gap-2 flex-wrap">
    <div class="btn-group">
        <a href="@Url.Action("DownloadMappingImportTemplate", "CoachMapping")" class="btn btn-sm btn-outline-secondary">
            <i class="bi bi-download me-1"></i>Download Template
        </a>
        <button class="btn btn-sm btn-outline-secondary" data-bs-toggle="modal" data-bs-target="#importMappingModal">
            <i class="bi bi-file-earmark-arrow-up me-1"></i>Import Excel
        </button>
        <a asp-controller="CoachMapping" asp-action="CoachCoacheeMappingExport" class="btn btn-sm btn-outline-secondary">
            <i class="bi bi-file-earmark-excel me-1"></i>Export Excel
        </a>
    </div>
    @* dead onclick DIHAPUS (D-11); modal toggle DIPERTAHANKAN (H-9) *@
    <button class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#assignModal">
        <i class="bi bi-plus-circle me-1"></i>Tambah Mapping
    </button>
</div>
```
> Source: existing L48-61 [VERIFIED]. Warna outline btn-group = Claude's discretion (UI-SPEC mengizinkan; contoh pakai `btn-outline-secondary` untuk keseragaman — boleh `btn-outline-success`/`primary` per pilihan executor selama konsisten).

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Grouped `<table>` + `<tbody class="collapse show">` per coach | Card per coach + `card-header` collapse toggle | Phase 389 (ini) | Visual; behavior parity dipertahankan. |
| Coach header sebagai `<tr class="table-primary">` toggle | `card-header` `role=button` toggle | Phase 389 | id/target pindah; a11y ditingkatkan (role/aria). |
| Default `show` (semua terbuka) | Default closed, independent | Phase 389 (D-05) | Scan banyak coach lebih rapi. |
| Kolom "Coachee Aktif" di tabel | Badge ActiveCount di card-header | Phase 389 (D-07) | 1 kolom dihapus; nilai pindah ke header. |
| Dead `onclick` di Tambah Mapping | Dihapus (modal via `data-bs-toggle`) | Phase 389 (D-11) | Bersih; fungsi sama. |

**Deprecated/outdated:** Tidak ada library deprecated. Bootstrap 5.3.0 + Icons 1.10.0 = stack stabil app (jangan bump).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Bootstrap menambah/menghapus kelas `.collapsed` pada trigger sesuai state (dipakai untuk chevron CSS `:not(.collapsed)`) | Technique / chevron | LOW — perilaku `.collapsed` terdokumentasi Bootstrap 5; bila beda versi, fallback: tie rotate ke `aria-expanded="true"` selector. Diverifikasi runtime di Playwright. |
| A2 | Keyboard Space mengaktifkan toggle pada `<div role=button tabindex=0 data-bs-toggle=collapse>` | Pattern 1 a11y | LOW-MED — Enter umumnya jalan; Space pada `<div>` kadang tidak tanpa handler. **Mitigasi: render header sebagai `<button>` real** (UI-SPEC mengizinkan) → Enter+Space native. Assert runtime. |
| A3 | Tidak ada CSS app yang menarget `tbody.collapse`/`tr.table-primary` secara spesifik di `site.css` (sehingga penghapusan struktur lama tak meninggalkan style yatim) | Pitfalls | LOW — file ini self-contained Bootstrap utility; bila ada style global, tampak di runtime visual check. |

**Catatan:** Tabel ini TIDAK kosong — 3 asumsi LOW/LOW-MED, semua ter-mitigasi via Playwright runtime assertion + opsi `<button>` real. Semua klaim parity (H-1..H-23) = VERIFIED, bukan assumed.

## Open Questions

1. **Header sebagai `<div role=button>` vs `<button>` real?**
   - What we know: Keduanya valid; `<button>` lebih kuat a11y (Enter+Space+focus native), `<div>` lebih fleksибel styling tanpa reset button default.
   - What's unclear: Apakah `<button>` full-width dgn flex children + badge + avatar memerlukan reset (`.btn` base / `w-100 text-start border-0`).
   - Recommendation: Coba `<button type="button" class="card-header w-100 text-start border-0 bg-white d-flex ...">`. Bila styling rewel, fallback `<div role=button tabindex=0>` + assert Space via Playwright. **Keputusan = Claude's discretion; tetapkan di plan, validasi runtime.**

2. **Warna outline btn-group Excel (D-10 discretion).**
   - What we know: UI-SPEC mengizinkan keseragaman outline; tombol lama campur `outline-success`/`outline-primary`.
   - Recommendation: Seragamkan ke satu warna outline (mis. `btn-outline-secondary` netral) agar "Tambah Mapping" `btn-primary` jadi satu-satunya aksen — paling konsisten dgn Color contract UI-SPEC. Non-blocking.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (`dotnet build`/`run`) | gate verifikasi lokal (CLAUDE.md) | ✓ (repo aktif) | repo TFM | — |
| Bootstrap 5 + Icons (CDN) | render | ✓ (CDN `_Layout`) | 5.3.0 / 1.10.0 | — |
| Playwright (`tests/`) | runtime parity assert | ✓ | config `tests/playwright.config.ts` | — |
| App lokal `localhost:5277` | Playwright baseURL | ✓ (manual `dotnet run`) | — | app TIDAK auto-start (no `webServer` di config) — harus `dotnet run` manual dgn `Authentication__UseActiveDirectory=false` |
| SQL Server lokal (login admin) | login Playwright | ✓ | — | `--workers=1` WAJIB (NTLM loopback/shared-mem conn, ref: local e2e SQL env fix) |

**Missing dependencies with no fallback:** Tidak ada — semua tersedia.
**Catatan run:** `playwright.config.ts` TIDAK punya `webServer` → app harus dijalankan manual sebelum test. Login pakai `/Account/Login` (bukan `/Account/Login` legacy lain) per sibling spec 388 — **verifikasi route login** saat menulis spec (spec 388 pakai `/Account/Login`, helper-login lama pakai itu). [VERIFIED: coachworkload-388.spec.ts L23].

## Validation Architecture

> nyquist_validation = **true** (config.json L15) → section ini WAJIB. Strategi = Playwright runtime parity (Phase 354 lesson: grep+build TAK cukup). Template = `tests/e2e/coachworkload-388.spec.ts` (sibling, pola login + parity + data-skip).

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright Test (`@playwright/test`) — `tests/playwright.config.ts` |
| Config file | `tests/playwright.config.ts` (baseURL `http://localhost:5277`, `fullyParallel:false`, project `chromium` deps `setup`) |
| Quick run command | `cd tests; npx playwright test coachcoacheemapping-389 --workers=1` |
| Full suite command | `cd tests; npx playwright test --workers=1` |
| App prerequisite | `dotnet run` dgn env `Authentication__UseActiveDirectory=false` (login admin lokal), app TIDAK auto-start |
| C# build gate | `dotnet build` 0 error (Razor compile) |
| Login helper | `loginAny(page, 'admin')` via `/Account/Login` + `accounts.admin` (admin@pertamina.com / 123456) [VERIFIED: accounts.ts L2] |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DSN-01 | Tiap coach = 1 `.card`; header berisi avatar `.avatar-initial`, nama, badge | unit/e2e | `npx playwright test coachcoacheemapping-389 -g "card per coach + header"` | ❌ Wave 0 |
| DSN-01 | Badge warna threshold: `<5`→`bg-info`, `>=5`→`bg-warning`, `>=8`→`bg-danger` | e2e | `... -g "badge threshold"` | ❌ Wave 0 |
| DSN-02 | Default ALL CLOSED; klik header → collapse `.show`; klik lagi → tutup | e2e | `... -g "collapse buka tutup"` | ❌ Wave 0 |
| DSN-02 | Card INDEPENDEN (buka A tak menutup B — no data-bs-parent) | e2e | `... -g "independent multi-open"` | ❌ Wave 0 |
| DSN-02 | Mini-table 9 kolom; "Coachee Aktif" TIDAK ada | e2e | `... -g "9 kolom"` | ❌ Wave 0 |
| DSN-02 | a11y header: `role=button`/`<button>`, `aria-expanded` toggle, `aria-controls`, keyboard Enter+Space | e2e | `... -g "a11y header toggle"` | ❌ Wave 0 |
| DSN-03 | Toolbar: "Tambah Mapping" `btn-primary` solo; Excel grouped; semua `btn-sm` | e2e | `... -g "toolbar seragam"` | ❌ Wave 0 |
| DSN-03 | Dead `onclick` hilang; "Tambah Mapping" tetap buka `#assignModal` | e2e | `... -g "tambah mapping buka modal"` | ❌ Wave 0 |
| DSN-06* | Edit → `#editModal` muncul + field terisi (`openEditModal` 7 arg) | e2e | `... -g "edit modal"` | ❌ Wave 0 |
| DSN-06* | Hapus → `#deleteModal` muncul; submit → `tr[data-mapping-id]` hilang dari DOM | e2e (data-guard) | `... -g "delete hapus row"` | ❌ Wave 0 |
| DSN-06* | Nonaktifkan/Aktifkan/Graduated tombol terrender sesuai state (IsCompleted DULU) | e2e | `... -g "aksi branch"` | ❌ Wave 0 |
| DSN-06* | AJAX pakai `appUrl` → request URL ber-prefix basePath (intercept `**/Admin/CoachCoacheeMapping*`) | e2e (route intercept) | `... -g "ajax appUrl subpath"` | ❌ Wave 0 |
| DSN-06* | Filter Seksi + Cari + Tampilkan Semua + pagination tetap jalan | e2e | `... -g "filter pagination"` | ❌ Wave 0 |

> *DSN-06 parity verifikasi PENUH = Phase 390. Di Phase 389, spec ini = **smoke parity** (struktur hook hadir + collapse/modal buka). Aksi mutasi data penuh (assign/import/export end-to-end) didorong ke Phase 390 / UAT bila DB lokal kurang data — pakai pola `test.skip` sibling 388 (L73).

### Concrete Playwright Parity Test Plan (per existing action — enumerasi assertion)
1. **Card render (DSN-01):** `page.locator('.card.shadow-sm')` count == jumlah coach group; tiap card punya `.avatar-initial` + nama coach text.
2. **Badge threshold (DSN-01):** untuk card dgn ActiveCount diketahui (atau cek kelas badge cocok dgn angka): assert badge `.bg-info`/`.bg-warning`/`.bg-danger` sesuai threshold 5/8. (Bila data tak deterministik → assert badge ADA + kelasnya salah satu dari 3, dan korelasi angka↔kelas via `evaluate`.)
3. **Default closed (DSN-02):** `page.locator('.collapse.show')` count == 0 saat load; tiap header `aria-expanded="false"`.
4. **Toggle open (DSN-02):** klik card-header pertama → `#collapse-0` punya `.show` (atau `toBeVisible`); `aria-expanded` jadi `"true"`; chevron rotated (assert transform via computed style atau kelas `:not(.collapsed)`).
5. **Toggle close:** klik lagi → collapse hilang `.show`; `aria-expanded="false"`.
6. **Independent (DSN-02):** buka card 0 dan card 1 → KEDUA `.show` hadir bersamaan (buktikan no `data-bs-parent`). Skip bila <2 coach.
7. **9 kolom (DSN-02/D-07):** dalam card terbuka, `thead th` count == 9; tak ada `th` bertuliskan "Coachee Aktif".
8. **a11y keyboard (Phase 354):** focus header (`page.locator(header).focus()`), tekan `Enter` → terbuka; tekan `Space` → toggle. Assert `role="button"` atau tagName `BUTTON`; `aria-controls` == id body.
9. **Edit modal (parity):** dalam card terbuka, klik tombol Edit baris pertama → `#editModal` visible; `#editCoacheeName` ter-set (bukti `openEditModal` jalan).
10. **Delete row removal (parity H-1):** intercept/route POST delete ATAU (bila data disposable) klik Hapus → confirm modal → Hapus Permanen → assert `tr[data-mapping-id="<id>"]` hilang dari DOM. **Bila tak ada data disposable → assert hanya `#deleteModal` buka + tombol submit ada** (mutasi penuh = Phase 390).
11. **Toolbar (DSN-03):** "Tambah Mapping" = `.btn-primary`; ketiga tombol Excel `.btn-sm` dalam `.btn-group`; tak ada atribut `onclick` mengandung `querySelector('[data-bs-dismiss]')` di tombol Tambah Mapping (`page.locator('button:has-text("Tambah Mapping")')` → `getAttribute('onclick')` == null/empty).
12. **Tambah Mapping buka modal (DSN-03/H-9):** klik → `#assignModal` visible.
13. **AJAX appUrl sub-path (parity D-12):** `page.route('**/Admin/CoachCoacheeMappingDeletePreview*', ...)` → trigger confirmDelete → assert request URL berisi basePath prefix (lokal basePath kosong → minimal assert path `/Admin/CoachCoacheeMappingDeletePreview` ter-hit). Verifikasi tak 404.
14. **Filter/pagination (parity):** ubah dropdown Seksi → `resetPageAndSubmit` submit form → URL berubah `section=`. (Pola sibling 388 L103-122.)

### Sampling Rate
- **Per task commit:** `dotnet build` (Razor 0 error) + `npx playwright test coachcoacheemapping-389 -g "<task-scope>" --workers=1`.
- **Per wave merge:** `npx playwright test coachcoacheemapping-389 --workers=1` (full spec phase).
- **Phase gate:** `dotnet build` 0 error + spec 389 hijau + visual smoke (`dotnet run` localhost:5277 — card render, collapse, modal). Parity PENUH (semua aksi mutasi + import/export) = Phase 390.

### Wave 0 Gaps
- [ ] `tests/e2e/coachcoacheemapping-389.spec.ts` — covers DSN-01/02/03 + smoke parity (model dari `coachworkload-388.spec.ts`).
- [ ] (opsional) helper login sudah ada (`tests/helpers/accounts.ts` + pola `loginAny` inline di spec 388) — **reuse, tak perlu buat baru**.
- [ ] Framework install: tidak perlu — Playwright sudah terpasang (`tests/` aktif, 28 spec existing) [VERIFIED: Glob spec list].

## Project Constraints (from CLAUDE.md)

> Diperlakukan dgn otoritas setara locked decision. Planner WAJIB patuh.

1. **Bahasa:** Selalu respons & UI dalam Bahasa Indonesia. Label UI = verbatim existing (D-08).
2. **Develop Workflow (WAJIB):** Lokal → Dev (10.55.3.3) → Prod. Phase ini:
   - Fix/develop di **lokal** saja. Verifikasi lokal WAJIB: `dotnet build` + `dotnet run` (cek `http://localhost:5277`) + Playwright + cek DB lokal bila relevan.
   - **❌ JANGAN edit kode/DB langsung di Dev/Prod.** **❌ JANGAN push tanpa verifikasi lokal.**
   - Commit & push (sertakan migration bila ada — **phase ini migration=FALSE**).
   - Promosi ke Dev = tanggung jawab Team IT. Notify IT dgn commit hash + flag migration (=FALSE).
3. **Seed Data Workflow:** Bila butuh seed untuk test — klasifikasi dulu (`temporary+local-only` vs `permanent+prod`), snapshot DB lokal, catat di `docs/SEED_JOURNAL.md`, restore + tandai `cleaned` setelah test. ❌ Jangan biarkan seed temporary nempel. (Untuk phase ini: hindari seed; pakai data existing + pola `test.skip` bila data kurang, seperti sibling 388.)

## Sources

### Primary (HIGH confidence)
- `Views/Admin/CoachCoacheeMapping.cshtml` (1-1059) — target file, dibaca penuh [parity inventory, semua field & hook].
- `Controllers/CoachMappingController.cs` L1-45 — `[Route("Admin/[action]")]` + override View resolution [routing parity].
- `Views/Admin/ManageWorkers.cshtml` L244-263 — avatar-initial idiom verbatim (D-03).
- `Views/Shared/_Layout.cshtml` L36-56 — Bootstrap 5.3.0 + Icons 1.10.0 CDN, `basePath`/`appUrl` PathBase helper, `@section Styles` slot.
- `tests/e2e/coachworkload-388.spec.ts` — sibling parity spec (template login + parity + data-skip).
- `tests/playwright.config.ts` + `tests/helpers/accounts.ts` — run mechanics + admin credentials.
- `.planning/config.json` — `nyquist_validation:true`, `ui_phase:true`.
- `.planning/phases/389-.../389-CONTEXT.md` + `389-UI-SPEC.md` — locked decisions D-01..D-14 + UI contract.
- `.planning/REQUIREMENTS.md` + `ROADMAP.md` — DSN-01/02/03 acceptance + success criteria.

### Secondary (MEDIUM confidence)
- Bootstrap 5.3 collapse component behavior (`.collapsed` class on trigger, `aria-expanded` auto-toggle, non-button trigger via `data-bs-toggle`) [CITED: getbootstrap.com/docs/5.3/components/collapse — training knowledge konsisten dgn dokumentasi; A1/A2 di Assumptions Log untuk diverifikasi runtime].

### Tertiary (LOW confidence)
- (tidak ada — semua klaim verified atau cited dgn mitigasi runtime).

## Metadata

**Confidence breakdown:**
- Parity contract inventory (H-1..H-23): **HIGH** — semua dibaca langsung dari file + cross-ref JS↔markup.
- Standard stack: **HIGH** — versi dikonfirmasi dari CDN URL `_Layout`.
- Architecture/collapse technique: **HIGH** (markup) / **MEDIUM** (chevron `.collapsed` + keyboard Space — Assumptions A1/A2, ter-mitigasi runtime).
- Data shape (no controller change): **HIGH** — enumerasi field render lengkap.
- Validation architecture: **HIGH** — template sibling spec nyata + run mechanics terverifikasi.
- Pitfalls: **HIGH** — diturunkan dari ketergantungan kode aktual.

**Research date:** 2026-06-17
**Valid until:** ~30 hari (stack Bootstrap 5.3 stabil; file target bisa berubah bila phase lain menyentuhnya — file terisolasi di milestone v32.1 jadi risiko rendah).
