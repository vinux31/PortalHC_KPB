---
phase: 415-section-foundation-import-excel-diperluas
plan: 04
subsystem: assessment-packages
tags: [aspnet-mvc, ef-core, sql-server, section, deep-clone, pre-post-sync, exam-guard, xunit, integration]

# Dependency graph
requires:
  - phase: 415-01-section-foundation-data-model
    provides: "Entity AssessmentPackageSection + PackageQuestion.SectionId int? nullable FK + Section nav + DbSet + FK Question->Section SetNull + Section->Package Restrict + unique index (AssessmentPackageId, SectionNumber)"
  - phase: 415-02-section-surface-admin
    provides: "Section CRUD (CreateSection/EditSection/DeleteSection/SetAllSectionsNewPage) + sectionId pada CreateQuestion/EditQuestion + ViewBag.Sections grouping"
  - phase: 415-03-import-excel-diperluas
    provides: "D-13 import hard-block (titik #1): per-Section count compare, sibling key Title+Category+Schedule.Date LOCKED, grup null->'Lainnya', full mismatch list, 0-write atomic; fingerprint 8-arg; Opsi A-F stored"
provides:
  - "SyncPackagesToPost deep-clone Section rows (SectionNumber/Name/StartNewPage/ShuffleEnabled) ke paket Post + remap SectionId via nav property newQ.Section (old->new section map per-paket, NO naive copy) (SEC-06)"
  - "Opsi 5-6 (E/F) ikut tersalin saat sync (existing q.Options.Select clone SEMUA opsi tanpa batas A-D)"
  - "Re-sync (CopyPackagesFromPre 2x) tidak meninggalkan stale Section Post (Wave-1 RemoveRange Post sections sebelum clone)"
  - "Semua 6 pemanggil SyncPackagesToPost mewarisi clone Section (body tunggal): callers @6605 (CopyPackagesFromPre), 6646, 6741, 7627, 7905, 7998"
  - "CMPController.StartExam D-13 re-guard (titik #2) SEBELUM BuildQuestionAssignment: per-SectionNumber count compare antar paket saudara, blok mulai ujian saat drift edit-manual pasca-import (SEC-04 #2)"
  - "Re-guard parity dgn import: sibling key Title+Category+Schedule.Date (LOCKED) + grup per SectionNumber (null->Lainnya); fire HANYA bila >=2 paket berisi soal DAN >=1 punya Section (legacy all-null + 1-paket lolos)"
affects: [416-scoped-shuffle, 417-section-pagination, 418-opsi-dinamis, 419-export-polish-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Deep-clone remap FK via navigation property (newQ.Section = sectionMap[old]) supaya EF wire SectionId baru di SaveChanges; old->new map per-paket berkunci old SectionId (Pitfall 8 anti cross-package leak)"
    - "Server-authoritative exam-start re-guard: load paket saudara (sibling key import) dgn .ThenInclude(q => q.Section), bandingkan count per SectionNumber paket-referensi-pertama vs tiap saudara; skip bila <2 paket atau all-null section (Pitfall 6 backward-compat)"
    - "Integration test drive REAL controller (CopyPackagesFromPre / StartExam) atas SQLEXPRESS; stub ISession agar ImpersonationService.IsImpersonating()==false (UseRealUser path); justStarted=false (StartedAt seeded) hindari jalur write-on-GET/broadcast/LogActivityAsync"

key-files:
  created:
    - "HcPortal.Tests/SectionSyncPrePostTests.cs"
    - "HcPortal.Tests/SectionMismatchGuardTests.cs"
    - ".planning/phases/415-section-foundation-import-excel-diperluas/415-04-SUMMARY.md"
  modified:
    - "Controllers/AssessmentAdminController.cs"
    - "Controllers/CMPController.cs"

key-decisions:
  - "Remap SectionId via nav property newQ.Section (BUKAN newQ.SectionId = q.SectionId) — copy mentah menunjuk Section paket Pre = cross-package leak/FK violation (Pitfall 8). old->new map per-paket karena SectionNumber unik per-paket."
  - "Re-guard StartExam fire-condition: >=2 paket berisi soal DAN >=1 soal punya Section. Legacy all-null + 1-paket lolos (Pitfall 6) — re-guard TIDAK memblok ujian legacy/non-section (kompatibel-mundur keystone)."
  - "Re-guard pakai sibling key plain Title+Category+Schedule.Date (parity import 415-03), BUKAN SiblingPrePostAwarePredicate yang dipakai pemuatan paket StartExam — supaya cakupan validasi identik dengan import D-13."
  - "Re-guard jalan di jalur assignment==null (first-build) saja; assignment terkunci tak bisa di-corrupt drift retroaktif (shuffle-lock final §6.5)."

patterns-established:
  - "Sync Pre->Post: clone Section + remap via nav; opsi 5-6 gratis dari iterasi q.Options"
  - "Exam-start structural integrity guard: server-side, sebelum shuffle, skip-on-legacy"

requirements-completed: [SEC-06, SEC-04]

# Metrics
duration: 11min
completed: 2026-06-22
---

# Phase 415 Plan 04: Section Sync Pre->Post + StartExam D-13 Re-guard Summary

**Dua seam terakhir fondasi Section: (1) `SyncPackagesToPost` kini deep-clone record `AssessmentPackageSection` + remap `SectionId` soal ke section paket Post via navigation property (no cross-package leak) + opsi 5-6 (E/F) ikut tersalin otomatis (SEC-06, satu body tunggal mewarisi 6 pemanggil), dan (2) re-guard D-13 titik #2 di `CMPController.StartExam` SEBELUM `BuildQuestionAssignment` yang memblok mulai ujian saat struktur Section antar-paket-saudara tidak identik (tangkap drift edit-manual pasca-import) sambil membiarkan ujian legacy all-null & paket-tunggal lolos (kompatibel-mundur). 6/6 test baru hijau + Shuffle keystone 48/48 + suite penuh 631/631; build 0 error; migration=FALSE. Menutup 9/9 REQ Phase 415.**

## Performance

- **Duration:** 11 min
- **Started:** 2026-06-22T13:33:59Z
- **Completed:** 2026-06-22T13:45:40Z
- **Tasks:** 3
- **Files modified:** 4 (2 created test, 2 modified controller) + 1 SUMMARY

## Accomplishments

- **SEC-06 SyncPackagesToPost Section clone** (`AssessmentAdminController.cs:6549-6629`): muat Section rows paket Pre (`AssessmentPackageSections.Where(prePkgIds.Contains).GroupBy(AssessmentPackageId)`), untuk tiap `prePkg` clone Section rows ke `newPkg` (copy `SectionNumber`/`Name`/`StartNewPage`/`ShuffleEnabled`, wire `AssessmentPackage = newPkg`), bangun `Dictionary<int oldSectionId, AssessmentPackageSection newSection>` per-paket, set `newQ.Section = sectionMap[q.SectionId.Value]` **via navigation property** (EF wire FK baru di SaveChanges). Soal `SectionId == null` → tetap null ("Lainnya"); soal ber-section yang section-nya tak ada di map (defensif) → biarkan null daripada cross-link.
- **Opsi 5-6 (E/F) gratis**: clone existing `q.Options.Select(o => new PackageOption {...})` mengiterasi SELURUH koleksi opsi tanpa batas A-D → opsi E/F ikut tersalin otomatis (ditambah komentar kode penegas). Dibuktikan test: soal 6-opsi Pre → 6 opsi Post.
- **Stale-section guard sudah ada (Wave-1)**: blok penghapusan paket Post lama (`6536-6545`) ber-`RemoveRange` Section Post lama sebelum clone (FK Section->Package=Restrict) → re-sync tak meninggalkan Section yatim. Diverifikasi test re-sync 2x: tepat 2 Section, 0 orphan.
- **Audit 6 pemanggil sync (Pitfall 7 — VERIFIED, bukan asumsi)**: `grep SyncPackagesToPost` → 1 deklarasi (`:6522`) + 6 call site: **`:6605`** (CopyPackagesFromPre), **`:6646`**, **`:6741`**, **`:7627`**, **`:7905`**, **`:7998`**. Semua route lewat body tunggal → semua mewarisi clone Section tanpa logika per-caller.
- **SEC-04 #2 StartExam re-guard** (`CMPController.cs:1067-1118`, di dalam `if (packages.Any())` → `if (assignment == null)`, SEBELUM `BuildQuestionAssignment` @1126): resolve paket saudara (sibling key plain `Title+Category+Schedule.Date`) dgn `.ThenInclude(q => q.Section)`, fire HANYA bila `guardPackages.Count >= 2 && guardAnySections` (>=1 soal punya Section), bandingkan count per `SectionNumber` (null->Lainnya via `q.Section?.SectionNumber`) paket-referensi-pertama vs tiap saudara; drift → `TempData["Error"] = "Ujian tidak dapat dimulai: struktur Section antar-paket tidak identik. Hubungi HC untuk memperbaiki paket soal."` + `RedirectToAction("Assessment")`, JANGAN bangun assignment/shuffle. Server-authoritative.
- **Kompatibel-mundur keystone (Pitfall 6)**: legacy all-null SectionId + paket-tunggal TIDAK terblok. Dibuktikan test (c) legacy all-null total beda (3 vs 2) tetap lolos + (d) paket-tunggal ber-section lolos.
- **SectionSyncPrePostTests** (2 test, Integration SQLEXPRESS): drive REAL `CopyPackagesFromPre` (memanggil private `SyncPackagesToPost`). (1) clone Section + remap SectionId ke section POST (assert `AssessmentPackageId == postPkg.Id` + SectionNumber asal) + opsi E/F + Lainnya null + NO Pre-id leak; (2) re-sync 2x → 1 paket, 2 Section, 0 orphan.
- **SectionMismatchGuardTests** (4 test, Integration SQLEXPRESS): drive REAL `StartExam`. (a) Section 1 = 3 vs 2 soal → blok + pesan re-guard + 0 assignment; (b) count cocok per-Section → lolos + assignment ter-build; (c) legacy all-null → lolos; (d) paket-tunggal → lolos. De-tautology: guard ASLI yang memblok/melewatkan (no replica compare).

## Task Commits

Each task committed atomically:

1. **Task 1: SyncPackagesToPost clone Section + remap SectionId + opsi 5-6 (SEC-06)** - `d5d7084c` (feat)
2. **Task 2: StartExam D-13 re-guard before BuildQuestionAssignment (SEC-04 #2)** - `aae3e6c0` (feat)
3. **Task 3: SectionSyncPrePostTests + SectionMismatchGuardTests** - `c280142e` (test)

**Plan metadata:** _(final commit)_ `docs(415-04)`

_migration=FALSE — `git diff Migrations/ Data/` kosong. Notify IT tetap hanya 415-01 (`AddAssessmentPackageSection`, hash `2391257c`, migration=TRUE)._

### Verified SyncPackagesToPost callers (Pitfall 7)
```
Deklarasi:  AssessmentAdminController.cs:6522  private async Task SyncPackagesToPost(int preSessionId, int postSessionId)
Caller 1:   :6605  CopyPackagesFromPre (tombol HC "salin dari Pre")
Caller 2:   :6646
Caller 3:   :6741
Caller 4:   :7627
Caller 5:   :7905
Caller 6:   :7998
```
Body tunggal → semua 6 caller mewarisi clone Section. TIDAK ada logika per-caller.

### Remap mechanism (anti cross-package leak)
```csharp
var sectionMap = new Dictionary<int, AssessmentPackageSection>();  // old SectionId -> new Section (per-paket)
foreach (var s in preSections) {
    var newSection = new AssessmentPackageSection { AssessmentPackage = newPkg, SectionNumber = s.SectionNumber,
        Name = s.Name, StartNewPage = s.StartNewPage, ShuffleEnabled = s.ShuffleEnabled };
    _context.AssessmentPackageSections.Add(newSection);
    sectionMap[s.Id] = newSection;
}
// ...
if (q.SectionId.HasValue && sectionMap.TryGetValue(q.SectionId.Value, out var mappedSection))
    newQ.Section = mappedSection;   // nav property → EF wire FK baru (BUKAN newQ.SectionId = q.SectionId)
```

### StartExam re-guard skip-condition (backward-compat)
- Fire HANYA bila `guardPackages.Count >= 2` (paket berisi soal) **DAN** `guardAnySections` (>=1 soal `SectionId != null`).
- Legacy all-null SectionId → `guardAnySections=false` → skip (lolos).
- Paket tunggal → `Count < 2` → skip (lolos).
- Jalan di jalur `assignment == null` (first-build) saja, SEBELUM `ShuffleEngine.BuildQuestionAssignment`.

## Files Created/Modified

- `Controllers/AssessmentAdminController.cs` — `SyncPackagesToPost` body: muat Pre Section rows (group per package), clone Section ke paket Post + bangun old->new section map, set `newQ.Section` via nav property, komentar penegas opsi 5-6 carried.
- `Controllers/CMPController.cs` — `StartExam`: re-guard D-13 titik #2 di dalam `if (assignment == null)` SEBELUM `BuildQuestionAssignment` (sibling resolve + per-Section count compare + skip-on-legacy + blok-with-message).
- `HcPortal.Tests/SectionSyncPrePostTests.cs` — 2 Integration test (drive CopyPackagesFromPre ASLI).
- `HcPortal.Tests/SectionMismatchGuardTests.cs` — 4 Integration test (drive StartExam ASLI; stub ISession non-impersonating).

## Decisions Made

- **Remap via nav property** (`newQ.Section`) bukan `newQ.SectionId = q.SectionId` — copy mentah cross-link ke Section paket Pre (Pitfall 8). Map berkunci old SectionId, per-paket (SectionNumber unik per-paket; nilai Id global → map by Id paling aman).
- **Re-guard sibling key = plain Title+Category+Schedule.Date** (parity import 415-03 LOCKED), BUKAN `SiblingPrePostAwarePredicate` yang dipakai pemuatan paket StartExam. Cakupan validasi re-guard = cakupan validasi import D-13 → konsisten (Phase 397 lesson: grouping key standalone WAJIB == service write key).
- **Re-guard di jalur first-build (assignment==null)**: assignment terkunci tak bisa di-corrupt drift retroaktif (shuffle-lock §6.5). Guard melindungi pembentukan assignment baru.
- **Test harness StartExam**: stub `ISession` (Mode unset → `ImpersonationService.IsImpersonating()==false` → `UseRealUser`); seed sesi `InProgress` + `StartedAt != null` (`justStarted=false`) → lewati jalur write-on-GET/broadcast/`LogActivityAsync` (yang butuh scopeFactory), fokus murni ke re-guard.

## Deviations from Plan

None — plan dieksekusi persis seperti tertulis. Ketiga task selesai sesuai action; tidak ada bug auto-fix (Rule 1-3) maupun keputusan arsitektural (Rule 4). Tidak ada auth gate. Stale-section RemoveRange (Task 1 langkah 2) sudah dikerjakan Wave-1 (`6536-6545`) — dikonfirmasi ada (tidak diduplikasi), diverifikasi via test re-sync.

## Issues Encountered

None — build 0 error sekali jalan tiap task; 6/6 test baru hijau iterasi pertama (harness mirror SectionCrudTests + NoopHubContext + ImpersonationService stub-session).

## Known Stubs

None — clone Section + remap + opsi 5-6 + re-guard semua di-wire ke jalur produksi nyata. Render/grading huruf opsi E/F memang dijadwalkan Phase 418 (data E/F sudah DISIMPAN + ikut clone sekarang; bukan stub).

## Threat Flags

None — tidak ada surface keamanan baru di luar threat_model plan. SEC-06 remap server-side via nav (T-415-15 no cross-leak — test assert no Pre-id leak); re-guard server-authoritative sebelum shuffle (T-415-17), skip-on-legacy mencegah over-broad block (T-415-16 Pitfall 6), stale Post sections di-RemoveRange (T-415-18, Wave-1).

## User Setup Required

None - no external service configuration required.

## TDD Gate Compliance

Task 3 (`tdd="true"`) adalah Integration controller-driven atas perilaku yang dibangun Task 1-2 (`feat` mendahului `test` — gate ordering terpenuhi: `d5d7084c`/`aae3e6c0` sebelum `c280142e`). RED murni-baru tidak applicable karena seam clone (SyncPackagesToPost) + re-guard (StartExam) sudah hidup saat test ditulis; test berfungsi sebagai gate kunci yang mengunci kontrak SEC-06 (deep-clone remap + no leak + opsi E/F + no stale) dan SEC-04 #2 (block-on-drift + legacy/single-pkg pass). Konsisten dengan TDD compliance Plan 01/02/03 (behavior layer-dalam mendahului test integration).

## Next Phase Readiness

- **9/9 REQ Phase 415 LENGKAP** lintas Plan 01-04: SEC-01 (01), SEC-02/03/05 (02), IMP-01/02/03 + SEC-04 titik #1 (03), **SEC-06 + SEC-04 titik #2 (04)**. Fondasi Section siap dikonsumsi 416 (scoped shuffle), 417 (pagination), 418 (opsi dinamis render), 419 (export + UAT milestone).
- **migration=FALSE** Plan 04 — notify IT tetap hanya Plan 01 (`AddAssessmentPackageSection` `2391257c`, migration=TRUE).
- **Untuk Phase 416 (scoped shuffle):** `SyncPackagesToPost` sekarang menjaga struktur Section identik Pre<->Post (fondasi LinkPrePost 397 + pooling per-section D-09). `BuildSectionQuestionAssignment` 416 bisa mengandalkan Section count identik antar-paket-saudara (di-enforce re-guard StartExam + import D-13).
- **Untuk Phase 419 (UAT):** re-guard StartExam = jalur Razor/SignalR-adjacent → real-browser Playwright UAT WAJIB (lesson 354): buat 2 paket saudara dgn drift Section manual → verifikasi blok di browser + pesan BI; verifikasi ujian legacy/non-section tetap mulai normal.
- **Lesson:** remap FK saat deep-clone WAJIB via navigation property (`newQ.Section = newSection`), bukan copy SectionId mentah — EF wire FK baru di SaveChanges; copy mentah = cross-package leak. Re-guard exam-start WAJIB skip-on-legacy (>=2 paket DAN >=1 section) agar tak memblok ujian non-section.

## Self-Check: PASSED
- Files created: 2/2 found (SectionSyncPrePostTests.cs + SectionMismatchGuardTests.cs)
- Files modified: 2/2 found (AssessmentAdminController.cs + CMPController.cs)
- Commits: 3/3 found (d5d7084c, aae3e6c0, c280142e)
- Tests: SectionSyncPrePost 2/2 + SectionMismatchGuard 4/4 (= 6/6 new green); Shuffle keystone 48/48 green; full suite 631/631 green; build 0 error; migration=FALSE

---
*Phase: 415-section-foundation-import-excel-diperluas*
*Completed: 2026-06-22*
