using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;

namespace DotnetThoughts.Web
{
    public class UrlResolver
    {
        [FunctionName("UrlResolver")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string url = req.Query["url"];
            if (string.IsNullOrEmpty(url))
            {
                return new BadRequestResult();
            }

            using (var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            })
            {
                using (var httpClient = new HttpClient(handler))
                {
                    var flag = true;
                    while (flag)
                    {
                        try
                        {
                            var response = await httpClient.GetAsync(url);
                            if (response.StatusCode == HttpStatusCode.MovedPermanently || response.StatusCode == HttpStatusCode.Found)
                            {
                                url = response.Headers.Location.AbsoluteUri;
                            }
                            else
                            {
                                flag = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogError(ex, $"Unable to resolve the URL - {url}");
                            flag = false;
                        }
                    }
                }
            }

            return new OkObjectResult(url);
        }
    }
}
