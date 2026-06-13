---
phase: 374-ui-managepackages-lock-pre-post
plan: 02
subsystem: shuffle-toggle-endpoint
tags: [controller, endpoint, prg, viewbag, security]
requires:
  - "HcPortal.Helpers.ShuffleToggleRules (Plan 01)"
provides:
  - "POST UpdateShuffleSettings (guard+propagate+audit+PRG)"
  - "ManagePackages GET ViewBag contract for Plan 03 view"
affects:
  - "Plan 03 view ManagePackages.cshtml (consumes 7 ViewBag + form posts UpdateShuffleSettings)"
tech-stack:
  added: []
  patterns:
    - "PRG (form POST + RedirectToAction + TempData), no AJAX/Json (D-01a)"
    - "Server lock guard re-check (defense-in-depth D-04a) via same helper as GET (Pitfall 2 killed)"
    - "Audit try/catch warn-only (mirror ReshufflePackage/ReshuffleAll)"
key-files:
  created: []
  modified:
    - "Controllers/AssessmentAdminController.cs"
key-decisions:
  - "Endpoint disisip SETELAH ReshuffleAll (semantik berkerumun reshuffle block); using HcPortal.Helpers sudah ada (L13)"
  - "GET ViewBag pakai prefix var shuf* (shufSiblingIds/shufAnyStarted/shufAnyAssignment) cegah collision dgn local GET existing"
  - "UpdatedAt di-set (field ada di model, konsistensi audit-trail EditAssessment) — Q3 RESOLVED: ada"
  - "Field injeksi confirmed: _context, _userManager, _auditLog, _logger (sama dgn ReshufflePackage)"
requirements-completed: [SHUF-10, SHUF-11, SHUF-13, SHUF-14]
duration: ~10 min
completed: 2026-06-13
---

# Phase 374 Plan 02: Shuffle Toggle Endpoint + GET Enrich Summary

Endpoint `UpdateShuffleSettings` (PRG + server lock guard + propagate sibling + audit) + `ManagePackages` GET enrich 7 ViewBag. Satu file `AssessmentAdminController.cs`, TANPA migration.

**Durasi:** ~10 min | **Task:** 3 | **File:** 1 modified (+105 baris).

## Yang Dibangun

**Task 1 — POST `UpdateShuffleSettings(int assessmentId, bool shuffleQuestions, bool shuffleOptions)`** (disisip setelah `ReshuffleAll`):
- `[HttpPost]` + `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` (T-374-priv/csrf).
- Sibling key `(Title, Category, Schedule.Date)` — spec §5.
- Server lock guard (T-374-lock/D-04a): `anyStarted` + `anyAssignment` → `ShuffleToggleRules.IsShuffleLocked` → locked → `TempData["Error"]` + redirect TANPA write.
- Propagate foreach ke SEMUA sibling + `UpdatedAt = now` → `SaveChangesAsync`.
- Audit warn-only `_auditLog.LogAsync(actorId, "NIP - FullName", "UpdateShuffleSettings", desc, assessmentId, "AssessmentSession")` (T-374-audit).
- PRG: `TempData["Success"]` + `RedirectToAction("ManagePackages")`. No Json (D-01a).

**Task 2 — GET `ManagePackages` enrich** (sisip setelah `ViewBag.IsSamePackageLocked`, sebelum `return View()`):
- `ViewBag.ShuffleQuestions` / `ShuffleOptions` (saved state → render checked).
- `ViewBag.IsShuffleLocked` (helper SAMA dgn POST — 2 call-site, no divergensi).
- `ViewBag.HideShuffleToggle` (Proton Th3 / Manual).
- `ViewBag.PackagesWithQuestions` + `HasSizeMismatch` + `ShowSizeMismatchWarning` (mirror view L70-81; emit raw juga utk JS recompute Plan 03, Pitfall 4 opsi b).
- `ViewBag.PreShuffleQuestions` (Post page only, via `LinkedSessionId`, nullable; SHUF-13).

## ViewBag Contract untuk Plan 03

| ViewBag | Tipe | Pakai |
|---------|------|-------|
| ShuffleQuestions / ShuffleOptions | bool | `checked` switch |
| IsShuffleLocked | bool | `disabled` switch+tombol + lock alert |
| HideShuffleToggle | bool | `@if (ViewBag.HideShuffleToggle != true)` wrap seluruh card |
| PackagesWithQuestions | int | JS recompute warning |
| HasSizeMismatch | bool | JS recompute warning |
| ShowSizeMismatchWarning | bool | initial server-render warning |
| PreShuffleQuestions | bool? | reminder `== false && sqChecked` (null Pre → tak muncul) |

## Verifikasi

- `dotnet build HcPortal.csproj` → Build succeeded.
- `dotnet test --filter "FullyQualifiedName~Shuffle"` → **41/41 pass** (Wave 0 Plan 01 + 372/373, no regresi).
- 2 call-site `ShuffleToggleRules.IsShuffleLocked` (GET+POST) — single-source.
- No `return Json` di body endpoint (PRG).
- Attribute trio proximity terverifikasi grep.

## Deviations from Plan

**[Rule 1 - cosmetic] Alignment double-space** — Found during: Task 2 verify | `ViewBag.IsShuffleLocked  =` (2 spasi alignment) gagal grep literal acceptance | Normalize 1 spasi | Commit: d76f9b13. Nol dampak.

**Total deviations:** 1 auto-fixed (kosmetik). **Impact:** nol.

## Self-Check: PASSED

- File modified ada (+105 baris), build hijau.
- Commit `d76f9b13` di git log.
- Semua acceptance_criteria Task 1+2+3 re-run PASS.

Ready for **374-03** (view card + live JS + UAT checkpoint).
