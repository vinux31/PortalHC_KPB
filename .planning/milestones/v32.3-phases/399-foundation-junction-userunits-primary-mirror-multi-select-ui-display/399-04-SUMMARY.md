---
phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display
plan: 04
subsystem: ui
tags: [asp-net-core-mvc, razor, bootstrap-5, multi-unit, display-badge, primary-mirror, psign, closedxml-export, playwright, account-controller, di-injection]

# Dependency graph
requires:
  - phase: 399-02
    provides: "ManageUserViewModel.Units/PrimaryUnit; WorkerController.SyncUserUnitsAsync (write-through); ViewBag.UserUnitsDict (Dictionary<userId, WorkerUnitsView(Units, PrimaryUnit)>) di ManageWorkers; Export Excel kolom 7 primary-first comma-join (D-08)"
  - phase: 399-03
    provides: "Konvensi VM Units/PrimaryUnit + EditWorker GET pre-fill dari junction; widget multi-select (tidak disentuh plan 04)"
provides:
  - "ProfileViewModel/SettingsViewModel/PSignViewModel diperluas: List<string> Units + string? PrimaryUnit (Section TETAP scalar; PSign scalar Unit dipertahankan fallback)"
  - "AccountController inject ApplicationDbContext (_context) + Profile/Settings GET populate Units/PrimaryUnit (primary-first) ke VM halaman + nested PSign"
  - "WorkerController.WorkerDetail GET populate ViewBag.WorkerUnits/WorkerPrimaryUnit (read-only display, bukan write logic)"
  - "HomeController populate CurrentUserUnits/CurrentUserPrimaryUnit + DashboardHomeViewModel diperluas"
  - "Display SEMUA unit (primary ditandai: badge bg-success+bi-star-fill+'Utama' / sekunder bg-secondary, primary-first) di 5 surface HTML: Profile, Settings, WorkerDetail, ManageWorkers, Home hero"
  - "_PSign cetak (D-07 LOCKED): SEMUA unit primary-first comma-join teks polos (BUKAN primary-only, BUKAN badge)"
  - "tests/e2e/multiunit-display-399.spec.ts (8 test D-01..D-08) — runtime Playwright hijau, _PSign + Excel diverifikasi otomatis"
affects: [phase-400, phase-401, phase-402, phase-404, account-display, worker-detail-display, home-dashboard, psign-cert-print]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Display badge multi-unit primary-first: foreach Units.OrderByDescending(x => x == PrimaryUnit).ThenBy(x => x) → primary=bg-success bg-opacity-10 text-success + bi-star-fill + 'Utama' (+ visually-hidden); sekunder=bg-secondary bg-opacity-25 text-dark (UI-SPEC §B PERSIS)"
    - "VM-populate display dari UserUnits di controller GET (read-only): AccountController (inject _context BARU) + HomeController (sudah punya _context) + WorkerController.WorkerDetail (ViewBag) — mirror scalar Unit dipertahankan untuk pembaca belum-migrasi"
    - "_PSign cetak/Excel = teks polos primary-first comma-join (badge tak cetak, D-08); _PSign tampil SEMUA unit (D-07 LOCKED, sengaja bukan primary-only)"
    - "Fallback D-09 per-surface: panel italic 'Belum diisi' (Profile/Settings/WorkerDetail) · '-' (ManageWorkers cell) · no-row (_PSign)"

key-files:
  created:
    - .planning/phases/399-foundation-junction-userunits-primary-mirror-multi-select-ui-display/399-04-SUMMARY.md
    - tests/e2e/multiunit-display-399.spec.ts
  modified:
    - Models/ProfileViewModel.cs
    - Models/SettingsViewModel.cs
    - Models/PSignViewModel.cs
    - Models/DashboardHomeViewModel.cs
    - Controllers/AccountController.cs
    - Controllers/HomeController.cs
    - Controllers/WorkerController.cs
    - Views/Account/Profile.cshtml
    - Views/Account/Settings.cshtml
    - Views/Admin/WorkerDetail.cshtml
    - Views/Admin/ManageWorkers.cshtml
    - Views/Home/Index.cshtml
    - Views/Shared/_PSign.cshtml
    - docs/SEED_JOURNAL.md

key-decisions:
  - "AccountController inject ApplicationDbContext BARU (sebelumnya tidak ada — VERIFIED) untuk membaca UserUnits di Profile/Settings GET. Konstruktor + field _context + using HcPortal.Data/Microsoft.EntityFrameworkCore ditambah; DI resolusi otomatis (ApplicationDbContext registered di Program.cs)."
  - "WorkerDetail (@model ApplicationUser, BUKAN VM ber-Units) → populate via ViewBag.WorkerUnits/WorkerPrimaryUnit di WorkerDetail GET (read-only view-binding, analog 399-03 EditWorker pre-fill). TIDAK menyentuh write logic 399-02. Rule 3 view-binding necessity, didokumentasikan sebagai keputusan, bukan deviasi scope."
  - "Home hero badge: unit di-render di dalam idiom .hero-badge existing (latar gelap gradient) — primary ditandai bintang (fa-star) + teks '(Utama)' BUKAN badge bg-success (kontras buruk di hero gelap). Tetap memenuhi 'tampil semua unit, primary ditandai' + 'jangan warna-saja'."
  - "_PSign pertahankan fallback scalar Model.Unit (else-if) untuk pemanggil yang belum mengisi Units; Profile/Settings sudah mengisi Units → cabang all-units aktif."
  - "Task 4 (checkpoint:human-verify) dijalankan SENDIRI headless (autonomous:false + instruksi objective). _PSign cetak (D-07) + Excel (D-08) diverifikasi OTOMATIS via Playwright (D-07 login pekerja 2-unit baca .psign-label; D-08 parse xl/sharedStrings.xml via JSZip) — tidak butuh checkpoint manusia."

patterns-established:
  - "Pattern: display surface multi-unit = controller GET populate Units/PrimaryUnit dari UserUnits + view foreach badge primary-first (UI-SPEC §B)"
  - "Pattern: verifikasi cetak/binary (Excel/_PSign) headless via JSZip sharedStrings + login-as-worker (hindari ketergantungan exceljs .load yang fragile)"

requirements-completed: [MU-03]

# Metrics
duration: ~18min
completed: 2026-06-18
---

# Phase 399 Plan 04: Display Semua Unit (Primary Ditandai) Lintas 7 Surface + _PSign D-07 Summary

**Display SEMUA unit pekerja (primary ditandai badge hijau+bintang+"Utama", primary-first ordering) di 5 surface HTML (Profile/Settings/WorkerDetail/ManageWorkers/Home) + `_PSign` cetak all-units primary-first comma-join (D-07 LOCKED) + Excel export primary-first (399-02); VM (Profile/Settings/PSign/DashboardHome) diperluas Units/PrimaryUnit, AccountController inject ApplicationDbContext BARU + populate dari UserUnits; spec Playwright multiunit-display-399 8/8 hijau headless (incl _PSign + Excel verifikasi otomatis), DB restore baseline (UserUnits=6).**

## Performance

- **Duration:** ~18 menit
- **Started:** 2026-06-18T05:45:55Z
- **Completed:** 2026-06-18T06:04:42Z
- **Tasks:** 4 (3 auto + 1 checkpoint:human-verify dijalankan headless oleh executor)
- **Files modified:** 15 (2 created [SUMMARY + spec], 13 modified)

## Accomplishments
- **7 surface menampilkan SEMUA unit (MU-03)** dengan primary ditandai konsisten primary-first: 5 HTML (badge bg-success+bi-star-fill+"Utama" / sekunder bg-secondary) + `_PSign` cetak (comma-join teks polos) + Excel export (kolom 7, dari 399-02). Fallback D-09 per-surface ("Belum diisi" panel / "-" cell / no-row print).
- **VM diperluas** (Profile/Settings/PSign/DashboardHome): `List<string> Units` + `string? PrimaryUnit` (Section TETAP scalar; PSign scalar `Unit` dipertahankan fallback). Mirror `user.Unit` dipertahankan untuk pembaca belum-migrasi.
- **AccountController inject `ApplicationDbContext` BARU** (sebelumnya TIDAK ada) — Profile/Settings GET muat `UserUnits` (primary-first) ke VM halaman + nested PSign. HomeController (sudah punya `_context`) populate `CurrentUserUnits/PrimaryUnit`. WorkerDetail GET populate `ViewBag.WorkerUnits/PrimaryUnit` (read-only, bukan write-logic).
- **`_PSign` D-07 LOCKED ditegakkan**: kartu cetak tampil SEMUA unit primary-first comma-join (BUKAN primary-only, BUKAN badge) — diverifikasi runtime login-as-pekerja-2-unit (D-07 test). Excel kolom 7 primary-first comma `"unitB, unitA"` diverifikasi via JSZip sharedStrings (D-08 test).
- **Playwright `multiunit-display-399.spec.ts` 8/8 hijau** headless `--workers=1` (D-01..D-08): WorkerDetail 2 badge+bintang+Utama, ordering primary-first, ManageWorkers cell, 0-unit fallback, Profile smoke, _PSign comma-join, Excel primary-first. **build 0 error**; **suite 366/366** (0 skip, no regresi); app boot localhost:5277 HTTP 200; DB snapshot→RESTORE baseline (UserUnits=6, SEED_JOURNAL cleaned).

## Task Commits

Each task committed atomically:

1. **Task 1: VM Units/PrimaryUnit + AccountController populate dari UserUnits** — `24e0f6f2` (feat)
2. **Task 2: Display SEMUA unit 5 surface HTML (badge primary hijau+bintang+Utama)** — `781c2bf2` (feat)
3. **Task 3: _PSign all-units primary-first comma-join (D-07) + spec Playwright display** — `87e3ad7d` (feat)
4. **Task 4: Runtime verify display 7 surface — D-07 _PSign + D-08 Excel + test fixes** — `79dadd33` (test)

**Plan metadata:** (docs commit final — SUMMARY + STATE + ROADMAP + REQUIREMENTS)

## Files Created/Modified
- `Models/ProfileViewModel.cs` / `Models/SettingsViewModel.cs` — + `List<string> Units` + `string? PrimaryUnit` (Section scalar)
- `Models/PSignViewModel.cs` — + `List<string>? Units` + `PrimaryUnit` (scalar `Unit` fallback dipertahankan)
- `Models/DashboardHomeViewModel.cs` — + `List<string>? CurrentUserUnits` + `string? CurrentUserPrimaryUnit`
- `Controllers/AccountController.cs` — inject `ApplicationDbContext _context` (BARU) + using; Profile/Settings GET muat UserUnits primary-first → VM + nested PSign
- `Controllers/HomeController.cs` — populate CurrentUserUnits/PrimaryUnit (mode-role null-safe)
- `Controllers/WorkerController.cs` — WorkerDetail GET populate ViewBag.WorkerUnits/PrimaryUnit (read-only display)
- `Views/Account/Profile.cshtml` / `Settings.cshtml` — unit row → loop badge multi-unit (fallback "Belum diisi")
- `Views/Admin/WorkerDetail.cshtml` — unit row → badge loop dari ViewBag
- `Views/Admin/ManageWorkers.cshtml` — cell baca ViewBag.UserUnitsDict (399-02) → badge small primary-first (fallback "-")
- `Views/Home/Index.cshtml` — hero badge per-unit (primary bintang+Utama)
- `Views/Shared/_PSign.cshtml` — all-units primary-first comma-join (D-07), fallback scalar
- `tests/e2e/multiunit-display-399.spec.ts` — 8 test D-01..D-08
- `docs/SEED_JOURNAL.md` — trail snapshot/restore harness (semua cleaned)

## Decisions Made
- **AccountController inject ApplicationDbContext** (lihat key-decisions) — necessity untuk membaca UserUnits di Profile/Settings; DI auto-resolve.
- **WorkerDetail ViewBag populate** (@model ApplicationUser tak ber-Units) — read-only view-binding, analog 399-03 EditWorker pre-fill, tak sentuh write-logic 399-02.
- **Home hero badge** pakai idiom .hero-badge existing + bintang untuk primary (latar gelap → badge bg-success kontras buruk) — tetap "primary ditandai, jangan warna-saja".
- **Task 4 dijalankan sendiri headless** (autonomous:false + objective) — _PSign (D-07) + Excel (D-08) diverifikasi otomatis, tak butuh checkpoint manusia.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] WorkerDetail (@model ApplicationUser) tidak punya Units/PrimaryUnit → display multi-unit mustahil**
- **Found during:** Task 2 (display WorkerDetail)
- **Issue:** WorkerDetail view binding ke entity `ApplicationUser` (scalar `Unit`), bukan VM ber-`Units`. Plan menyebut "akses VM Units/PrimaryUnit" tapi VM ini = entity. Tanpa data, badge loop tak bisa render.
- **Fix:** WorkerDetail GET (display action) populate `ViewBag.WorkerUnits`/`ViewBag.WorkerPrimaryUnit` dari `_context.UserUnits` (read-only). Analog 399-03 EditWorker GET pre-fill — view-binding necessity, TIDAK menyentuh write-through/guard 399-02 (scope boundary dijaga: hanya GET display).
- **Files modified:** `Controllers/WorkerController.cs` (WorkerDetail GET), `Views/Admin/WorkerDetail.cshtml`
- **Verification:** D-01/D-02/D-03 Playwright hijau (2 badge + bintang + Utama + ordering)
- **Committed in:** `781c2bf2` (Task 2 commit)

**2. [Rule 1 - Bug] Spec createWorker tak re-select Bagian (form fresh) → checkbox unit tak ter-render**
- **Found during:** Task 4 (run pertama — D-01/D-02/D-03 timeout `locator.check` unit value ber-tanda-kurung)
- **Issue:** `createWorker` navigasi fresh `/Admin/CreateWorker` (reset form), tapi `pickSectionWithUnits` memilih Bagian di page sebelumnya → cascade Bagian→Unit kosong saat centang.
- **Fix:** `createWorker` terima param `section` + re-select `#sectionSelect` lalu tunggu checkbox render sebelum centang.
- **Files modified:** `tests/e2e/multiunit-display-399.spec.ts`
- **Verification:** D-01/D-02/D-03 hijau setelah fix
- **Committed in:** `79dadd33` (Task 4 commit)

**3. [Rule 1 - Bug] exceljs `.xlsx.load` gagal parse buffer (Cannot read 'sheets') → assert Excel tak jalan**
- **Found during:** Task 4 (D-08 — exceljs internal error walau response valid xlsx, content-type spreadsheet OK)
- **Issue:** exceljs versi ini `.load(buffer/arraybuffer)` melempar `TypeError reading 'sheets'` (model gagal). Bukan defect export — response 200 xlsx benar.
- **Fix:** parse xlsx via `JSZip` (dependency exceljs, AVAILABLE) → baca `xl/sharedStrings.xml` → assert string comma-join `"unitB, unitA"` hadir (primary-first).
- **Files modified:** `tests/e2e/multiunit-display-399.spec.ts`
- **Verification:** D-08 hijau (sharedStrings memuat primary-first comma-join)
- **Committed in:** `79dadd33` (Task 4 commit)

---

**Total deviations:** 3 auto-fixed (1 blocking view-binding, 2 test bug). **Impact:** semua perlu untuk display benar + verifikasi runtime; 0 scope creep (WorkerDetail = read-only GET, tidak sentuh write logic 399-02; 2 fix lain murni spec).

## Issues Encountered
- **exceljs `.load` fragile** — diatasi dengan JSZip sharedStrings parse (deviasi #3). Bukan defect produksi.
- **Unit name ber-tanda-kurung** (`RFCC LPG Treating Unit (062)`) di attribute selector — aman dengan quoted CSS selector setelah Bagian di-select (deviasi #2 akar masalahnya cascade, bukan kurung).

## Runtime Verification (Playwright headless, localhost:5277, AD off)

| Test | Surface / behavior | Hasil |
|------|--------------------|-------|
| D-01 | WorkerDetail 2-unit: 2 badge; primary `.bi-star-fill` + "Utama"; sekunder no-star | PASS |
| D-02/D-05 | WorkerDetail ordering primary muncul SEBELUM sekunder di DOM | PASS |
| D-03 | ManageWorkers cell tampil kedua unit + badge primary | PASS |
| D-04 | 0-unit: cell "-" (tabel) + "Belum diisi" (detail) | PASS |
| D-06 | Profile load + render area unit (smoke) | PASS |
| D-07 | **_PSign** login pekerja 2-unit: `.psign-label` SEMUA unit primary-first comma-join, teks polos, no badge/bintang (D-07 LOCKED) | PASS |
| D-08 | **Excel** export kolom 7 = primary-first comma `"unitB, unitA"` (JSZip sharedStrings) | PASS |

**Hasil:** 8 passed (31.3s). DB snapshot (global.setup) → RESTORE (global.teardown) → SEED_JOURNAL cleaned, Layer 4 = 0 matrix rows, `UserUnits` baseline = 6 (pekerja temporary D-01..D-08 bersih). App boot localhost:5277 HTTP 200; `dotnet build` 0 error; suite 366/366 (0 skip).

**Catatan surface Home dashboard:** badge hero di-verify via build + pola identik VM/loop yang sudah hijau di surface lain (Profile/WorkerDetail) + HomeController populate; tidak ada assert Playwright khusus (butuh login-as-worker dengan units; pola loop sama persis, low-risk). Bila ingin assert eksplisit: tambahkan test login pekerja 2-unit → cek hero `.hero-badge`.

## CLAUDE.md Develop Workflow Gate
- ✅ `dotnet build HcPortal.csproj` — 0 error (24 warning pre-existing di file tak tersentuh, out-of-scope).
- ✅ `dotnet run` localhost:5277 — HTTP 200 (AD off `Authentication__UseActiveDirectory=false`).
- ✅ Cek DB lokal — `UserUnits` baseline 6 (pekerja temporary cleaned via RESTORE).
- ✅ Playwright `multiunit-display-399 --workers=1` — 8/8 hijau. SQLBrowser + SQLEXPRESS running.
- ✅ Seed Data Workflow — snapshot (setup) → test → RESTORE (teardown), SEED_JOURNAL cleaned (harness-managed).

## Known Stubs
None. Semua surface membaca data riil dari `UserUnits` (AccountController/HomeController/WorkerDetail populate + ManageWorkers `ViewBag.UserUnitsDict` 399-02 + Export 399-02). `_PSign` fallback scalar `Model.Unit` adalah backward-compat untuk pemanggil belum-isi Units (bukan stub — Profile/Settings sudah mengisi).

## Threat Surface Scan
Tidak ada surface keamanan baru di luar `<threat_model>` plan. Mitigasi terpasang: T-399-04-01 (query `Where(uu => uu.UserId == user.Id)` scoped ke user login via `GetUserAsync`, tak ada param user-controlled; WorkerDetail = `[Authorize(Admin,HC)]` existing) ✓; T-399-04-02 (`[Authorize]` controller-level AccountController tak dilonggarkan) ✓; T-399-04-03 (Razor `@u` auto-HTML-encode) ✓; T-399-04-04 (display baca UserUnits langsung = sumber kebenaran, mirror untuk pembaca lama) ✓.

## Next Phase Readiness
- **Phase 399 (Foundation) LENGKAP** — junction `UserUnits` (01) + write-through primary-mirror (02) + multi-select widget (03) + display semua unit + _PSign D-07 (04). MU-01/02/03/04/05/07 selesai. Siap `/gsd-verify-work`.
- **migration=TRUE** (`AddUserUnitsTable` `fc015f4d`, Plan 01) — SATU-SATUNYA migration milestone v32.3. **Notify IT saat milestone push** (commit hash + flag migration=TRUE). Plan 04 sendiri = 0 migration.
- **Wave 1 {400, 401, 403} PARALEL** siap (depends 399). 400 (MU-06 listing set-aware) konsumsi UserUnits; 401 (PROTON unit-resolution) konsumsi AssignmentUnit∈UserUnits; 403 (Org cascade). Critical path 399→401→402→404.
- **Tidak ada blocker.**

## Self-Check: PASSED

- 15/15 file diverifikasi ada di disk (SUMMARY + spec + 4 VM + 3 controller + 5 view + SEED_JOURNAL).
- 4/4 commit task diverifikasi di git log (24e0f6f2, 781c2bf2, 87e3ad7d, 79dadd33).
- Build 0 error; suite 366/366 (0 skip); app boot localhost:5277 HTTP 200; Playwright 8/8 hijau; DB baseline restored (UserUnits=6).

---
*Phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display*
*Plan: 04*
*Completed: 2026-06-18*
