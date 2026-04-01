# Phase 279: Tambah Komponen Waktu pada ExamWindowCloseDate - Research

**Researched:** 2026-04-01
**Domain:** ASP.NET MVC form input (date+time combiner pattern)
**Confidence:** HIGH

## Summary

Phase ini menambahkan komponen waktu (jam:menit) pada field ExamWindowCloseDate di CreateAssessment dan EditAssessment. Saat ini field hanya `type="date"`, perlu diubah menjadi 2 field (date + time) dengan hidden combiner, mengikuti pola Schedule yang sudah ada di codebase.

Pola sudah terbukti di codebase: Schedule menggunakan 2 visible input (date + time) yang digabungkan via JavaScript ke hidden input sebelum form submit. Implementasi tinggal menduplikasi pola ini untuk ExamWindowCloseDate. Field juga berubah dari opsional menjadi wajib.

**Primary recommendation:** Copy pola Schedule combiner (date + time + hidden) untuk ExamWindowCloseDate di kedua view, hapus ModelState.Remove, tambah validasi frontend required.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: Gunakan 2 field terpisah (date + time) -- ikuti pola Schedule yang sudah ada di CreateAssessment
- D-02: Field tanggal: type="date", field waktu: type="time"
- D-03: Gabungkan via hidden input combiner ke ExamWindowCloseDate, sama seperti pola schedHidden untuk Schedule
- D-04: Default value time input: 23:59
- D-05: ExamWindowCloseDate wajib diisi (tanggal + jam) -- bukan opsional lagi
- D-06: Validasi frontend (required) + backend guard

### Claude's Discretion
- Label teks exact ("Tanggal Tutup Ujian" / "Waktu Tutup Ujian" atau variasi lain)
- Penempatan error message / invalid-feedback
- Handling data lama yang ExamWindowCloseDate-nya null di database

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

## Project Constraints (from CLAUDE.md)

- Selalu respond dalam Bahasa Indonesia

## Architecture Patterns

### Pola Schedule Combiner (Existing - HARUS Diikuti)

**CreateAssessment.cshtml (line 329-344):**
```html
<!-- Date input -->
<input type="date" id="schedDateInput" name="ScheduleDate" class="form-control" />

<!-- Time input -->
<input type="time" id="schedTimeInput" name="ScheduleTime" class="form-control" value="08:00" />

<!-- Hidden combiner -->
<input type="hidden" asp-for="Schedule" id="schedHidden" />
```

**JavaScript combiner (sebelum form submit):**
```javascript
schedHidden.value = schedDateInput.value + 'T' + (schedTimeInput.value || '08:00') + ':00';
```

**EditAssessment.cshtml (line 212-225) - Populate dari model:**
```html
<input type="date" id="ScheduleDate" value="@Model.Schedule.ToString("yyyy-MM-dd")" class="form-control" required />
<input type="time" id="ScheduleTime" value="@Model.Schedule.ToString("HH:mm")" class="form-control" required />
<input asp-for="Schedule" type="hidden" id="ScheduleHidden" value="@Model.Schedule.ToString("yyyy-MM-ddTHH:mm:ss")" />
```

### Pola yang Harus Diterapkan untuk ExamWindowCloseDate

**CreateAssessment:** Ganti single `<input type="date" asp-for="ExamWindowCloseDate">` (line 403) dengan:
- `<input type="date" id="ewcdDateInput" required />`
- `<input type="time" id="ewcdTimeInput" value="23:59" required />`
- `<input type="hidden" asp-for="ExamWindowCloseDate" id="ewcdHidden" />`

**EditAssessment:** Ganti single `<input type="date" asp-for="ExamWindowCloseDate">` (line 284) dengan:
- Date input populated via `@Model.ExamWindowCloseDate?.ToString("yyyy-MM-dd")`
- Time input populated via `@Model.ExamWindowCloseDate?.ToString("HH:mm")` atau default "23:59"
- Hidden combiner

### File yang Harus Diubah

| File | Perubahan |
|------|-----------|
| `Views/Admin/CreateAssessment.cshtml` line ~398-405 | Ganti date input menjadi date+time+hidden combiner |
| `Views/Admin/EditAssessment.cshtml` line ~280-288 | Ganti date input menjadi date+time+hidden combiner + populate |
| `Controllers/AdminController.cs` line 1360-1361 | Hapus `ModelState.Remove("ExamWindowCloseDate")` |

### JavaScript Combiner Locations

**CreateAssessment:** Tambah combiner di form submit handler (line ~1133-1140, setelah schedule combiner)

**EditAssessment:** Tambah combiner di kedua form submit handlers (line ~514-524 dan ~565-574)

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| Date+time combining | Custom parsing logic | Pola `dateValue + 'T' + timeValue + ':00'` yang sudah ada |
| Frontend validation | Manual if/else | HTML5 `required` attribute + `is-invalid` class pattern |

## Common Pitfalls

### Pitfall 1: Lupa combiner di EditAssessment
**What goes wrong:** EditAssessment punya 2 form submit handlers (line 514 dan 565). Harus tambah combiner di KEDUA handler.
**How to avoid:** Grep `schedHidden` di EditAssessment dan tambah ewcdHidden di setiap lokasi yang sama.

### Pitfall 2: Data lama null di EditAssessment
**What goes wrong:** Assessment lama yang ExamWindowCloseDate-nya null akan crash saat `.ToString("yyyy-MM-dd")`.
**How to avoid:** Gunakan null-conditional: `Model.ExamWindowCloseDate?.ToString("yyyy-MM-dd")`. Untuk default time, gunakan `Model.ExamWindowCloseDate?.ToString("HH:mm") ?? "23:59"`.

### Pitfall 3: CreateAssessment step validation
**What goes wrong:** CreateAssessment punya multi-step wizard. ExamWindowCloseDate ada di Step 3. Validasi step 3 (line ~757) perlu ditambah check untuk ewcdDateInput dan ewcdTimeInput.
**How to avoid:** Tambah validasi di block `if (n === 3)` mengikuti pola schedDate/schedTime.

### Pitfall 4: Summary panel di CreateAssessment
**What goes wrong:** CreateAssessment punya summary panel (Step 4) yang menampilkan nilai. ExamWindowCloseDate mungkin perlu ditampilkan di summary.
**How to avoid:** Check apakah ada summary entry untuk ExamWindowCloseDate, tambah jika perlu.

## Code Examples

### Combiner JavaScript (untuk ditambah di form submit)
```javascript
// Combine ExamWindowCloseDate date + time
var ewcdDateInput = document.getElementById('ewcdDateInput');
var ewcdTimeInput = document.getElementById('ewcdTimeInput');
var ewcdHidden = document.getElementById('ewcdHidden');
if (ewcdDateInput && ewcdDateInput.value && ewcdTimeInput && ewcdHidden) {
    ewcdHidden.value = ewcdDateInput.value + 'T' + (ewcdTimeInput.value || '23:59') + ':00';
}
```

### EditAssessment populate (Razor)
```html
<input type="date" id="ewcdDateInput"
       value="@Model.ExamWindowCloseDate?.ToString("yyyy-MM-dd")"
       class="form-control" required />
<input type="time" id="ewcdTimeInput"
       value="@(Model.ExamWindowCloseDate?.ToString("HH:mm") ?? "23:59")"
       class="form-control" required />
<input asp-for="ExamWindowCloseDate" type="hidden" id="ewcdHidden"
       value="@Model.ExamWindowCloseDate?.ToString("yyyy-MM-ddTHH:mm:ss")" />
```

## Open Questions

1. **Data lama dengan ExamWindowCloseDate null**
   - What we know: Model property adalah `DateTime?` (nullable). Assessment lama mungkin null.
   - Recommendation: Biarkan nullable di model. Di EditAssessment, jika null, tampilkan field kosong dengan default time 23:59. Backend validation: jika field kosong saat POST, reject (karena sekarang wajib). Untuk assessment lama yang sudah jalan, tidak perlu migrasi data.

## Sources

### Primary (HIGH confidence)
- `Views/Admin/CreateAssessment.cshtml` line 329-344, 757-771, 1133-1140 — Schedule combiner pattern
- `Views/Admin/EditAssessment.cshtml` line 209-225, 514-574 — EditAssessment Schedule pattern
- `Controllers/AdminController.cs` line 1360-1361 — ModelState.Remove
- `Models/AssessmentSession.cs` line 59 — Property definition

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - pola sudah ada di codebase, tinggal duplikasi
- Architecture: HIGH - semua file dan line number terverifikasi
- Pitfalls: HIGH - berdasarkan analisis kode aktual

**Research date:** 2026-04-01
**Valid until:** 2026-05-01 (stable codebase pattern)
