---
phase: 350-team-view-search-scope-export-parity
reviewed: 2026-06-05T00:00:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - Services/WorkerDataService.cs
  - Controllers/CMPController.cs
  - Views/CMP/RecordsTeam.cshtml
  - HcPortal.Tests/WorkerDataServiceSearchTests.cs
  - tests/sql/cmp350-seed.sql
  - tests/e2e/cmp-records-350.spec.ts
findings:
  critical: 0
  warning: 0
  info: 2
  total: 2
status: issues_found
---

# Phase 350: Code Review Report

**Reviewed:** 2026-06-05
**Depth:** standard
**Files Reviewed:** 6
**Status:** issues_found (2 Info only — no Critical/Warning)

## Summary

Review terbatas pada diff Phase 350 (CMP/Records Team View search scope + export parity):

1. `WorkerDataService.cs` — penambahan `assessmentMatch` di blok post-load search (SF-01, ~:412-415) + projeksi `a.Category → Kategori` di `GetAllWorkersHistory` (SF-06, :156 dan :199).
2. `CMPController.cs` — narrow `if (!string.IsNullOrEmpty(category))` Category pada `ExportRecordsTeamAssessment` setelah `var filtered = assessmentRows;` (:680-689).
3. `RecordsTeam.cshtml` — 2 string copy (placeholder :96 + display text option "Judul Kegiatan" :102).

Diff inti sehat. Logika SF-01 `assessmentMatch` benar mirror pola Category-union yang ada (:378-380) namun match `Title` dengan `.Contains` sesuai spec, dan invariant D-07 (badge count per-worker tetap utuh) terjaga karena filter hanya membuang worker dari `workerList`, bukan memodifikasi koleksi `AssessmentSessions`/`TrainingRecords`. Null-guard `!string.IsNullOrEmpty(a.Title)` benar (walau `AssessmentSession.Title` non-nullable default `""`, guard tetap defensif & konsisten gaya).

SF-06 narrow di controller benar: `currentRows` membawa `Kategori = a.Category`, `archivedRows` membiarkan `Kategori` null (memang tidak ada kolom Category di `AssessmentAttemptHistory`), sehingga narrow case-insensitive otomatis menggugurkan archived rows — sesuai komentar D-07 dan on-screen worker-visibility. Keputusan menaruh narrow di controller (bukan service) konsisten dengan invariant terkunci agar tidak meregresi caller no-arg `GetAllWorkersHistory`.

Test coverage proporsional: 5 [Fact] baru menutup assessment-title match (Training & Keduanya), invariant badge-count D-07, dan jalur export. Seed SQL idempotent (wipe-and-insert prefix `[PENDING350]`) + pre-condition guard. Spec e2e mengikuti SEED_WORKFLOW (backup → seed → restore afterAll → Layer 4 assert bersih).

Tidak ada isu keamanan, bug korektnis, atau pelanggaran invariant terkunci pada diff Phase 350. Dua catatan Info berikut bersifat konsistensi/observasi, bukan defect.

## Info

### IN-01: SF-06 Category-narrow ada di `ExportRecordsTeamAssessment` tapi tidak di `ExportRecordsTeamTraining` (asimetri by-design — worth a one-line note)

**File:** `Controllers/CMPController.cs:680-689` (vs `:734-741`)
**Issue:** `ExportRecordsTeamAssessment` melakukan narrow Category in-memory (`filtered = assessmentRows.Where(...)`) karena `GetAllWorkersHistory` dipanggil dengan `category: null` untuk cabang assessment. Sebaliknya `ExportRecordsTeamTraining` meneruskan `category: category` langsung ke service (SQL push-down) sehingga tidak butuh narrow ulang. Asimetri ini benar dan disengaja (assessment narrow di-current-session-only + drop archived; training di-push-down ke SQL), namun bagi pembaca berikutnya perbedaan dua endpoint yang tampak kembar bisa membingungkan.
**Fix:** Komentar di `ExportRecordsTeamAssessment` sudah menjelaskan alasan drop archived. Pertimbangkan tambah satu baris di `ExportRecordsTeamTraining` (mis. `// Category di-push-down via GetAllWorkersHistory(category:...) — beda dari Assessment yang narrow in-memory`) agar simetri keputusan terdokumentasi di kedua sisi. Opsional, non-blocking.

### IN-02: `assessmentMatch`/`trainingMatch` memakai `a.Title.ToLower().Contains()` — sadar-kultur (acceptable, konsisten dgn kode sekitar)

**File:** `Services/WorkerDataService.cs:410-415`
**Issue:** Pencocokan substring memakai `.ToLower().Contains(searchLower)` (ordinal-ish via lowercase) — sama persis dengan pola `trainingMatch` dan pre-narrow Nama (:262-265) yang sudah ada, jadi konsisten. Untuk data ber-aksara non-ASCII Turkish-i dsb. `ToLower()` invariant-budaya bisa berbeda dari `StringComparison.OrdinalIgnoreCase` yang dipakai di blok Category-narrow (:359, :379). Dalam konteks judul assessment/training berbahasa Indonesia, dampaknya nihil.
**Fix:** Tidak perlu diubah untuk Phase 350 (konsistensi lokal > keseragaman global di sini, dan mengubahnya berisiko meregresi pencocokan Nama/Training existing). Catat saja bila kelak menyatukan strategi string-compare di seluruh search Team View.

---

_Reviewed: 2026-06-05_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
