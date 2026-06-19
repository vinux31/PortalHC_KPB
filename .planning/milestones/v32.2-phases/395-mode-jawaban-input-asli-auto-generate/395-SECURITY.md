---
phase: 395-mode-jawaban-input-asli-auto-generate
asvs_level: 1
threats_total: 15
threats_closed: 15
threats_open: 0
generated: 2026-06-18
---

# Phase 395 Security Verification — SECURED 15/15

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-395-01 | Tampering / ceiling | mitigate | CLOSED | `Services/InjectAssessmentService.cs:571-576` — `if (targetPercent > ceilingPercent)` → `TargetReachable: false`; tidak ada cap diam-diam. Best-effort all-correct dikembalikan + controller guard `FindBlockedAutoGenNips` blokir commit. |
| T-395-02 | Repudiation / seed determinism | mitigate | CLOSED | `Services/InjectAssessmentService.cs:520-521` — `System.Security.Cryptography.SHA256.Create()` + `ComputeHash(UTF8(canonical))` + `BitConverter.ToInt32 & 0x7FFFFFFF`. `string.GetHashCode()` hanya muncul di komentar penjelas, bukan kode. |
| T-395-03 | Tampering / TextAnswer-wajib | mitigate | CLOSED | `Services/InjectAssessmentService.cs:396-397` — `if (ans.EssayScore.HasValue && string.IsNullOrWhiteSpace(ans.TextAnswer)) errors.Add(...)`. Guard `EssayScore.HasValue` memastikan essay auto-gen (omit) tidak terblokir. Reject-all (D-03, tidak menulis DB). |
| T-395-04 | Information Disclosure / SHA-256 seed | accept | CLOSED | SHA-256 digunakan semata untuk determinisme lintas-proses, bukan kontrol keamanan. Komentar kode eksplisit: "SHA-256 di sini = NON-secret (hanya determinisme, BUKAN kontrol keamanan)." Nilai seed non-secret, tidak dikembalikan ke client. Tidak ada klaim kriptografis. |
| T-395-05 | Elevation / PreviewInjectScore | mitigate | CLOSED | `Controllers/InjectAssessmentController.cs:103-104` — `[Authorize(Roles = "Admin, HC")]` pada action `PreviewInjectScore`. |
| T-395-06 | Tampering / CSRF preview+commit | mitigate | CLOSED | `Controllers/InjectAssessmentController.cs:50-52` — `[ValidateAntiForgeryToken]` pada POST `InjectAssessment`. `Controllers/InjectAssessmentController.cs:103-105` — `[ValidateAntiForgeryToken]` pada `PreviewInjectScore`. Kedua POST endpoint dilindungi. |
| T-395-07 | Tampering / forge skor | mitigate | CLOSED | `Controllers/InjectAssessmentController.cs:134` — `AssessmentScoreAggregator.Compute(qInMem, respInMem, preq.PassPercentage)` dalam body `PreviewInjectScore`. Skor dihitung server dari pola, bukan diterima dari client sebagai persen final. Preview tidak menulis DB (tidak ada `SaveChanges`). |
| T-395-08 | DoS / AnswersJson malformed | mitigate | CLOSED | `Controllers/InjectAssessmentController.cs:329-338` — `ParseAnswerVms` try/catch `JsonException` → fallback `new()`. JSON malformed tidak menghasilkan 500; pola identik `ParseQuestionVms`. |
| T-395-09 | Integrity/Repudiation / auto-gen unreachable commit | mitigate | CLOSED | `Controllers/InjectAssessmentController.cs:66-72` — `FindBlockedAutoGenNips` re-derive ceiling server-side; bila ada NIP dengan `TargetReachable=false` → tidak memanggil `InjectBatchAsync`, kembalikan `TempData["Error"]`. Audit log `ManualInject`/`ManualInjectRejected` sudah ada di `InjectBatchAsync`. |
| T-395-10 | Information Disclosure / preview bocor cert# | accept→mitigate | CLOSED | `Controllers/InjectAssessmentController.cs:136` — komentar eksplisit "NO CertNumberHelper (D-09), NO SaveChanges." Grep `CertNumberHelper` pada controller hanya menghasilkan komentar tersebut, bukan pemanggilan. Tidak ada nomor sertifikat di `InjectPreviewResult`. |
| T-395-11 | Information Disclosure / IDOR targetPercent/EssayScore | mitigate | CLOSED | `Services/InjectAssessmentService.cs:387-390` — validasi `EssayScore` range `0..ScoreValue` di `PreflightValidateAsync`. `BuildAutoGenAnswers` melaporkan `TargetReachable=false` bila `target > ceiling` (D-08.3), mencegah target arbitrer menghasilkan sertifikasi tidak valid. |
| T-395-12 | Tampering / XSS via user text render Step-5 | mitigate | CLOSED | `Views/Admin/InjectAssessment.cshtml:1214` — `qText.textContent = q.QuestionText`. `:1260` — `otxt.textContent = opt.OptionText`. `:1162` — `headingEl.textContent` untuk nama pekerja. Semua data user di Step-5 di-render via `.textContent`, bukan `innerHTML`. |
| T-395-13 | Tampering / CSRF fetch Pratinjau | mitigate | CLOSED | `Views/Admin/InjectAssessment.cshtml:1514-1536` — token diambil dari `document.querySelector('input[name="__RequestVerificationToken"]').value`, dikirim sebagai header `RequestVerificationToken` pada fetch POST `/Admin/PreviewInjectScore`. |
| T-395-14 | Tampering/DoS / lupa serialize #AnswersJson | mitigate | CLOSED | `Views/Admin/InjectAssessment.cshtml:990-994` — `answersHidden.value = JSON.stringify(buildWorkerAnswersPayload())` ada di submit-listener yang SAMA dengan serialize `#QuestionsJson` (:990). E2e `inject-assessment-395.spec.ts` memverifikasi `#AnswersJson` non-empty + skor di `/CMP/Results` bukan 0. |
| T-395-15 | Integrity / commit worker BLOCKING | mitigate | CLOSED | Defense-in-depth: (1) UI — markup `#step5Blocking` + tombol "Beralih ke input asli" (`Views/Admin/InjectAssessment.cshtml:511-517`) ditampilkan bila `preview.blocked`; (2) Server — `FindBlockedAutoGenNips` di `Controllers/InjectAssessmentController.cs:66-72` mencegah commit meski UI bypass. |

## Accepted Risks Log

| Threat ID | Rationale | Residual Risk |
|-----------|-----------|---------------|
| T-395-04 | SHA-256 dipakai semata untuk seed deterministik (preview==commit reproducibility). Nilai seed tidak pernah menjadi bahan keputusan akses/keamanan; non-secret; tidak dikembalikan ke client. ASVS L1 tidak mensyaratkan perlindungan seed determinisme non-kriptografis. | Minimal — bahkan jika seed dapat ditebak, penyerang hanya mengetahui pola jawaban simulasi, bukan mengubahnya (pola dihitung ulang server-authoritative saat commit). |
| T-395-10 | Disposisi plan berubah dari `accept` ke `mitigate` (D-09 diimplementasikan — no CertNumberHelper di preview). Tidak ada nomor sertifikat dalam response `InjectPreviewResult`. Risiko residual nol. | Nol. |

## Unregistered Flags (dari SUMMARY.md Threat Flags)

Tidak ada `## Threat Flags` eksplisit di SUMMARY files 395-01/02/03. Deviasi yang dicatat di SUMMARY (null-NIP user di-skip, pre-fill D-10 tidak diimplementasi) bukan vektor keamanan — tidak mempengaruhi integritas sertifikasi atau autentikasi.

## Verification Performed

- `Services/InjectAssessmentService.cs` — BuildAutoGenAnswers, ComputeAutoGenSeed, AutoGenResult, rule TextAnswer-wajib di PreflightValidateAsync
- `Controllers/InjectAssessmentController.cs` — PreviewInjectScore, ParseAnswerVms, FindBlockedAutoGenNips, MapToRequest, wire InjectBatchAsync; atribut RBAC + CSRF
- `Views/Admin/InjectAssessment.cshtml` — #AnswersJson hidden, serialize di submit-listener, Pratinjau fetch + token, Step-5 .textContent render, BLOCKING markup
