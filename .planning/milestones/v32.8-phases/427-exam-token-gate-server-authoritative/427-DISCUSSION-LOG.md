# Phase 427: Exam Token-Gate Server-Authoritative - Discussion Log

> **Audit trail only.** Decisions captured in CONTEXT.md.

**Date:** 2026-06-24
**Phase:** 427-exam-token-gate-server-authoritative
**Areas discussed:** Titik reset, TempData lama, Stamp VerifyToken

---

## Titik Reset TokenVerifiedAt

| Option | Description | Selected |
|--------|-------------|----------|
| Single source di RetakeService.ExecuteAsync | `.SetProperty(r => r.TokenVerifiedAt, null)` di ExecuteUpdateAsync (tempat StartedAt reset); cover worker RetakeExam + HC ResetAssessment | ✓ |
| Per-controller-site | Reset eksplisit tiap site mirror TempData.Remove; risiko drift | |

**User's choice:** Single source di RetakeService.ExecuteAsync
**Notes:** Atomik, satu titik kebenaran; kedua jalur retake lewat ExecuteAsync.

---

## TempData Token Lama

| Option | Description | Selected |
|--------|-------------|----------|
| Hapus penuh (full replacement) | Buang TempData set/Peek/Remove; token gate murni server-authoritative | ✓ |
| Biarkan sebagai no-op | Pertahankan baris TempData; minimal diff tapi dead-code | |

**User's choice:** Hapus penuh (full replacement)
**Notes:** Selaras tujuan "menggantikan TempData.Peek".

---

## Stamp di VerifyToken

| Option | Description | Selected |
|--------|-------------|----------|
| Hanya jalur token-required | Stamp TokenVerifiedAt=UtcNow hanya saat token valid (line 900); cabang not-required tak di-stamp | ✓ |
| Kedua cabang sukses | Stamp juga cabang IsTokenRequired=false demi keseragaman | |

**User's choice:** Hanya jalur token-required
**Notes:** Gate hanya cek TokenVerifiedAt saat IsTokenRequired; null tetap benar semantik.

## Claude's Discretion

- Penempatan kolom TokenVerifiedAt di model (dekat AccessToken).
- Nama test + struktur fixture.

## Deferred Ideas

None — write-on-GET StartExam = Phase 428.
