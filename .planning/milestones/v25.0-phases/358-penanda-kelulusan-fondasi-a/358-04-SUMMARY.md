---
phase: 358-penanda-kelulusan-fondasi-a
plan: 04
status: complete
requirements: [PCOMP-03, PCOMP-05]
commits: [016df998]
migration: false
---

# Plan 358-04 SUMMARY — Interview refactor + essay defensive + backfill endpoint

## Apa yang dibangun (`Controllers/AssessmentAdminController.cs`)
1. **Inject** `ProtonCompletionService _protonCompletionService` (ctor + field).
2. **Refactor `SubmitInterviewResults`** — blok inline-create `ProtonFinalAssessments.Add(...)` → `EnsureAsync(Origin="Interview")`. `await _context.SaveChangesAsync()` (session) tetap (Pitfall 2 urutan terjaga). `actorForFix` dipertahankan (dipakai audit). PCOMP-03 single-source.
3. **Defensive hook D-05a** di `FinalizeEssayGrading` (setelah audit, sebelum NotifyIfGroupCompleted): `Category=="Assessment Proton" && isPassed && ProtonTrackId.HasValue` → `EnsureAsync(Origin="Exam")`. Nutup celah `hasEssay` early-return di GradeAndCompleteAsync (RESEARCH Pitfall 1). Idempotent → aman walau Hook A juga terbit.
4. **`POST /Admin/BackfillProtonPenanda`** `[Authorize(Roles="Admin")]` + `[ValidateAntiForgeryToken]`, idempotent:
   - Query exam Proton `IsPassed && TahunKe in (Tahun 1, Tahun 2) && CompletedAt != null`.
   - Resolve assignment A-M10 (BUKAN EnsureAsync — Pitfall 3): tanpa filter IsActive, `AssignedAt <= exam.CompletedAt`, `OrderByDescending(AssignedAt).First()`. Pakai `exam.CompletedAt` (Pitfall 4 — assignment tak punya CompletedAt).
   - Idempotent: `AnyAsync(fa.ProtonTrackAssignmentId == assignment.Id)` → skip.
   - Enforce 100% (D-08): `statuses.Count>0 && statuses.All(s=="Approved")`.
   - Create penanda Origin="Exam", `CompletedAt=exam.CompletedAt`. Audit warn-only. No info-leak (Phase 334 D6 — `ex.Message` hanya ke `_logger`).

## Verifikasi
- `dotnet build` 0 error.
- `dotnet test` full suite → **148/148 pass**.
- **UAT live @5277 (Claude via Playwright) — PASS** (snapshot→restore, antiforgery POST via fetch):
  - **Phase A (enforce 100%):** assignment Id9 + deliverable 1 Approved + 1 Pending → `0 dibuat, 1 belum 100%`; DB penanda=0. ✅
  - **Phase B (eligible):** deliverable semua Approved → `1 penanda dibuat`. ✅
  - **Phase C (idempotency):** POST ulang → `0 dibuat, 1 dilewati`, no duplikat. ✅
  - **CSRF:** POST tanpa `__RequestVerificationToken` → **HTTP 400** (`[ValidateAntiForgeryToken]` aktif). ✅
  - **A-M10 + Pitfall 4:** penanda `CompletedAt=2026-03-24` (=exam.CompletedAt, BUKAN now); assignment inactive Id9 (`AssignedAt 2026-03-20 ≤ CompletedAt`) ter-resolve. ✅
  - DB restored bersih (penanda 0, seed gone, kolom Origin intact). Journal `cleaned`.
- **Interview path:** logic-verified — refactor pakai `EnsureAsync(Interview)` (sama helper idempotent yg proven Plan 02 [Fact] + Plan 03 live). Perilaku lama (JSON + penanda) utuh.

## Threat mitigations
- T-358-07 (IDOR): `[Authorize(Roles="Admin")]` (lebih ketat dari Admin,HC). ✅
- T-358-08 (CSRF): `[ValidateAntiForgeryToken]` → 400 tanpa token. ✅ (live-verified)
- T-358-09 (info-leak): pesan generik TempData; `ex.Message` ke log. ✅
- T-358-10 (tamper): enforce `All(Approved) && count>0`. ✅ (live Phase A)

migration=false. DB lokal bersih.
