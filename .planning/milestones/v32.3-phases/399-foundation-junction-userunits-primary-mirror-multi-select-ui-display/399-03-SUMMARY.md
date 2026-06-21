---
phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display
plan: 03
subsystem: ui
tags: [asp-net-core-mvc, razor, bootstrap-5, vanilla-js, cascade-widget, checkbox-list, primary-radio, mu-07-modal, playwright, a11y]

# Dependency graph
requires:
  - phase: 399-02
    provides: "ManageUserViewModel.Units/PrimaryUnit/ConfirmedDeactivate/ImpactedMappings; WorkerController POST wire SyncUserUnitsAsync (name=Units + PrimaryUnit binding); ViewBag.SectionUnitsJson + ViewBag.NeedConfirm; EvaluateRemoveUnitGuardAsync (MU-07 asimetris)"
provides:
  - "initSectionUnitMultiCascade(opts) di shared-cascade.js — checkbox-list Unit + radio Utama per baris, state machine UI-SPEC §A (8 state), no AJAX (reuse SectionUnitsJson dict)"
  - "Widget multi-select Unit di CreateWorker + EditWorker (Bagian tetap single <select>; Unit → #unitMultiContainer role=group)"
  - "MU-07 confirm modal di EditWorker (ViewBag.NeedConfirm → resubmit ConfirmedDeactivate=true; PROTON hard-block via asp-validation-summary merah)"
  - "EditWorker GET pre-fill Model.Units/PrimaryUnit dari UserUnits junction (round-trip: reopen Edit → boxes pre-checked + primary marked)"
  - "Spec Playwright tests/e2e/multiunit-widget-399.spec.ts (9 widget test + 1 fixture-skip) — runtime green"
affects: [phase-400, phase-401, phase-402, phase-404, account-controller-display, _PSign]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Cascade widget varian: extend shared-cascade.js (tambah fungsi, jangan ganti initSectionUnitCascade/togglePassword) — render checkbox-list + radio dari dict client-side, no AJAX (D-01)"
    - "State machine UI-SPEC §A wired di JS: default primary=first checked (D-02), promote-on-uncheck, disable+clear radio, reset-on-Bagian-change (invariant #1)"
    - "MU-07 server round-trip modal: render bila ViewBag.NeedConfirm; tombol submit form=editWorkerForm name=ConfirmedDeactivate value=true (no new endpoint)"
    - "GET pre-fill VM dari junction supaya widget round-trip (controller view-binding necessity)"
    - "Playwright data-driven pickSectionWithUnits (no hardcode Bagian); cleanup via global.teardown RESTORE (snapshot global.setup)"

key-files:
  created:
    - .planning/phases/399-foundation-junction-userunits-primary-mirror-multi-select-ui-display/399-03-SUMMARY.md
    - tests/e2e/multiunit-widget-399.spec.ts
  modified:
    - wwwroot/js/shared-cascade.js
    - Views/Admin/CreateWorker.cshtml
    - Views/Admin/EditWorker.cshtml
    - Controllers/WorkerController.cs

key-decisions:
  - "EditWorker GET ditambah pre-fill Model.Units/PrimaryUnit dari UserUnits junction (Rule 3 - blocking): tanpa ini widget Edit buka tanpa centang → round-trip W-06 gagal. 399-02 hanya menulis (POST); membaca-untuk-form belum ada. Murni view-binding necessity (no logic baru, tidak menyentuh write-through/guard 399-02)."
  - "Widget JS HTML-escape nama unit (esc()) sebelum innerHTML (T-399-03-04 residual ditutup, bukan accept) — defense-in-depth walau nama unit admin-curated."
  - "Placeholder copy diambil via container data-placeholder attribute (di-set di view dgn @OrgLabels.GetLabel) supaya JS bebas hardcode label org-tier; fallback string Bahasa Indonesia bila atribut absen."
  - "W-09 (MU-07 modal Playwright) di-skip fixture-guarded: butuh coach-mapping aktif pada pekerja multi-unit yang belum ada seed deterministik. Logika server (EvaluateRemoveUnitGuardAsync) sudah GREEN di unit test 399-02 (RemoveUnitGuardTests 5 fakta); modal markup verified via grep + build. UAT manual / Phase 402 seed."

patterns-established:
  - "Pattern: extend shared-cascade.js dgn varian fungsi (bukan ganti) — checkbox-list + primary radio state machine"
  - "Pattern: widget dinamis Razor WAJIB Playwright runtime (Lesson Phase 354) — 9 assert state machine UI-SPEC §A hijau headless"

requirements-completed: [MU-01, MU-02]

# Metrics
duration: ~9min
completed: 2026-06-18
---

# Phase 399 Plan 03: Multi-Select Unit Widget (Checkbox-List + Primary Radio) + MU-07 Modal Summary

**Widget multi-select Unit (`initSectionUnitMultiCascade` — checkbox-list + radio Utama per baris, state machine UI-SPEC §A 8-state) di-render client-side dari `ViewBag.SectionUnitsJson` (no AJAX) di CreateWorker/EditWorker; Bagian tetap single `<select>`; MU-07 confirm modal (coach-mapping aktif → re-prompt; PROTON aktif → error merah hard-block); EditWorker GET pre-fill `Units`/`PrimaryUnit` dari junction untuk round-trip; runtime Playwright 9/9 hijau (round-trip 2-unit Create→Edit + default-primary D-02 + promote-on-uncheck + a11y) — DB di-snapshot+RESTORE (baseline UserUnits=6).**

## Performance

- **Duration:** ~9 menit
- **Started:** 2026-06-18T05:31:27Z
- **Completed:** 2026-06-18T05:40:33Z
- **Tasks:** 4 (3 auto + 1 checkpoint:human-verify dijalankan headless)
- **Files modified:** 5 (1 created SUMMARY + 1 created spec + 3 modified: JS + 2 view; +1 controller GET pre-fill)

## Accomplishments
- **`initSectionUnitMultiCascade(opts)`** ditambah di `shared-cascade.js` (extend — `initSectionUnitCascade` + `togglePassword` UTUH). Render satu baris per unit Bagian: checkbox `name="Units"` + radio `name="PrimaryUnit"` + label `for`. State machine UI-SPEC §A WIRED PENUH: Bagian kosong → placeholder muted; pilih Bagian → checkbox-list; centang → radio enabled; default primary = first checked (D-02); uncheck primary → promote ke checked berikutnya; uncheck → radio disabled+clear; ganti Bagian → reset pilihan (invariant #1). Nama unit di-HTML-escape (T-399-03-04).
- **Widget terpasang di CreateWorker + EditWorker** (Bagian single `<select id=sectionSelect>` TETAP). Unit `<select id=unitSelect>` diganti `#unitMultiContainer` (`role=group`, `aria-label`, `border rounded p-2`, max-height scroll), label `@OrgLabels.GetLabel(1) Penugasan`, helper + `asp-validation-for` Units/PrimaryUnit. Init swap ke varian multi (selectedUnits + primaryUnit dari Model). Idiom form + tombol "Simpan Pekerja"/"Simpan Perubahan" tak berubah; tak ada inline font-size magic (DSN-05).
- **MU-07 modal di EditWorker** (UI-SPEC §D): render bila `ViewBag.NeedConfirm == true` — heading "Konfirmasi Penghapusan {Unit}", daftar `Model.ImpactedMappings`, tombol "Ya, Hapus & Nonaktifkan" (`submit form=editWorkerForm name=ConfirmedDeactivate value=true`) + "Batal". PROTON hard-block (D-11) tampil via `ModelState[""]` merah di validation summary existing (server `EvaluateRemoveUnitGuardAsync` 399-02). Backdrop ditambah.
- **EditWorker GET pre-fill** (Rule 3): `Model.Units` = baris junction; `Model.PrimaryUnit` = baris IsPrimary → widget Edit pre-check boxes + primary radio (round-trip). 399-02 hanya menulis (POST); membaca-untuk-form belum ada — view-binding necessity, no logic baru.
- **Spec Playwright `multiunit-widget-399.spec.ts`** (9 widget test + 1 fixture-skip) ditulis test-first lalu dijalankan headless `--workers=1`. Assert diturunkan dari UI-SPEC §A state machine + §Accessibility. Data-driven `pickSectionWithUnits` (no hardcode Bagian). **Runtime 9/9 hijau** (W-09 MU-07 skip fixture).

## Task Commits

1. **Task 1: initSectionUnitMultiCascade (state machine UI-SPEC §A)** — `2a2767aa` (feat)
2. **Task 2: Widget Create/Edit + MU-07 modal + EditWorker GET pre-fill** — `b3756903` (feat)
3. **Task 3: Spec Playwright multiunit-widget-399 (test-first)** — `60aad1ab` (test)
4. **Task 4: Runtime verify — scope submit selector (W-06/W-07 hijau)** — `03a775c6` (test)

**Plan metadata:** (docs commit final — SUMMARY + STATE + ROADMAP + REQUIREMENTS)

## Files Created/Modified
- `wwwroot/js/shared-cascade.js` — + `initSectionUnitMultiCascade(opts)` (render + state machine + a11y + esc); `initSectionUnitCascade`/`togglePassword` UTUH.
- `Views/Admin/CreateWorker.cshtml` — Unit single-select → `#unitMultiContainer` widget + init multi-cascade.
- `Views/Admin/EditWorker.cshtml` — idem + modal MU-07 (`ViewBag.NeedConfirm`) + backdrop.
- `Controllers/WorkerController.cs` — EditWorker GET pre-fill `Model.Units`/`Model.PrimaryUnit` dari `UserUnits` (round-trip).
- `tests/e2e/multiunit-widget-399.spec.ts` — 9 widget test (W-01..W-08 + W-09 skip).

## Runtime Verification (Playwright headless, localhost:5277, AD off)

| Test | State machine UI-SPEC §A | Hasil |
|------|--------------------------|-------|
| W-01 | Bagian kosong → placeholder muted, 0 checkbox | PASS |
| W-02 | pilih Bagian → checkbox-list count == jumlah unit (Units+PrimaryUnit binding 1:1) | PASS |
| W-03 | centang unit → radio Utama enabled (baseline disabled) | PASS |
| W-04 | centang pertama → radio auto-checked (default primary D-02); tepat 1 checked | PASS |
| W-05 | uncheck primary → promote ke checked berikutnya; radio uncheck disabled | PASS |
| W-06 | **round-trip 2-unit Create→Edit**: buat pekerja (primary=unitB) → reopen Edit → unitA+unitB checked + primary=unitB | PASS |
| W-07 | 0 unit dicentang → submit valid (sampai ManageWorkers, no error primary) | PASS |
| W-08 | a11y: role=group + aria-label + label[for] tiap kontrol + radio disabled native | PASS |
| W-09 | MU-07 modal (fixture coach-mapping aktif) | SKIP (fixture-guarded — UAT manual / Phase 402 seed) |

**Hasil:** 9 passed, 1 skipped (29.7s). DB di-snapshot (global.setup) → RESTORE (global.teardown) → SEED_JOURNAL cleaned, Layer 4 = 0 matrix rows, `UserUnits` kembali baseline = 6 (pekerja round-trip temporary bersih). App boot localhost:5277 HTTP 200; `dotnet build` 0 error/0 warning; suite 366/366.

## Decisions Made
- **EditWorker GET pre-fill** (lihat key-decisions) — satu-satunya sentuhan controller, Rule 3 blocking round-trip. Tidak menyentuh write-through/guard 399-02.
- **HTML-escape nama unit** di JS render — T-399-03-04 ditutup (mitigate), bukan accept residual.
- **W-09 fixture-skip** — logika MU-07 server sudah GREEN unit test 399-02; markup modal verified grep+build; jalur penuh = UAT manual Task 4 atau seed Phase 402.

## CLAUDE.md Develop Workflow Gate
- ✅ `dotnet build HcPortal.csproj` — 0 error / 0 warning (Razor compile Create/Edit + controller).
- ✅ `dotnet run` localhost:5277 — HTTP 200 (AD off `Authentication__UseActiveDirectory=false`).
- ✅ Cek DB lokal — `UserUnits` baseline 6 (round-trip worker cleaned via RESTORE).
- ✅ Playwright `multiunit-widget-399 --workers=1` — 9/9 hijau (W-09 skip fixture). SQLBrowser + SQLEXPRESS running.
- ✅ Seed Data Workflow — snapshot (setup) → test → RESTORE (teardown), SEED_JOURNAL cleaned (harness-managed).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] EditWorker GET tidak populate Model.Units/PrimaryUnit → round-trip W-06 gagal**
- **Found during:** Task 2 (saat menyiapkan round-trip)
- **Issue:** 399-02 mewire write-through (POST consume `model.Units`) tapi EditWorker GET hanya set scalar `model.Unit` — `model.Units`/`model.PrimaryUnit` kosong → widget Edit buka tanpa centang; round-trip mustahil.
- **Fix:** EditWorker GET load `UserUnits` junction → set `model.Units` (semua unit) + `model.PrimaryUnit` (baris IsPrimary). View-binding necessity, no logic baru, tidak menyentuh write-through/guard 399-02.
- **Files modified:** `Controllers/WorkerController.cs` (EditWorker GET)
- **Commit:** `b3756903`

**2. [Rule 1 - Bug] Spec `button[type=submit]` resolve 2 elemen (dropdown-item delete hidden)**
- **Found during:** Task 4 (run pertama — W-06/W-07 timeout)
- **Issue:** Playwright `page.click('button[type=submit]')` ambil tombol hidden `dropdown-item text-danger` (navbar) lebih dulu → not-visible timeout. Widget tidak bermasalah (W-01..W-05, W-08 hijau).
- **Fix:** scope selector ke `#createWorkerForm button[type=submit]`.
- **Files modified:** `tests/e2e/multiunit-widget-399.spec.ts`
- **Commit:** `03a775c6`

## Issues Encountered
- Tidak ada di luar 2 auto-fix di atas. Widget render + state machine + round-trip benar pada run pertama (kegagalan murni selector spec, bukan defect widget).

## Known Stubs
None. Widget bind data riil (`ViewBag.SectionUnitsJson` + `Model.Units`/`PrimaryUnit`); MU-07 modal render `Model.ImpactedMappings` riil. W-09 di-skip karena fixture absen (didokumentasikan, bukan stub UI). Display surfaces (Profile/WorkerDetail/Settings/ManageWorkers/Home/`_PSign`) = scope Plan 04 (sekuensing terdokumentasi).

## Threat Surface Scan
Tidak ada surface keamanan baru di luar `<threat_model>` plan. T-399-03-01/02 (tampering PrimaryUnit/bypass modal) tetap dimitigasi server-side oleh 399-02 (widget hanya UX, tidak dipercaya). T-399-03-03 (akses non-Admin/HC) = `[Authorize(Admin,HC)]` di controller, view tak menambah surface. T-399-03-04 (XSS innerHTML nama unit) DITUTUP (mitigate) via `esc()` HTML-escape di JS render (bukan accept residual).

## Next Phase Readiness
- **Widget multi-select + round-trip SIAP.** Operator dapat assign >1 unit (MU-01) dgn penanda primary eksplisit (MU-02) lewat UI; write-through 399-02 menyimpan.
- **Plan 04 (display surfaces)** belum tersentuh (scope boundary dijaga) — konsumsi `Model.Units`/`PrimaryUnit` + `ViewBag.UserUnitsDict` (399-02) + `_PSign`.
- **Carry:** migration=TRUE (`AddUserUnitsTable` `fc015f4d`, Plan 01) — notify IT saat milestone push. Plan 03 = 0 migration.
- **Tidak ada blocker.**

## Self-Check: PASSED

- 5/5 file diverifikasi ada di disk (SUMMARY + spec + JS + 2 view + controller).
- 4/4 commit task diverifikasi di git log (2a2767aa, b3756903, 60aad1ab, 03a775c6).
- Build 0 error/0 warning; app boot localhost:5277 HTTP 200; Playwright 9/9 hijau (1 fixture-skip); DB baseline restored (UserUnits=6); suite 366/366.

---
*Phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display*
*Plan: 03*
*Completed: 2026-06-18*
