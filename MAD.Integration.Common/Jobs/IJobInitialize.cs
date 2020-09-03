using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    public interface IJobInitialize
    {
        void Initialized();
        void Deinitialized();
    }
}
