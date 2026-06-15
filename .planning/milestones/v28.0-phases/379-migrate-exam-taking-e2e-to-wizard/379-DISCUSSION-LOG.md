# Phase 379: Migrate exam-taking e2e to wizard - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-14
**Phase:** 379-migrate-exam-taking-e2e-to-wizard
**Areas discussed:** Coverage essay (sinergi 376), Flow E Proton (double-drift), Definisi 'suite hijau' (DoD), Strategi helper (reuse/extend)

---

## Coverage Essay (sinergi GRADE-01)

| Option | Description | Selected |
|--------|-------------|----------|
| Tambah flow essay baru (full e2e) | Flow K: wizard essay create + worker answer + HC grade+finalize + assert Score teragregasi (validasi e2e fix 376) | ✓ |
| Delegasi ke e2e 376 (no-regression) | Essay di-cover 376 L6; 379 cuma migrasi MC/MA + no-regression | |
| Extend 1 flow existing jadi mixed+essay | Sisip essay ke Flow A, +grade+finalize+assert | |

**User's choice:** Tambah flow essay baru (full e2e)
**Notes:** Net regression terkuat; bukti GRADE-01 fix di suite exam-taking sendiri. Depends 376.

---

## Flow E Proton T3 Interview (double-drift)

| Option | Description | Selected |
|--------|-------------|----------|
| Migrasi penuh + re-check Proton form | Wizard E1 + re-verify interview E2/E3 vs Proton 358-363; E hijau penuh | ✓ |
| Wizard-create + best-effort, boleh skip sisa | E1 wizard; interview best-effort, boleh fixme/skip terdokumentasi | |
| Defer E ke phase terpisah | Keluarkan E (9 flow + K); E backlog Proton-interview-e2e | |

**User's choice:** Migrasi penuh + re-check Proton form
**Notes:** SC1 "10 flow" utuh; E tidak di-defer/skip.

---

## Definisi "suite hijau" (DoD)

| Option | Description | Selected |
|--------|-------------|----------|
| Full green run lokal wajib | Semua flow A-J+K dijalankan hijau `--workers=1`; bukti run; time-dependent dibuat deterministik | ✓ |
| Migrated+compile + spot-verify | tsc/lint bersih + un-fixme; run best-effort, spot-verify subset; flaky quarantine | |

**User's choice:** Full green run lokal wajib
**Notes:** Mandat verify lokal CLAUDE.md; G/H time-dependent di-handle deterministik hindari flaky.

---

## Strategi Helper

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse + extend examTypes.ts | Helper existing sbg jalur kanonik; extend additive utk token/interview/paste/durasi; no signature refactor | ✓ |
| Inline wizard-nav per flow | Tulis langkah wizard langsung tiap flow (duplikatif) | |
| Hybrid (helper inti + inline edge) | Helper path umum; inline edge unik (D3 paste, E3 interview) | |

**User's choice:** Reuse + extend examTypes.ts
**Notes:** DRY, konsisten dgn exam-types/shuffle/proton-bypass; preserve blame (extend additive).

---

## Claude's Discretion

- Penempatan Flow K (append exam-taking.spec.ts vs file terpisah).
- Cleanup package-layer (verifikasi DeleteAssessment cascade Phase 353).
- Bentuk extend helper (signature token/interview/paste/duration, additive).
- Handling deterministik Flow G timer / H real-time.

## Deferred Ideas

- Full e2e harness rewrite / refactor signature helper existing — ditolak.
- Fix bug produksi terungkap saat migrasi — surface backlog, jangan fix inline.
- Test debt e2e di luar exam-taking.spec.ts — out of scope.
