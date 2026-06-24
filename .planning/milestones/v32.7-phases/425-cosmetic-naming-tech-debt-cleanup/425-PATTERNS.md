# Phase 425: Cosmetic / Naming / Tech-Debt Cleanup - Pattern Map

**Mapped:** 2026-06-24
**Files analyzed:** 6 in-scope (2 NEW helper/test, 4 MODIFY) + label-only cshtml/model edits
**Analogs found:** 6 / 6 (semua punya analog exact/role-match di repo)

> Fase TERAKHIR v32.7, low-risk. Tema utama: **konvergensi ke aset existing** (pure-helper pattern 422/423/424) + dokumentasi akurat. Bukan bangun arsitektur baru. RESEARCH.md sudah memuat excerpt before/after konkret per CLN — PATTERNS ini memetakan tiap file ke analog kanonik yang harus disalin polanya.

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Helpers/ManualEntryRules.cs` (NEW, CLN-02) | utility (pure rule) | transform | `Helpers/ExamTimeRules.cs` / `Helpers/CertIssuanceRules.cs` | exact |
| `Helpers/ControllerGuards.cs` (NEW?, CLN-05) | utility (pure guard) | transform | `Helpers/CertIssuanceRules.cs` (pola static EF-free) | role-match |
| `Controllers/CMPController.cs` (MODIFY, CLN-04) | controller | request-response | `CMPController.cs:469` (424 GRDF-05 sudah wire `ExamTimeRules`) | exact (self) |
| `Controllers/TrainingAdminController.cs` (MODIFY, CLN-02) | controller | request-response | `TrainingAdminController.cs:725-798` (ModelState/TempData same action) | exact (self) |
| `Models/AssessmentSession.cs` (MODIFY, CLN-01/CLN-03) | model | n/a (POCO doc) | XML-doc/komentar field existing di file yang sama | exact (self) |
| `HcPortal.Tests/ExamTimeRulesTests.cs` (EXTEND, CLN-04) | test | n/a (pure parity) | `ExamTimeRulesTests.cs` 3×[Fact] existing | exact (self) |

---

## Pattern Assignments

### `Helpers/ManualEntryRules.cs` (NEW — utility/pure rule, CLN-02)

**Analog:** `Helpers/CertIssuanceRules.cs` (paling dekat — pure EF-free rule yang menerima POCO/scalar, dipakai dari beberapa site, di-unit-test tanpa DbContext). Sekunder: `Helpers/ExamTimeRules.cs` (bentuk paling minimal).

**Why extract:** RESEARCH §Wave-0 Rec — ekstrak logika cross-validate ke pure helper kecil agar testable tanpa DB + reusable bila Edit manual ikut (Open Question #1). Selaras pola 422/423/424.

**Imports/namespace + class header pattern** (salin dari `CertIssuanceRules.cs:1-13`):
```csharp
using System;
using HcPortal.Models;          // hanya bila menerima POCO; untuk scalar-only cukup tanpa ini

namespace HcPortal.Helpers
{
    /// <summary>
    /// v32.7 Phase 425 (CLN-02 / FLD-5.2-04, FLD-5.2-05) — cross-validation entry manual.
    /// Pure EF-free supaya bisa di-unit-test tanpa DbContext/controller. Analog
    /// <see cref="CertIssuanceRules"/> (fase 423) &amp; <see cref="ExamTimeRules"/> (fase 424).
    /// </summary>
    public static class ManualEntryRules
    {
```

**Core pure-predicate pattern** (salin bentuk static-bool dari `CertIssuanceRules.ShouldIssueCertificate` :15-18; signature scalar dari RESEARCH §Wave-0):
```csharp
        // CLN-02 — true bila status Lulus/Tidak-Lulus TIDAK selaras dgn (Score >= PassPercentage).
        // Score nullable: null => tidak ada basis cross-validate => return false (skip warning).
        public static bool PassStatusMismatch(int? score, int passPercentage, bool isPassed)
            => score.HasValue && (score.Value >= passPercentage) != isPassed;
    }
}
```

**Kontras penting (anti-pattern):** JANGAN sertakan side-effect / ModelState / DbContext di helper — itu tetap di controller (warning di-set di `TrainingAdminController`, lihat di bawah). Helper hanya menjawab "mismatch?". Lihat RESEARCH Anti-Pattern: CLN-02 via `ModelState.AddModelError` memblokir simpan → dilarang D-05.

---

### `Controllers/TrainingAdminController.cs` (MODIFY — controller/request-response, CLN-02)

**Analog:** action yang sama (`AddManualAssessment` POST `:689`) — pola ModelState-error (blocking) vs `TempData[...]` (non-blocking) sudah ada di action ini. Untuk **non-blocking warning**, analog kanonik = baris `TempData["Success"]` `:798` (set TempData lalu lanjut, bukan `return View`).

**Titik sisip (verified):** SETELAH gate `if (!ModelState.IsValid) { ... return View(model); }` (`:725-730`) dan SEBELUM/atau bersama loop build session (`:747`). Schedule/CompletedAt sudah selaras (`Schedule = model.CompletedAt`, `:762`) — CLN-02 align bagian ini sudah ter-mirror.

**Existing blocking pattern (JANGAN tiru untuk warning)** (`:725-730`):
```csharp
if (!ModelState.IsValid)
{
    await PopulateWorkersViewBag();
    await SetTrainingCategoryViewBag();
    return View(model);      // ← BLOKIR. Warning CLN-02 TIDAK boleh lewat jalur ini.
}
```

**Non-blocking warning pattern to ADD** (gunakan helper baru + analog `TempData` `:798`, NO `return`):
```csharp
// CLN-02 (D-05): cross-validate non-blocking. TETAP simpan (override sengaja HC), TIDAK auto-override.
if (model.WorkerCerts != null &&
    ManualEntryRules.PassStatusMismatch(model.Score, model.PassPercentage, model.IsPassed))
{
    TempData["Warning"] = model.IsPassed
        ? $"Ditandai Lulus walau Score {model.Score} < Pass {model.PassPercentage}%. Tersimpan apa adanya (override HC)."
        : $"Ditandai Tidak Lulus walau Score {model.Score} >= Pass {model.PassPercentage}%. Tersimpan apa adanya.";
    // TIDAK return — lanjut ke loop build session + SaveChanges (analog TempData["Success"] :798).
}
```

**Security guard yang WAJIB tetap utuh** (`:686-688`) — CLN-02 tidak boleh melemahkan:
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> AddManualAssessment(CreateManualAssessmentViewModel model)
```

**XSS note:** pesan warning hanya pakai nilai numerik (`Score`, `PassPercentage`) + teks statis — Razor auto-encode. JANGAN `Html.Raw`, JANGAN echo string input mentah.

---

### `Controllers/CMPController.cs` (MODIFY — controller/request-response, CLN-04)

**Analog:** `CMPController.cs:469` — **Phase 424 GRDF-05 sudah mewire situs ini ke `ExamTimeRules.AllowedExamSeconds`** (lihat docstring `ExamTimeRules.cs:6-8`: "menyatukan situs yang sudah benar... dan MEMPERBAIKI :469"). 4 situs sisa tinggal MENGIKUTI pola pemanggilan yang sama.

**Pattern to copy (call-site shape):** ganti formula inline `(DurationMinutes + (ExtraTimeMinutes ?? 0)) * 60` → panggilan helper. Situs verified `:1191`, `:1564`, `:1642` (detik, `int`):
```csharp
// SEBELUM (3 situs detik):
//   var allowed = (assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60;
// SESUDAH:
var allowed = ExamTimeRules.AllowedExamSeconds(assessment.DurationMinutes, assessment.ExtraTimeMinutes);
```

Situs `:4661` (menit → double) — jaga parity tipe (RESEARCH Pitfall #1):
```csharp
// SEBELUM:
//   int allowedMinutes = assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0);
//   double allowedSec  = allowedMinutes * 60.0;
// SESUDAH (int → implicit double; numerik identik; hapus allowedMinutes bila jadi unused):
double allowedSec = ExamTimeRules.AllowedExamSeconds(assessment.DurationMinutes, assessment.ExtraTimeMinutes);
```

**Hard constraint (D-03):** JANGAN sentuh token gate `TempData.Peek` (FLOW-08) atau side-effect GET StartExam (FLOW-10). Hanya konsolidasi formula timer. No migration, no behavior-change.

---

### `Models/AssessmentSession.cs` (MODIFY — model/POCO doc, CLN-01 + CLN-03)

**Analog:** XML-doc/komentar field yang sudah ada di file yang sama (mis. style `/// <summary>` pada field existing). RESEARCH §Code Examples sudah memuat teks before/after persis — salin verbatim. Ringkas:

**CLN-03 RESERVED marker** (`:177-180`, salin teks RESEARCH §"CLN-03 RESERVED XML-doc"):
```csharp
/// <summary>
/// RESERVED — tidak dipakai. Dideklarasikan di v14 (AddAssessmentV14Columns) untuk konsep
/// 'Phase1'/'Phase2' yang TIDAK PERNAH diimplementasikan. Linking Pre/Post nyata bertumpu pada
/// AssessmentType + LinkedGroupId + LinkedSessionId. 0 referensi di app (FLOW-06). Dipertahankan
/// di skema (kolom nullable, aman) untuk hindari migration destruktif. Jangan baca/tulis.
/// </summary>
public string? AssessmentPhase { get; set; }
```

**CLN-01 Status komentar 7-nilai** (`:20`), **ValidUntil `[Display]`** (`:84`, analog `[Display(Name=...)]` POCO), **LinkedSessionId XML-doc koreksi PA-04** (`:188-192`) — semua excerpt before/after ada di RESEARCH §Code Examples. Plus `Models/UserPackageAssignment.cs:17-18` sentinel `AssessmentPackageId` (PA-05). Murni komentar/`[Display]`/XML-doc; TANPA ubah perilaku/skema/binding.

**Constraint (CLN-03/D-01):** RESERVED via XML-doc saja — JANGAN drop kolom, JANGAN generate migration. migration=FALSE.

---

### `HcPortal.Tests/ExamTimeRulesTests.cs` (EXTEND — test/pure parity, CLN-04)

**Analog:** file yang sama — 3×`[Fact]` existing (`:9-19`) adalah template parity persis. EXTEND, bukan rewrite.

**Existing pattern (salin bentuk):**
```csharp
[Fact]
public void WithExtraTime_AddsBeforeMultiply() =>
    Assert.Equal(900, ExamTimeRules.AllowedExamSeconds(10, 5));   // (10+5)*60
```

**To ADD (parity 4 situs, RESEARCH Test Map):** `[Theory]`/`[Fact]` membuktikan output helper == formula lama untuk `(d,e)` representatif: `(10,5)=900`, `(10,null)=600`, `(0,0)=0`, `(60,15)=4500`:
```csharp
[Theory]
[InlineData(10, 5, 900)]
[InlineData(10, null, 600)]
[InlineData(0, 0, 0)]
[InlineData(60, 15, 4500)]
public void Parity_AllTimerSites(int duration, int? extra, int expected) =>
    Assert.Equal(expected, ExamTimeRules.AllowedExamSeconds(duration, extra));
```

**Baseline:** suite post-424 = 748/0/2. CLN-04 harus jaga 0 regresi.

---

## Shared Patterns

### Pure-helper (EF-free static rule + xUnit parity)
**Source:** `Helpers/ExamTimeRules.cs`, `Helpers/CertIssuanceRules.cs`, `Helpers/PrePostPairing.cs` (pola 422/423/424)
**Apply to:** `ManualEntryRules.cs` (CLN-02), `ControllerGuards.cs` (CLN-05, bila dibuat), parity test CLN-04
```csharp
// Bentuk kanonik: namespace HcPortal.Helpers; XML-doc 1-paragraf yang menyebut fase+REQ+analog;
// public static class; method static pure (input scalar/POCO → output bool/value); NO DbContext/IO.
```

### Non-blocking server warning (TempData) vs blocking (ModelState)
**Source:** `TrainingAdminController.cs:725-730` (blocking) vs `:798` (`TempData["Success"]`, non-blocking)
**Apply to:** CLN-02 warning (gunakan `TempData["Warning"]` + NO `return`)
**Precedent:** Phase 421 RTH-05/VAL-06 (`MaxAttempts < terpakai` → warning non-blocking).
```csharp
// Blocking  → ModelState.AddModelError(...) + return View(model)   (SAMPAH untuk CLN-02)
// Warning   → TempData["Warning"] = "..."; /* tanpa return, lanjut SaveChanges */
```

### Security guards yang harus tetap utuh (regresi-watch)
**Source:** `TrainingAdminController.cs:686-688`
**Apply to:** semua MODIFY di controller — verifikasi atribut tetap ada pasca-edit
```csharp
[ValidateAntiForgeryToken]                 // CSRF — jangan hapus
[Authorize(Roles = "Admin, HC")]           // authz — pertahankan
// FileUploadHelper.ValidateCertificateFile :704 — jangan sentuh
```

### CLN-05 guard-helper (opsional, minimal)
**Source pola:** `Helpers/CertIssuanceRules.cs` (static EF-free). Call-site target: ~52× `Json(new {success=false, message=...})` di `AssessmentAdminController.cs`.
**Apply to:** SUBSET representatif (1-2 action cluster) — JANGAN sweep semua (RESEARCH OQ #3, D-04 "minimal").
```csharp
// Helper hanya MEMBUNGKUS pembuatan objek respons — shape JSON {success, message} WAJIB byte-identik.
public static IActionResult JsonFail(this Controller c, string message)
    => new JsonResult(new { success = false, message });
// TANPA ubah signature action, TANPA introduksi [ApiController]/DTO ber-anotasi (melanggar D-04).
```

---

## No Analog Found

Tidak ada. Semua 6 file in-scope punya analog exact atau role-match di repo. Fase 425 murni konvergensi ke aset existing — tidak ada role/data-flow baru yang belum pernah ada di codebase.

---

## Metadata

**Analog search scope:** `Helpers/` (ExamTimeRules, CertIssuanceRules, PrePostPairing), `Controllers/` (CMPController, TrainingAdminController, AssessmentAdminController), `Models/` (AssessmentSession, UserPackageAssignment), `HcPortal.Tests/` (ExamTimeRulesTests).
**Files scanned this session:** ExamTimeRules.cs, CertIssuanceRules.cs, PrePostPairing.cs, ExamTimeRulesTests.cs, CMPController.cs, TrainingAdminController.cs (AddManualAssessment), AssessmentSession.cs, plus CONTEXT/RESEARCH 425.
**Pattern extraction date:** 2026-06-24
**Caveat (RESEARCH Pitfall #4):** line numbers bisa drift di branch ITHandoff — executor WAJIB re-grep simbol (`AssessmentPhase`, `AllowedExamSeconds`, `AddManualAssessment`, label teks) sebelum edit, JANGAN percaya line buta.
