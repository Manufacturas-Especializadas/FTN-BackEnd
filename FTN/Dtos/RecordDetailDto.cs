namespace FTN.Dtos
{
    public class RecordDetailDto
    {
        public int Id { get; set; }
        public int? Folio { get; set; }
        public string PartNumber { get; set; }
        public int? Pallets { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? ExitDate { get; set; }
        public int DaysInStorage { get; set; }
        public decimal EntryCost { get; set; }
        public decimal ExitCost { get; set; }
        public decimal StorageCost { get; set; }
        public decimal TotalCost { get; set; }
        public bool IsActive => !ExitDate.HasValue;
        public string Status => IsActive ? "Active" : "Completed";
    }
}