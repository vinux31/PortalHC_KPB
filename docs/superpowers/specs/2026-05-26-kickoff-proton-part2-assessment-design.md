# Kickoff PROTON — Part 2: Sistem Ujian Assessment (Design)

**Tanggal:** 2026-05-26
**Target file:** `docs/Kickoff-PROTON.html`
**Status awal:** v3.0 LOCAL (21 slide, commit `05ad9cc5`)
**Status setelah implementasi:** v4.0 (31 slide total — Part 1 1-21 + Part 2 22-31)

---

## 1. Konteks & Tujuan

### 1.1 Konteks

`docs/Kickoff-PROTON.html` saat ini berisi 21 slide deck Part 1 (introduction Portal HC KPB + 3 Pilar CMP/CDP/BP + PROTON methodology + Chain Coaching + KKJ + Silabus + Alur E2E + Manfaat + Outcome Sertifikasi Mahir + Akses Portal). Assessment hanya disinggung secara implisit di slide CMP dan slide Outcome — belum ada penjelasan eksplisit mengenai (a) alur assessment, (b) arsitektur sistem ujian, dan (c) cara coachee/HC mengoperasikan ujian.

### 1.2 Tujuan

Menambahkan **10 slide Part 2 (Sl22-31)** yang menjelaskan sistem assessment Portal HC KPB secara end-to-end, mencakup:

- Dua jenis assessment: **CMP Assessment** dan **PROTON Tahap Assessment** (dengan perbedaan eksplisit)
- Alur assessment masing-masing (E2E flow)
- Anatomi sistem ujian (entity model + grading engine + status lifecycle + timer)
- Cara ujian dari sudut pandang coachee (POV peserta) dan HC (POV grader)
- Outcome: sertifikat CMP vs CompetencyLevel PROTON / Sertifikasi Mahir

### 1.3 Format

Part 2 di-**append** setelah Sl21 dalam HTML file yang sama (1 file, 2 logical deck). Diawali dengan **cover divider slide (Sl22)** sebagai visual separator antara Part 1 dan Part 2.

---

## 2. Grounding Codebase (Single Source of Truth)

Seluruh konten Part 2 berbasis analisa codebase aktual untuk menghindari claim generic. Referensi utama:

| Topik | File:Line |
|-------|-----------|
| AssessmentSession entity (Status, ProtonTrackId, TahunKe, NomorSertifikat) | `Models/AssessmentSession.cs:1-193` |
| AssessmentConstants (Status enum exact) | `Models/AssessmentConstants.cs:13-21` |
| ProtonTrack / ProtonKompetensi / SubKompetensi / Deliverable hierarchy | `Models/ProtonModels.cs:8-66` |
| ProtonTrackAssignment / DeliverableProgress (3-role approval) | `Models/ProtonModels.cs:71-148` |
| ProtonFinalAssessment (CompetencyLevel 0-5) | `Models/ProtonModels.cs:207-226` |
| GradingService (auto + manual + cert generation retry) | `Services/GradingService.cs:41-306` |
| CertNumberHelper (KPB/seq:D3/Roman/Year format) | `Helpers/CertNumberHelper.cs:10-43` |
| CMPController (StartExam, SubmitExam, Results, Certificate) | `Controllers/CMPController.cs:195, 816, 1569, 1787, 2141` |
| AssessmentAdminController (SubmitInterviewResults Tahun 3) | `Controllers/AssessmentAdminController.cs:3478` |

---

## 3. Dimensi Penting — Track vs Bagian

Audience kickoff sering bingung karena PROTON punya **dua dimensi independen**:

- **Tahun (Track-level)** — `ProtonTrack.TahunKe` ∈ {`Tahun 1`, `Tahun 2`, `Tahun 3`} — timeline coaching cycle. Assessment dilakukan **per Tahun** (3 cycle total per coachee).
- **Bagian (Kompetensi-level)** — `ProtonKompetensi.Bagian` — sub-unit kompetensi *dalam* satu track (multi-Bagian per Tahun).

**Hirarki:**

```
ProtonTrack (TrackType + TahunKe)              ← Assessment scope
  └─ ProtonKompetensi (Bagian + Unit)
      └─ ProtonSubKompetensi
          └─ ProtonDeliverable                  ← Submission scope
```

**Implikasi presentasi:**
- 1 cycle assessment PROTON = 1 Tahun (bukan 1 Bagian).
- Tahun 1-2 = MC online (engine sama CMP).
- Tahun 3 = Interview offline + HC manual input → **Sertifikasi Mahir** granted.

---

## 4. Outline Final 10 Slide (Sl22-31)

### Slide 22 — Cover Part 2

- **Layout:** Divider full-page, mirror style Sl1 cover Part 1 tapi accent berbeda
- **Konten:** Title "Bagian 2: Sistem Ujian Assessment Portal HC KPB"; subtitle "Alur · Arsitektur · Cara Ujian"
- **Visual:** Big banner teal-dark + ikon trophy/exam (Bootstrap Icons CDN)
- **Tujuan:** Visual reset audience dari Part 1 → Part 2

### Slide 23 — 2 Jenis Assessment + Hirarki PROTON

- **Layout:** Atas — 2-column comparison card (CMP vs PROTON). Bawah — tree diagram hirarki PROTON
- **Konten 2-col card:**
  - **CMP Assessment**: Tujuan = ujian kompetensi multi-tahap (Pre/Mid/Post) · Trigger = HC create session per package · Output = NomorSertifikat score-based
  - **PROTON Assessment**: Tujuan = ujian akhir per Tahun cycle · Trigger = deliverable approval lengkap · Output = CompetencyLevel 0-5 + Sertifikasi Mahir (Tahun 3)
- **Konten tree diagram:**
  ```
  ProtonTrack (Operator/Panelman × Tahun 1/2/3)
    └─ ProtonKompetensi (Bagian + Unit)
        └─ ProtonSubKompetensi
            └─ ProtonDeliverable
  ```
- **Tujuan:** Anchor mental model — semua slide berikutnya reference balik ke slide ini

### Slide 24 — Alur Assessment CMP (E2E)

- **Layout:** Horizontal flowchart 7-step + status pill di bawah setiap node
- **Step:**
  1. HC create AssessmentSession + Package(s)
  2. Coachee lihat list (status: Open / Upcoming)
  3. Coachee klik Start Exam (status: InProgress)
  4. Coachee jawab soal + Timer berjalan
  5. Coachee Submit
  6. Auto-grade MC/MA + HC review Essay (status: Completed atau "Menunggu Penilaian")
  7. Result + Sertifikat (GradingService trigger NomorSertifikat jika IsPassed)
- **Tujuan:** Audience paham alur CMP end-to-end

### Slide 25 — Alur Assessment PROTON (Tahun 1-2 vs Tahun 3 side-by-side)

- **Layout:** 2-column parallel flowchart
- **Kiri — Tahun 1-2 (MC Online):**
  1. Coachee submit Deliverables (ProtonDeliverableProgress)
  2. 3-Role Approval: HCApprovalStatus + SrSpvApprovalStatus + ShApprovalStatus
  3. HC create AssessmentSession (`Category=Assessment Proton` + ProtonTrackId)
  4. Coachee ujian MC online (engine sama CMP)
  5. Auto-grade
  6. HC create ProtonFinalAssessment
- **Kanan — Tahun 3 (Interview):**
  1. Coachee submit Deliverables
  2. 3-Role Approval
  3. Interview offline
  4. HC `SubmitInterviewResults()` → InterviewResultsJson
  5. HC create ProtonFinalAssessment + CompetencyLevel 0-5
  6. **Sertifikasi Mahir** granted
- **Tujuan:** Bedakan dua mode PROTON assessment — hindari misperception "PROTON = 1 jenis ujian"

### Slide 26 — Sistem Ujian — Anatomi Soal (ER Diagram)

- **Layout:** Clean ER diagram center, label field penting di samping
- **Konten:**
  ```
  AssessmentSession ──1..*── AssessmentPackage ──1..*── PackageQuestion ──1..*── PackageOption
  ```
- **Field penting yang ditampilkan:**
  - AssessmentSession: `Status`, `DurationMinutes`, `ExtraTimeMinutes`, `PassPercentage`, `GenerateCertificate`, `NomorSertifikat`, `ProtonTrackId`, `TahunKe`, `Category`, `InterviewResultsJson`
  - PackageQuestion: `Type` (MultipleChoice/MultipleAnswer/Essay), `ScoreValue`, `Rubrik`
  - PackageOption: `IsCorrect`
- **Tujuan:** Audience HC/admin paham struktur data, sebagai fondasi Sl27-28

### Slide 27 — Sistem Ujian — Grading Engine

- **Layout:** Split panel — Auto Grading (kiri) vs Manual Grading (kanan)
- **Auto (kiri):**
  - MultipleChoice: match `PackageOption.IsCorrect`
  - MultipleAnswer: multi-select exact match
  - ElemenTeknisScore per grup soal (breakdown KKJ)
- **Manual (kanan):**
  - Essay: HC input `EssayScore` 0..ScoreValue via `Rubrik`
  - Interview Tahun 3: HC input InterviewResultsDto (per kompetensi)
- **Footer:** PassPercentage threshold → `IsPassed` (boolean) → trigger sertifikat
- **Tujuan:** Jelaskan engine grading hybrid (auto + manual)

### Slide 28 — Sistem Ujian — Status Lifecycle & Timer

- **Layout:** State machine diagram horizontal + timer mockup di pojok
- **Status state machine (string value exact dari `AssessmentConstants.AssessmentStatus`):**
  ```
  Upcoming → Open → InProgress → Completed
                              ↘ "Menunggu Penilaian" (PendingGrading) → Completed
                              ↘ Cancelled
  ```
- **Timer:** `DurationMinutes` server-enforced + client countdown; `ExtraTimeMinutes` (Phase 302) untuk akomodasi khusus; auto-submit saat timer habis
- **Tujuan:** Audience paham transition state + control time-bound

### Slide 29 — Cara Ujian — Coachee POV

- **Layout:** Numbered mockup grid 8-panel (atau 2 baris × 4 panel)
- **8 step coachee:**
  1. Login Portal HC KPB
  2. Buka menu CMP > Assessment (atau menu PROTON untuk PROTON assessment)
  3. Lihat list (filter Open / Upcoming / Completed)
  4. Klik "Start Exam"
  5. Confirm modal (warning: timer akan mulai)
  6. Jawab soal + countdown timer real-time
  7. Submit (auto-submit jika timer habis)
  8. Lihat Result + Score breakdown per Elemen Teknis + link Sertifikat
- **Tujuan:** Audience kickoff (calon coachee/coach) paham UX flow

### Slide 30 — Cara Grading — HC POV

- **Layout:** Mockup admin panel + sample NomorSertifikat
- **HC workflow:**
  1. Buka dashboard Admin Assessment
  2. Filter status "Menunggu Penilaian" (PendingGrading)
  3. Buka jawaban Essay coachee
  4. Input `EssayScore` per soal via `Rubrik` (0..ScoreValue)
  5. Finalize session → trigger `CertNumberHelper.Build()`
  6. NomorSertifikat auto-generated: **`KPB/{seq:D3}/{Roman}/{Year}`**
     - Contoh real: `KPB/001/V/2026`, `KPB/042/XII/2026`
     - Retry 3x idempotent (race-condition safe via `SetProperty` + `WHERE NomorSertifikat IS NULL`)
- **Tujuan:** Audience HC paham tooling grading + format sertifikat resmi

### Slide 31 — Outcome — Sertifikat & CompetencyLevel

- **Layout:** 2-column outcome card
- **Kiri — CMP:**
  - NomorSertifikat resmi (format `KPB/...`)
  - Score breakdown per Elemen Teknis
  - Sample sertifikat PDF mock
- **Kanan — PROTON:**
  - CompetencyLevel 0-5 (`ProtonFinalAssessment.CompetencyLevelGranted` `[Range(0, 5)]`)
  - Linked KkjMatrixItem (kompetensi reference)
  - **Sertifikasi Mahir** = milestone Tahun 3 Final Assessment
  - Level badge visual 0-5
- **Footer tie-back:** "Lihat Sl19 (Outcome Sertifikasi Mahir & IDP) untuk konteks Part 1"
- **Tujuan:** Closing Part 2 + tie back ke Part 1

---

## 5. Style & Konsistensi Visual

- **Inherit dari Part 1:** Gunakan CSS variables yang sudah ada (`--teal-dark`, `--teal-light`, accent colors), `slide-header`, `slide-title`, `slide-number`, `accent` span
- **Ikon:** Bootstrap Icons CDN (sudah dipakai mockup CMP-CDP & coaching-proton-mockup, konsisten)
- **Diagram inline:** SVG inline atau divbox + flex (no external dependency)
- **Sl22 cover:** Mirror style Sl1 tapi accent berbeda untuk visual divider yang jelas
- **Slide numbering:** Continue dari Sl21 → Sl22, Sl23, ..., Sl31 (tidak restart)

---

## 6. Scope Eksplisit

### 6.1 In-Scope

- 10 slide baru (Sl22-31) append setelah Sl21 dalam `docs/Kickoff-PROTON.html`
- Inherit semua CSS + style helper dari Part 1
- Konten grounded ke codebase actual (file refs di §2)
- Konsistensi terminologi: "Tahun" untuk track-level, "Bagian" untuk kompetensi-level
- Status enum value persis (`"Menunggu Penilaian"` Indonesian, bukan `"PendingGrading"` English)
- NomorSertifikat format akurat (`KPB/seq:D3/Roman/Year`)

### 6.2 Out-of-Scope

- Tidak ubah Sl1-21 (Part 1 frozen)
- Tidak buat versi PDF (hanya HTML)
- Tidak buat screenshot real dari aplikasi (gunakan mockup HTML/CSS-based)
- Tidak troubleshoot/edge-case slide
- Tidak ubah CSS global Part 1
- Tidak buat file terpisah (1 HTML file saja)

---

## 7. Acceptance Criteria

1. File `docs/Kickoff-PROTON.html` total 31 slide (21 Part 1 + 10 Part 2)
2. Sl22 (cover Part 2) visually distinct sebagai divider
3. Setiap slide Sl22-31 follow style Sl1-21 (CSS class inherit)
4. Tidak ada regression di Sl1-21 (verify dengan Playwright atau visual diff)
5. Status enum string value exact match codebase (`"Menunggu Penilaian"` literal)
6. NomorSertifikat format akurat `KPB/{seq:D3}/{Roman}/{Year}` di Sl30
7. Hirarki PROTON di Sl23 menampilkan 4 level (Track → Kompetensi → SubKompetensi → Deliverable)
8. Sl25 menampilkan 2 sub-flow side-by-side (Tahun 1-2 MC vs Tahun 3 Interview)
9. 3-role approval (HC + SrSpv + SectionHead) explicit di Sl25
10. Playwright snapshot OK + console error 0

---

## 8. Risiko & Mitigasi

| Risiko | Mitigasi |
|--------|----------|
| Terminology "Tahun" vs "Bagian" salah ditukar | Sl23 anchor diagram + glossary mini di Sl23 footer |
| Status enum drift (codebase pakai Indonesian, slide pakai English) | §2 grounding lock + acceptance criteria #5 |
| NomorSertifikat format keliru | Sample real (`KPB/001/V/2026`) di Sl30 + acceptance #6 |
| Regression Sl1-21 (CSS bocor) | Slide baru pakai scoped class atau inline style yang tidak override global |
| Slide 25 overflow (2-col parallel padat) | Kompres step 6→5 jika perlu, prioritas Tahun 3 Interview detail |
| Audience bingung CMP vs PROTON | Sl23 anchor + tie-back footer di Sl31 |

---

## 9. Referensi

- Memory: `project_kickoff_proton_v2_shipped.md` (v3.0 commit `05ad9cc5`)
- Memory: `project_assessment_akhir_shipped.md` (video Assessment Akhir Per Tahap Bagian 5 — context untuk Sertifikasi Mahir)
- Memory: `project_sertifikat_ecosystem_doc_shipped.md` (doc 18-section sertifikat ecosystem — reference NomorSertifikat lifecycle)
- File aktual: `docs/Kickoff-PROTON.html` (21 slide existing v3.0)

---

## 10. Next Step

Setelah spec di-approve user:
1. Invoke `superpowers:writing-plans` skill untuk generate implementation plan
2. Plan akan break down Sl22-31 jadi task atomic per slide + verification step (Playwright snapshot + visual diff Sl1-21)
3. Eksekusi via TDD/incremental per slide
