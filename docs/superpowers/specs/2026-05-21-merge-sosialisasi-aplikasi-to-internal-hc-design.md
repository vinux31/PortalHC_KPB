# Spec — Merge Sosialisasi-Aplikasi ke Sosialisasi-Internal-Tim-HC

**Status:** Brainstorming Q1-Q5 locked, Design Sections 1-5 locked, ready for user review.
**Date:** 2026-05-21 (revised 2026-05-22)
**Resume:** baca spec ini, semua section locked → langsung ke writing-plans skill.

---

## Konteks File

| # | File | Baris | Role | Audience |
|---|---|---|---|---|
| 1 | `docs/Panduan-Operasional-HC-PortalHC-KPB.html` | 1955 | Reference doc (6 bab + 2 lampiran) | Tim HC — operasional detail |
| 2 | `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` | 2429 | Slide deck (22 slide) — **secondary/source** | Semua pekerja KPB — sosialisasi umum |
| 3 | `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` | 1815 | Slide deck (**23 slide**) — **target/utama** | Tim HC internal |

User goal: konten konseptual + lifecycle + arsitektur dari **File 2** → masuk **File 3** (slide deck Internal HC). File 1 (Panduan) di luar scope merge ini.

**Real slide map File 3 (verified):**
1. Cover · 2. Selamat Datang Tim HC · 3. Posisi HC di Portal · 4. Alur Kerja Harian HC · 5. CMP Overview · 6. Records Team · 7. Analytics Dashboard · 8. Pre/Post Test · 9. CDP Reviewer Chain · 10. Coaching Proton Dashboard · 11. Histori Proton + Export · 12. Renewal Certificate Lifecycle · 13. Silabus + Guidance Files · 14. Override Data Pekerja · 15. Admin Panel Landing · 16. Manajemen Pekerja · 17. Assessment Monitoring · 18. Coach-Coachee Mapping · 19. Maintenance + Audit Log · 20. Notifikasi & Workflow · 21. Tugas HC Cepat · 22. Reference Card · 23. Penutup

---

## Decisions Locked

### Q1 — Target file
**File 3** (`Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`). File 1 (Panduan) tidak disentuh sesi ini.

### Q2 — Scope konten
**Opsi A**: ambil **semua** slide gap (konten File 2 yang belum ada di File 3). **18 slide tambahan**. Deck final **41 slide**.

**Gap matrix (File 2 → belum ada di File 3):**

| Kategori | Slide File 2 yang masuk gap | Count |
|---|---|---|
| Konteks/Why | Latar Belakang, Apa Itu HC Portal, 3 Platform Terpadu, Cara Mengakses HC Portal | 4 |
| Role/Struktur | Struktur Role Pengguna | 1 |
| Assessment Lifecycle | Sistem Assessment, 5 Kategori Assessment Umum, Alur Assessment 7-Step E2E | 3 |
| Proton Lifecycle | Assessment Proton, Alur Proton Th 1&2, Alur Proton Th 3 | 3 |
| Coaching Architecture | Coaching Proton Dual Track, IDP & Training Records, Hierarki Kompetensi per Track, Progresi Kompetensi per Tahun | 4 |
| Coaching Workflow | Alur Coaching Reguler 9-Step, Alur Coaching Mahir 9-Step | 2 |
| Tech | Integrasi & Keamanan | 1 |
| **Total** | | **18** |

**Exclusion accounting (File 2 = 22 slide total):**
- **Meta (3, skip):** #1 Cover, #2 Agenda Presentasi, #22 Penutup — File 3 punya equivalent sendiri
- **Overlap (1, skip):** #10 Pre/Post Test — sudah ada di File 3 #8
- **Gap (18, merge):** sisanya (lihat tabel kategori di atas)
- Total: 22 − 3 − 1 = **18 gap** ✓

**Catatan File 3 native slides** (CMP Overview #5, CDP Reviewer Chain #9, Coaching Proton Dashboard #10): bukan overlap — File 2 tidak punya slide setara. Tetap di File 3 apa adanya.

**Quirk File 2 — HTML order ≠ data-slide order:**
- `data-slide="10"` (Pre/Post) muncul di HTML line 1676 SEBELUM `data-slide="9"` (Alur Assessment) di line 1726
- `data-slide="19"` (IDP) muncul di antara `data-slide="14"` dan `data-slide="15"` (HTML line 1938)
- **Implementor: source slide content by `data-slide` attribute / urutan logis di spec, JANGAN ikuti HTML position File 2.**

### Q3 — Insertion strategy
**Opsi A — Hybrid (cluster konteks di depan + lifecycle distributed per topic).**

**Insertion plan (anchor by real File 3 slide#):**

| Step | Insert cluster | Slides | Anchor (pre-shift) |
|---|---|---|---|
| 1 | Konteks (Latar Belakang, Apa Itu HC Portal, 3 Platform Terpadu, Struktur Role, Cara Mengakses) | 5 | setelah #2 (Selamat Datang) |
| 2 | Assessment Lifecycle (Sistem Assessment, 5 Kategori, Alur 7-Step) | 3 | sebelum #8 (Pre/Post Test) |
| 3 | Proton + Coaching (Assessment Proton, Alur Proton Th 1&2, Th 3, Dual Track, IDP & Training, Hierarki Kompetensi, Progresi Tahunan, Alur Coaching Reguler 9, Alur Coaching Mahir 9) | 9 | sebelum #10 (Coaching Proton Dashboard) |
| 4 | Tech (Integrasi & Keamanan) | 1 | sebelum #22 (Reference Card) |
| | **Total** | **18** | Final = 23 + 18 = **41 slide** |

**Sequential shift accounting:**
- Start 23 → +5 = 28 (#1-2 stable, Cluster Konteks #3-7, old #3+ shift +5)
- 28 → +3 = 31 (Assessment cluster di posisi yg dulu old #8, sekarang #13-15)
- 31 → +9 = 40 (Proton cluster di posisi yg dulu old #10, sekarang #18-26)
- 40 → +1 = 41 (Tech slide di posisi yg dulu old #22, sekarang #39)

Reference Card final = #40. Penutup final = #41.

### Q4 — Adaptasi tone konten
**Opsi B — Verbatim + sisip "Implikasi untuk HC" callout.**

Tiap slide gap = copy konten persis dari File 2, tambah callout box di slide. Detail markup + CSS di Design Section 3.

### Q5 — Output Strategy
**Opsi A — In-place update** `Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`, bump v1.1 → v2.0. Single source of truth, history via git.

---

## Design Sections

### Section 1 — Insertion Plan + Slide Template (LOCKED)

**Slide template (re-use File 3 pattern):**
```html
<div class="slide default-deco" data-slide="N">
  <div class="slide-header">
    <span class="slide-badge">SLIDE N / 41</span>
    <h1 class="slide-title">[Judul] <span class="accent">[Highlight]</span></h1>
    <p class="slide-subtitle">[Sub-judul opsional]</p>
  </div>
  <div class="slide-body">
    [Konten verbatim dari File 2 — adapt class palette ke File 3]
    <div class="hc-callout">
      <strong>Implikasi untuk HC</strong>
      <ul><li>[bullet operasional 1]</li><li>[bullet operasional 2]</li></ul>
    </div>
  </div>
</div>
```

**Insertion mapping & math:** lihat Q3 tabel di atas.

### Section 2 — Visual Palette Strategy (LOCKED)

**Opsi A — Adapt full ke palette File 3.** Semua slide gap repaint ke teal `#0d9488` + amber `#f59e0b`. Drop merah Pertamina `#ed1c24` + hijau `#009640` + purple dari File 2.

Implementasi: per slide gap, ganti color/class hardcoded File 2 → palette File 3:
- `#ed1c24` (red Pertamina) → `var(--teal)` `#0d9488`
- `#009640` (green Pertamina) → `var(--green)` `#10b981` (sudah ada di File 3)
- `#7b1fa2` (purple) → `var(--amber-dark)` `#d97706` atau `var(--slate-dark)` `#334155` per konteks (accent vs neutral)
- Class `.text-red*`/`.bg-red*` → equivalent teal/amber class atau inline `var(--teal)`

### Section 3 — Callout HTML + CSS Pattern (LOCKED)

**CSS class baru `.hc-callout`:**
```css
.hc-callout {
  margin-top: 20px;
  padding: 14px 18px;
  background: rgba(245, 158, 11, 0.10);
  border-left: 4px solid var(--amber-dark);
  border-radius: 8px;
  font-size: 0.92em;
  color: var(--text);
}
body.dark .hc-callout {
  background: rgba(245, 158, 11, 0.15);
  border-left-color: var(--amber);
}
.hc-callout strong {
  display: block;
  color: var(--amber-dark);
  margin-bottom: 6px;
  font-size: 0.95em;
  letter-spacing: 0.3px;
  text-transform: uppercase;
}
body.dark .hc-callout strong { color: var(--amber); }
.hc-callout ul { margin: 0; padding-left: 18px; }
.hc-callout li { margin: 3px 0; line-height: 1.5; }
```

**Markup pattern per slide gap:**
```html
<div class="hc-callout">
  <strong>Implikasi untuk HC</strong>
  <ul>
    <li>[verb operasional + step/topic]</li>
    <li>[opsional bullet ke-2]</li>
  </ul>
</div>
```

Posisi: akhir `.slide-body`, di bawah konten utama.

**Konten callout per slide:** lihat tabel "Catatan Konten Callout" di bawah.

### Section 4 — Navigation Update (LOCKED)

3 spot perlu touch di `Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`:

1. **Counter init text** (line 1761): `1 / 23` → `1 / 41`
2. **JS const TOTAL** (line 1767): `const TOTAL = 23;` → `const TOTAL = 41;`
3. **data-slide attributes**: re-number sequential 1..41 setelah insert 18 slide
4. **slide-badge text per slide**: update `SLIDE N / 23` → `SLIDE N / 41` di semua 23 slide existing + 18 baru

Mechanics re-number: insert dulu 18 slide baru tanpa data-slide value, lalu sweep semua `.slide` elemen → assign `data-slide="N"` sequential + `.slide-badge` text `SLIDE N / 41` lewat script Node/PowerShell one-shot. Verify count = 41 sebelum commit.

Tidak ada perubahan keybind (Arrow/Space/Home/End dynamic via `TOTAL`).
Tidak ada perubahan progress bar (formula `current / TOTAL * 100` dynamic).

### Section 5 — Testing Checklist (LOCKED)

**Manual smoke (wajib):**
- [ ] Counter init `1 / 41`, last slide = Penutup (#41)
- [ ] Navigation Arrow Right/Left/Space/Home/End sampai #41, no overflow, no skip
- [ ] Progress bar monotonic 0→100% slide 1→41
- [ ] Dark mode toggle: 18 slide baru terbaca, callout amber kontras, body text visible
- [ ] Print PDF (Ctrl+P, A4/Letter landscape): 41 page, callout tidak terpotong, `.no-print` hilang
- [ ] Visual smoke: 18 slide baru palette teal/amber konsisten (no merah Pertamina leftover)

**data-slide integrity (grep verify):**
- [ ] `grep -c 'data-slide=' Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` = 41
- [ ] data-slide values monotonic 1..41, no duplicate, no gap
- [ ] slide-badge text `SLIDE N / 41` sync dengan data-slide N

**Playwright (opsional, nice-to-have):**
- [ ] Loop 1..41 → screenshot light + dark per slide → baseline diff
- [ ] Test keyboard nav (ArrowRight × 40 → counter = `41 / 41`)
- [ ] Test progress bar width = `current / 41 * 100%`

---

## Catatan Konten Callout

Draft 1-line "Implikasi untuk HC" per slide gap:

| Slide gap | Draft callout HC |
|---|---|
| Latar Belakang | HC: jembatan antara user pain "manual" → adopsi platform terintegrasi |
| Apa Itu HC Portal | HC: owner data + reviewer final di semua workflow Proton |
| 3 Platform Terpadu | HC operate di semua 3 (CMP analytics, CDP coaching, BP via integrasi) |
| Struktur Role | HC = L2 authority; scope: cross-section, final reviewer |
| Cara Mengakses | HC akses sama dengan user umum, tapi role-gated menu (Bab 1.2 Panduan) |
| Sistem Assessment | HC: setup jadwal, bank soal, monitor real-time, force-close |
| 5 Kategori Assessment | HC: assign kategori per jadwal, lihat fail rate per kategori (Analytics) |
| Alur Assessment 7-Step | HC: step 1 (setup), step 5 (monitor), step 7 (manual entry + cert) |
| Assessment Proton | HC: reviewer final chain Proton, manage silabus + guidance files |
| Alur Proton Th 1&2 | HC: jaga deliverable submission timeline, eskalasi bottleneck |
| Alur Proton Th 3 | HC: validasi mahir, certification renewal management |
| Coaching Dual Track | HC: monitor kedua track via Coaching Proton Dashboard |
| IDP & Training Records | HC: review IDP coachee, audit training records team |
| Hierarki Kompetensi | HC: gunakan KKJ matrix untuk gap analysis CPDP Mapping |
| Progresi Kompetensi per Tahun | HC: track progresi via Analytics + Bottleneck Report |
| Alur Coaching Reguler 9-Step | HC: reviewer final di step 8-9 (approval chain) |
| Alur Coaching Mahir 9-Step | HC: validation mahir + sertifikasi (Renewal Certificate Mgmt) |
| Integrasi & Keamanan | HC: tanggung jawab audit log review, impersonate dengan justifikasi |

---

## Pending Sections

- [x] Section 1: file structure, slide template
- [x] Section 2: visual palette strategy
- [x] Section 3: callout HTML + CSS pattern
- [x] Section 4: navigation update
- [x] Section 5: testing checklist
- [x] Spec self-review (placeholder/contradiction/ambiguity scan)
- [ ] User review spec
- [ ] Invoke writing-plans skill

---

## File yang Disentuh

- `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` — update in-place, v1.1 → v2.0
- `docs/superpowers/specs/2026-05-21-merge-sosialisasi-aplikasi-to-internal-hc-design.md` — this spec

## File Source (read-only reference)

- `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` — sumber konten gap (slide File 2)
