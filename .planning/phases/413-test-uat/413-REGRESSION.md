# Phase 413 — Regression Gate + Pemetaan 11 REQ End-to-End + Push-Readiness

**Milestone:** v32.5 — Flexible Add/Remove Participant
**Phase:** 413-test-uat (Plan 03 — gate penutup, ships last)
**Tanggal:** 2026-06-21
**migration:** FALSE (Phase 413; carry migration=TRUE hanya Phase 409 `01cd7dd0`)
**Branch:** main — **NOT pushed** (push = aksi koordinasi IT terpisah, lihat §5)

> Gate regresi penutup milestone v32.5. Mengonsumsi hasil **413-01** (xUnit lifecycle lintas-fase) + **413-02** (Playwright e2e 7 sinyal) dan menjalankan **full `dotnet test`** + per-grup guard untuk membuktikan **0 regresi** pada guard yang sudah ada di main (Phase 391 `DeriveReadyStatus`, tech-debt 398.1, guard re-entry 409, plus 410/411/412), lalu memetakan **11 REQ → bukti end-to-end**.

---

## §1 Suite Summary (angka AKTUAL, bukan asumsi)

Dijalankan 2026-06-21 di lokal (SQLEXPRESS / SQL Server 2025 hidup → Integration write-path BENAR berjalan, **bukan** silent-skip).

### Full suite

| Metrik | Hasil |
|--------|-------|
| `dotnet build` (HcPortal.csproj + HcPortal.Tests) | **0 error** (26 warning pre-existing, NOL dari file 413) |
| `dotnet test` full suite | **Passed: 605, Failed: 0, Skipped: 0, Total: 605** (Duration 11m 3s) |
| Baseline 412-VALIDATION | 602/602 → +3 lifecycle (413-01) = **605** (tak ada test HILANG) |
| **`Failed: 0` → 0 regresi** | ✅ TEGAS |
| Integration trait (`Category=Integration`) | **Passed: 197, Failed: 0, Skipped: 0** (Duration 3m 24s) — SQLEXPRESS write-path **executed**, BUKAN skip (T-413-R3 mitigated) |
| `dotnet run` @ http://localhost:5277 (AD-off) | Boot OK — "Now listening on: http://localhost:5277" + "Application started"; GET / → **HTTP 200** |
| `git status Migrations/ Data/` | **kosong** → migration=FALSE |

### Per-grup guard (no-regression — semua hijau)

| Filter | Fase / cakupan | Hasil |
|--------|----------------|-------|
| `~FlexibleParticipantAdd` | Phase 391 `DeriveReadyStatus` + add 410 | **Passed: 14, Failed: 0** |
| `~ParticipantRemovalGuard` | Phase 409 guard re-entry (StartExam/SubmitExam/JoinBatch block removed) | **Passed: 5, Failed: 0** |
| `~ParticipantRemovalExclude` | Phase 409 exclude-removed (list/count aktif) | **Passed: 3, Failed: 0** |
| `~FlexibleParticipantRemove` | Phase 411 remove hybrid + restore + Pre/Post + audit/RBAC | **Passed: 16, Failed: 0** |
| `~MonitoringRemovedPanel` | Phase 412 panel "Peserta Dikeluarkan" | **Passed: 5, Failed: 0** |
| `~MonitoringUserStatus` | Phase 412 status badge/broadcast | **Passed: 7, Failed: 0** |
| `~FlexibleParticipantLifecycle` | Phase 413-01 BARU (lintas-fase) | **Passed: 3, Failed: 0** |

**Verdict §1:** Full suite **605/605** (Failed: 0) — **0 regresi**, tak ada test hilang vs baseline 602; build 0 error; boot @5277 OK; Integration write-path benar berjalan (197 executed). Re-entry guard 409 + guard add-peserta 391/398.1 + 410/411/412 **semua utuh**. Bug produk `monFlashRow` (fix `c13fdd22`, view-only) **TIDAK** memengaruhi suite xUnit (Razor view) — build 0 error + suite 605/605 mengonfirmasi.

---

## §2 Pemetaan 11 REQ → Bukti End-to-End

Semua 11 REQ **COVERED** (tak ada gap). Kolom xUnit = test lintas-fase aktual; e2e = sinyal 413-02 (Playwright real-browser @5277).

| REQ | Deskripsi singkat | xUnit (fase) | e2e (413-02 sinyal) | Status |
|-----|-------------------|--------------|---------------------|--------|
| **PART-05** | Add peserta live dari Monitoring Detail, baris muncul tanpa reload | (UI-driven; verified runtime) | (a) "add picker → baris live tanpa reload" | **COVERED** |
| **PART-06** | Add buat sesi+UPA ready-status, reject window lewat, idempoten | `FlexibleParticipantAddLive` T5/T6/T7/T10 (410) + Lifecycle L1 (413-01) | (a) add live | **COVERED** |
| **PART-07** | Add ke Pre/Post buat pasangan sesi Pre+Post | `FlexibleParticipantAddLive` T9 Pre/Post (410) + Lifecycle L2 (413-01) | (a) add live | **COVERED** |
| **PRMV-01** | Hapus hybrid by-state (belum-mulai→hard; ada-data→soft) | `FlexibleParticipantRemove` soft/hard/idempotent (411) + Lifecycle L1/L3 (413-01) | (b) modal keras + (d) baris→panel | **COVERED** |
| **PRMV-02** | Hapus peserta aktif wajib konfirmasi keras + force-kick SignalR | (UI/force-kick runtime) | (b) modal keras InProgress + (c) force-kick 2-context (#examRemovedModal + "Anda telah dikeluarkan…" + redirect /CMP/Assessment) | **COVERED** |
| **PRMV-03** | Peserta removed tak bisa lanjut/submit (guard StartExam/SubmitExam/Hub.JoinBatch) | `ParticipantRemovalGuard` (409) + Lifecycle L1 guard `IsParticipantRemoved` (413-01) | (c) force-kick mengeluarkan worker dari layar ujian | **COVERED** |
| **PRMV-04** | Restore peserta soft-removed → aktif lagi | `FlexibleParticipantRemove` restore (411) + Lifecycle L1/L2 restore (413-01) | (e) Restore 1-klik → baris balik aktif | **COVERED** |
| **PRMV-05** | Pre/Post diperlakukan satu unit (hard/soft konsisten) | `FlexibleParticipantRemove` Pre/Post pair (411) + Lifecycle L2 (413-01) | (b/d via pair) | **COVERED** |
| **PLIV-01** | Soft-removed dikecualikan dari semua daftar/count aktif + panel terpisah | `MonitoringRemovedPanel` (412) + `ParticipantRemovalExclude` (409) | (d) baris → #tbodyRemoved live + (f) count aktif exclude #tbodyRemoved | **COVERED** |
| **PLIV-02** | Add/remove tersiar live ke semua Admin/HC via SignalR | (broadcast regression 412) | (g) multi-observer admin A+B (participantAdded/Removed tanpa reload) | **COVERED** |
| **PLIV-03** | Semua aksi tercatat audit + RBAC Admin/HC + antiforgery | `FlexibleParticipantRemove` audit+RBAC (411) | (semua aksi via endpoint ber-RBAC) | **COVERED** |

**Verdict §2:** **11/11 REQ COVERED.** PART-05/PRMV-02/PLIV-02 = e2e-driven (runtime browser, sesuai sifat UI+SignalR); sisanya xUnit + (sebagian) e2e. 0 gap, 0 orphan.

---

## §3 Regression Guards (konfirmasi eksplisit D-04 CONTEXT)

D-04 CONTEXT menuntut bukti regresi: full suite hijau **dan** jalur guard re-entry 409 + guard 391/398.1 tetap lulus. Konfirmasi:

**(a) Guard re-entry Phase 409 — UTUH.** `~ParticipantRemovalGuard` **5/5 Passed**. Helper produksi tunggal `CMPController.IsParticipantRemoved(session) => session.RemovedAt != null` (`CMPController.cs:2540`) — dipakai inline di `StartExam` (`:373`), `SubmitExam` (`:924`), dan `:1611`; Hub `JoinBatch`/`SaveTextAnswer`/`SaveMultipleAnswer` += `&& s.RemovedAt == null`. Peserta soft-removed **tak dapat melanjutkan/submit** (assert lintas-fase Lifecycle L1: `IsParticipantRemoved(reload) == true` setelah soft-remove). Tak ada perubahan kode produksi di Phase 413 yang menyentuh guard ini.

**(b) Guard penambahan-peserta Phase 391 (`DeriveReadyStatus`) + tech-debt 398.1 — UTUH.** `~FlexibleParticipantAdd` **14/14 Passed**. `DeriveReadyStatus` di-reuse Phase 410 (`BuildReadyParticipantSession`) — peserta baru selalu ready-status (Open/Upcoming, **NEVER InProgress**). Tech-debt 398.1 terserap di full suite (605/605, tak ada test yang HILANG vs baseline). Tidak ada regresi.

**(c) Phase 410/411/412 — TAK regresi.** `~FlexibleParticipantRemove` 16/16, `~MonitoringRemovedPanel` 5/5, `~MonitoringUserStatus` 7/7, `~ParticipantRemovalExclude` 3/3 — semua hijau. Kontrak JSON 410/411 + broadcast 412 utuh.

**(d) Bug produk monFlashRow (`c13fdd22`) — tak ada dampak regresi.** Fix view-only (pindah 1 baris `window.monFlashRow = flashRow;` ke blok `@section Scripts`). Razor view tak ter-cover xUnit; namun build 0 error + e2e 5/5 pasca-fix + full suite 605/605 mengonfirmasi tak ada regresi.

---

## §4 e2e 7 Sinyal Live (413-02)

Ringkas hasil checkpoint 413-02 (`tests/e2e/flexible-participant-412.spec.ts`, multi-context @5277 AD-off, `--workers=1`) — **5/5 green** (4 test block mencakup 7 sinyal):

| Sinyal | Deskripsi | Hasil |
|--------|-----------|-------|
| (a) | Add picker → baris live tanpa reload (PART-05) | **PASS** |
| (b) | Modal konfirmasi keras saat hapus peserta InProgress (PRMV-02) | **PASS** |
| (c) | Force-kick worker 2-context (#examRemovedModal + "Anda telah dikeluarkan dari ujian ini." + redirect /CMP/Assessment) (PRMV-02/PRMV-03) | **PASS** |
| (d) | Baris peserta → #tbodyRemoved (panel "Peserta Dikeluarkan") live (PLIV-01) | **PASS** |
| (e) | Restore 1-klik → baris balik aktif (PRMV-04) | **PASS** |
| (f) | Count aktif exclude #tbodyRemoved (updateSummaryFromDOM) (PLIV-01) | **PASS** |
| (g) | Multi-observer admin A+B broadcast (PLIV-02) | **PASS** |

**Temuan bernilai 413-02:** bug produk `flashRow is not defined` (ReferenceError lintas `<script>` block yang membatalkan SELURUH handler picker/hapus/restore — UI add/remove/restore live tak berfungsi di browser) **ditemukan + diperbaiki** (`c13fdd22`, Rule 1). Hanya terdeteksi via e2e real-browser; runtime-smoke 412-02 (cek markup render) tak menangkapnya (re-konfirmasi lesson Phase 354).

**DB & seed:** seed via SEED_WORKFLOW (BACKUP → flip sesi 172 InProgress → RESTORE). DB lokal **bersih** pasca-run, diverifikasi langsung 2026-06-21:
- `HcPortalDB%Test%` databases = **0**
- AssessmentSessions `RemovedAt IS NOT NULL` = **0**
- Matrix sentinel rows = **0**
- Sesi 172 Status = **Open** (baseline)
- SEED_JOURNAL entry 413 = **cleaned** (3 row `active` lain = histori lama 327/360/mobile-UAT, BUKAN 413).

---

## §5 Push-Readiness Gate (ships last — JANGAN eksekusi push di sini)

Phase 413 = fase **terakhir** milestone v32.5. Checklist kesiapan **1 push deploy bundle** (push + notify IT = aksi koordinasi terpisah pasca-approval, CLAUDE.md Develop Workflow step 4-5):

- [x] Full suite hijau (§1: 605/605, Failed: 0) + e2e 7 sinyal PASS (§4: 5/5) + build 0 error + boot @5277 OK.
- [x] migration=FALSE Phase 413 (`git status Migrations/ Data/` kosong).
- [x] **Carry notify IT (WAJIB hadir):** Phase 409 = **migration=TRUE** `AddParticipantRemovalColumns` hash **`01cd7dd0`** (3 kolom nullable additif `RemovedAt`/`RemovedBy`/`RemovalReason`). Phase **410-413 = migration=FALSE**. Bundle deploy = v32.2 (0 migration baru) + v32.5 → **1 push `origin/main`** saat koordinasi IT.
- [x] DB lokal bersih: SEED_JOURNAL 413 cleaned; **0** `HcPortalDB%Test%`; 0 removed rows; 0 matrix rows; sesi 172 = Open baseline.
- [ ] **JANGAN push di plan ini** — push = aksi terpisah pasca-approval. ❌ Tidak ada edit kode/DB Dev/Prod (promosi = Team IT). ❌ JANGAN tarik ITHandoff→main tanpa cherry-pick guard 391/398.1.

**Status push-readiness:** **SIAP-SHIP / PENDING-PUSH.** Semua gate teknis lokal terpenuhi; menunggu approval + koordinasi IT untuk 1 push bundle. **NOT pushed.**

---

## §6 Deferred / Carry

| Item | Status |
|------|--------|
| **IN-02** — Phase 411 `EditAssessment` belum exclude soft-removed | Backlog kecuali UAT 413-02 temukan inkonsistensi. **UAT 413-02 TIDAK menemukan inkonsistensi** (7 sinyal green, count exclude di Monitoring Detail benar). Tetap backlog (off-theme, surface EditAssessment lama). |
| **3 Info Phase 412** non-blocking | Deferred (non-blocking, dari 412-VALIDATION). |
| **T-409-10** (XSS-at-render RemovalReason) | Sudah ditangani di 412 (panel Razor `@`-encode + JS `textContent`, 0 Html.Raw untuk reason). Bukan carry terbuka. |
| **A2** export/impact (`ExportAssessmentResults`/`BulkExportPdf`/`GetDeleteImpact`) belum exclude removed | Deferred (revisit bila perlu; riwayat pekerja TETAP tampil removed by-design — sertifikat utuh & reversibel). |
| Bug produk e2e baru dari 413-02 | **Tak ada tersisa** — `monFlashRow` sudah fixed (`c13fdd22`); 7 sinyal green pasca-fix. |
| Carry-migration IT lama | 360 `PendingProtonBypass`+index + 372 `ShuffleToggles` — notify saat bundle deploy (terpisah dari 409). |

---

## Footer

- **Phase:** 413-test-uat (Plan 03 — regression gate + 11-REQ mapping + push-readiness)
- **Tanggal:** 2026-06-21
- **migration:** FALSE (carry migration=TRUE Phase 409 `01cd7dd0`)
- **Suite:** 605/605 (Failed: 0) · per-grup guard semua hijau · Integration 197 executed · e2e 5/5 (7 sinyal)
- **11 REQ:** 11/11 COVERED
- **Verdict milestone v32.5:** **SIAP-SHIP, PENDING-PUSH** (push + notify IT = aksi terpisah)
- **Branch:** main — **NOT pushed**
