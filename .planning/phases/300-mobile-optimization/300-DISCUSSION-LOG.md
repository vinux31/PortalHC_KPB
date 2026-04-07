# Phase 300: Mobile Optimization - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-07
**Phase:** 300-mobile-optimization
**Areas discussed:** Navigasi soal mobile, Touch target & layout, Sticky controls mobile, Swipe & anti-copy compat, Essay textarea di mobile, Landscape vs portrait, Page size di mobile

---

## Navigasi soal mobile

| Option | Description | Selected |
|--------|-------------|----------|
| Offcanvas drawer | Tombol floating di kanan bawah membuka drawer dari kanan berisi grid nomor soal | ✓ |
| Bottom sheet | Swipe up dari bawah untuk melihat grid nomor soal | |
| Horizontal scroll bar | Strip horizontal dengan nomor soal yang bisa di-scroll | |

**User's choice:** Offcanvas drawer
**Notes:** Pattern standar Bootstrap, familiar. Sidebar desktop tetap ada di lg+.

---

## Touch target & layout

| Option | Description | Selected |
|--------|-------------|----------|
| Perbesar padding saja | Tambah padding vertikal pada list-group-item di mobile, min-height 48px | ✓ |
| Full-width card per opsi | Setiap opsi jadi card terpisah dengan border dan shadow | |
| Scale radio/checkbox lebih besar | Naikkan scale input dari 1.2 ke 1.5 di mobile | |

**User's choice:** Perbesar padding saja
**Notes:** Desktop tetap tidak berubah — semua via @media query.

| Option | Description | Selected |
|--------|-------------|----------|
| < 992px / lg | Konsisten dengan col-lg yang sudah ada | ✓ |
| < 768px / md | Sidebar masih tampil di tablet landscape | |

**User's choice:** < 992px / lg

---

## Sticky controls mobile

| Option | Description | Selected |
|--------|-------------|----------|
| Sticky footer always visible | Bar fixed di bawah layar dengan Prev + Next/Submit | ✓ |
| Sticky hanya di halaman terakhir | Tombol Submit saja yang sticky di halaman terakhir | |
| Floating action buttons | Prev/Next sebagai FAB di kiri/kanan bawah | |

**User's choice:** Sticky footer always visible

| Option | Description | Selected |
|--------|-------------|----------|
| Compact timer | Hanya angka timer + badge save status, sembunyikan label | ✓ |
| Tetap sama | Tampilan timer sama persis dengan desktop | |

**User's choice:** Compact timer

---

## Swipe & anti-copy compat

| Option | Description | Selected |
|--------|-------------|----------|
| Swipe horizontal | Swipe kiri/kanan via touch events, threshold 50px | |
| Tanpa swipe | Hanya tombol Prev/Next di sticky footer | ✓ |
| Swipe + visual feedback | Swipe dengan animasi slide | |

**User's choice:** Tanpa swipe
**Notes:** Menjaga compatibility 100% dengan anti-copy Phase 280.

---

## Essay textarea di mobile

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-scroll ke textarea | Scroll otomatis saat focus, sembunyikan footer saat keyboard aktif | |
| Fullscreen textarea | Textarea expand fullscreen overlay | |
| Biarkan default browser | Tidak ada handling khusus | ✓ |

**User's choice:** Biarkan default browser

---

## Landscape vs portrait

| Option | Description | Selected |
|--------|-------------|----------|
| Portrait only focus | Optimasi hanya portrait, landscape bisa dipakai tanpa layout khusus | |
| Dual optimization | Layout berbeda: portrait = offcanvas, landscape = sidebar kembali | ✓ |

**User's choice:** Dual optimization
**Notes:** Landscape di mobile akan menampilkan sidebar kembali karena cukup ruang horizontal.

---

## Page size di mobile

| Option | Description | Selected |
|--------|-------------|----------|
| 5 soal per page di mobile | Kurangi dari 10 ke 5 di < lg | ✓ |
| Tetap 10 | Sama dengan desktop | |
| 1 soal per page | Satu soal per layar | |

**User's choice:** 5 soal per page di mobile

---

## Claude's Discretion

- Implementasi detail offcanvas drawer
- CSS transition untuk page change
- Exact spacing values
- Page size detection mechanism
- Sticky footer styling
- Landscape sidebar width

## Deferred Ideas

- Swipe gesture navigation — ditunda, bisa ditambah jika ada feedback
- Fullscreen essay mode — kompleks, belum diperlukan
