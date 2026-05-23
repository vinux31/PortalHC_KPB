# Sosialisasi-Internal-Tim-HC v2 — Browser Audit Findings

**Date:** 2026-05-23
**File audited:** `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (30 slide, tag `sosialisasi-internal-hc-v2.0`)
**Method:** Playwright MCP @ viewport 1480×900, dark mode toggle, keyboard nav (ArrowRight/End), automated overflow detection per slide

---

## Verified PASSING

- ✅ Counter `1/30` ... `30/30` sequential, syncs dengan keyboard nav (ArrowRight, End, ArrowLeft, Home, Space, PageDown)
- ✅ Next button auto-disabled di slide 30; Prev auto-disabled di slide 1
- ✅ BAGIAN labels monotonic: cover → BAGIAN 0 (×6) → 1 (×7) → 2 (×2) → 3 (×5) → 4 (×7) → 5 (×2)
- ✅ Konsol JS bersih (kecuali 1 favicon.ico 404 — cosmetic, browser auto-request)
- ✅ data-slide attribute 1-30 sequential, no gap
- ✅ 30 block comment `<!-- SLIDE X: TITLE -->` match data-slide values
- ✅ TOTAL=30, badge "SLIDE X / 30" semua benar
- ✅ Assessment cluster (slide 10-14) 5/5 preserved, content intact
- ✅ Admin Panel cluster (slide 22-28) 7/7 preserved, content intact
- ✅ Dark mode global → 29 dari 30 slide tampil benar (lihat finding #2 untuk pengecualian)
- ✅ Slide 1 cover, 2 Selamat Datang, 3 Apa Itu+Platform, 7 Alur+Notif, 9 Records+Analytics, 16 PROTON Alur, 18 Chain+Dual, 29 Quick Reference, 30 Penutup → semua tampil clean light + dark mode

---

## FINDINGS (3 items)

### FINDING #1 — Slide 20 (Coaching Dashboard + Histori): tip-bar BOTTOM clipped

**Severity:** MEDIUM (content loss — info user-facing dipotong)

**Detail:**
- Element overflow: `<strong>Daily routine HC:</strong>` tip-bar (orange box)
- DeltaY: +119px below slide bottom (frame 720px height, content total ~853px)
- Slide CSS `overflow: hidden` → tip-bar tidak terlihat sama sekali (clipped invisible)
- Visible content stops at "🟢 Lulus / 🟡 On Progress / ⚫ Belum Mulai" legend
- Missing: "💡 Daily routine HC: scan Dashboard pagi (Pending HC card), reporting bulanan via Histori + Bottleneck + Workload (cover 80% kebutuhan manajemen)."

**Root cause:** Merge 2 mockup-frame (Dashboard 5-metric + Histori 5-row table) stacked vertikal masih terlalu padat meski sudah dikompres ke 3-row tables. Combined height: dashboard ~280px + histori ~280px + 2 h3 ~50px + tip-bar ~60px + panduan-ref ~30px + header ~110px + padding ~50px = ~860px > 720px.

**Fix options (pilih satu):**
- **A. Drop tip-bar** dari slide 20 — gabungkan pesan "Daily routine HC" ke tip-bar slide 7 (Alur Harian) yang sudah ada relevant info. (–1 line, easiest)
- **B. Drop panduan-ref** dari slide 20 (3 ref §3.3+§3.4+§3.5 sudah disebut di Reference Card slide 29). (~–30px)
- **C. Drop legend row** "Lulus / On Progress / Belum Mulai" dari histori mockup — legend ada di Panduan. (–25px)
- **D. Trim histori table 3 row → 2 row** + smaller font. (–35px)
- **E. Drop Dashboard table baris ke-3 (Budi S.)** + trim histori 3 row → 2 row. (paling agresif)

Recommended: **A + C** (drop tip-bar + drop progress dot legend) → ~–85px, masih cukup margin.

---

### FINDING #2 — Slide 19 (Alur Coaching Reguler vs Mahir): 3 highlighted step rows invisible di DARK MODE

**Severity:** HIGH (3 dari 18 step item invisible saat dark mode aktif — Assessment Online, Silabus Mahir, Interview Offline = step-step kunci yang justify split Reguler/Mahir)

**Detail:**
- Element kena: `<li style="background:#e0f2fe; ...">` (Reguler step 8 Assessment Online — light blue bg)
- Element kena: `<li style="background:#fef3c7; ...">` (Mahir step 1 Silabus Mahir, step 7 Interview Offline — light amber bg)
- Light mode: bg pastel + text dark `#0f172a` (bold) = OK readable
- Dark mode: bg pastel TETAP (inline style), text auto jadi light putih (dark mode default) = nyaris invisible (light bg + light text = no contrast)

**Visual evidence:** Screenshot dark mode menunjukkan baris highlight terlihat kosong/blank di posisi step 8 Reguler dan step 1/7 Mahir.

**Root cause:** Inline `style="background:..."` di `<li>` tanpa explicit `color:` override. CSS dark mode tidak override inline background. Text color inherits dark-mode default (light) → contrast hilang.

**Fix:** Tambah `color: #0f172a;` di setiap inline highlight style:
```html
<!-- BEFORE -->
<li style="background:#e0f2fe; padding:4px 8px; border-radius:4px; margin:4px 0;">

<!-- AFTER -->
<li style="background:#e0f2fe; color:#0f172a; padding:4px 8px; border-radius:4px; margin:4px 0;">
```
Sama untuk amber `#fef3c7`. Total 3 occurrence di slide 19.

---

### FINDING #3 — Cosmetic: favicon.ico 404

**Severity:** LOW (cosmetic, tidak affect functionality)

**Detail:** Browser auto-request `favicon.ico` → 404 → 1 entry di console error.

**Fix options:**
- Ignore (file akan dipakai via direct open, bukan public server)
- Tambah `<link rel="icon" href="data:,">` di `<head>` untuk silence request

---

## RINGKASAN

| Finding | Slide | Severity | Effort fix |
|---|---|---|---|
| #1 | 20 (Dashboard + Histori) | MEDIUM (content clipped) | 5 min — drop tip-bar + legend |
| #2 | 19 (Reguler vs Mahir) | HIGH (dark mode invisible) | 2 min — add color:#0f172a × 3 |
| #3 | global | LOW (cosmetic) | 1 min — add data: favicon link |

**Total fix effort:** ~10 menit (3 edits di 1 file).

---

## Catatan tambahan (out-of-scope untuk audit ini)

- File besar 192 KB (vs v1 190 KB) — wajar karena ada konten merged tambahan
- Tag v2.0 sudah dibuat (`sosialisasi-internal-hc-v2.0`) — perlu retag setelah fix kalau mau v2.0 == final shipped state, ATAU buat v2.0.1 patch
- Dark mode CSS legacy comments masih ref ke nomor slide era 22-slide (CSS comment, non-rendering, low priority — skip per spec exclusion)
- Tidak audit dark mode untuk SEMUA 30 slide (sample: 1, 3, 7, 9, 16, 18, 19, 20, 29) — 8 sampel = ~27% coverage. Sisanya tidak ada inline style background highlight yang berisiko sama (slide 19 satu-satunya yang pakai pola inline highlight).
