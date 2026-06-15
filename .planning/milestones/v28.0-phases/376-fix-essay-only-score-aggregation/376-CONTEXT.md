# Phase 376: Fix Essay-Only Score Aggregation - Context

**Gathered:** 2026-06-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix bug grading backend: assessment **essay-only** saat HC nilai semua essay + `FinalizeEssayGrading` → `AssessmentSessions.Score=0` walau badge/status sudah "Sudah Dinilai"/Completed. **Diagnose root cause dulu** (kandidat: `GetShuffledQuestionIds()` mengembalikan list kosong → `allQuestions`/`maxScore=0` → `finalPercentage=0` DAN guard "semua essay dinilai" lolos vacuously; vs salah math agregasi; vs hook Proton completion Phase 358 menimpa). Lalu fix agregasi skor manual essay ke `Score`, **konsisten dengan jalur mixed (MC+MA+essay)**. Plus regression test.

**REQ:** GRADE-01, GRADE-02. **Migration=false. Backend-only — no view change.** Local-only sampai push IT.

**Di luar boundary (kapabilitas baru → fase lain):** perubahan UI grading, redesign alur finalize, full test-harness rewrite (e2e migrasi = Phase 379).
</domain>

<decisions>
## Implementation Decisions

### Penanganan data lama (backfill)
- **D-01:** **Forward-fix + recompute tool untuk IT.** Fix kode (forward path benar) + sediakan mekanisme recompute idempotent untuk repair baris essay-only lama yang `Score=0` padahal sudah dinilai+finalized. Developer verifikasi lokal; **eksekusi di DB Dev/Prod = tanggung jawab IT** (per CLAUDE.md — developer tak edit/push DB langsung). Tool diserahkan + di-flag ke IT bersama handoff.
- **D-02:** Bentuk tool = **endpoint admin** (POST) yang **me-reuse helper agregasi bersama** — bukan SQL script. Ekstrak logika agregasi skor (saat ini inline di `FinalizeEssayGrading` L3535-3564) ke **helper bersama / shared core** (pola kill-drift Phase 363/365), dipakai BERSAMA oleh forward-path finalize DAN endpoint recompute → single source of truth, hindari duplikasi math di SQL. Endpoint: gated role HC/Admin + antiforgery + audit log + **idempotent** (hanya sentuh baris kandidat: essay-only/HasManualGrading, Status=Completed, Score=0, semua EssayScore terisi).

### Side-effect recompute (baris lama yang di-repair)
- **D-03:** Recompute repair **HANYA `Score` + `IsPassed`** (perbaiki angka). Sertifikat + penanda Proton `Origin="Exam"` + `NotifyIfGroupCompleted` **TIDAK** auto-terbit retroaktif/massal — hindari ledakan notifikasi grup & nomor cert untuk data historis saat IT jalankan di prod. Bila butuh sertifikat untuk baris repaired yang lulus, **HC re-trigger per-orang manual**. (Forward-path baru TIDAK berubah — tetap terbit cert/Proton saat finalize normal.)

### Definisi & konsistensi skor
- **D-04:** **Formula locked = persentase int**, persis L3564 existing: `Score = (int)((double)totalScore / maxScore * 100)`. `totalScore` = Σ `EssayScore` manual (+ MC/MA auto bila mixed). `maxScore` = Σ `ScoreValue` soal. `IsPassed = Score >= PassPercentage`. **Satu formula, dua jalur** (essay-only & mixed) → SC3 terpenuhi. Helper bersama harus deterministik dengan formula ini.
- **D-05:** Edge `maxScore=0` (mis. semua essay `ScoreValue=0`, atau `shuffledIds` masih kosong walau root-cause beda) → **pertahankan fallback `Score=0` (existing)** + **tambah log warning** saat `maxScore=0` (sinyal anomali data). **TIDAK** block finalize → perilaku eksisting terjaga, no regresi.

### Strategi fix root-cause
- **D-06:** **Targeted + guard defensif tipis.** Fix tepat root-cause hasil diagnosis (mis. bila `shuffledIds` kosong → fallback ke seluruh soal package assignment), PLUS guard defensif tipis + log di titik agregasi supaya data-shape rusak lain tak diam-diam jadi `Score=0` tanpa sinyal. Low-risk, sedikit lebih robust. **Bukan** minimal-murni (tanpa guard) maupun defensif-penuh (revalidasi semua jalur = scope/regresi lebih besar).

### Regression test (dari requirement GRADE-02)
- **D-07:** Test **dua jalur**: essay-only (yang rusak) + mixed (yang sudah benar) → hijau keduanya. xUnit (helper agregasi bersama, real-SQL bila perlu pola fixture eksisting) + e2e `tests/e2e/exam-types.spec.ts` L6 un-`.fixme` (essay finalize). Recompute endpoint juga di-cover (idempotency + hanya-sentuh-kandidat). Test approach detail = Claude discretion (planner).

### Claude's Discretion
- Lokasi & signature persis helper agregasi bersama (Services vs Helpers).
- Mekanisme deteksi baris kandidat di endpoint recompute (query predicate).
- Bentuk/route endpoint recompute + UI trigger (bila ada tombol admin) — atau headless POST.
- Struktur fixture/test detail.
- Root-cause persis (dikonfirmasi saat eksekusi SC1 — diagnose-first).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

Tidak ada spec/ADR eksternal khusus fase ini (bug-fix codebase existing, promote backlog 999.8). Referensi = kode + dokumen keputusan fase terkait di bawah.

### Kode bug & jalur agregasi (WAJIB baca)
- `Controllers/AssessmentAdminController.cs` §`FinalizeEssayGrading` (L3469-3669) — jalur finalize essay HC; agregasi inline L3535-3564 (kandidat ekstrak helper D-02); replay-guard L3570 `WHERE Status==PendingGrading`.
- `Services/GradingService.cs` — `GradeAndCompleteAsync` (L56, cabang `hasEssay` L196-234 interim Score MC+MA), `RegradeAfterEditAsync` (L437), `ComputeScoreAndETInternalAsync` (L331); formula persen L200/L450/L564.
- `Models/UserPackageAssignment.cs` §`GetShuffledQuestionIds` (L60-70) — **return list kosong saat `ShuffledQuestionIds` null/empty/JSON rusak** (root-cause candidate utama).
- `Models/AssessmentConstants.cs` — `AssessmentStatus` (PendingGrading="Menunggu Penilaian", Completed); `PackageUserResponse.cs` — field `EssayScore`.

### Keputusan fase terkait (carry-forward locks)
- `.planning/milestones/v15.0-phases/310-essay-finalize-idempotency/310-CONTEXT.md` — idempotency `FinalizeEssayGrading` (D-03/D-04 friendly no-op + replay guard). **Fix tak boleh rusak ini.**
- `.planning/milestones/v18.0-phases/324-fix-duplicate-trainingrecord-auto-create-on-assessment-compl/324-CONTEXT.md` — D-02 NO TrainingRecord auto-create di finalize path; `AssessmentSession` sole source-of-truth Records.
- `.planning/phases/358-penanda-kelulusan-fondasi-a/358-CONTEXT.md` — PCOMP penanda Proton `Origin="Exam"` via hook; defensive di `FinalizeEssayGrading` L3643-3655.
- v27.0 Shuffle (Phase 372-373, archive `milestones/v27.0-ROADMAP.md`) — `ShuffledQuestionIds`/`GetShuffledQuestionIds` + fix reshuffle "{}" (373); konteks kenapa shuffledIds bisa kosong.
- v22.0 Phase 345 (archive `milestones/v22.0-ROADMAP.md`) — display jujur "Menunggu Penilaian"; passRate/average exclude-pending (jangan regresi tampilan).

### Requirement & roadmap
- `.planning/REQUIREMENTS.md` — GRADE-01 (agregasi essay-only), GRADE-02 (konsisten essay-only vs mixed + regression test).
- `.planning/ROADMAP.md` Phase 376 — SC1-SC4.

### Workflow constraint
- `CLAUDE.md` / `docs/DEV_WORKFLOW.md` — Lokal→Dev→Prod; developer TAK edit/push DB Dev/Prod (eksekusi recompute = IT); flag migration/hash ke IT.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Agregasi inline `FinalizeEssayGrading` L3535-3564** — kandidat utama untuk diekstrak jadi helper bersama (D-02). Sudah handle MC/MA/Essay switch + maxScore.
- **Pola shared-core (Phase 363/365)** — ekstrak static/helper murni + test real-SQL; reuse forward + recompute kill drift.
- **Pola bulk endpoint admin (Phase 320 EditAnswer / 338 BulkBackfill restore)** — precedent endpoint gated + antiforgery + audit + idempotent untuk operasi data batch oleh HC/IT.
- **`ExecuteUpdateAsync` + WHERE status guard** — pola atomic idempotent eksisting (L3569) untuk recompute aman.

### Established Patterns
- Formula skor persen int dipakai konsisten (GradeAndCompleteAsync, RegradeAfterEditAsync, FinalizeEssayGrading) — helper bersama harus match.
- `AssessmentSession` = sole source-of-truth Records (no TrainingRecord auto-create — Phase 324).
- Audit log non-fatal (try/catch warn-only) — precedent Phase 306/310.

### Integration Points
- Forward finalize: `AssessmentAdminController.FinalizeEssayGrading` → helper agregasi (baru).
- Recompute endpoint (baru) → helper agregasi (sama) → `ExecuteUpdateAsync` baris kandidat.
- Test: `HcPortal.Tests` (xUnit) + `tests/e2e/exam-types.spec.ts` (Playwright, `--workers=1`).
</code_context>

<specifics>
## Specific Ideas

- Root-cause hypothesis kuat: `GetShuffledQuestionIds()` kosong → `allQuestions` kosong → `maxScore=0` → `Score=0`, dan guard `essayResponses.Any(r => r.EssayScore == null)` lolos vacuously (Any pada set kosong = false). Konfirmasi/refute saat SC1 (diagnose-first) — JANGAN asumsi tanpa repro lokal.
- Recompute tool diposisikan sebagai **handoff IT** (sejalan workflow Lokal→Dev→Prod), bukan auto-run developer.
</specifics>

<deferred>
## Deferred Ideas

- Retroaktif generate sertifikat + penanda Proton untuk baris repaired yang lulus (D-03 sengaja dikecualikan dari recompute massal; HC re-trigger manual bila perlu).
- Block finalize saat `maxScore=0` (ditolak D-05 demi no-regresi; bisa fase polish terpisah bila jadi kebutuhan).
- Full revalidasi semua jalur agregasi (defensif penuh — ditolak D-06 demi scope terkendali).

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship` — cleanup data test lokal pasca-367 (area: database). Match lemah (score 0.2, keyword "phase"). **Tidak di-fold** — beda konteks (cleanup seed test 367, bukan grading essay).
</deferred>

---

*Phase: 376-fix-essay-only-score-aggregation*
*Context gathered: 2026-06-14*
