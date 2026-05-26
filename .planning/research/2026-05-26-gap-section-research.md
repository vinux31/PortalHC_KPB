# Research §9 Gap & Best Practice — 2026-05-26

Source notes untuk implementasi §9 di ekosistem-sertifikat.html. Berdasarkan WebSearch 5 platform (per 2026-05).

---

## 1. Workday Learning (HRIS Enterprise)

### Sources
- https://www.workday.com/en-us/services/training-certifications.html (vendor docs — Training & Certifications)
- https://cloudshinepro.com/workday-learning/ (independent guide — HR Leaders 2026)

### Key features (certification management)
- Compliance tracking otomatis: report siapa butuh recertification, siapa miss deadline, gap per department
- Auto-reminder pre-expired ke employee + manager
- Auto-enrollment rule berdasarkan job profile, location, employee attribute
- HCM integration native — promosi/pindah dept update training requirement otomatis tanpa admin
- Learning transcript: history lengkap (course, score, cert, date), downloadable
- Skill cloud: mapping ke job profile + auto-update skill record + identify gaps + auto-assign remediation
- Cert renewal: Pro setiap 2 tahun via proctored exam, Specialty/PM via non-proctored online

### Card content (untuk §9.1)
> Workday Learning terintegrasi langsung dengan modul HRIS, sehingga sertifikat, training, dan kompetensi pekerja terupdate otomatis saat pekerja pindah jabatan atau departemen. Punya skill cloud untuk mapping kompetensi ke jabatan + auto-assign remediation training saat ada gap. Compliance tracking real-time per departemen dengan reminder otomatis pre-expired.
>
> Relevansi Portal HC KPB: Pertamina punya ekosistem HR yang kompleks dengan KKJ per jabatan — pattern HRIS-Learning native Workday ideal untuk integrasi data pekerja.

---

## 2. SAP SuccessFactors Learning (HRIS Enterprise)

### Sources
- https://news.sap.com/2026/04/sap-successfactors-1h-2026-release/ (vendor blog — 1H 2026 Release)
- https://community.sap.com/t5/human-capital-management-blog-posts-by-members/from-tracking-to-acting-strategic-insights-from-the-sap-successfactors/ba-p/14377565 (SAP community — Learning 1H 2026)

### Key features
- Track completion + cert + due date dengan reporting suitable internal control & audit
- Compliance monitoring by population, job role, location
- Re-certification cycle management terkonfigurasi
- **Joule AI agent (1H 2026)**: proactive, surfacing siapa at-risk + kenapa, send pre-emptive reminder **4 minggu** ahead deadline, package pending approval ke guided action queue manager
- Shift dari reactive tracking ke proactive execution

### Card content
> SAP SuccessFactors Learning fokus pada compliance enterprise dengan reporting drill-down by population/role/location. Joule AI agent (rilis 1H 2026) bertindak proaktif: identifikasi siapa at-risk, kirim reminder 4 minggu sebelum deadline, dan bundle pending approval jadi action queue siap-pakai untuk manager.
>
> Relevansi Portal HC KPB: SAP dipakai luas di BUMN Indonesia termasuk Pertamina — pattern compliance + audit reporting SuccessFactors familiar untuk tim HC.

---

## 3. Cornerstone Learning (LMS Dedicated)

### Sources
- https://www.cornerstoneondemand.com/solutions/compliance-management/ (vendor docs — Compliance Management)
- https://www.cornerstoneondemand.com/industries/life-sciences/ (vendor docs — Validated LMS Pharma)

### Key features
- **Defensible audit trail**: timestamp + IP address + detailed completion record, meets regulatory documentation standards
- Auto-monitor completion + test score + cert expiration per assigned compliance course
- Auto-alert pre-deadline + auto-reassignment untuk recurring training
- Export multi-format untuk audit external (internal review + regulator inspection)
- **21 CFR Part 11 compliant** untuk life sciences/regulated — Electronic Signatures + audit trail via configuration

### Card content
> Cornerstone Learning dedicated LMS dengan fokus compliance regulated industry. Audit trail "defensible": setiap interaksi training tercatat dengan timestamp, IP address, dan score — siap untuk inspeksi regulator. Standar 21 CFR Part 11 (life sciences) didukung lewat konfigurasi e-signature + audit trail.
>
> Relevansi Portal HC KPB: Industri Migas/Energy = regulated sector. Pattern rigorous audit trail Cornerstone cocok untuk compliance audit Pertamina (BPK, internal audit, regulator).

---

## 4. Docebo (Cloud LMS Modern)

### Sources
- https://help.docebo.com/hc/en-us/articles/22057130562066-Certificates-and-certifications (vendor docs — Certificates)
- https://www.docebo.com/learning-network/blog/inspire-2026-announcements/ (vendor blog — Inspire 2026: AgentHub)

### Key features
- Cert expiration + renewal config flexible
- User-initiated renewal **anytime** jika course status Complete + cert allow renewal
- Auto-assign cert via course, learning plan, atau external training activity
- Automation renewal reminder + compliance tracking + advanced analytics
- **AI skill tagging**: AI auto-analyze content saat upload + identify relevant skill dari catalog + auto-assign
- Webhook event `course.updated` triggered saat skill ditambah/dihapus
- **AgentHub (2026)**: agentic AI + skill intelligence + enterprise knowledge

### Card content
> Docebo cloud LMS modern dengan fokus AI + automation. AI otomatis tag skill ke konten saat upload, certification renewal bisa di-trigger user kapan saja (self-service), webhook event untuk skill changes terintegrasi sistem external. AgentHub 2026 satukan AI agent + skill intelligence dalam satu platform.
>
> Relevansi Portal HC KPB: Pattern AI auto-tag skill + self-service renewal bisa diadopsi bertahap ke modul CDP/CMP, terutama mapping otomatis KKJ ↔ sertifikat.

---

## 5. TalentLMS (Mid-Market LMS)

### Sources
- https://www.talentlms.com/features/gamification-lms (vendor docs — Gamification)
- https://help.talentlms.com/hc/en-us/articles/9651488201628-A-guide-to-the-TalentLMS-gamification-features (support docs — gamification guide)

### Key features
- **Gamification**: Points (per login, per unit complete), Badges (8 milestones: 4/8/16/32/64/128/256/512 logins), Leaderboards (5 ranking modes: Levels/Points/Badges/Courses/Certifications), Rewards (convert virtual ke discount)
- Personalized certificate dengan expiration date
- **Branded certificate** dengan logo + customization
- Automated re-certification
- Mobile-first UX simplicity

### Card content
> TalentLMS mid-market LMS dengan fokus simplicity + engagement. Gamification mendalam (point/badge/leaderboard/reward), branded certificate yang bisa di-customize logo + design, automated re-certification flow. UX mobile-first dengan onboarding cepat.
>
> Relevansi Portal HC KPB: Pattern gamification + branded certificate bisa naikkan engagement pekerja untuk mandatory training (K3, HSE), terutama kalau ada leaderboard per section.

---

## Synthesis — 23 Gap Final List

Sebaran: 8 Sistem + 8 Flow + 7 Fitur = 23. Severity: 6 🔴 + 10 🟡 + 7 🟢.

### 🏗️ Sistem (8 gap)

| # | Gap | Severity | Best Practice (Platform) | Status Portal HC |
|---|---|---|---|---|
| 1 | Audit trail defensible (timestamp+IP+completion per CFR 21 Part 11) | 🔴 Kritis | Cornerstone: defensible audit trail dengan IP + signature digital | belum ada |
| 2 | HCM integration / employee master sync | 🔴 Kritis | Workday: native HRIS sync, promosi update training req otomatis | belum ada (entry manual) |
| 3 | SSO/SAML enterprise integration | 🟡 Penting | Workday/SAP: SSO HRIS-native | belum ada |
| 4 | Multi-channel notification (push/SMS/email) | 🟡 Penting | SAP Joule: pre-emptive 4-week reminder + manager queue | sebagian (in-app+email) |
| 5 | Observability/logging terstruktur (admin console) | 🟡 Penting | Workday: admin dashboard real-time | sebagian (file log) |
| 6 | Backup/DR strategy auto-replicate | 🟢 Nice | Enterprise SaaS auto-replicate region | sebagian (DB backup manual) |
| 7 | Scalable file storage (object storage) | 🟢 Nice | Docebo/Cornerstone: cloud-native blob | belum ada (file system local) |
| 8 | RBAC granularity per-field (data masking) | 🟢 Nice | SAP: role-based field masking + redaction | sebagian (per-endpoint) |

### 🔄 Flow (8 gap)

| # | Gap | Severity | Best Practice (Platform) | Status Portal HC |
|---|---|---|---|---|
| 9 | Self-service renewal trigger oleh pekerja | 🔴 Kritis | Docebo: user-initiated renewal anytime | belum ada (HC only) |
| 10 | Bulk action (renewal/export/re-issue dengan filter) | 🔴 Kritis | Cornerstone: auto-reassign mass + recurring | belum ada (per-record) |
| 11 | Approval workflow multi-tier (manager queue) | 🟡 Penting | SAP Joule: guided action queue manager | belum ada |
| 12 | Escalation policy expired un-renewed | 🟡 Penting | Workday: auto-escalate ke manager+HRBP | belum ada |
| 13 | Calendar integration (Outlook/Google) | 🟡 Penting | SuccessFactors: deep calendar event | belum ada |
| 14 | Mobile responsive flow ujian online | 🟡 Penting | TalentLMS: mobile-first native | sebagian (layout responsive, UX limited) |
| 15 | Offline mode learning content | 🟢 Nice | Docebo: mobile offline app | belum ada |
| 16 | Deep linking notif email → page spesifik | 🟢 Nice | Workday: email→direct context | sebagian (link homepage) |

### ✨ Fitur (7 gap)

| # | Gap | Severity | Best Practice (Platform) | Status Portal HC |
|---|---|---|---|---|
| 17 | QR code / public verify URL sertifikat | 🔴 Kritis | TalentLMS: branded cert + public link | belum ada |
| 18 | Digital signature pada sertifikat PDF | 🟡 Penting | Cornerstone: 21 CFR e-signature config | belum ada |
| 19 | Skill graph & competency mapping (visualisasi) | 🟡 Penting | Workday skill cloud + Docebo AI auto-tag | sebagian (KKJ tabel, no graph) |
| 20 | Analytics dashboard real-time drill-down | 🟡 Penting | SAP: reporting drill-down by role/location | sebagian (Excel export) |
| 21 | AI-recommendation training next-step | 🟢 Nice | Docebo AgentHub 2026 | belum ada |
| 22 | Gamification (badge/level/leaderboard) | 🟢 Nice | TalentLMS: 5-mode leaderboard + 8 badge tier | belum ada |
| 23 | External API webhook (LinkedIn/HRIS sync) | 🟢 Nice | Docebo webhook event lengkap | belum ada |

---

## Synthesis — Top-5 Gap Kritis (deep-dive accordion §9.3)

Filter dari 6 gap 🔴, drop SSO (#3 — pure IT infra, less HC-hands-on). Final 5 untuk awam-friendly impact:

### 1. Self-Service Renewal Portal (gap #9)
- **Current State**: Renewal certificate hanya bisa di-trigger HC/Admin via menu Renewal Certificate di Admin Panel. Pekerja yang sertifikatnya akan expired hanya bisa nunggu notif + tindak lanjut HC. Tidak ada visibility "saya bisa renew sendiri kapan?".
- **Best Practice External**: Docebo mengizinkan user trigger renewal **kapan saja** (selama status course Complete + cert config allow renewal). Pekerja proaktif, beban HC turun.
- **Rekomendasi**: (1) Tambah menu "Renew Sertifikat Saya" di CMP/CDP untuk Coachee, (2) Allowlist tipe sertifikat yang boleh self-renew (non-Permanent + IsPassed), (3) Setelah self-renew → trigger workflow approval ke Coach/HC, (4) Log audit per self-renew, (5) Notif Coach saat ada self-renew request.
- **Effort**: Medium (3-6 bulan)

### 2. Public Verification (QR/URL) (gap #17)
- **Current State**: Sertifikat PDF generate dengan nomor `KPB/SEQ/ROMAWI/YYYY` tapi tidak ada cara external (rekruter, vendor, BPK) verify keaslian sertifikat. Pihak eksternal harus phone/email HC manual.
- **Best Practice External**: TalentLMS branded certificate dengan public URL + verification. Recipient bisa share link, third-party verify status sertifikat real-time tanpa login.
- **Rekomendasi**: (1) Generate UUID per sertifikat saat issue, (2) Public endpoint `/verify/{uuid}` read-only (no auth), (3) Render halaman verify simple (nama, kompetensi, status, valid until — no detail PII), (4) Embed QR code di PDF sertifikat → arahkan ke URL verify, (5) Rate limit endpoint mencegah enumeration.
- **Effort**: Quick Win (1-3 bulan)

### 3. Bulk Action Suite (gap #10)
- **Current State**: Operasi sertifikat hanya per-record: HC harus klik satu-per-satu untuk renewal, re-issue, atau export individual. Dengan ribuan pekerja, ini bottleneck operasional.
- **Best Practice External**: Cornerstone mendukung auto-reassign mass untuk recurring training + bulk export by population/role/location. Workday auto-enrollment rule menjalankan mass-action tanpa intervention HC.
- **Rekomendasi**: (1) Tambah bulk-checkbox di list CDP Certification Management, (2) Bulk renewal action (1 form → ratusan record), (3) Bulk re-issue PDF (regenerate dengan template baru), (4) Bulk export Excel dengan filter advance (status, section, valid range), (5) Batch progress indicator + email summary saat selesai.
- **Effort**: Medium (3-6 bulan)

### 4. HCM Integration (gap #2)
- **Current State**: Data pekerja (NIP, nama, section, jabatan) di-entry manual atau diimpor periodik via Excel. Saat pekerja pindah section/jabatan, status sertifikat tidak otomatis ter-update.
- **Best Practice External**: Workday HRIS-Learning native — saat pekerja promosi, training requirement otomatis update tanpa admin intervention. Single source of truth.
- **Rekomendasi**: (1) Integrasi API ke sistem HR Pertamina (SAP HCM atau Oracle HCM), (2) Sync employee master nightly (NIP, jabatan, section, status active), (3) Saat detect pindah section → auto-update RBAC scope + flag sertifikat affected, (4) Webhook saat employee inactive → freeze sertifikat (tidak generate notif), (5) Log full audit perubahan source-of-truth.
- **Effort**: Long-term (>9 bulan, butuh IT Pertamina pusat)

### 5. Audit Trail Defensible (gap #1)
- **Current State**: Aksi di Portal HC ter-log umum via file log aplikasi, tapi tidak ada audit trail per-record yang siap diserahkan ke regulator (BPK, audit internal Pertamina). Sertifikat tidak punya record "siapa edit, kapan, dari IP mana".
- **Best Practice External**: Cornerstone implement 21 CFR Part 11 — defensible audit trail dengan timestamp + IP + signature digital, exportable PDF untuk regulator. Setiap event training tercatat permanen.
- **Rekomendasi**: (1) Tabel `SertifikatAuditLog` dedicated (UserId, Action, SertifikatId, OldValue, NewValue, Timestamp, IP, UserAgent), (2) Trigger insert log di tiap CRUD sertifikat + renewal + re-issue, (3) Endpoint admin export audit trail per-sertifikat ke PDF (BPK-ready), (4) Retention policy ≥7 tahun (regulasi Pertamina), (5) Read-only — no delete/update ke audit table.
- **Effort**: Medium (3-6 bulan)

---

## Synthesis — Roadmap 3-Bucket

### Quick Win (1-3 bulan, config/UI tweak)
1. **QR verify URL sertifikat** — generate QR + endpoint public read-only, embed di PDF. Effort kecil, impact besar untuk credibility external
2. **Email template polish + deep link** — re-design template HTML notif, link langsung ke renewal/detail page (bukan homepage). Reduce HC inbound query "saya buka mana?"
3. **Bulk export filter advance di CDP** — extend existing Export Excel dengan filter (status, section, valid range, renewal chain). Tambah opsi kolom output
4. **Analytics dashboard widget** — sum/group by status + section + tipe di homepage CDP. Top-10 expired list. Bootstrap card sederhana
5. **Branded certificate template improvement** — tambah logo Pertamina KPB, watermark, layout polish. PDF template upgrade

### Medium (3-9 bulan, new feature 1-2 controller)
1. **Self-Service Renewal Portal pekerja** — new controller + view + workflow approval. Beban HC turun drastis
2. **Bulk action UI HC (mass renewal/re-issue)** — bulk-checkbox + batch processor + progress indicator
3. **Skill graph foundation (KKJ ↔ sertifikat mapping)** — table mapping + visualisasi tree per pekerja. Foundation untuk skill gap analysis nanti
4. **Multi-channel notification (push/SMS Twilio/Firebase)** — extend notif engine. Kritis untuk SDM lapangan tidak rajin buka email
5. **Audit trail defensible (CFR-style log)** — tabel dedicated + trigger CRUD + export PDF per-sertifikat. Compliance audit internal+external

### Long-term (>9 bulan, arsitektur baru / external system)
1. **HCM integration / employee master sync (SAP HCM Pertamina)** — single source of truth. Butuh koordinasi IT pusat
2. **SSO/SAML enterprise auth (Pertamina AD)** — eliminasi password lokal, audit otomatis
3. **AI skill recommendation + auto-tag content** — Docebo-pattern. Butuh ML pipeline atau external API (OpenAI Skill Embed)
4. **Object storage migration (Blob/S3)** — scalability + DR. Migrasi file local → cloud-native
5. **External API webhook (LinkedIn import + HRIS bidirectional sync)** — extensibility long-term untuk eksposur kompetensi pekerja ke ekosistem external

---

## Top Sources for §9.5 References (final 7 URL)

1. Workday Learning Training & Certifications — https://www.workday.com/en-us/services/training-certifications.html
2. SAP SuccessFactors 1H 2026 Release (Joule AI for Learning) — https://news.sap.com/2026/04/sap-successfactors-1h-2026-release/
3. Cornerstone Compliance Management Solution — https://www.cornerstoneondemand.com/solutions/compliance-management/
4. Docebo Inspire 2026: AgentHub + Skills Intelligence — https://www.docebo.com/learning-network/blog/inspire-2026-announcements/
5. TalentLMS Gamification Features — https://www.talentlms.com/features/gamification-lms
6. SAP Community: From Tracking to Acting (Learning 1H 2026) — https://community.sap.com/t5/human-capital-management-blog-posts-by-members/from-tracking-to-acting-strategic-insights-from-the-sap-successfactors/ba-p/14377565
7. Cornerstone Validated LMS for Life Sciences (21 CFR Part 11) — https://www.cornerstoneondemand.com/industries/life-sciences/
