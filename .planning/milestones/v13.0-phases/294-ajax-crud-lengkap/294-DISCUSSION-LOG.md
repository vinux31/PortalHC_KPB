# Phase 294: AJAX CRUD Lengkap - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-03
**Phase:** 294-ajax-crud-lengkap
**Areas discussed:** Modal Add/Edit, Action dropdown per node, Toggle & Delete behavior, Tree refresh strategy

---

## Modal Add/Edit

| Option | Description | Selected |
|--------|-------------|----------|
| Satu modal shared | Judul berubah dinamis, form field sama, reuse HTML | ✓ |
| Dua modal terpisah | Masing-masing punya HTML sendiri, lebih eksplisit | |

**User's choice:** Satu modal shared
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Dari data tree yang sudah ada | Pakai flat array yang sudah di-fetch, exclude node+descendants saat Edit | ✓ |
| Fetch ulang dari server | Hit endpoint baru GetPotentialParents | |

**User's choice:** Dari data tree yang sudah ada
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Node yang diklik | Add di node X → parent otomatis X, bisa diubah | ✓ |
| Kosong (root) | Selalu mulai dari root | |

**User's choice:** Node yang diklik
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Hapus collapse panel, modal saja | Tombol Tambah di header langsung buka modal | ✓ |
| Tetap ada keduanya | Collapse panel untuk quick add root, modal untuk child | |

**User's choice:** Hapus, modal saja
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Diganti modal | Hapus seluruh Edit card Razor, edit hanya lewat modal AJAX | ✓ |
| Tetap ada | Keep Razor Edit card sebagai fallback | |

**User's choice:** Diganti modal
**Notes:** —

---

## Action Dropdown per Node

| Option | Description | Selected |
|--------|-------------|----------|
| Icon titik tiga, klik buka | Icon ⋮ kebab di kanan node, klik buka dropdown. Bootstrap standard. | ✓ |
| Muncul saat hover row | Action buttons on-hover, less discoverable | |
| Selalu visible | Tombol Edit/Toggle/Hapus selalu terlihat, crowded | |

**User's choice:** Icon titik tiga, klik buka
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| 4 item: Add Child, Edit, Toggle, Hapus | Add Child = tambah unit di bawah node | ✓ |
| 3 item: Edit, Toggle, Hapus | Tanpa Add Child | |

**User's choice:** 4 item
**Notes:** —

---

## Toggle & Delete Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Langsung + toast | Klik Toggle → AJAX → badge berubah + toast | ✓ |
| Dialog konfirmasi dulu | Modal "Yakin nonaktifkan X?" sebelum toggle | |

**User's choice:** Langsung + toast
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse modal existing, ganti jadi AJAX | Modal #deleteModal sudah ada, ganti form submit → ajaxPost() | ✓ |
| Buat modal baru dari scratch | Modal baru khusus AJAX | |

**User's choice:** Reuse modal existing
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Backend yang handle | Frontend cukup tampilkan message dari server response | |
| Warning di frontend | JS cek children dan tampilkan warning khusus sebelum toggle/delete parent | ✓ |

**User's choice:** Warning di frontend
**Notes:** JS cek apakah node punya children sebelum toggle/delete, tampilkan warning

---

## Tree Refresh Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Full re-render initTree() | Fetch ulang → rebuild tree. Simpel, data konsisten. | ✓ |
| Partial DOM update | Update hanya node yang berubah. Smooth tapi complex. | |

**User's choice:** Full re-render
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, simpan dan restore | Simpan expanded node IDs sebelum re-render, restore setelah | ✓ |
| Tidak, default state | Kembali ke default level 0+1 expanded | |

**User's choice:** Ya, simpan dan restore
**Notes:** —

---

## Claude's Discretion

Areas where Claude has flexibility:
- Animasi/transition modal
- Dropdown positioning
- Loading spinner di modal saat submit
- Validation feedback style
