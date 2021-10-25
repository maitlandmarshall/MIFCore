using System;
using System.Collections.Generic;
using System.Text;

namespace MAD.Integration.Common.EFCore.Tests.Data
{
    internal class ProjectOffice
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public OfficeAddress OfficeAddress { get; set; }
    }
}
