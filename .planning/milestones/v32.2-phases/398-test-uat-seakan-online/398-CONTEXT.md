# Phase 398: Test + UAT "seakan online" (INJ-13) - Context

**Gathered:** 2026-06-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase **penutup milestone v32.2**. TUJUAN: **membuktikan** hasil inject (Phase 393-397) tampil **identik dengan assessment online** end-to-end, jalur online tidak ter-regresi, dan seluruh requirement INJ-01..13 ter-trace. BUKAN membangun fitur baru — ini fase verifikasi/test.

Cakupan konkret (dari ROADMAP + REQUIREMENTS INJ-13):
- E2E full lifecycle: inject (Form / Auto-generate / Excel) → muncul di `/CMP/Records` berlabel "Assessment Online" → `/CMP/Results` menampilkan rincian per-soal benar/salah + breakdown elemen teknis → sertifikat dapat di-download.
- Regression suite hijau (jalur assessment **online** tidak rusak berdampingan dengan inject).
- Audit milestone 13/13 (INJ-01..INJ-13 ter-trace).
- **0 migration.**

DI LUAR scope: fitur baru, edit-massal sesi inject, perubahan engine grading/authoring (semua sudah final di 393-397).
</domain>

<decisions>
## Implementation Decisions

### Cakupan test (otomatis vs manual)
- **D-01:** **E2E otomatis SAJA** — Phase 398 menghasilkan Playwright spec konsolidasi yang repeatable. **TIDAK ada human UAT browser terpisah** untuk 398; bukti "mata manusia" sudah terpenuhi oleh per-phase UAT 394 (7/8), 395 (live), 396 (5/5), 397 (9/9). 398 = automation + regression + audit milestone.

### Parity "seakan online" (kedalaman verifikasi)
- **D-02:** Verifikasi **4 surface downstream WAJIB** (semua harus di-assert di E2E):
  - (a) `/CMP/Records` (+ `RecordsWorkerDetail`) menampilkan baris sesi inject berlabel **"Assessment Online"** (otomatis via `WorkerDataService.GetUnifiedRecords` yang tak filter `IsManualEntry` — spec §2.2).
  - (b) `/CMP/Results` menampilkan **rincian jawaban per-soal benar/salah** (butuh `ShuffledQuestionIds` + `PackageUserResponses` + anchor paket — spec §2.3).
  - (c) **Breakdown elemen teknis** tampil di Results (`SessionElemenTeknisScore`).
  - (d) **Sertifikat PDF dapat diunduh** (untuk skenario cert auto/manual).
- **D-03:** **Side-by-side parity** — assert sesi inject **TAK bisa dibedakan** dari sesi online asli di view yang sama. Dalam ≥1 skenario, sandingkan 1 sesi inject + 1 sesi online asli (struktur baris Records + render Results identik). Ini bukti load-bearing INJ-13 ("bagi pekerja tak bisa dibedakan", spec §1).

### Matriks mode inject di E2E
- **D-04:** Cakupan **representatif** (bukan full cartesian): tiap mode isi-jawaban diuji **1x tembus** inject→Records→Results→cert:
  - **Form** (ketik manual — Phase 395)
  - **Auto-generate** (dari skor target — Phase 395)
  - **Excel** (upload batch — Phase 396)
  - **WAJIB sertakan soal Essay** (risiko §13: harus berakhir `Status=Completed` bukan "Menunggu Penilaian", dan rincian per-soal essay tampil di Results).
  - **+1 skenario Pre/Post linked** (silang inject↔online — Phase 397).
  - Target ~4-5 skenario E2E. Soal MC/MA juga tercakup di dalam skenario tsb.

### Regresi + audit milestone
- **D-05:** Regresi = **(i)** full suite (`dotnet test` unit + integration) hijau **+ (ii)** **live online-path E2E** (create assessment online → pekerja ambil → grade → cert) sebagai bukti jalur online asli tetap utuh berdampingan dengan inject. Reuse spec online existing bila memungkinkan (lihat code_context).
- **D-06:** **Phase 398 menjalankan `/gsd-audit-milestone`** (traceability INJ-01..INJ-13 = 13/13) sebagai bagian penutup phase — bukan ditunda ke command terpisah.

### Claude's Discretion
- Struktur file: spec baru (mis. `tests/e2e/inject-seakan-online-398.spec.ts`) vs perluas existing — planner/researcher putuskan. Reuse helper `accounts.ts`/`dbSnapshot.ts`.
- Cara implement side-by-side parity (D-03): SQL fixture seed sesi online pembanding vs query struktur render — discretion.
- Urutan & granularitas skenario; pemilihan spec online mana yang dipakai untuk regresi jalur online (D-05).
- **0 migration** dipertahankan (konsisten milestone). Konfirmasi via `dotnet ef migrations add _verify` → 0 diff atau `git diff Migrations/` kosong.
- Semua skenario yang menulis DB WAJIB snapshot→restore per CLAUDE.md Seed Workflow; catat di `docs/SEED_JOURNAL.md` + tandai CLEANED. Playwright dari MAIN tree, AD-off (`Authentication__UseActiveDirectory=false`), `--workers=1` (pelajaran 354/392 + reference local e2e SQL env).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & requirement milestone
- `docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md` — §1 (tujuan "tak bisa dibedakan"), §2.2 (Records visibility otomatis, label "Assessment Online"), §2.3 (Results butuh 3 data: ShuffledQuestionIds + responses + paket), §13 (risiko essay Score/Status), Fase 6 (Test + UAT scope). **Sumber kebenaran INJ-13.**
- `.planning/REQUIREMENTS.md` — INJ-13 (acceptance) + INJ-01..INJ-12 (untuk audit milestone 13/13).
- `.planning/ROADMAP.md` — Phase 398 goal + Requirement Coverage table (13/13).

### Bukti prior phase (baseline regresi + apa yang sudah teruji)
- `.planning/phases/393-*/393-*-SUMMARY.md`, `.../394-*`, `.../395-*`, `.../396-*`, `.../397-*` — apa yang sudah dibangun + di-test per phase (hindari duplikasi).
- `.planning/phases/39{3-7}-*/39*-VERIFICATION.md` + `39*-UAT*.md` / SEED_JOURNAL entri — bukti UAT mata-manusia yang sudah ada (D-01 alasan tak ulang human UAT).

### Workflow & guardrail proyek
- `CLAUDE.md` — Develop Workflow + Seed Data Workflow (snapshot/restore WAJIB).
- `docs/DEV_WORKFLOW.md`, `docs/SEED_WORKFLOW.md` — SOP environment + SQL BACKUP/RESTORE.
- Memory `reference_local_e2e_sql_env_fix` — start SQLBrowser + lpc shared-memory override + Playwright `--workers=1`.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets (test infra)
- `tests/e2e/inject-assessment-395.spec.ts` / `inject-excel-396.spec.ts` / `inject-assessment-397.spec.ts` — pola login admin, `test.describe.serial`, `db.snapshot` di `beforeAll` + `db.restore` di `afterAll`, `--workers=1`. **Mirror struktur ini** untuk spec 398.
- `tests/e2e/helpers/accounts.ts` (login admin `admin@pertamina.com`) + `tests/e2e/helpers/dbSnapshot.ts` (`backup()`/`restore()` sqlcmd `-S localhost\\SQLEXPRESS -E -C`; default backup path `C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup`).
- **Online-path regresi (D-05):** reuse/contoh `tests/e2e/exam-taking.spec.ts`, `exam-types.spec.ts`, `assessment.spec.ts`, `cmp-records-346/350/351.spec.ts`, `export-per-peserta.spec.ts` — jalur online create→ambil→grade→cert + Records/Results.

### Established Patterns (surface "seakan online")
- `Services/WorkerDataService.cs:28` `GetUnifiedRecords()` — TIDAK filter `IsManualEntry`; sesi `Completed`/`Menunggu Penilaian` muncul di `/CMP/Records` berlabel "Assessment Online" (D-02a otomatis).
- `Controllers/CMPController.cs:2184` `Results` — render per-soal hanya bila sesi punya `ShuffledQuestionIds` + `PackageUserResponses` + paket ter-anchor; else empty-state "Tinjauan jawaban tidak tersedia" (`Views/CMP/Results.cshtml:413`). Inject menghasilkan ketiganya (Phase 393). (D-02b/c)
- Cert download (PDF) — endpoint existing yang dipakai jalur online (reuse, D-02d).
- `Services/InjectAssessmentService.cs` — sumber sesi inject (Form/Excel/auto-gen, link Pre/Post). Tidak diubah; hanya di-exercise dari UI.

### Integration Points
- E2E masuk via `/Admin/InjectAssessment` (wizard) → commit → lalu navigasi ke `/CMP/Records` + `/CMP/Results` + cert pekerja yang sama. Side-by-side (D-03): seed/ada sesi online asli untuk pekerja pembanding.

### Reusable
- 0 file produksi baru diharapkan (fase test). Output utama = file e2e baru + (opsional) xUnit + artifact audit milestone.
</code_context>

<specifics>
## Specific Ideas

- "Seakan online" = pekerja tak bisa membedakan inject vs online (spec §1). D-03 side-by-side adalah cara membuktikannya secara eksplisit.
- Essay adalah titik risiko utama (§13) — wajib ada di matriks E2E (D-04) dan dicek `Status=Completed` + per-soal tampil.
</specifics>

<deferred>
## Deferred Ideas

- Tidak ada scope creep dari diskusi — semua tetap di domain test/verifikasi INJ-13.

### Reviewed Todos (not folded)
- **One-time cleanup data test/audit lokal setelah Phase 367 ship** (`2026-06-11-one-time-cleanup-...md`, score 0.6) — REVIEWED, **tidak di-fold**. Itu cleanup data sisa Phase 367 (di luar scope INJ-13). Phase 398 sendiri self-clean via snapshot/restore per skenario (CLAUDE.md Seed Workflow), jadi tak menambah residu. Biarkan sebagai todo terpisah.
</deferred>

---

*Phase: 398-test-uat-seakan-online*
*Context gathered: 2026-06-18*
