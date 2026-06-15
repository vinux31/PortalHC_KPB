---
status: passed
phase: 356-audit-fix-assign-coach-coachee
source: [356-VERIFICATION.md, 356-05-SUMMARY.md]
started: 2026-06-09
updated: 2026-06-09
verifier: Claude (Playwright MCP @localhost:5277) + user sign-off
---

## Current Test

Selesai — UAT dijalankan Claude via Playwright MCP atas permintaan user ("verifikasi via browser, catat jika ada temuan"), user sign-off "approve".

## Tests

### 1. AF-1 — coachee track multi-unit eligible (headline)
expected: Coachee Alkylation 3/3 Approved muncul di GetEligibleCoachees track id=4; 2/3 tidak.
result: PASS — `GET /Admin/GetEligibleCoachees?protonTrackId=4` → Rino muncul (3/3); flip 1→Pending → `[]`. Old code 3==4(total) → tak pernah muncul; new per-unit 3==3 → muncul.

### 2. AF-2 — UI guard 1-unit/batch
expected: centang coachee unit X → checkbox unit lain disabled+redup+hint; clear → reset; display tak tersentuh.
result: PASS — `updateAssignmentDefaults()` exercised (DOM 2-unit sim, data nyata 1 unit): cross-unit disabled+text-muted+hint shown; clear → re-enable+hint hidden; style.display untouched.

### 3. AF-3 / D-06 — graduate per-unit + badge
expected: graduated (IsActive=0) tampil badge "Graduated" (bukan "Aktifkan"); re-assignable unit lain.
result: PASS — state-sim IsActive=0+IsCompleted=1 → showAll tampil badge "Graduated" + Edit only; graduated kembali ke pool eligible coachee. Action transaksi/cascade code-verified (full graduate butuh fixture Tahun-3).

### 4. AF-5 — notif reassign 3 recipient
expected: reassign → 3 notif (coach lama/baru/coachee).
result: PASS — ApproveReassignSuggestion → 3 row UserNotifications COACH_REASSIGNED: "Penugasan Coaching Dialihkan" / "Coach Ditunjuk" / "Coach Anda Berubah".

### 5. AF-7 — progression-warning parity
expected: coachee tanpa prev-track complete → warning verbatim; tak ada perubahan perilaku.
result: PASS — coachee tanpa Operator-Tahun-1 → "1 coachee belum menyelesaikan Operator - Tahun 1. Tetap lanjutkan?" (no insert); coachee dgn prev complete → assign tanpa warning. Kedua cabang batch-query terbukti.

### 6. AF-6 — pesan duplikat spesifik
expected: race duplikat → pesan ramah spesifik, no leak.
result: CODE-VERIFIED — race tak reprodusibel single-thread; catch DbUpdateException unique-index sebelum generic + pesan ramah + no ex.Message leak (grep).

### 7. Regresi assign/deactivate/reactivate + gate
expected: tak ada regresi; build+test hijau.
result: PASS — dotnet build 0 error; dotnet test 135/135 (131 baseline + 4 AF-1); 0 regresi.

## Summary

total: 7
passed: 6
code_verified: 1
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

None blocking. Test-hardening opsional (AF-3 full graduate e2e + AF-6 race) → backlog Phase 999.5. Code review WR-01 + WR-02 fixed (commit e672a110). DB lokal di-restore bersih (SEED_JOURNAL cleaned). User sign-off: approve.
