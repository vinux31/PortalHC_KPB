# Phase 270: Perbaiki resume exam: notif lanjutkan mengerjakan dan redirect ke page 1 - Context

**Gathered:** 2026-03-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Memperbaiki alur resume exam: menyederhanakan modal notifikasi resume dan mengubah redirect agar selalu ke halaman 1 (bukan halaman terakhir).

</domain>

<decisions>
## Implementation Decisions

### Modal Resume
- **D-01:** Modal resume disederhanakan — cukup tampilkan teks "Lanjutkan" tanpa info nomor soal atau detail lainnya. Hapus kalkulasi `resumeFromNum` dan elemen `resumePageNum`.
- **D-02:** Tombol tetap "Lanjutkan" dengan styling yang sudah ada.

### Redirect Target
- **D-03:** Saat worker klik "Lanjutkan" di modal resume, selalu redirect ke page 1 (index 0), bukan ke `LastActivePage`. Worker bisa navigasi sendiri ke soal yang belum dijawab.

### Claude's Discretion
- Styling/layout modal boleh disesuaikan selama tetap simpel dan konsisten dengan design system yang ada.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Exam Flow
- `Controllers/CMPController.cs` — StartExam action (line ~913): logika `IsResume`, `LastActivePage`, `ElapsedSeconds`
- `Views/CMP/StartExam.cshtml` — Modal resume (line ~182-202), redirect logic (line ~794-812), `resumeConfirmModal`

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Modal `resumeConfirmModal` sudah ada di StartExam.cshtml — perlu disederhanakan isinya
- Bootstrap Modal sudah dipakai — tidak perlu library baru

### Established Patterns
- Resume detection via `ViewBag.IsResume` dan `RESUME_PAGE` JavaScript variable
- Page navigation via `currentPage` variable dan `document.getElementById('page_' + currentPage)`

### Integration Points
- `StartExam.cshtml` line ~794-812: blok `if (IS_RESUME && RESUME_PAGE > 0)` — ubah redirect dari `RESUME_PAGE` ke `0`
- Modal HTML di StartExam.cshtml — sederhanakan konten teks

</code_context>

<specifics>
## Specific Ideas

- User ingin modal resume sesimpel mungkin — hanya tulisan "Lanjutkan", tanpa nomor soal atau detail progress

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 270-perbaiki-resume-exam-notif-lanjutkan-mengerjakan-dan-redirect-ke-page-1*
*Context gathered: 2026-03-28*
