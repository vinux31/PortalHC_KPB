---
phase: 420
plan: 01
one_liner: "EditQuestion POST upsert posisional → identity-based (OptionInput.Id carrier + anti-tamper fail-closed + set-difference guard + UPDATE/REMOVE/ADD by Id); GET JSON emit id"
status: complete
commit: f102879d
---

# Phase 420 Plan 01 — Summary

## What changed
- **`Models/OptionInput.cs`**: added `public int? Id { get; set; }` (carrier identity) as first property; revised T-418-06 comment (larangan Id dicabut → kontrak validasi-server: Id client-supplied DIVALIDASI `∈ q.Options` sebelum dipakai).
- **`Controllers/AssessmentAdminController.cs`**:
  - EditQuestion GET (AJAX JSON): added `id = o.Id` to options projection (`:~7908`).
  - EditQuestion POST: replaced positional guard block + positional upsert loop with **identity-based** mechanism:
    - Anti-tamper (D-01a): every non-null submitted `Id` must ∈ `existingIds`; foreign → reject "tidak valid"; duplicate submitted Ids → reject "duplikat" — both fail-closed BEFORE mutation.
    - `removedOptionIds = existingIds.Except(keptIds)` (Essay = all) — set-difference by Id (D-01c kill-drift; guard + upsert share the same set).
    - Guard `OptionShrinkGuard.FindBlockedOptionIds` reused as-is (D-03 answered = ALL responses any status); blocked message (D-04) = stored-order letter (OrderBy Id) + truncated text snippet.
    - Upsert: `keptIds.Contains(o.Id)` → UPDATE by Id (text+IsCorrect+image); existing not in keptIds → REMOVE (guard-cleared, no FK-Restrict); null-Id+text rows → ADD.
- CreateQuestion untouched (ignores Id by construction → OPTEDIT-05 safe).

## Requirements
OPTEDIT-01, OPTEDIT-02, OPTEDIT-03, OPTEDIT-04 (+ carrier for OPTEDIT-05).

## Verification
- `dotnet build HcPortal.csproj -c Debug` → **0 errors** (24 pre-existing nullable warnings, none from this change).
- grep AC all pass: `public int? Id` ×1; `JANGAN tambah properti Id` ×0 (removed); `id = o.Id,` ×1; `existingIds.Except(keptIds)` ×1; `!existingIds.Contains(id)` ×1; `Opsi duplikat terdeteksi` ×1; `tidak valid untuk soal ini` ×1; `keptIds.Contains(o.Id)` ×1; `for (int i = 0; i < bound` ×0 (positional loop removed); `OptionShrinkGuard.FindBlockedOptionIds` ×1 (reused).
- End-to-end behavior proven by Plan 03 integration tests + Playwright (wave 2).

## Threat model status
T-420-01 (IDOR/mass-assignment via client Id) mitigated by server-side `Id ∈ q.Options` fail-closed. T-420-02 (dup Id) rejected. T-420-04 (silent relabel) mitigated by identity match + set-difference guard. T-420-05 (FK-Restrict 500) guard pre-SaveChanges.

migration=FALSE. NOT pushed.
