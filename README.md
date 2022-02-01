## MIFCore: A library for building custom integrations using Hangfire and AspNetCore

||MIFCore|MIFCore.Common|MIFCore.Hangfire|MIFCore.Hangfire.JobActions|
|-|-|-|-|-|
|*NuGet*|[![NuGet](https://buildstats.info/nuget/MIFCore)](https://www.nuget.org/packages/MIFCore/)|[![NuGet](https://buildstats.info/nuget/MIFCore.Common)](https://www.nuget.org/packages/MIFCore.Common/)|[![NuGet](https://buildstats.info/nuget/MIFCore.Hangfire)](https://www.nuget.org/packages/MIFCore.Hangfire/)|[![NuGet](https://buildstats.info/nuget/MIFCore.Hangfire.JobActions)](https://www.nuget.org/packages/MIFCore.Hangfire.JobActions/)|

MIFCore is a framework that leverages the job scheduling power of Hangfire and the extensive functionality of .NET Core/.NET to provide you with the ability to develop custom integrations with ease.

# Table of Contents

* [Supported Platforms](#supported-platforms)  
* [Integration Host](#integration-host)
  * [CreateDefaultBuilder](#createdefaultbuilder)  
  * [Using AspNetCore](#using-aspnetcore)
  * [Using AppInsights](#using-appinsights)
* [Application Startup](#application-startup)
* [Configuration](#configuration)
  * [Bindings](#bindings)
  * [Overriding Recurring Job Schedules](#overriding-recurring-job-schedules)
* [Job Actions](#job-actions)
* [Batch Context Filter](#batch-context-filter)
* [Reschedule Job By Date Filter](#reschedule-job-by-date-filter) 
* [BackgroundJobContext](#backgroundjobcontext) 
* [DisableIdenticialQueuedItemsAttribute](#disable-identical-queued-items-attribute)
* [TrackLastSuccessAttribute](#track-last-success-attribute)  

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
    .UseStartup<Startup>();
}
```

By default, the `IntegrationHost.CreateDefaultBuilder()` method will add Hangfire and attempt to create the SQL database configured in the `ConnectionString` property of the `settings.json` file if it does not exist. MIFCore will then initialise the necessary Hangfire JobStorage using GeXiaoguo's [MAMQSqlServerStorage](https://github.com/GeXiaoguo/Hangfire.MAMQSqlExtension) extension. The Hangfire database tables will be created with the schema name "job".




Additionally, the following methods from the [Microsoft.Extensions.Hosting](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting?view=dotnet-plat-ext-6.0) namespace are also available in the IntegrationHostBuilder:.

* [ConfigureServices(Action\<IServiceCollection\>](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostbuilder.configurehostconfiguration?view=dotnet-plat-ext-6.0#microsoft-extensions-hosting-ihostbuilder-configurehostconfiguration(system-action((microsoft-extensions-configuration-iconfigurationbuilder))))
* [ConfigureHostConfiguration(Action\<IConfigurationBuilder\>)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostbuilder.configurehostconfiguration?view=dotnet-plat-ext-6.0#microsoft-extensions-hosting-ihostbuilder-configurehostconfiguration(system-action((microsoft-extensions-configuration-iconfigurationbuilder))))
* [ConfigureAppConfiguration(Action\<HostBuilderContext, IConfigurationBuilder\>)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.hostbuilder.configureappconfiguration?view=dotnet-plat-ext-6.0#microsoft-extensions-hosting-hostbuilder-configureappconfiguration(system-action((microsoft-extensions-hosting-hostbuildercontext-microsoft-extensions-configuration-iconfigurationbuilder))))

#### Using AspNetCore

```csharp
IntegrationHost.CreateDefaultBuilder(args)
    .UseStartup<Startup>()
    .UseAspNetCore();
}
```

Adding the .UseAspNetCore method to your application will enable the [Hangfire Dashboard](https://docs.hangfire.io/en/latest/configuration/using-dashboard.html), allowing you to easily view the status of your jobs and perform other job tasks like triggering, rescheduling or cancelling. The dashboard can be accesssed by browsing to `http://localhost:{BindingPort}/hangfire`.

The `.UseAspNetCore()` method will also instantiate a web server that can be used to mount custom controllers for your application:

```csharp
[Route("HelloWorld")]
public class HelloWorldController : Controller
{                
    public string Index()
    {
        return "Hello!";
    }
}
```    

[![alt text](https://github.com/anthonypos/MIFCore/blob/master/image.jpg?raw=true)]

#### Using AppInsights

MIFCore will link to your Microsoft Azure AppInsights service and automatically start tracking your jobs. To configure this functionality in your application, add the following code:

```csharp
IntegrationHost.CreateDefaultBuilder(args)
    .UseStartup<Startup>()
    .UseAppInsights();
}
```

MIFCore will attempt to retrieve the configured `InstrumentationKey` property from the `settings.json` file and instantiate a [Microsoft.ApplicationInsights.TelemetryClient](https://docs.microsoft.com/en-us/dotnet/api/microsoft.applicationinsights.telemetryclient?view=azure-dotnet) for use with an [IServerFilter](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html). This filter will track an event or exception in the telemetry client each time a job is executed and will provide the following information:

* Event Name/Description
* Application name
* Job Name ($"{Job.Type.Name}.{Job.Method.Name}")
* Job Arguments
* Exception Info

### Configuration

#### Bindings

#### Overriding Recurring Job Schedules

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

### Job Actions

### Batch Context Filter

### Reschedule Job By Date Filter

The `RescheduleJobByDateFilter` is an [IElectStateFilter](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html) that provides the ability for you to reschedule a job for a specified date without incrementing the RetryCount for the job. In order to trigger this filter, a `RescheduleJobException` must be thrown during execution of the job. The `RescheduleJobException` has a `rescheduleDate` parameter that allows you to specify the date that the job should be retried.

```csharp
var rescheduleDate = DateTime.UtcNow.AddMinutes(5);

throw new RescheduleJobException(rescheduleDate);
```

### BackgroundJobContext

The BackgroundJobContext filter is a global [IServerFilter](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html) that retrieves and stores the current PerformContext of a job each time it is executed. The PerformContext allows you to access the Hangfire Job Storage, BackgroundJob object and perform a number of other tasks.

E.g. adding/retrieving parameters during execution of a job:

```csharp 
public void MyRecurringJobMethod()
{  
    BackgroundJobContext.Current.SetJobParameter("MyString", "test");

    var myString = BackgroundJobContext.Current.GetJobParameter<string>("MyString");
}
```

### Disable Identical Queued Items Attribute

The `DisableIdenticalQueuedItems` attribute ensures that a job is not executed while a previous instance of the job is still running. 
The attribute works by creating a SHA384 hash or "fingerprint" and storing it in the `IStorageConnection` of the job context. The fingerprint comprises of:

* Job Type Name
* Job Method Name
* Parameter values passed to the method

When the [IClientFilter.OnCreating(CreatingContext filterContext)](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html) method is invoked, MIFCore will attempt to add a fingerprint to the connnection and will cancel the job if an existing fingerprint is found.

The `FingerprintTimeoutMinutes` parameter is required as this will determine how long execution of the job is locked for. 

```csharp
[DisableIdenticalQueuedItems(FingerprintTimeoutMinutes = 10)]
public void MyRecurringJobMethod()
{            
    var lastSuccessDate = BackgroundJobContext.Current.BackgroundJob.GetLastSuccess();
}
```

By default, jobs that fail will be excluded from this filter however they can be included by setting the `IncludeFailedJobs` parameter to true on the attribute if required:

```csharp
[DisableIdenticalQueuedItems(FingerprintTimeoutMinutes = 10, IncludeFailedJobs = true)]
public void MyRecurringJobMethod()
```

### Track Last Success Attribute

Each time a job with the `TrackLastSuccess` attribute is successfully executed, the current date time will be stored as a parameter on the job. To access the last successful run date, use the `GetLastSuccess` method in the `BackgroundJobContext` to retrieve the "LastSuccess" value.

```csharp
[TrackLastSuccess]
public void MyRecurringJobMethod()
{            
    var lastSuccessDate = BackgroundJobContext.Current.BackgroundJob.GetLastSuccess();
}
```