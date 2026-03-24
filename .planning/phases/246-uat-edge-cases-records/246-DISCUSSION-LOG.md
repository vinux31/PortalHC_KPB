# Phase 246: UAT Edge Cases & Records - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-24
**Phase:** 246-uat-edge-cases-records
**Areas discussed:** Strategi verifikasi edge cases, Scope renewal expired E2E, Records view & export, Plan structure

---

## Strategi Verifikasi Edge Cases

| Option | Description | Selected |
|--------|-------------|----------|
| Browser UAT langsung | Langsung test di browser. Code review sudah di Phase 244. | ✓ |
| Code review ulang + browser UAT | Review ulang kode + browser UAT. Redundan dengan Phase 244. | |
| Code review saja | Verifikasi di kode saja tanpa browser test. | |

**User's choice:** Browser UAT langsung (Recommended)
**Notes:** Code review sudah dilakukan di Phase 244 (MON-01/MON-02 semua 9 poin OK)

### Seed Data Sub-question

| Option | Description | Selected |
|--------|-------------|----------|
| Pakai data existing | Data dari Phase 241 seed sudah cukup. | |
| Seed skenario khusus | Buat seed tambahan untuk edge case scenarios. | ✓ |

**User's choice:** "coba check apakah butuh seed?" — Claude investigated and found all existing seeds have `IsTokenRequired = false`, so additional seed IS needed.
**Notes:** Perlu seed assessment dengan `IsTokenRequired = true` untuk test token validation edge cases.

---

## Scope Renewal Expired E2E

| Option | Description | Selected |
|--------|-------------|----------|
| Full flow: Home alarm → renewal | Test dari Home/Index alarm banner sampai sertifikat baru. | ✓ |
| Langsung ke RenewalCertificate | Skip alarm banner, langsung test renewal page. | |

**User's choice:** Full flow: Home alarm → renewal (Recommended)
**Notes:** Juga ditemukan bahwa tidak ada seed sertifikat expired — perlu ditambahkan.

---

## Records View & Export

| Option | Description | Selected |
|--------|-------------|----------|
| Verifikasi existing saja | Halaman sudah ada, cukup verifikasi kolom + export. | ✓ |
| Kemungkinan perlu coding baru | Mungkin ada kolom/export yang belum ada. | |

**User's choice:** Verifikasi existing saja (Recommended)

---

## Plan Structure

| Option | Description | Selected |
|--------|-------------|----------|
| 2 plans: Seed + Browser UAT | Plan 1: Seed data. Plan 2: Browser UAT semua requirements. | ✓ |
| 3 plans: Seed, Edge UAT, Records UAT | Lebih granular, 3 plan terpisah. | |
| 1 plan: Semua gabung | Semua dalam 1 plan. | |

**User's choice:** 2 plans: Seed + Browser UAT (Recommended)

---

## Claude's Discretion

- Detail implementasi seed data (nama, token value, tanggal expired)
- Urutan test scenario dalam browser UAT

## Deferred Ideas

None
