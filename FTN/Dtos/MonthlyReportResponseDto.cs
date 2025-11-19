using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client.Extensions.Msal;

namespace FTN.Dtos
{
    public class MonthlyReportResponseDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPallets { get; set; }
        public int ActiveRecords { get; set; }
        public int CompletedRecords { get; set; }
        public decimal TotalEntryCost { get; set; }
        public decimal TotalExitCost { get; set; }
        public decimal TotalStorageCost { get; set; }
        public decimal TotalGeneralCost { get; set; }
        public List<RecordDetailDto> Records { get; set; }
    }
}