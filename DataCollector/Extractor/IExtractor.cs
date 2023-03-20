using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCollector.Extractor
{
    public interface IExtractor
    {
        Task<bool> Extract();
        Task<bool> SaveToFile(string path);
        Task<bool> ImportData<T>(T data);
    }
}
