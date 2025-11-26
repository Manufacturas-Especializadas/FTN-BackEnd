using DocumentFormat.OpenXml.Math;
using FluentValidation;
using FTN.Data;
using FTN.Dtos;
using FTN.Models;
using FTN.Services;
using FTN.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using static FTN.Services.ExcelReportService;

namespace FTN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StageEntranceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IValidator<StageEntrancesDto> _validator;
        private readonly IExcelReportService _excelReportService;

        public StageEntranceController(
            AppDbContext context,
            IValidator<StageEntrancesDto> validator,
            IExcelReportService excelReportService
        )
        {
            _context = context;
            _validator = validator;
            _excelReportService = excelReportService;
        }

        [HttpGet]
        [Route("GetStageEntrances")]
        public async Task<IActionResult> GetStageEntrances()
        {
            var stageEntrances = await _context.StageEntrances
                    .AsNoTracking()
                    .Select(se => new
                    {
                        se.Id,
                        se.Folio,
                        PartNumbers = _context.StageEntrancePartNumbers
                            .Where(pn => pn.StageEntranceId == se.Id)
                            .Select(pn => new { pn.PartNumber, pn.Quantity })
                            .ToList(),
                        se.TotalPieces,
                        se.Platforms,
                        se.EntryDate,
                        se.ExitDate,
                        EntranceFee = se.IdEntranceFeeNavigation != null ? se.IdEntranceFeeNavigation.Cost : 67.50m,
                        StorageCost = se.IdStorageCostNavigation != null ? se.IdStorageCostNavigation.Cost : 133
                    })
                    .ToListAsync();

            return Ok(stageEntrances);
        }

        [HttpGet]
        [Route("GetStageEntranceById/{id}")]
        public async Task<IActionResult> GetStageEntranceById(int id)
        {
            var stageEntrance = await _context.StageEntrances
                    .AsNoTracking()
                    .Where(se => se.Id == id)
                    .Select(se => new
                    {
                        se.Id,
                        se.Folio,
                        se.TotalPieces,
                        se.Platforms,
                        se.EntryDate,
                        se.ExitDate,
                        se.CreatedAt,
                        se.UpdatedAt,
                        PartNumbers = _context.StageEntrancePartNumbers
                            .Where(pn => pn.StageEntranceId == id)
                            .Select(pn => new { pn.Id, pn.PartNumber, pn.Quantity }),
                        IdStorageCost = se.IdStorageCost,
                        IdEntranceFee = se.IdEntranceFee,
                    })
                    .FirstOrDefaultAsync();

            if (stageEntrance == null)
            {
                return NotFound(new { message = "Registro no encontrado" });
            }

            return Ok(stageEntrance);
        }

        [HttpGet]
        [Route("SearchByPartNumber/{partNumber}")]
        public async Task<IActionResult> SearchByPartNumber(string partNumber)
        {
            try
            {
                var query = from pn in _context.StageEntrancePartNumbers
                            join se in _context.StageEntrances on pn.StageEntranceId equals se.Id
                            where pn.PartNumber.Contains(partNumber) && se.Platforms > 0
                            select new
                            {
                                PartNumber = pn.PartNumber,
                                Quantity = pn.Quantity,
                                StageEntrance = new
                                {
                                    se.Id,
                                    se.Folio,
                                    se.Platforms,
                                    se.TotalPieces,
                                    se.EntryDate,
                                    se.ExitDate
                                }
                            };

                var results = await query.ToListAsync();

                if (!results.Any())
                {
                    return Ok(new List<object>());
                }

                var stageEntranceIds = results.Select(r => r.StageEntrance.Id).Distinct();

                var allPartNumbers = await _context.StageEntrancePartNumbers
                    .Where(pn => stageEntranceIds.Contains(pn.StageEntranceId))
                    .Select(pn => new
                    {
                        pn.StageEntranceId,
                        pn.PartNumber,
                        pn.Quantity
                    })
                    .ToListAsync();

                var groupedResults = results
                    .GroupBy(r => r.PartNumber)
                    .Select(g => new
                    {
                        PartNumber = g.Key,
                        Folios = g.Select(x => new
                        {
                            Folio = x.StageEntrance.Folio,
                            PartNumber = x.PartNumber,
                            Platforms = x.StageEntrance.Platforms,
                            TotalPieces = x.StageEntrance.TotalPieces,
                            EntryDate = x.StageEntrance.EntryDate,
                            ExitDate = x.StageEntrance.ExitDate,
                            PartNumbers = allPartNumbers
                                .Where(pn => pn.StageEntranceId == x.StageEntrance.Id)
                                .Select(pn => new { pn.PartNumber, pn.Quantity })
                                .ToList()
                        }).ToList(),
                        TotalPlatforms = g.Sum(x => x.StageEntrance.Platforms ?? 0),
                        TotalPieces = g.Sum(x => x.Quantity)
                    })
                    .ToList();

                return Ok(groupedResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en SearchByPartNumber: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al buscar por número de parte",
                    detalles = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("SearchByFolio/{folio}")]
        public async Task<IActionResult> SearchByFolio(int folio)
        {
            try
            {
                var query = from se in _context.StageEntrances
                            where se.Folio == folio
                            select new
                            {
                                StageEntrances = new
                                {
                                    se.Id,
                                    se.Folio,
                                    se.Platforms,
                                    se.TotalPieces,
                                    se.EntryDate,
                                    se.ExitDate
                                }
                            };

                var results = await query.ToListAsync();

                if (!results.Any())
                {
                    return Ok(new List<object>());
                }

                var stageEntranceIds = results.Select(r => r.StageEntrances.Id).Distinct();

                var allPartNumbers = await _context.StageEntrancePartNumbers
                        .Where(pn => stageEntranceIds.Contains(pn.StageEntranceId))
                        .Select(pn => new
                        {
                            pn.StageEntranceId,
                            pn.PartNumber,
                            pn.Quantity
                        })
                        .ToListAsync();

                var groupedResults = results
                        .GroupBy(r => r.StageEntrances.Folio)
                        .Select(g => new
                        {
                            Folio = g.Key,
                            Entrances = g.Select(x => new
                            {
                                Folio = x.StageEntrances.Folio,
                                Platforms = x.StageEntrances.Platforms,
                                TotalPieces = x.StageEntrances.TotalPieces,
                                EntryDate = x.StageEntrances.EntryDate,
                                ExitDate = x.StageEntrances.ExitDate,
                                PartNumbers = allPartNumbers
                                        .Where(pn => pn.StageEntranceId == x.StageEntrances.Id)
                                        .Select(pn => new { pn.PartNumber, pn.Quantity })
                                        .ToList()
                            }).ToList(),
                            TotalPlatforms = g.Sum(x => x.StageEntrances.Platforms ?? 0),
                            TotalPieces = g.Sum(x => x.StageEntrances.TotalPieces ?? 0)
                        }).ToList();

                var allActivePartNumbers = await _context.StageEntrancePartNumbers
                   .Where(pn => _context.StageEntrances
                       .Where(se => se.ExitDate == null && se.Platforms > 0)
                       .Select(se => se.Id)
                       .Contains(pn.StageEntranceId))
                   .GroupBy(pn => pn.PartNumber)
                   .Select(g => new
                   {
                       PartNumber = g.Key,
                       TotalQuantity = g.Sum(pn => pn.Quantity)
                   })
                   .ToListAsync();

                var response = new
                {
                    FolioResults = groupedResults,
                    AccumulatedPartNumbers = allActivePartNumbers
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al buscar por folio",
                    details = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("Available-Reports")]
        public async Task<IActionResult> GetAvailableReports()
        {
            try
            {
                var reports = await _context.StageEntrances
                    .Where(r => r.CreatedAt != null)
                    .Select(r => new
                    {
                        Year = r.CreatedAt.Value.Year,
                        Month = r.CreatedAt!.Value.Month
                    })
                    .Distinct()
                    .OrderByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .ToListAsync();

                var result = reports.Select(r => new
                {
                    year = r.Year,
                    month = r.Month,
                    monthName = new DateTime(r.Year, r.Month, 1)
                        .ToString("MMMM", new CultureInfo("es-MX"))
                        .ToUpper()[0] +
                        new DateTime(r.Year, r.Month, 1)
                        .ToString("MMMM", new CultureInfo("es-MX"))
                        .Substring(1)
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error obteniendo reportes", details = ex.Message });
            }
        }


        [HttpGet]
        [Route("DownloadMonthlyReport/{year}/{month}")]
        public async Task<IActionResult> DownloadMonthlyReport(int year, int month)
        {
            try
            {
                var result = await GetMonthlyReports(year, month);

                if (result is OkObjectResult okResult && okResult.Value != null)
                {
                    var reportData = okResult.Value;

                    var excelBytes = _excelReportService.GenerateMonthlyReport(reportData);

                    var monthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
                    var fileName = $"Reporte_Mensual_{monthName}_{year}.xlsx";

                    return File(excelBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
                else
                {
                    return NotFound(new { message = "No se encontraron datos para el reporte" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al generar el archivo Excel",
                    details = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("ProcessExits")]
        public async Task<IActionResult> ProcessExits([FromBody] ProcessExitsDto request)
        {
            try
            {
                if (request?.ExitItem == null || !request.ExitItem.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No se proporcionaron items para procesar"
                    });
                }

                var results = new List<ExitProcessingResult>();
                var exitDate = DateTime.Now;

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var folios = request.ExitItem.Select(e => e.Folio).Distinct().ToList();

                    var stageEntrances = await _context.StageEntrances
                        .Where(se => folios.Contains(se.Folio!.Value) && se.Platforms > 0)
                        .Select(se => new { se.Id, se.Folio, se.Platforms })
                        .ToListAsync();

                    foreach (var exitItem in request.ExitItem)
                    {
                        var stageEntrance = stageEntrances.FirstOrDefault(se => se.Folio == exitItem.Folio);

                        if (stageEntrance == null)
                        {
                            results.Add(new ExitProcessingResult
                            {
                                Folio = exitItem.Folio.ToString(),
                                Success = false,
                                Message = $"Folio no encontrado o sin tarimas disponibles"
                            });
                            continue;
                        }

                        if (exitItem.Quantity > stageEntrance.Platforms)
                        {
                            results.Add(new ExitProcessingResult
                            {
                                Folio = exitItem.Folio.ToString(),
                                Success = false,
                                Message = $"Cantidad solicitada ({exitItem.Quantity}) excede las tarimas disponibles ({stageEntrance.Platforms})"
                            });
                            continue;
                        }

                        var previousPlatforms = stageEntrance.Platforms ?? 0;

                        var updateResult = await _context.StageEntrances
                            .Where(se => se.Id == stageEntrance.Id)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(se => se.Platforms, se => se.Platforms - exitItem.Quantity)
                                .SetProperty(se => se.ExitDate, exitDate)
                                .SetProperty(se => se.UpdatedAt, DateTime.Now)
                            );

                        var currentPlatforms = previousPlatforms - exitItem.Quantity;

                        results.Add(new ExitProcessingResult
                        {
                            Folio = exitItem.Folio.ToString(),
                            Success = true,
                            Message = $"Salida procesada: {exitItem.Quantity} tarimas",
                            PreviousPlatforms = previousPlatforms,
                            CurrentPlatforms = currentPlatforms
                        });
                    }

                    await transaction.CommitAsync();

                    var successfulExits = results.Count(r => r.Success);
                    var failedExits = results.Count(r => !r.Success);

                    return Ok(new
                    {
                        success = true,
                        message = $"Procesamiento completado: {successfulExits} exitosos, {failedExits} fallidos",
                        results = results,
                        exitDate = exitDate
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception("Error durante el procesamiento de salidas", ex);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al procesar las salidas",
                    detalles = ex.Message
                });
            }
        }


        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create([FromBody] StageEntrancesDto request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new
                {
                    Message = "Errores de validación",
                    Errors = errors
                });
            }

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var folioExists = await _context.StageEntrances
                    .AnyAsync(se => se.Folio == request.Folio);

                if (folioExists)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "El folio ya existe"
                    });
                }

                var totalPieces = request.PartNumbers.Sum(pn => pn.Quantity);

                var newStageEntrance = new StageEntrances
                {
                    Folio = request.Folio,
                    TotalPieces = totalPieces,
                    Platforms = 1,
                    IdStorageCost = 1,
                    IdEntranceFee = 1,
                    EntryDate = request.EntryDate,
                    ExitDate = null,
                    CreatedAt = DateTime.Now
                };

                _context.StageEntrances.Add(newStageEntrance);
                await _context.SaveChangesAsync();

                var partNumbers = request.PartNumbers.Select(pn => new StageEntrancePartNumbers
                {
                    StageEntranceId = newStageEntrance.Id,
                    PartNumber = pn.PartNumber.Trim(),
                    Quantity = pn.Quantity,
                    CreateAt = DateTime.Now
                }).ToList();

                await _context.StageEntrancePartNumbers.AddRangeAsync(partNumbers);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Registro creado exitosamente",
                    IdStageEntrances = newStageEntrance.Id
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error de base de datos",
                    Detail = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error interno del servidor",
                    Detail = ex.Message
                });
            }
        }

        [HttpPut]
        [Route("Update/{id}")]
        public async Task<IActionResult> Update([FromBody] StageEntrancesDto request, int id)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new
                {
                    Message = "Errores de validación",
                    Errors = errors
                });
            }

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var stageEntrance = await _context.StageEntrances
                    .FirstOrDefaultAsync(se => se.Id == id);

                if (stageEntrance == null)
                {
                    return NotFound("Registro no encontrado");
                }

                var folioExists = await _context.StageEntrances
                    .AnyAsync(se => se.Folio == request.Folio && se.Id != id);

                if (folioExists)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El folio ya existe en otro registro"
                    });
                }

                stageEntrance.Folio = request.Folio;
                stageEntrance.TotalPieces = request.PartNumbers.Sum(pn => pn.Quantity);
                stageEntrance.EntryDate = request.EntryDate;
                stageEntrance.UpdatedAt = DateTime.Now;

                var existingPartNumbers = await _context.StageEntrancePartNumbers
                    .Where(pn => pn.StageEntranceId == id)
                    .ToListAsync();

                _context.StageEntrancePartNumbers.RemoveRange(existingPartNumbers);

                var newPartNumbers = request.PartNumbers.Select(pn => new StageEntrancePartNumbers
                {
                    StageEntranceId = stageEntrance.Id,
                    PartNumber = pn.PartNumber.Trim(),
                    Quantity = pn.Quantity,
                    CreateAt = DateTime.Now
                }).ToList();

                await _context.StageEntrancePartNumbers.AddRangeAsync(newPartNumbers);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Actualizado correctamente",
                    IdModified = stageEntrance.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    detail = ex.Message
                });
            }
        }

        [HttpPatch]
        [Route("UpdateExits/{id}")]
        public async Task<IActionResult> UpdateExits([FromBody] StageExitsDto request, int id)
        {
            var stageEntrance = await _context.StageEntrances
                .Where(se => se.Id == id)
                .Select(se => new { se.Platforms })
                .FirstOrDefaultAsync();

            if (stageEntrance == null)
            {
                return NotFound("Registro no encontrado");
            }

            if (request.Platforms > stageEntrance.Platforms)
            {
                return BadRequest("El nuevo valor no puede ser mayor al anterior");
            }

            await _context.StageEntrances
                .Where(se => se.Id == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(se => se.Platforms, request.Platforms)
                    .SetProperty(se => se.ExitDate, request.ExitDate)
                    .SetProperty(se => se.UpdatedAt, DateTime.Now)
                );

            return Ok(new
            {
                success = true,
                message = "Registro de salida actualizado"
            });
        }

        [HttpDelete]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var IdStageEntrance = await _context.StageEntrances.FindAsync(id);

            if (IdStageEntrance == null)
            {
                return NotFound("Id no encontrado");
            }

            _context.StageEntrances.Remove(IdStageEntrance);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Registro eliminado"
            });
        }

        private async Task<IActionResult> GetMonthlyReports(int year, int month)
        {
            try
            {
                if (month < 1 || month > 12)
                {
                    return BadRequest(new
                    {
                        succes = false,
                        message = "El mes debe estar entre 1 y 12"
                    });
                }

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var monthlyData = await _context.StageEntrances
                        .Where(se => se.EntryDate.HasValue &&
                                    se.EntryDate.Value.Year == year &&
                                    se.EntryDate.Value.Month == month)
                        .Include(se => se.StageEntrancePartNumbers)
                        .OrderBy(se => se.EntryDate)
                        .Select(se => new
                        {
                            se.Id,
                            se.Folio,
                            se.Platforms,
                            se.TotalPieces,
                            se.EntryDate,
                            se.ExitDate,
                            PartNumbers = se.StageEntrancePartNumbers
                                .Select(pn => new { pn.PartNumber, pn.Quantity })
                                .ToList(),
                            EntranceCost = 67.50m,
                            ExitCost = 67.50m,
                            StorageCost = 133.00m
                        })
                        .ToListAsync();

                if (!monthlyData.Any())
                {
                    return Ok(new
                    {
                        Year = year,
                        Month = month,
                        MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                        TotalRecords = 0,
                        TotalPallets = 0,
                        ActiveRecords = 0,
                        CompleteRecords = 0,
                        TotalEntranceCost = 0m,
                        TotalExitCost = 0m,
                        TotalStorageCost = 0m,
                        TotalGeneralCost = 0m,
                        Records = new List<object>()
                    });
                }

                var records = new List<MonthlyReportRecord>();
                var totalEntranceCost = 0m;
                var totalExitCost = 0m;
                var totalStorageCost = 0m;

                foreach (var record in monthlyData)
                {
                    var daysInStorage = CalculateDaysInStorage(record.EntryDate, record.ExitDate);

                    var entranceCost = record.EntranceCost;
                    totalEntranceCost += entranceCost;

                    var exitCost = record.ExitDate.HasValue ? record.ExitCost : 0m;
                    totalExitCost += exitCost;

                    var storageCost = 0m;

                    if (!record.ExitDate.HasValue && DateTime.Now >= endDate)
                    {
                        storageCost = record.StorageCost;
                        totalStorageCost += storageCost;
                    }

                    var totalCost = entranceCost + exitCost + storageCost;

                    records.Add(new MonthlyReportRecord
                    {
                        Id = record.Id,
                        Folio = record.Folio ?? 0,
                        PartNumbers = string.Join(", ", record.PartNumbers.Select(pn => $"{pn.PartNumber}({pn.Quantity})")),
                        Pallets = record.Platforms ?? 0,
                        EntryDate = record.EntryDate?.ToString("dd-MM-yyyy") ?? "N/A",
                        ExitDate = record.ExitDate?.ToString("dd-MM-yyyy") ?? "Sin salir",
                        DaysInStorage = daysInStorage,
                        EntranceCost = entranceCost,
                        ExitCost = exitCost,
                        StorageCost = storageCost,
                        TotalCost = totalCost,
                    });
                }

                var totalRecords = monthlyData.Count;
                var totalPallets = monthlyData.Sum(x => x.Platforms ?? 0);
                var activeRecords = monthlyData.Count(x => !x.ExitDate.HasValue);
                var completedRecords = monthlyData.Count(x => x.ExitDate.HasValue);
                var totalGeneralCost = totalEntranceCost + totalExitCost + totalStorageCost;

                var response = new
                {
                    Year = year,
                    Month = month,
                    MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                    TotalRecords = totalRecords,
                    TotalPallets = totalPallets,
                    ActiveRecords = activeRecords,
                    CompleteRecords = completedRecords,
                    TotalEntranceCost = totalEntranceCost,
                    TotalExitCost = totalExitCost,
                    TotalStorageCost = totalStorageCost,
                    TotalGeneralCost = totalGeneralCost,
                    Records = records
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al generar el reporte mensual",
                    details = ex.Message
                });
            }
        }

        private int CalculateDaysInStorage(DateTime? entryDate, DateTime? exitDate)
        {
            if (!entryDate.HasValue) return 0;

            var exitDateTime = exitDate ?? DateTime.Now;
            var diff = exitDateTime - entryDate.Value;

            return (int)Math.Ceiling(diff.TotalDays);
        }
    }
}