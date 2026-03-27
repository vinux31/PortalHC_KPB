# Phase 265: Worker Exam Flow — UAT Scenarios

**Server:** http://10.55.3.3/KPB-PortalHC/
**Date:** 2026-03-27

## Test Accounts

| Worker | Email | Password | Skenario |
|--------|-------|----------|----------|
| Rino Prasetyo | rino.prasetyo@pertamina.com | 123456 | Token + Happy Path Lengkap |
| Mohammad Arsyad | mohammad.arsyad@pertamina.com | Pertamina@2026 | Non-Token + Pagination |
| Moch Widyadhana | moch.widyadhana@pertamina.com | Balikpapan@2026 | Abandon Exam |

## Skenario 1 — rino.prasetyo: Assessment List + Token + Happy Path (EXAM-01, EXAM-02, EXAM-03, EXAM-04, EXAM-05, EXAM-07)

**Assessment:** UAT OJT Test 1 - Token (ID=7, token=U6J49L, 5 soal, 30 min, pass=70%)

### Langkah-langkah:

1. **Login** sebagai rino.prasetyo@pertamina.com / 123456
2. **Navigasi ke /CMP/Assessment** — verifikasi (EXAM-01):
   - Assessment cards tampil dengan status badge (Open/Upcoming)
   - Jadwal dan durasi tampil benar
   - Tab filtering berfungsi
   - Assessment 7 "UAT OJT Test 1 - Token" ada di list
3. **Klik "Start Assessment"** pada assessment 7 — verifikasi (EXAM-02):
   - Modal token muncul (karena IsTokenRequired=true)
   - Input 6-digit, auto-uppercase
   - Masukkan token "u6j49l" (lowercase) → harus auto-convert ke U6J49L
   - AJAX POST VerifyToken berhasil → redirect ke StartExam/7
4. **Di halaman StartExam** — verifikasi (EXAM-03):
   - 5 soal ditampilkan (1 halaman karena <=10 soal/halaman)
   - Setiap soal memiliki radio button options
   - Nomor soal tampil benar (1-5)
5. **Timer** — verifikasi (EXAM-04):
   - Timer countdown di sticky header
   - Format MM:SS
   - Timer berjalan (angka berkurang setiap detik)
6. **Auto-save** — verifikasi (EXAM-05):
   - Klik radio jawaban soal 1 → observe network badge berubah "Menyimpan..." lalu "Tersimpan"
   - Jawab soal 2, 3 juga
   - DB: SELECT dari PackageUserResponses WHERE AssessmentSessionId = (session rino) — harus ada 3 rows
7. **Network badges** — verifikasi (EXAM-07):
   - #hubStatusBadge = "Live" (badge hijau)
   - #networkStatusBadge = "Tersimpan" setelah save berhasil

## Skenario 2 — mohammad.arsyad: Non-Token Happy Path + Pagination (EXAM-01, EXAM-03, EXAM-06)

**Assessment:** UAT OJT Test 2 - No Token (ID=10, tanpa token, 15 soal, 60 min, pass=80%)

### Langkah-langkah:

1. **Login** sebagai mohammad.arsyad@pertamina.com / Pertamina@2026
2. **Navigasi ke /CMP/Assessment** — verifikasi assessment 10 ada
3. **Klik "Start Assessment"** pada assessment 10 — verifikasi (EXAM-01):
   - TIDAK ada modal token (IsTokenRequired=false)
   - Confirm dialog muncul → redirect ke StartExam/10
4. **Di StartExam** — verifikasi (EXAM-03):
   - 15 soal ditampilkan, paginated: halaman 1 = soal 1-10, halaman 2 = soal 11-15
5. **Navigasi halaman** — verifikasi (EXAM-06):
   - Klik "Next" → halaman 2 (soal 11-15)
   - Klik "Previous" → kembali ke halaman 1
   - Klik nomor halaman langsung (jump) jika ada
   - Jawab beberapa soal di halaman 1, pindah ke halaman 2, kembali → jawaban masih tercentang
   - Pagination indicator menunjukkan halaman aktif

## Skenario 3 — moch.widyadhana: Abandon Exam (EXAM-08)

**Assessment:** Assessment 7 atau 10 (mana saja)

### Langkah-langkah:

1. **Login** sebagai moch.widyadhana@pertamina.com / Balikpapan@2026
2. **Mulai ujian** di salah satu assessment
3. Jawab 1-2 soal (opsional)
4. **Klik "Keluar Ujian"** — verifikasi (EXAM-08):
   - Confirm dialog muncul
   - Setelah confirm → redirect ke /CMP/Assessment
   - DB: AssessmentSessions WHERE UserId=(widyadhana) → Status = 'Abandoned'
5. **Coba masuk ujian lagi** — verifikasi (EXAM-08):
   - Klik "Start Assessment" pada assessment yang sama
   - Harus ditolak: redirect dengan pesan error

## Requirement Coverage

| Req ID | Description | Skenario |
|--------|-------------|----------|
| EXAM-01 | Assessment list (badge, jadwal) | 1, 2 |
| EXAM-02 | Token verification | 1 |
| EXAM-03 | Start exam, soal tampil | 1, 2 |
| EXAM-04 | Timer akurat | 1 |
| EXAM-05 | Auto-save jawaban | 1 |
| EXAM-06 | Navigasi halaman | 2 |
| EXAM-07 | Network badges | 1 |
| EXAM-08 | Abandon exam | 3 |
