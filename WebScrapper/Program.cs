using HtmlAgilityPack;
using System.Collections.Concurrent;

namespace WebScraper
{
    class Program
    {
        static readonly ConcurrentBag<string> scrapedData = new ConcurrentBag<string>();

        static async Task Main(string[] args)
        {
            string url = "https://example.com";

            int maxConcurrentRequests = 5;

            using (var httpClient = new HttpClient())
            {
                var html = await httpClient.GetStringAsync(url);

                var htmlDocument = new HtmlDocument();

                htmlDocument.LoadHtml(html);

                var links = htmlDocument.DocumentNode.Descendants("a");

                SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrentRequests);

                var tasks = new Task[links.Count()];

                int i = 0;

                foreach (var link in links)
                {
                    await semaphore.WaitAsync();

                    tasks[i++] = ScrapeDataAsync(httpClient, link, semaphore);
                }

                await Task.WhenAll(tasks);
            }

            Console.WriteLine("Scraping completed. Scraped data:");

            foreach (var data in scrapedData)
            {
                Console.WriteLine(data);
            }
        }

        static async Task ScrapeDataAsync(HttpClient httpClient, HtmlNode link, SemaphoreSlim semaphore)
        {
            try
            {
                var href = link.GetAttributeValue("href", string.Empty);

                if (!string.IsNullOrEmpty(href))
                {
                    var linkHtml = await httpClient.GetStringAsync(href);

                    var linkHtmlDocument = new HtmlDocument();

                    linkHtmlDocument.LoadHtml(linkHtml);

                    // Extract data from linkHtmlDocument and store it in scrapedData
                    // For example:
                    var data = linkHtmlDocument.DocumentNode.Descendants("div")
                        .Select(div => div.InnerText)
                        .FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(data))
                        scrapedData.Add(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to scrape data from link: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}