namespace FTN.Dtos
{
    public class StageEntrancesDto
    {
        public int? Folio { get; set; }

        public DateTime? EntryDate { get; set; }

        public List<PartNumberDto> PartNumbers { get; set; } = new List<PartNumberDto>();
    }
}