using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire
{
    public interface IJobActivated
    {
        void Activated();
    }
}
