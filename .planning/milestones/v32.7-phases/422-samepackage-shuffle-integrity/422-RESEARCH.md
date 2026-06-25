# Phase 422: SamePackage & Shuffle Integrity - Research

**Researched:** 2026-06-23
**Domain:** ASP.NET Core 8 MVC + Razor + Bootstrap 5 + EF Core 8 + SQL Server (SQLEXPRESS) — controller hardening + 1 EF migration (filtered unique index + pra-migration dedup)
**Confidence:** HIGH (semua klaim diverifikasi langsung dari source ITHandoff via Read/Grep; tidak ada library baru)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (SHFX-02 toggle SamePackage pasca-create):** HC boleh ubah `SamePackage` pada grup Pre-Post existing.
  - **ON-kan:** overwrite paket Post dgn deep-clone dari Pre (via `SyncPackagesToPost`) + pasang lock.
  - **OFF-kan:** lepas lock; **pertahankan** paket hasil clone terakhir (tidak dikosongkan/revert).
  - **Guard pra-peserta:** tolak toggle bila ADA peserta yang **sudah mulai** di grup (StartedAt set / InProgress / Completed). Belum-mulai = boleh. Toast `TempData` non-blocking saat ditolak.
- **D-02 (SHFX-05 PackageNumber unik + deterministik → migration=TRUE):**
  - `CreatePackage` ganti count-based (`existingCount+1`) → **`MAX(PackageNumber)+1` per session** (gap nomor dibiarkan).
  - Tambah **`.ThenBy(p => p.Id)`** di SEMUA query `OrderBy(PackageNumber)`.
  - Tambah **filtered unique index `(AssessmentSessionId, PackageNumber)`** sebagai jaring pengaman DB-level. **migration=TRUE.**
  - ⚠️ **PRA-MIGRATION WAJIB:** dedup/renumber baris PackageNumber duplikat existing SEBELUM create unique index (else `CREATE INDEX` gagal).
- **D-03 (SHFX-07 warn-only non-blocking):** peringatan UI saat `ShuffleQuestions=ON` pada Post `SamePackage` — HC tetap boleh simpan (non-blocking).
- **D-04 (SHFX-07 K=min truncation warning ON-path):** `ShuffleToggleRules` perluas warning agar muncul juga saat ON (teks beda: ON="soal dipangkas ke K=min").
- **D-05 (SHFX-07 mismatch satu sumber):** `hasMismatch`/`referenceCount` hitung di SATU tempat (controller via ViewBag/helper `PackageSizeAnalysis.Compute`), hapus duplikasi view (`ManagePackages.cshtml:72-78`).
- **D-06 (SHFX-01 helper sync penuh, kill-drift):** ekstrak `SyncToLinkedPostIfSamePackageAsync(preSessionId)` + wire ke **6 jalur** (Import `:6483` BOCOR + CopyPackagesFromPre, CreatePackage, DeletePackage, CreateQuestion, EditQuestion, DeleteQuestion).
- **D-07 (SHFX-03 lock tolak-keras server-side):** ekstrak `IsSessionEditLocked(session)` (true bila `AssessmentType=="PostTest" && SamePackage`) + guard di awal **5 endpoint POST** (CreatePackage, DeletePackage, CreateQuestion, EditQuestion, DeleteQuestion) → tolak keras `TempData["Error"]` + redirect. Tetap sembunyikan/disable tombol di view.

### Claude's Discretion
- **SHFX-04 (PA-02):** newPost tambah-peserta D-31 (`:1988`/aktual `:2024-2045`) warisi `SamePackage = repPost.SamePackage` — mekanis.
- **SHFX-06 (SHUF-ISS-01):** ganti kunci sibling type-agnostic (`:5630-5635`, `:5704-5706`) → `SiblingSessionQuery.SiblingPrePostAwarePredicate` type-aware; koreksi komentar salah `:5629` ("key identik StartExam/Reshuffle").
- Nama/posisi final helper, presisi teks peringatan/toast (Bahasa Indonesia, idiom TempData existing), bentuk filtered index — diskresi asal invariant terjaga.
- **Backward-compat WAJIB:** grup Pre-Post existing tanpa toggle SamePackage tak berubah perilaku; Assessment Standard (non Pre-Post) tak tersentuh lock/sync.

### Deferred Ideas (OUT OF SCOPE)
- Strategi shuffle baru (per-attempt rotation, weighted), Scoped-Shuffle (v32.6 branch main).
- FLOW-10 write-on-GET StartExam side-effect → fase 425.
- E-01 shuffle reset-OFF di Edit + FORM-PP-01 letak SamePackage di form → fase 420 (FORM).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SHFX-01 | Import Excel ke paket Pre ber-SamePackage memicu sync otomatis ke Post [SHUF-ISS-03, HIGH] | `ImportPackageQuestions` terminal `:6480-6486` TIDAK panggil `SyncPackagesToPost` (verified). Pola sync ada di 6 call-site lain (`:5948/5989/6074/6783/7046/7139`). Ekstrak `SyncToLinkedPostIfSamePackageAsync` + wire 6 jalur. |
| SHFX-02 | Toggle SamePackage pasca-create + sync/unsync + guard pra-peserta [FLOW-07] | `SamePackage` saat ini param Create-only (`:870`, `:1309/1356`); 9 read pasca-create. Pola toggle = `UpdateShuffleSettings` endpoint (`:5623`). Guard pra-peserta = `StartedAt != null` / status (pola `:5546-5553`, `:5638-5641`). Deep-clone via `SyncPackagesToPost` (`:5875-5933`). |
| SHFX-03 | Endpoint POST kelola paket/soal tolak edit Post terkunci SamePackage (server-side) [SHUF-ISS-02] | 5 endpoint POST (`CreatePackage:5958`, `DeletePackage:5999`, `CreateQuestion:6625`, `EditQuestion:6842`, `DeleteQuestion:7084`) tanpa cek SamePackage. Lock saat ini view-only (`:5811`). Ekstrak `IsSessionEditLocked`. |
| SHFX-04 | Peserta baru di grup Pre-Post warisi SamePackage [PA-02] | `newPost` (`:2024-2045`) TIDAK set `SamePackage` → default false (verified). Fix: `SamePackage = repPost.SamePackage`. `repPost = postGroup.First()` (`:1999`). |
| SHFX-05 | PackageNumber unik & terurut deterministik setelah hapus paket [SHUF-ISS-08] | `CreatePackage:5969-5976` count-based; 5 `OrderBy(p => p.PackageNumber)` tanpa `.ThenBy(Id)` (`:5447,5527,5572,5764,5895`). PackageNumber = `int` non-nullable, hanya index `AssessmentSessionId` ada. migration=TRUE. |
| SHFX-06 | Kunci pasangan Pre/Post lock & save shuffle type-aware [SHUF-ISS-01] | `UpdateShuffleSettings:5630-5635` + `UpdateRetakeSettings:5704-5706` + GET `:5814-5819` pakai key type-AGNOSTIC (Title/Category/Schedule.Date) ≠ `SiblingPrePostAwarePredicate` type-aware. Komentar `:5629` salah. |
| SHFX-07 | Peringatan shuffle lengkap (SamePackage+Acak ON, K=min, mismatch satu sumber) [SHUF-ISS-04/05/07] | `ShuffleToggleRules.ShouldShowSizeMismatchWarning:18-20` OFF-only. mismatch dihitung 2× (controller `:5844-5856` + view `:72-78`). `ShuffleEngine.cs:117` K=min source. |
</phase_requirements>

## Summary

Fase 422 adalah **murni hardening** terhadap mesin SamePackage/shuffle Pre-Post yang sudah ada di `AssessmentAdminController.cs` (file 7300+ baris) — TIDAK ada fitur baru, TIDAK ada library baru, dan UI bersifat aditif kecil ke `Views/Admin/ManagePackages.cshtml` (lihat `422-UI-SPEC.md`). Tujuh REQ (SHFX-01..07) menutup tujuh lubang integritas yang sudah diverifikasi file:line di audit `2026-06-22-evaluasi-pretest-posttest.md` §5.2.4/§5.3. Stack: ASP.NET Core 8 MVC, EF Core 8 (SqlServer 8.0.0), Bootstrap 5.3, SQL Server SQLEXPRESS lokal. Branch `ITHandoff`, app port lokal **5270**.

Pola arsitektur kunci yang HARUS dipatuhi adalah **kill-drift pure-helper** (sama persis dgn `ShuffleToggleRules`/`RetakeRules`/`RetakeCountingRules` yang sudah ada): tiga ekstraksi helper yang diminta D-05/D-06/D-07 (`PackageSizeAnalysis`, `SyncToLinkedPostIfSamePackageAsync`, `IsSessionEditLocked`) menggantikan duplikasi-tersebar dengan satu sumber kebenaran. `SyncPackagesToPost` (`:5875-5933`) sudah ada dan benar — D-06 hanya membungkusnya jadi helper `SyncToLinkedPostIfSamePackageAsync` yang mengandung blok-guard "Pre-Test && linkedPost.SamePackage → sync" yang saat ini di-copy-paste di 5 dari 6 call-site, lalu menambahnya ke jalur Import yang bocor.

**Risiko tertinggi = D-02 migration (SHFX-05).** `PackageNumber` adalah `int` NON-NULLABLE (verified di model + snapshot), jadi filtered index TIDAK perlu klausa `WHERE ... IS NOT NULL` — cukup unique index biasa `(AssessmentSessionId, PackageNumber)`. Tapi data existing bisa sudah punya duplikat (akibat bug count-based `existingCount+1` + DeletePackage tak renumber), sehingga migration WAJIB me-renumber duplikat via `migrationBuilder.Sql(ROW_NUMBER() OVER PARTITION)` SEBELUM `CreateIndex(unique:true)`. Codebase punya idiom pas: `AddUserUnitsTable.cs` (raw `migrationBuilder.Sql` backfill + `CreateIndex` di satu migration) dan `ApplicationDbContext.cs:232-235`/`:353-356` (fluent `.HasFilter`).

**Primary recommendation:** Wire semua tujuh REQ via tiga pure-helper baru + satu migration dedup-then-index, reuse idiom controller/view yang sudah terbukti (UpdateShuffleSettings endpoint, TempData PRG, SiblingPrePostAwarePredicate, AddUserUnitsTable migration). Mulai dengan helper-helper + migration (Wave 0 paritas test), lalu wire call-site, lalu UI aditif terakhir. JANGAN bikin abstraksi/library baru.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Toggle SamePackage + sync/unsync (D-01) | API/Backend (`AssessmentAdminController` endpoint POST baru) | View (friendly disable + confirm) | Mutasi paket + lock = server-authoritative; view hanya UX layer. |
| Lock edit server-side (D-07) | API/Backend (guard di 5 endpoint POST) | View (hide/disable tombol) | "Tolak keras" = backend; view-only lock terbukti bocor (SHUF-ISS-02). |
| Auto-sync Pre→Post (D-06) | API/Backend (helper + 6 call-site) | Database (deep-clone insert) | Invariant data integrity, tak relevan ke client. |
| PackageNumber unik (D-02) | Database (filtered unique index) | API/Backend (MAX+1 + ThenBy(Id)) | Index = jaring pengaman DB-level; app logic = pencegahan utama. |
| Peringatan shuffle (D-03/D-04/D-05) | API/Backend (pure-rules `ShuffleToggleRules`/`PackageSizeAnalysis` → ViewBag) | View (render alert) | Keputusan warning = pure logic terpusat; view render saja (kill-drift). |
| Pewarisan SamePackage peserta baru (D-04) | API/Backend (`newPost` init `:2024-2045`) | — | Konstruksi entity = backend. |
| Sibling key type-aware (D-06/SHFX-06) | API/Backend (`SiblingSessionQuery` predicate) | — | Query EF, tak ada UI. |

## Standard Stack

### Core (semua SUDAH terpasang — TIDAK ada install baru)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 (SDK 8.0.418) | Controller + Razor view | Stack existing seluruh app `[VERIFIED: dotnet --version + .csproj]` |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | ORM + migration | Existing; semua migration pakai ini `[VERIFIED: HcPortal.Tests.csproj:13]` |
| Bootstrap | 5.3 | UI (alert/form-switch/badge/modal) | Existing layout `[CITED: 422-UI-SPEC.md:27]` |
| Bootstrap Icons + Font Awesome 6.5 | — | `bi-*` icon | Existing `[CITED: 422-UI-SPEC.md:28]` |

### Supporting (test — SUDAH terpasang)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit | 2.9.3 | Unit + integration test | Semua test fase 422 `[VERIFIED: HcPortal.Tests.csproj:15]` |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner | `[VERIFIED: .csproj:14]` |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Real-SQLEXPRESS fixture (integration) | Fixture seperti `RetakeServiceFixture`/`ProtonCompletionFixture` `[VERIFIED: .csproj:13]` |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | (tersedia, jarang dipakai untuk integrity test) | Pure-helper test tak butuh DB sama sekali `[VERIFIED: .csproj:12]` |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Filtered/plain unique index via fluent `HasIndex` | `migrationBuilder.Sql("CREATE UNIQUE INDEX...")` raw | Fluent lebih konsisten dgn snapshot EF (auto-tracked); raw SQL hanya bila EF tak bisa ekspresikan. Untuk `(AssessmentSessionId, PackageNumber)` plain-unique, **gunakan fluent** (`entity.HasIndex(p => new { p.AssessmentSessionId, p.PackageNumber }).IsUnique()`). |
| Dedup via app-code loop sebelum migration | `migrationBuilder.Sql(ROW_NUMBER OVER PARTITION)` di Up() | Raw SQL di migration = atomik + ter-version + jalan di setiap env (Dev/Prod). **Gunakan raw SQL dalam migration** (idiom `AddUserUnitsTable.cs:51-57`). |
| `DateTime.Now`/`TimeZoneInfo` untuk guard waktu | `nowUtc.AddHours(7)` verbatim | Konvensi +7h WIB SATU tempat (kill-drift). **Hanya relevan bila guard toggle butuh banding waktu** — D-01 guard berbasis status/StartedAt-presence, BUKAN waktu, jadi +7h kemungkinan TIDAK diperlukan di sini. |

**Installation:** Tidak ada. Semua dependency sudah ada. Migration dibuat via:
```bash
# Branch ITHandoff, dari root project
dotnet ef migrations add AddPackageNumberUniqueIndex
# Lalu EDIT file migration Up() untuk sisipkan dedup SQL SEBELUM CreateIndex (lihat Code Examples)
dotnet build
# Verifikasi lokal: dotnet ef database update (DB lokal SQLEXPRESS)
```

**Version verification:** `[VERIFIED: dotnet --version → 8.0.418]`. `[VERIFIED: HcPortal.Tests.csproj → EFCore.SqlServer 8.0.0, xunit 2.9.3, Test.Sdk 17.13.0]`. Tidak ada paket dari npm/registry eksternal yang perlu di-`npm view` — ini proyek .NET dgn dependency terkunci.

## Architecture Patterns

### System Architecture Diagram

```
                          ┌─────────────────────────────────────────────────┐
   HC (browser)           │  Views/Admin/ManagePackages.cshtml               │
   ──────────────────────▶│  + ManagePackageQuestions.cshtml (friendly UI)   │
   POST forms             │  - SamePackage toggle (form-switch + confirm)    │
   (antiforgery)          │  - shuffle/retake cards · lock banner · warnings │
                          └───────────────┬─────────────────────────────────┘
                                          │ POST (antiforgery)        ▲ GET render (ViewBag)
                                          ▼                           │
   ┌──────────────────────────────────────────────────────────────────────────────┐
   │  AssessmentAdminController (ASP.NET Core 8 MVC)                                 │
   │                                                                                │
   │  [POST] ToggleSamePackage (NEW, SHFX-02/D-01) ──┐                              │
   │   guard: IsSessionEditLocked? anyStarted?       │                              │
   │   ON → SyncToLinkedPostIfSamePackageAsync + lock │                              │
   │   OFF→ unset SamePackage (KEEP cloned pkgs)      │                              │
   │                                                  ▼                              │
   │  [POST] CreatePackage/DeletePackage ───▶ IsSessionEditLocked(session)? ──reject │
   │  [POST] CreateQuestion/EditQuestion/  ──▶  (D-07 guard di AWAL endpoint)        │
   │         DeleteQuestion                                                          │
   │       │ on success ──▶ SyncToLinkedPostIfSamePackageAsync(preSessionId) (D-06)  │
   │  [POST] ImportPackageQuestions ─────────┘  (jalur BOCOR — sekarang di-wire)     │
   │                                                                                │
   │  [POST] UpdateShuffleSettings / UpdateRetakeSettings                            │
   │       │ sibling key ──▶ SiblingPrePostAwarePredicate (SHFX-06, type-aware)      │
   │                                                                                │
   │  [GET]  ManagePackages ──▶ PackageSizeAnalysis.Compute (D-05, ViewBag) +        │
   │         ShuffleToggleRules.* (D-03/D-04 warning) + IsSessionEditLocked          │
   └──────────────────────────────┬────────────────────────────┬────────────────────┘
              pure helpers ────────┤                            │ EF Core 8
   ┌──────────────────────────────▼──────────┐    ┌─────────────▼─────────────────────┐
   │ Helpers/ (pure, EF-free, unit-testable)  │    │ SQL Server (SQLEXPRESS)           │
   │  · ShuffleToggleRules (extend D-04)      │    │  AssessmentSessions (SamePackage) │
   │  · PackageSizeAnalysis (NEW, D-05)       │    │  AssessmentPackages               │
   │  · SiblingSessionQuery (reuse, SHFX-06)  │    │   + UNIQUE(SessionId,PackageNum)  │
   │  · ShuffleEngine (K=min source, read)    │    │   (NEW index, D-02 + dedup)       │
   │ Controller helpers (EF-aware):           │    │  PackageQuestions / PackageOptions│
   │  · SyncToLinkedPostIfSamePackageAsync    │    │  UserPackageAssignments           │
   │  · IsSessionEditLocked                    │    │  (migration: dedup→CreateIndex)   │
   └──────────────────────────────────────────┘    └───────────────────────────────────┘
```

Alur primer (HC import bank-soal ke Pre ber-SamePackage): HC POST Import ke paket Pre → controller validasi+insert soal → on success panggil `SyncToLinkedPostIfSamePackageAsync(preSessionId)` → helper cek `Pre && linkedPost.SamePackage` → `SyncPackagesToPost` deep-clone Pre→Post → SaveChanges → redirect. Sebelum fix, langkah sync absen → Post stale (SHUF-ISS-03 HIGH).

### Recommended Project Structure (file yang disentuh — semua EXISTING kecuali ditandai NEW)
```
Controllers/
└── AssessmentAdminController.cs   # 5 endpoint POST + toggle baru + 6 wire sync + sibling key
Helpers/
├── ShuffleToggleRules.cs          # EXTEND: warning K=min ON-path (D-04)
├── PackageSizeAnalysis.cs         # NEW: Compute(packages) → (hasMismatch, referenceCount, packagesWithQuestions) (D-05)
├── SiblingSessionQuery.cs         # REUSE: SiblingPrePostAwarePredicate (SHFX-06)
└── ShuffleEngine.cs               # READ-ONLY ref: K=min source :117 (jangan ubah logika)
Views/Admin/
├── ManagePackages.cshtml          # toggle card + lock + warnings + hapus mismatch dup :72-78
└── ManagePackageQuestions.cshtml  # friendly disable layer (server sudah hard-reject)
Models/
└── AssessmentPackage.cs           # (opsional) anotasi index via fluent di ApplicationDbContext
Data/
└── ApplicationDbContext.cs        # NEW: HasIndex (AssessmentSessionId, PackageNumber).IsUnique()
Migrations/
└── <ts>_AddPackageNumberUniqueIndex.cs  # NEW: dedup SQL → CreateIndex (migration=TRUE)
HcPortal.Tests/
├── PackageSizeAnalysisTests.cs    # NEW pure-unit (D-05)
├── ShuffleToggleRulesTests.cs     # EXTEND (D-04 ON-path warning)
├── SamePackageToggleGuardTests.cs # NEW integration (D-01 guard)
├── SamePackageSyncTests.cs        # NEW integration (D-06 6-jalur, esp Import)
├── SessionEditLockTests.cs        # NEW integration/unit (D-07)
├── PackageNumberUniqueTests.cs    # NEW integration (D-02 MAX+1 + index)
└── PackageNumberMigrationTests.cs # NEW integration (dedup renumber)
```

### Pattern 1: Pure-helper kill-drift (D-04, D-05)
**What:** Logika keputusan/komputasi hidup di SATU static class pure (EF-free), dipanggil dari GET (ViewBag) DAN POST (guard) supaya tak divergen.
**When to use:** Setiap kali fakta dihitung di >1 tempat (saat ini mismatch dihitung 2×: controller + view).
**Example:**
```csharp
// Source: Helpers/ShuffleToggleRules.cs:18-20 (existing, EXTEND untuk D-04)
public static bool ShouldShowSizeMismatchWarning(int packagesWithQuestions, bool shuffleQuestions, bool hasMismatch)
    => packagesWithQuestions >= 2 && !shuffleQuestions && hasMismatch;
// D-04: tambah method ON-path, mis. ShouldShowKMinTruncationWarning(int packagesWithQuestions, bool shuffleQuestions, bool hasMismatch)
//        => packagesWithQuestions >= 2 && shuffleQuestions && hasMismatch;  (teks: "soal dipangkas ke K=min")

// Source: Helpers/PackageSizeAnalysis.cs (NEW, D-05) — ganti view :72-78 + controller :5844-5856
public static class PackageSizeAnalysis {
    public readonly record struct Result(int PackagesWithQuestions, int? ReferenceCount, bool HasMismatch);
    public static Result Compute(IEnumerable<AssessmentPackage> packages) {
        var withQ = packages.Where(p => p.Questions != null && p.Questions.Any()).ToList();
        if (withQ.Count == 0) return new Result(0, null, false);
        int refCount = withQ[0].Questions.Count;
        return new Result(withQ.Count, refCount, withQ.Any(p => p.Questions.Count != refCount));
    }
}
```

### Pattern 2: TempData PRG (Post-Redirect-Get) toast (D-01, D-07)
**What:** Endpoint POST set `TempData["Error"|"Success"|"Warning"]` lalu `RedirectToAction("ManagePackages")`. View render alert dismissible.
**When to use:** Semua mutasi paket/toggle/lock-reject.
**Example:**
```csharp
// Source: AssessmentAdminController.cs:5643-5646 (UpdateShuffleSettings guard — idiom yg di-mirror)
if (ShuffleToggleRules.IsShuffleLocked(anyStarted, anyAssignment)) {
    TempData["Error"] = "Pengaturan pengacakan tidak dapat diubah karena sudah ada peserta yang memulai ujian.";
    return RedirectToAction("ManagePackages", new { assessmentId });
}
// D-01/D-07: pola identik untuk reject toggle (anyStarted) + reject locked-edit (IsSessionEditLocked).
```

### Pattern 3: Sync-helper extraction (D-06) — bungkus blok copy-paste jadi satu helper
**What:** 5 call-site (`:5984-5991, :6067-6076, :6773-6786, :7036-7047, :7129-7140`) berisi blok identik: "jika session Pre-Test && punya linkedPost && linkedPost.SamePackage → SyncPackagesToPost(pre, post)". Ekstrak ke `private async Task SyncToLinkedPostIfSamePackageAsync(int preSessionId)`, panggil di 6 jalur (5 existing + Import).
**Example:**
```csharp
// Source: AssessmentAdminController.cs:6773-6786 (CreateQuestion auto-sync — pola yg di-ekstrak)
private async Task SyncToLinkedPostIfSamePackageAsync(int preSessionId) {
    var pre = await _context.AssessmentSessions.FindAsync(preSessionId);
    if (pre?.AssessmentType == "PreTest" && pre.LinkedSessionId.HasValue) {
        var post = await _context.AssessmentSessions.FindAsync(pre.LinkedSessionId.Value);
        if (post != null && post.SamePackage)
            await SyncPackagesToPost(pre.Id, post.Id);   // existing :5875-5933, deep-clone, sudah benar
    }
}
// Import wire (SHFX-01): SEBELUM `return RedirectToAction(...)` :6483 → await SyncToLinkedPostIfSamePackageAsync(pkg.AssessmentSessionId);
```

### Anti-Patterns to Avoid
- **View-only lock (SHUF-ISS-02 root cause):** menyembunyikan tombol TANPA guard endpoint → admin bisa POST langsung. SELALU guard server-side (D-07).
- **Mismatch dihitung di view (SHUF-ISS-07):** view `:72-78` re-derive `hasMismatch` ≠ controller `:5844-5856` → drift. Hapus view computation, render ViewBag saja (D-05).
- **`existingCount+1` untuk PackageNumber:** count turun setelah delete → nomor bentrok. Pakai `MAX+1` (D-02).
- **`OrderBy(PackageNumber)` tanpa `.ThenBy(Id)`:** non-deterministik saat nomor duplikat → worker geser paket lintas reshuffle. Tambah ThenBy(Id) di 5 site.
- **Type-agnostic sibling key (SHUF-ISS-01):** key Title/Category/Schedule.Date saja → Pre & Post tercampur (over-lock). Pakai `SiblingPrePostAwarePredicate` (SHFX-06).
- **`CREATE UNIQUE INDEX` sebelum dedup:** gagal bila ada duplikat existing → migration crash. Dedup DULU (D-02 ⚠️).
- **Bikin library/abstraksi baru:** fase ini hardening; reuse idiom existing.
- **JS modal tanpa `DOMContentLoaded` + `typeof bootstrap !== 'undefined'` guard:** `bootstrap is not defined` ReferenceError abort handler (lesson 390.1/421). `[CITED: 422-UI-SPEC.md:158]`

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Deep-clone Pre→Post packages | Loop manual clone baru | `SyncPackagesToPost(:5875-5933)` existing | Sudah handle delete-existing-Post + clone Q+Options + share ImagePath string + SaveChanges (SYN-01). |
| Type-aware sibling grouping | Predicate inline baru | `SiblingSessionQuery.SiblingPrePostAwarePredicate` | Kanonik StartExam/Reshuffle (Phase 373 invariant `:9-13`). |
| K=min truncation logic | Hitung min sendiri | `ShuffleEngine.cs:117` (read-only, jangan ubah) | Sumber kebenaran pemangkasan; warning hanya MEMBACA fakta, tak menduplikasi algoritma. |
| Lock/warning decision | `if` tersebar di view+controller | pure-helper `ShuffleToggleRules`/`PackageSizeAnalysis`/`IsSessionEditLocked` | Kill-drift pattern terbukti (374/420/421). |
| Filtered/unique index + dedup | App-code dedup loop | `migrationBuilder.Sql` + `CreateIndex` (idiom `AddUserUnitsTable.cs`) | Atomik, ter-version, jalan di Dev/Prod. |
| Audit log | Insert manual | `_auditLog.LogAsync` (try/catch warn-only) | Pola seragam (`:5663-5675`). |

**Key insight:** Hampir SEMUA logika yang dibutuhkan fase ini SUDAH ADA — masalahnya adalah duplikasi (mismatch, sync-block) dan absen-di-satu-jalur (Import sync, lock guard). Pekerjaan utama = **konsolidasi ke helper + wire ke jalur yang bolong**, bukan menulis logika baru.

## Runtime State Inventory

> Fase ini sebagian besar code/config, TAPI memuat 1 migration + 1 data-fix (dedup). Inventory runtime relevan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | **`AssessmentPackages.PackageNumber` duplikat existing** — akibat bug count-based (`existingCount+1`) + DeletePackage tak renumber. Bisa ada baris `(AssessmentSessionId, PackageNumber)` kembar di DB lokal/Dev/Prod. | **Data migration (dedup/renumber)** dalam migration Up() SEBELUM CreateIndex. Verifikasi lokal `sqlcmd -C -I` cek tak ada duplikat sebelum & sesudah. |
| Stored data | `SamePackage` kolom pada `AssessmentSessions` (PostTest). Toggle D-01 menulis kolom existing — tak ada schema change untuk SamePackage itu sendiri. | Code edit (toggle endpoint), bukan migration baru untuk kolom. |
| Live service config | Tidak ada — fitur internal app, tak ada n8n/external service. | None — verified by scope (controller + DB only). |
| OS-registered state | Tidak ada — tak ada Task Scheduler/pm2/systemd terkait paket-soal. | None — verified by domain (web app internal). |
| Secrets/env vars | Tidak ada secret/env baru. SQLEXPRESS connection string existing. | None. |
| Build artifacts | Snapshot EF `Migrations/ApplicationDbContextModelSnapshot.cs` ter-update otomatis saat `dotnet ef migrations add` (index baru tercatat). `.Designer.cs` baru ter-generate. | Commit migration + Designer + snapshot bersama (jangan parsial). |

**Canonical question — "Setelah semua file repo di-update, apa yang masih punya state lama?"** → DB existing `PackageNumber` duplikat. Migration WAJIB dedup. Tidak ada cache/registry runtime lain.

## Common Pitfalls

### Pitfall 1: CreateIndex gagal karena duplikat PackageNumber existing (D-02 ⚠️ HIGHEST RISK)
**What goes wrong:** `dotnet ef database update` / deploy Prod gagal dgn `Cannot create unique index ... duplicate key` karena data lama punya `(AssessmentSessionId, PackageNumber)` kembar.
**Why it happens:** Bug `existingCount+1` (`:5969-5976`) + DeletePackage tak renumber → nomor bentrok historis.
**How to avoid:** Di migration Up(), jalankan `migrationBuilder.Sql` renumber (ROW_NUMBER OVER PARTITION BY AssessmentSessionId ORDER BY PackageNumber, Id) SEBELUM `CreateIndex`. Down() drop index saja (renumber tak perlu di-revert).
**Warning signs:** Lokal: `SELECT AssessmentSessionId, PackageNumber, COUNT(*) FROM AssessmentPackages GROUP BY AssessmentSessionId, PackageNumber HAVING COUNT(*) > 1` mengembalikan baris.

### Pitfall 2: `PackageNumber` non-nullable → filter `IS NOT NULL` mubazir/menyesatkan
**What goes wrong:** Mengikuti CONTEXT harfiah "filter WHERE PackageNumber IS NOT NULL" pada kolom non-nullable → filter tak berguna (semua baris match) + bikin index "filtered" tanpa alasan.
**Why it happens:** CONTEXT D-02 menulis "(filter ... bila nullable)" — kondisional. Verifikasi: `PackageNumber` = `int` non-nullable (`AssessmentPackage.cs:19`, snapshot `:390-391`).
**How to avoid:** Buat **plain unique index** `(AssessmentSessionId, PackageNumber)` TANPA filter. Fluent: `entity.HasIndex(p => new { p.AssessmentSessionId, p.PackageNumber }).IsUnique().HasDatabaseName("IX_AssessmentPackages_SessionId_PackageNumber_Unique")`.
**Warning signs:** Migration menghasilkan `filter: "[PackageNumber] IS NOT NULL"` pada kolom int → hapus.

### Pitfall 3: Lock guard salah arah pada jalur Import (D-06 vs D-07 interaksi)
**What goes wrong:** Import beroperasi pada paket **Pre-Test** (Pre editable). D-06 sync dipicu setelah Import Pre. D-07 lock guard menolak edit **Post** terkunci. Jika lock guard naif (cek `IsSessionEditLocked(session of package)`) dipasang di Import, ia BENAR menolak Import langsung ke paket Post terkunci, TAPI harus TETAP mengizinkan Import ke Pre lalu sync ke Post.
**Why it happens:** `IsSessionEditLocked` true hanya untuk `PostTest && SamePackage`. Import ke Pre → session Pre → lock false → lolos → sync. Import langsung ke Post terkunci → lock true → reject. Ini PERILAKU BENAR — pastikan guard cek session-dari-package, bukan linkedPost.
**How to avoid:** Guard `IsSessionEditLocked(packageSession)` di awal endpoint. Sync helper terpisah jalan di akhir (success path) untuk Pre. Jangan campur.
**Warning signs:** Test: Import ke paket Post terkunci HARUS reject; Import ke Pre ber-SamePackage HARUS lolos + Post ter-sync.

### Pitfall 4: Sibling key inconsistency saat SHFX-06 (over-fix)
**What goes wrong:** Mengganti key di `UpdateShuffleSettings`/`UpdateRetakeSettings`/GET ManagePackages ke type-aware, TAPI lupa bahwa `UpdateShuffleSettings` SENGAJA propagate ke SEMUA sibling grup (Pre+Post) untuk shuffle (`:5649-5659`). Type-aware akan memisahkan Pre dari Post → shuffle Post tak ter-propagate ke Pre.
**Why it happens:** SHFX-06 fokus pada **lock detection** (over-lock fix), bukan tentu **propagation scope**. Audit SHUF-ISS-01 = "over-lock (Pre mulai → toggle Post terkunci)". Yang perlu type-aware = deteksi `anyStarted`/`anyAssignment` untuk LOCK, bukan tentu loop write.
**How to avoid:** Baca ulang maksud SHUF-ISS-01: lock check harus type-aware (Pre & Post tak saling kunci). Pertimbangkan apakah propagation shuffle tetap cross-type (Pre↔Post share shuffle by design `:5649`) atau juga type-aware — **klarifikasi di plan**; jangan asumsikan. Discretion CONTEXT: "Selama selaras StartExam/Reshuffle". StartExam pakai type-aware HANYA untuk workerIndex/packages, bukan propagation.
**Warning signs:** Setelah fix, test propagation shuffle Pre↔Post berubah perilaku → regresi. Lihat Open Question Q1.

### Pitfall 5: Toggle OFF mengosongkan paket Post (D-01 violation)
**What goes wrong:** Saat OFF SamePackage, naif memanggil sesuatu yang menghapus paket Post clone.
**Why it happens:** Asumsi "OFF = revert".
**How to avoid:** D-01 eksplisit: OFF = lepas lock SAJA (`SamePackage=false` + SaveChanges), **pertahankan** paket clone (jadi editable manual). JANGAN panggil sync/delete pada OFF.
**Warning signs:** Test: setelah OFF, paket Post masih ada + editable.

### Pitfall 6: Confirm-before JS quote-break / ReferenceError
**What goes wrong:** `confirm()` string dengan double-quote di attribute HTML, atau modal tanpa DOMContentLoaded guard.
**How to avoid:** Single-quote confirm string; mirror `confirmDeletePackage()` (`:405-412`); modal wrap `DOMContentLoaded` + `typeof bootstrap !== 'undefined'`. `[CITED: 422-UI-SPEC.md:158,168]`

## Code Examples

### D-02 Migration: dedup-then-CreateIndex (HIGHEST RISK)
```csharp
// Source idiom: Migrations/20260618045427_AddUserUnitsTable.cs:51-57 (migrationBuilder.Sql + CreateIndex)
//             + Data/ApplicationDbContext.cs:232-235 (fluent unique index)
protected override void Up(MigrationBuilder migrationBuilder)
{
    // STEP 1 — DEDUP/RENUMBER existing duplicates FIRST (else CreateIndex fails).
    // ROW_NUMBER per session by (PackageNumber, Id) → assign sequential 1..N (gap-free per session).
    // Statik, no user input. Idempotent-safe (re-run menghasilkan numbering sama).
    migrationBuilder.Sql(@"
        WITH Numbered AS (
            SELECT Id,
                   ROW_NUMBER() OVER (PARTITION BY AssessmentSessionId ORDER BY PackageNumber, Id) AS rn
            FROM AssessmentPackages
        )
        UPDATE p
        SET p.PackageNumber = n.rn
        FROM AssessmentPackages p
        INNER JOIN Numbered n ON p.Id = n.Id;
    ");

    // STEP 2 — plain UNIQUE index (PackageNumber NON-nullable → NO filter needed).
    migrationBuilder.CreateIndex(
        name: "IX_AssessmentPackages_SessionId_PackageNumber_Unique",
        table: "AssessmentPackages",
        columns: new[] { "AssessmentSessionId", "PackageNumber" },
        unique: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropIndex(
        name: "IX_AssessmentPackages_SessionId_PackageNumber_Unique",
        table: "AssessmentPackages");
    // Renumber tidak di-revert (data-fix permanen, aman).
}
```
**Catatan QUOTED_IDENTIFIER:** Filtered index di SQL Server memerlukan `SET QUOTED_IDENTIFIER ON` saat membuat. Di sini index PLAIN (tanpa filter), jadi syarat itu TIDAK berlaku. Bila planner tetap memilih filtered (tidak disarankan), bungkus `migrationBuilder.Sql("SET QUOTED_IDENTIFIER ON;")` sebelum CreateIndex. EF Core default sudah set ini untuk koneksi, jadi umumnya aman. `[CITED: docs.microsoft.com/sql filtered-index QUOTED_IDENTIFIER]`

### D-02 fluent config (untuk snapshot konsistensi)
```csharp
// Source: Data/ApplicationDbContext.cs:359-361 (idiom composite unique). Tambahkan blok AssessmentPackage entity.
modelBuilder.Entity<AssessmentPackage>(entity =>
{
    entity.HasIndex(p => new { p.AssessmentSessionId, p.PackageNumber })
          .IsUnique()
          .HasDatabaseName("IX_AssessmentPackages_SessionId_PackageNumber_Unique");
});
// Lalu `dotnet ef migrations add` akan auto-generate CreateIndex; EDIT untuk sisipkan dedup SQL di atasnya.
```

### D-02 CreatePackage MAX+1 (ganti count-based)
```csharp
// Source: AssessmentAdminController.cs:5969-5976 (ganti existingCount+1)
var maxNumber = await _context.AssessmentPackages
    .Where(p => p.AssessmentSessionId == assessmentId)
    .Select(p => (int?)p.PackageNumber)
    .MaxAsync();                       // null bila belum ada paket
var pkg = new AssessmentPackage {
    AssessmentSessionId = assessmentId,
    PackageName = packageName.Trim(),
    PackageNumber = (maxNumber ?? 0) + 1
};
```

### D-07 IsSessionEditLocked + guard
```csharp
// Source: AssessmentAdminController.cs:5811 (ViewBag.IsSamePackageLocked = isPostSession && SamePackage)
private static bool IsSessionEditLocked(AssessmentSession s)
    => s.AssessmentType == "PostTest" && s.SamePackage;

// Di AWAL 5 endpoint POST (CreatePackage/DeletePackage = session via assessmentId/packageId→session;
//   CreateQuestion/EditQuestion/DeleteQuestion = packageId → pkg.AssessmentSessionId → session):
var session = await _context.AssessmentSessions.FindAsync(assessmentId /* or resolved */);
if (session != null && IsSessionEditLocked(session)) {
    TempData["Error"] = "Paket Post-Test ini terkunci (paket-sama). Edit soal harus dilakukan di sesi Pre-Test.";
    return RedirectToAction("ManagePackages", new { assessmentId = session.Id });
}
```

### SHFX-06 sibling key type-aware (reuse predicate)
```csharp
// Source: Helpers/SiblingSessionQuery.cs:14-24 (kanonik). Ganti :5630-5635 + :5704-5706 + :5814-5819.
var siblingSessionIds = await _context.AssessmentSessions
    .Where(SiblingSessionQuery.SiblingPrePostAwarePredicate(
        assessment.Title, assessment.Category, assessment.Schedule.Date, assessment.AssessmentType))
    .Select(s => s.Id)
    .ToListAsync();
// + koreksi komentar :5629 ("key identik StartExam/Reshuffle" — kini BENAR setelah pakai predicate ini).
// ⚠️ Lihat Pitfall 4 + Open Question Q1: tentukan apakah type-aware hanya untuk LOCK-detection atau juga propagation.
```

### D-04 toggle SamePackage endpoint (NEW — mirror UpdateShuffleSettings)
```csharp
// Source: AssessmentAdminController.cs:5623-5679 (UpdateShuffleSettings = template lengkap PRG+guard+audit)
[HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleSamePackage(int assessmentId, bool samePackage)
{
    var post = await _context.AssessmentSessions.FindAsync(assessmentId);
    if (post == null) return NotFound();
    if (post.AssessmentType != "PostTest" || !post.LinkedSessionId.HasValue) {
        TempData["Error"] = "Pengaturan paket-sama hanya berlaku untuk Post-Test berpasangan.";
        return RedirectToAction("ManagePackages", new { assessmentId });
    }
    // GUARD D-01: tolak bila ADA peserta sudah-mulai di GRUP (StartedAt!=null || status InProgress/Completed).
    // Idiom anyStarted :5638-5641; status set :5546-5553. Type-aware grup via SiblingPrePostAwarePredicate? → grup = Pre+Post pasangan.
    bool anyStarted = /* AnyAsync sibling grup: StartedAt != null || Status in (InProgress, Completed) */;
    if (anyStarted) {
        TempData["Error"] = "Gagal mengubah pengaturan paket-sama: sudah ada peserta yang memulai ujian di grup ini.";
        return RedirectToAction("ManagePackages", new { assessmentId });
    }
    post.SamePackage = samePackage;
    post.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    if (samePackage) {                    // ON → sync + (lock implisit via SamePackage=true)
        await SyncToLinkedPostIfSamePackageAsync(post.LinkedSessionId.Value);  // pre→post
        TempData["Success"] = "Pengaturan paket-sama diaktifkan. Paket Post-Test telah disinkronkan dari Pre-Test dan dikunci.";
    } else {                              // OFF → KEEP paket clone (jangan kosongkan, D-01)
        TempData["Success"] = "Pengaturan paket-sama dinonaktifkan. Kunci dilepas; paket salinan dipertahankan untuk diedit.";
    }
    // + audit LogAsync try/catch (pola :5663-5675)
    return RedirectToAction("ManagePackages", new { assessmentId });
}
```
**Catatan:** `SyncToLinkedPostIfSamePackageAsync(preSessionId)` butuh PRE id. Untuk ON-toggle, kita punya `post.LinkedSessionId` = Pre id. Helper cek `pre.AssessmentType=="PreTest" && pre.linkedPost.SamePackage` — karena kita BARU set `post.SamePackage=true` + SaveChanges, helper akan menemukannya true. Pastikan SaveChanges SamePackage SEBELUM panggil helper (urutan di atas benar).

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Lock SamePackage hanya di view | Server-side guard di endpoint POST | Fase 422 (D-07) | Tutup SHUF-ISS-02 (admin tak bisa bypass via POST). |
| Sync block copy-paste 5× + absen di Import | Satu helper `SyncToLinkedPostIfSamePackageAsync`, 6 jalur | Fase 422 (D-06) | Tutup SHUF-ISS-03 HIGH + kill-drift. |
| Mismatch dihitung controller + view | `PackageSizeAnalysis.Compute` satu sumber | Fase 422 (D-05) | Tutup SHUF-ISS-07. |
| PackageNumber count-based | MAX+1 + ThenBy(Id) + unique index | Fase 422 (D-02) | Tutup SHUF-ISS-08; migration=TRUE. |
| Sibling key type-agnostic (shuffle lock/save) | `SiblingPrePostAwarePredicate` type-aware | Fase 422 (SHFX-06) | Tutup SHUF-ISS-01 over-lock. |
| SamePackage final-at-create | Toggle editable + guard | Fase 422 (D-01) | Tutup FLOW-07. |

**Deprecated/outdated:** Tidak ada API .NET/EF yang deprecated relevan. EF Core 8 `MaxAsync` + `migrationBuilder.Sql` + `CreateIndex(unique:true)` semua current untuk net8.0.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Guard "peserta sudah mulai" D-01 berbasis status/StartedAt-presence (BUKAN perbandingan waktu), jadi konvensi +7h WIB kemungkinan TIDAK diperlukan di toggle guard. | D-01 toggle / Stack alternatives | Bila bisnis ingin guard berbasis waktu (mis. "boleh toggle sampai jam mulai"), perlu +7h verbatim. Rendah — CONTEXT D-01 menyebut "StartedAt set / InProgress / Completed" (status-based). |
| A2 | "Grup" untuk guard toggle D-01 = pasangan Pre+Post yang ter-link (via LinkedGroupId/LinkedSessionId), mencakup semua peserta grup. | D-04 endpoint guard | Bila "grup" dimaksud hanya Post sessions, scope guard berbeda. Sedang — perlu konfirmasi scope di plan. |
| A3 | Propagation shuffle Pre↔Post (`:5649`) SENGAJA cross-type (share shuffle); SHFX-06 type-aware hanya untuk LOCK-detection, bukan propagation loop. | Pitfall 4 / Open Q1 | Tinggi bila salah — bisa regresi UpdateShuffleSettings (shuffle Post tak ter-propagate ke Pre). Harus diklarifikasi sebelum implement SHFX-06. |
| A4 | Filtered index `WHERE PackageNumber IS NOT NULL` TIDAK perlu karena kolom non-nullable; plain unique index cukup. | Pitfall 2 / migration | Rendah — diverifikasi model+snapshot. Bila planner tetap mau filtered (defensif), tak salah tapi mubazir. |
| A5 | Tidak ada baris PackageNumber duplikat di DB lokal saat ini belum diverifikasi via query (butuh DB live); ASUMSI duplikat MUNGKIN ada → dedup wajib defensif. | Runtime State / Pitfall 1 | Rendah — dedup idempotent aman walau 0 duplikat. Tetap WAJIB jalankan (Dev/Prod bisa beda dari lokal). |
| A6 | `UpdatedAt` ada di AssessmentSession (dipakai `:5658,:5715`). | Code examples | Rendah — terlihat dipakai di endpoint existing. |

**Konfirmasi sebelum lock-in:** A2 + A3 sebaiknya diklarifikasi planner/discuss sebelum menulis task SHFX-02 & SHFX-06.

## Open Questions

1. **SHFX-06: type-aware untuk lock-detection saja, atau juga untuk propagation write?**
   - What we know: `UpdateShuffleSettings` propagate shuffle ke SEMUA sibling grup (Pre+Post) by design (`:5649-5659`, komentar "Propagate ke SEMUA sibling grup"). StartExam pakai type-aware HANYA untuk `workerIndex`/`packages` (bukan propagation). Audit SHUF-ISS-01 = over-LOCK (deteksi), bukan over-propagate.
   - What's unclear: Apakah Pre & Post HARUS tetap berbagi setting shuffle yang sama (cross-type propagation), ATAU shuffle Pre & Post independen (type-aware propagation)?
   - Recommendation: Pertahankan propagation cross-type (Pre↔Post share shuffle, status quo), ubah HANYA lock-detection (`anyStarted`/`anyAssignment`) jadi type-aware. Konfirmasi di plan. Bila ragu, default = ubah seminimal mungkin (lock-detection only) untuk jaga backward-compat (CONTEXT WAJIB).

2. **SHFX-02 toggle: apakah perlu re-arm/clear UserPackageAssignment saat ON-sync mengganti paket Post?**
   - What we know: `SyncPackagesToPost` deep-clone paket, tapi guard D-01 sudah menolak toggle bila ada peserta sudah-mulai (yang biasanya berarti ada assignment). Peserta belum-mulai = boleh; assignment mungkin belum ada.
   - What's unclear: Jika peserta belum-mulai TAPI sudah ada UserPackageAssignment (di-reshuffle HC manual sebelum mulai), apakah toggle ON harus invalidate assignment lama (karena package Id berubah)?
   - Recommendation: Verifikasi `SyncPackagesToPost` sudah cascade-clean assignment lama Post (DeletePackage `:6029-6033` menghapus assignment by package). `SyncPackagesToPost` menghapus paket Post existing → assignment by-package-Id jadi orphan? Cek: `SyncPackagesToPost:5878-5889` hapus pkg+Q+Options TAPI tidak eksplisit hapus UserPackageAssignment. Plan harus uji ini (assignment Post jadi dangling setelah re-sync). Mitigasi: guard D-01 (no-started) mengurangi risiko, tapi belum-mulai-tapi-ada-assignment = edge. Catat untuk test.

3. **Dedup migration: apakah perlu jalan di Dev sebelum Prod?**
   - What we know: Migration jalan otomatis saat IT deploy + `database update`. Dedup idempotent.
   - What's unclear: Apakah DB Dev/Prod punya duplikat (tak bisa dicek dari sini).
   - Recommendation: IT jalankan query cek duplikat di Dev SEBELUM apply (notify IT). Migration tetap defensif (dedup-then-index). Flag migration=TRUE ke IT dgn commit hash.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | Build + EF tooling | ✓ | 8.0.418 | — |
| EF Core SqlServer | Migration | ✓ | 8.0.0 | — |
| SQL Server (SQLEXPRESS) | DB lokal + integration test | ✓ (per CLAUDE.md/MEMORY) | — | InMemory untuk pure-helper test (tak butuh DB) |
| dotnet ef CLI | `migrations add`/`database update` | Asumsi ✓ (proyek punya 100+ migration) | global/local tool | `dotnet tool install dotnet-ef` bila absen |
| xUnit + Test.Sdk | Test fase 422 | ✓ | 2.9.3 / 17.13.0 | — |
| App lokal port 5270 | UAT Playwright/live (branch ITHandoff) | ✓ (konvensi) | — | — |
| sqlcmd -C -I | Verifikasi DB duplikat | Asumsi ✓ (dipakai di fase lain) | — | EF query in test |

**Missing dependencies with no fallback:** Tidak ada teridentifikasi.
**Missing dependencies with fallback:** Pure-helper test (`PackageSizeAnalysisTests`, `ShuffleToggleRulesTests`) tak butuh SQLEXPRESS → bisa jalan di CI SQL-less. Integration test pakai `[Trait("Category","Integration")]` → skip via `--filter "Category!=Integration"` (pola existing).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (net8.0) |
| Config file | none (konvensi `[Trait("Category","Integration")]` untuk SQL-gated; pure test default) |
| Quick run command | `dotnet test --filter "Category!=Integration"` (pure helper, SQL-less, < 30s) |
| Full suite command | `dotnet test` (incl integration real-SQLEXPRESS) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SHFX-01 | Import ke Pre ber-SamePackage → Post ter-sync; Import ke Pre tanpa SamePackage → tidak | integration | `dotnet test --filter "FullyQualifiedName~SamePackageSyncTests"` | ❌ Wave 0 (`SamePackageSyncTests.cs`) |
| SHFX-01 | `SyncToLinkedPostIfSamePackageAsync` no-op bila session bukan PreTest / linkedPost null / !SamePackage | integration | (same file) | ❌ Wave 0 |
| SHFX-02 | Toggle ON → SamePackage=true + Post ter-sync; OFF → false + paket clone DIPERTAHANKAN | integration | `dotnet test --filter "FullyQualifiedName~SamePackageToggleGuardTests"` | ❌ Wave 0 (`SamePackageToggleGuardTests.cs`) |
| SHFX-02 | Guard: anyStarted (StartedAt/InProgress/Completed) di grup → toggle REJECT, SamePackage tak berubah | integration | (same file) | ❌ Wave 0 |
| SHFX-02 | Guard: grup belum-mulai → toggle ALLOW | integration | (same file) | ❌ Wave 0 |
| SHFX-03 | `IsSessionEditLocked` true bila PostTest && SamePackage, false selainnya (Pre, Standard, Post non-same) | unit | `dotnet test --filter "FullyQualifiedName~SessionEditLockTests"` | ❌ Wave 0 (`SessionEditLockTests.cs`) |
| SHFX-03 | 5 endpoint POST reject saat locked (no-write) + lolos saat tak-locked | integration | (same file, replicate-endpoint-body pola `ShuffleLockGuardTests`) | ❌ Wave 0 |
| SHFX-04 | newPost tambah-peserta warisi SamePackage = repPost.SamePackage | integration | `dotnet test --filter "FullyQualifiedName~SamePackageInheritTests"` | ❌ Wave 0 (`SamePackageInheritTests.cs`) |
| SHFX-05 | CreatePackage pakai MAX+1 (setelah delete paket tengah, nomor baru tak bentrok) | integration | `dotnet test --filter "FullyQualifiedName~PackageNumberUniqueTests"` | ❌ Wave 0 (`PackageNumberUniqueTests.cs`) |
| SHFX-05 | Unique index menolak duplikat (AssessmentSessionId, PackageNumber) → DbUpdateException | integration | (same file) | ❌ Wave 0 |
| SHFX-05 | Migration dedup: seed duplikat → run renumber SQL → 0 duplikat, gap-free per session | integration | `dotnet test --filter "FullyQualifiedName~PackageNumberMigrationTests"` | ❌ Wave 0 (`PackageNumberMigrationTests.cs`) |
| SHFX-06 | Lock-detection memakai SiblingPrePostAwarePredicate type-aware (Pre mulai → Post TIDAK terkunci) | integration | `dotnet test --filter "FullyQualifiedName~SiblingTypeAwareLockTests"` | ❌ Wave 0 (`SiblingTypeAwareLockTests.cs`) — atau extend `SiblingPrePostFilterTests` (existing) |
| SHFX-07 | `ShouldShowKMinTruncationWarning` (ON-path) true bila ≥2 paket-ber-soal, ON, mismatch | unit | `dotnet test --filter "FullyQualifiedName~ShuffleToggleRulesTests"` | ✅ extend (`ShuffleToggleRulesTests.cs` existing) |
| SHFX-07 | `PackageSizeAnalysis.Compute` paritas dgn view-lama (hasMismatch/referenceCount/packagesWithQuestions) | unit | `dotnet test --filter "FullyQualifiedName~PackageSizeAnalysisTests"` | ❌ Wave 0 (`PackageSizeAnalysisTests.cs`) |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (pure helper) + `dotnet build`
- **Per wave merge:** `dotnet test` (full, real-SQLEXPRESS)
- **Phase gate:** Full suite green + UAT live @5270 (toggle ON/OFF, lock reject, Import sync, mismatch warning) sebelum `/gsd-verify-work`. migration: `dotnet ef database update` lokal sukses + `sqlcmd` cek 0 duplikat.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/PackageSizeAnalysisTests.cs` — pure, covers SHFX-07 (mismatch single-source)
- [ ] `HcPortal.Tests/SamePackageSyncTests.cs` — integration, covers SHFX-01 (6-jalur, esp Import)
- [ ] `HcPortal.Tests/SamePackageToggleGuardTests.cs` — integration, covers SHFX-02 (toggle ON/OFF + guard)
- [ ] `HcPortal.Tests/SessionEditLockTests.cs` — unit + integration, covers SHFX-03 (lock guard 5 endpoint)
- [ ] `HcPortal.Tests/SamePackageInheritTests.cs` — integration, covers SHFX-04 (newPost inherit)
- [ ] `HcPortal.Tests/PackageNumberUniqueTests.cs` — integration, covers SHFX-05 (MAX+1 + index)
- [ ] `HcPortal.Tests/PackageNumberMigrationTests.cs` — integration, covers SHFX-05 dedup renumber
- [ ] `HcPortal.Tests/SiblingTypeAwareLockTests.cs` — integration, covers SHFX-06 (type-aware lock) [atau extend `SiblingPrePostFilterTests`]
- [ ] EXTEND `HcPortal.Tests/ShuffleToggleRulesTests.cs` — covers SHFX-07 K=min ON-path (D-04)
- Framework install: none (xUnit 2.9.3 sudah ada). Fixture reuse: `RetakeServiceFixture` / `ProtonCompletionFixture` (real-SQLEXPRESS pattern, lihat `ShuffleLockGuardTests.cs:22-30`).

## Security Domain

> `security_enforcement` tak di-set explicit false di config.json → enabled. Section disertakan.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Server-authoritative lock (D-07) — keputusan integritas di backend, bukan view. |
| V2 Authentication | yes (existing) | `[Authorize(Roles="Admin, HC")]` di semua endpoint (pola `:5621/5936/5957`). Endpoint toggle baru WAJIB sama. |
| V3 Session Management | no (delegated) | ASP.NET Core Identity existing, tak disentuh. |
| V4 Access Control | yes | RBAC `[Authorize(Roles="Admin, HC")]` + antiforgery `[ValidateAntiForgeryToken]` di ToggleSamePackage baru (pola `:5622/5937`). |
| V5 Input Validation | yes | `assessmentId`/`packageId` int (route/form); validasi session exists (`FindAsync` + NotFound). PackageNumber MAX+1 server-computed (tak terima dari client). |
| V6 Cryptography | no | Tak ada crypto baru. |
| V12 Files/Resources | partial (existing) | Import file guard (.xlsx/.xls + 5MB) sudah ada `:6203-6219`; tak diubah. |

### Known Threat Patterns for ASP.NET Core MVC + EF Core
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF pada toggle/lock endpoint | Tampering | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` di form (pola existing semua POST). |
| Bypass view-only lock via direct POST (SHUF-ISS-02 root) | Elevation/Tampering | D-07 server-side guard `IsSessionEditLocked` di AWAL endpoint (tolak keras). |
| IDOR (edit paket sesi lain) | Tampering | Resolve session via FindAsync + `[Authorize]` role; tak ada owner-check baru perlu (Admin/HC global). Verifikasi assessmentId milik konteks. |
| SQL injection di migration dedup | Tampering | Raw SQL `migrationBuilder.Sql` = literal statik, NO user input (idiom `AddUserUnitsTable.cs:50` "Literal statik, no user input"). |
| XSS pada toast/warning Bahasa Indonesia | Tampering | Razor auto-encode; copy statik (bukan user input). Confirm string single-quote (no Json.Serialize di attribute). `[CITED: 422-UI-SPEC.md:168]` |
| Mass-assignment SamePackage via form | Tampering | Toggle endpoint terima `bool samePackage` eksplisit, server set kolom; bukan model-bind seluruh entity. |

## Sources

### Primary (HIGH confidence) — diverifikasi langsung dari source ITHandoff
- `Controllers/AssessmentAdminController.cs` — `:870/1309/1356` (SamePackage Create param), `:1999-2045` (newPost SHFX-04), `:5519-5520/5567` (SiblingKey type-aware StartExam), `:5623-5679` (UpdateShuffleSettings template), `:5687-5741` (UpdateRetakeSettings + RetakeRules wire), `:5630-5635/5704-5706/5814-5819` (type-agnostic key SHFX-06), `:5754-5868` (ManagePackages GET + mismatch `:5844-5856`), `:5875-5933` (SyncPackagesToPost), `:5938-5994` (CopyPackagesFromPre + CreatePackage), `:5999-6082` (DeletePackage), `:6199-6225/6470-6486` (ImportPackageQuestions — BOCOR), `:6625-6638` (CreateQuestion sig), `:6773-6786` (sync block template), `:7036-7047/7084-7140` (EditQuestion/DeleteQuestion sync).
- `Helpers/SiblingSessionQuery.cs:14-24` — SiblingPrePostAwarePredicate kanonik.
- `Helpers/ShuffleToggleRules.cs:11-20` — pure-rules (extend D-04).
- `Helpers/ShuffleEngine.cs:96-119` — K=min source.
- `Helpers/RetakeRules.cs:49-82` — pola +7h WIB kill-drift + CooldownMayExceedWindow.
- `Models/AssessmentPackage.cs:19` + `Migrations/ApplicationDbContextModelSnapshot.cs:390-397` — PackageNumber int non-nullable, hanya index AssessmentSessionId.
- `Models/AssessmentSession.cs:200-205` — SamePackage kontrak.
- `Data/ApplicationDbContext.cs:232-235/353-361` — fluent filtered/composite unique index idiom.
- `Migrations/20260618045427_AddUserUnitsTable.cs:35-57` — migrationBuilder.Sql backfill + CreateIndex (canonical migration pattern).
- `Migrations/20260611001939_AddPendingProtonBypassActiveUniqueIndex.cs` — filtered unique index migration.
- `HcPortal.Tests/ShuffleLockGuardTests.cs` + `ParticipantRemoveGuardTests.cs` + `HcPortal.Tests.csproj` — fixture pattern (real-SQLEXPRESS, IClassFixture, [Trait Integration]), xUnit 2.9.3.
- `Views/Admin/ManagePackages.cshtml:29-132/300-413` — lock banner/shuffle card/mismatch dup/Create+Delete buttons/confirmDeletePackage.
- `Views/Admin/ManagePackageQuestions.cshtml` — 0 lock awareness (friendly layer Wave baru).
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` §5.2.4/§5.3/§6-P3 — file:line evidence + fix per finding.
- `422-CONTEXT.md` (D-01..D-07) + `422-UI-SPEC.md` (UI contract).
- `.planning/config.json` — nyquist_validation:true; security enabled.

### Secondary (MEDIUM confidence)
- `dotnet --version` → 8.0.418; `git branch --show-current` → ITHandoff (verified via Bash).

### Tertiary (LOW confidence — flag validasi)
- SQL Server filtered-index `QUOTED_IDENTIFIER ON` requirement — training knowledge; relevan HANYA bila planner pilih filtered index (tidak disarankan, lihat Pitfall 2). `[ASSUMED]` untuk detail; index plain tak terpengaruh.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua dependency diverifikasi dari .csproj + dotnet --version; tak ada paket baru.
- Architecture / call-sites: HIGH — setiap file:line dibaca langsung; 6 sync call-site + 5 endpoint POST + sibling key + mismatch dup terkonfirmasi.
- Migration (D-02): HIGH untuk pola (idiom AddUserUnitsTable terverifikasi) + MEDIUM untuk asumsi data (duplikat existing belum diquery dari DB live — A5).
- Pitfalls: HIGH — diturunkan dari evidence audit + verifikasi source.
- SHFX-06 propagation scope: MEDIUM — perilaku type-aware vs cross-type propagation perlu klarifikasi (Open Q1/A3).

**Research date:** 2026-06-23
**Valid until:** ~30 hari (stack stabil net8.0; risiko utama = drift branch main v32.6 shuffle saat merge — catat tiap perubahan shuffle untuk rekonsiliasi, per CONTEXT specifics).
