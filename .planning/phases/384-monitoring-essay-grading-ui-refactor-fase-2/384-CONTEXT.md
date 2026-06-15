# Phase 384: Monitoring Essay Grading UI Refactor (Fase 2) - Context

**Gathered:** 2026-06-15
**Status:** Ready for planning
**Source:** discuss-phase interactive 2026-06-15 (4 area, 4 keputusan inti). Sibling Fase 1 = Phase 383 (correctness hotfix).

<domain>
## Phase Boundary

Refactor UI penilaian essay di halaman Monitoring (`Views/Admin/AssessmentMonitoringDetail.cshtml`). Ganti **hanya** blok essay inline yang panjang & numpuk per-worker (`:381-481`) dengan **tabel list worker ringkas** (status + jumlah essay belum dinilai) + tombol **"Tinjau Essay"** yang membuka **page penilaian essay per-worker terpisah** (GET action baru). Tabel sesi utama di atas (~`:300-378`) TIDAK disentuh.

**Backend TIDAK diubah.** Reuse POST `SubmitEssayScore` (@3458) + `FinalizeEssayGrading` (@3499) + `EssayGradingItemViewModel`. Yang ditambah/diubah: view section (tabel worker-list) + 1 GET action baru + 1 view per-worker baru + (mungkin) ViewModel worker-list. **0 migration** (backend endpoints + DB tak berubah).

UI/UX-only. Tujuan: HC menilai essay per-worker dengan alur rapi (1 worker = 1 page), bukan scroll panjang kartu numpuk.

</domain>

<decisions>
## Implementation Decisions (LOCKED)

### Tabel list worker (Monitoring Detail)

- **D-01 Scope ganti:** Ganti HANYA section essay `:381-481`. Tabel sesi utama (atas) + sisanya tak disentuh. Pertahankan guard `essayGradingMap.Any()` (~385) — section disembunyikan bila tak ada worker beressay.
- **D-02 Isi tabel:** HANYA worker dengan `HasManualGrading == true` (punya essay). Worker murni MC TIDAK muncul di tabel ini. Kolom: Worker (FullName) + NIP, jumlah essay belum dinilai (`EssayPendingCount`), badge status, tombol "Tinjau Essay" (kanan).
- **D-03 Urutan:** Urut by **NIP / nama (alfabet)**, BUKAN pending-first. (User memilih konsistensi urutan vs prioritas pending.)
- **D-04 Status badge — 3 state:**
  - 🟡 `"{N} belum dinilai"` — `EssayPendingCount > 0`
  - 🔵 `"Siap difinalisasi"` — `EssayPendingCount == 0` && BELUM finalized
  - 🟢 `"Selesai"` — finalized (`Status == AssessmentConstants.AssessmentStatus.Completed && !string.IsNullOrEmpty(NomorSertifikat)`) — REUSE gate Phase 310 D-02 (LOCKED, jangan ubah kriteria).
- **D-05 Empty state:** Tak ada worker beressay → section essay disembunyikan total (guard existing dipertahankan).

### Navigasi & page per-worker

- **D-06 Tombol "Tinjau Essay":** Tiap baris worker → navigasi ke **page penilaian essay per-worker terpisah** (GET action baru). Route shape = Claude's Discretion (lihat bawah).
- **D-07 Layout page per-worker:** REUSE kartu essay existing (markup `AssessmentMonitoringDetail.cshtml:~407-446` — header soal + badge dinilai, gambar soal via `_QuestionImage`, jawaban pekerja, rubrik collapse, input skor `min=0 max=ScoreValue`, "Simpan Skor", section "Selesaikan Penilaian"). Tambah header identitas worker (nama + NIP) di atas + tombol **"Kembali"** ke Monitoring Detail.
- **D-08 Simpan Skor = AJAX in-place:** REUSE handler JS existing `btn-save-essay-score` + `btn-finalize-grading` (AJAX, tak reload page). Catatan: handler ini saat ini **inline `<script>` di dalam `AssessmentMonitoringDetail.cshtml`** (bukan file js terpisah) → planner putuskan extract ke shared script/partial vs duplikat (Discretion).
- **D-09 Return flow:** Setelah "Selesaikan Penilaian" → **TETAP di page per-worker**, update in-place tampilkan state "Selesai" (badge hijau, input/tombol jadi read-only). TIDAK auto-redirect. User klik "Kembali" manual. Status di tabel Monitoring Detail ter-update saat reload/poll berikutnya.

### Finalized & guard

- **D-10 Finalized = READ-ONLY (BUKAN re-grade):** Bila session sudah finalized (gate D-04 🟢), buka page per-worker dalam **mode read-only** — skor tampil, input `disabled`, tombol "Simpan Skor"/"Selesaikan Penilaian" disabled/hidden. **Backend TAK diubah** — read-only menghindari panggil `SubmitEssayScore`/`FinalizeEssayGrading` di sesi Completed (tak perlu cek apakah backend menolaknya). Tombol "Tinjau Essay" tetap MUNCUL setelah finalized (untuk lihat hasil), tapi page-nya read-only.

### Test

- **D-11 Playwright e2e (UIG-04):** Razor dynamic → Playwright runtime WAJIB (pelajaran Phase 354 — grep+build tak cukup). Flow: tabel worker-list render → klik "Tinjau Essay" → navigasi ke page per-worker → beri skor (Simpan Skor AJAX) + "Selesaikan Penilaian" round-trip sukses → state "Selesai" in-place. Local `dotnet run` → `http://localhost:5277` (CLAUDE.md Develop Workflow). Login admin lokal: `Authentication__UseActiveDirectory=false` saat `dotnet run` (pelajaran Phase 355).

### Claude's Discretion
- **Route/URL shape GET action baru** — mis. `EssayGrading?sessionId={id}` atau `/Admin/AssessmentEssayGrading/{id}`. Pilih yang konsisten dgn konvensi controller existing.
- **Perlu ViewModel worker-list baru atau tidak** — `MonitoringSessionViewModel` (`:48-68`) SUDAH punya `UserFullName`, `UserNIP`, `HasManualGrading`, `EssayPendingCount`, `Status`, `NomorSertifikat`. Tabel worker-list kemungkinan cukup pakai `Model.Sessions.Where(s => s.HasManualGrading)` tanpa ViewModel baru. Konfirmasi saat planning.
- **Cara page per-worker memuat `List<EssayGradingItemViewModel>` untuk 1 session** — clone logic map-builder (`AssessmentAdminController.cs:~3412-3433`) untuk single session.
- **Authorization page baru** — samakan dengan `AssessmentMonitoringDetail`/`SubmitEssayScore` (Admin/HC). Verifikasi attribute.
- **Extract inline JS handler ke shared script/partial vs duplikat** (lihat D-08).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Design + scope
- `docs/superpowers/specs/2026-06-15-essay-grading-correctness-design.md` — milestone v30.0 design; §D-05 split 2 phase (Fase 2 = UI refactor Monitoring)
- `.planning/REQUIREMENTS.md` — UIG-01..04 (`:31-34`)
- `.planning/ROADMAP.md` — Phase 384 section (goal + 4 success criteria + files list)
- `.planning/phases/383-essay-grading-correctness-test-fase-1/383-CONTEXT.md` — sibling Fase 1; carried-forward: essay Benar = `EssayScore > 0` (D-02), label pending = "Menunggu Penilaian" via `AssessmentConstants.AssessmentStatus.PendingGrading` (D-06)

### Code (surface yang disentuh)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — essay section `:381-481` (GANTI jadi tabel worker-list); kartu essay markup `~407-446` (REUSE di page baru); guard `essayGradingMap.Any()` `~385`; finalize gate `~451-453`; handler JS `btn-save-essay-score`/`btn-finalize-grading` = inline `<script>` di view ini
- `Controllers/AssessmentAdminController.cs` — GET `AssessmentMonitoringDetail(title,category,scheduleDate,assessmentType)` `@3273`; `EssayGradingMap` builder `@3412-3433` (clone untuk single session di GET action baru); POST `SubmitEssayScore(sessionId,questionId,score)` `@3458` (REUSE, jangan ubah); POST `FinalizeEssayGrading(sessionId)` `@3499` (REUSE, jangan ubah)
- `Models/AssessmentMonitoringViewModel.cs` — `MonitoringSessionViewModel` `:48-68` (UserFullName/UserNIP/HasManualGrading/EssayPendingCount/Status/NomorSertifikat); `EssayGradingItemViewModel` `:73-86` (REUSE)
- `Views/Shared/_QuestionImage.cshtml` (atau partial setara dipakai `@Html.PartialAsync("_QuestionImage", ...)` di `:416`) — reuse gambar soal di page baru

### Test
- `tests/e2e/assessment.spec.ts` — suite e2e existing (mengandung selector essay/monitoring); pola FLOW untuk tambah test UIG-04
- Phase 354 lesson: Razor dynamic → Playwright runtime wajib. Phase 355 lesson: `Authentication__UseActiveDirectory=false` saat `dotnet run` untuk login admin lokal.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Kartu essay grading** (`AssessmentMonitoringDetail.cshtml:407-446`) — markup lengkap (jawaban pekerja, rubrik collapse, gambar soal, input skor, Simpan Skor, badge dinilai). Clone ke page per-worker.
- **`EssayGradingItemViewModel`** (`:73-86`) — sudah punya QuestionId/DisplayNumber/QuestionText/Rubrik/TextAnswer/EssayScore/ScoreValue/ImagePath/ImageAlt. Cukup untuk page per-worker.
- **`MonitoringSessionViewModel`** (`:48-68`) — cukup untuk baris tabel worker-list (kemungkinan tak perlu ViewModel baru).
- **EssayGradingMap builder** (`AssessmentAdminController.cs:3412-3433`) — logic load essay items per session; clone untuk single session.
- **POST endpoints** `SubmitEssayScore`/`FinalizeEssayGrading` — dipakai apa adanya (backend unchanged, kontrak sama).
- **Finalize gate Phase 310 D-02** (`:451-453`) — `Status==Completed && NomorSertifikat!=null`. Reuse untuk badge 🟢 "Selesai" + mode read-only.

### Established Patterns
- JS handler essay grading = inline `<script>` di `AssessmentMonitoringDetail.cshtml` (AJAX + antiforgery via `#antiforgeryForm` `:484`). Page baru harus punya akses handler + token yang sama.
- Tabel monitoring existing (sessions) = pola tabel Bootstrap + dropdown aksi per-baris → pola visual untuk tabel worker-list baru.

### Integration Points
- GET action baru di `AssessmentAdminController` (sebelah `AssessmentMonitoringDetail`).
- Tombol "Tinjau Essay" link/navigasi dari tabel worker-list → GET action baru (sessionId).
- "Kembali" dari page per-worker → balik ke `AssessmentMonitoringDetail` (perlu carry title/category/scheduleDate atau referer).

</code_context>

<specifics>
## Specific Ideas

- 1 worker = 1 page (bukan scroll kartu numpuk). HC pilih worker dari tabel → fokus nilai essay worker itu → Kembali.
- Setelah finalize, page tetap bisa dibuka tapi read-only (lihat hasil, tak bisa edit).
- Verifikasi per CLAUDE.md: `dotnet build` + Playwright e2e (localhost:5277). Tak ada edit Dev/Prod.

</specifics>

<deferred>
## Deferred Ideas

- Alur grading berantai (auto-buka worker pending berikutnya setelah selesai 1) — DITOLAK untuk fase ini (user pilih "tetap di page"). Bisa jadi enhancement future bila diminta.
- Re-grade setelah finalized (edit skor sesi Completed) — DITOLAK (user pilih read-only). Butuh perubahan backend → di luar scope "backend unchanged".

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` (area: database) — cleanup data test lokal pasca Phase 367. Di luar scope UI refactor essay. Tidak di-fold.

</deferred>

---

*Phase: 384-monitoring-essay-grading-ui-refactor-fase-2*
*Context gathered: 2026-06-15 via discuss-phase*
