﻿namespace MIFCore.Hangfire.APIETL
{
    public class HandleResponseArgs
    {
        public HandleResponseArgs(ApiEndpoint endpoint, ApiData apiData)
        {
            this.Endpoint = endpoint;
            this.ApiData = apiData;
        }

        public ApiEndpoint Endpoint { get; }
        public ApiData ApiData { get; }
    }
}