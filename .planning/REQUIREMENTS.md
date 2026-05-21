# Requirements: Milestone v17.0 Assessment Admin Power Tools

**Milestone:** v17.0
**Defined:** 2026-05-21
**Spec source:** `docs/superpowers/specs/2026-05-20-assessment-admin-power-tools-design.md` (commit `c37e55ef`)
**Research per phase:** `.planning/phases/320-assessment-export-per-peserta-excel/320-RESEARCH.md` + `.planning/phases/321-assessment-edit-jawaban-peserta/321-RESEARCH.md` (commit `f442220b`)

## v17.0 Requirements

### EXP — Export Per-Peserta Excel (Phase 320)

- [ ] **EXP-01**: Admin/HC dapat export Excel hasil assessment dari halaman ManageAssessment dengan struktur 1 sheet `"Summary"` (rename dari `"Results"` — breaking change) + N sheet per peserta
- [ ] **EXP-02**: Sheet "Summary" mempertahankan tabel ringkas existing (Name/NIP/Jumlah Soal/Status/Score/Result/Completed At) dengan header info assessment (Judul/Kategori/Jadwal/Durasi/Batas Kelulusan)
- [ ] **EXP-03**: Sheet per peserta (Variant A — Online assessment) memuat: header (Nama+NIP, Started/Completed At, Durasi Aktual, Tipe Assessment) + section "Analisis Elemen Teknis" (tabel ET) + PNG spider chart 500×500 (render via SkiaSharp, skip kalau elemen < 3) + section "Detail Jawaban" (MC/MA per soal, "Tidak dijawab" untuk soal tanpa response, Essay skip dengan note)
- [ ] **EXP-04**: Sheet per peserta (Variant B — Manual Entry session, `IsManualEntry == true`) memuat: header + section "Info Sertifikasi Manual" (Penyelenggara/Kota/SubKategori/Tipe Sertifikat + hyperlink ManualSertifikatUrl). Skip section ElemenTeknis/Chart/Detail Jawaban.
- [ ] **EXP-05**: Filter peserta yang dapat sheet = Status `Completed` + `Abandoned` only. Skip `InProgress`, `Not Started`, `Cancelled`
- [ ] **EXP-06**: Sheet name format `{NIP}_{FullName}` (NIP-first → collision-free karena NIP unique), truncate FullName dari belakang kalau total > 31 char, exclude Excel-invalid chars (`\ / ? * [ ] :`)
- [ ] **EXP-07**: Permission Admin + HC (`[Authorize(Roles = "Admin, HC")]`) — HC full sama Admin, no divisi restriction
- [ ] **EXP-08**: Performance: Export 50 peserta < 30 detik (PNG generate paralel via `Task.WhenAll` dengan `MaxDegreeOfParallelism = Environment.ProcessorCount`)

### EDIT — Edit Jawaban Peserta (Phase 321)

- [ ] **EDIT-01**: Admin/HC dapat edit jawaban MC/MA peserta via halaman dedicated `/AssessmentAdmin/EditPesertaAnswers/{sessionId}` dengan form per soal (radio MC / checkbox MA)
- [ ] **EDIT-02**: Edit jawaban hanya tersedia untuk session `Status == Completed` + bukan `IsManualEntry` + bukan Assessment Proton Tahun 3 (helper `IsEditable` mengatur akses GET, POST, dan UI dropdown)
- [ ] **EDIT-03**: Edit jawaban auto-recompute Score, IsPassed, dan `SessionElemenTeknisScores` via `GradingService.RegradeAfterEditAsync` (DELETE existing ET + recompute + ExecuteUpdateAsync dengan status guard `WHERE Status == "Completed"`)
- [ ] **EDIT-04**: Pass↔Fail flip auto-cascade: Pass→Fail cabut `NomorSertifikat` + `ValidUntil` + update TrainingRecord status `Failed`. Fail→Pass generate NomorSertifikat baru (retry 3x via `CertNumberHelper`, hanya kalau `GenerateCertificate && AssessmentType != "PreTest"`) + upsert TrainingRecord status `Passed`
- [ ] **EDIT-05**: Edit per question wajib reason: dropdown preset (SoalSalah / KunciSalah / BugSistem / PermintaanPeserta / Lainnya). Kalau pilih `Lainnya` → field teks bebas wajib diisi (validasi client + server)
- [ ] **EDIT-06**: Audit dual-write per save: `AuditLog` generic (`ActionType="EditAssessmentAnswer"`) + tabel baru `AssessmentEditLog` granular per question (snapshot QuestionText + OldAnswerJson/Text + NewAnswerJson/Text + Old/New Score+IsPassed + Actor info + ReasonCode/Text + EditedAt)
- [ ] **EDIT-07**: Concurrency token via `AssessmentSession.UpdatedAt` — hidden field di form, compare di POST. Stale → reject dengan TempData error "Sesi sudah diubah admin lain. Refresh halaman."
- [ ] **EDIT-08**: Transaction scope membungkus seluruh edit save flow (update PackageUserResponses + insert AssessmentEditLog + RegradeAfterEditAsync + cascade cert/TR + insert AuditLog). Rollback total kalau exception
- [ ] **EDIT-09**: SignalR signal baru `workerAnswerEdited` ke group `monitor-{batchKey}` dengan payload `{ sessionId, workerName, oldScore, newScore, oldIsPassed, newIsPassed, actorName, actorRole }` → monitor row score+result update real-time + toast notification
- [ ] **EDIT-10**: Dry-run endpoint `POST PreviewEditScore` (terima draft answers, return `{ oldScore, newScore, oldIsPassed, newIsPassed, hasCert, willGenerateCert }`) → frontend deteksi flip Pass↔Fail → modal konfirmasi sebelum submit
- [ ] **EDIT-11**: Activity Log modal existing dapat tab baru `"Edit History"` (lazy-load partial) menampilkan `AssessmentEditLog` entries filtered by SessionId, sort EditedAt DESC, format `[timestamp] Soal #N: "QuestionText" — [Old] → [New] oleh ActorRole (ActorName). Alasan: ReasonCode`
- [ ] **EDIT-12**: Per-user table action column di `AssessmentMonitoringDetail` pakai layout hybrid: `View Results` + `Activity Log` 🕐 tetap inline, sisanya (`Edit Jawaban`, `Reset`, `Akhiri Ujian`, `Reshuffle`) pindah ke dropdown ⋮ dengan ARIA label + Bootstrap `dropdown-menu-end` + auto-flip mobile. Item `Edit Jawaban` conditional render hanya saat `IsEditable(session) == true`
- [ ] **EDIT-13**: Migration `AddAssessmentEditLogs` membuat tabel `AssessmentEditLogs` dengan index `IX_AssessmentEditLogs_SessionId_EditedAt` `(AssessmentSessionId, EditedAt DESC)`. Test apply + rollback lokal sebelum commit (per DEV_WORKFLOW §4)

## Future Requirements (Deferred ke milestone berikutnya)

| ID | Item | Alasan defer |
|----|------|---------------|
| FUTURE-KUNCI-01 | Fix Kunci Soal Global (cascade re-grade semua session yang pakai soal X) | Defer per spec Q11 — aktifkan saat kasus muncul |
| FUTURE-NOTIFY-01 | Notifikasi email/in-app ke peserta saat hasil diubah | Defer per spec Q10 (saat ini total silent) |
| FUTURE-APPROVAL-01 | Workflow approval 2-step (HC submit → Admin approve) | Defer per spec Section 8 |
| FUTURE-BULK-01 | Bulk edit grid (Excel-like) untuk banyak peserta sekaligus | Defer per spec Section 8 |
| FUTURE-UNDO-01 | Undo/redo edit jawaban | Defer per spec Section 8 |

## Out of Scope (eksplisit dikecualikan)

- **Edit jawaban Essay** — Sudah ditangani halaman Penilaian Essay existing dari Phase 298-05 (spec Section 8 + Q13)
- **Edit session non-Completed (`InProgress`/`Abandoned`/`Cancelled`)** — Status guard `IsEditable` blokir (spec Q6b)
- **Edit session Assessment Proton Tahun 3** — Interview manual, tidak ada jawaban MC/MA tersimpan
- **Edit session `IsManualEntry == true`** — Manual entry tidak ada jawaban yang bisa di-edit
- **HC area/divisi restriction** — HC permission full sama Admin (spec Q8)
- **Diff/changelog export** — Re-export setelah edit hanya refresh dari DB latest, tanpa highlight diff (spec Section 8)
- **Visibility ke peserta** — Total hidden, log internal saja (spec Q7)

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| EXP-01 | 320 | Pending plan |
| EXP-02 | 320 | Pending plan |
| EXP-03 | 320 | Pending plan |
| EXP-04 | 320 | Pending plan |
| EXP-05 | 320 | Pending plan |
| EXP-06 | 320 | Pending plan |
| EXP-07 | 320 | Pending plan |
| EXP-08 | 320 | Pending plan |
| EDIT-01 | 321 | Pending plan |
| EDIT-02 | 321 | Pending plan |
| EDIT-03 | 321 | Pending plan |
| EDIT-04 | 321 | Pending plan |
| EDIT-05 | 321 | Pending plan |
| EDIT-06 | 321 | Pending plan |
| EDIT-07 | 321 | Pending plan |
| EDIT-08 | 321 | Pending plan |
| EDIT-09 | 321 | Pending plan |
| EDIT-10 | 321 | Pending plan |
| EDIT-11 | 321 | Pending plan |
| EDIT-12 | 321 | Pending plan |
| EDIT-13 | 321 | Pending plan |

**Coverage:** 21/21 mapped ✓ — 0 orphans, 0 duplicates

---

**Last updated:** 2026-05-21
