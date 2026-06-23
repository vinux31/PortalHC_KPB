# Phase 422: SamePackage & Shuffle Integrity - Context

**Gathered:** 2026-06-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Tutup 7 lubang integritas **SamePackage** (komparasi paket-soal Pre↔Post identik) + **shuffle** atas mesin Pre-Post + ShuffleEngine yang sudah ada — hardening integritas, bukan fitur baru. Cakupan: auto-sync SamePackage di jalur Import (bocor), toggle SamePackage editable pasca-create + sync/unsync + guard pra-peserta, lock SamePackage server-side (bukan view-only), pewarisan SamePackage ke peserta baru, penomoran paket unik+deterministik, kunci sibling lock/save shuffle type-aware, dan peringatan shuffle lengkap (ON+SamePackage, K=min truncation, mismatch satu sumber).

**In-scope:** SHFX-01..07. Domain kode: `Controllers/AssessmentAdminController.cs` (ManagePackages area: `ImportPackageQuestions` ~`:6117-6484`, `CreatePackage` ~`:5876/5894`, `DeletePackage` ~`:5917/5992`, `CreateQuestion` ~`:6543/6691-6704`, `EditQuestion` ~`:6964`, `DeleteQuestion` ~`:7002/7057`, `SyncPackagesToPost` helper ~`:5793`, lock view-only ~`:5730`, sibling key lock/save ~`:5559-5564/5733-5738`, mismatch count ~`:5763-5774`, tambah-peserta newPost D-31 ~`:1968-1989`), `Helpers/ShuffleEngine.cs` (~`:117` K=min), `Helpers/ShuffleToggleRules.cs` (~`:18-20` warning OFF-only), `Helpers/SiblingSessionQuery.cs` (type-aware predicate), `Views/Admin/ManagePackages.cshtml` (~`:34-35` banner, `:72-78` mismatch view-dup, `:317-320` tombol Kelola Soal), `Views/Admin/ManagePackageQuestions.cshtml`. **migration=TRUE** (filtered unique index `(AssessmentSessionId, PackageNumber)` — keputusan D-05c).

**Out-of-scope (fase lain):** E-01 shuffle reset-OFF di Edit + FORM-PP letak SamePackage (sudah/akan di **420** FORM); aturan terbit cert (423); grading/gating Pre→Post (424); FLOW-10 write-on-GET StartExam side-effect + cosmetic/naming (425). Tidak menambah strategi shuffle baru, tidak menyentuh Scoped-Shuffle v32.6.
</domain>

<decisions>
## Implementation Decisions

### SHFX-02 — Toggle SamePackage pasca-create (FLOW-07, keputusan bisnis b)
- **D-01 (sync + guard no-started):** HC boleh ubah `SamePackage` pada grup Pre-Post existing.
  - **ON-kan:** overwrite paket Post dgn deep-clone dari Pre (via `SyncPackagesToPost`) + pasang lock.
  - **OFF-kan:** lepas lock; **pertahankan** paket hasil clone terakhir (jadi editable, tidak dikosongkan/revert).
  - **Guard pra-peserta:** tolak toggle bila ADA peserta yang **sudah mulai** di grup (StartedAt set / InProgress / Completed). Peserta yang **belum mulai** = boleh. Toast `TempData` non-blocking saat ditolak. Konsisten "guard sebelum peserta mulai" (ROADMAP) + pola confirm-before fase 421.

### SHFX-05 — PackageNumber unik + deterministik (SHUF-ISS-08)
- **D-02 (MAX+1 + ThenBy(Id) + filtered unique index → migration=TRUE):**
  - `CreatePackage` ganti count-based (`existingCount+1`) → **`MAX(PackageNumber)+1` per session** (anti-duplikat; gap nomor dibiarkan — pembagian round-robin pakai urutan, bukan nilai).
  - Tambah **`.ThenBy(p => p.Id)`** di SEMUA query `OrderBy(PackageNumber)` (OFF-path round-robin deterministik lintas reshuffle).
  - Tambah **filtered unique index `(AssessmentSessionId, PackageNumber)`** (filter `WHERE PackageNumber IS NOT NULL` bila nullable) sbg jaring pengaman level-DB. **migration=TRUE.**
  - ⚠️ **PRA-MIGRATION WAJIB:** data existing bisa sudah punya PackageNumber duplikat (akibat bug count-based) → migration HARUS dedup/renumber baris duplikat existing SEBELUM create unique index, kalau tidak `CREATE INDEX` gagal. Planner: sertakan langkah dedup di migration (atau data-fix terpisah lebih dulu).

### SHFX-07 — Peringatan shuffle (SHUF-ISS-04/05/07)
- **D-03 (warn-only non-blocking):** Tampilkan peringatan UI saat `ShuffleQuestions=ON` pada Post `SamePackage` ("acak soal mengaburkan komparasi item-level Pre/Post; sarankan OFF") — **HC tetap boleh simpan** (non-blocking). Konsisten idiom warning existing + keputusan bisnis fleksibel.
- **D-04 (K=min truncation warning ON-path):** `ShuffleToggleRules` perluas warning agar muncul juga saat ON (teks beda: ON="soal dipangkas ke K=min"), bukan hanya OFF (`:18-20`).
- **D-05 (mismatch satu sumber):** `hasMismatch`/`referenceCount` hitung di SATU tempat (controller via ViewBag/helper `PackageSizeAnalysis.Compute`), hapus duplikasi view (`ManagePackages.cshtml:72-78` vs controller `:5763-5774`). Pola kill-drift.

### SHFX-01 + SHFX-03 — Auto-sync Import + lock server-side (SHUF-ISS-03 HIGH + SHUF-ISS-02)
- **D-06 (helper sync penuh, kill-drift):** Ekstrak **`SyncToLinkedPostIfSamePackageAsync(preSessionId)`** + wire ke **6 jalur** (Import `:6483` yang saat ini BOCOR + 5 existing: CopyPackagesFromPre, CreatePackage, DeletePackage, CreateQuestion, EditQuestion, DeleteQuestion). Hilangkan 6 sumber sync terpisah → satu sumber kebenaran. Selaras pola kill-drift fase 420/421.
- **D-07 (lock tolak-keras server-side):** Ekstrak **`IsSessionEditLocked(session)`** (return true bila `AssessmentType=="PostTest" && SamePackage`) + guard di awal **5 endpoint POST** (CreatePackage, DeletePackage, CreateQuestion, EditQuestion, DeleteQuestion) → **tolak keras** dgn `TempData["Error"]` + redirect (bukan hanya sembunyikan tombol). Tetap sembunyikan/disable tombol "Kelola Soal" di view (`ManagePackages.cshtml:317-320`) utk UX. Server-authoritative.

### Claude's Discretion
- **SHFX-04 (PA-02):** newPost pada tambah-peserta D-31 (`:1988`) warisi `SamePackage = repPost.SamePackage` — mekanis, ikut fix audit.
- **SHFX-06 (SHUF-ISS-01):** ganti kunci sibling type-agnostic (`:5559-5564`, `:5733-5738`) → `SiblingPrePostAwarePredicate`/`SiblingSessionQuery` type-aware; koreksi komentar salah `:5558`. Selama selaras StartExam/Reshuffle.
- Nama/posisi final helper (`SyncToLinkedPostIfSamePackageAsync`, `IsSessionEditLocked`, `PackageSizeAnalysis`), presisi teks peringatan/toast (Bahasa Indonesia, idiom TempData existing), bentuk filtered index — diskresi asal invariant terjaga.
- **Backward-compat WAJIB:** grup Pre-Post existing tanpa toggle SamePackage tak berubah perilaku; Assessment Standard (non Pre-Post) tak tersentuh lock/sync.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Audit sumber (temuan + bukti file:line + fix)
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` §5.2.4 (~`:293-296` SHUF-ISS-03 HIGH Import bocor; `:345` PA-02; `:351-354` SHUF-ISS-02 lock; `:356` SHUF-ISS-08 PackageNumber; `:414-420` SHUF-ISS-01/04/05/07; `:426` FLOW-07 toggle)
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` §5.2.2 (tabel field SamePackage `:210`, Shuffle `:215-216`) — peran field + relasi
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` §6 P3 (~`:466-468`) — grouping fase 422 (9 temuan)

### Requirements & roadmap
- `.planning/REQUIREMENTS.md` — SHFX-01..07 (baris 33-41, traceability 108-114) + keputusan bisnis a/b/c (baris 5)
- `.planning/ROADMAP.md` — Phase 422 (goal + success criteria; flag OVERLAP v32.6)

### Spec mesin SamePackage/shuffle
- `Models/AssessmentSession.cs` (~`:201` invariant "package otomatis disalin dari Pre-Test") — kontrak yang dilanggar SHUF-ISS-03
- `Helpers/SiblingSessionQuery.cs` — predicate type-aware kanonik (acuan SHFX-06)

### Out-of-scope (jangan duplikasi)
- v32.6 branch `main` (Scoped Shuffle, fase 415-419) — OVERLAP shuffle; rekonsiliasi saat merge, JANGAN tarik/duplikasi di ITHandoff.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`SyncPackagesToPost`** (`AssessmentAdminController.cs:~5793`) — deep-clone Pre→Post existing; basis helper `SyncToLinkedPostIfSamePackageAsync` (D-06). Pola guard sync ada di CreateQuestion `:6691-6704` (`AssessmentType=="PreTest" && linkedPost.SamePackage` → sync) — replikasi ke Import.
- **`ShuffleToggleRules`** (`Helpers/ShuffleToggleRules.cs`) — pure-rules warning (saat ini OFF-only `:18-20`); perluas ke ON (D-04). Pola kill-drift sama `RetakeRules`/`RetakeCountingRules` (fase 420/421).
- **`SiblingSessionQuery.cs`** — predicate type-aware kanonik untuk lock/save sibling (SHFX-06).
- **`ShuffleEngine.cs:~117`** — K=min truncation (sumber kebenaran pemangkasan; warning-nya D-04).

### Established Patterns
- **Pure/helper kill-drift:** `RetakeRules`/`RetakeCountingRules`/`ShuffleToggleRules` — caller suplai fakta, keputusan terpusat. D-06 (`SyncToLinkedPostIfSamePackageAsync`) + D-07 (`IsSessionEditLocked`) + D-05 (`PackageSizeAnalysis`) ikut pola ini.
- **Toast non-blocking:** `TempData["Error"]`/`TempData["Warning"]` (guard + warning).
- **Server-authoritative guard:** tolak di endpoint POST, bukan hanya sembunyikan tombol view (selaras lock fase 421 RTH-04).
- **Konvensi +7h WIB** (fase 421) — bila guard toggle butuh perbandingan waktu peserta mulai, reuse.

### Integration Points
- `ImportPackageQuestions` terminal `return RedirectToAction` `:6483` — sisipkan sync SEBELUM return.
- 5 endpoint POST paket/soal — guard `IsSessionEditLocked` di awal.
- newPost tambah-peserta `:1988` — set SamePackage warisan.
- Migration filtered unique index → koordinasi IT deploy (flag notify-IT akhir milestone); pra-migration dedup existing PackageNumber.
</code_context>

<specifics>
## Specific Ideas

- **SHUF-ISS-03 = HIGH:** jalur Import paling lazim dipakai HC (impor bank-soal massal); bocor senyap → Post jalankan paket lama/kosong, komparasi Pre/Post gagal tanpa peringatan. Prioritas tutup.
- **Toggle SamePackage (D-01):** pertahankan paket clone saat OFF (jangan kosongkan) — HC mungkin mau lanjut edit manual paket Post setelah lepas lock.
- **Migration risk (D-02):** filtered unique index TAK boleh di-apply sebelum data existing dedup — planner wajib langkah dedup di migration/data-fix. Verifikasi lokal: cek tak ada `(AssessmentSessionId, PackageNumber)` duplikat via `sqlcmd -C -I` sebelum & sesudah.
- **Overlap v32.6:** shuffle disentuh juga di branch main (Scoped Shuffle) — catat tiap perubahan shuffle untuk rekonsiliasi merge; jangan asumsikan kode shuffle ITHandoff == main.
</specifics>

<deferred>
## Deferred Ideas

- Strategi shuffle baru (per-attempt rotation, weighted), Scoped-Shuffle (v32.6 branch main) — out-of-scope.
- FLOW-10 write-on-GET StartExam side-effect → fase 425 (cleanup).
- E-01 shuffle reset-OFF di Edit + FORM-PP-01 letak SamePackage di form → fase 420 (FORM).
- None lain — diskusi tetap dalam batas 7 REQ SHFX.
</deferred>

---

*Phase: 422-samepackage-shuffle-integrity*
*Context gathered: 2026-06-23*
