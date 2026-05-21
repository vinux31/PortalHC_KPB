# Design Spec — PCP SMART 2026 §3.4 HC Portal (v3)

**Tanggal:** 2026-05-21
**Versi:** 3 (fresh total dari v2.0 yang sudah di-tag dan di-cleanup)
**Topik:** Diagram landscape §3.4 HC Portal — fokus pada Gambar Teknik utama
**Status:** Draft — pending user review

---

## 1. Pivot dari v2.0

| Versi | Strategi | Hasil |
|-------|----------|-------|
| v1.0 | Swimlane-only per fitur | 12 file MD, terlalu detail tanpa overview |
| v2.0 | Hybrid Gambar Teknik (2 versi) + Flow Proses (lampiran) | 15 file, struktur kompleks |
| **v3.0** | **Fokus diagram landscape (2 versi) — eliminate complexity** | **Lean & impactful** |

v3 = **fresh total fokus pada visualisasi landscape yang BAGUS dan LENGKAP**, tanpa swimlane lampiran (karena slide PCP referensi tidak punya).

## 2. Tujuan §3.4 v3

Memenuhi requirement PCP SMART 2026 §3.4 dengan **2 diagram landscape yang bagus dan lengkap** untuk reviewer pilih:

1. **Versi P — Workflow Topology** (Purdue Layered) — utama, match slide PCP referensi
2. **Versi C — Comparison Dashboard** (Card-Grid Executive View) — secondary, data-dense

Skip:
- Swimlane per fitur (out of scope — slide referensi tidak punya)
- Design mockup UI
- Formula
- C4 / ArchiMate / SIPOC / VSM

## 3. Decision Matrix yang Ditolak

| Kandidat 2nd Version | Skor | Alasan Tolak |
|---------------------|:----:|--------------|
| Module Constellation (M) | 38 | Artistic risk, susah redraw PPT |
| Transformation Journey (T) | 48 | Tidak se-impact, mirip stacked Sebelum/Sesudah |
| Ecosystem Map (E) | 37 | Risk cluttered |
| **Comparison Dashboard (C)** | **50** | **✅ Dipilih — data-dense, executive-friendly, mudah PPT** |

## 4. Versi P — Workflow Topology (Primary)

### 4.1 Konsep

5 layer vertikal aktor (Manajemen / HC / Atasan / Coach / Pekerja) dengan:
- Komponen modul/tools di tiap layer
- **Connection lines eksplisit** (data flow antar layer)
- **DMZ-analog "HC Portal Buffer Zone"** di tengah (sebagai pemisah seperti DMZ di slide OT)
- Issue marker A-F di Sebelum
- Improvement marker 1-N di Sesudah
- **Header bar** dengan logo Pertamina + PCP SMART 2026 badge
- **Audience + Tujuan section** di atas diagram
- **Tabel komparasi terintegrasi** di bawah (bukan file terpisah)

### 4.2 Layout

```
┌──────────────────────────────────────────────┐
│ [Pertamina Logo] [Title] [PCP SMART Badge]  │
├──────────────────────────────────────────────┤
│ Audience: Reviewer PCP, Mgmt HC              │
│ Tujuan: Tunjukkan transformasi workflow      │
├──────────────────────────────────────────────┤
│                                              │
│  ❌ SEBELUM                                  │
│  ┌─ L5 Strategic ─┐ Manajemen + tools (D)   │
│  ├─ L4 Governance┤ HC + 5 Excel master (A B)│
│  ├─ L3 Supervisor┤ Atasan + Email/WA (C E)  │
│  ├─ L2 Coaching ─┤ Coach + paperwork (A E)  │
│  └─ L1 Operational Pekerja + FleQi/HC (A F) │
│  ↕ connection lines (red, semrawut)         │
│  Legend: Issue A-F + lokasi                 │
│                                              │
│  ──────► transformation ──────►             │
│                                              │
│  ✅ SESUDAH                                  │
│  ┌─ L5 Strategic ─┐ Analytics Dashboard ①   │
│  ├─ L4 Governance┤ Master Data ② ③         │
│  ├═ Buffer Zone ═┤ HC PORTAL HUB ④         │
│  ├─ L3 Supervisor┤ Records Team ⑤          │
│  ├─ L2 Coaching ─┤ Coaching PROTON ⑥       │
│  └─ L1 Operational Self-service ⑦          │
│  ↕ connection lines (blue, terstruktur)     │
│  Legend: Improvement 1-7 + deskripsi        │
│                                              │
├──────────────────────────────────────────────┤
│  Tabel Komparasi Aspek (inline)             │
│  Aspek | Sebelum | Sesudah                  │
├──────────────────────────────────────────────┤
│ Footer: tag v3.0 | source | print button    │
└──────────────────────────────────────────────┘
```

### 4.3 Visual Enhancement vs v2

| Element | v2 (Versi A) | v3 (Versi P) |
|---------|:------------:|:------------:|
| Header bar dengan logo | ❌ | ✅ |
| Audience + Tujuan section | ❌ | ✅ |
| Connection lines eksplisit | ❌ implicit | ✅ SVG arrows |
| DMZ Buffer Zone label | ⚠️ hub only | ✅ explicit zone |
| Tabel komparasi inline | ❌ separate file | ✅ inline |
| Tooltip popup di marker | ❌ | ✅ web-only |
| Print-friendly CSS | ✅ | ✅ enhanced |
| Color coding sistematis | ⚠️ | ✅ palette terbatas |

## 5. Versi C — Comparison Dashboard (Secondary)

### 5.1 Konsep

Card-grid layout dengan 7 fitur dalam card, masing-masing menampilkan:
- Icon + nama fitur
- Sebelum vs Sesudah metric
- Improvement Δ% prominent

Bottom: HC Portal hub showcase + agregat impact.

### 5.2 Layout

```
┌──────────────────────────────────────────────┐
│ [Pertamina Logo] [Title] [PCP SMART Badge]  │
├──────────────────────────────────────────────┤
│ HC Portal — Comparison Dashboard            │
│ Audience: Executive review                  │
├──────────────────────────────────────────────┤
│                                              │
│  ┌────────────────┐  ┌────────────────┐    │
│  │ 📝 Assessment  │  │ 🎯 PROTON      │    │
│  │ Sebelum: 6 step│  │ Sebelum: 5 step│    │
│  │ Sesudah: 2 step│  │ Sesudah: 2 step│    │
│  │ Δ -67% ⬇       │  │ Δ -60% ⬇       │    │
│  │ Issue: A B C D │  │ Issue: A C E   │    │
│  └────────────────┘  └────────────────┘    │
│  ┌────────────────┐  ┌────────────────┐    │
│  │ 📋 IDP         │  │ 📊 KKJ         │    │
│  │ Δ -75% step    │  │ Δ -99% waktu   │    │
│  └────────────────┘  └────────────────┘    │
│  ┌────────────────┐  ┌────────────────┐    │
│  │ 🏆 Sertifikat  │  │ 📈 Reporting   │    │
│  │ Δ -67% step    │  │ Δ -96% waktu   │    │
│  └────────────────┘  └────────────────┘    │
│  ┌────────────────┐  ┌─ AGREGAT ──────┐    │
│  │ 👥 Data Pekerja│  │ Step: -67% med │    │
│  │ Δ -83% step    │  │ Tools: -75% med│    │
│  └────────────────┘  │ Waktu: ~95% med│    │
│                       └────────────────┘    │
│                                              │
│  ┌─────── HC PORTAL HUB ──────┐             │
│  │ ASP.NET Core 8             │             │
│  │ SQL Server + SignalR       │             │
│  │ Audit Log + RBAC           │             │
│  └────────────────────────────┘             │
│                                              │
│  Tabel Issue A-F + Coverage Matrix          │
├──────────────────────────────────────────────┤
│ Footer: tag v3.0 | print button             │
└──────────────────────────────────────────────┘
```

### 5.3 Karakteristik

- **7 fitur card** dengan icon + metrik kunci
- **Color-coded card border:** range improvement (low: kuning, med: oranye, high: hijau)
- **HC Portal hub** sebagai bottom showcase
- **Agregat box** dengan median impact
- **Tabel issue A-F + matriks coverage** inline di bawah

## 6. Konvensi Visual (Both Versions)

### 6.1 Color Palette (5 warna max)

| Warna | Hex | Meaning |
|-------|-----|---------|
| Pertamina Red | `#C8102E` | Pain / Issue / Sebelum |
| Pertamina Blue | `#00558C` | Portal / Digital / Sesudah |
| Pertamina Green | `#00A551` | Improvement / Success |
| Pertamina Yellow | `#FFC72C` | Transition / Decision |
| Neutral Gray | `#6b7280` | Background / Metadata |

### 6.2 Typography Hierarchy

| Level | Style | Use |
|-------|-------|-----|
| H1 | 1.8rem bold, biru, border red | Title diagram |
| H2 | 1.4rem bold, biru | Section header |
| H3 | 1.1rem semibold | Subsection |
| Body | 0.95rem normal | Text content |
| Caption | 0.75rem italic muted | Footer/source |

### 6.3 Iconography

- Aktor: emoji konsisten (👔 Mgmt, 👤 HC, 🏢 Atasan, 🧑‍🏫 Coach, 👷 Pekerja)
- Tools: emoji ringkas (📊 Excel, 📧 Email, 💬 WA, 🌐 Web, 📝 Form, 📁 Arsip)
- Modul portal: emoji + label (🎯 PROTON, 📈 Analytics, dll.)

### 6.4 Marker Symbol

- **Issue:** lingkaran merah, huruf A-F, position: bottom-right component
- **Improvement:** lingkaran hijau, angka 1-N, position: bottom-right component

## 7. Struktur Output File

```
docs/pcp-HCPortal-2026/3.4-solusi-terpilih/
├─ README.md                       (executive summary + index + 2 versi rationale)
├─ versi-p-workflow-topology.html  (PRIMARY — Purdue Layered)
├─ versi-c-comparison-dashboard.html (SECONDARY — Card Grid Executive)
└─ index.html                      (master viewer link ke kedua versi)
```

**Total: 4 file** (vs v2 = 15 file). Lean & focused.

## 8. Eksekusi (Wave-Based)

| Wave | Output | Justifikasi |
|------|--------|-------------|
| **1** | README.md | Index + executive summary + cara baca |
| **2** | versi-p-workflow-topology.html | Primary diagram, paling penting |
| **3** | versi-c-comparison-dashboard.html | Secondary, executive view |
| **4** | index.html | Master viewer minimal (link ke 2 versi) |
| **5** | Verifikasi + tag v3.0 | Final state |

## 9. Acceptance Criteria

| Criteria | Pass |
|----------|:----:|
| README.md ada dengan 2 versi rationale + struktur | ☐ |
| versi-p-workflow-topology.html render: 5 layer + komponen + marker A-F & 1-7 + DMZ buffer zone + connection lines + tabel komparasi inline + header logo | ☐ |
| versi-c-comparison-dashboard.html render: 7 card fitur + agregat + HC Portal hub showcase + tabel issue A-F | ☐ |
| index.html minimal link ke 2 versi | ☐ |
| Konsisten color palette (5 warna max) | ☐ |
| Typography hierarchy konsisten | ☐ |
| Print-friendly CSS (@media print) | ☐ |
| Bahasa Indonesia full | ☐ |
| Tidak ada placeholder TBD/TODO | ☐ |
| Tag `pcp-hcportal-3.4-v3.0` dibuat | ☐ |

## 10. Risiko & Mitigasi

| Risiko | Dampak | Mitigasi |
|--------|--------|----------|
| 2 versi membingungkan reviewer (mana primary?) | Reviewer skip | README jelaskan: P utama, C executive backup |
| Detail per fitur hilang (no swimlane lampiran) | Reviewer minta detail | Versi C card per fitur sudah cukup; siapkan TKI sebagai backup |
| Tooltip popup tidak terlihat saat print | Detail loss | Tooltip = supplementary, info utama di legend |
| Tabel komparasi inline bikin diagram panjang | Scrolling perlu | Section toggle / collapse di web view |

## 11. Out of Scope (Eksplisit)

- 7 swimlane lampiran per fitur (out — slide referensi tidak punya, v3 simpler)
- C4 Context, ArchiMate, SIPOC, VSM (out — opsi sudah ditolak)
- Mockup UI / screenshot portal (out — Design, optional)
- Formula matematis (N/A)
- PowerPoint export otomatis (manual redraw oleh user)
- Implementation code

## 12. Referensi

- Slide PCP template: `C:\Users\Administrator\OneDrive - PT Pertamina (Persero)\Documents\PCP SMART 2026 APQ Rev 9999 Final_1.png`
- TKI feature inventory: `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md`
- Recovery v1.0: `git checkout pcp-hcportal-3.4-v1.0 -- <path>`
- Recovery v2.0: `git checkout pcp-hcportal-3.4-v2.0 -- <path>`

## 13. Next Step

Setelah spec approved → invoke `superpowers:writing-plans` untuk implementation plan v3.
