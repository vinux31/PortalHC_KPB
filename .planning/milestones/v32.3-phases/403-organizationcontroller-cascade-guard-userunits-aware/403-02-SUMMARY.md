---
phase: 403-organizationcontroller-cascade-guard-userunits-aware
plan: 02
subsystem: organization
tags: [organization, ui, cascade-preview, modal, userunits]
requires: [affectedUserUnitsCount JSON field (403-01 PreviewEditCascade)]
provides: [cascade-confirm modal baris keanggotaan unit, modal-appears-on-UserUnits-only-impact]
affects: [ManageOrganization view, orgTree.js]
tech-stack:
  added: []
  patterns: [read-only server-authoritative count via .textContent (no innerHTML, no client compute)]
key-files:
  created:
    - .planning/phases/403-organizationcontroller-cascade-guard-userunits-aware/403-02-SUMMARY.md
  modified:
    - Views/Admin/ManageOrganization.cshtml
    - wwwroot/js/orgTree.js
key-decisions:
  - "Baris ke-5 meniru byte-for-byte gaya saudaranya (li>strong+text), label literal 'baris keanggotaan unit' (bukan @OrgLabels.GetLabel)"
  - "Total sum tambah term (pv.affectedUserUnitsCount || 0) → modal muncul saat dampak HANYA UserUnits (terbukti pure-edge UAT: 0/0/0/0/1)"
  - "Checkpoint human-verify di-drive executor via Playwright @5270 (snapshot→seed→UAT→restore) atas instruksi user 'verifikasi via browser'"
requirements-completed: [ORG-02]
duration: ~40 min (incl browser UAT)
completed: 2026-06-19
---

# Phase 403 Plan 02: Cascade-Confirm Modal Baris Keanggotaan Unit Summary

Menambah SATU baris read-only `#cascadeUserUnits` "X baris keanggotaan unit" ke modal `#cascadeConfirmModal` existing (ORG-TREE-07), terisi `affectedUserUnitsCount` dari `PreviewEditCascade` (Plan 01), plus memastikan modal tetap muncul bila satu-satunya dampak adalah `UserUnits`.

**Duration:** ~40 min (incl browser UAT) | **Tasks:** 2 (1 auto + 1 checkpoint) | **Files:** 2 modified

## What Was Built

- **Task 1 (auto, commit `986fedff`):**
  - `ManageOrganization.cshtml`: `<li><strong id="cascadeUserUnits">0</strong> baris keanggotaan unit</li>` sebagai baris ke-5 di `<ul>` modal (setelah "file panduan"), gaya identik 4 saudaranya, label literal.
  - `orgTree.js`: populate `document.getElementById('cascadeUserUnits').textContent = pv.affectedUserUnitsCount || 0` + tambah `+ (pv.affectedUserUnitsCount || 0)` ke `total` di `submitUnitModal()` → modal muncul saat dampak UserUnits-only.
- **Task 2 (checkpoint:human-verify, gate=blocking — PASSED via executor-driven browser UAT @localhost:5270):**
  Snapshot DB → seed fixture multi-unit Iwan (NIP 123456) → Playwright UAT → RESTORE WITH REPLACE → full suite hijau. Journal `SEED_JOURNAL.md` (active→cleaned).

## Browser UAT (Playwright @5270) — 5/5 PASS

| # | Skenario | Hasil | Bukti |
|---|----------|-------|-------|
| A | Rename Alkylation Unit (065) (6 anggota) | Modal baris ke-5 "**6** baris keanggotaan unit" (gaya identik saudaranya), == 6 UU aktual; Batal (read-only) | 403-uat-A-cascade-modal-6.png |
| B | Rename RFCC NHT (053) (edge: 0 scalar user, 1 sekunder) | Modal muncul "**0** user / **1** baris keanggotaan unit"; Lanjut Simpan → DB: 1 baris UU ter-rename, Iwan primary Alkylation + Users.Unit mirror UNTOUCHED; toast "...1 keanggotaan unit terupdate." | 403-uat-B-edge-modal-0user-1uu.png |
| C | Delete RFCC NHT (053) (ber-membership sekunder) | Ditolak: "...keanggotaan (sekunder)...", unit tidak terhapus | (snapshot text) |
| D | Reparent Alkylation → RFCC (Iwan split >1 Bagian) | Block: "...1 pekerja akan **terpecah** ke >1 Bagian (**123456 - Iwan**)..."; DB: Alkylation ParentId tetap 4 (GAST) = no-mutation | 403-uat-D-reparent-split-block.png |
| Pure-edge | Rename Wet Gas (066) (0 user/0 mapping/0 komp/0 panduan, 1 UU) | Modal MUNCUL dgn SEMUA 0 kecuali "**1** baris keanggotaan unit" → membuktikan modal muncul dari term UserUnits saja (D-03) | 403-uat-pure-edge-modal-only-uu.png |

DB cross-check post-restore: UserUnits total=6 (baseline), Iwan 1 row (Alkylation primary), seed-leftover=0, OrgUnit Id15/16/17 names reverted.

## Verification

- `dotnet build` 0 error (Razor compile).
- grep: `id="cascadeUserUnits"` + `baris keanggotaan unit` di view; `getElementById('cascadeUserUnits').textContent = pv.affectedUserUnitsCount || 0` + `+ (pv.affectedUserUnitsCount || 0)` di orgTree.js.
- Full suite `dotnet test --nologo` → **532 passed, 0 failed, 5 skipped** (baseline DB, post-restore).
- Browser UAT 5/5 PASS (table above), preview==actual DB-verified.

## Deviations from Plan

**[Scope addition, additive] Pure-edge scenario ditambahkan** — Scenario B (RFCC NHT) ternyata punya kompetensi=1+panduan=1 pre-existing → tidak mengisolasi "modal muncul saat HANYA UserUnits". Ditambah skenario pure-edge (Wet Gas 066, 0 semua kecuali UU) untuk membuktikan must_have D-03 secara diskriminatif. Tidak mengubah kode; memperkuat UAT.

**Total deviations:** 1 (penambahan cakupan UAT, bukan fix). **Impact:** strengthens verification.

## Issues Encountered

None.

## Next Phase Readiness

Phase 403 selesai (2/2 plan). Wave-1 paralel {400,401,403} — 403 = unit terakhir Wave-1 yang dieksekusi. Lanjut: phase verification (gsd-verifier) → 402 (depends 400/401/403) → 404 QA.

## Self-Check: PASSED
- key-files.modified ada di disk (ManageOrganization.cshtml, orgTree.js) ✓
- `git log --grep="403-02"` → 1 commit (feat Task 1) ✓
- Acceptance_criteria Task 1 re-run PASS (grep view+js, build 0 error) ✓
- Task 2 checkpoint: browser UAT 5/5 PASS + DB cross-check + restore baseline + full suite 532/0/5 ✓
