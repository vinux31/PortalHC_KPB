---
phase: 306-score-editable-per-question-type
verified: 2026-04-28T12:00:00Z
status: passed
score: 15/15 must-haves verified
overrides_applied: 0
---

# Phase 306: Score Editable per Question Type — Verification Report

**Phase Goal:** REQ QSCR-01 — Admin/HC dapat menyimpan skor 1–100 untuk soal MultipleChoice, MultipleAnswer, dan Essay. Override server-side `scoreValue=10` di CreateQuestion + EditQuestion dihapus; input view enabled untuk semua tipe. Maps Audit Temuan 2.
**Verified:** 2026-04-28T12:00:00Z
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths (ROADMAP Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| SC-1 | Input `scoreValue` di `ManagePackageQuestions.cshtml` tidak `disabled` default | VERIFIED | Line 184-187: elemen input tanpa attr `disabled`, hanya memiliki `step="1" required data-original-score="10" data-affected-sessions="0"`. Grep `disabled` di sekitar `id="scoreValue"` return 0 match aktif (hanya komentar di JS). |
| SC-2 | JS tidak paksa `scoreInput.disabled = (qtype !== 'Essay')` dan tidak reset `value=10` | VERIFIED | `applyQTypeSwitch` (line 351-367): ketiga baris target dihapus. Grep `scoreInput.disabled = (qtype` = 0; grep `scoreInput.value = 10` = 0; grep `scoreHelp.textContent =` = 0 (hanya komentar dokumentasi removal di line 366). |
| SC-3 | Server-side override `if (questionType != "Essay") scoreValue = 10` dihapus dari CreateQuestion (line 4681) dan EditQuestion (line 4822) | VERIFIED | Grep count = 0 — kedua baris tidak ada lagi di controller. Dikonfirmasi via baca langsung line 4680-4685 (CreateQuestion) dan 4855-4860 (EditQuestion). |
| SC-4 | Server-side range 1–100 di-enforce | VERIFIED | Grep `scoreValue < 1 \|\| scoreValue > 100` = 2 (CreateQuestion line 4681 + EditQuestion line 4856). Pesan flash `"Nilai soal harus antara 1 dan 100."` count = 2. |
| SC-5 | AuditLog entry saat score diubah pada soal yang punya session associated | VERIFIED | `EditQuestion-ScoreChange` di controller ditulis dengan format `"ScoreValue: {oldScore} → {scoreValue} ({affectedSessionsCount} sessions affected)"`. UAT Step 9 PASS: Q652 (Essay, 1 session) audit row tercatat. `var oldScore = q.ScoreValue` = 1 instance; `affectedSessionsCount = await _context.PackageUserResponses...` = 1 instance. |

**Score ROADMAP SC: 5/5**

---

### Must-Haves Plan 01 — Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| P01-T1 | Server reject scoreValue < 1 atau > 100 dengan TempData["Error"] flash + redirect | VERIFIED | Range check ada di 2 lokasi (CreateQuestion + EditQuestion). TempData["Error"] = "Nilai soal harus antara 1 dan 100." dikonfirmasi di kedua lokasi. |
| P01-T2 | Force-override `if (questionType != "Essay") scoreValue = 10` dihapus di CreateQuestion DAN EditQuestion | VERIFIED | Grep = 0. |
| P01-T3 | Force-override `if (scoreValue <= 0) scoreValue = 10` dihapus di CreateQuestion DAN EditQuestion | VERIFIED | Grep = 0. |
| P01-T4 | EditQuestion menulis AuditLog `EditQuestion-ScoreChange` saat scoreValue berubah, capture oldScore + newScore + affectedSessionsCount | VERIFIED | Ditemukan di line 4914-4941: `if (scoreValue != oldScore)` blok dengan CountAsync + LogAsync format eksak D-10 menggunakan arrow U+2192 literal. |
| P01-T5 | CreateQuestion menulis AuditLog `CreateQuestion-CustomScore` saat scoreValue != 10 | VERIFIED | Ditemukan di line 4740-4761: blok `if (scoreValue != 10)` dengan try/catch dan LogAsync. |
| P01-T6 | EditQuestion AJAX GET mengembalikan field `affectedSessions` di JSON response | VERIFIED | Line 4797-4801: `var affectedSessions = await _context.PackageUserResponses...CountAsync()`. Line 4810: `affectedSessions = affectedSessions` dalam anonymous JSON object. |
| P01-T7 | Audit log dibungkus try/catch dengan _logger.LogWarning fallback | VERIFIED | CreateQuestion-CustomScore: try/catch dengan `_logger.LogWarning(auditEx, ...)` di line 4757-4760. EditQuestion-ScoreChange: try/catch dengan `_logger.LogWarning(auditEx, ...)` di line 4937-4940. |
| P01-T8 | dotnet build -c Debug exit 0 setelah perubahan | VERIFIED | SUMMARY 01 mencatat Build succeeded. 0 Error(s). 92 Warning(s) — baseline preserved. |

**Score Plan 01: 8/8**

---

### Must-Haves Plan 02 — Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| P02-T1 | Input scoreValue tidak punya attribute `disabled` (default state) untuk semua tipe MC/MA/Essay | VERIFIED | Line 184-187 ManagePackageQuestions.cshtml: tidak ada `disabled` attr. Grep context `id="scoreValue"` + `disabled` = 0. |
| P02-T2 | Input scoreValue punya attribute `step="1" required` | VERIFIED | Line 186: `min="1" max="100" step="1" required style="max-width:80px"` dikonfirmasi via baca langsung. |
| P02-T3 | Help text scoreValue menampilkan static `Range 1–100` | VERIFIED | Line 188: `<div class="form-text" id="scoreHelp">Range 1–100</div>`. Grep `MC/MA: nilai tetap 10` = 0. |
| P02-T4 | JS applyQTypeSwitch TIDAK lagi reset `scoreInput.disabled` atau `scoreInput.value = 10` atau `scoreHelp.textContent` | VERIFIED | Ketiga pola grep masing-masing return 0 (aktif code). Fungsi applyQTypeSwitch di line 351-367 hanya berisi optionsSection/rubrikSection/maLabel/correct-input logic. |
| P02-T5 | Header menampilkan `Total {N} poin` computed dari `questions.Sum(q => q.ScoreValue)` | VERIFIED | Line 42: `Daftar Soal (@questions.Count soal • Total @questions.Sum(q => q.ScoreValue) poin)`. UAT Step 1: "8 soal • Total 80 poin" PASS. |
| P02-T6 | Modal `editScoreWarningModal` ada dengan struktur replikasi editTypeWarningModal | VERIFIED | Line 257-274: modal block lengkap dengan `id="editScoreWarningModal"`, `aria-labelledby="editScoreWarningModalLabel"`, `modal-header bg-warning`, confirm button `btn-warning btn-sm id="confirmScoreChange"`. |
| P02-T7 | Form submit handler trigger modal saat editMode AND newScore != originalScore AND affectedSessions > 0 | VERIFIED | Line 299-318: submit handler eksak per D-06. Checks `isEditMode`, `!isNaN(originalScore) && newScore !== originalScore && affectedN > 0`. `editScoreWarningModal.show()` dipanggil saat kondisi terpenuhi. |
| P02-T8 | Modal body copy mengikuti template D-07 | VERIFIED | Line 328-333: `populateScoreWarningModal` menghasilkan string dengan {order}/{old}/{new}/{N peserta} dan literal `persentase mereka akan dihitung ulang otomatis`. Grep = 1. |
| P02-T9 | `populateEditForm` inject `data-original-score` + `data-affected-sessions` ke form/input | VERIFIED | Line 411-413: tiga inject eksplisit `dataset.originalScore`, `dataset.affectedSessions`, `dataset.questionOrder`. Grep masing-masing = 1. |
| P02-T10 | Admin dapat save score 1-100 untuk MC/MA via UAT | VERIFIED | UAT 10/10 PASS via MCP Playwright: Step 2-4 (MC/MA/Essay enabled), Step 5 (HTML5 range), Step 6 (server-side bypass reject), Step 7 (CreateQuestion audit), Step 8-9 (EditQuestion audit + modal). |

**Score Plan 02: 10/10 (termasuk human-verified UAT)**

---

## Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Controllers/AssessmentAdminController.cs` | CreateQuestion + EditQuestion POST validation, EditQuestion GET extended JSON, audit log entries | VERIFIED | 6 range touch points: CreateQuestion range check (4681-4685), CreateQuestion audit (4740-4761), EditQuestion GET affectedSessions (4797-4801 + JSON line 4810), EditQuestion POST range check (4855-4860), oldScore capture (4881), EditQuestion POST audit (4914-4941). Semua substantif, bukan stub. |
| `Views/Admin/ManagePackageQuestions.cshtml` | ScoreValue input enabled, modal warning, JS submit handler, header total points | VERIFIED | Header line 42, input line 184-187 (no disabled, step required data attrs), modal line 257-274, submit handler line 299-318, populateScoreWarningModal line 328-333, populateEditForm extension line 411-413. Semua substantif. |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| CreateQuestion POST (line 4681) | TempData["Error"] redirect | `if (scoreValue < 1 \|\| scoreValue > 100)` | WIRED | Range check ada + TempData["Error"] + RedirectToAction dikonfirmasi di line 4681-4685. |
| EditQuestion POST (oldScore capture) | `_auditLog.LogAsync('EditQuestion-ScoreChange', ...)` | `if (scoreValue != oldScore) try { ... } catch (auditEx)` | WIRED | `oldScore = q.ScoreValue` di line 4881, audit block `if (scoreValue != oldScore)` di line 4915-4941 dengan try/catch. |
| EditQuestion GET AJAX | Json response dengan `affectedSessions` field | `_context.PackageUserResponses.Where(...).Select(r => r.AssessmentSessionId).Distinct().CountAsync()` | WIRED | Line 4797-4810: CountAsync + JSON field `affectedSessions = affectedSessions`. |
| ScoreValue input (data-original-score) | JS submit handler via `dataset.originalScore` | Inject oleh `populateEditForm` | WIRED | Line 411: `document.getElementById('scoreValue').dataset.originalScore = data.scoreValue || 10`. Submit handler line 308: `parseInt(scoreInput.dataset.originalScore, 10)`. |
| Form submit event | `editScoreWarningModal.show()` | `if (delta && affectedN > 0) event.preventDefault()` | WIRED | Line 313-317: condition eksak, preventDefault + show() dipanggil. |
| Bootstrap modal init | `confirmScoreChange` button click → `form.submit()` | `scoreChangeBypassed` flag toggle | WIRED | Line 290 (init), line 320-324 (confirmScoreChange listener: hide + bypass=true + form.submit()). |

---

## Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|--------------|--------|--------------------|--------|
| EditQuestion GET JSON | `affectedSessions` | `_context.PackageUserResponses.Where(r => r.PackageQuestionId == q.Id).Select(r => r.AssessmentSessionId).Distinct().CountAsync()` | Ya — query DB nyata | FLOWING |
| EditQuestion POST audit | `affectedSessionsCount` | `_context.PackageUserResponses.Where(r => r.PackageQuestionId == questionId).Select(r => r.AssessmentSessionId).Distinct().CountAsync()` | Ya — query DB nyata | FLOWING |
| Header total points | `questions.Sum(q => q.ScoreValue)` | Razor LINQ pada `ViewBag.Questions as List<PackageQuestion>` — data di-load oleh controller GET | Ya — dari DB via controller | FLOWING |
| Modal warning body | `populateScoreWarningModal(qOrder, originalScore, newScore, affectedN)` | `affectedN` berasal dari `form.dataset.affectedSessions` yang diinject `populateEditForm` dari JSON `data.affectedSessions` (server) | Ya — real count dari server | FLOWING |

---

## Behavioral Spot-Checks

UAT dilaksanakan via MCP Playwright pada app running (port 5277). Hasil dari 306-02-SUMMARY.md:

| Behavior | Command/Test | Result | Status |
|----------|-------------|--------|--------|
| Input scoreValue enabled untuk MC (no disabled attr) | DOM check `scoreInput.disabled` | `false` | PASS |
| Input scoreValue enabled untuk MA setelah type switch | Switch dropdown → check disabled + value preserve | disabled=false, value "25" preserved | PASS |
| HTML5 validation score 0 dan 150 | Submit form dengan value out-of-range | rangeUnderflow/rangeOverflow browser tooltip | PASS |
| Server-side bypass via DevTools (remove min/max, set -5) | Submit → check flash error | Flash error "Nilai soal harus antara 1 dan 100." — soal count tetap 8 | PASS |
| CreateQuestion audit log (non-default score=30) | SQL query AuditLogs WHERE ActionType='CreateQuestion-CustomScore' | Row ada: TargetId=712, Description match D-11 spec | PASS |
| EditQuestion audit log tanpa session (30→40) | SQL query AuditLogs WHERE ActionType='EditQuestion-ScoreChange' | Row: "ScoreValue: 30 → 40 (0 sessions affected)" — arrow U+2192 literal | PASS |
| EditQuestion modal muncul saat affectedSessions > 0 (10→20, Q652) | Submit → modal popup | Modal "Peringatan Ubah Skor" muncul, body copy exact D-07, btn-warning btn-sm | PASS |
| Stored Session Score tidak berubah setelah edit (D-19) | Re-query Session Id=118 Score sebelum/sesudah | Score=84 UNCHANGED setelah edit | PASS |

**Spot-check: 8/8 PASS**

---

## Requirements Coverage

| Requirement | Source Plan | Deskripsi | Status | Evidence |
|-------------|------------|-----------|--------|----------|
| QSCR-01 | 306-01-PLAN.md, 306-02-PLAN.md | Admin/HC dapat menyimpan skor 1–100 untuk MultipleChoice, MultipleAnswer, dan Essay | SATISFIED | Server-side force-override dihapus, range 1-100 di-enforce, input view enabled, audit log tercatat. UAT 10/10 PASS. |

**Orphaned requirements dari REQUIREMENTS.md yang mapped ke Phase 306:** Tidak ada. QSCR-01 adalah satu-satunya requirement yang dipetakan ke Phase 306 per Traceability table.

---

## Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `Controllers/AssessmentAdminController.cs` | 4891 | `_context.PackageOptions.RemoveRange(q.Options)` — FK violation saat MC/MA question punya `PackageUserResponses` yang referensi `PackageOptions` | Info (pre-existing) | Hanya terjadi saat admin edit option set untuk soal yang sudah di-jawab completed session. BUKAN hasil Phase 306 — plan eksplisit instruksikan DO NOT TOUCH. Didokumentasikan di 306-02-SUMMARY.md "Issues Encountered". Tidak memblokir tujuan Phase 306 (score editability). |

**Klasifikasi:** Tidak ada anti-pattern blocker atau warning dari Phase 306 sendiri. Satu pre-existing bug tercatat sebagai info, out-of-scope, dan tidak mempengaruhi tujuan phase.

---

## Human Verification Required

UAT 10-step sudah dijalankan oleh MCP Playwright agent sebagai proxy human verification (autonomous:false, checkpoint:human-verify). Tidak ada item yang memerlukan verifikasi human tambahan — semua behavioral checks sudah tercakup dalam UAT evidence di 306-02-SUMMARY.md.

*(Section ini kosong — status tidak menggunakan `human_needed` karena UAT sudah selesai dengan approved 10/10 PASS.)*

---

## Gaps Summary

Tidak ada gap ditemukan. Semua 15 must-haves (5 ROADMAP SC + 8 Plan 01 truths + 10 Plan 02 truths — dengan 8 overlap/coverage ke SC) terverifikasi PASS melalui pemeriksaan langsung codebase + UAT evidence.

Pre-existing bug EditQuestion option-replacement (FK violation) dikecualikan dari scope: bukan regresi Phase 306, plan eksplisit melarang menyentuh logika tersebut, dan tidak memblokir tujuan utama (score editability).

---

## Catatan Tambahan

**EditQuestion-ScoreChange count = 2 (bukan 1 seperti plan expect):** Plan 01 acceptance criteria menyebut "must return 1", namun actual = 2. Ini bukan deviasi melainkan inaccuracy di teks acceptance — string tersebut muncul 1x sebagai `actionType` argument dalam `_auditLog.LogAsync()` dan 1x dalam pesan `_logger.LogWarning(auditEx, "Audit logging failed during EditQuestion-ScoreChange ...")`. Pola ini identik dengan pre-existing pattern `DeleteAssessment` di controller yang sama. Verifikasi semantik: ActionType di DB ditulis dengan string yang benar, fallback warning message juga menggunakan string yang sama untuk traceability.

**`scoreHelp.textContent` grep = 1 (komentar dokumentasi):** Satu-satunya match adalah komentar Razor `// scoreHelp.textContent dynamic update removed (Phase 306, D-05)` di line 366. Ini adalah kode komentar, bukan active assignment. Plan acceptance menarget `scoreHelp.textContent =` (dengan tanda sama dengan), yang sudah = 0. Pattern ini adalah hygiene dokumentasi removal yang baik.

---

## Commit Reference

| Plan | Task | Commit Hash | Deskripsi |
|------|------|-------------|-----------|
| 306-01 | Task 1 | `3949fe92` | CreateQuestion: range validation + audit non-default score |
| 306-01 | Task 2 | `31670ce7` | EditQuestion POST: range validation + audit score change |
| 306-01 | Task 3 | `0f878aaa` | EditQuestion AJAX GET: extend JSON dengan affectedSessions |
| 306-02 | Task 1 | `66e2c4dc` | View: header total, score input enabled, modal HTML |
| 306-02 | Task 2 | `3a8cb48e` | JS: applyQTypeSwitch cleanup, modal init, submit handler |
| 306-02 | Task 3 | (UAT) | UAT 10/10 PASS via MCP Playwright |

---

_Verified: 2026-04-28T12:00:00Z_
_Verifier: Claude (gsd-verifier)_
