# Assessment Akhir Per Tahap (Bagian 5) Animation — Design Spec

**Status:** Draft — awaiting user review
**Author:** Brainstorm session 2026-05-21
**Target file:** `docs/assets/proton-video/assessment-akhir.html`
**Series context:** PROTON video bagian 5 (1:55–2:10, 15s narasi). Animasi durasi 20s (5s buffer untuk editor video splice cutaway Y3 actor).

---

## 1. Goal

Bangun animasi HTML 16:9 standalone untuk PROTON video bagian 5 (Assessment Akhir Per Tahap, durasi 20s), record menjadi `.webm` lewat Playwright. Scope HTML: **full mockup website Portal HC KPB** untuk Quiz Y1+Y2 + Result. Y3 panel juri wawancara **out-of-scope** (editor video splice actor footage).

**Quantified targets:**
- Durasi exact 20s.
- Action density ≥95%.
- 2 scene cross-fade (quiz Y1+Y2 + result).
- Visual identity match series (track-progresi + alur-pelaksanaan): navy + red + Inter + grid backdrop + spring easing.

---

## 2. Decision Log (from brainstorm)

| # | Decision | Choice |
|---|----------|--------|
| D1 | Cutaway Y3 panel juri | Out-of-scope HTML (editor splice actor footage) |
| D2 | HTML durasi | 20s (extended 15s narasi + 5s buffer) |
| D3 | Quiz scene structure | Single mockup + Y1/Y2 chip toggle progression |
| D4 | Confetti style | Hybrid burst radial + falling ambient |
| D5 | Result content | Card hasil + meta (score hero + breakdown line) |
| D6 | Series continuity | Identical tone match series (no stepper bottom, scope beda) |
| D7 | Audio sync | HTML quiz (9s) + cutaway Y3 (~4s) + result (7s) splice di editor |
| D8 | Mockup authenticity | Hybrid recognizable (browser frame + Portal HC KPB style) |
| D9 | Quiz content swap | Brief opacity fade transition (300ms) saat Y1→Y2 swap |
| D10 | Badge "LULUS" | Stamp drop spring + impact ring + glow pulse 2× (compound animation) |
| D11 | Z-index layering | card z=2, confetti z=3, pass-caption z=4 |

---

## 3. Architecture

### 3.1 File Scope

| File | Status | Tanggung jawab |
|------|--------|----------------|
| `docs/assets/proton-video/assessment-akhir.html` | CREATE | Animasi 20s (Quiz Y1+Y2 + Result), CSS+JS inline |
| `docs/assets/proton-video/record-assessment-akhir.mjs` | CREATE | Playwright record 21s, port 8769 |
| `docs/assets/proton-video/assessment-akhir.webm` | GENERATE | Output 20s, 1920×1080, target 3-5 MB |

Naming consistent dengan series (`track-progresi.*`, `alur-pelaksanaan.*`).

### 3.2 Tech Stack

CSS3 + vanilla JS inline + Inter (Google Fonts) + Playwright chromium (record) + Python http.server (serve lokal saat record).

### 3.3 Out of Scope

- Y3 panel juri wawancara cutaway (actor footage).
- Naskah audio docx update.
- Real Portal HC KPB screenshot — pakai hybrid recognizable mockup.
- Stepper component (bagian 5 scope beda dari bagian 4 "6 langkah").

---

## 4. Timeline 20s Breakdown

| Slot | Window | Konten |
|------|--------|--------|
| **Intro** | 0.0–2.0s | Stage + grid backdrop fade-in (0–0.4s). Header eyebrow + title fade-down (0.2–1.0s). Em "Per Tahap" underline draw red (1.2–1.8s). Brand mark fade-in (1.5s). |
| **Quiz Y1** | 2.0–6.5s | Cross-fade to scene-quiz. Browser frame "Portal HC KPB · Assessment Online" fade-up (2.0–2.4s). Stage chip "TAHUN 1" active scale 1.08 (2.5s). Meta strip "Sub-Kompetensi: Refinery Ops · Soal 5/10" (2.8s). Quiz card pop spring (3.2s). 4 option pilihan ganda stagger 150ms (3.6–4.2s). Cursor select option C "Mempercepat reaksi kimia" — radio fill + highlight navy (4.8s). Progress bar fill 50%→100% sweep 1.3s (5.2–6.5s). |
| **Quiz Y1→Y2 swap** | 6.5–6.8s | `.quiz-card` opacity fade 1→0 (200ms). `swapQuizContent()` JS update q-text + q-meta + reset selected option. Card opacity 0→1 (300ms total swap window). Chip Y1 transition `.active` → `.done` (✓ navy-soft). Progress bar reset 100%→0%. |
| **Quiz Y2** | 6.8–9.0s | Stage chip "TAHUN 2" active (6.8s). Quiz card content swapped: q-text "Bagaimana prinsip kerja energy management di unit refinery?", q-meta "Sub-Kompetensi: Energy Management · Soal 5/10". Progress bar fill 0%→100% (7.0–8.5s). Cursor select option B (7.5s). Chip Y2 transition `.done` (8.5s). Caption "10 soal · penilaian otomatis" fade-in (9.0s). |
| **Quiz hold** | 9.0–11.0s | Both chip ✓ done. Card + caption static. |
| **Cross-fade** | 11.0–12.0s | Scene-quiz fade-out 0.5s, scene-result fade-in 0.5s (overlap). |
| **Result intro** | 12.0–13.0s | Browser frame "Portal HC KPB · Hasil Assessment" (12.0–12.4s). Result card pop spring (12.4–13.0s). |
| **Score tween** | 13.0–14.7s | Title "HASIL ASSESSMENT" appear (13.0s). Score angka **0 → 85** rAF counter tick 1.5s easeOutCubic (13.2–14.7s). "/100" unit + "NILAI" label appear (14.0s). |
| **Meta + Badge + Confetti** | 14.7–17.0s | Meta line "8 dari 10 soal benar · Durasi 45 menit" fade-up (14.7s). **Badge "LULUS" stamp drop spring** (15.2–15.8s) + impact ring (15.6–16.2s) + glow pulse 2× (15.8–20.8s, runs into hold). **Burst confetti** 12 partikel radial expand outward (15.5–16.7s). **Falling confetti ambient** trigger (15.8s, runs 3-4s). |
| **Pass caption** | 17.5–19.0s | Caption "Nilai minimal 75 — syarat naik ke tahap berikutnya" fade-in subtle (17.5s). |
| **Outro hold** | 19.0–20.0s | Final poster: result card + badge + falling confetti residual + caption + glow pulse residual. Static. |

**Action density:** active windows (intro+quiz+swap+result+caption) = 19s / 20s = **95%** ✓ (target ≥85%).

**Audio sync untuk editor video (R7 documented):**
- HTML 20s = quiz active (2–11s, 9s) + cross-fade (11–12s) + result (12–19s, 7s) + hold (19–20s, 1s)
- Editor splice: HTML quiz scene → cutaway Y3 panel juri actor (~4s) → HTML result scene = 15s final video
- Narasi 15s overlay: paragraf 1 (Y1+Y2 ujian) di quiz scene, paragraf 2 (Y3 panel juri) di cutaway, paragraf 3 (nilai min 75) di result scene

---

## 5. Layout Structure

### 5.1 Stage 16:9 (1600px max)

```
┌────────────────────────────────────────────────────┐
│  Header bar (8% top)                                │
│  ▸ Eyebrow: BAGIAN 5 · ASSESSMENT AKHIR (red, upper)│
│  ▸ Title: Standar Penilaian Per Tahap (em red under)│
├────────────────────────────────────────────────────┤
│                                                    │
│  MAIN SCENE AREA (~92%)                             │
│  ▸ position: relative                              │
│  ▸ 2 scene cross-fade: scene-quiz, scene-result    │
│                                                    │
└────────────────────────────────────────────────────┘
[grid backdrop hairline navy 4%, z=0]
[brand mark "PROTON × KPB" bottom-right, opacity 0.5]
```

**No stepper bottom** (bagian 5 standalone scope, beda dari bagian 4 "6 langkah").

### 5.2 Header CSS (carry from series)

```css
.header {
  margin-bottom: clamp(12px, 1.5vw, 24px);
  opacity: 0;
  transform: translateY(-8px);
  animation: fadeDown 0.8s 0.2s cubic-bezier(0.2, 0, 0.2, 1) forwards;
}
.eyebrow {
  font-size: clamp(11px, 0.9vw, 14px);
  font-weight: 800;
  color: var(--red);
  letter-spacing: 0.2em;
  text-transform: uppercase;
  margin-bottom: 8px;
}
.title {
  font-size: clamp(24px, 2.2vw, 38px);
  font-weight: 900;
  color: var(--navy-deep);
  line-height: 1.1;
  letter-spacing: -0.01em;
}
.title em {
  font-style: normal;
  color: var(--red);
  background-image: linear-gradient(90deg, var(--red), var(--red));
  background-size: 0% 6px;
  background-position: 0 92%;
  background-repeat: no-repeat;
  padding-bottom: 2px;
  animation: underlineDraw 0.6s 1.2s cubic-bezier(0.65, 0, 0.35, 1) forwards;
}
@keyframes underlineDraw { to { background-size: 100% 6px; } }
@keyframes fadeDown { to { opacity: 1; transform: translateY(0); } }
@keyframes fadeIn { to { opacity: 1; } }
@keyframes fadeUp { to { opacity: 1; transform: translateY(0); } }
```

### 5.3 Scene Container + Cross-Fade

```css
.scenes {
  flex: 1;
  position: relative;
  min-height: 0;
  margin: clamp(8px, 1vw, 16px) 0;
}
.scene {
  position: absolute;
  inset: 0;
  opacity: 0;
  transform: translateY(8px);
  transition: opacity 0.5s ease, transform 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
  pointer-events: none;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: clamp(12px, 1.5vw, 24px);
}
.scene.active {
  opacity: 1;
  transform: translateY(0);
}
```

### 5.4 Scene Quiz Layout

Browser frame "Portal HC KPB · Assessment Online", body content:

```
┌─ Stage Chips Top (horizontal row) ──────────────────┐
│ [TAHUN 1]  [TAHUN 2]                                │
├─────────────────────────────────────────────────────┤
│ Meta strip                                           │
│ Sub-Kompetensi: Refinery Ops · Soal 5/10             │
├─────────────────────────────────────────────────────┤
│ Quiz Card                                            │
│  Pertanyaan                                          │
│  Apa fungsi utama refinery catalyst dalam            │
│  proses cracking?                                    │
│                                                      │
│   ○ A) Menyimpan crude oil                          │
│   ○ B) Meningkatkan suhu boiler                     │
│   ● C) Mempercepat reaksi kimia ← selected           │
│   ○ D) Memurnikan air pendingin                     │
├─────────────────────────────────────────────────────┤
│ Progress: ▓▓▓▓▓░░░░░ 5/10                           │
│ Caption (bottom, fade-in t=9s):                      │
│ 10 soal · penilaian otomatis                         │
└─────────────────────────────────────────────────────┘
```

**Quiz text Y1:** "Apa fungsi utama refinery catalyst dalam proses cracking?" — neutral teknis, no controversy.
**Quiz text Y2:** "Bagaimana prinsip kerja energy management di unit refinery?" — match track-progresi Y2 sub-kompetensi "Energy Management".

### 5.5 Scene Result Layout

Browser frame "Portal HC KPB · Hasil Assessment", body content:

```
┌─ Result Card (centered, z=2) ───────────────────────┐
│                                                     │
│              HASIL ASSESSMENT                       │
│                                                     │
│                ┌──────────┐                         │
│                │    85    │  /100                   │
│                └──────────┘                         │
│                   NILAI                             │
│                                                     │
│              ┌─ Badge LULUS ─┐                      │
│              │   ✓ LULUS     │ ← stamp drop spring  │
│              └───────────────┘                      │
│                                                     │
│   8 dari 10 soal benar · Durasi 45 menit            │
│                                                     │
└─────────────────────────────────────────────────────┘

[confetti-burst 12 partikel radial from card center, z=3]
[confetti-fall 24 flake ambient top→bottom, z=3]

Pass caption bottom (z=4):
   Nilai minimal 75 — syarat naik ke tahap berikutnya
```

---

## 6. Motion Catalog

### 6.1 Reused Primitives (from series)

| Primitive | Source | Usage |
|-----------|--------|-------|
| Grid backdrop static | track-progresi | Stage z=0 |
| Stage shadow + border | track-progresi | Stage container |
| Header fade-down + em underline | series | Header intro |
| Brand mark fade-in | series | Bottom-right |
| Browser frame component | alur-pelaksanaan | Quiz + Result body |
| Card pop spring | series | Quiz card + Result card |
| Cross-fade scene transition | alur-pelaksanaan | Quiz → Result |
| Progress bar sweep | alur-pelaksanaan | Quiz progress 0→100% |
| Counter rAF tween | track-progresi | Score 0→85 |

### 6.2 Stage Chip Y1/Y2 Toggle

```css
.stage-chips {
  display: flex;
  gap: clamp(8px, 1vw, 16px);
  margin-bottom: clamp(12px, 1.4vw, 20px);
}
.stage-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 14px;
  border-radius: 20px;
  background: var(--paper);
  border: 1px solid var(--hairline);
  font-size: clamp(11px, 0.95vw, 14px);
  font-weight: 700;
  color: var(--muted);
  letter-spacing: 0.08em;
  text-transform: uppercase;
  transition: all 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.stage-chip.active {
  background: var(--navy);
  border-color: var(--navy);
  color: #FFFFFF;
  transform: scale(1.08);
}
.stage-chip.done {
  background: var(--navy-soft);
  border-color: var(--navy-soft);
  color: #FFFFFF;
  transform: scale(1);
}
.stage-chip.done::before {
  content: "✓ ";
  font-weight: 900;
}
```

### 6.3 Quiz Body — Card + Meta + Options + Progress

```css
.quiz-body {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.quiz-meta {
  font-size: clamp(11px, 0.9vw, 13px);
  font-weight: 700;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.08em;
  opacity: 0;
  transform: translateY(6px);
}
#scene-quiz.active .quiz-meta { animation: fadeUp 0.4s 0.8s ease-out forwards; }

.quiz-card {
  padding: clamp(14px, 1.6vw, 22px);
  background: #FFFFFF;
  border: 1px solid var(--hairline);
  border-radius: 8px;
  opacity: 0;
  transform: translateY(8px);
  transition: opacity 0.3s ease;
}
#scene-quiz.active .quiz-card { animation: fadeUp 0.5s 1.2s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }

.q-label {
  font-size: clamp(10px, 0.85vw, 12px);
  font-weight: 800;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.1em;
  margin-bottom: 6px;
}
.q-text {
  font-size: clamp(13px, 1.15vw, 16px);
  font-weight: 700;
  color: var(--navy-deep);
  margin-bottom: 12px;
}

.options {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.opt {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background: #FFFFFF;
  border: 1px solid var(--hairline);
  border-radius: 6px;
  font-size: clamp(11px, 0.95vw, 13px);
  color: var(--text);
  opacity: 0;
  transform: translateY(4px);
  transition: border-color 0.3s ease, background 0.3s ease, font-weight 0.3s ease;
}
#scene-quiz.active .opt:nth-of-type(1) { animation: fadeUp 0.3s 1.6s ease-out forwards; }
#scene-quiz.active .opt:nth-of-type(2) { animation: fadeUp 0.3s 1.75s ease-out forwards; }
#scene-quiz.active .opt:nth-of-type(3) { animation: fadeUp 0.3s 1.90s ease-out forwards; }
#scene-quiz.active .opt:nth-of-type(4) { animation: fadeUp 0.3s 2.05s ease-out forwards; }
.opt.selected {
  border-color: var(--navy);
  background: rgba(15,45,92,0.06);
  font-weight: 700;
  color: var(--navy-deep);
}
.opt input { accent-color: var(--navy); }

.progress-strip {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 6px;
}
.p-label {
  font-size: clamp(10px, 0.85vw, 12px);
  font-weight: 700;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}
.progress-strip .progress-bar { flex: 1; }
.p-count {
  font-size: clamp(11px, 0.9vw, 13px);
  font-weight: 700;
  color: var(--navy-deep);
  font-variant-numeric: tabular-nums;
}

.progress-bar {
  height: 8px;
  background: var(--hairline);
  border-radius: 4px;
  overflow: hidden;
  width: 100%;
}
.progress-bar .fill {
  height: 100%;
  background: var(--navy);
  width: 0%;
  transition: width 1.3s cubic-bezier(0.65, 0, 0.35, 1);
}
.progress-bar.active .fill { width: var(--target, 100%); }

.quiz-caption {
  margin-top: 8px;
  text-align: center;
  font-size: clamp(11px, 0.9vw, 13px);
  font-weight: 700;
  color: var(--muted);
  letter-spacing: 0.08em;
  text-transform: uppercase;
  opacity: 0;
  transition: opacity 0.5s ease;
}
.quiz-caption.show { opacity: 1; }
```

### 6.4 Result Body — Card + Score + Badge + Confetti

```css
.result-body {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
}
.result-card {
  position: relative;
  z-index: 2;
  width: clamp(360px, 38vw, 540px);
  padding: clamp(24px, 2.4vw, 36px);
  background: #FFFFFF;
  border: 1px solid var(--hairline);
  border-radius: 12px;
  text-align: center;
  box-shadow: 0 1px 2px rgba(10,36,71,0.03), 0 12px 32px rgba(10,36,71,0.08);
  opacity: 0;
  transform: translateY(12px) scale(0.96);
}
#scene-result.active .result-card { animation: cardPop 0.6s 0.4s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
@keyframes cardPop {
  0%   { opacity: 0; transform: translateY(12px) scale(0.96); }
  100% { opacity: 1; transform: translateY(0) scale(1); }
}

.r-title {
  font-size: clamp(12px, 1vw, 14px);
  font-weight: 800;
  color: var(--navy-deep);
  text-transform: uppercase;
  letter-spacing: 0.15em;
  opacity: 0;
}
#scene-result.active .r-title { animation: fadeUp 0.4s 1.0s ease-out forwards; }

.score-block {
  display: flex;
  align-items: baseline;
  justify-content: center;
  gap: 8px;
  margin: clamp(16px, 2vw, 24px) 0 4px;
}
.score-num {
  font-size: clamp(72px, 8vw, 120px);
  font-weight: 900;
  color: var(--navy-deep);
  font-variant-numeric: tabular-nums;
  line-height: 1;
}
.score-unit {
  font-size: clamp(20px, 2vw, 32px);
  font-weight: 700;
  color: var(--muted);
}
.score-label {
  font-size: clamp(11px, 0.9vw, 13px);
  font-weight: 800;
  color: var(--navy-soft);
  letter-spacing: 0.2em;
  text-transform: uppercase;
  margin-bottom: clamp(14px, 1.6vw, 22px);
  opacity: 0;
}
#scene-result.active .score-label { animation: fadeIn 0.4s 2.0s ease-out forwards; }

.badge-lulus {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 10px 28px;
  background: #FFFFFF;
  border: 3px solid var(--red);
  border-radius: 999px;
  font-size: clamp(18px, 1.8vw, 28px);
  font-weight: 900;
  color: var(--red);
  letter-spacing: 0.15em;
  opacity: 0;
  transform: translateY(-40px) rotate(-15deg) scale(0.5);
  position: relative;
}
.badge-lulus .b-tick {
  font-size: 1.1em;
  color: var(--red);
}
.badge-lulus.fired {
  animation:
    badgeDrop 0.6s cubic-bezier(0.34, 1.56, 0.64, 1) forwards,
    badgeGlow 2.5s 0.6s ease-out 2;
}
@keyframes badgeDrop {
  60%  { opacity: 1; transform: translateY(8px) rotate(2deg) scale(1.1); }
  100% { opacity: 1; transform: translateY(0) rotate(-3deg) scale(1); }
}
@keyframes badgeGlow {
  0%, 100% { box-shadow: 0 0 0 4px rgba(230,51,41,0.15); }
  50%      { box-shadow: 0 0 0 16px rgba(230,51,41,0.06); }
}
.badge-lulus.fired::after {
  content: "";
  position: absolute;
  inset: -8px;
  border: 3px solid var(--red);
  border-radius: 999px;
  opacity: 0;
  animation: badgeImpact 0.6s ease-out 0.4s forwards;
}
@keyframes badgeImpact {
  0%   { opacity: 0.9; transform: scale(0.7); }
  100% { opacity: 0;   transform: scale(1.5); }
}

.result-meta {
  margin-top: clamp(14px, 1.6vw, 22px);
  font-size: clamp(11px, 0.95vw, 14px);
  font-weight: 600;
  color: var(--muted);
  opacity: 0;
  transform: translateY(6px);
  transition: opacity 0.4s ease, transform 0.4s ease;
}
.result-meta.show { opacity: 1; transform: translateY(0); }

.pass-caption {
  position: absolute;
  bottom: clamp(8px, 1vw, 16px);
  left: 50%;
  transform: translateX(-50%);
  z-index: 4;
  font-size: clamp(11px, 0.9vw, 13px);
  font-weight: 700;
  color: var(--navy-soft);
  letter-spacing: 0.1em;
  text-transform: uppercase;
  opacity: 0;
  text-align: center;
  white-space: nowrap;
  transition: opacity 0.6s ease;
}
.pass-caption.show { opacity: 1; }
```

### 6.5 Confetti Burst (12 partikel radial)

```css
.confetti-burst {
  position: absolute;
  top: 50%; left: 50%;
  width: 0; height: 0;
  pointer-events: none;
  z-index: 3;
}
.confetti-burst .particle {
  position: absolute;
  width: clamp(8px, 0.8vw, 12px);
  height: clamp(8px, 0.8vw, 12px);
  opacity: 0;
  margin-left: -6px;
  margin-top: -6px;
}
.confetti-burst .particle:nth-child(1)  { --angle: 0deg;   background: var(--red);       }
.confetti-burst .particle:nth-child(2)  { --angle: 30deg;  background: var(--navy);      }
.confetti-burst .particle:nth-child(3)  { --angle: 60deg;  background: #FFC107;  border-radius: 50%; }
.confetti-burst .particle:nth-child(4)  { --angle: 90deg;  background: var(--red);       }
.confetti-burst .particle:nth-child(5)  { --angle: 120deg; background: var(--navy-soft); }
.confetti-burst .particle:nth-child(6)  { --angle: 150deg; background: #FFC107;  border-radius: 50%; }
.confetti-burst .particle:nth-child(7)  { --angle: 180deg; background: var(--red);       }
.confetti-burst .particle:nth-child(8)  { --angle: 210deg; background: var(--navy);      }
.confetti-burst .particle:nth-child(9)  { --angle: 240deg; background: #FFC107;  border-radius: 50%; }
.confetti-burst .particle:nth-child(10) { --angle: 270deg; background: var(--red);       }
.confetti-burst .particle:nth-child(11) { --angle: 300deg; background: var(--navy-soft); }
.confetti-burst .particle:nth-child(12) { --angle: 330deg; background: #FFC107;  border-radius: 50%; }

.confetti-burst.fired .particle {
  animation: confettiBurst 1.2s ease-out forwards;
}
@keyframes confettiBurst {
  0%   { opacity: 0.9; transform: rotate(var(--angle)) translateX(0) rotate(0deg); }
  100% { opacity: 0;   transform: rotate(var(--angle)) translateX(clamp(120px, 14vw, 220px)) rotate(360deg); }
}
```

### 6.6 Confetti Fall (24 flake ambient)

```css
.confetti-fall {
  position: absolute;
  inset: 0;
  pointer-events: none;
  overflow: hidden;
  z-index: 3;
}
.confetti-fall .flake {
  position: absolute;
  top: -20px;
  width: clamp(6px, 0.6vw, 10px);
  height: clamp(6px, 0.6vw, 10px);
  opacity: 0;
}
.confetti-fall .flake:nth-child(3n+1)  { background: var(--red); }
.confetti-fall .flake:nth-child(3n+2)  { background: var(--navy); border-radius: 50%; }
.confetti-fall .flake:nth-child(3n)    { background: #FFC107; }

/* 24 flake positions + delays */
.confetti-fall.active .flake:nth-child(1)  { left: 4%;  animation: confettiFall 3.5s 0.0s ease-in forwards; }
.confetti-fall.active .flake:nth-child(2)  { left: 10%; animation: confettiFall 3.8s 0.15s ease-in forwards; }
.confetti-fall.active .flake:nth-child(3)  { left: 16%; animation: confettiFall 3.2s 0.30s ease-in forwards; }
.confetti-fall.active .flake:nth-child(4)  { left: 22%; animation: confettiFall 3.6s 0.10s ease-in forwards; }
.confetti-fall.active .flake:nth-child(5)  { left: 28%; animation: confettiFall 3.3s 0.45s ease-in forwards; }
.confetti-fall.active .flake:nth-child(6)  { left: 34%; animation: confettiFall 3.9s 0.20s ease-in forwards; }
.confetti-fall.active .flake:nth-child(7)  { left: 40%; animation: confettiFall 3.4s 0.55s ease-in forwards; }
.confetti-fall.active .flake:nth-child(8)  { left: 46%; animation: confettiFall 3.7s 0.35s ease-in forwards; }
.confetti-fall.active .flake:nth-child(9)  { left: 52%; animation: confettiFall 3.5s 0.65s ease-in forwards; }
.confetti-fall.active .flake:nth-child(10) { left: 58%; animation: confettiFall 3.8s 0.05s ease-in forwards; }
.confetti-fall.active .flake:nth-child(11) { left: 64%; animation: confettiFall 3.2s 0.75s ease-in forwards; }
.confetti-fall.active .flake:nth-child(12) { left: 70%; animation: confettiFall 3.6s 0.25s ease-in forwards; }
.confetti-fall.active .flake:nth-child(13) { left: 76%; animation: confettiFall 3.3s 0.85s ease-in forwards; }
.confetti-fall.active .flake:nth-child(14) { left: 82%; animation: confettiFall 3.9s 0.40s ease-in forwards; }
.confetti-fall.active .flake:nth-child(15) { left: 88%; animation: confettiFall 3.4s 0.95s ease-in forwards; }
.confetti-fall.active .flake:nth-child(16) { left: 94%; animation: confettiFall 3.7s 0.50s ease-in forwards; }
.confetti-fall.active .flake:nth-child(17) { left: 8%;  animation: confettiFall 3.5s 1.05s ease-in forwards; }
.confetti-fall.active .flake:nth-child(18) { left: 20%; animation: confettiFall 3.8s 1.15s ease-in forwards; }
.confetti-fall.active .flake:nth-child(19) { left: 32%; animation: confettiFall 3.2s 1.25s ease-in forwards; }
.confetti-fall.active .flake:nth-child(20) { left: 44%; animation: confettiFall 3.6s 1.20s ease-in forwards; }
.confetti-fall.active .flake:nth-child(21) { left: 56%; animation: confettiFall 3.3s 1.30s ease-in forwards; }
.confetti-fall.active .flake:nth-child(22) { left: 68%; animation: confettiFall 3.9s 1.35s ease-in forwards; }
.confetti-fall.active .flake:nth-child(23) { left: 80%; animation: confettiFall 3.4s 1.40s ease-in forwards; }
.confetti-fall.active .flake:nth-child(24) { left: 92%; animation: confettiFall 3.7s 1.45s ease-in forwards; }

@keyframes confettiFall {
  0%   { opacity: 0; transform: translateY(-20px) rotate(0deg); }
  10%  { opacity: 1; }
  90%  { opacity: 1; }
  100% { opacity: 0; transform: translateY(clamp(300px, 35vw, 500px)) rotate(540deg); }
}
```

### 6.7 Grid Backdrop + Brand Mark (carry)

```css
.stage::before {
  content: "";
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(rgba(15,45,92,0.04) 1px, transparent 1px),
    linear-gradient(90deg, rgba(15,45,92,0.04) 1px, transparent 1px);
  background-size: 40px 40px;
  pointer-events: none;
  opacity: 0;
  animation: fadeIn 0.4s 0.1s ease-out forwards;
  z-index: 0;
}
.stage > * { position: relative; z-index: 1; }
.brand-mark {
  position: absolute;
  bottom: 20px;
  right: 28px;
  font-size: clamp(10px, 0.78vw, 12px);
  font-weight: 700;
  letter-spacing: 0.2em;
  color: var(--muted);
  opacity: 0;
  animation: fadeIn 0.8s 1.5s ease-out forwards;
  pointer-events: none;
}
```

---

## 7. Markup Structure

```html
<body>
  <div class="stage">
    <!-- Header -->
    <div class="header">
      <div class="eyebrow">Bagian 5 · Assessment Akhir</div>
      <h1 class="title">Standar Penilaian <em>Per Tahap</em></h1>
    </div>

    <!-- Scenes -->
    <div class="scenes">

      <!-- Scene Quiz Y1+Y2 -->
      <div class="scene" id="scene-quiz">
        <div class="browser-frame">
          <div class="browser-bar">
            <span class="dots"><i></i><i></i><i></i></span>
            <span class="url">portalhc.kpb · <em>Assessment Online</em></span>
          </div>
          <div class="browser-body quiz-body">
            <div class="stage-chips">
              <span class="stage-chip" id="chip-y1">Tahun 1</span>
              <span class="stage-chip" id="chip-y2">Tahun 2</span>
            </div>
            <div class="quiz-meta" id="q-meta">Sub-Kompetensi: Refinery Ops · Soal 5/10</div>
            <div class="quiz-card">
              <div class="q-label">Pertanyaan</div>
              <div class="q-text" id="q-text">Apa fungsi utama refinery catalyst dalam proses cracking?</div>
              <div class="options">
                <label class="opt"><input type="radio" disabled> A) Menyimpan crude oil</label>
                <label class="opt"><input type="radio" disabled> B) Meningkatkan suhu boiler</label>
                <label class="opt"><input type="radio" disabled> C) Mempercepat reaksi kimia</label>
                <label class="opt"><input type="radio" disabled> D) Memurnikan air pendingin</label>
              </div>
            </div>
            <div class="progress-strip">
              <span class="p-label">Progres</span>
              <div class="progress-bar" id="quiz-progress"><div class="fill"></div></div>
              <span class="p-count">10 / 10</span>
            </div>
            <div class="quiz-caption" id="quiz-caption">10 soal · penilaian otomatis</div>
          </div>
        </div>
      </div>

      <!-- Scene Result -->
      <div class="scene" id="scene-result">
        <div class="browser-frame">
          <div class="browser-bar">
            <span class="dots"><i></i><i></i><i></i></span>
            <span class="url">portalhc.kpb · <em>Hasil Assessment</em></span>
          </div>
          <div class="browser-body result-body">
            <div class="confetti-burst" aria-hidden="true">
              <span class="particle"></span><span class="particle"></span>
              <span class="particle"></span><span class="particle"></span>
              <span class="particle"></span><span class="particle"></span>
              <span class="particle"></span><span class="particle"></span>
              <span class="particle"></span><span class="particle"></span>
              <span class="particle"></span><span class="particle"></span>
            </div>
            <div class="confetti-fall" aria-hidden="true">
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
            </div>
            <div class="result-card">
              <div class="r-title">Hasil Assessment</div>
              <div class="score-block">
                <span class="score-num" id="score-num">0</span>
                <span class="score-unit">/100</span>
              </div>
              <div class="score-label">NILAI</div>
              <div class="badge-lulus">
                <span class="b-tick">✓</span>
                <span class="b-text">LULUS</span>
              </div>
              <div class="result-meta" id="result-meta">8 dari 10 soal benar · Durasi 45 menit</div>
            </div>
            <div class="pass-caption" id="pass-caption">Nilai minimal 75 — syarat naik ke tahap berikutnya</div>
          </div>
        </div>
      </div>

    </div>

    <!-- Brand mark -->
    <div class="brand-mark">PROTON × KPB</div>
  </div>

  <script>
    // orchestration
  </script>
</body>
```

---

## 8. JS Orchestration

```javascript
// ===== ASSESSMENT AKHIR — ORCHESTRATION =====

function showScene(id) {
  document.querySelectorAll('.scene').forEach(s => s.classList.remove('active'));
  document.getElementById(id).classList.add('active');
}

function setChip(id, state) {
  const el = document.getElementById(id);
  el.classList.remove('active', 'done');
  state.split(' ').forEach(c => el.classList.add(c));
}

function fillProgress(id, target) {
  const el = document.getElementById(id);
  el.style.setProperty('--target', target + '%');
  el.classList.add('active');
}

function resetProgress(id) {
  const el = document.getElementById(id);
  el.classList.remove('active');
  el.style.setProperty('--target', '0%');
}

function tweenCounter(id, from, to, duration) {
  const el = document.getElementById(id);
  const start = performance.now();
  function frame(now) {
    const p = Math.min(1, (now - start) / duration);
    const eased = 1 - Math.pow(1 - p, 3);
    el.textContent = Math.round(from + (to - from) * eased);
    if (p < 1) requestAnimationFrame(frame);
  }
  requestAnimationFrame(frame);
}

function fireBadge() {
  document.querySelector('.badge-lulus').classList.add('fired');
  document.querySelector('.confetti-burst').classList.add('fired');
  setTimeout(() => document.querySelector('.confetti-fall').classList.add('active'), 300);
}

function selectOption(idx) {
  document.querySelectorAll('#scene-quiz .opt').forEach(o => o.classList.remove('selected'));
  document.querySelectorAll('#scene-quiz .opt')[idx].classList.add('selected');
}

function swapQuizContent() {
  const card = document.querySelector('#scene-quiz .quiz-card');
  card.style.opacity = '0';
  setTimeout(() => {
    document.getElementById('q-text').textContent = 'Bagaimana prinsip kerja energy management di unit refinery?';
    document.getElementById('q-meta').textContent = 'Sub-Kompetensi: Energy Management · Soal 5/10';
    document.querySelectorAll('#scene-quiz .opt').forEach(o => o.classList.remove('selected'));
    card.style.opacity = '1';
  }, 200);
}

const milestones = [
  // Quiz Y1 (2-6.5s)
  { t: 2000, fn: () => { showScene('scene-quiz'); setChip('chip-y1', 'active'); } },
  { t: 4800, fn: () => selectOption(2) },
  { t: 5200, fn: () => fillProgress('quiz-progress', 100) },

  // Y1 done → Y2 active (6.5-8.5s)
  { t: 6500, fn: () => { setChip('chip-y1', 'done'); resetProgress('quiz-progress'); swapQuizContent(); } },
  { t: 6800, fn: () => setChip('chip-y2', 'active') },
  { t: 7000, fn: () => fillProgress('quiz-progress', 100) },
  { t: 7500, fn: () => selectOption(1) },

  // Y2 done + caption (8.5-9s)
  { t: 8500, fn: () => setChip('chip-y2', 'done') },
  { t: 9000, fn: () => document.getElementById('quiz-caption').classList.add('show') },

  // Cross-fade to Result (11-12s)
  { t: 11000, fn: () => showScene('scene-result') },

  // Score tween (13.2-14.7s)
  { t: 13200, fn: () => tweenCounter('score-num', 0, 85, 1500) },

  // Meta line (14.7s)
  { t: 14700, fn: () => document.getElementById('result-meta').classList.add('show') },

  // Badge + confetti (15.2s)
  { t: 15200, fn: () => fireBadge() },

  // Pass criteria caption (17.5s)
  { t: 17500, fn: () => document.getElementById('pass-caption').classList.add('show') }
];

const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
if (reduced) {
  showScene('scene-result');
  document.getElementById('score-num').textContent = '85';
  document.getElementById('result-meta').classList.add('show');
  document.querySelector('.badge-lulus').classList.add('fired');
  document.querySelector('.confetti-burst').classList.add('fired');
  document.querySelector('.confetti-fall').classList.add('active');
  document.getElementById('pass-caption').classList.add('show');
} else {
  milestones.forEach(m => setTimeout(m.fn, m.t));
}
```

**JS budget total:** ~75 LOC. Helpers + 13-entry milestones + reduced-motion branch.

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
  .scene#scene-result { opacity: 1; transform: none; }
  .title em { background-size: 100% 6px; }
  .result-card, .result-meta, .pass-caption { opacity: 1; transform: none; }
}
```

JS branch: skip setTimeout chain, langsung set final state (showScene result, score 85, badge fired, confetti both, meta+caption shown).

---

## 10. Risks & Mitigations

| # | Risk | Likelihood | Mitigation |
|---|------|-----------|------------|
| R1 | 36 partikel confetti = render heavy, jank di Playwright | Low | CSS-only GPU-accelerated transform. Verify record. |
| R2 | Confetti fall distance hard-coded clamp px — partikel hilang sebelum bottom atau overflow | Med | Test 1920×1080 Playwright fixed viewport. Stage 16:9 konsisten. |
| R3 | Quiz scene swap Y1→Y2 visual jump tanpa fade | Mitigated | `swapQuizContent()` JS opacity fade 1→0→1 300ms window (Section 8) |
| R4 | Badge stamp drop di tengah confetti burst = visual clash | Low | Z-index spec: badge z=2 (di dalam result-card), confetti z=3 di belakang/depan. Card layer above burst spawn point. |
| R5 | Score tween 0→85 1.5s mungkin terasa lambat untuk 7s result scene | Low | Pacing fine. Count perlu nafas baca. |
| R6 | File size HTML besar (36 partikel + CSS nth-child) | Med | Target <30 KB. Realistic estimasi 28-33 KB. |
| R7 | Naskah audio 15s vs HTML 20s mismatch confuse editor | Med | Section 4 audio sync window documented eksplisit |
| R8 | Badge fired sibling selector bug | Mitigated | Section 6.4 use compound animation langsung di `.badge-lulus.fired` (no sibling needed) |
| R9 | Pass caption tertutup confetti fall | Mitigated | Section 6.4 + 6.6: pass-caption z=4, confetti z=3 |
| R10 | Action density 95% — terlalu padat, tidak ada nafas | Low | 1s hold di outro 19-20s + transition cross-fade 11-12s memberikan visual rest |

---

## 11. Testing Checklist

- [ ] Local browser refresh — visual 20s end-to-end smooth
- [ ] Quiz Y1 chip active → done ✓ transition smooth
- [ ] Quiz content swap Y1 ("refinery catalyst") → Y2 ("energy management") via opacity fade
- [ ] Progress bar fill 0→100% sweep × 2 (Y1 + Y2)
- [ ] Option C selected Y1, option B selected Y2
- [ ] Cross-fade quiz → result scene smooth
- [ ] Result card pop spring
- [ ] Score counter 0 → 85 smooth tabular nums
- [ ] Badge "LULUS" stamp drop + impact ring + glow pulse 2×
- [ ] Confetti burst 12 partikel radial expand outward
- [ ] Confetti fall 24 flake ambient 3-4s
- [ ] Pass caption "Nilai minimal 75" muncul t=17.5s, di atas confetti
- [ ] Em "Per Tahap" underline draw red
- [ ] DevTools `prefers-reduced-motion: reduce` → static final state visible
- [ ] No console errors
- [ ] Playwright record 21s → output `.webm` 20-21s, 3-5 MB

---

## 12. Success Criteria

| # | Criterion | Verify |
|---|-----------|--------|
| SC1 | Durasi 20s, action density ≥95% | Stopwatch + manual review |
| SC2 | Quiz scene Y1 active → Y1 done ✓ → Y2 active → Y2 done ✓ chip transition | Visual + DevTools class inspect |
| SC3 | Quiz content swap Y1 → Y2 dengan opacity fade smooth | Visual text check |
| SC4 | Progress bar fill 0→100% sweep × 2 | Visual |
| SC5 | Cross-fade quiz → result scene smooth | Visual transition 11-12s |
| SC6 | Result card pop spring | Visual |
| SC7 | Score counter 0 → 85 smooth rAF tween | DevTools final state = 85 |
| SC8 | Badge "LULUS" stamp drop + impact ring + glow pulse 2× | Visual badge animation |
| SC9 | Confetti burst 12 partikel radial outward | Visual t=15.5s |
| SC10 | Confetti fall 24 partikel ambient 3-4s | Visual t=15.8-19s |
| SC11 | Pass caption visible above confetti (z=4) | Visual t=17.5s |
| SC12 | Palette navy + red + Inter + grid backdrop match series | Side-by-side visual |
| SC13 | Em "Per Tahap" underline draw red | Visual |
| SC14 | Browser frame "Portal HC KPB · Assessment Online" + "Hasil Assessment" | Visual |
| SC15 | `prefers-reduced-motion: reduce` → scene-result static visible | DevTools simulate |
| SC16 | File HTML <30 KB | `wc -c` |
| SC17 | `.webm` 20-21s, 3-5 MB, no glitch | Playback |
| SC18 | Recordable idempotent | Re-run |
| SC19 | Naskah docx tidak berubah | `git status` |
| SC20 | Audio sync window documented (Section 4) | Spec exists |

---

## 13. Task Breakdown (preview for writing-plans)

| # | Task | Files |
|---|------|-------|
| 0 | Setup Playwright | env |
| 1 | Scaffold HTML — stage + palette + grid + header + brand mark | assessment-akhir.html |
| 2 | Scene container + cross-fade transition CSS | assessment-akhir.html |
| 3 | Browser-frame reusable CSS | assessment-akhir.html |
| 4 | Scene Quiz Y1+Y2 (markup + stage chip + meta + card + options + progress + caption) | assessment-akhir.html |
| 5 | Scene Result (markup + result card + score block + score label + badge + meta + pass caption) | assessment-akhir.html |
| 6 | Confetti burst (12 particle + radial keyframes) | assessment-akhir.html |
| 7 | Confetti fall (24 flake + falling keyframes + nth-child positions) | assessment-akhir.html |
| 8 | JS orchestration — milestones + helpers (showScene, setChip, fillProgress, resetProgress, tweenCounter, fireBadge, selectOption, swapQuizContent) | assessment-akhir.html |
| 9 | `prefers-reduced-motion` fallback | assessment-akhir.html |
| 10 | Record script `record-assessment-akhir.mjs` (port 8769) | record-assessment-akhir.mjs |
| 11 | Run record + verify webm | assessment-akhir.webm |
| 12 | Spec compliance final check | (verification) |

13 atomic task (Task 0 + 1-12).

---

## 14. Definition of Done

- [ ] All 20 SC pass
- [ ] 12 atomic commits (Task 1-12, Task 0 no commit)
- [ ] `assessment-akhir.html` <30 KB
- [ ] `assessment-akhir.webm` 3-5 MB, 20-21s
- [ ] `record-assessment-akhir.mjs` idempotent
- [ ] Naskah docx tidak di-touch
- [ ] `git status` bersih
- [ ] Spec updated kalau ada deviasi
- [ ] DevTools `prefers-reduced-motion: reduce` simulate → static layout immediate

---

## 15. Rollback Strategy

3 file baru: `assessment-akhir.html`, `record-assessment-akhir.mjs`, `assessment-akhir.webm`. Kalau gagal:

```bash
git log --oneline docs/assets/proton-video/assessment-akhir.html
git revert <task1-commit>..<lastcommit>
```

---

## 16. Audio Sync Note (untuk Editor Video)

Animasi HTML durasi 20s, narasi 15s. Mapping:

| Slot | HTML | Editor Final Video (1:55-2:10, 15s) |
|------|------|-------------------------------------|
| 0.0–2.0s | Intro animasi | Lead-in (boleh skip atau pakai sebagai title appear) |
| 2.0–11.0s (9s) | Quiz Y1+Y2 scene | Narasi paragraf 1 "Y1 + Y2 ujian online" (~5s) overlay |
| 11.0–12.0s | Cross-fade | Editor cut transition |
| —    | Editor insert cutaway Y3 actor (~4s) | Narasi paragraf 2 "Y3 wawancara panel" overlay |
| 12.0–19.0s (7s) | Result scene | Narasi paragraf 3 "Nilai minimal 75" overlay |
| 19.0–20.0s | Outro hold | Optional tail buffer |

Editor splice cutaway Y3 antara HTML quiz dan HTML result. Quiz scene berakhir t=11s di HTML, cutaway 4s, lalu result scene mulai t=12s di HTML.
