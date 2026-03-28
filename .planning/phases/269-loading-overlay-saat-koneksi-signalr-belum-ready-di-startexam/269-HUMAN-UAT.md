---
status: partial
phase: 269-loading-overlay-saat-koneksi-signalr-belum-ready-di-startexam
source: [269-VERIFICATION.md]
started: 2026-03-28T12:00:00+07:00
updated: 2026-03-28T12:00:00+07:00
---

## Current Test

[awaiting human testing]

## Tests

### 1. Overlay visual appearance & fade-out
expected: Buka StartExam, overlay full-screen semi-transparan tampil dengan spinner dan teks "Mempersiapkan ujian..." + "Menghubungkan ke server...". Setelah hub connected (~1 detik), teks berubah "Terhubung!" lalu overlay fade-out ~300ms.
result: [pending]

### 2. Keyboard/klik diblokir saat overlay tampil
expected: Selama overlay aktif, klik soal tidak merespons. Tab/arrow key tidak memindahkan fokus ke elemen di belakang overlay.
result: [pending]

### 3. Error state saat koneksi gagal
expected: Blokir WebSocket di DevTools Network tab, overlay berubah ke error state: spinner hilang, teks "Koneksi gagal. Periksa jaringan Anda.", tombol "Muat Ulang" tampil. Klik tombol → reload page.
result: [pending]

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps
