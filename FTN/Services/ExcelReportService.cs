using ClosedXML.Excel;
using FTN.Dtos;

namespace FTN.Services
{
    public interface IExcelReportService
    {
        byte[] GenerateMonthlyReport(dynamic reportData);
    }

    public class ExcelReportService : IExcelReportService
    {
        public byte[] GenerateMonthlyReport(dynamic reportData)
        {
            using (var workbook = new XLWorkbook())
            {
                var detailsWorksheet = workbook.Worksheets.Add("Detalles por Folio");
                GenerateDetailsSheet(detailsWorksheet, reportData);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        private void GenerateDetailsSheet(IXLWorksheet worksheet, dynamic reportData)
        {
            var titleCell = worksheet.Cell("A1");
            titleCell.Value = $"REPORTE MENSUAL - {reportData.MonthName.ToUpper()} {reportData.Year}";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Font.FontColor = XLColor.White;
            titleCell.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A1:L1").Merge();
            worksheet.Row(1).Height = 30;

            worksheet.Cell("A2").Value = $"Total Registros: {reportData.TotalRecords}";
            worksheet.Cell("B2").Value = $"Total Tarimas: {reportData.TotalPallets}";
            worksheet.Cell("C2").Value = $"Activos: {reportData.ActiveRecords}";
            worksheet.Cell("D2").Value = $"Completados: {reportData.CompleteRecords}";
            worksheet.Cell("E2").Value = $"Costo Total: ${reportData.TotalGeneralCost:N2}";

            var summaryRange = worksheet.Range("A2:E2");
            summaryRange.Style.Font.Bold = true;
            summaryRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            summaryRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            summaryRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            var headers = new[]
            {
                "ID", "Folio", "Números de Parte", "Tarimas", "Fecha Entrada",
                "Fecha Salida", "Días en Almacén", "Costo Entrada", "Costo Salida",
                "Costo Almacén", "Costo Total", "Estatus"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int row = 5;
            foreach (var record in reportData.Records)
            {
                worksheet.Cell(row, 1).Value = record.Id;
                worksheet.Cell(row, 2).Value = record.Folio;
                worksheet.Cell(row, 3).Value = record.PartNumbers;
                worksheet.Cell(row, 4).Value = record.Pallets;
                worksheet.Cell(row, 5).Value = record.EntryDate;
                worksheet.Cell(row, 6).Value = record.ExitDate;
                worksheet.Cell(row, 7).Value = record.DaysInStorage;
                worksheet.Cell(row, 8).Value = record.EntranceCost;
                worksheet.Cell(row, 9).Value = record.ExitCost;
                worksheet.Cell(row, 10).Value = record.StorageCost;
                worksheet.Cell(row, 11).Value = record.TotalCost;

                for (int col = 8; col <= 11; col++)
                {
                    worksheet.Cell(row, col).Style.NumberFormat.Format = "$#,##0.00";
                }

                var statusCell = worksheet.Cell(row, 12);
                if (record.ExitDate == "Sin salir")
                {
                    statusCell.Value = "ACTIVO";
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                    statusCell.Style.Font.FontColor = XLColor.DarkOrange;
                }
                else
                {
                    statusCell.Value = "COMPLETADO";
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                    statusCell.Style.Font.FontColor = XLColor.DarkGreen;
                }
                statusCell.Style.Font.Bold = true;
                statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (row % 2 == 0)
                {
                    worksheet.Range($"A{row}:L{row}").Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                row++;
            }

            var dataRange = worksheet.Range($"A4:L{row - 1}");
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            worksheet.SheetView.FreezeRows(4);

            worksheet.Range($"A4:L{row - 1}").SetAutoFilter();

            var daysColumn = worksheet.Range($"G5:G{row - 1}");
            daysColumn.AddConditionalFormat().WhenGreaterThan(30).Fill.SetBackgroundColor(XLColor.LightSalmon);
            daysColumn.AddConditionalFormat().WhenBetween(15, 30).Fill.SetBackgroundColor(XLColor.LightYellow);

            worksheet.Columns().AdjustToContents();

            worksheet.Cell($"A{row + 1}").Value = "TOTALES:";
            worksheet.Cell($"A{row + 1}").Style.Font.Bold = true;
            worksheet.Cell($"D{row + 1}").FormulaA1 = $"=SUBTOTAL(9,D5:D{row - 1})";
            worksheet.Cell($"H{row + 1}").FormulaA1 = $"=SUBTOTAL(9,H5:H{row - 1})";
            worksheet.Cell($"I{row + 1}").FormulaA1 = $"=SUBTOTAL(9,I5:I{row - 1})";
            worksheet.Cell($"J{row + 1}").FormulaA1 = $"=SUBTOTAL(9,J5:J{row - 1})";
            worksheet.Cell($"K{row + 1}").FormulaA1 = $"=SUBTOTAL(9,K5:K{row - 1})";

            var totalRange = worksheet.Range($"A{row + 1}:L{row + 1}");
            totalRange.Style.Fill.BackgroundColor = XLColor.DarkGray;
            totalRange.Style.Font.FontColor = XLColor.White;
            totalRange.Style.Font.Bold = true;

            for (int col = 8; col <= 11; col++)
            {
                worksheet.Cell(row + 1, col).Style.NumberFormat.Format = "$#,##0.00";
            }
        }
    }
}