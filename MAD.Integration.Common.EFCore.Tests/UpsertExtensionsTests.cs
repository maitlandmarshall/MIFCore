using Microsoft.VisualStudio.TestTools.UnitTesting;
using MAD.Integration.Common.EFCore;
using System;
using System.Collections.Generic;
using System.Text;
using MAD.Integration.Common.EFCore.Tests.Data;

namespace MAD.Integration.Common.EFCore.Tests
{
    [TestClass()]
    public class UpsertExtensionsTests
    {
        [TestMethod()]
        public void Upsert_Update_OwnedTypePropertiesAreSet()
        {
            var project = new Project
            {
                Id = 1337,
                Name = "Cool project",
                Office = new ProjectOffice
                {
                    Id = 1899,
                    Name = "Main office",
                    OfficeAddress = new OfficeAddress
                    {
                        Address = "123 Fake Street",
                        City = "Fake City"
                    }
                },
                Region = new ProjectRegion
                {
                    Name = "Australia",
                    Id = 998
                },
                Departments = new List<ProjectDepartment>
                {
                   new ProjectDepartment
                   {
                       Id = 1,
                       Name = "Mech"
                   },
                   new ProjectDepartment
                   {
                       Id = 2,
                       Name = "Dech"
                   }
                }
            };

            using (var db = TestDbContextFactory.Create())
            {
                db.Add(new Project
                {
                    Id = 1337,
                    Name = "Cool project"
                });

                db.SaveChanges();
            }

            using (var db = TestDbContextFactory.Create())
            {
                db.Upsert(project, entity =>
                {
                    switch (entity)
                    {
                        case ProjectDepartment pd:
                            db.Entry(entity).Property("ProjectId").CurrentValue = project.Id;
                            break;
                    }
                });

                db.SaveChanges();
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            using var db = TestDbContextFactory.Create();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
    }
}