# Phase 412: Live Monitoring UI + SignalR - Pattern Map

**Mapped:** 2026-06-21
**Files analyzed:** 5 (4 modified existing + 1 new test, opsional)
**Analogs found:** 5 / 5 (semua exact-match in-file)

> **Karakter fase:** integrasi UI + SignalR murni di atas backend 409/410/411 yang sudah lengkap. **Hampir NOL kode baru.** 95% = merangkai komponen existing yang sudah VERIFIED file:line. Satu-satunya kode **genuinely-new** = query `removedSessions` (`RemovedAt != null`) di action `AssessmentMonitoringDetail`. Sisanya = broadcast 3-baris (reuse `_hubContext`), markup modal/panel (copy kelas Bootstrap existing), handler JS (mirror handler existing). migration=FALSE.

---

## File Classification

| File (modified/new) | Role | Data Flow | Closest Analog | Match Quality |
|---------------------|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` (broadcast wiring 410/411) | controller | event-driven (server→client push) | self — broadcast preseden `:4351` (`workerSubmitted`), `:4905` (`examClosed` Clients.User) | **exact (in-file)** |
| `Controllers/AssessmentAdminController.cs` (query removed-sessions di action) | controller | request-response (read query) | self — exclude-query `:3807` (`RemovedAt == null`) + remover-name resolve pattern `:2387` | **exact (in-file, kebalikan query)** |
| `Hubs/AssessmentHub.cs` | hook/provider (SignalR hub) | pub-sub (group membership) | self — `JoinMonitor`/`LeaveMonitor` `:43`/`:61` (group `monitor-{batchKey}` SUDAH ada) | **exact — NO CHANGE needed** |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` (modal+panel+handlers) | component (Razor + vanilla JS) | event-driven + request-response | self — handlers `workerStarted`/`workerSubmitted`/`workerAnswerEdited` `:1301`/`:1318`/`:1383`; helpers `buildActionsHtml`:832/`flashRow`:1249/`updateSummaryFromDOM`:1258/`getToken`:811; AJAX `addExtraTime`:1418; modal `akhiriSemuaModal` + dropdown `:322` | **exact (in-file)** |
| `Views/CMP/StartExam.cshtml` (examRemoved handler + modal) | component (Razor + vanilla JS) | event-driven (server→client) | self — `examClosed` handler `:1254` + `examClosedModal` `:337` | **exact (mirror in-file)** |
| `tests/e2e/flexible-participant-412.spec.ts` (opsional smoke) | test | — | `tests/e2e/*.spec.ts` (33 spec existing) | role-match |

---

## Pattern Assignments

### `Controllers/AssessmentAdminController.cs` — Broadcast wiring (controller, event-driven)

**Analog:** self, broadcast preseden `:4350-4359` (`workerSubmitted`) + `:4905` (`examClosed` Clients.User).

**KRITIS (jangan terlewat):** `_hubContext` **SUDAH ter-inject** — field `:27`, ctor param `:41`, assignment `:51`. **JANGAN tambah constructor param** (duplikat → compile error). Endpoint 410/411 tinggal pakai `_hubContext`.

**batchKey format** (VERIFIED identik di seluruh codebase `:3942`, `:4350`):
```csharp
var batchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
```

**Broadcast ke grup monitor — preseden `:4350-4359`** (copy struktur ini):
```csharp
var fbatchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"monitor-{fbatchKey}").SendAsync("workerSubmitted", new
{
    sessionId,
    workerName = session.User?.FullName ?? "Unknown",
    score = finalPercentage,
    result = isPassed ? "Pass" : "Fail",
    status = AssessmentConstants.AssessmentStatus.Completed,
    nomorSertifikat = updatedSession?.NomorSertifikat
});
```

**Force-kick ke worker tunggal — preseden `:4905`** (copy untuk `examRemoved`):
```csharp
await _hubContext.Clients.User(session.UserId).SendAsync("examClosed", new { reason = "hc_closed" });
```

**Aplikasi 412 (3 titik insert, semua POST-commit):**

1. **`AddParticipantsLive`** — GANTI komentar D-04 `:2480-2481` ("JANGAN sentuh _hubContext") dengan loop broadcast `participantAdded` SETELAH commit (commit ada di blok transaksi `:2455-2459`; notif sudah di luar tx `:2469-2478` → broadcast di samping notif). Payload per `createdSessions`: `{ sessionId=s.Id, userId=s.UserId, fullName, nip, status=s.Status }` (re-pakai `userDictionary` `:2485-2486`).
2. **`RemoveParticipantLive`** — GANTI komentar D-03 `:2576` dengan broadcast `participantRemoved` ke grup SETELAH `outcome.Ok` true. Payload WAJIB sertakan `mode = outcome.Mode` (`"hard"`/`"soft"`, struct `:2649`) — client cabang hard→`tr.remove()` vs soft→panel (Pitfall 6). Tambah `examRemoved` ke `Clients.User(session.UserId)` HANYA bila target InProgress (lihat kondisi di bawah). Server set `TempData["Error"] = "Anda telah dikeluarkan dari ujian ini."` bukan relevan di sini (worker redirect via JS; TempData di-set bila ada jalur full-page).
3. **`RestoreParticipantLive`** — GANTI komentar D-03 `:2611` dengan broadcast `participantAdded` SETELAH `SaveChangesAsync` `:2606` (baris balik live ke tabel aktif).

**Kondisi kirim `examRemoved` (Open Question #1, rekomendasi research):** kirim HANYA bila sesi benar-benar di tengah ujian — `session.StartedAt != null && session.CompletedAt == null` (cek SEBELUM mutasi soft di `RemoveParticipantCoreAsync`, atau capture flag sebelum core dipanggil). Completed-cert tak perlu kick (worker sudah tak di halaman ujian). InProgress selalu jatuh ke jalur SOFT (`SessionHasDataAsync` `:2659` return true bila `StartedAt != null`) → broadcast aman setelah commit.

**Pre/Post payload (Open Question #2, rekomendasi research):** broadcast 2 event `participantRemoved` (satu per sesi ter-soft-remove) — `outcome.PartnerId` `:2651` tersedia. Monitoring Detail biasanya difilter per `assessmentType` (Pre ATAU Post per layar) → 2 event jamin kedua baris konsisten lintas-tab.

**Anti-pattern (WAJIB hindari):** broadcast SEBELUM `CommitAsync` → notif untuk tx rollback (spec §G). Semua broadcast HARUS post-commit.

---

### `Controllers/AssessmentAdminController.cs` — Query removed-sessions (controller, request-response) — **SATU-SATUNYA KODE GENUINELY-NEW**

**Analog:** self, exclude-query `:3802-3807` (action `AssessmentMonitoringDetail`) + remover-name resolve `:2387-2389` (dictionary lookup pattern).

**Exclude-query existing `:3802-3807`** (panel = KEBALIKAN-nya):
```csharp
var query = _context.AssessmentSessions
    .Include(a => a.User)
    .Where(a => a.Title == title
             && a.Category == category
             && a.Schedule.Date == scheduleDate.Date
             && a.RemovedAt == null);   // v32.5 Phase 409 PLIV-01 — exclude removed
```

**Query baru (sisipkan sebelum `ViewBag.AssessmentBatchKey` `:3942` atau sebelum `return View(model)` `:3984`):**
```csharp
var removedSessions = await _context.AssessmentSessions
    .Include(a => a.User)
    .Where(a => a.Title == title && a.Category == category
             && a.Schedule.Date == scheduleDate.Date
             && a.RemovedAt != null)                    // <-- KEBALIKAN exclude-query :3807
    .OrderByDescending(a => a.RemovedAt)
    .ToListAsync();
// Resolve RemovedBy userId → FullName (kolom panel "Oleh") — pola dictionary :2387-2389
var removerIds = removedSessions.Where(s => s.RemovedBy != null).Select(s => s.RemovedBy!).Distinct().ToList();
var removerMap = await _context.Users.Where(u => removerIds.Contains(u.Id))
    .ToDictionaryAsync(u => u.Id, u => u.FullName);
ViewBag.RemovedSessions = removedSessions.Select(s => new {
    s.Id, FullName = s.User?.FullName ?? "Unknown", Nip = s.User?.NIP ?? "",
    s.RemovedAt,
    RemovedByName = s.RemovedBy != null && removerMap.TryGetValue(s.RemovedBy, out var n) ? n : (s.RemovedBy ?? "—"),
    s.RemovalReason
}).ToList();
```

> **Planner pertimbangkan:** ekstrak ke ViewModel typed (mis. `RemovedParticipantViewModel`) ketimbang anonymous ViewBag — konsisten dgn pola `MonitoringSessionViewModel` existing. **Proton guard:** panel tak relevan untuk `Category == "Assessment Proton"` (spec §F) — guard render di view via `Model.Category != "Assessment Proton"`.

---

### `Hubs/AssessmentHub.cs` — **NO CHANGE NEEDED**

**Analog:** self, `JoinMonitor` `:43-59` + `LeaveMonitor` `:61-64`.

Group `monitor-{batchKey}` membership + role-gate (Admin/HC `:56`) + rejoin-after-reconnect (view `:1241`) **sudah lengkap**. Broadcast dikirim dari controller (`_hubContext`), BUKAN dari hub method (hub tak punya konteks commit). `participantAdded`/`participantRemoved`/`examRemoved` = server→client only; **tak ada hub method client-invokable** untuk event ini (anti-spoofing — V-Spoofing security). **Kemungkinan besar file ini TIDAK disentuh sama sekali.**

---

### `Views/Admin/AssessmentMonitoringDetail.cshtml` — Modal + panel + handlers (component, event-driven + request-response)

**Analog:** self — semua helper + handler + markup existing.

**1. Helper `getToken()` `:811-814`** (reuse verbatim tiap POST AJAX baru):
```javascript
function getToken() {
    var inp = document.querySelector('#antiforgeryForm input[name="__RequestVerificationToken"]');
    return inp ? inp.value : '';
}
```

**2. AJAX POST pattern — `addExtraTime()` `:1418-1452`** (template untuk Add/Remove/Restore; `appUrl()` WAJIB untuk sub-path):
```javascript
var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
fetch(appUrl('/Admin/AddExtraTime'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
    body: 'assessmentId=' + assessmentId + '&minutes=' + minutes + '&__RequestVerificationToken=' + encodeURIComponent(token)
}).then(function(r) { return r.json(); }).then(function(data) { ... });
```
> Untuk `userIds[]` array di `AddParticipantsLive`: kirim berulang `userIds=id1&userIds=id2` (MVC form-array binding; signature `List<string> userIds` `:2356`).

**3. Helper `buildActionsHtml(session, isPackageMode)` `:832-887`** — render kolom Aksi (col `tds[6]`) saat inject baris baru. **WAJIB diperluas**: tambah item "Hapus Peserta" (dropdown) + `data-status`/`data-has-cert`. Status-gate existing: Cancelled→no actions `:837`, InProgress→Akhiri Ujian `:849`, Completed→View Results `:870`.

**4. Helper `flashRow(tr, cssClass)` `:1249-1255`** — animasi flash setelah inject/update (`flash-update`/`flash-complete`).

**5. Helper `updateSummaryFromDOM()` `:1258-1281`** — **WAJIB DIUBAH** (Pitfall 2, lihat Shared Patterns). Selector existing `:1259` = `tbody tr[data-session-id]` (ikut hitung `#tbodyRemoved`).

**6. Handler SignalR existing (mirror untuk participantAdded/Removed):**
- `workerStarted` `:1301-1315` — pola: cari `tr` by sessionId, update cell, `flashRow`, `showAssessmentToast`, `updateSummaryFromDOM`, update `#last-updated-time`.
- `workerSubmitted` `:1318-1380` — pola inject/update multi-cell via `tds[i]` + dedup terminal-check.
- `workerAnswerEdited` `:1383-1410` — pola update cell + toast verbose.

**Handler `participantAdded` baru** (mirror `workerStarted`, + dedup Pitfall 4 + kasus restore dari panel):
```javascript
window.assessmentHub.on('participantAdded', function(data) {
    // 1. dedup: skip bila tr aktif sudah ada
    if (document.querySelector('tbody:not(#tbodyRemoved) tr[data-session-id="' + data.sessionId + '"]')) return;
    // 2. bila ada di #tbodyRemoved (restore) → pindahkan; 3. else inject baru (kolom + buildActionsHtml)
    // 4. flashRow(tr, 'flash-update'); 5. updateSummaryFromDOM(); 6. update #countRemoved bila dari panel
    // 7. showAssessmentToast(data.fullName + ' ditambahkan ke batch'); 8. update #last-updated-time
});
```

**Handler `participantRemoved` baru** (mode-aware Pitfall 6, XSS-safe Pitfall 3):
```javascript
window.assessmentHub.on('participantRemoved', function(data) {
    var tr = document.querySelector('tbody:not(#tbodyRemoved) tr[data-session-id="' + data.sessionId + '"]');
    if (!tr) return;
    if (data.mode === 'hard') { tr.remove(); }
    else {  // soft → pindah ke #tbodyRemoved; kolom Alasan/Nama via textContent (BUKAN innerHTML)
        var newTr = document.createElement('tr');
        newTr.setAttribute('data-session-id', data.sessionId);
        newTr.setAttribute('data-removed', 'true');
        // bangun 5 td; td-alasan.textContent = data.removalReason (XSS-safe)
        document.getElementById('tbodyRemoved').prepend(newTr);
        tr.remove();   // + tampilkan panel + #countRemoved++
    }
    updateSummaryFromDOM();
    window.showAssessmentToast(data.fullName + ' dikeluarkan dari batch');
});
```

**7. Markup card-header (+Tambah Peserta) `:187-216`** — sisipkan `#btnTambahPeserta` di `<div class="d-flex gap-2 align-items-center">` sebelah Ekspor/Akhiri Semua. Conditional `Model.Category != "Assessment Proton"`.

**8. Markup dropdown per-row `:313-369`** — sisipkan `<li>` "Hapus Peserta" di `<ul class="dropdown-menu dropdown-menu-end">` (bawah Reset `:333`, samping Akhiri Ujian `:344`). `data-status="@session.UserStatus"`, `data-has-cert` dari `session.NomorSertifikat`.

**9. Modal picker + 2 modal hapus + panel** — copy kelas dari `#akhiriSemuaModal` (header `bg-danger text-white`) + token-card/extra-time-card (`card border-0 shadow-sm`). Full markup di UI-SPEC §2/§4/§5/§6. Modal keras = `data-bs-backdrop="static" data-bs-keyboard="false"`.

**Server-render panel (Pitfall 5, rekomendasi LOCKED-ish):** server SELALU render `#panelPesertaDikeluarkan` (collapsed, mungkin kosong) untuk batch non-Proton → JS tak perlu bangun panel from scratch; hindari null-error first-remove.

---

### `Views/CMP/StartExam.cshtml` — examRemoved handler + modal (component, event-driven)

**Analog:** self — `examClosed` handler `:1254-1284` + `examClosedModal` `:337-357` (mirror VERBATIM struktur).

**Handler `examClosed` existing `:1254-1284`** (copy → `examRemoved`):
```javascript
var examClosed = false;   // var existing :1249 — dual-trigger guard, REUSE
window.assessmentHub.on('examClosed', function(payload) {
    if (examClosed) return;
    examClosed = true;
    clearInterval(timerInterval);   // var existing :489
    clearInterval(saveInterval);    // var existing :812
    window.onbeforeunload = null;
    // ... set reason text
    var redirectTarget = '@Url.Action("Results", "CMP")/' + SESSION_ID;
    var modal = new bootstrap.Modal(document.getElementById('examClosedModal'));
    modal.show();
    // countdown 5s → window.location.href = redirectTarget (:1274-1283)
});
```

**Mirror untuk 412** — handler `examRemoved` (sisipkan setelah `examClosed` ~:1284): reuse var `examClosed` `:1249` sebagai guard, `clearInterval(timerInterval/saveInterval)` `:489`/`:812`, redirect ke `@Url.Action("Assessment", "CMP")` (BUKAN Results), modal `#examRemovedModal` baru. Full handler di UI-SPEC §9.

**Modal `examClosedModal` existing `:337-357`** (mirror → `#examRemovedModal`, sisipkan setelah `#sessionResetModal` `:359`): `data-bs-backdrop="static" data-bs-keyboard="false"`, `style="z-index: 9999;"`, header `bg-warning`→ganti `bg-danger text-white`, countdown 5 detik. Full markup di UI-SPEC §9. Tampilkan `payload.reason` via `textContent` (XSS-safe).

---

## Shared Patterns

### Antiforgery (semua POST AJAX baru)
**Source:** `getToken()` view `:811` + `#antiforgeryForm` hidden form existing.
**Apply to:** Add/Remove/Restore fetch.
```javascript
var token = getToken();   // single-source #antiforgeryForm
// body: '...&__RequestVerificationToken=' + encodeURIComponent(token)
```
Endpoint 410/411 sudah `[ValidateAntiForgeryToken]` (`:2355`/`:2555`/`:2583`) + `[Authorize(Roles="Admin, HC")]` — RBAC/CSRF dijaga server. UI tinggal kirim token.

### URL sub-path-aware
**Source:** `appUrl(path)`/`basePath` (global `assessment-hub.js`).
**Apply to:** SEMUA fetch URL (deploy `/KPB-PortalHC`). Hardcode `/Admin/...` rusak di sub-path (pelajaran PXF-01).

### Toast notifikasi
**Source:** `window.showAssessmentToast(msg, ...)` (`assessment-hub.js:98`).
**Apply to:** setelah add/remove/restore sukses. Konsisten dgn semua handler existing.

### Broadcast post-commit guard
**Source:** notif post-commit `AddParticipantsLive` `:2469-2478` (loop setelah blok tx commit).
**Apply to:** SEMUA broadcast 410/411. HANYA setelah `CommitAsync()`/`SaveChangesAsync()` sukses (spec §G; hindari notif rollback).

### batchKey / group name format
**Source:** `$"{Title}|{Category}|{Schedule.Date:yyyy-MM-dd}"` (`:3942`, `:4350`) + `window.assessmentBatchKey` (view `:1242`).
**Apply to:** `Clients.Group($"monitor-{batchKey}")` controller-side.

---

## ⚠ Pitfalls — WAJIB dibaca planner (mitigasi dibakukan ke plan)

### Pitfall 1 — Label status Indonesia (BUKAN "Cancelled"/"Completed" English)
`DeriveUserStatus` `:3243-3253` RETURN **`"Dibatalkan"`** (bukan "Cancelled") + **`"Menunggu Penilaian"`** (bukan "PendingGrading"); `Completed`/`InProgress`/`Abandoned`/`Not started` tetap English. **Logic tingkat-modal D-01:** cek `data-status === "InProgress"` (English, aman) ATAU (`data-status === "Completed"` && has-cert). `"Dibatalkan"`/`"Menunggu Penilaian"`/`"Not started"`/`"Abandoned"` → jalur ringan. `data-status` baris server diisi dari `session.UserStatus` (hasil `DeriveUserStatus`). **UI-SPEC §3 contoh `"Cancelled"` = ilustrasi, BUKAN nilai literal.**

### Pitfall 2 — `updateSummaryFromDOM()` ikut hitung baris panel removed
Selector existing `:1259` = `document.querySelectorAll('tbody tr[data-session-id]')` → match SEMUA tbody termasuk `#tbodyRemoved` → `count-total` over-count. **Ubah ke** `tbody:not(#tbodyRemoved) tr[data-session-id]` (atau filter `:not([data-removed])`). `count-total` = peserta aktif saja (selaras exclude-query 409 server-side `:3807`). Pastikan `#tbodyRemoved` id unik.

### Pitfall 3 — Stored XSS via `RemovalReason` bebas-teks
Reason masuk DB tanpa sanitasi (server validasi non-kosong saja). **Render:** Razor `@removalReason`/`Html.Encode()` (auto-encode); JS inject `cell.textContent = data.removalReason` (**BUKAN `innerHTML`**). Sama untuk `fullName`/`removedBy`. Carry T-409-10 + 411 SUMMARY `:108`. (Catatan: handler existing `workerStarted` `:1306` pakai `innerHTML` untuk badge status statis — aman karena bukan user-input; JANGAN tiru pola itu untuk reason/nama.)

### Pitfall 4 — Double-inject (echo SignalR + optimistic)
Admin pelaku ADD juga anggota grup → terima echo sendiri. **Andalkan echo sebagai sumber kebenaran** (jangan inject dari response POST). Dedup: `querySelector('tbody:not(#tbodyRemoved) tr[data-session-id="X"]')` sebelum inject. Fallback: echo tak datang 3 detik setelah POST sukses → inject dari `added[]` (tetap dedup). UI-SPEC §2/§8.

### Pitfall 5 — Panel belum ada di DOM saat removed pertama
Render kondisional "hanya bila ada removed" → null-error first-remove. **Mitigasi:** server SELALU render `#panelPesertaDikeluarkan` (collapsed, kosong) untuk batch non-Proton. Panel visible setelah ≥1 baris.

### Pitfall 6 — `participantRemoved` mode hard vs soft
Endpoint 411 return `mode: "hard"|"soft"|"noop"` (struct `RemoveOutcome.Mode` `:2649`). Hard (not-started, 0 response) → `tr.remove()` saja (tak bisa restore). Soft → arsip ke `#tbodyRemoved` (+Restore). **Payload broadcast WAJIB sertakan `mode`** (dari `outcome.Mode`).

---

## No Analog Found

Tidak ada. **Semua file punya analog exact in-file** (VERIFIED). Satu-satunya kode genuinely-new (query `removedSessions`) adalah **kebalikan langsung** dari exclude-query existing `:3807` + reuse pola dictionary-resolve `:2387`.

---

## Metadata

**Analog search scope:** `Controllers/AssessmentAdminController.cs`, `Hubs/AssessmentHub.cs`, `Views/Admin/AssessmentMonitoringDetail.cshtml`, `Views/CMP/StartExam.cshtml`, `wwwroot/js/assessment-hub.js`.
**Files scanned:** 5 (semua VERIFIED via Read di line yang dikutip).
**Endpoint 410/411 dependency:** `AddParticipantsLive` `:2356` (defer broadcast `:2480`), `RemoveParticipantLive` `:2556` (defer `:2576`), `RestoreParticipantLive` `:2585` (defer `:2611`), `RemoveParticipantCoreAsync` `:2667` (RemoveOutcome `:2645`, has-data `:2657`), `DeriveUserStatus` `:3241`, action `AssessmentMonitoringDetail` `:3800` (exclude `:3807`, batchKey `:3942`).
**migration:** FALSE (kolom `RemovedAt`/`RemovedBy`/`RemovalReason` sudah ada dari 409). Panel hanya MEMBACA.
**Pattern extraction date:** 2026-06-21
