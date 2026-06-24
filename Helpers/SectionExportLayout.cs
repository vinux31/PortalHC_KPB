namespace HcPortal.Helpers;

// Phase 419 PAG-04 (code-review 419 fix #5) — single source of truth untuk URUTAN & LABEL Section di
// SEMUA jalur export: PDF per-peserta (GeneratePerPesertaPdf), Excel sheet agregat "Detail Per Soal"
// (ExcelExportHelper.AddDetailPerSoalSheet) dan Excel per-peserta "Detail Jawaban". Sebelumnya logika
// "OrderBy SectionNumber ?? int.MaxValue + label 'Section {n}: {Nama}' / 'Lainnya'" diduplikat di tiga
// tempat (risiko drift saat format berubah).
//
// CATATAN penting: BERBEDA dari SectionStructureComparer. Comparer dipakai untuk PERBANDINGAN struktur
// dan memetakan "Lainnya" ke int.MinValue (LainnyaKey) — di export grup "Lainnya" justru tampil
// TERAKHIR, jadi kunci urut di sini = int.MaxValue. Jangan campur kedua sentinel.
public static class SectionExportLayout
{
    // Kunci urut tampil: Section tanpa nomor ("Lainnya") selalu terakhir.
    public const int LainnyaOrderKey = int.MaxValue;

    public static int OrderKey(int? sectionNumber) => sectionNumber ?? LainnyaOrderKey;

    // Label band/heading: "Section {n}: {Nama}" (Nama opsional — boleh kosong, §5.1) atau "Lainnya" saat
    // tak ber-Section. Pemanggil memilih Nama secara deterministik (grup soal urut (Order, Id), elemen pertama)
    // sehingga sibling ber-SectionNumber sama tapi Nama beda (langka; idealnya disinkronkan SEC-06) menghasilkan
    // label yang stabil dan tunggal — bukan acak (code-review 419 fix #2).
    public static string Label(int? sectionNumber, string? name)
        => sectionNumber.HasValue ? $"Section {sectionNumber}: {name}" : "Lainnya";
}
