# Phase 387: Post-Lisensor Assessment Polish - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-15
**Phase:** 387-post-lisensor-assessment-polish
**Areas discussed:** PXF-06 policy, Strictness timer/data (F-22+F-20), PXF-08 cert fail, Verify depth

---

## Area selection

| Option | Description | Selected |
|--------|-------------|----------|
| PXF-06: policy edit essay pasca-finalize | Block / Recompute / Allow+re-finalize | ✓ |
| Strictness timer/data (F-22 + F-20) | Strict vs toleran | ✓ |
| PXF-08: cert nomor gagal — surface vs silent | Log diam vs surface ke HC | ✓ |
| Depth verifikasi pasca-acara | Unit+build LOW vs full Playwright | ✓ |

**User's choice:** Keempat area dibahas.

---

## PXF-06 — Edit skor essay pasca-finalize

| Option | Description | Selected |
|--------|-------------|----------|
| BLOCK + pesan jelas | Tolak SubmitEssayScore bila status terminal; 0 risiko divergen; simpel | ✓ |
| RECOMPUTE skor (cert tetap) | Edit + recompute Score/IsPassed, cert tak reissue (risiko skor≠cert) | |
| ALLOW + re-finalize + regen cert | Edit + finalize ulang + regen cert (cert resmi bisa berubah — bahaya audit) | |

**User's choice:** BLOCK + pesan jelas.
**Notes:** Lisensor high-stakes, cert = bukti resmi. Nuansa kritis ditambah Claude (D-01a): guard hanya saat terminal pasca-finalize, JANGAN saat window grading normal (PendingGrading) — else alur nilai HC rusak.

---

## Strictness — F-22 (essay lewat timer) + F-20 (MC absent-key)

| Option | Description | Selected |
|--------|-------------|----------|
| Strict — mirror SaveMultipleAnswer | F-22 tolak essay post-timer (mirror :205-212); F-20 update key yang ada saja | ✓ |
| Toleran | Grace essay + proses semua (inkonsisten, risiko data-loss laten) | |

**User's choice:** Strict — mirror SaveMultipleAnswer.

---

## PXF-08 — Nomor cert gagal generate

| Option | Description | Selected |
|--------|-------------|----------|
| Retry 3x + log, surface ke HC | Match GradingService; gagal → pesan ke HC, tak lolos diam | ✓ |
| Retry 3x + log diam | Tak ganggu HC, tapi bisa lulus tanpa nomor cert silent | |

**User's choice:** Retry 3x + log + surface ke HC.

---

## Depth verifikasi

| Option | Description | Selected |
|--------|-------------|----------|
| Unit+build LOW, Playwright PXF-11 saja | Unit PXF-06/07/09/12/14 + build; Playwright hanya a11y PXF-11 | ✓ |
| Full Playwright semua | E2e semua 9 (mahal untuk LOW, sebagian sulit di-e2e) | |

**User's choice:** Unit+build LOW, Playwright PXF-11 saja.

---

## Claude's Discretion

- PXF-07/09/10/11/14 detail implementasi (ikut pola kanonik v30.0 + SignalR existing + derive huruf opsi by index).
- Enum status terminal pasca-finalize yang persis untuk guard PXF-06 — researcher konfirmasi.

## Deferred Ideas

- F-01 (UX MA-warn) — OUT, mitigasi briefing.
- F-18 (export by-paket) — OUT kondisional multi-paket.
- Todo cleanup data test post-367 — false-positive, tidak difold.
