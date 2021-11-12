using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MAD.Integration.Common.EFCore.Tests.Data
{
    internal class TestDbContext : DbContext
    {
        public TestDbContext([NotNull] DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>(cfg =>
            {
                cfg.HasKey(y => y.Id);
                cfg.Property(y => y.Id).ValueGeneratedNever();

                cfg.OwnsOne(y => y.Office, cfg =>
                {
                    cfg.Property(y => y.Id).ValueGeneratedNever();
                    cfg.OwnsOne(y => y.OfficeAddress);
                    cfg.OwnsOne(y => y.Region);
                });
                cfg.OwnsOne(y => y.Region);
                cfg.OwnsMany(y => y.Departments, cfg =>
                {
                    cfg.Property(y => y.Id).ValueGeneratedNever();
                });
            });
        }
    }
}
