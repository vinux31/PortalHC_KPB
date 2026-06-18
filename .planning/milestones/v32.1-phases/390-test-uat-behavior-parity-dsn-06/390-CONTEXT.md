# Phase 390: Test & UAT Behavior Parity (DSN-06) - Context

**Gathered:** 2026-06-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase **penutup milestone v32.1** (cross-cutting). Tujuan: **buktikan redesign tak meregresi**
aksi existing pada 2 surface Admin coach pasca-perubahan Phase 388 (CoachWorkload polish) + Phase 389
(CoachCoacheeMapping accordion). Bukan menambah fitur, bukan ubah perilaku — murni **verifikasi parity**
+ perbaikan defect parity (batas view-only) bila ditemukan.

Cakupan verifikasi:
- **CoachCoacheeMapping** — tambah/edit/nonaktifkan/graduated/hapus/aktifkan-kembali mapping + import & export Excel + modal assign/edit/deactivate/delete.
- **CoachWorkload** — filter section, export Excel, set threshold (Admin), setujui & lewati saran penyeimbangan.

**0 backend, 0 controller, 0 endpoint, 0 migration.** Hanya test (Playwright/xUnit existing) + UAT browser
+ (bila perlu) perbaikan defect tingkat view (`.cshtml` / JS inline).

OUT: fitur baru, refactor, sentuh controller/service/migration, ubah JS-contract di luar yang dibutuhkan
render baru, halaman admin lain di luar 2 surface ini.
</domain>

<decisions>
## Implementation Decisions

### DSN-06 — Strategi & kedalaman verifikasi (Hybrid)
- **D-01 (Hybrid):** Playwright = **parity-assert non-destruktif** (modal field ter-isi benar, AJAX route ter-fire
  via `page.route` intercept, **0 console error**, struktur DSN-01/02/03 utuh). Roundtrip mutasi nyata dijalankan
  via **UAT browser live** (lihat D-03). Alasan: phase ini view-only → risk = regresi wiring/markup, bukan logika
  backend. Full-mutation E2E otomatis ditolak (effort besar + flaky + risk DB lokal); smoke murni ditolak (jaring terlalu tipis).
- **D-02:** **Promote** spec smoke yang sudah ada — `coachcoacheemapping-389.spec.ts` (V-10..V-14) + hook parity
  `coachworkload-388.spec.ts` — dari "modal-buka/hook-ada" jadi **parity-assert lebih kuat** (assert isi field,
  route fired, no-error), tetap **non-destruktif/idempotent**. JANGAN buat seed permanen di spec.

### DSN-06 — Mode aksi tulis (Claude via Playwright MCP)
- **D-03:** Aksi **mutasi DB** (tambah/hapus/reactivate/nonaktif/graduated/edit/setujui-lewati saran) dijalankan
  **Claude via Playwright MCP live** di `localhost:5277`. WAJIB **snapshot DB SEBELUM** (SEED_WORKFLOW `BACKUP DATABASE`)
  + **RESTORE SESUDAH** (sukses atau gagal). Catat di `docs/SEED_JOURNAL.md` (tujuan + klasifikasi temporary+local-only
  + entitas tersentuh) → tandai `cleaned`. User cukup **spot-check akhir** + screenshot bukti.
- **D-04 (daftar aksi roundtrip wajib):**
  - CoachCoacheeMapping: **tambah** (assign modal → simpan), **edit** (edit modal → simpan), **nonaktifkan**
    (`confirmDeactivate`), **graduated** (`MarkMappingCompleted` form POST), **hapus** (`confirmDelete` → row remove),
    **aktifkan-kembali** (`reactivateMapping`). Semua sukses tanpa error/500 + data ter-update benar di UI/DB.
  - CoachWorkload: **filter section**, **set threshold** (sebagai **Admin**), **setujui** (`.approve-btn`) & **lewati**
    (`.skip-btn`) saran penyeimbangan; warna/badge status (Normal/Mendekati/Overloaded) benar.

### DSN-06 — Verifikasi Excel (Export auto, Import manual)
- **D-05:** **Export Excel** (CoachCoacheeMapping + CoachWorkload) = **otomatis Playwright** — assert `download` event
  (file ter-unduh) + cek ringan filename/extension (header/row-count opsional). **Import Excel** = **UAT manual**
  (user upload fixture `.xlsx` kecil disposable; lalu RESTORE) — file-upload otomatis flaky & butuh fixture+restore →
  tak sepadan. **Download Template** = smoke (klik → download event).

### DSN-06 — Scope perbaikan defect (View-only inline; backend → defer)
- **D-06:** Defect parity yang bisa dibetulkan di **view** (`.cshtml` / `@@section Scripts` inline) → **fix inline**
  di phase ini (commit atomic per fix). Defect yang menuntut sentuh **controller/service/backend/migration** →
  **STOP**, dokumentasikan sbg temuan + lapor user untuk keputusan (jangan langgar constraint **0-backend v32.1**).

### Dependency / sequencing
- **D-07:** Phase 390 **depends 388 (DONE) + 389**. Per STATE 2026-06-17, **389-02** (rewrite markup accordion)
  **belum ter-ship** (`M Views/Admin/CoachCoacheeMapping.cshtml` uncommitted, plan 2/2 in-progress). **Eksekusi 390
  TUNGGU 389-02 ter-commit + `dotnet build` hijau.** Discuss/plan boleh sekarang.

### Claude's Discretion
- Organisasi spec: **extend** `coachcoacheemapping-389.spec.ts` + `coachworkload-388.spec.ts` (promote smoke→parity)
  vs file baru `dsn06-parity-390.spec.ts`. Pilih saat plan — kecenderungan **extend existing** (hindari duplikasi spec).
- Detail assert per test, helper login/auth, urutan langkah UAT, format checklist UAT.
- Pemilihan fixture `.xlsx` untuk template/import manual.
- Apakah jalankan `dotnet test` suite penuh atau subset relevan (default: suite penuh harus hijau, tak regresi).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirement & roadmap
- `.planning/REQUIREMENTS.md` §DSN-06 (L29) — acceptance "semua aksi existing tetap berfungsi".
- `.planning/ROADMAP.md` §v32.1 Phase 390 — goal + 4 success criteria (depends 388+389; 0 migration).

### Sibling phase context (parity hooks yang HARUS tetap jalan)
- `.planning/phases/389-.../389-CONTEXT.md` — D-08 (`tr[data-mapping-id]` hook delete), D-09 (kolom Aksi branch
  `IsCompleted→IsActive→else`), D-12 (kontrak `@@section Scripts` tak disentuh: `openEditModal`/`submitAssign|Edit|
  Deactivate|Delete`/`confirmDeactivate|Delete`/`reactivateMapping`/`resetPageAndSubmit`), D-13 (5 modal + id field).
- `.planning/phases/388-.../388-CONTEXT.md` — D-08 (hook `id="sug-@@MappingId"` + `.suggestion-card` + `.approve-btn`/
  `.skip-btn` + `data-*`; `fadeOutCard()`; role-gate Admin pada tombol).

### Test assets (existing — promote smoke → parity)
- `tests/e2e/coachcoacheemapping-389.spec.ts` — V-01..V-09 struktural DSN-01/02/03 + V-10..V-14 smoke DSN-06
  (edit modal / delete / aksi branch / ajax appUrl intercept / filter).
- `tests/e2e/coachworkload-388.spec.ts` — DSN-04/05 + hook parity approve/skip + filter submit/reset.
- `tests/e2e/global.setup.ts` + `tests/e2e/helpers/` — auth/login fixture.

### SOP & workflow (WAJIB ikuti)
- `docs/SEED_WORKFLOW.md` — klasifikasi seed + SQL Server `BACKUP`/`RESTORE` command (snapshot sebelum mutasi).
- `docs/SEED_JOURNAL.md` — format jurnal; catat tiap mutasi temporary → tandai `cleaned`.
- `CLAUDE.md` §Develop Workflow — verifikasi lokal `dotnet build` + `dotnet run` (localhost:5277) + Playwright + UAT
  sebelum commit; ❌ jangan edit Dev/Prod. §Seed Data Workflow — snapshot+restore wajib.

No external ADR/spec — requirements fully captured in decisions above + sibling CONTEXT parity hooks.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **2 spec parity siap di-promote** (D-02): `coachcoacheemapping-389.spec.ts` + `coachworkload-388.spec.ts` — extend
  assert, jangan duplikasi.
- **Playwright auth fixture**: `tests/e2e/global.setup.ts` + `helpers/` (login admin lokal).
- **Avatar/card idiom** sudah dipakai (389) — verifikasi visual saja, bukan target ubah.

### Established Patterns (verifikasi tetap utuh, JANGAN dirusak)
- AJAX semua `fetch(appUrl('/Admin/...'))` + `RequestVerificationToken` (PathBase-aware) — assert via `page.route`
  intercept bahwa URL pakai basePath, bukan hardcode (cegah 404 sub-path).
- Endpoint AJAX TIDAK disentuh: `CoachCoacheeMappingAssign/Edit/Deactivate/Delete/Reactivate/GetSessionCount/
  ActiveAssignmentCount/DeletePreview`; CoachWorkload approve/skip/set-threshold.
- Selector struktural JS: `tr[data-mapping-id]`, id modal (`#assignModal/#editModal/#deactivateModal/#deleteModal/
  #importMappingModal`), `.suggestion-card/.approve-btn/.skip-btn` (CoachWorkload).

### Integration Points
- Tak ada integrasi baru. Hanya test + UAT + (opsional) fix view.

### Verification / environment gotchas (dari memory referensi)
- **Playwright combined run WAJIB `--workers=1`** (login 500/err53 saat paralel) — lihat memory
  `reference_local_e2e_sql_env_fix` (start SQLBrowser + `lpc:` shared-memory conn override utk NTLM loopback).
- Kredensial UAT lokal: `admin@pertamina.com` (memory `reference_dev_credentials`) untuk `/Admin/*`.
- `dotnet run` lokal kadang butuh `Authentication__UseActiveDirectory=false` (lesson Phase 355).
</code_context>

<specifics>
## Specific Ideas

- Phase **penutup v32.1**. Setelah PASS penuh (build 0 error + `dotnet test` hijau + Playwright parity 2 halaman +
  UAT mutasi roundtrip + Excel export auto/import manual): milestone siap **1 push → notify IT re-deploy Dev**
  (**migration=FALSE**).
- Constraint global v32.1 terkunci: **0 backend, 0 controller, 0 migration, behavior parity wajib.**
- Setiap mutasi DB lokal dibungkus snapshot→restore (jangan tinggalkan seed temporary nempel).
</specifics>

<deferred>
## Deferred Ideas

None — diskusi tetap dalam scope phase. Bila defect butuh backend ditemukan saat tes → di-defer keluar v32.1
(D-06) untuk keputusan user, bukan dikerjakan di phase ini.
</deferred>

---

*Phase: 390-test-uat-behavior-parity-dsn-06*
*Context gathered: 2026-06-17*
