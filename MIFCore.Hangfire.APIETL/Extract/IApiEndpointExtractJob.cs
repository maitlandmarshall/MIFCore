using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Extract
{
    public interface IApiEndpointExtractJob
    {
        Task Extract(string endpointName, ExtractArgs extractArgs = null);
    }

    public class ExtractArgs
    {
        public ExtractArgs(IDictionary<string, object> requestData, Guid? parentApiDataId)
        {
            this.RequestData = requestData;
            this.ParentApiDataId = parentApiDataId;
        }

        public IDictionary<string, object> RequestData { get; set; }
        public Guid? ParentApiDataId { get; set; }
    }
}