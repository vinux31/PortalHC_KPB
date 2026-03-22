# Phase 231: Audit Assessment Management & Monitoring — Research

**Researched:** 2026-03-22
**Domain:** ASP.NET Core MVC — Assessment CRUD, Package Management, SignalR Monitoring
**Confidence:** HIGH (berbasis pembacaan kode aktual)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Pendekatan hybrid: gunakan rekomendasi Phase 228 sebagai checklist PLUS audit independen dari kode untuk cari bug/issue yang tidak terdeteksi riset
- **D-02:** Prioritas fix: must-fix + should-improve. Nice-to-have di-defer ke backlog
- **D-03:** Output: HTML report di `docs/` (konsisten dengan audit docs existing)
- **D-04:** Pembagian 2 plans: Plan 1 = ManageAssessment CRUD + filter/list. Plan 2 = Monitoring + Package + Proton special case
- **D-05:** Full list audit: filter kategori/status, search by judul, pagination, sorting, column display, empty state, performance
- **D-06:** Phase 228 flag filter sebagai must-fix — prioritaskan
- **D-07:** Audit ulang renewal integration dari perspektif CreateAssessment (renewSessionId/renewTrainingId params) — independen dari fix Phase 229-230
- **D-08:** Validasi lengkap: judul, kategori, tanggal, peserta, passing grade
- **D-09:** Standard audit: data preservation, package warning, field validation, edge cases (edit saat exam berlangsung)
- **D-10:** Audit KEDUA variant: DeleteAssessment (single) dan DeleteAssessmentGroup — cascade cleanup packages, questions, sessions, responses
- **D-11:** Audit akurasi stats (participant count, completed, passed), filter kategori/status, dan group status derivation
- **D-12:** Monitoring detail sudah punya SignalR real-time (progressUpdate, workerStarted, workerSubmitted). Audit fungsionalitas handlers + reconnection behavior saat connection lost + fallback
- **D-13:** Audit semua HC actions dengan kedalaman sama: Reset session, Force Close, Bulk Close, Regenerate Token
- **D-14:** Token card: copy dan regenerate berfungsi, token lama invalidated
- **D-15:** Audit lengkap: CRUD package, ImportPackageQuestions (Excel), assignment peserta, PreviewPackage
- **D-16:** ImportPackageQuestions: validasi format, error handling, duplicate detection, partial import behavior
- **D-17:** PreviewPackage: pastikan soal render benar, gambar/media, jawaban tersembunyi
- **D-18:** Assignment conflict handling: audit behavior saat assign ke peserta yang sudah punya active session atau reassign
- **D-19:** Interview mode Tahun 3: 5 aspek interview (Pengetahuan Teknis, Kemampuan Operasional, Keselamatan Kerja, Komunikasi & Kerjasama, Sikap Profesional), scoring per aspek, total score, special UI
- **D-20:** Proton exam flow Tahun 1-2: audit special handling vs assessment reguler
- **D-21:** Proton package/soal: audit format khusus vs assessment reguler
- **D-22:** Proton monitoring: badge "Assessment Proton", group status, pass rate calculation
- **D-23:** Pastikan SEMUA CRUD dan HC actions punya audit log, format konsisten, warning-only untuk audit log failure
- **D-24:** Verify semua assessment actions punya [Authorize(Roles)] yang benar dan konsisten (Admin, HC)
- **D-25:** Basic verification: data akurat (score, status, tanggal), link dari monitoring detail berfungsi

### Claude's Discretion

- Urutan audit per-action dalam setiap plan
- Detail level HTML report layout
- Pendekatan fix untuk issue yang ditemukan (refactor vs patch)

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| AMGT-01 | CreateAssessment form validasi lengkap (judul, kategori, tanggal, peserta, passing grade) | POST validation sudah ada (lines 1150-1200) tapi kategori tidak divalidasi — GAP ditemukan |
| AMGT-02 | EditAssessment mempertahankan data existing dan warning jika ada package terkait | GET EditAssessment sudah load siblings & hitung packageCount — perlu audit field preservation di POST |
| AMGT-03 | DeleteAssessment cascade cleanup benar (packages, questions, sessions, responses) | Single delete hanya hapus PackageUserResponses + AttemptHistory; AssessmentPackages & PackageQuestions tidak dihapus manual (bergantung cascade DB) — perlu verifikasi |
| AMGT-04 | Package assignment ke peserta berfungsi (single + bulk assign) | BulkAssign ada di EditAssessment POST — perlu audit ManagePackages assignment flow |
| AMGT-05 | ManageAssessment list view filter dan search berfungsi | Filter kategori/status TIDAK ADA di ManageAssessment list — GAP dikonfirmasi (D-06 flag) |
| AMON-01 | AssessmentMonitoring group list menampilkan stats real-time (participant count, completed, passed, status) | GroupStatus derivation logic ada, tapi IsCompleted dihitung dari CompletedAt OR Score — edge case mungkin |
| AMON-02 | MonitoringDetail per-participant live progress (answered/total, status, score, time remaining) | SignalR handlers ada (progressUpdate, workerStarted, workerSubmitted) — Time Remaining selalu "—" saat load awal |
| AMON-03 | HC actions berfungsi (Reset, Force Close, Bulk Close, Close Early, Regenerate Token) | ResetAssessment + AkhiriUjian + AkhiriSemuaUjian ada. "Force Close" dan "Bulk Close" adalah nama lain dari AkhiriUjian/AkhiriSemuaUjian. CloseEarly dihapus di Phase 162 — dead ref check diperlukan |
| AMON-04 | Token card dengan copy dan regenerate berfungsi | copyToken() + regenToken() ada di view; RegenerateToken endpoint ada — perlu audit JS behavior |

</phase_requirements>

---

## Summary

Phase 231 mengaudit halaman ManageAssessment (CRUD + list), AssessmentMonitoring (group list + detail), ManagePackages, dan Assessment Proton special handling. Audit berbasis pembacaan kode aktual di AdminController.cs (lines 633-2336, 6050-6420+) dan view-view terkait.

**Gap paling kritis yang ditemukan dari audit kode:**
1. **AMGT-05 (must-fix):** ManageAssessment list view tidak punya filter kategori/status — hanya ada search box. Ini adalah flag Phase 228 yang dikonfirmasi.
2. **AMGT-01 partial gap:** CreateAssessment POST tidak memvalidasi `Category` field (bisa kosong string).
3. **AMGT-03 risk:** DeleteAssessment single tidak secara eksplisit menghapus AssessmentPackages dan PackageQuestions — bergantung pada cascade DB. Perlu konfirmasi FK cascade behavior.
4. **AMON-02:** Time Remaining column di MonitoringDetail selalu render "—" pada initial load; hanya SignalR yang update nilai ini tapi tidak ada handler yang menulis ke kolom timeremaining-cell.
5. **D-13 terminology:** "Force Close" = AkhiriUjian (individual), "Bulk Close" = AkhiriSemuaUjian (group). "CloseEarly" sudah dihapus di Phase 162 — comment di line 3181 mengkonfirmasi.

**Primary recommendation:** Plan 1 fokus pada AMGT-05 (filter list), AMGT-01 (validasi kategori), dan AMGT-03 (cascade audit). Plan 2 fokus pada AMON-02 (time remaining), package assignment, dan Proton special case.

---

## Standard Stack

### Core (already in use — no new installs)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | (project version) | Controller/View framework | Established project stack |
| Entity Framework Core | (project version) | ORM + cascade delete config | Established project stack |
| SignalR (server + client) | (project version) | Real-time monitoring push | Sudah terintegrasi di assessmentHub |
| ClosedXML | (project version) | Excel import/export | Pattern sama dengan ImportWorkers |
| Bootstrap 5 | (project version) | UI components | Established project stack |

### Established Patterns (dari kode aktual)

| Pattern | Lokasi | Catatan |
|---------|--------|---------|
| TempData flash messages | Semua CRUD actions | Success/Error/Warning keys |
| Audit log try-catch | Semua destructive actions | `_auditLog.LogAsync` dengan warning-only on failure |
| `[Authorize(Roles = "Admin, HC")]` | Semua assessment actions | Per-action attribute (class-level hanya [Authorize]) |
| Grouping by (Title, Category, Schedule.Date) | ManageAssessment + Monitoring | Key identitas group assessment |
| `ValidateAntiForgeryToken` | Semua POST actions | Termasuk form di view |
| `GenerateSecureToken()` | CreateAssessment + RegenerateToken | Alphanumeric 6-char, excluding ambiguous chars |

---

## Architecture Patterns

### Grouping Model
Assessment di portal ini TIDAK one-session-per-peserta yang standalone — setiap "assessment event" adalah satu grup dengan banyak `AssessmentSession` (satu per peserta), diidentifikasi dengan key `(Title, Category, Schedule.Date)`. Ini fundamental untuk semua operasi CRUD, monitoring, dan deletion.

```
AssessmentSession (per peserta)
  ├── AssessmentPackage (per session, package mode)
  │     ├── PackageQuestion
  │     │     └── PackageOption
  │     └── UserPackageAssignment (link: session ↔ package)
  └── PackageUserResponse (jawaban peserta per question)
```

### Cascade Delete — Temuan Kritis

**DeleteAssessment (single session)** — line 1899:
- Hapus manual: `PackageUserResponses` (Restrict FK), `AssessmentAttemptHistory` (no FK)
- Bergantung cascade DB: `UserPackageAssignments` (comment: "cascade-deleted by DB")
- TIDAK dihapus manual: `AssessmentPackage` + `PackageQuestion` + `PackageOption`

Artinya: jika cascade DB tidak dikonfigurasi untuk AssessmentPackage → AssessmentSession, maka delete akan gagal dengan FK violation jika ada packages. Perlu verifikasi schema migration.

**DeleteAssessmentGroup** — line 1979:
- Sama seperti single, diperluas ke semua siblings

### SignalR Architecture
```
assessmentHub (window.assessmentHub — shared, diinisialisasi di _Layout atau partial)
  ├── JoinMonitor(batchKey)  → join SignalR group untuk monitor halaman
  ├── progressUpdate        → update progress-cell (answered/total)
  ├── workerStarted         → update status-cell ke InProgress + increment count
  └── workerSubmitted       → update score, result, completed-at, decrement inprogress
```

Reconnect handling: `onreconnecting` → badge "Reconnecting...", `onreconnected` → rejoin monitor group + badge "Live".

**Gap yang ditemukan:** Initial state badge menggunakan `setTimeout(2000)` — jika hub connect lebih dari 2 detik, badge tetap "Connecting..." meski sudah connected. Ini fragile.

### HC Actions Mapping

| CONTEXT.md nama | Controller action | Scope |
|-----------------|-------------------|-------|
| Reset | `ResetAssessment` (POST) | Individual session |
| Force Close | `AkhiriUjian` (POST) | Individual session (InProgress only) |
| Bulk Close | `AkhiriSemuaUjian` (POST) | Semua Open/InProgress di group |
| Close Early | DIHAPUS di Phase 162 | — |
| Regenerate Token | `RegenerateToken` (POST, JSON) | Semua siblings di group |

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel import soal | Custom parser | ClosedXML (sudah ada `DownloadQuestionTemplate` + `ImportPackageQuestions`) | Pattern sudah proven di ImportWorkers |
| Secure token generation | `Random.Next()` | `GenerateSecureToken()` pakai `RandomNumberGenerator` | Sudah ada, cryptographically secure |
| Group identity | Custom ID | Tuple `(Title, Category, Schedule.Date)` | Established grouping key di seluruh codebase |
| Cascade delete | Manual loop | EF Core + DB FK cascade (tapi HARUS diverifikasi schema) | DB cascade lebih reliable dari manual loop |
| SignalR connection management | Custom polling | `window.assessmentHub` (shared hub) | Sudah ada, hanya perlu audit handler |

---

## Common Pitfalls

### Pitfall 1: Filter Kategori/Status Tidak Ada di ManageAssessment
**What goes wrong:** HC/Admin tidak bisa filter assessment berdasarkan kategori atau status — harus scroll manual di list.
**Why it happens:** ManageAssessment GET action menerima parameter `category` dan `statusFilter` tapi TIDAK digunakan untuk filter `AssessmentSession` query (lines 636-707). Parameter diteruskan ke Training tab saja (line 731).
**How to avoid:** Tambahkan filter di `managementQuery` sebelum `ToListAsync()`, konsisten dengan AssessmentMonitoring yang sudah punya filter kategori (line 2147).
**Warning signs:** Search bar ada, tapi tidak ada dropdown filter di tab Assessment Groups.

### Pitfall 2: Validasi Kategori Kosong di CreateAssessment POST
**What goes wrong:** Assessment bisa dibuat tanpa memilih kategori (Category = "").
**Why it happens:** POST CreateAssessment (line 1145) tidak memvalidasi `model.Category`. ModelState validation untuk Title ada, tapi Category tidak.
**How to avoid:** Tambahkan `if (string.IsNullOrWhiteSpace(model.Category)) ModelState.AddModelError(...)`.
**Warning signs:** AMGT-01 requires "validasi lengkap (judul, kategori, tanggal, peserta, passing grade)" — kategori missing.

### Pitfall 3: Cascade Delete Packages Tidak Eksplisit
**What goes wrong:** DeleteAssessment bisa gagal dengan FK violation jika `AssessmentPackage.AssessmentSessionId` tidak dikonfigurasi cascade delete di EF migrations.
**Why it happens:** Code comment line 1938 menyebut UserPackageAssignments di-cascade oleh DB, tapi AssessmentPackages tidak disebutkan. Jika ada FK constraint tanpa cascade, `_context.AssessmentSessions.Remove(assessment)` akan throw.
**How to avoid:** Audit migration file untuk `AssessmentPackage` FK behavior. Jika belum cascade, tambahkan hapus manual packages sebelum hapus session (sama seperti DeletePackage action di line 6134).
**Warning signs:** Tidak ada explicit `_context.AssessmentPackages.RemoveRange(...)` di DeleteAssessment.

### Pitfall 4: Time Remaining Selalu "—" di MonitoringDetail
**What goes wrong:** Kolom "Time Remaining" di tabel per-participant selalu menampilkan "—" untuk semua status.
**Why it happens:** Initial render dari server tidak mengisi nilai. SignalR handlers (`progressUpdate`, `workerStarted`, `workerSubmitted`) tidak menulis ke `.timeremaining-cell`. Tidak ada client-side countdown timer.
**How to avoid:** Untuk peserta InProgress, hitung `TimeRemaining = Schedule + DurationMinutes - Now` di controller dan pass ke ViewModel, atau implementasi JS countdown berdasarkan `StartedAt + DurationMinutes`.
**Warning signs:** HTML cell di line 258: `<td class="timeremaining-cell text-muted">—</td>` — hardcoded dash.

### Pitfall 5: Edit Assessment saat Exam Berlangsung
**What goes wrong:** EditAssessment POST tidak mengecek apakah ada session dengan status "InProgress" dalam group sebelum mengubah Title, Category, atau Schedule.
**Why it happens:** Line 1668 hanya guard terhadap `assessment.Status == "Completed"`. InProgress tidak dicek.
**How to avoid:** Tambahkan check: jika ada sibling dengan StartedAt != null && CompletedAt == null, tampilkan warning (atau block edit untuk field kritikal seperti Title/Category/Schedule).

### Pitfall 6: ManageAssessment Filter Status (tab "assessment") — Bug yang Dikonfirmasi Phase 228
**What goes wrong:** ManageAssessment menampilkan semua assessment dalam 7-day window tanpa filter status. HC tidak bisa cepat lihat hanya assessment "Open" atau "Upcoming".
**Why it happens:** Berbeda dengan AssessmentMonitoring yang punya status filter (line 2202-2213), ManageAssessment tidak punya.
**How to avoid:** Sesuai D-06, ini must-fix: tambahkan filter kategori dan status di ManageAssessment GET action.

---

## Code Examples

### Filter yang Perlu Ditambahkan di ManageAssessment GET

```csharp
// Source: audit kode aktual line 636-680 (harus ditambahkan)
// Di AdminController.cs, ManageAssessment GET, setelah search filter:

if (!string.IsNullOrEmpty(category))
    managementQuery = managementQuery.Where(a => a.Category == category);

// Status filter perlu diterapkan SETELAH grouping (sama dengan AssessmentMonitoring):
// if (statusFilter == "Open") grouped = grouped.Where(g => g.Status == "Open").ToList();
```

### Validasi Kategori di CreateAssessment POST

```csharp
// Source: audit kode aktual line 1145-1200 (gap yang ditemukan)
// Tambahkan setelah validasi Title:
if (string.IsNullOrWhiteSpace(model.Category))
{
    ModelState.AddModelError("Category", "Kategori wajib dipilih.");
}
```

### Cascade Delete yang Lebih Aman untuk DeleteAssessment

```csharp
// Source: audit kode aktual line 1899 + referensi DeletePackage line 6134
// Tambahkan sebelum _context.AssessmentSessions.Remove(assessment):
var packages = await _context.AssessmentPackages
    .Include(p => p.Questions).ThenInclude(q => q.Options)
    .Where(p => p.AssessmentSessionId == id)
    .ToListAsync();
foreach (var pkg in packages)
{
    foreach (var q in pkg.Questions)
        _context.PackageOptions.RemoveRange(q.Options);
    _context.PackageQuestions.RemoveRange(pkg.Questions);
}
_context.AssessmentPackages.RemoveRange(packages);
```

### Token Copy JS Pattern (sudah ada, perlu audit)

```javascript
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml (sudah ada)
// copyToken() dan regenToken() menggunakan Clipboard API + fetch POST ke /Admin/RegenerateToken
// Pattern ini sudah benar — yang perlu diaudit adalah error handling saat clipboard permission denied
```

### AssessmentMonitoring Status Filter (referensi pattern)

```csharp
// Source: AdminController.cs line 2202-2213 — pattern yang benar untuk filtering post-grouping
if (string.IsNullOrEmpty(status))
    grouped = grouped.Where(g => g.GroupStatus != "Closed").ToList();
else if (status == "Open" || status == "Upcoming" || status == "Closed")
    grouped = grouped.Where(g => g.GroupStatus == status).ToList();
// status == "All" → no filter
```

---

## Findings per Requirement (Audit Kode)

### AMGT-01: CreateAssessment Validasi
**Status:** Partial — beberapa field sudah divalidasi, kategori tidak
- Title: divalidasi (ModelState)
- Schedule: divalidasi (past/future range)
- Duration: divalidasi (kecuali Proton Tahun 3 dengan sentinel DurationMinutes=0)
- UserIds: divalidasi (min 1, max 50)
- PassPercentage: TIDAK ada validasi eksplisit di POST (model binding saja)
- **Category: TIDAK divalidasi** — GAP
- AccessToken: divalidasi jika IsTokenRequired=true

### AMGT-02: EditAssessment Data Preservation
**Status:** Sebagian besar baik, perlu audit edge case
- GET: load sibling sessions, hitung packageCount, tampilkan assigned users
- POST: validasi mirip CreateAssessment, termasuk Rate limit 50 users
- Package warning: `ViewBag.PackageCount` dikirim ke view — perlu audit apakah view menampilkan warning
- **Gap:** Edit COMPLETED session diblokir (line 1668), tapi edit saat InProgress tidak diblokir

### AMGT-03: DeleteAssessment Cascade
**Status:** RISK — perlu verifikasi FK cascade di schema
- `PackageUserResponses` dihapus manual (FK: Restrict)
- `AssessmentAttemptHistory` dihapus manual (no FK)
- `UserPackageAssignments` — comment "cascade-deleted by DB"
- `AssessmentPackage` + `PackageQuestion` + `PackageOption` — TIDAK ada hapus manual, diasumsikan cascade

### AMGT-04: Package Assignment
**Status:** Perlu audit lebih dalam
- BulkAssign ada di EditAssessment POST (line 1865)
- ManagePackages menampilkan assignment counts per package
- Tidak ada explicit "assign package to user" form — assignment terjadi lewat ReshufflePackage/AssignPackage actions (perlu baca lebih lanjut)

### AMGT-05: ManageAssessment Filter
**Status:** GAP DIKONFIRMASI — MUST-FIX
- Search by title/category/name/NIP: ADA
- Filter kategori: TIDAK ADA (parameter diterima tapi tidak digunakan untuk query assessment)
- Filter status: TIDAK ADA
- Pagination: ADA (20 per page, PaginationHelper)

### AMON-01: Monitoring Stats
**Status:** Ada tapi ada edge case
- TotalCount, CompletedCount, PassedCount, PendingCount: semua ada di MonitoringGroupViewModel
- GroupStatus derivation: Open > Upcoming > Closed (line 2177-2182)
- `IsCompleted = a.CompletedAt != null || a.Score != null` — dual-condition bisa menyebabkan inconsistency

### AMON-02: Live Progress
**Status:** Ada SignalR, ada gap Time Remaining
- progressUpdate: update progress-cell (answered/total)
- workerStarted: update status badge
- workerSubmitted: update score + result + completedAt
- **Time Remaining: selalu "—"** — no server data, no client countdown

### AMON-03: HC Actions
**Status:** Implementasi ada, nama berbeda dari spec
- Reset → `ResetAssessment` POST
- Force Close → `AkhiriUjian` POST (individual, InProgress only)
- Bulk Close → `AkhiriSemuaUjian` POST (semua Open/InProgress di group)
- Close Early → DIHAPUS di Phase 162, diganti AkhiriSemuaUjian (line 3181)
- Regenerate Token → `RegenerateToken` POST (JSON response)
- Semua punya `[Authorize(Roles = "Admin, HC")]` dan `[ValidateAntiForgeryToken]`

### AMON-04: Token Card
**Status:** Implementasi ada, perlu audit JS
- Token display: `<code id="token-display">@Model.AccessToken</code>`
- Copy button: `copyToken()` JS function
- Regenerate button: `regenToken(this)` → AJAX POST ke RegenerateToken
- RegenerateToken endpoint update semua siblings dengan token baru (line 2083-2093)
- Token lama di-invalidate implisit (semua siblings dapat token baru)

---

## Assessment Proton Special Cases

### Tahun 1-2 vs Tahun 3

| Aspek | Tahun 1-2 | Tahun 3 |
|-------|-----------|---------|
| Mode | Exam online (package/soal) | Interview (5 aspek, tidak ada soal) |
| DurationMinutes | > 0 | 0 (sentinel value) |
| Input HC | Tidak ada input manual | `SubmitInterviewResults` form di MonitoringDetail |
| Scoring | Auto-grade dari jawaban | Manual per aspek (1-5 scale) + isPassed |
| ViewBag.GroupTahunKe | Tidak di-set | "Tahun 3" → tampilkan interview form |

### Interview Aspects (Tahun 3)
5 aspek yang di-hardcode di view (line 27-31):
1. Pengetahuan Teknis
2. Kemampuan Operasional
3. Keselamatan Kerja
4. Komunikasi & Kerjasama
5. Sikap Profesional

Scoring: dropdown 1-5 per aspek. Hasil disimpan sebagai `InterviewResultsJson` di `AssessmentSession`.

### Proton Badge
View menggunakan badge `bg-purple` untuk kategori "Assessment Proton" (bukan "Proton") — perlu verifikasi CSS class `bg-purple` terdefinisi.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (tidak ada unit test framework terdeteksi) |
| Config file | none |
| Quick run command | Build: `dotnet build` |
| Full suite command | Manual flow verification di browser |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| AMGT-01 | CreateAssessment tidak submit tanpa kategori | manual | — | N/A |
| AMGT-02 | EditAssessment preserve data, show package warning | manual | — | N/A |
| AMGT-03 | DeleteAssessment tidak leave orphan packages | manual + DB check | — | N/A |
| AMGT-04 | Package assignment single + bulk | manual | — | N/A |
| AMGT-05 | ManageAssessment filter kategori/status berfungsi | manual | — | N/A |
| AMON-01 | Stats akurat di group list | manual | — | N/A |
| AMON-02 | Live progress update saat peserta jawab soal | manual (butuh 2 tab) | — | N/A |
| AMON-03 | HC actions: Reset, Force Close, Bulk Close | manual | — | N/A |
| AMON-04 | Token copy + regenerate | manual | — | N/A |

### Wave 0 Gaps
None — project menggunakan manual browser testing.

---

## Open Questions

1. **FK Cascade untuk AssessmentPackage**
   - What we know: Code comment mengklaim `UserPackageAssignments` cascade-deleted, tapi AssessmentPackage tidak disebutkan
   - What's unclear: Apakah EF migration/DB schema sudah set cascade delete untuk `AssessmentPackage.AssessmentSessionId`?
   - Recommendation: Baca migration file paling baru untuk tabel `AssessmentPackages`. Jika tidak ada cascade, Plan 1 harus tambahkan explicit delete.

2. **Time Remaining — approach terbaik**
   - What we know: Kolom ada tapi selalu "—". DurationMinutes ada di session, StartedAt ada.
   - What's unclear: Apakah kita ingin server-rendered initial value, atau JS countdown dari StartedAt + DurationMinutes?
   - Recommendation: JS countdown (client-side) lebih real-time. Load StartedAt + DurationMinutes ke data attribute di `<tr>`, hitung `timeLeft = startedAt + durationMs - now`.

3. **Package Assignment Flow**
   - What we know: BulkAssign terjadi di EditAssessment POST saat `NewUserIds` ditambahkan. Tapi bagaimana package di-assign ke specific user? Apakah ada "Assign Package to User" action?
   - What's unclear: Perlu baca lebih lanjut ReshufflePackage dan AssignPackage actions
   - Recommendation: Audit lines sekitar 3183+ (`ReshufflePackage`) di AdminController.cs pada Plan 2.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| CloseEarly (terminate individual) | AkhiriUjian (individual force-grade) | Phase 162 | Auto-grading dari saved answers |
| Legacy question format (AssessmentQuestion/UserResponse) | Package format (AssessmentPackage/PackageQuestion/PackageUserResponse) | v8.0 CLEN-02 | Legacy deprecated, packages adalah standar |
| Single session per assessment | Group model (Title+Category+Date) | v6.x-ish | Semua monitoring + delete operate pada group |

---

## Sources

### Primary (HIGH confidence)
- `Controllers/AdminController.cs` lines 633-2336 — ManageAssessment, CreateAssessment, EditAssessment, DeleteAssessment, DeleteAssessmentGroup, RegenerateToken, AssessmentMonitoring, AssessmentMonitoringDetail, ResetAssessment, AkhiriUjian, AkhiriSemuaUjian
- `Controllers/AdminController.cs` lines 6050-6420+ — ManagePackages, CreatePackage, DeletePackage, PreviewPackage, ImportPackageQuestions
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — SignalR handlers, HC action buttons, token card, interview Proton Tahun 3
- `Views/Admin/ManageAssessment.cshtml` — List view, search, tabs, empty state
- `Views/Admin/ManagePackages.cshtml` — Package CRUD, ET coverage matrix
- `Views/Admin/CreateAssessment.cshtml` — Form wizard, renewal mode banner
- `.planning/phases/231-audit-assessment-management-monitoring/231-CONTEXT.md` — User decisions D-01 to D-25

### Secondary (MEDIUM confidence)
- `.planning/REQUIREMENTS.md` — Requirement definitions AMGT + AMON
- `.planning/STATE.md` — Accumulated context v8.1

---

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — dibaca dari kode aktual
- Architecture: HIGH — GroupBy pattern dikonfirmasi di multiple locations
- Pitfalls: HIGH — bug konkret dikonfirmasi dengan line number
- Cascade delete risk: MEDIUM — butuh konfirmasi migration schema

**Research date:** 2026-03-22
**Valid until:** 2026-04-21 (30 days — stack stable)
