---
quick_id: 260526-ncm
slug: cdp-portal-platform-rename
date: 2026-05-26
status: in-progress
---

# Quick Task: Rename CDP "Portal" → "Platform"

## Description
User: "page CDP, teks Competency Development Portal. portalnya ganti jadi Platform"

Ganti label CDP dari "Competency Development Portal" → "Competency Development Platform" agar konsisten dengan CMP (yang sudah pakai "Platform").

## Scope (Live Code Only)
4 occurrences di Views (mockup docs/superpowers/assets dibiarkan — historical):

1. `Views/CDP/Index.cshtml:2` — `ViewData["Title"]` → `"CDP - Competency Development Platform"`
2. `Views/CDP/Index.cshtml:9` — h1/heading text → `Competency Development Platform`
3. `Views/Home/Index.cshtml:54` — `aria-label="Buka Competency Development Platform"`
4. `Views/Home/Index.cshtml:59` — card `<p>` text → `Competency Development Platform`

## Out of Scope
- `docs/**` (mockup/spec/plan historical artifacts)
- `.planning/quick/260317-n4g-*` (prior task journal)
- `docs/assets/proton-video/cmp-cdp-mockup.html` (video asset)

## Done When
- `grep -r "Competency Development Portal" Views/` → 0 hits
- `dotnet build` pass
- Commit pushed-ready (push by user/IT)
