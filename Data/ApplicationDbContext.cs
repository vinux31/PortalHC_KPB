using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;

namespace HcPortal.Data
{
    /// <summary>
    /// Database context untuk HC Portal dengan Identity support
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customize Identity table names (opsional, untuk lebih bersih)
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
            });
        }
    }
}
