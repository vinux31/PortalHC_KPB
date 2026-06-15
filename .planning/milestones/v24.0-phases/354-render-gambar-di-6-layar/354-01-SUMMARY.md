---
phase: 354-render-gambar-di-6-layar
plan: 01
subsystem: view-partials
tags: [razor, partial, lightbox, image-render, anti-drift]
requires: []
provides:
  - _QuestionImage partial (reusable img render + lightbox trigger)
  - _ImageLightboxModal partial (global 1-instance lightbox + JS swap src)
affects:
  - Views/Shared/
tech-stack:
  added: []
  patterns: [razor-partial-dynamic-model, bootstrap-modal-lightbox, render-if-not-null]
key-files:
  created:
    - Views/Shared/_QuestionImage.cshtml
    - Views/Shared/_ImageLightboxModal.cshtml
  modified: []
key-decisions:
  - "Partial _QuestionImage pakai @model dynamic + param anonymous (ImagePath/ImageAlt/Cap/AriaContext) agar reusable lintas semua VM tanpa ikat tipe konkret (anti-reuse)"
  - "isOption = cap <= 120 membedakan otomatis gambar opsi (block-bawah d-block w-100 mt-2) vs soal (cap 240 mb-3)"
  - "src/alt encode Razor @ (BUKAN Html.Raw), render-if-not-null — L-02 no XSS surface baru"
  - "Lightbox JS inline di partial (bukan @section Scripts) + guard dataset.bound — self-contained, aman di view AJAX-injected (host RND-04)"
requirements-completed: [RND-07]
duration: 8 min
completed: 2026-06-09
---

# Phase 354 Plan 01: Fondasi Render Gambar (2 Partial Reusable) Summary

Membuat fondasi anti-drift render gambar: partial `_QuestionImage.cshtml` (markup `<img>` img-fluid+lazy+alt + trigger lightbox) dan `_ImageLightboxModal.cshtml` (modal global Bootstrap modal-xl + JS swap `src` on `show.bs.modal`). Single source of truth markup gambar untuk 6 surface — menutup RND-07 (responsif inheren) secara otomatis bagi semua pemanggil.

**Tasks:** 3 (2 file baru + 1 build gate) | **Files:** 2 created | **Duration:** ~8 min

## What was built

- **`Views/Shared/_QuestionImage.cshtml`** — `@model dynamic`, terima `ImagePath/ImageAlt/Cap/AriaContext` via param anonymous. Render `<img class="img-fluid rounded border ... question-image-zoom">` dengan `loading="lazy"`, `role="button" tabindex="0"`, `data-bs-toggle/target="#imageLightboxModal"`, `data-img-src/alt`, `aria-label` Bahasa Indonesia. `cap<=120` → tambah `d-block w-100 mt-2 mb-0` (opsi block-bawah D-03). Render NOTHING bila `ImagePath` null/whitespace.
- **`Views/Shared/_ImageLightboxModal.cshtml`** — modal `id=imageLightboxModal` (modal-xl modal-dialog-centered), title "Pratinjau Gambar", `btn-close aria-label="Tutup"`, body `<img id="imageLightboxImg" class="img-fluid">`. JS inline swap `src`/`alt` dari `e.relatedTarget`, guard `dataset.bound` (bind sekali).

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- Acceptance criteria 9/9 (_QuestionImage) + 9/9 (_ImageLightboxModal) PASS via grep.
- `Html.Raw` count = 0 (XSS guard, L-02).
- `dotnet build` → **Build succeeded. 0 Error(s)**.

## Issues Encountered

None.

## Next Phase Readiness

Partial siap dipanggil. Belum ada view yang memanggilnya (render end-to-end diverifikasi visual di Plan 05/06). Ready for 354-02 (field VM, paralel-safe Wave 1).

## Self-Check: PASSED
