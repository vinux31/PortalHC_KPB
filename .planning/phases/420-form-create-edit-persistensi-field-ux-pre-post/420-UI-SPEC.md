---
phase: 420
slug: form-create-edit-persistensi-field-ux-pre-post
status: draft
shadcn_initialized: false
preset: none
created: 2026-06-22
---

# Phase 420 — UI Design Contract

> Kontrak visual & interaksi untuk redesign form Create/Edit assessment (mode Standard & Pre-Post). Dibuat oleh gsd-ui-researcher, diverifikasi gsd-ui-checker.
>
> **BUKAN greenfield.** Ini redesign form yang SUDAH ADA (`Views/Admin/CreateAssessment.cshtml` wizard 4-langkah + `Views/Admin/EditAssessment.cshtml`). Design system SUDAH terkunci: **Bootstrap 5.3.0 + Bootstrap Icons 1.10.0** (CDN, `Views/Shared/_Layout.cshtml:38-39/246`). UI-SPEC ini RAMPING — hanya mengunci perubahan layout/copy/interaksi yang diputuskan user (CONTEXT D-01..D-04) memakai komponen Bootstrap existing. **JANGAN usulkan design system/library/warna/tipografi brand baru.**

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (server-rendered Razor + Bootstrap CDN; bukan React/Vite — shadcn gate N/A) |
| Preset | not applicable |
| Component library | Bootstrap 5.3.0 (CDN `_Layout.cshtml:38`) |
| Icon library | Bootstrap Icons 1.10.0 (`bi-*`, `_Layout.cshtml:39`) |
| Font | system default (Bootstrap `--bs-body-font-family`; tidak ada font kustom — JANGAN tambah) |
| Binding | ASP.NET tag-helper `asp-for` / `asp-validation-for` |

**Idiom kartu kanonik (PAKAI persis, jangan ciptakan baru):**
```
<div class="card mb-4">
  <div class="card-header bg-light"><h6 class="mb-0"><i class="bi bi-... me-2"></i>JUDUL</h6></div>
  <div class="card-body"><div class="row g-3"> ...field col-md-6... </div></div>
</div>
```
**Idiom toggle kanonik:** `<div class="form-check form-switch mb-2"><input class="form-check-input" type="checkbox" asp-for="X" id="X" /><label class="form-check-label" for="X">…</label></div>` + `<div class="form-text text-muted">…keterangan…</div>`.

**Sub-kartu di DALAM Group (D-02) pakai idiom nested existing** (lihat kartu Pre/Post `CreateAssessment.cshtml:429/452`): `<div class="card mb-3"><div class="card-header bg-light fw-semibold"><i class="bi bi-..."></i> JUDUL</div><div class="card-body">…</div></div>`.

---

## Spacing Scale

Bootstrap spacing utilities existing (kelipatan 4px via skala Bootstrap 0–5). JANGAN inline style / px hardcode baru.

| Token | Value | Utility Bootstrap | Usage di fase ini |
|-------|-------|-------------------|-------------------|
| xs | 4px | `g-1`, `me-1`, `mt-1` | Jarak ikon↔teks, gap inline |
| sm | 8px | `g-2`, `mb-2`, `py-2` | Jarak antar-switch dalam satu kelompok, padding alert |
| md | 16px | `g-3`, `mb-3`, `mt-3`, `px-3` | Default gap grid (`row g-3`), jarak antar sub-kartu |
| lg | 24px | `mb-4`, `p-4` | Jarak antar Group kartu (`card mb-4`) |
| xl | 32px | `mt-4` (nav) | Jarak ke baris navigasi wizard |

Exceptions: none. Semua spacing memakai utility Bootstrap existing — TIDAK ada nilai px kustom.

---

## Typography

Memakai skala tipografi Bootstrap existing (`h6` heading kartu, `form-label`, `form-text small`). JANGAN deklarasi ukuran/berat/line-height kustom — semua mengikuti default Bootstrap brand.

| Role | Element existing | Berat | Catatan |
|------|------------------|-------|---------|
| Heading kartu (Group) | `<h6 class="mb-0">` + `<i class="bi-… me-2">` | semibold (Bootstrap `h6`) | Judul Group A–D & sub-kartu |
| Heading sub-kartu | `.card-header.bg-light.fw-semibold` + `bi-…` | semibold (`fw-semibold`) | Judul "Setelan Post-Test" / "Setelan Bersama Pre & Post" |
| Label field | `.form-label.fw-bold` | bold (`fw-bold`) | Konsisten Group B existing (`:509/538/554`) |
| Label sekunder (sub-field jadwal) | `.form-label.small.fw-semibold.text-muted` | semibold | Konsisten kartu Pre/Post existing (`:436/441`) |
| Body keterangan | `.form-text.text-muted` | regular | Teks bantuan di bawah field |
| Badge scope/info | `.badge.bg-info` / `.badge.bg-secondary` | semibold (Bootstrap badge) | Penanda scope (lihat Copywriting) |

Line-height: default Bootstrap (`--bs-body-line-height: 1.5`). Tidak diubah.

---

## Color

Memakai palet Bootstrap brand existing — TIDAK ada warna hex kustom. Tabel di bawah memetakan peran ke utility Bootstrap yang SUDAH dipakai form ini, bukan mendefinisikan brand baru.

| Role | Utility Bootstrap | Usage di fase ini |
|------|-------------------|-------------------|
| Dominant (permukaan) | `bg-white` / body default | Latar form & card-body |
| Secondary (kartu/header) | `bg-light` (card-header) | Header tiap Group & sub-kartu |
| Accent | `text-primary` (ikon `bi-shuffle`/`bi-arrow-repeat`/`bi-shield-lock`) + `btn-primary` (CTA) | HANYA: ikon judul field interaktif + tombol "Selanjutnya"/"Simpan" |
| Info (scope/sinkron) | `text-info` / `badge bg-info` / `alert-info` | Penanda scope "Berlaku Pre & Post", note SamePackage, note cert Pre-Post |
| Warning | `alert-warning` / `text-warning` | Note "Pre Test biasanya tidak menerbitkan sertifikat", warning MaxAttempts (carry 421) |
| Required marker | `text-danger` (`<span class="text-danger">*</span>`) | Penanda wajib-isi existing |

Accent (`text-primary`/`btn-primary`) reserved for: ikon judul field interaktif (Token/Shuffle/Retake) + tombol navigasi/submit utama wizard. JANGAN warnai elemen lain dengan primary. TIDAK ada perubahan warna brand.

---

## Layout Contract — D-01..D-04 (inti fase ini)

> Ini bagian load-bearing. Executor implementasi dari sini. Mode **Standard = TIDAK berubah perilaku/layout** (backward-compat WAJIB); hanya mode **Pre-Post** mendapat layout baru.

### L-1 — Render toggle Acak Soal/Pilihan di EditAssessment (D-01 / FORM-01)
**Masalah:** `EditAssessment.cshtml` TIDAK merender shuffle sama sekali (grep `ShuffleQuestions`/`ShuffleOptions` = 0 → akar bug E-01: tiap simpan Edit shuffle ter-reset OFF).
**Kontrak:**
- Tambah kartu/blok "Pengacakan Soal & Jawaban" di EditAssessment, **idiom & copy IDENTIK** `CreateAssessment.cshtml:536-551` (ikon `bi-shuffle text-primary`, dua `form-switch`: "Acak Soal" + "Acak Pilihan Jawaban", dua `form-text` keterangan persis).
- Kedua `<input>` pakai `asp-for="ShuffleQuestions"` / `asp-for="ShuffleOptions"` → **terisi dari `Model`** (state tersimpan), bukan default.
- Tempatkan dalam Group "Pengaturan Ujian" Edit (sebaris pola Create), berdampingan dengan kartu Retake existing (`:417-428`).

### L-2 — Dua sub-kartu Step-3 saat mode Pre-Post (D-02 / FORM-08, FORM-11)
Saat `assessmentTypeInput == "PrePostTest"`, Group B "Pengaturan Ujian" disajikan sebagai **DUA sub-kartu** (nested `card mb-3`), menggantikan blok `row g-3` tunggal yang sekarang campur-scope:

**Sub-kartu 1 — "Setelan Post-Test"** (ikon `bi-clock-history`)
Urutan field (atas→bawah):
1. **Nilai Lulus (Pass Percentage)** — `asp-for="PassPercentage"`, label "Nilai Lulus (%)", form-text "Nilai minimum untuk lulus Post-Test (0–100)".
2. **Sertifikat** — `asp-for="GenerateCertificate"` (form-switch "Aktifkan") + `asp-for="ValidUntil"` (Tanggal Expired Sertifikat). Pertahankan note `prePostCertNote` (text-info) "Sertifikat hanya diterbitkan untuk peserta yang lulus Post-Test".
3. **Ujian Ulang** — blok existing `:552-576`, **TAPI DISEMBUNYIKAN** di mode Pre-Post (lihat L-3 / D-03).

**Sub-kartu 2 — "Setelan Bersama Pre & Post"** (ikon `bi-arrow-left-right` atau `bi-link-45deg`)
Urutan field:
1. **Pengacakan Soal & Jawaban** — blok shuffle `:536-551` (dua form-switch).
2. **Izinkan Review Jawaban** — `asp-for="AllowAnswerReview"` (pindah dari Group D ke sini saat Pre-Post).
3. **Security Token** — blok Token `:516-534` (form-switch + AccessToken).

**Mode Standard:** TIDAK ada pemisahan — layout Group B/C/D **tetap seperti sekarang** (satu `row g-3`, Status + PassPercentage + Token + Shuffle + Retake; Cert & AllowAnswerReview di Group C/D). Sub-kartu HANYA muncul saat Pre-Post.

**Heading sub-kartu** pakai idiom `card-header bg-light fw-semibold` (sama kartu Pre/Post existing `:430/453`).

### L-3 — Sembunyikan Ujian Ulang + rapikan Status/PassPercentage saat Pre-Post (D-03 / FORM-11)
- Mode Pre-Post → blok **Ujian Ulang disembunyikan** (retake hanya relevan Post; Pre = baseline murni). Sembunyikan via toggle JS yang memberi `d-none` pada wrapper retake **dan** menghapusnya dari payload POST (lihat L-5), bukan sekadar visual.
- `statusFieldWrapper` SUDAH disembunyikan di mode Pre-Post (`:2004-2005`, Status di-set "Upcoming"). Karena Status hilang, **PassPercentage TIDAK boleh tertinggal sebagai kolom `col-md-6` setengah-baris asimetris** (FORM-PP-06). Di sub-kartu Post (L-2), PassPercentage berdiri sebagai field penuh/rapi dalam konteks Post — bukan sebaris dengan Status yang kosong.
- PassPercentage di mode Pre-Post berlabel jelas konteks Post ("Nilai Lulus Post-Test"); Pre tidak menampilkan ambang lulus.

### L-4 — SamePackage pindah ke header section Pre-Post (D-04 / FORM-07)
- Pindahkan checkbox `SamePackage` (`:474-481`) dari DALAM kartu Post → ke **header section Pre-Post**: tepat di bawah pemilih "Tipe Assessment" (`:213-223`) / di atas pasangan kartu Pre & Post (`#ppt-jadwal-section`).
- Idiom: `form-check` + label "Gunakan paket soal yang sama untuk Pre dan Post" (copy existing dipertahankan) + badge sinkron `#samePackageBadge` (`badge bg-info` "Paket soal Post-Test akan otomatis disinkronkan dari Pre-Test").
- Hanya tampil saat mode Pre-Post (kelola via toggle JS bersama `#ppt-jadwal-section`).

### L-5 — Eliminasi input standard tersembunyi dari POST (D-04 / FORM-09)
- Saat mode Pre-Post, input jadwal/durasi/EWCD **standard** (`ScheduleDate/Time`, `DurationMinutes`, `ewcd*`, hidden combiner `Schedule`/`ExamWindowCloseDate` `:382-425`) **TIDAK ikut ter-POST** (disable/strip dari payload, bukan sekadar `d-none`). Sebaliknya saat Standard, input Pre/Post (`Pre*`/`Post*`) tidak dikirim.
- Pendekatan teknis bebas (mis. `disabled` attribute pada section non-aktif, atau strip saat submit) asal binding mode aktif tidak putus.

### L-6 — Penamaan tipe assessment (D-04 discretion / FORM-10)
- Label UI "Tipe Assessment" + dropdown "Standard"/"Pre-Post Test" boleh tetap (tidak ada perubahan visual wajib). Yang berubah = penanda internal/parameter agar tidak rancu dengan kolom DB `AssessmentType` (planner pilih, mis. `CreationMode`). **Tidak ada kontrak visual baru di sini** — dicatat agar checker tahu ini bukan perubahan UI.

---

## Visibility Matrix (per-mode)

Diperluas dari toggle JS existing `CreateAssessment.cshtml:1996-2018`. Saat `assessmentTypeInput` berubah, elemen berikut di-toggle:

| Elemen | Mode Standard | Mode Pre-Post |
|--------|---------------|---------------|
| `#standard-jadwal-section` (jadwal/durasi/EWCD std) | tampil + ter-POST | sembunyi + **TIDAK ter-POST** (L-5) |
| `#ppt-jadwal-section` (kartu Pre & Post) | sembunyi | tampil |
| SamePackage (header section) | sembunyi | tampil (L-4) |
| `#statusFieldWrapper` (Status) | tampil | sembunyi (set "Upcoming") |
| Sub-kartu "Setelan Post-Test" / "Setelan Bersama" | tidak ada (layout tunggal existing) | tampil (L-2) |
| Blok Ujian Ulang | tampil | **sembunyi + tidak ter-POST** (L-3) |
| `#prePostCertNote` (text-info) | sembunyi | tampil |

Backward-compat: jalur Standard menghasilkan DOM & payload **identik perilaku** dengan sekarang.

---

## Accessibility Contract

| Item | Kontrak |
|------|---------|
| Label↔input | Setiap field pakai `asp-for` (auto `id`) + `<label for>` cocok. Switch retake/shuffle/token sudah punya `form-check-label for=…` — pertahankan. |
| Grup field | Bungkus dua sub-kartu D-02 dengan heading `card-header` deskriptif (berperan sebagai group label). Untuk kelompok switch terkait (Shuffle 2-switch), boleh `role="group"` + `aria-label` bila planner perlu, mengikuti pola `unitMultiContainer` di proyek (399). |
| Required | Penanda `<span class="text-danger">*</span>` + `aria` opsional; pertahankan `invalid-feedback` existing. |
| Sembunyi/tampil | Section non-aktif yang di-`d-none` saat mode beralih harus juga `disabled` (L-5) agar tidak terbaca screen-reader & tidak ter-POST. |
| Badge scope | Badge `bg-info`/`bg-secondary` bersifat dekoratif-informatif; teks scope juga muncul di heading sub-kartu agar tidak bergantung warna saja (color-independent). |
| Ikon | Ikon `bi-*` dekoratif; makna selalu didampingi teks label (tidak ada kontrol icon-only). |

---

## Responsive Contract

- Pertahankan grid `col-md-6` existing untuk field di Group/sub-kartu (dua kolom di ≥md, satu kolom di mobile). Sub-field jadwal Pre/Post tetap `col-md-4` (existing `:435/440/444`).
- Sub-kartu D-02 full-width dalam card-body (`row g-3` di dalamnya tetap responsif).
- TIDAK ada breakpoint/util responsif baru di luar yang sudah dipakai.

---

## Copywriting Contract

Semua copy **Bahasa Indonesia**, konsisten gaya existing (kalimat penjelas di `form-text text-muted`, sapaan netral, istilah "peserta"/"sesi"/"Pre-Test"/"Post-Test").

| Element | Copy |
|---------|------|
| Primary CTA (wizard) | "Selanjutnya" (langkah) / "Simpan" (Edit) — existing, TIDAK diubah |
| Heading sub-kartu Post | "Setelan Post-Test" |
| Heading sub-kartu bersama | "Setelan Bersama Pre & Post" |
| Badge scope (opsional per-field) | "Berlaku Pre & Post" (`badge bg-info`) / "Hanya Post" (`badge bg-secondary`) |
| Label SamePackage (header) | "Gunakan paket soal yang sama untuk Pre dan Post" |
| Badge sinkron SamePackage | "Paket soal Post-Test akan otomatis disinkronkan dari Pre-Test" |
| Note cert Pre-Post | "Sertifikat hanya diterbitkan untuk peserta yang lulus Post-Test" |
| Label Pass (Pre-Post) | "Nilai Lulus Post-Test (%)" + form-text "Nilai minimum untuk lulus Post-Test (0–100)" |
| Shuffle (render Edit, dari Create) | "Acak Soal" / "Acak Pilihan Jawaban" + form-text identik `CreateAssessment.cshtml:545/550` |
| Empty/info state | Tidak ada empty-state baru (form input, bukan list). N/A. |
| Error state (validasi) | Pertahankan `invalid-feedback`/`asp-validation-for` existing (mis. "Durasi (menit) wajib diisi.", "Status wajib dipilih."). |
| Destructive | Tidak ada aksi destruktif di fase ini (form persistensi/layout). Lock Completed (FORM-05) = guard read-only, bukan delete. |

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| Bootstrap 5.3.0 (CDN) | card, form-switch, form-check, badge, alert, input-group, collapse | not required (bukan shadcn registry; CDN pinned existing) |

Tidak ada registry pihak ketiga / shadcn. Gate vetting tidak berlaku.

---

## Out-of-Scope (jangan masuk UI-SPEC ini)

- Section / Scoped-Shuffle / Opsi-Dinamis = v32.6 branch `main` (fase 415-419). Overlap layout — rekonsiliasi saat merge, BUKAN didesain di sini.
- Logika retake (421), SamePackage sync backend (422), aturan cert (423), grading/gating Pre→Post (424). Fase 420 HANYA form/binding/layout/copy.

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
