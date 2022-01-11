using Microsoft.EntityFrameworkCore;

namespace MIFCore.JobActions.Database
{
    public class JobActionDbContext : DbContext
    {
        public JobActionDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<JobAction> JobActions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
        }
    }
}
