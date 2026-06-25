# Phase 407: Worker Self-Service + Gating Tier Feedback + Riwayat Pekerja - Research

**Researched:** 2026-06-22
**Domain:** ASP.NET Core 8 MVC (Razor + Bootstrap 5.3) — worker-facing wiring of an existing retake engine; security-critical answer-key leak prevention
**Confidence:** HIGH (semua klaim diverifikasi langsung terhadap kode 405/406 yang sudah ter-ship di repo)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
Spec `docs/superpowers/specs/2026-06-19-attempt-retake-assessment-design.md` AUTHORITATIVE. Carry-forward dari 405 (TIDAK di-discuss ulang):
- `RetakeRules.CanRetake(...)` pure; `RetakeService.ExecuteAsync(sessionId, actorUserId, actorName, actionType, reason)` → `RetakeResult(bool Success, string? Error)`; `RetakeService.CanRetakeAsync(sessionId)` (era-retake counting via snapshot-presence; legacy HC-reset natural-excluded — D-01); `RetakeArchiveBuilder.Build(...)`; tabel `AssessmentAttemptResponseArchive`.
- **Must-fix #1:** clear `TempData[TokenVerified_{id}]` saat retake (re-entry minta token ulang).
- **Must-fix #7:** tier `showWrongFlagsOnly` flag disediakan `RetakeRules` (407 yang wire ke UI).
- **D-02 retroaktif:** sesi yang sudah gagal sebelum `AllowRetake` di-ON-kan langsung eligible (tunduk cooldown+cap).
- Counting `(UserId, Title, Category)` anti-konflasi Pre/Post; exclude `IsManualEntry` + PendingGrading (IsPassed null); graded-only (`AssessmentType != "PreTest"`); cap habis → lock + "hubungi HC".

**Gray areas di-discuss (2026-06-22):**
- **D-01 (Cooldown UX):** Live countdown JS + tombol disabled selama cooldown belum lewat (teks ticking "Bisa ulang dalam HH:MM:SS" dari `CompletedAt + RetakeCooldownHours`). Saat habis → tombol auto-enabled (atau aktif saat reload). Server TETAP otoritatif (re-cek `CanRetakeAsync` saat POST — countdown hanya UX).
- **D-02 (Konfirmasi retake):** Modal konfirmasi WAJIB sebelum POST (aksi destruktif: arsip + reset).
- **D-03 (Tier feedback — BERGANTUNG `AllowAnswerReview` HC):** truth table tier (lihat §Architecture Patterns Pattern 3). Prinsip leak-safe: kunci jawaban TIDAK PERNAH tampil selama masih ada percobaan tersisa.
- **D-04 (Riwayat pekerja gating):** Reuse POLA HC (RiwayatUnifier + accordion), TAPI drill-down per-soal TUNDUK gating yang sama dengan D-03. JANGAN pakai partial HC `_RiwayatPercobaan` apa adanya untuk worker — butuh varian ter-gate (lihat §Common Pitfalls Pitfall 1 untuk koreksi nuansa).

### Claude's Discretion
- Komputasi tier konkret (helper pure di `RetakeRules` atau service) — planner pilih; WAJIB unit-testable + deterministik.
- Bentuk partial worker-riwayat (modal vs inline accordion di Results/Records) — planner pilih; WAJIB reuse `RiwayatUnifier` + hormati gating D-03/D-04.
- Cara redirect re-entry pasca-ExecuteAsync (`StartExam(id)` per spec) + UX flash pesan.
- Format teks countdown + threshold auto-enable (poll vs reload).
- Records.cshtml: tambah per-row "Riwayat Percobaan" trigger ATAU andalkan "Lihat Hasil" → Results (planner pilih).

### Deferred Ideas (OUT OF SCOPE)
None — diskusi tetap di dalam scope fase. Test lifecycle penuh + security audit = **Phase 408 RTK-14** (Playwright lifecycle @5270 + RBAC/antiforgery/cooldown-cap-revalidation/no-leak audit). Out of scope umum: schema/migration (0), admin config (406), perubahan engine grading.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **RTK-09** | Endpoint worker `CMP/RetakeExam(id)` — `[ValidateAntiForgeryToken]`, ownership (`session.UserId==user.Id`), re-cek `CanRetakeAsync` server-side → `ExecuteAsync` → clear `TempData[TokenVerified_{id}]` → redirect StartExam | Pola lengkap ter-mirror dari HC `ResetAssessment` (AssessmentAdminController.cs:4244-4327). `RetakeService.ExecuteAsync` + `CanRetakeAsync` siap-pakai (Services/RetakeService.cs). Ownership via `GetCurrentUserRoleLevelAsync` (effective user, impersonation-aware). Lihat §Architecture Patterns Pattern 1 + §Code Examples. |
| **RTK-10** | UI Hasil self-service: tombol "Ujian Ulang" + "Percobaan ke-X dari N" + cooldown countdown (disable bila belum lewat) + lock "hubungi HC" | Markup preskriptif sudah di 407-UI-SPEC §Components 1. Mirror exam-timer JS (StartExam.cshtml:460-490) untuk countdown. VM perlu field baru (CurrentAttempt/MaxAttempts/CanRetake/CooldownUntilUtc/IsCapReached). Lihat §Architecture Patterns Pattern 2. |
| **RTK-11** | Gating review + tier feedback: gagal+attempt-sisa → skor + ✓/✗ TANPA kunci (`showWrongFlagsOnly`); lulus/habis → `AllowAnswerReview` normal | Tier = fungsi `(AllowAnswerReview, isPassed, attemptsRemaining)`. Existing review block Results.cshtml:316-418 (boolean) jadi 3-state. Helper pure baru di `RetakeRules` (rekomendasi). Leak sites: option list :355-394, essay key :403. Lihat §Architecture Patterns Pattern 3 + §Don't Hand-Roll. |
| **RTK-12** | Riwayat percobaan pekerja: daftar attempt + drill-down per-soal (tunduk gating) di Results/Records; flag `IsCurrentAttempt` di `AllWorkersHistoryRow` | `RiwayatUnifier.Build(...)` + `RiwayatAttemptViewModel` siap reuse. Pola data-load persis HC `RiwayatPercobaan` action (AssessmentAdminController.cs:3485-3524). `AllWorkersHistoryRow` (Models/) ditambah `IsCurrentAttempt`. Lihat §Architecture Patterns Pattern 4. |
| **RTK-13** | Guards komprehensif (shared 405+407): exclude PreTest/IsManualEntry/PendingGrading/Cancelled/Abandoned | Sudah dijamin `RetakeRules.CanRetake` (Helpers/RetakeRules.cs:41-51) yang dibungkus `CanRetakeAsync`. Endpoint 407 cukup memanggil `CanRetakeAsync` (jangan duplikasi guard). |
</phase_requirements>

## Summary

Phase 407 adalah **pure wiring fase** (UI + read-path + satu POST endpoint, 0 migration) di atas mesin retake yang sudah 100% ter-ship di Phase 405 (`RetakeRules`, `RetakeService.ExecuteAsync`/`CanRetakeAsync`, `RetakeArchiveBuilder`, `RiwayatUnifier`, tabel arsip) dan pola UI/data yang sudah terbukti di Phase 406 (HC `RiwayatPercobaan` action + `_RiwayatPercobaan` partial + `UpdateRetakeSettings` endpoint guard). **Nilai utama fase ini = REUSE, bukan membangun.** Tiga komponen baru murni: (1) endpoint `CMP/RetakeExam` yang men-cermin HC `ResetAssessment` baris-per-baris (ganti actor & redirect target), (2) tier feedback 3-state yang mengganti boolean `AllowAnswerReview` di view, (3) varian riwayat pekerja ter-gate.

407 adalah **PERTAMA KALI seorang pekerja bisa men-trigger retake sendiri** — sebelumnya hanya HC. Itu menjadikan dua hal security-critical: (a) endpoint WAJIB punya ownership guard + antiforgery + re-cek `CanRetakeAsync` server-side (countdown JS hanya UX, bukan gate), dan (b) **answer-key leak prevention** — selama satu percobaan masih tersisa, kunci jawaban (`list-group-item-success` highlight, label "(Jawaban Benar)", `CorrectAnswer`/rubrik essay) TIDAK BOLEH render. Tier feedback dihitung server-side dan view hanya mem-percaya VM.

Satu temuan penting (koreksi nuansa CONTEXT/UI-SPEC): partial HC `_RiwayatPercobaan.cshtml` **sebenarnya SUDAH tidak membocorkan kunci jawaban** — ia hanya menampilkan `AnswerText` (jawaban worker) + verdict ✓/✗, tanpa option-list/kunci. Jadi "full-leak" yang dikhawatirkan CONTEXT tidak akurat untuk partial itu sendiri; namun keputusan untuk membuat **varian terpisah** tetap benar karena (i) header badge HC pakai "Gagal", worker harus "Tidak Lulus", dan (ii) gating per-attempt (kunci muncul saat lulus/habis) belum ada di partial HC — partial HC SELALU verdict-only. Detail di §Common Pitfalls Pitfall 1.

**Primary recommendation:** Mirror HC `ResetAssessment` untuk endpoint, ekstrak satu helper pure `RetakeRules.ResolveReviewMode(...)` (3-state enum, unit-testable) untuk tier, dan reuse `RiwayatUnifier` + partial baru ter-gate untuk riwayat. Hitung SEMUA flag retake/tier di `CMPController.Results` ke dalam `AssessmentResultsViewModel`; view hanya render.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Eligibility decision (canRetake, cooldown, cap) | API/Backend (`RetakeService.CanRetakeAsync` + `RetakeRules.CanRetake`) | — | Server otoritatif (D-01). Countdown JS hanya UX, tak boleh jadi gate. Counting era-retake DB-aware. |
| Retake mutation (archive→delete→reset) | API/Backend (`RetakeService.ExecuteAsync`) | DB/Storage | Transaksi atomik claim-first sudah ada di 405; 407 hanya memanggil. |
| Tier resolution (showFullReview/showWrongFlagsOnly/showScoreOnly) | API/Backend (helper pure + `CMPController.Results`) | — | Leak-safety = keputusan server. Tier dihitung di controller, di-inject ke VM; view tak menghitung. |
| Cooldown countdown display | Browser/Client (inline `<script>` setInterval) | — | Murni presentasi; baca `data-cooldown-until` ISO-8601 UTC, tick mundur. Tak ada gate-power. |
| Confirmation modal (anti-accidental) | Browser/Client (Bootstrap modal) | API/Backend (antiforgery re-check) | Modal = click-safety; submit aktual = POST ber-antiforgery + server re-validate. |
| Riwayat unify (archived + current) | API/Backend (`RiwayatUnifier.Build` pure) | DB/Storage | Helper pure EF-free; controller menyuplai fakta dari DbContext. |
| Riwayat render + per-attempt gating | Frontend Server (Razor partial) | — | Gating per-attempt diterapkan di layer view/VM, BUKAN di unifier (CONTEXT D-04). |
| TempData token clear | API/Backend (`CMPController.RetakeExam`) | — | HTTP-scoped; service TIDAK boleh sentuh TempData (must-fix #1; service doc :38-39 eksplisit menyerahkan ini ke caller). |

## Standard Stack

Brownfield — stack sudah terkunci, TIDAK ada library baru untuk fase ini. Daftar di bawah = yang dipakai/disentuh.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller + Razor views + PRG pattern | Framework aplikasi [VERIFIED: HcPortal.Tests.csproj `<TargetFramework>net8.0`] |
| EF Core | 8.0.0 | Read-path (riwayat load); 407 TIDAK menulis schema | [VERIFIED: csproj `Microsoft.EntityFrameworkCore.SqlServer` 8.0.0] |
| Bootstrap | 5.3.0 | card/modal/accordion/alert/badge/btn | [CITED: 407-UI-SPEC.md Design System table; `_Layout.cshtml:38,246`] |
| Bootstrap Icons | 1.10.0 | `bi bi-*` glyph (arrow-repeat, hourglass-split, lock-fill, eye-slash, check/x-circle-fill, clock-history) | [CITED: 407-UI-SPEC.md; `_Layout.cshtml:39`] |

### Supporting (test)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit | 2.9.3 | Unit tests (RetakeRules tier helper) + integration (real-SQL fixture) | Tier helper unit test + integration retake-then-pass [VERIFIED: HcPortal.Tests.csproj] |
| EF Core InMemory | 8.0.0 | (tersedia, tapi integration retake pakai real-SQL fixture) | Mirror `RetakeServiceFixture` (real SQLEXPRESS disposable DB) untuk integration |
| Playwright | (project terpisah, @5270) | E2E lifecycle UAT | **Phase 408 (RTK-14), BUKAN 407** — tetapi 407 plan boleh tambah Wave-0 e2e smoke leak-safety |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Helper pure `RetakeRules.ResolveReviewMode` | Inline tier `if/else` di controller | Inline tidak unit-testable + risiko drift dgn `CanRetake`. Helper pure = deterministik + ter-uji (mirror pola `CanRetake`/`ShouldHideRetakeToggle`). **Rekomendasi: helper pure.** |
| Partial baru `_RiwayatPekerja` ter-gate | Tambah param `bool showKey` ke `_RiwayatPercobaan` HC | HC partial saat ini verdict-only (TIDAK render kunci) — menambah `showKey` justru menambah surface leak baru ke partial HC. Partial worker terpisah lebih aman & jelas. **Rekomendasi: partial terpisah.** |
| Inline accordion di Results | Modal AJAX-loaded | Inline default per UI-SPEC; AJAX lazy-load (mirror HC lazy-fetch) lebih hemat payload bila banyak attempt. Planner pilih (D-04). |

**Installation:** Tidak ada — semua dependency sudah terpasang. `RetakeService` sudah ter-DI [VERIFIED: Program.cs:63 `AddScoped<HcPortal.Services.RetakeService>()`]. `CMPController` perlu inject `RetakeService` di constructor (belum ter-inject — saat ini hanya di Admin controllers) [VERIFIED: grep `_retakeService` hanya di AssessmentAdminController/TrainingAdminController].

**Version verification:** Versi adalah pin brownfield existing (net8.0, EF 8.0.0, xUnit 2.9.3) — bukan dependency baru, jadi tidak perlu `npm view`/`dotnet add`. Verifikasi via csproj langsung [VERIFIED: HcPortal.Tests.csproj dibaca].

## Architecture Patterns

### System Architecture Diagram

```
WORKER (browser, halaman Results)
   │
   │ (A) GET /CMP/Results/{id}
   ▼
CMPController.Results ──► authz (IsResultsAuthorized owner||L<=3||L4-section)
   │                       │
   │   build AssessmentResultsViewModel:
   │     • existing review data (QuestionReviews)
   │     • NEW: CanRetakeAsync(id) ───────────────► RetakeService.CanRetakeAsync
   │     • NEW: ResolveReviewMode(AllowAnswerReview, isPassed, attemptsRemaining)  [pure helper]
   │     • NEW: CurrentAttempt/MaxAttempts/CooldownUntilUtc/IsCapReached
   │     • NEW: RiwayatAttempts (RiwayatUnifier.Build) [load histories+archives+currentRows]
   ▼
Results.cshtml renders (TRUSTS VM — no eligibility/tier compute in view):
   ├─ Retake control block ── btn-primary "Ujian Ulang" | disabled+countdown | lock alert
   ├─ Tier feedback ── switch(RetakeMode): FullReview | WrongFlagsOnly | ScoreOnly
   ├─ Riwayat card ── accordion per-attempt, drill-down GATED per attempt-group
   └─ Confirmation modal ── POST form (antiforgery) → CMP/RetakeExam
   │
   │ (B) worker clicks "Ujian Ulang" → modal confirm → submit
   ▼
POST /CMP/RetakeExam/{id}  [ValidateAntiForgeryToken]
   │
   ├─ load session; ownership: session.UserId == effectiveUser.Id  (else Forbid)
   ├─ re-check CanRetakeAsync(id) server-side  (else TempData[Error] + redirect Results)
   ├─ RetakeService.ExecuteAsync(id, worker, name, "RetakeAssessment", "worker_retake")
   │        └─► claim-atomik → snapshot+archive → delete live → audit → SignalR sessionReset
   ├─ TempData.Remove($"TokenVerified_{id}")     [must-fix #1]
   └─ RedirectToAction("StartExam", new { id })  → worker re-enters lobby (token re-required)
```

**Trace use case (retake gagal→ulang):** Worker buka Results (A) → lihat skor + ✓/✗ tanpa kunci (tier WrongFlagsOnly) + tombol "Ujian Ulang" enabled (cooldown lewat) → klik → modal → konfirmasi → POST (B) → server re-validate eligible → ExecuteAsync arsipkan percobaan + reset sesi → token di-clear → redirect StartExam → worker masukkan token → ujian dari awal.

### Recommended Project Structure (file yang disentuh — disjoint dari 406)
```
Controllers/CMPController.cs        # + action RetakeExam (POST); extend Results (build VM flags + riwayat)
Models/AssessmentResultsViewModel.cs # + RetakeMode enum, CanRetake, CurrentAttempt, MaxAttempts, CooldownUntilUtc, IsCapReached, RiwayatAttempts
Models/AllWorkersHistoryRow.cs      # + IsCurrentAttempt (bool)
Helpers/RetakeRules.cs              # + ResolveReviewMode(...) pure (REKOMENDASI) + RetakeReviewMode enum
Views/CMP/Results.cshtml           # retake control + modal + tier 3-state + riwayat card
Views/CMP/Records.cshtml           # (opsional) per-row "Riwayat Percobaan" trigger / andalkan "Lihat Hasil"
Views/CMP/_RiwayatPekerja.cshtml   # (BARU) partial riwayat ter-gate (varian dari _RiwayatPercobaan HC)
HcPortal.Tests/RetakeRulesTests.cs # + tes ResolveReviewMode semua cabang
```

### Pattern 1: Worker self-service endpoint (mirror HC ResetAssessment)
**What:** POST `CMP/RetakeExam(id)` = cermin baris-per-baris `AssessmentAdminController.ResetAssessment` (:4244-4327), beda: actor=worker, guard=ownership+`CanRetakeAsync` (bukan IsResettable/Pre-Post HC), redirect=`StartExam`.
**When to use:** Satu-satunya jalur tulis di fase ini.
**Example (skeleton):**
```csharp
// Source: cermin AssessmentAdminController.cs:4244-4327 (ResetAssessment) + Services/RetakeService.cs:69,232
[HttpPost]
[ValidateAntiForgeryToken]                 // RTK-09 (CMPController class-level [Authorize] sudah ada :25)
public async Task<IActionResult> RetakeExam(int id)
{
    var assessment = await _context.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == id);
    if (assessment == null) return NotFound();

    var (user, _) = await GetCurrentUserRoleLevelAsync();      // effective user (impersonation-aware)
    if (user == null) return Challenge();
    if (assessment.UserId != user.Id) return Forbid();        // RTK-09 ownership (worker self-service only)

    if (!await _retakeService.CanRetakeAsync(id))             // server-authoritative re-check (D-01)
    {
        TempData["Error"] = "Ujian ulang tidak bisa dijalankan saat ini. Coba muat ulang halaman atau hubungi HC.";
        return RedirectToAction("Results", new { id });
    }

    var actorName = string.IsNullOrWhiteSpace(user.NIP) ? (user.FullName ?? "Unknown") : $"{user.NIP} - {user.FullName}";
    var rs = await _retakeService.ExecuteAsync(id, user.Id, actorName, "RetakeAssessment", "worker_retake");
    if (!rs.Success) { TempData["Error"] = rs.Error ?? "Gagal."; return RedirectToAction("Results", new { id }); }

    TempData.Remove($"TokenVerified_{id}");                   // must-fix #1 (StartExam pakai Peek non-consume)
    return RedirectToAction("StartExam", new { id });         // spec re-entry target
}
```
**Catatan:** `CMPController` belum inject `RetakeService` — tambahkan ke constructor + field (mirror AssessmentAdminController.cs:32,47,58). Tidak perlu DI baru (sudah `AddScoped` di Program.cs:63).

### Pattern 2: VM-driven retake control + countdown
**What:** Controller hitung semua flag retake → VM; view render; countdown JS murni client.
**When to use:** RTK-10.
**Source flag:** `CooldownUntilUtc = AllowRetake && CompletedAt.HasValue && RetakeCooldownHours>0 ? CompletedAt.AddHours(RetakeCooldownHours) : null`. `CurrentAttempt = eraRetakeArchives + 1` (sama formula `CanRetakeAsync` :237-242). `MaxAttempts = assessment.MaxAttempts`. `IsCapReached = eraRetakeArchives + 1 >= MaxAttempts` (saat IsPassed==false). Markup di 407-UI-SPEC §Components 1.
**Countdown JS:** mirror exam-timer `setInterval(updateTimer,1000)` (StartExam.cshtml:489) + `clearInterval` saat `<=0` (:472-473). Baca `data-cooldown-until` (ISO-8601 `"o"` round-trip), hitung `new Date(attr) - Date.now()`.

### Pattern 3: Tier feedback 3-state (boolean → enum)
**What:** Ganti `if (AllowAnswerReview)` (Results.cshtml:316) jadi `switch (Model.RetakeMode)` 3 cabang.
**Truth table (D-03) — SUMBER kebenaran:**
| AllowAnswerReview | isPassed | attempts-left | RetakeMode |
|---|---|---|---|
| true | passed (true) | — | `ShowFullReview` |
| true | failed (false) | **ya** | `ShowWrongFlagsOnly` |
| true | failed (false) | tidak (habis) | `ShowFullReview` |
| true | pending (null) | — | `ShowFullReview` (pending bukan retake-eligible → tak ada percobaan tersisa untuk ditahan; verifikasi di plan) |
| false | apa pun | apa pun | `ShowScoreOnly` |
**Helper pure (rekomendasi):**
```csharp
// Source: pola mirror RetakeRules.CanRetake (Helpers/RetakeRules.cs)
public enum RetakeReviewMode { ShowFullReview, ShowWrongFlagsOnly, ShowScoreOnly }
public static RetakeReviewMode ResolveReviewMode(bool allowAnswerReview, bool? isPassed, bool attemptsRemaining)
{
    if (!allowAnswerReview) return RetakeReviewMode.ShowScoreOnly;
    if (isPassed == false && attemptsRemaining) return RetakeReviewMode.ShowWrongFlagsOnly;
    return RetakeReviewMode.ShowFullReview;
}
```
**Leak sites yang HARUS disuppress di `ShowWrongFlagsOnly`** (Results.cshtml):
- Option loop :355-394 — `list-group-item-success` (:366), `icon="bi-check-circle-fill"` (:367), label `(Jawaban Benar)` (:388). RENDER hanya jawaban worker + verdict ✓/✗.
- Essay branch :397-406 — `@question.CorrectAnswer` (:403, rubrik/kunci) JANGAN render; hanya `@question.UserAnswer`.
**View tetap render existing markup untuk `ShowFullReview` verbatim** (jangan hapus :318-411).

### Pattern 4: Riwayat pekerja ter-gate (reuse RiwayatUnifier)
**What:** Data-load = persis HC `RiwayatPercobaan` (AssessmentAdminController.cs:3485-3524). Render = partial baru ter-gate.
**Data-load (cermin):** histories by `(UserId,Title,Category)` desc by AttemptNumber → archiveRows by `histIds` → currentRows via `RetakeArchiveBuilder.Build(0,qs,resp)` hanya bila `Status=="Completed"` → `RiwayatUnifier.Build(session, histories, archiveRows, currentRows)`.
**Gating per-attempt (D-04):** `IsCurrent` attempt = sesi aktif → tier-nya = tier Results saat ini. Attempt archived = sudah lewat; secara umum saat masih ada percobaan tersisa untuk group itu → verdict-only (no key). Karena `AssessmentAttemptResponseArchive` **hanya menyimpan `AnswerText`+`IsCorrect`+`AwardedScore` (TIDAK ada option-list/kunci)** [VERIFIED: Models/AssessmentAttemptResponseArchive.cs:23-39], drill-down archived **secara struktural sudah verdict-only** — tidak bisa membocorkan kunci meski mau. Gating worker-riwayat karenanya berarti: (a) jangan tampilkan apa pun saat `ShowScoreOnly` (AllowAnswerReview==false), (b) "Tidak Lulus" bukan "Gagal" di header.
**IsCurrentAttempt (RTK-12):** flag di `AllWorkersHistoryRow` (Models/AllWorkersHistoryRow.cs) — set true untuk baris current Completed (mirror `RiwayatAttemptViewModel.IsCurrent`).

### Anti-Patterns to Avoid
- **Menghitung eligibility/tier di view (Razor):** view HARUS percaya VM. Compute di controller (leak-safety = server decision).
- **Memakai countdown JS sebagai gate:** `CMP/RetakeExam` WAJIB re-cek `CanRetakeAsync` (D-01). Attacker bisa enable tombol via DevTools.
- **Service menyentuh TempData:** `RetakeService` HTTP-agnostic (doc :38-39). Clear token di controller.
- **Reuse `_RiwayatPercobaan` HC verbatim untuk worker:** header pakai "Gagal" (worker butuh "Tidak Lulus") + tak ada gating `ShowScoreOnly`. Buat partial terpisah.
- **`Html.Raw` untuk konten user** (QuestionText/UserAnswer/AnswerText/option text): selalu Razor `@` auto-encode. Existing `Html.Raw` di Records :204 hanya untuk app-controlled URL/attr, bukan free-text.
- **Duplikasi guard eligibility:** jangan tulis ulang cooldown/cap di controller — panggil `CanRetakeAsync`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Eligibility (cooldown/cap/status/PreTest/Manual) | if-chain baru di CMPController | `RetakeService.CanRetakeAsync` (membungkus `RetakeRules.CanRetake`) | Counting era-retake DB-aware (snapshot-presence D-01) sudah benar + ter-uji; duplikasi = drift. [VERIFIED: Services/RetakeService.cs:232-248] |
| Retake mutation (archive→delete→reset, atomik) | inline copy ResetAssessment | `RetakeService.ExecuteAsync` | Claim-atomik anti double-archive + transaksi + WR-01/02/03 fixes sudah di service. [VERIFIED: RetakeService.cs:69-223] |
| Unify archived+current attempts | manual merge histories+session | `RiwayatUnifier.Build` (pure) | Group strict by AttemptHistoryId (Pitfall 3 anti salah-attach) + ordering desc. [VERIFIED: Helpers/RiwayatUnifier.cs:21-69] |
| Per-soal verdict ✓/✗ | recompute correctness di view | `AssessmentScoreAggregator.IsQuestionCorrect` (untuk current) + `AssessmentAttemptResponseArchive.IsCorrect` (untuk archived, beku) | Kill-drift; essay-aware tri-state. [VERIFIED: Helpers/AssessmentScoreAggregator.cs:73-98] |
| Snapshot current attempt untuk riwayat | query manual | `RetakeArchiveBuilder.Build(0, qs, resp)` (sentinel id=0) | Pola persis HC RiwayatPercobaan :3517. |
| Tier resolution | inline if/else | helper pure `ResolveReviewMode` (baru) | Unit-testable + deterministik (mirror CanRetake purity). |

**Key insight:** Hampir SEMUA logika 407 sudah ada. Risiko terbesar bukan "bagaimana membangun" tapi "bagaimana TIDAK membocorkan kunci & TIDAK men-duplikasi guard." Fase ini = perakitan + suppress.

## Common Pitfalls

### Pitfall 1: Asumsi `_RiwayatPercobaan` HC adalah full-leak (KOREKSI nuansa CONTEXT/UI-SPEC)
**What goes wrong:** CONTEXT D-04 + UI-SPEC menyebut partial HC "full-leak untuk Admin/HC". Faktanya partial itu **hanya** render `QuestionText` + `AnswerText` (jawaban peserta) + verdict ✓/✗ + skor — **TANPA option-list/kunci jawaban** [VERIFIED: Views/Admin/_RiwayatPercobaan.cshtml:48-97, tidak ada loop Options/IsCorrect-option].
**Why it happens:** Misread; "Jawaban Peserta" ≠ "kunci jawaban".
**How to avoid:** Tetap buat partial worker terpisah, tapi alasannya: (1) header HC pakai badge "Gagal" (:33) — worker WAJIB "Tidak Lulus" (konsisten Results :82 / Records :222); (2) gating `ShowScoreOnly` (saat AllowAnswerReview==false) belum ada di partial HC; (3) "Jawaban Peserta" → "Jawaban Saya". Struktur arsip yang verdict-only justru MEMUDAHKAN leak-safety (tak ada kunci untuk dibocorkan di drill-down archived).
**Warning signs:** Plan menulis "tambah suppress kunci di drill-down archived" — tak perlu; arsip tak punya kunci.

### Pitfall 2: Countdown JS dianggap sebagai gate
**What goes wrong:** Tombol disabled by JS, tapi POST tetap bisa di-fire (form submit / DevTools enable).
**Why it happens:** Lupa server re-validate.
**How to avoid:** `RetakeExam` WAJIB `await CanRetakeAsync(id)` sebelum `ExecuteAsync`. Cooldown lock = enforced di `RetakeRules.CanRetake` :49-51 (server). [VERIFIED]
**Warning signs:** Endpoint langsung panggil `ExecuteAsync` tanpa `CanRetakeAsync`.

### Pitfall 3: Lupa clear TempData token → worker bisa skip lobby token
**What goes wrong:** Setelah retake, `StartExam` cek `TempData.Peek($"TokenVerified_{id}")` (:944) — non-consume. Token stale dari percobaan sebelumnya masih ada → worker masuk tanpa re-verify token.
**Why it happens:** `ExecuteAsync` sengaja TIDAK clear TempData (HTTP-scoped, doc :38-39).
**How to avoid:** `TempData.Remove($"TokenVerified_{id}")` SETELAH ExecuteAsync sukses (cermin ResetAssessment :4319). Must-fix #1.
**Warning signs:** Worker bisa langsung StartExam tanpa modal token pasca-retake.

### Pitfall 4: Razor dynamic/JS render TIDAK ter-cover grep+build (lesson 354/413)
**What goes wrong:** Tombol retake / countdown / modal / accordion render OK di build tapi handler/leak-suppression mati di browser (mis. `ReferenceError` abort, atau kunci tampil karena cabang switch salah). Lesson v32.5 Phase 413: runtime-smoke (cek simbol render) TIDAK menangkap `ReferenceError` yang abort handler-attach.
**Why it happens:** C# compile-check tak menjangkau perilaku DOM/JS runtime.
**How to avoid:** Verifikasi WAJIB pakai real-browser Playwright @5270 (branch ITHandoff). Cek eksplisit: (a) `ShowWrongFlagsOnly` TIDAK ada teks "(Jawaban Benar)"/highlight success di DOM, (b) countdown tick + tombol enable saat 0, (c) modal submit POST sukses, (d) accordion expand. **Catatan: e2e penuh = Phase 408 RTK-14**, tapi 407 plan SEBAIKNYA sertakan smoke leak-safety minimal (lihat §Validation Architecture Wave 0).
**Warning signs:** "build hijau" dijadikan bukti leak-safe — tidak cukup.

### Pitfall 5: `IsPassed` truthiness — pending grading (null) vs failed (false)
**What goes wrong:** Tier/retake salah saat essay pending. `RetakeRules.CanRetake` :45 menolak `isPassed != false` (null=pending → tak eligible). Tapi VM `AssessmentResultsViewModel.IsPassed` adalah **bool non-nullable** (:11, dihitung `score >= passPercentage`) — kehilangan distinction pending.
**Why it happens:** VM existing `IsPassed` bool, sedangkan tier butuh tri-state (`assessment.IsPassed` bool? + `IsPendingGrading`).
**How to avoid:** Untuk tier, JANGAN pakai VM.IsPassed bool. Pakai `assessment.IsPassed` (bool?) langsung di controller saat hitung RetakeMode + `IsPendingGrading` existing (:21). Pending → `ShowFullReview` (atau pertahankan existing pending banner :37) — verifikasi: pending bukan retake-eligible jadi tak ada percobaan tersisa untuk ditahan.
**Warning signs:** Worker essay-pending melihat `ShowWrongFlagsOnly` padahal belum dinilai.

## Code Examples

### Build VM flags di CMPController.Results (sisipan)
```csharp
// Source: cermin RetakeService.CanRetakeAsync counting (RetakeService.cs:237-242) + Results VM build (:2347)
int eraRetakeArchives = await _context.AssessmentAttemptHistory
    .Where(h => h.UserId == assessment.UserId && h.Title == assessment.Title && h.Category == assessment.Category
             && _context.AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id))
    .CountAsync();
int currentAttempt = eraRetakeArchives + 1;
bool canRetake = await _retakeService.CanRetakeAsync(id);
bool attemptsRemaining = currentAttempt < assessment.MaxAttempts;
// tier: pakai assessment.IsPassed (bool?) BUKAN VM.IsPassed (bool) — Pitfall 5
var reviewMode = RetakeRules.ResolveReviewMode(assessment.AllowAnswerReview, assessment.IsPassed, attemptsRemaining);
// CooldownUntilUtc null bila no-jeda / passed / belum completed
DateTime? cooldownUntil = (assessment.AllowRetake && assessment.RetakeCooldownHours > 0 && assessment.CompletedAt.HasValue)
    ? assessment.CompletedAt.Value.AddHours(assessment.RetakeCooldownHours) : (DateTime?)null;
bool isCapReached = assessment.IsPassed == false && assessment.AllowRetake && currentAttempt >= assessment.MaxAttempts;
// → set viewModel.RetakeMode/CanRetake/CurrentAttempt/MaxAttempts/CooldownUntilUtc/IsCapReached
```

### Riwayat load (cermin HC, di CMPController.Results)
```csharp
// Source: AssessmentAdminController.RiwayatPercobaan (:3493-3522), reuse penuh
var histories = await _context.AssessmentAttemptHistory
    .Where(h => h.UserId == assessment.UserId && h.Title == assessment.Title && h.Category == assessment.Category)
    .OrderByDescending(h => h.AttemptNumber).ToListAsync();
var histIds = histories.Select(h => h.Id).ToList();
var archiveRows = await _context.AssessmentAttemptResponseArchives
    .Where(a => histIds.Contains(a.AttemptHistoryId)).ToListAsync();
var currentRows = new List<AssessmentAttemptResponseArchive>();
if (assessment.Status == "Completed") {
    var assign = await _context.UserPackageAssignments.FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
    var qids = assign?.GetShuffledQuestionIds() ?? new List<int>();
    if (qids.Count > 0) {
        var qs = await _context.PackageQuestions.Include(q => q.Options).Where(q => qids.Contains(q.Id)).ToListAsync();
        var resp = await _context.PackageUserResponses.Where(r => r.AssessmentSessionId == id).ToListAsync();
        if (qs.Count > 0) currentRows = RetakeArchiveBuilder.Build(0, qs, resp);
    }
}
viewModel.RiwayatAttempts = RiwayatUnifier.Build(assessment, histories, archiveRows, currentRows);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Retake hanya via HC `ResetAssessment` | Worker self-service `CMP/RetakeExam` | Phase 407 (ini) | Pertama kali worker trigger sendiri → ownership+antiforgery+re-check kritis |
| Review = boolean `AllowAnswerReview` (full / none) | Tier 3-state (full / wrong-flags-only / score-only) | Phase 407 | Leak-safety: kunci ditahan selama percobaan tersisa |
| Riwayat hanya HC (`AssessmentMonitoringDetail`) | Riwayat juga ke worker (Results/Records) ter-gate | Phase 407 | RiwayatUnifier reuse; partial worker baru |

**Deprecated/outdated:** Tidak ada. Existing full-review markup (Results.cshtml:318-411) DIPERTAHANKAN sebagai cabang `ShowFullReview` — jangan hapus.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Pending grading (`IsPassed==null`) → tier `ShowFullReview` (bukan WrongFlagsOnly) karena pending tak retake-eligible | Pattern 3 truth table + Pitfall 5 | Bila salah: worker essay-pending lihat mode keliru. Mitigasi: pending bukan eligible (CanRetake :45), jadi tak ada percobaan tersisa untuk ditahan — tapi planner WAJIB konfirmasi cabang pending eksplisit di plan + test. |
| A2 | `IsCapReached` saat `IsPassed==false && currentAttempt >= MaxAttempts` (cap = bisa-lock hanya saat gagal) | Pattern 2 / Code Examples | Bila passed, tak ada lock relevan. Low risk — passed worker tak lihat retake control. |
| A3 | Drill-down archived secara struktural verdict-only (arsip tak simpan option-list/kunci) cukup untuk leak-safety archived | Pattern 4 / Pitfall 1 | [VERIFIED dari model] — rendah. Tetap perlu suppress total saat `ShowScoreOnly`. |
| A4 | `CMPController` perlu inject `RetakeService` (belum ter-inject saat ini) | Standard Stack / Pattern 1 | [VERIFIED grep] — pasti benar; tambah ke constructor. |

**Catatan:** A1 adalah satu-satunya yang butuh konfirmasi planner/discuss (cabang pending tidak eksplisit di truth table CONTEXT D-03 — D-03 hanya sebut passed/failed/exhausted, tidak pending). Sisanya verified.

## Open Questions

1. **Cabang `IsPassed == null` (pending grading) di tier resolution**
   - What we know: D-03 truth table hanya cover passed/failed-with-attempts/failed-exhausted. CanRetake menolak pending (tak eligible).
   - What's unclear: Tier mode eksplisit saat pending+AllowAnswerReview==true. Existing perilaku: pending banner (:37) + full review markup tampil (essay "Menunggu Penilaian" badge).
   - Recommendation: `ShowFullReview` (pertahankan existing pending UX) — pending bukan retake-context. Tambahkan test eksplisit cabang ini. Flag ke discuss bila planner ragu (A1).

2. **Records.cshtml: per-row trigger vs andalkan "Lihat Hasil"**
   - What we know: "Lihat Hasil" (:262) sudah route ke Results tempat riwayat hidup. D-04 discretion.
   - What's unclear: Apakah perlu trigger riwayat langsung di baris Records.
   - Recommendation: Default andalkan "Lihat Hasil" (minimal surface); trigger per-row opsional (mirror HC dropdown) bila planner mau parity dengan HC.

3. **Auto-enable countdown: poll vs reload-prompt**
   - What we know: D-01 discretion. UI-SPEC sediakan kedua copy.
   - Recommendation: Auto-enable + relabel tombol saat `<=0` (mirror exam-timer clearInterval) — UX lebih baik daripada minta reload. Server tetap re-validate.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (net8.0) | build/run | ✓ (asumsi — proyek aktif) | net8.0 | — |
| SQL Server (LocalDB/SQLEXPRESS) | integration test fixture (`RetakeServiceFixture` real-SQL @localhost\SQLEXPRESS) | ✓ (dipakai 405 integration) | — | Unit test (RetakeRules tier) SQL-less; integration `[Trait("Category","Integration")]` skippable via `--filter "Category!=Integration"` |
| dotnet run @ port 5270 | manual + Playwright UAT (branch ITHandoff) | ✓ | — | port 5277 dipakai worktree main — JANGAN tabrakan (CLAUDE.md) |
| Playwright | E2E lifecycle | ✓ (project terpisah) | — | **Phase 408 RTK-14** untuk lifecycle penuh; 407 smoke leak-safety opsional |

**Missing dependencies with no fallback:** Tidak ada — semua tooling sudah dipakai 405/406.
**Missing dependencies with fallback:** Integration test butuh SQLEXPRESS live; bila absen di CI → filter skip Integration (unit tier helper tetap jalan).

## Validation Architecture

> nyquist_validation = true [VERIFIED: .planning/config.json `workflow.nyquist_validation: true`]. Section disertakan.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (+ EF Core InMemory 8.0.0, real-SQL fixture untuk Integration) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (net8.0, IsTestProject) |
| Quick run command | `dotnet test HcPortal.Tests --filter "Category!=Integration"` (unit-only, SQL-less, cepat) |
| Full suite command | `dotnet test HcPortal.Tests` (incl Integration real-SQL @localhost\SQLEXPRESS) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| RTK-11 | `ResolveReviewMode` — semua cabang truth table (incl pending null) | unit | `dotnet test --filter "FullyQualifiedName~RetakeRulesTests"` | ⚠️ extend RetakeRulesTests.cs (file ada; tambah tes ResolveReviewMode) |
| RTK-13 | `CanRetake` guards (existing, regresi tidak rusak) | unit | `dotnet test --filter "FullyQualifiedName~RetakeRulesTests"` | ✅ RetakeRulesTests.cs (semua cabang existing) |
| RTK-09 | `RetakeExam` ownership reject (non-owner → Forbid) + re-check + token clear | integration/controller | `dotnet test --filter "FullyQualifiedName~RetakeExam"` | ❌ Wave 0 (new — controller test atau integration mirror RetakeServiceTests fixture) |
| RTK-09/07 | retake-then-pass → 1 cert, counting (UserId,Title,Category) no-conflate | integration | `dotnet test --filter "Category=Integration"` | ⚠️ RetakeServiceTests.cs (counting/snapshot sudah ada; retake-then-pass-via-worker = Phase 408 RTK-14 lifecycle) |
| RTK-12 | `RiwayatUnifier.Build` unify+order+IsCurrent (regresi) | unit | `dotnet test --filter "FullyQualifiedName~RiwayatUnifierTests"` | ✅ RiwayatUnifierTests.cs |
| RTK-11 leak-safety | DOM: `ShowWrongFlagsOnly` tak ada "(Jawaban Benar)"/list-group-item-success/CorrectAnswer | e2e (manual-justified untuk 407; full @408) | Playwright @5270 (Phase 408 RTK-14) | ❌ Phase 408 (smoke 407 opsional) |
| RTK-10 | countdown tick + enable@0 + modal POST | e2e | Playwright @5270 (Phase 408) | ❌ Phase 408 |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test HcPortal.Tests --filter "Category!=Integration"` (unit hijau; tier helper + regresi RetakeRules/RiwayatUnifier).
- **Per wave merge:** `dotnet test HcPortal.Tests` (full incl Integration; pastikan counting/snapshot 405 tidak regresi).
- **Phase gate:** full suite hijau + `dotnet build` 0 error + real-browser smoke leak-safety @5270 (lesson 354/413 — build hijau ≠ leak-safe) SEBELUM `/gsd-verify-work`. Lifecycle Playwright penuh + security audit = Phase 408.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/RetakeRulesTests.cs` — TAMBAH tes `ResolveReviewMode` (5 cabang truth table incl pending null) — covers RTK-11.
- [ ] Controller/integration test untuk `RetakeExam`: non-owner→Forbid, not-eligible→redirect, sukses→token-cleared+redirect StartExam — covers RTK-09 (pola: NoOpHubContext + NullLogger mirror RetakeServiceTests.cs:15-16, atau controller unit dengan mocked service).
- [ ] (Opsional, kuat) Playwright smoke leak-safety @5270: assert DOM `ShowWrongFlagsOnly` TIDAK mengandung kunci — covers RTK-11 leak risk lebih awal dari 408.
- [ ] Framework install: TIDAK perlu (xUnit + fixture sudah ada).

*(Existing test infra menutup RTK-13/RTK-12 regresi; gap utama = tier helper unit + RetakeExam endpoint test.)*

## Security Domain

> security_enforcement: tidak eksplisit false di config → enabled. Fase ini security-critical (pertama kali worker self-trigger retake + answer-key leak surface).

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Server-authoritative eligibility (countdown UX-only); tier dihitung server, view trusts VM |
| V2 Authentication | yes (indirect) | `CMPController [Authorize]` (:25) class-level; effective user via `GetCurrentUserRoleLevelAsync` (impersonation-aware) |
| V4 Access Control | **yes (CRITICAL)** | Ownership `session.UserId == effectiveUser.Id` → `Forbid()` (IDOR prevention pada retake another worker's session); re-check `CanRetakeAsync` server-side |
| V5 Input Validation | yes | `id` route int; tidak ada free-form input di RetakeExam (hanya id) |
| V6 Cryptography | no | Tidak ada operasi kripto baru |
| V13/CSRF | **yes (CRITICAL)** | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` di form modal (state-changing POST) |
| V5 Output Encoding (leak) | **yes (CRITICAL)** | Answer-key concealment (`ShowWrongFlagsOnly`): suppress correct-option highlight/(Jawaban Benar)/CorrectAnswer; Razor `@` auto-encode user content (no `Html.Raw`) |

### Known Threat Patterns for ASP.NET MVC retake/feedback

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Worker retakes another worker's session (IDOR) | Elevation/Tampering | Ownership guard `session.UserId==user.Id` → Forbid (RTK-09) |
| CSRF on retake POST (force-archive victim's attempt) | Tampering | `[ValidateAntiForgeryToken]` + token di form |
| Cooldown/cap bypass via DevTools (enable disabled btn) | Tampering | Server re-check `CanRetakeAsync` — countdown JS non-authoritative (D-01) |
| Answer-key leak while retake possible | Information Disclosure | Tier `ShowWrongFlagsOnly` suppress kunci (Results.cshtml leak sites :366/:388/:403) — verifikasi DOM via Playwright (408) |
| Double-click double-archive | Tampering | Sudah di-handle `ExecuteAsync` claim-atomik (status Open → no-op, RetakeService :89-120) |
| Stale token skip lobby pasca-retake | Tampering/AuthN bypass | `TempData.Remove($"TokenVerified_{id}")` (must-fix #1) |
| XSS via QuestionText/UserAnswer/AnswerText | Tampering | Razor `@` auto-encode; never `Html.Raw` user content |

**Catatan untuk secure-phase (Phase 408 RTK-14):** validasi ulang leak-safe D-03 di DOM real-browser (UI-SPEC checker sign-off :398 eksplisit minta ini). 407 sebaiknya sudah lulus smoke leak-safety sebelum hand-off ke 408.

## Sources

### Primary (HIGH confidence)
- `Helpers/RetakeRules.cs` (CanRetake :29-52, ShouldHideRetakeToggle :59-60) — eligibility pure
- `Services/RetakeService.cs` (ExecuteAsync :69-223, CanRetakeAsync :232-248, TempData doc :38-39) — engine + counting
- `Helpers/RiwayatUnifier.cs` (Build :21-69) + `Models/RiwayatAttemptViewModel.cs` — unify pure
- `Helpers/AssessmentScoreAggregator.cs` (IsQuestionCorrect :73-98) — verdict
- `Models/AssessmentAttemptResponseArchive.cs` (:23-39) — arsip verdict-only (KEY untuk Pitfall 1)
- `Controllers/AssessmentAdminController.cs` — ResetAssessment :4244-4327 (mirror endpoint), RiwayatPercobaan :3485-3524 (mirror riwayat load), UpdateRetakeSettings :5613-5660 (guard pattern), DI :32/47/58
- `Controllers/CMPController.cs` — Results :2184-2391 (VM build site + AllowAnswerReview branch :2243), StartExam :901-950 (TempData.Peek :944, token re-require), class [Authorize] :25, GetCurrentUserRoleLevelAsync usage
- `Views/CMP/Results.cshtml` — review block :316-418 (leak sites :355-394/:403), action area :420-431
- `Views/CMP/Records.cshtml` — Aksi cell :260-283, "Lihat Hasil" :262
- `Views/Admin/_RiwayatPercobaan.cshtml` — HC partial (verdict-only, badge "Gagal" :33)
- `Views/CMP/StartExam.cshtml` — exam-timer JS idiom :460-490 (countdown mirror)
- `Models/AssessmentResultsViewModel.cs`, `Models/AllWorkersHistoryRow.cs`, `Models/AssessmentSession.cs` (AllowRetake/MaxAttempts/RetakeCooldownHours/AllowAnswerReview/AssessmentType/IsManualEntry)
- `HcPortal.Tests/{RetakeRulesTests,RetakeServiceTests,RiwayatUnifierTests}.cs` (test pola + fixture)
- `Program.cs:63` (RetakeService DI), `HcPortal.Tests/HcPortal.Tests.csproj` (framework), `.planning/config.json` (nyquist=true), `.planning/REQUIREMENTS.md` (RTK-09..14)
- `407-CONTEXT.md`, `407-UI-SPEC.md`, `405-CONTEXT.md`, `CLAUDE.md`

### Secondary (MEDIUM confidence)
- MEMORY index: lesson 354/413 (Razor/JS WAJIB Playwright runtime; build hijau ≠ runtime-safe)

### Tertiary (LOW confidence)
- None — semua klaim diverifikasi terhadap kode repo.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — brownfield pin, dibaca dari csproj/Program.cs/_Layout (via UI-SPEC)
- Architecture: HIGH — semua pola mirror kode 405/406 yang sudah ter-ship & dibaca langsung
- Pitfalls: HIGH — Pitfall 1 (arsip verdict-only) + Pitfall 3 (TempData Peek) diverifikasi dari source
- Tier/leak-safety: HIGH untuk struktur; A1 (cabang pending) MEDIUM — butuh konfirmasi planner

**Research date:** 2026-06-22
**Valid until:** 2026-07-22 (kode internal stabil; re-cek bila 405/406 di-refactor)
