# Phase 277: Delete Peserta Assessment di EditAssessment - Research

**Researched:** 2026-04-01
**Domain:** ASP.NET Core MVC — AdminController delete action + EditAssessment view
**Confidence:** HIGH

## Summary

Phase ini menambahkan aksi `DeleteAssessmentPeserta` (POST) di AdminController dan memperluas tabel Currently Assigned Users di EditAssessment.cshtml dengan dua kolom baru: "Status Assessment" dan "Actions" (tombol hapus per-baris). Guard logic berbasis field `StartedAt`, `CompletedAt`, dan `Status == "Completed"` menentukan apakah baris eligible untuk dihapus.

Semua pola implementasi sudah ada di codebase: pola delete cascade dari `DeleteAssessment` (lines 2067–2159), pola redirect sibling dari `GET EditAssessment` (lines 1741–1812), dan pola UX confirm() dari `DeleteAssessment`/`DeleteAssessmentGroup`. Tidak ada library atau pola baru yang perlu diperkenalkan.

**Primary recommendation:** Buat action POST `DeleteAssessmentPeserta(int sessionId, int returnToId)` yang mengikuti urutan delete dari `DeleteAssessment` secara verbatim, lalu perbarui `ViewBag.AssignedUsers` di GET EditAssessment untuk menyertakan field `Status` dan `CanDelete`.

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- **D-01:** CanDelete = false jika `StartedAt != null` ATAU `CompletedAt != null` ATAU `Status == "Completed"`.
- **D-02:** Hard delete satu AssessmentSession + semua data turunan (PackageUserResponses, AssessmentAttemptHistory, AssessmentPackages + nested, UserPackageAssignments via cascade).
- **D-03:** Konfirmasi pakai browser `confirm()` sederhana.
- **D-04:** Delete satu-per-satu saja (per row). Tidak ada bulk delete.
- **D-05:** Jika session yang dihapus bukan session yang sedang dibuka → redirect ke EditAssessment session yang sedang dibuka.
- **D-06:** Jika session yang dihapus adalah session yang sedang dibuka → cari sibling lain, redirect ke sibling tersebut.
- **D-07:** Jika peserta terakhir di grup dihapus → redirect ke ManageAssessment dengan success message.
- **D-08:** Tambah kolom "Status Assessment" dan kolom "Actions" pada tabel Currently Assigned Users.
- **D-09:** Badge status: Open (hijau), Upcoming (biru), Completed (abu). Fallback badge netral untuk status lain.
- **D-10:** Row yang tidak eligible delete → tombol disabled.

### Claude's Discretion
- Warna badge exact dan styling tombol delete
- Pesan error exact saat delete diblok
- Audit log format detail

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | (project existing) | Controller action + view | Framework proyek |
| Entity Framework Core | (project existing) | Query + delete DB entities | ORM proyek |
| Bootstrap 5 | (project existing) | Badge, button disabled state | CSS framework proyek |

### Tidak Ada Dependensi Baru
Seluruh implementasi menggunakan stack yang sudah ada. Tidak perlu install package.

---

## Architecture Patterns

### Pattern 1: Controller Action DELETE Peserta

```csharp
// Ikuti pola DeleteAssessment (AdminController.cs:2067)
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteAssessmentPeserta(int sessionId, int returnToId)
{
    // 1. Load session yang akan dihapus
    // 2. Guard: cek CanDelete (StartedAt == null && CompletedAt == null && Status != "Completed")
    // 3. Jika tidak eligible → TempData["Error"] + redirect kembali
    // 4. Urutan delete (SAMA PERSIS dengan DeleteAssessment):
    //    a. PackageUserResponses WHERE AssessmentSessionId == sessionId
    //    b. AssessmentAttemptHistory WHERE SessionId == sessionId
    //    c. AssessmentPackages (include Questions → Options) WHERE AssessmentSessionId == sessionId
    //       - RemoveRange Options, Questions, Packages
    //    d. AssessmentSessions.Remove(session)
    //    e. SaveChangesAsync()
    // 5. Audit log (try/catch terpisah)
    // 6. Redirect logic (D-05 / D-06 / D-07)
}
```

### Pattern 2: Redirect Logic Setelah Delete

```csharp
// Setelah SaveChangesAsync berhasil:

// Kasus D-07: peserta terakhir di grup
var remainingSiblings = await _context.AssessmentSessions
    .Where(a => a.Title == title && a.Category == category && a.Schedule.Date == scheduleDate)
    .ToListAsync();

if (!remainingSiblings.Any())
{
    TempData["Success"] = $"Peserta '{userName}' telah dihapus. Tidak ada peserta tersisa.";
    return RedirectToAction("ManageAssessment");
}

// Kasus D-05: session yang dihapus BUKAN yang sedang dibuka
if (sessionId != returnToId)
{
    TempData["Success"] = $"Peserta '{userName}' berhasil dihapus.";
    return RedirectToAction("EditAssessment", new { id = returnToId });
}

// Kasus D-06: session yang dihapus ADALAH yang sedang dibuka → cari sibling
var sibling = remainingSiblings.First();
TempData["Success"] = $"Peserta '{userName}' berhasil dihapus.";
return RedirectToAction("EditAssessment", new { id = sibling.Id });
```

### Pattern 3: ViewBag.AssignedUsers — Perluasan Anonymous Object

Di GET EditAssessment (line ~1768), ubah anonymous object yang di-select agar menyertakan field baru:

```csharp
ViewBag.AssignedUsers = siblings
    .Where(a => a.User != null)
    .Select(a => new
    {
        Id = a.Id,
        FullName = a.User!.FullName ?? "",
        Email = a.User!.Email ?? "",
        Section = a.User!.Section ?? "",
        Status = a.Status,
        CanDelete = a.StartedAt == null && a.CompletedAt == null && a.Status != "Completed"
    })
    .ToList();
```

### Pattern 4: Tabel View — Kolom Baru + Form POST Per-Baris

```html
<!-- Thead: tambah 2 kolom setelah Section -->
<th>Status Assessment</th>
<th>Actions</th>

<!-- Tbody: per baris -->
<td>
    @{
        var badgeClass = au.Status switch {
            "Open" => "bg-success",
            "Upcoming" => "bg-primary",
            "Completed" => "bg-secondary",
            _ => "bg-light text-dark"
        };
    }
    <span class="badge @badgeClass">@au.Status</span>
</td>
<td>
    @if (au.CanDelete)
    {
        <form method="post" asp-action="DeleteAssessmentPeserta" style="display:inline;"
              onsubmit="return confirm('Hapus peserta @au.FullName dari assessment ini?')">
            @Html.AntiForgeryToken()
            <input type="hidden" name="sessionId" value="@au.Id" />
            <input type="hidden" name="returnToId" value="@Model.Id" />
            <button type="submit" class="btn btn-sm btn-outline-danger">
                <i class="bi bi-trash"></i> Hapus
            </button>
        </form>
    }
    else
    {
        <button type="button" class="btn btn-sm btn-outline-secondary" disabled title="Tidak dapat dihapus: ujian sudah dimulai atau selesai">
            <i class="bi bi-trash"></i> Hapus
        </button>
    }
</td>
```

### Anti-Patterns to Avoid
- **Jangan pakai AJAX untuk delete ini** — `confirm()` sederhana + full POST/redirect sudah disepakati (D-03), konsisten dengan DeleteAssessment existing.
- **Jangan ubah urutan delete** — urutan di DeleteAssessment sudah proven safe (Restrict FK harus dihapus dulu sebelum session).
- **Jangan lupa `returnToId` hidden field** — tanpa ini, redirect D-05/D-06 tidak bisa dibedakan.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| Cascade delete order | Custom SQL atau logika baru | Urutan exact dari `DeleteAssessment` lines 2086–2127 |
| Sibling query | Query ulang dari scratch | Pattern `same Title + Category + Schedule.Date` dari GET EditAssessment line 1754 |
| Audit log | Custom audit implementation | Pattern `_auditLog.LogAsync(...)` dari DeleteAssessment lines 2132–2147 |
| Flash messages | Custom notification | `TempData["Success"]` / `TempData["Error"]` pattern existing |

---

## Common Pitfalls

### Pitfall 1: Guard di View Saja Tanpa Guard di Controller
**What goes wrong:** Tombol disabled di view, tapi tidak ada validasi di action POST — bisa di-bypass via direct POST.
**How to avoid:** Guard logic `CanDelete` harus ada DI CONTROLLER, bukan hanya di view. Jika `StartedAt != null || CompletedAt != null || Status == "Completed"` → kembalikan error.

### Pitfall 2: Lupa Load Session Ulang untuk Redirect Logic
**What goes wrong:** Redirect D-06 (session yang dihapus adalah yang sedang dibuka) memerlukan query sibling yang masih ada SETELAH delete. Jika query dilakukan sebelum `SaveChangesAsync`, session yang baru dihapus masih ada di hasil query.
**How to avoid:** Query remaining siblings SETELAH `SaveChangesAsync`.

### Pitfall 3: returnToId Tidak Dikirim dari Form
**What goes wrong:** Action menerima `returnToId = 0` (default), redirect D-05 salah arah.
**How to avoid:** Pastikan `<input type="hidden" name="returnToId" value="@Model.Id" />` ada di setiap form delete baris.

### Pitfall 4: Tabel Overflow Saat Kolom Bertambah
**What goes wrong:** Tabel sudah punya 4 kolom (#, Name, Email, Section) + max-height 250px. Tambah 2 kolom bisa menyebabkan layout squeeze.
**How to avoid:** Pertimbangkan `table-responsive` wrapper atau persempit kolom # dan Section. Badge status cukup ringkas.

---

## Code Examples

### AssessmentSession Model Fields (Verified)
```csharp
// Models/AssessmentSession.cs
public string Status { get; set; } = "";   // "Open", "Upcoming", "Completed"
public DateTime? CompletedAt { get; set; }
public DateTime? StartedAt { get; set; }
```

### Existing Delete Cascade Order (dari DeleteAssessment)
```
1. PackageUserResponses (Restrict FK — WAJIB dihapus dulu)
2. AssessmentAttemptHistory (orphan cleanup)
3. PackageOptions (nested dalam Questions)
4. PackageQuestions (nested dalam Packages)
5. AssessmentPackages
6. AssessmentSessions (session itu sendiri)
// UserPackageAssignments: cascade otomatis via DB FK
```

---

## Environment Availability

Step 2.6: SKIPPED (phase ini murni perubahan kode/controller/view, tidak ada external dependency baru).

---

## Validation Architecture

Step 4: SKIPPED — tidak ada test framework yang terdeteksi di proyek ini, dan fase ini adalah ad-hoc enhancement. Verifikasi dilakukan manual oleh user di browser sesuai pola proyek (lihat STATE.md).

---

## Open Questions

1. **Tabel max-height 250px dengan 6 kolom**
   - What we know: Tabel existing sudah 4 kolom dalam 250px scroll area.
   - What's unclear: Apakah perlu responsive wrapper atau column width adjustment.
   - Recommendation: Claude tentukan layout (Claude's Discretion per CONTEXT.md).

---

## Sources

### Primary (HIGH confidence)
- `Controllers/AdminController.cs` lines 1741–1812 — GET EditAssessment: sibling query + ViewBag.AssignedUsers pattern
- `Controllers/AdminController.cs` lines 2067–2159 — DeleteAssessment: cascade delete order (proven safe)
- `Controllers/AdminController.cs` lines 2161–2220 — DeleteAssessmentGroup: group delete pattern
- `Views/Admin/EditAssessment.cshtml` lines 326–370 — Tabel Currently Assigned Users existing
- `Models/AssessmentSession.cs` — Status field values ("Open", "Upcoming", "Completed") + StartedAt + CompletedAt

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua existing, tidak ada library baru
- Architecture: HIGH — pola delete dan redirect sudah proven di codebase
- Pitfalls: HIGH — bersumber dari kode existing dan pola delete yang sudah diketahui

**Research date:** 2026-04-01
**Valid until:** Stabil — tidak ada dependency eksternal yang berubah
