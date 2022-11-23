﻿using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL
{
    public interface IApiEndpointAttributeFactory
    {
        IAsyncEnumerable<ApiEndpoint> Create();
    }
}