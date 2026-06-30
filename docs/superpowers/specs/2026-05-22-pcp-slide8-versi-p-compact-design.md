# Design Spec: PCP Slide 8 — Versi P Compact (Diagram Solusi Terpilih)

**Tanggal:** 2026-05-22
**Penulis:** Rino A P (via brainstorming session)
**Status:** Approved (awaiting written-spec review gate)
**Konteks:** PCP SMART 2026 APQ — Risalah Web.pptx slide 8, placeholder #1 "Design / Gambar Teknik / Flow Proses / Formula Solusi Terpilih"

---

## 1. Tujuan

Membuat varian compact HTML dari `versi-p-workflow-topology.html` (v3.7 master) yang dapat di-export sebagai PNG dan di-insert ke area "GAMBAR DESAIN OLEH RINO" pada slide 8 Risalah Web.pptx, **tanpa memodifikasi** file master Versi P existing.

Slide 8 punya 3 placeholder paralel (Gambar Desain dominan top, Rencana Pembuatan kiri-bawah, Standard Design kanan-bawah). Spec ini fokus **hanya placeholder #1 (Gambar Desain)**. Placeholder #2 dan #3 di-handle spec terpisah.

## 2. Audience + Use Case

| Audience | Konteks |
|---|---|
| Reviewer PCP Pertamina | Lihat slide 8 sebagai bagian submission PCP STEP 3 "Solusi Terpilih" |
| Management HC + Engineering Ops | Pahami transformasi workflow dari multi-tools manual ke single portal terintegrasi |
| Self (penulis PCP) | Sumber gambar yang reusable + maintainable; bukan one-off PNG static |

**Bukan untuk:** reviewer detail teknik full (gunakan `versi-p-workflow-topology.html` master), executive showcase (gunakan Versi C), atau BPM/Lean reviewer (gunakan Versi X/Z).

## 3. Area Slide 8 + Dimensi Target

Slide PPT 16:9 (~1600×900 px @hi-res). Area placeholder "GAMBAR DESAIN" inner box:
- Width: ~1100-1150 px (dominan, landscape wide)
- Height: ~420-450 px (terbatas)
- Aspect ratio: ~2.5:1 landscape

**HTML render target:**
- Body width fixed `1100px`, height auto (~440-460 px)
- Retina export `@2x` scale → effective 2200×880 px PNG → crisp di slide projector + print

## 4. Layout: Side-by-Side Horizontal

```
+--------- 1100px wide -----------+
|  SEBELUM (545px)  | SESUDAH (545px) |
|  border-top RED   | border-top GREEN |
|                   |                  |
|  Title row        | Title row        |
|  L5 Manajemen     | L5 Manajemen     |
|  L4 HC            | L4 HC            |
|  [NO-BUFFER ▔▔]   | [BUFFER ZONE ★]  |
|  L3 Atasan        | L3 Atasan        |
|  L2 Coach         | L2 Coach         |
|  L1 Pekerja       | L1 Pekerja       |
+----------------------------------+
```

Gap antar panel: 8px. Total 545+8+545 = 1098 px.

## 5. Konten Per Panel (Symmetric 6 Content Row + 1 Title)

Sumber data: `versi-p-workflow-topology.html` lines 168-369. Marker A-F (issue) + 1-7 (improvement) **dipertahankan visual saja**, tooltip text di-drop karena PNG static.

### Panel SEBELUM (kiri)

| Row | Layer | Aktor + Komponen | Marker |
|---|---|---|---|
| Title | — | `❌ SEBELUM (Kondisi Aktual)` font 13px bold red | — |
| L5 Strategic | 👔 Manajemen | 📄 Laporan PDF/Excel, 📧 Email Pertamina | D |
| L4 Governance | 👤 HC | 📊 Excel Master Pekerja, Assessment, Training, KKJ, Sertifikat + 📝 Word Template (wrap 2 baris) | A, B |
| **Buffer slot** | **(no-buffer spacer)** | Gray dashed border, italic gray text: *"Tidak ada hub terintegrasi"* | — |
| L3 Supervisory | 🏢 Atasan | 📧 Email kotak masuk, 💬 WhatsApp approval lisan | C, E |
| L2 Coaching | 🧑‍🏫 Coach | 📋 Form PROTON cetak, 📁 Arsip fisik, 💬 WhatsApp (bukti foto), 📧 Email (lampiran) | A, E |
| L1 Operational | 👷 Pekerja | 🌐 FleQi Quiz (eksternal), 🎓 Sertifikat hardcopy, 📊 Excel pribadi (IDP) | A, F |

### Panel SESUDAH (kanan)

| Row | Layer | Aktor + Komponen | Marker |
|---|---|---|---|
| Title | — | `✅ SESUDAH (Konsep HC Portal)` font 13px bold blue | — |
| L5 Strategic | 👔 Manajemen | 📈 Analytics Dashboard, 🔥 Heatmap Gap Kompetensi, 📤 Export Excel/PDF | 1 |
| L4 Governance | 👤 HC | 👥 Kelola Pekerja, 🎯 PROTON Data (IDP), 📝 Kelola Paket Assessment, 📊 Kelola KKJ, 🔄 Renewal Certificate, 🔍 Audit Log (wrap 2 baris) | 2, 3 |
| **Buffer zone** | **🛡️ BUFFER ZONE — 🌐 HC PORTAL** | Gradient blue→green hero, label besar tengah: `🌐 HC PORTAL — Single Source of Truth`. Tech stack subtitle DROP (pindah ke spec panel #3 Rencana Pembuatan) | 4 |
| L3 Supervisory | 🏢 Atasan | 👀 Records Team, ✅ Approval Deliverable, 📊 View Matriks KKJ Bagian | 5 |
| L2 Coaching | 🧑‍🏫 Coach | 🎯 Coaching PROTON (5 fase), 📎 Upload Evidence, 📜 Histori PROTON | 6 |
| L1 Operational | 👷 Pekerja | 📝 Assessment Online, 📋 Plan IDP, 🏆 Certificate Download, 🔔 Notifikasi In-App | 7 |

### Tinggi row symmetric

Sebelum & Sesudah 7 row (title + 6 content). Tinggi panel sama untuk visual align. Buffer slot Sebelum (gray dashed) = posisi + tinggi sama dgn buffer Sesudah → asymmetry resolved.

## 6. Styling Spec

### Token (re-use dari master v3.7)

```css
--pertamina-red: #C8102E;
--pertamina-red-light: #fce8eb;
--pertamina-blue: #00558C;
--pertamina-blue-dark: #003D63;
--pertamina-blue-light: #e6f0f7;
--pertamina-green: #00A551;
--pertamina-green-light: #d4f0dd;
--pertamina-yellow: #FFC72C;
--neutral-gray: #6b7280;
--neutral-light: #d1d5db;
--bg: #f6f7fb;
--hub-grad: linear-gradient(135deg, #00558C, #00A551);

/* Compact-specific scale (override master) */
--fs-xxs: 0.5rem;   /* 8px */
--fs-xs: 0.55rem;   /* 8.8px - comp box content */
--fs-sm: 0.65rem;   /* 10.4px - layer actor label */
--fs-base: 0.75rem; /* 12px - buffer zone hero */
--fs-title: 0.85rem;/* 13.6px - panel title */
```

### Komponen

| Komponen | Style |
|---|---|
| Body | `width: 1100px`, padding 0, margin 0, bg `--bg` |
| Diagram wrap (per panel) | width 545px, bg white, border-radius .5rem, border-top 4px solid (red/green), padding 8px, box-shadow 0 2px 8px rgba(0,0,0,.06) |
| Panel title | font-size `--fs-title`, font-weight 700, margin-bottom 6px, color sebelum=red / sesudah=blue |
| Layer row | grid 90px 1fr, gap 4px, min-height ~50px, border-bottom 1px dashed `--neutral-light` |
| Layer label | font-size `--fs-sm`, bold, kolom kiri, format: `<icon> <Actor>` + small "Lv N" subtitle. Padding 2px 4px |
| Layer content | flex-wrap, gap 3px, padding 2px 4px, align-items center |
| Comp box | padding 2px 4px, border 1px solid, border-radius 3px, font-size `--fs-xs`, white-space nowrap. Color variant: `.manual` red-light, `.tool-ext` yellow-light, `.paper` orange-light, `.portal` blue-light |
| Marker | inline 12px circle, font-size `--fs-xxs`, bold white. `.issue` bg red, `.improvement` bg green |
| Buffer zone Sesudah | full panel width, bg `--hub-grad`, color white, padding 6px 8px, border-radius 4px, height ~50px. Label center: `🌐 HC PORTAL — Single Source of Truth` font `--fs-base` bold |
| Buffer slot Sebelum (no-buffer) | full panel width, border 1.5px dashed `--neutral-light`, bg transparent, padding 6px 8px, height ~50px. Label center: `— Tidak ada hub terintegrasi —` font `--fs-sm` italic `--neutral-gray` |

### Density handling Layer 4 HC (6 box per panel)

Layer L4 padat (6 comp box + 2 marker). Solusi:
- `flex-wrap: wrap` di `.layer-content`
- Comp box `max-width: 130px`, `white-space: nowrap` (kalau terlalu panjang, allow ellipsis)
- Padding row L4 extra 2px atas+bawah
- Effective: 6 box wrap ke 2 baris dalam ~55-60px tinggi row

## 7. Export Workflow

### Tombol bawaan HTML

Top-right corner (absolute position, drop saat print/screenshot):

```html
<button id="exportBtn" onclick="exportPNG()">📸 Export PNG</button>
```

Pakai **html2canvas** CDN (`https://cdn.jsdelivr.net/npm/html2canvas@1.4.1/dist/html2canvas.min.js`):

```javascript
async function exportPNG() {
  const target = document.getElementById('diagram-row');
  const canvas = await html2canvas(target, {
    scale: 2,            // retina @2x
    backgroundColor: '#ffffff',
    useCORS: true
  });
  const link = document.createElement('a');
  link.download = 'versi-p-slide8.png';
  link.href = canvas.toDataURL('image/png');
  link.click();
}
```

Tombol di-hide via CSS `@media print` + class `.export-hidden` saat dipakai untuk screenshot manual.

### Manual fallback

Chrome DevTools → inspect `#diagram-row` → ⋯ menu → `Capture node screenshot`. Atau headless CLI:

```bash
chrome --headless --screenshot=versi-p-slide8.png --window-size=2200,920 --device-scale-factor=2 file:///path/to/versi-p-compact.html
```

## 8. Lokasi File + Naming

```
docs/pcp-HCPortal-2026/3.4-solusi-terpilih/
├── versi-p-workflow-topology.html   (master v3.7 — UNTOUCHED)
├── slide8/                          (subfolder baru)
│   ├── versi-p-compact.html         (file baru, output spec ini)
│   └── exports/
│       └── versi-p-slide8.png       (hasil export, gitignored optional)
└── ...
```

Sub-folder `slide8/` reserved untuk varian PPT-specific. Jika nanti placeholder #2 (Rencana Pembuatan) dan #3 (Standard Design) juga di-HTML-kan compact, masuk subfolder sama.

## 9. Yang Di-DROP (vs master v3.7)

| Komponen | Alasan |
|---|---|
| `.header-bar` (Pertamina logo + title + PCP badge) | Slide PPT sudah punya header sendiri |
| `.meta-bar` (Domain + Tujuan) | Konteks sudah tertulis di slide title strip |
| `.toolbar` (Print button) | Diganti tombol Export PNG corner |
| `.transformation-arrow` ▼ | Layout side-by-side tidak butuh; border color + title sudah cukup signaling |
| `.komparasi-section` (Tabel Komparasi Aspek) | Di luar scope slide 8 placeholder #1 |
| `<table class="legend-table">` Issue A-F + Improvement 1-7 | Marker visual saja cukup di compact; legend full di master v3.7 |
| Tech stack subtitle di buffer zone (`ASP.NET Core 8 • SQL Server • ...`) | Pindah ke spec panel #3 Rencana Pembuatan |
| Marker tooltip (`title="..."`) | PNG static, tooltip non-functional |
| Hover transition + box-shadow animation | Static export, irrelevant |

## 10. Recovery + Versioning

- File baru, tidak merge ke master Versi P
- Setelah ship + screenshot inserted ke pptx, commit dengan message: `feat(pcp-slide8-v1.0): versi P compact edition untuk Risalah Web slide 8`
- Tag: `pcp-hcportal-3.4-slide8-v1.0` (allow iterasi v1.1, v1.2 untuk refinement)
- Master Versi P v3.7 tetap intact di `versi-p-workflow-topology.html`

## 11. Test + Verify Checklist

Sebelum claim done:
- [ ] HTML render bersih di Chrome (no console error)
- [ ] Width body exact 1100px (DevTools verify)
- [ ] Tombol Export PNG download file `versi-p-slide8.png` dimensi ~2200×880
- [ ] PNG di-insert ke slide 8 fit-to-box tanpa stretch/distort
- [ ] Marker A-F + 1-7 visible (warna red issue, green improvement)
- [ ] Buffer zone Sesudah jelas hub center, gradient blue→green readable
- [ ] Buffer slot Sebelum (no-buffer spacer) align tinggi dgn buffer Sesudah
- [ ] Density Level 4 HC 6 box wrap rapi, tidak overflow
- [ ] Color comp box: manual=red-light, tool-ext=yellow-light, paper=orange-light, portal=blue-light
- [ ] Font readable saat slide projected (full-screen test)

## 12. Out of Scope

- ❌ Modify master `versi-p-workflow-topology.html` v3.7
- ❌ Spec panel #2 (Rencana Pembuatan) — terpisah
- ❌ Spec panel #3 (Standard Design) — terpisah
- ❌ Print stylesheet A3 landscape (master v3.7 sudah cover full-detail print)
- ❌ Responsive breakpoints (slide 8 = single fixed dimension)
- ❌ Automation pipeline pptx ↔ HTML sync (manual insert PNG OK)

## 13. Referensi

- Master Versi P v3.7: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html`
- README 4 versi: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md`
- Reference PCP page 8 (IT/OT DMZ): `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/pendukung/reference-pcp-page8.png`
- Risalah Web.pptx slide 8: `docs/pcp-HCPortal-2026/Risalah Web.pptx`
- Spec design master v3 (Versi P+C): `docs/superpowers/specs/2026-05-21-pcp-hcportal-3.4-v3-design.md`
- Spec design v3.6 (Versi X+Z): `docs/superpowers/specs/2026-05-22-pcp-hcportal-3.4-versi-x-z-design.md`
