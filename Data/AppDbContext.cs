using Microsoft.EntityFrameworkCore;
using MindtecFinance.Models;

namespace MindtecFinance.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<FinancialTransaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>().HasData(
                new AppUser
                {
                    Id = 1,
                    UserName = "Admin",
                    Password = "P@ssw0rd123", // In actual app use hashing
                    UserTypeId = 2,
                    Email = "ashokrana1977@gmail.com",
                    Status = "active"
                }
            );
        }
    }
}
