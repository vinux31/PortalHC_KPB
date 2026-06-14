# Phase 355: Test & UAT (gambar di soal assessment) — Research

**Researched:** 2026-06-09
**Domain:** Test consolidation (xUnit gap-audit) + committed Playwright E2E UAT untuk fitur gambar v24.0 (admin upload → peserta StartExam/Results render)
**Confidence:** HIGH (semua temuan code-grounded; file:line diverifikasi sesi ini)

> Semua keputusan sudah LOCKED di CONTEXT.md. Riset ini menjawab **HOW** mengeksekusinya, bukan mengevaluasi ulang. Fokus: selector persis, file:line, command, dan pemetaan tes existing → SC.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (Metode UAT):** Spec Playwright **committed** baru `tests/e2e/image-in-assessment.spec.ts` (nama final = discretion). Pakai pola infra existing. BUKAN MCP-only (MCP boleh ad-hoc debugging).
- **D-02 (Scope xUnit TST-01):** **Gap-audit + suite hijau saja.** 3 file tes existing TIDAK ditulis ulang. Tambah HANYA gap nyata. Cek wajib: butir "replace gambar menghapus file LAMA" — pastikan ada assert eksplisit bahwa path lama benar-benar di-`File.Delete` (bukan hanya jadi delete-candidate). TIDAK konsolidasi kelas. TIDAK tambah tes integrasi controller-level.
- **D-03 (Setup data UAT):** Admin-UI-driven create di dalam spec — login admin → buat soal + opsi ber-gambar via form upload NYATA (`setInputFiles`) → login peserta → StartExam → Results, assert `<img>`.
- **D-04 (Fixture):** Commit 2 fixture kecil (1 JPG + 1 PNG) di `tests/fixtures/` (nama/path = discretion). xUnit tetap pakai byte-array in-memory; fixture file hanya untuk Playwright upload.
- **D-05 (Guardrail Seed Workflow WAJIB):** Spec memutasi DB + tulis file fisik ke `wwwroot/uploads/questions/{packageId}/`. Wajib: snapshot/restore DB (pola BACKUP→RESTORE) + cleanup file upload yang dibuat + entry `docs/SEED_JOURNAL.md`. Tak boleh ada seed/file nyangkut.
- **D-06 (Regression):** Rerun suite existing + 1 assert eksplisit "soal **tanpa** gambar → tidak render `<img>`".

### Claude's Discretion
- Nama persis file spec & fixture, path fixture, reuse helper existing (`examTypes.ts`/`wizardSelectors.ts`) atau buat helper kecil baru.
- Spec membuat assessment penuh via wizard ATAU pakai paket dasar minimal/existing lalu upload gambar via UI. Syarat tetap: upload admin diuji live (D-03) + guardrail snapshot/restore (D-05).
- Bentuk assert `<img>` (src non-null + `img-fluid` + `loading=lazy` + `alt` + trigger lightbox) — minimal: img tampil + responsif.
- Surface admin essay (`AssessmentMonitoringDetail` RND-05) + `EditPesertaAnswers` (RND-06) di-UAT atau tidak — **opsional**; fokus SC#2 = StartExam + Results.

### Deferred Ideas (OUT OF SCOPE)
- Tes integrasi controller-level (CreateQuestion/EditQuestion HTTP-path → ImagePath persist DB) — ditolak (kecuali gap-audit temukan butir SC#1 tak tercover).
- Konsolidasi/refactor 3 file tes jadi 1 kelas image — ditolak.
- UAT MCP-only sebagai deliverable — ditolak (spec committed).
- UAT essay-monitoring (RND-05) + EditPesertaAnswers (RND-06) dalam spec — opsional/nice-to-have.

### Locked dari ROADMAP/Spec (jangan re-decide)
- **L-01 (SC#2 alur):** admin upload gambar soal+opsi → simpan → peserta StartExam lihat gambar soal+opsi (responsif) → peserta Results lihat gambar soal+opsi.
- **L-02 (SC#1 butir):** upload valid (JPG/PNG tersimpan) + invalid (ditolak via magic-byte) + SyncPackagesToPost salin ImagePath/ImageAlt Pre→Post + DeleteQuestion hapus file gambar soal+opsi (post-commit) + replace hapus file lama.
- **L-03 (SC#3 gate):** `dotnet build` 0 error + `dotnet test` hijau + UAT di `localhost:5277` + tanpa regresi flow ujian existing (MC/MA/Essay tanpa gambar tetap normal).
- **L-04 (no migration):** Phase 355 tidak ada migration.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TST-01 | xUnit cover upload valid (JPG/PNG tersimpan) + invalid (ditolak magic-byte) + SyncPackagesToPost salin ImagePath + DeleteQuestion hapus file gambar | 3 file tes existing memetakan ~semua butir (lihat **xUnit Gap Audit**). Gap tunggal teridentifikasi: "replace hapus file LAMA" belum di-assert end-to-end (path lama benar di-`File.Delete`) dalam satu tes. |
| TST-02 | Playwright UAT admin upload gambar soal+opsi → peserta StartExam → peserta Results | Selector form upload + render `<img>` + flow login/exam helper semua tersedia & terdokumentasi di bawah. Pola seed guardrail per-spec (`cmp-records-351.spec.ts`) langsung dipakai. |
</phase_requirements>

## Summary

Phase 355 = (a) **gap-audit** 3 file tes xUnit gambar yang sudah ada + pastikan `dotnet test` hijau, dan (b) tulis **satu spec Playwright committed** end-to-end yang upload gambar via form admin NYATA lalu memverifikasi render di StartExam + Results, dengan guardrail Seed Workflow (BACKUP→test→RESTORE + cleanup file `wwwroot/uploads/questions/` + entry SEED_JOURNAL).

Net-baru terbesar = spec Playwright. Infrastruktur sudah lengkap: helper `examTypes.ts` (`createAssessmentViaWizard`/`createDefaultPackage`/`addQuestionViaForm`/`submitExamTwoStep`), helper `auth.ts` (`login(page,'admin'|'hc'|'coachee')`), helper `dbSnapshot.ts` (BACKUP/RESTORE/queryScalar), dan pola per-spec `beforeAll`/`afterAll` snapshot dari `cmp-records-351.spec.ts`. Yang KURANG dari helper existing: `addQuestionViaForm` **tidak** meng-upload gambar — Phase 355 harus extend helper itu (tambah opsi `setInputFiles`) atau tulis create-question inline di spec.

xUnit: 3 file (`FileUploadHelperTests.cs`, `PackageImageSyncTests.cs`, `PackageImageDeleteTests.cs`) menutup hampir semua butir SC#1. **Satu gap konkret:** tes replace (`ReplaceConflict_NewFileWins_OverRemoveCheckbox`) hanya membuktikan path lama jadi *delete-candidate* (`deleteList`), TIDAK menulis file lama nyata lalu assert `File.Exists==false` setelah delete loop. Tambah 1 tes `Replace_DeletesOldFile_OnDisk` (tulis file lama → ApplyIntent file-baru → jalankan DeleteIfUnreferenced → assert file lama hilang, file baru tetap).

**Primary recommendation:** Buat `tests/e2e/image-in-assessment.spec.ts` dengan pola `beforeAll/afterAll` snapshot **per-spec** (BUKAN edit `global.setup.ts` yang hardcoded untuk matrix Phase 315). Extend `addQuestionViaForm` di `examTypes.ts` agar terima image fixture path. Tambah 1 tes xUnit replace-delete-on-disk. Commit `tests/fixtures/q-image.jpg` + `tests/fixtures/opt-image.png`. Gate: `dotnet build` + `dotnet test` + `cd tests; npx playwright test image-in-assessment.spec.ts` dengan app jalan di localhost:5277.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Validasi file (magic-byte, ext, size) | Backend (FileUploadHelper) | — | Sudah dicover unit; xUnit murni in-memory byte array. |
| Save/delete file fisik | Backend (AssessmentAdminController + FileUploadHelper) | Filesystem `wwwroot/uploads/questions/{pkgId}/` | Diuji unit (logic-mirror) + E2E (file nyata via upload). |
| Render `<img>` responsif | Frontend Server (Razor partial `_QuestionImage`) | Browser (lightbox JS) | Lesson 354: Razor dynamic WAJIB Playwright runtime — build/grep tak cukup. |
| Login + create assessment + exam flow | Frontend Server (MVC) | Browser (SignalR autosave) | E2E only — helper Playwright existing. |
| DB mutation guardrail | Test infra (dbSnapshot.ts + sqlcmd) | Filesystem (.bak + uploads cleanup) | Seed Workflow per CLAUDE.md; DB via RESTORE, file via manual cleanup. |

## Standard Stack (test tooling — sudah terpasang, no install baru)

### Core
| Tool | Version | Purpose | Sumber |
|------|---------|---------|--------|
| @playwright/test | ^1.58.2 | E2E UAT runner | `tests/package.json` [VERIFIED: file] |
| xUnit (net8.0) | (existing) | Unit test runner | `HcPortal.Tests/HcPortal.Tests.csproj` [VERIFIED: glob] |
| .NET SDK | 8.0.418 | build + test | `dotnet --version` [VERIFIED: bash] |
| sqlcmd | 17.0.1000.7 | BACKUP/RESTORE DB lokal | `command -v sqlcmd` [VERIFIED: bash] — path `C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\sqlcmd` |
| exceljs | ^4.4.0 | (existing dep, tidak relevan 355) | `tests/package.json` |

### Supporting (helper TS existing — reuse)
| Helper | Path | Fungsi yang dipakai Phase 355 |
|--------|------|-------------------------------|
| auth | `tests/helpers/auth.ts` | `login(page, 'admin'\|'hc'\|'coachee')` — fill `input[name="email"]`/`input[name="password"]`, click `button[type="submit"]`, wait `**/Home/**` |
| accounts | `tests/helpers/accounts.ts` | `admin@pertamina.com` / `123456` (key `admin`); `rino.prasetyo@pertamina.com` (key `coachee`); HC `meylisa.tjiang@pertamina.com` (key `hc`) — semua pwd `123456` |
| utils | `tests/helpers/utils.ts` | `uniqueTitle(prefix)`, `today()`, `autoConfirm(page)` |
| examTypes | `tests/e2e/helpers/examTypes.ts` | `createAssessmentViaWizard`, `createDefaultPackage`, `addQuestionViaForm` (**perlu extend untuk gambar**), `submitExamTwoStep` |
| wizardSelectors | `tests/e2e/helpers/wizardSelectors.ts` | `wizardSelectors`, `questionFormSelectors` (selector form soal) |
| dbSnapshot | `tests/helpers/dbSnapshot.ts` | `backup(path)`, `restore(path)`, `execScript(path)`, `queryScalar(sql)`, `queryString(sql)` |

**No `npm install` / no NuGet baru.** Semua sudah ada.

**Version verification:** Playwright 1.58.2 (`tests/package.json` devDependencies) [VERIFIED: file]. Tidak ada package baru yang perlu diverifikasi registry — Phase 355 tidak menambah dependency.

## Architecture Patterns

### System Architecture Diagram (alur UAT spec)

```
[Playwright runner: cd tests; npx playwright test image-in-assessment.spec.ts]
        |
        v
  beforeAll: db.queryString(InstanceDefaultBackupPath) -> db.backup(.bak)   [snapshot]
        |   (capturedUploadFiles[] = [] — diisi saat upload utk cleanup)
        v
  test 1 (admin): login('admin') -> createAssessmentViaWizard -> createDefaultPackage(pkgId)
        |          -> addQuestionViaForm(+image fixtures via setInputFiles)
        |             POST /Admin/CreateQuestion (multipart) -> SaveFileAsync -> wwwroot/uploads/questions/{pkgId}/...
        |             (record pkgId utk cleanup folder)
        v
  test 2 (peserta): login('coachee') -> /CMP/Assessment -> .btn-start-standard (accept dialog)
        |            -> waitForURL **/CMP/StartExam/** 
        |            assert: img.question-image-zoom visible (soal Cap=240 + opsi Cap=120)
        |            assert: klik gambar opsi -> #imageLightboxModal show + radio TIDAK ke-toggle (bug 926a57e1)
        |            -> submitExamTwoStep -> waitForURL **/CMP/Results/**
        |            assert: img.question-image-zoom visible di Tinjauan Jawaban (soal + opsi)
        v
  test 3 (D-06 regresi): soal TANPA gambar -> card-nya TIDAK ada img.question-image-zoom
        |
        v
  afterAll: db.restore(.bak) -> assert Layer 4 (0 row sisa) -> hapus folder wwwroot/uploads/questions/{pkgId}
            -> (global.teardown atau journal regex active->cleaned) -> SEED_JOURNAL status cleaned
```

### Component Responsibilities (file → peran)

| File | Peran di Phase 355 |
|------|--------------------|
| `tests/e2e/image-in-assessment.spec.ts` (BARU) | Deliverable utama TST-02. `beforeAll/afterAll` snapshot + 3 test. |
| `tests/e2e/helpers/examTypes.ts` (EDIT) | Extend `addQuestionViaForm` agar terima `images?: {question?, optionA..D?}` → `setInputFiles`. |
| `tests/fixtures/*.jpg`/`*.png` (BARU) | 2 fixture upload (D-04). |
| `HcPortal.Tests/PackageImageDeleteTests.cs` (EDIT, +1 tes) | Tutup gap "replace hapus file LAMA on disk" (D-02). |
| `docs/SEED_JOURNAL.md` (APPEND) | Entry guardrail (D-05). |

### Pattern 1: Per-spec snapshot guardrail (PILIH INI, bukan global.setup)
**What:** Tiap spec yang memutasi DB melakukan BACKUP di `beforeAll` + RESTORE + Layer-4 assert di `afterAll`. Self-contained, tidak bergantung matrix seed.
**When to use:** Phase 355 (memutasi DB + file). **Sumber persis:** `tests/e2e/cmp-records-351.spec.ts:44-70` [VERIFIED: file].
```typescript
// Source: tests/e2e/cmp-records-351.spec.ts:44-70
let snapshotPath: string;
test.beforeAll(async () => {
  const dir = (await db.queryString(
    "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
  )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre355-${ts}.bak`;
  await db.backup(snapshotPath);
});
test.afterAll(async () => {
  if (!snapshotPath) return;
  let restoreError: unknown = null;
  try {
    await db.restore(snapshotPath);
    const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch {}
  } catch (e) { restoreError = e; }
  // ... + cleanup file upload (lihat Pattern 3) ...
  if (restoreError) throw restoreError;
});
```
> CRITICAL: `C:\Temp\` DIBLOKIR oleh service account `MSSQL$SQLEXPRESS` — WAJIB resolve `SERVERPROPERTY('InstanceDefaultBackupPath')` (default dir, mis. `C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\`). [VERIFIED: global.setup.ts:43-52 + dbSnapshot.ts comment + SEED_JOURNAL contoh].

### Pattern 2: Upload gambar via form admin (D-03 setInputFiles)
**What:** File input gambar di form `#questionForm` adalah `hidden` (`accept="image/png,image/jpeg"`). Playwright `setInputFiles` bekerja pada input hidden tanpa perlu klik tombol "Pilih".
**When to use:** create soal ber-gambar dalam spec. **Sumber selector:** `Views/Admin/ManagePackageQuestions.cshtml:145-211` [VERIFIED: file].
```typescript
// Setelah goto /Admin/ManagePackageQuestions?packageId={pkgId} + isi teks/opsi seperti addQuestionViaForm
await page.setInputFiles('#questionImgField', 'tests/fixtures/q-image.jpg');   // gambar soal
await page.fill('#questionImageAlt', 'diagram pompa');
await page.setInputFiles('#optAImgField', 'tests/fixtures/opt-image.png');     // gambar opsi A
await page.fill('#optAImageAlt', 'opsi impeller');
await page.locator('#submitBtn').click();
await page.waitForLoadState('networkidle');
await expect(page.locator('.alert-success').first()).toBeVisible({ timeout: 5_000 });
```
> Path fixture RELATIF cwd Playwright. `cmp-records-351.spec.ts` pakai `path.resolve(__dirname, '../sql/...')` untuk seed; untuk fixture lebih aman `path.resolve(__dirname, '../fixtures/q-image.jpg')` (cwd bisa `tests/` saat `cd tests`).

### Pattern 3: Cleanup file upload (D-05 — TIDAK ter-cover oleh DB RESTORE)
**What:** RESTORE DB membalikkan baris, TAPI file fisik di `wwwroot/uploads/questions/{pkgId}/` tetap nyangkut. Harus dihapus manual di `afterAll`.
**Sumber path:** `FileUploadHelper.SaveFileAsync` → `$"uploads/questions/{packageId}"` (controller `AssessmentAdminController.cs:6172,6202,6392,6535`). Folder belum ada sampai upload pertama (verified: glob `wwwroot/uploads/questions/**` → no files).
```typescript
// afterAll, setelah db.restore — hapus folder package yang dibuat spec
const fs = await import('node:fs');
const p = await import('node:path');
const dir = p.resolve(__dirname, '../../wwwroot/uploads/questions', String(createdPackageId));
try { fs.rmSync(dir, { recursive: true, force: true }); } catch {}
```
> Alternatif lebih aman: rekam tiap URL yang dibuat (dari thumbnail src atau dari _QuestionImage `src` di StartExam) lalu hapus per file. Tapi karena `pkgId` unik per run dan folder per-package, `rmSync` folder `{pkgId}` cukup & bersih.

### Anti-Patterns to Avoid
- **JANGAN edit `tests/e2e/global.setup.ts`/`global.teardown.ts`.** Keduanya HARDCODED untuk matrix test Phase 315 (seed 18 sessions, Layer-1 expect 18, journal "matrix" regex). Menambah logic image di sana akan: (a) tetap menjalankan matrix seed tiap run, (b) merusak Layer-1/Layer-4 assertion. Pakai per-spec `beforeAll/afterAll` (Pattern 1). [VERIFIED: global.setup.ts:76-104 hardcode 18/10/30/80].
- **JANGAN andalkan DB RESTORE untuk membersihkan file upload.** File fisik bukan bagian DB (Pattern 3).
- **JANGAN tulis ulang 3 file tes xUnit** (D-02) — hanya tambah 1 tes gap.
- **JANGAN pakai positional `.nth()` untuk option setelah render StartExam** — StartExam SHUFFLE opsi per-question (`StartExam.cshtml:125-128` + `CMPController BuildCrossPackageAssignment`). Untuk assert gambar opsi, scope by `img.question-image-zoom` di dalam qcard, atau match by `alt`/`data-img-alt`, BUKAN posisi.

## Concrete Selectors & Targets

### A. Admin upload form (`Views/Admin/ManagePackageQuestions.cshtml`) [VERIFIED: file:line]
Form: `<form id="questionForm" asp-action="CreateQuestion" enctype="multipart/form-data">` (L122). Edit mode swap action ke `/Admin/EditQuestion` via JS (L452). Submit btn `#submitBtn` (L247).

| Field | Selector (id) | name attr | Catatan |
|-------|---------------|-----------|---------|
| Tipe soal | `#QuestionType` | `questionType` | option `MultipleChoice`/`MultipleAnswer`/`Essay` (L130-134) |
| Teks soal | `#questionText` | `questionText` | textarea, required (L140) |
| **Gambar soal (file)** | `#questionImgField` | `questionImage` | **hidden**, `accept="image/png,image/jpeg"` (L155) |
| Alt soal | `#questionImageAlt` | `questionImageAlt` | text (L165) |
| Hapus gambar soal | `#removeQuestionImage` | `removeQuestionImage` | checkbox value=true, EDIT-only (L162) |
| Opsi teks A-D | `#option_A`..`#option_D` | `optionA`..`optionD` | (L188) |
| Correct A-D | `#correct_A`..`#correct_D` | `correctA`..`correctD` | radio (MC) / checkbox (MA via JS) (L182) |
| **Gambar opsi A-D (file)** | `#optAImgField`..`#optDImgField` | `optionAImage`..`optionDImage` | **hidden**, accept image (L201) |
| Alt opsi A-D | `#optAImageAlt`..`#optDImageAlt` | `optionAImageAlt`..`optionDImageAlt` | text (L198) |
| Hapus gambar opsi | `#removeOptionAImage`.. | `removeOptionAImage`.. | checkbox EDIT-only (L207) |
| Skor | `#scoreValue` | `scoreValue` | number (L232) |
| Submit | `#submitBtn` | — | (L247) |

> Sudah ada di `questionFormSelectors` (`wizardSelectors.ts:103-121`): `optionA..D`, `correctA..D`, dst. **TAPI selector gambar (`questionImgField`/`optAImgField`/alt/remove) BELUM ada di sana** — tambahkan saat extend helper.

### B. Render `<img>` (`Views/Shared/_QuestionImage.cshtml`) [VERIFIED: file]
Partial dipakai 6 surface. Markup `<img>` (render HANYA jika ImagePath non-null, L32 — ini guard D-06/L-02):
```html
<img src="@imagePath" alt="@imageAlt"
     class="img-fluid rounded border mb-3 question-image-zoom[ d-block w-100 mt-2 mb-0 jika opsi]"
     style="max-height:{cap}px; cursor:pointer" loading="lazy"
     role="button" tabindex="0" onclick="event.preventDefault();"
     data-bs-toggle="modal" data-bs-target="#imageLightboxModal"
     data-img-src="@imagePath" data-img-alt="@imageAlt" aria-label="..." />
```
**Selector assert (rekomendasi):**
- Gambar ADA: `page.locator('img.question-image-zoom')` (`.count() > 0` / `toBeVisible`).
- Responsif: assert class `img-fluid` + attr `loading=lazy` (`await img.getAttribute('class')` contains `img-fluid`; `await img.getAttribute('loading')` === `lazy`).
- src non-null: `await img.getAttribute('src')` truthy + match `/uploads/questions/`.
- Soal vs opsi: soal `Cap=240` → `max-height:240px`; opsi `Cap=120` + class tambahan `d-block w-100 mt-2 mb-0`.
- Lightbox trigger: `data-bs-target="#imageLightboxModal"` + `data-img-src`.

**Lightbox modal** (`Views/Shared/_ImageLightboxModal.cshtml`): id `#imageLightboxModal`, img dalamnya `#imageLightboxImg`. Di-host di StartExam (`StartExam.cshtml:401`), Results (`Results.cshtml:419`), ManagePackageQuestions (`:286`).

### C. Lokasi `_QuestionImage` per surface [VERIFIED: grep]
| Surface | Soal (Cap 240) | Opsi (Cap 120) | File |
|---------|----------------|----------------|------|
| StartExam | `StartExam.cshtml:105` | `:148` (MA-label), `:181` (MC-label) | `Views/CMP/StartExam.cshtml` |
| Results | `Results.cshtml:330` | `:388` | `Views/CMP/Results.cshtml` |
| (host lightbox) | StartExam:401, Results:419, ManageQ:286 | | |

### D. Peserta exam flow [VERIFIED: exam-taking.spec.ts:116-134 + examTypes.ts:223-233]
- Coachee start: `login(page,'coachee')` → `/CMP/Assessment` → `page.locator('.assessment-card', { hasText: title })` → `.btn-start-standard` → `page.once('dialog', d=>d.accept())` → click → `waitForURL('**/CMP/StartExam/**')`. Header `#examHeader`/`#examTimer` muncul.
- Submit→Results: helper `submitExamTwoStep(page)` (examTypes.ts:223): click `#reviewSubmitBtn` → waitURL ExamSummary → arm dialog accept → click `button:has-text("Kumpulkan")` → waitURL `**/CMP/Results/**`.
- Results butuh `AllowAnswerReview=true` agar section "Tinjauan Jawaban" render (`Results.cshtml:316`). `createAssessmentViaWizard` punya opsi `allowAnswerReview: true`.

### E. Toggle pitfall (bug 926a57e1) [VERIFIED: StartExam.cshtml:135-149,168-182 + _QuestionImage.cshtml:43]
Di StartExam, gambar opsi berada DI DALAM `<label class="list-group-item ...">` yang membungkus radio/checkbox. Tanpa `onclick="event.preventDefault()"`, klik gambar mengaktifkan label → toggle radio + auto-save. Partial sudah punya `onclick="event.preventDefault();"` (L43). Di Results, gambar opsi di dalam `<div class="list-group-item">` (BUKAN label, `Results.cshtml:372`) → tak ada risiko toggle.
**UAT guard (opsional tapi direkomendasikan, sesuai lesson 354):** Di StartExam, klik gambar opsi → assert `#imageLightboxModal` jadi visible **DAN** radio/checkbox opsi itu TIDAK `checked`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| BACKUP/RESTORE DB lokal | sqlcmd manual / SQL DELETE | `helpers/dbSnapshot.ts` (`backup`/`restore`/`queryScalar`) | localhost-guard + `-b` exit-code + SINGLE_USER handling sudah benar |
| Login Playwright | fill form manual tiap test | `helpers/auth.ts` `login(page, key)` | account map terpusat + wait `**/Home/**` |
| Create assessment/package/soal | klik wizard manual | `examTypes.ts` `createAssessmentViaWizard`/`createDefaultPackage`/`addQuestionViaForm` | pitfall wizard (static modal, schedDateInput, applyQTypeSwitch) sudah dimitigasi |
| Submit exam 2-step | click manual | `examTypes.ts` `submitExamTwoStep` | dialog-arm timing benar |
| Resolve backup dir | hardcode C:\Temp | `SERVERPROPERTY('InstanceDefaultBackupPath')` | C:\Temp diblokir service account |

**Key insight:** Satu-satunya kode test net-baru yang BENAR-BENAR baru = (1) extend `addQuestionViaForm` untuk image, (2) assert `<img>` di StartExam/Results, (3) cleanup folder upload, (4) 1 tes xUnit replace-delete. Selebihnya komposisi helper existing.

## xUnit Gap Audit (D-02 — pemetaan SC#1 → tes existing)

3 file: `HcPortal.Tests/FileUploadHelperTests.cs`, `PackageImageSyncTests.cs`, `PackageImageDeleteTests.cs` [semua VERIFIED: dibaca penuh].

| SC#1 / L-02 butir | Tes existing | Status | Catatan |
|-------------------|--------------|--------|---------|
| Upload VALID JPG tersimpan | `ValidateImageFile_ValidJpg_ReturnsValid` (FUHT:124) | ✅ COVER | byte-array magic-byte in-memory |
| Upload VALID PNG | `ValidateImageFile_ValidPng_ReturnsValid` (FUHT:132) | ✅ COVER | |
| Upload VALID JPEG (EXIF) | `ValidateImageFile_ValidJpeg_ReturnsValid` (FUHT:140) | ✅ COVER | |
| INVALID non-image ditolak (PDF) | `ValidateImageFile_Pdf_ReturnsInvalidExtension` (FUHT:148) | ✅ COVER | |
| INVALID exe-renamed (magic-byte) | `ValidateImageFile_ExeRenamedPng_ReturnsInvalidMagicByte` (FUHT:157) | ✅ COVER | inti IMG-04 |
| INVALID oversize (>5MB) | `ValidateImageFile_Oversize_ReturnsInvalid` (FUHT:166) | ✅ COVER | |
| (bonus) null/empty file | `ValidateImageFile_NullFile/EmptyFile` (FUHT:108,116) | ✅ COVER | |
| SyncPackagesToPost salin ImagePath soal Pre→Post | `SyncCopiesQuestionImagePath` (Sync:40) | ⚠️ COVER (logic-mirror) | TIDAK panggil method asli; `Clone()` mirror blok `AssessmentAdminController.cs ~L5370`. Komentar header minta "keep in sync". Acceptable per D-02 (no integration test). |
| SyncPackagesToPost salin ImageAlt opsi | `SyncCopiesOptionImageAlt` (Sync:56) | ⚠️ COVER (logic-mirror) | |
| Shared-file (path identik, no dup file) | `SyncSharesSamePath_NoFileDuplication` (Sync:77, `Assert.Same`) | ✅ COVER | SYN-01 invariant |
| Sync null-safe | `SyncHandlesNullImagePath` (Sync:99) | ✅ COVER | |
| DeleteQuestion hapus file (orphan) | `RefCount_Deletes_WhenNoOtherRowSharesPath` (Del:67) | ✅ COVER | tulis file nyata + assert `File.Exists==false` |
| DeleteQuestion SKIP file shared | `RefCount_SkipsDelete_WhenPathSharedByOtherRow` (Del:41) | ✅ COVER | D-10 ref-count |
| DeletePackage collect semua path | `DeletePackageImage_CollectsAllNonNullPaths` (Del:90) | ✅ COVER | |
| DeletePackage skip-shared + delete-orphan | `RefCount_DeletePackage_SkipsShared_DeletesOrphan` (Del:153) | ✅ COVER | tulis file nyata |
| Opsi untouched preserve gambar (OQ1) | `OptionPreserve_KeepsImagePath_WhenOptionUntouched` (Del:203) | ✅ COVER | |
| **Replace: file baru menang + path lama jadi delete-candidate** | `ReplaceConflict_NewFileWins_OverRemoveCheckbox` (Del:187) | ✅ COVER (partial) | assert `deleteList` contains old path + path baru menang |
| **Replace: file LAMA benar-benar DI-DELETE on disk** | — | ❌ **GAP** | Tidak ada satu tes yang: tulis file lama nyata → ApplyIntent file-baru → jalankan `DeleteIfUnreferenced(oldPath)` → assert old file `File.Exists==false` + new file ada. |

### Gap konkret (satu-satunya) — D-02 wajib
`ReplaceConflict_NewFileWins_OverRemoveCheckbox` (Del:187-201) HANYA membuktikan `target.ImagePath == new` + `deleteList.Contains(old)`. Ia TIDAK menulis file lama nyata, TIDAK menjalankan delete loop, TIDAK assert `File.Exists(old)==false`. Tahap "delete-candidate → benar di-`File.Delete`" memang tertutup oleh `RefCount_Deletes_WhenNoOtherRowSharesPath` (Del:67) tapi sebagai tes TERPISAH dengan path generik, bukan dirantai dari skenario replace.

**Produksi nyata** (`AssessmentAdminController.cs:6386-6525`, EditQuestion) [VERIFIED]: kumpul `imagePathsToDelete` saat `questionImage != null` (L6393) / `ApplyOptionImageIntent` (L6536) → SETELAH SaveChanges + auto-sync → ref-count `AnyAsync(ImagePath==relUrl)` (L6513-6515) → `File.Delete` (L6519). Jadi alurnya nyata; tes hanya kurang merantainya end-to-end.

**Rekomendasi tambah 1 tes** (di `PackageImageDeleteTests.cs`, pakai helper `MakeTempDir`/`ApplyIntent`/`DeleteIfUnreferenced` yang SUDAH ada di file itu):
```csharp
[Fact]
public void Replace_NewFileWins_DeletesOldFileOnDisk()
{
    var dir = MakeTempDir();
    try
    {
        var oldPath = Path.Combine(dir, "old.jpg");
        var newPath = Path.Combine(dir, "new.jpg");
        File.WriteAllBytes(oldPath, new byte[] { 1 });
        File.WriteAllBytes(newPath, new byte[] { 2 });

        var target = new PackageOption { OptionText = "A", IsCorrect = true, ImagePath = oldPath };
        var deleteList = new List<string>();
        ApplyIntent(target, newFilePresent: true, savedNewPath: newPath,
            alt: "baru", removeChecked: false, deleteList);

        // Tidak ada baris lain memuat oldPath → harus dihapus on disk.
        var remainingQ = new List<PackageQuestion>();
        var remainingO = new List<PackageOption> { target }; // target kini menunjuk newPath
        foreach (var p in deleteList.Distinct())
            DeleteIfUnreferenced(p, remainingQ, remainingO);

        Assert.False(File.Exists(oldPath), "file LAMA harus terhapus on disk (SYN-02 replace).");
        Assert.True(File.Exists(newPath), "file BARU harus tetap ada.");
        Assert.Equal(newPath, target.ImagePath);
    }
    finally { Directory.Delete(dir, recursive: true); }
}
```
> Ini tetap "logic-mirror" (konsisten dengan gaya 3 file existing) — BUKAN integration test (Deferred). Memenuhi D-02 ("tambah 1 tes" bila gap nyata).

### Harness pattern xUnit (untuk planner)
- IFormFile fake: `FormFile(new MemoryStream(bytes), 0, len, "file", fileName)` + `Headers = new HeaderDictionary()` (FUHT:19-27).
- Magic-byte payloads: JPG `FF D8 FF E0`, PNG `89 50 4E 47 0D 0A 1A 0A`, PDF `25 50 44 46`, exe `4D 5A 90 00` (FUHT contoh).
- Temp dir: `Path.Combine(Path.GetTempPath(), "hcportal-test-"+Guid)` (FUHT:41 / Del:20).
- ILogger fake: `TestLogger` capture List<(LogLevel,Message)> (FUHT:30-39).
- Sync/Delete tes: **pure in-memory object**, TIDAK pakai DbContext (komentar Sync:8 + Del:7) — `Clone()`/`ApplyIntent()`/`DeleteIfUnreferenced()` lokal mirror controller.

## Common Pitfalls

### Pitfall 1: Mengedit global.setup.ts (matrix hardcode)
**What goes wrong:** Menambah seed image ke `global.setup.ts` → matrix seed tetap jalan + Layer-1 assert 18 sessions gagal / waktu run melonjak.
**Why:** `global.setup.ts` adalah dedicated matrix Phase 315 (L76-104 hardcode 18/10/30/80).
**Avoid:** Pakai per-spec `beforeAll/afterAll` (Pattern 1). [HIGH]

### Pitfall 2: File upload tidak dibersihkan (D-05)
**What goes wrong:** DB di-RESTORE tapi `wwwroot/uploads/questions/{pkgId}/*.jpg` tetap ada → "file nyangkut" melanggar Seed Workflow.
**Avoid:** `fs.rmSync(uploadsDir/{pkgId}, {recursive:true})` di `afterAll` (Pattern 3). Verifikasi folder bersih.

### Pitfall 3: Razor dynamic — build/grep tak deteksi (lesson 354)
**What goes wrong:** `_QuestionImage` adalah `@model object` reflection-based; bug runtime (RuntimeBinderException 738217cc, label-toggle 926a57e1) LOLOS build + grep, hanya ketahuan runtime.
**Avoid:** UAT WAJIB jalankan render NYATA di browser (Playwright) — itu inti TST-02. Sertakan guard toggle (klik gambar opsi StartExam → lightbox open + radio NOT checked).

### Pitfall 4: Positional option selector setelah shuffle
**What goes wrong:** StartExam shuffle opsi (`StartExam.cshtml:125-128`) → `.nth(i)` salah memetakan gambar↔opsi.
**Avoid:** Assert `img.question-image-zoom` by count/visibility atau by `alt`/`data-img-alt`, bukan posisi.

### Pitfall 5: SignalR autosave timing di StartExam
**What goes wrong:** Submit ditembak sebelum jawaban tersimpan → ExamSummary `unanswered>0` → submit disabled (didokumentasikan panjang di examTypes.ts:304-329). Untuk Phase 355 FOKUS render `<img>`, BUKAN grading — tidak perlu submit kalau hanya assert render di StartExam. Untuk Results perlu submit; isi minimal 1 jawaban valid + pakai `submitExamTwoStep`. Pertimbangkan opsi assert Results via review section setelah submit cepat (boleh jawab sebagian; section Tinjauan render bila `AllowAnswerReview=true`).
**Avoid:** Reuse `submitExamTwoStep` + (bila ada opsi) helper `checkMAOptionsForQuestion`/radio check sebelum submit. Pertimbangkan soal Essay agar tak perlu select opsi (tapi Essay tak punya gambar opsi → untuk assert gambar opsi pakai MC, jawab 1 radio dulu).

### Pitfall 6: Backup path C:\Temp diblokir
**What goes wrong:** `db.backup('C:/Temp/...bak')` → permission denied service account.
**Avoid:** resolve `InstanceDefaultBackupPath` (Pattern 1). [VERIFIED: global.setup.ts:43-52].

## Code Examples (command strings)

### Build + test (gate L-03) — jalankan dari worktree root
```bash
# Build solusi (0 error wajib)
dotnet build HcPortal.sln
# atau test project saja:
dotnet test HcPortal.Tests/HcPortal.Tests.csproj
# Jalankan 1 kelas tes saja saat dev:
dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~PackageImageDeleteTests"
```
> .NET 8.0.418 [VERIFIED]. Test framework xUnit (existing). Quick-run < 30s (tes image murni in-memory/temp-dir).

### Playwright UAT (gate L-03) — app HARUS jalan di localhost:5277
```bash
# Terminal 1 (worktree root): jalankan app
dotnet run
# (cek output port — default 5277 HTTP; PathBase /KPB-PortalHC tapi baseURL playwright = http://localhost:5277)
# Terminal 2:
cd tests
npx playwright test image-in-assessment.spec.ts
# headed saat debug:
npx playwright test image-in-assessment.spec.ts --headed
```
> `tests/package.json` scripts: `npm test` = `npx playwright test`. baseURL `http://localhost:5277` (playwright.config.ts:12). Project `chromium` depends `setup` — setup project (global.setup matrix) AKAN jalan kecuali kamu pakai `--project=chromium` saja TANPA dependency... NOTE: `chromium` declares `dependencies: ['setup']` (config L31) → matrix setup ikut jalan untuk SEMUA chromium test. **Implikasi planner:** jalankan spec image akan men-trigger global.setup (matrix BACKUP+seed) + global.teardown (matrix RESTORE) DI LUAR spec. Spec image sendiri lalu butuh `beforeAll/afterAll` snapshot-nya sendiri (nested) — aman, karena restore terakhir (global.teardown) mengembalikan ke snapshot pre-matrix yang berisi state pre-image juga? TIDAK — urutan: globalSetup backup(A)→matrix seed; spec beforeAll backup(B = A+matrix); spec afterAll restore(B); globalTeardown restore(A). Net: bersih. Lihat **Open Question 1** untuk opsi menghindari matrix overhead.

### SEED_JOURNAL entry (D-05) — append 1 baris (status diawali active, jadi cleaned post-restore)
Format kolom (dari `docs/SEED_JOURNAL.md`): `| Tanggal | Phase | Klasifikasi | Tujuan | Dampak (entitas tersentuh) | Snapshot file | Status |`
```markdown
| 2026-06-09 | 355 (test-uat gambar) | temporary + local-only | UAT TST-02 image-in-assessment.spec — admin upload gambar soal+opsi (setInputFiles fixture) → peserta StartExam+Results render <img> + guard toggle. Cleanup file wwwroot/uploads/questions/{pkgId}. | AssessmentSessions(+1), AssessmentPackages(+1), PackageQuestions(+N), PackageOptions(+~4N), file wwwroot/uploads/questions/{pkgId}/*.jpg|png (semua revert via RESTORE + rmSync) | {InstanceDefaultBackupPath}/HcPortalDB_Dev-pre355-{ts}.bak | cleaned |
```
> Contoh persis fitur ini sudah ada (Phase 354, SEED_JOURNAL.md:9). Bila spec append journal otomatis (pola global.setup), pakai regex active→cleaned di teardown; bila manual, isi langsung `cleaned` setelah RESTORE sukses.

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| Old exam-taking spec pakai `/Admin/ManageQuestions?id=` + `name="question_text"`/`name="options"` (exam-taking.spec.ts:81-90) | Form aktual = `/Admin/ManagePackageQuestions?packageId=` + `#questionText`/`#option_A` (Phase 353) | Gunakan `addQuestionViaForm` (examTypes.ts) yang sudah pakai route+selector BENAR; JANGAN tiru `exam-taking.spec.ts` A3 (route lama, kemungkinan stale untuk soal teks lama). |
| `addQuestionViaForm` tanpa gambar | perlu extend `images?` param | net-baru kecil di helper |

**Tidak ada deprecation relevan.** Playwright 1.58.2 current.

## Runtime State Inventory

> Phase 355 BUKAN rename/refactor, TAPI menulis file fisik + DB rows yang harus dibersihkan (Seed Workflow). Inventory cleanup:

| Category | Items | Action Required |
|----------|-------|-----------------|
| Stored data (DB) | AssessmentSessions/Packages/Questions/Options + UserPackageAssignment/Responses (lazy saat StartExam) yang dibuat spec | RESTORE DB dari snapshot pre-355 (`afterAll`) + Layer-4 assert 0 sisa |
| Live service config | None — tak ada n8n/Datadog/scheduler tersentuh | None |
| OS-registered state | None | None |
| Secrets/env vars | None | None |
| Build artifacts | None (no migration, no package rename) | None |
| **File upload (NON-DB)** | `wwwroot/uploads/questions/{pkgId}/*.jpg\|png` ditulis `SaveFileAsync` | `fs.rmSync` folder `{pkgId}` di `afterAll` — TIDAK ter-cover RESTORE |
| Snapshot .bak | `{InstanceDefaultBackupPath}/HcPortalDB_Dev-pre355-{ts}.bak` | `fs.unlinkSync` best-effort post-restore |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Coachee `rino.prasetyo@pertamina.com` punya akses & melihat assessment yang baru dibuat HC di `/CMP/Assessment` (peserta dipilih saat wizard) | Peserta flow | Jika peserta tidak ter-assign, StartExam tak reachable. Mitigasi: pilih `rino.prasetyo` di wizard Step 2 (examTypes.ts default) + verifikasi card muncul. [pola sama exam-taking.spec.ts:116-119 — historically works] |
| A2 | Membuat soal MC dengan gambar opsi lalu peserta StartExam: butuh select 1 radio sebelum submit untuk capai Results | Pitfall 5 | Jika tidak, ExamSummary blokir submit. Untuk assert render di StartExam saja (sebelum submit), tak perlu jawab — assert `<img>` langsung di StartExam load. Results perlu jawab minimal. |
| A3 | `dotnet test` saat ini hijau (3 file image tes GREEN, bukan Skip) | xUnit gate | Memory project_354 klaim "test 112/112" + project_353 "130 test". Asumsi suite hijau pre-355. Verifikasi `dotnet test` di Wave 0. |
| A4 | Project `chromium` dependency `['setup']` memaksa global.setup (matrix) jalan untuk spec image juga | Command/Open Q1 | Bila benar → overhead matrix tiap run. Bisa dihindari (Open Q1). Tidak memblokir kebenaran, hanya kecepatan + DB churn ekstra. |

## Open Questions

1. **Apakah jalankan spec image memicu global.setup matrix (Phase 315)?**
   - Yang kita tahu: `playwright.config.ts` project `chromium` punya `dependencies: ['setup']` (L31); `setup` = `global.setup.ts` matrix seed. Default `npx playwright test image-in-assessment.spec.ts` tetap menjalankan project `setup` lebih dulu (Playwright menjalankan dependency project).
   - Yang tak jelas: apakah planner ingin menanggung overhead + DB churn matrix tiap run UAT image.
   - **Rekomendasi:** (a) Terima saja — net bersih (global.teardown RESTORE ke snapshot pre-matrix; spec image punya snapshot sendiri di dalamnya). ATAU (b) Buat project Playwright terpisah TANPA dependency setup khusus untuk spec image (mis. project `image` di config, no `dependencies`) lalu jalankan `--project=image`. Opsi (a) paling sedikit perubahan & sudah terbukti untuk spec lain (cmp-records-351 yang juga punya beforeAll sendiri berjalan di project chromium). **Default: opsi (a).**

2. **Apakah UAT meng-cover surface opsional RND-05 (AssessmentMonitoringDetail) + RND-06 (EditPesertaAnswers)?**
   - CONTEXT D-discretion: opsional. Helper `gradeSingleEssaySession` (examTypes.ts:354) bisa membawa ke AssessmentMonitoringDetail bila mau. **Rekomendasi:** SKIP di deliverable inti (fokus SC#2 StartExam+Results); planner boleh tambah sebagai test terpisah opsional bila waktu cukup. Live-verify Phase 354 sudah mencakup ManagePkgQ + Preview.

3. **Fixture gambar: ukuran & validitas magic-byte.**
   - Fixture harus JPG/PNG VALID (magic-byte benar) agar lolos `ValidateImageFile`. **Rekomendasi:** generate 2 file kecil nyata (mis. 1x1 atau ~2-10KB) — JPG header `FF D8 FF` + PNG header `89 50 4E 47`. Jangan rename .txt→.jpg (akan ditolak magic-byte, justru itu yang diuji invalid). Commit di `tests/fixtures/`. Konten visual tak relevan (partial hanya render src) — tapi file harus parseable sebagai gambar oleh browser untuk render `<img>`.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | dotnet build/test | ✓ | 8.0.418 | — |
| sqlcmd | dbSnapshot BACKUP/RESTORE | ✓ | 17.0.1000.7 | — |
| @playwright/test | E2E UAT | ✓ | ^1.58.2 (tests/node_modules) | `npx playwright install chromium` bila browser belum ter-download |
| SQL Server Express `localhost\SQLEXPRESS` DB `HcPortalDB_Dev` | DB lokal | ✓ (asumsi, per appsettings.Development.json) | — | — |
| App running localhost:5277 | Playwright | manual (dotnet run) | — | global.setup assert `/Account/Login` ok |

**Tidak ada dependency hilang yang memblokir.** Catatan: pastikan `npx playwright install` sudah dijalankan sebelumnya (browser chromium). Spec existing berjalan → kemungkinan sudah ter-install.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework (unit) | xUnit (net8.0), project `HcPortal.Tests/HcPortal.Tests.csproj` |
| Framework (e2e) | Playwright @playwright/test ^1.58.2, config `tests/playwright.config.ts` |
| Quick run (unit) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~PackageImage"` |
| Full suite (unit) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| E2E run | `cd tests; npx playwright test image-in-assessment.spec.ts` (app di localhost:5277) |

### Phase Requirements → Test Map
| Req | Behavior | Test Type | Command | File Exists? |
|-----|----------|-----------|---------|-------------|
| TST-01 | upload valid/invalid magic-byte | unit | `dotnet test --filter ValidateImageFile` | ✅ FileUploadHelperTests.cs |
| TST-01 | sync copy ImagePath/Alt Pre→Post | unit | `dotnet test --filter PackageImageSync` | ✅ PackageImageSyncTests.cs |
| TST-01 | delete file (orphan/skip-shared/collect) | unit | `dotnet test --filter PackageImageDelete` | ✅ PackageImageDeleteTests.cs |
| TST-01 | **replace hapus file LAMA on disk** | unit | (tes baru `Replace_NewFileWins_DeletesOldFileOnDisk`) | ❌ Wave 0 — tambah ke PackageImageDeleteTests.cs |
| TST-02 | admin upload soal+opsi → StartExam render | e2e | `npx playwright test image-in-assessment.spec.ts` | ❌ Wave 0 — spec baru |
| TST-02 | Results render gambar soal+opsi | e2e | (same spec) | ❌ Wave 0 |
| D-06 | soal tanpa gambar → tak render `<img>` | e2e | (same spec, test 3) | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~PackageImage"` (cepat).
- **Per wave merge:** full `dotnet test` + 1× `npx playwright test image-in-assessment.spec.ts`.
- **Phase gate:** `dotnet build HcPortal.sln` 0 error + full `dotnet test` hijau + spec image hijau + (rerun 1 spec exam existing mis. `exam-taking.spec.ts` sebagai baseline regresi D-06).

### Wave 0 Gaps
- [ ] `tests/e2e/image-in-assessment.spec.ts` — TST-02 + D-06 (BARU)
- [ ] `tests/fixtures/q-image.jpg` + `tests/fixtures/opt-image.png` — fixture upload (BARU, D-04)
- [ ] `HcPortal.Tests/PackageImageDeleteTests.cs` — +1 `[Fact]` replace-delete-on-disk (EDIT)
- [ ] `tests/e2e/helpers/examTypes.ts` — extend `addQuestionViaForm` (param image) + `wizardSelectors.ts` tambah selector gambar (EDIT)
- [ ] `docs/SEED_JOURNAL.md` — append entry Phase 355 (D-05)
- [ ] Framework install: jika `npx playwright test` gagal "browser not found" → `cd tests; npx playwright install chromium`

## Security Domain

> `security_enforcement` tidak di-set false; namun Phase 355 = test-only (no production code, no new endpoint, no migration). ASVS surface minimal.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V5 Input Validation | yes (yang DIUJI, bukan diubah) | magic-byte image-only `ValidateImageFile` (sudah ada) — Phase 355 memverifikasi, tidak mengubah |
| V12 File Upload | yes (diuji) | path-traversal strip (`SaveFileAsync` strip dir component) + ext+size+magic-byte — sudah dicover `SaveFileAsync_PathTraversalFilename_*` (FUHT:178-259) |
| V2/V3/V4/V6 | no | tak ada auth/crypto/session baru |

### Known Threat Patterns
| Pattern | STRIDE | Mitigation (existing, diverifikasi) |
|---------|--------|--------------------------------------|
| Upload non-image (exe/PDF renamed) | Tampering | magic-byte gate (diuji `ValidateImageFile_ExeRenamedPng`) |
| Path traversal filename | Tampering | strip + LogWarning (diuji `SaveFileAsync_PathTraversalFilename_StripsToFlatName`) |
| Test creds leak | Info Disclosure | dev creds local-only `123456`, JANGAN dipakai staging/prod (reference_dev_credentials) |
| sqlcmd target non-localhost | Tampering | `dbSnapshot.runSqlcmd` REJECT non-localhost `-S` (dbSnapshot.ts:39-44) |

## Sources

### Primary (HIGH confidence — dibaca penuh sesi ini)
- `Views/Admin/ManagePackageQuestions.cshtml` (form upload, L122-255) — selector
- `Views/Shared/_QuestionImage.cshtml` + `_ImageLightboxModal.cshtml` — markup render
- `Views/CMP/StartExam.cshtml` (L95-188,401) + `Views/CMP/Results.cshtml` (L315-419) — target assert + toggle context
- `HcPortal.Tests/FileUploadHelperTests.cs` / `PackageImageSyncTests.cs` / `PackageImageDeleteTests.cs` — gap audit
- `Controllers/AssessmentAdminController.cs` (L6172-6536 EditQuestion/Apply/Delete; grep DeleteQuestion/File.Delete) — produksi nyata
- `Helpers/FileUploadHelper.cs` — ValidateImageFile/SaveFileAsync/DeleteFile
- `tests/e2e/helpers/examTypes.ts` + `wizardSelectors.ts` + `tests/helpers/auth.ts`/`accounts.ts`/`utils.ts`/`dbSnapshot.ts` — helper
- `tests/e2e/cmp-records-351.spec.ts` (L44-75) — pola per-spec snapshot (TEMPLATE)
- `tests/e2e/exam-taking.spec.ts` (L116-159) — coachee start-exam flow
- `tests/e2e/global.setup.ts` / `global.teardown.ts` — matrix hardcode (anti-pattern untuk diedit)
- `tests/playwright.config.ts` / `tests/package.json` — runner config
- `docs/SEED_WORKFLOW.md` + `docs/SEED_JOURNAL.md` (format + contoh Phase 354 L9)
- `CLAUDE.md` (Develop + Seed Workflow)

### Secondary (MEDIUM)
- `.planning/REQUIREMENTS.md` (TST-01/02), `.planning/STATE.md` (history), `355-CONTEXT.md` (decisions)

### Tertiary (verifikasi saat Wave 0)
- Memory `reference_dev_credentials` (creds 123456 — 32 hari, diverifikasi konsisten dengan `accounts.ts`)
- Memory project_354 (lesson Razor dynamic + bug 926a57e1/738217cc)

## Project Constraints (from CLAUDE.md)
- **Develop Workflow:** verifikasi lokal `dotnet build` + `dotnet run` (localhost:5277) + cek DB lokal + Playwright SEBELUM commit/push. ❌ Tidak edit kode/DB di Dev/Prod. Promosi Dev = tugas IT.
- **Seed Workflow (WAJIB untuk spec ini):** klasifikasi (`temporary + local-only`) → snapshot DB (.bak) → catat `SEED_JOURNAL.md` (status active) → test → RESTORE (sukses ATAU gagal) → journal `cleaned`. ❌ Jangan biarkan seed/file nyangkut.
- **Bahasa:** respons user-facing Bahasa Indonesia; teknis/kode/path English (sudah diikuti).
- **No skills dir:** `.claude/skills/` / `.agents/skills/` tidak ditemukan (tidak ada project skill rules tambahan).

## Metadata

**Confidence breakdown:**
- Selector form + render markup: HIGH — dibaca file:line langsung.
- xUnit gap audit: HIGH — 3 file dibaca penuh + controller produksi cross-check; gap tunggal teridentifikasi presisi.
- E2E flow + helper reuse: HIGH — pola dari spec existing (351/exam-taking) + helper terverifikasi.
- Global.setup interaction (Open Q1): MEDIUM — perilaku dependency project Playwright benar secara dokumentasi, tapi konsekuensi run belum dieksekusi sesi ini.

**Research date:** 2026-06-09
**Valid until:** ~2026-07-09 (stabil; selektor view bisa berubah bila ada hotfix render — re-verify bila Phase 354 view di-touch lagi)
