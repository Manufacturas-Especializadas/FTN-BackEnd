using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using FTN.Dtos;

namespace FTN.Services
{
    public interface IExcelReportService
    {
        byte[] GenerateMonthlyReport(MonthlyReportResponseDto reportData);
    }

    public class ExcelReportService : IExcelReportService
    {
        public byte[] GenerateMonthlyReport(MonthlyReportResponseDto reportData)
        {
            using (var workbook = new XLWorkbook())
            {
                var summaryWorkSheet = workbook.Worksheets.Add("Summary");
                GenerateSummarySheet(summaryWorkSheet, reportData);

                var detailsWorksheet = workbook.Worksheets.Add("Detailed Records");
                GenerateDetailsSheet(detailsWorksheet, reportData);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        private void GenerateSummarySheet(IXLWorksheet worksheet, MonthlyReportResponseDto reportData)
        {
            worksheet.Cell("A1").Value = $"REPORTE MENSUAL - {reportData.MonthName} {reportData.Year}";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A1:F1").Merge();

            worksheet.Cell("A2").Value = $"Generado el: {DateTime.Now:dd-MM-yyyy HH:mm}";
            worksheet.Cell("A2").Style.Font.Italic = true;
            worksheet.Range("A2:F2").Merge();

            worksheet.Cell("A4").Value = "RESUMEN";
            worksheet.Cell("A4").Style.Font.Bold = true;
            worksheet.Cell("A4").Style.Font.FontSize = 14;
            worksheet.Cell("A4").Style.Fill.BackgroundColor = XLColor.LightGray;

            var summaryData = new[]
            {
                new { Metric = "Registros totales", Value = reportData.TotalRecords.ToString() },
                new { Metric = "Tarimas totales", Value = reportData.TotalPallets.ToString() },
                new { Metric = "Registros activos", Value = reportData.ActiveRecords.ToString() },
                new { Metric = "Registros completados", Value = reportData.CompletedRecords.ToString() },
                new { Metric = "Costo total de entrada", Value = reportData.TotalEntryCost.ToString("C2") },
                new { Metric = "Costo total de salida", Value = reportData.TotalExitCost.ToString("C2") },
                new { Metric = "Costo total de almacenamiento", Value = reportData.TotalStorageCost.ToString("C2") },
                new { Metric = "Costo general total", Value = reportData.TotalGeneralCost.ToString("C2") }
            };

            for (int i = 0; i < summaryData.Length; i++)
            {
                worksheet.Cell($"A{i + 5}").Value = summaryData[i].Metric;
                worksheet.Cell($"B{i + 5}").Value = summaryData[i].Value;
            }

            var summaryRange = worksheet.Range("A4:B12");
            summaryRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            summaryRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            worksheet.Columns().AdjustToContents();
        }

        private void GenerateDetailsSheet(IXLWorksheet worksheet, MonthlyReportResponseDto reportData)
        {
            worksheet.Cell("A1").Value = $"DETALLES {reportData.MonthName} - {reportData.Year}";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A1:L1").Merge();

            var headers = new[]
            {
        "ID", "Folio", "Número de parte", "Tarimas", "Fecha de entrada", "Fecha de salida",
        "Dias en almacén", "Costo de entrada", "Costo de salida", "Costo de almacén",
        "Costo total", "Estatus"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(3, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 4;
            foreach (var record in reportData.Records)
            {
                worksheet.Cell(row, 1).Value = record.Id;
                worksheet.Cell(row, 2).Value = record.Folio;
                worksheet.Cell(row, 3).Value = record.PartNumber;
                worksheet.Cell(row, 4).Value = record.Pallets;
                worksheet.Cell(row, 5).Value = record.EntryDate.ToString("dd-MM-yyyy HH:mm");
                worksheet.Cell(row, 6).Value = record.ExitDate?.ToString("dd-MM-yyyy HH:mm") ?? "Pendiente";
                worksheet.Cell(row, 7).Value = record.DaysInStorage;
                worksheet.Cell(row, 8).Value = record.EntryCost;
                worksheet.Cell(row, 9).Value = record.ExitCost;
                worksheet.Cell(row, 10).Value = record.StorageCost;
                worksheet.Cell(row, 11).Value = record.TotalCost;
                worksheet.Cell(row, 12).Value = record.Status;

                for (int col = 8; col <= 11; col++)
                {
                    worksheet.Cell(row, col).Style.NumberFormat.Format = "$#,##0.00";
                }

                var statusCell = worksheet.Cell(row, 12);
                if (record.IsActive)
                {
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                }
                else
                {
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                }

                row++;
            }

            var dataRange = worksheet.Range($"A3:L{row - 1}");
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            worksheet.SheetView.FreezeRows(3);

            worksheet.Range($"A3:L{row - 1}").SetAutoFilter();

            worksheet.Columns().AdjustToContents();

            var daysColumn = worksheet.Range($"G4:G{row - 1}");
            daysColumn.AddConditionalFormat().WhenGreaterThan(30).Fill.SetBackgroundColor(XLColor.LightSalmon);
        }
    }
}