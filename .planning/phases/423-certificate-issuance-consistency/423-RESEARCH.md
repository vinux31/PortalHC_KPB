# Phase 423: Certificate Issuance Consistency - Research

**Researched:** 2026-06-24
**Domain:** ASP.NET Core MVC + EF Core (SQL Server) — certificate-issuance domain rules, concurrency/atomicity, pure-helper extraction (carry-forward pola 422)
**Confidence:** HIGH (semua klaim diverifikasi terhadap kode current di branch ITHandoff via Grep/Read; tidak ada dependensi eksternal baru)

> Catatan provenance: semua tag `[VERIFIED: file:line]` di bawah dikonfirmasi dengan membaca kode aktual sesi ini. `[CITED: audit]` = dari `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md`. Tidak ada klaim `[ASSUMED]` material (lihat Assumptions Log).

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** `ShouldIssueCertificate` jadi **SATU gate kelayakan cert untuk SEMUA jalur**: `GradingService.GradeAndCompleteAsync` + (re-grade) + jalur manual `AddManualAssessment` + jalur essay-finalize. Manual **berhenti hardcode** `GenerateCertificate=true`. Helper konsisten menolak `AssessmentType == "PreTest"` di semua kasus. (Saat ini hanya sebagian jalur cek PreTest — divergensi ditutup.)
- **D-02 (namespace):** Nomor cert manual tetap **free-text** tapi **divalidasi TIDAK boleh menyerupai format auto** `KPB/{seq}/{ROMAN}/{YEAR}`; insert manual dibungkus `try/catch DbUpdateException` (pakai `CertNumberHelper.IsDuplicateKeyException`) → **pesan error ramah** saat kolisi (bukan 500). TIDAK mengubah tampilan nomor cert yang sudah tercetak.
- **D-03 (seq atomik):** Harden generator seq (`CertNumberHelper.GetNextSeqAsync` MAX+1 race-prone) — perbanyak retry + jitter di atas pola existing (retry-3x + filtered `WHERE NomorSertifikat == null` + unique index `IX_AssessmentSessions_NomorSertifikat_Unique`). **Tanpa tabel SEQUENCE baru** (migration=FALSE). Saat seq tetap gagal: **sesi tetap selesai TANPA cert (non-destruktif) + ditandai agar HC bisa terbitkan/retry manual**.
- **D-04:** Cert non-Pre wajib tangani ValidUntil eksplisit. `CertificateType` "Permanent" → **tolak ValidUntil** (null); "Annual" → **+1 tahun**; "3-Year" → **+3 tahun**.
- **D-05 (tanggal dasar):** ValidUntil diturunkan dari **`CompletedAt`**, bukan tanggal terbit/hari-ini.
- **D-06 (retroaktif):** **Hanya berlaku ke depan** — baris lama mismatch TIDAK disentuh.
- **D-07 (anti double-cert):** Guard server-side cegah dua cert AKTIF untuk (peserta, judul). **"aktif" = `ValidUntil == null` ATAU `ValidUntil >= hari ini`**. **Pengecualian renewal**: `RenewsSessionId` terisi → lolos. **Tidak bisa di-bypass** via `ConfirmDuplicateTitle`.
- **D-08 (tampil di):** Umur sesi "Menunggu Penilaian" di **halaman EssayGrading DAN daftar (Monitoring Detail / ManageAssessment)**. **TANPA auto-finalize**.
- **D-09 (ambang):** **>3 hari = kuning, >7 hari = merah**. Teks umur + badge warna.

### Claude's Discretion
- Penempatan persis & nama kelas helper (kemungkinan `Helpers/CertIssuanceRules.cs` analog `SessionEditLockRules`).
- Bentuk penanda "cert gagal terbit utk HC" (kolom flag existing vs TempData/log + tampilan) — selama non-destruktif & terlihat HC.
- Jumlah retry & strategi jitter konkret (selama lebih tahan burst dari retry-3x).
- Format teks umur PendingGrading.

### Deferred Ideas (OUT OF SCOPE)
- Cert/analytics atribusi per-unit akurat (kolom unit-at-issue + backfill) — backlog v2, butuh migration ke-2. **TIDAK masuk 423.**
- Sinkronisasi `DeriveCertificateStatus` agar konsumen membaca Annual/3-Year — bila tak terselesaikan oleh CERT-06, masuk Phase 425/backlog.
- Dedupe scoring & gating Pre→Post (Phase 424), penamaan/dead-field cosmetic (Phase 425).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CERT-01 | Satu helper bersama, tolak Pre-Test di SEMUA jalur grading-time [GRD-01, FLD-5.2-10] | 4 cert-issue site dipetakan (lihat tabel di bawah); helper `ShouldIssueCertificate(session)` pure-static menyatukan gate. |
| CERT-02 | Cert non-Pre wajib ValidUntil eksplisit [GRD-06] | `DeriveValidUntil(certType, completedAt)` pure; gate tolak terbit tanpa masa berlaku tak sengaja; sumber tanggal `CompletedAt`. |
| CERT-03 | Seq atomik tanpa race [GRD-08] | Harden `GetNextSeqAsync` + loop retry: tambah retry-count, jitter (`Task.Delay` random), di bawah filtered unique index existing. Sinyal "cert gagal terbit utk HC". |
| CERT-04 | Manual vs auto tak bentrok, error ramah, namespace dipisah [FLD-5.2-07, FLD-5.2-02] | `ResemblesAutoCertFormat(nomor)` pure regex; try/catch `DbUpdateException` di `AddManualAssessment` SaveChanges; `GenerateCertificate` berhenti hardcode. |
| CERT-05 | Guard anti double-cert aktif, tak bisa di-bypass ConfirmDuplicateTitle [VAL-04, GRD-10] | Query EF `HasActiveCertForTitleAsync(userId, normalizedTitle, excludeSessionId)`; guard di luar cabang `ConfirmDuplicateTitle`; dipasang di create-issue paths. |
| CERT-06 | CertificateType×ValidUntil konsisten [FLD-5.2-09] | `DeriveValidUntil` + validasi server Permanent⊥ValidUntil; forward-only (D-06). |
| CERT-07 | Sesi "Menunggu Penilaian" tampilkan umur [GRD-05] | `CompletedAt` = "waiting since" (di-set bersamaan saat status→PendingGrading); pure `PendingAgeBadge(completedAt, now)`; render di 2 view. |
</phase_requirements>

---

## Summary

Fase 423 adalah **refactor konsolidasi murni**: tidak ada schema baru (migration=FALSE), tidak ada library baru. Seluruh perubahan menyatukan aturan kelayakan & penomoran sertifikat yang saat ini tersebar (dan divergen) di **4 jalur penerbitan** ke **satu helper pure** plus pengerasan loop retry yang sudah ada. Pola yang diikuti persis = Phase 422: ekstrak keputusan inline ke kelas `static` di `Helpers/` (analog `SessionEditLockRules`), uji dengan pure-unit-test truth-table + 1 integration real-SQL `IClassFixture<RetakeServiceFixture>` (recipe dari `RetakeThenPassCertTests`).

Temuan kunci: **ada 4 cert-issue site, bukan 2-3**. CONTEXT menyebut `RecomputeAfterEssayGradingAsync (:520)` — nama itu **tidak ada**; method di `:520` sebenarnya `RegradeAfterEditAsync` (re-grade pasca edit jawaban). Jalur essay-finalize sesungguhnya ada di **controller** (`AssessmentAdminController.FinalizeEssayGrading :3887`), bukan service. Keempat site: (1) `GradingService.GradeAndCompleteAsync :287` — TANPA cek PreTest, (2) `GradingService.RegradeAfterEditAsync :520` — ADA cek PreTest, (3) `AssessmentAdminController.FinalizeEssayGrading :3887` — TANPA cek PreTest, (4) `TrainingAdminController.AddManualAssessment :759` — hardcode `GenerateCertificate=true`, TANPA cek PreTest dan TANPA try/catch.

Penemuan penting untuk CERT-07: saat sesi ber-essay masuk status PendingGrading, kode **men-set `CompletedAt = DateTime.UtcNow` bersamaan** (`GradingService.cs:224`). Jadi **`CompletedAt` adalah timestamp "menunggu sejak"** yang tepat — sudah ter-expose di `EssayGradingPageViewModel.CompletedAt` dan `MonitoringSessionViewModel.CompletedAt`. Tidak perlu kolom timestamp baru.

**Primary recommendation:** Buat `Helpers/CertIssuanceRules.cs` (pure static) berisi 4-5 fungsi murni + 1-2 query helper async terpisah; pasang sebagai gate tunggal di 4 site; harden loop retry di tempat (tambah jitter + naikkan cap); pakai `UpdatedAt` (atau audit-log + queryable predicate `IsPassed==true && GenerateCertificate && NomorSertifikat==null`) sebagai sinyal "cert gagal terbit"; semua dijaga test pure + 1 real-SQL.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Aturan kelayakan cert (PreTest reject, type×ValidUntil) | Domain helper (pure, `Helpers/`) | Service/Controller (caller) | Aturan bisnis murni, EF-free → testable tanpa DB; satu source-of-truth (CERT-01/02/06). |
| Penomoran seq atomik + retry | Helper async (`CertNumberHelper`) + Service/Controller (loop) | DB (filtered unique index) | Butuh `DbContext` (read MAX) + concurrency control DB; index sudah ada. (CERT-03/04) |
| Anti double-cert query | Service/Controller (async EF query) | DB | Butuh query lintas sesi (UserId+Title); guard server-side authoritative (CERT-05). |
| Validasi namespace manual nomor | Domain helper (pure regex) | Controller (POST) | Format check murni; collision handling di controller try/catch (CERT-04). |
| PendingGrading age badge | View (Razor) + pure helper (umur→kelas badge) | ViewModel (sudah expose CompletedAt) | Presentasi; data sudah tersedia; tidak ada write (CERT-07). |

---

## Standard Stack

Tidak ada paket baru. Seluruh kapabilitas memakai stack existing.

### Core (existing, dipakai apa adanya)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore (SQL Server) | sesuai csproj current | Query/`ExecuteUpdateAsync`/filtered unique index | Sudah jadi data layer; `IX_AssessmentSessions_NomorSertifikat_Unique` melindungi penomoran [VERIFIED: Migrations/20260317143630_AddNomorSertifikatToAssessmentSessions.cs:19-24] |
| xUnit + `IClassFixture` | current | Pure unit + integration real-SQL | Pola test proyek (RetakeServiceFixture, NoOpHubContext) [VERIFIED: HcPortal.Tests/RetakeServiceTests.cs:34-70] |

### Alternatives Considered (dan DITOLAK oleh decisions)
| Instead of | Could Use | Tradeoff | Verdict |
|------------|-----------|----------|---------|
| Retry+jitter di app-level | DB `SEQUENCE` / `UPDATE...OUTPUT` / `SELECT...WITH(UPDLOCK,HOLDLOCK)` (audit menyebut sbg opsi [CITED: audit:311]) | Lebih kuat tapi butuh schema/migration | **DITOLAK D-03** (migration=FALSE). Pakai retry+jitter. |
| Validasi format manual | Prefix namespace baru (mis. "MAN/...") | Mengubah nomor cert yang akan tercetak | **DITOLAK** (CONTEXT §specifics). Pakai regex-reject-resembles. |
| Kolom flag baru `CertIssuanceFailed` | — | Butuh migration | **DITOLAK** (migration=FALSE). Pakai sinyal non-schema (lihat Pitfall/Discretion). |

**Installation:** Tidak ada. `dotnet build` cukup.

**Version verification:** Tidak relevan — tidak ada paket baru ditambahkan untuk fase ini. [VERIFIED: scope refactor, 0 dependency baru]

---

## Architecture Patterns

### System Architecture Diagram (alur penerbitan cert sesudah refactor)

```
                       ┌─────────────────────────────────────────────────────┐
   Worker submit ─────▶│ GradeAndCompleteAsync (:287)                          │
   (online MC/MA)      │   non-essay → Completed                               │
                       └───────────────┬─────────────────────────────────────┘
   Worker submit ─────▶ (essay) ───────┼──▶ Status=PendingGrading,            
   (ber-essay)                          │     CompletedAt=UtcNow (:224)  ◀── "menunggu sejak"
                                        │            │
   HC finalize ────────────────────────┼───▶ FinalizeEssayGrading (:3887)     
                                        │            │
   HC edit jawaban ────────────────────┼───▶ RegradeAfterEditAsync (:520)     
                                        │     (Fail→Pass)                       
   HC manual entry ────────────────────┼───▶ AddManualAssessment (:759)        
                                        ▼            │
                       ┌─────────────────────────────────────────────────────┐
                       │  GATE TUNGGAL: CertIssuanceRules.ShouldIssueCertificate(session)
                       │   • reject AssessmentType=="PreTest"  (CERT-01)        │
                       │   • require GenerateCertificate && IsPassed            │
                       └───────────────┬─────────────────────────────────────┘
                                       │ eligible?
                          ┌────────────┴────────────┐
                       no │                          │ yes
                          ▼                          ▼
                    (no cert)         ┌──────────────────────────────────────┐
                                      │ DeriveValidUntil(type, CompletedAt)   │ CERT-02/06
                                      │   Permanent→null; Annual→+1y; 3Y→+3y  │
                                      ├──────────────────────────────────────┤
                                      │ HasActiveCertForTitleAsync? ──▶ block │ CERT-05
                                      │   (exclude RenewsSessionId)           │
                                      ├──────────────────────────────────────┤
                                      │ GetNextSeqAsync + loop retry+JITTER   │ CERT-03
                                      │  WHERE NomorSertifikat==null          │
                                      │  ▲ filtered unique index              │
                                      │  fail → no cert + FLAG for HC (D-03)  │
                                      └──────────────────────────────────────┘
   Manual nomor free-text ──▶ ResemblesAutoCertFormat? reject (CERT-04)
   Manual insert ──▶ try/catch DbUpdateException → friendly error (CERT-04)
```

### Recommended Project Structure (delta)
```
Helpers/
├── CertNumberHelper.cs        # EXISTING — harden GetNextSeqAsync (retry/jitter helper), tambah ResemblesAutoCertFormat
└── CertIssuanceRules.cs       # NEW — pure: ShouldIssueCertificate, DeriveValidUntil, PendingAgeBadge, (active-dup as expression?)
Services/
└── GradingService.cs         # wire gate di :287 + :520; harden loop retry; derive ValidUntil
Controllers/
├── AssessmentAdminController.cs   # FinalizeEssayGrading :3887 wire gate; ConfirmDuplicateTitle anti-dup guard :995-1007
└── TrainingAdminController.cs     # AddManualAssessment :759 stop hardcode; try/catch; namespace validate
HcPortal.Tests/
├── CertIssuanceRulesTests.cs      # NEW pure truth-table (analog SessionEditLockRulesTests)
└── CertIssuanceIntegrationTests.cs# NEW real-SQL (analog RetakeThenPassCertTests)
```

### Pattern 1: Pure rules helper (analog 422 SessionEditLockRules)
**What:** Kelas `static`, EF-free, menerima `AssessmentSession` / scalar, kembalikan keputusan.
**When to use:** Aturan deterministik tanpa I/O (PreTest reject, ValidUntil derivation, badge class, format regex).
**Example:**
```csharp
// Pola sumber: Helpers/SessionEditLockRules.cs:21-22 (VERIFIED)
public static class CertIssuanceRules
{
    // CERT-01 — gate tunggal: tolak PreTest, wajib GenerateCertificate && lulus.
    public static bool ShouldIssueCertificate(AssessmentSession s)
        => s.GenerateCertificate
           && s.IsPassed == true
           && s.AssessmentType != AssessmentConstants.AssessmentType.PreTest;

    // CERT-02/06 — derive ValidUntil dari CompletedAt + CertificateType (D-04/D-05).
    // Permanent → null; Annual → +1y; 3-Year → +3y; lain/null → null (atau policy planner).
    public static DateOnly? DeriveValidUntil(string? certificateType, DateTime? completedAt)
    {
        if (completedAt == null) return null;
        var baseDate = DateOnly.FromDateTime(completedAt.Value);
        return certificateType switch
        {
            AssessmentConstants.CertificateType.Permanent => null,
            AssessmentConstants.CertificateType.Annual    => baseDate.AddYears(1),
            AssessmentConstants.CertificateType.ThreeYear => baseDate.AddYears(3),
            _ => null
        };
    }

    // CERT-04 — manual nomor TIDAK boleh menyerupai auto KPB/{seq:D3}/{ROMAN}/{YEAR}.
    public static bool ResemblesAutoCertFormat(string? nomor)
        => !string.IsNullOrWhiteSpace(nomor)
           && System.Text.RegularExpressions.Regex.IsMatch(nomor!, @"^KPB/\d{3}/[IVX]+/\d{4}$");

    // CERT-07 — umur PendingGrading → Bootstrap badge class (D-09).
    public static string PendingAgeBadgeClass(DateTime? completedAtUtc, DateTime nowUtc)
    {
        if (completedAtUtc == null) return "bg-secondary";
        var days = (nowUtc - completedAtUtc.Value).TotalDays;
        if (days > 7) return "bg-danger";
        if (days > 3) return "bg-warning text-dark";
        return "bg-secondary";
    }
}
```
> Catatan: tanggal-dasar `CompletedAt` adalah `DateTime` (UTC), `ValidUntil` adalah `DateOnly?` [VERIFIED: Models/AssessmentSession.cs:57,84]. `ThreeYear` konstanta = `"3-Year"` [VERIFIED: Models/AssessmentConstants.cs:28].

### Pattern 2: Hardened seq loop (in-place, no schema)
**What:** Loop retry yang sudah ada di 3 site (`while certAttempts < maxCertAttempts`) ditingkatkan: cap lebih besar + jitter delay sebelum retry, gate `WHERE NomorSertifikat==null` dipertahankan, filtered unique index dipertahankan.
**Example:**
```csharp
// Pola sumber: GradingService.cs:295-318 (VERIFIED). Delta: maxCertAttempts ↑, + jitter.
const int maxCertAttempts = 8;                       // ↑ dari 3 (D-03 lebih tahan burst)
var rng = Random.Shared;
while (!certSaved && certAttempts < maxCertAttempts)
{
    certAttempts++;
    try
    {
        var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
        var updated = await _context.AssessmentSessions
            .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
        if (updated > 0) certSaved = true;
    }
    catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex))
    {
        await Task.Delay(rng.Next(10, 60));          // jitter — kurangi thundering-herd MAX+1
    }
}
if (!certSaved)
{
    // D-03 non-destruktif: sesi sudah Completed/IsPassed (commit lebih dulu) → JANGAN rollback.
    // Sinyal HC (lihat Don't-Hand-Roll / Discretion): UpdatedAt stamp + audit "CertIssuanceFailed".
}
```
> Konkret: bisa ekstrak loop ini ke `CertNumberHelper.TryAssignNextSeqAsync(ctx, sessionId, certNow, maxAttempts)` agar 3 service/controller-site memanggil 1 fungsi (mengurangi drift). Itu menyelaraskan CERT-03 lintas site.

### Pattern 3: Anti double-cert query (server-authoritative, di luar ConfirmDuplicateTitle)
**What:** Query EF: ada sesi lain (UserId, judul-ternormalisasi) yang cert-nya AKTIF (ValidUntil null OR >= today), kecuali sesi renewal.
**Example:**
```csharp
// Aktif = ValidUntil==null OR ValidUntil>=today (D-07). Renewal (RenewsSessionId terisi) DIKECUALIKAN.
var today = DateOnly.FromDateTime(DateTime.UtcNow);
var norm = AdminBaseController.NormalizeTitleForDup(model.Title);   // reuse normalizer (VERIFIED AdminBaseController.cs:271)
bool hasActive = (await _context.AssessmentSessions
        .Where(s => s.UserId == targetUserId
                 && s.NomorSertifikat != null
                 && s.IsPassed == true
                 && s.RenewsSessionId == null            // renewal lolos guard
                 && (s.ValidUntil == null || s.ValidUntil >= today))
        .Select(s => s.Title)
        .ToListAsync())                                  // normalisasi di memory (normalizer C#-only)
    .Any(t => AdminBaseController.NormalizeTitleForDup(t) == norm);
// Guard HARUS server-side, di luar cabang `!ConfirmDuplicateTitle` (VAL-04 — ConfirmDuplicateTitle
// hanya override soft-block JUDUL, bukan guard cert-aktif domain).
```
> Penting: `FindTitleDuplicatesAsync` existing TIDAK cek IsPassed/UserId/cert [VERIFIED: AdminBaseController.cs:278-293] → ia bukan guard cert. Guard baru ini terpisah & unconditional (kontras dengan soft-block judul di `:995-1007` yang dibungkus `!ConfirmDuplicateTitle`).

### Anti-Patterns to Avoid
- **Menaruh PreTest-check hanya di sebagian site.** Justru itu root-cause GRD-01/FLD-5.2-10 — keempat site WAJIB lewat helper sama.
- **Rollback sesi saat seq gagal.** Melanggar D-03 (non-destruktif). Sesi sudah Completed sebelum cert; biarkan selesai, tandai.
- **Memasang anti-dup guard di dalam `if (!ConfirmDuplicateTitle)`.** Itulah bypass VAL-04. Guard cert-aktif harus di luar.
- **Menyentuh baris lama mismatch type×ValidUntil.** D-06 forward-only; jangan batch-update.
- **Mengubah format/teks nomor cert yang sudah tercetak.** D-02 — hanya validasi namespace untuk entri baru.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Deteksi kolisi nomor cert | Parse pesan SQL manual | `CertNumberHelper.IsDuplicateKeyException(ex)` | Sudah handle nama index + error 2601/2627 [VERIFIED: CertNumberHelper.cs:37-42] |
| Build nomor cert | String concat ad-hoc | `CertNumberHelper.Build(seq, date)` + `ToRomanMonth` | Format kanonik `KPB/{seq:D3}/{ROMAN}/{YEAR}` single-source [VERIFIED: CertNumberHelper.cs:20-21] |
| Normalisasi judul utk dup-check | Lowercase manual | `AdminBaseController.NormalizeTitleForDup` | trim+collapse-ws+lowercase, dipakai existing dup logic [VERIFIED: AdminBaseController.cs:271-272] |
| Uniqueness nomor cert | Cek manual sebelum insert | filtered unique index `IX_AssessmentSessions_NomorSertifikat_Unique` | DB-enforced, race-safe, sudah ada [VERIFIED: Migrations/...AddNomorSertifikat:19-24] |
| Timestamp "menunggu sejak" | Kolom baru | `session.CompletedAt` (di-set saat →PendingGrading) | Sudah di-set bersamaan [VERIFIED: GradingService.cs:224]; sudah di VM [VERIFIED: AssessmentMonitoringViewModel.cs:56,99] |

**Key insight:** Hampir semua primitif penerbitan cert SUDAH ADA — fase ini soal **menyatukan & mengeraskan**, bukan membangun baru. Risiko terbesar = *melewatkan satu dari 4 site*.

### Sinyal "cert gagal terbit utk HC" (Claude's Discretion D-03) — opsi non-schema
1. **`UpdatedAt` + queryable predicate** (REKOMENDASI utama, zero-schema): sesi yang `IsPassed==true && GenerateCertificate==true && AssessmentType!="PreTest" && NomorSertifikat==null` adalah secara definisi "lulus tapi cert belum/gagal terbit". HC bisa difilter dengan predikat ini di list. Tambah `UpdatedAt=UtcNow` saat gagal agar terlihat "baru". **Kelebihan:** tidak ada kolom baru, query deterministik, idempotent (begitu cert terbit predikat hilang). **Catatan:** PXF-08 sudah memakai prinsip ini di FinalizeEssayGrading — `certError` di-surface bila `GenerateCertificate && isPassed && string.IsNullOrEmpty(NomorSertifikat)` [VERIFIED: AssessmentAdminController.cs:3972-3974]. Konsisten-kan ke semua site + tampilkan badge di list.
2. **AuditLog `"CertIssuanceFailed"`** (komplementer, queryable trail): pola sudah ada (`audit.LogAsync(..., actionType, ...)`) [VERIFIED: CertGateAuditTests.cs:25-28]. Beri HC jejak terbit-ulang.
3. **TempData** (hanya untuk jalur interaktif HC, mis. FinalizeEssayGrading yang return ke HC) — kurang tahan untuk jalur worker submit (tak ada HC di request). Gunakan #1 sebagai primary.

> Rekomendasi planner: **#1 sebagai sinyal queryable utama** (predikat lulus-tanpa-nomor) + **#2 audit trail** + reuse pola PXF-08 untuk friendly message di jalur HC. Semua non-destruktif, no migration.

---

## Common Pitfalls

### Pitfall 1: Nama method salah di CONTEXT (`RecomputeAfterEssayGradingAsync`)
**What goes wrong:** Planner mencari method `RecomputeAfterEssayGradingAsync` — TIDAK ADA.
**Root cause:** Method di `:520` sebenarnya `RegradeAfterEditAsync` (re-grade pasca edit jawaban; cert hanya saat flip Fail→Pass). Jalur essay-finalize sesungguhnya ada di **controller** `FinalizeEssayGrading :3887`. [VERIFIED: Grep "RecomputeAfterEssayGrading" = 0 match; method definitions di GradingService.cs:456, AssessmentAdminController.cs:3749]
**How to avoid:** Petakan ke 4 site by-line (tabel di Code Examples). Jangan mencari nama dari CONTEXT verbatim.

### Pitfall 2: Melewatkan site keempat (manual) & ketiga (essay-finalize)
**What goes wrong:** Hanya 2 site GradingService di-wire; manual & essay-finalize tetap divergen → GRD-01 belum tertutup.
**Root cause:** Cert-issue tersebar lintas 2 file + 2 controller. Manual: hardcode `=true` [VERIFIED: TrainingAdminController.cs:759]; essay-finalize: no PreTest check [VERIFIED: AssessmentAdminController.cs:3887].
**How to avoid:** Checklist 4-site wajib lulus gate sebelum verify. Test memverifikasi PreTest reject di tiap site.

### Pitfall 3: Anti-dup guard di-bypass via ConfirmDuplicateTitle
**What goes wrong:** Guard ditaruh dalam blok `if (!ConfirmDuplicateTitle)` → operator centang konfirmasi = cert dobel.
**Root cause:** Soft-block judul existing memang dibungkus `!ConfirmDuplicateTitle && !isRenewalModePost` [VERIFIED: AssessmentAdminController.cs:995-997].
**How to avoid:** Guard cert-aktif terpisah & unconditional (kontras pola double-renewal guard `:1014-1028` yang unconditional). Pertahankan skip judul untuk renewal tapi tetap cek kelulusan-aktif.

### Pitfall 4: Lupa filter renewal di anti-dup query
**What goes wrong:** Renewal resmi (RenewsSessionId terisi) ditolak guard → HC tak bisa perpanjang.
**Root cause:** Renewal *memang* punya judul sama dengan cert aktif asal.
**How to avoid:** `s.RenewsSessionId == null` di predikat (D-07 pengecualian renewal).

### Pitfall 5: ValidUntil pakai tanggal terbit/hari-ini
**What goes wrong:** Peserta yang sesi-nya lama di-grade dapat masa berlaku lebih pendek/tak adil.
**Root cause:** Tergoda pakai `DateTime.Now` saat issue.
**How to avoid:** D-05 — `DeriveValidUntil` dari `CompletedAt`. `CompletedAt` adalah `DateTime?` UTC; konversi `DateOnly.FromDateTime` sebelum `AddYears`.

### Pitfall 6: DateOnly vs DateTime + UTC drift
**What goes wrong:** Bandingkan `ValidUntil` (DateOnly) dengan `DateTime.Now` → compile/logic error.
**Root cause:** `ValidUntil` = `DateOnly?` (TZ-01 refactor) [VERIFIED: AssessmentSession.cs:84]; `DeriveCertificateStatus` pakai `DateOnly.FromDateTime(DateTime.UtcNow)` + `DayNumber` arithmetic [VERIFIED: CertificationManagementViewModel.cs:61-62].
**How to avoid:** Ikuti pola existing: `today = DateOnly.FromDateTime(DateTime.UtcNow)`; bandingkan `ValidUntil >= today`. Untuk umur PendingGrading (CERT-07) `CompletedAt` adalah `DateTime` UTC → pakai `(DateTime.UtcNow - CompletedAt).TotalDays`.

### Pitfall 7: Permanent×ValidUntil mutual-exclusion sudah ada di Training tapi BUKAN di AddManualAssessment
**What goes wrong:** Validasi Permanent⊥ValidUntil ada di `AddManualTraining` (`:269`, `:499`) [VERIFIED] tapi TIDAK di `AddManualAssessment` POST (`:689`).
**How to avoid:** Tambah cek yang sama (atau panggil helper) di jalur assessment manual (CERT-06).

---

## Code Examples

### Peta presisi 4 cert-issue site (kondisi terverifikasi)
```
SITE 1 — Services/GradingService.cs:287
  if (session.GenerateCertificate && isPassed)          // ❌ TANPA cek PreTest (GRD-01)
  → retry-3x loop :295-318, WHERE NomorSertifikat==null :302, log-only fail :316
  → ValidUntil TIDAK di-set di sini (GRD-06)

SITE 2 — Services/GradingService.cs:520 (di dalam RegradeAfterEditAsync, cabang Fail→Pass)
  if (session.GenerateCertificate && session.AssessmentType != "PreTest")  // ✅ ADA cek PreTest
  → retry-3x loop :527-545, updated>0 guard :539

SITE 3 — Controllers/AssessmentAdminController.cs:3887 (FinalizeEssayGrading — jalur essay HC)
  if (session.GenerateCertificate && isPassed)          // ❌ TANPA cek PreTest (GRD-01/FLD-5.2-10)
  → retry-3x loop :3894-3910; PXF-08 certError surface :3972-3974 (sudah ada pola sinyal)

SITE 4 — Controllers/TrainingAdminController.cs:759 (AddManualAssessment POST :689)
  GenerateCertificate = true,                           // ❌ HARDCODE (FLD-5.2-02)
  NomorSertifikat = wc.NomorSertifikat,  (free-text :750, namespace tak divalidasi — FLD-5.2-07)
  → SaveChangesAsync :765 TANPA try/catch DbUpdateException (kolisi → 500)
  → dup-guard existing hanya ManualDuplicatePredicate (UserId+Title+CompletedAt, :722) — bukan cert-aktif
```

### Pure-helper unit test (analog SessionEditLockRulesTests)
```csharp
// Pola sumber: HcPortal.Tests/SessionEditLockRulesTests.cs:12-27 (VERIFIED)
public class CertIssuanceRulesTests
{
    [Theory]
    [InlineData("PreTest", true, true, false)]    // PreTest → SELALU tolak (CERT-01)
    [InlineData("PostTest", true, true, true)]    // lulus + generate → terbit
    [InlineData("PostTest", false, true, false)]  // tak lulus → tidak
    [InlineData("PostTest", true, false, false)]  // GenerateCertificate=false → tidak
    [InlineData("Standard", true, true, true)]
    public void ShouldIssueCertificate_TruthTable(string? type, bool generate, bool passed, bool expected)
    {
        var s = new AssessmentSession { AssessmentType = type, GenerateCertificate = generate, IsPassed = passed };
        Assert.Equal(expected, CertIssuanceRules.ShouldIssueCertificate(s));
    }

    [Theory]
    [InlineData("Permanent", null)]       // Permanent → null (D-04)
    [InlineData("Annual", 1)]
    [InlineData("3-Year", 3)]
    public void DeriveValidUntil_FromCompletedAt(string certType, int? addYears)
    {
        var completed = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc);
        var got = CertIssuanceRules.DeriveValidUntil(certType, completed);
        var expected = addYears == null ? (DateOnly?)null
            : DateOnly.FromDateTime(completed).AddYears(addYears.Value);
        Assert.Equal(expected, got);
    }
}
```

### Integration real-SQL recipe (verbatim dari RetakeThenPassCertTests)
```csharp
// Pola sumber: HcPortal.Tests/RetakeThenPassCertTests.cs:39-61 (VERIFIED)
[Trait("Category", "Integration")]
public class CertIssuanceIntegrationTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public CertIssuanceIntegrationTests(RetakeServiceFixture f) => _fixture = f;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static GradingService NewGrading(ApplicationDbContext ctx)
    {
        var fakeNotif = new FakeNotificationService();
        var audit = new AuditLogService(ctx);
        var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
        var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
        var worker = new FakeWorkerDataService();
        return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
    }
    // Test: PreTest + GenerateCertificate=true + all-correct → graded TRUE tetapi NomorSertifikat==null (CERT-01).
    // Test: PostTest lulus → tepat 1 cert + format KPB/\d{3}/[IVX]+/\d{4} (sudah di RetakeThenPassCertTests — jangan regress).
}
```
> CI tanpa SQL: filter `--filter "Category!=Integration"`. Pure tests selalu jalan. [VERIFIED: RetakeThenPassCertTests.cs:8-9]

### CERT-07 badge di view
```cshtml
@* EssayGrading.cshtml — Model.CompletedAt sudah dipakai (line 101-102 tooltip). Tambah badge: *@
@if (Model.CompletedAt.HasValue) {
  var days = (DateTime.UtcNow - Model.CompletedAt.Value).TotalDays;
  var cls = days > 7 ? "bg-danger" : days > 3 ? "bg-warning text-dark" : "bg-secondary";
  <span class="badge @cls">Menunggu @((int)days) hari</span>
}
@* AssessmentMonitoringDetail.cshtml — per-session row punya session.CompletedAt + session.Status
   (MonitoringSessionViewModel:56,66). Render badge bila UserStatus=="Menunggu Penilaian". *@
```
> Lokasi render list: `AssessmentMonitoringDetail.cshtml` — status badge "Menunggu Penilaian" di `:248/255`, tabel essay-pending + link `EssayGrading` di `:404-450` [VERIFIED]. Aggregate count di `MenungguPenilaianCount` (kartu `:179`). ManageAssessment top-level menampilkan group aggregate; detail per-worker di MonitoringDetail.

---

## Runtime State Inventory

> Fase ini = refactor + guard di branch ITHandoff, migration=FALSE. Bukan rename/migrasi data. Inventory ringkas:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | Baris cert lama dengan mismatch CertificateType×ValidUntil (Permanent + ValidUntil terisi) MUNGKIN ada di DB Dev | **None — D-06 forward-only**, jangan sentuh. |
| Live service config | None — verified (tak ada service eksternal terlibat penerbitan cert). | None. |
| OS-registered state | None — verified (tak ada Task Scheduler/background service; grep `BackgroundService/AddHostedService`=0 [CITED: audit:406]). | None. |
| Secrets/env vars | None — verified. | None. |
| Build artifacts | None baru — kelas helper baru ter-compile via `dotnet build`. | `dotnet build` standar. |

**Migration:** FALSE (dikonfirmasi CONTEXT + ROADMAP). Unique index `IX_AssessmentSessions_NomorSertifikat_Unique` SUDAH ADA — tidak ditambah [VERIFIED: Migrations/20260317143630].

---

## Validation Architecture

> nyquist_validation = true [VERIFIED: .planning/config.json:15] → section ini WAJIB.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (assembly `HcPortal.Tests`) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` |
| Quick run command | `dotnet test HcPortal.Tests --filter "Category!=Integration"` |
| Full suite command | `dotnet test HcPortal.Tests` (butuh `localhost\SQLEXPRESS` untuk Integration) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CERT-01 | `ShouldIssueCertificate` tolak PreTest, truth-table | unit (pure) | `dotnet test --filter "FullyQualifiedName~CertIssuanceRulesTests"` | ❌ Wave 0 |
| CERT-01 | PreTest+generate+lulus → NomorSertifikat==null end-to-end (≥1 dari 4 site) | integration | `dotnet test --filter "FullyQualifiedName~CertIssuanceIntegrationTests"` | ❌ Wave 0 |
| CERT-02/06 | `DeriveValidUntil` Permanent→null / Annual→+1y / 3-Year→+3y dari CompletedAt | unit (pure) | (sama CertIssuanceRulesTests) | ❌ Wave 0 |
| CERT-03 | Loop retry+jitter terbit tepat 1 cert (regression anti-double) | integration | `dotnet test --filter "FullyQualifiedName~RetakeThenPassCert"` (existing) + baru | ✅ existing (RetakeThenPassCertTests) |
| CERT-03 | Seq-fail → sesi tetap Completed + sinyal predikat (lulus & NomorSertifikat==null) | integration | (CertIssuanceIntegrationTests) | ❌ Wave 0 |
| CERT-04 | `ResemblesAutoCertFormat` regex truth-table | unit (pure) | (CertIssuanceRulesTests) | ❌ Wave 0 |
| CERT-04 | Manual insert kolisi → ModelState error ramah (bukan 500) | integration/controller | (CertIssuanceIntegrationTests atau controller test) | ❌ Wave 0 |
| CERT-05 | `HasActiveCertForTitleAsync` block; renewal lolos; ConfirmDuplicateTitle tak bypass | integration | (CertIssuanceIntegrationTests) | ❌ Wave 0 |
| CERT-07 | `PendingAgeBadgeClass` >3→warning, >7→danger | unit (pure) | (CertIssuanceRulesTests) | ❌ Wave 0 |
| CERT-07 | Badge render di EssayGrading + MonitoringDetail | manual UAT (browser @5270) | manual — Playwright opsional | n/a |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests --filter "Category!=Integration"` (pure cepat <30s)
- **Per wave merge:** `dotnet test HcPortal.Tests` (full incl Integration; SQLEXPRESS live)
- **Phase gate:** Full suite green sebelum `/gsd-verify-work` + UAT browser @5270 untuk CERT-07 badge.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/CertIssuanceRulesTests.cs` — pure truth-table CERT-01/02/04/06/07 (analog SessionEditLockRulesTests)
- [ ] `HcPortal.Tests/CertIssuanceIntegrationTests.cs` — real-SQL CERT-01 (PreTest reject di site nyata) + CERT-03 (seq-fail signal) + CERT-05 (anti-dup, renewal-exempt)
- [ ] Reuse fixture: `RetakeServiceFixture` + `NoOpHubContext` + GradingService ctor recipe — SUDAH ADA, tidak perlu install [VERIFIED: RetakeServiceTests.cs:34-70, RetakeThenPassCertTests.cs:53-61]
- [ ] Regression guard: jangan rusak `RetakeThenPassCertTests`, `CertDedupTests`, `CertificateStatusTests`, `CertGateAuditTests`, `CertAlertConsistencyTests` (existing cert tests)

---

## Security Domain

> security_enforcement diasumsikan enabled (absent = enabled). Fase ini menyentuh penerbitan kredensial (sertifikat) → relevan.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | tak diubah |
| V4 Access Control | yes | Semua endpoint cert sudah `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` [VERIFIED: AddManualAssessment :686-688; FinalizeEssayGrading :3749; SubmitEssayScore :3667-3669] — pertahankan saat refactor. |
| V5 Input Validation | yes | Manual `NomorSertifikat` free-text → validasi namespace regex (CERT-04); cross-validate Permanent⊥ValidUntil server-side (CERT-06). |
| V6 Cryptography | no | tak ada crypto baru |
| V11 Business Logic | yes | Anti double-cert (CERT-05) = business-logic integrity; PreTest-no-cert (CERT-01) = authoritative server gate; seq atomicity (CERT-03) = race/integrity. |

### Known Threat Patterns
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Bypass anti-dup via ConfirmDuplicateTitle (VAL-04) | Elevation/Tampering | Guard server-side di luar cabang konfirmasi, unconditional |
| Race penerbitan nomor (burst finalize) GRD-08 | Tampering/DoS-soft | Filtered unique index + retry+jitter; non-destruktif fallback |
| Cert untuk PreTest via judul "Pre Test" + Standard (FLD-5.2-10) | Spoofing (kredensial palsu) | Server-side gate `ShouldIssueCertificate` di SEMUA site, bukan default/JS-warning |
| Manual nomor menabrak namespace auto (FLD-5.2-07) | Tampering | Regex reject + try/catch friendly error |

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Cert-gate inline tersebar 4 site (sebagian cek PreTest, sebagian tidak) | Helper pure `ShouldIssueCertificate` single-source | Fase 423 ini | Konsisten, testable (pola 422 SessionEditLockRules) |
| `ValidUntil` `DateTime?` | `DateOnly?` (TZ-01 v19.0) | Phase 327 | Hindari tz drift; pakai `DayNumber` arithmetic [VERIFIED] |
| Seq retry-3x log-only fail | retry+jitter cap lebih tinggi + sinyal HC | Fase 423 ini | Tahan burst; non-destruktif visible |

**Deprecated/outdated:**
- Nama `RecomputeAfterEssayGradingAsync` (disebut CONTEXT) — tidak pernah ada di kode current. Gunakan `RegradeAfterEditAsync` (:456/:520) + `FinalizeEssayGrading` (:3749/:3887).

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Untuk `CertificateType` selain Permanent/Annual/3-Year (atau null), `DeriveValidUntil` mengembalikan `null`. CONTEXT hanya kunci 3 nilai. | Pattern 1 / CERT-06 | Rendah — manual entry punya CertificateType lain (Kompetensi/Profesi/Pelatihan [VERIFIED: TrainingAdminController.cs:1249]); planner harus konfirmasi apakah nilai-nilai itu memerlukan ValidUntil. **Perlu konfirmasi user/planner.** |
| A2 | Sinyal "cert gagal terbit" pakai predikat queryable (lulus & GenerateCertificate & NomorSertifikat==null) + audit, BUKAN kolom baru. | Don't Hand-Roll | Rendah — D-03 menyerahkan bentuk ke discretion; predikat ini idempotent & no-schema. Planner pilih final. |
| A3 | "Daftar ManageAssessment" untuk badge umur = surface `AssessmentMonitoringDetail.cshtml` (tempat link EssayGrading + status badge per-worker). ManageAssessment top-level hanya aggregate count. | CERT-07 / Code Examples | Rendah — D-08 minta "dua tempat HC bekerja"; MonitoringDetail adalah list per-worker yang relevan. Planner verifikasi spot final saat planning UI. |

**Catatan:** A1 adalah satu-satunya yang berpotensi mengubah scope CERT-06 — planner sebaiknya klarifikasi perilaku ValidUntil untuk CertificateType manual non-kanonik.

---

## Open Questions

1. **CertificateType non-kanonik (Kompetensi/Profesi/Pelatihan) di manual entry — apakah dapat ValidUntil?**
   - What we know: D-04 hanya menyebut Permanent/Annual/3-Year [VERIFIED: AssessmentConstants.cs:24-29]. Manual entry punya nilai lain [VERIFIED: TrainingAdminController.cs:1249].
   - What's unclear: Apakah manual entry tetap pakai `model.ValidUntil` apa adanya (HC isi manual) dan helper derive HANYA untuk online auto-gen?
   - Recommendation: Untuk **online/grading-time** site, gunakan `DeriveValidUntil`. Untuk **manual entry**, HC mengisi ValidUntil sendiri → cukup validasi Permanent⊥ValidUntil (CERT-06) tanpa auto-derive. Planner konfirmasi.

2. **Apakah `GetNextSeqAsync` mau diekstrak jadi `CertNumberHelper.TryAssignNextSeqAsync` (loop di 1 tempat) atau loop tetap di tiap site?**
   - What we know: Loop retry identik di 3 site (:295, :527, :3894).
   - Recommendation: Ekstrak ke helper agar CERT-03 satu-source (mengurangi drift, satu titik jitter). Lebih bersih untuk verify.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK / dotnet | build + test | ✓ (proyek aktif) | sesuai global.json | — |
| SQL Server (localhost\SQLEXPRESS) | Integration tests (RetakeServiceFixture, MigrateAsync) | ✓ (dipakai test existing) | — | Pure tests via `--filter "Category!=Integration"` |
| App @ http://localhost:5270 (branch ITHandoff) | UAT browser CERT-07 badge | ✓ (per CLAUDE.md) | — | manual review markup |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** Integration tests perlu SQLEXPRESS; pure tests tidak.

---

## Sources

### Primary (HIGH confidence — verified this session)
- `Services/GradingService.cs:170-336, 450-572` — site 1 (:287) & site 2 (:520, RegradeAfterEditAsync); PendingGrading set CompletedAt (:224)
- `Controllers/AssessmentAdminController.cs:825-1045, 3260-3290, 3603-3974` — ConfirmDuplicateTitle/FindTitleDuplicatesAsync; willGenerateCert; EssayGrading; FinalizeEssayGrading site 3 (:3887) + PXF-08 (:3972)
- `Controllers/TrainingAdminController.cs:230-270, 685-774, 1249` — AddManualAssessment site 4 (:759, hardcode); Permanent⊥ValidUntil di Training (:269/:499); ManualDuplicatePredicate guard (:722)
- `Controllers/AdminBaseController.cs:262-294` — ManualDuplicatePredicate, NormalizeTitleForDup, FindTitleDuplicatesAsync
- `Helpers/CertNumberHelper.cs` (full) — Build, GetNextSeqAsync (MAX+1), IsDuplicateKeyException, ToRomanMonth
- `Helpers/SessionEditLockRules.cs` (full) — pola pure-helper analog
- `Models/AssessmentSession.cs` (full) — kolom: ValidUntil DateOnly?, NomorSertifikat, RenewsSessionId, GenerateCertificate, CompletedAt, IsPassed, AssessmentType, CertificateType
- `Models/AssessmentConstants.cs` (full) — CertificateType (Permanent/Annual/3-Year), AssessmentType (PreTest…), PendingGrading status
- `Models/CertificationManagementViewModel.cs` — DeriveCertificateStatus (:54-66, DateOnly+UtcNow pattern)
- `Models/AssessmentMonitoringViewModel.cs` — MonitoringSessionViewModel.CompletedAt/Status (:56,66); EssayGradingPageViewModel.CompletedAt (:99)
- `Views/Admin/EssayGrading.cshtml:101-102` — Model.CompletedAt tooltip (spot badge)
- `Views/Admin/AssessmentMonitoringDetail.cshtml:179,248,255,404-450` — status badge + essay-pending list + EssayGrading link
- `HcPortal.Tests/RetakeThenPassCertTests.cs` (full) — recipe integration real-SQL (fixture+ctor+seed)
- `HcPortal.Tests/SessionEditLockRulesTests.cs` (full) — recipe pure truth-table
- `HcPortal.Tests/RetakeServiceTests.cs:34-70` — RetakeServiceFixture + NoOpHubContext
- `Migrations/20260317143630_AddNomorSertifikatToAssessmentSessions.cs` — filtered unique index existing
- `.planning/config.json` — nyquist_validation:true, commit_docs:true

### Secondary (MEDIUM — audit doc)
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` (lines 134-138, 168-180, 234, 304-311, 366-369, 381-388, 396-408) — GRD-01/05/06/08/10, FLD-5.2-02/07/09/10, VAL-04 evidence + saran fix

### Tertiary (LOW)
- None — semua klaim material diverifikasi terhadap kode.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new deps; semua primitif terverifikasi.
- Architecture (4-site map, helper placement, anti-dup query): HIGH — line-verified.
- Pitfalls: HIGH — termasuk koreksi nama method CONTEXT yang salah (verified 0-match).
- Validation: HIGH — fixture & test recipe terverifikasi dari test existing.
- CERT-06 manual ValidUntil semantics: MEDIUM — A1 perlu konfirmasi planner/user.

**Research date:** 2026-06-24
**Valid until:** 2026-07-24 (stable; kode internal, no fast-moving external dep)
