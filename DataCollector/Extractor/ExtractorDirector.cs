using DataCollector.Extractor.Factorry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCollector.Extractor
{
    public class ExtractorDirector
    {
        private IExtractor _extractor;
        public ExtractorDirector(string startUrl) 
        { 
            _extractor = ExtractorFactory.GetKnowledgeBaseExtractor(startUrl);
        }

        public async Task<bool> ExtractAsync()
        {
            return await _extractor.Extract();
        }

        public async Task<bool> SaveToFile(string filePath)
        {
            return await _extractor.SaveToFile(filePath);
        }

        public async Task<bool> ImportData<T>(T data)
        {
            if (data != null)
            {
                var type = typeof(T);
                if (type == typeof(List<Tuple<string, string, string, int>>))
                {
                    return await _extractor.ImportData(data);
                }
            }
            return false;
        }
    }
}
