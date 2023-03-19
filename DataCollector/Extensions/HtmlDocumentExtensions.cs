using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCollector.Extensions
{
    public static class HtmlDocumentExtensions
    {
        public static IEnumerable<HtmlNode> Css(this HtmlDocument htmlDocument, string dClass, bool exactMatch = false)
        {
            return exactMatch ? htmlDocument.DocumentNode.Descendants().Where(x => x.Attributes is not null && x.Attributes.Contains("class") && String.Equals(x.Attributes["class"].Value,dClass))
                : htmlDocument.DocumentNode.Descendants().Where(x => x.Attributes is not null && x.Attributes.Contains("class") && x.Attributes["class"].Value.Contains(dClass));
        }

    }
}
