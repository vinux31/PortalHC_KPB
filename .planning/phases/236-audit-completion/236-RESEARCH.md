# Phase 236: Audit Completion - Research

**Researched:** 2026-03-23
**Domain:** ASP.NET Core MVC — Proton Coaching completion flow (ProtonFinalAssessment, CoachingSession, HistoriProton, 3-year lifecycle)
**Confidence:** HIGH — semua temuan berdasarkan pembacaan langsung source code

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

- **D-01:** Claude investigasi apakah unique constraint sudah ada di DB untuk ProtonTrackAssignmentId — jika belum, tambah migration + controller guard
- **D-02:** Claude audit existing CompetencyLevelGranted logic dan pastikan accuracy-nya
- **D-03:** Final assessment hanya bisa di-create oleh HC/Admin — role guard harus benar
- **D-04:** Jika final assessment sudah ada untuk assignment, block + pesan error "Final assessment sudah ada untuk assignment ini" — tidak boleh create duplikat
- **D-05:** Session hanya di-create via SubmitEvidenceWithCoaching — tidak ada standalone session creation flow
- **D-06:** Claude audit existing action items dan pastikan status tracking konsisten — tidak perlu tambah state baru
- **D-07:** Session bisa diedit/dihapus tapi setiap perubahan harus tercatat di audit log
- **D-08:** Setiap session wajib ter-link ke 1 ProtonDeliverableProgressId — tidak boleh orphan session
- **D-09:** Claude investigasi apakah ada legacy CoachingLog data yang masih direferensikan di HistoriProton view
- **D-10:** Claude audit HistoriProtonDetail completeness — identifikasi gap/duplikasi di timeline
- **D-11:** Audit view DAN export — data di ExportHistoriProton harus konsisten dengan data di view
- **D-12:** Multi-year coachee: tampilkan data terpisah per tahun (Tahun 1 selesai, Tahun 2 ongoing sebagai section tersendiri)
- **D-13:** Completion criteria: semua ProtonDeliverableProgress di track status Approved **DAN** final assessment proton tahun tersebut sudah selesai/lulus
- **D-14:** Transisi antar tahun manual oleh HC/Admin — assign track tahun berikutnya secara manual
- **D-15:** Setelah coachee selesai Tahun 3 (completion criteria terpenuhi), mapping ditandai completed/graduated
- **D-16:** Competency level per tahun independen — setiap tahun punya CompetencyLevelGranted sendiri dari final assessment-nya

### Claude's Discretion

- Implementasi unique constraint migration detail
- Audit log mechanism untuk session edit/delete (existing AuditLog service atau tambahan)
- Completion status marker implementation (field baru atau status existing)
- Legacy CoachingLog handling approach berdasarkan investigasi
- HistoriProton gap fix detail berdasarkan audit findings

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| COMP-01 | Audit Final Assessment — tambah DB unique constraint pada ProtonTrackAssignmentId, competency level granting accuracy | Temuan: unique constraint BELUM ada, guard di AdminController ada tapi bersyarat (hanya untuk interview-pass path), tidak ada HC create flow yang ditemukan di CDPController |
| COMP-02 | Audit Coaching Sessions — linkage ke deliverable progress, action items status tracking, session CRUD integrity | Temuan: session hanya dibuat via SubmitEvidenceWithCoaching (benar), ProtonDeliverableProgressId nullable (risiko orphan), edit/delete belum ada di CDPController |
| COMP-03 | Audit HistoriProton — timeline accuracy, legacy CoachingLog coexistence, data completeness | Temuan: CoachingLog tidak direferensikan sama sekali di HistoriProton/HistoriProtonDetail — data sudah bersih; view vs export sudah konsisten (logic identik) |
| COMP-04 | Audit 3-year journey — lifecycle end-to-end, assignment transition, completion flow | Temuan: CoachCoacheeMapping tidak punya field graduated/completed — perlu migration baru; completion criteria belum di-enforce di controller |
</phase_requirements>

---

## Summary

Phase ini adalah audit bug-fix murni terhadap 4 area completion flow Proton Coaching. Semua temuan didasarkan pada pembacaan langsung source code di CDPController.cs, AdminController.cs, Models/, dan ApplicationDbContext.cs.

**Temuan paling kritis:**
1. `ProtonFinalAssessment` tidak punya unique constraint di DB pada `ProtonTrackAssignmentId` — hanya ada guard parsial di AdminController untuk path interview-pass, dan itu pun cukup memadai untuk path tersebut.
2. Tidak ada action HC create final assessment di CDPController — hanya dibuat otomatis dari AdminController ketika interview Tahun 3 lulus.
3. `CoachingSession.ProtonDeliverableProgressId` adalah nullable — tidak ada DB-level enforcement bahwa setiap session harus punya link.
4. `CoachCoacheeMapping` tidak punya field graduated/completed — field ini perlu ditambah via migration.
5. Legacy `CoachingLog` tidak direferensikan di HistoriProton — tidak ada masalah coexistence.

**Primary recommendation:** Tambah migration untuk (a) unique index `ProtonFinalAssessment.ProtonTrackAssignmentId` dan (b) field `IsCompleted`/`CompletedAt` di `CoachCoacheeMapping`, lalu tambah controller guards sesuai keputusan user.

---

## Standard Stack

### Core (tidak ada library baru — v8.2 decision)

| Library | Versi | Purpose |
|---------|-------|---------|
| Entity Framework Core | (project) | Migration, unique index, query |
| ASP.NET Core Identity | (project) | Role guard via `[Authorize(Roles = ...)]` |
| ClosedXML | (project) | Export Excel HistoriProton |
| AuditLog service | internal | Logging edit/delete session |

**Tidak ada library baru.** Keputusan v8.2: "no new libraries."

---

## Architecture Patterns

### Pola Migration Unique Index (EF Core)

```csharp
// Di ApplicationDbContext.OnModelCreating
builder.Entity<ProtonFinalAssessment>(entity =>
{
    // Tambahkan ini:
    entity.HasIndex(fa => fa.ProtonTrackAssignmentId).IsUnique();
    // ... existing config
});
```

Kemudian jalankan `dotnet ef migrations add AddUniqueConstraintFinalAssessment`.

### Pola Controller Guard (duplikat check)

```csharp
// Sebelum _context.ProtonFinalAssessments.Add(...)
var alreadyExists = await _context.ProtonFinalAssessments
    .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignmentId);
if (alreadyExists)
{
    TempData["Error"] = "Final assessment sudah ada untuk assignment ini.";
    return RedirectToAction(...);
}
```

Pola ini sudah ada di AdminController L2527 — replikasi ke HC create flow.

### Pola AuditLog Service

```csharp
await _auditLog.LogAsync(
    user.Id,
    actorName,
    "EditCoachingSession",
    $"Session ID={session.Id} diubah.",
    session.Id,
    "CoachingSession");
```

`_auditLog` sudah diinjeksi di CDPController — langsung pakai.

### Pola RecordStatusHistory (reusable helper)

```csharp
// CDPController L3183 — helper yang sudah ada
private void RecordStatusHistory(int progressId, string statusType, string actorId,
    string actorName, string actorRole, string? rejectionReason = null)
```

Tidak langsung relevan untuk coaching session CRUD, tapi polanya sama jika perlu tracking.

### Pola BeginTransaction (Phase 234/235)

```csharp
await using var tx = await _context.Database.BeginTransactionAsync();
try
{
    // ... operations
    await _context.SaveChangesAsync();
    await tx.CommitAsync();
}
catch { await tx.RollbackAsync(); throw; }
```

Pakai ini untuk operasi edit/delete session yang perlu atomic dengan audit log.

---

## Temuan Audit per Area

### COMP-01: ProtonFinalAssessment

**Bug ditemukan:**

1. **Unique constraint BELUM ada di DB** — ApplicationDbContext L352-361 hanya punya index `CoacheeId` dan composite `(CoacheeId, Status)`. Tidak ada `HasIndex(fa => fa.ProtonTrackAssignmentId).IsUnique()`.

2. **Guard parsial di AdminController** — AdminController L2526-2542 sudah punya guard `if (!alreadyExists)` untuk path interview-pass Tahun 3. Guard ini benar tapi **hanya melindungi satu creation path**.

3. **Tidak ada HC create action** — Tidak ada action `CreateFinalAssessment` di CDPController. `ProtonFinalAssessments` hanya dibuat otomatis dari `SubmitInterviewResults` di AdminController. Ini berarti seluruh HC create flow perlu diaudit: apakah HC bisa membuat final assessment secara manual?

   Berdasarkan CONTEXT.md D-03 ("Final assessment hanya bisa di-create oleh HC/Admin"), ada kemungkinan HC perlu create manually untuk kasus non-interview. Ini perlu dikonfirmasi saat planning.

4. **Query di L365 tidak scope ke assignment** — Di `BuildCoacheeProgressSubModelAsync` (L365), query `ProtonFinalAssessments.Where(fa => fa.CoacheeId == userId)` mengambil assessment terbaru per CoacheeId, bukan per assignment. Jika coachee punya 3 assignment (Tahun 1, 2, 3), query ini mengembalikan assessment terbaru tanpa memfilter per assignment — menyebabkan `CompetencyLevelGranted` di sub-model salah untuk assignment yang bukan terbaru.

**Perbaikan yang diperlukan:**
- Migration: tambah unique index `ProtonTrackAssignmentId` di `ProtonFinalAssessment`
- Fix query di L365: scope ke `ProtonTrackAssignmentId` yang aktif
- Jika HC butuh create manual: tambah action dengan role guard `[Authorize(Roles = "HC, Admin")]` + duplicate check

### COMP-02: CoachingSession

**Temuan:**

1. **Session creation — sudah benar via SubmitEvidenceWithCoaching** — CDPController L2181 membuat `CoachingSession` dengan `ProtonDeliverableProgressId = progress.Id`. Ini sudah benar sesuai D-05.

2. **ProtonDeliverableProgressId nullable** — Field di model adalah `int?` (nullable), bukan `int`. Tidak ada DB-level `NOT NULL` enforcement. Existing sessions yang dibuat via SubmitEvidence selalu punya value, tapi model membuka kemungkinan orphan insert dari kode lain.

3. **Edit/Delete CoachingSession belum ada** — Tidak ada action `EditCoachingSession` atau `DeleteCoachingSession` di CDPController. Sesuai D-07, perlu ditambah dengan audit log.

4. **ActionItem status tracking — sudah ada state** — `ActionItem.Status` punya 3 nilai: `"Open"`, `"In Progress"`, `"Done"`. Tidak perlu tambah state baru (sesuai D-06). Yang perlu diaudit adalah apakah view menampilkan status ini dengan akurat.

**Perbaikan yang diperlukan:**
- Tambah action `EditCoachingSession` (GET+POST) dengan audit log
- Tambah action `DeleteCoachingSession` (POST) dengan audit log
- Tambah non-nullable enforcement untuk `ProtonDeliverableProgressId` di model dan/atau migration

### COMP-03: HistoriProton

**Temuan:**

1. **Legacy CoachingLog — tidak direferensikan di HistoriProton** — Grep di CDPController untuk "CoachingLog" tidak mengembalikan hasil. HistoriProton dan HistoriProtonDetail tidak menggunakan CoachingLog sama sekali. Tidak ada masalah coexistence atau duplikasi data.

2. **HistoriProton view vs ExportHistoriProton — konsisten** — Logic di kedua action (L2795 dan L2940) identik: query assignments → finalAssessments → build HistoriProtonWorkerRow. Tidak ada divergensi.

3. **HistoriProtonDetail — data gap** — `HistoriProtonDetail` (L3095) hanya menampilkan assignment nodes dengan status (Lulus/Dalam Proses) dan CompetencyLevel. Tidak ada detail coaching sessions atau deliverable progress per assignment. Ini bisa jadi "gap" sesuai D-10 tergantung ekspektasi user.

4. **Multi-year display — sudah ada per-tahun** — HistoriProton list sudah memisah `Tahun1Done/InProgress`, `Tahun2Done/InProgress`, `Tahun3Done/InProgress`. Namun HistoriProtonDetail menampilkan nodes berurutan per `TahunUrutan` tanpa section separator visual.

5. **Status "Lulus" hanya berdasarkan ada/tidaknya final assessment** — Bukan berdasarkan completion criteria lengkap (semua deliverable Approved + final assessment). Ini bisa menyebabkan status "Lulus" muncul meski ada deliverable yang belum Approved.

**Perbaikan yang diperlukan:**
- Klarifikasi D-10 dengan planner: apakah detail sessions/deliverables perlu ditampilkan di HistoriProtonDetail?
- Perbaiki "Lulus" logic: validasi kedua kriteria (D-13)
- Tambah section separator visual per tahun di HistoriProtonDetail (D-12)

### COMP-04: Lifecycle Tahun 1→2→3

**Temuan:**

1. **CoachCoacheeMapping tidak punya field graduated/completed** — Model saat ini hanya punya `IsActive`, `StartDate`, `EndDate`. Tidak ada `IsCompleted` atau `IsGraduated`. Perlu migration baru (sesuai Claude's Discretion).

2. **Completion criteria tidak di-enforce di controller** — Tidak ada logic yang memeriksa "semua deliverable Approved DAN final assessment sudah ada" sebelum mengizinkan transisi atau marking.

3. **Transisi antar tahun sudah manual** — Tidak ada auto-transition. HC/Admin assign track tahun berikutnya via AdminController (assign ProtonTrackAssignment baru). Sesuai D-14.

4. **Completion criteria logic yang tepat:**
   ```csharp
   bool allApproved = progresses.All(p => p.Status == "Approved");
   bool hasFinalAssessment = await _context.ProtonFinalAssessments
       .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignment.Id);
   bool isComplete = allApproved && hasFinalAssessment;
   ```

**Perbaikan yang diperlukan:**
- Migration: tambah `IsCompleted` (bool) dan `CompletedAt` (DateTime?) ke `CoachCoacheeMapping`
- Tambah action `MarkMappingCompleted` di AdminController (setelah Tahun 3 lulus)
- Tambah validation helper `IsYearCompleted(assignmentId)` yang memeriksa kedua criteria (D-13)

---

## Don't Hand-Roll

| Problem | Jangan Bangun | Gunakan Ini |
|---------|---------------|-------------|
| Unique constraint enforcement | Custom duplicate check saja | EF Core `HasIndex(...).IsUnique()` + migration — DB-level guarantee |
| Audit logging | Custom log table baru | `_auditLog` (IAuditLogService) sudah tersedia di CDPController dan AdminController |
| Transaction wrapping | Manual try/catch tanpa transaction | `BeginTransactionAsync` pattern dari Phase 234/235 |
| Role guard | Manual role check di action body | `[Authorize(Roles = "HC, Admin")]` attribute |

---

## Common Pitfalls

### Pitfall 1: Unique Index Conflict Saat Migration
**Apa yang salah:** Jika sudah ada ProtonFinalAssessment duplikat di DB (dua record dengan ProtonTrackAssignmentId sama), migration akan gagal.
**Kenapa terjadi:** Migration `IsUnique()` gagal jika data existing sudah melanggar constraint.
**Cara menghindari:** Sebelum migration, jalankan query audit:
```sql
SELECT ProtonTrackAssignmentId, COUNT(*) as cnt
FROM ProtonFinalAssessments
GROUP BY ProtonTrackAssignmentId
HAVING COUNT(*) > 1;
```
Jika ada duplikat, hapus/merge dulu sebelum `dotnet ef database update`.

### Pitfall 2: ToDictionary() Crash jika Ada Duplikat
**Apa yang salah:** `assessments.ToDictionary(fa => fa.ProtonTrackAssignmentId)` di HistoriProton (L2844, L2987, L3136) akan throw `ArgumentException: An item with the same key has already been added` jika ada duplikat.
**Kenapa terjadi:** Tanpa unique constraint di DB, duplikat bisa masuk.
**Cara menghindari:** Tambah unique constraint SEBELUM atau bersamaan dengan perbaikan guard. Jika perlu aman sementara, ganti `ToDictionary` dengan `GroupBy(...).ToDictionary(g => g.Key, g => g.First())`.

### Pitfall 3: Query CoacheeId vs ProtonTrackAssignmentId
**Apa yang salah:** L365 query final assessment by `CoacheeId` saja → mengembalikan assessment dari tahun yang berbeda untuk coachee multi-year.
**Kenapa terjadi:** Query tidak scope ke assignment aktif.
**Cara menghindari:** Selalu scope query ProtonFinalAssessments via `ProtonTrackAssignmentId` milik assignment yang sedang diproses.

### Pitfall 4: Cascade Delete CoachCoacheeMapping
**Apa yang salah:** AdminController L4521 menghapus `ProtonFinalAssessments` sebelum `ProtonTrackAssignments` karena ada `OnDelete(DeleteBehavior.Restrict)`. Jika field baru `IsCompleted` ditambah ke mapping, pastikan delete cascade tetap benar.
**Cara menghindari:** Field baru di CoachCoacheeMapping tidak mempengaruhi cascade — aman ditambah.

### Pitfall 5: Session Edit/Delete Tanpa Ownership Check
**Apa yang salah:** Coach bisa edit session coaching orang lain.
**Cara menghindari:** Saat tambah EditCoachingSession/DeleteCoachingSession, selalu validasi `session.CoachId == user.Id` kecuali HC/Admin.

---

## Code Examples

### Pola Unique Index Migration

```csharp
// ApplicationDbContext.cs — tambah di blok ProtonFinalAssessment
builder.Entity<ProtonFinalAssessment>(entity =>
{
    entity.HasOne(fa => fa.ProtonTrackAssignment)
        .WithMany()
        .HasForeignKey(fa => fa.ProtonTrackAssignmentId)
        .OnDelete(DeleteBehavior.Restrict);
    entity.HasIndex(fa => fa.CoacheeId);
    entity.HasIndex(fa => new { fa.CoacheeId, fa.Status });
    // TAMBAH INI (COMP-01):
    entity.HasIndex(fa => fa.ProtonTrackAssignmentId).IsUnique();
});
```

### Pola Completion Check (D-13)

```csharp
private async Task<bool> IsYearCompletedAsync(int assignmentId)
{
    var progresses = await _context.ProtonDeliverableProgresses
        .Where(p => p.ProtonTrackAssignmentId == assignmentId)
        .ToListAsync();
    if (!progresses.Any()) return false;
    bool allApproved = progresses.All(p => p.Status == "Approved");
    bool hasFinalAssessment = await _context.ProtonFinalAssessments
        .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignmentId);
    return allApproved && hasFinalAssessment;
}
```

### Pola CoachCoacheeMapping Completion Field

```csharp
// CoachCoacheeMapping.cs — tambah field
/// <summary>True jika coachee sudah selesai Tahun 3 (graduated).</summary>
public bool IsCompleted { get; set; } = false;
/// <summary>Tanggal completion dikonfirmasi oleh HC/Admin.</summary>
public DateTime? CompletedAt { get; set; }
```

### Pola Edit CoachingSession dengan AuditLog

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditCoachingSession(int id, /* form params */)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    var session = await _context.CoachingSessions.FindAsync(id);
    if (session == null) return NotFound();

    // Ownership guard
    bool isHcOrAdmin = User.IsInRole("HC") || User.IsInRole("Admin");
    if (!isHcOrAdmin && session.CoachId != user.Id) return Forbid();

    // Update fields
    session.CatatanCoach = catatanCoach;
    session.Kesimpulan = kesimpulan;
    session.Result = result;
    session.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    var actorName = string.IsNullOrWhiteSpace(user.NIP)
        ? (user.FullName ?? "Unknown")
        : $"{user.NIP} - {user.FullName}";
    await _auditLog.LogAsync(user.Id, actorName, "EditCoachingSession",
        $"Session ID={id} diubah.", id, "CoachingSession");

    TempData["Success"] = "Sesi coaching berhasil diperbarui.";
    return RedirectToAction("Deliverable", new { id = session.ProtonDeliverableProgressId });
}
```

---

## State of the Art

| Kondisi Lama | Kondisi Setelah Phase 236 | Dampak |
|-------------|--------------------------|--------|
| Tidak ada unique constraint ProtonFinalAssessment | Unique index di DB | Duplikat tidak bisa masuk di level DB |
| Tidak ada field graduated di CoachCoacheeMapping | IsCompleted + CompletedAt | Lifecycle Tahun 3 bisa ditandai selesai |
| Tidak ada edit/delete CoachingSession | Action edit/delete dengan audit log | CRUD integrity terpenuhi |
| HistoriProton status "Lulus" = ada final assessment saja | Status "Lulus" = semua deliverable Approved + final assessment | Accuracy completion meningkat |

---

## Open Questions

1. **Apakah HC bisa membuat ProtonFinalAssessment secara manual (bukan dari interview)?**
   - Apa yang diketahui: Saat ini tidak ada HC create action di CDPController; hanya AdminController yang membuat via interview-pass path.
   - Yang tidak jelas: D-03 menyebut "HC/Admin bisa create" — apakah ini artinya perlu tambah action baru di CDPController?
   - Rekomendasi: Planner harus putuskan apakah perlu tambah `CreateFinalAssessment` GET+POST di CDPController, atau cukup guard di path yang sudah ada.

2. **Seberapa detail HistoriProtonDetail yang diharapkan user?**
   - Yang diketahui: D-12 minta "section terpisah per tahun". Saat ini nodes ditampilkan berurutan tanpa section header.
   - Yang tidak jelas: Apakah deliverable progress details dan coaching session summaries perlu ditampilkan di timeline?
   - Rekomendasi: Tambah visual section header per tahun minimal (sesuai D-12), deliverable details optional.

---

## Validation Architecture

> nyquist_validation tidak ditemukan di config — treat as enabled.

### Test Framework

Tidak ada test infrastructure yang terdeteksi di project ini (tidak ada pytest, jest, vitest, xunit). Tidak ada folder `tests/` atau `__tests__/`.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Cara Verify |
|--------|----------|-----------|-------------|
| COMP-01 | Tidak bisa create duplicate final assessment | Manual | Browser: buat 2x final assessment untuk assignment yang sama — harus error |
| COMP-01 | Unique constraint di DB | DB check | SQL: `PRAGMA index_list(ProtonFinalAssessments)` / migration snapshot |
| COMP-02 | Session ter-link ke deliverable | Manual | Browser: submit evidence → cek session punya ProtonDeliverableProgressId |
| COMP-02 | Edit/delete session terecord di audit log | Manual | Browser: edit session → cek AuditLogs table |
| COMP-03 | HistoriProton dan ExportHistoriProton konsisten | Manual | Browser: bandingkan data view vs download Excel |
| COMP-04 | IsCompleted mapping bisa diset setelah Tahun 3 | Manual | Browser: marking completed → cek field di DB |

### Wave 0 Gaps

- Tidak ada test framework — semua verifikasi dilakukan manual via browser dan DB inspection.

---

## Sources

### Primary (HIGH confidence)

- `Controllers/CDPController.cs` — L365, L494, L707, L2177-2204, L2795-2936, L2940-3093, L3095-3180
- `Controllers/AdminController.cs` — L2515-2543 (ProtonFinalAssessment auto-create dari interview)
- `Models/ProtonModels.cs` — ProtonFinalAssessment, CoachingSession, ActionItem class definitions
- `Models/CoachCoacheeMapping.cs` — field inventory (tidak ada IsCompleted)
- `Data/ApplicationDbContext.cs` — L352-361 (ProtonFinalAssessment config — tidak ada unique index)

### Secondary (MEDIUM confidence)

- Pembacaan CONTEXT.md — keputusan locked D-01 s/d D-16

---

## Metadata

**Confidence breakdown:**
- Temuan bug (COMP-01): HIGH — langsung dari ApplicationDbContext dan controller source
- Temuan gap (COMP-02 edit/delete): HIGH — grep tidak menemukan action yang dimaksud
- Temuan legacy CoachingLog (COMP-03): HIGH — grep di HistoriProton tidak ada referensi CoachingLog
- Lifecycle field gap (COMP-04): HIGH — CoachCoacheeMapping.cs dibaca langsung, tidak ada field IsCompleted

**Research date:** 2026-03-23
**Valid until:** 2026-04-23 (stable codebase)
