﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL
{
    public interface IPrepareNextRequest : IApiEndpointService
    {
        Task<IDictionary<string, object>> OnPrepareNextRequest(PrepareNextRequestArgs args);
    }
}