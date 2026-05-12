# Sosialisasi PortalHC KPB — Interactive 4-File Design

**Status:** Design draft (brainstorm 2026-05-12)
**Deliverable:** Hub + 3 file HTML interaktif (Sosialisasi, Panduan, Praktik)

## 1. Konteks

PT Pertamina KPB butuh sosialisasi aplikasi PortalHC ke Tim HC operasional (~15-25 orang). Asset existing (15 slide HTML overview + 9 panduan HTML terpisah) tidak match vision karena terbelah dan tidak interaktif. Output baru = **4 file HTML self-contained** dengan interactivity, dapat digunakan saat acara presenter-led + post-acara self-paced.

## 2. Deliverable

4 file di-bundle dalam **offline zip** untuk distribusi (email/shared drive/USB).

### 2.1 `index.html` — Hub Landing
Entry point peserta. **Hero section** (judul + intro 1-2 baris) + **3 card navigasi**:
- Card 1 — Sosialisasi (overview portal)
- Card 2 — Panduan (tutorial step-by-step)
- Card 3 — Praktik (mockup drill interactive)

Klik card → buka file dedicated. Cross-link inter-file (Panduan → Praktik anchor relevan).

### 2.2 `sosialisasi.html` — Slide Overview
15 slide mirror struktur existing `Sosialisasi-Aplikasi-PortalHC-KPB.html`, tech rebuild Alpine.js + Tailwind:

| # | Slide | Konten |
|---|---|---|
| 1 | Cover | HC Portal KPB |
| 2 | Agenda | Daftar isi presentasi |
| 3 | Latar Belakang | Problem (Excel scatter, audit susah) |
| 4 | Apa Itu HC Portal | Solution overview |
| 5 | 3 Pilar Manfaat | Unified, traceable, scalable |
| 6 | Struktur Role | 6 level akses (10 role total — Admin/HC/Direktur/VP/Manager/SectionHead/SrSupervisor/Coach/Supervisor/Coachee) |
| 7 | Modul CMP | Competency Management |
| 8 | 6 Kategori Assessment | Lingkup assessment |
| 9 | Modul CDP | Continuous Development |
| 10 | Coaching Proton Journey | Tahun 1 → 3 lifecycle |
| 11 | Dashboard Analytics | Insight + report |
| 12 | Integrasi & Keamanan | Login form (email + password) + role-based access + audit log. **Catatan refresh:** existing slide klaim "LDAP/AD SSO" — codebase `UseActiveDirectory: false`, AD belum diaktifkan. Drop klaim AD atau tambah label "siap diaktifkan IT, belum live" |
| 13 | Progress Penyiapan | Refresh konten terbaru (drop expired April 2026) |
| 14 | Cara Mengakses | URL Dev `http://10.55.3.3/KPB-PortalHC` + URL Prod (TBD per IT) + Login form email/password. **Catatan refresh:** existing slide klaim "Akun Active Directory Pertamina" — INAKURAT, fix ke "Email + password" sesuai state saat ini |
| 15 | Penutup | Next step + handoff Panduan + Praktik |

**Refresh konten WAJIB (bukan port as-is):**
- Slide 6 — clarify "6 level akses (10 role)", existing subtitle akurat
- Slide 8 — 6 kategori (OJ, IHT, Licencor, OTS, HSSE, Proton) = business reference, code dynamic. Flag bahwa admin bisa tambah kategori
- Slide 12 — **drop klaim AD/LDAP**, ganti "Login form + role-based + audit log"
- Slide 13 — drop "Progress April 2026" expired, replace "Status Mei 2026" atau remove slide
- Slide 14 — fix "Login: Email + password" (bukan AD), update URL Dev + Prod

Refactor styling vanilla CSS → Tailwind. Refactor logic Chart.js + custom JS → Alpine.js.

### 2.3 `panduan.html` — Tutorial Guided
**22 use case** dengan scaffolding **2 tier**:

**Quick Start (top, = Daily Ops cluster)** — 4 fitur daily cross-cluster:
- #1 Login form
- #2 Navigasi dashboard
- #3 Buka panduan icon ?
- #6 Notifikasi bell

**Per Cluster (body)**:

| Cluster | Items |
|---|---|
| Master Data | 8, 9, 13 |
| CMP Assessment | 19, 20, 21, 22, 23, 25, 26, 29, 31 |
| CDP Coaching | 32, 33 |
| Analytics & Laporan | 36, 40, 43, 44 |

**Format per item**: numbered step list + 1-3 screenshot annotated (PNG + SVG overlay) + 1 tip/gotcha box. Persona ringan (role saja, no nama fiksi) untuk konteks.

**Navigasi cluster**: tab switcher di top Panduan (Quick Start / Master Data / CMP Assessment / CDP Coaching / Analytics) — klik tab → konten section ganti tanpa pindah halaman (Alpine.js `x-show`).

**Quiz interlude — 5 quiz** concept-check di transisi cluster: multi-choice + reveal jawaban + alasan singkat. Distribusi: 1 quiz per cluster (Quick Start, Master Data, CMP, CDP, Analytics).

### 2.4 `praktik.html` — Mockup Interactive
**8 workflow drill simulasi** (form fake, validation fake, feedback fake, no portal connect):

| # | Workflow | Rasional drill |
|---|---|---|
| 1 | Login form | Universal daily |
| 8 | Worker CRUD | Deactivate vs delete confusion |
| 19 | ManagePackageQuestions CRUD | Multi-field + type selector |
| 22 | CreateAssessment single | Workflow paling complex (signature drill) |
| 23 | CreateAssessment Pre-Post | Concept tricky (linked group) |
| 26 | Manual grading | Konsistensi penilaian |
| 32 | CoachingProton | Multi-state approval flow |
| 33 | PlanIdp | Competency mapping concept |

**State behavior**:
- `localStorage` preserve state per workflow (refresh-safe untuk workflow panjang)
- Button "Drill Ulang" explicit reset per workflow
- Validation feedback: success checkmark / error message kontekstual
- localStorage key dengan schema version: `mockup_v1_<workflow>_state` (mis. `mockup_v1_login_state`)

**Catatan #1 Login mockup**: simulasi login form email + password sesuai state saat ini. Kalau IT enable AD/LDAP di masa depan, mockup wajib di-update (versi v2). Lock di tracker maintenance.

**Cross-link**: setiap mockup punya tombol "Kembali ke Panduan" → buka `panduan.html` anchor relevan.

## 3. Scope Konten (22 item dari 47 use case)

Dari 47 use case mapping codebase, **22 item dipilih** sebagai prioritas tinggi (daily/weekly tasks HC ops). 25 use case sisa (monthly/quarterly/specialist) **tidak dibahas di acara** — sebut singkat di Panduan footer dengan link panduan terpisah `wwwroot/documents/guides/`.

Cluster breakdown lengkap:
- **Daily Ops (4)**: 1, 2, 3, 6
- **Master Data (3)**: 8 Worker CRUD, 9 Worker Import, 13 Coach Mapping
- **CMP Assessment (9)**: 19 Question CRUD, 20 Question Import, 21 Manage tabs, 22 Create single, 23 Create Pre-Post, 25 Monitoring, 26 Manual grading, 29 Results, 31 Certificate
- **CDP Coaching (2)**: 32 Coaching Proton, 33 Plan IDP
- **Analytics (4)**: 36 Dashboard, 40 ExpiringSoon, 43 Certification, 44 Export results

## 4. Tech Stack

- **HTML5 + Tailwind CSS** utility framework (bundled `assets/vendor/tailwind.min.css`)
- **Alpine.js** state mgmt + reveal step-by-step (bundled `assets/vendor/alpinejs.min.js`)
- **Alpine `$persist` plugin** untuk localStorage di mockup state (bundled `assets/vendor/alpine-persist.min.js`)
- **Script loading order** (wajib): Alpine `$persist` plugin DULU (`defer`), lalu Alpine core (`defer`). Plugin register sebelum Alpine init.
- **No CDN runtime** — semua vendor di-bundle dalam zip
- **Browser target**: Chrome/Edge desktop modern (no IE11, no mobile responsive)
- **Bundle script tool**: PowerShell `.ps1` (Windows env) — `Compress-Archive` cmdlet untuk zip. Atau manual zip kalau preferensi GUI

## 5. Animasi Standard

- Slide nav: fade-out → fade-in (300ms)
- Reveal step-by-step di slide (klik next atau key `space`)
- Hover state card hub: lift + shadow
- Scroll-trigger fade-in di Panduan section
- Mockup feedback: success checkmark slide-in, error shake
- Loading state file picker mockup (Excel import simulation)

## 6. Persona Usage (Ringan)

Role saja, **tanpa nama fiksi**:
- "Admin master data"
- "Asesor / koordinator assessment"
- "Coach senior"
- "Manager HC / reporter"
- "Auditor internal"
- "Pemain baru / HC staff"

Konteks role muncul di:
- Slide: "Section ini relevan untuk Admin master data"
- Panduan: "Tutorial — biasa dilakukan oleh Asesor"
- Mockup: data dummy pakai role-based naming ("Asesor 1", "Coach 1", "Worker 001-010")

## 7. Distribusi Offline Zip

Struktur folder zip:

```
sosialisasi-portalhc-v1.zip/
├── index.html
├── sosialisasi.html
├── panduan.html
├── praktik.html
└── assets/
    ├── vendor/
    │   ├── alpinejs.min.js
    │   ├── alpine-persist.min.js
    │   └── tailwind.min.css
    └── screenshots/
        ├── cluster-master-data/
        ├── cluster-cmp/
        ├── cluster-cdp/
        └── cluster-analytics/
```

Peserta extract zip → buka `index.html` di Chrome/Edge. Semua relative path, no server dependency.

**Reference link 9 panduan existing**: Panduan footer link ke `wwwroot/documents/guides/*.html` pakai URL absolut Dev `http://10.55.3.3/KPB-PortalHC/documents/guides/...` (saat acara via LAN). Untuk offline post-acara, panduan terpisah tidak accessible — accept tradeoff (peserta yang butuh deep dive akses portal real).

## 8. Effort & Timeline

Estimasi developer 1 orang full-time:

| Komponen | Effort |
|---|---|
| Hub landing | ~1 hari |
| Slide overview (15 slide rebuild) | ~5-7 hari |
| Panduan (22 item: screenshot capture + annotation + bullet + 5 quiz) | ~5-7 hari |
| Mockup (8 workflow: form + state + validation + reset) | ~7-10 hari |
| Cross-link inter-file + zip bundle script | ~1 hari |
| QA + cross-browser + polish | ~2-3 hari |
| **Total** | **~21-29 hari kerja** |

Hari ini 12 Mei 2026. Target acara sebelumnya 17-24 Mei = **9 hari kerja remaining**. **Tidak realistic** untuk full scope solo.

**Default assumption** (perlu konfirmasi user):
- **Deadline target: 5 Juni 2026** (~17 hari kerja, realistic full scope)
- **Fallback urutan cut kalau slip**: (1) animasi polish, (2) mockup 8 → 6, (3) quiz 5 → 3, (4) screenshot annotation depth

## 9. Risiko

| # | Risiko | Mitigasi |
|---|---|---|
| 1 | Scope creep — 22 item × 2 file mode | Cut prioritas urutan di section 8 |
| 2 | Codebase drift — screenshot/mockup outdated kalau portal UI berubah | Versioning file (v1, v2). Checklist maintenance pasca milestone portal |
| 3 | Distribusi friction — peserta zip extract dependency | Instruksi 1-baris di hub README |
| 4 | localStorage edge case — version mismatch saat mockup update | Schema versioning di localStorage key (`mockup_v1_login_state`) |
| 5 | Browser quirk — Tailwind purge atau Alpine.js compatibility | Test Chrome + Edge sebelum release |

## 10. Open Gaps + Locked Defaults

Status post-brainstorm 2026-05-12:

| Gap | Lock |
|---|---|
| Deadline spesifik | **Flexible** (user: "gampang nanti") — full scope tanpa pressure waktu. Best-effort, no hard cutoff. |
| Maintenance owner | **Self** (user yang implement = owner update file kalau portal UI berubah) |
| Fallback cut prioritas | Order kalau perlu (saat ini tidak block): (1) animasi (2) mockup count (3) quiz count (4) screenshot depth |
| Slide 13 (Progress) konten refresh | Drop slide expired April 2026, replace dengan "Status Mei 2026" atau remove |
| Slide 14 (Akses) URL update | URL Dev `http://10.55.3.3/KPB-PortalHC` + URL Prod (TBD per IT) |
| Mockup #25 AssessmentMonitoring live | **Tidak masuk mockup** (real-time dynamic data — pakai screenshot + screencast 30-detik di Panduan) |

## 11. Definition of Done (DoD) per File

**`index.html` (Hub)** — done kalau:
- Hero section render dengan judul + intro
- 3 card click → navigate ke file dedicated (test di Chrome + Edge)
- Hover state card lift + shadow
- Mobile viewport graceful degradation (acceptable, no broken layout — meskipun tidak mobile-first)

**`sosialisasi.html` (Slide)** — done kalau:
- 15 slide render dengan struktur match table section 2.2
- Slide 6/12/14 konten **REFRESH** (bukan port as-is) — verify against accuracy notes
- Navigation next/prev keyboard + button
- Reveal step-by-step working
- Transition fade 300ms
- Dark mode toggle preserved kalau ada di existing

**`panduan.html` (Tutorial)** — done kalau:
- 22 item ter-render lengkap (4 Quick Start + 18 cluster body)
- Setiap item: numbered step + 1-3 screenshot + tip box
- Tab switcher cluster nav working (5 tab)
- 5 quiz concept-check working (1 per cluster) — reveal jawaban
- Cross-link ke `praktik.html` anchor (untuk item yang punya mockup)
- Footer link ke 9 panduan existing

**`praktik.html` (Mockup)** — done kalau:
- 8 workflow drill render lengkap (1, 8, 19, 22, 23, 26, 32, 33)
- State persist via `$persist` (refresh → state preserve)
- "Drill Ulang" button reset per workflow
- Validation feedback (success / error) working
- Cross-link "Kembali ke Panduan" working
- localStorage key follow schema `mockup_v1_<workflow>_state`

**Bundle zip** — done kalau:
- Folder structure match section 7
- Semua vendor (Alpine, Tailwind, persist plugin) bundled
- Screenshot folder lengkap (PNG + SVG annotation)
- README singkat (instruksi extract + buka `index.html`)
- File zip max 50MB (kalau lebih, optimize screenshot)
- Test extract di laptop Windows fresh — buka index.html, semua link working

## 12. Versioning Strategy

- **Format versi**: `v<major>.<minor>` (mis. `v1.0`, `v1.1`, `v2.0`)
- **Major bump**: breaking change di mockup state schema, atau full content rewrite
- **Minor bump**: konten refresh, screenshot update, bug fix, polish
- **Filename**: `sosialisasi-portalhc-v1.0.zip`
- **Changelog**: `CHANGELOG.md` di root zip dengan entry per versi (tanggal + perubahan)
- **localStorage compatibility**: schema version embedded di key (`mockup_v1_login_state`). Kalau v2 ubah schema, key beda → no conflict dengan state v1 lama

## 13. Next Step

1. **User review spec doc ini** — feedback adjust gap atau lock default
2. **Transition ke writing-plans** — buat implementation plan tasked per file
3. **Execute MVP** — Hub landing first sebagai POC, validate stack + arsitektur
4. **Expand iteratif** — Slide → Panduan → Mockup parallel atau sequential per developer capacity
5. **Acara hari-H** — test rehearsal H-3, deploy zip H-1, distribusi peserta H-1 atau saat acara

## Appendix — Brainstorm Decision Log (19 lock)

| # | Aspek | Lock | Q ref |
|---|---|---|---|
| 1 | Komponen | Slide + Panduan + Mockup | Q2 |
| 2 | Struktur file | Hub + 3 specialized | Q3 F |
| 3 | Audiens | Tim HC ops 15-25 org | Q4 A |
| 4 | Konsumsi | Acara + self-paced | Q5 C |
| 5 | Scope konten | 22 item prioritas tinggi | Q6 B |
| 6 | Akses portal | Self-contained, no portal | Q7 B |
| 7 | Mockup subset | 8 workflow (#1, #8, #19, #22, #23, #26, #32, #33) | Q8 C |
| 8 | Tech stack | Alpine.js + Tailwind | Q9 D |
| 9 | Slide rebuild | Tech refresh, struktur mirror 15 slide | Q10 C + Q13 D |
| 10 | Persona | Ringan (role saja) | Q11 E |
| 11 | Panduan struktur | Quick Start + per cluster | Q12 F |
| 12 | Hub design | Hero + 3 card | Q14 B |
| 13 | Animasi | Standard | Q15 B |
| 14 | Quiz | Concept-check Panduan + mockup feedback built-in | Q16 B+D |
| 15 | Distribusi | Offline zip | Q17 |
| 16 | Browser | Chrome/Edge desktop modern | Q18 A |
| 17 | Screenshot | PNG + SVG overlay + bundle folder | Q19 D |
| 18 | Mockup state | localStorage + Reset button | Q20 E |
| 19 | Effort/timeline | ~21-29 hari, deadline default 5 Juni 2026 | Q21 + section 8 |
