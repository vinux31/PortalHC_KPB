# Sosialisasi PortalHC v2 — Slide Deck Web (Design Spec)

**Status:** Draft — pending user review
**Date:** 2026-05-12
**Author:** Brainstorming session (Claude + Rino)
**Source artifact:** `sosialisasi-portalhc/PPT HC PORTAL KPB (1).pptx` (18 slide, 2.5 MB)

---

## 1. Goal

Bikin file HTML interaktif (`sosialisasi-v2.html`) yang merupakan **redesign dari `PPT HC PORTAL KPB (1).pptx`** dengan format **slide deck 16:9 presenter mode**, lebih bagus dan interaktif dibanding PPT asli. File v1 (`sosialisasi.html`) tetap ada sebagai arsip — tidak disentuh.

### Use case
- Presentasi tatap muka tanggal 27 Maret 2026 di Balikpapan (via proyektor, fullscreen)
- Share link ke pekerja KPB untuk review materi mandiri
- Export ke PDF untuk distribusi offline (email/WA)

---

## 2. Scope

### In scope
- File baru `sosialisasi-portalhc/sosialisasi-v2.html` — single HTML, tanpa build step
- 18 slide mirror dari PPT, sequential 1:1
- Slide deck mode 16:9 (presenter mode, fullscreen)
- Navigasi keyboard + klik + dot indicator + URL hash deep link
- Animasi transisi antar slide, reveal per element
- Dark mode toggle (persist)
- Polish konten: typo fix, normalize whitespace, restructure tabel rigid jadi card responsif
- Print stylesheet → output PDF landscape A4, 1 slide = 1 halaman
- Update `index.html` (hub) — tambah card link ke v2
- Copy 6 image dari PPT ke `assets/ppt-images/` (optimize image1 yang 1.9 MB)
- Reuse screenshot existing di `assets/screenshots/` untuk ilustrasi fitur (di slide tertentu)

### Out of scope
- Mode scroll vertikal (sebelumnya dipertimbangkan, ditolak — pilihan akhir = slide deck only)
- Backend, form submit, video embed
- Framework build (no Vite, no React, no Astro)
- Edit `shared.css` (extend via inline `<style>` di v2)
- Hapus `sosialisasi.html` v1
- Bikin file panduan/praktik baru (`panduan.html`, `praktik.html` tidak disentuh)

---

## 3. Stack & Architecture

### File layout
```
sosialisasi-portalhc/
├── sosialisasi-v2.html         BARU (single HTML, ~1500-2500 baris)
├── sosialisasi.html            v1 arsip (tidak diubah)
├── index.html                  UPDATE: tambah card link v2
├── assets/
│   ├── vendor/                 reuse
│   │   ├── alpinejs.min.js
│   │   ├── alpine-persist.min.js
│   │   └── tailwind.min.js
│   ├── css/shared.css          reuse (link, jangan modify)
│   ├── screenshots/            reuse (analytics/cdp/cmp/daily-ops/master-data)
│   └── ppt-images/             BARU: 6 image dari PPT (image1-6.png, image1 dioptimize)
```

### Tech
- HTML5 single file
- Tailwind CDN lokal (`assets/vendor/tailwind.min.js`)
- Alpine.js lokal (`assets/vendor/alpinejs.min.js`)
- Alpine Persist plugin lokal (`assets/vendor/alpine-persist.min.js`) — untuk dark mode persistence
- `shared.css` linked + custom `<style>` inline di v2 (extend only, no modify shared.css)
- Font: `'Segoe UI', Tahoma, sans-serif` (match existing)
- No build step, no node, no server. Buka langsung di browser.
- Offline-capable (semua asset lokal)

### Brand tokens (match `assets/css/shared.css`)
```css
--navy: #002e6d
--navy-dark: #001c44
--red: #ed1c24
--red-dark: #b0121a
--green: #009640
--amber: #f59e0b
--slate: #64748b
--bg-light: #f0f2f5
--bg-dark: #0f172a
--text-light: #1a1a1a
--text-dark: #f1f5f9
```

### Utility class siap reuse (dari shared.css)
- `.brand-navy`, `.brand-red`, `.bg-brand-navy`, `.bg-brand-red`
- `.card-hover` (lift + shadow on hover)
- `.slide-fade-enter` + `@keyframes slide-fade`
- `.shake`, `.checkmark-pop`
- `body.dark` (dark mode root class)

---

## 4. Layout — Slide Deck 16:9

```
┌─────────────────────────────────────────────────────┐
│                                                     │
│           AREA SLIDE 16:9 (fullscreen-capable)      │
│                                                     │
│           1 slide PPT = 1 layar penuh                │
│           Aspect ratio dipertahankan                 │
│                                                     │
└─────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────┐
│ [‹ Prev]  Slide 5 / 18  ●●●●○○○○○○○○○○○○○○         │  ← floating bottom bar
│           [⛶ Fullscreen] [🌓 Dark] [Hub]            │
└─────────────────────────────────────────────────────┘
```

### Aspect ratio handling
- Container `aspect-video` (Tailwind: `aspect-ratio: 16/9`)
- Max-width desktop: 1280px
- Mobile portrait: tetap 16:9, scale down, prompt "putar HP horizontal"
- Mobile landscape: full 16:9 nyaman

---

## 5. Slide Mapping (18 slide PPT → 18 slide web, sequential 1:1)

| # | Konten PPT | Treatment web v2 |
|---|---|---|
| 1 | Cover: HUMAN CAPITAL PORTAL, KPB, Balikpapan, 27 Maret 2026 | Bg gradient navy→red, big title, fade-in. Logo Pertamina (image1 optimized) |
| 2 | Definisi HC Portal — sistem informasi berbasis web untuk Tim HC KPB | Card center + icon. Intro polish (fix spasi acak) |
| 3 | Struktur Role Pengguna (Admin/HC/IT) — tabel PPT | **Convert tabel → 3 card grid** (icon + role + akses) |
| 4 | Sistem Assessment CMP — tabel jenis/kategori/metode/penilaian | Tabel styled (header navy, row alternating, responsive) |
| 5 | Alur Assessment OJT — Persiapan Data + Buat Assessment | **Flowchart 4 step horizontal**, step clickable popup detail |
| 6 | Alur OJT — Monitoring Real-Time + Penilaian Otomatis | Flowchart 4 step lanjutan (step 5-8) |
| 7 | Assessment Proton — tabel aspek/track per tahun | **Convert tabel → 3 column card** (Tahun 1/2/3) |
| 8 | Alur Proton Tahun 1-2 (Ujian Online) | Flowchart horizontal |
| 9 | Alur Proton Tahun 3 (Interview Offline) | Flowchart horizontal, beda warna (offline=amber) |
| 10 | Coaching CDP overview — program 3 tahun, tabel track type | Intro paragraf + 3 card track (Panelman, Operator, dll) |
| 11 | Hierarki kompetensi per track — Track → Kompetensi Level 1 | **Tree diagram CSS** (parent-child indentasi visual) |
| 12 | Fokus kompetensi Tahun 1-2-3 (Dasar / Lanjutan / Mahir) | 3 card progression (warna gradasi navy → red) |
| 13 | Alur Coaching Tahun 1 & 2 — Siapkan Silabus, Upload Guidance, Mapping | Flowchart 4 step |
| 14 | Coaching Th 1&2 — Hitung Progress + Sertifikasi | Flowchart step 5-8 |
| 15 | Alur Coaching Tahun 3 — Silabus mahir, Mapping, Deliverable | Flowchart 4 step |
| 16 | Coaching Th 3 — Sertifikasi final + Penetapan level | Flowchart step 5-8, sertifikasi pakai checkmark-pop animation |
| 17 | Timeline 3 tahun program Proton — diagram summary | **Diagram horizontal timeline** (3 milestone, animated counter durasi) |
| 18 | Closing / Q&A | Big "Terima Kasih" + kontak HC + link ke `panduan.html` |

### Polish guidelines (pilihan C dari brainstorming)
- Fix `&amp;` artefak → `&`
- Normalize spasi acak hasil paste PPT
- Tambah intro singkat (1 kalimat) di slide yang content-heavy
- Restructure tabel rigid jadi card responsif
- Diagram alur statis dari PPT → HTML/CSS flowchart interaktif (clickable step kalau memungkinkan)

---

## 6. Interaksi

### State global (Alpine `x-data`)
```js
deck() {
  return {
    current: 1,                  // slide aktif
    total: 18,
    dark: $persist(false),       // dark mode
    fullscreen: false,
    showDotPreview: null,        // hover dot → slide title
    init() {
      // URL hash sync: load #slide-N → set current
      // Listen hashchange
      // IntersectionObserver untuk reveal element dalam slide
    },
    next() { if (this.current < this.total) this.current++; this.syncHash() },
    prev() { if (this.current > 1) this.current--; this.syncHash() },
    goTo(n) { this.current = n; this.syncHash() },
    syncHash() { location.hash = `slide-${this.current}` },
    toggleFullscreen() { ... },
    toggleDark() { this.dark = !this.dark }
  }
}
```

### Navigasi
| Trigger | Action |
|---|---|
| Klik tombol `Next ›` | `next()` |
| Klik tombol `‹ Prev` | `prev()` |
| Keyboard `→` / `Space` | `next()` |
| Keyboard `←` / `Backspace` | `prev()` |
| Keyboard `Home` | `goTo(1)` |
| Keyboard `End` | `goTo(total)` |
| Keyboard `Esc` | exit fullscreen kalau aktif |
| Keyboard `F` | toggle fullscreen |
| Klik area kiri 1/3 slide | `prev()` |
| Klik area kanan 1/3 slide | `next()` |
| Klik dot indicator | `goTo(n)` |
| Hover dot | show slide title preview tooltip |
| URL `#slide-5` saat load | `current = 5` |
| Tombol 🌓 | `toggleDark()` |
| Tombol ⛶ | `toggleFullscreen()` |
| Tombol "Hub" | link ke `index.html` |

### Animasi
| Element | Trigger | Behaviour |
|---|---|---|
| Slide enter | `current` berubah | Slide-in dari kanan (next) / kiri (prev), fade, durasi 350ms |
| Element reveal | Slide aktif tampil | Bullet/card muncul bertahap (stagger 100ms) |
| Card hover | Mouse hover | `.card-hover` (lift + shadow) |
| Flowchart step | Slide tampil | Pulse glow di step pertama, sequential pulse |
| Counter (slide 17) | Slide masuk viewport | Angka 0 → target durasi 1.5s |
| Sertifikasi checkmark (slide 16) | Step "sertifikasi" reveal | `.checkmark-pop` |
| Dark mode switch | Klik 🌓 | Smooth transition warna (0.3s) |
| Transition CSS | `prefers-reduced-motion` | Disable semua animasi |

### Accessibility
- `<button>` untuk semua control (bukan `<div onclick>`)
- `aria-label` di tombol icon-only (⛶, 🌓, ‹, ›)
- `aria-current="true"` di dot indicator aktif
- `aria-live="polite"` region untuk announce "Slide 5 dari 18"
- Focus visible outline (Tailwind default `focus:ring`)
- Skip link "ke konten utama" untuk screen reader
- `prefers-reduced-motion` honored

---

## 7. Asset Handling

### PPT images
| File | Size | Treatment |
|---|---|---|
| `image1.png` | 1.9 MB | **Optimize** — resize max 1200px width, convert PNG, target <200 KB |
| `image2.png` | 7.7 KB | Pakai as-is (atau replace SVG Heroicons kalau cuma icon) |
| `image3.png` | 20 KB | Same |
| `image4.png` | 277 KB | Pakai as-is (chart kompleks) |
| `image5.png` | 169 KB | Pakai as-is (chart kompleks) |
| `image6.png` | 22 KB | Pakai as-is atau SVG |

Copy ke `assets/ppt-images/`. Decide replace SVG vs PNG per image saat execute (inspect content).

### Screenshot existing
- Slide 2 (HC Portal pengenalan): pakai screenshot dari `assets/screenshots/analytics/` sebagai ilustrasi dashboard
- Slide 4-9 (Assessment): screenshot dari `assets/screenshots/cmp/`
- Slide 10-16 (Coaching): screenshot dari `assets/screenshots/cdp/`
- Decide screenshot mana persis per slide saat execute (cek isi tiap folder)

### Hub update (`index.html`)
Tambah card baru:
```html
<a href="sosialisasi-v2.html" class="card-hover ...">
  <h3>Sosialisasi v2 (2026)</h3>
  <p>Slide deck interaktif 16:9, redesign dari PPT 27 Maret 2026</p>
  <span class="badge-new">BARU</span>
</a>
```
v1 card tetap ada, dilabel "Versi 1.0 (arsip)".

---

## 8. Print to PDF

### `@media print` rules
- Sticky nav bar bottom → `display: none`
- Dark mode → force light (`body.dark { background: white; color: black }` di print)
- Semua slide → `display: block` (semua slide visible berurutan)
- Page break: `break-before: page` di tiap `.slide`
- Page orientation: landscape A4
- Aspect ratio slide dipertahankan
- Animasi/transition: disable
- Background gradient → solid color fallback
- Font ukuran sedikit diadjust (slide font scale 0.85x)

### Hasil PDF
- 18 halaman A4 landscape, 1 slide = 1 halaman
- Semua konten visible (no hidden tab/accordion — slide deck mode gak ada tab anyway)
- Brand color tetap (navy + red), readable
- Bisa di-distribute via email/WA

---

## 9. Testing

| Test | Cara | Pass criteria |
|---|---|---|
| Render desktop 1920×1080 | Chrome | 16:9 slide fit center, nav bar bottom |
| Render mobile portrait (390px) | DevTools iPhone 12 | Slide scale down, prompt rotasi muncul |
| Render mobile landscape (844×390) | DevTools | 16:9 full nyaman |
| Render tablet (768px) | DevTools iPad | 16:9 fit, nav bar OK |
| Keyboard navigation | →/←/Space/Esc/F/Home/End | Semua keystroke berfungsi |
| Click navigation | Klik kiri/kanan slide area | Prev/next bekerja |
| Dot indicator | Klik dot, hover dot | Jump ke slide, tooltip muncul |
| URL hash deep link | Load `sosialisasi-v2.html#slide-7` | Langsung ke slide 7 |
| Fullscreen API | Klik ⛶ atau F | Browser masuk fullscreen, nav hidden |
| Dark mode persist | Toggle 🌓, reload | State tersimpan |
| Slide transition | Klik next/prev | Slide-in dari kanan/kiri, fade smooth |
| Element reveal | Tampil slide content-heavy | Bullet/card muncul bertahap |
| Print preview | Ctrl+P | 18 halaman A4 landscape, 1 slide/page |
| Reduced motion | OS setting → reduce motion | Animasi disable |
| Offline | Disconnect wifi, reload | Tetap jalan (vendor lokal) |
| Hub link | Klik card v2 di `index.html` | Pindah ke `sosialisasi-v2.html` |

---

## 10. Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `image1.png` 1.9 MB lambat di koneksi kantor | High | Medium | Optimize ke <200 KB, lazy load, atau replace SVG kalau ternyata cuma logo |
| Konten PPT padat (slide 4 tabel 4 kolom) gak muat 16:9 | Medium | Medium | Adjust font-size responsive, atau split jadi 2 slide |
| Fullscreen API ditolak browser (e.g., embedded iframe) | Low | Low | Fallback: maximize viewport via CSS, hide nav |
| Tabel rigid PPT → card responsif tidak match data | Low | Medium | Side-by-side review user setelah implement |
| Polish text (typo fix) salah tafsir konteks | Medium | Low | Konservatif — fix obvious typo aja, kalau ragu skip |
| Alpine init race dengan IntersectionObserver | Low | Low | `x-init` + `defer` script + safe-guard `if (!Alpine) return` |
| Dark mode konflik kontras dengan brand red | Low | Low | Test pair WCAG kontras, adjust hue jika perlu |
| Mobile portrait prompt rotasi annoying | Medium | Low | Dismissible prompt, simpan state dismiss di sessionStorage |

---

## 11. Success Criteria

1. ✅ File `sosialisasi-v2.html` exist di `sosialisasi-portalhc/`, single file, no build, dibuka langsung di browser
2. ✅ 18 slide tercover, urut sequential sesuai PPT
3. ✅ Layout 16:9 presenter mode, fullscreen API jalan
4. ✅ Keyboard navigation lengkap (arrow, space, esc, F, home/end, backspace)
5. ✅ Click navigation (area kiri/kanan slide, tombol prev/next)
6. ✅ Dot indicator + hover preview + click jump
7. ✅ URL hash deep link (`#slide-N`)
8. ✅ Animasi transisi smooth (slide-in, fade, reveal stagger)
9. ✅ Brand consistent: `--navy #002e6d`, `--red #ed1c24`, font Segoe UI
10. ✅ Dark mode toggle + persist via `$persist`
11. ✅ Print to PDF → 18 halaman A4 landscape, 1 slide/page, semua konten visible
12. ✅ Mobile responsive: portrait scale + prompt rotasi, landscape full
13. ✅ Offline-capable (semua vendor + asset lokal)
14. ✅ `index.html` updated, card v2 tampil + link kerja
15. ✅ Konten polish (no `&amp;` artefak, no spasi acak)
16. ✅ Accessibility: aria-label, focus visible, reduced-motion honor
17. ✅ Lighthouse: Performance ≥80, Accessibility ≥90, Best Practices ≥90
18. ✅ Page load <2s di koneksi normal

---

## 12. Open Questions (resolve saat execute)

1. **Isi `image1.png` (1.9 MB)** — logo Pertamina atau foto cover? Kalau bukan official Pertamina logo, cari di repo / skip
2. **Screenshot mapping per slide** — folder mana yang representative per slide (cek `analytics/`, `cdp/`, `cmp/`, `daily-ops/`, `master-data/`)
3. **Image PPT replace strategy** — image2/3/6 (kecil) replace SVG Heroicons atau keep PNG?
4. **Slide split jika overflow** — kalau slide 4 atau 10 tabel terlalu padat untuk 16:9, split jadi 2 slide (total bisa jadi 19-20 slide)?
5. **Dummy data untuk flowchart popup** — popup detail per step alur perlu narasi tambahan (gak ada di PPT). Generate atau skip popup?

---

## 13. Approval

- [x] User: brainstorming Q&A completed (B → A revisi, slide deck mode konfirmasi)
- [x] User: design section 1-5 approved (revised version)
- [ ] User: review spec doc ini, beri approval / koreksi
- [ ] Lanjut: invoke `writing-plans` skill → buat implementation plan
