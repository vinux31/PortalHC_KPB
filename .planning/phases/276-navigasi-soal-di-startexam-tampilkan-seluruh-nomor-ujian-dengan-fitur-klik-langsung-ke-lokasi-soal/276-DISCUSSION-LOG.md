# Phase 276: Navigasi soal di StartExam - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-31
**Phase:** 276-navigasi-soal-di-startexam-tampilkan-seluruh-nomor-ujian-dengan-fitur-klik-langsung-ke-lokasi-soal
**Areas discussed:** Tampilan Panel, Indikator Visual, Behavior Klik Navigasi, Layout & Responsiveness

---

## Area 1: Tampilan Panel (Banyak Soal)

| Option | Description | Selected |
|--------|-------------|----------|
| Grid layout (multi-kolom) | Susunan grid 5-6 kolom (5x10 untuk 50 soal). Compact dan mudah dipindai mata, cocok untuk desktop dengan space cukup. | ✓ |
| Vertical list 1 kolom | Susunan 1 kolom vertikal dengan scroll. Lebih sederhana, mudah diimplement, cocok untuk mobile, tapi butuh scroll panjang untuk banyak soal. | |
| Grouped by page | Nomor soal dikelompokkan per halaman (Page 1: 1-10, Page 2: 11-20, dll). Setiap group ada label/separator. | |

**User's choice:** Grid layout (multi-kolom)

**Follow-up — Grid columns:**

| Option | Description | Selected |
|--------|-------------|----------|
| 5 kolom (default) | 5 kolom, cukup compact untuk 100 soal (20 baris). Cocok untuk panel col-lg-3 yang ada. | |
| 6 kolom (lebih compact) | 6 kolom, lebih compact lagi (17 baris untuk 100 soal). Butuh space horizontal lebih. | |
| 4 kolom (lebih longgar) | 4 kolom, lebih longgar (25 baris untuk 100 soal). Nomor lebih besar dan mudah diklik. | |
| Claude tentukan | Anda tentukan berdasarkan space panel yang ada dan best practice. | |

**User's choice:** 10 kolom, jadi soal 1-10 1 baris (free-text input)

**Notes:** User menentukan 10 kolom agar sejajar dengan 10 soal per halaman (consistent dengan existing pagination pattern).

---

## Area 2: Indikator Visual

| Option | Description | Selected |
|--------|-------------|----------|
| 4 warna (answered, unanswered, current Q, current page) | Hijau=answered, Abu-abu=unanswered, Bir=current question, Kuning=current page (bukan current question). 4 warna berbeda. | |
| 3 warna (answered, unanswered, current Q) | Hijau=answered, Abu-abu=unanswered, Bir=bold/highlight untuk current question saja. 3 warna. | ✓ (initially) |
| 2 warna + border untuk current Q | Hijau=answered, Abu-abu=unanswered. Current question ditandai dengan border/frame, bukan warna berbeda. | |
| Claude tentukan | Anda tentukan berdasarkan best practice accessibility dan consistency dengan UI yang ada. | |

**User's choice:** Awalnya memilih 3 warna, kemudian direvisi menjadi **2 warna saja** (Hijau=answered, Abu-abu=unanswered)

**Follow-up — Current question marking:**

| Option | Description | Selected |
|--------|-------------|----------|
| Tandai dengan visual lain (border/bold) | Current question ditandai dengan visual lain (border, bold, icon, dll) meskipun warna sama dengan answered/unanswered. | |
| Tidak perlu penandaan khusus | Current question tidak ditandai sama sekali. User tahu posisi dari scroll position. | ✓ |

**User's choice:** Tidak perlu penandaan khusus

**Notes:** User merasa current question tidak perlu ditandai karena user bisa melihat posisi dari scroll position.

---

## Area 3: Behavior Klik Navigasi

| Option | Description | Selected |
|--------|-------------|----------|
| Jump langsung + smooth scroll | Klik nomor soal → langsung loncat ke halaman + scroll ke soal tersebut. Smooth scroll animation ke soal. Update current question indicator. | |
| Jump langsung, no animation | Klik nomor soal → loncat ke halaman + scroll tanpa animasi. Lebih cepat, user langsung lihat soal. | ✓ |
| Switch page, scroll to top | Klik nomor soal → switch ke halaman tersebut, scroll ke top halaman saja (tidak ke soal spesifik). User scroll manual ke soal. | |
| Claude tentukan | Anda tentukan berdasarkan best practice UX untuk exam navigation. | |

**User's choice:** Jump langsung, no animation

**Notes:** Immediate jump tanpa animasi lebih cepat dan responsif.

---

## Area 4: Layout & Responsiveness

| Option | Description | Selected |
|--------|-------------|----------|
| Desktop tetap, mobile collapsed | Desktop: panel kanan tetap (col-lg-3), mobile: panel collapsed/hidden dengan toggle button. User buka panel jika perlu navigasi. | ✓ |
| Selalu visible, responsive grid | Panel tetap visible di desktop dan mobile. Responsive grid: 10 kolom di desktop, 5 kolom di mobile. Lebih accessible tapi makan space di mobile. | |
| Always collapsible | Panel always collapsed dengan toggle button, user click toggle untuk show/hide panel. Default visible atau collapsed tergantung screen size. | |

**User's choice:** Desktop tetap, mobile collapsed

**Follow-up — Header text:**

| Option | Description | Selected |
|--------|-------------|----------|
| Navigasi Soal | Nama simple dan jelas, menggantikan 'Questions this page'. | |
| Question Navigator | Simple dan bahasa Inggris, konsisten dengan UI elements lain. | |
| Daftar Soal | Fungsional dan deskriptif. | ✓ |
| Claude tentukan | Anda tentukan yang paling appropriate. | |

**User's choice:** Daftar Soal

**Notes:** Menggantikan "Questions this page" karena sekarang menampilkan SEMUA soal, bukan hanya soal di halaman saat ini.

---

## Area 5: Technical Implementation

| Option | Description | Selected |
|--------|-------------|----------|
| Modify updatePanel() existing | Modify updatePanel() function yang sudah ada untuk render ALL questions (bukan hanya current page). Minimal changes, consistent dengan existing pattern. | ✓ |
| Buat function baru | Buat function baru khusus render full navigation panel, biarkan updatePanel() untuk page-specific update. Lebih modular. | |

**User's choice:** Modify updatePanel() existing

**Notes:** Lebih minimal changes dan consistent dengan existing pattern.

---

## Area 6: Fitur Tambahan

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-scroll ke current question | Panel otomatis scroll ke current question saat page change. | ✓ (Claude's discretion) |
| Jump ke soal spesifik | Quick jump buttons: 'First unanswered', 'Last answered', dll. | |
| Tidak perlu (basic saja) | Tidak perlu fitur tambahan. Kumpulkan semua basic dulu. | |
| Claude tentukan | Anda tentukan fitur tambahan yang valuable tapi tidak scope creep. | |

**User's choice:** Claude tentukan

**Claude's decision:** Auto-scroll ke current question saat page change. Ini membantu user tetap oriented terutama untuk assessment dengan banyak soal. Fitur lain seperti "Jump to first unanswered" dianggap scope creep.

---

## Summary of All Decisions

1. **D-01:** Grid layout (multi-kolom)
2. **D-02:** Grid 10 kolom — soal 1-10 dalam satu baris
3. **D-03:** 2 warna — Hijau (answered), Abu-abu (unanswered)
4. **D-04:** Klik → jump langsung + scroll ke soal, no animation
5. **D-05:** Desktop tetap visible, mobile collapsed
6. **D-06:** Header text: "Daftar Soal"
7. **D-07:** Current question tidak perlu penandaan khusus
8. **D-08:** Modify existing updatePanel() function
9. **D-09:** Auto-scroll ke current question (Claude's discretion)

---

## Claude's Discretion

Areas where user said "you decide" or deferred to Claude:
- Auto-scroll ke current question saat page change (D-09)
- CSS styling detail (padding, gap, border-radius untuk badge grid)
- Exact implementation untuk auto-scroll behavior (smooth vs instant scroll)

---

## Deferred Ideas

Features mentioned but noted for future phases:
- "Jump to first unanswered" button
- "Jump to last answered" button
- Question search/filter di panel
- Progress percentage indicator di panel

---

*Discussion Log: Phase 276*
*Date: 2026-03-31*
