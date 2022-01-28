## MIFCore: A library for building custom integrations using Hangfire and AspNetCore

||MIFCore|MIFCore.Common|MIFCore.Hangfire|MIFCore.Hangfire.JobActions|
|-|-|-|-|-|
|*NuGet*|[![NuGet](https://img.shields.io/nuget/v/MIFCore.svg)](https://www.nuget.org/packages/MIFCore/)<br />[![NuGet](https://img.shields.io/nuget/dt/MIFCore)](https://www.nuget.org/packages/MIFCore/)|[![NuGet](https://img.shields.io/nuget/v/MIFCore.Common.svg)](https://www.nuget.org/packages/MIFCore.Common/)<br />[![NuGet](https://img.shields.io/nuget/dt/MIFCore.Common)](https://www.nuget.org/packages/MIFCore.Common/)|[![NuGet](https://img.shields.io/nuget/v/MIFCore.Hangfire.svg)](https://www.nuget.org/packages/MIFCore.Hangfire/)<br />[![NuGet](https://img.shields.io/nuget/dt/MIFCore.Hangfire)](https://www.nuget.org/packages/MIFCore.Hangfire/)|[![NuGet](https://img.shields.io/nuget/v/MIFCore.Hangfire.JobActions.svg)](https://www.nuget.org/packages/MIFCore.Hangfire.JobActions/)<br />[![NuGet](https://img.shields.io/nuget/dt/MIFCore.Hangfire.JobActions)](https://www.nuget.org/packages/MIFCore.Hangfire.JobActions/)|

MIFCore is a framework that leverages the job scheduling power of Hangfire and the extensive functionality of .NET Core/.NET to provide you with the ability to develop custom integrations with ease.

E.g. override the default IntegrationHost in the Program.cs class with the MIFCore package:
```csharp
using MIFCore.Hangfire.Analytics;
using MIFCore.Hangfire.JobActions;
using MIFCore.Http;

//

class Program
{
    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args)
        => IntegrationHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()            
            .UseAspNetCore() //Creates a web server for app that hosts the Hangfire Dashboard
            .UseAppInsights(); //Connects your app to Microsoft Azure App Insights
}
```

Then add the following Configur and PostConfigure methods to your Startup.cs where you can register any filters, recurring integration jobs you wish to run, perform database migrations and any other initial configuration you require:
```csharp
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MIFCore.Settings;

//

class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddIntegrationSettings<MyConfig>(); //Bind custom configuration items in settings.json file to a config class
        services.AddDbContext<MyDbContext>((svc, options) => options.UseSqlServer(svc.GetRequiredService<MyConfig>().ConnectionString));
        services.AddScoped<MyCustomJob>();            
    }

    public void Configure(IGlobalConfiguration hangfireGlobalConfig)
    {
        hangfireGlobalConfig.UseFilter(new MyCustomFilter());
    }

    public void PostConfigure(MyDbContext dbContext, IRecurringJobManager recurringJobManager)
    {        
        dbContext.Database.Migrate();

        recurringJobManager.AddOrUpdate<MyCustomJob>("MyJobName", y => y.DoTheJob(), Cron.Daily());
    }
}
```

# Table of Contents

* [Supported Platforms](#supported-platforms)  
* [Configuration](#configuration)
* [Integration Host](#integration-host)
* [Startup](#startup)
* [Hangfire Filters](#hangfire-filters)
  * [Global Filters](#global-filters)
  * [Job Filters](#job-filters)
* [Exceptions](#exceptions)
* [Job Actions](#body-content)