# Phase 365: Test-hardening Coach×Coachee — AF-3 xUnit - Context

**Gathered:** 2026-06-12
**Status:** Ready for planning

<domain>
## Phase Boundary

Kunci (lock) perilaku graduate `MarkMappingCompleted` (`Controllers/CoachMappingController.cs:1116`) dengan xUnit baru `MarkMappingCompletedTests` supaya regresi pada AF-3 D-03/D-04 ketahuan otomatis. Perilaku produksi sudah ada (diterapkan di Phase 356) — fase ini **mengunci**, bukan mengubah fungsi.

Scope opsi (b) dari backlog 999.5: **xUnit saja**. Varian e2e Playwright re-assign-after-graduate + race harness AF-6 (butuh fixture Tahun-2+) TETAP di backlog.

**Yang dikunci (AF-3 D-03/D-04):** `IsCompleted=true` + `CompletedAt`/`EndDate` di-stamp + `IsActive=false` (membebaskan unique-index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` → coachee re-assignable) + cascade deactivate `ProtonTrackAssignment` aktif (`IsActive=false` + `DeactivatedAt`) + histori progress utuh (BUKAN `RemoveRange`).

</domain>

<decisions>
## Implementation Decisions

### Infrastruktur & Testability (Area 1)

- **D-01:** **Extract static core** `MarkMappingCompletedCore` mengikuti pola Phase 363 (`ApproveDeliverableCoreAsync`/`RejectDeliverableCoreAsync`). Endpoint controller jadi wrapper tipis. **Alasan:** endpoint deref `_userManager.GetUserAsync(User)` di baris PERTAMA (`:1118`) — sebelum lookup mapping — jadi trik null-substitute UserManager (pola `OrganizationControllerTests`/`OrgLabelControllerTests`) GAGAL; dan repo TIDAK punya preseden UserManager-double. Core extraction membuat logika testable tanpa UserManager.
- **D-02:** **Core murni** — berisi: validasi guard (kembalikan tuple `(bool ok, string? error)` pola 363) + mutasi flag/timestamp + cascade deactivate `ProtonTrackAssignment`. **TANPA** `BeginTransactionAsync` di dalam core (core stateless terhadap transaksi).
- **D-03:** **Transaksi + audit log + resolve-user tetap di wrapper.** Controller: resolve user (`Challenge()` bila null) → `BeginTransactionAsync` → panggil core → bila ok `CommitAsync` + `_auditLog.LogAsync` (post-commit) + `TempData["Success"]`; bila gagal `RollbackAsync` + `TempData["Error"]`. Side-effect (audit, TempData, redirect) TIDAK masuk core.
- **D-04:** **Test core via real-SQL `ProtonCompletionFixture`** (sudah ada di `HcPortal.Tests/ProtonCompletionServiceTests.cs:25`, `UseSqlServer`, `IClassFixture`/`IAsyncLifetime`). **Alasan:** hanya real SQL yang meng-enforce `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` — bukti D-03 (index bebas → re-assignable) tak bisa diuji nyata di InMemory.
- **D-05 (helper ikut):** `IsYearCompletedAsync` (`:1100`, private; query `ProtonDeliverableProgresses` + `ProtonFinalAssessments`) WAJIB ikut diekstrak/dijadikan static agar core bisa memvalidasi kelengkapan Tahun 3 tanpa instance controller.

### Cakupan Skenario (Area 2)

- **D-06:** **Standard 5-6 [Fact]:**
  1. **Happy** — Tahun 3 lulus → `ok=true` + full end-state mutasi (lihat D-09).
  2. **Re-assignability** — pasca-graduate, insert `CoachCoacheeMapping` aktif baru untuk coachee yang sama → **sukses** (unique-index bebas; bukti D-03 — real-SQL only).
  3. **Guard no-Tahun3** — coachee tanpa assignment Tahun 3 → `(false, error)`, TANPA mutasi.
  4. **Guard Tahun3-belum-lulus** — Tahun 3 ada tapi belum complete (tidak semua progress `Approved` / tak ada `ProtonFinalAssessment`) → `(false, error)`, TANPA mutasi.
  5. **Mapping null / not-found** → jalur not-found (core balikkan `(false, error)` atau wrapper `NotFound()` — finalkan saat planning sesuai bentuk core).
  6. **Histori progress utuh** — jumlah baris progress TAK berubah setelah graduate (tak ter-`RemoveRange`).

### Kedalaman Asersi (Area 3)

- **D-07:** **Full end-state pada core.** Happy path assert: `IsCompleted==true` + `IsActive==false` + `CompletedAt`/`EndDate` non-null + setiap cascaded `ProtonTrackAssignment` `IsActive==false` + `DeactivatedAt` non-null + `cascadeCount == jumlah assignment aktif sebelum graduate` + jumlah baris progress TIDAK berubah.
- **D-08:** **Guard assert:** `ok==false` + `error` non-empty (cek **token kunci** saja, BUKAN verbatim penuh — hindari brittle microcopy lock) + mapping & assignment TIDAK termutasi (tetap `IsActive=true`, `IsCompleted=false`).
- **D-09:** **Audit log TIDAK di-assert** di xUnit (ada di wrapper post-commit; perilaku tak berubah; assert butuh test level-controller + UserManager-double yang sengaja dihindari).

### Claude's Discretion
- Bentuk signature core final (apakah mapping-null ditangani di core vs wrapper) — finalkan saat planning agar konsisten dgn pola 363.
- Strategi seed fixture (reuse `SeedProgressChainAsync`-style dari `ProtonApproveRejectParityTests` + tambah Tahun-3 chain + `ProtonFinalAssessment` + `CoachCoacheeMapping`).
- Apakah wrapper diberi 1 smoke-test ringan opsional (di luar core) — opsional, bukan keharusan.

### Folded Todos
Tidak ada todo yang di-fold (1 todo pending tapi `match-phase` mengembalikan 0 relevansi).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents (researcher, planner) WAJIB baca ini sebelum planning/implement.**

### Target produksi (yang di-refactor → core)
- `Controllers/CoachMappingController.cs:1112-1179` — `MarkMappingCompleted` (method yang diekstrak jadi wrapper + core).
- `Controllers/CoachMappingController.cs:1100-1110` — `IsYearCompletedAsync` (private helper; ikut diekstrak/static per D-05).
- `Controllers/CoachMappingController.cs:646` — referensi nama unique-index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (pola catch DbUpdateException).

### Pola test yang ditiru (WAJIB ditiru, bukan InMemory)
- `HcPortal.Tests/ProtonApproveRejectParityTests.cs` — pola panggil **static core langsung** + `SeedProgressChainAsync` + DB shared antar-fact (coacheeId unik per fact).
- `HcPortal.Tests/ProtonCompletionServiceTests.cs:25,42` — definisi `ProtonCompletionFixture` (real-SQL `UseSqlServer`, `IClassFixture`/`IAsyncLifetime`) yang dipakai ulang Phase 365.
- `.planning/phases/363-audit-fix-alur-proton-temuan-verifikasi-t1-t10/363-01-PLAN.md` — preseden ekstraksi core parity-locked (referensi prosedur).

### Model & index
- `Models/CoachCoacheeMapping.cs` — field `IsActive:24`, `EndDate:34`, `IsCompleted:47`, `CompletedAt:50`.
- `Models/ProtonModels.cs` — `ProtonTrackAssignment` (`IsActive`, `DeactivatedAt`).
- Index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (di Migrations; enforce hanya di real SQL).

### Sumber acuan AF-3
- `.planning/phases/356-audit-fix-assign-coach-coachee-pastikan-fungsi-assign-benar-/356-05-SUMMARY.md` — evidence AF-3 (Temuan 4): graduate set IsActive=0+IsCompleted=1, re-assignability, cascade code-verified.
- `.planning/phases/356-.../356-CONTEXT.md` — keputusan asli AF-3 D-03/D-04.

### Diabaikan untuk fase ini
- `HcPortal.Tests/OrganizationControllerTests.cs` — pola InMemory yang DISEBUT di SC roadmap; **TIDAK dipakai** karena UserManager deref + transaksi + unique-index (lihat D-01/D-04).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ProtonCompletionFixture` (real-SQL disposable) — dipakai ulang sebagai `IClassFixture` untuk `MarkMappingCompletedTests`.
- Pola seed `SeedProgressChainAsync` (`ProtonApproveRejectParityTests`) — basis seed chain track→kompetensi→sub→deliverable→progress; perlu ditambah Tahun-3 + `ProtonFinalAssessment` + `CoachCoacheeMapping`.
- Pola static-core test (panggil `XxxCoreAsync(ctx, ...)` langsung, verifikasi via `ApplicationDbContext` kedua + `AsNoTracking`).

### Established Patterns
- **363 core extraction:** controller endpoint dipecah jadi wrapper tipis (UserManager/transaksi/notif) + static core (logika DB murni, return `(ok, error)`). Phase 365 mengikuti pola ini.
- Semua test controller existing menghindari `GetUserAsync` via null-substitute — TIDAK berlaku di sini (D-01).

### Integration Points
- `CoachMappingController.MarkMappingCompleted` + `IsYearCompletedAsync` — satu-satunya file produksi yang disentuh (`Controllers/CoachMappingController.cs`).
- File test baru: `HcPortal.Tests/MarkMappingCompletedTests.cs`.

</code_context>

<specifics>
## Specific Ideas

⚠️ **AMENDEMEN ROADMAP DIPERLUKAN.** SC#2 Phase 365 berbunyi "zero file produksi berubah (git diff Controllers/ Services/ kosong)". Keputusan D-01 (extract core, pola 363) **melonggarkan** ini menjadi: *"refactor behavior-preserving + parity-locked — `Controllers/CoachMappingController.cs` disentuh (extract core + wrapper tipis), zero behavior change, dibuktikan via test core + build hijau."* Migration tetap `false`. Planner/user harus update SC#2 ROADMAP saat planning agar verifier tidak gagal di gate "git diff Controllers/ kosong".

</specifics>

<deferred>
## Deferred Ideas

- **AF-6 race harness** — uji unique-index race (butuh fixture Tahun-2+, multi-thread). Tetap backlog 999.5.
- **e2e Playwright re-assign-after-graduate** — varian (a) backlog 999.5.
- **Rollback-on-failure test** — butuh test level-controller (UserManager-double) karena transaksi di wrapper (D-03). Ditolak demi hindari boilerplate.
- **Assert baris audit log** — `MarkMappingCompleted` audit di wrapper; butuh UserManager-double. Ditolak (D-09).

### Reviewed Todos (not folded)
- 1 todo pending — `todo match-phase 365` mengembalikan 0 relevansi; tidak terkait scope test-hardening ini.

</deferred>

---

*Phase: 365-test-hardening-coach-coachee-af-3-xunit*
*Context gathered: 2026-06-12*
