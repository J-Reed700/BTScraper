using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCollector.Extractor.Filters
{
    internal abstract class FilterBase<T>
    {
        protected T data;

        public FilterBase()
        {
            if (!typeof(T).IsValueType)
            {
                data = (T)(object)Activator.CreateInstance(typeof(T));
            }
        }

    }
}
