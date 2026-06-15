---
status: partial
phase: 375-test-uat
source: [375-VERIFICATION.md]
started: 2026-06-14
updated: 2026-06-14
---

## Current Test

UAT @ `http://localhost:5277` (`Authentication__UseActiveDirectory=false dotnet run`). Dua bagian:
- **Grup A** — ManagePackages render/toggle/lock/reminder/warning/hide → **otomatis** via `tests/e2e/shuffle.spec.ts` (`npx playwright test e2e/shuffle.spec.ts --workers=1`).
- **Grup B** — exam-taking effect (urutan soal & opsi) → **manual browser** 2 peserta (D-03), bukti visual + screenshot.

## Tests

### Grup A — ManagePackages e2e (5 skenario, Plan 02) — 5/5 PASS

| # | Skenario | Hasil |
|---|----------|-------|
| 1 | Card render + toggle default ON (migration) + uncheck Acak Soal + Simpan → `.alert-success` "berhasil disimpan" (PRG) | **PASS** |
| 2 | Lock — peserta started (StartedAt) → banner "Pengaturan pengacakan terkunci" + kedua switch + saveBtn disabled | **PASS** |
| 3 | Reminder — Post (ShuffleQuestions ON) linked Pre OFF → alert "Pre diatur OFF, Post masih ON" muncul di Post, TIDAK di Pre | **PASS** |
| 4 | Warning §9 live-JS — multi-paket ukuran beda + uncheck Acak Soal → `#shuffleSizeWarning` visible; check → hidden (no reload) | **PASS** |
| 5 | Hide — IsManualEntry → card Pengacakan `toHaveCount(0)` (tidak dirender) | **PASS** |

Bukti: `npx playwright test e2e/shuffle.spec.ts --workers=1` → **6 passed** (1 setup + 5 skenario). DB ter-restore otomatis (teardown Layer 4: 0 rows).

### Grup B — Exam-taking effect manual (SC#2, pass-bar D-03a)

Setup: matrix seed S5 (Online, paket 9009 multi-soal) + 3 soal tambah → **6 soal × 4 opsi**, ShuffleQuestions ON + ShuffleOptions ON. 2 peserta sibling-pool: **9009 = rino.prasetyo**, **9010 = iwan3**. Masing-masing StartExam, urutan soal/opsi yang dirender dibandingkan.

| # | Pass-bar (D-03a) | expected | result |
|---|------------------|----------|--------|
| B1 | **ShuffleQuestions ON → urutan soal BEDA antar 2 peserta** | Rino vs Iwan urutan soal beda | **PASS** — Rino: `S5MC#2, SoalE, S5MC#3, S5MC#1, SoalF, SoalD` (qid `50026,59002,50027,50025,59003,59001`) vs Iwan: `SoalF, S5MC#1, SoalE, SoalD, S5MC#3, S5MC#2` (qid `59003,50025,59002,59001,50027,50026`). Urutan **berbeda total**. Screenshot: `docs/uat-evidence/375-exam-rino-9009.png`, `375-exam-iwan3-9010.png` |
| B2 | **ShuffleOptions ON → urutan opsi BEDA antar 2 peserta** | opsi salah satu soal beda | **PASS** — soal "S5 MC #2": Rino opsi `[Jawaban D, Jawaban C, Jawaban A(benar), Jawaban B]` vs Iwan `[Jawaban B, Jawaban D, Jawaban A(benar), Jawaban C]`. Posisi opsi **berbeda** (jawaban benar tetap "Jawaban A (benar)", hanya posisi berpindah). Lihat 2 screenshot. |
| B3 | **ShuffleQuestions OFF + ≥2 paket → tiap worker 1 paket UTUH urutan asli (round-robin index)** | worker beda dapat paket beda utuh | **PASS (live)** — assessment 2 paket (PN1=9009 6 soal, PN2=9999 4 soal), ShuffleQuestions OFF + ShuffleOptions OFF. **Rino (worker 0, idx 0 → PN1)** dapat paket A utuh: `S5 MC #1, #2, #3, SoalD, SoalE, SoalF` urutan asli `q.Order`, opsi urutan asli (A=benar di posisi A). **Iwan (worker 1, idx 1 → PN2)** dapat paket B utuh: `PaketB Soal 1,2,3,4` urutan asli. Paket **berbeda per worker** (round-robin `workerIndex % count`), tiap worker 1 paket UTUH. Screenshot: `docs/uat-evidence/375-examOFF-rino-9009-paketA.png`, `375-examOFF-iwan3-9010-paketB.png`. Diperkuat xUnit hijau: `ShuffleEngineTests.Off_MultiPackage_WorkerIndexMapsToPackage` (4 InlineData) + `ShuffleModeMatrixTests (false,*,2)`. |

## Summary

- total: 8 (Grup A 5 + Grup B 3)
- passed: 8 (semua live di browser)
- issues: 0
- pending: checkpoint human-approve (Grup B bukti visual)

Ketiga pass-bar SC#2 terbukti **live di browser** dengan 2 peserta nyata (rino + iwan3) + screenshot: B1 (ON urutan soal beda), B2 (ON urutan opsi beda), B3 (OFF ≥2 paket round-robin — tiap worker 1 paket UTUH berbeda urutan asli). Diperkuat 6 xUnit shuffle hijau (engine determinism semua mode).

## Gaps

None — ketiga pass-bar SC#2 live-verified + 5 skenario ManagePackages PASS.

## Catatan

- Metode Grup B: app live @5277 di-drive untuk gather bukti visual (manual UAT). Seed = matrix S5 (temporary + local-only) + 3 soal tambah, snapshot/RESTORE per `docs/SEED_WORKFLOW.md` (D-04). Bukan automated assertion permanen (D-03 menolak order-diff e2e permanen; ini one-off evidence + screenshot untuk review human).
- Pass-bar B3 boleh di-upgrade ke bukti visual oleh reviewer; status doc tetap `partial` per D-08 (manual-approve checkpoint memenuhi SC#2; verifier terima manual-UAT — render-conditional + exam-effect = Razor/JS runtime, dijustifikasi 374-VALIDATION Manual-Only).
