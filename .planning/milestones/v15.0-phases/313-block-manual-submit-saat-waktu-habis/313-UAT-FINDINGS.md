---
phase: 313-block-manual-submit-saat-waktu-habis
uat_started: 2026-05-08T03:09:00Z
uat_updated: 2026-05-08T03:30:00Z
uat_status: f02_resolved_f01_open
findings_count: 2
blocking_count: 1
resolved_count: 1
positive_observations: 8
---

> **Update 2026-05-08T03:30:** F-313-UAT-02 RESOLVED via commit `2759f748` — `[FromForm(Name="answers")]` prefix + nullable default. Tier-1 reject end-to-end verified: AuditLog `SubmitExamBlocked` row written (Type=Online ElapsedSec=1866 AllowedSec=1800, ActorName "29007720 - Rino"), token-based auto-submit lolos ke Results 200. F-313-UAT-01 (seed package linkage) masih open — UAT 7-step via fixture tetap blocked, harus pakai pivot session-hijack atau extend seed.

# Phase 313 — UAT Findings (Live Browser Verification)

**Tester:** Claude (automated via Playwright MCP)
**Login:** rino.prasetyo@pertamina.com (Coachee)
**Server:** http://localhost:5277 (running)
**Seed:** `.planning/seeds/313-timer-fixtures.sql` executed → 7 fixture id 150-156 inserted
**Snapshot:** `C:\Temp\HcPortalDB_Dev.20260508-pre313.bak`

## Summary

UAT 7-step **tidak bisa diselesaikan via UI** karena 2 blocker:
1. **F-313-UAT-01** — fixture seed kekurangan `AssessmentPackages` linkage
2. **F-313-UAT-02** — POST `/CMP/SubmitExam` kembalikan **HTTP 500** saat auto-submit dari ExamSummary timer-expired tanpa MC answers (regression terkait dictionary model binding)

Walaupun begitu, **6 observasi positif** Plan 03 frontend behavior berhasil diverifikasi via session 72 hijack pivot.

## Findings

### F-313-UAT-01 — BLOCKER: Seed Fixtures Tidak Punya `AssessmentPackages` Linkage

**Severity:** BLOCKING (UAT 7-step tidak bisa dijalankan via UI)

**Reproduce:**
1. Login coachee rino.prasetyo@pertamina.com
2. Navigate `/CMP/Assessment` → 7 fixture muncul (Title "Phase 313 Timer Fixture …")
3. Klik "Resume" pada fixture id 150 (`Phase 313 Timer Fixture Online ManualBeforeTime`)
4. Browser navigasi ke `/CMP/StartExam/150`

**Expected:** Masuk halaman exam dengan timer countdown + 1 question pertama tampil (per UAT Step 1).

**Actual:** Server redirect kembali ke `/CMP/Assessment` dengan alert merah:
> **Error:** Sesi ujian ini tidak memiliki paket soal. Hubungi Admin atau HC.

**Root Cause:**

Controller `CMPController.StartExam` line 906-912 query packages via Title+Category+Schedule.Date join:
```csharp
var siblingSessionIds = await _context.AssessmentSessions
    .Where(s => s.Title == assessment.Title && s.Category == assessment.Category && s.Schedule.Date == assessment.Schedule.Date)
    .Select(s => s.Id).ToListAsync();
var packages = await _context.AssessmentPackages
    .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId)).ToListAsync();
```

Fixture sessions 150-156 punya Title unik ("Phase 313 Timer Fixture …") dan Category "Test Phase 313" yang **tidak ada di session lain manapun** → `packages.Any() == false` → fall ke else branch line 1112-1116 → `TempData["Error"] = "Sesi ujian ini tidak memiliki paket soal …"` + redirect.

Plan 01 seed (`313-timer-fixtures.sql`) hanya INSERT row di tabel `AssessmentSessions`. Tidak ada INSERT ke `AssessmentPackages`, `PackageQuestions`, `PackageOptions` — fixture lengkap exam butuh 4 entitas.

**Mengapa Tidak Tertangkap di Plan 01:**
- Plan 01 verifikasi cuma `npx tsc --noEmit` (TypeScript compile) + `npx playwright test --list` (test enumeration)
- Playwright FLOW 313 assertion bodies hanya `targetRow.toBeVisible()` (verify fixture muncul di list page) — tidak actually click "Resume" / navigate ke StartExam
- Plan 01 executor auto-fix Rule 1+Rule 2 menangani schema NOT NULL columns (AccessToken, BannerColor, Schedule type) tapi miss functional FK ke entity lain

**Impact:**
- UAT Step 1-7 (semua 7 step) tidak bisa dijalankan via UI — semua butuh masuk halaman exam
- SC#6 (3 timer types verification) BLOCKED
- SC#7 (Playwright FLOW 313 GREEN state) BLOCKED — tests masih hanya verify list visibility, tidak verify actual exam flow
- Tier-1 reject behavior (CR-01 fix verification) tidak bisa diverifikasi end-to-end via fixture

**Mitigation Options:**

| Option | Effort | Pros | Cons |
|--------|--------|------|------|
| **A. Extend seed dengan Package + Question + Option INSERT** | Medium (~50 SQL lines + clone existing question content) | Fixture self-contained, identity preserved | Plan 01 supplement work; data clone fragile |
| **B. Pivot UAT: hijack existing session via UPDATE** | Very Low (single UPDATE) | Cepat, real questions | Tidak test "fixture flow"; modifies real data |
| **C. Server-only test via POST /CMP/SubmitExam** | Low (CSRF token quirk) | Verify Tier-1 logic langsung | Skip UI verification — CR-01/CR-02 not full-verified |

**Pivot taken (per user instruction):** Option **B** — hijack session 72 (existing InProgress with 3 questions) via SQL UPDATE: Title="Phase 313 Live Test ManualAfterGrace v2", AssessmentType="Online", StartedAt=NOW-31min, ExamWindowCloseDate future. Snapshot revert akan kembalikan ke aslinya.

---

### F-313-UAT-02 — BLOCKER + REGRESSION: HTTP 500 di POST `/CMP/SubmitExam` saat Auto-Submit Timer-Expired Tanpa MC Answers

**Severity:** BLOCKING (Tier-1 reject behavior + grading flow tidak bisa diverifikasi end-to-end)

**Reproduce:**
1. Hijack session 72: AssessmentType=Online, StartedAt=NOW-31min, Duration=30min, 3 MC questions (existing dari Legacy Exam), 0 answers saved
2. Login rino, navigate `/CMP/StartExam/72`
3. Server auto-redirect ke `/CMP/ExamSummary/72` (timer expired di server-side)
4. ExamSummary view render dengan `timerExpired=true`, hidden field `isAutoSubmit=true`, hidden field `autoSubmitToken=239a77b8a4a244c9a6374fdeea0eb399` (server-issued one-shot per CR-01 fix)
5. JavaScript retry handler (Plan 03 D-10) auto-fire POST ke `/CMP/SubmitExam` setelah page load

**Expected:** POST 302 redirect (Tier-1 reject ke StartExam dengan banner D-01) ATAU 200 success grading (kalau token valid → Tier-1 skip).

**Actual:** **HTTP 500 Server Error** di setiap retry attempt (3x: 1s, 2s, 4s backoff). Banner permanent muncul: "Submit gagal karena masalah jaringan. Hubungi admin. Jawaban Anda tersimpan di tab ini, jangan tutup browser." (D-11).

**Server stack trace (capture via inline fetch evaluate):**
```
System.FormatException: The input string 'id' was not in a correct format.
   at System.Number.ThrowFormatException[TChar](ReadOnlySpan`1 value)
   at System.Int32.Parse(String s, NumberStyles style, IFormatProvider provider)
   at System.ComponentModel.Int32Converter.FromString(String value, NumberFormatInfo formatInfo)
   at System.ComponentModel.BaseNumberConverter.ConvertFrom(...)
   at Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingHelper.ConvertTo[T](Object value, CultureInfo culture)
   at Microsoft.AspNetCore.Mvc.ModelBinding.Binders.DictionaryModelBinder`2.BindModelAsync(ModelBindingContext bindingContext)
```

**Root Cause:**

Controller signature line 1569:
```csharp
public async Task<IActionResult> SubmitExam(int id, Dictionary<int, int> answers, bool isAutoSubmit = false, string? autoSubmitToken = null)
```

Form `<form asp-action="SubmitExam" method="post">` di `Views/CMP/ExamSummary.cshtml` line 120 mengirim hidden fields:
- `id=72`
- `isAutoSubmit=true`
- `autoSubmitToken=239a77b8...`
- `__RequestVerificationToken=...`
- (No `answers[N]=M` fields karena 0 questions answered)

ASP.NET Core `DictionaryModelBinder<int,int>` untuk parameter `answers`:
- Saat **tidak ada** form field bernama `answers[N]=M`, binder **fallback ke greedy mode** — coba parse semua top-level form fields sebagai dictionary entries dengan field name = key, field value = value.
- Field `id` mencoba di-convert ke `int` (Dictionary key type) → `Int32Converter.FromString("id")` → `FormatException`

**Mengapa Tertangkap Sekarang Tapi Tidak Sebelumnya:**

Phase 313 perubahan auto-submit flow membuat skenario "submit dengan zero answers" jadi mainstream — sebelumnya Phase 272 incomplete-block guard mencegah submit zero-answer kecuali timer expired. Phase 313 menambah Tier-1 (manual reject after time) + memastikan auto-submit fire di ExamSummary saat `timerExpired=true` regardless of answer count. Empty answers Dict + non-empty form fields → binder fallback fire.

**Phase 313 Code yang Mendorong Skenario Ini:**
- Plan 03 ExamSummary.cshtml: form auto-submit fire pada timer-expired regardless answers (D-10 retry handler)
- Plan 02 CMPController.SubmitExam: Tier-1 helper baru di-invoke; tidak menyentuh `answers` parameter binding

**Impact:**
- UAT Step 2 (Tier-1 reject manual after-time Online) — TIDAK bisa diverifikasi via UI submit
- UAT Step 3-7 — DEPENDENT on submit endpoint work
- AuditLog `SubmitExamBlocked` — TIDAK ditulis (server crash sebelum reach helper)
- 500 errors crash retry handler → user lihat banner permanent walaupun ini bukan "network problem" actual — banner copy misleading
- Auto-submit reliability di production — broken untuk worker yang belum jawab apapun saat timer habis

**Mitigation Options:**

| Option | Effort | Approach |
|--------|--------|----------|
| **A. Defensive null/optional binding** | Very Low | Change signature: `Dictionary<int, int>? answers = null` + handle null in code |
| **B. Sentinel field di form** | Low | Tambah `<input type="hidden" name="answers[-1]" value="0" />` ke ExamSummary form (tapi controller harus filter -1) |
| **C. Explicit FromForm prefix** | Low | `[FromForm(Name="answers")] Dictionary<int,int> answers` — force prefix-only binding |
| **D. Custom DictionaryModelBinder** | High | Override fallback behavior (overengineering) |

**Rekomendasi:** Option **C** (paling clean) atau Option **A** (paling defensive). Treat sebagai gap closure phase 313.1 atau hot-fix sebelum verify-work.

**Pre-existing vs Phase 313 Regression:**

Bug **`Dictionary<int,int>` greedy binder fallback** kemungkinan pre-existing di .NET — tapi **Phase 313 expose-nya** dengan auto-submit zero-answer flow. Sebelum Phase 313, auto-submit zero-answer hanya fire setelah timer expired DAN user pernah load ExamSummary — kombinasi rare. Phase 313 D-10 retry handler + force-fire saat timerExpired make this commonplace.

---

## Positive Observations (Plan 03 Frontend — VERIFIED via Session 72 Hijack)

| ID | Observation | Status |
|----|-------------|--------|
| O-01 | StartExam server-side redirect ke ExamSummary saat timer expired (`elapsed > duration`) | ✅ PASS |
| O-02 | ExamSummary form hidden field `autoSubmitToken` di-render server-side dengan value GUID-like (CR-01 fix wired) | ✅ PASS |
| O-03 | Submit button label & state: `disabled` + text "Waktu Habis - Submit Otomatis Berjalan..." (D-03) | ✅ PASS |
| O-04 | Banner informational visible: "Waktu ujian sudah habis. Submit otomatis sudah/sedang berjalan." (D-13) | ✅ PASS |
| O-05 | JavaScript retry handler 3x dengan backoff 1s/2s/4s (D-10) — verify via console log "[Phase 313] Submit attempt 1/2/3 failed" | ✅ PASS |
| O-06 | Banner permanent setelah retry exhausted: "Submit gagal karena masalah jaringan. Hubungi admin. Jawaban Anda tersimpan di tab ini, jangan tutup browser." (D-11) | ✅ PASS |

CR-02 redirect-loop fix **TIDAK bisa diverifikasi** karena server 500 sebelum redirect path tercapai — needs F-313-UAT-02 fix dulu.

## Status per UAT Step

| Step | Skenario | Status | Catatan |
|------|----------|--------|---------|
| 1 | Manual + before-time Online → Submit OK | ❌ BLOCKED (F-313-UAT-01) | Fixture tidak bisa enter exam |
| 2 | Manual + after-time Online → Tier-1 BLOCK | ❌ BLOCKED (F-313-UAT-02) | Submit endpoint 500 error |
| 3 | Auto + after-time Online → Submit OK | ❌ BLOCKED (F-313-UAT-02) | Submit endpoint 500 error |
| 4 | Auto + after-grace Online → Tier-2 BLOCK | ❌ BLOCKED (F-313-UAT-02) | Submit endpoint 500 error |
| 5 | Manual + after-time PreTest → Tier-1 BLOCK | ⏸ DEPENDENT | |
| 6 | Manual + after-time PostTest → Tier-1 BLOCK | ⏸ DEPENDENT | |
| 7 | Manual + after-time + Manual type → Submit OK | ⏸ DEPENDENT | |

## Next Action

Tester / orchestrator memutuskan:
1. **Fix F-313-UAT-02 (recommended priority)** — ubah controller signature `Dictionary<int, int>? answers` di `Controllers/CMPController.cs:1569` (defensive option A) atau `[FromForm(Name="answers")]` (option C). Treat sebagai hot-fix kritik sebelum close phase.
2. **Fix F-313-UAT-01** — extend seed SQL dengan AssessmentPackages + PackageQuestions + PackageOptions clone untuk fixture self-containment, ATAU dokumentasikan limitasi seed di `313-UAT.md` (UAT operator manual create assessment via admin UI).
3. **Re-run UAT 7-step** setelah kedua fix landed.

Snapshot DB (`C:\Temp\HcPortalDB_Dev.20260508-pre313.bak`) tetap dipertahankan sampai keputusan diambil. Restore akan revert seed fixtures + session 72 hijack.
