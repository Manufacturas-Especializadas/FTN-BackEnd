namespace FTN.Dtos
{
    public class MonthlyReportDto
    {
        public int Id { get; set; }
        public int? Folio { get; set; }
        public string PartNumbers { get; set; } = string.Empty;
        public int? Platforms { get; set; }
        public int? TotalPieces { get; set; }
        public DateTime? EntryDate { get; set; }
        public DateTime? ExitDate { get; set; }
        public DateTime? CreatedAt { get; set; }


        public int DaysInStorage { get; set; }
        public decimal EntranceCost { get; set; }
        public decimal ExitCost { get; set; }
        public decimal StorageCost { get; set; }
        public decimal TotalCost { get; set; }
        public int Pallets { get; set; }
    }
}