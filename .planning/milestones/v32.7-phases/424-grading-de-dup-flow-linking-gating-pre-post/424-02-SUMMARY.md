# Plan 424-02 Summary — Gate + pairing + clamp + essay (CMPController)

**Status:** ✅ Complete
**Requirements:** GRDF-01, GRDF-03, GRDF-05, GRDF-07
**Commits:** `3c08c1eb` (Task 1), `727a5692` (Task 2)
**migration:** false

## What was built

**Task 1 — Gate Pre→Post + pairing UserId (`3c08c1eb`):**
- `CMPController.StartExam`: gate GRDF-01 worker-only disisipkan SETELAH cek `Completed`, SEBELUM token gate & `StartedAt` write. Post dgn pasangan Pre (`PrePostPairing.FindPairedPreAsync`, link eksplisit terfilter UserId) ber-`Status != "Completed"` → `TempData["Error"]="Selesaikan Pre-Test dulu sebelum mulai Post-Test."` + redirect Assessment. Pre Completed / orphan / Standard / Pre milik user lain → pass-through (D-01 Completed-saja bukan IsPassed; D-02). Owner-check (V4) dipertahankan di atas gate.
- Pairing display `:292-297`: tambah `&& s.UserId == userId` (GRDF-03, fix FLOW-01 cross-user pairing).
- `PrePostGatingTests` real-SQL 6/6 (block InProgress, pass Completed, orphan/Standard/user-lain null, LinkedSessionId > LinkedGroupId).

**Task 2 — Clamp ExtraTime + essay on-time reject (`727a5692`):**
- `CMPController.UpdateSessionProgress` clamp `:470`: `DurationMinutes*60` → `ExamTimeRules.AllowedExamSeconds(DurationMinutes, ExtraTimeMinutes)` (GRDF-05, root fix under-report export "Durasi Aktual").
- `CMPController.SubmitExam` on-time gate (`!serverTimerExpired`): soal Essay "terjawab" kini berbasis ISI (`!IsNullOrWhiteSpace(TextAnswer)`), bukan baris-ada. Pure helper `EvaluateOnTimeCompletion` (pola `ShouldEnforceSubmitTimer`) → blok SELURUH submit + pesan ramah ("Isi semua jawaban essay terlebih dahulu sebelum submit.") tanpa membocorkan kunci. Cabang timeout (`serverTimerExpired==true`) TIDAK disentuh → finalize PendingGrading (D-04/PXF-04 utuh).
- `EnsureCanSubmitStandardTests` +4 (empty-essay block, all-filled pass, missing-MC, MC-via-DB); `EssayEmptyPendingParityTests` 4/4 tak regress.

## Key files
- created: `HcPortal.Tests/PrePostGatingTests.cs`
- modified: `Controllers/CMPController.cs` (gate + pairing filter + clamp + essay helper), `HcPortal.Tests/EnsureCanSubmitStandardTests.cs`

## Verification
- `dotnet build` 0 error (3×).
- Task 1: `--filter PrePostGating` → 6/6.
- Task 2: `--filter EnsureCanSubmit|EssayEmptyPendingParity|ExamTimeRules` → **16/16** (DO-NOT-REGRESS timeout finalize hijau).
- Acceptance grep: `FindPairedPreAsync` di StartExam · `pairedPre.Status != "Completed"` (no IsPassed) · owner-check `assessment.UserId != user.Id` utuh · `s.UserId == userId` di pairing :292 · clamp lama `Math.Min(...DurationMinutes*60)`=0 · `ExamTimeRules.AllowedExamSeconds(session.DurationMinutes`=1 · `IsNullOrWhiteSpace` di essay gate.

## Deviations
- GRDF-07 essay-empty decision diekstrak ke pure helper `CMPController.EvaluateOnTimeCompletion` (+record `OnTimeCompletionResult`) supaya unit-testable tanpa konstruksi controller (14-dep ctor infeasible) — sesuai konvensi repo `ShouldEnforceSubmitTimer`/`EvaluateSubmitTimerDecision`. Pesan non-essay tetap tampilkan jumlah `Unanswered` (UX preserved).
- GRDF-03: minimal fix (tambah filter UserId di pairing display `:292-297`) — konvergensi penuh seluruh jalur ke `FindPairedPreAsync` tidak dipaksakan (`:3505-3523` & `:2404-2413` sudah filter UserId; gate StartExam pakai helper). Sesuai instruksi plan ("minimal WAJIB tambah filter UserId di SEMUA cabang tak terfilter").

## Self-Check: PASSED
- GRDF-01 gate worker-only (Completed-saja, orphan/Standard/user-lain pass-through). ✓
- GRDF-03 pairing terfilter UserId (FLOW-01 ditutup). ✓
- GRDF-05 clamp ExtraTime (under-report root fix). ✓
- GRDF-07 essay kosong on-time ditolak server-side; timeout finalize utuh (D-04). ✓
- 0 regresi guard (EssayEmptyPendingParity 4/4). ✓
