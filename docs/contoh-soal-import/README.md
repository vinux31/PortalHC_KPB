# Contoh File Import Soal — Portal HC KPB

4 file `.xlsx` contoh, **langsung bisa diunggah** ke halaman Import Questions tanpa edit.
Isi soal nyata (tema K3 + operasi kilang / PROTON), bukan placeholder.

| File | Tipe | Jumlah |
|------|------|--------|
| `Contoh_Soal_SA.xlsx` | Single Answer (`MultipleChoice`) | 5 soal |
| `Contoh_Soal_MA.xlsx` | Multiple Answer (`MultipleAnswer`) | 4 soal |
| `Contoh_Soal_Essay.xlsx` | Essay (`Essay`) | 3 soal |
| `Contoh_Soal_Campuran.xlsx` | Campur 3 tipe | 6 soal (2+2+2) |

## Cara pakai

1. Buka **Admin → Manage Packages → [paket] → Import Questions**.
2. Tab **Upload Excel File** → pilih salah satu file di atas → **Import from File**.
3. Importer baca **sheet pertama**; baris 1 (header) di-skip otomatis.

> Halaman Import juga punya tombol download template bawaan (Single Answer / Multiple Answer / Essay / Universal). File ini = versi isi nyata dari template tersebut.

## Format kolom (9 kolom, urut wajib)

`Pertanyaan | Opsi A | Opsi B | Opsi C | Opsi D | Jawaban Benar | Elemen Teknis | QuestionType | Rubrik`

| Tipe | `QuestionType` | Opsi A–D | Jawaban Benar | Rubrik |
|------|----------------|----------|---------------|--------|
| **SA** | `MultipleChoice` (default jika kosong) | wajib 4-4nya | 1 huruf: `A`/`B`/`C`/`D` | — |
| **MA** | `MultipleAnswer` | wajib 4-4nya | huruf dipisah koma: `A,C` / `A,B,D` | — |
| **Essay** | `Essay` | dikosongkan | dikosongkan | **wajib** |

## Catatan importer (perilaku sistem)

- **Skor** tiap soal di-set otomatis `10`; **MaxCharacters** essay `2000` — tidak ada di Excel.
- **Dedup**: soal dengan teks + opsi sama persis (sudah ada / dobel dalam batch) di-skip.
- **Cek jumlah antar paket**: kalau paket "saudara" (judul + kategori + tanggal sama) sudah berisi soal, jumlah soal yang diimpor harus sama. Tidak berlaku untuk paket kosong/baru.
- File lama tanpa kolom `QuestionType` otomatis dibaca sebagai `MultipleChoice`.
- Format file: `.xlsx` / `.xls`, maks **5 MB**.

## Regenerate

Edit data di `_generate.py` lalu jalankan:

```powershell
python _generate.py
```

Butuh `openpyxl` (`pip install openpyxl`). Sumber kebenaran format = `Controllers/AssessmentAdminController.cs` → `DownloadQuestionTemplate` & `ImportPackageQuestions`.
