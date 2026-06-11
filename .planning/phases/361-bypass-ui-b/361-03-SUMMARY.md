---
phase: 361-bypass-ui-b
plan: 03
status: complete
completed: 2026-06-11
commits:
  - 24b52004: "feat(361-03): restructure Override jadi 2 tab + markup Tab2 Bypass Tahun [D-01,D-14,D-16]"
  - 0d507eaa: "feat(361-03): JS IIFE Tab2 Bypass - wizard, pending panel, deep-link, showToast [D-02..D-21]"
key-files:
  created: []
  modified:
    - Views/ProtonData/Override.cshtml
---

# Plan 361-03 Summary — UI Tab2 Bypass Tahun

**One-liner:** Override jadi 2 tab (Tab1 utuh) + Tab2 lengkap: panel pending selalu-tampil, filter+search worker, wizard modal 3-step linear, modal konfirmasi, showToast baru, deep-link routing. 447→1147 baris, build hijau.

## What was built

1. **Task 1 (`24b52004`):** nav-tabs shell `tab-deliverable`/`tab-bypass`; markup Tab1 existing dibungkus `#pane-deliverable` byte-for-byte (filter card + `#overrideTableContainer` + `overrideModal`); `#pane-bypass`: panel "Menunggu Konfirmasi (N)" selalu tampil (D-16) + filter cascade distinct (`bypassBagian/Unit/Track/btnLoadBypass`, tanpa Status Filter) + `#bypassSearch` + empty-state; `#bypassWizardModal` (indicator 1-2-3, 3 step div, footer Kembali/Lanjut/Jalankan Bypass) + `#bypassConfirmModal` (Tutup/Batal/Konfirmasi Pindah); `var allCoaches` dari ViewBag Plan 01; dropdown coach default "Pertahankan coach saat ini".
2. **Task 2 (`0d507eaa`):** IIFE Tab2 terpisah (Tab1 tak disentuh):
   - `showToast(message, variant)` top-level baru, auto-dismiss 4s, stack offset.
   - Cascade `wireCascade` reuse `orgStructure` untuk filter + `wizTargetBagian→wizTargetUnit` (D-11).
   - `loadPendingPanel()` bare-array → badge D-17, [Lihat & Konfirmasi] hanya Siap (D-13), [Batal] selalu; cache `pendingRows` untuk deep-link stale check.
   - `loadBypassWorkers()` + `renderWorkerTable()` search client-side (D-15), kolom 5 sesuai spec §9.
   - Wizard state machine linear (D-02): step1 validasi Δurutan≤1 + anti same-track client-side, step2 4 mode card eligible/disabled+alasan (D-10), step3 recap + warning verbatim per mode (D-03), submit `DurationMinutes: null` (D-09) + reminder paket soal CL-B(b) (D-04).
   - Modal konfirmasi render field D-18 (skor/tanggal/reason/coach); `postPendingAction` disable+spinner (D-21), toast pesan backend + auto-refresh (D-20); confirm dialog batal (D-19).
   - Deep-link: `URLSearchParams` → `bootstrap.Tab.show()` → auto-open modal pending (D-05) / toast stale (D-06); lazy-load `shown.bs.tab` (D-07); `history.replaceState` (D-08).

## Verification

- `dotnet build` Build succeeded 0 Error — 3x (per task + setelah perbaikan appUrl call-site).
- Semua acceptance grep pass (tab ids 2/2, pane 2/2, Tab1 ids 17 hits utuh, hx-* = 0, showToast 1, GET appUrl 3, POST appUrl 4, RequestVerificationToken 5, deep-link/replaceState/Tab() ada, copy D-06/D-17/D-19/warning ada, spinner 7).
- Runtime verify penuh dilakukan Plan 04 (e2e + UAT) sesuai plan.

## Deviations

- **escHtml reuse tidak mungkin tanpa ubah Tab1** — `escHtml`/`statusToBadgeClass` scoped DI DALAM IIFE Tab1. Solusi: `showToast` top-level + `escB()` lokal Tab2 (body identik escHtml). Tab1 utuh; semua innerHTML Tab2 tetap escaped (T-361-08 mitigated).
- `postPendingAction` menerima URL penuh (`appUrl(...)` di call site) supaya kontrak key-link `appUrl('/ProtonData/Bypass...` eksplisit ter-grep.

## Self-Check: PASSED
