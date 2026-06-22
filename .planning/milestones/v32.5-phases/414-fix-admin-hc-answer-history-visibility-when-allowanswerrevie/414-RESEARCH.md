# Phase 414: Fix Visibilitas History Jawaban Admin/HC saat AllowAnswerReview OFF - Research

**Researched:** 2026-06-22
**Domain:** ASP.NET Core MVC (.NET 8) — view-gating / authorization decoupling (owner vs non-owner), pure-static-helper seam + xUnit
**Confidence:** HIGH (semua klaim diverifikasi langsung dari kode di working tree; tidak ada dependensi eksternal)

## Summary

Fix ini sempit dan terverifikasi penuh: satu-satunya tempat di seluruh codebase yang men-*gate* tampilan history jawaban per-soal ("Tinjauan Jawaban") berdasarkan toggle `AllowAnswerReview` adalah `CMPController.Results` (build `questionReviews`) + `Views/CMP/Results.cshtml` (render section/alert). Semua pembacaan `AllowAnswerReview` lain di codebase adalah WRITE (create/edit/inherit) atau projeksi metadata daftar manajemen — **bukan** gate per-soal. Tidak ada surface sibling (UserAssessmentHistory hanya daftar ringkas tanpa per-soal; Excel/PDF export sudah unconditional via `BuildAnswerCell`). Maka scope 414 = persis 3 file di CONTEXT, tidak perlu diperluas. `[VERIFIED: grep AllowAnswerReview seluruh repo + baca tiap call-site]`

Pola implementasi sudah punya template kuat di repo: `IsResultsAuthorized` dan `IsParticipantRemoved` adalah pure static helper `public static` di `CMPController`, di-unit-test tanpa DB / tanpa `WebApplicationFactory`. Test analog `ResultsAuthorizationTests.cs` (xUnit `[Theory]` + `[InlineData]` matrix) adalah cetakan persis untuk helper baru `CanReviewAnswers`. Lesson 999.12 (hindari replica tautologis) terhormat otomatis karena helper murni. `[VERIFIED: HcPortal.Tests/ResultsAuthorizationTests.cs]`

**Primary recommendation:** Ekstrak `public static bool CanReviewAnswers(bool allowAnswerReview, bool isOwner) => allowAnswerReview || !isOwner;` di `CMPController` (#region Helper Methods, dekat `IsResultsAuthorized` L2526 / `IsParticipantRemoved` L2540). Hitung `bool canReviewAnswers` SEKALI di `Results` tepat setelah auth (setelah `user` & `isAuthorized` diketahui, sebelum branch package), pakai di gate build (L2266) DAN VM assignment (L2379). Tambah `CanReviewAnswers` ke VM. View: gate → `Model.CanReviewAnswers`, alert → `!Model.CanReviewAnswers`, nota admin baru bila `Model.CanReviewAnswers && !Model.AllowAnswerReview`. Test cermin `ResultsAuthorizationTests` (4 InlineData). Build + unit + manual UAT dua-persona.

## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Tambah **field VM baru** `CanReviewAnswers` (bool, nilai efektif). `AllowAnswerReview` di VM **tetap = raw toggle** `assessment.AllowAnswerReview`. Controller mengisi keduanya; gate per-soal di view pindah ke `Model.CanReviewAnswers`.
- **D-02:** **Semua non-owner** di-bypass. Efektif: `CanReviewAnswers = assessment.AllowAnswerReview || (currentUser.Id != assessment.UserId)`. Owner (`currentUser.Id == assessment.UserId`) → hanya lihat saat toggle ON. Aman karena setiap non-owner yang mencapai titik ini SUDAH lolos `IsResultsAuthorized` (hanya Admin/HC/L3/L4-section-scoped) — owner-check tunggal cukup, tak perlu cek role lagi.
- **D-03:** Ekstrak **pure static helper** `CanReviewAnswers(bool allowAnswerReview, bool isOwner)` di `CMPController` (pola identik `IsResultsAuthorized` / `IsParticipantRemoved` — static, no-DB, testable). Body: `return allowAnswerReview || !isOwner;`. Unit test (xUnit, no DB): non-owner+OFF→true, non-owner+ON→true, owner+OFF→false, owner+ON→true. Action `Results` memanggil helper ini untuk gate build `questionReviews` (L2266) **dan** mengisi `viewModel.CanReviewAnswers` (L2379 region). Gate build & VM flag WAJIB pakai sumber yang sama.
- **D-04:** **Tampil review + nota admin.** Saat review tampil hanya karena bypass non-owner (`CanReviewAnswers == true && AllowAnswerReview == false`), tampilkan badge/alert kecil di view: "Peserta tak bisa melihat tinjauan ini (Tinjauan Jawaban OFF)". Owner + OFF → tetap alert lama "Tinjauan jawaban tidak tersedia untuk assessment ini." Kondisi nota **diturunkan di view** dari kombinasi 2 flag (`CanReviewAnswers && !AllowAnswerReview`) — tak butuh field VM ke-3.

### Claude's Discretion
- Teks/ikon/warna persis badge nota admin (info vs secondary), penempatan (header card vs di atas list). Penamaan helper boleh `CanReviewAnswers` atau sinonim selama static+pure.

### Deferred Ideas (OUT OF SCOPE)
- **DEF-01** — Fasilitas grant akses / share URL hasil ke atasan (non-owner) + keterkaitan tinjau jawaban essay → fase/spec terpisah. Catatan: atasan sesama Section SUDAH otomatis berwenang via `IsResultsAuthorized` (L4 section-scoped), jadi setelah 414 mereka langsung bisa lihat review tanpa fasilitas tambahan. JANGAN implementasi di 414.

## Project Constraints (from CLAUDE.md)

- **Bahasa:** Respons user-facing dalam Bahasa Indonesia (string nota admin di view = Bahasa Indonesia).
- **Develop Workflow:** Verifikasi lokal WAJIB sebelum commit — `dotnet build` + `dotnet run` (cek di `http://localhost:5277`). Branch saat ini = `main`. **JANGAN push** (deploy ke Dev/Prod = tanggung jawab IT). Notifikasi IT dengan commit hash + flag migration (414 migration=FALSE).
- **JANGAN edit kode/DB di server Dev/Prod.**
- **Seed Data:** Bila butuh seed untuk UAT (assessment dgn `AllowAnswerReview=false` + responses) → klasifikasi `temporary + local-only`, snapshot DB sebelum insert, catat di `docs/SEED_JOURNAL.md`, restore + tandai `cleaned` setelah selesai. JANGAN biarkan seed temporary nempel.
- **Worktree stale:** Abaikan `.claude/worktrees/pensive-saha-4b1351/` (salinan lama CMPController/VM/test) — kerja HANYA di main tree.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| (none) | Off-theme bugfix, tidak menambah REQ ke v32.5 (11/11). `phase_req_ids = null`. | Verifikasi diarahkan ke Success Criteria ROADMAP §414 (SC-1..SC-4), bukan REQ. |

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Keputusan "boleh lihat review?" (owner vs non-owner) | API / Backend (CMPController static helper) | — | Otorisasi WAJIB server-authoritative; helper pure no-DB agar testable & bebas detail role |
| Build data `questionReviews` (per-soal benar/salah) | API / Backend (CMPController.Results) | DB (PackageQuestion/PackageUserResponse) | Data sensitif jawaban benar; hanya dibangun bila gate lolos |
| Render section + nota admin | Frontend Server (Razor `Results.cshtml`) | — | View hanya baca `Model.*`; tak ada logika role di Razor (pola VM-flag → view-gate) |
| Penetapan flag efektif ke VM | API / Backend (CMPController.Results) | — | Controller satu-satunya yang tahu `currentUser.Id` vs `assessment.UserId` |

## Standard Stack

Tidak ada library baru. Fix murni di kode aplikasi existing.

### Core (existing, tidak berubah versi)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller `Results` + Razor view | Stack aplikasi |
| xUnit | 2.9.3 | Unit test helper baru | Framework test repo `[VERIFIED: HcPortal.Tests.csproj L15]` |
| xunit.runner.visualstudio | 3.0.1 | Test runner | `[VERIFIED: HcPortal.Tests.csproj L16]` |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test SDK | `[VERIFIED: HcPortal.Tests.csproj L14]` |

**Installation:** Tidak ada. `dotnet build` + `dotnet test` cukup.

## Architecture Patterns

### System Architecture Diagram (alur Results page pasca-fix)

```
Admin/HC klik "View Results" (Monitoring / UserAssessmentHistory list)
        │
        ▼
[GET] CMPController.Results(id)
        │  1. Load assessment (+User)              ← L2209-2213
        │  2. (user, roleLevel) = GetCurrentUserRoleLevelAsync()  ← L2216 (impersonation-aware)
        │  3. isAuthorized = IsResultsAuthorized(...)  ← L2218  ─┐ owner ∥ L1-3 ∥ L4-section
        │     if (!isAuthorized) return Forbid();              ─┘ (gerbang utama, tak berubah)
        │  4. status submitted? (else redirect)        ← L2222
        │
        │  ┌──── NEW (D-02/D-03): hitung SEKALI di sini ────┐
        │  │  bool isOwner = (assessment.UserId == user.Id) │
        │  │  bool canReviewAnswers =                       │
        │  │      CanReviewAnswers(assessment.AllowAnswerReview, isOwner)
        │  └────────────────────────────────────────────────┘
        ▼
   packageAssignment != null ?
   ├─ YES (package path)
   │     if (canReviewAnswers)  ← was: if (assessment.AllowAnswerReview)  [L2266]
   │         build questionReviews (per-soal benar/salah/essay)
   │     else  count-only correctCount  [L2329]
   │     viewModel.AllowAnswerReview = assessment.AllowAnswerReview  ← raw, UNCHANGED [L2379]
   │     viewModel.CanReviewAnswers = canReviewAnswers               ← NEW [region L2379]
   │
   └─ NO (legacy/empty path)  [L2391]
         viewModel.AllowAnswerReview = false      ← UNCHANGED (D-01: raw, no package = no review)
         viewModel.CanReviewAnswers = ???         ← lihat Open Question OQ-1 (rekomendasi: false)
        │
        ▼
   return View(viewModel)
        │
        ▼
Views/CMP/Results.cshtml
   @if (Model.CanReviewAnswers && Model.QuestionReviews != null)   ← was Model.AllowAnswerReview [L316]
        render card "Tinjauan Jawaban"
        + NEW nota admin bila (Model.CanReviewAnswers && !Model.AllowAnswerReview)  ← D-04
   else if (!Model.CanReviewAnswers)   ← was !Model.AllowAnswerReview [L413]
        render alert-info "Tinjauan jawaban tidak tersedia"
```

### Component Responsibilities

| File:Line (saat ini) | Tanggung jawab | Edit yang dibutuhkan |
|----------------------|----------------|----------------------|
| `Controllers/CMPController.cs:2207` | Action `Results` entry | (no edit) |
| `Controllers/CMPController.cs:2216-2219` | Resolve user + `IsResultsAuthorized` | Setelah baris ini (post-auth, user/owner diketahui) → tambah hitung `isOwner` + `canReviewAnswers`. Tempatkan SEBELUM `if (packageAssignment != null)` di L2240 agar dipakai kedua branch. |
| `Controllers/CMPController.cs:2266` | Gate build `questionReviews` (`if (assessment.AllowAnswerReview)`) | Ganti kondisi → `if (canReviewAnswers)` |
| `Controllers/CMPController.cs:2379` | VM assign `AllowAnswerReview = assessment.AllowAnswerReview` (package path) | BIARKAN baris ini (raw, D-01). Tambah baris baru `CanReviewAnswers = canReviewAnswers,` di region L2370-2389. |
| `Controllers/CMPController.cs:2403` | VM assign `AllowAnswerReview = false` (legacy path) | BIARKAN (D-01, no package). Tambah `CanReviewAnswers = ...` (lihat OQ-1). |
| `Controllers/CMPController.cs:2526` | `IsResultsAuthorized` (template helper) | (no edit — cetakan) |
| `Controllers/CMPController.cs:2540` | `IsParticipantRemoved` (template helper) | (no edit — cetakan) |
| `Models/AssessmentResultsViewModel.cs:12` | `public bool AllowAnswerReview { get; set; }` | Tambah `public bool CanReviewAnswers { get; set; }` tepat setelah L12 (sebelum `GenerateCertificate` L13). |
| `Views/CMP/Results.cshtml:316` | `@if (Model.AllowAnswerReview && Model.QuestionReviews != null)` | Ganti → `@if (Model.CanReviewAnswers && Model.QuestionReviews != null)`. Tambah nota admin di dalam card (mis. di `card-header` L319-321 atau atas list-group) bila `Model.CanReviewAnswers && !Model.AllowAnswerReview`. |
| `Views/CMP/Results.cshtml:413` | `else if (!Model.AllowAnswerReview)` (alert "tidak tersedia") | Ganti → `else if (!Model.CanReviewAnswers)`. Owner+OFF tetap masuk sini (alert lama). |
| `HcPortal.Tests/ResultsAuthorizationTests.cs` | Template test pure static | (no edit — cetakan; buat file baru `CanReviewAnswersTests.cs`) |

### Pattern 1: Pure static helper untuk keputusan gate/authorize (no-DB)
**What:** Method `public static bool X(args primitif)` di controller, body deterministik tanpa DB/HTTP/IO.
**When to use:** Setiap keputusan otorisasi/gate yang bisa diekspresikan dari primitif → langsung unit-testable tanpa `WebApplicationFactory` (lesson 999.12).
**Example:**
```csharp
// Source: Controllers/CMPController.cs:2526 (IsResultsAuthorized) & :2540 (IsParticipantRemoved) [VERIFIED]
public static bool IsResultsAuthorized(string? ownerUserId, string currentUserId, int roleLevel, string? currentUserSection, string? ownerSection)
{
    if (ownerUserId == currentUserId) return true;          // owner
    if (roleLevel is >= 1 and <= 3) return true;            // Admin/HC/L3
    if (roleLevel == 4 && !string.IsNullOrEmpty(currentUserSection) && ownerSection == currentUserSection) return true;
    return false;
}

public static bool IsParticipantRemoved(AssessmentSession session) => session.RemovedAt != null;

// NEW (D-03) — cetak dari pola di atas:
// allowAnswerReview || !isOwner  → non-owner selalu true; owner ikut toggle.
public static bool CanReviewAnswers(bool allowAnswerReview, bool isOwner) => allowAnswerReview || !isOwner;
```

### Pattern 2: VM-flag → view-gate (tanpa logika role di Razor)
**What:** Controller menghitung flag efektif, View hanya membaca `Model.*`. View tidak pernah meng-query role/owner.
**When to use:** Selalu (sudah konvensi repo). D-04 mempertahankan ini: nota admin diturunkan di view dari **dua flag VM** (`CanReviewAnswers && !AllowAnswerReview`) — tetap zero-logic-role.
**Example markup nota (Claude's Discretion soal teks/warna):**
```cshtml
@* Source: pola alert existing Results.cshtml:415 (alert-info) — visual consistency *@
@if (Model.CanReviewAnswers && !Model.AllowAnswerReview)
{
    <div class="alert alert-info py-2 px-3 mb-3 small">
        <i class="bi bi-eye-slash me-1"></i>Peserta tidak dapat melihat tinjauan ini (Tinjauan Jawaban OFF). Hanya admin/HC yang melihat.
    </div>
}
```

### Anti-Patterns to Avoid
- **Overwrite `Model.AllowAnswerReview` dengan nilai efektif** — DITOLAK di discuss (D-01). View butuh membedakan "toggle OFF tapi admin tetap lihat" untuk nota. JANGAN.
- **Hitung gate dua kali dengan sumber berbeda** — gate build (L2266) dan VM flag (L2379) WAJIB pakai variabel `canReviewAnswers` yang SAMA. Kalau hanya satu diubah → null-data (review null tapi gate true) atau hide tak konsisten. Hitung sekali, pakai dua kali.
- **Cek roleLevel lagi di dalam helper** — tak perlu (D-02/specifics). Titik kode pasca-`IsResultsAuthorized` dijamin hanya non-owner berwenang; owner-check tunggal (`isOwner`) cukup dan menjaga helper pure.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Otorisasi siapa boleh akses Results | Cek role ulang di `Results`/view | `IsResultsAuthorized` (sudah dipanggil L2218) | Gerbang utama; helper baru hanya keputusan review owner-vs-non-owner, BUKAN ulang otorisasi akses |
| Per-soal benar/salah | Logika benar/salah manual | `AssessmentScoreAggregator.IsQuestionCorrect` (dipakai L2286/2336/2357) | Sumber kebenaran terpusat (essay-aware); tak tersentuh fix ini |
| Test harness untuk helper | `WebApplicationFactory` / replica DB | xUnit `[Theory]`+`[InlineData]` (pola `ResultsAuthorizationTests`) | Helper pure no-DB; replica tautologis dilarang (lesson 999.12) |

**Key insight:** Fix ini secara sengaja TIDAK menambah surface keamanan baru — ia hanya melonggarkan gate review **setelah** `IsResultsAuthorized` (gerbang akses) sudah lolos. Tidak ada data baru yang ter-ekspos ke pihak tak berwenang; non-owner yang sampai ke titik ini sudah pasti Admin/HC/L3/L4-section.

## Common Pitfalls

### Pitfall 1: Gate build & VM flag desinkron
**What goes wrong:** Mengubah hanya L2266 (gate build) tapi lupa set `CanReviewAnswers` di VM (atau sebaliknya) → review dibangun tapi view hide-nya, atau view show tapi `QuestionReviews == null`.
**Why it happens:** Dua titik berbeda di action panjang (~280 baris), region VM jauh dari gate.
**How to avoid:** Deklarasi `bool canReviewAnswers` SEKALI sebelum `if (packageAssignment != null)` (post-auth). Gunakan variabel itu di L2266 dan L2379.
**Warning signs:** Review section kosong padahal nota tampil; atau alert "tidak tersedia" muncul untuk admin.

### Pitfall 2: Legacy/empty path (L2391) terlewat
**What goes wrong:** Branch `else` (no package) hardcode `AllowAnswerReview = false` dan tidak set `CanReviewAnswers` → default `false` (aman, tapi planner harus sadar). Kalau salah set `true` → view coba render `QuestionReviews` yang `null` → tidak crash (`&& Model.QuestionReviews != null`) tapi membingungkan.
**Why it happens:** Legacy path dilupakan karena jarang aktif (sesi tanpa package, Phase 227 CLEN-02 empty-state).
**How to avoid:** Lihat OQ-1 — rekomendasi default `CanReviewAnswers = false` di legacy path (tidak ada data per-soal untuk ditinjau, owner-vs-non-owner tak relevan).
**Warning signs:** Sesi tanpa package menampilkan nota admin walau tak ada soal.

### Pitfall 3: Razor view butuh runtime verification (lesson 354)
**What goes wrong:** Build hijau + grep cocok TIDAK menjamin Razor `@if` benar saat render — perubahan kondisi view harus diuji di browser (lesson 354 + lesson v32.5/413 `monFlashRow` ReferenceError).
**Why it happens:** Razor compile error/logic salah sering hanya muncul di runtime, bukan `dotnet build`.
**How to avoid:** UAT browser lokal dua persona (admin non-owner + owner) di `http://localhost:5277` pada assessment `AllowAnswerReview=false`. Bukan grep/build saja.
**Warning signs:** —

### Pitfall 4: `currentUser.Id` saat impersonation
**What goes wrong:** `user.Id` berasal dari `GetCurrentUserRoleLevelAsync` (impersonation-aware, L2500). Saat impersonate X, `user.Id == X.Id`. `isOwner` dihitung terhadap user EFEKTIF — konsisten dengan `IsResultsAuthorized` yang juga pakai `user.Id` efektif (lihat `ResultsAuthorizationTests` baris 25-29).
**Why it happens:** Bisa salah pakai `User` (claims asli) alih-alih `user` (efektif).
**How to avoid:** Pakai variabel `user` yang sudah di-resolve di L2216 (`var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();`). JANGAN panggil `_userManager.GetUserAsync(User)` lagi.
**Warning signs:** Owner-gate salah saat sesi di-impersonate.

## Code Examples

### Verified: action snippet pasca-fix (sketsa, planner finalisasi)
```csharp
// Source: Controllers/CMPController.cs:2216-2219 (existing) + edit baru
var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
if (user == null) return Challenge();
bool isAuthorized = IsResultsAuthorized(assessment.UserId, user.Id, roleLevel, user.Section, assessment.User?.Section);
if (!isAuthorized) return Forbid();

// ... status submitted check (L2222) ...

// NEW (D-02/D-03): hitung sekali, pakai dua kali. Tempatkan sebelum `if (packageAssignment != null)` (L2240).
bool isOwner = assessment.UserId == user.Id;
bool canReviewAnswers = CanReviewAnswers(assessment.AllowAnswerReview, isOwner);

// L2266 gate:  if (canReviewAnswers) { ... build questionReviews ... }
// L2379 region: AllowAnswerReview = assessment.AllowAnswerReview,  // raw (UNCHANGED)
//               CanReviewAnswers = canReviewAnswers,                // NEW
```

### Verified: test cermin (buat `HcPortal.Tests/CanReviewAnswersTests.cs`)
```csharp
// Source: cermin HcPortal.Tests/ResultsAuthorizationTests.cs [VERIFIED]
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class CanReviewAnswersTests
{
    [Theory]
    // allowAnswerReview, isOwner, expected
    [InlineData(false, false, true)]   // non-owner + OFF  -> bypass, lihat review (inti fix)
    [InlineData(true,  false, true)]   // non-owner + ON   -> lihat (tak berubah)
    [InlineData(false, true,  false)]  // owner + OFF       -> tetap diblok (perilaku worker)
    [InlineData(true,  true,  true)]   // owner + ON        -> lihat (tak berubah)
    public void CanReviewAnswers_Matrix(bool allow, bool isOwner, bool expected)
        => Assert.Equal(expected, CMPController.CanReviewAnswers(allow, isOwner));
}
```

## Sibling Surface Audit

ROADMAP §414 minta: "Pertimbangkan jalur sejenis: UserAssessmentHistory + export (cek apakah pola gate sama)." Hasil grep `AllowAnswerReview` SELURUH repo (file `.cs`/`.cshtml`), tiap call-site dibaca:

| File:Line | Sifat read | Gate per-soal? | Surface | Aksi 414 |
|-----------|-----------|----------------|---------|----------|
| `Controllers/CMPController.cs:2266` | gate build `questionReviews` | **YA** | admin+owner (shared) | **FIX (inti)** |
| `Controllers/CMPController.cs:2379` | VM assign (package path) | flag ke view | admin+owner | **FIX (tambah CanReviewAnswers)** |
| `Controllers/CMPController.cs:2403` | VM assign (legacy path = `false`) | flag ke view | empty path | **FIX ringan (OQ-1)** |
| `Views/CMP/Results.cshtml:316` | render gate | **YA** | admin+owner | **FIX (→ CanReviewAnswers)** |
| `Views/CMP/Results.cshtml:413` | render alert | **YA** | admin+owner | **FIX (→ !CanReviewAnswers)** |
| `AssessmentAdminController.cs:147/168/189` | projeksi anonim ke list manajemen | TIDAK (metadata config) | admin list | tidak tersentuh |
| `AssessmentAdminController.cs:690/1250/1286/1465/1849/1958/1979/2104/2209/2333` | WRITE (create/edit/inherit/clone) | TIDAK | admin authoring | tidak tersentuh |
| `TrainingAdminController.cs:1203` | WRITE (`= true`) | TIDAK | admin | tidak tersentuh |
| `InjectAssessmentController.cs:472` / `InjectAssessmentService.cs:152` / `Models/InjectAssessmentDtos.cs:56` / `ViewModels/InjectAssessmentViewModel.cs:20` | WRITE / default Inject | TIDAK | admin inject | tidak tersentuh |
| `Data/ApplicationDbContext.cs:215` | EF default value `true` | TIDAK | schema | tidak tersentuh |
| `Views/Admin/InjectAssessment.cshtml:229` | form checkbox (set toggle) | TIDAK | admin authoring | tidak tersentuh |

**UserAssessmentHistory** (`AssessmentAdminController.cs:5831`, `[Authorize(Roles="Admin,HC")]`): daftar ringkas per-user (membangun `AssessmentReportItem`: Id/Title/Category/Score/PassPercentage/IsPassed/CompletedAt). **TIDAK membaca `AllowAnswerReview`**, **TIDAK** memuat `QuestionReviews` per-soal. Tiap baris hanya menaut ke `CMP/Results` (via `_AssessmentGroupsTab.cshtml:261` `Url.Action("UserAssessmentHistory",...)` → lalu ke Results). **Bukan surface per-soal → tidak butuh fix.** `[VERIFIED: baca L5831-5885]`

**Export Excel** (`Helpers/ExcelExportHelper.cs:93`): membangun kolom jawaban via `AssessmentScoreAggregator.BuildAnswerCell` **tanpa** gate `AllowAnswerReview` — sudah unconditional (admin-facing). **Tidak ada perubahan perilaku diharapkan; tidak butuh fix.** `[VERIFIED: grep ExcelExportHelper.cs]`

**Export/PDF jawaban di AssessmentAdminController** (`BuildAnswerCell`/`AnswerCell` family, `PdfAnswerCellTests`): admin-facing, tidak gate by toggle. **Tidak butuh fix.**

**Kesimpulan:** Scope 414 = persis 3 file di CONTEXT (CMPController.Results, AssessmentResultsViewModel, Results.cshtml) + 1 file test baru. **TIDAK ada surface sibling yang perlu dimasukkan ke 414.** DEF-01 (share-URL ke atasan lintas-section) tetap deferred. `[VERIFIED: audit penuh semua call-site AllowAnswerReview]`

## Runtime State Inventory

> Bukan rename/migration murni, tapi ROADMAP minta audit sibling-surface. Inventory di-isi untuk kelengkapan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — `CanReviewAnswers` adalah field VM in-memory (tidak dipersist; bukan kolom DB). migration=FALSE. | none |
| Live service config | None — tidak ada konfigurasi eksternal yang menyimpan string ini. | none |
| OS-registered state | None — tidak ada task/service teregistrasi. | none |
| Secrets/env vars | None. | none |
| Build artifacts | `HcPortal.Tests/obj/` ada (build cache). Test baru akan recompile otomatis via `dotnet test`. Worktree stale `.claude/worktrees/pensive-saha-4b1351/` punya salinan lama CMPController/VM/test — **JANGAN edit di sana.** | Abaikan worktree stale; kerja di main tree. |

## Validation Architecture

> nyquist_validation aktif (tidak di-set false). Section disertakan.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (`HcPortal.Tests`) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` |
| Quick run command | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~CanReviewAnswers"` |
| Full suite command | `dotnet test HcPortal.Tests` |

### Success Criterion → Test Map
| SC | Behavior | Test Type | Automated Command | File Exists? |
|----|----------|-----------|-------------------|-------------|
| SC-1 | Admin/HC (non-owner) + `AllowAnswerReview=false` → review per-soal TAMPIL | unit (helper) + manual UAT (render+data) | `dotnet test --filter "FullyQualifiedName~CanReviewAnswers"` (kasus `false,false→true`) | ❌ Wave 0 (buat `CanReviewAnswersTests.cs`) |
| SC-2 | Owner (peserta) + `AllowAnswerReview=false` → tetap diblok (alert lama) | unit (helper) + manual UAT | `dotnet test --filter "...CanReviewAnswers"` (kasus `false,true→false`) | ❌ Wave 0 |
| SC-3 | `AllowAnswerReview=true` → admin & owner tidak berubah (regresi nol) | unit (helper) + regresi e2e existing | `dotnet test --filter "...CanReviewAnswers"` (kasus `true,*→true`) + `npx playwright test exam-types.spec.ts -g "FLOW S"` + `-g "FLOW N"` | unit ❌ Wave 0; e2e ✅ ada (FLOW N/S) |
| SC-4 | Build hijau + suite hijau + run lokal OK | build + suite + manual | `dotnet build` && `dotnet test HcPortal.Tests` && `dotnet run` (cek :5277) | ✅ infra ada |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests --filter "FullyQualifiedName~CanReviewAnswers"` (helper) + `dotnet build`.
- **Per wave merge / phase gate:** `dotnet test HcPortal.Tests` (full unit suite — baseline v32.5 = 605/605, target tetap hijau + 4 test baru).
- **Regresi view (lesson 354):** UAT browser lokal dua-persona WAJIB (lihat di bawah) — bukan hanya unit.

### Unit-coverable vs Integration/UAT
- **Unit-coverable (helper, deterministik):** Logika owner-vs-non-owner × toggle (4 InlineData). Inilah inti SC-1/SC-2/SC-3 dari sisi keputusan. Tidak butuh DB/WebApplicationFactory (lesson 999.12).
- **Butuh manual UAT browser (lesson 354 — Razor runtime):** Render section "Tinjauan Jawaban" benar untuk admin saat OFF (data per-soal benar/salah muncul); nota admin tampil hanya saat `CanReviewAnswers && !AllowAnswerReview`; owner+OFF lihat alert lama; toggle ON tidak berubah. Razor `@if` tidak terjamin oleh build/grep.
- **Regresi e2e existing:** `exam-types.spec.ts` FLOW N (`AllowAnswerReview=false` negative) + FLOW S (true-vs-false paired) menguji surface owner/worker. **PERHATIAN:** FLOW N/S dijalankan sebagai worker (owner) → setelah fix mereka tetap HIJAU (owner-path tak berubah, D-02). Bila e2e dijalankan sebagai admin non-owner, asersi `.alert-info` "tidak tersedia" pada `AllowAnswerReview=false` akan GAGAL (kini admin lihat review) — pastikan tes itu owner-context. `[VERIFIED: exam-types.spec.ts L588-596 N1 "HC creates ... false"; persona pengambil = worker]` — planner verifikasi persona e2e agar tidak salah-merah.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/CanReviewAnswersTests.cs` — 4 InlineData cermin `ResultsAuthorizationTests.cs` (covers SC-1/2/3 sisi keputusan).
- [ ] Seed lokal temporary (UAT): 1 assessment package `AllowAnswerReview=false` + responses peserta, untuk UAT dua-persona. Klasifikasi `temporary+local-only`, snapshot+restore (CLAUDE.md SEED_WORKFLOW). *(opsional bila sudah ada sesi lama OFF di DB lokal)*

*(Infra test framework sudah ada; tidak perlu install.)*

## Security Domain

> `security_enforcement` tidak di-set false → section disertakan. Fix ini melonggarkan gate, jadi analisis keamanan relevan.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | tidak diubah (`Challenge()` existing L2217) |
| V3 Session Management | no | tidak diubah |
| V4 Access Control | **yes** | `IsResultsAuthorized` (L2218 gerbang utama) **tetap** dipanggil sebelum helper baru. `CanReviewAnswers` hanya mengatur tampilan review **setelah** akses lolos — TIDAK melonggarkan siapa yang boleh membuka Results. |
| V5 Input Validation | no | tidak ada input baru (helper terima bool) |
| V6 Cryptography | no | — |

### Known Threat Patterns
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| IDOR — non-owner buka Results peserta lain tanpa wewenang | Information Disclosure | `IsResultsAuthorized` (L2218) **tetap** gerbang; `CanReviewAnswers` TIDAK bypass otorisasi akses, hanya gate review pasca-akses. Threat tetap ter-mitigasi penuh. |
| Owner-bypass salah (owner lihat review saat OFF) | Tampering of intended behavior | Helper `allowAnswerReview \|\| !isOwner` → owner+OFF = `false` (terverifikasi unit test SC-2). Worker-facing behavior tak berubah. |
| Impersonation salah-owner | Spoofing | `user.Id` efektif (impersonation-aware, L2500); `isOwner` konsisten dgn `IsResultsAuthorized` (lihat `ResultsAuthorizationTests` L25-29). |

**Verdict keamanan:** Net-neutral. Tidak ada surface baru ter-ekspos. Non-owner yang melihat review tambahan SUDAH berwenang membuka Results (Admin/HC/L3/L4-section) sebelum fix.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| (kosong) | Semua klaim diverifikasi langsung dari kode working tree atau dari struktur test repo. | — | — |

**Tabel kosong:** Semua klaim VERIFIED — tidak ada yang `[ASSUMED]`. Tidak butuh konfirmasi user tambahan (keputusan desain sudah di-lock di CONTEXT D-01..D-04).

## Open Questions

1. **Legacy/empty path (L2391-2414): set `CanReviewAnswers` ke apa?**
   - What we know: Branch ini aktif untuk sesi tanpa package (Phase 227 CLEN-02 empty-state). `AllowAnswerReview` hardcode `false`, `QuestionReviews = null`, `TotalQuestions = 0`. Tidak ada data per-soal.
   - What's unclear: Apakah owner-aware (`CanReviewAnswers(false, isOwner)` → non-owner true) atau hardcode `false`.
   - Recommendation: **Set `CanReviewAnswers = false`** (hardcode). Rasional: tidak ada `QuestionReviews` untuk ditampilkan (`null`), jadi gate view `Model.CanReviewAnswers && Model.QuestionReviews != null` tetap `false` apa pun nilainya; tapi `false` mencegah nota admin (`CanReviewAnswers && !AllowAnswerReview`) muncul keliru pada sesi kosong, dan alert "tidak tersedia" (`!CanReviewAnswers`) konsisten dgn empty-state. CONTEXT §code_context L64 menyerahkan ke planner ("default ikut helper") — namun karena owner-vs-non-owner tak relevan saat tak ada data, hardcode `false` lebih bersih. Planner putuskan; keduanya tidak meng-ekspos data (tak ada data).

2. **Persona e2e FLOW N/S — owner atau admin?**
   - What we know: `exam-types.spec.ts` FLOW N (L588) menguji `AllowAnswerReview=false` negative (`.alert-info` tampil). FLOW S paired true/false.
   - What's unclear: Apakah asersi negatif dijalankan dari konteks pengambil ujian (owner) atau admin.
   - Recommendation: Planner konfirmasi di Wave verify bahwa asersi `.alert-info`/`tidak tersedia` dijalankan sebagai **owner** (worker yang ambil ujian). Jika ya → tetap HIJAU pasca-fix (owner-path tak berubah). Jika ada asersi dari admin → update tes itu (kini admin lihat review). `[VERIFIED: N1 = "HC creates assessment with AllowAnswerReview=false"; pengambil = worker context]` — kemungkinan besar aman, tapi verifikasi.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | build/test/run | ✓ (asumsi, project net8.0 aktif) | net8.0 | — |
| SQL Server lokal | UAT browser (DB lokal) | ✓ (workflow existing) | — | — |
| Playwright (Node) | regresi e2e FLOW N/S (opsional) | ✓ (suite e2e ada) | — | manual UAT (cukup untuk SC) |

**Catatan:** Fix murni kode + view + unit test → tidak ada dependensi eksternal baru. UAT browser lokal `http://localhost:5277` per CLAUDE.md Develop Workflow.

## State of the Art

Tidak ada perubahan teknologi/best-practice yang relevan. Pola pure-static-helper + VM-flag→view-gate adalah konvensi internal repo yang stabil (dipakai sejak Phase 346 REC-04 / 409 PRMV-03). Tidak ada deprecated.

## Sources

### Primary (HIGH confidence) — kode working tree (diverifikasi langsung)
- `Controllers/CMPController.cs` — Results action L2206-2489 (gate L2266, VM L2379/L2403, helper IsResultsAuthorized L2526, IsParticipantRemoved L2540, GetCurrentUserRoleLevelAsync L2500)
- `Models/AssessmentResultsViewModel.cs` — properties L1-22 (AllowAnswerReview L12)
- `Views/CMP/Results.cshtml` — gate L316, alert L413-417
- `HcPortal.Tests/ResultsAuthorizationTests.cs` — pola test pure static (cetakan)
- `HcPortal.Tests/HcPortal.Tests.csproj` — versi framework test
- `Controllers/AssessmentAdminController.cs` — UserAssessmentHistory L5831 (bukan surface per-soal), call-site AllowAnswerReview (semua WRITE/projeksi)
- `Helpers/ExcelExportHelper.cs` — export unconditional via BuildAnswerCell
- Grep `AllowAnswerReview` seluruh repo (audit sibling-surface lengkap)
- `.planning/phases/414-.../414-CONTEXT.md` — D-01..D-04 locked
- `.planning/ROADMAP.md` §414 (SC-1..SC-4)

### Secondary (MEDIUM)
- `.planning/milestones/v16.0-phases/318-.../318-RESEARCH.md` — referensi historis FLOW N/S Razor branch (line numbers lama, konteks regresi)

### Tertiary (LOW)
- (none)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tidak ada library baru; versi test diverifikasi dari csproj.
- Architecture (lokasi edit, helper, VM, view): HIGH — tiap baris dibaca langsung; line numbers dikonfirmasi (sedikit bergeser dari CONTEXT: Results L2207, gate L2266, VM L2379, helper L2526/L2540 — semua sesuai CONTEXT ±0-2 baris).
- Sibling-surface audit: HIGH — grep menyeluruh + tiap call-site dibaca; UserAssessmentHistory & export dikonfirmasi bukan surface per-soal.
- Pitfalls: HIGH — diturunkan dari struktur kode aktual + lesson terdokumentasi (354, 999.12, 413 monFlashRow).
- Test infra: HIGH — `ResultsAuthorizationTests` adalah analog persis; project path & pola dikonfirmasi.
- Open questions: 2 (keduanya non-blocking, rekomendasi disertakan).

**Research date:** 2026-06-22
**Valid until:** 2026-07-22 (stabil — kode internal; selama Results action / VM / view tidak refactor besar)
