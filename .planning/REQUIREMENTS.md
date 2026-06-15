---
milestone: v30.0
milestone_name: Essay Grading Correctness + Monitoring UI Refactor
created: 2026-06-15
status: active
---

# Requirements: v30.0 Essay Grading Correctness + Monitoring UI Refactor

**Milestone goal:** Hasil assessment menampilkan soal essay yang sudah dinilai HC secara benar (count "X/Y benar", Elemen Teknis, Tinjauan Jawaban badge, PDF export) — menutup bug user 2026-06-15 (`CMP/Results` 100% tapi 4/6) lewat satu helper correctness terpusat; lalu rapikan UI penilaian essay di Monitoring jadi tabel list worker + page "Tinjau Essay" per-worker.

**Source:** Brainstorming design `docs/superpowers/specs/2026-06-15-essay-grading-correctness-design.md` (root cause workflow-verified, multi-agent). Menutup backlog **RES-02** (display-drift X/Y vs Score%) + **GRD-02** (empty-MA SetEquals non-empty guard).

**Eksekusi:** 2 phase. Phase 383 (Fase 1) = correctness + test (hotfix, ship duluan, isolated). Phase 384 (Fase 2) = refactor UI Monitoring (decoupled). Keduanya **0 migration**.

---

## v30.0 Requirements

### Phase 383 (Fase 1) — Essay Grading Correctness + Test (poin 2 & 3) · no-migration, read-path only

- [x] **ECG-01**: Helper terpusat `AssessmentScoreAggregator.IsQuestionCorrect(question, responses)` menentukan benar/salah per-soal untuk display (MC/MA/Essay), `bool?` (null=essay belum dinilai/pending). Essay Benar = `EssayScore > 0`, `EssayScore==0` = Salah (D-02). MC/MA replikasi logic inline existing byte-for-byte (kill-drift, pure/EF-free/unit-testable). *(Closes GRD-02 — MA non-empty guard.)*
- [ ] **ECG-02**: Hitungan "(X/Y benar)" di `CMP/Results` menyertakan soal essay yang dinilai benar — di kedua jalur (answer-review ON `CMPController.cs:~2258-2271` + review OFF `~2304-2327`). *(Closes RES-02.)*
- [ ] **ECG-03**: Breakdown "Elemen Teknis" (`CMPController.cs:~2336-2369`) menghitung soal essay sesuai nilai HC (tidak lagi selalu salah).
- [ ] **ECG-04**: Badge "Tinjauan Jawaban" per-soal menampilkan Benar/Salah essay sesuai nilai (bukan selalu "Salah" merah) setelah finalize; essay belum dinilai tampil "Menunggu Penilaian" (pending) terlepas status sesi. Teks jawaban essay (`TextAnswer`) tampil di baris review.
- [ ] **ECG-05**: PDF export hasil (`AssessmentAdminController.cs:~5017`) memakai helper `IsQuestionCorrect` yang sama (essay `>0`) — web Results & PDF konsisten (D-03 unify, threshold lama `>=ScoreValue/2` diselaraskan ke `>0`).
- [ ] **ECG-06**: Regression test mengunci logic Simpan Skor + Selesaikan Penilaian (poin 2, sudah benar — tanpa ubah kode): `SubmitEssayScore` persist `EssayScore` + authz; `FinalizeEssayGrading` recompute score termasuk essay + idempotent.

### Phase 384 (Fase 2) — Refactor UI Monitoring Penilaian Essay (poin 1) · backend unchanged, no-migration

- [ ] **UIG-01**: Halaman Monitoring (`AssessmentMonitoringDetail`) menampilkan **tabel list worker** (kolom: Worker/NIP, jumlah essay belum dinilai, status) menggantikan blok essay inline yang panjang (`:381-481`).
- [ ] **UIG-02**: Tiap baris worker punya tombol "Tinjau Essay" (kanan) yang membuka **page penilaian essay per-worker** terpisah.
- [ ] **UIG-03**: Page penilaian essay per-worker me-reuse endpoint existing `SubmitEssayScore` + `FinalizeEssayGrading` (backend TIDAK diubah) + `EssayGradingItemViewModel`; tombol "Selesaikan Penilaian" ada di page ini.
- [ ] **UIG-04**: Playwright e2e — list worker render, "Tinjau Essay" navigasi ke page, beri skor + Selesaikan Penilaian round-trip sukses.

---

## Future Requirements (deferred)

- Essay rubric editor / partial-credit weighting di luar aturan badge biner `>0`.
- Bulk essay grading lintas worker dalam satu aksi.

## Out of Scope (eksplisit)

- **Migration / schema** — `EssayScore` sudah ada + terisi; fix display-path murni.
- **Pass/Fail logic** — derive dari Score% (sudah benar), tidak disentuh.
- **Cara capture `EssayScore`** atau tipe datanya.
- **Proton / grading non-essay** lifecycle.

---

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| ECG-01 | 383 | Complete |
| ECG-02 | 383 | Pending |
| ECG-03 | 383 | Pending |
| ECG-04 | 383 | Pending |
| ECG-05 | 383 | Pending |
| ECG-06 | 383 | Pending |
| UIG-01 | 384 | Pending |
| UIG-02 | 384 | Pending |
| UIG-03 | 384 | Pending |
| UIG-04 | 384 | Pending |

**Coverage:** 10 REQ → 2 phase (383-384). 0 migration. Menutup backlog RES-02 + GRD-02.
