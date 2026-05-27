# Flow Sistem Sertifikat — Design Spec

**Tanggal:** 2026-05-27
**Output file:** `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html`
**Audience:** Staff HC admin Portal HC KPB (awam non-IT, sama dengan `overview-awam.html`)
**Tujuan:** Visualisasi end-to-end **25 flow distinct** sistem sertifikat Portal HC KPB sekarang — sebagai dokumen referensi awam HC untuk pahami "siapa-trigger-apa-kapan-mengakibatkan-apa".

---

## 1. Latar Belakang

`ekosistem-sertifikat.html` punya alur 4-kotak + happy path renewal, tapi cover hanya ~5 flow (terbit otomatis, renewal manual HC, status lifecycle, notif, RBAC view).

Audit codebase mendalam (caveman-investigator agent 2026-05-27) menemukan **25 flow distinct** lengkap:
- 22 flow ada di sistem (17 baseline + 5 koreksi/split)
- 3 gap kritis (Self-Service Renewal, Public Verify QR, External Verify) — tidak ada di codebase

**Hidden insight penting**: Notif expiry **BUKAN cron job**, tapi on-login passive trigger (`HomeController.TriggerCertExpiredNotificationsAsync`, cache 1-jam, dedup hash). Implikasi: kalau tidak ada user login → notif tidak terkirim. Ini hidden gap operasional yang perlu di-surface ke HC.

Dokumen ini melengkapi 3 file existing dengan **visualisasi flow lengkap** yang sebelumnya implicit.

---

## 2. Scope

### IN SCOPE

- 1 file HTML standalone (~600-800 baris)
- Bootstrap 5.3 + Bootstrap Icons + Mermaid 11 (semua CDN)
- 1 Master Lifecycle Diagram (overview 25 flow as zones)
- 8 section per fase (A-I, kecuali H+I digabung)
- Per fase: 1 intro paragraph + 1 Mermaid diagram + 1 tabel detail flow
- Theme toggle (light/dark, localStorage persist)
- Print CSS lengkap (font 10pt, color-adjust:exact, page-break per section, Mermaid SVG scaleable)
- Cross-reference link ke 3 file companion (`overview-awam.html`, `ekosistem-sertifikat.html`, `analisa-gap-benchmark.html`)
- Bahasa Indonesia plain, jargon minimal (define inline kalau perlu)

### OUT OF SCOPE

- Code snippet C# (lihat `index.html` versi teknis dev)
- Endpoint detail / controller signature (lihat `index.html`)
- Gap remediation roadmap (lihat `analisa-gap-benchmark.html`)
- Test automation (visual verify browser only, konsisten file lain di folder)
- Tutorial step-by-step UI (ini diagram referensi, bukan how-to)

---

## 3. Struktur Konten

### Header

- `<h1>` "Flow Sistem Sertifikat Portal HC KPB"
- Subtitle: "Visualisasi 25 flow lengkap — referensi awam untuk Tim HC"
- Audience banner: jelaskan ini diagram referensi untuk Staff HC, tunjuk file companion
- Versi + tanggal

### Mini-nav (sticky)

9 link section: `#sec-master` + 8 fase (`#sec-a` .. `#sec-i`) + theme toggle

### §0 Master Lifecycle Diagram

1 Mermaid besar overview — zones untuk 8 fase. Pembaca dapat orientasi sebelum dive ke detail per fase.

### §A. Terbit / Lahir Sertifikat (6 flow)

Intro: "Sertifikat lahir dari 3 jalur — assessment online, manual upload bukti, atau manual entry assessment external."

**Mermaid flowchart**: 3 jalur terbit + NOT-terbit branch + pre-post sibling coupling

**Tabel 6 baris** (no, nama flow, trigger, endpoint, hasil):
1. Assessment Online → Sertifikat Otomatis
2. HC Upload Bukti Training External (`AddTraining` / `EditTraining`)
3. HC Manual Assessment Entry (`AddManualAssessment` / `EditManualAssessment`)
4. Training Record Bulk Import Excel (`ImportTraining`)
5. Pre-Post Test Sibling Link (flag override)
6. Assessment → NOT-Terbit (3 alasan branch)

### §B. Essay Grading Conditional (3 flow)

Intro: "Kalau assessment ada soal essay, sertifikat belum terbit sebelum HC selesai grading. 2-step proses: Submit per essay → Finalize."

**Mermaid flowchart**: Assessment lulus pilihan ganda → Status PendingGrading → HC SubmitEssayScore per essay → HC FinalizeEssayGrading → cek IsPassed → terbit cert (atau tidak)

**Tabel 3 baris**:
1. Assessment Interim Status PendingGrading (lifecycle state baru)
2. HC Submit Essay Score (`SubmitEssayScore` per essay)
3. HC Finalize Essay Grading (`FinalizeEssayGrading` trigger cert generation)

### §C. Lifecycle Status (2 flow)

Intro: "Status sertifikat dihitung sistem real-time, bukan disimpan di database."

**Mermaid state-diagram**: Aktif → AkanExpired (≤30 hari) → Expired (lewat tanggal) + Permanent (no transition)

**Tabel 2 baris**:
1. Aktif → Akan Expired → Expired (auto real-time)
2. Permanent (no transition)

### §D. Notifikasi (1 flow + hidden gap callout)

Intro: "Notif expiry **bukan cron job** — di-trigger saat user login. Kalau tidak ada user login dalam window kritis, notif tidak terkirim."

**Mermaid flowchart**: User login → HomeController.Index → cek cache 1-jam → kalau expired → TriggerCertExpiredNotificationsAsync → dedup hash → kirim notif HC/Admin

**Alert box merah**: "⚠️ Hidden Gap Operasional — kalau tidak ada user login → notif tidak fired. Recommended: tambah dedicated scheduler (Hangfire / IHostedService) untuk decouple dari user activity."

**Tabel 1 baris**:
1. Cert Expiry Notif — On-Login Passive Trigger

### §E. Renewal (3 flow + 1 gap)

Intro: "Renewal saat ini HC-driven. Self-service coachee belum ada (gap top kritis #1)."

**Mermaid flowchart**: Notif expiry → HC trigger Renewal Certificate menu → Form Renewal → Training/Assessment baru → Sertifikat baru → FK Renewal Chain Link ke lama → Union-Find resolution di view CertificateHistory

**Tabel 4 baris** (3 ada + 1 gap):
1. HC-Trigger Renewal Certificate (`/Admin/RenewalCertificate` + Filter + Group)
2. Renewal Chain Link (FK `RenewsSessionId` / `RenewsTrainingId`)
3. Certificate History Modal (Union-Find chain resolution)
4. ~~Self-Service Coachee Renewal~~ — **BELUM ADA** (gap)

### §F. View / Read (3 flow + RBAC tier)

Intro: "View flow dipengaruhi role-tier scoping L1-L5. L5 coachee lihat data sendiri, L4+ HC/Manager/Admin lihat aggregate Bagian/Unit."

**Mermaid swim-lane**: 3 lane (Coachee / HC / Manager) — apa yang masing-masing lihat

**Tabel 3 baris**:
1. Certificate View + Download PDF (`CMP/Certificate` + `CMP/CertificatePdf`)
2. Coachee Records Unified View (`CMP/Records` + `RecordsWorkerDetail`)
3. HC Dashboard CDP CertificationManagement (`/CDP/CertificationManagement`)

**Alert info**: tabel mini RBAC tier (L1=Admin, L2=HC, L3=Manager, L4=SectionHead, L5=Coachee/Coach + scope view)

### §G. Edit / Manage (4 flow)

Intro: "HC kelola data sertifikat: edit, delete cascade, export, re-issue PDF."

**Mermaid flowchart**: HC action picker → 4 branch (Edit / Delete / Export / Re-issue)

**Tabel 4 baris**:
1. HC Edit Data Sertifikat
2. Hard Delete Training/Assessment + Cascade File Cleanup (DeleteAssessment explicit cascade, DeleteTraining simple, sertifikat FK ON DELETE SET NULL)
3. Export Excel (`/CDP/ExportSertifikatExcel`, `/CMP/ExportRecords`)
4. Re-issue PDF Sertifikat (regenerate tanpa ubah data)

### §H+I. Verifikasi + Audit (1 gap + 1 flow)

Intro: "Verifikasi external belum ada (gap top kritis #2). Audit log saat ini hanya per-action sebagian (gap audit trail defensible)."

**Tabel 2 baris**:
1. ~~External Public Verify QR/URL~~ — **BELUM ADA** (gap)
2. File Log Umum + Per-Action Log (FinalizeEssayGrading rowsAffected-gated, Delete per-action)

**Alert warning**: link ke gap #2 (Public Verify) + #5 (Audit Trail) di analisa-gap-benchmark.

### Footer

- Tanggal update + versi
- Cross-ref 3 companion files

---

## 4. Style Decisions

| Aspek | Pilihan | Alasan |
|-------|---------|--------|
| CSS framework | Bootstrap 5.3 (CDN) | Konsisten file lain |
| Icons | Bootstrap Icons 1.11 (CDN) | Konsisten file lain |
| Diagram | **Mermaid 11 (CDN)** | Rich diagram, konsisten `ekosistem-sertifikat.html`, SVG scaleable PDF-friendly |
| Dark mode | Toggle + localStorage persist + Mermaid theme switch | Mermaid render ulang saat theme change |
| Print | `@media print` lengkap (font 10pt + color-adjust:exact + page-break per section + Mermaid SVG render preserved) | PDF-friendly priority |
| Mini-nav | Sticky top, 9 link | Section banyak, perlu jump nav |
| Width | `max-width: 1100px` | Mermaid butuh ruang lebih lebar dari overview-awam (900px) |

---

## 5. Cross-References

- **Out-link**: setiap callout gap → link ke section spesifik `analisa-gap-benchmark.html` (deep link)
- **Out-link**: footer link ke 3 companion files
- **In-link**: tambah link di `overview-awam.html` §1.4 (footer link existing → expand jadi 2 link: ekosistem + flow-sistem) — task tambahan

---

## 6. Success Criteria

Staff HC bisa:
1. Buka file, scroll master diagram → langsung paham 8 fase lifecycle
2. Click mini-nav per fase → dive ke diagram detail
3. Print/export PDF → diagram render rapi tanpa overflow
4. Pahami hidden gap notif (on-login passive bukan cron)
5. Pahami semua endpoint admin terkait sertifikat (tanpa baca code)

---

## 7. Implementation Notes

- File standalone, no build step
- Mermaid CDN init di footer script, re-render saat theme toggle
- Test: buka browser localhost, cek render Mermaid (semua diagram tampil), responsiveness mobile, dark mode, print preview Ctrl+P
- Tidak ada Playwright automation
- Commit per fase (10-12 commit atomic): scaffold → §0 master → §A → §B → §C → §D → §E → §F → §G → §H+I → footer + cross-ref update di overview-awam → final verify
