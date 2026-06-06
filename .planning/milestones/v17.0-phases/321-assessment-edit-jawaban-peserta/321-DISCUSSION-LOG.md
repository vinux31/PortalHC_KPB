# Phase 321: Assessment Edit Jawaban Peserta - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-21
**Phase:** 321-assessment-edit-jawaban-peserta
**Areas discussed:** PLAN sub-numbering split, Testing strategy (Playwright + manual), Reason preset + flip modal copy, Migration + commit cadence, Branch strategy, Proton Tahun 3 detection, TrainingRecord upsert behavior

---

## PLAN Sub-Numbering Split

| Option | Description | Selected |
|--------|-------------|----------|
| 4-PLAN (recommended) | 01 model+migration (T1-T2), 02 service (T3-T4), 03 controller+view+JS+dropdown (T5-T11), 04 SignalR+ActivityLog+UAT (T12-T13). Atomic per layer. | ✓ |
| 3-PLAN (à la 320) | 01 foundation (T1-T4), 02 controller+view+JS (T5-T10), 03 SignalR+ActivityLog+UAT (T11-T13). Bulk per PLAN. | |
| 5-PLAN granular | 01 model+migration, 02 service, 03 controller backend, 04 view+JS+dropdown, 05 SignalR+ActivityLog+UAT. Paling granular. | |

**User's choice:** 4-PLAN (recommended)
**Notes:** Sequential strict deps (01 → 02 → 03 → 04) — no paralelisasi karena PLAN 02 service signature dipakai PLAN 03 controller, dan PLAN 03 multi-file edit di `AssessmentAdminController.cs` (sama file Phase 320) sequential hindari race.

| Option (deps) | Description | Selected |
|--------|-------------|----------|
| Sequential strict (recommended) | 01 → 02 → 03 → 04 wajib urut. | ✓ |
| Wave (01 lock, 02+03 paralel, 04 last) | PLAN 02 + 03 paralel setelah 01. Risk: controller depend service signature. | |
| Fully sequential per-task | Strict serial, no paralelisasi. | |

---

## Testing Strategy (Playwright + manual)

### Playwright (automated, multi-select)

| Option | Description | Selected |
|--------|-------------|----------|
| Auth gate (Admin/HC/Worker) | Login 3 role → assert 200 vs 403/redirect. | ✓ |
| Happy-path edit save | Login Admin → edit MC → save → assert log count + score recompute. | ✓ |
| Concurrency stale stage | 2 context paralel → assert stale error untuk admin kedua. | ✓ |
| Flip preview dry-run AJAX | POST PreviewEditScore → assert JSON shape. | ✓ |

**User's choice:** All 4 selected

### Manual UAT (multi-select)

| Option | Description | Selected |
|--------|-------------|----------|
| DB verify cascade flip | Pass→Fail cek NomorSertifikat NULL + TR Failed; Fail→Pass cek cert generated + TR Passed. | ✓ |
| SignalR cross-tab live update | 2 tab monitoring, edit di tab 1 → tab 2 auto-update + toast. | ✓ |
| Activity Log Edit History tab | Modal Activity Log → tab Edit History → verify timeline. | ✓ |
| Migration rollback lokal | dotnet ef database update {prev} → verify drop → re-apply. | ✓ |

**User's choice:** All 4 selected
**Notes:** Cakupan lengkap — automated + manual saling complement. Concurrency stale di Playwright susah tapi user tetap pilih (perlu 2 context setup).

---

## Reason Preset + Flip Modal Copy

### Reason label

| Option | Description | Selected |
|--------|-------------|----------|
| Verbose user-friendly (recommended) | "Soal salah / typo", "Kunci jawaban salah", "Bug sistem / glitch", "Permintaan koreksi peserta", "Lainnya (jelaskan)". Code value tetap PascalCase. | ✓ |
| Short label | "Soal salah", "Kunci salah", "Bug sistem", "Permintaan peserta", "Lainnya". | |
| Code as-is (no remap) | Label pakai ReasonCode PascalCase. | |

**User's choice:** Verbose user-friendly

### Flip modal

| Option | Description | Selected |
|--------|-------------|----------|
| Eksplisit konsekuensi (recommended) | Sebut "NomorSertifikat akan dicabut" / "NomorSertifikat baru akan di-generate" + TrainingRecord status flip. | ✓ |
| Ringkas + diff | Cuma "Score X→Y, Result Flip. Lanjutkan?" | |
| Eksplisit + actor checklist | Eksplisit + checkbox "Saya paham konsekuensi" wajib. | |

**User's choice:** Eksplisit konsekuensi

### Toast SignalR

| Option | Description | Selected |
|--------|-------------|----------|
| Verbose audit-style (recommended) | "{actorRole} {actorName} edit jawaban {workerName}: Score {oldScore}→{newScore}, {oldResult}→{newResult}" | ✓ |
| Minimal | "{actorName} edit jawaban {workerName}" | |
| Verbose + reason | Tambah reason label di toast. | |

**User's choice:** Verbose audit-style

---

## Migration + Commit Cadence

### IT handoff timing

| Option | Description | Selected |
|--------|-------------|----------|
| Preemptif + final (recommended) | Heads-up awal saat PLAN 01 selesai + notify final setelah tag + push. | ✓ |
| Final only | Notify cuma setelah tag + push. | |
| Per PLAN milestone | Notify per PLAN selesai (4 ping). | |

**User's choice:** Preemptif + final

### Phase order (320 vs 321)

| Option | Description | Selected |
|--------|-------------|----------|
| Paralel lokal, push bundled | Push 320 + 321 bareng. | |
| Tunggu 320 promoted dulu (safer) | Block sampai IT promo Dev pass. | |
| Push 320 sekarang, lokal 321 | Push 320 + notify IT, paralel lokal 321. | |

**User's choice:** Free-text "320 sudah push (coba check) tapi belum promote"
**Notes:** Verified via `git log origin/main` + `git ls-remote --tags origin v17.0-p320-complete` → commit `5f2306ba`. Phase 320 commits + tag sudah remote. Phase 321 jalan paralel tanpa blocker IT promo Dev 320 (decoupled).

### Commit cadence

| Option | Description | Selected |
|--------|-------------|----------|
| 1 task = 1 commit (recommended) | 13 task = 13 commit, pola Phase 320. | ✓ |
| Group coupled task | ~6-7 commit, model+migration sebagai 1. | |
| 1 PLAN = 1 commit | 4 PLAN = 4 commit besar. | |

**User's choice:** 1 task = 1 commit

---

## Audit Follow-Up (after main 4 areas)

### Branch strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Langsung di main (pola 320) | Commit langsung main, push setelah tag. | |
| Feature branch + merge | `feature/phase-321-edit-jawaban` + merge after UAT pass. | ✓ |
| Worktree isolasi | `.worktrees/phase-321/` isolasi penuh. | |

**User's choice:** Feature branch + merge
**Notes:** Rationale: migration baru = risk lebih tinggi, isolasi mudah revert seluruh phase kalau gagal UAT.

### Proton Tahun 3 detection

| Option | Description | Selected |
|--------|-------------|----------|
| Claude's discretion (codebase check) | Claude verify field/relasi saat plan/execute. | ✓ |
| Flag eksplisit | User sebutkan field/value spesifik. | |

**User's choice:** Claude's discretion (codebase check)

### TrainingRecord upsert behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Upsert (insert kalau gak ada, update kalau ada) | Defensive. | |
| Strict update (assume exist) | Throw kalau missing. | |
| Claude's discretion (verify behavior existing) | Ikuti pola existing `GradingService.OnSessionComplete` / setara. | ✓ (rekomendasi Claude) |

**User's choice:** "sesuai reko kamu" → Claude's discretion (verify behavior existing)
**Notes:** Claude recommend verify pola existing dulu, fallback upsert defensive kalau no existing pattern handle missing TR.

---

## Claude's Discretion

- Proton Tahun 3 detection (verify codebase saat plan/execute)
- TrainingRecord Fail→Pass upsert behavior (ikuti pola existing GradingService)
- Dirty-state JS approach (vanilla addEventListener + form serialize compare)
- Activity Log "Edit History" tab lazy-load (AJAX partial saat tab klik)
- SignalR `monitor-{batchKey}` group naming (ikuti AssessmentHub existing)
- Anti-forgery, error boundary, ARIA dropdown a11y (per spec §5.4/5.6/5.7 + research code)
- Merge strategy feature branch → main (default `--no-ff`, fallback squash)

## Deferred Ideas

- Bulk edit multi-session recompute mass
- Edit Essay (rubrik + score manual override)
- Manual override sertifikat tanpa edit jawaban
- AssessmentEditLog → CSV export (compliance audit)
- Webhook notification eksternal (Slack/Teams)
- Reshuffle history tab dedicated
- Theming brand color modal/toast Pertamina
