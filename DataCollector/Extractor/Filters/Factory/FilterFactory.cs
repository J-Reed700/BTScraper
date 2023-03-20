using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCollector.Extractor.Filters.Factory
{
    internal static class FilterFactory
    {
        public static IFilter GetEmptyFilter()
        {
            return new EmptyFilter();
        }
        public static IFilter GetKBFilter() { 

            return new KBFilter();
        }
    }
}
