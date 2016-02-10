using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class RestError
    {
        public long code { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public List<string> details { get; set; }
        public string internalCodeError { get; set; }
        public string technicalDetails { get; set; }
    }
}
