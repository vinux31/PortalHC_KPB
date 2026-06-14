---
phase: 355-test-uat
verified: 2026-06-09T11:30:00+08:00
status: passed
score: 7/7
overrides_applied: 0
---

# Phase 355: Test & UAT — Verification Report

**Phase Goal:** Bukti otomatis & manual bahwa fitur gambar bekerja end-to-end dari admin upload sampai peserta melihat di ujian & pembahasan, dengan integritas file & data ter-cover.
**Verified:** 2026-06-09
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths (dari ROADMAP Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| SC1a | Suite xUnit TST-01 mencakup upload valid JPG/PNG | VERIFIED | `FileUploadHelperTests.cs` 8 `[Fact]` ValidateImageFile_Valid{Jpg,Png,Jpeg} + 3 invalid — file exists, committed |
| SC1b | Suite xUnit TST-01 mencakup upload invalid ditolak via magic-byte | VERIFIED | `ValidateImageFile_ExeRenamedPng_ReturnsInvalidMagicByte` + `ValidateImageFile_Pdf_ReturnsInvalidExtension` + oversize — semua ada di `FileUploadHelperTests.cs` |
| SC1c | Suite xUnit TST-01 mencakup SyncPackagesToPost salin ImagePath/ImageAlt | VERIFIED | `PackageImageSyncTests.cs` 4 `[Fact]`: `SyncCopiesQuestionImagePath`, `SyncCopiesOptionImageAlt`, `SyncSharesSamePath_NoFileDuplication`, `SyncHandlesNullImagePath` |
| SC1d | Suite xUnit TST-01 mencakup DeleteQuestion menghapus file gambar soal+opsi | VERIFIED | `PackageImageDeleteTests.cs`: `RefCount_Deletes_WhenNoOtherRowSharesPath`, `RefCount_SkipsDelete_WhenPathSharedByOtherRow`, `RefCount_DeletePackage_SkipsShared_DeletesOrphan`, `DeletePackageImage_CollectsAllNonNullPaths` |
| SC1e | Suite xUnit TST-01 mencakup replace menghapus file lama | VERIFIED | `Replace_NewFileWins_DeletesOldFileOnDisk` (commit a0f8ad42) — menulis old.jpg + new.jpg ke disk, loop `DeleteIfUnreferenced`, assert `File.Exists(old)==false` + `File.Exists(new)==true` |
| SC2 | Playwright UAT TST-02 lulus alur penuh: admin upload gambar soal+opsi -> StartExam render responsif -> Results render | VERIFIED | `tests/e2e/image-in-assessment.spec.ts` committed (db5b7115 + d4edae7c), 3 tests live passes di localhost:5277 — assert `img.question-image-zoom[img-fluid+loading=lazy+src /uploads/questions/]` StartExam + Results, guard toggle lightbox + radio NOT checked, guard null `toHaveCount(0)`. Human UAT approved 2026-06-09 |
| SC3 | dotnet build 0 error + seluruh suite hijau + UAT di localhost:5277 + tanpa regresi flow ujian existing | VERIFIED | dotnet test 131/131 passed (baseline 130 + 1 baru), build 0 error. Regresi exam-taking/exam-types pre-broken oleh validator naming v20 (REST-06, hutang teknis pra-eksis bukan regresi 355); bukti non-regresi diganti: image spec menjalankan soal MC tanpa gambar end-to-end + dotnet test 131/131. Human UAT approved. |

**Score: 7/7 truths verified**

---

### Required Artifacts

| Artifact | Provides | Status | Commit |
|----------|---------|--------|--------|
| `HcPortal.Tests/PackageImageDeleteTests.cs` | `[Fact] Replace_NewFileWins_DeletesOldFileOnDisk` reuse helper `MakeTempDir`/`ApplyIntent`/`DeleteIfUnreferenced` | VERIFIED — exists, substantive (assert `File.Exists` on disk), wired (reuses existing helpers) | a0f8ad42 |
| `tests/fixtures/q-image.jpg` | Fixture upload gambar soal (magic-byte JPEG valid FF D8 FF, 160B) | VERIFIED — exists (160B), magic-byte confirmed via node check, committed via 570fddfb + .gitignore exception | 570fddfb |
| `tests/fixtures/opt-image.png` | Fixture upload gambar opsi (magic-byte PNG valid 89 50 4E 47, 69B) | VERIFIED — exists (69B), magic-byte confirmed, committed | 570fddfb |
| `tests/e2e/helpers/wizardSelectors.ts` | Selector gambar `questionImgField`/`optAImgField`../alt di-extend additive | VERIFIED — `questionImgField` at line 122, semua selector gambar tersedia | 570fddfb |
| `tests/e2e/helpers/examTypes.ts` | `addQuestionViaForm` extended param `images?` → `setInputFiles` pada hidden file input | VERIFIED — `images?: QuestionImages` (L172), `setInputFiles` (L225+), additive (tidak merusak existing) | 570fddfb |
| `tests/e2e/image-in-assessment.spec.ts` | Spec UAT TST-02 + D-06 guard toggle + guard null, dengan snapshot/restore + cleanup file | VERIFIED — 11 acceptance grep checks PASS, committed, ran live 3/3 | db5b7115, d4edae7c |
| `docs/SEED_JOURNAL.md` | Entry Phase 355 (temporary+local-only, uploads/questions dicatat, status cleaned) | VERIFIED — baris `2026-06-09 \| 355 (test-uat gambar)` + `temporary + local-only` + `uploads/questions` + `cleaned` | 6f0dfdbd |
| `.planning/phases/355-test-uat/355-VALIDATION.md` | Frontmatter `nyquist_compliant: true` + `wave_0_complete: true` + `status: complete` | VERIFIED — semua 3 field confirmed via node check | 88d42430 |

---

### Key Link Verification

| From | To | Via | Status | Evidence |
|------|----|-----|--------|----------|
| `Replace_NewFileWins_DeletesOldFileOnDisk` | `DeleteIfUnreferenced` + `ApplyIntent` (helper existing di file yang sama) | reuse helper, tidak buat helper baru | WIRED | `PackageImageDeleteTests.cs:218-225` — `ApplyIntent(...)` + `foreach (var p in deleteList.Distinct()) DeleteIfUnreferenced(p, remainingQ, remainingO)` |
| `image-in-assessment.spec.ts beforeAll` | `InstanceDefaultBackupPath` + `db.backup` | SERVERPROPERTY query | WIRED | L45-50 — `db.queryString("SELECT CAST(SERVERPROPERTY(...)")` + `await db.backup(snapshotPath)` |
| `image-in-assessment.spec.ts afterAll` | `wwwroot/uploads/questions/{createdPackageId}` | `fs.rmSync` recursive | WIRED | L66 — `fs.rmSync(dir, { recursive: true, force: true })` |
| `addQuestionViaForm(images)` | `#questionImgField` / `#optAImgField` | `page.setInputFiles` pada hidden input | WIRED | `examTypes.ts:225-234` — conditional `setInputFiles` calls per image field |
| `spec assert StartExam/Results` | `img.question-image-zoom` (`_QuestionImage` partial) | locator visible + img-fluid + loading=lazy + src /uploads/questions/ | WIRED | `image-in-assessment.spec.ts:132-136,180-181` — `qcardImg.locator(img.question-image-zoom[data-img-alt="${Q_IMG_ALT}"])`, semua attribute assertions |

---

### Data-Flow Trace (Level 4)

Level 4 tidak applicable untuk fase test/UAT ini. Artefak yang diproduksi adalah kode tes, bukan komponen yang me-render data dinamis. Alur data fitur gambar itu sendiri sudah diverifikasi oleh tes yang berjalan live (spec image-in-assessment menjalankan upload nyata -> render nyata di browser).

---

### Behavioral Spot-Checks

| Behavior | Verifikasi | Hasil | Status |
|----------|-----------|-------|--------|
| `Replace_NewFileWins_DeletesOldFileOnDisk` ada dan substantif | `grep -n "Replace_NewFileWins_DeletesOldFileOnDisk\|Assert.False.*File.Exists"` di `PackageImageDeleteTests.cs` | L204 (Fact), L227 (`Assert.False`), L228 (`Assert.True`) ditemukan | PASS |
| Magic-byte fixtures valid | `node -e "... j[0]===255 ... p[0]===137 ..."` | "fixtures magic-byte OK - jpg:160B png:69B" | PASS |
| Spec parse valid (--list) | Dikonfirmasi via SUMMARY-02 (`cd tests; npx playwright test image-in-assessment.spec.ts --list` exits 0, 2 tests discovered) | Exits 0 | PASS |
| Semua acceptance grep spec | 11 pattern checks via `node -e` | "ALL spec checks OK (11 items)" | PASS |
| SEED_JOURNAL entry Phase 355 cleaned | `node -e "... /2026-06-09 \| 355/ ... /cleaned/ ..."` | "journal 355 OK" | PASS |
| VALIDATION frontmatter updated | `node -e "... nyquist_compliant:\s*true ... wave_0_complete:\s*true ..."` | "VALIDATION updated OK" | PASS |
| Semua commit hash terlacak di git | `git log --oneline --all \| grep -E "a0f8ad42\|570fddfb\|db5b7115\|d4edae7c\|88d42430\|6f0dfdbd"` | 6/6 commit ditemukan di branch ITHandoff | PASS |

Run live end-to-end (3/3 passes) dan dotnet test 131/131 dikonfirmasi via SUMMARY-03 — tidak dapat di-run ulang dari verifier tanpa server aktif (butuh `dotnet run`). Sudah ter-cover sebagai human verification yang diapprove.

---

### Requirements Coverage

| Requirement | Source Plan | Deskripsi | Status | Evidence |
|-------------|------------|-----------|--------|----------|
| TST-01 | 355-01-PLAN.md, 355-03-PLAN.md | xUnit cover upload valid (JPG/PNG tersimpan) + invalid (non-image ditolak) + SyncPackagesToPost menyalin ImagePath + DeleteQuestion menghapus file gambar | SATISFIED | 3 file tes gambar existing hijau (FileUploadHelper 8 Fact + PackageImageSync 4 Fact + PackageImageDelete 7 Fact termasuk Replace_NewFileWins_DeletesOldFileOnDisk baru); dotnet test 131/131 |
| TST-02 | 355-02-PLAN.md, 355-03-PLAN.md | Playwright UAT — admin upload gambar soal+opsi → peserta lihat di StartExam → lihat di Results (pembahasan) | SATISFIED | `tests/e2e/image-in-assessment.spec.ts` committed, 3/3 tests passed live di localhost:5277, human UAT approved |

Tidak ada requirement orphan: TST-01 dan TST-02 adalah satu-satunya requirement yang dipetakan ke Phase 355 di REQUIREMENTS.md (baris 87-88). Kedua-duanya diklaim di PLAN frontmatter dan terbukti di codebase.

---

### Anti-Patterns Found

| File | Pola | Severity | Impact |
|------|------|----------|--------|
| `image-in-assessment.spec.ts:166` | `waitForTimeout(2_000)` hard sleep | Info | SignalR settle wait — sudah didahului assert `#answeredProgress 2/2`; non-blocking, dicatat sebagai hutang teknis |
| `PackageImageDeleteTests.cs:127` | `Assert.DoesNotContain(null, collected)` redundan | Info | `collected` sudah difilter `.Where(!IsNullOrEmpty)` — assert trivially true, tidak memengaruhi coverage |
| `image-in-assessment.spec.ts:139,145` | `optImg` dan `optLabel` dibangun via jalur locator independen | Info | Aman selama `OPT_IMG_ALT` unik di halaman; advisory robustness |
| `PackageImageDeleteTests.cs:130-151` | Inline-mirror logika controller tanpa anti-drift guard | Info | Batasan inheren strategi "logic-mirror tanpa DbContext" — dicatat di komentar "keep in sync", tidak memblokir tujuan |

Tidak ada blocker atau warning yang memblokir goal achievement. 2 Warning dari code review (WR-01 state-leak parsial, WR-02 isChecked timing) adalah robustness/advisory — sudah diidentifikasi sebelum verifikasi ini, bukan temuan baru.

---

### Human Verification

Human UAT telah diselesaikan dan di-approve sebelum verifikasi ini. Tidak ada item yang memerlukan human verification tambahan.

**Human UAT (selesai 2026-06-09):**

- Spec `image-in-assessment.spec.ts` dijalankan live di localhost:5277 (headed/headless)
- Gambar soal (240px) + gambar opsi render di StartExam; klik gambar opsi membuka lightbox, radio TIDAK ter-pilih
- Results / pembahasan: gambar soal + opsi render di "Tinjauan Jawaban"
- Cleanup terverifikasi: `wwwroot/uploads/questions/` bersih, SEED_JOURNAL entry 355 = `cleaned`, DB lokal bersih (0 residue `Pre Test OJT IMG355%`)
- User mengetik "approved" sebagai resume-signal Plan 03 Task 4

---

### Gaps Summary

Tidak ada gap. Semua 7 observable truths VERIFIED. Semua artefak exist, substantive, dan wired. Semua key links terbukti. TST-01 dan TST-02 keduanya SATISFIED. Human UAT telah di-approve.

**Non-blocking findings (bukan gap, tidak memblokir goal):**

1. Regresi `exam-taking.spec.ts`/`exam-types.spec.ts` pre-broken oleh validator naming v20 (REST-06) — hutang teknis pra-eksis, bukan regresi Phase 355 (zero production code diubah). Bukti non-regresi sudah diganti via image spec MC no-image + dotnet test 131/131. Saran backlog: update judul spec lama comply v20.
2. WR-01 (cleanup leak parsial saat TEST 1 gagal sebelum packageId diperoleh) — risiko minimal, advisory robustness.
3. WR-02 (isChecked timing guard-toggle) — false-negative kecil pada deteksi regresi; test sudah hijau.

---

## Kesimpulan

Phase 355 mencapai goal-nya. Milestone v24.0 (gambar di soal assessment, Phase 352-355) memiliki coverage test yang lengkap: xUnit unit tests hijau 131/131 mencakup seluruh butir TST-01, Playwright UAT e2e committed dan terbukti hijau live membuktikan alur end-to-end TST-02, dan human sign-off diperoleh. Seed Workflow dipatuhi (backup/restore + cleanup file + journal cleaned). Tidak ada production code yang diubah di fase ini.

---

_Verified: 2026-06-09T11:30:00+08:00_
_Verifier: Claude (gsd-verifier)_
