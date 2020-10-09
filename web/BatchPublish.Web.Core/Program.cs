using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Forge.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BatchPublish.Web.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration(configBuilder => {
                        configBuilder.AddEnvironmentVariables();
                        /*
                         * 
                         * TODO: you must supply your appsettings.user.json with the           following content:
                        {
                            "Forge": {
                                "ClientId": "<your client Id>",
                                "ClientSecret": "<your secret>"
                            }
                        }
                         */
                        configBuilder.AddJsonFile("appsettings.users.json",false,true);
                        configBuilder.AddForgeAlternativeEnvironmentVariables();
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
