# Phase 413: Test + UAT - Context

**Gathered:** 2026-06-21
**Status:** Ready for planning
**Source:** Auto-generated (no gray areas — Test+UAT phase, scope locked by spec §Testing + 412 handoff list + SEED_WORKFLOW)

<domain>
## Phase Boundary

Fase penutup milestone v32.5 — bukti end-to-end + kunci regression untuk seluruh fitur add/remove/restore peserta live. Deliverable Phase 413:
1. **xUnit Integration end-to-end** (pola existing `FlexibleParticipantAddLiveTests`/`FlexibleParticipantRemoveTests`, DB disposable `HcPortalDB_Test_{guid}`) — tutup jalur **lifecycle lintas-fase** yang belum di-cover unit per-fase: add→start→soft-remove→guard re-entry blocked→restore→start lagi; Pre/Post pair add+remove sebagai unit; add-idempotent + window + Proton (410); remove hard/soft/idempotent/restore (411); guard re-entry 409.
2. **Playwright e2e multi-context** (`tests/e2e/flexible-participant-412.spec.ts` baru) — **7 sinyal live yang di-defer dari 412**: (a) add picker → baris muncul live tanpa reload (PART-05); (b) modal keras InProgress (PRMV-02); (c) force-kick worker 2-context (admin hapus → worker kena `examRemoved` modal+redirect, PRMV-02); (d) baris pindah ke panel "Peserta Dikeluarkan" live (PLIV-01); (e) Restore 1-klik → baris balik live (D-04); (f) `updateSummaryFromDOM` count aktif turun (exclude #tbodyRemoved, Pitfall 2); (g) multi-observer broadcast (admin A + admin B lihat perubahan, PLIV-02).
3. **Full regression** — `dotnet test` hijau; **tak ada regresi guard Phase 391/398.1** yang sudah ada di main (re-entry guard existing).
4. **Verifikasi lokal lengkap** sebelum 1 push → notify IT (migration=TRUE di Phase 409 `01cd7dd0`).

**TIDAK** ada REQ baru (verifikasi end-to-end 11 REQ existing: PART-05/06/07, PRMV-01/02/03/04/05, PLIV-01/02/03). migration=FALSE.

</domain>

<decisions>
## Implementation Decisions

### Scope test (locked — bukan gray area)
- **D-01:** Playwright e2e **WAJIB real browser multi-context** (lesson Phase 354: build+grep tak cukup untuk Razor/JS/SignalR dinamis). 2 context (admin + worker) untuk force-kick + multi-observer. App @localhost:5277, `Authentication__UseActiveDirectory=false`, login `admin@pertamina.com` (lihat [[reference_dev_credentials]]), combined run `--workers=1` (lihat [[reference_local_e2e_sql_env_fix]]).
- **D-02:** **Seed via SEED_WORKFLOW** (`docs/SEED_WORKFLOW.md`): snapshot DB lokal → seed batch dengan peserta `InProgress` (+ peserta belum-mulai + Completed-bersertifikat untuk tier modal) → jalankan e2e → **restore DB** → tandai SEED_JOURNAL `cleaned`. Klasifikasi seed = `temporary + local-only`.
- **D-03:** xUnit integration **fokus jalur lifecycle lintas-fase** (hindari duplikasi test per-fase 410/411 yang sudah ada). De-tautologis (drive action ASLI + assert DB nyata; no replica predikat — lesson 999.12).
- **D-04:** Bukti regresi: full suite hijau + jalur guard re-entry 409 (StartExam/SubmitExam/JoinBatch block removed) + guard Phase 391/398.1 tetap lulus.

### Claude's Discretion
- Bentuk helper Playwright (seed via API vs sqlcmd langsung), jumlah skenario e2e (minimal 7 sinyal handoff 412; tambah bila perlu).
- Apakah perlu xUnit lifecycle test baru vs cukup yang existing (planner nilai gap coverage).
- Penanganan IN-02 (411): EditAssessment exclude soft-removed — evaluasi di sini bila UAT temukan inkonsistensi tampilan, atau tetap backlog.

</decisions>

<canonical_refs>
## Canonical References

- `docs/superpowers/specs/2026-06-19-flexible-add-remove-participant-design.md` — §Testing (xUnit list + Playwright list), §C/D/E.
- `.planning/phases/412-live-monitoring-ui-signalr/412-VALIDATION.md` — **§Deferred to Phase 413 e2e (Handoff List)** = 7 sinyal + named test draft (sumber kebenaran scope e2e 413).
- `.planning/REQUIREMENTS.md` — 11 REQ (verifikasi end-to-end).
- `.planning/phases/410-...410-02-PLAN.md` + `411-...411-02-PLAN.md` — pola test existing (FlexibleParticipantAddLive/Remove) + fixture `FlexibleParticipantAddFixture` (SQLEXPRESS disposable) + mini-DI SP-stub (hard-delete).
- `CLAUDE.md` — Develop Workflow + **Seed Workflow** (snapshot/restore wajib).
- `tests/` existing e2e specs (pola Playwright @5277).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`FlexibleParticipantAddLiveTests` / `FlexibleParticipantRemoveTests`** + `FlexibleParticipantAddFixture` (SQLEXPRESS disposable) + mini-DI SP-stub — pola xUnit integration langsung pakai.
- **`MonitoringRemovedPanelTests`** (412) — pola test query panel.
- **Endpoint live** 410/411 + UI/SignalR 412 — target e2e.
- **Playwright infra existing** (33 spec) @5277 — pola login + page object.

### Established Patterns
- De-tautologis (drive action/UI ASLI, assert DB/DOM nyata).
- SEED_WORKFLOW snapshot/restore + SEED_JOURNAL.
- Playwright multi-context (2 browser) untuk SignalR live.

### Integration Points
- Konsumsi SELURUH backend 410/411 + UI/SignalR 412 + guard 409.
- Ships terakhir milestone → setelah hijau, 1 push (deploy bundle).

</code_context>

<specifics>
## Specific Ideas

- 7 sinyal e2e = handoff eksplisit 412-VALIDATION.md (jangan kurang).
- Force-kick = 2-context wajib (single-context tak bisa buktikan live kick).
- Seed batch harus punya peserta InProgress (untuk modal keras + force-kick) — DB lokal sering kosong/expired (catatan 412-03: flip sesi temp).
- Restore DB WAJIB pasca-UAT (sukses atau gagal).

</specifics>

<deferred>
## Deferred Ideas

- **IN-02 (411): EditAssessment exclude soft-removed** — backlog kecuali UAT 413 temukan inkonsistensi mengganggu.
- **3 Info 412** (restore-Completed "—" sampai reload, broadcast partner redundan idempoten, fullName quote-escape) — non-blocking, backlog.

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship` — bukan scope 413.

</deferred>

---

*Phase: 413-test-uat*
*Context gathered: 2026-06-21 (auto-generated — Test+UAT, no gray areas)*
