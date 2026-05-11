---
phase: 306-score-editable-per-question-type
reviewed: 2026-04-28T00:00:00Z
depth: standard
files_reviewed: 2
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
  - Views/Admin/ManagePackageQuestions.cshtml
findings:
  critical: 0
  warning: 2
  info: 4
  total: 6
status: issues_found
---

# Phase 306: Code Review Report

**Reviewed:** 2026-04-28
**Depth:** standard
**Files Reviewed:** 2
**Status:** issues_found

## Ringkasan

Review dilakukan terhadap perubahan Phase 306 (score-editable-per-question-type) yang mencakup 6 commit dari `d9427c14` ke `HEAD`. Ruang lingkup mencakup penambahan validasi server-side (range 1-100), audit log entries (`EditQuestion-ScoreChange` dan `CreateQuestion-CustomScore`), perluasan JSON GET dengan field `affectedSessions`, modal warning client-side, dan submit handler JS di view.

Kualitas perubahan secara keseluruhan baik — minimal-change diterapkan secara konsisten, validation order benar (range check → type-specific validation → mutation → audit), dan resilience pattern (try/catch audit) sudah tepat. Namun ada **2 Warning** terkait scope try/catch audit yang belum lengkap dan satu data-attribute yang mismatch antara HTML markup dan JS handler. **4 Info** mencakup magic number, character encoding nit, dan naming inconsistency.

Catatan: temuan FK violation pada `_context.PackageOptions.RemoveRange(q.Options)` (line 4891) **EXCLUDED dari scope review** sesuai instruksi orchestrator (sudah didokumentasikan di 306-02-SUMMARY.md sebagai known finding out-of-scope).

## Warnings

### WR-01: `affectedSessionsCount` query berada di luar try/catch audit guard

**File:** `Controllers/AssessmentAdminController.cs:4917-4921`
**Issue:** Pada handler `EditQuestion` POST, query untuk menghitung `affectedSessionsCount` (yang dipakai untuk audit message) ditempatkan **di luar** try/catch yang membungkus operasi audit log. Resilience pattern di Phase 306 secara eksplisit dirancang agar kegagalan audit tidak membatalkan operasi utama (SaveChanges sudah commit di line 4912). Namun jika query `affectedSessionsCount` gagal (mis. transient DB disconnect, timeout), exception akan propagate ke caller dan menampilkan error 500 ke user — padahal soal sudah tersimpan dengan benar. Hal ini melanggar intent "audit failure must not surface as user-facing error" yang menjadi dasar pattern try/catch di line 4923-4940.

**Fix:** Pindahkan query `affectedSessionsCount` ke dalam blok try, atau bungkus seluruh blok audit (query + log) dalam satu try/catch:

```csharp
if (scoreValue != oldScore)
{
    try
    {
        var affectedSessionsCount = await _context.PackageUserResponses
            .Where(r => r.PackageQuestionId == questionId)
            .Select(r => r.AssessmentSessionId)
            .Distinct()
            .CountAsync();

        var currentUser = await _userManager.GetUserAsync(User);
        var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
            ? (currentUser?.FullName ?? "Unknown")
            : $"{currentUser.NIP} - {currentUser.FullName}";
        await _auditLog.LogAsync(
            currentUser?.Id ?? "",
            actorName,
            "EditQuestion-ScoreChange",
            $"Question #{q.Id} (Order {q.Order}, Package #{packageId}) ScoreValue: {oldScore} -> {scoreValue} ({affectedSessionsCount} sessions affected)",
            q.Id,
            "PackageQuestion");
    }
    catch (Exception auditEx)
    {
        _logger.LogWarning(auditEx, "Audit logging failed during EditQuestion-ScoreChange for Question {Id}", q.Id);
    }
}
```

### WR-02: Atribut `data-affected-sessions` pada `<input>` tidak pernah dibaca (misleading dead attribute)

**File:** `Views/Admin/ManagePackageQuestions.cshtml:187` dan referensi di JS line 310
**Issue:** Markup Razor mendeklarasikan `data-affected-sessions="0"` pada element `<input id="scoreValue">` (line 187). Namun JS submit handler membaca nilai ini dari `form.dataset.affectedSessions` (line 310), **bukan** dari `scoreInput.dataset.affectedSessions`. Function `populateEditForm` (line 412) set atribut tersebut pada element `<form>`, bukan pada input. Akibatnya, atribut HTML pada input tidak pernah dibaca dan menyesatkan reader yang akan mengasumsikan input adalah source-of-truth untuk state ini.

Skenario edge: user yang langsung submit di create-mode tanpa pernah memanggil `populateEditForm` tidak akan terkena dampak (karena submit handler return early jika `editQuestionId` kosong, line 303-304). Namun atribut HTML statis ini tetap tidak digunakan sama sekali — pure dead code yang menambah confusion.

**Fix:** Pilih satu lokasi sebagai source-of-truth. Rekomendasi: hapus `data-affected-sessions="0"` dari input (line 187), karena form-level lebih natural untuk state per-edit-session:

```html
<input type="number" name="scoreValue" id="scoreValue"
       class="form-control form-control-sm" value="10"
       min="1" max="100" step="1" required style="max-width:80px"
       data-original-score="10" />
```

Atau, jika tetap ingin atribut di input untuk konsistensi dengan `data-original-score`, ubah JS line 310 untuk membaca dari `scoreInput.dataset.affectedSessions` dan ubah `populateEditForm` line 412 set pada `scoreValue.dataset` bukan `questionForm.dataset`.

## Info

### IN-01: Magic number `10` untuk default score di audit guard

**File:** `Controllers/AssessmentAdminController.cs:4741`
**Issue:** Kondisi `if (scoreValue != 10)` di handler `CreateQuestion` POST membandingkan dengan literal `10`, padahal default score juga muncul sebagai literal di view (line 185: `value="10"`) dan di model binding fallback. Jika di masa depan default score berubah, semua tiga lokasi harus disinkronkan manual.

**Fix:** Ekstrak ke konstanta di controller atau model:

```csharp
private const int DefaultScoreValue = 10;
// ...
if (scoreValue != DefaultScoreValue)
{
    // audit log
}
```

### IN-02: Karakter Unicode `→` (U+2192) di string audit message

**File:** `Controllers/AssessmentAdminController.cs:4933`
**Issue:** String audit description menggunakan arrow Unicode `→` (`ScoreValue: {oldScore} → {scoreValue}`). Walaupun .NET string adalah UTF-16 dan SQL Server NVARCHAR mendukung karakter ini, message yang di-output ke log file plaintext, console, atau email notification dengan encoding non-Unicode bisa muncul sebagai mojibake. Sama halnya untuk en-dash `–` di view (line 188 "Range 1–100") dan modal body em-dash (line 332).

**Fix:** Gunakan ASCII `->` di string audit untuk safety lintas-encoding:

```csharp
$"Question #{q.Id} (Order {q.Order}, Package #{packageId}) ScoreValue: {oldScore} -> {scoreValue} ({affectedSessionsCount} sessions affected)"
```

Karakter Unicode di view (Razor file) lebih aman karena di-render ke HTML dengan UTF-8 encoding, namun pastikan file disimpan sebagai UTF-8 (with or without BOM) konsisten.

### IN-03: Naming inconsistency: `affectedSessions` (GET) vs `affectedSessionsCount` (POST)

**File:** `Controllers/AssessmentAdminController.cs:4797` dan `4917`
**Issue:** Variabel hasil query yang sama (count distinct AssessmentSessionId per PackageQuestionId) di-named berbeda di dua tempat: `affectedSessions` di handler GET (line 4797) dan `affectedSessionsCount` di handler POST audit (line 4917). Konsistensi penamaan memudahkan future refactor ke helper method.

**Fix:** Pilih satu nama dan terapkan konsisten — `affectedSessionsCount` lebih deskriptif (menunjukkan ini adalah count, bukan list):

```csharp
// Di GET line 4797:
var affectedSessionsCount = await _context.PackageUserResponses
    .Where(r => r.PackageQuestionId == q.Id)
    .Select(r => r.AssessmentSessionId)
    .Distinct()
    .CountAsync();
// dan field JSON tetap dinamai affectedSessions agar tidak break client (line 4810):
affectedSessions = affectedSessionsCount,
```

### IN-04: Duplikasi query `affectedSessions` antara GET dan POST handler

**File:** `Controllers/AssessmentAdminController.cs:4797-4801` dan `4917-4921`
**Issue:** Query identik untuk menghitung affected sessions count diduplikasi di dua handler. Bukan bug, tapi kandidat untuk extract ke private helper method untuk DRY principle dan agar future change (mis. tambah filter status session) hanya satu titik.

**Fix:** Ekstrak ke helper:

```csharp
private async Task<int> CountAffectedSessionsAsync(int packageQuestionId)
{
    return await _context.PackageUserResponses
        .Where(r => r.PackageQuestionId == packageQuestionId)
        .Select(r => r.AssessmentSessionId)
        .Distinct()
        .CountAsync();
}
```

Lalu pakai `await CountAffectedSessionsAsync(q.Id)` di GET dan `await CountAffectedSessionsAsync(questionId)` di POST audit.

---

_Reviewed: 2026-04-28_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
_Diff base: d9427c1492d7151d10c8c2ad2686260e2f254f65..HEAD_
