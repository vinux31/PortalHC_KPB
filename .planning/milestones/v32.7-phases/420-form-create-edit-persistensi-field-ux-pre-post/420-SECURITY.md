---
phase: 420-form-create-edit-persistensi-field-ux-pre-post
audit_date: 2026-06-23
asvs_level: 1
block_on: high
threats_total: 13
threats_closed: 13
threats_open: 0
verdict: SECURED
---

# Phase 420 — Security Audit (SECURED)

**Phase:** 420 — form-create-edit-persistensi-field-ux-pre-post
**Milestone:** v32.7 Pre/Post — branch ITHandoff
**Threats Closed:** 13/13
**ASVS Level:** L1 | **block_on:** high
**Verdict:** SECURED (0 open)

Scope: verifikasi keberadaan mitigasi yang dideklarasikan di threat register PLAN 420-01/02/03 terhadap kode produksi terimplementasi. Tidak ada scan ancaman baru. File implementasi READ-ONLY (tak dimodifikasi). Live UAT 2026-06-23 @localhost:5270 sebagai korroborasi produk; audit ini memverifikasi mitigasi di level kode (clamp, antiforgery, authz, ordering guard, redirect, no-new-Html.Raw, rename atomik).

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence (file:line) |
|-----------|----------|-------------|--------|----------------------|
| T-420-01 | Tampering | mitigate | CLOSED | `Math.Clamp(model.MaxAttempts,1,5)` + `Math.Clamp(model.RetakeCooldownHours,0,168)` di SEMUA 4 jalur tulis: Create Pre `AssessmentAdminController.cs:1257-1258`, Create Post `:1296-1297`, Create std `:1489-1490`, Edit std loop `:2124-2125`. |
| T-420-02 | Tampering | mitigate | CLOSED | Object-init whitelist eksplisit (bukan trust model penuh) di Create std `:1474-1501`, Pre `:1244-1267`, Post `:1283-1309`. `NomorSertifikat = null` di Create std `:1494`; `ModelState.Remove("NomorSertifikat")` `:1038` (server-generated, tak di-bind dari form). |
| T-420-03 / T-420-csrf | Spoofing | mitigate | CLOSED | `[ValidateAntiForgeryToken]` di POST CreateAssessment `:863` dan POST EditAssessment `:1822` (dipertahankan). |
| T-420-04 | Information Disclosure | accept | CLOSED (accepted) | EditAssessment shuffle render = boolean toggle saja `EditAssessment.cshtml:446` (`asp-for="ShuffleQuestions"`) + `:451` (`asp-for="ShuffleOptions"`), dari Model yang sudah di-authorize. Tidak ada PII/kunci jawaban. Lihat Accepted Risks. |
| T-420-lock | Tampering | mitigate | CLOSED | Guard group-aware `isCompleted` via `AnyAsync(a => a.LinkedGroupId == assessment.LinkedGroupId && a.Status == "Completed")` `AssessmentAdminController.cs:1842-1847`, DIANGKAT ke atas cabang Pre-Post `:1854-1856` → guard mendahului mutasi; menolak → `RedirectToAction("ManageAssessment")` + `TempData["Error"]` `:1848-1852`. |
| T-420-manual | Tampering | mitigate | CLOSED | GET Edit: `if (assessment.IsManualEntry) return RedirectToAction("EditManualAssessment", "TrainingAdmin", new { id })` `:1706-1707`, SEBELUM `bool isPrePost` `:1710`. Defense-in-depth POST Completed guard `:1842-1852` mencegah mutasi sesi manual yang Completed. |
| T-420-authz | Elevation of Privilege | mitigate | CLOSED | `[Authorize(Roles="Admin, HC")]` di keempat aksi: CreateAssessment GET `:653`, CreateAssessment POST `:862`, EditAssessment GET `:1691`, EditAssessment POST `:1821`. |
| T-420-idor-edit | Information Disclosure | accept | CLOSED (accepted) | Edit di tingkat assessment (bukan per-jawaban-peserta); authz role-based Admin/HC membatasi ke pengelola; tidak ada eskalasi baru. Konsisten pola existing. Lihat Accepted Risks. |
| T-420-stale | DoS / Tampering | mitigate | CLOSED | `setStdInputsDisabled(true)` men-`disabled` (BUKAN d-none) input `#standard-jadwal-section` + `#schedHidden` + `#ewcdHidden` `CreateAssessment.cshtml:2050-2058`, dipanggil `applyPrePostLayout()` `:2102`; aksi-balik `setStdInputsDisabled(false)` di `applyStandardLayout()` `:2118`. disabled = tidak ter-submit; server cabang Pre-Post baca PreSchedule/PostSchedule, bukan field std. |
| T-420-massassign | Tampering | mitigate | CLOSED | `CreationMode` = param scalar `string?` `AssessmentAdminController.cs:867`, hanya penanda mode (`:878`, `:925`, `:1040-1044`); TIDAK mem-bind kolom DB sensitif. Kolom DB `AssessmentType` di-set via literal (`"Standard"`/`"PreTest"`/`"PostTest"`), tak di-bind dari form. |
| T-420-xss | Information Disclosure / Tampering | mitigate | CLOSED | 0 `Html.Raw` BARU di region redesign. Satu-satunya `Html.Raw` = `CreateAssessment.cshtml:901` (`ViewBag.CreatedAssessment` — pre-existing, di luar scope fase). Semua copy Bahasa Indonesia via Razor `@`-encode default. |
| T-420-binding-break | DoS (availability) | mitigate | CLOSED | Rename atomik selesai: grep `AssessmentTypeInput`/`assessmentTypeInput` di Controllers/+Views/+Models/ = **0 sisa**. Binding name↔param utuh (`name="CreationMode"` ↔ param `:867`). Korroborasi runtime: live UAT 2026-06-23 + e2e 8/8 green (commit `3b8f8ac6`). |

## Unregistered Flags

None. Tidak ada section `## Threat Flags` di SUMMARY 420-01/02/03 — executor tidak mendeteksi attack surface baru di luar register. Tidak ada endpoint baru, tidak ada eskalasi privilege di fase ini (perubahan = baris penyalinan + render checkbox + guard/redirect + redesign view + rename atomik).

## Accepted Risks Log

| Threat ID | Category | Rationale |
|-----------|----------|-----------|
| T-420-04 | Information Disclosure | EditAssessment hanya merender dua boolean toggle `ShuffleQuestions`/`ShuffleOptions` dari Model yang sudah dilindungi `[Authorize(Roles="Admin, HC")]`. Tidak ada PII maupun kunci jawaban ter-ekspos. Verifikasi: `EditAssessment.cshtml:446,451` — input `type="checkbox"` saja, label deskriptif (tidak ada nilai sensitif). Risiko: Low. Diterima by-design. |
| T-420-idor-edit | Information Disclosure | Edit beroperasi di tingkat sesi/assessment (bukan akses per-jawaban-peserta). Authz role-based Admin/HC sudah membatasi akses ke pengelola; tidak ada jalur eskalasi baru yang ditambahkan fase ini. Konsisten dengan pola edit existing pra-420. Risiko: Low. Diterima — konsisten perilaku existing. |

## Audit Trail

- **Sumber threat register:** `<threat_model>` di 420-01-PLAN.md (T-420-01..04), 420-02-PLAN.md (T-420-lock/manual/csrf/authz/idor-edit), 420-03-PLAN.md (T-420-stale/massassign/csrf/xss/binding-break). ID di-dedup lintas plan (T-420-csrf/authz muncul di 02+03; diverifikasi sekali pada surface masing-masing).
- **Metode per disposition:** `mitigate` → grep pola mitigasi di file yang dikutip; `accept` → entri dicatat di Accepted Risks Log (di atas).
- **State implementasi:** seluruh file implementasi sudah ter-commit (working tree bersih untuk Controllers/+Views/+Models/ — tidak ada edit liar). Commit relevan: `e043983c` (FORM-05/06 guard+redirect), `1ddfc952` (FORM-10 rename atomik), `3bdc29f8` (FORM-07/08/09/11 redesign), `d72b304a`+`9ec4d657` (FORM-01..04 persistensi), `3b8f8ac6` (UAT 8/8 green).
- **Korroborasi runtime:** live UAT @localhost:5270 2026-06-23 — FORM-09 disabled-anti-POST, FORM-10 binding `#creationMode`, shuffle render-from-model + persist-after-edit, regresi Standard; e2e 8/8 passed, 0 product issue.
- **block_on=high:** 0 ancaman open → tidak ada blocker. Gerbang secure LULUS.

---
*Audit: gsd-security-auditor — 2026-06-23. File implementasi tidak dimodifikasi (READ-ONLY).*
