# Sosialisasi-Internal-Tim-HC Audit Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply 21 audit findings (HIGH 5 + MED 8 + LOW 8) ke `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` — fix redundansi, salah urutan, dan minor inkonsistensi tanpa mengubah visual style atau konten faktual.

**Architecture:** Edit-only fix di 1 file HTML. Eksekusi 3 fase:
1. **Phase 1 (text-only):** Edit konten/label per slide tanpa ubah slide count. 13 task atomic commit.
2. **Phase 2 (structural):** Drop S8, pindah 3 slide (S38, S30, S24), reorder Admin S33-S36. 5 task. Setiap commit termasuk renumber data-slide + badge consistency.
3. **Phase 3 (verify):** Final renumber 41→40 + JS TOTAL update + browser smoke test.

**Tech Stack:** HTML statis, vanilla JS counter, CSS classes (.slide, .section-eyebrow, .slide-badge, .hc-callout, .tip-bar). Edit tool + Read for verification. Grep untuk count audit.

**Decisions locked (default user approve):**
- O1 → drop S8, sebar LDAP→S7 + Anti-Copy→S16
- O3 → pindah S30 ke akhir BAGIAN 1 (slot 17.5)
- O4 → pindah S38 ke akhir BAGIAN 0 (slot 10.5)
- M6 → disclaim (no new slide)
- G2 → keep "BAGIAN 0" (no renumber section)
- Scope → semua tier 1+2+3 (21 finding)

**Spec:** `docs/superpowers/specs/2026-05-22-sosialisasi-hc-audit-design.md`

---

## File Structure

**Modified:** `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (single file, 3713 baris pre-fix)

**No new files.** Tidak ada test file (HTML deck = visual artifact, verifikasi via grep + browser snapshot).

**Verification helpers (inline bash):**
- `grep -c 'data-slide=' file.html` → count slide attribute
- `grep -c 'slide-badge">SLIDE' file.html` → count badge
- `grep -o 'SLIDE [0-9]* / [0-9]*' file.html | sort -u` → unique badge patterns
- `grep -c 'class="section-eyebrow"' file.html` → count section labels

---

## Phase 1 — Text-Only Edits (no slide count change)

### Task 1: M4 — Tambah section-eyebrow di S3-S8 (6 slide)

**Why:** Slide 3-8 di BAGIAN 0 PENGENALAN tidak punya HTML element `<p class="section-eyebrow">BAGIAN 0 — PENGENALAN</p>`. Hanya S2, S9, S10 yang punya. BAGIAN 1-5 semua slide punya — inkonsisten.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (S3 line ~1843, S4 ~1890, S5 ~1929, S6 ~1994, S7 ~2062, S8 ~2101)

- [ ] **Step 1: Read S3 header block** (line 1840-1850)
- [ ] **Step 2: Add section-eyebrow above each h1.slide-title di S3-S8**

Pattern edit untuk S3 (replicate untuk S4-S8):

```html
<!-- BEFORE -->
      <div>
        <h1 class="slide-title">Latar <span class="accent">Belakang</span></h1>

<!-- AFTER -->
      <div>
        <p class="section-eyebrow">BAGIAN 0 &mdash; PENGENALAN</p>
        <h1 class="slide-title">Latar <span class="accent">Belakang</span></h1>
```

Repeat pattern untuk S4, S5, S6, S7, S8 (judul h1 berbeda tapi tambah baris section-eyebrow sama persis di posisi yang sama).

- [ ] **Step 3: Verify count via grep**

```bash
grep -c 'class="section-eyebrow"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```
Expected: 40 (sebelum: 34 — semua slide non-cover non-penutup, 41-1-1-6 = 33... actually verify pre-state first; ekspektasi setelah fix = 39 = semua slide kecuali cover + penutup)

Pre-check: hitung dulu pre-state baseline, lalu konfirmasi delta +6.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): M4 — tambah section-eyebrow BAGIAN 0 di S3-S8"
```

---

### Task 2: M1 — Rename S21 title "Coaching Proton — Reviewer Chain"

**Why:** S21 title "Proton — Reviewer Chain" ambigu (Proton bisa diartikan Assessment Proton). Reviewer Chain = mekanisme review Coaching.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` line ~2668

- [ ] **Step 1: Edit title**

```html
<!-- BEFORE -->
        <h1 class="slide-title">Proton &mdash; <span class="accent">Reviewer Chain</span></h1>

<!-- AFTER -->
        <h1 class="slide-title">Coaching Proton &mdash; <span class="accent">Reviewer Chain</span></h1>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): M1 — rename S21 title jadi 'Coaching Proton — Reviewer Chain'"
```

---

### Task 3: M2 — Rename S36 title "Override KKJ + Mapping Silabus"

**Why:** S36 title "Override Data Pekerja" misleading. Konten = fallback manual KKJ + silabus mapping saat sync gagal, BUKAN CRUD pekerja.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` line ~3404

- [ ] **Step 1: Edit title + subtitle**

```html
<!-- BEFORE -->
        <h1 class="slide-title">Override <span class="accent">Data Pekerja</span></h1>
        <p class="slide-subtitle">Fallback manual saat data Proton gagal sync &mdash; setiap override ter-log di Audit Log</p>

<!-- AFTER -->
        <h1 class="slide-title">Override <span class="accent">KKJ + Mapping Silabus</span></h1>
        <p class="slide-subtitle">Fallback manual saat data Proton gagal sync &mdash; setiap override ter-log di Audit Log</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): M2 — rename S36 title jadi 'Override KKJ + Mapping Silabus'"
```

---

### Task 4: M3 — Tambah disclaim 2-of-6 sub-modul di S11

**Why:** S11 CMP Overview list 6 sub-modul tapi deck hanya deep-dive 2 (Records + Analytics). Audience expect 6 tapi dapat 2.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` line ~2260

- [ ] **Step 1: Edit mockup-tip text di S11**

```html
<!-- BEFORE -->
          <div class="mockup-tip"><strong>&#128161; Inti HC:</strong> fokus harian di <strong>Records Team</strong> + <strong>Analytics Dashboard</strong>. Selain itu ad-hoc.</div>

<!-- AFTER -->
          <div class="mockup-tip"><strong>&#128161; Fokus HC harian:</strong> <strong>Records Team</strong> + <strong>Analytics Dashboard</strong> (2 dari 6 sub-modul). Sub-modul lain (KKJ, Assessment Saya, Sertifikasi, Budget Training) = ad-hoc &mdash; lihat <strong>Panduan Bab 2</strong>.</div>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): M3 — disclaim S11 'Fokus HC: 2 dari 6 sub-modul'"
```

---

### Task 5: M5 — Tambah tip-bar fungsi kuning di S37 + S38

**Why:** Batch fix `ef2a018f` (Penjelasan fungsi per slide menu) tambah tip-bar style `background:#fef3c7;border-left:3px solid #f59e0b` di 7 mockup-slide (S27, S28, S31, S32, S33, S34, S35). S37 + S38 juga mockup-slide tapi miss.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (S37 line ~3470 sebelum closing div + panduan-ref; S38 line ~3540 sebelum closing div + panduan-ref)

- [ ] **Step 1: Tambah tip-bar fungsi di S37 setelah slide-mockup-split closing**

Cari struktur:
```html
        </div>
      </div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &sect;5.14, 5.15, 5.16 &mdash; Audit / Maintenance / Impersonate</p>
  </div>

  <!-- ================= SLIDE 38: NOTIFIKASI & WORKFLOW ================= -->
```

Insert SEBELUM `<p class="panduan-ref">` di S37:
```html
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> Audit Log untuk investigasi aksi sistem (filter tanggal, export Excel, 4 action badge) + Maintenance Mode untuk freeze modul saat update. Impersonate Admin-only via request HC.</div>
```

- [ ] **Step 2: Tambah tip-bar fungsi di S38 setelah slide-mockup-split closing**

Insert SEBELUM `<p class="panduan-ref">` di S38:
```html
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> Bell icon navbar = entry point notifikasi terpusat. Klik bell &rarr; dropdown notif terbaru &rarr; klik item navigate ke halaman. Mark as Read / Mark all as read / Dismiss per item.</div>
```

- [ ] **Step 3: Verify count via grep**

```bash
grep -c 'tip-bar.*fef3c7' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```
Expected: 9 (sebelum 7, +2)

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): M5 — tambah tip-bar fungsi kuning di S37 + S38"
```

---

### Task 6: M6 — Drop "Bank Soal" dari S2 agenda + tambah CPDP cross-ref di S31

**Why:** Agenda S2 promise "Bank Soal" + S31 list "CPDP" menu, tapi tidak ada slide dedicated. User decide opsi (b) disclaim.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (S2 line ~1825, S31 line ~3247)

- [ ] **Step 1: Update S2 agenda item 5 (drop "Bank Soal")**

```html
<!-- BEFORE (line ~1822-1827) -->
        <div class="agenda-item">
          <div class="agenda-num">05</div>
          <div class="agenda-content">
            <h4>Admin Panel HC</h4>
            <p>Pekerja, Bank Soal, Mapping, Maintenance</p>
          </div>
        </div>

<!-- AFTER -->
        <div class="agenda-item">
          <div class="agenda-num">05</div>
          <div class="agenda-content">
            <h4>Admin Panel HC</h4>
            <p>Pekerja, Mapping, Silabus, Override, Maintenance</p>
          </div>
        </div>
```

- [ ] **Step 2: Tambah CPDP cross-ref di S31 mockup-tip (line ~3248)**

```html
<!-- BEFORE -->
          <div class="mockup-tip"><strong>&#128161; Renewal badge:</strong> angka di card Renewal = sertifikat yang perlu di-renew. Cek harian.</div>

<!-- AFTER -->
          <div class="mockup-tip"><strong>&#128161; Renewal badge:</strong> angka di card Renewal = sertifikat yang perlu di-renew. Cek harian. <strong>CPDP</strong> = menu sync data eksternal, lihat <strong>Panduan &sect;5.3</strong>.</div>
```

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): M6 — drop Bank Soal dari S2 agenda + CPDP cross-ref S31"
```

---

### Task 7: M7 — Tambah Renewal cross-ref di S31

**Why:** S31 Admin section C list "Renewal" actionable, tapi deep-dive ada di BAGIAN 3 CDP (S29). Audience cari di Admin → tidak ketemu.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (lanjutan S31 mockup-tip dari Task 6)

- [ ] **Step 1: Extend Renewal badge mockup-tip dengan cross-ref**

```html
<!-- BEFORE (hasil Task 6) -->
          <div class="mockup-tip"><strong>&#128161; Renewal badge:</strong> angka di card Renewal = sertifikat yang perlu di-renew. Cek harian. <strong>CPDP</strong> = menu sync data eksternal, lihat <strong>Panduan &sect;5.3</strong>.</div>

<!-- AFTER -->
          <div class="mockup-tip"><strong>&#128161; Renewal badge:</strong> angka di card Renewal = sertifikat yang perlu di-renew. Cek harian. Deep-dive lifecycle &rarr; <strong>BAGIAN 3 slide Renewal Certificate Lifecycle</strong>. <strong>CPDP</strong> = menu sync data eksternal, lihat <strong>Panduan &sect;5.3</strong>.</div>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): M7 — Renewal cross-ref BAGIAN 3 di S31 mockup-tip"
```

---

### Task 8: O5 — Rename label BAGIAN 3 jadi "PROTON LIFECYCLE & COACHING"

**Why:** 10 slide di BAGIAN 3 cover lifecycle bukan murni coaching (Dashboard, Histori, Renewal, IDP). Label sempit.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` — semua section-eyebrow di S21-S30 (10 slide)

- [ ] **Step 1: Find-replace all section-eyebrow label B3**

```html
<!-- BEFORE -->
        <p class="section-eyebrow">BAGIAN 3 &mdash; COACHING PROTON / CDP</p>

<!-- AFTER -->
        <p class="section-eyebrow">BAGIAN 3 &mdash; PROTON LIFECYCLE &amp; COACHING</p>
```

Gunakan Edit tool dengan `replace_all=true`.

- [ ] **Step 2: Verify all 10 changed**

```bash
grep -c 'PROTON LIFECYCLE' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```
Expected: 10

```bash
grep -c 'COACHING PROTON / CDP' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```
Expected: 0

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): O5 — rename label BAGIAN 3 jadi 'PROTON LIFECYCLE & COACHING'"
```

---

### Task 9: O6 — Update S2 agenda jadi 5 item match 5 BAGIAN

**Why:** Agenda 6 item ≠ 5 BAGIAN deck. "Kelola Data Proton" standalone tapi di deck masuk Admin Panel.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` line ~1792-1834 (agenda-grid)

- [ ] **Step 1: Restructure agenda 6→5 items**

```html
<!-- BEFORE (line ~1792-1834) -->
      <div class="agenda-grid">
        <div class="agenda-item">
          <div class="agenda-num">01</div>
          <div class="agenda-content">
            <h4>Pengenalan Role HC</h4>
            <p>Authority, scope, dan alur kerja harian Anda</p>
          </div>
        </div>
        <div class="agenda-item">
          <div class="agenda-num">02</div>
          <div class="agenda-content">
            <h4>CMP &mdash; Competency Mgmt</h4>
            <p>Records, Analytics, Pre/Post, Budget Training</p>
          </div>
        </div>
        <div class="agenda-item">
          <div class="agenda-num">03</div>
          <div class="agenda-content">
            <h4>CDP &mdash; Coaching Proton</h4>
            <p>Dashboard, Reviewer Chain, Histori, Renewal</p>
          </div>
        </div>
        <div class="agenda-item">
          <div class="agenda-num">04</div>
          <div class="agenda-content">
            <h4>Kelola Data Proton</h4>
            <p>Silabus, Guidance, Override data</p>
          </div>
        </div>
        <div class="agenda-item">
          <div class="agenda-num">05</div>
          <div class="agenda-content">
            <h4>Admin Panel HC</h4>
            <p>Pekerja, Mapping, Silabus, Override, Maintenance</p>
          </div>
        </div>
        <div class="agenda-item">
          <div class="agenda-num">06</div>
          <div class="agenda-content">
            <h4>Notifikasi &amp; Tugas Cepat</h4>
            <p>Bell icon, daily/weekly checklist, reference</p>
          </div>
        </div>
      </div>

<!-- AFTER -->
      <div class="agenda-grid">
        <div class="agenda-item">
          <div class="agenda-num">01</div>
          <div class="agenda-content">
            <h4>Pengenalan &amp; Role HC</h4>
            <p>Authority, scope, alur kerja harian, bell icon</p>
          </div>
        </div>
        <div class="agenda-item">
          <div class="agenda-num">02</div>
          <div class="agenda-content">
            <h4>CMP &mdash; Competency Mgmt</h4>
            <p>Records Team, Analytics, Sistem Assessment, Pre/Post, IDP</p>
          </div>
        </div>
        <div class="agenda-item">
          <div class="agenda-num">03</div>
          <div class="agenda-content">
            <h4>Assessment Proton</h4>
            <p>Format per tahun, alur Tahun 1-2 online, Tahun 3 offline</p>
          </div>
        </div>
        <div class="agenda-item">
          <div class="agenda-num">04</div>
          <div class="agenda-content">
            <h4>Coaching Proton + Lifecycle</h4>
            <p>Reviewer Chain, Dual Track, Alur 9 step, Dashboard, Histori, Renewal</p>
          </div>
        </div>
        <div class="agenda-item">
          <div class="agenda-num">05</div>
          <div class="agenda-content">
            <h4>Admin Panel HC</h4>
            <p>Pekerja, Mapping, Silabus, Override, Maintenance + Audit</p>
          </div>
        </div>
      </div>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): O6 — agenda S2 dari 6 item jadi 5 match BAGIAN deck"
```

---

### Task 10: G1 — Update S9 "Area Kerja HC" jadi 5 area

**Why:** S9 4 area (CMP/CDP/Kelola Data/Admin) missing Assessment Proton. Tidak match 5 BAGIAN deck. Audience bingung Proton subset CMP?

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` line ~2153-2174 (area-grid)

- [ ] **Step 1: Restructure area-grid 4→5 area**

```html
<!-- BEFORE (line ~2153-2174) -->
      <div class="area-grid" style="grid-template-columns:repeat(2,1fr);gap:18px;max-width:760px;margin:30px auto;">
        <div class="area-card">
          <div class="area-icon">&#128202;</div>
          <div class="area-name">CMP</div>
          <div class="area-desc">Monitor lintas section</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#127919;</div>
          <div class="area-name">CDP</div>
          <div class="area-desc">Final reviewer chain</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#128193;</div>
          <div class="area-name">Kelola Data</div>
          <div class="area-desc">Silabus &amp; Override</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#9881;</div>
          <div class="area-name">Admin Panel</div>
          <div class="area-desc">14 menu operasional</div>
        </div>
      </div>

<!-- AFTER -->
      <div class="area-grid" style="grid-template-columns:repeat(5,1fr);gap:12px;max-width:980px;margin:30px auto;">
        <div class="area-card">
          <div class="area-icon">&#128202;</div>
          <div class="area-name">CMP</div>
          <div class="area-desc">Monitor lintas section</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#127891;</div>
          <div class="area-name">Assessment Proton</div>
          <div class="area-desc">Program 3-tahun Panelman / Operator</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#127919;</div>
          <div class="area-name">Coaching Proton</div>
          <div class="area-desc">Final reviewer + lifecycle</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#128193;</div>
          <div class="area-name">Kelola Data</div>
          <div class="area-desc">Silabus &amp; Override</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#9881;</div>
          <div class="area-name">Admin Panel</div>
          <div class="area-desc">14 menu operasional</div>
        </div>
      </div>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): G1 — S9 Area Kerja HC dari 4 jadi 5 area (+Assessment Proton)"
```

---

### Task 11: R3 — Drop Training Records card dari S30

**Why:** "Training Records" sudah di S11 CMP Overview sub-modul. Duplikat di S30 module-card. Drop dari S30, jadikan single-card "IDP".

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` line ~3179-3210 (modules-grid)

- [ ] **Step 1: Drop Training Records card, restructure grid jadi single card centered**

```html
<!-- BEFORE (line ~3179-3210) -->
      <div class="modules-grid" style="grid-template-columns:1fr 1fr;gap:18px;">
        <div class="module-card cmp">
          <div class="module-head">
            <div class="module-icon">&#128203;</div>
            <div>
              <div class="module-name">IDP</div>
              <div class="module-subname">Individual Development Plan (Perpustakaan)</div>
            </div>
          </div>
          <ul class="module-bullets">
            <li>&#128194; Repository dokumen IDP per pekerja</li>
            <li>&#128196; Akses dokumen KKJ (Kebutuhan Kompetensi Jabatan)</li>
            <li>&#128065; Worker view &amp; download dokumen</li>
            <li>&#128269; Filter &amp; search per jabatan / unit</li>
          </ul>
        </div>
        <div class="module-card cdp">
          <div class="module-head">
            <div class="module-icon">&#128218;</div>
            <div>
              <div class="module-name">Training Records</div>
              <div class="module-subname">Riwayat Pelatihan</div>
            </div>
          </div>
          <ul class="module-bullets">
            <li>&#127979; Training internal &amp; eksternal</li>
            <li>&#127991; Kategori + sub-kategori</li>
            <li>&#128206; Sertifikat upload (PDF/image)</li>
            <li>&#9203; Validity period &amp; renewal</li>
          </ul>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:14px;">&#128161; IDP &amp; Training Records <strong>terintegrasi dengan profile pekerja</strong> &mdash; jadi referensi gap analysis &amp; promosi.</div>

<!-- AFTER -->
      <div class="modules-grid" style="grid-template-columns:1fr;gap:18px;max-width:520px;margin:0 auto;">
        <div class="module-card cmp">
          <div class="module-head">
            <div class="module-icon">&#128203;</div>
            <div>
              <div class="module-name">IDP</div>
              <div class="module-subname">Individual Development Plan (Perpustakaan)</div>
            </div>
          </div>
          <ul class="module-bullets">
            <li>&#128194; Repository dokumen IDP per pekerja</li>
            <li>&#128196; Akses dokumen KKJ (Kebutuhan Kompetensi Jabatan)</li>
            <li>&#128065; Worker view &amp; download dokumen</li>
            <li>&#128269; Filter &amp; search per jabatan / unit</li>
          </ul>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:14px;">&#128161; IDP <strong>terintegrasi dengan profile pekerja</strong> &mdash; referensi gap analysis &amp; promosi. <strong>Training Records</strong> sudah dibahas di CMP (slide Riwayat Pelatihan).</div>
```

- [ ] **Step 2: Update title S30 (drop "Training Records") + hc-callout text**

```html
<!-- BEFORE (line ~3173) -->
        <h1 class="slide-title">IDP &amp; <span class="accent">Training Records</span></h1>
        <p class="slide-subtitle">Dua komponen pelengkap Coaching Proton di CDP</p>

<!-- AFTER -->
        <h1 class="slide-title">IDP <span class="accent">Library</span></h1>
        <p class="slide-subtitle">Perpustakaan dokumen development plan + KKJ per pekerja</p>
```

```html
<!-- BEFORE (line ~3213-3216 hc-callout) -->
      <div class="hc-callout">
        <strong>Penjelasan</strong>
        <ul>
          <li>Dua modul pelengkap CDP non-coaching: IDP (perpustakaan dokumen development plan per pekerja) dan Training Records (riwayat pelatihan + sertifikat).</li>
        </ul>
      </div>

<!-- AFTER -->
      <div class="hc-callout">
        <strong>Penjelasan</strong>
        <ul>
          <li>IDP = perpustakaan dokumen development plan + KKJ per pekerja. Worker akses untuk lihat target kompetensi posisi.</li>
        </ul>
      </div>
```

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): R3 — drop Training Records dari S30 (sudah di S11 CMP)"
```

---

### Task 12: R4 — Simplify S19 Alur Proton T1-2 jadi callout-only

**Why:** S19 4-step stepper overlap dengan S16 7-step Alur Umum. Callout sendiri akui "Mirip Assessment Umum — beda kategori & paket soal". Replace stepper dengan callout konsolidated highlight perbedaan saja.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` line ~2586-2618 (slide-body S19)

- [ ] **Step 1: Replace alur-stepper block dengan condensed callout**

```html
<!-- BEFORE (line ~2586-2618) -->
    <div class="slide-body">
      <div class="alur-stepper">
        <div class="alur-step">
          <span class="as-num">1</span>
          <div class="as-title">Buat Assessment</div>
          <div class="as-desc">Kategori "Assessment Proton", pilih track (Operator/Panelman) &amp; tahun</div>
        </div>
        <div class="alur-step">
          <span class="as-num">2</span>
          <div class="as-title">Set Paket Soal</div>
          <div class="as-desc">Pilih paket sesuai track tahun, set durasi &amp; passing grade</div>
        </div>
        <div class="alur-step">
          <span class="as-num">3</span>
          <div class="as-title">Peserta Ujian Online</div>
          <div class="as-desc">Login portal, kerjakan soal dalam timer otomatis</div>
        </div>
        <div class="alur-step final">
          <span class="as-num">4</span>
          <div class="as-title">Penilaian Otomatis</div>
          <div class="as-desc">Skor otomatis, laporan lulus/tidak per peserta</div>
        </div>
      </div>
      <div class="alur-crossref">&#128161; <strong>Mirip Assessment Umum</strong> &mdash; beda di kategori &amp; paket soal per track</div>
      <div class="alur-warning">&#9888; <strong>Wajib lulus Tahun N</strong> untuk lanjut ke Tahun N+1</div>
      <div class="hc-callout">
        <strong>Penjelasan</strong>
        <ul>
          <li>Tahun 1 dan Tahun 2 Proton menggunakan ujian online (pilihan ganda) seperti assessment umum &mdash; perbedaannya hanya di kategori dan paket soal per track.</li>
        </ul>
      </div>
    </div>

<!-- AFTER -->
    <div class="slide-body">
      <div class="alur-crossref" style="font-size:13pt;padding:18px 24px;margin:20px auto;max-width:820px;">
        &#128221; <strong>Alur identik Assessment Umum (S16)</strong> &mdash; 7 step end-to-end (Persiapan &rarr; Pelaksanaan &rarr; Penilaian).
      </div>
      <div class="modules-grid" style="grid-template-columns:1fr 1fr;gap:18px;max-width:820px;margin:20px auto;">
        <div class="module-card cmp">
          <div class="module-head">
            <div class="module-icon">&#127919;</div>
            <div>
              <div class="module-name">Yang Beda dari Umum</div>
            </div>
          </div>
          <ul class="module-bullets">
            <li>Kategori: pilih <strong>"Assessment Proton"</strong> (bukan kategori unit kerja)</li>
            <li>Paket soal: <strong>per track tahun</strong> (Panelman/Operator + Tahun 1 / Tahun 2)</li>
            <li>Hasil: <strong>terhubung ke Coaching Proton</strong> &mdash; lulus tahun = eligible naik tahun</li>
          </ul>
        </div>
        <div class="module-card cdp">
          <div class="module-head">
            <div class="module-icon">&#9203;</div>
            <div>
              <div class="module-name">Aturan Sequential</div>
            </div>
          </div>
          <ul class="module-bullets">
            <li>&#9888; <strong>Wajib lulus Tahun N</strong> untuk lanjut Tahun N+1</li>
            <li>Tidak boleh skip tahun</li>
            <li>Tahun 3 = offline interview (lihat slide berikutnya)</li>
          </ul>
        </div>
      </div>
      <div class="hc-callout">
        <strong>Penjelasan</strong>
        <ul>
          <li>Alur teknis Tahun 1 &amp; 2 Proton identik Assessment Umum (S16) &mdash; perbedaan hanya kategori, paket soal per track tahun, dan integrasi ke Coaching Proton untuk progresi sequential.</li>
        </ul>
      </div>
    </div>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): R4 — simplify S19 Alur Proton T1-2, ref alur S16 + highlight beda"
```

---

### Task 13: R1 + R2 — Pre-cleanup before drop S8 (handled di Phase 2 Task 14)

**Note:** R1 (bell card duplikasi) + R2 (RBAC duplikasi) di S8 akan otomatis resolved saat O1 drop S8 di Task 14 Phase 2. Tidak perlu task terpisah.

**Skip task.** Lanjut Phase 2.

---

## Phase 2 — Structural Moves + Renumber

**Strategy:** Setiap structural task self-contained — pindah/drop blok HTML + renumber `data-slide` attribute + update `slide-badge` text. Verify count konsisten setelah setiap commit.

**Pre-Phase 2 baseline:**
```bash
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
# Expected: 41
grep -c 'slide-badge">SLIDE' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
# Expected: 38 (S1 cover + S20 amber + S26 amber + S41 penutup = no standard badge → 41-4 = 37? verifikasi pre-state)
```

---

### Task 14: O1 — Drop S8 Integrasi & Keamanan + sebar LDAP→S7 + Anti-Copy→S16

**Why:** S8 cat-grid 6 fitur premature di BAGIAN 0. 4 fitur sudah punya slide dedicated (Notif S38, Audit S37, RBAC S6, Import Excel S32/34/35). Hanya LDAP + Anti-Copy unik — sebar ke slot relevan.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (S7 line ~2078-2090, S8 full delete line ~2097-2140, S16 line ~2489 add note)

- [ ] **Step 1: Tambah LDAP info di S7 akses-card dev** (sudah ada "Login: Akun Active Directory" — tambah penjelasan SSO singkat)

```html
<!-- BEFORE (line ~2075-2078) -->
          <div class="akses-info">
            <strong>Login:</strong> Akun Active Directory Pertamina<br>
            <strong>Jaringan:</strong> Intranet Pertamina
          </div>

<!-- AFTER -->
          <div class="akses-info">
            <strong>Login:</strong> Akun Active Directory Pertamina (LDAP SSO &mdash; tanpa password baru)<br>
            <strong>Jaringan:</strong> Intranet Pertamina
          </div>
```

Apply same edit pada akses-card prod (line ~2087-2090).

- [ ] **Step 2: Tambah Anti-Copy note di S16 step 3 Peserta Ujian**

```html
<!-- BEFORE (line ~2453-2456) -->
            <div class="swim-step">
              <span class="step-num-mini">3</span>
              <span class="step-icon-mini">&#128187;</span>
              <div class="step-title">Peserta Ujian</div>
              <div class="step-desc">Login portal, random soal, timer otomatis</div>
            </div>

<!-- AFTER -->
            <div class="swim-step">
              <span class="step-num-mini">3</span>
              <span class="step-icon-mini">&#128187;</span>
              <div class="step-title">Peserta Ujian</div>
              <div class="step-desc">Login portal, random soal, timer otomatis, <strong>anti-copy</strong> aktif</div>
            </div>
```

- [ ] **Step 3: Delete full S8 block (line ~2097-2140)**

Hapus dari komentar `<!-- ================= SLIDE 8: INTEGRASI & KEAMANAN ================= -->` sampai closing `</div>` block S8 (sebelum komentar SLIDE 9).

- [ ] **Step 4: Renumber data-slide S9-S41 jadi S8-S40 (decrement 1)**

Gunakan Edit `replace_all=false` per slide manual, atau Edit `replace_all=true` dengan unique anchor per slide. Loop manual:
- `data-slide="9"` → `data-slide="8"`
- `data-slide="10"` → `data-slide="9"`
- ... etc untuk semua hingga `data-slide="41"` → `data-slide="40"`

**Hati-hati order:** harus dari atas ke bawah (low to high) supaya tidak conflict. Tapi `data-slide="10"` → `9` setelah `9` → `8` aman karena value beda.

Alternatif: gunakan bash sed (windows: gunakan PowerShell). Tapi prefer Edit tool karena reproducible.

Actually safer: dari high ke low jadi tidak ada conflict (`41` → `40` dulu, lalu `40` → `39`, ..., `9` → `8`).

- [ ] **Step 5: Update slide-badge text "/41" → "/40" semua**

```bash
# Pakai grep untuk lihat semua badge dulu
grep -n 'slide-badge">SLIDE' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```

Lalu Edit `replace_all=true` dengan unique context:
```html
<!-- Per slide, update "SLIDE N / 41" → "SLIDE (N-1) / 40" -->
<!-- Plus drop SLIDE 8 badge entirely karena slide hilang -->
```

Pattern global: `/ 41<` → `/ 40<` (gunakan unique context "/" + space).

- [ ] **Step 6: Update JS TOTAL = 41 → 40**

```javascript
// BEFORE (line ~3652)
    const TOTAL = 41;
// AFTER
    const TOTAL = 40;
```

- [ ] **Step 7: Verify count**

```bash
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
# Expected: 40
grep -o 'SLIDE [0-9]* / [0-9]*' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | sort -u | head
# Expected: all "/ 40"
grep -n 'const TOTAL' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
# Expected: TOTAL = 40
```

- [ ] **Step 8: Browser smoke test**

Open `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` di browser, navigate slide 1 → 40 dengan arrow key. Verify:
- Counter "1 / 40" di bottom
- Setiap slide muncul
- Tidak ada blank slide
- S7 (Cara Mengakses) tampil LDAP SSO note
- S15 (Alur Assessment, was S16) tampil "anti-copy" note di step 3

- [ ] **Step 9: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): O1 — drop S8 Integrasi&Keamanan, sebar LDAP→S7 + Anti-Copy→S16"
```

---

### Task 15: O4 — Pindah S38 (sekarang S37 setelah Task 14) Notifikasi ke akhir BAGIAN 0 (slot 10.5)

**Why:** S38 (post-T14 = S37) feature explanation, bukan closing. Pindah jadi slide setelah S10 Alur Kerja Harian (bell = entry point harian).

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (cut S37 block dari current position, paste setelah S10)

- [ ] **Step 1: Read full block S37 Notifikasi & Workflow (yang dulunya S38)**

Find komentar `<!-- ================= SLIDE 38: NOTIFIKASI & WORKFLOW ================= -->` (post-rename mungkin masih `38` di komentar — ignore atau update). Read entire block dari komentar sampai closing `</div>` block sebelum komentar slide berikutnya.

- [ ] **Step 2: Cut block dari posisi current (post-T14 = S37)**

- [ ] **Step 3: Paste block setelah closing div S10 (sebelum komentar SLIDE 11)**

Update komentar block: `<!-- ================= SLIDE 11: NOTIFIKASI & WORKFLOW (was S38) ================= -->` (atau renumber komentar sekalian — keep simple, biarkan komentar, fokus data-slide).

- [ ] **Step 4: Renumber data-slide affected:**

Setelah pindah, urutan baru:
- S1-S10: tetap
- S11 baru: Notifikasi & Workflow (was S37 post-T14)
- S12 baru: CMP Overview (was S11 pre-T15)
- ... shift +1 untuk S11-S36 pre-T15
- S37 = Maintenance + Audit (was S37 post-T14, shift -0 karena Notifikasi moved INTO ke atas dan moved OUT dari posisi original)

Wait — Notifikasi was S37 post-T14, di-cut + paste ke posisi setelah S10. Maka:
- Posisi 1-10: unchanged
- Posisi 11: Notifikasi (was 37)
- Posisi 12-37: old S11-S36 shift +1 (jadi posisi 12-37)
- Posisi 38-40: old S38, S39, S40 (Tugas HC Cepat, Reference, Penutup) shift -1 jadi 37, 38, 39

Hmm S40 jadi posisi 39 saja. Wait, S40 = penutup. Let me recount:

Pre-T15 (post-T14): 40 slide. Cut slide-37 (Notif) out, paste between slide-10 and slide-11.

After T15:
- Posisi 1-10: same data-slide 1-10
- Posisi 11: Notifikasi (was data-slide 37)
- Posisi 12-37: was data-slide 11-36, shift to 12-37
- Posisi 38: was data-slide 38 (Tugas HC Cepat) — shift to 38? No.

Wait. Slot count tetap 40. Cut from middle, paste earlier = positions shift.

Actually:
- Slot 1-10: unchanged (10 slot)
- Slot 11: Notifikasi inserted here
- Slot 12-37: old slots 11-36 shifted +1 (26 slot)
- Slot 38: old slot 38 stayed (Tugas HC Cepat) — wait no, Notifikasi cut from slot 37, so slot 38 onwards shift -1 to fill gap

Better way: think of array operation.
- Array index 0-39 = data-slide 1-40
- pop index 36 (data-slide 37 Notif)
- insert at index 10 (= position 11)

Result array:
- index 0-9 (slot 1-10): unchanged
- index 10 (slot 11): Notif
- index 11-36 (slot 12-37): old index 10-35 (data-slide 11-36)
- index 37-39 (slot 38-40): old index 37-39 (data-slide 38, 39, 40 = Tugas HC, Reference, Penutup)

Renumber semua `data-slide` attribute jadi 1..40 matching new position.

- [ ] **Step 5: Update slide-badge text** untuk semua slide affected — "SLIDE 11" jadi Notifikasi, etc.

- [ ] **Step 6: Verify**

```bash
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
# Expected: 40
```

- [ ] **Step 7: Browser smoke test** — slide 11 = Notifikasi & Workflow. Counter "11 / 40".

- [ ] **Step 8: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): O4 — pindah Notifikasi&Workflow ke slot 11 (akhir BAGIAN 0)"
```

---

### Task 16: O3 — Pindah S30 (post-T15 mungkin S31) IDP Library ke akhir BAGIAN 1 (slot 18 baru)

**Why:** S30 IDP+Training Records (post-T11 = IDP only) break Coaching narrative di BAGIAN 3. Pindah ke akhir BAGIAN 1 CMP (setelah S17 Pre-Post).

**Important:** Post-T15, slide indices sudah shift. Identify slide by **title** ("IDP Library"), bukan data-slide number.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (cut IDP Library block, paste setelah Pre-Post Test block)

- [ ] **Step 1: Identify current data-slide IDP Library**

```bash
grep -n 'IDP <span class="accent">Library' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```

- [ ] **Step 2: Identify current data-slide Pre-Post Test**

```bash
grep -n 'Pre / Post Test' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```

- [ ] **Step 3: Read both blocks, cut IDP Library, paste setelah Pre-Post Test closing div**

- [ ] **Step 4: Update section-eyebrow IDP Library dari "BAGIAN 3 — PROTON LIFECYCLE & COACHING" jadi "BAGIAN 1 — CMP"**

```html
<!-- BEFORE -->
        <p class="section-eyebrow">BAGIAN 3 &mdash; PROTON LIFECYCLE &amp; COACHING</p>
        <h1 class="slide-title">IDP <span class="accent">Library</span></h1>

<!-- AFTER -->
        <p class="section-eyebrow">BAGIAN 1 &mdash; CMP</p>
        <h1 class="slide-title">IDP <span class="accent">Library</span></h1>
```

- [ ] **Step 5: Renumber data-slide + badge sequential 1..40**

- [ ] **Step 6: Verify count + browser smoke test**

- [ ] **Step 7: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): O3 — pindah IDP Library ke akhir BAGIAN 1 CMP"
```

---

### Task 17: O2 — Pindah Progresi Kompetensi ke awal BAGIAN 3 (slot setelah Alur Proton T3)

**Why:** Progresi Kompetensi (was S24) di tengah Coaching mechanics. S18 (Assessment Proton) forward-ref ke Progresi. Jadikan jembatan BAGIAN 2 → 3.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: Identify current data-slide Progresi Kompetensi**

```bash
grep -n 'Progresi <span class="accent">Kompetensi' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```

- [ ] **Step 2: Identify current data-slide Alur Proton Tahun 3**

```bash
grep -n 'Alur Proton.*Tahun.*3' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```

- [ ] **Step 3: Cut Progresi Kompetensi block, paste setelah Alur Proton T3 closing div**

- [ ] **Step 4: Update section-eyebrow Progresi tetap "BAGIAN 3 — PROTON LIFECYCLE & COACHING"** (slot pertama BAGIAN 3 baru). Atau pindahkan ke BAGIAN 2? Per spec, posisi jembatan — keep BAGIAN 3 sebagai opener.

Decision: keep section-eyebrow "BAGIAN 3 — PROTON LIFECYCLE & COACHING" — Progresi jadi opener BAGIAN 3.

- [ ] **Step 5: Update S18 callout — remove forward-ref (sekarang backward-ref dari Reviewer Chain)**

```html
<!-- BEFORE (di S18 — post-T16 mungkin shift, identify via grep) -->
        <div class="tip-bar" style="display:inline-block;text-align:left;max-width:640px;">
          &#128161; Detail komparasi 5 aspek per tahun (fokus, deliverable, coaching, assessment, akhir tahun) di <strong>slide Progresi Kompetensi</strong> (BAGIAN 3).
        </div>

<!-- AFTER -->
        <div class="tip-bar" style="display:inline-block;text-align:left;max-width:640px;">
          &#128161; Detail komparasi 5 aspek per tahun (fokus, deliverable, coaching, assessment, akhir tahun) di <strong>slide berikutnya: Progresi Kompetensi</strong>.
        </div>
```

- [ ] **Step 6: Renumber data-slide + badge sequential 1..40**

- [ ] **Step 7: Verify count + browser smoke test**

- [ ] **Step 8: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): O2 — pindah Progresi Kompetensi ke awal BAGIAN 3 (jembatan B2→B3)"
```

---

### Task 18: O7 — Reorder Admin slides match ABCD landing

**Why:** S31 declare struktur A→B→C→D, tapi slide sequence A→C→B→B→B→D+C. Reorder S33-S36 jadi: ManageWorkers (A) → Coach-Coachee Mapping (B) → Silabus+Guidance (B) → Override (B) → Assessment Monitoring (C) → Maintenance+Audit (C+D).

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: Identify current data-slide untuk 6 Admin slides**

```bash
grep -n 'class="section-eyebrow">BAGIAN 4' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
# Expected: 7 hits (Admin Landing + 6 sub-slide)
```

```bash
grep -n 'class="slide-title">[A-Z]' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | grep -A0 -B0 'BAGIAN 4'
# Alternative: list titles
```

- [ ] **Step 2: Read 4 Admin slide blocks yang perlu di-reorder**

Yang perlu move: Coach-Coachee Mapping + Silabus+Guidance + Override (3 slide section B) — pindah ke posisi setelah ManageWorkers (sebelum Assessment Monitoring).

Sequence current (post-T16): Landing → Workers(A) → AssessMonitor(C) → CoachMap(B) → Silabus(B) → Override(B) → Maintenance(C+D)

Sequence target: Landing → Workers(A) → CoachMap(B) → Silabus(B) → Override(B) → AssessMonitor(C) → Maintenance(C+D)

Operation: cut AssessMonitor block dari posisi #2 setelah Landing → paste antara Override dan Maintenance.

- [ ] **Step 3: Cut AssessmentMonitoring block**

- [ ] **Step 4: Paste AssessmentMonitoring block setelah Override closing div, sebelum Maintenance**

- [ ] **Step 5: Renumber data-slide + badge sequential 1..40**

- [ ] **Step 6: Verify count + browser smoke test**

```bash
# Smoke test: BAGIAN 4 sequence should be Landing → Workers → CoachMap → Silabus → Override → AssessMonitor → Maintenance
grep -A1 'BAGIAN 4 &mdash; ADMIN' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | grep slide-title
```

- [ ] **Step 7: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): O7 — reorder Admin slides match ABCD landing (CoachMap→Silabus→Override→Monitor)"
```

---

## Phase 3 — Final Verification

### Task 19: Full verification + browser smoke test

**Why:** Setelah 18 task edit, pastikan final state konsisten dan tidak ada broken state.

**Files:**
- Read only verification: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: Verify slide count**

```bash
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```
Expected: **40**

- [ ] **Step 2: Verify badge text consistency**

```bash
grep -o 'SLIDE [0-9]* / [0-9]*' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | sort -u
```
Expected: all entries end with `/ 40`. No `/ 41`.

- [ ] **Step 3: Verify data-slide sequential**

```bash
grep -o 'data-slide="[0-9]*"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | sort -u | wc -l
```
Expected: **40** (1..40 unique).

```bash
# Find gaps:
grep -o 'data-slide="[0-9]*"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | sed 's/[^0-9]//g' | sort -n | uniq
```
Expected: 1, 2, 3, ..., 40 (no gap, no duplicate).

- [ ] **Step 4: Verify JS TOTAL = 40**

```bash
grep -n 'const TOTAL' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```
Expected: `const TOTAL = 40;`

- [ ] **Step 5: Verify section-eyebrow count**

```bash
grep -c 'class="section-eyebrow"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```
Expected: 38 (semua slide kecuali cover S1 + penutup S40 = 38).

- [ ] **Step 6: Verify no remaining "COACHING PROTON / CDP" label**

```bash
grep -c 'COACHING PROTON / CDP' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```
Expected: 0.

- [ ] **Step 7: Browser smoke test full sweep**

Open file di browser:
- Press Home → counter "1 / 40", cover slide
- Press End → counter "40 / 40", penutup
- Arrow Right walk through semua slide. Verify:
  - No blank slide
  - Each slide-badge counter matches counter di bottom
  - Section eyebrow muncul di semua slide non-cover/non-penutup
  - BAGIAN 0: 9 slide (was 10 → minus S8 + plus Notifikasi)
  - BAGIAN 1: 8 slide (was 7 → plus IDP)
  - BAGIAN 2: 3 slide (was 3, tapi Progresi pindah keluar)
  - BAGIAN 3: 8 slide (was 10 → minus Progresi pindah ke awal, minus IDP, plus Progresi back as opener)

Recount expected slide distribution:
```
B0 Pengenalan: S2-S11 (10 slide) = Welcome+Latar+ApaItu+3Platform+Role+Akses+AreaHC+AlurHarian+Notif
B1 CMP:         S12-S19 (8 slide)  = Overview+Records+Analytics+Sistem+5Kategori+AlurUmum+PrePost+IDP
B2 Proton:      S20-S22 (3 slide)  = Assessment+AlurT1-2+AlurT3
B3 Lifecycle:   S23-S30 (8 slide)  = Progresi+Reviewer+Dual+Hierarki+Alur9+AlurMahir+Dashboard+Histori+Renewal
                                     (wait that's 9, hmm)
```

Recount manually:
- BAGIAN 3 pre-fix slides: 21 Reviewer, 22 Dual, 23 Hierarki, 24 Progresi, 25 Alur9, 26 AlurMahir, 27 Dashboard, 28 Histori, 29 Renewal, 30 IDP = 10
- Operations: -IDP (T11 menjadi single IDP, kemudian T16 pindah ke B1), -Progresi (T17 pindah ke awal B3 — still in B3), reorder dengan Progresi at start
- After fix: Progresi at start of B3 (10 - 1 IDP = 9 slide di B3)

Let me recount slide total:
- B0: 9 original (S2-S10) + S8 dropped (-1) + Notif moved in (+1) = 9 slide post-fix
- B1: 7 original (S11-S17) + IDP moved in (+1) = 8 slide
- B2: 3 original (S18-S20)
- B3: 10 original (S21-S30) - IDP moved out (-1) = 9 slide  
- B4: 7 original (S31-S37) — reorder no count change
- B5: 4 original (S38-S41) - Notif moved out (-1) = 3 slide
- Cover S1 + Penutup (was S41) = 2

Total: 1 + 9 + 8 + 3 + 9 + 7 + 3 + 1 = 41? Wait cover + penutup udah dihitung di B0 + B5.

Reset count:
- Slot 1: Cover
- B0: 9 slot (Welcome, Latar, ApaItu, 3Platform, Role, Akses, AreaHC, AlurHarian, Notif) = slots 2-10
- B1: 8 slot (Overview, Records, Analytics, Sistem, 5Kategori, AlurUmum, PrePost, IDP) = slots 11-18
- B2: 3 slot (Assessment, AlurT1-2, AlurT3) = slots 19-21
- B3: 9 slot (Progresi, Reviewer, Dual, Hierarki, Alur9, AlurMahir, Dashboard, Histori, Renewal) = slots 22-30
- B4: 7 slot (Landing, Workers, CoachMap, Silabus, Override, AssessMonitor, Maintenance) = slots 31-37
- B5: 3 slot (TugasCepat, ReferenceCard, Penutup) = slots 38-40

Total: 1 + 9 + 8 + 3 + 9 + 7 + 3 = **40** ✓

- [ ] **Step 8: No commit needed jika verifikasi pass. Jika ada issue, return ke task affected, fix, commit.**

---

## Phase 4 — Bonus Tier 3 LOW (sudah covered atau no-op)

- **R1** Bell card S8 — DROPPED via Task 14 ✓
- **R2** RBAC card S8 — DROPPED via Task 14 ✓
- **R5** Dual Track repetition — acceptable, no-op
- **M1** S21 rename — done Task 2
- **M2** S36 rename — done Task 3
- **M3** S11 disclaim — done Task 4
- **M5** S37/S38 tip-bar — done Task 5
- **M6** S2/S31 disclaim — done Task 6
- **G2** "BAGIAN 0" — kept, no change

Semua Tier 3 sudah ter-handle. Tidak ada bonus task tambahan.

---

## Risk & Rollback

**Renumber complexity:** Phase 2 task 14-18 punya step renumber data-slide yang verbose. Risk typo → broken navigation.

**Mitigation per task:**
- Verify count grep setelah setiap commit
- Browser smoke test setelah Phase 2 selesai (Task 19)
- Tiap commit terpisah → granular `git revert` jika fail

**Rollback strategy:**
- Single task fail: `git revert HEAD` rollback ke pre-task state
- Multiple task fail: `git revert <commit-hash>...HEAD` per task
- Catastrophic: `git reset --hard <pre-audit-commit>` (commit `535c45e9` pre-audit baseline)

**No DB changes, no migration, no Production impact.** Only `docs/` static file. Aman.

---

## Self-Review Results

**Spec coverage check:** semua 21 finding di spec mapped ke task atau marked no-op:
- HIGH 5: O1→T14, O2→T17, O4→T15, O7→T18, G1→T10 ✓
- MED 8: R1→T14 byproduct, R3→T11, R4→T12, O3→T16, O5→T8, O6→T9, M4→T1, M7→T7 ✓
- LOW 8: R2→T14 byproduct, R5 no-op, M1→T2, M2→T3, M3→T4, M5→T5, M6→T6, G2 no-op ✓

**Placeholder scan:** zero TBD/TODO. Each step actionable.

**Type consistency:** N/A (HTML edit, no types).

**Total commits expected:** ~19 commits (T1-T13 phase 1 = 12 commits karena T13 skipped, T14-T18 phase 2 = 5 commits, T19 verify = 0 commit). Total **17 commits** atomic.

---

## Execution Notes

- **Wajib gunakan Edit tool** untuk text-level changes (no sed/awk — Windows bash quirks bisa corrupt encoding, sudah ada precedent `02859e63` mojibake fix).
- **Hati-hati encoding:** file pakai HTML entities (`&mdash;`, `&amp;`, dll). Preserve verbatim.
- **Pre-each-task:** Read affected slide block dulu untuk verify current state — line numbers di plan ini approximate (pre-T1), akan shift setelah edit.
- **Browser test:** chrome/edge file://path, atau pakai Playwright kalau ada di config.

---

## Next Step Setelah Plan Complete

Pilih eksekusi:

1. **Subagent-Driven (recommended)** — dispatch fresh subagent per task, review antar-task, fast iterate
2. **Inline Execution** — execute di session ini pakai executing-plans skill, batch checkpoint

User decide.
