# Phase 326: Validator Hardening (P03 + P06) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md — this log preserves alternatives considered.

**Date:** 2026-05-27
**Phase:** 326-validator-hardening-p03-p06
**Areas discussed:** Edit-side P03 scope, RenewsSessionId branch parity, Test approach, Same-day edge case, Edit-side UX form follow-up

---

## Edit-side P03 Scope (Q1)

**Context:** EditTrainingRecordViewModel TIDAK punya field `RenewsTrainingId`/`RenewsSessionId` (Models/EditTrainingRecordViewModel.cs:1-66). Edit form tidak bisa change renewal source. SC-6 spec "Edit case self-renewal check" vacuous tanpa extension.

| Option | Description | Selected |
|--------|-------------|----------|
| Add-only, drop SC-6 (Recommended) | Apply P03 hanya AddTraining POST. SC-6 N/A — Edit handler tidak terima perubahan renewal jadi tidak ada cycle risk. Effort minimal. | |
| Extend EditTrainingRecordViewModel + Edit handler | Tambah RenewsTrainingId/RenewsSessionId ke VM, modify Edit form Razor + handler accept renewal source change, apply P03 + self-renewal check. Scope creep ubah VM + view + handler. Effort +1-2 jam. | ✓ |
| Add-only + tambah guard di Edit | Apply P03 di Add. Tambah guard di Edit handler reject form tampering RenewsTrainingId via request body. Defense-in-depth tanpa ubah VM/form. | |

**User's choice:** Extend EditTrainingRecordViewModel + Edit handler
**Notes:** Honor SC-6 roadmap. Buka pertanyaan follow-up Q1b (UX scope) karena view full-page TIDAK punya picker UI saat ini.

---

## Edit-side UX Form (Q1 follow-up)

**Context:** Q1 pilih "Extend" — perlu klarifikasi UX renewal di Edit form. View EditTraining.cshtml full-page zero renewal source UI saat ini.

| Option | Description | Selected |
|--------|-------------|----------|
| A — Display read-only + Clear button (Recommended) | Section read-only "Renewal dari: {source title}" + button [Hapus link renewal] (set FK null). User TIDAK bisa change ke source berbeda. Effort ~15 menit Razor + handler clear-only logic. SC-6 = guard kalau form tampering set RenewsTrainingId=model.Id. | ✓ |
| B — Full picker UI (radio + dropdown) | Major UX work — radio toggle TR/AS source + dropdown typeahead worker filter mirror AddTraining renewal-mode. Effort +3-4 jam (view + JS + handler validators full). Scope creep besar. | |
| C — Hidden input passthrough (defense only) | TIDAK ubah UI. Tambah hidden RenewsTrainingId/SessionId di view (passthrough). Handler validate (P03 + self-renewal) defense kalau form tampering. SC-6 vacuous karena user tidak punya jalan UI legit. | |

**User's choice:** A — Display read-only + Clear button
**Notes:** User minta penjelasan plain-language sebelum jawab (caveman drop auto-clarity). Pilih opsi minimal-effort yang tetap kasih user kontrol "hapus link salah" tanpa bangun UI lengkap.

---

## RenewsSessionId Branch Parity (Q2)

**Context:** AddTraining POST punya symmetric validator untuk `RenewsTrainingId` (TR source) DAN `RenewsSessionId` (AS source) di L241+L248. Spec §6.1 hanya tunjukkan kode P03 buat `RenewsTrainingId`.

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, symmetric kedua FK (Recommended) | Tambah validator buat RenewsTrainingId (query TrainingRecord.Tanggal) DAN RenewsSessionId (query AssessmentSession.{DateField}). Bug-findings P03 implicit cover both — renewal chain bisa AS→TR atau TR→AS. Coverage parity. | ✓ |
| TrainingRecord saja per spec literal | Implement exactly spec §6.1 — only RenewsTrainingId branch. AS source tidak divalidasi DAG-wise. Risk: cycle AS→TR→AS lolos. | |

**User's choice:** Ya, symmetric kedua FK
**Notes:** Plan-phase confirm field nama tanggal di AssessmentSession model (kandidat: TanggalMulai, StartTime, CreatedAt).

---

## Test Approach (Q3)

**Context:** Phase 325 D-08 set up `HcPortal.Tests/` xUnit. P03/P06 = controller-level validator (butuh EF Core context).

| Option | Description | Selected |
|--------|-------------|----------|
| Manual repro saja per spec (Recommended) | Per spec §6.3: 2 scenario P03 + 3 scenario P06 via UI Add/Edit Training. Playwright optional. Zero xUnit overhead. | ✓ |
| xUnit controller test pakai EF InMemory | Extend HcPortal.Tests/ dengan TrainingAdminControllerTests.cs — EF Core InMemory + WebApplicationFactory atau direct controller. 5-6 test case otomatis. Effort +1-2 jam. | |
| Hybrid: xUnit P06 + manual P03 | P06 pure sync (no DB query) — testable inline. P03 butuh DB query — manual saja. Minimal effort dengan coverage P06 regression. | |

**User's choice:** Manual repro saja per spec
**Notes:** 6 scenario manual cover 6 SC roadmap (SC-1..SC-6). xUnit defer ke Phase 327 (DeriveCertificateStatus lebih test-friendly).

---

## Same-day Renewal Edge Case (Q4)

**Context:** Spec §6.1 line 203 pakai `src.Tanggal >= model.Tanggal` → reject. Artinya same-day renewal (renewal tanggal === source tanggal) DITOLAK.

| Option | Description | Selected |
|--------|-------------|----------|
| Strict > per spec literal (Recommended) | Reject same-day renewal. Semantic clean: renewal pasti hari berikutnya minimum. Tidak ada workflow legit same-day renewal. | ✓ |
| Allow same-day (`>` not `>=`) | Pakai `src.Tanggal > model.Tanggal` reject — same-day diperbolehkan. Use case: renewal certificate diterbitkan hari sama dengan source expiry. Departure dari spec literal. | |

**User's choice:** Strict > per spec literal
**Notes:** Konfirmasi spec sebagai locked decision.

---

## Claude's Discretion

- Razor section markup style — Bootstrap card border konsisten dengan section "Data Training" existing
- JavaScript clear-button approach — inline onclick handler minimal footprint
- GetTraining Edit handler lookup query — include User untuk display "Renewal dari: {Judul} ({UserName})" lengkap mirror AddTraining L278-284

## Deferred Ideas

- Full picker UI Edit renewal (radio + dropdown + typeahead) — defer ke milestone improvement UX
- Async client-side validator (real-time AJAX cycle check) — defer indefinitely
- DB CHECK constraint Permanent+ValidUntil (P09) — defer v20.0 backlog
- xUnit controller test extension untuk P03/P06 — defer Phase 327
- DateOnly migration ValidUntil — Phase 327 separate
- Multi-step renewal chain depth validator — out-of-scope monotonic constraint cukup
