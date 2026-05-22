# Sosialisasi PROTON Deck — Design Spec

**Tanggal:** 2026-05-22
**Topic:** Restructure HTML slide deck dari "Sosialisasi Umum HC Portal KPB" jadi deck fokus PROTON
**Source file:** `docs/Sosialisasi-Umum-PortalHC-KPB.html` (2307 baris, 19 slide)
**Target file:** `docs/Sosialisasi-PROTON-KPB.html` (estimasi ~2500 baris, 22 slide)

---

## 1. Konteks & Goal

Acara sosialisasi sekarang fokus pada program PROTON (branding baru, dulu CPDP). Deck existing terlalu generik — overview HC Portal mendominasi, PROTON cuma ~21% (4 dari 19 slide). Audience event: **Section Head + Sr. Supervisor + Coach + Coachee** (4-tier, mix manajemen lini + eksekutor).

**Goal:**
- Rename file → drop label "Umum", branding PROTON eksplisit
- Restructure: PROTON jadi pilar utama (≥60% slide), overview tetap di awal (konteks payung 6 slide)
- Tambah slide REFERENSI DOKUMEN (KKJ, Silabus, Coaching Guidance) — pakai layout split kiri foto · kanan teks (user isi screenshot sendiri)
- Tambah slide ALUR EKSEKUSI dengan screenshot portal real (CoachingProton, HistoriProton, dll)
- Durasi target: 60 menit / 22 slide ≈ 2.7 menit per slide

**Non-goal:**
- Tidak ubah codebase ASP.NET (read-only inventory saja untuk dasar konten slide)
- Tidak buat PDF export — boleh export browser saja
- Tidak ada Q&A block di slide penutup

---

## 2. File & Branding

- **Rename:** `git mv docs/Sosialisasi-Umum-PortalHC-KPB.html docs/Sosialisasi-PROTON-KPB.html` (preserve history)
- **`<title>`:** `Sosialisasi PROTON · Portal HC KPB`
- **Cover title:** `PROTON` (XL)
- **Cover subtitle:** `Human Capital Portal KPB`
- **Cover meta eyebrow:** `Sosialisasi Program · Section Head · Sr. Supervisor · Coach · Coachee`
- **Cover date:** `Balikpapan, [TBD]` — placeholder, user isi sebelum acara
- **Slide badge format:** `SLIDE X / 22`
- **Bahasa:** Indonesia, semi-formal, POV manajemen-aware + eksekutor
- **Drop semua referensi "CPDP" / "Umum"** (grep cek setelah edit)

---

## 3. Struktur Deck — 22 Slide Final

### Prolog (1-6) — overview konteks, Template B

| # | Judul | Treatment |
|---|---|---|
| 1 | Cover PROTON | Template A — full-bleed |
| 2 | Agenda Presentasi | Template B — rewrite isi sesuai deck baru |
| 3 | Latar Belakang | Template B — keep-ringkas, callout tujuan |
| 4 | Apa Itu HC Portal | Template B — keep-ringkas, payung |
| 5 | 3 Platform — CDP Highlight | Template B — keep-ringkas, CDP card lebih besar |
| 6 | Struktur Role — 4 Audience Highlight | Template B — retone, 4 role audience highlight |

### PROTON Fondasi (7-11) — konsep, Template B

| # | Judul | Treatment |
|---|---|---|
| 7 | Sistem Assessment — PROTON Pilar | Template B — FLIP dari ex-#7, PROTON 75% bobot |
| 8 | Role di PROTON | Template B — slide BARU, 4 kolom card per audience |
| 9 | Assessment PROTON Overview | Template B — keep-expand ex-#10, 3-tahun timeline mini |
| 10 | Timeline 3 Tahun PROTON | Template B — slide BARU, kalender 36 bulan |
| 11 | Progresi Kompetensi per Tahun | Template B — keep-expand ex-#13, tabel 5 aspek × 3 tahun |

### Referensi Dokumen (12-14) — Template C demo (kiri foto · kanan teks)

| # | Judul | Source Screenshot |
|---|---|---|
| 12 | KKJ — Kebutuhan Kompetensi Jabatan | `docs/doc support Proton/SS KPB_KKJ Fungsi System Completion & Simops.pdf` |
| 13 | Silabus PROTON | `docs/doc support Proton/silabus/Panelman_Kompetensi 1-5.docx` + `Operator_Kompetensi 1-5.docx` |
| 14 | Coaching Guidance | `docs/doc support Proton/coaching guidance/CoachingGuidance_RFCC NHT Dimensi 1-5 (Operator+Panelman).docx` |

### Alur Eksekusi di Portal (15-19) — Template C demo (kiri screenshot portal · kanan teks)

| # | Judul | Source Screenshot (file portal) |
|---|---|---|
| 15 | Alur PROTON T1&2 — Ujian Online | `Views/CMP/Assessment.cshtml` atau Views/CDP/PlanIdp.cshtml |
| 16 | Alur PROTON T3 — Interview Mahir | `Views/CDP/EditCoachingSession.cshtml` |
| 17 | Alur Coaching 9-step T1&2 | `Views/CDP/CoachingProton.cshtml` (1771 baris, form Result+Kesimpulan+AcuanPedoman) |
| 18 | Alur Coaching Mahir T3 | `Views/CDP/Shared/_CoachingProtonContentPartial.cshtml` |
| 19 | Dashboard Tracking PROTON | `Views/CDP/HistoriProton.cshtml` + `Dashboard.cshtml` |

### Pendukung & Penutup (20-22)

| # | Judul | Treatment |
|---|---|---|
| 20 | IDP & Training Records | Template B — appendix ringkas (ex-#16, 1 slide) |
| 21 | Cara Mengakses HC Portal | Template B — URL Dev, login, browser rekomendasi |
| 22 | Terima Kasih | Template D — tanpa Q&A block |

### Slide DI-DROP dari deck lama

- Ex-#8 "5 Kategori Assessment Umum" — bukan PROTON
- Ex-#9 "Alur Assessment 7-step End-to-End Umum" — diganti alur PROTON spesifik
- Ex-#17 "Integrasi & Keamanan" — drop total (user request)

### Slide BARU

- #8 Role di PROTON
- #10 Timeline 3 Tahun PROTON
- #12-14 Referensi Dokumen (KKJ, Silabus, Coaching Guidance)
- #19 Dashboard Tracking PROTON

---

## 4. Layout Templates

### Template A — Cover (slide #1)
Full-bleed gradient navy. Title `PROTON` XL, subtitle `Human Capital Portal KPB`, divider, meta block.

### Template B — Konvensional (slide #2-11, #20-21)
Header (title+subtitle+badge) + body full-width. Reuse CSS existing (`.slide-header`, `.slide-body`, `.slide-title`, `.slide-subtitle`, `.slide-badge`, `.accent`, `.section-eyebrow`).

### Template C — Demo Split (slide #12-19) — **BARU**
2-kolom: kiri 55% screenshot frame + caption, kanan 45% lead + bullet + takeaway.

**Aturan:**
- Placeholder default: bg `#f1f5f9`, border dashed `#94a3b8`, ratio `16:10`, label `📷 Screenshot akan ditambah`
- Caption italic kecil di bawah frame (sumber file)
- Kanan: 1 lead italic + 3-5 bullet (▸ marker) + 1 takeaway box (amber gradient highlight)
- Saat user paste screenshot: ganti `<div class="demo-image-frame">…</div>` jadi `<div class="demo-image-frame"><img src="..."></div>`

### Template D — Penutup (slide #22)
Full-bleed centered. Title `🙏 Terima Kasih`, subtitle tagline, tanpa Q&A block.

---

## 5. CSS Tambahan

```css
/* TEMPLATE C — Demo split layout */
.slide-body.demo-split {
  display: grid;
  grid-template-columns: 55% 45%;
  gap: 32px;
  align-items: start;
}
.demo-image-frame {
  border: 2px dashed #94a3b8;
  border-radius: 12px;
  background: #f1f5f9;
  aspect-ratio: 16 / 10;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  color: #64748b;
  font-size: 0.9rem;
  overflow: hidden;
}
.demo-image-frame img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  border-radius: 10px;
}
.demo-image-caption {
  margin-top: 10px;
  font-size: 0.78rem;
  color: #64748b;
  text-align: center;
  font-style: italic;
}
.demo-text-col { padding: 8px 0; }
.demo-lead {
  font-style: italic;
  color: #475569;
  margin-bottom: 14px;
  font-size: 0.95rem;
}
.demo-bullets {
  list-style: none;
  padding: 0;
  margin: 0 0 18px 0;
}
.demo-bullets li {
  padding: 6px 0 6px 22px;
  position: relative;
  font-size: 0.95rem;
  line-height: 1.5;
}
.demo-bullets li::before {
  content: "▸";
  position: absolute;
  left: 0;
  color: var(--navy);
  font-weight: 700;
}
.demo-takeaway {
  background: linear-gradient(90deg, #fef3c7, #fde68a);
  border-left: 4px solid #f59e0b;
  padding: 12px 14px;
  border-radius: 6px;
  font-weight: 600;
  font-size: 0.92rem;
}
body.dark .demo-image-frame { background: #1e293b; border-color: #475569; color: #94a3b8; }
body.dark .demo-text-col { color: #e2e8f0; }
body.dark .demo-lead { color: #cbd5e1; }
body.dark .demo-takeaway { background: rgba(245, 158, 11, 0.15); color: #fef3c7; }
```

---

## 6. HTML Pattern Template C

```html
<div class="slide default-deco" data-slide="12">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">REFERENSI · DOKUMEN DASAR</p>
      <h1 class="slide-title">KKJ — <span class="accent">Kebutuhan Kompetensi</span> Jabatan</h1>
      <p class="slide-subtitle">Peta kompetensi target per jabatan — dasar acuan PROTON</p>
    </div>
    <div class="slide-badge">SLIDE 12 / 22</div>
  </div>
  <div class="slide-body demo-split">
    <div>
      <div class="demo-image-frame">
        📷 Screenshot akan ditambah<br>
        <small>SS KPB_KKJ Fungsi System Completion & Simops</small>
      </div>
      <div class="demo-image-caption">Sumber: docs/doc support Proton/SS KPB_KKJ Fungsi System Completion & Simops.pdf</div>
    </div>
    <div class="demo-text-col">
      <p class="demo-lead">KKJ = peta kompetensi per jabatan</p>
      <ul class="demo-bullets">
        <li>Sumber dasar acuan PROTON</li>
        <li>Per fungsi (System Completion, Simops, dll)</li>
        <li>Mapping ke level kompetensi target</li>
        <li>Dasar penyusunan silabus</li>
      </ul>
      <div class="demo-takeaway">Tanpa KKJ, PROTON tidak punya target</div>
    </div>
  </div>
</div>
```

---

## 7. Konten Talking Points per Slide

### #1 Cover PROTON
- Title `PROTON`, subtitle `Human Capital Portal · Kilang Pertamina Balikpapan`
- Meta: `Sosialisasi Program · Section Head · Sr. Supervisor · Coach · Coachee`, `Balikpapan, [TBD]`

### #2 Agenda
- 4 chip clickable: PROLOG · FONDASI PROTON · REFERENSI · ALUR EKSEKUSI · PENDUKUNG
- Sub-list per chip ke slide target

### #3 Latar Belakang
- Kompetensi pekerja KPB dulu manual & tersebar
- PROTON: jalur pengembangan terstruktur 3 tahun
- HC Portal: alat digital pengelola PROTON
- Callout: "Tujuan — Panelman/Operator KPB kompeten & tersertifikasi"

### #4 Apa Itu HC Portal
- 1 kalimat definisi (sistem informasi pengembangan kompetensi pekerja KPB)
- 3 mini-card: CMP · CDP · BP
- Highlight CDP = rumah PROTON
- Treatment: **keep-ringkas** — buang detail yang tumpang tindih sama slide #5 (3 Platform). Slide #4 fokus payung definisi, slide #5 fokus breakdown 3 platform

### #5 3 Platform — CDP Highlight
- CMP (assessment generic) · **CDP (PROTON + Coaching + IDP, card besar)** · BP (Business Process)

### #6 Struktur Role — 4 Audience Highlight
- **POV: role akses di HC Portal** (siapa punya akses fitur apa)
- 6 level role visual (L1 Admin → L6 Coachee)
- Highlight 4 audience: SectionHead L4 · Sr. Supervisor L4 · Coach L5 · Coachee L6
- Role lain (Admin, HC, Manager, VP, Direktur) ditampilkan mini
- **Pembeda dari slide #8:** slide ini = peta role HC Portal generik. Slide #8 = role spesifik di alur kerja PROTON

### #7 Sistem Assessment — PROTON Pilar
- Diagram bobot: PROTON 75% (besar) · Pre/Post 25% (kecil)
- 1 kalimat: "PROTON adalah program assessment & coaching utama di KPB"

### #8 Role di PROTON (BARU)
- **POV: tanggung jawab di alur kerja PROTON** (siapa eksekusi step apa)
- 4 kolom card:
  - **Coachee** — ikut ujian, kerjakan deliverable, upload evidence
  - **Coach** — silabus, coaching, validasi evidence, isi catatan sesi
  - **Sr. Supervisor** — approve deliverable (`SrSpvApprovalStatus`), mapping Coach-Coachee
  - **Section Head** — overview section, approve final, eskalasi
- Footer: HC review final + Admin manage data
- **Pembeda dari slide #6:** slide #6 = role HC Portal (akses fitur). Slide #8 = role di workflow PROTON (eksekusi step). Boleh ada role yg sama disebut 2x dengan POV beda

### #9 Assessment PROTON Overview
- 3 tahun timeline mini: T1 (ujian online) · T2 (ujian online) · T3 (interview mahir)
- Output per tahun (sertif / level)

### #10 Timeline 3 Tahun PROTON (BARU)
- Kalender besar 36 bulan horizontal
- Milestone per tahun: silabus → coaching → assessment → sertifikasi

### #11 Progresi Kompetensi per Tahun
- Tabel 5 aspek × 3 tahun (Pengetahuan · Keterampilan · Aplikasi · Kompleksitas · Otonomi)
- Visual bar/level progression

### #12 KKJ (Template C)
- Lead: "KKJ = peta kompetensi per jabatan"
- Bullet: sumber dasar acuan PROTON · per fungsi · mapping level · dasar silabus
- Takeaway: "Tanpa KKJ, PROTON tidak punya target"

### #13 Silabus PROTON (Template C)
- Lead: "Silabus = penjabaran KKJ jadi deliverable"
- Bullet: 2 track (Panelman/Operator) · 5 dimensi Kompetensi per track · hierarki Kompetensi→SubKompetensi→Deliverable · diinput Admin/HC via Silabus Manager
- Takeaway: "Silabus = peta kerja Coach + Coachee"

### #14 Coaching Guidance (Template C)
- Lead: "Pedoman materi coaching per dimensi"
- Bullet: 5 Dimensi × Operator/Panelman = 10 dokumen · rujukan Coach saat sesi · diakses via portal (`CoachingGuidanceFile`) · update ikut revisi KKJ
- Takeaway: "Coach tidak coaching tanpa pedoman"

### #15 Alur PROTON T1&2 — Ujian Online (Template C)
- Lead: "Ujian online pilihan ganda per Kompetensi"
- Bullet: login portal → pilih ujian aktif → kerjakan soal → submit → hasil otomatis tampil → sertif Kompetensi
- Takeaway: "Lulus T1 & T2 → syarat naik ke T3 (Mahir)"
- **Screenshot source TBD:** PROTON T1&2 ujian online di portal kemungkinan pakai `Views/CMP/Assessment.cshtml` (generic test engine) dengan filter ujian PROTON aktif. Alternatif: `Views/CDP/PlanIdp.cshtml` jika ujian launcher embed di sana. User capture screenshot sesuai flow real saat acara — pilih view yg paling representatif

### #16 Alur PROTON T3 — Interview Mahir (Template C)
- Lead: "Penilaian Mahir via wawancara panel"
- Bullet: panel juri (Section Head + HC + ahli) · format tanya-jawab · kriteria mahir berdasar KKJ · output sertifikasi final
- Takeaway: "T3 = pintu sertifikasi Mahir"

### #17 Alur Coaching 9-step T1&2 (Template C)
- Lead: "Coaching = pendampingan Coachee oleh Coach"
- Bullet: silabus → assign → coaching → evidence → review SrSpv → review HC → approve → next deliverable → sertif
- Takeaway: "Coaching = jantung PROTON, bukan formalitas"

### #18 Alur Coaching Mahir T3 (Template C)
- Lead: "Coaching level Mahir dengan kriteria lebih tegas"
- Bullet: silabus Mahir → assign → coaching intensif → evidence → review SrSpv → review HC → final assessment → interview panel → sertif Mahir
- Takeaway: "Coach Mahir = kunci kualitas sertifikasi"

### #19 Dashboard Tracking PROTON (Template C)
- Lead: "Tool monitoring untuk Section Head & Sr. Supervisor"
- Bullet: filter section/track/year · status progress per coachee · audit trail (`DeliverableStatusHistory`) · export Excel/PDF
- Takeaway: "Dashboard = visibility manajemen lini"

### #20 IDP & Training Records
- Ringkas 1 slide: rencana karir + riwayat training tersimpan
- Sambung ke PROTON: deliverable PROTON masuk training record otomatis

### #21 Cara Mengakses HC Portal
- URL Dev: `http://10.55.3.3/KPB-PortalHC` (disclaimer fase Dev)
- Login: SSO Pertamina / akun yang disiapkan HC
- Browser rekomendasi: Edge / Chrome
- Kontak support: HC KPB

### #22 Terima Kasih (Template D)
- 🙏 Terima Kasih
- Tagline: "Mari kembangkan kompetensi pekerja KPB lewat PROTON"
- **Tanpa Q&A block**

---

## 8. JavaScript Update

- Konstanta `TOTAL_SLIDES` 19 → 22
- Renumber semua `data-slide` 1..22 (sequential)
- Update teks slide-badge: `SLIDE X / 22`
- Update agenda chip → goto target IDs sesuai urutan baru
- Cek listener navigation (← → arrow keys, scroll) tetap berfungsi
- Cek dark mode toggle persist

---

## 9. Branch + Commit Strategy

**Branch:** `feat/sosialisasi-proton-deck` (off main)

**Atomic commit per langkah** (25 commit):
1. `chore(sosialisasi-proton): git mv + rename title + cover retone`
2. `feat(sosialisasi-proton): add Template C demo-split CSS`
3. `refactor(sosialisasi-proton): drop slide #8 #9 #17 — non-PROTON`
4. `chore(sosialisasi-proton): renumber data-slide + JS TOTAL_SLIDES 22 + agenda goto IDs`
5. `refactor(sosialisasi-proton): retone slide #3 latar belakang`
6. `refactor(sosialisasi-proton): ringkas slide #4 apa itu HC Portal`
7. `refactor(sosialisasi-proton): retone slide #5 platform — CDP highlight`
8. `refactor(sosialisasi-proton): retone slide #6 role — 4 audience highlight`
9. `refactor(sosialisasi-proton): flip slide #7 assessment — PROTON pilar`
10. `feat(sosialisasi-proton): new slide #8 role di PROTON`
11. `refactor(sosialisasi-proton): expand slide #9 assessment overview`
12. `feat(sosialisasi-proton): new slide #10 timeline 3 tahun`
13. `refactor(sosialisasi-proton): expand slide #11 progresi kompetensi`
14. `feat(sosialisasi-proton): new slide #12 KKJ — Template C demo`
15. `feat(sosialisasi-proton): new slide #13 Silabus — Template C demo`
16. `feat(sosialisasi-proton): new slide #14 Coaching Guidance — Template C demo`
17. `refactor(sosialisasi-proton): slide #15 alur T1&2 → Template C demo`
18. `refactor(sosialisasi-proton): slide #16 alur T3 → Template C demo`
19. `refactor(sosialisasi-proton): slide #17 coaching 9-step → Template C demo`
20. `refactor(sosialisasi-proton): slide #18 coaching mahir → Template C demo`
21. `feat(sosialisasi-proton): new slide #19 dashboard tracking — Template C demo`
22. `refactor(sosialisasi-proton): slide #20 IDP appendix ringkas`
23. `refactor(sosialisasi-proton): slide #21 cara akses`
24. `refactor(sosialisasi-proton): slide #22 penutup tanpa Q&A`
25. `chore(sosialisasi-proton): rewrite agenda #2 + audit final`

---

## 10. Testing Strategy

**Otomatis (developer):**
- Browser open file lokal `docs/Sosialisasi-PROTON-KPB.html` di Edge/Chrome
- Cek navigasi: panah ←/→, agenda goto chip, ESC keyboard handler
- Cek dark mode toggle: semua Template C demo slide render benar di light + dark
- Cek aspect ratio screenshot frame `16:10` di viewport berbeda (1280×800, 1920×1080)
- Validasi: `<div class="slide" data-slide="N">` ada untuk N=1..22, tidak ada duplikat / skip
- Validasi: badge teks `SLIDE X / 22` semua konsisten
- Validasi: tidak ada string `"CPDP"` atau `"Umum"` tersisa (grep)

**Manual (user verify post-implementation):**
- Paste screenshot real ke 8 slide demo (#12-19)
- Isi tanggal acara (cover meta)
- Visual final per slide (typo, terminology)
- Print/export PDF browser → cek tidak ada slide terpotong

**Out of scope:**
- Tidak test backend (ASP.NET tidak diubah)
- Tidak test responsive mobile (deck dipresentasikan via laptop/proyektor)
- Tidak test PDF export tools eksternal

---

## 11. Risk & Mitigation

| Risk | Mitigation |
|---|---|
| File 2307 baris, edit banyak — risiko regression | Atomic commit per slide, browser test setiap commit |
| Renumber data-slide bisa pecahin agenda goto | Update agenda chip target ID di commit terpisah (commit #24 audit) |
| CSS Template C bentrok existing | Class namespace `.demo-*` prefix, isolasi pakai `.slide-body.demo-split` |
| Dark mode break di Template C | Eksplisit `body.dark .demo-*` selector, manual toggle test |
| Sumber screenshot file belum siap saat presentasi | Placeholder visible jelas (`📷 Screenshot akan ditambah`), aspect ratio jaga layout |

---

## 12. Out of Scope

- Bikin PDF export tool (browser print cukup)
- Edit codebase ASP.NET / Views / Controllers (read-only inventory saja)
- Sosialisasi alternatif version (Internal Tim HC, Aplikasi, dll — file terpisah)
- Translation Bahasa Inggris
- Animasi transisi slide custom (pakai existing keyboard navigation)
- A11y audit (focus order, screen reader) — bukan goal sosialisasi internal
