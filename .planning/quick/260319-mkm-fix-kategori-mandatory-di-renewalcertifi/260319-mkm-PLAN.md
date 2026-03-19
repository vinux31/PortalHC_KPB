---
phase: quick-260319-mkm
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Controllers/AdminController.cs
  - Controllers/CDPController.cs
autonomous: true
requirements:
  - MKM-fix-kategori-mandatory
must_haves:
  truths:
    - "Filter 'Mandatory HSSE Training' menampilkan baris yang sebelumnya tersimpan sebagai 'MANDATORY'"
    - "Filter 'Assessment Proton' menampilkan baris yang tersimpan sebagai 'PROTON'"
    - "Filter 'OJT' tetap berjalan normal"
  artifacts:
    - path: "Controllers/AdminController.cs"
      provides: "BuildRenewalRowsAsync dengan mapping kategori"
      contains: "Mandatory HSSE Training"
    - path: "Controllers/CDPController.cs"
      provides: "BuildSertifikatRowsAsync dengan mapping kategori"
      contains: "Mandatory HSSE Training"
  key_links:
    - from: "TrainingRecord.Kategori (nilai lama: MANDATORY)"
      to: "RenewalRow.Kategori (nilai tampil: Mandatory HSSE Training)"
      via: "dictionary mapping di BuildRenewalRowsAsync (AdminController:6664)"
    - from: "TrainingRecord.Kategori (nilai lama: MANDATORY)"
      to: "SertifikatRow.Kategori (nilai tampil: Mandatory HSSE Training)"
      via: "dictionary mapping di BuildSertifikatRowsAsync (CDPController:3298)"
---

<objective>
Perbaiki mapping TrainingRecord.Kategori (nilai legacy: "MANDATORY", "PROTON", "OJT") ke nama
display yang sesuai AssessmentCategories ("Mandatory HSSE Training", "Assessment Proton", "OJT")
di dua method builder — BuildRenewalRowsAsync dan BuildSertifikatRowsAsync.

Purpose: Dropdown filter RenewalCertificate memuat dari AssessmentCategories sehingga nilai
"MANDATORY" tidak pernah cocok dengan "Mandatory HSSE Training", menyebabkan filter tidak berfungsi.

Output: Kedua method builder menghasilkan Kategori dengan nama yang sesuai dropdown filter.
</objective>

<execution_context>
@C:/Users/Administrator/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/Administrator/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Tambah mapping kategori di BuildRenewalRowsAsync (AdminController)</name>
  <files>Controllers/AdminController.cs</files>
  <action>
Di AdminController.cs, cari method BuildRenewalRowsAsync. Temukan baris sekitar line 6664
di mana `Kategori = t.Kategori` di-assign ke row object.

Sebelum atau di dalam select/projection, terapkan mapping menggunakan static dictionary atau
inline expression. Tambahkan private static Dictionary atau helper expression di dalam method
(atau sebagai private static field di class):

```csharp
private static string MapKategori(string raw) => raw switch
{
    "MANDATORY" => "Mandatory HSSE Training",
    "PROTON"    => "Assessment Proton",
    _           => raw   // "OJT" dan lainnya tetap
};
```

Lalu ubah assignment dari:
```csharp
Kategori = t.Kategori,
```
menjadi:
```csharp
Kategori = MapKategori(t.Kategori),
```

Pastikan method MapKategori ditempatkan di scope yang dapat diakses (private static di class
AdminController, bukan di dalam lambda). Jika sudah ada helper serupa di codebase, gunakan
pola yang sama.
  </action>
  <verify>
    <automated>grep -n "Mandatory HSSE Training" "Controllers/AdminController.cs"</automated>
  </verify>
  <done>AdminController.cs mengandung mapping "MANDATORY" → "Mandatory HSSE Training" yang dipanggil di BuildRenewalRowsAsync</done>
</task>

<task type="auto">
  <name>Task 2: Terapkan mapping yang sama di BuildSertifikatRowsAsync (CDPController)</name>
  <files>Controllers/CDPController.cs</files>
  <action>
Di CDPController.cs, cari method BuildSertifikatRowsAsync. Temukan baris sekitar line 3298
di mana `Kategori = t.Kategori` di-assign.

Cek apakah CDPController sudah memiliki MapKategori helper atau helper serupa. Jika belum,
tambahkan private static method yang identik dengan yang dibuat di Task 1:

```csharp
private static string MapKategori(string raw) => raw switch
{
    "MANDATORY" => "Mandatory HSSE Training",
    "PROTON"    => "Assessment Proton",
    _           => raw
};
```

Ubah assignment dari:
```csharp
Kategori = t.Kategori,
```
menjadi:
```csharp
Kategori = MapKategori(t.Kategori),
```

Catatan: Jika di Phase 190, CDPController sudah melakukan mapping kategori via AssessmentCategories
(bukan langsung t.Kategori), verifikasi terlebih dahulu — mungkin masalahnya berbeda. Pastikan
perubahan konsisten dengan pola yang ada.
  </action>
  <verify>
    <automated>grep -n "Mandatory HSSE Training" "Controllers/CDPController.cs"</automated>
  </verify>
  <done>CDPController.cs mengandung mapping "MANDATORY" → "Mandatory HSSE Training" yang dipanggil di BuildSertifikatRowsAsync</done>
</task>

</tasks>

<verification>
1. Build project: `dotnet build` — tidak ada compile error
2. Jalankan aplikasi, buka halaman RenewalCertificate di Kelola Data
3. Filter by "Mandatory HSSE Training" — baris yang sebelumnya bernilai "MANDATORY" harus muncul
4. Filter by "Assessment Proton" — baris "PROTON" harus muncul
5. Filter by "OJT" — tetap berjalan normal
</verification>

<success_criteria>
- Filter dropdown "Mandatory HSSE Training" menampilkan baris training Mandatory
- Filter dropdown "Assessment Proton" menampilkan baris training Proton
- Tidak ada compile error setelah perubahan
- Halaman Sertifikat di CDP (BuildSertifikatRowsAsync) juga menampilkan kategori yang benar
</success_criteria>

<output>
Setelah selesai, buat `.planning/quick/260319-mkm-fix-kategori-mandatory-di-renewalcertifi/260319-mkm-SUMMARY.md`
</output>
