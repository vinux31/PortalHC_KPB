---
status: complete
phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display
source: [399-VERIFICATION.md]
started: 2026-06-18
updated: 2026-06-18
method: Playwright MCP drive @localhost:5277 (AD off) + SQL seed (snapshot→seed→restore, journal cleaned)
---

## Current Test

[testing complete]

## Tests

### 1. MU-07 modal coach-mapping — round-trip browser (W-09 fixture-skipped di e2e)
expected: Edit pekerja yang hapus unit yang masih dirujuk CoachCoacheeMapping.AssignmentUnit aktif → modal konfirmasi muncul → setujui → mapping ter-deactivate + unit terhapus (1 transaksi). Server guard sudah unit-tested (RemoveUnitGuardTests 5/5); yang belum: alur UI→confirm→deactivate runtime.
result: pass
evidence: |
  Fixture: pekerja T399UAT01 (GAST, primary "RFCC NHT (053)" + secondary "Alkylation Unit (065)") via UI; CoachCoacheeMapping Id=13 aktif AssignmentUnit="Alkylation Unit (065)" via SQL.
  Edit → uncheck "Alkylation Unit (065)" → submit → #mu07ConfirmModal VISIBLE (display:block), body "Mapping coach aktif unit 'Alkylation Unit (065)' akan dinonaktifkan", tombol merah "Ya, Hapus & Nonaktifkan" (name=ConfirmedDeactivate value=true). Screenshot 399-uat-test1-mu07-modal.png.
  Klik confirm → redirect ManageWorkers. DB VERIFIED: mapping Id13 IsActive=0 + EndDate SET; worker1 UserUnits tinggal "RFCC NHT (053)" (primary), Alkylation terhapus; mirror Users.Unit="RFCC NHT (053)" — 1 transaksi atomik.

### 2. MU-07 PROTON hard-block — red error render
expected: Edit pekerja yang hapus unit yang masih dirujuk ProtonTrackAssignment aktif → error merah hard-block ("Tutup/bypass PROTON dulu"), simpan ditolak. Server logic unit-tested; render error di browser belum dikonfirmasi (fixture PROTON-aktif absent di e2e).
result: pass
evidence: |
  Fixture: pekerja T399UAT02 (GAST, 2 unit) via UI; ProtonTrackAssignment Id=9 aktif (ProtonTrackId=1) via SQL, tanpa mapping.
  Edit → uncheck primary "RFCC NHT (053)" → submit → .alert-danger MERAH "Terdapat kesalahan: Tidak bisa menghapus Unit 'RFCC NHT (053)': masih ada PROTON tahun-berjalan aktif. Tutup atau bypass PROTON terlebih dahulu melalui halaman PROTON." TANPA modal (hard-block, bukan confirm). Screenshot 399-uat-test2-proton-block.png.
  DB VERIFIED: worker2 UserUnits TETAP 2 (RFCC NHT primary + Alkylation), mirror unchanged, PTA Id9 masih aktif — form DITOLAK, no orphan.

### 3. _PSign cetak + tampilan badge visual
expected: Kartu tanda tangan `_PSign` (cetak/cert) tampil SEMUA unit primary-first koma (D-07). Badge primary (hijau bg-success + bintang + "Utama") vs secondary kontras cukup di Home hero (background gelap) + ManageWorkers; cell tidak pecah/wrap aneh. Inherently visual — perlu mata manusia.
result: pass
evidence: |
  5 surface diverifikasi visual (pekerja multi-unit T399UAT02):
  - WorkerDetail: badge hijau "★ RFCC NHT (053) Utama" + abu "Alkylation Unit (065)" (399-uat-test3-workerdetail.png).
  - ManageWorkers: cell badge primary-first 2 baris, kontras OK, no wrap aneh (399-uat-test3-manageworkers.png).
  - Home hero (background gradient gelap): badge "★ RFCC NHT (053) (Utama)" + "Alkylation Unit (065)" putih legible kontras cukup (399-uat-test3-home-hero.png).
  - Profile: Unit row 2 badge + **Preview P-Sign card = "RFCC NHT (053), Alkylation Unit (065)"** (SEMUA unit primary-first comma-join, D-07 — BUKAN primary-only) (399-uat-test3-profile-psign.png).
  Settings = pola identik Profile (sama VM/partial); Excel kolom-7 unit-tested. _PSign all-units D-07 = item kritis → CONFIRMED.

## Summary

total: 3
passed: 3
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

[none — all 3 deferred items verified via Playwright drive + SQL fixture]
