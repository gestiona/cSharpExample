using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class AddonAuthorization
    {
        public string access_token { get; set; }
        public string authorization_date { get; set; }
        public Link[] links { get; set; }
    }
}
