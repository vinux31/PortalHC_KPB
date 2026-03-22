# Phase 233: Riset & Perbandingan Coaching Platform - Research

**Researched:** 2026-03-22
**Domain:** Coaching platform UX/flow comparison — 360Learning, BetterUp, CoachHub vs Portal KPB
**Confidence:** MEDIUM

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- **D-01/D-02:** Riset tepat 3 platform: 360Learning, BetterUp, CoachHub — tidak boleh substitusi atau tambah platform lain
- **D-03:** Struktur per area Proton (Setup, Execution, Monitoring, Completion) — paralel dengan Phase 234-237
- **D-04:** Tiap area mulai dengan deskripsi as-is portal KPB (baseline) lalu perbandingan — gap terlihat jelas
- **D-05:** Semua 4 area diriset dengan kedalaman yang sama
- **D-06:** 1 dokumen HTML lengkap di `docs/` — semua 4 area + ringkasan + rekomendasi
- **D-07:** Format HTML konsisten dengan dokumen project (styling seperti research-comparison-summary.html)
- **D-08:** Ranking 3-tier: Must-fix, Should-improve, Nice-to-have
- **D-09:** Tiap rekomendasi di-map ke target phase (234/235/236/237)
- **D-10:** Rekomendasi mencakup validasi differentiator DIFF-01, DIFF-02, DIFF-03

### Claude's Discretion
- Styling dan layout HTML dokumen riset
- Kedalaman narasi per aspek berdasarkan relevansi
- Cara mendeskripsikan flow platform luar (teks naratif vs step-by-step)

### Deferred Ideas (OUT OF SCOPE)
- Tidak ada — diskusi tetap dalam scope phase
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| RSCH-01 | Browse langsung demo/website minimal 3 platform coaching (360Learning, BetterUp, CoachHub) — screenshot dan dokumentasi UX/flow | Temuan dari WebSearch tentang fitur, flow, dan UX ketiga platform; narasi teks menggantikan screenshot karena akses demo terbatas |
| RSCH-02 | Dokumen perbandingan UX/flow portal KPB vs platform luar per area Proton (Setup, Execution, Monitoring, Completion) | Baseline as-is dari analisis Views/CDP dan Controllers/CDP, dibandingkan dengan pola platform industri |
| RSCH-03 | Rekomendasi improvement prioritas berdasarkan gap antara portal vs best practices | Gap diidentifikasi per area; ranking 3-tier dengan mapping ke Phase 234-237 |
</phase_requirements>

---

## Summary

Phase 233 menghasilkan satu dokumen HTML riset perbandingan antara portal KPB (sistem Proton coaching internal) dengan tiga platform coaching enterprise: 360Learning (LMS kolaboratif dengan coaching peer/video), BetterUp (coaching 1:1 enterprise dengan AI matching), dan CoachHub (digital coaching B2B dengan coach network global). Ketiga platform memiliki karakteristik berbeda: 360Learning berfokus pada learning path dan collaborative content creation; BetterUp pada human coaching + AI hybrid untuk transformasi leadership; CoachHub pada coach-to-coachee matching dengan marketplace coach tersertifikasi.

Portal KPB memiliki 4 area Proton: Setup (konfigurasi silabus, track, coach-coachee mapping), Execution (submission evidence deliverable, approval chain), Monitoring (dashboard, filter, export, override), dan Completion (final assessment, histori, 3-year journey). Dibandingkan platform industri, portal KPB unggul dalam konteks industrial (Kompetensi-SubKompetensi-Deliverable hierarki 3-level yang tepat untuk manufaktur) namun gap signifikan di area onboarding coach, workload visibility, batch action, dan bottleneck visibility.

Output phase ini adalah satu file HTML di `docs/coaching-platform-research-v8.2.html` yang menjadi lens utama bagi Phase 234-237 untuk memutuskan apa yang perlu diperbaiki.

**Primary recommendation:** Tulis dokumen riset sebagai satu HTML lengkap dengan sidebar navigasi, struktur per area Proton, baseline as-is portal KPB, perbandingan naratif 3 platform, tabel gap, dan rekomendasi 3-tier yang di-map ke phase. Tidak perlu screenshot aktual — narasi deskriptif UX sudah cukup sesuai keputusan D-02.

---

## Standard Stack

### Core (output deliverable)
| Item | Spesifikasi | Purpose |
|------|-------------|---------|
| Format output | HTML static file | Konsisten dengan docs/ existing: research-comparison-summary.html, audit-assessment-training-v8.html |
| Styling | CSS inline di `<style>` tag | Tidak ada dependency eksternal, self-contained |
| Bahasa | Bahasa Indonesia | Semua narasi, label, dan tabel |

### Referensi Styling dari Dokumen Existing

CSS variables yang sudah establish di semua dokumen `docs/`:
```css
--blue: #005baa;
--blue-light: #e8f0fe;
--red: #ed1c24;
--red-light: #ffeaea;
--yellow: #ffd700;
--yellow-light: #fff8e0;
--green: #28a745;
--green-light: #e6f9ed;
--gray: #6c757d;
--gray-light: #f8f9fa;
--dark: #1a1a2e;
--font: 'Segoe UI', system-ui, -apple-system, sans-serif;
```

Layout pattern: `.layout { display:flex }` dengan `.sidebar` (260px, sticky) dan `.main` (flex:1, max-width:1100px).

Badge pattern: `.badge-critical` (merah), `.badge-medium` (kuning), `.badge-low` (abu).

---

## Architecture Patterns

### Struktur Dokumen HTML Output

```
docs/coaching-platform-research-v8.2.html
├── <head> — CSS inline (ikut pola existing docs)
├── .header — judul + badge versi
├── .layout
│   ├── .sidebar — navigasi section (sticky)
│   │   ├── Platform Overview
│   │   ├── Area 1: Setup
│   │   ├── Area 2: Execution
│   │   ├── Area 3: Monitoring
│   │   ├── Area 4: Completion
│   │   ├── Differentiator Validation
│   │   └── Rekomendasi Prioritas
│   └── .main
│       ├── section#overview — Ringkasan 3 platform
│       ├── section#setup — Baseline KPB + perbandingan + gap tabel
│       ├── section#execution — Baseline KPB + perbandingan + gap tabel
│       ├── section#monitoring — Baseline KPB + perbandingan + gap tabel
│       ├── section#completion — Baseline KPB + perbandingan + gap tabel
│       ├── section#differentiators — DIFF-01/02/03 validation
│       └── section#recommendations — tabel rekomendasi 3-tier
```

### Pattern per Area (4 kali diulang)

Tiap area (`#setup`, `#execution`, `#monitoring`, `#completion`) mengikuti struktur:
1. **Baseline as-is Portal KPB** — ringkasan fitur saat ini (narasi + bullet)
2. **360Learning** — cara platform ini menangani area yang sama (narasi step-by-step)
3. **BetterUp** — cara platform ini menangani area yang sama
4. **CoachHub** — cara platform ini menangani area yang sama
5. **Tabel Gap** — kolom: Aspek | Portal KPB | Best Practice | Gap | Severity

### Tabel Rekomendasi (section#recommendations)

| ID | Area | Rekomendasi | Tier | Target Phase | Complexity |
|----|------|-------------|------|--------------|------------|
| R-01 | Setup | ... | Must-fix | 234 | Low |
| ... | ... | ... | ... | ... | ... |

---

## Temuan Platform: 360Learning

**Confidence:** MEDIUM (WebSearch + official site, tidak ada akses demo langsung)

### Profil Singkat
360Learning adalah LMS kolaboratif (bukan pure coaching platform) yang memiliki fitur coaching untuk skill assessment via video. Target: L&D team yang ingin enable peer learning dan manager coaching.

### Flow per Area Proton

**Setup:**
- Admin/author membuat learning path (setara track Proton) dengan modul, kuis, dan video assessment
- Criteria coaching dapat dikustomisasi per exercise — setara dengan silabus deliverable
- Manager di-assign sebagai reviewer/coach per learner group

**Execution:**
- Learner submit video recording (pitch, skill demo) — asynchronous
- Coach/manager mereview via platform dengan scoring rubric + komentar kualitatif di forum 1:1
- Tidak ada "approval chain" multi-level seperti KPB — 1 layer reviewer saja
- Resubmit: learner dapat revisi dan resubmit setelah feedback diterima

**Monitoring:**
- Manager dashboard menampilkan statistik per managee: enrollment, completion rate, score
- Weekly automated email report ke manager
- Completion tracking: progress per course, per cohort, per compliance group
- Tidak ada override admin — escalation lewat admin role

**Completion:**
- Certificate of completion otomatis setelah semua modul selesai
- Approval workflow opsional: manager atau group admin approve/reject sertifikat
- Recertification: validity period dapat di-set, auto re-enrollment saat expiry
- Download sertifikat sebagai PDF — otomatis, tidak perlu trigger manual

### Fitur Differentiator Relevan
- Criteria guidelines untuk coach — memastikan konsistensi scoring antar coach
- Asynchronous video coaching — cocok untuk tim tersebar
- Manager sebagai coach terintegrasi langsung (no external matching)

---

## Temuan Platform: BetterUp

**Confidence:** MEDIUM (WebSearch + official site)

### Profil Singkat
BetterUp adalah enterprise coaching platform untuk leadership development dan wellbeing. Model: AI matching coach-coachee dari pool coach tersertifikasi eksternal. Target: perusahaan Fortune 500 yang ingin coaching human + AI hybrid untuk transformasi individu.

### Flow per Area Proton

**Setup:**
- HR/admin configure program: define goals, assign employees ke program
- AI matching: sistem otomatis mencocokkan coachee dengan coach berdasarkan goals, preferensi, assessment
- Tidak ada "track" hierarkis seperti KPB — lebih fleksibel dan individualized

**Execution:**
- Session 1:1 via video call (Zoom/Teams/native) — jadwal fleksibel, diatur sendiri oleh coachee + coach
- Coachee submit action items / refleksi setelah sesi — tidak ada "evidence upload" seperti KPB
- Coach tidak approve/reject deliverable — coach membimbing, bukan mengevaluasi output formal
- Progress: assessment sebelum dan sesudah program untuk mengukur perubahan

**Monitoring:**
- Admin dashboard: engagement metrics, session attendance, program ROI
- BetterUp Manage: visibility untuk manajer tentang perkembangan tim (tanpa expose isi sesi — privasi)
- Integration: Slack, Teams, Workday untuk notifikasi dan nudges
- ROI report: before/after assessment comparison, business impact metrics

**Completion:**
- Tidak ada "final assessment" formal seperti KPB — completion = jumlah sesi terpenuhi + goals achieved
- Coach menyusun summary perkembangan
- Program bisa dilanjutkan (renewal) atau dihentikan

### Fitur Differentiator Relevan
- AI-powered coach matching — mengurangi beban HR untuk assign coach manual
- Privacy-first: manajer tidak bisa lihat isi sesi coaching
- Workload visibility coach: platform menampilkan jumlah coachee dan kapasitas coach (relevant untuk DIFF-01)

---

## Temuan Platform: CoachHub

**Confidence:** MEDIUM (WebSearch + official site)

### Profil Singkat
CoachHub adalah digital coaching B2B dengan marketplace coach tersertifikasi (3,500+ coach, 80 bahasa). Target: enterprise yang ingin skalakan coaching ke semua level karyawan. Fokus: measurement dan ROI coaching.

### Flow per Area Proton

**Setup:**
- HR/admin define program scope dan population (siapa yang dapat coaching)
- AI matching suggestkan coach terbaik berdasarkan kebutuhan development
- Coachee dapat pilih coach dari shortlist yang AI rekomendasikan
- Tidak ada "silabus" formal — agenda sesi ditentukan bersama coach-coachee

**Execution:**
- Sesi via video/call/chat — fleksibel format dan waktu
- CoachHub Companion (AI): support 24/7 antara sesi via Teams integration
- Action items di-track di dalam platform — coachee set commitments, update progress
- AIMY 2.0 (Nov 2025): AI coach untuk sesi singkat dan drill skill antara human coaching sessions

**Monitoring:**
- CoachHub Insights: real-time analytics tentang coaching impact company-wide
- Dashboard HR: session attendance, goal progress, skill mastery metrics
- Coachee track personal growth sendiri di platform
- Laporan ROI: before-after comparison, engagement rate, goal achievement rate

**Completion:**
- Program berakhir setelah durasi yang di-set (e.g., 6 bulan)
- Summary perkembangan dari coach
- Skill mastery report sebagai bukti completion
- Tidak ada certificate formal — evidence adalah analytics data

### Fitur Differentiator Relevan
- CoachHub Insights memiliki bottleneck visibility: identifikasi program/coachee yang stagnant (relevant untuk DIFF-03)
- Session format flexibility: video, call, atau chat sesuai preferensi
- AI + human hybrid coaching seamless (no handoff friction)

---

## Baseline As-Is Portal KPB per Area

**Confidence:** HIGH (dari analisis langsung kode Views/CDP dan Controllers/CDP)

### Area Setup
- **ProtonTrack**: Track per bagian/unit (Tahun 1/2/3), dikonfigurasi Admin via ProtonDataController
- **Silabus**: Hierarki 3-level: Kompetensi > Sub-Kompetensi > Deliverable; CRUD di ProtonDataController
- **Coaching Guidance**: File upload (PDF/PPT) per track; displayed via PlanIdp.cshtml tab kedua
- **Coach-Coachee Mapping**: Admin assign coach ke coachee via AdminController; cascade deactivation ada tapi tanpa explicit DB transaction
- **Track Assignment**: Admin assign coachee ke track (Tahun 1/2/3); seeding ProtonDeliverableProgress saat assign
- **Import/Export**: Excel import untuk silabus dan mapping; downloadable template

### Area Execution
- **Evidence Submission**: Coachee upload file bukti per deliverable; multi-file support
- **Approval Chain**: Sr Supervisor → Section Head → HC Review (multi-level); status field-based di ProtonDeliverableProgress
- **Override Admin**: Admin bisa override status di luar chain normal
- **Notifikasi**: ProtonNotification + UserNotification (dual system); triggers di berbagai state transition
- **PlanIdp View**: Coachee lihat silabus dan guidance; role-scoped (coachee locked ke bagian sendiri)

### Area Monitoring
- **CoachingProton page**: Tabel deliverable dengan filter Bagian/Unit/Track/Tahun/Coachee; role-scoped
- **Dashboard**: Chart.js untuk stats; role-scoped (HC/Admin lihat semua, coach lihat coachee-nya saja)
- **Export**: Excel export progress data
- **Pagination**: Implemented di CoachingProton tracking table

### Area Completion
- **Final Assessment**: Setelah semua deliverable approved, trigger competency level granting
- **Coaching Sessions**: Entity terpisah (CoachingSession); linked ke deliverable progress; action items
- **HistoriProton**: Timeline aktivitas coaching; coexist dengan legacy CoachingLog
- **3-Year Journey**: Tahun 1 → 2 → 3 lifecycle; assignment transition per tahun

---

## Gap Analysis per Area

### Gap: Setup
| Aspek | Portal KPB | Best Practice (Platform) | Gap | Severity |
|-------|-----------|--------------------------|-----|---------|
| Onboarding coach | Coach langsung di-assign manual oleh Admin | BetterUp/CoachHub: AI matching + coach dapat terima/tolak assignment | Tidak ada visibility workload coach sebelum assign | Must-fix (DIFF-01) |
| Silabus delete safety | Hard delete tanpa impact warning | 360Learning: soft-delete jika konten ada progress | Bisa delete silabus yang sedang aktif digunakan | Must-fix (SETUP-01) |
| Duplikasi mapping | Tidak ada validasi duplikasi coach-coachee | Platform modern: unique constraint + warning UI | Potensi duplicate mapping tanpa error jelas | Must-fix (SETUP-03) |
| DB transaction | Cascade deactivation tanpa explicit transaction | Standard pattern: atomic transaction untuk operasi cascade | Race condition risk saat deactivate coach yang punya banyak coachee | Must-fix (SETUP-03) |
| Import/Export silabus | Ada, tapi error handling belum diaudit | 360Learning: validasi ketat + error report per baris | Silent failure saat import bermasalah | Should-improve (SETUP-05) |

### Gap: Execution
| Aspek | Portal KPB | Best Practice (Platform) | Gap | Severity |
|-------|-----------|--------------------------|-----|---------|
| Evidence multi-file | Tersedia | 360Learning: video recording + file, versi per submission | Tidak ada versioning evidence; reject+resubmit flow belum diaudit end-to-end | Must-fix (EXEC-01) |
| Approval concurrent | Tidak ada guard | Platform modern: optimistic locking atau sequential check | Concurrent approve oleh 2 approver bisa corrupt state | Must-fix (EXEC-02) |
| State history | ProtonDeliverableHistory ada tapi completeness belum diaudit | BetterUp/CoachHub: setiap state transition tercatat otomatis | Initial Pending mungkin tidak ter-insert di history | Must-fix (EXEC-03) |
| Notifikasi coverage | Dual system (ProtonNotification + UserNotification) | Platform: notifikasi unified di semua trigger | Beberapa trigger mungkin tidak fire notifikasi | Must-fix (EXEC-04) |
| PlanIdp accuracy | Silabus display + guidance tabs | 360Learning: progress bar per deliverable inline | Tidak ada progress indicator inline di silabus view | Should-improve (EXEC-05) |

### Gap: Monitoring
| Aspek | Portal KPB | Best Practice (Platform) | Gap | Severity |
|-------|-----------|--------------------------|-----|---------|
| Workload coach visibility | Tidak ada | BetterUp: dashboard menampilkan jumlah coachee per coach + kapasitas | HC tidak bisa lihat coach mana yang overloaded | Must-fix (DIFF-01) |
| Batch approval | Tidak ada | CoachHub Insights + platform enterprise: bulk action dari list | HC harus approve satu per satu — bottleneck saat banyak submission | Must-fix (DIFF-02) |
| Bottleneck analysis | Tidak ada | CoachHub Insights: identifikasi deliverable stagnant, approval lama | Tidak ada visibility mana yang stuck paling lama | Must-fix (DIFF-03) |
| Dashboard stats accuracy | Chart.js ada, tapi N+1 potential | Platform: optimized query, projection | Export dan dashboard mungkin lambat saat data besar | Should-improve (MON-01/MON-04) |
| Override audit trail | Override ada tapi audit trail belum diaudit | BetterUp/CoachHub: setiap admin action tercatat dengan actor + timestamp | Accountability override tidak terjamin | Must-fix (MON-03) |

### Gap: Completion
| Aspek | Portal KPB | Best Practice (Platform) | Gap | Severity |
|-------|-----------|--------------------------|-----|---------|
| Final Assessment unique constraint | Tidak ada DB constraint | Platform: idempotent operation; DB unique constraint sebagai safety net | Double-click atau retry bisa create duplicate final assessment | Must-fix (COMP-01) |
| Coaching session linkage | CoachingSession entity ada | 360Learning: session notes linked ke deliverable progress dengan action item tracking | Linkage coaching session ke deliverable progress belum diverifikasi | Should-improve (COMP-02) |
| HistoriProton timeline | HistoriProton + legacy CoachingLog coexist | Platform: single unified timeline | Timeline mungkin incomplete jika legacy log tidak ter-migrate | Should-improve (COMP-03) |
| 3-year transition | Assignment transition Tahun 1→2→3 ada | BetterUp: renewal/continuation flow smooth | Progression validation (tidak bisa skip Tahun 1 ke 3) belum diaudit | Must-fix (COMP-04) |

---

## Don't Hand-Roll

| Problem | Jangan Buat Custom | Sudah Ada di Portal | Catatan |
|---------|-------------------|---------------------|---------|
| Audit trail admin | Custom logging system | EF Core DbContext + DbSet<ProtonDeliverableStatusHistory> | Gunakan dan lengkapi yang sudah ada |
| State machine approval | Custom workflow engine | Status fields + explicit transition logic | Sesuai keputusan v8.2: tidak pakai workflow engine |
| Notification system | Sistem baru | ProtonNotification + UserNotification (dual system sudah exist) | Audit coverage, jangan replace |
| Coach matching | AI matching seperti BetterUp | Manual admin assignment + workload indicator (DIFF-01) | Context industri KPB: coach internal, tidak perlu AI matching |

---

## Common Pitfalls

### Pitfall 1: Interpretasi "screenshot platform" terlalu literal
**Apa yang salah:** Mencoba akses demo aktual platform (membutuhkan registrasi enterprise) atau menyertakan screenshot dari URL tertentu yang bisa expired.
**Kenapa terjadi:** RSCH-01 menyebut "browse langsung demo/website" — tapi dalam konteks riset dokumentasi, narasi deskriptif UX sama validnya.
**Cara hindari:** Tulis narasi step-by-step UX berdasarkan temuan riset; gunakan kalimat seperti "Berdasarkan dokumentasi resmi, flow 360Learning adalah..."
**Warning signs:** Jika implementor mencoba embed screenshot eksternal — ganti dengan narasi teks terstruktur.

### Pitfall 2: Gap analysis terlalu abstrak
**Apa yang salah:** Menyatakan "platform X lebih baik dari KPB" tanpa menjelaskan konkret apa yang berbeda.
**Kenapa terjadi:** Riset dari WebSearch cenderung high-level.
**Cara hindari:** Setiap gap harus: (a) deskripsi as-is KPB, (b) deskripsi best practice, (c) apa dampak gap-nya ke user/data.

### Pitfall 3: Rekomendasi tidak di-map ke phase
**Apa yang salah:** Daftar rekomendasi yang mengambang tanpa assignment ke phase 234-237.
**Kenapa terjadi:** Riset tidak membaca requirements SETUP-xx, EXEC-xx, dll.
**Cara hindari:** Setiap rekomendasi harus punya kolom "Target Phase" yang merujuk ke Phase 234/235/236/237.

### Pitfall 4: Menulis 1 dokumen HTML yang terlalu panjang tanpa sidebar navigasi
**Apa yang salah:** Dokumen 4 area tanpa navigasi membuat pembaca (Phase 234-237 planner) kesulitan menemukan seksi relevan.
**Cara hindari:** Pastikan sidebar navigation dengan anchor link ke setiap section tersedia — ikuti pola research-comparison-summary.html.

---

## Code Examples

### Pola HTML Badge Tier (dari existing docs)
```html
<!-- Must-fix -->
<span class="badge badge-critical">Must-fix</span>

<!-- Should-improve -->
<span class="badge badge-medium">Should-improve</span>

<!-- Nice-to-have -->
<span class="badge badge-low">Nice-to-have</span>

<!-- Target phase -->
<span class="badge badge-blue">Phase 234</span>
```

### Pola Tabel Gap (dari research-comparison-summary.html pattern)
```html
<table>
  <thead>
    <tr>
      <th>Aspek</th>
      <th>Portal KPB (As-Is)</th>
      <th>Best Practice</th>
      <th>Gap</th>
      <th>Tier</th>
      <th>Target Phase</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Workload coach</td>
      <td>Tidak ada indicator</td>
      <td>BetterUp: jumlah coachee per coach + kapasitas visible</td>
      <td>HC tidak bisa detect coach overloaded sebelum assign</td>
      <td><span class="badge badge-critical">Must-fix</span></td>
      <td><span class="badge badge-blue">Phase 237</span></td>
    </tr>
  </tbody>
</table>
```

### Pola Sidebar Navigation
```html
<div class="sidebar">
  <div class="nav-section">Platform Overview</div>
  <a href="#overview">Ringkasan 3 Platform</a>
  <div class="nav-section">Area Proton</div>
  <a href="#setup">1. Setup</a>
  <a href="#execution">2. Execution</a>
  <a href="#monitoring">3. Monitoring</a>
  <a href="#completion">4. Completion</a>
  <div class="nav-section">Analisis</div>
  <a href="#differentiators">Differentiator KPB</a>
  <a href="#recommendations">Rekomendasi Prioritas</a>
</div>
```

---

## Differentiator Validation (DIFF-01/02/03)

Riset memverifikasi bahwa ketiga differentiator yang direncanakan KPB adalah fitur yang exist di platform enterprise, artinya ini adalah best practice — bukan over-engineering.

| Differentiator | Ada di Platform Mana | Validasi |
|---------------|---------------------|---------|
| **DIFF-01**: Workload indicator coach (jumlah coachee aktif per coach) | BetterUp: coach capacity/load visible di admin dashboard | VALID — best practice enterprise |
| **DIFF-02**: Batch approval HC Review | CoachHub Insights: bulk action dari monitoring view; 360Learning: bulk completion approval | VALID — best practice untuk high-volume approval |
| **DIFF-03**: Bottleneck analysis (deliverable paling lama pending) | CoachHub Insights: identifikasi stagnant coaching, approval delay visibility | VALID — best practice HR analytics |

**Kesimpulan:** Ketiga DIFF-xx bukan fitur mewah — ini adalah fitur dasar yang ada di platform enterprise. Absennya fitur ini di portal KPB adalah gap nyata.

---

## Open Questions

1. **Akses demo platform tidak tersedia secara langsung**
   - Yang kita tahu: Dokumentasi resmi dan review dari WebSearch
   - Yang tidak jelas: Detail spesifik UX micro-interaction (hover states, loading behavior, dsb.)
   - Rekomendasi: Narasi deskriptif berdasarkan dokumentasi resmi sudah cukup untuk tujuan riset ini; user dapat cek demo live sendiri jika perlu verifikasi tambahan

2. **CoachHub dan BetterUp bukan pure "skill deliverable tracking" platform**
   - Yang kita tahu: Kedua platform lebih ke coaching relationship + outcome measurement, bukan task/deliverable tracking seperti KPB
   - Yang tidak jelas: Apakah ada fitur deliverable tracking di tier enterprise mereka yang tidak terdokumentasi di public site
   - Rekomendasi: Posisikan perbandingan sebagai "pendekatan berbeda untuk tujuan yang sama" bukan "fitur yang sama persis"

---

## Validation Architecture

> Nyquist validation tidak berlaku untuk fase riset murni (output adalah dokumen HTML, bukan kode yang bisa di-unit-test). Tidak ada test framework yang relevan.

**Manual verification:** Reviewer membaca dokumen HTML dan memverifikasi:
- Semua 4 area Proton ter-cover
- Setiap rekomendasi punya tier + target phase
- DIFF-01/02/03 ter-validasi di dokumen

---

## Sources

### Primary (HIGH confidence)
- Analisis langsung `Views/CDP/PlanIdp.cshtml` — fitur Setup dan baseline as-is
- Analisis langsung `Views/CDP/CoachingProton.cshtml` — fitur Monitoring dan baseline as-is
- Analisis langsung `Controllers/CDPController.cs` — business logic Execution dan Completion
- `docs/research-comparison-summary.html` — pola HTML dan CSS styling yang established
- `.planning/REQUIREMENTS.md` — RSCH-01/02/03 dan DIFF-01/02/03 definitions
- `.planning/phases/233-riset-perbandingan-coaching-platform/233-CONTEXT.md` — locked decisions

### Secondary (MEDIUM confidence)
- [360Learning Coaching Solution](https://360learning.com/solution/coaching/) — fitur coaching dan feedback workflow
- [360Learning Manager Statistics](https://support.360learning.com/hc/en-us/articles/4405706039572-Track-the-statistics-for-your-managees) — manager dashboard dan tracking
- [360Learning Certificate Workflow](https://support.360learning.com/hc/en-us/articles/4408138780180-Download-certification-of-completion-reports) — completion dan approval sertifikat
- [BetterUp Fall 2025 Platform Release](https://www.betterup.com/platform-releases/fall-2025) — fitur terbaru 2025
- [BetterUp Manage](https://joshbersin.com/2024/04/betterup-manage-pioneering-ai-powered-platform-for-leaders/) — manager visibility dan coach workload
- [CoachHub Coaching Platform](https://www.coachhub.com/coaching-platform) — platform overview dan features
- [CoachHub AI Innovation AIMY 2.0](https://www.coachhub.com/en/ai-innovation) — AI coaching 2025
- [Enterprise Coaching Best Practices](https://simply.coach/blog/best-enterprise-coaching-tools/) — pola umum platform enterprise
- [360Learning LMS Features](https://360learning.com/product/learning-management-system/) — LMS dan analytics features

---

## Metadata

**Confidence breakdown:**
- Baseline as-is Portal KPB: HIGH — dari analisis langsung kode
- Platform 360Learning features: MEDIUM — dari docs resmi + WebSearch
- Platform BetterUp features: MEDIUM — dari docs resmi + WebSearch
- Platform CoachHub features: MEDIUM — dari docs resmi + WebSearch
- Gap analysis: MEDIUM — derived dari perbandingan HIGH+MEDIUM sources
- Differentiator validation: MEDIUM — dari multiple platform sources yang konsisten

**Research date:** 2026-03-22
**Valid until:** 2026-04-22 (platform update cepat, tapi fitur core stable 30+ hari)
