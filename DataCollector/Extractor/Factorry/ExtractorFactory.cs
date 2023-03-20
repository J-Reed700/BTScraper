using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCollector.Extractor.Factorry
{
    public static class ExtractorFactory 
    { 
   
        public static IExtractor GetKnowledgeBaseExtractor(string startUrl, string rootUrl = "https://help.bigtime.net", string chromeBrowserLocation = @"C:\Program Files\Google\Chrome\Application\chrome.exe")
        {
            return new KnowledgeBaseExtractor(startUrl, rootUrl, chromeBrowserLocation);
        }
    }

}
