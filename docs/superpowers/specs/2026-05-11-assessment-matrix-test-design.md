# Assessment Matrix Test — Design Spec

**Date:** 2026-05-11
**Author:** Rino (with Claude assist)
**Status:** Draft (pending user approval)

## Goal

Automated end-to-end test yang menyapu seluruh kombinasi (tipe assessment × tipe soal) untuk PortalHC. Tujuan utama: **discovery bug**, bukan regression suite. Output: 1 markdown report yang merangkum semua temuan dengan severity + screenshot.

## Scope

### In-scope

- 7 skenario E2E (4 mixed per tipe assessment + 3 single-type Online per tipe soal)
- Full lifecycle per skenario: peserta kerjakan → submit → grading manual essay (jika perlu) → verifikasi score di result page
- 2 peserta (`coachee` + `coachee2`) per skenario
- DB seed temporary lokal + cleanup otomatis (BACKUP/RESTORE)
- Bug report markdown dengan klasifikasi severity

### Out-of-scope

- Regression test (hanya discovery sweep)
- Concurrency/load testing (1-2 peserta per session, sequential)
- UAT manual scenario (timer-enforcement Phase 313 sudah punya spec sendiri)
- Test di server Dev/Prod (lokal saja)
- Test wizard admin create-assessment UI (asumsikan working — di-cover spec terpisah jika perlu)

## Decisions (dari brainstorming)

| Dim | Pilihan | Alasan |
|---|---|---|
| Scope | Full matrix sweep | Coverage maksimal kombinasi |
| Setup | DB seed temporary + cleanup | Cepat <10s, deterministic, match SEED_WORKFLOW.md |
| Depth | Full lifecycle (submit + grading + verify) | Uji integrasi ujian-grading-result |
| Bug behavior | Continue-and-collect | Peta bug menyeluruh dalam 1 run |
| Matrix shape | 7 session (4 mixed + 3 single-type Online) | Realistis + isolation jelas |
| File | Baru: `tests/e2e/assessment-matrix.spec.ts` | Isolated, gampang run khusus |
| Peserta | 2 per session (coachee + coachee2) | Concurrency ringan + partial scoring |

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│ Playwright globalSetup                                           │
│  1. sqlcmd BACKUP DATABASE PortalHC_Local → snapshot file        │
│  2. sqlcmd EXEC tests/sql/assessment-matrix-seed.sql             │
│  3. write seed metadata → tests/.matrix-state.json               │
│  4. append docs/SEED_JOURNAL.md (klasifikasi temporary+local)    │
│  5. validasi Layer 1 (count rows match config)                   │
└────────────────────────────┬─────────────────────────────────────┘
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│ assessment-matrix.spec.ts (1 file, 10 test() blocks)             │
│  Each test = 1 scenario:                                         │
│    Login peserta1 (coachee) → exam → answer → submit             │
│    Login peserta2 (coachee2) → exam → answer (1 MA salah) → submit│
│    Login HC (hc) → grade essay (jika ada) → finalize             │
│    Login peserta1 → result page → assert score                   │
│  Soft-assert: failure → record() → lanjut, tidak halt            │
└────────────────────────────┬─────────────────────────────────────┘
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│ Playwright globalTeardown                                        │
│  1. matrixReport.flush() → docs/test-reports/YYYY-MM-DD-...md    │
│  2. sqlcmd RESTORE DATABASE FROM snapshot WITH REPLACE           │
│  3. validasi Layer 4 (post-restore row count = 0)                │
│  4. update SEED_JOURNAL.md → cleaned                             │
│  5. delete .matrix-state.json + snapshot file                    │
└──────────────────────────────────────────────────────────────────┘
```

### Komponen baru

| File | Tanggung jawab |
|---|---|
| `tests/sql/assessment-matrix-seed.sql` | INSERT 9 session (7 discovery + 2 sentinel) + package + question + option, tagged `MATRIX_TEST_2026_05_11` |
| `tests/helpers/dbSnapshot.ts` | Wrapper `sqlcmd` BACKUP/RESTORE/EXEC, throw on non-zero exit |
| `tests/helpers/matrixReport.ts` | Collector findings + render markdown |
| `tests/helpers/examMatrix.ts` | POM-style: `takeExam`, `gradeEssaysAsHc`, `verifyResultPage` |
| `tests/e2e/assessment-matrix.spec.ts` | Spec utama, loop 7 scenario configs |
| `tests/global.setup.ts` (extend) | Tambah BACKUP + seed setelah app-running check |
| `tests/global.teardown.ts` (baru) | Report flush + RESTORE + journal update |
| `playwright.config.ts` (edit) | Register `globalTeardown` |

### Komponen reused

- `tests/helpers/auth.ts` — `login(page, accountKey)` existing
- `tests/helpers/accounts.ts` — fixture user (admin/hc/coachee/coachee2)
- `docs/SEED_JOURNAL.md` — append entry baru, format existing
- `docs/SEED_WORKFLOW.md` — SOP BACKUP/RESTORE diikuti

## 7 Scenario configs

| # | Title | Type | Composition | Total Q |
|---|---|---|---|---|
| 1 | Matrix Manual Mixed | Manual | 2 MC + 2 MA + 1 Essay | 5 |
| 2 | Matrix Online Mixed | Online | 2 MC + 2 MA + 1 Essay | 5 |
| 3 | Matrix PreTest Mixed | PreTest | 2 MC + 2 MA + 1 Essay | 5 |
| 4 | Matrix PostTest Mixed | PostTest | 2 MC + 2 MA + 1 Essay | 5 |
| 5 | Matrix Online MC-only | Online | 3 MC | 3 |
| 6 | Matrix Online MA-only | Online | 3 MA | 3 |
| 7 | Matrix Online Essay-only | Online | 3 Essay | 3 |

Total: 31 questions, 7 sessions. Semua peserta enrolled = `coachee` + `coachee2`.

### Sentinel skenario tambahan (untuk meta-validation)

3 sentinel `test()` block tambahan, **bukan** discovery skenario — fungsi: validasi framework test sendiri. Tidak ikut hitung statistik bug discovery di report (di-mark `[META]`).

| # | Title | Data | Tujuan |
|---|---|---|---|
| M1 | `[META-AllCorrect]` Sentinel | Dedicated session #M1 (3 MC, Online) | Layer 2: helper jujur saat semua jawaban benar — assert score = max |
| M2 | `[META-AllWrong]` Sentinel | Dedicated session #M2 (3 MC, Online) | Layer 2: helper jujur saat semua jawaban salah — assert score = 0 |
| M3 | `[META-CollectorCheck]` Sentinel | No DB interaction | Layer 3: assert palsu `expect(true).toBe(false)` di severity minor — verifikasi collector mencatat tepat 1 finding |

M1/M2 pakai dedicated session terpisah supaya tidak konflik attempt dengan discovery skenario #5 (1 peserta tidak bisa attempt session sama 2x tanpa reset). Total seeded = **7 discovery + 2 sentinel = 9 sessions**. Total `test()` blocks = **7 discovery + 3 sentinel = 10**.

ID range update: `AssessmentSession 9001-9009` (was 9001-9007), `PackageQuestion 50001-50037` (was 50001-50031, +6 untuk M1+M2), `PackageOption 80001-80124` (was 80001-80100, +24).

⚠️ Implikasi: report perlu filter `[META]` skenario saat hitung summary statistik discovery.

## Endpoint contract (verified)

Verified terhadap `Controllers/CMPController.cs` & `Controllers/AssessmentAdminController.cs`:

| Action | URL | Notes |
|---|---|---|
| Login | `POST /Account/Login` | redirect `**/Home/**` |
| Start exam | `GET /CMP/StartExam/{id}` | id = AssessmentSessionId |
| Save answer (MC) | `POST /CMP/SaveAnswer(sessionId, questionId, optionId)` | 1 optionId per call |
| Save answer (MA) | **Unknown — investigation required** (lihat Open Questions) |
| Save answer (Essay) | **Unknown — investigation required** |
| Submit exam | `POST /CMP/SubmitExam(id, answers, isAutoSubmit, autoSubmitToken)` | redirect `/CMP/Results/{id}` |
| Result page | `GET /CMP/Results/{id}` | |
| Monitoring detail | `GET /Admin/AssessmentMonitoringDetail?title=&category=&scheduleDate=` | composite key, bukan sessionId |
| Grade essay | `POST /Admin/SubmitEssayScore(sessionId, questionId, score)` | per soal |
| Finalize grading | `POST /Admin/FinalizeEssayGrading(sessionId)` | per session |

## Data flow per scenario

```
PER SCENARIO (test() block):
  Read scenario config dari .matrix-state.json
  ┌─ Peserta 1 (coachee) flow ──────────────────────────────────┐
  │  login(page, 'coachee')                                      │
  │  GET  /CMP/StartExam/{cfg.sessionId}                         │
  │  ASSERT page render: question count, type-specific UI        │
  │  FOR each question:                                          │
  │    IF MC: select option correctOptionIds[0] → SaveAnswer    │
  │    IF MA: investigation → likely loop SaveAnswer per option  │
  │           atau endpoint terpisah (TBD pre-impl)              │
  │    IF Essay: fill textarea → SaveAnswer atau auto-save TBD   │
  │  POST /CMP/SubmitExam                                        │
  │  ASSERT redirect → /CMP/Results/{cfg.sessionId}              │
  │  ASSERT status: 'Completed' (pure MC/MA) OR                  │
  │                 'PendingGrading' (jika ada Essay)            │
  └──────────────────────────────────────────────────────────────┘
  ┌─ Peserta 2 (coachee2) flow ─ (parallel context, beda browser)┐
  │  Sama seperti peserta 1, TAPI:                               │
  │   - Skenario MA-bearing: sengaja salah 1 jawaban MA          │
  │     → uji partial scoring                                    │
  │   - Skenario MC-only: sengaja salah 1 MC                     │
  │   - Skenario Essay-only: jawab apa adanya                    │
  └──────────────────────────────────────────────────────────────┘
  ┌─ HC grading flow (jika cfg punya Essay) ─────────────────────┐
  │  login(page, 'hc')                                           │
  │  GET  /Admin/AssessmentMonitoringDetail?title=...            │
  │       &category=...&scheduleDate=...                         │
  │  FOR each peserta dengan status PendingGrading:              │
  │    FOR each Essay question:                                  │
  │      POST /Admin/SubmitEssayScore(sessionId, questionId,     │
  │           score=ScoreValue) → full score                     │
  │    POST /Admin/FinalizeEssayGrading(sessionId)               │
  │    ASSERT status flip → 'Completed'                          │
  └──────────────────────────────────────────────────────────────┘
  ┌─ Verification flow ──────────────────────────────────────────┐
  │  login(page, 'coachee')                                      │
  │  GET  /CMP/Results/{cfg.sessionId}                           │
  │  ASSERT total score = sum(ScoreValue) for correct answers    │
  │  ASSERT per-question breakdown rendering                     │
  └──────────────────────────────────────────────────────────────┘
```

## State files

### `tests/.matrix-state.json` (gitignored, generated)

```json
{
  "snapshotPath": "C:/temp/PortalHC_Local-matrix-2026-05-11T14-30-00.bak",
  "seededAt": "2026-05-11T14:30:05Z",
  "scenarios": [
    {
      "id": 1, "sessionId": 9001, "title": "Matrix Manual Mixed", "type": "Manual",
      "category": "Matrix Test Category", "scheduleDate": "2026-05-11",
      "questions": [
        { "id": 50001, "type": "MultipleChoice", "scoreValue": 10, "correctOptionIds": [80001] },
        { "id": 50002, "type": "MultipleChoice", "scoreValue": 10, "correctOptionIds": [80006] },
        { "id": 50003, "type": "MultipleAnswer", "scoreValue": 10, "correctOptionIds": [80009, 80011] },
        { "id": 50004, "type": "MultipleAnswer", "scoreValue": 10, "correctOptionIds": [80014, 80015] },
        { "id": 50005, "type": "Essay",          "scoreValue": 10, "correctOptionIds": [] }
      ]
    }
    // ... 6 more
  ]
}
```

### ID range reserved

| Entity | Range | Capacity |
|---|---|---|
| AssessmentSession | 9001-9009 | 9 (7 discovery + 2 sentinel) |
| AssessmentPackage | 9001-9009 | 9 (1 per session) |
| PackageQuestion | 50001-50037 | 37 (31 discovery + 6 sentinel) |
| PackageOption | 80001-80124 | up to 124 |

Out of normal range → mudah identifikasi + tidak collide dengan `Data/SeedData.cs` existing (dicek pre-impl).

### `docs/test-reports/2026-05-11-assessment-matrix.md` (output)

```markdown
# Assessment Matrix Test Report — 2026-05-11

**Run:** 2026-05-11 14:30:05 → 14:42:18 (12m 13s)
**Total scenarios:** 7
**Status:** 5 PASS, 2 FAIL (3 critical, 4 major, 2 minor findings)

## Summary table
| # | Scenario | Status | Critical | Major | Minor |
|---|---|---|---|---|---|
| 1 | Manual Mixed | PASS | 0 | 0 | 0 |
| 2 | Online Mixed | FAIL | 1 | 1 | 0 |
| ...

## Findings

### S2 — Online Mixed
**Step:** submit assessment (coachee)
**Severity:** critical
**Expected:** redirect /CMP/Results dalam 5s
**Actual:** TimeoutError: page navigation tidak terjadi setelah 10s
**Screenshot:** ![s2-submit](../../test-results/matrix/s2-submit.png)
**Hypothesis:** SignalR connection drop saat submit

## Recommended next steps
1. Investigate ... (S2 critical)
```

## File contracts

### `tests/helpers/dbSnapshot.ts`

```ts
export async function backup(snapshotPath: string): Promise<void>;
export async function restore(snapshotPath: string): Promise<void>;
export async function execScript(sqlPath: string): Promise<void>;
export async function queryScalar<T = unknown>(sql: string): Promise<T>;
// Internal: spawn sqlcmd dengan -S localhost -d PortalHC_Local -E (windows auth)
// Throw Error('sqlcmd exit N: <stderr>') on non-zero
```

### `tests/helpers/matrixReport.ts`

```ts
export type Severity = 'critical' | 'major' | 'minor';
export type Finding = {
  scenarioId: number;
  scenarioTitle: string;
  step: string;
  expected: string;
  actual: string;
  screenshotPath?: string;
  severity: Severity;
};
export function record(f: Finding): void;
export async function flush(outPath: string): Promise<void>;
export class SkipScenarioError extends Error {}
export async function softAssert<T>(
  ctx: { scenario: ScenarioConfig; step: string; severity: Severity; page: Page },
  fn: () => Promise<T>,
  expected: string
): Promise<T | null>;
```

### `tests/helpers/examMatrix.ts`

```ts
import type { ScenarioConfig } from './matrixTypes';
import type { AccountKey } from './accounts';

export async function takeExam(
  page: Page,
  cfg: ScenarioConfig,
  peserta: AccountKey,
  options?: { sabotageOneAnswer?: boolean }
): Promise<void>;

export async function gradeEssaysAsHc(
  page: Page,
  cfg: ScenarioConfig
): Promise<void>;

export async function verifyResultPage(
  page: Page,
  cfg: ScenarioConfig,
  peserta: AccountKey
): Promise<void>;
```

Setiap fungsi internal pakai `softAssert()` — tidak throw kecuali critical (yang jadi `SkipScenarioError`).

## Error handling

### Klasifikasi failure

| Tipe | Behavior | Contoh |
|---|---|---|
| Setup failure | Halt total, restore + abort | BACKUP gagal, sqlcmd not found, seed SQL syntax error, existing tagged rows |
| Critical assertion | Soft-fail + record + skip sisa step skenario ini | Login 500, halaman exam tidak ter-render, redirect loop |
| Major assertion | Soft-fail + record + lanjut step | Score salah, status flip salah, jumlah soal mismatch |
| Minor assertion | Soft-fail + record + lanjut | Label salah, badge warna salah, copy text typo |
| Teardown failure | Critical alert tapi report tetap ditulis | RESTORE gagal — log instruksi manual restore |

### Continue-on-fail boundary

- Within scenario: critical → skip sisa step, major/minor → lanjut
- Across scenario: tidak pernah halt
- Setup phase: zero tolerance — halt sebelum apa-apa
- Teardown: report ditulis dulu sebelum RESTORE

### Edge cases

- **Test crash mid-run:** RESTORE jalan via `process.on('SIGINT')` handler
- **Duplicate run:** Setup detect tagged rows existing → halt + suruh manual cleanup
- **Disk full saat BACKUP:** sqlcmd error caught → halt sebelum spec
- **Snapshot file corrupt:** RESTORE error caught → log path + suruh manual RESTORE

## Cleanup safety net

3 layer defense:
1. **Primary:** RESTORE di teardown (paling clean)
2. **Backup:** Manual SQL `DELETE FROM AssessmentSession WHERE Notes='MATRIX_TEST_2026_05_11'` (cascade ke package/question/option/participant)
3. **Detection:** Setup cek tagged rows existing → halt jika ditemukan

## Meta-validation (testing the test)

### Layer 1 — Setup correctness

- Setelah seed: query verifikasi count
  - `SELECT COUNT(*) FROM AssessmentSession WHERE Notes='MATRIX_TEST_2026_05_11'` = 9
  - `SELECT COUNT(*) FROM PackageQuestion WHERE AssessmentPackageId BETWEEN 9001 AND 9009` = 37
- Per skenario: assert distribusi `QuestionType` matches config
- Setup gagal validasi → halt sebelum spec

### Layer 2 — Helper correctness

- 1 sentinel skenario `[META-AllCorrect]`: jawab semua benar, assert score = max
- 1 sentinel skenario `[META-AllWrong]`: jawab semua salah, assert score = 0
- Sentinel marked `[META]` di report, tidak ikut count statistik bug discovery

### Layer 3 — Bug collector correctness

- 1 sentinel `[META-CollectorCheck]`: assert palsu (`expect(true).toBe(false)`) di step minor
- Setelah teardown, baca report: harus mengandung exact 1 finding minor dari sentinel ini
- Tidak ada → collector rusak, fail seluruh run

### Layer 4 — Cleanup correctness

- Setelah RESTORE: `SELECT COUNT(*) FROM AssessmentSession WHERE Notes='MATRIX_TEST_2026_05_11'` = 0
- Hash check: `CHECKSUM_AGG` baris `AssessmentSession` sebelum BACKUP vs setelah RESTORE harus match
- Tidak match → log critical alert + instruksi manual fix

### Smoke run protocol

Sebelum trust full report 7 skenario, jalankan dulu **1 skenario** terkecil:
```bash
npx playwright test assessment-matrix --grep "Scenario 5"
```

Verifikasi: setup sukses, 1 skenario E2E, report ditulis dengan sentinel finding, RESTORE bersih, Layer 4 lewat. Jika smoke OK → run full 7.

### Self-check criteria

Sebelum percaya report:
- [ ] Setup validasi semua passed (Layer 1)
- [ ] 2 sentinel correctness (semua benar / semua salah) hasil expected (Layer 2)
- [ ] 1 sentinel collector menghasilkan tepat 1 minor finding (Layer 3)
- [ ] Post-RESTORE row count = 0 + checksum match (Layer 4)
- [ ] Tidak ada error di console Playwright runtime
- [ ] Tidak ada error Bahasa Indonesia muncul di test (e.g. "session expired") yang bukan bagian dari skenario

## Open Questions (blocker investigation pre-impl)

1. **MA save flow:** `CMPController.SaveAnswer(sessionId, questionId, optionId)` cuma terima 1 optionId. Multiple Answer butuh: (a) loop call SaveAnswer per option terpilih, (b) endpoint terpisah `SaveMultipleAnswers`, atau (c) staging client-side baru kirim saat SubmitExam? Investigation: baca `Views/CMP/StartExam.cshtml` + JS exam handler.
2. **Essay save flow:** Apakah Essay disimpan via SaveAnswer juga (text bukan optionId), via endpoint terpisah, atau cuma submit-time? Investigation: same source.
3. **`Notes` field di AssessmentSession:** Apakah field tersedia? Inspect `Models/AssessmentSession.cs`. Jika tidak ada, pakai marker lain (e.g. Title prefix `[MATRIX_TEST]`).
4. **`SeedData.cs` ID range:** Confirm 9001-9007 / 50001-50031 / 80001-80100 tidak collide dengan seed permanent existing.
5. **HC monitoring detail params:** Composite key `title+category+scheduleDate` perlu URL-encode (Indonesian chars, spaces). Confirm encoding di test.

Investigation untuk #1-#5 dilakukan sebagai task pertama di phase plan.

## Constraints & dependencies

- **Hanya lokal.** SQL Server lokal dengan database `PortalHC_Local`. Tidak boleh dijalankan ke 10.55.3.3 atau prod.
- **Windows auth (`-E`)** untuk sqlcmd, sesuai pattern existing.
- **Snapshot path** di `C:/temp/` — pastikan dir exist atau buat di setup.
- **Disk space:** snapshot ~500MB-2GB. Cek free space di setup.
- **Playwright** (sudah installed di tests/).
- **`mssql` driver** TIDAK dipakai — cukup sqlcmd via spawn.
- **Append-only ke SEED_JOURNAL.md** — format match existing entries.
- **`tests/.gitignore`** tambah `.matrix-state.json` + `sql/*.bak` (jika snapshot ditaruh di tests/sql/).

## Success criteria

- [ ] 7 skenario jalan end-to-end di lokal tanpa human intervention
- [ ] Report markdown ter-generate dengan struktur sesuai contoh
- [ ] DB lokal kembali ke state pre-test setelah teardown (Layer 4 pass)
- [ ] Smoke run protocol lewat sebelum full run dipercaya
- [ ] 4-layer meta-validasi semua pass di clean run
- [ ] Finding (jika ada) actionable: severity + screenshot + URL/lokasi + hypothesis

## Non-goals

- Bukan replacement untuk regression suite (hanya discovery)
- Bukan CI integration — manual run by developer
- Bukan multi-environment (hanya lokal)
- Bukan visual regression (no Percy/Chromatic)
