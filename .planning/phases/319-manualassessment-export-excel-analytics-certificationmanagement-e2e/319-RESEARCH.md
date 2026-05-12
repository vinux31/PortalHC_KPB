# Phase 319: ManualAssessment + Export Excel + Analytics + CertificationManagement E2E — Research

**Researched:** 2026-05-12
**Domain:** Playwright E2E test coverage untuk 5 admin features di Portal HC (ManualAssessment CRUD, ManageCategories CRUD, Export Excel, Analytics Dashboard, CertificationManagement)
**Confidence:** HIGH (semua claim verified by direct source-file read — production code dibaca langsung, bukan training data)
**Language:** Bahasa Indonesia (per CLAUDE.md)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**D-319-01 — FLOW Structure:** 5 FLOWs T/U/V/W/X (one per feature group), append ke `tests/e2e/exam-types.spec.ts`. 20+ sub-tests target.
- FLOW T — ManualAssessment full CRUD (T1-T6, 6 sub-tests)
- FLOW U — ManageCategories CRUD (U1-U4, 4 sub-tests)
- FLOW V — Export Excel endpoint (V1-V3, 3 sub-tests)
- FLOW W — Analytics dashboard (W1-W4, 4 sub-tests)
- FLOW X — CertificationManagement (X1-X3, 3 sub-tests)
- Cumulative target post-319: **69 sub-tests** (49 + 20).

**D-319-02 — Wave Structure:** Wave 1 parallel (FLOW T + U), Wave 2 sequential (V → W → X). 4 plans:
- 319-01: helpers ext + FLOW T
- 319-02: FLOW U
- 319-03: FLOW V + FLOW W
- 319-04: FLOW X + docs finalize

**D-319-03 — Export Excel:** APIRequest pattern (`page.request.get`) — cookies inherited dari page context. Assertions: status 200, content-type matches Excel MIME, content-disposition `attachment.*\.xlsx`, bytes > 2048. Helper: `verifyExcelDownload(page, endpointPath)`.

**D-319-04 — Analytics Chart:** JSON intercept primary + DOM canvas smoke + DB cross-check. Helper: `interceptAnalyticsResponse(page, params)`.

**D-319-05 — CertificationManagement:** CMP variant only (`CMPController.cs:3666`). CDP variant DEFERRED Phase 320+.

**D-319-06 — ManageCategories:** 4 sub-tests CRUD + 1 negative duplicate. Categories tab di `/Admin/ManageCategories`.

**D-319-07 — ManualAssessment:** 6 sub-tests full CRUD + worker visibility.

**D-319-08 — Helpers:** Append ke existing `tests/e2e/helpers/examTypes.ts` (consistency, single-file rule).

**D-319-09 — REQUIREMENTS sync:** Add QA-09 ke REQUIREMENTS.md + sync ROADMAP Phase 319 mapping `QA-04 → QA-09`.

### Claude's Discretion

- Exact Plan numbering split (3 vs 4 plans) — planner decides.
- Per-FLOW selector pattern (data-testid vs class+text) — researcher reads existing patterns (semua data-testid TIDAK tersedia di production markup — pakai `#id` + `name=` + class+text pattern, sama dengan Phase 317-318).
- Test runtime budget per sub-test — planner sets (default 60s typical).
- Wave 0 smoke untuk YELLOW assumptions — planner adds W0.x kalau researcher flags (FLAGGED: 4 YELLOW assumptions di bawah).

### Deferred Ideas (OUT OF SCOPE)

- CDP CertificationManagement variant (`CDPController.cs:3539`) — Phase 320+
- Search-by-NomorSertifikat UAT scenarios
- Excel re-query independent verification
- Multi-page pagination edge cases
- ManualAssessment bulk import
- Analytics drill-down per-employee

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| QA-09 | Admin features E2E coverage — ManualAssessment CRUD, ManageCategories CRUD, Export Excel endpoint validation (content-type + size guard), Analytics dashboard JSON+DOM verification, CertificationManagement listing+filter. | Semua 5 features sudah diverifikasi exist di controllers + views. Mapping table § Standard Stack di bawah. QA-09 belum ada di REQUIREMENTS.md — Plan 04 task akan menambahkan (D-319-09). |

</phase_requirements>

## Summary

Phase 319 menambah **5 FLOWs (T/U/V/W/X = 20 sub-tests)** ke single-file consolidated `tests/e2e/exam-types.spec.ts`, plus 1-3 helpers baru di `tests/e2e/helpers/examTypes.ts`. Fondasi sudah matang: APIRequest pattern (Phase 318 Plan 04 R4 PROVEN dengan PDF 159KB), JSON endpoint intercept (Phase 318 P6 fallback proven), `db.queryString` + `db.queryScalar` cross-check, sequential mode per-describe. **Tidak ada infrastructure baru — semuanya additive.**

Tiga temuan kritis yang harus planner perhatikan **SEBELUM mulai coding:**

1. **Excel endpoint `/Admin/ExportAssessmentMatrix` (yang disebut di CONTEXT D-319-03) TIDAK ADA.** Grep across all controllers menunjukkan ZERO match. Kandidat real existing endpoints (semua verified): `/Admin/ExportCategoriesExcel` (paling sederhana, tanpa param), `/CMP/ExportSertifikatExcel` (support filter), `/CMP/ExportFailRateExcel` (analytics). Planner WAJIB pilih ulang di Plan 03 wave 0.

2. **CMP variant CertificationManagement (D-319-05) PUNYA controller action tapi TIDAK punya view file `Views/CMP/CertificationManagement.cshtml`.** Action `CMPController.cs:3666` returns `View(vm)` — Razor view resolver akan **gagal mencari view** (hanya `Views/CDP/CertificationManagement.cshtml` yang ada). Semua link UI di production point ke `/CDP/CertificationManagement`, BUKAN `/CMP/CertificationManagement`. Status `/CMP/CertificationManagement` URL = kemungkinan besar **500 InvalidOperationException** (`The view 'CertificationManagement' was not found`). Planner WAJIB user-confirm scope: (a) re-scope ke CDP variant, ATAU (b) tambah pre-task untuk add `Views/CMP/CertificationManagement.cshtml`. Default discretion: **re-scope FLOW X ke CDP variant** karena CDP adalah variant yang real users pakai.

3. **ManualAssessment list rows (FLOW T) BUKAN visible directly — nested di collapse table.** Row Edit/Delete buttons ada di `Views/Admin/Shared/_TrainingRecordsTab.cshtml:308-321` dalam `<tr class="collapse" id="@workerCollapseId">` — caller HARUS click chevron button (`#tab-training` tab → expand worker row chevron → expanded table baru muncul). Test pattern: navigate `?tab=training` → click `button[data-bs-target="#@workerCollapseId"]` → wait `.collapse.show` → assert nested table row visible.

**Primary recommendation:** Wave 0 smoke (3 sub-tests minimum) untuk verifikasi: (a) Excel endpoint final selection, (b) CertificationManagement scope clarification, (c) ManualAssessment expand-row interaction pattern. Setelah Wave 0 green, lanjut FLOW T-U-V-W-X as planned.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| ManualAssessment form CRUD | Frontend Server (Razor view + form POST) | DB persistence | TrainingAdminController POST → EF Core writes `AssessmentSessions` row dengan `IsManualEntry=true` (verified TrainingAdminController.cs:603-628). Tidak ada client-side hidden state. |
| ManageCategories CRUD | Frontend Server (Razor + redirect-on-POST) | DB + IMemoryCache invalidation | AssessmentAdminController.cs:378-498 — full POST-redirect-GET pattern, no AJAX. Cache invalidated via `_cache.Remove(CategoriesCacheKey)` Phase 311. |
| Excel export | API endpoint (binary file response) | DB read-only query | ClosedXML `XLWorkbook` → byte[] → `File(bytes, MIME, filename)` via `ExcelExportHelper.ToFileResult()`. Direct HTTP GET, cookie auth. |
| Analytics Dashboard view | Frontend Server (Razor shell + JS bootstrap) | API endpoints (JSON) + Chart.js v4 (canvas) | Razor renders shell + filter form. JS fetches `GetAnalyticsSummary`, `GetFailRateData`, `GetTrendData`, `GetEtBreakdownData`, `GetExpiringSoonData`, `GetGainScoreData` then mounts Chart.js v4 (`canvas#failRateChart`, `canvas#trendChart`, `canvas#gainScoreTrendChart`). |
| CertificationManagement listing | Frontend Server (Razor + AJAX partial refresh) | DB query (BuildSertifikatRowsAsync) | Initial render `Views/CDP/CertificationManagement.cshtml`, filter cascading via `fetch('/CDP/FilterCertificationManagement')` → partial HTML reload `cert-table-container`. |

---

## Standard Stack

### Core (semua sudah terinstall — NO new deps)

| Library | Version | Purpose | Source |
|---------|---------|---------|--------|
| `@playwright/test` | already in `tests/package.json` | E2E runner | `tests/playwright.config.ts:1` (verified) |
| `ClosedXML` (server-side) | already referenced di production | `.xlsx` generation | `Helpers/ExcelExportHelper.cs:1` `using ClosedXML.Excel;` (verified) |
| `Chart.js` (CDN) | v4 (`chart.umd.min.js`) | Analytics canvas | `Views/CMP/AnalyticsDashboard.cshtml:585` (verified) |
| `chartjs-plugin-datalabels` | v2 (CDN) | Chart labels | `Views/CMP/AnalyticsDashboard.cshtml:586` |
| `chartjs-plugin-annotation` | v3 (CDN) | Chart annotations | `Views/CMP/AnalyticsDashboard.cshtml:587` |

### Helper Files (semua existing — append-only)

| File | Role | Append Plan |
|------|------|-------------|
| `tests/e2e/exam-types.spec.ts` (1406 LOC) | All FLOWs spec | Append 5 new describe blocks di akhir (sebelum `void verifyResultPage`) |
| `tests/e2e/helpers/examTypes.ts` (622 LOC) | POM-flat helpers + types | Append `verifyExcelDownload`, `interceptAnalyticsResponse`, optional `createManualAssessmentViaForm` |
| `tests/e2e/helpers/wizardSelectors.ts` (121 LOC) | Selector constants | Likely tidak perlu touch — selectors FLOW T-X tidak overlap dengan wizard. Pakai inline locators atau add `manualAssessmentSelectors` const baru kalau perlu. |
| `tests/helpers/utils.ts` (39 LOC) | Date + title utils | No new utility needed |
| `tests/helpers/dbSnapshot.ts` (157 LOC) | sqlcmd wrapper | No append — `queryScalar` + `queryString` already cover Phase 319 needs |
| `tests/helpers/accounts.ts` | Test creds | No append — `hc` (meylisa.tjiang) + `coachee` (rino.prasetyo) sudah cukup |

### Alternatives Considered

| Instead of | Could Use | Tradeoff (Why Standard Choice Wins) |
|------------|-----------|--------|
| APIRequest binary | `page.waitForEvent('download')` | Download API fires only on `<a download>` or auto-download response. CDP/CMP Export Excel returns `Content-Disposition: attachment` triggering browser download — viable. NAMUN APIRequest lebih cepat (no UI click chain), tidak tergantung browser dialog, dan sudah PROVEN di Phase 318 Plan 04 R4. **Pakai APIRequest** (consistency). |
| JSON intercept | Pixel diff Chart canvas | Pixel diff brittle (font rendering, anti-aliasing per OS). JSON shape lebih stable + dapat assert exact data. **Pakai JSON intercept** (D-319-04 confirmed). |
| Direct DB INSERT seed untuk ManualAssessment | HC UI form submission | Tujuan QA-09 = test full CRUD via UI, bukan validate DB schema. **Pakai UI form** untuk T1-T5; T6 verify via DB query. |

---

## Architecture Patterns

### System Architecture Diagram

```
Worker browser (Playwright HC page context)
  │
  ├──[HC login]──> POST /Account/Login (form) → cookie .AspNetCore.Identity.*
  │
  ├──[FLOW T navigate]──> GET /TrainingAdmin/AddManualAssessment (Razor form)
  │                              │
  │                              └─ form#WorkerCerts + dynamic TomSelect JS rows
  │                                 → POST /TrainingAdmin/AddManualAssessment
  │                                 → TrainingAdminController.cs:566 → INSERT AssessmentSessions
  │                                 → redirect /AssessmentAdmin/ManageAssessment?tab=training
  │
  ├──[FLOW U navigate]──> GET /AssessmentAdmin/ManageCategories
  │                              │
  │                              └─ POST /AssessmentAdmin/AddCategory (form, NO ajax)
  │                                 → AssessmentAdminController.cs:378 → INSERT/UPDATE/DELETE
  │                                 → IMemoryCache invalidation
  │                                 → redirect /ManageCategories
  │
  ├──[FLOW V APIRequest]──> page.request.get('/AssessmentAdmin/ExportCategoriesExcel')
  │                              │
  │                              └─ ClosedXML XLWorkbook → byte[]
  │                                 → File(bytes, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'filename.xlsx')
  │                                 → response inherits page cookies (verified Phase 318 R4)
  │
  ├──[FLOW W navigate]──> GET /CMP/AnalyticsDashboard (Razor shell)
  │                              │
  │                              └─ JS bootstrap fetch [parallel]:
  │                                  - GET /CMP/GetAnalyticsSummary?bagian=&unit=... → Json{totalSessions,passRate,...}
  │                                  - GET /CMP/GetFailRateData → Json[{Section,Category,Total,Failed}]
  │                                  - GET /CMP/GetTrendData → Json[{Year,Month,Passed,Failed}]
  │                                  - GET /CMP/GetEtBreakdownData
  │                                  - GET /CMP/GetExpiringSoonData
  │                                  - GET /CMP/GetGainScoreData
  │                                 → Chart.js v4 mount: canvas#failRateChart, #trendChart, #gainScoreTrendChart
  │
  └──[FLOW X navigate]──> GET /CDP/CertificationManagement  ⚠️ (NOT /CMP — see Pitfall 2)
                                 │
                                 └─ Initial Razor render → partial Shared/_CertificationManagementTablePartial
                                    JS filter cascade: fetch /CDP/GetCascadeOptions?section= → unit dropdown
                                    JS filter apply: fetch /CDP/FilterCertificationManagement?... → reload partial HTML
```

### Recommended Test Structure (append-only)

```
tests/
├── e2e/
│   ├── exam-types.spec.ts          # 1406 LOC → +400 LOC = ~1800 LOC
│   │   ├── (existing) FLOW K-S    # 49 sub-tests
│   │   ├── FLOW T (NEW)            # 6 sub-tests — ManualAssessment CRUD
│   │   ├── FLOW U (NEW)            # 4 sub-tests — ManageCategories CRUD
│   │   ├── FLOW V (NEW)            # 3 sub-tests — Export Excel
│   │   ├── FLOW W (NEW)            # 4 sub-tests — Analytics
│   │   └── FLOW X (NEW)            # 3 sub-tests — CertificationManagement
│   └── helpers/
│       ├── examTypes.ts            # 622 LOC → +~80 LOC
│       │   ├── (existing) 10 exports
│       │   ├── verifyExcelDownload (NEW) — APIRequest pattern
│       │   ├── interceptAnalyticsResponse (NEW) — JSON shape parse
│       │   └── createManualAssessmentViaForm (NEW, optional) — HC submit wrapper
│       └── wizardSelectors.ts       # No change (Phase 319 selectors inline atau new file kalau >5)
└── helpers/
    ├── utils.ts                     # No change
    ├── dbSnapshot.ts                # No change — queryScalar + queryString cover all
    └── accounts.ts                  # No change
```

### Pattern 1: Sequential describe + per-FLOW shared state

**What:** Phase 317-318 pattern — `test.describe.configure({ mode: 'serial' })` global + per-describe `let` variables shared antar sub-tests.

**When to use:** Setiap FLOW (T/U/V/W/X) yang sub-test berikutnya tergantung state sub-test sebelumnya (e.g., T1 capture `assessmentId` → T2 edit pakai id → T6 verify deleted).

**Example (skeleton):**
```typescript
// Source: tests/e2e/exam-types.spec.ts:22 (existing pattern)
test.describe('FLOW T — ManualAssessment Full CRUD', () => {
  let manualTitle: string;
  let workerId: string;
  let manualSessionId: number;

  test('T1 — HC navigate AddManualAssessment form + verify fields', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    manualTitle = uniqueTitle('[319-T] Manual CRUD');
    await login(page, 'hc');
    await page.goto('/TrainingAdmin/AddManualAssessment');
    await expect(page.locator('form[action*="AddManualAssessment"]')).toBeVisible();
    await expect(page.locator('#Title')).toBeVisible();
    await expect(page.locator('#WorkerSelect')).toBeAttached();
  });

  test('T2 — HC submit form (1 worker, score 85, IsPassed=true) + DB verify INSERT', async ({ page }) => {
    // ... fill TomSelect → addWorkerRow JS → fill cert fields → submit
    // capture redirect URL → query DB latest AssessmentSession dengan Title=manualTitle
  });
  // ... T3, T4, T5, T6
});
```

### Pattern 2: APIRequest binary verification (proven Phase 318 R4)

**What:** Skip browser download UI, hit endpoint directly via `page.request.get()` yang inherit page context cookies.

**When to use:** Any binary file endpoint — PDF, Excel, image, ZIP. Pre-condition: caller logged in.

**Example (Excel adaptation):**
```typescript
// Source pattern: tests/e2e/helpers/examTypes.ts:600-621 (verifyCertificatePdfDownload — verified Phase 318 R4)
export async function verifyExcelDownload(
  page: Page,
  endpointPath: string,
  opts: { minBytes?: number; filenamePattern?: RegExp } = {}
): Promise<{ bytes: number; filename: string; contentType: string }> {
  const response = await page.request.get(endpointPath);
  expect(response.status(), `Excel download status (${endpointPath})`).toBe(200);

  const contentType = response.headers()['content-type'] ?? '';
  // Verified ExcelExportHelper.cs:36 → "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
  expect(contentType, 'Content-Type').toMatch(
    /application\/vnd\.(openxmlformats-officedocument\.spreadsheetml\.sheet|ms-excel)/i
  );

  const contentDisp = response.headers()['content-disposition'] ?? '';
  expect(contentDisp, 'Content-Disposition').toMatch(/attachment.*\.xlsx/i);
  const filenameMatch = contentDisp.match(/filename=(?:"([^"]+)"|([^;\s]+))/i);
  const filename = (filenameMatch?.[1] ?? filenameMatch?.[2] ?? '').trim();
  if (opts.filenamePattern) {
    expect(filename, 'Filename pattern').toMatch(opts.filenamePattern);
  }

  const body = await response.body();
  const minBytes = opts.minBytes ?? 2048;
  expect(body.length, `Excel body bytes (min ${minBytes})`).toBeGreaterThan(minBytes);

  return { bytes: body.length, filename, contentType };
}
```

### Pattern 3: JSON intercept untuk JS-bootstrapped data (proven Phase 318 P6 fallback)

**What:** `page.waitForResponse()` capture JSON response, parse, assert shape + values. Bypass DOM scrape of JS-rendered content.

**When to use:** Chart.js canvas opaque pixels, DataTables AJAX, dynamic dropdowns, any JS-injected content.

**Example (Analytics adaptation):**
```typescript
// Pattern source: Phase 318 Plan 03 P6 fallback strategy reference
export interface AnalyticsResponseShape {
  // Verified CMPController.cs:2812 GetAnalyticsSummary returns:
  totalSessions: number;
  passRate: number;
  expiringCount: number;
  avgGainScore: number;
}

export async function interceptAnalyticsResponse(
  page: Page,
  action: () => Promise<void>,
  endpointMatcher: string | RegExp = '/CMP/GetAnalyticsSummary'
): Promise<AnalyticsResponseShape> {
  const responsePromise = page.waitForResponse(
    (r) => typeof endpointMatcher === 'string'
      ? r.url().includes(endpointMatcher)
      : endpointMatcher.test(r.url()),
    { timeout: 15_000 }
  );
  await action();
  const response = await responsePromise;
  expect(response.status()).toBe(200);
  return (await response.json()) as AnalyticsResponseShape;
}
```

### Anti-Patterns to Avoid

- **DOM-scrape Chart.js canvas pixel content:** Canvas is opaque to Playwright; `getByText()` returns nothing. Selector `canvas#failRateChart` valid hanya untuk visibility check, BUKAN data assertion. → **Use JSON intercept** (Pattern 3).
- **Multiple-endpoint Excel testing:** D-319-03 explicit "1 representative endpoint untuk efficiency". Jangan paralel test 7 Excel endpoints — duplikatif coverage, lambat. → Pick 1: `/AssessmentAdmin/ExportCategoriesExcel` (simplest, no params).
- **Direct `Views/CMP/CertificationManagement.cshtml` link assumption:** View file TIDAK ADA. → See Pitfall 2 + planner re-scope.
- **Click ManualAssessment row without expanding collapse:** Row buttons di `tr.collapse` — invisible by default. → See Pitfall 3.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Binary download verification | Custom `fetch()` + manual cookie cookbook | `page.request.get()` (APIRequest API) | Cookies auto-inherited; Phase 318 R4 PROVEN. |
| Excel byte-level structure parse | Read `.xlsx` content (unzip + parse XML) | Just verify content-type + bytes>2048 + filename | Out of scope; QA-09 = endpoint validation, not file content audit. Phase 301 D-19 already proved server-side query independence. |
| Chart pixel diff | Percy/Chromatic snapshot | JSON intercept + DB cross-check | No Percy infrastructure (QA-04 deferred per REQUIREMENTS.md:21). |
| Test database fixture | Custom SQL inserts pre-test | `db.queryScalar` + `db.queryString` post-test verify; let UI do INSERT | Realistic E2E coverage = exercise full controller path. |
| Date format helpers | Custom `formatDate` | `today()` / `yesterday()` / `tomorrow()` di `tests/helpers/utils.ts` | All return `YYYY-MM-DD` already. |
| Unique title isolation | UUID library | `uniqueTitle('[319-T] Manual CRUD')` → suffix `Date.now()` | Battle-tested pattern Phase 317-318. |

**Key insight:** Phase 319 is a **consumption phase, not a foundation phase**. All test infra battle-tested. Append helpers + sub-tests, no new tooling, no new files. ESPECIALLY DO NOT create `manualAssessment.spec.ts` standalone file — D-319-08 explicit single-file rule.

---

## Runtime State Inventory

> Phase 319 = **additive code only** (helpers + sub-tests). No rename/refactor/migration. **SKIPPED** per template rule.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — verified scan: no DB rename, no string-replace, additive tests only | None |
| Live service config | None | None |
| OS-registered state | None | None |
| Secrets/env vars | None — `tests/helpers/accounts.ts` (Windows Integrated Auth via sqlcmd `-E`) sudah cukup | None |
| Build artifacts | None | None |

---

## Common Pitfalls

### Pitfall 1: Excel endpoint `/Admin/ExportAssessmentMatrix` TIDAK ADA

**What goes wrong:** Plan 03 menulis test `verifyExcelDownload(page, '/Admin/ExportAssessmentMatrix')` → response 404 → V1-V3 RED.

**Why it happens:** CONTEXT.md D-319-03 sebut nama hipotetis "ExportAssessmentMatrix" tanpa grep verification. Grep across `Controllers/*.cs` menunjukkan ZERO match untuk pattern `ExportAssessmentMatrix|AssessmentMatrix`.

**How to avoid:** Planner WAJIB pilih dari 7 existing Excel endpoints (semua verified):

| Endpoint | Controller:line | Params | Use case |
|----------|----------------|--------|----------|
| `/AssessmentAdmin/ExportCategoriesExcel` | AssessmentAdminController.cs:526 | none | Simplest, deterministic data (1 row per category) |
| `/CMP/ExportSertifikatExcel` | CMPController.cs:3788 | `?category=&subCategory=&search=` | Cert-related, larger dataset |
| `/CMP/ExportSertifikatDetailExcel` | CMPController.cs:3827 | `?judul=&bagian=&unit=&status=` | Sertifikat per worker detail |
| `/CMP/ExportFailRateExcel` | CMPController.cs:3053 | analytics filters | Analytics-related (overlap FLOW W) |
| `/CMP/ExportTrendExcel` | CMPController.cs:3101 | analytics filters | Analytics-related |
| `/CMP/ExportEtBreakdownExcel` | CMPController.cs:3148 | analytics filters | Analytics-related |
| `/CMP/ExportItemAnalysisExcel` | CMPController.cs:3455 | `?assessmentGroupId=N` | Per-assessment analysis |
| `/CMP/ExportGainScoreExcel` | CMPController.cs:3553 | `?assessmentGroupId=N` | Per-assessment gain |

**Recommendation:** `/AssessmentAdmin/ExportCategoriesExcel` — paling sederhana, no params, deterministic content (1 row per category in DB). Plan 03 Wave 0 V0: HC GET → 200 + xlsx MIME + bytes>2048.

**Warning signs:** V1 returns 404 → endpoint wrong. V1 returns 200 but bytes<100 → empty workbook (DB has no rows; consider seeded fixture or wait for U2-U3 categories created).

### Pitfall 2: CMP CertificationManagement view file MISSING

**What goes wrong:** FLOW X1 `await page.goto('/CMP/CertificationManagement')` → server returns 500 `InvalidOperationException: The view 'CertificationManagement' was not found. The following locations were searched: /Views/CMP/CertificationManagement.cshtml, /Views/Shared/CertificationManagement.cshtml`.

**Why it happens:**
- `CMPController.cs:3666` action exists dan return `View(vm)` (verified)
- `Views/CMP/CertificationManagement.cshtml` **TIDAK ADA** (verified `Bash ls`)
- `Views/Shared/CertificationManagement.cshtml` **TIDAK ADA** (verified)
- `Views/CDP/CertificationManagement.cshtml` ADA tapi Razor view resolver tidak fallback ke folder controller lain
- Comment di controller `// CertificationManagement — dipindah dari CDPController` mengindikasikan **action di-copy** dari CDP tapi view file ditinggal di folder CDP — kemungkinan bug yang belum ketahuan karena tidak ada link UI ke `/CMP/CertificationManagement`
- Semua UI navigation menu (verified: `Views/CMP/Index.cshtml:98`) point ke `/CDP/CertificationManagement`

**How to avoid:** TWO OPTIONS — planner WAJIB user-confirm:

**Option A (RECOMMENDED): Re-scope FLOW X ke CDP variant**
- Update CONTEXT.md D-319-05 dari "CMP variant only" → "CDP variant only"
- Test target: `/CDP/CertificationManagement` + filter `/CDP/FilterCertificationManagement` + detail `/CDP/CertificationManagementDetail?judul=...`
- Pros: actual user-facing page, no production code change needed, view markup verified at `Views/CDP/CertificationManagement.cshtml:1-589`

**Option B: Add pre-task untuk create CMP view file**
- Plan 04 add task "Create `Views/CMP/CertificationManagement.cshtml` (copy from CDP, swap `/CDP/` URLs ke `/CMP/`)" — but this is production code change yang harus melalui DEV_WORKFLOW (Team IT promosi)
- Pros: honor literal D-319-05; Cons: out-of-scope production change, kompleks

**Recommendation:** Option A. Planner ajukan ke user lewat brief diskusi sebelum Plan 04 (atau via plan-checker). Default kalau user tidak respond: Option A.

**Warning signs:** X1 navigate → 500 atau text "view was not found" → CMP variant truly broken; switch ke CDP.

### Pitfall 3: ManualAssessment row hidden di collapse-row

**What goes wrong:** FLOW T4 (HC edit existing ManualAssessment) → `page.click('a[href*="EditManualAssessment"]')` → strict-mode error atau timeout (button not visible).

**Why it happens:** Production markup `Views/Admin/Shared/_TrainingRecordsTab.cshtml:211-331` nests training/assessment rows dalam `<tr class="collapse" id="@workerCollapseId">` per-worker. Outer row chevron button (`data-bs-toggle="collapse"`) controls visibility. Default state = collapsed = nested table HIDDEN.

**How to avoid:** Test sequence:
```typescript
await page.goto('/AssessmentAdmin/ManageAssessment?tab=training');
// Tab activation — direct ?tab=training URL bypasses click
// Find worker row by name (rino.prasetyo full name di DB sebagai 'Rino Prasetyo' atau similar)
const workerRow = page.locator('tr', { hasText: 'Rino' }).first();
const chevronBtn = workerRow.locator('button[data-bs-target^="#"]');
await chevronBtn.click();
// Wait collapse animation done (Bootstrap default 350ms; pakai .collapse.show selector)
const expandedRow = page.locator('tr.collapse.show').filter({ hasText: 'Assessment Manual' });
await expandedRow.waitFor({ state: 'visible', timeout: 5_000 });
// NOW locator nested table cells visible
const editLink = expandedRow.locator(`a[href*="EditManualAssessment/${manualSessionId}"]`);
await editLink.click();
```

**Alternative (preferred):** Direct nav ke edit form via captured ID:
```typescript
// T2 captured manualSessionId via DB query post-INSERT
await page.goto(`/TrainingAdmin/EditManualAssessment/${manualSessionId}`);
// Bypass ManageAssessment UI entirely — faster + deterministic + skip collapse
```

**Warning signs:** Locator strict mode violation "expected 1 found 2+" → button visible di luar collapse (TomSelect dropdown), pakai `.filter()` lebih spesifik.

### Pitfall 4: ManualAssessment form requires TomSelect JS interaction (dynamic worker rows)

**What goes wrong:** T2 form submit blocked — submit button stays disabled.

**Why it happens:** `Views/Admin/AddManualAssessment.cshtml:283-388` JS logic:
- `#WorkerSelect` is **TomSelect-wrapped** `<select multiple>` (lines 60-66 + 292)
- TomSelect `onItemAdd` callback dynamically inserts `.worker-cert-card` div dengan `<input name="WorkerCerts[0].UserId" value="..." />`
- `#btnSimpanAssessment` starts `disabled = true` (line 311); enabled hanya ketika `workerCertsContainer.querySelectorAll('.worker-cert-card').length > 0` (lines 386-387)

**How to avoid:** Cannot use `page.selectOption('#WorkerSelect', ...)` directly — TomSelect intercepts dan render hidden. Must interact via TomSelect DOM:

```typescript
// TomSelect renders `.ts-control` next to original select
// Click control → dropdown opens → type search → click option
await page.click('.ts-control');                       // open dropdown
await page.fill('.ts-control input', 'rino');          // search
await page.click('.ts-dropdown .option:has-text("Rino Prasetyo")');  // pick
// After click, JS addWorkerRow fires → .worker-cert-card appears
await expect(page.locator('.worker-cert-card')).toHaveCount(1);
await expect(page.locator('#btnSimpanAssessment')).toBeEnabled();
```

**Alternative (cleaner):** Inject hidden form fields directly via `page.evaluate()` and submit form bypassing TomSelect UI:
```typescript
await page.evaluate((userId) => {
  const container = document.getElementById('workerCertsContainer')!;
  container.innerHTML = `
    <div class="worker-cert-card" data-user-id="${userId}">
      <input type="hidden" name="WorkerCerts[0].UserId" value="${userId}" />
      <input type="text" name="WorkerCerts[0].NomorSertifikat" value="CERT-319-T-001" />
    </div>
  `;
  (document.getElementById('btnSimpanAssessment') as HTMLButtonElement).disabled = false;
}, workerId);
```
But this bypasses real UI rendering — only use kalau Pattern 1 TomSelect interaction proves flaky di Wave 0.

**Recommendation:** Try Pattern 1 (real TomSelect interaction) first. Wave 0 T0 verifikasi. Fallback ke Pattern 2 evaluate kalau Pattern 1 flaky.

**Warning signs:** `Submit failed: button still disabled` → no `.worker-cert-card` rendered.

### Pitfall 5: ManageCategories duplicate-reject via TempData, BUKAN ModelState

**What goes wrong:** FLOW U4 `await expect(page.locator('span[asp-validation-for]')).toBeVisible()` — fails (no inline validation message).

**Why it happens:** Verified `AssessmentAdminController.cs:386-390`:
```csharp
if (await _context.AssessmentCategories.AnyAsync(c => c.Name == name))
{
    TempData["Error"] = "Nama kategori sudah digunakan. Gunakan nama yang berbeda.";
    return RedirectToAction("ManageCategories");
}
```
No DB unique constraint — pure server-side `.AnyAsync()` check returning TempData error message. View renders `.alert.alert-danger` (verified `ManageCategories.cshtml:52-58`).

**How to avoid:**
```typescript
// U4 expected pattern
await page.fill('input[name="name"]', existingCategoryName);
await page.click('button[type="submit"]:has-text("Tambah")');
await expect(
  page.locator('.alert-danger', { hasText: /sudah digunakan/i })
).toBeVisible({ timeout: 5_000 });
```

**Warning signs:** Toast/alert appears tapi assertion misses → check actual text (could be "tidak boleh kosong" if `name` empty by mistake).

### Pitfall 6: Analytics empty data set returns 200 with empty arrays — not 404

**What goes wrong:** W2 (intercept `GetFailRateData`) parsed JSON has `[]` empty array → assertion `datasets.length > 0` fails di fresh DB.

**Why it happens:** `CMPController.cs:2836-2845` query returns empty list kalau ada zero `AssessmentSessions` matching `IsPassed.HasValue && CompletedAt.HasValue` in periode default 1 year. Dev DB pasca-RESTORE bisa kosong.

**How to avoid:** Either:
- **(Preferred)** Run FLOW W after FLOW K-S — accumulated 49+ sub-tests creates ~15+ completed assessments. Sequencing per D-319-02 Wave 2 V→W→X already implies post-Wave 1 state. Plan 03 should depend on accumulated state.
- **OR** Pre-condition: Wave 0 W0 smoke that creates 1 dummy Completed session via UI atau verifies DB has ≥1 Completed AssessmentSession with `IsPassed.HasValue`.
- **OR** Loosen assertion: `expect(Array.isArray(data)).toBe(true)` + `expect(response.status()).toBe(200)` — verify endpoint reachable, not data shape.

**Warning signs:** W2 returns `[]` → check `db.queryScalar('SELECT COUNT(*) FROM AssessmentSessions WHERE IsPassed IS NOT NULL AND CompletedAt IS NOT NULL')` → should be ≥1.

### Pitfall 7: AddManualAssessment + EditManualAssessment redirect ke ManageAssessment (NOT same-page success message)

**What goes wrong:** Test asserts `.alert-success` on AddManualAssessment URL — fails (URL changed).

**Why it happens:** Verified TrainingAdminController.cs:636-637 + :728-729:
```csharp
TempData["Success"] = $"Berhasil membuat {model.WorkerCerts!.Count} assessment manual.";
return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
```

**How to avoid:**
```typescript
// After submit, wait redirect first
await Promise.all([
  page.waitForURL(/\/AssessmentAdmin\/ManageAssessment/, { timeout: 10_000 }),
  page.click('#btnSimpanAssessment'),
]);
// THEN assert TempData rendered as alert-success
await expect(page.locator('.alert-success')).toBeVisible();
```

### Pitfall 8: Users table custom Identity rename — `Users` not `AspNetUsers`

**What goes wrong:** DB query `SELECT Id FROM AspNetUsers WHERE Email='...'` → "Invalid object name 'AspNetUsers'".

**Why it happens:** Phase 318 Plan 03 finding (verified `318-03-SUMMARY.md:92`): "Q2 DB table = `Users` (bukan `AspNetUsers`) — project pakai custom Identity table rename."

**How to avoid:** All DB joins/lookups use `Users`. Confirmed already in `interceptAnalyticsResponse` skeleton above. Reminder for FLOW T DB verify:
```sql
SELECT Id FROM Users WHERE Email='rino.prasetyo@pertamina.com'
SELECT Id, Title, IsManualEntry, Score, Status FROM AssessmentSessions WHERE Title=N'[319-T] Manual CRUD ...'
```

---

## Code Examples

Verified patterns from production source files (line citations included).

### Example 1: HC navigate ManualAssessment Add form

```typescript
// Source: Views/Admin/AddManualAssessment.cshtml:46 (form action)
// Source: Controllers/TrainingAdminController.cs:555 GET action
await login(page, 'hc');
await page.goto('/TrainingAdmin/AddManualAssessment');
await expect(page).toHaveURL(/AddManualAssessment/);

// Form visible
await expect(page.locator('form[action*="AddManualAssessment"]')).toBeVisible();

// Card headers (verified Views lines 51, 71, 148, 248)
await expect(page.locator('.card-header', { hasText: 'Peserta Assessment' })).toBeVisible();
await expect(page.locator('.card-header', { hasText: 'Informasi Assessment' })).toBeVisible();
await expect(page.locator('.card-header', { hasText: 'Hasil & Tanggal' })).toBeVisible();

// Required fields IDs (verified Views asp-for binding)
await expect(page.locator('#Title')).toBeAttached();
await expect(page.locator('#Category')).toBeAttached();
await expect(page.locator('#WorkerSelect')).toBeAttached();
await expect(page.locator('#Score')).toBeAttached();
await expect(page.locator('#CompletedAt')).toBeAttached();
```

### Example 2: Submit ManualAssessment + DB verify (FLOW T2-T3)

```typescript
// Phase: T2 — submit form, T3 — DB verify
// Source: Controllers/TrainingAdminController.cs:566-638 (POST handler)
test('T2 — HC submit ManualAssessment via UI', async ({ page }) => {
  await login(page, 'hc');
  await page.goto('/TrainingAdmin/AddManualAssessment');

  // Fill required fields
  await page.fill('#Title', manualTitle);
  await page.selectOption('#Category', 'OJT');
  await page.fill('#Score', '85');
  // PassPercentage default 70 — leave as-is
  await page.fill('#CompletedAt', today());

  // TomSelect interaction — pick worker via dropdown
  await page.locator('.ts-control').click();
  await page.locator('.ts-dropdown input').fill('Rino');
  await page.locator('.ts-dropdown .option', { hasText: /Rino/i }).first().click();
  await expect(page.locator('.worker-cert-card')).toHaveCount(1);

  // Optional: NomorSertifikat
  await page.fill('input[name="WorkerCerts[0].NomorSertifikat"]', `CERT-319-T-${Date.now()}`);

  await expect(page.locator('#btnSimpanAssessment')).toBeEnabled();

  await Promise.all([
    page.waitForURL(/\/AssessmentAdmin\/ManageAssessment/, { timeout: 10_000 }),
    page.click('#btnSimpanAssessment'),
  ]);

  await expect(page.locator('.alert-success', { hasText: /berhasil/i })).toBeVisible();
});

test('T3 — DB verify INSERT (IsManualEntry=1, Status=Completed)', async () => {
  // queryScalar returns COUNT (numeric)
  const count = await db.queryScalar(
    `SELECT COUNT(*) FROM AssessmentSessions WHERE Title = N'${manualTitle.replace(/'/g, "''")}' AND IsManualEntry = 1`
  );
  expect(count).toBe(1);

  // queryString for Status text
  const status = await db.queryString(
    `SELECT TOP 1 Status FROM AssessmentSessions WHERE Title = N'${manualTitle.replace(/'/g, "''")}'`
  );
  expect(status).toBe('Completed');

  // Capture id for T4-T6
  const idStr = await db.queryString(
    `SELECT TOP 1 CAST(Id AS NVARCHAR(20)) FROM AssessmentSessions WHERE Title = N'${manualTitle.replace(/'/g, "''")}' ORDER BY Id DESC`
  );
  manualSessionId = parseInt(idStr, 10);
  expect(manualSessionId).toBeGreaterThan(0);
});
```

### Example 3: ManageCategories CRUD happy path (FLOW U1-U3)

```typescript
// Source: Controllers/AssessmentAdminController.cs:378-498
// Source: Views/Admin/ManageCategories.cshtml:60-130 (add form)
test('U1 — HC create category', async ({ page }) => {
  catName = `[319-U] OJT-${Date.now()}`;
  await login(page, 'hc');
  await page.goto('/AssessmentAdmin/ManageCategories');

  // Click "Tambah Kategori" → expands collapse#addCategoryForm
  await page.click('button[data-bs-target="#addCategoryForm"]');
  await expect(page.locator('#addCategoryForm.show')).toBeVisible();

  await page.fill('#addCategoryForm input[name="name"]', catName);
  await page.fill('#addCategoryForm input[name="defaultPassPercentage"]', '75');
  await page.fill('#addCategoryForm input[name="sortOrder"]', '99');

  await Promise.all([
    page.waitForURL(/\/ManageCategories/, { timeout: 5_000 }),
    page.click('#addCategoryForm button[type="submit"]:has-text("Tambah")'),
  ]);
  await expect(page.locator('.alert-success', { hasText: /berhasil ditambahkan/i })).toBeVisible();
  await expect(page.locator('td', { hasText: catName })).toBeVisible();
});

test('U4 — Duplicate name rejected via TempData alert-danger', async ({ page }) => {
  // Source: AssessmentAdminController.cs:386-390 (duplicate check)
  await login(page, 'hc');
  await page.goto('/AssessmentAdmin/ManageCategories');
  await page.click('button[data-bs-target="#addCategoryForm"]');
  await page.fill('#addCategoryForm input[name="name"]', catName); // same name dari U1
  await page.fill('#addCategoryForm input[name="defaultPassPercentage"]', '80');

  await page.click('#addCategoryForm button[type="submit"]:has-text("Tambah")');
  await page.waitForURL(/\/ManageCategories/, { timeout: 5_000 });
  await expect(
    page.locator('.alert-danger', { hasText: /sudah digunakan/i })
  ).toBeVisible({ timeout: 5_000 });
});
```

### Example 4: Excel APIRequest verify (FLOW V1)

```typescript
// Source: Helpers/ExcelExportHelper.cs:27-38 (ToFileResult — MIME + filename)
// Source: Controllers/AssessmentAdminController.cs:526 ExportCategoriesExcel
test('V1 — HC export categories Excel → 200 + xlsx MIME + bytes>2048', async ({ page }) => {
  await login(page, 'hc');
  const { bytes, filename, contentType } = await verifyExcelDownload(
    page,
    '/AssessmentAdmin/ExportCategoriesExcel',
    { minBytes: 2048, filenamePattern: /\.xlsx$/i }
  );
  expect(contentType).toContain('spreadsheetml');
  expect(filename).toMatch(/\.xlsx$/);
  expect(bytes).toBeGreaterThan(2048);
});
```

### Example 5: Analytics JSON intercept (FLOW W2)

```typescript
// Source: Controllers/CMPController.cs:2731-2813 (GetAnalyticsSummary)
// Verified return shape: { totalSessions, passRate, expiringCount, avgGainScore }
test('W2 — Intercept GetAnalyticsSummary → assert shape', async ({ page }) => {
  await login(page, 'hc');

  const data = await interceptAnalyticsResponse(
    page,
    async () => {
      await page.goto('/CMP/AnalyticsDashboard');
      // Initial bootstrap fires endpoints automatically — no extra click needed
    },
    '/CMP/GetAnalyticsSummary'
  );

  expect(data).toHaveProperty('totalSessions');
  expect(data).toHaveProperty('passRate');
  expect(data).toHaveProperty('expiringCount');
  expect(data).toHaveProperty('avgGainScore');
  expect(typeof data.totalSessions).toBe('number');
  expect(typeof data.passRate).toBe('number');
});

test('W3 — DOM canvas smoke + analyticsConfig div presence', async ({ page }) => {
  // Source: Views/CMP/AnalyticsDashboard.cshtml:38 + 244 + 291 + 331
  await login(page, 'hc');
  await page.goto('/CMP/AnalyticsDashboard');
  await expect(page.locator('#analyticsConfig')).toBeAttached();
  await expect(page.locator('canvas#failRateChart')).toBeAttached();
  await expect(page.locator('canvas#trendChart')).toBeAttached();
  await expect(page.locator('canvas#gainScoreTrendChart')).toBeAttached();
  // Verify Chart.js library loaded
  const chartLoaded = await page.evaluate(() => typeof (window as any).Chart === 'function');
  expect(chartLoaded).toBe(true);
});

test('W4 — DB cross-check: GetAnalyticsSummary totalSessions matches COUNT', async ({ page }) => {
  await login(page, 'hc');
  const data = await interceptAnalyticsResponse(
    page,
    () => page.goto('/CMP/AnalyticsDashboard'),
    '/CMP/GetAnalyticsSummary'
  );
  // Default periode = last 1 year
  const dbCount = await db.queryScalar(
    `SELECT COUNT(*) FROM AssessmentSessions WHERE IsPassed IS NOT NULL AND CompletedAt IS NOT NULL AND CompletedAt >= DATEADD(year, -1, GETDATE())`
  );
  expect(data.totalSessions).toBe(dbCount);
});
```

### Example 6: CertificationManagement (FLOW X — CDP variant per Pitfall 2)

```typescript
// Source: Views/CDP/CertificationManagement.cshtml:1-589
// Source: Controllers/CMPController.cs:3666 (same action body works via /CDP if route exists,
//         OR Controllers/CDPController.cs:~3539 untuk literal /CDP URL)
test('X1 — HC navigate CertificationManagement', async ({ page }) => {
  await login(page, 'hc');
  await page.goto('/CDP/CertificationManagement');
  await expect(page).toHaveURL(/CertificationManagement/);
  await expect(page.locator('h2', { hasText: /Certification Management/i })).toBeVisible();
  await expect(page.locator('#filter-category')).toBeVisible();
  await expect(page.locator('#cert-table-container')).toBeVisible();
});

test('X2 — Filter by judul → AJAX partial refresh', async ({ page }) => {
  // Source: CDP/CertificationManagement.cshtml:280-310 (refreshTable fetch)
  await login(page, 'hc');
  await page.goto('/CDP/CertificationManagement');

  // Pick first category from dropdown
  const firstCat = await page.locator('#filter-category option').nth(1).getAttribute('value');
  if (!firstCat) test.skip(); // no categories di DB

  const responsePromise = page.waitForResponse(
    (r) => r.url().includes('/FilterCertificationManagement'),
    { timeout: 10_000 }
  );
  await page.selectOption('#filter-category', firstCat);
  const response = await responsePromise;
  expect(response.status()).toBe(200);
  // Response is partial HTML
  const html = await response.text();
  expect(html).toContain('table'); // or '_SertifikatGroupTablePartial' marker
});

test('X3 — Detail page navigation', async ({ page }) => {
  // Source: CMPController.cs:3716 CertificationManagementDetail (or CDP equivalent)
  await login(page, 'hc');
  await page.goto('/CDP/CertificationManagement');
  const firstJudulLink = page.locator('#cert-table-container a[href*="CertificationManagementDetail"]').first();
  const hasJudul = await firstJudulLink.count() > 0;
  if (!hasJudul) test.skip(); // empty data
  await firstJudulLink.click();
  await page.waitForURL(/CertificationManagementDetail/, { timeout: 10_000 });
  await expect(page.locator('h2, h3, h4', { hasText: /Detail/i }).first()).toBeVisible();
});
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Multi-file spec per FLOW (`flow-t.spec.ts`, `flow-u.spec.ts`) | Single consolidated `exam-types.spec.ts` (49→69 sub-tests) | Phase 317 D | Easier sequential coordination + shared state via describe-scoped `let` |
| Custom selectors per phase | Centralized `wizardSelectors.ts` + inline helpers | Phase 307+317 | Single source of truth for IDs (e.g., `#Title`, `#Category`, `#schedDateInput`) |
| Browser `<a download>` capture | APIRequest pattern `page.request.get()` | Phase 318 Plan 04 R4 | Cookies inherited, no UI dialog wait, faster |
| `queryScalar` for all DB lookups | `queryScalar` (numeric) + `queryString` (text) | Phase 318 Plan 03 | `queryScalar` regex `/^-?\d+/m` rejected string Status values |
| Custom AspNetUsers table | Custom `Users` table rename | Pre-v15 (verified Phase 318 P03) | All DB joins use `Users`, NOT `AspNetUsers` |

**Deprecated/outdated:**
- `tests/helpers/dbSnapshot.queryScalar()` for strings: use `queryString()` instead — already deprecated by Phase 318 Plan 03 finding.
- Manual `fetch()` cookie cookbook: superseded by `page.request.get()` APIRequest.

---

## Assumptions Log

> Claims yang TIDAK fully verified — flagged untuk Wave 0 sanity check.

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `/AssessmentAdmin/ExportCategoriesExcel` returns >2048 bytes pada dev DB lokal | Pitfall 1 / Example 4 | Empty DB → 1-row header-only workbook ~500 bytes → V1 fail. **Mitigation:** Wave 0 V0 smoke download + log bytes; adjust min threshold kalau perlu (e.g., 256 bytes for header-only minimum sufficient untuk "endpoint reachable" guarantee). |
| A2 | `/CDP/CertificationManagement` reachable + `BuildSertifikatRowsAsync` works dengan dev DB data | Pitfall 2 / Example 6 | Empty DB or auth role gating fails → X1 RED. **Mitigation:** Wave 0 X0 smoke navigate + assert `<h2>` heading; gracefully `test.skip()` di X2-X3 kalau zero data. |
| A3 | TomSelect `.ts-control` interaction pattern works untuk `#WorkerSelect` (Pattern 1 ManualAssessment) | Pitfall 4 / Example 2 | TomSelect upgrade or markup variation breaks selector. **Mitigation:** Wave 0 T0 smoke TomSelect search + pick; fallback to `page.evaluate()` injection (Pattern 2). |
| A4 | `GetAnalyticsSummary` returns `totalSessions` field name exact (camelCase) — verified controller emits `Json(new { totalSessions, ... })` C# anonymous object which ASP.NET Core serializes camelCase by default | Example 5 / Pattern 3 | If JsonSerializerOptions configured PascalCase, key name = `TotalSessions`. **Mitigation:** Wave 0 W0 smoke log raw JSON, adjust property names. |

**Assumptions tertinggi (P1):** A1 + A2 (akan menentukan FLOW V dan X bisa jalan). A3 + A4 = pattern flexibility (mitigation cepat).

---

## Open Questions

1. **CMP variant CertificationManagement scope (Pitfall 2)**
   - What we know: CMP action body identical ke CDP semantically; view file missing di Views/CMP/
   - What's unclear: Apakah user expectation "CMP variant only" benar-benar `/CMP/CertificationManagement` URL, atau secara logis "non-CDP coachee personal cert view" (yang sebenarnya = `/CDP/CertificationManagement` route digunakan HC perspective)
   - Recommendation: Planner ask user via plan-checker iter 1 OR default to Option A (CDP variant). Document the decision di Plan 04 SUMMARY.

2. **FLOW V Excel endpoint final pick**
   - What we know: 7 endpoints exist (table di Pitfall 1)
   - What's unclear: D-319-03 sebut "Admin/ExportAssessmentMatrix" yang tidak ada
   - Recommendation: Plan 03 use `/AssessmentAdmin/ExportCategoriesExcel` default; user-confirm via task checkbox.

3. **Wave 0 sub-test count**
   - What we know: Phase 317 had 2 W0.x (A4+A5), Phase 318 had 1-2 W0.x
   - What's unclear: Apakah 4 YELLOW assumptions di atas perlu 4 individual W0 sub-tests atau dapat dibundle ke 1-2
   - Recommendation: Plan 01 bundle W0.T0 (TomSelect smoke), Plan 03 bundle W0.V0 (Excel smoke) + W0.W0 (Analytics JSON smoke), Plan 04 bundle W0.X0 (CertMgmt navigate smoke). Total = 4 Wave 0 sub-tests across 3 plans. Acceptable budget.

4. **REQUIREMENTS.md QA-09 wording**
   - What we know: D-319-09 menentukan additive entry
   - What's unclear: Exact wording — bisa terlalu broad atau terlalu sempit
   - Recommendation: Use exact verbatim dari D-319-09 — "Admin features E2E coverage — ManualAssessment CRUD, ManageCategories CRUD, Export Excel endpoint validation, Analytics dashboard JSON+DOM verification, CertificationManagement listing+filter." Lalu append `| QA-09 | 319 | Complete |` row di Traceability table.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET runtime (Kestrel) | Local site `http://localhost:5277` | Required at test time | per `dotnet build` | None — must be running |
| SQL Server Express (`localhost\SQLEXPRESS`) | `db.queryScalar`, `db.queryString` | Required (existing project setup) | per appsettings.Development.json | None — local-only constraint |
| Node.js + npm + Playwright | Test execution | Required (existing project setup) | per `tests/package.json` | None |
| `sqlcmd` (Windows Integrated Auth `-E`) | DB queries | Required (Phase 315 verified) | per SQL Server install | None |
| Chart.js v4 (CDN) | Browser-side runtime (FLOW W) | Required via cdn.jsdelivr.net | v4 | None — but graceful: if CDN down, JSON intercept (W2) still works without canvas mount (W3 may fail) |
| ClosedXML (server-side NuGet) | Excel export response | Already in `Helpers/ExcelExportHelper.cs` deps | — | None |
| TomSelect v2 (CDN) | ManualAssessment form (FLOW T) | Required via cdn.jsdelivr.net | v2 | Pattern 2 `page.evaluate()` injection bypass |

**Missing dependencies with no fallback:** None — all required deps already in production project + existing test infra.

**Missing dependencies with fallback:** Chart.js CDN, TomSelect CDN (offline fallback documented above).

---

## Validation Architecture

> Including per workflow.nyquist_validation enabled state (key absent in `.planning/config.json` = treat as enabled).

### Test Framework
| Property | Value |
|----------|-------|
| Framework | `@playwright/test` (per `tests/package.json`) |
| Config file | `tests/playwright.config.ts` (verified — `testDir: './e2e'`, sequential `fullyParallel: false`, 60s timeout per test, action 10s) |
| Quick run command | `cd tests && npx playwright test exam-types.spec.ts -g "FLOW T"` (per-FLOW) |
| Full suite command | `cd tests && npx playwright test exam-types.spec.ts` (all 69 sub-tests) |
| Type gate | `cd tests && npx tsc --noEmit` (Phase 318 pattern) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|--------------|
| QA-09 (T1-T6) | ManualAssessment CRUD via HC UI | E2E (browser + DB) | `npx playwright test -g "FLOW T"` | ❌ Plan 01 |
| QA-09 (U1-U4) | ManageCategories CRUD via HC UI | E2E (browser + DB) | `npx playwright test -g "FLOW U"` | ❌ Plan 02 |
| QA-09 (V1-V3) | Excel endpoint validation (MIME + size) | E2E (APIRequest binary) | `npx playwright test -g "FLOW V"` | ❌ Plan 03 |
| QA-09 (W1-W4) | Analytics JSON shape + canvas smoke + DB cross-check | E2E (intercept + DOM + DB) | `npx playwright test -g "FLOW W"` | ❌ Plan 03 |
| QA-09 (X1-X3) | CertificationManagement navigate + filter + detail | E2E (browser + AJAX) | `npx playwright test -g "FLOW X"` | ❌ Plan 04 |

### Sampling Rate
- **Per task commit:** `cd tests && npx playwright test exam-types.spec.ts -g "FLOW {X}"` (single FLOW, < 60s)
- **Per wave merge:** `cd tests && npx playwright test exam-types.spec.ts` (full 69 sub-tests, ~4 min based on Phase 318 baseline 3.2m / 49 tests)
- **Phase gate:** Full suite 69/69 HIJAU + `npx tsc --noEmit` exit 0 sebelum `/gsd-verify-work`

### Wave 0 Gaps
- [ ] **W0.T0** — TomSelect `#WorkerSelect` smoke (Plan 01) — verify A3 interaction pattern
- [ ] **W0.V0** — Excel endpoint final pick + smoke download (Plan 03) — verify A1 byte size
- [ ] **W0.W0** — Analytics JSON shape log raw response (Plan 03) — verify A4 camelCase
- [ ] **W0.X0** — CertificationManagement navigate smoke (Plan 04) — verify A2 reachability + scope decision

*(No new test framework install — existing Playwright setup covers all phase requirements.)*

---

## Security Domain

> Required (no explicit `security_enforcement: false` in config).

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | Cookie auth via ASP.NET Identity (existing). Tests login via `login(page, 'hc')` — credentials hardcoded in `tests/helpers/accounts.ts` for **local-only** scope. |
| V3 Session Management | yes | `.AspNetCore.Identity.Application` cookie; APIRequest pattern inherits via page context |
| V4 Access Control | yes | `[Authorize(Roles = "Admin, HC")]` di semua relevant actions (verified TrainingAdminController.cs:565, AssessmentAdminController.cs:367, CMPController.cs:2492+2508). Tests pakai role `hc`. |
| V5 Input Validation | yes | Server-side: `if (!ModelState.IsValid)` (TrainingAdminController.cs:588), `string.IsNullOrWhiteSpace(name)` (AssessmentAdminController.cs:380), file validation `FileUploadHelper.ValidateCertificateFile` (TrainingAdminController.cs:581). Tests verify positive + negative paths (U4 duplicate, T2 happy). |
| V6 Cryptography | no | No new crypto in Phase 319 |
| V13 API & Web Service | partial | JSON endpoints (GetAnalyticsSummary, FilterCertificationManagement) → tests verify auth gating + response shape, NOT injection (out of QA-09 scope) |

### Known Threat Patterns for Portal HC stack

| Pattern | STRIDE | Standard Mitigation (already in code) |
|---------|--------|---------------------------------------|
| Antiforgery bypass | Tampering | `[ValidateAntiForgeryToken]` on all POST actions (verified). Playwright uses real form submission inheriting cookies + token. |
| SQL injection in category Name | Tampering | EF Core parameterized queries (no raw SQL). Test U4 uses real form input — exercises parameterization. |
| Path traversal in file upload | Tampering | `FileUploadHelper.ValidateCertificateFile` + `FileUploadHelper.SaveFileAsync` (TrainingAdminController.cs:601). Out of FLOW T scope (file optional). |
| IDOR EditManualAssessment/{id} | Authorization | `[Authorize(Roles = "Admin, HC")]` + DB filter `s.Id == id && s.IsManualEntry` (TrainingAdminController.cs:647). Worker can't access. Test T5 verifies worker visibility through Worker context, not direct edit URL. |
| Role escalation | Elevation of Privilege | Role check `Authorize(Roles="Admin, HC")` — Coachee blocked. Out of FLOW T-X positive scope. |

**No new security controls required** — Phase 319 = test coverage of existing controls, not new auth/crypto.

---

## Sources

### Primary (HIGH confidence — direct source-file read this session)

- `Controllers/TrainingAdminController.cs:555-753` — AddManualAssessment GET+POST, EditManualAssessment GET+POST, DeleteManualAssessment POST
- `Controllers/AssessmentAdminController.cs:344-549` — ManageCategories + AddCategory + EditCategory + DeleteCategory + ToggleCategoryActive + ExportCategoriesExcel
- `Controllers/CMPController.cs:2493-2848` — AnalyticsDashboard + GetAnalyticsData + GetAnalyticsSummary + GetFailRateData + GetTrendData (verified actual return shapes)
- `Controllers/CMPController.cs:3637-3865` — CertificationManagement (CMP variant action body) + Filter + Detail + ExportSertifikatExcel + ExportSertifikatDetailExcel
- `Helpers/ExcelExportHelper.cs:1-40` — XLWorkbook → File() with exact MIME `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- `Views/Admin/AddManualAssessment.cshtml:1-467` — form structure, TomSelect, dynamic worker rows, submit button gating
- `Views/Admin/EditManualAssessment.cshtml:1-100` — edit form fields
- `Views/Admin/ManageCategories.cshtml:1-516` — add/edit/delete forms, table structure, TempData alerts, delete modal
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml:200-337` — nested collapse row table structure (Pitfall 3)
- `Views/CMP/AnalyticsDashboard.cshtml:1-589` — Chart.js mount IDs, filter form, JS config div
- `Views/CDP/CertificationManagement.cshtml:1-589` — actual production CertMgmt UI (CDP route reference for Pitfall 2)
- `tests/e2e/exam-types.spec.ts:1-1406` — current 49 sub-tests, sequential mode, describe pattern
- `tests/e2e/helpers/examTypes.ts:1-622` — 10 existing exports including `verifyCertificatePdfDownload` pattern reference
- `tests/e2e/helpers/wizardSelectors.ts:1-122` — selector constants pattern
- `tests/helpers/utils.ts:1-39` — `today()`, `yesterday()`, `tomorrow()`, `uniqueTitle()`
- `tests/helpers/dbSnapshot.ts:1-157` — `queryScalar` numeric, `queryString` text
- `tests/helpers/accounts.ts:1-13` — `hc` = `meylisa.tjiang@pertamina.com`, `coachee` = `rino.prasetyo@pertamina.com`
- `tests/playwright.config.ts` — global config (60s timeout, sequential, action 10s)
- `.planning/phases/318-*/318-03-SUMMARY.md` — `Users` table custom rename + shared-package finding
- `.planning/phases/318-*/318-04-SUMMARY.md` — APIRequest PDF proof (159KB)

### Secondary (MEDIUM confidence — extrapolation dari Phase 317-318)

- TomSelect v2 `.ts-control` interaction pattern — Phase 317 Step 2 worker check uses similar dropdown, but TomSelect specific in AddManualAssessment is new. **Flag A3 untuk Wave 0 verify.**
- Chart.js v4 `canvas#failRateChart` attached state — DOM element verified, mount confirmation requires runtime check (W3).
- AnalyticsDashboard JSON response camelCase serialization — ASP.NET Core default behavior, but project-specific JsonSerializerOptions tidak di-verify. **Flag A4.**

### Tertiary (LOW confidence — not applicable)

None — all critical claims either VERIFIED (primary) atau FLAGGED (assumptions log).

---

## Metadata

**Confidence breakdown:**
- Standard stack: **HIGH** — all libraries verified from production code direct read
- Architecture: **HIGH** — controllers + views all read line-by-line for relevant ranges
- Pitfalls: **HIGH** — each pitfall backed by specific line citation in production code
- Helpers integration: **HIGH** — existing examTypes.ts read fully (622 LOC), append point verified

**Research date:** 2026-05-12
**Valid until:** 2026-06-12 (30 days — Portal HC codebase stable, Phase 318 baseline frozen)

**Open Q resolution required before Plan 03 execute:** Q1 (CertMgmt scope), Q2 (Excel endpoint pick). Q3 + Q4 = planner discretion.

---

*Phase: 319-manualassessment-export-excel-analytics-certificationmanagement-e2e*
*Research completed: 2026-05-12 — RESEARCH.md ready for `/gsd-plan-phase 319` consumption.*
