# Phase 296: Data Foundation + GradingService Extraction - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-06
**Phase:** 296-Data Foundation + GradingService Extraction
**Areas discussed:** GradingService Design, Migration Strategy, QuestionType Enum Storage, Grading Extensibility, NomorSertifikat, Interview Handling, Error Handling, Tipe Soal Simplification, Multiple Answer Storage

---

## GradingService Design — Struktur

| Option | Description | Selected |
|--------|-------------|----------|
| Interface + DI | IGradingService + GradingService, bisa di-mock untuk testing | |
| Concrete class + DI | Seperti AuditLogService, tanpa interface | ✓ |
| Static helper class | Seperti CertNumberHelper, static methods | |

**User's choice:** Concrete class + DI (Recommended)
**Notes:** User meminta penjelasan simpel terlebih dahulu. Setelah dijelaskan analogi dengan AuditLogService yang sudah familiar, user setuju.

---

## GradingService Design — Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Grading + TrainingRecord + Notification (semua 4 hal) | Satu method handle semuanya, eliminasi duplikasi total | ✓ |
| Grading saja | Hanya hitung skor dan update session | |

**User's choice:** Semua 4 hal (Recommended)
**Notes:** User meminta analisa dan penjelasan dulu sebelum memilih.

---

## Migration Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Satu migration | Semua kolom baru dalam 1 file migration, atomic | ✓ |
| Dua migration terpisah | Pisah per concern (PackageQuestion vs AssessmentSession) | |

**User's choice:** Satu migration (Recommended)

---

## QuestionType Enum Storage

| Option | Description | Selected |
|--------|-------------|----------|
| String | 'MultipleChoice', 'TrueFalse', dll. Readable, konsisten dengan Status | ✓ |
| Integer | 0, 1, 2, 3, 4. Compact tapi perlu mapping | |

**User's choice:** String (Recommended)

---

## Grading Extensibility

| Option | Description | Selected |
|--------|-------------|----------|
| Switch-case siap, logika MC saja | Kerangka switch sudah ada, tipe lain NotImplementedException | ✓ |
| MC saja, tanpa switch | Phase 298 yang refactor struktur | |

**User's choice:** Switch-case siap (Recommended)
**Notes:** User meminta penjelasan dengan contoh kode sebelum memilih.

---

## NomorSertifikat di GradingService

| Option | Description | Selected |
|--------|-------------|----------|
| Masukkan ke GradingService | GradeAndCompleteAsync() juga generate sertifikat | ✓ |
| Tetap di controller | NomorSertifikat generate di controller masing-masing | |

**User's choice:** Masukkan (Recommended)
**Notes:** User meminta analisa lengkap. Diberikan penjelasan tentang dependency chain (IsPassed → sertifikat) dan dampak ke Phase 297 (PPT-09).

---

## Interview (Tahun 3) Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Tetap terpisah | GradingService khusus exam online | ✓ |
| Masukkan juga | GradingService handle semua jenis grading | |

**User's choice:** Tetap terpisah (Recommended)

---

## Error Handling & Logging

| Option | Description | Selected |
|--------|-------------|----------|
| Ikuti pattern yang ada | Race guard + status guard + ILogger + AuditLogService | ✓ |
| Return result object | GradingResult { Success, Score, ErrorMessage } | |

**User's choice:** Ikuti pattern yang ada (Recommended)

---

## Tipe Soal Simplification

| Option | Description | Selected |
|--------|-------------|----------|
| 3 tipe (MC, MA, Essay) | Drop True/False dan Fill in the Blank | ✓ |
| 5 tipe (original) | Semua tipe dari REQUIREMENTS.md | |

**User's choice:** 3 tipe saja
**Notes:** User secara proaktif menyederhanakan tipe soal. True/False bisa dibuat sebagai MC biasa. Fill in the Blank di-drop karena exact match bermasalah.

---

## Multiple Answer Storage

| Option | Description | Selected |
|--------|-------------|----------|
| Multiple rows per soal | 1 row per opsi yang dipilih, konsisten dengan struktur tabel | ✓ |
| JSON array di field baru | SelectedOptionIds JSON string, 1 row per soal | |

**User's choice:** Multiple rows per soal (Recommended)

---

## Claude's Discretion

- File placement GradingService
- SaveChanges strategy
- SessionElemenTeknisScore handling
- Exact method signatures

## Deferred Ideas

- True/False sebagai tipe terpisah — bisa jadi MC 2 opsi
- Fill in the Blank — exact match bermasalah
- Interview Tahun 3 di GradingService — alur berbeda
