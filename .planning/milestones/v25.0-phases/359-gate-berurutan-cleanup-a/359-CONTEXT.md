# Phase 359: Gate Berurutan + Cleanup (A) - Context

**Gathered:** 2026-06-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Paksa urutan & eligibility Proton di **server-side** + bersihkan tampilan level. Deliverable:
- Gate eligibility server-side di POST `CreateAssessment` (deliverable 100% + gate antar-tahun) — bukan cuma JS (PCOMP-06).
- Gate antar-tahun keras: Tahun N butuh Tahun N-1 (TrackType sama) lulus; bypass-assignment exempt (PCOMP-07).
- Tahun 3 data-driven (PCOMP-08).
- Graduation gate: "Mark graduated" diblok kalau Tahun 3 belum lulus (PCOMP-09).
- Matikan tampilan `CompetencyLevelGranted` + grafik tren (PCOMP-10).

**Tidak termasuk:** Bypass (Phase 360/361). Tidak ada migration (kolom `Origin` sudah dari 358; `CompetencyLevelGranted` dibiarkan dormant, tidak di-drop). Pengisian silabus Tahun 3 = tugas data/admin di luar kode.
</domain>

<decisions>
## Implementation Decisions

### Gate eligibility server-side (PCOMP-06)
- **D-01:** POST `CreateAssessment` (`AssessmentAdminController.cs:844`) untuk `Category=="Assessment Proton"`: re-validate TIAP `UserId` server-side SEBELUM bikin session. Worker tak-eligible (deliverable <100% ATAU Tahun N-1 belum lulus) **di-SKIP**, session tetap dibuat untuk worker yang lolos. Tampilkan ringkasan TempData: "X session dibuat, Y di-skip (alasan: belum 100% / Tahun N-1 belum lulus)". **BUKAN all-or-nothing** — operasional, admin tak perlu ulang batch.
- **D-02:** Reuse `CoacheeEligibilityCalculator.IsEligiblePerUnit` (`Helpers/CoacheeEligibilityCalculator.cs:14`) untuk cek deliverable 100% per-unit (sama seperti CoachMapping:1420 + backfill 358).

### Gate antar-tahun (PCOMP-07)
- **D-03:** "Tahun N-1 lulus" = ada `ProtonFinalAssessment` untuk assignment Tahun N-1, TrackType sama, worker sama (pakai `GetPassedYearsAsync` dari 358 atau query setara). Tahun 1 = tanpa prasyarat.
- **D-04:** Enforce di **KEDUA titik**: (a) assign normal `CoachMapping` + (b) POST `CreateAssessment`. Tutup semua pintu (spec §4.3).
- **D-05:** Style penolakan = **hard-block + pesan jelas** ("Tahun N-1 belum lulus, tidak bisa assign/exam Tahun N"). Tidak ada warning-override (gate keras server-side, spec eksplisit).
- **D-06:** **Bypass-assignment exempt** (A-M4): assignment hasil bypass (Phase 360) tidak kena gate antar-tahun. Phase 359 siapkan titik exempt secara logis (flag/kondisi), implementasi bypass penuh di 360.
- **D-07 (renewal A-M13):** Gate antar-tahun cek Tahun N-1 → **renewal Tahun N** (`RenewsSessionId`) tidak keblok. Pastikan jalur renewal exam Proton tetap lewat gate eligibility server-side yang sama (4.2) — tidak ada pintu samping yang skip gate.

### Tahun 3 data-driven (PCOMP-08)
- **D-08:** Netralin shortcut "Tahun 3 no deliverable → all eligible" (`CoachMapping GetEligibleCoachees:1363` area) jadi murni data-driven: track punya deliverable → wajib 100% apapun tahunnya. **Fallback all-eligible (transisi)** dipertahankan: kalau track Tahun 3 punya 0 deliverable → eligible (final = interview, Duration=0). Begitu silabus diisi, gate 100% otomatis berlaku (A-M7).
- **D-09:** Pastikan bootstrap `AutoCreateProgressForAssignment` bikin `ProtonDeliverableProgress` untuk deliverable Tahun 3 saat assign (verifikasi/extend bila perlu) supaya gate data-driven konsisten. Deteksi tipe final (Duration=0 → interview) tetap jalan utk Tahun 3.

### Graduation gate (PCOMP-09)
- **D-10:** Tombol "Mark graduated" (`CoachMapping:1138`, set `IsCompleted`) hanya boleh kalau **Tahun 3 sudah lulus** (penanda Tahun 3 ada utk worker). Hard-block + pesan kalau belum.

### Matikan level (PCOMP-10 / A-M12)
- **D-11:** Hapus tampilan `CompetencyLevelGranted` + grafik tren di CDP (`CDPController.cs:376/542/592/3517` + 2 partial `_CoacheeDashboardPartial.cshtml`/`_CoachingProtonContentPartial.cshtml`). Penanda render badge **"Lulus/Selesai"** tanpa angka.
- **D-12 (A-M12 scope):** Pembersihan menyeluruh — **prune field di `CDPDashboardViewModel`** + binding di view + grafik tren + export yang bind level (bukan cuma 4 baris baca). Kolom DB `CompetencyLevelGranted` tetap dormant (TIDAK di-drop, no migration).

### Claude's Discretion
- Detail mekanis helper gate antar-tahun (method baru `ProtonYearGate` murni vs extend `ProtonCompletionService.GetPassedYearsAsync`) — planner pilih. Spec sebut "ProtonYearGate" helper cross-year; bisa reuse `GetPassedYearsAsync` (sudah no-gate dari 358).
- Bentuk pesan ringkasan skip (TempData vs JSON) + lokasi tombol/flow.
- Strategi test (xUnit gate logic + Playwright UAT CDP render tanpa level/grafik).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec (otoritas)
- `docs/superpowers/specs/2026-06-09-proton-completion-logic-design.md` §4.2 (gate eligibility server-side A-M2), §4.3 (gate antar-tahun A-5.1), §4.4 (matikan level A-3), §4.5 (Tahun 3 data-driven A-5.2/A-M7), §4.6 (graduation gate A-M8), §4.10 (renewal A-M13), §6 (edge cases A-M2/M4/M7/M12), §5 (yang TIDAK berubah).

### Prior phase context
- `.planning/phases/358-penanda-kelulusan-fondasi-a/358-CONTEXT.md` — D-02 (ProtonYearGate ditahan ke 359), helper/Origin/guard locked.
- `.planning/phases/358-penanda-kelulusan-fondasi-a/358-04-SUMMARY.md` — backfill IsEligiblePerUnit pattern + A-M10.

### Project workflow
- `CLAUDE.md` — Develop Workflow (verifikasi lokal build+run+Playwright sebelum commit; AD lokal `Authentication__UseActiveDirectory=false`).
- `docs/SEED_WORKFLOW.md` — snapshot/restore untuk UAT seed (gate butuh skenario eligible/tak-eligible).

### Catatan plan draft
- `docs/superpowers/plans/2026-06-09-proton-completion-logic.md` — Task 2/6/7/8/9 (gate/level), referensi sekunder sketsa kode, BUKAN otoritas (per 358 D-01).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Helpers/CoacheeEligibilityCalculator.IsEligiblePerUnit(statuses, expectedCount)` (`:14`) — cek deliverable 100% per-unit; sudah dipakai CoachMapping:1420 + backfill 358. Reuse untuk gate D-02.
- `Services/ProtonCompletionService.GetPassedYearsAsync(coacheeId, trackType)` (dari 358) — query no-gate daftar TahunKe ber-penanda; basis gate antar-tahun D-03.

### Established Patterns
- Gate per-unit + skip-with-summary = pola sama dengan backfill 358 (`AssessmentAdminController.cs:3797+`).
- CoachMapping assign + GetEligibleCoachees:1363/1420 = titik filter eligibility existing.

### Integration Points
- `CreateAssessment` POST `:844` (gate server-side D-01).
- `CoachMapping` assign + `:1138` Mark graduated + `:1363` GetEligibleCoachees (D-04/D-08/D-10).
- `CDPController.cs:376/542/592/3517` + `CDPDashboardViewModel` + 2 CDP partial (D-11/D-12).
</code_context>

<specifics>
## Specific Ideas

- Gate harus **server-side** — bukan cuma JS filter (PCOMP-06 eksplisit). Worker bisa nyelip via request manual → server tolak (A-M2).
- Bypass exempt disiapkan logis di 359, implementasi penuh 360 (A-M4).
- Edge case A-M (spec §6 tabel): deliverable di-unapprove setelah penanda terbit → penanda tetap (historis), dashboard `allApproved` jadi false → status turun. Acceptable, koreksi via override/bypass.
</specifics>

<deferred>
## Deferred Ideas

- Bypass Tahun (PendingProtonBypass, closure CL-A/B/C) — Phase 360/361 (Diskusi B).
- Audit/improve Tab1 Override Deliverable — backlog 999.x (spec B §13 out of scope).
- Drop kolom `CompetencyLevelGranted` (cleanup DB) — tidak dilakukan; dormant by decision (A-M5).

None lain — diskusi tetap dalam scope phase.
</deferred>

---

*Phase: 359-gate-berurutan-cleanup-a*
*Context gathered: 2026-06-10*
