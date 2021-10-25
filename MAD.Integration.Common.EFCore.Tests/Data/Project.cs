using System;
using System.Collections.Generic;
using System.Text;

namespace MAD.Integration.Common.EFCore.Tests.Data
{
    internal class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ProjectOffice Office { get; set; }
        public ProjectRegion Region { get; set; }

        public IEnumerable<ProjectDepartment> Departments { get; set; }
    }
}
