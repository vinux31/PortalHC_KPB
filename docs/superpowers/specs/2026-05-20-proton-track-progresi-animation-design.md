# Design — Animasi PROTON Bagian 3: Track & Progresi Tahunan

**Date:** 2026-05-20
**Author:** Brainstorm session (PROTON video segmen 3)
**Status:** Draft — awaiting user review

## 1. Konteks

Naskah video PROTON (`docs/Naskah Video PROTON.docx`) bagian 3 menjelaskan dua track keahlian (Panelman dan Operator) berjalan selama tiga tahun dengan deliverable dan kompetensi yang berbeda di setiap tahap.

Durasi segmen di naskah: **0:50 – 1:15** (25 detik).

Output animasi akan di-record menjadi `.webm` dan disisipkan ke video utama PROTON.

### Naskah visual (referensi)

- Dua ikon besar bersanding: Panelman & Operator. Garis vertikal pemisah.
- Di bawah masing-masing, timeline 3 tahap horizontal (Tahun 1, Tahun 2, Tahun 3) dengan badge warna berbeda.
- Tahun 1: ikon foundation/basic. Tahun 2: ikon advanced/process. Tahun 3: ikon mastery/optimization.
- Indikator panah "lulus, naik tahun" antara tahap.

### Naskah narasi (referensi)

> "PROTON memiliki dua track keahlian utama: Panelman dan Operator. Masing-masing track berjalan selama tiga tahun dengan deliverable dan kompetensi yang berbeda di setiap tahap. Tahun pertama fokus pada pengenalan dan praktik dasar. Tahun kedua pada pendalaman proses dan kemandirian. Tahun ketiga pada validasi kompetensi tingkat mahir. Setiap pekerja harus menyelesaikan satu tahap sebelum melanjutkan ke tahap berikutnya."

## 2. Decisions (hasil brainstorming)

| Aspek | Pilihan | Alasan |
|-------|---------|--------|
| Audience | Sosialisasi umum pegawai | Tone hangat, mengajak, branded |
| Content depth | Medium — 5 nama kompetensi level 1 (tanpa sub-kompetensi) | Cukup informatif tanpa overload non-teknis |
| Human element | Tanpa karakter (typographic + ikon abstrak) | Clean, brand-safe |
| Pacing | Single screen accumulate (intro → Y1 → Y2 → Y3 → hold) | 25 detik tight; final frame = poster lengkap |
| File approach | Fresh file baru, hiraukan `v-G.html`/`v-H.html`/`animation.html` existing | User direction |

## 3. Mapping kompetensi per tahun

Sumber: matrix kompetensi CPDP (image referensi user).

Catatan penting: **nama kompetensi level 1 (5 kompetensi) identik antara Panelman & Operator**. Perbedaan ada di level 2 (sub-kompetensi) yang TIDAK ditampilkan di animasi ini. Maka chip kompetensi muncul sekali per tahun, berlaku untuk kedua track.

| Tahun | Tema | Kompetensi (level 1) |
|-------|------|----------------------|
| Tahun 1 | Foundation — pengenalan & praktik dasar | Safe Work Practice & Lifesaving Rules · Refinery Process Operations (basic: BOC/BEC, Feed & Product Spec, P&ID, Start-up/Shutdown, Pengoperasian Fasilitas, Risk ID) |
| Tahun 2 | Pendalaman — proses & kemandirian | Energy Management · Catalyst & Chemical Management · Refinery Process Operations (sub-proses: Pengoperasian Peralatan, Sub-Proses, Equipment Ops) |
| Tahun 3 | Mastery — validasi tingkat mahir | Process Control & Computer Operations · Refinery Process Operations (optimization: Karakteristik Unit, Day-to-Day Monitoring, Data Collecting, Operating Windows) |

Penyajian di animasi (ringkas, sosialisasi-friendly):

- **Y1**: `Safe Work Practice` · `Refinery Ops · Basic`
- **Y2**: `Energy Mgmt` · `Catalyst & Chemical` · `Refinery Ops · Sub-Proses`
- **Y3**: `Process Control` · `Refinery Ops · Optimization`

## 4. Arsitektur

- **Format**: 1 file HTML standalone 16:9, pure CSS animation (no JS framework).
- **Path**: `docs/assets/proton-video/track-progresi.html`
- **Record script**: `docs/assets/proton-video/record-track-progresi.mjs` (Playwright record ke `.webm`).
- **Output rendered**: `docs/assets/proton-video/track-progresi.webm`
- **Pattern reference**: `docs/assets/proton-video/smart-animation.html` (existing, sebagai analog struktur file).

Existing files yang tidak dipakai (di-ignore, tidak dihapus karena untracked):
- `track-progresi-animation.html`
- `track-progresi-v-G.html`
- `track-progresi-v-H.html`

## 5. Layout (single screen 16:9)

```
┌────────────────────────────────────────────────────────────┐
│ EYEBROW: TRACK & PROGRESI TAHUNAN          [●●●] dot progress│
│ TITLE: Dua Track. Tiga Tahun. Satu Tujuan.                 │
│                                                              │
│ [▣ PANELMAN]               │               [⚙ OPERATOR]    │  ← 2 ikon role abstrak
│ ───────────── garis vertikal pemisah ────────────────────── │
│                                                              │
│ ┌─ TAHUN 1 ─┐  →  ┌─ TAHUN 2 ─┐  →  ┌─ TAHUN 3 ─┐           │
│ │ FOUNDATION│     │ PENDALAMAN│     │  MASTERY  │           │
│ │           │     │           │     │           │           │
│ │ • Safe    │     │ • Energy  │     │ • Process │           │
│ │   Work    │     │   Mgmt    │     │   Control │           │
│ │ • Ref Ops │     │ • Catalyst│     │ • Ref Ops │           │
│ │   Basic   │     │ • Ref Ops │     │   Opt.    │           │
│ │           │     │   Sub-Proc│     │           │           │
│ │ LULUS →   │     │ LULUS →   │     │    ✓      │           │
│ └───────────┘     └───────────┘     └───────────┘           │
│   navy              navy-soft         red                    │
│                                                              │
│ FOOTER: PROTON · Program Coaching Pekerja KPB                │
└────────────────────────────────────────────────────────────┘
```

### Komponen layout

1. **Header bar** (top): eyebrow merah + title navy + 3 progress dots (kanan-atas)
2. **Role badges** (di bawah header): 2 ikon role abstrak (Panelman = panel/HMI screen grid icon; Operator = gear/wrench icon) + label uppercase + garis vertikal pemisah ringan (decorative). Tidak menggunakan karakter manusia atau emoji helmet.
3. **Timeline 3 kartu** (body utama): kartu Y1, Y2, Y3 horizontal, ada arrow penghubung "LULUS →" antar kartu
4. **Footer**: brand line tipis

### Spesifikasi kartu tahun

Tiap kartu berisi:
- **Badge tahun**: "TAHUN 1" / "TAHUN 2" / "TAHUN 3" (background solid color)
- **Tema** uppercase: "FOUNDATION" / "PENDALAMAN" / "MASTERY"
- **Chip list** kompetensi level 1 (2-3 chip per tahun)
- **Arrow keluar** "LULUS →" (Y1, Y2) atau **icon check** ✓ (Y3)

## 6. Color & typography

### Palette (konsisten dengan Sosialisasi v2)

| Token | Hex | Usage |
|-------|-----|-------|
| navy-deep | `#0A2447` | Title, primary text |
| navy | `#0F2D5C` | Y1 accent, kartu Y1 border |
| navy-soft | `#1E4280` | Y2 accent, kartu Y2 border |
| red | `#E63329` | Y3 mastery accent, eyebrow, ✓ icon |
| ink | `#1A1A1A` | Body text |
| muted | `#888888` | Eyebrow secondary, footer |
| hairline | `#E5E7EB` | Border tipis, divider |
| paper | `#F9FAFB` | Background subtle, chip bg |
| sky | `#F0F4FA` | Background hint warmth |

### Typography

- Font: **Inter** 400/500/700/800/900 (Google Fonts, sudah dipakai existing)
- Title: 36-44px, weight 900, letter-spacing -0.01em
- Eyebrow: 12-14px, weight 800, uppercase, letter-spacing 0.2em
- Tema kartu: 14-16px, weight 800, uppercase, letter-spacing 0.15em
- Chip kompetensi: 12-13px, weight 600
- Footer: 11px, weight 500, muted

## 7. Motion timeline (25 detik)

| Time | Event | Easing | Duration |
|------|-------|--------|----------|
| 0:00–0:01 | Stage fade-in dari putih | `ease-out` | 1.0s |
| 0:01–0:03 | Eyebrow + title fade-up | `cubic-bezier(0.2, 0, 0.2, 1)` | 0.8s overlapping |
| 0:03–0:05 | Role badges (Panelman + Operator) slide-in dari atas + divider vertikal grow | `ease-out` | 1.0s |
| 0:05–0:07 | Kartu Y1 slide-up + scale-in | `cubic-bezier(0.34, 1.56, 0.64, 1)` (soft bounce) | 0.7s |
| 0:07–0:09 | Chip kompetensi Y1 reveal sequential (stagger 120ms) | `ease-out` | 0.6s total |
| 0:09–0:10 | Arrow "LULUS →" Y1→Y2 reveal | `ease-out` | 0.5s |
| 0:10–0:13 | Kartu Y2 slide-up + chip reveal stagger | sama Y1 | 1.3s |
| 0:13–0:14 | Arrow "LULUS →" Y2→Y3 reveal | sama | 0.5s |
| 0:14–0:17 | Kartu Y3 slide-up + chip reveal stagger + red accent fade-in | sama | 1.3s |
| 0:17–0:18 | ✓ check Y3 pop-in | `cubic-bezier(0.34, 1.56, 0.64, 1)` | 0.4s |
| 0:18–0:21 | Progress dots animate (●○○ → ●●○ → ●●●) | `ease-in-out` | 1.5s |
| 0:21–0:25 | Hold full frame visible | — | 4s |

Total: 25 detik.

## 8. Implementation guidance

### File structure

```
docs/assets/proton-video/
├── track-progresi.html              # animation source (new)
├── track-progresi.webm              # rendered output (gitignored or committed sesuai pattern)
└── record-track-progresi.mjs        # Playwright record script (new)
```

### Pattern reference

- `smart-animation.html` — struktur HTML 16:9 dengan `.stage` container, animation keyframes CSS, font loading
- `record-smart-animation.mjs` — pattern Playwright record (referensi parameter viewport, codec, duration)

### Tech constraints

- **No external dependencies** selain Google Fonts (Inter). Pure HTML+CSS.
- **No JavaScript** kecuali helper minor untuk timing (sebaiknya juga 0 JS).
- **Viewport**: 1600×900 (16:9) untuk recording. Stage scale responsif via `min(96vw, 1600px)` + `aspect-ratio`.
- **Codec output**: webm vp9, frame rate 30fps (sama smart-animation pattern).

## 9. Out of scope

- Sub-kompetensi level 2 (1.1, 1.2, 2.1, dst) — tidak ditampilkan
- Deliverable detail per tahun — tidak ditampilkan
- Sertifikat / Final Assessment visual — itu bagian 5 & 6 video, beda segmen
- Suara narator / audio — itu pasca-produksi terpisah
- Character illustration / B-roll foto karyawan — keputusan: no character

## 10. Success criteria

- ✓ Durasi 25 detik exact (0:50–1:15 di video utama)
- ✓ Semua 5 nama kompetensi tampil terbagi ke tahun yang sesuai matrix
- ✓ Dual track (Panelman + Operator) terwakili visual via 2 ikon role di header
- ✓ Konsep "naik tahap" terlihat eksplisit via arrow "LULUS →"
- ✓ Y3 punya accent warna berbeda (red) sebagai puncak mastery
- ✓ Final frame (0:21–0:25) bisa dipakai sebagai screenshot poster
- ✓ Konsisten dengan palette + typography Sosialisasi v2 series
- ✓ Record-able ke `.webm` via Playwright script pattern existing

## 11. Open questions

(Akan diisi user saat review jika ada)
