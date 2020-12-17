using System;
using Google.Apis.Safebrowsing.v4;
using Google.Apis.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(DotnetThoughts.Web.Startup))]

namespace DotnetThoughts.Web
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            
        }
    }
}