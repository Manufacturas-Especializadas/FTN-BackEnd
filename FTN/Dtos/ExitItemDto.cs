using System.ComponentModel.DataAnnotations;

namespace FTN.Dtos
{
    public class ExitItemDto
    {
        public int Folio { get; set; }

        public string PartNumber { get; set; } = string.Empty;

        public int Quantity { get; set; } 

    }
}