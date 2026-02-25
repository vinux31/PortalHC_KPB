---
status: complete
phase: 44-real-time-monitoring
source: 44-01-SUMMARY.md, 44-02-SUMMARY.md
started: 2026-02-25T00:00:00Z
updated: 2026-02-25T00:00:00Z
---

## Current Test
<!-- OVERWRITE each test - shows where we are -->

[testing complete]

## Tests

### 1. 4-Card Summary Panel
expected: Buka AssessmentMonitoringDetail (halaman monitoring group active). Di bagian atas tabel, ada 4 kartu summary: Total, Completed, InProgress, NotStarted. (Sebelumnya hanya 3 kartu: Total/Completed/PassRate)
result: pass

### 2. NIP Column Dihapus + Progress Column
expected: Tabel tidak ada kolom NIP. Ada kolom "Progress" yang menampilkan format —/N (misal: —/20) saat halaman pertama dimuat, sebelum polling mengisi angka jawaban.
result: pass

### 3. Time Remaining Column
expected: Ada kolom "Time Remaining" sebelum kolom Actions. Untuk session InProgress, tampil countdown (misal: 23:45). Untuk session Completed/Abandoned/Not started, kolom ini kosong atau —.
result: pass

### 4. Progress Auto-Update (10s Polling)
expected: Tunggu ~10 detik tanpa refresh manual. Kolom Progress di baris session InProgress berubah dari — ke angka aktual (misal: 5/20). Kolom Status, Score, dll juga ikut update.
result: pass

### 5. Time Remaining Countdown (1s Tick)
expected: Untuk session yang InProgress, angka di kolom Time Remaining berkurang 1 setiap detik (client-side tick). Tidak perlu nunggu 10s — langsung terlihat turun tiap detik.
result: pass

### 6. Action Buttons per Status
expected: Cek tombol di kolom Actions untuk masing-masing status:
- InProgress → tombol "Force Close"
- Completed → tombol "View Results" + "Reset"
- Abandoned → tombol "Reset"
- Not started → tombol "Force Close"
result: pass

### 7. Last Updated Indicator
expected: Di bawah tabel ada teks "Last updated: HH:MM:SS" yang berubah setiap 10s setelah polling berhasil.
result: pass

### 8. Polling Berhenti Saat Semua Selesai
expected: Ketika semua session sudah Completed, tombol "Submit Assessment" (tutup lebih awal) hilang dari halaman, dan polling berhenti (tidak ada lagi request ke server setiap 10s).
result: pass

## Summary

total: 8
passed: 8
issues: 0
pending: 0
skipped: 0

## Gaps

[none yet]
