using System.Linq;
using ClosedXML.Excel;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Phase 999.17 (DRP-01/02, SKR-01) — pure/EF-free builder template soal Excel.
    /// Diekstrak dari <c>AssessmentAdminController.DownloadQuestionTemplate</c> agar DataValidation
    /// (greenfield ClosedXML 0.105.0) unit-testable TANPA membangun controller (~12 ctor arg) & tanpa DB.
    /// Analog bentuk: <see cref="InjectExcelHelper.GenerateTemplate"/> (static, namespace HcPortal.Helpers,
    /// kembalikan <see cref="XLWorkbook"/> hidup; streaming tetap di controller via
    /// <see cref="ExcelExportHelper.ToFileResult"/>).
    ///
    /// Header Universal 14-kolom (Pertanyaan | A-F | Jawaban Benar(8/H) | No.Section(9) | Nama Section(10) |
    /// ET(11) | QuestionType(12/L) | Rubrik(13/M) | Skor(14/N)). Legacy 9-kolom (QuestionType=8/H) TANPA Skor
    /// (kompatibel-mundur D-07). DataValidation: dropdown QuestionType di SEMUA varian (D-01/02/03), numeric
    /// Skor 1-100 HANYA Universal (D-10); kolom lain TANPA dropdown (D-04/05).
    /// </summary>
    public static class QuestionTemplateBuilder
    {
        public static XLWorkbook Build(string type)
        {
            // Normalize type — whitelist only
            var validTypes = new[] { "MC", "MA", "Essay", "Universal" };
            if (!validTypes.Contains(type)) type = "MC";

            var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Question Import");

            // Phase 415 IMP-01 (D-415-03): "Universal" memakai header diperluas 13-kolom
            // (Opsi A–F + No. Section + Nama Section). Tipe legacy (MC/MA/Essay) tetap 9-kolom
            // (kompatibel-mundur — import dual-format mendeteksi ≤9 = lama / >9 = baru dari header).
            bool universal = type == "Universal";
            var headers = universal
                ? new[] { "Pertanyaan", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Opsi E", "Opsi F", "Jawaban Benar", "No. Section", "Nama Section", "Elemen Teknis", "QuestionType", "Rubrik" }
                : new[] { "Pertanyaan", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Jawaban Benar", "Elemen Teknis", "QuestionType", "Rubrik" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            int nextRow = 2;

            void AddExampleRow(int row, string[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ws.Cell(row, i + 1).Value = values[i];
                    ws.Cell(row, i + 1).Style.Font.Italic = true;
                    ws.Cell(row, i + 1).Style.Font.FontColor = XLColor.Gray;
                }
            }

            if (universal)
            {
                // 13-kolom: Pertanyaan | A-F(2-7) | Jawaban Benar(8) | No.Section(9) | Nama Section(10) | ET(11) | Type(12) | Rubrik(13)
                // Contoh MC dgn Section 1, contoh MA dgn 6 opsi (A,C,E) Section 1, contoh Essay tanpa Section (Lainnya).
                AddExampleRow(nextRow++, new[] { "Contoh soal MC bagian Pompa?", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "", "", "B", "1", "Pompa & Kompresor", "K3 Dasar", "MultipleChoice", "" });
                AddExampleRow(nextRow++, new[] { "Contoh soal MA 6 opsi?", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Opsi E", "Opsi F", "A,C,E", "1", "Pompa & Kompresor", "K3 Dasar", "MultipleAnswer", "" });
                AddExampleRow(nextRow++, new[] { "Contoh soal Essay tanpa Section?", "", "", "", "", "", "", "", "", "", "K3 Dasar", "Essay", "Rubrik: Jawaban harus mencakup..." });
            }
            else
            {
                // 9-kolom legacy: Pertanyaan | A-D(2-5) | Jawaban Benar(6) | ET(7) | Type(8) | Rubrik(9)
                var mcExample  = new[] { "Contoh soal MC?", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "A", "K3 Dasar", "MultipleChoice", "" };
                var maExample  = new[] { "Contoh soal MA?", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "A,C", "K3 Dasar", "MultipleAnswer", "" };
                var essayExample = new[] { "Contoh soal Essay?", "", "", "", "", "", "K3 Dasar", "Essay", "Rubrik: Jawaban harus mencakup..." };

                if (type == "MC") AddExampleRow(nextRow++, mcExample);
                else if (type == "MA") AddExampleRow(nextRow++, maExample);
                else AddExampleRow(nextRow++, essayExample); // Essay
            }

            // Instruction rows
            void AddInstruction(int row, string text)
            {
                ws.Cell(row, 1).Value = text;
                ws.Cell(row, 1).Style.Font.Italic = true;
                ws.Cell(row, 1).Style.Font.FontColor = XLColor.DarkRed;
            }

            AddInstruction(nextRow++, "QuestionType: MultipleChoice (default jika kosong), MultipleAnswer, atau Essay");
            if (universal)
            {
                AddInstruction(nextRow++, "Jawaban Benar: huruf A-F. MA dipisah koma, contoh: A,C atau A,C,E");
                AddInstruction(nextRow++, "Opsi E/F: opsional (2-6 opsi). Kosongkan jika tidak dipakai.");
                AddInstruction(nextRow++, "No. Section: angka (1, 2, 3...). Kosongkan = soal tanpa Section (Lainnya). Section dibuat otomatis dari kolom ini.");
                AddInstruction(nextRow++, "Nama Section: opsional, label tampilan Section.");
            }
            else
            {
                AddInstruction(nextRow++, "Jawaban Benar MA: isi huruf dipisah koma, contoh: A,C atau A,B,D");
            }
            AddInstruction(nextRow++, "Essay: Opsi dan Jawaban Benar dikosongkan. Rubrik wajib diisi");
            AddInstruction(nextRow++, "Kolom Elemen Teknis: opsional, isi nama elemen teknis. Kosongkan jika tidak ada.");

            return workbook;
        }
    }
}
