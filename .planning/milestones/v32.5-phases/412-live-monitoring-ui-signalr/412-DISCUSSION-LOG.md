# Phase 412: Live Monitoring UI + SignalR - Discussion Log

> **Audit trail only.** Decisions in CONTEXT.md.

**Date:** 2026-06-21
**Phase:** 412-live-monitoring-ui-signalr
**Areas discussed:** Trigger modal keras, UX force-kick examRemoved, Input alasan modal, Panel Peserta Dikeluarkan + Restore

---

## Trigger modal konfirmasi keras
| Option | Selected |
|--------|----------|
| InProgress + Completed-cert (belum-mulai ringan) | ✓ |
| Hanya InProgress | |
| Semua soft-remove | |

**Choice:** Modal keras untuk InProgress + Completed-bersertifikat; belum-mulai (hard-delete) konfirmasi ringan.

## UX force-kick examRemoved
| Option | Selected |
|--------|----------|
| Redirect daftar + banner (reuse examClosed, no view baru) | ✓ |
| Halaman dedicated | |

**Choice:** Kunci UI → redirect daftar Assessment + banner "Anda telah dikeluarkan dari ujian ini." Reuse pola examClosed.

## Input alasan di modal hapus
| Option | Selected |
|--------|----------|
| Selalu tampil + hint (server enforce) | ✓ |
| Kondisional by status | |

**Choice:** Field alasan selalu tampil + hint "wajib bila peserta sudah mengerjakan"; server 411 D-02 penjaga akhir.

## Panel Peserta Dikeluarkan + Restore
| Option | Selected |
|--------|----------|
| Restore 1-klik langsung | ✓ |
| Konfirmasi dulu | |

**Choice:** Restore 1-klik (aman/reversibel), baris balik live via SignalR participantAdded. Panel collapsible: nama/waktu/oleh/alasan.

## Claude's Discretion
- Bentuk picker modal, styling panel, banner; tingkat modal via data-status; optimistic vs SignalR-echo (dedup by sessionId); cakupan e2e (lengkap di 413).

## Deferred Ideas
- xUnit+Playwright lengkap → 413; halaman dedicated ditolak; IN-02 EditAssessment exclude → evaluasi planner/413.
