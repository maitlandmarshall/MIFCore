using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Http
{
    public class AspNetCoreConfig
    {
        public int BindingPort { get; set; } = 1337;
        public string BindingPath { get; set; }
    }
}
