using System;
using System.Collections.Generic;

namespace FTN.Models;

public partial class EntranceFee
{
    public int Id { get; set; }

    public decimal? Cost { get; set; }

    public virtual ICollection<StageEntrances> StageEntrances { get; set; } = new List<StageEntrances>();
}