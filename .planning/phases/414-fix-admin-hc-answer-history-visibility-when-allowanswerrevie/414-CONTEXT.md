# Phase 414: Fix Visibilitas History Jawaban Admin/HC saat AllowAnswerReview OFF - Context

**Gathered:** 2026-06-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Bugfix off-theme (BUKAN bagian tema Flexible Add/Remove v32.5, tidak menambah REQ ke 11/11). Decouple gate per-soal "Tinjauan Jawaban" di `CMP/Results` dari toggle `AllowAnswerReview` **berdasarkan owner-vs-non-owner**:

- **Non-owner** yang sudah lolos `IsResultsAuthorized` (Admin L1 / HC L2 / Direktur-VP-Manager L3 / SectionHead-SrSupervisor L4 section-scoped) **selalu** melihat history jawaban per-soal peserta, terlepas dari nilai toggle `AllowAnswerReview`.
- **Owner** (peserta melihat hasilnya sendiri, `assessment.UserId == currentUser.Id`) **tetap** di-gate toggle: `AllowAnswerReview` OFF → tidak melihat review (perilaku worker-facing tidak berubah).

Scope file: `Controllers/CMPController.cs` (action `Results`), `Models/AssessmentResultsViewModel.cs`, `Views/CMP/Results.cshtml`, + unit test seam. migration=FALSE. TIDAK menambah REQ.
</domain>

<decisions>
## Implementation Decisions

### Bentuk flag efektif (ViewModel)
- **D-01:** Tambah **field VM baru** `CanReviewAnswers` (bool, nilai efektif). `AllowAnswerReview` di VM **tetap = raw toggle** `assessment.AllowAnswerReview` (makna asli utuh — view butuh membedakan "toggle OFF tapi admin tetap lihat"). Controller mengisi keduanya; gate per-soal di view pindah ke `Model.CanReviewAnswers`. (Sesuai arah ROADMAP: "expose field efektif di VM, view gate pakai flag baru".)

### Cakupan bypass (siapa selalu lihat history)
- **D-02:** **Semua non-owner** di-bypass. Efektif: `CanReviewAnswers = assessment.AllowAnswerReview || (currentUser.Id != assessment.UserId)`. Owner (`currentUser.Id == assessment.UserId`) → hanya lihat saat toggle ON. Aman karena setiap non-owner yang mencapai titik ini SUDAH lolos `IsResultsAuthorized` (hanya Admin/HC/L3/L4-section-scoped) — owner-check tunggal cukup, tak perlu cek role lagi.

### Pola seam + test
- **D-03:** Ekstrak **pure static helper** `CanReviewAnswers(bool allowAnswerReview, bool isOwner)` di `CMPController` (pola identik `IsResultsAuthorized` / `IsParticipantRemoved` — static, no-DB, testable). Body: `return allowAnswerReview || !isOwner;`. Unit test (xUnit, no DB): non-owner+OFF→true, non-owner+ON→true, owner+OFF→false, owner+ON→true. Action `Results` memanggil helper ini untuk gate build `questionReviews` (L2266) **dan** mengisi `viewModel.CanReviewAnswers` (L2379 region). Gate build & VM flag WAJIB pakai sumber yang sama (kalau hanya satu yang diubah → null-data atau hide tak konsisten).

### Pesan untuk Admin saat toggle OFF
- **D-04:** **Tampil review + nota admin.** Saat review tampil hanya karena bypass non-owner (`CanReviewAnswers == true && AllowAnswerReview == false`), tampilkan badge/alert kecil di view: "Peserta tak bisa melihat tinjauan ini (Tinjauan Jawaban OFF)". Owner + OFF → tetap alert lama "Tinjauan jawaban tidak tersedia untuk assessment ini." Kondisi nota **diturunkan di view** dari kombinasi 2 flag (`CanReviewAnswers && !AllowAnswerReview`) — tak butuh field VM ke-3.

### Claude's Discretion
- Teks/ikon/warna persis badge nota admin (info vs secondary), penempatan (header card vs di atas list). Penamaan helper boleh `CanReviewAnswers` atau sinonim selama static+pure.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Bug site & pola
- `Controllers/CMPController.cs` action `Results` (L2207) — gate build review **L2266** `if (assessment.AllowAnswerReview)`; VM build region **L2370–2389** (`AllowAnswerReview = assessment.AllowAnswerReview` L2379); auth **L2218–2219** `IsResultsAuthorized`.
- `Controllers/CMPController.cs:2526` — `IsResultsAuthorized(ownerUserId, currentUserId, roleLevel, currentUserSection, ownerSection)` (pola pure static helper + makna owner/non-owner).
- `Controllers/CMPController.cs:2540` — `IsParticipantRemoved` (contoh kedua pure static seam testable).
- `Models/AssessmentResultsViewModel.cs:12` — `public bool AllowAnswerReview` (tambah `CanReviewAnswers` di sini).
- `Views/CMP/Results.cshtml:316` — `@if (Model.AllowAnswerReview && Model.QuestionReviews != null)` (gate tampil); **L413** `else if (!Model.AllowAnswerReview)` alert "tidak tersedia".
- `.planning/ROADMAP.md` §"Phase 414" (L155–171) — arah fix + catatan off-theme (no REQ, migration=FALSE).

No external spec/ADR — fix sepenuhnya tercakup di code anchors + decisions di atas.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IsResultsAuthorized` / `IsParticipantRemoved` (CMPController) — template pure static helper testable; tiru untuk `CanReviewAnswers`.
- `AssessmentScoreAggregator.IsQuestionCorrect` — sudah dipakai di build `questionReviews`; tak tersentuh fix ini.

### Established Patterns
- Pure static method untuk keputusan authorize/gate (no-DB) → di-unit-test langsung tanpa `WebApplicationFactory`. Sesuai lesson 999.12 (hindari replica tautologis).
- VM-flag → view-gate: view hanya baca `Model.*`, tak ada logika role di Razor.

### Integration Points
- Hanya `Results` action (1 jalur package; legacy path L2391 sudah `AllowAnswerReview = false` permanen — owner-only/empty, biarkan; tapi cek apakah perlu `CanReviewAnswers` di branch legacy juga → planner putuskan, default ikut helper).
- View consumer flag: L316 (gate) + L413 (alert) + nota baru (D-04).

### Hati-hati
- Ada worktree stale `.claude/worktrees/pensive-saha-4b1351/` berisi salinan CMPController/VM lama — JANGAN edit di sana, kerja di main tree.
</code_context>

<specifics>
## Specific Ideas

Owner-check tunggal (`currentUser.Id != assessment.UserId`) sengaja dipilih ketimbang re-cek roleLevel: titik kode pasca-`IsResultsAuthorized` dijamin hanya non-owner berwenang. Helper menerima `isOwner` (bool sudah dihitung) agar pure & bebas dari detail role.
</specifics>

<deferred>
## Deferred Ideas

### DEF-01 — Fasilitas grant akses / share URL hasil ke atasan (non-owner) + keterkaitan tinjau jawaban essay (NEW CAPABILITY → fase sendiri)
Dari jawaban discuss Q2: user ingin HC/Admin bisa **memberikan akses / mengirim URL** hasil+tinjauan ke non-owner (atasan) agar dia bisa melihat & me-review, "kemungkinan ada hubungannya dengan tinjau jawaban essay (perlu analisa ulang)".

**Kenapa ditunda:** Ini kapabilitas baru (access-provisioning + shareable link + kemungkinan delegasi review essay), BUKAN sekadar decouple gate. Phase 414 tetap sempit = non-owner berwenang selalu lihat history. Catatan: atasan **sesama Section** SUDAH otomatis berwenang via `IsResultsAuthorized` (L4 section-scoped), jadi setelah 414 mereka langsung bisa lihat review tanpa fasilitas tambahan. Fasilitas baru dibutuhkan untuk: grant eksplisit, akses lintas-section, link shareable bertoken, dan/atau surface review essay (lihat halaman `/Admin/EssayGrading` v30.0).

**Tindak lanjut:** butuh discuss/spec terpisah → kandidat fase baru (atau backlog 999.x). Analisa: model akses (token-link vs grant-table), expiry, audit, tie-in EssayGrading.

### None lain — discussion stayed within phase scope (selain DEF-01).
</deferred>

---

*Phase: 414-fix-admin-hc-answer-history-visibility-when-allowanswerrevie*
*Context gathered: 2026-06-22*
