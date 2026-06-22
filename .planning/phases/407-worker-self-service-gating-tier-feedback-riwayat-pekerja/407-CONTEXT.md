# Phase 407: Worker Self-Service + Gating Tier Feedback + Riwayat Pekerja - Context

**Gathered:** 2026-06-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Sisi PEKERJA dari ujian ulang (backend 405 done, admin UI 406 done, 0 migration). Pekerja:
1. Memicu ujian ulang sendiri dari halaman Hasil — endpoint `CMP/RetakeExam(id)` ber-guard server-side (antiforgery + ownership + re-cek `CanRetakeAsync` + `RetakeService.ExecuteAsync` + clear token → redirect `StartExam`). *(RTK-09, RTK-13)*
2. UI di `Results.cshtml`: tombol "Ujian Ulang" saat eligible, "Percobaan ke-X dari N", cooldown countdown, lock message saat cap habis. *(RTK-10)*
3. Feedback bertingkat tier `showWrongFlagsOnly` — gagal + attempt-sisa → skor + tanda ✓/✗ per-soal TANPA kunci/pembahasan. *(RTK-11)*
4. Riwayat percobaan pekerja sendiri di `Results.cshtml`/`Records.cshtml` (daftar attempt + drill-down per-soal dari archive, TUNDUK gating) + flag `IsCurrentAttempt` di `AllWorkersHistoryRow`. *(RTK-12)*

**File cluster (disjoint dari 406):** `Controllers/CMPController.cs`, `Views/CMP/Results.cshtml`, `Views/CMP/Records.cshtml`, `Models/AllWorkersHistoryRow.cs`, `Models/AssessmentResultsViewModel.cs`. **BUKAN** exam-taking flow (itu Phase 408 lifecycle test).

**Out of scope:** schema/migration (0), admin config (406), perubahan engine grading.
</domain>

<decisions>
## Implementation Decisions

### Locked dari spec + Phase 405 (carry-forward — TIDAK di-discuss ulang)
Spec `docs/superpowers/specs/2026-06-19-attempt-retake-assessment-design.md` AUTHORITATIVE. Dari 405 sudah terkunci & TERSEDIA:
- `RetakeRules.CanRetake(...)` pure (cooldown `<=0`=no jeda; `attemptsUsed < maxAttempts`); `RetakeService.ExecuteAsync(sessionId, initiatedBy, bypassGuards)` → `(bool success, string? error)`; `RetakeService.CanRetakeAsync(...)` (era-retake counting via snapshot-presence, legacy HC-reset natural-excluded — D-01); `RetakeArchiveBuilder.Build(...)`; tabel `AssessmentAttemptResponseArchive`.
- **Must-fix #1:** clear `TempData[TokenVerified_{id}]` saat retake (re-entry minta token ulang). **#7:** tier `showWrongFlagsOnly` flag disediakan `RetakeRules` (407 yang wire ke UI).
- **D-02 retroaktif:** sesi yang sudah gagal sebelum `AllowRetake` di-ON-kan langsung eligible (tunduk cooldown+cap).
- Counting `(UserId, Title, Category)` anti-konflasi Pre/Post; exclude `IsManualEntry` + PendingGrading (IsPassed null); graded-only (`AssessmentType != "PreTest"`); cap habis → lock + "hubungi HC".

### Gray areas di-discuss (2026-06-22)

- **D-01 (Cooldown UX):** **Live countdown + tombol disabled.** Tombol "Ujian Ulang" disabled selama cooldown belum lewat, dengan teks ticking JS (mis. "Bisa ulang dalam 23:14:05") yang menghitung mundur dari `CompletedAt + RetakeCooldownHours`. Mirror pola timer ujian existing. Saat habis → tombol auto-enabled (atau aktif saat reload). Server TETAP otoritatif (re-cek `CanRetakeAsync` saat POST — countdown hanya UX, bukan gate).

- **D-02 (Konfirmasi retake):** **Modal konfirmasi WAJIB sebelum POST.** Aksi destruktif (arsip percobaan saat ini + reset mulai dari awal). Modal: "Percobaan saat ini akan diarsipkan & kamu mulai dari awal. Lanjut?" → konfirmasi baru POST `RetakeExam`. Cegah klik tak sengaja.

- **D-03 (Tier feedback — BERGANTUNG `AllowAnswerReview` HC):** ⚠️ **Refinement penting RTK-11.** Tier feedback HORMATI setting `AssessmentSession.AllowAnswerReview` yang di-set HC:
  - `AllowAnswerReview == true` **DAN** (gagal + attempt-sisa) → tier **`showWrongFlagsOnly`**: skor total + tanda ✓/✗ per-soal + **jawaban yang DIA pilih sendiri**, TANPA kunci/pembahasan (correct option / explanation disembunyikan).
  - `AllowAnswerReview == true` **DAN** (lulus ATAU attempt habis) → **`showFullReview`** (review penuh existing: skor + ✓/✗ + jawaban sendiri + kunci/pembahasan).
  - `AllowAnswerReview == false` → **skor saja**, tanpa rincian per-soal sama sekali (apa pun status retake) — HC sengaja tidak izinkan review.
  - Prinsip leak-safe: kunci jawaban TIDAK PERNAH tampil selama masih ada percobaan tersisa untuk assessment group itu.

- **D-04 (Riwayat pekerja gating):** **Reuse POLA HC (RiwayatUnifier + accordion/modal), TAPI drill-down per-soal TUNDUK gating yang sama dengan D-03.** Worker riwayat NYALAKAN ulang RiwayatUnifier (archived + current), tampilkan daftar attempt (skor/lulus-gagal/tanggal, `IsCurrentAttempt` ter-flag). Drill-down per-soal per attempt mengikuti tier per assessment group: selama retake masih mungkin → ✓/✗ tanpa kunci (tunduk `AllowAnswerReview`); lulus/habis → full review (bila `AllowAnswerReview`). **JANGAN** pakai partial HC `_RiwayatPercobaan` apa adanya (itu full-leak untuk Admin/HC) — butuh varian ter-gate untuk pekerja.

### Claude's Discretion
- Komputasi tier konkret (helper pure di `RetakeRules` atau service) — planner pilih; harus unit-testable + deterministik.
- Bentuk partial worker-riwayat (modal vs inline accordion di Results/Records) — planner pilih; WAJIB reuse `RiwayatUnifier` + hormati gating D-03/D-04.
- Cara redirect re-entry pasca-ExecuteAsync (`StartExam(id)` per spec) + UX flash pesan.
- Format teks countdown + threshold auto-enable (poll vs reload).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & milestone
- `docs/superpowers/specs/2026-06-19-attempt-retake-assessment-design.md` — AUTHORITATIVE; §7 gating tier (showWrongFlagsOnly), self-service flow, lock policy.
- `.planning/phases/405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint/405-CONTEXT.md` — 10 keputusan D1–D10 + 7 must-fix + signature service/rules.
- `.planning/phases/406-admin-config-ui-riwayat-hc/406-CONTEXT.md` + `406-VERIFICATION.md` — pola RiwayatUnifier + `_RiwayatPercobaan` partial HC (referensi pola, BUKAN reuse langsung untuk worker).

### Kode integrasi (read sebelum implement)
- `Helpers/RetakeRules.cs` — `CanRetake` + `ShouldHideRetakeToggle` (tier flag di sini).
- `Services/RetakeService.cs` — `ExecuteAsync` + `CanRetakeAsync` (re-cek server-side).
- `Helpers/RiwayatUnifier.cs` + `Models/RiwayatAttemptViewModel.cs` — unifier archived+current (reuse untuk riwayat pekerja).
- `Controllers/CMPController.cs:2243-2380` — build `AssessmentResultsViewModel` (titik inject tier gating).
- `Views/CMP/Results.cshtml:316` (review block `AllowAnswerReview`) + `:413` (else branch) — titik wire tombol retake + tier feedback.
- `Models/AllWorkersHistoryRow.cs` — tambah `IsCurrentAttempt`.
- `Helpers/AssessmentScoreAggregator.cs` (`IsQuestionCorrect`) — verdict ✓/✗ konsisten.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RiwayatUnifier.Build(...)` (pure) + `RiwayatAttemptViewModel` — pakai ulang untuk riwayat pekerja; gating ditambah di layer view/VM, bukan di unifier.
- `RetakeService.CanRetakeAsync` + `ExecuteAsync` — endpoint `RetakeExam` tinggal panggil (jangan duplikasi logika eligibility).
- `AssessmentScoreAggregator.IsQuestionCorrect` — verdict per-soal ✓/✗ tier feedback.
- Pola endpoint sibling/PRG + antiforgery dari `UpdateRetakeSettings` (406) — pola guard untuk `RetakeExam`.

### Established Patterns
- Gating review existing = boolean `AllowAnswerReview` (`Results.cshtml:316` vs `:413`). Tier 407 = **3 keadaan** (full / wrong-flags-only / score-only) yang menggantikan boolean tunggal — JANGAN hapus AllowAnswerReview, perluas jadi enum/mode di VM.
- Ownership guard: `session.UserId == effectiveUser.Id` (impersonation-aware via `ImpersonationService`/effective user) — WAJIB di `RetakeExam`.

### Integration Points
- `CMPController` action `Results`/`Records` (build VM) + action baru `RetakeExam` (POST).
- `Results.cshtml` + `Records.cshtml` (UI tombol + riwayat).
- `StartExam(id)` (target redirect re-entry; TempData token sudah di-clear).
</code_context>

<specifics>
## Specific Ideas

- Countdown teks gaya "Bisa ulang dalam HH:MM:SS" (ticking), tombol disabled → enabled saat 0.
- Modal konfirmasi destruktif sebelum POST retake.
- Tier feedback adalah fungsi dari `(AllowAnswerReview, sudahLulus?, attemptSisa?)` — tabel kebenaran di D-03.
</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope. (Test lifecycle penuh + security audit = Phase 408 RTK-14.)
</deferred>

---

*Phase: 407-worker-self-service-gating-tier-feedback-riwayat-pekerja*
*Context gathered: 2026-06-22*
