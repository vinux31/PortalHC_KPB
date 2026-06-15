# Phase 371: Sesi Online Tampil di Tab Input Records (visibility-only) - Context

**Gathered:** 2026-06-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Longgarkan filter `IsManualEntry` di `Views/Admin/Shared/_TrainingRecordsTab.cshtml:266` — AssessmentSessions online (IsManualEntry=false) ikut tampil per worker di expand Tab Input Records dengan badge pembeda "Assessment Online" (vs "Assessment Manual"/"Training Manual"). Visibility-only: TANPA tombol hapus untuk online (delete cascade = scope Phase 367). Empty-state copy disesuaikan. REQ: URG-03. Migration=false. **View-only — service TIDAK disentuh** (verified: `GetWorkersInSection` sudah load SEMUA AssessmentSessions per user tanpa filter status/IsManualEntry).

**Koordinasi:** selesaikan SEBELUM `/gsd-plan-phase 367` — 367 SC4 build aksi hapus di atas badge ini.

</domain>

<decisions>
## Implementation Decisions

### Cakupan status online
- **D-01:** SEMUA status sesi online tampil — Open ("Belum Mulai"), InProgress ("Sedang Dikerjakan"), Abandoned, Cancelled ("Dibatalkan"), Completed, Menunggu Penilaian — masing-masing dengan badge status. Alasan: 367 pasang tombol hapus di row ini; admin harus bisa LIHAT sesi stale (kasus Rino) untuk hapus nanti. Konsisten "tanpa batas" Phase 370.

### Aksi row online
- **D-02:** Tombol **Lihat hasil** (ikon mata, link ke halaman hasil exam) HANYA untuk sesi Completed/Menunggu Penilaian; sesi belum selesai TANPA tombol. TANPA aksi hapus/edit untuk semua row online (placeholder 367). Route hasil = research/planner verifikasi route admin eksisting (kandidat: CMP Results / Worker detail) — JANGAN bikin halaman baru.

### Label status row online
- **D-03:** Derivasi lengkap pola `DeriveUserStatus` (AssessmentAdminController, static): Lulus (hijau) / Tidak Lulus (merah) / Menunggu Penilaian (kuning) / Belum Mulai (abu) / Sedang Dikerjakan (biru) / Dibatalkan & Abandoned (abu gelap). Mapping existing IsPassed-only TIDAK boleh dipakai untuk online (salah label sesi belum selesai). Row manual existing TIDAK berubah mapping-nya.

### Empty-state
- **D-04:** Copy jadi **"Belum ada record untuk pekerja ini."** (drop kata "manual"). Tombol Tambah Training/Tambah Assessment tetap.

### Default wajar (diputuskan eksplisit)
- **D-05:** Sesi belum selesai: kolom Tanggal = `Schedule` tanpa prefix; kolom Detail = "—". Badge status sudah cukup membedakan.
- **D-06:** Counter rekap worker (`CompletedAssessments`/`CompletionDisplayText`) TIDAK disentuh — sudah include online sejak dulu (service `passedAssessmentLookup` tanpa filter IsManualEntry); pasca-371 justru konsisten dengan isi expand.
- **D-07:** Pasangan Pre-Post online (LinkedGroupId) tampil 2 row terpisah (per session) — tanpa grouping/penanda pasangan. 367 delete juga per-session.

### Claude's Discretion
- Wording title tombol Lihat hasil + ikon persis.
- Posisi badge "Assessment Online" mengikuti pola badge tipe existing (kolom Tipe).
- Urutan sort tetap `OrderByDescending(Date)` gabungan (existing).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Target & pola existing
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` :262-380 — region target: filter :266, anon row projection :263-268, empty-state :270-281, badge Tipe :309-318, status switch :322-333, kolom Aksi :338-369 (branch Training vs Assessment Manual + HTMX delete MAM-08)
- `Controllers/AssessmentAdminController.cs` ~:2800 — `DeriveUserStatus(status, completedAt, startedAt)` static pure — pola derivasi status untuk D-03 (catatan: row view butuh logika serupa; boleh panggil static ini langsung dari Razor atau duplikasi inline — planner putuskan)
- `Services/WorkerDataService.cs` :287-297, :322-323 — bukti service load semua sessions (JANGAN diubah)

### Koordinasi lintas fase
- `docs/superpowers/specs/2026-06-10-delete-input-records-full-cascade-design.md` — spec C; 367 SC4 build tombol hapus di atas badge 371 (anotasi pull-forward di ROADMAP Phase 367)
- `.planning/ROADMAP.md` §Phase 371 — SC lengkap

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `DeriveUserStatus` static (AssessmentAdminController ~:2800) — derivasi status sesi 6-way, testable
- Pola badge tipe + status switch existing di view (:309-333) — extend, jangan ganti
- `antiToken` + HTMX delete pattern MAM-08 — TIDAK dipakai untuk online (367)

### Established Patterns
- Tab Input Records = HTMX partial `ManageAssessmentTab_Training` (initial-state MAM-06: tanpa filter → empty-state "Pilih filter")
- Search nama/NIP (369 H1 fix) di level worker — tidak terpengaruh row online
- Filter tanggal (dateFrom/dateTo CMP-25) di service apply ke sessions `CompletedAt ?? Schedule` — row online ikut terfilter secara konsisten (tidak perlu perubahan)

### Integration Points
- Route `[Authorize(Roles="Admin, HC")]` `ManageAssessmentTab_Training` — tak berubah
- Phase 367 nanti menambah tombol hapus di branch online kolom Aksi (extension point)

</code_context>

<specifics>
## Specific Ideas

- Kasus referensi: Rino @Dev — Post Test OJT online lama tak terlihat di tab Input Records (asal-usul URG-03, brainstorm 2026-06-10).
- UAT lokal: data legacy existing punya sesi online >7 hari (pola D-06 Phase 370, zero seed).

</specifics>

<deferred>
## Deferred Ideas

- Tombol hapus sesi online → **Phase 367** (delete cascade engine + preview).
- Grouping visual pasangan Pre-Post di expand worker — kalau nanti dibutuhkan HC, fase UX terpisah.

</deferred>

---

*Phase: 371-sesi-online-tampil-di-tab-input-records-visibility-only*
*Context gathered: 2026-06-11*
