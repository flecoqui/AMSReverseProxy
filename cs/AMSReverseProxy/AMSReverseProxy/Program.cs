using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AMSReverseProxy
{
    public class Program
    {
        public static IConfiguration Configuration { get; set; }
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
             .ConfigureAppConfiguration((webHostBuilderContext, configurationbuilder) =>
             {
                 var environment = webHostBuilderContext.HostingEnvironment;
                 bool isWindows = System.Runtime.InteropServices.RuntimeInformation
                                               .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

                 
                 if (isWindows)
                 configurationbuilder
                         .AddJsonFile($"win.appsettings.{webHostBuilderContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                 else
                 configurationbuilder
                         .AddJsonFile($"linux.appsettings.{webHostBuilderContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                 configurationbuilder.AddEnvironmentVariables();

                 Configuration = configurationbuilder.Build();
             })
            .ConfigureLogging((hostingContext, logging) =>
             {
                 logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                 //logging.AddConsole();
                 //logging.AddDebug();
             })
                .UseStartup<Startup>()
            .UseKestrel(options => options.ConfigureEndpoints())
            .Build();
    }
}
