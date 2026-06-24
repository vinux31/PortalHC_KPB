# Requirements: Portal HC KPB — v32.8 Exam Security & Audit Hardening

**Defined:** 2026-06-24
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Sumber:** Backlog 999.x (defer Phase 425 D-03 + Phase 403 review) + cross-branch overlap check vs `main` (2026-06-24, verified git). 999.9 DROP (SUPERSEDED — main Phase 396-05 sudah hapus BulkBackfill); 999.12 ops-aside (cleanup DB lokal, no code).

## v1 Requirements

Requirements untuk milestone v32.8. Tiap REQ memetakan satu backlog item + temuan, dipetakan ke satu fase (426-428). Branch ITHandoff (bundle deploy v32.1+v32.3+v32.4+v32.7+v32.8).

### AUDIT — Audit Trail Organisasi (Phase 426)

- [x] **AUDIT-01**: Setiap rename/reparent unit via `EditOrganizationUnit` menulis `AuditLog` (mirror pola `DeleteOrganizationUnit`), mencakup actor NIP/nama, oldName→newName, oldParentId→parentId, dan cascade counts (users/mappings/UserUnits). Audit swallow-on-failure (tak memblokir respons). [999.11 / 403-REVIEW WR-01, FLOW: traceability gap]

### EXSEC — Exam Security Hardening (Phases 427-428)

- [x] **EXSEC-01**: Verifikasi token ujian bersifat server-authoritative via kolom `AssessmentSession.TokenVerifiedAt` (menggantikan `TempData.Peek`), dan di-reset (`=null`) saat retake/`ResetExam` agar gate re-arm konsisten. **migration=TRUE** (`AddTokenVerifiedAt`, `DateTime? null` aditif). [999.13 / FLOW-08]
- [ ] **EXSEC-02**: `StartExam` tidak melakukan mutasi status via GET (idempotensi GET) — side-effect transisi `Upcoming→Open` dipindah/diamankan ke jalur POST atau guarded, tanpa mengganggu gate GRDF-01 (Pre→Post) dan time-gate yang tetap berjalan di GET. [999.14 / FLOW-10]

## Future Requirements (deferred)

- **Cleanup ~45 sesi test/legacy HcPortalDB_Dev lokal** (999.12) — OPS-only (no production code); kerjakan ad-hoc via cascade-delete 367 + snapshot, di luar milestone.
- **Refactor DTO penuh ModelState** (VAL-07 versi besar, defer Phase 425 D-04) — backlog bila ingin arsitektur lebih rapi.
- **DROP kolom AssessmentPhase** (defer Phase 425 D-01) — RESERVED dipilih; angkat bila ingin betul-betul hapus.

## Out of Scope (eksklusi eksplisit)

- **999.9 relabel UI BulkBackfill** — **SUPERSEDED**: branch `main` Phase 396-05 (`74f266bf`, INJ-11) sudah hard-remove total fitur BulkBackfill (file `.cshtml` + 2 route + kartu Index + dropdown-item). Commit BUKAN ancestor ITHandoff (verified git). Relabel = kerja terbuang (pasca-merge UI lenyap; resolusi konflik delete/modify = delete). Catat di merge-note.
- **Section/ScopedShuffle/Pagination/OpsiDinamis (v32.6, branch main)** — sudah dikerjakan di main; ITHandoff terima saat merge (bukan duplikasi).

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| AUDIT-01 | Phase 426 | Complete |
| EXSEC-01 | Phase 427 | Complete |
| EXSEC-02 | Phase 428 | Pending |

**Coverage:** 3/3 v1 REQ mapped ke 3 fase (426-428), 0 orphan, 0 duplikat. migration=TRUE hanya Phase 427.

## Merge-Risk Notes (ITHandoff ↔ main — wajib saat rekonsiliasi)

- **R-1 `StartExam` (`CMPController.cs`) = zona konflik PASTI.** ITHandoff-only: GRDF-01 Pre→Post gate (ph424), token re-arm ResetExam, retake. main-only: guard `IsParticipantRemoved` (ph409), Section drift re-guard (ph415), `BuildSectionAwareOptionShuffle`+`Include q.Section` (ph416), `ComputePages` (ph417). Saat merge: pertahankan KEDUA; **GRDF-01 ditempatkan SETELAH cek Completed, SEBELUM token-gate**. Refactor EXSEC-02 dikerjakan di atas metode hasil-merge (patch by-line ITHandoff tak apply bersih).
- **R-2 rantai migrasi divergen** (snapshot model divergen sejak `AddShuffleTogglesToAssessmentSession` 20260613095102). `AddTokenVerifiedAt` (EXSEC-01): timestamp > semua migrasi kedua branch; regen `ApplicationDbContextModelSnapshot.cs`; jangan edit migrasi lama. Kolom nullable aditif → zero-downtime, no backfill (guard `StartedAt==null` bypass sesi InProgress lama).
- **R-3 internal 427↔428** (sama-sama `StartExam`): sekuensial — 427 dulu (migration+token), commit, lalu 428 rebase di atasnya.

---

*Milestone v32.8 — defined 2026-06-24 from backlog + cross-branch overlap analysis.*
