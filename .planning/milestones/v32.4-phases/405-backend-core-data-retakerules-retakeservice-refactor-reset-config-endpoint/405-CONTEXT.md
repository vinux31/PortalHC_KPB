# Phase 405: Backend Core — Data + RetakeRules + RetakeService + Refactor Reset + Config Endpoint - Context

**Gathered:** 2026-06-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Fondasi backend ujian ulang (Attempt/Retake) **tanpa UI**: model data (3 kolom config `AssessmentSession` + tabel snapshot per-soal `AssessmentAttemptResponseArchive`), aturan kelayakan murni `RetakeRules`, builder snapshot murni `RetakeArchiveBuilder`, mesin retake bersama `RetakeService.ExecuteAsync` (claim atomik → snapshot → archive → reset → clear-token → audit), refactor `ResetAssessment` HC agar delegasi ke service (override bypass), dan endpoint config `UpdateRetakeSettings` (sibling propagation). UI admin/worker = Phase 406/407.

</domain>

<decisions>
## Implementation Decisions

### Locked dari spec (carry-forward — TIDAK di-discuss ulang)
Spec `docs/superpowers/specs/2026-06-19-attempt-retake-assessment-design.md` adalah AUTHORITATIVE. Terkunci:
- **10 keputusan D1–D10:** self-service pekerja (HC override) · attempt terakhir = record (in-place reset) · cooldown configurable default 24h (`0`=no jeda) · setting per-assessment · feedback skor+tanda-salah (kunci ditahan) · graded-only (`AssessmentType!="PreTest"`) · cap habis→lock+HC · MaxAttempts default 2 (range 1–5, = 1× ulang) · riwayat pekerja+HC · full snapshot per-soal.
- **7 must-fix:** (1) clear `TempData[TokenVerified_{id}]` saat retake; (2) exclude `IsManualEntry`; (3) counting `(UserId,Title,Category)` anti-konflasi Pre/Post; (4) claim-transisi-atomik DULU anti double-archive; (5) exclude PendingGrading (IsPassed null); (6) audit `RetakeAssessment` (worker) / `ResetAssessment` (HC); (7) tier `showWrongFlagsOnly` (di Phase 407, tapi `RetakeRules` sediakan flag-nya).
- **Signature/shape:** `RetakeRules.CanRetake` + `ShouldHideRetakeToggle` (pure); `RetakeService.ExecuteAsync(sessionId, initiatedBy, bypassGuards)` return `(bool success, string? error)`; `RetakeArchiveBuilder.Build(attemptHistoryId, questions, responses)` (verdict via `AssessmentScoreAggregator.IsQuestionCorrect`, jawaban via `BuildAnswerCell`, beku SEBELUM `RemoveRange`); tabel `AssessmentAttemptResponseArchive` (FK→`AssessmentAttemptHistory` cascade, index `AttemptHistoryId`, `PackageQuestionId` plain int).
- **Target & mirror:** `ResetAssessment` @ `AssessmentAdminController.cs:4192` (refactor → delegasi service, guard HC tetap di controller); `UpdateRetakeSettings` mirror `UpdateShuffleSettings:5556` (sibling key Title/Category/Schedule.Date, `[Authorize(Admin,HC)]`+AntiForgery+audit+clamp+PRG); `RetakeRules` mirror `Helpers/ShuffleToggleRules.cs` + test mirror `HcPortal.Tests/ShuffleToggleRulesTests.cs`.

### Gray areas yang di-discuss (2026-06-21)

- **D-01 (Hitungan attempt legacy):** `attemptsUsed` hanya hitung percobaan **era-retake** — arsip HC-reset LAMA (pre-v32.4) di `AssessmentAttemptHistory` **TIDAK** pre-consume cap `MaxAttempts`. Tujuan: pekerja yang pernah di-HC-reset 2× tidak langsung terkunci saat `AllowRetake` di-ON-kan. **Mekanisme = diskresi planner** (rekomendasi: hitung hanya arsip yang punya baris `AssessmentAttemptResponseArchive` — arsip legacy tak punya snapshot, jadi natural-excluded; alternatif: `ArchivedAt >= cutoff` migration-applied). ⚠️ Ini **deviasi dari spec** yang meng-count `archivedCount(UserId,Title,Category)` polos — planner WAJIB implement diskriminator era-retake.
- **D-02 (Eligibility retroaktif):** **Ya, retroaktif.** Saat admin nyalakan `AllowRetake` (propagasi ke sibling batch via `UpdateRetakeSettings`), sesi yang **sudah gagal sebelumnya** langsung jadi eligible (tunduk cooldown + cap). `CanRetake` cukup cek flag `AllowRetake` current + cooldown dari `CompletedAt` — tidak perlu bandingkan enabled-at timestamp.
- **D-03 (Paket migration):** **Satu migration gabung** `AddRetakeColumnsAndArchive` = 3 kolom `AssessmentSession` (AllowRetake/MaxAttempts/RetakeCooldownHours) + tabel `AssessmentAttemptResponseArchive` dalam satu migration atomik. 1 entri notify-IT. Pola 399-style single-migration.
- **D-04 (Retensi arsip):** **Simpan selamanya** (retain-all, audit/ISO 17024, sesuai D10). Hapus hanya saat parent `AssessmentAttemptHistory` dihapus (FK ON DELETE CASCADE). **Tanpa pruning.**

### Claude's Discretion
- Mekanisme konkret D-01 (snapshot-presence vs date-cutoff) — planner pilih yang paling robust + testable.
- `IRetakeService` DI lifetime (scoped, ikut pola service existing), namespace/folder, struktur file test.
- Status transient saat claim-transisi (`Completed→"Open"` per spec; reset set field fresh setelahnya — monitoring sempat lihat sesi "Open/belum mulai" sebentar, diterima).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & Plan (AUTHORITATIVE)
- `docs/superpowers/specs/2026-06-19-attempt-retake-assessment-design.md` — desain lengkap 16 bagian: 10 keputusan, arsitektur RetakeService, model data §4, RetakeRules §5, alur self-service §6, 7 must-fix §11, requirements RTK-01..14 §12, fase §13, test §14.
- `docs/superpowers/plans/2026-06-19-v32.4-phase-405-backend-core.md` — plan superpowers 9-task TDD (reference/input; planner GSD re-derive ke `.planning/phases/405-*/PLAN.md`, baca ini sebagai sumber).
- `.planning/REQUIREMENTS.md` — RTK-01/02/03/04/06/07/13 (scope Phase 405) + acceptance criteria + traceability.

### Mirror / pola yang diikuti
- `Helpers/ShuffleToggleRules.cs` + `HcPortal.Tests/ShuffleToggleRulesTests.cs` — pola helper pure + test untuk `RetakeRules`.
- `Controllers/AssessmentAdminController.cs:5556` `UpdateShuffleSettings` — pola endpoint config (sibling propagation Title/Category/Schedule.Date + RBAC + AntiForgery + audit + clamp + PRG) untuk `UpdateRetakeSettings`.
- `Controllers/AssessmentAdminController.cs:4192` `ResetAssessment` — target refactor (delegasi ke `RetakeService.ExecuteAsync(bypassGuards:true)`; guard HC IsResettable/Pre-Post/status TETAP di controller).
- `Models/AssessmentAttemptHistory.cs` (`AttemptNumber`) — parent FK untuk `AssessmentAttemptResponseArchive`.
- `Services/GradingService.cs` (`CompletedAt` set saat gagal; anti-double-cert `WHERE NomorSertifikat==null`+retry) — sumber cooldown + invariant cert-tetap-1.
- `Services/AssessmentScoreAggregator` (`IsQuestionCorrect`/`BuildAnswerCell`) — verdict + jawaban beku untuk `RetakeArchiveBuilder` (kill-drift).

### Codebase maps
- `.planning/codebase/CONVENTIONS.md`, `STRUCTURE.md`, `TESTING.md` — konvensi + lokasi test.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ShuffleToggleRules.cs` — template `RetakeRules` (static pure, mudah unit-test, no DI).
- `UpdateShuffleSettings` — template `UpdateRetakeSettings` byte-for-byte pola sibling-propagation + audit.
- `ResetAssessment:4192` — inti archive+reset existing yang diekstrak ke `RetakeService` (jangan rewrite logika, pindahkan).
- `AssessmentScoreAggregator.IsQuestionCorrect`/`BuildAnswerCell` — verdict+jawaban terpusat (jangan re-grade inline).
- `AssessmentAttemptHistory` — sudah punya `AttemptNumber`; archive existing reuse.

### Established Patterns
- Endpoint config admin: `[Authorize(Admin,HC)]` + `[ValidateAntiForgeryToken]` + sibling propagation by `(Title, Category, Schedule.Date)` + AuditLog + PRG (TempData) — diikuti utuh.
- Migration: EF default value menutup semua jalur create; explicit copy di `EditAssessment` bulk-add. Single-migration per fase (pola 399).
- Claim atomik: `ExecuteUpdateAsync WHERE Status==...` cek `rowsAffected` (pola `SaveAnswer:348`) untuk anti-race.

### Integration Points
- `AssessmentSession` model + `ApplicationDbContext` (tambah `DbSet<AssessmentAttemptResponseArchive>` + config FK/index).
- DI container (`Program.cs`/`Startup`) — register `IRetakeService`/`RetakeService` (scoped).
- `ResetAssessment` (controller) — refactor delegasi.
- SignalR hub — `sessionReset { reason }` parameterized (dulu hardcode "hc_reset").

</code_context>

<specifics>
## Specific Ideas

- D-01 deviasi dari spec di-flag eksplisit (era-retake counting, bukan archivedCount polos) — planner/researcher WAJIB resolve mekanisme + tulis test cabang "legacy archive tidak menghitung cap".
- D-02 retroaktif diuji: sesi gagal SEBELUM AllowRetake ON → setelah toggle → `CanRetake==true` (cooldown/cap terpenuhi).
- Migration gabung `AddRetakeColumnsAndArchive` — verifikasi `dotnet ef database update` + kolom/tabel hadir via `sqlcmd -C -I` (branch ITHandoff, DB lokal `HcPortalDB_Dev`).

</specifics>

<deferred>
## Deferred Ideas

None — diskusi tetap dalam scope Phase 405 (backend core). UI admin (card config + riwayat HC) = Phase 406; UI worker (tombol/gating/riwayat) = Phase 407; test menyeluruh + Playwright + security = Phase 408.

</deferred>

---

*Phase: 405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint*
*Context gathered: 2026-06-21*
