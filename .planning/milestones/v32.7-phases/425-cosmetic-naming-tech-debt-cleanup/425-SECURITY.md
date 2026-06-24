---
phase: 425
slug: cosmetic-naming-tech-debt-cleanup
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-24
---

# Phase 425 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Verdict: **SECURED 14/14** (ASVS L1, block_on: high). Fase net-positif keamanan — cleanup non-fungsional, atribut [Authorize]/[ValidateAntiForgeryToken] existing dijaga utuh; cross-validation server-authoritative menambah validasi (CLN-02).

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| HC browser → AddManualAssessment POST | Form input (Score/IsPassed/PassPercentage) menyeberang ke controller; dilindungi `[Authorize(Admin,HC)]` + `[ValidateAntiForgeryToken]` existing | Skor/status assessment (sensitif HC) |
| HC browser → SubmitEssayScore POST (AJAX) | Input (sessionId/questionId/score) menyeberang; dilindungi `[Authorize(Admin,HC)]` + `[ValidateAntiForgeryToken]`; respons JSON dibaca frontend JS | Skor essay |
| controller → TempData → view | Pesan warning CLN-02 di-render Razor (auto-encode) | Teks peringatan numerik+statis |
| client → exam timer (existing) | Durasi ujian dihitung server-side; CLN-04 ganti formula inline → helper identik (parity) | Tidak ada input baru |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-425-01 | Tampering | AssessmentSession.cs CLN-03 RESERVED | accept | Kolom `AssessmentPhase` (:190) TIDAK di-drop; no migration baru (git diff Migrations/ empty) — zero schema change | closed |
| T-425-02 | Information Disclosure | Views label ValidUntil | accept | `[Display(Name="Berlaku Sampai")]` static label; Razor auto-encode, no Html.Raw | closed |
| T-425-03 | Tampering | UserPackageAssignment sentinel | accept | Komentar saja; nama field `AssessmentPackageId` (:20) & binding tak berubah | closed |
| T-425-04 | Tampering | CMPController timer (allowedSec/graceLimit) | mitigate | 4 situs (:1191/:1564/:1642/:4663) → `ExamTimeRules.AllowedExamSeconds`; formula inline `*60` habis (0 match); `graceLimitSec = allowedSec + 120.0` tak berubah; parity tests green | closed |
| T-425-05 | Spoofing | Token gate FLOW-08 TempData.Peek | accept | OUT OF SCOPE (D-03 defer); `TempData.Peek` count=3 tak berkurang; blok StartExam tak diedit; impersonation guard existing aktif | closed |
| T-425-06 | Elevation | GET StartExam side-effect FLOW-10 | accept | OUT OF SCOPE (D-03 defer); tidak disentuh; mitigasi existing tetap berlaku | closed |
| T-425-07 | Tampering (CSRF) | AddManualAssessment POST | mitigate | `[ValidateAntiForgeryToken]` (:687) utuh — CLN-02 hanya tambah blok body, tidak sentuh atribut | closed |
| T-425-08 | Elevation (authz) | AddManualAssessment POST | mitigate | `[Authorize(Roles="Admin, HC")]` (:688) utuh | closed |
| T-425-09 | Tampering/Info (XSS) | TempData["Warning"] render | mitigate | Pesan numerik (Score/PassPercentage) + teks statis (:752-754); `@TempData["Warning"]` (ManageAssessment.cshtml:44) auto-encode, no Html.Raw | closed |
| T-425-10 | Integrity | cross-validate logic | mitigate | `ManualEntryRules.PassStatusMismatch` (pure, EF-free) non-blocking (no return/AddModelError di if-block); server-authoritative net-positif; `Schedule = model.CompletedAt` (:775) preserved; tests green | closed |
| T-425-11 | Tampering (CSRF) | SubmitEssayScore POST | mitigate | `[ValidateAntiForgeryToken]` (:3683) utuh | closed |
| T-425-12 | Elevation (authz) | SubmitEssayScore POST | mitigate | `[Authorize(Roles="Admin, HC")]` (:3682) utuh; signature tak berubah | closed |
| T-425-13 | Integrity | JsonFail JSON shape | mitigate | 6 guard cluster → `this.JsonFail(...)`; `ControllerGuards.JsonFail` (:17) shape byte-identik `{"success":false,"message":"..."}`; parity test green; jalur success=true tak tersentuh | closed |
| T-425-14 | Information Disclosure | error message | accept | Teks pesan IDENTIK existing; helper tak bocorkan info baru | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-425-01 | T-425-05 / T-425-06 | FLOW-08 token server-authoritative + FLOW-10 write-on-GET StartExam **DEFER ke backlog** (D-03): by-design + sudah dimitigasi (impersonation guard); migration/ubah-perilaku di fase cleanup = risiko regresi tinggi. Verified tak dilemahkan (TempData.Peek count=3, StartExam block unedited). | Rino (developer), D-03 CONTEXT | 2026-06-24 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-24 | 14 | 14 | 0 | gsd-security-auditor (sonnet, ASVS L1) |

**Verification:** build `dotnet build` 0 error (24 baseline warning); 23/23 security/parity test green (ControllerGuards shape + ManualEntryRules cross-validate + ExamTimeRules timer); 0 implementation file dimodifikasi audit (read-only).

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-24
