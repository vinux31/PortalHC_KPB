# Sosialisasi-v2 Revisi R2 — Design Spec

**Tanggal:** 2026-05-13
**File target:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`
**Scope:** Round 2 revisi setelah v2.1 ship — merge 3 BigMenu slide, definisi resmi, fix grid bug 2 slide.
**Total slide:** 20 → 18.

## Context

v2.1 shipped tag `sosialisasi-v2.1` 2026-05-13. User cek di browser, ada feedback:

1. Slide 2: "For Future" perlu **bold + italic** (highlight visual).
2. Slide 3/4/5 (3 BigMenu terpisah) terlalu boros — merge jadi 1 slide.
3. Definisi CMP/CDP yang ada di slide 3/4 perlu diganti pakai teks resmi Pertamina (disederhanakan jadi 1 kalimat).
4. Card "Assessment OJT" di BigMenu CMP terlalu spesifik — generalize jadi "Assessment".
5. Slide 6 catatan kaki sebut `Models/UserRoles.cs` — buang (audience non-teknis).
6. Slide 8/9/17/18 alur "berantakan" di browser.

**Audit Playwright (2026-05-13):**

| Slide | Status |
|---|---|
| 8 Pre/Post Test | ❌ BUG — step 4 wrap baris 2 (overflow `grid-cols-9` dengan total col-span 10) |
| 9 Alur OJT | ❌ BUG — step 4 wrap baris 2 (overflow sama) |
| 17 Coaching Th1-2 | ✅ OK secara visual — `grid-cols-11` match |
| 18 Coaching Th3 | ✅ OK secara visual — `grid-cols-11` match |

User confirm: fix scope hanya slide 8 & 9. Slide 17/18 keep.

## Decisions (from brainstorm)

| Topic | Decision |
|---|---|
| Slide 2 "For Future" | `<strong><em>For Future</em></strong>` |
| Merge slide 3/4/5 | 3 kolom side-by-side dalam 1 slide |
| Definisi CMP | "Platform digital untuk pengelolaan kompetensi secara terintegrasi — penyusunan kebutuhan kompetensi jabatan, pelaksanaan asesmen teknis & leadership, serta IDP." |
| Definisi CDP | "Pembelajaran terstruktur untuk menutup gap kompetensi teknis hasil asesmen — prinsip blended Learning (Assignment, Coaching, Self Study)." |
| Card "Assessment OJT" | → "Assessment", sub-text "Ujian online & sertifikasi kompetensi" (general, drop "per unit operasi") |
| Slide 6 catatan kaki | Hapus referensi `Models/UserRoles.cs` |
| Slide 8 grid bug | `grid-cols-9` → `grid-cols-11` (4 card × col-span-2 + 3 arrow × col-span-1 = 11) |
| Slide 9 grid bug | Row 1 sama fix; Row 2 ubah ke col-span-3 × 3 + col-span-1 × 2 arrow = 11 (standardize grid-cols-11) |
| Slide 17/18 | NO CHANGE — visually clean |

## Urutan Slide Baru (20 → 18)

| # Baru | # Lama (v2.1) | Slide | Status |
|---|---|---|---|
| 1 | 1 | Cover | unchanged |
| 2 | 2 | Definisi HC Portal | minor — "For Future" bold+italic |
| 3 | 3+4+5 | **3 Platform PortalHC** (merged CMP/CDP/BP) | **REWRITE** |
| 4 | 6 | Role Piramida 6 Tier | minor — hapus catatan UserRoles.cs |
| 5 | 7 | Sistem Assessment CMP | unchanged |
| 6 | 8 | Pre/Post Test | **FIX grid bug** |
| 7 | 9 | Alur Assessment OJT | **FIX grid bug** |
| 8 | 10 | Assessment Proton | unchanged |
| 9 | 11 | Alur Proton Th 1-2 | unchanged |
| 10 | 12 | Alur Proton Th 3 | unchanged |
| 11 | 13 | Coaching CDP Overview | unchanged |
| 12 | 14 | IDP & Training Records | unchanged |
| 13 | 15 | Hierarki Kompetensi | unchanged |
| 14 | 16 | Fokus Kompetensi (Table 4x5) | unchanged |
| 15 | 17 | Alur Coaching Th 1-2 | unchanged (audit pass) |
| 16 | 18 | Alur Coaching Th 3 | unchanged (audit pass) |
| 17 | 19 | Timeline Summary | unchanged |
| 18 | 20 | Closing | unchanged |

## Detail per Slide

### Slide 2 — minor tweak

Find:
```html
<strong class="brand-red">BP</strong> (Business Partner &mdash; For Future).
```

Replace:
```html
<strong class="brand-red">BP</strong> (Business Partner &mdash; <strong><em>For Future</em></strong>).
```

### Slide 3 — MERGED 3 Platform (REWRITE)

Replace seluruh slide 3 (BigMenu CMP) dengan layout 3 kolom. Hapus slide 4 (CDP) dan slide 5 (BP) — content masuk ke slide 3.

**Header:** `Tiga Platform PortalHC KPB` + sub "Manajemen · Pengembangan · Strategic Partner"

**Layout:** `grid grid-cols-3 gap-4`

**Kolom 1 — CMP:**
- Border-top brand-navy
- Header: `CMP` (besar) + sub `Competency Management`
- Definisi: "Platform digital untuk pengelolaan kompetensi secara terintegrasi — penyusunan kebutuhan kompetensi jabatan, pelaksanaan asesmen teknis & leadership, serta IDP."
- Sub-modul (4 list-item):
  - 📊 Assessment — Ujian online & sertifikasi kompetensi
  - 🎓 Assessment Proton — Program 3-tahun
  - 🔄 Pre/Post Test — Ukur efektivitas training
  - 🏆 Sertifikasi — Otomatis + renewal
- Footer chip: Admin · HC · Coachee

**Kolom 2 — CDP:**
- Border-top brand-red
- Header: `CDP` + sub `Competency Development`
- Definisi: "Pembelajaran terstruktur untuk menutup gap kompetensi teknis hasil asesmen — prinsip blended Learning (Assignment, Coaching, Self Study)."
- Sub-modul (3 list-item):
  - 🎯 Coaching Proton — Silabus + deliverable + review multi-role
  - 📋 IDP — Individual Development Plan tahunan
  - 📚 Training Records — Riwayat training + sertifikat
- Footer chip: HC · Coach · SrSpv · SH · Coachee

**Kolom 3 — BP (For Future):**
- Border-top dashed slate (muted)
- Opacity 0.7
- Header: `BP` + sub `Business Partner` + badge 🚧 Coming Soon
- Definisi: "Modul HRBP — strategic partner HC ↔ unit operasional untuk workforce planning, employee relations, & advisory."
- Sub-modul (3 placeholder):
  - 🤝 Workforce Planning
  - 👁️ Employee Relations
  - 💡 Strategic Advisory
- Footer chip: **_For Future_** (bold italic)

### Slide 4 — Role Piramida (minor)

Replace catatan kaki teks:

Find:
```html
<strong>Catatan:</strong> Hover tiap role untuk detail akses. Hierarki sesuai <code>Models/UserRoles.cs</code> — Admin (L1) akses penuh, Coachee (L6) akses operasional pekerja.
```

Replace:
```html
<strong>Catatan:</strong> Hover tiap role untuk detail akses. Admin (L1) akses penuh, Coachee (L6) akses operasional pekerja.
```

### Slide 6 — Pre/Post Test (FIX grid bug)

**Bug:** `grid-cols-9` dengan 3 card `col-span-2` + 1 card `col-span-1` + 3 arrow `col-span-1` = total 10 → step 4 wrap baris 2.

**Fix:** Ganti `grid-cols-9` → `grid-cols-11`. 4 card semua `col-span-2` (equal width), 3 arrow `col-span-1`. Total 8 + 3 = 11 ✓.

Card "Gain Score" yang sebelumnya `col-span-1` ditingkatkan ke `col-span-2`, hapus border green hover treatment (atau pertahankan style, tinggal class col-span yang berubah).

### Slide 7 — Alur Assessment OJT (FIX grid bug)

**Row 1 bug sama dengan slide 6.** Fix: `grid-cols-9` → `grid-cols-11`, 4 card equal `col-span-2`, 3 arrow `col-span-1`. Card "Monitoring" (yang tadinya col-span-1) ke col-span-2.

**Row 2 (3 card 5/6/7):** existing `grid-cols-9` dengan col-span-3 + col-span-2 + col-span-2 + 2 arrow = 9. Standardize ke `grid-cols-11`: col-span-3 × 3 card + col-span-1 × 2 arrow = 9 + 2 = 11 ✓.

## File Yang Disentuh

- `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — 4 region edit (slide 2 minor, slide 3 rewrite + delete slide 4/5, slide 4 minor, slide 6 fix, slide 7 fix), plus renumber all x-show ID untuk slide 6 → 18, plus update `total: 20` → `total: 18` di Alpine deck().

## Verification (Test Plan)

1. **Server lokal** — `python -m http.server 8765` di folder sosialisasi-v2.
2. **Playwright screenshot** — slide 3 (merged), 6 (Pre/Post), 7 (Alur OJT), 4 (Role). Verifikasi:
   - Slide 3: 3 kolom side-by-side, BP muted, semua content fit dalam 1 viewport tanpa wrap
   - Slide 6: 4 card single row, no wrap
   - Slide 7: 4 card Row 1, 3 card Row 2, arrow → antar card aligned, ↓ antar row centered
   - Slide 4: footer catatan tidak sebut UserRoles.cs lagi
3. **Counter test** — slide 1/18 sampai 18/18, dot indicator 18 buah, End key → slide 18.
4. **Hash route** — `#slide-3`, `#slide-18` jump direct.
5. **Dark mode** — toggle, semua slide tetap readable.
6. **Tag git** — tag `sosialisasi-v2.2` setelah verify.

## Out of Scope

- Slide 17/18 layout (audit pass — keep).
- Refactor Alpine state machine.
- Asset/icon perubahan (semua emoji unicode, no SVG).
- Mobile portrait optimization (existing media query OK).
