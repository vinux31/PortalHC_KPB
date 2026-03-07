# Requirements: Portal HC KPB v3.10

**Defined:** 2026-03-07
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.10 Requirements

### Modal Cleanup

- [x] **MOD-01**: Field "Kompetensi Coachee" dihapus dari evidence modal CoachingProton (textarea #evidenceKoacheeComp)
- [x] **MOD-02**: CoachingSession model dan SubmitEvidenceWithCoaching action tidak lagi menyimpan/menerima field koacheeCompetencies

### Status History

- [x] **HIST-01**: Tabel DeliverableStatusHistory menyimpan setiap perubahan status (Created, Submitted, Approved, Rejected, Reviewed, Re-submitted) dengan timestamp dan actor
- [x] **HIST-02**: Rejection record menyimpan rejection reason dan tidak terhapus saat coach resubmit evidence baru
- [x] **HIST-03**: Setiap approval per-role (SrSpv Approved, SH Approved, HC Reviewed) tercatat sebagai entry terpisah di history
- [x] **HIST-04**: Re-submit evidence setelah rejection tercatat sebagai entry "Re-submitted" di history

### Deliverable Page Restructure

- [ ] **PAGE-01**: Halaman Deliverable detail dibagi menjadi sections yang jelas: Detail Coachee & Kompetensi, Evidence Coach, Approval Chain, Riwayat Status
- [ ] **PAGE-02**: Section Riwayat Status menampilkan timeline kronologis semua status changes dari DeliverableStatusHistory
- [ ] **PAGE-03**: Section Evidence Coach menampilkan coaching session data (Catatan Coach, Kesimpulan, Result) dan file evidence dengan tombol download

### P-Sign Infrastructure

- [x] **PSIGN-01**: Setiap ApplicationUser memiliki data P-Sign (Position/Role text dan Unit) yang bisa di-render menjadi badge visual
- [x] **PSIGN-02**: P-Sign badge berisi: Logo Pertamina (gambar statis), Role + Unit (dari user data), dan Nama lengkap user
- [x] **PSIGN-03**: P-Sign dapat di-generate sebagai image atau embeddable component untuk digunakan di PDF dan halaman web

### PDF Evidence

- [ ] **PDF-01**: Setelah coach submit evidence, sistem auto-generate PDF form evidence coaching
- [ ] **PDF-02**: PDF berisi: info Coachee, Track, Kompetensi, SubKompetensi, Deliverable, Tanggal, Catatan Coach, Kesimpulan, Result
- [ ] **PDF-03**: PDF memiliki P-Sign Coach di pojok kanan bawah
- [ ] **PDF-04**: PDF bisa di-download dari halaman Deliverable detail via tombol "Download PDF"

## Out of Scope

| Feature | Reason |
|---------|--------|
| P-Sign untuk approval export (SrSpv, SH, HC) | Akan dibahas di milestone terpisah setelah P-Sign infrastructure ready |
| Perubahan approval workflow logic | Hanya visual restructure, logic tetap sama |
| Perubahan file upload mechanism | Tetap pakai wwwroot/uploads pattern yang sudah ada |
| Export PDF approval chain dengan multi P-Sign | Butuh analisa terpisah untuk layout multi-signer |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| MOD-01 | Phase 116 | Complete |
| MOD-02 | Phase 116 | Complete |
| HIST-01 | Phase 117 | Complete |
| HIST-02 | Phase 117 | Complete |
| HIST-03 | Phase 117 | Complete |
| HIST-04 | Phase 117 | Complete |
| PAGE-01 | Phase 119 | Pending |
| PAGE-02 | Phase 119 | Pending |
| PAGE-03 | Phase 119 | Pending |
| PSIGN-01 | Phase 118 | Complete |
| PSIGN-02 | Phase 118 | Complete |
| PSIGN-03 | Phase 118 | Complete |
| PDF-01 | Phase 120 | Pending |
| PDF-02 | Phase 120 | Pending |
| PDF-03 | Phase 120 | Pending |
| PDF-04 | Phase 120 | Pending |

**Coverage:**
- v3.10 requirements: 16 total
- Mapped to phases: 16
- Unmapped: 0

---
*Requirements defined: 2026-03-07*
