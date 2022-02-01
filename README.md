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
* [Global Default Configuration](#global-default-configuration)
* [Binding Configuration](#binding-configuration)  
* [Job Actions](#job-actions)
* [RecurringJobFactory](#recurring-job-factory)
  * [Overriding Recurring Job Schedules](#overriding-recurring-job-schedules)
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

By default, the `IntegrationHost.CreateDefaultBuilder()` method will add Hangfire and if it does not exist, attempt to create the SQL database configured in the `ConnectionString` property of the `settings.json` file. MIFCore will then initialise the necessary Hangfire JobStorage using GeXiaoguo's [MAMQSqlServerStorage](https://github.com/GeXiaoguo/Hangfire.MAMQSqlExtension) extension. The Hangfire database tables will be created with the schema name "job".

The CreateDefaultBuilder method will register the following filters to the global Hangfire configuration:

* [BackgroundJobContext](#backgroundjobcontext)
* [RescheduleJobByDateFilter](#reschedule-job-by-date-filter)

And, register the [RecurringJobFactory](#recurring-job-factory), which is MIFCore's version of the Hangfire `RecurringJobManager`.

Additionally, the following methods from the [Microsoft.Extensions.Hosting](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting?view=dotnet-plat-ext-6.0) namespace are also available in the IntegrationHostBuilder:.

* [ConfigureServices(Action\<IServiceCollection\>)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostbuilder.configurehostconfiguration?view=dotnet-plat-ext-6.0#microsoft-extensions-hosting-ihostbuilder-configurehostconfiguration(system-action((microsoft-extensions-configuration-iconfigurationbuilder))))
* [ConfigureHostConfiguration(Action\<IConfigurationBuilder\>)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostbuilder.configurehostconfiguration?view=dotnet-plat-ext-6.0#microsoft-extensions-hosting-ihostbuilder-configurehostconfiguration(system-action((microsoft-extensions-configuration-iconfigurationbuilder))))
* [ConfigureAppConfiguration(Action\<HostBuilderContext, IConfigurationBuilder\>)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.hostbuilder.configureappconfiguration?view=dotnet-plat-ext-6.0#microsoft-extensions-hosting-hostbuilder-configureappconfiguration(system-action((microsoft-extensions-hosting-hostbuildercontext-microsoft-extensions-configuration-iconfigurationbuilder))))

##### Using AspNetCore

```csharp
IntegrationHost.CreateDefaultBuilder(args)
    .UseStartup<Startup>()
    .UseAspNetCore();
}
```

Adding the .UseAspNetCore method to your application will enable the [Hangfire Dashboard](https://docs.hangfire.io/en/latest/configuration/using-dashboard.html), allowing you to easily view the status of your jobs and perform other job tasks like triggering, rescheduling or cancelling. The dashboard can be accesssed by browsing to `http://localhost:{BindingPort}/hangfire`.

The `.UseAspNetCore()` method will also instantiate a kestrel web server that can be used to mount custom controllers for your application:

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

![alt text](https://github.com/maitlandmarshall/MIFCore/blob/master/helloworld.jpg?raw=true)

If the `BindingPort` property in the `settings.json` file is 80 or 443, a HTTP.sys webserver will be used instead of a kestrel web server. This configuration also requires a binding path value to be configured on the `BindingPath` property in the `settings.json`;

##### Using AppInsights

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

#### Application Startup
When using MIFCore, the startup of the application goes through 3 stages - the `ConfigureServices`, `Configure` and `PostConfigure` methods:

```csharp
class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Executed before anything is initialised / configured in the application
    }

    public void Configure()
    {
        // Executed after the ConfigureServices method has run and after the IHost is built
    }

    public void PostConfigure()
    {        
        // Executed after everything has been initialised and the IHost.Run() method is called
    }
}
```

The `ConfigureServices` method works the same as the standard `ConfigureServices` method in a .NET Core app and should be used to register/initialise the dependencies for the application. It is executed before anything has been initialised in the application.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddIntegrationSettings<MyConfig>();
    services.AddDbContext<MyDbContext>((svc, builder) => builder.UseSqlServer(svc.GetRequiredService<MyConfig>().ConnectionString));
    services.AddScoped<MyRecurringJob>();
}
```

For information regarding `services.AddIntegrationSetting<MyConfig>();` see [Configuration](#configuration).

The `Configure` method is executed after the `IHost` has been built and the dependencies registered in the `ConfigureServices` method have been initialised. This method is useful for performing any other functionality required before the application `IHost.Run()` method is called e.g. registering custom global Hangfire configuration:

```csharp
public void Configure(IGlobalConfiguration hangfireGlobalConfig, MyConfig myConfig)
{
    hangfireGlobalConfig.Queues = new[] { myConfig.MyQueueName, "default" };
    hangfireGlobalConfig.UseFilter(new MyCoolFilter());
}
```

The `PostConfigure` method is executed after the above methods have finished and the `IHost.Run()` method has been called. This method can be used for running any other startup functionality required e.g. running migrations for your DbContext, registering your recurring Hangfire jobs or executing a job immediately:

```csharp
public void PostConfigure(MyDbContext myDbContext, MyConfig myConfig, IRecurringJobFactory recurringJobFactory)
{
    myDbContext.Database.Migrate();

    recurringJobFactory.CreateRecurringJob<MyRecurringJob>("MyJobName", y => y.RunMyJob(), Cron.Daily(), myConfig.MyQueueName);
}
```

### Global Default Configuration

MIFCore relies on a `settings.json` configuration file being present in the base directory of the application. The first time the application is run, MIFCore will create a `settings.json` file with some default properties if no file is found. The default configuration properties created on launch are:

* ConnectionString
* BindingPort
* InstrumentationKey

The `ConnectionString` and `InstrumentationKey` will be created as blank strings and the `BindingPort` will be set to 1337 by default.

Whilst your application is under development and you wish to have configuration that is stored on your local machine, a `settings.default.json` file can be created in your project that stores any configuration necessary. This file will require you to add the `ConnectionString` and `InstrumentationKey` properties manually, the `BindingPort` in the application will default to 1337.


### Binding Configuration

The binding configuration within the application is used primarily in the `IntegrationHostBuilder.UseAspNetCore()` extension method. When MIFCore launches the Kestrel/HTTP.sys webserver, the `BindingPort` and `BindingPath` will be used to configure the Url. If the `BindingPath` has a value, then the `IApplicationBuilder` will add this value to the base path:

```
{
    "BindingPath: "MyBindingPath"
}
```

![alt text](https://github.com/maitlandmarshall/MIFCore/blob/master/bindingpath.jpg?raw=true)

### Job Actions

### Recurring Job Factory

The `IRecurringJobFactory` class should be used when registering your recurring jobs in MIFCore. When a new job is registered, the `IRecurringJobFactory` will check for any CRON overrides specifies in the `settings.json`, create or update your job in Hangfire and immediately trigger the job if it has not been run previously.

```csharp
recurringJobFactory.CreateRecurringJob<MyRecurringJob>("MyJobName", y => y.RunMyJob(), Cron.Daily());
```

#### Overriding Recurring Job Schedules

The CRON schedule for a job can be overriden in the `settings.json` file by adding a property with the job name as the key and the CRON string as the value:

```
{
    "MyJobName": "0 */3 * * *"
}
```
If an override is added while the application is running, restart the application for the changes to take effect.

### Batch Context Filter

### Reschedule Job By Date Filter

The `RescheduleJobByDateFilter` is an [IElectStateFilter](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html) that provides the ability for you to reschedule a job for a specified date without incrementing the RetryCount for the job. In order to trigger this filter, a `RescheduleJobException` must be thrown during execution of the job. The `RescheduleJobException` has a `rescheduleDate` parameter that allows you to specify the date that the job should be retried.

```csharp
var rescheduleDate = DateTime.UtcNow.AddMinutes(5);

throw new RescheduleJobException(rescheduleDate);
```

### BackgroundJobContext

The BackgroundJobContext filter is a global [IServerFilter](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html) that retrieves and stores the current PerformContext of a job each time it is executed. The PerformContext allows you to access the Hangfire Job Storage, BackgroundJob object and perform a number of other tasks e.g. adding/retrieving parameters during execution of a job:

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

By default, jobs that fail will be excluded from this filter however they can be included by setting the `IncludeFailedJobs` parameter to true:

```csharp
[DisableIdenticalQueuedItems(FingerprintTimeoutMinutes = 10, IncludeFailedJobs = true)]
public void MyRecurringJobMethod()
```

### Track Last Success Attribute

Each time a job with the `TrackLastSuccess` attribute is successfully executed, the current date time will be stored as a parameter on the job. To access the last successful run date, use the `BackgroundJob.GetLastSuccess()` extension method in the `BackgroundJobContext` to retrieve the "LastSuccess" value.

```csharp
[TrackLastSuccess]
public void MyRecurringJobMethod()
{            
    var lastSuccessDate = BackgroundJobContext.Current.BackgroundJob.GetLastSuccess();
}
```