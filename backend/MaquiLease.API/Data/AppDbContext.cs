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
        public DbSet<ClientSector> ClientSectors { get; set; } = null!;
        public DbSet<AssetCategory> AssetCategories { get; set; } = null!;
        public DbSet<AssetBrand> AssetBrands { get; set; } = null!;
        public DbSet<ServiceCategory> ServiceCategories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique index for catalogs
            modelBuilder.Entity<ClientSector>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<AssetCategory>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<AssetBrand>()
                .HasIndex(b => b.Name)
                .IsUnique();

            modelBuilder.Entity<ServiceCategory>()
                .HasIndex(c => c.Name)
                .IsUnique();

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

        public override int SaveChanges()
        {
            ResolveCatalogKeys();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ResolveCatalogKeys();
            return base.SaveChangesAsync(cancellationToken);
        }

        private static string NormalizeString(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant().Replace(" ", "_");
        }

        private void ResolveCatalogKeys()
        {
            // 1. Resolve Sectors for Clients
            var clients = ChangeTracker.Entries<Client>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            foreach (var client in clients)
            {
                if (string.IsNullOrWhiteSpace(client.Sector))
                {
                    client.ClientSectorId = null;
                    client.ClientSector = null;
                    continue;
                }

                var sectorName = client.Sector.Trim();
                var sectorNormalizedName = NormalizeString(sectorName);

                var sector = ClientSectors.Local.FirstOrDefault(s => s.Name == sectorNormalizedName || s.Label.ToLower() == sectorName.ToLower())
                             ?? ClientSectors.FirstOrDefault(s => s.Name == sectorNormalizedName || s.Label.ToLower() == sectorName.ToLower());

                if (sector == null)
                {
                    sector = new ClientSector
                    {
                        Name = sectorNormalizedName,
                        Label = char.ToUpper(sectorName[0]) + sectorName.Substring(1)
                    };
                    ClientSectors.Add(sector);
                }
                client.ClientSector = sector;
                client.Sector = sector.Name;
            }

            // 2. Resolve Categories & Brands for Assets
            var assets = ChangeTracker.Entries<Asset>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            foreach (var asset in assets)
            {
                // Category
                if (string.IsNullOrWhiteSpace(asset.Category))
                {
                    asset.AssetCategoryId = null;
                    asset.AssetCategory = null;
                }
                else
                {
                    var catName = asset.Category.Trim();
                    var catNormalized = NormalizeString(catName);

                    var cat = AssetCategories.Local.FirstOrDefault(c => c.Name == catNormalized || c.Label.ToLower() == catName.ToLower())
                              ?? AssetCategories.FirstOrDefault(c => c.Name == catNormalized || c.Label.ToLower() == catName.ToLower());
                    if (cat == null)
                    {
                        cat = new AssetCategory
                        {
                            Name = catNormalized,
                            Label = char.ToUpper(catName[0]) + catName.Substring(1)
                        };
                        AssetCategories.Add(cat);
                    }
                    asset.AssetCategory = cat;
                    asset.Category = cat.Name;
                }

                // Brand
                if (string.IsNullOrWhiteSpace(asset.Brand))
                {
                    asset.AssetBrandId = null;
                    asset.AssetBrand = null;
                }
                else
                {
                    var brandName = asset.Brand.Trim();
                    var brandNormalized = NormalizeString(brandName);

                    var brand = AssetBrands.Local.FirstOrDefault(b => NormalizeString(b.Name) == brandNormalized || b.Label.ToLower() == brandName.ToLower())
                                ?? AssetBrands.FirstOrDefault(b => NormalizeString(b.Name) == brandNormalized || b.Label.ToLower() == brandName.ToLower());
                    if (brand == null)
                    {
                        brand = new AssetBrand
                        {
                            Name = char.ToUpper(brandName[0]) + brandName.Substring(1),
                            Label = char.ToUpper(brandName[0]) + brandName.Substring(1)
                        };
                        AssetBrands.Add(brand);
                    }
                    asset.AssetBrand = brand;
                    asset.Brand = brand.Name;
                }
            }

            // 3. Resolve Categories for Services
            var services = ChangeTracker.Entries<Service>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            foreach (var svc in services)
            {
                if (string.IsNullOrWhiteSpace(svc.Category))
                {
                    svc.ServiceCategoryId = null;
                    svc.ServiceCategory = null;
                    continue;
                }

                var catName = svc.Category.Trim();
                var catNormalized = NormalizeString(catName);

                var cat = ServiceCategories.Local.FirstOrDefault(c => c.Name == catNormalized || c.Label.ToLower() == catName.ToLower())
                          ?? ServiceCategories.FirstOrDefault(c => c.Name == catNormalized || c.Label.ToLower() == catName.ToLower());
                if (cat == null)
                {
                    cat = new ServiceCategory
                    {
                        Name = catNormalized,
                        Label = char.ToUpper(catName[0]) + catName.Substring(1)
                    };
                    ServiceCategories.Add(cat);
                }
                svc.ServiceCategory = cat;
                svc.Category = cat.Name;
            }
        }
    }
}
