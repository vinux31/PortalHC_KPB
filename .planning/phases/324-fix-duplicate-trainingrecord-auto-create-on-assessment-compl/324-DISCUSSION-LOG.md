# Phase 324: Fix duplicate TrainingRecord auto-create on assessment completion - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-26
**Phase:** 324-fix-duplicate-trainingrecord-auto-create-on-assessment-compl
**Areas discussed:** Data cleanup scope, Regrade cascade behavior, Test approach, DB backup

---

## Data Cleanup Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Sejak 10 Apr 2026 | Hapus TR `Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10'`. Aman, tidak ganggu data pre-bug | ✓ |
| Semua tanpa filter tanggal | Hapus SEMUA TR dengan Judul LIKE 'Assessment:%'. Risiko admin manual entry similar pattern | |
| Biarkan, hanya fix kode | Tidak cleanup data. Duplikat lama tetap muncul sampai expire natural | |

**User's choice:** Sejak 10 Apr 2026 untuk lokal. Untuk Dev/Prod: bikin HTML handoff IT versi 2026-05-26 ikut template `docs/DB_HANDOFF_IT_2026-05-13.html`.

**Notes:**
- User explicit konfirmasi: cleanup hanya target TrainingRecord, AssessmentSession TIDAK kehapus (data nilai+jawaban+sertifikat aman)
- User minta tambahan deliverable: `docs/DB_HANDOFF_IT_2026-05-26.html` mengikuti template + style versi 2026-05-13 (Pertamina-branded HTML)
- Workflow standar Portal HC KPB: kode fix push origin/main → IT promo ke Dev → IT eksekusi SQL cleanup pakai script + handoff doc

---

## Regrade Cascade Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Hapus cascade TR seluruhnya | Cascade TR di RegradeAfterEditAsync dihapus. AssessmentSession.IsPassed + NomorSertifikat update saja | ✓ |
| Pertahankan cascade TR conditional | Kalau TR existing ada (legacy), update Status-nya. Kalau gak ada (post-fix), skip insert | |

**User's choice:** Hapus cascade TR seluruhnya.

**Notes:**
- User reasoning: "kalau HC edit jawaban worker. ya yang existing (assessment itu) berubah statusnya" — match Opsi A logic
- Records page baca status terbaru dari `AssessmentSession`, gak butuh TR copy

---

## Test Approach

| Option | Description | Selected |
|--------|-------------|----------|
| Playwright UAT automated | Spec mengikuti pattern Phase 322. Coverage worker submit + records page assertion + admin grading paths | ✓ |
| Manual repro + Playwright smoke test | Manual untuk core bug, Playwright cuma dasar grading flow | |
| Manual repro saja | Lokal browser + DB query, no automated guard | |

**User's choice:** Playwright UAT automated.

---

## DB Backup

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, backup DB dulu | Snapshot DB lokal via sqlcmd BACKUP DATABASE sebelum DELETE. Per SEED_WORKFLOW mandatory. Catat di SEED_JOURNAL.md | ✓ |
| Skip backup | Lokal restore dari seed; IT eksekusi Dev/Prod dengan backup mereka | |

**User's choice:** Ya, backup DB dulu.

## Claude's Discretion

- Naming Playwright spec file + folder structure (ikut convention existing `tests/e2e/`)
- SQL script file naming + folder (saran: `docs/sql/cleanup-2026-05-26-trainingrecord-duplicates.sql`)
- HTML handoff content structure (selama follow template 2026-05-13)
- Logger statement format saat hapus block

## Deferred Ideas

- Tambah unique index `(UserId, Judul, Tanggal)` di TrainingRecord — tidak diperlukan setelah auto-create dihapus, pertimbangkan di phase masa depan kalau ada use case import manual yang bisa generate duplicate
- Refactor `GetUnifiedRecords` query — tidak perlu di Phase 324
- Audit TR legacy dengan Judul similar pattern tapi admin manual — phase terpisah kalau diperlukan
