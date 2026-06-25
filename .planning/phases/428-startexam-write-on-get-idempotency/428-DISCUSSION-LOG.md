# Phase 428: StartExam Write-on-GET Idempotency - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-25
**Phase:** 428-startexam-write-on-get-idempotency
**Areas discussed:** Cakupan (scope), Mekanisme (mechanism), Test/UAT depth

---

## Cakupan (Scope)

| Option | Description | Selected |
|--------|-------------|----------|
| Hanya Upcoming→Open | Hapus persist transisi Upcoming→Open saja (922-932); InProgress/StartedAt + assignment-create tetap di GET. Minimal, risiko regresi terkecil, selaras R-1. | ✓ |
| Semua write-on-GET | Pindah juga InProgress/StartedAt + assignment-create ke POST. Idempotensi penuh tapi ubah alur worker + risiko besar + melebihi SC. | |

**User's choice:** Hanya Upcoming→Open
**Notes:** Untuk worker asli, Upcoming→Open (923) langsung ditimpa InProgress (1023) di request sama → persisted-Open standalone tak pernah "diam". Hapus persist-Open tak ada regresi badge admin (InProgress write tetap flip badge).

---

## Mekanisme (Mechanism)

| Option | Description | Selected |
|--------|-------------|----------|
| In-memory effective-status | Hitung openable dari Schedule tanpa persist; commit status hanya saat start aktual (InProgress existing). Mirror lobby Assessment (245-251). Perubahan terkecil. | ✓ |
| Jalur POST eksplisit | Commit transisi di POST (VerifyToken/StartExamConfirm). Lebih benar REST tapi tambah endpoint/alur + ubah link worker. | |

**User's choice:** In-memory effective-status
**Notes:** Pola sudah terbukti di lobby (display-only, no SaveChanges). Gate time/GRDF/token tetap jalan atas effective-status.

---

## Test/UAT depth

| Option | Description | Selected |
|--------|-------------|----------|
| xUnit integration + verify | Real-SQL: GET idempoten (Status tak berubah di DB), time-gate, GRDF-01, worker start end-to-end, regresi token-gate. Konsisten 427. Cepat. | ✓ |
| Tambah Playwright lifecycle | e2e browser lobby→token→start. Lebih yakin tapi lebih lama; sudah ter-cover xUnit + pernah UAT lifecycle. | |

**User's choice:** xUnit integration + verify
**Notes:** UI hint=no, tak ada perubahan view. Backend refactor murni.

## Claude's Discretion

- Bentuk kode (inline vs ekstrak helper `IsEffectivelyOpen`), share dgn lobby untuk kill-drift (tanpa over-refactor; R-1 minimal).
- Penamaan test + reuse fixture real-SQL.

## Deferred Ideas

- Idempotensi GET penuh (InProgress/StartedAt + assignment → POST) — refactor besar, di luar EXSEC-02.
- Admin monitoring effective-status by-schedule — tak perlu (tak ada regresi), kosmetik masa depan.
