# Phase 364: Restore Baseline Regresi e2e Exam - Pattern Map

**Mapped:** 2026-06-11
**Files analyzed:** 2 modified (test-only — zero kode produksi)
**Analogs found:** 2 / 2 (kedua file punya analog hidup + helper-reuse penuh)

> **Sifat fase:** Test-only restoration. Tidak ada file BARU dibuat — hanya 2 spec lama (`tests/e2e/exam-taking.spec.ts`, `tests/e2e/exam-types.spec.ts`) dimodifikasi agar lolos validator REST-06 + fix drift selector. Semua infrastruktur (helper, lifecycle, DB query) sudah ada. "Pattern map" di sini = **pola edit per-call yang harus disalin dari spec analog yang sudah comply**, bukan pembuatan file baru.

---

## File Classification

| Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---------------|------|-----------|----------------|---------------|
| `tests/e2e/exam-taking.spec.ts` | test (e2e, direct form-fill create) | request-response (UI form POST + DB assert) | `tests/e2e/image-in-assessment.spec.ts` (title pattern) + dalam-file FLOW A (own pattern) | exact (pola judul) + self (struktur) |
| `tests/e2e/exam-types.spec.ts` | test (e2e, wizard-helper create) | request-response (wizard POST + DB assert) | `tests/e2e/image-in-assessment.spec.ts` (wizard + title) + dalam-file FLOW K/P (DB query) | exact |

**Catatan klasifikasi:**
- Kedua file = **test, request-response** (create assessment via POST → assert hasil/DB). Bukan CRUD murni — fokus ke create + verifikasi.
- `exam-taking.spec.ts` create via **direct `#Title` fill** di `/Admin/CreateAssessment` (single-page form). Analog title-pattern ada di `image-in-assessment.spec.ts`, tapi pola form-fill-nya self-contained di FLOW A spec itu sendiri.
- `exam-types.spec.ts` create via **`createAssessmentViaWizard` helper** — identik dengan cara `image-in-assessment.spec.ts` create. Analog hampir 1:1.

---

## Pattern Assignments

### `tests/e2e/exam-taking.spec.ts` (test, request-response — direct form fill)

**Analog utama (pola judul comply):** `tests/e2e/image-in-assessment.spec.ts:74-76`
**Analog struktur (form fill + sukses-check):** dalam-file FLOW A `exam-taking.spec.ts:43-57`

#### Pola judul yang HARUS disalin (analog `image-in-assessment.spec.ts:74-76`)

Spec hidup yang sudah comply v20 validator menambahkan komentar acuan + prefix `Pre Test`:

```typescript
// Title WAJIB pola naming-convention Phase 336/339 (AssessmentAdminController.cs:866-874):
// standard assessment non-PrePostTest harus match ^(Pre|Post)\s*Test\s+.+$ .
assessmentTitle = `Pre Test OJT IMG355 ${Date.now()}`;
```

**Terapkan ke 10 call site `uniqueTitle(...)`** (semua kena validator REST-06 — pola edit per-call D-03):

| Line | Sebelum | Sesudah |
|------|---------|---------|
| :35 | `uniqueTitle('Legacy Exam')` | `uniqueTitle('Pre Test Legacy Exam')` |
| :316 | `uniqueTitle('Token Exam')` | `uniqueTitle('Pre Test Token Exam')` |
| :402 | `uniqueTitle('ForceClose Exam')` | `uniqueTitle('Pre Test ForceClose Exam')` |
| :548 | `uniqueTitle('Package Exam')` | `uniqueTitle('Pre Test Package Exam')` |
| :702 | `uniqueTitle('Proton T3 Interview')` | `uniqueTitle('Pre Test Proton T3 Interview')` |
| :833 | `uniqueTitle('Multi Worker')` | `uniqueTitle('Pre Test Multi Worker')` |
| :963 | `uniqueTitle('Timer Expired')` | `uniqueTitle('Pre Test Timer Expired')` |
| :1057 | `uniqueTitle('RealTime Mon')` | `uniqueTitle('Pre Test RealTime Mon')` |
| :1291 | `uniqueTitle('EditTest')` | `uniqueTitle('Pre Test EditTest')` |
| :1404 | `uniqueTitle('Abandon Test')` | `uniqueTitle('Pre Test Abandon Test')` |

> **D-02/D-04:** Nama lama dipertahankan sebagai remainder (grep-able). Regex `^(Pre|Post)\s*Test\s+.+$` lolos: `Pre Test Legacy Exam 1718064000000`. Cukup lolos regex, BUKAN konvensi penuh `{Stage} Test {Track} {Lokasi}`.

#### Imports pattern (analog header `exam-taking.spec.ts:1-4` — JANGAN tambah/ubah kecuali butuh `db`)

```typescript
import { test, expect, Page } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today, autoConfirm } from '../helpers/utils';
import { clickResumeForFixture, assertTier1Reject, assertTier2Reject, assertSubmitSuccess } from './helpers/exam313';
```

> **D-11 catatan:** Asersi DB `LinkedGroupId IS NULL` **direkomendasikan ditaruh di exam-types FLOW K** (sudah `import * as db`). Kalau ditaruh di exam-taking, perlu tambah `import * as db from '../helpers/dbSnapshot';` (zero produksi, OK). Research merekomendasikan exam-types K — lebih murah.

#### Core create pattern (analog `exam-taking.spec.ts:43-57` — self, JANGAN ubah strukturnya, hanya argumen judul)

Direct form-fill + dual-fallback sukses-check. Validator reject memanifestasi sebagai `expect(success || alert).toBeTruthy()` FAIL:

```typescript
await page.fill('#Title', title);
await page.selectOption('#Category', 'OJT');
await page.fill('#ScheduleDate', today());
await page.fill('#ScheduleTime', '00:01');
await page.fill('#DurationMinutes', '30');
await page.fill('#PassPercentage', '60');
await page.locator('#AllowAnswerReview').check();
await page.locator('#GenerateCertificate').check();

await page.click('#submitBtn');
await page.waitForTimeout(3_000);
const success = await page.locator('#successModal').evaluate(el => el.classList.contains('show')).catch(() => false);
const alert = await page.locator('.alert-success').isVisible().catch(() => false);
expect(success || alert).toBeTruthy();   // ← FAIL saat validator reject (judul belum comply)
```

> **Setelah judul comply**, kegagalan create hilang. Sisa kegagalan = selector/feature drift (lihat §Shared Patterns → Selector Drift). Drift HIGH-risk: FLOW E (Proton T3, :702-805) — kandidat `test.fixme` (D-06/D-09).

---

### `tests/e2e/exam-types.spec.ts` (test, request-response — wizard helper)

**Analog utama:** `tests/e2e/image-in-assessment.spec.ts:79-91` (`createAssessmentViaWizard` call)
**Analog DB-assert (D-11):** dalam-file FLOW K `exam-types.spec.ts:287-296` + FLOW P `:903-909`

#### Pola judul yang HARUS disalin (analog wizard `image-in-assessment.spec.ts:79-91`)

```typescript
await createAssessmentViaWizard(page, {
  title: assessmentTitle,        // = `Pre Test OJT IMG355 ${Date.now()}` (comply)
  category: 'OJT',
  scheduleDate: today(),
  ...
});
```

**Terapkan ke 11 call site standard** (edit per-call D-03). **JANGAN sentuh FLOW P :860 (exempt) dan FLOW T :1457 (controller beda):**

| Line | Sebelum | Sesudah | Catatan |
|------|---------|---------|---------|
| :37 | `uniqueTitle('[317-SMOKE-W0] Order Verify')` | `uniqueTitle('Pre Test [317-SMOKE-W0] Order Verify')` | kena validator |
| :191 | `uniqueTitle('[317-K] MA Exam')` | `uniqueTitle('Pre Test [317-K] MA Exam')` | **+ asersi D-11 di sini** |
| :310 | `uniqueTitle('[317-L] Essay Exam')` | `uniqueTitle('Pre Test [317-L] Essay Exam')` | kena validator |
| :435 | `uniqueTitle('[317-M] Mixed Exam')` | `uniqueTitle('Pre Test [317-M] Mixed Exam')` | kena validator |
| :588 | `uniqueTitle('[317-N] NoReview Exam')` | `uniqueTitle('Pre Test [317-N] NoReview Exam')` | kena validator |
| :690 | `uniqueTitle('[317-O] ExtraTime Exam')` | `uniqueTitle('Pre Test [317-O] ExtraTime Exam')` | kena validator |
| **:860** | `uniqueTitle('[318-P] PrePost Exam')` | **TIDAK DIUBAH** | **EXEMPT (PrePostTest) — Pitfall 2** |
| :1065 | `uniqueTitle('[318-Q] EWCD Past Exam')` | `uniqueTitle('Pre Test [318-Q] EWCD Past Exam')` | kena validator |
| :1151 | `uniqueTitle('[318-R] Cert Exam')` | `uniqueTitle('Pre Test [318-R] Cert Exam')` | kena validator |
| :1278 | `uniqueTitle('[318-S-TRUE] Review Exam')` | `uniqueTitle('Pre Test [318-S-TRUE] Review Exam')` | kena validator |
| :1364 | `uniqueTitle('[318-S-FALSE] NoReview Exam')` | `uniqueTitle('Pre Test [318-S-FALSE] NoReview Exam')` | kena validator |
| **:1457** | `uniqueTitle('[319-T] Manual CRUD')` | **TIDAK DIUBAH** (rekomendasi) | jalur `AddManualAssessment`, no REST-06 — Pitfall 2 |

> **Total edit exam-types: 11 judul** (W0/K/L/M/N/O/Q/R/S-TRUE/S-FALSE). P exempt, T jalur beda. Roadmap sebut 6 — itu under-count (research §Flow Inventory).

#### Imports pattern (analog header `exam-types.spec.ts:1-22` — sudah lengkap, JANGAN tambah)

```typescript
import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today, yesterday } from '../helpers/utils';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm, ... } from './helpers/examTypes';
import * as db from '../helpers/dbSnapshot';   // ← sudah ada — siap untuk asersi D-11
```

#### DB Assertion pattern (D-11 / SC#3) — analog `exam-types.spec.ts:287-296` (FLOW K K5) + `:903-909` (FLOW P P1)

Pola query DB sudah hidup di file ini (`db.queryScalar` inline). **Tambah 1 asersi** `LinkedGroupId IS NULL` di FLOW K (K1 setelah `assessmentId` ter-extract :206, ATAU K5). Reuse persis pola FLOW K K5:

```typescript
// Analog persis — FLOW K K5 (:287-290):
const score = await db.queryScalar(
  `SELECT ISNULL(Score, -1) FROM AssessmentSessions WHERE Id = ${sessionId}`
);
expect(score).toBe(100);
```

**Asersi BARU D-11 (salin pola, ganti kolom):**

```typescript
// SC#3 — auto-pair Phase 338 TIDAK salah-pasang LinkedGroupId (timestamp uniqueTitle jaga isolasi).
const linkedNull = await db.queryScalar(
  `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${assessmentId} AND LinkedGroupId IS NULL`
);
expect(linkedNull).toBe(1);
```

> **Keamanan SQL (research §Security):** `assessmentId` = integer literal (parameter-free int, BUKAN string user). Aman dari injection. JANGAN interpolasi string mentah ke SQL — hanya int. Pola FLOW P P1 (:886-895) yang interpolasi `${title}` string adalah pengecualian existing (judul test-controlled), hindari menambah pola string baru.

#### Wizard reject manifestation (analog `examTypes.ts:103`)

Validator reject di wizard memanifestasi sebagai timeout 15s di helper (BUKAN error judul eksplisit):

```typescript
// examTypes.ts:103 — saat validator reject, #successModal tak pernah .show → throw timeout
await page.locator(`${wizardSelectors.successModal}.show`).waitFor({ state: 'visible', timeout: 15_000 });
```

> Setelah judul comply → tidak timeout. Sisa kegagalan = drift.

---

## Shared Patterns

### Title Compliance (REST-06) — pola inti seluruh fase
**Source acuan hidup:** `tests/e2e/image-in-assessment.spec.ts:74-76` (commit `d4edae7c`, Phase 355)
**Validator (read-only):** `Controllers/AssessmentAdminController.cs:876` regex `^(Pre|Post)\s*Test\s+.+$` (case-SENSITIVE)
**Apply to:** 21 call site standard-create (10 exam-taking + 11 exam-types)

```typescript
// Pola: uniqueTitle('Pre Test ' + <marker lama>) — prefix seragam "Pre Test" (huruf P+T kapital persis)
uniqueTitle('Pre Test [317-K] MA Exam')   // → "Pre Test [317-K] MA Exam 1718064000000"
```

> **Case-sensitive:** "pre test"/"PRE TEST" DITOLAK. Wajib "Pre Test" (P+T kapital). Spasi antara Pre/Test wajib (D-01/D-02 lock, walau `\s*` izinkan "PreTest").

### Unique Title Helper (isolasi + guard auto-pair SC#3)
**Source:** `tests/helpers/utils.ts:23` — JANGAN diubah (D-03; dipakai spec lain)
**Apply to:** semua call site

```typescript
export function uniqueTitle(prefix = 'E2E Test'): string {
  return `${prefix} ${Date.now()}`;   // timestamp epoch-ms → remainder unik → counterpart "Post Test ..." selalu absen → LinkedGroupId NULL
}
```

> Edit hanya **argumen prefix per-call**. Timestamp `Date.now()` adalah guard struktural SC#3 (auto-pair `TryAutoDetectCounterpartGroup` butuh remainder PERSIS sama → tak pernah match).

### Login (worker-side flow)
**Source:** `tests/helpers/auth.ts:4-11`
**Apply to:** semua flow yang punya langkah peserta (coachee/coachee2)

```typescript
export async function login(page: Page, account: AccountKey) {
  const { email, password } = accounts[account];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL('**/Home/**', { timeout: 15_000 });   // ← timeout di sini = AD belum off (Pitfall 3)
}
```

> **D-13/Pitfall 3:** App WAJIB jalan `Authentication__UseActiveDirectory=false dotnet run` supaya login peserta sukses. Env override — JANGAN edit `appsettings.json` (HEAD = AD on, siap handoff).

### Snapshot/Restore Lifecycle (otomatis — D-13/D-14 ter-enforce)
**Source:** `tests/playwright.config.ts:5,22-32` + `tests/e2e/global.setup.ts` + `tests/e2e/global.teardown.ts`
**Apply to:** seluruh run (otomatis via project dependency)

```typescript
// playwright.config.ts — setup project = dependency chromium; globalTeardown selalu jalan
globalTeardown: require.resolve('./e2e/global.teardown.ts'),  // RESTORE + Layer 4
projects: [
  { name: 'setup', testMatch: /global\.setup\.ts/ },          // BACKUP DB + seed matrix + collision pre-check 9001-9018
  { name: 'chromium', use: { browserName: 'chromium' }, dependencies: ['setup'] },
],
retries: 0,   // D-08 "default" = 0 retry; 1x run hijau = PASS penuh
```

> **Konsekuensi:** setiap `npx playwright test <file>` otomatis BACKUP sebelum + RESTORE sesudah. Data test `Pre Test ...` ikut ter-revert. D-13/D-14 terpenuhi otomatis — snapshot manual tidak perlu, TAPI catat journal `docs/SEED_JOURNAL.md` sesuai D-13. Verifier: residue check = Layer 4 teardown + cek tak ada `Pre Test%{ts}` nyangkut.

### Backup/Restore + DB query primitives (kalau butuh manual)
**Source:** `tests/helpers/dbSnapshot.ts` — `backup()` :67, `restore()` :80, `queryScalar()` :116, `queryString()` :139
**Apply to:** asersi D-11 (`queryScalar`); manual snapshot (analog `image-in-assessment.spec.ts:44-69` beforeAll/afterAll bila perlu)

> localhost-only guard (`runSqlcmd` reject non-localhost `-S`, :40). Koneksi hardcoded `localhost\SQLEXPRESS`/`HcPortalDB_Dev`. Tidak perlu config tambahan.

### Selector Drift (D-05/D-06) — fix di test-code, JANGAN fix produksi
**Apply to:** flow yang patah NON-judul setelah judul comply

| Flow | Lokasi | Risiko | Aksi |
|------|--------|--------|------|
| exam-taking E (Proton T3) | :702-805 | **HIGH** — Proton overhaul v25.0 (Phase 358-363) | Kalau perilaku produksi berubah → `test.fixme('drift Proton v25.0 + ref backlog 999.x')` (D-06/D-09) |
| exam-taking H4/H7 (Monitoring) | :1136-1262 | MEDIUM — markup/endpoint monitoring sering disentuh | fix selector di test-code (D-05) |
| exam-types K5/M (Results MA) | :278-296 | bug LAMA SURF-317-A (Results 500) | sudah workaround DB; **JANGAN fix** (Pitfall 5, D-06) |
| exam-types O (SignalR ExtraTime) | :747-754 | MEDIUM — hub state gate | fix selector (D-05) |
| asersi judul panjang (card/history/cert) | banyak | UI truncate | **partial-match boleh** (D-17) — substring marker + timestamp |

> **D-09 KRITIS:** run ber-`test.fixme` = BUKAN "PASS penuh". Gate jatuh ke jalur SC#4 alternatif "failure terdokumentasi bukan-karena-judul". Jujur ke verifier; jangan hitung skip sebagai hijau.

---

## No Analog Found

Tidak ada — fase ini 100% reuse infrastruktur existing. Tidak ada file baru, tidak ada helper baru, tidak ada pola tanpa analog. Satu-satunya "pola baru" = 1 baris asersi DB D-11 yang disalin persis dari FLOW K K5 (`exam-types.spec.ts:287-290`).

| File | Role | Data Flow | Alasan |
|------|------|-----------|--------|
| (none) | — | — | Semua pola punya analog hidup; zero file baru |

---

## Metadata

**Analog search scope:** `tests/e2e/`, `tests/helpers/`, `tests/e2e/helpers/`, `Controllers/AssessmentAdminController.cs` (read-only ref)
**Files scanned:** `image-in-assessment.spec.ts`, `exam-taking.spec.ts`, `exam-types.spec.ts`, `examTypes.ts`, `utils.ts`, `dbSnapshot.ts`, `auth.ts`, `playwright.config.ts`
**Key correction propagated:** 22 call site `uniqueTitle` (10 + 12), **21 perlu edit** (P exempt, T jalur beda) — BUKAN "9 flow" roadmap.
**Pattern extraction date:** 2026-06-11
