# Phase 305: Question Type Naming Clarity - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-28
**Phase:** 305-question-type-naming-clarity
**Areas discussed:** Wording, Per-surface (long vs short), Source of truth, Doc cross-cutting scope, Long form syntax, Controller error msg style, Import button label, ExamSummary badge, Worker MC badge symmetry

---

## Wording — Final Label

| Option | Description | Selected |
|--------|-------------|----------|
| Pilihan Tunggal / Pilihan Jamak (sesuai roadmap) | Long: 'Pilihan Tunggal (1 jawaban benar)' / 'Pilihan Jamak (≥2 jawaban benar)' | |
| Satu Jawaban / Banyak Jawaban | Plain Indonesia, tanpa istilah formal | |
| Pilihan Tunggal / Pilihan Ganda (swap) | Risk: 'Pilihan Ganda' rancu transisi | |
| (Other) — request research | User: "coba kamu search dan analisa lagi, dari bahasa inggris juga gapapa" | ✓ |

**User's choice (round 1):** "Other" — minta riset standar Kemendikbud + LMS internasional, bahasa Inggris OK.

**Riset findings (presented to user):**
- Kemendikbud AKM: "Pilihan Ganda" (1 jawaban) / "Pilihan Ganda Kompleks" (multi)
- Moodle: "Single answer" / "Multiple answers"
- Canvas: "Multiple Choice" / "Multiple Answer"

**Round 2 options:**

| Option | Description | Selected |
|--------|-------------|----------|
| Kemendikbud AKM: 'Pilihan Ganda' / 'Pilihan Ganda Kompleks' (Recommended) | Standard Asesmen Nasional Indonesia | |
| Roadmap default: 'Pilihan Tunggal' / 'Pilihan Jamak' | Sesuai success criteria #1 ROADMAP | |
| Plain: 'Satu Jawaban Benar' / 'Lebih dari Satu Jawaban Benar' | Self-explanatory tanpa istilah teknis | |
| Inggris: 'Single Choice' / 'Multiple Answers' | Match Moodle/Canvas LMS | ✓ |

**User's choice:** Inggris — `MC = "Single Choice"`, `MA = "Multiple Answers"`, Essay tetap.

**Notes:** User open ke English setelah riset menunjukkan international LMS convention pakai pure English. Justified karena pair singular/plural ("Single Choice" / "Multiple Answers") paling clean linguistic. Bahasa hybrid (long form + error msg) tetap Indonesia agar konsisten dengan rest of app.

---

## Per-Surface — Long vs Short Label

| Option | Description | Selected |
|--------|-------------|----------|
| Long full di dropdown, short di badge (Recommended) | Educational di form, hemat di tabel/exam | ✓ |
| Long full di semua tempat | Konsisten — risk wrap di kolom 110px | |
| Short di semua tempat | Tooltip/info icon untuk disambiguation terpisah | |

**User's choice:** Long full di dropdown, short di badge.

**Notes:** Implementation di helper `QuestionTypeLabels.cs`:
- `Long(type)` → "Single Choice (1 jawaban benar)" — dropdown form
- `Short(type)` → "Single Choice" — badge tabel/exam/preview

---

## Source of Truth — Helper vs Hard-code

| Option | Description | Selected |
|--------|-------------|----------|
| Hard-code 6 lokasi (Phase 304 minimal-change spirit) | No new abstraction, atomic per file | |
| Extract C# helper QuestionTypeLabels.cs (Recommended) | Single source untuk view + controller + dropdown | ✓ |
| Hybrid: helper untuk badge saja | Parsial — dropdown tetap inline | |

**Round 1:** User said "analisa dulu" → Claude prepared comparative analysis.

**Round 2 (after analysis):** User chose **Extract C# helper**.

**Notes:** 6 touch points (3 badge view + dropdown + controller flash + import helper) sudah melewati threshold YAGNI. Phase 305 menyentuh semuanya = momen tepat consolidate. Future relabel = 1 file edit. Standardize 3-place badge duplikasi yang ada (existing tech debt).

---

## Doc Cross-Cutting Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Update 8 file HTML guide (.html di wwwroot/documents + docs/) | 14 occurrences, sed-replace context-aware | ✓ |
| Update controller flash error messages | TempData line 4688/4693/4829/4834 | ✓ |
| Update ImportPackageQuestions.cshtml helper text + button labels | Bullet text + button "Template MC/MA" | ✓ |
| Re-screenshot training material + regenerasi PDF panduan | Manual user task | ✓ |

**User's choice:** **All four** (multiSelect).

**Notes:** Doc HTML, controller, dan import helper di-update di Phase 305. PDF + screenshot deferred ke manual task user setelah deployment label baru.

---

## Long Form Syntax (parentheses bahasa)

| Option | Description | Selected |
|--------|-------------|----------|
| Hybrid: 'Single Choice (1 jawaban benar)' / 'Multiple Answers (≥2 jawaban benar)' (Recommended) | Label Inggris, hint Indonesia | ✓ |
| Pure English: '(1 correct answer)' / '(2 or more correct answers)' | Konsisten penuh Inggris | |
| Short only: tanpa parentheses | Disambiguasi via tooltip terpisah | |

**User's choice:** Hybrid Inggris+Indonesia.

**Notes:** Konsisten dengan rest of app (form labels, button, error lain) Bahasa Indonesia. Worker dapat hint Indonesia di parentheses.

---

## Controller Flash Error Style

| Option | Description | Selected |
|--------|-------------|----------|
| Hybrid: 'Single Choice hanya boleh memiliki 1 jawaban benar.' (Recommended) | Tipe Inggris, instruksi Indonesia | ✓ |
| Pure English: 'Single Choice can only have 1 correct answer.' | Full English | |
| Format dengan kata 'tipe' di depan: 'Tipe Single Choice hanya boleh...' | Lebih natural Indo grammar | |

**User's choice:** Hybrid.

**Notes:** Hanya nama tipe Inggris, instruksi tetap Indonesia. Konsisten dengan rest of error/flash di app.

---

## Import Button Label

| Option | Description | Selected |
|--------|-------------|----------|
| Biarkan abbreviation 'MC' / 'MA' (Recommended) | Kompak, button tidak wrap di mobile | |
| Ganti ke full label: 'Template Single Choice' / 'Template Multiple Answers' | Konsisten dengan keputusan #1 | ✓ |
| Hybrid: 'Template Single (MC)' / 'Template Multi (MA)' | Compromise | |

**User's choice:** Full label.

**Notes:** Button: "Template Single Choice" / "Template Multiple Answers" / "Template Essay" / "Template Universal". Planner verifikasi layout tidak wrap di mobile narrow (<375px) — jika wrap, fallback ke responsive `d-block d-md-inline-block` atau truncate.

---

## ExamSummary Badge (current state: no label)

| Option | Description | Selected |
|--------|-------------|----------|
| Biarkan apa adanya — success criteria sudah satisfied (Recommended) | No old label = no rancu | |
| Tambah badge konsisten dengan StartExam | Worker melihat tipe konsisten antara exam & summary | ✓ |

**User's choice:** Tambah badge konsisten dengan StartExam.

**Notes:** Badge baru di kolom "Pertanyaan" di `ExamSummary.cshtml`. Worker setelah selesai ujian melihat ringkasan dengan badge tipe yang sama dengan saat ujian (StartExam). +10 baris scope, manageable.

---

## Worker StartExam — MC Badge Symmetry

| Option | Description | Selected |
|--------|-------------|----------|
| Biarkan: hanya MA & Essay punya badge — MC default (Recommended) | Visual hierarchy clean | |
| Tambah badge MC juga (semua tipe simetris) | Worker selalu tahu tipe, konsisten dgn admin preview | ✓ |

**User's choice:** Tambah badge MC juga — simetris.

**Notes:** Semua 3 tipe (MC/MA/Essay) punya badge di StartExam. Konsisten dengan admin preview yang sudah pakai 3 badge. Trade-off: sedikit nambah visual noise, tapi worker selalu jelas konteksnya.

---

## Claude's Discretion

- Implementasi `switch expression` vs `Dictionary<>` lookup di `QuestionTypeLabels.cs`
- Nama variabel JS (jika ada — tidak ada di Phase 305 scope)
- Order/granularity commit (atomic per file vs grouped commit)
- Bullet text addition opsional di ImportPackageQuestions

## Deferred Ideas

1. PDF panduan regenerasi — manual user task
2. Screenshot training material — manual user task
3. Multi-language i18n full architecture — defer milestone v16+
4. Tooltip transisi/legend "MC = Single Choice (dulu Pilihan Ganda)"
5. Excel import template binary regenerate
6. Rename enum value `MultipleChoice` → `SingleChoice` (DB migration — Out of Scope per REQUIREMENTS)
7. Tooltip / info-icon di dropdown form
8. DB query log audit verifikasi enum unchanged (post-implementation task — planner tambah)
