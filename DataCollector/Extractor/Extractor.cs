using DataCollector.Extensions;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Selenium.Extensions;
using Selenium.WebDriver.UndetectedChromeDriver;
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
        private SlDriver _driver;
        public void Init()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var webRequest = new HttpClient();//
                webRequest.DefaultRequestHeaders.Accept.Clear();
                webRequest.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                webRequest.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36");
                webRequest.DefaultRequestHeaders.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                webRequest.DefaultRequestHeaders.Add("accept-encoding", "gzip, deflate, br");
                /*webRequest.DefaultRequestHeaders.Add("accept-language", "en-US,en-CA;q=0.9,en;q=0.8,fr-CA;q=0.7,fr;q=0.6");
                webRequest.DefaultRequestHeaders.Add("cache-control", "max-age=0");
                webRequest.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
                webRequest.DefaultRequestHeaders.Add("sec-fetch-dest", "document");
                webRequest.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate");
                webRequest.DefaultRequestHeaders.Add("sec-fetch-site", "none");
                webRequest.DefaultRequestHeaders.Add("sec-fetch-user", "?1");
                webRequest.DefaultRequestHeaders.Add("upgrade-insecure-requests", "1");*/
                webRequest.DefaultRequestHeaders.Add("referer", "https://help.bigtime.net/hc/en-us");

                var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://help.bigtime.net/hc/en-us/categories/9780094626583-Account-Management"));

                var result = webRequest.SendAsync(request).GetAwaiter().GetResult();
                _filter = new Filter();
                _errors = new List<HtmlNode>();
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("start-maximized"); // open Browser in maximized mode
                options.AddArgument("disable-infobars"); // disabling infobars
                options.AddArgument("--disable-extensions"); // disabling extensions
                options.AddArgument("--disable-gpu"); // applicable to windows os only
                options.AddArgument("--disable-dev-shm-usage"); // overcome limited resource problems
                options.AddArgument("--no-sandbox"); // Bypass OS security model

                _driver = UndetectedChromeDriver.Instance(Headless: true);
                _driver.GoTo("http://help.bigtime.net/hc/en-us/categories/9780094626583-Account-Management");
                Doc = new HtmlDocument();
                Doc.LoadHtml(_driver.PageSource);
                Parse(Doc);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }



        public void Parse(HtmlDocument response)
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
                            Request(url, ParseLinks);
                        }
                    }
                    else
                    {
                        _errors.Add(title_link);
                    }
                }
               
            }
        }

        public void ParseLinks(HtmlDocument response)
        {
            var x = 0;
            foreach (HtmlNode title_link in response.Css(".lt-article-list-item__link"))
            {
                var strTitle = Regex.Match(title_link.OuterHtml, "href=\\\"(.*?)\"");
                if (strTitle.Success)
                {
                    if (strTitle.Groups.Count >= 2)
                    {
                        var url = strTitle.Groups[1].Value;
                        Request(url, ParseArticle);
                    }
                }
                else
                {
                    _errors.Add(title_link);
                }
            }
        }

        public void ParseArticle(HtmlDocument response)
        {
            var x = 0;
            foreach (HtmlNode title_link in response.Css(".lt-article"))
            {
                var strTitle = Regex.Match(title_link.OuterHtml, "href=\\\"(.*?)\"");
                if (strTitle.Success)
                {
                    if (strTitle.Groups.Count >= 2)
                    {
                        var url = strTitle.Groups[1].Value;
                        Request(url, ProcessElements);                     
                    }
                }
                else
                {
                    _errors.Add(title_link);
                }
            }
        }

        public void ProcessElements(HtmlDocument primaryNode)
        {
            //First grab header
            var header = primaryNode.Css("lt-article__title").FirstOrDefault();
            if(header is null) { return; }
            _filter.SetHeader(header?.InnerText);
            HtmlNode body = primaryNode.Css(".lt-article__body").FirstOrDefault();
            _filter.ProcessBody(body);

        }

        private void Request(string url, Action<HtmlDocument> Action)
        {
            Action(CallUrl(url));    
        }


        private HtmlDocument CallUrl(string url)
        {
            try
            {
               var urll = _driver.Url;
               using(var d = UndetectedChromeDriver.Instance(Headless: true))
                {
                    var script = "window.performance.mark('navigationStart');" +
             $"Object.defineProperty(navigator, 'referer', {{value: '{urll}', configurable: true}});" +
             "window.performance.mark('navigationEnd');" +
             "window.performance.measure('navigationTiming', 'navigationStart', 'navigationEnd');" +
             "return performance.getEntriesByName('navigationTiming')[0].toJSON();";

                    var navigationTiming = (Dictionary<string, object>)((IJavaScriptExecutor)d).ExecuteScript(script);

                    var doc = new HtmlDocument();
                    d.GoTo(url);
                    doc.LoadHtml(d.PageSource);
                    Thread.Sleep(3000);
                    return doc;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new HtmlDocument();
            }

        }
        /*
        public static object filter_olympic_2020_titles(object titles)
        {
            titles = (from title in titles
                      where title.Contains("2020") && title.lower().Contains("olympi")
                      select title).ToList();
            return titles;
        }

        // 
        //     Get the wikipedia page given a title
        //     
        public static object get_wiki_page(object title)
        {
            try
            {
                return wikipedia.page(title);
            }
            catch
            {
                return wikipedia.page(e.options[0]);
            }
            catch
            {
                return null;
            }
        }

        // 
        //     Recursively find all the pages that are linked to the Wikipedia titles in the list
        //     
        public static object recursively_find_all_pages(object titles, object titles_so_far = new HashSet<object>())
        {
            var all_pages = new List<object>();
            titles = (new HashSet<object>(titles) - titles_so_far).ToList();
            titles = filter_olympic_2020_titles(titles);
            titles_so_far.update(titles);
            foreach (var title in titles)
            {
                var page = get_wiki_page(title);
                if (page == null)
                {
                    continue;
                }
                all_pages.append(page);
                var new_pages = recursively_find_all_pages(page.links, titles_so_far);
                foreach (var pg in new_pages)
                {
                    if (!(from p in all_pages
                          select p.title).ToList().Contains(pg.title))
                    {
                        all_pages.append(pg);
                    }
                }
                titles_so_far.update(page.links);
            }
            return all_pages;
        }

        public static object pages = recursively_find_all_pages(new List<object> {
            "2020 Summer Olympics"
        });

        static Module()
        {
            pages.Count;
        }*/
    }
}
