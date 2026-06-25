# Phase 408: Test & UAT - Discussion Log

> Audit trail only. Decisions captured in CONTEXT.md.

**Date:** 2026-06-22
**Phase:** 408-test-uat
**Areas discussed:** Cakupan lifecycle e2e, Layer retake→cert, Security milestone

---

## Cakupan lifecycle e2e

| Option | Selected |
|--------|----------|
| 1 alur happy-path penuh (gagal→feedback→Ujian Ulang→ulang→lulus→cert) | ✓ |
| Happy-path + cabang lock/cooldown | |
| Skip e2e baru | |

**Notes:** Cabang lock/cooldown sudah dibuktikan smoke 407 (sc 5 & 6); 408 fokus alur retake yang benar-benar eksekusi ExecuteAsync→StartExam→lulus→cert (belum ada).

## Layer retake-then-pass → 1 cert + counting no-conflate

| Option | Selected |
|--------|----------|
| xUnit integration (real-SQL) | ✓ |
| Playwright e2e | |
| Keduanya | |

**Notes:** Deterministik, no-flake; cert# sudah dicek visual smoke 406. Mirror fixture RetakeServiceTests.

## Security milestone 408

| Option | Selected |
|--------|----------|
| Andalkan audit per-fase | |
| Secure-phase 408 baru | ✓ |

**Notes:** Gerbang formal penutup milestone; plan 408 wajib `<threat_model>` block (konsolidasi 406/407 surface).

## Claude's Discretion
- Struktur file test, apakah counting sudah ter-cover (assert vs test baru), detail seed lifecycle, regresi guard.
