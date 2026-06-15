# Phase 364: Restore Baseline Regresi e2e Exam - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-11
**Phase:** 364-restore-baseline-regresi-e2e-exam
**Areas discussed:** Pola judul baru, Definisi selesai (SC#4), Guard auto-pair + exempt, Eksekusi & cleanup, Kebijakan skip/fixme, Status baseline ke depan, Asersi sensitif judul panjang

---

## Pola judul baru

| Option | Description | Selected |
|--------|-------------|----------|
| Seragam 'Pre Test' | Semua 9 flow prefix 'Pre Test' — zero risiko auto-pair counterpart | ✓ |
| Semantik per flow | Pre/Post sesuai makna flow — buka peluang pasangan tak disengaja | |

| Option | Description | Selected |
|--------|-------------|----------|
| Setelah prefix | 'Pre Test [317-K] MA Exam {ts}' — marker grep-able, diff minimal | ✓ |
| Di akhir judul | 'Pre Test MA Exam [317-K] {ts}' | |
| Hapus marker | Judul bersih, kehilangan traceability | |

| Option | Description | Selected |
|--------|-------------|----------|
| Edit per-call di spec | 9 argumen uniqueTitle() di 2 file; helper utils.ts tak disentuh | ✓ |
| Helper baru compliantTitle() | DRY tapi nambah API untuk 2 file saja | |

| Option | Description | Selected |
|--------|-------------|----------|
| Lolos regex saja | Nama lama jadi suffix, asersi minimal berubah | ✓ |
| Konvensi penuh Track+Lokasi | Lebih mirip data real, tanpa validasi server yang menuntut | |

**User's choice:** Semua opsi recommended.

---

## Definisi selesai (SC#4)

| Option | Description | Selected |
|--------|-------------|----------|
| Fix test-code sampai PASS | Judul + selector/wait/flow diperbaiki sampai hijau; zero kode produksi | ✓ |
| Judul saja + dokumentasi | Scope ketat, baseline mungkin tetap merah | |

| Option | Description | Selected |
|--------|-------------|----------|
| Catat, jangan fix (bug produksi) | Temuan → backlog/fase lain; spec dapat fixme + alasan | ✓ |
| Fix kalau kecil | Melanggar batas test-only roadmap | |

| Option | Description | Selected |
|--------|-------------|----------|
| SUMMARY + backlog | Deviasi di SUMMARY + backlog 999.x kalau actionable (pola 355) | ✓ |
| File FINDINGS terpisah | Overkill untuk fase kecil | |

| Option | Description | Selected |
|--------|-------------|----------|
| 1x full run hijau | PASS sekali full run live @5277, retry default | ✓ |
| 2x run stabil | Bukti non-flaky, 2x waktu | |

**User's choice:** Semua opsi recommended.

---

## Guard auto-pair + exempt

| Option | Description | Selected |
|--------|-------------|----------|
| Run baseline dulu | Run as-is @5277, catat failure per-flow sebelum edit (SC#2) | ✓ |
| Langsung edit | Lebih cepat, tanpa bukti baseline | |

| Option | Description | Selected |
|--------|-------------|----------|
| Struktural + 1 asersi DB | Prefix seragam + timestamp + asersi LinkedGroupId NULL di 1 flow | ✓ |
| Struktural saja | By-design tanpa bukti runtime | |

| Option | Description | Selected |
|--------|-------------|----------|
| Ikut full run saja (FLOW P) | PASS = bukti exempt; patah non-judul → ikuti SC#4 | ✓ |
| Verifikasi exempt eksplisit | Asersi khusus untuk code path yang sudah jelas | |

**User's choice:** Semua opsi recommended.

---

## Eksekusi & cleanup

| Option | Description | Selected |
|--------|-------------|----------|
| Keep as-is (pola 355) | AD off env override + SEED snapshot/restore + residue check + journal | ✓ |
| Revisit | — | |

| Option | Description | Selected |
|--------|-------------|----------|
| Snapshot/restore saja | Restore DB = semua data test hilang; zero teardown baru | ✓ |
| Tambah teardown delete | Redundant dengan restore | |

| Option | Description | Selected |
|--------|-------------|----------|
| 2 spec + dotnet test | Gate = 2 spec PASS @5277 + suite xUnit hijau | ✓ |
| Full e2e suite | Lebih lama, spec lain di luar scope | |

**User's choice:** Semua opsi recommended.

---

## Kebijakan skip/fixme (round 2)

| Option | Description | Selected |
|--------|-------------|----------|
| fixme + gate turun | test.fixme('alasan + ref backlog'); run dengan fixme ≠ PASS penuh | ✓ |
| skip + tetap PASS penuh | Menyembunyikan hutang di angka gate | |

---

## Status baseline ke depan (round 2)

| Option | Description | Selected |
|--------|-------------|----------|
| On-demand | Dipakai saat fase relevan sentuh area exam; bukan gate wajib | ✓ |
| Gate wajib tiap UAT | Coverage kuat tapi mahal + mengikat fase lain | |

---

## Asersi sensitif judul panjang (round 2)

| Option | Description | Selected |
|--------|-------------|----------|
| Partial-match boleh | Substring unik (marker+ts) kalau UI truncate | ✓ |
| Exact-match dipertahankan | Effort lebih besar untuk nilai kecil | |

---

## Claude's Discretion

- Urutan run spec, workers/serial Playwright, timeout tuning
- Pencatatan hasil run baseline diagnosa (di SUMMARY plan)
- Pemilihan flow untuk asersi DB LinkedGroupId
- Detail teknis fix selector drift per-flow

## Deferred Ideas

- Bug produksi yang ketemu saat restore → backlog 999.x / fase lain
- 2 spec jadi gate wajib tiap UAT → ditolak sekarang (on-demand), bisa dipertimbangkan ulang kalau area exam sering regresi

## Catatan sesi

- Sesi sempat terputus setelah area 1 (Pola judul baru) — di-resume dari `364-DISCUSS-CHECKPOINT.json`, zero re-ask.
