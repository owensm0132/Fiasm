using Fiasm.Repository.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Fiasm.Repository
{
    public class FiasmDbContext : DbContext
    {
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<AppUserClaim> AppUserClaims { get; set; }
        public DbSet<AppClaim> AppClaims { get; set; }

    }
}
