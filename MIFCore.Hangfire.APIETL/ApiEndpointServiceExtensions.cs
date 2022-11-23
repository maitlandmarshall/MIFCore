using System.Reflection;
using System.Text.RegularExpressions;

namespace MIFCore.Hangfire.APIETL
{
    public static class ApiEndpointServiceExtensions
    {
        public static bool RespondsToEndpointName(this IApiEndpointService apiEndpointService, string endpointName)
        {
            var type = apiEndpointService.GetType();
            var endpointNameAttribute = type.GetCustomAttribute<ApiEndpointNameAttribute>();
            var endpointSelectorAttribute = type.GetCustomAttribute<ApiEndpointSelectorAttribute>();

            if (endpointNameAttribute?.EndpointName == endpointName)
            {
                return true;
            }
            else if (endpointSelectorAttribute != null)
            {
                var regex = new Regex(endpointSelectorAttribute.Regex);

                return regex.IsMatch(endpointName);
            }

            return false;
        }
    }
}
