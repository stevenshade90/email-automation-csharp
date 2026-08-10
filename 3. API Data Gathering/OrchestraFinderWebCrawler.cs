using HtmlAgilityPack;
using System.Net.Http.Headers;


namespace Orchestra_Finder_Web_Crawler
{
    public class WebCrawler
    {
        public static HttpClient client = new HttpClient();

        public List<string> additionalWebsitePages = new List<string>();
        public List<string> allHtmls = new List<string>();

        static WebCrawler()
        {
            var productHeader = new ProductHeaderValue("MyAppCustom", "1.0.0.0");
            var agent = new ProductInfoHeaderValue(productHeader);
            client.DefaultRequestHeaders.UserAgent.Add(agent);
        }

        public async Task<string> GetEmailFromWebsiteHtml(string primaryWebsite)
        {
            string primaryWebsiteHtmlData = await GetHtmlDataAsync(primaryWebsite);
            additionalWebsitePages = await GetAdditionalWebsitePages(primaryWebsiteHtmlData);

            foreach (string page in additionalWebsitePages)
            {
                string absolute = ToAbsoluteUrl(primaryWebsite, page);
                if (string.IsNullOrEmpty(absolute)) continue;

                string currentHtml = await GetHtmlDataAsync(absolute);
                allHtmls.Add(currentHtml);
            }

            string email = GetEmail(allHtmls);

            return email;
        }

        public static async Task<string> GetHtmlDataAsync(string website)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(website);
                string htmlData = await response.Content.ReadAsStringAsync();

                return htmlData;
            }
            catch
            {
                Console.WriteLine("\t\tFailed to retrieve HTML data from website.");
                throw;
            }
        }

        public static async Task<List<string>> GetAdditionalWebsitePages(string htmlData)
        {
            string[] tags = { "connect", "contact", "about" };

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlData);

            List<string> links = doc.DocumentNode.SelectNodes("//a[@href]")
                ?.Select(a => a.GetAttributeValue("href", string.Empty))
                .Where(href => !string.IsNullOrWhiteSpace(href)
                               && (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                   || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                                   || href.StartsWith("//")
                                   || !href.Contains(':'))) // exclude mailto:, tel:, etc.
                .Where(href => tags.Any(t => href.Contains(t, StringComparison.OrdinalIgnoreCase)))
                .Distinct()
                .ToList()
                ?? new List<string>();

            return links;
        }

        static string GetEmail(List<string> htmlPages)
        {
            var doc = new HtmlDocument();

            foreach (string page in htmlPages)
            {
                doc.LoadHtml(page);

                var emailNode = doc.DocumentNode.SelectSingleNode("//a[starts-with(@href, 'mailto:')]");

                if (emailNode != null)
                {
                    string hrefValue = emailNode.GetAttributeValue("href", string.Empty);

                    hrefValue = hrefValue.Contains("mailto:")
                        ? hrefValue.Replace("mailto:", "")
                        : hrefValue;

                    return hrefValue.Trim();
                }
            }

            return "NO-EMAIL";
        }

        static string ToAbsoluteUrl(string baseUrl, string href)
        {
            //Maybe to switch statement
            if (string.IsNullOrWhiteSpace(href))
            {
                return string.Empty;
            }

            if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return href;
            }

            if (href.StartsWith("//"))
            {
                var uri = new Uri(baseUrl);
                return uri.Scheme + ":" + href;
            }

            if (Uri.TryCreate(new Uri(baseUrl), href, out var result))
            {
                return result.ToString();
            }

            return string.Empty;
        }
    }
}