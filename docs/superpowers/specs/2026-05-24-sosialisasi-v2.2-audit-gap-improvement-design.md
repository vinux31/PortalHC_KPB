# Sosialisasi-Internal-Tim-HC v2.2 — Audit Gap + Improvement Design

**Tanggal:** 2026-05-24
**File target:** `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`
**Status:** Design draft — pending user review
**Base version:** v2.1 (34 slide, tag `sosialisasi-internal-hc-v2.1` di main lokal)

---

## Konteks

Audit deck v2.1 (34 slide) versus alur kerja HC harian + kelengkapan UI tutorial Admin Panel. Ditemukan 7 gap material kritis (6 Admin menu UI tutorial + 1 Help system overview) + 3 improvement slide existing.

**Audit summary:**
- Sl22 Admin Landing claim **14 menu** HC kelola, deck v2.1 cuma cover **8 menu** dengan UI tutorial (gap: Organization, KKJ Files, CPDP Sync, Coach Workload, Categories, Certificate Renewal).
- **CMP Guide / Help System** (refactored terminal 2 — Guide.cshtml + GuideContentProvider + 6 RoleGroup) tidak ada slide overview. HC perlu tahu untuk arahin user self-service.
- **Onboarding Pekerja Baru E2E** (ImportWorkers + assign role + first login) tidak ada flow narrative.
- Sl7 Notif cuma 3 contoh notif, tidak ada tabel tipe notif lengkap.
- Sl4 Role stair visual bagus, tidak ada matriks role × menu visibility.
- Sl33 Quick Ref bagus, tidak ada keyboard shortcut / URL cheatsheet / link ke Guide.

**Target:** 34 → 41 slide (+7 new + 3 improvement edit slide existing).

---

## Section Map Sebelum vs Sesudah

| # Sebelum | Title | # Sesudah | Action |
|---|---|---|---|
| 1 | Cover | 1 | unchanged |
| 2 | Selamat Datang | 2 | unchanged |
| 3 | 3 Platform | 3 | unchanged |
| 4 | Struktur Role | 4 | **I3 EDIT** — tambah matriks role × menu |
| 5 | Cara Mengakses | 5 | unchanged |
| — | **NEW** | **6** | **G6 CMP Guide / Help System** |
| 6 | Area Kerja HC | 7 | shift +1 |
| 7 | Alur Harian + Notif | **8** | **I2 EDIT** — tambah tabel tipe notif |
| 8 | CMP Overview | 9 | shift +1 |
| 9 | Records Team + Analytics | 10 | shift +1 |
| 10 | Sistem Assessment | 11 | shift +1 |
| 11 | 5 Kategori Assessment | 12 | shift +1 |
| 12 | Alur Assessment 7 Step | 13 | shift +1 |
| 13 | Pre/Post Test | 14 | shift +1 |
| 14 | IDP Library | 15 | shift +1 |
| 15 | Assessment Proton | 16 | shift +1 |
| 16 | Alur PROTON Th 1-2 vs Th 3 | 17 | shift +1 |
| 17 | Progresi Kompetensi | 18 | shift +1 |
| 18 | Coaching Chain + Dual | 19 | shift +1 |
| 19 | Alur Coaching Reg vs Mahir | 20 | shift +1 |
| 20 | Coaching Dashboard | 21 | shift +1 |
| 21 | Renewal Certificate Lifecycle | 22 | shift +1 |
| 22 | Admin Panel Landing | 23 | shift +1 |
| — | **NEW** | **24** | **G1 Organization Management** |
| 23 | Manajemen Pekerja | 25 | shift +2 |
| — | **NEW** | **26** | **G7 Onboarding Pekerja Baru E2E** |
| — | **NEW** | **27** | **G2 KKJ Files + CPDP Sync** |
| 24 | Coach-Coachee Mapping | 28 | shift +4 |
| — | **NEW** | **29** | **G3 Coach Workload** |
| 25 | Silabus + Guidance | 30 | shift +5 |
| 26 | Manage Package Question | 31 | shift +5 |
| 27 | Override KKJ | 32 | shift +5 |
| — | **NEW** | **33** | **G4 Categories CRUD** |
| 28 | Create Assessment Overview | 34 | shift +6 |
| 29 | Create Assessment Detail | 35 | shift +6 |
| 30 | Assessment Monitoring | 36 | shift +6 |
| 31 | Monitoring Actions | 37 | shift +6 |
| — | **NEW** | **38** | **G5 Certificate Renewal UI** |
| 32 | Maintenance + Audit Log | 39 | shift +7 |
| 33 | Quick Reference | 40 | shift +7, **I6 EDIT** — keyboard shortcut + URL cheatsheet + ref-card Guide |
| 34 | Terima Kasih | 41 | shift +7 |

**Total 7 slide baru insert + 3 slide edit + 35 slide shift renumber.**

---

## Layout Pattern (Konsisten Existing)

Semua slide baru pakai struktur Bagian 4:

```html
<div class="slide default-deco" data-slide="{N}">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">BAGIAN {X} &mdash; {TITLE}</p>
      <h1 class="slide-title">{Title} <span class="accent">{Accent}</span></h1>
      <p class="slide-subtitle">{Subtitle}</p>
    </div>
    <div class="slide-badge">SLIDE {N} / 41</div>
  </div>
  <div class="slide-body">
    <div class="slide-mockup-split">
      <div class="mockup-frame">{...mockup...}</div>
      <div class="mockup-content">{...content...}</div>
    </div>
    <div class="tip-bar">{fungsi}</div>
  </div>
  <p class="panduan-ref">{ref}</p>
</div>
```

**Icon convention:** HTML entity emoji (`&#NNNN;`) — NO Bootstrap Icons.
**CSS reuse:** `slide-mockup-split`, `mockup-frame/bar/recreated`, `mockup-content/tip/warn`, `tip-bar`, `panduan-ref`, `mr-table/btn/badge-pill/tab-strip/filter-chip`.

---

## Slide G6 NEW — CMP Guide / Help System

**Posisi:** Sl6 (Bagian 0, setelah Sl5 Akses)
**Eyebrow:** BAGIAN 0 — PENGENALAN
**Title:** CMP Guide <span class="accent">Help System</span>
**Subtitle:** Panduan self-service per RoleGroup — accordion 6 modul + 3 PDF download
**Route:** `localhost:5277/KPB-PortalHC/Home/Guide`

### Mockup (kiri)
- **Header tabs**: 6 RoleGroup (All · AdminHC · Manager · Atasan · Coach · Coachee) — chip filter
- **Accordion 6 modul** (per Guide.cshtml structure): Profile · CMP · CDP · BP · Admin Panel · Sistem
- 2 accordion expanded sample:
  - "CMP" → 6 sub-item link
  - "Admin Panel" → 8 sub-item link (visible kalau role AdminHC)
- **3 PDF download button** top-right: `Panduan Coachee v1.2` · `Panduan Admin v2.0` · `Panduan Umum`

### Content (kanan)
- **h4** `📚 Guide System — Self-Service`
- bullet RoleGroup behavior: 6 group, `All` = visible semua, `AdminHC` = HC + Admin only, dst
- **HC Use Case**:
  - User tanya cara X → "lihat Guide modul Y" (deflect to self-service)
  - PDF cetak untuk offline (Admin v2.0 = manual lengkap HC)
  - Update content via `GuideContentProvider.cs` (developer edit)
- **mockup-tip**: `💡 Guide accessible dari navbar top-right Profile dropdown → "Panduan"`

### Tip-bar
`📝 Fungsi: Sistem panduan self-service per RoleGroup. HC arahin user ke Guide sebelum jawab manual. 3 PDF download untuk reference offline.`

### Panduan ref
`Panduan Operasional HC — Bab 2 Help System & Guide`

---

## Slide G1 NEW — Organization Management

**Posisi:** Sl24 (Bagian 4, setelah Sl23 Admin Landing)
**Eyebrow:** BAGIAN 4 — ADMIN PANEL
**Title:** Organization <span class="accent">Management</span>
**Subtitle:** Hierarki Bagian → Unit → Section · CRUD master struktur organisasi
**Route:** `localhost:5277/KPB-PortalHC/Admin/ManageOrganization`

### Mockup (kiri)
- **Tree view hierarki** 3-level:
  - Operations (Bagian)
    - Alkylation (Unit)
      - Section A (Section)
      - Section B (Section)
    - RFCC (Unit)
    - NHT (Unit)
  - HSSE (Bagian)
  - Maintenance (Bagian)
- **Form Add Node**: Parent (dropdown) · Nama · Kode · Sort Order · Aktif/Nonaktif
- Per-node action: `✏ Edit` `🔄 Toggle Aktif` `🗑 Hapus`

### Content (kanan)
- **h4** `🏢 3-Level Hierarchy`
- bullet: Bagian (root) → Unit (child) → Section (grandchild)
- **mockup-warn**: `⚠ Cascade delete:` Hapus Bagian = hapus semua Unit + Section di bawahnya. Pakai Nonaktif dulu sebelum Hapus.
- **mockup-tip**: `💡 Mapping ke pekerja:` Worker Management (Sl25) pakai dropdown dari sini.

### Tip-bar
`📝 Fungsi: Master data struktur organisasi 3-level. Drive dropdown unit/bagian di Worker Management + Coach Mapping + Filter Monitoring.`

### Panduan ref
`Panduan Operasional HC — §5.1 Organization`

---

## Slide G7 NEW — Onboarding Pekerja Baru E2E

**Posisi:** Sl26 (Bagian 4, setelah Sl25 Manajemen Pekerja)
**Eyebrow:** BAGIAN 4 — ADMIN PANEL
**Title:** Onboarding <span class="accent">Pekerja Baru</span> — E2E
**Subtitle:** Import Excel → Assign Role → First Login → Reset Password
**Route:** `localhost:5277/KPB-PortalHC/Admin/ImportWorkers` + alur narrative

### Mockup (kiri)
- **4-step flow chart vertikal** (atau horizontal):
  1. **📥 Download Template** — Excel kolom (NIP, Nama, Email, Bagian, Unit, Role)
  2. **📤 Upload + Validate** — drag-drop / klik, server validate format + duplicate NIP
  3. **🔍 Preview Result** — table preview "X created, Y error, Z duplicate"
  4. **✅ Confirm + Assign Role** — bulk assign role via Worker Management edit
- **Mockup ImportWorkers UI**: drop zone + button download template + result table

### Content (kanan)
- **h4** `🚀 Onboarding Lifecycle`
- **ul Steps**:
  - Step 1 — Template Excel (download dari ImportWorkers UI)
  - Step 2 — Upload `.xlsx` (validate NIP unik, email valid, role exist)
  - Step 3 — Review error per baris (fix di Excel + re-upload, atau partial import)
  - Step 4 — Default password dikirim ke email (atau set manual)
- **First Login Flow**: user terima email → klik link → set password baru → masuk Portal
- **mockup-tip**: `💡 Bulk role assign:` Setelah import, pakai Worker Management filter "Belum ada role" → bulk-edit
- **mockup-warn**: `⚠ Email salah:` Import success tapi notif email tidak nyampe — verify kolom email sebelum upload.

### Tip-bar
`📝 Fungsi: E2E onboarding pekerja baru — dari import Excel sampai first login. 4 step, ~5 menit per batch 20 pekerja.`

### Panduan ref
`Panduan Operasional HC — §5.2 Worker Import + First Login Flow`

---

## Slide G2 NEW — KKJ Files + CPDP Sync

**Posisi:** Sl27 (Bagian 4, setelah G7)
**Eyebrow:** BAGIAN 4 — ADMIN PANEL
**Title:** KKJ Files + <span class="accent">CPDP Sync</span>
**Subtitle:** Master data kompetensi (KKJ) + sync data eksternal (CPDP)
**Routes:** `/Admin/KkjUpload` + `/Admin/CpdpUpload`

### Mockup (kiri) — Split 2 mini-frame
**Frame A — KKJ Files** (atas):
- URL: `Admin/KkjUpload`
- Tabs: `Upload` · `Matrix` · `History`
- Tabel matrix mini: Jabatan × Kompetensi (cell = required/optional)
- Button: `📤 Upload .xlsx` · `📊 View Matrix` · `📋 History`

**Frame B — CPDP Sync** (bawah):
- URL: `Admin/CpdpUpload`
- Drop zone Excel
- History sync tabel: Tanggal · File · Status · User
- Button: `📤 Upload CPDP` · `📋 File History`

### Content (kanan)
- **h4** `📊 KKJ vs CPDP — Difference`
- **tabel ringkas**:
  | Aspek | KKJ Files | CPDP Sync |
  |---|---|---|
  | Isi | Matrix kompetensi per jabatan | Data pekerja eksternal (master HR) |
  | Source | Manual Excel HC | Sistem HR korporat |
  | Frekuensi | Per perubahan jabatan | Berkala (weekly/monthly) |
  | Impact | KKJ assessment requirement | Sync data pekerja terbaru |
- **mockup-tip**: `💡 CPDP frequency:` cek History sebelum sync ulang — hindari duplicate. KKJ Matrix sebaiknya update saat ada jabatan baru.

### Tip-bar
`📝 Fungsi: 2 master data input untuk drive assessment. KKJ = kompetensi per jabatan. CPDP = sync pekerja eksternal.`

### Panduan ref
`Panduan Operasional HC — §5.3 KKJ + CPDP Sync`

---

## Slide G3 NEW — Coach Workload

**Posisi:** Sl29 (Bagian 4, setelah Sl28 Coach Mapping)
**Eyebrow:** BAGIAN 4 — ADMIN PANEL
**Title:** Coach <span class="accent">Workload</span> — Distribusi & Penyeimbangan
**Subtitle:** Tabel beban coach + warna threshold + rekomendasi redistribusi
**Route:** `localhost:5277/KPB-PortalHC/Admin/CoachWorkload`

### Mockup (kiri)
- **Filter row**: Section dropdown + Apply button
- **Chart mini**: Bar chart coach × jumlah coachee (canvas placeholder)
- **Tabel workload**:
  | Nama Coach | Section | Jumlah | Daftar Coachee | Status |
  |---|---|---|---|---|
  | Andi P. | Alkylation | 8 | (expandable) | `🔴 Overload` |
  | Budi S. | Alkylation | 5 | (expandable) | `🟢 Normal` |
  | Citra R. | RFCC | 2 | (expandable) | `🟡 Under` |
- Threshold legend: Overload >7 (red) · Normal 3-7 (green) · Under <3 (yellow)

### Content (kanan)
- **h4** `⚖ Workload Balance`
- bullet thresholds:
  - **🔴 Overload >7 coachee** — pertimbangkan redistribusi
  - **🟢 Normal 3-7** — sweet spot
  - **🟡 Under <3** — bisa terima coachee tambahan
- **mockup-tip**: `💡 Weekly task:` cek halaman ini setiap Senin (selaras Sl33 Quick Ref Weekly checklist). Redistribusi via Coach Mapping (Sl28).
- **mockup-warn**: `⚠ Empty state:` "Belum ada mapping aktif" → arahin ke Coach Mapping dulu.

### Tip-bar
`📝 Fungsi: Real-time view distribusi coach-coachee. Identifikasi over/under capacity. Trigger redistribusi via Coach Mapping.`

### Panduan ref
`Panduan Operasional HC — §5.4 Coach Workload Balance`

---

## Slide G4 NEW — Categories CRUD

**Posisi:** Sl33 (Bagian 4, sebelum Sl34 Create Asm Overview)
**Eyebrow:** BAGIAN 4 — ADMIN PANEL
**Title:** Categories <span class="accent">CRUD</span> — Master Kategori Assessment
**Subtitle:** Parent-child hierarchy · Default passing % · Signatory user binding
**Route:** `localhost:5277/KPB-PortalHC/Admin/ManageCategories`

### Mockup (kiri)
- **Tabel parent-child** dengan indent visual:
  | Nama | Default Pass % | Sort | Signatory | Aksi |
  |---|---|---|---|---|
  | **HSSE** (parent) | 75% | 1 | Manager HSSE | Edit · Hapus |
  | **Operations** (parent) | 70% | 2 | Manager Ops | Edit · Hapus |
  | ↳ Alkylation (child) | 70% | 1 | — | Edit · Hapus |
  | ↳ RFCC (child) | 70% | 2 | — | Edit · Hapus |
  | ↳ NHT (child) | 70% | 3 | — | Edit · Hapus |
  | **OJT** (parent) | 80% | 3 | Sr Supervisor | Edit · Hapus |
- **Form Add Category**: Nama · Parent (dropdown, optional) · Default Pass % · Sort Order · Signatory User · Aktif

### Content (kanan)
- **h4** `🏷 Category Hierarchy`
- bullet: Parent kategori (root) + Child kategori (sub-spesifik per unit)
- **Field penting**:
  - **Default Passing %** — auto-fill saat Create Assessment (Sl34 Step 1)
  - **Signatory User** — penandatangan sertifikat (kalau lulus)
  - **Sort Order** — urutan tampil di dropdown Create Asm
- **mockup-tip**: `💡 Linked ke Sl34:` Kategori di sini muncul di Create Assessment Step 1 dropdown (optgroup parent-child).
- **mockup-warn**: `⚠ Cascade delete:` Hapus parent = hapus semua child + assessment terkait. Pakai Nonaktif dulu.

### Tip-bar
`📝 Fungsi: Master kategori assessment (5 kategori umum + Proton + custom). Drive dropdown Create Assessment + signatory sertifikat.`

### Panduan ref
`Panduan Operasional HC — §5.5 Categories CRUD`

---

## Slide G5 NEW — Certificate Renewal UI

**Posisi:** Sl38 (Bagian 4, setelah Sl37 Monitoring Actions)
**Eyebrow:** BAGIAN 4 — ADMIN PANEL
**Title:** Certificate <span class="accent">Renewal</span> UI
**Subtitle:** Filter sertifikat near-expired + bulk schedule renewal · auto-create assessment
**Route:** `localhost:5277/KPB-PortalHC/Admin/RenewalCertificate`

### Mockup (kiri)
- **Filter row** (6 dropdown):
  - Bagian · Unit · Kategori · Sub-Kategori · Tipe (Assessment/Training) · Status
- **Tabel sertifikat near-expired**:
  | Pekerja | Sertifikat | Expired | Sisa Hari | Status | Pilih |
  |---|---|---|---|---|---|
  | Widodo A. | LOTO | 2026-06-15 | `H-22` 🔴 | Aktif | ☑ |
  | Andi P. | Hot Work | 2026-07-08 | `H-45` 🟡 | Aktif | ☑ |
  | Citra R. | Pump Op | 2026-08-20 | `H-88` 🟢 | Aktif | ☐ |
  | Budi S. | ESD | 2026-09-12 | `H-111` ⚪ | Aktif | ☐ |
- Bottom action: `📅 Bulk Schedule Renewal` button + count "2 selected"
- Legend: 🔴 <30 hari · 🟡 30-60 · 🟢 60-90 · ⚪ >90

### Content (kanan)
- **h4** `⏳ Renewal Lifecycle`
- bullet: filter near-expired → bulk select → schedule renewal → auto-create assessment (Pre-fill kategori + peserta)
- **mockup-warn**: `⚠ Tidak campur tipe:` Bulk renew Assessment + Training campuran ditolak. Filter Tipe dulu.
- **mockup-tip**: `💡 Monthly task:` cek H-60 setiap awal bulan (selaras Sl40 Quick Ref Monthly). Auto-notify pekerja H-30/14/7.

### Tip-bar
`📝 Fungsi: Workflow renewal sertifikat near-expired. Bulk schedule untuk efisiensi. Linked Renewal Lifecycle Sl22 (konsep).`

### Panduan ref
`Panduan Operasional HC — §5.6 Certificate Renewal`

---

## Improvement I3 — Sl4 Role: Tambah Matriks

**Posisi:** Sl4 (no shift)
**Change:** Tambah section **Matriks Role × Menu Visibility** di bawah stair existing.

### Content Tambahan
Tabel 6 level × 5 top-level area:

| Level | Role | CMP | CDP | Admin Panel | Profile | Guide |
|---|---|---|---|---|---|---|
| L1 | Admin | ✓ All | ✓ All | ✓ All | ✓ | ✓ All access |
| L2 | HC | ✓ All | ✓ All | ✓ All (14 menu) | ✓ | ✓ AdminHC group |
| L3 | Direktur / VP / Manager | ✓ View Dashboard | ✓ View Dashboard | ❌ | ✓ | ✓ Manager group |
| L4 | Section Head / Sr Supervisor | ✓ Section data | ✓ Section data | ❌ | ✓ | ✓ Atasan group |
| L5 | Coach / Supervisor | ✓ Coachee data | ✓ Coachee mapping | ❌ | ✓ | ✓ Coach group |
| L6 | Coachee | ✓ Self assessment | ✓ Self IDP | ❌ | ✓ | ✓ Coachee group |

**Tip bar updated:** add hint "Cross-ref Guide RoleGroup (G6) — 6 group filter di Help system selaras matriks ini."

---

## Improvement I2 — Sl7 (→Sl8) Notif: Tambah Tabel Tipe Notif

**Posisi:** Sl8 (was Sl7, shift +1)
**Change:** Tambah section **Tabel Tipe Notif Lengkap** di bawah mockup bell dropdown existing.

### Content Tambahan
Tabel 10 tipe notif:

| Kategori | Tipe Notif | Trigger | Penerima |
|---|---|---|---|
| **Evidence** | Approved by Section Head | Atasan review approve | HC (final reviewer) |
| Evidence | Rejected — Chain Reset | Atasan reject | HC + Coach + Coachee |
| Evidence | Submitted by Coach | Coach submit deliverable | Atasan (next reviewer) |
| **Renewal** | H-90 Warning | Auto cron daily | HC + Pekerja |
| Renewal | H-30 Critical | Auto cron daily | HC + Pekerja + Atasan |
| Renewal | H-7 Last Call | Auto cron daily | HC + Atasan eskalasi |
| **Assessment** | Assessment Created | HC publish | Peserta + Coach |
| Assessment | Force-Close by HC | HC akhiri ujian | Peserta |
| **System** | Override KKJ Logged | HC override manual | Admin (audit) |
| System | Maintenance Mode On | Admin enable | All users |

---

## Improvement I6 — Sl33 (→Sl40) Quick Ref: Expand

**Posisi:** Sl40 (was Sl33, shift +7)
**Changes:**
1. Tambah section **Keyboard Shortcut Table** di bawah Daily/Weekly/Monthly checklist:
   - `Ctrl+/` — Quick search (kalau ada)
   - `Esc` — Close modal
   - `←` / `→` — Navigate slide (di deck ini)
   - `Home` / `End` — First/Last slide
2. Tambah section **URL Cheatsheet** (6 route penting):
   - `/Admin` — Admin Panel landing
   - `/Admin/AssessmentMonitoring` — Real-time monitor
   - `/Admin/RenewalCertificate` — Renewal queue
   - `/Admin/CoachWorkload` — Workload balance
   - `/Home/Guide` — Help system
   - `/Admin/AuditLog` — Investigation
3. Tambah ref-card ke **3 existing ref-card**:
   - **CMP Guide (Help System)** — `/Home/Guide` (link ke G6 slide)
4. Tambah tip line: `💡 Password reset = self-service via login page "Lupa Password?" link. HC tidak punya tool reset langsung — arahin user.`

---

## Renumber Strategy

**Total: 35 slide shift + 8 insert + 3 edit. Complex.**

**6-pass strategy (descending shift dulu, avoid collision):**

### Pass 1: Shift `data-slide` existing (descending dari Sl34 ke Sl6)

Edit attribute `data-slide="N"` → `data-slide="N+shift"`. Lakukan descending:

| Old | New | Shift |
|---|---|---|
| 34 | 41 | +7 |
| 33 | 40 | +7 |
| 32 | 39 | +7 |
| 31 | 37 | +6 |
| 30 | 36 | +6 |
| 29 | 35 | +6 |
| 28 | 34 | +6 |
| 27 | 32 | +5 |
| 26 | 31 | +5 |
| 25 | 30 | +5 |
| 24 | 28 | +4 |
| 23 | 25 | +2 |
| 22 | 23 | +1 |
| 21 | 22 | +1 |
| 20 | 21 | +1 |
| 19 | 20 | +1 |
| 18 | 19 | +1 |
| 17 | 18 | +1 |
| 16 | 17 | +1 |
| 15 | 16 | +1 |
| 14 | 15 | +1 |
| 13 | 14 | +1 |
| 12 | 13 | +1 |
| 11 | 12 | +1 |
| 10 | 11 | +1 |
| 9 | 10 | +1 |
| 8 | 9 | +1 |
| 7 | 8 | +1 |
| 6 | 7 | +1 |
| 5, 4, 3, 2, 1 | unchanged | 0 |

### Pass 2: Shift badge text (same descending order)

`SLIDE N / 34` → `SLIDE N+shift / 41`. Lakukan descending. Sl1 cover + Sl34 (now Sl41) penutup tidak punya badge — skip.

### Pass 3: Cascade denominator Sl2..Sl5 (no shift, denominator only)

` / 34</div>` → ` / 41</div>` via `replace_all` setelah Pass 2 selesai (Pass 2 sudah handle Sl6+ shifted slides denominator).

**Sequence aman:** Pass 2 sudah update badge shifted slides ke `/ 41`. Sisanya Sl2..Sl5 masih `/ 34`. Pass 3 = `replace_all ' / 34</div>' → ' / 41</div>'` aman karena tidak ada match `/ 34` lain.

### Pass 4: JS `TOTAL` + slideCounter

| Line | Element | Sebelum | Sesudah |
|---|---|---|---|
| 3748 | `<span id="slideCounter">` | `1 / 34` | `1 / 41` |
| 3754 | `const TOTAL` | `= 34` | `= 41` |

### Pass 5: HTML section comments shifted

Update 29 comment `<!-- SLIDE N: ... =====` untuk slide shifted (Sl6..Sl34). Maintenance grep-ability.

### Pass 6: Insert 7 new slides + edit 3 existing

- Insert G6 di posisi Sl6 (sebelum old-Sl6 yang sekarang Sl7)
- Insert G1 di Sl24 (sebelum old-Sl23 sekarang Sl25)
- Insert G7 di Sl26 (sesudah Sl25 Pekerja)
- Insert G2 di Sl27 (sesudah G7)
- Insert G3 di Sl29 (sesudah old-Sl24 sekarang Sl28 Coach Map)
- Insert G4 di Sl33 (sebelum old-Sl28 sekarang Sl34 Create Asm)
- Insert G5 di Sl38 (sesudah old-Sl31 sekarang Sl37 Monitor Actions)

Edit:
- Sl4: tambah matriks role × menu (I3)
- Sl8: tambah tabel tipe notif (I2)
- Sl40: tambah keyboard shortcut + URL cheatsheet + ref-card + tip (I6)

---

## Verifikasi & Test Plan

1. **Count check**:
   - `data-slide=` = 42 (41 slides + 1 JS selector)
   - `class="slide-badge"` = 39 (Sl2..Sl40 — Sl1 cover + Sl41 penutup no badge)
   - ` / 34</div>` = 0
   - ` / 41</div>` = 39
   - `const TOTAL = 41;` + `slideCounter">1 / 41<`

2. **Sequential check**: `data-slide="N"` count = 1 untuk N=1..41.

3. **Keyboard nav**: `End` → Sl41, `Home` → Sl1.

4. **Browser visual** (Playwright auto):
   - 7 slide baru muat tanpa overflow 1280×720
   - 3 slide edit (Sl4, Sl8, Sl40) tetap muat setelah tambah konten — risk overflow tinggi, mungkin perlu compress font

5. **Dark mode** toggle — 7 slide baru contrast OK.

6. **Cross-ref check**:
   - Sl4 matrix mention Guide RoleGroup → link/tip ke Sl6 G6
   - Sl8 notif → ref Sl37 Monitor Actions (Force-Close trigger notif)
   - Sl22 Renewal Lifecycle → link/tip ke Sl38 G5 (UI tutorial)
   - Sl34 Create Asm Step 1 → link ke Sl33 G4 (Categories source)

---

## Out of Scope

- ❌ Tidak refactor section eyebrow (mis. pindah Sl15 IDP dari "BAGIAN 1 CMP" ke "BAGIAN 1.5 CDP") — out of scope, classification ambiguity diterima.
- ❌ Tidak buat versi v3 deck baru — edit v2 in-place jadi v2.2.
- ❌ Tidak refactor 8 slide existing yang tidak disebut di improvement (cuma I2/I3/I6).
- ❌ Tidak buat slide Q&A reserved / troubleshooting / mobile responsive (LOW priority).
- ❌ Tidak buat slide BP modul (Future-state, belum dibangun per Sl3).

---

## Open Questions (User Review)

1. **Mockup data sample**: pakai NIP 754201..754204 (Widodo, Andi, Citra, Budi) konsisten v2.1 — OK?
2. **Panduan ref §**: pakai placeholder `§5.X` per slide baru. Kalau Panduan Operasional HC actual punya nomor section beda, update later.
3. **G7 Onboarding** — default password policy: dikirim email vs HC set manual vs random hash → mana yang real di codebase? (Asumsi: email-based set new password via token link, standar ASP.NET Identity.)
4. **I3 Sl4 Matrix** — kalau tambah matriks 6×5, slide muat? Stair visual + matrix mungkin overflow → mockup font size 7pt di matrix.
5. **G5 Cert Renewal** — `H-day` color threshold (30/60/90) — confirm dari codebase atau asumsi?
6. **Tag release** post-merge — `sosialisasi-internal-hc-v2.2`?
7. **CDP eyebrow classification** — IDP (Sl15) ada di "BAGIAN 1 CMP" tapi Sl3 bilang CDP. Biarkan ambigu atau fix? **Default: skip (out of scope).**
