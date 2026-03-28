---
phase: 269
slug: loading-overlay-saat-koneksi-signalr-belum-ready-di-startexam
status: draft
shadcn_initialized: false
preset: none
created: 2026-03-28
---

# Phase 269 — UI Design Contract

> Visual and interaction contract untuk loading overlay SignalR di StartExam.
> Dihasilkan oleh gsd-ui-researcher. Diverifikasi oleh gsd-ui-checker.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none — proyek menggunakan Bootstrap 5 + Razor Pages (bukan React/Next.js/Vite) |
| Preset | not applicable |
| Component library | Bootstrap 5 (sudah di-include di layout) |
| Icon library | Bootstrap Icons (sudah digunakan di proyek) |
| Font | Default system font (Bootstrap default) |

Sumber: RESEARCH.md Standard Stack — tidak ada package baru yang perlu diinstall.

shadcn gate: SKIPPED — proyek adalah ASP.NET Core Razor Pages, bukan React/Next.js/Vite.

---

## Spacing Scale

Declared values (multiples of 4):

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | Gap antara icon dan teks status |
| sm | 8px | Padding internal overlay content |
| md | 16px | Margin bawah spinner ke heading |
| lg | 24px | Margin bawah heading ke status teks |
| xl | 32px | — |
| 2xl | 48px | — |
| 3xl | 64px | — |

Gunakan Bootstrap utility classes: `mb-2` (8px), `mb-3` (16px), `mt-3` (16px) untuk konsistensi dengan file HTML lain di proyek.

Exceptions: Spinner size 3rem x 3rem (48px) — digunakan untuk visibilitas di overlay full-screen. Overlay z-index: 2000 (di atas examHeader z-index 1020 dan Bootstrap modal backdrop 1040).

Sumber: RESEARCH.md architecture patterns + kode existing (StartExam.cshtml line 944 `z-index: 1020`, line 454 `z-index: 2000`).

---

## Typography

| Role | Size | Weight | Line Height |
|------|------|--------|-------------|
| Overlay heading | 20px (Bootstrap `fs-5`) | 600 (semibold, `fw-semibold`) | 1.2 |
| Overlay status | 14px (Bootstrap `small`) | 400 (regular) | 1.5 |
| Button label | 14px (Bootstrap `btn-sm`) | 500 (medium, Bootstrap default button) | 1.5 |
| Body (background — tidak diubah) | 16px | 400 | 1.5 |

Hanya 2 weight digunakan di overlay: 400 (regular) dan 600 (semibold). Konsisten dengan Bootstrap default.

Sumber: RESEARCH.md Code Examples HTML — `fs-5 fw-semibold` untuk heading, `small` untuk status.

---

## Color

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | `rgba(0, 0, 0, 0.7)` | Background overlay full-screen semi-transparan |
| Secondary (30%) | `#ffffff` (putih) | Teks heading, spinner, status teks di atas overlay |
| Accent (10%) | `#f8f9fa` (Bootstrap `btn-light`) | Tombol "Muat Ulang" di error state |
| Destructive | — | Tidak ada destructive action di phase ini |

Accent reserved for: SATU elemen saja — tombol "Muat Ulang" di error state overlay.

Opacity teks status: 75% (`opacity-75` Bootstrap) — membedakan visual antara heading utama dan info status.

Sumber: CONTEXT.md D-01 "semi-transparan" + RESEARCH.md CSS Pattern `rgba(0, 0, 0, 0.7)` + HTML example `btn btn-light`.

---

## Overlay States

Tiga state visual yang harus diimplementasikan:

### State 1: Loading (default saat page load)

| Element | Value |
|---------|-------|
| Spinner | Bootstrap `spinner-border text-light`, 3rem x 3rem, animasi border-spin bawaan Bootstrap |
| Heading | "Mempersiapkan ujian..." |
| Status teks | "Menghubungkan ke server..." |
| Tombol | Tersembunyi (`display: none`) |

### State 2: Connected (transisi sesaat sebelum fade-out)

| Element | Value |
|---------|-------|
| Spinner | Tetap tampil |
| Heading | "Mempersiapkan ujian..." |
| Status teks | "Terhubung!" |
| Tombol | Tersembunyi |
| Durasi tampil | 400ms setelah teks berubah, lalu mulai fade-out |

### State 3: Error (hub gagal konek)

| Element | Value |
|---------|-------|
| Spinner | Tersembunyi (`display: none`) |
| Heading | "Mempersiapkan ujian..." (tetap) |
| Status teks | "Koneksi gagal. Periksa jaringan Anda." |
| Tombol | Tampil — label "Muat Ulang", style `btn btn-light btn-sm mt-3` |
| Overlay | TIDAK DITUTUP — user harus tetap terblokir |

Sumber: CONTEXT.md D-02, D-06 + RESEARCH.md overlayShowError() pattern.

---

## Animation Contract

| Property | Value |
|----------|-------|
| Fade-out duration | 300ms (`transition: opacity 0.3s ease`) |
| Minimum display time | 1000ms (Promise.all dengan setTimeout 1000) |
| Fade-out trigger | Setelah hub connected DAN 1000ms berlalu |
| Post-fade cleanup | Set `display: none` setelah 300ms fade selesai |
| Error state | Tidak ada animasi — langsung ganti konten |

Sumber: CONTEXT.md D-04 + RESEARCH.md CSS overlay pattern.

---

## Interaction Block Contract

| Mekanisme | Implementasi |
|-----------|-------------|
| Block klik dan pointer | Overlay z-index 2000 menutupi semua konten |
| Block keyboard (Tab, Arrow) | `inert` attribute pada `<div class="container-fluid py-3">` (exam container) |
| Block fokus | Otomatis oleh `inert` attribute |
| Restore interaksi | Set `examContainer.inert = false` setelah overlay fade-out selesai |
| Sticky header | Tidak di-inert — di luar exam container, timer tetap berjalan |

Sumber: CONTEXT.md D-03 + RESEARCH.md "Block keyboard" section, rekomendasi `inert` attribute.

---

## Accessibility

| Attribute | Value | Alasan |
|-----------|-------|--------|
| `role="status"` | Pada `#examLoadingOverlay` | Screen reader mengumumkan loading state |
| `aria-label` | `"Mempersiapkan ujian"` | Label deskriptif untuk overlay |
| `aria-live` | Tidak diperlukan — overlay blocking | User tidak bisa berinteraksi selama loading |

Sumber: RESEARCH.md HTML overlay example + CONTEXT.md "Claude's Discretion — aria attributes".

---

## Copywriting Contract

| Element | Copy |
|---------|------|
| Overlay heading | Mempersiapkan ujian... |
| Status — loading | Menghubungkan ke server... |
| Status — connected | Terhubung! |
| Status — error | Koneksi gagal. Periksa jaringan Anda. |
| Button CTA (error state) | Muat Ulang |
| Empty state | Tidak ada — overlay tidak memiliki empty state |
| Destructive confirmation | Tidak ada — tidak ada destructive action di phase ini |

Sumber: CONTEXT.md D-02 "Menghubungkan ke server..." dan D-06 "Koneksi gagal" + "Muat Ulang".

---

## HTML Structure Contract

```html
<!-- Posisi: setelah sticky header closing tag, sebelum <div class="container-fluid py-3"> -->
<div id="examLoadingOverlay" role="status" aria-label="Mempersiapkan ujian">
    <div class="text-center">
        <div class="spinner-border text-light mb-3" style="width: 3rem; height: 3rem;"></div>
        <div class="fs-5 fw-semibold mb-2">Mempersiapkan ujian...</div>
        <div class="small opacity-75" id="overlayStatus">Menghubungkan ke server...</div>
        <button id="overlayReloadBtn" class="btn btn-light btn-sm mt-3"
                style="display:none" onclick="window.location.reload()">
            Muat Ulang
        </button>
    </div>
</div>
```

ID wajib dipertahankan persis: `examLoadingOverlay`, `overlayStatus`, `overlayReloadBtn`.

---

## CSS Contract

```css
#examLoadingOverlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.7);
    z-index: 2000;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-direction: column;
    color: #fff;
    opacity: 1;
    transition: opacity 0.3s ease;
}

#examLoadingOverlay.fade-out {
    opacity: 0;
    pointer-events: none;
}
```

Penempatan CSS: inline di dalam `<style>` block di StartExam.cshtml (bukan file terpisah — konsisten dengan pola existing di halaman yang sama).

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| shadcn official | none | not applicable — proyek bukan React |
| Third-party | none | not applicable |

Tidak ada komponen dari registry eksternal. Implementasi murni menggunakan Bootstrap 5 class yang sudah ada + CSS inline + vanilla JavaScript.

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
