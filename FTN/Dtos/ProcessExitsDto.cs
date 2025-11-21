using System.ComponentModel.DataAnnotations;

namespace FTN.Dtos
{
    public class ProcessExitsDto
    {
        [Required]
        public List<ExitItemDto> ExitItem { get; set; }
    }
}