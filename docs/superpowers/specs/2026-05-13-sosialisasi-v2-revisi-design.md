# Sosialisasi-v2 Revisi — Design Spec

**Tanggal:** 2026-05-13
**File target:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`
**Scope:** Revisi slide 2, 3, 5, 12, 13 + tambah 5 slide baru (3 BigMenu + Pre/Post Test + IDP/Training Records).
**Total slide:** 15 → 20.

## Context

Sosialisasi-v2 v1 (15 slide) sudah ship tag `sosialisasi-v1.0`. User review menemukan beberapa gap:

1. Definisi HC Portal di slide 2 hanya menyebut "Tim HC" dan CMP — belum cakup tiga platform (CMP/CDP/BP).
2. Tiga card sub-modul slide 2 (Assessment/Coaching/Sertifikasi) belum mewakili portal secara utuh.
3. Tidak ada slide khusus penjelasan tiap big menu (CMP, CDP, BP).
4. Slide 3 hanya tampil 5 role utama; 5 role lain (Direktur, VP, Manager, Section Head, Sr Supervisor) hanya disebut di catatan kaki.
5. Alur slide 5/12/13 belum ada panah antar-card → audiens bingung urutan.
6. Beberapa step di alur tidak sesuai dengan implementasi codebase (review workflow, interview mode).
7. Fitur Pre/Post Test (v14.0) dan IDP/Training Records belum diceritakan eksplisit.

Spec ini revisi semua isu di atas dalam satu pass.

## Decisions (from brainstorm)

| Topic | Decision |
|---|---|
| Definisi BP | **HRBP** — HC strategic partner, liaison HC↔unit, workforce planning, employee relations |
| Cakupan CMP | Assessment OJT + Assessment Proton + Pre/Post Test + Sertifikasi |
| Cakupan CDP | Coaching Proton + IDP + Training Records |
| Definisi slide 2 | Versi A — satu kalimat ringkas, sebut 3 platform (CMP/CDP/BP) |
| 3 card slide 2 | Tiga value: **Terpusat · Terstandar · Terukur** |
| Layout role slide 3 | **Piramida vertikal 6 tier**, semua 10 role tampil |
| Pattern arrow alur | → antar card, ↓ antar row (snake-flow) |
| Audit fix | Slide 12 reviewer, slide 13 interview mode, slide 5 merge distribusi |
| Slide tambahan | 3 BigMenu (CMP/CDP/BP) + Pre/Post Test + IDP/Training Records |

## Urutan Slide Baru

| # Baru | # Lama | Slide | Status |
|---|---|---|---|
| 1 | 1 | Cover | unchanged |
| 2 | 2 | Definisi HC Portal | **REWRITE** |
| 3 | — | **BigMenu CMP** | **NEW** |
| 4 | — | **BigMenu CDP** | **NEW** |
| 5 | — | **BigMenu BP** (HRBP — For Future) | **NEW** |
| 6 | 3 | Role (piramida 6 tier) | **REWRITE** |
| 7 | 4 | Sistem Assessment CMP | unchanged |
| 8 | — | **Pre/Post Test** | **NEW** |
| 9 | 5 | Alur Assessment | **FIX** (7-step + arrow) |
| 10 | 6 | Assessment Proton | unchanged |
| 11 | 7 | Alur Proton Th 1-2 | unchanged (sudah ada →) |
| 12 | 8 | Alur Proton Th 3 | unchanged (sudah ada →) |
| 13 | 9 | Coaching CDP Overview | unchanged |
| 14 | — | **IDP & Training Records** | **NEW** |
| 15 | 10 | Hierarki Kompetensi | unchanged |
| 16 | 11 | Fokus Kompetensi (Table 4x5) | unchanged |
| 17 | 12 | Alur Coaching Th 1-2 | **FIX** (review multi-role + arrow) |
| 18 | 13 | Alur Coaching Th 3 | **FIX** (interview→coaching intensif + arrow) |
| 19 | 14 | Timeline Summary | unchanged |
| 20 | 15 | Closing | unchanged |

## Detail per Slide

### Slide 2 — Definisi HC Portal (REWRITE)

**Heading:** "Apa itu HC Portal KPB?"

**Definisi (versi A):**
> Sistem informasi berbasis web Tim Human Capital Kilang Pertamina Balikpapan untuk **MENGELOLA · MENGEMBANGKAN · MENDAMPINGI** kompetensi pekerja lewat tiga platform terpadu: **CMP** (Assessment), **CDP** (Coaching & Development), **BP** (Business Partner — For Future).

**3 card di bawah (pengganti Assessment/Coaching/Sertifikasi):**

| Icon | Title | Sub-text |
|---|---|---|
| 🎯 | Terpusat | Satu portal untuk seluruh proses kompetensi & pengembangan |
| 📐 | Terstandar | Kriteria, deliverable, sertifikasi mengacu standard KPB |
| 📊 | Terukur | Skor, progress, level kompetensi tertrace per pekerja |

### Slide 3 — BigMenu CMP (NEW)

**Header:** `BIG MENU 1/3` + "CMP — Competency Management Platform"

**Definisi:** "Platform untuk **mengukur & memvalidasi** kompetensi pekerja melalui assessment & sertifikasi."

**Grid sub-modul (4 card):**
- 📝 Assessment OJT — ujian online per unit operasi
- 🎓 Assessment Proton — program 3-tahun (Th 1-2 online, Th 3 interview offline)
- 🔄 Pre/Post Test — ukur effectiveness training (gain score)
- 🏆 Sertifikasi — otomatis berdasar passing grade + renewal lifecycle

**Footer role akses:** Admin · HC · Coachee

### Slide 4 — BigMenu CDP (NEW)

**Header:** `BIG MENU 2/3` + "CDP — Competency Development Platform"

**Definisi:** "Platform untuk **mengembangkan** kompetensi melalui coaching, development plan, & training records."

**Grid sub-modul (3 card):**
- 🎯 Coaching Proton — silabus + deliverable + review multi-role (Th 1-3)
- 📋 IDP — Individual Development Plan, target tahunan per pekerja
- 📚 Training Records — riwayat training internal/eksternal + sertifikat upload

**Footer role akses:** HC · Coach · SrSpv · SH · Coachee

### Slide 5 — BigMenu BP (NEW)

**Header:** `BIG MENU 3/3` + "BP — Business Partner" + badge **🚧 COMING SOON**

**Style:** Visual muted (opacity 70%, grayscale icon) untuk indikasi "in roadmap".

**Definisi:** "Modul **HRBP** (Human Resources Business Partner) — strategic partner antara HC & unit operasional untuk workforce planning, employee relations, & advisory."

**Grid placeholder sub-modul (3 card, semua bertanda 🚧):**
- 🤝 Workforce Planning — perencanaan SDM unit
- 👁️ Employee Relations — manajemen hubungan pekerja
- 💡 Strategic Advisory — konsultasi HC untuk leadership unit

**Footer:** "Status: In Roadmap — definisi & implementasi menyusul."

### Slide 6 — Role (piramida 6 tier, REWRITE)

**Layout:** Flexbox vertikal, 6 row, label level di kiri.

```
L1 ┃        🛡️ Admin                                     ┃
L2 ┃        👥 HC                                         ┃
L3 ┃    [Direktur]  [VP]  [Manager]                       ┃
L4 ┃        [Section Head]  [Sr Supervisor]               ┃
L5 ┃           [Coach]  [Supervisor]                      ┃
L6 ┃                👨‍🎓 Coachee                            ┃
```

**Styling:** Gradient background per tier (navy → green dari atas ke bawah). Tier 3-5 = multi-chip horizontal centered. Tooltip per chip untuk deskripsi akses singkat.

**Source authority:** `Models/UserRoles.cs:8-29` (level 1-6, AllRoles).

### Slide 8 — Pre/Post Test (NEW)

**Header:** `BAGIAN 1 · CMP` + "Pre & Post Test — Ukur Efektivitas Training"

**Layout flow horizontal 4 step:**

`[1 📋 Pre Test]` → `[2 🎓 Training/OJT]` → `[3 ✅ Post Test]` → `[4 📊 Gain Score]`

**Highlight box:** Item Analysis + Gain Score Reporting (v14.0 feature).

**Source authority:** `Controllers/CMPController.cs` (PrePost methods).

### Slide 9 — Alur Assessment OJT (FIX, 7-step + arrow)

**Heading:** "Alur Assessment OJT" (rename, agar tidak ambigu dengan Proton).

**Step revisi (8 → 7, merge step 3 lama "Distribusi Soal" ke step 4):**

```
Row 1 (4 card):
[1 📁 Persiapan Data] → [2 📝 Buat Assessment] → [3 💻 Peserta Ujian*] → [4 👁️ Monitoring]
                                                                              ↓
Row 2 (3 card):
[5 📤 Submit Ujian] → [6 ⚙️ Penilaian Otomatis] → [7 🏆 Hasil & Laporan]
```

`*Step 3` sub-text: "Login portal, **sistem random soal otomatis**, kerjakan timer".

**Arrow CSS:** `.flow-arrow` Unicode `→` (horizontal) + `↓` (between rows). Match style slide 11/12.

### Slide 14 — IDP & Training Records (NEW)

**Header:** `BAGIAN 3 · CDP` + "IDP & Training Records"

**Layout 2 kolom:**

| IDP | Training Records |
|---|---|
| 📋 Individual Development Plan | 📚 Riwayat training |
| Target tahunan per pekerja | Internal & eksternal |
| Status approval bertahap | Kategori & sub-kategori |
| Tracking pencapaian | Sertifikat upload + validity |

**Footer:** "Akses: HC kelola → Coachee submit → Atasan review".

**Source authority:** `Views/CDP/Index.cshtml:25` (IDP), v15.0 phase 286 (Training Records hub di Kelola Data).

### Slide 17 — Alur Coaching Th 1-2 (FIX)

**Step 5 revisi:** "Review HC" → **"Review Multi-Role Paralel"**

**Sub-text step 5:** "Coach, SrSpv, SectionHead, HC — review independent per-role (Phase 65)".

**Arrow:** tambah `→` antar card di tiap row + `↓` antar row (sudah ada di v1, perlu visual lebih tegas).

**Source authority:** `Models/ProtonModels.cs:125-138` (`SrSpvApprovalStatus`, `ShApprovalStatus`, `HCApprovalStatus` — independent per role, bukan cascade).

### Slide 18 — Alur Coaching Th 3 (FIX)

**Step 5 revisi:** "Interview Online" → **"Coaching Intensif"**

**Sub-text step 5:** "Sesi coaching mendalam per deliverable, dicatat di CoachingSession (log per session)".

**Arrow:** tambah `→` + `↓` snake-flow (sama pattern slide 17).

**Source authority:** `Models/CoachingSession.cs:1-26` — generic session model, no online/offline flag, beda dari Assessment Proton Th 3 yang interview offline.

## File Yang Disentuh

- `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — rewrite section slide 2/3, tambah section slide 3-5/8/14, fix section slide 9/17/18, update Alpine `current` total state max value (15 → 20), update navigation slide indicator.
- `sosialisasi-portalhc/sosialisasi-v2/assets/` — kemungkinan tambah icon SVG untuk BigMenu / Pre/Post / IDP. Cek dulu folder, kalau pakai emoji unicode → skip.

## Verification (Test Plan)

1. **Visual check** — buka `sosialisasi-v2.html` di browser, klik through 20 slide. Verifikasi:
   - Slide counter `n/20` benar
   - Tombol Next/Prev navigasi sampai slide 20
   - Tombol Slide jumper (kalau ada) include slide baru
2. **Slide 6 piramida** — semua 10 role dari `Models/UserRoles.cs:38-42` tampil (Admin, HC, Direktur, VP, Manager, SectionHead, SrSupervisor, Coach, Supervisor, Coachee).
3. **Arrow alur** — slide 9/17/18 punya `→` antar card horizontal + `↓` antar row.
4. **Audit konten**:
   - Slide 9: 7 step (bukan 8), "Distribusi Soal" sudah merge ke step "Peserta Ujian".
   - Slide 17: step 5 = "Review Multi-Role Paralel" (bukan "Review HC" saja).
   - Slide 18: step 5 = "Coaching Intensif" (bukan "Interview Online").
5. **Cross-platform** — uji print preview / landscape orientation tetap rapi.
6. **Tag git** — setelah verify, tag `sosialisasi-v2.0` (atau `sosialisasi-v1.1` kalau treat revisi sebagai minor).

## Out of Scope

- Refactor Alpine.js state management (keep current structure).
- Tema dark mode penyesuaian baru (existing dark style tetap apply).
- Mobile-specific layout optimization (slide deck dipakai presentation desktop/projector).
- Translation EN (bahasa Indonesia only per CLAUDE.md).
- BP module backend implementation (slide hanya teaser "Coming Soon").
