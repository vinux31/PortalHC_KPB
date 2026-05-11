# Phase 309: Worker Certificate Defensive Fix + Submitted Status Handling - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-29
**Phase:** 309 — worker-cert-defensive-submitted-status
**REQ:** WCRT-01 (audit 27 Apr T10) + SUB-01 (audit 29 Apr T3, bundled 2026-04-29)
**Areas discussed:** Exception copy & redirect, IsAssessmentSubmitted API, Results rendering pending, Helper rollout scope

---

## Gray Area Selection (multiSelect)

**Question:** Phase 309: gray area mana yang ingin didiskusikan?

| Option | Description | Selected |
|--------|-------------|----------|
| Exception copy & redirect | Copy pesan Indonesia + redirect destination saat try-catch Certificate menangkap exception | ✓ |
| IsAssessmentSubmitted API | Bentuk helper baru: bool only / + constant / + dual helper, lokasi placement | ✓ |
| Results rendering pending | Cara render Results page saat status "Menunggu Penilaian" | ✓ |
| Helper rollout scope | Strict 3 lokasi mandated vs grep-swap completeness | ✓ |

**User's choice:** All 4 areas selected.

---

## Exception copy & redirect

| Option | Description | Selected |
|--------|-------------|----------|
| Mirror CertificatePdf | TempData["Error"] = "Gagal memuat sertifikat. Silakan coba lagi." + RedirectToAction("Results", new { id }). Konsisten dengan CertificatePdf catch line 2080. (Recommended) | ✓ |
| Redirect ke Assessment list | TempData["Error"] sama tapi RedirectToAction("Assessment") — lebih aman jika Results sendiri rusak | |
| Per-exception copy | DbException / NRE / generic dengan copy berbeda — lebih informatif tapi lebih lebar surface | |

**User's choice:** "rekomendasi kamu apa?" → Mirror CertificatePdf (recommended option selected)
**Notes:** User explicitly defer to Claude's recommendation. Rationale captured in CONTEXT D-01: konsistensi dengan pattern existing CertificatePdf (line 2080-2082), Results page aman karena tidak depend pada Category/Signatory chain (sumber exotic data), worker tetap punya context score saat redirect.

---

## IsAssessmentSubmitted API

| Option | Description | Selected |
|--------|-------------|----------|
| Constant + bool helper | Tambah `AssessmentStatus.PendingGrading` constant + `IsAssessmentSubmitted(string)` helper. Single helper sufficient. (Recommended) | ✓ |
| Bool helper saja | Cuma helper dengan literal "Menunggu Penilaian" di body, tanpa constant | |
| Konstan + 2 helper | Constant + IsAssessmentSubmitted + IsPendingGrading terpisah | |

**User's choice:** "untuk selanjutnya berikan rekomendasi kamu juga" → Constant + bool helper
**Notes:** User defer to Claude. Rationale captured in CONTEXT D-04, D-05: literal "Menunggu Penilaian" sudah muncul di GradingService L196 & L199 — typo risk → constant cegah itu. Single helper sufficient karena call site yang butuh distinguish PendingGrading bisa pakai langsung `status == AssessmentStatus.PendingGrading` (D-07 di Certificate/CertificatePdf branch).

---

## Results rendering pending

| Option | Description | Selected |
|--------|-------------|----------|
| Interim score + banner + hide pass/fail | Score field (MC+MA interim) + banner alert-info + Essay questions tampil dengan label "Menunggu Penilaian". (Recommended) | ✓ |
| Banner saja, sembunyikan score | Konservatif: banner + skor & review questions di-hide sampai grading selesai | |
| Banner + score, tanpa essay review | Score visible + essay questions di-hide, MC/MA tetap reviewable | |

**User's choice:** "sama" → Interim score + banner + hide pass/fail
**Notes:** Rationale captured in CONTEXT D-08: sesuai SC #10 "Results() render hasil sementara"; transparansi ke worker (mereka tetap lihat usaha MC/MA mereka); pass/fail badge di-hide karena IsPassed null per GradingService L201 — tidak ada Razor `Model.IsPassed == true/false` branch yang valid saat null.

---

## Helper rollout scope

| Option | Description | Selected |
|--------|-------------|----------|
| Strict 3 lokasi | Hanya CMPController L1792, L1858, L2105 sesuai SC #9. (Recommended) | ✓ |
| Grep & swap completeness | Grep `Status == "Completed"` controllers/, swap ke helper di mana semantic submitted | |
| Strict 3 + audit comment | Strict 3 + TODO comment di lokasi lain | |

**User's choice:** "sama" → Strict 3 lokasi
**Notes:** Rationale captured in CONTEXT D-06: minimal-surface compliance, cegah scope creep, deferred touchpoint audit ke milestone next jika user report bug spesifik.

---

## Claude's Discretion

User memberikan otoritas pada Claude untuk:
- Pilih opsi rekomendasi untuk semua 4 gray areas (user explicit: "rekomendasi kamu apa?", "untuk selanjutnya berikan rekomendasi kamu juga", "sama", "sama")
- Razor structure exact untuk Results pending mode (banner placement, conditional ordering)
- Logger scope/category convention (menyesuaikan existing)
- Whether `_Layout.cshtml` butuh TempData Info handler addition (verify existing first)

## Deferred Ideas

Lihat CONTEXT.md `<deferred>` section untuk daftar lengkap. Highlights:
- Per-exception user-facing copy — defer until forensics data
- Grep-swap helper di luar 3 lokasi — defer to milestone next
- GradingService literal "Menunggu Penilaian" → constant refactor — planner judgement call
- Inline pending certificate placeholder view — explicit ROADMAP redirect choice
- AuditLog forensics for pending access — omitted for consistency

---

*Audit trail captured: 2026-04-29*
*Format: AskUserQuestion 2-step (gray area selection → 4-question batch with options + recommendations)*
