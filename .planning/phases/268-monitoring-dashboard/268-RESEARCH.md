# Phase 268: Monitoring Dashboard - Research

**Researched:** 2026-03-28
**Domain:** ASP.NET Core MVC — SignalR real-time monitoring, UAT browser testing
**Confidence:** HIGH

## Summary

Phase 268 adalah fase UAT terakhir dari milestone v10.0. Tujuannya adalah menguji monitoring dashboard assessment di server development (http://10.55.3.3/KPB-PortalHC/) dari perspektif Admin/HC — memastikan progress real-time, lifecycle status, timer/elapsed, dan hasil setelah worker submit semua berfungsi benar.

Infrastruktur monitoring sudah sepenuhnya diimplementasi: controller (`AssessmentMonitoring` dan `AssessmentMonitoringDetail` di `AdminController.cs`), view (`AssessmentMonitoringDetail.cshtml`), hub (`AssessmentHub.cs`), dan event push SignalR (`workerStarted`, `progressUpdate`, `workerSubmitted`). Pola UAT sama dengan fase sebelumnya (265-267): user test di browser → Claude analisa kode → fix batch di project lokal.

Tidak ada fitur baru yang perlu dibangun. Fokus adalah menemukan bug di implementasi yang sudah ada melalui UAT dua browser bersamaan.

**Primary recommendation:** Jalankan UAT dua-browser (admin monitor + worker ujian) untuk verifikasi 4 requirement MON secara berurutan, dokumentasikan bug, fix batch di project lokal.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Test full real-time dengan 2 browser bersamaan — 1 browser worker mengerjakan soal, 1 browser admin buka monitoring dashboard. Verifikasi progress update otomatis tanpa refresh (SignalR push).
- **D-02:** Jika SignalR real-time tidak berfungsi, catat sebagai bug. Fallback: test dengan manual refresh untuk verifikasi data tetap benar.
- **D-03:** Kombinasi data existing + ujian baru: data existing dari Phase 264-267 untuk verifikasi session Completed menampilkan skor & pass/fail (MON-04), 1 worker mulai ujian baru supaya bisa test lifecycle lengkap Open → InProgress → Completed secara real-time (MON-02).
- **D-04:** Pakai akun yang sama dari Phase 264: admin@pertamina.com (monitor) dan rino.prasetyo@pertamina.com (worker).
- **D-05:** Verifikasi eyeball — buka monitoring, pastikan timer/elapsed tampil, bergerak, tidak nol/negatif. Tidak perlu perbandingan eksak milidetik antara worker dan monitor.
- **D-06:** Sama dengan fase sebelumnya: verifikasi manual oleh user di browser, Claude analisa kode dan catat potensi bug.
- **D-07:** Alur: jalankan semua skenario → kumpulkan bug → fix batch di project lokal.

### Claude's Discretion
- Urutan langkah test spesifik (Claude tentukan berdasarkan analisa kode monitoring)
- Detail query/check apa yang perlu dijalankan untuk verifikasi data

### Deferred Ideas (OUT OF SCOPE)
(none)
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| MON-01 | Progress real-time (x/total soal terjawab) | SignalR `progressUpdate` event push dari `SaveAnswer` di CMPController; view handler mengupdate `.progress-cell`; polling tidak ada — murni push |
| MON-02 | Status lifecycle (Open → InProgress → Completed) | `workerStarted` push saat StartExam pertama kali; `workerSubmitted` push saat submit; status badge diupdate live tanpa refresh |
| MON-03 | Timer/elapsed akurat dan sinkron dengan worker | Dua mekanisme: (1) `data-started-at` + `data-duration` di-parse saat load halaman → countdown lokal; (2) `remainingSeconds` dari server saat polling update. Tidak ada server-push khusus untuk timer — hitung client-side dari StartedAt |
| MON-04 | Result menampilkan skor & pass/fail setelah submit | `workerSubmitted` payload berisi `{ score, result, status, totalQuestions }`; view langsung update col 3 (Score), col 4 (Result) tanpa reload |
</phase_requirements>

---

## Architecture Patterns

### Arsitektur Monitoring yang Sudah Ada

```
[Worker Browser]                    [Admin Browser]
   |                                     |
   | StartExam (HTTP)                    |
   |→ CMPController.StartExam()          |
   |   → push "workerStarted" ──────────→| AssessmentMonitoringDetail.cshtml
   |                                     |   handler: updateStatusCell("InProgress")
   |                                     |
   | SaveAnswer (HTTP)                   |
   |→ CMPController.SaveAnswer()         |
   |   → push "progressUpdate" ─────────→| handler: update .progress-cell "x/total"
   |                                     |
   | ExamSummary POST (HTTP)             |
   |→ CMPController.ExamSummary()        |
   |   → push "workerSubmitted" ─────────→| handler: update Score%, Result Pass/Fail,
   |                                     |           Status=Completed, hide Akhiri btn
   |                                     |
   [AssessmentHub.cs — SignalR]          |
   JoinBatch(batchKey) ← worker         |
   JoinMonitor(batchKey) ←──────────────|
```

### SignalR Events — Mapping Lengkap

| Event | Dikirim oleh | Diterima di | Payload | Fungsi |
|-------|-------------|-------------|---------|--------|
| `workerStarted` | CMPController.StartExam (justStarted=true) | monitor group | `{ sessionId, workerName, status }` | MON-02: update badge InProgress |
| `progressUpdate` | CMPController.SaveAnswer | monitor group + worker user | `{ sessionId, progress, totalQuestions }` | MON-01: update x/total |
| `workerSubmitted` | CMPController.ExamSummary (package path) | monitor group | `{ sessionId, workerName, score, result, status, totalQuestions }` | MON-02+MON-04: Completed + skor |
| `sessionReset` | AdminController.ResetAssessment | worker user | `{ reason }` | Bukan untuk monitor |
| `examClosed` | AdminController.AkhiriUjian/AkhiriSemuaUjian | worker user / batch group | `{ reason }` | Bukan untuk monitor |

### Timer di Monitoring View (MON-03)

Dua lapisan timer yang ada:

1. **Load-time countdown** (baris `updateTimeRemaining()`):
   - Membaca `data-started-at` (ISO string) dan `data-duration` (menit) dari setiap `<tr>`
   - Hitung sisa waktu: `(startedAt + durationMs) - Date.now()`
   - Tampil format "Xm Ys" di kolom `.timeremaining-cell`
   - `setInterval(updateTimeRemaining, 1000)` — update setiap detik
   - Jika waktu habis: tampil "Waktu Habis" dengan class `text-danger`

2. **Server-synced countdown** (dari `countdownMap` dalam IIFE):
   - `countdownMap[sessionId] = session.remainingSeconds` saat `updateRow()` dipanggil
   - `tickCountdowns()` kurangi 1 detik setiap detik
   - Di-sync ulang dari server hanya saat ada polling (tidak ada polling endpoint — hanya pada `workerStarted`/`workerSubmitted` yang tidak membawa remainingSeconds)

**Potensi bug yang perlu di-verifikasi:**
- `workerStarted` payload tidak berisi `remainingSeconds` — timer di kolom "Time Remaining" untuk worker yang baru mulai mungkin tidak muncul sampai halaman di-refresh, karena `countdownMap` belum terisi. Load-time countdown (`updateTimeRemaining`) bergantung pada `data-started-at` yang diisi saat server render — jika worker mulai SETELAH halaman monitoring dimuat, `data-started-at` di `<tr>` akan kosong (karena `StartedAt` belum ada saat render).
- `workerSubmitted` juga tidak memperbarui `data-started-at` pada `<tr>`, tapi itu tidak masalah karena status sudah Completed.

### Potensi Bug MON-01: progressUpdate di non-package path

`workerSubmitted` hanya dikirim dari **package path** (ada pengecekan `if (AssessmentPackages.Any())`). Jika assessment OJT menggunakan **non-package path** (soal langsung, bukan via paket), `workerSubmitted` mungkin tidak dikirim, sehingga MON-04 tidak akan update secara real-time.

Perlu cek: apakah assessment test di server dev pakai package mode atau non-package mode?

### Potensi Bug MON-02: workerStarted tidak update timer

`workerStarted` handler hanya update status badge, tidak update `data-started-at` attribute di `<tr>` dan tidak mengisi `countdownMap`. Akibatnya, timer "Time Remaining" untuk worker yang baru mulai tidak akan muncul sampai halaman di-refresh.

### Potensi Bug MON-03: Timer sinkronisasi

Setelah Admin buka halaman monitoring, timer hanya berjalan dari StartedAt yang ada di server render. Jika Admin buka monitoring setelah worker sudah mulai ujian, `data-started-at` akan terisi dan timer berjalan. Tapi jika timing tidak sinkron (clock skew antara client dan server), nilai timer bisa berbeda beberapa detik dari yang dilihat worker — ini acceptable per D-05 (tidak perlu akurasi milidetik).

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Real-time push | Polling endpoint baru | SignalR sudah ada di AssessmentHub.cs | Infrastructure sudah ada, hub sudah terhubung |
| Progress counter | Custom endpoint baru | `progressUpdate` event yang sudah ada | Sudah push dari CMPController.SaveAnswer |
| Status update | DB polling | `workerStarted`/`workerSubmitted` yang sudah ada | Sudah diimplementasi |

---

## Common Pitfalls

### Pitfall 1: SignalR JoinMonitor tidak terpanggil
**What goes wrong:** Hub status badge menampilkan "Live" tapi update tidak masuk ke admin browser.
**Why it happens:** `JoinMonitor` dipanggil melalui `assessmentHubStartPromise.then()` — jika promise gagal atau `window.assessmentBatchKey` tidak di-set oleh view, admin tidak masuk ke grup `monitor-{batchKey}`.
**How to avoid:** Verifikasi di browser console bahwa `window.assessmentBatchKey` terdefinisi dan `JoinMonitor` berhasil dipanggil.
**Warning signs:** Badge "Live" tapi tidak ada update saat worker menjawab soal.

### Pitfall 2: workerSubmitted hanya untuk package mode
**What goes wrong:** Setelah worker submit, monitoring tidak update real-time (Score dan Result tetap "—").
**Why it happens:** `workerSubmitted` hanya dikirim di blok `if (packageMode)` di ExamSummary handler.
**How to avoid:** Verifikasi apakah assessment test menggunakan package mode. Jika non-package, catat sebagai bug MON-04.
**Warning signs:** Score dan Result tidak update setelah worker submit tanpa refresh.

### Pitfall 3: Timer tidak muncul untuk worker yang mulai setelah monitoring dibuka
**What goes wrong:** Kolom "Time Remaining" tetap "—" untuk worker InProgress yang baru mulai.
**Why it happens:** `data-started-at` di `<tr>` kosong (diisi saat server render sebelum StartedAt ada), dan `countdownMap` tidak diisi oleh `workerStarted` event.
**How to avoid:** Refresh halaman monitoring setelah worker mulai untuk mendapatkan data StartedAt yang benar.
**Warning signs:** Worker sudah InProgress, timer di worker berjalan, tapi kolom "Time Remaining" di monitoring tetap "—".

### Pitfall 4: Dua browser beda akun diperlukan
**What goes wrong:** Test dilakukan di satu browser — perubahan status tidak terlihat real-time.
**Why it happens:** SignalR hub memakai `Context.UserIdentifier` untuk routing. Satu user tidak bisa sekaligus jadi worker dan monitor.
**How to avoid:** Per D-01 dan D-04 — gunakan akun berbeda: admin@pertamina.com (monitor) dan rino.prasetyo@pertamina.com (worker), di dua browser atau dua tab browser berbeda.

---

## Code Examples

### Cara JoinMonitor dipanggil (dari AssessmentMonitoringDetail.cshtml)

```javascript
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml — @section Scripts
window.assessmentHubStartPromise.then(function() {
    if (monBadge) { monBadge.className = 'badge bg-success ms-1 small'; monBadge.textContent = 'Live'; }
    if (window.assessmentBatchKey) {
        window.assessmentHub.invoke('JoinMonitor', window.assessmentBatchKey)
            .catch(function(err) { console.warn('[monitor] JoinMonitor failed:', err); });
    }
});
// window.assessmentBatchKey = '{title}|{category}|{scheduleDate:yyyy-MM-dd}'
// ViewBag.AssessmentBatchKey diset di AdminController.AssessmentMonitoringDetail()
```

### Handler progressUpdate di monitoring view

```javascript
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml
window.assessmentHub.on('progressUpdate', function(data) {
    var tr = document.querySelector('tr[data-session-id="' + data.sessionId + '"]');
    if (!tr) return;
    var progressCell = tr.querySelector('.progress-cell');
    if (progressCell) {
        progressCell.textContent = data.totalQuestions > 0
            ? (data.progress + '/' + data.totalQuestions)
            : '\u2014';
    }
    flashRow(tr, 'flash-update');
});
// Payload dari CMPController.SaveAnswer: { sessionId, progress, totalQuestions }
```

### Timer load-time di monitoring

```javascript
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml
function updateTimeRemaining() {
    document.querySelectorAll('tr[data-started-at]').forEach(function(row) {
        var startedAtStr = row.getAttribute('data-started-at');
        var durationMin = parseInt(row.getAttribute('data-duration') || '0', 10);
        if (!startedAtStr || durationMin === 0) return;
        var startedAt = new Date(startedAtStr);
        var durationMs = durationMin * 60000;
        var timeLeft = (startedAt.getTime() + durationMs) - Date.now();
        // ... render ke .timeremaining-cell
    });
}
setInterval(updateTimeRemaining, 1000);
// data-started-at diisi saat server render, HANYA jika session.UserStatus == "InProgress"
// dan session.StartedAt sudah ada. Jika worker mulai setelah halaman dirender, cell tetap kosong.
```

---

## Skenario UAT yang Direkomendasikan

Berdasarkan analisa kode, urutan test yang optimal:

**Skenario A — MON-04: Data existing (session Completed)**
1. Admin buka `AssessmentMonitoring` — verifikasi assessment group dari Phase 264-267 muncul
2. Klik detail grup — verifikasi worker Completed menampilkan Score% dan Pass/Fail
3. Klik "View Results" untuk salah satu worker Completed — verifikasi halaman Results terbuka

**Skenario B — MON-01 + MON-02 + MON-03: Live test dua browser**
1. Admin buka `AssessmentMonitoringDetail` untuk assessment yang masih Open
2. Worker (rino) buka assessment dan mulai ujian → Admin verifikasi: status badge berubah InProgress (MON-02)
3. Worker jawab beberapa soal → Admin verifikasi: progress counter "x/total" update tanpa refresh (MON-01)
4. Admin verifikasi timer/elapsed tampil dan bergerak, tidak nol/negatif (MON-03)
5. Worker submit ujian → Admin verifikasi: status Completed, Score%, Pass/Fail muncul real-time (MON-02 + MON-04)

**Skenario C — Fallback jika SignalR tidak bekerja (per D-02)**
1. Jika ada update yang tidak muncul real-time, catat sebagai bug
2. Refresh halaman monitoring → verifikasi data tetap benar di DB (tidak perlu real-time)

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|-------------|-----------|---------|----------|
| Server development | Semua skenario UAT | ✓ | http://10.55.3.3/KPB-PortalHC/ | — |
| akun admin@pertamina.com | Monitor browser | ✓ | Dipakai sejak Phase 264 | — |
| akun rino.prasetyo@pertamina.com | Worker browser | ✓ | Dipakai sejak Phase 264 | — |
| Data assessment dari Phase 264-267 | MON-04 | ✓ | Assessment ID 10 (moch.widyadhana) confirmed Phase 267 | — |
| Browser 1 (admin) | Monitor | ✓ | — | — |
| Browser 2 / incognito (worker) | Worker | ✓ | — | — |

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UAT — tidak ada automated test untuk real-time SignalR |
| Config file | none |
| Quick run command | Manual browser verification |
| Full suite command | Manual browser verification (dua browser bersamaan) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MON-01 | Progress "x/total" update tanpa refresh | Manual browser | — (manual-only: membutuhkan dua browser bersamaan + real-time verification) | N/A |
| MON-02 | Status lifecycle Open→InProgress→Completed | Manual browser | — (manual-only: membutuhkan aksi real-time dari worker) | N/A |
| MON-03 | Timer/elapsed tampil, bergerak, tidak nol/negatif | Manual browser eyeball | — (manual-only: visual verification) | N/A |
| MON-04 | Skor dan pass/fail muncul setelah submit | Manual browser | — (manual-only: membutuhkan workflow submit lengkap) | N/A |

**Justifikasi manual-only:** Semua 4 requirement memerlukan interaksi real-time antara dua browser yang authenticated sebagai user berbeda (Admin dan Worker). Tidak ada cara otomatis yang praktis untuk menguji SignalR push dalam context ini tanpa infrastructure test tambahan yang out of scope untuk milestone UAT.

### Wave 0 Gaps
None — tidak ada test infrastructure yang perlu disiapkan. Semua verifikasi dilakukan manual.

---

## Open Questions

1. **Package mode vs non-package mode di server dev**
   - What we know: `workerSubmitted` push hanya dikirim di package path di CMPController
   - What's unclear: Apakah assessment OJT di server dev menggunakan package mode (soal via AssessmentPackages) atau non-package?
   - Recommendation: Cek saat UAT — jika non-package dan MON-04 gagal real-time, ini adalah bug yang perlu di-fix

2. **Assessment yang available untuk live test (MON-01, MON-02, MON-03)**
   - What we know: Assessment ID 10 dengan moch.widyadhana dipakai di Phase 267 (status terakhir: kemungkinan Completed atau reset)
   - What's unclear: Apakah ada assessment dengan status Open yang bisa dipakai untuk live test, atau perlu Admin buat/reset dulu
   - Recommendation: Rencana plan perlu mencakup langkah awal "verifikasi assessment tersedia / reset jika perlu"

---

## Sources

### Primary (HIGH confidence)
- `Controllers/AdminController.cs` — AssessmentMonitoring, AssessmentMonitoringDetail actions, GetActivityLog
- `Controllers/CMPController.cs` — SaveAnswer (progressUpdate push), StartExam (workerStarted push), ExamSummary (workerSubmitted push)
- `Hubs/AssessmentHub.cs` — JoinMonitor, JoinBatch, event routing
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — SignalR handler JS (progressUpdate, workerStarted, workerSubmitted), countdown logic
- `Views/Admin/AssessmentMonitoring.cshtml` — list view
- `Models/AssessmentMonitoringViewModel.cs` — MonitoringGroupViewModel, MonitoringSessionViewModel
- `wwwroot/js/assessment-hub.js` — connection setup, startHub, reconnect logic

### Secondary (MEDIUM confidence)
- `.planning/phases/268-monitoring-dashboard/268-CONTEXT.md` — keputusan implementasi dan test strategy
- `.planning/REQUIREMENTS.md` — definisi MON-01 sampai MON-04
- `.planning/STATE.md` — context dari Phase 264-267

---

## Metadata

**Confidence breakdown:**
- Arsitektur SignalR: HIGH — kode sudah ada dan bisa dibaca langsung
- Potensi bug: HIGH — identified dari analisa kode, bukan dari testing
- UAT skenario: HIGH — derived dari CONTEXT.md decisions dan kode
- Availability server dev: MEDIUM — diasumsikan sama seperti Phase 267 (confirmed running)

**Research date:** 2026-03-28
**Valid until:** 2026-04-04 (7 hari — phase UAT yang pendek)
