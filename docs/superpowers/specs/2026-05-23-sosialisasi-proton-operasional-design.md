# Sosialisasi PROTON Operasional — Design Spec

**File output:** `docs/Sosialisasi-PROTON-Operasional.html`
**Tanggal:** 2026-05-23
**Status:** Draft, menunggu approval user

---

## 1. Tujuan

Membuat deck sosialisasi PROTON untuk audiens pekerja lapangan KPB — dari **Section Head ke bawah sampai Coachee** — yang menjelaskan PROTON sebagai sistem pengembangan kompetensi, dengan pendekatan **overall (tidak terlalu teknis)** dan **fokus pada materi PROTON**.

Deck ini berbeda dari `Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (audiens tim HC, materi how-to admin) dan berbeda dari existing `Sosialisasi-PROTON-KPB.html` (audiens lebih luas, scope umum portal).

## 2. Audiens

Mixed dalam satu sesi sosialisasi:
- Section Head (SH)
- Sr Supervisor (SrSpv)
- Coach
- Coachee

Pendekatan: bukan tutorial per role, melainkan narasi overall yang relevan untuk semua audiens di chain coaching.

## 3. Tone & gaya

- Bahasa: **Bahasa Indonesia 100%** (sesuai `CLAUDE.md`)
- Tone: overall, tidak teknis — minimal jargon sistem (hindari "controller", "ProtonTrack", "form input", "SrSpvApprovalStatus", dll)
- Sudut: "PROTON dari sudut pekerja yang terlibat" — apa itu, kenapa dijalankan, apa posisi saya
- Fokus konten: **materi PROTON** (KKJ, Silabus, Coaching Guidance, alur, 9-step, sertifikasi, IDP)

## 4. Non-goals

- Bukan tutorial hands-on per role (tidak ada slide "cara login", "cara upload evidence", "cara approve")
- Bukan dokumentasi teknis sistem (tidak detail controller / database / approval flow internal)
- Bukan replacement existing `Sosialisasi-PROTON-KPB.html` — file ini berdiri sendiri
- Bukan deck per-role module (audiens setara, narasi linear)

## 5. Design system

**Ikut `Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`** — primary palette teal (bukan navy Pertamina default).

| Token | Value | Note |
|---|---|---|
| `--teal` | `#0d9488` | Primary brand |
| `--teal-dark` | `#0f766e` | Hover / emphasis |
| `--teal-light` | `#14b8a6` | Accent ringan |
| `--amber` | `#f59e0b` | Accent kontras |
| `--amber-dark` | `#d97706` | Hover amber |
| `--green` | `#10b981` | Status sukses |
| `--orange` | `#ea580c` | Highlight |
| `--red` | `#dc2626` | Warning |
| `--bg` | `#f0fdfa` | Page background mint pale |
| `--slide-bg` | `#ffffff` | Slide putih |

Layout, typography, dark mode, slide structure (.deck/.slide, controls, progress bar): **copy mekanik** dari Internal-Tim-HC.

## 6. Filename & lokasi

- Output: `docs/Sosialisasi-PROTON-Operasional.html`
- Single-file HTML (CSS + JS inline) — konsisten dengan deck lain di `docs/`

## 7. Struktur deck — 20 slide, 4 part

### Part 1 — Apa itu Web HC (3 slide)
| # | Judul | Isi sketch |
|---|---|---|
| 1 | Cover — Sosialisasi PROTON · Portal HC KPB | Tagline, audience tag (SH ke bawah), tanggal/versi |
| 2 | Apa itu Portal HC KPB | Definisi: satu pintu HC digital untuk semua pekerja KPB |
| 3 | Tujuan & manfaat Portal HC | Kenapa dibangun: transparansi, terstruktur, semua role terhubung |

### Part 2 — Fitur/Menu besar Portal (4 slide)
| # | Judul | Isi sketch |
|---|---|---|
| 4 | 3 pilar Portal: CMP · CDP · BP | Overview tiga rumah utama Portal |
| 5 | CMP (Competency Management) | Apa, untuk siapa — singkat |
| 6 | CDP (Coaching & Development) | Apa, untuk siapa — **flag: rumah PROTON ada di sini** |
| 7 | BP (Business Performance) | Apa, untuk siapa — singkat |

### Part 3 — Apa itu PROTON (9 slide, core)
| # | Judul | Isi sketch |
|---|---|---|
| 8 | PROTON — definisi & tujuan | Program pengembangan kompetensi operator, target outcome |
| 9 | 4 peran chain coaching | Coachee · Coach · SH · SrSpv — siapa lakukan apa (hook posisi audiens) |
| 10 | Peta 3 tahun PROTON | T1 → T2 → T3 progresi kompetensi |
| 11 | Komponen PROTON — overview 4 pilar | KKJ + Silabus + Coaching Guidance + Assessment |
| 12 | KKJ — peta kompetensi per jabatan | Pondasi referensi kompetensi (reuse struktur slide KKJ dari existing PROTON deck) |
| 13 | Silabus PROTON — penjabaran deliverable | Detail apa yang harus dikerjakan per kompetensi (reuse struktur Silabus existing) |
| 14 | Coaching Guidance — pedoman materi | Pedoman coach untuk mengajar (reuse struktur Coaching Guidance existing) |
| 15 | Alur PROTON end-to-end | Assessment → IDP → coaching → evaluasi → sertifikasi (reuse struktur Alur Kerja PROTON existing) |
| 16 | Coaching 9-step — inti praktik lapangan | 9 langkah praktik coaching di lapangan (reuse struktur 9-step existing) |

### Part 4 — Kenapa PROTON ada buat saya (4 slide)
| # | Judul | Isi sketch |
|---|---|---|
| 17 | Manfaat per role | Coachee (jenjang karir), Coach (mentor recognition), SH/SrSpv (develop team) |
| 18 | Outcome — Sertifikasi Mahir + IDP | Hasil konkret: sertifikat + riwayat & rencana karir |
| 19 | Akses Portal — cara masuk + cek progres saya | Login basic + di mana lihat status diri sendiri |
| 20 | Penutup — bantuan & kontak | Tagline + kontak PIC HC untuk bertanya |

## 8. Asset reuse strategy

**Design system:**
- Copy CSS tokens + layout + dark mode dari `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

**Slide content reusable (modif teks supaya kurang teknis, visual struktur dipertahankan):**
- Dari `docs/Sosialisasi-PROTON-KPB.html`: KKJ, Silabus, Coaching Guidance, Alur Kerja PROTON, Coaching 9-step

**Screenshot reuse (kalau diperlukan minimal):**
- `docs/sosialisasi-screenshots/proton/` — KKJ, Silabus, Coaching Guidance
- `docs/sosialisasi-screenshots/` — CDP dashboard, Histori PROTON

**Tidak perlu screenshot baru** — deck overall, bukan tutorial hands-on. Visual fokus diagram/ilustrasi/iconography, bukan capture UI mentah.

## 9. Asumsi durasi sesi

20 slide ≈ 45 menit sesi sosialisasi (2-3 menit/slide rata-rata, lebih untuk Part 3 core, ringan Part 1-2).

## 10. Out of scope

- Tutorial hands-on per role (login, upload, approve, reject flow)
- Modul terpisah per role (Coachee/Coach/SH/SrSpv masing-masing punya bab)
- Screenshot baru via Playwright
- Animasi kompleks (deck tetap deck statis dengan transisi standar)
- PDF export (kalau dibutuhkan, generate separate phase)
- Versi presenter notes / script narasi presenter (deck self-contained baca sendiri)

## 11. Implementation notes

- Single-file HTML, CSS + JS inline
- Struktur deck: `.deck > .slide.active` pattern (copy dari Internal-Tim-HC)
- Controls: prev/next, kbd shortcut, fullscreen toggle, dark mode toggle (ikut existing)
- Progress bar slide indicator
- Responsive: viewport ≥1366px optimal, fallback ≤1366px via media query (lihat fix `791e2cde` di existing PROTON deck)
- Favicon: inline SVG (silence 404, lihat `efd1bff3` existing PROTON)
- Tidak ada dependensi eksternal selain font system

## 12. Open questions

Belum ada — semua pertanyaan klarifikasi sudah dijawab di brainstorming session.

---

**Next step:** Setelah user review & approve spec ini, lanjut ke `writing-plans` skill untuk buat implementation plan.
