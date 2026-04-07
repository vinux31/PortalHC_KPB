# Requirements: PortalHC KPB

**Defined:** 2026-04-02
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v13.0 Requirements

Requirements untuk milestone v13.0. Redesign halaman ManageOrganization dari tabel flat menjadi tree view modern.

### Tree View & Rendering

- [x] **TREE-01**: Admin/HC dapat melihat struktur organisasi sebagai tree view dengan indentasi visual per level
- [x] **TREE-02**: Admin/HC dapat expand/collapse node individual dan semua node sekaligus
- [x] **TREE-03**: Setiap node menampilkan badge status Aktif/Nonaktif
- [x] **TREE-04**: Tree view mendukung kedalaman unlimited (recursive rendering)

### CRUD & Aksi

- [ ] **CRUD-01**: Admin/HC dapat menambah unit baru via modal (AJAX, tanpa page reload)
- [ ] **CRUD-02**: Admin/HC dapat mengedit nama dan parent unit via modal (AJAX, tanpa page reload)
- [ ] **CRUD-03**: Admin/HC dapat toggle aktif/nonaktif unit via AJAX tanpa page reload
- [ ] **CRUD-04**: Admin/HC dapat menghapus unit via modal konfirmasi (AJAX, tanpa page reload)
- [ ] **CRUD-05**: Setiap node memiliki action dropdown (edit, toggle, hapus) menggantikan tombol inline

### Reorder

- [ ] **REORD-01**: Admin/HC dapat drag-and-drop unit untuk mengubah urutan dalam sibling yang sama
- [ ] **REORD-02**: Drag cross-parent diblokir untuk mencegah bypass cascade logic

## v14.0 Requirements

Requirements untuk milestone v14.0. Data foundation dan GradingService extraction untuk assessment grading.

### Data Foundation (Phase 296)

- [x] **FOUND-01**: GradingService terdaftar di DI container (Program.cs)
- [x] **FOUND-02**: QuestionType enum (MultipleChoice, MultipleAnswer, Essay) di PackageQuestion model
- [x] **FOUND-03**: AssessmentSession.AssessmentType kolom baru via migration
- [x] **FOUND-04**: AssessmentSession.AssessmentPhase, LinkedGroupId, LinkedSessionId kolom baru
- [x] **FOUND-05**: PackageUserResponse.TextAnswer + HasManualGrading field + migration
- [x] **FOUND-06**: GradeAndCompleteAsync dengan switch-case QuestionType (MC implemented, MA/Essay graceful skip)
- [x] **FOUND-07**: AkhiriUjian menggunakan GradingService
- [x] **FOUND-08**: AkhiriSemuaUjian menggunakan GradingService (dengan per-session error handling)
- [x] **FOUND-09**: SubmitExam menggunakan GradingService

## Future Requirements (v14+)

### Enhancements

- **ENH-01**: Badge jumlah pekerja per unit
- **ENH-02**: Badge jumlah children per node
- **ENH-03**: Search/filter tree
- **ENH-04**: Inline rename (double-click)
- **ENH-05**: Konfirmasi delete dengan detail dampak (jumlah user/file terdampak)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Org chart box-and-line diagram | Anti-feature untuk management UI, tidak scale, butuh library besar |
| Drag cross-parent (reparent via drag) | Bypass cascade rename/reparent logic, risiko data rusak |
| Refactor GetSectionUnitsDictAsync | Status quo 2-level cukup untuk kebutuhan saat ini |
| Backend cascade logic changes | Backend sudah solid, fokus milestone ini pada UI/UX saja |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| TREE-01 | Phase 292, 293 | Complete |
| TREE-02 | Phase 293 | Complete |
| TREE-03 | Phase 293 | Complete |
| TREE-04 | Phase 292, 293 | Complete |
| CRUD-01 | Phase 294 | Pending |
| CRUD-02 | Phase 294 | Pending |
| CRUD-03 | Phase 294 | Pending |
| CRUD-04 | Phase 294 | Pending |
| CRUD-05 | Phase 294 | Pending |
| REORD-01 | Phase 295 | Pending |
| REORD-02 | Phase 295 | Pending |

| FOUND-01 | Phase 296 | Complete |
| FOUND-02 | Phase 296 | Complete |
| FOUND-03 | Phase 296 | Complete |
| FOUND-04 | Phase 296 | Complete |
| FOUND-05 | Phase 296 | Complete |
| FOUND-06 | Phase 296 | Complete |
| FOUND-07 | Phase 296 | Complete |
| FOUND-08 | Phase 296 | Complete |
| FOUND-09 | Phase 296 | Complete |

**Coverage:**
- v13.0 requirements: 11 total, mapped: 11
- v14.0 requirements: 9 total, mapped: 9
- Unmapped: 0 ✓

---
*Requirements defined: 2026-04-02*
*Last updated: 2026-04-06 — Added v14.0 FOUND-01~09 (Phase 296 Data Foundation)*
