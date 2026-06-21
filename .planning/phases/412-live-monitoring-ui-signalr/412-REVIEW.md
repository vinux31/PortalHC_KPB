---
phase: 412-live-monitoring-ui-signalr
reviewed: 2026-06-21T00:00:00Z
depth: deep
files_reviewed: 6
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
  - Models/AssessmentMonitoringViewModel.cs
  - Views/Admin/AssessmentMonitoringDetail.cshtml
  - Views/CMP/StartExam.cshtml
  - HcPortal.Tests/NoopHubContext.cs
  - HcPortal.Tests/FlexibleParticipantAddLiveTests.cs
findings:
  critical: 0
  warning: 0
  info: 3
  total: 3
status: issues_found
---

# Phase 412: Code Review Report

**Reviewed:** 2026-06-21
**Depth:** deep
**Files Reviewed:** 6
**Status:** issues_found (0 Critical, 0 Warning, 3 Info)

## Summary

Reviewed Phase 412 (Live Monitoring UI + SignalR, milestone v32.5): SignalR broadcast wiring post-commit di 3 endpoint 410/411 (`AddParticipantsLive`/`RemoveParticipantLive`/`RestoreParticipantLive`), `examRemoved` force-kick, query `removedSessions` + `RemovedParticipantViewModel`, picker/modal/panel UI di `AssessmentMonitoringDetail.cshtml`, dan handler `examRemoved` di `StartExam.cshtml`.

**Penilaian: bersih untuk korektnes dan keamanan.** Seluruh fokus-area review terverifikasi terhadap kode aktual:

- **Broadcast strictly post-commit** — Semua `SendAsync` ditempatkan SETELAH `CommitAsync`/`SaveChangesAsync`/`outcome.Ok` (controller :2614, :2675-2698, :2497). Tidak ada kebocoran state rollback (T-412-01 mitigated).
- **`examRemoved` di-gate ke `wasInProgress`** — Flag ditangkap SEBELUM `RemoveParticipantCoreAsync` mutasi (controller :2593), dikirim hanya bila tadinya InProgress (:2641). Target `Clients.User(targetUserId)` dengan `targetUserId = session.UserId` di-resolve server (:2594) — identik preseden `examClosed` (:5013)/`sessionReset` (:4948), tak ada cross-user kick (T-412-02 mitigated).
- **Pre/Post 2-event consistency** — Partner di-broadcast event kedua bila `outcome.PartnerId != null` (:2627). `rPartner` di-load dari `_context` yang sama → EF identity-map mengembalikan instance tracked yang sama dengan yang dimutasi core, sehingga `rPartner.RemovedAt`/`RemovalReason` mencerminkan nilai post-mutasi (bukan stale).
- **Dedup-by-sessionId** — `monInjectParticipantRow` + handler `participantAdded` cek `tbody:not(#tbodyRemoved) tr[data-session-id]` sebelum inject, plus fallback 3-detik dibatalkan via `monClearAddedFallback` saat echo datang (Pitfall 4 mitigated; tak ada double-row).
- **Modal tier logic** — `isHard = (status === 'InProgress') || (status === 'Completed' && hasCert)` cocok dengan label aktual `DeriveUserStatus` (`InProgress`/`Completed` English; `Dibatalkan`/`Menunggu Penilaian` Indonesia → jalur ringan, Pitfall 1 mitigated).
- **`updateSummaryFromDOM` exclude `#tbodyRemoved`** — selector diubah ke `tbody:not(#tbodyRemoved) tr[data-session-id]` (Pitfall 2 mitigated).
- **Restore moves row back** — handler `participantAdded` mendeteksi baris di `#tbodyRemoved`, menghapus + decrement count, lalu inject ulang ke tbody aktif.
- **Picker eligible refresh** — `show.bs.modal` selalu fetch ulang `GetEligibleParticipantsToAdd?sessionId={REP_ID}` dengan reset state.

**Security (verified clean):**
- **XSS** — Razor server-side (`@r.FullName`/`@r.RemovalReason`/`@r.RemovedByName`) auto-encode; JS inject pakai `.textContent` (bukan `innerHTML`) untuk `fullName`/`nip`/`removalReason`/`removedBy`; worker modal reason via `.textContent` (StartExam :1325). 0 `Html.Raw` untuk field user-input (T-409-10 carry mitigated).
- **Antiforgery** — Semua POST AJAX (Add/Remove/Restore) kirim `__RequestVerificationToken` via `getToken()`; endpoint tetap `[ValidateAntiForgeryToken]` + `[Authorize(Roles="Admin, HC")]` (tak diubah).
- **No JSON-shape change** — Kontrak JSON 410/411 endpoint tidak berubah (broadcast ditambah sebelum `return Json(...)` yang identik). Suite `FlexibleParticipant*` tetap hijau.
- **Group scoping** — `JoinMonitor` (Hub :43-58) memverifikasi role Admin/HC sebelum `AddToGroupAsync`, sehingga payload (`removalReason`, `actorName`/identitas admin) hanya menjangkau Admin/HC, bukan worker (T-412-03 mitigated).

**Tests:** Full fast-suite **597/597 PASS** (no regression); `dotnet build` 0 error. `NoopHubContext` test stub adalah no-op murni (menelan `SendAsync` Group/User/All tanpa efek samping, tanpa replica logika produksi) — tidak memasking issue nyata; ia hanya mencegah NRE pada harness `hubContext: null!` lama. E2e live multi-tab deferred ke Phase 413 (sesuai CONTEXT).

Tiga temuan Info di bawah bersifat polish/UX minor, non-blocking.

## Info

### IN-01: Baris hasil Restore menampilkan placeholder "—" untuk Skor/Hasil/Sertifikat sampai reload

**File:** `Views/Admin/AssessmentMonitoringDetail.cshtml:1989-2024` (fungsi `monInjectParticipantRow`)
**Issue:** Saat peserta `Completed`+bersertifikat di-restore, handler `participantAdded` menginject baris via `monInjectParticipantRow` yang SELALU merender `tdScore`/`tdResult`/`tdDone` sebagai `'—'`. Badge status ikut benar (mis. "Completed"), tetapi kolom Skor/Hasil/Waktu Selesai/Sertifikat tampil kosong sampai halaman di-reload (data tetap utuh di server — exclude-query 409 sudah mengembalikannya). Payload broadcast `participantAdded` juga hanya membawa `{sessionId,userId,fullName,nip,status}` — tanpa score/cert. Ini cosmetic; korektnes data tidak terpengaruh, dan reload memperbaiki tampilan.
**Fix (opsional, kandidat 413):** Sertakan field skor/hasil/cert di payload `participantAdded` (atau cabang khusus restore) dan render di `monInjectParticipantRow` saat `status === 'Completed'`. Alternatif murah: tampilkan badge kecil "muat ulang untuk skor" pada baris restore Completed. Aman untuk dibiarkan bila UAT 413 tak mempermasalahkan (restore umumnya untuk Not-started/InProgress).

### IN-02: Broadcast `participantRemoved` kedua tetap dikirim untuk partner yang sudah soft-removed sebelumnya

**File:** `Controllers/AssessmentAdminController.cs:2627-2639`
**Issue:** Pada jalur soft, core menjaga `x.RemovedAt == null` (:2784) agar tidak menimpa metadata partner yang SUDAH soft-removed lebih dulu. Namun wrapper tetap mem-broadcast event `participantRemoved` kedua untuk `outcome.PartnerId` tanpa cek apakah partner memang baru dipindah pada operasi ini. Bila partner sudah berada di panel "Peserta Dikeluarkan" (state-drift), broadcast kedua redundan. Tidak berbahaya: handler client `participantRemoved` mencari `tbody:not(#tbodyRemoved) tr[data-session-id]` dan `return` lebih awal bila baris tak ada di tabel aktif (sudah di panel) — jadi no-op idempoten.
**Fix (opsional):** Hanya broadcast event partner bila `rPartner != null && rPartner.RemovedAt` berubah pada operasi ini (mis. capture `partnerWasActive = rPartner?.RemovedAt == null` sebelum core, lalu `if (outcome.PartnerId != null && partnerWasActive)`). Memangkas trafik SignalR redundan; bukan koreksi bug (idempoten di client).

### IN-03: Tombol "Hapus Peserta" yang di-inject SignalR meng-escape hanya tanda kutip pada `fullName`

**File:** `Views/Admin/AssessmentMonitoringDetail.cshtml:1126-1133` (`buildActionsHtml`)
**Issue:** Untuk baris yang di-inject via SignalR, `buildActionsHtml` membangun tombol `.btn-hapus-peserta` lewat string-concat ke `innerHTML`, dengan `hpName = (wn || session.fullName || '').replace(/"/g, '&quot;')` ditaruh di `data-worker-name="..."`. Hanya `"` yang di-escape; `<`/`>`/`&` tidak. Karena nilai berada dalam atribut ber-kutip-ganda dan `"` sudah di-escape, breakout atribut tidak mungkin (XSS via atribut termitigasi), dan parser HTML tidak memperlakukan `<` mentah di dalam nilai atribut sebagai awal tag. Pola ini KONSISTEN dengan kode existing pada baris yang sama (mis. `wn.replace(/"/g, '&quot;')` di :1117 untuk tombol activity-log) — bukan regresi baru. Jalur server-rendered + JS-handler utama (`monInjectParticipantRow`, panel removed) sudah memakai `.textContent` yang aman penuh.
**Fix (opsional, hardening):** Untuk konsistensi defense-in-depth, escape juga `&`/`<`/`>` (atau bangun tombol via `createElement`+`setAttribute` seperti `monInjectParticipantRow` melakukannya untuk baris removed). Risiko praktis rendah; tetap di-flag agar tidak meluas bila helper di-copy lagi.

---

_Reviewed: 2026-06-21_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: deep_
