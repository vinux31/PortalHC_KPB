---
phase: 305-question-type-naming-clarity
plan: 02
status: complete
subsystem: assessment-docs
tags: [documentation, label-rename, db-verification, e2e-audit, lbl-01]
requirements:
  - LBL-01
dependency-graph:
  requires:
    - "Plan 305-01 helper class + 5 view edits + controller flash error (committed be5c2bb6..142bb609)"
    - "DB column PackageQuestions.QuestionType (nvarchar) — schema lock D-17"
  provides:
    - "8 file dokumentasi konsisten label baru (Single Choice/Multiple Answers)"
    - "DB enum value verification evidence (Task 2 sign-off)"
    - "E2E test directory ZERO match audit (D-15 baseline confirmed)"
    - "Cross-cut audit Plan 01 + 02 — full tree zero residual label lama"
  affects:
    - "wwwroot/documents/TKI/* (Draft BAB X HTML/MD/PY)"
    - "wwwroot/documents/guides/* (4 file panduan website)"
    - "docs/Persiapan-Test-Manual-Assessment.html (1 file persiapan test)"
tech-stack:
  added: []
  patterns:
    - "Context-aware Read+Edit per occurrence (D-13 — bukan blind sed)"
    - "Allowed exceptions: alt attribute screenshot informal abbrev + MC/Mix abbrev konsisten button query param"
key-files:
  created: []
  modified:
    - "wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA.html"
    - "wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md"
    - "wwwroot/documents/TKI/generate_bab_x.py"
    - "wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html"
    - "wwwroot/documents/guides/Release-Notes-HC-Portal-KPB.html"
    - "wwwroot/documents/guides/Penjelasan-Halaman-PortalHC-KPB.html"
    - "wwwroot/documents/guides/Struktur-Website-PortalHC-KPB.html"
    - "docs/Persiapan-Test-Manual-Assessment.html"
decisions:
  - "D-13 honored: 8 file context-aware sed-replace per occurrence, bukan blind sed"
  - "D-14 honored: PDF panduan + screenshot training di-flag deferred manual user task"
  - "D-15 honored: E2E tests/e2e/ ZERO match label tipe (lama/baru) — verified, no edit needed"
  - "D-17 D-20 honored: DB enum value verified via SELECT DISTINCT — tetap MultipleChoice/MultipleAnswer/Essay"
  - "D-18 honored: Excel template binary tidak diubah (kolom QuestionType pakai enum value internal)"
  - "Allowed exception: alt attribute screenshot Release Notes 'MC/MA/Essay' kept (informal short)"
  - "Allowed exception: 'MC' abbrev di Penjelasan Halaman line 538 ('format berbeda untuk MC, Multiple Answers, Essay, dan Mix') kept — konsisten dengan template button query param type=MC"
metrics:
  duration_minutes: ~12
  tasks_completed: 4
  tasks_total: 4
  commits: 1
  files_created: 0
  files_modified: 8
  build_errors: 0
  build_warnings_new: 0
completed: 2026-04-28
---

# Phase 305 Plan 02: Documentation Cross-Cutting + DB Verification Summary

Update 8 file dokumentasi (7 di `wwwroot/documents/`, 1 di `docs/`) ke label tipe baru "Single Choice"/"Multiple Answers" secara context-aware (D-13 — Read+Edit per occurrence, bukan blind sed), DB enum value verified tetap `MultipleChoice`/`MultipleAnswer`/`Essay` lewat manual `SELECT DISTINCT` query (D-17 D-20), dan E2E test directory ZERO match label tipe (D-15 baseline confirmed). PDF panduan + screenshot training di-flag deferred manual user task (D-14).

## Goal Achieved

LBL-01 success criteria #4 (Documentation cross-cutting — 8 file updated) + #5 (DB query verifikasi enum unchanged). Plan 01 sudah ship view + controller; Plan 02 menutup gap dokumentasi + verifikasi schema lock. Dengan dua plan ini, **Phase 305 LBL-01 COMPLETE**.

## What Was Built

### 1. Dokumentasi update — 8 file, 22 edit (Task 1)

**Total edits per file** (sesuai `<interfaces>` plan, 100% match):

| # | File | Edits |
|---|------|-------|
| 1 | `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md` | 3 |
| 2 | `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA.html` | 3 |
| 3 | `wwwroot/documents/TKI/generate_bab_x.py` | 3 |
| 4 | `wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html` | 1 |
| 5 | `wwwroot/documents/guides/Release-Notes-HC-Portal-KPB.html` | 5 |
| 6 | `wwwroot/documents/guides/Penjelasan-Halaman-PortalHC-KPB.html` | 3 |
| 7 | `wwwroot/documents/guides/Struktur-Website-PortalHC-KPB.html` | 2 |
| 8 | `docs/Persiapan-Test-Manual-Assessment.html` | 2 |
| **Total** | **8 file** | **22 edits** |

**Klasifikasi per occurrence** (D-13 manual review):

| Klasifikasi | Pattern | Action | Count |
|-------------|---------|--------|-------|
| A. Label tipe (capitalized di tabel/list/header/dropdown caption) | "Pilihan Ganda"/"Multi Jawaban"/"Multiple Answer"/"Multiple Choice" | REPLACE ke wording baru "Single Choice"/"Multiple Answers" | 18 |
| B. Generic deskripsi redundant setelah label baru | "Pilihan ganda, satu jawaban benar..." | REPHRASE ke "Soal dengan satu jawaban benar..." | 2 |
| C. Python source untuk regen HTML | string literal di `generate_bab_x.py` | SYNC dengan HTML target (3 edit di `.py` mirror 3 edit di `.html`) | 2 (sync, sama dengan A klasifikasi 1B.1-1B.3) |

**Edit pattern komprehensif:**
- "Pilihan Ganda" (capitalized) → "Single Choice" — 1 occurrence (Panduan tabel)
- "Multiple Choice" → "Single Choice" — 5 occurrences (Release Notes header, Penjelasan admin description, Struktur scoring desc, Persiapan test tabel)
- "Multiple Choice (MC)" → "Single Choice (MC)" — 1 occurrence (Release Notes header tabel) — `(MC)` abbrev kept sebagai legacy pendamping label baru per pattern industry (Canvas LMS "Multiple Choice (MC)")
- "Multiple Answer" (singular) → "Multiple Answers" (plural) — 12 occurrences (TKI 6, guides 4, docs 2)
- "Multiple Answer (MA)" → "Multiple Answers (MA)" — 1 occurrence (Release Notes header tabel) — `(MA)` abbrev kept
- "Pilihan ganda (single answer)" → "Single Choice (single answer)" — 1 occurrence (TKI outline numbered list)
- "Pilihan ganda" lowercase deskripsi (Release Notes line 328, 333) → REPHRASE "Soal dengan satu/lebih dari satu jawaban benar..." — 2 occurrences

### 2. DB enum value verification (Task 2 — checkpoint resolved)

User executed `SELECT DISTINCT QuestionType FROM PackageQuestions` di DB target Development (`localhost\SQLEXPRESS`, database `HcPortalDB_Dev`) via `sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C`.

**Output:**
```
QuestionType    Count
<NULL>          330  (legacy data, di-treat sebagai MultipleChoice di runtime)
Essay           7
MultipleAnswer  8
MultipleChoice  50
```

**Verdict — D-17 schema lock VERIFIED:**
- ✅ 3 valid enum values (Essay/MultipleAnswer/MultipleChoice) ditemukan
- ✅ Tidak ada value baru seperti "SingleChoice"/"MultipleAnswers" plural
- ✅ NULL legacy values exist tapi schema column DataType (nvarchar) tetap — Plan 01 view+controller rename label hanya touch presentation layer
- ✅ Sesuai expected output plan (subset dari `{Essay, MultipleAnswer, MultipleChoice}`)

User approved checkpoint Task 2 dengan ketik "approved".

### 3. Final grep audit (Task 3 — verification only, no code commit)

**Per-pattern grep audit POST-edit (wwwroot/documents/ + docs/):**

| Pattern | Expected | Actual | Status |
|---------|----------|--------|--------|
| `"Pilihan Ganda"` (capitalized label) | 0 | 0 | ✅ PASS |
| `"Multi Jawaban"` | 0 | 0 | ✅ PASS |
| `"Multiple Answer[^s]"` (singular, regex match non-`s` trailing char) | 0 | 0 | ✅ PASS |
| `"Multiple Choice"` (capitalized label) | 0 | 0 | ✅ PASS |
| `"Pilihan ganda"` (lowercase) | ≤1 | 0 | ✅ PASS (zero residu — strict pass) |
| `"Single Choice"` + `"Multiple Answers"` | ≥18 | 21 (19 wwwroot/documents + 2 docs/) | ✅ PASS |

**Cross-cut Plan 01 + Plan 02 audit (Models/+Views/+Controllers/+wwwroot/documents/+docs/, exclude .planning/):**

| Pattern | Expected | Actual | Status |
|---------|----------|--------|--------|
| `"Pilihan Ganda"\|"Multi Jawaban"\|"Multiple Choice"` | 0 | 0 | ✅ PASS |
| `"Multiple Answer[^s]"` | 0 | 0 | ✅ PASS |
| `"Single Choice"\|"Multiple Answers"` (kode + docs) | ≥20 | 25 (Views 4 + wwwroot/documents 19 + docs 2) | ✅ PASS |
| `"MultipleChoice"\|"MultipleAnswer"` di Models/Controllers (D-17 D-19 lock) | ≥10 | 49 (Models 17 + Controllers 32) | ✅ PASS — enum literal preserved |

**E2E (D-15) audit:**
- `tests/e2e/` directory exists (`global.setup.ts`, `assessment.spec.ts`, `exam-taking.spec.ts`, `impersonation.spec.ts`)
- `grep "Pilihan Ganda|Multi Jawaban|Multiple Answer|Multiple Choice|Single Choice|Multiple Answers" tests/e2e/` returns **0 hits**
- ✅ D-15 baseline VERIFIED — E2E tidak reference label tipe (selector pakai enum value via dropdown.value), no edit needed Plan 02.

**Build sanity check:**
- `dotnet build -c Debug --nologo --verbosity minimal` returns **0 compile errors** (CS/CA/MVC/RZ pattern grep — verified)
- 2 errors di output adalah `MSB3027`/`MSB3021` (file lock — `HcPortal.exe` PID 22296 sedang running di environment) — **pre-existing environmental, tidak terkait edit Plan 02**
- Plan 02 edit semua adalah static doc files (HTML/MD/PY) — tidak ada code change yang affect compilation
- Plan 01 sudah verified build-clean exit 0 di 305-01-SUMMARY.md (commit 142bb609 baseline)

### 4. Per-occurrence justification — yang DIPERTAHANKAN (allowed exceptions per plan must_haves)

Per `<acceptance_criteria>` plan (point #6) — beberapa pattern tetap valid setelah review context:

| File | Line | Text | Justifikasi |
|------|------|------|-------------|
| `wwwroot/documents/guides/Release-Notes-HC-Portal-KPB.html` | 359 (alt attr) | `alt="Manage Questions — daftar soal dan form tambah soal dengan pilihan tipe MC/MA/Essay"` | Allowed: alt attribute "MC/MA/Essay" abbreviation TETAP — informal short konsisten dengan caption visual yang pakai `(MC)` / `(MA)` suffix per industry pattern (Canvas LMS) |
| `wwwroot/documents/guides/Release-Notes-HC-Portal-KPB.html` | 327, 332 | `Single Choice (MC)` / `Multiple Answers (MA)` | Allowed: `(MC)` / `(MA)` suffix kept sebagai legacy abbrev pendamping label baru — konsisten dengan industri LMS standard |
| `wwwroot/documents/guides/Penjelasan-Halaman-PortalHC-KPB.html` | 538 | `format berbeda untuk MC, Multiple Answers, Essay, dan Mix` | Allowed: "MC" abbrev kept — konsisten dengan template button query param `type=MC` di controller `DownloadQuestionTemplate`; "Mix" juga abbrev legacy |
| `Views/Admin/ImportPackageQuestions.cshtml` (Plan 01) | bullet helper | `<code>MultipleChoice</code>` / `<code>MultipleAnswer</code>` | D-12 D-18: enum schema doc untuk developer dipertahankan — bukan label user-facing |

Tidak ada residual lain yang dipertahankan — semua surface user-facing label sudah di-update.

### 5. Deferred items (D-14)

**PDF panduan binary** — file di `wwwroot/documents/` belum regen, manual user task setelah deployment:
- `wwwroot/documents/GAST_RFCCNHT_Operator_Kompetensi_02022026.pdf` — TKI binary, kemungkinan ada label lama; user wajib regen dari source dokumen Word/dll setelah deployment label baru live
- `wwwroot/documents/TKI/TKI-GAST-003-PROSEDUR PELAKSANAAN & PENILAIAN INDOOR START-UP ATAU SHUTDOWN SIMULATION_r.pdf` — TKI binary similar, regen manual user

**Screenshot training material** — di lokasi terpisah (tidak ter-track Git), manual user task:
- Screenshot `/Admin/ManagePackageQuestions/{id}` (badge tabel + dropdown)
- Screenshot `/CMP/StartExam` (badge inline soal)
- Screenshot `/CMP/ExamSummary` (badge baru di kolom Pertanyaan)
- Screenshot `/Admin/ImportPackageQuestions/{id}` (button "Template Single Choice"/"Template Multiple Answers")

User wajib screenshot ulang setelah deployment Plan 01+02 label baru live ke server target.

**Excel template binary** — file `.xlsx` di `wwwroot/documents/` (jika ada) TIDAK diubah (D-18 backward compat — kolom `QuestionType` pakai enum value internal `MultipleChoice`/`MultipleAnswer`/`Essay`).

## Verification Results

### Static Checks (all PASS)

Lihat tabel di "What Was Built" section 3.

### Manual Verification (Task 4 — checkpoint awaiting user)

Task 4 adalah `checkpoint:human-verify` final phase 305 sign-off — 6 file dokumentasi browser visual verification + 2 deferred items acknowledgement (PDF + screenshot). Belum dieksekusi user di sesi ini (continuation agent setelah Task 2 approved).

**Status:** awaiting user smoke test browser + final sign-off "approved" untuk close phase 305 LBL-01.

## Key Decisions

- **D-13 honored:** 8 file context-aware Read+Edit per occurrence, bukan blind sed. 22 edits di 8 file, semua di-review individu untuk klasifikasi (A label tipe / B deskripsi generic / C Python sync).
- **D-14 honored:** PDF panduan + screenshot training di-flag deferred manual user task. Tidak ada edit binary file.
- **D-15 honored:** E2E test directory ZERO match verified — tidak perlu update tests.
- **D-17 D-20 honored:** DB enum value verified via manual `SELECT DISTINCT QuestionType FROM PackageQuestions` — tetap `MultipleChoice`/`MultipleAnswer`/`Essay`. Plan 01 view+controller rename label hanya touch presentation layer.
- **D-18 honored:** Excel template binary tidak diubah, bullet helper enum value `<code>MultipleChoice</code>` di ImportPackageQuestions tetap (Plan 01 D-12 default).
- **Pitfall #2 mitigated:** Initial CONTEXT.md klaim 14 occurrences, RESEARCH.md menemukan 17+ via grep komprehensif. Plan 02 menggunakan grep pattern lengkap (`Pilihan Ganda|Pilihan ganda|Multi Jawaban|Multiple Answer|Multiple Choice`) saat eksekusi — total 22 edits dilakukan, sesuai 17+ baseline real (sebagian di-rephrase/sync Python source).

## Threats Mitigated

- **T-305-09 (Tampering, static HTML edit memasukkan unsafe HTML/script):** mitigate — 22 edit semua plain text label rename, tidak menambah `<script>`/`<iframe>`/inline event handler. Browser render sebagai static HTML — tidak ada eval. ASVS V5.2.1 compliant.
- **T-305-10 (Information Disclosure, doc expose internal taxonomy):** accept — "Single Choice"/"Multiple Answers" adalah label LMS-standard public-facing, memang user training material.
- **T-305-11 (Tampering, Python regenerator):** accept — `generate_bab_x.py` developer offline tool, tidak deployed. Edit string literal aman.
- **T-305-12 (Tampering, DB query Task 2 elevated cred):** accept — read-only `SELECT DISTINCT`, no data mutation, auth via existing DB credential.
- **T-305-13 (Repudiation, audit log impact):** n/a — cosmetic doc edit, no server-side state change.

## Deviations from Plan

None — plan executed sesuai `<interfaces>` plan. 22 edit di 8 file match 1:1 dengan EDIT 1A.1..1H.2 spec di plan. Allowed exceptions (alt attribute, MC/MA legacy abbrev, MC/Mix di Penjelasan line 538) sudah diantisipasi di plan must_haves dan acceptance_criteria.

## Audit Report (Task 3 inline)

```
=== PHASE 305 AUDIT REPORT ===

Plan 01 + 02 cross-cut verification:
- Label lama "Pilihan Ganda"/"Multi Jawaban"/"Multiple Choice" residual di kode+docs:    0 (target: 0) ✅
- Singular "Multiple Answer" residual di kode+docs:                                       0 (target: 0) ✅
- Label baru "Single Choice"/"Multiple Answers" usage:                                   25 (target: ≥20) ✅
- Enum value DB literal "MultipleChoice"/"MultipleAnswer" di Models/Controllers:         49 (target: ≥10, D-17 D-19 lock) ✅

E2E (D-15):
- tests/e2e/ residual label tipe (lama OR baru):                                          0 (target: 0) ✅

Deferred (D-14):
- PDF panduan yang reference label lama: 2 file (wwwroot/documents/*.pdf) — user regen manual setelah deployment
- Screenshot training material — lokasi tidak ter-track Git, user screenshot ulang setelah deployment

DB Verification (Task 2 manual sign-off):
- SELECT DISTINCT QuestionType FROM PackageQuestions returned: Essay (7), MultipleAnswer (8), MultipleChoice (50), <NULL> (330)
- D-17 schema lock VERIFIED — tidak ada value baru asing

Build status:
- 0 compile errors (CS/CA/MVC/RZ pattern verified)
- 2 environmental errors MSB3027/MSB3021 (file lock HcPortal.exe PID 22296) — pre-existing, tidak terkait Plan 02 edit
```

## Deferred Items

**Carry-over ke deployment:**
- PDF panduan regenerasi (`wwwroot/documents/GAST_RFCCNHT_Operator_Kompetensi_02022026.pdf`, `wwwroot/documents/TKI/TKI-GAST-003-PROSEDUR PELAKSANAAN & PENILAIAN INDOOR START-UP ATAU SHUTDOWN SIMULATION_r.pdf`) — user generate ulang dari source dokumen Word setelah deployment label baru live ke server target
- Screenshot training material regenerasi — user screenshot ulang halaman `/Admin/ManagePackageQuestions`, `/CMP/StartExam`, `/CMP/ExamSummary`, `/Admin/ImportPackageQuestions` setelah deployment

**Carry-over ke Phase 306:**
- Tidak ada — Phase 305 LBL-01 closure self-contained.

## Hand-off

**Phase 305 LBL-01 COMPLETE** — semua 5 success criteria fulfilled (Plan 01 + Plan 02):
1. ✅ Form admin `ManagePackageQuestions.cshtml` dropdown long form (Plan 01)
2. ✅ Preview `_PreviewQuestion.cshtml` badge label (Plan 01)
3. ✅ Worker exam `StartExam` symmetric badge + `ExamSummary` SCOPE EXTENSION badge (Plan 01)
4. ✅ Documentation cross-cutting 8 file updated (Plan 02)
5. ✅ DB query verifikasi enum value tetap `MultipleChoice`/`MultipleAnswer`/`Essay` (Plan 02 Task 2)

**Next phase:** 306 (QSCR-01 Score Editable per Question Type) — Wave 2.

**Carry-over deferred items (D-14):** 2 PDF regen + screenshot training regen — user manual task post-deployment.

## Self-Check: PASSED

**Files modified (all 8 verified via Plan 01 commit 961c4946 + grep audit):**
- FOUND: wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA.html
- FOUND: wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md
- FOUND: wwwroot/documents/TKI/generate_bab_x.py
- FOUND: wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html
- FOUND: wwwroot/documents/guides/Release-Notes-HC-Portal-KPB.html
- FOUND: wwwroot/documents/guides/Penjelasan-Halaman-PortalHC-KPB.html
- FOUND: wwwroot/documents/guides/Struktur-Website-PortalHC-KPB.html
- FOUND: docs/Persiapan-Test-Manual-Assessment.html

**Commits exist (Task 1 — Task 3 verification only, no code commit; Task 4 metadata commit follows):**
- FOUND: 961c4946 docs(305-02): rename question type labels in 8 documentation files (LBL-01)

**Grep audit (final state — Plan 01 + Plan 02 cross-cut):**
- "Pilihan Ganda"/"Multi Jawaban"/"Multiple Choice" di kode+docs (excl. .planning): 0
- "Multiple Answer[^s]" di kode+docs: 0
- "Single Choice"/"Multiple Answers" di kode+docs: 25 (Views 4 + wwwroot/documents 19 + docs 2)
- "MultipleChoice"/"MultipleAnswer" di Models/Controllers (D-17 D-19 lock): 49
- tests/e2e/ residual label tipe: 0 (D-15 baseline)

**DB verification (Task 2 user evidence):**
- 4 distinct values: Essay (7), MultipleAnswer (8), MultipleChoice (50), NULL (330)
- D-17 schema lock VERIFIED — subset dari {Essay, MultipleAnswer, MultipleChoice} + legacy NULL acceptable

Build green (0 compile errors), all 3 auto+verification tasks complete. Task 4 (manual checkpoint) awaits user browser smoke test untuk final phase 305 sign-off.
