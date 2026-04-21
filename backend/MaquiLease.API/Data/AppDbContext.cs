using MaquiLease.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Asset> Assets { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Contract> Contracts { get; set; } = null!;
        public DbSet<Installment> Installments { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Alert> Alerts { get; set; } = null!;
        public DbSet<PredictionLog> PredictionLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relación Contract -> Client
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Client)
                .WithMany(cl => cl.Contracts)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Contract -> Asset
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Asset)
                .WithMany(a => a.Contracts)
                .HasForeignKey(c => c.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Contract -> Service
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Service)
                .WithMany(s => s.Contracts)
                .HasForeignKey(c => c.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Installment -> Contract
            modelBuilder.Entity<Installment>()
                .HasOne(i => i.Contract)
                .WithMany(c => c.Installments)
                .HasForeignKey(i => i.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación Payment -> Installment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Installment)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InstallmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación Alert -> Contract
            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Contract)
                .WithMany(c => c.Alerts)
                .HasForeignKey(a => a.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.RUC)
                .IsUnique();

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.Code)
                .IsUnique();

            modelBuilder.Entity<Service>()
                .HasIndex(s => s.Code)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
