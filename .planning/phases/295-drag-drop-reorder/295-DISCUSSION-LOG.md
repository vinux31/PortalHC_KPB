# Phase 295: Drag-drop Reorder - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-03
**Phase:** 295-drag-drop-reorder
**Areas discussed:** Backend endpoint strategy, Drag handle & visual feedback, Cross-parent blocking

---

## Backend Endpoint Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Endpoint baru batch | ReorderBatch terima {parentId, orderedIds}. Satu POST set semua DisplayOrder. | ✓ |
| Pakai existing, loop | Client panggil ReorderOrganizationUnit beberapa kali. Race condition risk. | |
| Ganti existing jadi batch | Modifikasi existing. Breaking change. | |

**User's choice:** Endpoint baru batch
**Notes:** Existing ReorderOrganizationUnit tetap ada untuk backward compat

---

## Drag Handle & Visual Feedback

| Option | Description | Selected |
|--------|-------------|----------|
| Icon grip kiri, on-hover | Grip dots muncul saat hover row. Cursor grab. Tidak clutter saat idle. | ✓ |
| Selalu visible di kiri | Grip icon selalu terlihat. Visual noise. | |
| Seluruh row draggable | Tanpa handle. Conflict dengan expand/collapse. | |

**User's choice:** Icon grip kiri, muncul on-hover
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Ghost + placeholder line | Semi-transparent ghost + garis biru di posisi drop. SortableJS default. | ✓ |
| Ghost saja | Tanpa indikator posisi drop. | |
| You decide | Claude pilih. | |

**User's choice:** Ghost + placeholder line
**Notes:** —

---

## Cross-parent Blocking

| Option | Description | Selected |
|--------|-------------|----------|
| SortableJS group:false + snap back | Per-parent instance, group:false. Drag ke luar → snap back. | ✓ |
| Visual "no drop" indicator | Icon/warna merah saat drag melewati batas parent. | |

**User's choice:** SortableJS group:false + snap back
**Notes:** Tidak perlu visual indicator khusus

---

## Claude's Discretion

Areas where Claude has flexibility:
- SortableJS config details (animation, ghostClass, chosenClass)
- Grip icon styling & hover transition
- Loading state saat reorder in-flight
- Error handling (toast + revert)
