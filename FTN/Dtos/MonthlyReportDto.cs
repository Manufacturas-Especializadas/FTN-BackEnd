namespace FTN.Dtos
{
    public class MonthlyReportDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int TotalPallets { get; set; }
        public int ActiveRecords { get; set; }
        public int CompleteRecords { get; set; }
        public decimal TotalEntranceCost { get; set; }
        public decimal TotalExitCost { get; set; }
        public decimal TotalStorageCost { get; set; }
        public decimal TotalGeneralCost { get; set; }
        public List<MonthlyRecordDto> Records { get; set; } = new();
    }
}