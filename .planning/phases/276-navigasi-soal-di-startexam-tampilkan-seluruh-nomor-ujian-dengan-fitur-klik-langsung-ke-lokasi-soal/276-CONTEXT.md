# Phase 276: Navigasi soal di StartExam - Context

**Gathered:** 2026-03-31
**Status:** Ready for planning

<domain>
## Phase Boundary

Enhancement navigasi soal di halaman StartExam — menampilkan SELURUH nomor soal (bukan hanya soal di halaman saat ini) dengan fitur klik untuk langsung loncat ke lokasi soal tertentu. Panel navigasi akan menampilkan semua nomor soal dari 1 sampai N dalam grid layout dengan indikator status jawaban.

</domain>

<decisions>
## Implementation Decisions

### Tampilan Panel untuk Banyak Soal
- **D-01:** Grid layout (multi-kolom) untuk menampilkan semua nomor soal
- **D-02:** Grid 10 kolom — soal 1-10 dalam satu baris (sejajar dengan 10 soal per halaman)

### Indikator Visual
- **D-03:** 2 warna saja — Hijau (answered/sudah diisi jawaban), Abu-abu (unanswered/belum diisi)
- **D-07:** Current question tidak perlu penandaan khusus (border, bold, atau warna berbeda). User bisa melihat posisi dari scroll position

### Behavior Klik Navigasi
- **D-04:** Klik nomor soal → langsung loncat ke halaman + scroll ke soal tersebut, tanpa animasi (immediate jump)
  - Klik soal di halaman yang sama: scroll ke soal tersebut
  - Klik soal di halaman berbeda: switch page terlebih dahulu, lalu scroll ke soal

### Layout & Responsiveness
- **D-05:** Desktop — panel kanan tetap visible (col-lg-3). Mobile — panel collapsed/hidden dengan toggle button
- **D-06:** Header panel text: "Daftar Soal" (menggantikan "Questions this page")

### Technical Implementation
- **D-08:** Modify existing `updatePanel()` function untuk render ALL questions, bukan hanya current page

### Claude's Discretion
- **D-09:** Auto-scroll ke current question saat page change — panel otomatis scroll menampilkan questions di halaman saat ini. Ini membantu user tetap oriented terutama untuk assessment dengan banyak soal.
- CSS styling detail (padding, gap, border-radius untuk badge grid)
- Exact implementation untuk auto-scroll behavior (smooth vs instant scroll)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Existing Navigation Panel
- `Views/CMP/StartExam.cshtml` (lines 159-177) — Question panel HTML structure
- `Views/CMP/StartExam.cshtml` (lines 772-791) — `updatePanel()` function yang perlu dimodifikasi
- `Views/CMP/StartExam.cshtml` (line 163) — Header text "Questions this page" yang perlu diganti

### Related Code
- `Views/CMP/StartExam.cshtml` (lines 334-339) — `pageQuestionIds` array mapping (page index → question IDs)
- `Views/CMP/StartExam.cshtml` (line 331) — `answeredQuestions` Set untuk tracking jawaban
- `Views/CMP/StartExam.cshtml` (lines 82-90) — Page structure dengan `id="page_N"` untuk navigation

### Prior Context
- `.planning/phases/265-worker-exam-flow/265-CONTEXT.md` — Exam flow context, pagination pattern
- `.planning/phases/272-block-submit-jika-belum-semua-soal-terisi/272-CONTEXT.md` — Answer tracking pattern

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `pageQuestionIds` array — sudah ada mapping dari page index ke array of question IDs
- `answeredQuestions` Set — tracking jawaban, bisa dipakai untuk menentukan warna badge
- `updatePanel()` function — pattern rendering badge numbers, tinggal dimodifikasi untuk loop ALL questions
- `#questionPanel` dan `#panelNumbers` HTML structure — container untuk grid badges
- Bootstrap badge classes (`bg-success`, `bg-secondary`) — warna untuk answered/unanswered
- `#togglePanelBtn` dan `togglePanel()` function — collapsible panel pattern

### Established Patterns
- 10 questions per page (constant `QUESTIONS_PER_PAGE`)
- Client-side page switching via `changePage()` function
- Panel sticky position (top: 70px)
- Panel visibility toggle (show/hide)

### Integration Points
- StartExam.cshtml: Line 772 `updatePanel()` — modifikasi untuk render semua questions
- StartExam.cshtml: Line 163 header text — ganti "Questions this page" → "Daftar Soal"
- StartExam.cshtml: Question cards (`#qcard_{qId}`) — untuk scroll position saat klik nomor soal
- CSS: Grid layout untuk badge container (display: grid, grid-template-columns: repeat(10, 1fr))

</code_context>

<specifics>
## Specific Ideas

- Grid 10 kolom: display: grid; grid-template-columns: repeat(10, 1fr); gap: 0.5rem;
- Badge styling: rounded-pill, font-size 0.85rem, padding 6px 10px, cursor pointer
- Click handler: onclick="jumpToQuestion({qId}, {pageNumber})"
- Mobile: panel default hidden via CSS media query, toggle button untuk show/hide

</specifics>

<deferred>
## Deferred Ideas

- "Jump to first unanswered" button — fitur tambahan yang bisa jadi phase terpisah
- "Jump to last answered" button — fitur tambahan yang bisa jadi phase terpisah
- Question search/filter di panel — enhancement untuk future phase
- Progress percentage indicator di panel — enhancement untuk future phase

</deferred>

---

*Phase: 276-navigasi-soal-di-startexam-tampilkan-seluruh-nomor-ujian-dengan-fitur-klik-langsung-ke-lokasi-soal*
*Context gathered: 2026-03-31*
