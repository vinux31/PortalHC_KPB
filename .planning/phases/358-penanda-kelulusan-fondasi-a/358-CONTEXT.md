# Phase 358: Penanda Kelulusan (fondasi A) - Context

**Gathered:** 2026-06-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Bikin kelulusan Proton konsisten: exam Tahun 1/2 yang lulus ikut menerbitkan penanda `ProtonFinalAssessment` (dulu cuma interview Tahun 3), lewat helper tunggal `ProtonCompletionService`, plus migration `Origin` + backfill data lama. Fix bug "exam Tahun 1/2 lulus tak tercatat Lulus" di dashboard CDP/HistoriProton.

**Scope = PCOMP-01..05:** helper `EnsureProtonFinalAssessment`/`RemoveExamOrigin` (single source) · migration#1 `Origin` · wire `GradingService` (exam lulus + re-grade flip Pass↔Fail) · refactor `SubmitInterviewResults` ke helper · endpoint backfill 1x idempotent.

**BUKAN scope (→ phase lain):** gate eligibility server-side, `ProtonYearGate`, gate antar-tahun, Tahun 3 data-driven, graduation gate, matikan display level (PCOMP-06..10 → Phase 359). Bypass Tahun (→ Phase 360/361).
</domain>

<decisions>
## Implementation Decisions

### Sumber plan & cakupan helper
- **D-01:** Planner susun **fresh dari spec design** (`2026-06-09-proton-completion-logic-design.md`), TIDAK pakai plan draft sebagai `--prd` source. Planner re-validate semua keputusan dari spec. Plan draft (`docs/superpowers/plans/2026-06-09-proton-completion-logic.md`) = referensi sekunder (sketsa kode konkret Task 1/3/4/5/10), bukan otoritas.
- **D-02:** `ProtonYearGate` (helper murni cross-year, "Tahun N butuh N-1 lulus") **ditahan ke Phase 359**. Phase 358 = penanda-only. Gate antar-tahun = PCOMP-07 = Phase 359. Jangan bangun ProtonYearGate di 358.

### Helper & penanda (PCOMP-03/04)
- **D-03:** Satu helper idempotent `ProtonCompletionService` = sumber TUNGGAL pembuatan/penghapusan penanda. `EnsureAsync(coacheeId, protonTrackId, createdById, origin, notes)` hormati 1-penanda-per-`ProtonTrackAssignmentId`; resolve assignment aktif `(CoacheeId, ProtonTrackId, IsActive)`. `CompetencyLevelGranted=0` dormant (A-3). Register scoped DI di `Program.cs`.
- **D-04:** Kolom `Origin` nullable `[MaxLength(20)]` values `"Exam"`/`"Interview"`/`"Bypass"`. Migration set baris lama → `"Interview"`; backfill set `"Exam"`; helper set eksplisit. Re-grade Pass→Fail hapus penanda **HANYA `Origin=="Exam"`** — Bypass/Interview kebal (A-M9). Kolom `CompetencyLevelGranted` dibiarkan dormant (tidak di-drop, no migration utk itu).
- **D-05:** Guard helper (A-M11): jalan HANYA kalau `Category=="Assessment Proton"` && `IsPassed` && `ProtonTrackId.HasValue`. **Path Essay N/A** — Proton = Standard-only (spec §4.9). Jangan kepicu di exam non-Proton/Pre-Test.

### Wire GradingService (PCOMP-01/02)
- **D-06:** `GradeAndCompleteAsync` — exam Proton lulus → `EnsureAsync(Origin="Exam")`. `RegradeAfterEditAsync`: cabang Fail→Pass → `EnsureAsync(Origin="Exam")`; cabang Pass→Fail → `RemoveExamOriginAsync` (selektif Origin=Exam). GradingService sekarang belum baca `ProtonTrackId` → tambah pembacaan + guard D-05.
- **D-07:** `SubmitInterviewResults` (Tahun 3, AssessmentAdminController.cs:~3737-3766) refactor inline-create → panggil helper `EnsureAsync(Origin="Interview")`. Perilaku lama tak berubah. Hati-hati urutan SaveChanges: session (InterviewResultsJson) + penanda dua-duanya harus tersimpan.

### Backfill (PCOMP-05)
- **D-08:** Backfill **ENFORCE deliverable 100% approved** — spec §4.7 + PCOMP-05 eksplisit. Hanya terbitkan penanda untuk `AssessmentSession` Proton Tahun 1/2 `IsPassed==true` + deliverable track 100% approved + belum ada penanda. (Plan draft Task 10 tulis cek-100% "opsional" = **drift dari spec; ABAIKAN, ikut spec**.) Idempotent, pakai helper.
- **D-09:** Backfill assignment resolution (A-M10): match `(coachee, exam.ProtonTrackId)` — bisa **inactive** & bisa **>1**. Pilih assignment paling sesuai era exam: `AssignedAt` terdekat **sebelum** `exam.CompletedAt`. Log sesi yang di-skip (tak ada assignment match).

### Claude's Discretion
- **Mekanisme backfill** (endpoint admin POST idempotent vs migration data-script): planner pilih. Spec §4.7 bilang "script/migration"; plan draft pilih endpoint admin (`POST /Admin/BackfillProtonPenanda`, lebih operasional utk IT). Dua-duanya OK asal 1x + idempotent + snapshot DB dulu. **Lean: endpoint admin** (audit + admin-trigger jelas).
- Strategi test integration `ProtonCompletionService` (fixture real-SQL TEST-05 disposable) — planner detail.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & requirements (otoritas)
- `docs/superpowers/specs/2026-06-09-proton-completion-logic-design.md` — **Spec final Diskusi A** (sumber otoritas). Keputusan A-1..A-5.2, edge A-M1..M13, perubahan kode §4.1-4.10. Khusus §4.9 (Standard-only/guard), §4.7+A-M10 (backfill), §4.8 (Origin migration).
- `.planning/REQUIREMENTS.md` §PCOMP — PCOMP-01..05 (scope 358); PCOMP-06..10 (Phase 359, konteks dependency).

### Referensi sekunder
- `docs/superpowers/plans/2026-06-09-proton-completion-logic.md` — plan DRAFT (sketsa kode Task 1/3/4/5/10 relevan 358; Task 6-9 → 359; Task 2 ProtonYearGate → 359 per D-02). **Bukan --prd source** (D-01); pakai sebagai contoh konkret, bukan otoritas.

### Workflow & ops
- `docs/DEV_WORKFLOW.md` — SOP migration + notify IT flag migration (migration#1 `Origin`).
- `docs/SEED_WORKFLOW.md` — snapshot/restore DB lokal sebelum apply migration + sebelum jalan backfill.
- `CLAUDE.md` — Develop Workflow (lokal→Dev→Prod) + Seed Data Workflow.

### Pattern (reuse)
- `docs/superpowers/specs/2026-06-06-coach-coachee-assign-audit-fix.md` — Phase 356 `CoacheeEligibilityCalculator.IsEligiblePerUnit` (reuse terutama gate Phase 359; pattern helper murni unit-testable).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Models/ProtonModels.cs:207-226` (`ProtonFinalAssessment`) — tambah `Origin`.
- `Services/GradingService.cs` — `GradeAndCompleteAsync` (~L49/262-299) + `RegradeAfterEditAsync` (L420-516, cabang L458 Pass→Fail / L471 Fail→Pass). Sekarang belum baca `ProtonTrackId`.
- `Controllers/AssessmentAdminController.cs:~3737-3766` — `SubmitInterviewResults` inline-create penanda (refactor ke helper).
- `Helpers/CoacheeEligibilityCalculator.IsEligiblePerUnit` (Phase 356) — cek deliverable per-unit (backfill 100% + gate 359).

### Established Patterns
- DI scoped registration di `Program.cs` (`builder.Services.AddScoped<...>`).
- Test integration real-SQL disposable fixture (Phase 344 TEST-05) untuk service ber-DbContext.
- Helper murni static unit-testable tanpa DbContext (pola `CoacheeEligibilityCalculator`).

### Integration Points
- `Program.cs` — register `ProtonCompletionService`.
- `GradingService` constructor — inject service (pola field DI existing, mis. `_workerDataService`).
- `AssessmentAdminController` constructor — inject untuk interview refactor + (endpoint backfill bisa di `AdminController`).

</code_context>

<specifics>
## Specific Ideas

- **Single-source rationale:** helper bersama dipanggil 3 jalur (exam/interview/bypass) supaya Bypass (Phase 360, `Origin="Bypass"`) tinggal pakai helper yang sama — itu sebab `Origin` selektif penting di re-grade.
- **Dashboard tak berubah:** CDP:3204 (`allApproved && penanda ada`) tetap baca penanda; kita cuma bikin penanda terbit untuk Tahun 1/2 (A-4).
- **Edge acceptable (spec §6):** deliverable di-unapprove setelah penanda terbit → penanda tetap (historis), `allApproved` jadi false → status turun. Tak perlu auto-hapus penanda.

</specifics>

<deferred>
## Deferred Ideas

- **`ProtonYearGate` + gate antar-tahun + gate eligibility server-side + Tahun 3 data-driven + graduation gate + matikan display level** → **Phase 359** (PCOMP-06..10). D-02.
- **Bypass Tahun** (tabel `PendingProtonBypass`, 4 closure mode, notif, UI) → **Phase 360/361** (Diskusi B).
- **Drop kolom `CompetencyLevelGranted`** → tidak pernah (dibiarkan dormant, A-3/A-M5).
- **Konfigurasi gate via UI** → out of scope (gate = aturan tetap di kode).

### Reviewed Todos (not folded)
None — `todo match-phase 358` = 0 match.

</deferred>

---

*Phase: 358-penanda-kelulusan-fondasi-a*
*Context gathered: 2026-06-10*
