---
phase: 310-essay-finalize-idempotency
type: code-review
depth: standard
reviewed: 2026-05-05
files_reviewed: 5
status: issues_found
findings:
  critical: 0
  warning: 4
  info: 5
  total: 9
---

# Phase 310 — Code Review Report

**Reviewed:** 2026-05-05
**Depth:** standard
**Reviewer:** gsd-code-reviewer (orchestrator-transcribed)

## Files Reviewed

- `Models/AssessmentMonitoringViewModel.cs`
- `Controllers/AssessmentAdminController.cs` (focus L2715-2918 `FinalizeEssayGrading`)
- `Services/WorkerDataService.cs` (focus L314-363 `NotifyIfGroupCompleted`)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` (focus L414-442 D-02 gate, L1303-1458 JS handlers)
- `tests/e2e/assessment.spec.ts` (FLOW 9 scaffold)

## Findings Summary

| Severity | Count |
|----------|-------|
| Critical | 0 |
| Warning  | 4 |
| Info     | 5 |
| **Total**| **9** |

---

## Warnings

### WR-01: D-03 LOCKED path bisa render pesan "pada  WIB" (double-space) saat `CompletedAt` null

**File:** `Controllers/AssessmentAdminController.cs:2738`
**Issue:** Pada cabang D-03 LOCKED (Status sudah `Completed`), pesan dirakit dengan format string `$"Penilaian sudah diselesaikan sebelumnya pada {session.CompletedAt:dd MMM yyyy HH:mm} WIB"`. Karena `CompletedAt` adalah `DateTime?`, kalau nilainya `null` (jarang tapi mungkin kalau ada data lama / di-edit manual), interpolasi `{null:dd MMM yyyy HH:mm}` menghasilkan string kosong → user melihat "Penilaian sudah diselesaikan sebelumnya pada  WIB" (double space). View Razor sudah menangani case ini (L420-422 dengan `HasValue` ternary), tapi controller belum. Race-lost path L2843 punya masalah identik (`current?.CompletedAt:...`).

**Fix:**
```csharp
var completedAtText = session.CompletedAt.HasValue
    ? $" pada {session.CompletedAt.Value:dd MMM yyyy HH:mm} WIB"
    : "";
return Json(new {
    success = true,
    alreadyFinalized = true,
    message = $"Penilaian sudah diselesaikan sebelumnya{completedAtText}",
    score = session.Score,
    isPassed = session.IsPassed,
    nomorSertifikat = session.NomorSertifikat
});
```
Apply same guard di race-lost path L2843.

### WR-02: Test 9.2 verifies `data.message` mengandung literal "Penilaian sudah diselesaikan sebelumnya pada", tapi controller bisa kirim format berbeda kalau CompletedAt null

**File:** `tests/e2e/assessment.spec.ts:354`
**Issue:** Assertion `expect(response.message).toContain('Penilaian sudah diselesaikan sebelumnya pada')` akan **gagal kalau WR-01 terealisir** dan controller dibetulkan menghilangkan kata "pada" saat CompletedAt null. Selain itu, test 9.2 click button finalize PERTAMA via UI (`finalizeBtn.click()`) **setelah** sudah `page.on('dialog', dialog => dialog.accept())`, lalu `waitForLoadState('networkidle')`. Kalau click pertama itu sukses (path normal `location.reload()`), `'networkidle'` ada kemungkinan race terhadap reload navigation — fragile.

**Fix:** Setelah memperbaiki WR-01, ubah assertion menjadi:
```ts
expect(response.message).toContain('Penilaian sudah diselesaikan sebelumnya');
expect(response.message).toMatch(/(WIB|sebelumnya$)/);
```
Selain itu, gunakan `await page.waitForURL` atau `await Promise.all([page.waitForResponse(...), finalizeBtn.click()])` untuk explicit await reload dari first click.

### WR-03: NotifyIfGroupCompleted dedup window guard pakai `Schedule.Date` — ada race kalau notif pertama dibuat ber-jam sebelum Schedule.Date

**File:** `Services/WorkerDataService.cs:336-341`
**Issue:** Filter `n.CreatedAt >= completedSession.Schedule.Date` mengasumsikan notif untuk grup ini selalu dikirim ON OR AFTER tanggal Schedule. Ini benar untuk flow normal (finalize pasti setelah jadwal), tetapi:
1. `Schedule` disimpan dalam local time / UTC mix di codebase ini (cek pattern Phase 215+ — `CompletedAt = DateTime.UtcNow` di L2827, sedangkan `Schedule` sering di-set dari user input tanpa timezone tag). Comparison `n.CreatedAt >= Schedule.Date` bisa salah membandingkan UTC ↔ local kalau Schedule.Date sudah lewat tengah malam UTC tapi belum lewat tengah malam local.
2. Kalau ada manual data backfill / re-trigger finalize untuk grup yang Schedule-nya di masa depan (tidak biasa, tapi mungkin via testing), guard `>= Schedule.Date` jadi terlalu permisif.
3. Tidak ada upper bound — misal grup yang sama digunakan ulang setelah edit, dedup akan False-Positive forever.

**Fix:** Tambahkan upper bound time window (mis. 24-48 jam) DAN normalize ke UTC consistent:
```csharp
var windowStart = DateTime.UtcNow.AddDays(-2);
bool alreadySent = await _context.UserNotifications.AnyAsync(n =>
    n.UserId == recipientId
    && n.Type == "ASMT_ALL_COMPLETED"
    && n.Title == "Assessment Selesai"
    && n.Message.Contains(completedSession.Title)
    && n.CreatedAt >= windowStart);
```
Atau, idealnya, schema-extend `UserNotifications` dengan `SourceEntityId/SourceTitle` proper key untuk dedup deterministic (sudah disebut di komentar L334).

### WR-04: D-04 status switch tidak pakai constant untuk `"InProgress"` dan `"Cancelled"` — drift risiko

**File:** `Controllers/AssessmentAdminController.cs:2748-2754`
**Issue:** Switch arm pakai literal `"InProgress"` dan `"Cancelled"` sebagai case label. `AssessmentConstants.AssessmentStatus` (Models/AssessmentConstants.cs L13-19) saat ini hanya define `Open / Upcoming / Completed / PendingGrading` — tidak ada `InProgress` / `Cancelled`. CONTEXT phase 310 L101 menyatakan "Phase 310 WAJIB pakai constant, BUKAN literal". Saat ini Open pakai constant (`AssessmentConstants.AssessmentStatus.Open`) tapi InProgress/Cancelled literal — inkonsisten dan bisa silent-drift kalau status string berubah di tempat lain.

**Fix:** Tambahkan dua const di `AssessmentConstants.AssessmentStatus`:
```csharp
public const string InProgress = "InProgress";
public const string Cancelled  = "Cancelled";
```
Lalu refactor switch:
```csharp
var statusMsg = session.Status switch
{
    AssessmentConstants.AssessmentStatus.Open        => "Belum bisa di-finalize. Peserta belum mulai mengerjakan ujian.",
    AssessmentConstants.AssessmentStatus.InProgress  => "Belum bisa di-finalize. Peserta sedang mengerjakan ujian.",
    AssessmentConstants.AssessmentStatus.Cancelled   => "Tidak bisa di-finalize. Session sudah dibatalkan.",
    _                                                => $"Tidak bisa di-finalize. Status saat ini: {session.Status}."
};
```
Note default arm `$"Status saat ini: {session.Status}"` mempassthrough nilai status apa pun ke client — bukan user-controlled input (admin-authenticated, value berasal dari DB/enum), tapi pertimbangkan untuk replace dengan generic message agar konsisten dengan T-310-08 hardening.

---

## Info

### IN-01: `essayQuestions` dan `essayResponses` validation incomplete kalau ada essay tanpa response row sama sekali

**File:** `Controllers/AssessmentAdminController.cs:2766-2776`
**Issue:** Cek "semua essay sudah dinilai" pakai `essayResponses.Any(r => r.EssayScore == null)`. Kalau peserta skip soal essay sehingga tidak ada `PackageUserResponses` row untuk soal itu sama sekali, cek ini lolos (zero null). Idempotency Phase 310 tidak introduce regression ini, tapi worth flagged.

**Fix (defensive):** Bandingkan `essayResponses.Count` dengan `essayQuestions.Count`, atau iterate `essayQuestions` dan untuk masing-masing soal pastikan ada response dengan EssayScore non-null. Out of scope Phase 310, layak diangkat ke phase berikut.

### IN-02: Tooltip text di view dan controller tidak DRY — risiko copy drift

**File:** `Views/Admin/AssessmentMonitoringDetail.cshtml:421` & `Controllers/AssessmentAdminController.cs:2738`
**Issue:** String pattern "...pada {dt:dd MMM yyyy HH:mm} WIB" duplicated di tiga tempat (view tooltip, controller D-03 LOCKED, controller race-lost). Kalau format diubah di satu tempat, lainnya silent drift.

**Fix:** Extract helper static method, mis. `AssessmentTimeFormatter.FormatCompletedAt(DateTime?)` returning "pada {fmt} WIB" atau "" — single source of truth.

### IN-03: Test 9.1/9.2/9.3 pakai PLACEHOLDER fixture yang otomatis `test.skip()` — silent green CI

**File:** `tests/e2e/assessment.spec.ts:283,290-292,315,319-321,360,363-366`
**Issue:** Ketiga test FLOW 9 akan otomatis SKIP kalau seed fixture tidak ada. Di CI environment yang fresh, test selalu skip → coverage gap tidak terlihat di test report. Ini sudah di-acknowledge di komentar (Wave 1 manual seed required), tapi sebaiknya: (a) tambahkan check explicit di `test.beforeAll` yang **fail loud** kalau env var `PHASE310_FIXTURES=1` di-set tapi seed missing, atau (b) buat helper seeder lewat API admin.

**Fix:** Ganti `test.skip(true, ...)` jadi conditional: skip kalau env `CI=1 && PHASE310_FIXTURES=0`, fail kalau env `PHASE310_FIXTURES=1` tapi data missing.

### IN-04: Test 9.3 derive `sessionId` dari URL path tail — fragile kalau routing berubah

**File:** `tests/e2e/assessment.spec.ts:375`
**Issue:** `parseInt(window.location.pathname.split('/').pop() || '0')` mengasumsikan URL berakhir dengan numeric ID. Kalau routing future ditambah query string atau slug, parse jadi NaN→0 dan request ke backend kirim sessionId=0 (controller will return "Session tidak ditemukan" — bukan error D-04 yang test harapkan).

**Fix:** Pakai `data-session-id` attribute dari `.btn-finalize-grading` (sama seperti test 9.2 line 339), atau extract dari hidden input.

### IN-05: ViewModel `Status` field comment salah-typo "Dibatalkan" sebagai status mentah

**File:** `Models/AssessmentMonitoringViewModel.cs:65`
**Issue:** Komentar L65 menyebut `UserStatus` di-remap ke `"Dibatalkan"`, tapi cek L1293 view JS pakai literal `'Dibatalkan'` — konsisten. Namun controller mapping di L2570 belum verified — kemungkinan UserStatus value: `"Not started" | "In Progress" | "Completed" | "Dibatalkan" | "Abandoned"`. Komentar di ViewModel tidak menyebut "Abandoned" — minor doc drift.

**Fix:** Update komentar XML L65 untuk include semua kemungkinan UserStatus values, atau ganti dengan reference ke konstanta tunggal.

---

## Highlights (positive)

- **Idempotency contract correct end-to-end:** D-03 friendly no-op + D-06 ExecuteUpdateAsync rowsAffected guard + D-07 audit gating semuanya wired dengan benar. Race-lost path L2829-2848 reads current state setelah race lost dan return same shape JSON sebagai D-03 LOCKED — frontend tidak perlu branch tambahan.
- **D-05 dedup correct:** Dedup `UserNotifications.AnyAsync` dengan 5 field check (UserId, Type, Title, Message.Contains, CreatedAt window) sesuai LOCKED contract. EF Core translate `Message.Contains` ke SQL `LIKE` dengan parameterization (no SQL injection).
- **T-310-08 XSS mitigated:** `showAlert(type, icon, message)` di view L1307-1324 menggunakan `insertAdjacentHTML('afterbegin', html)` dengan message yang dirakit dari server-side BI literal strings (controller hard-coded di switch arm). Tidak ada user-controlled input passthrough ke message — D-04 default arm `Status saat ini: {session.Status}` adalah satu-satunya passthrough, value-nya dari DB enum status (admin-controlled).
- **Pitfall #6 fix correct:** Disabled button di-wrap `<span data-bs-toggle="tooltip">` dengan `pointer-events: none` di button — tooltip akan fire di span ancestor saat mouseenter (L427-434).
- **Tooltip activation correct:** `DOMContentLoaded` listener di L1455-1458 instantiate `bootstrap.Tooltip` untuk semua `[data-bs-toggle="tooltip"]` — canonical pattern.
- **Bahasa Indonesia copy correct:** Semua user-facing string dalam BI; AuditLog Action name `"FinalizeEssayGrading"` dalam English (machine-readable) sesuai konvensi.
- **Pattern parity dengan Phase 309-03 GradingService:** capture rowsAffected → guard side-effects → race-lost early return — 1:1 sama.

---

## Recommended Next Steps

1. **WR-01 fix critical-ish** untuk message rendering — apply nullable guard di controller, perbaiki test 9.2 assertion (WR-02). Bisa run via `/gsd-code-review-fix 310`.
2. **WR-04 fix** quick — tambah dua const + refactor switch arm.
3. **WR-03 dedup hardening** — apply UTC normalize + bounded window. Schema fix (extend UserNotifications dengan SourceEntityId) bisa di-track ke phase berikut.
4. **IN findings** layak di-track sebagai phase 310 tech-debt; non-blocking untuk closure.

Phase 310 OK untuk closure dengan WR-01, WR-04 di-fix sebelum next finalize lifecycle production. WR-03 dedup window optional improvement (current implementation correct untuk happy path, edge case unlikely). IN findings non-blocking.
