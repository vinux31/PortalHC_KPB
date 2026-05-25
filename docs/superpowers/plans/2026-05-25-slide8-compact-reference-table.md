# Slide 8 Compact — Reference & Standard Table Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tambahkan section tabel referensi & standar (X2 grouped, 4 kategori, 12 baris) di `index.html` bagian compact, antara compact cards dan audience matrix.

**Architecture:** Single HTML edit — insert satu `<div class="reference-matrix">` block setelah compact cards `</div>` (line 93) dan sebelum `<div class="audience-matrix">` (line 95). Gunakan CSS class baru `.reference-matrix` dengan style inline minimal, ikuti token warna existing.

**Tech Stack:** HTML, CSS inline (existing token vars), tidak ada JS.

---

### Task 1: Insert Section Tabel Referensi di index.html

**Files:**
- Modify: `docs/pcp-HCPortal-2026/slide8-risalah/index.html:93-95`

- [ ] **Step 1: Tambahkan CSS class `.reference-matrix` ke `<style>` block (sebelum `</style>` di line 44)**

Buka file. Tepat sebelum `@media (max-width: 900px)` (line 41), insert:

```css
  .reference-matrix { background: white; padding: 1.5rem; border-radius: .75rem; box-shadow: 0 2px 12px rgba(0,0,0,.05); margin-bottom: 1.5rem; }
  .reference-matrix h3 { margin: 0 0 .35rem; color: var(--pertamina-blue); }
  .reference-matrix .ref-subtitle { margin: 0 0 1rem; font-size: .85rem; color: var(--neutral-gray); }
  .reference-matrix table { width: 100%; border-collapse: collapse; font-size: .85rem; }
  .reference-matrix th { background: var(--pertamina-blue); color: white; text-align: left; padding: .5rem .75rem; }
  .reference-matrix th.center { text-align: center; }
  .reference-matrix td { padding: .4rem .75rem; border-bottom: 1px solid #e5e7eb; }
  .reference-matrix td.center { text-align: center; }
  .reference-matrix td.indented { padding-left: 1.25rem; }
  .reference-matrix tr.group-header td { background: #dbeafe; font-weight: 700; font-size: .75rem; color: #1d4ed8; text-transform: uppercase; letter-spacing: .06em; padding: .35rem .75rem; border-bottom: none; }
  .reference-matrix tr.stripe { background: #f9fafb; }
  .reference-matrix .ref-ok { color: #15803d; font-weight: 600; }
  .reference-matrix .ref-na { color: #9ca3af; }
  .reference-matrix .ref-desc { display: block; font-size: .75rem; color: var(--neutral-gray); font-weight: 400; }
  .reference-matrix .ref-note { margin: .6rem 0 0; font-size: .78rem; color: #9ca3af; }
```

- [ ] **Step 2: Insert HTML section antara compact cards dan audience-matrix (antara line 93 dan 95)**

Tepat setelah `</div>` penutup compact cards (setelah `</div>` di line 93, sebelum blank line menuju `<div class="audience-matrix">`), insert:

```html
  <div class="reference-matrix">
    <h3>Basis Referensi &amp; Standar — Compact v1.1</h3>
    <p class="ref-subtitle">Kedua versi compact dibangun di atas landasan yang sama. Kolom menunjukkan peran masing-masing referensi per opsi.</p>
    <table>
      <thead>
        <tr>
          <th style="width:52%">Referensi / Standar</th>
          <th class="center" style="width:24%">Opsi II<br><span style="font-weight:400;opacity:.85;font-size:.78rem">Pipeline Outcome</span></th>
          <th class="center" style="width:24%">Opsi IV<br><span style="font-weight:400;opacity:.85;font-size:.78rem">Workflow Topology</span></th>
        </tr>
      </thead>
      <tbody>
        <tr class="group-header"><td colspan="3">📚 Akademik</td></tr>
        <tr>
          <td class="indented">Ogoun &amp; Tamunosiki-Amadi (2023)<span class="ref-desc">Competence Monitoring Pipeline · Spearman R</span></td>
          <td class="center ref-ok">✅ PRIMARY</td>
          <td class="center ref-ok">✅ callout</td>
        </tr>
        <tr class="stripe">
          <td class="indented">Ellström &amp; Kock (2008)<span class="ref-desc">Competence Development framework</span></td>
          <td class="center ref-na">—</td>
          <td class="center ref-ok">✅ footer</td>
        </tr>
        <tr>
          <td class="indented">Korelasi Jurnal &amp; PPT KPB (RL1) + Laporan Kuantitatif V2 (RL2)<span class="ref-desc">Data korelasi internal KPB</span></td>
          <td class="center ref-ok">✅ formula</td>
          <td class="center ref-ok">✅ formula</td>
        </tr>

        <tr class="group-header"><td colspan="3">🌐 Standar Eksternal</td></tr>
        <tr class="stripe">
          <td class="indented">ISO/IEC 27001:2022<span class="ref-desc">Information Security Management</span></td>
          <td class="center ref-ok">✅</td>
          <td class="center ref-ok">✅</td>
        </tr>
        <tr>
          <td class="indented">OWASP Top 10 (2021) + ASVS 4.0.3<span class="ref-desc">Application Security Verification</span></td>
          <td class="center ref-ok">✅</td>
          <td class="center ref-ok">✅</td>
        </tr>
        <tr class="stripe">
          <td class="indented">WCAG 2.2 (W3C, 2023)<span class="ref-desc">Web Content Accessibility</span></td>
          <td class="center ref-ok">✅</td>
          <td class="center ref-ok">✅</td>
        </tr>

        <tr class="group-header"><td colspan="3">🏢 Standar Internal Pertamina</td></tr>
        <tr>
          <td class="indented">Pedoman Kompetensi Teknis A5.2-01/K20000/2025/S9 (SI1)<span class="ref-desc">Pedoman Kompetensi Teknis KPB</span></td>
          <td class="center ref-ok">✅</td>
          <td class="center ref-ok">✅</td>
        </tr>
        <tr class="stripe">
          <td class="indented">TKO B5.3-04/K20100/2025-S9 (SI2)<span class="ref-desc">Tata Kerja Operasi Coaching &amp; Mentoring</span></td>
          <td class="center ref-ok">✅</td>
          <td class="center ref-ok">✅</td>
        </tr>
        <tr>
          <td class="indented">Kamus Direktori Kompetensi Teknis Pertamina (SI3)<span class="ref-desc">Direktori Kompetensi Teknis</span></td>
          <td class="center ref-ok">✅</td>
          <td class="center ref-ok">✅</td>
        </tr>

        <tr class="group-header"><td colspan="3">🔢 Formula &amp; Data Internal</td></tr>
        <tr class="stripe">
          <td class="indented">Risalah Inovasi PROTON — Fishbone + FMEA (P1)<span class="ref-desc">Issue codes A-F + Improvement 1-7</span></td>
          <td class="center ref-ok">✅ Issue codes</td>
          <td class="center ref-ok">✅ Issue codes</td>
        </tr>
        <tr>
          <td class="indented">Risalah Panca Mutu (P2)<span class="ref-desc">Formula R → Time, Innov, Alert mapping</span></td>
          <td class="center ref-ok">✅ formula</td>
          <td class="center ref-ok">✅ formula</td>
        </tr>
      </tbody>
    </table>
    <p class="ref-note">* ✅ PRIMARY = konten utama slide. ✅ callout = disebut di callout box. ✅ footer = credit di footer.</p>
  </div>
```

- [ ] **Step 3: Verifikasi visual di browser**

Buka `docs/pcp-HCPortal-2026/slide8-risalah/index.html` langsung di browser (file://).

Cek:
- Section muncul di antara compact cards dan Audience Matrix
- 4 group header tampil biru muda (`#dbeafe`)
- 12 baris data lengkap
- Kolom Opsi II / Opsi IV center-aligned
- ✅ hijau, — abu-abu
- Stripe zebra (`#f9fafb`) pada baris genap
- Sub-deskripsi tiap referensi tampil font kecil abu
- Footer note muncul di bawah tabel
- Tidak ada scroll horizontal di desktop

- [ ] **Step 4: Commit**

```bash
git add docs/pcp-HCPortal-2026/slide8-risalah/index.html
git commit -m "feat(slide8): tambah tabel referensi & standar X2-grouped di section compact index.html"
```
