using DataCollector.Extensions;
using DataCollector.Extractor.Filters;
using DataCollector.Extractor.Filters.Factory;
using FlareSolverrSharp;
using HtmlAgilityPack;
using Microsoft.Playwright;
using PuppeteerExtraSharp;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerSharp;
using ScraperApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataCollector.Extractor
{
    internal class KnowledgeBaseExtractor : ExtractorBase
    {
        private IFilter _filter;
        private HtmlDocument Doc;
        private List<HtmlNode> _errors;
        private string _rootUrl;
        private string _beginningUrl;
        private string _chromeBrowserLocation;
        private PuppeteerSharp.IPage page;

        public KnowledgeBaseExtractor(string startUrl, string rootUrl = "https://help.bigtime.net", string chromeBrowserLocation = @"C:\Program Files\Google\Chrome\Application\chrome.exe")
        {
            _rootUrl = rootUrl;
            _beginningUrl = startUrl;
            _chromeBrowserLocation = chromeBrowserLocation;
            _filter = FilterFactory.GetKBFilter();
            _errors = new List<HtmlNode>();
             Doc = new HtmlDocument();
        }

        public override async Task<bool> Extract()
        {
            
            var extra = new PuppeteerExtra();

            // Use stealth plugin
            extra.Use(new StealthPlugin());

            // Launch the puppeteer browser with plugins
            var browser = extra.LaunchAsync(new LaunchOptions()
            {
                Headless = true,
                Timeout = 0,
                ExecutablePath = _chromeBrowserLocation,
                
            }).GetAwaiter().GetResult();

            // Create a new page
            page = await browser.NewPageAsync();

            await page.GoToAsync(_rootUrl);

            // Wait 2 second
            await page.WaitForTimeoutAsync(2000);

            var content = await page.GetContentAsync();

             Doc.LoadHtml(content);
             await BeginParse(Doc);
            _filter.EvaluateSections();
            return true;
        }

        public override async Task<bool> SaveToFile(string path)
        {
            return _filter.SaveToLocalCsvFile(path);
        }

        private async Task<bool> BeginParse(HtmlDocument response)
        {
            if (response is not null)
            {
                foreach (HtmlNode title_link in response.Css("lt-block-list-item__link"))
                {
                    var strTitle = Regex.Match(title_link.OuterHtml, "href=\\\"(.*?)\"");
                    if (strTitle.Success)
                    {
                        if (strTitle.Groups.Count >= 2)
                        {
                            var url = strTitle.Groups[1].Value;
                            url = _rootUrl + url;
                            if (!strTitle.Value.Contains("Quick-Start", StringComparison.OrdinalIgnoreCase))
                            {
                                Log(url, "Parse");
                                await Request(url, Parse);
                            }
                        }
                        else
                        {
                            _errors.Add(title_link);
                            return false;
                        }
                    }

                }
                return true;
            }
            return false;
        }

        public override async Task<bool> ImportData<T>(T data)
        {
            if (data is not null)
            {
                var convertedData = (List<Tuple<string, string, string, int>>)(object)data;
                _filter.AddContents(convertedData);
                return true;
            }
            return false;
        }

        private async Task<bool> Parse(HtmlDocument response)
        {
            if (response is not null)
            {
                foreach (HtmlNode title_link in response.Css("see-all-articles"))
                {
                    var strTitle = Regex.Match(title_link.OuterHtml, "href=\\\"(.*?)\"");
                    if (strTitle.Success)
                    {
                        if (strTitle.Groups.Count >= 2)
                        {
                            var url = strTitle.Groups[1].Value;
                            url = _rootUrl + url;
                            if (strTitle.Value.Contains("sections"))
                            {
                                Log(url, "ParseLinks");
                                await Request(url, ParseLinks);
                            }
                        }
                        else
                        {
                            _errors.Add(title_link);
                            return false;
                        }
                    }

                }
                return true;
            }
            return false;
        }

        private async Task<bool> ParseLinks(HtmlDocument response)
        {
            if (response is not null)
            {
                var currentHeader = response.Css("lt-header").FirstOrDefault().InnerText.Trim();
                _filter.InitialHeader = currentHeader;
                foreach (HtmlNode title_link in response.Css("lt-article-list-item__link"))
                {
                    var strTitle = Regex.Match(title_link.OuterHtml, "href=\\\"(.*?)\"");
                    if (strTitle.Success)
                    {
                        if (strTitle.Groups.Count >= 2)
                        {
                            var url = strTitle.Groups[1].Value;
                            url = _rootUrl + url;
                            Log(url, "ParseArticle");
                            await Request(url, ParseArticle);
                        }
                    }
                    else
                    {
                        _errors.Add(title_link);
                    }
                }
                return true;
            }
            return false;
        }

        private async Task<bool> ParseArticle(HtmlDocument response)
        {
            if (response is not null)
            {
                var values = response.Css("lt-article-container");
                foreach (HtmlNode title_link in values)
                {
                    await ProcessElements(title_link);
                }
                var nextLink = response.Css("pagination-next-link").FirstOrDefault();
                if (nextLink != null)
                {
                    var strTitle = Regex.Match(nextLink.OuterHtml, "href=\\\"(.*?)\"");
                    if (strTitle.Success)
                    {
                        if (strTitle.Groups.Count >= 2)
                        {
                            var url = strTitle.Groups[1].Value;
                            url = _rootUrl + url;
                            Log(url, "ParseArticle");
                            await Request(url, ParseArticle);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private async Task<bool> ProcessElements(HtmlNode primaryNode)
        {
            var header = primaryNode.Css("lt-article__title").FirstOrDefault();
            if (header is null && String.IsNullOrEmpty(_filter.Header)) { return false; }
            _filter.Header = header?.InnerText.Trim();
            return await _filter.ProcessNodeBody(primaryNode);

        }

        public override async Task<bool> Request(string url, Func<HtmlDocument,Task<bool>> Action)
        {
            return await Action(await CallUrl(url));    
        }


        private async Task<HtmlDocument> CallUrl(string url)
        {
            try
            {
                // Wait 2 second
                await page.WaitForTimeoutAsync(2000);

                await page.GoToAsync(url);

                var content = await page.GetContentAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(content);
                return doc;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

        }

        private async Task<HtmlDocument> RetryCall(string url)
        {
            return await CallUrl(url);
        }

        private void Log(string url, string method)
        {
            Console.WriteLine($"url: {url} method: {method}");
        }
    }
}
