# Phase 302: Accessibility WCAG Quick Wins - Context

**Gathered:** 2026-04-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Fitur aksesibilitas dasar pada halaman ujian StartExam — skip link, keyboard navigation, extra time per assessment, dan auto-focus saat pindah halaman. Tidak menambah fitur baru di luar aksesibilitas. Screen reader support dan font size control di-drop dari scope.

</domain>

<decisions>
## Implementation Decisions

### Skip Link
- **D-01:** Skip link "Lewati ke konten utama" ditambahkan hanya di StartExam.cshtml (bukan _Layout global)
- **D-02:** Hidden by default, muncul saat user menekan Tab pertama kali (visually-hidden + :focus-visible pattern)
- **D-03:** Target skip link: area soal (main content container), melewati header/timer/sidebar

### Keyboard Navigation
- **D-04:** Navigasi antar opsi jawaban dalam satu soal menggunakan Arrow Up/Down keys (native radio group behavior untuk MC, native checkbox behavior untuk MA)
- **D-05:** Navigasi antar soal menggunakan Tab — setelah selesai opsi soal 1, Tab terus ke soal 2
- **D-06:** Sticky footer mobile (Prev, offcanvas trigger, Next) masuk tab order natural — bisa diakses via Tab
- **D-07:** Essay textarea: Tab masuk ke textarea, Tab keluar ke elemen berikutnya (native behavior)

### Extra Time
- **D-08:** Extra time berlaku per assessment (semua peserta), bukan per individu/sesi
- **D-09:** Peserta yang sudah submit tidak terpengaruh oleh penambahan extra time
- **D-10:** UI: tombol "Extra Time" di halaman AssessmentMonitoring, klik buka modal dengan input waktu
- **D-11:** Range: 5-120 menit, kelipatan 5 (dropdown atau number input step=5)
- **D-12:** Field baru ExtraTimeMinutes di model/tabel Assessment (bukan AssessmentSession)
- **D-13:** Timer peserta diupdate real-time via SignalR saat HC menambah extra time (peserta tidak perlu refresh)

### Auto-focus
- **D-14:** Saat pindah halaman soal (Prev/Next), focus otomatis berpindah ke card soal pertama di halaman baru
- **D-15:** Implementasi di `performPageSwitch()` — tambah `.focus()` ke elemen soal pertama setelah page switch

### Scope
- **D-16:** Semua fitur accessibility hanya diterapkan di halaman StartExam
- **D-17:** Anti-copy Phase 280 tidak konflik — hanya block Ctrl+C/A/U/S/P, tidak block Tab/Arrow/Enter/Space

### Dropped dari Scope
- **D-18:** Screen reader / aria-live timer announcement (A11Y-03) — DIHAPUS dari phase ini
- **D-19:** Font size control A+/A- (A11Y-04) — DIHAPUS dari phase ini

### Testing
- **D-20:** Validasi manual saja — Tab through halaman, test keyboard nav, test extra time flow. Tanpa automated tooling (axe-core)

### Claude's Discretion
- CSS styling untuk skip link (visually-hidden class)
- Exact focus outline styling
- Modal layout untuk extra time input
- SignalR message format untuk extra time update
- Apakah ExtraTimeMinutes pakai dropdown atau number input

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Exam UI (target utama)
- `Views/CMP/StartExam.cshtml` — Halaman ujian: soal, opsi, page navigation, timer, anti-copy section (line 1349-1358)
- `Views/CMP/StartExam.cshtml` line 913-958 — `changePage()` dan `performPageSwitch()` functions (target auto-focus)

### Anti-copy (must not break)
- `Views/CMP/StartExam.cshtml` lines 1349-1358 — Block Ctrl+C/A/U/S/P only, no conflict with Tab/Arrow/Enter/Space

### Monitoring (extra time UI)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — HC monitoring page, target untuk tombol extra time
- `Views/Admin/AssessmentMonitoring.cshtml` — Assessment list monitoring

### Models
- `Models/AssessmentSession.cs` — Session model (extra time field akan di Assessment, bukan di sini)

### Controllers
- `Controllers/CMPController.cs` — StartExam action
- `Controllers/AssessmentAdminController.cs` — AssessmentMonitoring, AssessmentMonitoringDetail

### Prior phase decisions
- `.planning/phases/300-mobile-optimization/300-CONTEXT.md` — D-07 sticky footer, D-10 tanpa swipe
- `.planning/phases/298-question-types/298-CONTEXT.md` — D-19 MA checkbox, D-20 Essay textarea, D-24 sidebar nav

### Requirements
- `.planning/REQUIREMENTS.md` — A11Y-01 through A11Y-06 (A11Y-03 dan A11Y-04 di-drop)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `performPageSwitch()` di StartExam.cshtml — sudah handle scroll to top, tinggal tambah focus()
- Bootstrap `visually-hidden` class — sudah tersedia via CDN untuk skip link
- SignalR ExamHub — sudah ada untuk auto-save, bisa di-extend untuk extra time broadcast
- Anti-copy keydown handler — sudah terbukti hanya block Ctrl+kombinasi, aman untuk keyboard nav

### Established Patterns
- Radio buttons untuk MC sudah native keyboard accessible (Arrow keys)
- Page navigation via `changePage()` JS function
- Modal pattern sudah dipakai di banyak halaman admin (Bootstrap modal)
- AJAX + JSON response pattern untuk admin actions

### Integration Points
- `performPageSwitch()` — tambah focus management setelah page switch
- `StartExam.cshtml` header area — tambah skip link di atas
- `AssessmentMonitoring/Detail` — tambah tombol extra time + modal
- SignalR ExamHub — tambah method broadcast extra time update
- DB migration — tambah ExtraTimeMinutes column di Assessment table
- Timer calculation — extend durasi exam dengan ExtraTimeMinutes

</code_context>

<specifics>
## Specific Ideas

- Extra time berlaku bulk (semua peserta) bukan individual — satu tombol di monitoring, bukan per baris
- Modal extra time cukup sederhana: input waktu + tombol simpan
- Skip link hanya di StartExam, bukan global — scope minimal sesuai "Quick Wins"

</specifics>

<deferred>
## Deferred Ideas

- **Screen reader support (A11Y-03)** — aria-live timer, ARIA labels per soal. Bisa jadi phase terpisah jika ada kebutuhan
- **Font size control (A11Y-04)** — Tombol A+/A- dengan localStorage persistence. Bisa ditambah kemudian
- **Skip link global di _Layout.cshtml** — Untuk semua halaman, bukan hanya exam. Phase accessibility lanjutan
- **Automated accessibility testing (axe-core)** — Integration testing otomatis untuk WCAG compliance

</deferred>

---

*Phase: 302-accessibility-wcag-quick-wins*
*Context gathered: 2026-04-07*
