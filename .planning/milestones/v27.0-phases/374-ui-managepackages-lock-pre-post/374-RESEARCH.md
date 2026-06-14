# Phase 374: UI ManagePackages + Lock + Pre/Post - Research

**Researched:** 2026-06-13
**Domain:** ASP.NET Core MVC 8 admin frontend (Razor + Bootstrap 5.3) + 1 POST endpoint (sibling-propagate + lock guard + audit) — fitur Shuffle Toggle v27.0
**Confidence:** HIGH (semua pola di-verifikasi via grep/Read terhadap codebase aktual sesi ini; engine + test infra sudah ada dari Phase 372/373)

## Summary

Phase 374 murni **frontend + satu endpoint** di atas engine yang sudah jadi (Phase 372 kolom + propagasi create/edit; Phase 373 `Helpers/ShuffleEngine.cs` + StartExam/reshuffle wiring). Tidak ada perubahan engine, **tidak ada migration** (kolom `ShuffleQuestions`/`ShuffleOptions` sudah live di `AssessmentSession` sejak Phase 372 — `[VERIFIED: Models/AssessmentSession.cs:38-42]`). Semua pola yang dibutuhkan sudah ADA verbatim di codebase: form-switch toggle (`CreateAssessment.cshtml:520-535`), sibling-propagate `foreach` (`EditAssessment` POST `:2019-2045`), lock-condition + sibling query + audit (`ReshufflePackage` `:5062-5147`), lock alert banner (`ManagePackages.cshtml:29-44`), TempData PRG alert (`:19-27`). Pekerjaan = **menyalin & menggabungkan** pola tersebut, bukan menciptakan pola baru.

Tiga keputusan teknis utama, semua sudah locked di CONTEXT.md/UI-SPEC dan terkonfirmasi feasible:
1. **Endpoint `UpdateShuffleSettings`** = mirror `EditAssessment` propagate-foreach + `ReshufflePackage` lock-detection + audit `LogAsync` try/catch warn-only + **PRG redirect** (BUKAN Json — D-01a; berbeda dari Reshuffle yang return Json karena Reshuffle dipanggil via AJAX, sedangkan UpdateShuffleSettings adalah form-submit eksplisit).
2. **Lock = defense-in-depth** (D-04a): controller GET menghitung `IsShuffleLocked` → ViewBag → UI `disabled`; endpoint POST **mengulang** cek lock server-side → tolak + TempData error. UI disabled BUKAN satu-satunya guard (SHUF-11 acceptance = server reject).
3. **Warning §9 live JS** (D-03a) recompute saat flip Acak Soal + page load; **reminder Pre/Post saved-state** (D-03b) server-render only di halaman Post.

**Primary recommendation:** Plan sebagai 1 wave dengan ~3 tugas: (T1) endpoint `UpdateShuffleSettings` + lock-helper, (T2) ManagePackages GET ViewBag enrich (lock/hide/Pre-state), (T3) Razor card + live JS. Test Phase 374 = endpoint guard-reject + propagate (real-SQL, pola `ShufflePropagationTests`) + lock-condition pure helper bila diekstrak. **Full mode-matrix + Playwright UAT = Phase 375** (spec §11, deferred). **Re-grep SEMUA line number di execute-time** — `AssessmentAdminController.cs` adalah area sibuk v25.0 (367/368) dan offset PASTI bergeser.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Render 2 toggle + alert + tombol Simpan | Frontend Server (Razor view) | — | Server-rendered Razor; state dari ViewBag (pola halaman ini ViewBag-driven, bukan VM) |
| Hitung lock-condition (started/assignment) | API/Backend (controller GET) | DB | Query sibling `StartedAt`/`UserPackageAssignment` butuh EF — tidak bisa di view |
| Enforce lock saat simpan | API/Backend (endpoint POST) | DB | SHUF-11 acceptance = **server reject**, bukan UI disabled. Defense-in-depth |
| Propagate flag ke sibling | API/Backend (endpoint POST) | DB | `foreach siblings` + `SaveChangesAsync` (pola EditAssessment) |
| Live recompute warning §9 | Browser/Client (vanilla JS) | — | Baca state checkbox **saat ini** (unsaved) — tak bisa server-side; data mismatch dari server |
| Reminder Pre/Post (saved-state) | Frontend Server (controller GET → ViewBag) | DB | Baca SAVED Pre `ShuffleQuestions` via `LinkedSessionId` — lintas-halaman, harus saved-state |
| Hide toggle (Proton Th3/Manual) | Frontend Server (controller GET → ViewBag flag) | — | Flag dihitung controller, `@if` di view; HIDE total (bukan disabled) |
| Audit log perubahan | API/Backend (endpoint POST) | DB | `AuditLogService.LogAsync` try/catch warn-only (pola Reshuffle `:5132-5144`) |

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Tombol "Simpan Pengaturan" **eksplisit** — BUKAN auto-save-on-flip. HC ubah toggle → klik Simpan sekali → POST `UpdateShuffleSettings`. Alasan: perubahan kena SEMUA sibling (propagate), tombol eksplisit hindari salah-klik massal.
- **D-01a:** Return endpoint → **form POST + Redirect (PRG) ke ManagePackages + `TempData` sukses/gagal**, konsisten pola form existing (`CreatePackage`/`DeletePackage`/`CopyPackagesFromPre`). **BUKAN AJAX Json.** (Scout sempat usul Json — abaikan, pilih PRG.)
- **D-02:** **Card khusus "Pengacakan Soal & Jawaban"** sendiri, di bawah header ManagePackages (L8-17) & **sebelum** panel ringkasan paket (L83-114). Host: 2 toggle + tombol Simpan + alert lock + warning §9 + reminder Pre/Post. Reuse `card` + `card-header bg-light` + `form-check form-switch` (pola `IsTokenRequired`) + ikon `bi-shuffle text-primary`.
- **D-03:** Warning §9 + reminder Pre/Post = **alert di DALAM card Pengacakan** (bukan digabung ke panel ringkasan). Copy final dari spec §9 + §7.1 — **JANGAN dipangkas/diubah.**
- **D-03a:** Warning §9 **live JS recompute** saat Acak Soal di-flip + saat page load. Trigger: jumlah paket-ber-soal ≥2 + Acak Soal OFF + ukuran paket beda. Mismatch ukuran paket sudah dihitung — **reuse, jangan hitung ulang dari nol.**
- **D-03b:** Reminder Pre/Post (opsi Z) **HANYA di halaman Post** (`ViewBag.IsPostSession`), berdasar **SAVED state Pre** via `LinkedSessionId` (Pre.`ShuffleQuestions`==false && Post.`ShuffleQuestions`==true). Saved-state-driven (bukan live JS lintas-halaman), no auto-cascade, no hidden state.
- **D-04:** Saat terkunci: **switch `disabled` + alert banner di card** jelaskan alasan. Pola konsisten dgn lock `SamePackage` (`ManagePackages.cshtml:29-44`).
- **D-04a:** **Defense-in-depth** — UI disabled BUKAN satu-satunya guard. Endpoint `UpdateShuffleSettings` WAJIB tolak perubahan server-side saat lock-condition true (SHUF-11), kembalikan TempData error.

### Locked dari spec (carry-forward — JANGAN diubah, JANGAN ditanya ulang)
- Per-assessment + propagate sibling key `(Title, Category, Schedule.Date)` — pola `EditAssessment` POST `foreach siblings`.
- Endpoint `UpdateShuffleSettings(int,bool,bool)` `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` + audit (`LogAsync(actorUserId, actorName "NIP - FullName", "UpdateShuffleSettings", desc, assessmentId, "AssessmentSession")`, try/catch warn-only) + propagate foreach sibling.
- Lock condition: ada sibling `StartedAt != null` ATAU ada `UserPackageAssignment` dgn `AssessmentSessionId ∈ siblingSessionIds`.
- Hide toggle: Proton Tahun 3 (`Category == "Assessment Proton" && TahunKe == "Tahun 3"`) ATAU `IsManualEntry == true`.
- SamePackage TIDAK dipindah; toggle shuffle tetap aktif walau `SamePackage` lock isi paket (`ViewBag.IsSamePackageLocked` hanya lock create/delete paket).
- Copy toggle "Acak Soal" / "Acak Pilihan Jawaban" + help-text = **372-UI-SPEC** verbatim; frasa "jawaban benar tetap dinilai dengan benar" WAJIB ada.
- Default ON; grading by `PackageOption.Id` (acak opsi tak pengaruh nilai).
- **Migration = false.**

### Claude's Discretion
- Bentuk return exact endpoint (form POST+Redirect PRG vs partial render) — **lean PRG** karena D-01 explicit-save.
- Markup/ID exact card + toggle + alert (selama reuse pola Bootstrap existing + copy 372-UI-SPEC verbatim).
- Apakah jalankan `/gsd-ui-phase 374` sebelum plan — **sudah dijalankan**: `374-UI-SPEC.md` status `approved` (2026-06-13). Kontrak visual lengkap tersedia.

### Deferred Ideas (OUT OF SCOPE)
- xUnit lock-guard + propagate test + Playwright UAT (toggle ON/OFF + lock + reminder + warning) → **Phase 375**.
- Memindah setting `SamePackage` ke halaman package → out of scope (spec §12).
- Auto-cascade Pre→Post → ditolak by design (pakai reminder opsi Z saja).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SHUF-10 | Toggle UI di ManagePackages aktif walau SamePackage lock; endpoint POST `UpdateShuffleSettings` ([Authorize Admin,HC]+AntiForgery+audit+propagate) | Endpoint = mirror `EditAssessment:2019-2045` propagate-foreach + attribute trio `ReshufflePackage:5062-5064`. Toggle independen dari `IsSamePackageLocked` (lock itu hanya gate create/delete paket — `ManagePackages.cshtml:170,237`). Card dirender selalu (kecuali hide-condition). |
| SHUF-11 | Lock toggle (read-only + guard server-side) saat ada peserta mulai (`StartedAt!=null` ATAU ada `UserPackageAssignment` grup) | Lock-detection = `ReshufflePackage:5078` (`StartedAt!=null`) + `UserPackageAssignment` lookup (pola `:5103`/`:5278-5282`). Defense-in-depth: ViewBag (UI disabled) + endpoint re-check (server reject). |
| SHUF-12 | Warning non-blocking saat multi-paket + Acak Soal OFF + jumlah soal antar paket berbeda | `hasMismatch`/`referenceCount` SUDAH dihitung di **view** `ManagePackages.cshtml:70-81` (⚠️ di view, BUKAN controller — lihat Open Questions Q1). Reuse + live JS recompute. |
| SHUF-13 | Reminder visual di Post bila Pre OFF tapi Post ON (cek via `LinkedSessionId`); no auto-cascade (opsi Z) | `LinkedSessionId` ada (`AssessmentSession.cs:178`). Controller GET load Pre session → `ViewBag.PreShuffleQuestions`. Render kondisional di view (Post only). |
| SHUF-14 | Sembunyikan toggle untuk Proton Tahun 3 / Manual entry | `Category=="Assessment Proton" && TahunKe=="Tahun 3"` (pola `:3370`) ATAU `IsManualEntry==true` (`:137`). Controller flag `ViewBag.HideShuffleToggle` → `@if` wrap seluruh card. |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia** untuk SEMUA copy (mandat CLAUDE.md). Copy toggle/warning/reminder sudah di-spec verbatim di 374-UI-SPEC — implementasi harus persis.
- **Develop Workflow:** verifikasi lokal WAJIB sebelum commit — `dotnet build` + `dotnet run` (cek `http://localhost:5277`) + cek DB lokal + Playwright bila ada. ❌ Jangan edit kode/DB langsung di Dev/Prod. ❌ Jangan push tanpa verifikasi lokal. Migration flag (= **false** phase ini) WAJIB dinotifikasi ke IT saat handoff.
- **Seed Workflow:** bila butuh seed data testing → klasifikasi (temporary/permanent) → snapshot DB → catat `docs/SEED_JOURNAL.md` → restore setelah test. Test real-SQL Phase 374 pakai disposable DB per-fixture (`HcPortalDB_Test_<guid>`, auto EnsureDeletedAsync) — tidak menyentuh DB kerja, tidak butuh seed manual.

## Standard Stack

Stack **fixed** — tidak ada riset alternatif (instruksi orchestrator + 374-UI-SPEC §Design System).

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 (net8.0) | Controller + Razor server-render | Stack project, `[VERIFIED: HcPortal.Tests.csproj TargetFramework net8.0]` |
| Bootstrap | 5.3 (vendored admin theme) | Card, form-switch, alert, btn | Sudah dipakai di seluruh halaman admin |
| Bootstrap Icons | `bi-*` | `bi-shuffle`, `bi-lock-fill`, `bi-exclamation-triangle`, `bi-question-circle`, `bi-save` | Sudah di-load layout existing |
| EF Core + SQL Server | 8.0.0 | Query sibling/assignment, propagate, audit | `[VERIFIED: HcPortal.Tests.csproj Microsoft.EntityFrameworkCore.SqlServer 8.0.0]` |
| xUnit | 2.9.3 | Test endpoint guard + propagate | `[VERIFIED: HcPortal.Tests.csproj xunit 2.9.3]` |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `Helpers/ShuffleEngine.cs` | Phase 373 | Pure shuffle engine | **TIDAK disentuh Phase 374** — engine sudah jadi. Hanya referensi konseptual. |
| `Services/AuditLogService.cs` | existing | `LogAsync(...)` | Audit `UpdateShuffleSettings` (try/catch warn-only) |
| Vanilla JS (no framework) | — | Live warning recompute | `@section Scripts` di view (pola `confirmDeletePackage` `:261-272`) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Form POST + PRG (D-01a) | AJAX Json (pola Reshuffle) | Json butuh JS handler + DOM update manual; PRG = full reload TempData. **D-01a LOCK = PRG** karena explicit-save form. Reshuffle pakai Json karena dipanggil tombol AJAX, beda interaksi. |
| ViewBag (pola halaman ini) | Strongly-typed ViewModel | ManagePackages 100% ViewBag-driven (`ViewBag.Packages`, `IsPostSession`, dst). Konsisten = ViewBag. VM = inkonsisten, lebih banyak kerja. |

**Installation:** Tidak ada paket baru. Semua dependency sudah ada.

**Version verification:** Tidak ada paket baru ditambahkan di phase ini → tidak ada `npm`/`dotnet add package`. Versi runtime di-verifikasi via `HcPortal.Tests.csproj` (net8.0, EF Core 8.0.0, xunit 2.9.3) `[VERIFIED: Read HcPortal.Tests/HcPortal.Tests.csproj]`.

## Architecture Patterns

### System Architecture Diagram

```
ADMIN/HC membuka /Admin/ManagePackages?assessmentId=N
        │
        ▼
┌─────────────────────────────────────────────────────────────┐
│ AssessmentAdminController.ManagePackages(int) GET  [~:5264]   │
│  ├─ load assessment + packages (existing)                     │
│  ├─ [NEW] hitung siblingSessionIds (Title+Category+Sched.Date)│
│  ├─ [NEW] IsShuffleLocked = any sibling.StartedAt!=null       │
│  │         OR any UserPackageAssignment in sibling group      │
│  ├─ [NEW] HideShuffleToggle = (Proton Th3) OR IsManualEntry   │
│  ├─ [NEW] ShuffleQuestions/ShuffleOptions (dari assessment)   │
│  └─ [NEW] if Post: PreShuffleQuestions via LinkedSessionId    │
│       └─ ViewBag.* → View()                                   │
└─────────────────────────────────────────────────────────────┘
        │  ViewBag
        ▼
┌─────────────────────────────────────────────────────────────┐
│ Views/Admin/ManagePackages.cshtml                             │
│  @if (!HideShuffleToggle) {                                   │
│   ┌─── Card "Pengacakan Soal & Jawaban" ──────────────────┐  │
│   │ [lock alert-info]  (if IsShuffleLocked)               │  │
│   │ <form POST UpdateShuffleSettings + AntiForgeryToken>  │  │
│   │   switch Acak Soal      (disabled if locked)          │  │
│   │   switch Acak Pilihan   (disabled if locked)          │  │
│   │   [reminder alert-warning] (if Post && PreOFF&&PostON)│  │
│   │   [warning §9 alert-warning] (live JS, hidden init)   │  │
│   │   <button Simpan> (disabled if locked)                │  │
│   │ </form>                                                │  │
│   └────────────────────────────────────────────────────────┘ │
│  }   @section Scripts: live recompute warning on flip+load   │
└─────────────────────────────────────────────────────────────┘
        │  form submit (klik Simpan)
        ▼
┌─────────────────────────────────────────────────────────────┐
│ [NEW] UpdateShuffleSettings(int assessmentId,                 │
│        bool shuffleQuestions, bool shuffleOptions) POST        │
│  [Authorize(Roles="Admin, HC")] [ValidateAntiForgeryToken]    │
│  ├─ load assessment → 404 if null                             │
│  ├─ siblingSessionIds (Title+Category+Sched.Date)             │
│  ├─ RE-CHECK lock (started OR assignment) ── locked? ──┐      │
│  │                                                      ▼      │
│  │                                  TempData["Error"] + Redirect (no write)
│  ├─ foreach sibling: set ShuffleQuestions/ShuffleOptions       │
│  ├─ SaveChangesAsync()                                         │
│  ├─ AuditLogService.LogAsync(...) try/catch warn-only          │
│  └─ TempData["Success"] + RedirectToAction(ManagePackages)     │
└─────────────────────────────────────────────────────────────┘
```

### Recommended Project Structure
Tidak ada file/folder baru. Edit in-place:
```
Controllers/AssessmentAdminController.cs   # +UpdateShuffleSettings POST; enrich ManagePackages GET ViewBag
Views/Admin/ManagePackages.cshtml          # +card Pengacakan (setelah L68, sebelum L83) +JS di @section Scripts
HcPortal.Tests/Shuffle*.cs                 # +test endpoint guard-reject + propagate (real-SQL, pola existing)
```

### Pattern 1: Sibling-propagate foreach (mirror EditAssessment)
**What:** Update field shared ke SEMUA baris sibling grup `(Title, Category, Schedule.Date)`.
**When to use:** Body `UpdateShuffleSettings` POST.
**Example:**
```csharp
// Source: [VERIFIED: Controllers/AssessmentAdminController.cs:2019-2045 EditAssessment POST]
var siblings = await _context.AssessmentSessions
    .Where(a => a.Title == origTitle
             && a.Category == origCategory
             && a.Schedule.Date == origScheduleDate)
    .ToListAsync();
var now = DateTime.UtcNow;
foreach (var sibling in siblings)
{
    sibling.ShuffleQuestions = model.ShuffleQuestions;   // ← Phase 374: hanya 2 field ini
    sibling.ShuffleOptions   = model.ShuffleOptions;
    sibling.UpdatedAt = now;
}
await _context.SaveChangesAsync();
```
Catatan: `EditAssessment` sudah men-set `ShuffleQuestions`/`ShuffleOptions` di foreach-nya (`:2040-2041`) — `UpdateShuffleSettings` adalah versi **dedicated 2-field** dari pola identik. Jangan duplikasi seluruh EditAssessment; cukup query sibling + set 2 field.

### Pattern 2: Lock-condition detection (mirror ReshufflePackage)
**What:** Tentukan apakah ada peserta sudah mulai di grup sibling.
**When to use:** ManagePackages GET (→ViewBag) DAN endpoint POST (re-check) — defense-in-depth.
**Example:**
```csharp
// Source: [VERIFIED: Controllers/AssessmentAdminController.cs:5086-5104 + :5278-5282]
var siblingSessionIds = await _context.AssessmentSessions
    .Where(s => s.Title == assessment.Title &&
                s.Category == assessment.Category &&
                s.Schedule.Date == assessment.Schedule.Date)
    .Select(s => s.Id)
    .ToListAsync();

// (a) ada peserta sudah mulai
bool anyStarted = await _context.AssessmentSessions
    .AnyAsync(s => siblingSessionIds.Contains(s.Id) && s.StartedAt != null);

// (b) ada UserPackageAssignment di grup (paket grup)
//     packageIds = paket milik sibling group; assignment.AssessmentSessionId ∈ siblingSessionIds
bool anyAssignment = await _context.UserPackageAssignments
    .AnyAsync(a => siblingSessionIds.Contains(a.AssessmentSessionId));

bool isShuffleLocked = anyStarted || anyAssignment;
```
⚠️ **Konsistensi siblingSessionIds:** spec §3 + `ReshufflePackage:5113` menegaskan worker-index/sibling-set HARUS pakai key `(Title, Category, Schedule.Date)` yang sama dengan StartExam. Pakai key yang identik di GET dan POST.

### Pattern 3: Form POST + PRG + TempData (D-01a)
**What:** Form submit → endpoint → RedirectToAction → TempData ditampilkan.
**When to use:** Form card + return endpoint.
**Example (view form):**
```html
<!-- Source: [VERIFIED: Views/Admin/ManagePackages.cshtml:52-56 CopyPackagesFromPre form pattern] -->
<form method="post" asp-action="UpdateShuffleSettings" asp-controller="AssessmentAdmin">
    @Html.AntiForgeryToken()
    <input type="hidden" name="assessmentId" value="@ViewBag.AssessmentId" />
    <!-- switches with name="shuffleQuestions" / name="shuffleOptions" -->
    <button type="submit" class="btn btn-primary"><i class="bi bi-save me-1"></i>Simpan Pengaturan</button>
</form>
```
**Example (endpoint return):**
```csharp
// Source: [VERIFIED: pola RedirectToAction + TempData di controller halaman ini]
TempData["Success"] = "Pengaturan pengacakan berhasil disimpan.";   // copy 374-UI-SPEC verbatim
return RedirectToAction("ManagePackages", new { assessmentId });
```
TempData di-render oleh blok existing `ManagePackages.cshtml:19-27` (Success=alert-success, Error=alert-danger) — **tidak perlu blok baru**, sudah ada.

### Pattern 4: Audit log try/catch warn-only (mirror Reshuffle)
**What:** Tulis audit; jangan gagalkan operasi bila audit error.
**Example:**
```csharp
// Source: [VERIFIED: Controllers/AssessmentAdminController.cs:5132-5144 ReshufflePackage]
try
{
    var hcUser = await _userManager.GetUserAsync(User);
    var actorNameStr = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
    await _auditLog.LogAsync(
        hcUser?.Id ?? "",
        actorNameStr,
        "UpdateShuffleSettings",
        $"Set Acak Soal={shuffleQuestions}, Acak Pilihan={shuffleOptions} for assessment '{assessment.Title}' [grup {siblingSessionIds.Count} sesi]",
        assessmentId,
        "AssessmentSession");
}
catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for UpdateShuffleSettings (assessmentId={Id})", assessmentId); }
```

### Pattern 5: form-switch toggle (reuse verbatim dari CreateAssessment)
**What:** Markup switch + label + help-text.
**Example:**
```html
<!-- Source: [VERIFIED: Views/Admin/CreateAssessment.cshtml:525-534] — copy 372-UI-SPEC verbatim -->
<div class="form-check form-switch mb-2">
    <input class="form-check-input" type="checkbox" name="shuffleQuestions" id="shuffleQuestions"
           value="true" @(ViewBag.ShuffleQuestions == true ? "checked" : "") @(isLocked ? "disabled" : "") />
    <label class="form-check-label" for="shuffleQuestions">Acak Soal</label>
</div>
<div class="form-text text-muted mb-2">Saat aktif, urutan dan pemilihan soal diacak berbeda untuk setiap peserta — sehingga peserta yang duduk berdekatan kecil kemungkinan mendapat soal yang sama urutannya. Saat nonaktif, semua peserta mendapat soal yang sama dengan urutan yang sama persis (urutan asli sesuai paket soal).</div>
```
⚠️ **Checkbox + bool binding gotcha** (lihat Pitfall 3): `CreateAssessment` pakai `asp-for` (Tag Helper auto-render hidden fallback). Di card ManagePackages BUKAN strongly-typed view → pakai `name="shuffleQuestions"` manual. Tambahkan `<input type="hidden" name="shuffleQuestions" value="false" />` SEBELUM checkbox bila perlu false-fallback, ATAU andalkan default param `bool`=false saat unchecked. Endpoint signature `bool shuffleQuestions` MVC binder: checkbox unchecked → tidak terkirim → bind ke `false`. Verifikasi pola exact di plan/implement.

### Anti-Patterns to Avoid
- **Json return untuk UpdateShuffleSettings:** D-01a LOCK = PRG, BUKAN Json. Reshuffle pakai Json karena AJAX; ini form-submit. Jangan campur.
- **Hitung mismatch ulang dari nol di JS/controller baru:** `hasMismatch`/`referenceCount` sudah ada (`ManagePackages.cshtml:70-81`). Reuse. (Lihat Open Questions Q1 — currently di view, perlu dijembatani ke JS.)
- **UI disabled sebagai satu-satunya guard:** SHUF-11 = server reject WAJIB (D-04a). Jangan andalkan `disabled` HTML saja (bisa di-bypass via curl/devtools).
- **Hide diperlakukan sebagai disabled:** Hide (Proton Th3/Manual) = card TIDAK dirender sama sekali (`@if`). Started = card TAMPIL tapi disabled. Dua perilaku BEDA (UI-SPEC Interaction Contract).
- **Lock shuffle dari `IsSamePackageLocked`:** SamePackage lock HANYA isi paket. Toggle shuffle independen — lock HANYA dari started/assignment.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Sibling group resolution | Query baru ad-hoc | Key `(Title, Category, Schedule.Date)` pola `:5086-5091` | Konsistensi dgn StartExam/Reshuffle/EditAssessment; key lain = drift |
| Propagate ke sibling | Loop manual baru | `foreach siblings` pola `EditAssessment:2028-2045` | Pola teruji, sudah set 2 field shuffle yang sama |
| Lock detection | Logika started baru | `StartedAt!=null` + `UserPackageAssignment` AnyAsync pola Reshuffle | Identik dgn reshuffle-guard (konsep lock sama, D-04a) |
| Audit log | Insert AuditLog manual | `AuditLogService.LogAsync(...)` try/catch | Signature stabil `[VERIFIED: Services/AuditLogService.cs:21-42]`; auto SaveChanges |
| Lock alert markup | Alert baru | Copy `ManagePackages.cshtml:29-44` (alert-info + bi-lock-fill) | HC kenali pola "terkunci" konsisten |
| TempData alert render | Blok alert baru | Sudah ada `:19-27` | Jangan duplikasi |
| Toggle markup + copy | Tulis copy baru | Copy `CreateAssessment.cshtml:525-534` verbatim | 372-UI-SPEC kontrak; frasa grading WAJIB persis |
| Mismatch computation | Hitung ulang | `hasMismatch`/`referenceCount` `ManagePackages.cshtml:70-81` | Sudah ada (lihat Q1) |

**Key insight:** Phase 374 ≈ 90% copy-paste-adapt dari 4 pola existing dalam file yang SAMA (`AssessmentAdminController.cs`, `ManagePackages.cshtml`, `CreateAssessment.cshtml`). Nilai phase = menggabungkan benar + menjaga konsistensi key sibling + defense-in-depth, BUKAN inovasi.

## Common Pitfalls

### Pitfall 1: Line-number drift (v25.0 367/368 file-overlap)
**What goes wrong:** Plan/implement pakai line number stale → edit di tempat salah / anchor tak ketemu.
**Why it happens:** `AssessmentAdminController.cs` = area sibuk v25.0 (367/368 SHIPPED LOCAL). CONTEXT.md + 373-CONTEXT.md DUA-DUANYA mewanti-wanti drift. Line di research ini valid **per sesi 2026-06-13** tapi bisa bergeser.
**How to avoid:** **Re-grep SEMUA symbol di execute-time** (`ManagePackages`, `ReshufflePackage`, `EditAssessment` propagate foreach, TempData blok view). Gunakan symbol/anchor-string, bukan line absolut.
**Warning signs:** Edit gagal "string not found"; konteks sekitar tidak cocok deskripsi.

### Pitfall 2: Lock-condition divergen GET vs POST
**What goes wrong:** UI bilang editable tapi POST tolak (atau sebaliknya) → confusing UX / security gap.
**Why it happens:** Dua tempat menghitung lock (GET ViewBag + POST guard) dengan logika berbeda.
**How to avoid:** Ekstrak SATU helper pure/private (`bool IsShuffleLocked(...)` atau hitung `siblingSessionIds` + `anyStarted || anyAssignment` identik). Sama key sibling, sama predikat. (Ini juga membuat lock-condition unit-testable — lihat Validation Architecture.)
**Warning signs:** Test guard-reject pass tapi UI tidak disabled untuk kondisi sama.

### Pitfall 3: EF/MVC checkbox bool false-trap
**What goes wrong:** Toggle OFF (unchecked) → endpoint terima `false` benar, TAPI bila pakai hidden-fallback salah posisi → selalu `false`/selalu `true`.
**Why it happens:** HTML checkbox unchecked = TIDAK terkirim. MVC `bool` param unchecked → `false` (OK). Tapi bila tambah `<input type="hidden" value="false">` SETELAH checkbox, atau name kembar salah, binding kacau. Spec §4 ("EF bool trap") sudah angkat isu ini untuk create-path; di sini konteksnya endpoint param binding, bukan model default.
**How to avoid:** Endpoint param `bool shuffleQuestions, bool shuffleOptions` → checkbox `name` cocok param, unchecked→false default binder. Verifikasi via test guard ATAU manual UAT (Phase 375). Render `checked` dari `ViewBag.ShuffleQuestions` (saved state), bukan default browser.
**Warning signs:** Simpan OFF tapi reload tampil ON; semua sibling selalu false.

### Pitfall 4: Mismatch data tidak tersedia untuk JS (currently in-view)
**What goes wrong:** D-03a minta live JS recompute pakai `hasMismatch`, tapi `hasMismatch` dihitung di Razor view (`:70-81`), bukan controller ViewBag → JS tak punya data terstruktur.
**Why it happens:** CONTEXT.md menulis "dihitung controller (≈L72-78)" — sebenarnya dihitung **di view** L70-81 (`[VERIFIED: Read ManagePackages.cshtml:70-81]`). Discrepancy.
**How to avoid:** Pilih satu: (a) pindahkan/duplikasi hitungan `hasMismatch` + `packageWithQuestionsCount` ke controller ViewBag agar bisa di-emit ke JS sebagai `data-*` attribute / inline var; ATAU (b) emit nilai Razor `@hasMismatch` + `@packagesWithQuestions` langsung ke JS var di `@section Scripts` (view sudah punya variabel di scope). Opsi (b) lebih ringan & sesuai "jangan hitung ulang". Plan harus eksplisit memilih.
**Warning signs:** Warning tidak muncul/hilang saat flip; JS `undefined` mismatch.

### Pitfall 5: Reminder Pre/Post salah arah / live alih-alih saved
**What goes wrong:** Reminder muncul di Pre, atau berubah saat HC flip toggle Post sebelum simpan.
**Why it happens:** Salah baca D-03b — reminder HANYA Post, HANYA saved-state.
**How to avoid:** Render hanya `@if (ViewBag.IsPostSession == true && ViewBag.PreShuffleQuestions == false && savedPostShuffleQuestions == true)`. SERVER-render, JANGAN ikut live JS. Pre `ShuffleQuestions` dibaca controller via `LinkedSessionId` → ViewBag.
**Warning signs:** Reminder di halaman Pre; reminder berubah saat klik toggle (harusnya statis sampai reload).

## Code Examples

### Reminder Pre/Post — controller GET enrich
```csharp
// Source: [VERIFIED: ManagePackages GET :5313-5318 already loads IsPostSession + PreSessionId via LinkedSessionId]
// Phase 374 extends: also load Pre's SAVED ShuffleQuestions for opsi-Z reminder.
if (isPostSession && assessment.LinkedSessionId.HasValue)
{
    ViewBag.PreSessionId = assessment.LinkedSessionId.Value;
    var preShuffle = await _context.AssessmentSessions
        .Where(s => s.Id == assessment.LinkedSessionId.Value)
        .Select(s => (bool?)s.ShuffleQuestions)
        .FirstOrDefaultAsync();
    ViewBag.PreShuffleQuestions = preShuffle;   // null bila Pre tak ada
}
ViewBag.ShuffleQuestions = assessment.ShuffleQuestions;   // saved Post state (render checked)
ViewBag.ShuffleOptions   = assessment.ShuffleOptions;
```

### Hide-toggle flag — controller GET
```csharp
// Source: [VERIFIED: pola Proton Th3 :3370 (TahunKe=="Tahun 3") + IsManualEntry :137]
ViewBag.HideShuffleToggle =
    (assessment.Category == "Assessment Proton" && assessment.TahunKe == "Tahun 3")
    || assessment.IsManualEntry;
```

### Live warning §9 — vanilla JS (di @section Scripts)
```javascript
// Source: pola @section Scripts ManagePackages.cshtml:261-272 (vanilla, no framework)
(function () {
  var sq = document.getElementById('shuffleQuestions');
  var warn = document.getElementById('shuffleSizeWarning');   // alert §9, hidden init via d-none
  if (!sq || !warn) return;
  var hasMismatch = @(((bool?)ViewBag.ShowSizeMismatchWarning ?? false) ? "true" : "false");
  // ATAU emit dari var Razor view: @hasMismatch + (packagesWithQuestions >= 2)
  function recompute() {
    var show = hasMismatch && !sq.checked;   // ≥2 paket sudah di-fold ke hasMismatch precondition
    warn.classList.toggle('d-none', !show);
  }
  sq.addEventListener('change', recompute);
  recompute();   // page load
})();
```
Catatan: precondition "≥2 paket-ber-soal" sebaiknya di-fold ke nilai `hasMismatch` yang di-emit (mismatch hanya mungkin bila ≥2 paket ber-soal — lihat `:74-77` `packages.Where(p=>p.Questions.Any())`). Plan finalisasi exact predikat.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Option shuffle hard-code `"{}"` di reshuffle | `ShuffleEngine.BuildOptionShuffle` gerbang flag | Phase 373 (SHUF-09) | **Sudah selesai** — Phase 374 tidak menyentuh reshuffle |
| Shuffle selalu ON (implicit) | Per-assessment toggle + propagate | Phase 372/373 | Phase 374 = UI untuk toggle yang enginenya sudah ada |
| Toggle hanya di CreateAssessment wizard | + ManagePackages (edit setelah create) | **Phase 374 (ini)** | HC bisa ubah shuffle pasca-create selama belum ada peserta mulai |

**Deprecated/outdated:**
- Non-package legacy exam path: MATI sejak Phase 227 CLEN-02 (`CMPController.cs:1161`). Semua ujian wajib paket → tidak relevan untuk hide-rule (hide-rule = Proton Th3/Manual, bukan non-package).

## Runtime State Inventory

> Phase 374 = greenfield UI + endpoint (TIDAK ada rename/refactor/migration). Tidak ada string runtime yang di-rename.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — tidak ada migration, kolom `ShuffleQuestions`/`ShuffleOptions` sudah live + ter-default ON sejak Phase 372 (`[VERIFIED: AssessmentSession.cs:38-42]`) | none |
| Live service config | None — tidak ada service eksternal disentuh | none |
| OS-registered state | None — tidak ada task/daemon | none |
| Secrets/env vars | None — tidak ada secret/env baru | none |
| Build artifacts | None — tidak ada paket/csproj baru, tidak ada migration; `dotnet build` cukup | none |

## Validation Architecture

> nyquist_validation = **true** (`[VERIFIED: .planning/config.json workflow.nyquist_validation]`). Section ini WAJIB.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (net8.0) `[VERIFIED: HcPortal.Tests.csproj]` |
| Config file | none — konvensi `HcPortal.Tests/*.cs`, fixture `IClassFixture<ProtonCompletionFixture>` (disposable SQL Server DB per-fixture) |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~Shuffle"` |
| Full suite command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (329/329 baseline saat ini per MEMORY Phase 373) |
| SQL-less subset | `dotnet test --filter "Category!=Integration"` (real-SQL test ber-`[Trait("Category","Integration")]`) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SHUF-10 | Endpoint propagate flag ke SEMUA sibling grup | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~Shuffle"` | ⚠️ partial — `ShufflePropagationTests` cover propagate INVARIANT; Phase 374 tambah test replika `UpdateShuffleSettings` foreach (pola identik) → **Wave 0 / new test** |
| SHUF-11 | Lock-guard: tolak perubahan saat ada peserta mulai (started OR assignment) | unit (pure helper) + integration | `dotnet test --filter "FullyQualifiedName~Shuffle"` | ❌ **Wave 0** — test baru `ShuffleLockGuardTests`: (a) pure `IsShuffleLocked` decision (started true / assignment true / clean false), (b) real-SQL guard-reject (POST-replica tidak menulis saat locked) |
| SHUF-12 | Warning logic: ≥2 paket-ber-soal + Acak Soal OFF + mismatch → tampil | unit (pure) ATAU manual-only | `dotnet test --filter "..."` bila helper diekstrak | ⚠️ logika trivial (3 bool AND) — pure helper opsional; render live = Razor-runtime → Playwright **Phase 375** |
| SHUF-13 | Reminder Pre/Post: Pre OFF & Post ON → tampil di Post (saved-state) | manual-only / Razor-runtime | — (Razor conditional, butuh runtime) | ❌ render-conditional → **Playwright Phase 375**; logika data (Pre via LinkedSessionId) bisa di-cover real-SQL bila diinginkan |
| SHUF-14 | Hide toggle Proton Th3 / Manual entry | manual-only / Razor-runtime | — (`@if` render) | ❌ render-conditional → **Playwright Phase 375**; flag-computation pure-testable bila diekstrak |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "FullyQualifiedName~Shuffle"` (< 30s subset).
- **Per wave merge:** `dotnet test --filter "Category!=Integration"` (SQL-less cepat) + Shuffle subset.
- **Phase gate:** Full suite hijau (`dotnet test`, baseline 329/329) sebelum `/gsd-verify-work` + `dotnet run` cek `http://localhost:5277` (CLAUDE.md verifikasi lokal wajib).

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ShuffleLockGuardTests.cs` — covers SHUF-11: (a) pure decision `started||assignment` → locked; (b) real-SQL POST-replica reject saat locked (tak menulis), accept saat clean (menulis ke semua sibling). Pola `IClassFixture<ProtonCompletionFixture>` + `[Trait("Category","Integration")]`.
- [ ] `HcPortal.Tests/ShuffleUpdateEndpointTests.cs` (atau extend `ShufflePropagationTests`) — covers SHUF-10: replika `UpdateShuffleSettings` foreach atas grup REAL → assert SEMUA sibling ikut nilai POST (pola `ShufflePropagationTests.Propagation_Standard_AllSiblingsFollowModel`).
- [ ] (opsional) pure helper `IsShuffleLocked` + `ShouldHideShuffleToggle` + warning-predikat diekstrak → unit test cepat tanpa DB (SHUF-11/12/14 decision-logic). Membuat lock-condition testable sekaligus mencegah Pitfall 2 (divergensi GET/POST).

**Catatan pembagian (spec §11 + CONTEXT deferred):** Full mode-matrix engine + Playwright UAT (toggle ON/OFF + lock state + reminder + warning render) = **Phase 375** (SHUF-16). Phase 374 test = endpoint guard-reject + propagate (real-SQL) + pure decision-logic. Render-conditional (hide, reminder, warning visibility) = Razor-runtime → Playwright Phase 375; tidak dipaksakan jadi unit test rapuh di 374.

## Security Domain

> security_enforcement default = enabled. Endpoint baru = attack surface baru → relevan.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | `[Authorize(Roles="Admin, HC")]` pada endpoint (pola `ReshufflePackage:5063`) — hanya Admin/HC boleh ubah |
| V3 Session Management | no | Pakai cookie auth existing; tidak ada session handling baru |
| V4 Access Control | yes | Role-gated endpoint; lock-guard server-side mencegah ubah pasca-mulai (business authorization) |
| V5 Input Validation | yes | Param `int assessmentId, bool, bool` — type-safe binder; assessment null → 404. Tidak ada string user-input bebas |
| V6 Cryptography | no | Tidak ada crypto |
| CSRF | yes | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` di form (pola CopyPackagesFromPre `:53`) |

### Known Threat Patterns for ASP.NET MVC admin endpoint
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF pada state-changing POST | Tampering | `[ValidateAntiForgeryToken]` + token di form (WAJIB, locked) |
| Bypass UI-disabled via direct POST (curl/devtools) saat locked | Tampering / Elevation | **Server-side lock re-check di endpoint** (D-04a defense-in-depth) → TempData error, no write. SHUF-11 acceptance. |
| Privilege escalation (non-Admin ubah shuffle) | Elevation | `[Authorize(Roles="Admin, HC")]` |
| IDOR (`assessmentId` milik orang lain) | Tampering | Admin/HC = admin role global (pola existing reshuffle/edit tidak per-owner-check; konsisten). Tidak ada per-user ownership di assessment admin. |
| Audit-log gap | Repudiation | `AuditLogService.LogAsync(...)` actor NIP-FullName + targetId (try/catch warn-only, tidak gagalkan operasi) |

## Open Questions

1. **Mismatch (`hasMismatch`) dihitung di VIEW, bukan controller.**
   - What we know: `ManagePackages.cshtml:70-81` menghitung `hasMismatch`/`referenceCount` inline di Razor. CONTEXT.md/UI-SPEC menyebut "dihitung controller (≈L72-78)" — **discrepancy** (sebenarnya di view).
   - What's unclear: Apakah live JS sebaiknya baca dari Razor var (`@hasMismatch`) langsung di `@section Scripts`, atau pindahkan hitungan ke controller ViewBag.
   - Recommendation: **Emit dari Razor var di `@section Scripts`** (opsi b, Pitfall 4) — paling ringan, tidak menggeser logika, sesuai "jangan hitung ulang dari nol". Plan tegaskan pilihan ini agar executor tak bingung. (Card disisipkan SETELAH blok `@{...}` L70-81 sehingga var dalam scope — verifikasi urutan saat implement.)

2. **Checkbox→bool binding exact (Pitfall 3).**
   - What we know: `CreateAssessment` pakai `asp-for` (Tag Helper). Card ManagePackages bukan strongly-typed view → `name=` manual.
   - What's unclear: Perlu hidden-fallback `value="false"` atau cukup andalkan binder default (unchecked→false)?
   - Recommendation: Andalkan binder default (`bool` param, unchecked tidak terkirim → false). Konfirmasi via test guard SHUF-11 yang juga assert nilai tersimpan, ATAU manual UAT. Plan boleh tetapkan hidden-fallback bila ragu.

3. **`UpdatedAt` ada di model?**
   - What we know: `EditAssessment:2044` set `sibling.UpdatedAt = now`. Belum di-verifikasi `AssessmentSession.UpdatedAt` ada (kemungkinan besar ada karena EditAssessment memakainya).
   - Recommendation: Re-grep `UpdatedAt` di `AssessmentSession.cs` saat implement; bila ada, set untuk konsistensi audit-trail; bila tidak, skip (non-blocking).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build + test + run | ✓ (asumsi — project aktif) | net8.0 | — |
| SQL Server (localhost\SQLEXPRESS) | real-SQL test (`ProtonCompletionFixture`) + DB lokal | ✓ (dipakai test existing 329/329) | — | unit pure helper tetap jalan tanpa SQL (`--filter "Category!=Integration"`) |
| Playwright | UAT render-conditional | n/a Phase 374 | — | Deferred Phase 375 (SHUF-16) |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** Playwright UAT bukan scope 374 — di-defer ke 375; tidak memblok.

> Verifikasi runtime (dotnet/SQL Server up) dilakukan di execute-time per CLAUDE.md "verifikasi lokal wajib"; baseline test 329/329 (MEMORY Phase 373) menunjukkan SQL Server lokal fungsional. `[ASSUMED]` SDK/SQL tersedia — terbukti dari riwayat phase sebelumnya, bukan probe sesi ini.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Binder MVC: checkbox unchecked → `bool` param `false` (tanpa hidden-fallback) cukup | Pitfall 3 / Q2 | Toggle OFF tak tersimpan; mitigasi: test guard assert nilai + manual UAT |
| A2 | `hasMismatch` precondition sudah implikasikan ≥2 paket-ber-soal (mismatch mustahil dgn 1 paket) | Code Examples (JS) | Warning muncul salah; mitigasi: emit `packagesWithQuestions>=2` eksplisit ke JS |
| A3 | `AssessmentSession.UpdatedAt` ada (dipakai EditAssessment:2044) | Open Q3 | Compile error bila tak ada; mitigasi: re-grep saat implement, non-blocking |
| A4 | .NET SDK + SQL Server lokal tersedia di execute-time | Environment | Test/build gagal; mitigasi: terbukti dari baseline 329/329 phase 373 |
| A5 | `_logger` + `_userManager` + `_auditLog` ter-inject di `AssessmentAdminController` (dipakai Reshuffle:5134-5144) | Patterns 4 | — terbukti dipakai di method existing file yang sama; risiko ~0 |

## Sources

### Primary (HIGH confidence)
- `[VERIFIED]` `Controllers/AssessmentAdminController.cs` — ManagePackages GET (:5264-5324), ReshufflePackage (:5062-5147), ReshuffleAll (:5150-5240), EditAssessment propagate foreach (:2019-2055), audit pattern (:5132-5144), AssignmentCounts (:5278-5283)
- `[VERIFIED]` `Views/Admin/ManagePackages.cshtml` — header (1-17), TempData alerts (19-27), SamePackage lock (29-44), CopyFromPre form (46-68), mismatch compute IN-VIEW (70-81), summary panel (83-114), package list lock-gate (170,237), @section Scripts (261-272)
- `[VERIFIED]` `Views/Admin/CreateAssessment.cshtml:505-535` — form-switch toggle + copy verbatim (Token + Shuffle blocks)
- `[VERIFIED]` `Services/AuditLogService.cs:9-43` — LogAsync signature
- `[VERIFIED]` `Models/AssessmentSession.cs` — ShuffleQuestions/Options (38-42), TahunKe (108), IsManualEntry (137), AssessmentType (161), LinkedSessionId (178), SamePackage (191)
- `[VERIFIED]` `Helpers/ShuffleEngine.cs:1-40` — engine sudah ada (TIDAK disentuh)
- `[VERIFIED]` `HcPortal.Tests/ShufflePropagationTests.cs` — pola real-SQL propagate test (template SHUF-10 test)
- `[VERIFIED]` `HcPortal.Tests/ProtonCompletionServiceTests.cs:25-61` — ProtonCompletionFixture (disposable SQL DB pattern)
- `[VERIFIED]` `HcPortal.Tests/HcPortal.Tests.csproj` — xUnit 2.9.3, EF Core 8.0.0, net8.0
- `[CITED]` `docs/superpowers/specs/2026-06-13-shuffle-toggle-design.md §2/§3/§5/§7/§7.1/§9/§11/§12/§13`
- `[CITED]` `.planning/phases/374-ui-managepackages-lock-pre-post/374-CONTEXT.md` (D-01..D-04 + carry-forward)
- `[CITED]` `.planning/phases/374-ui-managepackages-lock-pre-post/374-UI-SPEC.md` (approved — copy + interaction contract)
- `[CITED]` `.planning/REQUIREMENTS.md:68-72` (SHUF-10..14)
- `[VERIFIED]` `.planning/config.json` — nyquist_validation=true, ui_phase=true

### Secondary (MEDIUM confidence)
- `[VERIFIED via grep]` Proton Th3 pattern `AssessmentAdminController.cs:3370` (`TahunKe == "Tahun 3"`), `:941/:1168` (`Category == "Assessment Proton"`)

### Tertiary (LOW confidence)
- None — semua klaim arsitektural di-verifikasi via Read/Grep terhadap codebase aktual sesi ini.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — fixed stack, semua dependency terverifikasi di csproj, tidak ada paket baru.
- Architecture/patterns: HIGH — keempat pola (propagate, lock-detect, PRG, audit, form-switch) di-Read langsung di file aktual; Phase 374 = kombinasi pola existing.
- Pitfalls: HIGH-MEDIUM — drift & defense-in-depth terverifikasi dari CONTEXT; checkbox-binding & mismatch-in-view ditandai Open Questions untuk plan-time finalisasi.
- Validation: HIGH — test infra (`ShufflePropagationTests`, `ProtonCompletionFixture`) sudah ada sebagai template; pembagian 374 vs 375 sesuai spec §11.

**Research date:** 2026-06-13
**Valid until:** Line numbers — re-grep WAJIB di execute-time (v25.0 367/368 drift). Pola/keputusan arsitektur — stabil (~30 hari).
