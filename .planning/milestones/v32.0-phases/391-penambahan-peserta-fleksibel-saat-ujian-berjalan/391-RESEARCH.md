# Phase 391: Penambahan Peserta Fleksibel saat Ujian Berjalan - Research

**Researched:** 2026-06-17
**Domain:** ASP.NET Core 8 MVC controller logic (EditAssessment POST) + EF Core sibling-session update + xUnit real-SQL integration test
**Confidence:** HIGH (semua temuan diverifikasi langsung dari kode terbaca, bukan asumsi)

## Summary

Phase 391 adalah perubahan **murni logika controller + 1 baris view + 1 file test** di dalam satu file panas: `Controllers/AssessmentAdminController.cs` method `EditAssessment` POST (~L1790-2229). Tidak ada migration, tidak ada model baru, tidak ada library baru. Empat keputusan terkunci (D-01..D-06) memetakan ke titik kode yang persis sudah teridentifikasi di CONTEXT.md, dan setiap titik sudah saya baca langsung. `AssessmentSession` = satu baris per peserta; "tambah peserta" = INSERT sesi baru lewat blok BULK ASSIGN (standard branch, param `NewUserIds`) atau blok Pre-Post (param `UserIds`).

Sumber kebenaran tunggal untuk derive status siap-mulai (Open vs Upcoming) dan cek window ujian adalah `CMPController.StartExam` (L914-957). Pola WIB-nya konsisten dan harus ditiru **verbatim**: `Schedule <= DateTime.UtcNow.AddHours(7)` (Schedule disimpan sebagai WIB-local naive), dan window tutup bila `DateTime.UtcNow.AddHours(7) > ExamWindowCloseDate.Value`. Cabang Pre-Post sudah benar (set sesi baru `Upcoming` + `return` sebelum guard `Completed`), jadi D-01/D-02 di sana sudah lolos; yang perlu di Pre-Post hanya proteksi sesi-berjalan (D-03) bila ada overwrite serupa.

Temuan paling penting yang mengubah strategi D-04: `TempData["Warning"]` saat ini **memang tampil** lewat `_Layout.cshtml` (L190-199) sebagai alert kuning ber-prefix "Warning:" dengan ikon segitiga — kesan-error persis yang user keluhkan. `_Layout` juga sudah render `TempData["Info"]` (L210-219) sebagai alert biru ber-ikon info. Jadi D-04 paling bersih = ganti `TempData["Warning"]` → `TempData["Info"]` dengan wording menenangkan; nol perubahan view diperlukan (layout sudah handle).

**Primary recommendation:** Di standard branch — (1) saring sibling sesi-berjalan SEBELUM loop update field bersama (D-03); (2) ganti guard `Completed` representatif menjadi guard berbasis window agar jalur penambahan lolos (D-02); (3) di BULK ASSIGN ganti `Status = savedAssessment.Status` (L2161) jadi helper `DeriveReadyStatus(schedule, examWindowCloseDate)` yang mengembalikan `Open`/`Upcoming` meniru StartExam (D-01); (4) ganti `TempData["Warning"]` → `TempData["Info"]` informatif (D-04). Kunci semua dengan 4+ xUnit integration fact (D-06) memakai harness `PostLisensorPolishTests.cs`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Tambah peserta = INSERT sesi baru | API/Backend (`AssessmentAdminController.EditAssessment` POST) | DB (AssessmentSessions) | Server-authoritative; status & window dihitung di server, bukan dipercayakan ke form |
| Derive status siap-mulai (Open/Upcoming) | API/Backend (helper baru, mirror `CMPController.StartExam`) | — | Status harus dihitung dari Schedule vs now-WIB di server; klien tak boleh menentukan |
| Cek window ujian (boleh-tambah) | API/Backend (`EditAssessment` guard) | — | Otorisasi operasi = backend; mirror cek window `StartExam` L953 |
| Proteksi sesi berjalan (timer/integritas) | API/Backend (filter sibling pre-loop) | DB | Integritas ujian peserta berjalan = invariant server-side |
| Notice informatif | Frontend Server (Razor `_Layout.cshtml` TempData render) | API/Backend (set `TempData["Info"]`) | Notice = flash message via TempData; rendering sudah ada di layout |
| Regression lock | Test tier (xUnit + real SQL disposable) | — | Mengunci keputusan controller di level data |

## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01 (PART-01):** Peserta baru **TIDAK mewarisi status induk** (`Status = savedAssessment.Status` di L2161 diganti). Status di-set **siap-mulai berdasarkan jadwal**: `Open` jika window ujian sudah buka (Schedule sudah tiba & `ExamWindowCloseDate` belum lewat), `Upcoming` jika jadwal belum tiba. Konsisten dengan cabang Pre-Post yang sudah set `Upcoming`.
  > ⚠️ **Ini menggantikan kalimat ROADMAP "mewarisi status induk" yang sudah usang.** Riset wajib ikut D-01.
- **D-02 (PART-02):** HC boleh menambah peserta **selama window ujian belum tutup** — `ExamWindowCloseDate` belum lewat (jika null, fallback ke jadwal + durasi). Guard `Completed` (L1992) **tidak boleh memblokir** operasi penambahan walau sesi representatif/sebagian sudah `Completed`. Cek berbasis **window**, bukan status satu sesi representatif. (Guard `Completed` untuk EDIT murni tanpa penambahan boleh tetap.)
- **D-03 (PART-01c/PART-04):** Saat Edit+Tambah, **JANGAN overwrite** `Status`/`Schedule`/`DurationMinutes` pada sesi **sedang berjalan** (`StartedAt != null && CompletedAt == null`). Hanya sesi belum-mulai yang ikut update field bersama. Perbaiki teks warning L2082-2084 yang menyesatkan. Default aman: sesi berjalan jangan ubah field volatil apa pun.
- **D-04 (PART-03):** Ganti `TempData["Warning"]` kosmetik (L2082) jadi **info non-blocking** di ManageAssessment: pesan netral, gaya info/success bukan warning kesan-error. Tanpa konfirmasi/friksi.
- **D-05:** Surface utama = `EditAssessment` POST standar. Cabang **Pre-Post** (L1806-1988) diperiksa konsistensi (sudah set `Upcoming` + `return` sebelum guard); terapkan D-03/D-04 bila relevan. `CreateAssessment` di luar fokus.
- **D-06 (PART-04):** Regression test xUnit **integration real-SQL** (disposable `HcPortalDB_Test_{guid}`, `[Trait Category=Integration]`, pola `PostLisensorPolishTests.cs`) mengunci 4 perilaku.

### Claude's Discretion

- Penentuan persis `Open` vs `Upcoming` (bandingkan `Schedule`/`ExamWindowCloseDate` vs now; ikuti `StartExam` L914-924 / L952-957).
- Penempatan cek window (helper vs inline) + perlakuan `ExamWindowCloseDate == null` (fallback jadwal+durasi).
- Bentuk proteksi sesi-berjalan (filter sibling sebelum loop vs guard per-field).
- Field non-volatil (AllowAnswerReview/GenerateCertificate/PassPercentage/Token) untuk sesi berjalan — default aman = jangan ubah apa pun.
- Notice pakai `TempData["Info"]` baru atau gabung ke `Success`; wording final.

### Deferred Ideas (OUT OF SCOPE)

- Dialog konfirmasi opsional sebelum tambah ke ujian live (friksi) — user pilih tanpa friksi.
- Bulk import peserta-ke-assessment via Excel.
- Hard-block penambahan saat InProgress (bertentangan keputusan fleksibel).
- Perubahan controller/model CreateWorker (Phase 392), AD-sync, migration.
- Cleanup data test Phase 367 (housekeeping terpisah, tidak di-fold).

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PART-01 | Tambah peserta saat ada `InProgress` berhasil; sesi baru siap-mulai (Open/Upcoming per jadwal, BUKAN warisi induk) | BULK ASSIGN L2155-2174, `Status = savedAssessment.Status` L2161 → ganti helper `DeriveReadyStatus`. Pola Open/Upcoming dari `StartExam` L914-924. Pre-Post sudah benar (L1940/L1961 set "Upcoming") |
| PART-02 | Guard `Completed` tak salah-blokir selama window terbuka | Guard `if (assessment.Status == "Completed")` L1992 → ganti dengan cek window saat ada penambahan; pola window `StartExam` L953. Pre-Post `return` L1987 sebelum guard = sudah aman |
| PART-03 | Notice informatif ganti warning kosmetik | `TempData["Warning"]` L2082-2084 → `TempData["Info"]`. `_Layout.cshtml` L210-219 sudah render `Info` (alert biru) |
| PART-04 | Regression test mengunci 4 perilaku | Harness `PostLisensorPolishTests.cs` (disposable real-SQL, `[Trait Category=Integration]`). Filter sesi-berjalan via group-key sibling query (pola L2050-2054 / L2078-2081) |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller `EditAssessment` POST | Stack project; tak ada perubahan |
| EF Core (SqlServer) | 8.0.0 | Query/update `AssessmentSession` | ORM existing; `Where` group-key sibling + `AddRange` |
| xUnit | 2.9.3 | Regression test PART-04 | Framework test existing (HcPortal.Tests) |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Disposable real-SQL test | Harness `PostLisensorPolishTests` pakai ini |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner | Existing |

[VERIFIED: HcPortal.Tests/HcPortal.Tests.csproj] — versi paket dibaca langsung dari csproj.

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | (tersedia) test in-memory | TIDAK dipakai untuk PART-04 — harus real-SQL (D-06 disposable DB) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Real-SQL disposable DB | EF InMemory provider | DITOLAK oleh D-06: InMemory tak menangkap perilaku SQL Server (TRIM, datetime, query translation). Pola project = real-SQL `[Trait Integration]` |
| Helper method `DeriveReadyStatus` | Inline ternary di L2161 | Helper lebih testable + reusable di Pre-Post bila perlu; tapi inline juga sah (Discretion). Rekomendasi: helper privat static agar test bisa panggil langsung tanpa instantiate controller |

**Installation:** Tidak ada paket baru. `dotnet build` + `dotnet test` cukup.

**Version verification:** Tidak ada paket NuGet baru ditambahkan; semua versi sudah pinned di csproj existing [VERIFIED: csproj terbaca].

## Architecture Patterns

### System Architecture Diagram

```
HC submit form Edit Assessment (ManageAssessment → EditAssessment POST)
        │  params: id, model (AssessmentSession), NewUserIds[] (standard) / UserIds[] (Pre-Post)
        ▼
┌─────────────────────────────────────────────────────────────┐
│ EditAssessment POST (AssessmentAdminController L1794)         │
│                                                              │
│  FindAsync(id) → assessment (sesi representatif)             │
│        │                                                     │
│        ├──► IS Pre-Post + LinkedGroupId? ──► YES ──┐         │
│        │                                           ▼         │
│        │                          Pre-Post branch (L1806-1988)│
│        │                          • shared-field update semua │
│        │                          • new Pre+Post sesi="Upcoming"│  ◄── D-01 sudah benar
│        │                          • return SEBELUM guard       │  ◄── D-02 sudah aman
│        │                          ▲ D-03: terapkan filter      │
│        │                            sesi-berjalan bila overwrite│
│        │                          ▼ RETURN ManageAssessment    │
│        │                                                     │
│        ▼ NO (standard branch)                                │
│  ┌──────────────────────────────────────────────┐           │
│  │ GUARD Completed (L1992) ── target D-02         │           │
│  │   saat ada penambahan → cek WINDOW, bukan status│          │  ◄── D-02
│  └──────────────────────────────────────────────┘           │
│        ▼                                                     │
│  validasi field (Title/Schedule/Duration/Pass/Token)        │
│        ▼                                                     │
│  fetch siblings (Title+Category+Schedule.Date) L2050-2054   │
│        ▼                                                     │
│  ┌──────────────────────────────────────────────┐           │
│  │ LOOP update shared field SEMUA sibling L2058   │           │
│  │   ── target D-03: SKIP sesi berjalan            │          │  ◄── D-03
│  │      (StartedAt!=null && CompletedAt==null)     │          │
│  └──────────────────────────────────────────────┘           │
│        ▼                                                     │
│  hasInProgress? → TempData["Info"] (bukan Warning) L2082     │  ◄── D-04
│        ▼                                                     │
│  SaveChanges + audit "EditAssessment"                        │
│        ▼                                                     │
│  ┌──────────────────────────────────────────────┐           │
│  │ BULK ASSIGN (L2114-2226) — NewUserIds          │           │
│  │   filter sudah-assign (idempotent L2134)        │          │
│  │   new sesi: Status = DeriveReadyStatus(...)     │          │  ◄── D-01 (ganti L2161)
│  │   notif + audit "BulkAssign"                    │          │
│  └──────────────────────────────────────────────┘           │
│        ▼ RETURN ManageAssessment                            │
└─────────────────────────────────────────────────────────────┘
        ▼
_Layout.cshtml render TempData["Info"]/["Success"] (alert biru/hijau)
        ▼
Peserta baru muncul Open/Upcoming → StartExam normal (status lolos time-gate)
```

### Recommended Project Structure
Tidak ada struktur baru. File tersentuh:
```
Controllers/AssessmentAdminController.cs   # EditAssessment POST (D-01..D-04)
HcPortal.Tests/FlexibleParticipantAddTests.cs  # BARU (D-06) — atau extend pola PostLisensorPolishTests
(opsional) Views/Admin/EditAssessment.cshtml   # teks hint UI bila perlu (UI hint: yes)
```
Catatan: `_Layout.cshtml` SUDAH render `Info` — tidak perlu diubah untuk D-04.

### Pattern 1: Derive status siap-mulai (Open vs Upcoming) — mirror StartExam
**What:** Hitung status sesi baru dari Schedule vs now-WIB + window-close, persis seperti StartExam.
**When to use:** Saat INSERT sesi baru di BULK ASSIGN (D-01).
**Example:**
```csharp
// Source: mirror Controllers/CMPController.cs StartExam L915 + L953 (WIB = UTC+7)
// Schedule disimpan sebagai WIB-local naive (lihat State of the Art).
private static string DeriveReadyStatus(DateTime schedule, DateTime? examWindowCloseDate)
{
    var nowWib = DateTime.UtcNow.AddHours(7);
    // Window sudah lewat? (fallback null → tidak blokir di sini; cek window dilakukan di guard D-02)
    // Jadwal sudah tiba → Open; belum → Upcoming. (mirror StartExam L915: Schedule <= nowWib → Open)
    if (schedule <= nowWib)
        return AssessmentConstants.AssessmentStatus.Open;       // "Open"
    return AssessmentConstants.AssessmentStatus.Upcoming;       // "Upcoming"
}
```
> Pakai `AssessmentConstants.AssessmentStatus.Open/.Upcoming` — JANGAN hardcode string [VERIFIED: AssessmentConstants.cs L15-16].

### Pattern 2: Cek window boleh-tambah (guard D-02)
**What:** Saat ada penambahan peserta (`NewUserIds.Any()`), guard `Completed` representatif jangan blokir; ganti dengan cek window grup.
**When to use:** Sebelum jalur penambahan (D-02).
**Example:**
```csharp
// Source: mirror StartExam L953 — window tutup bila now-WIB > ExamWindowCloseDate
bool hasAddition = NewUserIds != null && NewUserIds.Count > 0;
var nowWib = DateTime.UtcNow.AddHours(7);

// ExamWindowCloseDate null → fallback ke Schedule + DurationMinutes (Discretion)
DateTime effectiveClose = assessment.ExamWindowCloseDate
    ?? assessment.Schedule.AddMinutes(assessment.DurationMinutes > 0 ? assessment.DurationMinutes : 0);

bool windowOpen = nowWib <= effectiveClose;

// Guard Completed: blokir HANYA untuk EDIT murni (tanpa penambahan).
// Untuk penambahan, izinkan selama window terbuka.
if (assessment.Status == AssessmentConstants.AssessmentStatus.Completed && !hasAddition)
{
    TempData["Error"] = "Cannot edit completed assessments.";
    return RedirectToAction("ManageAssessment");
}
// (opsional) bila ingin tegas: penambahan saat window tutup → tolak ramah.
```
> ⚠️ Verifikasi planner: pastikan fallback `ExamWindowCloseDate == null` tidak menolak penambahan saat user memang tak set window (mayoritas kasus). Default aman = bila null, anggap window terbuka (jangan blokir) ATAU fallback jadwal+durasi. CONTEXT D-02 = "jika null, fallback ke jadwal + durasi". Rekomendasi: gunakan fallback tapi pastikan tidak menolak kasus normal (sesi baru ditambah di hari-H setelah jadwal tapi sebelum durasi habis).

### Pattern 3: Skip sesi berjalan di loop shared-field (D-03)
**What:** Loop update field bersama (L2058-2075) hanya untuk sesi belum-mulai.
**When to use:** Standard branch shared-field update.
**Example:**
```csharp
// Source: filter pada koleksi siblings yang sudah di-fetch L2050-2054
foreach (var sibling in siblings)
{
    // D-03: sesi sedang berjalan = jangan sentuh field volatil (lindungi timer & integritas)
    bool isRunning = sibling.StartedAt != null && sibling.CompletedAt == null;
    if (isRunning) continue;   // skip total (default aman — Discretion)

    sibling.Title = model.Title;
    sibling.Category = model.Category;
    sibling.Schedule = model.Schedule;
    sibling.DurationMinutes = model.DurationMinutes;
    sibling.Status = model.Status;
    // ... field lain ...
    sibling.UpdatedAt = now;
}
```
> Pola group-key sibling sudah dipakai di L2050-2054 (fetch) & L2078-2081 (`hasInProgress` AnyAsync) — reuse predikat yang sama.

### Pattern 4: Notice informatif (D-04)
**What:** Ganti `TempData["Warning"]` jadi `TempData["Info"]` dengan wording menenangkan.
**Example:**
```csharp
// Source: ganti L2082-2084. _Layout.cshtml L210-219 sudah render Info (alert biru).
if (hasInProgress)
{
    TempData["Info"] = "Ada peserta yang sedang mengerjakan ujian. " +
        "Peserta baru tetap dapat ditambahkan dan langsung mulai selama waktu ujian masih terbuka. " +
        "Sesi peserta yang sedang berjalan tidak terpengaruh perubahan.";
}
```
> Wording lama menyesatkan: "Perubahan ... tidak akan berlaku untuk sesi yang sedang berjalan" — padahal kode LAMA justru menimpa. Setelah D-03, kalimat itu jadi benar. Tapi D-04 minta nada informatif/menenangkan, bukan peringatan.

### Anti-Patterns to Avoid
- **Hardcode string status** (`"Open"`, `"Upcoming"`) — pakai `AssessmentConstants.AssessmentStatus.*` (single-source, lihat [v22.0 decision STATE.md]).
- **Pakai `DateTime.Now`/`DateTime.UtcNow` polos** untuk compare Schedule — Schedule = WIB-local; harus `DateTime.UtcNow.AddHours(7)` (lihat Pitfall 1).
- **Hapus guard `Completed` total** — OUT OF SCOPE. D-02 = jangan salah-blokir penambahan, BUKAN buang guard untuk EDIT murni.
- **Mengandalkan form/klien menentukan status sesi baru** — status harus dihitung server-side.
- **Mengubah `TempData["Success"]` untuk notice** — Success di-render DUA KALI di ManageAssessment (layout L220 + page L33). `Info` render sekali. Pakai `Info` untuk hindari duplikat & beri warna netral.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Konversi WIB / cek window | Util timezone baru | Mirror `StartExam` L915/L953 (`DateTime.UtcNow.AddHours(7)`) | Pola sudah ter-uji produksi; util baru = drift risk |
| Filter sibling grup | Query group-key baru | Reuse predikat `Title==&&Category==&&Schedule.Date==` (L2050-2054, L2078-2081, L2125-2131) | Konsisten; mencegah split-brain definisi "satu assessment" |
| Konstanta status | String literal | `AssessmentConstants.AssessmentStatus.*` | Single-source lintas 11+ surface |
| Notice rendering | Partial/alert baru di view | `TempData["Info"]` (layout L210-219 sudah render) | Zero view change; konsisten dgn flash pattern |
| Test harness disposable DB | Setup DB baru | Copy `PostLisensorPolishFixture` (IAsyncLifetime MigrateAsync→EnsureDeletedAsync) | Pola terbukti; isolasi dari `HcPortalDB_Dev` |

**Key insight:** Hampir semua yang dibutuhkan sudah ada di kode. Phase ini = **wire ulang + filter**, bukan bangun baru. Bahaya terbesar = menyimpang dari pola WIB/group-key yang sudah benar.

## Common Pitfalls

### Pitfall 1: UTC vs WIB local date (regresi historis d844c552)
**What goes wrong:** Menghitung Open/Upcoming atau window pakai `DateTime.UtcNow` polos atau `DateTime.Now` → sesi baru salah status (Upcoming padahal harusnya Open, atau ditolak window padahal masih buka), terutama antara tengah-malam-WIB s/d rollover-UTC.
**Why it happens:** `Schedule` & `ExamWindowCloseDate` disimpan sebagai **WIB-local naive DateTime** (CreateAssessment validasi `model.Schedule < DateTime.Today` LOCAL; StartExam compare `Schedule <= DateTime.UtcNow.AddHours(7)`). Bug d844c552 persis ini: kode test pakai UTC date, server pakai LOCAL → flake harian di WIB.
**How to avoid:** Untuk SEMUA perbandingan Schedule/ExamWindowCloseDate gunakan `DateTime.UtcNow.AddHours(7)` (now-WIB), mirror StartExam L915 & L953. JANGAN `DateTime.Now` (tergantung TZ mesin server), JANGAN `DateTime.UtcNow` polos.
**Warning signs:** Sesi baru ditambah hari-H tapi statusnya Upcoming (tak bisa StartExam); atau test integration flake tergantung jam dijalankan.
[VERIFIED: git show d844c552] + [VERIFIED: CMPController.cs L915/L953] + [VERIFIED: AssessmentAdminController.cs L933 `model.Schedule < DateTime.Today`].

### Pitfall 2: Edit-loop berjalan SEBELUM BULK ASSIGN dan menimpa SEMUA sibling
**What goes wrong:** Loop L2058-2075 menimpa `Status`/`Schedule`/`DurationMinutes` pada SEMUA sibling termasuk sesi berjalan — merusak timer & integritas ujian. Bila tak difilter, D-03 gagal dan test (c) gagal.
**Why it happens:** Urutan eksekusi: validasi → fetch siblings → **loop overwrite semua** → SaveChanges → BULK ASSIGN. Loop tidak punya guard sesi-berjalan.
**How to avoid:** Tambah `if (sibling.StartedAt != null && sibling.CompletedAt == null) continue;` di awal loop body (D-03). Default aman = skip total (jangan ubah field apa pun).
**Warning signs:** Peserta yang sedang ujian tiba-tiba timer-nya reset / status berubah saat HC menambah peserta lain.
[VERIFIED: AssessmentAdminController.cs L2056-2075].

### Pitfall 3: Guard `Completed` memakai sesi representatif by-id, bukan status grup
**What goes wrong:** `assessment = FindAsync(id)` adalah SATU sesi (yang di-klik HC). Bila sesi itu kebetulan `Completed` (mis. peserta itu sudah selesai), guard L1992 blokir SELURUH operasi — termasuk penambahan peserta baru — padahal grup masih aktif.
**Why it happens:** Guard cek `assessment.Status == "Completed"` pada satu sesi, bukan window grup.
**How to avoid:** Saat ada penambahan (`NewUserIds.Any()`), lewati guard berbasis-status; gunakan cek window grup (Pattern 2). Pertahankan guard hanya untuk EDIT murni tanpa penambahan (D-02).
**Warning signs:** "Cannot edit completed assessments" muncul saat HC coba tambah peserta ke ujian yang sebagian peserta sudah selesai.
[VERIFIED: AssessmentAdminController.cs L1992].

### Pitfall 4: BULK ASSIGN mewarisi `Status = savedAssessment.Status` (L2161)
**What goes wrong:** Sesi baru lahir dengan status sesi representatif (bisa `InProgress`/`Completed`) → muncul "InProgress" padahal `Progress=0`, monitoring salah, dan StartExam time-gate bisa salah.
**Why it happens:** L2161 menyalin status apa adanya dari `savedAssessment`.
**How to avoid:** Ganti `Status = savedAssessment.Status` → `Status = DeriveReadyStatus(savedAssessment.Schedule, savedAssessment.ExamWindowCloseDate)` (D-01).
**Warning signs:** Peserta baru muncul InProgress di monitoring tanpa pernah masuk ujian.
[VERIFIED: AssessmentAdminController.cs L2161].

### Pitfall 5: `TempData["Success"]` double-render di ManageAssessment
**What goes wrong:** Bila notice ditaruh di `Success`, ManageAssessment menampilkannya DUA KALI (layout L220 + page L33-39).
**Why it happens:** ManageAssessment punya blok Success/Error sendiri DAN dirender di dalam `_Layout` yang juga punya blok Success/Error.
**How to avoid:** Pakai `TempData["Info"]` untuk notice (hanya dirender layout L210-219, sekali, warna netral biru).
**Warning signs:** Pesan sukses muncul dobel di halaman.
[VERIFIED: ManageAssessment.cshtml L33-46] + [VERIFIED: _Layout.cshtml L190-228].

### Pitfall 6: Pre-Post branch return SEBELUM guard — jangan duplikasi guard di sana
**What goes wrong:** Menambah guard `Completed`/window ke Pre-Post yang sebenarnya sudah `return` (L1987) sebelum guard standar — over-engineering.
**Why it happens:** D-05 minta "periksa konsistensi", bisa salah-tafsir jadi "tambah guard".
**How to avoid:** Pre-Post sudah: set sesi baru `Upcoming` (L1940/L1961) + `return` (L1987). D-01/D-02 di sana SUDAH aman. Yang perlu hanya: pertimbangkan D-03 (Pre-Post shared-field loop L1832-1844 & per-phase L1847-1866 juga menimpa semua sesi — apakah ada sesi berjalan? Pre/Post umumnya belum mulai, tapi konsisten = terapkan filter sesi-berjalan bila relevan) + D-04 notice bila ada InProgress.
**Warning signs:** Logika ganda / regресi Pre-Post.
[VERIFIED: AssessmentAdminController.cs L1806-1988].

## Code Examples

### Verifikasi pola window/status (source of truth)
```csharp
// Source: Controllers/CMPController.cs StartExam (VERIFIED L914-957)
// Auto Upcoming → Open saat jadwal tiba (WIB):
if (assessment.Status == "Upcoming" && assessment.Schedule <= DateTime.UtcNow.AddHours(7)) { /* → Open */ }
// Window tutup:
if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value) { /* ditutup */ }
```

### Harness integration test (D-06) — adaptasi PostLisensorPolishTests
```csharp
// Source: HcPortal.Tests/PostLisensorPolishTests.cs (VERIFIED L25-94)
public class FlexibleAddFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    public DbContextOptions<ApplicationDbContext> Options { get; private set; } = null!;
    public FlexibleAddFixture() =>
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    public async Task InitializeAsync()
    {
        Options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.MigrateAsync();   // disposable DB; HcPortalDB_Dev TAK disentuh
    }
    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class FlexibleAddTests : IClassFixture<FlexibleAddFixture>
{
    // Replikasi keputusan controller PERSIS di level data (pola PostLisensorPolish):
    // - DeriveReadyStatus(schedule, window) byte-identik helper controller
    // - filter sesi-berjalan: StartedAt!=null && CompletedAt==null → skip
    // - cek window: now-WIB <= effectiveClose
    // 4 fact mengunci: (a) add saat InProgress → sesi baru tercipta; (b) sesi baru Open/Upcoming bukan InProgress;
    // (c) sesi InProgress existing Status/Schedule/Duration UNCHANGED; (d) add tak terblokir saat sebagian Completed + window terbuka.
}
```
> Catatan harness: logika guard hidup di controller yang berat di-instantiate (DI penuh). Pola project = **replikasi keputusan di level data** (bukan WebApplicationFactory). 4 fact harus byte-identik dengan urutan keputusan controller agar mengunci perilaku nyata.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| ROADMAP: sesi baru "mewarisi status induk" | CONTEXT D-01: sesi baru siap-mulai (Open/Upcoming derive) | 2026-06-17 (discuss-phase) | **D-01 menang.** REQUIREMENTS.md PART-01/PART-04 masih bertuliskan "mewarisi status induk" — itu usang; ikuti CONTEXT |
| `TempData["Warning"]` (alert kuning "Warning:", kesan-error) | `TempData["Info"]` (alert biru, informatif) | Phase 391 D-04 | Notice menenangkan; layout sudah render Info |
| Loop overwrite SEMUA sibling | Skip sesi berjalan (StartedAt!=null && CompletedAt==null) | Phase 391 D-03 | Lindungi timer/integritas |

**Deprecated/outdated:**
- Kalimat REQUIREMENTS.md PART-01 "peserta baru mewarisi status induk" & PART-04 "(b) peserta baru mewarisi status induk" — **superseded oleh CONTEXT D-01**. Planner harus map ke "siap-mulai (Open/Upcoming)".
- Teks warning L2082-2084 ("Perubahan ... tidak akan berlaku untuk sesi berjalan") — menyesatkan SEBELUM D-03 (kode lama justru menimpa); benar SETELAH D-03 tapi nada salah → diganti D-04.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `ExamWindowCloseDate == null` → anggap window terbuka (jangan blokir penambahan); fallback jadwal+durasi hanya bila ingin tegas | Pattern 2 / D-02 Discretion | Bila salah, kasus normal (window tak di-set) bisa salah-blokir penambahan. Mayoritas assessment tak set ExamWindowCloseDate → default-terbuka lebih aman. Planner konfirmasi: D-02 bilang "fallback jadwal+durasi" — verifikasi tak menolak kasus normal hari-H |
| A2 | Sesi Pre/Post di branch Pre-Post umumnya belum-mulai saat tambah peserta, jadi D-03 di Pre-Post low-risk; tetap terapkan filter untuk konsistensi | Pitfall 6 | Bila ada Pre/Post yang sudah InProgress saat HC tambah peserta + edit, loop L1832/L1847 bisa menimpa. Risiko rendah tapi nyata; terapkan filter sama |

> Catatan: A1 & A2 = area Discretion (sudah di-grant CONTEXT), bukan keputusan terkunci yang dilanggar. Keduanya diturunkan dari kode terbaca, bukan training data.

## Open Questions

1. **Fallback `ExamWindowCloseDate == null` — terbuka selamanya vs jadwal+durasi?**
   - What we know: D-02 bilang "jika null, fallback ke jadwal + durasi". StartExam hanya cek window bila `ExamWindowCloseDate.HasValue` (L953) — jadi tanpa window, StartExam TIDAK pernah menolak by-window.
   - What's unclear: Apakah penambahan harus ikut "fallback jadwal+durasi" (tegas) atau "null = selalu boleh tambah" (longgar, sejajar StartExam)?
   - Recommendation: Default LONGGAR (null → boleh tambah) untuk sejajar perilaku StartExam yang tak menolak tanpa window — paling konsisten dengan "fleksibel tanpa friksi". Bila planner pilih tegas (fallback jadwal+durasi), pastikan threshold = `Schedule + DurationMinutes` agar penambahan hari-H sebelum durasi habis tetap lolos. **Putuskan saat planning; kunci di test (d).**

2. **Apakah perlu ubah teks hint di EditAssessment.cshtml (UI hint: yes)?**
   - What we know: STATE.md menandai "UI hint: yes". EditAssessment.cshtml punya komentar "schedule-change warning verified" (L25).
   - What's unclear: Apakah ada teks statis di form yang menyebut perilaku lama (mis. "tidak bisa tambah saat berjalan").
   - Recommendation: Grep EditAssessment.cshtml saat planning untuk teks usang; ubah hanya bila ada (D-04 fokus = TempData, view-text sekunder). Jangan tambah Playwright wajib kecuali ada perubahan render dinamis.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build + test | ✓ | 8.0.418 | — |
| SQL Server (localhost\SQLEXPRESS) | PART-04 integration test (disposable DB) | ✓ (Dev DB pakai instance sama) | — | Tanpa SQLEXPRESS, test Integration skip; fast suite tetap jalan |
| Playwright (e2e) | Opsional verif notice UX | ✓ (project punya tests/e2e) | — | Manual browser verify localhost:5277 |

**Missing dependencies with no fallback:** Tidak ada.
**Missing dependencies with fallback:** Tidak ada yang missing — semua tersedia.
[VERIFIED: `dotnet --version` = 8.0.418] + [VERIFIED: appsettings.Development.json connection `localhost\SQLEXPRESS;Database=HcPortalDB_Dev`].

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (HcPortal.Tests) |
| Config file | HcPortal.Tests/HcPortal.Tests.csproj (net8.0) |
| Quick run command | `dotnet test --filter "Category!=Integration"` (fast suite — Integration di-exclude) |
| Full suite command | `dotnet test` (termasuk Integration real-SQL; butuh SQLEXPRESS up) |
| Integration-only | `dotnet test --filter "Category=Integration"` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PART-01 | Tambah saat ada InProgress → sesi baru tercipta | integration | `dotnet test --filter "Category=Integration"` | ❌ Wave 0 (file baru) |
| PART-01 | Sesi baru ber-status siap-mulai (Open/Upcoming, BUKAN InProgress) | integration | idem | ❌ Wave 0 |
| PART-02 | Penambahan tak terblokir saat sebagian Completed + window terbuka | integration | idem | ❌ Wave 0 |
| PART-03 | Notice = Info (bukan Warning) — pesan informatif | manual / opsional e2e | browser localhost:5277 ATAU assert TempData via unit | ❌ Wave 0 (opsional) |
| PART-04 (c) | Sesi InProgress existing Status/Schedule/Duration UNCHANGED | integration | `dotnet test --filter "Category=Integration"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet build` 0 error + `dotnet test --filter "Category!=Integration"` (fast suite hijau).
- **Per wave merge:** `dotnet test` penuh (termasuk Integration real-SQL) — pastikan SQLEXPRESS up; `HcPortalDB_Dev` TIDAK tersentuh (disposable `HcPortalDB_Test_{guid}`).
- **Phase gate:** Full suite hijau + (opsional) browser verify notice Info di ManageAssessment SEBELUM `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/FlexibleParticipantAddTests.cs` — fixture disposable + 4 fact (a/b/c/d) mengunci PART-01/02/04. Pola: copy `PostLisensorPolishFixture` (IAsyncLifetime MigrateAsync→EnsureDeletedAsync, `[Trait Category=Integration]`).
- [ ] Helper `DeriveReadyStatus(schedule, window)` harus dapat dipanggil dari test (buat `private static` di controller + replikasi byte-identik di test, ATAU `internal static` + `InternalsVisibleTo` — pola project = replikasi di test, bukan expose).
- [ ] (Opsional) `tests/e2e/flexible-add-notice-391.spec.ts` — assert alert Info muncul di ManageAssessment setelah tambah peserta saat ada InProgress. Hanya bila notice butuh runtime verify; default = manual browser cukup (notice = TempData statis, bukan render dinamis kompleks → grep+build+manual cukup, beda dari kasus aria Phase 354).

*Catatan: tidak ada framework install dibutuhkan — xUnit + SqlServer provider sudah ada di csproj.*

## Sources

### Primary (HIGH confidence)
- `Controllers/AssessmentAdminController.cs` L1790-2229 — EditAssessment POST (Pre-Post branch L1806-1988, guard Completed L1992, edit-loop L2056-2075, warning L2077-2085, BULK ASSIGN L2114-2226, Status inherit L2161) — dibaca langsung.
- `Controllers/CMPController.cs` L899-1024 — StartExam window/status logic (auto Upcoming→Open L915, window-close L953, set InProgress L1001) — dibaca langsung.
- `Models/AssessmentConstants.cs` L13-22 — AssessmentStatus constants — dibaca langsung.
- `Models/AssessmentSession.cs` — field Status/Schedule/DurationMinutes/StartedAt/CompletedAt/ExamWindowCloseDate/Progress — dibaca langsung.
- `HcPortal.Tests/PostLisensorPolishTests.cs` L1-388 — pola disposable real-SQL integration test — dibaca langsung.
- `Views/Shared/_Layout.cshtml` L189-228 — render TempData Warning/Error/Info/Success — dibaca langsung.
- `Views/Admin/ManageAssessment.cshtml` L1-60 — own Success/Error block + _Layout assignment — dibaca langsung.
- `HcPortal.Tests/HcPortal.Tests.csproj` — versi paket xUnit 2.9.3 / EF 8.0.0 / Test.Sdk 17.13.0 — dibaca langsung.
- `appsettings.Development.json` L10 — connection localhost\SQLEXPRESS;HcPortalDB_Dev — dibaca langsung.
- `git show d844c552` — bukti regresi UTC vs WIB-local date — dibaca langsung.

### Secondary (MEDIUM confidence)
- `.planning/STATE.md` — keputusan lintas-milestone (AssessmentStatus single-source v22.0; pola test Phase 387 D-09).
- `Views/Admin/EditAssessment.cshtml` L25/L629 — fieldName NewUserIds vs UserIds, schedule-change warning comment.

### Tertiary (LOW confidence)
- Tidak ada — semua klaim diverifikasi via kode/tool.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tidak ada paket baru; semua versi dibaca dari csproj.
- Architecture (titik insert D-01..D-04): HIGH — setiap baris target dibaca langsung & nomor baris cocok dengan CONTEXT.
- Pitfalls: HIGH — UTC/WIB (git show d844c552 + StartExam), edit-loop order, double-render Success semua diverifikasi.
- Test harness: HIGH — pola PostLisensorPolishTests dibaca lengkap; connection string & SDK terverifikasi.
- Open Question A1 (fallback window null): MEDIUM — area Discretion; rekomendasi diberikan, putusan final saat planning.

**Research date:** 2026-06-17
**Valid until:** 2026-07-17 (stabil — kode internal, tak ada dependency eksternal fast-moving). Re-verify nomor baris bila `AssessmentAdminController.cs` diedit sebelum planning.
