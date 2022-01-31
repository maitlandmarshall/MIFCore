## MIFCore: A library for building custom integrations using Hangfire and AspNetCore

||MIFCore|MIFCore.Common|MIFCore.Hangfire|MIFCore.Hangfire.JobActions|
|-|-|-|-|-|
|*NuGet*|[![NuGet](https://buildstats.info/nuget/MIFCore)](https://www.nuget.org/packages/MIFCore/)|[![NuGet](https://buildstats.info/nuget/MIFCore.Common)](https://www.nuget.org/packages/MIFCore.Common/)|[![NuGet](https://buildstats.info/nuget/MIFCore.Hangfire)](https://www.nuget.org/packages/MIFCore.Hangfire/)|[![NuGet](https://buildstats.info/nuget/MIFCore.Hangfire.JobActions)](https://www.nuget.org/packages/MIFCore.Hangfire.JobActions/)|

MIFCore is a framework that leverages the job scheduling power of Hangfire and the extensive functionality of .NET Core/.NET to provide you with the ability to develop custom integrations with ease.

# Table of Contents

* [Supported Platforms](#supported-platforms)  
* [Integration Host](#integration-host)
  * [CreateDefaultBuilder](#default-builder)  
  * [UseAspNetCore](#asp-net-core)
  * [UseAppInsights](#app-insights)
* [Application Startup](#app-startup)
* [Configuration](#configuration)
  * [Binding Port & Binding Path](#binding-port-path)
  * [Overriding Recurring Job Schedules](#recurring-job-overrides)
* [Job Actions](#job-actions)
* [Hangfire Filters](#hangfire-filters)
  * [Batch Context Filter](#batch-context-filter)
  * [Reschedule Job By Date Filter](#reschedule-filter)  
* [DisableIdenticialQueuedItemsAttribute](#disable-identical-queued-items)
* [TrackLastSuccessAttribute](#track-last-success)  
* [Background Job Context](#background-job-context)

### Supported Platforms

MIFCore currently supports the following platforms:

* .NET Standard 2.1
* .NET Core 3.1
* .NET 5
* .NET 6

### Integration Host

The IntegrationHost is the main building block of the MIFCore framework and builds a Microsoft.Extensions.Hosting.IHost on launch, similar to a vanilla .NET Core application. The IntegrationHostBuilder is utilized by the IntegrationHost to create a host builder where startup operations can be performed.

#### CreateDefaultBuilder

The `IntegrationHost.CreateDefaultBuilder()` method creates an instance of the IntegrationHostBuilder which allows you to configure a Startup class where you can configure/initialise your services:

```csharp
IntegrationHost.CreateDefaultBuilder(args)
    .UseStartup<Startup>()
    .UseAspNetCore();
}
```

Additionally, the following methods from the [Microsoft.Extensions.Hosting](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting?view=dotnet-plat-ext-6.0) namespace are also available in the IntegrationHostBuilder:.

* [ConfigureServices(Action\<IServiceCollection\>](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostbuilder.configurehostconfiguration?view=dotnet-plat-ext-6.0#microsoft-extensions-hosting-ihostbuilder-configurehostconfiguration(system-action((microsoft-extensions-configuration-iconfigurationbuilder))))
* [ConfigureHostConfiguration(Action\<IConfigurationBuilder\>)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostbuilder.configurehostconfiguration?view=dotnet-plat-ext-6.0#microsoft-extensions-hosting-ihostbuilder-configurehostconfiguration(system-action((microsoft-extensions-configuration-iconfigurationbuilder))))
* [ConfigureAppConfiguration(Action\<HostBuilderContext, IConfigurationBuilder\>)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.hostbuilder.configureappconfiguration?view=dotnet-plat-ext-6.0#microsoft-extensions-hosting-hostbuilder-configureappconfiguration(system-action((microsoft-extensions-hosting-hostbuildercontext-microsoft-extensions-configuration-iconfigurationbuilder))))

### Application Startup

```csharp
class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Executed before anything is initialised / configured in the application
    }

    public void Configure(IGlobalConfiguration hangfireGlobalConfig)
    {
        // Executed after the ConfigureServices method has run and after the IHost is built
    }

    public void PostConfigure(MyDbContext dbContext, IRecurringJobManager recurringJobManager)
    {        
        // Executed after everything has been initialised and the IHost.Run() method is called
    }
}
```

### Configuration

#### Binding Port & Binding Path

#### Overriding Recurring Job Schedules

### Job Actions

### Batch Context Filter

The BatchContextFilter provides the ability for you to add 

### Reschedule Job By Date Filter

The `RescheduleJobByDateFilter` is an [IElectStateFilter](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html) that provides the ability for you to reschedule a job for a specified date without incrementing the RetryCount for the job. In order to trigger this filter, a `RescheduleJobException` must be thrown during execution of the job. The `RescheduleJobException` has a `rescheduleDate` parameter that allows you to specify the date that the job should be retried.

```csharp
var rescheduleDate = DateTime.UtcNow.AddMinutes(5);

throw new RescheduleJobException(rescheduleDate);
```

### Background Job Context Filter

The BackgroundJobContext filter is a global [IServerFilter](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html) stores the current PerformContext each time a job is executed. The PerformContext allows you to access the Hangfire Job Storage, BackgroundJob object and perform a number of other tasks

E.g. adding/retrieving parameters during execution of a job:

```csharp 
public void MyRecurringJobMethod()
{  
    BackgroundJobContext.Current.SetJobParameter("MyString", "test");

    var myString = BackgroundJobContext.Current.GetJobParameter<string>("MyString");
}
```

### Disable Identical Queued Items Attribute

The `DisableIdenticalQueuedItems` attribute ensures that a job is not executed while a previous instance of the job is still running. This attribute requires a `FingerprintTimeoutMinutes` value to be provided as this will determine how long execution of the job is locked for. 

```csharp
[DisableIdenticalQueuedItems(FingerprintTimeoutMinutes = 10)]
public void MyRecurringJobMethod()
{            
    var lastSuccessDate = BackgroundJobContext.Current.GetJobParameter<DateTime?>("LastSuccess");
}
```

By default, jobs that fail will be excluded from this filter however if required they can be included by setting the `IncludeFailedJobs` parameter to true on the attribute `[DisableIdenticalQueuedItems(FingerprintTimeoutMinutes = 10, IncludeFailedJobs = true)]`.



### Track Last Success Attribute

Each time a job with the `TrackLastSuccess` attribute is successfully executed, the current date time will be stored as a parameter on the job. To access the last successful run date, use the `GetJobParameter` method in the `BackgroundJobContext` to retrieve the "LastSuccess" value.

```csharp
[TrackLastSuccess]
public void MyRecurringJobMethod()
{            
    var lastSuccessDate = BackgroundJobContext.Current.GetJobParameter<DateTime?>("LastSuccess");
}
```