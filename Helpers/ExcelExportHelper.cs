using ClosedXML.Excel;
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
    }
}
