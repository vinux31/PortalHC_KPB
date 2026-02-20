# Requirements: Portal HC KPB

**Defined:** 2026-02-20
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v1.6 Requirements

Requirements for milestone v1.6 Training Records Management.

### Training Records Management (TRN)

- [ ] **TRN-01**: HC/Admin can create a manual training record for any worker system-wide from the Training Records page ("Create Training Offline" button)
- [ ] **TRN-02**: HC/Admin can edit an existing manual training record
- [ ] **TRN-03**: HC/Admin can delete a manual training record
- [ ] **TRN-04**: HC/Admin can upload a certificate file (PDF/image) when creating or editing a training record; the file is stored and accessible/downloadable

**Training record form fields:**
- Nama Pelatihan *(required)*
- Penyelenggara *(required)*
- Kategori *(required — OJT / IHT / MANDATORY / Training Licensor / OTS / Proton / ISS / OSS)*
- Tanggal Mulai *(optional)*
- Tanggal Selesai *(optional)*
- Nomor Sertifikat *(optional)*
- Berlaku Sampai / ValidUntil *(optional)*
- Certificate file upload *(optional — PDF or image)*

## Future Requirements

### Worker Self-Management (deferred — v1.7+)

- **WTRN-01**: Worker can submit their own manual training record from their personal Records page
- **WTRN-02**: HC/Admin can approve or reject worker-submitted training records from a Pending Approvals tab

## Out of Scope

| Feature | Reason |
|---------|--------|
| Worker self-add training records | Deferred; HC manages all manual entries for v1.6 |
| Approval workflow | No worker submissions in v1.6; not needed |
| Email notifications | No email system in project |
| Bulk import of training records | Not requested; complex scope |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| TRN-01 | Phase 19 | Pending |
| TRN-02 | Phase 20 | Pending |
| TRN-03 | Phase 20 | Pending |
| TRN-04 | Phase 19 | Pending |

**Coverage:**
- v1.6 requirements: 4 total
- Mapped to phases: 4 (100%)
- Unmapped: 0

---
*Requirements defined: 2026-02-20*
*Last updated: 2026-02-20 after v1.6 roadmap creation*
