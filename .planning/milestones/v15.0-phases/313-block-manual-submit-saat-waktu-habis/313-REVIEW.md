---
phase: 313-block-manual-submit-saat-waktu-habis
reviewed: 2026-05-08T00:00:00Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - Controllers/CMPController.cs
  - Views/CMP/ExamSummary.cshtml
  - Views/CMP/StartExam.cshtml
  - tests/e2e/exam-taking.spec.ts
findings:
  critical: 2
  warning: 4
  info: 5
  total: 11
status: issues_found
---

# Phase 313: Code Review Report

**Reviewed:** 2026-05-08
**Depth:** standard
**Files Reviewed:** 4
**Status:** issues_found

## Summary

Review fokus pada perubahan Phase 313 (TMR-01 — block manual submit saat waktu habis): helper baru
`EnsureCanSubmitExamAsync` + `WriteSubmitBlockedAuditAsync` di `CMPController.cs`, perubahan tombol/retry
di `ExamSummary.cshtml`, modal info-only + auto-submit fire-paralel di `StartExam.cshtml`, serta
penambahan FLOW 313 di `exam-taking.spec.ts`.

Implementasi sudah meniru pattern Phase 312 dengan baik (helper extraction, audit-log swallow,
defense-in-depth exclude `Manual` type). Namun ditemukan dua isu **Critical** yang berpotensi
men-defeat tujuan utama TMR-01:

1. **CR-01** — `isAutoSubmit` hanya dibaca dari form-field client, sehingga attacker bisa spoof
   `isAutoSubmit=true` untuk bypass Tier-1 (no-grace) dan jatuh ke Tier-2 (2-min grace). Inilah yang
   seharusnya dicegah oleh Phase 313.
2. **CR-02** — Auto-submit retry handler memperlakukan redirect Tier-2 sebagai sukses
   (`r.redirected = true`) sehingga melompati counter `maxAttempts=3`. Setelah grace habis, browser
   client bisa loop StartExam → ExamSummary → SubmitExam → redirect StartExam tanpa batas.

Sisanya warning (race double-submit pada path EXAM_EXPIRED, inkonsistensi unit perbandingan elapsed)
dan info (DRY, truncasi cast int, regex pattern).

---

## Critical Issues

### CR-01: `isAutoSubmit` spoofing bypasses Tier-1 enforcement (TMR-01 defeated)

**File:** `Controllers/CMPController.cs:1556` + `Controllers/CMPController.cs:4550`
**Issue:**
Parameter `bool isAutoSubmit` di-bind langsung dari form field `name="isAutoSubmit"` (lihat
`Views/CMP/ExamSummary.cshtml:119`). Tidak ada cross-check ke kondisi server (mis. apakah user benar-benar
sudah lewat waktu, apakah request datang dari path auto-submit yang sah).

Tier-1 di `EnsureCanSubmitExamAsync` line 4550:
```csharp
if (!isAutoSubmit && elapsed.TotalMinutes > allowedMinutes) { ... reject ... }
```

Attacker yang sudah lewat waktu cukup buka DevTools, ubah hidden field `isAutoSubmit` dari `false`
ke `true`, klik submit. Kondisi `!isAutoSubmit` jadi `false` → Tier-1 tidak fire. Falls through ke
Tier-2 yang baru block setelah `allowedMinutes + 2`. User dapat 2 menit "free" yang seharusnya
ditiadakan oleh D-09 strict-0-grace. **Ini meniadakan tujuan TMR-01.**

Bukti server tidak validasi: di `SubmitExam` line 1580-1585 server menghitung `serverTimerExpired`
dari elapsed/allowed, tapi nilai itu hanya dipakai untuk allow-incomplete check (line 1588), tidak
disilangkan dengan `isAutoSubmit` flag.

**Fix:**
Ganti dependency pada client-supplied `isAutoSubmit` dengan kombinasi server-truth + client-hint.
Pattern: kalau server menghitung `elapsed > allowedMinutes`, maka untuk Tier-1 anggap submission
itu manual KECUALI ada bukti server-side path auto-submit (mis. flag dari `EXAM_EXPIRED` GET branch
yang tidak bisa dispoof, atau anti-forgery + referer check ke route auto-submit khusus).

```csharp
private async Task<IActionResult?> EnsureCanSubmitExamAsync(
    AssessmentSession assessment,
    bool isAutoSubmitClientHint)
{
    if (!IsTimedAssessmentType(assessment.AssessmentType)) return null;
    if (!assessment.StartedAt.HasValue) return null;

    var elapsed = DateTime.UtcNow - assessment.StartedAt.Value;
    int allowedMinutes = assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0);
    int graceLimitMinutes = allowedMinutes + 2;

    bool serverElapsedPastAllowed = elapsed.TotalMinutes > allowedMinutes;
    bool serverElapsedPastGrace   = elapsed.TotalMinutes > graceLimitMinutes;

    // D-09 hardening: client hint TIDAK boleh meng-bypass Tier-1.
    // Tier-1: jika elapsed > allowed dan request bukan dari path auto-submit yang trustworthy,
    // tolak. Trustworthy = server menyimpan signal di session/TempData saat ExamSummary dirender
    // dengan TimerExpired=true, ATAU pakai HMAC-signed token yang generated server-side.
    bool serverApprovedAutoSubmit = TryConsumeAutoSubmitToken(assessment.Id); // new helper

    if (serverElapsedPastAllowed && !serverApprovedAutoSubmit)
    {
        await WriteSubmitBlockedAuditAsync(assessment, elapsed, allowedMinutes);
        TempData["Error"] = "Waktu ujian Anda sudah habis. ...";
        return RedirectToAction("StartExam", new { id = assessment.Id });
    }
    if (serverElapsedPastGrace)
    {
        TempData["Error"] = "Waktu ujian Anda telah habis. Pengiriman jawaban tidak dapat diproses.";
        return RedirectToAction("StartExam", new { id = assessment.Id });
    }
    return null;
}
```

Implementasi `TryConsumeAutoSubmitToken` paling sederhana: simpan one-shot token di
`TempData["AutoSubmitToken_{sessionId}"]` saat `ExamSummary` GET render dengan
`TimerExpired=true`, embed di hidden field, lalu pada `SubmitExam` validate + consume.
Token itu signed/random, tidak bisa ditebak attacker.

Alternatif minimal-effort kalau threat model menganggap user ujian tidak akan pakai DevTools:
dokumentasikan eksplisit di NOTES.md / DECISIONS.md bahwa Phase 313 mengandalkan goodwill client
dan bukan defense terhadap power-user. Tapi itu meniadakan klaim "server-side enforcement".

---

### CR-02: Tier-2 redirect dianggap sukses oleh fetch retry → potensi infinite loop

**File:** `Views/CMP/ExamSummary.cshtml:182-200`
**Issue:**
Handler `attemptSubmit()` memperlakukan `r.redirected === true` sebagai jalur happy path:
```js
if (r.redirected) { window.location.href = r.url; return; }
```
Tetapi Tier-2 (`elapsed > allowedMinutes + 2`) di `EnsureCanSubmitExamAsync` line 4560-4563
mengembalikan `RedirectToAction("StartExam", new { id })` — fetch otomatis follow → `r.redirected=true`,
`r.url = "/CMP/StartExam/{id}"`. JS lalu set `window.location.href` ke URL itu.

Sequence loop:
1. Timer di StartExam habis → `examForm.submit()` POST ke ExamSummary
2. ExamSummary render dengan `TimerExpired=true` → auto-submit JS fire fetch ke SubmitExam
3. Server hitung elapsed > grace → Tier-2 redirect ke StartExam
4. Fetch follow → JS arahkan window ke StartExam
5. StartExam render dengan `EXAM_EXPIRED=true` → `setTimeout(() => examForm.submit(), 5000)` (line 1128-1130)
6. POST ke ExamSummary → kembali ke step 2

Counter `maxAttempts=3` tidak melindungi karena reset tiap page-load. Loop berhenti hanya kalau:
- HC manually `examClosed` via SignalR (set `Status=Completed` di server, line 1572 jaga)
- User tutup tab
- Tier-1/Tier-2 logic berubah selama loop (tidak akan)

Worst case: tab user looping POST tiap ~5 detik → server load + audit-log noise (tier-2 tidak audit
per D-06, tapi 1572 "already completed" check juga tidak fire karena Tier-2 redirect terjadi
SEBELUM grading flow yang men-set Status=Completed).

**Fix:**
Deteksi redirect ke StartExam sebagai signal "submit ditolak" di handler retry, JANGAN langsung
follow. Beri user banner permanen seperti `showRetryFailBanner()`.

```js
function attemptSubmit() {
    attempt++;
    var fd = new FormData(form);
    return fetch(form.action, { method: 'POST', body: fd, redirect: 'follow' })
        .then(function (r) {
            if (r.redirected) {
                // Distinguish: redirect ke /CMP/Results = sukses, redirect ke /CMP/StartExam = tolak.
                if (/\/CMP\/StartExam(\/|\?|$)/i.test(r.url)) {
                    showRetryFailBanner('Server menolak submit (waktu sudah lewat grace). Hubungi admin.');
                    return; // STOP retry chain, jangan navigate.
                }
                window.location.href = r.url;
                return;
            }
            if (!r.ok) throw new Error('HTTP ' + r.status);
            window.location.reload();
        })
        .catch(function (err) {
            console.error('[Phase 313] Submit attempt ' + attempt + ' failed', err);
            if (attempt < maxAttempts) {
                setTimeout(attemptSubmit, delays[attempt - 1]);
            } else {
                showRetryFailBanner();
            }
        });
}
```

Pertimbangkan juga di server side: kalau Tier-2 fire, set `assessment.Status = "Abandoned"` atau
`"TimedOut"` agar guard di line 1572 mencegah re-entry. Itu memutus loop di server-level.

---

## Warnings

### WR-01: Race double-submit antara updateTimer() dan EXAM_EXPIRED init block

**File:** `Views/CMP/StartExam.cshtml:463-475` + `Views/CMP/StartExam.cshtml:1117-1130`
**Issue:**
`submitted` guard flag dipakai di `updateTimer()` (line 468) dan `visibilitychange` (line 485) tetapi
TIDAK dipakai di branch `EXAM_EXPIRED` (line 1118-1130). Kalau saat page-load `REMAINING_SECONDS_FROM_DB <= 0`,
maka:
1. `setInterval(updateTimer, 1000)` di line 477 dijadwalkan
2. `updateTimer()` segera dipanggil sekali di line 478 → `remaining <= 0` → submit fire + `submitted=true`
3. Setelah init block, line 1118 `if (EXAM_EXPIRED)` juga fire `examForm.submit()` di line 1126/1129

Browser umumnya tolak double-submit pada form yang sudah di-submit, tapi handler `setTimeout 5000` di
line 1128-1130 fire belakangan dengan kondisi yang tidak dicek lagi → submit kedua dapat berjalan
kalau page belum unload.

**Fix:**
Gunakan flag `submitted` yang sama di EXAM_EXPIRED branch:
```js
if (EXAM_EXPIRED) {
    clearInterval(timerInterval);
    clearInterval(saveInterval);
    window.onbeforeunload = null;
    if (!submitted) {
        const expiredModal = new bootstrap.Modal(document.getElementById('examExpiredModal'));
        expiredModal.show();
        document.querySelector('#examExpiredModal button').addEventListener('click', function() {
            if (submitted) return;
            submitted = true;
            document.getElementById('examForm').submit();
        });
        setTimeout(function() {
            if (submitted) return;
            submitted = true;
            document.getElementById('examForm').submit();
        }, 5000);
    }
}
```

---

### WR-02: Inkonsistensi satuan perbandingan elapsed (seconds vs minutes float)

**File:** `Controllers/CMPController.cs:4545-4550` vs `Controllers/CMPController.cs:1583-1585`
**Issue:**
- ExamSummary GET (line 1538-1542) menghitung `serverTimerExpired = elapsed >= allowed` (detik, ≥)
- SubmitExam awal (line 1580-1585) sama: detik, ≥
- Tier-1 di helper baru (line 4550): `elapsed.TotalMinutes > allowedMinutes` (float menit, >)

Akibat: pada window `elapsed ∈ [allowed*60, allowed*60+1)` detik, `serverTimerExpired` = true
(ExamSummary menampilkan `TimerExpired=true`, banner muncul, JS auto-submit fire) — tetapi Tier-1
`> allowedMinutes` belum tentu true (kalau elapsed.TotalMinutes ≤ allowedMinutes pas).

Kebetulan di kondisi `>=` lawan `>` ini tidak menyebabkan blocked-padahal-harus-allow karena
`isAutoSubmit=true` akan dipakai (Tier-1 skip via `!isAutoSubmit` check). Tetapi akan mengganggu
kalau CR-01 di-fix dengan menghapus reliance pada `isAutoSubmit` client.

**Fix:**
Gunakan satuan dan operator yang sama (preferensi: detik integer, ≥), reuse helper:

```csharp
private bool IsExamTimeUp(AssessmentSession a)
{
    if (!a.StartedAt.HasValue) return false;
    var elapsedSec = (DateTime.UtcNow - a.StartedAt.Value).TotalSeconds;
    var allowedSec = (a.DurationMinutes + (a.ExtraTimeMinutes ?? 0)) * 60.0;
    return elapsedSec >= allowedSec;
}
```
Pakai di GET ExamSummary, awal SubmitExam, dan di Tier-1 (`elapsed_sec >= allowed_sec` untuk Tier-1,
`elapsed_sec >= (allowed_sec + 120)` untuk Tier-2).

---

### WR-03: `WriteSubmitBlockedAuditAsync` swallow tanpa fallback alert

**File:** `Controllers/CMPController.cs:4603-4609`
**Issue:**
Empty-style catch (hanya `_logger.LogWarning`) di audit-log writer. Pattern ini OK untuk *tidak
memblok primary action* (justifikasi Phase 312 T-306-02 valid), TAPI implikasinya: kalau audit log
gagal terus-menerus (mis. _auditLog service rusak), Tier-1 reject tidak terdeteksi sama sekali oleh
forensic flow. D-05 menyebut audit log sebagai bukti "user mencoba bypass" — kalau hilang, bukti
hilang.

Mitigasi minimal sudah ada (warning di logger). Tapi tidak ada metric/counter untuk men-trigger
alarm kalau drop-rate tinggi.

**Fix (low effort):**
Tambah counter via `_logger` dengan structured field `event=audit_drop` agar dashboard log dapat
men-grep:
```csharp
_logger.LogWarning(auditEx,
    "AuditLog SubmitExamBlocked write failed for SessionId={SessionId} event={Event}",
    assessment.Id, "audit_drop_phase313");
```
Atau jika ada `IMetrics` / `Activity` di project (cek Phase 312 precedent), increment counter
`audit.write.failure`. Kalau tidak ada, cukup tambahkan structured key di log untuk filterability.

---

### WR-04: Cast `(int)elapsed.TotalMinutes` truncates → audit log understates elapsed

**File:** `Controllers/CMPController.cs:4591`
**Issue:**
`ElapsedMin={(int)elapsed.TotalMinutes}` melakukan truncasi (bukan rounding). Contoh: kalau user
submit pada elapsed 30 menit 59 detik, log mencatat `ElapsedMin=30`, bukan 31. Karena Tier-1
fire di `> allowedMinutes` (float), bisa jadi `AllowedMin=30 ElapsedMin=30` muncul di audit padahal
sebenarnya elapsed 30.5 menit — kelihatan kontradiktif untuk reviewer audit ("kok di-block padahal
elapsed = allowed?").

**Fix:**
Log dengan presisi detik atau gunakan rounding-up:
```csharp
$"ElapsedSec={(int)elapsed.TotalSeconds} " +
$"AllowedSec={allowedMinutes * 60} " +
```
Atau format ISO-duration: `Elapsed={elapsed:c}`.

---

## Info

### IN-01: Duplicate read of `ViewBag.TimerExpired` di ExamSummary.cshtml

**File:** `Views/CMP/ExamSummary.cshtml:9` + `Views/CMP/ExamSummary.cshtml:172`
**Issue:**
Line 9: `bool timerExpired = ViewBag.TimerExpired as bool? ?? false;`
Line 172: `var timerExpired = @Json.Serialize((bool?)(ViewBag.TimerExpired) ?? false);`
Sama-sama membaca ViewBag dengan defaulting yang berbeda style. Kedua variabel bernama sama tapi
satu C# satu JS — bukan error, tapi redundansi yang mudah salah-konsisten kalau salah satu diubah.
**Fix:** Render JS variable dari Razor variable yang sudah ada:
```cshtml
var timerExpired = @Json.Serialize(timerExpired);
```

### IN-02: Regex `escapeRegex` + `m` flag kombinasi tidak meaningful di hasText

**File:** `tests/e2e/exam-taking.spec.ts:1613` (dan baris fixture lain)
**Issue:**
```ts
new RegExp(`^\\s*${escapeRegex(fixtureTitle)}\\s*`, 'm')
```
Playwright `hasText` regex match terhadap text content row (single-line strip). Anchor `^` + flag `m`
tidak menambah ketelitian dibanding regex tanpa anchor (`escapeRegex(fixtureTitle)` saja). Pattern ini
diadopsi dari Phase 312 WR-03 mitigation, tapi konteks di sana adalah substring-match guard. Untuk
exact-match row, lebih jelas pakai `hasText: fixtureTitle` (string, bukan regex) yang Playwright
treat sebagai substring; atau regex eksplisit `^${escapeRegex(fixtureTitle)}$` (tanpa `m`).
**Fix:** Sederhanakan ke string atau anchor jelas:
```ts
const targetRow = page.locator('tr', { hasText: fixtureTitle }).first();
```

### IN-03: Test FLOW 313 belum punya assertion submit-result, hanya visibility row

**File:** `tests/e2e/exam-taking.spec.ts:1609-1689`
**Issue:**
Setiap test 313.1–313.7 hanya verifikasi `targetRow.toBeVisible()`. Tidak ada flow real klik fixture
→ submit → assert TempData/banner/AuditLog. Komentar di line 1617 mengakui "Plan 03 implementasi
finalisasi flow assertion. Wave 0 placeholder". Itu by-design Wave-0 RED, tapi:
- Test akan PASS hanya karena row ada → false-positive coverage di laporan CI
- Tidak ada `test.fail()` atau `test.fixme()` marker untuk membedakan dari placeholder pass
**Fix:** Tandai test ini sebagai `test.fixme(...)` atau tambah assertion minimal yang sengaja gagal
(`expect(true).toBe(false)` di-wrap `test.fail()`) sampai Wave 1 mengisi flow. Atau dokumentasikan
di README test bahwa green=placeholder, bukan coverage real.

### IN-04: AssessmentType allow-list duplikasi (drift risk)

**File:** `Controllers/CMPController.cs:4535-4540`
**Issue:**
List `Online | PreTest | PostTest` di-hardcode di Tier-1 helper. Kalau ke depan ada AssessmentType
baru (mis. `Practice`), developer harus ingat menambahnya di sini. Pattern Phase 312 punya issue
yang sama dan sudah ditandai sebagai known TechDebt.
**Fix (opsional, low priority):** Pindahkan allow-list ke property/method di
`AssessmentConstants.AssessmentType.IsTimedType(string)`:
```csharp
public static bool IsTimedType(string type) =>
    type == Online || type == PreTest || type == PostTest;
```

### IN-05: Komentar `D-XX` di kode tanpa link ke DECISIONS.md

**File:** `Controllers/CMPController.cs:4524-4559` (dan tempat lain)
**Issue:**
Komentar referensi `D-09`, `D-15`, `D-06`, `D-05`, `D-01` mengasumsikan reviewer punya akses ke
`.planning/phases/313-.../DECISIONS.md`. Saat file phase di-archive (Phase 312 precedent), referensi
tetap di kode tapi context hilang. Bukan bug, tapi maintenance smell.
**Fix (sangat opsional):** Tambah link inline di komentar pertama:
`// Phase 313 .planning/phases/313-block-manual-submit-saat-waktu-habis/DECISIONS.md`
atau cukup paste 1-line essence per decision:
`// D-15: Manual & null AssessmentType di-skip (defense-in-depth, type yang tidak punya timer)`

---

_Reviewed: 2026-05-08_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
