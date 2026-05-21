# CMP & CDP Mockup Animation — Design Spec

**Status:** Draft — awaiting user review
**Author:** Brainstorm session 2026-05-21
**Target file:** `docs/assets/proton-video/cmp-cdp-mockup.html`
**Series context:** PROTON video — animasi ilustrasi 2 platform inti Portal HC KPB (CMP + CDP). Durasi 20s, 2 scene cross-fade.

---

## 1. Goal

Bangun animasi HTML 16:9 standalone untuk PROTON video — 2 scene cross-fade real mockup Portal HC KPB menu CMP (3 card) + menu CDP (4 card). Match exact Bootstrap card style dari real `Views/CMP/Index.cshtml` + `Views/CDP/Index.cshtml`. Record `.webm` lewat Playwright.

**Quantified targets:**
- Durasi exact 20s
- Action density ≥85%
- 2 scene cross-fade
- Card mockup pixel-recognizable match real Portal HC KPB

---

## 2. Decision Log (from brainstorm)

| # | Decision | Choice |
|---|----------|--------|
| D1 | File output | Single HTML 2 scene cross-fade (CMP + CDP) |
| D2 | Durasi | 20s |
| D3 | Card count | Faithful narasi: 3 CMP + 4 CDP cards |
| D4 | Mockup style | Real Portal HC KPB exact (portal-frame + Bootstrap-style cards) |
| D5 | Bootstrap Icons | CDN load `bootstrap-icons.css` (match real Portal authentic) |
| D6 | Card animation conflict | Hapus `.bs-card.show`, gunakan `#scene-X.active` stagger only |
| D7 | CDP scene body min-height | Override `#scene-cdp .portal-body { min-height: clamp(280px, 30vw, 400px); }` untuk 2×2 grid |
| D8 | Riwayat Pelatihan subtitle | Tweak ke "Catatan & Pengembangan" bridge narasi |
| D9 | Audio sync mapping | Editor splice paragraf 1 (CMP) di t=2-10s, paragraf 2 (CDP) di t=11-19s |

---

## 3. Architecture

### 3.1 File Scope

| File | Status | Tanggung jawab |
|------|--------|----------------|
| `docs/assets/proton-video/cmp-cdp-mockup.html` | CREATE | Animasi 20s, 2 scene cross-fade, CSS+JS inline + Bootstrap-Icons CDN |
| `docs/assets/proton-video/record-cmp-cdp-mockup.mjs` | CREATE | Playwright record 21s, port 8770 |
| `docs/assets/proton-video/cmp-cdp-mockup.webm` | GENERATE | Output 20s, 1920×1080, ~2-3 MB |

### 3.2 Tech Stack

CSS3 + vanilla JS inline + Inter (Google Fonts) + **Bootstrap-Icons CDN** (`https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css`) + Playwright chromium.

### 3.3 Out of Scope

- Naskah audio docx
- Real screenshot (pakai HTML+CSS mockup match real styling)
- Interactivity (animation only)

---

## 4. Timeline 20s Breakdown

| Slot | Window | Konten |
|------|--------|--------|
| **Intro** | 0.0–2.0s | Stage + grid backdrop fade-in (0–0.4s). Header eyebrow "Platform Portal HC KPB" + title "Dua Platform <em>Inti</em>" fade-down (0.2–1.0s). Em "Inti" underline draw red (1.2–1.8s). Brand mark fade-in (1.5s). |
| **Scene CMP** | 2.0–10.0s (8s) | Cross-fade to scene-cmp. Portal-frame navbar+breadcrumb (Beranda › CMP)+title appear (2.0–2.6s). Title "Competency Management Platform" (h2 + `bi-mortarboard` icon) + subtitle "Kelola kompetensi, assessment, dan rencana pengembangan" fade-up (2.6–3.0s). 3 card stagger pop spring 400ms apiece (3.0–4.2s). Card highlight pulse sequential 800ms (4.5–6.5s). Hold (6.5–10.0s). |
| **Cross-fade** | 10.0–11.0s | Scene-cmp fade-out 0.5s, scene-cdp fade-in 0.5s overlap. |
| **Scene CDP** | 11.0–19.0s (8s) | Portal-frame update breadcrumb (Beranda › CDP) + title "Competency Development Portal" (h2 + `bi-person-workspace` icon) + subtitle "Kelola rencana pengembangan individu, coaching, dan pemantauan progres" fade-up (11.0–12.0s). 4 card 2×2 grid stagger pop spring 400ms apiece (12.0–13.8s). Card highlight pulse sequential 750ms (14.0–17.0s). Hold (17.0–19.0s). |
| **Outro hold** | 19.0–20.0s | Final poster: scene-cdp + 4 cards visible + portal-frame static. |

**Action density:** 19s / 20s = 95%.

---

## 5. Layout Structure

### 5.1 Stage 16:9 (carry series)

Same pattern: stage 1600×900 max, grid backdrop, header + scenes + brand-mark.

### 5.2 Header

```css
.eyebrow {
  font-size: clamp(11px, 0.9vw, 14px);
  font-weight: 800; color: var(--red); letter-spacing: 0.2em;
  text-transform: uppercase; margin-bottom: 8px;
}
.title {
  font-size: clamp(24px, 2.2vw, 38px);
  font-weight: 900; color: var(--navy-deep); line-height: 1.1;
}
.title em {
  font-style: normal; color: var(--red);
  background-image: linear-gradient(90deg, var(--red), var(--red));
  background-size: 0% 6px; background-position: 0 92%;
  background-repeat: no-repeat; padding-bottom: 2px;
  animation: underlineDraw 0.6s 1.2s cubic-bezier(0.65, 0, 0.35, 1) forwards;
}
@keyframes underlineDraw { to { background-size: 100% 6px; } }
```

### 5.3 Portal Frame (carry from assessment-akhir)

Reuse portal-navbar, portal-breadcrumb, portal-title, portal-body classes. With override:

```css
.portal-title h2 {
  display: flex;
  align-items: center;
  gap: 8px;
}
.portal-title h2 .bi {
  font-size: 1.1em;
  color: var(--navy);
}

/* CDP scene body needs more vertical space for 2×2 */
#scene-cdp .portal-body {
  min-height: clamp(280px, 30vw, 400px);
}
```

### 5.4 Scene Container + Cross-Fade (carry)

Standard pattern same as series.

---

## 6. Card Mockup (Bootstrap-Style)

### 6.1 Card Primitive CSS

```css
.bs-card {
  border: 0;
  border-radius: 8px;
  background: #FFFFFF;
  box-shadow: 0 1px 2px rgba(0,0,0,0.06), 0 4px 12px rgba(0,0,0,0.05);
  padding: clamp(14px, 1.6vw, 22px);
  display: flex;
  flex-direction: column;
  gap: clamp(10px, 1vw, 16px);
  opacity: 0;
  transform: translateY(12px) scale(0.96);
  transition: box-shadow 0.4s ease;
}
.bs-card-head {
  display: flex;
  align-items: center;
  gap: clamp(10px, 1.2vw, 16px);
}
.icon-box {
  width: clamp(40px, 4vw, 60px);
  height: clamp(40px, 4vw, 60px);
  border-radius: 8px;
  display: grid;
  place-items: center;
  font-size: clamp(20px, 2vw, 28px);
  flex-shrink: 0;
}
.icon-box.primary { background: rgba(13,110,253,0.10); color: #0d6efd; }
.icon-box.info    { background: rgba(13,202,240,0.12); color: #0dcaf0; }
.icon-box.success { background: rgba(25,135,84,0.10); color: #198754; }
.icon-box.warning { background: rgba(255,193,7,0.14); color: #b8870b; }
.icon-box.secondary { background: rgba(108,117,125,0.10); color: #6c757d; }

.bs-card .head-text h5 {
  font-size: clamp(13px, 1.1vw, 16px);
  font-weight: 700;
  color: var(--ink);
  line-height: 1.2;
  margin-bottom: 2px;
}
.bs-card .head-text small {
  font-size: clamp(10px, 0.82vw, 12px);
  color: var(--muted);
  font-weight: 600;
}
.bs-card .desc {
  font-size: clamp(11px, 0.9vw, 13px);
  color: var(--muted);
  line-height: 1.4;
}
.bs-card .action {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 8px 12px;
  border-radius: 6px;
  font-size: clamp(11px, 0.92vw, 13px);
  font-weight: 600;
  color: #FFFFFF;
  text-decoration: none;
  margin-top: auto;
}
.action.primary { background: #0d6efd; }
.action.info    { background: #0dcaf0; color: #053742; }
.action.success { background: #198754; }
.action.warning { background: #ffc107; color: #533f03; }
.action.secondary { background: #6c757d; }

.bs-card.highlight {
  box-shadow:
    0 1px 2px rgba(15,45,92,0.06),
    0 8px 20px rgba(15,45,92,0.12),
    0 0 0 3px rgba(230,51,41,0.18);
}
@keyframes cardPop {
  0%   { opacity: 0; transform: translateY(12px) scale(0.96); }
  100% { opacity: 1; transform: translateY(0) scale(1); }
}
```

### 6.2 Stagger Reveal (per scene)

```css
/* CMP 3 cards single row */
#scene-cmp.active .cmp-cards .bs-card:nth-child(1) { animation: cardPop 0.5s 1.0s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
#scene-cmp.active .cmp-cards .bs-card:nth-child(2) { animation: cardPop 0.5s 1.4s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
#scene-cmp.active .cmp-cards .bs-card:nth-child(3) { animation: cardPop 0.5s 1.8s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }

/* CDP 4 cards 2x2 grid */
#scene-cdp.active .cdp-cards .bs-card:nth-child(1) { animation: cardPop 0.5s 1.0s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
#scene-cdp.active .cdp-cards .bs-card:nth-child(2) { animation: cardPop 0.5s 1.4s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
#scene-cdp.active .cdp-cards .bs-card:nth-child(3) { animation: cardPop 0.5s 1.8s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
#scene-cdp.active .cdp-cards .bs-card:nth-child(4) { animation: cardPop 0.5s 2.2s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
```

### 6.3 Grid Layout

```css
.cmp-cards {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: clamp(12px, 1.4vw, 24px);
  width: 100%;
}
.cdp-cards {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  grid-template-rows: repeat(2, 1fr);
  gap: clamp(12px, 1.4vw, 24px);
  width: 100%;
}
```

---

## 7. Markup Structure

### 7.1 CMP 3 Cards Content (per real `Views/CMP/Index.cshtml`)

| # | Card | Icon (Bootstrap) | Color | h5 Title | Small Subtitle | Desc | Btn Label |
|---|------|------------------|-------|----------|-----------------|------|-----------|
| 1 | Assessment Saya | `bi-clipboard-check` | info | Assessment Saya | Ujian & Evaluasi | Ikuti assessment, lihat hasil, dan pantau riwayat evaluasi Anda | Assessment Saya |
| 2 | Manajemen Sertifikasi | `bi-patch-check` | success | Manajemen Sertifikasi | Kelola Sertifikat | Lihat dan kelola semua sertifikat pelatihan dan asesmen pekerja | Kelola Sertifikat |
| 3 | Riwayat Pelatihan | `bi-journal-text` | secondary | Riwayat Pelatihan | **Catatan & Pengembangan** | Lihat dan kelola riwayat pelatihan serta catatan penilaian | Lihat Riwayat |

(Subtitle Riwayat Pelatihan tweaked dari real "Pengembangan Kapabilitas" → "Catatan & Pengembangan" untuk bridge narasi "catatan penilaian".)

### 7.2 CDP 4 Cards Content (per real `Views/CDP/Index.cshtml` + Deliverable)

| # | Card | Icon | Color | h5 Title | Small Subtitle | Desc | Btn Label |
|---|------|------|-------|----------|-----------------|------|-----------|
| 1 | Individual Development Plan | `bi-file-earmark-pdf` | primary | Individual Development Plan | Dokumen Silabus | Lihat dokumen kurikulum dan rencana pengembangan individu Proton | Lihat Dokumen |
| 2 | Proton Coaching | `bi-graph-up-arrow` | warning | Proton Coaching | Status IDP | Pantau progres deliverable Proton dan status persetujuan | Proton Coaching |
| 3 | Deliverable | `bi-cloud-upload` | info | Deliverable | Bukti Pekerjaan | Upload dan kelola bukti pekerjaan deliverable per tugas | Kelola Deliverable |
| 4 | Manajemen Sertifikasi | `bi-patch-check` | success | Manajemen Sertifikasi | Sertifikat Kompetensi | Lihat dan kelola sertifikat kompetensi pekerja | Kelola Sertifikat |

### 7.3 Full Markup

```html
<body>
  <div class="stage">
    <div class="header">
      <div class="eyebrow">Platform Portal HC KPB</div>
      <h1 class="title">Dua Platform <em>Inti</em></h1>
    </div>

    <div class="scenes">

      <!-- Scene CMP -->
      <div class="scene" id="scene-cmp">
        <div class="portal-frame">
          <div class="portal-navbar">
            <div class="portal-brand">
              <svg class="portal-brand-icon" viewBox="0 0 24 24" aria-hidden="true">
                <path d="M3 21V7l9-4v4h9v14h-7v-6h-4v6H3zm2-2h5v-4h6v4h5V9h-9V5.5L5 8.45V19z"/>
              </svg>
              <span>HC Portal</span>
            </div>
            <div class="portal-nav">
              <a class="portal-nav-item">Beranda</a>
              <a class="portal-nav-item active">CMP</a>
              <a class="portal-nav-item">Histori</a>
            </div>
            <div class="portal-user">
              <div class="portal-avatar">AP</div>
              <span>Andi P.</span>
            </div>
          </div>
          <div class="portal-breadcrumb">
            <span class="crumb">Beranda</span><span class="sep">›</span><span class="crumb active">CMP</span>
          </div>
          <div class="portal-title">
            <h2><i class="bi bi-mortarboard"></i>Competency Management Platform</h2>
            <p>Kelola kompetensi, assessment, dan rencana pengembangan</p>
          </div>
          <div class="portal-body">
            <div class="cmp-cards">
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box info"><i class="bi bi-clipboard-check"></i></div>
                  <div class="head-text"><h5>Assessment Saya</h5><small>Ujian & Evaluasi</small></div>
                </div>
                <div class="desc">Ikuti assessment, lihat hasil, dan pantau riwayat evaluasi Anda</div>
                <a class="action info">Assessment Saya <i class="bi bi-arrow-right-circle"></i></a>
              </div>
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box success"><i class="bi bi-patch-check"></i></div>
                  <div class="head-text"><h5>Manajemen Sertifikasi</h5><small>Kelola Sertifikat</small></div>
                </div>
                <div class="desc">Lihat dan kelola semua sertifikat pelatihan dan asesmen pekerja</div>
                <a class="action success">Kelola Sertifikat <i class="bi bi-arrow-right-circle"></i></a>
              </div>
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box secondary"><i class="bi bi-journal-text"></i></div>
                  <div class="head-text"><h5>Riwayat Pelatihan</h5><small>Catatan & Pengembangan</small></div>
                </div>
                <div class="desc">Lihat dan kelola riwayat pelatihan serta catatan penilaian</div>
                <a class="action secondary">Lihat Riwayat <i class="bi bi-arrow-right-circle"></i></a>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Scene CDP -->
      <div class="scene" id="scene-cdp">
        <div class="portal-frame">
          <div class="portal-navbar">
            <div class="portal-brand">
              <svg class="portal-brand-icon" viewBox="0 0 24 24" aria-hidden="true">
                <path d="M3 21V7l9-4v4h9v14h-7v-6h-4v6H3zm2-2h5v-4h6v4h5V9h-9V5.5L5 8.45V19z"/>
              </svg>
              <span>HC Portal</span>
            </div>
            <div class="portal-nav">
              <a class="portal-nav-item">Beranda</a>
              <a class="portal-nav-item active">CDP</a>
              <a class="portal-nav-item">Histori</a>
            </div>
            <div class="portal-user">
              <div class="portal-avatar">AP</div>
              <span>Andi P.</span>
            </div>
          </div>
          <div class="portal-breadcrumb">
            <span class="crumb">Beranda</span><span class="sep">›</span><span class="crumb active">CDP</span>
          </div>
          <div class="portal-title">
            <h2><i class="bi bi-person-workspace"></i>Competency Development Portal</h2>
            <p>Kelola rencana pengembangan individu, coaching, dan pemantauan progres</p>
          </div>
          <div class="portal-body">
            <div class="cdp-cards">
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box primary"><i class="bi bi-file-earmark-pdf"></i></div>
                  <div class="head-text"><h5>Individual Development Plan</h5><small>Dokumen Silabus</small></div>
                </div>
                <div class="desc">Lihat dokumen kurikulum dan rencana pengembangan individu Proton</div>
                <a class="action primary">Lihat Dokumen <i class="bi bi-arrow-right-circle"></i></a>
              </div>
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box warning"><i class="bi bi-graph-up-arrow"></i></div>
                  <div class="head-text"><h5>Proton Coaching</h5><small>Status IDP</small></div>
                </div>
                <div class="desc">Pantau progres deliverable Proton dan status persetujuan</div>
                <a class="action warning">Proton Coaching <i class="bi bi-arrow-right-circle"></i></a>
              </div>
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box info"><i class="bi bi-cloud-upload"></i></div>
                  <div class="head-text"><h5>Deliverable</h5><small>Bukti Pekerjaan</small></div>
                </div>
                <div class="desc">Upload dan kelola bukti pekerjaan deliverable per tugas</div>
                <a class="action info">Kelola Deliverable <i class="bi bi-arrow-right-circle"></i></a>
              </div>
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box success"><i class="bi bi-patch-check"></i></div>
                  <div class="head-text"><h5>Manajemen Sertifikasi</h5><small>Sertifikat Kompetensi</small></div>
                </div>
                <div class="desc">Lihat dan kelola sertifikat kompetensi pekerja</div>
                <a class="action success">Kelola Sertifikat <i class="bi bi-arrow-right-circle"></i></a>
              </div>
            </div>
          </div>
        </div>
      </div>

    </div>

    <div class="brand-mark">PROTON × KPB</div>
  </div>
  <script>/* orchestration */</script>
</body>
```

---

## 8. JS Orchestration

```javascript
function showScene(id) {
  document.querySelectorAll('.scene').forEach(s => s.classList.remove('active'));
  document.getElementById(id).classList.add('active');
}

function highlightCard(sceneId, idx, duration = 800) {
  const card = document.querySelectorAll(`#${sceneId} .bs-card`)[idx - 1];
  if (!card) return;
  card.classList.add('highlight');
  setTimeout(() => card.classList.remove('highlight'), duration);
}

const milestones = [
  // Scene CMP (2-10s)
  { t: 2000, fn: () => showScene('scene-cmp') },
  { t: 4500, fn: () => highlightCard('scene-cmp', 1) },
  { t: 5300, fn: () => highlightCard('scene-cmp', 2) },
  { t: 6100, fn: () => highlightCard('scene-cmp', 3) },

  // Cross-fade to CDP (11s)
  { t: 11000, fn: () => showScene('scene-cdp') },

  // CDP highlight sequence (14-17s)
  { t: 14000, fn: () => highlightCard('scene-cdp', 1, 750) },
  { t: 14750, fn: () => highlightCard('scene-cdp', 2, 750) },
  { t: 15500, fn: () => highlightCard('scene-cdp', 3, 750) },
  { t: 16250, fn: () => highlightCard('scene-cdp', 4, 750) }
];

const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
if (reduced) {
  showScene('scene-cdp');
  document.querySelectorAll('.bs-card').forEach(c => {
    c.style.opacity = '1';
    c.style.transform = 'none';
  });
} else {
  milestones.forEach(m => setTimeout(m.fn, m.t));
}
```

**JS budget:** ~30 LOC.

---

## 9. Accessibility — `prefers-reduced-motion`

```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-delay: 0ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
  .scene { opacity: 0; }
  .scene#scene-cdp { opacity: 1; transform: none; }
  .title em { background-size: 100% 6px; }
  .bs-card { opacity: 1; transform: none; }
}
```

---

## 10. Risks & Mitigations

| # | Risk | Likelihood | Mitigation |
|---|------|-----------|------------|
| R1 | Card content panjang melebihi container = wrap glitch | Med | Title <30 char + desc <80 char. Fixed padding. |
| R2 | CDP 2×2 grid di portal-body padat vertical | Med | Override min-height 280-400px. Card padding moderate. |
| R3 | Highlight pulse 750ms × 4 sequential = 3s — masih fit 8s window | Low | Acceptable |
| R4 | Bootstrap Icons CDN load delay saat Playwright record | Low | CDN reliable, ~300-500ms load di domcontentloaded |
| R5 | File size HTML melebihi 32 KB | Med | Target 28-32 KB realistic |
| R6 | Bootstrap colors mix dengan Pertamina red highlight = clash | Low | Highlight = red ring outside card, jelas vs icon-box bg-color inside |

---

## 11. Testing Checklist

- [ ] Local browser refresh — 20s end-to-end smooth
- [ ] Header em "Inti" underline draw red
- [ ] Scene CMP: portal-frame breadcrumb "Beranda › CMP" + title "Competency Management Platform"
- [ ] CMP 3 card stagger pop 400ms spring
- [ ] CMP card highlight pulse sequential (red ring 800ms each)
- [ ] Cross-fade smooth CMP → CDP
- [ ] Scene CDP: breadcrumb "Beranda › CDP" + title "Competency Development Portal"
- [ ] CDP 4 card 2×2 grid stagger pop
- [ ] CDP card highlight pulse sequential
- [ ] Bootstrap Icons render correctly (clipboard-check, patch-check, journal-text, file-earmark-pdf, graph-up-arrow, cloud-upload, mortarboard, person-workspace, arrow-right-circle)
- [ ] Color theme per card (info/success/secondary/primary/warning) match real Portal
- [ ] DevTools reduced-motion → scene-cdp static final visible
- [ ] No console errors
- [ ] Playwright record 21s → .webm 2-3 MB

---

## 12. Success Criteria

| # | Criterion | Verify |
|---|-----------|--------|
| SC1 | Durasi 20s, action density ≥85% | Stopwatch |
| SC2 | Header "Dua Platform Inti" + em underline red | Visual |
| SC3 | Scene CMP portal-frame match real Portal layout | Visual side-by-side real Views |
| SC4 | CMP 3 card pop spring sequential | Visual |
| SC5 | CMP card highlight pulse 3× | Visual t=4.5-6.5s |
| SC6 | Cross-fade CMP → CDP smooth | Visual t=10-11s |
| SC7 | Scene CDP portal-frame | Visual |
| SC8 | CDP 4 card 2×2 grid pop spring sequential | Visual |
| SC9 | CDP card highlight pulse 4× | Visual t=14-17s |
| SC10 | Card style exact Bootstrap (border-0 shadow rounded + icon-box bg-opacity-10 + h5 + small + p + btn full) | Visual diff real Index |
| SC11 | Icon colors per card sesuai real (info/success/secondary/primary/warning) | Visual |
| SC12 | Bootstrap Icons load (no fallback ☐ box) | Visual icon glyphs visible |
| SC13 | `prefers-reduced-motion: reduce` → static final | DevTools simulate |
| SC14 | File HTML <32 KB | `wc -c` |
| SC15 | `.webm` 20-21s, 2-3 MB | Playback |
| SC16 | Recordable idempotent | Re-run |

---

## 13. Task Breakdown (preview)

| # | Task | Files |
|---|------|-------|
| 0 | Setup Playwright + http.server | env |
| 1 | Scaffold HTML — stage + palette + grid + header + brand mark + Bootstrap-Icons CDN link | cmp-cdp-mockup.html |
| 2 | Scene container + cross-fade + portal-frame CSS (carry assessment-akhir) | cmp-cdp-mockup.html |
| 3 | Bootstrap card primitive CSS (.bs-card + .icon-box + .action variants) | cmp-cdp-mockup.html |
| 4 | Scene CMP markup (portal-frame + 3 card grid 1×3) | cmp-cdp-mockup.html |
| 5 | Scene CDP markup (portal-frame + 4 card grid 2×2) | cmp-cdp-mockup.html |
| 6 | Card stagger reveal CSS (#scene-X.active nth-child delays) | cmp-cdp-mockup.html |
| 7 | Card highlight class CSS (.bs-card.highlight red ring) | cmp-cdp-mockup.html |
| 8 | JS orchestration (showScene + highlightCard + milestones) | cmp-cdp-mockup.html |
| 9 | `prefers-reduced-motion` fallback | cmp-cdp-mockup.html |
| 10 | Record script port 8770 | record-cmp-cdp-mockup.mjs |
| 11 | Run record + verify webm | cmp-cdp-mockup.webm |
| 12 | Spec compliance final check | (verification) |

13 atomic task.

---

## 14. Definition of Done

- All 16 SC pass
- 11 atomic commits (Task 1-11; Task 0 + 12 no commit)
- `cmp-cdp-mockup.html` < 32 KB
- `cmp-cdp-mockup.webm` 2-3 MB, 20-21s
- Naskah docx tidak di-touch
- `git status` bersih
- Spec updated kalau ada deviasi

---

## 15. Rollback Strategy

3 file baru: `cmp-cdp-mockup.html`, `record-cmp-cdp-mockup.mjs`, `cmp-cdp-mockup.webm`. Kalau gagal:

```bash
git revert <task1-commit>..<lastcommit>
```

---

## 16. Audio Sync Note (untuk Editor Video)

Animasi HTML 20s, narasi 2 paragraf ~12-15s. Mapping:

| Slot | HTML | Narasi Audio |
|------|------|--------------|
| 0.0–2.0s | Intro animasi | Lead-in (silent) |
| 2.0–10.0s (8s) | Scene CMP | Paragraf 1: "CMP (Competency Management Platform) — mencangkup pengelolaan assessment/ujian kompetensi, sertifikasi, dan catatan penilaian" (~6-7s) overlay |
| 10.0–11.0s | Cross-fade | Editor transition |
| 11.0–19.0s (8s) | Scene CDP | Paragraf 2: "CDP (Continuous Development Platform) — mencangkup pengelolaan coaching, deliverable, IDP (Individual Development Plan), dan sertifikasi kompetensi" (~7-8s) overlay |
| 19.0–20.0s | Outro hold | Tail (silent) |

Editor align narasi audio mulai t=2s ke t=19s. CMP paragraf di scene-cmp window, CDP paragraf di scene-cdp window.
