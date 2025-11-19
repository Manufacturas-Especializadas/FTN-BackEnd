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

    public virtual DbSet<StageEntrances> StageEntrances { get; set; }

    public virtual DbSet<StorageCost> StorageCost { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntranceFee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Entrance__3214EC07A7EAA5F9");

            entity.Property(e => e.Cost)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("cost");
        });

        modelBuilder.Entity<StageEntrances>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StageEnt__3214EC07685A44BC");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.EntryDate)
                .HasColumnType("datetime")
                .HasColumnName("entryDate");
            entity.Property(e => e.ExitDate)
                .HasColumnType("datetime")
                .HasColumnName("exitDate");
            entity.Property(e => e.Folio)
                .HasMaxLength(100)
                .HasColumnName("folio");
            entity.Property(e => e.NumberOfPieces).HasColumnName("numberOfPieces");
            entity.Property(e => e.PartNumber)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasColumnName("partNumber");
            entity.Property(e => e.Platforms).HasColumnName("platforms");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updatedAt");

            entity.HasOne(d => d.IdEntranceFeeNavigation).WithMany(p => p.StageEntrances)
                .HasForeignKey(d => d.IdEntranceFee)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__StageEntr__IdEnt__5CD6CB2B");

            entity.HasOne(d => d.IdStorageCostNavigation).WithMany(p => p.StageEntrances)
                .HasForeignKey(d => d.IdStorageCost)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__StageEntr__IdSto__5BE2A6F2");
        });

        modelBuilder.Entity<StorageCost>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StorageC__3214EC0703916003");

            entity.Property(e => e.Cost).HasColumnName("cost");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}