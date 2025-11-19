using System;
using System.Collections.Generic;

namespace FTN.Models;

public partial class StorageCost
{
    public int Id { get; set; }

    public int? Cost { get; set; }

    public virtual ICollection<StageEntrances> StageEntrances { get; set; } = new List<StageEntrances>();
}