# Phase 382: Grading / Lifecycle / Cert - Discussion Log

> **Audit trail only.** Jangan dipakai sebagai input langsung planning/research/execution.
> Keputusan canonical di `382-CONTEXT.md` — log ini menyimpan alternatif yang dipertimbangkan.

**Date:** 2026-06-14
**Phase:** 382-grading-lifecycle-cert
**Areas discussed:** SAVE-01 strategy, CERT-01 null-semantics, TMR-01 late-submit enforcement, STAT-01/02 rejection UX
**Mode:** Manual (init phase-op 382 gagal — STATE pinned v22.0; STATE TIDAK di-advance, paralel-safe)

---

## SAVE-01 strategy (anti-duplikat single-answer, WSE-06)

Temuan grounding: `PackageUserResponse` tak punya kolom diskriminator QuestionType → filtered unique index via EF `HasFilter` tak feasible (filter SQL Server tak bisa refer joined table).

| Option | Description | Selected |
|--------|-------------|----------|
| Dedupe-read, NO migration | Grading+SubmitExam baca final via GroupBy.OrderByDescending(SubmittedAt).First() + harden SaveAnswer upsert. 0 migration, 0 IT DB action. | ✓ |
| Kolom diskriminator + filtered index | Tambah kolom IsSingleAnswer/QuestionType + backfill + filtered unique index. Fix DB-level, tapi migration besar + IT action. | |
| Both (index + dedupe-read) | Belt-and-suspenders. Overkill. | |

**User's choice:** Dedupe-read, NO migration (sesuai rekomendasi).
**Notes:** Flip milestone — Phase 382 `Migration=false` → **v29.0 = 0 migration baru**, tak perlu notify IT migration. ROADMAP migration field 382 di-update saat plan. Roadmap memang sebut dedupe sebagai kontingensi NO-migration yang sah.

---

## CERT-01 null-semantics (WSE-11)

| Option | Description | Selected |
|--------|-------------|----------|
| Permanen/Aktif + exclude renewal | null=tanpa kedaluwarsa, Aktif di semua surface, dikeluarkan dari renewal worklist + badge + notif. | ✓ |
| Aktif tapi tetap di renewal | Aktif di dashboard tapi masih muncul di worklist renewal HC. | |
| Hitung sebagai akan-expired | null=needs-attention (di-count badge+notif+renewal). | |

**User's choice:** Permanen/Aktif + exclude renewal/notif.
**Notes:** Konsisten lintas helper + HomeController 124/215 + AdminBase 198-203 + CDP/Renewal tally. Test lama `DeriveCertificateStatus_NullValidUntil_NonPermanent_ReturnsExpired` di-rewrite.

---

## TMR-01 late-submit enforcement (WSE-09)

| Option | Description | Selected |
|--------|-------------|----------|
| Tersimpan tetap ter-grade | Submit manual jauh-telat ditolak (Tier-1/2 + audit), tapi jawaban tersimpan tetap dinilai via auto-submit on-time (fix TMR-03 token). | ✓ |
| Telat = hangus (strict) | Submit telat ditolak penuh, jawaban tak di-grade sampai auto-submit valid. | |

**User's choice:** Tersimpan tetap ter-grade.
**Notes:** Memicu fix TMR-03 (one-shot AutoSubmitToken jangan dikonsumsi sebelum grading commit). Worker submit-telat lihat pesan "waktu habis".

---

## STAT-01/02 rejection UX (WSE-07/08)

| Option | Description | Selected |
|--------|-------------|----------|
| Pesan jelas + audit log | Reject + TempData message + audit (pola 380); guard Abandoned/Cancelled/PendingGrading; AbandonExam ExecuteUpdate Where(InProgress\|\|Open), rowsAffected==0→reject. | ✓ |
| Silent no-op | rowsAffected==0 diam, tak ada pesan/audit. | |

**User's choice:** Pesan jelas + audit log.
**Notes:** Konsisten pola defensif Phase 380, auditable.

---

## Claude's Discretion

- Bentuk persis upsert SaveAnswer (delete-then-insert vs find-update) dalam transaksi.
- Wording pesan reject BI (submit-telat, resurrect, abandon-overwrite).
- Bentuk audit log entry STAT reject + SubmitExamBlocked.
- Dedupe-read pakai GroupBy in-memory vs window-function SQL.

## Deferred Ideas

- RES-02, GRD-02 → backlog (milestone-level, di REQUIREMENTS.md).
- CERT-02..05/EDT/ESS/MAN/REC/GAIN → out-of-scope (admin/essay/multi-answer path).
