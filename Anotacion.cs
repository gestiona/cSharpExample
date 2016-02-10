using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Anotacion
    {
        public Link[] links { get; set; }
        public string code { get; set; }
        public string date { get; set; }
        public string state { get; set; }
        public string originDate { get; set; }
        public string originCode { get; set; }
        public string originOrganization { get; set; }
        public string originRegistryOffice { get; set; }
        public string shortDescription { get; set; }
        public string longDescription { get; set; }
        public string classification { get; set; }
        public string incomeType { get; set; }
        public string deliveryType { get; set; }
        public string type { get; set; }
        public string annulledDate { get; set; }
        public string annulledReason { get; set; }
        public string category { get; set; }
    }
}
