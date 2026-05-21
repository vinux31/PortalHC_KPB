# Design Spec — PCP SMART 2026 §3.4 HC Portal (v2)

**Tanggal:** 2026-05-21
**Versi:** 2 (revisi total dari v1.0 yang sudah di-tag dan di-cleanup)
**Topik:** Dokumen §3.4 "Design/Gambar Teknik/Flow Proses/Formula Solusi Terpilih" untuk PCP SMART 2026
**Domain:** HC Portal (PortalHC_KPB) — web app pengelolaan kompetensi pekerja CSU Process KPB
**Status:** Draft — pending user review

---

## 1. Pivot dari v1.0

**v1.0** menggunakan pendekatan **swimlane-only** untuk 7 fitur. Setelah diskusi mendalam, ditemukan:

- Slide PCP referensi (OT cybersecurity) menggunakan **topology diagram** (statis, single master view), bukan swimlane (dinamis, per fitur).
- Untuk match format slide referensi, HC Portal perlu **Gambar Teknik (landscape diagram)** sebagai komponen utama §3.4.
- Swimlane tetap relevan, tetapi diturunkan posisinya sebagai **lampiran detail per fitur**.

v2 = strategi hybrid **Gambar Teknik (utama) + Flow Proses (lampiran)**.

## 2. Tujuan §3.4

Memenuhi requirement PCP SMART 2026 §3.4 dengan visualisasi solusi terpilih HC Portal dalam **2 jenis valid** dari 4 pilihan §3.4:

1. **Gambar Teknik** — diagram landscape arsitektur Sebelum vs Sesudah
2. **Flow Proses** — swimlane workflow per fitur impactful

Skip:
- **Design** (mockup UI) — optional, di-mention saja
- **Formula** — N/A untuk web app HR

## 3. Cakupan

### 3.1 Gambar Teknik (Utama)

Diagram landscape **Sebelum** dan **Sesudah** dalam **2 versi style** untuk reviewer memilih:

| Versi | Style | Standar | Match Slide Referensi |
|-------|-------|---------|:--------------------:|
| **A — Layered Aktor** | 5 layer vertikal per peran (Manajemen/HC/Atasan/Coach/Pekerja) | Industry de-facto Layered Architecture | ⭐⭐⭐⭐⭐ |
| **B — C4 System Context** | Hub-and-spoke (HC Portal di tengah, aktor mengelilingi) | C4 Model (Simon Brown) | ⭐⭐⭐ |

ArchiMate skip (terlalu teknis untuk PCP narrative).

### 3.2 Flow Proses (Lampiran)

**7 swimlane per fitur impactful:**

1. Assessment Online
2. PROTON Coaching
3. IDP / Plan
4. KKJ & Matriks Kompetensi
5. Sertifikat & Renewal
6. Reporting / Analytics
7. Pengelolaan Data Pekerja

Setiap fitur: **Sebelum** (workflow manual multi-tools) + **Sesudah** (workflow di HC Portal).

### 3.3 Tabel Komparasi (Pendukung)

- **Tabel Aspek × Sebelum × Sesudah** lintas 7 fitur (kuantitatif + kualitatif)
- **Tabel Issue Resolved (A-F)** dengan mapping ke fitur

### 3.4 Out of Scope

- Mockup UI / screenshot portal asli (Design — optional)
- Formula matematis (N/A)
- ArchiMate diagram (terlalu teknis)
- VSM / SIPOC (tidak ada di slide referensi)
- Implementation code

## 4. Format Output

### 4.1 Pipeline

| Tahap | Format | Tujuan |
|-------|--------|--------|
| Draft / Source | Markdown + Mermaid (.md) | Versionable git, AI-assisted edit |
| Review | HTML standalone (.html) | Easy browser review |
| Submission PCP | PowerPoint (.pptx) | Format wajib panitia (manual redraw) |
| Lampiran formal | PDF (.pdf) | Export dari HTML/PPT |

### 4.2 Tone

- **Narasi & header eksekutif** (untuk reviewer PCP, manajemen)
- **Tabel & diagram label teknis** (konsisten terminologi sistem)
- **Bahasa Indonesia full** per CLAUDE.md

### 4.3 Metrics

**Hybrid:**
- Kuantitatif estimasi (step, tools, waktu, %)
- Kualitatif justifikasi (audit, governance, single source of truth)

## 5. Struktur Output File

```
docs/pcp-HCPortal-2026/3.4-solusi-terpilih/
├─ README.md                       ← Index + executive summary + struktur §3.4
├─ index.html                      ← Master HTML viewer (consolidated, all-in-one)
│
├─ gambar-teknik/
│  ├─ versi-a-layered-aktor.html   ← Diagram landscape Layered (Sebelum + Sesudah)
│  ├─ versi-b-c4-context.html      ← Diagram landscape C4 (Sebelum + Sesudah)
│  └─ tabel-komparasi.md           ← Tabel Aspek × Sebelum × Sesudah lintas 7 fitur
│
├─ flow-proses/
│  ├─ 01-assessment.md             ← Swimlane Sebelum + Sesudah
│  ├─ 02-proton-coaching.md
│  ├─ 03-idp-plan.md
│  ├─ 04-kkj-matriks.md
│  ├─ 05-sertifikat-renewal.md
│  ├─ 06-reporting-analytics.md
│  └─ 07-data-pekerja.md
│
├─ pendukung/
│  ├─ tabel-issue-resolved.md      ← Issue A-F + mapping fitur
│  └─ legend-konvensi.md           ← Legend aktor + notasi swimlane + warna
│
└─ archive/
   └─ diagram-landscape-options.html  ← (existing) discussion mockup, reference
```

**Total file:** 13 file Markdown + 2-3 file HTML.

## 6. Template Per File

### 6.1 Gambar Teknik — Versi A (Layered Aktor)

```html
<!DOCTYPE html>
[Header + Style Pertamina]

<section id="sebelum">
  <h2>❌ Sebelum (Kondisi Aktual)</h2>
  [5 layer rows: L5 Manajemen / L4 HC / L3 Atasan / L2 Coach / L1 Pekerja]
  [Tiap layer: tools/aplikasi yang dipakai aktor]
  [Marker A-F issue di lokasi pain point]
  [Legend issue A-F]
</section>

<section id="sesudah">
  <h2>✅ Sesudah (Konsep Improvement)</h2>
  [5 layer rows dengan komponen portal di tiap layer]
  [HC Portal Hub di tengah (gradient biru-hijau)]
  [Marker 1-7 improvement]
  [Legend improvement]
</section>

<section id="komparasi">
  [Tabel ringkasan Sebelum vs Sesudah]
</section>
```

### 6.2 Gambar Teknik — Versi B (C4 Context)

```html
<!DOCTYPE html>
[Header + Style Pertamina]

<section id="sebelum">
  <h2>❌ Sebelum — Tools Tersebar (No Hub)</h2>
  [Canvas: 4 aktor di corner (Manajemen, HC, Pekerja, Atasan/Coach)]
  [Tools manual di tengah (Excel, FleQi, Email, WhatsApp)]
  [Garis koneksi semrawut]
</section>

<section id="sesudah">
  <h2>✅ Sesudah — HC Portal sebagai Hub</h2>
  [Canvas: HC Portal hub di tengah]
  [4 aktor di corner]
  [Garis lurus dari tiap aktor ke hub]
</section>

<section id="komparasi">
  [Tabel ringkasan]
</section>
```

### 6.3 Flow Proses (Swimlane) — Template Konsisten

```markdown
# Process Flow — [Nama Fitur]

## Konteks (Eksekutif, 1 paragraf)

## Flow SEBELUM — Workflow Manual

```mermaid
flowchart LR
    [swimlane Sebelum: aktor manual + tools eksternal]
```

## Flow SESUDAH — HC Portal

```mermaid
flowchart LR
    [swimlane Sesudah: aktor + step portal]
```

## Tabel Komparasi Step

| Aspek | Sebelum | Sesudah | Δ Improvement |

## Issue yang Diselesaikan

[Mapping ke tabel-issue-resolved.md]

## Benefit (Kuantitatif + Kualitatif)
```

## 7. Konvensi Aktor & Notasi (Konsisten Semua File)

| Kode | Nama | Definisi |
|------|------|----------|
| `USER` | User / Pekerja | Pekerja umum CSU Process |
| `COACHEE` | Coachee | Pekerja program pengembangan |
| `COACH` | Coach | Pendamping coachee |
| `ATASAN` | Atasan | Sr Spv / Section Head / Manager |
| `HC` | Human Capital | Fungsi pengelola kompetensi |
| `SISTEM` | HC Portal | Web app (otomatis) |
| `MANAJEMEN` | Manajemen | Direktur / VP |

**Warna konsisten (untuk redraw PowerPoint):**
- Pain point / manual: abu-abu / merah muda
- Portal / digital: biru / hijau Pertamina
- Decision: kuning
- Hub portal: gradient biru-hijau

## 8. Eksekusi (Wave-Based)

| Wave | Output | Justifikasi |
|------|--------|-------------|
| **1 — Foundation** | README + legend-konvensi + tabel-issue-resolved | Konvensi dulu sebelum konten |
| **2 — Gambar Teknik Versi A** | versi-a-layered-aktor.html | Match slide referensi paling kuat |
| **3 — Gambar Teknik Versi B** | versi-b-c4-context.html | Alternative untuk pilihan |
| **4 — Tabel Komparasi Master** | tabel-komparasi.md | Konsolidasi data lintas fitur |
| **5 — Flow Proses** | 7 file flow swimlane | Detail per fitur (lampiran) |
| **6 — Index HTML Master** | index.html | Consolidated viewer all-in-one |
| **7 — Verifikasi + Tag v2.0** | git tag pcp-hcportal-3.4-v2.0 | Final state |

## 9. Acceptance Criteria

| Criteria | Pass |
|----------|:----:|
| README.md ada dengan executive summary + struktur §3.4 | ☐ |
| Folder `gambar-teknik/` berisi 2 versi HTML + tabel komparasi | ☐ |
| Versi A (Layered) punya 5 layer aktor + marker A-F + 1-7 | ☐ |
| Versi B (C4) punya hub-and-spoke + 4-5 aktor + connection lines | ☐ |
| Folder `flow-proses/` berisi 7 file swimlane (Sebelum + Sesudah) | ☐ |
| Folder `pendukung/` berisi tabel-issue + legend | ☐ |
| index.html consolidated dengan navigasi sidebar | ☐ |
| Mermaid render benar di semua file flow | ☐ |
| Konsisten tone: eksekutif narasi, teknis data | ☐ |
| Bahasa Indonesia full | ☐ |
| Tidak ada placeholder TBD/TODO | ☐ |
| Tag `pcp-hcportal-3.4-v2.0` dibuat | ☐ |

## 10. Risiko & Mitigasi

| Risiko | Dampak | Mitigasi |
|--------|--------|----------|
| 2 versi Gambar Teknik bikin confused (mana yang dipakai?) | Reviewer / user bingung | README jelaskan rationale; user bisa pilih 1 saat redraw ke PPT |
| Data kuantitatif estimasi tetap "internal" | Reviewer ragukan kredibilitas | Label "estimasi internal" eksplisit, rencana refine pasca-implementasi |
| C4 Context kurang familiar di reviewer non-IT | Reviewer skip Versi B | Versi B = optional/backup, Versi A = primary |
| HTML render Mermaid butuh internet CDN | Offline gagal render | Tetap simpan Mermaid source di MD, render manual bila perlu |

## 11. Referensi

- Slide PCP template: `C:\Users\Administrator\OneDrive - PT Pertamina (Persero)\Documents\PCP SMART 2026 APQ Rev 9999 Final_1.png`
- TKI feature inventory: `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md`
- Existing discussion mockup: `docs/pcp-HCPortal-2026/diagram-landscape-options.html`
- Recovery v1.0 (kalau perlu): `git checkout pcp-hcportal-3.4-v1.0 -- <path>`

## 12. Next Step

Setelah spec ini approved → invoke `superpowers:writing-plans` untuk implementation plan per wave.
