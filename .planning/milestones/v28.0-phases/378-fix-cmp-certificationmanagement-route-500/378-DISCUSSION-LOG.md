# Phase 378: Fix CMP CertificationManagement Route 500 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-14
**Phase:** 378-fix-cmp-certificationmanagement-route-500
**Areas discussed:** Fix approach (route), Test Y0 regression net

---

## Fix Approach — /CMP/CertificationManagement 500

| Option | Description | Selected |
|--------|-------------|----------|
| Redirect + bersihkan helper (hybrid) | Action CMP → RedirectToAction ke CDP canonical + hapus helper CMP dead. URL kerja (302→CDP), dead code hilang. **Recommended.** | ✓ (via "sesuai reko") |
| Redirect saja (minimal) | Ganti body action jadi RedirectToAction, helper dibiarkan. Diff terkecil, dead code tinggal. | |
| Full delete → 404 | Hapus action + semua helper. Route jadi 404 bersih. Sesuai keputusan asli no-duplicate, tapi URL hilang. | |

**User's choice:** "check dulu, sesuai reko" — defer ke rekomendasi setelah audit diverifikasi.
**Notes:** Audit dead-set diverifikasi saat discuss (grep `.cshtml`/JS = 0 caller CMP helper; builders private hanya dipakai cluster orphan). Reko (hybrid) di-lock sbg D-01..D-05. Redirect type = 302 default (hindari 301 cache lock-in).

---

## Test Y0 — Regression Net

| Option | Description | Selected |
|--------|-------------|----------|
| Tegaskan jadi assert | Y0 assert behavior baru (redirect→200 CDP, status ≠ 500). Regression net nyata, kunci SC2. **Recommended.** | ✓ (via "sesuai reko") |
| Biarkan documenting | Y0 tetap log-only; andalkan FLOW X CDP. Minimal touch. | |

**User's choice:** "check dulu, sesuai reko".
**Notes:** Y0 (~exam-types.spec.ts:2044) saat ini no-assert (toleran 500/200/404). Di-tighten jadi assert resolve `/CDP/CertificationManagement` 200 + ≠500. D-06.

## Claude's Discretion

- View `_SertifikatGroupTablePartial.cshtml` hapus-atau-biarkan (D-05).
- Hapus komentar misleading "dipindah dari CDPController".
- Redirect type 302 vs 301 → 302 (locked di D-02).

## Deferred Ideas

None.
