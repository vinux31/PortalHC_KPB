# UI Review — CoachingProton Page

**Halaman:** `/CDP/CoachingProton`
**Tanggal Audit:** 2026-04-10
**Files:** `CoachingProton.cshtml` (1766 lines), `_CoachingProtonPartial.cshtml` (97 lines), `_CoachingProtonContentPartial.cshtml` (324 lines)
**Stack:** Bootstrap 5, Chart.js, vanilla JS (AJAX)

---

## Skor Ringkasan

| Pilar | Skor (1-4) | Verdict |
|-------|:----------:|---------|
| Copywriting | 3 | Baik — bilingual terkontrol, CTA jelas |
| Visuals | 2 | Cukup — tabel sangat padat, chart OK |
| Color | 3 | Baik — semantic color konsisten |
| Typography | 2 | Cukup — hierarki lemah di tabel besar |
| Spacing | 2 | Cukup — filter bar terlalu padat, section kurang nafas |
| Experience Design | 3 | Baik — empty states, AJAX in-place, toast feedback |

**Rata-rata: 2.5 / 4**

---

## 1. Copywriting (Skor: 3)

### Positif
- Empty state messages kontekstual per scenario (`select_coachee`, `no_coachees`, `no_filter_match`, `no_deliverables`) — sangat baik
- Label form konsisten: "Semua Bagian", "Semua Unit", "Semua Track"
- CTA button jelas: "Submit Evidence", "Tinjau", "Review", "Lihat Detail"
- Toast messages informatif: "Alasan penolakan wajib diisi"

### Temuan & Rekomendasi

| # | Severity | Temuan | Rekomendasi |
|---|----------|--------|-------------|
| C1 | Minor | Campur bahasa: heading "Coaching Proton" + subtitle EN "Monitor deliverable progress and approval status" | Ganti subtitle ke Bahasa: "Pantau progress deliverable dan status approval" |
| C2 | Minor | Label "Pending Actions" dan "Pending Approvals" di stat cards dalam bahasa Inggris | Ganti ke "Aksi Tertunda" dan "Approval Tertunda" — atau konsisten semua EN |
| C3 | Minor | Placeholder search "Cari kompetensi..." — tapi juga search deliverable name. Menyesatkan | Ubah ke "Cari kompetensi atau deliverable..." |
| C4 | Minor | Button "Export Excel" dan "Export PDF" — ikon sama (`bi-file-earmark-excel` untuk PDF) | Gunakan `bi-file-earmark-pdf` untuk tombol Export PDF |
| C5 | Info | ContentPartial: "No Proton final assessments completed yet in this scope" — full EN di alert | Terjemahkan: "Belum ada final assessment Proton di scope ini" |
| C6 | Info | ContentPartial: chart header "Competency Level Granted Over Time" — EN | Terjemahkan atau buat konsisten bilingual |

---

## 2. Visuals (Skor: 2)

### Positif
- Stat cards dengan icon Bootstrap Icons — clean
- Chart.js doughnut + line + horizontal bar — variasi visual yang baik
- Progress bar di tabel coachee (ContentPartial) — compact & informatif
- Badge system untuk status (bg-success, bg-danger, bg-warning, bg-secondary) — readable

### Temuan & Rekomendasi

| # | Severity | Temuan | Rekomendasi |
|---|----------|--------|-------------|
| V1 | Major | Tabel utama 9-10 kolom (checkbox + coachee + kompetensi + sub + deliverable + evidence + 3 approval + detail) — sangat lebar, overflow horizontal pasti terjadi di layar <1400px | Pertimbangkan: (1) sembunyikan kolom approval di mobile, (2) gunakan card-based layout untuk <768px, atau (3) collapse approval jadi 1 kolom expandable |
| V2 | Medium | `rowspan` pada kolom Coachee/Kompetensi/SubKompetensi membuat tabel sulit di-scan secara vertikal saat banyak row | Tambahkan `border-left: 3px solid var(--bs-primary)` pada first-row setiap coachee group sebagai visual separator |
| V3 | Medium | Evidence modal (`#evidenceModal`) terlalu panjang — 8+ fields dalam 1 scroll | Bagi menjadi 2 step wizard atau grouped accordion (Acuan section sudah pakai card, tapi masih panjang) |
| V4 | Minor | Loading spinner overlay fixed fullscreen (`position:fixed`) — menutupi semua navigasi | Pertimbangkan inline skeleton loader di area tabel saja |
| V5 | Minor | Export buttons (3 laporan HC) menggunakan `btn-outline-warning` — kuning untuk download action tidak intuitif | Gunakan `btn-outline-primary` atau `btn-outline-success` yang lebih associated dengan download |
| V6 | Info | ContentPartial stat cards hanya 5 col (col-lg-2) — akan ada gap kosong di kanan pada large screen | Tambah 1 stat card atau ubah ke `col-lg` auto-sizing |

---

## 3. Color (Skor: 3)

### Positif
- Semantic color mapping konsisten: success=approved/green, danger=rejected/red, warning=pending, secondary=belum, info=HC review
- Chart.js warna: doughnut 5 warna distinct, bottleneck bar merah — sesuai semantik
- Badge `bg-opacity-10` pada ContentPartial tabel — soft badges yang readable
- Tooltip approval badges menggunakan `border border-success/danger` — visual reinforcement

### Temuan & Rekomendasi

| # | Severity | Temuan | Rekomendasi |
|---|----------|--------|-------------|
| CL1 | Medium | "Sudah Upload" dan "Approved" di kolom Evidence **keduanya** pakai `badge bg-success` — user sulit bedakan status berbeda | Gunakan warna berbeda: "Sudah Upload" → `bg-info`, "Approved" tetap `bg-success` |
| CL2 | Minor | Stat card "Pending Actions" text-warning, "Pending Approvals" text-info — keduanya "pending" tapi warna berbeda tanpa penjelasan | Samakan warna atau tambah ikon pembeda yang lebih kuat |
| CL3 | Minor | `table-bordered` pada tabel utama menambah visual noise — kebanyakan border | Ganti ke `table-hover` saja (sudah ada) tanpa `table-bordered`, gunakan subtle row divider |
| CL4 | Info | Bottleneck chart pure merah `rgba(220, 53, 69)` — gradient atau threshold coloring (30-60 hari kuning, >60 merah) akan lebih informatif | Pertimbangkan conditional bar color berdasarkan severity |

---

## 4. Typography (Skor: 2)

### Positif
- `fw-semibold` dan `fw-bold` digunakan untuk hierarki di header dan nama coachee
- `small` class konsisten untuk secondary text
- Form label `form-label small fw-semibold` — compact tapi readable

### Temuan & Rekomendasi

| # | Severity | Temuan | Rekomendasi |
|---|----------|--------|-------------|
| T1 | Medium | Heading utama `<h2>` langsung ke stat card `<h6>` + `<h3>` — skip heading level (h2 → h3/h6) tanpa visual hierarchy yang jelas | Buat page title `<h1 class="h3">`, stat card label `<p class="small">`, stat value `<span class="fs-2 fw-bold">` |
| T2 | Medium | Tabel utama: semua kolom teks ukuran sama — tidak ada penekanan pada kolom penting (deliverable name, status) | Tambah `fw-semibold` pada kolom Deliverable, buat kolom status sedikit lebih besar |
| T3 | Minor | ContentPartial tabel: kolom "No." redundant (nomor urut) — membuang space horizontal | Pertimbangkan hapus kolom No. dan gunakan `counter-reset` CSS jika benar-benar perlu |
| T4 | Minor | Coachee info card (line 271-280): `<strong>` tag langsung tanpa heading — semantic weak | Gunakan `<h6 class="mb-0">` untuk nama coachee |
| T5 | Info | Filter bar semua `form-select-sm` — pada desktop, dropdown terlalu kecil saat banyak pilihan | Override ke default size (`form-select`) pada `>= lg` breakpoint |

---

## 5. Spacing (Skor: 2)

### Positif
- `g-3` gap konsisten pada row/grid
- `mb-3`, `mb-4` rhythm yang reasonable
- ContentPartial stat cards menggunakan `h-100` — equal height

### Temuan & Rekomendasi

| # | Severity | Temuan | Rekomendasi |
|---|----------|--------|-------------|
| S1 | Medium | Filter bar (`d-flex flex-wrap gap-2`) — pada mobile, 5-6 dropdown + search + reset button stack rapat tanpa visual grouping | Group related filters (lokasi: bagian+unit, content: track+tahun+coachee) dengan separator atau card section |
| S2 | Medium | Antara filter bar dan tabel hanya `small` result count (`mb-2`) — stat cards + coachee info + export buttons + empty state semua stacked tanpa section separator | Tambah `<hr>` atau section divider antara filter, stats, dan content area |
| S3 | Minor | Tabel `table-bordered` menambahkan padding yang inconsistent dengan card-based layout di stat section | Konsistenkan: tabel tanpa border (table-borderless) atau wrap dalam card seperti ContentPartial |
| S4 | Minor | HC Review Panel `mt-4` — spacing dari akhir tabel ke panel bisa lebih generous | Gunakan `mt-5` atau tambahkan section heading |
| S5 | Minor | Modal Evidence body: form fields tanpa group spacing — "Acuan" card sudah baik, tapi sebelum dan sesudahnya spacing flat | Tambah `mb-4` pada group transitions (sebelum Acuan card, sebelum file upload) |
| S6 | Info | Inline style `style="width:auto;min-width:150px"` pada setiap filter dropdown — inconsistent sizing | Pindah ke CSS class `.filter-select { width: auto; min-width: 150px; }` |

---

## 6. Experience Design (Skor: 3)

### Positif
- **Empty states yang kontekstual** — 5 scenario berbeda dengan ikon, heading, dan CTA yang sesuai role
- **AJAX in-place update** — approve/reject/review tanpa full page reload, badge + tooltip update langsung
- **Batch HC approve** — checkbox select-all + modal konfirmasi dengan list detail
- **Client-side search** — debounced 300ms, clear button, no-results message
- **Loading spinner** — overlay saat filter/pagination
- **Auto-scroll to table** setelah pagination
- **Toast notifications** — success/error feedback
- **Cascading filter** — bagian → unit dependency
- **Tooltip pada approval badges** — siapa dan kapan approve

### Temuan & Rekomendasi

| # | Severity | Temuan | Rekomendasi |
|---|----------|--------|-------------|
| X1 | Medium | Full page reload pada setiap filter change (`onchange="this.form.submit()"`) — slow UX terutama jika data besar | Migrate ke AJAX partial refresh seperti yang sudah dilakukan di `_CoachingProtonPartial.cshtml` (pattern sudah ada!) |
| X2 | Medium | Client-side search hanya filter **visible rows** di current page — tidak search across all pages | Tambah info text: "Pencarian hanya berlaku untuk halaman ini" atau implementasi server-side search |
| X3 | Minor | `confirm()` dialog untuk HC review — native browser dialog, inconsistent dengan modal-based UX di tempat lain | Ganti dengan Bootstrap modal konfirmasi (sudah ada pattern di batch approve) |
| X4 | Minor | Export buttons tidak ada loading state — user bisa double-click saat generate file besar | Tambah `disabled` + spinner saat diklik, re-enable setelah download start |
| X5 | Minor | Pagination preserves filter state via URL params — bagus, tapi search input (client-side) hilang setelah navigasi | Persist search term di sessionStorage atau jadikan server-side param |
| X6 | Info | Tabel utama tidak sortable — pada dataset besar, user mungkin ingin sort by status atau nama | Pertimbangkan client-side sort header atau server-side sort param |

---

## Top 5 Prioritas Perbaikan

1. **[V1] Tabel terlalu lebar** — responsive strategy untuk 9-10 kolom (hide/collapse pada mobile)
2. **[X1] Filter masih full page reload** — migrate ke AJAX partial refresh (pattern sudah ada di codebase)
3. **[CL1] Badge "Sudah Upload" vs "Approved" sama-sama hijau** — bedakan warna untuk status berbeda
4. **[S1] Filter bar terlalu padat di mobile** — visual grouping dan better stacking
5. **[V3] Evidence modal terlalu panjang** — bagi ke step/section yang lebih manageable

---

## Catatan

- Halaman ini adalah **view utama (CoachingProton.cshtml)** + **dashboard partial** untuk level management. Dua view ini memiliki pattern UX yang cukup berbeda (tabel approval vs dashboard chart).
- Kualitas code cukup baik: XSS escaping (`escHtml`), CSRF token, role-based rendering, semantic HTML.
- Pattern AJAX partial refresh di `_CoachingProtonPartial.cshtml` adalah best practice yang belum diterapkan ke main view.
