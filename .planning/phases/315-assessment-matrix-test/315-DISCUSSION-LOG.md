# Phase 315: Assessment Matrix Test - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-11
**Phase:** 315-assessment-matrix-test
**Areas discussed:** Helper folder layout, Wave 0 investigation approach, Notes field fallback marker, Sentinel + smoke gating

---

## Area selection

| Option | Description | Selected |
|--------|-------------|----------|
| Helper folder layout | Spec menaruh dbSnapshot/matrixReport/examMatrix di `tests/helpers/`. Phase 307 sudah putuskan e2e-specific helper di `tests/e2e/helpers/`. | ✓ |
| Wave 0 investigation approach | 5 open questions (MA save flow, Essay save flow, Notes field, ID collision, URL encoding). | ✓ |
| Notes field fallback marker | Spec asumsi `AssessmentSession.Notes` ada. Jika tidak ada: fallback strategy? | ✓ |
| Sentinel + smoke gating | Layer 3 sentinel sengaja fail. Smoke run protocol enforced atau documented? | ✓ |

**User's choice:** Semua 4 area dipilih.

---

## Helper folder layout

### Question 1: Layout helper Phase 315

| Option | Description | Selected |
|--------|-------------|----------|
| Split per Phase 307 | `dbSnapshot.ts` + `matrixTypes.ts` → `tests/helpers/`. `matrixReport.ts` + `examMatrix.ts` → `tests/e2e/helpers/`. | ✓ (setelah klarifikasi) |
| All in tests/helpers/ | Ikut spec persis. Simpler import path, tapi konflik dgn Phase 307. | |
| All in tests/e2e/helpers/ | Konsisten matrix test = e2e. Tapi dbSnapshot reuse susah nanti. | |

**User's choice:** Awalnya minta klarifikasi ("saya tidak paham ini, jelaskan dulu dengan sederhana"). Setelah re-explanation, pilih "Pisah: generic vs khusus matrix".
**Notes:** Pembagian final: `dbSnapshot.ts` → `tests/helpers/` (generic SQL util). `matrixReport.ts`, `examMatrix.ts`, `matrixTypes.ts` → `tests/e2e/helpers/` (matrix-spesifik). matrixTypes ikut e2e folder karena tipe `ScenarioConfig` cuma dipakai matrix test, bukan shared.

### Question 2: globalTeardown location

| Option | Description | Selected |
|--------|-------------|----------|
| tests/e2e/global.teardown.ts | Konsisten dgn global.setup.ts existing | ✓ |
| tests/global.teardown.ts | Per spec, tapi inkonsisten dgn setup existing | |

**User's choice:** `tests/e2e/global.teardown.ts`

---

## Wave 0 investigation approach

### Question: Cara investigasi 5 open question Wave 0

| Option | Description | Selected |
|--------|-------------|----------|
| Baca source code dulu | Grep + Read source. Cepat, deterministic, no UI. | ✓ |
| Playwright probe / manual recording | Jalankan app, record network tab. Real tapi lambat. | |
| Scripted spike test | Mini Playwright spec hit endpoint. Overkill. | |
| Combo: source + 1 manual probe | Baca source dulu, lalu cek manual MA save flow. | |

**User's choice:** Baca source code dulu (Recommended).
**Notes:** Target source: CMPController.cs, AssessmentAdminController.cs, Views/CMP/StartExam.cshtml, wwwroot/js/, Models/AssessmentSession.cs, Data/SeedData.cs. Output: investigation report singkat di phase dir sebelum Wave 1 helpers dimulai.

---

## Notes field fallback marker

### Question: Kalau AssessmentSession.Notes tidak ada

| Option | Description | Selected |
|--------|-------------|----------|
| Title prefix [MATRIX_TEST] | AssessmentSession.Title pasti ada. No schema change. | ✓ |
| ID range only | Skip marker, identifikasi via ID 9001-9009. Gampang nabrak. | |
| Add migration: kolom Notes baru | nvarchar(200) Notes. Schema change untuk test infra. | |
| Decide setelah Wave 0 | Tunda. Plan punya 2 path. | |

**User's choice:** Title prefix `[MATRIX_TEST_2026_05_11]`.
**Notes:** Primary marker tetap `Notes` field kalau ada. Fallback ke Title prefix kalau Wave 0 nemu Notes tidak ada. Migration ditolak — test infra tidak boleh ubah schema produksi.

---

## Sentinel + smoke gating

### Question 1: Exit code sentinel Layer 3

| Option | Description | Selected |
|--------|-------------|----------|
| Sentinel pakai test.fail() | Expected to fail annotation. Run exit 0 saat sentinel benar gagal. | ✓ |
| Skenario sentinel selalu fail, run exit non-zero | Default Playwright. CI integration nyusahin. | |
| Custom reporter swallow sentinel failure | Filter di matrixReport. Lebih ribet. | |

**User's choice:** `test.fail()` annotation (Recommended).

### Question 2: Smoke run gating

| Option | Description | Selected |
|--------|-------------|----------|
| Documented di README + CLAUDE memory | Developer disiplin sendiri. Simpel. | ✓ |
| npm script enforced | Marker file `.smoke-passed`. Otomatis tapi nambah moving part. | |
| Skip gating | Saran opsional saja. | |

**User's choice:** Documented di README + CLAUDE memory (Recommended).

---

## Claude's Discretion

Area di mana user delegate ke Claude untuk decide saat impl:
- Snapshot file path location (`C:/temp/` vs `tests/sql/`)
- Sabotage strategy peserta2 (deterministic salah option pertama)
- Report file naming (date saja, bukan timestamp)
- Console error whitelist
- Severity threshold exact per finding type

## Deferred Ideas

- QA-02 CI integration (future requirement)
- QA-03 Regression subset conversion (future)
- QA-04 Visual regression Percy/Chromatic (out of scope v16.0)
- QA-05 Multi-environment staging/prod (out of scope v16.0)
- QA-06 Concurrency stress test (out of scope)
- QA-07 Coverage expansion ke flow lain (foundation dulu)
- Schema migration tambah Notes column (ditolak untuk test infra)
- Smoke gating via marker file enforcement (too many moving parts)
- mssql node driver (cukup sqlcmd via spawn)
