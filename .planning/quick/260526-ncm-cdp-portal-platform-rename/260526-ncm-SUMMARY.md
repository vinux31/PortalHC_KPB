---
quick_id: 260526-ncm
slug: cdp-portal-platform-rename
date: 2026-05-26
status: complete
---

# Summary: Rename CDP "Portal" → "Platform"

## Changes
- `Views/CDP/Index.cshtml:2` — ViewData Title
- `Views/CDP/Index.cshtml:9` — h2 heading
- `Views/Home/Index.cshtml:54` — aria-label CDP shortcut card
- `Views/Home/Index.cshtml:59` — card label text

## Verification
- `grep "Competency Development Portal" Views/` → 0 hits
- `grep "Competency Development Platform" Views/` → 4 hits (CDP/Index ×2 + Home/Index ×2)
- `dotnet build` → 0 errors, 23 warnings (pre-existing, unrelated)

## Out of Scope (left intact)
- Mockup HTML (`docs/assets/proton-video/cmp-cdp-mockup.html`)
- Plans/specs (`docs/superpowers/**`)
- Prior journal (`.planning/quick/260317-n4g-*`)

## Notes
Konsistensi dengan CMP yang sudah pakai "Competency Management Platform". User belum verifikasi browser localhost — pending UAT manual.
