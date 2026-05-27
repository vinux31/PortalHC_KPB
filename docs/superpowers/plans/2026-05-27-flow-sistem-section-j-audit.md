# Flow Sistem Sertifikat §J Audit Findings — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` v1.0 → v1.1 dengan section §J Audit Findings & Maturity (3 kritis + 11 penting + scorecard 6 kategori + 7 strengths + 9 rekomendasi hardening).

**Architecture:** Modify single file, append §J di akhir sebelum footer, update mini-nav (9→10 link), bump versi footer. Konsisten style existing (callout + table + card).

**Spec reference:** `docs/superpowers/specs/2026-05-27-flow-sistem-section-j-audit-design.md`

**File Structure:**
- **Modify**: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` (488 → ~620 baris target, maks 700)

**Test approach:** Visual verify Playwright HTTP server.

---

### Task 1: Update mini-nav + tulis §J Audit Findings full

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html`

- [ ] **Step 1: Update mini-nav (tambah link §J)**

Cari di file:
```html
      <a href="#sec-i">§H+I Verifikasi+Audit</a>
    </div>
```

Replace dengan:
```html
      <a href="#sec-i">§H+I Verifikasi+Audit</a>
      <a href="#sec-j">§J Audit</a>
    </div>
```

- [ ] **Step 2: Tambah section §J sebelum `</main>` footer**

Cari closing `</section>` dari §H+I + footer block:
```html
          </tbody>
        </table>
      </div>
    </section>

    <footer class="text-center text-muted small mt-5 pt-4 border-top">
```

Insert antara `</section>` §H+I dan `<footer>`:

```html
    </section>

    <section id="sec-j">
      <h2><span class="badge bg-secondary">§J</span> Audit Findings &amp; Maturity</h2>
      <p>Audit deep teknis post-shipped flow-sistem v1.0 menemukan <strong>temuan code-level baru</strong> yang belum ter-cover di 50 gap (<code>analisa-gap-benchmark</code>) atau 25 flow (§A-§I). Sumber: caveman-investigator agent 2026-05-27 (2&times; run: flow + maturity audit). <strong>Verdict:</strong> Siap Dev/UAT, BELUM siap audit BPK / production-grade tanpa hardening 8-10 minggu.</p>

      <h5 class="mt-4">J.1 Maturity Scorecard</h5>
      <div class="row g-3 my-3">
        <div class="col-md-6 col-lg-4">
          <div class="card h-100"><div class="card-body">
            <h6 class="card-title">Code Quality <span class="badge bg-danger">2/5</span></h6>
            <p class="small mb-0">Race condition ET insert + fire-and-forget log + DTO no validation + loose exception.</p>
          </div></div>
        </div>
        <div class="col-md-6 col-lg-4">
          <div class="card h-100"><div class="card-body">
            <h6 class="card-title">Database/Schema <span class="badge bg-warning text-dark">3/5</span></h6>
            <p class="small mb-0">Index well-placed (67 defined) + check constraint OK. Missing composite <code>(Status, ValidUntil)</code> + NoAction FK orphan risk.</p>
          </div></div>
        </div>
        <div class="col-md-6 col-lg-4">
          <div class="card h-100"><div class="card-body">
            <h6 class="card-title">Security <span class="badge bg-info text-dark">4/5</span></h6>
            <p class="small mb-0">No raw SQL + CSRF + auth solid. Minor: notif read no user context, log no PII masking.</p>
          </div></div>
        </div>
        <div class="col-md-6 col-lg-4">
          <div class="card h-100"><div class="card-body">
            <h6 class="card-title">Test Coverage <span class="badge bg-danger">1/5</span></h6>
            <p class="small mb-0"><strong>ZERO unit test</strong>. Playwright E2E 10 spec happy path. Race / regrade / renewal chain untested.</p>
          </div></div>
        </div>
        <div class="col-md-6 col-lg-4">
          <div class="card h-100"><div class="card-body">
            <h6 class="card-title">Operational <span class="badge bg-danger">2/5</span></h6>
            <p class="small mb-0">Plain ILogger no Serilog, no <code>/health</code>, no <code>/metrics</code>, no correlation ID, no error monitor.</p>
          </div></div>
        </div>
        <div class="col-md-6 col-lg-4">
          <div class="card h-100"><div class="card-body">
            <h6 class="card-title">Documentation <span class="badge bg-warning text-dark">3/5</span></h6>
            <p class="small mb-0">ARCHITECTURE.md + XML doc ada. Missing ADR + dev onboarding.</p>
          </div></div>
        </div>
      </div>
      <div class="alert alert-warning border">
        <strong><i class="bi bi-bar-chart-fill"></i> Aggregate Maturity:</strong> <span class="badge bg-warning text-dark fs-6">2.5/5</span> &mdash; minimal-acceptable internal-only, JAUH dari production-grade enterprise.
      </div>

      <h5 class="mt-4">J.2 3 Temuan Kritis</h5>
      <div class="row g-3 my-3">
        <div class="col-md-4">
          <div class="gap-callout h-100">
            <strong><i class="bi bi-x-circle-fill text-danger"></i> 1. Race Condition ET Score Insert</strong>
            <p class="small mb-1 mt-2"><strong>Lokasi:</strong> <code>Services/GradingService.cs:178</code></p>
            <p class="small mb-0"><strong>Risiko:</strong> 2 request simultan submit assessment bisa duplicate insert ET scores. Mitigation existing (DbUpdateException catch + ChangeTracker.Clear) hanya tutup gejala, akar belum di-fix (no DB transaction + unique constraint).</p>
          </div>
        </div>
        <div class="col-md-4">
          <div class="gap-callout h-100">
            <strong><i class="bi bi-x-circle-fill text-danger"></i> 2. Fire-and-Forget Audit Log</strong>
            <p class="small mb-1 mt-2"><strong>Lokasi:</strong> <code>Controllers/CMPController.cs:1763</code></p>
            <p class="small mb-0"><strong>Risiko:</strong> <code>_ = Task.Run(...)</code> background exception ter-swallow. Kalau DbContext dispose sebelum task selesai, audit log hilang silent. Compliance risk.</p>
          </div>
        </div>
        <div class="col-md-4">
          <div class="gap-callout h-100">
            <strong><i class="bi bi-x-circle-fill text-danger"></i> 3. ZERO Unit Test</strong>
            <p class="small mb-1 mt-2"><strong>Lokasi:</strong> seluruh <code>Services/</code></p>
            <p class="small mb-0"><strong>Risiko:</strong> GradingService + NotificationService + EditLog logic untested. Core state machine tanpa safety net. Regression risk tinggi tiap perubahan code.</p>
          </div>
        </div>
      </div>

      <h5 class="mt-4">J.3 11 Temuan Penting</h5>
      <div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle">
          <thead class="table-dark"><tr><th>#</th><th>Issue</th><th>Lokasi</th><th>Risiko</th><th>Severity</th></tr></thead>
          <tbody>
            <tr><td>1</td><td><strong>DTO no validation</strong></td><td><code>Models/EditAnswersSubmission.cs</code></td><td>Tidak ada <code>[Required]</code>/<code>[MaxLength]</code>. DoS risk via payload besar, server-side blind</td><td><span class="badge bg-warning text-dark">Penting</span></td></tr>
            <tr><td>2</td><td><strong>Loose exception handling</strong></td><td><code>Services/NotificationService.cs:121,141,170,198,216,260,286</code> (7&times;)</td><td><code>catch (Exception)</code> return false &mdash; caller tidak bisa bedakan "notif tidak ada" vs "DB error"</td><td><span class="badge bg-warning text-dark">Penting</span></td></tr>
            <tr><td>3</td><td><strong>N+1 query batch</strong></td><td><code>Controllers/RenewalController.cs:246</code></td><td><code>BuildRenewalRowsAsync</code> tanpa pagination &mdash; bisa O(full scan) saat banyak sertifikat</td><td><span class="badge bg-warning text-dark">Penting</span></td></tr>
            <tr><td>4</td><td><strong>Optimistic lock 1-detik grace</strong></td><td><code>Controllers/AssessmentAdminController.cs:2837</code></td><td><code>Math.Abs(...TotalSeconds) &gt; 1</code> terlalu lebar, 2 rapid edit bisa lolos. Pakai EF row version (byte[]) lebih precision</td><td><span class="badge bg-warning text-dark">Penting</span></td></tr>
            <tr><td>5</td><td><strong>Missing composite index</strong></td><td><code>Data/ApplicationDbContext.cs:180-188</code></td><td>Tidak ada <code>(Status='Completed', ValidUntil)</code> &mdash; renewal/expiry query full-scan</td><td><span class="badge bg-warning text-dark">Penting</span></td></tr>
            <tr><td>6</td><td><strong>NoAction FK orphan risk</strong></td><td><code>Data/ApplicationDbContext.cs:152-170,214-232</code></td><td>RenewsSessionId/RenewsTrainingId NoAction + tidak ada application cleanup &rarr; orphan record kalau source di-delete</td><td><span class="badge bg-warning text-dark">Penting</span></td></tr>
            <tr><td>7</td><td><strong>Notif MarkAsRead no user context</strong></td><td><code>Services/NotificationService.cs:152-175</code></td><td>Validation pakai param userId, bukan current user. User bisa mark-as-read notif user lain via tampered request</td><td><span class="badge bg-warning text-dark">Penting</span></td></tr>
            <tr><td>8</td><td><strong>No PII masking audit log</strong></td><td><code>Controllers/AssessmentAdminController.cs:2915-2925</code></td><td><code>OldAnswerTextSnapshot</code>/<code>NewAnswerTextSnapshot</code> simpan raw answer &mdash; kalau ada PII, kompromis privacy</td><td><span class="badge bg-warning text-dark">Penting</span></td></tr>
            <tr><td>9</td><td><strong>Plain logging</strong></td><td><code>appsettings.json</code></td><td>Tidak ada Serilog/structured logging, no correlation ID, no log rotation policy. Sulit debug production</td><td><span class="badge bg-warning text-dark">Penting</span></td></tr>
            <tr><td>10</td><td><strong>No /health endpoint</strong></td><td>startup</td><td>Container orchestration (Kubernetes probe) tidak bisa monitor app readiness</td><td><span class="badge bg-warning text-dark">Penting</span></td></tr>
            <tr><td>11</td><td><strong>No /metrics endpoint</strong></td><td>startup</td><td>Prometheus/observability absent. Query performance / SignalR connection pool / cache hit-rate tidak ter-track</td><td><span class="badge bg-warning text-dark">Penting</span></td></tr>
          </tbody>
        </table>
      </div>

      <h5 class="mt-4">J.4 Yang Sudah BAGUS (Strengths)</h5>
      <div class="alert alert-success">
        <ul class="mb-0 small">
          <li><strong>Zero raw SQL</strong> &mdash; semua via LINQ-to-EF, parameterized otomatis</li>
          <li><strong>CSRF protected</strong> &mdash; <code>[ValidateAntiForgeryToken]</code> di semua POST admin</li>
          <li><strong>Authorization granular</strong> &mdash; <code>[Authorize(Roles=...)]</code> + ownership check eksplisit (mis. <code>assessment.UserId != user.Id</code>)</li>
          <li><strong>67 HasIndex defined</strong> + filtered index (CoachCoacheeMapping.IsActive, NomorSertifikat != null)</li>
          <li><strong>Check constraints</strong> Progress 0-100, PassPercentage 0-100, DurationMinutes &ge;0</li>
          <li><strong>Scoped DI</strong> &mdash; no singleton state leak</li>
          <li><strong>Phase 323 cascade hardening</strong> DeleteAssessment explicit cascade EditLog + Responses + Packages + Options</li>
        </ul>
      </div>

      <h5 class="mt-4">J.5 Rekomendasi Prioritas Hardening</h5>
      <div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle">
          <thead class="table-dark"><tr><th>#</th><th>Prioritas</th><th>Action</th><th>Effort Estimate</th></tr></thead>
          <tbody>
            <tr><td>1</td><td><span class="badge bg-danger">Wajib</span></td><td>Bikin unit test suite (xUnit) untuk GradingService + NotificationService + RenewalChain (min 60% coverage)</td><td>4-6 minggu</td></tr>
            <tr><td>2</td><td><span class="badge bg-danger">Wajib</span></td><td>Fix race condition <code>GradingService.cs:178</code> (DB transaction + unique constraint)</td><td>1 minggu</td></tr>
            <tr><td>3</td><td><span class="badge bg-danger">Wajib</span></td><td>Fix fire-and-forget <code>CMPController.cs:1763</code> (IHostedService background channel dengan error visibility)</td><td>3 hari</td></tr>
            <tr><td>4</td><td><span class="badge bg-danger">Wajib</span></td><td>Tambah <code>/health</code> + <code>/metrics</code> + Serilog structured logging + correlation ID middleware</td><td>1 minggu</td></tr>
            <tr><td>5</td><td><span class="badge bg-warning text-dark">Penting</span></td><td>Add DTO validation <code>[Required]</code>/<code>[MaxLength]</code> semua submission model</td><td>2 hari</td></tr>
            <tr><td>6</td><td><span class="badge bg-warning text-dark">Penting</span></td><td>Composite index <code>(Status, ValidUntil)</code> + migration</td><td>1 hari</td></tr>
            <tr><td>7</td><td><span class="badge bg-warning text-dark">Penting</span></td><td>NoAction FK &rarr; application-level orphan cleanup atau trigger</td><td>3 hari</td></tr>
            <tr><td>8</td><td><span class="badge bg-warning text-dark">Penting</span></td><td>Optimistic lock pakai EF row version (byte[]) ganti TimeSpan grace</td><td>2 hari</td></tr>
            <tr><td>9</td><td><span class="badge bg-warning text-dark">Penting</span></td><td>NotificationService.MarkAsRead pakai <code>HttpContext.User</code> bukan param</td><td>1 hari</td></tr>
          </tbody>
        </table>
      </div>
      <div class="alert alert-danger mt-3">
        <strong><i class="bi bi-clock-history"></i> Estimasi Total Hardening:</strong> <strong>8-10 minggu</strong> untuk production-ready audit BPK-grade.
      </div>
    </section>
```

- [ ] **Step 3: Verify browser**

Start HTTP server, navigate ke `http://127.0.0.1:8765/docs/sertifikat-ecosystem/flow-sistem-sertifikat.html`. Expected:
- Mini-nav 10 link, `§J Audit` link visible
- Section §J render di akhir sebelum footer
- J.1 scorecard 6 card dengan badge score color (1-2 merah, 3 kuning, 4 biru, 5 hijau)
- J.2 3 callout merah parallel
- J.3 tabel 11 baris scroll-responsive
- J.4 alert success 7 bullet
- J.5 tabel 9-baris dengan badge prioritas
- Footer estimasi total 8-10 minggu alert danger

- [ ] **Step 4: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §J Audit Findings & Maturity flow-sistem — 3 kritis + 11 penting + scorecard 6 kategori + 9 rekomendasi hardening"
```

---

### Task 2: Footer version bump + final verify

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html`

- [ ] **Step 1: Update footer versi + source attribution**

Cari:
```html
      <p class="mb-1">Flow Sistem Sertifikat &mdash; Portal HC KPB &middot; Versi 1.0 &middot; 2026-05-27</p>
      <p class="mb-2">
        <strong>Audit codebase:</strong> 25 flow distinct (22 ada + 3 gap kritis). Sumber: caveman-investigator agent 2026-05-27.
      </p>
```

Replace dengan:
```html
      <p class="mb-1">Flow Sistem Sertifikat &mdash; Portal HC KPB &middot; Versi 1.1 &middot; 2026-05-27 (patch: +§J Audit Findings)</p>
      <p class="mb-2">
        <strong>Audit codebase:</strong> 25 flow distinct (22 ada + 3 gap kritis) + 14 audit findings (3 kritis + 11 penting + 7 strengths). Sumber: caveman-investigator agent 2026-05-27 (2&times; run: flow + maturity audit).
      </p>
```

- [ ] **Step 2: Final verify browser end-to-end**

Refresh. Cek:
- Footer versi 1.1 muncul
- Mini-nav §J click → smooth scroll ke section
- Print preview Ctrl+P: section §J inherit print CSS (page-break-before + color-adjust:exact), scorecard card grid kompres OK

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): bump flow-sistem footer ke v1.1 + attribution audit findings"
```

---

## Self-Review

**Spec coverage check (vs `2026-05-27-flow-sistem-section-j-audit-design.md`):**
- §3 Intro + verdict singkat → Task 1 §J intro paragraph
- §3 J.1 Maturity Scorecard 6 card grid → Task 1
- §3 J.2 3 Temuan Kritis callout → Task 1
- §3 J.3 11 Temuan Penting tabel → Task 1
- §3 J.4 Strengths alert → Task 1
- §3 J.5 Rekomendasi 9-baris + total estimasi → Task 1
- §5 Mini-nav update 9→10 link → Task 1
- §6 Footer versi bump 1.0→1.1 + attribution → Task 2

All spec sections mapped. No gap.

**Placeholder scan:** No "TBD"/"TODO". All content concrete.

**Type consistency:** Section ID `#sec-j` konsisten di mini-nav (Task 1) + section heading (Task 1). CSS class `gap-callout` reuse existing (didefinisikan di Task 1 dari plan v1.0). Tidak ada CSS class baru.

Plan ready.
