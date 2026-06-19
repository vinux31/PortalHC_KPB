---
phase: 395-mode-jawaban-input-asli-auto-generate
plan: 01
subsystem: inject-assessment / auto-generate
tags: [inject, auto-generate, subset-sum, sha256-seed, grading, tdd]
requires:
  - "AssessmentScoreAggregator.Compute (formula floor + lulus '>=', Helpers/AssessmentScoreAggregator.cs:26-60)"
  - "InjectAnswerSpec / InjectQuestionSpec DTO (Models/InjectAssessmentDtos.cs, dari Phase 393)"
  - "PreflightValidateAsync essay branch (Services/InjectAssessmentService.cs, dari Phase 393)"
provides:
  - "InjectAssessmentService.BuildAutoGenAnswers (static-pure subset-sum hit-target)"
  - "InjectAssessmentService.ComputeAutoGenSeed (SHA-256 seed deterministik lintas-proses)"
  - "InjectAssessmentService.AutoGenResult record (Answers/CeilingPercent/MaxScore/TargetReachable)"
  - "Rule TextAnswer-wajib (D-04 mode-guarded) di PreflightValidateAsync"
affects:
  - "Phase 396 (Import Excel) — reuse BuildAutoGenAnswers/ComputeAutoGenSeed server-side"
  - "Phase 395 Plan 02 (controller) — konsumsi helper untuk auto-gen + preview"
  - "Phase 395 Plan 03 (view) — UI mode auto-gen + Pratinjau"
tech-stack:
  added:
    - "System.Security.Cryptography.SHA256 (BCL — pertama kali dipakai di repo; untuk seed determinisme, non-secret)"
  patterns:
    - "Subset-sum hit-target deterministik: greedy by ScoreValue + seeded shuffle dalam grup equal-weight + smallest-such trim + re-cek floor() setelah seleksi"
    - "Seed lintas-proses SHA-256 atas string kanonik (NIP + room identity + target), CompletedAt date-only, unit-separator U+001F"
    - "static-pure helper EF-free di service (reuse Phase 396), unit-testable tanpa DB (pola AssessmentScoreAggregator)"
key-files:
  created:
    - "HcPortal.Tests/BuildAutoGenAnswersTests.cs (unit pure, 30 test, no Integration trait)"
  modified:
    - "Services/InjectAssessmentService.cs (+AutoGenResult +ComputeAutoGenSeed +BuildAutoGenAnswers +rule D-04)"
decisions:
  - "BuildAutoGenAnswers re-cek floor((int)((double)total/max*100)) SETELAH seleksi subset (boundary off-by-one) — BUKAN percaya k=ceil(target*N/100)"
  - "Ceiling MC/MA-only: target>ceiling -> TargetReachable=false (D-08.3 BLOCKING, JANGAN cap diam-diam); best-effort=semua MC/MA benar untuk laporkan ceiling"
  - "ComputeAutoGenSeed pakai SHA-256 (BUKAN string.GetHashCode yang randomized per-proses) + CompletedAt date-only agar preview==commit"
  - "Rule TextAnswer-wajib ber-guard EssayScore.HasValue (essay engaged) — bukan global; skip=omit & auto-gen essay (tak di-emit) tidak terblokir"
  - "forced-correct pre-scan: MC semua-opsi-benar / soal <=1 opsi -> selalu benar; MA salah via {1 opsi salah} atau proper-subset"
metrics:
  duration: "~8 menit"
  tasks: 3
  files_created: 1
  files_modified: 1
  tests_added: 30
  completed: 2026-06-18
---

# Phase 395 Plan 01: Fondasi server-side auto-generate (BuildAutoGenAnswers + ComputeAutoGenSeed + rule TextAnswer-wajib) Summary

Lapisan translasi server-side untuk auto-generate Phase 395: helper murni `BuildAutoGenAnswers` (skor target → pola jawaban MC/MA benar/salah eksplisit, dijamin ≥ target via subset-sum + re-cek `floor()`), `ComputeAutoGenSeed` (seed deterministik lintas-proses SHA-256 atas NIP + identitas room + target), record `AutoGenResult`, dan rule validasi baru "teks essay wajib bila skor diisi" (D-04, mode-guarded) di `PreflightValidateAsync` — semua dikunci 30 unit test murni tanpa DB. Satu-satunya algoritma genuine-baru di phase 395; diletakkan di service agar Phase 396 (Import Excel) reuse.

## What Was Built

**Task 1 — RED** (`test(395-01)` @`561944f7`): `HcPortal.Tests/BuildAutoGenAnswersTests.cs` — 30 unit test murni (no DB, no `[Trait Integration]`, masuk fast suite). Membuktikan "skor ≥ target" dengan memetakan `AutoGenResult.Answers` + soal ke in-memory `PackageQuestion`/`PackageUserResponse` (TempId = Id sintetis) lalu memanggil `AssessmentScoreAggregator.Compute` (engine identik commit → preview==commit). Build gagal HANYA karena API belum ada (CS0117 "does not contain a definition for") — RED murni, bukan syntax error.

**Task 2 — GREEN** (`feat(395-01)` @`c79d27a4`): implementasi di `Services/InjectAssessmentService.cs`:
- `public sealed record AutoGenResult(List<InjectAnswerSpec> Answers, int CeilingPercent, int MaxScoreIncludingEssay, bool TargetReachable)`.
- `ComputeAutoGenSeed` — SHA-256 (`System.Security.Cryptography.SHA256.Create()` + `ComputeHash(UTF8(canonical))` + `BitConverter.ToInt32 & 0x7FFFFFFF`). String kanonik = `nip + title + category + CompletedAt(yyyy-MM-dd) + target`, dipisah unit-separator `U+001F`. CompletedAt date-only (tahan beda jam preview vs commit). **TIDAK** memakai `string.GetHashCode()`.
- `BuildAutoGenAnswers` — denominator `maxScore` = Σ ScoreValue SEMUA soal (termasuk essay); ceiling MC/MA-only = `floor(Σ(MC+MA ScoreValue)/maxScore×100)`. Bila `target > ceiling` → `TargetReachable=false` + best-effort semua MC/MA benar (D-08.3). Pre-scan forced-correct (MC semua-benar / ≤1 opsi). Seleksi WHICH soal benar: greedy `OrderByDescending(ScoreValue)` dengan shuffle ber-seed dalam grup ScoreValue-sama → akumulasi sampai `floor() >= target` → smallest-such trim → **re-cek `floor()` setelah seleksi** (tambah soal bila masih < target). Konstruksi MC/MA salah deterministik (`OrderBy(TempId)`). Essay TIDAK di-emit.
- Rule D-04 TextAnswer-wajib disisip di branch essay `PreflightValidateAsync` setelah cek range `EssayScore`, ber-guard `ans.EssayScore.HasValue` (essay engaged). Pesan Bahasa Indonesia, reject-all (kumpul error), tak menulis DB.

**Task 3 — Gate** (verification-only, no code change): fast suite penuh `dotnet test --filter "Category!=Integration"` = **381/381 GREEN** (baseline 351 + 30 baru, no regression). 0 migration dikonfirmasi: `dotnet ef migrations add _verify395p1` → Up/Down body KOSONG (0 model diff) → `dotnet ef migrations remove`. Model snapshot di-restore (`git checkout`) karena `ef` rewrite cosmetic-only (`ToTable("X")`→`ToTable("X",(string)null)`, no-op formatting tooling).

## Verification

- `dotnet test --filter "FullyQualifiedName~BuildAutoGenAnswers&Category!=Integration"` → **Passed! 30/30** (hit-target equal-weight smallest-such, mixed-weight boundary, ceiling-essay TargetReachable=false, seed reproducible/komposisi, seed→pola-beda, degenerate forced-correct, MA-salah, essay-untouched).
- `dotnet test --filter "Category!=Integration"` → **Passed! 381/381** (no regression vs baseline 351).
- `dotnet build HcPortal.csproj` → **Build succeeded** (0 error).
- 0 migration: probe migration Up/Down KOSONG → removed; snapshot restored.
- grep `SHA256` ada di `InjectAssessmentService.cs` (kode `:520`); `GetHashCode` HANYA muncul di doc comment (penjelasan kenapa TIDAK dipakai), bukan kode.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Karakter unit-separator (U+001F) literal di source merusak edit-matching**
- **Found during:** Task 2 (penulisan `ComputeAutoGenSeed`).
- **Issue:** Plan menentukan string kanonik dipisah `` (unit separator U+001F). Menulis literal kontrol-char `'\x1F'` langsung di source membuat Edit tool gagal exact-match (kontrol-char tak ter-render), menyebabkan duplikat metode + brace tak seimbang sementara.
- **Fix:** Bersihkan metode duplikat (`ComputeAutoGenSeed_DEAD`) via skrip Python by-line-range (hindari kontrol-char matching), kembalikan namespace-closing brace. Karakter final terverifikasi = tepat `U+001F` (Python codepoint check `0x1f`). Semantik sesuai plan (unit-separator cegah tabrakan concat).
- **Files modified:** `Services/InjectAssessmentService.cs`.
- **Commit:** `c79d27a4` (bagian dari GREEN; tidak ada commit terpisah karena belum pernah ter-commit dalam keadaan rusak).

### Catatan non-deviasi
- **STATE.md** ter-modifikasi di working tree sejak SEBELUM plan dimulai (orchestrator set `status: executing`); bukan perubahan plan ini — diurus di langkah state-update.
- **Untracked** `docs/395-QUESTIONS.json` + `docs/KPB - Licensor Training - SRU - Pre Test batch 1.xlsx` ada sejak awal sesi (artefak discuss), di luar scope plan — dibiarkan.
- **Carry-in 394 cosmetic LBL-02** (`injTypeLabel`/validasi "Pilihan Ganda"→"Single Answer" di `InjectAssessment.cshtml`) **TIDAK** dikerjakan di Plan 01 — file view bukan bagian `files_modified` plan ini; akan dikerjakan di Plan 03 (view) bersama UI mode auto-gen.

## Known Stubs
None — Plan 01 adalah helper backend murni dengan kontrak lengkap + test. Konsumsi UI/controller ada di Plan 02/03.

## TDD Gate Compliance
- RED gate: `test(395-01): add failing unit tests ... (RED)` @`561944f7` (build gagal CS0117 API-missing, bukan syntax error).
- GREEN gate: `feat(395-01): implement ... (GREEN)` @`c79d27a4` (30/30 pass).
- REFACTOR: tidak diperlukan (implementasi langsung bersih).

## Self-Check: PASSED
- FOUND: `HcPortal.Tests/BuildAutoGenAnswersTests.cs`
- FOUND: `Services/InjectAssessmentService.cs` (berisi `BuildAutoGenAnswers`, `ComputeAutoGenSeed`, `AutoGenResult`, rule D-04)
- FOUND commit: `561944f7` (test/RED)
- FOUND commit: `c79d27a4` (feat/GREEN)
