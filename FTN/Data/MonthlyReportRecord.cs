namespace FTN.Data
{
    public class MonthlyReportRecord
    {
        public int Id { get; set; }
        public int Folio { get; set; }
        public string PartNumbers { get; set; } = string.Empty;
        public int Pallets { get; set; }
        public string EntryDate { get; set; } = string.Empty;
        public string ExitDate { get; set; } = string.Empty;
        public int DaysInStorage { get; set; }
        public decimal EntranceCost { get; set; }
        public decimal ExitCost { get; set; }
        public decimal StorageCost { get; set; }
        public decimal TotalCost { get; set; }
    }
}