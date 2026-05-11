# Phase 306: Score Editable per Question Type — Research

**Researched:** 2026-04-28
**Domain:** ASP.NET Core MVC form validation + audit logging + Bootstrap modal UX
**Confidence:** HIGH

## Summary

Phase 306 melepaskan restriksi `scoreValue=10` untuk soal MultipleChoice/MultipleAnswer (sebelumnya only Essay editable). Investigasi codebase menunjukkan bahwa **semua infrastruktur yang dibutuhkan sudah tersedia dan production-tested**:

1. **Audit log pattern** sudah established di 9+ call sites di `AssessmentAdminController.cs` dengan signature `_auditLog.LogAsync(userId, actorName, actionType, description, targetId?, targetType?)` — pattern try/catch fallback `_logger.LogWarning(auditEx, ...)` verified di line 1342 dan 2015.
2. **Modal warning pattern** sudah ada di `ManagePackageQuestions.cshtml` line 237-253 (`editTypeWarningModal` "Peringatan Ubah Tipe Soal") — phase 306 dapat replikasi struktur HTML+JS untuk modal "Peringatan Ubah Skor".
3. **Scoring formula** di `Services/GradingService.cs:82-110` dan `Controllers/CMPController.cs:1646-1699` sudah robust untuk varied ScoreValue — tidak butuh change. **Verified via grep:** Zero downstream caller yang assume `ScoreValue == 10` (semua caller pakai sebagai variable arithmetic).
4. **DB constraint** existing `CK_AssessmentQuestion_ScoreValue: [ScoreValue] > 0` (Migration `20260212110029`) — TIDAK ada upper bound. Range 1–100 enforcement HARUS di app-layer (D-12 confirmed: HTML5 + server-side).
5. **ImportPackageQuestions hardcode `ScoreValue = 10`** di line 4482 — Excel template HEADERS ada 9 kolom (Pertanyaan/A/B/C/D/Correct/Elemen/QuestionType/Rubrik), TIDAK ADA kolom ScoreValue. Phase 306 SHOULD defer Excel import scoreValue acceptance (per CONTEXT.md deferred section) — UI-form-only scope.

**Primary recommendation:** Implementasi minimal-change linear sequence — 1 plan tunggal dengan 6-8 tasks, mengikuti pattern Phase 305 yang baru selesai. Tidak perlu helper class baru atau service extraction.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Form UX (D-01..D-05):**
- D-01: Default value input baru tetap `value="10"` untuk semua tipe (MC/MA/Essay). Familiar untuk user existing, minimal disruption ke UX.
- D-02: Saat user switch tipe via dropdown (Essay→MC, MC→MA, dll), nilai user-entered DI-PRESERVE (tidak reset ke 10). Hapus baris `if (qtype !== 'Essay') scoreInput.value = 10;` di line ~298.
- D-03: Hapus attribute `disabled` dari input scoreValue di line 186 (`<input ... value="10" min="1" max="100" disabled />` → `<input ... value="10" min="1" max="100" step="1" required />`).
- D-04: Hapus juga baris JS `scoreInput.disabled = (qtype !== 'Essay');` di line ~297. Input selalu enabled untuk semua tipe.
- D-05: Help text `<div id="scoreHelp">` line 187 dan baris JS line ~308-309 yang ubah text per tipe → ganti jadi static text **"Range 1–100"**. Hapus logika dynamic per-tipe wording. (Atau drop entirely jika label form `Nilai Soal (1–100)` sudah cukup. Implementasi pilih salah satu — Claude's Discretion.)

**AuditLog UX & gating (D-06..D-11):**
- D-06: Saat user submit EditQuestion AND `newScoreValue != oldScoreValue` AND ada `PackageUserResponses` rows untuk soal tersebut → tampilkan **modal warning + confirm** sebelum POST.
- D-07: Modal text: *"Skor soal #{Order} akan diubah dari **{old}** menjadi **{new}**. **{N} peserta** sudah menjawab — persentase mereka akan dihitung ulang otomatis. Lanjutkan?"*
- D-08: Modal buttons: "Ya, Lanjutkan" (btn-primary) + "Batal" (btn-secondary). Konsisten dengan modal pattern existing di ManagePackageQuestions (Peringatan Ubah Tipe Soal di **line 237-253**, button class existing `btn-warning` — CD-02 delegasi pemilihan class final ke implementer).
- D-09: Implementation: server pass `data-original-score` + `data-affected-sessions` attribute ke form Edit. Client-side: form submit handler check delta + count, trigger modal jika condition met.
- D-10: Audit log entry MANDATORY di server-side EditQuestion (line ~4822) saat scoreValue change: `await _auditLog.LogAsync(currentUser.Id, actorName, "EditQuestion-ScoreChange", $"Question #{q.Id} (Order {q.Order}, Package #{packageId}) ScoreValue: {oldScore} → {scoreValue} ({affectedSessionsCount} sessions affected)");`. Pakai pattern existing yang sudah established (line 326, 378, 1292, 1794, dll).
- D-11: Audit log entry juga dibuat untuk CreateQuestion non-default score (mis. admin set score=15 saat create) — tapi WITHOUT modal (no existing session to recalculate). Format: `"CreateQuestion: Question added with custom ScoreValue={scoreValue} (default 10) for Package #{packageId}"`.

**Validation error UX (D-12..D-15):**
- D-12: **Both layered defense in depth:** HTML5 client-side (`min="1" max="100" step="1" required`) + server-side explicit check.
- D-13: Server-side check di CreateQuestion (line ~4681) DAN EditQuestion (line ~4822):
  ```csharp
  if (scoreValue < 1 || scoreValue > 100)
  {
      TempData["Error"] = "Nilai soal harus antara 1 dan 100.";
      return RedirectToAction("ManagePackageQuestions", new { packageId });
  }
  ```
- D-14: **HAPUS** baris existing line 4681-4682 (CreateQuestion) dan 4822-4823 (EditQuestion):
  - `if (questionType != "Essay") scoreValue = 10;` → REMOVE (over-restrictive — phase 306 hapus)
  - `if (scoreValue <= 0) scoreValue = 10;` → REMOVE (over-permissive — silently coerce invalid input ke default tanpa user awareness; ganti dengan range check yang reject)
- D-15: Error message konsisten Bahasa Indonesia, format flash error pakai TempData["Error"] (sama dengan validation error existing untuk correctCount + Essay rubrik).

**Existing data + Total possible score impact (D-16..D-19):**
- D-16: **NO backfill DB.** Existing 50 MC + 8 MA dengan ScoreValue=10 dibiarkan as-is (sudah explicit, bukan NULL — verified via sqlcmd query). Admin bisa edit per-soal sesuai kebutuhan.
- D-17: **Tampilkan "Total Points" di header list** ManagePackageQuestions.cshtml **line 42** (existing `<span class="fw-semibold">Daftar Soal (@questions.Count soal)</span>` → `<span class="fw-semibold">Daftar Soal (@questions.Count soal • Total @questions.Sum(q => q.ScoreValue) poin)</span>`). Computed inline dari `questions.Sum(q => q.ScoreValue)` (variabel local `questions` sudah ada di view scope).
- D-18: **No formula change.** Scoring formula `finalPercentage = (totalScore / maxScore) * 100` di `CMPController.cs:1705` sudah robust untuk varied score. Tidak perlu adjustment di SubmitExam atau ExamSummary atau CertificatePdf.
- D-19: **No retroactive rescore.** Saat admin edit score soal yang sudah punya completed sessions, **percentage di stored di AssessmentSessions.Score** TIDAK auto-recalculate (per architecture existing — Score di-persist saat SubmitExam). Modal warning di D-07 menjelaskan dampak hanya untuk session **future** yang akan menjawab soal ini, plus session yang sedang InProgress (belum SubmitExam) — Completed sessions retain their stored Score.

### Claude's Discretion

- CD-01: Help text exact wording — "Range 1–100" vs "Skor 1–100 (default 10)" vs drop entirely. Pilih saat plan/execute berdasarkan visual fit.
- CD-02: Modal CSS styling — reuse existing `peringatan-ubah-tipe` modal pattern atau pakai bootstrap modal generik. Implementer pilih based on existing assets.
- CD-03: "Total Points" exact format — `• Total 30 poin` vs `(30 pts)` vs `— 30 pts total`. Pilih yang konsisten dengan visual style ManagePackageQuestions header.
- CD-04: Apakah tambah `[Range(1, 100)]` data annotation di parameter signature di addition to inline check (D-13). Jika MVC convention demand, pakai. Jika tidak konflik dengan inline check style, skip.
- CD-05: Apakah CreateQuestion juga perlu detect non-default score creation (D-11) sebagai informational audit, atau cukup edit-only audit (D-10). Implementer pilih based on audit verbosity tolerance.

### Deferred Ideas (OUT OF SCOPE)

- **Bulk-set score** (semua soal di package set ScoreValue=X sekaligus) — T2 differentiator audit, defer ke roadmap backlog v16+.
- **Per-question elemen teknis weighted score** — beyond MVP audit fix.
- **Re-score historical Completed sessions** — risk-heavy (Pass↔Fail flip historical), butuh business approval. Defer ke separate phase jika ada permintaan eksplisit.
- **Excel import scoreValue range validation** — perlu di-verify apakah ImportQuestions juga perlu accept varied score. Jika butuh, tambah ke phase 306 saat planning, atau spawn sub-task. Default: ImportQuestions tetap accept current behavior (MC/MA→10, Essay varied) sebelum confirmed need to update. **Riset finding (lihat ImportQuestions Decision section): defer = correct choice (Excel template tidak punya kolom ScoreValue, current behavior hardcode `ScoreValue = 10` di line 4482 untuk semua tipe).**
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **QSCR-01** | Admin/HC dapat menyimpan skor 1–100 untuk soal MultipleChoice, MultipleAnswer, dan Essay. Override server-side `scoreValue=10` di `CreateQuestion` (line 4681) dan `EditQuestion` (line 4822) dihapus; input view enabled untuk semua tipe. *(maps Temuan 2)* | (1) Server overrides confirmed di line 4681-4682 dan 4821-4823. (2) View `disabled` attribute confirmed di line 186. (3) JS reset di line 297-298 dan 308-309. (4) Audit log infrastructure ready (9+ call sites). (5) PackageUserResponses index (AssessmentSessionId, PackageQuestionId) sudah exist (line 480 ApplicationDbContext) — `affectedSessions` count optimal. (6) Scoring formula varied-aware. (7) DB constraint hanya `> 0` — tidak konflik dengan range 1-100. |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Score input UI (form, dropdown handler, modal) | Browser/Client (vanilla JS + Bootstrap 5) | — | Form binding ke `<input scoreValue>`, dropdown change handler, modal show/hide — pure client interaction |
| Range validation HTML5 (`min/max/step/required`) | Browser/Client | API/Backend (defense in depth) | First-line defense via native form validation; backend re-check untuk DevTools bypass |
| Affected sessions count + data attribute injection | Frontend Server (Razor SSR) | Database | Razor mendapatkan count dari `_context.PackageUserResponses` saat render Edit form (atau di EditQuestion AJAX endpoint line 4768-4786) |
| Score range enforcement (1–100) | API/Backend (CreateQuestion + EditQuestion) | — | Authoritative validation — TempData["Error"] flash + redirect |
| ScoreValue persistence | Database (PackageQuestions.ScoreValue, int) | EF Core | DB CK constraint `> 0` only — range 1–100 enforced by app layer |
| Audit log entry | API/Backend (AuditLogService injected via DI) | Database (AuditLogs table) | `_auditLog.LogAsync(...)` → `AuditLogs.Add()` → `SaveChangesAsync()` |
| Total Points header display | Frontend Server (Razor inline calc) | — | `Model.Questions.Sum(q => q.ScoreValue)` di view langsung |
| In-flight session score impact | API/Backend (CMPController.SubmitExam, GradingService) | — | NO change — formula sudah varied-aware; storeed Score di Completed sessions tidak diubah |

## Standard Stack

### Core (Existing — No Additions)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 | Controller + Razor view rendering | [VERIFIED: STACK.md] established di codebase |
| Entity Framework Core | 8.0 | DB ORM (PackageQuestions, PackageUserResponses, AuditLogs) | [VERIFIED: STACK.md] |
| Bootstrap | 5.3 | Modal `editTypeWarningModal` + form styling | [VERIFIED: existing modal pattern di ManagePackageQuestions.cshtml line 237] |
| Vanilla JS | — | Dropdown change handler + form submit interceptor | [VERIFIED: pattern di ManagePackageQuestions.cshtml line 256-398, no jQuery used] |
| `_auditLog` (AuditLogService) | Internal | Audit trail | [VERIFIED: Services/AuditLogService.cs:21] DI singleton, `LogAsync` writes to AuditLogs table |
| `_logger` (ILogger<AssessmentAdminController>) | Internal | Defensive try/catch fallback for audit failures | [VERIFIED: AssessmentAdminController.cs:23] |
| ClosedXML | 0.105.0 | Excel parsing (NOT TOUCHED in this phase) | [VERIFIED: STACK.md] |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Inline validation block in CreateQuestion + EditQuestion | `[Range(1, 100)]` data annotation on parameter | Annotation cleaner but: (a) parameter is already `int` not model class — annotation requires ModelState-based validation flow which differs from existing inline-check pattern, (b) konflik dengan flash-error redirect style (D-13 spec). **Stick with inline check (D-13)** — implementation parity dengan correctCount + Essay rubrik checks at lines 4685-4699. |
| Service extraction (e.g., `ScoreValidationService`) | Standalone helper class | Premature abstraction — used hanya 2x (Create + Edit), inline lebih clean dan match pattern phase 304-305 ("no extract premature") |
| Separate "Total Points" badge component | Razor partial view | 1 inline `Model.Questions.Sum(q => q.ScoreValue)` lebih simple — match phase 305 inline calc style |

**Installation:** None — semua dependency sudah ada di proyek.

**Version verification:** No new packages.

## Architecture Patterns

### System Architecture Diagram

```
[Admin/HC Browser]
    │
    │ (1) Open ManagePackageQuestions form (existing or Edit)
    ▼
[Razor SSR Render — ManagePackageQuestions.cshtml]
    │ Inject: data-original-score=@q.ScoreValue
    │         data-affected-sessions=@(affectedSessionsCount)
    │ Display: input scoreValue (NO disabled, value=10 default)
    │ Display: header "Daftar Soal (N soal • Total X poin)" (D-17)
    │
    │ (2) User types new score, dropdown change preserves value (D-02)
    ▼
[Vanilla JS Dropdown Handler — applyQTypeSwitch (line 289)]
    │ REMOVED: scoreInput.disabled = (qtype !== 'Essay')   [D-04]
    │ REMOVED: if (qtype !== 'Essay') scoreInput.value = 10 [D-02]
    │ REMOVED: scoreHelp.textContent dynamic per type      [D-05]
    │
    │ (3) User clicks Submit
    ▼
[Form Submit Handler — NEW logic for Edit Mode]
    │ if (editMode && newScore != originalScore && affectedSessions > 0):
    │   ┌────────────────────────────────────────┐
    │   │ Show "Peringatan Ubah Skor" Modal      │ (replicate editTypeWarningModal)
    │   │ Text: D-07 with placeholders           │
    │   │ Buttons: Ya, Lanjutkan / Batal         │ (D-08)
    │   └────────────────────────────────────────┘
    │   On Confirm: form.submit()
    │   On Cancel: prevent submit
    │
    │ (4) POST to CreateQuestion or EditQuestion
    ▼
[AssessmentAdminController.CreateQuestion / EditQuestion]
    │ REMOVED line 4681-4682 (or 4822-4823): if (questionType != "Essay") scoreValue = 10  [D-14]
    │ REMOVED line 4682 (or 4823): if (scoreValue <= 0) scoreValue = 10                    [D-14]
    │ ADDED:   if (scoreValue < 1 || scoreValue > 100) {                                    [D-13]
    │            TempData["Error"] = "Nilai soal harus antara 1 dan 100.";
    │            return RedirectToAction(...);
    │          }
    │ EXISTING: validate correctCount, rubrik (UNTOUCHED — line 4684-4700, 4825-4841)
    │ EXISTING: q.ScoreValue = scoreValue (line 4709 / 4845)
    │ EXISTING: SaveChangesAsync()
    │
    │ NEW (Edit only when delta detected — D-10):
    │   var oldScore = q.ScoreValue (capture BEFORE assignment line 4845)
    │   var affectedCount = await _context.PackageUserResponses
    │                         .Where(r => r.PackageQuestionId == questionId)
    │                         .Select(r => r.AssessmentSessionId).Distinct().CountAsync()
    │   if (oldScore != scoreValue) {
    │     try {
    │       await _auditLog.LogAsync(currentUser.Id, actorName, "EditQuestion-ScoreChange",
    │         $"Question #{q.Id} (Order {q.Order}, Package #{packageId}) " +
    │         $"ScoreValue: {oldScore} → {scoreValue} ({affectedCount} sessions affected)");
    │     } catch (Exception auditEx) {
    │       _logger.LogWarning(auditEx, "Audit log write failed for EditQuestion-ScoreChange");
    │     }
    │   }
    │
    │ NEW (Create — D-11 if implementer chooses CD-05):
    │   if (scoreValue != 10) {
    │     try { await _auditLog.LogAsync(...); } catch { _logger.LogWarning(...); }
    │   }
    │
    ▼
[Database — SQL Server]
    PackageQuestions.ScoreValue = (1..100)
    AuditLogs row inserted (if changed/non-default)

[Worker Browser — Future Sessions]
    │
    ▼
[CMPController.SubmitExam — line 1644-1705]
    │ NO CHANGE — formula:
    │   maxScore = shuffledIds.Sum(qId => questionLookup[qId].ScoreValue)
    │   totalScore += q.ScoreValue (per correct MC/MA answer)
    │   finalPercentage = (totalScore / maxScore) * 100
    │
    │ Stored Score (AssessmentSessions.Score) untuk Completed sessions DOES NOT recalculate (D-19)
```

### Recommended Project Structure

**No new files.** Phase 306 hanya MODIFIES existing files:

```
Views/Admin/
└── ManagePackageQuestions.cshtml  (modified: header total, score input, JS handler, NEW score warning modal)

Controllers/
└── AssessmentAdminController.cs    (modified: CreateQuestion ~4675-4700, EditQuestion ~4796-4870)
```

### Pattern 1: Audit Log Call with Try/Catch Fallback

**What:** Defensive audit logging that never breaks the main flow if AuditLogs DB write fails.
**When to use:** Setiap kali write ke audit log — match pattern di codebase (line 1342, 2015).
**Example (extracted from `AssessmentAdminController.cs:2003-2018`):**
```csharp
// Audit log
try
{
    var deleteUser = await _userManager.GetUserAsync(User);
    var deleteActorName = string.IsNullOrWhiteSpace(deleteUser?.NIP)
        ? (deleteUser?.FullName ?? "Unknown")
        : $"{deleteUser.NIP} - {deleteUser.FullName}";
    await _auditLog.LogAsync(
        deleteUser?.Id ?? "",
        deleteActorName,
        "DeleteAssessment",
        $"Deleted assessment '{assessmentTitle}' [ID={id}]",
        id,
        "AssessmentSession");
}
catch (Exception auditEx)
{
    logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessment {Id}", id);
}
```

### Pattern 2: Bootstrap Modal Warning + JS Confirm Trigger

**What:** Modal Bootstrap 5 dengan dua tombol (confirm + cancel), dipicu JS sebelum submit form.
**When to use:** Saat user submit memerlukan konfirmasi tambahan untuk impactful action.
**Example (extracted from `ManagePackageQuestions.cshtml:237-271`):**
```html
<!-- Edit Warning Modal -->
<div class="modal fade" id="editTypeWarningModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-warning">
                <h5 class="modal-title"><i class="bi bi-exclamation-triangle me-2"></i>Peringatan Ubah Tipe Soal</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                Mengubah tipe soal akan menghapus jawaban peserta yang ada untuk soal ini. Lanjutkan?
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-warning btn-sm" id="confirmTypeChange">Ya, Lanjutkan</button>
                <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Batal</button>
            </div>
        </div>
    </div>
</div>
```

```javascript
// Initialize Bootstrap modals
document.addEventListener('DOMContentLoaded', function () {
    editTypeWarningModal = new bootstrap.Modal(document.getElementById('editTypeWarningModal'));

    document.getElementById('confirmTypeChange').addEventListener('click', function () {
        document.getElementById('QuestionType').value = pendingType;
        applyQTypeSwitch(pendingType);
        editTypeWarningModal.hide();
    });
});
```

### Pattern 3: TempData Flash Error + Redirect (Validation Failure)

**What:** Server-side validation error → set TempData["Error"] → redirect to caller, view renders alert from TempData.
**When to use:** Validation gagal di POST handler.
**Example (extracted from `AssessmentAdminController.cs:4686-4690`):**
```csharp
if (questionType == "MultipleChoice" && correctCount != 1)
{
    TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
```

View renders flash dari `ManagePackageQuestions.cshtml:32-35`:
```html
@if (TempData["Error"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show">@TempData["Error"]<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>
}
```

### Pattern 4: Razor Inline Aggregate Display (Total Points header)

**What:** Compute aggregate inline dalam Razor `@{}` block atau langsung di markup.
**When to use:** Simple aggregate yang tidak butuh ViewModel field baru.
**Example (D-17 implementation):**
```razor
@{
    var totalPoints = questions.Sum(q => q.ScoreValue);
}
<span class="fw-semibold">Daftar Soal (@questions.Count soal • Total @totalPoints poin)</span>
```

### Anti-Patterns to Avoid

- **Silent coerce invalid input ke default** — current code line 4682 (CreateQuestion) dan 4823 (EditQuestion) melakukan `if (scoreValue <= 0) scoreValue = 10;`. Ini tampak defensive tapi sebenarnya **menutupi user mistake** — user input "abc" dikirim sebagai 0 (model binding), system silently set ke 10. Phase 306 D-14 menghapus pattern ini. **Pakai explicit reject dengan TempData["Error"]**.
- **Modal trigger tanpa server-side authoritative check** — modal di-bypass oleh DevTools. **Selalu verify scoreValue range di server** (D-12 layered defense).
- **Hardcoding affectedCount di view tanpa query** — kalau tidak query DB, count = 0 selalu, modal tidak pernah trigger meskipun ada session. **Inject via `data-affected-sessions` setelah query DB di EditQuestion AJAX response (line 4770-4786) atau di Razor render.**
- **Audit log dalam main try block tanpa fallback** — kalau AuditLogs table down, main operation rollback. **Selalu wrap audit log dalam separate try/catch dengan `_logger.LogWarning` fallback** (pattern phase 296+).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Audit log writer | Custom DB insert + SaveChanges | `_auditLog.LogAsync(...)` (Services/AuditLogService.cs) | Already exists, used 9+ times, DI registered |
| Bootstrap modal | Custom div + jQuery show/hide | `new bootstrap.Modal(element)` + `.show()`/`.hide()` (line 264) | Bootstrap 5.3 native, accessible (focus trap, ESC key, aria) |
| Number range validation client-side | Custom `keypress` handler | HTML5 `<input type="number" min="1" max="100" step="1" required>` | Native browser tooltip (Chrome/Edge), keyboard accessible, screen reader friendly |
| Distinct session count for affected | Custom GroupBy aggregation in C# loop | `_context.PackageUserResponses.Where(r => r.PackageQuestionId == qId).Select(r => r.AssessmentSessionId).Distinct().CountAsync()` | EF Core translates to `SELECT COUNT(DISTINCT AssessmentSessionId)` SQL — index `(AssessmentSessionId, PackageQuestionId)` di line 480 ApplicationDbContext supports this query optimally |
| Form submit interception | Custom AJAX wrapper | `form.addEventListener('submit', e => { if (...) e.preventDefault(); modal.show(); })` | Vanilla JS sufficient, no jQuery needed (codebase pattern) |

**Key insight:** Phase 306 adalah **pure delta** terhadap pattern yang sudah established di codebase — tidak ada kebutuhan untuk introduce abstraction baru. Setiap operation memetakan 1:1 ke existing pattern.

## Existing Patterns

### Audit Log Pattern (Q1 Resolution)

**Verified consistency across 9 call sites in `AssessmentAdminController.cs`:**

| Line | Action | Has try/catch? | Has logger fallback? |
|------|--------|----------------|----------------------|
| 326  | AddCategory | NO (linear flow) | — |
| 378  | EditCategory | NO (linear flow) | — |
| 407  | DeleteCategory | NO (linear flow) | — |
| 430  | ToggleCategoryActive | NO (linear flow) | — |
| 1292 | CreateAssessment (success path) | NO (inside outer transaction try) | — |
| 1334 | CreateAssessment_Failed (catch path) | YES (nested try/catch line 1331-1342) | YES `_logger.LogWarning(auditEx, "Audit logging failed during CreateAssessment error handling")` |
| 1794 | EditAssessment | YES (line 1789-1810) | YES `logger.LogError(ex, "Error updating assessment")` (less defensive — wraps SaveChanges) |
| 1897 | BulkAssign | NO (inside transaction try) | — |
| 2007 | DeleteAssessment | YES (line 2003-2018) | YES `logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessment {Id}", id)` |

**Two distinct sub-patterns:**

**Sub-pattern A — Linear (audit log fail = whole flow fails):**
Used in low-risk admin actions (categories) where audit failure is acceptable to surface as error. Lines 326, 378, 407, 430, 1292, 1897.

**Sub-pattern B — Defensive (audit log isolated try/catch):**
Used in high-stakes flows where main operation must succeed even if audit fails. Lines 1334, 2007. **This is the recommended pattern for Phase 306 EditQuestion + CreateQuestion** — score change is core data update; audit failure should not roll back user save.

**Recommended template for D-10 (verbatim from line 2003-2018):**
```csharp
// AFTER successful await _context.SaveChangesAsync();
if (oldScore != scoreValue)
{
    try
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
            ? (currentUser?.FullName ?? "Unknown")
            : $"{currentUser.NIP} - {currentUser.FullName}";
        await _auditLog.LogAsync(
            currentUser?.Id ?? "",
            actorName,
            "EditQuestion-ScoreChange",
            $"Question #{q.Id} (Order {q.Order}, Package #{packageId}) ScoreValue: {oldScore} → {scoreValue} ({affectedSessionsCount} sessions affected)",
            q.Id,
            "PackageQuestion");
    }
    catch (Exception auditEx)
    {
        _logger.LogWarning(auditEx, "Audit log write failed for EditQuestion-ScoreChange QuestionId={Id}", q.Id);
    }
}
```

[VERIFIED: AssessmentAdminController.cs lines 326, 378, 407, 430, 1292, 1334, 1794, 1897, 2007, 2015, 2017]

### Modal "Peringatan Ubah Tipe Soal" Pattern (Q2 Resolution)

**Lokasi exact:** `Views/Admin/ManagePackageQuestions.cshtml` lines **237-253** (HTML) + lines **260-271** (JS init + click handler) + lines **274-287** (trigger logic in change handler).

**Struktur Bootstrap modal:**
```html
<!-- Edit Warning Modal -->
<div class="modal fade" id="editTypeWarningModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-warning">
                <h5 class="modal-title"><i class="bi bi-exclamation-triangle me-2"></i>Peringatan Ubah Tipe Soal</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                Mengubah tipe soal akan menghapus jawaban peserta yang ada untuk soal ini. Lanjutkan?
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-warning btn-sm" id="confirmTypeChange">Ya, Lanjutkan</button>
                <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Batal</button>
            </div>
        </div>
    </div>
</div>
```

**JS init pattern (line 260-271):**
```javascript
var editTypeWarningModal = null;

document.addEventListener('DOMContentLoaded', function () {
    editTypeWarningModal = new bootstrap.Modal(document.getElementById('editTypeWarningModal'));

    document.getElementById('confirmTypeChange').addEventListener('click', function () {
        document.getElementById('QuestionType').value = pendingType;
        applyQTypeSwitch(pendingType);
        editTypeWarningModal.hide();
    });
});
```

**Trigger pattern (line 274-287, type-change handler):**
```javascript
document.getElementById('QuestionType').addEventListener('change', function () {
    var newType = this.value;
    var isEditMode = document.getElementById('editQuestionId').value !== '';

    // Warn if editing an existing question and type changes
    if (isEditMode && originalType && originalType !== newType) {
        pendingType = newType;
        this.value = originalType; // revert visually until confirmed
        editTypeWarningModal.show();
        return;
    }

    applyQTypeSwitch(newType);
});
```

**Replikasi untuk Phase 306 "Peringatan Ubah Skor" — recommended structure:**

```html
<!-- Score Change Warning Modal (D-06..D-08) -->
<div class="modal fade" id="editScoreWarningModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-warning">
                <h5 class="modal-title"><i class="bi bi-exclamation-triangle me-2"></i>Peringatan Ubah Skor</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body" id="editScoreWarningBody">
                <!-- Populated by JS: D-07 message template -->
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-primary btn-sm" id="confirmScoreChange">Ya, Lanjutkan</button>
                <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Batal</button>
            </div>
        </div>
    </div>
</div>
```

**Trigger logic (form submit interceptor):**
```javascript
document.getElementById('questionForm').addEventListener('submit', function (e) {
    var isEditMode = document.getElementById('editQuestionId').value !== '';
    if (!isEditMode) return; // CreateQuestion: no modal (D-11 — audit only, no warning)

    var newScore = parseInt(document.getElementById('scoreValue').value, 10);
    var originalScore = parseInt(this.dataset.originalScore || '0', 10);
    var affectedSessions = parseInt(this.dataset.affectedSessions || '0', 10);

    if (newScore !== originalScore && affectedSessions > 0) {
        e.preventDefault();
        var order = document.getElementById('formTitle').textContent.match(/\d+/)?.[0] || '?';
        document.getElementById('editScoreWarningBody').innerHTML =
            'Skor soal #' + order + ' akan diubah dari <strong>' + originalScore + '</strong> menjadi <strong>' + newScore + '</strong>. ' +
            '<strong>' + affectedSessions + ' peserta</strong> sudah menjawab — persentase mereka akan dihitung ulang otomatis. Lanjutkan?';
        editScoreWarningModal.show();
    }
});

document.getElementById('confirmScoreChange').addEventListener('click', function () {
    editScoreWarningModal.hide();
    document.getElementById('questionForm').submit(); // bypass interceptor (or use a flag)
});
```

**Edge case:** confirmScoreChange button calls `form.submit()` which would re-trigger the interceptor. Solution — set a flag like `form.dataset.confirmed = 'true'` and short-circuit interceptor on next call.

[VERIFIED: ManagePackageQuestions.cshtml line 237-271, 274-287]

### PackageUserResponses Affected Sessions Query (Q3 Resolution)

**Bagaimana cara count `affectedSessions`:** Query EF Core dengan DISTINCT pada `AssessmentSessionId`:

```csharp
var affectedSessionsCount = await _context.PackageUserResponses
    .Where(r => r.PackageQuestionId == questionId)
    .Select(r => r.AssessmentSessionId)
    .Distinct()
    .CountAsync();
```

**Why DISTINCT?** PackageUserResponses memiliki **multiple rows per session-question pair** untuk MultipleAnswer questions (1 row per option selected). Tanpa DISTINCT, count akan over-count untuk MA. Verified di `CMPController.cs:1693-1696`:
```csharp
var maResponses = allExistingResponses
    .Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)
    .Select(r => r.PackageOptionId!.Value)
    .ToHashSet();  // ← multiple rows, deduped via HashSet
```

**Index optimization:** Index `(AssessmentSessionId, PackageQuestionId)` exists at `Data/ApplicationDbContext.cs:480`:
```csharp
entity.HasIndex(r => new { r.AssessmentSessionId, r.PackageQuestionId });
```

This composite index supports:
- ✅ `WHERE PackageQuestionId == X` — covers (PackageQuestionId is NOT leading column, so partial scan, but acceptable for low-cardinality questionId per session)
- ✅ `SELECT DISTINCT AssessmentSessionId` — index includes the column

**Alternative more-optimal index** — `IX_PackageUserResponses_PackageQuestionId` (single column, leading on PackageQuestionId). **Recommendation:** Don't add — current index sufficient for expected query volume (admin manual edits < 10/day, low traffic). YAGNI.

**Where to inject count:**
- **Server-side at EditQuestion AJAX endpoint** (line 4768-4786) — return `affectedSessionsCount` field di JSON response.
- **Client-side**: `populateEditForm` (line 342-380) reads from JSON and sets `form.dataset.affectedSessions = data.affectedSessions || 0`.

[VERIFIED: ApplicationDbContext.cs:480, CMPController.cs:1693-1696, EditQuestion AJAX line 4768-4786]

### TempData["Error"] Flash Pattern (Q4 Resolution)

**Existing example untuk validation error redirect** (CreateQuestion correctCount validation, line 4685-4690):

```csharp
var correctCount = (correctA ? 1 : 0) + (correctB ? 1 : 0) + (correctC ? 1 : 0) + (correctD ? 1 : 0);
if (questionType == "MultipleChoice" && correctCount != 1)
{
    TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
```

**Format konsistensi** dengan D-13/D-15:
- ✅ TempData["Error"] (not ViewBag, not ModelState)
- ✅ Bahasa Indonesia
- ✅ `RedirectToAction("ManagePackageQuestions", new { packageId })` — same target
- ✅ Hybrid bahasa OK (D-11 phase 305 sudah set precedent — "Single Choice hanya boleh memiliki 1 jawaban benar.")

**Recommended verbatim text untuk D-13:**
```csharp
TempData["Error"] = "Nilai soal harus antara 1 dan 100.";
```

**Alternative dengan more context** (mengikuti format error existing yang menyertakan info field):
```csharp
TempData["Error"] = $"Nilai soal harus antara 1 dan 100. Anda memasukkan {scoreValue}.";
```

Implementer pilih — D-15 spec menerima keduanya.

[VERIFIED: AssessmentAdminController.cs:4685-4699, ManagePackageQuestions.cshtml:32-35]

## Downstream Consumer Analysis (Q5 Resolution)

**Grep `ScoreValue` di seluruh `*.cs` (excluded Migrations):**

### Production Read Sites (10 occurrences)

| File | Line | Context | Behavior with varied ScoreValue |
|------|------|---------|----------------------------------|
| `Controllers/CMPController.cs` | 1646 | `maxScore = shuffledIds.Sum(qId => qq.ScoreValue : 0)` | **Already varied-aware.** Sum semua questions (MC/MA/Essay) — works untuk any int value. |
| `Controllers/CMPController.cs` | 1670 | `totalScore += q.ScoreValue` (MC correct) | **Already varied-aware.** Adds full ScoreValue per correct MC. |
| `Controllers/CMPController.cs` | 1699 | `totalScore += q.ScoreValue` (MA correct) | **Already varied-aware.** All-or-nothing for MA — sets value in totalScore. |
| `Controllers/AssessmentAdminController.cs` | 2660 | `EssayGradingItemViewModel.ScoreValue = q.ScoreValue` | **Already varied-aware.** Used as max value in essay scoring UI (line 2689 `score > question.ScoreValue` validation). |
| `Controllers/AssessmentAdminController.cs` | 2689-2690 | `if (score < 0 \|\| score > question.ScoreValue) return Json(... message: $"Skor harus antara 0 dan {question.ScoreValue}")` | **Already varied-aware.** Essay manual grading — uses ScoreValue as upper bound dynamically. |
| `Controllers/AssessmentAdminController.cs` | 2749 | `maxScore += q.ScoreValue` (FinalizeEssayGrading recalculate) | **Already varied-aware.** Sum semua questions saat finalize. |
| `Controllers/AssessmentAdminController.cs` | 2757 | `totalScore += q.ScoreValue` (MC scoring in FinalizeEssayGrading) | **Already varied-aware.** |
| `Controllers/AssessmentAdminController.cs` | 2765 | `totalScore += q.ScoreValue` (MA scoring in FinalizeEssayGrading) | **Already varied-aware.** |
| `Controllers/AssessmentAdminController.cs` | 3971 | `ScoreValue = q.ScoreValue` (deep clone Pre→Post in `SyncPackagesToPost`) | **Already varied-aware.** Copies value verbatim. **PHASE 306 IMPACT NOTED:** When admin edits Pre-Test question score, auto-sync clones to Post-Test. This is correct behavior — Pre and Post should have same scoring. No code change needed. |
| `Services/GradingService.cs` | 82, 95, 110 | `maxScore += q.ScoreValue`, `totalScore += q.ScoreValue` (MC + MA grading) | **Already varied-aware.** Verified via `Services/GradingService.cs:82-117`. |

### Production Write Sites (4 occurrences)

| File | Line | Context | Phase 306 Action |
|------|------|---------|-------------------|
| `Controllers/AssessmentAdminController.cs` | 4482 | `ScoreValue = 10` (ImportPackageQuestions hardcode) | **DEFER per CONTEXT.md.** See ImportQuestions Decision section. |
| `Controllers/AssessmentAdminController.cs` | 4709 | `ScoreValue = scoreValue` (CreateQuestion) | **MODIFIED in scope.** Remove override at line 4681-4682. |
| `Controllers/AssessmentAdminController.cs` | 4845 | `q.ScoreValue = scoreValue` (EditQuestion) | **MODIFIED in scope.** Remove override at line 4822-4823. |
| `Models/AssessmentPackage.cs` | 41 | `public int ScoreValue { get; set; } = 10;` (default value) | **NO CHANGE** — default 10 untuk new entity creation, OK. |

### Read Sites di View (Razor)

| File | Line | Context | Behavior with varied ScoreValue |
|------|------|---------|----------------------------------|
| `Views/Admin/AssessmentMonitoringDetail.cshtml` | 400 | `<input type="number" min="0" max="@essayItem.ScoreValue" />` (HC manual essay grading input) | **Already varied-aware.** Uses ScoreValue as dynamic max. |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` | 404 | `/ @essayItem.ScoreValue` (display denominator) | **Already varied-aware.** Renders int dynamically. |
| `Views/Admin/ManagePackageQuestions.cshtml` | 79 | `<td class="text-center">@q.ScoreValue</td>` (table column) | **Already varied-aware.** Renders int dynamically. |
| `Views/Admin/ManagePackageQuestions.cshtml` | 184-187 | Form input scoreValue (NEW behavior in scope) | **MODIFIED in scope** (D-03, D-05). |
| `Views/Admin/_PreviewQuestion.cshtml` | 67 | `Nilai: <strong>@Model.ScoreValue</strong>` (preview footer) | **Already varied-aware.** |

### Verdict

**Zero downstream caller assumes `ScoreValue == 10`.** Semua callers treat ScoreValue sebagai variable arithmetic. **No additional change beyond the locked decisions D-01..D-19** is needed for production correctness.

**Notable side-effect (D-19 already locked):**
- ✅ AssessmentSessions.Score (stored final percentage) does NOT auto-recalculate when admin edits ScoreValue.
- ✅ Future sessions (StartedAt > edit time, atau In Progress) will use new ScoreValue via existing formula.

[VERIFIED: All references searched in *.cs files outside Migrations folder — see toolu_01AruF388b6KKje3zcBPWkMW.txt for full grep dump]

## ImportQuestions Decision (Q6 Resolution)

### Finding

**ImportPackageQuestions hardcode `ScoreValue = 10` di line 4482** untuk semua tipe (MC/MA/Essay):

```csharp
var newQ = new PackageQuestion
{
    AssessmentPackageId = packageId,
    QuestionText = q,
    QuestionType = questionType,
    Rubrik = questionType == "Essay" ? rubrik : null,
    MaxCharacters = 2000,
    Order = order++,
    ScoreValue = 10,                  // ← HARDCODED, no Excel column read
    ElemenTeknis = NormalizeElemenTeknis(rawSubComp),
};
```

**Excel template (`DownloadQuestionTemplate`) headers** di line 4153 — **9 kolom, ZERO mention of ScoreValue:**
```csharp
var headers = new[] { "Pertanyaan", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Jawaban Benar", "Elemen Teknis", "QuestionType", "Rubrik" };
```

Excel parser line 4304-4313 reads cells 1-9 — TIDAK ADA cell ke-10 untuk ScoreValue.

### Decision

**DEFER scope expansion.** Per CONTEXT.md Deferred Ideas section:
> Excel import scoreValue range validation — perlu di-verify apakah ImportQuestions juga perlu accept varied score. Default: ImportQuestions tetap accept current behavior (MC/MA→10, Essay varied) sebelum confirmed need to update.

**Rationale:**
1. **Scope discipline.** Phase 306 success criteria fokus pada UI form (`ManagePackageQuestions.cshtml`) + 2 controller endpoints (`CreateQuestion`, `EditQuestion`). Excel import = separate concern.
2. **Adding ScoreValue Excel column** = BREAKING CHANGE untuk template binary file (`Template_Soal_*.xlsx`). User audit Pertamina belum confirm need ini.
3. **Workaround tersedia.** Admin can: (a) Import via Excel dengan default 10, then (b) Use Edit form to adjust per-question score 1-100. Multi-step but acceptable for MVP audit fix.
4. **Phase 306 audit + modal sudah handle the post-import flow** — admin who imports questions and then changes score will trigger audit log + modal warning correctly via the form path.

**Recommendation:** Add a **note in `ImportPackageQuestions.cshtml`** (existing line 32-35 bullet helper text) — *"Skor soal default 10. Untuk skor 1-100 custom, edit setelah import via tabel Soal."* Optional Claude Discretion.

### Future Phase (v16+ candidate)

Jika user audit explicitly request varied score di Excel:
- Add column 10 "ScoreValue" ke Excel template (`DownloadQuestionTemplate` line 4153 + example rows)
- Parse `int.TryParse(row.Cell(10).GetString(), out var scoreValue)` dengan fallback `10`
- Validate `scoreValue >= 1 && scoreValue <= 100` sebelum insert
- Preserve backward compat: rows tanpa kolom 10 → default 10
- Estimasi effort: 1 plan, 4 tasks, ~30 min

[VERIFIED: AssessmentAdminController.cs lines 4140-4213 (DownloadQuestionTemplate), 4234-4504 (ImportPackageQuestions), 4482 (hardcode)]

## Validation Architecture (Nyquist)

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Playwright v1.58.2 (E2E browser tests) |
| Config file | `tests/playwright.config.ts` |
| Quick run command | (no per-test command — Playwright runs serial) |
| Full suite command | `cd tests && npx playwright test` |
| **Note** | **No C# unit test framework** — codebase is E2E + manual UAT only [VERIFIED: TESTING.md] |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| QSCR-01 | Admin can save score 1-100 for MC/MA/Essay (form submit happy path) | E2E (Playwright) — extend `tests/e2e/assessment.spec.ts` OR new `score-editing.spec.ts` | `cd tests && npx playwright test e2e/assessment.spec.ts` | ❌ Wave 0 (need to author) |
| QSCR-01 | Server rejects score < 1 or > 100 with TempData["Error"] | E2E + manual DevTools bypass | Manual: open DevTools, remove `min/max`, submit invalid → expect flash error | ❌ Wave 0 (manual UAT script) |
| QSCR-01 | HTML5 validation prevents invalid input client-side | Manual visual check (browser tooltip) | Manual: type "150" in input → expect Chrome native tooltip | ❌ Manual-only |
| QSCR-01 | Modal "Peringatan Ubah Skor" appears when score changes for question with sessions | E2E with seeded data OR manual UAT | Manual: edit question with completed session, change score, expect modal | ❌ Wave 0 (manual UAT) |
| QSCR-01 | Audit log entry created in AuditLogs table | DB query post-test | `SELECT * FROM AuditLogs WHERE ActionType LIKE 'EditQuestion-ScoreChange%' ORDER BY CreatedAt DESC LIMIT 5` | DB verification — manual |
| QSCR-01 | Total Points header displays sum correctly | E2E visual assertion | `await expect(page.locator('.card-header')).toContainText('Total')` | ❌ Wave 0 |
| QSCR-01 | Stored Score di Completed sessions tidak retroactively recalculate (D-19) | DB query before/after | Manual: note `AssessmentSessions.Score` for session, edit question score, re-query — value unchanged | DB verification — manual |
| QSCR-01 | Future session menggunakan new ScoreValue (formula correctness) | E2E full exam flow | Extension of `exam-taking.spec.ts` — assign new exam after score edit, complete, verify computed % uses new ScoreValue | ❌ Wave 0 (high effort) |

### Sampling Rate

- **Per task commit:** Manual smoke test — `dotnet build` (must pass) + login admin + open ManagePackageQuestions + visual check.
- **Per wave merge:** Run existing `npx playwright test e2e/assessment.spec.ts` (regression — should not break Phase 305 LBL).
- **Phase gate:** Manual UAT script (10 checks below) per `/gsd-verify-work` convention.

### Recommended Manual UAT Script (Phase Gate)

1. Login Admin → `/Admin/ManagePackageQuestions/{id}` → header tampil "Total {N} poin"
2. Tambah soal MC dengan ScoreValue=15 → tabel tampil 15
3. Tambah soal MC dengan ScoreValue=150 → flash error "Nilai soal harus antara 1 dan 100."
4. Edit soal yang punya completed session, ubah ScoreValue dari 10 ke 20 → modal "Peringatan Ubah Skor" muncul
5. Klik "Batal" di modal → form tidak submit, ScoreValue tetap 10 di tampilan
6. Klik "Ya, Lanjutkan" → submit, redirect, flash success
7. Cek AuditLogs table → entry "EditQuestion-ScoreChange" muncul dengan format expected
8. DevTools → ubah `<input min>` ke `-100`, masukkan -5, submit → flash error muncul (server-side defense)
9. Switch dropdown tipe Essay→MC → ScoreValue value DI-PRESERVE (D-02), tidak reset ke 10
10. Edit soal tanpa associated session, ubah ScoreValue → modal TIDAK muncul (D-09 condition: affectedSessions > 0)

### Wave 0 Gaps

- [ ] **No automated test for QSCR-01** — Playwright suite needs new `tests/e2e/score-editing.spec.ts` (medium effort, can defer to follow-up phase)
- [ ] Existing `tests/e2e/assessment.spec.ts` — verify no regression after view changes
- [ ] Build verification: `dotnet build -c Debug` after each commit (already standard in project)

*(Decision: ZERO automated coverage for QSCR-01 expected — codebase pattern is manual UAT for admin form changes per phase 304/305 precedent. Validation = manual UAT script per phase gate.)*

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes (existing) | `[Authorize(Roles = "Admin, HC")]` already on Create/EditQuestion endpoints (line 4655, 4793) — no change needed |
| V3 Session Management | yes (existing) | ASP.NET Core Identity cookie auth — no change |
| V4 Access Control | yes | Authorize attribute restricts to Admin/HC roles — verified line 4655, 4793 |
| V5 Input Validation | **yes (new in scope)** | (1) HTML5 `min/max/step/required` (D-12 client) + (2) C# inline `if (scoreValue < 1 \|\| scoreValue > 100)` (D-13 server) — layered defense |
| V6 Cryptography | no | Phase 306 tidak touch crypto |

### Known Threat Patterns for ASP.NET Core MVC (Phase 306 specific)

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Tampering: DevTools bypass HTML5 validation, submit `scoreValue=-100` | Tampering | Server-side `if (scoreValue < 1 \|\| scoreValue > 100)` check at controller (D-13) |
| Tampering: Skip modal client-side, submit form directly with new score | Tampering | Server-side audit log entry on score change (D-10) — even if modal bypassed, audit trail exists. Plus server still re-validates range. |
| Information Disclosure: Audit log details leak | Information Disclosure | Audit log details (oldScore→newScore) only visible to Admin/HC via `/Admin/AuditLog` view (existing infrastructure). Not exposed to workers. |
| Denial of Service: Submit thousands of edits to fill AuditLogs table | DoS | Out of scope — Admin role inherently rate-limited by user behavior. Future: add CreateRateLimit if abuse observed. |
| Repudiation: Admin denies changing score | Repudiation | AuditLog `ActorUserId + ActorName` recorded (line 322-326 pattern) — provides non-repudiation evidence. |
| Race condition: 2 admins edit same question simultaneously | (concurrency) | Last-write-wins via EF Core change tracking — no optimistic concurrency token in current PackageQuestion model. **Acceptable** — admin ops low frequency, audit log captures both attempts. |

### Specific Phase 306 Threats

**T-306-01 (Tampering, server-side range bypass):** Mitigated via D-13 explicit `if (scoreValue < 1 || scoreValue > 100)` check. **Critical:** if planner forgets server-side check (only HTML5), this is a single point of failure.

**T-306-02 (Tampering, audit log skip):** D-10 audit log INSIDE try/catch — failure doesn't block save (sub-pattern B). If `_auditLog.LogAsync` throws, `_logger.LogWarning` records the failure — still leaves a trace via app log file.

**T-306-03 (Repudiation, missing oldScore in log):** D-10 spec captures both `oldScore` and `newScore` plus `affectedSessionsCount` — full forensic record.

**T-306-04 (Information Disclosure, count exposure):** `affectedSessions` count is metadata about exam participation. Exposing to admin is acceptable (admin already has full visibility). Not exposed to workers.

## Risks & Edge Cases

### R1 — Race condition: Modal vs Server Validation

**Scenario:** Admin opens Edit form → another admin completes a session → first admin's `data-affected-sessions` is stale → first admin sees modal with old count.

**Impact:** Modal shows incorrect count, but server-side audit log records actual count at submit time. **Acceptable** — admin can re-edit if needed.

**Mitigation:** None needed. Document expectation in code comment.

### R2 — In-progress sessions during score edit

**Scenario:** Worker has session in `Status = "InProgress"`, has answered some questions, hasn't submitted. Admin edits ScoreValue from 10 to 20 for a question worker hasn't answered yet.

**Impact:** When worker submits, formula uses **new** ScoreValue (20) at the question. `maxScore = sum(all questions)` includes the new value. Worker's percentage might be different than expected.

**Mitigation:** Modal D-07 wording **explicitly mentions** this dampak: *"persentase mereka akan dihitung ulang otomatis"*. Admin given choice to proceed or cancel.

### R3 — Excel import bypass

**Scenario:** Admin imports questions via Excel (hardcoded ScoreValue=10), then forgets to edit individually. Net effect: phase 306 doesn't help unless admin uses UI form.

**Impact:** Audit Temuan 2 partially fulfilled — UI editing works, but bulk Excel import doesn't.

**Mitigation:** Optional bullet text addition in `ImportPackageQuestions.cshtml` explaining limitation (documented above in ImportQuestions Decision section).

### R4 — Default value=10 retained ambiguity

**Scenario:** D-01 keeps `value="10"` default. Admin types nothing (uses default), submits → score = 10. But maybe they wanted score = 5.

**Impact:** Identical behavior to existing — no regression. User who wants different score must type it.

**Mitigation:** None — D-01 explicitly chose this default for "minimal disruption to UX". Acceptable.

### R5 — Accessibility: HTML5 validation tooltip browser-dependent

**Scenario:** User on Firefox sees "Please enter a valid value" but Chrome shows "Value must be 1 or higher". Tooltip wording is browser-controlled.

**Impact:** Inconsistent UX across browsers. Not blocking.

**Mitigation:** Server-side D-13 message provides authoritative text. HTML5 tooltip is enhancement, not required.

### R6 — Modal flag bypass loop

**Scenario:** confirmScoreChange handler calls `form.submit()`, which re-triggers submit interceptor, which detects delta+count again, shows modal again — infinite loop.

**Impact:** Form never submits, page hangs.

**Mitigation:** **Critical** — implementer MUST set a flag (e.g., `form.dataset.confirmed = 'true'`) before re-submitting and check flag at top of interceptor. Document explicitly in plan.

### R7 — Backward compat: data-original-score absent on Create form

**Scenario:** New question Create form doesn't have `data-original-score` attribute (no original — it's a new question).

**Impact:** JS reads `parseInt(undefined)` → NaN → `NaN !== originalScore` always evaluates falsy → no modal shown. Correct behavior.

**Mitigation:** JS code MUST check `isEditMode` first (line 276 pattern: `var isEditMode = document.getElementById('editQuestionId').value !== '';`).

### R8 — DB CK constraint conflict

**Scenario:** DB constraint is `[ScoreValue] > 0`. App submits ScoreValue = 0 → CK violation → 500 error.

**Impact:** Untranslated SQL exception bubble up to user.

**Mitigation:** D-13 server-side check `< 1` rejects 0 BEFORE DB hit. Should never reach DB. Defense in depth.

## Recommended Plan Structure

**Suggestion:** **1 plan with 7-9 tasks** (prefer single plan over multi-plan split).

**Rationale:** 
- Phase 306 changes 2 files only (1 view + 1 controller)
- Tasks are sequential — JS depends on Razor changes, controller change small
- Total estimated effort: 30-45 min
- Match phase 305 Plan 01 granularity (8 tasks, ~21 min duration)
- Atomic phase boundary — easier to revert if issues found

### Recommended Task Breakdown for Plan 01

| # | Task | File | Dependency |
|---|------|------|------------|
| 1 | Update `<input scoreValue>` attributes (remove `disabled`, ensure `min/max/step/required`) | ManagePackageQuestions.cshtml line 184-187 | — |
| 2 | Update help text scoreHelp to static "Range 1–100" or remove | ManagePackageQuestions.cshtml line 187 | T1 |
| 3 | Update JS `applyQTypeSwitch` — remove `scoreInput.disabled` line 297 + value reset line 298 + scoreHelp dynamic line 308-309 | ManagePackageQuestions.cshtml line 289-310 | T1 |
| 4 | Update header "Daftar Soal" to include Total Points (D-17) | ManagePackageQuestions.cshtml line 42 | — |
| 5 | Add NEW modal `editScoreWarningModal` HTML | ManagePackageQuestions.cshtml after line 253 | — |
| 6 | Add NEW JS form submit interceptor + confirmScoreChange handler | ManagePackageQuestions.cshtml in `@section Scripts` | T5 |
| 7 | Inject `data-original-score` + `data-affected-sessions` (extend EditQuestion AJAX response line 4768-4786 + `populateEditForm` line 342-380) | AssessmentAdminController.cs + ManagePackageQuestions.cshtml | T6 |
| 8 | Update CreateQuestion: remove override line 4681-4682, add range check (D-13), optional audit (D-11/CD-05) | AssessmentAdminController.cs line 4675-4700 | — |
| 9 | Update EditQuestion: remove override line 4822-4823, add range check (D-13), capture oldScore + count affectedSessions + audit (D-10) | AssessmentAdminController.cs line 4796-4870 | T8 |

**Atomic commits:** Each task = 1 commit. Total: 9 commits.

### Optional Plan 02 (if Plan 01 grows beyond 10 tasks)

Split JS modal logic into separate task group OR document dengan visualisasi.

**Verdict: 1 plan recommended.** Phase scope tight enough untuk single plan execution.

## Common Pitfalls

### Pitfall 1: Forgetting to Capture oldScore Before Assignment

**What goes wrong:** D-10 spec: log `{oldScore} → {scoreValue}`. If implementer writes `q.ScoreValue = scoreValue;` BEFORE capturing oldScore, the audit log shows `{newScore} → {newScore}` (no delta).

**Why it happens:** Natural code flow: validate → assign → save. Audit added at end forgets to look back.

**How to avoid:** Capture `var oldScore = q.ScoreValue;` IMMEDIATELY after `var q = await _context.PackageQuestions.FindAsync(...)` (line ~4818) — BEFORE any mutation.

**Warning signs:** Audit log shows scoreValue→scoreValue equal numbers.

### Pitfall 2: Modal Bypass via Direct Form.submit()

**What goes wrong:** confirmScoreChange button calls `form.submit()`, which re-triggers submit interceptor, which detects delta+count again, shows modal again — infinite loop.

**Why it happens:** Submit interceptor doesn't differentiate between "first submit" vs "post-confirmation submit".

**How to avoid:** Set `form.dataset.confirmed = 'true'` before `form.submit()`. Check this flag at top of interceptor: `if (this.dataset.confirmed === 'true') return;`.

**Warning signs:** Page appears frozen; modal keeps re-appearing.

### Pitfall 3: Server-Side Range Check Order Wrong

**What goes wrong:** Putting range check AFTER `if (questionType != "Essay") scoreValue = 10;` at line 4681. The override coerces 150 to 10, then range check 1-100 passes — silent bug.

**Why it happens:** Phase 306 D-14 says REMOVE the override. If implementer ADDS the range check before removing, validation gets short-circuited.

**How to avoid:** **Sequence matters** — remove override FIRST (line 4681-4682), THEN add range check. Plan task ordering: T8 / T9 must combine both edits in single commit.

**Warning signs:** User submits scoreValue=150 → receives no error → DB stores 10 (silent coerce).

### Pitfall 4: Forgetting `Distinct()` in Affected Sessions Query

**What goes wrong:** MA questions have multiple PackageUserResponses rows per session (1 per option selected). `.Count()` without `.Distinct()` over-counts.

**Why it happens:** Misunderstanding of MA storage model.

**How to avoid:** Always `.Select(r => r.AssessmentSessionId).Distinct().CountAsync()`.

**Warning signs:** Modal shows "5 peserta" when only 2 actually answered (because question is MA with 3 options selected per session).

### Pitfall 5: Exception Type Mismatch in Audit Try/Catch

**What goes wrong:** Wrap audit log in `catch (DbException)` only — but `_auditLog.LogAsync` might throw `InvalidOperationException` from EF Core context state.

**Why it happens:** Over-specific exception filter.

**How to avoid:** Use `catch (Exception auditEx)` (per pattern line 1342, 2017). Audit log failure is non-critical — catch broadly with logger fallback.

**Warning signs:** 500 error on save when audit fails. Should be silent (logged) failure.

### Pitfall 6: Missing data-original-score on AJAX-Loaded Edit Form

**What goes wrong:** EditQuestion AJAX endpoint returns JSON (line 4770-4786) with question data. `populateEditForm` reads JSON — if `affectedSessions` field not added to response, JS reads `undefined` → modal never triggers.

**Why it happens:** Two-touch implementation — server JSON response + client populateEditForm — easy to forget one.

**How to avoid:** Plan task T7 explicitly covers BOTH endpoints. Verify with: open form via Edit button (AJAX path), inspect DOM via DevTools → `<form data-original-score="X" data-affected-sessions="Y">` attributes present.

**Warning signs:** Modal never shows in Edit mode despite expected condition.

## Code Examples

### Example 1: D-10 Audit Log + Affected Sessions Count Pattern

```csharp
// In EditQuestion (AssessmentAdminController.cs) — placement after validation, before SaveChangesAsync

// Capture original score BEFORE mutation
var oldScore = q.ScoreValue;

// Existing validation (untouched)
// ... correctCount, rubrik checks ...

// NEW: Range check (D-13)
if (scoreValue < 1 || scoreValue > 100)
{
    TempData["Error"] = "Nilai soal harus antara 1 dan 100.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}

// Compute affectedSessions count (only when score changed — optimization)
int affectedSessionsCount = 0;
if (oldScore != scoreValue)
{
    affectedSessionsCount = await _context.PackageUserResponses
        .Where(r => r.PackageQuestionId == questionId)
        .Select(r => r.AssessmentSessionId)
        .Distinct()
        .CountAsync();
}

// Existing assignment (untouched)
q.QuestionText = questionText.Trim();
q.QuestionType = questionType;
q.ScoreValue = scoreValue;  // ← line 4845
// ... other assignments ...

await _context.SaveChangesAsync();

// NEW: Audit log (D-10) — outside main try/catch, defensive
if (oldScore != scoreValue)
{
    try
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
            ? (currentUser?.FullName ?? "Unknown")
            : $"{currentUser.NIP} - {currentUser.FullName}";
        await _auditLog.LogAsync(
            currentUser?.Id ?? "",
            actorName,
            "EditQuestion-ScoreChange",
            $"Question #{q.Id} (Order {q.Order}, Package #{packageId}) ScoreValue: {oldScore} → {scoreValue} ({affectedSessionsCount} sessions affected)",
            q.Id,
            "PackageQuestion");
    }
    catch (Exception auditEx)
    {
        _logger.LogWarning(auditEx, "Audit log write failed for EditQuestion-ScoreChange QuestionId={Id}", q.Id);
    }
}
```

[Source: synthesis of patterns at AssessmentAdminController.cs:1334-1342, 2003-2018]

### Example 2: D-09 Server-Side data-* Attribute Injection

```csharp
// In EditQuestion AJAX endpoint (line 4768-4786)
if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
{
    // NEW: Compute affected sessions count
    var affectedSessions = await _context.PackageUserResponses
        .Where(r => r.PackageQuestionId == q.Id)
        .Select(r => r.AssessmentSessionId)
        .Distinct()
        .CountAsync();

    return Json(new
    {
        id = q.Id,
        order = q.Order,
        questionText = q.QuestionText,
        questionType = q.QuestionType ?? "MultipleChoice",
        scoreValue = q.ScoreValue,
        elemenTeknis = q.ElemenTeknis,
        rubrik = q.Rubrik,
        maxCharacters = q.MaxCharacters,
        affectedSessions = affectedSessions,  // ← NEW field
        options = q.Options.OrderBy(o => o.Id).Select(o => new
        {
            optionText = o.OptionText,
            isCorrect = o.IsCorrect
        }).ToList()
    });
}
```

```javascript
// In populateEditForm (ManagePackageQuestions.cshtml line 342-380)
function populateEditForm(data) {
    var form = document.getElementById('questionForm');
    form.action = (window.basePath || '') + '/Admin/EditQuestion';

    // NEW: stash original score + affected count on form for submit handler
    form.dataset.originalScore = data.scoreValue || 10;
    form.dataset.affectedSessions = data.affectedSessions || 0;

    // ... existing field population ...
}
```

[Source: pattern extension to AssessmentAdminController.cs:4770-4786, ManagePackageQuestions.cshtml:342-380]

### Example 3: D-17 Total Points Header Display

```razor
@{
    ViewData["Title"] = "Manage Questions";
    int packageId = (int)ViewBag.PackageId;
    int assessmentId = (int)ViewBag.AssessmentId;
    var questions = ViewBag.Questions as List<PackageQuestion> ?? new List<PackageQuestion>();
    var totalPoints = questions.Sum(q => q.ScoreValue);  // ← NEW
}

<!-- ... -->

<div class="card-header bg-light d-flex justify-content-between align-items-center">
    <span class="fw-semibold">Daftar Soal (@questions.Count soal • Total @totalPoints poin)</span>
</div>
```

[Source: pattern from existing line 42, extended per D-17]

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hardcode ScoreValue=10 for MC/MA at controller (line 4681-4682) | Range 1-100 with explicit reject (D-13) | Phase 306 (this phase) | Admin can vary score per question |
| Disabled input for non-Essay tipe (line 186 `disabled`) | Always-enabled input (D-03) | Phase 306 | UX consistency across types |
| JS reset value=10 on dropdown change (line 298) | Preserve user-entered value (D-02) | Phase 306 | No data loss when admin switches type |
| No audit trail for score edits | AuditLog entry "EditQuestion-ScoreChange" (D-10) | Phase 306 | Compliance with audit Temuan 2 |
| No warning when score change affects sessions | Modal "Peringatan Ubah Skor" (D-06..D-08) | Phase 306 | User awareness of impact |

**Deprecated/outdated:**
- ❌ `if (questionType != "Essay") scoreValue = 10;` — Phase 306 removes (D-14)
- ❌ `if (scoreValue <= 0) scoreValue = 10;` — Phase 306 removes (D-14, replaced with explicit reject)
- ❌ Help text dynamic per type — Phase 306 simplifies to static "Range 1-100" (D-05)

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | DB CK constraint `[ScoreValue] > 0` is the only constraint on ScoreValue (no upper bound exists in DB layer) | Standard Stack / Security | LOW — verified via Migration files. If wrong, range 1-100 still enforced at app layer. |
| A2 | Index `(AssessmentSessionId, PackageQuestionId)` (line 480) supports affected sessions query optimally | Existing Patterns Q3 | LOW — partial scan acceptable for low-frequency admin ops. Worst case: 100ms slow query. |
| A3 | Phase 306 should NOT include Excel import scoreValue parsing (defer per CONTEXT.md) | ImportQuestions Decision | LOW — explicit user decision in CONTEXT.md deferred section. |
| A4 | Phase 306 needs ZERO automated test coverage (manual UAT only) per codebase pattern | Validation Architecture | MEDIUM — if user requests automated test, Wave 0 needs to add Playwright spec. |
| A5 | `_logger.LogWarning(auditEx, ...)` fallback is the canonical defensive pattern | Existing Patterns Q1 | LOW — verified via 2 occurrences (line 1342, 2017). |
| A6 | CD-05 default = "Skip Create audit (D-11)" — only EditQuestion logs by default | Recommended Plan Structure | LOW — implementer can choose either; both are valid per Claude's Discretion. |

**If user confirms all assumptions:** Plan can proceed with single plan / 9 tasks / ~30-45 min effort.

## Open Questions

1. **Should CreateQuestion also log audit for non-default score (D-11)?**
   - What we know: D-11 spec accepts both — implementer's discretion (CD-05).
   - What's unclear: User preference for audit verbosity.
   - Recommendation: **Default to YES** — captures admin intent for creation, parity with EditQuestion. Cost: 1 extra try/catch block. Benefit: full audit trail.

2. **Wording of help text under input — "Range 1–100" vs drop entirely?**
   - What we know: D-05 + CD-01 both valid.
   - What's unclear: Visual fit when label `Nilai Soal (1–100)` is shown.
   - Recommendation: **Drop entirely** — label already contains "(1–100)" range info, dynamic help text removed (D-05), no static help adds noise.

3. **Total Points exact wording — "• Total 30 poin" vs "(30 pts)" vs "— 30 pts total"?**
   - What we know: CD-03 = implementer choice.
   - What's unclear: Visual style preference.
   - Recommendation: **"• Total {N} poin"** (Bahasa Indonesia, bullet separator matches existing card-header style).

## Sources

### Primary (HIGH confidence)

- **`Controllers/AssessmentAdminController.cs:1-50`** — DI injection (`_logger`, `_auditLog`, `_userManager`, `_notificationService`)
- **`Controllers/AssessmentAdminController.cs:322-413`** — Audit log linear pattern (categories CRUD)
- **`Controllers/AssessmentAdminController.cs:1331-1342`** — Audit log defensive pattern (Sub-pattern B with try/catch)
- **`Controllers/AssessmentAdminController.cs:2003-2018`** — Audit log defensive pattern (DeleteAssessment — recommended template)
- **`Controllers/AssessmentAdminController.cs:4655-4754`** — CreateQuestion endpoint (target for D-13, D-14)
- **`Controllers/AssessmentAdminController.cs:4757-4892`** — EditQuestion endpoint (target for D-09, D-10, D-13, D-14)
- **`Controllers/AssessmentAdminController.cs:4140-4213`** — DownloadQuestionTemplate (Excel template = 9 columns, NO ScoreValue)
- **`Controllers/AssessmentAdminController.cs:4234-4504`** — ImportPackageQuestions (line 4482 hardcode `ScoreValue = 10`)
- **`Controllers/CMPController.cs:1620-1730`** — SubmitExam scoring formula (varied-aware, no change needed per D-18)
- **`Services/GradingService.cs:75-130`** — GradingService scoring (varied-aware)
- **`Services/AuditLogService.cs`** — Full audit log service contract (`LogAsync` signature)
- **`Views/Admin/ManagePackageQuestions.cshtml`** — Full view file (modal pattern line 237-271, score input line 184-187, JS line 256-398)
- **`Views/Admin/_PreviewQuestion.cshtml`** — Score display in preview (line 67)
- **`Views/Admin/AssessmentMonitoringDetail.cshtml:385-419`** — Essay scoring UI (uses ScoreValue dynamically)
- **`Models/AssessmentPackage.cs:27-61`** — PackageQuestion entity (ScoreValue property, default 10)
- **`Models/PackageUserResponse.cs`** — Response entity (FK to PackageQuestion + AssessmentSession)
- **`Data/ApplicationDbContext.cs:480`** — Composite index `(AssessmentSessionId, PackageQuestionId)`
- **`Migrations/20260322032905_MigrateLegacyQuestionsAndDropTables.cs:126`** — DB CK constraint `[ScoreValue] > 0`
- **`.planning/codebase/STACK.md`** — Tech stack (.NET 8, ASP.NET Core MVC, Bootstrap 5.3, ClosedXML, Playwright)
- **`.planning/codebase/TESTING.md`** — Testing infrastructure (Playwright E2E only, no C# unit tests)

### Secondary (MEDIUM confidence)

- **Phase 305 SUMMARY** (`305-01-SUMMARY.md`, `305-CONTEXT.md`) — pattern precedent for view edits + helper class + flash error format

### Tertiary (LOW confidence)

- None — Phase 306 entirely scoped against verified codebase artifacts.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all dependencies pre-existing in codebase, version-locked via NuGet
- Architecture: HIGH — patterns extracted directly from production code (3 distinct callsites for audit log defensive pattern)
- Pitfalls: HIGH — 6 pitfalls identified from code review of existing patterns + edge case analysis
- Validation: MEDIUM — codebase has no C# unit tests; manual UAT pattern is established but ad-hoc

**Research date:** 2026-04-28
**Valid until:** 2026-05-12 (14 days — codebase changes infrequent)

---

## RESEARCH COMPLETE

**Phase:** 306 — Score Editable per Question Type
**Confidence:** HIGH

### Key Findings

1. **Audit log pattern verified** — `_auditLog.LogAsync(userId, actorName, action, details, targetId?, targetType?)` is established at 9+ call sites; defensive try/catch with `_logger.LogWarning(auditEx, ...)` fallback is the recommended sub-pattern (verified at lines 1342, 2017). Recommended template extracted verbatim from line 2003-2018 (`DeleteAssessment` audit).

2. **Modal pattern fully extracted** — `editTypeWarningModal` HTML at lines 237-253, init JS at 260-271, trigger logic at 274-287. Phase 306 can replicate as `editScoreWarningModal` with form-submit interceptor pattern. Critical pitfall: must set `form.dataset.confirmed = 'true'` flag to avoid re-trigger loop.

3. **PackageUserResponses count query optimal** — Index `(AssessmentSessionId, PackageQuestionId)` exists at ApplicationDbContext.cs:480. Query: `.Where(r => r.PackageQuestionId == X).Select(r => r.AssessmentSessionId).Distinct().CountAsync()`. **DISTINCT required** for MA questions (multiple rows per session-question pair).

4. **Zero downstream consumer assumes ScoreValue == 10** — Verified via grep: 10 read sites + 4 write sites in production code, all treat ScoreValue as variable arithmetic. Scoring formula at GradingService.cs:82-110 + CMPController.cs:1646-1699 is varied-aware. NO additional code change beyond locked decisions.

5. **ImportQuestions defer = correct** — Excel template has 9 columns (NO ScoreValue column), parser hardcodes `ScoreValue = 10` at line 4482. Adding scoreValue Excel parsing would be breaking change to template binary. Defer is right call.

### File Created

`.planning/phases/306-score-editable-per-question-type/306-RESEARCH.md`

### Confidence Assessment

| Area | Level | Reason |
|------|-------|--------|
| Standard Stack | HIGH | All dependencies pre-existing, version-locked |
| Architecture | HIGH | Patterns extracted from 3+ production callsites each |
| Existing Patterns (audit/modal/flash) | HIGH | Direct code citations with line numbers |
| Downstream Consumer Analysis | HIGH | Full grep performed; 10+10+4 sites verified |
| ImportQuestions Decision | HIGH | Both controller methods inspected (DownloadQuestionTemplate + ImportPackageQuestions) |
| Validation Architecture | MEDIUM | Codebase = manual UAT only; no automated test for QSCR-01 expected per phase 304/305 precedent |
| Risks & Edge Cases | HIGH | 8 edge cases identified including loop, race, accessibility, DB constraint conflict |
| Plan Structure | HIGH | 1 plan / 9 tasks / 30-45 min effort recommended; matches phase 305 precedent |

### Open Questions

- CD-05 (CreateQuestion audit Y/N) — recommended YES default
- CD-01 (help text wording) — recommended drop entirely
- CD-03 (Total Points format) — recommended "• Total {N} poin"

These are Claude's Discretion items — implementer can decide during plan or execute.

### Ready for Planning

Research complete. Planner dapat membuat **1 PLAN.md (306-01-PLAN.md)** dengan 7-9 tasks mengikuti rekomendasi Recommended Plan Structure section. Tidak ada riset gap yang perlu dieksplorasi lebih lanjut.
