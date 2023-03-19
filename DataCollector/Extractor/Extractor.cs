using DataCollector.Extensions;
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataCollector.Extractor
{
    public class Extractor 
    {
        private Filter _filter;
        private HtmlDocument Doc;
        private List<HtmlNode> _errors;
        private string _previousUrl;
        private string originIP = "34.194.114.206";
        private PuppeteerSharp.IPage page;
        private PuppeteerSharp.IBrowser browser;

        public async Task<bool> Init()
        {
            

            var extra = new PuppeteerExtra();

            // Use stealth plugin
            extra.Use(new StealthPlugin());

            // Launch the puppeteer browser with plugins
            browser = extra.LaunchAsync(new LaunchOptions()
            {
                Headless = true,
                Timeout = 0,
                ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                
            }).GetAwaiter().GetResult();

            // Create a new page
            page = await browser.NewPageAsync();

            await page.GoToAsync($"http://help.bigtime.net/hc/en-us/categories/9780094626583-Account-Management");

            // Wait 2 second
            await page.WaitForTimeoutAsync(2000);

            var content = await page.GetContentAsync();

            //var content = client.GetStringAsync("https://help.bigtime.net/hc/en-us/categories/9780094626583-Account-Management").GetAwaiter();
            Console.WriteLine(content);

            _filter = new Filter();
            _errors = new List<HtmlNode>();
             Doc = new HtmlDocument();
             Doc.LoadHtml(content);
             await Parse(Doc);
            _filter.EvaluateSections();
            return true;
        }



        public async Task<bool> Parse(HtmlDocument response)
        {
            foreach (HtmlNode title_link in response.Css("see-all-articles"))
            {
                var strTitle = Regex.Match(title_link.OuterHtml, "href=\\\"(.*?)\"");
                if(strTitle.Success)
                {
                    if (strTitle.Groups.Count >= 2)
                    {
                        var url = strTitle.Groups[1].Value; 
                        url = "https://help.bigtime.net" + url;
                        if (strTitle.Value.Contains("sections"))
                        {
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

        public async Task<bool> ParseLinks(HtmlDocument response)
        {
            var x = 0;
            foreach (HtmlNode title_link in response.Css("lt-article-list-item__link"))
            {
                var strTitle = Regex.Match(title_link.OuterHtml, "href=\\\"(.*?)\"");
                if (strTitle.Success)
                {
                    if (strTitle.Groups.Count >= 2)
                    {
                        var url = strTitle.Groups[1].Value;
                        url = "https://help.bigtime.net" + url;
                        return await Request(url, ParseArticle);
                    }
                }
                else
                {
                    _errors.Add(title_link);
                    return false;
                }
            }
            return true;
        }

        public async Task<bool> ParseArticle(HtmlDocument response)
        {
            var x = 0;
            var header = response.Css("lt-article__title").FirstOrDefault();
            if (header is not null) { _filter.SetHeader(header?.InnerText.Trim()); }
            var values = response.Css("lt-article-container");
            foreach (HtmlNode title_link in values)
            {
                return await ProcessElements(title_link);
            }
            return true;
        }

        public async Task<bool> ProcessElements(HtmlNode primaryNode)
        {
            var header = primaryNode.Css("lt-article__title").FirstOrDefault();
            if (header is null && String.IsNullOrEmpty(_filter.GetHeader())) { return false; }
            _filter.SetHeader(header?.InnerText.Trim());
            return await _filter.ProcessBody(primaryNode);

        }

        private async Task<bool> Request(string url, Func<HtmlDocument,Task<bool>> Action)
        {
            return await Action(await CallUrl(url));    
        }


        private async Task<HtmlDocument> CallUrl(string url)
        {
            // Wait 2 second
            await page.WaitForTimeoutAsync(2000);

            await page.GoToAsync(url);

            var content = await page.GetContentAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            return doc;
            
        }
    }
}
