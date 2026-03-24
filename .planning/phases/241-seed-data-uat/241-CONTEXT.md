# Phase 241: Seed Data UAT - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Menyediakan seluruh data prasyarat UAT di environment Development sehingga semua fase UAT berikutnya (242-246) dapat dieksekusi tanpa setup manual. Mencakup: coach-coachee mapping, sub-kategori, assessment reguler + Proton, paket soal + 15 soal, dan completed assessment dengan sertifikat.

</domain>

<decisions>
## Implementation Decisions

### Konten Soal
- **D-01:** Soal semi-realistis — judul dan opsi terlihat nyata terkait operasi kilang/Alkylation, tapi konten tidak harus akurat secara teknis
- **D-02:** 4 Elemen Teknis dengan nama generik kilang: "Proses Distilasi", "Keselamatan Kerja", "Operasi Pompa", "Instrumentasi" (atau sejenisnya)
- **D-03:** 15 soal dengan 4 opsi masing-masing, ET di-assign merata ke soal

### Completed Assessment
- **D-04:** Seed 2 completed assessment untuk Rino: 1 lulus (skor tinggi ~80) + 1 gagal (skor rendah ~40)
- **D-05:** Keduanya lengkap dengan UserResponses (jawaban per soal) agar review jawaban dan radar chart ET bisa ditest
- **D-06:** Assessment yang lulus: sertifikat ter-generate dengan ValidUntil = 1 tahun dari tanggal completed
- **D-07:** Assessment yang gagal: tanpa sertifikat, IsPassed=false

### Seed Mechanism
- **D-08:** Extend `Data/SeedData.cs` — tambah method `SeedUatDataAsync()` dipanggil dari `InitializeAsync`, konsisten dengan pattern existing
- **D-09:** Guard `IsDevelopment()` sama seperti `CreateUsersAsync`

### Idempotency
- **D-10:** Skip jika data sudah ada (check by nama assessment) — pattern sama dengan `CreateUsersAsync` yang check `FindByEmailAsync`

### User Assignment
- **D-11:** Assessment reguler "OJT Proses Alkylation Q1-2026": peserta Rino + Iwan sesuai requirements
- **D-12:** Assessment Proton Tahun 1 & Tahun 3: hanya Rino, tidak ada user tambahan

### Tanggal & Jadwal
- **D-13:** Semua tanggal relative dari waktu startup — `CreatedAt = DateTime.UtcNow`, jadwal assessment = UtcNow + 7 hari, sehingga selalu valid kapan pun app dijalankan

### Coach-Coachee Detail
- **D-14:** Seed CoachCoacheeMapping Rustam→Rino + ProtonTrackAssignment aktif, agar Proton coaching flow langsung bisa ditest

### Claude's Discretion
- Nama spesifik soal dan opsi jawaban (selama semi-realistis kilang)
- Distribusi jawaban benar/salah pada completed assessment
- Nomor sertifikat format (mengikuti pattern KPB/SEQ/BULAN/TAHUN yang sudah ada)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Seed Data Pattern
- `Data/SeedData.cs` — Pattern existing: idempotent seed, IsDevelopment() guard, role/user creation
- `Program.cs` — Di mana SeedData.InitializeAsync dipanggil

### Data Models
- `Data/ApplicationDbContext.cs` — DbSet definitions untuk semua entity yang perlu di-seed
- `Models/` — Entity classes untuk Assessment, Question, UserResponse, Certificate, CoachCoacheeMapping, ProtonTrackAssignment

### Requirements
- `.planning/REQUIREMENTS.md` §Seed Data — SEED-01 s/d SEED-07 detail requirements

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SeedData.cs` — Pattern idempotent seeding sudah established, tinggal extend
- `CreateUsersAsync` — 10 sample users sudah di-seed termasuk Rustam (Coach), Rino (Coachee), Iwan (Coachee)
- `SeedOrganizationUnitsAsync` — Pattern check `AnyAsync()` sebelum seed

### Established Patterns
- Idempotent seeding: check exists → skip
- Development-only guard: `environment.IsDevelopment()`
- Console.WriteLine untuk logging seed results

### Integration Points
- `InitializeAsync` — Entry point untuk semua seed, dipanggil dari Program.cs
- Assessment entities: Assessment, AssessmentAssignment, QuestionPackage, Question, QuestionOption, UserResponse, AssessmentResult, Certificate
- Proton entities: CoachCoacheeMapping, ProtonTrackAssignment

</code_context>

<specifics>
## Specific Ideas

- Completed assessment yang lulus harus bisa digunakan untuk test analytics dashboard, records view, dan renewal flow
- Completed assessment yang gagal untuk test flow gagal dan review jawaban salah
- Coach-coachee mapping + ProtonTrackAssignment agar coaching Proton langsung testable

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 241-seed-data-uat*
*Context gathered: 2026-03-24*
