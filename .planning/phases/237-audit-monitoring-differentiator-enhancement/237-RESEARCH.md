# Phase 237: Audit Monitoring & Differentiator Enhancement - Research

**Researched:** 2026-03-23
**Domain:** ASP.NET Core MVC — CDPController audit, Chart.js, batch AJAX, ClosedXML export
**Confidence:** HIGH

## Summary

Phase 237 adalah fase terakhir dari v8.2 audit. Terdapat dua kelompok pekerjaan: (1) audit bug-fix pada empat area monitoring yang sudah ada (dashboard stats, CoachingProton tracking, Override, Export), dan (2) tiga fitur differentiator baru (workload indicator, batch HC approval, bottleneck chart + export).

Kode existing sudah matang — `BuildProtonProgressSubModelAsync` sudah melakukan role-scoped filtering, pagination CoachingProton sudah group-boundary-aware, dan `OverrideSave` sudah mencatat audit trail via `_auditLog.LogAsync`. Yang perlu diaudit adalah: `ExportHistoriProton` tidak punya `[Authorize(Roles=...)]` attribute (hanya class-level `[Authorize]`), `ExportProgressExcel` tidak menyertakan Coach role, dan `OverrideSave` tidak memvalidasi legal status transitions (bisa override dari Approved ke Pending). Fitur baru semuanya dapat diimplementasikan dengan pattern yang sudah ada di codebase tanpa library tambahan.

**Primary recommendation:** Audit-first per area (temukan bug, fix), lalu tambah fitur baru secara berurutan. Semua implementasi menggunakan stack existing: ClosedXML untuk export, Chart.js untuk chart, AJAX partial-refresh untuk batch approval.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Dashboard & Chart Accuracy (MON-01)**
- D-01: Audit accuracy saja — Claude investigasi apakah query stats dan Chart.js data sudah benar, fix bugs tanpa tambah card baru
- D-02: Tambah chart bottleneck — horizontal bar chart menampilkan top 5-10 deliverable paling lama pending (>30 hari), jumlah hari sebagai value
- D-03: Chart bottleneck ditambahkan di `_CoachingProtonContentPartial.cshtml` bersama trend line dan doughnut chart yang sudah ada

**Batch Approval HC (DIFF-02)**
- D-04: Batch approval dilakukan dari halaman CoachingProton tracking (CDPController) — tambah checkbox per row + "Approve Selected" button
- D-05: Scope: HC Review saja — hanya deliverable dengan status "Pending HC Review" yang bisa di-batch approve
- D-06: UX: modal konfirmasi sebelum proses — tampilkan "Approve X deliverable?" dengan daftar item, user konfirmasi baru proses
- D-07: Endpoint baru di CDPController untuk batch HC approve — POST dengan list of deliverable IDs

**Workload Indicator (DIFF-01)**
- D-08: Workload indicator ditampilkan di halaman CoachCoacheeMapping (AdminController) — tambah kolom "Jumlah Coachee Aktif" per coach
- D-09: Tidak perlu tampilkan di dashboard — cukup di mapping page dimana HC assign coachee

**Bottleneck Analysis (DIFF-03)**
- D-10: Threshold bottleneck: 30 hari — deliverable pending >30 hari masuk kategori bottleneck
- D-11: Visualisasi: horizontal bar chart di dashboard (D-02) — top deliverable terlama

**CoachingProton Tracking Audit (MON-02)**
- D-12: Claude audit filter cascade, pagination, role-based column visibility — fix bugs yang ditemukan

**Override Audit (MON-03)**
- D-13: Audit trail + transition rules — Claude audit apakah setiap override tercatat di audit log
- D-14: Validasi status transition — tidak bisa override ke status ilegal (misalnya dari Approved kembali ke Pending)

**Export Audit + Export Baru (MON-04)**
- D-15: Audit existing exports: N+1 query elimination, projection (select hanya kolom yang dipakai), role attribute check
- D-16: Export baru 1 — Bottleneck report Excel: daftar deliverable pending >30 hari dengan coachee, coach, section, jumlah hari pending
- D-17: Export baru 2 — Coaching tracking Excel: export data dari halaman CoachingProton tracking sesuai filter yang aktif
- D-18: Export baru 3 — Workload summary Excel: daftar coach dengan jumlah coachee aktif, jumlah deliverable pending per coach

### Claude's Discretion
- Detail implementasi horizontal bar chart (Chart.js config, color scheme)
- Batch approve endpoint design (AJAX + partial refresh atau full page reload)
- Checkbox UI pattern di CoachingProton tracking (select all, per-row, header checkbox)
- Export file naming dan column layout
- Override audit trail mechanism (existing AuditLogService atau tambahan)
- Query optimization detail untuk existing exports

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| MON-01 | Audit Dashboard — role-scoped filtering accuracy, stats correctness, Chart.js data integrity | BuildProtonProgressSubModelAsync sudah diinspeksi; stat card logic di L537-538 + bottleneck query perlu ditambah ke ProtonProgressSubModel |
| MON-02 | Audit CoachingProton tracking — filter cascade, pagination, role-based column visibility | CoachingProton action L1386-1685 sudah diinspeksi lengkap; filter cascade sudah ada, pagination group-boundary sudah ada |
| MON-03 | Audit Override — validasi status transition rules, audit trail lengkap, admin accountability | OverrideSave L1368-1421 diinspeksi; audit log ADA tapi transition validation TIDAK ADA — perlu ditambah |
| MON-04 | Audit Export — data accuracy, query optimization (N+1 elimination, projection), semua export actions | ExportHistoriProton tidak punya role attribute; ExportProgressExcel ada role attr tapi tidak include Coach; perlu 3 export baru |
| DIFF-01 | Workload indicator coach — tampilkan jumlah coachee aktif per coach di mapping page | CoachCoacheeMapping action sudah menggroup by coach — tinggal tambah hitungan aktif per coach |
| DIFF-02 | Batch approval HC Review — approve multiple deliverables sekaligus dari monitoring view | Pattern batch save ada di OverrideSave; perlu endpoint baru BatchHCApprove di CDPController + UI checkbox |
| DIFF-03 | Bottleneck analysis — identifikasi deliverable paling lama pending, approval bottleneck visibility di dashboard | Perlu query baru: ProtonDeliverableProgresses WHERE Status='Submitted' AND SubmittedAt < (NOW - 30 days), ditambah ke ProtonProgressSubModel |
</phase_requirements>

---

## Standard Stack

### Core (sudah ada di project, tidak perlu install)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | Existing | Excel export | Sudah dipakai di ExportProgressExcel, ExportHistoriProton, ExportSilabus |
| Chart.js | Existing (CDN) | Data visualization | Sudah ada trend line + doughnut di `_CoachingProtonContentPartial.cshtml` |
| ASP.NET Core MVC | Existing | Controller + Razor views | Project framework |
| AuditLogService | Existing | Audit trail | Sudah dipakai di OverrideSave, CDPController setelah Phase 236 |

**Installation:** Tidak ada library tambahan yang diperlukan.

---

## Architecture Patterns

### Pola yang sudah ada dan harus diikuti

**1. Chart.js in Partial Views**
Chart di `_CoachingProtonContentPartial.cshtml` menggunakan pattern: data dikirim via ViewModel property (TrendLabels, TrendValues, StatusLabels, StatusData) → di-serialize ke JavaScript variable dengan `@Json.Serialize(Model.X)` → Chart instance dibuat di `<script>` block di bagian bawah partial.

Bottleneck chart baru harus mengikuti pattern yang sama: tambah properties `BottleneckLabels` (List<string>) dan `BottleneckValues` (List<int>) ke `ProtonProgressSubModel`, populate di `BuildProtonProgressSubModelAsync`, render di partial.

**2. Role-Scoped Export Pattern**
`ExportProgressExcel` menggunakan `[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]`. `ExportHistoriProton` hanya punya class-level `[Authorize]` tanpa role restriction — ini adalah bug yang perlu di-fix (D-15). Pattern yang benar: semua export action harus punya explicit `[Authorize(Roles = ...)]`.

**3. Batch Save Pattern (OverrideSave)**
`OverrideSave` di ProtonDataController menerima `OverrideSaveRequest` sebagai `[FromBody]` JSON. Batch HC approve endpoint harus mengikuti pattern serupa: `[HttpPost]`, `[ValidateAntiForgeryToken]`, `[FromBody]` DTO dengan `List<int> DeliverableProgressIds`, return JSON `{ success, message }`.

**4. Workload Count in GroupBy Query**
`CoachCoacheeMapping` action sudah GroupBy coach dengan `ActiveCount = g.Count(r => r.Mapping.IsActive)`. Data ini sudah ada di `grouped` list — tinggal pass ke ViewBag atau expose di View sebagai kolom baru. Tidak perlu query tambahan.

**5. ClosedXML Export Helper**
Semua export menggunakan `ExcelExportHelper.CreateSheet(workbook, sheetName, columnHeaders)` dan `ExcelExportHelper.ToFileResult(workbook, filename, this)`. Export baru harus mengikuti pattern yang sama persis.

### Recommended Plan Structure
```
Plan 01: Audit & Fix — MON-01 dashboard, MON-02 tracking, MON-03 override
Plan 02: Export Audit & New Exports — MON-04 role attributes, query optimization, 3 export baru
Plan 03: Differentiator Features — DIFF-01 workload, DIFF-02 batch approval, DIFF-03 bottleneck chart
```

### Anti-Patterns to Avoid
- **Menambah library baru:** Project sudah decision "no new libraries" (v8.2 state). Chart.js horizontal bar adalah built-in type, tidak perlu plugin.
- **SignalR untuk batch approve:** Batch approval tidak time-critical, AJAX JSON endpoint + full page reload sudah cukup.
- **Over-fetch tanpa projection:** Export yang baru jangan `.Include()` semua navigation properties — hanya include yang dipakai di kolom Excel.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel export | Custom CSV/HTML table | ClosedXML via `ExcelExportHelper` | Sudah ada helper, consistent format |
| Chart rendering | D3.js atau custom SVG | Chart.js (existing) | Sudah ter-load di semua halaman dashboard |
| Audit logging | Custom log table | `AuditLogService._auditLog.LogAsync()` | Sudah ada, sudah di-inject ke CDPController sejak Phase 236 |
| Role check in action | Manual string comparison | `[Authorize(Roles = "...")]` attribute | Declarative, consistent dengan pattern project |

---

## Findings: Bug & Gap Inventory

### MON-01 — Dashboard Accuracy

**Stats yang ada (L537-538):**
```csharp
int pendingSpv = allProgresses.Count(p => p.Status == "Submitted");
int pendingHC  = allProgresses.Count(p => p.HCApprovalStatus == "Pending" && p.Status == "Approved");
```
Stat `pendingHC` sudah benar — deliverable yang sudah Approved oleh supervisor tapi belum direview HC. Logic ini konsisten dengan intent.

**Masalah potensial yang perlu diverifikasi:** Ketika filter category/track aktif, `allProgresses` sudah difilter oleh `filteredCoacheeIds` berdasarkan filter tersebut — tapi `assignmentDict` juga difilter. Bisa terjadi ketidakkonsistenan jika progresses dari assignment yang tidak cocok filter masih masuk hitungan stats. Perlu audit apakah filter category/track di assignment (L484-486) juga di-apply ke `allProgresses` sebelum menghitung stats.

**Bottleneck data baru yang perlu ditambah ke ProtonProgressSubModel:**
```csharp
// Query: deliverable yang paling lama pending (status Submitted, bukan Approved)
var bottleneckItems = allProgresses
    .Where(p => p.Status == "Submitted" && p.SubmittedAt.HasValue
                && (DateTime.UtcNow - p.SubmittedAt.Value).TotalDays > 30)
    .OrderByDescending(p => (DateTime.UtcNow - p.SubmittedAt.Value).TotalDays)
    .Take(10)
    .ToList();
```
Properties yang perlu ditambah ke `ProtonProgressSubModel`: `BottleneckLabels` (List<string>, nama deliverable + coachee), `BottleneckValues` (List<int>, jumlah hari pending).

### MON-02 — CoachingProton Tracking

**Filter cascade status:** Filter Bagian (L1438-1452), Unit (L1454-1480), dan Track (L1482-1491) sudah ada dan sudah role-scoped dengan benar.

**Pagination:** Group-boundary pagination sudah diimplementasikan (L1646-1685), tidak split SubKompetensi group.

**Potensi bug:** Tahun filter (L1553-1556) di-apply ke query `progresses` tapi bukan ke `scopedCoacheeIds` — artinya dropdown coachee list menampilkan semua coachee dalam scope meskipun mereka tidak memiliki deliverable untuk tahun yang dipilih. Ini bisa membingungkan user (coachee muncul di dropdown tapi tidak ada data). Perlu diverifikasi apakah ini intended behavior atau bug.

**Role-based column visibility:** Perlu dicek di View apakah kolom approver (SrSpv, SH, HC) hanya muncul untuk role yang berwenang.

### MON-03 — Override Audit

**Audit trail:** OverrideSave SUDAH mencatat audit trail di L1416-1418:
```csharp
await _auditLog.LogAsync(user.Id, user.FullName ?? ..., "Override",
    $"Override deliverable progress #{progress.Id}: {oldStatus} → {req.NewStatus}. Alasan: {req.OverrideReason}",
    targetId: progress.Id, targetType: "ProtonDeliverableProgress");
```
Ini sudah benar dan lengkap.

**BUG DITEMUKAN — Status transition validation (D-14):** OverrideSave (L1376-1382) hanya memvalidasi bahwa status ada dalam `validStatuses[]` tapi tidak memvalidasi legal transitions. Misalnya, bisa override dari `Approved` kembali ke `Pending` tanpa restriction. Legal transition matrix yang perlu di-enforce:

| Current | Allowed Override Targets |
|---------|--------------------------|
| Pending | Submitted, Approved, Rejected |
| Submitted | Pending, Approved, Rejected |
| Approved | Submitted, Rejected (TIDAK boleh ke Pending langsung — harus lewat Submitted dulu?) |
| Rejected | Pending, Submitted |

Catatan: karena ini adalah **admin override**, mungkin semua transitions harus diizinkan dengan alasan, tapi setidaknya perlu konfirmasi bahwa ini adalah intended. Berdasarkan D-14, validasi harus mencegah transisi ilegal — minimal mencegah regress dari `Approved` ke status sebelumnya tanpa explicit justification (atau bisa diblokir sepenuhnya).

**Rekomendasi:** Implement transition validation sederhana: jika `currentStatus == "Approved"` maka hanya izinkan override ke `Rejected` (undo approval yang salah). Semua transisi lain diizinkan.

### MON-04 — Export Audit

**BUG — ExportHistoriProton tidak punya role attribute:**
```csharp
// Baris 3021-3022:
[HttpGet]
public async Task<IActionResult> ExportHistoriProton(...)
```
Tidak ada `[Authorize(Roles = "...")]` — siapa pun yang terautentikasi bisa mengaksesnya. Fix: tambah `[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]` (atau sesuai business requirement — Coach mungkin juga perlu akses export untuk coachee-nya).

**ExportProgressExcel role attribute:**
```csharp
[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]
```
Coach role tidak ada di sini. Coach seharusnya bisa export progress coachee mereka (scope sudah ada via role check L2358-2362). Fix: tambah `Coach` ke roles.

**Query Analysis — ExportProgressExcel (L2365-2373):**
```csharp
var progresses = await _context.ProtonDeliverableProgresses
    .Include(p => p.ProtonDeliverable)
        .ThenInclude(d => d!.ProtonSubKompetensi)
            .ThenInclude(s => s!.ProtonKompetensi)
    .Where(p => p.CoacheeId == coacheeId)
    ...
```
Ini menggunakan `.Include()` tapi hanya membutuhkan nama kolom navigasi — bisa dioptimalkan ke projection. Tapi ini bukan N+1, hanya over-fetch. Prioritas medium.

**Query Analysis — ExportHistoriProton (L3058-3074):**
Load `assignments` → load semua `allProgressesExport` untuk assignment IDs. Ini juga bukan N+1 — sudah batched dengan `.Where(p => assignmentIds.Contains(...))`. Over-fetch ada tapi tidak kritis.

---

## Common Pitfalls

### Pitfall 1: Filter Stats Inconsistency
**What goes wrong:** Ketika filter aktif (category/track), stats card (pendingSpv, pendingHC) mungkin terhitung dari `allProgresses` yang belum difilter ulang setelah assignment filter di-apply.
**How to avoid:** Pastikan `allProgresses` sudah di-intersect dengan `filteredAssignmentIds` setelah category/track filter di-apply.

### Pitfall 2: Batch HC Approve Race Condition
**What goes wrong:** User submit batch approve dengan 5 deliverable IDs. Sementara request diproses, admin override salah satu ke Rejected. Batch approve akan silently override hasil admin.
**How to avoid:** Di batch approve endpoint, check `HCApprovalStatus == "Pending"` per item sebelum update. Return partial success response dengan detail item yang tidak bisa diproses.

### Pitfall 3: Bottleneck Chart Data Scope
**What goes wrong:** Bottleneck chart menampilkan data yang tidak sesuai dengan filter yang aktif di dashboard (misalnya user filter by Section A tapi chart menampilkan bottleneck dari semua section).
**How to avoid:** Bottleneck query harus menggunakan `allProgresses` yang sudah di-scope dan filtered (bukan query baru terpisah).

### Pitfall 4: OverrideSave HCApprovalStatus Field
**What goes wrong:** Transition validation hanya memeriksa `Status` field tapi tidak memeriksa `HCApprovalStatus`. Admin bisa set Status=Approved tapi HCApprovalStatus=Reviewed untuk deliverable yang seharusnya masih pending HC review.
**How to avoid:** Tambah validasi: jika `NewStatus != "Approved"` maka `NewHCStatus` harus "Pending" (tidak bisa HC review deliverable yang belum approved).

### Pitfall 5: Export Coaching Tracking — Filter State
**What goes wrong:** Export coaching tracking (D-17) harus mengikuti filter yang aktif. Jika endpoint export tidak menerima parameter filter yang sama dengan CoachingProton action, hasilnya tidak konsisten.
**How to avoid:** Export endpoint harus menerima parameter identik: `coacheeId, bagian, unit, trackType, tahun`. Implementasi query menggunakan logika filter yang sama persis.

---

## Code Examples

### Chart.js Horizontal Bar (Bottleneck) — sesuai pattern existing
```javascript
// Source: Mengikuti pattern _CoachingProtonContentPartial.cshtml L206-279
@if (Model.BottleneckLabels.Any())
{
    <text>
    var bottleneckLabels = @Json.Serialize(Model.BottleneckLabels);
    var bottleneckValues = @Json.Serialize(Model.BottleneckValues);
    var bottleneckCtx = document.getElementById('bottleneckChart');
    if (bottleneckCtx) {
        new Chart(bottleneckCtx.getContext('2d'), {
            type: 'bar',
            data: {
                labels: bottleneckLabels,
                datasets: [{
                    label: 'Hari Pending',
                    data: bottleneckValues,
                    backgroundColor: 'rgba(220, 53, 69, 0.7)',
                    borderColor: 'rgba(220, 53, 69, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                indexAxis: 'y',  // horizontal bar
                responsive: true,
                plugins: { legend: { display: false } },
                scales: {
                    x: { beginAtZero: true, title: { display: true, text: 'Jumlah Hari Pending' } }
                }
            }
        });
    }
    </text>
}
</text>
```

### Batch HC Approve Endpoint Pattern
```csharp
// CDPController — mengikuti pattern OverrideSave di ProtonDataController
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "HC, Admin")]
public async Task<IActionResult> BatchHCApprove([FromBody] BatchHCApproveRequest req)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    if (req.ProgressIds == null || !req.ProgressIds.Any())
        return Json(new { success = false, message = "Tidak ada deliverable yang dipilih." });

    var progresses = await _context.ProtonDeliverableProgresses
        .Where(p => req.ProgressIds.Contains(p.Id)
                    && p.Status == "Approved"
                    && p.HCApprovalStatus == "Pending")
        .ToListAsync();

    foreach (var p in progresses)
    {
        p.HCApprovalStatus = "Reviewed";
        p.HCReviewedById = user.Id;
        p.HCReviewedAt = DateTime.UtcNow;
    }
    await _context.SaveChangesAsync();

    await _auditLog.LogAsync(user.Id, user.FullName ?? user.Id, "BatchHCApprove",
        $"Batch HC Approve {progresses.Count} deliverable: IDs {string.Join(",", progresses.Select(p => p.Id))}",
        targetType: "ProtonDeliverableProgress");

    return Json(new { success = true, approvedCount = progresses.Count });
}

public class BatchHCApproveRequest
{
    public List<int> ProgressIds { get; set; } = new();
}
```

### Workload Count Query — CoachCoacheeMapping
```csharp
// Tidak perlu query baru — data sudah ada di `grouped` list (L3674-3697)
// grouped sudah memiliki: ActiveCount = g.Count(r => r.Mapping.IsActive)
// Tambah ke View: @foreach dalam table menampilkan @coach.ActiveCount sebagai "Jumlah Coachee Aktif"
// Tidak ada perubahan query yang diperlukan
```

### Status Transition Validation — OverrideSave
```csharp
// Tambah setelah L1386 (setelah FindAsync)
var illegalTransitions = new Dictionary<string, HashSet<string>>
{
    { "Approved", new HashSet<string> { "Pending" } }, // Cannot regress Approved to Pending directly
};
if (illegalTransitions.TryGetValue(progress.Status, out var blockedTargets)
    && blockedTargets.Contains(req.NewStatus))
{
    return Json(new { success = false,
        message = $"Transisi dari '{progress.Status}' ke '{req.NewStatus}' tidak diizinkan." });
}
```

---

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| Export tanpa role attr | Semua export harus punya `[Authorize(Roles=...)]` | Security fix |
| Override tanpa transition validation | Tambah illegal transition check | Data integrity |
| Stats dihitung dari unfiltered `allProgresses` | Verifikasi stats konsisten dengan filter | Accuracy fix |
| Tidak ada bottleneck visibility | Horizontal bar chart top 10 pending > 30 hari | Differentiator feature |

---

## Environment Availability

Step 2.6: SKIPPED — phase ini adalah pure code changes tanpa external dependencies baru. Semua tools (ClosedXML, Chart.js, EF Core, ASP.NET Core) sudah tersedia di project.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (existing project pattern) |
| Config file | none |
| Quick run command | `dotnet build` |
| Full suite command | `dotnet build` + browser UAT |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MON-01 | Dashboard stats akurat per role | manual | `dotnet build` | N/A |
| MON-01 | Chart bottleneck menampilkan data | manual | `dotnet build` | N/A |
| MON-02 | Filter cascade CoachingProton | manual | `dotnet build` | N/A |
| MON-02 | Pagination tidak split group | manual | `dotnet build` | N/A |
| MON-03 | Override tercatat di audit log | manual | `dotnet build` | N/A |
| MON-03 | Transition ilegal diblokir | manual | `dotnet build` | N/A |
| MON-04 | ExportHistoriProton punya role attr | code review | `dotnet build` | N/A |
| MON-04 | Export baru berfungsi | manual | `dotnet build` | N/A |
| DIFF-01 | Kolom jumlah coachee aktif muncul | manual | `dotnet build` | N/A |
| DIFF-02 | Batch approve HC berhasil | manual | `dotnet build` | N/A |
| DIFF-03 | Bottleneck chart muncul di dashboard | manual | `dotnet build` | N/A |

### Wave 0 Gaps
None — existing build system covers all phase requirements. No new test infrastructure needed.

---

## Open Questions

1. **Legal transition matrix untuk Override**
   - What we know: D-14 mengatakan "tidak bisa override ke status ilegal"
   - What's unclear: Apakah "ilegal" berarti semua regress diblokir, atau hanya Approved → Pending?
   - Recommendation: Block minimal Approved → Pending. Semua transisi lain (termasuk Approved → Rejected) diizinkan karena admin mungkin perlu undo approval yang salah.

2. **Coach role di ExportProgressExcel**
   - What we know: Role attr saat ini `"Sr Supervisor, Section Head, HC, Admin"` — Coach tidak ada
   - What's unclear: Apakah Coach memang tidak boleh export, atau ini oversight?
   - Recommendation: Tambah Coach ke role attr karena scope validation sudah ada di body action (section check). Coach seharusnya bisa export progress coachee mereka.

3. **ExportHistoriProton — role yang seharusnya**
   - What we know: Saat ini tidak ada role restriction
   - What's unclear: Apakah Coach level (level 5) boleh access ExportHistoriProton?
   - Recommendation: Minimal `[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]`. Coach bisa lihat HistoriProton via halaman detail tapi export agregat mungkin tidak perlu.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CDPController.cs` — inspeksi langsung L260-619 (Dashboard + BuildProtonProgressSubModelAsync), L1386-1685 (CoachingProton tracking), L2344-2409 (ExportProgressExcel)
- `Controllers/ProtonDataController.cs` — inspeksi langsung L1246-1421 (Override flow)
- `Controllers/AdminController.cs` — inspeksi langsung L3613-3735 (CoachCoacheeMapping)
- `Views/CDP/Shared/_CoachingProtonContentPartial.cshtml` — inspeksi L200-279 (Chart.js pattern)
- `.planning/phases/237-audit-monitoring-differentiator-enhancement/237-CONTEXT.md` — locked decisions D-01 s/d D-18

### Secondary (MEDIUM confidence)
- Chart.js documentation (built-in knowledge): `indexAxis: 'y'` untuk horizontal bar chart sudah merupakan Chart.js 3.x+ syntax, sesuai dengan version yang dipakai project (digunakan di partial view)

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — kode existing sudah diinspeksi langsung
- Architecture: HIGH — pattern sudah terdokumentasi dari kode actual
- Bug findings: HIGH untuk OverrideSave (audit trail ada, transition validation tidak ada) dan ExportHistoriProton (tidak ada role attr) — diverifikasi dari kode langsung
- Pitfalls: MEDIUM — beberapa perlu verifikasi browser (filter stats inconsistency, dropdown coachee saat tahun filter aktif)

**Research date:** 2026-03-23
**Valid until:** 2026-04-22 (stabil — tidak ada dependency library yang berubah)
