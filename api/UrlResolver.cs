using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net;
using AngleSharp;
using System.Linq;

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
            bool includePreviewInResponse = false;
            string ogpreview = req.Query["ogp"];
            if (!string.IsNullOrEmpty(ogpreview))
            {
                Boolean.TryParse(ogpreview, out includePreviewInResponse);
            }

            var actualUrl = await GetActualUrl(url, log);

            if (includePreviewInResponse)
            {
                var openGraph = await GetPreview(actualUrl, log);
                return new OkObjectResult(openGraph);
            }

            return new OkObjectResult(new OpenGraph
            {
                Url = actualUrl
            });
        }

        private async Task<object> GetPreview(string url, ILogger log)
        {
            var openGraph = new OpenGraph
            {
                Url = url
            };
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36");
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en;q=0.8");
                    var html = await httpClient.GetStringAsync(url);
                    var config = Configuration.Default.WithDefaultLoader();
                    var context = BrowsingContext.New(config);
                    var document = await context.OpenAsync(c => c.Content(html));
                    var titleElement = document.QuerySelectorAll("meta[property=\"og:title\"]")
                                .FirstOrDefault();
                    if (titleElement != null)
                    {
                        openGraph.Title = titleElement.GetAttribute("content");
                    }
                    else
                    {
                        titleElement = document.QuerySelectorAll("title")
                                    .FirstOrDefault();
                        if (titleElement != null)
                        {
                            openGraph.Title = titleElement.TextContent;
                        }
                    }

                    var descriptionElement = document.QuerySelectorAll("meta[property=\"og:description\"]")
                                            .FirstOrDefault();
                    if (descriptionElement != null)
                    {
                        openGraph.Description = descriptionElement.GetAttribute("content");
                    }
                    else
                    {
                        descriptionElement = document.QuerySelectorAll("meta[name=\"Description\"]")
                                                .FirstOrDefault();
                        if (descriptionElement != null)
                        {
                            openGraph.Description = descriptionElement.GetAttribute("content");
                        }
                    }

                    var imageElements = document.QuerySelectorAll("meta[property=\"og:image\"]");
                    foreach (var imageElement in imageElements)
                    {
                        Uri imageUri = null;
                        string imageUrl = imageElement.GetAttribute("content");
                        if (imageUrl != null && Uri.TryCreate(imageUrl, UriKind.Absolute, out imageUri))
                        {
                            openGraph.Image = imageUrl.Replace("http://", "https://");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Unable to fetch open graph information for the url : {url}");
            }

            return openGraph;
        }

        private async Task<string> GetActualUrl(string url, ILogger log)
        {
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
                            if (response.StatusCode == HttpStatusCode.MovedPermanently
                                || response.StatusCode == HttpStatusCode.Found)
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

            return url;
        }
    }
}
