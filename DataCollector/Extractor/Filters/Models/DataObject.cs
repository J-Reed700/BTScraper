using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCollector.Extractor.Filters.Models
{
    public class DataObject<T>
    {
        private readonly T _value;
        public DataObject(T data) {
            _value = data;
        }
        public T ReturnData()
        {
            return _value;
        }
    }
}
