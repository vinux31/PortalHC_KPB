---
phase: 384
slug: monitoring-essay-grading-ui-refactor-fase-2
status: draft
shadcn_initialized: false
preset: none
created: 2026-06-15
---

# Phase 384 — UI Design Contract

> Kontrak visual & interaksi untuk refactor UI penilaian essay di Monitoring. Dibuat oleh gsd-ui-researcher, diverifikasi gsd-ui-checker.
> **Sifat phase:** STRUCTURAL REFACTOR — REUSE komponen Bootstrap + markup essay-grading yang sudah ada. BUKAN design system baru. Tidak ada token/komponen/library baru. Backend & endpoint TIDAK diubah (UIG-03). 0 migration.
> **Bahasa UI:** Bahasa Indonesia (semua copy).

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (ASP.NET Core MVC + Razor `.cshtml`) — shadcn gate **N/A** (bukan React/Next/Vite; tidak ada `components.json`/`tailwind.config.*` di root) |
| Preset | not applicable |
| Component library | **Bootstrap 5** (utility + component classes: `card`, `badge`, `table table-hover align-middle`, `btn`, `form-control`, `d-flex`, `breadcrumb`, `alert`, `dropdown`) — sudah terpasang app-wide |
| Icon library | **Bootstrap Icons** (`<i class="bi bi-*">`) |
| Font | Default theme Bootstrap (font app existing — tidak diubah phase ini) |

**Sumber design system:** dideteksi dari `Views/Admin/AssessmentMonitoringDetail.cshtml` (Bootstrap 5 + Bootstrap Icons, copy Bahasa Indonesia). ui-brand.md generik = FALLBACK; phase ini DEFER ke pola in-repo existing.

**Aturan inti reuse (jangan dilanggar):**
- Tabel worker-list baru = MIRROR pola tabel sesi existing (`:218-231`): `<div class="table-responsive"><table class="table table-hover align-middle mb-0"><thead class="table-light">`.
- Kartu penilaian essay di page per-worker = CLONE markup `AssessmentMonitoringDetail.cshtml:~407-446` byte-for-byte (struktur), termasuk `_QuestionImage` partial (`:416`), rubrik collapse (`:421-430`), input skor `min=0 max=ScoreValue` (`:433-437`), tombol "Simpan Skor" (`:439-443`), section "Selesaikan Penilaian" (`:448-476`).
- Handler AJAX `.btn-save-essay-score` + `.btn-finalize-grading` (inline `<script>` `:1472-1558`) di-REUSE apa adanya — kontrak request/response sama (lihat Interaction Contract). Token via `#antiforgeryForm` (`:484`). Helper URL `appUrl(...)`.

---

## Spacing Scale

Bootstrap 5 spacing utilities (`.m*/.p*/.g*` step 0–5 = 0/4/8/16/24/48px) — sudah multiple-of-4. Tidak ada token baru.

| Token (Bootstrap) | Value | Usage di phase ini |
|-------|-------|-------|
| `g-1` / `gap-1` | 4px | Jarak ikon-teks dalam tombol, gap antar tombol aksi baris |
| `*-2` | 8px | Padding compact, gap badge |
| `mb-3` / `p-3` | 16px | Jarak antar kartu essay, padding kartu (sesuai existing `:407 mb-3`, `:417 p-3`) |
| `mb-4` / `mt-4` | 24px | Jarak antar section / header → konten (sesuai existing `:64 mb-4`, `:387 mt-4`) |
| `py-4` | 24px | Padding vertikal container halaman (sesuai `:36 py-4`) |

Exceptions:
- **Touch target tombol:** `style="min-height:40px;"` pada tombol aksi tabel (POLA EXISTING dropdown `:321`). Tombol "Tinjau Essay" dan tombol read-only toolbar MENGIKUTI min-height 40px agar target sentuh memadai. (Catatan: <44px tapi konsisten dengan pola in-repo existing — JANGAN naikkan ke 44 karena akan inkonsisten dengan tabel sesi di atasnya.)
- **Lebar input skor:** `style="max-width:80px"` (EXISTING `:434`) — dipertahankan persis.
- **Accent border kartu essay:** `border-left: 3px solid #ffc107` (EXISTING `:392`) — dipertahankan di kartu/header per-worker.

---

## Typography

Tidak ada skala typografi baru — pakai heading & utility Bootstrap existing. Deklarasi eksplisit untuk surface phase ini:

| Role | Element / Class | Weight | Catatan |
|------|------|--------|-------------|
| Page heading | `<h2 class="fw-bold">` (judul assessment, EXISTING `:77`) | bold (700) | Header Monitoring Detail tidak diubah |
| Section heading | `<h5 class="fw-semibold">` ("Penilaian Essay", EXISTING `:388`) | semibold (600) | Pada page per-worker: identitas worker = `h5/h6 fw-semibold` |
| Card / soal heading | `<h6 class="fw-semibold">` ("Soal {n}: {teks}", EXISTING `:410`) | semibold (600) | CLONE persis |
| Body | `<p>` default + `small` untuk meta | regular (400) | "Jawaban Pekerja", teks jawaban, rubrik |
| Label / meta | `.small .text-muted` / `.form-label.small` | regular/semibold | Label "Skor:", "/ {ScoreValue}", NIP |

Weights dipakai: **2** efektif (regular 400 + semibold 600; `fw-bold` page-title existing tidak disentuh). Line-height = default Bootstrap (`1.5` body). Tidak ada perubahan font-size pixel — semua via class Bootstrap existing.

---

## Color

Pakai palet semantik Bootstrap existing yang SUDAH dipakai di view ini. Tidak ada warna kustom baru selain accent essay `#ffc107` yang sudah ada.

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | `#ffffff` / `bg-light` (`#f8f9fa`) | Background halaman, card-body, panel jawaban (`:417 bg-light`) |
| Secondary (30%) | `card` + `table-light` header (`#f8f9fa`) | Kartu, header tabel `<thead class="table-light">`, card-header `bg-light` |
| Accent (10%) | `#ffc107` (warning) | **Hanya:** border-left kartu essay (`3px solid #ffc107`), ikon section `bi-pencil-square text-warning` (`:388`), badge status PENDING |
| Destructive/info semantik | `bg-success` `#198754` (selesai/benar) · `bg-info` (siap difinalisasi) · `bg-secondary` (netral) | Badge status 3-state + tombol aksi |

Accent (`#ffc107` warning) reserved for: **border-left kartu essay**, **ikon judul "Penilaian Essay"**, dan **badge status "{N} belum dinilai"**. TIDAK dipakai untuk tombol primary atau elemen interaktif umum.

**Kontrak badge status 3-state (D-04 — LOCKED, jangan ubah kriteria):**

| State | Kondisi (per `MonitoringSessionViewModel`) | Class | Copy (Bahasa Indonesia) |
|-------|---------|-------|------|
| 🟡 Pending | `EssayPendingCount > 0` | `badge bg-warning text-dark` | `"{N} belum dinilai"` (mis. "2 belum dinilai") |
| 🔵 Siap | `EssayPendingCount == 0` && BELUM finalized | `badge bg-info` (text default; jika kontras kurang gunakan `text-dark`) | `"Siap difinalisasi"` |
| 🟢 Selesai | finalized = `Status == AssessmentConstants.AssessmentStatus.Completed && !string.IsNullOrEmpty(NomorSertifikat)` (REUSE gate Phase 310 D-02, `:451-453`) | `badge bg-success` | `"Selesai"` |

> **Kontras a11y wajib:** `bg-warning` (kuning `#ffc107`) HARUS dipasangkan `text-dark` — `text-white` di atas kuning gagal WCAG. Pola ini SUDAH dipakai existing (`:397`, `:11`, `:246`). `bg-success` pakai teks putih default (OK). `bg-info` cek kontras — default Bootstrap 5 `bg-info` cukup gelap untuk teks putih, tapi jika tema custom membuatnya terang, pakai `text-dark`.

---

## Copywriting Contract

Semua copy Bahasa Indonesia. CTA/label REUSE dari markup existing agar konsisten lintas surface.

| Element | Copy |
|---------|------|
| Primary CTA (tabel → page) | **"Tinjau Essay"** (tombol per-baris worker, posisi kanan) — ikon `bi-pencil-square` (konsisten dgn ikon section essay) |
| CTA simpan skor (page per-worker) | **"Simpan Skor"** (REUSE existing `:442`) |
| CTA finalisasi (page per-worker) | **"Selesaikan Penilaian"** + ikon `bi-check-circle` (REUSE existing `:466/473`) |
| Tombol kembali (page per-worker) | **"Kembali ke Monitoring"** + ikon `bi-arrow-left` (POLA existing back-link `:65-67`) |
| Badge status tabel | 🟡 "{N} belum dinilai" / 🔵 "Siap difinalisasi" / 🟢 "Selesai" (lihat tabel Color) |
| Badge status soal (page) | "Sudah Dinilai" (`bg-success`) / "Belum Dinilai" (`bg-secondary`) — REUSE existing `:411-414` |
| Label jawaban kosong | "(tidak ada jawaban)" (REUSE existing `:419`) |
| Empty state (section level) | **TIDAK ADA empty-state copy** — D-05: bila tak ada worker beressay (`essayGradingMap` null/kosong, guard `:385`), seluruh section "Penilaian Essay" DISEMBUNYIKAN total. Jangan render heading/tabel/placeholder. |
| Empty state (judul section saat ADA data) | `<h5>` "Penilaian Essay" + ikon `bi-pencil-square text-warning` (REUSE `:388`) |
| Error state (AJAX simpan skor gagal) | "Gagal menyimpan skor. Silakan coba lagi." (REUSE existing `:1512`) + validasi input "Masukkan nilai skor yang valid." (`:1482`) |
| Error state (AJAX finalisasi gagal) | "Gagal menyelesaikan penilaian. Silakan coba lagi." (REUSE `:1554`) + error spesifik per-status via `showAlert('danger', ...)` (`:1550`) |
| Destructive/irreversible confirmation | **"Selesaikan Penilaian"** → `confirm('Setelah diselesaikan, status peserta akan diperbarui dan sertifikat akan digenerate. Lanjutkan?')` (REUSE existing `:1522`). Ini satu-satunya aksi tak-balik (generate sertifikat). Tidak ada aksi delete/destruktif lain di phase ini. |

---

## Layout & Component Contract (refactor-specific)

> Section tambahan di luar template generik — inti dari refactor. Executor mengimplementasikan dari sini.

### A. Tabel worker-list (menggantikan blok inline `:381-481` di `AssessmentMonitoringDetail.cshtml`)

- **Guard render (D-01/D-05):** pertahankan `@if (essayGradingMap != null && essayGradingMap.Any())` (`:385`). Tanpa worker beressay → section hidden total.
- **Data source (D-02):** `Model.Sessions.Where(s => s.HasManualGrading)` — HANYA worker dengan essay. Worker murni MC TIDAK muncul. Diskresi: kemungkinan **tak perlu ViewModel baru** — `MonitoringSessionViewModel` (`:48-68`) sudah punya `UserFullName`, `UserNIP`, `HasManualGrading`, `EssayPendingCount`, `Status`, `NomorSertifikat` (planner konfirmasi).
- **Urutan (D-03):** `.OrderBy(s => s.UserNIP)` (alfabet/NIP), BUKAN pending-first.
- **Struktur tabel:** mirror tabel sesi existing — `<div class="table-responsive"><table class="table table-hover align-middle mb-0"><thead class="table-light">`.
- **Kolom (4):**

  | # | Header | Isi | Catatan |
  |---|--------|-----|---------|
  | 1 | "Worker" | `<td class="fw-semibold">@UserFullName</td>` + baris kedua `<small class="text-muted">@UserNIP</small>` | NIP di bawah nama (compact) |
  | 2 | "Essay Belum Dinilai" | `EssayPendingCount` (angka; tampilkan "—" / "0" bila 0) | `text-center` |
  | 3 | "Status" | badge 3-state (lihat kontrak badge D-04) | `text-center` atau kiri konsisten tabel atas |
  | 4 | "Aksi" | tombol "Tinjau Essay" (kanan) | lihat D-06 |

- **Tombol "Tinjau Essay" (D-06):** `<a class="btn btn-primary btn-sm" href="@Url.Action(...)"><i class="bi bi-pencil-square me-1"></i>Tinjau Essay</a>` (navigasi GET, BUKAN AJAX). `min-height:40px` opsional konsisten. **Tetap MUNCUL setelah finalized** (D-10) — untuk lihat hasil read-only.
- **Responsif (default, konsisten in-repo):** `table-responsive` sudah memberi horizontal-scroll pada viewport sempit (pola existing tabel sesi). TIDAK perlu kartu/stack kustom. Kolom NIP ditaruh sebagai sub-baris di kolom Worker (bukan kolom terpisah) agar tabel tetap ringkas di mobile.

### B. Page penilaian essay per-worker (view + GET action baru)

- **Route (Diskresi → DEFAULT dipilih):** GET action baru di `AssessmentAdminController` (sebelah `AssessmentMonitoringDetail`), parameter `sessionId`. **Default route shape:** `EssayGrading?sessionId={id}` (attribute-routing/conventional konsisten controller existing — planner finalisasi). Authorization = SAMAKAN dengan `AssessmentMonitoringDetail`/`SubmitEssayScore` (`Admin, HC`) — verifikasi attribute.
- **Header identitas worker (D-07):** di atas konten —
  - Breadcrumb opsional MENGIKUTI pola existing (`:55-61`): Kelola Data → Assessment Monitoring → {Title} → {Nama Worker}. **Default:** cukup back-link (lebih ringkas, konsisten `:64-67`); breadcrumb = nice-to-have, planner boleh tambah.
  - Tombol "Kembali ke Monitoring" `btn btn-outline-secondary btn-sm` + `bi-arrow-left` (POLA `:65`). Harus carry `title`/`category`/`scheduleDate` (atau pakai referer) agar balik ke `AssessmentMonitoringDetail` yang benar.
  - Identitas: `<h2 fw-bold>{UserFullName}</h2>` + `<p class="text-muted"><i class="bi bi-person-badge me-1"></i>NIP: {UserNIP}</p>`.
- **Konten utama:** CLONE kartu essay existing (`:407-446`) untuk SETIAP `EssayGradingItemViewModel` dari session ini. Data load: clone logic `EssayGradingMap` builder (`AssessmentAdminController.cs:~3412-3433`) untuk SINGLE session (Diskresi — planner). REUSE `_QuestionImage` partial.
- **Section "Selesaikan Penilaian":** CLONE `:448-476` — `id="finalizeSection_{sessionId}"`, `display:block` bila `EssayPendingCount==0`, gate finalized Phase 310 D-02 untuk disabled+tooltip.
- **Token AJAX:** sertakan `<form id="antiforgeryForm" style="display:none">@Html.AntiForgeryToken()</form>` (REUSE `:484`) + helper `appUrl(...)` di page ini.

### C. State finalized = READ-ONLY (D-10)

Bila session finalized (gate `Status==Completed && NomorSertifikat!=null`), page per-worker dibuka mode read-only:
- Skor TAMPIL (value terisi), `<input>` skor `disabled`.
- Tombol "Simpan Skor" `disabled` atau `hidden`.
- Tombol "Selesaikan Penilaian" `disabled` + WRAP `<span data-bs-toggle="tooltip" title="...">` agar tooltip fire (Pitfall #6 mitigation, POLA EXISTING `:460-468`), `style="pointer-events:none;"`.
- Badge soal "Sudah Dinilai" `bg-success`. Tujuan: HINDARI panggilan `SubmitEssayScore`/`FinalizeEssayGrading` di sesi Completed (backend tak diubah, tak perlu cek penolakan backend).

### D. Interaction Contract — AJAX (REUSE handler `:1472-1558`, JANGAN ubah kontrak backend)

| Aksi | Endpoint (REUSE) | Request body | Response sukses | Behavior UI |
|------|---------|--------------|-----------------|-------------|
| Simpan Skor | `POST /Admin/SubmitEssayScore` | `sessionId`, `questionId`, `score`, `__RequestVerificationToken` (form-urlencoded, header `X-Requested-With: XMLHttpRequest`) | `{success, allGraded, message?}` | Badge soal → `bg-success` "Sudah Dinilai"; bila `allGraded` → tampilkan `finalizeSection_{sessionId}`. In-place, tak reload. |
| Selesaikan Penilaian | `POST /Admin/FinalizeEssayGrading` | `sessionId`, `__RequestVerificationToken` | `{success, alreadyFinalized?, nomorSertifikat?, message}` | **D-09 OVERRIDE:** handler existing melakukan `location.reload()` pada finalisasi pertama (`:1547`). Pada page per-worker, ganti jadi **update in-place ke state "Selesai"** (badge hijau, input/tombol read-only) TANPA redirect. User klik "Kembali" manual. Cabang `alreadyFinalized` (no-op friendly `showAlert('info', ...)`) dipertahankan. |

> **Catatan untuk planner (D-08):** handler `.btn-save-essay-score`/`.btn-finalize-grading` saat ini inline `<script>` di `AssessmentMonitoringDetail.cshtml`. Untuk page per-worker, putuskan: extract ke shared script/partial vs duplikat. `.btn-save-essay-score` mencari `.closest('.essay-grading-card')` → markup clone HARUS mempertahankan class `essay-grading-card` + `essay-score-input` + `id="badge_{sessionId}_{questionId}"` agar selector tetap match. Modifikasi finalize-handler untuk D-09 (in-place, bukan reload) berlaku di page per-worker.

### E. Aksesibilitas

- Tombol "Tinjau Essay" = elemen `<a>` dengan teks visible "Tinjau Essay" (bukan ikon-only) → screen-reader friendly. Bila ikon-only di masa depan, wajib `aria-label`.
- Badge `bg-warning` WAJIB `text-dark` (kontras — sudah di kontrak Color).
- Tombol disabled (read-only/finalized) WAJIB dibungkus `<span data-bs-toggle="tooltip">` agar tooltip muncul (pola existing `:460-468`); inisialisasi tooltip Bootstrap di page.
- `table-responsive` memberi keyboard/scroll access pada viewport sempit; header tabel `<thead>` semantik.
- Heading hierarki: page per-worker `h2` (worker) → `h6` (soal). Section Monitoring `h5` (Penilaian Essay).

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| none (Bootstrap 5 CDN/vendored, sudah app-wide) | tidak ada block pihak ketiga | **not applicable** — bukan shadcn; tidak ada registry pihak ketiga; tidak ada komponen baru di-fetch. Refactor murni REUSE markup in-repo + Bootstrap existing. |

Tidak ada deklarasi registry pihak ketiga → vetting gate tidak dijalankan (tidak diperlukan).

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
