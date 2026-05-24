# Design Spec — Slide 8 Risalah Web Compact (Opsi II + IV) v1.1

> **Tanggal:** 2026-05-24
> **Konteks:** PCP SMART 2026 §3.4, Risalah Web.pptx slide 8 — compact format pas masuk kotak 8,11 × 25,72 cm PPT
> **Output:** 2 HTML mockup + PNG export 300 DPI untuk drop-in PowerPoint
> **Folder target:** `docs/pcp-HCPortal-2026/slide8-risalah/compact/`
> **Parent spec:** `2026-05-24-slide8-risalah-opsi-ii-iv-design.md` (v1.0 full-detail)

## 1. Latar Belakang

Existing slide8-risalah v1.0 (`pipeline-outcome.html` + `workflow-topology-refined.html`) adalah full-polish landscape A3 mockup — bagus untuk reference, tapi tidak pas paste langsung ke kotak konten slide 8 PPT yang dimensinya **8,11 cm tinggi × 25,72 cm lebar** (di bawah bar judul hijau "Design / Gambar Teknik / Flow Proses / Formula Solusi Terpilih").

Spec ini menghasilkan **compact version** v1.1 yang spesifik fits kotak PPT tersebut (aspect 3.17:1, sangat wide-pendek), pakai approach β (Diagram-Focused — hanya 2 panel side-by-side, tabel/standar/coret PISAH ke area lain slide).

## 2. Output

### File structure
```
docs/pcp-HCPortal-2026/slide8-risalah/compact/
├─ pipeline-outcome-compact.html               (Opsi II)
├─ pipeline-outcome-compact.png                (PNG 3035 × 957 ≈ 300 DPI)
├─ workflow-topology-refined-compact.html      (Opsi IV)
└─ workflow-topology-refined-compact.png       (PNG 3035 × 957)
```

### Dimensions
- **Logical:** 25,72 cm × 8,11 cm (target PPT box)
- **Aspect ratio:** 3.17 : 1 (wide pendek)
- **Viewport export:** 3035 × 957 px (300 DPI equivalent)
- **CSS body:** `width: 100vw; height: 100vh; overflow: hidden`
- **Print page:** `@page { size: 25.72cm 8.11cm; margin: 0 }`

### Title bar
Bar judul hijau 0,9 × 14,5 cm ("Design / Gambar Teknik / Flow Proses / Formula Solusi Terpilih") **TIDAK include** di HTML — handled di PPT terpisah.

## 3. Approach β — Diagram-Focused

Kedua opsi pakai layout **2-panel side-by-side** (kiri Sebelum, kanan Sesudah/Solusi Terpilih). Hanya konten visual (diagram + markers + legend inset). Tidak ada tabel/standar/coret di dalam kotak — semua itu pindah ke area lain slide PPT.

## 4. Common HTML/CSS Shell (shared)

### Color palette (5 token, identik v1.0)
```css
:root {
  --pertamina-red: #C8102E;
  --pertamina-red-light: #fce8eb;
  --pertamina-blue: #00558C;
  --pertamina-blue-dark: #003D63;
  --pertamina-blue-light: #e6f0f7;
  --pertamina-green: #00A551;
  --pertamina-green-light: #d4f0dd;
  --pertamina-yellow: #FFC72C;
  --neutral-gray: #6b7280;
  --bg: #f6f7fb;
  --border: #d1d5db;
  --hub-grad: linear-gradient(135deg, #00558C, #00A551);
}
```

### Compact font scale (lebih kecil dari v1.0)
```css
:root {
  --fs-xs: 9px;
  --fs-sm: 11px;
  --fs-base: 13px;
  --fs-md: 15px;
  --fs-lg: 18px;
}
```
Floor 9px utk readable @ 25,72 cm wide saat printed @ 300 DPI.

### Layout shell
```css
body { width: 100vw; height: 100vh; margin: 0; padding: 0; background: white;
       font-family: -apple-system, "Segoe UI", Roboto, sans-serif; color: #1f2937;
       overflow: hidden; box-sizing: border-box; }
.compact-container {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5vw;
  width: 100%; height: 100%;
  padding: 0.3vw 0.5vw;
}
.panel {
  border: 2px solid;
  border-radius: 6px;
  padding: 0.4vw 0.6vw;
  display: flex; flex-direction: column;
  overflow: hidden;
  position: relative;
}
.panel-sebelum { border-color: var(--pertamina-red);
                  background: linear-gradient(180deg, #fff5f5 0%, white 30%); }
.panel-sesudah { border-color: var(--pertamina-green);
                  background: linear-gradient(180deg, #f0fdf4 0%, white 30%); }
.panel-header { font-size: var(--fs-sm); font-weight: 700;
                 margin-bottom: 0.3vw; padding-bottom: 0.2vw;
                 border-bottom: 1px dashed var(--border); }
.panel-sebelum .panel-header { color: var(--pertamina-red); }
.panel-sesudah .panel-header { color: var(--pertamina-green); }
```

### Layer row (Sebelum + Opsi IV Sesudah)
```css
.layer-list { display: flex; flex-direction: column; gap: 0.15vw; flex: 1; }
.layer { display: flex; gap: 0.4vw; align-items: center;
         padding: 0.2vw 0.3vw; border-bottom: 1px dashed #f3f4f6; font-size: var(--fs-xs); }
.layer:last-child { border-bottom: none; }
.layer-actor { width: 32%; font-weight: 600; }
.layer-tools { flex: 1; display: flex; gap: 0.2vw; flex-wrap: wrap; align-items: center; }
.chip { font-size: 9px; padding: 1px 5px; border-radius: 3px; white-space: nowrap; font-weight: 500; }
.chip.manual { background: var(--pertamina-red-light); color: var(--pertamina-red); border: 1px solid var(--pertamina-red); }
.chip.ext { background: #fff3cd; color: #856404; border: 1px solid #d4a017; }
.chip.paper { background: #e8e8e8; color: #555; border: 1px solid #999; }
.chip.portal { background: var(--pertamina-blue-light); color: var(--pertamina-blue-dark); border: 1px solid var(--pertamina-blue); }
```

### Marker (issue + improvement)
```css
.marker {
  display: inline-flex; align-items: center; justify-content: center;
  width: 18px; height: 18px; border-radius: 50%;
  color: white; font-weight: 700; font-size: 10px;
  margin-left: 2px;
}
.marker.issue { background: var(--pertamina-red); }
.marker.improvement { background: var(--pertamina-green); }
```

### Legend inset
```css
.legend-inset {
  position: absolute; top: 0.3vw; right: 0.4vw;
  font-size: var(--fs-xs);
  background: rgba(255,255,255,.9);
  padding: 0.15vw 0.3vw;
  border-radius: 3px;
  border: 1px solid var(--border);
}
```

### Buffer Zone (Opsi IV hero)
```css
.buffer-zone {
  background: var(--hub-grad); color: white;
  padding: 0.35vw 0.5vw; border-radius: 5px;
  margin: 0.2vw 0; text-align: center;
  border-left: 4px solid var(--pertamina-yellow);
  position: relative;
}
.buffer-label { font-weight: 700; font-size: var(--fs-sm); }
.buffer-content { font-size: var(--fs-xs); opacity: .95; margin-top: 0.1vw; }
```

### Pipeline (Opsi II hero)
```css
.pipeline-row { display: flex; gap: 0.3vw; flex: 1; align-items: stretch; padding: 0.2vw 0; }
.stage { color: white; padding: 0.4vw 0.5vw; border-radius: 5px;
          flex: 1; display: flex; flex-direction: column; gap: 0.2vw;
          font-size: var(--fs-xs); }
.stage-1 { background: linear-gradient(135deg, #00558C, #0077B5); }
.stage-2 { background: linear-gradient(135deg, #0077B5, #00A551); }
.stage-3 { background: linear-gradient(135deg, #00A551, #5cba7d); }
.stage-num { font-size: var(--fs-md); font-weight: 800; }
.stage-title { font-weight: 700; font-size: var(--fs-xs); }
.stage-actors { font-size: 9px; opacity: .85; font-style: italic; }
.stage-features { font-size: 9px; line-height: 1.4; }
.stage-markers { margin-top: auto; display: flex; gap: 2px; }
.pipeline-arrow { display: flex; align-items: center;
                  font-size: var(--fs-md); color: var(--pertamina-yellow);
                  padding: 0 0.1vw; font-weight: 700; }
```

## 5. Opsi II — `pipeline-outcome-compact.html`

### Layout

**Kiri panel (Sebelum, 5-layer compact):**

| Layer | Aktor | Tools (chips) | Markers |
|-------|-------|---------------|---------|
| Lv5 | 👔 Manajemen | 📄 PDF/Excel · 📧 Email | D |
| Lv4 | 👤 HC | 📊 Excel ×5 · 📝 Word | A, B |
| Lv3 | 🏢 Atasan | 📧 Email · 💬 WA approval | C, E |
| Lv2 | 🧑‍🏫 Coach | 📋 Form cetak · 📁 Arsip · 💬 WA · 📧 Email | A, E |
| Lv1 | 👷 Pekerja | 🌐 FleQi · 🎓 Cert hardcopy · 📊 Excel IDP | A, F |

Footer mini: `FMEA RPN · Method 140 · Machine 140 · Man 90`

**Kanan panel (Solusi Terpilih, Pipeline 3-stage hero):**

| Stage | Title | Actors | Features | Markers |
|-------|-------|--------|----------|---------|
| ① | Information Gathering & Evaluation<br/>*Self-Asm · Directed · Shop-Floor* | 👤 HC · 👷 Pekerja | 📝 Assessment Online · 📊 KKJ Matrix · 🎯 PROTON IDP | 2, 7 |
| ② | Activity Auditing<br/>*Evidence Gathering* | 🏢 Atasan · 🧑‍🏫 Coach · 👤 HC | 📎 Upload Evidence · ✅ Approval · 🔍 Audit Log · 🔐 RBAC | 3, 5, 6 |
| ③ | Feedback Loop<br/>*Real-time · Transparent* | 👔 Manajemen · 👤 HC · 👷 Pekerja | 🔥 Heatmap Gap · 🏆 Cert Download · 🔔 Notifikasi In-App | 1, 4 |

Legend inset top-right: "Marker 1-7 · Improvement (HC Portal)"

### Markup outline
```html
<body>
<div class="compact-container">
  <div class="panel panel-sebelum">
    <div class="panel-header">❌ Sebelum (Kondisi Aktual)</div>
    <div class="layer-list">
      <!-- 5 layers Lv5→Lv1 -->
    </div>
    <div class="rpn-note">FMEA RPN · Method 140 · Machine 140 · Man 90</div>
    <div class="legend-inset">A-F · Issue (Risalah PROTON FMEA)</div>
  </div>
  <div class="panel panel-sesudah">
    <div class="panel-header">✅ Solusi Terpilih — Pemantauan Kompetensi Pipeline · Ogoun &amp; Tamunosiki-Amadi (2023)</div>
    <div class="pipeline-row">
      <!-- 3 stages + 2 arrows -->
    </div>
    <div class="legend-inset">1-7 · Improvement (HC Portal)</div>
  </div>
</div>
</body>
```

## 6. Opsi IV — `workflow-topology-refined-compact.html`

### Layout

**Kiri panel (Sebelum, identik Opsi II):**
Sama dengan Opsi II Sebelum (DRY) — 5-layer, markers A-F.

**Kanan panel (Solusi Terpilih, 5-layer + Buffer Zone):**

| Layer | Aktor | Fitur HC Portal | Markers |
|-------|-------|-----------------|---------|
| Lv5 | 👔 Manajemen | 📈 Analytics · 🔥 Heatmap · 📤 Export | 1 |
| Lv4 | 👤 HC | 👥 Pekerja · 🎯 IDP · 📝 Asm · 📊 KKJ | 2, 3 |
| **Buffer Zone** | 🛡️ — | HC Portal SSoT · .NET 8 · SQL · SignalR · Audit Log | 4 |
| Lv3 | 🏢 Atasan | 👀 Records · ✅ Approval · 📊 View KKJ | 5 |
| Lv2 | 🧑‍🏫 Coach | 🎯 Coaching PROTON (5 fase) · 📎 Evidence · 📜 Histori | 6 |
| Lv1 | 👷 Pekerja | 📝 Asm · 📋 IDP · 🏆 Cert · 🔔 Notif | 7 |

Legend inset top-right: "Marker 1-7 · Improvement (HC Portal)"

### Markup outline
```html
<body>
<div class="compact-container">
  <div class="panel panel-sebelum">
    <!-- identik Opsi II Sebelum -->
  </div>
  <div class="panel panel-sesudah">
    <div class="panel-header">✅ Solusi Terpilih — HC Portal (Buffer Zone DMZ-analog)</div>
    <div class="layer-list">
      <!-- Lv5 + Lv4 -->
      <div class="buffer-zone">
        <div class="buffer-label">🛡️ BUFFER ZONE — HC PORTAL</div>
        <div class="buffer-content">Single Source of Truth · .NET 8 · SQL Server · SignalR · Audit Log <span class="marker improvement">4</span></div>
      </div>
      <!-- Lv3 + Lv2 + Lv1 -->
    </div>
    <div class="legend-inset">1-7 · Improvement (HC Portal)</div>
  </div>
</div>
</body>
```

## 7. Yang TIDAK di dalam kotak compact (semua opsi)

Sesuai approach β, elemen berikut PINDAH ke area lain slide PPT (tidak include di HTML compact):

- Outcome matrix R-coefficient (R=0.777/0.610/0.190 → Panca Mutu)
- Tabel Komparasi 10 Aspek
- Issue detail table (A-F)
- Improvement detail table (1-7)
- Tech stack box (rincian .NET 8 etc — di Opsi IV ada singkatan di Buffer; rincian lengkap di luar)
- Standar External (ISO/IEC 27001, OWASP, WCAG)
- Standar Internal (Pedoman A5.2-01, TKO B5.3-04, Kamus Direktori)
- "Coret yang tidak digunakan" section
- Theory footer citation (Ogoun, Ellström, Panca Mutu)

User handle elemen-elemen tsb di area slide PPT yang lain (di luar kotak 8,11 × 25,72 cm).

## 8. Reference Mapping (identik v1.0)

| Kode | Sumber | Dipakai di compact |
|------|--------|---------------------|
| R4 | Ogoun & Tamunosiki-Amadi (2023) | Opsi II header pipeline (citation only) |
| P1 | Risalah PROTON FMEA | Both Sebelum RPN note |
| P2 | Risalah Panca Mutu | Implicit (markers 1-7) |
| All other refs (R1, RL1-3, P2, SE1-3, SI1-3) | dipakai di v1.0 full, **tidak di compact** | — |

## 9. Approved Decisions

Approach **β (Diagram-Focused)** sesuai user pilihan, dengan defaults:
1. 2-panel side-by-side, grid 1fr 1fr
2. Sebelum panel identik di kedua opsi (DRY)
3. Sesudah panel beda: Opsi II = Pipeline 3-stage, Opsi IV = 5-layer + Buffer Zone
4. Markers A-F + 1-7 embedded di diagram
5. Legend inset top-right corner (compact)
6. Font compact 9-18px floor
7. Title bar hijau 0,9 × 14,5 cm TIDAK include (handled PPT)
8. Naming `-compact` suffix konsisten dengan v1.0 base
9. Print CSS `@page { size: 25.72cm 8.11cm; margin: 0 }` available

## 10. PNG Export Process

```
mcp__plugin_playwright_playwright__browser_resize(width=3035, height=957)
mcp__plugin_playwright_playwright__browser_navigate(url="http://localhost:<port>/pipeline-outcome-compact.html")
mcp__plugin_playwright_playwright__browser_take_screenshot(type="png", filename="docs/pcp-HCPortal-2026/slide8-risalah/compact/pipeline-outcome-compact.png")
```

Repeat untuk `workflow-topology-refined-compact.html`.

Body fills 100vw × 100vh — PNG output otomatis 3035 × 957 = 300 DPI equivalent saat scaled ke 25,72 × 8,11 cm di PPT.

## 11. Implementation Sequence

1. Create folder `docs/pcp-HCPortal-2026/slide8-risalah/compact/`
2. Build `pipeline-outcome-compact.html` (Opsi II)
3. Visual QA Playwright snapshot
4. Build `workflow-topology-refined-compact.html` (Opsi IV)
5. Visual QA
6. PNG export both (300 DPI)
7. Update slide8-risalah/README.md (mention compact/ subfolder)
8. Commit + tag (extend `slide8-risalah-v1.1` atau pakai tag yang sama force-update)

## 12. Acceptance Criteria

- [ ] Both HTML render dengan body fill exactly 100vw × 100vh
- [ ] Aspect ratio output PNG = 3.17 : 1 (3035 × 957)
- [ ] Sebelum panel: 5 layer Lv5→Lv1 + markers A-F + RPN note + legend inset
- [ ] Opsi II Sesudah: Pipeline 3-stage horizontal + actor-per-stage + features + markers 1-7
- [ ] Opsi IV Sesudah: 5 layer + Buffer Zone hero + markers 1-7
- [ ] Font readable saat printed @ 25,72 × 8,11 cm (floor 9px @ 300 DPI = ~ 7pt printed)
- [ ] PNG file size 200KB-1MB each
- [ ] No element overflow (overflow: hidden body)
- [ ] All 9 approved decisions applied

## 13. Tag & Versioning

**New tag `slide8-risalah-v1.1`** (compact variant). Preserve `v1.0` sebagai stable reference (full landscape A3 format). v1.1 = compact (8,11 × 25,72 cm box).

## 14. Out-of-Scope

- Tidak modify existing v1.0 files (`pipeline-outcome.html`, `workflow-topology-refined.html`, `index.html`)
- Tidak ubah v1.0 PNG exports
- Tidak update spec/plan v1.0
- Tidak handle title bar 0,9 × 14,5 cm rendering (PPT user handle)
- Tidak generate tabel/standar/coret/footer di area kotak (di luar kotak, PPT user handle)
