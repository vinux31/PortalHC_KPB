---
phase: 313-block-manual-submit-saat-waktu-habis
verified: 2026-05-08T07:00:00Z
status: human_needed
score: 5/7 must-haves verified (SC#6 dan SC#7 memerlukan UAT manual)
overrides_applied: 0
overrides:
  - must_have: "AuditLog entry rejection alasan `manual_after_timeup` dengan {UserId, SessionId, ElapsedMin, AllowedMin}"
    reason: "D-05 di 313-CONTEXT.md memutuskan ActionType=`SubmitExamBlocked` mengikuti Phase 312 naming convention {Action}Blocked. UserId tercatat di actorUserId field LogAsync. Alasan terdokumentasi sebelum implementasi dimulai."
    accepted_by: "auto-detected dari 313-CONTEXT.md line 62"
    accepted_at: "2026-05-07T00:00:00Z"
human_verification:
  - test: "SC#6 — Verifikasi 3 tipe ber-timer (Online, PreTest, PostTest) dan Manual exclude"
    expected: "Jalankan 313-UAT.md Step 1 (Online ManualBeforeTime), Step 2 (Online Tier-1 BLOCK), Step 5 (PreTest Tier-1 BLOCK), Step 6 (PostTest Tier-1 BLOCK), Step 7 (Manual ExcludeVerify → submit OK, 0 BlockedEntry)"
    why_human: "Memerlukan server running + DB seed 313-timer-fixtures.sql dijalankan + login coachee. Tidak bisa diverifikasi via grep/static analysis."
  - test: "SC#7 — E2E test 6 skenario (manual/auto × before-time/in-grace/after-grace) via Playwright FLOW 313"
    expected: "Setelah DB seed dijalankan, `npx playwright test --grep 'Phase 313'` mengembalikan 7 PASS (bukan SKIP). Assertion body Wave 0 masih placeholder; tester perlu finalisasi assertion 313.2/313.3/313.4/313.7 sesuai UAT result."
    why_human: "Playwright FLOW 313 saat ini adalah Wave 0 RED/SKIP state — test.skip graceful kalau fixture absent. Memerlukan DB seed + server running + assertion body finalisasi post-UAT."
  - test: "CR-01 — isAutoSubmit spoofing risk: tester verifikasi apakah ancaman diterima atau perlu fix"
    expected: "Tester/developer memutuskan apakah CR-01 (DevTools ubah isAutoSubmit=false→true bypass Tier-1) acceptable untuk threat model Phase 313, atau perlu fix server-approved token sebelum close phase."
    why_human: "Code review menemukan isu kritis bahwa Tier-1 bisa di-bypass dengan spoof isAutoSubmit=true via DevTools. Perlu keputusan acceptance dari developer/PM."
  - test: "CR-02 — Redirect loop risk: tester verifikasi via DevTools Network tab"
    expected: "Pada skenario Tier-2 reject (auto-submit setelah grace), fetch retry handler TIDAK melakukan navigate loop StartExam → ExamSummary → SubmitExam → redirect StartExam. Jika loop terjadi, kode di ExamSummary.cshtml baris `if (r.redirected) { window.location.href = r.url; }` perlu dibedakan redirect ke Results vs StartExam."
    why_human: "Memerlukan simulasi skenario Tier-2 reject live (elapsed > grace 2min) untuk mengamati perilaku fetch handler. Tidak bisa diverifikasi via static analysis."
  - test: "AuditLog ditulis benar di DB — verifikasi SQL spot-check"
    expected: "Setelah Step 2 UAT dijalankan, query `SELECT TOP 5 ActionType, Description, TargetId FROM AuditLogs WHERE ActionType = 'SubmitExamBlocked' ORDER BY CreatedAt DESC` mengembalikan 1+ row dengan Description berisi `Type=Online ElapsedMin=... AllowedMin=60 SessionId=...`"
    why_human: "Memerlukan app running + DB aksesibel + AuditLog row ditulis via real HTTP POST."
---

# Phase 313: Block Manual Submit Saat Waktu Habis — Verification Report

**Phase Goal:** Block Manual Submit Saat Waktu Habis — Modify LIFE-03 jadi 2-tier (manual reject tanpa grace, auto reject setelah grace) — REQ TMR-01

**Verified:** 2026-05-08
**Status:** human_needed
**Re-verification:** Tidak — verifikasi awal

---

## Goal Achievement

### Observable Truths (dari ROADMAP Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|---------|
| 1 | Modify `CMPController.SubmitExam()` LIFE-03 block jadi 2-tier branching `isAutoSubmit` | VERIFIED | `EnsureCanSubmitExamAsync` menggantikan inline LIFE-03 block di line 1616-1621; 2-tier logic ada di helper (Tier-1 line 4550, Tier-2 line 4558) |
| 2 | Tier 1: `!isAutoSubmit && elapsed > allowed` → reject manual + TempData D-01 + redirect StartExam | VERIFIED | Line 4550-4555: kondisi `!isAutoSubmit && elapsed.TotalMinutes > allowedMinutes` → `WriteSubmitBlockedAuditAsync` + TempData D-01 verbatim + `RedirectToAction("StartExam")` |
| 3 | Tier 2: `elapsed > allowed + 2min grace` → reject auto-submit telat (existing LIFE-03 preserved) | VERIFIED | Line 4557-4561: `elapsed.TotalMinutes > graceLimitMinutes` (allowedMinutes+2) → TempData "Pengiriman jawaban tidak dapat diproses" + redirect. D-06: Tier-2 tidak tulis AuditLog. |
| 4 | Frontend `StartExam.cshtml`: countdown=0 disable tombol Submit manual; auto-submit handler tetap aktif | VERIFIED (parsial) | StartExam.cshtml: modal C-03 (info-only spinner, tanpa OK button). ExamSummary.cshtml: `id="manualSubmitDisabledBtn"` disabled + spinner. Catatan: tombol disable ada di ExamSummary (saat sudah di review page), bukan StartExam langsung — per C-02 intentional decision Pitfall 2. |
| 5 | AuditLog entry rejection alasan `manual_after_timeup` dengan `{UserId, SessionId, ElapsedMin, AllowedMin}` | VERIFIED (override) | ActionType=`SubmitExamBlocked` (D-05 decision mengganti `manual_after_timeup` per Phase 312 naming convention). UserId via `actorUserId: blockUser?.Id`. Description: `Type={X} ElapsedMin={Y} AllowedMin={Z} SessionId={id}`. |
| 6 | Verifikasi 3 tipe ber-timer: Online, PreTest, PostTest (Manual exclude) | HUMAN NEEDED | Guard di EnsureCanSubmitExamAsync line 4535-4537 menggunakan `AssessmentConstants.AssessmentType.Online/PreTest/PostTest` constants. SQL seed ada 5 fixture terkait 3 tipe. Verifikasi behavior hidup memerlukan UAT (313-UAT.md Step 2, 5, 6, 7). |
| 7 | E2E test 6 skenario manual/auto × before-time/at-time/in-grace/after-grace | HUMAN NEEDED | FLOW 313 di exam-taking.spec.ts ada 7 test (313.1..313.7) — listed via Playwright. Saat ini Wave 0 SKIP state (assertion body placeholder `targetRow.toBeVisible()`). Memerlukan DB seed + assertion finalisasi post-UAT. |

**Score:** 5/7 truths verified (SC#6 dan SC#7 memerlukan human verification)

---

### Deferred Items

Tidak ada item yang di-defer ke fase berikutnya. SC#6 dan SC#7 memerlukan UAT live, bukan deferred ke milestone berikutnya.

---

### Required Artifacts

| Artifact | Expected | Status | Detail |
|----------|----------|--------|--------|
| `Controllers/CMPController.cs` | `EnsureCanSubmitExamAsync` + `WriteSubmitBlockedAuditAsync` helpers | VERIFIED | 2 helper ada di line 4529+ dan 4573+. Invocation di line 1620-1621. |
| `Views/CMP/ExamSummary.cshtml` | 3-branch button conditional + `@section Scripts` retry handler | VERIFIED | `id="manualSubmitDisabledBtn"` present, `@section Scripts` present, `delays = [1000, 2000, 4000]`, `fetch(form.action,` present |
| `Views/CMP/StartExam.cshtml` | Modal info-only (spinner, no OK button) + fire submit paralel | VERIFIED | `Phase 313 C-03` anchor present, `Mengirim...` spinner present, `timeUpOkBtn` count=0, `}, 10000)` count=0 |
| `tests/e2e/exam-taking.spec.ts` | FLOW 313 describe block (7 tests, SKIP state) | VERIFIED | `test.describe('Exam Taking - Phase 313 Block Manual Submit')` present, 7 test titles 313.1..313.7, 10 `test.skip` occurrences, 7 fixture title references |
| `.planning/seeds/313-timer-fixtures.sql` | 7 fixture SQL seed, idempotent | VERIFIED | File exists, 18 occurrences `Phase 313 Timer Fixture`, idempotent `DELETE FROM AssessmentSessions WHERE Title LIKE 'Phase 313 Timer Fixture%'`, `THROW 50001` guard present |
| `.planning/phases/.../313-UAT.md` | Manual UAT 7-step Bahasa Indonesia + AuditLog SQL spot-check | VERIFIED | File exists, 7 step headers, 17 occurrences `SubmitExamBlocked`, D-01 message present, `Final Sign-Off` section present |

---

### Key Link Verification

| From | To | Via | Status | Detail |
|------|----|-----|--------|--------|
| `CMPController.SubmitExam` (~line 1620) | `EnsureCanSubmitExamAsync` helper | `var timerBlockResult = await EnsureCanSubmitExamAsync(assessment, isAutoSubmit);` | WIRED | Pattern exact match confirmed, count=1 |
| `EnsureCanSubmitExamAsync` Tier-1 branch | `WriteSubmitBlockedAuditAsync` | `await WriteSubmitBlockedAuditAsync(assessment, elapsed, allowedMinutes);` | WIRED | Pattern confirmed, count=2 (declaration+call) |
| `WriteSubmitBlockedAuditAsync` | `_auditLog.LogAsync` | `actionType: "SubmitExamBlocked"` | WIRED | Pattern confirmed |
| `ExamSummary.cshtml @if (timerExpired)` | `ViewBag.TimerExpired` | Razor conditional `@if (timerExpired)` | WIRED | Pattern confirmed, `bool timerExpired = ViewBag.TimerExpired as bool? ?? false` (line ~9) |
| `ExamSummary.cshtml @section Scripts` retry handler | POST `/CMP/SubmitExam` | `fetch(form.action, { method: 'POST', ...})` | WIRED | `form.action` DOM property (WR-02 mitigated, no hardcoded URL) |
| `StartExam.cshtml updateTimer()` countdown=0 | `examForm.submit()` | Modal show + `document.getElementById('examForm').submit()` | WIRED | `Phase 313 C-03` comment confirmed, `submitted=true` set before modal |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|-------------------|--------|
| `EnsureCanSubmitExamAsync` | `assessment.StartedAt`, `assessment.DurationMinutes` | `assessment` object dari EF Core query di SubmitExam (line ~1560 area) | Ya — DB query upstream | FLOWING |
| `EnsureCanSubmitExamAsync` | `elapsed` | `DateTime.UtcNow - assessment.StartedAt.Value` | Ya — server UTC clock | FLOWING |
| `ExamSummary.cshtml` disabled button | `timerExpired` | `ViewBag.TimerExpired` set by ExamSummary GET action server-side | Ya — server-computed dari elapsed/allowed | FLOWING |
| FLOW 313 tests | fixture row | SQL seed `.planning/seeds/313-timer-fixtures.sql` (harus dijalankan manual) | Belum (Wave 0) — fixture absent = 7 SKIP | STATIC (wave 0 intended) |

---

### Behavioral Spot-Checks

Step 7b SKIPPED untuk item yang memerlukan running server. Verifikasi code-level yang dapat dilakukan:

| Behavior | Check | Result | Status |
|----------|-------|--------|--------|
| Helper exists dan dikompile | `dotnet build` exit 0 (per 313-02-SUMMARY.md) | 0 errors, 92 warnings (baseline preserved) | PASS (per Summary) |
| 7 test Playwright listed | `npx playwright test --grep "Phase 313" --list` (per 313-01-SUMMARY.md) | 7 tests listed | PASS (per Summary) |
| TypeScript compile | `npx tsc --noEmit` (per 313-01-SUMMARY.md) | exit 0 | PASS (per Summary) |
| EnsureCanSubmitExamAsync count | grep count | 2 (declaration + 1 call) | PASS |
| WriteSubmitBlockedAuditAsync count | grep count | 2 (declaration + 1 call dari Tier-1) | PASS |
| `timeUpOkBtn` removed | grep count | 0 | PASS |
| `}, 10000)` removed | grep count | 0 | PASS |
| Hardcoded URL absent | grep `'/CMP/SubmitExam'` | 0 | PASS |
| Anti-pattern C-01 absent | grep `assessment.Type ==` | 0 | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Deskripsi | Status | Evidence |
|-------------|------------|-----------|--------|---------|
| TMR-01 | Plan 01, 02, 03 | Server menolak submit manual worker pada assessment ber-timer saat `elapsed > DurationMinutes + ExtraTimeMinutes`; hanya jalur auto-submit sah setelah waktu habis dengan grace 2 menit | PARTIAL — kode terpasang, UAT pending | Backend (Plan 02) + Frontend (Plan 03) implemented. Butuh UAT untuk konfirmasi behavioral correctness. |

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `Controllers/CMPController.cs` | 4550 | `elapsed.TotalMinutes > allowedMinutes` (float minutes, `>`) vs `ExamSummary` GET yang pakai seconds `>=` (WR-02 code review) | Warning | Inkonsistensi unit detik vs menit antara ExamSummary GET dan EnsureCanSubmitExamAsync. Pada window `elapsed ∈ [allowed*60, allowed*60+1)` detik, timerExpired=true di UI tapi Tier-1 belum tentu fire. Tidak blocker karena `isAutoSubmit=true` akan dipakai di kondisi tersebut. Menjadi masalah serius kalau CR-01 difix. |
| `Views/CMP/ExamSummary.cshtml` | ~187 | `if (r.redirected) { window.location.href = r.url; return; }` — tidak membedakan redirect ke Results vs redirect ke StartExam (CR-02 code review) | Blocker potensial | Tier-2 reject (elapsed > grace) menghasilkan redirect ke StartExam. Fetch follow → JS navigate ke StartExam → StartExam `EXAM_EXPIRED` init block fire submit lagi → loop potensial. Counter `maxAttempts=3` tidak melindungi karena reset tiap page-load. |
| `tests/e2e/exam-taking.spec.ts` | 1609-1689 | Assertion body Wave 0 placeholder (`await expect(targetRow).toBeVisible()`) — hanya cek fixture visible, tidak cek redirect/banner/AuditLog | Warning | False-positive coverage: test PASS hanya karena row visible, bukan karena behavior benar. IN-03 dari code review. Perlu finalisasi assertion post-UAT. |
| `Controllers/CMPController.cs` | 4591 | `(int)elapsed.TotalMinutes` truncasi → audit log bisa menulis `ElapsedMin=30` padahal elapsed 30m59s dan `allowedMinutes=30` (WR-04) | Info | Audit log bisa tampak kontradiktif untuk reviewer. Tidak mempengaruhi correctness guard. |

---

### Critical Concerns dari Code Review (CR-01 dan CR-02)

#### CR-01: `isAutoSubmit` Spoofable — Tier-1 Dapat Di-bypass (CRITICAL)

**File:** `Controllers/CMPController.cs:1556` + `Views/CMP/ExamSummary.cshtml:119`

Parameter `bool isAutoSubmit` di-bind dari hidden field client `name="isAutoSubmit"`. Attacker yang sudah lewat waktu cukup buka DevTools, ubah hidden field `isAutoSubmit` dari `false` ke `true`, klik submit. Kondisi `!isAutoSubmit` menjadi `false` → Tier-1 tidak fire, falls through ke Tier-2 yang baru block setelah `allowedMinutes + 2` (2 menit "free"). **Ini berpotensi meniadakan tujuan TMR-01.**

Kode review merekomendasikan `TryConsumeAutoSubmitToken` — one-shot token di `TempData["AutoSubmitToken_{sessionId}"]` di-set saat `ExamSummary` GET render dengan `TimerExpired=true`, lalu divalidasi di `SubmitExam` POST.

**Status:** Belum difix. Perlu keputusan developer apakah diterima (dengan dokumentasi eksplisit di decisions bahwa threat model menganggap user ujian tidak pakai DevTools) atau perlu perbaikan sebelum phase ditutup.

#### CR-02: Fetch Retry Handler Loop — Tier-2 Redirect Dianggap Sukses (CRITICAL)

**File:** `Views/CMP/ExamSummary.cshtml:187`

Handler `attemptSubmit()` memperlakukan semua `r.redirected === true` sebagai happy path (`window.location.href = r.url`). Tier-2 reject mengembalikan redirect ke StartExam → JS navigate → StartExam `EXAM_EXPIRED` block fire submit lagi → loop potensial tanpa batas.

Fix: Deteksi URL redirect. Jika `/CMP/StartExam` → `showRetryFailBanner()` dan stop. Jika `/CMP/Results` → sukses navigate.

**Status:** Belum difix. Memerlukan UAT skenario Tier-2 live untuk konfirmasi apakah loop terjadi dalam kondisi aktual.

---

### Human Verification Required

#### 1. SC#6 — Verifikasi 3 Tipe Timer (Online, PreTest, PostTest) + Manual Exclude

**Test:** Jalankan 313-UAT.md Step 2 (Online Tier-1 BLOCK), Step 5 (PreTest Tier-1 BLOCK), Step 6 (PostTest Tier-1 BLOCK), Step 7 (Manual ExcludeVerify → submit OK)

**Expected:**
- Step 2: Redirect ke StartExam, TempData banner D-01 "Waktu ujian Anda sudah habis. Sistem akan otomatis mengirim jawaban Anda dalam beberapa detik...", AuditLog `SubmitExamBlocked` row dengan `Type=Online`
- Step 5: Sama tapi `Type=PreTest` di Description
- Step 6: Sama tapi `Type=PostTest` di Description
- Step 7: Submit OK → redirect Results, `SELECT COUNT(*) WHERE ActionType='SubmitExamBlocked' AND TargetId={ManualFixtureId}` = 0

**Mengapa human:** Memerlukan server running + DB seed `.planning/seeds/313-timer-fixtures.sql` dijalankan + login coachee `rino.prasetyo@pertamina.com / 123456`

#### 2. SC#7 — E2E Test FLOW 313 Playwright (7 Skenario)

**Test:** Setelah DB seed dijalankan, jalankan `cd tests && npx playwright test --grep "Phase 313"` dan verifikasi 7 PASS (bukan SKIP). Kemudian finalisasi assertion body (313.2: assert redirect + `.alert-danger` D-01 copy; 313.7: NEGATIVE AuditLog check)

**Expected:** 7 test GREEN (bukan SKIP). Assertion bodies terfinalisi untuk 313.2, 313.3, 313.4, 313.7.

**Mengapa human:** Playwright Wave 0 saat ini hanya `expect(targetRow).toBeVisible()` — placeholder. Butuh assertion body finalisasi berdasarkan UAT result.

#### 3. CR-01 Risk Acceptance — Keputusan isAutoSubmit Spoofing

**Test:** Developer/PM memutuskan apakah CR-01 (isAutoSubmit spoofable via DevTools) diterima atau perlu difix sebelum phase close.

**Expected:** Dokumentasi eksplisit di 313-CONTEXT.md decisions bahwa threat model menganggap user ujian tidak akan pakai DevTools (acceptance), ATAU tiket backlog untuk perbaikan `TryConsumeAutoSubmitToken`.

**Mengapa human:** Keputusan threat model dan risk acceptance harus dari developer/PM, bukan verifier.

#### 4. CR-02 Redirect Loop — Verifikasi Live

**Test:** Gunakan fixture "Phase 313 Timer Fixture Online AutoAfterGrace" (elapsed > grace 2min). Biarkan auto-submit fire, amati DevTools Network tab. Verifikasi apakah loop terjadi.

**Expected:** Tidak ada loop. Jika loop terjadi, fix `attemptSubmit()` untuk bedakan redirect URL.

**Mengapa human:** Memerlukan skenario live dengan waktu benar-melewati-grace untuk trigger Tier-2 path dan amati perilaku fetch handler.

#### 5. AuditLog SQL Spot-Check

**Test:** Setelah UAT Step 2 dijalankan, jalankan di SSMS: `SELECT TOP 5 ActionType, Description, TargetId, TargetType, CreatedAt FROM AuditLogs WHERE ActionType = 'SubmitExamBlocked' ORDER BY CreatedAt DESC`

**Expected:** 1+ row dengan `Description` berisi `HC/User role manual submit blocked after timeup. Type=Online ElapsedMin=... AllowedMin=60 SessionId=...`, `TargetType = 'AssessmentSession'`

**Mengapa human:** Memerlukan DB aksesibel + AuditLog row ditulis via real HTTP POST.

---

### Gaps Summary

Tidak ada gap **kode** yang menyebabkan tujuan fase gagal secara statik. Semua Success Criteria 1-5 terverifikasi di level kode:

- SC#1: Modify LIFE-03 → 2-tier TERPASANG
- SC#2: Tier-1 manual reject TERPASANG
- SC#3: Tier-2 auto reject (existing preserved) TERPASANG
- SC#4: Frontend button disabled + modal info-only TERPASANG
- SC#5: AuditLog `SubmitExamBlocked` entry (dengan override pada nama alasan `manual_after_timeup` → `SubmitExamBlocked`) TERPASANG

SC#6 dan SC#7 secara definitif memerlukan server running + DB untuk diverifikasi (tidak bisa di-verify statis).

**Dua isu kritis dari Code Review (CR-01 dan CR-02) BELUM difix dan memerlukan keputusan developer** sebelum fase dapat dinyatakan fully closed:

1. **CR-01** — `isAutoSubmit` spoofable: Tier-1 bisa di-bypass dengan DevTools. Berpotensi meniadakan TMR-01. Butuh keputusan acceptance atau fix.
2. **CR-02** — Redirect loop: Fetch retry handler memperlakukan semua redirect (termasuk Tier-2 reject ke StartExam) sebagai sukses, berpotensi menyebabkan loop POST. Butuh UAT live untuk konfirmasi + kemungkinan fix.

---

_Verified: 2026-05-08_
_Verifier: Claude (gsd-verifier)_
