# Phase 267: Resilience & Edge Cases - Research

**Researched:** 2026-03-28
**Domain:** UAT Ujian — Ketahanan terhadap gangguan (koneksi, refresh, resume, timer habis)
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Test/UAT dilakukan di **server dev** (`http://10.55.3.3/KPB-PortalHC/`)
- **D-02:** Bug/temuan dicatat, diperbaiki di **coding lokal**
- **D-03:** Verifikasi fix di **web lokal** (`https://localhost:7241/KPB-PortalHC/`)
- **D-04:** Setelah fix terverifikasi, kirim ke IT untuk deploy ulang
- **D-05:** Gunakan **Playwright intercept** untuk semua simulasi: route intercept (block request), page.reload(), close+reopen tab
- **D-06:** Otomatis dan reproducible — tidak perlu manual DevTools
- **D-07:** Buat **assessment baru dengan durasi 1-2 menit** di server dev untuk test timer
- **D-08:** Biarkan timer habis secara natural, verifikasi behavior (auto-submit/block/pesan)
- **D-09:** Paling realistis — test end-to-end termasuk server-side enforcement (2-min grace period)
- **D-10:** **Regan** — test resilience utama (koneksi putus, refresh, resume, tab close)
- **D-11:** **Arsyad** — buat assessment baru, khusus test timer habis (skenario terpisah)
- **D-12:** Perlu buat assessment baru di server dev untuk kedua worker (assessment lama sudah completed/submitted)

### Claude's Discretion
- Urutan skenario test (mana dulu yang dijalankan)
- Detail Playwright intercept pattern (route blocking, timing)
- Query database untuk verifikasi data integrity setelah gangguan
- Setup assessment baru (judul, durasi, jumlah soal) untuk test edge cases

### Deferred Ideas (OUT OF SCOPE)
- Stress test: banyak user simultan mengerjakan ujian
- Admin monitoring real-time (Phase 268)
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| EDGE-01 | Lost connection — warning/retry, jawaban tidak hilang | pendingAnswers queue + flushPendingAnswers() sudah ada; perlu test via Playwright route block |
| EDGE-02 | Tab tertutup & resume — kembali ke halaman soal terakhir | resumeConfirmModal + RESUME_PAGE dari LastActivePage DB sudah ada |
| EDGE-03 | Resume — timer lanjut dari sisa waktu (tidak reset) | REMAINING_SECONDS_FROM_DB = DurationSeconds - ElapsedSeconds dari DB; wall-clock timer sudah drift-proof |
| EDGE-04 | Resume — jawaban yang sudah dipilih masih tercentang | prePopulateAnswers() dari SAVED_ANSWERS (ViewBag dari DB) sudah ada |
| EDGE-05 | Resume — progress counter akurat, indikasi "lanjutkan" | answeredQuestions Set di-populate dari SAVED_ANSWERS + updateAnsweredCount() dipanggil setelah prePopulateAnswers |
| EDGE-06 | Browser refresh — jawaban, posisi, timer tetap benar | Sama dengan EDGE-02 sampai EDGE-05 — refresh = resume dari halaman yang sama |
| EDGE-07 | Timer habis — behavior sesuai (auto-submit/block/pesan) | timeUpWarningModal + auto-submit 10 detik; server-side 2-min grace period di SubmitExam |
</phase_requirements>

---

## Summary

Phase 267 adalah UAT keempat dari milestone v10.0 yang berfokus pada ketahanan (resilience) sistem ujian terhadap gangguan nyata: koneksi putus, tab tertutup, browser refresh, dan timer habis. Tidak ada fitur baru yang dibangun — ini murni fase test-dan-fix.

Kode resilience sudah sepenuhnya diimplementasikan di fase sebelumnya. StartExam.cshtml memiliki: pendingAnswers retry queue, offline badge, resume modal (resumeConfirmModal), prePopulateAnswers(), wall-clock timer yang drift-proof, dan UpdateSessionProgress setiap 30 detik. CMPController.cs punya SaveAnswer (upsert atomik), UpdateSessionProgress (update ElapsedSeconds + LastActivePage), dan SubmitExam dengan 2-menit grace period enforcement.

Pola UAT sama persis seperti Phase 264-266: Playwright otomatis untuk simulasi gangguan di server dev, temuan dicatat, fix di lokal, verifikasi di lokal. Karena assessment lama sudah completed, perlu buat assessment baru untuk kedua worker.

**Primary recommendation:** Jalankan skenario Regan (koneksi putus, refresh, tab close/resume) terlebih dahulu, lalu skenario Arsyad (timer habis) karena Arsyad butuh assessment durasi pendek yang khusus dibuat.

---

## Standard Stack

### Core (sudah ada di proyek)
| Komponen | Lokasi | Fungsi Resilience |
|----------|--------|-------------------|
| Playwright | uat-265-test.js (pattern) | Route intercept, reload, close+reopen page |
| SignalR | wwwroot/js/assessment-hub.js | onreconnecting/onreconnected/onclose handlers |
| pendingAnswers queue | StartExam.cshtml ~line 387 | Retry queue untuk jawaban yang gagal tersimpan saat offline |
| UpdateSessionProgress | CMPController.cs line 329 | Simpan ElapsedSeconds + LastActivePage ke DB setiap 30 detik |
| prePopulateAnswers() | StartExam.cshtml ~line 531 | Restore jawaban dari DB saat resume |
| Wall-clock timer | StartExam.cshtml ~line 330 | REMAINING_SECONDS_FROM_DB dikurangi Date.now() delta, drift-proof |

### Playwright Patterns (dari uat-265-test.js)

**Confidence: HIGH** — Dibuktikan oleh keberadaan file uat-265-test.js dan pattern yang sudah terbukti berhasil di Phase 265.

```javascript
// Block SaveAnswer endpoint (simulasi koneksi putus parsial)
await page.route('**/CMP/SaveAnswer', route => route.abort());

// Restore koneksi
await page.unroute('**/CMP/SaveAnswer');

// Reload halaman (simulasi browser refresh)
await page.reload();

// Close tab + reopen (simulasi tab tertutup)
const context = page.context();
await page.close();
const newPage = await context.newPage();
await newPage.goto(examUrl);
```

---

## Architecture Patterns

### Alur Resume (EDGE-02, EDGE-03, EDGE-04, EDGE-05, EDGE-06)

```
Worker close tab / refresh
    ↓
Browser navigasi ke StartExam URL
    ↓
CMPController.StartExam GET
    ├── assessment.StartedAt != null → isResume = true
    ├── ElapsedSeconds dari DB → remainingSeconds = duration - elapsed
    ├── ExamExpired = remainingSeconds <= 0
    ├── SavedAnswers dari PackageUserResponses → ViewBag.SavedAnswers (JSON)
    └── LastActivePage → ViewBag.LastActivePage
    ↓
StartExam.cshtml JS init:
    ├── prePopulateAnswers() → restore radio selections + answeredQuestions Set
    ├── timerStartRemaining = REMAINING_SECONDS_FROM_DB (sudah dikurangi elapsed)
    └── IS_RESUME && RESUME_PAGE > 0 → show resumeConfirmModal
    ↓
Worker klik "Lanjutkan" di modal
    └── currentPage = RESUME_PAGE, tampilkan halaman yang benar
```

### Alur Koneksi Putus (EDGE-01)

```
Worker pilih jawaban (radio change)
    ↓
saveAnswerWithDebounce() → 300ms debounce
    ↓
saveAnswerAsync() → fetch POST /CMP/SaveAnswer
    ↓ (jika gagal)
Retry 3x: attempt 0 → 1s delay, attempt 1 → 3s delay, attempt 2 → fail
    ↓ (setelah 3 attempt gagal)
Masukkan ke pendingAnswers[]
updateNetworkBadge('offline')
showFailureToast() — "Koneksi bermasalah, cek jaringan"
    ↓ (saat koneksi pulih)
flushPendingAnswers() dipanggil dari onreconnected / manual trigger
    └── POST semua pendingAnswers ke SaveAnswer endpoint
```

### Alur Timer Habis (EDGE-07)

```
Client: remaining <= 0
    ↓
clearInterval(timerInterval + saveInterval)
window.onbeforeunload = null
Show timeUpWarningModal
    ↓ (worker klik OK atau 10s timeout)
submitted = true
examForm.submit() → POST /CMP/ExamSummary
    ↓
ExamSummary → SubmitExam POST
    ↓
SubmitExam: cek elapsed + 2-menit grace period
    ├── elapsed <= allowedMinutes + 2 → proses grading normal
    └── elapsed > allowedMinutes + 2 → reject: TempData["Error"] + RedirectToAction StartExam
```

### Anti-Pattern yang Harus Dihindari

- **Jangan test EDGE-07 dengan memanipulasi waktu client-side saja** — harus test end-to-end agar server-side grace period enforcement juga terverifikasi. Gunakan assessment durasi 1-2 menit (D-07).
- **Jangan close Playwright browser context sepenuhnya untuk simulasi tab close** — gunakan `page.close()` lalu buka page baru di context yang sama agar session cookie/auth tetap valid.
- **Jangan skip `page.waitForURL()` atau `page.waitForSelector()` setelah reload** — server butuh waktu untuk load dan render resume state.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Alasan |
|---------|-------------|-------------|--------|
| Simulasi offline | Manual DevTools throttle | Playwright `page.route()` abort | Reproducible, otomatis (D-05/D-06) |
| Verifikasi data DB | Query manual di pgAdmin | Playwright `request.get()` ke endpoint JSON atau DB query via shell | Terotomasi, consistent |
| Timer manipulation | Override Date.now() di browser | Buat assessment baru 1-2 menit (D-07/D-08/D-09) | Server-side enforcement juga ditest |

---

## Common Pitfalls

### Pitfall 1: flushPendingAnswers tidak dipanggil setelah restore koneksi

**Apa yang salah:** Setelah route intercept dimatikan (koneksi pulih), pendingAnswers di JS memory tidak otomatis di-flush kecuali SignalR reconnect event terjadi.

**Mengapa terjadi:** `flushPendingAnswers()` dipanggil dari `connection.onreconnected()` di assessment-hub.js. Jika Playwright hanya block HTTP fetch (bukan WebSocket), SignalR mungkin tetap connected dan onreconnected tidak akan fired.

**Cara menghindari:** Saat test EDGE-01, verifikasi: (1) apakah pendingAnswers di-flush setelah koneksi pulih, (2) apakah manual trigger diperlukan. Jika tidak ter-flush otomatis, ini adalah bug yang perlu difix — tambahkan trigger flush saat fetch berhasil kembali.

**Tanda peringatan:** Setelah koneksi pulih dan worker klik pilihan baru, jawaban ter-save. Tapi jawaban yang ada di pendingAnswers (saat offline) tidak ter-kirim.

### Pitfall 2: UpdateSessionProgress dengan currentPage < 1 akan ditolak server

**Apa yang salah:** Validasi server: `if (sessionId <= 0 || elapsedSeconds < 0 || currentPage < 1)` — currentPage yang dikirim adalah 0-indexed, sedangkan server mungkin mengharapkan 1-indexed (perlu verifikasi).

**Cara menghindari:** Cek line 332 CMPController — validasi `currentPage < 1` berarti page 0 (halaman pertama) tidak akan tersimpan. Worker yang abandon di halaman pertama akan resume ke halaman 0, yang benar — tapi ElapsedSeconds tidak akan terupdate untuk halaman pertama.

### Pitfall 3: EXAM_EXPIRED dihitung dari ElapsedSeconds yang stale

**Apa yang salah:** `remainingSeconds = durationSeconds - session.ElapsedSeconds`. ElapsedSeconds hanya diupdate setiap 30 detik via UpdateSessionProgress. Jika worker refresh tepat saat ujian expired tapi UpdateSessionProgress belum dipanggil, timer mungkin masih tampil positif.

**Cara menghindari:** Untuk EDGE-07, tunggu minimal 30 detik setelah timer habis sebelum verifikasi behavior server-side. Atau verifikasi bahwa `StartedAt` + `DurationMinutes` juga digunakan sebagai fallback check.

### Pitfall 4: Assessment lama Arsyad sudah Completed — tidak bisa dipakai

**Apa yang salah:** Assessment dari Phase 265-266 kemungkinan besar sudah dalam status `Completed` atau `Abandoned`. CMPController.StartExam akan redirect ke Assessment list jika status Completed.

**Cara menghindari:** Buat assessment baru untuk Arsyad dan Regan di server dev. Admin perlu assign worker ke assessment baru tersebut sebelum test dimulai (D-12).

### Pitfall 5: prePopulateAnswers gagal jika questionId di SAVED_ANSWERS tidak match dengan soal di view

**Apa yang salah:** Jika assignment (ShuffledQuestionIds) berubah antara session pertama dan resume, questionId yang disimpan di PackageUserResponses tidak akan match dengan radio button di halaman.

**Cara menghindari:** Resume harus menggunakan assignment yang sama (idempotent check di StartExam sudah ada). Tapi verifikasi bahwa tidak ada edge case di mana assignment bisa berbeda.

---

## Code Examples

### Playwright: Block SaveAnswer untuk simulasi offline (EDGE-01)

```javascript
// Source: Pattern dari uat-265-test.js (verified di proyek)
// Simulasi koneksi putus (hanya block autosave)
await page.route('**/CMP/SaveAnswer', route => route.abort());

// Jawab beberapa soal → should show offline badge + failureToast
await page.locator('input[name="radio_QUESTIONID"]').first().click();
await page.waitForTimeout(2000); // tunggu retry exhausted (1s + 3s backoff)

// Verifikasi offline state
await expect(page.locator('#networkStatusBadge')).toHaveText('Offline');
await expect(page.locator('.alert-warning')).toBeVisible(); // failure toast

// Restore koneksi
await page.unroute('**/CMP/SaveAnswer');

// Verifikasi flush: network badge harus kembali ke Tersimpan
await page.waitForTimeout(5000);
await expect(page.locator('#networkStatusBadge')).toHaveText('Tersimpan');
```

### Playwright: Close tab + reopen (EDGE-02, EDGE-03, EDGE-04, EDGE-05)

```javascript
// Source: Playwright docs pattern (verified)
const context = page.context();
const examUrl = page.url();

// Simulate tab close
await page.close();

// Reopen (same auth context)
const newPage = await context.newPage();
await newPage.goto(examUrl);

// Wait for resume modal
await newPage.waitForSelector('#resumeConfirmModal.show', { timeout: 5000 });

// Verify resume modal content
const resumeNum = await newPage.locator('#resumePageNum').innerText();
expect(parseInt(resumeNum)).toBeGreaterThan(0); // EDGE-02 + EDGE-05

// Verify timer shows remaining (not full duration) — EDGE-03
const timerText = await newPage.locator('#examTimer').innerText();
// Timer should show less than full duration

// Confirm resume
await newPage.locator('#resumeConfirmBtn').click();

// Verify pre-populated answers — EDGE-04
const checkedRadio = await newPage.locator('input.exam-radio:checked').count();
expect(checkedRadio).toBeGreaterThan(0);
```

### Playwright: Browser refresh (EDGE-06)

```javascript
// Source: Playwright docs
await page.reload();
await page.waitForLoadState('networkidle');

// Same verifications as tab close: resume modal, timer, answers
```

### Query DB: Verifikasi ElapsedSeconds tersimpan

```sql
-- Verifikasi sesudah tiap skenario
SELECT Id, Status, StartedAt, ElapsedSeconds, LastActivePage, DurationMinutes
FROM AssessmentSessions
WHERE Id = <SESSION_ID>;

-- Verifikasi jawaban tersimpan
SELECT COUNT(*) FROM PackageUserResponses WHERE AssessmentSessionId = <SESSION_ID>;
```

---

## State of the Art

| Komponen | Implementasi Saat Ini | Status |
|----------|----------------------|--------|
| pendingAnswers flush | Hanya saat SignalR `onreconnected` | Perlu verifikasi: apakah ter-flush jika koneksi HTTP pulih tapi SignalR tetap connected |
| UpdateSessionProgress | Setiap 30 detik interval | Timer bisa stale max 30 detik antara actual elapsed dan tersimpan di DB |
| Timer sync | Wall-clock anchor dari client Date.now() | Tidak ada server push untuk sync timer (hanya initial REMAINING_SECONDS_FROM_DB) |
| EXAM_EXPIRED check | Hanya dari ElapsedSeconds di DB | Tidak cek `DateTime.UtcNow - StartedAt` sebagai fallback (potensi gap saat ElapsedSeconds stale) |

---

## Open Questions

1. **Apakah flushPendingAnswers dipanggil saat koneksi HTTP pulih (bukan SignalR reconnect)?**
   - Yang kita tahu: `flushPendingAnswers()` ada di StartExam.cshtml, dipanggil dari `connection.onreconnected()`
   - Yang tidak jelas: Jika Playwright hanya memblock fetch request (bukan WebSocket), SignalR tetap connected → onreconnected tidak fired → pendingAnswers tidak di-flush
   - Rekomendasi: Test dulu, jika tidak ter-flush → ini bug yang perlu difix di Wave fix

2. **Apakah UpdateSessionProgress di-call pada page close/refresh (beforeunload)?**
   - Yang kita tahu: Hanya ada `setInterval(saveSessionProgress, 30000)` dan di-call saat `changePage()`
   - Yang tidak jelas: Tidak ada `window.addEventListener('beforeunload', saveSessionProgress)` — jadi ElapsedSeconds bisa stale hingga 30 detik saat tab close
   - Rekomendasi: Verifikasi apakah ini menyebabkan resume timer tidak akurat. Jika iya → fix dengan tambah beforeunload sync call (beacon API atau navigator.sendBeacon)

3. **Berapa lama assessment baru harus punya soal?**
   - Rekomendasi: Minimum 10 soal (satu halaman) agar test navigasi halaman bermakna. Durasi 2 menit untuk timer test.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Server dev `http://10.55.3.3/KPB-PortalHC/` | Semua UAT | Diasumsikan ✓ (dipakai Phase 264-266) | — | — |
| Node.js + Playwright | Script UAT otomatis | ✓ | (lihat package.json) | Manual browser test |
| Admin account `admin@pertamina.com` | Buat assessment baru | ✓ | — | — |
| Worker account Regan | EDGE-01, EDGE-02, EDGE-03, EDGE-04, EDGE-05, EDGE-06 | Perlu verifikasi — apakah sudah di-assign ke assessment baru? | — | Gunakan worker lain |
| Worker account Arsyad | EDGE-07 | Perlu verifikasi — assessment baru dengan durasi 1-2 menit | — | — |

**Missing dependencies dengan no fallback:**
- Assessment baru di server dev — harus dibuat oleh Admin sebelum test dimulai (D-12)

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright (Node.js) |
| Config file | package.json (lihat scripts) |
| Quick run command | `node uat-267-resilience.js` |
| Full suite command | `node uat-267-resilience.js` (single script, semua skenario) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| EDGE-01 | Lost connection — offline badge + pending answers flushed | integration (Playwright route block) | `node uat-267-resilience.js --scenario edge01` | ❌ Wave 0 |
| EDGE-02 | Tab close + resume → halaman soal terakhir | integration (Playwright page.close) | `node uat-267-resilience.js --scenario edge02` | ❌ Wave 0 |
| EDGE-03 | Resume timer lanjut dari sisa waktu | integration (Playwright page.close + timer check) | `node uat-267-resilience.js --scenario edge03` | ❌ Wave 0 |
| EDGE-04 | Resume → jawaban masih tercentang | integration (Playwright page.close + radio check) | `node uat-267-resilience.js --scenario edge04` | ❌ Wave 0 |
| EDGE-05 | Resume → progress counter akurat | integration (Playwright answered count) | `node uat-267-resilience.js --scenario edge05` | ❌ Wave 0 |
| EDGE-06 | Browser refresh → jawaban/posisi/timer tetap | integration (Playwright page.reload) | `node uat-267-resilience.js --scenario edge06` | ❌ Wave 0 |
| EDGE-07 | Timer habis → modal + auto-submit + server reject late | integration (assessment 2 menit, tunggu natural) | `node uat-267-resilience.js --scenario edge07` | ❌ Wave 0 |

### Sampling Rate
- **Per skenario selesai:** verifikasi visual + DB query
- **Phase gate:** Semua 7 skenario PASS atau bug tercatat + difix sebelum `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `uat-267-resilience.js` — script utama, semua 7 skenario
- [ ] Assessment baru di server dev (durasi 2 menit, min 10 soal) — untuk Arsyad (EDGE-07)
- [ ] Assessment baru di server dev (durasi 30-60 menit, min 10 soal) — untuk Regan (EDGE-01 sampai EDGE-06)

---

## Sources

### Primary (HIGH confidence)
- `Views/CMP/StartExam.cshtml` — Semua logika resilience client-side (pendingAnswers, timer, prePopulateAnswers, resumeModal)
- `wwwroot/js/assessment-hub.js` — SignalR reconnect handlers
- `Controllers/CMPController.cs` lines 263-362 — SaveAnswer, UpdateSessionProgress
- `Controllers/CMPController.cs` lines 705-957 — StartExam resume logic, ViewBag population
- `Controllers/CMPController.cs` lines 1307-1400 — SubmitExam, 2-min grace period
- `.planning/phases/267-resilience-edge-cases/267-CONTEXT.md` — User decisions

### Secondary (MEDIUM confidence)
- `.planning/phases/265-worker-exam-flow/265-CONTEXT.md` — D-05/D-06 context network indicator
- `.planning/phases/266-review-submit-hasil/266-CONTEXT.md` — Deferred items ke Phase 267

### Tertiary (LOW confidence)
- Analisis pendingAnswers flush behavior saat HTTP koneksi pulih tapi SignalR tidak disconnect — belum diverifikasi via test aktual, hanya dari code reading

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — kode sudah ada dan terbaca langsung
- Architecture patterns: HIGH — alur resume dan offline sudah terimplementasi, diverifikasi dari source code
- Pitfalls: MEDIUM — sebagian dari code analysis, sebagian (flush behavior) belum terverifikasi via actual runtime
- Test plan: HIGH — mengikuti pattern Phase 265 yang sudah terbukti

**Research date:** 2026-03-28
**Valid until:** 2026-04-28 (kode stabil, tidak ada dependency eksternal yang berubah cepat)
