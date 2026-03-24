# Phase 248: UI & Annotations - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-24
**Phase:** 248-ui-annotations
**Areas discussed:** Lokasi CSS global

---

## Lokasi CSS `.bg-purple`

| Option | Description | Selected |
|--------|-------------|----------|
| Buat `wwwroot/css/site.css` | File CSS baru + link di `_Layout.cshtml` — standar, reusable, sesuai pattern existing | ✓ |
| Inline `<style>` di `_Layout.cshtml` | Tidak ada file baru, tapi campur CSS di HTML layout | |
| `<style>` di `AssessmentMonitoring.cshtml` | Scoped tapi perlu duplikasi karena `bg-purple` dipakai di 2+ view | |

**User's choice:** Opsi 1 — Buat `wwwroot/css/site.css`
**Notes:** User meminta analisa pro/con terlebih dahulu. Opsi 1 dipilih karena: (1) pattern file CSS terpisah sudah ada di project, (2) requirements bilang "CSS global", (3) `.bg-purple` dipakai di minimal 2 view berbeda.

---

## Claude's Discretion

- Pilihan warna hex untuk `.bg-purple`
- Nilai `MaxLength` per field di TrainingRecord

## Deferred Ideas

None
