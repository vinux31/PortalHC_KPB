---
phase: 389-coachcoacheemapping-redesign-accordion-card-per-coach-dsn-01-dsn-02-dsn-03
plan: 01
subsystem: testing
tags: [playwright, e2e, accordion, parity, razor, ui-spec]

# Dependency graph
requires:
  - phase: 388-label-hasil-coachworkload-polish-lbl-03-dsn-04-dsn-05
    provides: "Spec template sibling coachworkload-388.spec.ts (loginAny scaffold + data-skip + filter idiom)"
provides:
  - "tests/e2e/coachcoacheemapping-389.spec.ts — Playwright parity spec V-01..V-14 (struktural + smoke parity, test-first)"
  - "Kontrak verifikasi runtime accordion card (DSN-01/02/03) tersedia SEBELUM markup rewrite (Nyquist safeguard)"
  - "Grep-label V-01..V-14 byte-cocok dgn 389-VALIDATION.md untuk feedback sampling Plan 02"
affects: [389-02 markup rewrite CoachCoacheeMapping.cshtml, 390 Test & UAT parity DSN-06]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Test-first Wave 1: spec ditulis terhadap markup TARGET (accordion), RED sampai Plan 02 (Phase 354 lesson — a11y/Razor dinamis WAJIB Playwright runtime assert)"
    - "test.skip data-guard per test (card count 0 / coachee row 0 → skip, jangan fail) — smoke parity di 389, full mutation = Phase 390"
    - "route intercept appUrl sub-path (**/Admin/CoachCoacheeMappingDeletePreview*) bukti PathBase-aware AJAX tak 404"

key-files:
  created:
    - tests/e2e/coachcoacheemapping-389.spec.ts
  modified: []

key-decisions:
  - "Spec ditulis penuh terhadap markup accordion TARGET (Plan 02), bukan tabel lama — test-first; --list parse OK = deliverable, full-green ditunda ke Plan 02"
  - "V-14 submit button = 'Cari' (verified view L197), bukan 'Filter' (beda dari sibling 388 yg pakai 'Filter')"
  - "V-13 route intercept pakai expect.poll(() => hit) krn confirmDelete fetch async (modal show dulu, fetch belakangan)"

patterns-established:
  - "Pattern: grep-label test name = substring persis VALIDATION.md (V-NN <label>) → enable -g feedback sampling per task di Plan 02"
  - "Pattern: a11y header toggle (V-07) — assert role=button-or-BUTTON + aria-controls==id body + keyboard Enter/Space toggle (Phase 354 anti-grep)"

requirements-completed: [DSN-01, DSN-02, DSN-03]

# Metrics
duration: ~3min
completed: 2026-06-17
---

# Phase 389 Plan 01: CoachCoacheeMapping Accordion Parity Spec Summary

**Playwright parity spec 14-test (V-01..V-14) yang mengunci kontrak runtime redesign accordion card per coach (DSN-01/02/03) + smoke parity aksi existing (DSN-06*) SEBELUM markup di-rewrite — Nyquist safeguard test-first.**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-06-17T04:43:59Z
- **Completed:** 2026-06-17T04:46:22Z
- **Tasks:** 2
- **Files modified:** 1 (created)

## Accomplishments
- `tests/e2e/coachcoacheemapping-389.spec.ts` baru (289 baris) dgn 14 test V-01..V-14, parse OK via `--list` (15 entri termasuk setup).
- Scaffold login admin via `loginAny` + nav `/Admin/CoachCoacheeMapping` + H2 guard "Coach-Coachee Mapping" (template sibling 388).
- V-01..V-09 struktural (DSN-01/02/03): card per coach + avatar, badge threshold via evaluate, default-closed, collapse buka-tutup, independent multi-open, mini-tabel 9 kolom (no "Coachee Aktif"), a11y header toggle (Enter/Space + aria-controls), toolbar seragam (btn-primary solo + btn-group Excel btn-sm), Tambah Mapping onclick null → #assignModal.
- V-10..V-14 smoke parity (DSN-06*): edit modal (#editCoacheeName set), delete modal, aksi branch (IsCompleted Graduated cek dulu, no Aktifkan), AJAX appUrl route intercept (CoachCoacheeMappingDeletePreview), filter Seksi → URL `section=`.
- Tiap test ber-`test.skip` data-guard (18 skip-guard) — RED-aman pra-Plan 02; full mutation parity dikunci Phase 390.

## Task Commits

Each task was committed atomically:

1. **Task 1: Scaffold spec + V-01..V-09 (struktur card/collapse/toolbar — DSN-01/02/03)** - `190b2b19` (test)
2. **Task 2: V-10..V-14 (smoke parity DSN-06 — edit/delete/aksi/ajax/filter)** - `275b3a42` (test)

_Note: spec test-first; full-green ditunda ke Plan 02 (markup target belum ada — itu BENAR)._

## Files Created/Modified
- `tests/e2e/coachcoacheemapping-389.spec.ts` - Playwright parity spec 14-test (V-01..V-14): scaffold loginAny admin + 9 test struktural accordion (DSN-01/02/03) + 5 test smoke parity aksi existing (DSN-06*), semua ber-test.skip data-guard.

## Decisions Made
- **V-14 submit button "Cari" bukan "Filter":** view aktual (`CoachCoacheeMapping.cshtml:197`) pakai tombol submit ber-teks "Cari" (beda dari sibling CoachWorkload 388 yg "Filter"). Disesuaikan ke kontrak halaman ini.
- **V-13 expect.poll untuk route-hit:** `confirmDelete` (view L944) menampilkan `#deleteModal` LALU `fetch(appUrl(...DeletePreview))` async; jadi assert pakai `expect.poll(() => hit)` setelah modal visible, bukan asumsi sinkron.
- **Spec ditulis penuh ke markup accordion TARGET (test-first):** sebagian besar RED bila dijalankan sekarang (markup masih tabel) — sesuai objective plan; deliverable Wave 1 = parse-able + 14 test terdaftar, bukan green.

## Deviations from Plan

None - plan executed exactly as written. Dua penyesuaian kecil (nama tombol submit "Cari", `expect.poll` untuk async route) adalah pengisian detail kontrak halaman yang memang diminta plan ("Cek nama tombol submit aktual di halaman ... sesuaikan"), bukan deviasi terhadap spesifikasi.

## Issues Encountered
None. Verifikasi `npx playwright test coachcoacheemapping-389 --list` exit 0 di kedua task (9 test setelah Task 1, 14 test setelah Task 2). App tidak perlu jalan untuk `--list` (parse-only) sesuai catatan plan.

## User Setup Required
None - no external service configuration required. Spec hanya import kredensial test lokal existing (`tests/helpers/accounts.ts` admin@pertamina.com), tak ada secret baru.

## Next Phase Readiness
- **Plan 02 siap:** kontrak runtime accordion (V-01..V-14) tersedia sebagai `<automated>` nyata. Plan 02 me-rewrite `Views/Admin/CoachCoacheeMapping.cshtml` (toolbar L48-61 + else-block L228-347) ke accordion card → spec ini jadi hijau (sampling per task via `-g "<label>"`).
- **Catatan untuk Plan 02:** markup TARGET yang di-assert spec ini = card `.card.shadow-sm` per coach, header `.card-header` (role=button/BUTTON) dgn `.avatar-initial` + `.badge` threshold (verbatim ternary `>=8 bg-danger / >=5 bg-warning / else bg-info`), body `.collapse` id `#collapse-N` TANPA `show`/`data-bs-parent`, mini-tabel 9 `thead th` (drop "Coachee Aktif"), toolbar `btn-primary` solo + `.btn-group` 3 Excel `btn-sm`, Tambah Mapping TANPA dead-onclick, chevron `@section Styles`. Modal IDs + JS hooks (openEditModal/confirmDelete/appUrl) FROZEN.
- **Phase 390:** full data-mutation parity (assign/import/export end-to-end + row-removal) — spec ini sengaja skip via data-guard.
- **Blocker:** none.

## Self-Check: PASSED

- FOUND: `tests/e2e/coachcoacheemapping-389.spec.ts`
- FOUND: `389-01-SUMMARY.md`
- FOUND commit: `190b2b19` (Task 1)
- FOUND commit: `275b3a42` (Task 2)

---
*Phase: 389-coachcoacheemapping-redesign-accordion-card-per-coach-dsn-01-dsn-02-dsn-03*
*Completed: 2026-06-17*
