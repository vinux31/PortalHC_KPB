# Phase 421: Retake Lifecycle Hardening - Research

**Researched:** 2026-06-23
**Domain:** ASP.NET Core 8 MVC + EF Core 8 + Bootstrap 5/Razor (server-rendered) — retake lifecycle integrity hardening (logic/guard, no schema)
**Confidence:** HIGH (semua temuan di-ground ke source aktual yang dibaca baris-per-baris dalam sesi ini)

## Summary

Phase 421 menutup 5 lubang integritas lifecycle ujian-ulang di atas mesin retake v32.4 yang sudah ada (`Helpers/RetakeRules.cs`, `Services/RetakeService.cs`, `Controllers/CMPController.cs`, `Controllers/AssessmentAdminController.cs`). Ini **bukan fitur baru** — murni hardening: gate window sebelum destruksi retake (RTH-01, HIGH), nol-kan NomorSertifikat saat HC reset (RTH-02), satu-sumber penghitungan percobaan (RTH-03), guard hapus peserta untuk sesi ber-riwayat + cleanup arsip (RTH-04), dan konfirmasi saat MaxAttempts diturunkan (RTH-05). Semua keputusan implementasi sudah TERKUNCI di CONTEXT (D-01..D-07) dan kontrak UI sudah ditetapkan di UI-SPEC.

Temuan paling penting yang menyederhanakan planning: **(1)** `RetakeRules.CanRetake` punya tepat SATU pemanggil produksi langsung (`RetakeService.CanRetakeAsync:244`) — blast radius perubahan signature sangat sempit. **(2)** `ResetAssessment` (controller) mendelegasikan SELURUH mutasi reset ke `RetakeService.ExecuteAsync` — jadi menambah `NomorSertifikat=null` ke satu `ExecuteUpdateAsync` di service (D-03) otomatis memenuhi jalur HC; tidak ada nol-kan-cert terpisah di controller. **(3)** Predikat snapshot-presence (D-05) sudah identik di 3 situs (`RetakeService:145-150`, `RetakeService:237-242`, `CMPController:2472-2475`); hanya situs ke-4 (warning ManagePackages `AssessmentAdminController:5795-5798`) yang divergen. **(4)** Pada branch ITHandoff jalur hapus-peserta SATU-SATUNYA yang berfungsi adalah loop `removedUserIds` di `EditAssessment` POST (Pre-Post saja); endpoint `DeleteAssessmentPeserta` yang dirujuk view TIDAK ADA di controller mana pun (dead form, v32.5 ada di `main`). **(5)** Cascade `AttemptResponseArchive → AttemptHistory` SUDAH dikonfigurasi `OnDelete(DeleteBehavior.Cascade)` di DbContext — cleanup orphan D-06 bisa mengandalkan cascade DB, tapi guard delete saat ini menghapus `AttemptHistory` by `SessionId` tanpa menolak/peringatkan sesi ber-riwayat.

**Primary recommendation:** Kerjakan dalam 5 unit kecil yang dipetakan 1:1 ke D-01..D-07. Mulai dari pure-rules change (RetakeRules.CanRetake + window param + RetakeRulesTests) sebagai keystone, lalu service-layer (D-03 cert-null + D-01 abort-before-destroy), lalu helper kill-drift D-05 (`CountEraRetakeArchives`), lalu controller guards (D-02, D-04, D-06, D-07) + view affordances. migration=FALSE terkonfirmasi.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (D-01..D-07 — JANGAN re-litigasi)

- **D-01 (gate dua lapis, RTH-01):**
  - **Eligibility:** `RetakeRules.CanRetake` menerima parameter baru `examWindowCloseDate` (+ konvensi waktu +7h WIB sama dengan `StartExam`) → return `false` bila window sudah tutup. Efek: tombol "Ujian Ulang" otomatis disembunyikan + tier-feedback (`ResolveReviewMode`) konsisten.
  - **Eksekusi (defense-in-depth):** `RetakeService.ExecuteAsync` **abort SEBELUM** `RemoveRange`/`ExecuteUpdateAsync` bila window tutup. Cegah race antara eligibility-check dan klaim atomik.
- **D-02 (peringatan dini HC, RTH-01):** Di `UpdateRetakeSettings`, bila `RetakeCooldownHours` bisa mendorong eligibility lewat sisa window sampai `ExamWindowCloseDate`, tampilkan **warning non-blocking** ke HC. Setelan tetap tersimpan.
- **D-03 (nol-kan cert, RTH-02):** Tambah `.SetProperty(r => r.NomorSertifikat, (string?)null)` pada `ExecuteUpdateAsync` di jalur reset (`RetakeService.cs:103-112`) **dan** pastikan `ResetAssessment` menol-kan NomorSertifikat juga.
- **D-04 (konfirmasi cabut cert, RTH-02):** Sebelum HC reset sesi yang **sudah LULUS / ber-NomorSertifikat**, peringatkan + minta **konfirmasi** bahwa sertifikat akan dicabut.
- **D-05 (samakan counting, RTH-03):** Tambahkan predikat **snapshot-presence** yang sama (dipakai cap) ke warning ManagePackages. Ekstrak helper bersama **`CountEraRetakeArchives`** dan wire ke keempat situs hitung. Nama & posisi helper = diskresi Claude asal satu sumber kebenaran.
- **D-06 (soft-confirm + cleanup, server round-trip, RTH-04):** Perluas guard hapus peserta (EditAssessment POST `removedUserIds` loop) agar mendeteksi sesi **Abandoned** atau punya **AttemptHistory/StartedAt**. Soft-confirm via **server round-trip + hidden flag** (BUKAN modal/JS murni). Cleanup wajib **cascade hapus `AssessmentAttemptResponseArchives` by AttemptHistoryId**.
- **D-07 (konfirmasi pra-simpan non-blocking, RTH-05):** Saat HC menurunkan `MaxAttempts` di bawah jumlah percobaan terpakai, tampilkan **modal konfirmasi pra-simpan**. Tetap **non-blocking** — bila dikonfirmasi, simpan apa adanya. Hitung "terpakai" pakai helper snapshot-presence yang sama (D-05).

### Claude's Discretion
- Nama & posisi helper `CountEraRetakeArchives`, selama keempat situs hitung memakai predikat snapshot-presence identik.
- Presisi pesan/teks peringatan & konfirmasi (Bahasa Indonesia), posisi guard, bentuk modal — ikuti idiom existing (TempData untuk toast non-blocking; JS confirm/modal Bootstrap untuk konfirmasi pra-aksi).
- Konvensi +7h WIB untuk perbandingan window pada D-01 — reuse pola persis `StartExam`.
- **Backward-compat WAJIB:** jalur worker retake existing (eligibility, tier-feedback leak-safe A1, cap, cooldown) tak berubah perilaku kecuali penambahan gate window. Assessment Standard (non Pre-Post) tetap retakeable seperti existing.

### Deferred Ideas (OUT OF SCOPE)
- Strategi grading retake selain "attempt terakhir" (highest/avg), cooldown escalating, rotasi AccessToken per-attempt, cap per-tahun — YAGNI v32.4.
- Dedicated participant-management endpoint (v32.5 FlexibleParticipantRemove) ada di branch `main` — JANGAN tarik ke ITHandoff; soft-confirm RTH-04 dikerjakan di alur EditAssessment POST existing.
- Tidak ada scope creep lain — diskusi tetap dalam batas 5 REQ RTH.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **RTH-01** | Ujian ulang ditolak bila ExamWindowCloseDate lewat — sesi live tidak dihapus (anti dead-end). [RTK-LOGIC-02, HIGH] | D-01 dua-lapis: param window di `RetakeRules.CanRetake` (eligibility) + abort di `RetakeService.ExecuteAsync` sebelum `RemoveRange` (eksekusi). Konvensi +7h verbatim dari `StartExam:956`. D-02 warning di `UpdateRetakeSettings`. |
| **RTH-02** | Reset HC menghapus NomorSertifikat (tak ada nomor menggantung). [RTK-LOGIC-01] | D-03 satu-baris `SetProperty(NomorSertifikat, null)` di `RetakeService:103-112`; `ResetAssessment` mendelegasi ke service → otomatis tercakup. D-04 confirm cabut-cert di view `AssessmentMonitoringDetail:335` (conditional copy via `session.IsPassed`/`NomorSertifikat`). |
| **RTH-03** | Counting percobaan konsisten cap vs warning ManagePackages (satu sumber). [RTK-LOGIC-03] | D-05 helper `CountEraRetakeArchives` — 3 situs cap sudah identik, situs warning `:5795-5798` divergen (no snapshot filter). |
| **RTH-04** | Guard hapus peserta tangani Abandoned/ber-riwayat + bersihkan arsip. [PA-06] | D-06: perluas guard `:1930-1939` (kini hanya InProgress/Completed) + server round-trip hidden flag + cleanup archives by AttemptHistoryId (cascade DB sudah ada). |
| **RTH-05** | Turunkan MaxAttempts < terpakai → peringatan non-blocking. [VAL-06] | D-07 modal pra-simpan di `ManagePackages.cshtml`/`UpdateRetakeSettings`; hitung "terpakai" pakai helper D-05. Inline warning pasca-simpan `:157-163` sudah ada. |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Bahasa:** SEMUA teks UI hadap-pengguna WAJIB Bahasa Indonesia. Respons juga Bahasa Indonesia.
- **Develop Workflow:** Branch ITHandoff → jalankan lokal di `http://localhost:5270` (BUKAN 5277 — itu worktree `main`). Override `dotnet run --urls` + Playwright baseURL; JANGAN commit `launchSettings.json`.
- **Verifikasi wajib sebelum commit:** `dotnet build` + `dotnet run` (cek @5270) + cek DB lokal (+ Playwright bila ada). Jangan push tanpa verifikasi lokal.
- **Deploy ke Dev/Prod = tanggung jawab Team IT.** Developer notify IT dengan commit hash + flag migration. Jangan edit kode/DB langsung di server Dev/Prod.
- **Seed Workflow:** Klasifikasi seed (temporary local-only vs permanent prod). Snapshot DB sebelum insert seed temporary, catat di `docs/SEED_JOURNAL.md`, restore setelah test. `sqlcmd` butuh `-C -I` (TrustServerCertificate). Untuk fase ini, seed test pakai disposable-DB xUnit (lihat Validation Architecture), bukan DB lokal kerja.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Keputusan eligibility retake (window gate, cooldown, cap) | Helper (pure `RetakeRules`) | Service (`CanRetakeAsync` suplai fakta DB) | Pure-rules kill-drift: caller suplai fakta, keputusan terpusat & unit-testable. Window param baru masuk di sini (D-01 eligibility). |
| Eksekusi destruktif retake (claim-atomik → snapshot → delete → cert-null) | Service (`RetakeService.ExecuteAsync`) | — | Satu mesin transaksional bersama HC-reset + worker-retake; abort-before-destroy (D-01 eksekusi) + cert-null (D-03) di sini. |
| Penghitungan era-retake (snapshot-presence) | Helper baru `CountEraRetakeArchives` (DB-aware) | Service + 2 controller | Kill-drift: satu predikat untuk 4 situs (D-05). |
| Guard + soft-confirm hapus peserta | Controller (`EditAssessment` POST) | View (hidden flag + tombol) | Server-authoritative round-trip (D-06); keputusan akhir di server, bukan JS. |
| Warning/konfirmasi non-blocking (cooldown>window, MaxAttempts<terpakai, cabut cert) | View (Razor/Bootstrap) | Controller (TempData/ViewBag trigger) | Lapis UX; eksekusi tetap server-side (gate, cert-null, hapus arsip). |
| Visibilitas tombol "Ujian Ulang" / tier feedback | View (`Results.cshtml`) | Helper (eligibility flag) | Eligibility-driven: `CanRetake=false` → cabang `else if (Model.CanRetake)` tak render (tak ada komponen baru). |

## Standard Stack

Fase ini TIDAK menambah library baru. Semua sudah ada di solution (verified via `*.csproj` grep).

### Core (verified, terinstal)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller + Razor server-rendered | `TargetFramework=net8.0` (verified semua csproj) |
| EF Core (SqlServer) | 8.0.0 | ORM + `ExecuteUpdateAsync` (set-based bulk update) | `Microsoft.EntityFrameworkCore.SqlServer 8.0.0` (verified csproj) |
| Bootstrap 5 + Bootstrap Icons | (bundled, server-rendered) | Alert/Modal/Form/confirm | Idiom existing lintas `Views/Admin/*` + `Views/CMP/*` (UI-SPEC locked) |

### Supporting (test, verified)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit | 2.9.3 | Test framework | Semua unit + integration test |
| xunit.runner.visualstudio | 3.0.1 | Test runner | `dotnet test` |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test SDK | `dotnet test` |
| EF Core SqlServer (test DB) | 8.0.0 | Real-SQL integration via disposable DB | Integration test ber-`[Trait("Category","Integration")]` |

**Installation:** N/A — tidak ada paket baru. `[ASSUMED]` tidak relevan: tak ada dependency tambahan.

**Version verification:** Dilakukan via `grep PackageReference` pada `*.csproj` — `[VERIFIED: *.csproj]`. Tidak ada `npm`/registry lookup karena ini solusi .NET tanpa paket baru.

## Architecture Patterns

### System Flow Diagram (retake lifecycle, dengan gate baru)

```
                    ┌─────────────────────────────────────────────────────────┐
                    │  ELIGIBILITY (pure, EF-free) — RetakeRules.CanRetake     │
   Worker buka      │  allowRetake → !PreTest → !Manual → status=Completed →   │
   Results.cshtml ─▶│  isPassed==false → attemptsUsed<maxAttempts → cooldown   │
                    │  ★ + NEW: examWindowCloseDate gate (D-01) ★              │
                    └───────────────┬─────────────────────────────────────────┘
                                    │ CanRetakeAsync (suplai fakta DB: snapshot count, window)
                       false ◀──────┤──────▶ true
                  (tombol auto-hide)│        (render tombol "Ujian Ulang")
                                    ▼
   POST /CMP/RetakeExam ──▶ re-check CanRetakeAsync (server-authoritative)
                                    │
                                    ▼
   HC POST /ResetAssessment ──▶ guard (IsResettable, Pre-Post, status) ──┐
                                                                          │
                                    ┌─────────────────────────────────────▼──────────────┐
                                    │  EXECUTION — RetakeService.ExecuteAsync (TX atomik)  │
                                    │  ★ NEW (D-01): abort bila window tutup SEBELUM ↓ ★   │
                                    │  1. claim-atomik: ExecuteUpdateAsync Status→Open,    │
                                    │     null skor/IsPassed/Progress/StartedAt/...        │
                                    │     ★ NEW (D-03): + NomorSertifikat=null ★           │
                                    │  2. snapshot per-soal (jika wasCompleted)            │
                                    │  3. RemoveRange responses/assignment/ET-scores       │
                                    │  4. commit → audit → SignalR sessionReset            │
                                    └──────────────────────────────────────────────────────┘
                                    │
                                    ▼
   Worker redirect /StartExam ──▶ window enforcement (:956, now+7h > EWCD → blok)
                  (TANPA gate baru: ini titik dead-end lama yang D-01 cegah di hulu)

   ── COUNTING (D-05 kill-drift) — helper CountEraRetakeArchives dipakai 4 situs:
      (a) RetakeService.ExecuteAsync :145-150   (b) RetakeService.CanRetakeAsync :237-242
      (c) CMPController Results VM :2472-2475    (d) ManagePackages warning :5795-5798 ◀ DIVERGEN
```

### Pattern 1: Pure-rules kill-drift (RetakeRules / ShuffleToggleRules)
**What:** Keputusan terpusat di helper static PURE (EF-free, sinkron, `nowUtc` di-inject). Caller suplai SEMUA fakta sebagai parameter.
**When to use:** D-01 eligibility — tambah `DateTime? examWindowCloseDate` param ke `CanRetake`.
**Example (existing `CanRetake` signature yang akan diubah):**
```csharp
// Source: Helpers/RetakeRules.cs:29-52 (VERIFIED)
public static bool CanRetake(
    bool allowRetake, string? assessmentType, bool isManualEntry,
    string status, bool? isPassed, int attemptsUsed, int maxAttempts,
    int retakeCooldownHours, DateTime? completedAt, DateTime nowUtc)
{
    if (!allowRetake) return false;
    if (assessmentType == "PreTest") return false;
    if (isManualEntry) return false;
    if (status != "Completed") return false;
    if (isPassed != false) return false;
    if (attemptsUsed >= maxAttempts) return false;
    // ★ D-01: gate window di sini, SEBELUM/SESUDAH cooldown (urutan diskresi planner) ★
    //   konvensi +7h WIB verbatim: nowUtc.AddHours(7) > examWindowCloseDate.Value → false
    if (retakeCooldownHours <= 0) return true;
    if (completedAt == null) return false;
    return nowUtc >= completedAt.Value.AddHours(retakeCooldownHours);
}
```
**Konvensi +7h WIB (HARUS byte-identik dengan StartExam):**
```csharp
// Source: CMPController.cs:956 (VERIFIED) — gunakan ekspresi ini PERSIS
if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value)
// Di RetakeRules (pure, nowUtc injected): nowUtc.AddHours(7) > examWindowCloseDate.Value
```

### Pattern 2: Snapshot-presence predicate (D-05, kill-drift counting)
**What:** "Era-retake archive" = `AssessmentAttemptHistory` yang punya ≥1 child `AssessmentAttemptResponseArchive`. Legacy HC-reset pre-v32.4 (tanpa snapshot child) natural-excluded dari cap.
**Predikat IDENTIK di 3 situs (verified — keystone untuk helper):**
```csharp
// Source: RetakeService.cs:145-150 & :237-242, CMPController.cs:2472-2475 (VERIFIED — 3 situs identik)
_context.AssessmentAttemptHistory
    .Where(h => h.UserId == X && h.Title == Y && h.Category == Z
             && _context.AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id))
    .CountAsync();
```
**Situs ke-4 (DIVERGEN — tidak pakai snapshot filter, keys hanya Title+Category):**
```csharp
// Source: AssessmentAdminController.cs:5795-5798 (VERIFIED) — INI yang harus diselaraskan D-05
int retakeMaxArchivedForGroup = await _context.AssessmentAttemptHistory
    .Where(h => h.Title == assessment.Title && h.Category == assessment.Category)  // ❌ no snapshot, no UserId-scope
    .GroupBy(h => h.UserId).Select(g => g.Count()).OrderByDescending(c => c).FirstOrDefaultAsync();
ViewBag.RetakeMaxAttemptsUsedInGroup = retakeMaxArchivedForGroup + 1;
```
**Catatan semantik penting:** Situs cap (a/b/c) menghitung untuk SATU user spesifik `(UserId, Title, Category)`. Situs warning (d) mencari MAX di antara SEMUA user di grup (group-by UserId). Helper `CountEraRetakeArchives` harus mendukung kedua bentuk: per-user count, dan untuk warning = max-per-user-in-group. Saran: helper terima predikat dasar (snapshot-presence + Title/Category) lalu caller pilih `.Where(UserId==x).Count()` vs `.GroupBy(UserId).Max()`. **Pertahankan +1** (current attempt = arsip + 1) di semua situs.

### Pattern 3: ExecuteUpdateAsync set-based reset (D-03)
**What:** Reset = satu `ExecuteUpdateAsync` dengan 9 `SetProperty` (set-based, tanpa load entity), guard `WHERE Status != Cancelled && != Open`.
**Where to add cert-null (D-03):** tambah 1 baris ke 9 SetProperty existing.
```csharp
// Source: RetakeService.cs:101-112 (VERIFIED) — tambah SetProperty NomorSertifikat
var rows = await _context.AssessmentSessions
    .Where(s => s.Id == sessionId && s.Status != "Cancelled" && s.Status != "Open")
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Status, "Open")
        .SetProperty(r => r.Score, (int?)null)
        .SetProperty(r => r.IsPassed, (bool?)null)
        .SetProperty(r => r.Progress, 0)
        .SetProperty(r => r.StartedAt, (DateTime?)null)
        .SetProperty(r => r.CompletedAt, (DateTime?)null)
        .SetProperty(r => r.ElapsedSeconds, (int)0)
        .SetProperty(r => r.LastActivePage, (int?)null)
        // ★ D-03: + .SetProperty(r => r.NomorSertifikat, (string?)null) ★
        .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));
```

### Pattern 4: Server round-trip + hidden confirm flag (D-06)
**What:** POST destruktif → server deteksi kondisi → batalkan + render warning → user submit ulang dengan hidden flag → server eksekusi. Server-authoritative (bukan JS confirm).
**Existing reuse target:** guard di `EditAssessment` POST `:1930-1939` (kini TempData+continue saat InProgress/Completed). Tambah param `bool confirmRemoveWithHistory = false` ke signature `EditAssessment(int id, AssessmentSession model, List<string> NewUserIds, ..., List<string>? UserIds)` (`:1823-1826`, VERIFIED).
```csharp
// Source: AssessmentAdminController.cs:1929-1939 (VERIFIED) — guard SAAT INI (hanya InProgress/Completed)
if (userPreSession != null && (userPreSession.Status == "InProgress" || userPreSession.Status == "Completed")) {
    TempData["Error"] = $"Tidak dapat menghapus peserta — sesi Pre-Test sudah {userPreSession.Status}.";
    continue;
}
// ★ D-06: perluas deteksi → Abandoned OR StartedAt!=null OR ada AttemptHistory by SessionId.
//   Bila terdeteksi & !confirmRemoveWithHistory → TempData["Warning"] + continue (batal hapus).
//   Bila confirmRemoveWithHistory==true → lanjut hapus + cleanup archives.
```

### Anti-Patterns to Avoid
- **JS-only confirm untuk hapus ber-riwayat (D-06):** native `confirm()` boleh first-line UX tapi keputusan akhir HARUS server (hidden flag). Audit menuntut server-authoritative.
- **Duplikasi predikat snapshot-presence (D-05):** jangan tulis ulang `.Any(a => a.AttemptHistoryId == h.Id)` di situs ke-4 — ekstrak helper agar 4 situs tak drift lagi.
- **Drift konvensi waktu (D-01):** jangan pakai `DateTime.Now`, timezone library, atau `+8h`/UTC-only. WAJIB `nowUtc.AddHours(7) > examWindowCloseDate` (verbatim StartExam:956).
- **Hard-block pada D-07/D-02/D-05:** semua warning ini NON-BLOCKING. Setelan tetap tersimpan setelah konfirmasi. Jangan mengubah perilaku retroaktif existing (`RetakeRules.cs:46`).
- **Menarik v32.5 FlexibleParticipantRemove dari `main`:** OUT OF SCOPE. Kerjakan di `EditAssessment` POST existing.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Window gate eligibility | Cek window terpisah di tiap controller | Param `examWindowCloseDate` di `RetakeRules.CanRetake` (1 tempat) | Eligibility-driven: button hide + tier feedback ikut otomatis; 1 pemanggil produksi. |
| Konversi WIB | Library timezone / `TimeZoneInfo` | `nowUtc.AddHours(7)` verbatim StartExam:956 | Hindari TZ drift; konsisten dengan enforcement existing. |
| Counting era-retake | GroupBy ad-hoc per situs | Helper `CountEraRetakeArchives` (snapshot-presence) | 3 situs sudah identik; situs ke-4 drift = bug RTH-03. |
| Cleanup orphan archive | Load + RemoveRange archives manual di setiap delete | DB cascade `OnDelete(Cascade)` (sudah ada) saat AttemptHistory dihapus | Cascade DB sudah dikonfigurasi (DbContext:588-594); cukup pastikan AttemptHistory dihapus. |
| Cert-null saat reset | Nol-kan cert di controller ResetAssessment terpisah | 1 SetProperty di `RetakeService.ExecuteUpdateAsync` | ResetAssessment mendelegasi penuh ke service — 1 tempat. |

**Key insight:** Semua 5 lubang adalah konsekuensi dari logika yang sudah hampir-terpusat tapi bocor di 1 titik. Hardening = menutup titik bocor dengan reuse pola existing, BUKAN membangun mekanisme baru.

## Runtime State Inventory

> Fase ini = logic/guard hardening, BUKAN rename/refactor/migration string. Inventory tetap diisi untuk memastikan tidak ada state runtime tersembunyi yang perlu di-migrate.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | NomorSertifikat menggantung di sesi yang sudah di-HC-reset SEBELUM fix ini (IsPassed=false/null + NomorSertifikat≠null). Audit RTK-LOGIC-01: nomor menggantung mengisi unique index + meng-inflasi `certCount` proxy (`NomorSertifikat!=null`). | **Pertimbangkan data-cleanup opsional** (one-shot): nol-kan NomorSertifikat untuk sesi non-lulus existing. NOT diminta CONTEXT (D-03 hanya forward-fix). Flag ke planner sebagai Open Question — D-03 mencegah kasus BARU; data lama tetap menggantung kecuali ada migrasi data terpisah. Kemungkinan kecil karena worker-path sudah blok sesi lulus. |
| Live service config | None — tidak ada konfigurasi di service eksternal (n8n/Datadog/dll) yang menyimpan string retake. Verified: scope hanya kode + DB lokal/Dev. | None. |
| OS-registered state | None — tidak ada Task Scheduler / pm2 / systemd yang menyimpan state retake. Verified: aplikasi web ASP.NET, deploy via IT. | None. |
| Secrets/env vars | None — tidak ada secret/env var yang mereferensi retake config by name. Verified: retake config = kolom DB (`AllowRetake`/`MaxAttempts`/`RetakeCooldownHours`), bukan env. | None. |
| Build artifacts | None — tidak ada egg-info/binary/image tag. Perubahan C#/Razor di-compile ulang oleh `dotnet build`. | `dotnet build` (standar). |

## Common Pitfalls

### Pitfall 1: Cooldown countdown button tetap render saat window tutup (dead-end UX baru)
**What goes wrong:** D-01 membuat `CanRetake=false` saat window tutup (tombol "Ujian Ulang" hilang), TAPI cabang `else if (Model.IsInCooldown && CooldownUntilUtc.HasValue)` di `Results.cshtml:507` TIDAK bergantung window. Jika window tutup TAPI cooldown belum lewat, tombol countdown disabled masih muncul → worker tunggu countdown habis lalu tetap tak bisa retake (dead-end UX, lebih halus dari dead-end destruktif tapi tetap membingungkan).
**Why it happens:** `IsInCooldown` dihitung di `CMPController.cs:2493-2494` = `attemptsRemaining && IsPassed==false && CooldownUntilUtc > now` — tanpa cek window.
**How to avoid:** Saat D-01, pastikan `IsInCooldown` juga false bila window tutup (mis. AND dengan window-open check), ATAU render `alert alert-secondary` "Masa ujian sudah ditutup" (UI-SPEC copy: closed-window state) menggantikan cabang cooldown. Planner harus eksplisit memutuskan ini.
**Warning signs:** Test E2E/UAT: window tutup + cooldown aktif → masih ada tombol countdown.

### Pitfall 2: Tier feedback (ResolveReviewMode) tidak window-aware → kunci jawaban tetap disembunyikan padahal retake mustahil
**What goes wrong:** `attemptsRemaining` (CMPController:2480-2482) = "retake mungkin secara prinsip" — abaikan cooldown DAN window. Saat window tutup, retake mustahil, tapi `attemptsRemaining` masih true → `ResolveReviewMode` → `ShowWrongFlagsOnly` (kunci disembunyikan) padahal worker tak akan pernah bisa retake → seharusnya `ShowFullReview`.
**Why it happens:** `attemptsRemaining` sengaja mengabaikan timing (cooldown) by-design A1. Window adalah hard-close (bukan timing transient seperti cooldown), jadi semestinya diperlakukan seperti "exhausted".
**How to avoid:** Planner pertimbangkan: saat window tutup, set `attemptsRemaining=false` (atau setara) agar tier feedback membuka full review (retake sudah mustahil — tak ada gunanya menahan kunci). CONTEXT D-01 menyebut "tier-feedback (ResolveReviewMode) konsisten" — ini perlu keputusan eksplisit. **MEDIUM confidence** apakah CONTEXT bermaksud full window-awareness di tier atau cukup button-hide. Flag sebagai Open Question.
**Warning signs:** Window tutup, worker belum-lulus + attempt-sisa → kunci jawaban masih disembunyikan selamanya.

### Pitfall 3: Helper D-05 dipakai 2 bentuk berbeda (per-user vs max-in-group) → salah selaras
**What goes wrong:** Naif menyamakan semua 4 situs ke bentuk yang sama merusak semantik. Situs cap (a/b/c) = per-user-specific count. Situs warning (d) = max di antara semua user di grup.
**Why it happens:** Audit RTK-LOGIC-03 menyebut "GroupBy UserId tanpa filter" — yang dimaksud adalah ketiadaan snapshot-presence filter, BUKAN bahwa GroupBy-nya salah. GroupBy untuk warning memang benar (cari worker dengan attempt terbanyak).
**How to avoid:** Helper expose predikat dasar `IQueryable<AssessmentAttemptHistory>` (snapshot-presence + Title/Category), lalu caller terapkan `.Where(UserId==x)` (cap) atau `.GroupBy(UserId).Select(Count).Max()` (warning). Keduanya +1.
**Warning signs:** Warning count berubah jadi per-user (under-count) atau cap jadi cross-user (over-count).

### Pitfall 4: D-06 cascade orphan — RemoveRange tracked-parent vs untracked-child
**What goes wrong:** Loop delete existing menghapus `AssessmentAttemptHistory` by `SessionId` (`:1953-1956`) tanpa load archives. Cascade DB (`OnDelete(Cascade)`, DbContext:591-594) MENGHAPUS archives saat history dihapus — TAPI hanya jika DELETE benar-benar di-emit ke DB (EF tracked RemoveRange + SaveChanges → DB cascade trigger). Risiko: jika ada error/partial save, atau jika archives di-load tracked tanpa cascade-aware, bisa muncul orphan.
**Why it happens:** Audit PA-06 mengklaim orphan terjadi. Dengan cascade DB terkonfigurasi, orphan via path ini seharusnya TIDAK terjadi untuk AttemptHistory yang benar dihapus. Risiko nyata = AttemptHistory yang TIDAK dihapus (mis. ter-skip guard) meninggalkan archives "live" yang tak relevan.
**How to avoid:** D-06 cleanup eksplisit: setelah konfirmasi, hapus `AttemptHistory` by `SessionId` (cascade hapus archives), DAN verifikasi via test tidak ada `AssessmentAttemptResponseArchive` ber-`AttemptHistoryId` yatim setelah delete. Andalkan cascade DB; jangan double-delete (akan error). Lihat Validation Architecture (delete-guard test).
**Warning signs:** Query `AttemptResponseArchives` ber-AttemptHistoryId yang tak ada parent setelah hapus peserta.

### Pitfall 5: Data lama NomorSertifikat menggantung tidak terbersihkan oleh D-03
**What goes wrong:** D-03 forward-fix — hanya sesi yang di-reset SETELAH deploy yang ter-null. Sesi non-lulus existing yang sudah di-HC-reset SEBELUM fix tetap menyandang NomorSertifikat (inflasi `certCount` proxy + unique index).
**Why it happens:** D-03 = code change, bukan data migration. CONTEXT tidak meminta backfill.
**How to avoid:** Flag ke planner. Karena worker-path sudah blok reset sesi lulus (`RetakeRules.cs:45`), kasus ini langka (hanya HC-reset manual sesi lulus pre-fix). Planner putuskan apakah perlu one-shot cleanup script (NOT in 5 REQ — kemungkinan defer).
**Warning signs:** Hitungan cert (`NomorSertifikat!=null`) lebih tinggi dari jumlah sesi IsPassed=true.

## Code Examples

### Snapshot-presence count (per-user, untuk cap — D-05 source of truth)
```csharp
// Source: RetakeService.cs:237-242 (VERIFIED) — bentuk per-user
int eraRetakeArchives = await _context.AssessmentAttemptHistory
    .Where(h => h.UserId == s.UserId && h.Title == s.Title && h.Category == s.Category
             && _context.AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id))
    .CountAsync();
// attemptsUsed = eraRetakeArchives + 1
```

### Cascade FK config (D-06 cleanup mengandalkan ini)
```csharp
// Source: Data/ApplicationDbContext.cs:588-595 (VERIFIED)
builder.Entity<AssessmentAttemptResponseArchive>(entity =>
{
    entity.HasIndex(e => e.AttemptHistoryId);
    entity.HasOne(e => e.AttemptHistory)
          .WithMany()
          .HasForeignKey(e => e.AttemptHistoryId)
          .OnDelete(DeleteBehavior.Cascade);   // ← hapus AttemptHistory → archives ikut terhapus
});
```

### D-04 conditional confirm copy (view-only, VM sudah expose field)
```razor
@* Source: AssessmentMonitoringDetail.cshtml:334-341 (VERIFIED) + MonitoringSessionRow.IsPassed/NomorSertifikat (VERIFIED) *@
@{
    bool hasCert = session.IsPassed == true || session.NomorSertifikat != null;
    string resetConfirm = hasCert
        ? "Reset sesi ini? Sesi ini SUDAH LULUS dan memiliki nomor sertifikat. Mereset akan MENCABUT sertifikat (nomor dihapus) dan semua jawaban dihapus. Peserta dapat mengulang ujian. Lanjutkan?"
        : "Reset sesi ini? Semua jawaban akan dihapus dan peserta dapat mengulang ujian.";
}
<form asp-action="ResetAssessment" ... onsubmit="return confirm('@Html.Raw(Json.Serialize(resetConfirm))')">
```
**Catatan:** `MonitoringSessionRow` (AssessmentMonitoringViewModel.cs:53-67) SUDAH expose `IsPassed` (bool?) + `NomorSertifikat` (string?) — TIDAK perlu ubah VM/controller untuk D-04.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Reset inline di controller (archive→delete→reset) | Delegasi ke `RetakeService.ExecuteAsync` (TX atomik bersama HC+worker) | v32.4 (Phase 405) | D-01/D-03 cukup ubah service (1 tempat) → HC+worker konsisten |
| Counting attempt ad-hoc | snapshot-presence di 3 situs (belum 4) | v32.4 | D-05 tinggal selaraskan situs ke-4 |
| `confirm()` JS untuk semua aksi | server round-trip untuk destruktif kritis | — (D-06 baru di fase ini) | Server-authoritative untuk hapus ber-riwayat |

**Deprecated/outdated:**
- Endpoint `DeleteAssessmentPeserta` yang dirujuk `EditAssessment.cshtml:709` (`deletePesertaForm`) **TIDAK ADA di controller mana pun di ITHandoff** (grep 0 match) — dead form (v32.5 FlexibleParticipantRemove ada di `main`). Hapus-peserta yang berfungsi HANYA loop `removedUserIds` Pre-Post. **JANGAN bingung kedua mekanisme ini** saat implement D-06.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | CONTEXT D-01 "tier-feedback konsisten" cukup dipenuhi dengan button-hide via `CanRetake=false`; full window-awareness pada `ResolveReviewMode.attemptsRemaining` adalah enhancement opsional | Pitfall 2 / Open Q | Bila user mengharapkan kunci jawaban dibuka penuh saat window tutup, scope D-01 lebih besar dari sekadar button-hide |
| A2 | Data lama NomorSertifikat menggantung TIDAK perlu di-backfill (D-03 forward-fix saja) | Runtime State / Pitfall 5 | Bila compliance butuh `certCount` proxy akurat segera, perlu one-shot cleanup (di luar 5 REQ) |
| A3 | DB cascade `OnDelete(Cascade)` cukup untuk cleanup orphan D-06; tak perlu explicit RemoveRange archives | Pitfall 4 | Bila ada path yang menghapus AttemptHistory via raw SQL / detached tanpa trigger cascade, orphan tetap muncul (low — semua path EF tracked) |
| A4 | `IsInCooldown` countdown button perlu di-gate window (Pitfall 1) sebagai bagian D-01 | Pitfall 1 | Bila tak di-gate, muncul dead-end UX baru (countdown habis tapi tetap tak bisa retake) |

**Catatan:** A1 & A4 adalah keputusan desain yang harus dikonfirmasi planner/discuss; A2 & A3 adalah risiko data/teknis rendah. Tidak ada `[ASSUMED]` pada fakta library/versi/source (semua `[VERIFIED]`).

## Open Questions

1. **Apakah D-01 window-gate harus merembet ke `ResolveReviewMode` (tier feedback) dan ke cabang `IsInCooldown` (countdown button), bukan hanya `CanRetake`?**
   - What we know: `CanRetake=false` menyembunyikan tombol utama (UI-SPEC eksplisit). `IsInCooldown` (Results.cshtml:507) & `attemptsRemaining` (tier) TIDAK bergantung window.
   - What's unclear: CONTEXT menyebut "tier-feedback konsisten" + "tak pernah masuk jalur destruktif" — apakah ini menuntut tier full-review saat window tutup + suppress countdown button?
   - Recommendation: Planner gate KEDUA-nya (countdown + tier) pada window. Aman secara UX (window tutup = retake mustahil = perlakukan seperti exhausted). Konfirmasi via UAT.

2. **Apakah perlu data-cleanup one-shot untuk NomorSertifikat menggantung existing (pre-fix)?**
   - What we know: D-03 forward-fix; worker-path sudah blok reset sesi lulus → kasus langka.
   - What's unclear: Berapa banyak sesi non-lulus ber-NomorSertifikat ada di DB Dev/Prod.
   - Recommendation: Defer (di luar 5 REQ). Bila planner ingin, tambah task opsional cleanup terpisah dengan snapshot DB (Seed Workflow). Bisa dicek via query: `SELECT COUNT(*) FROM AssessmentSessions WHERE NomorSertifikat IS NOT NULL AND (IsPassed = 0 OR IsPassed IS NULL)`.

3. **Posisi gate window di `CanRetake` — sebelum atau sesudah cooldown check?**
   - What we know: Urutan guard fail-fast existing. Window dan cooldown independen.
   - What's unclear: Tidak ada dampak korektnes (keduanya return false bila gagal); hanya urutan evaluasi.
   - Recommendation: Letakkan window gate SEBELUM cooldown (window = hard-close lebih fundamental dari timing cooldown). Diskresi planner; tak load-bearing.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (net8.0) | build + run | ✓ (asumsi — proyek aktif di-build) | net8.0 | — |
| SQL Server (LocalDB/SQLEXPRESS) | run lokal + integration test | ✓ (test fixture pakai `localhost\SQLEXPRESS`) | — | InMemory provider (ADA: `Microsoft.EntityFrameworkCore.InMemory 8.0.0`) untuk unit non-SQL |
| Playwright | E2E UAT lifecycle | ✓ (dipakai fase sebelumnya, e.g. 408/413) | — | UAT manual browser @5270 |

**Missing dependencies with no fallback:** None — semua tool sudah dipakai fase 405-420 di branch ini.
**Missing dependencies with fallback:** Integration test butuh SQLEXPRESS live; bila CI SQL-less, `[Trait("Category","Integration")]` di-skip via `--filter "Category!=Integration"` (pola existing).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 (verified csproj) |
| Config file | none (xUnit konvensi; `HcPortal.Tests.csproj`) |
| Quick run command | `dotnet test --filter "Category!=Integration"` (pure unit, SQL-less, cepat) |
| Full suite command | `dotnet test` (incl integration real-SQL @ `localhost\SQLEXPRESS`) |
| Integration convention | `[Trait("Category","Integration")]` + disposable DB `HcPortalDB_Test_{guid}` + `MigrateAsync` full chain + `EnsureDeletedAsync` on dispose + `NoOpHubContext` (tak ada Moq) + `NullLogger`. (Source: RetakeServiceTests.cs:1-67 VERIFIED) |
| E2E | Playwright @ `http://localhost:5270` (branch ITHandoff port) untuk UAT lifecycle |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| RTH-01 | `CanRetake` return false saat window tutup; boundary +7h WIB (now+7h > EWCD) | unit (pure) | `dotnet test --filter "FullyQualifiedName~RetakeRulesTests"` | ✅ extend RetakeRulesTests.cs (`Can(...)` helper + param baru) |
| RTH-01 | `ExecuteAsync` abort SEBELUM RemoveRange bila window tutup (sesi live utuh, tak jadi shell) | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~RetakeServiceTests"` | ✅ extend RetakeServiceTests.cs (seed EWCD lewat + assert responses/assignment masih ada + Status tak berubah) |
| RTH-02 | Reset → NomorSertifikat == null (sesi tak menggantung cert) | integration | `dotnet test --filter "FullyQualifiedName~RetakeServiceTests"` | ✅ extend RetakeServiceTests.cs (seed sesi lulus+cert → ExecuteAsync → assert NomorSertifikat null) |
| RTH-03 | Counting parity: cap (per-user) == warning predikat (snapshot-presence), legacy archive tak dihitung | unit + integration | `dotnet test --filter "FullyQualifiedName~CountEraRetakeArchives"` | ❌ Wave 0 (helper baru + test baru) |
| RTH-04 | Delete-guard: Abandoned/ber-riwayat ditolak tanpa flag; dengan flag dihapus + 0 orphan archive | integration | `dotnet test --filter "FullyQualifiedName~ParticipantRemove"` | ❌ Wave 0 (replikasi body `EditAssessment` removedUserIds loop atas real-SQL, pola RetakeSettingsEndpointTests) |
| RTH-05 | MaxAttempts < terpakai → warning trigger benar (predikat snapshot-presence), simpan tetap jalan (non-blocking) | integration | `dotnet test --filter "FullyQualifiedName~RetakeSettingsEndpointTests"` | ✅ extend RetakeSettingsEndpointTests.cs (warning count pakai helper D-05) |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (pure unit RetakeRules — sub-detik, no SQL).
- **Per wave merge:** `dotnet test` (full incl integration real-SQL — butuh SQLEXPRESS live).
- **Phase gate:** Full suite green + Playwright UAT lifecycle @5270 (window-tutup retake ditolak, reset cabut cert, hapus ber-riwayat) sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `RetakeRulesTests.cs` — tambah param `examWindowCloseDate` ke helper `Can(...)` + case boundary +7h (window-open eligible, window-closed blocked, EWCD null = no gate). Covers RTH-01 eligibility.
- [ ] `RetakeServiceTests.cs` — tambah `SeedSessionAsync` param `examWindowCloseDate` + `nomorSertifikat`; test abort-before-destroy (RTH-01) + cert-null (RTH-02). Extend fixture existing.
- [ ] Helper test untuk `CountEraRetakeArchives` (RTH-03) — unit/integration parity test (cap == warning predikat; legacy tak dihitung). File baru.
- [ ] Participant-remove guard test (RTH-04) — file baru, replikasi loop `removedUserIds` atas real-SQL (pola `RetakeSettingsEndpointTests` "replicate endpoint body"); assert Abandoned/ber-riwayat ditolak tanpa flag, dihapus+0-orphan dengan flag.
- [ ] `RetakeSettingsEndpointTests.cs` — extend untuk warning-count parity (RTH-05) bila helper D-05 mengubah ViewBag count.
- [ ] (Opsional) ekstrak predikat delete-guard ke static pure method (mis. `HasRetakeHistory(session, ...)`) seperti `IsResettable` → testable via pola `ResetGuardTests` (pure, no DB).

*Catatan: RBAC + AntiForgery + PRG = atribut HTTP-layer, diverifikasi via code-grep (pola 405-SUMMARY), BUKAN HTTP integration test — tidak ada WebApplicationFactory di project (parked 999.12).*

## Security Domain

> `security_enforcement` enabled (default). Fase backend hardening lifecycle — relevan untuk integritas data & otorisasi.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tidak menyentuh auth flow |
| V3 Session Management | no (web session) — tapi "assessment session" lifecycle = domain | N/A web; domain lifecycle di-guard server-side |
| V4 Access Control | **yes** | `[Authorize(Roles="Admin, HC")]` pada ResetAssessment/UpdateRetakeSettings (VERIFIED :4284, :5652); worker RetakeExam ownership `Forbid()` IDOR-guard (VERIFIED CMPController:2534); pertahankan saat menambah guard/flag |
| V5 Input Validation | **yes** | Hidden flag `confirmRemoveWithHistory` = server-authoritative (D-06); clamp MaxAttempts/Cooldown server-side existing (`:5667-5668`); window gate server-side (D-01 eksekusi defense-in-depth) |
| V6 Cryptography | no | Tidak ada crypto baru |
| V13 API/Anti-Forgery | **yes** | `[ValidateAntiForgeryToken]` di semua POST (VERIFIED); hidden-flag form re-submit harus tetap ber-`@Html.AntiForgeryToken()` |

### Known Threat Patterns for ASP.NET MVC retake lifecycle

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Worker retake sesi orang lain (IDOR) | Elevation | `assessment.UserId != user.Id → Forbid()` existing (CMPController:2534); jangan regresi |
| Client bypass window/cooldown (JS-only gate) | Tampering | Server-authoritative re-check `CanRetakeAsync` di RetakeExam POST + abort di ExecuteAsync (D-01 eksekusi) — countdown/disable JS BUKAN gate |
| Bypass soft-confirm hapus ber-riwayat via crafted POST | Tampering | D-06 server round-trip + hidden flag dievaluasi SERVER (bukan JS confirm); guard re-cek riwayat tiap POST |
| Dead-end destruktif (retake hapus sesi live lalu StartExam blok) | Denial of Service (self-inflicted) | RTH-01 D-01 dua-lapis: cegah di eligibility (button hide) + abort sebelum RemoveRange |
| Orphan archive leak data lintas attempt | Information Disclosure | D-06 cleanup cascade by AttemptHistoryId; test 0-orphan |
| Cert menggantung di sesi non-lulus | Tampering (integritas) | D-03 nol-kan NomorSertifikat; download PDF sudah di-guard `IsPassed!=true→NotFound` (audit RTK-LOGIC-01) — bukan cert-leak, tapi integritas index |

## Sources

### Primary (HIGH confidence — dibaca baris-per-baris sesi ini)
- `Helpers/RetakeRules.cs` (full) — `CanRetake` signature, guard order, ShouldHideRetakeToggle, ResolveReviewMode
- `Services/RetakeService.cs` (full) — ExecuteAsync (claim :101-112, snapshot :122-171, delete :174-194), CanRetakeAsync (:232-248), snapshot-presence predicate (:145-150, :237-242)
- `Controllers/CMPController.cs:945-1014` — StartExam window enforcement +7h WIB (:956); `:2460-2555` — Results VM retake flags + RetakeExam POST + snapshot-presence (:2472-2475)
- `Controllers/AssessmentAdminController.cs` — ResetAssessment (:4282-4365, delegasi penuh ke service), UpdateRetakeSettings (:5651-5698), ManagePackages warning count (:5795-5798, DIVERGEN), EditAssessment POST signature (:1823-1826) + removedUserIds delete loop + guard (:1916-1971), Standard add-only bulk-assign (:2166-2230), IsResettable predicate (:4280)
- `Models/AssessmentAttemptHistory.cs` + `Models/AssessmentAttemptResponseArchive.cs` (full) — relasi AttemptHistoryId
- `Data/ApplicationDbContext.cs:573-595` — cascade FK config (OnDelete Cascade) + index AttemptHistoryId
- `Models/AssessmentSession.cs:77,91` — ExamWindowCloseDate (DateTime?), NomorSertifikat (string?)
- `Views/Admin/ManagePackages.cshtml:140-199` — retake card + warning consumer (:157-163)
- `Views/CMP/Results.cshtml:485-553` — retake control branches (IsCapReached/CanRetake/IsInCooldown) + confirm modal
- `Views/Admin/AssessmentMonitoringDetail.cshtml:1,325-349` — reset confirm form (:334-341), MonitoringGroupViewModel
- `Views/Admin/EditAssessment.cshtml:593-754` — btn-delete-peserta + deletePesertaForm (dead endpoint) + UserIds/NewUserIds fields
- `Models/AssessmentMonitoringViewModel.cs:53-67` — MonitoringSessionRow exposes IsPassed/NomorSertifikat
- `HcPortal.Tests/{RetakeRulesTests,RetakeServiceTests,RetakeSettingsEndpointTests,ResetGuardTests}.cs` — test conventions, fixtures, seed helpers
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` §F (:142-171 lifecycle/state machine), §5.3 (RTK-LOGIC-01/02/03 :288-321, PA-06 :347-349, VAL-06 :410), field table (:207-219 EWCD vs Cooldown)
- `*.csproj` (grep) — net8.0, EF Core 8.0.0, xUnit 2.9.3, Test SDK 17.13.0
- `Migrations/` (ls) — latest = AddRetakeColumnsAndArchive (Phase 405); konfirmasi migration=FALSE untuk fase ini

### Secondary (MEDIUM confidence)
- CONTEXT.md + UI-SPEC.md (locked decisions D-01..D-07; kontrak UI) — internal spec, di-cross-check ke source

### Tertiary (LOW confidence)
- None — semua klaim di-verifikasi ke source aktual.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versi dari csproj aktual (net8.0, EF 8.0.0, xUnit 2.9.3); tidak ada paket baru.
- Architecture/source mapping: HIGH — setiap titik perubahan (D-01..D-07) di-ground ke baris source yang dibaca; line numbers diverifikasi (catat: warning count audit menyebut :5757 tapi aktual :5795 pasca-Phase 420 — gunakan :5795).
- Blast radius CanRetake: HIGH — grep menyeluruh, 1 pemanggil produksi langsung.
- Pitfalls (window pada tier/cooldown): MEDIUM — perilaku terkonfirmasi dari source, tapi keputusan scope (apakah CONTEXT bermaksud gate penuh) perlu konfirmasi planner/discuss.
- migration=FALSE: HIGH — semua kolom (NomorSertifikat, ExamWindowCloseDate, AttemptHistory/Archive) sudah ada (Phase 405 migration); fase ini logic/guard saja.

**Research date:** 2026-06-23
**Valid until:** 2026-07-23 (stable codebase; valid selama branch ITHandoff tak menerima refactor besar pada RetakeService/RetakeRules/AssessmentAdminController. Jika v32.5 FlexibleParticipantRemove di-merge dari main, re-cek delete-path D-06.)
