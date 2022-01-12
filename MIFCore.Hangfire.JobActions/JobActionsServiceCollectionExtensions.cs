using Microsoft.Extensions.DependencyInjection;
using MIFCore.Hangfire.JobActions.Database;

namespace MIFCore.Hangfire.JobActions
{
    internal static class JobActionsServiceCollectionExtensions
    {
        public static IServiceCollection AddJobActions(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddDbContext<JobActionDbContext>();
            serviceDescriptors.AddDbContextFactory<JobActionDbContext>();
            serviceDescriptors.AddSingleton<JobActionFilter>();

            return serviceDescriptors;
        }
    }
}
