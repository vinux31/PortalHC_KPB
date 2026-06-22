---
phase: 413-test-uat
plan: 02
subsystem: testing
tags: [playwright, e2e, signalr, multi-context, seed-workflow, assessment, participant, force-kick]

# Dependency graph
requires:
  - phase: 412-live-monitoring-ui-signalr
    provides: "UI live AssessmentMonitoringDetail (picker Tambah, modal keras/ringan, panel Peserta Dikeluarkan, handler participantAdded/Removed/examRemoved) + 7 sinyal handoff Playwright-only"
  - phase: 410-add-participant-backend-live
    provides: "AddParticipantsLive + GetEligibleParticipantsToAdd"
  - phase: 411-remove-restore-backend-live
    provides: "RemoveParticipantLive (hybrid soft/hard) + RestoreParticipantLive"
  - phase: 409-data-foundation-reentry-guards
    provides: "IsParticipantRemoved guard + exclude-removed query + examRemoved force-kick"
provides:
  - "tests/e2e/flexible-participant-412.spec.ts — Playwright e2e multi-context 7 sinyal live (add/modal-keras/force-kick/panel/restore/count-exclude/multi-observer)"
  - "tests/sql/flexible-participant-413-seed.sql — seed reliable flip InProgress (punya paket, non-Proton, non-pair, milik rino, non-matrix)"
  - "Bukti runtime end-to-end fitur Flexible Add/Remove Participant berfungsi di real browser (5/5 e2e green)"
  - "Product fix: monFlashRow ReferenceError yang sebelumnya membuat seluruh UI add/remove/restore tak berfungsi"
affects: [413-03 full regression gate, milestone v32.5 close]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Multi-context SignalR e2e (browser.newContext ×2) — waitHubConnected KEDUA context sebelum fire (pola Flow O)"
    - "Seed via SEED_WORKFLOW inline beforeAll BACKUP + flip InProgress sqlcmd / afterAll RESTORE (isolasi penuh, tak bergantung global teardown matrix)"
    - "openRowHapusModal helper — buka dropdown ⋮ dulu sebelum klik dropdown-item (Bootstrap dropdown)"

key-files:
  created:
    - tests/e2e/flexible-participant-412.spec.ts
    - tests/sql/flexible-participant-413-seed.sql
  modified:
    - Views/Admin/AssessmentMonitoringDetail.cshtml
    - docs/SEED_JOURNAL.md

key-decisions:
  - "Worker login = account 'coachee' (rino.prasetyo); seed SQL memfilter UserId=rino → TIDAK perlu flip UserId sesi"
  - "Sinyal b+c+d+f digabung dalam 1 test force-kick (state-flow alami: modal keras → kick → panel → count exclude); a/e/g terpisah"
  - "Seed exclude '[MATRIX_TEST_%' — global.setup matrix seed 18 sesi (Id 9001+, sebagian milik rino) bisa salah-terpilih + picker eligible-list batch matrix hang"

patterns-established:
  - "Sebelum tiap run e2e: restart app fresh (matrix global-teardown RESTORE ROLLBACK IMMEDIATE membunuh koneksi pool app → run berikut SqlException 596 kill-state)"
  - "#tbodyRemoved row assert state:attached (panel collapse default tertutup), expand collapse sebelum klik Restore"

requirements-completed: [PART-05, PRMV-02, PRMV-04, PLIV-01, PLIV-02]

# Metrics
duration: 27min
completed: 2026-06-21
---

# Phase 413 Plan 02: Playwright e2e 7 Sinyal Live Multi-Context Summary

**Spec e2e multi-context membuktikan add/force-kick/restore/broadcast live berfungsi runtime (5/5 green) DAN menemukan + memperbaiki bug produk `flashRow is not defined` yang sebelumnya membuat seluruh UI add/remove/restore Monitoring tak berfungsi di browser.**

## Performance

- **Duration:** ~27 min
- **Started:** 2026-06-21T11:11:22Z
- **Completed:** 2026-06-21T11:38:34Z
- **Tasks:** 3 (Task 3 = AUTOPILOT UAT dijalankan sendiri, bukan diserahkan ke manusia)
- **Files modified:** 4 (2 created, 2 modified)

## Accomplishments
- **7 sinyal live PASS di real browser @5277 AD-off** (`--workers=1`): (a) add picker → baris live tanpa reload; (b) modal keras InProgress; (c) force-kick worker 2-context (#examRemovedModal + "Anda telah dikeluarkan dari ujian ini." + redirect /CMP/Assessment); (d) baris → #tbodyRemoved live; (e) Restore 1-klik → baris balik aktif; (f) count aktif exclude #tbodyRemoved; (g) multi-observer admin A+B.
- **Menemukan + memperbaiki bug produk nyata** (`flashRow is not defined`) — ReferenceError lintas `<script>` block yang membatalkan SELURUH handler picker/hapus/restore. 412-02 "runtime smoke" (cek markup render) tak menangkapnya; e2e real-browser (lesson Phase 354) yang menangkap.
- **Seed via SEED_WORKFLOW** (BACKUP → flip sesi 172 InProgress → RESTORE); DB lokal bersih pasca-run (sesi 172 = Open baseline, batch = 1 sesi, RemovedAt 0, 0 HcPortalDB_Test%, 0 matrix rows). SEED_JOURNAL 413 active→cleaned.

## Task Commits

1. **Task 1: Scaffold + seed lifecycle** — `57c1971d` (test)
2. **Task 2: 7 sinyal multi-context** — `a4316fe7` (test)
3. **Task 3 (UAT-driven fixes):**
   - **Product bug fix (Rule 1)** — `c13fdd22` (fix) — hoist monFlashRow ke @section Scripts
   - **Test robustness** — `71a09ac9` (test) — dropdown toggle + panel collapse + modal-wait + matrix exclude + journal cleaned

## Files Created/Modified
- `tests/e2e/flexible-participant-412.spec.ts` (338 baris) — 4 test block (cakup 7 sinyal), beforeAll BACKUP+flip InProgress / afterAll RESTORE, helper waitHubConnected + openRowHapusModal.
- `tests/sql/flexible-participant-413-seed.sql` (63 baris) — resolve+flip 1 sesi reliable (punya paket, non-Proton, non-pair, milik rino, non-matrix, batch punya eligible-to-add).
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — pindah `window.monFlashRow = flashRow;` dari blok atas (1290) ke blok @section Scripts (setelah def flashRow).
- `docs/SEED_JOURNAL.md` — entry 413 (active→cleaned).

## Decisions Made
- **Worker login = coachee (rino)**; seed SQL memfilter `UserId=rino` sehingga flip-UserId sesi TIDAK diperlukan (worker dapat Resume `/CMP/StartExam/172`).
- **Sesi target = 172** (`UAT Mobile HP`, Standard, single-participant batch, punya paket) — bukan Pre/Post pair (hindari pair-as-unit removal yang mempersulit force-kick) dan bukan matrix sentinel.
- **Sinyal digabung:** b+c+d+f dalam 1 test force-kick (alur state alami); a/e/g terpisah.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] `flashRow is not defined` ReferenceError membatalkan seluruh UI add/remove/restore live**
- **Found during:** Task 3 (UAT run — picker stuck "Memuat daftar peserta...")
- **Issue:** `window.monFlashRow = flashRow;` di blok `<script>` atas (`AssessmentMonitoringDetail.cshtml:1290`) mereferensi `flashRow` yang didefinisikan di blok `@section Scripts` TERPISAH (`:1786`). Function declaration tidak ter-hoist lintas `<script>` block → `ReferenceError` saat parse → seluruh blok script atas (termasuk handler `show.bs.modal` picker, `btnKonfirmasiTambah`, hapus-modal, restore) GAGAL ter-attach. Akibat: di browser, fitur add/remove/restore live Monitoring tak berfungsi sama sekali (picker hang, tombol mati).
- **Fix:** Hapus ekspos di blok atas; pindah `window.monFlashRow = flashRow;` ke blok `@section Scripts` tepat setelah definisi `flashRow`.
- **Files modified:** Views/Admin/AssessmentMonitoringDetail.cshtml
- **Verification:** e2e 5/5 green setelah fix (sebelumnya picker gagal load); build 0 error.
- **Committed in:** `c13fdd22`

**2. [Rule 3 - Blocking] Seed salah-pilih sesi matrix global-setup**
- **Found during:** Task 3 (run pertama — batch yang dimonitor = `[MATRIX_TEST_...] Sentinel`)
- **Issue:** `global.setup.ts` (config `dependencies: ['setup']`) seed 18 sesi matrix ber-Id tinggi (9001+), sebagian milik rino → seed `ORDER BY Id DESC` salah-pilih sesi matrix; batch matrix eligible-list picker kosong/hang.
- **Fix:** Tambah `Title NOT LIKE '[[]MATRIX[_]TEST%'` + filter "batch punya ≥1 user eligible-to-add" di seed SQL & resolve query.
- **Files modified:** tests/sql/flexible-participant-413-seed.sql, tests/e2e/flexible-participant-412.spec.ts
- **Committed in:** `71a09ac9`

**3. [Rule 1 - Test bug] Selector tak buka dropdown / panel collapse / modal-wait instan**
- **Found during:** Task 3 (run iteratif)
- **Issue:** (a) `.btn-hapus-peserta` di dalam dropdown Bootstrap (perlu klik ⋮ dulu); (b) baris `#tbodyRemoved` di panel collapse default tertutup (assert `visible` gagal → pakai `attached` + expand sebelum Restore); (c) `isVisible({timeout})` di multi-observer mengabaikan timeout (return instan) → pakai `waitFor({state:'visible'})`.
- **Fix:** helper `openRowHapusModal` (buka dropdown→klik item); assert `state:'attached'` + expand panel; `waitFor` real.
- **Files modified:** tests/e2e/flexible-participant-412.spec.ts
- **Committed in:** `71a09ac9`

---

**Total deviations:** 3 auto-fixed (1 product bug Rule 1, 1 blocking Rule 3, 1 test-bug Rule 1).
**Impact on plan:** Deviasi #1 adalah temuan paling bernilai — bug produk blocking yang HANYA terdeteksi via e2e real-browser (justifikasi penuh phase ini, lesson 354). #2/#3 = pengerasan test agar deterministik. Tak ada scope creep; kode produksi hanya 1 baris dipindah (nol perubahan logika).

## Issues Encountered
- **SqlException 596 "session is in the kill state"** saat run berulang: matrix `global.teardown` RESTORE (`SET SINGLE_USER WITH ROLLBACK IMMEDIATE`) membunuh koneksi pool app yang masih hidup; run berikutnya memungut koneksi mati. **Resolusi:** restart app fresh sebelum tiap run (bukan bug produk; artefak BACKUP/RESTORE lokal). Dicatat sebagai pola di frontmatter.

## User Setup Required
None — verifikasi lokal saja. migration=FALSE. **DO NOT push** (deploy bundle v32.5 nanti; carry IT migration=TRUE Phase 409 `01cd7dd0`).

## Next Phase Readiness
- **413-03 (full regression gate)** siap dijalankan — semua sinyal live terbukti hijau. Catatan: bug produk `monFlashRow` (fix `c13fdd22`) menyentuh view; tak ada dampak xUnit (Razor view) tetapi build 0 error sudah diverifikasi.
- Bug produk fix layak diperhatikan reviewer: 412-02 "runtime smoke" tipe (cek simbol render di DOM) TIDAK menjamin JS exec bebas error — gunakan e2e real-browser untuk Razor/JS dinamis (re-konfirmasi lesson Phase 354).
- Tak ada bug produk tersisa dari 7 sinyal (semua green pasca fix).

## Self-Check: PASSED

- Files: `tests/e2e/flexible-participant-412.spec.ts`, `tests/sql/flexible-participant-413-seed.sql`, `413-02-SUMMARY.md` — all FOUND.
- Commits: `57c1971d`, `a4316fe7`, `c13fdd22`, `71a09ac9` — all FOUND.
- Product fix present: `window.monFlashRow = flashRow;` in @section Scripts (2 refs: export+consumer).
- e2e result: **5/5 green** (7 sinyal). DB restored to baseline (S172=Open, 0 leftover). SEED_JOURNAL 413 = cleaned.

---
*Phase: 413-test-uat*
*Completed: 2026-06-21*
