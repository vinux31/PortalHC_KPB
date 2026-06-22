---
phase: 408
slug: test-uat
status: verified
threats_total: 13
threats_closed: 13
threats_open: 0
asvs_level: 1
created: 2026-06-22
---

# Phase 408 — Security

> Capstone security gate (D-03) for milestone v32.4 (Ujian Ulang).
> Phase 408 is test-only (zero production code added). This audit verifies:
> (a) consolidated threat register from phases 406+407 shows no regression in implementation;
> (b) invariant T-408-cert (retake-then-pass → exactly 1 certificate) is now test-covered by GAP-1.
> ASVS Level 1. block_on: high (threats_open:0 = SECURED).

---

## Trust Boundaries (Consolidated Milestone v32.4)

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| browser (worker, untrusted) → `CMP/RetakeExam` (POST) | Worker memicu mutasi destruktif (arsip + reset sesi) atas `id` route int | `id` (int), antiforgery token |
| view (Razor) ← VM (server) | `Results.cshtml` mempercaya `RetakeMode`/`CanRetake` dari server (`CMPController.Results`); keputusan leak-safety = server | `RetakeReviewMode` enum + eligibility flags |
| browser (Admin/HC) → `RiwayatPercobaan` GET / `UpdateRetakeSettings` POST | Admin/HC baca riwayat worker (sessionId int) / set config (maxAttempts, cooldown) | jawaban worker; nilai config numerik |
| client countdown JS → server | Enable/disable tombol = UX saja; server re-cek `CanRetakeAsync` saat POST | tidak ada data melintas |
| grade → DB (cert) | `GradingService.GradeAndCompleteAsync` menerbitkan `NomorSertifikat` di bawah unique index | sequence cert (bukan kripto) |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Evidence | Status |
|-----------|----------|-----------|-------------|------------|----------|--------|
| T-408-cert | Tampering / Logic (double-cert) | retake-then-pass menerbitkan sertifikat dobel | mitigate | unique index `IX_AssessmentSessions_NomorSertifikat` + retry 3× `WHERE NomorSertifikat==null` (GradingService.cs:287-312 `if (session.GenerateCertificate && isPassed) { ... WHERE s.NomorSertifikat == null ... }`). DIBUKTIKAN GAP-1: `RetakeThenPassCertTests.RetakeThenPass_IssuesExactlyOneCertificate` asserts `certCount == 1` + format regex. GREEN @SQLEXPRESS (Passed:1, Failed:0). | Services/GradingService.cs:287-312 + HcPortal.Tests/RetakeThenPassCertTests.cs:160-163 | closed |
| T-407-idor | Elevation / Access Control | `CMPController.RetakeExam` ownership guard | mitigate | `if (assessment.UserId != user.Id) return Forbid();` sebelum `CanRetakeAsync` + `ExecuteAsync`. Tidak berubah dari 407. | Controllers/CMPController.cs:2534 | closed |
| T-407-csrf | Tampering / CSRF | RetakeExam POST + modal form | mitigate | `[ValidateAntiForgeryToken]` pada action `RetakeExam`; `@Html.AntiForgeryToken()` di form modal. Tidak berubah dari 407. Lifecycle e2e 408 assert `input[name=__RequestVerificationToken]` count 1. | Controllers/CMPController.cs:2526 | closed |
| T-407-bypass | Tampering / Logic Bypass | cooldown/cap bypass via DevTools | mitigate | Server re-cek `await _retakeService.CanRetakeAsync(id)` SEBELUM `ExecuteAsync` (CMPController.cs:2537); countdown JS non-authoritative (UX only). Tidak berubah dari 407. | Controllers/CMPController.cs:2537 | closed |
| T-407-leak | Information Disclosure | `Results.cshtml` ShowWrongFlagsOnly branch + `_RiwayatPekerja.cshtml` | mitigate | Branch `ShowWrongFlagsOnly` (Results.cshtml:421) TIDAK merender `list-group-item-success`, `(Jawaban Benar)`, atau `CorrectAnswer` — comment "OWN answer only — NO correct-option highlight, NO '(Jawaban Benar)', NO CorrectAnswer/explanation" (Results.cshtml:452). Tidak berubah dari 407. Lifecycle e2e 408 assert pra-retake `not.toContain('(Jawaban Benar)')` + `.list-group-item-success` count 0. | Views/CMP/Results.cshtml:421-452 | closed |
| T-407-token | Tampering / Auth Bypass | stale TempData token pasca-retake | mitigate | `TempData.Remove($"TokenVerified_{id}")` setelah `ExecuteAsync` sukses (CMPController.cs:2553); `StartExam` pakai `TempData.Peek` non-consume (:947). Tidak berubah dari 407. | Controllers/CMPController.cs:2553 | closed |
| T-407-doublearchive | Tampering / Race Condition | double-submit double-archive | accept | Dikelola `RetakeService.ExecuteAsync` via claim-atomik (Open→no-op; `ExecuteUpdateAsync WHERE Status NOT IN (Cancelled,Open) → rows==0 → rollback`). Tidak ada surface baru di fase 408. | AR-407-01 (accepted risk log, 407-SECURITY.md) | closed |
| T-407-xss | Tampering / XSS | user-content riwayat/review | mitigate | Semua user-content di-render via Razor `@` (auto HTML-encode). ZERO `Html.Raw` di `_RiwayatPekerja.cshtml` dan `ShowWrongFlagsOnly` branch. Countdown JS menggunakan `.textContent`. Tidak berubah dari 407. | Views/CMP/Results.cshtml (Razor `@` throughout) + 407-SECURITY.md T-407-xss | closed |
| T-407-jsabort | Availability / Handler-Abort | countdown ReferenceError abort page scripts (lesson 413) | mitigate | Guard `if(!btn)return`/`if(!iso)return`/`isNaN` (verified 407). Lifecycle e2e 408 `page.on('pageerror')` asserted empty di TIAP langkah (408-UAT.md: "0 pageerror"). Tidak berubah dari 407. | 407-SECURITY.md T-407-jsabort + 408-UAT.md "0 pageerror" | closed |
| T-407-drift | Tampering / Logic Drift | tier dihitung inline di view vs helper | accept | Tier diekstrak ke `RetakeRules.ResolveReviewMode` (pure, unit-tested). View hanya merender `Model.RetakeMode`. Tidak berubah dari 407. | AR-407-02 (accepted risk log, 407-SECURITY.md) | closed |
| T-406-01 | Information Disclosure | RiwayatPercobaan GET (riwayat worker mana pun) | mitigate | `[Authorize(Roles="Admin, HC")]` pada action (AssessmentAdminController.cs:3484 pattern, per 406-SECURITY.md). RBAC re-cek tiap fetch. Tidak berubah dari 406. | 406-SECURITY.md T-406-01 (threats_open:0 verified 2026-06-21) | closed |
| T-406-02 | Information Disclosure | per-soal cell membocorkan KUNCI | mitigate | Partial render hanya AnswerText + verdict glyph + AwardedScore; tak ada field kunci di arsip. Tidak berubah dari 406. | 406-SECURITY.md T-406-02 | closed |
| T-406-06 | Tampering / CSRF | UpdateRetakeSettings POST | mitigate | `@Html.AntiForgeryToken()` di form (ManagePackages.cshtml:144) + `[ValidateAntiForgeryToken]` pada endpoint (AssessmentAdminController.cs:5615 pattern). Tidak berubah dari 406. | 406-SECURITY.md T-406-06 | closed |
| T-406-07 | Tampering | bypass min/max → MaxAttempts/cooldown out-of-range | mitigate | Server `Math.Clamp(maxAttempts,1,5)` + `Math.Clamp(cooldown,0,168)` (AssessmentAdminController.cs:5629-5630 pattern). Tidak berubah dari 406. | 406-SECURITY.md T-406-07 | closed |
| T-406-09 | Information Disclosure | card retake muncul di Pre-Test/Manual (invalid) | mitigate | `@if(ViewBag.HideRetakeToggle != true)` guard (ManagePackages.cshtml:135, backed by `RetakeRules.ShouldHideRetakeToggle`). Tidak berubah dari 406. | Views/Admin/ManagePackages.cshtml:135 | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-407-01 | T-407-doublearchive | Double-submit double-archive dikelola `RetakeService.ExecuteAsync` claim-atomik (Open→no-op; `ExecuteUpdateAsync WHERE Status NOT IN(Cancelled,Open)→rows==0 rollback`). Ter-cover `RetakeServiceTests`. Tidak ada surface baru di 408. | orchestrator (v32.4) | 2026-06-22 |
| AR-407-02 | T-407-drift | `eraRetakeArchives` counting diduplikasi 3 tempat (Results + CanRetakeAsync + ExecuteAsync); identik + ter-cover test (RetakeServiceTests + RetakeExamEndpointTests + e2e lifecycle). Single-source = backlog IN-03. JANGAN refactor di 408. | orchestrator (v32.4) | 2026-06-22 |

---

## Unregistered Threat Flags

408-01-SUMMARY.md dan 408-02-SUMMARY.md tidak mencantumkan seksi `## Threat Flags` (fase test-murni, zero production attack surface baru). IN-408-A (grade switch silent-zero untuk QuestionType tak dikenal) dan IN-408-B (seed 406/407 menggunakan label 'SingleAnswer') dicatat di 408-UAT.md sebagai informational / non-blocking — tidak membuka ancaman produksi baru (data valid selalu `MultipleChoice`; guard tidak diperlukan untuk threat register saat ini).

Tidak ada flag yang tidak terpetakan.

---

## T-408-cert Verification Detail

GAP-1 ditutup oleh `HcPortal.Tests/RetakeThenPassCertTests.cs` (commit `39e3ef46`):
- `ExecuteAsync` (reset-only: Status→Open, hapus responses/assignment) → re-seed responses-benar → `GradeAndCompleteAsync` (grade-dari-DB; step 6 issue cert)
- Assert core: `CountAsync(a => a.Id == sid && a.NomorSertifikat != null) == 1` (anti-double-cert guard ter-exercise)
- Assert format: `Assert.Matches(@"^KPB/\d{3}/[IVX]+/\d{4}$", cert)`
- Test result: Passed:1 / Failed:0 / Skipped:0 @SQLEXPRESS (real-SQL, disposable DB dengan full migration chain incl `AddRetakeColumnsAndArchive`)
- Lifecycle e2e (408-02) memperkuat visual: sesi gagal → Ujian Ulang → StartExam → jawab benar → LULUS 100% + "Nomor Sertifikat: KPB/..." ("Lihat Sertifikat") di browser nyata @5270 (408-UAT.md: PASS)

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-21 | 13 | 13 | 0 | Claude (gsd-secure-phase 406), ASVS L1 |
| 2026-06-22 | 9  | 9  | 0 | Claude (gsd-secure-phase 407), ASVS L1 |
| 2026-06-22 | 13 | 13 | 0 | Claude (gsd-secure-phase 408), ASVS L1 — consolidated milestone gate (no regression + T-408-cert covered) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] Phase 408 added zero production code — no new attack surface
- [x] T-408-cert invariant now test-covered (GAP-1 xUnit GREEN + lifecycle e2e PASS)
- [x] 406+407 mitigations confirmed present in implementation (no regression)
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-22
