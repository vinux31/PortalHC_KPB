# Phase 406: Admin Config UI + Riwayat HC - Context

**Gathered:** 2026-06-21
**Status:** Ready for planning

<domain>
## Phase Boundary

UI Admin/HC untuk fitur ujian ulang (backend 405 sudah jadi, 0 migration): (1) **card "Ujian Ulang"** di `ManagePackages.cshtml` (mirror card shuffle) + binding form `CreateAssessment`/`EditAssessment` — atur AllowRetake/MaxAttempts/RetakeCooldownHours per-assessment, hide untuk Pre-Test/Manual; (2) **view riwayat percobaan HC** di `AssessmentMonitoringDetail.cshtml` — semua attempt per-pekerja (archived + current) dengan detail per-soal. Konsumsi endpoint `UpdateRetakeSettings` + ViewBag (405-04) + arsip `AssessmentAttemptResponseArchive` (405). RTK-05, RTK-08.

</domain>

<decisions>
## Implementation Decisions

### Locked dari spec/405 (carry-forward, tak di-discuss ulang)
- `RetakeRules.ShouldHideRetakeToggle(assessmentType, isManualEntry)` sembunyikan card untuk Pre-Test/Manual (helper pure 405 siap).
- Binding data sudah di-expose ViewBag di `AssessmentAdminController.cs:5703-5711`: `AllowRetake`/`MaxAttempts`/`RetakeCooldownHours`/`HideRetakeToggle`/`RetakeMaxAttemptsUsedInGroup`.
- Endpoint `UpdateRetakeSettings:5564` (RBAC Admin/HC + AntiForgery + sibling propagation Title/Category/Schedule.Date + clamp + audit + PRG) — sudah ada, card POST ke sini.
- Arsip per-soal `AssessmentAttemptResponseArchive` (QuestionText/AnswerText/IsCorrect/AwardedScore/AttemptHistoryId) + `AssessmentAttemptHistory` (AttemptNumber/Score/IsPassed/CompletedAt) = sumber data riwayat.
- Card mirror pola card shuffle (`ManagePackages.cshtml:87-117`).

### Gray areas di-discuss (2026-06-21)

- **D-01 (Presentasi Riwayat HC, RTK-08):** **Modal per-pekerja.** Tombol di baris peserta `AssessmentMonitoringDetail` → Bootstrap modal berisi daftar attempt (accordion: "Percobaan ke-N: skor% — Lulus/Gagal — tanggal") + expand per-soal. Read-only view → modal lebih ringan dari page; konsisten dgn modal existing (`#extraTimeModal`/`#akhiriSemuaModal`/`#reshuffleResultModal`). BUKAN page terpisah (EssayGrading page krn editing; riwayat cuma view) dan BUKAN inline-expand (tabel jadi ramai). Tandai attempt mana yang current.
- **D-02 (Kedalaman per-soal):** **Penuh** — tiap soal tampil teks soal (`QuestionText`) + jawaban pekerja (`AnswerText`, essay full-text) + ✓/✗ (`IsCorrect`, null=essay pending) + skor per-soal (`AwardedScore`). HC perlu lihat KENAPA pekerja gagal; ini tujuan snapshot D-10. HTML-escape semua field (XSS — `@`-encode/`.textContent`, JANGAN Html.Raw).
- **D-03 (Lock & warning card):** **No-lock + warning inline.** Card retake **TIDAK** di-lock saat ujian mulai (beda dari shuffle yang lock — krn D-02 retroaktif butuh config bisa diubah kapan saja). Warning **non-blocking** inline (alert kuning dekat input `MaxAttempts`) saat `MaxAttempts < RetakeMaxAttemptsUsedInGroup` (ViewBag) — peringatan saja, tidak blokir simpan.
- **D-04 (Layout field card):** **Number input + helper + progressive disclosure.** Toggle `AllowRetake` (checkbox, mirror shuffle); saat ON → reveal 2 input `<input type="number">`: `MaxAttempts` (min 1 max 5) + `RetakeCooldownHours` (min 0 max 168) dengan helper text ("0 = tanpa jeda", satuan "jam"). Stacked dalam card. Card ditempatkan **setelah card shuffle** di ManagePackages. Mirror styling/markup card shuffle. (Server tetap `Math.Clamp` — client min/max = UX, bukan satu-satunya guard.)

### Claude's Discretion
- Markup detail (kelas Bootstrap, ikon bi-*), id elemen, struktur accordion modal.
- Penempatan binding di `CreateAssessment` Step 3 (antara Scoring & Certificate per spec §8) + `EditAssessment` (dekat PassPercentage).
- Apakah riwayat modal load via ViewBag pre-rendered atau AJAX endpoint (planner pilih; data per-pekerja bisa banyak — pertimbangkan lazy-load bila berat, tapi default pre-render bila ringan).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & Requirements
- `docs/superpowers/specs/2026-06-19-attempt-retake-assessment-design.md` §8 (UI Config Admin — card mirror shuffle, binding Create/Edit, lock-note) + §9 (UI Riwayat Percobaan — pekerja+HC, drill per-soal).
- `.planning/REQUIREMENTS.md` — RTK-05 (UI config admin), RTK-08 (riwayat HC).

### Mirror / pola yang diikuti
- `Views/Admin/ManagePackages.cshtml:87-117` — card shuffle (toggle + lock + warning) = pola card retake. **Card retake JANGAN tiru lock-when-started** (D-03).
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — tabel per-peserta (`Model.Sessions` foreach `:241`, `<tr data-session-id>` `:277`, dropdown action `:326`) = tempat tombol Riwayat; modal existing (`#extraTimeModal :129`, `#akhiriSemuaModal :205`, `#reshuffleResultModal :481`) = pola modal.
- `Views/Admin/CreateAssessment.cshtml` (Step 3) + `Views/Admin/EditAssessment.cshtml` — binding form.
- `Controllers/AssessmentAdminController.cs:5564` `UpdateRetakeSettings` (POST target) + `:5703-5711` ViewBag exposure.
- `Models/AssessmentAttemptResponseArchive.cs` + `Models/AssessmentAttemptHistory.cs` — data riwayat per-soal + per-attempt.
- `Helpers/RetakeRules.cs` `ShouldHideRetakeToggle` — hide-card guard.

### Codebase maps
- `.planning/codebase/CONVENTIONS.md` — konvensi view/Razor + XSS.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Card shuffle (`ManagePackages.cshtml:87-117`) — copy struktur card (form POST, checkbox toggle, label), buang lock-logic, tambah 2 number input.
- Modal existing di AssessmentMonitoringDetail (Bootstrap `data-bs-toggle="modal"`) — pola untuk modal riwayat.
- ViewBag retake (405-04) — semua data card siap; `RetakeMaxAttemptsUsedInGroup` untuk warning D-03.
- `AssessmentAttemptResponseArchive` — per-soal snapshot siap query untuk modal riwayat.

### Established Patterns
- Form config POST ke action + `[ValidateAntiForgeryToken]` + PRG (TempData success) — `UpdateRetakeSettings` sudah implement; card cukup POST ke sana.
- `@OrgLabels`/i18n + Bootstrap card + bi-* ikon konsisten app-wide.
- Razor dynamic UI WAJIB diverifikasi runtime Playwright (Lesson Phase 354: grep+build tak cukup untuk Razor).

### Integration Points
- ManagePackages card → `UpdateRetakeSettings` (existing).
- AssessmentMonitoringDetail modal → query `AssessmentAttemptHistory` + `AssessmentAttemptResponseArchive` per session/worker (ViewBag pre-render atau AJAX — planner).
- CreateAssessment/EditAssessment form fields → bind ke AllowRetake/MaxAttempts/RetakeCooldownHours (jalur create EF default sudah cover; form cukup set nilai).

</code_context>

<specifics>
## Specific Ideas

- Modal riwayat: header "Riwayat Percobaan — {nama pekerja}", body accordion per-attempt (badge Lulus/Gagal warna), expand → tabel per-soal (No / Soal / Jawaban / ✓✗ / Skor). Tandai attempt current ("Percobaan saat ini").
- Warning D-03: `@if (ViewBag.MaxAttempts < ViewBag.RetakeMaxAttemptsUsedInGroup) { <div class="alert alert-warning">...</div> }` dekat input.
- XSS: teks soal + jawaban pekerja di modal = user-content → encode (`@Html.DisplayFor`/`.textContent`/`@`-default), JANGAN `Html.Raw`.

</specifics>

<deferred>
## Deferred Ideas

None — dalam scope 406. Riwayat percobaan PEKERJA (Results/Records) + gating tier feedback + endpoint worker = Phase 407. Test menyeluruh + Playwright lifecycle penuh + security = Phase 408.

</deferred>

---

*Phase: 406-admin-config-ui-riwayat-hc*
*Context gathered: 2026-06-21*
