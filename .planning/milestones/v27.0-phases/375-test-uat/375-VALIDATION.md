---
phase: 375
slug: test-uat
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-14
updated: 2026-06-14
---

# Phase 375 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

Phase 375 **IS** the Test & UAT phase for the v27.0 shuffle toggle (closes the render-conditional + exam-effect verifications that Phases 373/374 deferred here). Its deliverables ARE the validation artifacts: xUnit consolidation sweep + Playwright e2e + manual exam-diff. Gap analysis therefore checks that every SHUF-15/SHUF-16 behavior has automated coverage where feasible, with manual-only items explicitly justified.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (net8.0, `HcPortal.Tests`) + Playwright (`tests/e2e`, `--workers=1`) |
| **Config file** | xUnit: konvensi `HcPortal.Tests/*.cs`; e2e: `tests/e2e/*.spec.ts` + `tests/helpers/dbSnapshot.ts` (localhost-guard) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~Shuffle"` (19 shuffle test) |
| **Full suite command** | `dotnet test` (352/352) + `npx playwright test e2e/shuffle.spec.ts --workers=1` (6 passed incl setup) |
| **Estimated runtime** | Shuffle subset ~5s; full xUnit ~2m28s (real-SQL fixtures); e2e ~beberapa menit |

---

## Sampling Rate

- **After every task commit:** `dotnet build` + `dotnet test --filter "FullyQualifiedName~Shuffle"`
- **After every plan wave:** Full `dotnet test` + `npx playwright test e2e/shuffle.spec.ts --workers=1`
- **Before `/gsd-verify-work`:** Full suite hijau (352/352) + exam-diff manual @localhost:5277
- **Max feedback latency:** ~5s (shuffle subset per task)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 375-01 | 01 | 1 | SHUF-15 | T-375-01 | CMPController clean (komentar stale fixed + helper duplikat → ShuffleEngine), no regresi | unit + regression suite | `dotnet test` (352/352) | ✅ `HcPortal.Tests/ShuffleModeMatrixTests.cs` | ✅ green |
| 375-01 | 01 | 1 | SHUF-15 | — | Mode-matrix determinism semua mode (ON/OFF × 1/≥2 paket) + DivideByZero guard (T-373-01) | unit (Theory 4 InlineData + guard Fact) | `dotnet test --filter "FullyQualifiedName~ShuffleModeMatrixTests"` | ✅ ShuffleModeMatrixTests (5) | ✅ green |
| 375-01 | 01 | 1 | SHUF-16 (engine) | — | OFF+≥2 paket round-robin `workerIndex % count` → 1 paket utuh urutan asli (B3 backing) | unit (Theory 4 InlineData) | `dotnet test --filter "FullyQualifiedName~ShuffleEngineTests"` | ✅ ShuffleEngineTests (14) | ✅ green |
| 375-02 | 02 | 1 | SHUF-16 (ManagePackages render) | T-375-02 | Card render + save-PRG, lock disabled+banner, reminder Pre/Post, warning §9 live-JS, hide IsManualEntry | e2e Playwright (5 skenario) | `npx playwright test e2e/shuffle.spec.ts --workers=1` | ✅ `tests/e2e/shuffle.spec.ts` | ✅ green (5/5) |
| 375-03 | 03 | 2 | SHUF-16 (exam-effect visual) | T-375-03 | ON soal/opsi beda antar 2 peserta (B1/B2); OFF+≥2 paket round-robin paket utuh (B3) — VISUAL render | manual (Razor/JS runtime) | — (D-03: automated order-diff permanen ditolak by design) | n/a (manual) | ✅ live-verified 3/3 |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.* xUnit (352/352 baseline) + Playwright e2e (`tests/helpers/dbSnapshot.ts` localhost-guard, `image-in-assessment.spec.ts` template) already terinstall. Phase 375 menambah test files (`ShuffleModeMatrixTests.cs`, `shuffle.spec.ts`), bukan setup framework.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Exam render — ShuffleQuestions ON → urutan soal beda antar 2 peserta (B1) | SHUF-16 | Razor/JS runtime render order; engine determinism sudah automated, tapi efek visual di view = runtime. D-03 menolak automated order-diff permanen (brittle, anti-pattern). | `Authentication__UseActiveDirectory=false dotnet run` → 2 peserta StartExam `/CMP/StartExam/{id}` → banding urutan soal. Bukti 375-HUMAN-UAT B1 (qid Rino vs Iwan set-sama urutan-beda) + screenshot. |
| Exam render — ShuffleOptions ON → urutan opsi beda antar 2 peserta (B2) | SHUF-16 | Sama — render opsi runtime di view | Banding urutan opsi salah satu soal. Bukti 375-HUMAN-UAT B2 (S5 MC#2 posisi opsi beda, jawaban benar tetap). |
| Exam render — ShuffleQuestions OFF + ≥2 paket → tiap worker 1 paket utuh round-robin (B3) | SHUF-16 | Visual render paket-utuh; logic round-robin sudah automated (ShuffleEngineTests) | Banding paket per worker. Bukti 375-HUMAN-UAT B3 (Rino worker0→PN1, Iwan worker1→PN2). Diperkuat `ShuffleEngineTests.Off_MultiPackage_WorkerIndexMapsToPackage` + `ShuffleModeMatrixTests (false,*,2)`. |

*Exam-effect VISUAL order-diff sengaja manual-only (D-03 by design, pola 374-VALIDATION Manual-Only). Engine determinism yang MENGHASILKAN diff = 100% automated (19 shuffle test). ManagePackages render-conditional yang 374 defer = sekarang automated via shuffle.spec.ts (5/5). Manual-approve checkpoint di-resolve via verifikasi otomatis (dotnet test 352/352 + 7-skeptik adversarial) 2026-06-14.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify atau manual-only justified (exam-effect visual → D-03)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (existing infra + new test files)
- [x] No watch-mode flags
- [x] Feedback latency < 30s (shuffle subset ~5s)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-14

---

## Validation Audit 2026-06-14

| Metric | Count |
|--------|-------|
| Requirements | 2 (SHUF-15, SHUF-16) |
| Gaps found | 0 actionable |
| Resolved (automated) | SHUF-15 (suite 352/352 + matrix) + SHUF-16 engine (ShuffleEngineTests) + SHUF-16 ManagePackages (shuffle.spec.ts 5/5) |
| Manual-only (justified) | SHUF-16 exam-effect VISUAL order-diff (B1/B2/B3) — D-03 by design, live-verified 3/3 |
| Escalated | 0 |

**Coverage map (tests delivered, all green):**
- SHUF-15 → `ShuffleModeMatrixTests` (5: 4 InlineData mode-matrix + DivideByZero guard) + full suite **352/352** + CMPController clean (V5 adversarial confirmed) — COVERED
- SHUF-16 engine → `ShuffleEngineTests` (14, incl `Off_MultiPackage_WorkerIndexMapsToPackage` 4 InlineData) — COVERED (backing B1/B2/B3 determinism)
- SHUF-16 ManagePackages render → `shuffle.spec.ts` 5 skenario (render+save-PRG, lock, reminder, warning live-JS, hide) — COVERED (automates 374 deferred render-conditional)
- SHUF-16 exam-effect visual → MANUAL-ONLY (D-03 Razor/JS runtime); 375-HUMAN-UAT B1/B2/B3 live-verified 3/3 + screenshot

**Rationale manual-only:** Exam-taking VISUAL order-diff = Razor/JS runtime render — automated order-diff permanen ditolak by design (D-03, brittle anti-pattern). Engine logic yang menghasilkan diff = 100% automated (19 test, determinism semua mode). ManagePackages render-conditional (yang 374 tandai manual) kini automated via Playwright. Nyquist-compliant: tiap behavior punya automated coverage di mana feasible; satu-satunya manual-only punya justifikasi eksplisit (D-03) + diperkuat automated engine test + di-verifikasi otomatis 2026-06-14 (dotnet test 352/352 + adversarial 6 confirmed/0 refuted). Tidak ada gap auto-fillable yang dilewati diam-diam.

---

## Validation Audit 2026-06-14 (re-audit, State A)

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 (sudah compliant) |
| Escalated | 0 |

Re-run `gsd-validate-phase 375` (State A audit existing). Test files unchanged & present: `ShuffleModeMatrixTests.cs` (5 case), `ShuffleEngineTests.cs` (14 case), `tests/e2e/shuffle.spec.ts` (5 skenario). Coverage tidak drift — `nyquist_compliant: true` dipertahankan. Tidak ada gap baru.
