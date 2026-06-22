# v32.4 Requirements — Ujian Ulang (Attempt/Retake Assessment)

> **Status:** ACTIVE (milestone v32.4 started 2026-06-21).
> **Authoritative spec:** `docs/superpowers/specs/2026-06-19-attempt-retake-assessment-design.md`
> **Plan fase 405:** `docs/superpowers/plans/2026-06-19-v32.4-phase-405-backend-core.md` (PLAN READY)
> **Roadmap:** `.planning/ROADMAP.md` (Phases 405-408, wave `405 → (406 ∥ 407) → 408`)

## Ringkasan

Pekerja boleh **ujian ulang** assessment yang gagal (skor < `PassPercentage`), self-service, dengan kontrol Admin/HC (on/off + MaxAttempts + cooldown per-assessment). Reuse mesin `ResetAssessment` + `AttemptNumber` existing. **migration=TRUE** (Phase 405 only).

**10 keputusan terkunci:** D1 self-service pekerja (HC override) · D2 attempt terakhir = record (in-place reset) · D3 cooldown configurable per-assessment, default 24 jam · D4 setting per-assessment saja · D5 feedback skor+tanda-salah (kunci ditahan sampai lulus/habis) · D6 graded only (`AssessmentType != "PreTest"`) · D7 cap habis = lock + HC override · D8 MaxAttempts default 2 (range 1–5) · D9 riwayat ke pekerja + HC · D10 full snapshot per-soal.

## Requirements

| ID | Requirement | Acceptance Criteria | Fase |
|----|-------------|---------------------|------|
| **RTK-01** | Config fields per-assessment | `AllowRetake` (bool, default false), `MaxAttempts` (int, default 2, range 1–5), `RetakeCooldownHours` (int, default 24, range 0–168) di `AssessmentSession` + migration + ter-set di semua jalur create (EF default cukup; eksplisit copy di EditAssessment bulk-add). | 405 |
| **RTK-02** | Snapshot per-soal | Tabel `AssessmentAttemptResponseArchive` (FK→AssessmentAttemptHistory cascade, index AttemptHistoryId) + builder pure `RetakeArchiveBuilder.Build` (verdict via `IsQuestionCorrect`, jawaban via `BuildAnswerCell`, beku sebelum delete). | 405 |
| **RTK-03** | Aturan kelayakan (pure) | `RetakeRules.CanRetake` = AllowRetake && AssessmentType!=PreTest && !IsManualEntry && Status=="Completed" && IsPassed==false && attemptsUsed<MaxAttempts && cooldownElapsed. `ShouldHideRetakeToggle` = PreTest \|\| ManualEntry. Unit-tested semua cabang. | 405 |
| **RTK-04** | Endpoint config + propagation | `UpdateRetakeSettings(assessmentId, allowRetake, maxAttempts, retakeCooldownHours)` — `[Authorize(Admin,HC)]`+AntiForgery, sibling propagation key (Title/Category/Schedule.Date), audit `UpdateRetakeSettings`, clamp range. | 405 |
| **RTK-05** | UI config admin | Card "Ujian Ulang" di `ManagePackages.cshtml` (mirror card shuffle) + binding form `CreateAssessment`/`EditAssessment`; hide untuk Pre-Test/Manual; warning non-blocking bila MaxAttempts < attempt terpakai. | 406 |
| **RTK-06** | Refactor ResetAssessment | `ResetAssessment` HC delegasi ke `RetakeService.ExecuteAsync` (HC override = bypass cap/cooldown); guards HC (IsResettable, Pre/Post block, status) tetap di controller; ResetGuardTests regresi hijau. | 405 |
| **RTK-07** | Engine retake bersama | `RetakeService.ExecuteAsync` — claim transisi atomik DULU (anti double-archive), snapshot per-soal → archive `AttemptNumber+1` → delete responses/assignment/ET → audit (`RetakeAssessment` worker / `ResetAssessment` HC) → SignalR `reason` parameterized. | 405 |
| **RTK-08** | Riwayat percobaan HC | View drill-down di `AssessmentMonitoringDetail` — semua attempt per-pekerja (archived + current) dgn skor, pass/fail, tanggal, detail per-soal benar/salah. | 406 |
| **RTK-09** | Endpoint worker | `CMP/RetakeExam(id)` — `[ValidateAntiForgeryToken]`, ownership (`session.UserId==user.Id`), re-cek `CanRetakeAsync` server-side, panggil `ExecuteAsync` → clear `TempData[TokenVerified_{id}]` → redirect StartExam. | 407 |
| **RTK-10** | UI Hasil self-service | `Results.cshtml`: tombol "Ujian Ulang" saat eligible + "Percobaan ke-X dari N" + cooldown countdown (disable bila belum lewat) + "Batas percobaan tercapai, hubungi HC" saat habis. | 407 |
| **RTK-11** | Gating review + tier feedback | Saat gagal+attempt-sisa: tampilkan skor + tanda soal benar/salah (✓/✗) TANPA kunci (tier baru `showWrongFlagsOnly`); saat lulus ATAU attempt habis: `AllowAnswerReview` normal. | 407 |
| **RTK-12** | Riwayat percobaan pekerja | View riwayat di `Results.cshtml`/`Records.cshtml` (daftar attempt + drill-down per-soal dari archive, tunduk gating); flag `IsCurrentAttempt` di `AllWorkersHistoryRow`. | 407 |
| **RTK-13** | Guards komprehensif | Exclude: PreTest (D6), IsManualEntry, PendingGrading (IsPassed null), Cancelled/Abandoned (Status!=Completed). HC reset escape-hatch tetap (bypassGuards). | 405+407 |
| **RTK-14** | Test & UAT | xUnit (RetakeRules + RetakeArchiveBuilder + RetakeService) + integration (retake-then-pass 1 cert; counting (UserId,Title,Category) no-conflate Pre/Post) + Playwright lifecycle penuh @5270 + security (RBAC, antiforgery, server-side cooldown/cap revalidation, no answer-key leak). | 408 |

**Total: 14 REQ, 0 orphan.** Fase→REQ: 405 {01,02,03,04,06,07,13} · 406 {05,08} · 407 {09,10,11,12,13} · 408 {14}.

## Traceability

| Fase | Requirements | Status |
|------|--------------|--------|
| 405 Backend Core | RTK-01, RTK-02, RTK-03, RTK-04, RTK-06, RTK-07, RTK-13 | ✅ COMPLETE (4/4 plan; 405-01/02/03 + 405-04: RTK-01/02/03/04/06/07/13 ✓) |
| 406 Admin Config UI + Riwayat HC | RTK-05, RTK-08 | ✅ COMPLETE (3/3 plan: 406-01 riwayat BACKEND + 406-02 retake config UI + 406-03 riwayat HC modal mount — **RTK-05 ✓** card `ManagePackages` + binding `CreateAssessment`/`EditAssessment` + e2e 6/6 @5270; **RTK-08 ✓** drill-down modal di `AssessmentMonitoringDetail` (dropdown trigger + shared modal + lazy-fetch _RiwayatPercobaan accordion+per-soal tri-state) + e2e 5/5 @5270; migration=FALSE) |
| 407 Worker Self-Service | RTK-09, RTK-10, RTK-11, RTK-12, RTK-13 | ✅ COMPLETE (3/3 plan; 407-01 fondasi pure + 407-02 controller + 407-03 worker UI. **RTK-09 ✓** POST `CMP/RetakeExam` antiforgery+ownership+CanRetakeAsync + form modal `@Html.AntiForgeryToken()`; **RTK-10 ✓** Results.cshtml retake control tombol "Ujian Ulang"/counter/cooldown-countdown disabled→enable/IsCapReached lock "Batas percobaan tercapai" + modal konfirmasi; **RTK-11 ✓** `@switch(RetakeMode)` 3-state LEAK-SAFE — ShowWrongFlagsOnly render verdict ✓/✗ + "Jawaban Anda" TANPA kunci (`list-group-item-success`/"(Jawaban Benar)"/`CorrectAnswer`), ShowFullReview lulus/exhausted, ShowScoreOnly notice; **RTK-12 ✓** partial `_RiwayatPekerja.cshtml` ter-gate (Tidak Lulus/Jawaban Saya/ViewData[HideDetail] ScoreOnly) dirender di Results, Records.cshtml andalkan "Lihat Hasil" (D-04); **RTK-13 ✓** server-authoritative guards. build 0 err; unit 448/0/2; Playwright `retake-worker-407.spec.ts` 6 skenario leak-safety + seed 3 sesi (live UAT @5270 = orchestrator gate); migration=FALSE. Commits `b57fdc6b`+`810ffb60`+`0bd3c1ac`.) |
| 408 Test & UAT | RTK-14 | 🚧 IN PROGRESS (1/2 plan; 408-01 ✅ xUnit integration GAP-1 `RetakeThenPassCertTests` — retake→grade-lulus→**TEPAT 1 cert** [anti-double-cert guard + unique index] + format `KPB/\d{3}/[IVX]+/\d{4}`, GREEN @SQLEXPRESS, REUSE RetakeServiceFixture, 0 test hijau tersentuh, commit `39e3ef46`; counting (UserId,Title,Category) no-conflate Pre/Post = `RetakeServiceTests.Counting_PrePostSameTitle_NoConflate` existing. **SISA 408-02**: Playwright lifecycle penuh @5270 + security gate `gsd-secure-phase 408` [RBAC/antiforgery/cooldown-cap revalidation/no-leak]. migration=FALSE) |

100% coverage (14/14 REQ mapped, 0 orphan).

## Invariant / Risk (dijaga selama implementasi)

- **Cert tetap 1** — guard anti-double-cert existing (unique index + `WHERE NomorSertifikat==null` + retry 3×) menangani retake-lalu-lulus. Retake ≠ renewal (no `RenewsSessionId`).
- **Counting** pakai `(UserId, Title, Category)` (bukan Title saja) — anti-konflasi Pre/Post yang ber-Title sama.
- **Cooldown** `DateTime.UtcNow` konsisten; sumber = `session.CompletedAt` sesi gagal (sebelum reset).
- **Token** wajib di-clear (`TempData[TokenVerified_{id}]`) saat retake — `StartExam` pakai `TempData.Peek` non-consume.
- **Atomic** claim-transisi-dulu di `RetakeService` — anti double-archive dari double-click.

## Out of Scope (YAGNI)

Grading method selain "attempt terakhir" (highest/average — D2 latest), cooldown escalating (ISC2-style), default MaxAttempts per-kategori (D4 per-assessment saja), pre-retake remediation/reflection gate, rotasi AccessToken per-attempt, cap attempt per-tahun.
