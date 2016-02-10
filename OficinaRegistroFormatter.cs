using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;

namespace ConsoleApplication1
{
    class OficinaRegistroFormatter : JsonMediaTypeFormatter
    {
        public OficinaRegistroFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/vnd.gestiona.registry-offices+json"));
        }

        public override bool CanReadType(Type type)
        {
            return base.CanReadType(type) || type == typeof(OficinaRegistroContent);
        }
    }
}
