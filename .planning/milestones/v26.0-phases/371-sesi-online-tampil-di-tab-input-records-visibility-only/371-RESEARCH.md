# Phase 371: Sesi Online Tampil di Tab Input Records (visibility-only) - Research

**Researched:** 2026-06-11
**Domain:** ASP.NET Core 8 Razor view (HTMX partial) — extend anon-type LINQ projection di `_TrainingRecordsTab.cshtml`
**Confidence:** HIGH (semua target diverifikasi langsung di kode + DB lokal; zero unknown tersisa)

## Summary

Phase 371 adalah perubahan **view-only di satu file** (`Views/Admin/Shared/_TrainingRecordsTab.cshtml`, region :262-380). Service `WorkerDataService.GetWorkersInSection` SUDAH load semua `AssessmentSessions` per worker tanpa filter `IsManualEntry`/status `[VERIFIED: WorkerDataService.cs:288-297]` — yang perlu diubah hanya proyeksi anon-type di view (:265-267) yang saat ini memfilter `.Where(a => a.IsManualEntry)`. Longgarkan filter → online sessions ikut tampil, dengan badge "Assessment Online", derivasi status 6-way pola `DeriveUserStatus`, dan tombol "Lihat hasil" untuk sesi Completed/Menunggu Penilaian. TANPA aksi hapus/edit online (placeholder Phase 367).

Tiga unknown utama TERPECAHKAN: **(1) Route hasil = `CMP/Results?id={sessionId}`** — accepts `AssessmentSession.Id` (= `row.Id`), grant akses Admin(roleLevel 1)/HC(roleLevel 2) via `IsResultsAuthorized`, gate `IsAssessmentSubmitted` (Completed OR PendingGrading) yang persis sama dengan kondisi tombol D-02 `[VERIFIED: CMPController.cs:2218,2546,2233]`. **(2) `DeriveUserStatus` static** bisa dipanggil dari Razor TAPI butuh `@using HcPortal.Controllers` (saat ini `_ViewImports.cshtml` hanya import `HcPortal` + `HcPortal.Models`, BUKAN `.Controllers`) `[VERIFIED: _ViewImports.cshtml:1-4]`. **(3) Anon-type Concat pitfall (lesson Phase 354)** — `trainingRows` dan `assessmentRows` di-`.Concat()` di :268; menambah baris online butuh shape anon IDENTIK (urutan + tipe property sama persis) atau Concat gagal compile. Rekomendasi: bangun **satu `.Select` gabungan** atau tambah field umum (`IsOnline`, `RawStatus`, `StartedAt`, `CompletedAt`, `OnlineId`) ke KETIGA proyeksi.

**Primary recommendation:** Ubah hanya `_TrainingRecordsTab.cshtml`. Tambah proyeksi `onlineRows` dari `worker.AssessmentSessions.Where(a => !a.IsManualEntry)`, samakan shape anon dengan training/manual rows (tambah field `IsOnline`, `RawStatus`, `StartedAt`, `CompletedAt`, `OnlineSessionId`), derivasi label via inline switch yang meniru `DeriveUserStatus` (lebih bersih daripada `@using HcPortal.Controllers` di Razor karena view butuh juga mapping warna badge + label Lulus/Tidak Lulus yang TIDAK ada di `DeriveUserStatus`), dan render tombol "Lihat hasil" → `Url.Action("Results", "CMP", new { id = row.OnlineSessionId })` HANYA saat status Completed/Menunggu Penilaian.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Filter `IsManualEntry` dilonggarkan, render online rows | Frontend Server (Razor view) | — | Data sudah tersedia di ViewModel; murni presentasi `[VERIFIED: WorkerDataService.cs:288-297 load semua sessions]` |
| Derivasi label status online (6-way + warna) | Frontend Server (Razor inline switch) | API (`DeriveUserStatus` static, opsional dipanggil) | View butuh label+warna+Lulus/Tidak; `DeriveUserStatus` hanya hasilkan string status mentah, tak ada warna/IsPassed |
| Tombol "Lihat hasil" → halaman hasil exam | API/Backend (`CMP/Results`) | — | Route EKSISTING; view hanya bikin anchor `Url.Action` |
| Counter rekap worker (D-06, JANGAN disentuh) | API/Backend (service) | — | Sudah include online via `passedAssessmentLookup` tanpa filter `IsManualEntry` `[VERIFIED: WorkerDataService.cs:308-313]` |
| Aksi hapus online (DEFERRED Phase 367) | — | — | Out of scope; extension point di branch online kolom Aksi |

## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** SEMUA status sesi online tampil — Open ("Belum Mulai"), InProgress ("Sedang Dikerjakan"), Abandoned, Cancelled ("Dibatalkan"), Completed, Menunggu Penilaian — masing-masing dengan badge status.
- **D-02:** Tombol **Lihat hasil** (ikon mata, link ke halaman hasil exam) HANYA untuk sesi Completed/Menunggu Penilaian; sesi belum selesai TANPA tombol. TANPA aksi hapus/edit untuk semua row online. Route hasil = research/planner verifikasi route admin eksisting — JANGAN bikin halaman baru.
- **D-03:** Derivasi lengkap pola `DeriveUserStatus`: Lulus (hijau) / Tidak Lulus (merah) / Menunggu Penilaian (kuning) / Belum Mulai (abu) / Sedang Dikerjakan (biru) / Dibatalkan & Abandoned (abu gelap). Mapping existing IsPassed-only TIDAK dipakai untuk online. Row manual existing TIDAK berubah mapping-nya.
- **D-04:** Empty-state copy jadi **"Belum ada record untuk pekerja ini."** (drop kata "manual"). Tombol Tambah tetap.
- **D-05:** Sesi belum selesai: kolom Tanggal = `Schedule` tanpa prefix; kolom Detail = "—". Badge status sudah cukup.
- **D-06:** Counter rekap worker (`CompletedAssessments`/`CompletionDisplayText`) TIDAK disentuh — service JANGAN diubah.
- **D-07:** Pasangan Pre-Post online (LinkedGroupId) tampil 2 row terpisah (per session) — tanpa grouping.

### Claude's Discretion
- Wording title tombol Lihat hasil + ikon persis.
- Posisi badge "Assessment Online" mengikuti pola badge tipe existing (kolom Tipe).
- Urutan sort tetap `OrderByDescending(Date)` gabungan (existing).

### Deferred Ideas (OUT OF SCOPE)
- Tombol hapus sesi online → **Phase 367** (delete cascade engine + preview).
- Grouping visual pasangan Pre-Post di expand worker → fase UX terpisah bila dibutuhkan HC.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| URG-03 | Sesi assessment online (IsManualEntry=false) tampil di tab Input Records per worker dengan badge pembeda "Assessment Online" — visibility-only, aksi hapus tetap scope Phase 367 | Filter :266 dilonggarkan; service sudah load online `[VERIFIED: WorkerDataService.cs:288-297]`; route hasil `CMP/Results` `[VERIFIED: CMPController.cs:2218]`; derivasi `DeriveUserStatus` `[VERIFIED: AssessmentAdminController.cs:2799-2812]`; UAT data tersedia (55 online sessions, 52 >7hari, Rino GAST) `[VERIFIED: DB lokal HcPortalDB_Dev]` |

## Standard Stack

Tidak ada dependency baru. Semua yang dibutuhkan sudah ada di codebase.

| Komponen | Sumber | Purpose | Status |
|----------|--------|---------|--------|
| Razor view + HTMX partial | `_TrainingRecordsTab.cshtml` | Render Tab Input Records | Existing — file target tunggal |
| `DeriveUserStatus(status, completedAt, startedAt)` static | `AssessmentAdminController.cs:2799` | Pola derivasi status 6-way (referensi logika) | Existing — pure static, testable |
| `CMP/Results?id={int}` route | `CMPController.cs:2218` | Halaman hasil exam per-session | Existing — Admin/HC accessible |
| `AssessmentConstants.AssessmentStatus.PendingGrading` | `AssessmentConstants.cs:18` | Konstanta "Menunggu Penilaian" | Existing — sudah dipakai di view :267,329 |
| `Bootstrap 5 + Bootstrap Icons` | `_Layout` | Badge + ikon mata (`bi-eye`) | Existing |

**Installation:** `(tidak ada — zero package change, Migration=false)`

## Architecture Patterns

### System Architecture Diagram (data flow Tab Input Records)

```
[Admin pilih filter Bagian/Unit/Kategori/Status/Search]
        │ (HTMX hx-get)
        ▼
GET /Admin/ManageAssessmentTab_Training  [Authorize(Admin,HC)]
   (AssessmentAdminController)
        │  panggil
        ▼
WorkerDataService.GetWorkersInSection(section, ...)
   ── load TrainingRecords per user (date-filter)
   ── load AssessmentSessions per user  ◄── SEMUA sessions, TANPA filter IsManualEntry/status
        │  ViewBag.Workers = List<WorkerTrainingStatus>
        ▼
_TrainingRecordsTab.cshtml  (partial)
   foreach worker → expand region (:262-380)
        │
        ├─ trainingRows  = worker.TrainingRecords.Select(anon)         (:263-264)
        ├─ manualRows    = worker.AssessmentSessions.Where(IsManualEntry).Select(anon)   (:265-267)  ◄ EXISTING
        ├─ onlineRows    = worker.AssessmentSessions.Where(!IsManualEntry).Select(anon)  ◄ TAMBAH (Phase 371)
        │       └─ derivasi status pola DeriveUserStatus + warna badge + IsPassed→Lulus/Tidak
        └─ allRows = Concat(trainingRows, manualRows, onlineRows).OrderByDescending(Date)
                │  per row:
                ├─ badge Tipe: Training Manual / Assessment Manual / Assessment Online
                ├─ badge Status: warna per derivasi
                └─ kolom Aksi:
                     ├─ Training/Manual → Edit + Hapus (HTMX, existing)
                     └─ Online → [Lihat hasil → CMP/Results?id] HANYA Completed/PendingGrading
                                  (NO Edit, NO Hapus — placeholder Phase 367)
```

### Pattern 1: Satu `.Select` gabungan ATAU shape anon identik untuk `.Concat()`
**What:** Saat ini `allRows = trainingRows.Concat(assessmentRows)` di :268. Anon-type `.Concat()` di C# HANYA kompatibel kalau **nama, tipe, dan URUTAN** property identik di semua operand. Menambah `onlineRows` dengan shape berbeda → CS error.
**When to use:** Selalu, untuk Phase 371.
**Current anon shape (:264 training):**
```csharp
new { Type = "Training", Date = r.Tanggal, Title = r.Judul,
      Detail = r.Penyelenggara ?? "—", Status = r.Status ?? "",
      ValidUntil = r.ValidUntil, Id = r.Id, Score = (int?)null,
      IsPassed = (bool?)(r.Status == "Passed" || r.Status == "Valid") }
```
**Recommended new shape (semua 3 proyeksi pakai shape ini):**
```csharp
// Source: pola existing :264-267 + DeriveUserStatus :2799 + lesson Phase 354 RuntimeBinderException
new {
    Type        = "Training" | "AssessmentManual" | "AssessmentOnline", // string penanda
    Date        = ...,                 // DateTime — Tanggal | (CompletedAt ?? Schedule)
    Title       = ...,                 // string
    Detail      = ...,                 // string (— untuk online belum selesai, D-05)
    Status      = ...,                 // string label final (sudah-derived)
    StatusClass = ...,                 // string Bootstrap class (warna badge)
    ValidUntil  = (DateOnly?)...,      // sesuaikan tipe — CATATAN: training ValidUntil bertipe DateOnly? (lihat Pitfall 2)
    Id          = ...,                 // int (training/manual record id; online = session id)
    IsOnline    = true|false,          // bool — branch kolom Aksi
    CanViewResult = true|false         // bool — true hanya online Completed/PendingGrading
}
```
**Catatan:** lebih aman bangun derivasi status+warna+CanViewResult di blok `@{ }` SEBELUM `.Select` (variabel lokal per item lewat method/helper inline), lalu proyeksikan hasil string final — menghindari logic kompleks di dalam ekspresi anon.

### Pattern 2: Inline switch derivasi status online (D-03) — RECOMMENDED over calling `DeriveUserStatus`
**What:** D-03 minta label + warna + Lulus/Tidak Lulus. `DeriveUserStatus` hanya hasilkan string status mentah (`"Completed"`, `"Menunggu Penilaian"`, `"InProgress"`, `"Dibatalkan"`, `"Abandoned"`, `"Not started"`) — TIDAK membedakan Lulus vs Tidak Lulus (keduanya `"Completed"`) dan TIDAK punya mapping warna. View butuh keduanya.
**Recommended derivation (Razor inline, meniru `DeriveUserStatus` + tambah IsPassed branch):**
```razor
@* Source: meniru AssessmentAdminController.DeriveUserStatus:2799-2812 + D-03 mapping *@
@{
    string OnlineLabel(HcPortal.Models.AssessmentSession a)
    {
        if (a.Status == AssessmentConstants.AssessmentStatus.PendingGrading) return "Menunggu Penilaian";
        if (a.CompletedAt != null) return a.IsPassed == true ? "Lulus" : "Tidak Lulus";
        if (a.Status == "Cancelled")  return "Dibatalkan";
        if (a.Status == "Abandoned")  return "Abandoned";
        if (a.StartedAt != null)      return "Sedang Dikerjakan";
        return "Belum Mulai";  // Open + Upcoming jatuh ke sini
    }
    string OnlineClass(string label) => label switch
    {
        "Lulus"              => "bg-success",       // hijau
        "Tidak Lulus"        => "bg-danger",        // merah
        "Menunggu Penilaian" => "bg-warning text-dark", // kuning
        "Sedang Dikerjakan"  => "bg-primary",       // biru
        "Dibatalkan"         => "bg-dark",          // abu gelap
        "Abandoned"          => "bg-dark",          // abu gelap
        _                    => "bg-secondary"      // "Belum Mulai" abu
    };
}
```
**When to use:** Phase 371 derivasi label online. Jika planner pilih panggil `DeriveUserStatus` langsung, WAJIB tambah `@using HcPortal.Controllers` di view ATAU `_ViewImports.cshtml` — tapi tetap perlu lapis IsPassed→Lulus/Tidak + warna di atasnya, jadi inline lebih ringkas.

### Pattern 3: Tombol "Lihat hasil" (D-02)
```razor
@* Source: pola anchor existing :286 + route CMP/Results:2218 (terima id = session id) *@
@if (row.IsOnline && row.CanViewResult)
{
    <a href="@Url.Action("Results", "CMP", new { id = row.Id })"
       class="btn btn-sm btn-outline-info" title="Lihat hasil"
       target="_blank">
        <i class="bi bi-eye"></i>
    </a>
}
@* online belum-selesai: kolom Aksi kosong (no tombol). NO Edit/Hapus untuk semua online. *@
```

### Anti-Patterns to Avoid
- **Reuse `GetUnifiedRecords` untuk Tab ini:** JANGAN. `GetUnifiedRecords` (`WorkerDataService.cs:28`) hanya load sesi `Completed`/`PendingGrading` `[VERIFIED: WorkerDataService.cs:33]` — D-01 minta SEMUA status (Open/InProgress/Abandoned/Cancelled). Surface berbeda (`/CMP/Records` RecordsWorkerDetail), requirement berbeda. Bangun proyeksi inline dari `worker.AssessmentSessions`.
- **Mengubah service (`WorkerDataService`):** DILARANG (D-06). View-only murni. Service sudah load semua sessions.
- **`@model dynamic`/anon mismatch Concat:** Pitfall utama (lihat Pitfall 1). Lesson Phase 354: Razor dynamic + anon shape mismatch = compile/runtime error yang grep+build kadang tak deteksi sampai render.
- **Pakai mapping `IsPassed`-only untuk online belum-selesai:** salah-label (D-03). Sesi InProgress `IsPassed==null` → jangan map ke "Tidak Lulus".

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Halaman hasil exam online | Halaman/partial baru | `CMP/Results?id={sessionId}` | Sudah ada, Admin/HC authorized, gate submitted `[VERIFIED: CMPController.cs:2218-2237]` |
| Derivasi status 6-way | Switch ad-hoc dari nol | Pola `DeriveUserStatus:2799` (tiru urutan cek: PendingGrading→Completed→Cancelled→Abandoned→StartedAt→else) | Urutan cek penting: PendingGrading punya CompletedAt terisi, salah urut = salah label |
| Konstanta "Menunggu Penilaian" | Literal string | `AssessmentConstants.AssessmentStatus.PendingGrading` | Sudah dipakai view :267,329; anti drift |

**Key insight:** Phase 371 = recombination, bukan creation. Semua building block (route, derivasi, konstanta, data) sudah ada; tugas planner adalah menyusun proyeksi anon yang konsisten + render branch online.

## Runtime State Inventory

> Bukan fase rename/refactor/migration murni, tapi mengubah visibility data existing. Inventory ringkas:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | 55 AssessmentSessions online di DB lokal (Completed 26, InProgress 12, Open 9, Upcoming 6, Cancelled 2). 52 dari 55 >7 hari. `[VERIFIED: DB HcPortalDB_Dev]` | None — view-only, tak mengubah data |
| Live service config | None — tak ada konfigurasi eksternal | None |
| OS-registered state | None | None |
| Secrets/env vars | None | None |
| Build artifacts | None — single `.cshtml` edit, Razor recompile otomatis | None |

**Migration:** FALSE (dikonfirmasi CONTEXT + tak ada perubahan schema).

## Common Pitfalls

### Pitfall 1: Anonymous-type `.Concat()` shape mismatch (CS compile error)
**What goes wrong:** Menambah `onlineRows` ke `trainingRows.Concat(...)` dengan property berbeda nama/tipe/urutan → C# anggap tipe anon berbeda → `.Concat()` gagal compile (CS1929/CS0411) atau `OrderByDescending` ambigu.
**Why it happens:** Anon types structurally typed; HARUS identik persis untuk dianggap tipe sama.
**How to avoid:** Definisikan SATU bentuk anon (atau buat class kecil `record InputRecordRow`) dan pakai di KETIGA proyeksi. Perhatikan `ValidUntil` (lihat Pitfall 2) dan `Id` (int konsisten).
**Warning signs:** `dotnet build` error CS pada baris `.Concat(` atau `.OrderByDescending(`. `[VERIFIED: anon shape current :264-267]`

### Pitfall 2: `ValidUntil` tipe campur — `DateOnly?` (training/manual) vs sesi online tak punya `ValidUntil` relevan
**What goes wrong:** `TrainingRecord.ValidUntil` dan `AssessmentSession.ValidUntil` keduanya `DateOnly?` `[VERIFIED: AssessmentSession.cs:66]`. Existing training anon proyeksikan `ValidUntil = r.ValidUntil` (DateOnly?) — TAPI manual assessment row juga `a.ValidUntil` (DateOnly?). Konsisten. Untuk online, ValidUntil biasanya null (sertifikat belum terbit) — proyeksikan `(DateOnly?)null` atau `a.ValidUntil` apa adanya. JANGAN campur `DateTime?` dengan `DateOnly?`.
**How to avoid:** Pastikan semua proyeksi `ValidUntil` bertipe `DateOnly?`. Kolom "Berlaku Sampai" render `.Value.ToString("dd MMM yyyy")` aman untuk DateOnly.
**Warning signs:** CS error tipe pada property `ValidUntil` saat Concat.

### Pitfall 3: Status `"Upcoming"` tidak ada di enumerasi D-01
**What goes wrong:** DB punya 6 sesi online berstatus `"Upcoming"` (tak terstart, tak complete) `[VERIFIED: DB query — Id 104/105/123/125/160/161 noStart/noComplete]`. D-01 enumerasi Open/InProgress/Abandoned/Cancelled/Completed/Menunggu — tidak sebut Upcoming.
**Why it happens:** `Status` enum sejarah punya "Upcoming" (`AssessmentSession.cs:20` komentar). `DeriveUserStatus` tak handle eksplisit → fallthrough ke "Not started"/"Belum Mulai".
**How to avoid:** Derivasi inline (Pattern 2) sudah benar: Upcoming jatuh ke else → "Belum Mulai" (abu), konsisten dengan Open. Tidak perlu branch khusus. Catat ini ke planner agar UAT cek sesi Upcoming muncul sebagai "Belum Mulai".
**Warning signs:** Sesi Upcoming hilang atau label aneh saat UAT.

### Pitfall 4: Lesson Phase 354 — Razor dynamic/anon butuh runtime verify
**What goes wrong:** `dotnet build` + grep BISA lolos tapi runtime `RuntimeBinderException` muncul saat render (akses property absen di anon). Phase 354 kena ini.
**How to avoid:** Selain `dotnet build`, WAJIB UAT Playwright @5277 render aktual expand worker (D-04 SC4). Jangan andalkan build saja untuk Razor view changes.
**Warning signs:** HTTP 500 saat expand worker; stack `Microsoft.CSharp.RuntimeBinder`.

### Pitfall 5: Empty-state ganda — copy lama "manual" di DUA tempat
**What goes wrong:** Copy "Belum ada record" muncul di :273 (per-worker expand, hardcode "manual") DAN tombol Tambah. D-04 minta drop "manual" → "Belum ada record untuk pekerja ini." TAPI ada juga empty-state level-list di :201 ("Tidak ada pekerja ditemukan") — JANGAN sentuh yang itu (beda konteks).
**How to avoid:** Ubah HANYA :273 (`Belum ada record manual untuk pekerja ini.` → `Belum ada record untuk pekerja ini.`). Tombol Tambah Training/Assessment tetap (D-04).
**Warning signs:** Copy "manual" masih muncul setelah fix; atau salah ubah empty-state level-list.

## Code Examples

### Proyeksi gabungan 3-sumber (rekomendasi struktur)
```razor
@* Source: extend _TrainingRecordsTab.cshtml:262-268 + DeriveUserStatus pattern *@
@{
    var trainingRows = (worker.TrainingRecords ?? new List<HcPortal.Models.TrainingRecord>())
        .Select(r => new {
            Type = "Training", Date = r.Tanggal, Title = r.Judul,
            Detail = r.Penyelenggara ?? "—",
            StatusLabel = r.Status ?? "", StatusClass = TrainingClass(r.Status),
            ValidUntil = r.ValidUntil, Id = r.Id, IsOnline = false, CanViewResult = false });

    var manualRows = (worker.AssessmentSessions ?? new List<HcPortal.Models.AssessmentSession>())
        .Where(a => a.IsManualEntry)
        .Select(a => new {
            Type = "AssessmentManual", Date = a.CompletedAt ?? a.Schedule, Title = a.Title,
            Detail = a.Score.HasValue ? $"{a.Score} - {(a.IsPassed == true ? "Lulus" : a.IsPassed == false ? "Tidak Lulus" : AssessmentConstants.AssessmentStatus.PendingGrading)}" : "—",
            StatusLabel = a.IsPassed == true ? "Passed" : a.IsPassed == false ? "Failed" : AssessmentConstants.AssessmentStatus.PendingGrading,
            StatusClass = ManualClass(a.IsPassed),
            ValidUntil = a.ValidUntil, Id = a.Id, IsOnline = false, CanViewResult = false });

    var onlineRows = (worker.AssessmentSessions ?? new List<HcPortal.Models.AssessmentSession>())
        .Where(a => !a.IsManualEntry)
        .Select(a => {
            var label = OnlineLabel(a);
            bool done = a.Status == AssessmentConstants.AssessmentStatus.PendingGrading || a.CompletedAt != null;
            return new {
                Type = "AssessmentOnline",
                Date = a.CompletedAt ?? a.Schedule, Title = a.Title,
                Detail = (a.CompletedAt != null && a.Score.HasValue) ? $"{a.Score}" : "—", // D-05: belum selesai = —
                StatusLabel = label, StatusClass = OnlineClass(label),
                ValidUntil = a.ValidUntil, Id = a.Id, IsOnline = true, CanViewResult = done };
        });

    var allRows = trainingRows.Concat(manualRows).Concat(onlineRows)
                              .OrderByDescending(r => r.Date).ToList();
}
```
*(Catatan planner: `TrainingClass`/`ManualClass`/`OnlineLabel`/`OnlineClass` = local function di blok `@functions` atau `@{ }`. Pastikan SEMUA anon shape identik — `StatusLabel`/`StatusClass`/`IsOnline`/`CanViewResult` ditambah ke semua. `Score`/`IsPassed` lama yang tak dipakai bisa di-drop bila tak ada konsumen lain.)*

### Badge Tipe 3-way (extend :309-318)
```razor
@if (row.Type == "Training")        { <span class="badge bg-success">Training Manual</span> }
else if (row.Type == "AssessmentManual") { <span class="badge bg-info text-white">Assessment Manual</span> }
else                                { <span class="badge bg-secondary">Assessment Online</span> }
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Filter `.Where(a => a.IsManualEntry)` sembunyikan online di Tab Input Records | Longgarkan → online tampil read-only + badge | Phase 371 (sekarang) | Admin lihat sesi stale untuk persiapan hapus Phase 367 |
| `GetUnifiedRecords` (Completed/Pending only) di `/CMP/Records` | Tab Input Records bangun proyeksi sendiri (semua status) | — | Dua surface, requirement beda; jangan disatukan |

**Deprecated/outdated:** Tidak ada.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| — | (kosong) | — | — |

**Semua klaim diverifikasi via kode/DB/registry — tidak ada `[ASSUMED]`.** Satu-satunya item butuh konfirmasi planner = pilihan implementasi (inline switch vs `@using HcPortal.Controllers`), yang sudah ditandai Claude's Discretion di CONTEXT.

## Open Questions

1. **Inline switch derivasi vs panggil `DeriveUserStatus` static langsung dari Razor**
   - What we know: Keduanya bisa. `DeriveUserStatus` butuh `@using HcPortal.Controllers` (belum di `_ViewImports`). View tetap butuh lapisan IsPassed→Lulus/Tidak + warna di atas output `DeriveUserStatus`.
   - What's unclear: Preferensi tim untuk DRY (panggil static) vs lokalitas (inline).
   - Recommendation: Inline switch (Pattern 2) — lebih ringkas karena butuh label+warna+Lulus/Tidak yang `DeriveUserStatus` tak sediakan; hindari coupling view→Controllers namespace. CONTEXT D-03 catatan: "boleh panggil static ini langsung dari Razor atau duplikasi inline — planner putuskan."

2. **Apakah refactor anon → record `InputRecordRow` worth it**
   - What we know: 3 proyeksi anon identik rentan drift. Class kecil lebih aman + testable.
   - Recommendation: Opsional. Untuk view-only single-file, anon konsisten cukup; tapi record memudahkan unit test bila planner mau tambah test logic-bearing.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build + test | ✓ | 8.0.418 (net8.0) | — |
| SQL Server Express (HcPortalDB_Dev) | UAT lokal | ✓ | localhost\SQLEXPRESS | — |
| sqlcmd | UAT read-only check | ✓ | (tersedia) | — |
| Playwright (e2e @5277) | UAT SC4 | ✓ (tests/e2e ada) | — | manual browser |

**Missing dependencies:** None.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (net8.0) + Playwright (e2e TypeScript) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (ProjectReference ke HcPortal); `tests/e2e/` |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| Full suite command | `dotnet build` + `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (suite ~226-231) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| URG-03 (SC1) | Online sessions (semua status, termasuk >7hari) tampil di expand worker dengan badge "Assessment Online" | manual/e2e | UAT Playwright @5277 (expand worker GAST → row online) | ❌ Wave 0 (opsional e2e) / ✅ manual |
| URG-03 (SC2) | Manual rows render tak berubah; edit/hapus manual tetap; online TANPA hapus | manual/e2e | UAT Playwright @5277 | ✅ manual |
| URG-03 (SC2b) | Derivasi label 6-way benar (Lulus/Tidak/Pending/Belum/Sedang/Dibatalkan) | unit (opsional) | `dotnet test` bila derivasi diekstrak ke helper static testable | ❌ Wave 0 (jika helper extract) |
| URG-03 (SC3) | Empty-state copy "Belum ada record untuk pekerja ini." | manual | UAT @5277 (worker tanpa record) | ✅ manual |
| URG-03 (SC4) | `dotnet build` 0 error + full suite hijau + UAT render OK | smoke | `dotnet build && dotnet test ...` | ✅ existing |

### Sampling Rate
- **Per task commit:** `dotnet build` (Razor view → WAJIB build, lesson Phase 354) + `dotnet test HcPortal.Tests/HcPortal.Tests.csproj`.
- **Per wave merge:** Full suite hijau (~226).
- **Phase gate:** Build 0 error + suite hijau + UAT Playwright @5277 (expand worker GAST, render row online, tombol Lihat hasil Completed) SEBELUM `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] (Opsional) Jika derivasi label online diekstrak ke helper static (mis. `DeriveOnlineRowLabel`) → tambah xUnit di `HcPortal.Tests/` mirip `MonitoringUserStatusTests.cs` (6-cabang). Tanpa ekstrak, derivasi inline view tak unit-testable → andalkan UAT.
- [ ] (Opsional) e2e spec baru `tests/e2e/input-records-online.spec.ts` — expand worker → assert badge "Assessment Online" muncul + tombol Lihat hasil hanya Completed. Tidak ada e2e existing yang assert isi row Tab Input Records (yang ada hanya assert XHR/filter di `manage-assessment-filter.spec.ts` dan `/CMP/Records` di `Phase324_NoDuplicateTrainingRecord.spec.ts` — DUA-duanya surface berbeda, TIDAK akan regresi).
- *(Jika derivasi tetap inline & cukup UAT manual: "None — UAT Playwright + dotnet build/test existing menutup SC.")*

**Test impact existing:** `WorkerDataServiceSearchTests.cs:135` sudah assert `RecordType == "Assessment Online"` (untuk `GetUnifiedRecords`, surface /CMP/Records — TIDAK terdampak Phase 371). `TrainingInitialStateTests.cs` test `IsTrainingInitialState` (filter-state, tak terdampak). Tidak ada test yang assert komposisi row `_TrainingRecordsTab` → menambah online rows tidak memecah suite existing. `[VERIFIED: grep HcPortal.Tests]`

## Security Domain

> `security_enforcement` tidak diset eksplisit `false` → diperlakukan enabled. Phase 371 = view-only read, risiko minimal.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Endpoint `[Authorize(Admin,HC)]` existing tak berubah |
| V4 Access Control | yes | Tombol "Lihat hasil" → `CMP/Results` punya `IsResultsAuthorized` (roleLevel ≤3 full) `[VERIFIED: CMPController.cs:2546]` — admin/HC sudah authorized; tak ada IDOR baru (route existing, gate submitted) |
| V5 Input Validation | yes (minimal) | `Url.Action("Results", new {id})` — id integer dari data terpercaya (session worker dalam scope filter). Tak ada input user baru |
| V6 Cryptography | no | Tak ada |

### Known Threat Patterns for {ASP.NET Core Razor view-only}

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| IDOR via Results link (lihat hasil worker lain) | Elevation of Privilege | `CMP/Results` enforce `IsResultsAuthorized` server-side (Admin/HC lolos by design) `[VERIFIED: CMPController.cs:2229]` — link client hanya kenyamanan, otorisasi tetap di server |
| XSS via Title/Detail render | Tampering | Razor auto-encode `@row.Title` (default HTML-encode) — tak ada `Html.Raw` pada data online baru |
| Expose sesi online belum-selesai | Information Disclosure | Diizinkan by design (D-01) — admin/HC scope; tombol hasil hanya Completed/Pending (gate `IsAssessmentSubmitted`) |

## Sources

### Primary (HIGH confidence)
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml:1-467` — file target, anon proyeksi :263-268, badge :309-318, status switch :322-333, aksi :338-369, empty-state :273
- `Controllers/AssessmentAdminController.cs:2799-2812` — `DeriveUserStatus` static (6-way derivasi, urutan PendingGrading→Completed→Cancelled→Abandoned→StartedAt→else)
- `Controllers/CMPController.cs:2218-2237` — `Results(int id)` (terima session id, gate `IsAssessmentSubmitted`); `:2546-2555` `IsResultsAuthorized` (roleLevel 1-3 full); `:24` class `[Authorize]`
- `Services/WorkerDataService.cs:288-297,308-313` — bukti load semua sessions tanpa filter IsManualEntry; `:28-88` `GetUnifiedRecords` (Completed/Pending only — surface berbeda)
- `Models/AssessmentSession.cs:18-20,38-40,66,131` — Status/IsPassed/CompletedAt/StartedAt/ValidUntil(DateOnly?)/IsManualEntry
- `Models/AssessmentConstants.cs:13-21` — AssessmentStatus konstanta (PendingGrading = "Menunggu Penilaian")
- `Views/_ViewImports.cshtml:1-4` — import HcPortal + HcPortal.Models (BUKAN .Controllers)
- DB lokal `HcPortalDB_Dev` (sqlcmd, read-only) — 55 online sessions; status dist; 52 >7hari; Rino NIP 29007720 GAST punya old completed online (Lulus/Gagal/2025)

### Secondary (MEDIUM confidence)
- `HcPortal.Tests/WorkerDataServiceSearchTests.cs:135` — `RecordType=="Assessment Online"` (GetUnifiedRecords, tak terdampak)
- `tests/e2e/manage-assessment-filter.spec.ts`, `Phase324_NoDuplicateTrainingRecord.spec.ts` — surface berbeda, tak regresi

### Tertiary (LOW confidence)
- (tidak ada — semua diverifikasi langsung)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — zero dependency baru, semua komponen diverifikasi di kode
- Architecture (route hasil + derivasi + Concat): HIGH — route/static/shape diverifikasi file:line
- Pitfalls: HIGH — Concat shape + ValidUntil tipe + Upcoming status + lesson 354 semua kode/DB-verified
- UAT data: HIGH — query langsung DB lokal, Rino GAST + 52 sesi >7hari ada

**Research date:** 2026-06-11
**Valid until:** 2026-07-11 (stabil — view-only, kode internal; re-cek hanya jika `CMP/Results` signature atau `_ViewImports` berubah)
