using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MIFCore.Hangfire;

namespace MIFCore.Hangfire.JobActions.Database
{
    public class JobActionDbContext : DbContext
    {
        private readonly HangfireConfig hangfireConfig;

        public JobActionDbContext(HangfireConfig hangfireConfig)
        {
            this.hangfireConfig = hangfireConfig;
        }

        public DbSet<JobAction> JobActions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseSqlServer(this.hangfireConfig.ConnectionString, opt => opt.EnableRetryOnFailure().MigrationsHistoryTable("__EFMigrationsHistory", HangfireConfig.SchemaName));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema(HangfireConfig.SchemaName);

            modelBuilder.Entity<JobAction>(cfg =>
            {
                cfg.HasKey(y => new
                {
                    y.JobName,
                    y.Action,
                    y.Order
                });

                cfg.Property(y => y.Timing).HasConversion(new EnumToStringConverter<JobActionTiming>());
            });
        }
    }
}
