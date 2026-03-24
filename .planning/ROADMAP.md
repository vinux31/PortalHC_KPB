# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** - Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** - Phases 176-222 (shipped)
- ✅ **v8.0** - Phases 223-227 (shipped)
- ✅ **v8.1** - Phases 228-232 (shipped)
- ✅ **v8.2** - Phases 233-238 (shipped)
- ✅ **v8.3** - Phase 239 (shipped)
- ✅ **v8.4** - Phase 240 (shipped)
- 📋 **v8.5** - Phases 241-247 (defined, pending execution)
- 🚧 **v8.6 Codebase Audit & Hardening** - Phases 248-251 (in progress)

## Phases

### 🚧 v8.6 Codebase Audit & Hardening (In Progress)

**Milestone Goal:** Perbaiki semua bug Critical/High/Medium dari deep audit codebase — null safety, input validation, data integrity, security leak, dan performa. Setiap fix atomic, tidak ada fitur baru.

- [x] **Phase 248: UI & Annotations** - Tambah CSS global, data annotations MaxLength/Range tanpa mengubah logika apapun (completed 2026-03-24)
- [x] **Phase 249: Null Safety & Input Validation** - Tambah defensive null checks dan safe parse di seluruh codebase (completed 2026-03-24)
- [x] **Phase 250: Security & Performance** - Fix XSS, hapus console.log yang mengekspos data, password policy, throttle notifikasi (completed 2026-03-24)
- [x] **Phase 251: Data Integrity & Logic** - Fix timezone, unique index migration, validasi business rule, thread safety (completed 2026-03-24)
- [ ] **Phase 252: XSS Escape AJAX Approval Badge** - Escape data.approverName di JavaScript AJAX handler CoachingProton agar XSS tertutup di semua jalur

## Phase Details

### Phase 248: UI & Annotations
**Goal**: Anotasi data model dan CSS global tersedia sehingga badge tampil benar dan validasi string/range konsisten di seluruh portal
**Depends on**: Nothing (first phase of milestone)
**Requirements**: UI-01, UI-02, UI-03
**Success Criteria** (what must be TRUE):
  1. Badge Proton di halaman AssessmentMonitoring tampil dengan warna ungu yang benar (tidak kosong/transparan)
  2. Seluruh string field di TrainingRecord memiliki MaxLength terdefinisi — EF Core tidak akan menggunakan nvarchar(MAX) untuk field tersebut
  3. Field `CompetencyLevelGranted` di ProtonFinalAssessment menolak nilai di luar range 0–5 pada validasi model
**Plans:** 1/1 plans complete
Plans:
- [x] 248-01-PLAN.md — CSS global .bg-purple + MaxLength/Range annotations pada model
**UI hint**: yes

### Phase 249: Null Safety & Input Validation
**Goal**: Semua titik null-dereference dan unsafe cast yang berpotensi crash di CMP dihilangkan melalui guard yang defensive
**Depends on**: Phase 248
**Requirements**: SAFE-01, SAFE-02, SAFE-03, SAFE-04, SAFE-05
**Success Criteria** (what must be TRUE):
  1. Membuka 5 halaman CMP saat session user tidak valid tidak menyebabkan unhandled exception — aplikasi mengembalikan redirect atau error message yang jelas
  2. Export data dengan parameter tanggal kosong atau tidak valid tidak menyebabkan crash — action menangani input buruk dengan fallback yang aman
  3. Bulk renewal dengan data duplikat tidak menyebabkan crash `ArgumentException: An item with the same key` — operasi selesai atau menampilkan error bermakna
  4. Halaman WorkerDetail dengan `FullName` null menampilkan string kosong, bukan exception
  5. Halaman ExamSummary dengan `ViewBag.UnansweredCount` atau `ViewBag.AssessmentId` null tidak melempar `InvalidCastException`
**Plans:** 2/2 plans complete
Plans:
- [x] 249-01-PLAN.md — Null-safe GetCurrentUserRoleLevelAsync + TryParse + ToDictionary guard
- [x] 249-02-PLAN.md — Null-safe FullName di WorkerDetail + ViewBag cast di ExamSummary

### Phase 250: Security & Performance
**Goal**: Kebocoran data sensitif via console.log dihilangkan, XSS vector di CoachingProton ditutup, password policy diperkuat, dan throttle notifikasi mencegah query berulang setiap page load
**Depends on**: Phase 249
**Requirements**: SEC-01, SEC-02, PERF-01
**Success Criteria** (what must be TRUE):
  1. Membuka DevTools browser di halaman Assessment tidak menampilkan token atau response payload di console
  2. Nama approver yang mengandung karakter HTML (misalnya `<script>`) tampil sebagai teks literal di badge tooltip CoachingProton, bukan dieksekusi sebagai markup
  3. Dashboard yang di-refresh berulang kali dalam satu jam hanya memicu query notifikasi sertifikat expired sekali, bukan setiap request
**Plans:** 1/1 plans complete
Plans:
- [x] 250-01-PLAN.md — Hapus console.log, XSS escape tooltip, throttle notifikasi

### Phase 251: Data Integrity & Logic
**Goal**: Seluruh operasi temporal menggunakan UTC, unique constraint database mencerminkan aturan bisnis yang benar, validasi business rule renewal dan edit assessment diperbaiki, dan `_lastScopeLabel` tidak menimbulkan race condition di multi-thread
**Depends on**: Phase 250
**Requirements**: DATA-01, DATA-02, DATA-03, DATA-04, DATA-05, DATA-06
**Success Criteria** (what must be TRUE):
  1. Status sertifikat "Akan Expired" dan "Expired" konsisten antara server timezone UTC dan timezone lokal — tidak ada sertifikat yang salah klasifikasi karena offset jam
  2. Dua unit organisasi dengan nama sama di bawah parent berbeda dapat dibuat tanpa unique constraint violation
  3. Membuat bulk renewal tanpa mengisi `ValidUntil` ditolak dengan pesan validasi yang jelas, bukan silent failure
  4. HC dapat mengedit assessment yang jadwalnya sudah lewat tanpa diblokir validasi past date
  5. Kegagalan deserialize `RenewalFkMap` tercatat sebagai warning di log, bukan silent ignore
**Plans:** 2/2 plans complete
Plans:
- [x] 251-01-PLAN.md — DateTime.UtcNow + fix AdminController (bulk renewal, past-date, log warning)
- [x] 251-02-PLAN.md — Thread-safe _lastScopeLabel refactor + composite unique index migration

### Phase 252: XSS Escape AJAX Approval Badge
**Goal**: Jalur AJAX approval di CoachingProton.cshtml me-escape data.approverName sebelum interpolasi ke DOM, sehingga XSS tertutup di semua jalur (server-side dan client-side)
**Depends on**: Phase 250
**Requirements**: SEC-02
**Gap Closure**: Closes SEC-02-AJAX from v8.6 audit
**Success Criteria** (what must be TRUE):
  1. Approval via AJAX menghasilkan badge tooltip yang menampilkan karakter HTML sebagai teks literal, bukan dieksekusi sebagai markup
**Plans:** 1 plan
Plans:
- [ ] 252-01-PLAN.md — Tambah escHtml() helper + escape 3 blok AJAX handler

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 248. UI & Annotations | v8.6 | 1/1 | Complete    | 2026-03-24 |
| 249. Null Safety & Input Validation | v8.6 | 2/2 | Complete    | 2026-03-24 |
| 250. Security & Performance | v8.6 | 1/1 | Complete    | 2026-03-24 |
| 251. Data Integrity & Logic | v8.6 | 2/2 | Complete    | 2026-03-24 |
| 252. XSS Escape AJAX Approval Badge | v8.6 | 0/1 | Pending     | — |
