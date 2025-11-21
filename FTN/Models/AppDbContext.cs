using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FTN.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<EntranceFee> EntranceFee { get; set; }

    public virtual DbSet<StageEntrancePartNumbers> StageEntrancePartNumbers { get; set; }

    public virtual DbSet<StageEntrances> StageEntrances { get; set; }

    public virtual DbSet<StorageCost> StorageCost { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntranceFee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Entrance__3214EC0734FD4A0B");

            entity.Property(e => e.Cost)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("cost");
        });

        modelBuilder.Entity<StageEntrancePartNumbers>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StageEnt__3214EC0733403153");

            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("create_At");
            entity.Property(e => e.PartNumber)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("part_Number");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.StageEntranceId).HasColumnName("stage_entrance_id");

            entity.HasOne(d => d.StageEntrance).WithMany(p => p.StageEntrancePartNumbers)
                .HasForeignKey(d => d.StageEntranceId)
                .HasConstraintName("FK__StageEntr__stage__6E01572D");
        });

        modelBuilder.Entity<StageEntrances>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StageEnt__3214EC0784E6F172");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EntryDate)
                .HasColumnType("datetime")
                .HasColumnName("entry_date");
            entity.Property(e => e.ExitDate)
                .HasColumnType("datetime")
                .HasColumnName("exit_date");
            entity.Property(e => e.Folio).HasColumnName("folio");
            entity.Property(e => e.PartNumbers)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasColumnName("partNumber");
            entity.Property(e => e.Platforms).HasColumnName("platforms");
            entity.Property(e => e.TotalPieces).HasColumnName("total_pieces");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_At");

            entity.HasOne(d => d.IdEntranceFeeNavigation).WithMany(p => p.StageEntrances)
                .HasForeignKey(d => d.IdEntranceFee)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__StageEntr__IdEnt__6A30C649");

            entity.HasOne(d => d.IdStorageCostNavigation).WithMany(p => p.StageEntrances)
                .HasForeignKey(d => d.IdStorageCost)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__StageEntr__IdSto__693CA210");
        });

        modelBuilder.Entity<StorageCost>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StorageC__3214EC077901FBF1");

            entity.Property(e => e.Cost).HasColumnName("cost");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}