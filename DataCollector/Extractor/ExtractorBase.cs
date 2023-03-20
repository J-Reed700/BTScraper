using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCollector.Extractor
{
    public abstract class ExtractorBase : IExtractor
    {
        public abstract Task<bool> Extract();

        public abstract Task<bool> ImportData<T>(T data);

        public abstract Task<bool> Request(string url, Func<HtmlDocument, Task<bool>> Action);
        public abstract Task<bool> SaveToFile(string path);
    }
}
