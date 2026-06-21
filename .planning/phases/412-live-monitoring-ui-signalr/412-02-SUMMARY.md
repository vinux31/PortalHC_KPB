---
phase: 412-live-monitoring-ui-signalr
plan: 02
subsystem: ui
tags: [aspnet-mvc, razor, bootstrap5, signalr-client, vanilla-js, modal, collapsible-panel, xss-safe, ajax]

# Dependency graph
requires:
  - phase: 412-01-monitoring-signalr-broadcast
    provides: "Broadcast participantAdded/participantRemoved (mode hard/soft) + ViewBag.RemovedSessions (List<RemovedParticipantViewModel> typed)"
  - phase: 410-add-participant-backend-live
    provides: "GetEligibleParticipantsToAdd (GET JSON [{id,fullName,nip}]) + AddParticipantsLive (POST added[]/skipped[])"
  - phase: 411-remove-restore-backend-live
    provides: "RemoveParticipantLive (POST {sessionId,mode,linkedSessionId}; 400 reason-wajib) + RestoreParticipantLive (POST {sessionId,restored})"
provides:
  - "UI live add/remove/restore di AssessmentMonitoringDetail.cshtml: picker Tambah Peserta, modal hapus keras/ringan (D-01), panel collapsible Peserta Dikeluarkan + Restore 1-klik (D-04)"
  - "Handler SignalR participantAdded (dedup + restore-from-panel) / participantRemoved (mode-aware hard remove vs soft→panel) — sinkron live tanpa reload"
  - "buildActionsHtml diperluas (inject tombol Hapus utk baris SignalR) + updateSummaryFromDOM exclude #tbodyRemoved (Pitfall 2)"
  - "Helper IIFE diekspos ke window.mon* agar handler @section Scripts (blok terpisah) bisa pakai buildActionsHtml/flashRow/statusBadgeClass"
affects: [413-test-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Ekspos helper IIFE-private ke window.mon* untuk dijembatani ke blok @section Scripts (window.assessmentHub hanya tersedia di @section)"
    - "Inject baris DOM via createElement + textContent (XSS-safe) BUKAN innerHTML untuk field user-input (nama/alasan) — T-409-10 carry"
    - "Fallback timeout 3 detik: bila echo SignalR tak datang setelah POST sukses, inject dari response (dedup by sessionId)"
    - "Tingkat modal hapus ditentukan dari data-status (label-aware Pitfall 1): InProgress/Completed-cert → keras, lainnya → ringan"

key-files:
  created:
    - ".planning/phases/412-live-monitoring-ui-signalr/412-02-SUMMARY.md"
  modified:
    - "Views/Admin/AssessmentMonitoringDetail.cshtml (+731: markup tombol+modal+panel, JS picker/hapus/restore, 2 handler SignalR, perluas buildActionsHtml/updateSummaryFromDOM)"

key-decisions:
  - "Helper buildActionsHtml/flashRow/statusBadgeClass diekspos via window.mon* — handler SignalR ada di @section Scripts (blok JS terpisah; window.assessmentHub hanya tersedia di sana) sementara helper IIFE-private di blok atas. Ekspos = cara terbersih lintas-blok (alternatif re-implement inline rawan drift)"
  - "Picker + modal hapus + restore + chevron diletakkan di IIFE blok-atas (akses penuh helper + hCategory/REP_ID); HANYA handler SignalR di @section (butuh window.assessmentHub)"
  - "Fallback inject 3 detik (window.monInjectParticipantRow) dibatalkan via window.monClearAddedFallback saat echo datang — anti double-inject (Pitfall 4)"
  - "Panel SELALU render utk non-Proton (Pitfall 5); display:none saat removedList.Count==0, JS set display='' saat removed pertama"

patterns-established:
  - "window.mon* bridge: helper IIFE-private diekspos ke window agar blok @section Scripts lain bisa konsumsi"
  - "monInjectParticipantRow: factory baris peserta DOM (createElement + textContent), reusable handler participantAdded + fallback POST"

requirements-completed: [PART-05, PRMV-02, PLIV-01, PLIV-02]

# Metrics
duration: 40min
completed: 2026-06-21
---

# Phase 412 Plan 02: Live Monitoring UI + SignalR DOM Handlers Summary

**Seluruh kontrol UI live di `AssessmentMonitoringDetail.cshtml` — picker "Tambah Peserta" → AddParticipantsLive, item Hapus per-baris + modal konfirmasi keras/ringan (D-01) → RemoveParticipantLive, panel collapsible "Peserta Dikeluarkan" + Restore 1-klik (D-04) → RestoreParticipantLive, dan handler SignalR participantAdded/participantRemoved (dedup, mode-aware, XSS-safe) yang sinkron live tanpa reload. Mengonsumsi broadcast + ViewBag.RemovedSessions dari Plan 01. migration=FALSE.**

## Performance
- **Duration:** ~40 min
- **Completed:** 2026-06-21
- **Tasks:** 3
- **Files modified:** 1 (`Views/Admin/AssessmentMonitoringDetail.cshtml`, +731 baris)

## Accomplishments
- **Tombol + modal picker "Tambah Peserta"** (`#btnTambahPeserta` / `#tambahPesertaModal`): `show.bs.modal` → fetch `GetEligibleParticipantsToAdd?sessionId={REP_ID}` → checklist multi-select (loading/empty/error states) → `#btnKonfirmasiTambah` POST `AddParticipantsLive` (userIds[] repeated form-field + antiforgery). Sumber kebenaran baris = SignalR `participantAdded`; fallback inject 3 detik dari `added[]` (dedup). Guard `@Model.Category != "Assessment Proton"`.
- **Item "Hapus Peserta" per-baris** (`.btn-hapus-peserta`, data-status/data-has-cert) di dropdown ⋮ + di `buildActionsHtml` (baris SignalR). **Modal keras/ringan (D-01):** delegated click tentukan tingkat dari `data-status` (Pitfall 1: `InProgress` ATAU `Completed`+cert → KERAS dengan `data-bs-backdrop=static` + warning block; lainnya → RINGAN). Field alasan SELALU tampil + hint "wajib bila peserta sudah mengerjakan" (D-03). POST `RemoveParticipantLive`; HTTP 400 reason-wajib → error inline (modal tak ditutup, server penjaga akhir).
- **Panel "Peserta Dikeluarkan"** (`#panelPesertaDikeluarkan` collapsible, border-left merah): render server-side `ViewBag.RemovedSessions` (nama/NIP/waktu/oleh/alasan via Razor auto-encode XSS-safe) + tombol Restore. **Restore 1-klik (D-04, no confirm)** → POST `RestoreParticipantLive`; baris balik via `participantAdded`. Chevron rotate on collapse.
- **Handler SignalR `participantAdded`** (mirror workerStarted): dedup `tbody:not(#tbodyRemoved) tr[data-session-id]` → kasus restore (hapus dari `#tbodyRemoved` + dec countRemoved) → inject baris baru via `monInjectParticipantRow` (createElement + `textContent` nama, badge via `monStatusBadgeClass`, aksi via `monBuildActionsHtml`) → flashRow + updateSummaryFromDOM + toast "ditambahkan ke batch".
- **Handler SignalR `participantRemoved`** (mode-aware Pitfall 6): `mode==='hard'` → `tr.remove()` (tak masuk panel); soft → bangun 5-kolom baris removed (**Alasan/Nama/Oleh via `textContent` — XSS-safe, Pitfall 3**), `prepend` ke `#tbodyRemoved`, inc countRemoved, tampilkan panel, hapus baris aktif → updateSummaryFromDOM + toast "dikeluarkan dari batch".
- **`updateSummaryFromDOM` diubah** (Pitfall 2): selector `tbody tr[data-session-id]` → `tbody:not(#tbodyRemoved) tr[data-session-id]` (count aktif exclude removed, selaras query 409).

## Task Commits
1. **Task 1: Markup tombol+modal+panel** - `f2781740` (feat)
2. **Task 2+3: JS picker/hapus/restore + handler SignalR participantAdded/Removed** - `d6ca5765` (feat)

_Catatan: Task 2 (JS interaktif) dan Task 3 (handler SignalR) digabung dalam satu commit karena keduanya menyentuh region JS yang sama dalam satu file dan tak terpisahkan di working tree tanpa interactive-add (tak didukung). Pesan commit mencakup keduanya. Runtime smoke Task 3 = verifikasi (bukan perubahan file)._

## Files Created/Modified
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — (1) markup: `#btnTambahPeserta` di card-header, `.btn-hapus-peserta` di dropdown, `#tambahPesertaModal`/`#hapusPesertaLightModal`/`#hapusPesertaHardModal`, panel `#panelPesertaDikeluarkan`+`#tbodyRemoved` render `ViewBag.RemovedSessions`. (2) JS blok-atas IIFE: picker fetch+POST, modal hapus delegated (keras/ringan), restore 1-klik, chevron, perluas `buildActionsHtml`, ubah `updateSummaryFromDOM`, ekspos `window.mon*`. (3) JS @section Scripts: `monInjectParticipantRow` + handler `participantAdded`/`participantRemoved` + helper format/count.

## Decisions Made
- **window.mon* bridge** — `buildActionsHtml`/`flashRow`/`statusBadgeClass`/`statusDisplayLabel`/`isPackageMode` di-IIFE-private blok-atas, sedangkan handler SignalR WAJIB di `@section Scripts` (satu-satunya tempat `window.assessmentHub` tersedia setelah `assessment-hub.js` load). Ekspos ke `window.mon*` = jembatan lintas-blok terbersih (alternatif: re-implement row-logic inline seperti `workerSubmitted` existing — rawan drift, ditolak).
- **Picker/hapus/restore di blok-atas** (bukan @section) — fetch POST tak butuh hub, dan blok-atas punya akses langsung `hCategory`/`REP_ID`/helper. Hanya 2 handler SignalR yang di @section.
- **Tingkat modal label-aware (Pitfall 1)** — cek `data-status === "InProgress"` (English literal, aman) ATAU (`"Completed"` && has-cert). `"Dibatalkan"`/`"Menunggu Penilaian"`/`"Not started"`/`"Abandoned"` → ringan. `data-status` diisi dari `@session.UserStatus` (hasil DeriveUserStatus).
- **Fallback 3 detik + cancel** — `monInjectParticipantRow` di-schedule 3s setelah POST sukses; `monClearAddedFallback` membatalkan saat echo SignalR datang (anti double-inject Pitfall 4).

## Deviations from Plan
None - plan dieksekusi sesuai tulisan. Semua markup verbatim UI-SPEC (kelas Bootstrap, copywriting, warna), semua endpoint/payload sesuai kontrak 410/411 SUMMARY, semua pitfall dimitigasi (label-aware modal, summary exclude removed, textContent XSS-safe, dedup echo, panel selalu render, mode-aware hard/soft).

Catatan implementasi (bukan deviasi): Task 2 + Task 3 digabung dalam satu commit (`d6ca5765`) karena tak terpisahkan di working tree (region JS sama, satu file) — lihat Task Commits.

## Threat Surface
Semua mitigasi threat register diterapkan:
- **T-412-08 (Stored XSS):** Razor `@`-auto-encode di panel server-side (JANGAN Html.Raw — verified 0 Html.Raw) + JS `.textContent` untuk nama/alasan/oleh (verified 0 innerHTML utk reason/fullName).
- **T-412-09 (CSRF):** `getToken()` kirim `__RequestVerificationToken` di semua POST (Add/Remove/Restore); endpoint `[ValidateAntiForgeryToken]` server.
- **T-412-10/11 (Elevation/Tampering):** UI render hanya utk Admin/HC; server re-validasi eligible + RBAC (410/411 server-authoritative).
- **T-412-13 (double-inject):** dedup `querySelector('tbody:not(#tbodyRemoved) tr[data-session-id]')` sebelum inject + cancel fallback saat echo (Pitfall 4).

Tidak ada threat surface baru di luar register.

## Issues Encountered
- **Background `dotnet run` exit code 1** — akibat `taskkill //F //PID` manual untuk menghentikan app setelah runtime smoke selesai, BUKAN boot failure. App booted sukses ("Now listening on: http://localhost:5277" + "Application started"), melayani semua request (login 302, detail 200, eligible 200) tanpa error di log. Pola identik 410-01.

## Runtime Smoke @5277 (Task 3 — WAJIB, lesson Phase 354)
App di-launch `Authentication__UseActiveDirectory=false ... --urls http://localhost:5277` (booted 14s). Login `admin@pertamina.com`/`123456` (bootstrap admin SeedData, RoleLevel 1) → 302. GET `/Admin/AssessmentMonitoringDetail?title=Quiz Random&category=OJT&scheduleDate=2026-07-10` → **HTTP 200, 103KB**. Verifikasi server-rendered HTML (membuktikan Razor dinamis + JS ter-load tanpa error):
- `#btnTambahPeserta` x1, `#tambahPesertaModal` x5, `.btn-hapus-peserta` x3 (3 baris), `#hapusPesertaHardModal`/`#hapusPesertaLightModal` x3, `#panelPesertaDikeluarkan` x8, `#tbodyRemoved` x12, `data-has-cert` x3 (per-baris) — SEMUA render.
- Handler `participantAdded`/`participantRemoved` + `monBuildActionsHtml` ter-ekspos di output.
- `GetEligibleParticipantsToAdd?sessionId=161` → **HTTP 200** JSON `[{id,fullName,nip}]` valid (picker akan populate).
- Panel wrapper `style="...#dc3545 !important; display:none;"` saat `countRemoved=0` (Pitfall 5: panel selalu di DOM, hidden sampai removed pertama).
- **NOL error page** (`grep "Unhandled exception|Developer Exception"` = 0) + **NOL error server-side di log** selama request.
- Batch dengan soft-removed tidak ada di DB lokal saat ini → path panel-berbaris diverifikasi via Razor `@foreach` compile-clean (build 0 error). Full e2e multi-tab broadcast + force-kick + panel-berbaris = Phase 413.

## User Setup Required
None - tidak ada konfigurasi external service. migration=FALSE (view-only; kolom removal sudah dari Phase 409). Branch main, NOT pushed (deploy bundle v32.5 oleh IT — carry migration=TRUE Phase 409 hash `01cd7dd0`).

## Next Phase Readiness
- **Plan 412-03** (StartExam force-kick): siap — handler `examRemoved` (reason) + modal `#examRemovedModal` di `Views/CMP/StartExam.cshtml`, mirror `examClosed`.
- **Phase 413** (Test + UAT lengkap): siap — Playwright multi-tab (admin add live → baris muncul tab lain; remove keras → force-kick worker; restore → baris balik; panel-berbaris render). Semua endpoint+handler ter-wire, kontrak JSON verified live.
- Build 0 error; runtime smoke @5277 PASS; migration=FALSE.

## Self-Check: PASSED

- FOUND: `Views/Admin/AssessmentMonitoringDetail.cshtml` (semua simbol render live: btnTambahPeserta, tambahPesertaModal, btn-hapus-peserta, hapusPesertaHard/LightModal, panelPesertaDikeluarkan, tbodyRemoved, participantAdded, participantRemoved)
- FOUND: `.planning/phases/412-live-monitoring-ui-signalr/412-02-SUMMARY.md`
- FOUND commit: `f2781740` (Task 1 markup)
- FOUND commit: `d6ca5765` (Task 2+3 JS + handlers)
- Build: succeeded, 0 error · Runtime smoke @5277: page 200 + eligible 200 + 0 console/server error · migration=FALSE · NOT pushed

---
*Phase: 412-live-monitoring-ui-signalr*
*Completed: 2026-06-21*
