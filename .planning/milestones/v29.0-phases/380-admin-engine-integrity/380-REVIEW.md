---
phase: 380-admin-engine-integrity
reviewed: 2026-06-14T00:00:00Z
depth: standard
files_reviewed: 8
files_reviewed_list:
  - Helpers/ShuffleEngine.cs
  - Controllers/CMPController.cs
  - Controllers/AssessmentAdminController.cs
  - HcPortal.Tests/ShuffleEngineTests.cs
  - HcPortal.Tests/AddExtraTimeAuthTests.cs
  - HcPortal.Tests/VerifyTokenTests.cs
  - HcPortal.Tests/AddExtraTimeCapTests.cs
  - tests/e2e/exam-taking.spec.ts
findings:
  critical: 0
  warning: 0
  info: 3
  total: 3
status: issues_found
---

# Phase 380: Code Review Report

**Reviewed:** 2026-06-14
**Depth:** standard
**Files Reviewed:** 8 (scoped to `git diff ad26f929..HEAD` only)
**Status:** issues_found (0 critical / 0 warning / 3 info)

## Summary

Review difokuskan HANYA pada diff Phase 380 (404 insertion / 4 deletion lintas 8 file),
mengabaikan kode pre-existing di luar hunk sesuai batas scope. Tiga requirement diverifikasi:

- **WSE-01** — Filter paket kosong di ON-path `BuildCrossPackageAssignment` (ShuffleEngine
  `:99-103`) **benar mirror** OFF-path (`:53-57`): setelah filter, `Count==1` collapse ke
  single-package shuffle dan `K = packages.Min(...)` tak pernah 0. Guard `StartExam`
  (CMPController `:973-990`) **ditempatkan dengan benar** SEBELUM semua write
  (`StartedAt`/`Status` di `:992`, SignalR di `:1000`, assignment di `:1039`). Kasus
  "1 kosong di antara banyak" **tidak** terblokir palsu (`anyWithQuestions=true`); kasus
  "zero paket" tetap ditangani else `:1228`. Logika korek.
- **WSE-02** — `AccessTokenMatches` (`:850`) null-safe both-sides `Trim().ToUpper()`,
  null stored → tak match input non-kosong. `VerifyToken` (`:882`) merutekan via helper.
  `EditAssessment` Pre/Post (`:1820`) menulis uppercase + preserve fallback-to-existing
  (tidak menghapus token saat model kosong). Konsisten dengan single-mode (`:2020`) dan
  `CreateAssessment` (`:1107`). Korek.
- **WSE-03** — `AddExtraTime` digerbangi `[Authorize(Roles="Admin, HC")]` (string persis
  sibling). `ExtraTimeWithinCap` boundary benar (`30+30<=60` allow; `40+30>60` reject).
  Reject-whole-batch **atomic**: loop validasi penuh `:6912-6918` return sebelum loop
  akumulasi `:6920` dan `SaveChangesAsync` `:6924` — tidak ada partial-apply. Sesuai desain.

Tidak ada bug korektnes maupun lubang keamanan baru. Tiga catatan INFO bersifat
konsistensi/kebersihan, bukan defect. Deviasi yang dideklarasikan (dua helper static murni
untuk unit test) sesuai konteks dan **tidak** dihitung sebagai temuan.

## Info

### IN-01: Query preCheck paket-kosong menduplikasi query siblingSessionIds (3 roundtrip ekstra saat first-load)

**File:** `Controllers/CMPController.cs:975-984`
**Issue:** Guard WSE-01 menjalankan query `AssessmentSessions` (preCheckSiblingIds) yang
identik dengan query `siblingSessionIds` di `:1012-1017` (predikat
`Title + Category + Schedule.Date` sama persis), lalu dua `AnyAsync` terhadap
`AssessmentPackages`. Karena guard hanya berjalan saat `justStarted`, total 3 DB roundtrip
tambahan hanya pada entri pertama — bukan bug korektnes, dan performa di luar scope v1.
Dicatat agar terlihat bila ada refactor kelak.
**Fix (opsional):** Pindahkan komputasi `siblingSessionIds` ke ATAS guard lalu gunakan
ulang untuk preCheck, mengganti satu query roundtrip:
```csharp
// hitung siblingSessionIds sekali, sebelum guard
bool anyPackages = await _context.AssessmentPackages
    .AnyAsync(p => siblingSessionIds.Contains(p.AssessmentSessionId));
bool anyWithQuestions = anyPackages && await _context.AssessmentPackages
    .AnyAsync(p => siblingSessionIds.Contains(p.AssessmentSessionId) && p.Questions.Any());
```
Catatan: pola `p.Questions.Any()` di EF query sudah berpreseden di file lain
(AssessmentAdminController `:6010`) sehingga translasi EXISTS aman.

### IN-02: `ToUpper()` culture-dependent pada normalisasi token (pertimbangkan `ToUpperInvariant()` untuk konsistensi)

**File:** `Controllers/CMPController.cs:851`, `Controllers/AssessmentAdminController.cs:1799`
**Issue:** `AccessTokenMatches` dan `normalizedToken` memakai `.ToUpper()` (culture-aware).
Token bersifat alfanumerik ASCII (mis. `ABC23X`) sehingga praktis tak ada perbedaan; dan
karena KEDUA sisi compare memakai `ToUpper()` yang sama, hasil match tetap konsisten —
bukan bug. Namun locale Turkish-I (`i`/`I`) bisa mengubah hasil bila token mengandung huruf
i. Untuk kebal-locale dan menyatu dengan praktik normalisasi lain (mis. `ToLowerInvariant`
di `NormalizeText` `:1298` CMPController), pertimbangkan `ToUpperInvariant()`.
**Fix (opsional):**
```csharp
public static bool AccessTokenMatches(string? stored, string? input)
    => (stored ?? "").Trim().ToUpperInvariant() == (input ?? "").Trim().ToUpperInvariant();
```
Terapkan juga di 3 write-site `EditAssessment` agar simetris. Bukan blocker.

### IN-03: e2e DB UPDATE memakai string-interpolation (test-only, input bukan dari user)

**File:** `tests/e2e/exam-taking.spec.ts:1809`
**Issue:** `db.queryScalar(\`UPDATE AssessmentSessions SET AccessToken = '${tokenLower}' WHERE Id = ${assessmentId}; ...\`)`
menginterpolasi nilai ke string SQL. Secara teknis ini pola SQL-injection, namun
`assessmentId` adalah integer hasil `parseInt` dari URL test sendiri dan `tokenLower` adalah
konstanta test (`'abc23x'`) — keduanya bukan input pengguna, dan ini fixture test-only.
Konsisten dengan pola e2e existing lain di repo (banyak `db.queryString` interpolasi). Tidak
memengaruhi reliabilitas test. Dicatat sebagai kebiasaan saja.
**Fix (opsional):** Bila helper `db` mendukung parameterized query, gunakan parameter untuk
nilai string. Untuk integer dari `parseInt` aman dibiarkan. Tidak perlu untuk kelulusan fase.

---

_Reviewed: 2026-06-14_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
