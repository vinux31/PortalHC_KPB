---
phase: 407
slug: worker-self-service-gating-tier-feedback-riwayat-pekerja
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-22
---

# Phase 407 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| view (Razor) ← VM (server) | `Results.cshtml` mempercaya nilai `RetakeMode`/`CanRetake`/eligibility dari `AssessmentResultsViewModel`; nilai dihitung server (`CMPController.Results`). View hanya merender — keputusan leak-safety = server. | `RetakeReviewMode` enum + eligibility flags |
| browser → `CMP/RetakeExam` (POST) | Worker (untrusted) memicu mutasi destruktif (arsip + reset sesi) atas `id` route int. | `id` (int), antiforgery token |
| client countdown JS → server | Countdown enable/disable tombol adalah UX saja; server re-cek `CanRetakeAsync` saat POST. | tidak ada data melintas — UX display only |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-407-idor | Elevation / Access Control | `CMPController.RetakeExam` ownership guard | mitigate | `if (assessment.UserId != user.Id) return Forbid();` tepat sebelum `CanRetakeAsync` + `ExecuteAsync` — mutasi tak terjadi bila bukan pemilik. Diuji `RetakeExam_NonOwner_ReturnsForbid`. | closed |
| T-407-csrf | Tampering / CSRF | `RetakeExam` POST + modal form | mitigate | `[ValidateAntiForgeryToken]` di action; `@Html.AntiForgeryToken()` di form modal `#retakeConfirmModal`. Diverifikasi Playwright (assert `input[name=__RequestVerificationToken]` count 1). | closed |
| T-407-bypass | Tampering / Logic Bypass | cooldown/cap bypass via DevTools | mitigate | Server re-cek `await _retakeService.CanRetakeAsync(id)` SEBELUM `ExecuteAsync`; countdown JS non-authoritative (UX only). Diuji `RetakeExam_NotEligible_RedirectsToResultsWithError`. | closed |
| T-407-leak | Information Disclosure | `Results.cshtml` `ShowWrongFlagsOnly` branch + `_RiwayatPekerja.cshtml` | mitigate | Branch `ShowWrongFlagsOnly` TIDAK merender `list-group-item-success`, `(Jawaban Benar)`, atau `CorrectAnswer`. `ResolveReviewMode` pakai `isPassed != true && attemptsRemaining` (pending diperlakukan sama dengan failed — A1 lock). Arsip riwayat hanya `AnswerText + IsCorrect` (verdict-only, tanpa kunci). Diverifikasi grep branch + Playwright smoke DOM `not.toContain("KUNCIBENAR_*")` + `.list-group-item-success` count 0. | closed |
| T-407-token | Tampering / Auth Bypass | stale TempData token pasca-retake | mitigate | `TempData.Remove($"TokenVerified_{id}")` setelah `ExecuteAsync` sukses, sebelum `RedirectToAction("StartExam")`. Diuji `RetakeExam_Success_ClearsTokenAndRedirectsToStartExam`. | closed |
| T-407-doublearchive | Tampering / Race Condition | double-submit double-archive | accept | Dikelola `RetakeService.ExecuteAsync` via claim-atomik: status `Open` → early `return Success no-op`; `ExecuteUpdateAsync` WHERE `Status NOT IN (Cancelled,Open)` → `rows==0` → rollback no-op. Endpoint hanya memanggil service. Tidak ada surface baru di fase ini. | closed |
| T-407-xss | Tampering / XSS | user-content di view (QuestionText / UserAnswer / AnswerText) | mitigate | Semua user-content di-render via Razor `@` (auto HTML-encode). ZERO `Html.Raw` di `_RiwayatPekerja.cshtml` dan `ShowWrongFlagsOnly` branch. Countdown JS menggunakan `.textContent` untuk nilai timer; `btn.innerHTML` hanya berisi string literal statis — tanpa interpolasi data pengguna. | closed |
| T-407-jsabort | Availability / Handler-Abort | countdown ReferenceError abort page scripts (lesson 413) | mitigate | Guard `if (!btn) return;` (tombol absen → no-op) + `if (!iso) return;` (tidak ada cooldown → no-op) + `if (isNaN(target)) return;` (atribut invalid → no-op). Playwright `page.on('pageerror')` assert empty (no uncaught error). | closed |
| T-407-drift | Tampering / Logic Drift | tier dihitung inline di view vs helper | accept | Tier diekstrak ke `RetakeRules.ResolveReviewMode` (pure, unit-tested — 6 Fact truth-table termasuk pending null); view hanya merender berdasar `Model.RetakeMode`. Tidak ada if/else inline tier di view. Risiko drift: rendah; logika identik + ter-cover test. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-407-01 | T-407-doublearchive | Double-submit race dikelola di `RetakeService.ExecuteAsync` (claim-atomik + transaction). Endpoint `RetakeExam` hanya memanggil service tanpa logika tambahan. Semantik Open→no-op sudah ter-cover di `RetakeServiceTests`. Tidak ada permukaan baru. | orchestrator (v32.4) | 2026-06-22 |
| AR-407-02 | T-407-drift | `eraRetakeArchives` counting diduplikasi di 3 tempat (controller Results + `CanRetakeAsync` + `ExecuteAsync`). Logika identik dan ter-cover test. Refactor single-source dijadwalkan ke backlog/Phase 408 (IN-03 dari code review). Risiko drift saat ini: rendah. | orchestrator (v32.4) | 2026-06-22 |

---

## Unregistered Threat Flags

Tidak ada flag di `## Threat Flags` pada SUMMARY.md (407-02 dan 407-03) yang tidak terpetakan ke threat register. Semua surface (endpoint POST, view render, JS countdown) sudah dimodelkan dalam threat register di atas.

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-22 | 9 | 9 | 0 | gsd-security-auditor (Claude) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-22
