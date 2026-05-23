# Sosialisasi PROTON Operasional — Design Spec

**File output:** `docs/Sosialisasi-PROTON-Operasional.html`
**Tanggal:** 2026-05-23
**Status:** Draft v2 (post research), menunggu approval user

---

## 1. Tujuan

Membuat deck sosialisasi PROTON untuk audiens pekerja lapangan KPB — dari **Section Head ke bawah sampai Coachee** — yang menjelaskan PROTON sebagai program pengembangan kompetensi, dengan pendekatan **overall (tidak terlalu teknis)** dan **fokus pada materi PROTON**.

Deck ini berdiri sendiri dan **tidak menggantikan** dua file yang sudah ada:
- `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` — audiens tim HC (admin Portal), tetap utuh
- `docs/Sosialisasi-PROTON-KPB.html` — audiens lebih luas (22 slide existing), tetap utuh

## 2. Audiens

Mixed dalam satu sesi sosialisasi:
- Section Head (SH)
- Sr Supervisor (SrSpv)
- Coach
- Coachee

Pendekatan: bukan tutorial per role, melainkan narasi overall yang relevan untuk semua audiens di chain coaching.

## 3. Glossary resmi (verified dari codebase + Naskah Video PROTON)

| Singkatan | Kepanjangan | Sumber |
|---|---|---|
| **PROTON** | **Professional Refinery Operations Competency Development** | Resmi (konfirmasi user 2026-05-23). Naskah Video PROTON sebut "Program Coaching Pekerja" sebagai gloss casual — singkatan resmi yang dipakai di deck adalah versi panjang ini. |
| **CMP** | **Competency Management Platform** | `.planning/PROJECT.md:10` |
| **CDP** | **Competency Development Platform** | `.planning/PROJECT.md:11` |
| **BP** | **Business Partner** (Coming Soon / For Future) | `.planning/PROJECT.md:864`, multi spec |
| **KKJ** | **Kebutuhan Kompetensi Jabatan** | `.planning/MILESTONES.md:236` |
| **IDP** | Individual Development Plan | existing PROTON deck |
| **OJT** | On-the-Job Training | konvensi umum |

**Insight kritis dari Naskah Video PROTON:** PROTON **bukan** murni fitur CDP. PROTON adalah program yang tersebar di **2 modul**: CDP (Coaching Proton) dan CMP (Assessment Proton). Spec v1 saya keliru menyebut "CDP = rumah PROTON" — koreksi: PROTON jalan di CDP + CMP.

Plus dari naskah video:
- PROTON dirancang dengan **prinsip SMART** (Specific, Measurable, Achievable, Relevant, Time-bound)
- PROTON punya **2 track**: Panelman + Operator
- Tiap track jalan **3 tahun** (Tahun 1 Foundation → Tahun 2 Pendalaman → Tahun 3 Mastery)
- Alur pelaksanaan **6 langkah** (HC Assign Coach → Deliverable → Coaching Proton → Multi-Role Approval → ...)

## 4. Tone & gaya

- Bahasa: **Bahasa Indonesia 100%** (sesuai `CLAUDE.md`)
- Tone: overall, tidak teknis — minimal jargon sistem (hindari "controller", "ProtonTrack", "form input", "approval status field", dll)
- Sudut: "PROTON dari sudut pekerja yang terlibat" — apa itu, kenapa dijalankan, apa posisi saya
- Fokus konten: **materi PROTON** (definisi, 2 track, 3 tahun, KKJ, Silabus, Coaching Guidance, alur, 9-step, sertifikasi, IDP)
- **Alignment narasi dengan `docs/Naskah Video PROTON.docx`** — pakai definisi & terminologi yang sama supaya konsisten dengan video sosialisasi PROTON

## 5. Non-goals

- Bukan tutorial hands-on per role (tidak ada slide "cara login", "cara upload evidence", "cara approve")
- Bukan dokumentasi teknis sistem (tidak detail controller / database / approval status field)
- Bukan replacement existing `Sosialisasi-PROTON-KPB.html` — file ini berdiri sendiri
- Bukan deck per-role module (audiens setara, narasi linear funnel)

## 6. Design system

**Ikut `Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`** — primary palette teal (bukan navy Pertamina default existing PROTON deck).

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

## 7. Filename & lokasi

- Output: `docs/Sosialisasi-PROTON-Operasional.html`
- Single-file HTML (CSS + JS inline) — konsisten dengan deck lain di `docs/`

## 8. Struktur deck — 20 slide, 4 part

### Part 1 — Apa itu Web HC (3 slide)
| # | Judul | Isi sketch |
|---|---|---|
| 1 | Cover — Sosialisasi PROTON · Portal HC KPB | Tagline, audience tag (SH ke bawah), tanggal/versi |
| 2 | Apa itu Portal HC KPB | Definisi: satu pintu HC digital untuk semua pekerja KPB |
| 3 | Tujuan & manfaat Portal HC | Kenapa dibangun: transparansi, terstruktur, semua role terhubung |

### Part 2 — Fitur/Menu besar Portal (4 slide)
| # | Judul | Isi sketch |
|---|---|---|
| 4 | 3 pilar Portal: CMP · CDP · BP | Overview tiga rumah utama Portal (BP "Coming Soon") |
| 5 | CMP (Competency Management Platform) | Assessment & sertifikasi — **rumah Assessment Proton** |
| 6 | CDP (Competency Development Platform) | Coaching, IDP, deliverable — **rumah Coaching Proton** |
| 7 | BP (Business Partner) | Profil talent & karir — Coming Soon / For Future |

### Part 3 — Apa itu PROTON (9 slide, core)
| # | Judul | Isi sketch |
|---|---|---|
| 8 | PROTON — definisi (Professional Refinery Operations Competency Development) | Singkatan resmi + tujuan + prinsip SMART (5 poin singkat) + jalan di 2 modul (Coaching Proton di CDP, Assessment Proton di CMP) |
| 9 | 4 peran chain coaching | Coachee · Coach · SH · SrSpv — siapa lakukan apa (hook posisi audiens) |
| 10 | 2 track × 3 tahun | Matrix: Panelman (T1/T2/T3) + Operator (T1/T2/T3) dengan tema per tahun (Foundation → Pendalaman → Mastery) |
| 11 | Komponen PROTON — 4 pilar | KKJ (pondasi) + Silabus (jabaran) + Coaching Guidance (pedoman coach) + Assessment (evaluasi) |
| 12 | KKJ — Kebutuhan Kompetensi Jabatan | Pondasi referensi kompetensi (reuse struktur slide KKJ existing PROTON deck) |
| 13 | Silabus PROTON | Penjabaran KKJ → deliverable konkrit (reuse struktur Silabus existing) |
| 14 | Coaching Guidance | Pedoman materi coaching per dimensi (reuse struktur Coaching Guidance existing) |
| 15 | Alur PROTON end-to-end (6 langkah) | HC Assign Coach → Deliverable → Coaching Proton → Multi-Role Approval → Assessment → Sertifikasi (sumber: Naskah Video PROTON langkah 4) |
| 16 | Coaching 9-step — inti praktik lapangan | 9 langkah praktik coaching (reuse struktur 9-step existing) |

### Part 4 — Kenapa PROTON ada buat saya (4 slide)
| # | Judul | Isi sketch |
|---|---|---|
| 17 | Manfaat per role | Coachee (jenjang karir), Coach (mentor recognition), SH/SrSpv (develop team) |
| 18 | Outcome — Sertifikasi Mahir + IDP | Hasil konkret: sertifikat + riwayat & rencana karir |
| 19 | Akses Portal — cara masuk + cek progres saya | Login basic + di mana lihat status diri sendiri + QR code link Portal (opsional, tunggu konfirmasi) |
| 20 | Penutup — bantuan & kontak | Tagline + kontak PIC HC untuk bertanya |

## 9. Asset reuse strategy

**Design system:**
- Copy CSS tokens + layout + dark mode dari `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
- Pendekatan: **copy mekanik HTML scaffold** (deck container, slide controls, dark mode toggle), lalu edit content per slide

**Slide content reusable dari `docs/Sosialisasi-PROTON-KPB.html`:**
- KKJ, Silabus, Coaching Guidance, Alur Kerja PROTON, Coaching 9-step
- Pendekatan: **copy HTML slide existing → rewrite teks supaya tone non-teknis → visual struktur dipertahankan** (bukan rewrite full from scratch)

**Screenshot reuse (minimal, hanya kalau perlu ilustrasi):**
- `docs/sosialisasi-screenshots/proton/` — KKJ, Silabus, Coaching Guidance
- `docs/sosialisasi-screenshots/` — CDP dashboard, Histori PROTON

**Tidak perlu screenshot baru** — deck overall, bukan tutorial hands-on. Visual fokus diagram/ilustrasi/iconography, bukan capture UI mentah.

**Tidak mengubah file existing:**
- `docs/Sosialisasi-PROTON-KPB.html` — tetap utuh
- `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` — tetap utuh (hanya jadi reference design)

## 10. Asumsi durasi sesi

20 slide ≈ 45 menit sesi sosialisasi (2-3 menit/slide rata-rata, lebih untuk Part 3 core, ringan Part 1-2).

## 11. Out of scope

- Tutorial hands-on per role (login, upload, approve, reject flow)
- Modul terpisah per role (Coachee/Coach/SH/SrSpv masing-masing punya bab)
- Screenshot baru via Playwright
- Animasi kompleks (deck tetap deck statis dengan transisi standar)
- PDF export (kalau dibutuhkan, generate separate phase)
- Versi presenter notes / script narasi presenter (deck self-contained baca sendiri)

## 12. Implementation notes

- Single-file HTML, CSS + JS inline
- Struktur deck: `.deck > .slide.active` pattern (copy dari Internal-Tim-HC)
- Controls: prev/next, kbd shortcut, fullscreen toggle, dark mode toggle (ikut existing)
- Progress bar slide indicator
- Responsive: viewport ≥1366px optimal, fallback ≤1366px via media query (lihat fix `791e2cde` di existing PROTON deck)
- Favicon: inline SVG (silence 404, lihat `efd1bff3` existing PROTON)
- Tidak ada dependensi eksternal selain font system

## 13. Verification

Konsisten dengan workflow project (`CLAUDE.md` + memory project_sosialisasi_*):
1. Build/render lokal — buka langsung di browser file://
2. Manual visual check — semua 20 slide tampil, transisi mulus, dark mode toggle, fullscreen
3. (Opsional) Playwright snapshot per slide — kalau user minta automated check
4. Pending: user buka & approve sebelum dianggap shipped

## 14. Open questions (perlu dijawab sebelum implementation)

| # | Pertanyaan | Default kalau user diam |
|---|---|---|
| Q1 | QR code link Portal di slide #19 — perlu atau tidak? URL Portal: dev (`http://10.55.3.3/KPB-PortalHC`) atau prod? | Skip QR (tulisan URL dev saja) |
| Q2 | Version tag git setelah ship (mis. `sosialisasi-proton-operasional-v1.0`)? | Ya, pakai pattern `sosialisasi-proton-operasional-v1.0` |
| Q3 | Audience tag di Cover — eksplisit "Section Head · Sr Supervisor · Coach · Coachee" atau cukup "Pekerja Operasional"? | Eksplisit 4 role |
| Q4 | Branch strategy — direct ke `main` (pattern existing sosialisasi deck) atau branch `feat/sosialisasi-proton-operasional`? | Direct ke `main` per pattern |
| Q5 | Playwright verification — ya/tidak? | Tidak (manual browser check cukup, deck statis) |

---

**Next step:** Setelah user review & approve spec v2 ini (+ jawab Q1-Q5), lanjut ke `writing-plans` skill untuk buat implementation plan.
