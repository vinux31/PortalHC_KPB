---
phase: 299-worker-pre-post-test-comparison
verified: 2026-04-07T00:00:00Z
status: human_needed
score: 12/13 must-haves verified
human_verification:
  - test: "Buka /CMP/Assessment sebagai worker yang punya Pre-Post Test session — verifikasi card pair tampil dengan badge dan arrow connector"
    expected: "Pre-Test dan Post-Test muncul sebagai 2 card terhubung dengan border kiri biru, badge Pre-Test/Post-Test, dan arrow icon di tengah"
    why_human: "Verifikasi visual browser tidak dilakukan saat eksekusi — Plan 02 Task 3 checkpoint diselesaikan dengan 'approved' tanpa live browser test (ditangguhkan ke UAT v14.0)"
  - test: "Post-Test card saat Pre-Test belum Completed — verifikasi tampilan disabled"
    expected: "Post card opacity-50 (grayed) dengan tombol disabled 'Selesaikan Pre-Test terlebih dahulu'"
    why_human: "Blocking logic memerlukan state Pre-Test non-Completed yang hanya bisa diverifikasi dengan data real di browser"
  - test: "Post-Test card saat Pre-Test expired — verifikasi pesan 'Pre-Test tidak diselesaikan'"
    expected: "Badge merah 'Pre-Test tidak diselesaikan' tampil di Post card"
    why_human: "Kondisi expired bergantung pada ExamWindowCloseDate yang sudah lewat — butuh data real atau manipulasi waktu"
  - test: "Tab filtering — pair card mengikuti status Post untuk tab placement"
    expected: "Pair card muncul di tab yang sesuai status Post-Test (Open/Upcoming/dll)"
    why_human: "Tab filtering JS diverifikasi secara logika kode (outer wrapper punya data-status dari Post), tapi belum ditest di browser"
  - test: "Riwayat Ujian — badge Pre-Test/Post-Test tampil di kolom Judul"
    expected: "Badge bg-info 'Pre-Test' atau badge bg-primary 'Post-Test' tampil sebelum judul di tabel Riwayat"
    why_human: "Riwayat Ujian query (completedHistory) tidak meng-include AssessmentType dalam SELECT — lihat baris 317-327 CMPController.cs"
  - test: "Results Post-Test — section perbandingan tampil dengan gain score per elemen"
    expected: "Card 'Perbandingan Pre-Post Test' tampil dengan tabel Elemen Kompetensi, Skor Pre, Skor Post, Gain Score"
    why_human: "Memerlukan session PostTest dengan ET scores dan Pre session ter-link di database"
  - test: "Gain score color coding — positif hijau, negatif merah, 0 abu-abu, pending dash"
    expected: "+X.X% hijau, -X.X% merah, 0% abu-abu, dash dengan 'Menunggu penilaian Essay' saat pending"
    why_human: "Memerlukan data ET scores real dengan variasi gain score"
  - test: "Mobile responsive — card pair stack vertikal dan arrow berubah ke panah bawah"
    expected: "Di viewport < 768px: card Pre dan Post stack vertikal, bi-arrow-down-circle-fill tampil, bi-arrow-right-circle-fill tersembunyi"
    why_human: "Perlu resize browser ke mobile viewport"
---

# Phase 299: Worker Pre-Post Test Comparison — Laporan Verifikasi

**Phase Goal:** Worker Pre-Post Test Comparison — Pre-Post pair grouping di Assessment, comparison gain score di Results
**Verified:** 2026-04-07
**Status:** human_needed
**Re-verification:** Tidak — verifikasi awal

## Pencapaian Goal

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Assessment() action mengembalikan PairedGroups dan StandaloneExams via ViewBag | VERIFIED | CMPController.cs baris 302-303: `ViewBag.PairedGroups = pairedGroups; ViewBag.StandaloneExams = standaloneExams;` |
| 2 | Results() action mengembalikan ComparisonData, GainScorePending, HasComparisonSection via ViewBag saat session PostTest | VERIFIED | CMPController.cs baris 2256-2320: default false/null ditetapkan, diisi saat PostTest dengan ET scores |
| 3 | Gain score formula benar termasuk edge case PreScore=100 dan PreScore=0 | VERIFIED | CMPController.cs baris 2303-2305: `preScore >= 100 ? 100 : Math.Round((postScore - preScore) / (100 - preScore) * 100, 1)` |
| 4 | Pre session ownership divalidasi saat query comparison data | VERIFIED | CMPController.cs baris 2271: `preSession != null && preSession.UserId == assessment.UserId` |
| 5 | Null-check pada LinkedSessionId dan preSession sebelum akses | VERIFIED | CMPController.cs baris 2262 `assessment.LinkedSessionId.HasValue` + baris 2271 `preSession != null` |
| 6 | Pre-Post card pair tampil sebagai 2 card terhubung dengan badge dan arrow connector | ? UNCERTAIN | Kode tersedia (Assessment.cshtml baris 107-270): outer wrapper `assessment-card`, inner `assessment-card-item`, badge Pre-Test/Post-Test, arrow connector — belum ditest di browser |
| 7 | Post-Test card disabled saat Pre belum Completed | ? UNCERTAIN | Logika `postBlocked = !preCompleted` ada (baris 118), tombol disabled di baris 239-241 — belum ditest di browser |
| 8 | Post-Test card menampilkan pesan 'Pre-Test tidak diselesaikan' saat Pre expired | ? UNCERTAIN | Logika `postBlockedByExpiry` ada (baris 115-119), badge danger baris 234-235 — belum ditest di browser |
| 9 | Riwayat Ujian menampilkan badge Pre-Test/Post-Test | PARTIAL | View code ada (Assessment.cshtml baris 501-508) tapi `completedHistory` query di controller (baris 317-327) hanya SELECT field Id, Title, Category, CompletedAt, Score, IsPassed, Status — AssessmentType TIDAK di-include |
| 10 | Results Post-Test menampilkan section perbandingan dengan gain score per elemen | ? UNCERTAIN | View code ada dan terhubung (Results.cshtml baris 113-168) — belum ditest dengan data real |
| 11 | Gain score positif hijau, negatif merah, 0 abu-abu, pending dash | ? UNCERTAIN | Kode color-coding ada (Results.cshtml baris 148-160) — belum ditest dengan data real |
| 12 | Inner cards dalam pair wrapper TIDAK punya class assessment-card | VERIFIED | Assessment.cshtml baris 128 dan 199: inner card hanya punya `assessment-card-item`, bukan `assessment-card` |
| 13 | ARIA label pada arrow connector untuk screen reader | VERIFIED | Assessment.cshtml baris 190: `aria-label="Pre-Test menuju Post-Test" role="img"` |

**Score:** 8/13 truths VERIFIED, 4 UNCERTAIN (butuh human), 1 PARTIAL

### Temuan Kritis: Riwayat Ujian Badge (Truth #9 — PARTIAL)

**Masalah:** Kode view untuk badge sudah ada di Assessment.cshtml baris 501-508, tetapi `completedHistory` query di CMPController.cs baris 317-327 tidak meng-include field `AssessmentType` dalam `Select()` projection:

```csharp
// Baris 317-327 — AssessmentType TIDAK ada:
.Select(a => new
{
    a.Id,
    a.Title,
    a.Category,
    a.CompletedAt,
    a.Score,
    a.IsPassed,
    a.Status
    // a.AssessmentType — HILANG
})
```

View code di baris 501 melakukan `(string)item.AssessmentType` pada anonymous object yang tidak punya property `AssessmentType`. Ini akan menghasilkan `null` atau runtime error saat badge dicoba dirender. Badge Pre-Test/Post-Test di Riwayat Ujian **tidak akan tampil** meskipun kode view-nya sudah benar.

### Required Artifacts

| Artifact | Expected | Status | Detail |
|----------|----------|--------|--------|
| `Controllers/CMPController.cs` | Assessment() pair grouping + Results() comparison data | VERIFIED | Pair grouping baris 239-303; comparison data baris 2255-2323 |
| `Views/CMP/Assessment.cshtml` | Pre-Post card pair + Riwayat badge | WIRED | PairedGroups dikonsumsi baris 107-109; StandaloneExams baris 277; badge Riwayat baris 501-508 (view ada, data hilang di controller) |
| `Views/CMP/Results.cshtml` | Comparison section dengan gain score | WIRED | HasComparisonSection guard baris 114; ComparisonData loop baris 132 |

### Key Link Verification

| From | To | Via | Status | Detail |
|------|----|-----|--------|--------|
| CMPController.Assessment() | Views/CMP/Assessment.cshtml | ViewBag.PairedGroups | WIRED | Controller set baris 302, view consume baris 107 |
| CMPController.Assessment() | Views/CMP/Assessment.cshtml | ViewBag.StandaloneExams | WIRED | Controller set baris 303, view consume baris 277 |
| CMPController.Results() | Views/CMP/Results.cshtml | ViewBag.ComparisonData | WIRED | Controller set baris 2318, view consume baris 132 |
| CMPController.Results() | Views/CMP/Results.cshtml | ViewBag.HasComparisonSection | WIRED | Controller set baris 2320, view guard baris 114 |
| CMPController.Assessment() | Views/CMP/Assessment.cshtml | ViewBag.CompletedHistory[AssessmentType] | BROKEN | Query SELECT tidak include AssessmentType — field null saat view akses |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| Assessment.cshtml — pair cards | `ViewBag.PairedGroups` | CMPController query `_context.AssessmentSessions` | Ya — dari DB query + in-memory grouping | FLOWING |
| Assessment.cshtml — standalone | `ViewBag.StandaloneExams` | Derived dari `exams` (paginated DB query) | Ya | FLOWING |
| Assessment.cshtml — Riwayat badge | `item.AssessmentType` | `completedHistory` SELECT projection | Tidak — AssessmentType tidak di-SELECT | DISCONNECTED |
| Results.cshtml — comparison table | `ViewBag.ComparisonData` | ET scores dari `_context.SessionElemenTeknisScores` | Ya — 2 DB queries | FLOWING |

### Behavioral Spot-Checks

Step 7b: SKIPPED — memerlukan running server dan data Pre-Post Test di database.

### Requirements Coverage

| Requirement | Plan | Deskripsi | Status | Evidence |
|-------------|------|-----------|--------|----------|
| WKPPT-01 | 299-01, 299-02 | Daftar assessment menampilkan Pre dan Post sebagai 2 card terhubung | UNCERTAIN | Kode ada dan terhubung — butuh browser test |
| WKPPT-02 | 299-02 | Post-Test tidak bisa dimulai sebelum Pre-Test Completed | UNCERTAIN | Blocking logic ada di view — butuh browser test |
| WKPPT-03 | 299-02 | Post-Test dapat dimulai setelah Pre-Test Completed dan jadwal tiba | UNCERTAIN | Logic ada — butuh browser test |
| WKPPT-04 | 299-01, 299-02 | Halaman perbandingan Pre vs Post dengan skor side-by-side | UNCERTAIN | Kode ada dan terhubung — butuh data real |
| WKPPT-05 | 299-01 | Gain score formula: (Post - Pre) / (100 - Pre) x 100 | SATISFIED | CMPController.cs baris 2305: formula persis sesuai |
| WKPPT-06 | 299-01 | PreScore = 100 → Gain = 100 | SATISFIED | CMPController.cs baris 2303: `preScore >= 100 ? 100` |
| WKPPT-07 | 299-01, 299-02 | Gain score per elemen kompetensi | UNCERTAIN | ET scores per-elemen dihitung, view loop per elemen — butuh data real |

**Catatan orphaned requirements:** Semua 7 requirement ID (WKPPT-01 sampai WKPPT-07) tercakup di Plan 299-01 dan 299-02. Tidak ada requirement orphaned.

### Anti-Patterns Found

| File | Detail | Severity | Impact |
|------|--------|----------|--------|
| Controllers/CMPController.cs baris 317-327 | `completedHistory` SELECT projection tidak include `AssessmentType` — view code mengakses property yang tidak ada | BLOCKER | Badge Pre-Test/Post-Test di Riwayat Ujian tidak tampil (WKPPT-01 partial) |

### Human Verification Required

#### 1. Visual Pre-Post Card Pair

**Test:** Login sebagai worker yang punya Pre-Post Test session, buka /CMP/Assessment
**Expected:** Pre-Test dan Post-Test muncul sebagai 2 card terhubung dengan border kiri biru, badge "Pre-Test" (bg-info) / "Post-Test" (bg-primary), dan arrow icon di tengah
**Why human:** Visual layout tidak bisa diverifikasi dari kode statik; Plan 02 Task 3 checkpoint diselesaikan tanpa live browser test

#### 2. Post-Test Blocking Logic

**Test:** Akses Assessment dengan session PostTest yang Pre-nya belum Completed
**Expected:** Post card opacity-50 (grayed) dengan tombol disabled "Selesaikan Pre-Test terlebih dahulu"
**Why human:** Memerlukan data state Pre non-Completed di database

#### 3. Pre Expired — Pesan Error

**Test:** Akses Assessment dengan Pre-Test yang sudah expired (ExamWindowCloseDate sudah lewat) dan belum Completed
**Expected:** Badge merah "Pre-Test tidak diselesaikan" di Post card
**Why human:** Memerlukan data dengan ExamWindowCloseDate di masa lalu

#### 4. Riwayat Ujian Badge (BUG DITEMUKAN — perlu fix controller)

**Test:** Scroll ke tabel Riwayat Ujian di halaman Assessment
**Expected:** Badge "Pre-Test" atau "Post-Test" tampil sebelum judul di kolom Judul
**Why human:** Bug ditemukan di CMPController.cs — `completedHistory` SELECT projection tidak include `AssessmentType`. **PERLU FIX: tambahkan `a.AssessmentType` ke SELECT di baris 317-327 CMPController.cs**

#### 5. Tab Filtering untuk Pair Card

**Test:** Klik tab "Upcoming" / "Open" di halaman Assessment
**Expected:** Pair card muncul/tersembunyi sesuai status Post-Test
**Why human:** Tab filtering JS perlu ditest di browser dengan pair cards nyata

#### 6. Comparison Section di Results

**Test:** Buka halaman Results dari Post-Test yang sudah Completed dan punya ET scores
**Expected:** Card "Perbandingan Pre-Post Test" tampil dengan tabel Elemen Kompetensi, Skor Pre, Skor Post, Gain Score
**Why human:** Memerlukan session PostTest dengan ET scores di database

#### 7. Gain Score Color Coding

**Test:** Lihat tabel Gain Score di Results
**Expected:** "+X.X%" hijau, "-X.X%" merah, "0%" abu-abu, "—" dengan "Menunggu penilaian Essay" saat pending
**Why human:** Memerlukan data dengan variasi gain score (positif, negatif, nol, pending)

#### 8. Mobile Responsive

**Test:** Resize browser ke < 768px di halaman Assessment dengan pair cards
**Expected:** Card Pre dan Post stack vertikal, panah bawah tampil (bi-arrow-down-circle-fill), panah kanan tersembunyi
**Why human:** Responsive layout perlu ditest di mobile viewport

### Gaps Summary

**1 gap blocker ditemukan (Riwayat Ujian AssessmentType):**

CMPController.cs — `completedHistory` SELECT projection tidak include field `AssessmentType`. View di Assessment.cshtml baris 501-508 sudah ada kode badge yang benar, tetapi data yang dibutuhkan (`item.AssessmentType`) tidak pernah di-load dari database. Fix sederhana: tambahkan `a.AssessmentType` ke SELECT projection di baris 317-327.

**8 item memerlukan human verification** (visual + browser test) karena Plan 02 Task 3 checkpoint diselesaikan tanpa live browser test sesungguhnya.

---

_Verified: 2026-04-07_
_Verifier: Claude (gsd-verifier)_
