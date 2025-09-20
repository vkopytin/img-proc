using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;

namespace ImgProc
{
  class Program
  {
    private static IConfiguration Configuration { get; set; }

    static async Task Main(string[] args)
    {
      //Program.Train().Wait();
      var builder = new HostBuilder();
      //builder.UseEnvironment("Development");

      builder.ConfigureServices((context, s) =>
      {
        ConfigureServices(s);
        s.BuildServiceProvider();
      });

      builder.ConfigureLogging(logging =>
      {
        logging.AddConsole();
        string appInsightsKey = Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
        if (!string.IsNullOrEmpty(appInsightsKey))
        {
          // This uses the options callback to explicitly set the instrumentation key.
          logging.AddApplicationInsights(appInsightsKey)
                    .SetMinimumLevel(LogLevel.Information)
                    .AddApplicationInsightsWebJobs(o => { o.InstrumentationKey = appInsightsKey; });
        }
      });

      var tokenSource = new CancellationTokenSource();
      var ct = tokenSource.Token;
      using var host = builder.Build();
      using var scope = host.Services.CreateScope();
      var normalizeImage = scope.ServiceProvider.GetRequiredService<NormalizeImageService>();
      await normalizeImage.Process();
      tokenSource.Dispose();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
      var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

      Configuration = new ConfigurationBuilder()
          //.SetBasePath(Assembly.GetEntryAssembly().Location)
          .SetBasePath(GetBasePath())
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
          .AddEnvironmentVariables() //this doesnt do anything useful notice im setting some env variables explicitly.
          .Build();  //build it so you can use those config variables down below.

      #region RegisterServiceProviders
      var appSettingsSection = Configuration.GetSection("AppSettings");
      services.Configure<AppSettings>(Configuration);

      services.AddSingleton(p => p.GetRequiredService<IConfiguration>().GetSection("ImgBB")
        .Get<ImgBBConfig>() ?? new ImgBBConfig("", "")
      );
      var dbConn = Configuration.GetConnectionString("DefaultDB");
      services.AddMemoryCache();
      services.AddSingleton(Configuration);

      var client = Configuration.CreateMongoClient("MongoDBConnection");
      services.AddScoped(o => new MongoDbContext(client));
      services.AddScoped<NormalizeImageService>();
      services.AddHttpClient();

      #endregion
    }

    private static string GetBasePath()
    {
      var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
      var isDevelopment = string.Equals(environment, Environments.Development, StringComparison.InvariantCultureIgnoreCase);
      if (isDevelopment)
      {
        return Directory.GetCurrentDirectory();
      }
      //using var processModule = Process.GetCurrentProcess().MainModule;
      //return Path.GetDirectoryName(processModule?.FileName);
      var assemplyFullName = Assembly.GetEntryAssembly().Location;
      return Path.GetDirectoryName(assemplyFullName);
    }
  }
}
