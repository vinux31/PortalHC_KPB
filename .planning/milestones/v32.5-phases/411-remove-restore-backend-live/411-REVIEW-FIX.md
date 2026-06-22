---
phase: 411-remove-restore-backend-live
fixed_at: 2026-06-21T06:49:33Z
review_path: .planning/phases/411-remove-restore-backend-live/411-REVIEW.md
iteration: 1
findings_in_scope: 4
fixed: 4
skipped: 0
status: all_fixed
---

# Phase 411: Code Review Fix Report

**Fixed at:** 2026-06-21T06:49:33Z
**Source review:** .planning/phases/411-remove-restore-backend-live/411-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 4 (WR-01, WR-02, WR-03, IN-03)
- Fixed: 4
- Skipped: 0

**Out of scope (NOT touched, per instruction):** IN-01 (Proton-reject message konsistensi — kosmetik, aman), IN-02 (soft-removed masih tampil di `EditAssessment` query — kandidat scope Phase 412, view query tidak disentuh), IN-04 (refactor resolve-actor helper — opsional).

**Build:** `dotnet build HcPortal.csproj` + `HcPortal.Tests.csproj` → 0 errors (24 pre-existing nullability warnings, tak terkait perubahan).
**Test:** `dotnet test --filter "FullyQualifiedName~FlexibleParticipantRemove"` → **16/16 PASS** (termasuk test IN-03 baru). Full quick suite → **597/597 PASS**, 0 gagal, 0 skip (tanpa regresi).

## Fixed Issues

### WR-01: Audit soft-remove tidak atomik dengan mutasi

**Files modified:** `Controllers/AssessmentAdminController.cs`
**Commit:** `da2d2b8a`
**Applied fix:** Membungkus mutasi 3 kolom soft-remove + `_auditLog.LogAsync` dalam satu transaksi eksplisit (`BeginTransactionAsync` → mutasi → `SaveChangesAsync` → audit → `CommitAsync`), cermin pola `AddParticipantsLive:2455-2459` + preseden `RecordCascadeDeleteService:252-253`. Ditambah `try/catch` yang `RollbackAsync` lalu re-throw bila `LogAsync` gagal — sehingga soft-remove TIDAK ter-commit tanpa audit row (non-repudiation T-411-06). `LogAsync` flush internal tetap belum commit hingga `tx.CommitAsync()` → atomik.

### WR-02: Soft loop menimpa metadata removal partner yang sudah soft-removed

**Files modified:** `Controllers/AssessmentAdminController.cs`
**Commit:** `da2d2b8a`
**Applied fix:** Loop soft-remove diubah dari `.Where(x => x != null)` menjadi `.Where(x => x != null && x.RemovedAt == null)`, cermin guard restore loop di `:2600`. Mencegah penimpaan `RemovedAt`/`RemovedBy`/`RemovalReason` partner yang sudah soft-removed lebih dulu (state-drift) — konteks audit asli partner terjaga.

### WR-03: Hard-delete pasangan Pre/Post tidak atomik lintas dua sesi

**Files modified:** `Controllers/AssessmentAdminController.cs`
**Commit:** `da2d2b8a`
**Applied fix:** Risiko residual didokumentasikan eksplisit dalam komentar kode (single-tx-per-cascade adalah desain `RecordCascadeDeleteService`; nested-tx pada ctx berbagi akan konflik → tak dibungkus satu tx tanpa refactor besar; pra-validasi `anyHasData==false` membuat kegagalan langka). Ditambah pelacakan `deletedIds`: bila cascade partner gagal SETELAH ada sesi yang sudah terhapus, `_logger.LogError` menulis "PAIR-HALF-DELETED — pasangan Pre/Post terhapus separuh, butuh rekonsiliasi manual" dengan `sessionId(s) sudah-terhapus`, `partnerId gagal`, title, dan alasan — agar separuh-pair terdeteksi ops (bukan silent), lalu return `Fail` jelas.

### IN-03: Test coverage — restore Pre/Post pair (clear KEDUA partner) belum ada

**Files modified:** `HcPortal.Tests/FlexibleParticipantRemoveTests.cs`
**Commit:** `e51dc99b`
**Applied fix:** Menambah test `RestorePrePost_Pair_ClearsBothPartners` (B7/IN-03). De-tautologis: seed pasangan Pre(berdata)+Post(bersih) cross-linked via `LinkedSessionId` → drive `RemoveParticipantLive(preId)` ASLI (mode=soft, sanity assert KEDUA partner `RemovedAt!=null` sebelum restore) → drive `RestoreParticipantLive(preId)` ASLI → assert KEDUA partner (preId DAN postId) punya `RemovedAt`/`RemovedBy`/`RemovalReason == null` reload NYATA dari SQLEXPRESS (simetri restore PRMV-04/05, loop `:2600`). PASS.

---

_Fixed: 2026-06-21T06:49:33Z_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
