# Phase 425: Cosmetic / Naming / Tech-Debt Cleanup - Context

**Gathered:** 2026-06-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Fase TERAKHIR v32.7 — batch cleanup low-risk, **minim risiko regresi**. Cakupan:
1. **CLN-01** label & dokumentasi diselaraskan (label ValidUntil, komentar Status 7-nilai, nama field sentinel AssessmentPackageId, doc FK LinkedSessionId).
2. **CLN-02** entry manual — Schedule/CompletedAt diselaraskan + validasi-silang IsPassed vs Score/PassPercentage (PERINGATAN server-side, non-blocking).
3. **CLN-03** kolom dead-field `AssessmentPhase` ditandai **RESERVED** di XML-doc (TIDAK di-drop).
4. **CLN-04** tech-debt timing — konsolidasi formula timer sisa ke `ExamTimeRules` (helper dari Phase 424). Token server-authoritative (FLOW-08) & write-on-GET StartExam (FLOW-10) **DI-DEFER ke backlog** (by-design + sudah dimitigasi).
5. **CLN-05** konvensi validasi ModelState dirapikan via **guard-helper bersama minimal** (BUKAN refactor DTO).

**migration=FALSE** (RESERVED bukan drop; CLN-04 aman-saja tanpa kolom baru; tak ada schema/write DB baru).

**Bukan cakupan fase ini (deferred — lihat `<deferred>`):** drop kolom AssessmentPhase; token `TokenVerifiedAt` server-authoritative; refactor write-on-GET StartExam; refactor DTO penuh untuk ModelState.
</domain>

<decisions>
## Implementation Decisions

### CLN-03 — dead-field AssessmentPhase (FLOW-06)
- **D-01:** Kolom `AssessmentPhase` (`Models/AssessmentSession.cs:180`, 0 referensi di app — cuma definisi + migration snapshots) ditandai **RESERVED via XML-doc** (komentar `/// RESERVED ... tidak dipakai`), **JANGAN drop**. Nol-risiko skema; hindari migration destruktif di fase cleanup. **migration=FALSE.** (Alternatif DROP ditolak — risiko deploy untuk manfaat marginal; kolom null/unused tak mengganggu.)

### CLN-04 — tech-debt timing (FLOW-09 do; FLOW-08/FLOW-10 defer)
- **D-02 (timer satu sumber, FLOW-09):** Konsolidasi formula durasi timer yang berulang `(DurationMinutes + (ExtraTimeMinutes ?? 0)) * 60` ke `ExamTimeRules.AllowedExamSeconds` (helper dibuat di Phase 424). Situs terverifikasi: `CMPController.cs:1191`, `:1564`, `:1642` (detik) + `:4661` (menit — `AllowedExamSeconds(...)/60` atau helper menit). PARITAS: hasil numerik identik (formula sama). AMAN, no migration, no behavior-change.
- **D-03 (DEFER FLOW-08 token + FLOW-10 write-on-GET):** Token gate TempData.Peek (FLOW-08) → kolom `TokenVerifiedAt` server-authoritative dan write-on-GET StartExam (FLOW-10, Upcoming→Open) **TIDAK dikerjakan di 425**. Keduanya by-design + sudah dimitigasi (impersonation guard). Migration + ubah-perilaku di fase cleanup = risiko regresi tinggi → backlog.

### CLN-05 — konvensi ModelState (VAL-07)
- **D-04:** Rapikan pola validasi ModelState yang berulang via **guard-helper bersama minimal** (mis. helper static yang menyeragamkan cek/ModelState), **TANPA mengubah signature action** (tidak refactor param scalar → DTO ber-anotasi). Low-risk; refactor DTO penuh ditolak (sentuh banyak action + binding = risiko regresi).

### CLN-02 — entry manual cross-validation (FLD-5.2-04, FLD-5.2-05)
- **D-05:** Di entry manual (`TrainingAdminController.AddManualAssessment` POST `:689`, `CreateManualAssessmentViewModel`): selaraskan Schedule/CompletedAt + tambah validasi-silang `IsPassed` vs (`Score >= PassPercentage`). Bila mismatch → **PERINGATAN server-side (TempData/ModelState warning), TETAP simpan** (TIDAK auto-override nilai HC, TIDAK blokir). HC boleh override sengaja. (Blokir ditolak — bertentangan dgn 'tidak auto-override'.)

### Claude's Discretion (CLN-01 + detail)
- **CLN-01 label/doc (kosmetik):** label `ValidUntil` (FLD-5.2-06), komentar `Status` 7-nilai (FLOW-05), nama field sentinel `AssessmentPackageId` (PA-05), doc FK `LinkedSessionId` (PA-04) — researcher/planner tentukan teks persis; murni label/komentar/XML-doc, TANPA ubah perilaku/skema.
- Nama & lokasi guard-helper CLN-05; bentuk peringatan CLN-02; teks XML-doc RESERVED CLN-03.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Audit & requirements
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` — sumber temuan CLN: FLOW-06 (AssessmentPhase dead-field → CLN-03), FLOW-09 (redundansi timing → CLN-04 do), FLOW-08 (token TempData.Peek → DEFER), FLOW-10 (write-on-GET StartExam → DEFER), FLD-5.2-06/FLOW-05/PA-05/PA-04 (label/doc → CLN-01), FLD-5.2-04/FLD-5.2-05 (manual Schedule/CompletedAt + IsPassed cross-validate → CLN-02), VAL-07 (ModelState konvensi → CLN-05).
- `.planning/REQUIREMENTS.md` §CLN — CLN-01..05 (acceptance).
- `.planning/ROADMAP.md` §"Phase 425" — goal + 4 Success Criteria.

### Prior phase (pola yang diikuti)
- `.planning/phases/424-grading-de-dup-flow-linking-gating-pre-post/424-CONTEXT.md` — `ExamTimeRules` (dibuat 424, dikonsumsi CLN-04 D-02); pola pure-helper + forward-only non-destruktif (carry-forward).

[Tidak ada ADR eksternal — aturan tercakup di audit + decisions di atas.]
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Helpers/ExamTimeRules.cs` `AllowedExamSeconds(durationMinutes, extraTimeMinutes)` (Phase 424) — dipakai CLN-04 D-02 untuk konsolidasi 4 situs duplikat.
- Pola pure-helper (`ExamTimeRules`, `PrePostPairing`, `CertIssuanceRules`) + guard static + test xUnit dari 422/423/424.

### Integration Points (verified sesi ini)
- `Models/AssessmentSession.cs:180` — `AssessmentPhase` (RESERVED XML-doc, CLN-03).
- `Controllers/CMPController.cs:1191, :1564, :1642` — formula `(DurationMinutes + (ExtraTimeMinutes ?? 0)) * 60` (detik) + `:4661` (menit) → konsolidasi ke `ExamTimeRules` (CLN-04 D-02).
- `Controllers/TrainingAdminController.cs:689` `AddManualAssessment` POST + `CreateManualAssessmentViewModel`; `IsManualEntry=true` `:772` — CLN-02 cross-validate warning.
- Label/doc targets (CLN-01): `ValidUntil`, `Status`, `AssessmentPackageId`, `LinkedSessionId` di `Models/AssessmentSession.cs` + view/komentar terkait.

### New (dibuat di fase ini)
- Guard-helper ModelState minimal (CLN-05) — nama/lokasi diskresi.
- (opsional) helper menit untuk timer bila perlu (CLN-04).

### NOT touched (defer)
- Tidak ada kolom DB baru. Tidak menyentuh token gate behavior / write-on-GET StartExam.
</code_context>

<specifics>
## Specific Ideas

- Timer consolidation (CLN-04) WAJIB paritas — formula sama, hasil identik; jaga dengan test (mirror ExamTimeRulesTests 424).
- CLN-02 warning non-blocking konsisten dgn filosofi v32.x (tidak auto-override entry HC).
- CLN-03 RESERVED + CLN-04 aman-saja → fase 425 migration=FALSE (penting utk notify IT: 425 tak nambah migration; milestone tetap migration=TRUE dari 422).
</specifics>

<deferred>
## Deferred Ideas

- **DROP kolom AssessmentPhase** (migration) — ditolak utk 425 (RESERVED dipilih); bisa diangkat di milestone berikut bila ingin betul-betul hapus.
- **FLOW-08 token server-authoritative** (`TokenVerifiedAt` column) — by-design + dimitigasi; backlog (butuh migration + ubah-perilaku).
- **FLOW-10 write-on-GET StartExam** refactor (Upcoming→Open) — dimitigasi (impersonation guard); backlog.
- **Refactor DTO penuh ModelState** (VAL-07 versi besar) — ditolak utk fase low-risk; backlog bila ingin arsitektur lebih rapi.

None lain — diskusi tetap dalam scope fase.
</deferred>

---

*Phase: 425-cosmetic-naming-tech-debt-cleanup*
*Context gathered: 2026-06-24*
