# Sosialisasi PROTON Operasional Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `docs/Sosialisasi-PROTON-Operasional.html` — 20-slide standalone HTML deck for PROTON sosialisasi audiens Section Head → Coachee, mengikuti design system Sosialisasi-Internal-Tim-HC dan reuse struktur slide PROTON existing.

**Architecture:** Single-file HTML (CSS + JS inline), `.deck > .slide.active` pattern dengan fixed 1280×720 internal design + auto-scale ke viewport. Scaffold dicopy dari `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (teal palette), slide existing distripping, lalu 20 slide baru dibangun incremental. Slide KKJ/Silabus/Coaching Guidance/Alur/9-step reuse struktur HTML dari `docs/Sosialisasi-PROTON-KPB.html` dengan adaptasi teks supaya tone non-teknis dan re-skin warna teal.

**Tech Stack:** HTML5, CSS3 (inline, CSS vars), Vanilla JS (slide nav + dark mode toggle), tidak ada dependensi eksternal.

**Spec reference:** `docs/superpowers/specs/2026-05-23-sosialisasi-proton-operasional-design.md`

---

## File Structure

**Create:**
- `docs/Sosialisasi-PROTON-Operasional.html` — single-file deck, target ~1500-2000 baris

**Read (reference, tidak dimodifikasi):**
- `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` — scaffold source (CSS, JS, layout, dark mode)
- `docs/Sosialisasi-PROTON-KPB.html` — slide content source (KKJ, Silabus, Coaching Guidance, Alur, 9-step)
- `docs/Naskah Video PROTON.docx` — narasi alignment (kalau perlu konsultasi terminologi)
- `docs/superpowers/specs/2026-05-23-sosialisasi-proton-operasional-design.md` — spec authoritative

**Glossary resmi (wajib pakai persis):**
- PROTON = **Professional Refinery Operations Competency Development**
- CMP = Competency Management Platform
- CDP = Competency Development Platform
- BP = Business Partner (Coming Soon / For Future)
- KKJ = Kebutuhan Kompetensi Jabatan
- IDP = Individual Development Plan

---

## Task 1: Scaffold deck dari Sosialisasi-Internal-Tim-HC

**Files:**
- Create: `docs/Sosialisasi-PROTON-Operasional.html`
- Read: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: Copy scaffold file**

```bash
cp "docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html" "docs/Sosialisasi-PROTON-Operasional.html"
```

- [ ] **Step 2: Ganti `<title>` di head**

Edit `docs/Sosialisasi-PROTON-Operasional.html` baris title:

```html
<title>Sosialisasi PROTON Operasional &mdash; Portal HC KPB</title>
```

- [ ] **Step 3: Tambah inline SVG favicon** (silence 404)

Tepat di bawah `<meta name="viewport">` di `<head>`:

```html
<link rel="icon" href="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'%3E%3Ctext y='80' font-size='80'%3E⚙%3C/text%3E%3C/svg%3E">
```

- [ ] **Step 4: Strip semua `<section class="slide">` existing di dalam `<div class="deck">`**

Cari div `<div class="deck">...</div>`. Hapus seluruh isi slide existing — sisakan empty `<div class="deck" id="deck"></div>` (jangan hapus div container, controls, scripts).

- [ ] **Step 5: Verify scaffold loadable di browser**

Open file in browser:
```bash
start "docs/Sosialisasi-PROTON-Operasional.html"
```
Expected: page load tanpa slide (kosong) + controls fixed bottom visible + tidak ada console error. Dark mode toggle masih functional.

- [ ] **Step 6: Update slide indicator JS supaya tahan slide 0**

Cek JS section bagian inisialisasi (cari `currentSlide` atau `slides`). Pastikan jika `slides.length === 0`, controls tidak crash (mungkin disable next/prev). Kalau scaffold sudah handle ini, skip step.

- [ ] **Step 7: Commit scaffold**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): scaffold from Internal-Tim-HC"
```

---

## Task 2: Slide 1 — Cover

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html` (insert ke `<div class="deck">`)

- [ ] **Step 1: Tambah slide cover ke deck**

```html
<section class="slide active default-deco" data-slide="1">
  <div class="slide-body" style="justify-content: center; align-items: center; text-align: center;">
    <div class="section-eyebrow">PORTAL HC KPB &middot; 2026</div>
    <h1 style="font-size: 56pt; font-weight: 900; color: var(--teal-dark); line-height: 1.05; margin: 16px 0;">
      Sosialisasi <span class="accent" style="color: var(--amber-dark);">PROTON</span>
    </h1>
    <p style="font-size: 16pt; color: var(--slate); font-weight: 600; max-width: 800px;">
      Professional Refinery Operations Competency Development
    </p>
    <p style="font-size: 12pt; color: var(--text-muted); margin-top: 24px; max-width: 700px;">
      Untuk <strong>Section Head &middot; Sr Supervisor &middot; Coach &middot; Coachee</strong>
    </p>
    <div class="slide-badge" style="margin-top: 36px;">Versi 1.0 &middot; Mei 2026</div>
  </div>
</section>
```

- [ ] **Step 2: Verify slide 1 render**

Refresh browser. Cover slide tampil center, teal "Sosialisasi PROTON" + amber accent, audience tag visible. Dark mode toggle ubah background tanpa break.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 1 cover"
```

---

## Task 3: Slide 2-3 — Apa itu Portal HC + Tujuan/Manfaat (Part 1)

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`

- [ ] **Step 1: Tambah slide 2 — Apa itu Portal HC KPB**

Setelah slide cover, append:

```html
<section class="slide default-deco" data-slide="2">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 1 &middot; APA ITU WEB HC</div>
      <h2 class="slide-title">Apa itu <span class="accent">Portal HC KPB</span>?</h2>
    </div>
    <div class="slide-badge">2 / 20</div>
  </div>
  <div class="slide-body">
    <p style="font-size: 18pt; line-height: 1.5; color: var(--text); max-width: 1100px;">
      Portal HC KPB adalah <strong>satu pintu digital Tim Human Capital</strong> Kilang Pertamina Balikpapan untuk semua pekerja — dari operator lapangan hingga manajemen.
    </p>
    <ul style="font-size: 14pt; line-height: 1.8; color: var(--text-muted); margin-top: 16px; list-style: none;">
      <li>🌐 <strong>Berbasis web</strong> — diakses dari komputer kantor maupun perangkat pribadi</li>
      <li>👥 <strong>Untuk semua pekerja</strong> — Admin, HC, Manager, Section Head, Supervisor, Coach, Coachee</li>
      <li>🔗 <strong>Terhubung</strong> dengan data kepegawaian, kompetensi, dan pengembangan karir</li>
    </ul>
  </div>
</section>
```

- [ ] **Step 2: Tambah slide 3 — Tujuan & manfaat Portal HC**

```html
<section class="slide default-deco" data-slide="3">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 1 &middot; APA ITU WEB HC</div>
      <h2 class="slide-title">Kenapa Portal HC <span class="accent">dibangun</span>?</h2>
    </div>
    <div class="slide-badge">3 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; margin-top: 24px;">
      <div style="background: var(--slide-bg-2); border-left: 5px solid var(--teal); padding: 20px; border-radius: 10px;">
        <div style="font-size: 28pt;">🔍</div>
        <h3 style="font-size: 15pt; color: var(--teal-dark); margin-top: 8px;">Transparan</h3>
        <p style="font-size: 11pt; color: var(--text-muted); margin-top: 8px;">Semua pekerja bisa lihat status pengembangan kompetensinya sendiri secara real-time.</p>
      </div>
      <div style="background: var(--slide-bg-2); border-left: 5px solid var(--amber); padding: 20px; border-radius: 10px;">
        <div style="font-size: 28pt;">📋</div>
        <h3 style="font-size: 15pt; color: var(--amber-dark); margin-top: 8px;">Terstruktur</h3>
        <p style="font-size: 11pt; color: var(--text-muted); margin-top: 8px;">Proses coaching dan assessment terdokumentasi rapi, tidak lagi tercecer di kertas.</p>
      </div>
      <div style="background: var(--slide-bg-2); border-left: 5px solid var(--green); padding: 20px; border-radius: 10px;">
        <div style="font-size: 28pt;">🤝</div>
        <h3 style="font-size: 15pt; color: var(--green-dark); margin-top: 8px;">Terhubung</h3>
        <p style="font-size: 11pt; color: var(--text-muted); margin-top: 8px;">Coachee, Coach, Supervisor, dan HC saling terhubung dalam satu workflow.</p>
      </div>
    </div>
  </div>
</section>
```

- [ ] **Step 3: Verify slide 2 + 3 navigasi**

Refresh browser. Slide 2 tampil setelah klik next dari cover, slide 3 tampil setelah klik next lagi. Layout 3-card di slide 3 grid 3-kolom rapi. Dark mode tidak break.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 2-3 Part 1 Apa itu Portal HC"
```

---

## Task 4: Slide 4-7 — 3 pilar Portal + per pilar (Part 2)

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`

- [ ] **Step 1: Tambah slide 4 — 3 pilar overview**

```html
<section class="slide default-deco" data-slide="4">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &middot; FITUR PORTAL</div>
      <h2 class="slide-title">3 Pilar Portal HC: <span class="accent">CMP &middot; CDP &middot; BP</span></h2>
    </div>
    <div class="slide-badge">4 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; margin-top: 24px;">
      <div style="background: linear-gradient(135deg, var(--teal) 0%, var(--teal-dark) 100%); color: #fff; padding: 28px; border-radius: 14px; text-align: center;">
        <div style="font-size: 38pt; font-weight: 900; letter-spacing: 2px;">CMP</div>
        <p style="font-size: 11pt; opacity: 0.9; margin-top: 8px;">Competency Management Platform</p>
        <p style="font-size: 10pt; opacity: 0.85; margin-top: 14px;">Rumah Assessment Proton</p>
      </div>
      <div style="background: linear-gradient(135deg, var(--amber) 0%, var(--amber-dark) 100%); color: #fff; padding: 28px; border-radius: 14px; text-align: center;">
        <div style="font-size: 38pt; font-weight: 900; letter-spacing: 2px;">CDP</div>
        <p style="font-size: 11pt; opacity: 0.9; margin-top: 8px;">Competency Development Platform</p>
        <p style="font-size: 10pt; opacity: 0.85; margin-top: 14px;">Rumah Coaching Proton</p>
      </div>
      <div style="background: #e2e8f0; color: var(--slate-dark); padding: 28px; border-radius: 14px; text-align: center; border: 2px dashed var(--slate);">
        <div style="font-size: 38pt; font-weight: 900; letter-spacing: 2px;">BP</div>
        <p style="font-size: 11pt; margin-top: 8px;">Business Partner</p>
        <p style="font-size: 10pt; margin-top: 14px; font-style: italic;">🚧 Coming Soon</p>
      </div>
    </div>
    <p style="font-size: 12pt; color: var(--text-muted); margin-top: 20px; text-align: center; font-style: italic;">
      PROTON jalan di <strong>CMP</strong> dan <strong>CDP</strong>.
    </p>
  </div>
</section>
```

- [ ] **Step 2: Tambah slide 5 — CMP**

```html
<section class="slide default-deco" data-slide="5">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &middot; FITUR PORTAL</div>
      <h2 class="slide-title"><span class="accent">CMP</span> — Competency Management Platform</h2>
      <p class="slide-subtitle">Rumah <strong>Assessment Proton</strong></p>
    </div>
    <div class="slide-badge">5 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <p style="font-size: 15pt; color: var(--text); max-width: 1100px; line-height: 1.5;">
      CMP mengelola seluruh <strong>kegiatan asesmen kompetensi</strong> pekerja KPB — mulai dari soal, paket, jadwal, sampai sertifikat.
    </p>
    <ul style="font-size: 13pt; line-height: 1.9; color: var(--text-muted); margin-top: 16px; list-style: none;">
      <li>📝 <strong>Ujian online</strong> — pilihan ganda dan essay untuk PROTON Tahun 1 &amp; Tahun 2</li>
      <li>🎤 <strong>Interview offline</strong> — tatap muka panel juri untuk PROTON Tahun 3 (sertifikasi Mahir)</li>
      <li>📊 <strong>Records &amp; analytics</strong> — riwayat asesmen + visualisasi kompetensi tim</li>
      <li>📜 <strong>Sertifikat digital</strong> — bukti formal kompetensi (lengkap dengan masa berlaku &amp; renewal)</li>
    </ul>
  </div>
</section>
```

- [ ] **Step 3: Tambah slide 6 — CDP**

```html
<section class="slide default-deco" data-slide="6">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &middot; FITUR PORTAL</div>
      <h2 class="slide-title"><span class="accent">CDP</span> — Competency Development Platform</h2>
      <p class="slide-subtitle">Rumah <strong>Coaching Proton</strong></p>
    </div>
    <div class="slide-badge">6 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <p style="font-size: 15pt; color: var(--text); max-width: 1100px; line-height: 1.5;">
      CDP mengelola seluruh <strong>kegiatan pengembangan kompetensi</strong> — pendampingan, deliverable, dan rencana karir.
    </p>
    <ul style="font-size: 13pt; line-height: 1.9; color: var(--text-muted); margin-top: 16px; list-style: none;">
      <li>👥 <strong>Coaching Proton</strong> — Coach mencatat sesi pendampingan, Coachee upload bukti kerja</li>
      <li>📂 <strong>Deliverable tracking</strong> — daftar pekerjaan yang harus diselesaikan per kompetensi</li>
      <li>📈 <strong>Dashboard progres</strong> — Section Head &amp; Sr Supervisor pantau seluruh tim</li>
      <li>🎯 <strong>IDP</strong> — Individual Development Plan: riwayat &amp; rencana pelatihan pribadi</li>
    </ul>
  </div>
</section>
```

- [ ] **Step 4: Tambah slide 7 — BP**

```html
<section class="slide default-deco" data-slide="7">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &middot; FITUR PORTAL</div>
      <h2 class="slide-title"><span class="accent">BP</span> — Business Partner</h2>
      <p class="slide-subtitle">🚧 <em>Coming Soon / For Future</em></p>
    </div>
    <div class="slide-badge">7 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <div style="background: var(--slide-bg-2); padding: 28px; border-radius: 12px; border: 2px dashed var(--slate); max-width: 1100px;">
      <p style="font-size: 14pt; color: var(--text); line-height: 1.6;">
        BP akan menjadi <strong>jembatan strategis</strong> antara Tim HC dan unit operasional — profil talent, eligibilitas karir, dan rekomendasi pengembangan.
      </p>
      <p style="font-size: 12pt; color: var(--text-muted); margin-top: 16px; font-style: italic;">
        Fitur ini masih dalam pengembangan dan akan diluncurkan pada milestone berikutnya.
      </p>
    </div>
  </div>
</section>
```

- [ ] **Step 5: Verify slide 4-7 navigasi + layout**

Refresh browser. Slide 4 grid 3-card CMP/CDP/BP rapi (BP faded dashed). Slide 5/6/7 list bullet readable. Dark mode tidak break. Slide indicator bertambah ke 7/20 (kalau ada).

- [ ] **Step 6: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 4-7 Part 2 fitur portal CMP/CDP/BP"
```

---

## Task 5: Slide 8 — PROTON definisi + SMART

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`

- [ ] **Step 1: Tambah slide 8**

```html
<section class="slide default-deco" data-slide="8">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 3 &middot; APA ITU PROTON</div>
      <h2 class="slide-title">PROTON</h2>
      <p class="slide-subtitle"><strong>P</strong>rofessional <strong>R</strong>efinery <strong>O</strong>perations Competency Development</p>
    </div>
    <div class="slide-badge">8 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <p style="font-size: 15pt; color: var(--text); max-width: 1100px; line-height: 1.5;">
      PROTON adalah <strong>program digital pengembangan kompetensi</strong> pekerja operasional kilang, dirancang dengan <strong>prinsip SMART</strong> dan terdokumentasi di Portal HC KPB.
    </p>
    <div style="display: grid; grid-template-columns: repeat(5, 1fr); gap: 12px; margin-top: 24px;">
      <div style="background: var(--teal); color: #fff; padding: 14px; border-radius: 10px; text-align: center;">
        <div style="font-size: 22pt; font-weight: 900;">S</div>
        <div style="font-size: 10pt; font-weight: 700;">Specific</div>
        <div style="font-size: 8.5pt; opacity: 0.9; margin-top: 4px;">Target jelas</div>
      </div>
      <div style="background: var(--teal-dark); color: #fff; padding: 14px; border-radius: 10px; text-align: center;">
        <div style="font-size: 22pt; font-weight: 900;">M</div>
        <div style="font-size: 10pt; font-weight: 700;">Measurable</div>
        <div style="font-size: 8.5pt; opacity: 0.9; margin-top: 4px;">Terukur</div>
      </div>
      <div style="background: var(--amber); color: #fff; padding: 14px; border-radius: 10px; text-align: center;">
        <div style="font-size: 22pt; font-weight: 900;">A</div>
        <div style="font-size: 10pt; font-weight: 700;">Achievable</div>
        <div style="font-size: 8.5pt; opacity: 0.9; margin-top: 4px;">Bisa dicapai</div>
      </div>
      <div style="background: var(--amber-dark); color: #fff; padding: 14px; border-radius: 10px; text-align: center;">
        <div style="font-size: 22pt; font-weight: 900;">R</div>
        <div style="font-size: 10pt; font-weight: 700;">Relevant</div>
        <div style="font-size: 8.5pt; opacity: 0.9; margin-top: 4px;">Sesuai pekerjaan</div>
      </div>
      <div style="background: var(--green); color: #fff; padding: 14px; border-radius: 10px; text-align: center;">
        <div style="font-size: 22pt; font-weight: 900;">T</div>
        <div style="font-size: 10pt; font-weight: 700;">Time-Bound</div>
        <div style="font-size: 8.5pt; opacity: 0.9; margin-top: 4px;">Punya jadwal</div>
      </div>
    </div>
    <p style="font-size: 12pt; color: var(--text-muted); margin-top: 24px; text-align: center; font-style: italic;">
      Berjalan di <strong>2 modul</strong>: Coaching Proton (CDP) &amp; Assessment Proton (CMP).
    </p>
  </div>
</section>
```

- [ ] **Step 2: Verify slide 8**

Refresh browser, navigate ke slide 8. SMART 5-card grid rapi (teal → amber → green). Dark mode tidak break. Subtitle PROTON expansion terbaca jelas.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 8 PROTON definisi + SMART"
```

---

## Task 6: Slide 9 — 4 peran chain coaching

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`

- [ ] **Step 1: Tambah slide 9**

```html
<section class="slide default-deco" data-slide="9">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 3 &middot; APA ITU PROTON</div>
      <h2 class="slide-title">4 Peran dalam <span class="accent">Chain Coaching</span></h2>
      <p class="slide-subtitle">Siapa lakukan apa di PROTON</p>
    </div>
    <div class="slide-badge">9 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-top: 20px;">
      <div style="background: var(--slide-bg-2); border-top: 5px solid var(--green); padding: 18px; border-radius: 10px;">
        <div style="font-size: 26pt;">👤</div>
        <h3 style="font-size: 14pt; color: var(--green-dark); margin-top: 6px;">Coachee</h3>
        <p style="font-size: 10pt; color: var(--text-muted); margin-top: 8px; line-height: 1.5;">
          Pekerja yang <strong>dikembangkan</strong>. Menerima coaching, menyelesaikan deliverable, upload bukti kerja.
        </p>
      </div>
      <div style="background: var(--slide-bg-2); border-top: 5px solid var(--teal); padding: 18px; border-radius: 10px;">
        <div style="font-size: 26pt;">🧑‍🏫</div>
        <h3 style="font-size: 14pt; color: var(--teal-dark); margin-top: 6px;">Coach</h3>
        <p style="font-size: 10pt; color: var(--text-muted); margin-top: 8px; line-height: 1.5;">
          Pekerja senior yang <strong>mendampingi</strong>. Catat sesi coaching, beri arahan, approve hasil tahap awal.
        </p>
      </div>
      <div style="background: var(--slide-bg-2); border-top: 5px solid var(--amber); padding: 18px; border-radius: 10px;">
        <div style="font-size: 26pt;">👔</div>
        <h3 style="font-size: 14pt; color: var(--amber-dark); margin-top: 6px;">Sr Supervisor</h3>
        <p style="font-size: 10pt; color: var(--text-muted); margin-top: 8px; line-height: 1.5;">
          <strong>Verifikator</strong> unit. Approve / tolak deliverable, awasi beban kerja coach, lihat progres tim.
        </p>
      </div>
      <div style="background: var(--slide-bg-2); border-top: 5px solid var(--orange); padding: 18px; border-radius: 10px;">
        <div style="font-size: 26pt;">🎯</div>
        <h3 style="font-size: 14pt; color: var(--orange); margin-top: 6px;">Section Head</h3>
        <p style="font-size: 10pt; color: var(--text-muted); margin-top: 8px; line-height: 1.5;">
          <strong>Penanggung jawab strategis</strong>. Approve final, oversight section, dorong yang tertinggal.
        </p>
      </div>
    </div>
    <p style="font-size: 12pt; color: var(--text-muted); margin-top: 20px; text-align: center; font-style: italic;">
      Approval bergerak naik: Coachee submit → Coach review → Sr Supervisor approve → Section Head approve → HC review final.
    </p>
  </div>
</section>
```

- [ ] **Step 2: Verify slide 9**

Refresh + navigate ke slide 9. 4-card grid (green-teal-amber-orange) rapi. Dark mode tidak break.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 9 4 peran chain coaching"
```

---

## Task 7: Slide 10 — 2 track × 3 tahun matrix

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`

- [ ] **Step 1: Tambah slide 10**

```html
<section class="slide default-deco" data-slide="10">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 3 &middot; APA ITU PROTON</div>
      <h2 class="slide-title">2 Track &times; <span class="accent">3 Tahun</span></h2>
      <p class="slide-subtitle">Panelman &amp; Operator masing-masing jalani 3 tahap</p>
    </div>
    <div class="slide-badge">10 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <table style="width: 100%; border-collapse: separate; border-spacing: 8px; margin-top: 14px;">
      <thead>
        <tr>
          <th style="background: var(--slate-dark); color: #fff; padding: 12px; border-radius: 8px; font-size: 11pt; text-align: left; width: 14%;">Track</th>
          <th style="background: var(--teal); color: #fff; padding: 12px; border-radius: 8px; font-size: 11pt;">Tahun 1<br/><span style="font-size: 9pt; opacity: 0.85;">Foundation</span></th>
          <th style="background: var(--amber); color: #fff; padding: 12px; border-radius: 8px; font-size: 11pt;">Tahun 2<br/><span style="font-size: 9pt; opacity: 0.85;">Pendalaman</span></th>
          <th style="background: var(--orange); color: #fff; padding: 12px; border-radius: 8px; font-size: 11pt;">Tahun 3<br/><span style="font-size: 9pt; opacity: 0.85;">Mastery</span></th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td style="background: var(--slide-bg-2); padding: 16px; border-radius: 8px; font-weight: 700; color: var(--teal-dark); font-size: 13pt;">🎛️ Panelman</td>
          <td style="background: var(--slide-bg-2); padding: 16px; border-radius: 8px; font-size: 10.5pt; color: var(--text-muted); line-height: 1.5;">Dasar operasi kilang &amp; praktik kerja aman</td>
          <td style="background: var(--slide-bg-2); padding: 16px; border-radius: 8px; font-size: 10.5pt; color: var(--text-muted); line-height: 1.5;">Penguasaan sub-proses &amp; manajemen energi</td>
          <td style="background: var(--slide-bg-2); padding: 16px; border-radius: 8px; font-size: 10.5pt; color: var(--text-muted); line-height: 1.5;">Kontrol proses &amp; optimasi kilang (Mahir)</td>
        </tr>
        <tr>
          <td style="background: var(--slide-bg-2); padding: 16px; border-radius: 8px; font-weight: 700; color: var(--teal-dark); font-size: 13pt;">🔧 Operator</td>
          <td style="background: var(--slide-bg-2); padding: 16px; border-radius: 8px; font-size: 10.5pt; color: var(--text-muted); line-height: 1.5;">Pengenalan peralatan lapangan &amp; safety</td>
          <td style="background: var(--slide-bg-2); padding: 16px; border-radius: 8px; font-size: 10.5pt; color: var(--text-muted); line-height: 1.5;">Operasi lanjut &amp; troubleshooting rutin</td>
          <td style="background: var(--slide-bg-2); padding: 16px; border-radius: 8px; font-size: 10.5pt; color: var(--text-muted); line-height: 1.5;">Penanganan abnormal &amp; emergency response</td>
        </tr>
      </tbody>
    </table>
    <p style="font-size: 11pt; color: var(--text-muted); margin-top: 18px; text-align: center; font-style: italic;">
      Wajib selesai satu tahap dulu sebelum naik ke tahap berikutnya.
    </p>
  </div>
</section>
```

- [ ] **Step 2: Verify slide 10**

Refresh + navigate ke slide 10. Tabel matrix Panelman/Operator × T1/T2/T3 rapi, header gradient warna progresif. Dark mode tidak break.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 10 2 track x 3 tahun matrix"
```

---

## Task 8: Slide 11 — Komponen PROTON 4 pilar

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`

- [ ] **Step 1: Tambah slide 11**

```html
<section class="slide default-deco" data-slide="11">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 3 &middot; APA ITU PROTON</div>
      <h2 class="slide-title">Komponen PROTON — <span class="accent">4 Pilar</span></h2>
      <p class="slide-subtitle">Pondasi sampai evaluasi</p>
    </div>
    <div class="slide-badge">11 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; margin-top: 20px;">
      <div style="background: linear-gradient(135deg, var(--teal) 0%, var(--teal-dark) 100%); color: #fff; padding: 20px; border-radius: 12px;">
        <div style="font-size: 28pt;">🏗️</div>
        <h3 style="font-size: 14pt; margin-top: 8px;">KKJ</h3>
        <p style="font-size: 9.5pt; opacity: 0.9; margin-top: 6px; line-height: 1.5;">
          <strong>Kebutuhan Kompetensi Jabatan</strong> — pondasi referensi kompetensi tiap jabatan.
        </p>
      </div>
      <div style="background: linear-gradient(135deg, var(--amber) 0%, var(--amber-dark) 100%); color: #fff; padding: 20px; border-radius: 12px;">
        <div style="font-size: 28pt;">📚</div>
        <h3 style="font-size: 14pt; margin-top: 8px;">Silabus</h3>
        <p style="font-size: 9.5pt; opacity: 0.9; margin-top: 6px; line-height: 1.5;">
          Penjabaran KKJ → <strong>deliverable konkrit</strong> yang harus diselesaikan coachee.
        </p>
      </div>
      <div style="background: linear-gradient(135deg, var(--green) 0%, var(--green-dark) 100%); color: #fff; padding: 20px; border-radius: 12px;">
        <div style="font-size: 28pt;">🧭</div>
        <h3 style="font-size: 14pt; margin-top: 8px;">Coaching Guidance</h3>
        <p style="font-size: 9.5pt; opacity: 0.9; margin-top: 6px; line-height: 1.5;">
          <strong>Pedoman coach</strong> — materi standar untuk pendampingan tiap dimensi.
        </p>
      </div>
      <div style="background: linear-gradient(135deg, var(--orange) 0%, #c2410c 100%); color: #fff; padding: 20px; border-radius: 12px;">
        <div style="font-size: 28pt;">✅</div>
        <h3 style="font-size: 14pt; margin-top: 8px;">Assessment</h3>
        <p style="font-size: 9.5pt; opacity: 0.9; margin-top: 6px; line-height: 1.5;">
          <strong>Evaluasi formal</strong> — ujian online (T1/T2) &amp; interview (T3 Mahir).
        </p>
      </div>
    </div>
    <p style="font-size: 12pt; color: var(--text-muted); margin-top: 28px; text-align: center;">
      Tiga slide berikutnya zoom-in tiap pilar (kecuali Assessment, sudah dibahas di slide CMP).
    </p>
  </div>
</section>
```

- [ ] **Step 2: Verify slide 11**

Refresh + navigate ke slide 11. 4-card gradient progresif (teal → amber → green → orange) rapi. Dark mode tidak break.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 11 komponen 4 pilar"
```

---

## Task 9: Slide 12 — KKJ (reuse struktur existing PROTON)

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`
- Read: `docs/Sosialisasi-PROTON-KPB.html` (cari section yang judul mengandung "KKJ" atau "Peta Kompetensi per Jabatan")

- [ ] **Step 1: Cari slide KKJ existing di Sosialisasi-PROTON-KPB.html**

Search file untuk pola `Peta Kompetensi per Jabatan` atau `KKJ`. Catat baris awal & akhir section `<section class="slide" ...>` yang berisi konten KKJ.

```bash
grep -n "KKJ\|Peta Kompetensi per Jabatan" "docs/Sosialisasi-PROTON-KPB.html" | head -20
```

- [ ] **Step 2: Copy struktur HTML slide KKJ + adapt warna teal**

Salin section `<section class="slide" ...>` yang berisi KKJ. Tempel sebagai slide 12 di file target. Adaptasi:
- Class tetap `slide default-deco`
- `data-slide="12"`
- Ganti color reference `var(--navy)` → `var(--teal-dark)`, `var(--red)` → `var(--amber-dark)` (CSS alias di scaffold sudah handle, jadi mungkin tidak perlu)
- Header tambah `<div class="section-eyebrow">PART 3 &middot; APA ITU PROTON</div>`
- Badge `12 / 20`
- Edit teks supaya tone non-teknis (kalau ada istilah seperti "ProtonKompetensi", "Bagian", "Unit" — translasi ke bahasa pekerja: "kompetensi", "bagian fungsi", "unit kerja")

Template structure:

```html
<section class="slide default-deco" data-slide="12">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 3 &middot; APA ITU PROTON</div>
      <h2 class="slide-title"><span class="accent">KKJ</span> — Kebutuhan Kompetensi Jabatan</h2>
      <p class="slide-subtitle">Pondasi referensi kompetensi tiap jabatan</p>
    </div>
    <div class="slide-badge">12 / 20</div>
  </div>
  <div class="slide-body">
    <!-- TEMPEL adaptasi konten KKJ dari Sosialisasi-PROTON-KPB.html di sini -->
  </div>
</section>
```

- [ ] **Step 3: Tempel CSS terkait kalau ada class slide-specific**

Cari CSS rules di `<style>` Sosialisasi-PROTON-KPB.html yang dipakai slide KKJ (cari prefix `.kkj-`, `.peta-`, dll). Copy ke `<style>` file target.

- [ ] **Step 4: Verify slide 12**

Refresh + navigate ke slide 12. Konten KKJ tampil, warna teal-amber konsisten dengan deck. Dark mode tidak break.

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 12 KKJ (reuse from existing PROTON deck)"
```

---

## Task 10: Slide 13 — Silabus PROTON (reuse struktur existing)

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`
- Read: `docs/Sosialisasi-PROTON-KPB.html`

- [ ] **Step 1: Cari slide Silabus existing**

```bash
grep -n "Silabus" "docs/Sosialisasi-PROTON-KPB.html" | head -20
```

- [ ] **Step 2: Copy struktur HTML slide Silabus + adapt warna teal**

Sama proses Task 9 (copy slide section, ganti eyebrow + badge, adaptasi tone non-teknis). Template:

```html
<section class="slide default-deco" data-slide="13">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 3 &middot; APA ITU PROTON</div>
      <h2 class="slide-title"><span class="accent">Silabus</span> PROTON</h2>
      <p class="slide-subtitle">Dari KKJ ke deliverable konkrit</p>
    </div>
    <div class="slide-badge">13 / 20</div>
  </div>
  <div class="slide-body">
    <!-- TEMPEL adaptasi konten Silabus -->
  </div>
</section>
```

- [ ] **Step 3: Copy CSS slide-specific kalau ada**

Cari class prefix `.silabus-`, `.deliverable-` di scratch source CSS.

- [ ] **Step 4: Verify slide 13**

Refresh + navigate. Tone non-teknis. Dark mode OK.

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 13 Silabus PROTON (reuse)"
```

---

## Task 11: Slide 14 — Coaching Guidance (reuse struktur existing)

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`
- Read: `docs/Sosialisasi-PROTON-KPB.html`

- [ ] **Step 1: Cari slide Coaching Guidance existing**

```bash
grep -n "Coaching Guidance" "docs/Sosialisasi-PROTON-KPB.html" | head -20
```

- [ ] **Step 2: Copy + adapt sebagai slide 14**

```html
<section class="slide default-deco" data-slide="14">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 3 &middot; APA ITU PROTON</div>
      <h2 class="slide-title"><span class="accent">Coaching Guidance</span></h2>
      <p class="slide-subtitle">Pedoman coach untuk setiap dimensi</p>
    </div>
    <div class="slide-badge">14 / 20</div>
  </div>
  <div class="slide-body">
    <!-- TEMPEL adaptasi -->
  </div>
</section>
```

- [ ] **Step 3: Copy CSS slide-specific kalau ada**

- [ ] **Step 4: Verify slide 14**

Refresh + navigate. Tone non-teknis. Dark mode OK.

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 14 Coaching Guidance (reuse)"
```

---

## Task 12: Slide 15 — Alur PROTON 6 langkah

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`

- [ ] **Step 1: Tambah slide 15 — stepper 6 langkah**

```html
<section class="slide default-deco" data-slide="15">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 3 &middot; APA ITU PROTON</div>
      <h2 class="slide-title">Alur PROTON <span class="accent">End-to-End</span></h2>
      <p class="slide-subtitle">6 langkah dari assign coach sampai sertifikasi</p>
    </div>
    <div class="slide-badge">15 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <div style="display: grid; grid-template-columns: repeat(6, 1fr); gap: 8px; margin-top: 20px;">
      <div style="background: var(--teal); color: #fff; padding: 14px 10px; border-radius: 10px; text-align: center; position: relative;">
        <div style="font-size: 18pt; font-weight: 900;">1</div>
        <div style="font-size: 9.5pt; font-weight: 700; margin-top: 4px;">HC Assign Coach</div>
        <div style="font-size: 8pt; opacity: 0.9; margin-top: 4px;">HC pasangkan coachee → coach</div>
      </div>
      <div style="background: var(--teal-dark); color: #fff; padding: 14px 10px; border-radius: 10px; text-align: center;">
        <div style="font-size: 18pt; font-weight: 900;">2</div>
        <div style="font-size: 9.5pt; font-weight: 700; margin-top: 4px;">Deliverable</div>
        <div style="font-size: 8pt; opacity: 0.9; margin-top: 4px;">Coachee kerja + upload bukti</div>
      </div>
      <div style="background: var(--amber); color: #fff; padding: 14px 10px; border-radius: 10px; text-align: center;">
        <div style="font-size: 18pt; font-weight: 900;">3</div>
        <div style="font-size: 9.5pt; font-weight: 700; margin-top: 4px;">Coaching Proton</div>
        <div style="font-size: 8pt; opacity: 0.9; margin-top: 4px;">Coach dampingi &amp; catat sesi</div>
      </div>
      <div style="background: var(--amber-dark); color: #fff; padding: 14px 10px; border-radius: 10px; text-align: center;">
        <div style="font-size: 18pt; font-weight: 900;">4</div>
        <div style="font-size: 9.5pt; font-weight: 700; margin-top: 4px;">Multi-Role Approval</div>
        <div style="font-size: 8pt; opacity: 0.9; margin-top: 4px;">SrSpv → SH → HC review</div>
      </div>
      <div style="background: var(--orange); color: #fff; padding: 14px 10px; border-radius: 10px; text-align: center;">
        <div style="font-size: 18pt; font-weight: 900;">5</div>
        <div style="font-size: 9.5pt; font-weight: 700; margin-top: 4px;">Assessment</div>
        <div style="font-size: 8pt; opacity: 0.9; margin-top: 4px;">Ujian (T1/T2) / Interview (T3)</div>
      </div>
      <div style="background: var(--green); color: #fff; padding: 14px 10px; border-radius: 10px; text-align: center;">
        <div style="font-size: 18pt; font-weight: 900;">6</div>
        <div style="font-size: 9.5pt; font-weight: 700; margin-top: 4px;">Sertifikasi</div>
        <div style="font-size: 8pt; opacity: 0.9; margin-top: 4px;">Mahir / Naik tahun</div>
      </div>
    </div>
    <p style="font-size: 12pt; color: var(--text-muted); margin-top: 24px; text-align: center; font-style: italic;">
      Loop ini berjalan tiap dimensi kompetensi sepanjang 3 tahun PROTON.
    </p>
  </div>
</section>
```

- [ ] **Step 2: Verify slide 15**

Refresh + navigate ke slide 15. Stepper 6-card horizontal rapi, warna progresif teal → amber → orange → green. Dark mode tidak break.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 15 alur 6 langkah end-to-end"
```

---

## Task 13: Slide 16 — Coaching 9-step (reuse struktur existing)

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`
- Read: `docs/Sosialisasi-PROTON-KPB.html`

- [ ] **Step 1: Cari slide 9-step existing**

```bash
grep -n "9.*[Ss]tep\|9 [Ll]angkah" "docs/Sosialisasi-PROTON-KPB.html" | head -20
```

- [ ] **Step 2: Copy + adapt sebagai slide 16**

```html
<section class="slide default-deco" data-slide="16">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 3 &middot; APA ITU PROTON</div>
      <h2 class="slide-title">Coaching <span class="accent">9-Step</span></h2>
      <p class="slide-subtitle">Inti praktik pendampingan di lapangan</p>
    </div>
    <div class="slide-badge">16 / 20</div>
  </div>
  <div class="slide-body">
    <!-- TEMPEL adaptasi 9-step dari Sosialisasi-PROTON-KPB.html, fokus inti tanpa jargon teknis -->
  </div>
</section>
```

- [ ] **Step 3: Copy CSS slide-specific (kalau ada `.coaching-step-`)**

- [ ] **Step 4: Verify slide 16**

Refresh + navigate. 9 langkah readable. Dark mode OK.

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 16 coaching 9-step (reuse)"
```

---

## Task 14: Slide 17-18 — Manfaat per role + Outcome (Part 4)

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`

- [ ] **Step 1: Tambah slide 17 — Manfaat per role**

```html
<section class="slide default-deco" data-slide="17">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 4 &middot; KENAPA UNTUK SAYA</div>
      <h2 class="slide-title">Manfaat PROTON <span class="accent">Per Role</span></h2>
    </div>
    <div class="slide-badge">17 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 18px; margin-top: 20px;">
      <div style="background: var(--slide-bg-2); padding: 22px; border-radius: 12px; border-left: 5px solid var(--green);">
        <h3 style="font-size: 16pt; color: var(--green-dark);">👤 Coachee</h3>
        <ul style="font-size: 11pt; color: var(--text-muted); margin-top: 12px; line-height: 1.8;">
          <li>Jalur karir <strong>terstruktur &amp; transparan</strong></li>
          <li>Bukti kompetensi <strong>terdokumentasi rapi</strong></li>
          <li>Sertifikasi Mahir → <strong>peluang promosi</strong></li>
        </ul>
      </div>
      <div style="background: var(--slide-bg-2); padding: 22px; border-radius: 12px; border-left: 5px solid var(--teal);">
        <h3 style="font-size: 16pt; color: var(--teal-dark);">🧑‍🏫 Coach</h3>
        <ul style="font-size: 11pt; color: var(--text-muted); margin-top: 12px; line-height: 1.8;">
          <li>Pengakuan formal sebagai <strong>mentor</strong></li>
          <li>Catatan coaching <strong>terhindar tercecer</strong></li>
          <li>Beban kerja terpantau, <strong>tidak overload</strong></li>
        </ul>
      </div>
      <div style="background: var(--slide-bg-2); padding: 22px; border-radius: 12px; border-left: 5px solid var(--amber);">
        <h3 style="font-size: 16pt; color: var(--amber-dark);">👔 Sr Supervisor</h3>
        <ul style="font-size: 11pt; color: var(--text-muted); margin-top: 12px; line-height: 1.8;">
          <li>Oversight unit <strong>real-time</strong></li>
          <li>Lihat bottleneck &amp; dorong yang macet</li>
          <li>Approval <strong>terdokumentasi</strong> + audit trail</li>
        </ul>
      </div>
      <div style="background: var(--slide-bg-2); padding: 22px; border-radius: 12px; border-left: 5px solid var(--orange);">
        <h3 style="font-size: 16pt; color: var(--orange);">🎯 Section Head</h3>
        <ul style="font-size: 11pt; color: var(--text-muted); margin-top: 12px; line-height: 1.8;">
          <li>Develop seluruh <strong>tim section</strong></li>
          <li>Data kompetensi untuk <strong>perencanaan SDM</strong></li>
          <li>Performance review <strong>berbasis data</strong></li>
        </ul>
      </div>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Tambah slide 18 — Outcome (Sertifikasi Mahir + IDP)**

```html
<section class="slide default-deco" data-slide="18">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 4 &middot; KENAPA UNTUK SAYA</div>
      <h2 class="slide-title">Outcome — <span class="accent">Sertifikasi Mahir</span> &amp; IDP</h2>
    </div>
    <div class="slide-badge">18 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-top: 20px;">
      <div style="background: linear-gradient(135deg, var(--teal) 0%, var(--teal-dark) 100%); color: #fff; padding: 26px; border-radius: 14px;">
        <div style="font-size: 36pt;">🎖️</div>
        <h3 style="font-size: 18pt; margin-top: 8px;">Sertifikasi Mahir</h3>
        <p style="font-size: 11.5pt; opacity: 0.92; margin-top: 12px; line-height: 1.6;">
          Diberikan setelah pekerja menyelesaikan <strong>Tahun 3 PROTON</strong> dengan lulus interview panel dan semua deliverable disetujui.
        </p>
        <p style="font-size: 10pt; opacity: 0.85; margin-top: 12px; font-style: italic;">
          Bukti formal kompetensi yang diakui di KPB.
        </p>
      </div>
      <div style="background: linear-gradient(135deg, var(--amber) 0%, var(--amber-dark) 100%); color: #fff; padding: 26px; border-radius: 14px;">
        <div style="font-size: 36pt;">📈</div>
        <h3 style="font-size: 18pt; margin-top: 8px;">IDP — Rencana Karir</h3>
        <p style="font-size: 11.5pt; opacity: 0.92; margin-top: 12px; line-height: 1.6;">
          <strong>Individual Development Plan</strong> — riwayat pelatihan, kompetensi, dan rencana pengembangan jangka panjang.
        </p>
        <p style="font-size: 10pt; opacity: 0.85; margin-top: 12px; font-style: italic;">
          Bisa diakses kapan saja oleh pekerja dan atasan.
        </p>
      </div>
    </div>
  </div>
</section>
```

- [ ] **Step 3: Verify slide 17-18**

Refresh + navigate. 17: 4-card 2×2 manfaat per role rapi. 18: 2-card gradient (teal + amber). Dark mode OK.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 17-18 manfaat per role + outcome"
```

---

## Task 15: Slide 19-20 — Akses Portal + Penutup (Part 4)

**Files:**
- Modify: `docs/Sosialisasi-PROTON-Operasional.html`

- [ ] **Step 1: Tambah slide 19 — Akses Portal**

```html
<section class="slide default-deco" data-slide="19">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 4 &middot; KENAPA UNTUK SAYA</div>
      <h2 class="slide-title">Akses Portal — <span class="accent">Cek Progres Saya</span></h2>
    </div>
    <div class="slide-badge">19 / 20</div>
  </div>
  <div class="slide-body" style="justify-content: flex-start;">
    <div style="background: var(--slide-bg-2); padding: 26px; border-radius: 12px; border-left: 5px solid var(--teal); margin-top: 20px;">
      <h3 style="font-size: 16pt; color: var(--teal-dark);">🔐 Cara Login</h3>
      <ol style="font-size: 12pt; color: var(--text); margin-top: 12px; line-height: 1.9; padding-left: 22px;">
        <li>Buka <code style="background: var(--bg); padding: 2px 8px; border-radius: 4px; color: var(--teal-dark); font-weight: 700;">http://10.55.3.3/KPB-PortalHC</code> (Development) di browser komputer kantor</li>
        <li>Login menggunakan <strong>akun email Pertamina</strong></li>
        <li>Portal otomatis mengenali peran Anda (Coachee / Coach / SrSpv / SH)</li>
      </ol>
    </div>
    <div style="background: var(--slide-bg-2); padding: 26px; border-radius: 12px; border-left: 5px solid var(--amber); margin-top: 16px;">
      <h3 style="font-size: 16pt; color: var(--amber-dark);">📍 Tempat Cek Progres PROTON</h3>
      <ul style="font-size: 12pt; color: var(--text); margin-top: 12px; line-height: 1.9; list-style: none;">
        <li>📂 Menu <strong>CDP</strong> → Dashboard Coaching Proton</li>
        <li>📜 Menu <strong>CDP</strong> → Histori PROTON (lihat seluruh perjalanan kompetensi)</li>
        <li>🎯 Menu <strong>CDP</strong> → IDP (rencana &amp; riwayat pelatihan)</li>
      </ul>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Tambah slide 20 — Penutup**

```html
<section class="slide default-deco" data-slide="20">
  <div class="slide-body" style="justify-content: center; align-items: center; text-align: center;">
    <div style="font-size: 60pt;">🚀</div>
    <h2 style="font-size: 42pt; color: var(--teal-dark); font-weight: 900; margin-top: 12px;">Terima Kasih</h2>
    <p style="font-size: 16pt; color: var(--text); margin-top: 16px; max-width: 800px; line-height: 1.5;">
      Bersama PROTON, kita bangun <strong>kompetensi operasional kilang yang terukur dan terstruktur</strong>.
    </p>
    <div style="background: var(--slide-bg-2); padding: 24px 36px; border-radius: 14px; margin-top: 32px; border-top: 4px solid var(--amber);">
      <p style="font-size: 11pt; color: var(--text-muted); letter-spacing: 1px; text-transform: uppercase; font-weight: 700;">Butuh Bantuan?</p>
      <p style="font-size: 13pt; color: var(--text); margin-top: 8px;">Hubungi <strong>Tim HC KPB</strong> atau atasan langsung Anda</p>
    </div>
    <p style="font-size: 10pt; color: var(--text-muted); margin-top: 28px; font-style: italic;">
      Sosialisasi PROTON Operasional &middot; Versi 1.0 &middot; Mei 2026
    </p>
  </div>
</section>
```

- [ ] **Step 3: Verify slide 19-20**

Refresh + navigate. 19: 2-block (login + tempat cek progres) rapi. 20: penutup center, tagline + kontak. Dark mode OK.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "feat(sosialisasi-proton-operasional): slide 19-20 akses portal + penutup"
```

---

## Task 16: Cross-cutting verification (semua slide)

**Files:**
- Modify (kalau perlu fix): `docs/Sosialisasi-PROTON-Operasional.html`

- [ ] **Step 1: Buka file di browser dan jalani semua 20 slide manual**

```bash
start "docs/Sosialisasi-PROTON-Operasional.html"
```

Checklist per slide:
- [ ] Slide 1 — cover render
- [ ] Slide 2 — apa itu portal
- [ ] Slide 3 — tujuan portal (3-card grid)
- [ ] Slide 4 — 3 pilar (CMP/CDP/BP, BP dashed)
- [ ] Slide 5 — CMP
- [ ] Slide 6 — CDP
- [ ] Slide 7 — BP coming soon
- [ ] Slide 8 — PROTON definisi + SMART (5 card)
- [ ] Slide 9 — 4 peran chain coaching (4 card)
- [ ] Slide 10 — 2 track × 3 tahun matrix
- [ ] Slide 11 — komponen 4 pilar (4 card gradient)
- [ ] Slide 12 — KKJ
- [ ] Slide 13 — Silabus
- [ ] Slide 14 — Coaching Guidance
- [ ] Slide 15 — alur 6 langkah stepper
- [ ] Slide 16 — 9-step coaching
- [ ] Slide 17 — manfaat per role (4 card 2×2)
- [ ] Slide 18 — outcome sertifikasi + IDP
- [ ] Slide 19 — akses portal
- [ ] Slide 20 — penutup

- [ ] **Step 2: Test dark mode toggle di semua slide**

Klik dark mode toggle di controls. Ulangi navigasi 1→20. Pastikan tidak ada slide yang text putih di background putih (atau invisible).

Common issue: inline `style="color: var(--text)"` di dark mode auto-adjust. Tapi kalau ada hardcoded `color: #1a1a1a` atau `color: white` di style inline, perlu fix manual ke `color: var(--text)` atau `color: var(--text-muted)`.

- [ ] **Step 3: Test responsive viewport ≤1366px**

Resize browser ke 1366px width. Cek slide auto-scale tidak terpotong / overflow. Kalau ada slide yang content keluar dari frame, fix dengan kurangi padding atau font-size pakai media query.

- [ ] **Step 4: Test keyboard navigation**

Tekan tombol panah kanan/kiri / spasi. Slide harus pindah. Tombol `f` (kalau ada handler fullscreen) harus toggle fullscreen.

- [ ] **Step 5: Cek console error**

DevTools → Console. Pastikan tidak ada error (favicon 404, JS error, broken reference image).

- [ ] **Step 6: Commit fix kalau ada**

```bash
git add docs/Sosialisasi-PROTON-Operasional.html
git commit -m "fix(sosialisasi-proton-operasional): verifikasi cross-cutting (dark mode + responsive)"
```

Kalau tidak ada fix, skip commit step ini.

---

## Task 17: Final tag + push

**Files:** N/A (git operation)

- [ ] **Step 1: Final git status**

```bash
git status
git log --oneline -25
```

Pastikan working tree clean dan ada ~16-17 commit untuk deck ini.

- [ ] **Step 2: Tag versi**

```bash
git tag -a sosialisasi-proton-operasional-v1.0 -m "Sosialisasi PROTON Operasional v1.0 — 20 slide deck untuk audiens SH→Coachee"
```

- [ ] **Step 3: Push (TANYA USER DULU sebelum push)**

⚠️ Pattern project: direct push ke `origin/main`. Konfirmasi user sebelum eksekusi.

Setelah user setuju:

```bash
git push origin main
git push origin sosialisasi-proton-operasional-v1.0
```

- [ ] **Step 4: Update memory + report**

Tulis memory baru `project_sosialisasi_proton_operasional_shipped.md`:

```markdown
---
name: Sosialisasi PROTON Operasional v1.0 SHIPPED
description: 20 slide HTML deck untuk audiens SH→Coachee, fokus materi PROTON overall non-teknis, design ikut Internal-Tim-HC (teal palette), reuse struktur slide KKJ/Silabus/Coaching Guidance/9-step dari existing PROTON deck
type: project
---

Sosialisasi-PROTON-Operasional.html SHIPPED — tag sosialisasi-proton-operasional-v1.0 di origin/main, 17 task commits, audience Section Head → Coachee, durasi sesi ≈45 menit. Pending user verify browser sebelum distribute ke unit operasional.
```

Append entry di `MEMORY.md` index.

---

## Self-Review (post-write)

**1. Spec coverage** — semua section spec ada tasknya:
- ✅ Filename (Task 1)
- ✅ Design system teal (Task 1)
- ✅ 20 slide 4 part (Task 2-15)
- ✅ Reuse strategy (Task 9-11, 13)
- ✅ Glossary resmi (Task 5 + tiap slide pakai singkatan exact)
- ✅ Tone non-teknis (eksplisit di Task 9-11, 13 step "adapt teks")
- ✅ Audience tag eksplisit 4 role (Task 2 slide cover)
- ✅ Verification (Task 16)
- ✅ Branch direct main + tag (Task 17)

**2. Placeholder scan** — sweep:
- Tidak ada "TBD", "TODO", "implement later"
- Step "TEMPEL adaptasi" di Task 9-11, 13 ada justifikasi (content dari source file external, tidak bisa di-template tanpa lihat source — perlu engineer baca dulu)
- Tiap step task ada code/command/expected output konkret

**3. Type consistency** — semua slide pakai naming class konsisten (`slide default-deco`, `slide-header`, `slide-body`, `slide-badge`, `section-eyebrow`, `slide-title`, `slide-subtitle`). CSS vars konsisten (`--teal`, `--teal-dark`, `--amber`, `--amber-dark`, `--green`, `--orange`, `--orange`, `--slate-dark`). `data-slide="N"` numbering matches badge `N / 20`.

**4. Granularity** — 17 tasks, tiap task 3-7 step, tiap step 2-15 menit. Total estimasi 3-5 jam implementation.
