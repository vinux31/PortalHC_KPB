---
phase: 415-section-foundation-import-excel-diperluas
plan: 03
subsystem: import
tags: [aspnet-mvc, closedxml, excel-import, dual-format, section, fingerprint, xss-safe, sql-server, xunit]

# Dependency graph
requires:
  - phase: 415-01-section-foundation-data-model
    provides: "Entity AssessmentPackageSection + PackageQuestion.SectionId int? nullable FK + DbSet + FK Question→Section SetNull + unique index (AssessmentPackageId, SectionNumber)"
  - phase: 415-02-section-surface-admin
    provides: "Section CRUD endpoints (CreateSection/EditSection/DeleteSection/SetAllSectionsNewPage) + LogSectionAuditAsync + int? sectionId pada CreateQuestion/EditQuestion + ViewBag.Sections grouping"
provides:
  - "DownloadQuestionTemplate type=Universal = template 13-kolom (Pertanyaan | Opsi A–F | Jawaban Benar | No. Section | Nama Section | Elemen Teknis | QuestionType | Rubrik); MC/MA/Essay tetap 9-kolom legacy"
  - "ImportPackageQuestions dual-format parser: deteksi otomatis dari HEADER row (>9 kolom = baru, ≤9 = lama); legacy A–D SectionId=null kompatibel-mundur"
  - "Import menerima + menyimpan Opsi A–F (2–6 opsi, hanya non-kosong dibuat) + jawaban benar huruf A–F (O-1)"
  - "Section auto-create dari kolom No.Section saat commit (find-or-create per (packageId, sectionNumber), first-non-empty Nama menang, toggle default StartNewPage=false/ShuffleEnabled=true)"
  - "MakePackageFingerprint 8-arg (+Opsi E/F +SectionNumber); kedua caller comparable (existing-set load Section nav; new-row pass parsed E/F + SectionNumber)"
  - "ExtractPackageCorrectLetter widen ABCD→ABCDEF (import answer acceptance saja)"
  - "Per-Section count hard-block D-13 (titik #1) saat import: tolak keras + daftar ketidakcocokan LENGKAP (never stop-at-first) + 0 write via TempData[SectionMismatch] (JSON List<string>)"
  - "ImportPackageQuestions.cshtml: format card universal diperluas + dual-format note + D-13 mismatch alert-danger (XSS-safe @ encode)"
affects: [415-04-sync-pre-post, 416-scoped-shuffle, 417-section-pagination, 418-opsi-dinamis, 419-export-polish-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Dual-format Excel parse: deteksi format dari HEADER row LastCellUsed (bukan data rows — Pitfall 4: blank trailing cells pada Essay skew count); boundary >9 (D-415-03 authority, bukan spec stray '<11')"
    - "Section auto-create via navigation property (newQ.Section = FindOrCreateSection(...)) → EF wire FK di SaveChanges, persist Section + soal di transaksi yang SAMA (atomic)"
    - "Per-Section count validation = grup incoming + tiap sibling by SectionNumber (null → Lainnya), bandingkan per-key, daftar LENGKAP; legacy all-null kedua sisi → fallback total-count (Pitfall 6)"
    - "Mismatch list lewat TempData JSON-serialized List<string> (TempData tak andal bawa objek kompleks lintas redirect) → view deserialize + render <ul> @ encode"

key-files:
  created:
    - "HcPortal.Tests/SectionImportTests.cs"
    - ".planning/phases/415-section-foundation-import-excel-diperluas/415-03-SUMMARY.md"
  modified:
    - "Controllers/AssessmentAdminController.cs"
    - "Views/Admin/ImportPackageQuestions.cshtml"

key-decisions:
  - "Universal template (type=Universal) = 13-kolom; MC/MA/Essay legacy tetap 9-kolom (kompatibel-mundur; import deteksi dual-format dari header)"
  - "Opsi A–F: hanya opsi non-kosong dibuat sebagai PackageOption (2–6 opsi). E/F dengan teks → PackageOption dgn IsCorrect benar. Grading by PackageOption.Id sudah aman (huruf E/F render = Phase 418)"
  - "Section auto-create tiebreak: first-non-empty Nama menang HANYA untuk section yang baru-dibuat selama import (section existing keep Nama-nya). Toggle default false/true (Excel tak bawa toggle)"
  - "Mismatch key TempData[SectionMismatch] (JSON List<string>) — kontrak untuk Plan 04 re-guard StartExam parity"

patterns-established:
  - "Dual-format detect dari header row colCount > 9 (header selalu terisi penuh)"
  - "Paste branch tetap A–D 9-kolom (no new-format paste di 415)"

requirements-completed: [IMP-01, IMP-02, IMP-03, SEC-04]

# Metrics
duration: 13min
completed: 2026-06-22
---

# Phase 415 Plan 03: Import Excel Diperluas (Dual-Format Section-Aware) Summary

**Import Excel diperluas jadi dual-format Section-aware: template universal 13-kolom (Opsi A–F + No.Section/Nama), parser deteksi-otomatis dari header (>9 baru / ≤9 lama, kompatibel-mundur), terima+simpan Opsi A–F + jawaban A–F, auto-buat record Section dari kolom No.Section (atomic, find-or-create), fingerprint dedup 8-arg (+E/F+SectionNumber), dan validasi jumlah soal per-Section antar-paket dengan tolak-keras (daftar lengkap, 0 write) — runtime-verified @5277 + 7/7 SectionImport hijau; migration=FALSE.**

## Performance

- **Duration:** 13 min
- **Started:** 2026-06-22T13:14:56Z
- **Completed:** 2026-06-22T13:28:45Z
- **Tasks:** 4
- **Files modified:** 3 (1 created test, 2 modified) + 1 SUMMARY

## Accomplishments
- **DownloadQuestionTemplate** `type=Universal` → header 13-kolom (§9.1: Pertanyaan | Opsi A–F | Jawaban Benar | No. Section | Nama Section | Elemen Teknis | QuestionType | Rubrik) + contoh row (MC Section 1, MA 6-opsi A,C,E Section 1, Essay tanpa Section) + instruksi A–F/Section; MC/MA/Essay tetap 9-kolom legacy. **Runtime-verified**: file Universal punya 13 header benar; MC tanpa Opsi E/No.Section
- **ImportPackageQuestions dual-format parser**: `isNewFormat = ws.Row(1).LastCellUsed().ColumnNumber > 9` (deteksi dari HEADER row — Pitfall 4: data rows Essay punya sel kosong di akhir → skew). NEW: Q(1)|A–F(2–7)|Correct(8)|No.Section(9)|Nama(10)|ET(11)|Type(12)|Rubrik(13); OLD: unchanged 9-kolom + E/F="" SectionNumber=null
- **Opsi A–F (O-1)**: `ExtractPackageCorrectLetter` ABCD→ABCDEF + 3 whitelist A–F (count-validation, MA parse, MC parse). Build options hanya opsi non-kosong (2–6), IsCorrect untuk E/F via PackageOption.Id (render huruf E/F = Phase 418)
- **Section auto-create (O-2)**: `FindOrCreateSection(sectionNumber, sectionName)` cache existing + new; assign via `newQ.Section = ...` (nav → EF wire FK same tx); first-non-empty Nama menang untuk section baru-dibuat; toggle default false/true. Section + soal persist di SATU transaksi
- **Fingerprint 8-arg (IMP-03)**: `MakePackageFingerprint(q,a,b,c,d,e,f,int? sectionNumber)` join + sentinel `_NOSEC_`; existing-set load `q.Section?.SectionNumber` + opsi 0..5; new-row pass parsed E/F + SectionNumber. Soal sama beda section / beda jumlah opsi → fingerprint beda (no false dedup)
- **Per-Section count hard-block (SEC-04 / D-13 #1)**: grup incoming valid + tiap sibling by SectionNumber (null → "Lainnya"), bandingkan per-key, daftar mismatch LENGKAP (never stop-at-first) copy `Section {sn}: Paket "{me}" punya {x} soal, Paket "{sib}" punya {y} soal (harus sama).`; legacy all-null kedua sisi → fallback total-count (Pitfall 6); single-pkg/no-sibling → skip. Mismatch → `TempData[SectionMismatch]` (JSON List<string>) + 0 write
- **ImportPackageQuestions.cshtml**: format card "Format kolom (template universal diperluas)" + 13-kolom + dual-format note + D-13 mismatch alert-danger (deserialize JSON → full `<ul>` @ encode, no @Html.Raw) + render Success/Error/Warning TempData. **Runtime-verified @5277 HTTP 200** (lesson 354): semua marker render, 0 Razor compile error
- **SectionImportTests** (7 test, Integration SQLEXPRESS): IMP-01 roundtrip+auto-create+A–F, IMP-02 legacy, IMP-03 fingerprint (beda-section not-deduped + identik deduped), SEC-04 mismatch full-list+0-write + match-succeeds + single-pkg-skipped. Drive action ASLI via ClosedXML .xlsx + IFormFile stub (de-tautology)

## Task Commits

Each task committed atomically:

1. **Task 1: Universal 13-col template + 8-arg fingerprint + A–F acceptance** - `b321d286` (feat)
2. **Task 2: Dual-format parser + Section auto-create + per-Section count hard-block** - `6342cdde` (feat)
3. **Task 3: Import view universal format card + D-13 full mismatch list** - `f168d520` (feat)
4. **Task 4: SectionImportTests (IMP-01/02/03 + SEC-04)** - `6cc2c1a7` (test)

**Plan metadata:** _(final commit)_ `docs(415-03)`

_migration=FALSE — `git diff Migrations/ Data/` kosong. Notify IT tetap hanya 415-01 (`AddAssessmentPackageSection`, hash `2391257c`, migration=TRUE)._

### Mismatch-list TempData kontrak (untuk Plan 04 re-guard StartExam parity)
```
TempData["SectionMismatch"] = JsonSerializer.Serialize(List<string> mismatchList)
// entri: $"Section {sn|Lainnya}: Paket \"{me}\" punya {x} soal, Paket \"{sib}\" punya {y} soal (harus sama)."
// sibling key Title+Category+Schedule.Date (LOCKED); grup null SectionNumber → "Lainnya"
```

### Fingerprint signature (final)
```csharp
private static string MakePackageFingerprint(string q, string a, string b, string c, string d,
    string e, string f, int? sectionNumber)
    => string.Join("|||", new[]{q,a,b,c,d,e,f}.Select(NormalizePackageText)
        .Append(sectionNumber?.ToString() ?? "_NOSEC_"));
```

### Section auto-create behavior (tiebreak applied)
- find-or-create per `(packageId, sectionNumber)`; cache existing + new selama import
- **first-non-empty Nama menang** — hanya untuk section yang BARU dibuat di import ini; section existing keep Nama-nya
- toggle default `StartNewPage=false`, `ShuffleEnabled=true` (Excel tak bawa toggle, §15.D)
- assign via `newQ.Section` nav → EF wire `SectionId` di SaveChanges; Section + soal di transaksi SAMA (atomic)

## Files Created/Modified
- `Controllers/AssessmentAdminController.cs` — DownloadQuestionTemplate Universal 13-kolom; ImportPackageQuestions dual-format parser (header-detect + 13-kolom branch) + tuple diperluas (OptE/F + SectionNumber + SectionName) + per-Section count validation (full mismatch list) + Section auto-create (FindOrCreateSection) + build options A–F; MakePackageFingerprint 8-arg + kedua caller; ExtractPackageCorrectLetter ABCDEF; 3 whitelist A–F
- `Views/Admin/ImportPackageQuestions.cshtml` — format card universal diperluas + dual-format note + D-13 mismatch alert-danger (JSON deserialize → full `<ul>`) + Success/Error TempData render
- `HcPortal.Tests/SectionImportTests.cs` — 7 Integration test (ClosedXML .xlsx + IFormFile stub drive action ASLI)

## Decisions Made
- **Universal-only 13-kolom** (MC/MA/Essay tetap 9-kolom legacy) — satu template universal diperluas per D-415-03; dual-format detect dari header colCount > 9 (CONTEXT authority, abaikan spec stray "<11")
- **Opsi A–F: hanya non-kosong dibuat** — soal 2–6 opsi tersimpan apa-adanya; E/F dengan teks → PackageOption IsCorrect benar (grading Id-based aman; render huruf E/F = Phase 418)
- **Section auto-create first-non-empty Nama** untuk section baru-dibuat (O-2 tiebreak); toggle default false/true
- **Mismatch via TempData JSON List<string>** — TempData tak andal bawa objek kompleks lintas redirect; view deserialize + render. Kontrak untuk Plan 04 re-guard
- **Legacy guard (Pitfall 6)**: all-null SectionNumber kedua sisi → fallback total-count (perilaku lama); single-pkg/no-sibling → skip guard

## Deviations from Plan
None — plan dieksekusi persis seperti tertulis. Keempat task selesai sesuai action; tidak ada bug auto-fix (Rule 1-3) maupun keputusan arsitektural (Rule 4). Tidak ada auth gate.

## Issues Encountered
- Render probe membutuhkan PathBase `/KPB-PortalHC` + password dev `123456` (bukan default tebakan) — diperbaiki dengan baca `reference_dev_credentials.md`. Bukan bug; hanya setup probe.

## Known Stubs
None — template, parser, Section auto-create, fingerprint, per-Section validation, dan view error-list semua di-wire ke jalur nyata. Render/grading huruf opsi E/F memang dijadwalkan Phase 418 (UI-SPEC + CONTEXT D-415-03) — 415 HANYA terima + simpan Opsi A–F + jawaban A–F (data-only, grading by PackageOption.Id sudah aman). Bukan stub: kolom + IsCorrect E/F DISIMPAN benar sekarang.

## Threat Flags
None — tidak ada surface keamanan baru di luar threat_model plan. Format auto-detect dari worksheet server-read (T-415-10, tak ada field client isNewFormat); RBAC Admin,HC + antiforgery + 5MB guard dipertahankan (T-415-11); D-13 hard-block server-authoritative + 0 write atomic (T-415-12); Nama Section + mismatch list render via Razor @ encode, no @Html.Raw (T-415-13).

## User Setup Required
None - no external service configuration required.

## TDD Gate Compliance
Task 4 (`tdd="true"`) adalah Integration controller-driven atas perilaku yang dibangun Task 1-3 (`feat` mendahului `test` — gate ordering terpenuhi). RED murni-baru tidak applicable karena import path (parser/fingerprint/auto-create) sudah hidup saat test ditulis; test berfungsi sebagai gate kunci yang mengunci kontrak import (dual-format, A–F, auto-create, fingerprint distinctness, per-Section hard-block). Commit `test(...)` `6cc2c1a7` ada; behavior `feat` (`b321d286`/`6342cdde`/`f168d520`) mendahului. Konsisten dengan TDD compliance Plan 01/02 (data/perilaku layer-dalam mendahului test integration).

## Next Phase Readiness
- **Plan 04 (sync Pre→Post + StartExam re-guard) siap.** Mismatch TempData kontrak + fingerprint signature + Section auto-create behavior terdokumentasi di atas. Re-guard StartExam (D-13 titik #2) WAJIB pakai grouping key yang SAMA (SectionNumber, sibling key Title+Category+Schedule.Date) agar parity dengan import guard
- **migration=FALSE** Plan 03 — notify IT tetap hanya Plan 01 (`AddAssessmentPackageSection` `2391257c`)
- **Catatan Plan 04/418:** opsi E/F kini DISIMPAN (PackageOption); `SyncPackagesToPost` deep-clone existing sudah iterasi `q.Options` (semua opsi, termasuk E/F) — tinggal tambah clone record Section + remap SectionId (Pitfall 8). Render/authoring-form huruf A–F = Phase 418
- **Lesson re-confirmed (354):** Razor runtime compile — view-render probe @5277 (login PathBase /KPB-PortalHC + password `123456`) WAJIB; `dotnet build` 0-error tidak cukup. Template download diverifikasi via inspeksi .xlsx (sharedStrings namespace `x:` → grep raw, bukan default `<t>`)

## Self-Check: PASSED
- Files created: 2/2 found (SectionImportTests.cs + SUMMARY)
- Files modified: 2/2 found (AssessmentAdminController.cs + ImportPackageQuestions.cshtml)
- Commits: 4/4 found (b321d286, 6342cdde, f168d520, 6cc2c1a7)
- Tests: SectionImport 7/7 + fast 412/412 + Shuffle 48/48 + SectionCrud 9/9 green; build 0 error; import page render @5277 HTTP 200; migration=FALSE

---
*Phase: 415-section-foundation-import-excel-diperluas*
*Completed: 2026-06-22*
