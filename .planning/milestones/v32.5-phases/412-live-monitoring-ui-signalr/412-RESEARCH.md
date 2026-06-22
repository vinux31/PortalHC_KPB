# Phase 412: Live Monitoring UI + SignalR - Research

**Researched:** 2026-06-21
**Domain:** ASP.NET Core MVC (C#) — SignalR server→client broadcast + Razor/Bootstrap 5 vanilla-JS DOM manipulation + AJAX fetch (antiforgery)
**Confidence:** HIGH (semua jawaban file:line VERIFIED via Read/Grep di codebase aktual; nol klaim ASSUMED produksi)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Modal **keras** (extra-friction) untuk peserta **InProgress** DAN **Completed-bersertifikat**. Peserta **belum-mulai** → konfirmasi **ringan**. Client tentukan tingkat modal dari status baris (`data-status`/`data-has-cert`).
- **D-02:** Worker terima `examRemoved` → **kunci UI ujian + redirect ke daftar Assessment + banner/TempData** "Anda telah dikeluarkan dari ujian ini." **Reuse pola `examClosed`**. TANPA view/halaman dedicated baru (redirect+banner).
- **D-03:** Field **alasan (`RemovalReason`) SELALU tampil** di modal hapus + hint "wajib bila peserta sudah mengerjakan". **Server 411 tetap penjaga akhir** (soft-remove tanpa reason → 400). Client kirim reason apa adanya.
- **D-04:** Tombol **Restore = 1-klik langsung** (tanpa modal) → `RestoreParticipantLive` → baris balik ke tabel aktif live (SignalR `participantAdded`). Panel collapsible tampilkan soft-removed: nama/waktu/oleh/alasan.
- **Broadcast wiring** `participantAdded`/`participantRemoved` ditambah ke endpoint 410/411 **HANYA setelah `CommitAsync` sukses**. `examRemoved` dari `RemoveParticipantLive` bila target InProgress.
- **DOM handlers** di `AssessmentMonitoringDetail.cshtml` (~:1199-1411): `participantAdded` inject `<tr data-session-id>`; `participantRemoved` pindah baris ke panel + update summary count. Mirror DOM existing.
- **Grup SignalR** `monitor-{batchKey}` existing. `examRemoved` ke target user (pola `examClosed`).
- RBAC/antiforgery sudah di endpoint 410/411; UI kirim antiforgery token.

### Claude's Discretion
- Bentuk picker modal, styling panel collapsible, bentuk banner force-kick.
- Cara client bedakan tingkat modal (data-attribute status di `<tr>`).
- Optimistic UI vs tunggu SignalR echo (**rekomendasi:** andalkan SignalR broadcast sebagai sumber kebenaran, dedup by sessionId; fallback inject bila echo tak datang 3 detik).
- Cakupan Playwright e2e (413 punya e2e lengkap; 412 minimal smoke bila perlu).

### Deferred Ideas (OUT OF SCOPE)
- **xUnit + Playwright e2e lengkap** → Phase 413.
- **Halaman dedicated "Anda dikeluarkan"** → ditolak (D-02 pakai redirect+banner).
- **IN-02 (411): EditAssessment query exclude soft-removed** → evaluasi planner (atau tetap 413/backlog).
- Todo `2026-06-11-one-time-cleanup-data-test-lokal` — bukan scope 412.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **PART-05** | Admin/HC tambah peserta dari Monitoring Detail, baris muncul **live tanpa reload** | Endpoint `AddParticipantsLive` + `GetEligibleParticipantsToAdd` siap (410, VERIFIED `:2294`/`:2356`); helper `buildActionsHtml`/`flashRow`/`updateSummaryFromDOM` siap di view; tinggal picker modal + broadcast `participantAdded` |
| **PRMV-02** | Hapus peserta aktif wajib **modal keras** → **force-kick** via SignalR + redirect | `examClosed`/`examClosedModal` pattern siap di StartExam.cshtml (`:1254`/`:337`); `Clients.User(userId).SendAsync("examClosed",...)` pattern siap (`:4905`); endpoint `RemoveParticipantLive` siap (411, `:2556`) |
| **PLIV-01** | Soft-removed dikecualikan dari count aktif + panel "Peserta Dikeluarkan" + Restore | Exclude-query 409 sudah live (`RemovedAt == null` di `:3297`/`:3807`); panel = view-baru + **query removed-sessions baru** (BELUM ada di action `:3800`); `RestoreParticipantLive` siap (`:2585`) |
| **PLIV-02** | Add/remove tersiar live ke semua Admin/HC pemantau via SignalR | `_hubContext` SUDAH di-inject ke controller (`:27`/`:41`/`:51`); grup `monitor-{batchKey}` + Join/LeaveMonitor siap; pola broadcast `Clients.Group($"monitor-{batchKey}").SendAsync(...)` ada 2× (`:3732`/`:4351`) |
</phase_requirements>

## Summary

Phase 412 adalah fase **integrasi UI + SignalR murni** di atas backend 409/410/411 yang sudah lengkap. Hampir seluruh infrastruktur teknis yang dibutuhkan **sudah ada dan VERIFIED di codebase** — tidak ada library baru, tidak ada pola arsitektur baru, dan **migration=FALSE**. Tugas inti hanya: (1) menambah ~3 baris broadcast SignalR di tiap endpoint 410/411 (post-commit), (2) menambah satu query "removed sessions" di action `AssessmentMonitoringDetail`, (3) menulis markup modal/panel + handler JS di view, dan (4) mirror handler `examClosed` → `examRemoved` di StartExam.

Temuan paling penting: **`IHubContext<AssessmentHub> _hubContext` SUDAH di-inject ke `AssessmentAdminController`** (`:27`, `:41`, `:51`) — endpoint 410/411 sengaja men-defer broadcast (D-04/D-03) tapi punya akses penuh. Pola broadcast ke grup monitor (`Clients.Group($"monitor-{batchKey}").SendAsync(...)`) dan ke worker tunggal (`Clients.User(session.UserId).SendAsync("examClosed", ...)`) keduanya sudah ada sebagai preseden langsung. `batchKey` di seluruh codebase = `$"{Title}|{Category}|{Schedule.Date:yyyy-MM-dd}"` (pipe-delimited), dan `ViewBag.AssessmentBatchKey` sudah di-set di action (`:3942`) + di-expose ke JS sebagai `window.assessmentBatchKey` (view `:1200`).

**Primary recommendation:** Wire broadcast 3-baris ke 410/411 post-commit (reuse `_hubContext`), tambah query+ViewBag removed-sessions di action `:3800`, tulis markup+JS di view (reuse `buildActionsHtml`/`flashRow`/`updateSummaryFromDOM`/`getToken`/`window.showAssessmentToast`), mirror `examClosed`→`examRemoved` di StartExam. **Andalkan SignalR echo sebagai sumber kebenaran DOM (dedup by sessionId), fallback inject 3-detik.** Hati-hati 3 jebakan: label status Indonesia ("Dibatalkan"/"Menunggu Penilaian", BUKAN "Cancelled"), exclude `#tbodyRemoved` dari `updateSummaryFromDOM`, dan XSS pada kolom Alasan (gunakan `textContent`/`Html.Encode`).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Broadcast `participantAdded`/`participantRemoved`/`examRemoved` | API / Backend (`AssessmentAdminController` post-commit) | — | Server-authoritative; HANYA setelah `CommitAsync` (hindari notif rollback). Tier UI tak boleh broadcast — bisa di-spoof |
| Routing event SignalR ke grup/user | API / Backend (`_hubContext.Clients.Group`/`.User`) | SignalR hub (membership grup) | `monitor-{batchKey}` group membership dikelola `JoinMonitor` (Hub); broadcast dikirim controller |
| Data source panel "Peserta Dikeluarkan" | API / Backend (query `RemovedAt != null` di action `:3800`) | Razor (render server-side) | Soft-removed = data DB; harus di-query server (RemovedBy → FullName resolve juga server-side) |
| Picker modal + inject/pindah baris DOM | Browser / Client (vanilla JS di view) | — | Manipulasi DOM live tanpa reload = murni client; konsumsi JSON dari endpoint |
| Tingkat modal (keras/ringan) | Browser / Client (`data-status`/`data-has-cert`) | API (server penjaga akhir reason 411) | UX-only di client; server 411 tetap validasi reason-wajib (D-03) |
| Force-kick UI lock + redirect worker | Browser / Client (StartExam handler `examRemoved`) | API (kirim event + set TempData) | Lock UI = client; banner TempData = server-set saat redirect |
| Antiforgery token | Browser / Client (baca `#antiforgeryForm`) | API (`[ValidateAntiForgeryToken]`) | Token di-generate server (Razor), dikirim client, divalidasi server |

## Standard Stack

### Core (semua EXISTING — tidak ada install)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core SignalR (`@microsoft/signalr` client) | net8.0 / `~/lib/signalr/signalr.min.js` | Real-time server→client push | [VERIFIED: codebase] sudah dipakai `AssessmentHub` + `assessment-hub.js`; grup `monitor-{batchKey}` aktif |
| Bootstrap 5 | existing (layout) | modal/card/badge/collapse/toast | [VERIFIED: 412-UI-SPEC Registry] existing dependency; semua komponen reuse kelas existing |
| Bootstrap Icons | existing | `bi-person-plus/x/slash`, `bi-arrow-counterclockwise`, dll | [VERIFIED: codebase] sudah dipakai throughout page |
| Vanilla JS (no framework) | — | DOM manipulation, fetch AJAX | [VERIFIED: codebase] seluruh `AssessmentMonitoringDetail.cshtml` & `StartExam.cshtml` vanilla JS |
| EF Core | 8.0.0 (pinned local) | query removed sessions | [VERIFIED: codebase] read-only query tambahan di action |

### Supporting (helper JS view — semua EXISTING, reuse verbatim)

| Helper | Lokasi | Purpose | When to Use |
|--------|--------|---------|-------------|
| `buildActionsHtml(session, isPackageMode)` | view `:832` | Render kolom Aksi (col `tds[6]`) per status | Saat inject baris baru `participantAdded` — **WAJIB diperluas** tambah item "Hapus Peserta" |
| `flashRow(tr, cssClass)` | view `:1249` | Animasi flash baris (`flash-update`/`flash-complete`) | Setelah inject/update baris |
| `updateSummaryFromDOM()` | view `:1258` | Hitung ulang 7 kartu summary dari DOM | Setelah add/remove baris — **WAJIB diubah** exclude `#tbodyRemoved` |
| `getToken()` | view `:811` | Baca antiforgery dari `#antiforgeryForm` | Tiap POST AJAX baru |
| `window.showAssessmentToast(msg, ...)` | `wwwroot/js/assessment-hub.js:98` | Toast notifikasi | Setelah add/remove/restore sukses |
| `appUrl(path)` / `basePath` | `assessment-hub.js` (global) | URL sub-path-aware (PathBase) | **WAJIB** untuk semua fetch URL (deploy sub-path `/KPB-PortalHC`) |
| `statusBadgeClass(s)` / `statusDisplayLabel(s)` | view `:817`/`:826` | Badge class + label Indonesia | Saat render badge status baris baru |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| SignalR echo sebagai sumber kebenaran (dedup) | Optimistic UI (inject langsung dari response POST) | Optimistic = risiko double-inject (echo + optimistic). CONTEXT D-discretion + UI-SPEC §2/§8 sudah putuskan **echo-primary + fallback 3s** → ikuti itu |
| Server selalu render panel `#panelPesertaDikeluarkan` (collapsed) | JS inject panel from scratch saat removed pertama | UI-SPEC §6 rekomendasi: **server selalu render panel** (collapsed, mungkin kosong) untuk batch non-Proton → JS tak perlu bangun panel |
| Broadcast dari endpoint 410/411 (controller) | Broadcast dari dalam Hub | Hub tak punya konteks transaksi commit; controller pegang `_hubContext` + tahu commit sukses → broadcast di controller (preseden `:4351`) |

**Installation:** Tidak ada. Semua dependency existing.

**Version verification:** Tidak ada paket baru → tidak ada `npm install`/`dotnet add`. `@playwright/test` `^1.58.2` (test/package.json), xUnit `2.9.3` + `Microsoft.NET.Test.Sdk` `17.13.0` (HcPortal.Tests.csproj), net8.0. [VERIFIED: codebase grep]

## Architecture Patterns

### System Architecture Diagram

```
[Admin A browser]                                    [Admin B browser]
  Monitoring Detail                                    Monitoring Detail
  (JoinMonitor monitor-{batchKey})                     (JoinMonitor monitor-{batchKey})
        |                                                      ^
        | (1) klik "Tambah Peserta"                            |
        |     fetch GET GetEligibleParticipantsToAdd           | (5) on('participantAdded')
        |     fetch POST AddParticipantsLive  ----+            |     inject <tr> + flashRow
        v                                          |            |     + updateSummaryFromDOM
  ASP.NET Core AssessmentAdminController           |            |
        |                                          |            |
   (2) [Authorize Admin,HC] + ValidateAntiForgery  |            |
   (3) atomic tx: AssessmentSession + UPA          |            |
        |  CommitAsync()  <----- success gate -----+            |
        v                                                       |
   (4) _hubContext.Clients.Group("monitor-{batchKey}")          |
            .SendAsync("participantAdded", {sessionId,...}) ----+----> ALL monitors
        |
        |  (REMOVE path) target InProgress?
        v
   _hubContext.Clients.User(session.UserId)
            .SendAsync("examRemoved", {reason}) -------> [Worker browser StartExam.cshtml]
                                                            on('examRemoved'):
                                                            lock UI + show #examRemovedModal
                                                            + countdown redirect → /CMP/Assessment
                                                            (banner TempData "Anda telah dikeluarkan...")

  batchKey = $"{Title}|{Category}|{Schedule.Date:yyyy-MM-dd}"  (pipe-delimited, VERIFIED :3942)
```

### Recommended Project Structure (file yang disentuh — semua EXISTING kecuali test)

```
Controllers/AssessmentAdminController.cs   # +broadcast 3 baris di 410/411 (post-commit) + query removed di :3800
Hubs/AssessmentHub.cs                       # TIDAK perlu method baru (broadcast dari controller, grup existing)
Views/Admin/AssessmentMonitoringDetail.cshtml  # +picker modal +2 modal hapus +panel +handler JS participantAdded/Removed
Views/CMP/StartExam.cshtml                  # +#examRemovedModal +handler examRemoved (mirror examClosed)
tests/e2e/flexible-participant-412.spec.ts  # (opsional minimal smoke; e2e lengkap = 413)
```

### Pattern 1: Broadcast post-commit ke grup monitor (PLIV-02)
**What:** Setelah `CommitAsync()` sukses di endpoint 410/411, kirim event ke `monitor-{batchKey}`.
**When to use:** `AddParticipantsLive` (+`RestoreParticipantLive`) → `participantAdded`; `RemoveParticipantLive` → `participantRemoved`.
**Example:**
```csharp
// Source: VERIFIED AssessmentAdminController.cs:4351 (preseden workerSubmitted) + :3942 (batchKey format)
// Di AddParticipantsLive, GANTI komentar D-04 (:2481) dengan broadcast SETELAH transaction.CommitAsync():
var batchKey = $"{rep.Title}|{rep.Category}|{rep.Schedule.Date:yyyy-MM-dd}";
foreach (var s in createdSessions)
{
    await _hubContext.Clients.Group($"monitor-{batchKey}").SendAsync("participantAdded", new
    {
        sessionId = s.Id,
        userId    = s.UserId,
        fullName  = userDictionary.TryGetValue(s.UserId, out var u) ? u.FullName : "",
        nip       = userDictionary.TryGetValue(s.UserId, out var u2) ? u2.NIP : null,
        status    = s.Status   // "Open"/"Upcoming" — client map ke badge "Not started"
    });
}
```
> CATATAN PENTING (planner): `_hubContext` SUDAH ter-inject — endpoint tinggal pakai. JANGAN tambah constructor param. Broadcast HARUS setelah `CommitAsync` (di luar try/catch transaksi, atau di akhir try setelah commit). Payload `participantAdded` di-konsumsi handler view §8a UI-SPEC.

### Pattern 2: Force-kick worker via Clients.User (PRMV-02)
**What:** Kirim `examRemoved` HANYA ke worker yang dihapus (bukan broadcast grup).
**When to use:** Di `RemoveParticipantLive`/core, bila target sesi InProgress (`StartedAt != null && CompletedAt == null && RemovedAt == null` saat hapus).
**Example:**
```csharp
// Source: VERIFIED AssessmentAdminController.cs:4905 (examClosed precedent — targeting per-user)
// Worker connection di-map ke user via SignalR Context.UserIdentifier (Identity userId).
await _hubContext.Clients.User(session.UserId).SendAsync("examRemoved", new { reason });
```
> Force-kick dikirim untuk sesi InProgress. Karena 411 core sudah evaluasi has-data (InProgress pasti `StartedAt != null` → jalur SOFT), broadcast `examRemoved` aman setelah soft-remove commit. Planner: tentukan kondisi kirim (mis. `session.StartedAt != null && session.CompletedAt == null` sebelum mutasi). Worker-side handler mirror `examClosed` (view StartExam `:1254`).

### Pattern 3: Mirror examClosed → examRemoved (StartExam worker)
**What:** Handler client identik `examClosed` (`:1254`) + modal non-dismissable identik `examClosedModal` (`:337`).
**When to use:** Worker yang sedang ujian di-force-kick.
**Example:**
```javascript
// Source: VERIFIED StartExam.cshtml:1254 (examClosed handler — copy struktur)
window.assessmentHub.on('examRemoved', function(payload) {
    if (examClosed) return;            // reuse var existing :1249 (dual-trigger guard)
    examClosed = true;
    clearInterval(timerInterval);      // var existing :489
    clearInterval(saveInterval);       // var existing :812
    window.onbeforeunload = null;
    if (payload && payload.reason) { /* tampilkan #examRemovedReasonValue */ }
    var redirectTarget = '@Url.Action("Assessment", "CMP")';
    new bootstrap.Modal(document.getElementById('examRemovedModal')).show();
    // countdown 5s → window.location.href = redirectTarget (pola :1274-1283)
});
```

### Pattern 4: AJAX POST dengan antiforgery (form-urlencoded)
**What:** Konsumsi endpoint 410/411 dari client.
**When to use:** `AddParticipantsLive`, `RemoveParticipantLive`, `RestoreParticipantLive`.
**Example:**
```javascript
// Source: VERIFIED AssessmentMonitoringDetail.cshtml:1426 (addExtraTime pattern)
var token = getToken();   // :811
fetch(appUrl('/Admin/RemoveParticipantLive'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
    body: 'sessionId=' + sid + '&reason=' + encodeURIComponent(reason)
         + '&__RequestVerificationToken=' + encodeURIComponent(token)
}).then(r => r.json()).then(...)
```
> Endpoint 410/411 menerima `[FromForm]`-style param (`int sessionId, List<string> userIds` / `int sessionId, string? reason`). Untuk `userIds[]` array: kirim berulang `userIds=id1&userIds=id2` (model-binding ASP.NET). [VERIFIED: signature `:2356`/`:2556`]

### Anti-Patterns to Avoid
- **Optimistic inject TANPA dedup:** echo SignalR + inject manual → baris dobel. **Gunakan** dedup `querySelector('tbody tr[data-session-id="X"]:not([data-removed])')` sebelum inject (UI-SPEC §SignalR Contract).
- **`updateSummaryFromDOM()` menghitung baris removed:** selector `tbody tr[data-session-id]` (`:1259`) akan ikut hitung `#tbodyRemoved`. **WAJIB** ubah ke `tbody:not(#tbodyRemoved) tr[data-session-id]` atau filter `:not([data-removed])`.
- **`innerHTML` untuk kolom Alasan/Nama:** XSS via `RemovalReason` bebas-teks. **Gunakan** `textContent` (JS) / `Html.Encode()` (Razor) — carry T-409-10 (411 SUMMARY `:108`).
- **Broadcast sebelum commit:** notif untuk transaksi yang rollback. **WAJIB** post-`CommitAsync` (spec §G).
- **Hardcode URL tanpa `appUrl()`:** rusak di deploy sub-path `/KPB-PortalHC`. **Gunakan** `appUrl(path)`.
- **Tambah constructor param `IHubContext`:** sudah ada (`:41`). Menambah lagi = duplikat/compile error.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Kirim event real-time ke pemantau | WebSocket/polling custom | `_hubContext.Clients.Group($"monitor-{batchKey}")` (existing) | Grup, reconnect, rejoin sudah dikelola `AssessmentHub` + `assessment-hub.js` |
| Force-kick worker spesifik | Map connectionId manual | `Clients.User(userId)` (preseden `:4905`) | SignalR `Context.UserIdentifier` = Identity userId auto-map; multi-tab handled |
| Toast notifikasi | Toast div custom | `window.showAssessmentToast()` (`assessment-hub.js:98`) | Sudah dipakai semua handler existing — konsisten |
| Antiforgery token JS | Cari `<input>` ad-hoc | `getToken()` (`:811`) | Single source `#antiforgeryForm` |
| URL sub-path | String concat `/Admin/...` | `appUrl()`/`basePath` | Deploy `/KPB-PortalHC` butuh PathBase (pelajaran PXF-01) |
| Render kolom Aksi baris baru | HTML string ad-hoc | `buildActionsHtml()` (`:832`) — perluas | Konsisten dgn baris server-rendered; gate status sama |
| Recompute summary count | Hitung manual per event | `updateSummaryFromDOM()` (`:1258`) — perluas | Single source 7-kartu; tinggal exclude removed |
| Modal non-dismissable kick | Modal custom | `examClosedModal`/handler pattern (`:337`/`:1254`) | `data-bs-backdrop="static"` + countdown + onbeforeunload-clear sudah benar |
| batchKey/group name | Format ad-hoc | `$"{Title}|{Category}|{Schedule.Date:yyyy-MM-dd}"` + `window.assessmentBatchKey` | Format identik di seluruh codebase (`:3942`, `:4350`, `:4985`) |

**Key insight:** Phase 412 nyaris **nol kode infrastruktur baru** — 95% adalah merangkai komponen existing. Risiko terbesar BUKAN teknis-SignalR (terbukti jalan) melainkan **konsistensi label/selector** (status Indonesia, exclude `#tbodyRemoved`, XSS-safe render). Setiap "hand-roll" = drift dari preseden yang sudah teruji.

## Runtime State Inventory

> Fase ini BUKAN rename/migration — namun ada satu interaksi state runtime SignalR yang relevan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — migration=FALSE; kolom `RemovedAt/RemovedBy/RemovalReason` sudah ada (409). Panel hanya MEMBACA. | code edit (query read-only) |
| Live service config | **SignalR group membership** `monitor-{batchKey}` di-maintain in-memory oleh hub; bukan persisted. Worker connection di-map via `Context.UserIdentifier`. | none — reconnect rejoin sudah ada (`onreconnected` view `:1241`) |
| OS-registered state | None — verified (tak ada scheduler/service) | none |
| Secrets/env vars | None — verified | none |
| Build artifacts | None — view/JS/controller-edit; tak ada package rename | none |

**Catatan SignalR force-kick:** Worker yang sudah disconnect (tutup tab) saat `examRemoved` dikirim → event hilang (tidak persisted). Mitigasi sudah ada di 409: guard re-entry `StartExam`/`SubmitExam`/`JoinBatch` cek `RemovedAt != null` → worker yang reconnect setelah dihapus tetap diblok (PRMV-03 Complete). Force-kick = UX terbaik-effort untuk yang online; guard server = penjamin korektnes.

## Common Pitfalls

### Pitfall 1: Label status Indonesia (BUKAN "Cancelled"/"Completed" English)
**What goes wrong:** UI-SPEC §3 contoh `data-status` pakai nilai `"Cancelled"`, tapi `DeriveUserStatus` (`:3241`) RETURN `"Dibatalkan"` dan `"Menunggu Penilaian"` (Indonesia). Client logic yang cek `=== "Cancelled"` tak akan match baris server-rendered.
**Why it happens:** `DeriveUserStatus` map sebagian status ke Indonesia (`Cancelled`→`Dibatalkan`, `PendingGrading`→`Menunggu Penilaian`); status lain tetap English (`Completed`/`InProgress`/`Abandoned`/`Not started`).
**How to avoid:** Untuk `data-status` baris server (UI-SPEC §3), isi dari `session.UserStatus` (sudah hasil `DeriveUserStatus` = bisa Indonesia). Logic tingkat-modal D-01 cek: `data-status === "InProgress"` (English, aman) ATAU (`data-status === "Completed"` && has-cert). `"Dibatalkan"`/`"Menunggu Penilaian"`/`"Not started"`/`"Abandoned"` → jalur ringan. Verifikasi nilai exact via `DeriveUserStatus` switch (`:3242-3253`).
**Warning signs:** Modal keras tak muncul untuk InProgress; atau baris not-started salah dikira keras.

### Pitfall 2: `updateSummaryFromDOM()` ikut menghitung baris di panel removed
**What goes wrong:** Setelah pindah baris ke `#tbodyRemoved`, `count-total` masih hitung baris removed → count salah (peserta aktif over-counted).
**Why it happens:** Selector existing `document.querySelectorAll('tbody tr[data-session-id]')` (`:1259`) match SEMUA tbody termasuk panel removed.
**How to avoid:** Ubah selector ke `tbody:not(#tbodyRemoved) tr[data-session-id]` (UI-SPEC §Summary Count). Pastikan `#tbodyRemoved` punya id unik. `count-total` = peserta aktif saja (selaras exclude-query 409 server-side).
**Warning signs:** Setelah hapus 1 peserta, total tidak turun.

### Pitfall 3: XSS via `RemovalReason` bebas-teks di kolom Alasan
**What goes wrong:** `RemovalReason` (bebas-teks dari admin) di-render `innerHTML` → stored XSS saat panel dilihat admin lain.
**Why it happens:** Reason masuk DB tanpa sanitasi (by design — server validasi non-kosong saja).
**How to avoid:** Razor server-side: `@removalReason` (auto HTML-encode) atau `Html.Encode()`. JS inject (handler `participantRemoved`): `cell.textContent = data.removalReason` (BUKAN `innerHTML`). Sama untuk `fullName`/`removedBy`. Carry T-409-10 + 411 SUMMARY `:108`.
**Warning signs:** Reason berisi `<` tampil sebagai tag.

### Pitfall 4: Double-inject (echo SignalR + optimistic) → baris dobel
**What goes wrong:** POST sukses → inject manual; lalu echo `participantAdded` datang → inject lagi → 2 baris sessionId sama.
**Why it happens:** Admin pelaku ADD juga anggota grup `monitor-{batchKey}` → terima echo sendiri.
**How to avoid:** Andalkan echo sebagai sumber kebenaran (jangan inject dari response POST). Dedup: cek `querySelector('tbody tr[data-session-id="X"]:not([data-removed])')` sebelum inject. Fallback: jika echo tak datang 3 detik setelah POST sukses, inject dari `added[]` payload (tetap dedup). UI-SPEC §2/§8.
**Warning signs:** Baris duplikat muncul sesaat setelah tambah.

### Pitfall 5: Panel "Peserta Dikeluarkan" belum ada di DOM saat removed pertama
**What goes wrong:** `participantRemoved` handler coba `#tbodyRemoved` tapi panel tak dirender (belum ada removed sebelumnya) → null error.
**Why it happens:** Render kondisional "hanya bila ada removed" membuat panel absen di first-remove.
**How to avoid:** **Rekomendasi UI-SPEC §6 (LOCKED-ish):** server SELALU render `#panelPesertaDikeluarkan` (collapsed, mungkin kosong) untuk batch non-Proton → JS tak perlu bangun panel from scratch. Panel jadi visible setelah ≥1 baris. Planner: pilih ini (lebih sederhana dari inject-panel).
**Warning signs:** Remove pertama lempar `Cannot read properties of null`.

### Pitfall 6: `participantRemoved` mode "hard" vs "soft" — baris hard tak masuk panel
**What goes wrong:** Hard-delete (not-started) tak punya data removed → tak boleh masuk panel "Peserta Dikeluarkan" (tak bisa di-restore).
**Why it happens:** Endpoint 411 return `mode: "hard"|"soft"|"noop"` (`:2577`). Hard = baris hilang total; soft = arsip ke panel.
**How to avoid:** Handler `participantRemoved` cek `data.mode`: `"hard"` → `tr.remove()` saja; `"soft"` → pindah ke `#tbodyRemoved`. UI-SPEC §8b. Payload broadcast HARUS sertakan `mode` (dari `outcome.Mode`).
**Warning signs:** Peserta not-started yang dihapus muncul di panel dengan tombol Restore (salah — hard-deleted tak bisa restore).

## Code Examples

### Tambah query removed-sessions di action (PLIV-01, data panel)
```csharp
// Source: VERIFIED AssessmentAdminController.cs:3800-3942 (action AssessmentMonitoringDetail)
// SISIPKAN sebelum `return View(model);` (:3984). Action saat ini HANYA query RemovedAt==null (:3807);
// panel butuh query TERPISAH RemovedAt != null. RemovedBy (userId) → resolve ke FullName.
var removedSessions = await _context.AssessmentSessions
    .Include(a => a.User)
    .Where(a => a.Title == title && a.Category == category
             && a.Schedule.Date == scheduleDate.Date
             && a.RemovedAt != null)                    // <-- kebalikan exclude-query
    .OrderByDescending(a => a.RemovedAt)
    .ToListAsync();
// Resolve RemovedBy userId → display name (panel kolom "Oleh")
var removerIds = removedSessions.Where(s => s.RemovedBy != null).Select(s => s.RemovedBy!).Distinct().ToList();
var removerMap = await _context.Users.Where(u => removerIds.Contains(u.Id))
    .ToDictionaryAsync(u => u.Id, u => u.FullName);
ViewBag.RemovedSessions = removedSessions.Select(s => new {
    s.Id, FullName = s.User?.FullName ?? "Unknown", Nip = s.User?.NIP ?? "",
    s.RemovedAt, RemovedByName = s.RemovedBy != null && removerMap.TryGetValue(s.RemovedBy, out var n) ? n : (s.RemovedBy ?? "—"),
    s.RemovalReason
}).ToList();
```
> Planner: pertimbangkan ekstrak ke ViewModel typed (mis. `RemovedParticipantViewModel`) ketimbang anonymous ViewBag — konsisten dgn pola `MonitoringSessionViewModel`. Proton: `assessmentType`/Proton batch tak punya panel (spec §F) — guard `Model.Category != "Assessment Proton"`.

### Handler `participantRemoved` (mode-aware, XSS-safe)
```javascript
// Source: pola handler existing view :1383 (workerAnswerEdited) + UI-SPEC §8b
window.assessmentHub.on('participantRemoved', function(data) {
    var tr = document.querySelector('tbody:not(#tbodyRemoved) tr[data-session-id="' + data.sessionId + '"]');
    if (!tr) return;
    if (data.mode === 'hard') { tr.remove(); }
    else {  // soft → pindah ke panel
        var newTr = document.createElement('tr');
        newTr.setAttribute('data-session-id', data.sessionId);
        newTr.setAttribute('data-removed', 'true');
        // ... bangun 5 td; kolom Alasan/Nama via textContent (XSS-safe, Pitfall 3)
        document.getElementById('tbodyRemoved').prepend(newTr);
        tr.remove();
        // tampilkan panel + #countRemoved++
    }
    updateSummaryFromDOM();                          // (sudah exclude #tbodyRemoved, Pitfall 2)
    window.showAssessmentToast(data.fullName + ' dikeluarkan dari batch');
});
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Tombol hapus per-peserta mati (`DeleteAssessmentPeserta` stub) | Endpoint backend 411 live (`RemoveParticipantLive`/`RestoreParticipantLive`/fix stub) | Phase 411 (2026-06-21) | 412 tinggal konsumsi — backend lengkap |
| Add peserta via reload `EditAssessment` | `AddParticipantsLive` AJAX + JSON (410) | Phase 410 (2026-06-21) | 412 inject live tanpa reload |
| Monitoring count termasuk removed | Exclude `RemovedAt == null` (3 query) | Phase 409 (2026-06-21) | `count-total` sudah benar server-side; client mirror |

**Deprecated/outdated:**
- UI-SPEC §3 contoh `data-status="Cancelled"`: nilai aktual `DeriveUserStatus` = `"Dibatalkan"` (Pitfall 1). Treat UI-SPEC contoh sebagai ilustrasi, bukan nilai literal.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Force-kick dikirim hanya untuk sesi `StartedAt != null && CompletedAt == null` (InProgress) saat hapus | Pattern 2 | Bila kirim ke worker non-InProgress, harmless (handler guard `examClosed`), tapi UX noise. Planner tentukan kondisi exact. **LOW risk** |
| A2 | `userIds[]` di `AddParticipantsLive` di-bind via repeated form field `userIds=a&userIds=b` | Pattern 4 | Bila binding beda (mis. JSON body), POST gagal bind. Mitigasi: signature `List<string> userIds` = standard MVC form-array binding. **LOW risk** — verifikasi saat implement via test/manual |
| A3 | Server selalu render panel collapsed (UI-SPEC §6 rekomendasi) dipilih planner | Pitfall 5 | Bila pilih JS-inject-panel, butuh kode lebih + risiko null. Rekomendasi UI-SPEC sudah condong server-render. **LOW risk** |

**Catatan:** Semua klaim infrastruktur (endpoint, `_hubContext`, helper, batchKey, examClosed pattern) = `[VERIFIED: codebase file:line]`, BUKAN assumed. Tabel di atas hanya keputusan implementasi minor yang planner finalisasi.

## Open Questions (RESOLVED)

1. **Kondisi exact kirim `examRemoved`** — **RESOLVED:** kirim HANYA bila `session.StartedAt != null && session.CompletedAt == null && session.RemovedAt == null` (benar-benar di tengah ujian), di-capture sebagai `wasInProgress` SEBELUM core mutasi. Diterapkan di Plan 412-01 Task 1b. Completed tak di-kick.

2. **Bentuk payload `participantRemoved` untuk Pre/Post pair** — **RESOLVED:** broadcast **2 event** `participantRemoved` (satu per sesi ter-soft-remove) via `outcome.PartnerId` — client pindahkan kedua baris konsisten. Diterapkan di Plan 412-01 Task 1b.

3. **IN-02 (411 deferred): EditAssessment query exclude soft-removed** — **RESOLVED:** **dikeluarkan dari scope 412** (UI Monitoring saja). Tetap di 413/backlog. Tidak ada plan 412 menyentuhnya.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK / `dotnet` | build + run + test | ✓ (asumsi env dev existing) | net8.0 | — |
| SQL Server lokal (`HcPortalDB_Dev`) | `dotnet run` @5277 + UAT | ✓ (existing) | — | — |
| Node + Playwright | e2e smoke (opsional 412; lengkap 413) | ✓ | `@playwright/test ^1.58.2` | xUnit + manual UAT |
| SignalR client lib | runtime view | ✓ `~/lib/signalr/signalr.min.js` | existing | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None — semua tooling existing.

> Catatan port: MEMORY menunjukkan app lokal di **5277** (worktree main) atau **5270** (ITHandoff). CLAUDE.md & roadmap 412 = **localhost:5277** (branch main). Verifikasi port saat `dotnet run`. AD-off lokal: `Authentication__UseActiveDirectory=false`.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 (net8.0); Playwright `@playwright/test ^1.58.2` |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj`; `tests/playwright.config.ts` |
| Quick run command | `dotnet test --filter "FullyQualifiedName~Monitoring\|FullyQualifiedName~FlexibleParticipant"` |
| Full suite command | `dotnet test` (fast suite ~581-596) + `cd tests && npx playwright test --workers=1` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PLIV-02 | Broadcast `participantAdded` post-commit ke grup | integration (assert _hubContext invoked) ATAU Playwright (2-context live) | `dotnet test` mock IHubContext / `npx playwright test flexible-participant-412` | ❌ Wave 0 (412 minimal; full 413) |
| PRMV-02 | Modal keras muncul utk InProgress + force-kick redirect | Playwright (DOM + SignalR runtime — pelajaran Phase 354) | `npx playwright test flexible-participant-412 -g "kick"` | ❌ Wave 0 (full 413) |
| PART-05 | Add dari Monitoring → baris muncul live tanpa reload | Playwright (DOM live) | `npx playwright test flexible-participant-412 -g "add"` | ❌ Wave 0 (full 413) |
| PLIV-01 | Panel "Peserta Dikeluarkan" render + Restore + exclude count | Playwright (DOM) + assert `updateSummaryFromDOM` exclude removed | `npx playwright test flexible-participant-412 -g "panel"` | ❌ Wave 0 (full 413) |
| (regression) | `DeriveUserStatus` 6-cabang tetap benar | unit | `dotnet test --filter MonitoringUserStatus` | ✅ `MonitoringUserStatusTests.cs` |
| (regression) | 410/411 endpoint contract tak berubah saat +broadcast | integration | `dotnet test --filter FlexibleParticipant` | ✅ `FlexibleParticipantAddLiveTests.cs` + `FlexibleParticipantRemoveTests.cs` |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter FlexibleParticipant` (quick, <30s) — pastikan broadcast-edit tak rusak kontrak 410/411.
- **Per wave merge:** `dotnet test` full fast-suite (no regression 409/410/411 + guard 391/398.1).
- **Phase gate:** `dotnet build` 0 error + `dotnet run` @5277 manual smoke + (opsional) Playwright smoke sebelum `/gsd-verify-work`. **e2e lengkap = Phase 413.**

### Wave 0 Gaps
- [ ] `tests/e2e/flexible-participant-412.spec.ts` — smoke minimal: add baris live, remove modal keras, panel+restore, worker kick (CONTEXT izinkan minimal; full di 413). **412 boleh defer e2e penuh ke 413** per Claude's discretion.
- [ ] (opsional) integration test broadcast: assert `Mock<IHubContext>` `Clients.Group(...).SendAsync("participantAdded", ...)` dipanggil setelah commit. Berguna mengunci PLIV-02 tanpa browser. Planner pertimbangkan — kalau di-skip, e2e/UAT 413 yang cover.

*Catatan State-A: infrastruktur test sudah ada (5 file Flexible*/Monitoring* + 33 e2e spec). 412 = UI/SignalR runtime → Playwright wajib untuk bukti (grep+build tak cukup, pelajaran Phase 354). Bobot e2e dapat ditaruh di 413 sesuai CONTEXT.*

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | `[Authorize(Roles="Admin, HC")]` di SEMUA endpoint 410/411 (VERIFIED `:2354`/`:2554`/`:2584`); `AssessmentHub [Authorize]` (`:9`); `JoinMonitor` cek role Admin/HC (`:56`) |
| V3 Session Management | yes | SignalR `Context.UserIdentifier` (Identity); guard re-entry 409 cegah sesi removed lanjut |
| V4 Access Control | yes | Server-authoritative batch resolve dari sessionId (anti-tampering, 410 SUMMARY); `JoinMonitor` role-gate; force-kick `Clients.User` (tak bisa target user lain dari client) |
| V5 Input Validation | yes | `reason` bebas-teks → encode-at-render (Pitfall 3); `sessionId` int; server 411 validasi reason-wajib-soft (D-03) |
| V6 Cryptography | no | Tak ada operasi kripto baru di 412 |
| V13 API / CSRF | yes | `[ValidateAntiForgeryToken]` semua POST (`:2355`/`:2555`/`:2583`); client kirim token via `getToken()` |

### Known Threat Patterns for ASP.NET Core SignalR + Razor/JS

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Stored XSS via `RemovalReason` di panel | Tampering/Info-disclosure | `textContent` (JS) / `@`-encode (Razor) — NEVER `innerHTML` (Pitfall 3, T-409-10) |
| CSRF pada add/remove/restore | Spoofing | `[ValidateAntiForgeryToken]` (sudah) + `getToken()` client |
| Privilege escalation (non-Admin trigger broadcast) | Elevation | RBAC `[Authorize(Roles="Admin, HC")]` + Hub `JoinMonitor` role-gate; broadcast hanya dari endpoint ber-RBAC |
| Force-kick user lain (target arbitrary userId) | Tampering | userId di-resolve server dari `sessionId` (tak terima userId mentah dari client); `Clients.User` pakai userId DB |
| Information leak (Admin B lihat broadcast batch lain) | Info-disclosure | Group-scoped `monitor-{batchKey}`; `JoinMonitor` verifikasi role sebelum add-to-group |
| Spoofed SignalR event dari client | Spoofing | Client TAK bisa kirim `participantAdded`/`examRemoved` (server→client only; tak ada Hub method client-invokable untuk ini) |

> `security_enforcement` absent di config.json → enabled. 412 tak menambah skema/endpoint baru (broadcast pakai endpoint ber-RBAC existing) → permukaan ancaman minimal. Secure-phase fokus: XSS-at-render panel + verifikasi broadcast tak bocor lintas-batch.

## Sources

### Primary (HIGH confidence — VERIFIED codebase)
- `Controllers/AssessmentAdminController.cs` — `_hubContext` inject `:27/:41/:51`; `GetEligibleParticipantsToAdd` `:2294`; `AddParticipantsLive` `:2356`; `RemoveParticipantLive` `:2556`; `RestoreParticipantLive` `:2585`; `DeleteAssessmentPeserta` `:2621`; `RemoveParticipantCoreAsync` `:2667`; `DeriveUserStatus` `:3241`; `AssessmentMonitoringDetail` action `:3800` (+RemovedAt==null `:3807`, +ViewBag.AssessmentBatchKey `:3942`); broadcast precedents `:3732` (workerAnswerEdited), `:4351` (workerSubmitted), `:4905` (examClosed Clients.User), `:4986` (examClosed batch).
- `Hubs/AssessmentHub.cs` — `[Authorize]` `:9`; `JoinBatch` guard RemovedAt `:30`; `JoinMonitor`/`LeaveMonitor` role-gate `:43-64`.
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — SignalR handlers `:1199-1411`; `flashRow` `:1249`; `updateSummaryFromDOM` `:1258`; `getToken` `:811`; `buildActionsHtml` `:832`; `statusBadgeClass/Label` `:817/:826`; `updateRow` (tds[6]=Aksi) `:893`; addExtraTime AJAX pattern `:1418`; card-header (+Tambah Peserta) `:187`; table cols/dropdown `:220-372`; `#antiforgeryForm/#hTitle/#hCategory/#hScheduleDate` `:449-454`; `window.assessmentBatchKey` `:1200`; JoinMonitor/rejoin `:1219-1245`.
- `Views/CMP/StartExam.cshtml` — `examClosed` handler `:1254`; `examClosedModal` `:337`; `examClosed` var `:1249`; `timerInterval`/`saveInterval` `:489/:812`; sessionReset modal `:360`.
- `wwwroot/js/assessment-hub.js` — `window.showAssessmentToast` `:98`.
- `.planning/config.json` — `nyquist_validation: true`; `security_enforcement` absent (enabled).
- `HcPortal.Tests/` — `MonitoringUserStatusTests.cs`, `FlexibleParticipantAddLiveTests.cs`, `FlexibleParticipantRemoveTests.cs` (xUnit patterns); csproj net8.0/xunit 2.9.3.

### Secondary (MEDIUM — planning docs)
- `412-CONTEXT.md` (D-01..D-04 locked), `412-UI-SPEC.md` (design contract markup/copy), spec `2026-06-19-flexible-add-remove-participant-design.md` (§C/§D/§F), `410-01-SUMMARY.md`, `411-01-SUMMARY.md`, `.planning/ROADMAP.md` (Phase 412 details), `.planning/REQUIREMENTS.md`.

### Tertiary (LOW)
- None — semua klaim teknis cross-verified ke source file:line.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua existing, nol install, file:line VERIFIED.
- Architecture (broadcast/force-kick/DOM patterns): HIGH — 3 preseden broadcast langsung di codebase + examClosed pattern identik.
- Pitfalls: HIGH — derived dari pembacaan kode aktual (label Indonesia, selector summary, XSS carry-forward 409/411).
- Validation: MEDIUM — framework+file VERIFIED; cakupan e2e 412 vs 413 = keputusan planner (CONTEXT izinkan minimal).

**Research date:** 2026-06-21
**Valid until:** ~2026-07-21 (codebase stabil; risiko hanya bila 410/411 endpoint signature diubah sebelum 412 dieksekusi — re-verify file:line bila controller di-edit lagi)
