# Phase 232: Audit Assessment Flow — Worker Side - Context

**Gathered:** 2026-03-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit worker-side assessment flow end-to-end: daftar assessment, token entry, exam page, timer, auto-save, submit, scoring, session resume, results page. Fix semua bug dan improve UX berdasarkan riset Phase 228. Termasuk full real-time SignalR implementation untuk worker-side notifications (folded todo). Proton exam flow (Tahun 1-2 reguler + Tahun 3 interview) diaudit mendalam.

</domain>

<decisions>
## Implementation Decisions

### Worker Assessment List
- **D-01:** Audit + improve UX: filter Open/Upcoming/Completed, assignment matching, empty state, pagination, search
- **D-02:** Tambah status badge visual: Open (hijau), Upcoming (biru), Completed (abu-abu), Expired (merah)
- **D-03:** Improve empty state: pesan informatif 'Belum ada assessment yang ditugaskan'
- **D-04:** Audit search by judul dan pagination berfungsi benar
- **D-05:** Completed assessment: audit existing behavior, fix inkonsistensi

### Token Entry
- **D-06:** Audit + improve UX: auto-focus input, paste support, clear error state, error messages jelas

### Timer & Auto-Save
- **D-07:** Audit akurasi + edge cases: timer sinkron dengan server ElapsedSeconds, auto-save per-click, browser tab switch handling, network disconnect mid-save, timer drift
- **D-08:** Saat timer habis: warning modal 'Waktu habis' dulu, user klik OK, baru submit

### Browser Close
- **D-09:** beforeunload warning dialog + jawaban sudah auto-saved per-click

### Exam Navigation
- **D-10:** Audit flow existing + fix bugs: Next/Prev/jump-to-question, LastActivePage terupdate, no data loss

### Real-time Worker Notifications (Folded Todo)
- **D-11:** Full real-time implementation: semua HC actions (Reset, Force Close, Bulk Close) trigger real-time update di worker exam page via SignalR
- **D-12:** HC Reset saat worker di exam page: SignalR notify → modal 'Session di-reset oleh HC' → redirect ke assessment list

### Session Resume & Recovery
- **D-13:** Full state restore: ElapsedSeconds lanjut, LastActivePage restore, semua jawaban pre-populated, timer lanjut
- **D-14:** Network disconnect: auto-retry save saat reconnect, visual indicator 'Offline' / 'Tersimpan' di exam page

### Scoring & Results
- **D-15:** Full audit scoring chain: score calculation, IsPassed logic, NomorSertifikat generation, competency level update, ElemenTeknis scoring
- **D-16:** Results page: audit + improve UX — highlight jawaban benar hijau/salah merah, section-by-section breakdown
- **D-17:** HC toggle 'allow review' berfungsi, jawaban benar/salah ditampilkan benar, score breakdown visible

### Proton Special Handling
- **D-18:** Deep audit Proton Tahun 1-2 exam reguler + Tahun 3 interview 5 aspek — kedua path end-to-end
- **D-19:** Deep audit Proton scoring: scoring per aspek, total score calculation, pass/fail threshold, NomorSertifikat generation

### Accessibility
- **D-20:** Skip untuk sekarang — fokus fungsionalitas dan bug fix

### Claude's Discretion
- Urutan audit per-action dalam setiap plan
- Detail level HTML report layout
- Pendekatan fix (refactor vs patch)
- Pembagian task antar plans

### Folded Todos
- **Real-time Assessment System** (dari Phase 133 checkpoint): Worker exam page harus update real-time saat HC Reset/Force Close via SignalR. Diimplementasikan sebagai bagian dari D-11/D-12.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Riset Best Practices (Phase 228 output)
- `docs/audit-assessment-training-v8.html` — Dokumen riset assessment dan exam flow best practices
- `.planning/phases/228-best-practices-research/228-CONTEXT.md` — Keputusan riset dan rekomendasi

### Phase 231 Context & Output (admin/HC side — complementary)
- `.planning/phases/231-audit-assessment-management-monitoring/231-CONTEXT.md` — Admin-side audit decisions, SignalR patterns
- `.planning/phases/231-audit-assessment-management-monitoring/231-01-SUMMARY.md` — ManageAssessment CRUD fixes
- `.planning/phases/231-audit-assessment-management-monitoring/231-02-SUMMARY.md` — Monitoring + HC actions fixes

### Requirements
- `.planning/ROADMAP.md` — Phase 232 success criteria (AFLW-01 s/d AFLW-05)
- `.planning/REQUIREMENTS.md` — Requirement definitions AFLW

### Kode Worker-Side (audit targets)
- `Controllers/CMPController.cs` — Assessment list (~182), StartExam (~664), AbandonExam (~921), ExamSummary GET (~1170) & POST (~1142), SubmitExam (~1250)
- `Views/CMP/Assessment.cshtml` — Worker assessment list
- `Views/CMP/StartExam.cshtml` — Token entry + exam page
- `Views/CMP/ExamSummary.cshtml` — Exam summary sebelum submit
- `Views/CMP/Results.cshtml` — Results page dengan answer review

### SignalR Infrastructure
- `Hubs/AssessmentHub.cs` — SignalR hub (jika ada)
- `wwwroot/js/` — Client-side SignalR handlers (audit existing)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- SignalR hub (`window.assessmentHub`) — sudah ada di monitoring detail, perlu extend ke worker exam page
- Auto-save pattern — sudah ada per-click save di exam page
- Excel import pattern — tidak relevan untuk phase ini

### Established Patterns
- CMPController class-level `[Authorize]` — semua actions require authentication
- TempData untuk success/error messages
- Assessment/ExamSummary/Results view chain untuk exam flow

### Integration Points
- StartExam: token validation → create/resume session → exam page
- SubmitExam: scoring → NomorSertifikat → competency update → results redirect
- SignalR: monitoring detail sudah punya hub, worker side perlu subscribe ke events
- AbandonExam: session state management

</code_context>

<specifics>
## Specific Ideas

- Timer habis harus tampilkan warning modal dulu, bukan langsung auto-submit
- HC Reset/Force Close harus real-time notify worker via SignalR — bukan menunggu next save gagal
- Results page harus visual: jawaban benar hijau, salah merah, dengan section breakdown
- Network disconnect indicator di exam page ('Offline' / 'Tersimpan') untuk transparansi ke worker
- Assessment Proton interview mode Tahun 3 (5 aspek) harus deep audit termasuk scoring formula

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 232-audit-assessment-flow-worker-side*
*Context gathered: 2026-03-22*
