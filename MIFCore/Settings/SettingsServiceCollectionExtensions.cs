using MIFCore.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MIFCore.Settings
{
    public static class SettingsServiceCollectionExtensions
    {
        public static IServiceCollection AddIntegrationSettings<TSettings>(this IServiceCollection serviceDescriptors)
            where TSettings : class, new()
        {
            serviceDescriptors.Configure<TSettings>(Globals.DefaultConfiguration);
            serviceDescriptors.AddSingleton(y => y.GetRequiredService<IOptions<TSettings>>().Value);

            return serviceDescriptors;
        }
    }
}
