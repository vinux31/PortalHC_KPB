# Research Summary

**Milestone:** v15.0 Audit Findings 27 April 2026
**Synthesized:** 2026-04-28
**Source documents:** STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md (all 2026-04-28)

## Top-Line Recommendations

1. **Tidak perlu paket NuGet/JS/tooling tambahan.** Semua 11 fix dapat dikerjakan dengan stack existing (Bootstrap 5, EF Core 8, IMemoryCache, ILogger, jQuery validate).
2. **Tidak perlu service extraction baru.** YAGNI — semua fix tetap inline di action method/view existing. Mirror pattern existing (`CertificatePdf` try-catch, `ModelState.Remove`, `ExecuteUpdateAsync` replay guard).
3. **Wave-based execution disarankan:** UI label (T1+T5+T6+T7) → UI behavior (T2+T4+T11) → defensif/state (T10+T9) → perf (T3) → deferred (T8).
4. **Cross-cutting impact ZERO** — tidak ada perubahan ke `AdminBaseController`, `IAuthService`, `IWorkerDataService`, `GradingService`, `_Layout.cshtml`, atau `wwwroot/js/site.js`.

## Stack Additions

**TIDAK ADA.** Existing stack mencukupi:
- Bootstrap 5.3.0 + Bootstrap Icons 1.10.0 (T1, semua UI)
- EF Core 8 + AsNoTracking + Migration tooling (T3)
- IMemoryCache (sudah `AddMemoryCache()` di `Program.cs:17`) — T3
- ILogger default (T10)
- UseExceptionHandler (sudah di `Program.cs:155`)
- ExecuteUpdateAsync replay guard pattern (T9 — sudah ada baris 2778–2784)
- ModelState.Remove pattern (T11 — 5+ usage existing)

## Feature Table Stakes (per Temuan)

| # | Table Stakes |
|---|--------------|
| T1 | Eye icon toggle + aria-label + keyboard accessible + button type="button" |
| T2 | Editable di semua tipe (MC/MA/Essay) range 1–100, server-side hapus override |
| T3 | AsNoTracking + projection + DB index + IMemoryCache for dropdown |
| T4 | Live count badge + real-time list + DRY function reuse Step 2/Step 4 |
| T5/T6 | Label "(WIB)" eksplisit di setiap input + konsisten di summary |
| T7 | Label explicit "1 jawaban benar" vs "≥2 jawaban benar"; enum/DB tetap |
| T9 | Guard check (sudah ada) + friendly UI message + hide tombol post-completed + dedupe notif |
| T10 | Try-catch per-action + structured logging + null-safe view + TempData redirect |
| T11 | JS set value programmatic + ModelState.Remove conditional + jQuery validate re-parse |

## Watch Out For (Top Pitfalls)

1. **T2 server-side override** — `AssessmentAdminController.CreateQuestion` baris **4681** dan `EditQuestion` baris **4822** memaksa `scoreValue = 10`. **Wajib di-patch.** View-only fix TIDAK CUKUP.
2. **T3 N+1 risk** — drop `Include(a => a.User)` bisa jadi client-eval. Verify generated SQL = single JOIN via EF logging.
3. **T9 race condition** — concurrent click "Selesaikan". `ExecuteUpdateAsync` CAS sudah ada — jangan dirusak. Tambah dedupe untuk `NotifyIfGroupCompleted`.
4. **T10 generic catch hides root cause** — pakai specific exception catches + structured log fields.
5. **T11 mode switching regression** — toggle Standard ↔ PrePost harus idempotent. Test matrix 4 kombinasi.
6. **T7 documentation drift** — update PDF panduan + E2E tests + Excel template bersamaan dengan label change.
7. **File conflict** — T4, T5, T6, T11 semua di `Views/Admin/CreateAssessment.cshtml`. Serialize dalam 1 worktree.
8. **Phase 247 overlap** — T9 NotifyIfGroupCompleted overlaps dengan pending UAT di v8.6.

## Architecture Integration Map

| Layer | Touched | Files |
|-------|---------|-------|
| Views | YES | Login.cshtml, ManagePackageQuestions.cshtml, CreateAssessment.cshtml, _PreviewQuestion.cshtml, StartExam.cshtml, ExamSummary.cshtml, Certificate.cshtml |
| Controllers | YES (5 lokasi) | AssessmentAdminController.CreateQuestion (4681), EditQuestion (4822), ManageAssessment (57–188), CreateAssessment POST (730–940), FinalizeEssayGrading (2710–2827); CMPController.Certificate (1771–1811) + ResolveCategorySignatory (1813–1838) |
| Services | NO | — |
| Models | NO | — |
| DB Migrations | YES (T3 only) | Index `IX_AssessmentSessions_Schedule_ExamWindowCloseDate` + `IX_LinkedGroupId` (cek dulu apakah sudah ada) |
| AdminBaseController | NO | — |
| Site-wide JS/CSS | NO | — |

## Recommended Phase Structure

**8 phase** disarankan (lanjut dari Phase 303):

| # | Phase Name | Temuan | Risk | Effort |
|---|------------|--------|------|--------|
| 304 | UI Label Polish | T1, T5, T6 | Low | S |
| 305 | Question Type Naming Clarity | T7 | Low (UI), Medium (docs) | S |
| 306 | Score Editable per Question Type | T2 | Medium | M |
| 307 | Selected Participants Inline View | T4 | Low | S |
| 308 | PrePost Wizard Validation Fix | T11 | Medium | M |
| 309 | Worker Certificate Defensive Fix | T10 | Medium-High | M |
| 310 | Essay Finalize Idempotency | T9 | Medium-High | M |
| 311 | ManageAssessment Performance | T3 | Medium | M-L |

**T8 — DEFERRED**, tracked di STATE.md dengan due date. Akan masuk phase tersendiri (mis. 312) setelah user konfirmasi Jalur A vs B.

## Decision Surface for Roadmapper

- **Keep phases small.** Mayoritas temuan adalah single-commit changes.
- **Wave 1 dapat di-batch.** T1 + T5 + T6 ke Phase 304 sebagai "UI Label Polish" — semua pure-view label changes. T7 dipisah karena ada cross-cutting docs impact.
- **File conflict di CreateAssessment.cshtml** — Phase 304, 307, 308 semua menyentuh file ini. Sequential execution (bukan parallel) dalam 1 worktree.
- **Phase 309 (T10) dan Phase 310 (T9)** tidak punya file overlap, bisa parallel jika ada kapasitas.
- **Phase 311 (T3) di akhir** — measurement-driven, butuh baseline + DB migration.

## Cross-Cutting Backlog (Future v16.0+)

Patterns yang muncul, layak dijadikan milestone tersendiri:

1. Accessibility audit menyeluruh (lanjutan T1, T4)
2. Idempotency pattern propagation (Reset, ForceClose, BulkClose)
3. Defensive programming pattern audit (semua complex read-actions)
4. Multi-step validation review (semua wizard di portal)

---
*Synthesized for: v15.0 Audit Findings 27 April 2026*
*Date: 2026-04-28*
