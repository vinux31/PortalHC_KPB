# Phase 413: Test + UAT - Research

**Researched:** 2026-06-21
**Domain:** E2E test harness — Playwright multi-context (SignalR live DOM) + xUnit lifecycle integration + full-suite regression; ASP.NET Core MVC (C#, EF Core, SignalR, Razor)
**Confidence:** HIGH (semua pola di-VERIFIED dari kode + spec existing; nol library/API baru)

## Summary

Phase 413 adalah fase penutup milestone v32.5 (Flexible Add/Remove Participant): **tidak ada REQ baru, tidak ada kode produksi baru** — hanya bukti end-to-end + kunci regression. Tiga deliverable: (1) **xUnit lifecycle lintas-fase** (add→start→soft-remove→guard-blocked→restore→start) yang menjembatani celah antar test per-fase 410/411/412; (2) **Playwright e2e multi-context** `tests/e2e/flexible-participant-412.spec.ts` baru — 7 sinyal live yang di-defer eksplisit dari `412-VALIDATION.md §Deferred to Phase 413`; (3) **full regression** `dotnet test` hijau tanpa regresi guard re-entry 409 / guard Phase 391/398.1. `migration=FALSE`, verifikasi lokal, 1 push terakhir (deploy bundle, carry migration=TRUE Phase 409 hash `01cd7dd0`).

Investigasi mengkonfirmasi **seluruh infrastruktur yang dibutuhkan sudah ada di repo** — tidak perlu library baru, tidak perlu pola baru. Pola multi-context SignalR live (admin + worker simultan, `browser.newContext()` ×2, `waitForFunction` atas `window.assessmentHub.state === 'Connected'`, lalu await mutasi DOM yang dipicu context lain) sudah dipakai matang di `exam-types.spec.ts` Flow O (`addExtraTimeViaModal` — referensi kanonik untuk force-kick). Seed `InProgress` reliable sudah dipecahkan di `412-03-SUMMARY` (BACKUP → UPDATE in-place sesi yang punya paket soal jadi `InProgress` via sqlcmd → test → RESTORE). Semua endpoint, handler JS, modal, panel, broadcast sudah live dan ter-smoke runtime per 412. Yang belum: observasi perilaku DOM live di browser nyata multi-context.

**Primary recommendation:** Buat 1 spec e2e baru (`tests/e2e/flexible-participant-412.spec.ts`, `test.describe.configure({ mode: 'serial' })`) yang me-mirror pola Flow O (`exam-types.spec.ts:735-785`) untuk force-kick + multi-observer, gunakan seed `InProgress` reliable lewat `helpers/dbSnapshot.ts` (BACKUP → sqlcmd UPDATE sesi-punya-paket → RESTORE di akhir, atau via `global.setup/teardown` jika perlu state lintas-test). Tambah ≤1 file xUnit lifecycle lintas-fase (`FlexibleParticipantLifecycleTests`) HANYA jika planner menilai ada celah lintas-fase yang belum ter-cover; full regression = jalankan `dotnet test` existing (602/602 baseline), nol test produksi baru.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Add-live picker → baris muncul live (PART-05) | Browser/Client (DOM+SignalR) | API (`AddParticipantsLive`) | Mutasi DOM dipicu broadcast SignalR `participantAdded` — hanya bisa dibuktikan real-browser |
| Modal keras InProgress (PRMV-02 client) | Browser/Client (Bootstrap modal) | — | Tier modal ditentukan `data-status`/`data-has-cert` di JS klien (`:1440-1455`) |
| Force-kick worker 2-context (PRMV-02) | Browser/Client ×2 | API + SignalR Hub | Butuh 2 koneksi browser simultan (admin + worker); `examRemoved` ke `Clients.User(session.UserId)` |
| Panel "Peserta Dikeluarkan" live (PLIV-01) | Browser/Client (DOM move) | — | Handler `participantRemoved` pindah baris ke `#tbodyRemoved` |
| Restore 1-klik live (PRMV-04/D-04) | Browser/Client | API (`RestoreParticipantLive`) | `participantAdded` restore-from-panel |
| `updateSummaryFromDOM` exclude removed (Pitfall 2) | Browser/Client (DOM query) | — | `tbody:not(#tbodyRemoved)` count — verif via DOM |
| Multi-observer broadcast (PLIV-02) | Browser/Client ×2-3 | SignalR Hub group `monitor-{batchKey}` | Admin A + Admin B lihat perubahan sama |
| Lifecycle lintas-fase (add→start→remove→guard→restore) | API + DB (xUnit integration) | — | Kontrak controller ASLI atas SQLEXPRESS — assertable tanpa browser |
| Guard re-entry (PRMV-03) | API (`CMPController`/`AssessmentHub`) | — | `IsParticipantRemoved` server-side — sudah ter-test 409 |
| Full regression no-regression | xUnit suite (in-process) | — | `dotnet test` 602/602 baseline |

## Standard Stack

### Core (semua sudah terpasang — nol instalasi baru)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `@playwright/test` | ^1.58.2 [VERIFIED: tests/package.json] | e2e real-browser multi-context | Pola e2e existing 33 spec; mendukung `browser.newContext()` native |
| `typescript` | ^5.9.3 [VERIFIED: tests/package.json] | spec language | Konsisten suite existing |
| `exceljs` | ^4.4.0 [VERIFIED: tests/package.json] | (tak relevan 413) | — |
| xUnit (.NET) | (pin dlm HcPortal.Tests.csproj) [VERIFIED: 412-VALIDATION] | integration + InMemory real-controller | Framework test produksi (96 file test) |
| sqlcmd (SQL Server Express) | lokal `localhost\SQLEXPRESS` [VERIFIED: dbSnapshot.ts] | seed BACKUP/RESTORE + assert DB | SOP SEED_WORKFLOW; helper `dbSnapshot.ts` wrapper |

**Installation:** Tidak ada. Semua dependency sudah ada (`cd tests && npm ci` jika `node_modules` belum ada — verifikasi sebelum run).

**Version verification:** Tidak ada paket baru ditambahkan. Pin existing di-VERIFIED dari `tests/package.json` (Playwright 1.58.2). Jangan upgrade — phase ini test-only, no-churn.

### Supporting (helper existing — REUSE, jangan tulis ulang)
| Asset | Path | Purpose | When to Use |
|-------|------|---------|-------------|
| `login(page, account)` | `tests/helpers/auth.ts:4` [VERIFIED] | login per-account → tunggu `**/Home/**` | Setiap context (admin/hc/coachee) |
| `accounts` | `tests/helpers/accounts.ts` [VERIFIED] | kredensial AD-off (`admin@pertamina.com`/`123456`, `hc`=meylisa, `coachee`=rino, `coachee2`=iwan3) | Resolve email/password |
| `db.backup/restore/queryScalar/execScript/queryString` | `tests/helpers/dbSnapshot.ts` [VERIFIED] | sqlcmd wrapper (localhost-guard, `-b` exit-non-zero) | Seed BACKUP/RESTORE + assert DB |
| `today()/tomorrow()/yesterday()/uniqueTitle()/autoConfirm()` | `tests/helpers/utils.ts` [VERIFIED] | tanggal LOKAL (selaras `DateTime.Today` server) + judul unik | Create assessment + window dates |
| `createAssessmentViaWizard / createDefaultPackage / addQuestionViaForm` | `tests/e2e/helpers/examTypes.ts` [VERIFIED] | bangun assessment + paket + soal via wizard | Seed batch via UI (alternatif sqlcmd) |
| `addExtraTimeViaModal(pageHc, pageWorker, opts)` | `tests/e2e/helpers/examTypes.ts:534-580` [VERIFIED] | **REFERENSI KANONIK** multi-context SignalR (HC fire modal → worker DOM mutate via SignalR) | Pola force-kick di-mirror dari sini |
| `FlexibleParticipantAddFixture` | `HcPortal.Tests/FlexibleParticipantAddTests.cs:20-53` [VERIFIED] | SQLEXPRESS disposable `HcPortalDB_Test_{guid}` + `MigrateAsync`/`EnsureDeletedAsync` | xUnit lifecycle write-path (REUSE via IClassFixture) |
| `StubUserManager/StubUserStore/NoopNotificationService/MakeLiveController/SeedUserAsync/SeedRepSessionAsync` | `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` [VERIFIED] | drive action ASLI dgn service stub | xUnit lifecycle drive `AddParticipantsLive`/`Remove`/`Restore` |
| `BuildCascadeServiceProvider/MakeLiveControllerWithCascade/StubWebHostEnvironment` | `HcPortal.Tests/FlexibleParticipantRemoveTests.cs` [VERIFIED] | mini-DI untuk hard-delete (`RecordCascadeDeleteService`) | Lifecycle yang menyentuh hard-delete |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Seed via sqlcmd UPDATE in-place (412-03 pola) | Seed batch via UI wizard (`createAssessmentViaWizard`) | UI lebih lambat & rapuh; tapi tak butuh flip Completed→InProgress. **Rekomendasi: sqlcmd untuk InProgress (cepat+deterministik), UI untuk batch not-started bila perlu cert real.** Claude's Discretion (D-CONTEXT) |
| `global.setup/teardown` (seperti assessment-matrix) | Per-test `db.backup`+`db.restore` inline (`finally`) | global = state share lintas-test serial + 1 BACKUP/RESTORE; inline = isolasi per-test tapi BACKUP/RESTORE berulang. **413 spec serial → global setup/teardown lebih bersih + sesuai SEED_JOURNAL flow** |
| xUnit lifecycle test baru | Cukup test per-fase existing (410/411/412) | Planner nilai gap (D-CONTEXT discretion). Lifecycle lintas-fase (add→start→remove→guard→restore dalam 1 alur DB) belum ada test tunggal — **kandidat 1 test baru** |

## Architecture Patterns

### System Architecture Diagram (alur e2e force-kick — sinyal paling kompleks)

```
                          SEED (sebelum spec / global.setup)
                          BACKUP HcPortalDB_Dev → snapshot.bak
                          sqlcmd UPDATE: sesi worker → InProgress
                          (StartedAt=GETDATE, CompletedAt=NULL,
                           ExamWindowCloseDate=+1d, UPA.IsCompleted=0,
                           sesi PUNYA paket soal)
                                    │
              ┌─────────────────────┴──────────────────────┐
              ▼                                             ▼
     CONTEXT 1 (admin)                            CONTEXT 2 (worker/coachee)
     browser.newContext()                         browser.newContext()
     login(admin)                                 login(coachee)
     goto AssessmentMonitoringDetail              goto /CMP/StartExam/{id} (Resume)
     await assessmentHub.state==='Connected'      await assessmentHub.state==='Connected'
     JoinMonitor(batchKey)                        (worker di grup batch)
              │                                             │
              │  klik ⋮ → .btn-hapus-peserta               │  ujian render (#examTimer visible)
              │  (data-status="InProgress" → modal KERAS)  │
              │  #hapusPesertaHardModal show               │
              │  fill #hapusHardReason                      │
              │  klik #btnHapusHardKonfirmasi              │
              │     │                                       │
              │     ▼  POST /Admin/RemoveParticipantLive   │
              │     ▼  (soft-remove + post-commit broadcast)│
              │                                             │
   SignalR participantRemoved ──┐         ┌── SignalR examRemoved {reason}
   ke monitor-{batchKey}        │         │   ke Clients.User(session.UserId)
              ▼                  ▼         ▼              ▼
   baris pindah ke         updateSummary  #examRemovedModal show
   #tbodyRemoved (panel)    count aktif    "Anda telah dikeluarkan..."
   (assert: tr di panel)    turun 1        countdown 5s → redirect /CMP/Assessment
                                           (assert: modal visible + reason text + URL)
                                    │
                          TEARDOWN (akhir spec / global.teardown)
                          RESTORE HcPortalDB_Dev FROM snapshot.bak
                          SEED_JOURNAL active → cleaned
```

### Recommended Test Structure
```
tests/
├── e2e/
│   ├── flexible-participant-412.spec.ts   # BARU — 7 sinyal multi-context (deliverable utama)
│   ├── global.setup.ts                    # existing (assessment-matrix) — POLA seed (jangan tabrakan)
│   ├── global.teardown.ts                 # existing — POLA restore + journal cleaned
│   └── helpers/
│       └── examTypes.ts                    # REUSE addExtraTimeViaModal (referensi multi-context)
├── helpers/
│   ├── auth.ts / accounts.ts              # REUSE login()
│   ├── dbSnapshot.ts                       # REUSE backup/restore/queryScalar/execScript
│   └── utils.ts                            # REUSE today/tomorrow/uniqueTitle
HcPortal.Tests/
└── FlexibleParticipantLifecycleTests.cs   # OPSIONAL BARU — lifecycle lintas-fase (planner nilai gap)
```

### Pattern 1: Multi-Context SignalR Live (REFERENSI KANONIK — Flow O)
**What:** 2 `browser.newContext()` (cookie-isolated), masing-masing login berbeda, tunggu kedua hub `Connected`, fire aksi di context A → assert mutasi DOM live di context B.
**When to use:** force-kick (admin+worker), multi-observer (admin A+admin B), add-live (admin klik → admin B lihat baris).
**Example:**
```typescript
// Source: tests/e2e/exam-types.spec.ts:735-785 (Flow O — VERIFIED)
test('force kick worker — admin hapus InProgress → worker examRemoved (PRMV-02)', async ({ browser }) => {
  const ctxWorker = await browser.newContext();
  const ctxHc = await browser.newContext();
  const pageWorker = await ctxWorker.newPage();
  const pageHc = await ctxHc.newPage();
  try {
    // Worker: login + masuk ujian InProgress (seed)
    await login(pageWorker, 'coachee');
    await pageWorker.goto(`/CMP/StartExam/${sessionId}`);
    // SignalR readiness — VERIFIED window.assessmentHub.state pattern (exam-types.spec.ts:757-764)
    await pageWorker.waitForFunction(() => {
      const w = window as any; return w.assessmentHub?.state === 'Connected';
    }, undefined, { timeout: 10_000 });

    // Admin: login (context terpisah → no cookie collision) + monitoring detail
    await login(pageHc, 'hc'); // atau 'admin'
    await pageHc.goto(`/Admin/AssessmentMonitoringDetail?title=${t}&category=${c}&scheduleDate=${d}`);
    await pageHc.waitForFunction(() => (window as any).assessmentHub?.state === 'Connected', undefined, { timeout: 10_000 });

    // Admin fire: klik Hapus pada baris InProgress → modal KERAS → reason → konfirmasi
    await pageHc.locator(`tr[data-session-id="${sessionId}"] .btn-hapus-peserta`).first().click();
    await pageHc.locator('#hapusPesertaHardModal').waitFor({ state: 'visible' }); // data-bs-backdrop=static
    await pageHc.fill('#hapusHardReason', 'force-kick e2e test');
    await pageHc.locator('#btnHapusHardKonfirmasi').click();

    // ASSERT context B (worker): examRemoved modal live
    await pageWorker.locator('#examRemovedModal').waitFor({ state: 'visible', timeout: 10_000 });
    await expect(pageWorker.locator('#examRemovedModal')).toContainText('Anda telah dikeluarkan dari ujian ini.');
    // (opsional) tunggu redirect countdown 5s → /CMP/Assessment
    await pageWorker.waitForURL('**/CMP/Assessment**', { timeout: 12_000 });
  } finally {
    await ctxWorker.close().catch(() => {});
    await ctxHc.close().catch(() => {});
  }
});
```
> [VERIFIED] `window.assessmentHub.state === 'Connected'` valid di monitoring page (`AssessmentMonitoringDetail.cshtml:1742-1781` — `JoinMonitor(batchKey)`) DAN worker exam page (`StartExam.cshtml` — handler `examRemoved` :1319).

### Pattern 2: Await SignalR-Driven DOM Mutation (panel removed / add-live / restore)
**What:** Setelah aksi di satu context, tunggu selector yang HANYA muncul akibat handler SignalR.
**Example:**
```typescript
// add-live (PART-05): admin klik Tambah → baris muncul di tabel aktif tanpa reload
await pageHc.locator('#btnTambahPeserta').click();                 // buka picker modal (VERIFIED :197)
await pageHc.locator('.tambah-peserta-check').first().check();      // pilih (VERIFIED :1332)
await pageHc.locator('#btnKonfirmasiTambah').click();               // POST AddParticipantsLive (VERIFIED :1364)
// baris baru muncul live (participantAdded handler :2026 inject ke tbody:not(#tbodyRemoved))
await pageHc.waitForSelector(`tbody:not(#tbodyRemoved) tr[data-session-id="${newId}"]`, { timeout: 8_000 });

// panel removed (PLIV-01): baris soft-removed pindah ke panel
await pageHc.waitForSelector(`#tbodyRemoved tr[data-session-id="${sid}"][data-removed="true"]`, { timeout: 8_000 });

// restore (PRMV-04/D-04): tombol Restore → baris balik ke aktif
await pageHc.locator(`#tbodyRemoved tr[data-session-id="${sid}"] .btn-restore-peserta`).click(); // VERIFIED :2100
await pageHc.waitForSelector(`tbody:not(#tbodyRemoved) tr[data-session-id="${sid}"]`, { timeout: 8_000 });
await expect(pageHc.locator(`#tbodyRemoved tr[data-session-id="${sid}"]`)).toHaveCount(0);

// Pitfall 2: count aktif exclude #tbodyRemoved — assert #count-* turun setelah hapus
// VERIFIED: updateSummaryFromDOM :1796-1797 query 'tbody:not(#tbodyRemoved) tr[data-session-id]'
```
> [VERIFIED] Selector kontrak DOM dari `AssessmentMonitoringDetail.cshtml`: `#btnTambahPeserta`, `.tambah-peserta-check`, `#btnKonfirmasiTambah`, `.btn-hapus-peserta`, `#hapusPesertaHardModal`/`#hapusPesertaLightModal`, `#hapusHardReason`/`#hapusLightReason`, `#btnHapusHardKonfirmasi`/`#btnHapusLightKonfirmasi`, `#tbodyRemoved`, `.btn-restore-peserta`, `#countRemoved`, `#panelPesertaDikeluarkan`.

### Pattern 3: Reliable InProgress Seed (412-03 pola — sqlcmd flip)
**What:** Sesi `InProgress` lokal sering EXPIRED atau tak punya paket soal. Solusi terbukti: BACKUP → UPDATE in-place sesi yang PUNYA paket soal jadi InProgress → test → RESTORE.
**Example:**
```sql
-- Source: 412-03-SUMMARY §Runtime Smoke Setup (VERIFIED — pola sukses)
-- Cari sesi yang punya paket soal dulu:
SELECT TOP 1 s.Id FROM AssessmentSessions s
WHERE EXISTS (SELECT 1 FROM AssessmentPackages p WHERE p.AssessmentSessionId = s.Id)
  AND s.RemovedAt IS NULL;
-- Flip ke InProgress:
UPDATE AssessmentSessions
SET Status='InProgress', StartedAt=GETDATE(), CompletedAt=NULL,
    ExamWindowCloseDate=DATEADD(day,1,GETDATE())
WHERE Id=@sid;
UPDATE UserPackageAssignments SET IsCompleted=0 WHERE AssessmentSessionId=@sid;
```
> Catatan 412-03: sesi 150 (admin) TAK punya paket soal ("Sesi ujian ini tidak memiliki paket soal"); sesi 171 punya → flip 171. **Selalu pilih sesi yang punya paket soal.** Schedule date pakai `today()` lokal (lihat Pitfall — utils.ts `fmtLocal` selaras `DateTime.Today` server, hindari off-by-one UTC).

### Pattern 4: xUnit Lifecycle Lintas-Fase (drive action ASLI, assert DB nyata)
**What:** 1 test integrasi yang menjalankan SELURUH siklus dalam 1 alur DB nyata: `AddParticipantsLive` → flip InProgress → `RemoveParticipantLive` (soft) → assert `IsParticipantRemoved`==true → `RestoreParticipantLive` → assert aktif lagi.
**When to use:** Hanya jika planner menilai celah lintas-fase belum ter-cover (D-CONTEXT discretion). Test per-fase 410/411/412 menguji tiap potongan terpisah; lifecycle tunggal membuktikan integrasi.
**Example pattern (REUSE infra existing):**
```csharp
// Source: HcPortal.Tests/FlexibleParticipantRemoveTests.cs (Bagian B — VERIFIED pola)
[Trait("Category", "Integration")]
public class FlexibleParticipantLifecycleTests : IClassFixture<FlexibleParticipantAddFixture>
{
    // REUSE: StubUserManager, NoopNotificationService, MakeLiveController, MakeLiveControllerWithCascade,
    //        SeedUserAsync, SeedRepSessionAsync, BuildCascadeServiceProvider (copy/share dari Remove/AddLive tests)
    [Fact]
    public async Task Lifecycle_Add_Start_SoftRemove_GuardBlocks_Restore_StartAgain()
    {
        // 1. Add → ready-status (Open) + UPA (AddParticipantsLive ASLI)
        // 2. Flip StartedAt (InProgress) via ctx
        // 3. RemoveParticipantLive(reason) → mode="soft"; RemovedAt!=null NYATA
        // 4. Assert CMPController.IsParticipantRemoved(reload) == true  ← guard re-entry lintas-fase
        // 5. RestoreParticipantLive → RemovedAt==null; IsParticipantRemoved==false
        // De-tautology: assert kolom DB NYATA (reload ctx), NO replica predikat (lesson 999.12)
    }
}
```
> [VERIFIED] `CMPController.IsParticipantRemoved(session)` adalah helper produksi ASLI yang dipanggil guard inline StartExam/SubmitExam (`ParticipantRemovalGuardTests.cs:284-315` membuktikan ini di-test de-tautologis di 409).

### Anti-Patterns to Avoid
- **Sleep buta untuk SignalR:** JANGAN `waitForTimeout(5000)` menunggu broadcast. Gunakan `waitForSelector`/`waitForFunction` event-driven (Flow G pola :853-857, Flow O :572-579).
- **Single-context force-kick:** TIDAK bisa membuktikan live kick (D-01 CONTEXT). Force-kick + multi-observer WAJIB 2-context.
- **Cookie collision:** JANGAN login 2 role di context yang sama. `browser.newContext()` per role (Pitfall 5 Flow O :737).
- **Replica predikat (tautologi):** xUnit JANGAN tulis ulang `IsParticipantRemoved`/`SessionHasDataAsync` — drive action ASLI + assert kolom DB nyata (lesson 999.12, enforced di acceptance 410/411).
- **Skip RESTORE saat gagal:** RESTORE WAJIB di `finally`/`global.teardown` (sukses ATAU gagal) — `global.teardown.ts:34-95` pola.
- **Seed sesi tanpa paket soal:** flip InProgress sesi tanpa paket → StartExam error "tidak memiliki paket soal" (catatan 412-03). Pilih sesi yang punya paket.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| BACKUP/RESTORE DB lokal | sqlcmd manual / DELETE script | `db.backup()/db.restore()` (`dbSnapshot.ts`) | localhost-guard + `-b` exit-non-zero + SINGLE_USER rollback sudah benar |
| Login per-context | isi form manual | `login(page, account)` (`auth.ts`) | tunggu `**/Home/**` + akun AD-off resolved |
| Multi-context SignalR coordination | custom polling | mirror `addExtraTimeViaModal` (`examTypes.ts:534`) | pola HC-fire→worker-DOM-await sudah matang + `waitForFunction` hub-Connected |
| InProgress seed | create+take exam via UI tiap test | sqlcmd flip (412-03 pola) | UI lambat/rapuh; flip in-place deterministik |
| SQLEXPRESS disposable DB | InMemory mock cascade | `FlexibleParticipantAddFixture` | schema+kolom NYATA; cascade RecordCascadeDeleteService real |
| Drive controller dgn HttpContext lengkap | WebApplicationFactory | `MakeLiveController` + stub (existing) | repo TAK punya WebApplicationFactory (catatan 999.12 / 410-02-PLAN) |

**Key insight:** Phase 413 adalah fase test/UAT — value ada di MEMAKAI infra existing, bukan membangun yang baru. Setiap pola yang dibutuhkan (multi-context, SignalR-await, seed, disposable DB, mini-DI cascade) sudah ada dan terbukti. Membangun ulang = risiko drift + waktu terbuang.

## Runtime State Inventory

> Phase 413 = test-only, no kode/data produksi baru. Inventory difokus pada state TEST yang berisiko bocor.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | Seed `temporary + local-only` di `HcPortalDB_Dev`: batch dgn peserta InProgress + Completed-cert + not-started (untuk tier modal + force-kick) | BACKUP sebelum + RESTORE sesudah (SEED_WORKFLOW) — **wajib**, bukan code edit |
| Live service config | None — tidak ada layanan eksternal. App lokal @5277 (Kestrel), SignalR in-app | None (verified: tidak ada n8n/Datadog/dll di scope) |
| OS-registered state | None — tidak ada Task Scheduler/pm2/systemd | None |
| Secrets/env vars | `Authentication__UseActiveDirectory=false` saat `dotnet run` (AD-off untuk login lokal) — env var run-time, BUKAN commit (lihat reference_local_e2e_sql_env_fix) | Set saat run app; JANGAN commit launchSettings |
| Build artifacts | `HcPortalDB_Test_{guid}` (xUnit disposable) auto-deleted via `EnsureDeletedAsync`; snapshot `.bak` di SQL default backup dir | Fixture DisposeAsync auto; `.bak` di-unlink di teardown (best-effort) |

**Snapshot/restore wajib:** `docs/SEED_JOURNAL.md` entry status `active` saat seed, `cleaned` setelah RESTORE (sukses ATAU gagal) — `global.teardown.ts` Pattern G regex active→cleaned. Klasifikasi seed: `temporary + local-only`.

## Common Pitfalls

### Pitfall 1: InProgress seed expired / tanpa paket soal
**What goes wrong:** Flip sesi ke InProgress tapi StartExam render "Sesi ujian ini tidak memiliki paket soal", atau window sudah lewat → tak bisa Resume.
**Why it happens:** Sesi lokal lama window EXPIRED; sebagian sesi tak punya `AssessmentPackages`.
**How to avoid:** Pilih sesi `WHERE EXISTS (paket soal)`, set `ExamWindowCloseDate=+1d`, `StartedAt=GETDATE()`, `CompletedAt=NULL`, `UPA.IsCompleted=0` (412-03 pola). Atau buat batch fresh via wizard.
**Warning signs:** StartExam redirect ke /CMP/Assessment dengan TempData error; #examTimer tak muncul.

### Pitfall 2: Off-by-one tanggal (UTC vs lokal)
**What goes wrong:** `today()` via `toISOString()` di UTC+ dini hari = tanggal kemarin → server tolak "Schedule date cannot be in the past."
**Why it happens:** Server validasi pakai `DateTime.Today` (lokal).
**How to avoid:** Gunakan `today()` dari `utils.ts` (sudah `fmtLocal` — Phase 382 fix). JANGAN `new Date().toISOString().slice(0,10)`.
**Warning signs:** Create assessment gagal "in the past".

### Pitfall 3: Race SignalR readiness (assert sebelum Connected)
**What goes wrong:** Fire admin action sebelum worker hub `Connected` → `examRemoved` tak diterima → modal tak muncul → false-fail.
**Why it happens:** SignalR negotiate async (~1-2s).
**How to avoid:** `waitForFunction(() => window.assessmentHub?.state === 'Connected')` di KEDUA context sebelum aksi (Flow O :757-764). Plus 2s buffer setelah StartExam agar server set InProgress (Flow O :769).
**Warning signs:** Test flaky — kadang pass kadang modal tak muncul.

### Pitfall 4: Cookie collision multi-role
**What goes wrong:** Login admin lalu coachee di context yang sama → sesi tabrakan → role salah.
**How to avoid:** 1 `browser.newContext()` per role (force-kick: ctxWorker + ctxHc; multi-observer: ctxAdminA + ctxAdminB + ctxWorker).
**Warning signs:** Worker page redirect ke admin atau sebaliknya.

### Pitfall 5: Restore DB tidak jalan saat test gagal
**What goes wrong:** Spec throw di tengah → DB kotor seed → run berikutnya korup.
**How to avoid:** RESTORE di `global.teardown` (jalan apa pun hasil) ATAU `finally` per-test. Verifikasi Layer 4 (0 seed rows post-restore) seperti `global.teardown.ts:83-94`.
**Warning signs:** SEED_JOURNAL entry stuck `active`; `HcPortalDB_Test_*` tersisa.

### Pitfall 6: hard-delete tak masuk panel removed
**What goes wrong:** Test assert hard-deleted baris muncul di `#tbodyRemoved` — salah; hard-delete `tr.remove()` saja (tak ke panel).
**Why it happens:** `participantRemoved` handler: `mode==='hard'` → `tr.remove()`; `soft` → pindah panel (`:2059-2117`).
**How to avoid:** Seed peserta InProgress/Completed (→ soft → panel) untuk test panel; not-started (→ hard → hilang) untuk test hard-removal hilang.

## Code Examples

### Multi-observer broadcast (PLIV-02 — admin A + admin B)
```typescript
// 3 context: admin A monitoring, admin B monitoring, worker. A hapus → B lihat live.
const ctxA = await browser.newContext(); const ctxB = await browser.newContext();
const pageA = await ctxA.newPage(); const pageB = await ctxB.newPage();
await login(pageA, 'admin'); await login(pageB, 'hc');
await pageA.goto(monitoringUrl); await pageB.goto(monitoringUrl);
await pageA.waitForFunction(() => (window as any).assessmentHub?.state === 'Connected');
await pageB.waitForFunction(() => (window as any).assessmentHub?.state === 'Connected');
// A fire remove → B assert baris pindah ke panel TANPA reload
await pageA.locator(`tr[data-session-id="${sid}"] .btn-hapus-peserta`).click();
// ... isi modal + konfirmasi di pageA ...
await pageB.waitForSelector(`#tbodyRemoved tr[data-session-id="${sid}"]`, { timeout: 8_000 }); // B live
```

### Assert DB state via sqlcmd (komplemen DOM assert)
```typescript
// Source: exam-taking.spec.ts:862 (VERIFIED db.queryScalar usage)
const removedCount = await db.queryScalar(
  `SELECT COUNT(*) FROM AssessmentSessions WHERE Id=${sid} AND RemovedAt IS NOT NULL`);
expect(removedCount).toBe(1); // soft-remove tertulis DB nyata
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Polling monitoring (Flow H) | SignalR live broadcast `participantAdded/Removed` | Phase 412 | e2e await DOM mutation, bukan poll interval |
| Build+grep cukup untuk JS/Razor | Runtime smoke WAJIB (real browser) | Phase 354 lesson | 413 e2e real-browser non-negotiable (D-01) |
| InMemory mock cascade | SQLEXPRESS disposable + mini-DI cascade | Phase 411 | hard-delete assert baris NYATA hilang |

**Deprecated/outdated:**
- Polling-based monitoring assertion untuk add/remove: digantikan SignalR event-driven (Flow H polling masih ada untuk start/submit; add/remove = SignalR).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Full suite baseline = 602/602 (sebelum 413) | Validation Architecture | LOW — 412-VALIDATION melaporkan 602/602; jika berbeda, jalankan `dotnet test` untuk angka aktual (research tidak menjalankan suite penuh) |
| A2 | xUnit lifecycle lintas-fase test BARU mungkin diperlukan (gap coverage) | Pattern 4 | LOW — D-CONTEXT eksplisit serahkan keputusan ke planner; jika test per-fase sudah cukup, skip (nol kerugian) |
| A3 | `node_modules` di `tests/` sudah ter-install | Standard Stack | LOW — jika belum, `cd tests && npm ci` (Playwright 1.58.2 pinned, deterministik) |
| A4 | Seed Completed-cert untuk tier modal "keras Completed" perlu sesi dgn `NomorSertifikat`+`data-has-cert=true` | Seed | MED — modal keras dipicu `status==='Completed' && hasCert` (`:1441`); seed harus set NomorSertifikat agar `data-has-cert=true`. Verifikasi atribut `data-has-cert` di-render di baris (planner cek markup baris) |

**Catatan:** Mayoritas klaim VERIFIED dari kode/spec. Assumptions di atas semua LOW/MED-risk dan dapat dikonfirmasi cepat saat eksekusi (jalankan suite untuk A1; `npm ci` untuk A3).

## Open Questions

1. **xUnit lifecycle test baru: perlu atau tidak?**
   - What we know: test per-fase 410/411/412 menguji potongan terpisah (add, remove-soft/hard, restore, guard) secara de-tautologis. Lifecycle TUNGGAL (add→start→remove→guard→restore dalam 1 alur DB) belum ada.
   - What's unclear: apakah integrasi lintas-fase sudah cukup terbukti oleh kombinasi test existing, atau butuh 1 alur tunggal.
   - Recommendation: tambah 1 `FlexibleParticipantLifecycleTests` (REUSE infra) — murah, nilai regression tinggi, membuktikan guard `IsParticipantRemoved` di tengah lifecycle. Planner finalkan (D-CONTEXT discretion).

2. **Seed Completed-cert untuk tier modal keras-Completed (A4).**
   - What we know: tier keras dipicu `Completed && hasCert`; seed harus set `NomorSertifikat` + atribut baris `data-has-cert=true`.
   - What's unclear: apakah baris monitoring me-render `data-has-cert` (planner verifikasi markup `tr[data-session-id]`).
   - Recommendation: untuk 7 sinyal handoff, force-kick pakai InProgress (deterministik); tier keras-Completed = opsional tambahan jika seed cert mudah. Prioritaskan 7 sinyal handoff 412 dulu.

3. **IN-02 (411 EditAssessment exclude soft-removed) — evaluasi di 413?**
   - What we know: CONTEXT D-CONTEXT discretion — backlog kecuali UAT temukan inkonsistensi.
   - Recommendation: jangan tambah ke scope kecuali UAT live menemukan baris soft-removed muncul di EditAssessment. Default = backlog.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| `localhost\SQLEXPRESS` (HcPortalDB_Dev) | Seed BACKUP/RESTORE + DB assert | ✓ (assumed — dipakai 409/410/411 tests) [ASSUMED] | — | None — blocking jika absent (verifikasi `sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT 1"`) |
| `dotnet` SDK | build + xUnit suite | ✓ [ASSUMED, dipakai tiap fase] | (pin csproj) | None |
| Node + Playwright | e2e | ✓ [VERIFIED tests/package.json] | 1.58.2 | None — `cd tests && npm ci` jika node_modules absent |
| App @localhost:5277 (Kestrel, AD-off) | e2e target | jalankan manual sebelum e2e [VERIFIED baseURL config] | — | None — `dotnet run` dengan `Authentication__UseActiveDirectory=false` sebelum `npx playwright test` |
| SQLBrowser service | login lokal (NTLM loopback) | mungkin perlu start [CITED: reference_local_e2e_sql_env_fix] | — | Start SQLBrowser + shared-memory conn override; combined run `--workers=1` |

**Missing dependencies with no fallback:** SQLEXPRESS + app-running @5277 adalah prasyarat keras (bukan fallback). Verifikasi keduanya sebelum eksekusi.

**Catatan e2e run (CITED: reference_local_e2e_sql_env_fix):** combined Playwright run WAJIB `--workers=1` (NTLM loopback fail tanpa shared-memory override). `fullyParallel: false` sudah di config.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (.NET, pin csproj) + Playwright (^1.58.2) |
| Config file | `tests/playwright.config.ts` (baseURL `http://localhost:5277`, `fullyParallel:false`, `globalSetup`/`globalTeardown`, timeout 60s) [VERIFIED] |
| Quick run command (xUnit subset) | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~FlexibleParticipant"` |
| Full suite command | `dotnet test` (baseline 602/602 [ASSUMED A1]) |
| e2e command | `cd tests && npx playwright test flexible-participant-412 --workers=1` (app @5277 AD-off harus running) |

### Phase Requirements → Test Map (xUnit-assertable VS Playwright-only)

| REQ | Behavior | Test Type | Automated Command | File Exists? |
|-----|----------|-----------|-------------------|-------------|
| PART-05 | Add picker → baris muncul live tanpa reload | **Playwright-only** (SignalR DOM) | `npx playwright test flexible-participant-412 -g "add live"` | ❌ Wave 0 (spec baru) |
| PART-06 | Add ready-status + UPA + window-reject + idempotent | xUnit (sudah ada) | `dotnet test --filter FlexibleParticipantAddLive` | ✅ existing 410 |
| PART-07 | Add Pre/Post pair | xUnit (sudah ada) | `dotnet test --filter FlexibleParticipantAddLive` | ✅ existing 410 |
| PRMV-01 | soft/hard/idempotent | xUnit (sudah ada) | `dotnet test --filter FlexibleParticipantRemove` | ✅ existing 411 |
| PRMV-02 | Modal keras InProgress (client) | **Playwright-only** | `-g "hapus keras"` | ❌ Wave 0 |
| PRMV-02 | Force-kick worker 2-context (examRemoved) | **Playwright-only** (2-context) | `-g "force kick worker"` | ❌ Wave 0 |
| PRMV-03 | Guard re-entry StartExam/SubmitExam/JoinBatch | xUnit (sudah ada) | `dotnet test --filter ParticipantRemovalGuard` | ✅ existing 409 |
| PRMV-04 | Restore live 1-klik | **Playwright-only** (SignalR) + xUnit backend | `-g "restore"` + `dotnet test --filter FlexibleParticipantRemove` | ❌ Wave 0 (e2e) / ✅ (backend) |
| PRMV-05 | Pre/Post pair-as-unit | xUnit (sudah ada) | `dotnet test --filter FlexibleParticipantRemove` | ✅ existing 411 |
| PLIV-01 | Panel removed render + count exclude | xUnit (panel query) + **Playwright** (DOM move + count) | `dotnet test --filter MonitoringRemovedPanel` + `-g "panel removed"` | ✅ (query) / ❌ Wave 0 (DOM) |
| PLIV-02 | Multi-observer broadcast | **Playwright-only** (2-3 context) | `-g "multi observer"` | ❌ Wave 0 |
| PLIV-03 | Audit + RBAC | xUnit (sudah ada) | `dotnet test --filter FlexibleParticipantRemove` | ✅ existing 411 |
| (lifecycle) | add→start→remove→guard→restore (lintas-fase) | xUnit (OPSIONAL baru) | `dotnet test --filter FlexibleParticipantLifecycle` | ❌ Wave 0 (jika planner pilih) |
| (Pitfall 2) | `updateSummaryFromDOM` exclude #tbodyRemoved | **Playwright-only** (DOM count) | bagian "hapus keras"/"panel removed" | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests --filter "FullyQualifiedName~FlexibleParticipant"` (subset cepat)
- **Per wave merge:** `dotnet test` full suite + `npx playwright test flexible-participant-412 --workers=1`
- **Phase gate:** full xUnit suite hijau (602/602+) + 7 sinyal e2e PASS + RESTORE DB verified + build 0 error sebelum 1 push (CLAUDE.md Develop Workflow gate)

### Wave 0 Gaps
- [ ] `tests/e2e/flexible-participant-412.spec.ts` — 7 sinyal multi-context (PART-05, PRMV-02 ×2, PLIV-01, PLIV-02, restore, Pitfall-2) — **deliverable utama**
- [ ] Seed InProgress reliable: pilih `global.setup`/`global.teardown` BARU untuk spec ini ATAU inline `db.backup`/`db.restore` di `finally` (Claude's Discretion D-CONTEXT). **JANGAN tabrakan dengan `global.setup.ts` assessment-matrix existing** (config `globalTeardown` sudah ter-set ke matrix teardown — perlu strategi: spec 413 pakai inline backup/restore, ATAU extend teardown, ATAU run terpisah dgn config override). Planner finalkan.
- [ ] (OPSIONAL) `HcPortal.Tests/FlexibleParticipantLifecycleTests.cs` — lifecycle lintas-fase (planner nilai gap; REUSE infra 410/411)
- [ ] Framework install: `cd tests && npm ci` bila `node_modules` absent (Playwright 1.58.2)

*Catatan kritis seed: `playwright.config.ts` saat ini meng-hardcode `globalTeardown: './e2e/global.teardown.ts'` (matrix). Spec 413 TIDAK boleh bergantung pada matrix seed. Rekomendasi: per-spec `test.beforeAll` (BACKUP + sqlcmd seed) + `test.afterAll` (RESTORE + journal) inline via `db.*` helper — isolasi penuh tanpa menyentuh config global. Lihat Open Question.*

## Security Domain

> `security_enforcement` default enabled. Phase 413 = test-only (nol surface produksi baru), namun e2e MEMBUKTIKAN kontrol keamanan existing berfungsi runtime.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | login AD-off lokal (`admin@pertamina.com`); e2e buktikan RBAC Admin/HC-only endpoint |
| V3 Session Management | yes | cookie-isolated `browser.newContext()` per role; force-kick guard re-entry |
| V4 Access Control | yes | endpoint add/remove/restore `[Authorize(Roles="Admin,HC")]` — e2e worker-context buktikan worker tak bisa akses (auth-gate pola edit-peserta-answers.spec.ts:44-71) |
| V5 Input Validation | yes | `RemovalReason` bebas-teks → XSS-safe via `.textContent` (T-412-14, sudah review-passed 412) — e2e konfirmasi render |
| V6 Cryptography | no | tidak ada operasi kripto di scope 413 |

### Known Threat Patterns for ASP.NET MVC + SignalR

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Test menyentuh `HcPortalDB_Dev` permanen (korup data dev) | Tampering | BACKUP sebelum + RESTORE sesudah (SEED_WORKFLOW); `dbSnapshot.ts` localhost-guard; klasifikasi `temporary + local-only` |
| Worker re-submit setelah dihapus (resubmit removed) | Elevation/Tampering | Guard `IsParticipantRemoved` di StartExam/SubmitExam/JoinBatch (PRMV-03, ter-test 409) — e2e buktikan force-kick + re-entry block |
| XSS via RemovalReason di panel/modal | Tampering (XSS) | `.textContent` (bukan innerHTML) — e2e dapat inject reason `<script>` dan assert ter-encode |
| Non-admin akses endpoint add/remove | Elevation (RBAC bypass) | `[Authorize(Roles="Admin,HC")]` + antiforgery — e2e worker-context assert 403/redirect |
| Test bocorkan kredensial DB | Info Disclosure | `Integrated Security=True` (Windows auth lokal), nol secret literal |

## Project Constraints (from CLAUDE.md)

- **Respons Bahasa Indonesia; kode English.** (RESEARCH prose ID, test code EN.)
- **Develop Workflow:** verifikasi lokal WAJIB (`dotnet build` + `dotnet run` @localhost:5277 + cek DB lokal + Playwright). JANGAN edit kode/DB di Dev/Prod. JANGAN push tanpa verifikasi lokal. Promosi Dev/Prod = tanggung jawab IT.
- **migration=FALSE** Phase 413 (kolom removal sudah dari 409). Carry: notify IT migration=TRUE Phase 409 hash `01cd7dd0` saat 1 push deploy bundle v32.5.
- **Seed Workflow:** klasifikasi seed (`temporary + local-only`), snapshot DB sebelum, catat `SEED_JOURNAL.md`, RESTORE setelah test (sukses ATAU gagal), tandai journal `cleaned`. JANGAN biarkan seed temporary nempel. (Detail `docs/SEED_WORKFLOW.md` — sqlcmd BACKUP/RESTORE.)
- **App @localhost:5277** (catatan reference: ITHandoff worktree pakai 5270; main worktree 5277 — config baseURL = 5277).

## Sources

### Primary (HIGH confidence)
- `.planning/phases/413-test-uat/413-CONTEXT.md` — LOCKED D-01..D-04, scope 7 sinyal, seed workflow
- `.planning/phases/412-live-monitoring-ui-signalr/412-VALIDATION.md` — §Deferred to Phase 413 (7 sinyal + draft test names = sumber kebenaran scope)
- `.planning/phases/412-live-monitoring-ui-signalr/412-02-SUMMARY.md` + `412-03-SUMMARY.md` — selector UI/handler + pola seed InProgress sqlcmd (412-03 §Runtime Smoke Setup)
- `docs/superpowers/specs/2026-06-19-flexible-add-remove-participant-design.md` — §Testing (xUnit + Playwright list), §C/E (SignalR + guard)
- `.planning/REQUIREMENTS.md` — 11 REQ + traceability
- `tests/playwright.config.ts` — baseURL 5277, fullyParallel:false, globalSetup/teardown
- `tests/e2e/exam-types.spec.ts:735-785` + `tests/e2e/helpers/examTypes.ts:534-580` — **referensi kanonik multi-context SignalR (Flow O / addExtraTimeViaModal)**
- `tests/e2e/edit-peserta-answers.spec.ts:44-148` — multi-context (admin/worker auth-gate + 2-admin concurrency)
- `tests/e2e/exam-taking.spec.ts` — login/seed/db.queryScalar/SignalR-await pola (Flow G/H)
- `tests/helpers/{auth,accounts,dbSnapshot,utils}.ts` + `tests/e2e/global.{setup,teardown}.ts` — helper REUSE + BACKUP/RESTORE/journal pola
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — selector + handler SignalR (`#btnTambahPeserta`, modals, `#tbodyRemoved`, participantAdded/Removed, updateSummaryFromDOM exclude)
- `Views/CMP/StartExam.cshtml:1317-1350` — handler `examRemoved` + `#examRemovedModal`
- `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` / `FlexibleParticipantRemoveTests.cs` / `ParticipantRemovalGuardTests.cs` — pola xUnit (fixture, stub, mini-DI, de-tautology, guard)

### Secondary (MEDIUM confidence)
- MEMORY: reference_local_e2e_sql_env_fix (SQLBrowser + shared-memory + `--workers=1`), reference_dev_credentials (admin login), reference_ithandoff_test_port_5270 (port note)

### Tertiary (LOW confidence)
- None — semua klaim load-bearing VERIFIED dari kode/spec; assumptions di Assumptions Log (LOW/MED, mudah dikonfirmasi runtime).

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — nol library baru; semua pin VERIFIED dari package.json/csproj
- Architecture (multi-context SignalR pattern): HIGH — pola Flow O matang + selector kontrak VERIFIED dari view
- Seed strategy: HIGH — pola 412-03 terbukti sukses (BACKUP→flip→RESTORE)
- xUnit lifecycle: MEDIUM — infra REUSE VERIFIED; perlu-tidaknya test baru = D-CONTEXT discretion
- Pitfalls: HIGH — diturunkan dari catatan eksplisit fase 354/382/412 + Flow O/G/H

**Research date:** 2026-06-21
**Valid until:** ~2026-07-21 (30 hari — test infra stabil, nol dependency fast-moving; selector view dapat bergeser bila monitoring di-refactor pasca-413)
