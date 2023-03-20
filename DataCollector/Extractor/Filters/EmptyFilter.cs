using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCollector.Extractor.Filters
{
    internal class EmptyFilter : FilterBase<bool>, IFilter
    {
        public string InitialHeader { get; set; } = "";
        public string Header { get; set; } = "";

        public bool AddContents(List<Tuple<string, string, string, int>> importData)
        {
            return true;
        }

        public bool EvaluateSections()
        {
            return true;
        }


        public bool IsHeader(string str)
        {
            return false;
        }

        public Task<bool> ProcessNodeBody(HtmlNode body)
        {
            return Task.Run(async () => EvaluateSections());
        }

        public bool SaveToLocalCsvFile(string path)
        {
            return true;
        }

        public bool ShouldIgnoreString(string str)
        {
            return true;
        }
    }
}
