# Phase 267: Resilience & Edge Cases - Context

**Gathered:** 2026-03-28
**Status:** Ready for planning

<domain>
## Phase Boundary

UAT fase keempat: menguji ketahanan ujian terhadap gangguan — koneksi putus, tab tertutup, browser refresh, dan timer habis di server development. Temukan bug, catat, fix di project lokal, verifikasi fix di web lokal.

</domain>

<decisions>
## Implementation Decisions

### Alur Kerja (sama seperti Phase 264-266)
- **D-01:** Test/UAT dilakukan di **web lokal** (`https://localhost:7241/`)
- **D-02:** Bug/temuan dicatat, langsung diperbaiki di **coding lokal**
- **D-03:** Verifikasi fix langsung di **web lokal**
- **D-04:** Setelah semua fix terverifikasi, kirim ke IT untuk deploy ke server dev

### Cara Simulasi Gangguan
- **D-05:** Gunakan **Playwright intercept** untuk semua simulasi: route intercept (block request), page.reload(), close+reopen tab
- **D-06:** Otomatis dan reproducible — tidak perlu manual DevTools

### Skenario Timer Habis
- **D-07:** Buat **assessment baru dengan durasi 1-2 menit** di server dev
- **D-08:** Biarkan timer habis secara natural, verifikasi behavior (auto-submit/block/pesan)
- **D-09:** Paling realistis — test end-to-end termasuk server-side enforcement (2-min grace period)

### Pembagian Worker
- **D-10:** **Regan** — test resilience utama (koneksi putus, refresh, resume, tab close)
- **D-11:** **Arsyad** — buat assessment baru, khusus test timer habis (skenario terpisah)
- **D-12:** Perlu buat assessment baru di server dev untuk kedua worker (assessment lama sudah completed/submitted)

### Claude's Discretion
- Urutan skenario test (mana dulu yang dijalankan)
- Detail Playwright intercept pattern (route blocking, timing)
- Query database untuk verifikasi data integrity setelah gangguan
- Setup assessment baru (judul, durasi, jumlah soal) untuk test edge cases

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Resilience Code (StartExam)
- `Views/CMP/StartExam.cshtml` — pendingAnswers retry queue (line ~387), offline badge (line ~400-506), resume modal (line ~768-775), prePopulateAnswers (line ~530-567), wall-clock timer, auto-save debounce
- `wwwroot/js/assessment-hub.js` — SignalR onreconnecting/onreconnected/onclose handlers, JoinBatch after reconnect

### Server-side Handlers
- `Controllers/CMPController.cs` — SaveAnswer (autosave upsert), UpdateSessionProgress (elapsed+page), StartExam (resume detection), SubmitExam (2-min grace period enforcement)

### Data Models
- `Models/AssessmentSession.cs` — StartedAt, DurationMinutes, Status, ElapsedSeconds, CurrentPage
- `Models/UserResponse.cs` — PackageUserResponses (per-question autosaved answers)

### Prior Phase Context
- `.planning/phases/265-worker-exam-flow/265-CONTEXT.md` — D-05/D-06: network indicator scope, offline deferred to 267
- `.planning/phases/266-review-submit-hasil/266-CONTEXT.md` — ExamSummary merge fix (TempData + DB fallback)
- `.planning/REQUIREMENTS.md` — EDGE-01 through EDGE-07

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- pendingAnswers Map: stores failed saves, flushed on reconnect via flushPendingAnswers()
- Network badge system: Live/Saving/Tersimpan/Offline states with visual indicators
- Resume modal: detects non-first page on load, offers "lanjutkan dari soal X"
- prePopulateAnswers(): restores radio selections from SAVED_ANSWERS (ViewBag.SavedAnswers from DB)
- Wall-clock timer: anchored to Date.now(), drift-proof, syncs with server REMAINING_SECONDS

### Established Patterns
- Auto-save: radio change → debounce 300ms → fetch SaveAnswer → retry 3x exponential backoff → queue if all fail
- Session progress: every 30 sec → POST UpdateSessionProgress (elapsed, currentPage)
- SignalR reconnect: automatic with withAutomaticReconnect(), JoinBatch re-sent on reconnect
- Timer enforcement: client-side countdown + server-side SubmitExam checks elapsed vs allowed+2min grace

### Integration Points
- StartExam GET: checks session status, loads saved answers from DB, computes remaining time
- Resume flow: if Status=="InProgress" and StartedAt exists → compute remaining → show resume modal
- Timer expired modal: shown when REMAINING_SECONDS <= 0 on resume load

</code_context>

<specifics>
## Specific Ideas

- Regan: mulai ujian → jawab beberapa soal → intercept koneksi (block SaveAnswer) → jawab lagi → restore koneksi → verifikasi pending answers flushed
- Regan: di tengah ujian → close tab → reopen → verifikasi resume modal muncul, jawaban tetap tercentang, timer lanjut
- Regan: di tengah ujian → page.reload() → verifikasi jawaban tidak hilang, posisi halaman benar, timer akurat
- Arsyad: buat assessment 1-2 menit → mulai ujian → tunggu timer habis → verifikasi behavior (auto-submit/block/pesan)
- Arsyad: timer habis → coba submit manual → verifikasi server reject dengan pesan error

</specifics>

<deferred>
## Deferred Ideas

- Stress test: banyak user simultan mengerjakan ujian → bukan scope UAT ini
- Admin monitoring real-time → Phase 268

</deferred>

---

*Phase: 267-resilience-edge-cases*
*Context gathered: 2026-03-28*
