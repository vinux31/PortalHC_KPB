# Phase 421: Retake Lifecycle Hardening - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-23
**Phase:** 421-retake-lifecycle-hardening
**Areas discussed:** RTH-01 (dead-end), RTH-02/03 (mekanis), RTH-04 (guard hapus peserta), RTH-05 (warning MaxAttempts)

---

## RTH-01 — Gate window (dead-end destruktif)

| Option | Description | Selected |
|--------|-------------|----------|
| Eligibility + eksekusi | CanRetake terima examWindowCloseDate → tombol auto-hidden + guard ExecuteAsync defense-in-depth | ✓ |
| Eksekusi saja | Blok hanya di ExecuteAsync; tombol tetap tampil, klik → pesan gagal | |

**User's choice:** Eligibility + eksekusi (gate dua lapis).
**Notes:** Pekerja tak pernah masuk jalur destruktif; reuse konvensi +7h WIB StartExam.

## RTH-01 — Peringatan dini HC (UpdateRetakeSettings)

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, warning non-blocking | Toast saat cooldown > sisa window sampai EWCD | ✓ |
| Tidak perlu | Cukup gate sisi pekerja | |

**User's choice:** Ya, warning non-blocking.

## RTH-02 — Konfirmasi cabut sertifikat saat HC reset sesi LULUS

| Option | Description | Selected |
|--------|-------------|----------|
| Konfirmasi cabut cert | Peringatkan + minta konfirmasi sebelum reset sesi lulus | ✓ |
| Cabut langsung | Nol-kan NomorSertifikat tanpa konfirmasi | |

**User's choice:** Konfirmasi cabut cert (konsisten confirm-before).
**Notes:** Nol-kan NomorSertifikat tetap dieksekusi (SetProperty null); konfirmasi = lapis UX.

## RTH-03 — Counting cap vs warning

**User's choice:** Diskresi Claude — ekstrak helper snapshot-presence (`CountEraRetakeArchives`), wire 4 situs. Tak ada nuansa khusus.

## RTH-04 — Guard hapus peserta Abandoned/ber-riwayat

| Option | Description | Selected |
|--------|-------------|----------|
| Tolak (blok + pesan) | Konsisten existing InProgress/Completed | |
| Izinkan + bersihkan arsip | Hapus + cascade archives | |
| Soft-confirm + bersihkan | Warn → konfirmasi → hapus + cleanup | ✓ |

**User's choice:** Soft-confirm + bersihkan arsip.

## RTH-04 — Mekanisme soft-confirm

| Option | Description | Selected |
|--------|-------------|----------|
| Server round-trip + flag | POST batalkan → warning → submit ulang dgn flag konfirmasi | ✓ |
| JS confirm() klien | data-attr + confirm() klien, back-guard server | |
| Diskresi Claude | Bebas asal intent terpenuhi | |

**User's choice:** Server round-trip + flag (server-authoritative).

## RTH-05 — Warning MaxAttempts diturunkan di bawah pemakaian

| Option | Description | Selected |
|--------|-------------|----------|
| Toast pasca-simpan | TempData non-blocking setelah simpan | |
| Konfirmasi pra-simpan | Modal konfirmasi sebelum simpan | ✓ |
| Inline ModelState | Pesan inline di form | |

**User's choice:** Konfirmasi pra-simpan (tetap non-blocking).

## Claude's Discretion

- RTH-03 nama/posisi helper `CountEraRetakeArchives`.
- Teks pesan/konfirmasi (Bahasa Indonesia), posisi guard, bentuk modal.
- Konvensi +7h WIB untuk gate window (reuse StartExam).

## Deferred Ideas

- Strategi grading retake alternatif, cooldown escalating, rotasi token per-attempt, cap per-tahun (YAGNI).
- v32.5 FlexibleParticipantRemove (branch main) — soft-confirm dikerjakan di EditAssessment POST existing.
