# Phase 374: UI ManagePackages + Lock + Pre/Post - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-13
**Phase:** 374-ui-managepackages-lock-pre-post
**Areas discussed:** Interaksi simpan, Penempatan & layout, Warning + reminder, Affordance lock

---

## Gray Area Selection

User memilih SEMUA 4 area untuk dibahas (multiSelect). Boundary + carry-forward locked dari spec dijelaskan di pertanyaan; area yang sudah locked tidak ditanya ulang.

---

## Interaksi Simpan

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-save AJAX saat flip | POST langsung tiap flip, toast. Pola Reshuffle Json. (Rekomendasi awal) | |
| Tombol Simpan eksplisit | Ubah toggle → klik "Simpan Pengaturan" sekali | ✓ |

**User's choice:** Tombol Simpan eksplisit
**Notes:** Perubahan kena semua sibling (propagate) → tombol eksplisit hindari salah-klik massal. Implikasi: form POST + Redirect (PRG) + TempData, bukan AJAX Json (D-01/D-01a).

---

## Penempatan & Layout

| Option | Description | Selected |
|--------|-------------|----------|
| Card Pengacakan khusus | Panel sendiri di bawah header, sebelum panel ringkasan paket (Rekomendasi) | ✓ |
| Inline toolbar header | Sisip di toolbar header L8-17 | |
| Gabung ke panel ringkasan paket | Taruh di panel existing L83-114 | |

**User's choice:** Card Pengacakan khusus
**Notes:** Host terkumpul 2 toggle + tombol Simpan + status lock + warning + reminder (D-02).

---

## Warning + Reminder

| Option | Description | Selected |
|--------|-------------|----------|
| Alert di card + live JS | Warning §9 & reminder = alert dalam card; warning live-recompute saat flip + on load (Rekomendasi) | ✓ |
| Gabung warning ke panel paket + saved-state | Warning nempel panel L83-114; no live JS | |

**User's choice:** Alert di card + live JS
**Notes:** Warning §9 live JS baca state checkbox saat ini (D-03a). Reminder Pre/Post saved-state via LinkedSessionId, hanya halaman Post (D-03b).

---

## Affordance Lock

| Option | Description | Selected |
|--------|-------------|----------|
| Disabled + alert banner | Switch disabled + alert jelaskan alasan. Pola lock SamePackage L29-44 (Rekomendasi) | ✓ |
| Disabled + help-text inline | Teks text-muted kecil di bawah toggle | |
| Disabled + tooltip hover | Tooltip Bootstrap saat hover | |

**User's choice:** Disabled + alert banner
**Notes:** Defense-in-depth — UI disabled + server guard reject di endpoint (D-04/D-04a). SHUF-11 = guard server-side.

## Claude's Discretion

- Bentuk return exact endpoint (PRG vs partial) — lean PRG.
- Markup/ID exact card + toggle + alert (reuse pola Bootstrap + copy 372-UI-SPEC).
- Jalankan `/gsd-ui-phase 374` formal — opsional (372-UI-SPEC + spec §7/§9 cukup).

## Deferred Ideas

- xUnit lock/propagate + Playwright UAT → Phase 375.
- Pindah SamePackage → out of scope (spec §12).
- Auto-cascade Pre→Post → ditolak by design (reminder opsi Z).
