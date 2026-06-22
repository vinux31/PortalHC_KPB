---
phase: 412-live-monitoring-ui-signalr
verified: 2026-06-21T09:30:00Z
status: passed
human_verification_closed: 2026-06-21 (Phase 413 e2e flexible-participant-412.spec.ts 2-context 7 signals 5/5)
score: 5/5
overrides_applied: 0
human_verification:
  - test: "Tambah peserta dari Monitoring Detail — baris muncul live"
    expected: "Setelah klik Tambah Peserta + pilih user + klik Konfirmasi, baris peserta baru muncul di tabel aktif tanpa reload. Admin/HC di tab lain juga melihat baris masuk via SignalR participantAdded."
    why_human: "SignalR multi-context live behavior tidak bisa diverifikasi programatik tanpa Playwright multi-tab (dijadwalkan Phase 413). Runtime smoke hanya verifikasi DOM render + handler ter-register, bukan alur AJAX end-to-end."
  - test: "Hapus peserta (modal keras untuk InProgress/Completed-cert) — baris pindah ke panel"
    expected: "Klik tombol Hapus di baris InProgress -> modal keras muncul (backdrop static) -> isi alasan -> konfirmasi -> baris hilang dari tabel aktif dan muncul di panel Peserta Dikeluarkan."
    why_human: "Interaksi modal dua-tier (keras vs ringan) dan animasi pindah baris memerlukan Playwright human-or-e2e; runtime smoke 412-02 tidak melatih alur hapus end-to-end."
  - test: "Worker yang sedang ujian menerima force-kick (examRemoved)"
    expected: "Admin hapus peserta InProgress dari Monitoring Detail -> worker di StartExam.cshtml menerima examRemoved -> UI terkunci (timer berhenti) -> modal Dikeluarkan dari Ujian muncul -> redirect ke daftar Assessment dalam 5 detik."
    why_human: "Memerlukan 2 browser context (admin + worker simultan). Runtime smoke 412-03 hanya verifikasi modal ter-render di DOM + handler ter-register tanpa trigger live SignalR dari backend."
  - test: "Restore peserta dari panel — baris kembali ke tabel aktif"
    expected: "Klik Restore di panel Peserta Dikeluarkan -> baris pindah kembali ke tabel aktif (via SignalR participantAdded) -> panel tersembunyi jika tidak ada sisa removed."
    why_human: "Alur restore 1-klik + echo SignalR perlu verifikasi live; hanya teruji via kode path review + runtime smoke 412-01."
---

# Phase 412: Live Monitoring UI + SignalR — Verification Report

**Phase Goal:** Layar Monitoring Detail mendapat kontrol live Tambah & Hapus peserta yang menyiarkan perubahan real-time tanpa reload; peserta aktif yang dihapus langsung di-force-kick via SignalR examRemoved; perubahan tersiar ke semua Admin/HC pemantau; sesi soft-removed dikecualikan dari count aktif dan tampil di panel "Peserta Dikeluarkan" dengan tombol Restore.

**Verified:** 2026-06-21T09:30:00Z
**Status:** PASSED — 4/4 truths verified; 4 live-runtime items RESOLVED by Phase 413 e2e (flexible-participant-412.spec.ts, 2-context, 7 signals, 5/5 green). Human-verification gate closed 2026-06-21 (per milestone audit §2).
**Re-verification:** No — initial verification.

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|---------|
| 1 | Admin/HC dapat menambah peserta dari Monitoring Detail + baris muncul live tanpa reload (PART-05) | VERIFIED | `#btnTambahPeserta` + `#tambahPesertaModal` ter-render di view (non-Proton guard line 193). AJAX fetch ke `GetEligibleParticipantsToAdd` + POST ke `AddParticipantsLive` terwire di JS. Handler `participantAdded` ter-register di `@section Scripts`. `monInjectParticipantRow` factory baris XSS-safe via `textContent`. Dedup by sessionId. Fallback 3-detik bila echo SignalR tak datang. |
| 2 | Hapus peserta aktif memerlukan konfirmasi keras; peserta di-force-kick via SignalR examRemoved (PRMV-02) | VERIFIED | Modal keras `#hapusPesertaHardModal` (backdrop=static) + modal ringan `#hapusPesertaLightModal`. `data-status` + `data-has-cert` di setiap baris; JS tentukan modal level dari status InProgress atau Completed+cert (Pitfall 1 dimitigasi). Handler `examRemoved` di StartExam.cshtml ter-register, reuse guard `examClosed`, redirect ke `@Url.Action("Assessment","CMP")`. Grep 7/7 acceptance criteria PASS pada 412-03. `wasInProgress` di-capture sebelum core mutasi (line 2593). |
| 3 | Sesi soft-removed dikecualikan dari count aktif + ditampilkan di panel "Peserta Dikeluarkan" dengan Restore (PLIV-01) | VERIFIED | `updateSummaryFromDOM` menggunakan `tbody:not(#tbodyRemoved) tr[data-session-id]` (Pitfall 2 dimitigasi). Panel `#panelPesertaDikeluarkan` + `#tbodyRemoved` ter-render server-side dari `ViewBag.RemovedSessions` (typed `RemovedParticipantViewModel`). Kolom nama/waktu/oleh/alasan di-encode via Razor `@` (XSS-safe). Restore 1-klik POST ke `RestoreParticipantLive`. Handler `participantAdded` menangani kasus restore (hapus dari `#tbodyRemoved`, inject ulang ke aktif). |
| 4 | Penambahan & penghapusan tersiar live ke semua Admin/HC pemantau via SignalR post-commit (PLIV-02) | VERIFIED | `AddParticipantsLive` broadcast `participantAdded` ke `monitor-{batchKey}` HANYA setelah `transaction.CommitAsync()` (line 2459 commit, line 2488 broadcast). `RemoveParticipantLive` broadcast `participantRemoved` (mode hard/soft) + event KEDUA untuk Pre/Post partner (line 2629). `RestoreParticipantLive` broadcast `participantAdded` post-`SaveChangesAsync`. `examRemoved` via `_hubContext.Clients.User(targetUserId)` (line 2642). Semua broadcast menggunakan grup `monitor-{batchKey}` konsisten dengan pola existing. |
| 5 | Build 0 error + runtime smoke @5277 PASS (SC-5) | VERIFIED | Summary 412-01 task 3: suite 597/597 (`8623e68e`). Summary 412-02: runtime smoke 200 OK + semua simbol DOM ter-render. Summary 412-03: Playwright smoke 1/1 PASS, handler+modal+Razor ter-kompilasi. migration=FALSE dikonfirmasi `git status Migrations/`. |

**Score:** 4/4 truths programatik verified (+ 1 infrastructure verified). Human verification diperlukan untuk 4 alur live runtime.

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Controllers/AssessmentAdminController.cs` | Broadcast post-commit di AddParticipantsLive / RemoveParticipantLive / RestoreParticipantLive + query removedSessions | VERIFIED | Broadcast `participantAdded` line 2488; `participantRemoved` line 2616+2629; `examRemoved` line 2642; query `removedSessions` + `ViewBag.RemovedSessions` line 4030-4042. |
| `Models/AssessmentMonitoringViewModel.cs` | RemovedParticipantViewModel typed (Id/FullName/Nip/RemovedAt/RemovedByName/RemovalReason) | VERIFIED | Class ada line 75-83, semua field lengkap, komentar XSS encode-at-render. |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` | Picker Tambah, modal hapus keras/ringan, panel Peserta Dikeluarkan, handler participantAdded/participantRemoved | VERIFIED | +731 baris ditambah. Semua ID markup terkonfirmasi: `#btnTambahPeserta`, `#tambahPesertaModal`, `#hapusPesertaHardModal`, `#hapusPesertaLightModal`, `#panelPesertaDikeluarkan`, `#tbodyRemoved`. Handler SignalR `participantAdded`/`participantRemoved` di `@section Scripts`. |
| `Views/CMP/StartExam.cshtml` | Handler examRemoved + modal #examRemovedModal (force-kick worker) | VERIFIED | Modal `#examRemovedModal` ada (non-dismissable, backdrop=static, z-index:9999). Handler `examRemoved` line 1319, teks verbatim "Anda telah dikeluarkan dari ujian ini." line 396. Reason via `.textContent` (XSS-safe). Redirect ke daftar Assessment. |
| `HcPortal.Tests/NoopHubContext.cs` | Stub IHubContext no-op untuk test write-path | VERIFIED | Dibuat di task 3 commit `8623e68e`. Wired ke factory write-path AddLive + Remove tests. Suite 597/597 hijau. |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `AddParticipantsLive` | `monitor-{batchKey}` group | `_hubContext.Clients.Group(...).SendAsync("participantAdded")` SETELAH `CommitAsync` | VERIFIED | Line 2459 commit; line 2488 broadcast. Post-commit guard terkonfirmasi. |
| `RemoveParticipantLive` | `monitor-{batchKey}` group | `_hubContext.Clients.Group(...).SendAsync("participantRemoved")` SETELAH core | VERIFIED | Line 2616 + line 2629 (Pre/Post partner). `outcome.Ok == true` → state sudah committed via `RemoveParticipantCoreAsync`. |
| `RemoveParticipantLive` | worker (StartExam) | `_hubContext.Clients.User(targetUserId).SendAsync("examRemoved")` | VERIFIED | Line 2642; hanya bila `wasInProgress == true` (di-capture line 2593 sebelum core). |
| `RestoreParticipantLive` | `monitor-{batchKey}` group | `_hubContext.Clients.Group(...).SendAsync("participantAdded")` | VERIFIED | Line 2686; post-`SaveChangesAsync` confirmed in context. |
| `AssessmentMonitoringDetail.cshtml` → `AddParticipantsLive` | AJAX POST via picker | `fetch('/Admin/AddParticipantsLive')` + antiforgery token `getToken()` | VERIFIED | Line 1377; token header `__RequestVerificationToken` ter-kirim. |
| `AssessmentMonitoringDetail.cshtml` → `RemoveParticipantLive` | AJAX POST via modal konfirmasi | `fetch('/Admin/RemoveParticipantLive')` + antiforgery | VERIFIED | Line 1466; `submitHapusPeserta()` helper. |
| `AssessmentMonitoringDetail.cshtml` → `RestoreParticipantLive` | AJAX POST via tombol Restore 1-klik | `fetch('/Admin/RestoreParticipantLive')` + antiforgery | VERIFIED | Line 1531. |
| `participantAdded` broadcast → `#tbody` aktif | DOM inject baris baru | `window.assessmentHub.on('participantAdded', ...)` → `monInjectParticipantRow` | VERIFIED | Handler line 2026; dedup `tbody:not(#tbodyRemoved)` line 2028; restore-from-panel kasus line 2033. |
| `participantRemoved` broadcast → baris pindah ke panel | Mode-aware hard/soft | `window.assessmentHub.on('participantRemoved', ...)` mode hard→`tr.remove()`, soft→`tbodyRemoved.prepend()` | VERIFIED | Handler line 2055; `tbodyRemoved.prepend(newTr)` line 2111; reason via `textContent` line 2094. |
| `examRemoved` broadcast → StartExam modal + redirect | Force-kick worker UI | `window.assessmentHub.on('examRemoved', ...)` → modal `#examRemovedModal` + countdown redirect | VERIFIED | Handler line 1319; modal show line 1333; redirect `@Url.Action("Assessment","CMP")` line 1332. |
| `ViewBag.RemovedSessions` → panel server-side | Razor render `@foreach (var r in removedList)` | `AssessmentMonitoringDetail` action → `ViewBag.RemovedSessions` → view render | VERIFIED | Query controller line 4030-4042; view render line 445-453; Razor auto-encode `@r.RemovalReason`. |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|--------------|--------|-------------------|--------|
| `AssessmentMonitoringDetail.cshtml` — panel "Peserta Dikeluarkan" | `ViewBag.RemovedSessions` | EF query `_context.AssessmentSessions.Where(a => a.RemovedAt != null)` + `OrderByDescending(a => a.RemovedAt)` | Ya — DB query real dengan filter `RemovedAt != null` | FLOWING |
| `AssessmentMonitoringDetail.cshtml` — `updateSummaryFromDOM` | `tbody:not(#tbodyRemoved) tr[data-session-id]` | DOM count dari baris aktif (exclude removed) | Ya — realtime DOM count setelah setiap push event | FLOWING |
| `participantAdded` handler — baris inject | `data.fullName`, `data.nip`, `data.status` | Payload dari `_hubContext.SendAsync` (resolve via `userDictionary` lookup, bukan Include) | Ya — FullName/NIP resolve via DB query ke `_context.Users` (line 2492-2494) | FLOWING |
| `participantRemoved` handler — baris panel | `data.removalReason`, `data.fullName` | Payload dari `_hubContext.SendAsync` (resolve pre-core, line 2606-2624) | Ya — identitas di-capture sebelum potential hard-delete detach entity | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Build 0 error | `dotnet build HcPortal.csproj` (dari summary) | Build succeeded, 0 Error | PASS |
| Suite 597/597 hijau | `dotnet test` (dari 412-01 summary `8623e68e`) | 597/597 PASS, 38/38 FlexibleParticipant+Monitoring | PASS |
| Runtime smoke Monitoring Detail HTTP 200 | GET `/Admin/AssessmentMonitoringDetail?...` via Playwright (412-02) | HTTP 200, 103KB; semua simbol DOM (`#btnTambahPeserta`, `#panelPesertaDikeluarkan`, dll) ter-render; 0 error | PASS |
| Runtime smoke StartExam handler+modal | Playwright load `/CMP/StartExam/171` (412-03) | `#examRemovedModal` count==1; `window.assessmentHub` defined; 0 JS error | PASS |
| `GetEligibleParticipantsToAdd` JSON response | GET `?sessionId=161` (412-02 smoke) | HTTP 200, JSON `[{id,fullName,nip}]` valid | PASS |
| Force-kick e2e live (2-context) | Playwright multi-context: admin hapus InProgress → worker terima examRemoved | SKIP — Phase 413 | SKIP |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|---------|
| PART-05 | 412-02 | Admin/HC tambah peserta dari Monitoring Detail + baris muncul live tanpa reload | SATISFIED | Picker modal, AJAX AddParticipantsLive, handler participantAdded, fallback inject — semua ter-wire. |
| PRMV-02 | 412-02, 412-03 | Modal konfirmasi keras untuk peserta aktif + force-kick examRemoved | SATISFIED | Modal keras hapusPesertaHardModal (backdrop static); examRemoved handler StartExam.cshtml; wasInProgress guard. |
| PLIV-01 | 412-01, 412-02 | Exclude soft-removed dari count + panel "Peserta Dikeluarkan" + Restore | SATISFIED | updateSummaryFromDOM exclude #tbodyRemoved; panel server-render ViewBag.RemovedSessions; Restore 1-klik. |
| PLIV-02 | 412-01 | Broadcast participantAdded/participantRemoved live post-commit | SATISFIED | Broadcast setelah CommitAsync (AddParticipantsLive line 2459/2488) dan setelah core (RemoveParticipantLive line 2616). |

---

### Anti-Patterns Found

| File | Area | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `AssessmentMonitoringDetail.cshtml` | Panel server-render | `@r.RemovalReason` via Razor auto-encode (bukan Html.Raw) — XSS-safe. | Info | BUKAN anti-pattern; ini mitigasi yang benar. Terkonfirmasi 0 Html.Raw untuk field user-input. |
| `AssessmentMonitoringDetail.cshtml` | JS participantRemoved handler | `tdReason.textContent = data.removalReason` (line 2094) — XSS-safe. | Info | BUKAN anti-pattern; `.textContent` adalah mitigasi benar per T-412-08. |
| `StartExam.cshtml` | examRemoved handler | `document.getElementById('examRemovedReasonValue').textContent = payload.reason` (line 1328) — XSS-safe. | Info | BUKAN anti-pattern; `.textContent` per T-412-14. |

**Tidak ada blocker atau warning anti-pattern ditemukan.** Semua potensi XSS path (reason/fullName dari user/admin) menggunakan `textContent` atau Razor `@` encode. `innerHTML` tidak digunakan untuk field user-input (grep `innerHTML.*reason` = 0 match).

---

### Human Verification Required

#### 1. Tambah Peserta Live (End-to-End)

**Test:** Login sebagai Admin → buka Monitoring Detail batch aktif → klik "Tambah Peserta" → picker muncul + checklist user eligible → pilih user → klik Konfirmasi.
**Expected:** Baris peserta baru muncul di tabel aktif TANPA reload. Jika ada Admin/HC lain memantau batch yang sama di tab berbeda, mereka pun melihat baris baru masuk.
**Why human:** Alur AJAX picker → POST AddParticipantsLive → echo SignalR participantAdded → DOM inject memerlukan app berjalan live dengan SignalR connected. Runtime smoke 412-02 hanya membuktikan markup+endpoint terdaftar, bukan alur full end-to-end.

#### 2. Hapus Peserta — Modal Keras (InProgress/Completed-cert)

**Test:** Buka Monitoring Detail batch dengan peserta InProgress → klik dropdown "⋮" → klik "Hapus Peserta" → pastikan modal KERAS muncul (backdrop static, tidak bisa diklik luar) → isi alasan → klik Konfirmasi Hapus.
**Expected:** Modal ringan TIDAK muncul (peserta InProgress → harus keras). Baris hilang dari tabel aktif. Baris muncul di panel "Peserta Dikeluarkan" dengan nama/waktu/oleh/alasan.
**Why human:** Tingkat modal ditentukan dari `data-status` (English literal "InProgress"/"Completed"). Perlu verifikasi DeriveUserStatus menghasilkan label yang benar + modal routing JS bekerja saat klik live.

#### 3. Force-Kick Worker (examRemoved)

**Test:** Admin hapus peserta yang sedang mengerjakan ujian (InProgress) dari Monitoring Detail. Secara bersamaan, worker tersebut sedang membuka StartExam.cshtml di browser berbeda.
**Expected:** Worker menerima modal "#examRemovedModal" yang muncul otomatis ("Anda telah dikeluarkan dari ujian ini.") → UI terkunci (timer berhenti, tombol submit disabled) → redirect ke `/CMP/Assessment` dalam 5 detik.
**Why human:** Memerlukan 2 browser context simultan (Playwright multi-context atau 2 browser manual). Runtime smoke 412-03 hanya verifikasi handler ter-register + modal ada di DOM, bukan trigger live SignalR dari aksi admin.

#### 4. Restore Peserta dari Panel

**Test:** Setelah peserta di-soft-remove, buka panel "Peserta Dikeluarkan" (klik chevron untuk expand) → klik "Restore" di baris peserta.
**Expected:** Baris hilang dari panel "Peserta Dikeluarkan" (langsung) → baris muncul kembali di tabel aktif atas. Panel tersembunyi otomatis jika tidak ada sisa removed.
**Why human:** Alur restore 1-klik → POST RestoreParticipantLive → echo SignalR participantAdded (kasus restore) memerlukan DB dengan data soft-removed live. DB lokal 412-02 tidak punya soft-removed saat smoke dijalankan (diverifikasi via Razor `@foreach` compile-clean saja).

---

### Gaps Summary

Tidak ada gap yang memblok goal achievement. Semua must-have terverifikasi di level kode (exists + substantive + wired + data flowing). Status `human_needed` karena 4 alur live runtime (add/remove/force-kick/restore end-to-end) memerlukan Playwright multi-context yang dijadwalkan di Phase 413.

**Deferred ke Phase 413:** Full e2e live Playwright (tambah peserta baris muncul live + hapus modal keras + worker force-kick + restore). Ini bukan gap — ini memang scope Phase 413 per ROADMAP.md.

---

_Verified: 2026-06-21T09:30:00Z_
_Verifier: Claude (gsd-verifier)_
