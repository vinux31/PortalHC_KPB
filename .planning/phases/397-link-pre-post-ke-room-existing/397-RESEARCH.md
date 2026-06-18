# Phase 397: Link Pre/Post ke Room Existing - Research

**Researched:** 2026-06-18
**Domain:** ASP.NET Core MVC (net8.0) + EF Core 8 — Pre/Post test grouping wiring, brownfield extend of inject wizard
**Confidence:** HIGH (semua klaim diverifikasi vs live code, file:line dikutip)

## Summary

Phase 397 menambahkan kemampuan HC untuk menautkan sesi inject Pre/Post ke assessment room existing lewat search-picker modal, mendukung skenario silang inject↔online dan inject↔inject. Mekanik penautan adalah **mengisi `LinkedGroupId` (level-room) + `LinkedSessionId` (sibling per-pekerja)** pada `AssessmentSession` — kolom-kolom ini **sudah ada** dan `InjectRequest`/`InjectAssessmentService` sudah menerima keduanya, sehingga **0 migration** [VERIFIED: Models/AssessmentSession.cs:172,178; Models/InjectAssessmentDtos.cs:63-64].

Temuan kunci yang mengarahkan seluruh desain: **display memasangkan Pre↔Post by `LinkedGroupId` + `UserId`, BUKAN `LinkedSessionId`** [VERIFIED: CMPController.cs:3415-3433 `GetGainScoreData` query `LinkedGroupId == assessmentGroupId` lalu match `postSessionDict.TryGetValue(pre.UserId)`; CMPController.cs:264-267 `Results` pair by `LinkedGroupId`]. Jadi yang **wajib benar** untuk tampil-berpasangan = `LinkedGroupId`. `LinkedSessionId` adalah fidelitas "seakan online" (D-02), bukan penentu tampilan. Service saat ini **broadcast 1 nilai `LinkedSessionId`** ke semua sesi [VERIFIED: InjectAssessmentService.cs:120 `LinkedSessionId = req.LinkedSessionId`] — ini HARUS diubah menjadi resolusi per-pekerja by-UserId untuk D-02.

Template wiring sudah ada dan teruji: `CreateAssessment` Pre/Post 3-fase [VERIFIED: AssessmentAdminController.cs:1307-1314 — `pre.LinkedGroupId=linkedGroupId; pre.LinkedSessionId=post.Id; post.LinkedSessionId=pre.Id`; `linkedGroupId=preSessions[0].Id` :1270]. Ini adalah peta eksak untuk D-01/D-02. Picker query `ManageAssessmentTab_Assessment` mengembalikan **PartialView (HTML), BUKAN JSON** [VERIFIED: AssessmentAdminController.cs:245 `return PartialView("Shared/_AssessmentGroupsTab", null)`] → modal picker (D-05) memerlukan **endpoint JSON baru yang ringan** (rekomendasi), bukan reuse langsung.

**Primary recommendation:** Tambah endpoint JSON `SearchLinkTargets` baru di `InjectAssessmentController` (filter tipe-lawan, bawa `RepresentativeId`/`LinkedGroupId`/`IsPrePostGroup`/workerCount/`IsManualEntry`). Ubah `InjectBatchAsync` agar menerima **peta sibling per-UserId** + flag Kasus B; resolve `LinkedSessionId` per-pekerja & write-back ke sesi sibling, dan tulis `LinkedGroupId` ke SEMUA sesi room target bila standalone — semuanya dalam transaksi yang sama, dengan AuditLog `"LinkPrePost"` terpisah per sesi online yang dimutasi. Tambah endpoint `UnlinkInjectGroup` (D-12) yang mirror pola atomic+audit `DeleteAssessmentGroup`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Search picker target rooms | API/Backend (`InjectAssessmentController` JSON action) | Browser (modal fetch+debounce) | Query EF + filter tipe-lawan otoritatif server; modal hanya render |
| Resolve sibling per-pekerja by-UserId | API/Backend (`InjectAssessmentService`) | — | Logika integritas data; tak boleh client-trust |
| Write `LinkedGroupId`/`LinkedSessionId` (inject + online) | Database/Backend (EF, dalam tx `InjectBatchAsync`) | — | Atomicity + write-to-online (D-01 Kasus B) |
| Anti-double-link preflight (D-08) | API/Backend (preflight, pola PreflightValidateAsync) | Browser (render daftar error) | Query DB by (UserId, LinkedGroupId, AssessmentType) |
| Preview pairing summary (D-07) | API/Backend (`PreviewInjectScore` extend, dry-run) | Browser (render `#previewPairingSummary`) | Hitung pair/unpair tanpa write |
| Audit "LinkPrePost" (D-09) | Database/Backend (in-tx `_context.AuditLogs.Add`) | — | Jejak compliance per sesi online dimutasi |
| Chip + modal + tombol Cari Room | Browser (vanilla JS + Bootstrap modal) | Frontend Server (Razor render statis) | UI runtime — WAJIB Playwright (lesson 354) |
| Unlink/revert (D-12) | API/Backend (endpoint baru) + Database | Browser (modal konfirmasi) | Revert atomik + audit reverse |

## Standard Stack

Brownfield — TIDAK menambah dependency. Semua sudah terpasang.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller + Razor view | Stack project [VERIFIED: HcPortal.csproj `<TargetFramework>net8.0</TargetFramework>`] |
| EF Core (SqlServer) | 8.0.0 | Persist `LinkedGroupId`/`LinkedSessionId`, query target rooms | Stack project [VERIFIED: HcPortal.csproj `Microsoft.EntityFrameworkCore.SqlServer 8.0.0`] |
| Bootstrap | 5.3.0 (CDN) | Modal picker, chip, badge, alert | Sistem desain mapan 394/395/396 [CITED: 397-UI-SPEC.md §Design System] |
| Bootstrap Icons | 1.10.0 | bi-search, bi-link-45deg, dll | [CITED: 397-UI-SPEC.md §Design System] |
| Vanilla JS | — | Modal toggle, debounce search, chip state | Tak ada SPA framework (Razor + JS) [CITED: 397-UI-SPEC.md §Design System] |

### Supporting (existing assets di-reuse — JANGAN duplikasi)
| Asset | File:line | Purpose | When to Use |
|-------|-----------|---------|-------------|
| Cross-link template Pre/Post | AssessmentAdminController.cs:1307-1314 | Peta eksak bidirectional `LinkedGroupId`+`LinkedSessionId` | Pola untuk D-01/D-02 wiring |
| `linkedGroupId = preSessions[0].Id` | AssessmentAdminController.cs:1270 | Konvensi nilai LinkedGroupId baru online | Konfirmasi nilai Kasus B (gunakan `RepresentativeId` target) |
| `ManageAssessmentTab_Assessment` projection | AssessmentAdminController.cs:141-197 | Sumber field picker (Title/Category/Schedule/AssessmentType/LinkedGroupId/RepresentativeId/UserCount/IsPrePostGroup) | Salin SELECT shape ke endpoint JSON baru |
| `InjectBatchAsync` tx atomic | InjectAssessmentService.cs:42-334 | Host write link (extend, JANGAN fork) | Semua write link dalam tx ini |
| `PreviewInjectScore` dry-run | InjectAssessmentController.cs:106-156 | Engine preview (extend untuk ringkasan pairing D-07) | Hitung pair/unpair tanpa write |
| `AssessmentScoreAggregator.Compute` | Helpers/AssessmentScoreAggregator.cs | Engine skor pure EF-free (preview==commit) | Sudah dipakai preview; pairing summary tak butuh ini |
| `AuditLogService.LogAsync` / `_context.AuditLogs.Add` | Services/AuditLogService.cs:21-42; InjectAssessmentService.cs:297-307 | Tulis audit | `"LinkPrePost"` per sesi online dimutasi |
| `DeleteAssessmentGroup` atomic+audit | AssessmentAdminController.cs:2398-2535 | Pola atomic delete + audit + partial-failure guard | Mirror untuk Unlink (D-12) |
| `SiblingSessionQuery.SiblingPrePostAwarePredicate` | Helpers/SiblingSessionQuery.cs:14-24 | Predikat type-aware sibling | Referensi (BUKAN dipakai langsung — beda kunci) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Endpoint JSON baru `SearchLinkTargets` | Reuse `ManageAssessmentTab_Assessment` langsung | DITOLAK: aksi itu return PartialView HTML (:245), bukan JSON; parsing HTML di modal rapuh. Endpoint JSON ringan = lebih bersih + bisa filter tipe-lawan server-side |
| `RepresentativeId` target sbg `LinkedGroupId` baru (Kasus B) | int sintetis baru | Ikuti konvensi online (`preSessions[0].Id` :1270 = id sesi nyata representatif). `RepresentativeId` = id sesi PreTest representatif room target (sesi nyata) → konsisten level-room |
| Auto-detect by judul (`TryAutoDetectCounterpartGroup` :7301) | Picker eksplisit | DITOLAK D-05: user pilih picker. Auto-detect boleh jadi hint, bukan jalur utama |

**Installation:** Tidak ada. `0 migration`, tidak ada paket baru.

**Version verification:** [VERIFIED: Bash `dotnet --version` = 8.0.418; node v24.14.0; Playwright 1.58.2; HcPortal.csproj EF Core 8.0.0]. Tidak ada paket baru yang perlu di-`npm view`/`dotnet add`.

## Architecture Patterns

### System Architecture Diagram

```
HC di Step-1 wizard (AssessmentType=Pre/Post)
        │
        │ klik "Cari Room Pasangan"  ──────────────────────────────┐
        ▼                                                            │
[Modal #roomPickerModal] ── search (debounce 300ms) ──► GET /Admin/SearchLinkTargets?term=&type=PostTest
        │                                                            │  (NEW JSON action, filter tipe-LAWAN)
        │ ◄── JSON: [{ RepresentativeId, Title, Category, Schedule,  │
        │            AssessmentType, LinkedGroupId(null?), UserCount,│
        │            IsPrePostGroup, IsManualEntry }]                │
        │                                                            │
        │ klik baris room ──► set chip + hidden(#LinkedTargetRepId, #LinkedCaseFlag)
        ▼
[Step-5/6 Pratinjau] ── POST /Admin/PreviewInjectScore (extend D-07)
        │                  + lookup target group sessions by UserId (dry-run, NO write)
        │ ◄── { skor..., pairing: {paired, unpaired, willTouchOnline, dateWarn, doubleLinkNips[]} }
        ▼
[klik #btnInject] ── POST /Admin/InjectAssessment
        │                  MapToRequest: isi req.LinkTargetRepId + req.LinkCaseB + per-worker sibling map
        ▼
InjectAssessmentService.InjectBatchAsync (1 transaksi)
        │
        ├─ preflight (existing) + anti-double-link preflight (NEW, by UserId+LinkedGroupId+type)
        ├─ resolve LinkedGroupId target (Kasus A: target.LinkedGroupId; Kasus B: target.RepresentativeId)
        ├─ per sesi inject: set LinkedGroupId + LinkedSessionId(sibling.Id by UserId)
        ├─ write-back: sibling.LinkedSessionId = inject.Id   (online maupun inject)
        ├─ Kasus B: tulis LinkedGroupId ke SEMUA sesi room target (online) — skor/jawaban/status UTUH
        ├─ AuditLog "ManualInject" (per sesi inject) + "LinkPrePost" (per sesi ONLINE dimutasi)
        └─ COMMIT atau ROLLBACK total
        ▼
Records/CMP/Results & GetGainScoreData ── pair by LinkedGroupId + UserId (UTUH, silang inject↔online)

[Pasca-commit] Unlink: POST /Admin/UnlinkInjectGroup (D-12)
        └─ revert LinkedSessionId bidirectional + (Kasus B kosong-sebelah) revert LinkedGroupId online
           atomic + AuditLog "LinkPrePostUndo"
```

### Recommended Project Structure (file yang disentuh — TIDAK ada file/folder baru wajib)
```
Controllers/InjectAssessmentController.cs   # + SearchLinkTargets (JSON), + UnlinkInjectGroup, extend MapToRequest + PreviewInjectScore
Services/InjectAssessmentService.cs         # ubah broadcast :120 → per-UserId; + write-to-online Kasus B; + anti-double preflight; + UnlinkAsync
Models/InjectAssessmentDtos.cs              # extend InjectRequest (LinkTargetRepId, LinkCaseB, per-worker sibling) + pairing result fields
ViewModels/InjectAssessmentViewModel.cs     # + LinkedTargetRepId / LinkCaseFlag hidden fields
Views/Admin/InjectAssessment.cshtml         # ganti placeholder :162; + tombol Cari Room + chip + modal + #previewPairingSummary
HcPortal.Tests/*.cs                         # + xUnit: per-worker linking, write-online atomicity, anti-double, preview==commit pairing, unlink revert
tests/e2e/inject-assessment-397.spec.ts     # NEW Playwright: modal/picker/chip/preview/unlink runtime
```

### Pattern 1: Bidirectional per-pekerja linking (D-02)
**What:** Per sesi inject, cari sibling existing di group target by-UserId, set dua arah.
**When to use:** Saat `req.LinkTargetRepId` terisi (room tertaut).
**Example (peta eksak dari template online):**
```csharp
// Source: AssessmentAdminController.cs:1307-1314 (template online — adaptasi untuk inject)
// Online (template): index-paired karena Pre & Post dibuat bersamaan urut UserIds.
for (int i = 0; i < preSessions.Count; i++) {
    preSessions[i].LinkedGroupId = linkedGroupId;
    preSessions[i].LinkedSessionId = postSessions[i].Id;
    postSessions[i].LinkedSessionId = preSessions[i].Id;
}

// 397 ADAPTASI (inject): sibling di-resolve BY-UserId (bukan index — online side sudah ada di DB).
// Di dalam InjectBatchAsync, setelah session inject ter-insert (punya Id):
//   var siblingByUserId = await _context.AssessmentSessions
//       .Where(s => s.LinkedGroupId == resolvedGroupId
//                && s.AssessmentType == oppositeType   // tipe-lawan
//                && s.Id != session.Id)
//       .ToDictionaryAsync(s => s.UserId, s => s);     // 1 sibling per UserId
//   if (siblingByUserId.TryGetValue(user.Id, out var sib)) {
//       session.LinkedSessionId = sib.Id;              // inject → sibling
//       // write-back sibling → inject (ExecuteUpdate atau tracked update, dalam tx):
//       sib.LinkedSessionId = session.Id;
//   } else { session.LinkedSessionId = null; }          // unpaired (D-03) — tampil sisi tunggal
//   session.LinkedGroupId = resolvedGroupId;            // SELALU set (gabung grup, D-03)
```
**Catatan:** Resolusi siblingByUserId harus diambil SEKALI per batch (bukan per worker — hindari N+1). Untuk inject↔inject, "sibling" juga sesi inject di batch lain yang sudah commit.

### Pattern 2: Resolve LinkedGroupId target (D-01 Kasus A vs B)
**What:** Tentukan nilai `LinkedGroupId` yang dipakai.
```csharp
// Kasus A: room target sudah ber-grup → ADOPT (tak sentuh online grouping value)
//   resolvedGroupId = target.LinkedGroupId.Value
// Kasus B: room target standalone (LinkedGroupId == null) → tulis stiker baru:
//   resolvedGroupId = target.RepresentativeId    // id sesi representatif target (konvensi :1270)
//   → tulis resolvedGroupId ke SEMUA sesi room target (online) + sesi inject
//   → AuditLog "LinkPrePost" PER sesi online dimutasi (D-09)
//   → skor/jawaban/status online TIDAK disentuh (hanya kolom LinkedGroupId/LinkedSessionId)
```
**Sumber kebenaran nilai:** `linkedGroupId = preSessions[0].Id` [VERIFIED: AssessmentAdminController.cs:1270]. `RepresentativeId` dari picker = id sesi PreTest representatif (atau first) room target [VERIFIED: AssessmentAdminController.cs:168 `RepresentativeId = rep.Id`, rep = PreTest pertama atau first].

### Pattern 3: Anti-double-link preflight (D-08)
**What:** Tolak/peringatkan pekerja yang sudah punya sibling tipe-SAMA di group target.
```csharp
// Pekerja X inject Pre, tapi sudah punya Pre di group target → 2 Pre untuk 1 user = ambigu
// (gain-score match by UserId → tak deterministik). Query:
//   var existingSameType = await _context.AssessmentSessions
//       .Where(s => s.LinkedGroupId == resolvedGroupId
//                && s.AssessmentType == req.AssessmentType   // tipe SAMA dengan yang di-inject
//                && targetUserIds.Contains(s.UserId))
//       .Select(s => s.UserId).ToListAsync();
//   → untuk setiap UserId di existingSameType: tambah InjectRowError (daftar LENGKAP, pola 396 D-09)
```
**Penempatan:** Di preflight, sebelum tx menulis (pola `PreflightValidateAsync` [VERIFIED: InjectAssessmentService.cs:341-442 mengumpulkan SEMUA error tanpa early-return]). Default BLOK per-pekerja (rekomendasi UI-SPEC N4).

### Pattern 4: Atomic write-to-online + audit terpisah (D-01 Kasus B + D-09)
**What:** Semua write (inject + online) dalam SATU transaksi `InjectBatchAsync`.
```csharp
// Source: InjectAssessmentService.cs:88-322 (tx pattern existing)
using var transaction = await _context.Database.BeginTransactionAsync();
try {
    // ... insert sesi inject (existing) ...
    // ... resolve + set link (inject + online) — dalam tx yang SAMA ...
    // ... AuditLog _context.AuditLogs.Add(...) LANGSUNG (BUKAN AuditLogService.LogAsync
    //     yang SaveChanges sendiri → commit parsial; lihat catatan :289) ...
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
} catch { await transaction.RollbackAsync(); /* rollback total */ }
```
**KRITIS:** Audit in-tx HARUS pakai `_context.AuditLogs.Add` langsung, BUKAN `AuditLogService.LogAsync` — karena LogAsync memanggil `SaveChangesAsync` sendiri [VERIFIED: AuditLogService.cs:41] yang akan commit parsial di tengah transaksi [VERIFIED: catatan eksplisit InjectAssessmentService.cs:289-290].

### Anti-Patterns to Avoid
- **Broadcast 1 LinkedSessionId ke semua sesi:** state sekarang [VERIFIED: InjectAssessmentService.cs:120]. Semua sesi salah-tunjuk ke 1 sibling. HARUS per-UserId.
- **Set LinkedGroupId hanya pada sesi ter-pair (Kasus B):** group jadi tidak konsisten. Tulis ke SEMUA sesi room target.
- **Pakai LinkedSessionId sebagai penentu tampil-berpasangan:** display match by LinkedGroupId+UserId [VERIFIED: CMPController.cs:3417-3433]. LinkedSessionId salah jadi tetap tampil; LinkedGroupId salah = pasangan hilang.
- **`AuditLogService.LogAsync` di dalam transaksi:** commit parsial (lihat Pattern 4).
- **Menyentuh Score/IsPassed/Status/responses sesi online:** HARAM (D-01). Hanya `LinkedGroupId`/`LinkedSessionId`.
- **Parse HTML dari `ManageAssessmentTab_Assessment` di modal:** aksi return PartialView [VERIFIED: AssessmentAdminController.cs:245]. Buat endpoint JSON.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Pairing/grouping mekanisme | Sistem grup baru | `LinkedGroupId`+`LinkedSessionId` existing | Spec §9/§13 + D-01: "reuse logika PrePost, nol grouping baru". Display sudah pair by ini |
| Cross-link wiring | Loop tulis sendiri dari nol | Adaptasi AssessmentAdminController.cs:1307-1314 | Template teruji production |
| Atomic batch + rollback | tx baru | `InjectBatchAsync` tx existing | Sudah atomic + audit-in-tx pattern teruji 393 |
| Preview skor | Hitung ulang | `PreviewInjectScore` + `AssessmentScoreAggregator` | preview==commit dikunci 395/396 |
| Audit log | Tabel/format baru | `AuditLog` model + `_context.AuditLogs.Add` | ⚠ ActionType `[MaxLength(50)]` [VERIFIED: AuditLog.cs:29] — "LinkPrePost"(11)/"LinkPrePostUndo"(15) muat |
| Atomic delete/revert dengan partial-failure guard | Dari nol | Mirror `DeleteAssessmentGroup` (AssessmentAdminController.cs:2398-2535) | Pola partial-failure + audit + FK guard sudah matang |
| Modal Bootstrap + chip | Komponen kustom | Pola `#successModal` CreateAssessment + `.badge` | [CITED: 397-UI-SPEC.md N2 — reuse `CreateAssessment.cshtml:757-834`] |

**Key insight:** Penautan 397 = mengisi 2 kolom + audit. Semua perilaku downstream (Records, Results, gain-score, sertifikat) **gratis** asal `LinkedGroupId` benar. Bahaya satu-satunya = menulis ke data online (Kasus B + bidirectional) — itu satu-satunya tempat logika baru yang menyentuh data existing, dan harus atomic + audit.

## Runtime State Inventory

> Phase ini menulis ke `AssessmentSession` ONLINE existing (D-01 Kasus B + D-02 bidirectional write-back). Bukan rename, tapi memutasi data live → inventory relevan untuk memahami blast-radius.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data (sesi online dimutasi) | Kasus B: `LinkedGroupId` ditulis ke SEMUA sesi room target online; bidirectional: `LinkedSessionId` di-set pada sesi sibling (online/inject). Skor/jawaban/status TIDAK disentuh | Write in-tx + audit "LinkPrePost" per sesi. Unlink (D-12) harus bisa revert |
| Live service config | None — tak ada konfigurasi service eksternal (n8n/scheduler/dll) yang menyimpan string ini. Verified: fitur murni DB+web | None |
| OS-registered state | None — verified: tak ada task scheduler/process name terkait penautan | None |
| Secrets/env vars | None — verified: tak ada secret/env baru. RBAC reuse `[Authorize(Roles="Admin, HC")]` | None |
| Build artifacts | None — 0 migration (kolom sudah ada), tak ada egg-info/binary. Razor di-embed saat build → Playwright dari main tree wajib (lesson 354/392) | Rebuild + Playwright |

**Catatan rollback (D-12 Unlink):** Saat unlink, jika group jadi kosong-sebelah dan Kasus B sebelumnya menulis stiker ke online, revert `LinkedGroupId` pada sesi online tersebut. Ini memutasi data online lagi → atomic + audit reverse ("LinkPrePostUndo"). Putusan "apakah group jadi kosong-sebelah" = cek apakah masih ada sibling tipe-lawan di group setelah inject dilepas.

## Common Pitfalls

### Pitfall 1: Broadcast LinkedSessionId (state sekarang)
**What goes wrong:** Semua sesi inject menunjuk ke 1 sibling.id yang sama.
**Why it happens:** `LinkedSessionId = req.LinkedSessionId` [VERIFIED: InjectAssessmentService.cs:120] adalah nilai tunggal di DTO.
**How to avoid:** Resolve sibling per-pekerja by-UserId (Pattern 1). Hapus/ganti baris :120.
**Warning signs:** Test: 2 pekerja → keduanya `LinkedSessionId` identik = BUG.

### Pitfall 2: LinkedGroupId hanya pada sesi ter-pair (Kasus B)
**What goes wrong:** Group tidak konsisten; sebagian sesi room target masih null → pasangan hilang sebagian di Records.
**Why it happens:** Lupa bahwa Kasus B harus tulis ke SEMUA sesi room target.
**How to avoid:** Query semua sesi room target (by `LinkedGroupId IS NULL` + grouping key target) lalu set `LinkedGroupId` semuanya. CONTEXT D-01 → catatan: "tulis ke SEMUA sesi room target".
**Warning signs:** Sebagian pekerja online tampil berpasangan, sebagian standalone.

### Pitfall 3: AuditLogService.LogAsync di dalam transaksi
**What goes wrong:** Commit parsial — audit ter-commit duluan, lalu batch rollback → audit "berbohong".
**Why it happens:** `LogAsync` panggil `SaveChangesAsync` internal [VERIFIED: AuditLogService.cs:41].
**How to avoid:** `_context.AuditLogs.Add(...)` langsung, SaveChanges sekali di akhir tx (Pattern 4). [VERIFIED: pola sudah dipakai InjectAssessmentService.cs:297-307].

### Pitfall 4: ActionType melebihi MaxLength(50)
**What goes wrong:** DbUpdateException saat tulis audit.
**Why it happens:** `[MaxLength(50)]` pada `ActionType` [VERIFIED: AuditLog.cs:29].
**How to avoid:** "LinkPrePost"=11 char, "LinkPrePostUndo"=15 char → aman. Jangan buat ActionType verbose.

### Pitfall 5: Razor/JS modal "dianggap selesai" dari grep+build
**What goes wrong:** Modal/picker/chip/preview tidak benar-benar render runtime.
**Why it happens:** App pakai `AddControllersWithViews()` tanpa `AddRazorRuntimeCompilation` → view embedded saat build [CITED: 397-UI-SPEC.md §Design System; lesson 354/392].
**How to avoid:** Playwright runtime dari MAIN tree, AD-off (`Authentication__UseActiveDirectory=false`), `--workers=1` [VERIFIED: tests/e2e/inject-assessment-395.spec.ts:3-6].

### Pitfall 6: Picker endpoint kira return JSON
**What goes wrong:** Modal fetch dapat HTML, parsing gagal.
**Why it happens:** `ManageAssessmentTab_Assessment` return `PartialView("Shared/_AssessmentGroupsTab")` [VERIFIED: AssessmentAdminController.cs:245].
**How to avoid:** Endpoint JSON baru (`SearchLinkTargets`) yang salin SELECT shape :141-197 tapi `return Json(...)`.

### Pitfall 7: Picker tidak mem-filter IsManualEntry tapi salah filter tipe
**What goes wrong:** Inject Pre menampilkan room Pre (tipe-sama) → tak masuk akal.
**Why it happens:** Salah arah filter.
**How to avoid:** Inject Pre → tampilkan PostTest; inject Post → tampilkan PreTest [CITED: 397-CONTEXT.md D-06]. JANGAN filter `IsManualEntry` (D-10 — tampilkan inject & online).

## Code Examples

### Endpoint JSON picker (NEW — salin shape projection existing)
```csharp
// Source shape: AssessmentAdminController.cs:141-197 (projection ManageAssessmentTab_Assessment)
// Field yang TERSEDIA & dibawa picker (D-06): RepresentativeId(:168), Title, Category, Schedule,
//   AssessmentType, LinkedGroupId(:174/195 null=standalone Kasus B), UserCount(:171/192), IsPrePostGroup(:173/194)
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> SearchLinkTargets(string? term, string injectType) {
    // injectType = AssessmentType yang sedang di-inject; tampilkan tipe-LAWAN:
    var oppositeType = injectType == "PreTest" ? "PostTest" : "PreTest";  // D-06
    var q = _context.AssessmentSessions.AsNoTracking()
        .Where(s => s.AssessmentType == oppositeType);                    // tipe-lawan; JANGAN filter IsManualEntry (D-10)
    if (!string.IsNullOrWhiteSpace(term)) {
        var t = term.ToLower();
        q = q.Where(s => s.Title.ToLower().Contains(t) || s.Category.ToLower().Contains(t));
        // tanggal: bisa parse term → Schedule.Date == parsed (opsional)
    }
    // GroupBy LinkedGroupId (grouped) + (Title,Category,Schedule.Date) (standalone) — mirror :157-197
    // Proyeksikan: RepresentativeId, Title, Category, Schedule, AssessmentType,
    //              LinkedGroupId(null?), UserCount, IsPrePostGroup, IsManualEntry(any sibling manual)
    return Json(rows);  // ⚠ return Json — BUKAN PartialView
}
```

### Audit "LinkPrePost" per sesi online dimutasi (D-09)
```csharp
// Source pattern: InjectAssessmentService.cs:297-307 (ManualInject audit in-tx)
foreach (var onlineSessionId in mutatedOnlineSessionIds) {
    _context.AuditLogs.Add(new AuditLog {
        ActorUserId = actorUserId,
        ActorName = actorDisplay,
        ActionType = "LinkPrePost",                       // ⚠ 11 char ≤ MaxLength(50)
        Description = $"Stiker grup ditulis ke sesi online {onlineSessionId} (LinkedGroupId={resolvedGroupId}). Skor/jawaban tidak diubah. Room target={req.Title}.",
        TargetId = onlineSessionId,
        TargetType = "AssessmentSession",
        CreatedAt = DateTime.UtcNow
    });
}
// SaveChanges + CommitAsync di akhir tx (BUKAN per-add).
```

### Pairing summary di preview (D-07 — dry-run, NO write)
```csharp
// Source: InjectAssessmentController.cs:106-156 (PreviewInjectScore) — EXTEND
// Preview butuh data: sesi group target by UserId untuk hitung paired/unpaired tanpa write.
//   var targetSessions = await _context.AssessmentSessions.AsNoTracking()
//       .Where(s => s.LinkedGroupId == resolvedGroupId && s.AssessmentType == oppositeType)
//       .Select(s => new { s.UserId, s.AssessmentType, s.CompletedAt }).ToListAsync();
//   var siblingUserIds = targetSessions.Select(t => t.UserId).ToHashSet();
//   int paired = injectUserIds.Count(uid => siblingUserIds.Contains(uid));   // → LinkedSessionId terisi
//   int unpaired = injectUserIds.Count - paired;                             // D-03 sisi tunggal
//   bool willTouchOnline = (target.LinkedGroupId == null);                   // Kasus B (D-07 banner)
//   // D-11: warn jika Pre.CompletedAt > Post.CompletedAt (lihat §Date coherence)
//   // D-08: doubleLinkNips = pekerja punya sibling tipe-SAMA di group target
// → tambahkan field ini ke InjectPreviewResult (atau response baru) tanpa SaveChanges.
```

### Unlink (D-12) — mirror atomic delete pattern
```csharp
// Source pattern: AssessmentAdminController.cs:2398-2535 (DeleteAssessmentGroup atomic+audit+partial-guard)
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UnlinkInjectGroup(int injectGroupId /* atau representativeId */) {
    using var tx = await _context.Database.BeginTransactionAsync();
    try {
        // 1. ambil sesi inject ber-LinkedGroupId = injectGroupId
        // 2. untuk tiap sesi inject: jika LinkedSessionId != null → revert sibling.LinkedSessionId = null (bidirectional)
        // 3. set sesi inject LinkedGroupId = null, LinkedSessionId = null
        // 4. Kasus B kosong-sebelah: jika group jadi tak punya sibling tipe-lawan lagi →
        //    revert LinkedGroupId pada sesi online yang dulu di-stiker (HANYA jika ditulis oleh inject)
        // 5. AuditLog "LinkPrePostUndo" per sesi dimutasi
        await _context.SaveChangesAsync(); await tx.CommitAsync();
    } catch { await tx.RollbackAsync(); /* ... */ }
}
```
**⚠ Open Q (lihat §Open Questions):** Menentukan "stiker ditulis oleh inject vs online asli" untuk Kasus B revert — tidak ada flag eksplisit di skema. Rekomendasi: simpan jejak via AuditLog "LinkPrePost" (TargetId = sessionId) → unlink query audit untuk tahu sesi mana yang di-stiker oleh inject ini. ATAU: revert HANYA bila group jadi single-type (semua sisa = tipe sama) — heuristik aman.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| BulkBackfill (skor agregat saja, no pairing) | Inject "seakan online" full + linking | v32.2 (393-398) | 397 menutup gap linking Pre/Post |
| Auto-detect by judul (`TryAutoDetectCounterpartGroup`) | Picker eksplisit + reuse query | Phase 397 (D-05) | User pilih room, bukan tebak judul |
| Online Pre/Post dibuat bersamaan (index-paired :1308) | Inject side-cross link (resolve by-UserId) | Phase 397 | Sibling sudah ada di DB → resolve by-UserId bukan index |

**Deprecated/outdated:** Tidak ada. Semua pola yang di-reuse masih aktif di production.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Nilai `LinkedGroupId` baru (Kasus B) = `RepresentativeId` room target (id sesi nyata, konvensi :1270) | Pattern 2 | Jika konvensi beda, grouping value salah → pasangan tak tampil. Mitigasi: D-01 catatan eksplisit merekomendasi RepresentativeId; verifikasi saat plan dgn test Records render |
| A2 | "Sibling tipe-lawan di group target" = 1 sesi per UserId per tipe | Pattern 1 | Jika ada >1 sibling tipe-lawan per user di 1 group (tak normal), ToDictionary throw. Mitigasi: anti-double D-08 mencegah di sisi inject; sisi online diasumsikan bersih (1 Pre+1 Post per user per group, invariant online). Tambah guard FirstOrDefault bila perlu |
| A3 | Untuk membedakan "stiker Kasus B ditulis oleh inject" saat unlink revert, jejak AuditLog cukup (tak ada flag skema) | Pattern Unlink / Open Q | Jika tak bisa tentukan, unlink mungkin revert stiker yang sebenarnya milik online. Mitigasi: heuristik "revert hanya jika group jadi single-type" + audit query. Konfirmasi user/plan |
| A4 | 396 (UploadInjectExcel/Step5Method) ortogonal — tak mengubah MapToRequest/PreviewInjectScore signature yang 397 perlu | Dependency | Jika 396 mengubah seam, merge konflik. Mitigasi: 397 sequential SETELAH 396 commit [VERIFIED: 396 plans menambah endpoint baru + Step5Method VM flag, tidak mengubah MapToRequest core :163-220] |
| A5 | Picker tanggal-search bisa pakai Schedule (bukan CompletedAt) untuk room online upcoming | §Picker / Date | Room online belum complete punya Schedule, bukan CompletedAt. Date coherence (D-11) pakai CompletedAt yang mungkin null untuk online belum-jalan. Lihat §Date coherence |

**Tidak ada klaim ASUMSI tentang versi library/API** — semua diverifikasi dari live code/registry.

## Open Questions (RESOLVED)

1. **Membedakan stiker Kasus B "milik inject" saat unlink revert (D-12)**
   - What we know: Kasus B menulis `LinkedGroupId` ke sesi online. Skema tak punya flag "ditulis oleh inject".
   - What's unclear: Saat unlink, apakah aman revert `LinkedGroupId` online?
   - Recommendation: (a) Query AuditLog `"LinkPrePost"` dgn `TargetId` = sesi online untuk tahu yang di-stiker; ATAU (b) revert HANYA jika group jadi single-type pasca-unlink (heuristik konservatif). Plan pilih satu; default (b) lebih aman. Jaga minimal (D-12 "jangan melar").
   - **RESOLVED:** Revert stiker online HANYA bila group menjadi single-type setelah sesi inject dilepas (heuristik konservatif opsi-b) — diimplementasikan di Plan 02-T3 (`UnlinkInjectGroupAsync`, langkah Kasus B revert). Audit-trail "LinkPrePost" by `TargetId` (opsi-a) juga acceptable sebagai jejak tambahan, tetapi keputusan revert digerakkan oleh heuristik single-type.

2. **Date coherence (D-11) saat sisi online belum Completed**
   - What we know: `CompletedAt` nullable; room online Upcoming/Open belum punya `CompletedAt`.
   - What's unclear: Bandingkan tanggal apa jika Post online belum dikerjakan?
   - Recommendation: Bila sibling `CompletedAt` null → skip warn (tak ada urutan janggal yang bisa dideteksi). Warn hanya bila kedua tanggal ada dan Pre > Post. Warn-but-allow (tak blok).
   - **RESOLVED:** Skip date-warn bila `CompletedAt` sibling target null — diimplementasikan di Plan 02-T2 (`PreviewPairingAsync`, `DateWarn` hanya true bila kedua `CompletedAt` non-null dan Pre > Post). Warn-but-allow (tidak memblok commit).

3. **Inject↔inject dedup interaction (D-10)**
   - What we know: `FindDuplicateNipsAsync` [VERIFIED: InjectAssessmentService.cs:450-476] men-skip NIP dgn sesi inject duplikat by Title+Category+Date.
   - What's unclear: Saat inject Pre tautkan ke room INJECT Post existing (judul beda), dedup tak akan tabrakan (judul beda) — OK. Tapi anti-double-link (D-08) by group+type yang menangkap ini.
   - Recommendation: Anti-double-link D-08 (Pattern 3) adalah guard yang benar untuk skenario ini; dedup existing ortogonal. Pastikan keduanya tidak saling membatalkan di test.
   - **RESOLVED:** Anti-double-link preflight (D-08) berlaku per `(UserId, LinkedGroupId, AssessmentType)` TANPA memandang `IsManualEntry`; penautan inject↔inject DIIZINKAN dan memakai guard yang sama. Dedup `FindDuplicateNipsAsync` (by Title+Category+Date) ortogonal — keduanya tidak saling membatalkan (diverifikasi via test Plan 01-T3 CrossGrouping inject↔inject + Plan 02-T2 anti-double).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build + test | ✓ | 8.0.418 | — |
| EF Core 8 (SqlServer) | persist link | ✓ | 8.0.0 | — |
| SQL Server (SQLEXPRESS lokal) | integration test + DB lokal verify | ✓ (lesson: env login fix) | — | InMemory TIDAK cukup untuk ExecuteUpdate |
| Node.js | Playwright | ✓ | v24.14.0 | — |
| Playwright | runtime modal/picker verify | ✓ | 1.58.2 | grep+build TIDAK cukup (lesson 354) |
| `dotnet ef` migration | — | N/A | — | **0 migration phase ini** — tak perlu |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None (integration test wajib real SQL — bukan fallback, syarat existing pola InjectAssessmentFixture [VERIFIED: HcPortal.Tests/InjectAssessmentServiceTests.cs:9,33]).

## Validation Architecture

> nyquist_validation enabled [VERIFIED: .planning/config.json `"nyquist_validation": true`].

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (logic/atomicity) + Playwright 1.58.2 (modal/picker/preview/chip/unlink runtime) |
| Config file | HcPortal.Tests/HcPortal.Tests.csproj; tests/playwright.config.ts |
| Quick run command | `dotnet test --filter "Category!=Integration"` (cepat, in-memory/pure logic) |
| Full suite command | `dotnet test` (termasuk `[Trait("Category","Integration")]` real-SQL) + `cd tests && npx playwright test e2e/inject-assessment-397.spec.ts --workers=1` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INJ-12 | Per-worker bidirectional linking by-UserId (D-02, bukan broadcast) | integration (real SQL) | `dotnet test --filter "FullyQualifiedName~InjectLink"` | ❌ Wave 0 (NEW) |
| INJ-12 | Kasus A adopt LinkedGroupId, online tak disentuh (D-01) | integration | same filter | ❌ Wave 0 |
| INJ-12 | Kasus B tulis stiker ke SEMUA sesi online + atomic + audit (D-01/D-09) | integration | same filter | ❌ Wave 0 |
| INJ-12 | Rollback total bila error (write online + inject batal) | integration | same filter | ❌ Wave 0 |
| INJ-12 | Anti-double-link reject by (UserId,LinkedGroupId,type) — daftar lengkap (D-08) | unit/integration | `dotnet test --filter "FullyQualifiedName~AntiDoubleLink"` | ❌ Wave 0 |
| INJ-12 | Preview pairing summary == commit outcome (D-07, dry-run no write) | integration | `dotnet test --filter "FullyQualifiedName~PreviewPairing"` | ❌ Wave 0 |
| INJ-12 | Picker JSON filter tipe-lawan + IsManualEntry not filtered (D-06/D-10) | integration (controller) | same filter | ❌ Wave 0 |
| INJ-12 | Cross grouping UTUH di Records/gain-score (1 sisi inject 1 sisi online, spec §13) | integration | `dotnet test --filter "FullyQualifiedName~CrossLinkGrouping"` | ❌ Wave 0 (KRITIS) |
| INJ-12 | Unlink revert bidirectional + Kasus B stiker, atomic + audit (D-12) | integration | `dotnet test --filter "FullyQualifiedName~UnlinkInject"` | ❌ Wave 0 |
| INJ-12 | Modal picker buka/filter/chip + preview pairing render + unlink konfirmasi (runtime) | Playwright | `cd tests && npx playwright test e2e/inject-assessment-397.spec.ts --workers=1` | ❌ Wave 0 (NEW) |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (cepat) + `dotnet build`
- **Per wave merge:** `dotnet test` penuh (real-SQL integration)
- **Phase gate:** Full suite green + Playwright 397 spec green + DB lokal verify (Develop Workflow CLAUDE.md) sebelum `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `HcPortal.Tests/InjectLinkPrePostTests.cs` — per-worker linking, Kasus A/B, atomic rollback, write-to-online (covers INJ-12 core). Pakai `InjectAssessmentFixture` existing (real-SQL disposable DB) [VERIFIED: InjectAssessmentServiceTests.cs:24-57].
- [ ] `HcPortal.Tests/InjectAntiDoubleLinkTests.cs` — anti-double preflight daftar-lengkap (D-08).
- [ ] `HcPortal.Tests/InjectPreviewPairingTests.cs` — preview==commit pairing outcome (D-07).
- [ ] `HcPortal.Tests/InjectCrossGroupingTests.cs` — cross inject↔online grouping utuh di gain-score/Results pairing (spec §13, KRITIS).
- [ ] `HcPortal.Tests/UnlinkInjectGroupTests.cs` — unlink revert atomic + audit (D-12).
- [ ] `tests/e2e/inject-assessment-397.spec.ts` — modal/picker/chip/preview/unlink runtime (lesson 354). Mirror struktur inject-assessment-395.spec.ts (login admin, snapshot/restore DB, --workers=1).
- [ ] Framework install: tidak perlu — xUnit + Playwright sudah terpasang.

## Security Domain

> security_enforcement enabled (absent in config = enabled).

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | `[Authorize(Roles="Admin, HC")]` pada SEMUA action baru (picker JSON, unlink) — server-authoritative [VERIFIED: InjectAssessmentController.cs:41,51,104] |
| V3 Session Management | no | Reuse ASP.NET Identity cookie existing |
| V4 Access Control | yes | RBAC Admin,HC; picker tidak boleh leak room ke non-Admin/HC; unlink hanya Admin,HC |
| V5 Input Validation | yes | `injectType` whitelist (PreTest/PostTest); `term` parameterized via EF (LINQ → SQL param); `LinkTargetRepId` validasi server (room ada + tipe-lawan); malformed JSON → try/catch fallback [VERIFIED: pola ParseAnswerVms InjectAssessmentController.cs:327-340] |
| V6 Cryptography | no | Tak ada crypto baru (ComputeAutoGenSeed SHA-256 non-secret existing, tak disentuh) |

### Known Threat Patterns for ASP.NET MVC + EF8
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| IDOR — HC tautkan ke room yang bukan haknya / unlink arbitrary group | Elevation/Tampering | Validasi server: room target ada + tipe-lawan + RBAC. Unlink: validasi injectGroupId milik sesi inject (IsManualEntry) sebelum revert |
| Tampering — client kirim LinkTargetRepId palsu / LinkedGroupId arbitrary | Tampering | MapToRequest re-resolve server-authoritative; jangan trust LinkedGroupId dari client mentah |
| Tampering — write ke skor/status sesi online via penautan | Tampering | Hanya update kolom LinkedGroupId/LinkedSessionId; JANGAN SetProperty Score/Status/responses |
| CSRF pada POST link/unlink | Tampering | `[ValidateAntiForgeryToken]` (pola existing :52,105) |
| XSS — judul/kategori room user-authored di modal/chip | Tampering | Render via `.textContent`, BUKAN innerHTML [CITED: 397-UI-SPEC.md §Aksesibilitas] |
| SQL injection via search term | Tampering | EF LINQ parameterized (existing pola :125-133) |
| Compliance — data online dimutasi tanpa jejak | Repudiation | AuditLog "LinkPrePost" per sesi online (D-09) — wajib in-tx |

## Project Constraints (from CLAUDE.md)
- **Bahasa Indonesia** untuk semua copy/comment user-facing (alat internal HC).
- **Develop Workflow:** verifikasi LOKAL wajib — `dotnet build` + `dotnet run` (cek `http://localhost:5277`) + cek DB lokal + Playwright. JANGAN edit kode/DB di server Dev/Prod. JANGAN push tanpa verifikasi lokal.
- **0 migration** phase ini — jika ternyata butuh schema change, STOP (kontradiksi scope; kolom sudah ada).
- **Seed Workflow:** test yang menulis DB lokal → snapshot sebelum, restore sesudah, catat `docs/SEED_JOURNAL.md` (pola inject-assessment-395.spec.ts beforeAll/afterAll [VERIFIED: tests/e2e/inject-assessment-395.spec.ts:11-12]).
- **Branch:** main. Notify IT pasca-commit dengan commit hash + flag migration=FALSE.

## User Constraints (from CONTEXT.md)

> CRITICAL untuk planner — copy verbatim dari 397-CONTEXT.md. 12 keputusan TERKUNCI.

### Locked Decisions
- **D-01:** Adopt jika sudah ber-grup; tulis stiker grup ke ONLINE jika standalone. Kasus A (target ber-LinkedGroupId): inject ADOPT, tak sentuh online. Kasus B (target standalone): tulis LinkedGroupId baru ke sesi online existing JUGA + sesi inject, dalam transaksi atomic yang sama + AuditLog (D-09). Hanya nomor grup ditulis — skor/jawaban/status online TIDAK diubah. Rollback total bila error. Nilai LinkedGroupId baru = RepresentativeId room target, tulis ke SEMUA sesi room target + sesi inject.
- **D-02:** Bidirectional per-pekerja. sesiInject.LinkedSessionId → sibling existing, DAN siblingExisting.LinkedSessionId → sesi inject. Resolve sibling by-UserId per sesi (BUKAN broadcast 1 nilai :120) + write balik ke sesi existing (online maupun inject).
- **D-03:** Pekerja inject tanpa pasangan di room target → izinkan unpaired + warn. Set LinkedGroupId, LinkedSessionId=null → tampil sisi tunggal. Tandai di preview ("N pekerja tanpa pasangan"). Warn-but-allow, bukan blok.
- **D-04:** Penautan OPSIONAL saat tipe Pre/Post. Picker boleh di-skip → inject Pre/Post standalone (tautkan nanti).
- **D-05:** Picker = MODAL pop-up. Tombol "Cari Room" Step-1 → modal → pilih → chip di Step-1.
- **D-06:** Filter = tipe LAWAN saja + search. Inject Pre → hanya PostTest; inject Post → hanya PreTest (grouped maupun standalone). Reuse query ManageAssessmentTab_Assessment, search by Judul/Kategori/jadwal. Baris room: judul + kategori + tanggal + badge tipe + jumlah peserta + indikator sudah-ber-grup.
- **D-07:** Preview pra-commit tampilkan ringkasan PAIRING (selain skor): berapa ter-pair, berapa unpaired (D-03), apakah akan tempel stiker grup ke online (Kasus B, peringatkan). Reuse PreviewInjectScore + AssessmentScoreAggregator.
- **D-08:** Anti-dobel-link per-pekerja → blok/peringatkan. Bila per-pekerja sudah ada sibling tipe-sama di grup target → tolak/peringatkan pekerja itu. Masuk daftar error/warn preview (pola daftar-lengkap 396 D-09).
- **D-09:** Audit terpisah untuk perubahan data ONLINE. Tiap sesi online existing diubah (tempel stiker / set LinkedSessionId) → AuditLog tersendiri (ActionType="LinkPrePost") berisi actor + sessionId online + LinkedGroupId + room target — terpisah dari "ManualInject".
- **D-10:** Room target boleh = room hasil INJECT (bukan hanya online asli). Picker tampilkan inject MAUPUN online. Link inject↔inject tak menyentuh data online. Picker tak mem-filter IsManualEntry.
- **D-11:** Koherensi tanggal Pre vs Post → WARN di preview (allow). Bila tanggal Pre (CompletedAt) lebih BARU dari Post target → peringatan, jangan blok commit.
- **D-12:** Unlink/ubah tautan pasca-commit → SERTAKAN di Phase 397 (scope addition). Endpoint + UI unlink; saat unlink putuskan nasib stiker Kasus B (revert LinkedGroupId pada online bila grup kosong-sebelah) + revert LinkedSessionId bidirectional; atomic + audit ("LinkPrePost" reverse). Jaga minimal & fokus.

### Claude's Discretion
- Nilai LinkedGroupId baru (Kasus B): rekomendasi = RepresentativeId room target; tulis ke SEMUA sesi room target + sesi inject. Konfirmasi konvensi vs CreateAssessment:1270.
- Endpoint picker: reuse ManageAssessmentTab_Assessment (verifikasi JSON vs view) atau endpoint baru ringan ter-filter tipe-lawan.
- Komponen modal: reuse pola Bootstrap modal existing; styling chip, debounce search.
- Perubahan signature InjectAssessmentService: resolusi sibling per-UserId dalam grup target (ganti broadcast :120), write-back ke sesi existing — semua dalam transaksi InjectBatchAsync yang sama.
- Penamaan ActionType audit ("LinkPrePost") + payload.
- Penempatan tombol "Cari Room" + chip di Step-1; copy notice; ikon.

### Deferred Ideas (OUT OF SCOPE)
- Multi-paket variasi per room inject — out-of-scope spec §12.
- Import gambar soal via Excel — out-of-scope spec §12.
- Auto-detect by judul saja (TryAutoDetectCounterpartGroup) sebagai satu-satunya jalur — ditolak; picker eksplisit (boleh jadi hint).
- Editor link umum / bulk re-link — di luar D-12 (unlink minimal saja).
- Link untuk tipe Standard — hanya Pre/Post punya picker.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| INJ-12 | HC mencari & memilih assessment room existing untuk menautkan sesi inject Pre/Post (LinkedGroupId + LinkedSessionId, reuse ManageAssessmentTab_Assessment) — dukung silang inject↔online | Endpoint JSON picker (salin projection :141-197); per-worker bidirectional linking (template :1307-1314, ganti broadcast :120); Kasus A/B write-to-online (D-01, konvensi :1270); anti-double preflight (pola :341-442); preview pairing (extend :106-156); unlink (mirror :2398-2535); audit "LinkPrePost" (pola :297-307, MaxLength 50 OK); cross-grouping pair by LinkedGroupId+UserId (CMPController:3417-3433) |

## Sources

### Primary (HIGH confidence)
- Live code (file:line dikutip inline) — InjectAssessmentService.cs:42-476; InjectAssessmentController.cs:106-358; AssessmentAdminController.cs:112-246,1233-1315,1920-1992,2398-2535,7301-7327; CMPController.cs:250-344,3412-3502; Models/AssessmentSession.cs:137-191; Models/AssessmentConstants.cs:5-11; Models/InjectAssessmentDtos.cs:1-143; Models/AuditLog.cs:5-53; Services/AuditLogService.cs:9-44; ViewModels/InjectAssessmentViewModel.cs; Views/Admin/InjectAssessment.cshtml:110-179; Helpers/SiblingSessionQuery.cs:7-25
- 397-CONTEXT.md (12 keputusan D-01..D-12)
- 397-UI-SPEC.md (5 surface N1..N5, approved)
- docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md §9/§10/§13
- .planning/REQUIREMENTS.md INJ-12
- Bash version checks (dotnet 8.0.418, EF 8.0.0, node v24.14.0, Playwright 1.58.2)

### Secondary (MEDIUM confidence)
- 393/395/396 CONTEXT + SUMMARY (carry-forward pola atomic/preview/error-list; 396 endpoint ortogonal)

### Tertiary (LOW confidence)
- None — semua klaim diverifikasi dari live code.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versi diverifikasi via Bash + csproj; semua reuse-asset dikutip file:line.
- Architecture: HIGH — template cross-link (:1307-1314) + pairing logic (:3417-3433) + tx pattern (:88-322) semua dibaca langsung. 1 area MEDIUM = unlink Kasus B revert (no schema flag, lihat Open Q).
- Pitfalls: HIGH — broadcast (:120), audit-in-tx (:289), PartialView (:245), MaxLength (:29) semua diverifikasi.

**Research date:** 2026-06-18
**Valid until:** 2026-07-18 (stable brownfield; re-verify file:line jika 395/396 commit menggeser nomor baris sebelum plan)
