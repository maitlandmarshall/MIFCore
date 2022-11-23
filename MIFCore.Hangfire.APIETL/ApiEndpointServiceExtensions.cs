using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MIFCore.Hangfire.APIETL
{
    public static class ApiEndpointServiceExtensions
    {
        public static bool RespondsToEndpointName(this IApiEndpointService apiEndpointService, string endpointName)
        {
            var type = apiEndpointService.GetType();
            var endpointNameAttributes = type.GetCustomAttributes<ApiEndpointNameAttribute>();
            var endpointSelectorAttributes = type.GetCustomAttributes<ApiEndpointSelectorAttribute>();

            if (endpointNameAttributes.Any(y => y.EndpointName == endpointName))
            {
                return true;
            }
            else if (endpointSelectorAttributes.Any())
            {
                return endpointSelectorAttributes.Any(y => Regex.IsMatch(endpointName, y.Regex));
            }

            return false;
        }
    }
}
