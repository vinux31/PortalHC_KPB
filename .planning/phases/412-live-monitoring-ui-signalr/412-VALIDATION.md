---
phase: 412
slug: live-monitoring-ui-signalr
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-21
finalized: 2026-06-21
---

# Phase 412 — Validation Strategy

> Difinalisasi saat `/gsd-validate-phase 412`. Sumber: 412-RESEARCH.md §Validation Architecture.
> **Catatan:** mayoritas 412 = UI live + SignalR DOM → **Playwright-verified** (e2e lengkap = Phase 413).
> 412 = unit/integration untuk sinyal assertable + smoke runtime + handoff list 413.

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) + Playwright (e2e) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~MonitoringRemovedPanel"` |
| **Full suite command** | `dotnet test` |
| **e2e** | Playwright @localhost:5277 (AD-off) — smoke runtime sudah 412 (03-SUMMARY); lengkap = 413 |

## Per-Task Verification Map (FINAL)

| Req | Signal | Test Type | Automated Command | Status |
|-----|--------|-----------|-------------------|--------|
| PLIV-01 | Action `AssessmentMonitoringDetail` query `RemovedAt!=null` → `ViewBag.RemovedSessions` (panel data) | unit — InMemory real-controller | `dotnet test --filter MonitoringRemovedPanel` | ✅ green (5/5) |
| PLIV-01 | `RemovedParticipantViewModel.FullName` dari User navigation (`.Include(a=>a.User)`) | unit — M2 | id atas | ✅ green |
| PLIV-01 | `RemovedParticipantViewModel.RemovedByName` resolved dari removerMap (bukan raw userId) | unit — M3 | id atas | ✅ green |
| PLIV-01 | Panel kosong bila tidak ada removed (RemovedAt!=null count=0) | unit — M4 | id atas | ✅ green |
| PLIV-01 | Cross-batch isolation: removed batch lain tidak masuk panel batch ini | unit — M5 | id atas | ✅ green |
| PLIV-02 | Broadcast `participantAdded`/`participantRemoved` post-commit — endpoint tidak crash + kontrak JSON utuh | regression (write-path via NoopHubContext) | `dotnet test --filter FlexibleParticipant` | ✅ green (regression) |
| (regression) | `DeriveUserStatus` 6-cabang tetap benar | unit | `dotnet test --filter MonitoringUserStatus` | ✅ green (existing) |
| (regression) | 410/411 endpoint contract tak berubah setelah +broadcast | integration | `dotnet test --filter FlexibleParticipant` | ✅ green (existing) |
| (runtime smoke) | `AssessmentMonitoringDetail` @5277 render 200 + seluruh simbol UI (btn, modal, panel, handler) | runtime smoke | 412-02-SUMMARY §Runtime Smoke | ✅ done (Task 3 412-02) |
| (runtime smoke) | `StartExam` @5277 — `#examRemovedModal` di DOM + handler `examRemoved` ter-register + Razor compile | runtime smoke Playwright | 412-03-SUMMARY §Verification | ✅ done (Task 2 412-03) |
| **PART-05** | Add picker → baris muncul live tanpa reload (DOM + SignalR 2-context) | Playwright e2e lengkap | `npx playwright test flexible-participant-412 -g "add"` | 🕐 DEFERRED 413 |
| **PRMV-02** | Modal keras InProgress + force-kick redirect live 2-context | Playwright e2e lengkap | `npx playwright test flexible-participant-412 -g "kick"` | 🕐 DEFERRED 413 |
| **PLIV-01** | Panel "Peserta Dikeluarkan" render + Restore 1-klik live | Playwright e2e lengkap | `npx playwright test flexible-participant-412 -g "panel"` | 🕐 DEFERRED 413 |
| **PLIV-02** | Add/remove tersiar live ke semua pemantau (multi-context SignalR) | Playwright e2e lengkap | `npx playwright test flexible-participant-412` | 🕐 DEFERRED 413 |
| (pitfall) | `updateSummaryFromDOM` exclude `#tbodyRemoved` (count aktif benar) | Playwright e2e DOM | id atas | 🕐 DEFERRED 413 |
| (T-409-10) | XSS RemovalReason via `textContent`/`Html.Encode` | review (0C 412-REVIEW) + Playwright | — | ✅ review passed (412-REVIEW §T-412-08: 0 Html.Raw verified; JS textContent verified) |

## Gap yang Ditutup oleh Validasi Ini

### Gap sebelum validasi
Draft 412-VALIDATION.md mempunyai seluruh baris `⬜` (belum ter-cover). Gap assertable yang ditemukan:

**Gap 1 (assertable — CLOSED):** `ViewBag.RemovedSessions` query di `AssessmentMonitoringDetail` belum ada test.
- `ParticipantRemovalExcludeTests.MonitoringDetail_Counts_ExcludeRemoved` (Phase 409) hanya mengecek `TotalCount`/`InProgressCount` dari model query utama (`RemovedAt==null`) — TIDAK mengecek `ViewBag.RemovedSessions` (query kedua `RemovedAt!=null` + resolve FullName/RemovedByName).
- **Fix:** 5 test baru `MonitoringRemovedPanelTests` (M1–M5) menutup gap ini. Commit `7ba38136`.

**Gap 2 (PLIV-02 broadcast ordering — PARTIAL → runtime-level):** Tidak ada test xUnit yang assert `_hubContext.SendAsync("participantAdded",...)` dipanggil dengan payload tepat. Keputusan: dicover di tingkat regression (NoopHubContext — endpoint tidak crash + JSON contract utuh) + live multi-context = 413. Ini diterima per `412-RESEARCH.md §Wave 0 Gaps` ("kalau di-skip, e2e/UAT 413 yang cover").

## Deferred to Phase 413 e2e (Handoff List)

Sinyal-sinyal berikut secara eksplisit **diserahkan ke Phase 413** untuk verifikasi end-to-end Playwright multi-context. Semua infrastruktur (endpoint, handler JS, modal, panel, broadcast) sudah live dan ter-smoke di runtime. Yang belum dilakukan = observasi perilaku DOM live di browser nyata.

| Behavior | Why Deferred | 413 Test Name (draft) |
|----------|-------------|----------------------|
| Add dari picker → baris baru muncul **live** di tabel aktif **tanpa reload** (PART-05) | Butuh SignalR multi-context (admin A klik Tambah, admin B lihat baris muncul) — tak bisa unit | `flexible-participant-412 -g "add live"` |
| Modal keras muncul untuk peserta **InProgress** (PRMV-02 client-side) | DOM `data-status="InProgress"` + `data-bs-backdrop="static"` di browser nyata | `flexible-participant-412 -g "hapus keras"` |
| Worker InProgress terima `examRemoved` → modal `#examRemovedModal` muncul + countdown + redirect (PRMV-02 force-kick live) | Butuh 2-context: admin + worker di browser berbeda | `flexible-participant-412 -g "force kick worker"` |
| Baris ter-hapus pindah ke panel `#panelPesertaDikeluarkan` live (PLIV-01 UI) | DOM move `participantRemoved` handler — SignalR runtime | `flexible-participant-412 -g "panel removed"` |
| Tombol Restore 1-klik → baris balik ke tabel aktif live (D-04) | SignalR `participantAdded` + DOM restore-from-panel | `flexible-participant-412 -g "restore"` |
| `updateSummaryFromDOM` exclude `#tbodyRemoved` — count aktif turun 1 setelah hapus (Pitfall 2) | Perlu hapus live + verif counter DOM | bagian "hapus keras" + "panel removed" |
| Tambah/hapus tersiar ke **semua pemantau** (PLIV-02 multi-observer) | Multi-context admin A + admin B + worker | `flexible-participant-412 -g "multi observer"` |

**Scope 413:** `tests/e2e/flexible-participant-412.spec.ts` (baru) — Playwright multi-context, app @5277, AD-off, seed batch InProgress via SEED_WORKFLOW (snapshot/restore).

## De-Tautology
- **Sinyal assertable** (removedSessions query, model count exclude removed, DeriveUserStatus): drive controller ASLI + LINQ produksi atas InMemory real context. Tidak ada replica predikat.
- **Broadcast PLIV-02**: NoopHubContext (menelan SendAsync) membuktikan endpoint tidak NRE saat broadcast + JSON contract utuh. Full assert payload = 413.
- **UI live + SignalR**: Playwright real browser (UAT 412 runtime smoke sudah; e2e lengkap 413). Lesson Phase 354 diikuti: build+grep tak cukup untuk Razor/JS dinamis → runtime smoke WAJIB per phase.

## Wave 0 Requirements — COMPLETE

- [x] `MonitoringRemovedPanelTests` (5 unit) — `ViewBag.RemovedSessions` query assertable (M1–M5). ✅
- [x] Regression FlexibleParticipant (38 test) + MonitoringUserStatus (7 test) hijau. ✅
- [x] Runtime smoke @5277: `AssessmentMonitoringDetail` 200 + `StartExam` handler+modal ter-load. ✅ (412-02/03 SUMMARY)
- [x] Full suite 602/602 (sebelumnya 597/597 + 5 baru). ✅
- [x] Live e2e multi-context → terdaftar secara eksplisit untuk 413. ✅

## Manual-Only / Playwright (Deferred 413)

| Behavior | Why |
|----------|-----|
| Add live row, remove modal tiers (keras/ringan), force-kick worker, panel+Restore live | Live DOM/SignalR → Playwright real browser multi-context (Phase 413) |
| `updateSummaryFromDOM` exclude count via DOM | Perlu browser + SignalR runtime |
| XSS textContent (runtime render) | Runtime browser (static review sudah pass 412-REVIEW) |

## Suite Summary

| Suite | Count | Status |
|-------|-------|--------|
| `MonitoringRemovedPanelTests` (NEW — 412 Nyquist) | 5 | ✅ green |
| `FlexibleParticipantAddLive*` + `FlexibleParticipantRemove*` | 33 | ✅ green |
| `MonitoringUserStatus*` | 7 | ✅ green |
| `ParticipantRemovalExclude*` + `ParticipantRemovalGuard*` (SQLEXPRESS) | 11 | ✅ green |
| **Full fast suite** | **602/602** | ✅ green |

*Runtime smoke (Playwright 1-context): 412-02 Task 3 PASS + 412-03 Task 2 PASS — sebelum validate-phase.*

---
*Phase: 412-live-monitoring-ui-signalr*
*Finalized: 2026-06-21 by /gsd-validate-phase 412*
*Nyquist commit: `7ba38136` (MonitoringRemovedPanelTests, 5 unit)*
