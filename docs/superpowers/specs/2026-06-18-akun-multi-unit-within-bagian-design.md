# Design Spec — Akun Multi-Unit (dalam 1 Bagian) + Coaching Cross-Unit + PROTON Sekuensial

**Tanggal:** 2026-06-18
**Milestone target:** v32.3 (fase 399–404)
**Status:** Approved (brainstorm selesai)
**Migration:** TRUE (1 migration: tabel `UserUnits`)

## 1. Ringkasan

Saat ini satu pekerja (`ApplicationUser`) hard-wired ke **tepat 1 Bagian (Section) + 1 Unit** — keduanya `string?` polos tanpa FK. Kebutuhan riil: sebagian pekerja kerja di **>1 unit** (mis. PROTON Operator Tahun 1 di unit X, lalu Tahun 2 di unit Y), dan HC perlu bisa mapping **1 coach pegang coachee lintas-unit selama masih 1 Bagian**.

Milestone ini menambah **multi-unit di level akun**, dengan 3 batasan kunci yang menjaga scope tetap *medium* (bukan XL):

1. **Multi-unit SELALU dalam 1 Bagian.** Section/Bagian tetap **scalar** (1 per akun). Hanya **Unit** yang jadi himpunan (set). Pindah Bagian = proses **mutasi** terpisah (di luar scope ini).
2. **PROTON tetap SEKUENtial.** Tepat **1 `ProtonTrackAssignment` aktif** pada satu waktu (invariant E8 dipertahankan). Tahun 1 unit X **selesai dulu** → baru Tahun 2 unit Y. Tidak ada 2 track PROTON aktif paralel.
3. **Cert/analytics atribusi = primary unit** (keputusan D1=b) — diterima sebagai batasan terdokumentasi, **tanpa** kolom unit-at-issue baru di `AssessmentSession`/`TrainingRecord`.

Tiga lapisan fitur:
- **A — Coaching cross-unit mapping** (kebutuhan UI utama HC).
- **B — Akun/profil multi-unit** (tabel junction `UserUnits`).
- **C-lite — PROTON sekuensial lintas-unit** (sebagian besar sudah didukung; hardening resolusi unit).

## 2. Keputusan Brainstorm (locked)

| # | Keputusan | Pilihan |
|---|-----------|---------|
| Org scope | Span Bagian? | **Selalu dalam 1 Bagian**. Section tetap scalar; hanya Unit multi. Cross-Bagian = mutasi (out-of-scope) |
| PROTON mode | Paralel vs sekuensial | **Sekuensial** — 1 assignment aktif/waktu. Invariant E8 + index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` **dipertahankan** |
| D1 — atribusi cert/renewal/analytics | per-unit vs primary | **(b) primary unit** — didokumentasikan sbg batasan. **TANPA** kolom unit-at-issue baru. Tidak ada migration ke-2 |
| D2 — year-gate progresi | per-unit vs per-TrackType | **per-TrackType (keep current)** — Tahun N lulus (di unit manapun) → Tahun N+1 boleh. Cocok skenario T1@X → T2@Y |
| D3 — coach per coachee | 1 total vs 1 per-unit | **1 coach aktif/coachee (keep)** — index filtered-unique dipertahankan |
| D4 — reporting fan-out | tiap-unit vs primary | Cert/analytics ikut primary (D1=b, no double-count). **Listing membership** set-aware (pekerja muncul di tiap unit-nya), dedup di rollup Bagian |
| Junction storage | NAME string vs FK | **Unit NAME string** + validasi ∈ unit Bagian pekerja. Konsisten konvensi `AssignmentUnit`/`ProtonKompetensi.Unit`. Cascade minimal |
| Primary mirror | drop vs keep | **Keep `ApplicationUser.Unit` sbg mirror PRIMARY** (write-through) — ~30+ baca scalar jalan terus, migrasi bertahap |

## 3. Arsitektur Relevan (hasil investigasi codebase — 22 agent)

- `ApplicationUser.Section` (`:28`) + `ApplicationUser.Unit` (`:33`) = `string?` polos, **no FK, no default**. Org tree sebenarnya di `OrganizationUnit` (self-FK `ParentId`, Level0=Bagian root, Level1=Unit child), tapi user nyambung **cuma via Name-string**, bukan Id.
- `ApplicationDbContext.GetSectionUnitsDictAsync()` (`:118-133`) sudah balikin `Dictionary<Bagian, List<Unit>>`; `GetUnitsForSectionAsync(section)` enumerate unit anak 1 Bagian. → primitif siap dipakai untuk multi-select.
- **Authz Section AMAN (tidak berubah):** `IsResultsAuthorized` (`CMPController.cs:2503-2510`) + SectionHead L4 gate **100% berbasis Section scalar**, tidak baca Unit. Karena Section tetap scalar → lapisan ini **0 perubahan**. (De-risk terbesar.)
- **PROTON unit di-resolve, tidak disimpan:** pola `AssignmentUnit (active CoachCoacheeMapping) ?? User.Unit` di 5 site resolver (`CoachMappingController.cs:1409-1418`, `:1461-1473`; `AssessmentAdminController.cs:1411-1414`; `CDPController.cs:515-526`, `:1708-1719`). `ProtonTrackAssignment` **tidak punya kolom Unit** (7 kolom, 0 unit).
- **Single-active dijaga:** index filtered-unique `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (CoacheeId WHERE IsActive=1, `ApplicationDbContext.cs:330-333`) + invariant E8 di Bypass (`ProtonBypassService.cs:104-118`, `:226-231`). Re-assign year-advance + `MoveAssignmentAsync` **sudah** pindah Tahun1@X→Tahun2@Y dan **simpan histori + cert unit-X** (AF-3 cross-unit tolerance sudah ada).
- **Cert = `AssessmentSession.NomorSertifikat`** (`KPB/{seq:D3}/{Roman}/{year}`, sequence global tahunan, filtered-unique index). Per-exam-session, **no dimensi unit**, never overwrite. → 2 unit sekuensial = 2 cert sbg histori, otomatis. **Tapi atribusi unit di list/renewal diturunkan dari scalar `User.Unit` saat query** (`AdminBaseController.cs:58/155`, `CDPController.cs:3932/4036`) → D1=b: terima atribusi = primary.
- KKJ/competency rollup per-unit **tidak ada** (`UserCompetencyLevel`/`AssessmentCompetencyMap` di-drop Phase 227). Notifikasi by explicit `userId`, bukan unit. → keduanya non-issue.

## 4. Data Model

### 4.1 Tabel baru `UserUnits`

```csharp
public class UserUnit
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";        // FK → AspNetUsers.Id, ON DELETE CASCADE
    public string Unit { get; set; } = "";           // NAME string, anak dari Section pekerja
    public bool IsPrimary { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public ApplicationUser? User { get; set; }
}
```

**Index:**
- `HasIndex(UserId)`
- Filtered-unique `(UserId) WHERE IsPrimary = 1` → tepat 1 primary per user.
- (Opsional) unique `(UserId, Unit)` → cegah duplikat unit per user.

**Section tetap di `ApplicationUser.Section`** (1 Bagian/akun). `UserUnits.Unit` **WAJIB** anak dari `Section` pekerja — divalidasi via `GetUnitsForSectionAsync(user.Section)` di setiap write (Name tidak global-unique, hanya unik per parent Bagian — `OrganizationUnit` unique `(ParentId, Name)`).

### 4.2 Primary mirror (kontrak write-through)

`ApplicationUser.Unit` **DIPERTAHANKAN** = nilai unit `IsPrimary` (denormalisasi). Setiap write multi-unit (Create/Edit/Import worker) **WAJIB** sinkron:
1. Tulis baris-baris `UserUnits` (set unit terpilih, 1 ditandai primary).
2. Tulis `ApplicationUser.Unit = <unit primary>` (mirror).
3. Recompute primary bila primary lama dihapus dari set (promote unit lain / blok hapus primary).

→ ~30+ baca scalar `user.Unit` jalan terus tanpa diubah di fase awal; migrasi pembaca ke `UserUnits` dilakukan selektif (lihat §5 per-fase).

### 4.3 Backfill (data migration)

Untuk setiap `ApplicationUser` dengan `Unit` non-null: INSERT 1 baris `UserUnits { UserId, Unit = User.Unit, IsPrimary = true, IsActive = true }`. Pekerja `Unit` null → tidak ada baris (`"Belum diisi"` tetap).

## 5. Breakdown Fase (399–404)

> Catatan: D1=b membuang fase "kolom unit-at-issue" (tidak ada migration ke-2). Cert/renewal/analytics tetap berbasis primary (terdokumentasi).

### Fase 399 — Foundation (junction + UI + display)
- Model `UserUnit` + `DbSet` + index + migration `AddUserUnitsTable` + backfill.
- Write-through primary-mirror contract di `WorkerController` Create (`:269`)/Edit (`:405-413`)/Import (`:1026`).
- ViewModel: `ManageUserViewModel`/`ProfileViewModel`/`SettingsViewModel` — `Unit` scalar → `List<string> Units` + `PrimaryUnit`.
- UI: picker **Bagian tetap single**; **Unit jadi multi-select** unit Bagian itu (Create/Edit/Import). `initSectionUnitCascade` → multi-select varian.
- Display semua unit: Profile (`:80,86`), WorkerDetail, Settings, ManageWorkers, export Excel (`WorkerController.cs:186`).
- Audit-log: diff set unit (bukan scalar `if user.Unit != model.Unit`).
- **File:** Models, `ApplicationDbContext`, `SeedData`, Migrations, `WorkerController`, `AccountController`, views Worker+Account, ViewModels.

### Fase 400 — Membership listing set-aware
- `WorkerDataService.GetWorkersInSection` (`:255`) filter `u.Unit==unitFilter` → set-aware (pekerja muncul di tiap unit-nya); `WorkerTrainingStatus.Unit` (`:347`) jadi set/primary sesuai konteks.
- Dedup di rollup Bagian (cegah double-count denominator completion%).
- `WorkerController` role-filter (`:78,160`) set-aware.
- **CMP analytics/renewal TIDAK diubah** (D1=b: tetap primary, terdokumentasi).
- **File:** `WorkerDataService`, `WorkerController`, view CMP records team.

### Fase 401 — PROTON unit-resolution hardening
- Switch axis coachee-scope `User.Unit` → `AssignmentUnit` di surface PROTON: `CDPController.cs:491,1586,1596,4248`, `ProtonDataController.cs:1517` (BypassList — action surface).
- Drop fallback `User.Unit` di 5 resolver → `AssignmentUnit` always-explicit.
- Validasi `AssignmentUnit ∈ coachee.UserUnits` (bukan cuma vs Bagian) di Assign/Edit/Import.
- Validasi bypass `TargetUnit ∈ worker.UserUnits` + org (`ProtonDataController.cs:1638` saat ini cuma non-empty).
- Fix `CleanupCoachCoacheeMappingOrg` (`CoachMappingController.cs:880-907`) — **jangan clobber** `AssignmentUnit` ke primary; jadikan UserUnits-aware/gated. (Data-loss vector.)
- Import-mapping (`:356,372`): set `AssignmentUnit` dari unit member yg dipilih, bukan paksa primary.
- **File:** `CoachMappingController`, `CDPController`, `ProtonDataController`, `ProtonBypassService`, `AssessmentAdminController`.

### Fase 402 — Fitur A: coach × coachee cross-unit mapping
- Relax JS lock single-unit-per-batch → **Bagian-level** (`Views/Admin/CoachCoacheeMapping.cshtml:717-726,765-772`).
- Guard server: unit coachee ⊆ Bagian coach (pakai `GetSectionUnitsDictAsync`).
- **Coach-assign unit-picker:** operator pilih unit (dari `coachee.UserUnits`) untuk tiap mapping; `AssignmentUnit` per-coachee (bukan 1 nilai batch, `CoachMappingController.cs:572-580`).
- Eligible-coachee list set-aware (coachee eligible bila Bagian-nya = Bagian coach).
- Coaching-role self-scope `CDPController.cs:305,326,636` (`unit = user.Unit`) → set `IN(user.Units)`.
- **File:** `CoachMappingController` (assign/eligible), `CDPController`, view `CoachCoacheeMapping.cshtml`.

### Fase 403 — OrganizationController cascade/guard UserUnits-aware
- Rename unit (`:218`)/reparent (`:251`) → update `UserUnits.Unit` (bukan cuma scalar `Users.Unit`).
- Reparent yg geser unit ke Bagian beda → harus blok/trigger mutasi (jaga invariant 1-Bagian; cegah split-brain Section).
- Delete-guard (`:391,447`) scan `UserUnits` (cegah hapus unit yg masih jadi membership sekunder).
- **File:** `OrganizationController` (terisolasi).

### Fase 404 — Test + UAT + docs
- Test multi-unit **WAJIB SQL riil** (EF-InMemory tidak enforce filtered-unique-index — bisa lolos in-memory, gagal prod). Fixture baru: pekerja {X,Y}, coach cross-unit, PROTON T1@X→T2@Y.
- Invariant test: `ProtonKompetensi.Unit` 1:1 per deliverable (B-06 guard aman).
- UAT lokal (build+run+DB lokal per DEV_WORKFLOW) + Playwright bila ada.
- Docs: catat batasan **D1=b** (cert/analytics atribusi primary).

## 6. Dependency & Paralelisme Eksekusi

```
Wave 0:   399  (FOUNDATION — solo, semua nunggu junction + kontrak mirror)
   ┌──────────┼──────────┐
Wave 1:  400      401      403     ← PARALEL ×3 (cluster file disjoint)
              │
Wave 2:       402                  ← setelah 401 (shared CoachMapping+CDP + butuh aturan AssignmentUnit)
              │
Wave 3:       404                  ← setelah semua (399–403)
```

- **399 wajib solo** — junction + kontrak primary-mirror dipakai semua.
- **Wave 1 {400, 401, 403} paralel** — cluster file disjoint (400=`WorkerDataService`/`WorkerController`/CMP-view; 401=`CoachMapping`/`CDP`/`ProtonData`/`Bypass`/`AssessmentAdmin`; 403=`OrganizationController` terisolasi). Eksekusi via **git worktree terpisah per fase**, merge tiap selesai.
- **402 TIDAK paralel dgn 401** — dua-duanya berat di `CoachMappingController`+`CDPController`, dan 402 nulis `AssignmentUnit` yg aturan validasinya dibuat 401. → setelah 401.
- **Critical path:** `399 → 401 → 402 → 404`.

ROADMAP "Depends on": 400/401/403 → 399; 402 → 401; 404 → 400+402+403.

## 7. Invariant & Constraint (WAJIB dijaga)

1. **Section scalar** — 1 Bagian per akun. Semua `UserUnits.Unit` anak dari `Section`. Cross-Bagian via mutasi (out-of-scope).
2. **PROTON single-active** — tepat 1 `ProtonTrackAssignment` aktif; index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` dipertahankan.
3. **Primary mirror konsisten** — `ApplicationUser.Unit` selalu = baris `UserUnits.IsPrimary`. Tiap write sinkron; hapus primary → promote/blok.
4. **`AssignmentUnit ∈ coachee.UserUnits`** — setelah Fase 401, coaching/PROTON cuma boleh ke unit yg dimiliki coachee.
5. **`ProtonKompetensi.Unit` 1:1 per deliverable** — jaga B-06 anti-dobel guard (per CoacheeId+DeliverableId) tetap aman lintas-unit.

## 8. Out of Scope (eksplisit)

- **PROTON paralel** (2 track aktif barengan / 2 unit konkuren) — butuh relax unique index + kolom Unit di `ProtonTrackAssignment` + re-key ~21 `FirstOrDefault(aktif)` + routing cert/penanda per-assignment. **Tidak dikerjakan** (sekuensial dikonfirmasi).
- **Cert/analytics atribusi per-unit akurat** (D1=b memilih primary) — bila nanti dibutuhkan, tambah kolom unit-at-issue di `AssessmentSession`/`TrainingRecord` + backfill (milestone terpisah).
- **Multi-Bagian per akun** — mutasi tetap proses ganti-Bagian tunggal.
- **Multi-Role** (1 pekerja >1 role) — riset terpisah ([[project_akun_multirole_multiunit_research]]), bukan milestone ini.

## 9. Migration & IT Notes

- **1 migration:** `AddUserUnitsTable` (+ filtered-unique index primary). Data backfill 1 primary row/pekerja (idempotent, bisa di `SeedData` atau migration `Up`).
- **migration flag IT = TRUE.** Notify IT dgn commit hash saat promosi ke Dev.
- `ApplicationUser.Section`/`Unit` **tidak di-drop** (mirror permanen utk fase ini).

## 10. Risiko

| Risiko | Mitigasi |
|--------|----------|
| `CleanupCoachCoacheeMappingOrg` reset `AssignmentUnit`→primary = data-loss multi-unit | Fase 401 jadikan UserUnits-aware/gated **sebelum** data multi-unit dipakai produksi |
| Reparent unit lintas-Bagian bikin split-brain Section | Fase 403 blok/trigger mutasi; jaga invariant 1-Bagian |
| Primary-mirror desync (write tak sinkron) | Kontrak write-through terpusat + test |
| EF-InMemory tak enforce filtered-unique-index → test palsu hijau | Fase 404 test multi-unit WAJIB SQL riil (SQLEXPRESS) |
| Atribusi cert primary bikin cert unit-Y muncul di laporan unit-X | Diterima (D1=b), didokumentasikan di UI/docs |
