# Phase 427: Exam Token-Gate Server-Authoritative - Context

**Gathered:** 2026-06-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Verifikasi token masuk ujian menjadi server-authoritative & persisten — disimpan di kolom DB baru `AssessmentSession.TokenVerifiedAt` (`DateTime?` nullable) menggantikan `TempData.Peek`. Di-stamp saat `VerifyToken` sukses, dibaca di gate `StartExam`, di-reset (`=null`) saat retake/reset agar gate token re-arm konsisten pada percobaan baru. Requirement: **EXSEC-01** (sumber: backlog 999.13 / FLOW-08).

🔑 KEYSTONE milestone v32.8. **migration=TRUE** (`AddTokenVerifiedAt`). Sequential-before Phase 428 (sama-sama edit `StartExam` di `CMPController.cs`).

Out of scope: refactor write-on-GET StartExam (= Phase 428 / EXSEC-02); perubahan logika token-compare `AccessTokenMatches` (tetap); merge lintas-branch (R-1, bukan fase).
</domain>

<decisions>
## Implementation Decisions

### Titik Reset TokenVerifiedAt
- **D-01:** Reset `TokenVerifiedAt = null` **single source di `Services/RetakeService.cs` `ExecuteAsync`** — tambah `.SetProperty(r => r.TokenVerifiedAt, (DateTime?)null)` pada `ExecuteUpdateAsync` yang sudah me-reset `StartedAt` (RetakeService.cs ~line 115). Otomatis cover KEDUA jalur retake: worker `CMPController.RetakeExam` (line ~2585) DAN HC `AssessmentAdminController.ResetAssessment` (line ~4411) — keduanya memanggil `ExecuteAsync`. Atomik (satu ExecuteUpdateAsync), satu titik kebenaran, tahan drift.

### TempData Token Lama
- **D-02:** **Full replacement** — buang seluruh penggunaan TempData token gate:
  - `CMPController.VerifyToken`: hapus `TempData[$"TokenVerified_{assessment.Id}"] = true` di kedua return (line 889 not-required branch & 900 token-required branch).
  - `CMPController.StartExam`: ganti `var tokenVerified = TempData.Peek($"TokenVerified_{id}"); if (tokenVerified == null)` (line 963-964) → `if (assessment.TokenVerifiedAt == null)`.
  - `CMPController.RetakeExam`: hapus `TempData.Remove($"TokenVerified_{id}")` (line 2585, reset kini di RetakeService — D-01).
  - `AssessmentAdminController.ResetAssessment`: hapus `TempData.Remove($"TokenVerified_{id}")` (line 4411, reset kini di RetakeService — D-01).
  - Token gate jadi murni server-authoritative; selaras tujuan "menggantikan TempData.Peek". Tidak ada dead-code TempData token tersisa.

### Stamp di VerifyToken
- **D-03:** Stamp `assessment.TokenVerifiedAt = DateTime.UtcNow` + persist (`await _context.SaveChangesAsync()`) **hanya pada jalur token-required sukses** (CMPController.cs line ~899-900, setelah `AccessTokenMatches` lolos). Cabang `IsTokenRequired == false` (line 886-890) **tidak** di-stamp — gate `StartExam` hanya cek `TokenVerifiedAt` saat `IsTokenRequired==true`, jadi `null` tetap benar secara semantik untuk sesi tanpa token. (VerifyToken adalah POST → write+persist tidak melanggar idempotensi.)

### Terkunci oleh spec/SC (bukan gray area)
- **Migration=TRUE** `AddTokenVerifiedAt`: kolom `DateTime? TokenVerifiedAt` (nullable) di `AssessmentSession`, letak dekat `AccessToken`/`IsTokenRequired` (Models/AssessmentSession.cs ~line 105-106). Aditif zero-downtime, **no backfill** (`null` = belum verifikasi). R-2: timestamp migrasi > semua migrasi kedua branch; **regen `ApplicationDbContextModelSnapshot.cs`**; JANGAN edit migrasi lama.
- **Guard sesi InProgress lama tetap:** klausa `assessment.StartedAt == null` pada gate StartExam (line 961) DIPERTAHANKAN. Sesi InProgress lama (token sudah lewat via TempData dulu, `TokenVerifiedAt` masih null, tapi `StartedAt != null`) → gate dilewati → **tidak terkunci** (SC#4). Tidak ada backfill yang perlu.
- **GRDF-01 (Pre→Post gate, ph424)** dan time-gate StartExam tetap utuh (di luar scope 427; jangan disentuh).
- `AccessTokenMatches` (line 861) + authz/CSRF VerifyToken tetap.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Token gate (target)
- `Controllers/CMPController.cs` §`VerifyToken` (line 864-902) — stamp site (D-03, line ~900).
- `Controllers/CMPController.cs` §`StartExam` (line 905-996) — gate read site (line 961-969, ganti TempData.Peek → `TokenVerifiedAt == null`); guard `StartedAt==null` line 961 (pertahankan).
- `Controllers/CMPController.cs` §`RetakeExam` (line 2557-2587) — hapus TempData.Remove line 2585 (D-02; reset di RetakeService D-01).
- `Controllers/AssessmentAdminController.cs` §`ResetAssessment` (line 4340-4412) — hapus TempData.Remove line 4411 (D-02).
- `Services/RetakeService.cs` §`ExecuteAsync` (line 69+, ExecuteUpdateAsync ~line 115) — tambah reset `TokenVerifiedAt=null` (D-01). NB komentar line 38-39 (TempData clear = caller) jadi usang utk kolom DB.
- `Models/AssessmentSession.cs` (line 96-105) — tambah kolom `TokenVerifiedAt` dekat `AccessToken`.
- `Migrations/` + `Migrations/ApplicationDbContextModelSnapshot.cs` — `AddTokenVerifiedAt` + regen snapshot (R-2).

### Requirement & risiko
- `.planning/REQUIREMENTS.md` EXSEC-01 (line 17).
- `.planning/ROADMAP.md` §"Phase 427" (SC 1-4) + §"Merge-Risk Notes" (R-1 StartExam zona konflik, R-2 migrasi divergen).

Tidak ada ADR/spec eksternal — keputusan tertangkap penuh di atas.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RetakeService.ExecuteAsync` `ExecuteUpdateAsync().SetProperty(...)` chain (line ~115) sudah me-reset `StartedAt`, `Status`, dll dalam satu operasi atomik → tambahkan `TokenVerifiedAt` di chain yang sama (D-01).
- `AccessTokenMatches` (CMPController.cs:861) pure helper (auto-heal lowercase) — TIDAK diubah.
- Test analog: `HcPortal.Tests/VerifyTokenTests.cs` (pure helper) + `RetakeServiceTests.cs`/`RetakeExamEndpointTests.cs` (RetakeService real-SQL + endpoint, sudah ada FakeUserStore/MakeUserManager).

### Established Patterns
- StartExam = write-on-GET untuk Upcoming→Open (line 920-928, guarded `!IsImpersonating()`). Phase 427 TIDAK mengubah ini (itu Phase 428). Tapi VerifyToken POST stamp = write yang benar (bukan GET).
- Migration nullable aditif: pola `AddShuffleTogglesToAssessmentSession` (R-2 referensi) — EF migration + regen snapshot.

### Integration Points
- Gate StartExam line 961-969: satu titik baca (`IsTokenRequired && userId==user.Id && StartedAt==null`) → ganti predikat dalam dari TempData.Peek ke `TokenVerifiedAt == null`.
- VerifyToken line ~899-900: stamp + SaveChanges sebelum return success.
- RetakeService ExecuteUpdateAsync: satu SetProperty tambahan.
</code_context>

<specifics>
## Specific Ideas

Verifikasi lokal (CLAUDE.md): `dotnet ef migrations add AddTokenVerifiedAt` + `dotnet ef database update` (DB lokal SQLEXPRESS, `sqlcmd -C -I` cek kolom hadir) + `dotnet build` + `dotnet test`. Branch ITHandoff port 5270. Notify IT: migration=TRUE Phase 427 saat promosi.
</specifics>

<deferred>
## Deferred Ideas

None — write-on-GET StartExam refactor = Phase 428 (EXSEC-02, sudah terjadwal, depends 427).
</deferred>

---

*Phase: 427-exam-token-gate-server-authoritative*
*Context gathered: 2026-06-24*
