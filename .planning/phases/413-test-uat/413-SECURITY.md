---
phase: 413-test-uat
audited: 2026-06-21
asvs_level: 1
threats_total: 7
threats_closed: 7
threats_open: 0
block_on: HIGH
status: SECURED
---

# 413-SECURITY.md — Audit Keamanan Phase 413: Test + UAT

**Phase:** 413 — Test + UAT (Milestone v32.5, fase penutup)
**Tanggal Audit:** 2026-06-21
**ASVS Level:** 1
**Ancaman Tertutup:** 7/7
**Ancaman Terbuka:** 0

---

## Ringkasan

Phase 413 mencakup tiga deliverable: (1) xUnit Integration lifecycle lintas-fase `FlexibleParticipantLifecycleTests.cs`, (2) Playwright e2e multi-context 7 sinyal live `tests/e2e/flexible-participant-412.spec.ts`, dan (3) satu production scope-fix di `Views/Admin/AssessmentMonitoringDetail.cshtml` (hoist `window.monFlashRow`). Tidak ada surface produksi baru yang diperkenalkan selain fix scope JS yang bersifat non-data (memindah ekspos fungsi, bukan logika baru).

Semua 7 ancaman dalam threat register ketiga PLAN (413-01/413-02/413-03) berhasil diverifikasi TERTUTUP. Tidak ada ancaman terbuka.

---

## Verifikasi Ancaman Per Plan

### Plan 01 — xUnit Lifecycle Lintas-Fase (T-413-01 s.d. T-413-04)

| Threat ID | Kategori | Disposisi | Bukti (file:baris) | Status |
|-----------|----------|-----------|---------------------|--------|
| T-413-01 | Tampering | mitigate | `FlexibleParticipantLifecycleTests.cs:59` — `IClassFixture<FlexibleParticipantAddFixture>` (REUSE fixture `HcPortalDB_Test_{Guid}` disposable); `FlexibleParticipantAddTests.cs:20-53` FlexibleParticipantAddFixture `InitializeAsync→MigrateAsync` + `DisposeAsync→EnsureDeletedAsync`; TIDAK ada referensi ke `HcPortalDB_Dev` di file lifecycle | CLOSED |
| T-413-02 | Repudiation | mitigate | `FlexibleParticipantLifecycleTests.cs:290` — `CMPController.IsParticipantRemoved(added)` (PRODUKSI `public static`, `CMPController.cs:2540`); `:14` — header komentar eksplisit "NO replica predikat"; `AddParticipantsLive`/`RemoveParticipantLive`/`RestoreParticipantLive` dipanggil langsung sebagai action nyata; TIDAK ditemukan `SessionHasDataAsync`, `WindowAllowsAddition`, atau `ExecuteAsync` di luar komentar | CLOSED |
| T-413-03 | Tampering (Elevation) | mitigate | `FlexibleParticipantLifecycleTests.cs:317-329` — Langkah 5 L1: reload sesi hasil `AddParticipantsLive`+`RemoveParticipantLive` nyata → `CMPController.IsParticipantRemoved(removed)` PRODUKSI `== true` (guard PRMV-03 terbukti terpasang lintas-fase); `CMPController.cs:373,924,1611` — guard inline `StartExam`/`SaveAnswer`/`SubmitExam` memanggil helper yang sama | CLOSED |
| T-413-04 | Information Disclosure | accept | Connection string menggunakan `Integrated Security=True` (Windows auth lokal) via `FlexibleParticipantAddFixture` — tidak ada literal password/secret di file test; konsisten dengan fixture existing (FlexibleParticipantAddTests, FlexibleParticipantRemoveTests) | CLOSED |

**Catatan T-413-04 (accept):** Fixture menggunakan `Integrated Security=True` via `SQLEXPRESS` lokal. Tidak ada secret literal. Risiko diterima karena ini adalah test lokal (bukan CI publik) dan menggunakan mekanisme Windows auth yang sama dengan semua test suite existing.

---

### Plan 02 — Playwright E2E Multi-Context (T-413-E1 s.d. T-413-E5)

| Threat ID | Kategori | Disposisi | Bukti (file:baris) | Status |
|-----------|----------|-----------|---------------------|--------|
| T-413-E1 | Tampering | mitigate | `flexible-participant-412.spec.ts:80` — `db.backup(snapshotPath)` di `beforeAll`; `:117` — `db.restore(snapshotPath)` di `afterAll` (try/finally pattern, restoreError di-surface); `dbSnapshot.ts:38-43` — `runSqlcmd` localhost-only guard (reject non-localhost via `-S` arg check); `docs/SEED_JOURNAL.md:9` — Phase 413 entry status `cleaned` (RESTORE OK terverifikasi 2026-06-21) | CLOSED |
| T-413-E2 | Elevation/Tampering | mitigate | `flexible-participant-412.spec.ts:220-223` — force-kick t(c): `#examRemovedModal` visible + `toContainText('Anda telah dikeluarkan dari ujian ini.')` + `waitForURL('**/CMP/Assessment**')` membuktikan worker tidak bisa lanjut; Guard server `CMPController.IsParticipantRemoved` (PRMV-03) dikunci xUnit lifecycle L1 (`FlexibleParticipantLifecycleTests.cs:329`) | CLOSED |
| T-413-E3 | Tampering (XSS) | mitigate | `Views/Admin/AssessmentMonitoringDetail.cshtml:2026` — konsumsi via `typeof window.monFlashRow === 'function'` guard; alasan hapus (`RemovalReason`) di-render via `.textContent` (XSS-safe, dikonfirmasi di 413-REVIEW.md §IN-02 dan review T-412-14); `monFlashRow` hoist (scope-fix) tidak mengubah logika data handling atau escape path | CLOSED |
| T-413-E4 | Elevation (RBAC bypass) | mitigate | Endpoint `AddParticipantsLive`/`RemoveParticipantLive`/`RestoreParticipantLive` menggunakan `[Authorize(Roles="Admin,HC")]` + antiforgery (dikunci xUnit fase 411); Worker context e2e tidak memiliki akses tombol Tambah/Hapus di monitoring (by-design RBAC view); Dibuktikan e2e: `pageWorker` hanya menavigasi `/CMP/StartExam/{id}` (bukan endpoint admin) | CLOSED |
| T-413-E5 | Information Disclosure | accept | `dbSnapshot.ts:4,23` — connection string `Integrated Security=True`, `-S localhost\SQLEXPRESS`, `-E` (Windows auth); tidak ada password/secret literal di `flexible-participant-412.spec.ts`; konsisten dengan pola dbSnapshot existing (semua spec e2e pakai helper yang sama) | CLOSED |

**Catatan T-413-E5 (accept):** Credentials DB menggunakan Windows Integrated Security (lokal). Tidak ada secret literal di spec file. Risiko diterima karena ini adalah test lokal.

---

### Plan 03 — Gate Regression (T-413-R1 s.d. T-413-R3)

| Threat ID | Kategori | Disposisi | Bukti (file:baris) | Status |
|-----------|----------|-----------|---------------------|--------|
| T-413-R1 | Repudiation | mitigate | Plan 03 Task 1 mewajibkan `dotnet test` aktual (grep `Failed/Passed`); acceptance criteria `Failed: 0` + per-grup filter guard (409/410/411/412/413-01) harus jalan nyata; angka aktual dicatat di `413-REGRESSION.md` (bukan asumsi) | CLOSED |
| T-413-R2 | Tampering | mitigate | `413-03-PLAN.md:148-152` — §5 Push-Readiness Gate: checklist eksplisit + acceptance `grep "JANGAN push"` + carry `01cd7dd0` migration=TRUE wajib hadir; push = aksi manusia terpisah pasca-approval (CLAUDE.md Develop Workflow step 4-5) | CLOSED |
| T-413-R3 | Information Disclosure | accept | SQLEXPRESS digunakan di fase 409/410/411 (VERIFIED env); bila absent → Integration test gagal nyata (bukan silent-skip), karena fixture menggunakan `MigrateAsync` yang throw bila koneksi gagal; Task 1 mencatat Integration executed count untuk bukti non-skip | CLOSED |

**Catatan T-413-R3 (accept):** Risiko false-confidence dari silent-skip Integration test diterima karena behavior fail-fast: `FlexibleParticipantAddFixture.InitializeAsync()` memanggil `MigrateAsync()` yang akan throw jika SQLEXPRESS tidak tersedia, membuat test gagal nyata bukan skip diam.

---

## Verifikasi Produksi: monFlashRow Scope-Fix

**File:** `Views/Admin/AssessmentMonitoringDetail.cshtml`

Scope-fix `monFlashRow` **tidak memperkenalkan attack surface baru**:

1. **Sebelum fix:** `window.monFlashRow = flashRow` dikode di blok IIFE atas (baris ~1037-1571) sebelum `flashRow` didefinisikan di `@section Scripts` — ini menyebabkan ReferenceError lintas `<script>` block.
2. **Sesudah fix (baris 1797):** `window.monFlashRow = flashRow` dipindah ke `@section Scripts` tepat setelah definisi `flashRow` (baris 1788). Fungsi yang masih diekspos di blok atas (`buildActionsHtml`, `statusBadgeClass`, `statusDisplayLabel`, `isPackageMode`) terdefinisi di scope IIFE yang sama — tidak ada cross-block hazard.
3. **Konsumsi aman (baris 2026):** `if (typeof window.monFlashRow === 'function') window.monFlashRow(tr, 'flash-update')` — defensive check, tidak mengekspos data user.
4. **XSS posture tidak berubah:** `flashRow` hanya memanipulasi CSS class pada elemen `<tr>` yang sudah ada di DOM. Tidak ada penanganan data user baru; `RemovalReason` tetap di-render via `.textContent` (XSS-safe, tidak diubah oleh fix ini).
5. **Konfirmasi dari REVIEW.md:** "Fix tidak mengubah logika sama sekali — hanya memindah ekspos."

---

## Threat Flags dari SUMMARY.md

Tidak ada `## Threat Flags` yang dilaporkan di SUMMARY.md fase 413 (fase Test+UAT, nol surface produksi baru selain scope-fix JS yang sudah diaudit).

---

## Unregistered Flags

Tidak ada flag tidak terdaftar (unregistered_flag). Dua temuan dari REVIEW.md berstatus **Info** (IN-01, IN-02) dan sudah dipetakan ke threat register:
- IN-01 (`workers` tidak dikunci di `playwright.config.ts`) → mitigated secara operasional via SOP `--workers=1`; tidak memengaruhi keamanan.
- IN-02 (`waitForTimeout(2_000)` buffer post-StartExam) → buffer deterministik yang dibenarkan; bukan pola sleep buta untuk SignalR.

---

## Accepted Risks Log

| Risk ID | Threat | Alasan Diterima |
|---------|--------|-----------------|
| T-413-04 | Info Disclosure — SQLEXPRESS connection string | Integrated Security (Windows auth lokal); nol secret literal; konsisten fixture existing |
| T-413-E5 | Info Disclosure — Credentials DB di spec | Integrated Security (Windows auth lokal); nol secret literal; konsisten dbSnapshot existing |
| T-413-R3 | Info Disclosure — silent-skip Integration | Fail-fast via MigrateAsync throw; SQLEXPRESS wajib tersedia untuk suite 409/410/411 |

---

## Kesimpulan

Phase 413 (Test + UAT, milestone v32.5) **SECURED**: 7/7 ancaman tertutup, threats_open = 0.

- Isolasi DB dijamin via `HcPortalDB_Test_{Guid}` disposable (xUnit) + BACKUP/RESTORE lifecycle (e2e) + localhost-guard `dbSnapshot.ts`.
- Anti-tautologi (999.12) dijamin: action ASLI `AssessmentAdminController` + helper produksi `CMPController.IsParticipantRemoved` (`public static`, `CMPController.cs:2540`) + assert kolom DB nyata.
- Guard re-entry PRMV-03 terbukti lintas-fase (L1 lifecycle test, bukan hanya per-fase di 409).
- Scope-fix `monFlashRow` tidak menambah attack surface: hanya pemindahan ekspos function, XSS posture `.textContent` tidak berubah.
- SEED_JOURNAL Phase 413 dikonfirmasi status `cleaned` (RESTORE OK 2026-06-21).
- Push-readiness gate (carry IT migration=TRUE Phase 409 `01cd7dd0`) didokumentasikan; push = aksi terpisah.

_Phase: 413-test-uat_
_Auditor: Claude (gsd-secure-phase)_
_Tanggal: 2026-06-21_
_migration=FALSE (Phase 413); carry migration=TRUE Phase 409 hash `01cd7dd0`_
_NOT pushed_
