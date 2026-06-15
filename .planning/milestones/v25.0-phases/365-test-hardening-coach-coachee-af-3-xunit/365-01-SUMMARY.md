---
phase: 365-test-hardening-coach-coachee-af-3-xunit
plan: 01
subsystem: coaching-graduate
tags: [coaching, refactor, core-extraction, parity-lock, proton]
requires: []
provides: [MarkMappingCompletedCore-static, IsYearCompletedAsync-static]
affects:
  - Controllers/CoachMappingController.cs
  - .planning/ROADMAP.md
tech-stack:
  added: []
  patterns: [static-core-extraction-363, thin-wrapper]
key-files:
  created: []
  modified:
    - Controllers/CoachMappingController.cs
    - .planning/ROADMAP.md
key-decisions:
  - "Core murni SaveChanges (OQ-1); transaksi tetap di wrapper. Guard read-only di-pindah ke dalam transaksi wrapper = no-op rollback (zero behavior change)."
  - "Cascade reuse list 'assignments' (Include'd) ganti query kedua produksi lama — predikat identik, cascadeCount identik."
  - "ROADMAP SC#1 (InMemory→real-SQL fixture) + SC#2 (zero-change→behavior-preserving parity-locked) di-amend; tanpa ini verifier kontradiksi."
requirements-completed: []
duration: ~5 min
completed: 2026-06-12
---

# Phase 365 Plan 01: Extract MarkMappingCompletedCore + Parity

Refactor behavior-preserving: endpoint graduate `MarkMappingCompleted` dipecah jadi `public static MarkMappingCompletedCore(ApplicationDbContext, int)` (pola 363) + helper `IsYearCompletedAsync` jadi static + wrapper tipis. Plus amendemen ROADMAP SC#1/SC#2.

## Task 1 — Ekstrak core static + static helper + wrapper tipis
`Controllers/CoachMappingController.cs`:
- **(A)** `IsYearCompletedAsync(int)` instance → `static (ApplicationDbContext ctx, int)`. Hanya `_context`→`ctx`. 1 caller (grep-verified) ganti langsung, no delegasi (OQ-3).
- **(B)** Core baru `public static async Task<(bool ok, string? error, int cascadeCount)> MarkMappingCompletedCore(ApplicationDbContext, int)`: not-found + 2 guard → `(false, error, 0)` (OQ-2 not-found di core); mutasi mapping (IsCompleted/CompletedAt/IsActive/EndDate) + cascade deactivate assignment (IsActive=false + DeactivatedAt) + `SaveChangesAsync` (OQ-1). NO transaksi di core (D-02).
- **(C)** Wrapper tipis: attrs `[HttpPost]`/`[ValidateAntiForgeryToken]`/`[Authorize(Roles="Admin, HC")]` VERBATIM + resolve-user/`Challenge()` + `BeginTransactionAsync` → core → `Commit`/`Rollback` + audit post-commit + TempData + redirect.

Build: **Build succeeded, 0 Error**.

Grep acceptance (semua PASS): core signature ×1, static helper signature ×1, wrapper seam `MarkMappingCompletedCore(_context, mappingId)` ×1, core→`IsYearCompletedAsync(context, ...)` ×1, 3 attrs preserved, `git diff Services/` kosong.

## Task 2 — Amend ROADMAP + bukti parity
`.planning/ROADMAP.md` Phase 365:
- **SC#1 sebelum:** "...InMemory DbContext pola `OrganizationControllerTests`..." → **sesudah:** "...real-SQL `ProtonCompletionFixture` (D-04, enforce filtered unique index IX_CoachCoacheeMappings_CoacheeId_ActiveUnique)..."
- **SC#2 sebelum:** "Suite penuh `dotnet test` hijau; zero file produksi berubah (git diff Controllers/ Services/ kosong)." → **sesudah:** "Refactor behavior-preserving + parity-locked; `Controllers/CoachMappingController.cs` disentuh (extract static core...), zero behavior change dibuktikan via core test hijau + `dotnet test` full suite hijau + `dotnet build` 0 error; `git diff Services/` tetap kosong; migration=false."

Parity: **baseline pra-edit = 229/229 passed (0 failed, 26s)**; **pasca-refactor = 229/229 passed (0 failed, 26s)** → ZERO regresi. `git diff Services/` kosong.

## Zero-behavior-change reasoning
- Urutan eksekusi identik: resolve-user → guard (read-only) → mutasi → SaveChanges → Commit → audit → TempData/redirect.
- Guard yang dulu di LUAR transaksi kini di DALAM transaksi wrapper (core dipanggil dalam scope tx). Guard hanya READ (FindAsync/query/predikat) — tak ada write sebelum guard lolos → transaksi yang langsung di-rollback saat guard-fail = no-op efektif. TempData/redirect/NotFound identik per cabang.
- `_auditLog.LogAsync` tetap POST-commit; pesan & format string identik. CoacheeId via re-read entity tracked (nilai sama, mapping CoacheeId tak berubah).
- `cascadeCount` identik: produksi lama query `activeAssignments` KEDUA dengan predikat `CoacheeId==X && IsActive` identik dengan `assignments` pertama; core reuse list pertama → set sama, count sama.

## Files changed
- `Controllers/CoachMappingController.cs` — core+helper+wrapper.
- `.planning/ROADMAP.md` — SC#1/SC#2 amended.

## Next
Wave 2: Plan 02 — `MarkMappingCompletedTests.cs` (6 [Fact] real-SQL memanggil core ini).
