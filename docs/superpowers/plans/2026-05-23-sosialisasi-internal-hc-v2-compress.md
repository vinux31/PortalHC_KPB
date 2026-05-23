# Sosialisasi-Internal-Tim-HC v2 Compress (40→30 slide) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hasilkan `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (30 slide) dari sumber v1 (40 slide) via drop 2 slide + merge 8 pair, untuk presentasi live 60 menit.

**Architecture:** Bottom-up edit (slide 40 → 1) supaya renumber attribute paling akhir, mencegah chaos saat merge nomor di atas berubah saat edit nomor di bawah. Phase 7 sapu bersih semua data-slide/badge/comment. Phase 9 verifikasi via Playwright MCP smoke-test.

**Tech Stack:** HTML5 + CSS (existing, no framework), vanilla JS (counter + navigation), Playwright MCP (smoke-test browser).

**Spec ref:** `docs/superpowers/specs/2026-05-23-sosialisasi-internal-hc-v2-compress-design.md`

---

## Execution Notes (Important)

1. **Line numbers di plan = snapshot baseline v1.** Setelah edit Task 2/3/4/dst, line number bisa geser karena slide dihapus/diganti. **Edit tool pakai string match (bukan line number), jadi tetap reliable.** Untuk re-locate, gunakan `grep -n 'SLIDE X: TITLE'` atau `grep -n 'data-slide="N"'`.

2. **Bottom-up edit ideal.** Tasks 2-10 sengaja diurutkan dari slide nomor tinggi → rendah (file bottom → top), supaya delete/merge di bawah tidak bikin line number di atas geser saat task lain edit. Walau Edit tool tidak peduli line number, ini bantu human review.

3. **"COPY DARI v1 slide N slide-body"** = baca konten asli dari file v2 (sebelum edit task ini), pindahkan ke struktur baru. Bukan placeholder TBD — instruksi konkret "ambil isi dari sana, taruh di sini, condensed". Layout template di plan ini = wrapper struktural; konten teks/komponen visual ambil dari source.

4. **Verify count `class="slide.*data-slide=`** = pakai regex agar tidak ketelan `querySelector('[data-slide="...]')` di JS script footer.

---

## File Structure

- **Create:** `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (root, untracked → committed via v2 tasks)
- **Source ref (read-only):** `Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (root) atau `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (identik)
- **Spec ref (read-only):** `docs/superpowers/specs/2026-05-23-sosialisasi-internal-hc-v2-compress-design.md`

Tidak ada file CSS terpisah — semua style inline `<style>` di file HTML.

---

## Task 1: Baseline copy v1 → v2

**Files:**
- Create: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`
- Source: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: Copy v1 ke v2**

Run:
```bash
cp "Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html" "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```

- [ ] **Step 2: Verify file copy intact**

Run:
```bash
diff -q "Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html" "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: no output (files identical).

- [ ] **Step 3: Commit baseline**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "feat(sosialisasi-internal-hc-v2): baseline copy from v1 (40 slide)"
```

---

## Task 2: Phase 1 — Merge slide 38+39 (Closing: Tugas Cepat + Reference Card)

**Files:**
- Modify: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (slide v1 #38 at line 3787-3835, slide v1 #39 at line 3836-3872)

**Source content references (read v1 lines):**
- Slide 38 (Tugas HC Cepat): subtitle "Checklist harian, mingguan, bulanan" + 3-list checklist
- Slide 39 (Reference Card): subtitle "Dokumen detail untuk self-service kapan saja" + list dokumen rujukan

- [ ] **Step 1: Read both slide bodies di v2 file**

Read `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` lines 3787-3872. Identifikasi:
- Slide 38 `slide-body` content
- Slide 39 `slide-body` content

- [ ] **Step 2: Replace slide 38 block dengan merged version**

Edit slide 38 (data-slide="38"). Ganti seluruh `<div class="slide default-deco" data-slide="38">...</div>` jadi:

```html
<!-- ================= SLIDE 29: TUGAS CEPAT + REFERENCE CARD (MERGED) ================= -->
<div class="slide default-deco" data-slide="38">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">BAGIAN 5 &mdash; CLOSING</p>
      <h1 class="slide-title">Quick Reference <span class="accent">HC</span></h1>
      <p class="slide-subtitle">Checklist harian + dokumen rujukan self-service</p>
    </div>
    <div class="slide-badge">SLIDE 38 / 40</div>
  </div>
  <div class="slide-body">
    <div style="display:grid; grid-template-columns:1fr 1fr; gap:24px;">
      <div>
        <h3 style="margin-bottom:12px;">&#128203; Tugas HC Cepat</h3>
        <!-- COPY DARI v1 slide 38 slide-body content (checklist harian/mingguan/bulanan) -->
      </div>
      <div>
        <h3 style="margin-bottom:12px;">&#128218; Reference Card</h3>
        <!-- COPY DARI v1 slide 39 slide-body content (dokumen rujukan + URL cheatsheet) -->
        <p style="margin-top:12px; font-size:10pt; opacity:0.85;">&#128161; Hierarki kompetensi lengkap: lihat <strong>Panduan Operasional HC</strong> Bab 3.</p>
      </div>
    </div>
  </div>
</div>
```

Catatan: comment header masih `SLIDE 29` (target v2). data-slide=38 dan badge `SLIDE 38 / 40` belum diubah — Phase 7 nanti yang renumber. Note Hierarki ditambah supaya audience tau lokasi info Hierarki Kompetensi yg dropped.

- [ ] **Step 3: Delete slide 39 block entirely**

Hapus seluruh block dari `<!-- ================= SLIDE 40: REFERENCE CARD ================= -->` (line 3835) sampai `</div>` penutup slide 39 (sebelum baris `<!-- ================= SLIDE 41: TERIMA KASIH ================= -->`).

- [ ] **Step 4: Verify slide count turun jadi 39**

Run:
```bash
grep -c 'data-slide=' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: `39` (atau 40 termasuk JS querySelector line — filter via `grep -c 'class="slide.*data-slide='` untuk akurasi: expected `39`).

- [ ] **Step 5: Commit**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "feat(sosialisasi-internal-hc-v2): merge closing — Tugas Cepat + Reference Card (Phase 1)"
```

---

## Task 3: Phase 3a — Drop slide 25 (Hierarki Kompetensi)

**Files:**
- Modify: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (slide v1 #25 at line 3008-3049)

- [ ] **Step 1: Delete slide 25 block entirely**

Hapus dari `<!-- ================= SLIDE 23: HIERARKI KOMPETENSI PER TRACK ================= -->` (line 3007) sampai `</div>` penutup slide 25 sebelum `<!-- ================= SLIDE 25: ALUR COACHING — 9 STEP ================= -->`.

- [ ] **Step 2: Verify slide count turun jadi 38**

Run:
```bash
grep -cE 'class="slide.*data-slide=' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: `38`.

- [ ] **Step 3: Commit**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "feat(sosialisasi-internal-hc-v2): drop slide 25 Hierarki Kompetensi (Phase 3a)"
```

---

## Task 4: Phase 3b — Merge slide 23+24 (Coaching Chain + Dual Track)

**Files:**
- Modify: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (slide 23 line 2911-2966, slide 24 line 2967-3006)

- [ ] **Step 1: Read both slide bodies**

Read v2 file lines 2911-3006. Identifikasi:
- Slide 23 Reviewer Chain (Coach → SrSpv → SH → HC) — likely flow diagram CSS class
- Slide 24 Dual Track (Panelman | Operator dengan Th 1/2/3 pills) — `.dt-grid`, `.dt-col`, `.dt-pills`, `.dt-note`

- [ ] **Step 2: Replace slide 23 dengan merged version**

Ganti seluruh slide 23 block jadi merged:

```html
<!-- ================= SLIDE 18: COACHING CHAIN + DUAL TRACK (MERGED) ================= -->
<div class="slide default-deco" data-slide="23">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">BAGIAN 3 &mdash; PROTON LIFECYCLE &amp; COACHING</p>
      <h1 class="slide-title">Coaching PROTON &mdash; <span class="accent">Chain + Dual Track</span></h1>
      <p class="slide-subtitle">HC = Final Reviewer (ke-3) &middot; 2 track × 3 tahun independen (Panelman + Operator)</p>
    </div>
    <div class="slide-badge">SLIDE 23 / 40</div>
  </div>
  <div class="slide-body">
    <div style="display:grid; grid-template-rows:auto auto; gap:18px;">
      <div>
        <h3 style="margin-bottom:10px;">&#128279; Reviewer Chain</h3>
        <!-- COPY DARI v1 slide 23 slide-body inner content (flow Coach → SrSpv → SH → HC) -->
      </div>
      <div>
        <h3 style="margin-bottom:10px;">&#128106; Dual Track (2 track × 3 tahun)</h3>
        <!-- COPY DARI v1 slide 24 slide-body inner content (.dt-grid panelman + operator) -->
        <!-- PENTING: pas copy `.dt-note`, GANTI "3 track terpisah" jadi "3 tahun, hierarki & deliverable independen" (fix v1 typo per spec known-issue) -->
      </div>
    </div>
  </div>
</div>
```

Subtitle merge: "HC = Final Reviewer (ke-3) · 2 track × 3 tahun independen (Panelman + Operator)" — kombinasi 2 subtitle asli + fix wording konsisten.

- [ ] **Step 3: Delete slide 24 block entirely**

Hapus dari `<!-- ================= SLIDE 22: COACHING PROTON — DUAL TRACK ================= -->` (line 2966) sampai `</div>` penutup slide 24 sebelum baris kosong/komen berikutnya (sudah dihapus di Task 3, jadi sekarang sebelum `<!-- ================= SLIDE 25: ALUR COACHING — 9 STEP ================= -->`).

- [ ] **Step 4: Verify slide count 37**

Run:
```bash
grep -cE 'class="slide.*data-slide=' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: `37`.

- [ ] **Step 5: Commit**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "feat(sosialisasi-internal-hc-v2): merge Coaching Chain + Dual Track + fix v1 typo (Phase 3b)"
```

---

## Task 5: Phase 3c — Merge slide 26+27 (Alur Coaching Reguler vs Mahir)

**Files:**
- Modify: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (slide 26 line 3050-3141, slide 27 line 3142-3233)

- [ ] **Step 1: Read both slide bodies**

Slide 26 = Alur Coaching Reguler 9-Step. Slide 27 = Alur Coaching Mahir 9-Step. Cari swimlane/step CSS class (likely `.proton-stepper` atau similar dari CSS comment line 1729).

- [ ] **Step 2: Replace slide 26 dengan merged version (side-by-side 2-col)**

```html
<!-- ================= SLIDE 19: ALUR COACHING REGULER vs MAHIR (MERGED) ================= -->
<div class="slide default-deco" data-slide="26">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">BAGIAN 3 &mdash; PROTON LIFECYCLE &amp; COACHING</p>
      <h1 class="slide-title">Alur Coaching &mdash; <span class="accent">Reguler vs Mahir</span></h1>
      <p class="slide-subtitle">9-step parallel &middot; perbedaan kunci: Assessment Online (Reguler) vs Interview Offline (Mahir)</p>
    </div>
    <div class="slide-badge">SLIDE 26 / 40</div>
  </div>
  <div class="slide-body">
    <div style="display:grid; grid-template-columns:1fr 1fr; gap:20px;">
      <div>
        <h3 style="margin-bottom:10px;">&#128221; Reguler 9-Step</h3>
        <!-- COPY 9 step DARI v1 slide 26 slide-body, condensed: <span step-pill> 1. Step name </span> per step -->
        <p style="margin-top:8px; font-size:10pt; opacity:0.85;">Sertifikasi via Assessment Online</p>
      </div>
      <div>
        <h3 style="margin-bottom:10px;">&#127942; Mahir 9-Step</h3>
        <!-- COPY 9 step DARI v1 slide 27 slide-body, condensed -->
        <p style="margin-top:8px; font-size:10pt; opacity:0.85; color:var(--amber);">Sertifikasi Final via Interview Offline (Panel Juri)</p>
      </div>
    </div>
  </div>
</div>
```

Jika layout swimlane asli tidak muat 2-col 50/50, pakai fallback: vertical condensed list per kolom dengan step number + 1-line desc.

- [ ] **Step 3: Delete slide 27 block entirely**

Hapus dari `<!-- ================= SLIDE 26: ALUR COACHING MAHIR — 9 STEP ================= -->` (line 3141) sampai `</div>` penutup slide 27 sebelum `<!-- ================= SLIDE 27: COACHING PROTON DASHBOARD ================= -->`.

- [ ] **Step 4: Verify slide count 36**

Run:
```bash
grep -cE 'class="slide.*data-slide=' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: `36`.

- [ ] **Step 5: Commit**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "feat(sosialisasi-internal-hc-v2): merge Alur Coaching Reguler vs Mahir 9-step (Phase 3c)"
```

---

## Task 6: Phase 3d — Merge slide 28+29 (Coaching Dashboard + Histori)

**Files:**
- Modify: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (slide 28 line 3234-3291, slide 29 line 3292-3347)

- [ ] **Step 1: Read both slide bodies**

- Slide 28 Dashboard: 5 metric global + filter cascade + tab Bottleneck Report
- Slide 29 Histori PROTON: histori per pekerja + progress visual Th 1/2/3 + export Excel

- [ ] **Step 2: Replace slide 28 dengan merged version (top/bottom)**

```html
<!-- ================= SLIDE 20: COACHING DASHBOARD + HISTORI (MERGED) ================= -->
<div class="slide default-deco" data-slide="28">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">BAGIAN 3 &mdash; PROTON LIFECYCLE &amp; COACHING</p>
      <h1 class="slide-title">Coaching <span class="accent">Dashboard + Histori</span></h1>
      <p class="slide-subtitle">Dasbor metric real-time + histori coaching per pekerja dengan export Excel</p>
    </div>
    <div class="slide-badge">SLIDE 28 / 40</div>
  </div>
  <div class="slide-body">
    <div style="display:grid; grid-template-rows:1fr 1fr; gap:16px;">
      <div>
        <h3 style="margin-bottom:8px;">&#128202; Dashboard Global</h3>
        <!-- COPY DARI v1 slide 28 slide-body (5 metric + filter + tab bottleneck), condensed -->
      </div>
      <div>
        <h3 style="margin-bottom:8px;">&#128196; Histori per Pekerja</h3>
        <!-- COPY DARI v1 slide 29 slide-body (progress Th 1/2/3 + export Excel CTA), condensed -->
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 3: Delete slide 29 block entirely**

Hapus dari `<!-- ================= SLIDE 28: HISTORI PROTON + EXPORT ================= -->` (line 3291) sampai `</div>` penutup slide 29 sebelum `<!-- ================= SLIDE 29: RENEWAL CERTIFICATE LIFECYCLE ================= -->`.

- [ ] **Step 4: Verify slide count 35**

```bash
grep -cE 'class="slide.*data-slide=' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: `35`.

- [ ] **Step 5: Commit**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "feat(sosialisasi-internal-hc-v2): merge Coaching Dashboard + Histori (Phase 3d)"
```

---

## Task 7: Phase 4 — Merge slide 20+21 (Alur PROTON Th 1-2 + Th 3)

**Files:**
- Modify: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (slide 20 line 2755-2805, slide 21 line 2806-2848)

- [ ] **Step 1: Read both slide bodies**

- Slide 20 = Alur PROTON Th 1-2 ("Identik Alur Assessment Umum — beda di 4 aspek", online format)
- Slide 21 = Alur PROTON Th 3 ("Interview Offline (Tatap Muka, Panel Juri)")

- [ ] **Step 2: Replace slide 20 dengan merged version (top/bottom flow)**

```html
<!-- ================= SLIDE 16: ALUR PROTON Th 1-2 + Th 3 (MERGED) ================= -->
<div class="slide default-deco" data-slide="20">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">BAGIAN 2 &mdash; ASSESSMENT PROTON</p>
      <h1 class="slide-title">Alur PROTON &mdash; <span class="accent">Th 1-2 vs Th 3</span></h1>
      <p class="slide-subtitle">Th 1-2: format online (identik Alur Umum, beda 4 aspek) &middot; Th 3: panel offline tatap muka</p>
    </div>
    <div class="slide-badge">SLIDE 20 / 40</div>
  </div>
  <div class="slide-body">
    <div style="display:grid; grid-template-rows:1fr 1fr; gap:14px;">
      <div>
        <h3 style="margin-bottom:8px;">Th 1-2 &mdash; <span style="color:var(--teal);">Format Online</span></h3>
        <!-- COPY DARI v1 slide 20 slide-body flowchart/stepper, condensed -->
      </div>
      <div>
        <h3 style="margin-bottom:8px;">Th 3 &mdash; <span style="color:var(--amber);">Interview Offline (Panel Juri)</span></h3>
        <!-- COPY DARI v1 slide 21 slide-body flowchart/stepper, condensed -->
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 3: Delete slide 21 block entirely**

Hapus dari `<!-- ================= SLIDE 20: ALUR PROTON — TAHUN 3 ================= -->` (line 2805) sampai `</div>` penutup slide 21 sebelum `<!-- ================= SLIDE 24: PROGRESI KOMPETENSI PER TAHUN ================= -->` (line 2848).

- [ ] **Step 4: Verify slide count 34**

```bash
grep -cE 'class="slide.*data-slide=' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: `34`.

- [ ] **Step 5: Commit**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "feat(sosialisasi-internal-hc-v2): merge Alur PROTON Th1-2 + Th3 (Phase 4)"
```

---

## Task 8: Phase 5 — Merge slide 12+13 (Records Team + Analytics Dashboard)

**Files:**
- Modify: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (slide 12 line 2353-2408, slide 13 line 2409-2475)

- [ ] **Step 1: Read both slide bodies**

- Slide 12 = Records Team (filter cascade + table)
- Slide 13 = Analytics Dashboard (4 metric + 6 tab chart)

- [ ] **Step 2: Replace slide 12 dengan merged version (split-col)**

```html
<!-- ================= SLIDE 9: RECORDS TEAM + ANALYTICS DASHBOARD (MERGED) ================= -->
<div class="slide default-deco" data-slide="12">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">BAGIAN 1 &mdash; CMP</p>
      <h1 class="slide-title">Records Team + <span class="accent">Analytics Dashboard</span></h1>
      <p class="slide-subtitle">Riwayat tim real-time + dasbor metric assessment dengan export Excel</p>
    </div>
    <div class="slide-badge">SLIDE 12 / 40</div>
  </div>
  <div class="slide-body">
    <div style="display:grid; grid-template-columns:1fr 1fr; gap:20px;">
      <div>
        <h3 style="margin-bottom:10px;">&#128203; Records Team</h3>
        <!-- COPY DARI v1 slide 12 slide-body (filter cascade + table snippet), condensed -->
      </div>
      <div>
        <h3 style="margin-bottom:10px;">&#128200; Analytics Dashboard</h3>
        <!-- COPY DARI v1 slide 13 slide-body (4 metric tile + chart thumbnail), condensed -->
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 3: Delete slide 13 block**

Hapus dari `<!-- ================= SLIDE 13: ANALYTICS DASHBOARD ================= -->` (line 2408) sampai `</div>` penutup slide 13 sebelum `<!-- ================= SLIDE 14: SISTEM ASSESSMENT ================= -->`.

- [ ] **Step 4: Verify slide count 33**

```bash
grep -cE 'class="slide.*data-slide=' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: `33`.

- [ ] **Step 5: Commit**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "feat(sosialisasi-internal-hc-v2): merge CMP Records + Analytics (Phase 5)"
```

---

## Task 9: Phase 6a — Drop slide 3 (Latar Belakang) + Merge slide 4+5 (Apa Itu + 3 Platform)

**Files:**
- Modify: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (slide 3 line 1879-1926, slide 4 line 1927-1966, slide 5 line 1967-2032)

- [ ] **Step 1: Delete slide 3 block entirely**

Hapus dari `<!-- ================= SLIDE 3: LATAR BELAKANG ================= -->` (line 1878) sampai `</div>` penutup slide 3 sebelum `<!-- ================= SLIDE 4: APA ITU HC PORTAL KPB? ================= -->`.

- [ ] **Step 2: Read slide 4 + slide 5 bodies**

- Slide 4 = "Apa Itu HC Portal KPB?" — definisi sistem
- Slide 5 = "3 Platform Terpadu" — 3 card CMP/CDP/BP

- [ ] **Step 3: Replace slide 4 dengan merged version (top: definisi, bottom: 3 platform cards)**

```html
<!-- ================= SLIDE 3: APA ITU HC PORTAL + 3 PLATFORM (MERGED) ================= -->
<div class="slide default-deco" data-slide="4">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">BAGIAN 0 &mdash; PENGENALAN</p>
      <h1 class="slide-title">Apa Itu <span class="accent">HC Portal</span> KPB?</h1>
      <p class="slide-subtitle">Sistem informasi terpadu Tim HC Kilang Pertamina Balikpapan &middot; 3 platform: CMP &middot; CDP &middot; BP</p>
    </div>
    <div class="slide-badge">SLIDE 4 / 40</div>
  </div>
  <div class="slide-body">
    <div style="display:grid; grid-template-rows:auto 1fr; gap:18px;">
      <div>
        <!-- COPY definisi paragraf DARI v1 slide 4 slide-body, condensed 2-3 baris -->
      </div>
      <div>
        <!-- COPY 3 card platform DARI v1 slide 5 slide-body (CMP / CDP / BP grid), condensed -->
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 4: Delete slide 5 block entirely**

Hapus dari `<!-- ================= SLIDE 5: 3 PLATFORM TERPADU ================= -->` sampai `</div>` penutup slide 5 sebelum `<!-- ================= SLIDE 6: STRUKTUR ROLE PENGGUNA ================= -->`.

- [ ] **Step 5: Verify slide count 31**

```bash
grep -cE 'class="slide.*data-slide=' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: `31`.

- [ ] **Step 6: Commit**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "feat(sosialisasi-internal-hc-v2): drop Latar Belakang + merge Apa Itu+3 Platform (Phase 6a)"
```

---

## Task 10: Phase 6b — Merge slide 9+10 (Alur Harian + Notifikasi)

**Files:**
- Modify: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (slide 9 line 2185-2234, slide 10 line 2234-2304)

- [ ] **Step 1: Read both slide bodies**

- Slide 9 = "Alur Kerja Harian HC" — 5 aktivitas inti
- Slide 10 = "Notifikasi & Workflow" — bell icon di navbar

- [ ] **Step 2: Replace slide 9 dengan merged version (split-col)**

```html
<!-- ================= SLIDE 7: ALUR HARIAN + NOTIFIKASI (MERGED) ================= -->
<div class="slide default-deco" data-slide="9">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">BAGIAN 0 &mdash; PENGENALAN</p>
      <h1 class="slide-title">Alur <span class="accent">Harian</span> + Notifikasi</h1>
      <p class="slide-subtitle">5 aktivitas inti harian &middot; bell icon = entry point notif terpusat</p>
    </div>
    <div class="slide-badge">SLIDE 9 / 40</div>
  </div>
  <div class="slide-body">
    <div style="display:grid; grid-template-columns:1fr 1fr; gap:20px;">
      <div>
        <h3 style="margin-bottom:10px;">&#128338; Alur Kerja Harian HC</h3>
        <!-- COPY DARI v1 slide 9 slide-body (5 aktivitas inti), condensed -->
      </div>
      <div>
        <h3 style="margin-bottom:10px;">&#128276; Notifikasi &amp; Workflow</h3>
        <!-- COPY DARI v1 slide 10 slide-body (bell icon mockup + notif sample), condensed -->
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 3: Delete slide 10 block entirely**

Hapus dari `<!-- ================= SLIDE 38: NOTIFIKASI & WORKFLOW ================= -->` sampai `</div>` penutup slide 10 sebelum `<!-- ================= SLIDE 11: CMP OVERVIEW ================= -->`.

- [ ] **Step 4: Verify slide count 30**

```bash
grep -cE 'class="slide.*data-slide=' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: `30`. **Target achieved.**

- [ ] **Step 5: Commit**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "feat(sosialisasi-internal-hc-v2): merge Alur Harian + Notifikasi — reach 30 slide target (Phase 6b)"
```

---

## Task 11: Phase 7 — Renumber data-slide 1-30, TOTAL=30, badge, block comments

**Files:**
- Modify: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (semua occurrence data-slide, slide-badge, TOTAL constant, block comments)

**Mapping renumber lama → baru (data-slide value):**

| Old | New | Title |
|---|---|---|
| 1 | 1 | Cover |
| 2 | 2 | Selamat Datang |
| 4 | 3 | Apa Itu HC Portal + 3 Platform (merged) |
| 6 | 4 | Struktur Role |
| 7 | 5 | Cara Mengakses |
| 8 | 6 | Area Kerja HC |
| 9 | 7 | Alur Harian + Notifikasi (merged) |
| 11 | 8 | CMP Overview |
| 12 | 9 | Records Team + Analytics (merged) |
| 14 | 10 | Sistem Assessment |
| 15 | 11 | 5 Kategori Assessment |
| 16 | 12 | Alur Assessment 7-Step |
| 17 | 13 | Pre/Post Test Gain Score |
| 18 | 14 | IDP Library |
| 19 | 15 | Assessment PROTON intro |
| 20 | 16 | Alur PROTON Th 1-2 + Th 3 (merged) |
| 22 | 17 | Progresi Kompetensi |
| 23 | 18 | Coaching Chain + Dual Track (merged) |
| 26 | 19 | Alur Coaching Reguler vs Mahir (merged) |
| 28 | 20 | Coaching Dashboard + Histori (merged) |
| 30 | 21 | Renewal Certificate Lifecycle |
| 31 | 22 | Admin Panel Landing |
| 32 | 23 | Manajemen Pekerja |
| 33 | 24 | Coach-Coachee Mapping |
| 34 | 25 | Silabus + Guidance Files |
| 35 | 26 | Override KKJ + Mapping Silabus |
| 36 | 27 | Assessment Monitoring |
| 37 | 28 | Maintenance + Audit Log |
| 38 | 29 | Tugas Cepat + Reference Card (merged) |
| 40 | 30 | Penutup |

- [ ] **Step 1: Update JS TOTAL constant**

Edit: cari line `const TOTAL = 40;` → ganti jadi `const TOTAL = 30;`.

- [ ] **Step 2: Update controls counter default**

Cari line `<span class="slide-counter" id="slideCounter">1 / 40</span>` → ganti jadi `1 / 30`.

- [ ] **Step 3: Renumber semua data-slide attribute**

Karena urutan masih monoton old (1,2,4,6,7,8,9,11,12,14...) tapi nilai data-slide masih old, perlu sequential renumber. Strategi paling aman: **edit per slide top-down**, ganti `data-slide="OLD"` jadi `data-slide="NEW"` PLUS update badge `SLIDE OLD / 40` jadi `SLIDE NEW / 30` PLUS update block comment `<!-- ================= SLIDE X: TITLE -->` agar match.

Per slide (lakukan urut top-down):

1. `data-slide="1"` → tetap `1`, badge `1 / 30`, comment `SLIDE 1: COVER`
2. `data-slide="2"` → tetap `2`, badge `2 / 30`, comment `SLIDE 2: SELAMAT DATANG`
3. `data-slide="4"` → ganti `3`, badge `3 / 30`, comment `SLIDE 3: APA ITU HC PORTAL + 3 PLATFORM`
4. `data-slide="6"` → ganti `4`, badge `4 / 30`, comment `SLIDE 4: STRUKTUR ROLE PENGGUNA`
5. `data-slide="7"` → ganti `5`, badge `5 / 30`, comment `SLIDE 5: CARA MENGAKSES HC PORTAL`
6. `data-slide="8"` → ganti `6`, badge `6 / 30`, comment `SLIDE 6: AREA KERJA HC DI PORTAL`
7. `data-slide="9"` → ganti `7`, badge `7 / 30`, comment `SLIDE 7: ALUR HARIAN + NOTIFIKASI`
8. `data-slide="11"` → ganti `8`, badge `8 / 30`, comment `SLIDE 8: CMP OVERVIEW`
9. `data-slide="12"` → ganti `9`, badge `9 / 30`, comment `SLIDE 9: RECORDS TEAM + ANALYTICS DASHBOARD`
10. `data-slide="14"` → ganti `10`, badge `10 / 30`, comment `SLIDE 10: SISTEM ASSESSMENT`
11. `data-slide="15"` → ganti `11`, badge `11 / 30`, comment `SLIDE 11: 5 KATEGORI ASSESSMENT UMUM`
12. `data-slide="16"` → ganti `12`, badge `12 / 30`, comment `SLIDE 12: ALUR ASSESSMENT — 7 STEP END-TO-END`
13. `data-slide="17"` → ganti `13`, badge `13 / 30`, comment `SLIDE 13: PRE / POST TEST — GAIN SCORE`
14. `data-slide="18"` → ganti `14`, badge `14 / 30`, comment `SLIDE 14: IDP LIBRARY`
15. `data-slide="19"` → ganti `15`, badge `15 / 30`, comment `SLIDE 15: ASSESSMENT PROTON`
16. `data-slide="20"` → ganti `16`, badge `16 / 30`, comment `SLIDE 16: ALUR PROTON Th 1-2 + Th 3`
17. `data-slide="22"` → ganti `17`, badge `17 / 30`, comment `SLIDE 17: PROGRESI KOMPETENSI PER TAHUN`
18. `data-slide="23"` → ganti `18`, badge `18 / 30`, comment `SLIDE 18: COACHING CHAIN + DUAL TRACK`
19. `data-slide="26"` → ganti `19`, badge `19 / 30`, comment `SLIDE 19: ALUR COACHING REGULER vs MAHIR`
20. `data-slide="28"` → ganti `20`, badge `20 / 30`, comment `SLIDE 20: COACHING DASHBOARD + HISTORI`
21. `data-slide="30"` → ganti `21`, badge `21 / 30`, comment `SLIDE 21: RENEWAL CERTIFICATE LIFECYCLE`
22. `data-slide="31"` → ganti `22`, badge `22 / 30`, comment `SLIDE 22: ADMIN PANEL LANDING`
23. `data-slide="32"` → ganti `23`, badge `23 / 30`, comment `SLIDE 23: MANAJEMEN PEKERJA`
24. `data-slide="33"` → ganti `24`, badge `24 / 30`, comment `SLIDE 24: COACH-COACHEE MAPPING`
25. `data-slide="34"` → ganti `25`, badge `25 / 30`, comment `SLIDE 25: SILABUS + GUIDANCE FILES`
26. `data-slide="35"` → ganti `26`, badge `26 / 30`, comment `SLIDE 26: OVERRIDE KKJ + MAPPING SILABUS`
27. `data-slide="36"` → ganti `27`, badge `27 / 30`, comment `SLIDE 27: ASSESSMENT MONITORING`
28. `data-slide="37"` → ganti `28`, badge `28 / 30`, comment `SLIDE 28: MAINTENANCE + AUDIT LOG`
29. `data-slide="38"` → ganti `29`, badge `29 / 30`, comment `SLIDE 29: TUGAS CEPAT + REFERENCE CARD`
30. `data-slide="40"` → ganti `30`, badge `30 / 30`, comment `SLIDE 30: TERIMA KASIH`

**PENTING:** Renumber **descending order** (slide 40→1) supaya tidak terjadi collision (mis. saat ganti data-slide=11 jadi 8, sementara slide 8 lama masih ada — collision). Mulai dari ganti slide 40 → 30 dulu, slide 38 → 29, slide 37 → 28, dst sampai slide 4 → 3. Slide 1 dan 2 tetap.

- [ ] **Step 4: Verify all data-slide sequential 1-30**

Run:
```bash
grep -oE 'data-slide="[0-9]+"' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html" | sort -u | wc -l
```
Expected: `30`.

```bash
grep -oE 'data-slide="[0-9]+"' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html" | sort -V | uniq
```
Expected: `data-slide="1"` ... `data-slide="30"` (30 unique values, no gap).

- [ ] **Step 5: Verify all badge "X / 30"**

Run:
```bash
grep -c 'SLIDE [0-9]\+ / 30' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: `29` (slide 1 cover tidak punya badge, 29 slide lain punya).

Run:
```bash
grep 'SLIDE.*/ 40' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: no output (no leftover "/ 40").

- [ ] **Step 6: Verify TOTAL=30**

```bash
grep -n 'const TOTAL' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Expected: `const TOTAL = 30;`

- [ ] **Step 7: Commit**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "refactor(sosialisasi-internal-hc-v2): renumber data-slide 1-30 + TOTAL=30 + clean block comments (Phase 7)"
```

---

## Task 12: Phase 8 — Cross-ref fix (slide 15 → Progresi reference)

**Files:**
- Modify: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (cross-ref text dalam slide 15 baru, ex slide 19 v1)

- [ ] **Step 1: Cari cross-ref text**

Run:
```bash
grep -nE 'slide Progresi|lihat slide|slide [0-9]+' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```
Tangkap semua occurrence yang sebut nomor/title slide lain. Catat baris untuk fix.

- [ ] **Step 2: Update cross-ref pakai title-only (drop nomor spesifik)**

Cari text seperti `Detail komparasi 5 aspek per tahun ... di **slide Progresi Kompetensi** (awal BAGIAN 3).` — pastikan tidak ada nomor slide hardcoded yang invalid. Jika ada, drop nomor, pakai title saja.

Contoh edit: `di slide Progresi Kompetensi (BAGIAN 3)` (sudah title-based, no nomor — OK biarkan).

Untuk reference `lihat slide Struktur Role di awal deck` (line ex-2179) — sudah title-only, tidak perlu fix.

Untuk reference yang HARDCODED nomor (jika ada), ganti ke title-based.

- [ ] **Step 3: Verify no invalid number reference**

Run:
```bash
grep -nE 'slide [0-9]+' "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html" | grep -v 'data-slide\|SLIDE [0-9]\+ / 30\|/\* '
```
Expected: tidak ada output (atau hanya komentar CSS legacy yang non-rendering).

- [ ] **Step 4: Commit (jika ada perubahan)**

```bash
git add "Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
git commit -m "fix(sosialisasi-internal-hc-v2): cross-ref title-only (Phase 8)" || echo "no cross-ref to fix"
```

---

## Task 13: Phase 9 — Browser smoke-test via Playwright MCP

**Files:**
- Read: `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

- [ ] **Step 1: Open v2 file di browser**

Pakai tool `mcp__plugin_playwright_playwright__browser_navigate`:
URL: `file:///C:/Users/Administrator/OneDrive%20-%20PT%20Pertamina%20(Persero)/Desktop/PortalHC_KPB/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

- [ ] **Step 2: Verify counter awal "1 / 30"**

Snapshot via `browser_snapshot`. Confirm element with id `slideCounter` shows `1 / 30`.

- [ ] **Step 3: Navigate via keyboard ArrowRight 29x**

Loop 29x: `browser_press_key` dengan key `ArrowRight`. After each press, optional snapshot untuk verify counter increment.

After last press: counter must show `30 / 30`.

- [ ] **Step 4: Verify no JS console errors**

Pakai `browser_console_messages`. Expected: empty atau hanya info-level messages, no error.

- [ ] **Step 5: Verify BAGIAN label progression**

Take snapshot per BAGIAN transition. Confirm section-eyebrow text monotonic: cover → BAGIAN 0 → BAGIAN 1 → BAGIAN 2 → BAGIAN 3 → BAGIAN 4 → BAGIAN 5. No back-step.

- [ ] **Step 6: Spot-check merged slide layout overflow**

Navigate ke slide 7 (Alur+Notif merged), slide 9 (Records+Analytics), slide 16 (Alur PROTON), slide 18 (Chain+Dual), slide 19 (Reguler vs Mahir), slide 20 (Dashboard+Histori), slide 29 (Tugas+Ref).

Take screenshot per merged slide. Visual check: tidak ada teks overflow keluar slide frame 1280x720.

- [ ] **Step 7: Close browser**

`browser_close`.

- [ ] **Step 8: Commit smoke-test result (jika ada fix yang dilakukan)**

Jika ada layout overflow → fix CSS/HTML inline, commit. Jika smoke-test bersih:
```bash
git commit --allow-empty -m "test(sosialisasi-internal-hc-v2): playwright smoke-test passed (Phase 9)"
```

---

## Task 14: Final tag v2.0

**Files:** None (git tag only)

- [ ] **Step 1: Create annotated tag**

```bash
git tag -a "sosialisasi-internal-hc-v2.0" -m "Sosialisasi-Internal-Tim-HC v2.0 — 30-slide compressed deck for 60-min live presentation

Source: 40-slide v1 (deep-dive). Reduction: drop 2 slide (Latar Belakang, Hierarki Kompetensi), merge 8 pair (Apa Itu+Platform, Alur+Notif, Records+Analytics, PROTON Alur Th1-2+Th3, Chain+DualTrack, Reguler+Mahir, Dashboard+Histori, Tugas+Reference).

Priority preserve full: Assessment (5/5) + Admin Panel (7/7)."
```

- [ ] **Step 2: Verify tag created**

```bash
git tag -l "sosialisasi-internal-hc-v2.0"
```
Expected: `sosialisasi-internal-hc-v2.0`.

- [ ] **Step 3: Report ready for push (user-triggered, jangan auto-push)**

Final state: file `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (30 slide) committed di branch main + tag `sosialisasi-internal-hc-v2.0` local. Push ke origin manual oleh user.

---

## Verification Summary

End-state checklist (matches spec Success Criteria):

- [ ] File `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` punya 30 slide (data-slide 1-30 sequential, no gap).
- [ ] Counter "1 / 30" ... "30 / 30" benar di browser.
- [ ] BAGIAN label monotonic cover→0→1→2→3→4→5.
- [ ] Assessment (v2 #10-14): 5/5 intact.
- [ ] Admin Panel (v2 #22-28): 7/7 intact.
- [ ] Merged slide tidak overflow.
- [ ] Tidak ada nomor slide v1 invalid di cross-ref text.
- [ ] No JS console error.
- [ ] Tag `sosialisasi-internal-hc-v2.0` created.
