using System;
using System.Globalization;
using System.Net.Http.Headers;
using DIT.IntegrationService.Data;
using DIT.IntegrationService.Worker.Handlers;
using DIT.IntegrationService.Worker.Quartz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Serilog;
using Serilog.Events;

namespace DIT.IntegrationService.Worker
{
    public class Program
    {
        private static readonly string ENVIRONMENT = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Quartz", LogEventLevel.Information)
                .Enrich.WithProperty("Environment", ENVIRONMENT)
                .Enrich.WithProperty("Application", $"DIT Integration Worker Service - {ENVIRONMENT}")
                .WriteTo.Console()
                .WriteTo.Seq("http://your-seq-server-ip")
                .CreateLogger();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).UseEnvironment(ENVIRONMENT)
                .UseSerilog()
                .ConfigureAppConfiguration((host, configBuilder) =>
                {
                    var env = new CultureInfo("en-US", false).TextInfo.ToTitleCase(
                        host.HostingEnvironment.EnvironmentName);
                    configBuilder.SetBasePath(host.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env}.json",
                       optional: true, reloadOnChange: true)
                    .Build();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // For configurations
                    services.AddOptions().Configure<WorkerOptions>(
                        workerOptions => hostContext.Configuration.Bind("WorkerOptions", workerOptions));

                    // For crm odata endpoint
                    services.AddHttpClient<ICrmRepository, CrmRepository>((serviceProvider, http) =>
                    {
                        var options = serviceProvider.GetService<IOptions<WorkerOptions>>().Value;
                        http.BaseAddress = new Uri(options.AdOption.OrganizationUrl + "/" + options.AdOption.CrmApi);
                        http.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                        http.DefaultRequestHeaders.Add("OData-Version", "4.0");
                        http.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                    })
                    .ConfigurePrimaryHttpMessageHandler((serviceProvider) => new CrmAuth(
                        serviceProvider.GetService<IOptions<WorkerOptions>>().Value.AdOption).ClientHandler);

                    // For local database
                    services.AddSingleton<IStockItemRepository>(serviceProvider => new StockItemRepository(
                        serviceProvider.GetService<IOptions<WorkerOptions>>().Value.ConnectionString));

                    // Add Quartz services
                    services.AddSingleton<IJobFactory, SingletonJobFactory>();
                    services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

                    services.AddSingleton<ItemSyncJob>();
                    services.AddSingleton(serviceProvider => new JobSchedule(
                        jobType: typeof(ItemSyncJob),
                        cronExpression: serviceProvider.GetService<IOptions<WorkerOptions>>().Value.CronExpression));
                    services.AddHostedService<QuartzHostedService>();
                });
    }
}
