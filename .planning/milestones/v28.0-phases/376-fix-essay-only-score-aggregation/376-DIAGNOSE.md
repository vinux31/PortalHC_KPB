# Phase 376 — DIAGNOSE (SC1, diagnose-first)

**Tanggal:** 2026-06-14
**Metode repro:** e2e `tests/e2e/exam-types.spec.ts` FLOW L (Essay-only Full Cycle) `--workers=1`, app lokal `Authentication__UseActiveDirectory=false` + `lpc:` conn (localhost:5277), DB `HcPortalDB_Dev`. Harness auto snapshot→seed→restore (global.setup/teardown).
**Status:** ⚠️ **BUG TIDAK REPRODUCE di code current.** Root cause historis teridentifikasi + sudah ke-fix incidental oleh v27.0.

---

## 1. Ringkasan Eksekutif

Bug yang dilaporkan (backlog **999.8** / e2e L6 `.fixme` Phase 364): *essay-only assessment finalize → `AssessmentSessions.Score=0` walau HC nilai + finalize*.

**Hasil repro lokal (2× run, deterministik):** essay-only finalize menghasilkan **`Score=80` (BENAR)**, bukan 0. Test L6 (yg di-`.fixme` karena Score=0) **PASS** saat assertion asli (`expect(score).toBe(80)` + `Status='Completed'`) dijalankan tanpa fixme.

**Kesimpulan:** Bug sudah **resolved** di codebase ITHandoff current. Backlog 999.8 + fixme L6 = **STALE**.

---

## 2. Bukti Repro (data aktual)

### Run #1 (diagnostic logger di L6)
```
__DIAG376__ sessionId=9019 score=80 status=Completed hasManualGrading=1 isPassed=1
            passPct=70 upaCount=1 shuffled=[50055]
            responses=[{"PackageQuestionId":50055,"EssayScore":80,"QuestionType":"Essay","ScoreValue":100}]
```
### Run #2 (L6 assertion asli, fixme dihapus)
```
ok 7 [chromium] › FLOW L › L6 — Worker scores 80 (DB-based verify)  (136ms)   ← PASS
7 passed (26.8s)   teardown RESTORE OK, Layer 4 OK: 0 matrix rows
```

| Sinyal | Nilai aktual | Interpretasi |
|--------|--------------|--------------|
| `AssessmentSessions.Score` | **80** | BENAR (= EssayScore/ScoreValue×100 = 80/100×100). Bukan 0. |
| `Status` | `Completed` | Finalize sukses (rowsAffected>0, replay-guard lolos). |
| `HasManualGrading` | 1 | Jalur essay (PendingGrading→Completed). |
| `IsPassed` | 1 | 80 ≥ PassPercentage 70 → lulus. Benar. |
| `UserPackageAssignments.ShuffledQuestionIds` | `[50055]` | **POPULATED** (1 soal essay). BUKAN `[]`/`{}`/null. |
| `PackageUserResponses.EssayScore` | 80 | Skor manual ter-persist benar (QuestionType=`Essay`, ScoreValue=100). |
| `upaCount` | 1 | Assignment ada (bukan jalur non-package). |

---

## 3. Root Cause LOCKED (historis) + mengapa sudah fixed

Tabel hipotesis (RESEARCH H1..H5) divalidasi terhadap data:

| Hip | Deskripsi | Verdict |
|-----|-----------|---------|
| **H1** | `GetShuffledQuestionIds()` kosong → `allQuestions` kosong → `maxScore=0` → `Score=0`, guard "semua essay dinilai" lolos vacuously | **ROOT CAUSE HISTORIS** — saat ini TIDAK aktif (shuffled `[50055]` populated). |
| H2 | QuestionType casing/whitespace ≠ "Essay" | DITOLAK — `QuestionType="Essay"` tepat. |
| H3 | Math agregasi salah | DITOLAK — math L3535-3564 sound (mixed sesi 65/118 benar; essay-only kini 80). |
| H4 | Race rowsAffected=0 → interim 0 nempel | DITOLAK — Status=Completed, finalize menulis. |
| H5 | Proton hook 358 menimpa | DITOLAK — FLOW L kategori IHT (non-Proton); Proton tak ada essay. |

**Cerita root cause (evidence-based):**
- `FinalizeEssayGrading` (AssessmentAdminController.cs L3469-3669) — math agregasi inline L3535-3564 **TIDAK berubah** sejak Phase 298 (`f7390f58`). Question-set diturunkan dari `packageAssignment.GetShuffledQuestionIds()` (L3512).
- Era Phase 364 / v25.0: pembangkitan `ShuffledQuestionIds` essay-only **malformed/empty** (bug shuffle pra-v27, termasuk "{}" yg di-fix Phase 373). `shuffledIds` kosong → `allQuestions`(WHERE `shuffledIds.Contains(q.Id)`) kosong → `maxScore=0` → `finalPercentage=0` → **Score=0**. Guard `essayResponses.Any(EssayScore==null)` lolos vacuously (`Any` set kosong = false), finalize lanjut tulis 0. **= H1.**
- **v27.0 Phase 373** (`cdc1cc8a` "wire reshuffle endpoints to ShuffleEngine + fix hard-coded {} option bug") + ShuffleEngine rewrite (`Helpers/ShuffleEngine.cs`) → `ShuffledQuestionIds` kini **selalu terisi benar** utk essay-only package-path (single: `q.OrderBy(Order)`; ≥2: round-robin). → agregasi lihat soal essay → `Score=80`. **Bug ke-fix incidental.**

**Catatan prod:** Bundle v24-v27 **BELUM di-push ke prod** (memory project). Prod masih jalan code lama (bug LIVE). Sesi essay-only yg di-finalize di prod SEBELUM deploy bundle = `Score=0` (rusak). → tool recompute (Plan 03) tetap berguna utk repair baris historis pasca-deploy.

---

## 4. Keputusan turunan utk Plan 02/03

- **Derivasi fallback question-set (open-Q2):** bila `shuffledIds` kosong → derive dari `PackageUserResponses` session (`Distinct PackageQuestionId`). Aman utk essay-only (≥1 response pasti ada). Dipakai sbg **guard defensif D-06** (insurance anti-regresi shuffle masa depan / edge ≥2 paket dgn paket kosong) — BUKAN fix code yg sudah benar.
- **Predicate kandidat recompute (open-Q3/Q5):** `Status='Completed' AND HasManualGrading=1 AND (Score IS NULL OR Score=0)`. Lokal current = 0 baris (semua benar). Predicate sasar baris **prod historis** pasca-deploy.
- **Formula D-04** terkonfirmasi benar live: `(int)(totalScore/maxScore×100)` = 80/100×100 = 80, IsPassed = 80≥70.

---

## 5. Arah Phase 376 (revisi — Option 1, dikonfirmasi user 2026-06-14)

Premis fase (bug reproducible butuh forward-fix) **gugur** (Rule-4 deviation). Value bergeser:
1. **Regression-lock** incidental fix v27 — un-fixme e2e L6 (kini PASS) + xUnit `AssessmentScoreAggregator` test. Cegah regresi senyap.
2. **Hardening** — ekstrak helper `AssessmentScoreAggregator` (kill-drift D-02) + wire FinalizeEssayGrading + guard fallback/maxScore=0 (D-05/D-06) sbg insurance.
3. **Prod-repair** — endpoint `RecomputeEssayScores` (Plan 03) utk baris prod historis Score=0 pasca-deploy bundle.

Forward-"fix" Plan 02 = **hardening**, bukan perbaikan code rusak (code sudah benar).

---

## 6. Catatan kebersihan

- 2× e2e run: harness auto snapshot→restore. DB lokal kembali bersih (58 sessions, 0 baris 9019/matrix leftover). Verified.
- `tests/e2e/exam-types.spec.ts` di-restore (`git checkout`) setelah tiap diagnostic run — TIDAK ada edit diagnostic yg nempel (un-fixme resmi = Plan 01 Task 2).
- TIDAK menyentuh DB Dev/Prod (CLAUDE.md — lokal only).
