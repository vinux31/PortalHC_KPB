# UI REVIEW — Guide & GuideDetail Pages

**Scope:** `/Home/Guide` (hub) + `/Home/GuideDetail?module={cmp|cdp|account|data|admin}` (5 sub-pages)
**Files Audited:** `Views/Home/Guide.cshtml`, `Views/Home/GuideDetail.cshtml`, `wwwroot/css/guide.css`
**Date:** 2026-03-24

---

## Overall Score: 21/24

| Pillar | Score | Verdict |
|--------|-------|---------|
| Copywriting | 4/4 | Excellent |
| Visuals | 3/4 | Good |
| Color | 4/4 | Excellent |
| Typography | 4/4 | Excellent |
| Spacing | 3/4 | Good |
| Experience Design | 3/4 | Good |

---

## 1. Copywriting — 4/4

**Strengths:**
- Semua teks dalam Bahasa Indonesia yang konsisten dan natural
- FAQ answers informatif, to-the-point, dengan formatting bold pada kata kunci penting
- Step descriptions jelas dengan pola "Aksi → Konteks" yang mudah diikuti
- Placeholder search bar memberikan contoh konkret: "assessment, coaching, IDP, upload"
- Hero subtitle efektif: "langkah demi langkah"
- Role badge "Anda login sebagai: {role}" memberikan konteks langsung
- Setiap card module menampilkan jumlah panduan yang tersedia (e.g., "5 panduan tersedia")

**No issues found.**

---

## 2. Visuals — 3/4

**Strengths:**
- Card design konsisten dengan icon gradient per module (CMP=purple, CDP=green, Account=blue, Data=orange, Admin=pink)
- Hero section dengan decorative blur circles menambah depth
- Accent bar animation pada card hover (width 0→100%) halus dan informatif
- Step badges dengan gradient dan shadow memberikan visual hierarchy yang jelas
- Module-specific color variants (step-variant-green, teal, orange, blue) membedakan konten antar module
- Tutorial card variants (CMP purple, CDP green, Admin pink) konsisten dengan icon colors
- Back-to-top button clean dan fungsional

**Issues:**
- **[MINOR]** `guide-card-header-text p` deskripsi ("5 panduan tersedia") menggunakan warna `#4a5568` yang cukup mirip dengan body text — bisa lebih distinct sebagai secondary text
- **[MINOR]** GuideDetail header icon (`guide-detail-header-icon`) 80x80px besar — pada mobile bisa terasa dominan relatif terhadap teks

---

## 3. Color — 4/4

**Strengths:**
- 5 gradient palettes terdefinisi rapi di CSS variables dan digunakan konsisten
- FAQ hover state menggunakan subtle `rgba(102,126,234,0.04)` background + left border accent — tidak mengganggu readability
- Search highlight (`mark.search-highlight`) menggunakan kuning `#fef08a` dengan teks gelap `#854d0e` — kontras tinggi
- High contrast mode support (`@media (prefers-contrast: high)`) lengkap
- Print styles mengkonversi gradients ke solid colors untuk kualitas cetak
- Focus indicators konsisten menggunakan `#667eea` sebagai primary accent

**No issues found.**

---

## 4. Typography — 4/4

**Strengths:**
- Font family Inter dengan fallback system fonts yang tepat
- Type scale terstruktur: Hero h1 (2.4rem/800) → Section h2 (1.4rem/700) → Card h5 (1.1rem/700) → Body (0.95rem) → Small (0.82rem)
- FAQ question weight 600 membedakan dari answer text
- Step badge text 0.8rem/700 compact tapi readable
- Mobile responsive: hero h1 turun ke 1.5rem, section h2 ke 1.2rem
- iOS auto-zoom prevention dengan `font-size: 16px` pada mobile search input
- FAQ section h2 menggunakan 1.7rem/800 — sedikit lebih besar dari section headings, memberikan hierarchy yang tepat untuk section break

**No issues found.**

---

## 5. Spacing — 3/4

**Strengths:**
- Grid layout `repeat(auto-fill, minmax(320px, 1fr))` responsive tanpa media query
- Card padding konsisten 1.5rem
- FAQ section dipisahkan dengan `border-top: 2px solid #e8edf5` + `margin-top: 3rem` + `padding-top: 2.5rem` — clear visual break
- Step items menggunakan 0.75rem gap dengan consistent border-bottom
- Search wrapper margin-bottom 2.5rem memberikan breathing room
- Mobile: card grid beralih ke single column dengan reduced gap (1rem)

**Issues:**
- **[MINOR]** Mobile card grid menambahkan `padding: 0 1rem` — ini bisa conflict dengan parent container padding, potentially creating uneven horizontal margins
- **[MINOR]** GuideDetail `guide-list-body` padding `1rem 1rem 2rem 1rem` — bottom padding 2rem cukup besar, bisa terasa terlalu longgar pada accordion pendek (2 step items)

---

## 6. Experience Design — 3/4

**Strengths:**
- **Search:** Real-time client-side filter dengan highlight terms, shake animation pada no-results, aria-live announcements
- **Accessibility:** Skip links, sr-only labels, aria-expanded states, keyboard support (Escape clears search, Space toggles FAQ)
- **Reduced motion:** Complete `prefers-reduced-motion` support yang disable semua animasi
- **Print:** Comprehensive print styles — auto-expand all accordions, hide interactive elements, page-break management, Q: prefix untuk FAQ
- **Role-based content:** Admin/HC-only cards dan FAQ categories tersembunyi untuk user biasa
- **Breadcrumb navigation:** Clear path dari Beranda → Panduan → Module
- **Toggle All FAQ:** "Buka Semua / Tutup Semua" button untuk batch operation
- **Content-visibility:** `content-visibility: auto` pada FAQ section untuk rendering performance

**Issues:**
- **[MEDIUM]** GuideDetail accordion menggunakan `data-bs-parent="#guideAccordion"` yang menutup accordion lain saat satu dibuka — untuk halaman panduan reference, user mungkin ingin membuka beberapa guide sekaligus untuk perbandingan. Pertimbangkan menghapus `data-bs-parent` agar multiple accordions bisa terbuka bersamaan.
- **[MINOR]** Tidak ada "Kembali ke Panduan" button yang prominent di GuideDetail — user hanya mengandalkan breadcrumb. Sebuah back button di bawah accordion list bisa membantu navigasi.
- **[MINOR]** Search di Guide hub hanya filter cards dan FAQ — tidak ada search functionality di GuideDetail page untuk mencari dalam konten accordion steps.

---

## Top 3 Fixes (Priority Order)

1. **GuideDetail: Hapus `data-bs-parent` dari accordion** — Biarkan user membuka multiple guides sekaligus. Ini lebih cocok untuk halaman referensi/dokumentasi dibanding single-accordion behavior. (Experience Design)

2. **GuideDetail: Tambah back navigation button** — Tambahkan link "← Kembali ke Panduan" di akhir halaman detail untuk memudahkan navigasi tanpa scroll ke breadcrumb. (Experience Design)

3. **Mobile: Review card grid padding** — Pastikan `padding: 0 1rem` pada `.guide-card-grid` di mobile tidak conflict dengan parent container, atau gunakan negative margin technique. (Spacing)

---

## Positive Highlights

- Accessibility implementation sangat thorough (skip links, aria-live, focus-visible, high contrast, reduced motion)
- Print stylesheet adalah contoh best practice — auto-expand, clean layout, page numbers
- Color system konsisten dari hub → detail pages
- Content quality FAQ sangat baik — jawaban informatif dengan formatting yang membantu scanning
- Performance optimization (content-visibility, contain) sudah diterapkan

---

## UI REVIEW COMPLETE
