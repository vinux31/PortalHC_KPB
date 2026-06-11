---
phase: 369-sync-h1-search-drop-fix-main-ithandoff
plan: 01
subsystem: cmp-records-search
tags: [cherry-pick, search, worker-data-service, sync-main]

requires:
  - phase: v21-v23 bundle (main)
    provides: "Commit 14e7adc5 fix H1 di main (post-audit pre-delivery)"
provides:
  - "Guard pre-narrow GetWorkersInSection identik main: searchScope null/kosong di-treat 'Nama'"
  - "Test regresi Scope_Null_WithSearch_FiltersByName_H1 di ITHandoff"
affects: [370-window-unlimited, full-merge-main-pre-handoff]

tech-stack:
  added: []
  patterns: ["Cherry-pick -x untuk sync fix lintas branch dengan jejak audit"]

key-files:
  created: []
  modified:
    - Services/WorkerDataService.cs
    - HcPortal.Tests/WorkerDataServiceSearchTests.cs

key-decisions:
  - "D-01: cherry-pick 14e7adc5 SAJA — full merge main (13 commit) tetap event terpisah pre-handoff IT"
  - "D-02: -x flag — jejak '(cherry picked from commit 14e7adc5...)' untuk dedup saat merge"
  - "Deviasi minor menguntungkan: UAT pakai Playwright browser MCP (orchestrator inline), bukan curl — lebih kuat dari rencana plan"

requirements-completed: [URG-01]

duration: ~8min
completed: 2026-06-11
---

# Phase 369-01: Sync H1 Search-Drop Fix Summary

**Cherry-pick `-x 14e7adc5` masuk ITHandoff sebagai commit `5210e4d4` — guard pre-narrow identik main, test H1 hijau, full suite 229/229, UAT live Playwright: search "Rino" filter 7→1 row.**

## Verifikasi (semua SC)

- **SC#1 guard identik main:** grep ketemu `(string.IsNullOrEmpty(searchScope) || searchScope == "Nama")` di `Services/WorkerDataService.cs:261`; `git diff main` untuk KEDUA file = **KOSONG** (identik total).
- **SC#1 audit trail:** `git log -1` body berisi `(cherry picked from commit 14e7adc5b9e179d4a05e72dbbd7f346e92c10030)`.
- **SC#2 test:** `dotnet build` 0 error (23 warning pre-existing); test H1 `Scope_Null_WithSearch_FiltersByName_H1` Passed 1; **full suite 229/229 Failed: 0** (suite bertambah 214→229 dari sesi paralel 363 — semua hijau, zero regresi REC-06).
- **SC#3 UAT live @5277** (Playwright, `Authentication__UseActiveDirectory=false`): Tab Input Records `/Admin/ManageAssessment?tab=training` → Bagian **GAST** → baseline tanpa search = **7 row**; search **"Rino"** = **1 row** (Rino, NIP 29007720). List TERFILTER, bukan full roster. Read-only — zero perubahan DB, tanpa snapshot/restore.

## Task Commits

1. **Task 1: cherry-pick** — `5210e4d4` (pesan asli main + jejak -x; 2 file, +17/-2). TIDAK ada commit kode tambahan.
2. **Task 2: UAT** — tanpa commit (verifikasi runtime saja).

## Pre-flight (proteksi sesi paralel 363)

Branch ITHandoff ✓ · `.git/index.lock` absen ✓ · file target tidak dirty ✓ · commit belum ancestor ✓. Cherry-pick clean tanpa konflik (sesuai prediksi RESEARCH — pre-image byte-identik).

## Deviations from Plan

- UAT via **Playwright browser MCP** (inline orchestrator) menggantikan pendekatan curl+cookie — lebih kuat (verifikasi visual row table), menghindari kerumitan anti-forgery curl. Cakupan SC#3 sama persis.

## Catatan

- Migration = **FALSE**. ITHandoff **NOT PUSHED** (push = event pre-handoff IT).
- Full merge main→ITHandoff (12 commit tersisa + 1 konflik docs) tetap deferred — tercatat STATE "Push pending IT".
- Phase 370 (window unlimited) dan full merge nanti akan melewati file ini lagi — jejak -x memastikan dedup.

## Self-Check: PASSED

- Commit `5210e4d4` FOUND di ITHandoff (git log) ✓
- Guard line 261 FOUND (grep) ✓
- `git diff main` 2 file kosong ✓
- 229/229 test ✓ · UAT 7→1 ✓
