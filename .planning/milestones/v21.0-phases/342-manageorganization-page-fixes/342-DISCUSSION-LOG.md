# Phase 342: ManageOrganization Page Fixes - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-03
**Phase:** 342-manageorganization-page-fixes
**Areas discussed:** Sumber label tier, UI konfirmasi cascade, Visual badge tier, Trigger PreviewEditCascade

---

## Sumber label tier (legend/badge/title)

| Option | Description | Selected |
|--------|-------------|----------|
| JS fetch GetLevelLabels | Client-render via fetch GET /Admin/GetLevelLabels (Phase 340). Page sudah AJAX-driven, label live, Phase 343 urus @inject. | ✓ |
| @inject IOrgLabelService | Server-render @inject. Konsisten view Phase 341 tapi overlap scope Phase 343. | |

**User's choice:** JS fetch GetLevelLabels (D-01)
**Notes:** Page sudah AJAX (orgTree.js fetch tree) → label endpoint sama natural. Server @inject = scope Phase 343 app-wide.

---

## UI konfirmasi cascade impact (ORG-TREE-07)

| Option | Description | Selected |
|--------|-------------|----------|
| Bootstrap modal | 4-line count breakdown + Batal/Lanjut Simpan, per spec mockup §4.6 | ✓ |
| Native confirm() multi-line | confirm() dgn \n, konvensi Phase 341 D-03, simpler | |

**User's choice:** Bootstrap modal (D-02)
**Notes:** Multi-count warning lebih readable di modal; native confirm reserved untuk single-line delete Phase 341.

---

## Visual badge tier per row

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse palette warna level | Badge bg = warna icon level (CSS level-0..5), link visual badge↔icon | ✓ |
| Neutral Bootstrap badge | bg-secondary uniform, less noise | |

**User's choice:** Reuse palette warna level (D-03)
**Notes:** Perkuat sistem warna tier (legend + icon + badge koheren).

---

## Trigger PreviewEditCascade

| Option | Description | Selected |
|--------|-------------|----------|
| Selalu call, server early-return | JS selalu panggil saat Simpan; endpoint early-return kalau no change; modal kalau impact>0 | ✓ |
| Client pre-check dulu | Bandingkan form vs original di JS, skip call kalau no change | |

**User's choice:** Selalu call, server early-return (D-04)
**Notes:** Server authoritative, count akurat, reuse logic EditOrganizationUnit; no client duplicate logic.

## Claude's Discretion

- Path breadcrumb markup/render string (ORG-TREE-06)
- Cascade modal id/aria conventions (replicate Phase 341 pattern)
- Badge markup exact class
- Label JS cache strategy (1 fetch on load vs re-fetch)

## Deferred Ideas

- App-wide label integration 7 area page → Phase 343 (ORG-INTEG-01/02)
- Formal xUnit + Playwright + manual UAT → Phase 344 (TEST-01..06)
- Regression smoke drag/toggle/delete/add → Phase 344 (ORG-INTEG-03)
