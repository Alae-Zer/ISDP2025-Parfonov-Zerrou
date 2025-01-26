using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace ISDP2025_Parfonov_Zerrou.Models;

public partial class BestContext : DbContext
{
    public BestContext()
    {
    }

    public BestContext(DbContextOptions<BestContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Delivery> Deliveries { get; set; }

    public virtual DbSet<Deliverymethod> Deliverymethods { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Posn> Posns { get; set; }

    public virtual DbSet<Province> Provinces { get; set; }

    public virtual DbSet<Site> Sites { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<Txn> Txns { get; set; }

    public virtual DbSet<Txnaudit> Txnaudits { get; set; }

    public virtual DbSet<Txnitem> Txnitems { get; set; }

    public virtual DbSet<Txnstatus> Txnstatuses { get; set; }

    public virtual DbSet<Txntype> Txntypes { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;database=bullseyedb2025;user=admin;password=admin", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.4.3-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryName).HasName("PRIMARY");

            entity.Property(e => e.Active).HasDefaultValueSql("'1'");
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.DeliveryId).HasName("PRIMARY");

            entity.HasOne(d => d.VehicleTypeNavigation).WithMany(p => p.Deliveries)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("delivery_ibfk_1");
        });

        modelBuilder.Entity<Deliverymethod>(entity =>
        {
            entity.HasKey(e => e.DeliveryMethodId).HasName("PRIMARY");

            entity.Property(e => e.Active).HasDefaultValueSql("'1'");

            entity.HasOne(d => d.Province).WithMany(p => p.Deliverymethods)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("deliverymethod_ibfk_1");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeID).HasName("PRIMARY");

            entity.Property(e => e.Active).HasDefaultValueSql("'1'");
            entity.Property(e => e.Locked).HasDefaultValueSql("'0'");

            entity.HasOne(d => d.Position).WithMany(p => p.Employees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("employee_ibfk_1");

            entity.HasOne(d => d.Site).WithMany(p => p.Employees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("employee_ibfk_2");

            entity.HasMany(d => d.Permissions).WithMany(p => p.Employees)
                .UsingEntity<Dictionary<string, object>>(
                    "Additionalpermissionsmapping",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("additionalpermissionsmapping_ibfk_2"),
                    l => l.HasOne<Employee>().WithMany()
                        .HasForeignKey("EmployeeId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("additionalpermissionsmapping_ibfk_1"),
                    j =>
                    {
                        j.HasKey("EmployeeId", "PermissionId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("additionalpermissionsmapping");
                        j.HasIndex(new[] { "PermissionId" }, "permissionID");
                        j.IndexerProperty<int>("EmployeeId").HasColumnName("employeeID");
                        j.IndexerProperty<int>("PermissionId").HasColumnName("permissionID");
                    });
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => new { e.ItemId, e.SiteId, e.ItemLocation })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

            entity.Property(e => e.ItemLocation).HasDefaultValueSql("'Stock'");

            entity.HasOne(d => d.Item).WithMany(p => p.Inventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_ibfk_1");

            entity.HasOne(d => d.Site).WithMany(p => p.Inventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_ibfk_2");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PRIMARY");

            entity.Property(e => e.Active).HasDefaultValueSql("'1'");

            entity.HasOne(d => d.CategoryNavigation).WithMany(p => p.Items)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("item_ibfk_2");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Items)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("item_ibfk_1");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PRIMARY");

            entity.Property(e => e.PermissionId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Posn>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("PRIMARY");

            entity.Property(e => e.Active).HasDefaultValueSql("'1'");
        });

        modelBuilder.Entity<Province>(entity =>
        {
            entity.HasKey(e => e.ProvinceId).HasName("PRIMARY");

            entity.Property(e => e.Active).HasDefaultValueSql("'1'");
        });

        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasKey(e => e.SiteId).HasName("PRIMARY");

            entity.Property(e => e.Active).HasDefaultValueSql("'1'");

            entity.HasOne(d => d.Province).WithMany(p => p.Sites)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("site_ibfk_1");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PRIMARY");

            entity.Property(e => e.Active).HasDefaultValueSql("'1'");

            entity.HasOne(d => d.ProvinceNavigation).WithMany(p => p.Suppliers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("supplier_ibfk_1");
        });

        modelBuilder.Entity<Txn>(entity =>
        {
            entity.HasKey(e => e.TxnId).HasName("PRIMARY");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Delivery).WithMany(p => p.Txns).HasConstraintName("txn_ibfk_6");

            entity.HasOne(d => d.Employee).WithMany(p => p.Txns)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("txn_ibfk_1");

            entity.HasOne(d => d.SiteIdfromNavigation).WithMany(p => p.TxnSiteIdfromNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("txn_ibfk_3");

            entity.HasOne(d => d.SiteIdtoNavigation).WithMany(p => p.TxnSiteIdtoNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("txn_ibfk_2");

            entity.HasOne(d => d.TxnStatusNavigation).WithMany(p => p.Txns)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("txn_ibfk_4");

            entity.HasOne(d => d.TxnTypeNavigation).WithMany(p => p.Txns)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("txn_ibfk_5");
        });

        modelBuilder.Entity<Txnaudit>(entity =>
        {
            entity.HasKey(e => e.TxnAuditId).HasName("PRIMARY");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Delivery).WithMany(p => p.Txnaudits).HasConstraintName("txnaudit_ibfk_4");

            entity.HasOne(d => d.Employee).WithMany(p => p.Txnaudits)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("txnaudit_ibfk_1");

            entity.HasOne(d => d.Site).WithMany(p => p.Txnaudits)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("txnaudit_ibfk_3");

            entity.HasOne(d => d.TxnTypeNavigation).WithMany(p => p.Txnaudits)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("txnaudit_ibfk_2");
        });

        modelBuilder.Entity<Txnitem>(entity =>
        {
            entity.HasKey(e => new { e.TxnId, e.ItemId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.HasOne(d => d.Item).WithMany(p => p.Txnitems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("txnitems_ibfk_2");

            entity.HasOne(d => d.Txn).WithMany(p => p.Txnitems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("txnitems_ibfk_1");
        });

        modelBuilder.Entity<Txnstatus>(entity =>
        {
            entity.HasKey(e => e.StatusName).HasName("PRIMARY");

            entity.Property(e => e.Active).HasDefaultValueSql("'1'");
        });

        modelBuilder.Entity<Txntype>(entity =>
        {
            entity.HasKey(e => e.TxnType1).HasName("PRIMARY");

            entity.Property(e => e.Active).HasDefaultValueSql("'1'");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleType).HasName("PRIMARY");

            entity.Property(e => e.Active).HasDefaultValueSql("'1'");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
