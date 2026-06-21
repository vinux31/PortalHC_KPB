---
phase: 413
slug: test-uat
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-21
finalized: 2026-06-21
---

# Phase 413 — Validation Strategy

> Difinalisasi 2026-06-21 oleh `/gsd-validate-phase 413`.
> 413 = fase Test+UAT — test ITU SENDIRI deliverable-nya (xUnit lifecycle + Playwright e2e 7 sinyal + full regression).
> State-A: semua test sudah ada + hijau — tidak ada test generation baru yang diperlukan.

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) + Playwright 1.58.2 |
| **xUnit quick** | `dotnet test --filter "FullyQualifiedName~FlexibleParticipantLifecycle"` |
| **Full suite** | `dotnet test` (actual: 605/605) |
| **e2e** | `cd tests && npx playwright test flexible-participant-412 --workers=1` (app @5277 AD-off) |
| **Seed** | SEED_WORKFLOW: beforeAll BACKUP → flip sesi 172 InProgress (sqlcmd) → e2e → afterAll RESTORE → SEED_JOURNAL 413 cleaned |

## Hasil Run Aktual (2026-06-21)

| Metrik | Hasil |
|--------|-------|
| `dotnet build` | **0 error** (26 warning pre-existing) |
| `dotnet test` full suite | **Passed: 605, Failed: 0, Skipped: 0** (11m 3s) |
| `--filter FlexibleParticipantLifecycle` spot-confirm (re-run validate) | **Passed: 3, Failed: 0** |
| Integration trait (`Category=Integration`) | **Passed: 197, Failed: 0** — write-path BENAR berjalan (BUKAN skip) |
| e2e `flexible-participant-412` | **5/5 green** (7 sinyal tercakup) |
| Boot @5277 (AD-off) | HTTP 200 — "Application started" |
| `git status Migrations/ Data/` | kosong → migration=FALSE |

## Per-Task Verification Map

| Sinyal / Req | Test Named | File | Type | Hasil |
|---|---|---|---|---|
| PART-05 — add picker → baris muncul live tanpa reload | `add live — picker Tambah → baris muncul live tanpa reload (PART-05)` | `tests/e2e/flexible-participant-412.spec.ts` | Playwright multi-context | ✅ PASS |
| PRMV-02 — modal keras InProgress | `force kick worker — admin hapus InProgress → worker examRemoved + panel removed + count exclude` | `tests/e2e/flexible-participant-412.spec.ts` | Playwright multi-context | ✅ PASS |
| PRMV-02/PRMV-03 — force-kick worker 2-context (#examRemovedModal + redirect) | `force kick worker — admin hapus InProgress → worker examRemoved + panel removed + count exclude` | `tests/e2e/flexible-participant-412.spec.ts` | Playwright 2-context | ✅ PASS |
| PLIV-01 — baris pindah ke #tbodyRemoved live | `force kick worker — admin hapus InProgress → worker examRemoved + panel removed + count exclude` | `tests/e2e/flexible-participant-412.spec.ts` | Playwright | ✅ PASS |
| PRMV-04/D-04 — restore 1-klik → baris balik aktif live | `restore — .btn-restore-peserta 1-klik → baris balik aktif live (PRMV-04/D-04)` | `tests/e2e/flexible-participant-412.spec.ts` | Playwright | ✅ PASS |
| Pitfall-2 — count aktif exclude #tbodyRemoved | `force kick worker — admin hapus InProgress → worker examRemoved + panel removed + count exclude` | `tests/e2e/flexible-participant-412.spec.ts` | Playwright DOM | ✅ PASS |
| PLIV-02 — multi-observer broadcast (admin A+B) | `multi observer — admin A hapus → admin B lihat perubahan live (PLIV-02)` | `tests/e2e/flexible-participant-412.spec.ts` | Playwright 2-context | ✅ PASS |
| Lifecycle add→start→soft-remove→guard-blocked→restore | `Lifecycle_Add_Start_SoftRemove_GuardBlocks_Restore_Active` | `HcPortal.Tests/FlexibleParticipantLifecycleTests.cs` | xUnit integration SQLEXPRESS | ✅ PASS |
| Lifecycle Pre/Post pair add+remove pair-as-unit+restore | `Lifecycle_PrePost_Add_SoftRemoveBoth_RestoreBoth` | `HcPortal.Tests/FlexibleParticipantLifecycleTests.cs` | xUnit integration SQLEXPRESS | ✅ PASS |
| Lifecycle hard-delete row+UPA gone, restore 404/400 | `Lifecycle_Add_NotStarted_HardRemove_RowAndUpaGone` | `HcPortal.Tests/FlexibleParticipantLifecycleTests.cs` | xUnit integration SQLEXPRESS | ✅ PASS |
| Full regression 605/605 — guard 409/391/398.1 no-regress | `dotnet test` (seluruh suite) + per-grup filter | Full suite | `dotnet test` | ✅ PASS |

## Pemetaan 11 REQ → Bukti End-to-End

| REQ | Bukti xUnit (fase) | Bukti e2e (413-02) | Status |
|-----|--------------------|--------------------|--------|
| **PART-05** | Lifecycle L1 (413-01) | sinyal (a) add live | ✅ COVERED |
| **PART-06** | `FlexibleParticipantAddLive` T5/T6/T7/T10 (410) + L1 (413-01) | sinyal (a) | ✅ COVERED |
| **PART-07** | `FlexibleParticipantAddLive` T9 Pre/Post (410) + L2 (413-01) | sinyal (a) | ✅ COVERED |
| **PRMV-01** | `FlexibleParticipantRemove` soft/hard/idempotent (411) + L1/L3 (413-01) | sinyal (b) + (d) | ✅ COVERED |
| **PRMV-02** | (UI/force-kick runtime) | sinyal (b) modal keras + (c) force-kick 2-ctx | ✅ COVERED |
| **PRMV-03** | `ParticipantRemovalGuard` (409) + L1 guard `IsParticipantRemoved` (413-01) | sinyal (c) force-kick + redirect | ✅ COVERED |
| **PRMV-04** | `FlexibleParticipantRemove` restore (411) + L1/L2 restore (413-01) | sinyal (e) restore 1-klik | ✅ COVERED |
| **PRMV-05** | `FlexibleParticipantRemove` Pre/Post pair (411) + L2 (413-01) | sinyal (b/d via pair) | ✅ COVERED |
| **PLIV-01** | `MonitoringRemovedPanel` (412) + `ParticipantRemovalExclude` (409) | sinyal (d) → #tbodyRemoved + (f) count exclude | ✅ COVERED |
| **PLIV-02** | (broadcast regression 412) | sinyal (g) multi-observer A+B | ✅ COVERED |
| **PLIV-03** | `FlexibleParticipantRemove` audit+RBAC (411) | semua aksi via endpoint ber-RBAC | ✅ COVERED |

**11/11 REQ COVERED. 0 gap.**

## De-Tautology (Konfirmasi)

- **xUnit lifecycle:** drive controller `AddParticipantsLive` / `RemoveParticipantLive` / `RestoreParticipantLive` ASLI atas SQLEXPRESS disposable `HcPortalDB_Test_{guid}` → assert kolom DB nyata (`RemovedAt`, `RemovedBy`, `RemovalReason`, `LinkedSessionId`, `Status`, `AnyAsync`). Helper guard `CMPController.IsParticipantRemoved(session)` dipanggil sebagai **public static produksi ASLI** — bukan replica. Grep `SessionHasDataAsync|DeriveReadyStatus|WindowAllowsAddition|\.ExecuteAsync` di luar komentar = **0** (verified 413-01-SUMMARY).
- **Playwright e2e:** real browser @5277 AD-off, real SignalR hub, assert DOM nyata (`#tbodyRemoved`, `#examRemovedModal`, `#panelPesertaDikeluarkan`, count locator). No mock, no hardcode DOM. Bug produk `flashRow is not defined` ditemukan dan diperbaiki oleh e2e real-browser (re-konfirmasi lesson Phase 354 — build+grep tidak cukup).
- **DB assert:** `db.queryScalar` sqlcmd komplemen assertion DOM (soft-remove `RemovedAt IS NOT NULL` + restore clear + add `RemovedAt IS NULL`).

## Per-Grup Guard (No-Regression — semua hijau 2026-06-21)

| Filter | Fase / Cakupan | Hasil |
|--------|----------------|-------|
| `~FlexibleParticipantAdd` | Phase 391 `DeriveReadyStatus` + add 410 | **14/14 Passed** |
| `~ParticipantRemovalGuard` | Phase 409 guard re-entry (StartExam/SubmitExam/JoinBatch) | **5/5 Passed** |
| `~ParticipantRemovalExclude` | Phase 409 exclude-removed (list/count aktif) | **3/3 Passed** |
| `~FlexibleParticipantRemove` | Phase 411 remove hybrid + restore + Pre/Post + audit/RBAC | **16/16 Passed** |
| `~MonitoringRemovedPanel` | Phase 412 panel "Peserta Dikeluarkan" | **5/5 Passed** |
| `~MonitoringUserStatus` | Phase 412 status badge/broadcast | **7/7 Passed** |
| `~FlexibleParticipantLifecycle` | Phase 413-01 BARU (lintas-fase) | **3/3 Passed** |

## Wave 0 Requirements (semua terpenuhi)

- [x] `tests/e2e/flexible-participant-412.spec.ts` — 7 sinyal multi-context (PART-05, PRMV-02 ×2, PLIV-01, PRMV-04, Pitfall-2, PLIV-02) — commit `57c1971d` + `a4316fe7` + `71a09ac9`
- [x] Seed InProgress reliable: per-spec `test.beforeAll` BACKUP + sqlcmd flip + `test.afterAll` RESTORE (isolasi penuh, TIDAK bergantung global teardown matrix)
- [x] `HcPortal.Tests/FlexibleParticipantLifecycleTests.cs` — lifecycle lintas-fase 3 [Fact] — commit `e43025ce`
- [x] Product fix `monFlashRow` ReferenceError — commit `c13fdd22` (view-only, build 0 error, e2e 5/5 pasca-fix)

## Manual-Only

| Behavior | Why |
|----------|-----|
| (Tidak ada) | 413 = otomatisasi penuh; e2e Playwright = UAT terotomasi (real browser, real SignalR, bukan manual). |

## Temuan Bernilai dari Fase Ini

1. **Bug produk `flashRow is not defined`** — ReferenceError lintas `<script>` block yang membatalkan SELURUH handler picker/hapus/restore di `AssessmentMonitoringDetail.cshtml`. Hanya terdeteksi via e2e real-browser; 412-02 "runtime smoke" tidak menangkapnya. Fix: pindah `window.monFlashRow = flashRow;` ke blok `@section Scripts` (`c13fdd22`).
2. **Seed `global.setup` matrix bisa salah-pilih**: filter `Title NOT LIKE '[[]MATRIX[_]TEST%'` wajib di query seed spec ini agar tidak menimpa/salah-pilih sesi matrix sentinel.

## Deferred / Carry

| Item | Status |
|------|--------|
| **IN-02** — EditAssessment belum exclude soft-removed | Tetap backlog (UAT 413 tidak temukan inkonsistensi) |
| **3 Info Phase 412** | Deferred, non-blocking |
| **A2** export/impact exclude-removed | Deferred (by-design) |
| **Carry notify IT** | Phase 409 migration=TRUE `AddParticipantRemovalColumns` hash `01cd7dd0`; Phase 410-413 migration=FALSE. Push = aksi koordinasi IT terpisah. |

## Commits Fase 413

| Hash | Deskripsi |
|------|-----------|
| `e43025ce` | test(413-01): cross-phase lifecycle integration (+553 baris, 3 [Fact]) |
| `57c1971d` | test(413-02): scaffold spec e2e + seed SQL |
| `a4316fe7` | test(413-02): 7 sinyal multi-context |
| `c13fdd22` | fix(413-02): hoist monFlashRow ke @section Scripts (product bug) |
| `71a09ac9` | test(413-02): robustness — dropdown toggle + panel collapse + matrix exclude + journal |
| `2b6f45d1` | docs(413-03): regression gate + 11-REQ mapping |

---

## Verdict Akhir

**nyquist_compliant: true — 0 gaps**

- 11/11 REQ terpetakan ke test named + dijalankan hijau.
- 7 sinyal e2e handoff 412 semua tercakup (5/5 e2e, 7 sinyal).
- 3 xUnit lifecycle lintas-fase: 3/3 Passed (spot-confirm re-run validate 2026-06-21).
- Full suite 605/605 (Failed: 0) — 0 regresi guard 409/391/398.1 + 410/411/412.
- De-tautology: helper produksi ASLI + kolom DB nyata + real browser DOM.
- DB lokal bersih: SEED_JOURNAL 413 = cleaned; sesi 172 = Open baseline.
- **Milestone v32.5: SIAP-SHIP, PENDING-PUSH** (push + notify IT = aksi koordinasi terpisah).
