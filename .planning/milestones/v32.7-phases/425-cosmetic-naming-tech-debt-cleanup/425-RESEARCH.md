# Phase 425: Cosmetic / Naming / Tech-Debt Cleanup - Research

**Researched:** 2026-06-24
**Domain:** ASP.NET Core 8 MVC tech-debt cleanup — naming/doc alignment, pure-helper consolidation, server-side cross-validation (non-blocking), ModelState guard convention. Zero schema change.
**Confidence:** HIGH (semua klaim diverifikasi langsung di codebase sesi ini)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (D-01..D-05 — AUTHORITATIVE)
- **D-01 (CLN-03 / FLOW-06):** Kolom `AssessmentPhase` (`Models/AssessmentSession.cs:180`, 0 referensi app) **ditandai RESERVED via XML-doc**, **JANGAN drop**. **migration=FALSE.** Alternatif DROP ditolak.
- **D-02 (CLN-04 / FLOW-09):** Konsolidasi formula `(DurationMinutes + (ExtraTimeMinutes ?? 0)) * 60` ke `ExamTimeRules.AllowedExamSeconds` (helper Phase 424). Situs: `CMPController.cs:1191, :1564, :1642` (detik) + `:4661` (menit — `AllowedExamSeconds(...)/60` atau helper menit). **PARITAS WAJIB** — hasil numerik identik. No migration, no behavior-change.
- **D-03 (CLN-04 — DEFER):** Token gate TempData.Peek (FLOW-08, kolom `TokenVerifiedAt` server-authoritative) + write-on-GET StartExam (FLOW-10) **TIDAK dikerjakan di 425** → backlog. JANGAN kerjakan.
- **D-04 (CLN-05 / VAL-07):** Rapikan pola validasi ModelState berulang via **guard-helper bersama minimal (helper static)**, **TANPA mengubah signature action** (tidak refactor param scalar → DTO ber-anotasi). Refactor DTO penuh ditolak.
- **D-05 (CLN-02 / FLD-5.2-04, FLD-5.2-05):** Di `TrainingAdminController.AddManualAssessment` POST (`:689`) + `CreateManualAssessmentViewModel`: selaraskan Schedule/CompletedAt + validasi-silang `IsPassed` vs (`Score >= PassPercentage`). Mismatch → **PERINGATAN server-side (TempData/ModelState warning), TETAP SIMPAN, TIDAK auto-override, TIDAK blokir.** Blokir ditolak.

### Claude's Discretion
- **CLN-01 (kosmetik):** teks persis label `ValidUntil` (FLD-5.2-06), komentar `Status` 7-nilai (FLOW-05), nama/komentar field sentinel `AssessmentPackageId` (PA-05), doc FK `LinkedSessionId` (PA-04). Murni label/komentar/XML-doc, TANPA ubah perilaku/skema.
- Nama & lokasi guard-helper CLN-05; bentuk peringatan CLN-02; teks XML-doc RESERVED CLN-03.

### Deferred Ideas (OUT OF SCOPE)
- **DROP kolom AssessmentPhase** (migration) — ditolak untuk 425 (RESERVED dipilih).
- **FLOW-08 token server-authoritative** (`TokenVerifiedAt`) — backlog (butuh migration + ubah-perilaku).
- **FLOW-10 write-on-GET StartExam** refactor (Upcoming→Open) — backlog (dimitigasi impersonation guard).
- **Refactor DTO penuh ModelState** (VAL-07 versi besar) — backlog.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CLN-01 | Label & doc diselaraskan — label ValidUntil, komentar Status 7-nilai, sentinel AssessmentPackageId, doc FK LinkedSessionId. [FLD-5.2-06, FLOW-05, PA-05, PA-04] | Semua 4 target lokasi diverifikasi (lihat Integration Points). Pure text edit. |
| CLN-02 | Entry manual — Schedule/CompletedAt diselaraskan + cross-validate IsPassed vs Score/PassPercentage (warning, non-blocking, tidak auto-override). [FLD-5.2-04, FLD-5.2-05] | POST `:689` + VM diverifikasi; titik sisip warning teridentifikasi (setelah `ModelState.IsValid`, sebelum SaveChanges). Score nullable → handle. |
| CLN-03 | Kolom dead-field AssessmentPhase ditandai RESERVED di XML-doc (TIDAK drop). [FLOW-06] | `:180` diverifikasi; **0 referensi** di Controllers/Services/Views (grep konfirmasi). migration=FALSE. |
| CLN-04 | Tech-debt timing — timer satu sumber (helper). Token + write-on-GET = DEFER. [FLOW-09 do; FLOW-08/FLOW-10 defer] | 4 situs timer diverifikasi akurat; `ExamTimeRules.AllowedExamSeconds` sudah ada (Phase 424). Parity test mirror `ExamTimeRulesTests`. |
| CLN-05 | Konvensi ModelState dirapikan via guard-helper static minimal (tanpa ubah signature). [VAL-07] | Pola `if(cond) return Json(new{success=false,message=...})` berulang ~52× di AssessmentAdminController; helper minimal aman. |
</phase_requirements>

## Summary

Phase 425 adalah fase **terakhir & paling low-risk** milestone v32.7 — batch cleanup non-fungsional. Lima requirement: empat di antaranya murni **text/comment/XML-doc** (CLN-01) atau **konsolidasi formula yang terbukti identik secara numerik** (CLN-04), satu menambah **warning server-side non-blocking** (CLN-02), satu **menyeragamkan pola guard** tanpa mengubah signature (CLN-05). **migration=FALSE** — sudah diresolusi oleh CONTEXT (AssessmentPhase RESERVED bukan drop; tidak ada kolom/write DB baru).

Semua lokasi file:line yang disebut CONTEXT **diverifikasi akurat sesi ini** (`AssessmentSession.cs:180`, 4 situs timer `CMPController.cs:1191/:1564/:1642/:4661`, `TrainingAdminController.AddManualAssessment` POST `:689`). Beberapa referensi di **dokumen audit lebih lama** sudah bergeser line (mis. PA-05 `:1107-1109`→`:1087-1093`, FLD-5.2-06 label `:611`→`:637`) — wajar karena edit antar-fase 420-424; CONTEXT 425 memakai penomoran terbaru yang benar. Helper `ExamTimeRules.AllowedExamSeconds(durationMinutes, extraTimeMinutes)` sudah tersedia dari Phase 424 dengan signature persis seperti yang dibutuhkan CLN-04.

**Primary recommendation:** Ikuti pola pure-helper + xUnit parity-test yang sudah established (CertIssuanceRules/ShuffleToggleRules/ExamTimeRules dari 422/423/424). Untuk CLN-04, ganti 4 situs ke `ExamTimeRules.AllowedExamSeconds(...)` dengan parity test eksplisit (mirror `ExamTimeRulesTests.cs`); jaga tipe (`int` vs `double`) agar numerik identik. Untuk CLN-02, sisipkan warning via `TempData["Warning"]` SETELAH `ModelState.IsValid` lolos (jangan masuk `ModelState` error — itu akan blokir submit). Untuk CLN-01/CLN-03, edit komentar/XML-doc saja. Untuk CLN-05, ekstrak helper static minimal (mis. `Helpers/ControllerGuards.cs` `JsonFail(message)`) dan terapkan selektif tanpa menyentuh signature action.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| CLN-01 label/komentar/XML-doc | Model (POCO) + View (cshtml label) | — | `[Display]`/komentar di `AssessmentSession`/`UserPackageAssignment`; label teks di `*.cshtml`. Tidak ada logika. |
| CLN-02 cross-validation warning | API/Backend (controller action) | View (render TempData warning) | Otoritas validasi = server (`TrainingAdminController` POST). Filosofi v32.x: server-authoritative, non-blocking. |
| CLN-03 RESERVED marker | Model (POCO) | — | Pure XML-doc di `AssessmentSession.cs:180`. Tidak ada perilaku. |
| CLN-04 timer consolidation | API/Backend (controller) + Helper (pure rule) | — | Formula durasi = pure helper EF-free (`ExamTimeRules`), dipanggil dari `CMPController`. |
| CLN-05 ModelState guard | API/Backend (controller) + Helper (pure) | — | Guard static EF-free dipanggil dari action; tidak mengubah binding/signature. |

**Catatan tier:** TIDAK ada kapabilitas Phase 425 yang menyentuh Database tier (no migration, no new write), Browser/Client tier (no JS behavior change), atau CDN/Static. Ini sepenuhnya cleanup tier Backend + Model + View-label.

## Standard Stack

Phase ini TIDAK memperkenalkan dependency baru. Memakai stack & pola yang sudah ada di repo.

### Core (existing — diverifikasi)
| Library / Asset | Version | Purpose | Why Standard |
|-----------------|---------|---------|--------------|
| .NET SDK | 8.0.418 | Build/run | Target `net8.0` di `HcPortal.csproj` + `HcPortal.Tests.csproj` |
| xUnit | 2.9.3 | Unit/parity test | Framework test repo (`HcPortal.Tests`) |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner | Sudah dipakai |
| EFCore InMemory + SqlServer | 8.0.0 | Test fixtures | InMemory utk pure/EF-light test; real-SQL utk integrity (tak relevan utk 425 — pure helper saja) |

### Supporting (existing pure-helper pattern — diikuti)
| Asset | Purpose | When to Use |
|-------|---------|-------------|
| `Helpers/ExamTimeRules.cs` | `AllowedExamSeconds(int, int?)` = `(d + (e ?? 0)) * 60` | **CLN-04** — konsolidasi 4 situs. SUDAH ADA (Phase 424). |
| `HcPortal.Tests/ExamTimeRulesTests.cs` | 3 [Fact] pure parity | **CLN-04** — mirror/extend untuk parity 4 situs. |
| `Helpers/CertIssuanceRules.cs`, `ShuffleToggleRules.cs`, `SessionEditLockRules.cs`, `PrePostPairing.cs` | Pola pure static rule + test | **CLN-05** — analog penempatan helper guard baru. |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `TempData["Warning"]` (CLN-02) | `ModelState.AddModelError` | ModelState error me-render `View(model)` dan **memblokir** SaveChanges → melanggar D-05 (harus TETAP simpan). TOLAK. |
| Helper static `JsonFail(msg)` (CLN-05) | `[ApiController]` automatic 400 | `[ApiController]` mengubah binding behavior + butuh DTO ber-anotasi → melanggar D-04 (tanpa ubah signature). TOLAK. |
| `AllowedExamSeconds(...)/60` di `:4661` | helper menit baru `AllowedExamMinutes` | Keduanya valid (D-02). `/60` lebih minimal (tak nambah API). Helper menit lebih eksplisit. Diskresi planner — keduanya parity-safe. |

**Installation:** Tidak ada. Zero package change.

**Version verification:** Tidak ada package baru untuk diverifikasi. SDK 8.0.418 dikonfirmasi via `dotnet --version`. xUnit 2.9.3 dikonfirmasi di `HcPortal.Tests.csproj:15`.

## Architecture Patterns

### System Architecture Diagram

```
                     ┌─────────────────────────────────────────────┐
   HC submit form    │  TrainingAdminController.AddManualAssessment │
   (manual entry) ──►│  POST :689                                  │
                     │   1. file/dup/cert-format validation         │
                     │   2. if(!ModelState.IsValid) return View ────┼──► (blokir, existing)
                     │   ── CLN-02 INSERT HERE ───────────────      │
                     │   3. cross-validate IsPassed vs              │
                     │      (Score >= PassPercentage)               │
                     │        mismatch? ─► TempData["Warning"]      │  (NON-blocking, tetap lanjut)
                     │   4. build AssessmentSession                 │
                     │        CompletedAt = model.CompletedAt       │
                     │        Schedule    = model.CompletedAt  ◄────┼── CLN-02 align (sudah mirror :762)
                     │   5. SaveChangesAsync ────────────────────── │──► DB (unchanged schema)
                     └─────────────────────────────────────────────┘

   Exam timer flow (read-only consolidation — CLN-04):
   CMPController.StartExam/SubmitExam/EnsureCanSubmit
        ├─ :1191 durationSeconds  ─┐
        ├─ :1564 allowed          ─┤   (d + (e ?? 0)) * 60        ┌──────────────────────┐
        ├─ :1642 allowed          ─┼──────────────────────────►  │ ExamTimeRules        │
        └─ :4661 allowedMinutes   ─┘   (d + (e ?? 0))   *60.0     │ .AllowedExamSeconds  │ (pure, EF-free)
                                                                  └──────────────────────┘
        :469 already wired (Phase 424 GRDF-05) ────────────────►  (single source of truth)
```

### Recommended Touch Map (no new structure)
```
Models/
├── AssessmentSession.cs          # CLN-01 (ValidUntil [Display], Status komentar :20, LinkedSessionId XML-doc) + CLN-03 (AssessmentPhase :180 RESERVED)
├── UserPackageAssignment.cs      # CLN-01 (AssessmentPackageId :18 sentinel komentar/XML-doc)
Controllers/
├── CMPController.cs              # CLN-04 (4 situs → ExamTimeRules)
├── TrainingAdminController.cs    # CLN-02 (POST :689 warning + Schedule align)
├── AssessmentAdminController.cs  # CLN-05 (terapkan guard-helper selektif, opsional)
Helpers/
├── ExamTimeRules.cs             # CLN-04 konsumsi (sudah ada)
├── ControllerGuards.cs (NEW?)   # CLN-05 helper static minimal (nama/lokasi diskresi)
Views/Admin/
├── CreateAssessment.cshtml      # CLN-01 label ValidUntil :637 (standarkan)
├── EditAssessment.cshtml        # CLN-01 label ValidUntil :498
├── AddManualAssessment.cshtml   # CLN-01 label :218 + CLN-02 render TempData warning
HcPortal.Tests/
├── ExamTimeRulesTests.cs        # CLN-04 extend parity
├── ManualCrossValidationTests.cs (NEW?)  # CLN-02 warning logic test
```

### Pattern 1: Pure helper + xUnit parity test (CLN-04, CLN-05)
**What:** Ekstrak formula/aturan ke kelas static EF-free, uji dengan [Fact] tanpa DB.
**When to use:** Setiap konsolidasi formula atau guard murni.
**Example:**
```csharp
// Source: Helpers/ExamTimeRules.cs (existing, Phase 424 — diverifikasi sesi ini)
public static class ExamTimeRules
{
    public static int AllowedExamSeconds(int durationMinutes, int? extraTimeMinutes)
        => (durationMinutes + (extraTimeMinutes ?? 0)) * 60;
}

// Source: HcPortal.Tests/ExamTimeRulesTests.cs (existing — mirror utk CLN-04 parity)
[Fact]
public void WithExtraTime_AddsBeforeMultiply() =>
    Assert.Equal(900, ExamTimeRules.AllowedExamSeconds(10, 5));   // (10+5)*60
```

### Pattern 2: Non-blocking server warning (CLN-02)
**What:** Validasi-silang yang **memberi peringatan tapi tetap menyimpan** — bedakan dari ModelState error (yang memblokir).
**When to use:** Entri historis di mana HC boleh override sengaja (filosofi v32.x).
**Example (canonical untuk CLN-02 — sisip SETELAH `if(!ModelState.IsValid)` lolos):**
```csharp
// CLN-02: cross-validate non-blocking. JANGAN pakai ModelState.AddModelError (itu blokir).
// Score nullable: hanya cek bila terisi.
if (model.Score.HasValue && (model.Score.Value >= model.PassPercentage) != model.IsPassed)
{
    TempData["Warning"] = model.IsPassed
        ? $"Ditandai Lulus walau Score {model.Score} < Pass {model.PassPercentage}%. Tersimpan apa adanya (override HC)."
        : $"Ditandai Tidak Lulus walau Score {model.Score} >= Pass {model.PassPercentage}%. Tersimpan apa adanya.";
    // TIDAK return — tetap lanjut SaveChanges.
}
```
Precedent pola warning non-blocking ada di RTH-05/VAL-06 (Phase 421, `MaxAttempts < terpakai` → warning non-blocking) dan FORM persistensi v32.7.

### Anti-Patterns to Avoid
- **CLN-02 via ModelState.AddModelError:** memicu `return View(model)` → **memblokir simpan**, melanggar D-05. Gunakan `TempData["Warning"]` + lanjut.
- **CLN-04 ubah tipe diam-diam:** `:1564`/`:1642` saat ini `var allowed = ... * 60` bertipe `int`; helper return `int` → aman. `:4661` `allowedMinutes * 60.0` bertipe `double` → bila ganti `ExamTimeRules.AllowedExamSeconds(...)` (int) lalu assign ke `double allowedSec`, konversi implicit → numerik identik. JANGAN ubah ke arithmetic floating yang mengubah pembulatan.
- **CLN-04 menyentuh token/write-on-GET:** D-03 melarang. JANGAN sentuh `TempData.Peek` token gate atau side-effect GET StartExam.
- **CLN-05 ubah signature action / introduksi DTO ber-anotasi:** D-04 melarang. Helper static yang dipanggil DI DALAM action saja.
- **CLN-03 drop kolom / generate migration:** D-01 melarang. RESERVED via XML-doc saja.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Formula durasi timer (CLN-04) | Inline `(d + (e ?? 0)) * 60` di tiap situs | `ExamTimeRules.AllowedExamSeconds` (existing) | Sudah jadi single-source Phase 424; inline = drift kembali (akar FLOW-09). |
| Warning UI (CLN-02) | Custom flash mechanism | `TempData["Warning"]` (konvensi repo: `TempData["Error"]`/`["Success"]`) | Konsisten dgn gate existing (`CMPController.cs:904-975`, `TrainingAdminController` `:798`). |
| Validasi cross-field (CLN-02) | Auto-override nilai HC | Hanya warning, simpan verbatim | D-05 eksplisit: TIDAK auto-override, surface entri historis. |

**Key insight:** Phase 425 adalah cleanup yang membayar tech-debt drift — duplikasi formula/komentar usang. Membuat solusi "baru" justru menambah permukaan; pekerjaan benar = **konvergensi ke aset yang sudah ada** + dokumentasi yang akurat.

## Runtime State Inventory

> Rename/refactor-adjacent (CLN-01 menyelaraskan nama/komentar; CLN-03 menandai kolom). Diaudit eksplisit.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | **None** — CLN-01/03 tidak mengubah nama kolom DB atau key data; hanya komentar/XML-doc/`[Display]` di POCO + label cshtml. `AssessmentPhase` tetap ada di skema (RESERVED). Verified: grep `AssessmentPhase` di Controllers/Services/Views = 0; tidak ada data bergantung pada label/komentar. | Code/text edit only — TIDAK ada migrasi data. |
| Live service config | **None** — verified: tidak ada service eksternal (n8n/Datadog/dll) di scope; ini aplikasi MVC monolit. | None. |
| OS-registered state | **None** — verified: tidak ada task scheduler/pm2/systemd terkait nama field assessment. | None. |
| Secrets/env vars | **None** — verified: tidak ada secret/env var referensi `AssessmentPhase`/`AssessmentPackageId`/`ValidUntil`/`LinkedSessionId` by name. | None. |
| Build artifacts | **None baru** — no project rename, no package id change. Build artefak `bin/obj` regen normal saat `dotnet build`. | Standard rebuild. |

**Kesimpulan:** Phase 425 adalah refactor **text-level & formula-consolidation**, BUKAN rename yang menyentuh runtime state. `[Display(Name=...)]` yang ditambahkan ke `ValidUntil` (CLN-01) hanya mempengaruhi label tampilan (tag helper `asp-for`/`Html.DisplayNameFor`), TIDAK mengubah binding/skema.

## Common Pitfalls

### Pitfall 1: CLN-04 parity tipe (int vs double) di situs `:4661`
**What goes wrong:** `:4661` memakai `allowedMinutes` (menit, lalu `* 60.0` jadi double); 3 situs lain langsung detik (int `* 60`). Ganti naif bisa mengubah pembulatan.
**Why it happens:** Helper return `int` detik; situs `:4661` butuh `double allowedSec`.
**How to avoid:** `double allowedSec = ExamTimeRules.AllowedExamSeconds(...)` (int → implicit double, identik) ATAU pertahankan `allowedMinutes` lewat helper menit. Tulis parity test eksplisit dgn nilai `(d, e)` yang sama untuk membuktikan output 4 situs == formula lama.
**Warning signs:** Test `:4661` grace-limit (`allowedSec + 120.0`) berubah; timer "habis mendadak" muncul lagi.

### Pitfall 2: CLN-02 warning ikut memblokir simpan
**What goes wrong:** Developer refleks pakai `ModelState.AddModelError` → `return View(model)` → submit terblokir.
**Why it happens:** Pola dominan di POST `:689` (semua cek lain memang blokir via ModelState).
**How to avoid:** Sisip warning SETELAH `if(!ModelState.IsValid) return View` (`:725-730`), gunakan `TempData["Warning"]`, dan **jangan `return`**. Verifikasi: sesi tetap ter-`Add` + `SaveChanges`.
**Warning signs:** UAT: HC isi Lulus + Score<Pass → submit ditolak (SALAH; harus tersimpan + warning).

### Pitfall 3: CLN-02 Score null
**What goes wrong:** `model.Score` adalah `int?` (`CreateManualAssessmentViewModel.cs:18`). Cek `Score >= PassPercentage` pada null = compile error / perilaku tak jelas.
**Why it happens:** Score opsional di entry manual.
**How to avoid:** Guard `if (model.Score.HasValue && ...)`. Bila Score null → tidak ada basis cross-validate → skip warning (atau warning terpisah "Score kosong, status Lulus by HC").
**Warning signs:** NullReferenceException / submit gagal saat Score dikosongkan.

### Pitfall 4: Line drift antara dokumen audit vs kode aktual
**What goes wrong:** Dokumen audit (2026-06-22) menulis line lama (PA-05 `:1107-1109`, FLD-5.2-06 `:611`) yang sudah bergeser akibat edit fase 420-424.
**Why it happens:** Kode berubah 5 fase sejak audit; CONTEXT 425 memakai line terbaru yang akurat.
**How to avoid:** SELALU grep simbol (`AssessmentPhase`, `AllowedExamSeconds`, label teks) sebelum edit — JANGAN percaya line audit buta. Lihat tabel "Line Drift" di Integration Points.
**Warning signs:** Edit di line yang ternyata sudah berisi kode lain.

### Pitfall 5: CLN-05 over-reach mengubah perilaku
**What goes wrong:** "Merapikan" ModelState malah mengganti pesan/format respons → frontend JS yang baca `message`/`success` rusak.
**Why it happens:** ~52 call-site `Json(new {success=false, message=...})`; helper terlalu agresif.
**How to avoid:** Helper hanya MEMBUNGKUS pembuatan objek respons (output identik). Terapkan selektif, jaga shape JSON (`success`, `message`) byte-identik. D-04: minimal, tanpa ubah signature.
**Warning signs:** Endpoint AJAX (token regen, essay grade) menampilkan error format berbeda di UI.

## Code Examples

### CLN-03 RESERVED XML-doc (AssessmentSession.cs:177-180)
```csharp
// Source: Models/AssessmentSession.cs:177-180 (current — diverifikasi)
// SEBELUM:
/// <summary>
/// Fase assessment dalam siklus: 'Phase1', 'Phase2', dll. Null = tidak ada fase.
/// </summary>
public string? AssessmentPhase { get; set; }

// SESUDAH (CLN-03 — RESERVED, no drop, no migration):
/// <summary>
/// RESERVED — tidak dipakai. Dideklarasikan di v14 (AddAssessmentV14Columns) untuk konsep
/// 'Phase1'/'Phase2' yang TIDAK PERNAH diimplementasikan. Linking Pre/Post nyata bertumpu pada
/// AssessmentType + LinkedGroupId + LinkedSessionId. 0 referensi di app (FLOW-06). Dipertahankan
/// di skema (kolom nullable, aman) untuk hindari migration destruktif. Jangan baca/tulis.
/// </summary>
public string? AssessmentPhase { get; set; }
```

### CLN-01 Status komentar 7-nilai (AssessmentSession.cs:20)
```csharp
// Source: Models/AssessmentSession.cs:20 (current). Kanonik 7 status dari AssessmentConstants.AssessmentStatus.
// SEBELUM: public string Status { get; set; } = "";   // "Open", "Upcoming", "Completed"
// SESUDAH:
public string Status { get; set; } = "";
// Nilai kanonik: lihat AssessmentConstants.AssessmentStatus — Open, Upcoming, Completed,
// "Menunggu Penilaian" (PendingGrading), InProgress, Cancelled, Abandoned (7 status).
```

### CLN-01 ValidUntil [Display] (AssessmentSession.cs:84)
```csharp
// Source: Models/AssessmentSession.cs:79-84 (current). FLD-5.2-06: label inkonsisten online/manual.
// Tambah [Display] sebagai satu sumber label; selaraskan teks cshtml ke istilah tunggal.
[Display(Name = "Berlaku Sampai")]   // standarkan; CreateAssessment.cshtml:637 "Tanggal Expired Sertifikat" → samakan
public DateOnly? ValidUntil { get; set; }
```

### CLN-01 LinkedSessionId XML-doc koreksi (AssessmentSession.cs:188-192)
```csharp
// Source: Models/AssessmentSession.cs:188-192 (current). PA-04: klaim "ON DELETE SET NULL" KELIRU.
// Null-clear sebenarnya app-level di RecordCascadeDeleteService.cs:235-237 (Delta #8).
/// <summary>
/// FK ke AssessmentSession lain yang terhubung (misal: PreTest terhubung ke PostTest-nya).
/// CATATAN (PA-04): TIDAK ada FK cascade terkonfigurasi di DB. Null-clear saat pasangan
/// dihapus dilakukan di level aplikasi — RecordCascadeDeleteService.cs:235-237 (Delta #8).
/// </summary>
public int? LinkedSessionId { get; set; }
```

### CLN-01 AssessmentPackageId sentinel (UserPackageAssignment.cs:17-18)
```csharp
// Source: Models/UserPackageAssignment.cs:17-18 (current). PA-05: nama menyesatkan.
// SEBELUM: // FK to the package assigned to this user (Restrict — ...)
// SESUDAH:
// SENTINEL (PA-05): bukan "paket aktual" peserta — ini paket PERTAMA (seed) yang dipilih saat
// build assignment (CMPController.cs:1087-1093). Soal aktual ditentukan oleh ShuffledQuestionIds
// (bisa lintas paket). FK Restrict — assignment tak ikut terhapus saat package dihapus.
public int AssessmentPackageId { get; set; }
```

### CLN-04 konsolidasi 4 situs (CMPController.cs)
```csharp
// Source: Controllers/CMPController.cs — 4 situs diverifikasi sesi ini. Wire ke ExamTimeRules.
// :1191  int durationSeconds = (assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60;
//   → int durationSeconds = ExamTimeRules.AllowedExamSeconds(assessment.DurationMinutes, assessment.ExtraTimeMinutes);
// :1564  var allowed = (assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60;
//   → var allowed = ExamTimeRules.AllowedExamSeconds(assessment.DurationMinutes, assessment.ExtraTimeMinutes);
// :1642  (sama dengan :1564)
//   → var allowed = ExamTimeRules.AllowedExamSeconds(assessment.DurationMinutes, assessment.ExtraTimeMinutes);
// :4661  int allowedMinutes = assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0);
//        double allowedSec  = allowedMinutes * 60.0;
//   → double allowedSec = ExamTimeRules.AllowedExamSeconds(assessment.DurationMinutes, assessment.ExtraTimeMinutes);
//     (int→double implicit; numerik identik. allowedMinutes bisa dihapus bila tak dipakai lagi.)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Formula durasi inline tersebar | `ExamTimeRules.AllowedExamSeconds` single-source | Phase 424 (GRDF-05, `:469`) | CLN-04 menyelesaikan konvergensi 4 situs sisa. |
| `[ApiController]` auto-400 untuk validasi | Tidak dipakai di controller ini (manual if-guard) | by-design repo | CLN-05 hanya menyeragamkan guard manual, TIDAK migrasi ke `[ApiController]` (melanggar D-04). |
| `ModelState.AddModelError` untuk semua validasi | Pisahkan blocking (ModelState) vs non-blocking (`TempData["Warning"]`) | Phase 421 RTH-05/VAL-06 precedent | CLN-02 mengikuti pola warning non-blocking. |

**Deprecated/outdated:**
- Klaim XML-doc "ON DELETE SET NULL" pada `LinkedSessionId` — usang/keliru (app-level). Dikoreksi CLN-01/PA-04.
- Komentar `Status` 3-nilai — usang (kini 7 status). Dikoreksi CLN-01/FLOW-05.
- Konsep kolom `AssessmentPhase` — never-implemented dead-field. Ditandai RESERVED CLN-03/FLOW-06.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| — | (kosong) | — | — |

**Semua klaim diverifikasi langsung di codebase sesi ini (grep/read) atau di-cite dari CONTEXT/audit. Tidak ada klaim `[ASSUMED]`.** Tidak ada keputusan yang butuh konfirmasi user tambahan — D-01..D-05 sudah locked di CONTEXT.

## Open Questions

1. **CLN-02: apakah Edit manual (`EditManualAssessmentViewModel` / `UpdateManualAssessment` `:1069`) juga perlu cross-validation?**
   - What we know: `EditManualAssessmentViewModel` (`Models/CreateManualAssessmentViewModel.cs:65-118`) punya field identik (Score/PassPercentage/IsPassed). CONTEXT D-05 hanya menyebut **AddManualAssessment POST + CreateManualAssessmentViewModel**.
   - What's unclear: apakah konsistensi Edit termasuk scope minimal atau di luar.
   - Recommendation: **Ikut scope CONTEXT (Add saja)** untuk minim risiko. Catat Edit sebagai item konsistensi opsional (Claude's Discretion) — bila planner ingin, terapkan helper cross-validate yang SAMA di Edit POST (low marginal cost karena helper sudah ada). JANGAN jadikan blocking requirement.

2. **CLN-04 `:4661`: `/60` vs helper menit baru?**
   - What we know: D-02 izinkan keduanya. `:4661` butuh detik (`allowedSec`), bukan menit murni.
   - What's unclear: preferensi gaya.
   - Recommendation: pakai `ExamTimeRules.AllowedExamSeconds(...)` langsung ke `allowedSec` (paling minimal, 0 API baru, parity terjamin). Hapus `allowedMinutes` bila jadi unused.

3. **CLN-05 cakupan penerapan guard-helper.**
   - What we know: ~52 call-site `Json(new {success=false,...})` di AssessmentAdminController; ~18 guard di CMPController. D-04 minta "minimal".
   - What's unclear: berapa banyak yang dirapikan.
   - Recommendation: buat helper + terapkan ke SUBSET representatif (mis. cluster di satu/dua action besar) sebagai demonstrasi konvensi, JANGAN sweep semua (risiko regresi + di luar "minimal"). Output JSON shape WAJIB identik byte-per-byte.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/test (CLN-04 parity) | ✓ | 8.0.418 | — |
| dotnet build | verifikasi lokal (CLAUDE.md §3) | ✓ | (SDK) | — |
| dotnet run @localhost:5270 | UAT manual (branch ITHandoff) | ✓ | (SDK) | — |
| SQL Server lokal | hanya bila ada integration test SQL-real | ✓ (per precedent v32.x) | — | InMemory cukup utk pure parity CLN-04 |
| Playwright | UAT browser opsional | (per repo) | — | UAT manual cukup utk cleanup low-risk |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** Phase 425 pure-helper/text — parity test CLN-04 & cross-validation CLN-02 bisa diuji **tanpa DB** (pure xUnit). SQL-real tidak diperlukan.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (`HcPortal.Tests`) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (net8.0, IsTestProject) |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~ExamTimeRules\|FullyQualifiedName~ManualCrossValidation"` |
| Full suite command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| Baseline (post-424) | suite 748 pass / 0 fail / 2 skip (per commit `ab5beebe`) — 425 harus jaga 0 regresi |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CLN-04 | 4 situs timer == formula lama (parity numerik) | unit (pure) | `dotnet test --filter FullyQualifiedName~ExamTimeRules` | ✅ extend `ExamTimeRulesTests.cs` |
| CLN-04 | `(d,e)` representatif: (10,5)=900, (10,null)=600, (0,0)=0, (60,15)=4500 | unit (pure) | (sama) | ✅ tambah [Theory] parity |
| CLN-02 | mismatch IsPassed vs Score>=Pass → warning, sesi TETAP ter-add | unit/integration | `dotnet test --filter FullyQualifiedName~ManualCrossValidation` | ❌ Wave 0 (NEW `ManualCrossValidationTests.cs`) |
| CLN-02 | match (Score>=Pass)==IsPassed → NO warning | unit/integration | (sama) | ❌ Wave 0 |
| CLN-02 | Score null → no NRE, no spurious block | unit | (sama) | ❌ Wave 0 |
| CLN-01 | label/komentar/XML-doc selaras | static/grep-verifiable | `grep` / build 0-warning + code review | N/A (manual verify) |
| CLN-03 | `AssessmentPhase` RESERVED + 0 referensi app | static/grep-verifiable | `rg "AssessmentPhase" Controllers Services Views` = 0 | N/A (grep, diverifikasi=0) |
| CLN-05 | guard-helper: output JSON shape identik | unit (pure) | `dotnet test --filter FullyQualifiedName~ControllerGuards` | ❌ Wave 0 (bila helper dibuat) |

### Sampling Rate
- **Per task commit:** `dotnet build` (0 error) + `dotnet test --filter ~ExamTimeRules` (parity cepat <30s).
- **Per wave merge:** full suite `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (jaga 748/0/2, no regresi).
- **Phase gate:** full suite green + `dotnet build` 0 error/0 warning baru + UAT manual @5270 (CLN-02 warning hidup, CLN-01 label tampil) sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ExamTimeRulesTests.cs` — EXTEND dgn [Theory] parity 4 situs (CLN-04). File ADA, perlu tambah kasus.
- [ ] `HcPortal.Tests/ManualCrossValidationTests.cs` — NEW, covers CLN-02 (warning-on-mismatch / no-warning-on-match / null-safe). Bila logika cross-validate diekstrak ke helper pure (mis. `ManualEntryRules.IsPassMismatch`), test jadi pure tanpa DB.
- [ ] `HcPortal.Tests/ControllerGuardsTests.cs` — NEW (opsional, bila CLN-05 helper dibuat) — assert shape JSON identik.
- *Framework install:* tidak perlu — xUnit sudah terpasang.

**Rekomendasi Wave-0:** Ekstrak logika cross-validate CLN-02 ke pure helper kecil (mis. `Helpers/ManualEntryRules.cs` `bool PassStatusMismatch(int? score, int passPct, bool isPassed)`) agar testable tanpa DB + reusable bila Edit manual ikut (Open Question #1). Ini selaras pola 422/423/424.

## Security Domain

> `security_enforcement` absent di config → enabled. Disertakan meski fase cleanup low-risk.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tak ada perubahan auth. Action existing sudah `[Authorize(Roles="Admin, HC")]`. |
| V3 Session Management | no | Tak menyentuh session/token. **D-03 eksplisit DEFER token gate (FLOW-08)** — JANGAN sentuh. |
| V4 Access Control | no (pertahankan) | `AddManualAssessment` POST sudah `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` (`:686-688`). CLN-02 TIDAK boleh melemahkan ini. |
| V5 Input Validation | yes (ringan) | CLN-02 menambah cross-validation server-side (memperKUAT, non-blocking). CLN-05 menyeragamkan guard — output identik. |
| V6 Cryptography | no | Tidak ada kripto. |
| V12 File Resources | no (pertahankan) | `FileUploadHelper.ValidateCertificateFile` existing (`:704`) — CLN tidak menyentuh. |

### Known Threat Patterns for ASP.NET Core MVC (scope 425)

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF pada manual entry POST | Tampering | `[ValidateAntiForgeryToken]` SUDAH ADA `:687` — CLN-02 JANGAN hapus. |
| Privilege bypass entry manual | Elevation | `[Authorize(Roles="Admin, HC")]` SUDAH ADA `:688` — pertahankan. |
| XSS via TempData warning (CLN-02) | Tampering/Info | Pesan warning hanya gunakan nilai numerik (`Score`, `PassPercentage`) + teks statis — TIDAK echo input string mentah. Razor auto-encode. Aman selama tidak `Html.Raw`. |
| Token gate weakening (D-03 area) | Spoofing | **OUT OF SCOPE — JANGAN sentuh.** Defer ke backlog dengan impersonation guard existing tetap aktif. |
| JSON response shape regression (CLN-05) | (integrity, bukan security) | Helper jaga shape `{success,message}` identik; tidak membocorkan info baru. |

**Verdict keamanan:** Phase 425 net-positive atau netral untuk keamanan — CLN-02 memperkuat validasi server-side (otoritas server), CLN-05 hanya refactor presentasi. Tidak ada permukaan serangan baru. Risiko utama = **regresi tak sengaja melemahkan guard existing** (CSRF/authz/file-validation) — verifikasi atribut `[Authorize]`/`[ValidateAntiForgeryToken]` tetap utuh pasca-edit.

## Project Constraints (from CLAUDE.md)

| Directive | Implication untuk Phase 425 |
|-----------|------------------------------|
| Always respond in Bahasa Indonesia | Semua dokumen/komentar/pesan warning dalam Bahasa Indonesia. |
| Develop Workflow: Lokal → Dev → Prod | Verifikasi LOKAL dulu: `dotnet build` + `dotnet run`. JANGAN edit langsung di Dev/Prod. |
| Branch ITHandoff pakai port **5270** | UAT manual @ `http://localhost:5270` (bukan 5277 — itu worktree main). Override `dotnet run --urls`; JANGAN commit `launchSettings`. |
| Verifikasi lokal sebelum push | `dotnet build` + `dotnet run` + cek DB lokal + (Playwright bila ada) sebelum commit. |
| Promosi ke Dev = tanggung jawab IT | Notify IT dgn commit hash + **migration=FALSE** (penting: 425 TIDAK nambah migration; milestone tetap migration=TRUE dari fase lain). |
| Seed Data Workflow | Bila butuh seed untuk UAT CLN-02: klasifikasi → snapshot DB → catat `docs/SEED_JOURNAL.md` → restore + tandai `cleaned`. Untuk parity test CLN-04 (pure) tidak perlu seed. |

## Reconciliation Needed (untuk planner & milestone audit)

> **PENTING:** Dokumen planning tertentu masih berisi opsi LAMA yang sudah di-RESOLVE oleh CONTEXT (authoritative). Planner WAJIB menggunakan CONTEXT, bukan dokumen lama ini, dan men-surface ke milestone audit.

| Dokumen | Teks lama (usang) | Status CONTEXT (authoritative) |
|---------|-------------------|-------------------------------|
| `REQUIREMENTS.md:67` (CLN-03) | "di-drop (migration) **atau** ditandai RESERVED" | **RESERVED** (D-01). Drop ditolak. |
| `REQUIREMENTS.md:68` (CLN-04) | "token via mekanisme server-authoritative, side-effect write-on-GET dipindah/diamankan" | Token + write-on-GET **DEFER** ke backlog (D-03). Hanya timer satu-sumber yang dikerjakan. |
| `ROADMAP.md:48, :151` | "**425=KEMUNGKINAN TRUE** (CLN-03 drop AssessmentPhase — TBD plan-phase)" | **migration=FALSE** (D-01). Sudah pasti, bukan TBD. |
| `ROADMAP.md:156` (SC #3) | "di-drop (migration) atau ditandai RESERVED" | **RESERVED** only. |
| `ROADMAP.md:157` (SC #4) | "token via server-authoritative, write-on-GET diamankan" | DEFER (hanya timer satu-sumber + ModelState konvensi yang dikerjakan). |

**Action item planner:** Saat finalize fase / milestone audit, rekonsiliasi REQUIREMENTS/ROADMAP CLN-03/CLN-04 agar mencerminkan keputusan RESERVED + DEFER + migration=FALSE. (Sama pola action-item rekonsiliasi GRDF-06 di Phase 424.)

## Line Drift Reference (audit doc vs kode aktual sesi ini)

| Temuan | Line di audit (2026-06-22) | Line aktual (diverifikasi 2026-06-24) | Catatan |
|--------|----------------------------|----------------------------------------|---------|
| CLN-03 AssessmentPhase | `:175-178` | **`:180`** (deklarasi); XML-doc `:177-180` | CONTEXT akurat. |
| CLN-04 timer detik | `:1175 / :1548 / :1626` (docstring ExamTimeRules) | **`:1191 / :1564 / :1642`** | CONTEXT akurat. |
| CLN-04 timer menit | `:4590-4592` / `:4596` | **`:4661`** | CONTEXT akurat. |
| CLN-01 PA-05 sentinel | `:1107-1109` (CMPController) | **`:1087-1093`** (sentinel) + `:1123` (BUG-05) | Komentar "Sentinel" sudah ada inline. |
| CLN-01 ValidUntil label | `CreateAssessment.cshtml:611` | **`:637`** | + `EditAssessment.cshtml:498`, `AddManualAssessment.cshtml:218`. |
| CLN-01 Status komentar | `AssessmentSession.cs:20` | **`:20`** (`// "Open","Upcoming","Completed"`) | Akurat. |
| CLN-01 LinkedSessionId doc | `AssessmentSession.cs:188` | **`:188-192`** (XML-doc klaim SET NULL) | Null-clear app-level: `RecordCascadeDeleteService.cs:235-237`. |
| CLN-02 AddManualAssessment | `TrainingAdminController.cs:744-748, :759` | POST **`:689`**; Score `:758`, IsPassed `:760`, Schedule/CompletedAt `:761-762` | CONTEXT akurat; `GenerateCertificate` sudah `= !IsNullOrWhiteSpace(NomorSertifikat)` (`:774`, FLD-5.2-02 sudah ditangani Phase 423). |
| CLN-05 VAL-07 | `AssessmentAdminController.cs:1082` (1× ModelState.IsValid) | ~52 `Json(success=false)` + 1 `ModelState.IsValid` | Pola if-guard manual dominan. |

## Sources

### Primary (HIGH confidence — verified in codebase this session)
- `Models/AssessmentSession.cs:20, :84, :180, :188-192` — Status komentar, ValidUntil, AssessmentPhase, LinkedSessionId XML-doc
- `Models/UserPackageAssignment.cs:17-18` — AssessmentPackageId sentinel
- `Models/CreateManualAssessmentViewModel.cs:16-30, :65-118` — Score/PassPercentage/IsPassed/CompletedAt + EditManual VM
- `Models/AssessmentConstants.cs:13-21` — 7 status kanonik
- `Controllers/CMPController.cs:1087-1093, :1123, :1191, :1564, :1642, :4655-4664` — sentinel + 4 situs timer
- `Controllers/TrainingAdminController.cs:678-800` — AddManualAssessment GET/POST + IsManualEntry
- `Controllers/AssessmentAdminController.cs` — pola Json(success=false) (count 52)
- `Helpers/ExamTimeRules.cs` — AllowedExamSeconds (Phase 424)
- `HcPortal.Tests/ExamTimeRulesTests.cs` + `HcPortal.Tests.csproj` — framework xUnit 2.9.3, net8.0
- `Services/RecordCascadeDeleteService.cs:235-237` — LinkedSessionId null-clear app-level (PA-04 evidence)
- Grep: `AssessmentPhase` di Controllers/Services/Views = **0 referensi** (CLN-03 RESERVED safe)

### Secondary (CITED — planning/audit docs)
- `.planning/phases/425-cosmetic-naming-tech-debt-cleanup/425-CONTEXT.md` — D-01..D-05 (authoritative)
- `.planning/phases/424-.../424-CONTEXT.md` — pola pure-helper + ExamTimeRules + forward-only
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md:159, :226-227, :242-247, :390-394, :412, :432-440, :479` — sumber temuan CLN
- `.planning/REQUIREMENTS.md:63-69` (§CLN) + `.planning/ROADMAP.md:148-159` (§Phase 425) — acceptance (perlu rekonsiliasi, lihat di atas)
- `CLAUDE.md` — Develop/Seed Workflow, port 5270

### Tertiary (LOW confidence)
- None — semua klaim diverifikasi atau di-cite.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tidak ada dependency baru; semua aset existing diverifikasi (csproj, helper, test).
- Architecture: HIGH — 5 CLN dipetakan ke lokasi konkret yang diverifikasi line-by-line; pola pure-helper established.
- Pitfalls: HIGH — pitfall parity/blocking/null/drift diturunkan dari pembacaan kode aktual + presedent fase 421-424.
- Validation: HIGH — framework + baseline suite (748/0/2) diketahui; parity test CLN-04 punya template existing.

**Research date:** 2026-06-24
**Valid until:** ~2026-07-24 (30 hari — kode stabil, fase terakhir milestone; line bisa bergeser bila ada edit lain di branch ITHandoff sebelum eksekusi → re-grep simbol saat plan/execute).
