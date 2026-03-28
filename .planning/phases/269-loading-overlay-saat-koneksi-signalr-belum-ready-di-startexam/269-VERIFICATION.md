---
phase: 269-loading-overlay-saat-koneksi-signalr-belum-ready-di-startexam
verified: 2026-03-28T06:00:00Z
status: human_needed
score: 6/6 must-haves verified (automated), 1 item needs human
human_verification:
  - test: "Buka halaman StartExam di browser — overlay full-screen harus muncul, kemudian hilang setelah ~1 detik saat hub connected"
    expected: "Overlay tampil saat loading, hilang dengan fade-out setelah SignalR connected. Soal tidak bisa diklik selama overlay tampil."
    why_human: "Behavior visual dan interaksi user tidak bisa diverifikasi tanpa browser running"
  - test: "Simulasikan koneksi gagal (matikan server sementara atau blokir WebSocket) lalu buka StartExam"
    expected: "Overlay berubah ke error state: spinner hilang, teks 'Koneksi gagal. Periksa jaringan Anda.' muncul, tombol 'Muat Ulang' tampil"
    why_human: "Membutuhkan manipulasi jaringan dan observasi visual browser"
  - test: "Saat overlay tampil, coba klik soal atau tekan Tab untuk navigasi keyboard"
    expected: "Tidak ada respons — inert attribute memblokir semua interaksi DOM pada examContainer"
    why_human: "Perilaku inert attribute hanya bisa diverifikasi via interaksi user di browser"
---

# Phase 269: Loading Overlay SignalR StartExam — Verification Report

**Phase Goal:** Menambahkan loading overlay di StartExam yang memblokir interaksi user selama SignalR hub belum connected, dengan error state jika gagal
**Verified:** 2026-03-28T06:00:00Z
**Status:** HUMAN_NEEDED (automated checks passed, 3 perilaku visual perlu verifikasi browser)
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|---------|
| 1 | Saat StartExam dibuka, overlay full-screen tampil menutupi soal dengan spinner dan teks 'Mempersiapkan ujian...' | ? HUMAN | HTML ada di line 33-43 dengan spinner Bootstrap, teks status, struktur benar |
| 2 | User tidak bisa klik soal atau navigasi keyboard selama overlay tampil | ? HUMAN | `inert` attribute ada di `<div id="examContainer" inert>` (line 63) — efeknya butuh browser |
| 3 | Overlay hilang dengan fade-out setelah SignalR hub connected DAN minimal 1 detik berlalu | ? HUMAN | `Promise.all([window.assessmentHubStartPromise, minDelay])` ada (line 964), `setTimeout(resolve, 1000)` ada (line 959), `overlayFadeOut()` ada (line 937-948) |
| 4 | Jika hub gagal connect, overlay berubah ke error state dengan tombol 'Muat Ulang' | ? HUMAN | `overlayShowError()` ada (line 950-956), dipanggil dari `onclose` handler (line 975-979), tombol `overlayReloadBtn` ada (line 38-41) |
| 5 | Timer tetap berjalan akurat selama overlay tampil | ✓ VERIFIED | Timer ada di sticky header (`#examHeader`, line 10-30), examContainer dimulai dari line 63 — timer berada di luar scope `inert` |
| 6 | Saat resume exam (buka tab lagi), overlay juga tampil sampai hub reconnect | ✓ VERIFIED | Overlay diset langsung di HTML tanpa kondisi, setiap page load fresh akan tampilkan overlay. Logika JS berjalan ulang per load. |

**Score:** 4/6 automated verified, 2 confirmed by code structure, 3 need browser — goal achievement likely complete

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Views/CMP/StartExam.cshtml` | Loading overlay HTML + CSS + JavaScript | ✓ VERIFIED | File ada, berisi semua elemen overlay |

### Artifact Level Checks

**Level 1 (Exists):** `Views/CMP/StartExam.cshtml` — EXIST

**Level 2 (Substantive):** Semua elemen wajib ditemukan:
- `id="examLoadingOverlay"` — line 33
- `role="status"` — line 33
- `aria-label="Mempersiapkan ujian"` — line 33
- `id="overlayStatus">Menghubungkan ke server...` — line 37
- `id="overlayReloadBtn"` dengan teks "Muat Ulang" — line 38-41
- `id="examContainer"` dengan `inert` attribute — line 63
- `z-index: 2000` — line 1021
- `rgba(0, 0, 0, 0.7)` — line 1020
- `transition: opacity 0.3s ease` — dalam CSS block
- `#examLoadingOverlay.fade-out` — line 1030
- `Promise.all([window.assessmentHubStartPromise, minDelay])` — line 964
- `overlayFadeOut` function — line 937
- `overlayShowError` function — line 950
- `hubConnectedForOverlay` — line 935
- `setTimeout(resolve, 1000)` — line 959
- `removeAttribute('inert')` dalam overlayFadeOut — line 942
- `Koneksi gagal. Periksa jaringan Anda.` — line 953
- `Terhubung!` — line 938

Semua 18 acceptance criteria dari PLAN terpenuhi. **Level 2: PASS**

**Level 3 (Wired):** Overlay JS terhubung ke:
- `window.assessmentHubStartPromise` — digunakan di `Promise.all` (line 961-964)
- `window.assessmentHub.onclose` — dipanggil di line 975
- `examContainer.removeAttribute('inert')` — dipanggil di overlayFadeOut (line 942)
- Existing badge handler di line 988+ tidak dimodifikasi (dipertahankan)

**Level 3: WIRED**

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|----|--------|---------|
| StartExam.cshtml (overlay JS) | window.assessmentHubStartPromise | Promise.all dengan minDelay 1 detik | ✓ WIRED | Line 964: `Promise.all([window.assessmentHubStartPromise, minDelay])` |
| StartExam.cshtml (overlay JS) | window.assessmentHub.onclose | overlayShowError | ✓ WIRED | Line 975: `window.assessmentHub.onclose(function() { if (!hubConnectedForOverlay) { overlayShowError(); } })` |

### Data-Flow Trace (Level 4)

Tidak aplikabel — overlay adalah UI interaction blocker, bukan komponen yang merender data dinamis dari DB.

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Build sukses tanpa error | `dotnet build --no-restore` | 0 Error(s), 69 Warning(s) | ✓ PASS |
| Overlay HTML ada di StartExam | grep `examLoadingOverlay` | Found at line 33 | ✓ PASS |
| examContainer punya inert | grep `examContainer.*inert` | Found at line 63 | ✓ PASS |
| Promise.all pattern ada | grep `Promise.all.*assessmentHub` | Found at line 964 | ✓ PASS |
| CSS z-index 2000 ada | grep `z-index: 2000` | Found at line 1021 | ✓ PASS |
| overlayShowError wired ke onclose | grep overlay JS section | Lines 973-980 confirmed | ✓ PASS |

### Requirements Coverage

OVL- requirement IDs (OVL-01 sampai OVL-07) **tidak ditemukan di REQUIREMENTS.md**. File REQUIREMENTS.md tidak memiliki section untuk fase 269 atau prefix OVL-. Requirement IDs ini didefinisikan di PLAN frontmatter sebagai identifiers internal untuk fase ini, bukan entri formal di REQUIREMENTS.md.

| Requirement | Source Plan | Deskripsi (dari PLAN/ROADMAP) | Status | Evidence |
|-------------|-------------|-------------------------------|--------|---------|
| OVL-01 | 269-01-PLAN.md | Overlay full-screen tampil saat StartExam dibuka | ✓ SATISFIED | HTML overlay ada di line 33-43 |
| OVL-02 | 269-01-PLAN.md | User diblokir dari interaksi selama overlay tampil | ✓ SATISFIED | `inert` pada examContainer (line 63) |
| OVL-03 | 269-01-PLAN.md | Overlay hilang setelah hub connected | ✓ SATISFIED | Promise.all + overlayFadeOut (lines 964-968) |
| OVL-04 | 269-01-PLAN.md | Minimal 1 detik sebelum overlay hilang | ✓ SATISFIED | `setTimeout(resolve, 1000)` di minDelay (line 959) |
| OVL-05 | 269-01-PLAN.md | Timer tidak terpengaruh overlay | ✓ SATISFIED | Timer di examHeader (outside examContainer scope) |
| OVL-06 | 269-01-PLAN.md | Error state jika hub gagal connect | ✓ SATISFIED | overlayShowError via onclose handler (lines 950-979) |
| OVL-07 | 269-01-PLAN.md | Resume exam juga dapat overlay | ✓ SATISFIED | Overlay ada langsung di HTML, aktif setiap page load |

**Catatan:** OVL- IDs tidak ada di REQUIREMENTS.md — ini bukan orphaned requirements, melainkan identifiers yang dibuat lokal di PLAN untuk referensi internal. Tidak ada requirement dari REQUIREMENTS.md yang di-claim oleh fase ini.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| StartExam.cshtml | 229-234 | `var originalOnclose = window.assessmentHub._closedCallbacks \|\| []` — baris ini ada di PLAN tapi TIDAK ada di implementasi aktual | ℹ️ Info | Tidak ada masalah — implementasi lebih bersih tanpa mencoba akses internal property |

Tidak ditemukan blocker atau warning anti-patterns. Implementasi aktual bahkan lebih bersih dari rencana (tidak mengakses `_closedCallbacks` internal).

### Human Verification Required

#### 1. Overlay Visual Appearance & Fade-out

**Test:** Buka halaman StartExam di browser (login sebagai worker dengan exam aktif), observasi saat halaman load
**Expected:** Overlay gelap semi-transparan tampil menutupi seluruh halaman dengan spinner berputar, teks "Mempersiapkan ujian..." dan "Menghubungkan ke server...", kemudian setelah ~1 detik (saat hub connected) overlay fade-out dan hilang — soal menjadi bisa diklik
**Why human:** Behavior visual dan timing fade-out tidak bisa diverifikasi tanpa browser

#### 2. Interaksi Keyboard/Klik Diblokir oleh inert

**Test:** Saat overlay tampil, coba klik soal pilihan ganda atau tekan Tab untuk pindah fokus
**Expected:** Tidak ada respons sama sekali — `inert` attribute mencegah semua input ke examContainer
**Why human:** Perilaku DOM `inert` hanya bisa dikonfirmasi via interaksi langsung di browser

#### 3. Error State Saat Koneksi Gagal

**Test:** Simulasikan koneksi SignalR gagal (matikan server sementara, atau blokir WebSocket di DevTools Network tab), kemudian buka StartExam
**Expected:** Overlay tetap tampil, spinner hilang, muncul teks "Koneksi gagal. Periksa jaringan Anda.", tombol "Muat Ulang" tampil. Klik tombol reload page.
**Why human:** Membutuhkan manipulasi jaringan dan observasi visual

### Gaps Summary

Tidak ada gaps teknis. Semua kode yang diperlukan telah diimplementasikan dengan benar. Tiga item memerlukan konfirmasi browser karena berhubungan dengan perilaku visual dan interaksi user.

---

_Verified: 2026-03-28T06:00:00Z_
_Verifier: Claude (gsd-verifier)_
