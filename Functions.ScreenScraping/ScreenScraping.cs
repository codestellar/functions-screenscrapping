using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Functions.ScreenScraping
{
    public static class ScreenScraping
    {
        public static HttpClient _httpClient = new HttpClient();

        [FunctionName("ScrapDataFromUrl")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                var urlToScrap = data?.urlToScrap;

                // Parse Url
                string html = await _httpClient.GetStringAsync(urlToScrap.ToString());

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                var result = new List<string>();

                // Fetch Data
                switch (urlToScrap.ToString())
                {
                    case "https://global.azurebootcamp.net/locations/":
                        result = doc.DocumentNode
                            .SelectNodes("//td/a")
                            .Where(x => x.Attributes["href"].Value.Contains("locations"))
                            .Select(x => x.InnerText).ToList();
                        break;
                    case "http://dontcodetired.com/blog/archive":
                        result = doc.DocumentNode
                            .Descendants("td")
                            .Where(x => x.Attributes.Contains("class")
                                && x.Attributes["class"].Value.Contains("title"))
                                .Select(x => x.InnerText).ToList();
                        break;
                    default:
                        result = doc.DocumentNode.SelectNodes("//head/meta")
                            .Where(x => x.Attributes.Contains("name")
                                && x.Attributes["name"].Value.ToLower().Contains("keywords"))
                            .Select(x => x.Attributes["content"].Value).ToList();
                        break;
                }

                return new OkObjectResult(result);
            }
            catch (System.Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
