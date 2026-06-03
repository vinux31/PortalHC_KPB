# Cleanup Repo + Arsip Hasil-Kerja Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rapikan working tree PortalHC_KPB — hapus ~431 MB junk + pindah ~528 MB hasil-kerja non-runtime ke `Desktop\Arsip_PortalHC_HasilKerja\`, tanpa merusak app.

**Architecture:** Operasi filesystem deterministik di branch `chore/cleanup-arsip-2026-06-03` pada repo utama (BUKAN worktree — file yang dibersihkan ada di working tree utama). Pola move robust: `Move-Item` folder/file utuh (tangkap tracked + gitignored sekaligus) → `git add -A` stage deletion file tracked → commit atomik per fase. Gate keselamatan: `dotnet build` PASS (regenerate bin/obj) membuktikan nol file source kehapus.

**Tech Stack:** PowerShell (Windows), git, dotnet 8 (ASP.NET Core MVC), SQL Server.

**Spec:** `docs/superpowers/specs/2026-06-03-cleanup-repo-arsip-hasil-kerja-design.md`

**Konvensi command:** semua command dijalankan dari repo root (`...\Desktop\PortalHC_KPB`). Variabel `$arsip` di-set ulang tiap command (shell state tidak persist antar invocation). Path arsip relatif: `..\Arsip_PortalHC_HasilKerja`.

---

## File Structure

Plan ini TIDAK membuat/ubah file source. Yang berubah:
- **Buat (eksternal repo):** `..\Arsip_PortalHC_HasilKerja\` + 8 subfolder + `README.md`
- **Pindah (keluar repo):** lihat spec §3.3 (folder + HTML/pdf/docx/xlsx hasil-kerja)
- **Hapus:** build output (bin/obj), 135 root png, 2 .db kosong, scratch dirs, `package.json`+`package-lock.json` root, `docs/abstract/*` (sudah pending)
- **Git:** 3 commit move + 1 commit delete di branch baru

---

## Task 0: Pre-flight + branch + baseline

**Files:** none (git + build only)

- [ ] **Step 1: Pastikan app tidak sedang running**

Run:
```powershell
Get-Process -Name dotnet -ErrorAction SilentlyContinue | Select-Object Id, ProcessName, StartTime
```
Expected: kosong, ATAU hanya build-server. Kalau ada `dotnet run` aktif (app), hentikan dulu (Ctrl-C di terminal app) agar `bin/` tidak ter-lock. JANGAN `Stop-Process` paksa kalau ragu.

- [ ] **Step 2: Cek git bersih + di branch main**

Run:
```powershell
git branch --show-current; git status --short
```
Expected: branch `main`; status hanya menampilkan 5 pending `docs/abstract/*` (D). Kalau ada perubahan lain belum di-commit, STOP & klarifikasi.

- [ ] **Step 3: Buat branch cleanup**

Run:
```powershell
git checkout -b chore/cleanup-arsip-2026-06-03
```
Expected: `Switched to a new branch 'chore/cleanup-arsip-2026-06-03'`

- [ ] **Step 4: Baseline build (bukti app sehat SEBELUM cleanup)**

Run:
```powershell
dotnet build HcPortal.sln -v q
```
Expected: `Build succeeded` + `0 Error(s)`. Kalau gagal, STOP — masalah pre-existing, bukan dari cleanup.

- [ ] **Step 5: Catat baris build PASS sebagai bukti**

Tidak ada commit di task ini. Lanjut Task 1.

---

## Task 1: Scaffold folder arsip

**Files:** Create `..\Arsip_PortalHC_HasilKerja\` (+ 8 subfolder + README.md) — di luar repo, tidak masuk git.

- [ ] **Step 1: Buat folder arsip + subfolder**

Run:
```powershell
$arsip = "..\Arsip_PortalHC_HasilKerja"
'pcp','downloads-ojt','video-proton','source-proton','presentasi','reports','samples-xlsx','screenshots' | ForEach-Object {
  New-Item -ItemType Directory -Force -Path (Join-Path $arsip $_) | Out-Null
}
Get-ChildItem $arsip | Select-Object Name
```
Expected: 8 subfolder terdaftar (downloads-ojt, pcp, presentasi, reports, samples-xlsx, screenshots, source-proton, video-proton).

- [ ] **Step 2: Tulis README arsip**

Run:
```powershell
$arsip = "..\Arsip_PortalHC_HasilKerja"
$hash = (git rev-parse --short HEAD)
@"
# Arsip Hasil-Kerja PortalHC_KPB

Dipindah dari repo PortalHC_KPB tanggal 2026-06-03 (branch chore/cleanup-arsip-2026-06-03, base commit $hash).
File di sini = hasil kerja yang TIDAK dipakai runtime website (deck presentasi, PCP, naskah video, laporan, sample import, export OJT).
Recoverable: file tracked tetap ada di git history repo; file untracked (pcp PDF, downloads, doc support Proton) hanya ada di sini.

## Isi
- pcp/            : docs/pcp-HCPortal-2026 (PCP SMART deck + Risalah + slide8)
- downloads-ojt/  : export hasil test OJT Cilacap (BERISI PII peserta)
- video-proton/   : aset produksi video PROTON (webm + script) + Naskah Video docx
- source-proton/  : sumber silabus + coaching-guidance + KKJ (docx/pdf)
- presentasi/     : deck sosialisasi/kickoff/panduan (HTML + PDF) + mockup
- reports/        : MILESTONE-REPORT, commit-log, analysis, test-reports, DATABASE_MAPPING.pdf
- samples-xlsx/   : contoh file import (soal, training, workers) — app generate template in-memory
- screenshots/    : sosialisasi-screenshots + phase324 proof
"@ | Set-Content -Path (Join-Path $arsip 'README.md') -Encoding utf8
Test-Path (Join-Path $arsip 'README.md')
```
Expected: `True`. Tidak ada commit (folder di luar repo).

---

## Task 2: MOVE folder bulk (pcp + downloads + video + source + sosialisasi)

**Files:** Move 5 folder besar (~517 MB) ke arsip.

- [ ] **Step 1: Pindah 5 folder bulk**

Run:
```powershell
$arsip = "..\Arsip_PortalHC_HasilKerja"
Move-Item "docs\pcp-HCPortal-2026"   "$arsip\pcp\"
Move-Item "downloads"                "$arsip\downloads-ojt\"
Move-Item "docs\assets\proton-video" "$arsip\video-proton\"
Move-Item "docs\doc support Proton"  "$arsip\source-proton\"
Move-Item "sosialisasi-portalhc"     "$arsip\presentasi\"
```
Expected: tanpa error.

- [ ] **Step 2: Hapus dir `docs\assets` yang jadi kosong**

Run:
```powershell
if ((Get-ChildItem "docs\assets" -Force -ErrorAction SilentlyContinue | Measure-Object).Count -eq 0) { Remove-Item "docs\assets" -Force }
Test-Path "docs\assets"
```
Expected: `False` (assets cuma berisi proton-video, kini kosong → terhapus).

- [ ] **Step 3: Verifikasi pindah & sumber hilang**

Run:
```powershell
$arsip = "..\Arsip_PortalHC_HasilKerja"
"pcp\pcp-HCPortal-2026","downloads-ojt\downloads","video-proton\proton-video","source-proton\doc support Proton","presentasi\sosialisasi-portalhc" | ForEach-Object { "{0,-45} {1}" -f $_, (Test-Path "$arsip\$_") }
"--- sumber harus hilang ---"
"docs\pcp-HCPortal-2026","downloads","docs\doc support Proton","sosialisasi-portalhc" | ForEach-Object { "{0,-30} exists={1}" -f $_, (Test-Path $_) }
```
Expected: 5 baris arsip = `True`; 4 sumber = `exists=False`.

- [ ] **Step 4: Stage deletion file tracked + commit**

Run:
```powershell
git add -A
git status --short | Select-Object -First 5
git commit -m @'
chore(cleanup): arsip folder bulk (pcp/downloads/video/source/sosialisasi)

Move ke ..\Arsip_PortalHC_HasilKerja: docs/pcp-HCPortal-2026 (331MB),
downloads/ (147MB OJT export), docs/assets/proton-video (19MB),
docs/doc support Proton/ (13MB untracked), sosialisasi-portalhc/ (7MB).
File tracked di-stage sebagai deletion; untracked dipindah fisik.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
'@
```
Expected: commit sukses; `git status` menampilkan deletion `D docs/pcp-HCPortal-2026/...`, `D sosialisasi-portalhc/...` (untracked downloads/doc-support TIDAK muncul — memang tak tertrack).

---

## Task 3: MOVE docs HTML/PDF/naskah (presentasi + reports + video naskah)

**Files:** Move ~22 file deck/report/pdf/docx.

- [ ] **Step 1: Pindah deck → presentasi/**

Run:
```powershell
$arsip = "..\Arsip_PortalHC_HasilKerja"
Move-Item "docs\Kickoff-PROTON.html"                          "$arsip\presentasi\"
Move-Item "docs\Panduan-Operasional-HC-PortalHC-KPB.html"     "$arsip\presentasi\"
Move-Item "Panduan-Belajar-API-PortalHC.html"                 "$arsip\presentasi\"
Move-Item "Sosialisasi-PROTON-KPB.html"                       "$arsip\presentasi\"
Move-Item "docs\Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html"    "$arsip\presentasi\"
Move-Item "docs\Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html" "$arsip\presentasi\"
Move-Item "docs\mockup-presentasi"                            "$arsip\presentasi\"
Move-Item "docs\Sosialisasi PortalHC v2 — Slide Deck 2026.pdf" "$arsip\presentasi\"
Move-Item "docs\Fitur PROTON — Portal HC KPB (1).pdf"          "$arsip\presentasi\"
```
Expected: tanpa error.

- [ ] **Step 2: Pindah report → reports/ (termasuk DATABASE_MAPPING.pdf)**

Run:
```powershell
$arsip = "..\Arsip_PortalHC_HasilKerja"
Move-Item "docs\MILESTONE-REPORT.html"                  "$arsip\reports\"
Move-Item "docs\admin_features_analysis.html"           "$arsip\reports\"
Move-Item "docs\commit-log-maret.html"                  "$arsip\reports\"
Move-Item "docs\commit-log-april.html"                  "$arsip\reports\"
Move-Item "docs\commit-log-mei.html"                    "$arsip\reports\"
Move-Item "docs\tutorial-workflow-claude.html"          "$arsip\reports\"
Move-Item "docs\ActiveDirectory-Guide.html"             "$arsip\reports\"
Move-Item "docs\Persiapan-Test-Manual-Assessment.html"  "$arsip\reports\"
Move-Item "docs\test 16-april"                          "$arsip\reports\"
Move-Item "docs\test-reports"                           "$arsip\reports\"
Move-Item "Database\DATABASE_MAPPING.pdf"               "$arsip\reports\"
```
Expected: tanpa error. (DATABASE_MAPPING.pdf pindah; `Database\DATABASE_MAPPING.md` + `*.sql` TETAP.)

- [ ] **Step 3: Pindah naskah video → video-proton/**

Run:
```powershell
$arsip = "..\Arsip_PortalHC_HasilKerja"
Move-Item "docs\Naskah Video PROTON.docx" "$arsip\video-proton\"
Move-Item "docs\Naskah Video CPDP.docx"   "$arsip\video-proton\"
```
Expected: tanpa error.

- [ ] **Step 4: Verifikasi Database/ inti utuh + sumber hilang**

Run:
```powershell
"Database\DATABASE_MAPPING.md","Database\01_CreateDatabase.sql" | ForEach-Object { "{0,-40} keep={1}" -f $_, (Test-Path $_) }
"Database\DATABASE_MAPPING.pdf","docs\Kickoff-PROTON.html","docs\test-reports" | ForEach-Object { "{0,-40} gone={1}" -f $_, (-not (Test-Path $_)) }
```
Expected: 2 baris `keep=True`; 3 baris `gone=True`.

- [ ] **Step 5: Stage + commit**

Run:
```powershell
git add -A
git commit -m @'
chore(cleanup): arsip deck/report/naskah docs ke Desktop archive

Move ke presentasi/ (Kickoff, Sosialisasi-Internal x2, Panduan-Operasional,
2 root HTML, mockup, 2 PDF deck), reports/ (MILESTONE-REPORT,
admin_features_analysis, commit-log x3, tutorial, AD-Guide, Persiapan-Test,
test 16-april, test-reports, DATABASE_MAPPING.pdf), video-proton/ (2 Naskah).

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
'@
```
Expected: commit sukses.

---

## Task 4: MOVE sample xlsx + screenshots

**Files:** Move 7 xlsx + 2 folder screenshot.

- [ ] **Step 1: Pindah 7 sample xlsx → samples-xlsx/**

Run:
```powershell
$arsip = "..\Arsip_PortalHC_HasilKerja"
Move-Item "Contoh_Soal_Essay.xlsx"                  "$arsip\samples-xlsx\"
Move-Item "Contoh_Soal_Mix.xlsx"                    "$arsip\samples-xlsx\"
Move-Item "Contoh_Soal_MultipleAnswer.xlsx"         "$arsip\samples-xlsx\"
Move-Item "docs\question_import_template_GAST.xlsx" "$arsip\samples-xlsx\"
Move-Item "docs\training_import_meylisa.xlsx"       "$arsip\samples-xlsx\"
Move-Item "docs\workers_import_template (rev 1).xlsx" "$arsip\samples-xlsx\"
Move-Item "docs\Contoh_Paket_Soal_Alkylation.xlsx"  "$arsip\samples-xlsx\"
```
Expected: tanpa error.

- [ ] **Step 2: Pindah screenshots → screenshots/ + hapus dir kosong**

Run:
```powershell
$arsip = "..\Arsip_PortalHC_HasilKerja"
Move-Item "docs\sosialisasi-screenshots" "$arsip\screenshots\"
Move-Item "docs\screenshots\phase324"    "$arsip\screenshots\"
if ((Get-ChildItem "docs\screenshots" -Force -ErrorAction SilentlyContinue | Measure-Object).Count -eq 0) { Remove-Item "docs\screenshots" -Force }
Test-Path "docs\screenshots"
```
Expected: `False` (docs\screenshots cuma berisi phase324, kini kosong → terhapus).

- [ ] **Step 3: Verifikasi**

Run:
```powershell
$arsip = "..\Arsip_PortalHC_HasilKerja"
"samples-xlsx hitung: " + (Get-ChildItem "$arsip\samples-xlsx" -File | Measure-Object).Count
"screenshots\phase324 ada: " + (Test-Path "$arsip\screenshots\phase324")
"sumber xlsx gone: " + (-not (Test-Path "Contoh_Soal_Essay.xlsx"))
```
Expected: `samples-xlsx hitung: 7`; `phase324 ada: True`; `sumber xlsx gone: True`.

- [ ] **Step 4: Stage + commit (semua move selesai)**

Run:
```powershell
git add -A
git commit -m @'
chore(cleanup): arsip sample xlsx + screenshots ke Desktop archive

Move ke samples-xlsx/ (3 Contoh_Soal root + 4 import-template docs;
app generate template in-memory, file ini sample dev-reference) +
screenshots/ (sosialisasi-screenshots + phase324 proof). Hapus dir
docs/screenshots kosong.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
'@
```
Expected: commit sukses.

---

## Task 5: DELETE junk + orphan + abstract pending

**Files:** Remove build output, 135 png, 2 .db, scratch dirs; `git rm` orphan package; stage abstract deletions.

- [ ] **Step 1: Hapus build output + scratch (untracked, gitignored)**

Run:
```powershell
'bin','obj','HcPortal.Tests\bin','HcPortal.Tests\obj','.playwright-mcp','test-results','.worktrees' | ForEach-Object {
  if (Test-Path $_) { Remove-Item $_ -Recurse -Force }
}
'bin','obj','HcPortal.Tests\bin' | ForEach-Object { "{0,-25} gone={1}" -f $_, (-not (Test-Path $_)) }
```
Expected: 3 baris `gone=True`.

- [ ] **Step 2: Hapus 135 root png + 2 .db kosong (untracked)**

Run:
```powershell
Remove-Item ".\*.png" -Force -ErrorAction SilentlyContinue
Remove-Item "HcPortal.db","HcPortal-pre-329-uat.db" -Force -ErrorAction SilentlyContinue
"root png sisa: " + (Get-ChildItem ".\*.png" -File -ErrorAction SilentlyContinue | Measure-Object).Count
"db gone: " + (-not (Test-Path "HcPortal.db"))
```
Expected: `root png sisa: 0`; `db gone: True`. (wwwroot png TIDAK tersentuh — `*.png` non-recursive cuma root.)

- [ ] **Step 3: git rm orphan package.json root**

Run:
```powershell
git rm package.json package-lock.json
```
Expected: `rm 'package.json'` + `rm 'package-lock.json'`. (Playwright asli tetap di `tests/package.json`.)

- [ ] **Step 4: Stage semua (termasuk abstract pending) + commit**

Run:
```powershell
git add -A
git status --short
git commit -m @'
chore(cleanup): hapus build output + junk untracked + orphan package root

Remove (regenerable/throwaway): bin+obj root, HcPortal.Tests/bin+obj
(~400MB), 135 root *.png screenshot verify (~32MB), HcPortal.db +
HcPortal-pre-329-uat.db (0-byte SQLite leftover), .playwright-mcp,
test-results, .worktrees. git rm package.json+lock root (orphan,
Playwright asli di tests/). Finalize docs/abstract/* deletion pending.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
'@
```
Expected: commit sukses; `git status --short` SEBELUM commit menampilkan `D package.json`, `D package-lock.json`, `D docs/abstract/...` (5). Root png + bin/obj TIDAK muncul (gitignored).

---

## Task 6: Verify gate (build + integritas KEEP)

**Files:** none (verifikasi).

- [ ] **Step 1: Rebuild — bukti nol source kehapus**

Run:
```powershell
dotnet build HcPortal.sln -v q
```
Expected: `Build succeeded`, `0 Error(s)`. (Regenerate bin/obj root + Tests/bin.) **Kalau gagal → cleanup menghapus file source. STOP, rollback (Task 7 fallback).**

- [ ] **Step 2: Smoke run app (rekomendasi; butuh SQL Server Dev up)**

Run (background):
```powershell
Start-Job -Name hcsmoke -ScriptBlock { Set-Location $using:PWD; dotnet run --project HcPortal.csproj --urls http://localhost:5277 }
Start-Sleep -Seconds 25
try { (Invoke-WebRequest http://localhost:5277 -UseBasicParsing -TimeoutSec 15).StatusCode } catch { $_.Exception.Message }
Stop-Job hcsmoke; Remove-Job hcsmoke
```
Expected: `200`. Kalau DB Dev tak tersedia, build PASS (Step 1) sudah cukup sebagai gate utama; catat smoke di-skip.

- [ ] **Step 3: Cek integritas item KEEP (jangan ada yang ikut kehapus)**

Run:
```powershell
"wwwroot\uploads","Database\DATABASE_MAPPING.md","Database\01_CreateDatabase.sql","scripts\backup-dev-pre-migration.ps1","docs\checklist-deploy.html","docs\sertifikat-ecosystem","docs\DEV_WORKFLOW.md","docs\IT_NOTIFY.md","docs\superpowers","tests\package.json","HcPortal.Tests\OrganizationControllerTests.cs" | ForEach-Object { "{0,-45} keep={1}" -f $_, (Test-Path $_) }
```
Expected: SEMUA `keep=True`.

- [ ] **Step 4: Cek arsip lengkap**

Run:
```powershell
$arsip = "..\Arsip_PortalHC_HasilKerja"
Get-ChildItem $arsip -Directory | ForEach-Object { "{0,-16} files={1}" -f $_.Name, (Get-ChildItem $_.FullName -Recurse -File | Measure-Object).Count }
"Total arsip MB: " + [math]::Round((Get-ChildItem $arsip -Recurse -File | Measure-Object Length -Sum).Sum/1MB,1)
```
Expected: tiap subfolder `files>0` (kecuali bila ada yg kosong — investigasi); Total arsip ratusan MB (~520).

---

## Task 7: Tutup + ringkasan

**Files:** none.

- [ ] **Step 1: Status & log final**

Run:
```powershell
git status --short
git log --oneline -5
"working tree MB (tanpa .git): " + [math]::Round(((Get-ChildItem . -Recurse -File -Force -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\\.git\\' } | Measure-Object Length -Sum).Sum/1MB),0)
```
Expected: `git status` bersih (nothing to commit); 4 commit cleanup di log (3 move + 1 delete); working tree jauh lebih kecil.

- [ ] **Step 2: Update memory project**

Tulis file memory `project_cleanup_arsip_2026_06_03.md` (slug) + 1 baris di MEMORY.md: branch `chore/cleanup-arsip-2026-06-03`, 4 commit, arsip di `Desktop\Arsip_PortalHC_HasilKerja`, ~960MB direclaim, NOT merged/pushed, sertifikat-ecosystem defer s/d v19.0 push.

- [ ] **Step 3: Lapor — TIDAK auto-push/merge**

Sampaikan ke user: branch siap. Merge ke main + push = keputusan user/IT (banyak commit v19/v20/v21 lokal belum dipush — koordinasi Team IT per CLAUDE.md DEV_WORKFLOW). Tawarkan opsi: (a) merge ke main lokal, (b) biarkan di branch.

---

## Catatan rollback (bila Task 6 build GAGAL)

```powershell
# kembalikan file dari arsip + buang branch
git checkout main
git branch -D chore/cleanup-arsip-2026-06-03   # buang commit cleanup
# file tracked balik via git (sudah di main); file untracked dipindah balik manual:
$arsip = "..\Arsip_PortalHC_HasilKerja"
Move-Item "$arsip\pcp\pcp-HCPortal-2026" "docs\"
Move-Item "$arsip\downloads-ojt\downloads" "."
Move-Item "$arsip\source-proton\doc support Proton" "docs\"
```
File tracked yang dipindah otomatis kembali saat `git checkout main` (working tree main belum punya deletion). Junk untracked yang terhapus (bin/obj/png) regenerate via build / memang throwaway.

---

## Deferred (out of scope plan ini)

- Arsip `docs/sertifikat-ecosystem/` — tunggu v19.0 dipush ke IT.
- Prune baris `.gitignore` yang jadi stale (pcp PDF, downloads/, doc support Proton/, test-reports/) — harmless, opsional.
- Fix stale SQLite string `appsettings.json:11` + dead WAL `Program.cs:140-146` + paket Sqlite di csproj.
- Push / merge ke main.
