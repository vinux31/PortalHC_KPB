# Slide 8 Flow Proses Solusi Terpilih HC Portal — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Buat `flow-proses-solusi-terpilih.html` — slide 8 §3.4 format 2-panel (kiri = 3 bangunan pilar sebelum/target, kanan = 9-step flow implementasi snake clockwise) + update index.html tambah card Opsi V.

**Architecture:** Single standalone HTML file, no external CDN. Pertamina color palette + print A3 landscape. Foto = placeholder gray box, user isi sendiri.

**Tech Stack:** HTML5, CSS3 (clip-path, CSS Grid), vanilla JS (print button only)

---

### Task 1: Buat flow-proses-solusi-terpilih.html

**Files:**
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/flow-proses-solusi-terpilih.html`

- [ ] **Step 1: Tulis file HTML lengkap**

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8"/>
<meta name="viewport" content="width=device-width,initial-scale=1"/>
<title>§3.4 Opsi V — Flow Proses Solusi Terpilih HC Portal</title>
<style>
:root {
  --red:   #C8102E; --red-l:   #fce8eb;
  --blue:  #00558C; --blue-l:  #e6f0f7;
  --green: #00A551; --green-l: #d4f0dd;
  --yellow:#FFC72C; --yellow-l:#fff8e1;
  --gray:  #6b7280; --bg: #f6f7fb; --border: #d1d5db;
  --green-dark: #1a5c3a;
  --fs-xs:.75rem; --fs-sm:.85rem; --fs-base:.95rem;
  --fs-md:1.05rem; --fs-lg:1.3rem; --fs-xl:2rem;
}
*{box-sizing:border-box;margin:0;padding:0;}
body{font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif;
     background:#c8dfc8;color:#1f2937;padding:1rem;}

/* HEADER BAR */
.header-bar{
  max-width:1400px;margin:0 auto .75rem;
  display:grid;grid-template-columns:160px 1fr 160px;
  align-items:center;background:white;border-radius:.75rem .75rem 0 0;
  box-shadow:0 2px 10px rgba(0,0,0,.06);
  padding:.85rem 1.5rem;border-top:5px solid var(--green);
}
.logo{font-weight:900;font-size:1.1rem;color:var(--red);line-height:1.1;}
.logo small{display:block;font-weight:600;font-size:var(--fs-xs);color:var(--gray);}
.header-title{text-align:center;}
.header-title h1{margin:0;font-size:1.3rem;color:var(--blue);}
.header-title small{display:block;color:var(--gray);font-size:var(--fs-sm);margin-top:.15rem;}
.pcp-badge{text-align:right;background:var(--blue);color:white;
           padding:.5rem .75rem;border-radius:.35rem;font-weight:700;font-size:var(--fs-sm);}
.pcp-badge small{display:block;font-weight:400;font-size:var(--fs-xs);opacity:.9;}

/* TOOLBAR */
.toolbar{
  max-width:1400px;margin:0 auto .75rem;
  display:flex;justify-content:space-between;align-items:center;
  padding:.6rem 1.25rem;background:white;border-radius:.35rem;
  box-shadow:0 1px 3px rgba(0,0,0,.06);
}
.toolbar .note{font-size:var(--fs-sm);color:var(--gray);}
.toolbar button{
  background:var(--red);color:white;border:none;
  padding:.45rem .9rem;border-radius:.35rem;font-weight:600;
  cursor:pointer;font-size:var(--fs-sm);
}
.toolbar button:hover{background:#a50d26;}

/* SLIDE TITLE */
.slide-title{
  max-width:1400px;margin:0 auto .6rem;
  background:var(--green-dark);color:white;
  text-align:center;padding:.6rem 1rem;
  font-size:1.25rem;font-weight:700;border-radius:.35rem;
  letter-spacing:.02em;
}

/* TWO-PANEL */
.two-panel{
  max-width:1400px;margin:0 auto;
  display:grid;grid-template-columns:43% 57%;gap:.75rem;
}

/* ── LEFT PANEL ── */
.left-panel{
  background:white;border:2px solid var(--border);border-radius:.5rem;
  padding:.85rem;display:flex;flex-direction:column;gap:.5rem;
}
.left-panel-title{
  text-align:center;font-size:var(--fs-xs);font-weight:700;
  color:#374151;background:#f3f4f6;padding:.3rem .5rem;
  border-radius:.25rem;border:1px solid var(--border);
}
.buildings-row{
  display:grid;grid-template-columns:1fr 1fr 1fr;gap:.6rem;flex:1;
}
.building{display:flex;flex-direction:column;align-items:stretch;}

/* triangle roof via clip-path */
.building-roof{
  display:flex;align-items:flex-end;justify-content:center;
  padding:.3rem .2rem .4rem;text-align:center;
  font-size:.6rem;font-weight:700;color:white;line-height:1.2;
  clip-path:polygon(0% 100%, 50% 0%, 100% 100%);
  min-height:44px;
}
.roof-ref   {background:var(--gray);}
.roof-before{background:#b91c1c;}
.roof-target{background:#15803d;}

.building-body{
  border:2px solid;border-radius:0 0 .35rem .35rem;
  padding:.4rem .3rem;display:flex;flex-direction:column;gap:.28rem;flex:1;
}
.body-ref   {border-color:#9ca3af;background:#f9fafb;}
.body-before{border-color:#fca5a5;background:#fff5f5;}
.body-target{border-color:#86efac;background:#f0fdf4;}

.pillar{
  display:flex;align-items:center;justify-content:space-between;
  padding:.22rem .35rem;border-radius:.2rem;
  font-size:.65rem;font-weight:600;line-height:1.2;
}
.p-ref        {background:#e5e7eb;color:#374151;}
.p-before-ok  {background:#fecaca;color:#7f1d1d;}
.p-before-bad {background:#fee2e2;color:#991b1b;border:1px dashed #f87171;}
.p-target     {background:#bbf7d0;color:#14532d;}

.building-label{
  text-align:center;font-size:.62rem;font-weight:700;
  padding:.25rem;margin-top:.25rem;border-radius:.2rem;
}
.bl-ref   {background:#e5e7eb;color:#374151;}
.bl-before{background:#fecaca;color:#7f1d1d;}
.bl-target{background:#bbf7d0;color:#14532d;}

.ref-note{
  font-size:.65rem;color:var(--gray);text-align:center;
  border:1px dashed var(--border);border-radius:.25rem;padding:.3rem;
}

/* ── RIGHT PANEL ── */
.right-panel{
  background:var(--green-dark);border-radius:.5rem;
  padding:.85rem;display:flex;flex-direction:column;gap:.45rem;
}
.right-panel-title{
  text-align:center;font-size:var(--fs-xs);font-weight:700;
  color:white;letter-spacing:.06em;opacity:.9;text-transform:uppercase;
}

/* snake row */
.snake-row{
  display:grid;grid-template-columns:1fr 20px 1fr 20px 1fr;
  gap:.35rem;align-items:center;
}
.step-cell{
  background:white;border-radius:.35rem;padding:.45rem .4rem;
  display:flex;flex-direction:column;gap:.22rem;
}
.step-cell.highlight{border:2px solid var(--yellow);background:var(--yellow-l);}
.photo-ph{
  background:#d1d5db;border-radius:.2rem;
  display:flex;align-items:center;justify-content:center;
  font-size:.6rem;color:var(--gray);font-style:italic;
  min-height:56px;flex:1;
}
.step-cell.highlight .photo-ph{background:#fde68a;}
.step-num{font-size:.62rem;font-weight:700;color:var(--blue);}
.step-cell.highlight .step-num{color:#92400e;}
.step-lbl{font-size:.63rem;color:#1f2937;line-height:1.3;font-weight:600;}
.step-sub{font-size:.58rem;color:var(--gray);font-weight:400;line-height:1.2;}

.arr{
  display:flex;align-items:center;justify-content:center;
  color:white;font-size:1.1rem;font-weight:900;
}
.arr-down-row{
  display:flex;justify-content:flex-end;
  padding-right:calc(33.33% + .35rem + 20px);
  line-height:1;
}
.arr-down-row.left-side{
  justify-content:flex-start;
  padding-right:0;
  padding-left:calc(0% + .0rem);
}

/* PRINT */
@media print{
  @page{size:A3 landscape;margin:1cm;}
  body{background:white;padding:.5cm;}
  .toolbar{display:none;}
  .header-bar,.slide-title,.two-panel{max-width:100%;}
  .right-panel{-webkit-print-color-adjust:exact;print-color-adjust:exact;}
  .building-roof{-webkit-print-color-adjust:exact;print-color-adjust:exact;}
}
@media(max-width:1100px){
  .two-panel{grid-template-columns:1fr;}
}
</style>
</head>
<body>

<!-- HEADER -->
<div class="header-bar">
  <div class="logo">PERTAMINA<small>PT Kilang Pertamina Internasional</small></div>
  <div class="header-title">
    <h1>Flow Proses Solusi Terpilih — HC Portal KPB</h1>
    <small>PCP SMART 2026 · §3.4 · Opsi V · Flow Proses Implementasi</small>
  </div>
  <div class="pcp-badge">PCP SMART<small>2026</small></div>
</div>

<!-- TOOLBAR -->
<div class="toolbar">
  <span class="note">§3.4 Solusi Terpilih · Format: 2-panel (Sebelum/Target + Flow Implementasi)</span>
  <button onclick="window.print()">🖨️ Print / Save PDF</button>
</div>

<!-- SLIDE TITLE -->
<div class="slide-title">Flow Proses Solusi Terpilih</div>

<!-- TWO-PANEL -->
<div class="two-panel">

  <!-- LEFT PANEL: 3 BUILDINGS -->
  <div class="left-panel">
    <div class="left-panel-title">Workflow Pengelolaan Kompetensi Pekerja — CSU Process KPB</div>

    <div class="buildings-row">

      <!-- Building 1: REF -->
      <div class="building">
        <div class="building-roof roof-ref">PEDOMAN<br>CAP BUILDING</div>
        <div class="building-body body-ref">
          <div class="pillar p-ref">CMP</div>
          <div class="pillar p-ref">CDP</div>
          <div class="pillar p-ref">BP</div>
          <div class="pillar p-ref">Assessment</div>
          <div class="pillar p-ref">OJT</div>
          <div class="pillar p-ref">Laporan</div>
          <div class="pillar p-ref">Admin</div>
        </div>
        <div class="building-label bl-ref">Ref: Pedoman Cap Building</div>
      </div>

      <!-- Building 2: SEBELUM -->
      <div class="building">
        <div class="building-roof roof-before">SEBELUM<br>INOVASI</div>
        <div class="building-body body-before">
          <div class="pillar p-before-ok"><span>CMP</span><span>✓</span></div>
          <div class="pillar p-before-ok"><span>CDP</span><span>✓</span></div>
          <div class="pillar p-before-ok"><span>BP</span><span>✓</span></div>
          <div class="pillar p-before-bad"><span>Assessment</span><span>❌</span></div>
          <div class="pillar p-before-bad"><span>OJT</span><span>❌</span></div>
          <div class="pillar p-before-bad"><span>Laporan</span><span>❌</span></div>
          <div class="pillar p-before-bad"><span>Admin</span><span>❌</span></div>
        </div>
        <div class="building-label bl-before">Tools Manual Tersebar</div>
      </div>

      <!-- Building 3: TARGET -->
      <div class="building">
        <div class="building-roof roof-target">TARGET</div>
        <div class="building-body body-target">
          <div class="pillar p-target"><span>CMP</span><span>✓</span></div>
          <div class="pillar p-target"><span>CDP</span><span>✓</span></div>
          <div class="pillar p-target"><span>BP</span><span>✓</span></div>
          <div class="pillar p-target"><span>Assessment</span><span>✓</span></div>
          <div class="pillar p-target"><span>OJT</span><span>✓</span></div>
          <div class="pillar p-target"><span>Laporan</span><span>✓</span></div>
          <div class="pillar p-target"><span>Admin</span><span>✓</span></div>
        </div>
        <div class="building-label bl-target">HC Portal Terintegrasi</div>
      </div>

    </div>

    <div class="ref-note">
      [Lampiran: data monitoring kompetensi pekerja CSU Process KPB]
    </div>
  </div>

  <!-- RIGHT PANEL: 9-STEP SNAKE -->
  <div class="right-panel">
    <div class="right-panel-title">Flow Proses Implementasi Portal HC KPB</div>

    <!-- Row 1: Step 1 → 2 → 3 -->
    <div class="snake-row">
      <div class="step-cell">
        <div class="step-num">Step 1</div>
        <div class="photo-ph">[ foto / icon ]</div>
        <div class="step-lbl">Idea</div>
        <div class="step-sub">Identifikasi gap workflow manual</div>
      </div>
      <div class="arr">→</div>
      <div class="step-cell">
        <div class="step-num">Step 2</div>
        <div class="photo-ph">[ screenshot portal / foto dev ]</div>
        <div class="step-lbl">Development</div>
        <div class="step-sub">Pembangunan Portal HC</div>
      </div>
      <div class="arr">→</div>
      <div class="step-cell">
        <div class="step-num">Step 3</div>
        <div class="photo-ph">[ screenshot / foto input data ]</div>
        <div class="step-lbl">Penyusunan &amp; Pengisian Data Pekerja ke Web</div>
      </div>
    </div>

    <!-- Down arrow: right side (below step 3) -->
    <div class="arr-down-row">
      <span style="color:white;font-size:1.2rem;font-weight:900">↓</span>
    </div>

    <!-- Row 2: Step 6 ← 5 ← 4 -->
    <div class="snake-row">
      <div class="step-cell">
        <div class="step-num">Step 6</div>
        <div class="photo-ph">[ foto trial assessment ]</div>
        <div class="step-lbl">Trial Assessment Proton</div>
      </div>
      <div class="arr">←</div>
      <div class="step-cell">
        <div class="step-num">Step 5</div>
        <div class="photo-ph">[ foto penetapan coach/coachee ]</div>
        <div class="step-lbl">Trial Proton</div>
        <div class="step-sub">Penetapan Coach &amp; Coachee</div>
      </div>
      <div class="arr">←</div>
      <div class="step-cell">
        <div class="step-num">Step 4</div>
        <div class="photo-ph">[ foto sosialisasi internal ]</div>
        <div class="step-lbl">Sosialisasi Team HC Internal</div>
      </div>
    </div>

    <!-- Down arrow: left side (below step 6) -->
    <div class="arr-down-row left-side">
      <span style="color:white;font-size:1.2rem;font-weight:900">↓</span>
    </div>

    <!-- Row 3: Step 7 → 8 → 9 -->
    <div class="snake-row">
      <div class="step-cell">
        <div class="step-num">Step 7</div>
        <div class="photo-ph">[ foto / screenshot assessment ]</div>
        <div class="step-lbl">First Assessment</div>
        <div class="step-sub">Pre-Post Test</div>
      </div>
      <div class="arr">→</div>
      <div class="step-cell">
        <div class="step-num">Step 8</div>
        <div class="photo-ph">[ foto / dokumen TKI ]</div>
        <div class="step-lbl">Penyusunan TKI</div>
      </div>
      <div class="arr">→</div>
      <div class="step-cell highlight">
        <div class="step-num">Step 9 ★</div>
        <div class="photo-ph">[ foto Kick Off Meeting ]</div>
        <div class="step-lbl">Kick Off Meeting Proton</div>
        <div class="step-sub">Launch Resmi Program</div>
      </div>
    </div>

  </div>
</div>

</body>
</html>
```

- [ ] **Step 2: Verifikasi di browser**

Buka `docs/pcp-HCPortal-2026/slide8-risalah/flow-proses-solusi-terpilih.html` di browser. Cek:
- 2 panel tampil side-by-side
- 3 bangunan dengan atap segitiga clip-path terlihat
- Snake row 1 → row 2 ← row 3 → dengan panah putih
- Step 9 ter-highlight kuning

- [ ] **Step 3: Commit file baru**

```bash
git add docs/pcp-HCPortal-2026/slide8-risalah/flow-proses-solusi-terpilih.html
git commit -m "feat(slide8-flow-proses): add Opsi V 2-panel flow proses solusi terpilih HC Portal"
```

---

### Task 2: Update index.html — tambah card Opsi V

**Files:**
- Modify: `docs/pcp-HCPortal-2026/slide8-risalah/index.html`

- [ ] **Step 1: Tambah CSS class `opsi-v` ke `<style>` block**

Di `index.html`, di dalam `<style>`, setelah `.card.opsi-iv .tag { ... }` (sekitar baris 30), tambahkan:

```css
  .card.opsi-v { border-top: 6px solid var(--pertamina-red); }
  .card.opsi-v .tag { background: var(--pertamina-red); color: white; }
```

- [ ] **Step 2: Tambah card Opsi V di section cards utama**

Cari `<div class="cards">` pertama (sebelum Opsi II card), ubah grid jadi 3 kolom lalu tambah card ketiga. Ganti:

```html
  <div class="cards">
    <a href="pipeline-outcome.html" class="card opsi-ii">
```

jadi:

```html
  <div class="cards" style="grid-template-columns:1fr 1fr 1fr;">
    <a href="flow-proses-solusi-terpilih.html" class="card opsi-v">
      <div class="tag">OPSI V — BARU</div>
      <h2>Opsi V — Flow Proses Solusi Terpilih</h2>
      <p>2-panel: 3 bangunan pilar sebelum/target + 9-step flow implementasi snake clockwise</p>
      <ul>
        <li><b>Panel Kiri:</b> Ref Pedoman Cap Building → Sebelum (3✓ 4❌) → Target (7✓)</li>
        <li><b>Panel Kanan:</b> 9 step implementasi (Idea → Kick Off Meeting)</li>
        <li><b>Format:</b> Gaya referensi PCP — foto/screenshot per step</li>
        <li><b>Audience:</b> PCP reviewer, manajemen HC</li>
      </ul>
    </a>
    <a href="pipeline-outcome.html" class="card opsi-ii">
```

- [ ] **Step 3: Update hero subtitle**

Cari:
```html
    <p>PCP SMART 2026 · §3.4 · v1.0 · 2 versi alternatif</p>
```
Ganti:
```html
    <p>PCP SMART 2026 · §3.4 · v2.0 · 3 versi alternatif</p>
```

- [ ] **Step 4: Verifikasi di browser**

Buka `index.html` di browser. Cek:
- 3 card tampil (Opsi V merah, Opsi II hijau, Opsi IV biru)
- Klik Opsi V → buka `flow-proses-solusi-terpilih.html`

- [ ] **Step 5: Commit**

```bash
git add docs/pcp-HCPortal-2026/slide8-risalah/index.html
git commit -m "feat(slide8-index): add Opsi V card to index, update v1.0→v2.0"
```

---

## Self-Review

**Spec coverage:**
- [x] 2-panel landscape format → Task 1 HTML
- [x] Panel kiri: 3 bangunan (Ref/Sebelum/Target), 7 pilar → Task 1 left-panel
- [x] Panel kanan: 9-step snake clockwise → Task 1 right-panel
- [x] Foto = placeholder → photo-ph gray box
- [x] Pedoman Cap Building label → bl-ref class
- [x] Step 9 highlight kuning → `.step-cell.highlight`
- [x] Print A3 landscape → @media print
- [x] Pertamina color palette → :root CSS vars
- [x] Update index.html → Task 2
- [x] Tidak modify Opsi II/IV → hanya tambah card baru

**Placeholder scan:** Tidak ada TBD/TODO di plan. Semua code lengkap.

**Type consistency:** CSS class names konsisten: `.step-cell`, `.snake-row`, `.arr`, `.building-body`, `.pillar` — dipakai konsisten di Task 1 saja (Task 2 tidak menyentuh kelas ini).
