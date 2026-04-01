# Phase 279: Tambah Komponen Waktu pada ExamWindowCloseDate - Context

**Gathered:** 2026-04-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Tambahkan komponen waktu (jam:menit) pada field ExamWindowCloseDate di CreateAssessment dan EditAssessment. Saat ini field hanya menerima tanggal (type="date"), sehingga admin tidak bisa set batas waktu tutup ujian secara presisi. Field ini juga diubah dari opsional menjadi wajib.

</domain>

<decisions>
## Implementation Decisions

### Format input
- **D-01:** Gunakan 2 field terpisah (date + time) — ikuti pola Schedule yang sudah ada di CreateAssessment
- **D-02:** Field tanggal: `type="date"`, field waktu: `type="time"`
- **D-03:** Gabungkan via hidden input combiner ke `ExamWindowCloseDate`, sama seperti pola `schedHidden` untuk Schedule

### Default waktu
- **D-04:** Default value time input: `23:59`

### Validasi
- **D-05:** ExamWindowCloseDate wajib diisi (tanggal + jam) — bukan opsional lagi
- **D-06:** Validasi frontend (required) + backend guard

### Claude's Discretion
- Label teks exact ("Tanggal Tutup Ujian" / "Waktu Tutup Ujian" atau variasi lain)
- Penempatan error message / invalid-feedback
- Handling data lama yang ExamWindowCloseDate-nya null di database

</decisions>

<specifics>
## Specific Ideas

- Harus konsisten dengan pola Schedule yang sudah ada (2 field: date + time + hidden combiner)
- Default jam 23:59 agar "tanggal tutup" berarti "sampai akhir hari itu" jika admin tidak ubah

</specifics>

<canonical_refs>
## Canonical References

No external specs — requirements are fully captured in decisions above.

### Pola yang harus diikuti
- `Views/Admin/CreateAssessment.cshtml` line 329-344 — Pola Schedule: date input + time input + hidden combiner
- `Models/AssessmentSession.cs` line 59 — `ExamWindowCloseDate` property (DateTime?)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Schedule date+time combiner pattern di CreateAssessment.cshtml — bisa di-copy untuk ExamWindowCloseDate

### Established Patterns
- Hidden input combiner: 2 visible inputs (date + time) → gabung via JS → set ke hidden input yang di-bind ke model
- `ModelState.Remove("ExamWindowCloseDate")` di AdminController:1360 — perlu dihapus karena field sekarang wajib

### Integration Points
- `Views/Admin/CreateAssessment.cshtml` — tambah 2 field + hidden combiner
- `Views/Admin/EditAssessment.cshtml` — tambah 2 field + hidden combiner + populate dari existing value
- `Controllers/AdminController.cs` — hapus ModelState.Remove, tambah validasi backend
- Perbandingan ExamWindowCloseDate di CMPController, HomeController, AdminController — tidak perlu diubah (sudah DateTime comparison)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 279-tambah-komponen-waktu-jam-menit-pada-tanggal-tutup-ujian-examwindowclosedate*
*Context gathered: 2026-04-01*
