# Sosialisasi-v2 Revisi R3 — Design Spec

**Tanggal:** 2026-05-13
**File target:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`
**Scope:** Round 3 content revisi — singkatan platform akurat, nama assessment general, IDP page sebagai perpustakaan, alur coaching simplifikasi, semua chip role/akses dihapus.
**Total slide:** 18 (unchanged).
**Output tag:** `sosialisasi-v2.4` (skip patch level karena content + structural changes).

## Context

User feedback round 3 setelah PDF v2.2.3 ship:

1. **Singkatan tidak akurat** — Slide 2 sebut "CMP (Assessment)" / "CDP (Coaching & Development)" yang reduktif. User minta nama platform penuh.
2. **Nama assessment tidak general** — "Assessment OJT" terlalu spesifik, padahal modul juga cover IHT, K3, dll. Perlu nama generic.
3. **IDP page salah pahami** — Card IDP existing positioning sebagai "target tahunan dengan approval" tidak match realita: page IDP sebetulnya **perpustakaan** dokumen IDP & KKJ untuk worker view & download.
4. **Coaching workflow kurang akurat** — Slide 15/16 step naming tidak match user mental model. Step 3 harusnya emphasize HC sebagai aktor (HC Assign). Step 4 harusnya Coach yang submit evidence on behalf coachee (bukan Coachee submit).
5. **Slide 16 redundant** — "Review Mendalam" + "Coaching Intensif" terlalu spesifik Th3, sebaiknya sama struktur dengan slide 15 (Review Multi-Role + Approval/Revisi).
6. **Audience non-teknis tidak butuh chip role** — Semua role/akses chip yang menempel di card slide dihapus.
7. **Penilaian 5 aspek (skor 1-5) terlalu detail teknis** — Slide 8 + slide 10 generalize jadi "Penilaian Kompetensi".

## Decisions (from brainstorm)

| Topic | Decision |
|---|---|
| Singkatan CMP/CDP/BP | CMP = Competency Management Platform · CDP = Competency Development Platform · BP = Business Partner |
| Slide 3 CMP definisi | "Platform digital untuk pengelolaan kompetensi secara terintegrasi — penyusunan **KKJ, IDP**, pelaksanaan asesmen teknis & **Safety**" |
| Nama "Assessment OJT" | → "Assessment Umum" (cover OJT/IHT/K3 dll) |
| Slide 5 kategori | "Per unit operasi (misal Alkylation, RFCC, NHT)" → "Per batch unit operasi / batch" |
| Slide 5 tips | "Assessment Umum untuk evaluasi reguler per batch unit/jenis kompetensi, Proton untuk program pengembangan 3 tahun." |
| Slide 6 card #2 | "Training / OJT" → "Training" |
| Slide 7 judul | "Alur Assessment OJT" → "Alur Assessment" |
| Slide 8 Card Tahun 3 | hapus bullet "Penilaian 5 aspek (skor 1-5)" |
| Slide 9 callout | "Mirip OJT" → "Mirip Assessment Umum" |
| Slide 10 step 3 | "Penilaian 5 Aspek" → "Penilaian Kompetensi" + sub-text general |
| Slide 12 IDP card | revamp jadi perpustakaan dokumen IDP & KKJ |
| Slide 14 row Coaching Process | Th1/Th2: "Submit evidence (Coach) → Multi Approval → Final Assessment". Th3: "... → Final Assessment Interview" |
| Slide 15 step 3 | "Mapping Track" → "HC Assign Coachee" |
| Slide 15 step 4 | "Kerjakan Deliverable" → "Coach Submit Evidence" (per deliverable) |
| Slide 16 step 4 | "Review Mendalam" → "Review Multi-Role" (sub identical slide 15 step 5) |
| Slide 16 step 5 | "Coaching Intensif" → "Approval / Revisi" (sub identical slide 15 step 6) |
| Semua slide | hapus chip footer "Role: ...", "Akses: ..." |
| Slide 4 (Role Piramida) | UNCHANGED — content slide ini emang struktur role |

## Detail per Slide

### Slide 2 — Definisi (minor)

Replace inline strong text:

```
old: <strong class="brand-red">CMP</strong> (Assessment),
new: <strong class="brand-red">CMP</strong> (Competency Management Platform),

old: <strong class="brand-red">CDP</strong> (Coaching &amp; Development),
new: <strong class="brand-red">CDP</strong> (Competency Development Platform),
```

BP unchanged ("Business Partner — For Future" — already correct).

### Slide 3 — Tiga Platform (rewrite cards)

**Kolom 1 (CMP):**
- Subtitle: "Competency Management" → "Competency Management Platform"
- Definisi: rewrite jadi "Platform digital untuk pengelolaan kompetensi secara terintegrasi — penyusunan **KKJ, IDP**, pelaksanaan asesmen teknis & **Safety**."
- Hapus footer chip "Role: Admin · HC · Coachee"

**Kolom 2 (CDP):**
- Subtitle: "Competency Development" → "Competency Development Platform"
- Hapus footer chip "Role: HC · Coach · SrSpv · SH · Coachee"
- Definisi tetap (versi blended Learning)

**Kolom 3 (BP):**
- Subtitle "Business Partner" — keep
- Hapus footer chip kalau ada (cek)

### Slide 5 — Sistem Assessment (rewrite header + row 1)

- Judul: "Sistem Assessment (CMP)" → "Sistem Assessment"
- Subtitle: "Dua jenis assessment utama dalam Competency Management Program:" → "Dua jenis assessment utama:"
- Table Row 1 nama: "Assessment OJT" → "Assessment Umum"
- Table Row 1 Kategori: "Per unit operasi<br>(misal: Alkylation, RFCC, NHT)" → "Per batch unit operasi / batch"
- Tips: "OJT untuk evaluasi reguler per unit, Proton untuk program pengembangan 3 tahun." → "Assessment Umum untuk evaluasi reguler per batch unit/jenis kompetensi, Proton untuk program pengembangan 3 tahun."

### Slide 6 — Pre/Post Test (minor)

Card #2 title: "Training / OJT" → "Training"

### Slide 7 — Alur Assessment (rename judul)

- Comment: `<!-- Slide 7: Alur Assessment OJT -->` → `<!-- Slide 7: Alur Assessment -->`
- h2: "Alur Assessment OJT" → "Alur Assessment"
- Hapus footer Role catatan ("Role: Admin / HC siapkan data..." + "Output: ...") — atau keep Output, hapus Role saja. Pilih hapus Role saja.

### Slide 8 — Assessment Proton (Card Tahun 3 bullet hapus)

Hapus bullet:
```html
<li>• Penilaian 5 aspek (skor 1-5)</li>
```

Card Tahun 3 sisa 2 bullet:
- Track mahir
- **Interview offline** oleh panel juri

### Slide 9 — Alur Proton Th 1-2 (callout update)

Callout "Mirip OJT" box title → "Mirip Assessment Umum". Sub-text update: "Alur online sama dengan Assessment Umum, beda di kategori & paket soal per track."

### Slide 10 — Alur Proton Th 3 (Card #3 general)

- Title: "Penilaian 5 Aspek" → "Penilaian Kompetensi"
- Sub-text: "Skor 1-5 per aspek kompetensi oleh panel" → "Penilaian kompetensi oleh panel juri"

### Slide 11 — Coaching CDP Overview (cek footer role)

Read existing — jika ada chip role/akses hapus. Otherwise no change.

### Slide 12 — IDP & Training Records (IDP card revamp)

**IDP card baru:**
- Title: `IDP`
- Subtitle: "Individual Development Plan (Perpustakaan)"
- Bullets:
  - 📂 Repository dokumen IDP per pekerja
  - 📄 Akses dokumen KKJ (Kebutuhan Kompetensi Jabatan)
  - 👁️ Worker view & download dokumen
  - 🔍 Filter & search per jabatan / unit
- Hapus `Akses: ...` footer

**Training Records card:**
- Bullets dan title unchanged
- Hapus `Akses: ...` footer

### Slide 13 — Hierarki Kompetensi (no change)

### Slide 14 — Fokus Kompetensi Table (Row Coaching Process update)

Row "🔄 Coaching Process":
- Tahun 1: "Submit evidence → Coach review → HC review" → "Submit evidence (Coach) → Multi Approval → Final Assessment"
- Tahun 2: Same as Tahun 1
- Tahun 3: "Submit → Coach → HC review + **Final Assessment Interview**" → "Submit evidence (Coach) → Multi Approval → **Final Assessment Interview**"

Row Assessment Th3 sub-text "(5 aspek, skor 1-5)" → hapus (general).

### Slide 15 — Alur Coaching Th 1-2 (step 3 + 4 + footer)

**Step 3** "Mapping Track":
- Title: "Mapping Track" → "HC Assign Coachee"
- Sub-text: "Assign coachee ke track tahun program" → "HC assign coachee ke track tahun program"

**Step 4** "Kerjakan Deliverable":
- Title: "Kerjakan Deliverable" → "Coach Submit Evidence"
- Sub-text: "Coachee submit evidence per deliverable" → "Coach submit evidence per deliverable"
- Icon: 🔼 (atau keep ✍️)

**Footer:** Hapus "Role: HC kelola silabus + guidance. Coachee submit. Reviewer (Coach/SrSpv/SH/HC) independent per-role (Phase 65 architecture)." — keep "✅ Output: sertifikat tahun..."

### Slide 16 — Alur Coaching Th 3 (step 4 + 5 + footer)

**Step 4** "Review Mendalam":
- Title: "Review Mendalam" → "Review Multi-Role"
- Sub-text: "HC review lebih ketat" → "Coach + SrSpv + SH + HC review **paralel** per-role"
- Color: keep red theme

**Step 5** "Coaching Intensif":
- Title: "Coaching Intensif" → "Approval / Revisi"
- Sub-text: "Sesi coaching mendalam per deliverable, dicatat di CoachingSession log" → "Approve atau request revisi dgn komentar"
- Icon: 🔃 atau ✅

**Footer:** keep existing gradient banner (Output text), tidak ada chip Role.

### Slide 17 / 18 — No change

## File Yang Disentuh

- `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — 13 region edit.

## Verification (Test Plan)

1. **Spot grep** verify:
   - `Assessment OJT` count = 0 (semua sudah Umum)
   - `Penilaian 5 aspek` count = 0
   - `Mapping Track` count = 0
   - `Coaching Intensif` count = 0
   - `Review Mendalam` count = 0
   - `Competency Management Platform` count ≥ 2 (slide 2 + slide 3 CMP)
   - `Competency Development Platform` count ≥ 2 (slide 2 + slide 3 CDP)
   - `Perpustakaan` count ≥ 1 (slide 12 IDP)
2. **Playwright** screenshot slide 2/3/5/7/8/10/12/14/15/16. Verifikasi content match design.
3. **Headless Chrome --print-to-pdf** generate v2.2.4 PDF, verify 18 page A4 landscape, content sesuai.
4. **Tag** `sosialisasi-v2.4` setelah QA pass.

## Out of Scope

- Slide 1/4/13/17/18 (Cover, Role Piramida, Hierarki Kompetensi, Timeline, Closing) — no content change requested.
- Restructure flow grid (sudah fix di v2.2 + v2.2.3, layout match HTML/PDF).
- Tagging milestone level — pakai patch tag v2.2.4 atau bump minor v2.4 (final decision di plan).
