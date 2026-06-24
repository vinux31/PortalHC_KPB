---
phase: 418
slug: opsi-jawaban-dinamis-2-6
status: approved
shadcn_initialized: false
preset: none
created: 2026-06-24
reviewed_at: 2026-06-24
checker_verdict: PASS-with-flags (6/6 dimensi PASS, 0 BLOCK)
---

> **Checker flags (non-blocking — WAJIB diputuskan planner sebagai keystone, bukan detail belakangan):**
> 1. **Strategi `name` radio MC dinamis** — pola eksisting `name="correctA/B/C/D"` per-index (ManagePackageQuestions:401, _InjectQuestionForm:41). Single-select MC lintas baris hanya benar bila semua radio berbagi satu `name` grup ATAU binder per-index menangani single-correct. Pengaruh: server binder + JS re-letter (C3) + prefill (C6) sekaligus. Putuskan lebih awal.
> 2. **Konvensi `id` baris dinamis** — `populateEditForm` sekarang andalkan id statis `option_A`/`correct_A` (:701-702) + `IMAGE_FIELDS[i+1]` (:706). Setelah dinamis, pilih `option_{index}` vs `option_{letter}` (keputusan keystone; wajib backward-compat).
> 3. **`role="alert"` pada `#authError`** — naikkan dari "pertimbangkan" jadi WAJIB agar pesan edit-shrink (D-418-02) ter-announce SR.
> 4. **Reasosiasi gambar opsi saat hapus baris-tengah** — gambar terikat baris (FileReader thumbnail + field `optX` index-based). Uji eksplisit: hapus baris B saat C punya gambar → gambar tetap di soal benar (cakup di test C8, jangan terlewat).

# Phase 418 — UI Design Contract: Opsi Jawaban Dinamis 2–6

> Kontrak visual & interaksi untuk refactor opsi jawaban dinamis (2–6). Bukan sistem desain baru — ini me-refactor **form Bootstrap 5 yang sudah ada** (Kelola Soal + form Inject) dan menyamaratakan render huruf A–F di layar ujian/hasil/ringkasan/preview. Dihasilkan oleh gsd-ui-researcher, diverifikasi gsd-ui-checker.
>
> **Scope batas:** TIDAK mendefinisikan ulang token global. Reuse kelas Bootstrap + Bootstrap Icons yang sudah dipakai form. OPT-01/02/03. migration=FALSE.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (sistem yang ada — Bootstrap 5, no shadcn; tech stack Razor/MVC bukan React/Next/Vite → shadcn gate N/A) |
| Preset | not applicable |
| Component library | Bootstrap 5 (input-group, form-check, list-group, alert, badge, card) |
| Icon library | Bootstrap Icons (`bi-*`) — `bi-plus-circle`, `bi-x-lg`, `bi-trash`, `bi-info-circle`, `bi-exclamation-triangle` |
| Font | Sistem/tema yang ada (tidak diubah) |

**Catatan kepatuhan:** Semua elemen baru WAJIB memakai utility & komponen Bootstrap yang sudah dipakai form ini, bukan CSS kustom baru. Tabel di bawah men-cite kelas eksaknya agar planner/executor reuse, bukan menciptakan look baru.

---

## Spacing Scale

Skala mengikuti Bootstrap spacer (4px base → kelas `m*-N`/`p*-N`, N×4px). Hanya nilai berikut yang dipakai komponen fase ini:

| Token | Value | Kelas Bootstrap | Usage di fase ini |
|-------|-------|-----------------|-------------------|
| xs | 4px | `gap-1`, `py-1`, `ms-1`, `me-1` | Jarak inline ikon↔teks, padding alert tipis |
| sm | 8px | `px-2`, `mb-2`, `gap-2` | Jarak antar-baris opsi (`mb-2`), padding horizontal alert |
| md | 16px | `mb-3`, `gap-3` | Jarak antar-blok form (section opsi → field berikutnya) |
| lg | 24px | `mb-4` | (render) jarak antar-kartu soal |

Exceptions:
- Sel selektor-benar pada baris opsi: lebar tetap **`width:36px`** (sudah ada di markup) — bukan token spacing, ini lebar kolom input-group agar radio/checkbox sejajar. Pertahankan persis.
- Indentasi blok gambar opsi authoring: **`ms-4`** (sudah ada) — pertahankan agar thumbnail sejajar di bawah teks opsi.

---

## Typography

Mengikuti tipografi form yang ada (tidak ada ukuran/berat baru). Kelas eksak yang dipakai komponen fase ini:

| Role | Kelas | Catatan |
|------|-------|---------|
| Label section opsi | `form-label fw-medium small` | "Opsi Jawaban" — pertahankan persis |
| Huruf opsi (authoring) | `input-group-text fw-bold` | Sel huruf A–F di kiri input, bold |
| Huruf opsi (render ujian/hasil) | `fw-bold me-1 text-secondary` (StartExam) | Format `@letter.` (huruf + titik) |
| Teks bantuan | `form-text` | "Klik radio/checkbox di kiri untuk tandai jawaban benar." |
| Pesan inline (info/error) | `alert ... small` | py-1 px-2 small (lihat Color) |

Aturan: jangan menambah ukuran font kustom. Semua teks komponen baru memakai `small` (≈0.875rem) sesuai konvensi form atau ukuran default body tema.

---

## Color

Mengikuti palet semantik Bootstrap yang sudah dipakai form. Tidak ada warna brand baru.

| Role | Token Bootstrap | Usage di fase ini |
|------|-----------------|-------------------|
| Primary / aksi positif | `btn-primary`, `btn-outline-primary` | Tombol "+ Tambah Opsi" (outline-primary, sm) |
| Secondary / netral | `text-secondary`, `btn-secondary` | Huruf opsi di render; tombol Batal Edit (sudah ada) |
| Danger / hapus & error | `btn-outline-danger`, `alert alert-danger`, `text-danger` | Tombol "Hapus" baris opsi; pesan edit-shrink ditolak; marker wajib `*` |
| Info / petunjuk | `alert alert-info` | Banner MA "Centang semua opsi yang benar (minimal 2)." (sudah ada `#maLabel`) |
| Success | `list-group-item-success`, `badge bg-success`, `text-success` | Penanda jawaban benar di render/preview (sudah ada) |

Aksen (danger) khusus untuk: tombol Hapus baris opsi + ikon `bi-x-lg`/`bi-trash`, alert error edit-shrink, alert error validasi min/max. JANGAN pakai danger untuk aksi non-destruktif.

---

## Copywriting Contract

Bahasa Indonesia untuk semua teks user-facing (CLAUDE.md). Istilah teknis tetap English bila perlu.

| Element | Copy |
|---------|------|
| Tombol tambah opsi | `+ Tambah Opsi` (ikon `bi-plus-circle me-1`) |
| Tombol tambah — disabled tooltip @6 | `title="Maksimal 6 opsi"` (tombol di-`disabled` saat 6 baris) |
| Tombol hapus baris | `Hapus` atau ikon-saja `bi-x-lg` (lihat Komponen) dengan `aria-label="Hapus opsi {letter}"` |
| Bantuan section opsi | `Klik radio/checkbox di kiri untuk tandai jawaban benar.` (sudah ada — pertahankan) |
| Banner MA | `Centang semua opsi yang benar (minimal 2).` (sudah ada `#maLabel`) |
| Error min-2 (submit) | `Soal pilihan ganda butuh minimal 2 opsi terisi.` |
| Error max-6 | `Maksimal 6 opsi per soal.` |
| Error correct-tanpa-teks | `Opsi yang ditandai benar wajib diisi teksnya.` (sudah ada di validator) |
| Error correct hilang setelah hapus | `Pilih kembali jawaban benar — opsi yang sebelumnya benar telah dihapus.` |
| **Error edit-shrink ditolak (D-418-02)** | `Opsi "{teks/huruf}" sudah dijawab peserta dan tidak bisa dihapus. Batalkan perubahan atau pertahankan opsi ini.` |
| Empty/legacy hint (opsional) | (tidak ada empty-state baru — soal 4-opsi lama tampil identik) |

**Destructive actions di fase ini:**

| Aksi | Konfirmasi |
|------|-----------|
| Hapus baris opsi (authoring, client-side, belum tersimpan) | Tanpa modal — langsung hilang baris (data belum ke DB). Hanya boleh bila baris > minimum 2. |
| Edit-shrink opsi yang **sudah dijawab** peserta (server-side, D-418-02) | Bukan konfirmasi — **tolak keras** dengan pesan inline `alert alert-danger`. Tidak ada penghapusan data; HC diminta batalkan/sesuaikan. Cegah `DbUpdateException` FK Restrict (hazard 999.14). |

---

## Component & Interaction Contract

Kontrak inti fase ini. Semua merujuk markup/kelas yang SUDAH ADA agar planner reuse.

### C1 — Baris Opsi Dinamis (authoring + inject)

Struktur per-baris **pertahankan persis pola yang ada** (`ManagePackageQuestions.cshtml:398-407`, `_InjectQuestionForm.cshtml:38-47`):

```
<div class="input-group input-group-sm mb-2" data-option-row data-index="{i}">
  <div class="input-group-text" style="width:36px">
    <input class="form-check-input mt-0 correct-input" type="radio|checkbox"
           name="{correctName}" value="true" aria-label="Opsi {letter} benar" />
  </div>
  <span class="input-group-text fw-bold" data-letter>{letter}</span>
  <input type="text" class="form-control" name="{optionTextName}"
         placeholder="Opsi {letter}" aria-label="Teks opsi {letter}" />
  <!-- tombol hapus: hanya untuk baris ekstra (index >= minimum) -->
  <button type="button" class="btn btn-outline-danger remove-option-btn"
          aria-label="Hapus opsi {letter}" title="Hapus opsi {letter}">
    <i class="bi bi-x-lg"></i>
  </button>
</div>
```

Aturan:
- **Mulai 4 baris** (A–D) saat form fresh — sesuai kebiasaan A–D lama (D-418-01). Bukan 6, bukan 2.
- Tombol hapus (`.remove-option-btn`) ditempel di **akhir input-group** sebagai elemen input-group (`btn btn-outline-danger`), konsisten dengan pola tombol-dalam-input-group yang sudah dipakai di blok gambar (`opt{letter}ImgClear`). Ikon `bi-x-lg`.
- Baris A (index 0) dan baris B (index 1) = **wajib**, tombol Hapus **tidak dirender / disembunyikan** (`d-none`) selama jumlah baris == minimum (2). Begitu baris > 2, semua baris di atas indeks-1 boleh dihapus.
- **Authoring** mempertahankan blok gambar opsi inline (`img-drop ... ms-4`, id `opt{letter}Img*`) per baris — harus ikut tambah/hapus dinamis. **Inject TIDAK** punya blok gambar (scope 394) — baris inject = input-group saja.

### C2 — Tombol "+ Tambah Opsi"

Ditempatkan **tepat setelah daftar baris opsi, sebelum `form-text` bantuan**, di dalam `#optionsSection`.

```
<button type="button" class="btn btn-outline-primary btn-sm mt-1" id="addOptionBtn">
  <i class="bi bi-plus-circle me-1"></i>Tambah Opsi
</button>
```

- Style `btn-outline-primary btn-sm` (selaras tombol sekunder ringan yang sudah dipakai, mis. `opt{letter}ImgPickBtn`).
- Menambah 1 baris di posisi terakhir, hingga **maksimum 6** baris.
- Saat jumlah baris == 6 → tombol `disabled` + `title="Maksimal 6 opsi"`. Saat baris < 6 → enabled kembali.

### C3 — Penomoran Huruf A–F (re-letter)

- Huruf **display-only**, ditetapkan **dari posisi baris** (index 0→A, 1→B, … 5→F). Bukan disimpan; grading tetap by `PackageOption.Id`.
- Sumber huruf JS tunggal: `var LETTERS = ['A','B','C','D','E','F'];`.
- **Setiap tambah/hapus baris → re-letter semua baris berurutan** dari atas: update `data-letter` span, `placeholder` input ("Opsi {letter}"), `aria-label` (teks + correct-input + tombol hapus), dan `id`/`name` berbasis index (lihat C6).
- Authoring: blok gambar opsi ikut di-relabel (`aria`/meta), tapi gambar terikat ke baris-nya (pindah bila baris di atasnya dihapus).

### C4 — Selektor Jawaban Benar (radio ↔ checkbox)

- Reuse `.correct-input` + `applyQTypeSwitch()` yang sudah ada (`ManagePackageQuestions.cshtml:630-634`): SingleAnswer/MultipleChoice = `radio`, MultipleAnswer = `checkbox`; saat bukan MA, semua di-uncheck.
- Saat **baris ditambah** → `applyQTypeSwitch(currentType)` di-panggil ulang agar input baru ikut tipe yang benar (radio/checkbox) dan ter-wire.
- Saat **baris yang sedang ter-check "benar" dihapus**:
  - MultipleChoice (radio): jika opsi terbenar dihapus → tidak ada yang ter-check → submit tervalidasi gagal (correct wajib) → tampil error `Pilih kembali jawaban benar — opsi yang sebelumnya benar telah dihapus.` (inline, lihat C5). Tidak auto-pilih baris lain.
  - MultipleAnswer (checkbox): hilangkan satu correct; sisa correct lain tetap. Bila jadi <2 correct di submit, validator min-correct yang menangani.
- Radio MC: pastikan `name` dibuat seragam grup agar single-select tetap bekerja lintas baris dinamis (lihat C6 — gunakan satu `name` grup untuk radio, atau pertahankan pola per-index dengan binder yang benar; keputusan binder = diskresi planner).

### C5 — Pesan Validasi & Error (inline)

Reuse pola alert yang **sudah ada di form**:
- Banner info MA: `#maLabel` = `alert alert-info py-1 px-2 small mb-2` (sudah ada).
- Error inline: pola `#injAuthError` = `alert alert-danger py-1 px-2 small d-none mb-2` (sudah ada di inject form). **Authoring tambah elemen serupa** `#authError` di dalam/atas `#optionsSection` untuk menampung pesan client-side (min/max/correct-hilang).
- Semua error tampil **inline di atas/di bawah daftar opsi** (bukan toast, bukan `alert()` browser) — konsisten dengan form yang ada.

**Edit-shrink ditolak (D-418-02) — UX wajib:**
- Server meng-guard sebelum `SaveChangesAsync`. Bila gagal → **JANGAN 500 mentah**.
- Untuk submit AJAX (`EditQuestion`): kembalikan respons error terstruktur (status non-200 / `{ ok:false, message }`) lalu form tampilkan pesan di `#authError` (`alert alert-danger`) dengan copy edit-shrink di Copywriting Contract.
- Untuk submit full-page (fallback): redirect kembali ke form dengan `TempData`/model-error yang dirender sebagai `alert alert-danger` di atas form. Tidak ada kehilangan data; opsi tetap utuh.

### C6 — Binding & ID Konvensi (untuk render-array & populateEditForm)

- **Hapus** array hardcoded `optLetters/optFields/correctFields = [A,B,C,D]` di `populateEditForm()` (`:694-696`) → **enumerasi dinamis 0..n** dari `data.options` (panjang variabel; JSON GET sudah variable-length — JANGAN ubah bentuk JSON).
- `IMAGE_FIELDS` (`:726-732`) dibangun dinamis 0..MAX (6) untuk authoring (inject tak punya).
- ID/`name` per baris harus konsisten berbasis index agar binder server (list/array ≤6) dan JS prefill cocok. Konvensi eksak (id pattern, model binder list vs FormCollection) = **diskresi planner** (CONTEXT D-418-01 + spec §8.1), tapi WAJIB: edit soal 4-opsi lama mem-prefill identik (backward-compat, lihat C8).

### C7 — Render Huruf A–F (layar ujian/hasil/ringkasan/preview)

Semua sudah **index-derived dengan fallback numerik** — kontraknya: **perluas array `{A,B,C,D}` → `{A,B,C,D,E,F}`** (atau samakan jadi satu helper).

| View | Lokasi | Aksi |
|------|--------|------|
| `StartExam.cshtml` | `:137` (MA), `:170` (MC) | array → A–F; pola `oi < letters.Length ? letters[oi] : (oi+1).ToString()` dipertahankan |
| `Results.cshtml` | `:363` | array → A–F |
| `ExamSummary.cshtml` | `:57` | array → A–F |
| `_PreviewQuestion.cshtml` | (modal preview) | huruf dinamis A–F |
| `PreviewPackage.cshtml` | `:6`, `:62` | **PERBAIKI BUG**: `{A,B,C,D,E}` cap-at-E + `letters[idx % letters.Length]` (modulo → opsi ke-6 tampil "A") → jadikan `{A,B,C,D,E,F}` **tanpa modulo wrap**; opsi >6 fallback numerik (mestinya tak terjadi krn max-6) |

Penanda benar di render (success styling, badge) — tidak berubah. Format huruf render = `@letter.` (huruf + titik), warna `text-secondary` (StartExam) / inherit (Results/Preview).

### C8 — Backward Compatibility (wajib lolos)

- Soal lama 4-opsi (A–D) **render identik** di semua layar (array A–F superset; 4 opsi → A–D tampil sama).
- Edit soal 4-opsi lama → form mem-prefill **4 baris terisi** (bukan 6), tombol Hapus aktif untuk baris 3–4 (C/D di atas minimum), tombol Tambah enabled (bisa ke 5–6).
- Soal lama bergambar opsi → thumbnail prefill identik (IMAGE_FIELDS dinamis tetap menemukan field per index).
- Grading by `PackageOption.Id` — tidak berubah; nilai soal lama identik.

---

## Accessibility

- Setiap input teks opsi: `aria-label="Teks opsi {letter}"` (re-label saat re-letter).
- Setiap selektor benar: `aria-label="Opsi {letter} benar"` (sudah ada — re-label saat re-letter).
- Tombol Hapus: `aria-label="Hapus opsi {letter}"` + `title` sama; ikon-saja WAJIB punya aria-label (pola sudah dipakai `bi-x-lg` clear-image).
- Tombol Tambah disabled @6: pertahankan fokus-able? — gunakan `disabled` (cukup) + `title="Maksimal 6 opsi"`.
- Keyboard: tombol Tambah/Hapus = `<button type="button">` (operable Enter/Space native). Setelah tambah baris, fokus **dipindah ke input teks baris baru**; setelah hapus, fokus ke input baris sebelumnya (hindari fokus hilang).
- Screen-reader: span huruf (`data-letter`) cukup sebagai teks; asosiasi huruf↔opsi lewat `aria-label` input. Banner info MA pakai `alert alert-info` (role alert implisit cukup untuk konteks form statis).
- Pesan error inline: tempatkan di container yang sama tiap kali agar SR menemukannya; pertimbangkan `role="alert"` pada `#authError` agar dibacakan saat muncul.

---

## Copywriting / Color / Spacing — Kepatuhan Singkat

- **Spacing:** multiples of 4 via kelas Bootstrap; pengecualian `width:36px` & `ms-4` (eksisting) di-cite.
- **Color:** semantik Bootstrap (primary/secondary/danger/info/success) — danger hanya untuk hapus & error.
- **Typography:** reuse `small`, `fw-bold`, `fw-medium`, `form-label`, `form-text` — tidak ada ukuran kustom baru.

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| none (Bootstrap 5 lokal, no shadcn) | — | not applicable |

Tidak ada registry pihak ketiga; tidak ada vetting gate.

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
