# Tutorial Workflow Claude — Design Spec

**Tanggal:** 2026-04-28
**File output:** `docs/tutorial-workflow-claude.html`
**Audience:** Rekan developer PortalHC KPB yang sudah pakai Claude Code, belum kenal GSD/Superpower.
**Tujuan:** Rekan bisa pilih flow yang tepat (GSD vs Superpower) untuk tiap task, tahu langkah verifikasi via Playwright MCP, dan tahu format handoff ke IT.

---

## 1. Goals & Non-Goals

### Goals
- Satu file HTML standalone yang bisa di-export ke PDF (print-friendly, A4)
- Mengikuti gaya visual dokumen `docs/` yang sudah ada
- Menjelaskan 6 step alur kerja: trigger → tentukan kompleksitas → flow GSD/Superpower → verifikasi Playwright → commit/push → handoff IT
- Cukup ringkas (max ~5 halaman A4 cetak) supaya bisa dibaca rekan dalam ≤15 menit

### Non-Goals
- Tutorial install Claude Code dari nol (audience sudah pakai)
- Tutorial git dasar atau dotnet build (audience sudah developer)
- Duplikat penuh `checklist-deploy.html` (Step 6 hanya ringkasan + rujukan)
- Interaktif/JavaScript-heavy (cukup HTML+CSS statis)

---

## 2. Visual & Layout (Hybrid)

Mengambil struktur dari `docs/checklist-deploy.html` dan elemen step-num bulat dari `docs/ActiveDirectory-Guide.html`.

### Page Setup
- `@page { margin: 2cm; size: A4; }`
- `body { max-width: 720px; margin: 40px auto; font-family: 'Segoe UI', Tahoma, sans-serif; line-height: 1.6; color: #222; font-size: 11pt; }`
- `@media print { body { margin: 0; } h2 { break-after: avoid; } .step-card { break-inside: avoid; } }`

### Color System
- **Header utama (h1):** merah Pertamina `#c62828` dengan border-bottom 3px
- **Section heading (h2):** background biru `#1a56db`, text putih, padding 8px 14px, border-radius 4px (dari checklist-deploy)
- **Sub heading (h3):** biru `#1a56db`, no background
- **Accent text:** hijau `#1b5e20` (untuk text "GSD") dan oranye `#e65100` (untuk text "Superpower"). Background badge memakai pastel sesuai tabel di section komponen (`#c8e6c9` hijau & `#ffe0b2` oranye)

### Komponen Reusable
| Class | Tujuan | Style |
|---|---|---|
| `.step-num` | Lingkaran bertanda nomor step | bg `#1a56db`, white, 28px diameter, border-radius 50% |
| `.checklist` | List dengan kotak ☐ | `::before` content "\2610" |
| `.command-box` | Block command terminal | bg `#1e293b`, color `#e2e8f0`, monospace |
| `.code-inline` (`<code>`) | Kode inline | bg `#f3f4f6`, padding 2px 6px |
| `.info` | Box info biru | border-left 4px `#1a56db`, bg `#e3f2fd` |
| `.note` | Box catatan kuning | border-left 4px `#f59e0b`, bg `#fef3c7` |
| `.danger` | Box peringatan merah | border-left 4px `#dc2626`, bg `#fee2e2` |
| `.flow-diagram` | Box diagram alur (mono) | bg `#fafafa`, border `#e0e0e0`, monospace, white-space: pre |
| `.badge-gsd` | Label hijau | bg `#c8e6c9`, color `#1b5e20` |
| `.badge-sp` | Label oranye | bg `#ffe0b2`, color `#e65100` |
| `.footer` | Footer dokumen | text-align center, color `#999`, font-size 11px |

---

## 3. Konten — 6 Step

### Header
- `<h1>` "Alur Kerja Coding dengan Claude Code"
- `<p class="subtitle">` "PortalHC KPB — Panduan untuk Rekan Developer | April 2026"
- 1 paragraf intro pendek (3-4 kalimat): "Dokumen ini menjelaskan alur dari temuan audit / fitur baru sampai deploy ke server dev. Ada 2 mode utama: GSD untuk task kompleks, Superpower untuk task ringan. Pilih mode yang tepat di awal supaya tidak overhead."

### Step 1 — Trigger: Audit Findings atau Fitur Baru
- 1 paragraf pembuka: kapan siklus dimulai
- 2 trigger utama:
  - Temuan audit milestone (lihat `.planning/milestones/v{X}-MILESTONE-AUDIT.md`)
  - Request fitur/perbaikan dari HC/user
- Sub-checklist "Siapkan info sebelum prompt Claude":
  - Deskripsi masalah / kebutuhan (1-2 kalimat)
  - File terkait (kalau sudah tahu)
  - Screenshot / contoh output (untuk bug UI)
  - Skenario reproduksi (untuk bug)

### Step 2 — Tentukan Kompleksitas
- Heading "Pakai mode mana?" — diikuti dua box berdampingan:
  - **`<span class="badge-gsd">GSD</span>`** kalau:
    - Menyentuh ≥3 file
    - Ada migrasi DB
    - Lintas role / lintas halaman
    - Butuh perencanaan multi-step / multi-phase
  - **`<span class="badge-sp">Superpower</span>`** kalau:
    - Ubah 1–2 file
    - Tweak UI satu halaman
    - Bug fix terlokalisasi
    - Estimasi <30 menit
- Box `.info`: "Kalau ragu, mulai dari Superpower — bisa di-promote ke GSD nanti pakai `/gsd-import` kalau scope membesar."

### Step 3 — Flow GSD vs Superpower
Dua sub-section stacked vertikal (3a lalu 3b) — bukan side-by-side, supaya tetap rapi di lebar 720px dan saat dicetak A4:

#### 3a. Flow GSD
`<div class="flow-diagram">` berisi alur ASCII:
```
/gsd-discuss-phase   → kumpulkan konteks, tanya jawab
        ↓ checkpoint: review jawaban
/gsd-plan-phase      → buat PLAN.md
        ↓ checkpoint: review plan
/gsd-execute-phase   → eksekusi (atomic commits)
        ↓
/gsd-verify-work     → UAT & verifikasi goal
```
Catatan kecil di bawah:
- Artefak disimpan di `.planning/phases/<NN-nama-phase>/`
- Checkpoint = Claude berhenti minta konfirmasi sebelum lanjut
- Untuk task lebih cepat, ada `/gsd-quick` (skip optional agents)

#### 3b. Flow Superpower
`<div class="flow-diagram">` berisi alur:
```
brainstorming               → gali tujuan & desain
        ↓ design approval
writing-plans               → tulis plan implementasi
        ↓ plan review
executing-plans             → eksekusi step-by-step
        ↓
verification-before-completion → verifikasi sebelum claim selesai
```
Catatan kecil di bawah:
- Spec disimpan di `docs/superpowers/specs/YYYY-MM-DD-<topik>-design.md`
- Plan disimpan di `docs/superpowers/plans/`
- Skill diaktifkan via Skill tool (Claude memanggil otomatis), atau ketik nama skill di prompt

### Step 4 — Verifikasi via Playwright MCP (localhost)
- 1 paragraf: Claude Code yang Anda pakai sudah punya plugin Playwright MCP terpasang. Claude bisa buka browser otomatis ke localhost, klik UI, ambil screenshot/snapshot, dan baca console error.
- Prasyarat (sub-checklist):
  - Plugin Playwright MCP aktif di Claude Code (`mcp__plugin_playwright_playwright__*` tersedia)
  - Dev server jalan: `dotnet run` di terminal terpisah
  - Aplikasi accessible di `https://localhost:5001` (Kestrel default)
  - Login akun test sudah disiapkan
- Contoh prompt template (`.command-box`) — ganti placeholder `<...>` dengan kredensial test Anda:
  ```
  Pakai Playwright, buka https://localhost:5001, login sebagai HC
  (email: <email-test>, password: <password-test>),
  buat assessment baru, assign 1 worker, dan ambil screenshot tiap step.
  Laporkan kalau ada error di console.
  ```
- Box `.note`: "Dev server harus sudah jalan duluan. Kalau belum, suruh Claude jalankan `dotnet run` lewat background bash dulu sebelum verifikasi."

### Step 5 — Commit & Push
- Konvensi prefix commit (dari git log proyek):
  - `feat:` — fitur baru
  - `fix:` — bug fix
  - `chore:` — maintenance, cleanup, config
  - `docs:` — dokumentasi
  - `refactor:` — restrukturisasi tanpa ubah behavior
  - `test:` — penambahan/perubahan test
- Alur perintah (`.command-box`):
  ```
  git status              # cek file yang berubah
  git add -p              # review per-hunk
  git commit -m "fix: <deskripsi singkat>"
  git push origin main
  ```
- Box `.danger`: "Jangan commit:
  - File `.env`, `appsettings.Development.json` dengan secret
  - File database `HcPortal.db` development
  - File besar binary (>10MB) — pakai `.gitignore`"
- Catatan kecil: Claude bisa bantu draft commit message — minta `tolong buatkan commit message untuk perubahan ini`.

### Step 6 — Handoff ke IT
Bukan duplikat penuh `checklist-deploy.html` — fokus pada **info yang developer kirim ke IT**.

#### 6a. Info yang Harus Dikirim
Tabel:
| Item | Penjelasan |
|---|---|
| Branch / commit hash | Hash commit terakhir yang siap deploy (`git rev-parse HEAD`) |
| Migrasi DB baru | Daftar file migrasi di `Migrations/` yang belum ada di server, atau "tidak ada" |
| Perubahan `appsettings` | Apakah ada key baru di `appsettings.json` yang perlu diisi di `appsettings.Production.json` |
| File/folder baru | Folder upload, folder static, dll yang perlu permission write |
| UAT yang sudah dilakukan | Daftar flow yang sudah diverifikasi (Login, Assessment, Coaching, dll) |
| Catatan khusus | Bug bekas, cleanup data yang perlu dijalankan, dll |

#### 6b. Langkah IT (ringkas)
- IT pull dari GitHub: `git pull origin main`
- IT jalankan migrasi: `dotnet ef database update` (kalau ada migrasi baru)
- IT build & publish: `dotnet publish -c Release -o ./publish`
- Detail lengkap (security check, post-deploy verification, dll) ada di `docs/checklist-deploy.html`

Box `.info`: "Untuk checklist deploy lengkap (server config, security, post-deploy verification), lihat `docs/checklist-deploy.html`. Dokumen ini hanya menjelaskan alur sampai ke IT, bukan langkah teknis IT-nya."

### Footer
- `<div class="footer">` — "Dokumen ini dibuat 28 April 2026 — PortalHC KPB | Untuk pertanyaan, lihat `.planning/PROJECT.md` dan `.planning/MILESTONES.md`"

---

## 4. Struktur File

Single file HTML standalone:
- `docs/tutorial-workflow-claude.html`
- Self-contained CSS di `<style>` (tidak ada external stylesheet)
- Tidak ada JavaScript
- Tidak ada gambar eksternal — semua diagram pakai `<pre>` ASCII art atau Unicode arrows

---

## 5. Acceptance Criteria

1. File `docs/tutorial-workflow-claude.html` ada dan bisa dibuka di browser
2. Print preview di Chrome/Edge menampilkan max 5 halaman A4 dengan margin rapi
3. Export ke PDF (Save as PDF dari browser) menghasilkan PDF tanpa elemen terpotong di tengah step
4. Semua 6 step ada dengan struktur sesuai section 3 di atas
5. Konten tetap akurat referensi:
   - Slash command GSD yang disebut benar-benar tersedia (`/gsd-discuss-phase`, `/gsd-plan-phase`, `/gsd-execute-phase`, `/gsd-verify-work`, `/gsd-quick`)
   - Skill Superpower yang disebut benar-benar tersedia (`brainstorming`, `writing-plans`, `executing-plans`, `verification-before-completion`)
   - Path artefak benar (`.planning/phases/`, `docs/superpowers/specs/`, `docs/superpowers/plans/`)
   - Path deploy reference benar (`docs/checklist-deploy.html`)
6. Tidak ada lorem ipsum / placeholder text yang tertinggal
7. Konsisten Bahasa Indonesia (sesuai `CLAUDE.md`)

---

## 6. Out of Scope (eksplisit)

- Tidak menjelaskan internal mekanisme GSD/Superpower (cukup what & when, bukan how)
- Tidak menjelaskan API Claude Code, MCP protocol, atau plugin development
- Tidak ada section troubleshooting Claude Code (bukan support doc)
- Tidak ada SOP review code antar developer — dokumen ini fokus alur Claude→commit→IT, bukan code review human-to-human

---

## 7. Risiko / Catatan

- **Drift risiko:** kalau slash command GSD berubah nama (mis. update GSD plugin), section 3a perlu update. Mitigasi: catatan di footer "Versi GSD yang dirujuk: v1 dengan struktur `.planning/`".
- **Bahasa skill:** Superpower skill names dipakai apa adanya (English) karena memang itu nama tool — tapi penjelasan tetap Bahasa Indonesia.
- **Kontak IT:** sengaja tidak hardcode nama orang IT supaya dokumen tidak basi kalau personil berganti.
