# Phase 232: Audit Assessment Flow — Worker Side - Research

**Researched:** 2026-03-22
**Domain:** ASP.NET Core MVC — CMP worker-side exam flow, SignalR, timer, scoring
**Confidence:** HIGH (semua temuan berasal dari kode aktual)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Audit + improve UX: filter Open/Upcoming/Completed, assignment matching, empty state, pagination, search
- **D-02:** Tambah status badge visual: Open (hijau), Upcoming (biru), Completed (abu-abu), Expired (merah)
- **D-03:** Improve empty state: pesan informatif 'Belum ada assessment yang ditugaskan'
- **D-04:** Audit search by judul dan pagination berfungsi benar
- **D-05:** Completed assessment: audit existing behavior, fix inkonsistensi
- **D-06:** Audit + improve UX: auto-focus input, paste support, clear error state, error messages jelas
- **D-07:** Audit akurasi + edge cases: timer sinkron dengan server ElapsedSeconds, auto-save per-click, browser tab switch handling, network disconnect mid-save, timer drift
- **D-08:** Saat timer habis: warning modal 'Waktu habis' dulu, user klik OK, baru submit
- **D-09:** beforeunload warning dialog + jawaban sudah auto-saved per-click
- **D-10:** Audit flow existing + fix bugs: Next/Prev/jump-to-question, LastActivePage terupdate, no data loss
- **D-11:** Full real-time implementation: semua HC actions (Reset, Force Close, Bulk Close) trigger real-time update di worker exam page via SignalR
- **D-12:** HC Reset saat worker di exam page: SignalR notify → modal 'Session di-reset oleh HC' → redirect ke assessment list
- **D-13:** Full state restore: ElapsedSeconds lanjut, LastActivePage restore, semua jawaban pre-populated, timer lanjut
- **D-14:** Network disconnect: auto-retry save saat reconnect, visual indicator 'Offline' / 'Tersimpan' di exam page
- **D-15:** Full audit scoring chain: score calculation, IsPassed logic, NomorSertifikat generation, competency level update, ElemenTeknis scoring
- **D-16:** Results page: audit + improve UX — highlight jawaban benar hijau/salah merah, section-by-section breakdown
- **D-17:** HC toggle 'allow review' berfungsi, jawaban benar/salah ditampilkan benar, score breakdown visible
- **D-18:** Deep audit Proton Tahun 1-2 exam reguler + Tahun 3 interview 5 aspek — kedua path end-to-end
- **D-19:** Deep audit Proton scoring: scoring per aspek, total score calculation, pass/fail threshold, NomorSertifikat generation
- **D-20:** Skip untuk sekarang — fokus fungsionalitas dan bug fix (Accessibility)

### Claude's Discretion
- Urutan audit per-action dalam setiap plan
- Detail level HTML report layout
- Pendekatan fix (refactor vs patch)
- Pembagian task antar plans

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| AFLW-01 | Worker melihat daftar assessment (Open/Upcoming) sesuai assignment | Assessment.cshtml + CMPController Assessment() action sudah diaudit — temuan ada di bagian Audit Findings |
| AFLW-02 | StartExam flow benar (token entry → exam page → timer → auto-save per-click) | StartExam.cshtml JS + CMPController StartExam/VerifyToken sudah diaudit penuh |
| AFLW-03 | SubmitExam menghasilkan score, IsPassed, NomorSertifikat (jika lulus), competency update | CMPController SubmitExam action sudah diaudit — scoring chain lengkap tersedia |
| AFLW-04 | Session resume berfungsi (ElapsedSeconds, LastActivePage, pre-populated answers) | Resume state ViewBag + prePopulateAnswers() JS sudah diaudit |
| AFLW-05 | Results page menampilkan score, pass/fail, answer review (jika diaktifkan HC) | Results.cshtml + AssessmentResultsViewModel sudah diaudit |
</phase_requirements>

---

## Summary

Phase 232 mengaudit worker-side exam flow secara end-to-end: daftar assessment, token entry, exam page, timer, auto-save, submit, scoring, session resume, dan results page. Kode aktual telah diaudit dari `Controllers/CMPController.cs`, `Views/CMP/Assessment.cshtml`, `Views/CMP/StartExam.cshtml`, `Views/CMP/Results.cshtml`, `Hubs/AssessmentHub.cs`, dan `wwwroot/js/assessment-hub.js`.

Secara umum arsitektur sudah solid. SignalR handler untuk `examClosed` dan `sessionReset` sudah **ada** di StartExam.cshtml — ini adalah temuan penting karena D-11/D-12 sebagian sudah diimplementasikan. Yang belum ada adalah: hub.js belum expose `assessmentHubStartPromise` ke worker page, timer expired langsung auto-submit tanpa modal warning (bertentangan D-08), status badge di Assessment list belum lengkap (Expired belum ada), dan join ke `batch-{batchKey}` dari worker exam page perlu diaudit untuk HC push.

**Primary recommendation:** Audit per-area, identifikasi gap vs keputusan D-01 s/d D-19, lakukan targeted fix. Tidak perlu refactor besar — mayoritas adalah patch-level.

---

## Standard Stack

### Core (sudah digunakan di project)
| Library | Versi (project) | Purpose | Why Standard |
|---------|-----------------|---------|--------------|
| ASP.NET Core MVC | .NET 8 | Controller + Views | Project stack |
| SignalR | built-in .NET 8 | Real-time push ke worker | Sudah ada AssessmentHub |
| Bootstrap 5 | CDN | Modal, badge, layout | Project UI framework |
| Bootstrap Icons | CDN | Icon set | Project icon set |
| Chart.js | CDN | Radar chart di Results | Sudah digunakan di Results.cshtml |
| jQuery | CDN | AJAX di Assessment.cshtml verifyToken | Sudah digunakan |

### Tidak Ada Dependency Baru
Seluruh pekerjaan Phase 232 dilakukan dalam stack yang sudah ada. Tidak perlu install package baru.

---

## Architecture Patterns

### Pattern 1: Exam Action Chain
```
Assessment list (GET /CMP/Assessment)
  → VerifyToken (POST /CMP/VerifyToken) — jika IsTokenRequired
  → StartExam (GET /CMP/StartExam?id=X)
  → ExamSummary POST (hidden form submit)
  → ExamSummary GET (review page)
  → SubmitExam (POST /CMP/SubmitExam)
  → Results (GET /CMP/Results?id=X)
```

### Pattern 2: Auto-Save Per-Click
Worker memilih jawaban → radio change event → `saveAnswerWithDebounce()` (300ms debounce) → `saveAnswerAsync()` dengan exponential backoff (0 → 1s → 3s) → `ShowSaveIndicator()` feedback. Jawaban juga tersimpan di `<input type="hidden" id="ans_{qId}">` untuk form submit.

### Pattern 3: Resume State
Server menyediakan ViewBag: `IsResume`, `LastActivePage`, `ElapsedSeconds`, `RemainingSeconds`, `ExamExpired`, `SavedAnswers` (JSON). Client JS membaca ini saat page load, pre-populate radio buttons, navigate ke `RESUME_PAGE`, dan lanjutkan timer dari `REMAINING_SECONDS_FROM_DB`.

### Pattern 4: SignalR Worker Push
```
HC Action di AdminController → _hubContext.Clients.Group("monitor-{batchKey}").SendAsync("examClosed"/"sessionReset")
Worker StartExam.cshtml → window.assessmentHub.on('examClosed', ...) / .on('sessionReset', ...)
```
Worker harus Join ke group `batch-{batchKey}` (bukan `monitor-{batchKey}`) untuk menerima push dari HC. JoinBatch dipanggil dari assessment-hub.js saat start.

### Pattern 5: Session Progress Update
`saveSessionProgress()` dipanggil setiap 30 detik + setiap page switch → POST ke `UpdateSessionProgress` → update `ElapsedSeconds` dan `LastActivePage` di DB.

### Pattern 6: Status-Guarded Write di SubmitExam
```csharp
var rowsAffected = await _context.AssessmentSessions
    .Where(s => s.Id == id && s.Status != "Completed")
    .ExecuteUpdateAsync(...)
if (rowsAffected == 0) → race condition handled
```
Pola ini sudah benar — mencegah double-submission.

---

## Audit Findings — Gap Analysis Per Area

### Area 1: Assessment List (AFLW-01 / D-01 s/d D-05)

**Yang sudah ada (verified OK):**
- Filter Open/Upcoming/InProgress via controller `.Where(a => a.Status == "Open" || a.Status == "Upcoming" || a.Status == "InProgress")`
- Tab filtering client-side: Open tab menampilkan `open` + `inprogress`, Upcoming tab menampilkan `upcoming`
- Search by title + category + FullName + NIP — sudah ada
- Pagination via `PaginationHelper.Calculate()` — sudah ada
- Empty state: pesan "No assessments assigned to you" sudah ada
- Auto-transition Upcoming → Open (display-only, no SaveChangesAsync) sudah ada
- Riwayat Ujian section (Completed + Abandoned) di bawah kartu — sudah ada

**Gap yang perlu difix:**
- **D-02 GAP:** Status badge untuk status `Open` sudah ada (`bg-success`), `Upcoming` ada (`bg-warning`). TAPI **tidak ada badge warna biru untuk Upcoming** — sekarang Upcoming pakai `bg-warning text-dark` (kuning). D-02 minta Upcoming = biru, Open = hijau. Perlu update `statusBadgeClass` di Assessment.cshtml.
- **D-02 GAP:** Status `Expired` belum ada badge — karena `Expired` bukan nilai Status di DB. Perlu diperjelas: apakah `Expired` adalah display-state dari `ExamWindowCloseDate` yang sudah lewat? Controller tidak menampilkan Expired ke worker list (filter hanya Open/Upcoming/InProgress). Perlu audit apakah ada kasus session dengan ExamWindowCloseDate lewat yang masih Open/Upcoming — jika ya, butuh display-side handling.
- **D-03 GAP:** Empty state sudah ada tapi pesan dalam Bahasa Inggris: "No assessments assigned to you". Perlu translate ke "Belum ada assessment yang ditugaskan untuk Anda."
- **D-05 GAP:** Completed assessment di Riwayat Ujian: badge untuk `IsPassed == null` (session FC tapi score null?) tidak dihandle — akan tampil sebagai "Tidak Lulus" padahal seharusnya "Tidak Diketahui" atau handle gracefully.

### Area 2: Token Entry (AFLW-02 / D-06)

**Yang sudah ada (verified OK):**
- Token modal dengan Bootstrap Modal
- AJAX verify via jQuery `$.ajax` → `/CMP/VerifyToken`
- Error state sudah ada (`#tokenError` alert div)
- Server-side token validation: uppercase comparison `assessment.AccessToken != token.ToUpper()`

**Gap yang perlu difix:**
- **D-06 GAP: auto-focus** — Modal tidak punya `shown.bs.modal` listener untuk auto-focus ke `#tokenInput`. Setelah modal terbuka, user harus klik input manual.
- **D-06 GAP: paste support** — Input sudah `type="text"`, paste HTML5 default works. TAPI `maxlength="6"` bisa memotong token yang di-paste jika ada spasi. Perlu tambah `input` event listener yang trim + uppercase otomatis.
- **D-06 GAP: Enter key** — Tidak ada `keydown` listener di `#tokenInput` untuk trigger verify saat Enter ditekan. User harus klik tombol.
- **D-06 GAP: error message bahasa** — Error "Invalid Token. Please check and try again." hardcoded Inggris di server response message. Perlu Indonesiakan.
- **D-06 GAP: clear error on new input** — Saat user mengetik token baru, `#tokenError` tidak disembunyikan otomatis. Error lama dari percobaan sebelumnya tetap tampil saat user mulai mengetik.

### Area 3: Timer & Auto-Save (AFLW-02 / D-07, D-08)

**Yang sudah ada (verified OK):**
- Timer countdown dari `REMAINING_SECONDS_FROM_DB` (sudah sinkron dengan server)
- Auto-save per-click dengan debounce 300ms
- Exponential backoff retry (attempt 0: 1s, attempt 1: 3s)
- `showSaveIndicator()` feedback: saving/saved/error states
- `showFailureToast()` setelah 3 kali gagal
- `saveSessionProgress()` setiap 30 detik + setiap page switch
- `window.onbeforeunload` warning dialog (D-09 sudah OK)

**Gap yang perlu difix:**
- **D-08 CRITICAL GAP:** Saat `timeRemaining <= 0`, kode langsung `document.getElementById('examForm').submit()` — **tidak ada warning modal dulu**. D-08 mengharuskan modal 'Waktu habis' → user klik OK → baru submit. Perlu tambah modal dan ubah timer expiry handler.
- **D-07 GAP: timer drift** — `setInterval(updateTimer, 1000)` rentan drift setelah tab diminimize atau laptop sleep. Perlu gunakan `Date.now()` sebagai anchor point untuk menghitung elapsed time yang akurat, bukan hanya decrement counter.
- **D-07 GAP: browser tab switch** — Tab switch detection (AINT-02) belum diimplementasikan. Ini requirement terpisah (AINT-02) yang belum di-scope Phase 232 — tapi perlu dokumentasikan bahwa `document.addEventListener('visibilitychange')` tidak ada di StartExam.cshtml.
- **D-14 GAP: network disconnect indicator** — `showSaveIndicator(qId, 'error')` sudah ada, tapi tidak ada persistent 'Offline' badge di header saat koneksi terputus. `hubStatusBadge` menunjukkan status SignalR (`Connecting...`/`Live`/`Reconnecting...`/`Disconnected`) — ini sudah ada. TAPI tidak ada indikator network disconnect untuk answer save failure yang persisten (hanya toast sementara 5 detik yang auto-dismiss).
- **D-14 GAP: auto-retry save saat reconnect** — `connection.onreconnected()` di assessment-hub.js hanya `JoinBatch` kembali + show toast. Tidak ada mekanisme untuk retry jawaban yang gagal tersimpan saat offline.

### Area 4: Exam Navigation (D-10)

**Yang sudah ada (verified OK):**
- `changePage()` dengan pending saves guard (poll setiap 50ms, timeout 5s)
- `performPageSwitch()` → `saveSessionProgress()` → `LogPageNav()` via SignalR
- Panel angka soal (answered = hijau, unanswered = abu-abu)
- Prev/Next button di tiap halaman
- ReviewSubmitBtn juga menunggu pending saves

**Gap yang perlu difix:**
- **D-10 GAP: jump-to-question** — Panel sidebar hanya menampilkan nomor soal di halaman saat ini sebagai badge read-only. Tidak ada tombol untuk jump ke halaman tertentu (nomor soal di halaman lain). Panel hanya menampilkan soal halaman aktif, bukan semua soal.
- **D-10 GAP: no global question overview** — User tidak bisa melihat semua soal yang sudah/belum dijawab sekaligus (hanya per-halaman). ExamSummary page sebenarnya menampilkan semua soal + status answered — link ke ExamSummary (untuk review) perlu dipertimbangkan.

### Area 5: SignalR Worker Push (D-11, D-12)

**Yang sudah ada (verified OK):**
- `window.assessmentHub.on('examClosed', ...)` — sudah ada, modal dengan countdown 5 detik
- `window.assessmentHub.on('sessionReset', ...)` — sudah ada, form disabled + modal + redirect ke Assessment list
- `JoinBatch(batchKey)` dipanggil dari assessment-hub.js saat connect + reconnect

**Gap yang perlu difix:**
- **D-11/D-12 GAP: HC push group mismatch** — HC actions di AdminController mengirim ke group `"worker-{sessionId}"` untuk per-worker signals. TAPI AssessmentHub hanya expose `JoinBatch` (group `"batch-{batchKey}"`) dan `JoinMonitor`. **Tidak ada** `JoinWorkerSession` di hub — worker exam page tidak join group `"worker-{sessionId}"`.

  Perlu verifikasi: AdminController `ResetAssessment` mengirim ke grup apa? Perlu cek code AdminController untuk memastikan grup yang digunakan.

- **D-11 GAP: assessmentHubStartPromise** — assessment-hub.js tidak expose `assessmentHubStartPromise` (hanya `window.assessmentHub`). StartExam.cshtml menggunakan `setTimeout(2000)` fallback untuk set badge ke 'Live'. Lebih robust jika expose promise (seperti yang dilakukan di Phase 231 untuk monitoring badge).
- **D-12 STATUS:** Handler `sessionReset` sudah ada di StartExam.cshtml — D-12 sebagian sudah done. Yang perlu diverifikasi adalah apakah AdminController `ResetAssessment` mengirim event `sessionReset` ke worker session group yang benar.

### Area 6: Session Resume (AFLW-04 / D-13)

**Yang sudah ada (verified OK):**
- `ElapsedSeconds` dilanjutkan dari DB (`ELAPSED_SECONDS_FROM_DB`)
- `RemainingSeconds` dihitung server: `durationSeconds - elapsedSec`
- `LastActivePage` di-restore ke `RESUME_PAGE`
- `SavedAnswers` JSON di-pass ke view dan di-populate via `prePopulateAnswers()`
- Resume confirmation modal saat `IS_RESUME && RESUME_PAGE > 0`
- ExamExpired modal saat `EXAM_EXPIRED` (remaining <= 0 on resume)

**Gap yang perlu difix:**
- **D-13 GAP: timer saat resume** — `timeRemaining = REMAINING_SECONDS_FROM_DB` sudah benar. TAPI `elapsedSeconds = ELAPSED_SECONDS_FROM_DB` — ini menjadi baseline untuk `saveSessionProgress()`. Perlu verifikasi: saat worker resume dan `UpdateSessionProgress` dipanggil, apakah `elapsedSeconds` increment benar dari titik resume atau dari 0.
- **D-13 GAP: stale question detection** — Jika `SavedQuestionCount != currentQuestionCount`, session redirect ke Assessment list dengan error. Ini adalah edge case yang sudah dihandle, tapi worker kehilangan progress tanpa penjelasan memadai.

### Area 7: Scoring & Results (AFLW-03, AFLW-05 / D-15, D-16, D-17)

**Yang sudah ada (verified OK):**
- Score calculation: `totalScore / maxScore * 100` → `finalPercentage`
- `IsPassed = finalPercentage >= assessment.PassPercentage`
- `NomorSertifikat` generation via `CertNumberHelper` dengan retry loop (max 3 attempts)
- ElemenTeknis scores per session disimpan ke `SessionElemenTeknisScores`
- Status-guarded write dengan race condition handling
- Results.cshtml: Score%, PassThreshold%, Pass/Fail badge sudah ada
- ElemenTeknis radar chart sudah ada (jika >= 3 elemen)
- Answer review sudah ada (conditional pada `Model.AllowAnswerReview`)
- `list-group-item-success` (hijau) untuk jawaban benar, `list-group-item-danger` (merah) untuk jawaban salah yang dipilih

**Gap yang perlu difix:**
- **D-15 GAP: competency update** — Controller comment: "Competency auto-update removed in Phase 90 (KKJ tables dropped)". `CompetencyGains` di Results.cshtml ada block, tapi apakah `Model.CompetencyGains` terisi? Perlu audit controller `Results` action untuk memastikan model property ini diisi dengan benar atau dihapus jika tidak relevan.
- **D-16 GAP: section-by-section breakdown** — Results.cshtml punya ElemenTeknis table (per elemen teknis benar/total/persen). Ini sudah merupakan section breakdown. Yang mungkin kurang: tidak ada pengelompokan jawaban review per ElemenTeknis — semua pertanyaan ditampilkan flat dalam answer review.
- **D-17 GAP: AllowAnswerReview toggle** — Results.cshtml sudah conditional pada `Model.AllowAnswerReview`. TAPI perlu audit: apakah `AssessmentResultsViewModel.AllowAnswerReview` diset dari DB field `Assessment.AllowAnswerReview`? Perlu cek Results GET action di controller.
- **D-16 GAP: NomorSertifikat di Results** — Results.cshtml tidak menampilkan NomorSertifikat kepada worker. Jika lulus dan `GenerateCertificate = true`, hanya ada tombol "View Certificate" — tapi nomor sertifikat sendiri tidak ditampilkan di halaman results. Perlu tambah display NomorSertifikat.

### Area 8: Proton Special Handling (D-18, D-19)

**Yang sudah ada (verified OK):**
- Assessment.cshtml: Tahun 3 menampilkan "Interview Dijadwalkan" badge, bukan Start button
- Assessment.cshtml: Completed Tahun 3 dengan `InterviewResultsJson` menampilkan Lulus/Tidak Lulus
- Phase 231-02 SUMMARY: Proton Tahun 3 `SubmitInterviewResults` (score avg, isPassed, audit log) — verified OK

**Yang perlu diaudit:**
- **D-18/D-19:** Path Proton Tahun 1-2 menggunakan StartExam normal — perlu verifikasi apakah ada handling khusus untuk `Category == "Assessment Proton"` di StartExam flow atau identik dengan assessment reguler.
- **D-19 GAP:** Proton NomorSertifikat generation: apakah `GenerateCertificate` diset benar untuk Assessment Proton? Perlu verifikasi di CreateAssessment form behavior untuk kategori Proton.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Timer drift correction | Manual counter | `Date.now()` anchor | Interval drift setelah tab minimize — gunakan wall clock |
| Modal management | Custom overlay | Bootstrap Modal API | Sudah ada di project |
| SignalR retry | Custom WebSocket | `withAutomaticReconnect([0,2000,5000,10000,30000])` | Sudah dikonfigurasi di assessment-hub.js |
| Token auto-format | Custom regex | `input` event listener: `value = value.toUpperCase().replace(/\s/g,'')` | Simple, tidak perlu library |
| Answer persist | Custom localStorage | Server-side `SaveAnswer` endpoint | Sudah ada, lebih reliable |

---

## Common Pitfalls

### Pitfall 1: Hub Group Mismatch untuk Per-Worker Push
**What goes wrong:** AdminController mengirim event ke group tertentu, tapi worker tidak join group tersebut — signal tidak sampai.
**Why it happens:** Ada dua kategori push: broadcast ke batch (`batch-{batchKey}`) dan targeted ke satu worker (`worker-{sessionId}`). Jika coding menggunakan group yang berbeda di server vs client, signal hilang tanpa error.
**How to avoid:** Verifikasi AdminController `ResetAssessment` dan `AkhiriUjian` mengirim ke group apa, lalu pastikan AssessmentHub expose join method untuk group tersebut, dan worker exam page join group tersebut on start.
**Warning signs:** SignalR handler terpasang (`window.assessmentHub.on('sessionReset', ...)`) tapi tidak pernah triggered.

### Pitfall 2: Timer Direct Submit Tanpa Modal
**What goes wrong:** Timer mencapai 0 → langsung `examForm.submit()` — bertentangan D-08.
**Why it happens:** Kode awal didesain untuk auto-submit, keputusan D-08 datang belakangan.
**How to avoid:** Ubah timer expiry handler: clear timer → tampilkan modal warning → click handler di OK button → submit form.
**Warning signs:** Line `if (timeRemaining <= 0) { clearInterval(timerInterval); window.onbeforeunload = null; document.getElementById('examForm').submit(); }` masih ada tanpa modal interception.

### Pitfall 3: TempData Serialization int vs long
**What goes wrong:** `TempData["PendingAssignmentId"]` di-deserialize sebagai `long` bukan `int` karena JSON serialization.
**Why it happens:** CookieTempDataProvider serializes via JSON; JSON numbers tanpa type hint default ke `long`.
**How to avoid:** Pattern sudah dihandle di ExamSummary GET: `TempData["PendingAssignmentId"] switch { int i => i, long l => (int)l, _ => null }` — jangan ubah pattern ini.

### Pitfall 4: ExamExpired Modal Auto-Submit Race
**What goes wrong:** `EXAM_EXPIRED = true` → modal tampil → 5 detik `setTimeout` auto-submit → tapi user sudah klik OK dan submit jalan dua kali.
**Why it happens:** `setTimeout(5000, submit)` dan click handler keduanya call `examForm.submit()`.
**How to avoid:** Guard dengan flag `var submitted = false` sebelum submit, set `submitted = true` sebelum submit, cek flag di keduanya.

### Pitfall 5: prePopulateAnswers Keys sebagai String
**What goes wrong:** `SAVED_ANSWERS` keys dari JSON adalah string, tapi `answeredQuestions.add(String(qId))` — harus konsisten string di Set.
**Why it happens:** JavaScript dictionary dari JSON selalu punya string keys.
**How to avoid:** Pattern sudah benar di kode (`answeredQuestions.add(String(qId))`). Jangan ubah ke parseInt.

---

## Code Examples

### Timer Expiry dengan Modal (D-08 fix pattern)
```javascript
// Ganti handler langsung submit dengan modal interception
if (timeRemaining <= 0) {
    clearInterval(timerInterval);
    window.onbeforeunload = null;
    // Tampilkan warning modal dulu (bukan langsung submit)
    var timeupModal = new bootstrap.Modal(document.getElementById('timeUpWarningModal'));
    timeupModal.show();
    // Submit setelah user klik OK (atau auto setelah 10 detik)
    document.getElementById('timeUpOkBtn').addEventListener('click', function() {
        timeupModal.hide();
        document.getElementById('examForm').submit();
    });
    setTimeout(function() {
        document.getElementById('examForm').submit();
    }, 10000);
}
```

### Token Modal Auto-Focus + Enter Key + Auto-Clear Error (D-06 fix pattern)
```javascript
// Auto-focus saat modal terbuka
document.getElementById('tokenModal').addEventListener('shown.bs.modal', function() {
    document.getElementById('tokenInput').focus();
});

// Enter key submit
document.getElementById('tokenInput').addEventListener('keydown', function(e) {
    if (e.key === 'Enter') { e.preventDefault(); verifyToken(); }
});

// Clear error saat user mulai mengetik
document.getElementById('tokenInput').addEventListener('input', function() {
    document.getElementById('tokenError').classList.add('d-none');
    // Auto uppercase + trim spasi
    this.value = this.value.toUpperCase().replace(/\s/g, '');
});
```

### Timer Drift Fix — Wall Clock Anchor (D-07 fix pattern)
```javascript
// Gunakan Date.now() sebagai anchor, bukan hanya decrement counter
var timerStartWallClock = Date.now();
var timerStartRemaining = REMAINING_SECONDS_FROM_DB;

function updateTimer() {
    var elapsed = Math.floor((Date.now() - timerStartWallClock) / 1000);
    var remaining = Math.max(0, timerStartRemaining - elapsed);
    timeRemaining = remaining;  // sync state variable
    elapsedSeconds = ELAPSED_SECONDS_FROM_DB + elapsed;  // accurate for progress save

    var minutes = Math.floor(remaining / 60);
    var seconds = remaining % 60;
    // ... display update ...
    if (remaining <= 0) { /* handle expiry */ }
}
```

### Status Badge Mapping yang Benar (D-02 fix)
```csharp
// Di Assessment.cshtml — perbaikan statusBadgeClass
var statusBadgeClass = item.Status switch
{
    "Open" => "bg-success",          // hijau (sudah benar)
    "Upcoming" => "bg-primary",      // biru (ubah dari bg-warning)
    "Completed" => "bg-secondary",   // abu-abu (ubah dari bg-primary)
    "InProgress" => "bg-warning text-dark", // kuning (tetap)
    _ => "bg-secondary"
};
// "Expired" sebagai display-state: jika ExamWindowCloseDate < now && Status == "Open"
// → tampilkan badge bg-danger "Expired"
```

### Hub: Worker Join Session Group (D-11 fix)
```csharp
// Di AssessmentHub.cs — tambah method untuk per-worker session group
public async Task JoinWorkerSession(int sessionId)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"worker-session-{sessionId}");
}

// Di StartExam.cshtml init — join worker session group setelah hub connect
window.assessmentHubStartPromise.then(function() {
    connection.invoke('JoinWorkerSession', SESSION_ID).catch(function(err) {
        console.warn('[assessment-hub] JoinWorkerSession failed:', err);
    });
});
```

---

## Action Items yang Perlu Verifikasi Sebelum Planning

### CRITICAL: Verifikasi Group Name di AdminController

Sebelum plan dibuat, perlu verifikasi: AdminController `ResetAssessment` dan `AkhiriUjian` mengirim `sessionReset`/`examClosed` ke group dengan nama apa?

Kemungkinan:
- `$"worker-{sessionId}"` → perlu `JoinWorkerSession(sessionId)` di hub
- `$"batch-{batchKey}"` → broadcast ke semua worker di batch (sudah di-join via assessment-hub.js)
- `$"user-{userId}"` → perlu IHubContext user targeting

Verifikasi ini dilakukan saat implementasi Task 1 (audit fase).

### MEDIUM: UpdateSessionProgress Action

Perlu verifikasi `UpdateSessionProgress` action ada di CMPController. Kode JS me-reference `SESSION_PROGRESS_URL = '@Url.Action("UpdateSessionProgress", "CMP")'` — action ini tidak terlihat dalam range kode yang diaudit (offset 1-1528). Perlu scroll lebih jauh untuk konfirmasi.

---

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| Legacy path (AssessmentQuestion) | Package path only (Phase 227 CLEN-02) | Session tanpa package → error, bukan silent fail |
| Competency auto-update | Removed (Phase 90 KKJ tables dropped) | CompetencyGains di Results mungkin selalu empty |
| `setInterval` counter timer | Masih ada — GAP vs wall clock | Timer drift pada tab switch |
| Direct submit on timer expiry | Modal dulu (D-08) — GAP yang perlu difix | UX improvement |

---

## Open Questions

1. **Group name di AdminController untuk HC push ke worker**
   - Yang diketahui: Handler `examClosed` dan `sessionReset` sudah ada di StartExam.cshtml
   - Yang belum jelas: AdminController mengirim ke group apa saat Reset/ForceClose
   - Rekomendasi: Audit AdminController saat Task 1 Plan 01

2. **UpdateSessionProgress action existence**
   - Yang diketahui: JS mereferens URL `UpdateSessionProgress`
   - Yang belum jelas: Action ini tidak terlihat di range kode yang diaudit (offset 1-1528), kemungkinan ada di offset 1529+
   - Rekomendasi: Baca sisa CMPController saat audit Task 1

3. **CompetencyGains selalu empty?**
   - Yang diketahui: Results.cshtml punya blok `CompetencyGains`, tapi competency auto-update dihapus Phase 90
   - Yang belum jelas: Apakah `AssessmentResultsViewModel.CompetencyGains` selalu empty collection atau diisi dari data lain?
   - Rekomendasi: Audit Results GET action, jika selalu empty → hapus blok dari view

4. **Proton Tahun 1-2 StartExam path**
   - Yang diketahui: Assessment.cshtml menampilkan Start button normal untuk Tahun 1-2
   - Yang belum jelas: Apakah ada handling khusus `Category == "Assessment Proton"` di StartExam controller? Atau identik dengan assessment biasa?
   - Rekomendasi: Grep `Assessment Proton` di CMPController

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (tidak ada automated test suite) |
| Config file | None |
| Quick run command | Manual: buka halaman di browser, ikuti flow |
| Full suite command | Manual: all flows end-to-end |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| AFLW-01 | Worker melihat daftar yang benar | manual | — | ❌ manual |
| AFLW-02 | Token entry + exam page + timer + auto-save | manual | — | ❌ manual |
| AFLW-03 | Submit → score + IsPassed + NomorSertifikat | manual | — | ❌ manual |
| AFLW-04 | Resume session lengkap | manual | — | ❌ manual |
| AFLW-05 | Results page lengkap | manual | — | ❌ manual |

### Wave 0 Gaps
None — tidak ada automated test framework untuk fase ini. Testing dilakukan manual sesuai pola project.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CMPController.cs` — Dibaca offset 1-1528; Assessment, StartExam, AbandonExam, ExamSummary GET/POST, SubmitExam, LogActivityAsync, Certificate actions
- `Views/CMP/Assessment.cshtml` — Dibaca lengkap (674 baris)
- `Views/CMP/StartExam.cshtml` — Dibaca lengkap (834 baris)
- `Views/CMP/Results.cshtml` — Dibaca 1-346 (lengkap)
- `Hubs/AssessmentHub.cs` — Dibaca lengkap (138 baris)
- `wwwroot/js/assessment-hub.js` — Dibaca lengkap (97 baris)
- `.planning/phases/231-audit-assessment-management-monitoring/231-02-SUMMARY.md` — Referensi SignalR patterns dari Phase 231

### Secondary (MEDIUM confidence)
- `.planning/phases/232-audit-assessment-flow-worker-side/232-CONTEXT.md` — User decisions
- `.planning/REQUIREMENTS.md` — AFLW requirement definitions

---

## Metadata

**Confidence breakdown:**
- Audit findings: HIGH — berdasarkan kode aktual
- Gap analysis: HIGH — gap dikonfirmasi dengan membaca kode
- Fix patterns: HIGH — berdasarkan existing patterns di project
- SignalR group name: MEDIUM — belum verifikasi AdminController ResetAssessment

**Research date:** 2026-03-22
**Valid until:** 2026-04-22 (kode stabil, tidak ada dependency external)
