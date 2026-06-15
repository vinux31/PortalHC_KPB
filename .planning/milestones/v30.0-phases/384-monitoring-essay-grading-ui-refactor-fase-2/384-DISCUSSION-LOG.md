# Phase 384: Monitoring Essay Grading UI Refactor (Fase 2) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-15
**Phase:** 384-monitoring-essay-grading-ui-refactor-fase-2
**Areas discussed:** Tabel worker, Navigasi & return flow, Layout page per-worker, Status & empty state

---

## Tabel worker (isi & kolom & urutan)

| Option | Description | Selected |
|--------|-------------|----------|
| Hanya yg punya essay, pending dulu | Filter HasManualGrading, urut pending di atas | |
| Hanya yg punya essay, urut NIP/nama | Filter HasManualGrading, urut alfabet/NIP | ✓ |
| Semua worker session | Termasuk worker murni MC ditandai '—' | |

**User's choice:** Hanya yg punya essay (HasManualGrading), urut NIP/nama
**Notes:** Worker murni MC tak muncul. Kolom: Worker/NIP, jumlah belum dinilai, status, tombol Tinjau Essay.

---

## Navigasi & return flow

| Option | Description | Selected |
|--------|-------------|----------|
| Redirect ke Monitoring Detail + toast | Setelah Selesaikan Penilaian redirect balik + toast | |
| Tetap di page, tampilkan status selesai | Tak redirect; page update in-place jadi "Selesai" + tombol kembali manual | ✓ |
| Redirect + auto-buka worker pending berikutnya | Alur grading berantai | |

**User's choice:** Tetap di page, tampilkan status selesai
**Notes:** No auto-redirect. User klik "Kembali" manual. Status tabel ter-update saat reload/poll.

---

## Layout page per-worker + Simpan Skor

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse kartu + JS AJAX existing | Clone kartu essay 407-446, reuse handler JS AJAX, header worker, breadcrumb back | ✓ |
| Reuse kartu, Simpan Skor full reload | Kartu sama, Simpan Skor = form POST reload | |
| Layout baru ringkas (1 form submit sekaligus) | Redesign semua essay 1 form | |

**User's choice:** Reuse kartu + JS AJAX existing
**Notes:** Handler JS (`btn-save-essay-score`/`btn-finalize-grading`) saat ini inline `<script>` di view → planner putuskan extract vs duplikat.

---

## Status worker (badge) + finalized + empty state

| Option | Description | Selected |
|--------|-------------|----------|
| 3-state; finalized=read-only; hidden bila kosong | Badge 3 state; setelah finalized page read-only; section hidden bila tak ada essay | ✓ |
| 2-state; Tinjau hilang setelah finalized | Badge belum/selesai; tombol Tinjau hilang setelah finalized | |
| 3-state; Tinjau selalu aktif (re-grade) | Bisa edit skor walau finalized (⚠ mungkin bentrok backend) | |

**User's choice:** 3-state; finalized=read-only; hidden bila kosong
**Notes:** Badge 🟡 {N} belum dinilai / 🔵 Siap difinalisasi / 🟢 Selesai (gate Phase 310 D-02). Finalized → page read-only (input disabled), backend unchanged. Section disembunyikan bila essayGradingMap kosong.

## Claude's Discretion

- Route/URL shape GET action baru.
- Perlu ViewModel worker-list baru atau cukup reuse MonitoringSessionViewModel.
- Cara load EssayGradingItemViewModel untuk 1 session (clone map-builder).
- Authorization page baru (Admin/HC).
- Extract inline JS handler ke shared script/partial vs duplikat.

## Deferred Ideas

- Alur grading berantai (auto-buka worker pending berikutnya) — ditolak (user pilih tetap di page).
- Re-grade setelah finalized — ditolak (user pilih read-only); butuh perubahan backend, di luar scope.
- Todo `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` — reviewed, tak di-fold (out of scope).
