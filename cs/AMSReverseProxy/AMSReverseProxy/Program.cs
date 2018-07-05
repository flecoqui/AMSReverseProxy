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
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
             .ConfigureAppConfiguration((webHostBuilderContext, configurationbuilder) =>
             {
                 var environment = webHostBuilderContext.HostingEnvironment;
                 configurationbuilder
                         .AddJsonFile("win.appsettings.Development.json", optional: true);
                 configurationbuilder.AddEnvironmentVariables();
             })
                .UseStartup<Startup>()
            .UseKestrel(options => options.ConfigureEndpoints())
            .Build();
    }
}
