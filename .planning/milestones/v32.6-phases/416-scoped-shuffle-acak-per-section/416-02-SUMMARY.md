---
phase: 416-scoped-shuffle-acak-per-section
plan: 02
subsystem: assessment
tags: [shuffle, section, scoped-shuffle, wiring, call-site, et-coverage, viewbag, backward-compat]

# Dependency graph
requires:
  - phase: 416 Plan 01
    provides: "ShuffleEngine section-aware (BuildQuestionAssignment partisi per-Section) + BuildSectionAwareOptionShuffle (gate opsi per-Section, D-416-01)"
  - phase: 415-section-foundation-import-excel-diperluas
    provides: "AssessmentPackageSection (SectionNumber/Name/ShuffleEnabled) + PackageQuestion.SectionId + .Section navigation"
provides:
  - "3+ call-site assignment uniform memuat q.Section (.ThenInclude) + memanggil BuildSectionAwareOptionShuffle: StartExam, CreateEagerAssignmentsAsync (AddParticipantsLive), ReshufflePackage, ReshuffleAll — scoped-shuffle aktif runtime, drift-free (Pitfall 3 + Pitfall 5 ditutup)"
  - "Peringatan cakupan ET per-Section (D-416-03) non-blocking di ManagePackageQuestions: ViewBag.SectionEtWarnings (record SectionEtWarning) saat distinct-ET > K, dirender di view"
affects: [416-03 (Playwright UAT runtime), 417-section-pagination, 419-export-test-uat-milestone]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Wiring uniform 3 call-site: load+engine SAMA di setiap titik assignment (StartExam/Reshuffle*/EagerAssign) → SHF-04 + AddParticipantsLive drift-free otomatis"
    - "ET-coverage warning = compute di GET controller (strongly-typed record) → ViewBag → render view; NON-BLOCKING (sinyal, bukan error), tak menyentuh jalur mulai ujian"

key-files:
  modified:
    - "Controllers/CMPController.cs (StartExam + CreateEagerAssignmentsAsync: .ThenInclude(q => q.Section) + BuildSectionAwareOptionShuffle)"
    - "Controllers/AssessmentAdminController.cs (ReshufflePackage + ReshuffleAll: .ThenInclude(q => q.Section) + BuildSectionAwareOptionShuffle; ManagePackageQuestions GET: ViewBag.SectionEtWarnings + record SectionEtWarning)"
    - "Views/Admin/ManagePackageQuestions.cshtml (render peringatan cakupan ET per-Section)"

key-decisions:
  - "Wiring uniform: setiap call-site assignment load q.Section + panggil BuildSectionAwareOptionShuffle → tak ada drift antar StartExam/Reshuffle*/EagerAssign (SHF-04)"
  - "ET-warning strongly-typed record SectionEtWarning(SectionNumber, Name, K, DistinctEt) — filter DistinctEt > K, non-blocking, hanya sinyal di UI kelola Section"
  - "Tidak ubah grading/skema; reuse kolom 415; migration=FALSE"

requirements-completed: [SHF-01, SHF-02, SHF-03, SHF-04]

# Metrics
duration: ~35min (executor died pre-metadata; SUMMARY + tracking diselesaikan orchestrator pasca verify build+test)
completed: 2026-06-23
---

# Phase 416 Plan 02: Wire Section-Aware Engine + ET-Coverage Warning Summary

**Engine section-aware (Plan 01) kini AKTIF runtime: 3+ call-site assignment (StartExam, AddParticipantsLive eager, ReshufflePackage, ReshuffleAll) seragam memuat `q.Section` dan memanggil `BuildSectionAwareOptionShuffle` — plus peringatan cakupan ET per-Section non-blocking di ManagePackageQuestions (D-416-03).**

## Accomplishments
- **Wire 3 call-site uniform (`9493cf9d`):** `CMPController.StartExam` + `CreateEagerAssignmentsAsync` (AddParticipantsLive, v32.5 Phase 410) dan `AssessmentAdminController.ReshufflePackage` + `ReshuffleAll` semua di-`.ThenInclude(q => q.Section)` pada jalur load assignment, lalu `BuildOptionShuffle` → `BuildSectionAwareOptionShuffle(...)`. Tanpa Include, partisi senyap jatuh ke "Lainnya" (Pitfall 3); seragam-nya load+engine = SHF-04 + drift-free AddParticipantsLive (Pitfall 5).
- **Peringatan cakupan ET (`72569ed3`, D-416-03):** `ManagePackageQuestions` GET menghitung per-Section `K = jumlah soal` vs `distinct ET`; bila `distinctEt > K`, masukkan `SectionEtWarning` ke `ViewBag.SectionEtWarnings`. Dirender di `ManagePackageQuestions.cshtml` sebagai sinyal ke HC. **NON-BLOCKING** — tidak memblokir kelola/mulai ujian (Section kecil = konfigurasi sah).

## Task Commits
1. **Task 1: Wire 3 call-site uniform (load Section + BuildSectionAwareOptionShuffle)** - `9493cf9d` (feat)
2. **Task 2: Peringatan cakupan ET per-Section (non-blocking)** - `72569ed3` (feat)

## Files Modified
- `Controllers/CMPController.cs` — StartExam + CreateEagerAssignmentsAsync: `.ThenInclude(q => q.Section)` (2 site) + `BuildSectionAwareOptionShuffle` (2 call).
- `Controllers/AssessmentAdminController.cs` — ReshufflePackage + ReshuffleAll: `.ThenInclude(q => q.Section)` + `BuildSectionAwareOptionShuffle` (total 3 call AAC incl eager-path partner); ManagePackageQuestions GET: `ViewBag.SectionEtWarnings` + `record SectionEtWarning`.
- `Views/Admin/ManagePackageQuestions.cshtml` — render peringatan cakupan ET per-Section.

## Verification
- `dotnet build HcPortal.csproj` → **0 error, 0 warning**.
- Full suite `dotnet test HcPortal.Tests` → **665/665 PASS, 0 fail** (no regresi).
- grep gate: `ThenInclude(q => q.Section)` di CMPController (2) + AssessmentAdminController (5, incl reshuffle 6029/6111 + manage 7047/7275 + eager 2550); `BuildSectionAwareOptionShuffle` di CMPController (2) + AAC (3) — call-site assignment seragam.
- `ViewBag.SectionEtWarnings` di-set (AAC:7673) + dirender (ManagePackageQuestions.cshtml:184).
- migration=FALSE dipertahankan (0 perubahan Migrations/Data/Models).

## Deviations from Plan
None — code dieksekusi sesuai plan. **Catatan proses (bukan deviasi):** executor agent terputus (API "Connection closed") SETELAH commit kedua task kode tapi SEBELUM menulis SUMMARY + update STATE/ROADMAP. Orchestrator memverifikasi ulang (build 0-err + suite 665/665 + grep gerbang wiring/Include/ViewBag semua hadir) lalu menyelesaikan metadata. Tidak ada kode yang hilang/parsial.

## Issues Encountered
- Executor connection drop pasca-commit kode (×2 fenomena di fase ini: Plan 01 attempt-1 mati pra-commit → clean re-spawn; Plan 02 mati pasca-commit-kode pra-metadata → orchestrator finalize). Tidak berdampak pada korektezza kode (terverifikasi build+test).

## Next Phase Readiness
- **Runtime scoped-shuffle siap untuk Plan 03 (Playwright UAT).** Plan 03: e2e real-browser buktikan acak DALAM Section + backward-compat all-null + peringatan ET — lifecycle DB snapshot/restore (SEED_WORKFLOW), `--workers=1`.
- migration=FALSE → notify IT saat handoff (commit hash + flag).

## Self-Check: PASSED
- Files: CMPController.cs ✓, AssessmentAdminController.cs ✓, ManagePackageQuestions.cshtml ✓, 416-02-SUMMARY.md ✓
- Commits: 9493cf9d (wire) ✓, 72569ed3 (ET-warning) ✓
- Build 0-err ✓, suite 665/665 ✓

---
*Phase: 416-scoped-shuffle-acak-per-section*
*Completed: 2026-06-23*
