# Spec: Cleanup Repo + Arsip Hasil-Kerja — PortalHC_KPB

**Tanggal:** 2026-06-03
**Tipe:** Operasional (housekeeping repo, bukan fitur kode)
**Status:** Design — menunggu review user → writing-plans

---

## 1. Tujuan

Rapikan working tree `PortalHC_KPB` dengan 2 aksi terpisah:

1. **DELETE** — hapus junk murni (build output regenerable, screenshot throwaway, file 0-byte, orphan).
2. **MOVE** — pindah hasil-kerja yang **tidak dipakai runtime website** (deck presentasi, PCP, naskah, laporan, sample) ke folder arsip di Desktop, biar repo bersih tapi file tak hilang.

**Target ukuran:** working tree susut **~960 MB** (~431 MB delete, ~528 MB pindah). Sync OneDrive lega besar.

**Temuan dasar (terverifikasi 2 ronde workflow, 8 agent):** NOL file non-source dipakai runtime. `Program.cs:192 UseStaticFiles` cuma serve `wwwroot/`. Semua import-template di-generate in-memory via `ExcelExportHelper.ToFileResult` (`WorkerController.cs:891`, `TrainingAdminController.cs:1070`, `AssessmentAdminController.cs:5526`) — **tidak ada controller yang baca file `.xlsx`/`.docx`/`.db`/`.html` dari disk**. App pakai SQL Server (`Program.cs:28 UseSqlServer`), bukan SQLite.

---

## 2. Keputusan terkunci (dari user)

| Keputusan | Pilihan |
|---|---|
| Lokasi arsip | `Desktop\Arsip_PortalHC_HasilKerja\` (dalam OneDrive Desktop) |
| `downloads/` (147 MB export OJT) | **MOVE** ke arsip (bukan delete — ada PII peserta) |
| Build output (bin/obj ~400 MB) | **DELETE** (regenerate otomatis saat `dotnet build`) |
| 7 sample `.xlsx` | **MOVE** ke arsip |
| `docs/sertifikat-ecosystem/` | **KEEP** dulu (masih dirujuk IT_NOTIFY + spec v19.0; arsipkan nanti setelah v19.0 dipush ke IT) |
| `docs/checklist-deploy.html` | **KEEP** (link aktif IT di DEV_WORKFLOW.md:137) |
| `Database/DATABASE_MAPPING.pdf` | **MOVE** (kembar PDF dari `.md`, nol ref) |
| `docs/screenshots/phase324/` | **MOVE** (dirujuk DB_HANDOFF sebagai proof, jangan hapus) |

---

## 3. Manifest

### 3.1 ✅ KEEP — runtime / ops aktif (JANGAN sentuh)

- **Source dirs:** `Controllers/`, `Services/`, `Models/`, `Data/`, `Views/`, `wwwroot/`, `Helpers/`, `Hubs/`, `Middleware/`, `ViewComponents/`, `Migrations/`, `Properties/`
- ⚠️ **`wwwroot/uploads/`** — data user runtime (certificates, cpdp, evidence, guidance, interviews, kkj), gitignored. **MUTLAK jangan dihapus** saat operasi apa pun di wwwroot.
- **Build/config:** `appsettings*.json`, `HcPortal.csproj`, `HcPortal.sln`, `Program.cs`, `.gitignore`, `CLAUDE.md`
- **`scripts/backup-dev-pre-migration.ps1`** — dipakai SOP backup di `DEV_WORKFLOW.md:165/188`
- **`Database/`** — `*.sql` setup + `DATABASE_MAPPING.md` (kecuali `DATABASE_MAPPING.pdf` → MOVE)
- **Docs ops aktif:** `docs/DEV_WORKFLOW.md`, `docs/SEED_WORKFLOW.md`, `docs/SEED_JOURNAL.md`, `docs/IT_NOTIFY.md`, `docs/checklist-deploy.html`, `docs/templates/`, `docs/sql/`, `docs/superpowers/` (60 spec + 53 plan GSD aktif), `docs/DB_HANDOFF_IT_2026-*.html` (×3)
- **`docs/sertifikat-ecosystem/`** — KEEP sementara (defer arsip s/d v19.0 dipush)
- **Test source:** `tests/` (Playwright e2e source — bukan node_modules/test-results), `HcPortal.Tests/` (xUnit source — bukan bin/obj)
- **Tooling:** `.planning/` (GSD v21 aktif), `.claude/`, `.github/`, `.superpowers/`

### 3.2 🗑️ DELETE — junk (~431 MB)

| Item | Size | Git | Cara |
|---|---|---|---|
| 135 `*.png` di root (deck-slide/kickoff/verify/v2.2/slide/sl/bug/sertifikat-render) | ~32 MB | untracked | `Remove-Item` |
| `bin/` + `obj/` (root) | ~214 MB | untracked | `Remove-Item` (regenerate) |
| `HcPortal.Tests/bin/` + `obj/` | ~185 MB | untracked | `Remove-Item` (regenerate) |
| `HcPortal.db` + `HcPortal-pre-329-uat.db` | 0 byte | untracked | `Remove-Item` |
| `.playwright-mcp/`, `test-results/` (root), `.worktrees/` (kosong) | <1 MB | untracked | `Remove-Item` |
| `package.json` + `package-lock.json` (root) | 2 KB | **tracked** | `git rm` (orphan; Playwright asli di `tests/`) |

> `docs/abstract/*` (5 file: .docx/.html/2 .py) sudah ke-delete di working tree → ikut di-stage & commit.

### 3.3 📦 MOVE → `Desktop\Arsip_PortalHC_HasilKerja\` (~528 MB)

| Item | Size | Git | Subfolder arsip |
|---|---|---|---|
| `docs/pcp-HCPortal-2026/` (PDF 318MB+pptx gitignored, sisa tracked) | 331 MB | mixed | `pcp/` |
| `downloads/` (Post Test OJT Cilacap + .zip 67MB + xlsx — **PII**) | 147 MB | untracked | `downloads-ojt/` |
| `docs/assets/proton-video/` (5 webm + script .mjs/.html/.svg) | 19 MB | tracked | `video-proton/` |
| `docs/doc support Proton/` (silabus + coaching-guidance docx + KKJ) | 13 MB | untracked | `source-proton/` |
| `sosialisasi-portalhc/` (pptx + pdf + html + sosialisasi-v2/) | 7 MB | tracked | `presentasi/` |
| `docs/sosialisasi-screenshots/` (13 png incl proton/) | 3 MB | tracked | `screenshots/` |
| `Database/DATABASE_MAPPING.pdf` | 3.77 MB | tracked | `samples-xlsx/` |
| `docs/Sosialisasi PortalHC v2 — Slide Deck 2026.pdf` | 2.4 MB | tracked | `presentasi/` |
| `docs/Kickoff-PROTON.html` | 177 KB | tracked | `presentasi/` |
| `docs/Panduan-Operasional-HC-PortalHC-KPB.html` | 127 KB | tracked | `presentasi/` |
| `Panduan-Belajar-API-PortalHC.html` (root) | 114 KB | tracked | `presentasi/` |
| `Sosialisasi-PROTON-KPB.html` (root) | 94 KB | tracked | `presentasi/` |
| `docs/Sosialisasi-Internal-Tim-HC-*.html` (×2) | 385 KB | tracked | `presentasi/` |
| `docs/mockup-presentasi/` (coaching-proton-mockup + vendor) | <1 MB | tracked | `presentasi/` |
| `docs/Fitur PROTON — Portal HC KPB (1).pdf` | 579 KB | tracked | `presentasi/` |
| `docs/Naskah Video PROTON.docx` + `CPDP.docx` | 51 KB | tracked | `video-proton/` |
| `docs/MILESTONE-REPORT.html` | 49 KB | tracked | `reports/` |
| `docs/admin_features_analysis.html` | 70 KB | tracked | `reports/` |
| `docs/commit-log-{maret,april,mei}.html` | 77 KB | tracked | `reports/` |
| `docs/tutorial-workflow-claude.html` | 18 KB | tracked | `reports/` |
| `docs/ActiveDirectory-Guide.html` | 13 KB | tracked | `reports/` |
| `docs/Persiapan-Test-Manual-Assessment.html` | 33 KB | tracked | `reports/` |
| `docs/test 16-april/` (md/html/har tracked + screenshots/ gitignored) | 308 KB | mixed | `reports/` |
| `docs/test-reports/` (10 md, 1 gitignored) | 83 KB | mixed | `reports/` |
| `docs/screenshots/phase324/` (before/after/handoff proof png) | <1 MB | tracked | `screenshots/` |
| Root `Contoh_Soal_{Essay,Mix,MultipleAnswer}.xlsx` (×3) | 17 KB | tracked | `samples-xlsx/` |
| `docs/question_import_template_GAST.xlsx` | 24 KB | tracked | `samples-xlsx/` |
| `docs/training_import_meylisa.xlsx` | 80 KB | tracked | `samples-xlsx/` |
| `docs/workers_import_template (rev 1).xlsx` | 55 KB | tracked | `samples-xlsx/` |
| `docs/Contoh_Paket_Soal_Alkylation.xlsx` | 22 KB | tracked | `samples-xlsx/` |

---

## 4. Mekanik eksekusi

**Prinsip move robust (tracked + untracked sekaligus):** untuk tiap target, `Move-Item <path> <arsip-subfolder>` memindah SEMUA file fisik (abai status git). Setelah semua move, `git add -A` otomatis stage deletion file tracked yang hilang. Tidak perlu `git rm` per file. Ini menutup caveat folder mixed (`pcp-HCPortal-2026/`, `test 16-april/`, `test-reports/`, `sosialisasi-screenshots/proton/`).

### Urutan langkah

**Phase 0 — Pre-flight**
1. Pastikan app **tidak** sedang `dotnet run` (hindari lock `bin/`).
2. `git status` — pastikan cuma ada pending `docs/abstract/*` + kerja v19/v20/v21 lokal yang sudah ke-commit.
3. Buat branch: `git checkout -b chore/cleanup-arsip-2026-06-03`.
4. Baseline: `dotnet build` → catat **PASS** (bukti app sehat sebelum cleanup).

**Phase 1 — Scaffold arsip**
5. `New-Item -ItemType Directory` `Desktop\Arsip_PortalHC_HasilKerja\` + subfolder: `presentasi/`, `pcp/`, `video-proton/`, `source-proton/`, `screenshots/`, `reports/`, `samples-xlsx/`, `downloads-ojt/`.
6. Tulis `Arsip_PortalHC_HasilKerja\README.md` — tanggal, asal repo, commit hash saat arsip, daftar isi per subfolder.

**Phase 2 — MOVE hasil-kerja**
7. `Move-Item` tiap target §3.3 ke subfolder-nya.
8. `git add -A` → stage deletion file tracked.
9. Commit: `chore(cleanup): arsipkan hasil-kerja non-runtime ke Desktop archive`.

**Phase 3 — DELETE junk**
10. `Remove-Item` item untracked §3.2 (png, bin/obj root, Tests/bin+obj, *.db, .playwright-mcp, test-results, .worktrees).
11. `git rm package.json package-lock.json` (root orphan).
12. `git add -A` (termasuk pending `docs/abstract/*`).
13. Commit: `chore(cleanup): hapus build output + junk untracked + orphan package.json root`.

**Phase 4 — Verify (gate keselamatan)**
14. `dotnet build` → **PASS** (regenerate bin/obj — bukti hapus build output aman).
15. `dotnet run` → buka `http://localhost:5277` → smoke: home load, login, 1 halaman admin. (opsional Playwright cepat).
16. Cek manual: `wwwroot/uploads/` utuh, `Database/*.sql` + `DATABASE_MAPPING.md` ada, `scripts/` ada, `docs/{DEV_WORKFLOW,IT_NOTIFY,checklist-deploy}` ada.

**Phase 5 — Tutup**
17. `git status` bersih. `git log --oneline -3`.
18. Update `docs/SEED_JOURNAL.md`? Tidak relevan (bukan seed). Update memory project.
19. **Push = keputusan user/IT** (branch ini bisa merge ke main; banyak commit v19/v20/v21 lokal belum dipush — koordinasi IT). TIDAK auto-push.

---

## 5. Keselamatan & rollback

- **File tracked (delete/move):** recoverable penuh via git history (`git checkout <commit> -- <path>` atau `git revert`).
- **Hasil-kerja yang dipindah:** fisik ada di `Arsip_PortalHC_HasilKerja\` (tidak dihapus) — kembalikan dengan `Move-Item` balik bila perlu.
- **Junk untracked yang dihapus:** `bin/obj` regenerate via build; png/`.db` throwaway. Tidak ada nilai hilang.
- **Gate utama:** `dotnet build` + smoke run di Phase 4 membuktikan app tetap kompilasi + jalan setelah semua removal. Bila gagal → rollback branch (`git checkout main` + `Move-Item` balik dari arsip).
- **PII note:** `downloads/` berisi data peserta (sertifikat, nama, hasil test). Dipindah (bukan dihapus) ke arsip Desktop — tetap dalam OneDrive Pertamina, scope sama seperti sebelumnya. Bukan ter-expose keluar.

---

## 6. Broken-link yang diterima (cosmetic, non-runtime)

- `DB_HANDOFF_IT_2026-05-26.html` L346/564/660/664 rujuk `docs/screenshots/phase324/*` sebagai teks `<code>` (bukan `<img>`). Setelah MOVE, link teks nunjuk lokasi lama. **Diterima** — handoff lama, nol dampak render. (phase324 dipindah, bukan dihapus, jadi file masih ada di arsip.)
- Tidak ada `<img>`/`<src>` runtime yang putus (terverifikasi: 3 DB_HANDOFF self-contained inline-CSS, nol external src).

---

## 7. Out of scope / deferred

- **Arsip `docs/sertifikat-ecosystem/`** — tunda s/d batch v19.0 dipush ke IT (link `IT_NOTIFY.md:326` + spec v19.0 aktif). Phase terpisah nanti.
- **Stale SQLite string** `appsettings.json:11 "Data Source=HcPortal.db"` + dead code WAL `Program.cs:140-146` + paket `Microsoft.EntityFrameworkCore.Sqlite` di csproj — di-override SQL Server, tidak ganggu. Perbaikan opsional, **bukan** bagian cleanup ini.
- **Purge git history** (.git 115 MB) — TIDAK dilakukan (user pilih arsip, bukan rewrite history).
- **Push** — keputusan user/IT.

---

## 8. Acceptance criteria

1. `Desktop\Arsip_PortalHC_HasilKerja\` ada, berisi semua item §3.3 di subfolder benar + README.
2. Semua item §3.2 hilang dari working tree.
3. `dotnet build` PASS + `http://localhost:5277` load (home+login+1 admin page).
4. `wwwroot/uploads/`, `Database/*.sql`+`.md`, `scripts/`, docs ops (§3.1) semua utuh.
5. 2 commit cleanup atomik di branch `chore/cleanup-arsip-2026-06-03`; `git status` bersih.
6. Tidak ada file source/runtime yang terhapus (verifikasi build + smoke).
