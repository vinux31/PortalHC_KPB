# Phase 313: Block Manual Submit Saat Waktu Habis - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-07
**Phase:** 313-block-manual-submit-saat-waktu-habis
**Areas discussed:** Manual reject UX, Frontend disable visual, AuditLog ActionType naming, E2E test seeding, Race condition manual click, Network failure recovery, Page reload behavior, 3 timer types consistency

---

## Manual Reject UX (pesan + redirect)

### Q1: Pesan TempData saat manual submit di-reject

| Option | Description | Selected |
|--------|-------------|----------|
| Explanatory: tunggu auto-submit | "Waktu ujian Anda sudah habis. Sistem akan otomatis mengirim..." | ✓ |
| Generic: waktu habis | Sama existing tier-2 message, singkat | |
| Apologetic + technical hint | Tone informal, mention "auto-submit" eksplisit | |

**User's choice:** Explanatory: tunggu auto-submit
**Notes:** User paham auto-submit sedang berjalan, mengurangi panic action.

### Q2: Redirect destination

| Option | Description | Selected |
|--------|-------------|----------|
| Back ke StartExam (id) | Konsisten dengan existing tier-2, handler aktif | ✓ |
| Ke Assessment list (/CMP/Index) | Keluar layar ujian, handler hilang | |
| Ke ExamSummary page | Read-only, user bisa misinterpret submit success | |

**User's choice:** Back ke StartExam (id)

---

## Frontend Disable Visual saat countdown=0

### Q1: Submit button visual style

| Option | Description | Selected |
|--------|-------------|----------|
| Greyed out + label berubah | "Waktu Habis - Submit Otomatis..." + tooltip | ✓ |
| Hidden sepenuhnya | Replace dengan banner info | |
| Disabled minimal | `disabled` attribute saja, browser default | |

**User's choice:** Greyed out + label berubah

### Q2: Auto-submit handler timing

| Option | Description | Selected |
|--------|-------------|----------|
| Immediate at countdown=0 | Fire POST tepat saat countdown reach 0 | ✓ |
| Delayed 3-5 detik | Forgiving untuk give-up scenario | |
| Tetap existing behavior | Hanya tambah disable button, asumsi handler benar | |

**User's choice:** Immediate at countdown=0

---

## AuditLog ActionType Naming

### Q1: ActionType untuk reject manual_after_timeup

| Option | Description | Selected |
|--------|-------------|----------|
| SubmitExamBlocked | Pattern Phase 312 `{Action}Blocked` | ✓ |
| SubmitExamRejected | Beda nuance dari Blocked | |
| ManualSubmitAfterTimeup | Self-documenting tapi panjang, format beda | |

**User's choice:** SubmitExamBlocked

### Q2: AuditLog success entry modification

| Option | Description | Selected |
|--------|-------------|----------|
| Tetap existing, tidak modify | Scope minimal, SC tidak minta | ✓ |
| Tambah field IsAutoSubmit | Traceability tapi out-of-scope SC | |

**User's choice:** Tetap existing

---

## E2E Test Seeding Strategy

### Q1: Seeding strategy untuk 6 skenario timer

| Option | Description | Selected |
|--------|-------------|----------|
| DB manipulation (StartedAt back-dated) | Test cepat (<1 menit), reliable | ✓ |
| Real-time wait via test.slow() | 30+ menit per run, tidak praktis | |
| Mock server time | Inject IDateTimeProvider, butuh refactor | |

**User's choice:** DB manipulation (StartedAt back-dated)

### Q2: Test seed isolation

| Option | Description | Selected |
|--------|-------------|----------|
| Dedicated fixture title (Phase 312 WR-04 pattern) | Skip kalau tidak ada, validated pattern | ✓ |
| Test creates own assessment | Self-contained tapi destructive setup | |
| Mark integration-only (skip CI) | Coverage CI hilang | |

**User's choice:** Dedicated fixture title

---

## Race Condition: Manual Click di Detik Akhir

### Q1: Policy untuk manual click yang sampai server setelah elapsed > allowed

| Option | Description | Selected |
|--------|-------------|----------|
| Strict 0-grace (per ROADMAP literal) | Tier-1 reject, auto-submit cover via fallback path | ✓ |
| Small grace 10 detik untuk manual | Manual click last-second masuk, tapi deviasi spec | |
| Same grace 2 menit untuk semua | Reverts ke single-tier, batalkan tujuan phase | |

**User's choice:** Strict 0-grace
**Notes:** Defense-in-depth via auto-submit handler — strict guard di manual aman.

---

## Network Failure Recovery

### Q1: Retry mechanism saat auto-submit POST fail

| Option | Description | Selected |
|--------|-------------|----------|
| 3x retry dengan backoff (1s, 2s, 4s) | Robust untuk koneksi tidak stabil | ✓ |
| Fire-and-forget | Simple tapi rentan transient failure | |
| Continuous retry sampai sukses/grace | Aggressive, bisa flood server | |

**User's choice:** 3x retry dengan backoff

### Q2: Fallback kalau auto-submit gagal sampai grace habis

| Option | Description | Selected |
|--------|-------------|----------|
| Banner: hubungi admin + display draft (deferred server-save) | Transparency, user tahu keadaan, server-side save di phase lain | ✓ |
| Server-side last-resort save | Out-of-scope (endpoint + schema + frontend sync) | (initial pick, redirected) |
| Tidak ada fallback | Brutal, jawaban hilang | |

**User's choice (revised):** Banner-only fallback (server-side save deferred ke milestone v16.0+)
**Notes:** User initially picked server-side save tapi setelah scope check confirm defer ke phase tersendiri.

---

## Page Reload Behavior

### Q1: Frontend timer behavior saat reload

| Option | Description | Selected |
|--------|-------------|----------|
| Recompute dari server StartedAt | Source-of-truth server, robust | ✓ |
| Local storage persist | Rentan tampering | |
| Reload reset countdown ke Duration penuh | Exploit, unacceptable | |

**User's choice:** Recompute dari server StartedAt

### Q2: Behavior kalau reload setelah elapsed > allowed

| Option | Description | Selected |
|--------|-------------|----------|
| Submit disabled + banner info | Idempotent, informative | ✓ |
| Auto-redirect ke ExamSummary | Cleaner tapi user tidak tahu apa terjadi | |
| Force fire auto-submit segera | Risk double-submit kalau previous sukses | |

**User's choice:** Submit button disabled + banner info

---

## 3 Timer Types Consistency

### Q1: Pesan/redirect/ActionType uniformity

| Option | Description | Selected |
|--------|-------------|----------|
| Sama persis untuk 3 type | Type info di Description metadata, DRY | ✓ |
| Pesan & redirect sama, ActionType beda per type | Granular audit query | |
| Semua berbeda (kompleksitas tinggi) | Tanpa user benefit jelas | |

**User's choice:** Sama persis untuk 3 type

### Q2: Manual type exclude verification

| Option | Description | Selected |
|--------|-------------|----------|
| Exclude via Type field check (explicit) | Defense-in-depth, jangan rely on StartedAt null | ✓ |
| Implicit via StartedAt null check | Bergantung asumsi rentan | |

**User's choice:** Explicit Type field check

---

## Claude's Discretion

- Tooltip wording detail untuk disabled button (D-03 spec outline only)
- Spinner/icon visual di disabled button label (D-03 mention tapi tidak mandate)
- Banner styling untuk reload after timeup (D-13) dan retry-fail fallback (D-11)
- Test fixture seed script lokasi & format (SQL script vs Playwright helper)

## Deferred Ideas

- Server-side last-resort save (deferred ke milestone v16.0+, originally pick di Q2 network failure but redirected after scope check)
- IDateTimeProvider mockable injection (out-of-scope, DB manipulation cukup)
- PreTest → PostTest landing redirect (Area 8 alternative, sub-phase milestone berikut jika dibutuhkan)
- AuditLog success entry dengan IsAutoSubmit field (D-06 alternative, milestone v16.0+ jika audit query butuh distinct)
