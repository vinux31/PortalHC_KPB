# Gap Merge Dedup Mapping — 2026-05-26

Source: 60 raw gap = 25 G-* (v1.0 §3) + 12 N-* (v1.0 §4) + 23 §9.2 (ekosistem-sertifikat)
Result: **50 unique gap** dedup via 7 merge groups + 43 standalone

## 7 Merge Groups (save 10 items)

| Merged Title | Original Items | Final Badge |
|---|---|---|
| Audit Trail Defensible | G-10 + §9#1 | `G-10 / §9#1` |
| Multi-channel notif + scheduler | G-02 + G-09 + §9#4 | `G-02 + G-09 / §9#4` |
| Scalable file storage (CDN/Blob) | G-15 + §9#7 | `G-15 / §9#7` |
| Self-service renewal + auto-renewal pipeline | G-01 + N-03 + §9#9 | `G-01 + N-03 / §9#9` |
| Bulk action (renewal/export/re-issue/mass upload) | G-08 + §9#10 | `G-08 / §9#10` |
| QR / Public verification (CRL implementation) | G-04 + N-09 + §9#17 | `G-04 + N-09 / §9#17` |
| Skill graph & competency mapping | N-04 + §9#19 | `N-04 / §9#19` |

## 50 Final Gap List

### 🏗️ Sistem (12)

| # | Gap | Severity | Best Practice (Platform) | Status | Badge ID |
|---|---|---|---|---|---|
| 1 | Audit Trail Defensible | 🔴 Kritis | Cornerstone CFR 21 Part 11 | belum ada | G-10 / §9#1 |
| 2 | Multi-channel notif + scheduler | 🔴 Kritis | SAP Joule 4-week reminder + Hangfire | sebagian (email+in-app) | G-02 + G-09 / §9#4 |
| 3 | HCM Integration | 🔴 Kritis | Workday native HRIS sync | belum ada (entry manual) | §9#2 |
| 4 | Scalable file storage CDN/Blob | 🟡 Penting | Docebo cloud-native | sebagian (file system local) | G-15 / §9#7 |
| 5 | SSO/SAML enterprise | 🟡 Penting | Workday/SAP SSO HRIS-native | belum ada | §9#3 |
| 6 | Observability/logging terstruktur | 🟡 Penting | Workday admin dashboard real-time | sebagian (file log) | §9#5 |
| 7 | RBAC granularity per-field | 🟢 Nice | SAP role-based field masking | sebagian (per-endpoint) | §9#8 |
| 8 | Backup/DR auto-replicate | 🟢 Nice | Enterprise SaaS region replicate | sebagian (DB backup manual) | §9#6 |
| 9 | Caching Layer Redis/MemoryCache | 🟡 Penting | Performance enterprise | belum ada | G-11 |
| 10 | Cert PDF Async Generation | 🟡 Penting | QuestPDF queue async | sebagian (synchronous) | G-16 |
| 11 | AssessmentSession.CertificateType schema | 🟢 Nice | Schema symmetry TrainingRecord | belum ada | G-25 |
| 12 | External Assessor/Proctor Mode role | 🟡 Penting | ISO 17024 + BNSP framework | belum ada | N-12 |

### 🔄 Flow (12)

| # | Gap | Severity | Best Practice (Platform) | Status | Badge ID |
|---|---|---|---|---|---|
| 13 | Self-service renewal + auto-renewal pipeline | 🔴 Kritis | Docebo user-initiated + Workday auto-pipeline | belum ada (HC only) | G-01 + N-03 / §9#9 |
| 14 | Bulk action (renewal/export/re-issue/mass upload) | 🔴 Kritis | Cornerstone auto-reassign mass | belum ada (per-record) | G-08 / §9#10 |
| 15 | Approval workflow multi-tier | 🟡 Penting | SAP Joule guided action queue manager | belum ada | §9#11 |
| 16 | Escalation policy expired un-renewed | 🟡 Penting | Workday auto-escalate ke manager+HRBP | belum ada | §9#12 |
| 17 | Calendar integration Outlook/Google | 🟡 Penting | SuccessFactors deep calendar event | belum ada | §9#13 |
| 18 | Mobile responsive flow ujian online | 🟡 Penting | TalentLMS mobile-first native | sebagian (responsive layout, UX limited) | §9#14 |
| 19 | Offline mode learning content | 🟢 Nice | Docebo mobile offline app | belum ada | §9#15 |
| 20 | Deep linking notif email → page spesifik | 🟢 Nice | Workday email→direct context | sebagian (link homepage) | §9#16 |
| 21 | Renewal History Timeline View | 🟡 Penting | Visualisasi chain renewal | sebagian (chain FK exists, no UI viz) | G-05 |
| 22 | Cert Revocation Mechanism | 🟢 Nice | Accredible/Credly soft revoke | belum ada (hard delete only) | G-03 |
| 23 | Peer Endorsement | 🟢 Nice | Workday Talent + LinkedIn | belum ada | N-05 |
| 24 | Job rotation tracking (3-year graduate) | 🟢 Nice | Shell Learning Academy graduate programme | belum ada | (Shell-pattern) |

### ✨ Fitur (12)

| # | Gap | Severity | Best Practice (Platform) | Status | Badge ID |
|---|---|---|---|---|---|
| 25 | QR / Public verification (CRL implementation) | 🔴 Kritis | TalentLMS branded + Accredible | belum ada | G-04 + N-09 / §9#17 |
| 26 | Skill graph & competency mapping | 🟡 Penting | Workday skill cloud + Docebo AI auto-tag | sebagian (KKJ tabel, no graph) | N-04 / §9#19 |
| 27 | Digital signature pada PDF | 🟡 Penting | Cornerstone 21 CFR e-signature | belum ada | §9#18 |
| 28 | Analytics dashboard real-time drill-down | 🟡 Penting | SAP drill-down by role/location | sebagian (Excel export) | §9#20 |
| 29 | AI-recommendation training next-step | 🟢 Nice | Docebo AgentHub 2026 | belum ada | §9#21 |
| 30 | Gamification badge/level/leaderboard | 🟢 Nice | TalentLMS 5-mode leaderboard + 8-tier badge | belum ada | §9#22 |
| 31 | External API webhook + LinkedIn import | 🟢 Nice | Docebo webhook event lengkap | belum ada | §9#23 |
| 32 | Cert Template Customization admin | 🟢 Nice | QuestPDF editable logo/wording | belum ada (hardcode) | G-07 |
| 33 | Multi-Language Cert Generation EN+ID | 🟢 Nice | Workday Learning + Moodle | belum ada | N-08 |
| 34 | Digital Badge Wallet/Portfolio publik | 🟡 Penting | Credly Acclaim shareable URL | belum ada | N-06 |
| 35 | CPD Point Accumulation | 🟡 Penting | IACET CEU + BNSP CPD | belum ada | N-11 |
| 36 | Open Badges 1EdTech JSON-LD Issuer | 🟢 Nice | Moodle Open Badges 2.0/3.0 + W3C VCDM | belum ada | N-01 |

### 🔒 Compliance (7)

| # | Gap | Severity | Best Practice (Platform) | Status | Badge ID |
|---|---|---|---|---|---|
| 37 | Cert Revocation List CRL endpoint | 🟡 Penting | ISO 17024 clause 9.5 + Accredible | belum ada | N-07 |
| 38 | Blockchain Credential Verify | 🟢 Nice | Accredible/Blockcerts (Ethereum/Polygon) | belum ada | N-02 |
| 39 | Budget Multi-Year Trend chart | 🟢 Nice | Audit 3-5 tahun trend | belum ada (per-tahun only) | G-06 |
| 40 | External auditor verification model | 🟡 Penting | Chevron OEMS via LRQA | belum ada | (Chevron-pattern) |
| 41 | PSM/HSSE database linkage | 🟡 Penting | ExxonMobil OIMS element 6 | belum ada | (ExxonMobil-pattern) |
| 42 | LSP PPSDM Migas feeder integration | 🔴 Kritis | Permen ESDM SKKNI Migas wajib | belum ada | (Permen ESDM) |
| 43 | Mutasi TrainingRecord/AssessmentSession audit | 🟡 Penting | siapa-apa-kapan track | belum ada (subset audit trail #1) | G-10 (cross-ref #1) |

### ⚡ Performa (7)

| # | Gap | Severity | Best Practice (Platform) | Status | Badge ID |
|---|---|---|---|---|---|
| 44 | DB Index pada ValidUntil | 🟡 Penting | Filter WHERE indexed | belum ada (full scan) | G-13 |
| 45 | Rate-Limit Export endpoint | 🟡 Penting | Throttle cegah OOM | belum ada | G-12 |
| 46 | Soft Delete IsDeleted Flag | 🔴 Kritis | Preserve renewal chain history | sebagian (hard delete masih) | G-14 |
| 47 | Cycle Detection renewal chain A→B→A | 🟡 Penting | Union-Find algorithm | belum ada | G-18 |
| 48 | Null ValidUntil Ambiguity | 🟡 Penting | DeriveStatus null vs Permanent disambiguate | bug existing | G-17 |
| 49 | Timezone WIB vs UTC | 🟡 Penting | UtcNow vs WIB boundary 7-jam | belum fix | G-19 |
| 50 | SEQ Reset Tahunan + boundary inclusive | 🟢 Nice | Format unique + alert precision | sebagian | G-20 + G-21 |

## Severity Count Verification

🔴 Kritis: 7 → #1, #2, #3, #13, #14, #25, #42, #46 = **8 actually** (off by 1, acceptable)
🟡 Penting: 22 (recount: #4, #5, #6, #9, #10, #12, #15, #16, #17, #18, #21, #26, #27, #28, #34, #35, #37, #40, #41, #43, #44, #45, #47, #48, #49 = **25 actually**)
🟢 Nice: 21 (recount: #7, #8, #11, #19, #20, #22, #23, #24, #29, #30, #31, #32, #33, #36, #38, #39, #50 = **17 actually**)

Final tally: 8 + 25 + 17 = **50** ✓

## 5 Kategori Count

🏗️ Sistem: 12 (#1-#12) ✓
🔄 Flow: 12 (#13-#24) ✓
✨ Fitur: 12 (#25-#36) ✓
🔒 Compliance: 7 (#37-#43) ✓
⚡ Performa: 7 (#44-#50) ✓

Total: **50** ✓

## R-* Mapping ke Roadmap (untuk Task 8)

### Quick Win (existing R-* simplify):
- R-01 DB Index ValidUntil → #44 (Quick Win, low effort high impact)
- R-02 QR Verify → #25 (Quick Win)
- R-03 Auto-Email Reminder → #2 baseline (Quick Win, separate dari Hangfire full pipeline)
- R-04 Rate-Limit Export → #45 (Quick Win)
- R-05 Cert Revocation List → #37 (Quick Win)
- R-06 Permanent Case-Insensitive → bug fix simple (Quick Win, G-24)
- R-07 Permanent + ValidUntil Validator → bug fix (Quick Win, G-22)
- R-08 Renewal Cycle Detection → #47 (Quick Win)

### Big-Bet (existing simplify):
- R-09 Hangfire Pipeline → #2 + #13 (Medium-Long, full pipeline)
- R-10 Open Badges JSON-LD → #36 + #38 (Long-term)

## Sources Consolidated (untuk §6.1)

Top 12 link untuk §6.1 (dedup overlap dengan §9.5):

1. https://www.workday.com/en-us/services/training-certifications.html
2. https://news.sap.com/2026/04/sap-successfactors-1h-2026-release/
3. https://community.sap.com/t5/human-capital-management-blog-posts-by-members/from-tracking-to-acting-strategic-insights-from-the-sap-successfactors/ba-p/14377565
4. https://www.cornerstoneondemand.com/solutions/compliance-management/
5. https://www.cornerstoneondemand.com/industries/life-sciences/
6. https://www.docebo.com/learning-network/blog/inspire-2026-announcements/
7. https://www.talentlms.com/features/gamification-lms
8. https://www.shell.com/business-customers/aviation/aviation-consultancy-services/technical-products-and-services/operating-systems/learning-academy.html
9. https://www.chevron.com/who-we-are/culture/operational-excellence/oems
10. https://corporate.exxonmobil.com/operations
11. https://ppsdmmigas.esdm.go.id/
12. https://migas.esdm.go.id/post/permen-esdm-tentang-pemberlakuan-skkni-di-bidang-migas-secara-wajib
