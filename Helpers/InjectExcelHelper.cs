using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using HcPortal.Models;
using HcPortal.ViewModels;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Phase 396 INJ-10 — lapisan translasi tipis Excel ⟷ <see cref="InjectAssessmentViewModel.InjectWorkerAnswersVM"/>.
    /// Pure / EF-free (hanya System.*, ClosedXML.Excel, HcPortal.Models, HcPortal.ViewModels) → unit-testable tanpa DB
    /// (analog <see cref="AssessmentScoreAggregator"/>). NOL grading di sini — parser hanya menerjemahkan sel jadi spec
    /// yang BENTUKNYA SAMA dengan payload form (#AnswersJson) → preview &amp; commit reuse jalur 393/395 tanpa cabang baru.
    ///
    /// D-04 (Pitfall 1, HIGHEST RISK): SATU comparator dipakai BOTH generate &amp; parse
    /// (urut menaik Order lalu TempId) supaya kolom-soal ⟷ soal stabil round-trip.
    /// Huruf opsi memetakan ke urutan AUTHORED (A=Options[0], B=Options[1], …) — JANGAN OrderBy(TempId).
    /// D-06: sel kosong di-OMIT (SkippedBlank++), BUKAN spec MC/MA kosong (yang akan reject-all di PreflightValidate).
    /// D-02/D-09: NIP-not-in-picker / huruf invalid / skor essay di luar rentang → per-row/per-cell InjectRowError.
    /// </summary>
    public static class InjectExcelHelper
    {
        private const string AnswerSheetName = "Jawaban";
        private const string LegendSheetName = "Legenda";

        /// <summary>'A'→0, 'B'→1, … 'Z'→25. Selain 1 huruf A-Z → -1 (invalid).</summary>
        private static int LetterToIndex(string letter)
        {
            var s = (letter ?? "").Trim().ToUpperInvariant();
            if (s.Length != 1 || s[0] < 'A' || s[0] > 'Z') return -1;
            return s[0] - 'A';
        }

        /// <summary>0→"A", 1→"B", … (untuk header legenda + pesan error). Negatif → "?".</summary>
        private static string IndexToLetter(int i) => i < 0 ? "?" : ((char)('A' + i)).ToString();

        /// <summary>
        /// Bangun template 2-sheet: "Jawaban" (matrix: 1 baris per pekerja, 1 blok kolom per soal) + "Legenda"
        /// (kamus huruf↔teks opsi). Kolom-soal urut <c>OrderBy(Order).ThenBy(TempId)</c> (D-04). Tidak menulis stream —
        /// controller GET memanggil <see cref="ExcelExportHelper.ToFileResult"/>.
        /// </summary>
        public static XLWorkbook GenerateTemplate(
            IReadOnlyList<InjectQuestionSpec> questions,
            IReadOnlyList<(string Nip, string Name)> workers)
        {
            var orderedQ = questions.OrderBy(q => q.Order).ThenBy(q => q.TempId).ToList();

            var workbook = new XLWorkbook();

            // ── Sheet 1: "Jawaban" (matrix) ──
            var ws = workbook.Worksheets.Add(AnswerSheetName);
            ws.Cell(1, 1).Value = "NIP";
            ws.Cell(1, 2).Value = "Nama";

            int col = 3;
            for (int i = 0; i < orderedQ.Count; i++)
            {
                var q = orderedQ[i];
                int idx = i + 1;   // nomor soal tampilan (1-based)
                var type = q.QuestionType ?? "MultipleChoice";

                if (type == "Essay")
                {
                    ws.Cell(1, col++).Value = $"Soal {idx} Skor (0..{q.ScoreValue})";
                    ws.Cell(1, col++).Value = $"Soal {idx} Teks (opsional)";
                }
                else if (type == "MultipleAnswer")
                {
                    ws.Cell(1, col++).Value = $"Soal {idx} (MA huruf, pisah koma)";
                }
                else
                {
                    ws.Cell(1, col++).Value = $"Soal {idx} (MC 1 huruf)";
                }
            }

            int lastCol = col - 1;
            var headerRange = ws.Range(1, 1, 1, lastCol);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
            headerRange.Style.Font.FontColor = XLColor.White;
            ws.SheetView.FreezeRows(1);

            // Data: 1 baris per pekerja, sel jawaban dibiarkan kosong (diisi HC).
            int row = 2;
            foreach (var (nip, name) in workers)
            {
                ws.Cell(row, 1).Value = nip ?? "";
                ws.Cell(row, 2).Value = name ?? "";
                row++;
            }
            ws.Columns().AdjustToContents();

            // ── Sheet 2: "Legenda" (kamus opsi) ──
            var leg = workbook.Worksheets.Add(LegendSheetName);
            leg.Cell(1, 1).Value = "No";
            leg.Cell(1, 2).Value = "Teks Soal";
            leg.Cell(1, 3).Value = "Tipe";
            leg.Cell(1, 4).Value = "Skor Maks";
            leg.Cell(1, 5).Value = "Opsi (huruf = teks)";
            var legHeader = leg.Range(1, 1, 1, 5);
            legHeader.Style.Font.Bold = true;
            legHeader.Style.Fill.BackgroundColor = XLColor.LightGray;

            int lrow = 2;
            for (int i = 0; i < orderedQ.Count; i++)
            {
                var q = orderedQ[i];
                int idx = i + 1;
                var type = q.QuestionType ?? "MultipleChoice";

                leg.Cell(lrow, 1).Value = idx;
                leg.Cell(lrow, 2).Value = q.QuestionText ?? "";
                leg.Cell(lrow, 3).Value = type;
                leg.Cell(lrow, 4).Value = q.ScoreValue;

                if (type == "Essay")
                {
                    leg.Cell(lrow, 5).Value = "(jawaban teks/skor)";
                }
                else
                {
                    // Huruf ↔ teks dalam urutan AUTHORED (A=Options[0]).
                    var optStr = string.Join("; ",
                        q.Options.Select((opt, oi) => $"{IndexToLetter(oi)}={opt.OptionText}"));
                    leg.Cell(lrow, 5).Value = optStr;
                }
                lrow++;
            }
            leg.Columns().AdjustToContents();

            return workbook;
        }

        /// <summary>
        /// Parse matrix sheet "Jawaban" → daftar <see cref="InjectAssessmentViewModel.InjectWorkerAnswersVM"/>
        /// (Mode="manual"), daftar <see cref="InjectRowError"/> (D-02/D-09), dan jumlah sel kosong yang di-skip (D-06).
        /// Comparator soal IDENTIK dengan generate (D-04). Tidak melempar — file rusak → 1 error ramah (Security V5/V12).
        /// </summary>
        public static (List<InjectAssessmentViewModel.InjectWorkerAnswersVM> Workers,
                       List<InjectRowError> Errors,
                       int SkippedBlank) ParseMatrix(
            Stream stream,
            IReadOnlyList<InjectQuestionSpec> questions,
            IReadOnlySet<string> allowedNips,
            IReadOnlyDictionary<string, string> nipToUserId)
        {
            var workers = new List<InjectAssessmentViewModel.InjectWorkerAnswersVM>();
            var errors = new List<InjectRowError>();
            int skippedBlank = 0;

            XLWorkbook wb;
            try
            {
                wb = new XLWorkbook(stream);
            }
            catch (Exception)
            {
                errors.Add(new InjectRowError { Nip = "", Message = "Gagal membaca file Excel. Pastikan file .xlsx valid dan tidak rusak." });
                return (workers, errors, skippedBlank);
            }

            using (wb)
            {
                var ws = wb.Worksheets.FirstOrDefault(s => s.Name == AnswerSheetName)
                         ?? wb.Worksheets.First();

                var orderedQ = questions.OrderBy(q => q.Order).ThenBy(q => q.TempId).ToList();

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
                for (int row = 2; row <= lastRow; row++)
                {
                    var nip = ws.Cell(row, 1).GetString().Trim();
                    if (string.IsNullOrEmpty(nip)) continue;   // baris kosong total → skip

                    if (!allowedNips.Contains(nip))
                    {
                        errors.Add(new InjectRowError { Nip = nip, Message = $"Baris {row}: NIP {nip} tidak ada di daftar pekerja terpilih." });
                        continue;
                    }

                    int col = 3;
                    var answers = new List<InjectAssessmentViewModel.InjectAnswerVM>();
                    int idx = 0;

                    foreach (var q in orderedQ)
                    {
                        idx++;
                        var type = q.QuestionType ?? "MultipleChoice";

                        if (type == "Essay")
                        {
                            var scoreCell = ws.Cell(row, col++);
                            var text = ws.Cell(row, col++).GetString().Trim();

                            // Sel skor kosong → OMIT (D-06).
                            var scoreRaw = scoreCell.GetString().Trim();
                            bool hasScore = scoreCell.TryGetValue<double>(out var sd);
                            int score;
                            if (hasScore)
                            {
                                score = (int)sd;
                            }
                            else if (string.IsNullOrEmpty(scoreRaw))
                            {
                                skippedBlank++;
                                continue;
                            }
                            else if (int.TryParse(scoreRaw, out var si))
                            {
                                score = si;
                            }
                            else
                            {
                                errors.Add(new InjectRowError { Nip = nip, Message = $"Baris {row}, kolom Soal {idx} (Essay): skor tidak valid." });
                                continue;
                            }

                            if (score < 0 || score > q.ScoreValue)
                            {
                                errors.Add(new InjectRowError { Nip = nip, Message = $"Baris {row}, kolom Soal {idx} (Essay): skor {score} melebihi maksimum {q.ScoreValue}." });
                                continue;
                            }

                            answers.Add(new InjectAssessmentViewModel.InjectAnswerVM
                            {
                                QuestionTempId = q.TempId,
                                EssayScore = score,
                                TextAnswer = string.IsNullOrEmpty(text) ? null : text
                            });
                        }
                        else
                        {
                            var cell = ws.Cell(row, col++).GetString().Trim();
                            if (string.IsNullOrEmpty(cell))
                            {
                                skippedBlank++;   // OMIT (D-06) — JANGAN push spec kosong
                                continue;
                            }

                            var letters = cell.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            var selected = new List<int>();
                            foreach (var letter in letters)
                            {
                                int oi = LetterToIndex(letter);
                                if (oi < 0 || oi >= q.Options.Count)
                                {
                                    string maxLetter = q.Options.Count > 0 ? IndexToLetter(q.Options.Count - 1) : "?";
                                    errors.Add(new InjectRowError { Nip = nip, Message = $"Baris {row}, kolom Soal {idx}: opsi '{letter}' tidak valid (hanya A..{maxLetter})." });
                                    continue;   // lewati huruf invalid; tetap kumpulkan yang valid
                                }
                                selected.Add(q.Options[oi].TempId);   // AUTHORED order (A=Options[0])
                            }

                            if (selected.Count > 0)
                            {
                                answers.Add(new InjectAssessmentViewModel.InjectAnswerVM
                                {
                                    QuestionTempId = q.TempId,
                                    SelectedOptionTempIds = selected
                                });
                            }
                            // semua huruf invalid → error sudah dicatat; tidak push spec.
                        }
                    }

                    nipToUserId.TryGetValue(nip, out var uid);
                    workers.Add(new InjectAssessmentViewModel.InjectWorkerAnswersVM
                    {
                        UserId = uid ?? "",
                        Mode = "manual",
                        Answers = answers
                    });
                }
            }

            return (workers, errors, skippedBlank);
        }
    }
}
