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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var webRequest = new HttpClient();//
            webRequest.DefaultRequestHeaders.Accept.Clear();
            webRequest.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            webRequest.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36");
            webRequest.DefaultRequestHeaders.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            webRequest.DefaultRequestHeaders.Add("accept-encoding", "gzip, deflate, br");
            webRequest.DefaultRequestHeaders.Add("origin", "34.194.114.206");
            /*webRequest.DefaultRequestHeaders.Add("accept-language", "en-US,en-CA;q=0.9,en;q=0.8,fr-CA;q=0.7,fr;q=0.6");
            webRequest.DefaultRequestHeaders.Add("cache-control", "max-age=0");
            webRequest.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            webRequest.DefaultRequestHeaders.Add("sec-fetch-dest", "document");
            webRequest.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate");
            webRequest.DefaultRequestHeaders.Add("sec-fetch-site", "none");
            webRequest.DefaultRequestHeaders.Add("sec-fetch-user", "?1");
            webRequest.DefaultRequestHeaders.Add("upgrade-insecure-requests", "1");*/

            /*PlayWright

            using var playwright = Playwright.CreateAsync().GetAwaiter().GetResult();
            var browser = playwright.Chromium.LaunchAsync(new() { Headless = true }).GetAwaiter().GetResult();
            var page = browser.NewPageAsync().GetAwaiter().GetResult();
            page.GotoAsync("https://playwright.dev/dotnet");
            */
            /* Pupeteer */

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
            var values = response.Css("lt-article-container");
            foreach (HtmlNode title_link in values)
            {
                return await ProcessElements(title_link);
            }
            return true;
        }

        public async Task<bool> ProcessElements(HtmlNode primaryNode)
        {
            //First grab header
            //var header = primaryNode.Css("lt-article__title").FirstOrDefault();
            //if(header is null) { return false; }
            //_filter.SetHeader(header?.InnerText);
            //HtmlNode body = primaryNode.Css(".lt-article__body").FirstOrDefault();
            //;
            var header = primaryNode.Css("lt-article__title").FirstOrDefault();
            if (header is null) { return false; }
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
