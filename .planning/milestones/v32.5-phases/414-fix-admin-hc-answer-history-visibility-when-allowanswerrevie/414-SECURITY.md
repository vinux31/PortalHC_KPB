---
phase: 414-fix-admin-hc-answer-history-visibility-when-allowanswerrevie
secured: 2026-06-22
asvs_level: 1
block_on: high
threats_total: 3
threats_closed: 3
threats_open: 0
status: SECURED
---

# Phase 414 Security Verification — Fix Visibilitas History Jawaban Admin/HC saat AllowAnswerReview OFF

**Verified:** 2026-06-22
**ASVS Level:** 1 | **block_on:** high
**Scope:** Verifikasi mitigasi `<threat_model>` PLAN.md ADA di kode terimplementasi. Off-theme bugfix v32.5 net-neutral access-display (migration=FALSE). BUKAN scan kerentanan baru.

## Threat Verification Summary

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-414-01 | Information Disclosure (Broken Access Control) | mitigate | CLOSED | `Controllers/CMPController.cs:2218-2219` |
| T-414-02 | Tampering / XSS | mitigate | CLOSED | `Views/CMP/Results.cshtml:322-328` |
| T-414-03 | Spoofing (impersonation salah-owner) | accept | CLOSED | `Controllers/CMPController.cs:2216,2230` (accepted-risk log di bawah) |

**Closed:** 3/3 | **Open:** 0 | **Severity max:** < high. Tidak ada blocker (block_on=high).

---

## T-414-01 — Information Disclosure / Broken Access Control (mitigate) → CLOSED

**Mitigasi dideklarasi:** Gerbang akses `IsResultsAuthorized(...)` + `if (!isAuthorized) return Forbid();` TETAP dijalankan tanpa perubahan SEBELUM helper baru. `CanReviewAnswers` hanya melonggarkan DISPLAY review untuk non-owner yang SUDAH lolos akses. Regression-of-privacy guard: owner+toggle-OFF WAJIB tetap `false`.

**Bukti kode (read-verified):**
- `Controllers/CMPController.cs:2218` — `bool isAuthorized = IsResultsAuthorized(assessment.UserId, user.Id, roleLevel, user.Section, assessment.User?.Section);`
- `Controllers/CMPController.cs:2219` — `if (!isAuthorized) return Forbid();` — gerbang akses utama UTUH, berjalan SEBELUM helper baru.
- Helper baru dihitung SETELAH gerbang akses: `L2230 bool isOwner = ...; L2231 bool canReviewAnswers = CanReviewAnswers(assessment.AllowAnswerReview, isOwner);` — tidak ada endpoint baru, tidak ada query data baru sebelum auth, tidak ada auth-bypass. Net access surface tak berubah (non-owner yang melihat review tambahan SUDAH berwenang buka Results sebelum fix).
- Gate build memakai variabel efektif: `L2271 if (canReviewAnswers)` (bukan lagi `if (assessment.AllowAnswerReview)` — gate lama 0 occurrence).
- Regression-of-privacy guard terbukti: `HcPortal.Tests/CanReviewAnswersTests.cs:15` `[InlineData(false, true, false)]` (owner + OFF → false) PASS 4/4. `CanReviewAnswers(false,true) = false || !true = false` → VM `CanReviewAnswers=false` → owner tetap masuk alert lama (Results.cshtml:420 `else if (!Model.CanReviewAnswers)`).

**Verdict:** Mitigasi PRESENT. Gerbang akses ASVS V4 (Access Control) tak diubah; perubahan hanya DISPLAY pasca-auth. CLOSED.

---

## T-414-02 — Tampering / XSS pada Nota Admin (mitigate) → CLOSED

**Mitigasi dideklarasi:** Nota = teks Bahasa Indonesia STATIC via Razor encoded markup (`@if` + literal), TANPA interpolasi data user dan TANPA `@Html.Raw`.

**Bukti kode (read-verified):**
- `Views/CMP/Results.cshtml:323` — `@if (Model.CanReviewAnswers && !Model.AllowAnswerReview)` (kondisi nota, derived flag — bukan data user).
- `Views/CMP/Results.cshtml:325-327` — blok nota: `<div class="alert alert-info ..."><i class="bi bi-eye-slash me-1"></i>Peserta tidak dapat melihat tinjauan ini (Tinjauan Jawaban dinonaktifkan). Hanya admin/HC yang melihatnya.</div>` — teks STATIC literal, Razor-encoded by default, TANPA interpolasi `@Model.x` data user.
- `@Html.Raw` di seluruh `Results.cshtml` = hanya 2 occurrence (`L272-273`), keduanya pre-existing serialisasi data chart radar ElemenTeknis (numeric/label), DI LUAR blok nota Phase 414 dan out of scope. NOL `@Html.Raw` baru diperkenalkan.

**Verdict:** Mitigasi PRESENT. Tidak ada XSS surface baru. CLOSED.

---

## Accepted Risks Log

### T-414-03 — Spoofing via impersonation salah-owner (accept) → CLOSED (accepted)

**Risiko:** `isOwner` di action Results dapat salah jika dihitung dari identity yang keliru saat impersonation.

**Mengapa diterima (low):** `isOwner` dihitung dari user EFEKTIF, bukan klaim mentah `User`.
- `Controllers/CMPController.cs:2216` — `var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();` (impersonation-aware, sumber yang SAMA dipakai `IsResultsAuthorized`).
- `Controllers/CMPController.cs:2230` — `bool isOwner = assessment.UserId == user.Id;` (dihitung dari `user.Id` efektif, BUKAN `User` claims asli; Pitfall 4 — JANGAN `_userManager.GetUserAsync(User)` lagi).

Konsisten dengan pola existing yang sudah ter-mitigasi (gerbang akses `IsResultsAuthorized` juga memakai `user.Id` efektif). Risiko residual rendah; diterima by-design untuk fitur impersonasi yang sudah ada. CLOSED (accepted-risk).

---

## Threat Flags (dari SUMMARY.md)

SUMMARY.md `## Security Notes` mendokumentasi T-414-01/02/03 yang sudah ter-register di threat model — informasional, terpetakan ke threat ID existing. Tidak ada `## Threat Flags` section dengan attack surface baru yang tak terpetakan.

**Unregistered flags:** none.

---

## Net Assessment

- Perubahan net-neutral terhadap access surface: gerbang akses (`IsResultsAuthorized` + `Forbid()`) berjalan tanpa perubahan SEBELUM helper baru; helper hanya melonggarkan DISPLAY review untuk non-owner yang SUDAH berwenang.
- Tidak ada `@Html.Raw` data user baru; nota admin static encoded.
- `isOwner` dari user efektif (impersonation-aware), konsisten dengan gerbang akses.
- migration=FALSE. ASVS L1 terpenuhi untuk scope ini.

**threats_open: 0** — tidak ada gap implementasi. Status: SECURED.
