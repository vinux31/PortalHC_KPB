# Phase 300: Mobile Optimization - Context

**Gathered:** 2026-04-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Exam UI optimal di perangkat mobile untuk pekerja lapangan — navigasi sentuh berfungsi, antarmuka tidak terpotong, tombol mudah ditekan, timer tetap terlihat. Tidak menambah fitur baru, hanya mengoptimalkan StartExam yang sudah ada untuk layar kecil.

</domain>

<decisions>
## Implementation Decisions

### Navigasi soal mobile
- **D-01:** Sidebar `col-lg-3` disembunyikan di breakpoint < lg. Diganti offcanvas drawer Bootstrap dari kanan, dipicu tombol floating [≡] di sticky footer
- **D-02:** Offcanvas berisi grid nomor soal (sama seperti sidebar desktop). Klik nomor langsung navigate ke soal tersebut dan tutup drawer
- **D-03:** Sidebar desktop tetap ada di lg+ — tidak ada perubahan di desktop

### Touch target & layout
- **D-04:** Mobile (< 768px): list-group-item min-height 48px, padding 12px 16px. form-check-input scale 1.4 (naik dari 1.2). card-body padding 16px
- **D-05:** Desktop tetap tidak berubah — semua perubahan di dalam `@media (max-width: 767.98px)`
- **D-06:** Responsive breakpoint utama: < 992px (lg) untuk sidebar → offcanvas switch

### Sticky controls mobile
- **D-07:** Sticky footer fixed di bawah layar (< lg) berisi: tombol Prev + tombol offcanvas [≡] + tombol Next/Submit. Selalu terlihat tanpa scroll
- **D-08:** Timer di header mobile: compact — hanya angka timer + badge save status. Label "Time Remaining" dan hub status badge disembunyikan di mobile
- **D-09:** Title assessment di header mobile: truncate dengan ellipsis jika terlalu panjang

### Swipe & anti-copy compat
- **D-10:** TANPA swipe gesture — navigasi halaman hanya via tombol Prev/Next di sticky footer. Ini menjaga 100% compatibility dengan anti-copy Phase 280
- **D-11:** Anti-copy events (copy/cut/paste/contextmenu/selectstart/dragstart + user-select: none) tidak perlu dimodifikasi — tidak konflik dengan layout responsive

### Essay textarea di mobile
- **D-12:** Biarkan default browser handling untuk virtual keyboard. Tidak ada handling khusus (auto-scroll, fullscreen overlay, dll)

### Landscape vs portrait
- **D-13:** Dual optimization — portrait: offcanvas drawer + sticky footer. Landscape: sidebar kembali tampil (seperti desktop mini) karena ada cukup ruang horizontal
- **D-14:** Landscape detection via `@media (orientation: landscape) and (max-width: 991.98px)` — tampilkan sidebar, sembunyikan offcanvas trigger

### Page size di mobile
- **D-15:** Mobile (< lg): 5 soal per halaman (turun dari 10). Desktop tetap 10. Ini mengurangi scroll dan mempercepat navigasi di mobile
- **D-16:** Page size ditentukan server-side berdasarkan User-Agent atau client-side via JS (Claude's discretion)

### Claude's Discretion
- Implementasi detail offcanvas drawer (animasi, backdrop)
- CSS transition untuk page change
- Exact spacing values selama memenuhi 48dp minimum
- Page size detection mechanism (server-side vs client-side)
- Sticky footer height dan shadow styling
- Landscape sidebar width

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Exam UI (target refactor)
- `Views/CMP/StartExam.cshtml` — Current exam view: col-lg-9 + col-lg-3 layout, radio/checkbox options, page navigation, anti-copy section (line ~1263-1314)
- `Views/CMP/ExamSummary.cshtml` — Exam summary before submit (may need minor responsive fixes)

### Anti-copy (must not break)
- `Views/CMP/StartExam.cshtml` lines 1263-1314 — Phase 280 anti-copy: blocks copy/cut/paste/contextmenu/selectstart/dragstart + user-select:none on `.exam-protected`

### Controllers
- `Controllers/CMPController.cs` — StartExam action, ExamSummary action. Page size constant may need parameterization

### Prior phase decisions
- `.planning/phases/298-question-types/298-CONTEXT.md` — D-19 (MA checkbox), D-20 (Essay textarea), D-24 (sidebar nav panel)

### Requirements
- `.planning/REQUIREMENTS.md` — MOB-01 through MOB-06

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Bootstrap 5 offcanvas component — sudah tersedia via CDN, tidak perlu library baru
- `sticky-top` class sudah dipakai di header exam — pattern bisa di-reuse untuk footer
- `col-lg-9` / `col-lg-3` grid sudah ada — breakpoint lg sebagai pivot point
- SignalR auto-save — sudah handle mobile (tidak perlu modifikasi)

### Established Patterns
- `questionsPerPage = 10` hardcoded di StartExam.cshtml line 5 — perlu conditional
- Anti-copy menggunakan inline `<script>` dan `<style>` di dalam view — CSS mobile bisa ditambah di tempat yang sama
- Page navigation via `changePage()` JS function — bisa di-reuse oleh offcanvas click handler

### Integration Points
- `StartExam.cshtml` — semua perubahan dalam satu file (CSS + HTML + JS)
- `CMPController.cs StartExam action` — jika page size server-side, perlu parameter
- Header sticky div (`#examHeader`) — perlu responsive classes
- Page nav buttons — duplikasi ke sticky footer di mobile, hide inline buttons

</code_context>

<specifics>
## Specific Ideas

- Offcanvas drawer trigger button [≡] diletakkan di TENGAH sticky footer, antara Prev dan Next
- Timer compact di mobile: hanya "12:34" tanpa label, badge save status tetap ada
- Landscape mode: sidebar tampil kembali dengan width lebih kecil (~200px vs col-lg-3)

</specifics>

<deferred>
## Deferred Ideas

- **Swipe gesture navigation** — Ditunda karena belum diperlukan dan menjaga compatibility. Bisa ditambah di phase berikutnya jika ada feedback pekerja
- **Fullscreen essay mode** — Textarea expand fullscreen saat focus. Lebih immersive tapi kompleks
- **Pull-to-refresh** — Tidak relevan untuk exam (data sudah loaded)

</deferred>

---

*Phase: 300-mobile-optimization*
*Context gathered: 2026-04-07*
