# Phase 361: Bypass UI (B) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-11
**Phase:** 361-bypass-ui-b
**Areas discussed:** Bentuk wizard 3-langkah, Deep-link & perilaku tab, Form detail & UX closure mode, Strategi UAT e2e, Tabel worker & empty state/copy, Modal konfirmasi & error/race UI

---

## Bentuk wizard 3-langkah

| Option | Description | Selected |
|--------|-------------|----------|
| Modal multi-step | Satu modal Bootstrap 3 step, konsisten pola overrideModal existing | ✓ |
| Panel inline expand | Form expand di bawah row worker | |
| Page terpisah | Redirect ke page wizard sendiri | |

| Option | Description | Selected |
|--------|-------------|----------|
| Linear wajib | Lanjut disabled sampai valid, bisa Kembali, indicator 1-2-3 | ✓ |
| Bebas klik step | Step clickable, bisa lompat | |

| Option | Description | Selected |
|--------|-------------|----------|
| Ringkasan + warning | Recap pilihan + warning konsekuensi per mode + tombol "Jalankan Bypass" | ✓ |
| Langsung submit | Step 3 isi detail → submit langsung | |

| Option | Description | Selected |
|--------|-------------|----------|
| Toast + auto-refresh | Modal tutup → toast → refresh tabel+panel via fetch; CL-B(b) alert kuning reminder paket | ✓ |
| Full page reload | location.reload() | |

---

## Deep-link & perilaku tab

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-buka modal konfirmasi | Tab2 aktif → panel load → modal pending {id} langsung terbuka | ✓ |
| Tab2 + highlight row | Cuma highlight, HC klik sendiri | |

| Option | Description | Selected |
|--------|-------------|----------|
| Toast info + Tab2 normal | Pending stale → toast "sudah diproses / tidak ditemukan" | ✓ |
| Silent ignore | Tanpa pesan | |

| Option | Description | Selected |
|--------|-------------|----------|
| Lazy saat tab aktif | Fetch pertama kali Tab2 dibuka / deep-link | ✓ |
| Preload saat page load | Fetch semua saat Override dibuka | |

| Option | Description | Selected |
|--------|-------------|----------|
| Update query param | history.replaceState ?tab= saat switch | ✓ |
| Tanpa update URL | Bootstrap tabs murni | |

---

## Form detail & UX closure mode

| Option | Description | Selected |
|--------|-------------|----------|
| Default murni + info | Wizard tanpa field jadwal/durasi/KKM; info text arahkan Kelola Assessment | ✓ |
| Minimal field | Wizard minta jadwal+durasi+KKM | |

| Option | Description | Selected |
|--------|-------------|----------|
| Disabled + alasan | Card mode disabled + teks alasan dari BypassDetail | ✓ |
| Disembunyikan | Mode invalid hilang | |

| Option | Description | Selected |
|--------|-------------|----------|
| Dropdown cascading | Bagian → Unit dari OrganizationUnits | ✓ |
| Free text | Input bebas | |

| Option | Description | Selected |
|--------|-------------|----------|
| Badge + Batal saja | Row "Menunggu": badge kuning + [Batal]; [Lihat & Konfirmasi] hanya saat Siap | ✓ |
| Semua tombol disabled | Konfirmasi tampil tapi mati | |

---

## Strategi UAT e2e

| Option | Description | Selected |
|--------|-------------|----------|
| Live MCP + spec committed | UAT live @5277 + .spec.ts committed untuk regresi | ✓ |
| Live MCP saja | Tanpa spec committed | |
| Spec committed saja | Tanpa sesi live | |

| Option | Description | Selected |
|--------|-------------|----------|
| SQL fixture + restore | Pola 313-timer-fixtures.sql + SEED_WORKFLOW | ✓ |
| Seed via UI dalam test | Create data lewat UI | |

| Option | Description | Selected |
|--------|-------------|----------|
| E2e UI ringan | Re-grade via UI admin, assert badge balik "Menunggu" | ✓ |
| Skip e2e, cukup xUnit | Andalkan xUnit 360 | |

---

## Tabel worker & empty state/copy

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse pola Tab1 | Cascading Bagian→Unit→Track + "Muat Data" | ✓ |
| Auto-load semua | Load semua worker tanpa filter | |

| Option | Description | Selected |
|--------|-------------|----------|
| Search client-side | Input search nama, tanpa pagination | ✓ |
| Tanpa search | Tabel polos | |

| Option | Description | Selected |
|--------|-------------|----------|
| Sembunyikan (recommended) | Panel tidak dirender saat 0 pending (spec §9 "kalau ada") | |
| Tampil empty state | Panel selalu tampil + teks "Tidak ada bypass menunggu konfirmasi" | ✓ |

**Notes:** User memilih melawan rekomendasi — panel selalu kelihatan supaya HC aware antrian bypass.

| Option | Description | Selected |
|--------|-------------|----------|
| Deskriptif | Badge "Menunggu Exam" / "Siap Dikonfirmasi" (DB tetap Menunggu/Siap) | ✓ |
| Sama dengan DB | Badge persis nilai DB | |

---

## Modal konfirmasi & error/race UI

| Option | Description | Selected |
|--------|-------------|----------|
| Payload existing (recommended) | Pakai BypassPendingList apa adanya, zero perubahan backend | |
| Extend backend dikit | Tambah field select (skor, tanggal exam, Reason, coach target) — minor, tanpa migration | ✓ |

**Notes:** User memilih melawan rekomendasi — modal lebih informatif layak sentuhan minor kode 360.

| Option | Description | Selected |
|--------|-------------|----------|
| Confirm ringan | Dialog "Batalkan rencana bypass? Exam belum dikerjakan akan dihapus." | ✓ |
| Langsung eksekusi | Tanpa konfirmasi | |

| Option | Description | Selected |
|--------|-------------|----------|
| Toast pesan + refresh | Pesan backend di toast merah + auto-refresh panel, modal tutup | ✓ |
| Alert di modal | Pesan inline, modal tetap buka | |

| Option | Description | Selected |
|--------|-------------|----------|
| Disable + spinner | Semua tombol POST disable+spinner saat in-flight | ✓ |
| Andalkan backend saja | D-12 atomik cukup | |

---

## Claude's Discretion

- Struktur file JS Tab2 (inline vs file terpisah)
- Markup step indicator, styling card closure mode, ikon badge
- Pemecahan partial view vs satu file (asal Tab1 tak berubah perilaku)
- Detail assert + pembagian file spec e2e
- Sumber data dropdown coach wizard (risiko — planner tentukan)

## Deferred Ideas

- Audit/improve Tab1 Override Deliverable (backlog, spec §13)
- Undo bypass executed (spec §8.2 Opsi C — tidak dibangun)
- Polling/auto-refresh berkala panel pending
