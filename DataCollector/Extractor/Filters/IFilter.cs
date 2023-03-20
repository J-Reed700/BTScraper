using HtmlAgilityPack;

namespace DataCollector.Extractor.Filters
{
    internal interface IFilter
    {
        bool EvaluateSections();
        Task<bool> ProcessNodeBody(HtmlNode body);
        bool IsHeader(string str);
        bool SaveToLocalCsvFile(string path);
        bool ShouldIgnoreString(string str);
        bool AddContents(List<Tuple<string, string, string, int>> importData);
        string InitialHeader { get; set; }
        string Header { get; set; }
    }
}