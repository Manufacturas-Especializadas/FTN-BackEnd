using System;
using System.Collections.Generic;

namespace FTN.Models;

public partial class StageEntrances
{
    public int Id { get; set; }

    public int? Folio { get; set; }

    public string PartNumbers { get; set; }

    public int? Platforms { get; set; }

    public int? TotalPieces { get; set; }

    public int? IdStorageCost { get; set; }

    public int? IdEntranceFee { get; set; }

    public DateTime? EntryDate { get; set; }

    public DateTime? ExitDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual EntranceFee IdEntranceFeeNavigation { get; set; }

    public virtual StorageCost IdStorageCostNavigation { get; set; }

    public virtual ICollection<StageEntrancePartNumbers> StageEntrancePartNumbers { get; set; } = new List<StageEntrancePartNumbers>();
}