# Phase 382: Grading / Lifecycle / Cert - Context

**Gathered:** 2026-06-14
**Status:** Ready for planning

> **Catatan tooling:** `init phase-op 382` saat ini mengembalikan `phase_found=false` karena `STATE.md` frontmatter `milestone: v22.0` (STATE sengaja di-pin untuk paralel-safe; `extractCurrentMilestone()` men-strip section v29.0 dari ROADMAP). Phase 382 **ADA & lengkap terspesifikasi** di `.planning/ROADMAP.md` (section `## 🚀 v29.0`, header `### Phase 382`). Discuss-phase dijalankan **manual** dengan nilai diturunkan: `padded_phase=382`, `phase_slug=grading-lifecycle-cert`, `phase_dir=.planning/phases/382-grading-lifecycle-cert`. **STATE.md TIDAK di-advance** dan **TIDAK auto-advance ke plan** (jaga sesi paralel tak clobber).

<domain>
## Phase Boundary

Nilai, kelulusan, dan sertifikat worker **single-answer** (Normal + PrePost/PostTest, **non-Proton**) menjadi **BENAR & tahan-race**:
- grading membaca jawaban **FINAL** tanpa terganggu baris duplikat (SAVE-01),
- sesi **Abandoned/Cancelled tak bisa di-resurrect** jadi Completed-lulus (STAT-01),
- hasil yang sudah **Completed/graded tak ketimpa** AbandonExam telat (STAT-02),
- **timer Normal ("Standard") ditegakkan** (TMR-01/02/03),
- **gate token** tak bisa di-bypass via SaveAnswer/SubmitExam (TOK-02),
- cert lulus `ValidUntil=null` tampil **konsisten "Aktif/Permanen"** di semua surface (CERT-01).

Phase 382 = gabungan **P2 (grading & lifecycle correctness)** + **P3 (cert visibility)** dari audit FOCUS (5-phase/4-wave map dipadatkan jadi 3 phase di ROADMAP). Ini phase **TERPADAT** v29.0.

**Requirements:** WSE-06 (SAVE-01), WSE-07 (STAT-01), WSE-08 (STAT-02), WSE-09 (TMR-01+TMR-02+TMR-03), WSE-10 (TOK-02), WSE-11 (CERT-01).

**Depends on:** Phase 381 (sequential pada axis `Controllers/CMPController.cs` — hindari rebase churn). Discuss boleh paralel; **EXECUTE wajib seri** setelah 381 landing.

**OUT (locked roadmap):** Proton (BYP/PEL/T3), essay (EDT/ESS/GRD-01/PASS-01/RES-01/SAVE-03/OPS-03), multiple-answer (GRD-03/RES-03), admin-not-on-worker-path (CAT/MAN/REC/GAIN/CERT-02..05/SHF-02/03/OPS-02/04/05/SAVE-05/TMR-04), UI ujian peserta, data-governance admin.
</domain>

<decisions>
## Implementation Decisions

### SAVE-01 — Anti-duplikat single-answer (WSE-06)
- **D-01:** **Dedupe last-write-wins, NO migration.** `PackageUserResponse` TAK punya kolom diskriminator QuestionType (cuma FK `PackageQuestionId`) → filtered unique index via EF `HasFilter` **tak feasible** (filter SQL Server hanya boleh refer kolom tabel yg sama, tak bisa join `PackageQuestion.QuestionType`). Solusi:
  1. **Grading read final:** `GradingService` (FirstOrDefault tanpa ORDER BY → ganti) + `SubmitExam` (GroupBy.First ~1622-1624) baca jawaban FINAL per soal via `GroupBy(PackageQuestionId).OrderByDescending(r => r.SubmittedAt).First()`. Worker Score dihitung dari opsi **final** (bukan basi).
  2. **Write-side harden best-effort:** `SaveAnswer` (~348-417) untuk single-answer = upsert eksplisit (cari existing → update; jika tak ada → insert) dalam transaksi. Toleransi baris dup fisik bila tetap lolos race (hygiene debt minor, tak pengaruh skor karena read-side dedupe).
- **⚠️ D-01-IMPACT (milestone-level):** Keputusan ini membuat **Phase 382 `Migration=false`** → **v29.0 total = 0 migration baru**. Ini **flip** dari ROADMAP yang menulis `Migration=TRUE` + "notify IT" untuk 382. **Aksi saat plan:** update field `**Migration**` Phase 382 di ROADMAP ke `false`, hapus klaim "1 migration @ Phase 382" di summary v29.0/STATE, dan **tak perlu notify IT migration** untuk milestone ini. (Roadmap memang menyebut dedupe sebagai kontingensi NO-migration yang sah.)
- **Rationale:** Penuhi goal worker-success (Score benar) tanpa beban schema/IT DB; sejalan CLAUDE.md (developer tak promosi DB Dev/Prod). Filtered index sejati butuh kolom denormalisasi + backfill (migration lebih besar) — overkill untuk single-answer correctness.

### STAT-01 — Anti-resurrection (WSE-07)
- **D-02:** Guard grading + submit **exclude Abandoned/Cancelled/PendingGrading** (bukan cuma "Completed"). `GradeAndCompleteAsync` (`GradingService.cs:238-246`, cabang essay 202-211) tambah Abandoned/Cancelled/PendingGrading ke guard; `SubmitExam` (~1545) early lifecycle-guard sebelum mutasi → sesi Abandoned/Cancelled POST SubmitExam **ditolak** (tak jadi Completed-lulus, tak terbit cert).

### STAT-02 — Abandon tak menimpa graded (WSE-08)
- **D-03:** `AbandonExam` (~1220-1248) ubah dari blind UPDATE → **`ExecuteUpdate` ber-guard `Where(Status==InProgress || Status==Open)`**. Branch `rowsAffected==0` → reject. Sesi yang sudah Completed/graded tak bisa di-rollback ke Abandoned; verdict lulus + cert tetap tampil di Results/Records.

### TMR-01/02/03 — Timer enforcement Standard (WSE-09)
- **D-04:** `EnsureCanSubmitExamAsync` (~4382-4444) **cakup "Standard"** — balik logika agar hanya SKIP untuk Manual/null (atau tambah "Standard" ke allowlist). Submit MANUAL Standard jauh-telat → **ditolak (Tier-1/Tier-2) + audit `SubmitExamBlocked`**.
- **D-05:** **Jawaban yang sudah ke-save TETAP ter-grade** lewat jalur **auto-submit saat waktu habis** (on-time) — TIDAK hangus. Konsekuensi: fix **TMR-03** one-shot `AutoSubmitToken` agar token TAK dikonsumsi sebelum grading commit (retry aman bila DB hiccup). Worker yang submit-telat manual lihat pesan "waktu habis".
- **D-06:** TMR-02 — gate incomplete-submit jangan percaya client `isAutoSubmit` mentah; validasi server-side (defensif, locked-roadmap).

### TOK-02 — Token gate di save/submit (WSE-10)
- **D-07:** **StartedAt-gate** di `SaveAnswer` (~363-367) dan `SubmitExam` (~1539) — token-required tak bisa di-bypass dengan langsung POST save/submit tanpa lewat lobby. **Diimplementasi SEKALI, koheren**, bareng lifecycle-guard STAT-01 di handler yang sama (audit: jangan taruh TOK-02 di Phase 381 — same-method conflict dgn 382).

### CERT-01 — Visibilitas cert ValidUntil=null (WSE-11)
- **D-08:** **`ValidUntil==null` = "Permanen/Aktif" (tanpa kedaluwarsa)** — konsisten lintas SEMUA surface:
  - `DeriveCertificateStatus` (`CertificationManagementViewModel.cs:58-59`) return **Aktif/Permanen** (BUKAN Expired) untuk cert lulus null.
  - **DIKELUARKAN** dari worklist renewal + badge expiry + notif: `HomeController` badge `GetCertAlertCountsAsync` (~215) + notif `TriggerCertExpiredNotificationsAsync` (~124) + `AdminBaseController` renewal post-filter (~198-203, keep Expired||AkanExpired) + tally Renewal/CDP. Cert permanen tak perlu di-renew.
  - Terapkan **konsisten di SEMUA consumer** (helper + badge + notif + AdminBase + Renewal/CDP) — jangan setengah-setengah.
- **D-08-TEST:** Test lama `DeriveCertificateStatus_NullValidUntil_NonPermanent_ReturnsExpired` **AKAN break** → **rewrite** ke keputusan baru (null→Permanen/Aktif).

### Claude's Discretion
- Bentuk persis upsert SaveAnswer (delete-then-insert vs find-update) dalam transaksi.
- Wording pesan reject (BI): submit-telat "waktu habis", resurrect/abandon-overwrite ditolak.
- Nama/bentuk audit log entry untuk STAT reject + SubmitExamBlocked.
- Apakah dedupe-read pakai GroupBy in-memory atau window-function SQL (selama hasil = opsi final).

### Folded Todos
(none)
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Audit source (fix detail + bukti + test plan + risiko)
- `docs/assessment-audit/2026-06-14-E2E-worker-success-FOCUS.md` — Fase 2/3 (SAVE-01/STAT-01/STAT-02/TMR-01/CERT-01): masalah+file:line+fix; §"Risiko/miss saat plan" (intra-phase same-method density, filtered-index feasibility, CERT-01 cross-surface drop-out, TOK-02 placement); §"File overlap matrix"; E2E scenario #8-12.
- `docs/assessment-audit/2026-06-14-code-audit-findings.md` — master finding (severity, verifikasi adversarial).

### Requirements & roadmap
- `.planning/ROADMAP.md` §`## 🚀 v29.0` → `### Phase 382` (Goal, 6 REQ, Files, 5 Success Criteria, **Migration field — UPDATE ke false per D-01-IMPACT**).
- `.planning/REQUIREMENTS.md` — WSE-06..11 (+ traceability).

### Prior phase context (carry-forward)
- `.planning/phases/380-admin-engine-integrity/380-CONTEXT.md` — pola defensif (server-side reject + TempData), migration discipline (notify IT), engine ShuffleEngine yg di-CONSUME StartExam.

### Code (anchors)
- `Controllers/CMPController.cs` — `SaveAnswer` 348-417 (TOK-02 gate 363-367, upsert), `SubmitExam` 1523-1724 (STAT-01 guard 1545, SAVE-01 GroupBy.First 1622-1624, TMR 1553-1595, TOK-02 1539), `AbandonExam` 1220-1248 (STAT-02 ExecuteUpdate guard), `EnsureCanSubmitExamAsync` 4382-4444 (TMR-01 cakup Standard), cert region.
- `Services/GradingService.cs` — guard 238-246 & 202-211 (STAT-01 exclude), FirstOrDefault→ORDER BY (SAVE-01), cert 269-301.
- `Models/CertificationManagementViewModel.cs` 58-59 — `DeriveCertificateStatus` null-semantics (CERT-01).
- `Controllers/HomeController.cs` 124 (notif) + 215 (badge) — filter `ValidUntil.HasValue` (CERT-01 konsisten).
- `Controllers/AdminBaseController.cs` ~198-203 — renewal post-filter (CERT-01 exclude null).
- `Controllers/CDPController.cs` / `RenewalController.cs` — tally renewal (CERT-01 konsisten).
- `Models/PackageUserResponse.cs` — TAK ada kolom QuestionType (dasar D-01: filtered index tak feasible).
- `Migrations/20260407070949_RemoveUniqueIndexOnPackageUserResponse.cs` — referensi index yg di-drop (akar SAVE-01).

### Decision carry-forward (lintas milestone)
- [v14.0/296] `GradingService` = satu-satunya source of truth grading (GradeFromSavedAnswers dihapus) → SAVE-01 read-final ikut pola ini.
- [v22.0] `AssessmentConstants.AssessmentStatus.PendingGrading` = single source label lintas surface → guard STAT-01 pakai konstanta ini.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Pola guard `ExecuteUpdate` + `rowsAffected` branch** (STAT-02) — pola EF Core standar, mirror reject-pattern Phase 380.
- **`AssessmentConstants.AssessmentStatus.*`** — konstanta status canonical (Abandoned/Cancelled/PendingGrading/Completed) untuk guard.
- **`ImpersonationService` + audit pattern Phase 380** — TempData reject + server-side validation.

### Established Patterns
- Server-side validation reject + TempData message (380); `GradingService` otoritatif baca-ulang DB; status guard via konstanta canonical.

### Integration Points (DENSITAS TINGGI — koordinasi wajib)
- **`GradeAndCompleteAsync`** di-mutasi oleh SAVE-01 (read) **dan** STAT-01 (guard) — method SAMA.
- **`SubmitExam`** di-mutasi oleh SAVE-01 (1622-1624) + STAT-01 (1545) + TMR-* (1553-1595) + TOK-02 (1539) — method SAMA.
- → **Eksekusi 382 sebagai SATU phase koheren, TANPA intra-phase parallel sub-agent** di CMPController/GradingService. Urutan commit disarankan: SAVE-01 read-final → STAT-01 guard → STAT-02 → TMR-01/02/03 → TOK-02 → CERT-01 (5 surface).
- CERT-01 cross-surface: ubah helper + 4 consumer (badge/notif/AdminBase/CDP-Renewal) **konsisten** — kalau helper berhenti return Expired untuk null tapi consumer tak diupdate → renewal worklist silently drop / tally bergeser.

### File overlap matrix (dari audit)
- `[soft] Controllers/CMPController.cs` ← P1(381), P2+P3(382) — **EXECUTE SERI** setelah 381; jangan rebase paralel.
- `[none]` GradingService.cs / CertificationManagementViewModel.cs / HomeController.cs / AdminBaseController.cs / CDPController.cs / RenewalController.cs ← 382 only.
</code_context>

<specifics>
## Specific Ideas

- **SAVE-01 read-final:** `responses.GroupBy(r => r.PackageQuestionId).Select(g => g.OrderByDescending(r => r.SubmittedAt).First())` sebelum scoring; konsisten di GradingService + SubmitExam.
- **STAT-02 guarded update:** `ExecuteUpdate(... Status=Abandoned ...).Where(s => s.Id==id && (s.Status==InProgress || s.Status==Open))`; `if (rowsAffected==0) return reject("sesi sudah selesai/tak bisa dibatalkan")`.
- **CERT-01 null=Permanen:** semua titik perlakukan `ValidUntil==null` sebagai aktif-tanpa-kedaluwarsa **dan** keluar dari hitung+notif expiry.
- **Test (audit E2E):** #8 STAT-01 anti-resurrection (Abandoned→SubmitExam ditolak; Cancelled idem); #9 STAT-02 (Completed→AbandonExam rowsAffected==0, verdict tetap); #10 SAVE-01 concurrent (2 SaveAnswer beda opsi → 1 baris final → Score benar); #11 TMR-01 (Standard StartedAt-mundur → submit manual ditolak + audit, on-time diterima); #12 CERT-01 (lulus ValidUntil=null → Results LULUS+PDF + dashboard Aktif/Permanen + badge/notif konsisten).
- **PrePost same-day E2E (#4)** = acceptance test Wave pasca-382 (butuh grading 382 + entry 381), BUKAN gate 381.
</specifics>

<deferred>
## Deferred Ideas

### Reviewed Todos (not folded)
(none baru — todo cleanup lokal pasca-367 sudah dicatat di 380-CONTEXT, tak terkait 382.)

### Milestone-level defer (di REQUIREMENTS.md, bukan phase ini)
- **RES-02** (X/Y benar vs Score% display inconsistency) — backlog.
- **GRD-02** (empty-MA SetEquals, LOW, tak terjangkau via validasi create/edit) — backlog.

### Out-of-scope adjacents (jangan tarik masuk)
- CERT-02..05, EDT-*, ESS-*, MAN-*, REC-*, GAIN-* — admin/essay/multi-answer path, bukan worker take/pass single-answer.
</deferred>

---

*Phase: 382-grading-lifecycle-cert*
*Context gathered: 2026-06-14*
