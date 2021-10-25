using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAD.Integration.Common.EFCore.Tests.Data
{
    internal class TestDbContextFactory
    {
        public static TestDbContext Create()
        {
            var connectionString = TestConfigFactory.Create().ConnectionString;
            return new TestDbContext(new DbContextOptionsBuilder().UseSqlServer(connectionString).Options);
        }
    }
}
