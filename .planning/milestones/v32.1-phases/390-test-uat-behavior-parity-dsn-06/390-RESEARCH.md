# Phase 390: Test & UAT Behavior Parity (DSN-06) - Research

**Researched:** 2026-06-17
**Domain:** Regression/parity verification (Playwright runtime + live-mutation UAT) untuk 2 surface Admin coach pasca-redesign v32.1; SEED_WORKFLOW snapshot/restore; Excel export/import assert.
**Confidence:** HIGH (semua klaim diverifikasi langsung dari kode/config sesi ini — view, controller, spec, helper, config)

## Summary

Phase 390 adalah phase **penutup** v32.1. Tidak menambah fitur. Tujuan: buktikan redesign Phase 388 (CoachWorkload polish DSN-04/05) + Phase 389 (CoachCoacheeMapping accordion DSN-01/02/03) **tidak meregresi** satu pun aksi existing. Verifikasi 3-lapis sesuai CONTEXT D-01/D-03/D-05: (1) Playwright **parity-assert non-destruktif** (modal field ter-isi, AJAX route ter-fire via `page.route` dengan basePath, 0 console error, struktur utuh) — promote spec smoke V-10..V-14 + hook 388; (2) **UAT live-mutation** lewat Playwright MCP (tambah/edit/nonaktif/graduated/hapus/reactivate + filter/threshold/approve/skip) dibungkus **BACKUP → RESTORE**; (3) **Excel export auto** (download event) + **import manual** (fixture upload + restore).

Dependency **D-07 sudah TERPENUHI**: per snapshot awal STATE menyebut 389-02 in-progress, tapi `git log` sesi ini menunjukkan `aa377982 feat(389-02): rewrite grouped table jadi accordion card per coach (DSN-01/02)` **sudah ter-commit** dan `git status` bersih untuk `CoachCoacheeMapping.cshtml` (file=1077 baris, markup accordion lengkap). **390 boleh dieksekusi sekarang** setelah `dotnet build` hijau dikonfirmasi. [VERIFIED: git log + git status sesi ini]

Bonus temuan: **CoachWorkload.cshtml juga sudah dipolish** (filter bar + "Saran Penyeimbangan" sudah ber-card, item = `list-group-item suggestion-card`, `.legend-dot` ada) — Phase 388 shipped (ROADMAP progress table: 388 = Complete 2026-06-17). Jadi 390 memverifikasi **markup final keduanya**, bukan target lama. [VERIFIED: `Views/Admin/CoachWorkload.cshtml:125-147,245-292`]

**Primary recommendation:** **Extend** 2 spec yang sudah ada (`coachcoacheemapping-389.spec.ts` + `coachworkload-388.spec.ts`) untuk promote smoke → strong parity-assert non-destruktif (TIDAK buat `dsn06-parity-390.spec.ts` baru — hindari duplikasi, CONTEXT discretion condong extend). Mutasi DB dijalankan main agent via Playwright MCP, dibungkus `tests/helpers/dbSnapshot.ts` (atau sqlcmd manual) BACKUP→RESTORE + log `SEED_JOURNAL.md`. Excel export = `page.waitForEvent('download')` assert (3 file: `CoachCoacheeMapping.xlsx`, `coach_workload.xlsx`, `coach_coachee_import_template.xlsx`); import = UAT manual fixture 2-kolom (`NIP Coach`, `NIP Coachee`).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (Hybrid):** Playwright = parity-assert non-destruktif (modal field ter-isi benar, AJAX route ter-fire via `page.route` intercept, 0 console error, struktur DSN-01/02/03 utuh). Roundtrip mutasi nyata dijalankan via UAT browser live (D-03). Full-mutation E2E otomatis **ditolak** (effort besar + flaky + risk DB lokal); smoke murni **ditolak** (jaring terlalu tipis).
- **D-02:** Promote spec smoke yang sudah ada — `coachcoacheemapping-389.spec.ts` (V-10..V-14) + hook parity `coachworkload-388.spec.ts` — dari "modal-buka/hook-ada" jadi parity-assert lebih kuat (assert isi field, route fired, no-error), tetap non-destruktif/idempotent. **JANGAN buat seed permanen di spec.**
- **D-03:** Aksi mutasi DB (tambah/hapus/reactivate/nonaktif/graduated/edit/setujui-lewati saran) dijalankan **Claude via Playwright MCP live** di `localhost:5277`. WAJIB **snapshot DB SEBELUM** (SEED_WORKFLOW `BACKUP DATABASE`) + **RESTORE SESUDAH** (sukses atau gagal). Catat di `docs/SEED_JOURNAL.md` (tujuan + klasifikasi temporary+local-only + entitas tersentuh) → tandai `cleaned`. User cukup spot-check akhir + screenshot bukti.
- **D-04 (daftar aksi roundtrip wajib):**
  - CoachCoacheeMapping: **tambah** (assign modal → simpan), **edit** (edit modal → simpan), **nonaktifkan** (`confirmDeactivate`), **graduated** (`MarkMappingCompleted` form POST), **hapus** (`confirmDelete` → row remove), **aktifkan-kembali** (`reactivateMapping`). Semua sukses tanpa error/500 + data ter-update benar di UI/DB.
  - CoachWorkload: **filter section**, **set threshold** (sebagai Admin), **setujui** (`.approve-btn`) & **lewati** (`.skip-btn`) saran penyeimbangan; warna/badge status (Normal/Mendekati/Overloaded) benar.
- **D-05:** **Export Excel** (CoachCoacheeMapping + CoachWorkload) = otomatis Playwright — assert `download` event + cek ringan filename/extension (header/row-count opsional). **Import Excel** = UAT manual (user upload fixture `.xlsx` kecil disposable; lalu RESTORE). **Download Template** = smoke (klik → download event).
- **D-06:** Defect parity yang bisa dibetulkan di **view** (`.cshtml` / `@section Scripts` inline) → **fix inline** di phase ini (commit atomic per fix). Defect yang menuntut sentuh **controller/service/backend/migration** → **STOP**, dokumentasikan sbg temuan + lapor user (jangan langgar constraint 0-backend v32.1).
- **D-07:** Phase 390 depends 388 (DONE) + 389. **Eksekusi 390 TUNGGU 389-02 ter-commit + `dotnet build` hijau.** Discuss/plan boleh sekarang. *(Update RESEARCH: 389-02 SUDAH ter-commit `aa377982` — gate D-07 tinggal verifikasi `dotnet build` hijau.)*
- **Constraint global v32.1 (terkunci):** **0 backend, 0 controller, 0 endpoint, 0 migration.** Behavior parity wajib.

### Claude's Discretion
- Organisasi spec: extend `coachcoacheemapping-389.spec.ts` + `coachworkload-388.spec.ts` vs file baru `dsn06-parity-390.spec.ts`. Kecenderungan **extend existing** (hindari duplikasi spec).
- Detail assert per test, helper login/auth, urutan langkah UAT, format checklist UAT.
- Pemilihan fixture `.xlsx` untuk template/import manual.
- Apakah jalankan `dotnet test` suite penuh atau subset relevan (default: suite penuh harus hijau, tak regresi).

### Deferred Ideas (OUT OF SCOPE)
- **None** — diskusi tetap dalam scope phase. Bila defect butuh backend ditemukan saat tes → di-defer keluar v32.1 (D-06) untuk keputusan user, **bukan dikerjakan di phase ini**.
- Eksklusi global v32.1 (REQUIREMENTS §Out of Scope): perubahan backend/controller, migration/skema DB, redesign master-detail/kanban ("C"), halaman admin lain di luar 3 surface, perubahan kolom data/fungsi baru.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| DSN-06 | Setelah redesign, **semua aksi existing tetap berfungsi** — CoachCoacheeMapping: tambah/edit/nonaktifkan/graduated/hapus/aktifkan-kembali mapping + import & export Excel + modal assign/edit/deactivate/delete; CoachWorkload: filter section, export Excel, set threshold (Admin), setujui & lewati saran. Tidak ada perubahan endpoint/JS-contract di luar yang dibutuhkan untuk render baru. | **Parity Inventory** (di bawah) memetakan 15 aksi → trigger → JS fn → endpoint → success signal, 1:1 sehingga planner bisa tulis 1 verifikasi per aksi. **Spec Escalation** memberi cara strengthen V-10..V-14 + 388 hooks. **Live-Mutation UAT Plan** + **Excel** + **Env Gotchas** menutup sisanya. Endpoint/JS-contract diverifikasi UNCHANGED via grep `appUrl()`/`asp-action` + controller route. |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Render accordion card + collapse + modal | Frontend (Razor view + Bootstrap JS) | — | Murni markup `.cshtml` + `@section Scripts`; ini satu-satunya tier v32.1 boleh sentuh |
| AJAX mutation (assign/edit/deactivate/delete/reactivate/approve/skip/threshold) | API / Backend (`CoachMappingController` `[Route("Admin/[action]")]`) | Frontend (fetch wiring) | Endpoint **TIDAK boleh disentuh** (0-backend). Frontend hanya memanggil; parity = wiring frontend tetap memanggil endpoint yang sama dengan basePath benar |
| Excel export / template / import | API / Backend (ClosedXML di controller) | Frontend (link/form trigger) | Generation di controller (unchanged); frontend hanya `<a asp-action>` / `<form>` |
| DB persistence (CoachCoacheeMappings, ProtonTrackAssignments, WorkloadThreshold) | Database (`HcPortalDB_Dev`) | — | Mutasi UAT menyentuh DB → WAJIB snapshot/restore (SEED_WORKFLOW) |
| Test verification | Test infra (Playwright @ `tests/`) | DB snapshot helper (`dbSnapshot.ts`) | Spec parity non-destruktif; live mutation lewat Playwright MCP main agent |

**Catatan tier-correctness (untuk plan-checker):** Tidak ada satupun task 390 boleh mengubah tier API/Backend/Database. Jika defect parity butuh tier itu → STOP (D-06). Semua perbaikan parity = tier Frontend (view inline) saja.

## Standard Stack

### Core (yang dipakai phase — sudah terpasang, JANGAN install baru)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| @playwright/test | ^1.58.2 | Runtime parity assert (modal, AJAX route intercept, download event, console error) | Sudah jadi test infra proyek; spec 388/389 ditulis di atasnya [VERIFIED: `tests/package.json`] |
| typescript | ^5.9.3 | Bahasa spec | Sudah ada [VERIFIED: `tests/package.json`] |
| exceljs | ^4.4.0 | (Opsional) baca isi `.xlsx` hasil download / buat fixture import | Sudah dependency di `tests/package.json` — bisa dipakai assert row/header export & generate fixture import [VERIFIED] |
| sqlcmd (SQL Server Express) | system | BACKUP/RESTORE `HcPortalDB_Dev` | SOP SEED_WORKFLOW §5; di-wrap `tests/helpers/dbSnapshot.ts` [VERIFIED] |
| xUnit (`dotnet test`) | (existing) | Regression suite backend tidak regresi | Default suite penuh harus hijau (CONTEXT discretion) |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap 5 + Bootstrap Icons | (CDN/app) | Komponen modal/collapse/card yang diverifikasi | Tidak di-install — hanya diverifikasi render-nya |
| Chart.js 4 | CDN `chart.js@4` | `#workloadChart` di CoachWorkload | Diverifikasi render, bukan diubah [VERIFIED: `CoachWorkload.cshtml:331`] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Extend 2 spec existing | File baru `dsn06-parity-390.spec.ts` | Duplikasi `loginAny` + describe; CONTEXT condong extend → **PILIH extend** |
| Live-mutation via Playwright MCP + snapshot/restore | Full automated mutation E2E di spec | Ditolak D-01: flaky + effort + risk seed permanen di DB lokal |
| `page.waitForEvent('download')` | Assert HTTP 200 di `page.route` saja | Download event = bukti file benar-benar terunduh, lebih kuat untuk D-05 |

**Installation:** **TIDAK ADA**. Semua sudah terpasang. Verifikasi: `cd tests && npx playwright --version` (harus ~1.58.2).

**Version verification:** Versi diambil dari `tests/package.json` (lokal, bukan training). Tidak perlu `npm view` karena phase ini tidak menambah dependency. [VERIFIED: file lokal]

## Architecture Patterns

### System Architecture Diagram (alur verifikasi 390)

```
                         Phase 390 verification flow
                         ===========================

  [dotnet build] --green--> [dotnet run  Authentication__UseActiveDirectory=false]
                                          | localhost:5277 (Kestrel)
                                          v
        +---------------------------------+----------------------------------+
        |                                 |                                  |
        v                                 v                                  v
  (1) PLAYWRIGHT PARITY            (2) LIVE-MUTATION UAT           (3) REGRESSION
  (non-destruktif)                 (Playwright MCP, main agent)    dotnet test (xUnit)
        |                                 |                                  |
  cd tests; npx playwright          BACKUP DATABASE  <--SEED_WORKFLOW §5.1   full suite green
  test coachcoacheemapping-389        (dbSnapshot.ts / sqlcmd)              (no regression)
  + coachworkload-388 --workers=1         |
        |                                 v
  login admin@pertamina.com         do mutations: assign/edit/deactivate/
  /Account/Login                      graduated/delete/reactivate +
        |                             filter/threshold/approve/skip
        v                                 |
  assert: modal field set,          screenshot bukti + DB cross-check
  page.route(**/Admin/...*) fired,        |
  0 console error, struktur utuh          v
        |                             RESTORE DATABASE <--SEED_WORKFLOW §5.2
        v                             (sukses ATAU gagal) -> journal cleaned
  Excel export: page.waitForEvent
  ('download') x3 files
        |
        +--> all green --> milestone v32.1 siap 1 push --> notify IT re-deploy Dev (migration=FALSE)
```

### Recommended Test Structure (extend existing — JANGAN buat file baru)
```
tests/
├── e2e/
│   ├── coachcoacheemapping-389.spec.ts   # EXTEND: promote V-10..V-14 + tambah Export download assert
│   ├── coachworkload-388.spec.ts          # EXTEND: promote hook approve/skip + tambah Export download + threshold modal field assert
│   ├── global.setup.ts                    # JANGAN sentuh (matrix seed — runs sebagai dependency 'setup')
│   └── global.teardown.ts                 # JANGAN sentuh (matrix restore)
├── helpers/
│   ├── accounts.ts                        # admin@pertamina.com / 123456 (Admin)
│   ├── dbSnapshot.ts                      # backup()/restore()/execScript()/queryScalar() — pakai untuk live UAT
│   └── auth.ts                            # login() (pakai loginAny lokal di spec, lebih robust)
├── fixtures/                              # taruh fixture import .xlsx disini (mis. import-mapping-390.xlsx)
└── playwright.config.ts                   # baseURL localhost:5277, fullyParallel:false, projects setup->chromium
```

### Pattern 1: Non-destructive AJAX route intercept (basePath assert)
**What:** Assert wiring frontend memanggil endpoint dengan PathBase-aware URL (cegah 404 sub-path), tanpa benar-benar mengubah data.
**When to use:** Verifikasi tiap aksi AJAX (deactivate/delete-preview/reactivate/approve/skip/threshold) tanpa mutasi.
**Example:**
```typescript
// Source: tests/e2e/coachcoacheemapping-389.spec.ts:266-278 (V-13 — sudah benar, ini polanya)
let hit = false, hitPath = '';
await page.route('**/Admin/CoachCoacheeMappingDeletePreview*', route => {
  hit = true; hitPath = route.request().url(); route.continue();
});
await hapusBtn.click();
await expect(page.locator('#deleteModal')).toBeVisible();
await expect.poll(() => hit, { timeout: 10_000 }).toBe(true);
expect(hitPath).toContain('/Admin/CoachCoacheeMappingDeletePreview'); // basePath sub-path OK
```

### Pattern 2: Zero-console-error gate (parity proof)
**What:** Tangkap `console.error`/`pageerror` selama interaksi; assert kosong. Bukti JS-contract tak rusak pasca-rewrite markup.
**When to use:** Tiap describe block parity (CONTEXT D-01 "0 console error").
**Example:**
```typescript
// Pola standar Playwright (verifikasi runtime — Phase 354 lesson grep+build tak cukup)
const errors: string[] = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(e.message));
// ... lakukan klik modal/collapse/filter ...
expect(errors, errors.join('\n')).toHaveLength(0);
```

### Pattern 3: Excel export via download event (D-05)
**What:** Klik link export → tangkap download → assert filename/extension. Opsional baca isi via exceljs.
**Example:**
```typescript
// CoachCoacheeMapping export = <a asp-action="CoachCoacheeMappingExport"> (GET) -> file CoachCoacheeMapping.xlsx
const [download] = await Promise.all([
  page.waitForEvent('download'),
  page.getByRole('link', { name: 'Export Excel' }).click(),
]);
expect(download.suggestedFilename()).toBe('CoachCoacheeMapping.xlsx');
// Opsional header assert via exceljs (12 kolom): "Coach Name","Coach Section",...,"End Date"
```

### Pattern 4: Snapshot → mutate → restore (live UAT, SEED_WORKFLOW)
**What:** Bungkus SEMUA mutasi DB lokal dalam BACKUP→…→RESTORE. Catat journal.
**Example:**
```typescript
// Source: tests/helpers/dbSnapshot.ts (backup/restore) — atau sqlcmd manual SEED_WORKFLOW §5
import * as db from '../helpers/dbSnapshot';
const snap = `${defaultBackupDir}/HcPortalDB_Dev-pre390-${ts}.bak`;
await db.backup(snap);            // SEBELUM mutasi
try { /* mutations via Playwright MCP */ } finally { await db.restore(snap); } // SESUDAH (sukses/gagal)
```

### Anti-Patterns to Avoid
- **Membuat seed permanen di spec** — D-02 melarang. Parity spec HARUS non-destruktif/idempotent; mutasi nyata hanya di UAT live ber-snapshot.
- **Hardcode `/Admin/...` di assert tanpa basePath** — gunakan glob `**/Admin/...` di `page.route` (sub-path lokal `/` tapi Dev `/KPB-PortalHC`).
- **Menyentuh controller/endpoint/migration** untuk "memperbaiki" defect — langgar 0-backend. STOP + report (D-06).
- **Menjalankan Playwright `--workers` > 1** — login 500/err53 (NTLM loopback). WAJIB `--workers=1`.
- **Mengubah `global.setup.ts`/`global.teardown.ts`** — itu pipeline matrix Phase 315; tak relevan tapi jalan otomatis sebagai dependency. Biarkan.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Snapshot/restore DB lokal | SQL DELETE manual per-entity cleanup | `tests/helpers/dbSnapshot.ts` `backup()`/`restore()` atau sqlcmd SEED_WORKFLOW §5 | Cascade orphan (Sessions/Assignments/Progress) susah dibersihkan manual; restore deterministik [VERIFIED: SEED_WORKFLOW §7] |
| Login fixture | Tulis ulang auth flow | `loginAny()` lokal di spec (sudah ada) / `helpers/auth.ts` | Sudah teruji; field `input[name="email"]`/`input[name="password"]`/`button[type="submit"]` [VERIFIED: `Views/Account/Login.cshtml:173,181,195`] |
| Assert file terunduh | Polling filesystem | `page.waitForEvent('download')` + `download.suggestedFilename()` | API native Playwright, deterministik |
| Baca isi .xlsx | Parser CSV custom | `exceljs` (sudah dependency) | Sudah ada di `tests/package.json` |
| Resolve URL sub-path | Hardcode prefix | `appUrl()`/`window.basePath` (app) + glob `**/Admin/...` (test) | Helper global app sudah PathBase-aware [VERIFIED: `_Layout.cshtml:54-55`] |

**Key insight:** Seluruh "infrastruktur" yang dibutuhkan 390 SUDAH ADA (spec, login helper, dbSnapshot, exceljs, appUrl). Phase 390 = **merangkai**, bukan membangun. Risiko nyata = melewatkan satu aksi dari Parity Inventory atau menyentuh tier backend secara tak sengaja.

## Parity Inventory (Q1 — peta lengkap aksi → 1 verifikasi per aksi)

### CoachCoacheeMapping (`Views/Admin/CoachCoacheeMapping.cshtml`, endpoints di `Controllers/CoachMappingController.cs` `[Route("Admin/[action]")]`)

| # | Aksi | Trigger (selector / element) | JS fn / wiring | Endpoint (method) | Success signal |
|---|------|------------------------------|----------------|-------------------|----------------|
| C1 | **Tambah mapping** | `button:has-text("Tambah Mapping")` (`btn-primary`, `data-bs-target="#assignModal"`) → modal → Simpan `onclick="submitAssign()"` | `submitAssign()` L755 | POST `appUrl('/Admin/CoachCoacheeMappingAssign')` (JSON) L785 | `data.success` → `location.reload()`; baris baru muncul. Warning-path: `data.warning` → confirm → re-POST `ConfirmProgressionWarning=true` |
| C2 | **Edit mapping** | `#collapse-N button:has-text("Edit")` `onclick="openEditModal(7 arg)"` L318 → `#editModal` → Simpan `onclick="submitEdit()"` | `openEditModal()` L824 (set `#editCoacheeName`.textContent, `#editCoachSelect`, `#editStartDate`, cascade unit), `submitEdit()` L845 | POST `appUrl('/Admin/CoachCoacheeMappingEdit')` (JSON) L867 | `data.success` → reload; `#editCoacheeName` non-kosong = openEditModal jalan |
| C3 | **Nonaktifkan** | `button:has-text("Nonaktifkan")` `onclick="confirmDeactivate(id,name)"` L331 → `#deactivateModal` → `onclick="submitDeactivate()"` | `confirmDeactivate()` L887 (2 fetch pre-load), `submitDeactivate()` L929 | GET `appUrl('/Admin/CoachCoacheeMappingGetSessionCount')` (POST form) L893 + GET `.../ActiveAssignmentCount?id=` L912 + POST `.../CoachCoacheeMappingDeactivate` L932 | modal terbuka + `#deactivateSessionInfo` ter-isi; submit → reload, baris jadi Non-aktif |
| C4 | **Graduated** | `<form asp-action="MarkMappingCompleted">` POST + `@Html.AntiForgeryToken()` + `onclick="return confirm(...)"` L334-341 | form POST (bukan AJAX) | POST `/Admin/MarkMappingCompleted` (`mappingId`) | redirect; baris → badge "Graduated" (cek `IsCompleted` DULU, Phase 356 D-06) |
| C5 | **Hapus** | `button:has-text("Hapus")` `onclick="confirmDelete(id,name)"` L350 → `#deleteModal` → `onclick="submitDelete()"` | `confirmDelete()` L952 (fetch preview), `submitDelete()` L975 (row.remove) | GET `appUrl('/Admin/CoachCoacheeMappingDeletePreview?id=')` L962 + POST `.../CoachCoacheeMappingDelete` L979 | `data.success` → `deleteModal.hide()` + `document.querySelector('tr[data-mapping-id="ID"]').remove()` L991 + toast |
| C6 | **Aktifkan-kembali** | `button:has-text("Aktifkan")` `onclick="reactivateMapping(id)"` L346 (hanya pada baris non-aktif, non-graduated) | `reactivateMapping()` L1009 (confirm) | POST `appUrl('/Admin/CoachCoacheeMappingReactivate')` L1012 | `data.success` → reload (atau Swal prompt assign track bila `data.showAssignPrompt`) |
| C7 | **Import Excel** | `button data-bs-target="#importMappingModal"` L61 → `<form asp-action="ImportCoachCoacheeMapping" enctype="multipart/form-data">` L1056 → submit `#btnImportMapping` | form multipart (file `excelFile`) | POST `/Admin/ImportCoachCoacheeMapping` | redirect ke CoachCoacheeMapping + TempData ImportResults card render (Berhasil/Diaktifkan/Skip/Error counts) |
| C8 | **Export Excel** | `<a asp-action="CoachCoacheeMappingExport">` L64 | link GET | GET `/Admin/CoachCoacheeMappingExport` | download `CoachCoacheeMapping.xlsx` (12 kolom) |
| C9 | **Download Template** | `<a asp-action="DownloadMappingImportTemplate">` L58 | link GET | GET `/Admin/DownloadMappingImportTemplate` | download `coach_coachee_import_template.xlsx` (2 kolom: NIP Coach, NIP Coachee) |
| C10 | **Filter Seksi + Cari + Tampilkan Semua + Pagination** | `select[name="section"]` `onchange="resetPageAndSubmit()"` L191, submit `button:has-text("Cari")` L207, `#showAllCheck` L218, pagination links L370-389 | `resetPageAndSubmit()` L684 (set page=1, form.submit) | GET `/Admin/CoachCoacheeMapping?section=&search=&showAll=&page=` | URL berubah `section=` / `search=` / `showAll=true` / `page=N`; daftar ter-filter |

> **Catatan endpoint vs `asp-controller`:** Toolbar Excel (C7/C8/C9) ditulis `asp-controller="CoachMapping"` + filter form `asp-controller="Admin"` di markup, **tapi keduanya resolve ke route `/Admin/[action]`** karena `CoachMappingController` ber-`[Route("Admin/[action]")]` (L14). `AdminController` lama punya action shim yang redirect/serve sama. AJAX semua eksplisit `/Admin/...`. **Parity = jalur ini tetap utuh** (jangan ubah `asp-controller`). [VERIFIED: controller route + grep endpoints]

### CoachWorkload (`Views/Admin/CoachWorkload.cshtml`, endpoints sama controller `CoachMappingController`)

| # | Aksi | Trigger (selector / element) | JS fn / wiring | Endpoint (method) | Success signal |
|---|------|------------------------------|----------------|-------------------|----------------|
| W1 | **Filter section** | `select[name="section"]` L129 + submit `button:has-text("Filter")` L143 + `Reset` link L144 | form GET (native) | GET `/Admin/CoachWorkload?section=` | URL `section=` set; Reset → URL tanpa `section=` |
| W2 | **Export Excel** | `<a asp-action="ExportCoachWorkload">` L44 | link GET (param `section`) | GET `/Admin/ExportCoachWorkload?section=` | download `coach_workload.xlsx` |
| W3 | **Set Threshold (Admin)** | `button data-bs-target="#thresholdModal"` L50 (role-gate `User.IsInRole("Admin")`) → `#maxCoachees`/`#warningThreshold` → `#saveThreshold` L321 | listener `saveThreshold` L500 (validasi warning<=max) | POST `(window.basePath||'')+'/Admin/SetWorkloadThreshold'` L515 (`maxCoachees`,`warningThreshold`) | `data.success` → modal hide + reload; badge status re-evaluasi |
| W4 | **Setujui saran** | `.approve-btn` (5 data-*: mapping-id, new-coach-id, coachee-name, from-coach, to-coach) L273 | listener L400 (confirm) | POST `(window.basePath||'')+'/Admin/ApproveReassignSuggestion'` L416 (`mappingId`,`newCoachId`) | `data.success` → `fadeOutCard(#sug-ID)` L428 |
| W5 | **Lewati saran** | `.skip-btn` (data-mapping-id) L281 | listener L446 | POST `(window.basePath||'')+'/Admin/SkipReassignSuggestion'` L454 (`mappingId`) | `fadeOutCard(#sug-ID)` L465 |
| W6 | **Chart + legend + summary cards** | `#workloadChart` L170, `.legend-dot` x3 L173-175, summary cards L82-122 | Chart.js init L337 (`canvas.style.height` runtime) | (render-only, no endpoint) | canvas visible bila ada data; legend 3 dot; badge status OK/Warning/danger benar |
| W7 | **Expand "Lihat" coachee** | `button data-bs-toggle="collapse" data-bs-target="#coachee-N"` L206 (`.expand-chevron`) | chevron rotate listener L475 | (no endpoint) | `#coachee-N` collapse buka, chevron rotate 90deg |

**Cross-check vs CONTEXT D-04:** D-04 mendaftar CoachCoacheeMapping = tambah/edit/nonaktif/graduated/hapus/reactivate (= C1-C6 ✓) + import/export (= C7/C8 ✓). CoachWorkload = filter/threshold/approve/skip (= W1/W3/W4/W5 ✓) + export (W2 ✓). **Inventory MELEBIHI D-04** (juga C9/C10/W6/W7) — planner boleh tambah verifikasi ringan untuk C9/C10/W6/W7 (smoke) tapi WAJIB cover D-04 set penuh (roundtrip nyata di UAT live). [VERIFIED: CONTEXT D-04 vs kode]

## Spec Escalation (Q2 — strengthen V-10..V-14 + 388 hooks; rekomendasi extend)

**Rekomendasi: EXTEND 2 spec existing.** Rationale: (a) CONTEXT discretion condong extend (hindari duplikasi `loginAny`+describe); (b) spec sudah ber-`beforeEach` login+navigasi yang benar; (c) test baru = tambah `test(...)` di describe yang sama; (d) file baru `dsn06-parity-390.spec.ts` akan menduplikasi 100% scaffold. Satu-satunya alasan file baru = kalau ingin pisahkan "parity-390" dari "structural-389/388" untuk reporting — tidak sepadan.

### `coachcoacheemapping-389.spec.ts` — promote V-10..V-14 (saat ini sebagian "modal-buka/hook-ada")

| Test | Sekarang (smoke) | Escalate ke (parity-assert non-destruktif) |
|------|------------------|--------------------------------------------|
| **V-10 edit modal** | buka collapse → klik Edit → `#editModal` visible + `#editCoacheeName` non-kosong | TAMBAH: `#editCoachSelect` value non-kosong (coach ter-set) + `#editStartDate` value match `/\d{4}-\d{2}-\d{2}/` (openEditModal 7-arg set semua field) + 0 console error selama buka |
| **V-11 delete** | buka → Hapus → `#deleteModal` visible + tombol submit "Hapus" ada | TAMBAH: `page.route('**/Admin/CoachCoacheeMappingDeletePreview*')` fired (preload jalan) + `#deleteCoacheeName` ter-isi (bukan "Memuat...") — tetap TANPA submit (non-destruktif) |
| **V-12 aksi branch** | tiap baris ada Edit; baris Graduated tak ada "Aktifkan" | SUDAH cukup kuat (struktural). TAMBAH (opsional): baris IsActive → ada "Nonaktifkan" + form Graduated; baris non-aktif non-graduated → ada "Aktifkan"+"Hapus" (urutan if/else `IsCompleted→IsActive→else` Phase 356 D-06) |
| **V-13 ajax appUrl** | route `**/Admin/CoachCoacheeMappingDeletePreview*` fired saat confirmDelete | SUDAH benar (ini pola emas). TAMBAH: `hitPath` mengandung basePath bila ada (assert tak hardcode) |
| **V-14 filter** | select section → "Cari" → URL `section=` | SUDAH cukup. TAMBAH (opsional): `resetPageAndSubmit` via `#showAllCheck` onchange → URL `showAll=true`; pagination link → URL `page=` |

**Test BARU yang perlu ditambah ke spec ini (D-05 export, non-destruktif):**
- **V-15 Export Excel**: `page.waitForEvent('download')` saat klik `Export Excel` → `suggestedFilename() === 'CoachCoacheeMapping.xlsx'`.
- **V-16 Download Template**: download → `coach_coachee_import_template.xlsx`.
- **V-17 console-error gate**: buka 1 card + Edit modal + Hapus modal → 0 console.error.

### `coachworkload-388.spec.ts` — promote hook approve/skip + tambah export/threshold

| Test | Sekarang | Escalate ke |
|------|----------|-------------|
| `hook approve/skip & data-*` (test 3) | `test.skip` bila no overload; assert 5 data-* approve + 1 skip | SUDAH kuat. (Tetap data-gated — overload butuh data; full approve/skip = UAT live) |
| filter submit+reset (test 5) | select → Filter → URL `section=`; Reset → tanpa | SUDAH cukup |
| chart+legend (test 4) | 3 `.legend-dot` + inline background; `#workloadChart` visible bila data | SUDAH cukup |

**Test BARU untuk spec ini:**
- **W-EXP Export Excel**: `waitForEvent('download')` → `coach_workload.xlsx`.
- **W-THR Threshold modal (Admin, non-destruktif)**: klik "Set Threshold" → `#thresholdModal` visible → `#maxCoachees`/`#warningThreshold` ter-isi nilai existing (non-kosong) → **tutup tanpa simpan** (mutasi nyata = UAT live). Assert role-gate: tombol "Set Threshold" visible untuk admin.
- **W-ERR console-error gate**: load + buka threshold modal + expand "Lihat" → 0 console.error.

> **Catatan kontrak halaman (DARI memory + verifikasi):** CoachCoacheeMapping submit filter = **"Cari"** (`CoachCoacheeMapping.cshtml:207`); CoachWorkload submit filter = **"Filter"** (`CoachWorkload.cshtml:143`). JANGAN tertukar di assert. [VERIFIED]

## Live-Mutation UAT Plan (Q3 — Playwright MCP, snapshot→restore)

**Akun:** `admin@pertamina.com` / `123456` (role Admin) — perlu Admin untuk Set Threshold + approve/skip (role-gate). [VERIFIED: `tests/helpers/accounts.ts:2` + `CoachWorkload.cshtml:48,270,295`]

**DB lokal:** `HcPortalDB_Dev` di `localhost\SQLEXPRESS`, Integrated Security. [VERIFIED: SEED_WORKFLOW §1 + `dbSnapshot.ts:22-29`]

### Command snapshot/restore (pilih salah satu)

**Opsi A — helper TS (dipakai global.setup; konsisten):**
```bash
# default backup dir di-resolve runtime: SERVERPROPERTY('InstanceDefaultBackupPath')
# C:\Temp\ DIBLOKIR service account → pakai default backup dir
#   e.g. C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\
# panggil db.backup(path) / db.restore(path) dari script TS
```

**Opsi B — sqlcmd manual (SEED_WORKFLOW §5.1/§5.2):**
```bash
# Stop Kestrel dulu (atau pastikan tak ada koneksi)
# BACKUP (gunakan default backup dir, BUKAN C:\Temp jika diblokir):
sqlcmd -S "localhost\SQLEXPRESS" -E -C -Q "BACKUP DATABASE HcPortalDB_Dev TO DISK='C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\HcPortalDB_Dev-pre390.bak' WITH INIT, FORMAT"
# ... lakukan mutasi via Playwright MCP @ localhost:5277 ...
# RESTORE (sukses ATAU gagal):
sqlcmd -S "localhost\SQLEXPRESS" -E -C -Q "USE master; ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE HcPortalDB_Dev FROM DISK='C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\HcPortalDB_Dev-pre390.bak' WITH REPLACE; ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;"
```
> RESTORE butuh exclusive access → Kestrel HARUS mati saat restore. Alur: backup → mati Kestrel ATAU jaga 1 koneksi → mutasi (Kestrel hidup) → mati Kestrel → restore → hidupkan lagi untuk verify. (Atau jalankan restore dengan ROLLBACK IMMEDIATE yang evict koneksi Kestrel.) [VERIFIED: SEED_WORKFLOW §5.3 + `dbSnapshot.ts:80-99`]

### Disposable seed yang dibutuhkan
Aksi roundtrip butuh: (1) **coach eligible** + **coachee tersedia** untuk *tambah*; (2) **mapping aktif** untuk *edit/nonaktif/graduated*; (3) **mapping non-aktif** untuk *aktifkan-kembali*; (4) **mapping** untuk *hapus*; (5) **coach overload** (>threshold) untuk memunculkan **saran penyeimbangan** (approve/skip).

**Cara termudah (tanpa SQL seed manual):** lakukan mutasi **lewat UI sendiri** dalam window snapshot — *tambah* mapping baru lalu *edit*/*nonaktif*/*graduated*/*hapus*/*reactivate* mapping yang baru dibuat itu. Karena seluruhnya di-restore, tak perlu prefix marker. Coach/coachee eligible diambil dari dropdown assign modal yang sudah di-populate (`eligibleCoaches`/`eligibleCoachees` dari ViewBag). Untuk **saran penyeimbangan** (approve/skip): butuh kondisi overload — bila DB lokal tak punya, **assign banyak coachee ke 1 coach** (dalam window snapshot) hingga > threshold, lalu refresh CoachWorkload → saran muncul → approve/skip → screenshot → restore. Bila menyulitkan, approve/skip boleh ditandai **conditional UAT** (skip bila tak ada overload) + dicatat sebagai limitation. [VERIFIED: assign modal populate `CoachCoacheeMapping.cshtml:408-445`]

### Urutan langkah UAT live (1 sesi, 1 snapshot)
```
0. dotnet build (hijau) + dotnet run (Authentication__UseActiveDirectory=false) → localhost:5277
1. BACKUP DATABASE HcPortalDB_Dev → ...-pre390.bak  + tulis SEED_JOURNAL.md (status=active)
2. Login admin@pertamina.com  (Playwright MCP)
3. /Admin/CoachCoacheeMapping:
   a. TAMBAH: Tambah Mapping → pilih coach + coachee + bagian/unit + tanggal → Simpan → assert baris baru (no 500)
   b. EDIT: buka card coach → Edit baris baru → ubah tanggal/track → Simpan → assert nilai update
   c. NONAKTIFKAN: Nonaktifkan baris → modal → konfirmasi → assert badge Non-aktif
   d. AKTIFKAN-KEMBALI: showAll=true → Aktifkan baris non-aktif → assert badge Aktif
   e. GRADUATED: Graduated (form POST) pada baris aktif → assert badge "Graduated"
   f. HAPUS: Hapus baris (mapping disposable) → modal preview → Hapus Permanen → assert row hilang + toast
4. /Admin/CoachWorkload:
   g. FILTER: pilih section → Filter → assert URL section= + tabel ter-filter; Reset
   h. SET THRESHOLD (Admin): Set Threshold → ubah max/warning → Simpan → assert badge status re-evaluasi
   i. (jika overload ada / dibuat) SETUJUI saran (.approve-btn) → assert card fade-out
   j. (jika overload ada / dibuat) LEWATI saran (.skip-btn) → assert card fade-out
5. Screenshot bukti tiap langkah (untuk spot-check user)
6. Mati Kestrel → RESTORE DATABASE FROM ...-pre390.bak WITH REPLACE → hidupkan
7. SEED_JOURNAL.md status → cleaned (verifikasi COUNT mapping = baseline)
```

## Excel Verification (Q4)

### Export (otomatis Playwright — D-05)
| Surface | Trigger | Endpoint | Filename | Schema |
|---------|---------|----------|----------|--------|
| CoachCoacheeMapping | `Export Excel` link L64 | GET `/Admin/CoachCoacheeMappingExport` | `CoachCoacheeMapping.xlsx` | 12 kolom: Coach Name, Coach Section, Coachee Name, Coachee NIP, Coachee Section, Coachee Position, Bagian Penugasan, Unit Penugasan, Current Track, Status, Start Date, End Date [VERIFIED: controller L1315-1319,1353] |
| CoachWorkload | `Export Excel` link L44 | GET `/Admin/ExportCoachWorkload?section=` | `coach_workload.xlsx` | (sheet workload — header di controller ~L1760; assert filename cukup untuk D-05) [VERIFIED: L1770] |
| Template | `Download Template` link L58 | GET `/Admin/DownloadMappingImportTemplate` | `coach_coachee_import_template.xlsx` | 2 kolom: NIP Coach, NIP Coachee + baris contoh + note [VERIFIED: L177-199] |

Assert: `download.suggestedFilename()` match nama di atas + `.endsWith('.xlsx')`. Header/row-count = opsional via `exceljs`.

### Import (UAT manual — D-05)
**Endpoint:** POST `/Admin/ImportCoachCoacheeMapping` (multipart `excelFile`), `[Authorize(Roles="Admin, HC")]` + AntiForgery. [VERIFIED: L202-206]

**Skema fixture minimal valid `.xlsx`** (WAJIB persis):
- **Row 1 (header, case-insensitive):** `A1="NIP Coach"`, `B1="NIP Coachee"` — divalidasi ketat L266-275 (kolom 1 & 2 harus cocok atau ImportError).
- **Row 2+ (data):** `A=NIP coach existing`, `B=NIP coachee existing`. Kedua NIP **harus ada di tabel Users** (kalau tidak → status "Error" per baris, L301-313). StartDate otomatis hari ini, IsActive otomatis true.
- Baris kosong total dilewati (L283). Ukuran maks 10MB; ekstensi `.xlsx`/`.xls`. [VERIFIED: L214-227,277-284]

**Cara buat fixture:** termudah = klik **Download Template** → isi 1 baris NIP coach + NIP coachee yang valid (ambil dari assign modal / DB) → simpan ke `tests/fixtures/import-mapping-390.xlsx`. Atau generate via `exceljs` (2 sel header + 1 baris NIP). 

**Langkah UAT manual import (ber-snapshot):**
```
1. BACKUP (jika belum dalam window snapshot UAT live)
2. /Admin/CoachCoacheeMapping → Import Excel → pilih fixture → Upload & Proses
3. Assert: redirect + card "Hasil Import" muncul (Berhasil Dibuat >=1 / atau Diaktifkan / Skip / Error sesuai isi)
4. Verifikasi baris mapping baru muncul di accordion
5. RESTORE → COUNT mapping kembali baseline
```

## Common Pitfalls

### Pitfall 1: Playwright login 500 saat paralel
**What goes wrong:** Login gagal 500 / err53 saat `--workers > 1`.
**Why:** NTLM loopback + shared-memory SQL conn override; paralel rebut koneksi.
**How to avoid:** SELALU `--workers=1` (juga `fullyParallel:false` sudah di config). Start SQLBrowser bila perlu; `lpc:` shared-memory conn override. [VERIFIED: CONTEXT L126 + memory reference_local_e2e_sql_env_fix + config L8]
**Warning signs:** Test pertama hijau, sisanya 500 di `/Account/Login`.

### Pitfall 2: `dotnet run` lokal tolak login (AD)
**What goes wrong:** Login admin lokal gagal karena Active Directory diaktifkan.
**Why:** App default coba AD auth.
**How to avoid:** Jalankan dengan `Authentication__UseActiveDirectory=false` (env var). Spec header 388/389 menulis ini eksplisit. [VERIFIED: spec header `coachcoacheemapping-389.spec.ts:14` + memory Phase 355]

### Pitfall 3: RESTORE gagal "exclusive access could not be obtained"
**What goes wrong:** RESTORE error karena koneksi Kestrel/SSMS masih hidup.
**Why:** DB punya koneksi aktif.
**How to avoid:** `SET SINGLE_USER WITH ROLLBACK IMMEDIATE` (sudah di helper + SOP) evict koneksi; pastikan Kestrel mati / tutup SSMS. Cek session via `sys.dm_exec_sessions`. [VERIFIED: SEED_WORKFLOW §5.3 + dbSnapshot.ts:80-99]

### Pitfall 4: global.setup matrix seed jalan tak terduga
**What goes wrong:** Menjalankan `npx playwright test` (tanpa filter) memicu **matrix BACKUP+seed+RESTORE** (Phase 315) karena project `chromium` ber-dependency `setup`.
**Why:** `playwright.config.ts` projects: `setup` (matrix seed) → `chromium`.
**How to avoid:** Untuk parity 390, tetap pakai filter spec (`npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1`) — setup TETAP jalan (dependency) tapi idempotent + restore otomatis di teardown. JANGAN ubah setup/teardown. Pastikan tak ada collision ID 9001-9018 di DB lokal sebelum run (setup pre-check L58-66). [VERIFIED: config L22-32 + setup/teardown]
**Warning signs:** Log `[setup] Snapshot path:` + `[setup] Seed SQL executed` saat run spec coach.

### Pitfall 5: Razor dinamis lolos grep tapi rusak runtime
**What goes wrong:** Markup "kelihatan benar" di grep/build, tapi collapse/modal/AJAX rusak runtime.
**Why:** Bootstrap collapse + a11y + JS-contract hanya teruji saat dijalankan.
**How to avoid:** Phase 354 lesson — WAJIB Playwright runtime assert (sudah jadi strategi 390). Grep+build TAK cukup. [VERIFIED: ROADMAP L53 + CONTEXT D-12]

## Runtime State Inventory

> Phase 390 = test/UAT + (opsional) fix view inline. Mutasi DB **bersifat sementara** (snapshot/restore), bukan rename/migration. Tabel ini mengonfirmasi tak ada state runtime persisten yang tertinggal.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | Mutasi UAT live menulis ke `HcPortalDB_Dev` (CoachCoacheeMappings, ProtonTrackAssignments, WorkloadThreshold, AuditLogs) — **sementara** | BACKUP sebelum + RESTORE sesudah (SEED_WORKFLOW); journal cleaned. Tidak ada data permanen |
| Live service config | None — verified: tidak ada n8n/Datadog/eksternal; semua lokal | None |
| OS-registered state | None — verified: tidak ada Task Scheduler/pm2/systemd; hanya Kestrel `dotnet run` + SQL Express service | None |
| Secrets/env vars | `Authentication__UseActiveDirectory=false` (env var saat `dotnet run` lokal) — bukan secret, hanya flag dev | Set saat run; tidak di-commit |
| Build artifacts | None baru — `dotnet build` output normal; tidak ada egg-info/package rename | None |

**Nothing found in 4 dari 5 kategori** — satu-satunya state = mutasi DB temporer yang di-cover snapshot/restore.

## Defect-Fix Boundary (Q6 — view-only vs backend)

**Boleh diperbaiki di Phase 390 (Frontend tier, D-06):**
- `Views/Admin/CoachCoacheeMapping.cshtml` — markup + `@section Scripts` inline (L624-1046) + `@section Styles` (L32-38).
- `Views/Admin/CoachWorkload.cshtml` — markup + `@section Scripts` inline (L330-545) + `<style>` (L16-25).
- (Bila perlu) `tests/e2e/*.spec.ts` + `tests/fixtures/*` — test assets.

**HARUS STOP + report (backend tier, JANGAN sentuh):**
- `Controllers/CoachMappingController.cs` (semua endpoint C1-C10, W1-W7).
- `Controllers/AdminController.cs` (shim/route).
- Service/`Data/`/migration/`Models/`.
- `Helpers/ExcelExportHelper.cs` (export generation).

**Heuristik:** kalau perbaikan menyentuh file di `Controllers/`, `Services/`, `Data/`, `Migrations/`, `Models/`, atau `Helpers/` → STOP. Kalau cukup di `Views/Admin/*.cshtml` (markup/inline JS/inline style) → fix inline, commit atomic per defect. [VERIFIED: file-tier mapping kode]

## Regression Suite (Q7 — command + dependency)

### Dependency 389-02
**SUDAH TERPENUHI.** `git log` sesi ini: `aa377982 feat(389-02): rewrite grouped table jadi accordion card per coach (DSN-01/02)` + `7548c6d0 feat(389-02): normalisasi toolbar ... hapus dead onclick (DSN-03)` ter-commit; `git status` bersih untuk `CoachCoacheeMapping.cshtml`. Markup accordion lengkap (`.card.shadow-sm`, `#collapse-N`, `aria-controls`, badge threshold, `data-mapping-id`, btn-group, btn-primary). **Tinggal konfirmasi `dotnet build` hijau** (gate akhir D-07) sebelum eksekusi. [VERIFIED: git log + view content]

### Command lokal (CLAUDE.md Develop Workflow)
```bash
# 1. Build (gate D-07)
dotnet build                       # 0 error wajib

# 2. Run app (terminal terpisah, biarkan hidup untuk Playwright + UAT)
$env:Authentication__UseActiveDirectory="false"; dotnet run    # localhost:5277

# 3. Regression backend (suite penuh — default CONTEXT discretion)
dotnet test                        # semua hijau, tak regresi

# 4. Playwright parity (app harus sudah jalan di step 2)
cd tests
npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1
#   --workers=1 WAJIB (Pitfall 1). global.setup (matrix) jalan otomatis + restore di teardown.

# 5. (Listing tanpa run, untuk cek spec parse)
npx playwright test coachcoacheemapping-389 --list
```

### Urutan ship (akhir milestone)
build hijau → `dotnet test` hijau → Playwright parity 2 spec hijau → UAT live mutasi roundtrip (snapshot/restore) → Excel export auto + import manual → **1 push origin/ITHandoff** → notify IT re-deploy Dev (**migration=FALSE**). [VERIFIED: CONTEXT specifics + ROADMAP Phase 390 SC#4]

## Code Examples

### Login (pola robust — sudah dipakai di kedua spec)
```typescript
// Source: tests/e2e/coachcoacheemapping-389.spec.ts:24-33
async function loginAny(page: Page, accountKey: AccountKey) {
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);       // Views/Account/Login.cshtml:173
  await page.fill('input[name="password"]', password); // :181
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),               // :195
  ]);
}
```

### Threshold modal non-destruktif (parity W-THR)
```typescript
// Non-destruktif: buka modal, assert field ter-isi, TUTUP tanpa simpan (mutasi nyata = UAT live)
await page.getByRole('button', { name: 'Set Threshold' }).click();
await expect(page.locator('#thresholdModal')).toBeVisible();
await expect(page.locator('#maxCoachees')).not.toHaveValue('');      // existing threshold ter-render
await expect(page.locator('#warningThreshold')).not.toHaveValue('');
await page.locator('#thresholdModal [data-bs-dismiss="modal"]').first().click(); // batal
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| CoachCoacheeMapping = tabel grouped telanjang | Accordion card per coach (`#collapse-N`, badge header) | Phase 389-02 (`aa377982`) | Spec assert berbasis `.card.shadow-sm`/`#collapse-N`, BUKAN `<table class="table-primary">` lagi |
| CoachWorkload filter/saran = elemen telanjang | Filter + Saran ber-card `card shadow-sm`; item = `list-group-item suggestion-card`; `.legend-dot` class | Phase 388-02 | Spec assert `.card-header` "Filter"/"Saran Penyeimbangan" + `.list-group-item.suggestion-card` (BUKAN `.card.suggestion-card`) |

**Deprecated/outdated:** assert lama berbasis baris `table-primary` collapse (CoachCoacheeMapping) — sudah tak berlaku, accordion menggantikan.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | DB lokal lokasi backup default dir = `C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\` (versi instance bisa beda mis. MSSQL16) | Live-Mutation UAT Plan | Path .bak salah → backup gagal. Mitigasi: resolve runtime via `SERVERPROPERTY('InstanceDefaultBackupPath')` (dbSnapshot/setup sudah lakukan ini) — gunakan helper, jangan hardcode |
| A2 | DB lokal saat ini punya >=1 coach group + coachee untuk parity struktural; bila kosong, test struktural `test.skip` (sudah ada data-guard) | Spec Escalation | Bila DB kosong, parity struktural ter-skip (bukan gagal); roundtrip tetap via UAT live (tambah dulu). Risiko rendah — data-guard sudah menangani |
| A3 | Kondisi "overload" (saran penyeimbangan) mungkin tidak ada di DB lokal → approve/skip butuh dibuat manual atau conditional | Live-Mutation UAT + Spec | Bila tak ada overload, approve/skip jadi conditional UAT (di-skip + dicatat). Tidak memblok DSN-06 (CONTEXT D-04 tetap minta, tapi bisa dibuat dalam window snapshot) |
| A4 | `dotnet build` saat ini hijau (389-02 ter-commit) — belum dijalankan di sesi research | Regression Suite | Bila build merah, eksekusi 390 ditunda (gate D-07). Planner WAJIB jadikan `dotnet build` hijau sebagai Task pertama |

**Catatan:** A1-A4 perlu konfirmasi saat eksekusi (bukan saat plan). Tidak ada asumsi pada compliance/retensi/security.

## Open Questions

1. **Apakah ada coach overload di DB lokal untuk approve/skip?**
   - Yang kita tahu: saran muncul hanya bila ada coach > threshold.
   - Yang belum jelas: state data lokal saat ini.
   - Rekomendasi: dalam window snapshot UAT, assign banyak coachee ke 1 coach hingga > threshold → saran muncul → approve/skip → restore. Bila sulit, tandai conditional + screenshot kondisi "seimbang".

2. **Versi instance SQL (MSSQL16 vs 17) untuk path backup?**
   - Rekomendasi: JANGAN hardcode; pakai `db.backup()/restore()` (resolve `InstanceDefaultBackupPath` runtime) atau query dulu.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (`dotnet`) | build/run/test | ✓ (proyek aktif) | (existing) | — |
| SQL Server Express `localhost\SQLEXPRESS` | DB lokal + snapshot/restore | ✓ (appsettings.Development) | — | — |
| `sqlcmd` | BACKUP/RESTORE | ✓ (dipakai dbSnapshot.ts) | — | sqlcmd via helper |
| Node + @playwright/test | parity spec | ✓ (`tests/node_modules`) | 1.58.2 | — |
| exceljs | (opsional) baca/buat .xlsx | ✓ (`tests/package.json`) | 4.4.0 | manual via template download |
| Playwright MCP | live-mutation UAT (main agent) | (asumsi tersedia ke main agent) | — | UAT manual oleh user bila MCP tak ada |
| Chromium (Playwright) | run spec | ✓ (project chromium) | — | — |

**Missing dependencies with no fallback:** None terdeteksi.
**Missing dependencies with fallback:** Playwright MCP (jika tak ada → UAT manual user); konfirmasi saat eksekusi.

## Validation Architecture

> `.planning/config.json` tidak dibaca eksplisit di sesi ini; nyquist_validation diasumsikan enabled (key absen = enabled). Bila `false`, abaikan section ini.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright @playwright/test 1.58.2 (e2e) + xUnit (`dotnet test`, backend) |
| Config file | `tests/playwright.config.ts` (baseURL localhost:5277, fullyParallel:false, projects setup→chromium) |
| Quick run command | `cd tests && npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1` |
| Full suite command | `dotnet test` (backend) + `cd tests && npx playwright test --workers=1` (e2e penuh) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DSN-06 (C1-C6) | mutasi CoachCoacheeMapping | manual-UAT (live, Playwright MCP) + struktural smoke | (live MCP) + `npx playwright test coachcoacheemapping-389 --workers=1` | ✅ spec ada (V-10..V-14, perlu promote + V-15..V-17 baru) |
| DSN-06 (C7 import) | import Excel | manual-UAT | (upload fixture) | ❌ fixture `tests/fixtures/import-mapping-390.xlsx` perlu dibuat (Wave 0) |
| DSN-06 (C8/C9 export/template) | export/template Excel | auto | `npx playwright test coachcoacheemapping-389 --workers=1` (V-15/V-16 baru) | ❌ test export belum ada (tambah) |
| DSN-06 (W1-W5) | filter/threshold/approve/skip | auto (non-destruktif) + live UAT (mutasi) | `npx playwright test coachworkload-388 --workers=1` | ✅ spec ada (perlu W-EXP/W-THR/W-ERR baru) |
| DSN-06 (no-regression) | suite backend tidak regresi | auto | `dotnet test` | ✅ existing |

### Sampling Rate
- **Per task commit:** spec relevan single-file `npx playwright test <spec> --workers=1` + `dotnet build`.
- **Per wave merge:** `npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1` + `dotnet test`.
- **Phase gate:** full suite hijau + UAT live roundtrip + Excel export/import sebelum push.

### Wave 0 Gaps
- [ ] `tests/fixtures/import-mapping-390.xlsx` — fixture import valid (2 kolom NIP Coach/NIP Coachee, NIP existing). Cover C7.
- [ ] Promote V-10..V-14 + tambah V-15 (export CoachCoacheeMapping.xlsx), V-16 (template), V-17 (console-error gate) di `coachcoacheemapping-389.spec.ts`.
- [ ] Tambah W-EXP (coach_workload.xlsx), W-THR (threshold modal non-destruktif), W-ERR (console-error) di `coachworkload-388.spec.ts`.
- [ ] (Tidak ada framework install — Playwright/xUnit sudah ada.)

## Security Domain

> v32.1 = pure view/test. Tidak menambah surface auth/crypto/input baru. Section ringkas (tidak ada perubahan kontrol; hanya verifikasi kontrol existing tetap utuh).

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes (verifikasi tetap) | Cookie auth + role-gate `[Authorize(Roles="Admin, HC")]` / `[Authorize(Roles="Admin")]` — JANGAN ubah |
| V4 Access Control | yes (verifikasi tetap) | Set Threshold + approve/skip = Admin-only (role-gate UI `User.IsInRole("Admin")` + controller `[Authorize(Roles="Admin")]`); HC read-only notice |
| V5 Input Validation | yes (existing) | Import header validation (NIP Coach/Coachee) + ext/size check (controller, unchanged) |
| V6 Cryptography | no | — |
| CSRF | yes (verifikasi tetap) | Semua POST AJAX kirim `RequestVerificationToken`; form Import/Graduated `@Html.AntiForgeryToken()`; endpoint `[ValidateAntiForgeryToken]` — parity = token tetap terkirim |

### Known Threat Patterns for stack (ASP.NET MVC + AJAX + ClosedXML)
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF pada mutasi (assign/edit/delete/threshold/approve) | Tampering | AntiForgeryToken (sudah; verifikasi parity tetap kirim token via `appUrl` fetch) |
| Privilege escalation (HC/non-admin set threshold) | Elevation | Role-gate UI + controller `[Authorize(Roles="Admin")]` (verifikasi tombol tak muncul untuk non-admin) |
| Malicious .xlsx upload | Tampering/DoS | Ext whitelist `.xlsx/.xls` + 10MB cap + header validation (controller, unchanged) |

**Catatan:** 390 TIDAK mengubah kontrol keamanan apa pun. Verifikasi = kontrol existing tetap berfungsi pasca-redesign (mis. token masih terkirim, role-gate masih efektif).

## Sources

### Primary (HIGH confidence — verified langsung sesi ini)
- `Views/Admin/CoachCoacheeMapping.cshtml` (1077 baris) — toolbar L48-72, accordion L240-365, modal L393-622, Scripts L624-1046, import modal L1048-1077.
- `Views/Admin/CoachWorkload.cshtml` (545 baris) — header/export/threshold L37-55, filter card L124-147, chart L165-178, saran card L244-292, threshold modal L294-328, Scripts L330-545.
- `Controllers/CoachMappingController.cs` — `[Route("Admin/[action]")]` L14; endpoints L168-1354 (template/import/assign/edit/deactivate/reactivate/delete/preview/export); CoachWorkload/SetWorkloadThreshold/Approve/Skip/Export L1628-1770.
- `tests/e2e/coachcoacheemapping-389.spec.ts` (V-01..V-14) + `tests/e2e/coachworkload-388.spec.ts` (5 test).
- `tests/playwright.config.ts` (workers/baseURL/projects); `tests/e2e/global.setup.ts` + `global.teardown.ts` (matrix seed/restore); `tests/helpers/{accounts,dbSnapshot,auth,utils}.ts`; `tests/package.json`.
- `docs/SEED_WORKFLOW.md` (§5 BACKUP/RESTORE) + `docs/SEED_JOURNAL.md` (format); `CLAUDE.md` (Develop + Seed Workflow).
- `Views/Shared/_Layout.cshtml:54-55` (`appUrl`/`basePath`); `Views/Account/Login.cshtml:173,181,195`.
- `.planning/{REQUIREMENTS,ROADMAP,STATE}.md` + CONTEXT 388/389/390.
- `git log` / `git status` (dependency 389-02 ter-commit `aa377982`).

### Secondary (MEDIUM — memory referensi proyek)
- `reference_local_e2e_sql_env_fix` (workers=1, SQLBrowser, lpc shared-memory).
- `reference_dev_credentials` (admin@pertamina.com untuk /Admin/*).
- Phase 355 lesson (`Authentication__UseActiveDirectory=false`).

### Tertiary (LOW)
- None.

## Metadata

**Confidence breakdown:**
- Parity inventory (stack/aksi): HIGH — dibaca langsung dari view + controller + grep endpoint.
- Spec escalation: HIGH — spec dibaca penuh; promosi konkret per test.
- Live-UAT + snapshot/restore: HIGH untuk command (SEED_WORKFLOW + dbSnapshot verified); MEDIUM untuk ketersediaan data overload lokal (A3).
- Excel schema: HIGH — header/filename dibaca dari controller.
- Env gotchas: HIGH — config + spec header + memory konsisten.

**Research date:** 2026-06-17
**Valid until:** 2026-07-17 (stabil; view/test berubah hanya bila 390 mengedit — re-check bila view di-touch lagi)
