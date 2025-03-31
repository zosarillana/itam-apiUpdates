using ITAM.Models.Approval;
using ITAM.Models.Logs;
using ITAM.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ITAM.DataContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Asset_logs> asset_Logs { get; set; }
        public DbSet<Computer_logs> computer_Logs { get; set; }
        public DbSet<User_logs> user_logs { get; set; }
        public DbSet<UserAccountabilityList> user_accountability_lists { get; set; }
        public DbSet<ComputerComponents> computer_components { get; set; }
        public DbSet<Computer> computers { get; set; }
        public DbSet<Computer_components_logs> computer_components_logs { get; set; }
        public DbSet<CentralizedLogs> centralized_logs { get; set; }
        public DbSet<Repair_logs> repair_logs { get; set; }
        public DbSet<BusinessUnit> business_unit { get; set; }
        public DbSet<Department> department { get; set; }
        public DbSet<AccountabilityApproval> accountability_approval { get; set; }
        public DbSet<ReturnItems> return_items { get; set; }
        public DbSet<ReturnItemApproval> return_item_approval { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BusinessUnit>().ToTable("business_unit");
            modelBuilder.Entity<Department>().ToTable("department");

            modelBuilder.Entity<AccountabilityApproval>()
                .HasOne(a => a.accountability_list)
                .WithMany()
                .HasForeignKey(a => a.accountability_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReturnItemApproval>()
                .HasOne(a => a.accountability_list)
                .WithMany()
                .HasForeignKey(a => a.accountability_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Asset>()
                .Property(a => a.cost)
                .HasColumnType("decimal(18,4)");

            modelBuilder.Entity<Computer>()
                .Property(c => c.cost)
                .HasColumnType("decimal(18,4)");

            modelBuilder.Entity<ComputerComponents>()
                .Property(c => c.cost)
                .HasColumnType("decimal(18,4)");

            modelBuilder.Entity<Asset>()
                .HasOne(a => a.owner)
                .WithMany(u => u.assets)
                .HasForeignKey(a => a.owner_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Computer>()
                .HasOne(a => a.owner)
                .WithMany(u => u.computer)
                .HasForeignKey(a => a.owner_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Asset_logs>()
                .HasOne(al => al.assets)
                .WithMany()
                .HasForeignKey(al => al.asset_id);

            modelBuilder.Entity<User_logs>()
                .HasOne(a => a.user)
                .WithMany()
                .HasForeignKey(a => a.user_id);

            modelBuilder.Entity<Computer_logs>()
                .HasOne(cl => cl.computer)
                .WithMany()
                .HasForeignKey(cl => cl.computer_id);

            modelBuilder.Entity<Computer_components_logs>()
                .HasOne(ccl => ccl.computer_components)
                .WithMany()
                .HasForeignKey(ccl => ccl.computer_components_id);

            modelBuilder.Entity<UserAccountabilityList>()
                .HasKey(ual => ual.id);

            modelBuilder.Entity<UserAccountabilityList>()
                .HasOne(ual => ual.owner)
                .WithMany()
                .HasForeignKey(ual => ual.owner_id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ComputerComponents>()
                .HasOne(cc => cc.owner)
                .WithMany(u => u.computer_components)
                .HasForeignKey(cc => cc.owner_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ComputerComponents>()
                .HasOne(cc => cc.computer)
                .WithMany(c => c.Components)
                .HasForeignKey(cc => cc.computer_id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Asset>()
                .HasOne(a => a.computer)
                .WithMany(c => c.Assets)
                .HasForeignKey(a => a.computer_id)
                .IsRequired(false);

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.computer_id)
                .IsUnique(false);

            modelBuilder.Entity<ReturnItems>()
                .HasOne(r => r.user)
                .WithMany(u => u.ReturnItems)
                .HasForeignKey(r => r.user_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReturnItems>()
                .HasOne(r => r.asset)
                .WithMany(a => a.ReturnItems)
                .HasForeignKey(r => r.asset_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReturnItems>()
                .HasOne(r => r.computer)
                .WithMany(a => a.ReturnItems)
                .HasForeignKey(r => r.computer_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReturnItems>()
                .HasOne(r => r.user_accountability_list)
                .WithMany(a => a.ReturnItems)
                .HasForeignKey(r => r.accountability_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReturnItems>()
                .HasOne(r => r.components)
                .WithMany(a => a.ReturnItems)
                .HasForeignKey(r => r.component_id)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync();

            // Create e-signature folder if it doesn't exist
            string eSignatureFolderPath = Path.Combine("C:", "ITAM", "e-signature", "signature");
            if (!Directory.Exists(eSignatureFolderPath))
            {
                Directory.CreateDirectory(eSignatureFolderPath);
            }

            // Seed Business Units
            var businessUnits = new List<BusinessUnit>
            {
                new BusinessUnit { code = "ABFI", description = "Ana's Breeders Farms, Inc." },
                new BusinessUnit { code = "GSIII", description = "Green Solution Integrated Industries Inc." },
                new BusinessUnit { code = "JPPI", description = "Jomaray Pulp Packaging Industries" },
                new BusinessUnit { code = "QPMI", description = "Qualiproduct Marketing Inc." },
                new BusinessUnit { code = "SCC", description = "SuyChicken Corporation" },
                new BusinessUnit { code = "SFC", description = "Southmin Feedmill Corporation" },
                new BusinessUnit { code = "SZ", description = "Subzero Ice & Cold Storage, Inc." },
                new BusinessUnit { code = "SGFFS", description = "SuySarap Global Flavors & Food Services OPC" }
            };

            foreach (var bu in businessUnits)
            {
                if (!await context.business_unit.AnyAsync(b => b.code == bu.code))
                {
                    await context.business_unit.AddAsync(bu);
                }
            }

            // Seed Departments
            var departments = new List<Department>
            {
                new Department { code = "CADO", description = "Central for Administration Office" },
                new Department { code = "CAO", description = "Central Accounting Office" },
                new Department { code = "CENLO", description = "Central for Live Operation Office" },
                new Department { code = "CENTO", description = "Central for Technical Office" },
                new Department { code = "CESSO", description = "Central for Safety and Security Office" },
                new Department { code = "CHRADO", description = "Central for Human Resources and Administration Office" },
                new Department { code = "CISDEVO", description = "Central Information System Development Office" },
                new Department { code = "Dressing Plant", description = "Dressing Plant" },
                new Department { code = "Executive", description = "Executive Office" },
                new Department { code = "Logistics", description = "Logistics" },
                new Department { code = "Sales and Marketing", description = "Sales and Marketing" },
                new Department { code = "Auxiliary", description = "Auxiliary" },
                new Department { code = "Blown Film Plant", description = "Blown Film Plant" },
                new Department { code = "CHB", description = "Concrete Hollow Block" },
                new Department { code = "Recycling", description = "Recycling" },
                new Department { code = "Motorpool", description = "Central Motorpool Division" },
                new Department { code = "EMD", description = "Engineering and Maintenance Department" },
                new Department { code = "Can making", description = "Can making" },
                new Department { code = "Ice Plant", description = "Ice Plant" },
                new Department { code = "MPP", description = "Meat Processing Plant" },
                new Department { code = "QA", description = "Quality Assurance" },
                new Department { code = "Purchasing Department", description = "Purchasing Department" },
                new Department { code = "PED", description = "Planning & Project Engineering Department" },
                new Department { code = "Poultry Operations-FEM", description = "Poultry Operations-Farm Engineering & Maintenance" },
                new Department { code = "PO-FEM", description = "Poultry Operations-Farm Engineering & Maintenance" },
                new Department { code = "PO", description = "Poultry Operations" },
                new Department { code = "FAIP", description = "FAIP Administration" },
                new Department { code = "Procurement", description = "Procurement Department" },
                new Department { code = "OON", description = "Out of Nowhere" },
                new Department { code = "R&D", description = "Research and Development Department" },
                new Department { code = "L&W", description = "Logistics and Warehouse" },
                new Department { code = "SZ", description = "Maintenance" },
                new Department { code = "SZ - Maintenance", description = "SZ - Maintenance" }
            };

            foreach (var dept in departments)
            {
                if (!await context.department.AnyAsync(d => d.code == dept.code))
                {
                    await context.department.AddAsync(dept);
                }
            }

            // Seed Users
            var usersToSeed = new List<User>
            {
                new User { name = "Jefferson B. Arnado", company = "ABFI", department = "CISDEVO", employee_id = "211071", password = BCrypt.Net.BCrypt.HashPassword("@Temp1234!"), date_created = DateTime.UtcNow, is_active = true, designation = "Software Developer Jr.", role = "admin" },
                new User { name = "Jade Aberilla", company = "ABFI", department = "CISDEVO", employee_id = "210953", password = BCrypt.Net.BCrypt.HashPassword("@Temp1234!"), date_created = DateTime.UtcNow, is_active = true, designation = "IT/ OT Operations.", role = "receiver" },
                new User { name = "Irish Lungay", company = "ABFI", department = "CISDEVO", employee_id = "011404", password = BCrypt.Net.BCrypt.HashPassword("@Temp1234!"), date_created = DateTime.UtcNow, is_active = true, designation = "Lead, IT/ OT Operations", role = "receiver" },
                new User { name = "Williard Pernia IV", company = "ABFI", department = "CISDEVO", employee_id = "005029", password = BCrypt.Net.BCrypt.HashPassword("@Temp1234!"), date_created = DateTime.UtcNow, is_active = true, designation = "IT Manager", role = "admin" },
                new User { name = "Zoren Jake Sarillana", company = "ABFI", department = "CISDEVO", employee_id = "211125", password = BCrypt.Net.BCrypt.HashPassword("@Temp1234!"), date_created = DateTime.UtcNow, is_active = true, designation = "Software Developer Jr.", role = "admin" },
                new User { name = "Louivel John C. Cañizares", company = "ABFI", department = "CISDEVO", employee_id = "211030", password = BCrypt.Net.BCrypt.HashPassword("@Temp1234!"), date_created = DateTime.UtcNow, is_active = true, designation = "IT End-User Support", role = "checker" },
                new User { name = "Ian Iverson Suezo", company = "ABFI", department = "CISDEVO", employee_id = "211300", password = BCrypt.Net.BCrypt.HashPassword("@Temp1234!"), date_created = DateTime.UtcNow, is_active = true, designation = "IT/ OT Specialist", role = "checker" }
            };

            foreach (var user in usersToSeed)
            {
                string employeeId = user.employee_id.Trim(); // Ensure no extra spaces

                if (!await context.Users.AnyAsync(u => u.employee_id == employeeId))
                {
                    string uniqueFileName = $"{Guid.NewGuid()}_{employeeId}_signature.png";
                    user.e_signature = Path.Combine(eSignatureFolderPath, uniqueFileName);

                    string sourceFilePath = Path.Combine(Directory.GetCurrentDirectory(), "SeedData", "Signatures", $"{employeeId}_signature.png");
                    string destinationFilePath = Path.Combine(eSignatureFolderPath, uniqueFileName);

                    if (File.Exists(sourceFilePath))
                    {
                        try
                        {
                            File.Copy(sourceFilePath, destinationFilePath, true);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error copying signature for {employeeId}: {ex.Message}");
                        }
                    }

                    await context.Users.AddAsync(user);
                }
            }

            await context.SaveChangesAsync();
        }

    }
}
