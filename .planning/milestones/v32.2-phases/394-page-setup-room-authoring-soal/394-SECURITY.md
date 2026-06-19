---
phase: 394-page-setup-room-authoring-soal
auditor: gsd-security-auditor
asvs_level: 1
block_on: high
threats_open: 0
threats_closed: 17
date: 2026-06-18
---

# SECURITY — Phase 394 (page-setup-room-authoring-soal)

## Summary

**SECURED — 17/17 threats closed, 0 open.**

Phase 394 adalah UI/controller layer di atas mesin 393. Tidak ada commit DB di phase ini (D-07). Semua security gate yang bersifat server-authoritative berada di InjectAssessmentService (Phase 393, sudah SECURED 13/13).

---

## Threat Verification

| Threat ID | Category | Disposition | Evidence | Status |
|-----------|----------|-------------|----------|--------|
| T-394-RBAC | Elevation of Privilege | mitigate | `Controllers/InjectAssessmentController.cs:40,50` — `[Authorize(Roles = "Admin, HC")]` hadir pada GET (line 40) dan POST (line 50); Playwright "RBAC Coachee denied" memverifikasi server-side reject | CLOSED |
| T-394-CSRF | Tampering | mitigate | `Controllers/InjectAssessmentController.cs:51` — `[ValidateAntiForgeryToken]` pada POST; `Views/Admin/InjectAssessment.cshtml:115` — `@Html.AntiForgeryToken()` di dalam `<form>` | CLOSED |
| T-394-XSS | Tampering (Injection) | mitigate | `Views/Admin/InjectAssessment.cshtml` — tidak ada `@Html.Raw` pada input pengguna; `populateSummary` (line 596-617) seluruhnya menggunakan `.textContent`; ModelState errors dirender via Razor auto-encode (`@error.ErrorMessage` line 70) | CLOSED |
| T-394-IDOR | Information Disclosure | accept | `Controllers/InjectAssessmentController.cs:143-155` — `ViewBag.Users` hanya berisi `u.IsActive` users dari `_context.Users`; tidak ada user Id arbitrary dapat diquery; seleksi picker divalidasi NIP-side oleh 393 service preflight (NIP resolve dari DB); rasional diterima | CLOSED |
| T-394-02-CSRF-readonly | Tampering | accept | `CheckTitleAvailability` adalah `[HttpGet]` endpoint milik AssessmentAdminController (reuse existing) — read-only, tidak ada state change, tidak perlu antiforgery; rasional diterima | CLOSED |
| T-394-02-IDOR | Information Disclosure/Tampering | mitigate | `Views/Admin/InjectAssessment.cshtml:254` — picker hanya me-render `@user.Id` dari `ViewBag.Users` (active users only); `Controllers/InjectAssessmentController.cs:56-58` — arbitrary UserId yang tidak ada di DB menghasilkan NIP kosong dan di-skip (line 111-112); `Services/InjectAssessmentService.cs:346-350` — preflight reject-all jika NIP tidak ada di sistem | CLOSED |
| T-394-02-future-date | Tampering | mitigate | `Views/Admin/InjectAssessment.cshtml:169` — `max="@DateTime.Today.ToString("yyyy-MM-dd")"` hadir; `InjectAssessment.cshtml:571-573` — `validateStep(1)` menolak `cd.value > INJ_TODAY`; `Services/InjectAssessmentService.cs:356-357` — server preflight `req.CompletedAt.Date > today` = gate otoritatif | CLOSED |
| T-394-02-XSS | Tampering (Injection) | mitigate | Cek-judul result: server strings (category, tanggal, peserta) dirender via `li.textContent` (line 941-942); innerHTML hanya dipakai untuk literal HTML static (icons, alerts — bukan data pengguna); Razor `@foreach user rows` (line 253-260) auto-encode via `@user.FullName`, `@user.Email`, `@user.Section` | CLOSED |
| T-394-03-scorerange | Tampering | mitigate | `InjectAssessment.cshtml:821-833` — client validation scoreValue 1-100, MC=1 correct, MA≥2 correct, Essay rubrik (convenience); `Services/InjectAssessmentService.cs:47-48` — `PreflightValidateAsync` reject-all di service (393) adalah gate otoritatif; per-worker answer validation di service (line 380-391) | CLOSED |
| T-394-03-XSS-question | Tampering (Injection) | mitigate | Question text dalam Daftar Soal: `InjectAssessment.cshtml:763` — `span.textContent = q.QuestionText` (XSS-safe); tabel dibangun via DOM API `createElement`, bukan innerHTML data pengguna; `_InjectQuestionForm.cshtml` tidak menggunakan `@Html.Raw`; confirm summary line 599 `setText` via `.textContent` | CLOSED |
| T-394-03-certdup | Tampering/data integrity | mitigate | `Services/InjectAssessmentService.cs:280` — `CertNumberHelper.IsDuplicateKeyException` menangani UNIQUE constraint violation pada cert auto; cert manual uniqueness divalidasi oleh UNIQUE index (`NomorSertifikat`) pada AssessmentSessions; UI hint pada line 365 InjectAssessment.cshtml adalah informational saja | CLOSED |
| T-394-03-nodbwrite | Tampering | mitigate | `Controllers/InjectAssessmentController.cs:63-65` — POST hanya memanggil `MapToRequest` + `TempData`, tidak memanggil `_injectService.InjectBatchAsync`; tidak ada `CreateQuestion` endpoint dipanggil; `InjectAssessment.cshtml:871-874` — `QuestionsJson` hanya di-serialize ke hidden field saat submit; Playwright no-DB-write test memverifikasi `AssessmentSessions.Count` tidak berubah | CLOSED |
| T-394-04-RBAC | Elevation of Privilege | mitigate | `Controllers/InjectAssessmentController.cs:50` — `[Authorize(Roles = "Admin, HC")]` pada POST; identical dengan GET; Playwright RBAC test cover both roles + deny; sama dengan T-394-RBAC | CLOSED |
| T-394-04-CSRF | Tampering | mitigate | `Controllers/InjectAssessmentController.cs:51` — `[ValidateAntiForgeryToken]`; `InjectAssessment.cshtml:115` — `@Html.AntiForgeryToken()`; sama dengan T-394-CSRF | CLOSED |
| T-394-04-NIPforge | Tampering/IDOR | mitigate | `Controllers/InjectAssessmentController.cs:56-58` — NIP di-resolve HANYA dari `_context.Users` untuk UserIds yang dipilih; `line 111-112` — UserId yang tidak ditemukan di dict (tidak ada/asing) → NIP kosong → di-skip (`continue`); `Services/InjectAssessmentService.cs:346-370` — NIP tidak dikenal di DB → preflight error, reject-all; tidak ada NIP sembarang dari client diterima | CLOSED |
| T-394-04-future-backdate | Tampering | mitigate | `Controllers/InjectAssessmentController.cs:82-83` — `CompletedAt` dipetakan langsung ke `InjectRequest.CompletedAt`; `Services/InjectAssessmentService.cs:356-357` — server preflight `CompletedAt.Date > today` reject-all adalah gate otoritatif; client `max=today` adalah UX saja | CLOSED |
| T-394-04-XSS-summary | Tampering (Injection) | mitigate | `InjectAssessment.cshtml:594-617` — `populateSummary` seluruhnya menggunakan fungsi `setText(id, v)` yang mengakses `el.textContent = v` (line 596); tidak ada `@Html.Raw` atau `innerHTML` dengan data pengguna di blok confirm summary; `sum-worker-list` dirender via `injRenderSelectedNames` yang menggunakan `node.textContent` (line 663-665) | CLOSED |

---

## Accepted Risks Log

| Threat ID | Rationale |
|-----------|-----------|
| T-394-IDOR | ViewBag.Users hanya listing active users dari DB — tidak ada arbitrary-id surface baru. Picker selection divalidasi server-side oleh 393 preflight (NIP resolution). Tidak perlu mitigasi tambahan di layer 394. |
| T-394-02-CSRF-readonly | CheckTitleAvailability adalah GET endpoint read-only yang ada sebelum phase 394 (mirror pattern CreateAssessment). Tidak ada state change terjadi. Antiforgery tidak diperlukan untuk GET. |

---

## Threat Flags dari SUMMARY.md

SUMMARY 394-01..04 tidak mencantumkan `## Threat Flags` eksplisit — tidak ada ancaman baru tidak-terdaftar yang dilaporkan oleh executor.

Deviasi yang dicatat dalam SUMMARY (partial path 500, wizard nav validation, snapshot drift) tidak memiliki implikasi keamanan.

---

## XSS Analysis Notes

Semua penggunaan `innerHTML` dalam kode JavaScript diverifikasi menggunakan string literal saja (bukan data pengguna):
- Clear pattern (`innerHTML = ''`) sebelum DOM append — aman
- Literal HTML icons dan alert templates — aman
- Data dari server (CheckTitleAvailability response) dan dari client state (injQuestions[]) selalu dirender via `.textContent` atau DOM createElement

Satu-satunya Razor rendering yang bisa memasukkan data pengguna adalah via `@user.FullName`, `@user.Email`, `@user.Section` (line 256-257 InjectAssessment.cshtml) dan `@error.ErrorMessage` (line 70) — semua di-auto-encode oleh Razor (tidak ada `@Html.Raw`).

---

## Security Boundary Clarification

Phase 394 adalah UI layer. Semua gate keamanan yang bersifat server-authoritative berada di Phase 393:
- NIP resolution reject-all: `InjectAssessmentService.PreflightValidateAsync` line 341-420
- CompletedAt future-date reject: line 356-357
- Cert uniqueness (UNIQUE index + retry): line 268-281
- Score range validation: line 383-391

Phase 394 tidak mem-bypass gate-gate tersebut — POST hanya memanggil `MapToRequest` (pure mapping tanpa DB write) dan menampilkan TempData notice.
