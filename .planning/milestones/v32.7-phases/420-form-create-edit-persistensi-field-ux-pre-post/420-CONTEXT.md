# Phase 420: Form Create/Edit — Persistensi Field + UX Pre-Post - Context

**Gathered:** 2026-06-22
**Status:** Ready for planning

<domain>
## Phase Boundary

HC dapat membuat & mengedit assessment (mode **Standard** maupun **Pre-Post**) lewat form, dengan SEMUA setelan yang diisi benar-benar tersimpan (Acak Soal/Pilihan, Ujian Ulang, ValidUntil), sesi yang sudah Completed terlindung dari perubahan metadata, sesi entry-manual diarahkan ke form yang benar, dan tata-letak mode Pre-Post jelas scope-nya tanpa field duplikat tersembunyi.

**In-scope:** FORM-01..11 (persistensi field Create/Edit + UX redesign form Pre-Post). **migration=FALSE** (binding form + view + controller; tak ada schema baru).

**Out-of-scope (fase lain):** logika retake (421), SamePackage sync/toggle backend (422), aturan terbit cert (423), grading/gating Pre→Post (424). Fase 420 HANYA form/binding/layout — perubahan perilaku ujian/grading di fase berikutnya.
</domain>

<decisions>
## Implementation Decisions

### D-01 — Fix bug "Acak Soal/Pilihan reset OFF tiap Edit" (E-01, FORM-01)
**Render toggle Acak Soal & Acak Pilihan di `EditAssessment.cshtml`**, terisi dari `Model` (nilai tersimpan). Pertahankan write di POST Edit. Hasil: shuffle dapat diatur konsisten via **3 jalur** (Create + Edit + ManagePackages/`UpdateShuffleSettings`) tanpa saling menimpa secara diam-diam.
- Catatan integrasi: pastikan Edit & `UpdateShuffleSettings` tidak race — nilai yang dirender di Edit harus mencerminkan state terkini; saat HC submit Edit, nilai checkbox = sumber kebenaran untuk sesi + sibling.

### D-02 — Penyajian scope setelan di mode Pre-Post (FORM-08)
**Pisah jadi dua sub-kartu** di Step 3 saat mode Pre-Post aktif:
- **"Setelan Post-Test"** → Nilai Lulus (PassPercentage), Sertifikat (GenerateCertificate + ValidUntil), Ujian Ulang (lihat D-03).
- **"Setelan Bersama Pre & Post"** → Acak Soal/Pilihan, Izinkan Review Jawaban, Token.
- Mode **Standard**: layout setelan tetap seperti sekarang (tak ada pemisahan).

### D-03 — Retake & Nilai-Lulus untuk Pre baseline (FORM-11)
Mode Pre-Post → **kontrol Ujian Ulang DISEMBUNYIKAN** (retake hanya relevan untuk Post); **PassPercentage untuk Post**; **Pre = baseline murni** (tanpa lulus/gagal/retake). Baris Status/PassPercentage yang sebelumnya timpang dirapikan. Mode Standard: kontrol retake/pass tetap tampil seperti sekarang.

### D-04 — Letak SamePackage + input standard tersembunyi (FORM-07, FORM-09)
- **SamePackage** pindah ke **header section Pre-Post** (dekat pemilih Tipe Assessment / di atas kartu Pre & Post), bukan terkubur di kartu Post.
- Input jadwal/durasi/EWCD **standard TIDAK ikut ter-POST** saat mode Pre-Post (hapus dari payload yang dikirim, bukan sekadar `d-none`). Saat Standard, input Pre/Post yang tidak terpakai juga tidak dikirim.

### Claude's Discretion
- **FORM-10 (rename `AssessmentTypeInput`):** rename parameter/penanda internal agar tidak rancu dengan kolom DB `AssessmentType` (mis. `CreationMode` Standard/PrePostTest); label UI "Tipe Assessment" boleh tetap; perbarui XML-doc `AssessmentSession.AssessmentType` yang usang. Pendekatan teknis bebas asal binding tidak putus.
- **FORM-05 (lock Completed):** blok perubahan metadata bila sesi target — atau, untuk Pre-Post, **grup pasangannya** — sudah `Completed` (group-aware default). Planner tentukan presisi & posisi guard (idealnya sebelum cabang Pre-Post di POST Edit; lihat audit E-04).
- **FORM-02/03/04/06 (perbaikan persistensi/redirect):** ikuti pola existing — FORM-02 mirror penyalinan eksplisit pada bulk-add (`AssessmentAdminController.cs:2184-2186`); FORM-06 mirror filter `IsManualEntry` di `TrainingAdminController` EditManualAssessment GET (`:994`).
- **Backward-compat WAJIB:** mode **Standard tidak berubah perilaku**; hanya mode Pre-Post mendapat layout baru (sub-kartu + SamePackage header + sembunyikan retake).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Audit sumber (temuan + bukti file:line)
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` §5.2.4 (E-01, FLD-5.2-08, E-03, E-05) — konflik antar-field & bind-but-drop
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` §5.2.5 (FORM-PP-01..07) — audit khusus form Create mode Pre-Post (letak/scope/duplikat/naming)
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` §5.3 (E-04 lock Completed, E-08 redirect manual)

### Requirements & roadmap
- `.planning/REQUIREMENTS.md` — FORM-01..11 (traceability Phase 420)
- `.planning/ROADMAP.md` — Phase 420 (goal + 5 success criteria observable)

### Out-of-scope (jangan duplikasi)
- Section/Scoped-Shuffle/Opsi-Dinamis = v32.6 branch `main` (fase 415-419) — overlap layout form; rekonsiliasi saat merge, BUKAN dikerjakan di sini.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`Views/Admin/CreateAssessment.cshtml`** (2086 baris) — wizard 4-langkah. Step 3 Settings: Group A "Jadwal & Waktu" (`standard-jadwal-section` :382-425 + hidden combiner Schedule/EWCD :424-425; `ppt-jadwal-section` collapse :428-478 = kartu Pre :429-449 + kartu Post :452-478, SamePackage checkbox **:475** di DALAM kartu Post), Group B "Pengaturan Ujian" (:488-579: Status/PassPercentage :496-512, Token :516-533, Shuffle :536-551, Retake :552-576), Group C "Sertifikat" (:582-627: GenerateCertificate :591, ValidUntil :621), Group D "Opsi Lainnya" (:630-643: AllowAnswerReview :638). JS toggle mode `:1986-2033` (PrePost → tampil ppt-jadwal, sembunyikan standard-jadwal + statusFieldWrapper, set Status=Upcoming, tampil prePostCertNote).
- **`Views/Admin/EditAssessment.cshtml`** (940 baris) — **TIDAK render shuffle** (grep "acak"/"shuffle"=0 → akar bug E-01); retake fields ada tapi no-op `:420/427/433`; ValidUntil editable `:484-490`.
- **`Controllers/AssessmentAdminController.cs`** — POST `CreateAssessment` (~861-1676): build Pre/Post sessions `:1241-1318` (Pre GenerateCertificate=false D-20 :1257; cross-link :1310-1318), build standard `:1467-1491`. POST Edit standard loop `:2072-2089`, cabang Pre-Post `:1821-2002` (return :2001 sebelum guard :2006). **Pola penyalinan eksplisit retake bulk-add `:2184-2186`** = contoh untuk FORM-02. Shuffle write sibling `:2084-2085` (std) / `:1852-1853` (Pre-Post).
- **`Models/AssessmentSession.cs`** — ShuffleQuestions/Options `:38-42` (default true), AllowRetake/MaxAttempts/RetakeCooldownHours `:44-54`, ValidUntil `:84`, AssessmentType `:170-173`, SamePackage `:198-203`.
- **`TrainingAdminController` EditManualAssessment GET `:994`** — sudah punya filter `IsManualEntry` (mirror untuk FORM-06/E-08 di `AssessmentAdminController` EditAssessment GET `:1682-1686`).
- **`Helpers/AssessmentEditEligibility.IsEditableAsync`** — saat ini hanya dipakai EditPesertaAnswers (semantik berlawanan); pertimbangkan reuse untuk FORM-05 lock.

### Established Patterns
- Bootstrap card idiom: `<div class="card mb-4"><div class="card-header bg-light"><h6>...</h6></div><div class="card-body"><div class="row g-3">...` — pakai untuk sub-kartu D-02.
- Tag-helper `asp-for` untuk binding model (shuffle render D-01 ikut pola Create `:542/547`).
- JS toggle mode via `classList.add/remove('d-none'/'show')` di `:1986-2033` — perluas untuk sembunyikan retake (D-03) + sub-kartu (D-02) + SamePackage header (D-04).

### Integration Points
- POST `CreateAssessment` build paths (Pre/Post `:1241-1318`, std `:1467-1491`) — tempat FORM-02 (salin retake config).
- POST `EditAssessment` (std `:2072-2089`, Pre-Post `:1821-2002`) — tempat FORM-03/04/05 + shuffle D-01 (render di view, write tetap).
- JS payload (form submit) — tempat D-04 (jangan kirim input standard saat Pre-Post).
</code_context>

<specifics>
## Specific Ideas

- **3 jalur shuffle harus konsisten** (Create / Edit / ManagePackages) — D-01 jangan sampai Edit & `UpdateShuffleSettings` saling timpa diam-diam. Render Edit = mencerminkan + menulis nilai sebenarnya.
- Sub-kartu D-02 pakai idiom card existing (Group A-D), bukan komponen baru.
- Frontend phase → akan generate **UI-SPEC** (gsd-ui-phase) sebelum plan untuk mengunci layout sub-kartu + header SamePackage + visibilitas retake per-mode.
</specifics>

<deferred>
## Deferred Ideas

- **Overlap v32.6 (branch main):** redesign form yang sama disentuh Section/Opsi-Dinamis (fase 415-419 di main). Rekonsiliasi saat merge — JANGAN tarik scope Section/Opsi-Dinamis ke fase 420.
- Tidak ada scope creep lain — diskusi tetap dalam batas fase (form persistensi + UX Pre-Post).
</deferred>

---

*Phase: 420-form-create-edit-persistensi-field-ux-pre-post*
*Context gathered: 2026-06-22*
