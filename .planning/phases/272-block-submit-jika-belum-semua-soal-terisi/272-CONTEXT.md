# Phase 272: Block Submit Jika Belum Semua Soal Terisi - Context

**Gathered:** 2026-03-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Cegah peserta submit ujian jika masih ada soal yang belum dijawab. Tombol submit di-disable dan validasi backend menolak submission yang tidak lengkap. Pengecualian: auto-submit saat waktu habis tetap dibolehkan.

</domain>

<decisions>
## Implementation Decisions

### Perilaku tombol submit
- **D-01:** Tombol "Kumpulkan Ujian" di ExamSummary di-**disable** jika ada soal yang belum dijawab
- **D-02:** Tampilkan pesan "Jawab semua soal terlebih dahulu" saat tombol disabled
- **D-03:** Tombol otomatis aktif begitu semua soal terjawab (tanpa reload — cek saat page load sudah cukup karena ExamSummary adalah halaman review)

### Level enforcement
- **D-04:** **Frontend** — disable button di ExamSummary.cshtml berdasarkan hitungan `unanswered`
- **D-05:** **Backend** — `SubmitExam` action di CMPController menolak submission jika ada soal tanpa jawaban, return error redirect
- **D-06:** Backend validasi: hitung soal yang tidak ada di dictionary `answers` atau optionId = 0, bandingkan dengan total shuffled questions

### Handling waktu habis (auto-submit)
- **D-07:** Auto-submit saat waktu habis **dikecualikan** dari blocking — submit tetap diterima meskipun ada soal kosong
- **D-08:** Pembeda: auto-submit bisa diidentifikasi via parameter atau flag yang membedakan dari submit manual (misalnya `isAutoSubmit` parameter atau cek server-side bahwa waktu sudah habis)

### Claude's Discretion
- Mekanisme teknis untuk membedakan auto-submit vs manual submit (parameter, timer check, dll)
- Exact wording pesan error di frontend dan backend
- Styling disabled button (grayed out, tooltip, dll)

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<canonical_refs>
## Canonical References

No external specs — requirements are fully captured in decisions above

### Key source files
- `Views/CMP/ExamSummary.cshtml` — Submit button & unanswered count display (line 81-109)
- `Controllers/CMPController.cs` SubmitExam action (line 1345-1548) — Backend submit handler
- `Controllers/CMPController.cs` ExamSummary action (line 1229-1343) — Review page data preparation

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `unanswered` variable sudah dihitung di ExamSummary.cshtml — bisa langsung dipakai untuk kondisi disable
- Confirm dialog JavaScript sudah ada — tinggal diganti dengan disable logic

### Established Patterns
- ExamSummary sudah menampilkan warning "Anda memiliki X soal yang belum dijawab" — pattern pesan sudah ada
- SubmitExam sudah punya pattern return error redirect via TempData

### Integration Points
- Frontend: ExamSummary.cshtml submit button area
- Backend: SubmitExam action — tambah validasi sebelum scoring logic
- Auto-submit: JavaScript timer handler yang trigger form submit saat waktu habis

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 272-block-submit-jika-belum-semua-soal-terisi*
*Context gathered: 2026-03-28*
