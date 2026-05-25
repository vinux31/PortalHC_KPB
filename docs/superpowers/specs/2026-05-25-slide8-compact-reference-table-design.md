# Spec: Tabel Referensi & Standar — Compact v1.1 (index.html)

**Tanggal:** 2026-05-25
**File target:** `docs/pcp-HCPortal-2026/slide8-risalah/index.html`
**Konteks:** PCP SMART 2026 · §3.4 · Slide 8 Risalah Web

---

## Tujuan

Menambahkan section baru di `index.html` bagian compact yang menjelaskan basis akademik, standar internasional, dan regulasi internal Pertamina yang mendasari kedua versi compact (Opsi II & Opsi IV). Reviewer PPT dan HC management dapat langsung melihat legitimasi referensi tanpa membuka README.

---

## Keputusan Desain

| Dimensi | Keputusan |
|---------|-----------|
| Konten | Full reference table (akademik + standar eksternal + standar internal + formula) |
| Format | X2 — grouped by kategori, 3 kolom (Referensi · Opsi II · Opsi IV) |
| Posisi | Setelah compact cards, sebelum Audience Matrix |
| Kolom Opsi | Opsi II = Pipeline Outcome; Opsi IV = Workflow Topology Refined |

---

## Struktur Section

### Header
```
<h2>Basis Referensi & Standar — Compact v1.1</h2>
<p class="subtitle">Penjelasan singkat: kedua versi dibangun dari landasan yang sama, kolom menunjukkan peran per opsi</p>
```

### Tabel — 4 Grup, 12 Baris

#### Grup 1: Akademik
| Referensi | Opsi II | Opsi IV |
|-----------|---------|---------|
| Ogoun & Tamunosiki-Amadi (2023) — Competence Monitoring Pipeline · Spearman R | ✅ PRIMARY | ✅ callout |
| Ellström & Kock (2008) — Competence Development framework | — | ✅ footer |
| Korelasi Jurnal & PPT KPB (RL1) + Laporan Kuantitatif V2 (RL2) | ✅ formula | ✅ formula |

#### Grup 2: Standar Eksternal
| Referensi | Opsi II | Opsi IV |
|-----------|---------|---------|
| ISO/IEC 27001:2022 — Information Security Management | ✅ | ✅ |
| OWASP Top 10 (2021) + ASVS 4.0.3 — Application Security Verification | ✅ | ✅ |
| WCAG 2.2 (W3C, 2023) — Web Content Accessibility | ✅ | ✅ |

#### Grup 3: Standar Internal Pertamina
| Referensi | Opsi II | Opsi IV |
|-----------|---------|---------|
| Pedoman Kompetensi Teknis A5.2-01/K20000/2025/S9 (SI1) | ✅ | ✅ |
| TKO B5.3-04/K20100/2025-S9 (SI2) — Coaching & Mentoring | ✅ | ✅ |
| Kamus Direktori Kompetensi Teknis Pertamina (SI3) | ✅ | ✅ |

#### Grup 4: Formula & Data Internal
| Referensi | Opsi II | Opsi IV |
|-----------|---------|---------|
| Risalah Inovasi PROTON — Fishbone + FMEA (P1) | ✅ Issue codes | ✅ Issue codes |
| Risalah Panca Mutu (P2) — Formula R → Time, Innov, Alert | ✅ formula | ✅ formula |

### Footer Note
```
* ✅ PRIMARY = konten utama slide. ✅ callout = disebut di callout box. ✅ footer = credit di footer.
```

---

## Posisi di index.html

```
[hero]
[cards Opsi II + IV full]
[h2: Compact v1.1]
[compact cards Opsi II + IV]
→ [NEW: section Basis Referensi & Standar]  ← INSERT DI SINI
[audience-matrix]
[footer]
```

---

## Visual Style

Ikuti token warna existing index.html:
- Header grup: `background: #dbeafe` (biru muda), teks `#1d4ed8`, uppercase, letter-spacing
- Header kolom tabel: `background: var(--pertamina-blue)` = `#00558C`, teks white
- Zebra stripe baris: `#f9fafb` alternating
- Centang positif: `color: #15803d` (hijau)
- Dash negatif: `color: #9ca3af` (abu)
- Sub-deskripsi referensi: `font-size: .75rem`, `color: #6b7280`
- Wrapper section: `background: white`, `padding: 1.5rem`, `border-radius: .75rem`, `box-shadow` sesuai `.audience-matrix`

---

## Tidak Termasuk

- Referensi R2 (Staškeviča 2019), R3 (Ruggiero et al 2026), RL3 — deferred di v1.0, tidak dipakai
- PCP 7-slot compliance table — sudah ada di README, tidak duplikasi di index
- Perubahan pada file compact HTML itu sendiri (hanya index.html)

---

## File yang Diubah

| File | Aksi |
|------|------|
| `docs/pcp-HCPortal-2026/slide8-risalah/index.html` | Insert 1 section HTML baru |

---

## Referensi

- README: `docs/pcp-HCPortal-2026/slide8-risalah/README.md` — Reference Mapping table
- Spec sebelumnya: `docs/superpowers/specs/2026-05-24-slide8-risalah-opsi-ii-iv-design.md`
- Tag: `slide8-risalah-v1.0`
