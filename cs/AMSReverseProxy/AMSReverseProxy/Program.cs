//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AMSReverseProxy.Logs;
using System;

namespace AMSReverseProxy
{
    public class Program
    {
        public static IConfiguration Configuration { get; set; }
        public static ILogger Logger { get; set; }
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureLogging((hostingContext, logging) =>
            {

                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddFile((option) =>
                {
                    try
                    {
                        hostingContext.Configuration.GetSection("FileLoggerOptions").Bind(option);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Write("Exception while reading FileLogger settings: " + ex.Message);
                    }
                });               
                //logging.AddConsole();
                //logging.AddDebug();
            })
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

            .UseStartup<Startup>()
            .UseKestrel(options => options.ConfigureEndpoints())
            .Build();
    }
}
