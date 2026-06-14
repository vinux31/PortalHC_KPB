---
phase: 354
slug: render-gambar-di-6-layar
status: approved
reviewed_at: 2026-06-08
shadcn_initialized: false
preset: none
created: 2026-06-08
---

# Phase 354 — UI Design Contract

> Kontrak visual & interaksi untuk render gambar soal + opsi assessment di 6 layar. Dihasilkan gsd-ui-researcher, diverifikasi gsd-ui-checker.
>
> **Stack:** ASP.NET Core MVC + Razor + Bootstrap 5 (server-rendered, BUKAN SPA). shadcn/Tailwind tidak berlaku — pakai utility Bootstrap 5 existing + konvensi proyek. Tidak ada design system baru diperkenalkan.
>
> **Sumber kontrak:** 354-CONTEXT.md (D-01..D-04, L-01..L-04 locked), ROADMAP Phase 354 (5 SC), REQUIREMENTS (RND-01/02/03/05/06/07), dan baseline visual kanonik `Views/Admin/_PreviewQuestion.cshtml` (Phase 353, live). 5 layar lain MIRROR baseline ini — anti-drift via 1 partial reusable (D-04).
>
> **Catatan otoritas placement opsi (resolusi checker 2026-06-08):** untuk PENEMPATAN gambar opsi, **D-03 (block-bawah-teks) adalah AUTHORITATIVE**. Partial reusable (D-04) = single source of truth penempatan dan **MENGGANTIKAN** pola inline-flex lama di `_PreviewQuestion.cshtml` Phase 353. Baseline `_PreviewQuestion.cshtml` tetap otoritas untuk MARKUP `<img>` (kelas, cap, lazy, alt, render-if-not-null) — TAPI BUKAN untuk layout flex-row inline-samping. Lihat §3.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (Bootstrap 5 server-rendered, bukan React/Next/Vite — shadcn gate N/A) |
| Preset | not applicable |
| Component library | Bootstrap 5 (existing project) — modal, list-group, card, badge, utility classes |
| Icon library | Bootstrap Icons (`bi bi-*`) — existing project |
| Font | mengikuti `_Layout.cshtml` existing (tidak diubah fase ini) |

**Catatan registry:** tidak ada registry pihak ketiga. Tidak ada library lightbox baru (D-02: WAJIB reuse Bootstrap modal existing — pola `previewModal` di `ManagePackageQuestions.cshtml:263` + view CMP). Registry safety gate: not applicable.

---

## Komponen Inti Fase Ini

### 1. Partial Reusable Render Gambar (D-04 — anti-drift)

Satu sumber kebenaran markup `<img>` dipakai 6 layar. Bentuk persis (1 partial parametrik vs 2 terpisah soal/opsi) = diskresi planner/executor; nama file = diskresi (saran: `Views/Shared/_QuestionImage.cshtml`). Kontrak input-output WAJIB:

| Param | Tipe | Wajib | Keterangan |
|-------|------|-------|------------|
| `ImagePath` | `string?` | ya | Path file gambar (sudah ber-encode Razor lewat `src`). **Render NOTHING bila null/whitespace** (L-02). |
| `ImageAlt` | `string?` | ya | Alt text. Boleh null/empty (gambar dekoratif) → `alt=""` (lihat A11y). |
| `cap` / konteks | enum/int | ya | Menentukan `max-height`: **soal = 240px**, **opsi = 120px** (D-01). Pass param ATAU hardcode per-pemanggilan (diskresi). |

**Markup `<img>` yang dihasilkan partial — MARKUP `<img>` LOCKED, identik baseline `_PreviewQuestion.cshtml:22` & :71 (kelas/cap/lazy/alt/render-if-not-null). Catatan: penempatan/wrapper diatur §3, BUKAN baseline flex-row.**

```cshtml
@if (!string.IsNullOrWhiteSpace(Model.ImagePath))
{
    <img src="@Model.ImagePath" alt="@Model.ImageAlt"
         class="img-fluid rounded border mb-3 question-image-zoom"
         style="max-height:240px; cursor:pointer"
         loading="lazy"
         role="button" tabindex="0"
         data-bs-toggle="modal" data-bs-target="#imageLightboxModal"
         data-img-src="@Model.ImagePath"
         data-img-alt="@Model.ImageAlt"
         aria-label="@(string.IsNullOrWhiteSpace(Model.ImageAlt) ? "Perbesar gambar" : "Perbesar gambar: " + Model.ImageAlt)" />
}
```

> Untuk gambar OPSI: `style="max-height:120px; cursor:pointer"` (cap 120 per D-01). `<img>` opsi WAJIB **block** (`d-block w-100` atau setara) agar pecah dari flex-row — lihat §3. Sisanya identik markup soal.

Beda dari baseline `_PreviewQuestion.cshtml` Phase 353 (yang inline-flex, belum punya lightbox): (a) **tambah** atribut trigger lightbox (`cursor:pointer`, `role="button"`, `tabindex="0"`, `data-bs-toggle`, `data-img-src/alt`, `aria-label`); (b) **ubah penempatan opsi** dari inline-samping → block-bawah (§3, D-03 authoritative). Ini upgrade markup + harmonisasi layout, bukan markup baru. RND-04 (`_PreviewQuestion`) di-retrofit ke partial sama agar 6 layar benar-benar 1 sumber (CONTEXT D-04 "dipakai 6 layar") — konsekuensinya gambar opsi `_PreviewQuestion` ikut jadi block-bawah; lihat catatan §3.

### 2. Lightbox Modal Global (D-02 — 1 instance, reuse 6 layar)

Satu `<div class="modal">` tunggal per halaman (bukan per-gambar), `src` di-set saat klik via JS `show.bs.modal` handler. Struktur LOCKED (pola Bootstrap existing `previewModal`):

```cshtml
<div class="modal fade" id="imageLightboxModal" tabindex="-1"
     aria-labelledby="imageLightboxLabel" aria-hidden="true">
  <div class="modal-dialog modal-xl modal-dialog-centered">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title" id="imageLightboxLabel">Pratinjau Gambar</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Tutup"></button>
      </div>
      <div class="modal-body text-center">
        <img id="imageLightboxImg" src="" alt="" class="img-fluid" />
      </div>
    </div>
  </div>
</div>
```

JS (vanilla, pola event Bootstrap existing):
```js
const lb = document.getElementById('imageLightboxModal');
lb?.addEventListener('show.bs.modal', e => {
  const t = e.relatedTarget;
  lb.querySelector('#imageLightboxImg').src = t.getAttribute('data-img-src');
  lb.querySelector('#imageLightboxImg').alt = t.getAttribute('data-img-alt') || '';
});
```

**Kontrak interaksi lightbox:**

| Aspek | Kontrak |
|-------|---------|
| Trigger | Klik gambar inline (capped) ATAU Enter/Space saat fokus (keyboard) |
| Cursor | `pointer` pada gambar inline (afford klik) |
| Konten modal | Gambar resolusi ASLI (file full-res, no-resize Phase 352 D-04). Modal body `img-fluid` agar tetap fit viewport pada layar kecil; tinggi natural, scroll modal bila perlu |
| Ukuran dialog | `modal-xl` + `modal-dialog-centered` |
| Tutup | Tombol `btn-close` (X), klik backdrop, tombol `Esc` (default Bootstrap) |
| 1 instance | Satu modal per halaman; `src` di-swap on-click (bukan 1 modal per gambar) |

### 3. Penempatan Gambar Opsi (D-03 — AUTHORITATIVE: block-bawah-teks)

**Kontrak placement (LOCKED):** gambar opsi tampil sebagai **block penuh DI BAWAH teks opsi** — BUKAN inline-samping. D-03 adalah pilihan eksplisit user ("Bawah teks, full block, aman di mobile") dan **menang** atas pola flex-row lama di baseline.

**Klarifikasi sumber otoritas (resolusi checker 2026-06-08):**
- Baseline `_PreviewQuestion.cshtml:69-72` membungkus `<img>` opsi sebagai **sibling flex-child** di dalam `<label class="list-group-item ... d-flex align-items-center gap-3">` → secara render itu **inline-di-samping** teks. Pola flex-row itu **TIDAK ditiru**.
- Baseline `_PreviewQuestion.cshtml` adalah otoritas HANYA untuk **markup `<img>`**: kelas `img-fluid rounded border`, cap `max-height` (soal 240 / opsi 120), `loading="lazy"`, `alt`, dan aturan render-if-`ImagePath`-not-null. Otoritas baseline **berhenti di situ** — TIDAK mencakup layout penempatan.
- Partial reusable (D-04) = **single source of truth penempatan** dan **menggantikan** pola inline-flex Phase 353.

**Mekanisme block-bawah yang DI-LOCK (executor pilih salah satu, A diutamakan):**

- **Mekanisme A (diutamakan — restruktur `<label>`):** ubah `<label class="list-group-item ...">` opsi dari flex-row 1-baris jadi **stack vertikal**: baris atas = `[input] [huruf] [teks opsi]` (boleh tetap `d-flex align-items-center gap-3` UNTUK BARIS ITU saja, bungkus dalam `<div>`), lalu **panggilan partial gambar opsi di-tempatkan SEBAGAI BLOCK TERPISAH DI BAWAH baris teks** (sibling block, bukan flex-child sebaris). Wrapper `<label>` jadi `d-flex flex-column` (atau hilangkan `d-flex` dan biarkan block-flow normal), sehingga gambar jatuh ke bawah penuh-lebar.
- **Mekanisme B (alternatif minimal — paksa break dalam flex):** pertahankan `<label>` flex existing TAPI paksa `<img>` opsi `class="... d-block w-100"` + `style="max-height:120px"` sehingga `w-100` mengambil 100% lebar dan **wrap ke baris baru di bawah** teks (flex-wrap). WAJIB tambahkan `flex-wrap` pada `<label>` bila pakai B agar img benar-benar turun (bukan menyusut di samping).

> **Kriteria penerimaan placement (untuk checker/auditor):** pada viewport apa pun, gambar opsi muncul di **baris terpisah DI BAWAH** teks opsi dengan lebar penuh kolom opsi, TIDAK pernah berdampingan horizontal dengan teks. Cap visual 120px (tinggi). Aman di mobile (tidak gepeng/sempit).

**Catatan konsistensi `_PreviewQuestion.cshtml` (untuk planner — BUKAN scope baru):** saat `_PreviewQuestion.cshtml` (RND-04) mengadopsi partial bersama (demi 1-sumber lintas 6 surface, D-04), gambar opsinya **ikut berubah jadi block-bawah** menggantikan pola inline-flex Phase 353 saat ini. Ini **diterima & diinginkan** sebagai harmonisasi visual — RND-04 sudah ter-ship fungsional (gambar tampil benar), perubahan ini murni layout-consistency. **BUKAN penambahan scope**, hanya konsekuensi adopsi partial; planner cukup mencatat retrofit ini pada task RND-04.

---

## Spacing Scale

Skala spacing = utility Bootstrap 5 existing (kelipatan 4px via `$spacer` 1rem=16px). Tidak diperkenalkan token baru.

| Token Bootstrap | Value | Usage di fase ini |
|-----------------|-------|-------------------|
| `mb-3` | 16px | Margin bawah gambar SOAL (locked, identik baseline `_PreviewQuestion`) |
| `gap-3` | 16px | Jarak elemen dalam baris teks opsi (`[input] [huruf] [teks]`) — existing |
| `mt-2` | 8px | Jarak antara baris teks opsi dan gambar opsi block di bawahnya (D-03 placement) |
| `me-1` / `me-2` | 4px / 8px | Inline icon/badge gap (existing) |
| `p-0` / `py-2` | 0 / 8px | Card body & alert padding (existing baseline) |

Exceptions: gambar opsi (cap 120px) sebagai block-bawah memakai `mt-2` (8px) untuk pisah dari teks — bukan `mb-3` baseline lama (baseline lama inline tak butuh margin atas). Tidak ada nilai spacing non-kelipatan-4.

---

## Typography

Tidak ada perubahan tipografi. Fase render gambar reuse style teks soal/opsi existing tiap view. Dicantumkan untuk kelengkapan kontrak (nilai dari baseline `_PreviewQuestion.cshtml` + Bootstrap default).

| Role | Class | Weight | Keterangan |
|------|-------|--------|------------|
| Teks soal | `fw-medium` | 500 (medium) | `<p class="fw-medium mb-3">` baseline |
| Teks opsi | (default) | 400 (regular) | `<span>@opt.OptionText</span>` |
| Label huruf opsi | `fw-medium` | 500 | `<span class="fw-medium">@letter.</span>` |
| Judul modal lightbox | `modal-title` (h5) | 500 | "Pratinjau Gambar" |

Hanya 2 weight efektif (regular 400 + medium 500) — sesuai konvensi existing.

---

## Color

Tidak ada warna baru. Reuse tema Bootstrap existing proyek. Fase render gambar tidak menambah accent/destructive.

| Role | Value | Usage di fase ini |
|------|-------|-------------------|
| Dominant (surface) | Bootstrap `body`/`card` default (putih/terang existing) | Background view & modal |
| Secondary | `border` Bootstrap (`rounded border` pada gambar), `bg-light` modal header (pola existing) | Border gambar, header modal |
| Accent | TIDAK dipakai fase ini | — |
| Destructive | TIDAK ADA aksi destruktif di fase ini (render-only, no CRUD/delete) | — |

Accent reserved for: tidak ada elemen accent diintroduksi di fase ini. Gambar pakai `rounded border` netral (locked baseline).

---

## Copywriting Contract (Bahasa Indonesia)

Semua copy WAJIB Bahasa Indonesia (CLAUDE.md). Fase ini render-only — tidak ada CTA aksi/empty-state data/error form. Copy terbatas pada label lightbox & alt/aria.

| Element | Copy |
|---------|------|
| Primary CTA | (tidak ada — fase render pasif, tanpa tombol aksi) |
| Judul modal lightbox | `Pratinjau Gambar` |
| Tombol tutup modal (aria-label) | `Tutup` |
| Aria-label trigger gambar soal (alt kosong) | `Perbesar gambar` |
| Aria-label trigger gambar soal (alt terisi) | `Perbesar gambar: {ImageAlt}` |
| Aria-label trigger gambar opsi (alt kosong) | `Perbesar gambar` (saran tambah konteks: `Perbesar gambar opsi {huruf}` bila huruf opsi tersedia) |
| Alt text gambar | dari field `ImageAlt` (admin-input, opsional). Bila kosong → `alt=""` (dekoratif), JANGAN fabrikasi alt |
| Empty state (ImagePath null) | TIDAK ADA placeholder — render nothing (L-02). Tidak menampilkan ikon/teks "tidak ada gambar" |
| Error state (gambar gagal load 404) | Pakai perilaku `<img>` browser default (broken-image). TIDAK ada handler `onerror` khusus di scope fase ini (diskresi; bukan requirement) |
| Destructive confirmation | tidak ada (no destructive action) |

---

## Accessibility Contract

| Aspek | Kontrak |
|-------|---------|
| Alt text | `alt="@ImageAlt"` selalu ada. Bila `ImageAlt` kosong → `alt=""` (gambar dekoratif, di-skip screen reader). JANGAN auto-generate alt dari nama file |
| Keyboard fokus trigger | Gambar inline = `role="button"` + `tabindex="0"` → fokusable & aktif via Enter/Space (Bootstrap `data-bs-toggle` menangani Space/Enter pada elemen fokusable) |
| Aria-label trigger | Tiap gambar inline punya `aria-label` deskriptif ("Perbesar gambar[: alt]") agar maksud klik jelas tanpa lihat |
| Modal a11y | `role` dialog implisit via `class="modal"` + `aria-labelledby="imageLightboxLabel"` + `aria-hidden` dikelola Bootstrap. `tabindex="-1"` pada modal root |
| Esc menutup | Default Bootstrap modal (keyboard `Esc`) — tidak di-disable |
| Focus trap & restore | Ditangani Bootstrap modal bawaan (fokus pindah ke modal saat buka, balik ke trigger saat tutup) |
| Tombol close | `btn-close` dengan `aria-label="Tutup"` |
| Kontras | Tidak menambah elemen warna baru — kontras existing Bootstrap dipertahankan |

---

## Konsistensi Lintas 6 Surface

| Surface | Tipe | Gambar Soal | Gambar Opsi | Sumber data (VM) |
|---------|------|-------------|-------------|------------------|
| `StartExam.cshtml` (RND-01) | Peserta | ✅ 240px + lightbox | ✅ 120px block-bawah + lightbox | `ExamQuestionItem`/`ExamOptionItem` |
| `ExamSummary.cshtml` (RND-02) | Peserta | ✅ 240px + lightbox | ✅ 120px block-bawah + lightbox | item VM ExamSummary (EditPesertaAnswersViewModel.cs cluster) |
| `Results.cshtml` (RND-03) | Peserta | ✅ 240px + lightbox | ✅ 120px block-bawah + lightbox | `QuestionReviewItem`/`OptionReviewItem` |
| `_PreviewQuestion.cshtml` (RND-04) | Admin | ✅ 240px (live; +lightbox via partial) | ✅ 120px **block-bawah** (retrofit dari inline-flex → konsistensi via partial, lihat §3) | `PackageQuestion` (entity langsung) |
| `AssessmentMonitoringDetail.cshtml` (RND-05) | Admin | ✅ 240px + lightbox | ❌ **gambar soal SAJA** (essay tak punya opsi, RND-05) | `EssayGradingItemViewModel` |
| `EditPesertaAnswers.cshtml` (RND-06) | Admin | ✅ 240px + lightbox | ✅ 120px block-bawah + lightbox | item VM EditPesertaAnswers |

Semua surface memanggil partial sama (D-04) + 1 modal lightbox global per halaman. Penempatan gambar opsi = block-bawah-teks seragam (§3, D-03). Shuffle opsi aman otomatis (L-03, object-level spec §8).

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| (none) | Bootstrap 5 existing project (modal, list-group, card) | not applicable — no third-party registry, no shadcn |

Tidak ada library lightbox eksternal (D-02 melarang) — reuse Bootstrap modal existing. Tidak ada surface XSS baru (L-02: `src` ber-encode Razor, bukan HTML mentah).

---

## Checker Sign-Off

- [x] Dimension 1 Copywriting: PASS — label BI ("Pratinjau Gambar", "Tutup", "Perbesar gambar"), no fabricated alt, no placeholder empty-state
- [x] Dimension 2 Visuals: PASS — `img-fluid rounded border`, cap 240/120, lazy, lightbox full-res, gambar opsi block-bawah-teks (D-03 authoritative, §3 mekanisme di-lock)
- [x] Dimension 3 Color: PASS — tidak ada warna baru, reuse Bootstrap netral
- [x] Dimension 4 Typography: PASS — reuse fw-medium/regular existing, 2 weight
- [x] Dimension 5 Spacing: PASS — `mb-3`/`gap-3`/`mt-2` Bootstrap, kelipatan 4
- [x] Dimension 6 Registry Safety: PASS — no third-party, no XSS surface baru, Bootstrap modal existing

**Approval:** approved (6/6) — flag placement opsi (§3 ambiguity) RESOLVED 2026-06-08: D-03 block-bawah authoritative, baseline `_PreviewQuestion.cshtml` otoritas markup `<img>` saja (bukan flex-layout), partial reusable menggantikan pola inline-flex, mekanisme block-bawah di-lock (A/B), retrofit RND-04 dicatat sebagai konsistensi (bukan scope baru).
