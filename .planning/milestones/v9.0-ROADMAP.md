# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** - Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** - Phases 176-222 (shipped)
- ✅ **v8.0** - Phases 223-227 (shipped)
- ✅ **v8.1** - Phases 228-232 (shipped)
- ✅ **v8.2** - Phases 233-238 (shipped)
- ✅ **v8.3** - Phase 239 (shipped)
- ✅ **v8.4** - Phase 240 (shipped)
- ✅ **v8.5** - Phases 241-247 (shipped)
- ✅ **v8.6 Codebase Audit & Hardening** - Phases 248-252 (shipped 2026-03-24)
- ✅ **v8.7** - Phase 253 (shipped)
- 🚧 **v9.0 Pre-deployment Audit & Finalization** - Phases 254-256 (in progress)

## Phases

### 🚧 v9.0 Pre-deployment Audit & Finalization (Phases 254-256)

**Milestone Goal:** Finalisasi codebase untuk production — seed cleanup, production config, security hardening. Deployment & setup server ditangani tim IT (dokumen serah terima sudah dibuat).

- [ ] **Phase 254: Seed Cleanup & Tech Debt Closure** - Bersihkan seed data dari production path dan tutup 5 tech debt item v4.3
- [ ] **Phase 255: Production Configuration** - Siapkan file konfigurasi production (logging, connection string template, environment guard)
- [ ] **Phase 256: Security Hardening** - Hardening keamanan: error pages, cookie, anti-forgery, authorization, upload validation

## Phase Details

### Phase 254: Seed Cleanup & Tech Debt Closure
**Goal**: Codebase bersih dari data test di production path dan semua tech debt v4.3 tertutup
**Depends on**: Nothing (first phase v9.0)
**Requirements**: SEED-01, SEED-02, SEED-03, DEBT-01, DEBT-02, DEBT-03, DEBT-04, DEBT-05
**Success Criteria** (what must be TRUE):
  1. SeedProtonData hanya berjalan di environment Development (ada IsDevelopment guard)
  2. Semua method Seed* yang mengandung data test memiliki environment guard dan bersifat idempotent
  3. Bare catch di AdminController:1072 sudah diganti dengan proper exception handling dan logging
  4. 3 orphaned KkjMatrixItemId columns sudah di-cleanup dari model/migration
  5. 5 near-duplicate code pairs sudah di-extract atau didokumentasikan keputusan keep
**Plans**: TBD

### Phase 255: Production Configuration
**Goal**: File konfigurasi production siap — logging level benar, connection string template jelas, debug middleware hanya di Development
**Depends on**: Phase 254
**Requirements**: CONF-01, CONF-02, CONF-04
**Success Criteria** (what must be TRUE):
  1. appsettings.Production.json ada dengan logging level Warning untuk Microsoft.* dan Information untuk app
  2. Connection string di appsettings.Production.json menggunakan placeholder yang jelas dan terdokumentasi
  3. Debug/development middleware (Developer Exception Page, dsb) hanya aktif di Development environment
**Plans**: TBD

### Phase 256: Security Hardening
**Goal**: Aplikasi aman dari serangan umum web dengan error handling, cookie security, dan authorization yang lengkap
**Depends on**: Phase 255
**Requirements**: SEC-01, SEC-02, SEC-03, SEC-04, SEC-05
**Success Criteria** (what must be TRUE):
  1. Halaman error custom tampil untuk 404/403/500 tanpa expose stack trace atau detail internal
  2. Cookie authentication dikonfigurasi Secure=Always, HttpOnly=true, SameSite yang tepat
  3. Semua POST action memiliki anti-forgery token (tidak ada gap CSRF)
  4. Setiap controller/action memiliki atribut authorization yang benar (audit matrix lengkap)
  5. Semua file upload endpoint memvalidasi extension whitelist, size limit, dan content-type
**Plans**: TBD
**UI hint**: yes

<details>
<summary>✅ v8.5 UAT Assessment System End-to-End (Phases 241-247) — SHIPPED 2026-03-24</summary>

- [x] Phase 241: Seed Data UAT (2/2 plans)
- [x] Phase 242: UAT Setup Flow (2/2 plans)
- [x] Phase 243: UAT Exam Flow (2/2 plans)
- [x] Phase 244: UAT Monitoring & Analytics (2/2 plans)
- [x] Phase 245: UAT Proton Assessment (2/2 plans)
- [x] Phase 246: UAT Edge Cases & Records (2/2 plans)
- [x] Phase 247: Bug Fix Pasca-UAT (2/2 plans)

</details>

<details>
<summary>✅ v8.6 Codebase Audit & Hardening (Phases 248-252) — SHIPPED 2026-03-24</summary>

- [x] Phase 248: UI & Annotations (1/1 plans)
- [x] Phase 249: Null Safety & Input Validation (2/2 plans)
- [x] Phase 250: Security & Performance (1/1 plans)
- [x] Phase 251: Data Integrity & Logic (2/2 plans)
- [x] Phase 252: XSS Escape AJAX Approval Badge (1/1 plans)

</details>

<details>
<summary>✅ v8.7 AddTraining Multi-Select (Phase 253) — SHIPPED 2026-03-25</summary>

- [x] Phase 253: AddTraining multi-select pekerja dan perbaikan form (2/2 plans)

</details>

## Progress

**Execution Order:**
Phases execute in numeric order: 254 → 255 → 256

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 254. Seed Cleanup & Tech Debt Closure | v9.0 | 0/? | Not started | - |
| 255. Production Configuration | v9.0 | 0/? | Not started | - |
| 256. Security Hardening | v9.0 | 0/? | Not started | - |
