using Fiasm.Data.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Fiasm.Data
{
    public class FiasmDbContext : DbContext
    {
        public FiasmDbContext(DbContextOptions options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<UserChangeLog> UserChangeLogs { get; set; }

    }
}
