using ClosedXML.Excel;
using FTN.Dtos;
using System.Diagnostics;

namespace FTN.Services
{
    public interface IExcelReportService
    {
        byte[] GenerateMonthlyReport(dynamic reportData);
        byte[] GenerateMonthlyReport(List<MonthlyReportDto> reportData);
        byte[] GenerateMonthlyReportByRange(List<MonthlyReportDto> reportData, DateTime startDate, DateTime endDate);
        int CalculateDaysInStorage(DateTime? entryDate, DateTime? exitDate);
    }

    public class ExcelReportService : IExcelReportService
    {
        public byte[] GenerateMonthlyReport(dynamic reportData)
        {
            return GenerateMonthlyReportInternal(reportData);
        }

        public byte[] GenerateMonthlyReport(List<MonthlyReportDto> reportData)
        {
            return GenerateMonthlyReportByRange(reportData, DateTime.Now.AddMonths(-1), DateTime.Now);
        }

        public byte[] GenerateMonthlyReportByRange(List<MonthlyReportDto> reportData, DateTime startDate, DateTime endDate)
        {
            try
            {
                var dynamicReport = CreateDynamicReport(reportData, startDate, endDate);
                return GenerateMonthlyReportInternal(dynamicReport);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en GenerateMonthlyReportByRange: {ex.Message}", ex);
            }
        }

        public string DiagnoseNullFields(List<MonthlyReportDto> reportData)
        {
            var nullFields = new List<string>();
            int recordIndex = 0;

            foreach (var record in reportData)
            {
                recordIndex++;

                // Verificar cada propiedad individualmente
                if (record.Id == 0) nullFields.Add($"Record[{recordIndex}].Id");
                if (record.Folio == null) nullFields.Add($"Record[{recordIndex}].Folio");
                if (record.PartNumbers == null) nullFields.Add($"Record[{recordIndex}].PartNumbers");
                if (record.Platforms == null) nullFields.Add($"Record[{recordIndex}].Platforms");
                if (record.TotalPieces == null) nullFields.Add($"Record[{recordIndex}].TotalPieces");
                if (record.EntryDate == null) nullFields.Add($"Record[{recordIndex}].EntryDate");
                if (record.CreatedAt == null) nullFields.Add($"Record[{recordIndex}].CreatedAt");
                if (record.DaysInStorage == null) nullFields.Add($"Record[{recordIndex}].DaysInStorage");
                if (record.EntranceCost == null) nullFields.Add($"Record[{recordIndex}].EntranceCost");
                if (record.ExitCost == null) nullFields.Add($"Record[{recordIndex}].ExitCost");
                if (record.StorageCost == null) nullFields.Add($"Record[{recordIndex}].StorageCost");
                if (record.TotalCost == null) nullFields.Add($"Record[{recordIndex}].TotalCost");
                if (record.Pallets == null) nullFields.Add($"Record[{recordIndex}].Pallets");
            }

            return nullFields.Any()
                ? $"Campos nulos encontrados: {string.Join(", ", nullFields)}"
                : "No se encontraron campos nulos en los datos básicos";
        }

        public string DiagnoseDynamicObject(dynamic reportData)
        {
            var nullProperties = new List<string>();

            try
            {
                if (reportData == null) return "El objeto reportData es nulo";

                var propertiesToCheck = new[] { "MonthName", "Year", "TotalRecords", "TotalPallets",
                                          "ActiveRecords", "CompleteRecords", "TotalGeneralCost", "Records" };

                foreach (var prop in propertiesToCheck)
                {
                    try
                    {
                        var value = GetDynamicProperty(reportData, prop);
                        if (value == null) nullProperties.Add(prop);
                    }
                    catch
                    {
                        nullProperties.Add($"{prop} (no existe)");
                    }
                }

                if (reportData.Records != null)
                {
                    int recordIndex = 0;
                    foreach (var record in reportData.Records)
                    {
                        recordIndex++;
                        var recordProperties = new[] { "Id", "Folio", "PartNumbers", "Pallets", "EntryDate",
                                                 "ExitDate", "DaysInStorage", "EntranceCost", "ExitCost",
                                                 "StorageCost", "TotalCost" };

                        foreach (var prop in recordProperties)
                        {
                            try
                            {
                                var value = GetDynamicProperty(record, prop);
                                if (value == null)
                                    nullProperties.Add($"Records[{recordIndex}].{prop}");
                            }
                            catch
                            {
                                nullProperties.Add($"Records[{recordIndex}].{prop} (no existe)");
                            }
                        }
                    }
                }

                return nullProperties.Any()
                    ? $"Propiedades nulas en dynamic: {string.Join(", ", nullProperties)}"
                    : "No se encontraron propiedades nulas en el objeto dynamic";
            }
            catch (Exception ex)
            {
                return $"Error en diagnóstico: {ex.Message}";
            }
        }

        private object GetDynamicProperty(dynamic obj, string propertyName)
        {
            try
            {
                return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);
            }
            catch
            {
                try
                {
                    switch (propertyName)
                    {
                        case "MonthName": return obj.MonthName;
                        case "Year": return obj.Year;
                        case "TotalRecords": return obj.TotalRecords;
                        case "TotalPallets": return obj.TotalPallets;
                        case "ActiveRecords": return obj.ActiveRecords;
                        case "CompleteRecords": return obj.CompleteRecords;
                        case "TotalGeneralCost": return obj.TotalGeneralCost;
                        case "Records": return obj.Records;
                        case "Id": return obj.Id;
                        case "Folio": return obj.Folio;
                        case "PartNumbers": return obj.PartNumbers;
                        case "Pallets": return obj.Pallets;
                        case "EntryDate": return obj.EntryDate;
                        case "ExitDate": return obj.ExitDate;
                        case "DaysInStorage": return obj.DaysInStorage;
                        case "EntranceCost": return obj.EntranceCost;
                        case "ExitCost": return obj.ExitCost;
                        case "StorageCost": return obj.StorageCost;
                        case "TotalCost": return obj.TotalCost;
                        default: return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        private dynamic CreateDynamicReport(List<MonthlyReportDto> reportData, DateTime startDate, DateTime endDate)
        {
            try
            {
                if (reportData == null || !reportData.Any())
                {
                    return new
                    {
                        MonthName = $"{startDate:dd/MM/yyyy} a {endDate:dd/MM/yyyy}",
                        Year = DateTime.Now.Year,
                        TotalRecords = 0,
                        TotalPallets = 0,
                        ActiveRecords = 0,
                        CompleteRecords = 0,
                        TotalGeneralCost = 0m,
                        Records = new List<object>()
                    };
                }

                var totalPallets = reportData.Sum(r => r.Pallets);
                var activeRecords = reportData.Count(r => r.ExitDate == null);
                var completeRecords = reportData.Count(r => r.ExitDate != null);
                var totalGeneralCost = reportData.Sum(r => r.TotalCost);

                var records = new List<object>();
                foreach (var record in reportData)
                {
                    try
                    {
                        var safeRecord = new
                        {
                            Id = record.Id,
                            Folio = SafeGetString(record.Folio),
                            PartNumbers = SafeGetString(record.PartNumbers),
                            Pallets = record.Pallets,
                            EntryDate = SafeGetDateString(record.EntryDate),
                            ExitDate = SafeGetDateString(record.ExitDate) ?? "Sin salida",
                            DaysInStorage = record.DaysInStorage,
                            EntranceCost = record.EntranceCost,
                            ExitCost = record.ExitCost,
                            StorageCost = record.StorageCost,
                            TotalCost = record.TotalCost
                        };
                        records.Add(safeRecord);
                    }
                    catch (Exception recordEx)
                    {
                        var fallbackRecord = new
                        {
                            Id = 0,
                            Folio = "ERROR",
                            PartNumbers = $"Error procesando registro: {recordEx.Message}",
                            Pallets = 0,
                            EntryDate = "N/A",
                            ExitDate = "N/A",
                            DaysInStorage = 0,
                            EntranceCost = 0m,
                            ExitCost = 0m,
                            StorageCost = 0m,
                            TotalCost = 0m
                        };
                        records.Add(fallbackRecord);
                    }
                }

                return new
                {
                    MonthName = $"{startDate:dd/MM/yyyy} a {endDate:dd/MM/yyyy}",
                    Year = DateTime.Now.Year,
                    TotalRecords = reportData.Count,
                    TotalPallets = totalPallets,
                    ActiveRecords = activeRecords,
                    CompleteRecords = completeRecords,
                    TotalGeneralCost = totalGeneralCost,
                    Records = records
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creando reporte dinámico: {ex.Message}", ex);
            }
        }

        private byte[] GenerateMonthlyReportInternal(dynamic reportData)
        {
            try
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
            catch (Exception ex)
            {
                throw new Exception($"Error generando archivo Excel: {ex.Message}", ex);
            }
        }

        private void GenerateDetailsSheet(IXLWorksheet worksheet, dynamic reportData)
        {
            try
            {
                if (reportData == null)
                {
                    worksheet.Cell("A1").Value = "ERROR: Datos del reporte nulos";
                    return;
                }

                var titleCell = worksheet.Cell("A1");
                string monthName = SafeGetDynamicString(reportData.MonthName) ?? "Rango Personalizado";
                int year = SafeGetDynamicInt(reportData.Year) ?? DateTime.Now.Year;

                titleCell.Value = $"REPORTE MENSUAL - {monthName.ToUpper()} {year}";
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 16;
                titleCell.Style.Font.FontColor = XLColor.White;
                titleCell.Style.Fill.BackgroundColor = XLColor.DarkBlue;
                titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Range("A1:L1").Merge();
                worksheet.Row(1).Height = 30;

                worksheet.Cell("A2").Value = $"Total Registros: {SafeGetDynamicInt(reportData.TotalRecords) ?? 0}";
                worksheet.Cell("B2").Value = $"Total Tarimas: {SafeGetDynamicInt(reportData.TotalPallets) ?? 0}";
                worksheet.Cell("C2").Value = $"Activos: {SafeGetDynamicInt(reportData.ActiveRecords) ?? 0}";
                worksheet.Cell("D2").Value = $"Completados: {SafeGetDynamicInt(reportData.CompleteRecords) ?? 0}";
                worksheet.Cell("E2").Value = $"Costo Total: ${SafeGetDynamicDecimal(reportData.TotalGeneralCost) ?? 0m:N2}";

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
                if (reportData.Records != null)
                {
                    foreach (var record in reportData.Records)
                    {
                        try
                        {
                            worksheet.Cell(row, 1).Value = SafeGetDynamicInt(record?.Id) ?? 0;
                            worksheet.Cell(row, 2).Value = SafeGetDynamicString(record?.Folio) ?? "N/A";
                            worksheet.Cell(row, 3).Value = SafeGetDynamicString(record?.PartNumbers) ?? "N/A";
                            worksheet.Cell(row, 4).Value = SafeGetDynamicInt(record?.Pallets) ?? 0;
                            worksheet.Cell(row, 5).Value = SafeGetDynamicString(record?.EntryDate) ?? "N/A";
                            worksheet.Cell(row, 6).Value = SafeGetDynamicString(record?.ExitDate) ?? "Sin salida";
                            worksheet.Cell(row, 7).Value = SafeGetDynamicInt(record?.DaysInStorage) ?? 0;

                            worksheet.Cell(row, 8).Value = SafeGetDynamicDecimal(record?.EntranceCost) ?? 0m;
                            worksheet.Cell(row, 9).Value = SafeGetDynamicDecimal(record?.ExitCost) ?? 0m;
                            worksheet.Cell(row, 10).Value = SafeGetDynamicDecimal(record?.StorageCost) ?? 0m;
                            worksheet.Cell(row, 11).Value = SafeGetDynamicDecimal(record?.TotalCost) ?? 0m;

                            for (int col = 8; col <= 11; col++)
                            {
                                var cell = worksheet.Cell(row, col);
                                cell.Style.NumberFormat.Format = "$#,##0.00";
                            }

                            var statusCell = worksheet.Cell(row, 12);
                            var exitDate = SafeGetDynamicString(record?.ExitDate);
                            if (exitDate == "Sin salida" || string.IsNullOrEmpty(exitDate))
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
                        catch (Exception cellEx)
                        {
                            Debug.WriteLine($"Error en fila {row}: {cellEx.Message}");
                            row++;
                        }
                    }
                }

                if (row > 5)
                {
                    var dataRange = worksheet.Range($"A4:L{row - 1}");
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    worksheet.SheetView.FreezeRows(4);

                    worksheet.Range($"A4:L{row - 1}").SetAutoFilter();

                    var daysColumn = worksheet.Range($"G5:G{row - 1}");
                    daysColumn.AddConditionalFormat().WhenGreaterThan(30).Fill.SetBackgroundColor(XLColor.LightSalmon);
                    daysColumn.AddConditionalFormat().WhenBetween(15, 30).Fill.SetBackgroundColor(XLColor.LightYellow);

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

                worksheet.Columns().AdjustToContents();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generando hoja de detalles: {ex.Message}", ex);
            }
        }

        private string SafeGetString(object value)
        {
            return value?.ToString()?.Trim() ?? string.Empty;
        }

        private string SafeGetDateString(DateTime? date)
        {
            return date?.ToString("yyyy-MM-dd") ?? null!;
        }

        private string SafeGetDynamicString(dynamic value)
        {
            try
            {
                return value?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private int? SafeGetDynamicInt(dynamic value)
        {
            try
            {
                if (value == null) return 0;
                if (int.TryParse(value.ToString(), out int result)) return result;
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private decimal? SafeGetDynamicDecimal(dynamic value)
        {
            try
            {
                if (value == null) return 0m;
                if (decimal.TryParse(value.ToString(), out decimal result)) return result;
                return 0m;
            }
            catch
            {
                return 0m;
            }
        }

        public int CalculateDaysInStorage(DateTime? entryDate, DateTime? exitDate)
        {
            try
            {
                if (!entryDate.HasValue) return 0;
                var endDate = exitDate ?? DateTime.Now;
                return (int)(endDate - entryDate.Value).TotalDays;
            }
            catch
            {
                return 0;
            }
        }
    }
}