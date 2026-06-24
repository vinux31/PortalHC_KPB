using ClosedXML.Excel;
using HcPortal.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace HcPortal.Helpers
{
    public static class ExcelExportHelper
    {
        /// <summary>
        /// Creates a worksheet with bold headers in row 1.
        /// Caller populates data starting at row 2.
        /// </summary>
        public static IXLWorksheet CreateSheet(XLWorkbook workbook, string sheetName, string[] headers)
        {
            var ws = workbook.Worksheets.Add(sheetName);
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
            }
            ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
            return ws;
        }

        /// <summary>
        /// Adjusts all worksheet columns, saves workbook to byte array, and returns FileContentResult.
        /// </summary>
        public static FileContentResult ToFileResult(XLWorkbook workbook, string fileName, ControllerBase controller)
        {
            foreach (var ws in workbook.Worksheets)
                ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return controller.File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// Phase 338 CIL-05 (D-03 AMENDED): Sheet "Detail Per Soal" — grid per-peserta-per-soal.
        /// Header dinamis berdasarkan PackageQuestion list (urut by Order). 1 row per peserta.
        /// Phase 386 PXF-05 (F-DEV-02 D-13): Jawaban + Benar? di-derive lewat helper bersama
        /// AssessmentScoreAggregator.BuildAnswerCell (MC single / MA join ", " SEMUA opsi terpilih / Essay truncate / "—")
        /// dan IsQuestionCorrect (MC IsCorrect; MA all-or-nothing SetEquals; Essay > 0; pending = null) —
        /// SAMA dengan PDF GeneratePerPesertaPdf + web Results (kill-drift). Catatan: label Essay kini > 0
        /// (unifikasi v30.0 canonical) menggantikan EssayScore >= ScoreValue/2 yang lama (intentional, D-13).
        /// </summary>
        public static void AddDetailPerSoalSheet(
            XLWorkbook workbook,
            List<AssessmentSession> sessions,
            List<PackageUserResponse> responses,
            List<PackageQuestion> questions)
        {
            var ws = workbook.Worksheets.Add("Detail Per Soal");
            // Order field = stable sort key (AssessmentPackage.cs L38 comment)
            // Phase 419 PAG-04: urutan kolom soal Section-aware (canonical, mirror ShuffleEngine/SectionPaginator 416/417).
            // Soal tanpa Section (SectionNumber null) -> grup "Lainnya" terakhir (SectionExportLayout.OrderKey = int.MaxValue).
            var sortedQuestions = questions
                .OrderBy(q => SectionExportLayout.OrderKey(q.Section?.SectionNumber))
                .ThenBy(q => q.Order)
                .ThenBy(q => q.Id)
                .ToList();

            // Phase 419 PAG-04: band-header Section hanya saat ada >=1 Section. Tanpa Section -> output legacy identik
            // (no band, header row 1, data row 2) = backward-compat. Dengan Section: band row 1, header row 2, data row 3.
            bool anySection = sortedQuestions.Any(q => q.Section != null);
            int bandRow = 1;
            int headerRow = anySection ? 2 : 1;
            int dataStartRow = headerRow + 1;

            // Band-header merged per Section di atas grup kolom soal (label organisasi saja — D-12: TANPA skor per-Section).
            if (anySection)
            {
                int qIdx = 0;
                foreach (var grp in sortedQuestions
                    .GroupBy(q => q.Section?.SectionNumber)
                    .OrderBy(g => SectionExportLayout.OrderKey(g.Key)))
                {
                    int count = grp.Count();
                    int startCol = 4 + 2 * qIdx;             // kolom 1-3 = No/Nama/NIP (tanpa band)
                    int endCol = startCol + 2 * count - 1;   // tiap soal = 2 kolom (Jawaban + Benar?)
                    var label = SectionExportLayout.Label(grp.Key, grp.First().Section?.Name);
                    ws.Cell(bandRow, startCol).Value = label;
                    var band = ws.Range(bandRow, startCol, bandRow, endCol);
                    band.Merge();
                    band.Style.Font.Bold = true;
                    band.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    band.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    qIdx += count;
                }
            }

            ws.Cell(headerRow, 1).Value = "No";
            ws.Cell(headerRow, 2).Value = "Nama";
            ws.Cell(headerRow, 3).Value = "NIP";
            int col = 4;
            for (int i = 0; i < sortedQuestions.Count; i++)
            {
                ws.Cell(headerRow, col++).Value = $"Soal {i + 1} Jawaban";
                ws.Cell(headerRow, col++).Value = $"Soal {i + 1} Benar?";
            }
            ws.Cell(headerRow, col).Value = "Skor Total";

            var headerRange = ws.Range(headerRow, 1, headerRow, col);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int rowIdx = dataStartRow;
            int seq = 1;
            foreach (var session in sessions)
            {
                ws.Cell(rowIdx, 1).Value = seq++;
                ws.Cell(rowIdx, 2).Value = session.User?.FullName ?? "";
                ws.Cell(rowIdx, 3).Value = session.User?.NIP ?? "";
                int c = 4;
                foreach (var q in sortedQuestions)
                {
                    // Phase 386 PXF-05 (F-DEV-02 D-13) — Jawaban + Benar? via helper bersama (SAMA dengan PDF).
                    // MA multi-row: lewatkan SEMUA response (session, q) ke helper supaya all-or-nothing (SetEquals)
                    // + list semua opsi terpilih. No-response: BuildAnswerCell -> "—", IsQuestionCorrect -> false (MC/MA)
                    // / null (Essay pending). Essay kini di-label > 0 (unifikasi v30.0, ganti >= ScoreValue/2 lama).
                    var responsesForQ = responses
                        .Where(r => r.AssessmentSessionId == session.Id && r.PackageQuestionId == q.Id)
                        .ToList();
                    string jawabanText = AssessmentScoreAggregator.BuildAnswerCell(q, responsesForQ);   // D-10/D-13
                    bool? isCorrect = AssessmentScoreAggregator.IsQuestionCorrect(q, responsesForQ);    // D-09/D-13

                    ws.Cell(rowIdx, c++).Value = jawabanText;

                    string benar = isCorrect == true ? "✓"
                                 : (isCorrect == false ? "✗" : "—");
                    var cell = ws.Cell(rowIdx, c++);
                    cell.Value = benar;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    if (isCorrect == true) cell.Style.Font.FontColor = XLColor.Green;
                    else if (isCorrect == false) cell.Style.Font.FontColor = XLColor.Red;
                }
                ws.Cell(rowIdx, c).Value = session.Score ?? 0;
                rowIdx++;
            }

            // Phase 419 PAG-04 (review fix #8): auto-fit dari headerRow ke bawah — LEWATKAN band row.
            // Label band "Section {n}: {Nama}" yang panjang tersimpan hanya di sel kolom-awal grup; bila ikut
            // di-auto-fit, kolom "Jawaban" soal pertama tiap Section melebar tak proporsional. headerRow=1 (legacy) = identik.
            ws.Columns().AdjustToContents(headerRow);
            ws.SheetView.FreezeRows(anySection ? 2 : 1);
        }

        /// <summary>
        /// Phase 419 PAG-04 (review fix #1): tabel "Detail Jawaban" per-peserta (REQ EXP-03) di sheet per-peserta.
        /// Section-aware — heading "Section {n}: {Nama}" antar grup soal (paritas GeneratePerPesertaPdf + sheet
        /// agregat "Detail Per Soal"). Urut soal canonical (SectionExportLayout.OrderKey, Order, Id). Tanpa Section
        /// sama sekali → tak ada heading (backward-compat). Diekstrak dari AssessmentAdminController supaya bisa
        /// di-unit-test langsung. Mengembalikan baris berikutnya (setelah tabel) agar pemanggil lanjut.
        /// </summary>
        public static int AddPerPesertaDetailJawaban(
            IXLWorksheet ws,
            int startRow,
            List<PackageQuestion> sessionQuestions,
            List<PackageUserResponse> sessionResp)
        {
            int currentRow = startRow;
            var sorted = sessionQuestions
                .OrderBy(q => SectionExportLayout.OrderKey(q.Section?.SectionNumber))
                .ThenBy(q => q.Order)
                .ThenBy(q => q.Id)
                .ToList();

            ws.Cell(currentRow, 1).Value = "Detail Jawaban";
            ws.Cell(currentRow, 1).Style.Font.Bold = true;
            ws.Range(currentRow, 1, currentRow, 6).Merge();
            currentRow++;

            // Table header
            ws.Cell(currentRow, 1).Value = "No";
            ws.Cell(currentRow, 2).Value = "Soal";
            ws.Cell(currentRow, 3).Value = "Tipe";
            ws.Cell(currentRow, 4).Value = "Jawaban Peserta";
            ws.Cell(currentRow, 5).Value = "Jawaban Benar";
            ws.Cell(currentRow, 6).Value = "Status";
            ws.Range(currentRow, 1, currentRow, 6).Style.Font.Bold = true;
            ws.Range(currentRow, 1, currentRow, 6).Style.Fill.BackgroundColor = XLColor.LightBlue;
            currentRow++;

            int no = 1;
            // heading "Section {n}: {Nama}" antar grup; anySectionDetail gate = backward-compat (tanpa Section = legacy).
            bool anySectionDetail = sorted.Any(q => q.Section != null);
            foreach (var grp in sorted
                .GroupBy(q => q.Section?.SectionNumber)
                .OrderBy(g => SectionExportLayout.OrderKey(g.Key)))
            {
                if (anySectionDetail)
                {
                    ws.Cell(currentRow, 1).Value = SectionExportLayout.Label(grp.Key, grp.First().Section?.Name);
                    ws.Cell(currentRow, 1).Style.Font.Bold = true;
                    ws.Range(currentRow, 1, currentRow, 6).Merge();
                    ws.Range(currentRow, 1, currentRow, 6).Style.Fill.BackgroundColor = XLColor.LightGray;
                    currentRow++;
                }
                foreach (var q in grp)
                {
                    string tipe = q.QuestionType ?? "MultipleChoice";

                    if (tipe == "Essay")
                    {
                        // PXF-09: tampilkan jawaban teks peserta + skor essay (bukan placeholder "—").
                        var essayResp = sessionResp.FirstOrDefault(r => r.PackageQuestionId == q.Id);
                        ws.Cell(currentRow, 1).Value = no++;
                        ws.Cell(currentRow, 2).Value = q.QuestionText;
                        ws.Cell(currentRow, 3).Value = "Essay";
                        ws.Cell(currentRow, 4).Value = string.IsNullOrWhiteSpace(essayResp?.TextAnswer) ? "Tidak dijawab" : essayResp.TextAnswer;
                        ws.Cell(currentRow, 5).Value = "—"; // essay: tidak ada "jawaban benar" deterministik
                        ws.Cell(currentRow, 6).Value = essayResp?.EssayScore.HasValue == true
                            ? $"Skor: {essayResp.EssayScore}/{q.ScoreValue}"
                            : "Belum dinilai";
                        currentRow++;
                        continue;
                    }

                    var responses = sessionResp.Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue).ToList();
                    string jawabanText;
                    bool correct;

                    if (!responses.Any())
                    {
                        // Soal tanpa response (Abandoned skip soal) — REQ EXP-03
                        jawabanText = "Tidak dijawab";
                        correct = false;
                    }
                    else if (tipe == "MultipleChoice")
                    {
                        var optId = responses.First().PackageOptionId!.Value;
                        var opt = q.Options.FirstOrDefault(o => o.Id == optId);
                        jawabanText = opt?.OptionText ?? "—";
                        correct = opt?.IsCorrect == true;
                    }
                    else // MultipleAnswer
                    {
                        var selectedIds = responses.Select(r => r.PackageOptionId!.Value).ToHashSet();
                        jawabanText = string.Join(", ",
                            q.Options.Where(o => selectedIds.Contains(o.Id)).Select(o => o.OptionText));
                        var correctIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                        correct = selectedIds.SetEquals(correctIds);
                    }

                    string correctText = string.Join(", ", q.Options.Where(o => o.IsCorrect).Select(o => o.OptionText));

                    ws.Cell(currentRow, 1).Value = no++;
                    ws.Cell(currentRow, 2).Value = q.QuestionText;
                    ws.Cell(currentRow, 3).Value = tipe == "MultipleChoice" ? "SA" : "MA";
                    ws.Cell(currentRow, 4).Value = jawabanText;
                    ws.Cell(currentRow, 5).Value = correctText;
                    ws.Cell(currentRow, 6).Value = correct ? "✓" : "✗";
                    currentRow++;
                }
            }
            return currentRow;
        }

        /// <summary>
        /// Phase 338 CIL-05 (D-03 AMENDED): Sheet "Elemen Teknis" — matrix peserta x elemen.
        /// Header dinamis berdasarkan distinct ElemenTeknis. 1 row per peserta.
        /// Score = CorrectCount/QuestionCount*100 percentage (model: CorrectCount int + QuestionCount int).
        /// </summary>
        public static void AddElemenTeknisSheet(
            XLWorkbook workbook,
            List<AssessmentSession> sessions,
            List<SessionElemenTeknisScore> etScores)
        {
            var ws = workbook.Worksheets.Add("Elemen Teknis");

            var elements = etScores
                .Select(et => et.ElemenTeknis)
                .Where(e => !string.IsNullOrEmpty(e))
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            ws.Cell(1, 1).Value = "Nama";
            ws.Cell(1, 2).Value = "NIP";
            int col = 3;
            foreach (var elem in elements)
                ws.Cell(1, col++).Value = elem;
            ws.Cell(1, col).Value = "Avg";

            var headerRange = ws.Range(1, 1, 1, col);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int rowIdx = 2;
            foreach (var session in sessions)
            {
                ws.Cell(rowIdx, 1).Value = session.User?.FullName ?? "";
                ws.Cell(rowIdx, 2).Value = session.User?.NIP ?? "";
                int c = 3;
                var sessionScores = new List<double>();
                foreach (var elem in elements)
                {
                    var score = etScores.FirstOrDefault(et =>
                        et.AssessmentSessionId == session.Id && et.ElemenTeknis == elem);
                    if (score != null && score.QuestionCount > 0)
                    {
                        double pct = (double)score.CorrectCount / score.QuestionCount * 100.0;
                        ws.Cell(rowIdx, c++).Value = Math.Round(pct, 1);
                        sessionScores.Add(pct);
                    }
                    else
                    {
                        ws.Cell(rowIdx, c++).Value = "—";
                    }
                }
                ws.Cell(rowIdx, c).Value = sessionScores.Any()
                    ? Math.Round(sessionScores.Average(), 1)
                    : 0;
                rowIdx++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }
    }
}
