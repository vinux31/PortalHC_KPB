# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v7.7** - Phases 1–204 (shipped 2026-03-19)
- ✅ **v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu** - Phases 205–207 (shipped 2026-03-20)
- ✅ **v7.9 Renewal Certificate Grouped View** - Phases 208–209 (shipped 2026-03-20)
- ✅ **v7.10 RenewalCertificate Bug Fixes & Enhancement** - Phases 210–212 (shipped 2026-03-21)
- ✅ **v7.11 CMP Records Bug Fixes & Enhancement** - Phases 213–218 (shipped 2026-03-21)
- ✅ **v7.12 Struktur Organisasi CRUD** - Phases 219–222 (shipped 2026-03-21)
- ✅ **v8.0 Assessment Integrity & Analytics** - Phases 223–227 (shipped 2026-03-22)
- ✅ **v8.1 Renewal & Assessment Ecosystem Audit** - Phases 228–232 (shipped 2026-03-22)
- ✅ **v8.2 Proton Coaching Ecosystem Audit** - Phases 233–238 (shipped 2026-03-23)
- ✅ **v8.3 Date Range Filter Team View Records** - Phase 239 (shipped 2026-03-23)
- 🚧 **v8.4 Alarm Sertifikat Expired** - Phase 240 (in progress)

---

<details>
<summary>✅ v1.0–v8.3 (Phases 1–239) - SHIPPED</summary>

All prior milestones shipped. See MILESTONES.md for full detail.

Last completed phase: 239 (v8.3 — Date Range Filter Team View Records)

</details>

---

### 🚧 v8.4 Alarm Sertifikat Expired (In Progress)

**Milestone Goal:** Tampilkan alarm sertifikat expired kepada HC/Admin melalui 2 kanal: banner alert di Home/Index dan notifikasi bell (CERT_EXPIRED).

## Phases

- [x] **Phase 240: Alarm Sertifikat Expired** - Banner alert + bell notification CERT_EXPIRED untuk HC/Admin di Home/Index (completed 2026-03-23)

## Phase Details

### Phase 240: Alarm Sertifikat Expired
**Goal**: HC dan Admin dapat melihat status sertifikat bermasalah secara langsung saat membuka Home/Index — melalui banner ringkas di halaman dan notifikasi bell yang persisten — sehingga tindakan renewal dapat segera diambil
**Depends on**: Nothing (single-phase milestone)
**Requirements**: ALRT-01, ALRT-02, ALRT-03, ALRT-04, NOTF-01, NOTF-02, NOTF-03
**Success Criteria** (what must be TRUE):
  1. HC/Admin membuka Home/Index dan melihat banner alert yang menampilkan jumlah Expired (latar merah) dan Akan Expired ≤30 hari (latar kuning) secara terpisah
  2. Banner memiliki link "Lihat Detail" yang membawa user langsung ke halaman RenewalCertificate
  3. Banner tidak muncul sama sekali ketika tidak ada sertifikat expired maupun akan expired
  4. Setiap sertifikat expired yang belum pernah dinotifikasikan menghasilkan entri UserNotification tipe CERT_EXPIRED di bell dropdown saat HC/Admin membuka Home/Index
  5. Notifikasi CERT_EXPIRED di bell dropdown menampilkan nama pekerja dan judul sertifikat, dan hanya dikirim ke user dengan role HC atau Admin
**Plans**: 2 plans
Plans:
- [x] 240-01-PLAN.md — Banner alert sertifikat expired/akan expired di Home/Index
- [x] 240-02-PLAN.md — Bell notification CERT_EXPIRED on page load
**UI hint**: yes

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 239. Date Range Filter & Export | v8.3 | 2/2 | Complete | 2026-03-23 |
| 240. Alarm Sertifikat Expired | v8.4 | 2/2 | Complete    | 2026-03-23 |
