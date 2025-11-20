using FluentValidation;
using FTN.Dtos;
using FTN.Models;
using FTN.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
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
            var stageEntrance = await _context.StageEntrances
                            .AsNoTracking()
                            .Select(s => new
                            {
                                s.Id,
                                s.Folio,
                                s.PartNumber,
                                s.Platforms,
                                s.NumberOfPieces,
                                s.EntryDate,
                                EntranceFee = s.IdEntranceFeeNavigation.Cost,
                                StorageCost = s.IdStorageCostNavigation.Cost,
                            })
                            .ToListAsync();

            return Ok(stageEntrance);
        }

        [HttpGet]
        [Route("GetStageEntranceById/{id}")]
        public async Task<IActionResult> GetStageEntranceById(int id)
        {
            var stageEntrance = await _context.StageEntrances.FindAsync(id);

            if(stageEntrance == null)
            {
                NotFound(new { message = "Registro no encontrado" });
            }

            return Ok(stageEntrance);
        }

        [HttpGet]
        [Route("SearchByPartNumber/{partNumber}")]
        public async Task<IActionResult> SearchByPartNumber(string partNumber)
        {
            try
            {
                var results = await _context.StageEntrances
                        .Where(se => se.PartNumber.Contains(partNumber) && se.Platforms > 0)
                        .Select(se => new
                        {
                            se.Id,
                            se.Folio,
                            se.PartNumber,
                            se.Platforms,
                            se.NumberOfPieces,
                            se.EntryDate,
                            se.ExitDate,
                        })
                        .AsNoTracking()
                        .ToListAsync();

                var groupedResults = results
                        .SelectMany(se => se.PartNumber.Split(',')
                            .Select(pn => new
                            {
                                PartNumber = pn.Trim(),
                                se.Id,
                                se.Folio,
                                se.Platforms,
                                se.NumberOfPieces,
                                se.EntryDate,
                                se.ExitDate
                            }))
                        .Where(x => x.PartNumber.Contains(partNumber))
                        .GroupBy(x => x.PartNumber)
                        .Select(g => new
                        {
                            PartNumber = g.Key,
                            Folios = g.Select(x => new
                            {
                                x.Folio,
                                x.PartNumber,
                                x.NumberOfPieces,
                                x.EntryDate,
                                x.ExitDate
                            }).ToList(),
                            TotalPlatforms = g.Sum(x => x.Platforms ?? 0),
                            TotalPieces = g.Sum(x => x.NumberOfPieces ?? 0)
                        })
                        .ToList();

                return Ok(groupedResults);
            }
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al buscar por número de parte",
                    detalles = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("MonthlyReport/{year}/{month}")]
        public async Task<IActionResult> GetMonthlyReport(int year, int month)
        {
            try
            {
                if(month < 1 || month > 12)
                {
                    return BadRequest(new { message = "El mes debe se entre 1 y 12" });
                }

                if(year < 2000 || year > 2100)
                {
                    return BadRequest(new { message = "El año debe ser entre los 2000 y 2100" });
                }

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var monthlyRecords = await _context.StageEntrances
                        .Where(se => se.EntryDate >= startDate && se.EntryDate <= endDate)
                        .Include(se => se.IdEntranceFeeNavigation)
                        .Include(se => se.IdStorageCostNavigation)
                        .AsNoTracking()
                        .ToListAsync();

                if (!monthlyRecords.Any())
                {
                    return Ok(new MonthlyReportResponseDto
                    {
                        Year = year,
                        Month = month,
                        MonthName = GetMonthName(month),
                        TotalRecords = 0,
                        TotalPallets = 0,
                        ActiveRecords = 0,
                        CompletedRecords = 0,
                        TotalEntryCost = 0,
                        TotalExitCost = 0,
                        TotalStorageCost = 0,
                        TotalGeneralCost = 0,
                        Records = new List<RecordDetailDto>()
                    });
                }

                var recordsWithMetrics = monthlyRecords.Select(se => new RecordDetailDto
                {
                    Id = se.Id,
                    Folio = se.Folio,
                    PartNumber = se.PartNumber,
                    Pallets = se.Platforms ?? 0,
                    EntryDate = se.EntryDate ?? DateTime.MinValue,
                    ExitDate = se.ExitDate,
                    DaysInStorage = CalculateDaysInStorage(se.EntryDate, se.ExitDate),
                    EntryCost = se.IdEntranceFeeNavigation?.Cost ?? 67.50m,
                    ExitCost = se.ExitDate.HasValue ? (se.IdEntranceFeeNavigation?.Cost ?? 67.50m) : 0,
                    StorageCost = CalculateStorageCost(
                        se.EntryDate,
                        se.ExitDate,
                        se.IdStorageCostNavigation?.Cost ?? 133,
                        se.Platforms ?? 0
                    ),
                    TotalCost = CalculateTotalCost(
                        se.IdEntranceFeeNavigation?.Cost ?? 67.50m,
                        se.ExitDate.HasValue ? (se.IdEntranceFeeNavigation?.Cost ?? 67.50m) : 0,
                        se.EntryDate,
                        se.ExitDate,
                        se.IdStorageCostNavigation?.Cost ?? 133,
                        se.Platforms ?? 0
                    )
                }).ToList();

                var report = new MonthlyReportResponseDto
                {
                    Year = year,
                    Month = month,
                    MonthName = GetMonthName(month),
                    TotalRecords = recordsWithMetrics.Count,
                    TotalPallets = recordsWithMetrics.Sum(r => r.Pallets.Value),
                    ActiveRecords = recordsWithMetrics.Count(r => !r.ExitDate.HasValue),
                    CompletedRecords = recordsWithMetrics.Count(r => r.ExitDate.HasValue),
                    TotalEntryCost = recordsWithMetrics.Sum(r => r.EntryCost),
                    TotalExitCost = recordsWithMetrics.Sum(r => r.ExitCost),
                    TotalStorageCost = recordsWithMetrics.Sum(r => r.StorageCost),
                    TotalGeneralCost = recordsWithMetrics.Sum(r => r.TotalCost),
                    Records = recordsWithMetrics
                };

                return Ok(report);
            }
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al generar el reporte mensual",
                    detalles = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("MonthlyReportExcel/{year}/{month}")]
        public async Task<IActionResult> DownloadMonthlyReportExcel(int year, int month)
        {
            try
            {
                if(month < 1 || month > 12)
                {
                    return BadRequest(new { message = "El mes debe ser entre 1 y 12" });
                }

                if(year < 2000 || year > 2100)
                {
                    return BadRequest(new { message = "El año debe estar entre los 2000 y 2100" });
                }

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var monthlyRecords = await _context.StageEntrances
                        .Where(se => se.EntryDate >= startDate && se.EntryDate <= endDate)
                        .Include(se => se.IdEntranceFeeNavigation)
                        .Include(se => se.IdStorageCostNavigation)
                        .AsNoTracking()
                        .ToListAsync();

                if (!monthlyRecords.Any())
                {
                    return NotFound(new { message = "No se encontraron registros para el período especificado" });
                }

                var recordsWithMetrics = monthlyRecords.Select(se => new RecordDetailDto
                {
                    Id = se.Id,
                    Folio = se.Folio,
                    PartNumber = se.PartNumber,
                    Pallets = se.Platforms,
                    EntryDate = se.EntryDate ?? DateTime.MinValue,
                    ExitDate = se.ExitDate,
                    DaysInStorage = CalculateDaysInStorage(se.EntryDate, se.ExitDate),
                    EntryCost = se.IdEntranceFeeNavigation?.Cost ?? 67.50m,
                    ExitCost = se.ExitDate.HasValue ? (se.IdEntranceFeeNavigation?.Cost ?? 67.50m) : 0,
                    StorageCost = CalculateStorageCost(
                        se.EntryDate,
                        se.ExitDate,
                        se.IdStorageCostNavigation?.Cost ?? 133,
                        se.Platforms ?? 0
                    ),
                    TotalCost = CalculateTotalCost(
                        se.IdEntranceFeeNavigation?.Cost ?? 67.50m,
                        se.ExitDate.HasValue ? (se.IdEntranceFeeNavigation?.Cost ?? 67.50m) : 0,
                        se.ExitDate,
                        se.ExitDate,
                        se.IdStorageCostNavigation?.Cost ?? 133,
                        se.Platforms ?? 0
                    )
                }).ToList();

                var reportData = new MonthlyReportResponseDto
                {
                    Year = year,
                    Month = month,
                    MonthName = GetMonthName(month),
                    TotalRecords = recordsWithMetrics.Count,
                    TotalPallets = recordsWithMetrics.Sum(r => r.Pallets.Value),
                    ActiveRecords = recordsWithMetrics.Count(r => !r.ExitDate.HasValue),
                    CompletedRecords = recordsWithMetrics.Count(r => r.ExitDate.HasValue),
                    TotalEntryCost = recordsWithMetrics.Sum(r => r.EntryCost),
                    TotalExitCost = recordsWithMetrics.Sum(r => r.ExitCost),
                    TotalStorageCost = recordsWithMetrics.Sum(r => r.StorageCost),
                    TotalGeneralCost = recordsWithMetrics.Sum(r => r.TotalCost),
                    Records = recordsWithMetrics
                };

                var excelBytes = _excelReportService.GenerateMonthlyReport(reportData);

                var fileName = $"Reporte_{reportData.MonthName}_{reportData.Year}.xlsx";

                return File(excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al generar el reporte de excel",
                    detalles = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("AvailableYears")]
        public async Task<IActionResult> GetAvailableYear()
        {
            try
            {
                var years = await _context.StageEntrances
                        .Where(se => se.EntryDate.HasValue)
                        .Select(se => se.EntryDate.Value.Year)
                        .Distinct()
                        .OrderByDescending(year => year)
                        .ToListAsync();

                return Ok(years);
            }
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener los años disponibles",
                    detalles = ex.Message
                });
            }
        }

        private int CalculateDaysInStorage(DateTime? entryDate, DateTime? exitDate)
        {
            if(!entryDate.HasValue) return 0;

            var exitDateTime = exitDate ?? DateTime.Now;
            var diff = exitDateTime - entryDate.Value;

            return (int)Math.Ceiling(diff.TotalDays);
        }

        private decimal CalculateStorageCost(DateTime? entryDate, DateTime? exitDate, int? dailyCost, int pallets)
        {
            if(!entryDate.HasValue || dailyCost == null ) return 0;

            var days = CalculateDaysInStorage(entryDate, exitDate);

            return days * (dailyCost.Value * pallets);
        }

        private decimal CalculateTotalCost(decimal entryCost, decimal exitCost, DateTime? entryDate, DateTime? exitDate, int? dailyCost, int pallets)
        {
            var storageCost = CalculateStorageCost(entryDate, exitDate, dailyCost, pallets);

            return entryCost + exitCost + storageCost;
        }

        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "Enero",
                2 => "Febrero",
                3 => "Marzo",
                4 => "Abril",
                5 => "Mayo",
                6 => "Junio",
                7 => "Julio",
                8 => "Agosto",
                9 => "Septiembre",
                10 => "Octubre",
                11 => "Noviembre",
                12 => "Diciembre",
                _ => "Mes invalido"
            };
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
                var newStageEntrance = new StageEntrances
                {
                    Folio = request.Folio,
                    PartNumber = request.PartNumber,
                    Platforms = 1,
                    NumberOfPieces = request.NumberOfPieces,
                    IdStorageCost = 1,
                    IdEntranceFee = 1,
                    EntryDate = request.EntryDate,
                    ExitDate = null, 
                };

                _context.StageEntrances.Add(newStageEntrance);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Succes = true,
                    Message = "Registro creado",
                    IdStageEntrances = newStageEntrance.Id
                });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    Succees = false,
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
                var stageEntrance = await _context.StageEntrances.FindAsync(id);

                if(stageEntrance == null)
                {
                    return NotFound("Id no encontrado");
                }

                stageEntrance.Folio = request.Folio;
                stageEntrance.PartNumber = request.PartNumber;
                stageEntrance.NumberOfPieces = request.NumberOfPieces;
                stageEntrance.EntryDate = request.EntryDate;
                stageEntrance.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Actualizado correctamente",
                    IdModified = stageEntrance.Id
                });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor"
                });
            }
        }

        [HttpPatch]
        [Route("UpdateExits/{id}")]
        public async Task<IActionResult> UpdateExits([FromBody] StageExitsDto request, int id)
        {
            var stageEntrance = await _context.StageEntrances.FindAsync(id);

            if(stageEntrance == null)
            {
                return NotFound("Registro no encontrado");
            }

            if(request.Platforms > stageEntrance.Platforms)
            {
                return BadRequest("El nuevo valor no puede ser mayor al anterior");
            }

            stageEntrance.Platforms = request.Platforms;
            stageEntrance.ExitDate = request.ExitDate;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Registro de salida creado"
            });
        } 

        [HttpDelete]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var IdStageEntrance = await _context.StageEntrances.FindAsync(id);

            if(IdStageEntrance == null)
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
    }
}