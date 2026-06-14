# Phase 363: Audit Fix Alur PROTON (T1-T10) - Research

**Researched:** 2026-06-11
**Domain:** ASP.NET Core 8 MVC + EF Core — refactor anti-drift endpoint kembar, gate authorization, notif/audit, query dashboard (internal-codebase)
**Confidence:** HIGH (semua temuan diverifikasi langsung dari source dengan line number terkini)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Anti-drift T1/T2/T7 — helper bersama**
- **D-01 (T1/T2/T7):** Extract helper bersama di CDPController (atau service): `approve-core` (set approval role + allApproved check + notif HC `COACH_ALL_COMPLETE` + race-guard reload-fresh D-10) dan `reject-core` (full chain reset: Status="Rejected" + SrSpv/SH/HC→Pending + null approver ID/timestamp) — dipanggil dari KEDUA endpoint tiap pasangan (`ApproveDeliverable`/`ApproveFromProgress`, `RejectDeliverable`/`RejectFromProgress`). Drift mati permanen. Wajib test paritas (xUnit per pasangan: hasil state identik untuk input sama).
- **D-02 (T1):** `ApproveFromProgress` lewat helper otomatis dapat: allApproved check (pola `:908-931`) + `CreateHCNotificationAsync` `COACH_ALL_COMPLETE` (pola `:968`, `:1127-1136`). Paritas penuh perilaku post-approval.
- **D-03 (T2):** Reset `HCApprovalStatus`→"Pending" + null `HCReviewedById/At` di reject DAN kedua jalur resubmit (`UploadEvidence` wasRejected `:1297-1308` + `SubmitEvidenceWithCoaching` isResubmit `:2270-2279`) — belt-and-braces, rejection lama pre-fix tidak bawa HC review basi.
- **D-04 (T7):** Race-guard D-10 (reload-fresh AsNoTracking + re-validasi) masuk approve-core — kedua endpoint terlindungi.

**T3 — gate jalur reaktivasi**
- **D-05:** Gate reaktivasi. Jalur reaktivasi assignment (`CoachMappingController.cs:597-606`) jalankan year gate sama dengan assignment baru: `IsPrevYearPassedAsync` + exempt. Blocked → pesan error pola hard-block 359 D-05.
- **D-06:** Exempt stempel permanen — selain cek `isExemptFromCrossYear` existing (`:535-537`), reaktivasi exempt bila assignment yang mau direaktivasi punya `Origin="Bypass"`.

**T4 — penanda assignment nonaktif: surface warning**
- **D-07:** `EnsureAsync` tetap strict IsActive (`ProtonCompletionService.cs:39-43`) — assignment nonaktif = keputusan sadar admin, JANGAN auto-terbit penanda.
- **D-08:** Saat miss (lulus exam tapi tidak ada assignment aktif): (a) tulis AuditLogs entry (actor=system/grading, deskripsi sebut coachee+track+session), (b) kirim notif lonceng ke user role HC via NotificationService (pola `COACH_ALL_COMPLETE` broadcast; tipe template baru boleh), isi arahkan ke `BackfillProtonPenanda`. Warn log existing dipertahankan.

**T5 — tampilkan "Belum Mulai"**
- **D-09:** Ubah query `HistoriProton` (`CDPController.cs:3149-3180`): coachee dengan mapping aktif TANPA `ProtonTrackAssignment` ikut tampil, status "Belum Mulai". Ternary `:3250` jadi 3 cabang. Badge `HistoriProton.cshtml:152-155` + opsi filter `:79` jadi berfungsi. (Bukan hapus dead branch.)

**T6 — ValidUntil paritas**
- **D-10:** Buang hardcode +3 tahun di `RegradeAfterEditAsync` Fail→Pass (`GradingService.cs:516`): regrade berhenti set `ValidUntil`. SetProperty ValidUntil dihapus dari blok `:520-521`. Hook bypass 360 (`:314/:496/:549`) tidak disentuh.

**T8 — evidence history append**
- **D-11:** `SubmitEvidenceWithCoaching` (`:2284-2292`) append path lama ke `EvidencePathHistory` sebelum overwrite — pola persis `UploadEvidence:1280-1286`. File fisik lama TETAP disimpan (kebijakan E10 "KEEP orphan evidence"; JANGAN tambah File.Delete).

**T9 — guard log-warn**
- **D-12:** Saat prev-track resolve null padahal `requestedTrack.Urutan > 1`: log warning eksplisit di kedua titik (`AssessmentAdminController.cs:1348-1352` + `CoachMappingController.cs:506-509`). Gate tetap jalan. Tanpa throw/blok.

**T10 — by-design**
- **D-13:** `BackfillProtonPenanda` tanpa year-gate = by-design. Aksi: komentar kode di method + catatan di FINDINGS. Nol perubahan logic.

### Claude's Discretion
- Bentuk helper D-01: private method CDPController vs service class terpisah — planner pilih (perhatikan testability; pola ProtonCompletionService/ProtonBypassService tersedia).
- Nama tipe notif T4 (`PROTON_PENANDA_MISS` atau sejenis) + template title/body — ikuti pola `NotificationService._templates`.
- Pembagian plan/wave + urutan fix (file-overlap: CDPController T1/T2/T5/T7/T8; CoachMapping T3/T9; GradingService T6; ProtonCompletionService+AssessmentAdmin T4/T9/T10).
- Strategi test: xUnit paritas per pasangan endpoint + [Fact] gate reaktivasi + regresi existing; e2e/UAT scope.

### Deferred Ideas (OUT OF SCOPE)
- Hapus file fisik evidence lama saat re-upload (T8 opsi b) — TIDAK diambil; kebijakan keep-evidence E10 dipertahankan.
- Konsolidasi pasangan HCReview + jalur upload jadi helper juga — hanya kalau drift ditemukan kelak.
- Year-gate di backfill (T10 opsi b) — ditolak by-design.
</user_constraints>

<phase_requirements>
## Phase Requirements (T1-T10 dari 363-FINDINGS.md)

| ID | Deskripsi | Severity | Research Support |
|----|-----------|----------|------------------|
| T1 | `ApproveFromProgress` tidak cek allApproved → notif HC `COACH_ALL_COMPLETE` silent miss | HIGH | §Helper Extraction (approve-core) + Code Example 1; CreateHCNotificationAsync `:1111-1140` siap reuse |
| T2 | `RejectFromProgress` tidak reset chain; `HCApprovalStatus` "Reviewed" survive rejection | HIGH | §Helper Extraction (reject-core) + Code Example 2; pola reset `RejectDeliverable:1028-1037` |
| T3 | Reaktivasi assignment skip year gate total | HIGH | §T3 Gate Reaktivasi (analisis loophole cabang 1 + lokasi gate + exempt query baru) |
| T4 | Lulus exam saat assignment nonaktif → penanda silent miss | MED | §T4 Surface Warning (EnsureAsync return bool ambigu + opsi injeksi/enum + resolve HC) |
| T5 | Badge "Belum Mulai" HistoriProton unreachable | LOW | §T5 Query Belum Mulai (query shape + paritas ExportHistoriProton) |
| T6 | Asimetri ValidUntil regrade Fail→Pass hardcode +3thn | MED | §T6 ValidUntil (drop SetProperty `:516/:520-521`) + edge case Pass→Fail→Pass |
| T7 | `ApproveFromProgress` tanpa race-guard D-10 | MED | §Helper Extraction (race-guard masuk approve-core, D-04) |
| T8 | `SubmitEvidenceWithCoaching` overwrite EvidencePath tanpa append history | MED | §T8 Evidence History (pola append `UploadEvidence:1280-1286`) |
| T9 | Track tanpa Urutan-1 → prev null → diperlakukan Tahun 1 (gate lolos) | LOW | §T9 Guard Log-Warn (2 titik) |
| T10 | `BackfillProtonPenanda` tanpa year-gate | by-design | §T10 (komentar + catatan, nol logic) |
</phase_requirements>

## Summary

Phase 363 adalah fase **perbaikan internal-codebase** (bukan greenfield) yang menutup 10 temuan verifikasi adversarial alur PROTON. Akar 4 temuan (T1/T2/T7/T8) adalah **drift antar endpoint kembar**: setiap aksi punya 2 implementasi — versi "Deliverable page" (return redirect/View) dan versi "FromProgress" modal inline CoachingProton (return JSON). Versi FromProgress dibuat belakangan (Phase 65 AJAX) dan tidak ikut menerima penambahan logic yang masuk ke versi page (allApproved notif, full chain reset, race-guard, history append). Keputusan kunci **D-01** membunuh akar drift dengan extract helper bersama — bukan menambal tiap endpoint.

Tiga temuan sisanya menyentuh subsystem berbeda namun terisolasi: **T3** (loophole authorization gate di jalur reaktivasi assignment `CoachMappingController`), **T4** (silent-miss penanda saat assignment nonaktif di `ProtonCompletionService`/`GradingService`), **T6** (asimetri masa berlaku sertifikat di `GradingService.RegradeAfterEditAsync`). Sisanya (T5/T9/T10) adalah polish/defensive/dokumentasi.

Risiko utama tetap di **D-01**: `CDPController.cs` ~3600+ baris, 5 temuan menyentuhnya. Helper extraction harus dilakukan DULU dengan test paritas yang mem-pin perilaku `ApproveDeliverable`/`RejectDeliverable` existing SEBELUM refactor, lalu fix lain (T5/T8) menyusul di atasnya. Semantik approval v25.0 A-2 (1 approver L4 cukup, HC bukan approver) TERKUNCI — helper tidak boleh mengubahnya.

**Primary recommendation:** Wave 1 = extract `approve-core`/`reject-core` sebagai **private method di CDPController** (akses langsung ke `_userManager`, `_notificationService`, `RecordStatusHistory`, `CreateHCNotificationAsync` — risiko terendah), dengan test paritas via fixture SQL nyata. Wave 2 = fix subsystem terpisah (T3 CoachMapping, T4 ProtonCompletion/Grading, T6 Grading) yang bisa paralel. Wave 3 = polish CDPController di atas helper (T5/T8) + defensive (T9) + dokumentasi (T10).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Approve/Reject deliverable (chain L4 + notif) | API/Backend (CDPController) | DB (ProtonDeliverableProgress) | Logic approval + side-effect notif murni server-side; modal JS hanya konsumsi JSON response |
| Notif HC allApproved (T1) | API/Backend (CDPController.CreateHCNotificationAsync) | DB (UserNotification) | Broadcast ke role HC = server-only; tabel ProtonNotification DORMAN |
| Year-gate reaktivasi (T3) | API/Backend (CoachMappingController) | Service (ProtonCompletionService.IsPrevYearPassedAsync) | Authorization gate WAJIB server-side (PCOMP-07 precedent) — tidak boleh di JS |
| Penanda kelulusan + surface miss (T4) | Service (ProtonCompletionService) | API callers (GradingService, AssessmentAdminController) | Helper tunggal pembuatan penanda (A-4); surface = notif+audit |
| ValidUntil sertifikat (T6) | Service (GradingService) | DB (AssessmentSession) | Penerbitan sertifikat = service grading; setup ValidUntil = HC saat buat sesi |
| Histori Proton list + export (T5) | API/Backend (CDPController) | View (HistoriProton.cshtml — sudah siap) | Query + status string server-side; badge view sudah punya cabang ke-3 |
| Evidence path history (T8) | API/Backend (CDPController) | DB (ProtonDeliverableProgress.EvidencePathHistory) | JSON history field di-maintain server-side |

## Standard Stack

Internal-codebase phase — stack sudah terkunci oleh project. Tidak ada library baru.

### Core (existing, terverifikasi)
| Library | Version | Purpose | Catatan |
|---------|---------|---------|---------|
| .NET / ASP.NET Core | net8.0 | Runtime + MVC | [VERIFIED: HcPortal.Tests.csproj `TargetFramework=net8.0`] |
| Entity Framework Core | 8.0.0 | ORM + migrations | [VERIFIED: csproj]; migration=false fase ini |
| EFCore.SqlServer | 8.0.0 | SQL Server provider | [VERIFIED: csproj] |
| xUnit | 2.9.3 | Test framework | [VERIFIED: csproj] |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner | [VERIFIED: csproj] |
| EFCore.InMemory | 8.0.0 | InMemory test (tersedia, tapi pola fase Proton pakai SQL nyata) | [VERIFIED: csproj] |
| ClosedXML (XLWorkbook) | — | Export Excel HistoriProton | [VERIFIED: CDPController `using ClosedXML.Excel`] |

### Tidak ada library baru
Fase ini **murni logic/notif/UI refactor**. Tidak ada `npm install` / `dotnet add package`. Notif via `INotificationService` existing; audit via `AuditLogService` existing.

## Architecture Patterns

### System Architecture Diagram — alur Approve/Reject (akar T1/T2/T7)

```
                          ┌─────────────────────────────────────────┐
   Deliverable page  ───► │  ApproveDeliverable (:818)                │ ─► return RedirectToAction (ViewResult)
   (form POST)            │   • authz + section-check (Admin exempt)  │
                          │   • set role approval                     │
                          │   • RACE-GUARD reload-fresh (:882-901) ◄──┼── D-10 (HANYA di sini)
                          │   • allApproved check (:908-931)      ◄───┼── notif HC (HANYA di sini)
                          │   • CreateHCNotificationAsync (:968)      │
                          └─────────────────────────────────────────┘
                                            ▲
                                            │  DRIFT (logic ada di kiri, hilang di kanan)
                                            ▼
   CoachingProton    ───► │  ApproveFromProgress (:1939)             │ ─► return Json (modal)
   modal (AJAX POST)      │   • authz + section-check (NO Admin exem)│
                          │   • set role approval                    │
                          │   • ❌ tanpa race-guard (T7)             │
                          │   • ❌ tanpa allApproved/notif HC (T1)   │
                          └─────────────────────────────────────────┘

   SOLUSI D-01:
   ┌──────────────┐   panggil    ┌─────────────────────────────────────────────┐
   │ Endpoint A   │ ───────────► │ approve-core(progress, user, role, isSrSpv,  │
   │ (redirect)   │              │              isSH) → ApproveResult            │
   ├──────────────┤              │  • set role approval                          │
   │ Endpoint B   │ ───────────► │  • race-guard reload-fresh (D-04/T7)          │
   │ (JSON)       │              │  • set overall Approved                       │
   └──────────────┘              │  • RecordStatusHistory                        │
        ▲                        │  • allApproved check                          │
        │ authz + section-check  │  • SaveChanges                                │
        │ + response shaping     │  • notif coach/coachee + HC (jika allApproved)│
        │ TETAP di endpoint      │  → return {success, allApproved, newStatus}   │
        └────────────────────────┴───────────────────────────────────────────────┘
```

### Pola 1: Helper Extraction yang Preserve Dua Return Style (D-01) — RISIKO TERBESAR

**What:** Ekstrak DOMAIN logic ke method bersama; biarkan ENDPOINT-SPECIFIC concern (binding, authz, section-check, response shaping) tetap di tiap endpoint.

**Apa yang helper ABSORB (core):**
- Set per-role approval fields (SrSpv/SH) [VERIFIED: identik `:869-880` vs `:1977-1988`]
- Race-guard reload-fresh `AsNoTracking` + re-validasi `stillCanApprove` [VERIFIED: `:882-901`]
- Set overall `Status="Approved"` + `ApprovedAt/ById` [VERIFIED: `:903-906` vs `:1990-1993`]
- `RecordStatusHistory` (private method `:3559`, hanya butuh `_context`) [VERIFIED]
- allApproved check (load all progress track, order, `.All(Status=="Approved")`) [VERIFIED: `:908-931`]
- `SaveChangesAsync` + notif (coach/coachee `COACH_EVIDENCE_APPROVED` + HC `COACH_ALL_COMPLETE` jika allApproved) [VERIFIED: `:933-969`]
- Untuk reject-core: full chain reset (Status="Rejected" + SrSpv/SH/HC→"Pending" + null semua approver ID/timestamp + RejectedAt/Reason) [VERIFIED: `RejectDeliverable:1022-1037`]

**Apa yang helper TIDAK boleh absorb (endpoint-specific):**
- `_userManager.GetUserAsync(User)` + ambil role (butuh `User` claims principal)
- Authz: `UserRoles.HasSectionAccess(roleLevel)` → `Forbid()` vs `Json(success=false)`
- **Section-check (ADA DIVERGENSI — lihat Pitfall 1)**
- Response shaping: `RedirectToAction`/`TempData` vs `Json(new {...})`
- Guard input: `rejectionReason` empty check

**Bentuk yang direkomendasikan:** **private method di CDPController** (bukan service terpisah). Alasan:
1. Akses langsung ke `_userManager`, `_notificationService`, `_context`, `RecordStatusHistory`, `CreateHCNotificationAsync` — nol injeksi tambahan, nol perubahan DI. [VERIFIED: CDPController ctor `:41-50` punya semua dep]
2. Service terpisah harus inject `UserManager` + `INotificationService` + pindahkan `RecordStatusHistory`/`CreateHCNotificationAsync` → diff besar, risiko regresi tinggi.
3. Trade-off: private method lebih sulit di-unit-test isolasi. Mitigasi: test paritas pakai **pola predikat-replikasi** (lihat §Validation) ATAU helper menerima `ApplicationDbContext`+entity dan return result object yang bisa diuji lewat fixture SQL.

**Signature yang disarankan (planner finalize):**
```csharp
// Return object membawa apa yang endpoint butuh untuk response
private async Task<(bool ok, string? error, bool allApproved, string newStatus)>
    ApproveCoreAsync(ProtonDeliverableProgress progress, ApplicationUser user, string userRole, bool isSrSpv, bool isSH);

private async Task<(bool ok, string? error)>
    RejectCoreAsync(ProtonDeliverableProgress progress, ApplicationUser user, string userRole, bool isSrSpv, string rejectionReason);
```

### Pola 2: Predikat-Replikasi untuk Test Gate Controller-Embedded

**What:** Logic gate yang terkubur dalam controller (tak bisa dipanggil langsung tanpa HTTP context) diuji dengan **mereplikasi predikat persis** di test helper, lalu assert.
**Source:** [VERIFIED: `ProtonYearGateIntegrationTests.cs:69-78` — `SkippedByCrossYearGateAsync` mereplikasi gate `AssessmentAdminController:1372-1379`]
**When to use:** T3 (gate reaktivasi) jika tidak diekstrak ke method testable. **Catatan kelemahan:** menguji COPY logic, bukan controller asli — drift test-vs-prod mungkin. Mitigasi: jika logic diekstrak ke `private`/service method, test bisa panggil langsung (lebih kuat).

### Pola 3: Real-SQL Disposable Fixture (bukan InMemory)

**What:** Test integration pakai SQL Server nyata via DB disposable per-run.
**Source:** [VERIFIED: `ProtonCompletionFixture` di `ProtonCompletionServiceTests.cs:25-61`]
- DB `HcPortalDB_Test_{guid}` di `localhost\SQLEXPRESS`, `MigrateAsync()` penuh, drop di Dispose.
- Tag `[Trait("Category","Integration")]` → bisa di-skip di CI tanpa SQL: `dotnet test --filter "Category!=Integration"`.
- ProtonTracks sudah di-seed via migration HasData → reuse (`ctx.ProtonTracks.FirstAsync`), jangan insert (UNIQUE TrackType+TahunKe).
- Tiap [Fact] pakai `coacheeId` unik (`$"prefix-{Guid.NewGuid():N}"`) karena DB shared antar-fact dalam fixture.

### Recommended Project Structure (file yang tersentuh)
```
Controllers/
├── CDPController.cs            # T1/T2/T5/T7/T8 — helper + query + history (WAVE 1 dulu, WAVE 3 di atasnya)
├── CoachMappingController.cs   # T3 (gate reaktivasi) + T9 (log-warn) — Wave 2 paralel
└── AssessmentAdminController.cs# T9 (log-warn) + T10 (komentar by-design) — Wave 2/3
Services/
├── ProtonCompletionService.cs  # T4 (surface miss) — Wave 2
└── GradingService.cs           # T6 (drop ValidUntil hardcode) — Wave 2 paralel
Views/CDP/
└── HistoriProton.cshtml        # T5 — TIDAK perlu diubah (badge cabang-3 sudah ada :152-155)
HcPortal.Tests/
├── (baru) ProtonApproveRejectParityTests.cs  # T1/T2/T7 paritas
├── (baru/extend) ProtonYearGateIntegrationTests.cs # T3 reaktivasi
└── (baru) ProtonCompletionMissTests.cs        # T4 surface
```

### Anti-Patterns to Avoid
- **Patch per-endpoint (lawan D-01):** menambal `ApproveFromProgress` tanpa extract helper = drift balik lagi di fix berikutnya. Akar harus mati.
- **Refactor HCReview/Upload pair tanpa bukti drift:** scope creep. Pasangan `HCReviewDeliverable`/`HCReviewFromProgress` TIDAK ditemukan drift — jangan ikut. [VERIFIED: `:1142-1205` vs `:2093-2137` perilaku setara]
- **Mengubah semantik A-2:** helper tidak boleh menjadikan HC approver atau memaksa co-sign 2 L4. 1 approver L4 cukup. [LOCKED: STATE.md A-2]
- **Membangunkan tabel ProtonNotification dorman:** notif tetap via `UserNotification`/`SendAsync`. [VERIFIED: ProtonNotification tidak dipakai di jalur notif aktif]
- **Menambah `File.Delete` di T8:** kebijakan E10 keep-evidence; append history saja.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Broadcast notif ke semua HC | Loop manual + insert UserNotification | `CreateHCNotificationAsync(coacheeId)` existing | Sudah ada dedup exact-message (D-14) + iterasi GetUsersInRoleAsync(HC) [VERIFIED: `:1111-1140`] |
| Cek "Tahun N-1 lulus" | Re-query penanda + bandingkan TahunKe | `_protonCompletionService.IsPrevYearPassedAsync` | Reuse helper murni + `ProtonYearGate.IsAllowed` [VERIFIED: `ProtonCompletionService.cs:107-112`] |
| Append path ke history JSON | String concat manual | Pola `JsonSerializer.Deserialize<List<string>>` + Add + Serialize | Persis `UploadEvidence:1280-1286` — null-safe + idempotent |
| Audit log entry | Insert AuditLog manual | `_auditLog.LogAsync(actorId, actorName, actionType, desc, targetId, targetType)` | SaveChanges internal + signature stabil [VERIFIED: `AuditLogService.cs:21-42`] |
| Status history deliverable | Insert DeliverableStatusHistory | `RecordStatusHistory(progressId, type, actorId, name, role, reason?)` | Private helper existing [VERIFIED: `:3559-3571`] |
| Race-guard approver konkuren | Lock / serializable tx | reload-fresh `AsNoTracking` + re-check `stillCanApprove` | Pola D-10 sudah teruji [VERIFIED: `:882-901`] |

**Key insight:** Semua "bahan" untuk 10 fix sudah ADA di codebase. Fase ini bukan membangun yang baru, tapi **memindahkan/menyamakan** logic existing supaya tidak ada cabang yang ketinggalan. Custom solution = sumber drift baru.

## Runtime State Inventory

> Fase ini bukan rename, tapi MENGUBAH semantik data existing (T2/T3/T4). Inventory ringkas:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data (stale akibat bug pre-fix) | `ProtonDeliverableProgress` rows yang ditolak via `RejectFromProgress` sebelum fix → `HCApprovalStatus="Reviewed"` menempel (T2). [VERIFIED: `RejectFromProgress:2054-2071` tak reset HC] | **Tidak ada migration data.** D-03 belt-and-braces: reset HC di reject-core + kedua jalur resubmit → row basi dibersihkan otomatis saat resubmit berikutnya. Catat di plan: tidak ada one-shot cleanup (by-design, defensive-on-write). |
| Stored data (assignment inactive) | `ProtonTrackAssignment` inactive ber-`Origin="Bypass"` yang akan direaktivasi (T3 exempt D-06). [VERIFIED: field `Origin` ada `ProtonModels.cs:86`, nilai {null,"Bypass"}] | Code edit: query exempt baru cek `!IsActive && Origin=="Bypass"` (exempt existing `:535-537` hanya match `IsActive`). |
| Live service config | None — tidak ada config eksternal (n8n/scheduler/dll). Notif via DB internal. | None — verified by grep (notif & gate semua in-app). |
| OS-registered state | None — tidak ada task/cron. | None. |
| Secrets/env vars | None tersentuh. AD lokal `Authentication__UseActiveDirectory=false` saat UAT (existing). | None. |
| Build artifacts | None — migration=false, tidak ada `dotnet ef`/egg-info/binary baru. | None. |

**Catatan penting:** Penanda yang sudah hilang akibat T4 (miss historis sebelum fix) TIDAK otomatis terbit oleh fase ini — itu domain `BackfillProtonPenanda` (manual admin). T4 fix hanya MENCEGAH miss diam-diam ke depan (surface warning). Notif T4 mengarahkan admin ke backfill.

## Common Pitfalls

### Pitfall 1: Divergensi Section-Check antar pasangan endpoint
**What goes wrong:** `ApproveDeliverable` mengizinkan **Admin approve cross-section** (`userRole != UserRoles.Admin && ...`), sedangkan `ApproveFromProgress` TIDAK punya pengecualian Admin (langsung cek `coacheeUser.Section != user.Section`).
**Evidence:** [VERIFIED: `:858-862` (ada Admin exempt) vs `:1973-1974` (tanpa Admin exempt)]
**Why it happens:** Versi FromProgress dibuat belakangan, copy parsial.
**How to avoid:** Section-check adalah AUTHZ → **tetap di endpoint** (jangan masuk helper per D-01). Tapi planner WAJIB putuskan: pertahankan divergensi (low-risk, status quo) atau unifikasi (Admin boleh cross-section di kedua). Rekomendasi: pertahankan per-endpoint apa adanya kecuali user minta unifikasi — temuan T1/T2 tidak menyangkut section-check.
**Warning signs:** Test paritas yang menyetel section berbeda akan berperilaku beda antar endpoint — itu BENAR (bukan bug helper).

### Pitfall 2: Response `newStatus` berubah makna setelah full-chain-reset (T2)
**What goes wrong:** `RejectFromProgress` sekarang return JSON `newStatus = isSrSpv ? progress.SrSpvApprovalStatus : progress.ShApprovalStatus` — di mana role penolak diset `"Rejected"` (`:2057/2063`). Setelah reject-core menerapkan full reset (semua role → `"Pending"`), `newStatus` per-role jadi `"Pending"`, BUKAN `"Rejected"`.
**Why it happens:** D-03 menyamakan dengan `RejectDeliverable` yang reset semua role ke Pending; tapi `RejectDeliverable` return redirect (tak peduli per-role status), sedangkan FromProgress return JSON yang dikonsumsi JS modal.
**How to avoid:** Endpoint `RejectFromProgress` harus return **overall** `Status="Rejected"` di field response (bukan per-role status). Planner WAJIB cek consumer JS di `CoachingProton.cshtml`/JS terkait — apa yang dipakai dari `newStatus`. Sesuaikan response shaping di endpoint (di luar helper).
**Warning signs:** Setelah fix, badge modal menampilkan "Pending" alih-alih "Ditolak". UAT modal reject wajib di-Playwright.

### Pitfall 3: `EnsureAsync` return `false` ambigu (T4)
**What goes wrong:** `EnsureAsync` return `false` di DUA kasus berbeda: (a) tidak ada assignment aktif = **MISS** (perlu surface), (b) penanda sudah ada = idempotent normal (JANGAN surface). Caller yang cuma cek `bool` tak bisa bedakan.
**Evidence:** [VERIFIED: `ProtonCompletionService.cs:40-44` (no-assignment) vs `:48` (already-exists), keduanya `return false`]
**Why it happens:** Signature `Task<bool>` terlalu sempit untuk 3 outcome.
**How to avoid:** Ubah return jadi enum `EnsureResult { Created, AlreadyExists, NoActiveAssignment }`, ATAU lakukan surface DI DALAM branch no-assignment (`:40-43`). Lihat §T4 untuk trade-off lengkap (4 call-site + 3 test ctor terdampak).
**Warning signs:** Notif T4 terbit padahal penanda sudah ada (false alarm) = bukti caller tak bisa bedakan.

### Pitfall 4: Refactor helper SEBELUM test paritas → regresi senyap
**What goes wrong:** Mengekstrak helper lalu kedua endpoint berperilaku beda dari sebelumnya tanpa ketahuan.
**How to avoid:** Tulis test yang mem-PIN perilaku `ApproveDeliverable`/`RejectDeliverable` existing (state akhir progress untuk input X) DULU, lalu refactor, lalu hijau. Pola Wave 0 (lihat §Validation). CONTEXT eksplisit: "pastikan ApproveDeliverable existing behavior ter-pin SEBELUM refactor".
**Warning signs:** Tidak ada test approve/reject CDPController saat ini (cuma `CDPControllerAuthTests` reflection authz). Gap Wave 0 wajib.

### Pitfall 5: Drift BARU di ExportHistoriProton (T5)
**What goes wrong:** D-09 ubah status `HistoriProton` jadi 3 cabang, tapi `ExportHistoriProton` punya query+status logic IDENTIK terpisah (`:3399`) yang masih 2 cabang. User filter "Belum Mulai" di view lalu Export → hasil tidak konsisten.
**Evidence:** [VERIFIED: `HistoriProton:3149-3250` vs `ExportHistoriProton:3304-3399` — scoping & status logic ter-duplikasi; filter status `:3434`]
**Why it happens:** Sama persis akar drift T1/T2 — dua salinan logic.
**How to avoid:** Terapkan perubahan "Belum Mulai" di KEDUA method. Lebih baik: ekstrak shared private helper builder worker-rows (membunuh drift, sefilosofi D-01). Minimal: mirror perubahan. Planner putuskan (discretion).
**Warning signs:** Export Excel tidak punya baris "Belum Mulai" padahal view punya.

### Pitfall 6: T6 edge case Pass→Fail→Pass meninggalkan ValidUntil null
**What goes wrong:** Pass→Fail menset `ValidUntil=null` (`:483`). Drop hardcode di Fail→Pass (`:520-521`) berarti regrade balik ke Pass TIDAK set ValidUntil → tetap null (sertifikat tanpa masa berlaku).
**Evidence:** [VERIFIED: `:479-483` null on revoke; `:516-521` hardcode +3thn yang akan dibuang]
**Why it happens:** Jalur pass normal mengandalkan ValidUntil dari setup sesi HC; tapi setup itu mungkin sudah ke-null oleh revoke sebelumnya.
**How to avoid:** Keputusan D-10 TERKUNCI (drop hardcode, paritas jalur normal). Planner cukup CATAT edge case ini di plan + verifikasi UAT regrade flip ganda. Bila jadi isu nyata, eskalasi (bukan scope 363).
**Warning signs:** Sertifikat hasil regrade kedua punya `ValidUntil=null`.

## Code Examples

Pola terverifikasi dari source (semua dari codebase ini):

### Contoh 1: allApproved check + notif HC (yang HILANG di ApproveFromProgress — T1)
```csharp
// Source: CDPController.cs:908-969 (ApproveDeliverable) — VERIFIED 2026-06-11
// Load ALL progress records for this coachee's track (for all-approved check)
var allProgresses = await _context.ProtonDeliverableProgresses
    .Include(p => p.ProtonDeliverable).ThenInclude(d => d!.ProtonSubKompetensi).ThenInclude(s => s!.ProtonKompetensi)
    .Where(p => p.CoacheeId == progress.CoacheeId)
    .ToListAsync();
var orderedProgresses = allProgresses
    .Where(p => p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.ProtonTrackId == trackId)
    .OrderBy(...).ToList();
bool allApproved = orderedProgresses.All(p => p.Status == "Approved");
await _context.SaveChangesAsync();
// ... notif coach/coachee COACH_EVIDENCE_APPROVED ...
if (allApproved) { await CreateHCNotificationAsync(progress.CoacheeId); }
```

### Contoh 2: Full chain reset (target reject-core — T2)
```csharp
// Source: CDPController.cs:1022-1037 (RejectDeliverable) — VERIFIED 2026-06-11
progress.Status = "Rejected";
progress.RejectedAt = DateTime.UtcNow;
progress.RejectionReason = rejectionReason;
progress.ApprovedById = null;  progress.ApprovedAt = null;
progress.SrSpvApprovalStatus = "Pending"; progress.SrSpvApprovedById = null; progress.SrSpvApprovedAt = null;
progress.ShApprovalStatus = "Pending";    progress.ShApprovedById = null;    progress.ShApprovedAt = null;
progress.HCApprovalStatus = "Pending";    progress.HCReviewedById = null;    progress.HCReviewedAt = null;  // ◄── ini yang HILANG di RejectFromProgress (T2)
```

### Contoh 3: Race-guard reload-fresh (target approve-core — T7)
```csharp
// Source: CDPController.cs:882-901 (ApproveDeliverable, D-10) — VERIFIED 2026-06-11
var freshStatus = await _context.ProtonDeliverableProgresses
    .Where(p => p.Id == progressId)
    .Select(p => new { p.Status, p.SrSpvApprovalStatus, p.ShApprovalStatus })
    .AsNoTracking().FirstOrDefaultAsync();
if (freshStatus == null) return NotFound();
bool stillCanApprove = freshStatus.Status == "Submitted" ||
    (freshStatus.Status == "Approved" && (
        (isSrSpv && freshStatus.SrSpvApprovalStatus != "Approved") ||
        (isSH && freshStatus.ShApprovalStatus != "Approved")));
if (!stillCanApprove) { /* Endpoint: TempData/Json "sudah diproses approver lain" */ }
```

### Contoh 4: Gate reaktivasi T3 — analisis lokasi + exempt baru
```csharp
// LOOPHOLE (VERIFIED CoachMappingController.cs:516-545):
// cabang 1 = coachee yang punya assignment APAPUN (active/inactive) utk track diminta → SKIP gate
var hasForRequestedTrack = (await _context.ProtonTrackAssignments
    .Where(a => coacheeIdsForWarning.Contains(a.CoacheeId) && a.ProtonTrackId == req.ProtonTrackId.Value)
    .Select(a => a.CoacheeId).Distinct().ToListAsync()).ToHashSet();   // ◄── TANPA filter IsActive
foreach (var coacheeId in coacheeIdsForWarning) {
    if (hasForRequestedTrack.Contains(coacheeId)) continue;            // ◄── T3: reaktivasi lolos di sini
    // exempt existing HANYA match IsActive=true:
    bool isExemptFromCrossYear = await _context.ProtonTrackAssignments
        .AnyAsync(a => a.CoacheeId == coacheeId && a.ProtonTrackId == requestedTrack.Id && a.IsActive && a.Origin == "Bypass");
    if (isExemptFromCrossYear) continue;
    bool prevPassed = await _protonCompletionService.IsPrevYearPassedAsync(coacheeId, requestedTrack.TrackType, prevTrack.TahunKe);
    if (!prevPassed) incompleteCoachees.Add(coacheeId);
}

// FIX ARAH (D-05/D-06) — planner finalize: reaktivasi-candidate = coachee di hasForRequestedTrack
// yang TIDAK punya assignment AKTIF (hanya inactive yg akan direaktivasi :597-606).
// Untuk candidate ini, jalankan gate KECUALI inactive assignment-nya Origin="Bypass":
//   bool reactExempt = await _context.ProtonTrackAssignments
//       .AnyAsync(a => a.CoacheeId == coacheeId && a.ProtonTrackId == requestedTrack.Id && !a.IsActive && a.Origin == "Bypass");
// Reaktivasi yg tak exempt + prev belum lulus → masuk incompleteCoachees → hard-block :550.
```

### Contoh 5: Append EvidencePathHistory (target T8)
```csharp
// Source: CDPController.cs:1280-1286 (UploadEvidence) — pola yang SubmitEvidenceWithCoaching kurang
if (!string.IsNullOrEmpty(progress.EvidencePath)) {
    var pathHistory = string.IsNullOrEmpty(progress.EvidencePathHistory)
        ? new List<string>()
        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(progress.EvidencePathHistory) ?? new List<string>();
    pathHistory.Add(progress.EvidencePath);
    progress.EvidencePathHistory = System.Text.Json.JsonSerializer.Serialize(pathHistory);
}
// ── di SubmitEvidenceWithCoaching, sisipkan SEBELUM `progress.EvidencePath = ...` (:2290),
//    di dalam blok `if (evidenceBytes != null && evidenceSafeFileName != null)` (:2284)
```

### Contoh 6: Resolve HC users tanpa UserManager (opsi T4 di service)
```csharp
// HC = RoleLevel 2 [VERIFIED: UserRoles.GetRoleLevel "HC" => 2]
// Tanpa UserManager (cocok bila surface dilakukan di ProtonCompletionService):
var hcUserIds = await _context.Users.Where(u => u.RoleLevel == 2 && u.IsActive).Select(u => u.Id).ToListAsync();
// CAVEAT: RoleLevel = field denormalisasi di ApplicationUser. Sumber otoritatif role =
//   _userManager.GetUsersInRoleAsync(UserRoles.HC) (dipakai CreateHCNotificationAsync:1123).
//   NotifyReviewersAsync:1091 pakai RoleLevel==4 → preseden codebase mempercayai RoleLevel.
```

## T-by-T Deep Notes (untuk planner)

### T1/T2/T7 (Wave 1 — helper)
- `ApproveFromProgress` selain miss allApproved/HC notif, juga **tidak kirim notif coach/coachee `COACH_EVIDENCE_APPROVED`** yang ada di `ApproveDeliverable:935-964`. D-02 "paritas penuh perilaku post-approval" → helper sebaiknya sertakan notif coach/coachee juga. KONFIRMASI: ini penambahan perilaku ke FromProgress (sebelumnya senyap). Planner tandai sebagai paritas yang diinginkan.
- `RejectFromProgress` menulis `SrSpvApprovedById/At` saat REJECT (`:2058-2059`) — perilaku aneh (set approver field padahal menolak). reject-core (pola RejectDeliverable) tidak melakukan ini. Pastikan reject-core TIDAK set approver-id saat reject.

### T3 (Wave 2 — CoachMapping)
- Gate existing berjalan di blok PRE-transaction (`:500-555`); reaktivasi terjadi di blok side-effect INSIDE transaction (`:597-606`). **Rekomendasi: tempatkan gate reaktivasi di blok PRE-tx** (tempat gate existing) supaya hard-block bisa `return Json(false)` tanpa rollback. Opsi alternatif (gate di dalam loop :602) butuh `tx.RollbackAsync()` — lebih kompleks.
- Tension: komentar existing `:531-534` bilang "JANGAN ubah cabang 1". D-05 mengharuskan reaktivasi lewat gate → cabang 1's blanket-skip HARUS diperhalus (skip hanya untuk assignment AKTIF/alreadyActive; reaktivasi-candidate inactive masuk gate). Planner WAJIB rekonsiliasi komentar 360 vs keputusan 363 (lihat Open Question 1).
- Pesan error pola hard-block 359: `$"Tidak bisa assign {requestedTrack.TahunKe}: {prevTrack.TahunKe} ({requestedTrack.TrackType}) belum lulus untuk {n} coachee."` [VERIFIED: `:550-551`].

### T4 (Wave 2 — ProtonCompletion/Grading)
- 4 call-site `EnsureAsync`: `GradingService:310` (exam), `:545` (regrade Fail→Pass), `AssessmentAdminController:3764` (essay finalize, Origin="Exam"), `:3876` (interview, Origin="Interview"). Bypass (`ProtonBypassService:182`) SELALU buat assignment → tak pernah miss.
- **Rekomendasi:** surface (audit+notif) DI DALAM `ProtonCompletionService` branch no-assignment (`:40-43`) supaya 4 call-site ter-cover uniform. Inject `INotificationService` + `AuditLogService` (keduanya context-based, constructable di test). Resolve HC via `RoleLevel==2` (hindari UserManager yang sulit di-mock).
  - **Cost:** ubah ctor `ProtonCompletionService` → update 3 test file yang `new ProtonCompletionService(ctx, logger)`: `ProtonCompletionServiceTests.cs:110`, `ProtonYearGateIntegrationTests.cs:27`, `ProtonBypassServiceTests.cs:44`. + DI di `Program.cs:57` (auto-resolve, tidak perlu ubah jika service lain sudah scoped).
- **Alternatif:** ubah return `Task<bool>`→`Task<EnsureResult>` enum; handle surface di caller. Tapi `GradingService` tak punya notif/audit dep (harus inject) — non-uniform, lebih banyak titik.
- Actor untuk audit (system context): `session.CreatedBy` atau literal "system"/"grading". `AuditLogService.LogAsync` butuh `actorUserId` + `actorName` (string, bukan principal). [VERIFIED: signature `:21-27`].
- Notif T4 actionable: sebut nama coachee + track + sessionId, arahkan ke `/Admin/...` (BackfillProtonPenanda di ManageAssessment). Tipe baru `PROTON_PENANDA_MISS` (discretion). Catat: `COACH_ALL_COMPLETE` existing TIDAK terdaftar di `_templates` dict — dikirim via `SendAsync` langsung (string hardcode). Jadi tipe baru boleh ikut pola SendAsync langsung, tak wajib daftar template.

### T5 (Wave 3 — query)
- View `HistoriProton.cshtml` SUDAH siap: badge cabang-3 "Belum Mulai" (`:152-155` else-branch) + filter option (`:79`). **Hanya query + status string yang perlu diubah.**
- D-09: tambah coachee dengan `CoachCoacheeMappings.IsActive` TANPA `ProtonTrackAssignment` → row status "Belum Mulai". Saat ini scoping HANYA dari ProtonTrackAssignments (`:3149-3167`) lalu `coacheeIdsWithAssignments` (`:3180`). Perlu union: coachee mapping-aktif role-scoped yang belum ada di set assignment.
- Definisi presisi (CONTEXT): "Belum Mulai" = coachee ber-mapping aktif TANPA assignment track manapun (bukan semua user).
- **WAJIB mirror ke `ExportHistoriProton`** (Pitfall 5).

### T6 (Wave 2 — Grading)
- Hapus `var validUntil = DateOnly.FromDateTime(certNow).AddYears(3);` (`:516`) + `.SetProperty(r => r.ValidUntil, validUntil)` (`:521`). Sisakan `.SetProperty(r => r.NomorSertifikat, nomor)` (`:520`). Paritas dgn `GradeAndCompleteAsync:282-287` (NomorSertifikat saja).
- JANGAN sentuh blok Pass→Fail revoke (`:479-483`, set null = benar) maupun hook bypass `:496/:549`.

### T8 (Wave 3 — CDPController)
- Sisipkan append (Contoh 5) di `SubmitEvidenceWithCoaching` dalam blok `if (evidenceBytes != null && evidenceSafeFileName != null)` (`:2284`), SEBELUM `progress.EvidencePath = ...` (`:2290`). File fisik lama tidak dihapus (E10).

### T9 (Wave 2/3 — defensive)
- `AssessmentAdminController:1348-1352`: log-warn jika `protonUrutan > 1 && prevTahunKe == null`.
- `CoachMappingController:506-509`: log-warn jika `requestedTrack.Urutan > 1 && prevTrack == null`.
- Tanpa throw/blok; gate tetap (null prev untuk Urutan=1 = Tahun 1 sah).

### T10 (Wave 3 — doc)
- Tambah komentar di `BackfillProtonPenanda` (`AssessmentAdminController:3915`) menjelaskan tanpa year-gate = by-design (tambal historis pre-358). Catat di `363-FINDINGS.md`. Nol logic.

## State of the Art

Tidak relevan (internal-codebase refactor, bukan adopsi teknologi baru). Semua pola sudah established di proyek (Phase 65 AJAX, Phase 117 status history, Phase 296 GradingService, Phase 358-360 ProtonCompletion/Bypass).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Konsumer JS modal CoachingProton memakai `newStatus` dari response Reject/Approve untuk update badge | Pitfall 2 | Jika ya & tak disesuaikan, badge tampil "Pending" alih-alih "Ditolak" pasca-T2. Planner WAJIB cek JS sebelum finalize response shaping. |
| A2 | `RoleLevel==2` reliabel mengidentifikasi semua HC user (denormalisasi tersinkron) | Contoh 6 / T4 | Jika drift, sebagian HC tak dapat notif T4. Mitigasi: pakai `GetUsersInRoleAsync(HC)` (otoritatif) walau butuh UserManager. |
| A3 | Test count "211 hijau" (prompt) — terhitung 172 atribut `[Fact]/[Theory]` di 26 file (Theory + InlineData mengembang ke >211) | Validation | Jika baseline beda, angka regresi di plan perlu dikoreksi (jalankan `dotnet test` untuk angka pasti). |
| A4 | Pasangan `HCReviewDeliverable`/`HCReviewFromProgress` tidak punya drift bermakna | Anti-Patterns | Jika ternyata drift, ada temuan tambahan di luar T1-T10 (di luar scope; catat backlog). |
| A5 | Gate reaktivasi paling bersih di blok PRE-tx (bukan dalam loop side-effect) | T3 notes | Jika data scope di pre-tx tak cukup untuk identifikasi reaktivasi-candidate, planner perlu gate dalam tx + rollback. |

**Catatan:** Mayoritas klaim di research ini `[VERIFIED]` via pembacaan source langsung. Asumsi di atas adalah titik yang BELUM diverifikasi runtime (JS consumer, sinkronisasi RoleLevel, angka test pasti) dan perlu konfirmasi planner/eksekutor.

## Open Questions

1. **Rekonsiliasi komentar "JANGAN ubah cabang 1" (360 D-06b) vs gate reaktivasi (363 D-05).**
   - What we know: Komentar `CoachMappingController:531-534` melindungi cabang 1 (skip blanket) demi defense-in-depth bypass. D-05 mengharuskan reaktivasi lewat gate → cabang 1 harus diperhalus.
   - What's unclear: Apakah memperhalus cabang 1 (skip hanya assignment AKTIF; reaktivasi-inactive masuk gate) melanggar maksud 360, atau justru kompatibel (bypass tetap exempt via Origin check).
   - Recommendation: Perhalus cabang 1 dengan exempt `!IsActive && Origin=="Bypass"` (D-06) — semantik bypass tetap utuh, loophole tertutup. Update komentar `:531-534` mencerminkan keputusan 363.

2. **Response shaping `RejectFromProgress` pasca full-reset (T2).**
   - What we know: full-reset menjadikan per-role status "Pending"; overall Status "Rejected".
   - What's unclear: field apa yang dipakai JS modal.
   - Recommendation: return overall `Status` di JSON; cek `Views/CDP/CoachingProton.cshtml` + JS sebelum finalize. UAT Playwright modal reject.

3. **Bentuk surface T4: di service (uniform, ubah ctor + 3 test) vs di caller (non-uniform).**
   - Recommendation: di `ProtonCompletionService` (uniform 4 call-site), inject `INotificationService`+`AuditLogService`, resolve HC via RoleLevel==2. Trade-off test-ctor diterima.

4. **Cakupan anti-drift T5: mirror manual vs ekstrak shared builder.**
   - Recommendation: minimal mirror ke `ExportHistoriProton`; ideal ekstrak builder worker-rows (sefilosofi D-01). Planner discretion.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8 | build + test + run | ✓ (asumsi — proyek aktif net8.0) | net8.0 | — |
| SQL Server `localhost\SQLEXPRESS` | Integration tests (real-SQL fixture) + DB lokal HcPortalDB_Dev | ✓ (dipakai fixture existing + dev) | SQLEXPRESS | `dotnet test --filter "Category!=Integration"` (skip fixture SQL) |
| Playwright (MCP/CLI) | UAT modal approve/reject + HistoriProton @localhost:5277 | ✓ (dipakai fase 354-362) | — | UAT manual browser |

**Missing dependencies dengan no fallback:** Tidak ada. Migration=false → tidak perlu `dotnet ef`.
**Catatan UAT (CLAUDE.md):** `dotnet run` cek `http://localhost:5277`; AD lokal jalankan dengan `Authentication__UseActiveDirectory=false`. ❌ Jangan edit kode/DB di Dev/Prod. Promosi Dev = tanggung jawab IT (notify commit hash + flag migration=false).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (.NET 8.0) [VERIFIED: csproj] |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (tanpa runsettings; isolasi via `[Trait("Category","Integration")]`) |
| Quick run command | `dotnet test --filter "Category!=Integration"` (skip fixture SQL — cepat, untuk per-task) |
| Full suite command | `dotnet test` (butuh `localhost\SQLEXPRESS`; jalankan Integration penuh) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command / Strategi | File Exists? |
|--------|----------|-----------|------------------------------|-------------|
| T1 | approve TERAKHIR (kedua endpoint) → allApproved → notif HC `COACH_ALL_COMPLETE` terbit | integration (SQL fixture) | seed track+progress, panggil approve-core/endpoint, assert UserNotification HC ada | ❌ Wave 0 |
| T2 | reject (kedua endpoint) → SrSpv/SH/HC semua "Pending" + null ID/timestamp | integration | seed approved+reviewed progress, reject, assert HCApprovalStatus=="Pending" | ❌ Wave 0 |
| T1/T2 | **paritas**: state akhir IDENTIK utk input sama antar pasangan | integration | jalankan helper utk skenario sama via kedua jalur, assert state-equal | ❌ Wave 0 |
| T7 | approve konkuren → race-guard reload-fresh menolak approve kedua | integration | simulasikan state berubah antara load & write, assert stillCanApprove=false | ❌ Wave 0 |
| T3 | reaktivasi assignment inactive Tahun N tanpa N-1 lulus → diblok | integration (extend ProtonYearGate) | seed inactive assignment Tahun 2 tanpa penanda Tahun 1, predikat reaktivasi → blocked | ⚠️ extend `ProtonYearGateIntegrationTests.cs` |
| T3 (exempt) | reaktivasi assignment inactive `Origin="Bypass"` → lolos | integration | seed inactive Bypass, assert tidak diblok | ⚠️ extend |
| T4 | EnsureAsync no-active-assignment → surface (audit + notif HC), bukan silent | integration | seed exam pass tanpa assignment aktif, assert AuditLog + UserNotification HC | ❌ Wave 0 |
| T4 (idempotent) | penanda sudah ada → TIDAK surface (false-alarm guard) | integration | seed penanda existing, assert tak ada notif baru | ❌ Wave 0 |
| T5 | coachee mapping-aktif tanpa assignment → row status "Belum Mulai" | integration | seed mapping aktif tanpa ProtonTrackAssignment, assert worker row status | ❌ Wave 0 |
| T6 | regrade Fail→Pass TIDAK set ValidUntil (paritas jalur normal) | integration | regrade flip, assert ValidUntil tidak di-hardcode +3thn | ❌ Wave 0 (berat — butuh setup package/session) |
| T8 | SubmitEvidenceWithCoaching overwrite → path lama masuk EvidencePathHistory | integration | seed progress ber-EvidencePath, submit ulang, assert history JSON berisi path lama | ❌ Wave 0 |
| T9 | prev null & Urutan>1 → log warning (gate tetap jalan) | manual/inspection | sulit assert log; verifikasi via code review + opsional ILogger mock | ⚠️ manual |
| T10 | by-design (komentar) | none | tidak ada test (dokumentasi) | N/A |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (unit cepat) + `dotnet build` (CLAUDE.md).
- **Per wave merge:** `dotnet test` penuh (Integration + unit) di `localhost\SQLEXPRESS`.
- **Phase gate:** Full suite hijau + UAT Playwright @5277 (approve TERAKHIR via CoachingProton → notif HC; reject modal → chain reset; reaktivasi cross-year diblok; HistoriProton "Belum Mulai" tampil) sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ProtonApproveRejectParityTests.cs` — pin perilaku ApproveDeliverable/RejectDeliverable existing + paritas (T1/T2/T7). **WAJIB sebelum refactor D-01.**
- [ ] Extend `HcPortal.Tests/ProtonYearGateIntegrationTests.cs` — skenario reaktivasi (T3) + exempt Bypass inactive (D-06).
- [ ] `HcPortal.Tests/ProtonCompletionMissTests.cs` — surface T4 (audit+notif) + guard idempotent.
- [ ] (opsional) test T5 query "Belum Mulai" + T8 append history (bisa gabung ke file Proton existing).
- [ ] Strategi pin pre-refactor: karena CDPController berat di-construct, gunakan helper yang menerima `ApplicationDbContext`+entity (testable via fixture) ATAU predikat-replikasi. Putuskan saat Wave 0.
- Framework install: tidak perlu — xUnit + fixture sudah ada.

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Server-side gate authorization (T3) — keputusan tidak boleh di JS (preseden PCOMP-06/07) |
| V4 Access Control | **yes (utama)** | T3 year-gate reaktivasi = kontrol akses progresi; T1/T2 chain approval L4-only via `[Authorize(RolesReviewerAndAbove)]` + `HasSectionAccess`; section-check per-endpoint (Pitfall 1) |
| V5 Input Validation | yes (existing) | `rejectionReason` not-empty; file ext/size (UploadEvidence/SubmitEvidence); JSON deserialize try/catch — semua sudah ada, jangan regres |
| V7 Error/Logging | yes | T4 audit log (AuditLogService) untuk miss penanda; T9 log-warn; no info-leak (pesan generik, detail ke log — preseden Phase 334 D6) |
| V6 Cryptography | no | Tidak ada operasi kripto baru |
| V3 Session Mgmt | no | Pakai Identity existing; tak diubah |

### Known Threat Patterns untuk fase ini (STRIDE)

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Reaktivasi assignment Tahun N untuk lompati prasyarat Tahun N-1 (T3) | **Elevation of Privilege** | Gate `IsPrevYearPassedAsync` di jalur reaktivasi (D-05) + exempt eksplisit Origin="Bypass" (D-06); hard-block server-side |
| Approve/reject ganda konkuren → lost update / double history (T7) | Tampering | Race-guard reload-fresh AsNoTracking + re-validasi (D-04, port D-10 ke FromProgress) |
| HCApprovalStatus basi survive rejection → HC review "Reviewed" menempel di evidence ditolak (T2) | Tampering (integritas state) | Full chain reset di reject-core + belt-and-braces resubmit (D-03) |
| Notif T4 bocor info kelulusan/coachee ke non-HC | **Information Disclosure** | Broadcast HANYA ke role HC (RoleLevel==2 / GetUsersInRoleAsync(HC)); jangan ke coach/coachee/atasan |
| Notif HC `COACH_ALL_COMPLETE` (T1) silent miss → kontrol kelengkapan terlewat | (Completeness, bukan security murni) | allApproved check + CreateHCNotificationAsync di approve-core, dedup exact-message existing (D-14) |
| Audit miss penanda tanpa actor jelas (T4) | Repudiation | AuditLogService.LogAsync dengan actor=system/grading + deskripsi coachee+track+session |
| Pesan error gate expose detail internal | Information Disclosure | Pesan ramah generik (pola hard-block 359), detail teknis hanya ke `_logger` (preseden Phase 334/356) |

**Catatan authz terkunci:** A-2 (1 approver L4 cukup, HC bukan approver deliverable) TIDAK boleh berubah oleh fix manapun. Helper approve/reject mempertahankan `[Authorize(Roles=RolesReviewerAndAbove)]` + `HasSectionAccess(roleLevel)` (level==4) + section-match.

## Sources

### Primary (HIGH confidence — pembacaan source langsung 2026-06-11)
- `Controllers/CDPController.cs` — ApproveDeliverable `:818-973`, RejectDeliverable `:978-1080`, CreateHCNotificationAsync `:1111-1140`, UploadEvidence reset `:1280-1357`, ApproveFromProgress `:1939-2013`, RejectFromProgress `:2018-2091`, HCReview pair `:1142-1205`/`:2093-2137`, SubmitEvidenceWithCoaching `:2142-2391`, HistoriProton `:3138-3288`, ExportHistoriProton `:3290-3460`, RecordStatusHistory `:3559-3571`, ctor `:41-50`.
- `Controllers/CoachMappingController.cs` — CoachCoacheeMappingAssign `:456-675` (gate `:500-555`, exempt `:535-538`, prev-track `:506-509`, reaktivasi `:578-627`).
- `Controllers/AssessmentAdminController.cs` — gate eligibility `:1339-1389`, prev-track resolve `:1348-1352`, exempt `:1372-1379`, FinalizeEssay hook `:3758-3770`, SubmitInterviewResults `:3786-3884`, BackfillProtonPenanda `:3909-4007`.
- `Services/ProtonCompletionService.cs` — EnsureAsync `:36-64`, RemoveExamOriginAsync `:70-86`, GetPassedYearsAsync `:92-101`, IsPrevYearPassedAsync `:107-112`, ProtonYearGate `:120-131`.
- `Services/GradingService.cs` — ctor `:24-37`, GradeAndCompleteAsync cert `:269-301` + proton hook `:306-315`, RegradeAfterEditAsync `:437-555` (revoke `:479-483`, hardcode ValidUntil `:516-521`, hooks `:494-549`).
- `Services/NotificationService.cs` — `_templates` `:34-101`, SendAsync `:108-134`, SendByTemplateAsync `:239-273`.
- `Services/AuditLogService.cs` — LogAsync `:21-42`.
- `Models/UserRoles.cs` — role constants + GetRoleLevel (HC=2, L4=SectionHead/SrSupervisor) `:6-92`.
- `Models/ProtonModels.cs` — ProtonTrackAssignment (IsActive/DeactivatedAt/Origin) `:71-87`.
- `Views/CDP/HistoriProton.cshtml` — filter `:79`, badge cabang-3 `:144-155`.
- `HcPortal.Tests/` — ProtonCompletionServiceTests.cs (fixture `:25-61`), ProtonYearGateIntegrationTests.cs (predikat-replikasi `:69-78`), CDPControllerAuthTests.cs, ProtonBypassEndpointTests.cs (reflection authz), HcPortal.Tests.csproj.

### Secondary (MEDIUM — dokumen planning proyek)
- `.planning/phases/363-.../363-CONTEXT.md` (13 keputusan terkunci + scout line numbers).
- `.planning/phases/363-.../363-FINDINGS.md` (10 temuan + evidence).
- `.planning/STATE.md` (A-2/A-3/A-4 locked decisions), `.planning/REQUIREMENTS.md`.
- `CLAUDE.md` (Develop Workflow + Seed Workflow), `.planning/config.json` (nyquist_validation=true).

### Tertiary (LOW)
- Tidak ada — fase internal, tidak ada klaim dari web/training yang belum diverifikasi di codebase.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versi terbaca langsung dari csproj.
- Architecture (helper extraction): HIGH — kedua endpoint dibaca penuh, divergensi terpetakan baris-per-baris.
- T3 gate reaktivasi: HIGH untuk diagnosis loophole; MEDIUM untuk lokasi fix final (pre-tx vs in-tx = Open Q 1/5).
- T4 surface: HIGH untuk diagnosis (return bool ambigu); MEDIUM untuk bentuk solusi (Open Q 3).
- Pitfalls: HIGH — semua dari pembacaan source.
- Test landscape: HIGH — file & pola dibaca; angka test pasti perlu `dotnet test` (A3).

**Research date:** 2026-06-11
**Valid until:** 2026-07-11 (stable internal codebase; invalidasi bila CDPController/CoachMapping/Grading diubah fase lain sebelum 363 dieksekusi — cek konflik dgn 360/367 yang juga sentuh GradingService/cascade).
