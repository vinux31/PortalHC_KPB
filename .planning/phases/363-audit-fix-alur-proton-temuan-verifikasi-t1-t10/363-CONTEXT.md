# Phase 363: Audit Fix Alur PROTON (T1-T10) - Context

**Gathered:** 2026-06-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Tutup 10 temuan verifikasi adversarial alur PROTON (`363-FINDINGS.md`) — tiap item **fix ATAU by-design dengan alasan tercatat**. Migration=false (logic/notif/UI saja, tanpa schema).

**Scout ulang 2026-06-11 (pasca 360/361/362 shipped):** 10/10 temuan masih valid. Phase 362 hanya sentuh `ExportHistoriProton` (commit d9aff41c) — nol konflik. Phase 360 sudah isi exempt hook `isExemptFromCrossYear` (`CoachMappingController.cs:535-537`, Origin="Bypass") dan pasang 3 hook bypass di `GradingService.cs` (`:314/:496/:549`) — fix T6 ortogonal, tidak tabrakan.

**TERKUNCI (jangan diubah oleh fix manapun):**
- Semantik approval v25.0 A-2: 1 approver L4 cukup (SrSpv ATAU SH), co-sign opsional, HC BUKAN approver deliverable.
- Notif via `UserNotification`/`NotificationService.SendAsync` — tabel `ProtonNotification` DORMAN, jangan dibangunkan.
- Year gate hard-block 359 D-05 + exempt Origin="Bypass" 360 D-04/D-06.
</domain>

<decisions>
## Implementation Decisions

### Anti-drift T1/T2/T7 — helper bersama
- **D-01 (T1/T2/T7):** **Extract helper bersama** di CDPController (atau service): `approve-core` (set approval role + allApproved check + notif HC `COACH_ALL_COMPLETE` + race-guard reload-fresh D-10) dan `reject-core` (full chain reset: Status="Rejected" + SrSpv/SH/HC→Pending + null approver ID/timestamp) — dipanggil dari KEDUA endpoint tiap pasangan (`ApproveDeliverable`/`ApproveFromProgress`, `RejectDeliverable`/`RejectFromProgress`). Drift mati permanen. Wajib test paritas (xUnit per pasangan: hasil state identik untuk input sama).
- **D-02 (T1):** `ApproveFromProgress` lewat helper otomatis dapat: allApproved check (pola `:908-931`) + `CreateHCNotificationAsync` `COACH_ALL_COMPLETE` (pola `:968`, `:1127-1136`). Paritas penuh perilaku post-approval.
- **D-03 (T2):** Reset `HCApprovalStatus`→"Pending" + null `HCReviewedById/At` di **reject DAN kedua jalur resubmit** (`UploadEvidence` wasRejected `:1297-1308` + `SubmitEvidenceWithCoaching` isResubmit `:2270-2279`) — belt-and-braces, rejection lama pre-fix tidak bawa HC review basi.
- **D-04 (T7):** Race-guard D-10 (reload-fresh AsNoTracking + re-validasi) masuk approve-core — kedua endpoint terlindungi.

### T3 — gate jalur reaktivasi
- **D-05:** **Gate reaktivasi.** Jalur reaktivasi assignment (`CoachMappingController.cs:597-606`) jalankan year gate sama dengan assignment baru: `IsPrevYearPassedAsync` + exempt. Blocked → pesan error pola hard-block 359 D-05. Menutup escape: assignment Tahun N lama nonaktif tak bisa direaktivasi tanpa Tahun N-1 lulus.
- **D-06:** **Exempt stempel permanen** — selain cek `isExemptFromCrossYear` existing (`:535-537`), reaktivasi exempt bila **assignment yang mau direaktivasi punya `Origin="Bypass"`**. Konsisten makna stempel permanen 360 D-04; worker hasil bypass tidak false-block saat re-assign.

### T4 — penanda assignment nonaktif: surface warning
- **D-07:** `EnsureAsync` **tetap strict IsActive** (`ProtonCompletionService.cs:39-43`) — assignment nonaktif = keputusan sadar admin, JANGAN auto-terbit penanda.
- **D-08:** Saat miss (lulus exam tapi tidak ada assignment aktif): (a) tulis **AuditLogs** entry (actor=system/grading, deskripsi sebut coachee+track+session), (b) kirim **notif lonceng ke user role HC** via `NotificationService` (pola `COACH_ALL_COMPLETE` broadcast; tipe template baru boleh), isi arahkan ke `BackfillProtonPenanda` sebagai jalur tambal resmi. Warn log existing dipertahankan.

### T5 — tampilkan "Belum Mulai"
- **D-09:** Ubah query `HistoriProton` (`CDPController.cs:3149-3180`): coachee dengan **mapping aktif TANPA `ProtonTrackAssignment`** ikut tampil, status "Belum Mulai". Ternary `:3250` jadi 3 cabang. Badge `HistoriProton.cshtml:152-155` + opsi filter `:79` jadi berfungsi sesuai desain awal. (Bukan hapus dead branch.)

### T6 — ValidUntil paritas
- **D-10:** **Buang hardcode +3 tahun** di `RegradeAfterEditAsync` Fail→Pass (`GradingService.cs:516`): regrade berhenti set `ValidUntil` — nilai ikut setup sesi HC, paritas penuh dengan jalur pass normal (`GradeAndCompleteAsync:285-287` yang hanya set NomorSertifikat). SetProperty ValidUntil dihapus dari blok `:520-521`. Hook bypass 360 (`:314/:496/:549`) tidak disentuh.

### T8 — evidence history append
- **D-11:** `SubmitEvidenceWithCoaching` (`:2284-2292`) **append path lama ke `EvidencePathHistory`** sebelum overwrite — pola persis `UploadEvidence:1280-1286`. **File fisik lama TETAP disimpan** (konsisten kebijakan E10 "KEEP orphan evidence" — kedua jalur memang tidak hapus file; JANGAN tambah File.Delete).

### T9 — guard log-warn
- **D-12:** Saat prev-track resolve null padahal `requestedTrack.Urutan > 1` (Urutan tidak kontigu): **log warning eksplisit** di kedua titik (`AssessmentAdminController.cs:1348-1352` + `CoachMappingController.cs:506-509`). Gate tetap jalan seperti sekarang (null prev untuk Urutan=1 tetap sah = Tahun 1). Tanpa throw/blok.

### T10 — by-design
- **D-13:** `BackfillProtonPenanda` tanpa year-gate = **by-design**. Backfill menambal data historis pre-358 yang lulus exam sungguhan; year gate baru bermakna setelah penanda lengkap. Aksi: komentar kode di method + catatan di FINDINGS. Nol perubahan logic.

### Claude's Discretion
- Bentuk helper D-01: private method CDPController vs service class terpisah — planner pilih (perhatikan testability; pola ProtonCompletionService/ProtonBypassService tersedia).
- Nama tipe notif T4 (`PROTON_PENANDA_MISS` atau sejenis) + template title/body — ikuti pola `NotificationService._templates`.
- Pembagian plan/wave + urutan fix (file-overlap: CDPController T1/T2/T5/T7/T8; CoachMapping T3/T9; GradingService T6; ProtonCompletionService+AssessmentAdmin T4/T9/T10).
- Strategi test: xUnit paritas per pasangan endpoint + [Fact] gate reaktivasi + regresi 211 existing; e2e/UAT scope.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Temuan (otoritas)
- `.planning/phases/363-audit-fix-alur-proton-temuan-verifikasi-t1-t10/363-FINDINGS.md` — detail 10 temuan + evidence file:line + konteks pendukung (4 jalur tulis penanda, pasangan endpoint duplikat, semantik A-2).

### Keputusan terkait yang TERKUNCI
- `.planning/phases/359-gate-berurutan-cleanup-a/359-CONTEXT.md` — D-03 `IsPrevYearPassedAsync`, D-05 hard-block (pesan error pola untuk T3).
- `.planning/phases/360-bypass-backend-b/360-CONTEXT.md` — D-04 stempel permanen Origin="Bypass" (dasar T3 exempt D-06), D-06 dua titik exempt.
- `docs/superpowers/specs/2026-06-09-proton-completion-logic-design.md` — A-2 semantik approval (JANGAN diubah), A-M10 resolusi backfill.

### Workflow
- `CLAUDE.md` — Develop Workflow (build + test + UAT @5277 AD=false); migration=false.
- `docs/SEED_WORKFLOW.md` — kalau UAT butuh seed state (reject/approve chain), snapshot + journal + restore.
</canonical_refs>

<code_context>
## Existing Code Insights (scout 2026-06-11, line numbers terkini)

### Lokasi temuan terverifikasi
- **T1:** `CDPController.cs:1939-2013` (`ApproveFromProgress`) vs `:818-973` (`ApproveDeliverable`; allApproved `:908-931`, notif `:968` + `:1127-1136`).
- **T2:** `:2018-2091` (`RejectFromProgress`, blok set `:2055-2069`) vs `:978-1080` (`RejectDeliverable`, reset `:1028-1037`); resubmit `:1297-1308` + `:2270-2279` (keduanya TIDAK reset HC).
- **T3:** `CoachMappingController.cs:517-520` (query branch-1 tanpa IsActive), `:535-537` (exempt 360), `:542-543` (year gate), `:597-606` (reaktivasi TANPA gate).
- **T4:** `ProtonCompletionService.cs:38-43` (strict IsActive → warn+false); kontras `AssessmentAdminController.cs:3933-3938` (backfill resolve tanpa IsActive).
- **T5:** `CDPController.cs:3149-3180` (query), `:3250` (ternary 2 cabang); `Views/CDP/HistoriProton.cshtml:79` (filter), `:152-155` (badge unreachable).
- **T6:** `GradingService.cs:269-301` (normal pass, NomorSertifikat saja `:285-287`), `:499-550` (regrade Fail→Pass; hardcode ValidUntil `:516`, SetProperty `:520-521`).
- **T7:** race-guard D-10 `:882-901` (hanya ApproveDeliverable); ApproveFromProgress load sekali `:1955-1956` tanpa re-check.
- **T8:** `:1280-1286` (append history UploadEvidence) vs `:2290` (overwrite tanpa append). Kedua jalur tidak hapus file fisik (by-design keep).
- **T9:** `CoachMappingController.cs:506-509` + `AssessmentAdminController.cs:1348-1352` (Urutan-based, null→Tahun-1).
- **T10:** `AssessmentAdminController.cs:3915-4007` (BackfillProtonPenanda; enforce 100% `:3954-3958`, tanpa prev-year check).

### Titik 360 yang TIDAK boleh tersenggol
- `GradingService.cs:310-315` (EnsureAsync + MarkPendingReadyIfAnyAsync), `:492-497` (RevertPendingToMenungguAsync), `:543-550` (regrade pass hooks) — fix T6 hanya sentuh SetProperty blok sertifikat.
- `AssessmentAdminController.cs:1372-1379` (gate + exempt Bypass) — T9 hanya tambah log di resolusi `:1348-1352`.
- `CoachMappingController.cs:535-543` (exempt + gate existing) — T3 menambah gate di jalur reaktivasi `:597-606`, bukan mengubah yang ada.

### Reusable
- `ProtonCompletionService.IsPrevYearPassedAsync` (`:107`) — dipakai gate reaktivasi T3.
- `NotificationService._templates` + `SendAsync` — notif T4.
- `AuditLogService.LogAsync` — audit T4.
- Pola test: `HcPortal.Tests` 211/211 hijau; real-SQL disposable fixture (Phase 344 TEST-05) + [Fact] pola 358/359/360.

### Risiko planner
- CDPController.cs ~3000+ baris, 5 temuan menyentuhnya — wave/plan ordering hati-hati supaya diff tidak saling tabrak; helper extraction (D-01) duluan, fix lain menyusul di atasnya.
- Test paritas helper: pastikan ApproveDeliverable existing behavior ter-pin SEBELUM refactor (snapshot test dulu → refactor → hijau).
</code_context>

<specifics>
## Specific Ideas

- Akar T1/T2/T7/T8 = drift endpoint duplikat — keputusan helper bersama (D-01) sengaja membunuh akar, bukan gejala. Pasangan HCReview (HCReviewDeliverable/HCReviewFromProgress) TIDAK ditemukan drift — jangan ikut direfactor tanpa alasan (scope creep).
- T4: notif HC harus actionable — sebut nama coachee, track, session, dan arahkan ke backfill.
- T5: "Belum Mulai" = coachee ber-mapping aktif tanpa assignment track manapun (definisi presisi — bukan semua user).
</specifics>

<deferred>
## Deferred Ideas

- Hapus file fisik evidence lama saat re-upload (T8 opsi b) — TIDAK diambil; kebijakan keep-evidence E10 dipertahankan. Kalau disk jadi isu, fase tersendiri.
- Konsolidasi pasangan HCReview + jalur upload jadi helper juga — hanya kalau drift ditemukan kelak.
- Year-gate di backfill (T10 opsi b) — ditolak by-design.

### Reviewed Todos (not folded)
None — `todo match-phase 363` kosong.
</deferred>

---

*Phase: 363-audit-fix-alur-proton-temuan-verifikasi-t1-t10*
*Context gathered: 2026-06-11*
