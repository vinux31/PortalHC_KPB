# Phase 369: Sync H1 Search-Drop Fix main → ITHandoff - Research

**Researched:** 2026-06-11
**Domain:** Git cherry-pick (single-commit cross-branch sync) + .NET service-layer guard fix + xUnit regresi + UAT live
**Confidence:** HIGH

## Summary

Fase ini adalah operasi cherry-pick 1 commit yang sangat sempit dan sudah terverifikasi. Commit `14e7adc5` ("fix(cmp): ManageAssessment worker search ke-drop saat searchScope kosong (H1)") di branch `main` menyentuh 2 file (`Services/WorkerDataService.cs` +6/-2, `HcPortal.Tests/WorkerDataServiceSearchTests.cs` +13) dengan perubahan tunggal: guard SQL name pre-narrow di `GetWorkersInSection` dari `if (searchScope == "Nama" && ...)` menjadi `if ((string.IsNullOrEmpty(searchScope) || searchScope == "Nama") && ...)`, plus 1 test regresi `Scope_Null_WithSearch_FiltersByName_H1`. [VERIFIED: git show 14e7adc5]

Verifikasi paling kuat sesi ini: KEDUA file di ITHandoff HEAD (`33e14a2b`) **identik byte-for-byte dengan parent commit `14e7adc5^`** (`git diff HEAD 14e7adc5^ -- <file>` keluar kosong untuk dua file). Artinya cherry-pick akan apply 100% bersih tanpa risiko konflik — bukan sekadar prediksi merge-tree, tapi fakta bahwa konteks pre-image cocok sempurna. Commit `14e7adc5` belum ada di ITHandoff (`git merge-base --is-ancestor 14e7adc5 HEAD` = NO). [VERIFIED: git diff/merge-base 2026-06-11]

**Primary recommendation:** Stash/sisihkan dulu working tree yang dirty (3 file modified non-target + 2 untracked), jalankan `git cherry-pick -x 14e7adc5`, verifikasi guard identik main + `git diff main -- Services/WorkerDataService.cs` kosong, lalu `dotnet build` + `dotnet test` + 1 skenario UAT live. Tidak ada migration, tidak ada perubahan signature, tidak ada sentuhan caller. Risiko utama BUKAN teknis cherry-pick (sudah clean) melainkan koordinasi git dengan sesi paralel Phase 363 dan kebersihan working tree.

## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (Metode sync):** Cherry-pick `14e7adc5` SAJA (2 file: `Services/WorkerDataService.cs` +6/-2, `HcPortal.Tests/WorkerDataServiceSearchTests.cs` +13). BUKAN full merge main→ITHandoff — merge penuh (13 commit, termasuk self-heal seed F1/F2 + 1 konflik docs `DB_HANDOFF_IT_2026-06-06.html`) tetap event terpisah terencana sebelum handoff IT. Git dedup commit ter-pick saat merge nanti.
- **D-02 (Jejak commit):** `git cherry-pick -x 14e7adc5` — pesan asli + baris "(cherry picked from commit ...)" untuk audit trail; memudahkan deteksi duplikat saat full merge.
- **D-03 (Verifikasi):** Suite penuh `dotnet test` hijau + UAT live 1 skenario Playwright @5277: login admin → `/Admin/ManageAssessment` Tab Input Records → search nama/NIP → list TERFILTER (bukan balikin semua row). Konvensi CLAUDE.md: `Authentication__UseActiveDirectory=false dotnet run`.

### Claude's Discretion
- Penanganan kalau cherry-pick ternyata konflik (sudah diverifikasi clean via merge-tree 2026-06-11, tapi kalau ITHandoff berubah): resolve manual dengan hasil akhir = guard identik main.
- Urutan langkah verifikasi (build/test/UAT).

### Deferred Ideas (OUT OF SCOPE)
- **Full merge main→ITHandoff** (13 commit: self-heal seed F1/F2, docs audit, dll + resolve 1 konflik docs) — event terencana pre-handoff IT, BUKAN scope 369. Sudah tercatat di STATE.md "Push pending IT" + memory.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| URG-01 | Fix H1 search-drop (`14e7adc5` main) tersinkron ke ITHandoff — `GetWorkersInSection` searchScope null/kosong di-treat "Nama" (search tidak diabaikan diam-diam) + test regresi hijau | Diff persis commit terverifikasi (`git show`); guard target line 257-266 ITHandoff = pre-image; cherry-pick clean (file identik parent); test landing site terverifikasi (setelah `Scope_Null_NoFilter_BackwardCompat` line 93) |

## Project Constraints (from CLAUDE.md)

- **Respon dalam Bahasa Indonesia.** (Berlaku untuk plan/output downstream.)
- **Develop Workflow — verifikasi lokal WAJIB sebelum commit dianggap selesai:** `dotnet build` + `dotnet run` (cek `http://localhost:5277`) + cek DB lokal + Playwright bila ada. Untuk fase ini: build + test + UAT live @5277.
- **AD lokal:** UAT live HARUS pakai `Authentication__UseActiveDirectory=false dotnet run` supaya login admin lolos hybrid (appsettings handoff AD=true). [CITED: MEMORY.md project_355_shipped]
- **❌ Jangan edit kode/DB langsung di server Dev/Prod.** Fase ini murni operasi lokal pada branch ITHandoff.
- **❌ Jangan push tanpa verifikasi lokal.** Push ke IT = event terpisah (lihat STATE.md "Push pending IT"); fase ini SHIP LOCAL.
- **Seed Data Workflow:** Tidak ada seed temporary dibutuhkan — test pakai EFCore InMemory (Guid per test, tidak sentuh DB lokal). UAT live pakai data existing DB lokal (search existing worker). Tidak perlu snapshot/restore DB.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Filter worker by nama/NIP saat search | API / Backend (Service: `WorkerDataService`) | — | Guard SQL pre-narrow ada di service layer; ini server-side query, bukan client filter |
| Pemanggilan filter dari Tab Input Records | API / Backend (Controller: `AssessmentAdminController.ManageAssessmentTab_Training`) | — | Controller pass param ke service; tidak ada perubahan controller di fase ini |
| Regresi test (verifikasi behavior) | Test (xUnit + EFCore InMemory) | — | Test query-path murni, tidak sentuh DB fisik |
| Render hasil filter | Frontend Server (Razor partial `_AssessmentGroupsTab` / Tab Training partial) | — | View tidak diubah; benefit otomatis dari guard yang sudah benar |

## Standard Stack

Tidak ada dependency baru. Fase ini menggunakan tooling existing.

### Core
| Tool/Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| git | 2.53.0.windows.1 | cherry-pick `-x` cross-branch sync | Tooling repo standar; `-x` flag locked oleh D-02 [VERIFIED: git --version] |
| .NET SDK | 8.0.418 | `dotnet build` + `dotnet run` localhost:5277 | Project target net8.0 [VERIFIED: dotnet --version] |
| xunit | 2.9.3 | Test runner regresi | Framework test existing project [VERIFIED: HcPortal.Tests.csproj] |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test host | Existing [VERIFIED: csproj] |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | InMemory DB untuk WorkerDataServiceSearchTests | Existing — test tidak sentuh SQL Server lokal [VERIFIED: csproj] |
| xunit.runner.visualstudio | 3.0.1 | VS/CLI test discovery | Existing [VERIFIED: csproj] |

**Installation:** Tidak ada — semua sudah terinstal. Tidak ada `npm install` / `dotnet add package`.

## Architecture Patterns

### System Architecture Diagram (alur data yang difix)

```
[Browser: Tab Input Records /Admin/ManageAssessment]
   user ketik nama/NIP di search box
        │  GET /Admin/ManageAssessmentTab_Training?search=budi&section=...
        ▼
[AssessmentAdminController.ManageAssessmentTab_Training]  (line 253, [HttpGet][Authorize Admin,HC])
   │  isInitialState? → kalau ada search/filter, NOT initial
        ▼  panggil GetWorkersInSection(section, unit, category, search, statusFilter)  ← line 280: 5 args, searchScope = null (param ke-9 default)
        ▼
[WorkerDataService.GetWorkersInSection]  (line 244)
   │  filter section/unit
        ▼
   GUARD pre-narrow (line 259) ──────── INI YANG DIFIX
   ┌─ SEBELUM (ITHandoff sekarang): if (searchScope == "Nama" && !empty(search))
   │     searchScope == null → FALSE → guard SKIP → search DI-DROP diam-diam ❌
   │     → SQL load SEMUA worker di section (search diabaikan)
   └─ SESUDAH (fix H1): if ((IsNullOrEmpty(searchScope) || searchScope=="Nama") && !empty(search))
         searchScope == null → IsNullOrEmpty TRUE → guard JALAN → SQL Where FullName/NIP Contains(search) ✅
        ▼
   await usersQuery.AsNoTracking().ToListAsync()  → list terfilter
        ▼
[paginate di caller → PartialView _AssessmentGroupsTab] → list TERFILTER tampil
```

File-to-implementation mapping di Component Responsibilities table di bawah.

### Component Responsibilities

| File | Peran | Diubah fase ini? |
|------|-------|------------------|
| `Services/WorkerDataService.cs` (line 257-266) | Guard pre-narrow — TARGET fix | YA (via cherry-pick: +6/-2) |
| `HcPortal.Tests/WorkerDataServiceSearchTests.cs` | Test regresi — landing test H1 | YA (via cherry-pick: +13) |
| `Services/IWorkerDataService.cs` (line 14) | Interface signature `GetWorkersInSection` | TIDAK — signature tak berubah |
| `Controllers/AssessmentAdminController.cs` (line 280) | Caller terdampak (Tab Input Records), 5 args searchScope=null | TIDAK — caller dapat fix otomatis |
| `Controllers/CMPController.cs` (line 676, 737) | Caller lain, pass searchScope dari form (benefit saat scope kosong) | TIDAK |
| `Controllers/CMPController.cs` (line 516, 785) | Caller tanpa search (sectionFilter saja) | TIDAK terdampak (no search) |

### Pattern 1: Cherry-pick `-x` single commit (D-02)
**What:** Bawa 1 commit dari main ke branch lain dengan jejak audit.
**When to use:** Locked oleh D-02 untuk mempermudah dedup saat full merge nanti.
**Example:**
```bash
# Source: git docs — git-scm.com/docs/git-cherry-pick
git cherry-pick -x 14e7adc5
# -x menambah footer "(cherry picked from commit 14e7adc5...)" ke pesan commit
```

### Anti-Patterns to Avoid
- **Manual edit guard lalu commit (tanpa cherry-pick):** Melanggar D-01/D-02; menghilangkan jejak audit `-x` yang dipakai untuk dedup full-merge. Cherry-pick wajib karena sudah terverifikasi clean.
- **Full merge main→ITHandoff:** OUT OF SCOPE (Deferred). Akan bawa 13 commit + 1 konflik docs.
- **Ubah signature `GetWorkersInSection` atau tambah searchScope di caller:** Tidak perlu — fix bekerja di level guard. Comment existing line 268 di controller eksplisit "JANGAN ubah GetWorkersInSection signature".

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Bawa commit cross-branch | Copy-paste diff manual + commit baru | `git cherry-pick -x 14e7adc5` | D-02 locked; auto audit-trail; git dedup saat merge |
| Test regresi guard | Test manual baru | Cherry-pick bawa `Scope_Null_WithSearch_FiltersByName_H1` apa adanya | Test sudah ada di commit; identik main |
| Verifikasi hasil identik main | Bandingkan visual | `git diff main -- Services/WorkerDataService.cs` (harus kosong utk hunk) | Bukti objektif identik |

**Key insight:** Seluruh perubahan sudah ada dalam 1 commit yang terverifikasi clean. Tidak ada yang perlu "dibangun" — hanya di-apply via cherry-pick dan diverifikasi.

## Runtime State Inventory

> Fase ini cherry-pick kode + test (bukan rename/refactor/migration). Inventory minimal — diisi eksplisit per kategori.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — verified: tidak ada migration, tidak ada perubahan skema/seed. Test pakai EFCore InMemory (Guid per test). | none |
| Live service config | None — verified: tidak ada konfigurasi service eksternal disentuh. | none |
| OS-registered state | None — operasi git + build/test/run lokal saja. | none |
| Secrets/env vars | UAT pakai `Authentication__UseActiveDirectory=false` (env var sementara saat `dotnet run`, bukan secret baru, tidak persist). | none — set inline saat run |
| Build artifacts | `bin/`/`obj/` HcPortal + HcPortal.Tests akan rebuild saat `dotnet build`/`dotnet test`; normal. Tidak ada egg-info/package install. | none — rebuild otomatis |

**Canonical question — setelah cherry-pick di-apply, apa runtime system yang masih cache string lama?** Tidak ada. Ini bukan rename; fix mengubah 1 kondisi boolean di method query. Tidak ada cache/registry/datastore yang menyimpan string yang berubah.

## Common Pitfalls

### Pitfall 1: Working tree dirty saat cherry-pick
**What goes wrong:** `git cherry-pick` gagal/menolak atau mencampur perubahan lain kalau ada file modified/staged.
**Why it happens:** Saat research, `git status` menunjukkan 3 file modified (`.claude/settings.local.json`, `.planning/config.json`, `docs/SEED_JOURNAL.md`) + 2 untracked (`.planning/phases/361-bypass-ui-b/361-PATTERNS.md`, `.planning/phases/363-.../363-PATTERNS.md`). [VERIFIED: git status 2026-06-11]
**How to avoid:** Cherry-pick TIDAK menyentuh file-file ini (target = `Services/WorkerDataService.cs` + test), jadi secara teknis cherry-pick bisa jalan dengan dirty tree non-konflik. TAPI untuk kebersihan dan menghindari commit nyangkut: pastikan tidak ada file TARGET yang dirty (terverifikasi: kedua file target tidak modified). Opsional `git stash -u` dulu kalau ingin tree benar-benar bersih, lalu `git stash pop` setelah cherry-pick. Cek `git status` sebelum mulai.
**Warning signs:** `git status` memunculkan `Services/WorkerDataService.cs` atau `HcPortal.Tests/WorkerDataServiceSearchTests.cs` sebagai modified SEBELUM cherry-pick (saat ini TIDAK — keduanya clean).

### Pitfall 2: Operasi git paralel dari sesi Phase 363
**What goes wrong:** Sesi paralel 363 sedang commit ke branch `ITHandoff` yang SAMA (commit terbaru `33e14a2b`, sebelumnya 363-05/363-06). Kalau cherry-pick + commit 363 terjadi bersamaan → race / index lock.
**Why it happens:** `use_worktrees: true` di config tapi 363 dieksekusi di working dir yang sama (commit langsung ke ITHandoff). [VERIFIED: git log; config.json]
**How to avoid:** Lakukan cherry-pick saat TIDAK ada operasi git 363 berjalan. Cek `git status` + tidak ada `.git/index.lock`. File target 369 (`WorkerDataService.cs` + test) terverifikasi TIDAK disentuh phase 363-368 → zero file-conflict, aman secara konten; yang perlu dijaga hanya timing operasi git (jangan dua proses git tulis index bersamaan).
**Warning signs:** Error "Another git process seems to be running" / `index.lock` ada.

### Pitfall 3: CRLF warnings normal di repo ini
**What goes wrong:** Cherry-pick/checkout di Windows memunculkan `warning: LF will be replaced by CRLF` — bisa terlihat seperti error.
**Why it happens:** Repo berisi file dengan line-ending mixed; environment Windows + git autocrlf. Ini normal di project ini.
**How to avoid:** Abaikan warning CRLF — bukan kegagalan. Verifikasi sukses lewat exit code + `git diff main -- Services/WorkerDataService.cs` kosong untuk hunk.
**Warning signs:** Hanya warning (bukan "error" / "fatal"); cherry-pick tetap menghasilkan commit baru.

### Pitfall 4: AD login gagal saat UAT (lupa env var)
**What goes wrong:** `dotnet run` biasa → login admin@pertamina.com gagal karena appsettings AD=true (handoff config).
**Why it happens:** appsettings di branch ITHandoff set AD=true untuk handoff IT; lokal butuh override.
**How to avoid:** UAT WAJIB `Authentication__UseActiveDirectory=false dotnet run` (CLAUDE.md / MEMORY.md). Login admin@pertamina.com. [CITED: MEMORY.md reference_dev_credentials, project_355_shipped]
**Warning signs:** Halaman login menolak kredensial admin lokal.

### Pitfall 5: Verifikasi "identik main" tidak dijalankan
**What goes wrong:** Cherry-pick sukses tapi tidak ada bukti hasil = main (SC #1).
**Why it happens:** Lompat ke build/test tanpa cek diff.
**How to avoid:** Setelah cherry-pick, jalankan `git diff main -- Services/WorkerDataService.cs` → harus KOSONG (atau hanya beda di luar hunk H1; secara teori kosong total karena file identik parent + apply commit yang sama → konvergen ke versi main). Plus grep guard string.
**Warning signs:** diff menunjukkan perbedaan pada blok guard line 257-266.

## Code Examples

### Verifikasi pra-cherry-pick (working tree + tidak ada git op berjalan)
```bash
# Source: repo state 2026-06-11
git status --short                 # target file harus TIDAK muncul sbg modified
test -f .git/index.lock && echo "TUNGGU: git op lain berjalan" || echo "aman"
git rev-parse --abbrev-ref HEAD    # pastikan di ITHandoff
git merge-base --is-ancestor 14e7adc5 HEAD && echo "sudah ada (skip)" || echo "belum ada (lanjut)"
```

### Cherry-pick (D-02)
```bash
# Source: git-scm.com/docs/git-cherry-pick
git cherry-pick -x 14e7adc5
```

### Kalau konflik (Claude's Discretion — fallback; TIDAK diharapkan)
```bash
# Rollback bersih:
git cherry-pick --abort
# (lalu resolve manual: hasil akhir guard HARUS identik main:
#  if ((string.IsNullOrEmpty(searchScope) || searchScope == "Nama") && !string.IsNullOrEmpty(search)) )
```

### Verifikasi hasil (SC #1)
```bash
# Hasil harus identik main untuk file target:
git diff main -- Services/WorkerDataService.cs        # expect: KOSONG
git diff main -- HcPortal.Tests/WorkerDataServiceSearchTests.cs  # expect: KOSONG
# Grep guard string (SC #1 literal):
git grep -n "string.IsNullOrEmpty(searchScope) || searchScope == \"Nama\"" -- Services/WorkerDataService.cs
```

### Verifikasi build + test (SC #2)
```bash
dotnet build
dotnet test                                            # full suite hijau
# (opsional, cepat — hanya test H1):
dotnet test --filter "FullyQualifiedName~Scope_Null_WithSearch_FiltersByName_H1"
# (opsional — semua test WorkerDataService search):
dotnet test --filter "FullyQualifiedName~WorkerDataServiceSearchTests"
```

### UAT live (SC #3, D-03)
```bash
# AD off WAJIB (CLAUDE.md):
$env:Authentication__UseActiveDirectory="false"; dotnet run
# lalu http://localhost:5277 → login admin@pertamina.com →
# /Admin/ManageAssessment → Tab Input Records → pilih section → search nama/NIP →
# list TERFILTER (bukan semua row). PowerShell syntax (env Windows).
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Guard `searchScope == "Nama"` (REC-06 D-07, Phase 350 v23.0) | Guard `IsNullOrEmpty(searchScope) \|\| searchScope == "Nama"` (H1 fix) | commit `14e7adc5` (2026-06-06, di main) | Caller lama (searchScope=null) tidak lagi drop search diam-diam |

**Deprecated/outdated:** Tidak ada. REC-06 D-07 invariant tetap LOCKED (search assessment-title filter di level worker post-load); H1 hanya menambah cabang null/kosong → "Nama", tidak membatalkan post-load union untuk Training/Keduanya. [VERIFIED: STATE.md Accumulated Context; git show 14e7adc5 komentar guard]

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| — | (kosong) | — | — |

**Semua klaim di research ini terverifikasi via git/file inspection (HIGH) atau dikutip dari MEMORY.md/STATE.md/CLAUDE.md.** Tidak ada klaim `[ASSUMED]` yang butuh konfirmasi user. Catatan: jumlah test suite "~214+" disebut di prompt (dari quick task 260611-m9r yang ship 214/214); angka pasti pasca sesi 363 belum diukur ulang di research ini — planner cukup memastikan "full suite hijau" (SC #2), bukan angka spesifik.

## Open Questions

1. **Apakah `git diff main -- Services/WorkerDataService.cs` benar-benar kosong total pasca cherry-pick?**
   - What we know: File ITHandoff = parent commit `14e7adc5^` (identik), dan cherry-pick apply commit yang sama → konten file konvergen ke versi `14e7adc5` (= versi main untuk file ini, kecuali main punya commit lain yang menyentuh file ini setelahnya).
   - What's unclear: Apakah main punya commit LAIN setelah `14e7adc5` yang menyentuh `WorkerDataService.cs` (yang akan membuat diff main tidak kosong). Tidak diperiksa di research ini.
   - Recommendation: Planner sertakan langkah verifikasi `git diff main -- Services/WorkerDataService.cs`; kalau tidak kosong, periksa apakah perbedaan ada di LUAR hunk H1 (line 257-266). SC #1 sebenarnya hanya menuntut guard identik main — gunakan grep guard string sebagai kriteria utama, diff sebagai konfirmasi.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| git | cherry-pick `-x`, verifikasi diff | ✓ | 2.53.0.windows.1 | — |
| .NET SDK | `dotnet build` / `test` / `run` | ✓ | 8.0.418 | — |
| SQL Server lokal (HcPortalDB_Dev) | UAT live @5277 (search worker existing) | ✓ (asumsi — DB lokal aktif sesi sebelumnya) | — | UAT bisa pakai data existing; kalau kosong, search akan mengembalikan list kosong (masih valid: bukan "semua row") |
| Playwright (UAT browser) | UAT skenario D-03 | ✓ (dipakai konsisten di fase 352-362) | — | UAT manual browser kalau Playwright tidak siap |
| Commit `14e7adc5` di repo | Cherry-pick | ✓ (ada di main, reachable) | — | — |

**Missing dependencies with no fallback:** Tidak ada — semua tersedia.
**Missing dependencies with fallback:** Tidak ada blocking.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 + EFCore.InMemory 8.0.0 |
| Config file | none (tidak ada xunit.runner.json / .runsettings) [VERIFIED: ls] |
| Quick run command | `dotnet test --filter "FullyQualifiedName~Scope_Null_WithSearch_FiltersByName_H1"` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| URG-01 (SC#1) | Guard `WorkerDataService` = `(IsNullOrEmpty(searchScope) \|\| searchScope=="Nama") && !empty(search)` identik main | static/grep | `git grep -n "string.IsNullOrEmpty(searchScope) \|\| searchScope == \"Nama\"" -- Services/WorkerDataService.cs` | ✅ (post-cherry-pick) |
| URG-01 (SC#2) | searchScope=null + ada search → filter by name (tidak drop) | unit | `dotnet test --filter "FullyQualifiedName~Scope_Null_WithSearch_FiltersByName_H1"` | ✅ (dibawa cherry-pick; mendarat di `WorkerDataServiceSearchTests.cs` setelah `Scope_Null_NoFilter_BackwardCompat` line 93) |
| URG-01 (SC#2) | Regresi semua scope (Nama/Training/Keduanya/Null/PendingGrading) tetap hijau | unit | `dotnet test` (full) | ✅ (existing 4 test REC-06 + REC-07) |
| URG-01 (SC#3) | UAT live: Tab Input Records search nama/NIP memfilter | manual/e2e (Playwright) | manual @5277 (lihat Code Examples UAT) | N/A (live, bukan automated commit) |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~WorkerDataServiceSearchTests"` (cepat, hanya search tests)
- **Per wave merge / sebelum SHIP:** `dotnet test` (full suite hijau — SC#2)
- **Phase gate:** Full suite green + grep guard cocok + UAT live PASS sebelum `/gsd-verify-work`

### Wave 0 Gaps
- None — infrastruktur test sudah lengkap. Test H1 (`Scope_Null_WithSearch_FiltersByName_H1`) dibawa langsung oleh cherry-pick (bagian dari commit `14e7adc5`), tidak perlu ditulis tangan. File `WorkerDataServiceSearchTests.cs` + helper `MakeService`/`User`/`Training`/`Session` sudah ada di ITHandoff (Phase 346). Tidak perlu install framework.

## Security Domain

> `security_enforcement` tidak diset di config.json → treat as enabled. Fase ini tidak memperkenalkan permukaan serang baru (1 perubahan kondisi boolean di query read-only). Kategori ASVS relevan minimal.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tidak diubah (route `ManageAssessmentTab_Training` sudah `[Authorize(Roles="Admin, HC")]`) |
| V3 Session Management | no | Tidak disentuh |
| V4 Access Control | no (tak berubah) | Route target tetap `[Authorize(Roles = "Admin, HC")]` — fix tidak melonggarkan otorisasi [VERIFIED: AssessmentAdminController.cs:251] |
| V5 Input Validation | yes (sudah aman) | `search` dipakai di EF LINQ `.Where(... Contains(search))` → parameterized query (bukan string concat). `.ToLower()` aman. Tidak ada raw SQL. [VERIFIED: WorkerDataService.cs:259-265] |
| V6 Cryptography | no | Tidak relevan |

### Known Threat Patterns for stack (.NET EF Core read query)
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| SQL injection via `search` | Tampering | EF Core LINQ `.Contains()` → parameterized; sudah aman, tidak diubah fix ini |
| Information disclosure (search expose worker lain) | Information Disclosure | Fix menyempitkan hasil (filter), TIDAK memperluas; section/unit scope tetap. Risiko menurun, bukan naik. |

## Sources

### Primary (HIGH confidence)
- `git show 14e7adc5` (--stat + full diff) — spesifikasi perubahan persis: 2 file, +17/-2, guard `(IsNullOrEmpty(searchScope) || searchScope=="Nama")`, test `Scope_Null_WithSearch_FiltersByName_H1`
- `git diff HEAD 14e7adc5^ -- Services/WorkerDataService.cs` & `-- HcPortal.Tests/WorkerDataServiceSearchTests.cs` → KOSONG (file ITHandoff identik parent commit → cherry-pick clean)
- `git merge-base --is-ancestor 14e7adc5 HEAD` → NO (commit belum di ITHandoff)
- `git status --short` → working tree dirty: 3 modified (non-target) + 2 untracked
- `git log --oneline` → sesi paralel 363 commit ke ITHandoff (`33e14a2b`...); no cherry-pick/merge/rebase in progress
- File reads: `Services/WorkerDataService.cs:244-268`, `IWorkerDataService.cs:14`, `AssessmentAdminController.cs:250-288`, `WorkerDataServiceSearchTests.cs:1-120`, `CMPController.cs` (grep callers), `HcPortal.Tests.csproj`
- `.planning/CONTEXT.md` (369), `REQUIREMENTS.md` (URG-01), `STATE.md`, `ROADMAP.md` (§369 SC 1-3), `config.json`, `CLAUDE.md`
- `dotnet --version` → 8.0.418; `git --version` → 2.53.0.windows.1

### Secondary (MEDIUM confidence)
- MEMORY.md (reference_dev_credentials, project_355_shipped) — AD=false untuk UAT lokal, admin@pertamina.com
- git-scm.com/docs/git-cherry-pick — perilaku `-x` flag (footer audit-trail)

### Tertiary (LOW confidence)
- Jumlah test suite "~214+" (dari quick task 260611-m9r) — angka pasca-363 tidak diukur ulang; tidak load-bearing (SC#2 = "hijau", bukan angka)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua versi terverifikasi via csproj + CLI
- Architecture / diff persis: HIGH — `git show` + diff parent menunjukkan apply clean, file identik pre-image
- Cherry-pick clean: HIGH — bukan prediksi merge-tree saja, tapi fakta file ITHandoff = parent commit byte-identik
- Pitfalls: HIGH — working tree dirty + sesi 363 paralel + CRLF + AD env var semuanya terverifikasi di environment ini
- Validation: HIGH — test landing site + framework + command terverifikasi

**Research date:** 2026-06-11
**Valid until:** 2026-06-14 (3 hari — sesi paralel 363 aktif menulis ke ITHandoff; kalau 363 menyentuh file target atau ITHandoff bergerak signifikan, re-verify `git diff HEAD 14e7adc5^ -- <file>` sebelum cherry-pick)
