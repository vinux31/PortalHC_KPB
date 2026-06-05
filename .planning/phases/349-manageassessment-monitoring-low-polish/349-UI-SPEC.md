---
phase: 349
slug: manageassessment-monitoring-low-polish
status: draft
shadcn_initialized: false
preset: none
created: 2026-06-05
---

# Phase 349 — UI Design Contract

> Kontrak visual & interaksi untuk polish ManageAssessment + Monitoring (23 item MAP). Dibuat oleh gsd-ui-researcher, diverifikasi gsd-ui-checker.
>
> **Sifat phase:** PURE POLISH pada Razor view EXISTING (ASP.NET Core 8 MVC + Bootstrap 5.3 + HTMX). BUKAN desain UI baru. Design system sudah ada — kontrak ini MENGUNCI string/atribut/markup spesifik agar planner & executor konsisten, MATCH pola existing (Phase 347 + Phase 348). Tidak ada komponen baru, tidak ada token baru, tidak ada migration. Semua decision sudah locked di 349-CONTEXT.md (D-01..D-04 + Discretion) + 349-RESEARCH.md — ZERO pertanyaan terbuka ke user.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (ASP.NET Core 8 MVC Razor — bukan React/Next/Vite, shadcn gate N/A) |
| Preset | not applicable |
| Component library | Bootstrap 5.3 (card, badge, modal, dropdown, collapse, placeholder-glow) |
| Icon library | Bootstrap Icons (`bi-*`) |
| Font | inherit `_Layout` (default Bootstrap system stack — TIDAK diubah phase ini) |
| Interaksi | HTMX 2.0.x (tab partial swap + filter/pagination re-fetch); jQuery $.ajax (regenerate/reshuffle); SignalR (Monitoring Detail live push) |
| i18n | inline Bahasa Indonesia di Razor (BUKAN resource file — konvensi codebase, D out-of-scope .resx) |

**Aturan inti:** Match pola yang SUDAH ADA. Jangan invent. Reuse konstanta `AssessmentConstants.AssessmentStatus.*`, helper `PaginationHelper`, badge class Bootstrap existing, dan konvensi i18n/a11y Phase 347.

---

## Spacing Scale

Phase ini TIDAK mendeklarasikan spacing token baru — semua spacing pakai utility Bootstrap existing (`g-3`, `py-3`, `py-5`, `mb-4`, `me-1`, `ms-1`, `mt-2`). Skala 4px Bootstrap (`*-1`=4px, `*-2`=8px, `*-3`=16px, `*-4`=24px, `*-5`=48px) sudah multiple-of-4.

| Token | Value | Usage (existing Bootstrap utility) |
|-------|-------|-------|
| xs | 4px | `me-1`/`ms-1` gap ikon-teks (chevron, badge) |
| sm | 8px | `gap-2` antar tombol toolbar |
| md | 16px | `g-3` gutter summary card row, `py-3` card padding |
| lg | 24px | `mb-4` jarak antar section |
| xl | 48px | `py-5` padding vertikal empty-state |

Exceptions: tidak ada. Skeleton loader (MAP-09) HARUS pakai lebar/jumlah kolom yang IDENTIK dengan tabel asli agar zero layout-shift (lihat §Skeleton Loader Contract).

---

## Typography

Phase ini TIDAK mendeklarasikan font baru — pakai kelas Bootstrap existing pada elemen yang disentuh. Dikunci agar 2 kartu baru (MAP-10) + empty-state baru konsisten dengan pola existing.

**Tabel ini hanya mengunci elemen BARU/BERUBAH di Phase 349 (2 kartu summary + empty-state). Elemen existing Bootstrap (`<th>`, badge) tidak dideklarasikan ulang — pakai default Bootstrap apa adanya.**

| Role | Size | Weight | Kelas Bootstrap |
|------|------|--------|-------------|
| Angka kartu summary | `fs-3` (~1.75rem) | bold (700) | `fs-3 fw-bold text-{color}` |
| Label kartu summary + empty-state body | `small` (~0.875rem) | regular (400) | `text-muted small` |
| Empty-state heading | `h5` (~1.25rem) | regular (400) | `h5 text-muted` |

→ 3 ukuran (`fs-3`, `small`, `h5`), 2 berat (bold 700 untuk angka kartu, regular 400 untuk label/body/heading).

Line-height: inherit Bootstrap default (body 1.5). Tidak diubah.

---

## Color

Phase TIDAK mengubah palette. Pakai variabel semantik Bootstrap existing. Yang DIKUNCI di sini: pemetaan warna untuk badge status + 2 kartu summary baru (MAP-10), agar konsisten dengan badge yang sudah dipakai (Phase 345 "Menunggu Penilaian" amber).

| Role | Value (Bootstrap semantic) | Usage |
|------|-------|-------|
| Dominant (60%) | `#fff` / `bg-white` | Background page, card body, card-header |
| Secondary (30%) | `#f8f9fa` / `bg-light`, `table-dark` header | Card surface, sticky table header, skeleton placeholder |
| Accent (10%) | `text-primary` `#0d6efd` | HANYA: angka kartu "Total Ditugaskan", ikon section, tombol primer "View Detail" |
| Destructive | `btn-danger` `#dc3545` | HANYA: tombol "Akhiri Semua Ujian" (MAP-12) — aksi massal akhiri ujian |

Accent reserved for: angka kartu Total, ikon header tabel/section, link primer. JANGAN pakai accent untuk semua tombol.

### Status → warna (KUNCI — konstanta, bukan literal)

| Status | Badge / Kartu class | Konstanta sumber |
|--------|---------------------|------------------|
| Completed / Selesai | `bg-success` / `text-success` | `AssessmentStatus.Completed` |
| In Progress / Sedang Mengerjakan | `bg-warning text-dark` / `text-warning` | `"InProgress"` (UserStatus) |
| Not started / Belum Mulai | `bg-secondary` / `text-secondary` | `Model.PendingCount` |
| Dibatalkan (Cancelled) | `bg-secondary` / `text-secondary` | `AssessmentStatus.Cancelled` |
| **Abandoned** (kartu BARU MAP-10) | `bg-dark` / `text-dark` (muted/gelap) | `"Abandoned"` (DeriveUserStatus) |
| **Menunggu Penilaian** (kartu BARU MAP-10 + badge MAP-18/20) | `bg-warning text-dark` / `text-warning` (amber) | `AssessmentConstants.AssessmentStatus.PendingGrading` (D-C — WAJIB konstanta) |

**Anti-pattern (DILARANG):** literal string `"Menunggu Penilaian"` atau `"Cancelled"` di kode baru — WAJIB konstanta `AssessmentConstants.AssessmentStatus.*` (D-C). "Abandoned" boleh literal (belum ada konstanta), tapi sumber derivasi = `DeriveUserStatus`.

---

## Copywriting Contract

Inline Bahasa Indonesia. Tabel ini = string LOCK. Executor wajib pakai persis (case-sensitive).

### i18n string lock — Monitoring Detail (MAP-01)

| Lokasi | EN (sebelum) | ID (LOCK) |
|--------|--------------|-----------|
| Header tabel | Name | **Nama** |
| Header tabel | Progress | **Progres** |
| Header tabel | Status | Status (tetap) |
| Header tabel | Score | **Nilai** |
| Header tabel | Result | **Hasil** |
| Header tabel | Completed At | **Selesai Pada** |
| Header tabel | Actions | **Aksi** |
| Tombol back | Back to Monitoring | **Kembali ke Monitoring** |
| Heading section | Per-User Status | **Status Per-Peserta** |
| Tombol export | Export Results | **Ekspor Hasil** |
| Empty tabel | No sessions found | **Belum ada sesi** |

### i18n string lock — summary cards (MAP-01 + MAP-10, set lengkap 7 kartu)

Layout: `Total Ditugaskan = Selesai + Sedang Mengerjakan + Belum Mulai + Dibatalkan + Abandoned + Menunggu Penilaian` (angka pas — D-03). Lihat §Summary Card Layout untuk struktur penuh.

| Kartu | Label ID (LOCK) | Warna angka |
|-------|-----------------|-------------|
| Total | **Total Ditugaskan** | `text-primary` |
| Completed | **Selesai** | `text-success` |
| In Progress | **Sedang Mengerjakan** | `text-warning` |
| Not Started | **Belum Mulai** | `text-secondary` |
| Cancelled | **Dibatalkan** | `text-secondary` |
| Abandoned (BARU) | **Abandoned** | `text-dark` |
| Pending (BARU) | **Menunggu Penilaian** | `text-warning` |

### i18n string lock — label identitas (MAP-02)

| Lokasi | Sebelum | LOCK |
|--------|---------|------|
| `_HistoryTab.cshtml` Training header L178 | Nopeg | **NIP** |
| `_HistoryTab.cshtml` Training placeholder L159 | Nopeg | **NIP** (placeholder: `Cari nama atau NIP...`) |
| Assessment sub-tab (header L62 + placeholder L33) | — | sudah "NIP" — no-op verify |

### i18n string lock — Monitoring list (MAP-14/16/23)

| Lokasi | Aksi | LOCK |
|--------|------|------|
| `AssessmentMonitoring.cshtml:27` subtitle | Buang frasa "real-time" (list tak ada SignalR) | subtitle tanpa "real-time" |
| `AssessmentMonitoring.cshtml:256-259` | Hapus muted subtitle kategori dobel (badge L271 dipertahankan) | (dihapus) |
| `AssessmentMonitoring.cshtml:75` placeholder search | Extend scope Category | **`Cari nama atau kategori assessment...`** |

### Empty-state & feedback copy (MAP-05/06/07/08)

Pola visual MATCH existing empty-state (`text-center py-5`, ikon `bi-inbox` ~4rem `text-muted`, heading `h5 text-muted`, body `text-muted small`).

| Element | Copy (LOCK) | Konteks |
|---------|-------------|---------|
| Empty filter-aware Tab1 (MAP-05) — heading | **Tidak ada assessment untuk filter ini** | kategori/status aktif + search kosong (BUKAN "Buat assessment pertama") |
| Empty filter-aware Tab1 (MAP-05) — body | Coba ubah atau reset filter untuk melihat assessment lain. | cabang filter-aware |
| Tombol reset di empty-state (MAP-06 / D-01) | **Reset Semua Filter** | hapus search + kategori + status sekaligus (BUKAN "Hapus Pencarian") — jujur |
| 0-match client filter Tab3 (MAP-07) | **Tidak ada hasil untuk filter ini.** | inject baris saat semua row `display:none`; container `aria-live="polite"` (assessment + training) |
| Counter visible-row Tab3 (MAP-08) | **Menampilkan {X} dari {Y}** | X=visible pasca-filter, Y=total; update via JS host page |
| Empty Pre-Post/standard Monitoring (existing) | (tidak diubah) | — |

### Primary CTA / aksi (per surface)

| Surface | Primary CTA (LOCK) | Sifat |
|---------|--------------------|-------|
| Monitoring Detail | **Kembali ke Monitoring** (link sekunder) + **Ekspor Hasil** (`btn-outline-success`) | navigasi/ekspor |
| Monitoring Detail (destruktif) | **Akhiri Semua Ujian** (`btn-danger`) | conditional render (MAP-12) |
| Monitoring list — Pre-Post Aksi (MAP-17) | **View Detail** (item) + **Regenerate Token** (item `text-warning`) | dropdown |

### Destructive confirmation (MAP-12 — conditional render + modal wording)

| Action | Copy (LOCK) | Confirmation approach |
|--------|-------------|----------------------|
| Akhiri Semua Ujian | Tombol render HANYA bila `@Model.InProgressCount > 0 || Model.GroupStatus == "Open"`. Modal wording "{X} belum mulai" memakai predikat IDENTIK dengan aksi cancel (jangan divergen). | Bootstrap modal konfirmasi existing (`#akhiriSemuaModal`), populate count via `GetAkhiriSemuaCounts` |
| Regenerate Token (MAP-17) | Reuse handler `RegenerateToken` existing (sudah LinkedGroupId-aware Phase 348 MAM-01) + `[ValidateAntiForgeryToken]` | konfirmasi JS existing `.btn-regenerate-token` — tidak ubah |

---

## Summary Card Layout (MAP-10 — KONTRAK KRITIS)

**Intent (D-03):** Total = jumlah semua kartu ("angka pas"). `DeriveUserStatus` (Phase 348 MAM-04) menghasilkan 6 status bucket: Menunggu Penilaian / Completed / Dibatalkan / Abandoned / InProgress / Not started. Kartu existing hanya 5 → 2 bucket (Abandoned + Menunggu Penilaian) tak terhitung → Total ≠ sum saat ada essay pending atau sesi abandoned.

**Resolusi (LOCK):** Render set LENGKAP 7 kartu. Tambah 2 kartu baru.

```
[ Total Ditugaskan ]  [ Selesai ]  [ Sedang Mengerjakan ]  [ Belum Mulai ]  [ Dibatalkan ]  [ Abandoned ]  [ Menunggu Penilaian ]
       primary          success         warning               secondary       secondary        dark           warning
```

Invariant: `Total Ditugaskan == Selesai + Sedang Mengerjakan + Belum Mulai + Dibatalkan + Abandoned + Menunggu Penilaian`.

### Struktur kartu (MATCH existing — verbatim pola L146-176)

Setiap kartu = `<div class="col"><div class="card border-0 shadow-sm text-center py-3"><div class="fs-3 fw-bold text-{color}" id="count-{key}">@Model.{Count}</div><div class="text-muted small">{Label}</div></div></div>`.

| Kartu | id (untuk JS sync) | Sumber angka | Catatan implementasi |
|-------|--------------------|--------------|----------------------|
| Total | `count-total` | `@Model.TotalCount` | existing |
| Selesai | `count-completed` | `@Model.CompletedCount` | existing |
| Sedang Mengerjakan | `count-inprogress` | `@Model.InProgressCount` | **MAP-11**: ganti inline LINQ L161 → field `InProgressCount` |
| Belum Mulai | `count-notstarted` | `@Model.PendingCount` | existing |
| Dibatalkan | `count-cancelled` | `@Model.CancelledCount` | existing |
| **Abandoned** | `count-abandoned` | `@Model.AbandonedCount` | **BARU** — tambah field ViewModel + assign di Detail action (controller ~L3320-3336) |
| **Menunggu Penilaian** | `count-pending` | `@Model.MenungguPenilaianCount` | **BARU di Detail** — field SUDAH ada di ViewModel (L22) tapi TIDAK di-assign di Detail action; tambah assign |

### JS `updateSummaryFromDOM` sync (MAP-10 — L1281-1300)

JS live-update (dipanggil saat SignalR `workerStarted`/`workerSubmitted`) saat ini `else → notStarted++` (semua status non-explicit jatuh ke Not Started). Tambah cabang agar Abandoned + Menunggu Penilaian tidak salah-hitung:

```js
else if (text === 'Abandoned') abandoned++;
else if (text === '@AssessmentConstants.AssessmentStatus.PendingGrading') pending++;
else notStarted++;
```
+ update elemen `count-abandoned` dan `count-pending`. Tanpa ini, kartu live salah saat push SignalR (Pitfall 5).

### Catatan layout

- 7 kartu pakai `.col` auto-distribute existing dalam `<div class="row g-3 mb-4">`. 7 kolom dalam 1 baris bisa sempit di layar kecil; Bootstrap `.col` akan wrap otomatis. JIKA terlalu sempit, executor boleh tambah `row-cols-2 row-cols-md-4 row-cols-xl-7` untuk wrap rapi (md: 4/baris, xl: 7/baris) — tetap pakai utility Bootstrap, JANGAN custom CSS. Default: pertahankan `.col` existing.

---

## A11y Contract (MAP-03/04 — match konvensi Phase 347)

### Chevron rotate + aria-label toggle collapse (MAP-03)

CSS-only rotate via `[aria-expanded="true"]` (Bootstrap auto-set `aria-expanded` pada `data-bs-toggle="collapse"`). Lebih simpel & bebas-bug dari JS toggle.

```css
.toggle-chevron .chevron-icon { transition: transform 0.2s; }
.toggle-chevron[aria-expanded="true"] .chevron-icon { transform: rotate(180deg); }
```

| Lokasi | Aksi |
|--------|------|
| Tab1 "N peserta" (`_AssessmentGroupsTab.cshtml:232-237`) | Tambah `aria-label="Tampilkan/sembunyikan {N} peserta"` + ikon chevron `bi-chevron-down chevron-icon` + class `.toggle-chevron` |
| Tab2 expand-records (`_TrainingRecordsTab.cshtml:249-254`) | Chevron `bi-chevron-down` STATIS sudah ada → tambah CSS rotate + `aria-label` deskriptif ("Tampilkan/sembunyikan rekam jejak {nama}") |

**Catatan A1 (assumption):** Bootstrap 5 auto-set `aria-expanded` saat collapse toggle. Existing Monitoring-list `.ppt-expand-btn` pakai JS toggle (bukan CSS) — executor WAJIB verifikasi `aria-expanded` ter-set saat plan/execute. Fallback: JS toggle ala Monitoring-list bila CSS tak bereaksi.

### Drop ARIA nested-interactive (MAP-04)

Tab3 Riwayat Assessment: `<tr role="link" tabindex="0">` membungkus `<a>Lihat</a>` = nested-interactive (a11y violation). Pilih SATU affordance.

| Aksi (LOCK) |
|-------------|
| Drop `role="link"` + `tabindex="0"` + `aria-label` + `Html.Raw(...)` dari `<tr>` (`_HistoryTab.cshtml:78-81`) |
| Simpan tombol/link **"Lihat"** (L112) sebagai satu-satunya affordance interaktif |
| Hapus JS click/keydown row-navigation (`_HistoryTab.cshtml:132-147`) + CSS dead `tr.cil03-row-link` (L127-131) — kalau tidak, row tetap clickable tanpa role (UX inkonsisten) |

Bonus security: drop `Html.Raw` mengurangi surface XSS (sejalan konvensi Phase 347).

### Reuse konvensi a11y Phase 347 (untuk konsistensi cross-phase)

- Tombol non-submit → `type="button"` (cegah implicit submit).
- Modal (jika disentuh) → `role="dialog"` + `aria-labelledby` + btn-close `aria-label="Tutup"`.
- Pagination aktif → `aria-current="page"` (kondisi identik dengan `.active`).
- Filter input → `<label for=...>` visible. (Phase 349 tidak nambah filter baru; pertahankan yang ada.)

---

## Skeleton Loader Contract (MAP-09 — match kolom asli, zero layout-shift)

Skeleton placeholder (Bootstrap `placeholder-glow`) HARUS sama jumlah filter & kolom dengan konten asli, agar tidak flash layout-shift saat HTMX swap.

| Skeleton | Filter (asli) | Kolom tabel (asli) |
|----------|---------------|---------------------|
| Tab2 Training (`ManageAssessment.cshtml:149,156-165`) | **5** (section / category / unit / status / search) | **7** (No / Nama / NIP / Jabatan / Unit / Status Training / Aksi) |
| Tab3 History — Assessment | 2 (OK) | **8** (sesuai thead `_HistoryTab.cshtml:60-70`) |
| Tab3 History — Training | 2 (OK) | **5** (sesuai thead `_HistoryTab.cshtml:176-182`) |

Executor: hitung kolom REAL dari thead aktual (bukan dari spec line-drift). Skeleton lebih pendek/sempit dari tabel asli = bug (Pitfall 6).

---

## Display Binding & Conditional Render Contract (MAP-11/12/13/15/17/18/19/22/23)

Bukan visual murni, tapi mempengaruhi apa yang tampil. Dikunci agar tampilan benar.

| MAP | Kontrak tampilan (LOCK) |
|-----|--------------------------|
| MAP-11 | Kartu "Sedang Mengerjakan" bind `@Model.InProgressCount` (bukan inline LINQ). Drop dead var `completedPct`/`passRatePct` (L33-39) — JANGAN surface jadi progress bar baru (minimal-risk). |
| MAP-12 | Tombol "Akhiri Semua Ujian" conditional `@if (Model.InProgressCount > 0 || Model.GroupStatus == "Open")`. Modal wording predikat identik aksi cancel. |
| MAP-13 | Progress bar Monitoring list bisa capai 100%: `TotalCount = g.Count(a => a.Status != AssessmentConstants.AssessmentStatus.Cancelled)` (exclude Cancelled). Tambah `CancelledCount` parity ke standardGroups (saat ini MISSING). Pre-Post path apply ke `postSubs`/`preSubs` count, bukan `g`. |
| MAP-15 | Dropdown Status Monitoring list jangan misrepresent: saat search broaden scope (Closed ikut muncul), set `status="All"` / `ViewBag.SelectedStatus="All"`. Jangan tampilkan "Open+Upcoming" padahal Closed muncul. |
| MAP-17 | Pre-Post group Aksi `<td>` (saat ini kosong) → dropdown: **View Detail** + (bila `IsTokenRequired`) divider + **Regenerate Token** (`text-warning`, `data-id="@group.RepresentativeId"`). Reuse handler + JS existing. |
| MAP-18 | Tab2 manual-assessment tri-state: `IsPassed==null → "Menunggu Penilaian"` (konstanta D-C). Detail cell: `Score - {Lulus / Tidak Lulus / Menunggu Penilaian}`. Status badge null → `bg-warning text-dark`. |
| MAP-19 | Tab2 "Status Training" badge selalu render `CompletionDisplayText` (konsisten, BUKAN gated "Belum ada"). |
| MAP-20 | Tab3 History badge pending — **SUDAH DONE Phase 346 REC-07** (`_HistoryTab.cshtml:102-106`). NO-OP: verifikasi tidak ada double-badge. Jangan duplikat logic (D-D). |
| MAP-22 | Drop param mati `page`/`pageSize`/`statusFilter` di `ManageAssessmentTab_History` (signature) + cleanup wiring `urlHistory` (`ManageAssessment.cshtml`). Training `page`/`pageSize` SEKARANG DIPAKAI (Phase 348 MAM-07) — JANGAN drop. |
| MAP-23 | Search Monitoring list extend ke Category (`|| a.Category.ToLower().Contains(lower)`). Nama/NIP TIDAK (list aggregate, OUT OF SCOPE). Update placeholder (lihat copywriting). |

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| (none) | — | not applicable — bukan proyek shadcn; semua UI Bootstrap 5 vendored existing, zero third-party block, zero dependency baru |

Tidak ada registry pihak ketiga. Tidak ada `components.json`. Gate vetting N/A.

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS — string ID di-lock (Monitoring Detail header/kartu, NIP, empty-state, Reset Semua Filter, 0-match, Menampilkan X dari Y)
- [ ] Dimension 2 Visuals: PASS — 7-kartu set lengkap + skeleton match kolom + chevron rotate match Phase 347
- [ ] Dimension 3 Color: PASS — status→warna dikunci via konstanta; accent reserved; Abandoned dark, Pending amber
- [ ] Dimension 4 Typography: PASS — 3 ukuran (`fs-3`, `small`, `h5`) + 2 berat (bold 700, regular 400); hanya kunci elemen BARU/BERUBAH (kartu + empty-state), existing Bootstrap default tidak dideklarasi ulang
- [ ] Dimension 5 Spacing: PASS — pakai utility Bootstrap existing (multiple-of-4), zero token baru
- [ ] Dimension 6 Registry Safety: PASS — N/A (zero registry, zero dependency baru)

**Approval:** pending
