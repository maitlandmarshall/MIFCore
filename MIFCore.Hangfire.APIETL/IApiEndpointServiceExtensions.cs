using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MIFCore.Hangfire.APIETL
{
    public static class IApiEndpointServiceExtensions
    {
        public static bool IsDefaultResponder(this IApiEndpointService apiEndpointService)
        {
            var type = apiEndpointService.GetType();
            var endpointNameAttributes = type.GetCustomAttributes<ApiEndpointAttribute>();
            var endpointSelectorAttributes = type.GetCustomAttributes<ApiEndpointSelectorAttribute>();

            return endpointNameAttributes.Any() == false && endpointSelectorAttributes.Any() == false;
        }

        public static bool RespondsToEndpointName(this IApiEndpointService apiEndpointService, string endpointName, string inputPath = null)
        {
            var type = apiEndpointService.GetType();
            var endpointNameAttributes = type.GetCustomAttributes<ApiEndpointAttribute>();
            var endpointSelectorAttributes = type.GetCustomAttributes<ApiEndpointSelectorAttribute>();

            if (endpointNameAttributes.Any(y => y.EndpointName == endpointName && y.InputPath == inputPath))
            {
                return true;
            }
            else if (endpointSelectorAttributes.Any())
            {
                return endpointSelectorAttributes.Any(y => Regex.IsMatch(endpointName, y.Regex) && y.InputPath == inputPath);
            }
            else
            {
                return false;
            }
        }
    }
}
