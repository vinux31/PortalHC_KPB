---
phase: 377-impersonation-identity-across-surfaces
plan: 06
subsystem: impersonation-identity
tags: [e2e, integration, verification, fidelity, uat]
requires: ["377-03", "377-04", "377-05"]
provides:
  - "e2e impersonation.spec SC2/SC3/D-03 (impersonate X → data X lintas surface)"
  - "ResultsAuthorization impersonate-fidelity matrix (D-01)"
  - "seed fixture doc (reuse Iwan) + UAT browser approved"
affects:
  - "tests/e2e/impersonation.spec.ts (rewrite penuh ke UI /Admin/Impersonate)"
tech-stack:
  added: []
  patterns: ["e2e real-Chromium snapshot/restore", "pure fidelity matrix", "reuse-fixture"]
key-files:
  created:
    - .planning/seeds/377-impersonation-fixtures.sql
  modified:
    - HcPortal.Tests/ResultsAuthorizationTests.cs
    - tests/e2e/impersonation.spec.ts
    - docs/SEED_JOURNAL.md
key-decisions:
  - "Target X = Iwan (iwan3@pertamina.com): 4 AssessmentSessions + 2 TrainingRecords ('Pelatihan K3 Dasar'); admin 0 training → differensial deterministik. REUSE existing (no insert)."
  - "ResultsAuth +4 fidelity InlineData D-01 (impersonate X lihat data-X OK, data-Y Forbid, L4 same/diff section) — matrix lama T-346-* utuh."
  - "DRIFT MAJOR: impersonation UI di-redesign ke /Admin/Impersonate page (navbar dropdown 'Lihat Sebagai' lama DIHAPUS). SELURUH impersonation.spec (Phase 283) stale → rewrite penuh ke UI baru (#imp-search, role card confirm dialog) + tambah SC2/SC3/D-03."
requirements-completed: [IMP-01, IMP-02]
duration: "~70 min"
completed: 2026-06-14
---

# Phase 377 Plan 06: Integration & Verification Summary

Gate SC2/SC3/SC4 + fidelity D-01 + UAT browser. Bukti akar bug 999.6 fixed via e2e end-to-end + UAT live.

## Tasks
- **Task 1** (commit `a576810e`): `ResultsAuthorizationTests` +4 InlineData impersonate-fidelity (D-01); `377-impersonation-fixtures.sql` (reuse Iwan, idempotent fallback opsional); SEED_JOURNAL entri 377 (not-applied/reuse). ResultsAuth **15/15**.
- **Task 2** (commit `b73f83c0`): rewrite penuh `impersonation.spec.ts` ke UI `/Admin/Impersonate` + tambah `IMP-377-SC2/SC3/D03`. Full xUnit **372/372**; e2e impersonation **13/13** (`--workers=1`), teardown RESTORE OK + DB clean.
- **Task 3** (UAT browser — Playwright-MCP, driven): 7/7 langkah live PASS (lihat bawah).

## DRIFT MAJOR (Rule 2 — context)
Impersonation UI di-redesign (fase lampau, _Layout 347/309) dari navbar-dropdown inline ('Lihat Sebagai' + StartImpersonation form + #impersonate-search) → **halaman dedikasi `/Admin/Impersonate`** (#imp-search, role card `confirm()`→submit). SELURUH `impersonation.spec.ts` (Phase 283, 9 test) sudah **stale/merah** sebelum 377 (rot pra-existing). Rewrite penuh ke UI baru → 9 test lama HIJAU kembali + 3 baru (SC2/SC3/D-03). Regression guard impersonasi pulih. (Bukan disebabkan kode 377.)

## Verification — automated
- Full xUnit `dotnet test` → **372/372 GREEN** (368 + 4 fidelity), no regression SC4.
- e2e `npx playwright test impersonation --workers=1` → **13/13 GREEN**:
  - IMP-377-SC2: impersonate Iwan → /CMP/Records memuat 'Pelatihan K3 Dasar' (data Iwan, admin 0 training) ✓
  - IMP-377-SC3: /Home greeting 'Iwan' + /CMP/Assessment resolve X ✓
  - IMP-377-D03: impersonate role HC → /CMP/Records 'Pilih user spesifik' hint, no Login redirect ✓
  - 9 test existing (rewritten) + teardown RESTORE OK + DB clean (0 matrix leftover).

## Verification — UAT browser live (Playwright-MCP, 7/7 PASS)
1. Banner: impersonate Iwan → "Anda melihat sebagai Iwan" + navbar "Iwan/Coachee" ✓
2. /CMP/Records: Assessment Online 4 + Training Manual 2 (Pelatihan K3 Dasar + Training Test 2) + Total 6 = data Iwan, **BUKAN admin** (akar bug 999.6 FIXED) ✓
3. /Home: greeting "Selamat Siang, Iwan! · Operator · Alkylation Unit (065)" + Progress (Asm 4/11, Coaching 3/3) + Upcoming "Pre Test OJT GAST Cilacap" = data Iwan ✓ (SC3 split-brain folded)
4. /CMP/Assessment: resolve Iwan ✓
5. (D-03) impersonate role HC → /CMP/Records: hint "Pilih user spesifik untuk melihat data worker", counts 0/0/0, "Data belum ada", URL /CMP/Records (no Login), no admin identity ✓
6. (Pitfall 3) StartExam write-on-GET guard: terpasang `if(!IsImpersonating())` wrap SaveChangesAsync (Plan 03) + e2e flow; live exam-transition tidak diuji (butuh fixture Upcoming-due) — diverifikasi struktural + kode.
7. (SC4) Stop impersonation → kembali Admin KPB, banner hilang ✓; full suite 372/372.
- IMP-06 audit: log mencatat "Mulai/Mengakhiri impersonation (user: Iwan)" 14 Jun 06:38/06:39 ✓.

## Data hygiene
- Reuse existing Iwan data (no insert). e2e harness auto snapshot→restore (matrix). DB lokal post-UAT: 58 sessions, 0 matrix leftover, Iwan trainings=2 (untouched, read-only). DB Dev/Prod TIDAK disentuh.

## Deviations from Plan
**[Rule 2]** Plan "extend IMP-02 flow existing" — flow itu stale (UI redesign). Resolusi: rewrite penuh spec ke UI `/Admin/Impersonate` (lihat DRIFT MAJOR). **Impact:** scope sedikit meluas (perbaiki 9 test rot), nilai naik (regression guard impersonasi pulih). Seed: reuse > insert (data Iwan cukup).

## Next
Phase 377 = 6/6 plan SHIPPED LOCAL. SC1-SC4 + IMP-01/02 terpenuhi. Saran: `/gsd-secure-phase 377` (threat T-377-01..25 — banyak sudah mitigated di plan, audit retro) + `/gsd-verify-work` sudah inline (UAT 7/7). STATE.md TIDAK di-advance (paralel 376/378). NOT PUSHED.

## Self-Check: PASSED
- fidelity matrix 15/15, seed fixture + journal ✓
- e2e SC2/SC3/D03 + 9 rewritten = 13/13; full xUnit 372/372 ✓
- UAT browser 7/7 live (SC2 Records=Iwan, SC3 Home=Iwan, D-03 empty+hint, SC4 restored) ✓
- DB clean, read-only preserved ✓
