using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public interface IParseResponse : IApiEndpointService
    {
        Task<IEnumerable<IDictionary<string, object>>> OnParse(ParseResponseArgs args);
    }
}
