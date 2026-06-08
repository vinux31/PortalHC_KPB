---
phase: 354
slug: render-gambar-di-6-layar
status: draft
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

**Markup yang dihasilkan partial (LOCKED — identik baseline `_PreviewQuestion.cshtml:22` & :71):**

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

> Untuk gambar OPSI: `style="max-height:120px; cursor:pointer"` (cap 120 per D-01) — sisanya identik.
> `mb-3` pada gambar soal mengikuti baseline; gambar opsi baseline TANPA `mb-3` (di dalam `<label>` list-group-item) — pertahankan kesesuaian per surface.

Beda dari baseline `_PreviewQuestion.cshtml` Phase 353 (yang inline, belum punya lightbox): **tambah** atribut trigger lightbox (`cursor:pointer`, `role="button"`, `tabindex="0"`, `data-bs-toggle`, `data-img-src/alt`, `aria-label`). Ini upgrade markup, bukan markup baru. RND-04 (`_PreviewQuestion`) opsional di-retrofit ke partial sama agar 6 layar benar-benar 1 sumber (diskresi planner; CONTEXT D-04 menyebut "dipakai 6 layar").

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

### 3. Penempatan Gambar Opsi (D-03)

Gambar opsi di **BAWAH** teks opsi, **full block** (bukan layout samping/inline) — mirror `_PreviewQuestion.cshtml:67-73`: gambar disisipkan setelah `<span>@opt.OptionText</span>` di dalam `<label class="list-group-item ...">`. Aman di mobile (tidak sempit). Cap 120px.

---

## Spacing Scale

Skala spacing = utility Bootstrap 5 existing (kelipatan 4px via `$spacer` 1rem=16px). Tidak diperkenalkan token baru.

| Token Bootstrap | Value | Usage di fase ini |
|-----------------|-------|-------------------|
| `mb-3` | 16px | Margin bawah gambar SOAL (locked, identik baseline `_PreviewQuestion`) |
| `gap-3` | 16px | Jarak elemen dalam `list-group-item` opsi (existing baseline) |
| `me-1` / `me-2` | 4px / 8px | Inline icon/badge gap (existing) |
| `p-0` / `py-2` | 0 / 8px | Card body & alert padding (existing baseline) |

Exceptions: gambar opsi (cap 120px) TANPA `mb-3` di dalam `<label>` — mengikuti baseline Phase 353 (jarak diatur `gap-3` list-group). Tidak ada nilai spacing non-kelipatan-4.

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
| `StartExam.cshtml` (RND-01) | Peserta | ✅ 240px + lightbox | ✅ 120px + lightbox | `ExamQuestionItem`/`ExamOptionItem` |
| `ExamSummary.cshtml` (RND-02) | Peserta | ✅ 240px + lightbox | ✅ 120px + lightbox | item VM ExamSummary (EditPesertaAnswersViewModel.cs cluster) |
| `Results.cshtml` (RND-03) | Peserta | ✅ 240px + lightbox | ✅ 120px + lightbox | `QuestionReviewItem`/`OptionReviewItem` |
| `_PreviewQuestion.cshtml` (RND-04) | Admin | ✅ 240px (live; +lightbox via partial) | ✅ 120px (live) | `PackageQuestion` (entity langsung) |
| `AssessmentMonitoringDetail.cshtml` (RND-05) | Admin | ✅ 240px + lightbox | ❌ **gambar soal SAJA** (essay tak punya opsi, RND-05) | `EssayGradingItemViewModel` |
| `EditPesertaAnswers.cshtml` (RND-06) | Admin | ✅ 240px + lightbox | ✅ 120px + lightbox | item VM EditPesertaAnswers |

Semua surface memanggil partial sama (D-04) + 1 modal lightbox global per halaman. Shuffle opsi aman otomatis (L-03, object-level spec §8).

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| (none) | Bootstrap 5 existing project (modal, list-group, card) | not applicable — no third-party registry, no shadcn |

Tidak ada library lightbox eksternal (D-02 melarang) — reuse Bootstrap modal existing. Tidak ada surface XSS baru (L-02: `src` ber-encode Razor, bukan HTML mentah).

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS — label BI ("Pratinjau Gambar", "Tutup", "Perbesar gambar"), no fabricated alt, no placeholder empty-state
- [ ] Dimension 2 Visuals: PASS — `img-fluid rounded border`, cap 240/120, lazy, lightbox full-res, opsi di bawah teks
- [ ] Dimension 3 Color: PASS — tidak ada warna baru, reuse Bootstrap netral
- [ ] Dimension 4 Typography: PASS — reuse fw-medium/regular existing, 2 weight
- [ ] Dimension 5 Spacing: PASS — `mb-3`/`gap-3` Bootstrap, kelipatan 4
- [ ] Dimension 6 Registry Safety: PASS — no third-party, no XSS surface baru, Bootstrap modal existing

**Approval:** pending
