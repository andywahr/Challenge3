using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

[assembly: FunctionsStartup(typeof(Functions.Startup))]

namespace Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder().AddJsonFile("local.settings.json", true).AddEnvironmentVariables().Build();

            builder.Services.AddHttpClient("Products", c =>
            {
                c.BaseAddress = new Uri("https://serverlessohproduct.trafficmanager.net");
            });

            builder.Services.AddHttpClient("Users", c =>
            {
                c.BaseAddress = new Uri("https://serverlessohuser.trafficmanager.net");
            });

            builder.Services.AddHttpClient("Management", c =>
            {
                c.BaseAddress = new Uri("https://serverlessohmanagementapi.trafficmanager.net");
            });
            builder.Services.AddHttpClient("TextAnalysis", c =>
            {
                c.BaseAddress = new Uri("https://andywahr-serverless.cognitiveservices.azure.com");
                c.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", config["Challenge9SentimentKey"]);
            });
        }
    }
}