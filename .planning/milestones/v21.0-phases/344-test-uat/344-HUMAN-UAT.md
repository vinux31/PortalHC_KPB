---
status: passed
phase: 344-test-uat
source: [344-RESEARCH.md, "docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md §7"]
started: 2026-06-04
updated: 2026-06-04
---

> **Thin manual UAT (D-04).** Hanya item yang TIDAK diotomasi. 4 dari 5 skenario UAT spec §7 sudah
> diotomasi di `tests/e2e/manage-org-label.spec.ts` (sc.1-sc.5, semua PASS). Yang tersisa manual:
> 1 visual-accuracy judgment (cascade count) + 4 regression smoke (ORG-INTEG-03).
>
> **Dieksekusi oleh Claude via Playwright MCP** (live browser, `http://localhost:5277`, admin@pertamina.com)
> atas permintaan user 2026-06-04. Semua mutasi di-revert; dev DB `HcPortalDB_Dev` kembali ke baseline
> (21 unit, 0 dummy, 0 inactive, label Level 0 = "Bagian", GAST nama utuh — diverifikasi via SQL).

## Current Test

[selesai — 5/5 PASS via Playwright MCP 2026-06-04, 0 temuan]

## Tests

### UAT-5 — Cascade warning count AKURAT (visual judgment, D-04)
expected: Edit Bagian "GAST" (id 4), ubah nama → modal Konfirmasi Perubahan muncul dengan angka user/mapping/kompetensi/panduan yang COCOK dengan jumlah sebenarnya. Klik Batal (no mutation).
result: **PASS** (MCP 2026-06-04). openEditModal(4) → nama "GAST UATCHECK" → submit → `#cascadeConfirmModal` muncul dengan **user=7, mapping=1, kompetensi=2, panduan=1**. Cross-check independen via SQL: `SELECT COUNT(*) FROM Users WHERE Section='GAST'` = **7** → COCOK dengan displayed user count (logika cascade = `_context.Users.Where(Section==oldName)` per OrganizationController.cs:302). Klik Batal → GAST nama tetap "GAST" (SQL verified, no mutation). Akurasi count terkonfirmasi (didukung Phase 342 6 [Fact] PreviewEditCascade preview==actual).

### SMOKE-1 — Tree drag-reorder persist
expected: Reorder unit → reload → urutan baru tetap (ReorderBatch).
result: **PASS** (MCP 2026-06-04). RFCC(1) children baseline [6,5] → ReorderBatch [5,6] → **full page reload** → urutan tetap [5,6] (Propylene→RFCC LPG, dibaca dari tree fresh-load). Revert ReorderBatch [6,5] → urutan kembali [6,5]. Persist across reload terbukti.

### SMOKE-2 — Toggle Aktif/Nonaktif
expected: Nonaktifkan unit → badge "Nonaktif" + dropdown induk suffix " (nonaktif)" abu-abu → aktifkan lagi.
result: **PASS** (MCP 2026-06-04). doToggle(9) "Hydrogen Manufacturing Unit (068)": isActive true→false; badge baris = "Nonaktif"; opsi dropdown `#unitModalParent` = "    Hydrogen Manufacturing Unit (068) (nonaktif)". doToggle(9) revert → isActive true. Baseline restored.

### SMOKE-3 — Delete leaf unit
expected: Hapus leaf unit → hilang dari tree, no orphan.
result: **PASS** (MCP 2026-06-04). DeleteOrganizationUnit(25) (dummy ZZZ_UAT_DUMMY dari SMOKE-4) → success "Unit berhasil dihapus"; dummy hilang dari tree; totalUnits 22→21; tidak ada orphan/error. (Pakai unit dummy, bukan data nyata.)

### SMOKE-4 — Add unit di bawah parent existing + judul modal dinamis
expected: Tambah unit di bawah parent → muncul di posisi pre-order benar + judul modal dinamis per tier.
result: **PASS** (MCP 2026-06-04). openAddModal(1) (di bawah RFCC level-0) → judul modal = **"Tambah Unit"** (child level 1 = Unit). Cross-check: openAddModal() root → judul **"Tambah Bagian"** (level 0) — judul dinamis per tier terbukti (ORG-TREE-09). AddOrganizationUnit "ZZZ_UAT_DUMMY" parentId=1 → muncul sebagai child RFCC di posisi pre-order setelah sibling [6,5,25]. (Dihapus di SMOKE-3.)

## Summary

total: 5
passed: 5
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

None — 5/5 PASS. Tidak ada temuan. Dev DB di-restore ke baseline (21 unit, 0 dummy, 0 inactive, label "Bagian", GAST utuh — SQL verified). Metode: Playwright MCP live browser + cross-check SQL independen untuk UAT-5.
