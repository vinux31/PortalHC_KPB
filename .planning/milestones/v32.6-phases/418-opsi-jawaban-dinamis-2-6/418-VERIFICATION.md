---
phase: 418-opsi-jawaban-dinamis-2-6
verified: 2026-06-24T05:10:00Z
status: human_needed
score: 3/3 must-haves verified (OPT-01/02/03) + D-418-02 + security deviation confirmed
overrides_applied: 0
human_verification:
  - test: "Task 3 — UAT live @5277 (checkpoint orchestrator, 8 langkah <how-to-verify> 418-04-PLAN)"
    expected: "Alur opsi dinamis 2–6 berfungsi runtime di real browser: tambah/hapus baris A–F, single-select MC, render A–F di ujian/preview/results, edit-shrink alert-danger (bukan 500), prefill edit 5/6-opsi, backward-compat 4-opsi"
    why_human: "Lesson 354 — Razor/JS/SignalR WAJIB diuji real-browser; checkpoint human-verify gate=blocking yang dimiliki autopilot orchestrator (autopilot §5), dijalankan SETELAH verify/secure/validate. Bukan kegagalan — satu-satunya item outstanding by design (418-04 autonomous:false)."
---

# Phase 418: Opsi Jawaban Dinamis 2–6 — Laporan Verifikasi

**Phase Goal:** HC dapat membuat/mengubah soal dengan 2–6 opsi jawaban (bukan terkunci A–D) di form authoring web & form Inject, dan semua layar (ujian/preview/results) menampilkan huruf A–F dinamis dengan penilaian tetap benar.
**Verified:** 2026-06-24
**Status:** human_needed (semua truth otomatis VERIFIED; 1 item UAT live menunggu orchestrator)
**Re-verification:** No — verifikasi awal

## Goal Achievement

### Observable Truths (Success Criteria ROADMAP)

| # | Truth (SC) | Status | Evidence (file:line) |
|---|-----------|--------|----------------------|
| 1 | HC dapat membuat/mengubah soal dengan 2–6 opsi via form authoring web dan form Inject (tambah/hapus baris, min-2, max-6 ditegakkan) — **OPT-01** | ✓ VERIFIED | Controller binding `List<OptionInput>` di CreateQuestion (`AssessmentAdminController.cs:7720`) + EditQuestion (`:7940`); guard H3 `q.Options.Count > 4` DIHAPUS (grep → **0 match**, komentar penghapusan `:7985-7987`); form authoring baris dinamis `#addOptionBtn` (`ManagePackageQuestions.cshtml:442`), `.remove-option-btn` hidden saat `i<2` (`:413`), re-letter A–F disabled@6 (`reletterRows :827-833`, `addOptionRow :838-863`, `removeOptionRow :866-878`); form Inject dinamis `#injAddOptionBtn` (`_InjectQuestionForm.cshtml:59`, `data-option-row :41`); validator max-6 `filled > 6` (`QuestionOptionValidator.cs:31`). xUnit OptionValidation **GREEN** (run langsung). |
| 2 | Layar ujian, preview, dan hasil menampilkan huruf A–F dinamis sesuai jumlah opsi, dan penilaian tetap benar — **OPT-02** | ✓ VERIFIED | Array `{ "A","B","C","D","E","F" }` di StartExam (`:137` MA + `:170` MC), Results (`:363`), ExamSummary (`:57`), _PreviewQuestion (`:50`), PreviewPackage (`:6`); bug modulo `% letters.Length` DIHAPUS (grep → **0 match**); PreviewPackage render `optIdx < letters.Length ? letters[optIdx] : (optIdx+1)` (`:62`) → opsi ke-6 = "F" (bukan "A"); grading **tidak disentuh** (GradingService.cs bukan bagian diff 418) — match by `o.Id == PackageOptionId` (`GradingService.cs:110,123`) → agnostik jumlah/huruf, penilaian benar by design. |
| 3 | Kolom "Jawaban Benar" (form & import) menerima huruf A–F (multi untuk MA); min-2 & max-6 ditegakkan — **OPT-03** | ✓ VERIFIED | Validator `min-2` (`:26`) + `max-6` (`:31` "Maksimal 6 opsi per soal.") + correct-must-have-text (`:34-36`); MC single-select via `correctIndex` radio (`ManagePackageQuestions.cshtml:407`, `ResolveCorrectness AssessmentAdminController.cs:7698-7704`), MA per-baris `options[i].IsCorrect` (`:792`); test `MaxSix_Rejected`/`FiveOptions_Accepted`/`SixOptions_Accepted` **GREEN**. Import A–F sudah selesai di Phase 415 (di luar scope 418, dicatat CONTEXT). |

**Score:** 3/3 truths VERIFIED (semua via inspeksi kode + xUnit run langsung)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/OptionInput.cs` | Binding model whitelist (Text/IsCorrect/Image/ImageAlt/RemoveImage, NO Id) | ✓ VERIFIED | Class ada (`:17`), 5 properti whitelist, no Id (mitigasi mass-assignment T-418-06) |
| `Helpers/OptionShrinkGuard.cs` | `FindBlockedOptionIds` irisan distinct (kontrak Plan 01) | ✓ VERIFIED | Body nyata `removedOptionIds.Intersect(answeredOptionIds).Distinct().ToList()` (`:33`); stub NotImplementedException sudah diganti |
| `Helpers/QuestionOptionValidator.cs` | max-6 enforcement | ✓ VERIFIED | `if (filled > 6)` (`:31`); min-2 + correct-text dipertahankan |
| `Controllers/AssessmentAdminController.cs` | List-binding + guard edit-shrink + drop H3 + loop A–F | ✓ VERIFIED + WIRED | CreateQuestion/EditQuestion `List<OptionInput>` + `correctIndex`; H3 dihapus; loop upsert A–F `bound = Max(keep, existing.Count)` 4-cabang (`:8124-8153`); guard edit-shrink memanggil `OptionShrinkGuard.FindBlockedOptionIds` (`:8054`) SEBELUM SaveChanges (`:8156`) |
| `Views/Admin/ManagePackageQuestions.cshtml` | Baris dinamis + addOptionBtn + re-letter + populateEditForm dinamis + #authError role=alert | ✓ VERIFIED + WIRED | Semua hadir (`:396,:407,:411,:413,:442,:709-711,:769-835`); `if (i<4)` & arrays hardcoded A–D DIHAPUS (grep → 0) |
| `Views/Admin/_InjectQuestionForm.cshtml` + `InjectAssessment.cshtml` | Inject baris dinamis A–F (tanpa gambar) | ✓ VERIFIED | `data-option-row` + `injCorrect` single-name radio + `injAddOptionBtn` (`:41,:45,:59`) |
| 5 view render A–F + PreviewPackage modulo fix | Array A–F, no wrap | ✓ VERIFIED | 6 array A–F di 5 file; 0 modulo |
| `HcPortal.Tests/OptionValidationTests.cs` | Fact MaxSix + 5/6-opsi accept | ✓ VERIFIED | MaxSix/Five/Six Facts ada; **GREEN** |
| `HcPortal.Tests/EditShrinkGuardLogicTests.cs` | 4 Fact pure-logic irisan | ✓ VERIFIED | 4 Fact memanggil FindBlockedOptionIds; **GREEN** |
| `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | 2 test real-SQL (no 500 + removed-ok) | ✓ VERIFIED | `EditShrinkGuard_AnsweredOption_NotRemoved_NoException` + `_UnansweredOption_Removed_Succeeds`; **GREEN** (real-SQL, drive EditQuestion ASLI) |
| `tests/e2e/option-dynamic-418.spec.ts` | 8 skenario S1–S8 | ✓ VERIFIED (statis) | 8 `test(...)` (S1–S8) hadir; eksekusi runtime = ranah Task 3 UAT/orchestrator |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|----|--------|---------|
| EditQuestion POST | `OptionShrinkGuard` | `FindBlockedOptionIds(removed, answered)` pre-SaveChanges | ✓ WIRED | `:8054`; blocked→TempData["Error"]+redirect (`:8065-8066`), bukan 500 |
| CreateQuestion/EditQuestion | `QuestionOptionValidator` | `ValidateQuestionOptions(type, texts, corrects)` | ✓ WIRED | `:7793` (Create) + `:8016` (Edit) |
| Form authoring | controller | `options[i].Text` / `correctIndex` / `options[i].Image` | ✓ WIRED | name binding `:411,:407,:429`; re-letter renumber name index (`:781,:795,:818`) |
| StartExam/Results/ExamSummary/_PreviewQuestion | render huruf | array A–F index-derived + fallback numerik | ✓ WIRED | semua 5 view |
| CreateQuestion method | authz/antiforgery | `[HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]` | ✓ WIRED (deviasi 418-02 FIXED) | atribut kini tepat di atas `CreateQuestion` (`:7708-7711`); `TruncateAlt` direlokasi ke `:7689` (di atas atribut) — lubang CSRF/authz yang sebelumnya nyangkut ditutup |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| Render A–F (5 view) | `question.Options` / `q.Options` | EF query `Include(Options)` | Ya (opsi nyata dari DB, OrderBy Id) | ✓ FLOWING |
| Edit-shrink guard | `answered` (PackageOptionId terjawab) | `_context.PackageUserResponses.Where(...).ToListAsync()` (`:8049-8053`) | Ya (query DB nyata, bukan hardcoded) | ✓ FLOWING |
| Grading dynamic options | `selectedOption.Id` | `q.Options.FirstOrDefault(o => o.Id == PackageOptionId)` | Ya (Id-keyed, agnostik count) | ✓ FLOWING |
| populateEditForm prefill | `data.options` (length N) | GET JSON `EditQuestion` variable-length OrderBy Id | Ya (`ensureRowCount(opts.length)` enumerasi 0..n) | ✓ FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Build produksi 0 error | `dotnet build HcPortal.csproj` | Build succeeded, 0 Error, 24 Warning (pre-existing) | ✓ PASS |
| Validator max-6 + edit-shrink (pure+integration) GREEN | `dotnet test --filter OptionValidation\|EditShrinkGuard` | Passed: 17, Failed: 0 (incl 2 integration real-SQL + 4 pure + 11 validator) | ✓ PASS |
| Guard H3 benar-benar dihapus | grep `q.Options.Count > 4` | 0 match | ✓ PASS |
| Modulo bug PreviewPackage dihapus | grep `% letters.Length` di Views | 0 match | ✓ PASS |
| migration=FALSE | `git diff --name-only` 13 commit 418 (Migrations/\|Data/) | NONE | ✓ PASS |
| Grading tak disentuh | git diff GradingService.cs di 418 | NOT touched (Id-keyed preserved) | ✓ PASS |
| e2e suite jalan runtime (S1–S8) | `npx playwright test option-dynamic-418` | Butuh app live @5277 + DB backup/restore | ? SKIP → Task 3 UAT (orchestrator) |

### Requirements Coverage

| Requirement | Source Plan | Deskripsi | Status | Evidence |
|-------------|-------------|-----------|--------|----------|
| OPT-01 | 418-02, 418-03 | 2–6 opsi via form authoring + Inject (bukan terkunci A–D) | ✓ SATISFIED | List-binding + form dinamis + drop H3 + inject dinamis |
| OPT-02 | 418-03 | Render A–F dinamis di ujian/preview/results + penilaian benar | ✓ SATISFIED | 5 view A–F + modulo fix + grading Id-keyed unchanged |
| OPT-03 | 418-01, 418-02 | Jawaban benar A–F + min-2/max-6 ditegakkan | ✓ SATISFIED | Validator min-2+max-6 + correctIndex MC + MA multi-checkbox |

Tidak ada requirement ORPHANED — REQUIREMENTS.md memetakan tepat OPT-01/02/03 ke Phase 418, semuanya diklaim oleh plan.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| (none) | — | — | — | Tidak ada stub/TODO/placeholder/hardcoded-empty yang menghalangi goal. Stub `OptionShrinkGuard` (Plan 01 RED) sudah diganti body nyata (Plan 02 GREEN). |

### D-418-02 (Edit-Shrink Guard — tutup hazard 999.14)

✓ VERIFIED. Guard query `PackageUserResponses` untuk opsi yang akan dihapus, panggil `FindBlockedOptionIds`, dan bila ada irisan → `TempData["Error"]` + redirect (pesan menyebut huruf opsi terblok, UI-SPEC C5) — SEBELUM `SaveChangesAsync`. Aturan "opsi dihapus" di guard (`:8030-8046`) IDENTIK dengan loop upsert (`:8117-8153`) — kill-drift dijaga. Integration test real-SQL membuktikan `Record.ExceptionAsync == null` (TIDAK ada DbUpdateException/500 FK Restrict) dan opsi terjawab tetap utuh. **999.14 ditutup di jalur ini.**

### Security — Deviasi 418-02 (verifikasi khusus)

✓ CONFIRMED. CreateQuestion POST kini benar-benar didekorasi `[HttpPost]` + `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` (`AssessmentAdminController.cs:7708-7710`), tepat di atas signature method (`:7711`). `TruncateAlt` direlokasi ke `:7689` (di atas atribut) sehingga atribut tidak lagi nyangkut padanya. Lubang CSRF/authz pre-existing (T-418-03/08) DITUTUP oleh refactor ini. EditQuestion atribut juga benar (`:7927-7929`).

### Backward-Compatibility

✓ VERIFIED. Render letters memakai array superset A–F + fallback numerik (4-opsi tampil A–D identik); upsert loop preserve Id + gambar; grading Id-keyed tak berubah; populateEditForm enumerasi `data.options.length` (4/5/6 baris). e2e S8 (backward-compat 4-opsi) dirancang membuktikan ini runtime (pending Task 3).

### Human Verification Required

**1. Task 3 — UAT live @5277 (checkpoint orchestrator)**

- **Test:** Jalankan 8-langkah `<how-to-verify>` 418-04-PLAN di real browser @5277 (login admin@pertamina.com, snapshot DB sebelum, RESTORE sesudah): tambah/hapus baris A–F + disabled@6, single-select MC lintas 6 baris, render A–F di StartExam/Results/PreviewPackage, edit-shrink alert-danger (seed PackageUserResponse), prefill edit 5/6-opsi, reasosiasi gambar baris-tengah, backward-compat 4-opsi.
- **Expected:** Semua langkah PASS runtime (lesson 354 — Razor/JS tak bisa diuji unit).
- **Why human:** Checkpoint `human-verify gate=blocking` yang dimiliki autopilot orchestrator (autopilot §5), dijalankan SETELAH review/secure/validate. Plan 418-04 `autonomous:false`. Ini **bukan kegagalan** — satu-satunya item outstanding by design. Bukti pendukung kuat sudah ada: e2e spec (8 skenario) executor melaporkan 9/9 PASS + integration real-SQL 6/6 GREEN + build 0-err.

### Gaps Summary

Tidak ada gap. Ketiga requirement (OPT-01/02/03), D-418-02 (tutup 999.14), dan deviasi keamanan 418-02 semuanya VERIFIED via inspeksi kode + build + xUnit run langsung (17/17 GREEN pada surface 418). migration=FALSE terkonfirmasi (0 file Migrations/Data tersentuh di 13 commit). Grading tak disentuh (PackageOption.Id-keyed dipertahankan). Satu-satunya item terbuka adalah **Task 3 UAT live @5277**, yang merupakan checkpoint blocking milik orchestrator (bukan failure) — sehingga status keseluruhan = `human_needed`, bukan `passed`.

---

_Verified: 2026-06-24_
_Verifier: Claude (gsd-verifier)_
