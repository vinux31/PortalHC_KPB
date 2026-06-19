# Phase 397: Link Pre/Post ke room existing - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-18
**Phase:** 397-link-pre-post-ke-room-existing
**Areas discussed:** Semantik group-link, Pairing per-pekerja & LinkedSessionId, UX picker & link wajib/opsional, Validasi & preview link, + 4 gray-area tambahan (audit online, inject↔inject, koherensi tanggal, unlink)

---

## Semantik group-link (LinkedGroupId) — D-01

Penjelasan disederhanakan ("stiker grup") setelah user minta klarifikasi.

| Option | Description | Selected |
|--------|-------------|----------|
| Boleh tempel stiker ke online (+audit) | Kasus A adopt (tak sentuh online); Kasus B tulis LinkedGroupId baru ke sesi online existing + inject, atomic + AuditLog, hanya nomor grup (skor/jawaban tak diubah) | ✓ |
| Jangan sentuh online — blok Kasus B | Picker hanya room sudah-ber-grup; standalone ditolak | |
| Stiker di sesi inject saja | Tak sentuh online; Kasus B pasangan tak tampil (terputus) | |

**User's choice:** Boleh tempel stiker ke online (+audit)
**Notes:** User awalnya tanya "maksutnya apa ya ini, jelaskan dulu dengan sederhana" → diberi penjelasan analogi stiker grup + Kasus A/B → memilih write-to-online. Display memasangkan by LinkedGroupId; tanpa write, Kasus B tak tampil berpasangan.

---

## Pairing per-pekerja & LinkedSessionId — D-02

| Option | Description | Selected |
|--------|-------------|----------|
| Bidirectional per-pekerja | Inject↔existing sibling, dua arah (mirror CreateAssessment); ubah service resolve by-UserId + write-back existing | ✓ |
| One-way (inject→existing) | Set di sesi inject saja, tak mutate online | |
| Skip (andalkan group+UserId) | LinkedSessionId null; display/gain-score tetap jalan | |

**User's choice:** Bidirectional per-pekerja
**Notes:** "Seakan online" penuh. Catatan: display/gain-score sebenarnya tak butuh LinkedSessionId (pair by LinkedGroupId+UserId), tapi user pilih fidelitas penuh.

---

## Roster mismatch (pekerja inject tanpa pasangan) — D-03

| Option | Description | Selected |
|--------|-------------|----------|
| Izinkan unpaired + warn | Set LinkedGroupId, LinkedSessionId null → sisi tunggal; tandai di preview | ✓ |
| Blok jika ada yang tak berpasangan | Tolak batch bila ≥1 pekerja tak punya counterpart | |
| Hanya pair yang cocok, sisanya standalone | Pekerja tak cocok di-inject tanpa LinkedGroupId | |

**User's choice:** Izinkan unpaired + warn
**Notes:** Pola warn-but-allow (carry 395/396).

---

## Link wajib/opsional saat Pre/Post — D-04

| Option | Description | Selected |
|--------|-------------|----------|
| Opsional (boleh standalone) | Picker boleh di-skip; inject Pre/Post standalone, tautkan nanti | ✓ |
| Wajib pilih room target | Harus pilih room existing untuk commit | |

**User's choice:** Opsional (boleh standalone)
**Notes:** Mendukung "inject kedua sisi" & "link belakangan".

---

## Penempatan picker — D-05

| Option | Description | Selected |
|--------|-------------|----------|
| Panel inline di Step-1 | Panel pencarian inline di bawah pilihan tipe | |
| Modal pop-up | Tombol "Cari Room" → modal → pilih → chip di Step-1 | ✓ |

**User's choice:** Modal pop-up

---

## Filter picker — D-06

| Option | Description | Selected |
|--------|-------------|----------|
| Tipe LAWAN saja + search | Inject Pre → room PostTest; inject Post → room PreTest; search judul/kategori/jadwal; baris tampil metadata + indikator grup | ✓ |
| Semua room, HC yang pilih | Tampilkan semua, rawan salah-pasang | |

**User's choice:** Tipe LAWAN saja + search

---

## Preview pairing — D-07

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, tampilkan ringkasan pairing | Selain skor: ter-pair vs unpaired + apakah menyentuh data online (Kasus B) | ✓ |
| Tidak, skor saja | Pairing diam-diam saat commit | |

**User's choice:** Ya, tampilkan ringkasan pairing

---

## Anti-dobel-link per-pekerja — D-08

| Option | Description | Selected |
|--------|-------------|----------|
| Blok/peringatkan (anti dobel) | Pekerja sudah punya sibling tipe-sama di grup → tolak/warn; masuk daftar error/warn | ✓ |
| Izinkan (multi-attempt) | Biarkan; berisiko pairing/gain-score ambigu | |

**User's choice:** Blok/peringatkan (anti dobel)

---

## (Tambahan) Audit perubahan data online — D-09

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, entri audit terpisah | AuditLog "LinkPrePost" tiap sesi online diubah (actor, sessionId online, LinkedGroupId, room target) | ✓ |
| Cukup ikut ManualInject | Tak ada audit khusus perubahan online | |

**User's choice:** Ya, entri audit terpisah

---

## (Tambahan) Target room inject↔inject — D-10

| Option | Description | Selected |
|--------|-------------|----------|
| Boleh (inject & online) | Picker tampilkan room inject maupun online; inject↔inject tak sentuh data online | ✓ |
| Hanya online asli | Picker hanya room online | |

**User's choice:** Boleh (inject & online)

---

## (Tambahan) Koherensi tanggal Pre vs Post — D-11

| Option | Description | Selected |
|--------|-------------|----------|
| Warn di preview (allow) | Peringatan bila Pre.CompletedAt > Post target; jangan blok | ✓ |
| Blok (tolak) | Tolak commit bila urutan janggal | |
| Abaikan (tak cek) | Tak ada pengecekan tanggal | |

**User's choice:** Warn di preview (allow)

---

## (Tambahan) Unlink/ubah tautan pasca-commit — D-12

| Option | Description | Selected |
|--------|-------------|----------|
| Defer (out-of-scope) | Tak masuk 397; pakai Input Records existing | |
| Sertakan di 397 | Bangun fitur unlink di phase ini | ✓ |

**User's choice:** Sertakan di 397
**Notes:** SCOPE ADDITION — unlink masuk scope 397 (bukan defer). Jaga minimal (batal tautan + rollback aman), bukan editor link umum.

## Claude's Discretion

- Nilai LinkedGroupId baru (Kasus B) = RepresentativeId target; tulis ke semua sesi room target.
- Endpoint picker (reuse ManageAssessmentTab_Assessment vs baru), komponen modal/chip, perubahan signature service per-pekerja, ActionType audit naming, scope transaksi atomic.

## Deferred Ideas

- Multi-paket per room (§12), import gambar Excel (§12), auto-detect-by-judul sebagai satu-satunya jalur, editor link umum/bulk re-link, link untuk tipe Standard.
- Reviewed todo not folded: cleanup-data-test-367 (false-positive).
