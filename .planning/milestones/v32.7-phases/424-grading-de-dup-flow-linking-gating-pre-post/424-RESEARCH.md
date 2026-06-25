# Phase 424: Grading De-dup + Flow/Linking + Gating Pre→Post - Research

**Researched:** 2026-06-24
**Domain:** ASP.NET Core MVC + EF Core (SQL Server) — refactor konsolidasi: scoring/dedupe satu fungsi murni, pairing Pre/Post satu sumber, gate StartExam, validasi essay server-side. migration=FALSE (refactor only, NO schema/DB-write baru).
**Confidence:** HIGH (semua klaim `[VERIFIED]` dikonfirmasi dengan membaca kode aktual branch ITHandoff sesi ini; line number sudah DRIFTED dari CONTEXT — koreksi ada di Pitfalls)

> Provenance: `[VERIFIED: file:line]` = dibaca sesi ini. `[CITED: audit]` = `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md`. `[ASSUMED]` = lihat Assumptions Log (hanya 2, non-material). Mengikuti gaya 423-RESEARCH.md.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (Gating "Pre selesai"):** Syarat = status Pre `== "Completed"` SAJA (sudah submit/dinilai), TANPA cek `IsPassed`. Pre = baseline, tak ada konsep lulus.
- **D-02 (Gating scope / orphan lewat):** Gate **hanya aktif bila pasangan Pre untuk peserta itu MEMANG ADA** tapi belum `Completed`. Post tanpa pasangan Pre (orphan / mode Standard) → **lewat** (tak diblok). Pairing yang dipakai = hasil GRDF-03 (link eksplisit terfilter UserId, BUKAN pola judul).
- **D-03 (Gating UX):** Blok di `StartExam` mengikuti pola gate existing: `TempData["Error"] = "Selesaikan Pre-Test dulu sebelum mulai Post-Test."; return RedirectToAction("Assessment");`. TIDAK ada perubahan view lobby.
- **D-04 (Essay timeout vs on-time):** Server tolak essay kosong **HANYA saat submit on-time** (waktu ujian masih ada). Timeout / auto-submit → tetap finalize walau essay kosong (pertahankan Phase 386 PXF-04, hindari dead-end).
- **D-05 (Essay cakupan tolak):** on-time + ≥1 essay kosong → **blokir SELURUH submit** + pesan ramah; semua essay wajib; server authoritative (client `flushEssay` bisa gagal — lesson Phase 413).
- **D-06 (Dedupe kanonik):** >1 response per soal → **last-write-wins** = `SubmittedAt` terbaru. SERAGAM untuk MC, MA (set), Essay. Konvergensikan 3 jalur (`GradeAndCompleteAsync` sudah last-write-wins; `Aggregator` `FirstOrDefault`; `ComputeScoreAndET` `.First()`).
- **D-07 (Paritas — sesi lama):** **TIDAK me-recompute** sesi Completed. Hasil numerik unified harus IDENTIK dengan jalur dominan; dijaga characterization/parity test (fokus MC >1 response). Forward-only.
- **D-08 (Link semu — forward-only):** Hentikan auto-deteksi Pre/Post dari pola judul untuk Standard **ke depan**. Baris Standard lama ber-link semu **TIDAK disentuh**. Tidak ada cleanup retroaktif.

### Claude's Discretion
- **GRDF-03:** mekanik penyatuan 3 jalur pairing (LinkedGroupId/UserId/LinkedSessionId) → satu helper terfilter per-UserId; kanonik = link eksplisit, BUKAN pola judul. Bentuk konkret diserahkan ke researcher/planner.
- **GRDF-05:** ekstrak helper (mis. `AllowedExamSeconds`/`ActiveDurationSeconds`) yang konsisten masukkan `ExtraTimeMinutes`; samakan clamp `CMPController.cs:469` dengan situs lain; fix under-report export "Durasi Aktual".
- Penempatan & nama kelas/fungsi murni baru (analog `CertIssuanceRules`/`SessionEditLockRules`/`SiblingSessionQuery`).
- Bentuk pesan ramah, format teks (selama non-destruktif & server-authoritative).

### Deferred Ideas (OUT OF SCOPE — IGNORE ENTIRELY)
- **GRDF-06 (manajemen peserta simetris / PA-07, PA-08):** DIBUANG dari 424. Sudah dikerjakan PENUH oleh **v32.5 di branch `main`** (`AssessmentAdminController.cs:2358-2880`: `AddParticipantsLive`/`RemoveParticipantCoreAsync`/`RemoveParticipantLive`/`RestoreParticipantLive`/`DeleteAssessmentPeserta`). Garap di 424 = duplikasi + dijamin konflik merge. JANGAN research/plan. Action item milestone: rekonsiliasi REQUIREMENTS.md + ROADMAP → tandai "covered by v32.5 merge".
- Cleanup retroaktif link semu Standard lama (D-08 menolak ini).
- Tech-debt timing (timer satu sumber, token server-authoritative, write-on-GET StartExam) — Phase 425 (FLOW-08/09/10, CLN-04).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| GRDF-01 | Peserta tak dapat StartExam Post sebelum Pre pasangannya `Completed` [FLOW-04, HIGH] | Gate-point dipetakan presisi (`CMPController.StartExam` sesudah cek `Completed`, sebelum token gate); pairing via GRDF-03 helper + `Status=="Completed"`; orphan pass-through. |
| GRDF-02 | Skor per-soal (MC/MA/Essay) satu fungsi murni + dedupe konsisten di semua jalur [GRD-09, GRD-02, GRD-03] | 3 site dipetakan by-line; logika per-soal identik di MC (single correct) / MA (SetEquals all-or-nothing) / Essay (EssayScore>0); usul `AssessmentScoreAggregator.Compute` jadi single-source dengan dedupe last-write-wins + reuse `IsQuestionCorrect`. |
| GRDF-03 | Pairing Pre/Post satu sumber, terfilter UserId [FLOW-01] | 3 jalur (LinkedGroupId / UserId / LinkedSessionId) dipetakan; smell `:292-297` (Pre tak ter-filter UserId) ditemukan; usul helper pure `PrePostPairing` analog `SiblingSessionQuery`. |
| GRDF-04 | Standard tak dapat link Pre/Post semu dari pola judul [FLOW-03] | `TryAutoDetectCounterpartGroup` + call-site `:876-882` dipetakan; usul matikan auto-pair untuk Standard forward-only. |
| GRDF-05 | ElapsedSeconds memperhitungkan ExtraTimeMinutes konsisten [FLOW-02] | clamp `:469` (tanpa ExtraTime) vs `:1175,1548,1626,4596` (dengan) dikonfirmasi; export "Durasi Aktual" `:4929-4931` under-report dikonfirmasi; usul helper `AllowedExamSeconds`. |
| GRDF-07 | Submit on-time tolak essay kosong server-side [VAL-03] | gate incomplete `:1630-1656` (`!serverTimerExpired`) dipetakan; usul perketat count "terjawab" untuk essay (`!IsNullOrWhiteSpace(TextAnswer)`) HANYA di cabang on-time. |

**GRDF-06 SENGAJA TIDAK ADA di tabel ini** — out of scope (lihat User Constraints / Deferred).
</phase_requirements>

---

## Summary

Fase 424 = **refactor konsolidasi murni** (seperti 422/423): tidak ada schema baru (migration=FALSE), tidak ada library baru. Enam kapabilitas: (1) gate gating Pre→Post di `StartExam`, (2) satukan logika scoring/dedupe per-soal ke satu fungsi murni, (3) satukan 3 jalur pairing Pre/Post jadi satu helper terfilter UserId, (4) matikan link-semu judul untuk Standard, (5) konsistenkan ElapsedSeconds dengan ExtraTimeMinutes, (6) validasi essay kosong server-side on-time. Pola yang diikuti persis: kelas `static` di `Helpers/` (analog `SiblingSessionQuery`, `SessionEditLockRules`, `CertIssuanceRules`) diuji dengan pure-unit truth-table + 1-2 integration real-SQL (`IClassFixture` disposable DB, recipe `GradingDedupeFixture`).

**Temuan kunci paling penting:** Banyak pekerjaan dasar SUDAH SETENGAH JADI di branch ini, sehingga risiko terbesar bukan "membangun" tapi "menyatukan tanpa mengubah angka" (D-07 paritas). Secara spesifik: (a) `GradeAndCompleteAsync` SUDAH last-write-wins dedupe MC `[VERIFIED: GradingService.cs:87-90]`, (b) `AssessmentScoreAggregator` SUDAH punya `IsQuestionCorrect` (Phase 383) dan `Compute` (Phase 376) pure EF-free `[VERIFIED: AssessmentScoreAggregator.cs:26-98]` tapi `Compute` pakai `FirstOrDefault` tanpa dedupe — itu drift GRD-02/GRD-03, (c) `SiblingSessionQuery.SiblingPrePostAwarePredicate` SUDAH jadi pure-expression pairing pattern `[VERIFIED: SiblingSessionQuery.cs:14-24]` — model langsung untuk GRDF-03, (d) `CertIssuanceRules`/`TryAssignNextSeqAsync` SUDAH ada dari Phase 423 (jangan duplikasi). Jadi GRDF-02 = mempromosikan satu sumber yang sudah ada (`Aggregator.Compute`) + menyuntik dedupe last-write-wins yang sudah ada (`GradeAndCompleteAsync`) ke dalamnya, dengan parity test sebagai jaring.

**Drift line number penting (CONTEXT vs kode current — sudah berubah karena Phase 423):** CONTEXT menyebut `GradeAndCompleteAsync` switch `:103-142` (BENAR), tapi `ComputeScoreAndETInternalAsync` ada di `:373-471` dengan `mcSel.First()` di `:414` (CONTEXT bilang `:390`); Aggregator `Compute` ada di `Helpers/AssessmentScoreAggregator.cs:26` (BUKAN `Services/`) dengan `FirstOrDefault` `:39`; ExtraTime clamp site `:4590-4592` sebenarnya `:4596`; export "Durasi Aktual" `:4831` sebenarnya `:4929-4931` (`:4831` = NIP cell). Auto-pair call-site `:878-882` (bukan `:878-888`).

**Primary recommendation:** Buat **3 pure helper baru di `Helpers/`** — (1) per-soal scoring promosikan/perluas `AssessmentScoreAggregator` jadi single-source dengan dedupe last-write-wins (GRDF-02), (2) `PrePostPairing` helper terfilter UserId analog `SiblingSessionQuery` (GRDF-03 + dipakai gate GRDF-01 + GRDF-04), (3) `ExamTimeRules.AllowedExamSeconds`/`ClampElapsed` (GRDF-05). Plus 2 guard inline non-helper: gate Pre-Completed di `StartExam` (GRDF-01) + perketat essay-empty count di cabang `!serverTimerExpired` (GRDF-07). Semua dijaga pure-test + parity-characterization real-SQL.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Skor per-soal MC/MA/Essay + dedupe (GRDF-02) | Domain helper (pure, `Helpers/AssessmentScoreAggregator`) | Service/Controller (caller) | Aturan deterministik EF-free → testable tanpa DB; satu source-of-truth menutup GRD-09/02/03. |
| Pairing Pre/Post terfilter UserId (GRDF-03) | Domain helper (pure Expression, `Helpers/`) | Service/Controller (EF query `.Where`) | Predikat murni dapat di-`Compile()` untuk unit + dipakai sebagai Expression di EF; analog `SiblingSessionQuery`. |
| Gate gating Pre→Post (GRDF-01) | Controller (`CMPController.StartExam`, server-authoritative) | Domain helper (pairing GRDF-03) | Penegakan akses harus server-side di entry-point ujian; reuse pairing helper agar orphan pass-through (D-02). |
| Stop link-semu Standard (GRDF-04) | Controller (`AssessmentAdminController` create POST) | — | Keputusan creation-time; matikan call ke `TryAutoDetectCounterpartGroup` untuk Standard. |
| Durasi aktif konsisten (GRDF-05) | Domain helper (pure, `Helpers/`) | Controller (clamp + export) | Hitung deterministik `(Duration+Extra)*60`; dipakai di clamp tulis + export baca. |
| Validasi essay kosong server (GRDF-07) | Controller (`CMPController.SubmitExam`, server-authoritative) | View (client `flushEssay` UX, non-authoritative) | Server jadi otoritas (D-05); client tetap UX cepat tapi bukan satu-satunya gate. |

---

## Standard Stack

Tidak ada paket baru. Seluruh kapabilitas memakai stack existing. (Sama seperti 423.)

### Core (existing, dipakai apa adanya)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore (SQL Server) | sesuai csproj current | Query `.Where(Expression)`, `ExecuteUpdateAsync` | Data layer; predikat pairing dikirim sebagai Expression ke EF [VERIFIED: SiblingSessionQuery dipakai sbg `Expression<Func<...>>`] |
| xUnit + `IClassFixture` + disposable real-SQL DB | current | Pure unit + integration real-SQL parity | Pola proyek; `GradingDedupeFixture` (disposable `HcPortalDB_Test_{guid}` + `MigrateAsync` + drop-on-dispose) [VERIFIED: GradingDedupeTests.cs:29-83] |

### Alternatives Considered (dan DITOLAK oleh decisions)
| Instead of | Could Use | Tradeoff | Verdict |
|------------|-----------|----------|---------|
| Dedupe last-write-wins (SubmittedAt) | first-write / arbitrary `FirstOrDefault` | Lebih sederhana tapi non-deterministik & sudah jadi drift (GRD-02) | **DITOLAK D-06.** Last-write-wins = perilaku jalur dominan, perubahan minimal, paritas terjaga. |
| Helper pairing pure Expression | Filtered-unique index `(LinkedGroupId,UserId,AssessmentType)` (audit menyebut sbg opsi [CITED: audit:360]) | Lebih kuat tapi butuh schema/migration | **DITOLAK** (migration=FALSE). Pakai helper app-level. |
| Stop link-semu = jangan tempel LinkedGroupId | Cleanup retroaktif baris lama | Sentuh data lama | **DITOLAK D-08.** Forward-only, baris lama tak disentuh. |
| Gate gating di `StartExam` | Gate di lobby/disable tombol view | Perubahan view | **DITOLAK D-03.** Server-authoritative di StartExam, no view change. |

**Installation:** Tidak ada. `dotnet build` cukup.
**Version verification:** Tidak relevan — 0 dependency baru ditambahkan. [VERIFIED: scope refactor]

---

## Architecture Patterns

### System Architecture Diagram (alur sesudah refactor)

```
  Worker StartExam(Post) ──▶ CMPController.StartExam :904
    ├─ authz/owner :914 ─▶ Upcoming/Open :918 ─▶ Completed :936
    │                                                  │
    │   ╔══════════════ GATE GRDF-01 (BARU, sisip ~:941, sesudah Completed, sebelum token) ══════╗
    │   ║ pairedPre = PrePostPairing.FindPairedPre(ctx, thisPost, thisPost.UserId)  ◀─ GRDF-03    ║
    │   ║   • assessment.AssessmentType=="PostTest" && pairedPre != null              (D-02)       ║
    │   ║   • pairedPre.Status != "Completed"  → TempData["Error"]=... ; Redirect("Assessment")    ║
    │   ║   • pairedPre == null (orphan/Standard) → LEWAT (no block)                  (D-02)        ║
    │   ╚════════════════════════════════════════════════════════════════════════════════════════╝
    │                                                  │
    └─ token :945 ─▶ window :956 ─▶ duration :962 ─▶ Abandoned :970 ─▶ InProgress :977

  Worker SubmitExam :1576
    ├─ authz/STAT-01 :1592 ─▶ serverTimerExpired (server-authoritative) :1622-1628
    │                                                  │
    │   if (!serverTimerExpired)  ── on-time ──┐       │
    │     incomplete gate :1630-1656           │       │
    │     ╔═══ GRDF-07 (BARU): essay "terjawab" iff !IsNullOrWhiteSpace(TextAnswer) ═══╗
    │     ║  on-time + ≥1 essay kosong → blokir SELURUH submit + pesan ramah (D-05)     ║
    │     ╚════════════════════════════════════════════════════════════════════════════╝
    │   else (timeout/auto-submit) ── essay kosong TETAP finalize → PendingGrading (D-04, PXF-04)
    │                                                  ▼
    │                           GradingService.GradeAndCompleteAsync :57
    │                             dedupe last-write-wins (SubmittedAt) :87-90  ◀── kanonik D-06
    │                             ┌──────────────────────────────────────────────┐
    │                             │ SINGLE SCORER (GRDF-02): per-soal MC/MA/Essay │
    │                             │  promosikan AssessmentScoreAggregator.Compute │
    │                             │  + inject dedupe last-write-wins              │
    │                             │  paths converge: GradeAndComplete / Compute-  │
    │                             │  ScoreAndETInternal / Aggregator.Compute      │
    │                             └──────────────────────────────────────────────┘

  GRDF-05: ElapsedSeconds  ── ExamTimeRules.AllowedExamSeconds(Duration, Extra) (BARU)
    write: UpdateSessionProgress clamp :469  +export read: "Durasi Aktual" :4929
  GRDF-04: create POST ── matikan TryAutoDetectCounterpartGroup utk Standard :876-882 (forward-only)
```

### Recommended Project Structure (delta)
```
Helpers/
├── AssessmentScoreAggregator.cs   # EXISTING — promosikan jadi SINGLE per-soal scorer; inject dedupe last-write-wins (GRDF-02)
├── PrePostPairing.cs              # NEW — pure: pairedPre lookup terfilter UserId (GRDF-03); dipakai gate GRDF-01
├── ExamTimeRules.cs               # NEW — pure: AllowedExamSeconds(duration,extra)+ClampElapsed (GRDF-05)
├── SiblingSessionQuery.cs         # EXISTING — model pola pure-Expression untuk PrePostPairing
└── CertIssuanceRules.cs           # EXISTING (Phase 423) — JANGAN sentuh
Services/
└── GradingService.cs             # wire scorer di GradeAndCompleteAsync(:57) + ComputeScoreAndETInternalAsync(:373); paritas
Controllers/
├── CMPController.cs               # gate GRDF-01 StartExam(~:941); essay-empty GRDF-07 SubmitExam(:1630-1656); clamp GRDF-05(:469); pairing FLOW-01(:292-297,3510)
└── AssessmentAdminController.cs   # GRDF-04 matikan auto-pair Standard(:876-882); export Durasi GRDF-05(:4929)
HcPortal.Tests/
├── PrePostPairingTests.cs         # NEW pure truth-table (analog SiblingPrePostFilterTests)
├── ExamTimeRulesTests.cs          # NEW pure (AllowedExamSeconds, clamp)
├── GradingDedupeTests.cs          # EXISTING — extend: parity MC>1-response, essay/MA dedupe
├── EnsureCanSubmitStandardTests.cs# EXISTING — extend: essay-empty on-time block / timeout pass
└── (gate) PrePostGatingTests.cs   # NEW real-SQL StartExam gate (orphan pass-through, Pre-not-Completed block)
```

### Pattern 1: Pure pairing helper (analog `SiblingSessionQuery`)
**What:** Helper `static` mengembalikan paired-Pre untuk satu Post + UserId. Kanonik = link eksplisit (LinkedSessionId/LinkedGroupId), filter UserId, BUKAN pola judul.
**When to use:** GRDF-03 (semua konsumen pairing) + GRDF-01 (gate baca hasil yang sama agar orphan/Standard tak salah-gate, D-02).
**Example:**
```csharp
// Pola sumber: Helpers/SiblingSessionQuery.cs:14-24 (VERIFIED — pure Expression, Compile()-able untuk unit).
// Catatan: LinkedSessionId paling eksplisit (Post→Pre langsung). LinkedGroupId+UserId+AssessmentType backup.
public static class PrePostPairing
{
    // Async lookup paired-Pre untuk satu Post tertentu, terfilter UserId (GRDF-03 + GRDF-01 D-02).
    // Return null = orphan/Standard → caller (gate) HARUS pass-through.
    public static async Task<AssessmentSession?> FindPairedPreAsync(
        ApplicationDbContext ctx, AssessmentSession post)
    {
        if (post.AssessmentType != "PostTest") return null;        // Standard/Pre → bukan target gate
        // 1) Kanonik: LinkedSessionId eksplisit (Post→Pre) [VERIFIED: dipakai :2404-2407, :3510-3523]
        if (post.LinkedSessionId.HasValue)
            return await ctx.AssessmentSessions
                .FirstOrDefaultAsync(s => s.Id == post.LinkedSessionId.Value
                                       && s.UserId == post.UserId
                                       && s.AssessmentType == "PreTest");
        // 2) Fallback: LinkedGroupId + UserId + AssessmentType (filter UserId = fix FLOW-01 :292-297)
        if (post.LinkedGroupId.HasValue)
            return await ctx.AssessmentSessions
                .FirstOrDefaultAsync(s => s.LinkedGroupId == post.LinkedGroupId.Value
                                       && s.UserId == post.UserId         // ◀── KRITIS: tanpa ini bisa pair Pre pekerja lain
                                       && s.AssessmentType == "PreTest");
        return null;                                                // no link → orphan, pass-through
    }
}
```
> KRITIS (FLOW-01 smell): `CMPController.cs:292-297` query `completedPreSessions` TANPA `s.UserId == userId` → di display grouping bisa pasangkan Pre pekerja lain. Helper ini WAJIB filter UserId di SEMUA cabang. [VERIFIED: CMPController.cs:292-297 tak ada filter UserId]

### Pattern 2: Gate gating Pre→Post di StartExam (D-02/D-03)
**What:** Sisip gate sesudah cek `Completed` (`:936-940`), sebelum gate token (`:945`). Pakai `FindPairedPreAsync`. Orphan/Standard pass-through.
**Example:**
```csharp
// Sisip ~CMPController.cs:941. Pola gate existing: TempData["Error"]=...; RedirectToAction("Assessment").
// GRDF-01 (D-01/D-02/D-03): Post tak boleh mulai bila pasangan Pre ADA tapi belum Completed.
var pairedPre = await PrePostPairing.FindPairedPreAsync(_context, assessment);
if (pairedPre != null && pairedPre.Status != "Completed")   // D-01: Completed saja, BUKAN IsPassed
{
    TempData["Error"] = "Selesaikan Pre-Test dulu sebelum mulai Post-Test.";
    return RedirectToAction("Assessment");
}
// pairedPre == null (orphan/Standard) → lewat tanpa blok (D-02 non-destruktif, hindari false-block).
```
> Penempatan: SESUDAH `Completed` check (`:936`) penting agar reload Post yang sudah selesai tak ke-gate. SEBELUM `StartedAt` write (`:977`) agar tak ada write-on-GET untuk sesi terblok.

### Pattern 3: Single per-soal scorer + dedupe last-write-wins (GRDF-02 / D-06 / D-07)
**What:** Satu fungsi murni yang menerima jawaban FINAL per soal (sudah ter-dedupe last-write-wins) lalu skor MC/MA/Essay identik di 3 jalur. Promosikan `AssessmentScoreAggregator` jadi single-source.
**Logika per-soal KANONIK (harus identik byte-for-byte di 3 jalur — VERIFIED dari kode current):**
- **MC:** ambil response FINAL (last-write-wins by SubmittedAt) → `selectedOption.IsCorrect` → benar. [VERIFIED: GradingService.cs:106-113 = sudah dedupe; Aggregator.cs:39-44 = `FirstOrDefault` DRIFT; ComputeScoreAndET:411-417 = `.First()` DRIFT]
- **MA:** `selectedOptionIds.SetEquals(correctOptionIds)` all-or-nothing, non-empty guard (selected.Count>0). **MA TIDAK ter-dedupe** (multi-row sah). [VERIFIED: GradingService.cs:116-128; Aggregator.IsQuestionCorrect:78-82 non-empty guard]
- **Essay:** `EssayScore.HasValue ? EssayScore.Value > 0 : null` (null=pending). [VERIFIED: Aggregator.IsQuestionCorrect:84-88; Compute:52-54 add EssayScore]
- **Persentase (LOCKED D-04 Phase 376):** `maxScore>0 ? (int)((double)totalScore/maxScore*100) : 0`; `isPassed = pct >= PassPercentage`. [VERIFIED: Aggregator.cs:58; GradingService.cs:144-145; ComputeScoreAndET:429-430 — SUDAH IDENTIK]
```csharp
// Dedupe kanonik (SUDAH ADA di GradeAndCompleteAsync:87-90 — reuse pola ini sbg single source):
var finalByQuestion = allResponses
    .Where(r => r.PackageOptionId.HasValue)          // MC/single-answer only (MA dibaca penuh)
    .GroupBy(r => r.PackageQuestionId)
    .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.SubmittedAt).First());  // last-write-wins (D-06)
// Konvergensi: Aggregator.Compute :39 (FirstOrDefault) & ComputeScoreAndET :414 (.First()) HARUS
// di-feed dengan response yang SUDAH ter-dedupe ini (atau tarik dedupe ke dalam scorer).
```
> Strategi paritas (D-07): Untuk sesi normal (1 response/soal) hasil tak berubah — dedupe pada set 1-elemen = identik. Perubahan HANYA muncul saat >1 response (race multi-tab). Parity test WAJIB cover MC >1-response (sudah ada `GradingDedupeTests.Dedupe_PicksLatestSubmittedAt` — extend ke Aggregator/ComputeScoreAndET path).

### Pattern 4: ExamTimeRules helper (GRDF-05)
**What:** Pure helper hitung detik diizinkan termasuk ExtraTime, dipakai di clamp tulis + export baca.
**Example:**
```csharp
// Pola: (Duration+Extra)*60 — sudah dipakai konsisten di :1175,:1548,:1626,:4596 (VERIFIED), HANYA
// clamp :469 yang BOLONG (pakai DurationMinutes*60 tanpa Extra → over-clamp ElapsedSeconds → export under-report).
public static class ExamTimeRules
{
    public static int AllowedExamSeconds(int durationMinutes, int? extraTimeMinutes)
        => (durationMinutes + (extraTimeMinutes ?? 0)) * 60;
}
// Wire :469 — GANTI: clampedElapsed = Math.Min(clampedElapsed, session.DurationMinutes * 60);
//          MENJADI: clampedElapsed = Math.Min(clampedElapsed, ExamTimeRules.AllowedExamSeconds(session.DurationMinutes, session.ExtraTimeMinutes));
// Export "Durasi Aktual" :4930 — session.ElapsedSeconds/60 sudah benar SETELAH clamp :469 di-fix (root cause = clamp, bukan export math).
```

### Anti-Patterns to Avoid
- **Gate gating berdasarkan pola judul** ("Post Test ...") alih-alih link eksplisit → salah-gate orphan/Standard (langgar D-02). WAJIB pakai `FindPairedPreAsync`.
- **Gate gating cek `IsPassed`** → langgar D-01 (Pre baseline tak ada lulus). Cek `Status=="Completed"` saja.
- **Recompute sesi Completed** saat unifikasi scorer → langgar D-07. Forward-only; parity test jaga numerik.
- **Dedupe MA** (membuat MA ambil 1 baris) → MA multi-row sah, all-or-nothing rusak (selected jadi subset). [VERIFIED: GradingDedupeTests.Dedupe_MultipleAnswer_NotDeduped jaga ini]
- **Essay-empty reject di cabang `serverTimerExpired`** → langgar D-04, dead-end saat timeout. HANYA di cabang `!serverTimerExpired`.
- **Sentuh baris Standard lama ber-link-semu** → langgar D-08. Hanya matikan auto-pair untuk pembuatan baru.
- **Lupa filter UserId di salah satu cabang pairing** → root-cause FLOW-01.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Correctness per-soal (display + scoring) | Inline MC/MA/Essay switch baru | `AssessmentScoreAggregator.IsQuestionCorrect` / `Compute` | Sudah pure, EF-free, single-source kill-drift (Phase 376/383) [VERIFIED: AssessmentScoreAggregator.cs:26,73] |
| Dedupe last-write-wins MC | GroupBy ad-hoc baru | Pola `finalByQuestion` `OrderByDescending(SubmittedAt).First()` | Sudah jadi perilaku kanonik di GradeAndCompleteAsync [VERIFIED: GradingService.cs:87-90] |
| Pairing Pre/Post predikat | Query inline per-call | Helper pure `Expression` (analog `SiblingSessionQuery`) | Compile()-able untuk unit + Expression untuk EF; satu source-of-truth [VERIFIED: SiblingSessionQuery.cs] |
| Detik diizinkan (Duration+Extra) | `Duration*60` ad-hoc | `ExamTimeRules.AllowedExamSeconds` | `:1175/:1548/:1626/:4596` sudah pakai pola ini; `:469` bolong → satukan [VERIFIED] |
| Disposable test DB | InMemory provider | `GradingDedupeFixture` real-SQL | `ExecuteUpdateAsync` TAK didukung EF InMemory — throw sebelum logika [VERIFIED: GradingDedupeTests.cs:3-7] |
| ShuffledQuestionIds | Parse JSON manual | `packageAssignment.GetShuffledQuestionIds()` | Helper existing [VERIFIED: GradingService.cs:71] |

**Key insight:** Hampir semua primitif scoring/pairing/timing SUDAH ADA — fase ini soal **menyatukan & meng-gate**, bukan membangun baru. Risiko terbesar = (1) melewatkan satu dari 3 scoring-path, (2) lupa filter UserId di pairing, (3) merusak paritas numerik sesi lama (D-07).

---

## Runtime State Inventory

> Fase ini = refactor + guard di branch ITHandoff, migration=FALSE. Bukan rename/migrasi data. Inventory ringkas:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | Baris Standard lama ber-`LinkedGroupId` semu (dari auto-pair judul Phase 338) MUNGKIN ada di DB Dev; sesi Completed dengan >1 PackageUserResponse per soal MUNGKIN ada (race multi-tab) | **None — D-07/D-08 forward-only.** Jangan recompute, jangan cleanup. Parity test jaga numerik. |
| Live service config | None — verified (tak ada service eksternal terlibat scoring/pairing/gating). | None. |
| OS-registered state | None — verified. Tak ada `BackgroundService`/`AddHostedService` untuk grading [CITED: audit:406 grep=0]. | None. |
| Secrets/env vars | None — verified. | None. |
| Build artifacts | Helper baru (`PrePostPairing`, `ExamTimeRules`) + test baru ter-compile via `dotnet build`. | `dotnet build` standar. |

**Migration:** FALSE (dikonfirmasi CONTEXT + ROADMAP). TIDAK ada kolom/index baru. Filtered-unique index `(LinkedGroupId,UserId,AssessmentType)` yang disebut audit sebagai opsi backstop **DITOLAK** (butuh migration).

---

## Common Pitfalls

### Pitfall 1: Line number CONTEXT sudah DRIFT (Phase 423 menggeser kode)
**What goes wrong:** Planner cari `ComputeScoreAndETInternalAsync` di `:384-403` / `mcSel.First` di `:390` — sebenarnya method `:373-471`, `.First()` di `:414`. Aggregator di `Services/` — sebenarnya `Helpers/`. Export "Durasi Aktual" `:4831` — sebenarnya `:4929-4931` (`:4831`=NIP cell). ExtraTime site `:4590-4592` — sebenarnya `:4596`.
**Root cause:** Phase 423 menambah `CertIssuanceRules`/`TryAssignNextSeqAsync` ke GradingService → geser baris.
**How to avoid:** Pakai peta presisi di "Code Examples" (re-verified sesi ini), bukan line CONTEXT verbatim.

### Pitfall 2: Melewatkan satu scoring-path (GRD-09 belum tertutup)
**What goes wrong:** Hanya `GradeAndCompleteAsync` di-unify; `ComputeScoreAndETInternalAsync` (re-grade/preview) & `Aggregator.Compute` (essay-finalize) tetap divergen → GRD-02/03 belum tutup.
**Root cause:** Scoring tersebar 3 tempat lintas 2 file. `Aggregator.Compute :39` pakai `FirstOrDefault` (no dedupe); `ComputeScoreAndET :414` pakai `.First()` (no order). [VERIFIED]
**How to avoid:** Checklist 3-path wajib lewat scorer sama + dedupe sama sebelum verify. Parity test per-path.

### Pitfall 3: Dedupe merusak MA (all-or-nothing)
**What goes wrong:** Menerapkan last-write-wins ke MA → ambil 1 baris → `selected={X} ≠ correct={X,Y}` → MA jadi 0%.
**Root cause:** MA = multi-row sah; dedupe hanya untuk MC/single-answer (`PackageOptionId.HasValue` GroupBy).
**How to avoid:** Dedupe HANYA MC; MA dibaca penuh. [VERIFIED: GradingService.cs:86 komentar + GradingDedupeTests.Dedupe_MultipleAnswer_NotDeduped]

### Pitfall 4: Pairing tak filter UserId (FLOW-01 root)
**What goes wrong:** `completedPreSessions` `:292-297` pasangkan Pre pekerja lain dengan Post user.
**Root cause:** Query hanya filter `LinkedGroupId`+`AssessmentType`+`Status`, TANPA `UserId`. [VERIFIED: CMPController.cs:292-297]
**How to avoid:** `FindPairedPreAsync` filter `UserId` di SEMUA cabang. Gate GRDF-01 + display pairing pakai helper sama.

### Pitfall 5: Gate gating false-block orphan/Standard
**What goes wrong:** Gate aktif untuk Post tanpa Pre (mode Standard, atau Post berdiri sendiri) → dead-end.
**Root cause:** Gate berdasarkan pola judul / type saja tanpa cek "pasangan Pre EKSPLISIT ada".
**How to avoid:** D-02 — gate HANYA bila `FindPairedPreAsync != null`. `null` → pass-through. Test orphan pass-through wajib.

### Pitfall 6: Essay-empty reject di jalur timeout (dead-end)
**What goes wrong:** Worker kehabisan waktu, essay kosong → submit ditolak → sesi macet (tak bisa finalize, tak bisa PendingGrading).
**Root cause:** Menaruh reject di luar cabang `!serverTimerExpired`.
**How to avoid:** D-04 — reject HANYA `!serverTimerExpired` (`:1630-1656`). Timeout (`serverTimerExpired==true`) tetap finalize ke PendingGrading. [VERIFIED: GradingService.cs:206-248 essay→PendingGrading; CMPController.cs:1630 cabang]

### Pitfall 7: Essay "terjawab" dihitung dari baris-ada bukan isi-ada
**What goes wrong:** Gate incomplete `:1646-1648` hitung essay "answered" jika ada baris response di DB — padahal `SaveTextAnswer` terima `""` (kosong). Maka essay kosong lolos sebagai "terjawab".
**Root cause:** `dbResponses` `:1640-1644` count `PackageQuestionId` distinct tanpa cek `TextAnswer` non-kosong. [VERIFIED: CMPController.cs:1640-1648; audit VAL-03:377 "AssessmentHub.SaveTextAnswer terima ''"]
**How to avoid:** GRDF-07 — untuk soal essay, hitung "terjawab" hanya bila `!string.IsNullOrWhiteSpace(TextAnswer)`. Perlu join `TextAnswer` (saat ini `:1642` `Select(PackageQuestionId)` saja — perlu ambil `TextAnswer` juga + tahu mana qId essay via `QuestionType`).

### Pitfall 8: ElapsedSeconds over-clamp menyebabkan under-report (bukan export math)
**What goes wrong:** Coba fix export `:4930` (`ElapsedSeconds/60`) — padahal export benar; root cause = clamp `:469` membuang detik ExtraTime SAAT TULIS.
**Root cause:** `:469` `Math.Min(elapsed, DurationMinutes*60)` tanpa Extra → ElapsedSeconds tersimpan ter-cap di durasi-dasar. [VERIFIED: CMPController.cs:469 vs :1626 yang pakai Extra]
**How to avoid:** Fix di clamp `:469` (sumber). Export `:4930` otomatis benar.

---

## Code Examples

### Peta presisi site (re-verified sesi ini — pakai INI, bukan line CONTEXT)
```
GRDF-02 — 3 SCORING PATH:
  PATH 1 — Services/GradingService.cs:57 GradeAndCompleteAsync
    dedupe last-write-wins :87-90 (✅ kanonik D-06); MC switch :105-114; MA :116-128; Essay skip :130-134;
    ET score :149-191; pct :144-145. INITIAL grading worker submit.
  PATH 2 — Services/GradingService.cs:373 ComputeScoreAndETInternalAsync (private)
    MC `mcSel.First()` :414 (❌ no order — DRIFT GRD-03); MA SetEquals :419-422; pct :429-430.
    Dipakai RegradeAfterEditAsync(:479) re-grade + PreviewEditScore. NO dedupe.
  PATH 3 — Helpers/AssessmentScoreAggregator.cs:26 Compute
    MC `FirstOrDefault` :39 (❌ no dedupe — DRIFT GRD-02); MA :46-50; Essay EssayScore :52-54; pct :58.
    Dipakai jalur essay-finalize (FinalizeEssayGrading). Komentar :15-17 klaim "single source" — KELIRU (audit GRD-09).
  + IsQuestionCorrect :73 (display correctness, MC/MA/Essay) — single-source Phase 383, REUSE.

GRDF-01 — GATE: Controllers/CMPController.cs:904 StartExam
  insertion ~:941 (sesudah Completed :936, sebelum token :945). Pola gate :932-973 (TempData+Redirect).

GRDF-03 — PAIRING 3 JALUR:
  LinkedGroupId display: CMPController.cs:257-304 (smell :292-297 NO UserId filter ❌)
  LinkedGroupId+UserId: CMPController.cs:3510-3523 GetGainScoreData (✅ filter UserId via Dict)
  LinkedSessionId: CMPController.cs:2404-2413 (retake), :2790-2828/:2929-2952/:3033-3058 (trend/gain)

GRDF-04 — LINK SEMU: Controllers/AssessmentAdminController.cs
  call-site :876-882 (if CreationMode != "PrePostTest" → TryAutoDetectCounterpartGroup) ❌ matikan utk Standard
  helper :7663-7689 TryAutoDetectCounterpartGroup (regex judul `^(Pre|Post)\s*Test\s+(rest)$`)

GRDF-05 — ELAPSED: Controllers/CMPController.cs
  clamp BOLONG :469 (DurationMinutes*60 tanpa Extra ❌) vs :1175,:1548,:1626 (+Extra ✅), :4596 (+Extra ✅)
  export Durasi Aktual: AssessmentAdminController.cs:4929-4931 (ElapsedSeconds/60 — benar setelah :469 fix)

GRDF-07 — ESSAY: Controllers/CMPController.cs:1576 SubmitExam
  serverTimerExpired :1622-1628; incomplete gate :1630-1656 (count :1640-1648 ❌ tak cek TextAnswer isi)
  timeout finalize → GradingService.cs:206-248 PendingGrading (D-04 PXF-04 — JANGAN ubah)
```

### Pure pairing test (analog SiblingPrePostFilterTests)
```csharp
// Pola sumber: HcPortal.Tests/SiblingPrePostFilterTests.cs:12-58 (VERIFIED — Compile() predikat, no DB).
// Untuk FindPairedPreAsync (async EF) gunakan real-SQL fixture; untuk logika "Standard→null pass-through"
// & "PostTest→target" bisa pure jika diekstrak predikat. Minimal: real-SQL integration test.
public class PrePostGatingTests : IClassFixture<GradingDedupeFixture>   // reuse disposable DB recipe
{
    // Test: Post ber-LinkedSessionId ke Pre status=InProgress → FindPairedPreAsync != null & != Completed → gate BLOCK.
    // Test: Post ber-LinkedSessionId ke Pre status=Completed → pass (no block).
    // Test: Post tanpa LinkedSessionId/LinkedGroupId (orphan) → FindPairedPreAsync == null → pass-through (D-02).
    // Test: Standard (AssessmentType="Standard") → FindPairedPreAsync == null → pass-through.
    // Test: paired Pre milik USER LAIN (UserId beda) → TIDAK match (filter UserId) → pass-through.
}
```

### Parity test scorer (extend GradingDedupeTests)
```csharp
// Reuse GradingDedupeFixture (disposable real-SQL HcPortalDB_Test_{guid}, MigrateAsync, drop-on-dispose)
// [VERIFIED: GradingDedupeTests.cs:29-83]. Existing Dedupe_PicksLatestSubmittedAt jaga PATH 1.
// TAMBAH: parity untuk PATH 2 (ComputeScoreAndET via RegradeAfterEditAsync) & PATH 3 (Aggregator.Compute):
//   - MC 1-response: ketiga path skor IDENTIK (paritas D-07 baseline).
//   - MC >1-response (opsi beda, SubmittedAt beda): ketiga path ambil FINAL (last-write-wins) → skor sama.
//   - MA multi-row: tidak ter-dedupe (Dedupe_MultipleAnswer_NotDeduped sudah ada — extend ke Aggregator).
//   - Essay EssayScore>0 → benar; null → pending (tak menambah totalScore di interim).
```

### Essay-empty server validation test (extend EnsureCanSubmitStandardTests)
```csharp
// Test: on-time (StartedAt baru, belum lewat allowed) + 1 essay TextAnswer="" → SubmitExam Redirect ExamSummary
//        + TempData["Error"] berisi pesan ramah (D-05 blokir seluruh submit).
// Test: timeout (elapsed >= allowed) + 1 essay TextAnswer="" → SubmitExam LANJUT finalize → PendingGrading (D-04).
// Test: on-time + semua essay terisi → lolos (no block).
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Scoring inline 3 path (1 dedupe, 2 tidak) | Single scorer (`Aggregator`) + dedupe last-write-wins seragam | Fase 424 ini | Konsisten GRD-09/02/03; paritas dijaga parity-test (D-07) |
| Pairing 3 jalur divergen (1 tak filter UserId) | Helper `FindPairedPreAsync` terfilter UserId, link eksplisit | Fase 424 ini | Tutup FLOW-01; gate GRDF-01 reuse → orphan pass-through (D-02) |
| Post bisa mulai tanpa Pre Completed | Gate StartExam (Status=="Completed") | Fase 424 ini (keputusan bisnis a) | Tegakkan urutan pelaksanaan; non-destruktif orphan-safe |
| Standard auto-link via pola judul (Phase 338) | Forward-only stop auto-pair Standard | Fase 424 ini (D-08) | Hentikan link-semu baru; baris lama tak disentuh |
| Essay kosong hanya diblok client `flushEssay` | Server tolak on-time (D-04/D-05) | Fase 424 ini | Server authoritative; client UX tetap (lesson Phase 413 handler mati) |
| clamp ElapsedSeconds tanpa ExtraTime (`:469`) | `ExamTimeRules.AllowedExamSeconds` konsisten | Fase 424 ini | Fix export "Durasi Aktual" under-report (FLOW-02) |

**Deprecated/outdated:**
- Komentar `AssessmentScoreAggregator.cs:15-17` "single source of truth" — saat ini KELIRU (Compute hanya dipakai essay-finalize). Fase ini menjadikannya BENAR dengan mempromosikannya ke semua path. [VERIFIED + CITED: audit GRD-09:236]
- Auto-pair judul `TryAutoDetectCounterpartGroup` untuk Standard (Phase 338 REST-06) — dimatikan forward-only (D-08).

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Bentuk single-scorer = **mempromosikan `AssessmentScoreAggregator.Compute`** (yang sudah pure + punya Essay) jadi sumber + inject dedupe last-write-wins, BUKAN membuat kelas baru. Alternatif: kelas `QuestionScorer` baru. | Pattern 3 / GRDF-02 | Rendah — keduanya sah; promosi Aggregator = perubahan lebih kecil (sudah dipakai 1 path). Planner pilih final; D-06 kanonik tetap sama. |
| A2 | `FindPairedPreAsync` async (butuh EF query) cukup, TIDAK perlu pure-Expression `Compile()`-able seperti `SiblingSessionQuery`. Pairing butuh lookup DB (Pre lain), bukan filter in-memory list. | Pattern 1 / GRDF-03 | Rendah — gate butuh query Pre row apapun. Bisa diekstrak predikat pure untuk sub-test, tapi async tetap perlu. Tidak mengubah scope. |

**Catatan:** Tidak ada assumption material yang mengubah keputusan terkunci. Kedua A1/A2 hanya bentuk implementasi (Claude's Discretion).

---

## Open Questions

1. **Gate GRDF-01: apakah Admin/HC (impersonate / monitoring) ikut ter-gate?**
   - What we know: gate existing token (`:945`) di-skip untuk `assessment.UserId == user.Id` (worker) — Admin/HC bypass token. [VERIFIED: CMPController.cs:945]
   - What's unclear: apakah gate Pre-Completed harus skip untuk Admin/HC (debugging) atau berlaku universal.
   - Recommendation: Ikuti pola token gate — gate Pre-Completed hanya untuk worker (`assessment.UserId == user.Id`), Admin/HC bypass (konsisten + tak ganggu monitoring). Planner konfirmasi saat plan.

2. **GRDF-07: gate incomplete `:1640-1644` saat ini `Select(PackageQuestionId)` saja — perlu tahu mana qId essay + TextAnswer-nya.**
   - What we know: perlu join `QuestionType=="Essay"` (dari PackageQuestion) + `TextAnswer` non-kosong.
   - Recommendation: Perluas query `dbResponses` ambil `{PackageQuestionId, TextAnswer}` + lookup `QuestionType` dari packageQuestions; untuk essay, "terjawab" iff `!IsNullOrWhiteSpace(TextAnswer)`. Pertahankan MC/MA count existing. Planner detailkan di plan.

3. **Konvergensi scorer: tarik dedupe KE DALAM scorer, atau feed scorer dengan response sudah-dedupe?**
   - What we know: PATH 1 dedupe di luar lalu skor; PATH 2/3 tak dedupe.
   - Recommendation: Tarik dedupe ke dalam single-scorer (terima `allResponses` mentah, dedupe MC internal) agar 3 caller cukup panggil 1 fungsi — mengurangi drift. Planner pilih final (paritas tetap dijaga test).

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK / dotnet | build + test | ✓ (proyek aktif) | sesuai global.json | — |
| SQL Server (localhost\SQLEXPRESS) | Integration/parity tests (GradingDedupeFixture MigrateAsync) | ✓ (dipakai test existing) | — | Pure tests via `--filter "Category!=Integration"` |
| App @ http://localhost:5270 (branch ITHandoff) | UAT browser gate GRDF-01 + essay GRDF-07 | ✓ (per CLAUDE.md) | — | manual review markup |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** Integration/parity tests perlu SQLEXPRESS (`sqlcmd -C -I`); pure tests tidak.

---

## Validation Architecture

> nyquist_validation = true [VERIFIED: .planning/config.json] → section ini WAJIB.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (assembly `HcPortal.Tests`) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` |
| Quick run command | `dotnet test HcPortal.Tests --filter "Category!=Integration"` |
| Full suite command | `dotnet test HcPortal.Tests` (butuh `localhost\SQLEXPRESS` untuk Integration/parity) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| GRDF-01 | Post ber-pasangan Pre `!=Completed` → StartExam block (Redirect+TempData) | real-SQL integration | `dotnet test --filter "FullyQualifiedName~PrePostGating"` | ❌ Wave 0 |
| GRDF-01 | Pre `==Completed` → pass; orphan (no link) → pass-through; Pre user-lain → pass-through (D-02) | real-SQL integration | (PrePostGatingTests) | ❌ Wave 0 |
| GRDF-02 | Single scorer per-soal MC/MA/Essay truth-table (correctness + pct LOCKED) | unit (pure) | `dotnet test --filter "FullyQualifiedName~AssessmentScoreAggregator"` (existing+extend) | ✅ existing `AssessmentScoreAggregatorTests` / `IsQuestionCorrectTests` |
| GRDF-02 | **Parity:** MC >1-response → ketiga path (GradeAndComplete / Compute / Aggregator) skor IDENTIK last-write-wins | parity-characterization (real-SQL) | `dotnet test --filter "FullyQualifiedName~GradingDedupe"` (extend) | ✅ existing (`GradingDedupeTests`, extend) |
| GRDF-02 | MA multi-row TIDAK ter-dedupe (all-or-nothing utuh) | parity (real-SQL) | (GradingDedupeTests.Dedupe_MultipleAnswer) | ✅ existing |
| GRDF-03 | `FindPairedPreAsync` filter UserId; LinkedSessionId>LinkedGroupId; null=orphan | real-SQL integration | (PrePostGatingTests / PrePostPairingTests) | ❌ Wave 0 |
| GRDF-04 | Standard create → tidak set LinkedGroupId dari pola judul (forward-only) | controller/integration | `dotnet test --filter "FullyQualifiedName~AutoPair"` | ❌ Wave 0 |
| GRDF-05 | `AllowedExamSeconds(duration,extra)` = (d+e)*60; null extra=0 | unit (pure) | `dotnet test --filter "FullyQualifiedName~ExamTimeRules"` | ❌ Wave 0 |
| GRDF-05 | clamp `:469` pakai ExtraTime → ElapsedSeconds tak over-clamp (export benar) | real-SQL/controller | (ExamTimeRules integration atau UpdateSessionProgress test) | ❌ Wave 0 |
| GRDF-07 | on-time + essay kosong → block seluruh submit (D-05) | real-SQL integration | `dotnet test --filter "FullyQualifiedName~EnsureCanSubmit"` (extend) | ✅ existing (`EnsureCanSubmitStandardTests`, extend) |
| GRDF-07 | timeout + essay kosong → tetap finalize PendingGrading (D-04 PXF-04) | real-SQL integration | (EnsureCanSubmitStandardTests / EssayEmptyPendingParity) | ✅ existing (`EssayEmptyPendingParityTests`, jangan regress) |
| GRDF-01/07 | Gate browser + essay reject live | manual UAT (browser @5270) | manual — Playwright opsional | n/a |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests --filter "Category!=Integration"` (pure cepat <30s — Aggregator/ExamTimeRules/IsQuestionCorrect).
- **Per wave merge:** `dotnet test HcPortal.Tests` (full incl Integration/parity; SQLEXPRESS live).
- **Phase gate:** Full suite green sebelum `/gsd-verify-work` + UAT browser @5270 untuk gate gating + essay reject.
- **Coverage rationale (dedupe edge cases — KRITIS D-07):** Sampling WAJIB cover boundary: (a) 0 response (skip/0), (b) 1 response (paritas baseline — hasil tak boleh berubah), (c) 2 response opsi-beda SubmittedAt-beda (last-write-wins — satu-satunya kasus yang BERUBAH), (d) 2 response SubmittedAt SAMA (tie-break `.First()` deterministik?), (e) MA multi-row (no dedupe). Kasus (c)+(d) adalah inti GRD-02/03 — tanpa keduanya, drift bisa lolos. Parity test menjalankan input IDENTIK lewat 3 path dan assert skor sama (characterization), bukan hanya assert nilai absolut.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/PrePostGatingTests.cs` — real-SQL: gate GRDF-01 (block Pre-not-Completed, pass orphan/Standard/user-lain) + `FindPairedPreAsync` GRDF-03
- [ ] `HcPortal.Tests/ExamTimeRulesTests.cs` — pure: `AllowedExamSeconds` (GRDF-05)
- [ ] Extend `GradingDedupeTests.cs` — parity 3-path MC>1-response (GRDF-02 D-07)
- [ ] Extend `AssessmentScoreAggregatorTests.cs` / `IsQuestionCorrectTests.cs` — Aggregator jadi single-scorer (dedupe injected)
- [ ] Extend `EnsureCanSubmitStandardTests.cs` — essay-empty on-time block / timeout pass (GRDF-07)
- [ ] (opsional) AutoPair guard test — Standard tak dapat LinkedGroupId (GRDF-04)
- [ ] Reuse fixture: `GradingDedupeFixture` + `FakeNotificationService` + `FakeWorkerDataService` + GradingService ctor recipe — SUDAH ADA [VERIFIED: GradingDedupeTests.cs:75-83]
- [ ] Regression guard: jangan rusak `GradingDedupeTests`, `AssessmentScoreAggregatorTests`, `IsQuestionCorrectTests`, `ResultsEssayCorrectnessTests`, `EssayEmptyPendingParityTests`, `SiblingPrePostFilterTests`, `EnsureCanSubmitStandardTests`, `TokenGateTests`, `EssayFinalizeRecomputeTests`

---

## Security Domain

> security_enforcement diasumsikan enabled (absent = enabled). Fase menyentuh penegakan akses ujian (gate) + scoring authoritative → relevan.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | tak diubah |
| V4 Access Control | yes | `StartExam` owner-check existing `assessment.UserId != user.Id && !Admin && !HC → Forbid()` [VERIFIED: CMPController.cs:914]; gate GRDF-01 di-sisip SESUDAH owner-check — pertahankan. `SubmitExam` owner-check + antiforgery existing [VERIFIED: :1592]. |
| V5 Input Validation | yes | Essay kosong server-side (GRDF-07) = server-authoritative validation, tidak percaya client `flushEssay` (D-05). Pesan ramah JANGAN bocorkan jawaban-benar/kunci. |
| V6 Cryptography | no | tak ada crypto baru |
| V11 Business Logic | yes | Gate gating (GRDF-01) = authoritative server gate; scoring/dedupe (GRDF-02) = server-authoritative integrity (tak percaya skor client); pairing terfilter UserId (GRDF-03) = cegah cross-user data exposure. |

### Known Threat Patterns
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Bypass gating Post via manipulasi judul/type | Elevation | Gate pakai link EKSPLISIT terfilter UserId (`FindPairedPreAsync`), bukan pola judul (D-02) |
| Spoof essay terisi via client (handler mati Phase 413) | Tampering | Server-side reject on-time (D-05), `flushEssay` non-authoritative |
| Cross-user pairing (Pre pekerja lain) | Information Disclosure | Filter `s.UserId == userId` di SEMUA cabang pairing (FLOW-01 fix) |
| Skor non-deterministik via race multi-tab (>1 response) | Tampering | Dedupe last-write-wins deterministik (D-06) + parity test |
| Submit replay sesi terminal | Tampering | STAT-01 guard existing (Completed/Abandoned/Cancelled/PendingGrading) — pertahankan [VERIFIED: :1608] |

**Per-plan threat_model note:** setiap plan yang menyentuh `StartExam`/`SubmitExam` WAJIB catat: RBAC/ownership (present, jangan hapus), antiforgery POST (present pada SubmitExam), server-authoritative (gate/scoring/essay tak percaya client), no answer-key leak di pesan essay-rejection.

---

## Sources

### Primary (HIGH confidence — verified this session)
- `Services/GradingService.cs:57-360` — GradeAndCompleteAsync: dedupe last-write-wins :87-90, MC/MA/Essay switch :103-142, ET :149-191, pct :144-145, essay→PendingGrading :206-248, cert gate Phase 423 :301
- `Services/GradingService.cs:373-471` — ComputeScoreAndETInternalAsync: `mcSel.First()` :414 (drift), MA :419-422, pct :429-430
- `Services/GradingService.cs:479-588` — RegradeAfterEditAsync (caller PATH 2)
- `Helpers/AssessmentScoreAggregator.cs` (full) — Compute `FirstOrDefault` :39 (drift), IsQuestionCorrect :73 (single-source Phase 383), BuildAnswerCell :110, pct LOCKED :58
- `Controllers/CMPController.cs:450-484` — UpdateSessionProgress clamp BOLONG :469 (no ExtraTime)
- `Controllers/CMPController.cs:904-989` — StartExam gate chain (Completed :936, token :945, window :956, duration :962, Abandoned :970) — insertion point GRDF-01
- `Controllers/CMPController.cs:257-304` — pairing display (smell :292-297 NO UserId filter)
- `Controllers/CMPController.cs:1576-1665` — SubmitExam: serverTimerExpired :1622-1628, incomplete gate :1630-1656 (count :1640-1648 tak cek TextAnswer)
- `Controllers/CMPController.cs:2404-2413, 3505-3523` — LinkedSessionId/LinkedGroupId+UserId pairing paths
- `Controllers/AssessmentAdminController.cs:876-882` — auto-pair call-site (matikan Standard GRDF-04)
- `Controllers/AssessmentAdminController.cs:4929-4931` — export "Durasi Aktual" (ElapsedSeconds/60)
- `Controllers/AssessmentAdminController.cs:7663-7689` — TryAutoDetectCounterpartGroup (regex judul)
- `Helpers/SiblingSessionQuery.cs` (full) — pola pure-Expression pairing (model GRDF-03)
- `Helpers/CertIssuanceRules.cs` (existing Phase 423 — JANGAN sentuh)
- `HcPortal.Tests/GradingDedupeTests.cs` (full) — disposable real-SQL fixture recipe + dedupe MC/MA parity tests
- `HcPortal.Tests/SiblingPrePostFilterTests.cs` (full) — pure predikat truth-table recipe
- `.planning/config.json` — nyquist_validation:true, commit_docs:true

### Secondary (MEDIUM — audit doc)
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` — GRD-09 :236/:343, GRD-02 :402, GRD-03 :404, FLOW-01 :358-360, FLOW-03 :362-364, FLOW-02 :428, FLOW-04 :422, VAL-03 :376-379

### Tertiary (LOW)
- None — semua klaim material diverifikasi terhadap kode current.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — 0 dep baru; semua primitif terverifikasi.
- Architecture (3-path scoring map, pairing helper, gate point, clamp site): HIGH — line-verified sesi ini (line CONTEXT drift dikoreksi).
- Pitfalls: HIGH — termasuk koreksi line-drift + MA-no-dedupe + over-clamp root-cause.
- Validation: HIGH — fixture & extend-targets terverifikasi dari test existing.
- GRDF-02 bentuk single-scorer (promosi Aggregator vs kelas baru): MEDIUM — A1, Claude's Discretion; kanonik D-06 tetap.

**Research date:** 2026-06-24
**Valid until:** 2026-07-24 (stable; kode internal, no fast-moving external dep). ⚠️ Re-verify line number bila Phase 423 di-commit ulang / branch bergerak.
