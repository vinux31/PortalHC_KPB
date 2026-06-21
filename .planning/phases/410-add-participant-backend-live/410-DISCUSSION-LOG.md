# Phase 410: Add-Participant Backend Live - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-21
**Phase:** 410-add-participant-backend-live
**Areas discussed:** Re-add peserta dikeluarkan, Cakupan picker eligible, Feedback duplikat di-skip, Broadcast participantAdded

---

## Re-add peserta dikeluarkan

| Option | Description | Selected |
|--------|-------------|----------|
| Exclude → paksa Restore | Picker exclude user dengan sesi apapun di batch; user removed balik via Restore (411) | ✓ |
| Izinkan Add sesi baru | Picker tampilkan user removed; Add bikin sesi fresh di samping yang removed | |
| Add = auto-restore | Add un-remove sesi removed alih-alih bikin baru | |

**User's choice:** Exclude → paksa Restore
**Notes:** Hindari dobel sesi, history utuh, 410 tetap murni "add user baru". Re-add lewat panel Restore 412→411.

---

## Cakupan picker eligible

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse query assign-awal | Pakai query eligible unit/section scope assign pertama (rekomendasi) | |
| Semua pekerja eligible | Semua pekerja belum-punya-sesi, tanpa batas unit/section | ✓ |
| Assign-scope + multi-unit eksplisit | Scope assign + logika multi-unit UserUnits eksplisit | |

**User's choice:** Semua pekerja eligible
**Notes:** ⚠️ Override sadar spec §B4 (yang sebut unit/section scope). User pilih lebih luas — admin bebas tambah pekerja mana saja. Planner JANGAN batasi unit/section.

---

## Feedback duplikat di-skip

| Option | Description | Selected |
|--------|-------------|----------|
| Count + nama di-skip | Return added[] + skipped[] (nama/nip) untuk toast UI 412 (rekomendasi) | ✓ |
| Count saja | Cuma addedCount + skippedCount | |

**User's choice:** Count + nama di-skip
**Notes:** UI 412 bisa toast "X ditambah, Y dilewati (sudah terdaftar)".

---

## Broadcast participantAdded

| Option | Description | Selected |
|--------|-------------|----------|
| Defer wiring ke 412 | 410 return JSON saja; SignalR participantAdded + handler di 412 (rekomendasi) | ✓ |
| Emit seam di 410 | Endpoint 410 broadcast participantAdded setelah commit (no-op visual sampai 412) | |

**User's choice:** Defer wiring ke 412
**Notes:** Batas fase bersih; 410 fokus backend + return JSON baris baru.

## Claude's Discretion
- Param endpoint (sessionId representatif vs batchKey), sumber daftar pekerja eligible, ekstraksi helper bersama, cakupan integration test.

## Deferred Ideas
- SignalR wiring → 412; Remove/Restore → 411; UI panel → 412; Playwright/suite → 413; filter eligible by unit/section sengaja tidak dilakukan (D-02).
