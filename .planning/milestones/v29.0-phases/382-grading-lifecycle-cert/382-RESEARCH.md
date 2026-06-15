# Phase 382: Grading / Lifecycle / Cert - Research

**Researched:** 2026-06-14
**Domain:** ASP.NET Core 8 MVC + EF Core 8 (SQL Server) — grading correctness, session lifecycle race-safety, certificate status semantics
**Confidence:** HIGH (semua temuan diverifikasi langsung terhadap source code repo; tidak ada klaim external library yang belum dikonfirmasi)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (SAVE-01) = Dedupe last-write-wins, NO migration.** `PackageUserResponse` TAK punya kolom diskriminator QuestionType (cuma FK `PackageQuestionId`) → filtered unique index via EF `HasFilter` tak feasible (filter SQL Server hanya boleh refer kolom tabel yang sama). Solusi: (1) grading read FINAL per soal via `GroupBy(PackageQuestionId).OrderByDescending(r => r.SubmittedAt).First()` di `GradingService` + `SubmitExam`; (2) `SaveAnswer` upsert hardened best-effort (toleransi baris dup fisik). **Riset pola dedupe-read + SaveAnswer concurrency, BUKAN filtered-index migration.**
- **D-01-IMPACT (milestone):** Phase 382 → **Migration=false** → v29.0 total = 0 migration baru. Aksi saat plan: update field `Migration` Phase 382 di ROADMAP ke `false`, hapus klaim "1 migration @ Phase 382" di summary v29.0/STATE, dan TIDAK perlu notify IT migration.
- **D-02 (STAT-01) = Guard grading + submit exclude Abandoned/Cancelled/PendingGrading** (bukan cuma Completed). `GradeAndCompleteAsync` (essay & non-essay branch) + `SubmitExam` early lifecycle-guard.
- **D-03 (STAT-02) = AbandonExam → `ExecuteUpdate` ber-guard `Where(Status==InProgress||Open)` + branch `rowsAffected==0` reject.**
- **D-04 (TMR-01) = `EnsureCanSubmitExamAsync` cakup "Standard"** — balik logika agar hanya SKIP untuk Manual/null. Submit MANUAL Standard jauh-telat → ditolak (Tier-1/2) + audit `SubmitExamBlocked`.
- **D-05 (TMR-01/03) = Jawaban yang sudah ke-save TETAP ter-grade** lewat auto-submit on-time (tidak hangus); fix TMR-03 token TAK dikonsumsi sebelum grading commit.
- **D-06 (TMR-02) = Validasi server-side, jangan percaya client `isAutoSubmit` mentah.**
- **D-07 (TOK-02) = StartedAt-gate di `SaveAnswer` + `SubmitExam`** — diimplementasi SEKALI, koheren, bareng lifecycle-guard STAT-01 di handler yang sama (audit: JANGAN taruh di Phase 381 — same-method conflict).
- **D-08 (CERT-01) = `ValidUntil==null` = "Permanen/Aktif" (tanpa kedaluwarsa)** — konsisten lintas SEMUA surface (helper + badge + notif + AdminBase renewal post-filter + Renewal/CDP tally). DIKELUARKAN dari worklist renewal + badge + notif.
- **D-08-TEST:** Test lama `DeriveCertificateStatus_NullValidUntil_NonPermanent_ReturnsExpired` AKAN break → rewrite ke null→Permanen/Aktif.
- **STAT reject pattern = reject + pesan jelas (BI) + audit log** (Phase 380 defensive pattern).

### Claude's Discretion
- Bentuk persis upsert SaveAnswer (delete-then-insert vs find-update) dalam transaksi.
- Wording pesan reject (BI): submit-telat "waktu habis", resurrect/abandon-overwrite ditolak.
- Nama/bentuk audit log entry untuk STAT reject + `SubmitExamBlocked`.
- Apakah dedupe-read pakai GroupBy in-memory atau window-function SQL (selama hasil = opsi final).

### Deferred Ideas (OUT OF SCOPE)
- **RES-02** (X/Y benar vs Score% display drift) — backlog.
- **GRD-02** (empty-MA SetEquals, LOW) — backlog.
- Proton (BYP/PEL/T3), essay (EDT/ESS/GRD-01/PASS-01/RES-01/SAVE-03/OPS-03), multiple-answer (GRD-03/RES-03), admin-not-on-worker-path (CAT/MAN/REC/GAIN/CERT-02..05/SHF-02/03/OPS-02/04/05/SAVE-05/TMR-04), UI ujian peserta, data-governance admin.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| WSE-06 (SAVE-01) | Nilai single-answer dihitung dari jawaban FINAL (tanpa baris dup/stale) | §Pattern 1 (dedupe-read in-memory), §Pattern 2 (SaveAnswer upsert harden). Verified: GradingService L96-97 `FirstOrDefault` tanpa ORDER BY; SubmitExam L1622-1624 `GroupBy.First()` tanpa OrderBy. |
| WSE-07 (STAT-01) | Abandoned/Cancelled tak bisa di-resurrect jadi Completed-lulus + cert | §Pattern 3 (guard expand). Verified: GradeAndCompleteAsync non-essay guard L239 `Status != "Completed"` (kurang Abandoned/Cancelled/PendingGrading); essay guard L203; SubmitExam L1545 hanya blok `=="Completed"`. |
| WSE-08 (STAT-02) | Completed/graded tak ketimpa AbandonExam telat | §Pattern 4 (guarded ExecuteUpdate). Verified: AbandonExam L1234 read-check-then-`SaveChangesAsync` L1244 = TOCTOU race (bukan atomic). |
| WSE-09 (TMR-01/02/03) | Timer Standard ditegakkan; saved answers tetap di-grade via auto-submit; token tak konsumsi pre-commit | §Pattern 5. Verified: `EnsureCanSubmitExamAsync` L4390-4395 allowlist Online/PreTest/PostTest → "Standard" tak match = dead code; TempData token one-shot L4413-4422 di-consume SEBELUM grading. |
| WSE-10 (TOK-02) | Token-required tak bisa di-bypass via SaveAnswer/SubmitExam | §Pattern 6 (StartedAt-gate). Verified: SaveAnswer L348-417 + SubmitExam L1531 TIDAK cek `IsTokenRequired`/`StartedAt`; gate cuma di StartExam lobby L929/L869. |
| WSE-11 (CERT-01) | Cert lulus ValidUntil=null konsisten "Aktif/Permanen" semua surface | §Pattern 7 (cross-surface map). Verified: `DeriveCertificateStatus` L58-59 return Expired untuk null; 5 consumer dipetakan (lihat tabel CERT-01). |
</phase_requirements>

## Summary

Phase 382 adalah **bug-fix correctness phase murni** di stack yang sudah mapan (ASP.NET Core 8 MVC + EF Core 8 + SQL Server). TIDAK ada library baru, TIDAK ada schema change (Migration=false per D-01). Seluruh pekerjaan = mengubah logika read/guard di **2 file inti** (`Controllers/CMPController.cs` + `Services/GradingService.cs`) plus **1 helper + 3 controller** untuk CERT-01 (`Models/CertificationManagementViewModel.cs`, `AdminBaseController.cs`, `CDPController.cs`, `RenewalController.cs`).

Temuan kritikal yang memengaruhi rencana: **(1) Densitas same-method sangat tinggi** — `GradeAndCompleteAsync` dimutasi oleh SAVE-01 (read) + STAT-01 (guard); `SubmitExam` dimutasi oleh SAVE-01 + STAT-01 + TMR + TOK-02 — semuanya method yang SAMA. Ini WAJIB satu phase koheren tanpa intra-phase parallel sub-agent (audit eksplisit). **(2) AbandonExam saat ini BUKAN blind UPDATE tanpa guard sama sekali** — ada read-check `if (Status != InProgress && != Open)` di L1234, tapi itu **TOCTOU race** (cek lalu `SaveChangesAsync` non-atomic via change-tracker). STAT-02 = ubah ke single atomic `ExecuteUpdate` ber-`Where`. **(3) CERT-01 surface lebih sederhana dari yang diduga** — `CertificateStatus.Permanent` enum SUDAH ada; HomeController badge/notif (L124, L215) sudah filter `ValidUntil.HasValue` (= null sudah TIDAK dihitung expired, konsisten dengan keputusan); titik yang BENAR-BENAR salah hanya `DeriveCertificateStatus` L58-59 + downstream POST-filter/tally yang konsumsi return value-nya.

**Primary recommendation:** Fix `DeriveCertificateStatus` sebagai single-source CERT-01 (null→Aktif/Permanen) — semua POST-filter & tally (AdminBase L200, RenewalController L217/277/300, CDP L3734/3793) yang konsumsi `Status==Expired||AkanExpired` otomatis ikut benar. Untuk SAVE-01, pakai **in-memory dedupe** (`GroupBy(...).OrderByDescending(SubmittedAt).First()`) karena kedua call-site SUDAH `.ToListAsync()` semua response per-session lebih dulu (row count per session kecil) — EF Core 8 TIDAK menerjemahkan GroupBy-select-entity ke SQL, jadi in-memory adalah satu-satunya pola benar di sini.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Grading skor final (SAVE-01) | API/Backend (`GradingService`) | API (`CMPController.SubmitExam`) | GradingService = otoritatif baca-DB (carry-forward v14.0/296); SubmitExam tulis answer dulu lalu delegasi |
| Lifecycle guard resurrection (STAT-01) | API/Backend | Database (status constraint via ExecuteUpdate WHERE) | Guard = WHERE-clause race-safe di DB layer, bukan in-memory check |
| Abandon vs graded race (STAT-02) | Database (atomic guarded UPDATE) | API (`AbandonExam` branch reject) | Race-safety HARUS di DB via single round-trip ExecuteUpdate, bukan read-then-write |
| Timer enforcement (TMR) | API/Backend (`EnsureCanSubmitExamAsync`) | Client (timer UI, non-authoritative) | Server-side adalah satu-satunya otoritas; client hint TIDAK dipercaya (D-06) |
| Token gate (TOK-02) | API/Backend (`SaveAnswer`/`SubmitExam`) | — | Gate `StartedAt != null` = proxy "sudah lewat lobby token" |
| Cert status semantics (CERT-01) | API/Backend (helper `DeriveCertificateStatus`) | API (badge/notif/renewal consumer) | Single helper = source of truth; consumer derive dari Status enum |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET / ASP.NET Core | 8.0 | Runtime + MVC | [VERIFIED: HcPortal.csproj `<TargetFramework>net8.0</TargetFramework>`] |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | ORM + ExecuteUpdate API | [VERIFIED: HcPortal.csproj] — `ExecuteUpdateAsync` (EF Core 7+) sudah dipakai luas di repo |
| xUnit | 2.9.3 | Unit/integration test | [VERIFIED: HcPortal.Tests.csproj] |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | Unit test DB | [VERIFIED: HcPortal.Tests.csproj] — catatan: InMemory TIDAK menegakkan unique index/race; integration race pakai SqlServer |
| Microsoft.EntityFrameworkCore.SqlServer (test) | 8.0.0 | Integration real-SQL | [VERIFIED: HcPortal.Tests.csproj] — pola ProtonCompletionFixture (Phase 365) untuk concurrent test |
| Playwright | (tests/e2e) | E2E acceptance | [VERIFIED: tests/e2e/ dengan helper examTypes.ts, dbSnapshot]; WAJIB `--workers=1` (DB isolation, [CITED: MEMORY reference_local_e2e_sql_env_fix]) |

**Installation:** Tidak ada paket baru. `dotnet build` + `dotnet test` (xUnit) + `npx playwright test --workers=1` (e2e).

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| In-memory dedupe GroupBy | SQL window-function (`ROW_NUMBER() OVER PARTITION`) via raw SQL atau LINQ `GroupBy` yang translatable | Window-function lebih efisien untuk dataset besar, TAPI per-session response count kecil + kedua call-site sudah `ToListAsync()` semua row → in-memory lebih sederhana & zero-risk translatability. Audit menamai ini Claude's discretion; **pilih in-memory.** [VERIFIED: GradingService L78-80, SubmitExam L1619-1621 keduanya sudah ToListAsync] |
| Filtered unique index | Denormalisasi kolom QuestionType di PackageUserResponse + backfill | Migration lebih besar, melanggar CLAUDE.md (dev tak promosi DB Dev/Prod) untuk single-answer correctness — REJECTED per D-01 |

## Architecture Patterns

### System Architecture Diagram

```
WORKER (browser, exam UI)
   │
   ├── POST /CMP/SaveAnswer (per radio change) ──────────┐
   │      [TOK-02 gate: StartedAt != null?] ── no ──> reject
   │      [lifecycle: Status open?] ── closed ──> reject
   │      └─> upsert PackageUserResponse (SubmittedAt=now)   ── may leave dup row under race (tolerated)
   │
   └── POST /CMP/SubmitExam ─────────────────────────────┐
          [Status guard: Completed/Abandoned/Cancelled/PendingGrading?] ── yes ──> reject (STAT-01)
          [TOK-02 gate: StartedAt != null?] ── no ──> reject
          [EnsureCanSubmitExamAsync] ── timer expired + no valid token ──> reject + audit (TMR-01)
          │      └─ token NOT consumed until grading committed (TMR-03)
          ├─> persist form answers (SaveChangesAsync)
          ├─> GradingService.GradeAndCompleteAsync(session)
          │      ├─ load ALL responses .ToListAsync()
          │      ├─ DEDUPE-READ: GroupBy(QId).OrderByDesc(SubmittedAt).First()  (SAVE-01)
          │      ├─ score from FINAL option
          │      └─ ExecuteUpdate WHERE Status NOT IN (Completed,Abandoned,Cancelled,PendingGrading)  (STAT-01)
          │           └─ rowsAffected==0 → race/resurrection blocked → return false
          └─> Results

HC/WORKER ── POST /CMP/AbandonExam ───────────────────────┐
          [ExecuteUpdate SET Status=Abandoned WHERE Status IN (InProgress,Open)]  (STAT-02)
          └─ rowsAffected==0 → reject "sesi sudah selesai" (graded verdict preserved)

CERT DASHBOARDS (read path)
   DeriveCertificateStatus(validUntil, certType)  ◄── single source (CERT-01)
       null → Aktif/Permanen (was Expired)
   │
   ├── AdminBaseController renewal POST-filter (keep Expired||AkanExpired) → null DROPPED from worklist
   ├── RenewalController tally (Expired/AkanExpired count) → null excluded
   ├── CDPController tally → null excluded
   └── HomeController badge L215 / notif L124 → already filter ValidUntil.HasValue (null already excluded — consistent)
```

### Pattern 1: In-memory dedupe-read FINAL answer (SAVE-01)
**What:** Sebelum scoring, reduce duplikat baris per `PackageQuestionId` ke baris dengan `SubmittedAt` terbaru.
**When to use:** Di `GradingService.GradeAndCompleteAsync` (ganti `FirstOrDefault` L96-97 & L151-152) dan `SubmitExam` (ganti `g.First()` L1622-1624).
**Example:**
```csharp
// Source: VERIFIED pattern — GradingService L78-80 & SubmitExam L1619-1624 already ToListAsync first.
// EF Core 8 does NOT translate GroupBy(...).Select(g => g.First()) of an entity to SQL —
// it must run client-side on the already-materialized list. That is exactly the shape here.

// FINAL answer per question (last-write-wins by SubmittedAt):
var finalByQuestion = allResponses
    .Where(r => r.PackageOptionId.HasValue)
    .GroupBy(r => r.PackageQuestionId)
    .ToDictionary(
        g => g.Key,
        g => g.OrderByDescending(r => r.SubmittedAt).First());

// MC scoring then reads finalByQuestion[q.Id] instead of allResponses.FirstOrDefault(...)
```
**Catatan koherensi:** SubmitExam L1622-1624 saat ini `GroupBy(...).ToDictionary(g => g.Key, g => g.First())` TANPA OrderBy — itu memilih baris arbitrer (insertion order DB), bukan final. Tambah `.OrderByDescending(r => r.SubmittedAt)` sebelum `.First()` di SATU tempat. GradingService L96-97 pakai `allResponses.FirstOrDefault(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)` — sama-sama tak ber-order → ganti ke lookup dari `finalByQuestion`. **Terapkan identik di kedua call-site** agar Score SubmitExam (untuk SignalR push) == Score GradingService (otoritatif).

### Pattern 2: SaveAnswer upsert harden (SAVE-01 write-side, best-effort)
**What:** Kurangi kemungkinan baris dup tanpa unique constraint. Saat ini L370-401 sudah: `ExecuteUpdate` (update if exists) → jika `updatedCount==0` insert → catch `DbUpdateException` retry-as-update. Catch tsebenarnya **dead code** (tak ada unique index lagi sejak migration 20260407070949), jadi dua insert konkuren bisa lolos.
**When to use:** `SaveAnswer` L370-401.
**Keputusan (Claude's discretion D-01):** Karena read-side sudah dedupe, write-side cukup best-effort. Opsi paling murah: bungkus update+insert dalam satu DB transaction + `ExecuteUpdate`-first (sudah ada). JANGAN tambah unique index (Migration=false). Dokumentasikan baris dup fisik sebagai hygiene debt minor yang TIDAK memengaruhi skor (read-side dedupe = mitigasi sebenarnya).
**Anti-pattern:** Jangan andalkan catch `DbUpdateException` L391 sebagai proteksi — itu tak akan pernah trigger tanpa unique constraint.

### Pattern 3: Lifecycle guard via ExecuteUpdate WHERE NOT IN (STAT-01)
**What:** Guard grading & submit agar status terminal/non-resurrectable tidak bisa di-Complete.
**When to use:** GradeAndCompleteAsync non-essay L238-239 + essay L202-203; SubmitExam early L1545.
**Example:**
```csharp
// Source: VERIFIED — GradingService L238-246 (non-essay) currently only `Status != "Completed"`.
// Expand to exclude resurrection-prone terminal states using AssessmentConstants (carry-forward v22.0).
using S = AssessmentConstants.AssessmentStatus;

var rowsAffected = await _context.AssessmentSessions
    .Where(s => s.Id == session.Id
        && s.Status != S.Completed
        && s.Status != S.Abandoned
        && s.Status != S.Cancelled
        && s.Status != S.PendingGrading)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Score, finalPercentage)
        .SetProperty(r => r.Status, S.Completed)
        // ... rest unchanged
    );
if (rowsAffected == 0) { /* race OR resurrection blocked */ return false; }
```
**SubmitExam early guard (L1545):** ganti `if (assessment.Status == "Completed")` menjadi cek set terminal + pesan BI + audit:
```csharp
if (assessment.Status == S.Completed || assessment.Status == S.Abandoned
    || assessment.Status == S.Cancelled || assessment.Status == S.PendingGrading)
{
    // audit "SubmitExamBlocked" (resurrection attempt) — try/catch swallow
    TempData["Error"] = "Sesi ujian ini sudah berakhir dan tidak dapat dikirim ulang.";
    return RedirectToAction("Assessment");
}
```
**Catatan konstanta:** [VERIFIED: AssessmentConstants.cs] `Completed="Completed"`, `Abandoned` (ada — string literal "Abandoned" dipakai langsung di AbandonExam L1241; **konfirmasi apakah ada const `Abandoned`** — grep menemukan Open/Upcoming/Completed/PendingGrading/InProgress/Cancelled di const; "Abandoned" dipakai sebagai literal L1234/1241. Plan: tambah `public const string Abandoned = "Abandoned";` bila belum ada, atau pakai literal konsisten). `PendingGrading="Menunggu Penilaian"`, `InProgress="InProgress"`, `Cancelled="Cancelled"`.

### Pattern 4: Guarded atomic AbandonExam (STAT-02)
**What:** Ubah TOCTOU read-check-then-save jadi single atomic guarded UPDATE.
**When to use:** AbandonExam L1233-1244.
**Example:**
```csharp
// Source: VERIFIED — current AbandonExam L1234 reads status, L1241-1244 mutates via change-tracker
// SaveChangesAsync (TOCTOU: another request can Complete between read and save).
using S = AssessmentConstants.AssessmentStatus;

var rowsAffected = await _context.AssessmentSessions
    .Where(a => a.Id == id && (a.Status == S.InProgress || a.Status == S.Open))
    .ExecuteUpdateAsync(a => a
        .SetProperty(x => x.Status, "Abandoned")
        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

if (rowsAffected == 0)
{
    TempData["Error"] = "Sesi ujian ini tidak dapat dibatalkan karena sudah selesai atau dinilai.";
    return RedirectToAction("Assessment");
}
```
**PENTING (ownership):** AbandonExam saat ini load entity untuk cek `UserId == user.Id` (L1230) — pertahankan ownership check SEBELUM ExecuteUpdate (ExecuteUpdate tak bisa enforce ownership tanpa WHERE UserId). Tambahkan `&& a.UserId == user.Id` ke WHERE ExecuteUpdate ATAU pertahankan early load+Forbid lalu ExecuteUpdate. Pilih: WHERE include UserId agar atomic+authorized dalam satu round-trip.

### Pattern 5: Timer enforcement allowlist inversion (TMR-01/02/03)
**What:** `EnsureCanSubmitExamAsync` L4390-4395 saat ini allowlist `Online/PreTest/PostTest` — "Standard" tak match → skip (dead code). Balik jadi blocklist (hanya skip Manual/null).
**Example:**
```csharp
// Source: VERIFIED — CMPController L4388-4395. AssessmentType "Standard" never matched the allowlist.
// Invert: skip ONLY for Manual/null; everything else (incl. Standard) enforced.
if (assessment.AssessmentType == AssessmentConstants.AssessmentType.Manual
    || string.IsNullOrEmpty(assessment.AssessmentType))
{
    return null; // skip guard
}
// Standard / Online / PreTest / PostTest → enforce timer below (existing tier-1/tier-2 logic)
```
**TMR-03 token-not-consumed-before-commit:** Saat ini token di-`TempData.Remove(tempKey)` L4422 SEBELUM grading commit. Jika grading gagal (DB hiccup) retry kehilangan token → permanent reject. Fix: jangan consume token di `EnsureCanSubmitExamAsync`; pindahkan consume ke SETELAH `GradeAndCompleteAsync` sukses, ATAU validasi tanpa remove dan remove hanya pada success path. **Hati-hati interaksi D-05:** "saved answers tetap di-grade via auto-submit on-time" — artinya path auto-submit (token valid) HARUS sampai grading; jangan reject on-time auto-submit. Verifikasi: `serverApprovedAutoSubmit==true` melewati Tier-1 (L4427) — itu benar; yang diubah hanya MOMENT konsumsi token.
**TMR-02 (D-06):** SubmitExam incomplete-gate L1561 `if (!isAutoSubmit && !serverTimerExpired)` masih percaya client `isAutoSubmit`. Hardening: gunakan `serverTimerExpired` (server-computed L1554-1558) sebagai sumber utama; `isAutoSubmit` client hint hanya tambahan, jangan satu-satunya jalan lolos gate.

### Pattern 6: StartedAt token-gate (TOK-02)
**What:** Cegah bypass token dengan langsung POST SaveAnswer/SubmitExam tanpa lewat StartExam lobby (yang set StartedAt + validasi token).
**When to use:** SaveAnswer L362-368 (setelah ownership check) + SubmitExam L1539 (setelah ownership).
**Example:**
```csharp
// Source: VERIFIED — StartExam lobby sets StartedAt only after token verify (L929 guards StartedAt==null
// transition; VerifyToken L869). SaveAnswer/SubmitExam currently never check StartedAt or IsTokenRequired.
if (session.IsTokenRequired && session.StartedAt == null)
    return Json(new { success = false, error = "Ujian belum dimulai. Masukkan token melalui halaman ujian." });
// (SubmitExam: TempData["Error"] + RedirectToAction("Assessment") instead of Json)
```
**KOHERENSI (audit risk):** TOK-02 mengedit SaveAnswer L363-367 & SubmitExam L1539 — EXACT method yang juga diubah STAT-01/SAVE-01/TMR. **Implementasi SEKALI di handler yang sama**, urutan commit: STAT-01 guard → TOK-02 gate (keduanya di awal handler, sebelum mutasi). JANGAN split ke sub-agent paralel.

### Pattern 7: CERT-01 single-source semantics (CERT-01)
**What:** `DeriveCertificateStatus(null, non-Permanent)` return non-Expired; semua consumer ikut otomatis via Status enum.
**Cross-surface consumer map** (VERIFIED):

| Consumer | Lokasi | Konsumsi | Efek setelah fix helper |
|----------|--------|----------|--------------------------|
| `DeriveCertificateStatus` | `CertificationManagementViewModel.cs:53-65` (L58-59) | SOURCE | null → return `Aktif` (atau `Permanent`) bukan `Expired` |
| AdminBase renewal POST-filter | `AdminBaseController.cs:200` | `Status==Expired\|\|AkanExpired` | cert null otomatis DROP dari worklist renewal (sesuai D-08) |
| Renewal tally | `RenewalController.cs:217,277,300,351` | `Count(Status==Expired/AkanExpired)` | null tak terhitung (benar) |
| Renewal ordering | `RenewalController.cs:262,288,338` | `Status==Expired ? 0 : 1` | null tak naik ke atas |
| CDP tally | `CDPController.cs:3734,3793` | `Count(Status==Expired/AkanExpired && !IsRenewed)` | null tak terhitung |
| CDP/CMP row build | `CDPController.cs:4069`, AdminBase L187 | `DeriveCertificateStatus(a.ValidUntil, null)` | null → Aktif |
| HomeController badge | `HomeController.cs:214-220` (L215) | raw query `ValidUntil.HasValue` | SUDAH exclude null (konsisten — tak perlu ubah, tapi verifikasi tak ada drift) |
| HomeController notif | `HomeController.cs:121-126` (L124) | raw query `ValidUntil.HasValue` | SUDAH exclude null (konsisten) |

**Keputusan return value:** Enum `CertificateStatus.Permanent` SUDAH ada [VERIFIED: enum dipakai test L23/42]. Untuk cert lulus ValidUntil=null non-Permanent-type, pilih return `Aktif` ATAU `Permanent`. Rekomendasi: **`Aktif`** (semantik "berlaku tanpa kedaluwarsa" tanpa mengklaim certificateType="Permanent" yang punya makna admin terpisah). Konfirmasi di plan; pastikan label UI BI = "Aktif" / "Permanen" konsisten. JANGAN setengah-setengah (audit warning: helper berhenti return Expired tapi consumer tak update → drop silent — di sini POST-filter konsumsi Status enum jadi otomatis koheren, INI keuntungan single-source).

### Anti-Patterns to Avoid
- **Split same-method across sub-agents:** GradeAndCompleteAsync & SubmitExam = multi-REQ. JANGAN parallel sub-agent (audit eksplisit). Satu phase koheren, urutan commit terdefinisi.
- **Tambah unique index / migration:** Melanggar D-01 (Migration=false). Verifikasi `dotnet ef migrations add` TIDAK menghasilkan snapshot-diff setelah perubahan (perubahan murni logika, bukan model).
- **Dedupe via SQL GroupBy translatable:** EF Core 8 tak menerjemahkan `GroupBy().Select(g => g.First())` entity → akan throw atau client-eval implisit. Pakai in-memory pada list yang sudah di-`ToListAsync()`.
- **Consume auto-submit token sebelum grading commit (TMR-03):** retry pasca-DB-hiccup jadi permanent reject. Consume HANYA pada success.
- **Read-then-write status guard (STAT-02):** TOCTOU. Pakai atomic ExecuteUpdate WHERE.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Race-safe status transition | Manual lock / read-check-save | EF Core `ExecuteUpdateAsync` + `Where(status guard)` + `rowsAffected` branch | Sudah pola di repo (GradingService L238); single atomic round-trip, no TOCTOU |
| Status string literal | Hardcode "Completed"/"Abandoned" | `AssessmentConstants.AssessmentStatus.*` | Single-source label lintas surface (carry-forward v22.0) |
| Cert number generation | Custom sequence | `CertNumberHelper.GetNextSeqAsync` + retry (sudah ada L282-294) | Out of scope 382; jangan sentuh |
| Audit log reject | New table | Pola `WriteSubmitBlockedAuditAsync` L4450 (try/catch swallow) | Sudah ada; reuse untuk STAT reject + SubmitExamBlocked |

**Key insight:** Hampir semua infrastruktur (ExecuteUpdate guard, audit helper, status constants, GradingService otoritatif) SUDAH ada di repo dari phase sebelumnya. Phase 382 = memperluas guard yang kurang lengkap + menambah dedupe-read + membalik allowlist timer, BUKAN membangun mekanisme baru.

## Common Pitfalls

### Pitfall 1: Score divergen SubmitExam vs GradingService
**What goes wrong:** SubmitExam menghitung `finalPercentage` sendiri (untuk SignalR push L1674) dari form POST, lalu GradingService menghitung ulang dari DB. Jika dedupe-read hanya diterapkan di salah satu, push score ≠ stored score.
**Why it happens:** Dua path scoring paralel (L1613-1674 form-based, GradingService L82-134 DB-based).
**How to avoid:** Terapkan dedupe-read identik di KEDUA tempat; pastikan SubmitExam membaca FINAL DB answer (bukan hanya form `answers` dict) untuk MC supaya konsisten dengan GradingService yang otoritatif. Tes #10 SAVE-01 concurrent harus assert Score akhir = opsi final.
**Warning signs:** SignalR menampilkan score berbeda dari halaman Results.

### Pitfall 2: ExecuteUpdate kehilangan ownership check (STAT-02)
**What goes wrong:** Mengganti AbandonExam read+SaveChanges dengan ExecuteUpdate tapi lupa `UserId` di WHERE → worker lain bisa abandon sesi orang.
**How to avoid:** Sertakan `&& a.UserId == user.Id` di WHERE ExecuteUpdate (atomic + authorized). Tes ownership negatif.

### Pitfall 3: Token one-shot habis sebelum commit (TMR-03)
**What goes wrong:** `TempData.Remove` token sebelum `GradeAndCompleteAsync`; jika grading throw, retry tanpa token → permanent reject (DEGRADED untuk PreTest/PostTest).
**How to avoid:** Validasi token tanpa langsung remove; remove hanya pada grading success. Pertimbangkan idempotency: grading sudah race-safe (rowsAffected==0 = already done), jadi retry aman.
**Warning signs:** Worker terjebak "waktu habis" loop saat auto-submit retry.

### Pitfall 4: CERT-01 test break tidak di-rewrite
**What goes wrong:** `DeriveCertificateStatus_NullValidUntil_NonPermanent_ReturnsExpired` (CertificateStatusTests.cs:31-36) akan FAIL setelah fix.
**How to avoid:** Rewrite assertion ke keputusan baru (`Assert.Equal(CertificateStatus.Aktif, result)` atau `.Permanent`). [VERIFIED: test ada di L31-36]. Pertahankan test `_Permanent_ReturnsPermanent` (L38-43) apa adanya.

### Pitfall 5: Migration tak sengaja ter-scaffold (Migration=false guard)
**What goes wrong:** Perubahan model atau anotasi memicu snapshot-diff → migration baru → melanggar D-01-IMPACT (v29.0 = 0 migration).
**How to avoid:** Setelah implementasi, jalankan `dotnet ef migrations add _verify_382 --no-build` (atau cek `dotnet ef migrations list` + model snapshot) — pastikan TIDAK ada perubahan model. Hapus migration verifikasi. Update ROADMAP Phase 382 `Migration: false`.

## Code Examples

Lihat §Architecture Patterns 1-7 — semua contoh diturunkan dari source code repo yang sudah di-VERIFIED (file:line tercantum), bukan dari dokumentasi eksternal.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `GradeFromSavedAnswers` di controller | `GradingService` otoritatif baca DB | v14.0/296 | SAVE-01 read-final ikut pola single-source |
| Status string literal tersebar | `AssessmentConstants.AssessmentStatus.*` | v22.0 | Guard STAT-01 pakai konstanta |
| Read-check-then-SaveChanges | `ExecuteUpdateAsync` + WHERE guard + rowsAffected | EF Core 7+ (sudah dipakai GradingService) | STAT-02 ikut pola race-safe yang sudah ada |

**Deprecated/outdated:**
- Catch `DbUpdateException` di SaveAnswer L391-400 = dead code sejak migration `20260407070949_RemoveUniqueIndexOnPackageUserResponse` men-drop unique index. Jangan andalkan; read-side dedupe adalah mitigasi sebenarnya.

## Runtime State Inventory

> Phase 382 = perubahan kode/logika murni, BUKAN rename/refactor/migration. Section ini di-skip kecuali satu catatan data-existing di bawah.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | Baris dup `PackageUserResponse` yang mungkin SUDAH ada di DB Dev/Prod dari race historis (pra-fix) | None — read-side dedupe menangani existing dup tanpa data migration. Tidak ada cleanup wajib (hygiene debt, tak pengaruh skor). Developer TIDAK sentuh DB Dev/Prod (CLAUDE.md). |
| Live service config | None — verified: tak ada config eksternal terkait grading/cert | None |
| OS-registered state | None | None |
| Secrets/env vars | None | None |
| Build artifacts | None — verified: no package rename, no model change | None |

## Common Pitfalls — File Overlap & Sequencing (dari audit)

- **`[soft] Controllers/CMPController.cs`** ← Phase 381 + 382. **EXECUTE SERI** setelah 381 landing; jangan rebase paralel.
- **`[none]`** GradingService.cs / CertificationManagementViewModel.cs / AdminBaseController.cs / CDPController.cs / RenewalController.cs ← 382 only.
- **Urutan commit dalam phase (audit-recommended):** SAVE-01 read-final → STAT-01 guard → STAT-02 → TMR-01/02/03 → TOK-02 → CERT-01 (helper + 4 consumer). TIDAK ada intra-phase parallel sub-agent pada CMPController/GradingService.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (HcPortal.Tests/) + Playwright (tests/e2e/) |
| Config file | HcPortal.Tests/HcPortal.Tests.csproj; playwright config di tests/e2e |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| Full suite command | `dotnet test` + `npx playwright test --workers=1` (DB isolation WAJIB, [CITED: MEMORY reference_local_e2e_sql_env_fix]) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| WSE-06 (SAVE-01) | 2 SaveAnswer beda opsi → 1 baris final → Score dari opsi FINAL | integration (real SqlServer — InMemory tak race) | `dotnet test --filter "FullyQualifiedName~SaveAnswerConcurrent"` | ❌ Wave 0 (baru) |
| WSE-06 (SAVE-01) | GradingService dedupe-read pilih SubmittedAt terbaru | unit | `dotnet test --filter "FullyQualifiedName~GradingDedupe"` | ❌ Wave 0 |
| WSE-07 (STAT-01) | Submit pada sesi Abandoned/Cancelled/PendingGrading → reject, tak jadi Completed | unit/integration | `dotnet test --filter "FullyQualifiedName~SubmitResurrection"` | ❌ Wave 0 |
| WSE-08 (STAT-02) | AbandonExam pada sesi Completed → rowsAffected==0, status tetap Completed | integration | `dotnet test --filter "FullyQualifiedName~AbandonGuard"` | ❌ Wave 0 |
| WSE-09 (TMR-01) | Standard elapsed>allowed tanpa token → ditolak + audit SubmitExamBlocked; on-time diterima | unit | `dotnet test --filter "FullyQualifiedName~EnsureCanSubmitStandard"` | ❌ Wave 0 |
| WSE-09 (TMR-03) | Token tak dikonsumsi sebelum grading commit (retry aman) | unit | `dotnet test --filter "FullyQualifiedName~AutoSubmitTokenRetry"` | ❌ Wave 0 |
| WSE-10 (TOK-02) | SaveAnswer/SubmitExam token-required + StartedAt==null → reject | unit/integration | `dotnet test --filter "FullyQualifiedName~TokenGateSaveSubmit"` | ❌ Wave 0 |
| WSE-11 (CERT-01) | `DeriveCertificateStatus(null,null)` → Aktif/Permanen | unit | `dotnet test --filter "FullyQualifiedName~CertificateStatus"` | ✅ exists (REWRITE L31-36) — CertificateStatusTests.cs |
| WSE-11 (CERT-01) | Badge+notif+renewal tally konsisten untuk cert null | unit/integration | `dotnet test --filter "FullyQualifiedName~CertAlertConsistency"` | ❌ Wave 0 |

### E2E Acceptance (audit scenarios #8-12 = spec acceptance)
| Scenario | REQ | Assertion inti | File |
|----------|-----|----------------|------|
| #8 anti-resurrection | STAT-01 | Abandon→SubmitExam ditolak, tak Completed/cert; Cancelled idem | exam-taking.spec.ts (extend) |
| #9 abandon tak menimpa | STAT-02 | Completed→AbandonExam rowsAffected==0, verdict+cert tetap di Results/Records | exam-taking.spec.ts (extend) |
| #10 concurrent save | SAVE-01 | 2 SaveAnswer beda opsi → 1 baris final → Score benar | integration (preferred) atau exam-taking.spec.ts |
| #11 timer Standard | TMR-01 | StartedAt mundur (seed) → submit manual ditolak + audit; on-time diterima | exam-taking.spec.ts (extend) |
| #12 cert visibility | CERT-01 | lulus ValidUntil=null → Results LULUS+PDF + dashboard Aktif/Permanen + badge/notif konsisten | exam-taking.spec.ts + dashboard assert |
| #4 PrePost same-day | (post-382 acceptance) | butuh grading 382 + entry 381 — Wave acceptance, BUKAN gate | exam-taking.spec.ts |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (xUnit < 30s)
- **Per wave merge:** full xUnit + `npx playwright test --workers=1` e2e relevan
- **Phase gate:** full suite green + verifikasi `dotnet ef migrations` TIDAK scaffold migration baru (Migration=false guard) sebelum `/gsd-verify-work`

### Wave 0 Gaps
- [ ] Integration fixture real-SqlServer untuk concurrent SaveAnswer (pola ProtonCompletionFixture Phase 365) — covers WSE-06
- [ ] Unit `GradingDedupeTests` — covers WSE-06 read-final
- [ ] Unit/integration `SubmitResurrectionTests` — covers WSE-07
- [ ] Integration `AbandonGuardTests` — covers WSE-08
- [ ] Unit `EnsureCanSubmitStandardTests` + `AutoSubmitTokenRetryTests` — covers WSE-09
- [ ] Unit/integration `TokenGateTests` — covers WSE-10
- [ ] REWRITE `CertificateStatusTests.cs:31-36` + new `CertAlertConsistencyTests` — covers WSE-11
- [ ] Extend `tests/e2e/exam-taking.spec.ts` dengan scenario #8-12 (helper examTypes.ts + dbSnapshot sudah ada)

*(Framework sudah terpasang — tidak perlu install.)*

## Security Domain

> `security_enforcement` di-handle oleh `/gsd-secure-phase` terpisah. Ringkasan ASVS relevan untuk phase ini:

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V4 Access Control | yes | AbandonExam/SubmitExam ownership check (`UserId == user.Id`) — JANGAN hilang saat refactor ke ExecuteUpdate (Pitfall 2); TOK-02 = enforce token gate (mencegah owner bypass proctoring) |
| V5 Input Validation | yes | SubmitExam tak percaya client `isAutoSubmit` (D-06 TMR-02) — server-side serverTimerExpired otoritatif |
| V7 Error/Logging | yes | Audit `SubmitExamBlocked` + STAT reject (try/catch swallow, jangan block primary action) |
| V11 Business Logic | yes | Anti-resurrection (STAT-01), anti-overwrite-graded (STAT-02), timer enforcement (TMR-01) = business-logic integrity controls |

### Known Threat Patterns
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Resurrect Abandoned→Completed-lulus via crafted POST | Tampering / Elevation | STAT-01 guard ExecuteUpdate WHERE NOT IN terminal states |
| Overwrite graded verdict via late AbandonExam | Tampering | STAT-02 atomic guarded UPDATE + rowsAffected==0 reject |
| Bypass timer via spoofed isAutoSubmit | Tampering | TMR-01 server enforce + token-based (D-06) |
| Bypass token gate via direct SaveAnswer/SubmitExam | Elevation (proctoring bypass) | TOK-02 StartedAt-gate |
| Abandon other worker's session via ExecuteUpdate sans ownership | Spoofing | WHERE include UserId (Pitfall 2) |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | EF Core 8 TIDAK menerjemahkan `GroupBy(...).Select(g => g.OrderByDescending(...).First())` entity ke SQL → harus in-memory | Pattern 1 | LOW — kedua call-site sudah `.ToListAsync()` semua row dulu, jadi in-memory aman terlepas dari translatability. Jika ternyata translatable, in-memory tetap benar (hanya kurang optimal, irrelevan untuk row count kecil per session). |
| A2 | `AssessmentConstants.AssessmentStatus` mungkin BELUM punya const `Abandoned` (literal "Abandoned" dipakai langsung) | Pattern 3 | LOW — plan tambah const atau pakai literal konsisten; grep menemukan const untuk Open/Upcoming/Completed/PendingGrading/InProgress/Cancelled tapi tidak Abandoned. Konfirmasi saat plan. |
| A3 | Return value CERT-01 untuk null sebaiknya `CertificateStatus.Aktif` (bukan `.Permanent`) | Pattern 7 | LOW — Claude's discretion + locked "Permanen/Aktif"; label UI BI konsisten yang penting. Konfirmasi dengan planner/user jika perlu membedakan Aktif vs Permanen di tampilan. |
| A4 | Baris dup `PackageUserResponse` existing di DB tak perlu cleanup (read-side dedupe cukup) | Runtime State Inventory | LOW — sesuai D-01 (hygiene debt minor, tak pengaruh skor). Developer tak sentuh DB Dev/Prod per CLAUDE.md. |

## Open Questions

1. **Const `Abandoned` di AssessmentConstants**
   - What we know: const ada untuk Open/Upcoming/Completed/PendingGrading/InProgress/Cancelled; "Abandoned" dipakai sebagai literal di AbandonExam.
   - What's unclear: apakah perlu tambah const baru atau pakai literal.
   - Recommendation: tambah `public const string Abandoned = "Abandoned";` untuk konsistensi guard STAT-01/02 (single-source label v22.0 discipline).

2. **CERT-01 return: `Aktif` vs `Permanent`**
   - What we know: enum `Permanent` sudah ada; keputusan locked = "Permanen/Aktif".
   - What's unclear: apakah dashboard membedakan label/warna "Aktif" vs "Permanen".
   - Recommendation: return `Aktif` (semantik "berlaku tanpa kedaluwarsa" tanpa mengubah makna admin `certificateType="Permanent"`). Plan boleh override jika UI butuh label "Permanen" eksplisit.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8 | build/test | ✓ (asumsi, repo net8.0) | 8.0 | — |
| SQL Server (lokal) | integration race test + DB lokal | ✓ (SQLEXPRESS HcPortalDB_Dev, [CITED: MEMORY]) | — | InMemory untuk non-race unit |
| Playwright + Chromium bundled | e2e | ✓ ([CITED: MEMORY — pakai bundled chromium --headed]) | — | xUnit integration sebagai gate utama |

**Catatan e2e:** AD lokal WAJIB `Authentication__UseActiveDirectory=false dotnet run`; combined Playwright WAJIB `--workers=1` (DB isolation); SQLBrowser + `lpc:` shared-memory conn override untuk login 500 ([CITED: MEMORY reference_local_e2e_sql_env_fix]).

## Sources

### Primary (HIGH confidence — verified terhadap source code)
- `Controllers/CMPController.cs` — SaveAnswer L345-417, AbandonExam L1217-1248, SubmitExam L1523-1724, EnsureCanSubmitExamAsync L4382-4444, WriteSubmitBlockedAuditAsync L4450+, StartExam token gate L869/L929
- `Services/GradingService.cs` — GradeAndCompleteAsync L56-318 (guard L202-211 essay, L238-246 non-essay; FirstOrDefault read L96-97/L151-152; cert L269-301)
- `Models/CertificationManagementViewModel.cs` — DeriveCertificateStatus L53-65
- `Controllers/HomeController.cs` — notif L121-126, badge L214-220
- `Controllers/AdminBaseController.cs` — renewal POST-filter L187/L200
- `Controllers/CDPController.cs` — tally L3734/3793, row build L4069
- `Controllers/RenewalController.cs` — tally L217/277/300/351, ordering L262/288/338
- `Models/AssessmentConstants.cs` — status const L15-20
- `HcPortal.Tests/CertificateStatusTests.cs` — test L31-43 (rewrite target)
- `HcPortal.csproj` / `HcPortal.Tests.csproj` — net8.0, EF Core 8.0.0, xUnit 2.9.3
- `docs/assessment-audit/2026-06-14-E2E-worker-success-FOCUS.md` — fix detail + E2E test plan #8-12 + §Risiko/miss + §File overlap matrix
- `.planning/phases/382-grading-lifecycle-cert/382-CONTEXT.md` — locked decisions D-01..D-08

### Secondary (MEDIUM — project memory)
- MEMORY: reference_local_e2e_sql_env_fix (Playwright --workers=1, lpc: conn), project_365_shipped (ProtonCompletionFixture real-SQL pattern), project_v14/296 (GradingService single-source)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — diverifikasi langsung dari .csproj
- Architecture/patterns: HIGH — semua pattern diturunkan dari source code file:line yang dibaca, bukan asumsi
- Pitfalls: HIGH — race/TOCTOU/dead-code dikonfirmasi dari kode aktual
- CERT-01 cross-surface map: HIGH — 8 consumer di-grep + dibaca; single-source helper = propagasi otomatis terverifikasi

**Research date:** 2026-06-14
**Valid until:** stabil — bug-fix correctness, tak ada dependency fast-moving. Re-verify jika Phase 381 mengubah CMPController StartExam/SubmitExam region sebelum 382 dieksekusi (soft overlap).
