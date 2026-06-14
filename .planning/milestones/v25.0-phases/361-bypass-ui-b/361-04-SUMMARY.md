---
phase: 361-bypass-ui-b
plan: 04
status: complete
completed: 2026-06-11
commits:
  - "test(361-04): e2e spec proton-bypass - snapshot/seed/assert/restore [D-22,D-23]"
  - c278b54e: "docs(361-04): tandai seed journal Phase 361 cleaned - DB lokal restored"
key-files:
  created:
    - tests/e2e/proton-bypass.spec.ts
  modified:
    - docs/SEED_JOURNAL.md
---

# Plan 361-04 Summary — e2e Spec + UAT Live

**One-liner:** Spec committed `proton-bypass.spec.ts` 6/6 PASS live @5277 + UAT MCP penuh (4 closure mode, konfirmasi §5.3, D-11 negative, deep-link, batal) — DB restored, journal cleaned.

## What was built

1. **Task 1:** `tests/e2e/proton-bypass.spec.ts` (250 baris, serial) — beforeAll backup (InstanceDefaultBackupPath) + execScript fixture; afterAll restore; 5 test:
   - T1 2 tab + Tab1 utuh + URL sync; T2 wizard linear + Δ-validasi + card eligible/disabled D-10 + recap/warning; T3 CL-A submit + CL-B(b) pending + reminder paket (D-04); T4 deep-link auto-open (D-05) + stale toast (D-06); T5 refleksi badge Siap↔Menunggu (D-17/D-13) + modal D-18 skor + batal confirm dialog (D-19).
   - **Run live: 6 passed 32.2s** (1 fix kecil: helper `queryString` tolak output kosong → batch UPDATE diberi `SELECT 'OK'`).

2. **Task 2 (checkpoint UAT, dieksekusi Claude via Playwright MCP atas permintaan user):**
   - Tab1 regresi: cascade GAST→Alkylation→track + Muat Data render normal (empty = jalur existing OverrideList, fixture worker tanpa coach mapping — bukan regresi).
   - Tab2: URL `?tab=bypass`, panel pending (1) badge "Menunggu Exam" + hanya [Batal] (D-13/D-17), tabel worker 4 baris sesuai fixture.
   - Wizard: linear ✓, validasi Δ>1 error inline ✓, card mode per state — Worker B (CL-A disabled), Worker C punya final (CL-B(a)/(b) disabled + alasan, D-D) ✓.
   - **CL-B(a)** Regan: submit sukses, toast + catatan bootstrap, tabel auto-refresh (worker pindah). **CL-C** Arsyad: sukses, pindah tanpa nilai. (CL-A + CL-B(b) ter-cover spec T3 → **4/4 mode terverifikasi**.)
   - **Deep-link** `?tab=bypass&pending=3` → modal auto-open detail D-18 lengkap (Lulus skor 90, coach "Pertahankan").
   - **D-11 negative:** Konfirmasi tanpa penanda Origin='Exam' → backend tolak ("Kondisi rencana sudah berubah... Konfirmasi dibatalkan" di AuditLogs), pending tetap Siap, panel refresh (D-20).
   - **§5.3 positive:** setelah penanda Exam dibuat → Konfirmasi Pindah sukses ("Pindah tahun berhasil"), pending `Selesai`+ResolvedAt, assignment baru track 2 aktif **Origin='Bypass'** (D-04 360), lama deactivated, panel 0 → empty state.

3. **Task 3 (`c278b54e`):** SEED_JOURNAL entry 361 → **cleaned** (restore manual `WITH REPLACE` 1954 pages + verifikasi fixture rows 0/0/0).

## Verification

- Spec: `--list` parse bersih; run live 6/6 PASS.
- UAT: semua skenario SC3 (4 mode + pending konfirmasi + batal + re-grade-refleksi + deep-link) + SC1 Tab1 utuh.
- DB lokal bersih (fixture 0 rows post-restore).

## Deviations

- **Checkpoint human-verify dieksekusi Claude** via Playwright MCP atas instruksi user ("kamu sudah verifikasi?") — bukti tercatat di summary ini + AuditLogs.
- **T6 re-grade via UI Edit Nilai** tidak feasible untuk sesi bare tanpa paket soal → diganti (sesuai opsi plan): refleksi UI state via DB (spec T5) + D-11 negative test live (lebih kuat — membuktikan guard backend + surfacing UI). Logic flip service ter-cover xUnit 360.
- Fix `queryString` empty-output: batch UPDATE di spec diberi trailing `SELECT 'OK'`.

## Self-Check: PASSED
