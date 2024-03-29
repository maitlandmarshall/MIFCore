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

Adding the .UseAspNetCore method to your application will enable the [Hangfire Dashboard](https://docs.hangfire.io/en/latest/configuration/using-dashboard.html), allowing you to easily view the status of your jobs and perform other job tasks like triggering, rescheduling or cancelling. The dashboard can be accesssed by browsing to `http://localhost:{BindingPort}/{BindingPath}/hangfire`.

The `.UseAspNetCore()` method will also instantiate a Kestrel web server that can be used to mount custom controllers for your application:

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

If the `BindingPort` property in the `settings.json` file is 80 or 443, a HTTP.sys webserver will be used instead of a Kestrel web server. The advantage to setting your `BindingPort` to 80 is that the HTTP.sys web server allows multiple services to share the same port as long as each service has a different `BindingPath`.

##### Using AppInsights

MIFCore will link to your Microsoft Azure AppInsights resource and automatically start tracking your jobs. To configure this functionality in your application, add the following code:

```csharp
IntegrationHost.CreateDefaultBuilder(args)
    .UseStartup<Startup>()
    .UseAppInsights();
}
```

MIFCore will attempt to retrieve the configured `InstrumentationKey` property from the `settings.json` file and instantiate a [Microsoft.ApplicationInsights.TelemetryClient](https://docs.microsoft.com/en-us/dotnet/api/microsoft.applicationinsights.telemetryclient?view=azure-dotnet) that operates alongside a global [IServerFilter](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html). This filter will track an event or exception in the telemetry client each time a job is executed and will provide the following information:

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

For information regarding `services.AddIntegrationSetting<MyConfig>();` see [Global Default Configuration](#global-default-configuration).

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
public void PostConfigure(MyDbContext myDbContext, MyConfig myConfig, IRecurringJobManager recurringJobManager)
{
    myDbContext.Database.Migrate();

    recurringJobManager.CreateRecurringJob<MyRecurringJob>("MyJobName", y => y.RunMyJob(), Cron.Daily(), myConfig.MyQueueName);
}
```

### Global Default Configuration

MIFCore relies on a `settings.json` configuration file being present in the base directory of the application. The first time the application is run, MIFCore will create a `settings.json` file with some default properties if no file is found. The default configuration properties created on launch are:

* ConnectionString
* BindingPort
* InstrumentationKey

These configuration items are automatically added to the `Globals.DefaultConfiguration`. The `ConnectionString` and `InstrumentationKey` will be created as blank strings and the `BindingPort` will be set to 1337 by default.

Whilst your application is under development and you wish to have configuration that is stored on your local machine, a `settings.default.json` file can be created in your project that stores any required configuration. You must add the `ConnectionString` and `InstrumentationKey` properties to this file manually, the `BindingPort` in the application will default to 1337. If no `BindingPort` can be found in the `settings.json` file, the system will automatically bind to port 666.

You have the option of adding your own custom configuration by creating a class with the required properties and then registering it using the `IServiceCollection.AddIntegrationSettings()` extension method:

```csharp
// settings.json
{
    "ConnectionString": "",
    "BindingPort": 1337,
    "BindingPath": "",
    "InstrumentationKey": "",
    "MyConfigProperty": true
}

// Custom configuration class
public class MyConfig
{
    public bool MyConfigProperty {get; set;}
}

// Startup.cs 
public void ConfigureServices(IServiceCollection services)
{
    services.AddIntegrationSettings<MyConfig>();
}
```

The `.AddIntegrationSettings()` method will add your custom configuration plus the other default configuration properties into a single IConfiguration object that can be accessed globally using `Globals.DefaultConfiguration`:

```csharp
var myConfigItem = Globals.DefaultConfiguration["MyConfigProperty"];
```

### Binding Configuration

The binding configuration within the application is used primarily in the `IntegrationHostBuilder.UseAspNetCore()` extension method. When MIFCore launches the Kestrel/HTTP.sys webserver, the `BindingPort` and `BindingPath` will be used to configure the Url. If the `BindingPath` has a value, then the `IApplicationBuilder` will add this value to the base path:

```
{
    "BindingPath: "MyBindingPath"
}
```

![alt text](https://github.com/maitlandmarshall/MIFCore/blob/master/bindingpath.jpg?raw=true)

### Job Actions

Job Actions provides you with the ability to create an execution order for custom sql commands and recurring jobs. To enable Job Actions, run the `.UseJobActions()` extension method on the `IntegrationHostBuilder` during startup:

```csharp
IntegrationHost.CreateDefaultBuilder(args)
    .UseStartup<Startup>()
    .UseJobActions();
}
```

Once enabled, MIFCore will automatically create the `JobActions` table in database configured in your `ConnectionString`. This table will use the same schema ("job") as the Hangfire sql tables.

The `job.JobActions` table consists of the following columns:

* `JobName` The name of the job you wish to trigger the action.
* `Action` The sql command or recurring job id you wish to execute for the action.
* `Order` The sequence number for this action.
* `Timing` Must be set to `BEFORE` or `AFTER`. Specifies whether action should be run before or after the job.
* `IsEnabled` Specifies whether this action is enabled.
* `Database` Specifies which sql database this action should be run against. 

If the `Database` column is left as a null value, MIFCore will use the current database configured in the `ConnectionString` property of the `settings.json` file.

To create some Job Actions, register your required recurring jobs in the application startup class:

```csharp
public void PostConfigure(IRecurringJobManager recurringJobManager)
{
    recurringJobManager.CreateRecurringJob<MyRecurringJob>("MyJob", y => y.RunMyJob(), Cron.Daily());
    recurringJobManager.CreateRecurringJob<MyRecurringJob>("MyOtherJob", y => y.RunMyOtherJob(), Cron.Daily());
}
```

Then, manually insert the required job actions records into the `job.JobActions` table in SQL ensuring to use the same `JobName` as the recurring job you registered in the previous step:

```sql
INSERT [job].[JobActions] ([JobName],[Action],[Order],[Timing],[IsEnabled],[Database])
VALUES ('MyJob', 'EXECUTE [dbo].[MyStoredProc]', 1, 'BEFORE', 1, 'MyOtherDatabase')
GO

INSERT [job].[JobActions] ([JobName],[Action],[Order],[Timing],[IsEnabled])
VALUES ('MyJob', 'recurring-job:MyRecurringJob', 1, 'AFTER', 1)
GO
```

In the example above, the first action will execute a sql command of `EXECUTE [dbo].[MyStoredProc]` against the 'MyOtherDatabase' database before the recurring job 'MyJob' has been executed. Then, a second action will be run to trigger the 'MyRecurringJob' recurring job after the original job 'MyJob' has been executed.

The `Order` value should only be incremented for job actions that have the same `Timing` value:

```sql
INSERT [job].[JobActions] ([JobName],[Action],[Order],[Timing],[IsEnabled],[Database])
VALUES ('MyJob', 'EXECUTE [dbo].[MyStoredProc]', 1, 'BEFORE', 1, 'MyOtherDatabase')
GO

INSERT [job].[JobActions] ([JobName],[Action],[Order],[Timing],[IsEnabled],[Database])
VALUES ('MyJob', 'EXECUTE [dbo].[MyOtherStoredProc]', 2, 'BEFORE', 1, 'MyOtherDatabase')
GO

INSERT [job].[JobActions] ([JobName],[Action],[Order],[Timing],[IsEnabled])
VALUES ('MyJob', 'recurring-job:MyRecurringJob', 1, 'AFTER', 1)
GO

INSERT [job].[JobActions] ([JobName],[Action],[Order],[Timing],[IsEnabled])
VALUES ('MyJob', 'recurring-job:MyOtherRecurringJob', 2, 'AFTER', 1)
GO
```

The `recurring-job:` prefix is required on the action in order to execute an existing recurring job. The format of the action should be `recurring-job:{RecurringJobName}`. By default, Job Actions will be executed against the database configured in the `ConnectionString` property of the `settings.json` file.

### Recurring Jobs

NOTE: `IRecurringJobFactory & RecurringJobFactory are now obsolete in MIFCore.Hangfire v1.1.0`

The `Hangfire.IRecurringJobManager` interface should be used when registering your recurring jobs in MIFCore. When a new job is registered, the `IRecurringJobManager` will check for any CRON overrides specified in the `settings.json` and create or update your job in Hangfire.

```csharp
recurringJobManager.CreateRecurringJob<MyRecurringJob>("MyJobName", y => y.RunMyJob(), Cron.Daily());
```

Set the `triggerIfNeverExecuted` parameter to true if you need Hangfire to trigger the job if it has not been run previously:

```csharp
recurringJobManager.CreateRecurringJob<MyRecurringJob>("MyJobName", y => y.RunMyJob(), Cron.Daily(), triggerIfNeverExecuted: true);
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

The `BatchContextFilter` is a global [IClientFilter](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html)/[IServerFilter](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html) that can be added during the startup of your application. When a job is executed with the `BatchContextFilter` hangfire filter active, a new Guid will be stored in an `Id` batch parameter and the current Utc date will be stored in a `Started` batch parameter against the context of the job. You can also add custom batch parameters during execution of the job using the `SetBatchParameter` method in the `BackgroundJobContext`.

For any nested jobs that are queued during the execution of this initial job, batch parameters can be accessed by using the [BackgroundJobContext](#backgroundjobcontext):

```csharp
public class MyRecurringJobType
{
    private readonly IBackgroundJobClient backgroundJobClient;

    public MyRecurringJobType(IBackgroundJobClient backgroundJobClient)
    {
        this.backgroundJobClient = backgroundJobClient;
    }

    public void RootJob(IBackgroundJobClient backgroundJobClient)
    {
        BackgroundJobContext.Current.SetBatchParameter("MyCustomBatchParameter", "ParameterValue");

        this.backgroundJobClient.Enqueue<MyRecurringJobType>(y => y.NestedJob1());
    }

    public void NestedJob1()
    {
        var batchId = BackgroundJobContext.Current.GetBatchParameter<Guid>("Id");
        var started = BackgroundJobContext.Current.GetBatchParameter<DateTime>("Started");
        var myValue = BackgroundJobContext.Current.GetBatchParameter<string>("MyCustomBatchParameter");

        this.backgroundJobClient.Enqueue<MyRecurringJobType>(y => y.NestedJob2());
    }

    public void NestedJob2()
    {
        var batchId = BackgroundJobContext.Current.GetBatchParameter<Guid>("Id");
        var started = BackgroundJobContext.Current.GetBatchParameter<DateTime>("Started");
        var myValue = BackgroundJobContext.Current.GetBatchParameter<string>("MyCustomBatchParameter");
    }
}
```

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

If populated, the optional `FingerprintTimeoutMinutes` parameter will determine how long execution of the job is locked for. 

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