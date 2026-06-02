---
phase: 337
slug: cmp-records-full-overhaul-filter-data-arch-a11y
status: partial
nyquist_compliant: false
wave_0_complete: true
created: 2026-06-02
mode: state_b_reconstruction
notes: >
  Phase SHIPPED LOCAL (26/26 REQ, 13 commit lokal, belum push ke origin/main).
  VALIDATION.md ini adalah backfill retroaktif — tidak ada test file CMP baru yang
  di-generate karena implementation sudah closed. Coverage adalah kombinasi:
  (a) existing 18-baseline xUnit regression yang tetap GREEN, dan
  (b) auto-Playwright UAT lokal yang di-dokumentasikan di SUMMARY Wave 1/2/3.
  nyquist_compliant: false karena tidak ada unit test baru yang spesifik untuk
  CMP filter behavior — covered via manual Playwright + grep code-verify saja.
---

# Phase 337 — Validation Strategy

> Backfill retroaktif (State B). Phase SHIPPED LOCAL 2026-05-30. Dokumen ini memetakan
> coverage verification per REQ untuk 26 REQ (CMP-01..26) di 3 wave.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.x (HcPortal.Tests/) + Playwright MCP (manual UAT) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests/ --no-build -v q` |
| **Full suite command** | `dotnet test HcPortal.Tests/ -v n` |
| **Estimated runtime** | ~141ms (18 baseline tests) |
| **CMP-specific e2e** | `tests/e2e/CMP/Records.spec.ts` — MISSING (gap, tidak dibuat saat Phase 337) |

---

## Sampling Rate

- **Setelah setiap task commit Wave 1/2/3:** `dotnet build` (0 error) + `dotnet test` (18/18)
- **Sebelum claim wave complete:** Auto-Playwright UAT lokal (documented di SUMMARY)
- **Max feedback latency:** ~141ms (regression smoke xUnit only)

---

## Per-Task Verification Map

### Wave 1 — Filter + Data Integrity (Plan 01, CMP-01..11)

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 337-01-T1 | 01 | 1 | CMP-01 (statusFilter mandiri) | T-337-01-01 | statusFilter `!= "ALL"` guard — tidak bypass tanpa Category | manual+grep | `dotnet test HcPortal.Tests/ -v q` (regression) | ❌ no unit test — grep `statusFilter != "ALL"` WorkerDataService.cs:391 | ✅ green |
| 337-01-T1 | 01 | 1 | CMP-02 (subCategory server-side) | T-337-01-01 | subCategory param forwarded CMPController → GetWorkersInSection | manual+grep | `dotnet test HcPortal.Tests/ -v q` | ❌ no unit test — grep `GetWorkersInSection.*subCategory` | ✅ green |
| 337-01-T1 | 01 | 1 | CMP-03 (Category narrow workerList) | T-337-01-01 | post-loop workerList.Where(Category equality) | manual+grep | `dotnet test HcPortal.Tests/ -v q` | ❌ no unit test — grep `workerList.Where.*Kategori` WorkerDataService.cs:371 | ✅ green |
| 337-01-T1 | 01 | 1 | CMP-04 (Category equality tidak substring) | T-337-01-01 | `string.Equals(..., OrdinalIgnoreCase)` BUKAN `.Contains` | code-verify | `dotnet test HcPortal.Tests/ -v q` | ❌ no unit test — grep `string.Equals.*Kategori.*OrdinalIgnoreCase` | ✅ green (code-verified, UAT SKIP — tidak ada seed POJT) |
| 337-01-T2 | 01 | 1 | CMP-05 (Export Training honor subCategory) | T-337-01-01 | ExportRecordsTeamTraining forward subCategory ke service + SQL push-down | manual+grep | `dotnet test HcPortal.Tests/ -v q` | ❌ no unit test — grep `ExportRecordsTeamTraining.*subCategory` CMPController.cs:704 | ✅ green |
| 337-01-T3 | 01 | 1 | CMP-06 (IsPassed null → "Completed") | T-337-01-01 | Three-way switch `null => "Completed"` di GetUnifiedRecords | code-verify | `dotnet test HcPortal.Tests/ -v q` | ❌ no unit test — grep `null => "Completed"` WorkerDataService.cs:52-57 | ✅ green |
| 337-01-T3 | 01 | 1 | CMP-07 (Training SertifikatUrl link) | T-337-01-03 | `Records.cshtml` render `<a href="@item.SertifikatUrl">Lihat</a>` | manual | Playwright UAT lokal (SUMMARY L99 PASS) | ❌ no e2e spec | ✅ green (manual UAT) |
| 337-01-T3 | 01 | 1 | CMP-08 (Permanent badge hijau) | — | `Records.cshtml` badge `bg-success` + infinity icon bila Status=Permanent | manual | Playwright UAT lokal (SUMMARY L100 PASS) | ❌ no e2e spec | ✅ green (manual UAT) |
| 337-01-T3 | 01 | 1 | CMP-09 (HtmlEncode search "&" match) | — | `decodeEntities()` helper JS-side decode sebelum compare | manual | Playwright UAT lokal: search "&" match "Q&A Session 337" (SUMMARY L101) | ❌ no e2e spec | ✅ green (manual UAT) |
| 337-01-T3 | 01 | 1 | CMP-10 (per-filter counter real-time) | — | 4 counter IDs update via `filterTable()` JS | manual | Playwright UAT lokal: 4 counter update saat search (SUMMARY L102) | ❌ no e2e spec | ✅ green (manual UAT) |
| 337-01-T3 | 01 | 1 | CMP-11 (AttemptNumber title-null default 1) | — | title-null branch → `attemptNumber = 1` (no archived lookup) | code-verify | grep `attemptNumber = 1` WorkerDataService.cs:175-185 | ❌ no unit test | ✅ green (code-verified, UAT SKIP — count IS NULL = 0 di lokal) |

### Wave 2 — UX + Quality (Plan 02, CMP-12..23)

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 337-02-T3 | 02 | 2 | CMP-12 (AbortController AJAX race) | T-337-02-03 | `currentAbortController.abort()` sebelum fetch baru — late response di-cancel | code-verify | grep `currentAbortController` RecordsTeam.cshtml | ❌ no e2e spec | ✅ green (code-verified DOM probe) |
| 337-02-T3 | 02 | 2 | CMP-13 (Export URL sync sebelum debounce) | T-337-02-01 | `updateExportLinks()` dipanggil awal `filterTeamTable()` — SEBELUM setTimeout | manual | Playwright UAT: href update before 300ms (SUMMARY L96 PASS) | ❌ no e2e spec | ✅ green (manual UAT) |
| 337-02-T3 | 02 | 2 | CMP-14 (SubCategory disabled tanpa children) | — | `subSelect.disabled = !hasChildren` di categoryFilter onChange handler | manual | Playwright UAT: initial disabled + after Category → enabled (SUMMARY L97 PASS) | ❌ no e2e spec | ✅ green (manual UAT) |
| 337-02-T3 | 02 | 2 | CMP-15 (Date hint counter UX) | — | `updateDateHint()` tampil saat count < initial, tersembunyi saat no date filter | manual | Playwright UAT: dateFrom=2020 → hint display (SUMMARY L98 PASS) | ❌ no e2e spec | ✅ green (manual UAT) |
| 337-02-T2 | 02 | 2 | CMP-16 (Year quick filter btn-group) | — | btn-group `yearOptions.Take(3)` + "Semua Tahun" di My Records tab | manual | Playwright UAT: btn group [2026, Semua] render + active toggle (SUMMARY L99 PASS) | ❌ no e2e spec | ✅ green (manual UAT) |
| 337-02-T3 | 02 | 2 | CMP-17 (Filter state sessionStorage persist) | T-337-02-01 | `sessionStorage.setItem('cmp-records-team-filter', ...)` + restore DOMContentLoaded | manual | Playwright UAT: reload → Category restored + workerCount=2 (SUMMARY L100 PASS) | ❌ no e2e spec | ✅ green (manual UAT) |
| 337-02-T4 | 02 | 2 | CMP-18 (Dead data-* purge) | — | `_RecordsTeamBody.cshtml` hanya punya data-section/unit/name/nip | code-verify | grep `data-categories\|data-subcategories\|data-completed\|data-has-training` _RecordsTeamBody.cshtml → 0 match | ❌ no unit test | ✅ green (grep 0 match VERIFICATION.md L150) |
| 337-02-T2 | 02 | 2 | CMP-19 (Keyboard nav row Enter) | — | `tabindex="0" role="link"` + `addEventListener('keydown', ...)` di Records.cshtml | code-verify+manual | Playwright UAT: tabindex="0" + handler attached (SUMMARY L102 PASS) | ❌ no e2e spec | ✅ green (manual UAT) |
| 337-02-T2 | 02 | 2 | CMP-20 (ARIA tab roles) | — | `role="tablist"/"tab"/"tabpanel"` + `aria-controls` + `aria-selected` di Records.cshtml | code-verify+manual | Playwright UAT: ARIA toggle via shown.bs.tab (SUMMARY L103 PASS) | ❌ no e2e spec | ✅ green (manual UAT) |
| 337-02-T1 | 02 | 2 | CMP-21 (ViewModel ganti UserManager di view) | T-337-02-02 | `@model CMPRecordsViewModel` — 0 actual `@inject UserManager` di Records.cshtml + RecordsTeam.cshtml | code-verify | grep `@inject UserManager` Records.cshtml RecordsTeam.cshtml → 0 actual match (1 komentar only) | ❌ no unit test | ✅ green (grep verified VERIFICATION.md L151) |
| 337-02-T1 | 02 | 2 | CMP-22 (roleLevel single-source controller) | T-337-02-05 | `Model.RoleLevel` di view + `ViewData["RoleLevel"]` di partial — tidak ada duplikasi | code-verify | grep `roleLevel <= 4` Records.cshtml → single source via `Model.RoleLevel` | ❌ no unit test | ✅ green (code-verified) |
| 337-02-T1 | 02 | 2 | CMP-23 (Year memoize controller) | — | `YearOptions` di-compute controller, view consume `Model.YearOptions` — 0 `Model.Select(r => r.Date.Year)` di view | code-verify | grep `Model\.Select.*Date\.Year` Records.cshtml → 0 match (VERIFICATION.md L152) | ❌ no unit test | ✅ green (grep 0 match) |

### Wave 3 — Arch SQL Push-Down + Pagination (Plan 03, CMP-24..26)

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 337-03-T1 | 03 | 3 | CMP-24 (GetAllWorkersHistory SQL push-down) | T-337-03-03 | IQueryable + Select projection ke DTO SEBELUM ToListAsync — no full entity materialization | code-verify+EF-log | grep `IEnumerable<string>? workerIds = null` IWorkerDataService.cs + EF log single SELECT (SUMMARY SQL log) | ❌ no unit test | ✅ green (EF log verified lokal) |
| 337-03-T2 | 03 | 3 | CMP-25 (Date filter SQL WHERE COALESCE) | T-337-03-03 | `(tr.TanggalMulai ?? tr.Tanggal) >= dateFrom` → EF translate ke `COALESCE([t].[TanggalMulai], [t].[Tanggal]) >= @p` | code-verify+EF-log | grep `trainingsQuery.Where.*TanggalMulai.*dateFrom` WorkerDataService.cs + EF log COALESCE clause | ❌ no unit test | ✅ green (EF log COALESCE verified SUMMARY-03 L96) |
| 337-03-T3 | 03 | 3 | CMP-26 (Pagination Team View 20/50/100) | T-337-03-01 (pageSize whitelist) | `PaginationHelper.Calculate` + whitelist `(pageSize == 20\|50\|100)` + X-Pagination header + sessionStorage | manual+grep | grep `PaginationHelper\.Calculate` CMPController.cs:774 + Playwright UAT pager render (SUMMARY-03 L97) | ❌ no e2e spec | ✅ green (manual UAT — pager hidden di lokal 12 worker, API path verified) |

---

## Wave 0 Requirements

Existing infrastructure digunakan — tidak ada instalasi framework baru diperlukan.

**Infrastruktur yang digunakan:**
- [x] `HcPortal.Tests/` — xUnit baseline 18 test (regression smoke saja, bukan CMP-behavior-specific)
- [x] `dotnet build` sebagai automated verify per task commit (0 error = gate pass)

**File yang MISSING (gap, tidak dibuat selama Phase 337):**
- [ ] `tests/e2e/CMP/Records.spec.ts` — Playwright e2e spec untuk CMP/Records TIDAK ADA. UAT dilakukan via Playwright MCP interaktif dan tidak di-persist sebagai file test. Ini adalah gap Nyquist: tidak ada otomasi yang dapat di-re-run tanpa manual intervention.

**Justifikasi gap:** Phase 337 menggunakan pattern UAT interaktif via Playwright MCP (same pattern sebagai Phase 325-335). File e2e spec tidak di-mandate dalam execute workflow. Coverage tetap via kombinasi dotnet build + auto-Playwright MCP session + grep code-verify.

---

## Manual-Only Verifications

Item berikut adalah 6 pillar dari `337-VERIFICATION.md` yang memerlukan Dev environment setelah IT promo v20.0:

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Live UAT 6-pillar /CMP/Records di Dev (10.55.3.3) setelah IT promo | CMP-01..05, CMP-17, CMP-19, CMP-26 | Phase belum di-push ke origin/main. Dev environment dengan data prod-like diperlukan untuk konfirmasi penuh. Lokal hanya 12 worker seed. | Login admin di 10.55.3.3/KPB-PortalHC → /CMP/Records → (1) Status=Sudah tanpa Category → narrow, (2) Category+SubCategory → narrow, (3) Export Training dengan SubCategory → buka xlsx verify, (4) pager muncul jika >20 worker, (5) reload → sessionStorage restore, (6) keyboard Tab+Enter ke row |
| CMP-04 Equality OJT vs POJT di Dev | CMP-04 | UAT lokal SKIP — tidak ada seed POJT di DB lokal (hanya Gas Tester/Mandatory HSSE). Code diff verified saja. | Di DB Dev: pastikan ada training Kategori="POJT" terpisah dari "OJT". Set filter Category=OJT → verify worker POJT-only tidak muncul (equality, bukan substring) |
| CMP-11 AttemptNumber title-null di Dev | CMP-11 | DB lokal `SELECT COUNT(*) FROM AssessmentSessions WHERE Title IS NULL OR Title = ''` = 0. Perlu data legacy. | Di DB Dev: jalankan count query. Bila ada → login sebagai worker bersangkutan → My Records → verify AttemptNumber = 1 (bukan anomali collision) |
| SQL push-down EF Core log di SQL Server Dev (CMP-24/25) | CMP-24, CMP-25 | EF log verified di LocalDB lokal. SQL Server dialect di Dev perlu konfirmasi — khususnya COALESCE translation + DateOnly/DateTime2 mix. | Enable `Microsoft.EntityFrameworkCore.Database.Command: Information` di appsettings Dev (sementara). Filter date+category di Team View → inspect log → verify COALESCE clause + no N+1. REVERT setelah verify. |
| Pagination pager functional dengan >20 worker di Dev | CMP-26 extended | Lokal 12 worker → TotalPages=1 → pager hidden. Dev mungkin punya lebih banyak worker sehingga pager visible dan perlu diverifikasi. | Login admin Dev → /CMP/Records Team View → jika total worker >20 → verify pager muncul (sliding window ≤7 button) → klik page 2 → AJAX partial refresh → workerList halaman 2 tampil |
| Mobile responsive /CMP/Records di device nyata | (implicit quality) | Mobile-first redesign = SCOPE OUT (D-03), tapi basic responsive perlu eyeball. | Buka /CMP/Records di mobile device 375px viewport → verify filter card stack vertikal, tabel tidak overflow horizontal, pager readable |

---

## Nyquist Compliance Note

**`nyquist_compliant: false`** — alasan:

1. **Tidak ada unit test baru spesifik untuk CMP behavior.** CMP-01..26 tidak memiliki unit test xUnit yang menguji filter logic, SQL push-down, pagination, atau render behavior. Semua CMP REQ di-verify via kombinasi: (a) `dotnet build` 0 error per task, (b) `dotnet test 18/18` regression baseline, (c) auto-Playwright MCP session interaktif, (d) grep code-verify untuk path yang tidak memiliki seed data.

2. **Tidak ada `tests/e2e/CMP/Records.spec.ts`.** File ini adalah gap yang diidentifikasi — tidak dibuat selama atau setelah Phase 337 execution.

3. **Implikasi:** Regression detection untuk CMP filter behavior bergantung pada manual re-test di browser. Jika ada code change di `WorkerDataService.cs` atau `CMPController.cs` di future phase, tidak ada otomasi yang akan catch regression di CMP filter path.

**Mengapa tidak di-remediate sekarang:** Phase sudah SHIPPED. Instruction: "Don't auto-spawn new unit tests (Phase shipped — backfill only classification)." Unit test baru untuk CMP behavior akan dipertimbangkan di v21.0 sebagai tech debt item.

---

## Validation Sign-Off

- [x] Semua task memiliki verify path (dotnet build / grep / manual UAT documentation)
- [ ] Sampling continuity: TIDAK semua task punya automated verify — CMP-07..10 + semua Wave 2 UX = manual-only (TIDAK nyquist_compliant)
- [x] Wave 0: existing infra digunakan (tidak ada install baru)
- [x] Tidak ada watch-mode flags
- [x] Feedback latency: `dotnet test` ~141ms (regression only)
- [ ] `nyquist_compliant: true` — TIDAK dipenuhi (lihat note di atas)

**Verification reference:** `337-VERIFICATION.md` (2026-06-02T08:00:00Z, status: human_needed, score: 26/26 code-verified)

**Approval:** partial 2026-06-02 — code-verified 26/26, browser-verified 23/26, 2 code-only SKIP (CMP-04/CMP-11), 6 pillar pending Dev environment (post-IT promo v20.0)
