---
phase: quick-260611-m9r
plan: 01
subsystem: api
tags: [assessment, search, ef-core, linq, admin, monitoring]

requires:
  - phase: 338
    provides: "Preseden CIL-02 — pencarian eksplisit user mengalahkan penyempitan default"
provides:
  - "Helper static ApplySevenDayWindow (single source of truth window 7-hari + override search)"
  - "Search non-empty di Tab Assessment & AssessmentMonitoring menembus window 7-hari (assessment lama >7 hari ditemukan)"
affects: [assessment-monitoring, manage-assessment, 363-audit-fix-alur-proton]

tech-stack:
  added: []
  patterns: ["Window guard sebagai helper static pure (testable LINQ-to-Objects, dipakai 2 call site)"]

key-files:
  created:
    - HcPortal.Tests/AssessmentSearchWindowTests.cs
  modified:
    - Controllers/AssessmentAdminController.cs

key-decisions:
  - "Window 7-hari di-skip HANYA saat search non-empty; search kosong tetap pakai window (default view cepat, perilaku lama tak berubah)"
  - "Satu helper bersama ApplySevenDayWindow dipakai 2 method supaya logika window+search identik (no drift)"

patterns-established:
  - "Window guard sebagai helper static pure: testable tanpa DbContext, single source of truth lintas call site"

requirements-completed: [QUICK-260611-m9r]

duration: 12min
completed: 2026-06-11
---

# Quick Task 260611-m9r: Fix Search Blind-Spot Window 7-Hari Summary

**Search non-empty di Tab Assessment & AssessmentMonitoring sekarang menembus window 7-hari lewat helper bersama `ApplySevenDayWindow`, sehingga assessment lama (>7 hari, mis. Post Test OJT) bisa ditemukan via search; search kosong tetap mempertahankan window 7-hari (default view cepat tak berubah).**

## Performance

- **Duration:** ~12 min
- **Completed:** 2026-06-11
- **Tasks:** 2/2
- **Files modified:** 2 (1 controller modified, 1 test file created)

## Accomplishments

- Helper static `ApplySevenDayWindow(IQueryable<AssessmentSession>, string? search, DateTime cutoff)` — window 7-hari hanya saat `search` kosong; search non-empty → window di-SKIP (preseden CIL-02 Phase 338).
- Wire 2 call site ke helper bersama: `ManageAssessmentTab_Assessment` (L123) + `AssessmentMonitoring` (L2870) — satu sumber kebenaran, logika identik.
- Komentar 90-review di `AssessmentMonitoring` diperbarui mencerminkan perilaku baru (window hanya saat search kosong).
- 3 [Fact] unit test (TDD RED→GREEN) membuktikan: search kosong→sesi lama tersaring, search non-empty→sesi lama muncul, search kosong→sesi baru lolos. Null-coalesce (ExamWindowCloseDate=null → fallback Schedule) terverifikasi.

## Task Commits

1. **Task 1 (RED): test gagal ApplySevenDayWindow** - `c8ba81ad` (test)
2. **Task 1 (GREEN): helper + wire 2 call site + komentar** - `f25dff99` (fix)
3. **Task 2: gate full-suite + grep sanity + UAT handoff** - tanpa commit (verifikasi gate, tak ada perubahan kode baru di luar Task 1)

## Files Created/Modified

- `Controllers/AssessmentAdminController.cs` - Tambah helper static `ApplySevenDayWindow` (dekat `DeriveUserStatus`/`IsTrainingInitialState`); ganti 2 inline `.Where(... >= sevenDaysAgo)` dengan panggilan helper; perbarui komentar 90-review `AssessmentMonitoring`.
- `HcPortal.Tests/AssessmentSearchWindowTests.cs` - 3 [Fact] menguji override-window (static helper, LINQ-to-Objects, tanpa DbContext/controller).

## Verification

- `dotnet build`: **Build succeeded, 0 Error(s)** (warning yang ada semua pre-existing di file tak terkait — nullability/async-tanpa-await; tak ada warning baru dari perubahan ini).
- `dotnet test` (full suite): **Passed! Failed: 0, Passed: 214, Total: 214** — 3 [Fact] baru + suite eksisting tanpa regresi.
- Grep sanity:
  - `(a.ExamWindowCloseDate ?? a.Schedule) >=` HANYA muncul di body helper (L2821, pakai `cutoff`) — tak ada lagi inline di 2 method.
  - `ApplySevenDayWindow(` muncul di L2817 (definisi), L123 (`ManageAssessmentTab_Assessment`), L2870 (`AssessmentMonitoring`).
- Komentar 90-review `AssessmentMonitoring` sudah mencerminkan perilaku baru.

## Migration

**Migration = FALSE.** Zero perubahan schema (murni logika query + helper + test). Catat untuk notifikasi IT: hanya 2 commit kode (`c8ba81ad`, `f25dff99`), tanpa migration.

## UAT Handoff Note (untuk Orchestrator — live-browser @ localhost:5277)

Executor tak punya browser MCP. Orchestrator menjalankan UAT live via Playwright.
AD lokal: `Authentication__UseActiveDirectory=false dotnet run` lalu buka `http://localhost:5277`.

- **Skenario A — Tab Assessment:** buka `/Admin/ManageAssessment` → tab "Assessment". Pastikan ada assessment lama (>7 hari, mis. judul "Post Test OJT ..."). Tanpa search, sesi lama TIDAK tampil (window jalan). Ketik judul sesi lama di kotak search → sesi lama MUNCUL.
- **Skenario B — Monitoring:** buka `/Admin/AssessmentMonitoring?search=<judul-lama>` → grup assessment lama (>7 hari) MUNCUL. Tanpa `search`, grup lama tidak tampil.
- **Catatan seed:** kalau DB lokal tak punya sesi >7 hari, orchestrator/HUMAN seed sesuai SEED_WORKFLOW (temporary + local-only, snapshot DB dulu, restore + tandai journal `cleaned` setelah UAT).

## Decisions Made

- Window 7-hari di-skip HANYA saat `search` non-empty; search kosong tetap menerapkan window (default view cepat, perilaku lama dipertahankan).
- Satu helper bersama dipakai 2 method supaya logika window+search identik (hindari drift antar-endpoint).
- CIL-01 counter (L204-207) & CIL-02 hide-Closed (L209-214) di `ManageAssessmentTab_Assessment` TIDAK diubah — perilaku counter dengan search non-empty mengikuti hasil search (perilaku lama, tetap).

## Deviations from Plan

None - plan executed exactly as written (RED→GREEN TDD, scope ketat: helper baru + 2 call site + 1 komentar + 1 file test).

## Issues Encountered

None. Build 0 error, full suite 214/214 hijau di percobaan pertama setelah GREEN.

## Threat Model Compliance

- T-m9r-01 (Information Disclosure) — **accept**: skip window saat search tidak memperluas otorisasi (kedua endpoint `[Authorize(Roles = "Admin, HC")]`; Admin/HC sudah berhak lihat SEMUA sesi). Window murni perf/UX default, bukan kontrol akses.
- T-m9r-02 (DoS full-table scan) — **accept**: `Contains` + paging (PaginationHelper) tetap menyempitkan; endpoint admin-only low-volume; `AsNoTracking`.
- T-m9r-03 (Injection) — **mitigate (sudah terpenuhi)**: EF Core parameterize otomatis (LINQ→SQL parameter), helper tak menyentuh jalur search; pola eksisting tak diubah.

Tak ada surface keamanan baru di luar threat register (no threat flags).

## Self-Check: PASSED

- `Controllers/AssessmentAdminController.cs` — helper + 2 wire + komentar (verified via grep L123/L2817/L2870/L2821).
- `HcPortal.Tests/AssessmentSearchWindowTests.cs` — FOUND.
- Commit `c8ba81ad` (test RED) — FOUND.
- Commit `f25dff99` (fix GREEN) — FOUND.

## TDD Gate Compliance

- RED gate: `c8ba81ad` `test(...)` — build fail by design (helper belum ada).
- GREEN gate: `f25dff99` `fix(...)` — helper + wire, 3 [Fact] hijau + full suite 214/214.
- REFACTOR gate: tidak diperlukan (implementasi minimal sudah bersih).

## Next Readiness

- Kode siap untuk UAT live-browser orchestrator (Skenario A & B di atas).
- Branch tetap ITHandoff (NOT PUSHED — bundle v25.0 menunggu IT availability). Migration=FALSE → tak nambah batch migration.
- Catatan koordinasi: Phase 363 (Audit Fix Alur PROTON) menyentuh `CDPController`, BUKAN method ini — tak ada konflik file.

---
*Quick task: 260611-m9r-fix-search-blind-spot-7-day-window-searc*
*Completed: 2026-06-11*
