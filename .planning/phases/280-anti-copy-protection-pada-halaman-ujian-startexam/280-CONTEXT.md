# Phase 280: Anti-copy protection pada halaman ujian StartExam - Context

**Gathered:** 2026-04-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Implementasi anti-copy protection pada halaman ujian (StartExam.cshtml) agar peserta tidak bisa meng-copy teks soal dan opsi jawaban. Hanya fitur anti-copy — anti-screenshot dan anti-tab-switch sudah di-exclude dari scope.

</domain>

<decisions>
## Implementation Decisions

### Feedback ke user
- **D-01:** Silent block — aksi copy/right-click di-block tanpa notifikasi atau pesan peringatan. User hanya merasa tidak bisa select/copy. Pendekatan sama seperti Moodle & Canvas.

### Cakupan proteksi
- **D-02:** Proteksi hanya pada area soal dan opsi jawaban (question container & answer options), bukan seluruh halaman. Header, navigasi soal, timer, dan elemen lain tetap bisa di-select.

### Keyboard shortcuts
- **D-03:** Block agresif — Ctrl+C (copy), Ctrl+A (select all), Ctrl+U (view source), Ctrl+S (save page), Ctrl+P (print). Standar platform ujian serius.

### Teknik implementasi
- **D-04:** CSS `user-select: none` + `-webkit-touch-callout: none` pada container soal
- **D-05:** JS event blocking: `copy`, `cut`, `paste`, `contextmenu`, `selectstart`, `dragstart` dengan `preventDefault()`
- **D-06:** Keyboard shortcut blocking via `keydown` event listener di level `document`

### Claude's Discretion
- CSS selector targeting (class/id untuk container soal)
- Exact event handler placement dalam existing JS code
- Cross-browser compatibility approach

</decisions>

<specifics>
## Specific Ideas

- Best practice dari platform ujian besar (Moodle, Canvas, ProProfs, ExamSoft) sudah di-riset sebelumnya
- Pendekatan ini adalah deterrence layer (cegah ~95% casual cheating), bukan proteksi absolut
- Semua handler attach di level document atau container soal, bukan per-element

</specifics>

<canonical_refs>
## Canonical References

No external specs — requirements are fully captured in decisions above.

### Existing exam code
- `Views/CMP/StartExam.cshtml` — Halaman ujian utama yang akan dimodifikasi
- `Controllers/CMPController.cs` — Backend exam logic (context untuk memahami flow)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `StartExam.cshtml` sudah punya event listener `visibilitychange` dan `beforeunload` — pattern JS event handling sudah ada

### Established Patterns
- Anti-forgery token pada form submission
- SignalR activity logging (ExamActivityLog) untuk audit trail
- Timer re-anchor pattern pada visibilitychange

### Integration Points
- JS anti-copy handlers ditambahkan di `<script>` section StartExam.cshtml
- CSS anti-copy ditambahkan di `<style>` section atau inline pada container soal

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 280-anti-copy-protection-pada-halaman-ujian-startexam*
*Context gathered: 2026-04-01*
